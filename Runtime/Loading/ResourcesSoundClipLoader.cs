using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Default <see cref="ISoundClipLoader"/> — treats each key as a path under a <c>Resources</c>
    /// folder and caches what it loads. Concurrent requests for the same key share one load.
    /// Keys that don't resolve log a warning and play nothing.
    /// </summary>
    /// <remarks>
    /// The shared load deliberately ignores any individual caller's cancellation token: one caller
    /// cancelling must not abort a load other callers are still waiting on. Each caller's token is
    /// attached to its own wait instead, so cancelling stops that caller waiting without killing
    /// the underlying request.
    /// </remarks>
    public sealed class ResourcesSoundClipLoader : ISoundClipLoader
    {
        readonly Dictionary<string, AudioClip> _cache = new();
        readonly Dictionary<string, UniTask<AudioClip>> _inFlight = new();

        public UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(key, out var cached))
                return UniTask.FromResult(cached);

            if (!_inFlight.TryGetValue(key, out var shared))
            {
                shared = LoadCoreAsync(key).Preserve();
                _inFlight[key] = shared;
            }

            return ct.CanBeCanceled ? shared.AttachExternalCancellation(ct) : shared;
        }

        async UniTask<AudioClip> LoadCoreAsync(string key)
        {
            try
            {
                var request = Resources.LoadAsync<AudioClip>(key);
                await request.ToUniTask();

                var clip = request.asset as AudioClip;
                if (clip != null)
                    _cache[key] = clip;
                else
                    Debug.LogWarning($"[Audio] Clip not found at Resources path: '{key}'");

                return clip;
            }
            finally
            {
                _inFlight.Remove(key);
            }
        }

        public void Release(string key)
        {
            if (_cache.TryGetValue(key, out var clip))
            {
                if (clip != null) Resources.UnloadAsset(clip);
                _cache.Remove(key);
            }
        }

        public void ReleaseAll()
        {
            foreach (var clip in _cache.Values)
                if (clip != null) Resources.UnloadAsset(clip);
            _cache.Clear();
        }
    }
}
