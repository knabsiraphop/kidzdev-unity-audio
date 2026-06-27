using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KidzDev.Unity.AddressablesToolkit;
using UnityEngine;

namespace KidzDev.Unity.Audio.Samples
{
    /// <summary>
    /// ISoundClipLoader backed by IAssetLoader from com.kidzdev.unity.addressables-toolkit.
    ///
    /// Each unique key calls the toolkit loader exactly once; repeat calls return the cached clip
    /// (fixing the ref-count leak where PlaySfx x100 would accumulate 100 handles).
    /// Concurrent first-loads for the same key share one in-flight request (C3 race fix).
    /// Release decrements the single handle, matching the single load.
    ///
    /// Usage — pass at AudioManager construction:
    /// <code>
    /// var loader  = new AddressablesSoundClipLoader();
    /// var manager = new AudioManager(loader);
    /// AudioSystem.Default = manager;
    /// </code>
    /// </summary>
    public sealed class AddressablesSoundClipLoader : ISoundClipLoader
    {
        readonly IAssetLoader _assetLoader;
        readonly Dictionary<string, AudioClip> _cache   = new();
        readonly Dictionary<string, UniTaskCompletionSource<AudioClip>> _inflight = new();

        public AddressablesSoundClipLoader(IAssetLoader assetLoader = null)
        {
            _assetLoader = assetLoader ?? AssetLoader.Default;
        }

        public async UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
        {
            // Fast path: clip already loaded (one toolkit handle held)
            if (_cache.TryGetValue(key, out var cached)) return cached;

            // Concurrent callers for the same key share one in-flight request
            if (_inflight.TryGetValue(key, out var pending)) return await pending.Task;

            var tcs = new UniTaskCompletionSource<AudioClip>();
            _inflight[key] = tcs;

            AudioClip clip;
            try
            {
                clip = await _assetLoader.LoadAsync<AudioClip>(key, ct);
                if (clip == null)
                    Debug.LogWarning($"[Audio] Addressables clip not found: '{key}'");
                if (clip != null) _cache[key] = clip;
            }
            catch (OperationCanceledException)
            {
                _inflight.Remove(key);
                tcs.TrySetCanceled();
                throw;
            }
            catch (Exception ex)
            {
                _inflight.Remove(key);
                tcs.TrySetException(ex);
                throw;
            }

            _inflight.Remove(key);
            tcs.TrySetResult(clip);
            return clip;
        }

        public void Release(string key)
        {
            if (!_cache.ContainsKey(key)) return;
            _cache.Remove(key);
            _assetLoader.Release<AudioClip>(key); // exactly one Release per one LoadAsync call to the toolkit
        }

        public void ReleaseAll()
        {
            foreach (var key in _cache.Keys)
                _assetLoader.Release<AudioClip>(key);
            _cache.Clear();
        }
    }
}