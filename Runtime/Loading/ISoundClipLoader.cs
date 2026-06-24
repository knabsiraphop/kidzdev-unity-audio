using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    public interface ISoundClipLoader
    {
        UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default);
        void Release(string key);
        void ReleaseAll();
    }
}
