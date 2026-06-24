using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    public sealed class ResourcesSoundClipLoader : ISoundClipLoader
    {
        readonly Dictionary<string, AudioClip> _cache = new();

        public async UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var request = Resources.LoadAsync<AudioClip>(key);
            await request.ToUniTask(cancellationToken: ct);

            var clip = request.asset as AudioClip;
            if (clip != null)
                _cache[key] = clip;
            else
                Debug.LogWarning($"[Audio] Clip not found at Resources path: '{key}'");

            return clip;
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