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
        int _poolSize;
        int _poolCap;

        /// <summary>
        /// Active keyed loops. The entry volume is stored alongside the source so
        /// <see cref="SetVolume"/> can recompose master*category*entry instead of overwriting the
        /// entry factor.
        /// </summary>
        readonly Dictionary<string, LoopHandle> _loopSources = new();

        float _volume = 1f;

        readonly struct LoopHandle
        {
            internal readonly AudioSource Source;
            internal readonly float EntryVolume;

            internal LoopHandle(AudioSource source, float entryVolume)
            {
                Source      = source;
                EntryVolume = entryVolume;
            }
        }

        internal SfxChannel(Transform root, int poolSize, int poolCap)
        {
            _root     = root;
            _poolSize = poolSize;
            _poolCap  = poolCap;
            _pool     = new AudioSourcePool(root, "SfxSource", poolSize, poolCap);
        }

        internal void SetMixerGroup(AudioMixerGroup group)
        {
            _mixerGroup = group;
            _pool.SetMixerGroup(group);
        }

        /// <summary>
        /// Recreates the pool with new sizes. A no-op when the sizes already match, so a repeated
        /// <c>Configure()</c> cannot destroy live sources out from under an in-flight warm or
        /// active playback.
        /// </summary>
        internal void Reconfigure(int initial, int cap)
        {
            if (initial == _poolSize && cap == _poolCap) return;

            _poolSize = initial;
            _poolCap  = cap;
            StopAllLoops();
            _pool.Dispose();
            _pool = new AudioSourcePool(_root, "SfxSource", initial, cap);
            if (_mixerGroup != null) _pool.SetMixerGroup(_mixerGroup);
        }

        internal void SetVolume(float v01)
        {
            _volume = v01;
            foreach (var handle in _loopSources.Values)
                if (handle.Source != null) handle.Source.volume = handle.EntryVolume * v01;
        }

        /// <summary>One-shot on a shared source — <c>PlayOneShot</c> never stomps a clip already playing there.</summary>
        internal void PlayOneShot(AudioClip clip, float entryVolume)
        {
            if (clip == null) return;
            var src = _pool.Rent();
            if (src == null) return;
            src.PlayOneShot(clip, entryVolume * _volume);
        }

        /// <summary>One-shot from an already-loaded clip, bypassing the library.</summary>
        internal void PlayDirect(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var src = _pool.Rent();
            if (src == null) return;
            src.PlayOneShot(clip, volume * _volume);
        }

        /// <summary>
        /// Ramps pitch across the clip using one yield loop rather than a per-source
        /// <c>MonoBehaviour.Update</c>. Rents exclusively because it assigns <c>.clip</c> and resets
        /// pitch on completion, either of which would corrupt another borrower's source.
        /// </summary>
        internal async UniTaskVoid PlayPitchRamp(
            AudioClip clip, float entryVolume,
            float startPitch, float endPitch, float duration,
            CancellationToken ct)
        {
            if (clip == null) return;
            if (!_pool.TryRentExclusive(out var src)) return;

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
                if (src != null)
                {
                    src.Stop();
                    src.clip  = null;
                    src.pitch = 1f;
                }
                _pool.ReturnExclusive(src);
            }
        }

        /// <summary>Positional one-shot — borrows a shared source, sets it to 3D, and reverts it to 2D once the clip ends.</summary>
        internal void PlayAt(AudioClip clip, float entryVolume, Vector3 worldPos, CancellationToken lifetimeCt)
        {
            if (clip == null) return;
            var src = _pool.Rent();
            if (src == null) return;
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

        /// <summary>
        /// Starts a keyed loop. Rents exclusively so the loop owns its source until
        /// <see cref="StopLoop"/> — no one-shot or pitch ramp can reassign its clip underneath it.
        /// Warns and does nothing when the pool has no source to spare.
        /// </summary>
        internal void PlayLoop(string key, AudioClip clip, float entryVolume)
        {
            if (clip == null || _loopSources.ContainsKey(key)) return;
            if (!_pool.TryRentExclusive(out var src))
            {
                Debug.LogWarning($"[Audio] SFX pool exhausted — cannot start loop '{key}'. Raise the pool cap in AudioServiceSettings.");
                return;
            }

            src.clip   = clip;
            src.loop   = true;
            src.volume = entryVolume * _volume;
            src.Play();
            _loopSources[key] = new LoopHandle(src, entryVolume);
        }

        internal void StopLoop(string key)
        {
            if (!_loopSources.TryGetValue(key, out var handle)) return;
            ReleaseLoopSource(handle.Source);
            _loopSources.Remove(key);
        }

        internal void StopAllLoops()
        {
            foreach (var handle in _loopSources.Values)
                ReleaseLoopSource(handle.Source);
            _loopSources.Clear();
        }

        /// <summary>Stops a loop's source and hands it back to the shared pool. Tolerates a destroyed source.</summary>
        void ReleaseLoopSource(AudioSource src)
        {
            if (src != null)
            {
                src.Stop();
                src.clip = null;
                src.loop = false;
            }
            _pool.ReturnExclusive(src);
        }

        public void Dispose()
        {
            StopAllLoops();
            _pool.Dispose();
        }
    }
}
