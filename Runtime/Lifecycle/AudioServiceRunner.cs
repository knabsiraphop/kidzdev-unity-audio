using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Convenience MonoBehaviour that owns an <see cref="AudioManager"/>, auto-configures/initializes
    /// it on Awake, forwards application lifecycle events, and wires its cancellation token so all
    /// in-flight fades cancel cleanly when the runner is destroyed.
    ///
    /// Place on a DontDestroyOnLoad GameObject, or let <see cref="AudioSystem.Default"/> create one
    /// automatically via <see cref="GetOrCreate"/>.
    /// </summary>
    [AddComponentMenu("KidzDev/Audio/Audio Service Runner")]
    public sealed class AudioServiceRunner : MonoBehaviour
    {
        [SerializeField] AudioServiceSettings _settings;
        [SerializeField] bool _autoInitialize = true;

        AudioManager _manager;

        public AudioManager Manager => _manager;

        void Awake()
        {
            _manager = new AudioManager();
            _manager.SetLifetimeCancellationToken(destroyCancellationToken);
            _manager.Configure(_settings);

            // Register as the global default so AudioSystem.Default resolves to this manager.
            AudioSystem.Default = _manager;

            if (_autoInitialize)
                _manager.InitializeAsync(destroyCancellationToken).Forget();
        }

        void OnApplicationPause(bool paused) => _manager?.OnApplicationPause(paused);
        void OnApplicationFocus(bool focus)  => _manager?.OnApplicationPause(!focus);

        void OnDestroy()
        {
            var m = _manager;
            _manager?.Dispose();
            _manager = null;
            // ClearDefault compares against the backing field directly — avoids triggering GetOrCreate.
            AudioSystem.ClearDefault(m);
        }

        // ── Factory ───────────────────────────────────────────────────────────────────

        internal static IAudioService GetOrCreate()
        {
            var runner = FindAnyObjectByType<AudioServiceRunner>();
            if (runner != null) return runner.Manager;

            var go = new GameObject("[AudioServiceRunner]");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            runner = go.AddComponent<AudioServiceRunner>();
            return runner.Manager;
        }
    }
}
