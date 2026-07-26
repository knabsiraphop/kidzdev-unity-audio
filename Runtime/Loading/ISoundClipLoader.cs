using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Resolves a <see cref="SoundEntry.Key"/> to an <see cref="AudioClip"/>. Swapping the
    /// implementation is what lets the same library work against Resources or Addressables —
    /// <see cref="ResourcesSoundClipLoader"/> is the default; an Addressables adapter ships in the
    /// package samples.
    /// </summary>
    public interface ISoundClipLoader
    {
        UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default);
        void Release(string key);
        void ReleaseAll();
    }
}
