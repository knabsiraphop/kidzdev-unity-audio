using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace KidzDev.Unity.Audio.Samples
{
    /// <summary>
    /// ISoundClipLoader backed by Unity Addressables.
    ///
    /// Each unique key loads exactly one Addressables handle; repeat calls return the
    /// cached clip without a second load. Concurrent first-loads for the same key share
    /// one in-flight request. Release decrements the single handle.
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
        readonly Dictionary<string, AudioClip> _cache = new();
        readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _handles = new();
        readonly Dictionary<string, UniTaskCompletionSource<AudioClip>> _inflight = new();

        public async UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;

            if (_inflight.TryGetValue(key, out var pending)) return await pending.Task;

            var tcs = new UniTaskCompletionSource<AudioClip>();
            _inflight[key] = tcs;

            try
            {
                var handle = Addressables.LoadAssetAsync<AudioClip>(key);

                AudioClip clip;
                try
                {
                    // Await the load, then read the typed result off the handle. Reading handle.Result avoids
                    // binding to the non-generic AsyncOperationHandle.ToUniTask() overload (which yields void).
                    await handle.ToUniTask(cancellationToken: ct);
                    clip = handle.Result;
                }
                catch
                {
                    Addressables.Release(handle);
                    throw;
                }

                if (clip != null)
                {
                    _cache[key] = clip;
                    _handles[key] = handle;
                }
                else
                {
                    Addressables.Release(handle);
                    Debug.LogWarning($"[Audio] Addressables clip not found: '{key}'");
                }

                _inflight.Remove(key);
                tcs.TrySetResult(clip);
                return clip;
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
        }

        public void Release(string key)
        {
            if (!_cache.Remove(key)) return;
            if (_handles.TryGetValue(key, out var handle))
            {
                _handles.Remove(key);
                Addressables.Release(handle);
            }
        }

        public void ReleaseAll()
        {
            foreach (var key in new List<string>(_cache.Keys))
                Release(key);
        }
    }
}
