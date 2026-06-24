using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace KidzDev.Unity.Audio
{
    internal sealed class SfxChannel : IDisposable
    {
        readonly Transform _root;
        AudioSourcePool _pool;
        AudioMixerGroup _mixerGroup;
        readonly Dictionary<string, AudioSource> _loopSources = new();
        float _volume = 1f;

        internal SfxChannel(Transform root, int poolSize, int poolCap)
        {
            _root = root;
            _pool = new AudioSourcePool(root, "SfxSource", poolSize, poolCap);
        }

        internal void SetMixerGroup(AudioMixerGroup group)
        {
            _mixerGroup = group;
            _pool.SetMixerGroup(group);
        }

        // Recreates the pool with new sizes. Safe when called before any audio plays (i.e. in Configure).
        internal void Reconfigure(int initial, int cap)
        {
            StopAllLoops();
            _pool.Dispose();
            _pool = new AudioSourcePool(_root, "SfxSource", initial, cap);
            if (_mixerGroup != null) _pool.SetMixerGroup(_mixerGroup);
        }

        internal void SetVolume(float v01)
        {
            _volume = v01;
            foreach (var src in _loopSources.Values)
                if (src != null) src.volume = v01;
        }

        // One-shot: PlayOneShot never stomps an in-progress clip on the same source.
        internal void PlayOneShot(AudioClip clip, float entryVolume)
        {
            if (clip == null) return;
            _pool.Rent().PlayOneShot(clip, entryVolume * _volume);
        }

        // Direct AudioClip overload (varisoft.audio pattern).
        internal void PlayDirect(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _pool.Rent().PlayOneShot(clip, volume * _volume);
        }

        // Pitch ramp: one UniTask.Delay per ramp, no per-source MonoBehaviour Update.
        internal async UniTaskVoid PlayPitchRamp(
            AudioClip clip, float entryVolume,
            float startPitch, float endPitch, float duration,
            CancellationToken ct)
        {
            if (clip == null) return;
            var src = _pool.Rent();
            src.clip   = clip;
            src.loop   = false;
            src.volume = entryVolume * _volume;
            src.pitch  = startPitch;
            src.Play();

            float elapsed = 0f;

            try
            {
                while (elapsed < duration && src.isPlaying)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    elapsed  += Time.unscaledDeltaTime;
                    src.pitch = Mathf.Lerp(startPitch, endPitch, Mathf.Min(elapsed / duration, 1f));
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                src.Stop();
                src.clip  = null;
                src.pitch = 1f;
            }
        }

        // 3D one-shot: borrow a pool source, set to 3D, reset after clip ends.
        internal void PlayAt(AudioClip clip, float entryVolume, Vector3 worldPos, CancellationToken lifetimeCt)
        {
            if (clip == null) return;
            var src = _pool.Rent();
            src.transform.position = worldPos;
            src.spatialBlend = 1f;
            src.PlayOneShot(clip, entryVolume * _volume);
            ResetSpatialAfterClip(src, clip.length, lifetimeCt).Forget();
        }

        async UniTaskVoid ResetSpatialAfterClip(AudioSource src, float clipLength, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(clipLength + 0.05f), ignoreTimeScale: true, cancellationToken: ct);
            if (src == null) return;
            src.spatialBlend = 0f;
            src.transform.localPosition = Vector3.zero;
        }

        internal void PlayLoop(string key, AudioClip clip, float entryVolume)
        {
            if (clip == null || _loopSources.ContainsKey(key)) return;
            var src = _pool.Rent();
            src.clip   = clip;
            src.loop   = true;
            src.volume = entryVolume * _volume;
            src.Play();
            _loopSources[key] = src;
        }

        internal void StopLoop(string key)
        {
            if (!_loopSources.TryGetValue(key, out var src)) return;
            src.Stop();
            src.clip = null;
            src.loop = false;
            _loopSources.Remove(key);
        }

        internal void StopAllLoops()
        {
            foreach (var src in _loopSources.Values)
                if (src != null) { src.Stop(); src.clip = null; src.loop = false; }
            _loopSources.Clear();
        }

        public void Dispose()
        {
            foreach (var src in _loopSources.Values)
                if (src != null) { src.Stop(); src.clip = null; }
            _loopSources.Clear();
            _pool.Dispose();
        }
    }
}
