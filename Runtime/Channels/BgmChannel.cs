using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace KidzDev.Unity.Audio
{
    internal sealed class BgmChannel : IDisposable
    {
        AudioSource _current;
        AudioSource _previous;
        float _volume = 1f;
        bool _paused;
        CancellationTokenSource _fadeCts;

        internal BgmChannel(Transform root)
        {
            _current  = CreateSource(root, "BgmSource_A");
            _previous = CreateSource(root, "BgmSource_B");
        }

        internal void SetMixerGroup(AudioMixerGroup group)
        {
            _current.outputAudioMixerGroup  = group;
            _previous.outputAudioMixerGroup = group;
        }

        // Called by AudioManager after superseded-load guard passes.
        internal async UniTask PlayAsync(AudioClip clip, SoundEntry entry, float fadeDuration, CancellationToken lifetimeCt)
        {
            CancelFade();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt);
            var ct = _fadeCts.Token;

            // Swap: new clip plays on _previous slot, old on _current.
            (_current, _previous) = (_previous, _current);

            var newSrc = _current;
            var oldSrc = _previous;

            newSrc.clip  = clip;
            newSrc.loop  = entry?.Loop ?? true;
            newSrc.pitch = entry?.Pitch ?? 1f;
            if (entry != null && entry.BeginTime > 0f) newSrc.time = entry.BeginTime;
            newSrc.volume = 0f;
            newSrc.Play();
            if (_paused) newSrc.Pause();

            float oldStartVol = oldSrc.volume;
            float inv = fadeDuration > 0f ? 1f / fadeDuration : float.MaxValue;
            float elapsed = 0f;

            try
            {
                while (elapsed < fadeDuration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Min(elapsed * inv, 1f);
                    newSrc.volume = t * _volume;
                    if (oldSrc.isPlaying) oldSrc.volume = (1f - t) * oldStartVol;
                }
            }
            catch (OperationCanceledException) { return; }

            newSrc.volume = _volume;
            oldSrc.Stop();
            oldSrc.clip = null;
        }

        internal void PlayDirect(AudioClip clip, bool loop)
        {
            CancelFade();
            _current.Stop();
            _current.clip   = clip;
            _current.loop   = loop;
            _current.volume = _volume;
            _current.Play();
        }

        internal async UniTask StopAsync(float fadeDuration, CancellationToken lifetimeCt)
        {
            CancelFade();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt);
            var ct = _fadeCts.Token;

            var src       = _current;
            float startVol = src.volume;
            float inv      = fadeDuration > 0f ? 1f / fadeDuration : float.MaxValue;
            float elapsed  = 0f;

            try
            {
                while (elapsed < fadeDuration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    elapsed += Time.unscaledDeltaTime;
                    src.volume = Mathf.Lerp(startVol, 0f, Mathf.Min(elapsed * inv, 1f));
                }
            }
            catch (OperationCanceledException) { return; }

            src.Stop();
            src.clip   = null;
            src.volume = 0f;
        }

        internal void Pause()
        {
            _paused = true;
            if (_current.isPlaying) _current.Pause();
        }

        internal void Resume()
        {
            _paused = false;
            if (!_current.isPlaying && _current.clip != null) _current.UnPause();
        }

        internal void SetVolume(float v01)
        {
            _volume = v01;
            if (_current.isPlaying) _current.volume = v01;
        }

        void CancelFade()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = null;
        }

        static AudioSource CreateSource(Transform root, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.loop         = true;
            src.spatialBlend = 0f;
            src.volume       = 0f;
            return src;
        }

        public void Dispose()
        {
            CancelFade();
            if (_current  != null) UnityEngine.Object.Destroy(_current.gameObject);
            if (_previous != null) UnityEngine.Object.Destroy(_previous.gameObject);
        }
    }
}
