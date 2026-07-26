using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace KidzDev.Unity.Audio
{
    internal sealed class AmbienceChannel : IDisposable
    {
        readonly AudioSource _source;
        CancellationTokenSource _fadeCts;
        string _currentKey;
        float _volume = 1f;
        // Per-entry volume of the current clip, kept so SetVolume can recompose
        // master*category*entry instead of stomping the entry factor.
        float _entryVolume = 1f;

        internal string CurrentKey => _currentKey;

        float TargetVolume => _entryVolume * _volume;

        internal AmbienceChannel(Transform root)
        {
            var go = new GameObject("AmbienceSource");
            go.transform.SetParent(root, false);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake  = false;
            _source.loop         = true;
            _source.spatialBlend = 0f;
        }

        internal void SetMixerGroup(AudioMixerGroup group)
            => _source.outputAudioMixerGroup = group;

        internal void SetVolume(float v01)
        {
            _volume = v01;
            if (_source != null && _source.isPlaying) _source.volume = TargetVolume;
        }

        internal async UniTask PlayAsync(
            string key, AudioClip clip, SoundEntry entry,
            float fadeDuration, CancellationToken lifetimeCt)
        {
            if (clip == null || key == _currentKey) return;

            CancelFade();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt);
            var ct = _fadeCts.Token;

            // ── Fade out current ─────────────────────────────────────────────────────
            if (_source.isPlaying && fadeDuration > 0f)
            {
                float startVol = _source.volume;
                float elapsed  = 0f;
                try
                {
                    while (elapsed < fadeDuration)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        elapsed        += Time.unscaledDeltaTime;
                        _source.volume  = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                    }
                }
                catch (OperationCanceledException) { return; }
            }

            // ── Switch clip ──────────────────────────────────────────────────────────
            // Adopt the new entry's volume only here — the fade-out above still belongs
            // to the outgoing clip.
            _entryVolume   = entry?.Volume ?? 1f;
            _currentKey    = key;
            _source.Stop();
            _source.clip   = clip;
            _source.loop   = true;
            _source.pitch  = entry?.Pitch ?? 1f;
            _source.volume = fadeDuration > 0f ? 0f : TargetVolume;
            _source.Play();

            // ── Fade in new ──────────────────────────────────────────────────────────
            if (fadeDuration > 0f)
            {
                float elapsed = 0f;
                try
                {
                    while (elapsed < fadeDuration)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        elapsed        += Time.unscaledDeltaTime;
                        // Read TargetVolume per frame so a mid-fade SetVolume isn't overwritten.
                        _source.volume  = Mathf.Lerp(0f, TargetVolume, elapsed / fadeDuration);
                    }
                    _source.volume = TargetVolume;
                }
                catch (OperationCanceledException) { }
            }
        }

        // Fades out then stops. Use fadeDuration=0 for immediate stop.
        internal async UniTask StopAsync(float fadeDuration, CancellationToken lifetimeCt)
        {
            _currentKey = null;

            if (_source == null || !_source.isPlaying)
            {
                CancelFade();
                return;
            }

            CancelFade();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt);
            var ct = _fadeCts.Token;

            if (fadeDuration > 0f)
            {
                float startVol = _source.volume;
                float elapsed  = 0f;
                try
                {
                    while (elapsed < fadeDuration)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        elapsed        += Time.unscaledDeltaTime;
                        _source.volume  = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                    }
                }
                catch (OperationCanceledException) { return; }
            }

            if (_source == null) return;
            _source.Stop();
            _source.clip   = null;
            _source.volume = 0f;
        }

        // Immediate stop — used by Release() and null-guard paths.
        internal void Stop()
        {
            _currentKey = null;
            CancelFade();
            if (_source == null) return;
            _source.Stop();
            _source.clip = null;
        }

        public void Dispose()
        {
            Stop();
            if (_source != null) UnityEngine.Object.Destroy(_source.gameObject);
        }

        void CancelFade()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = null;
        }
    }
}
