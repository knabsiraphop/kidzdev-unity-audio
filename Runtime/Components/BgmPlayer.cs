using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Starts one BGM key from the Inspector on <c>Start</c>, optionally stopping it when this
    /// object is destroyed — handy for per-scene music without any glue code.
    /// </summary>
    [AddComponentMenu("KidzDev/Audio/BGM Player")]
    public sealed class BgmPlayer : MonoBehaviour
    {
        [SerializeField] string _soundKey;
        [SerializeField] bool _playOnStart   = true;
        [SerializeField] bool _stopOnDestroy = true;

        void Start()
        {
            if (_playOnStart) AudioSystem.PlayBgm(_soundKey);
        }

        /// <summary>Crossfades to the configured key. Safe to wire directly to a Button's onClick.</summary>
        public void Play() => AudioSystem.PlayBgm(_soundKey);

        void OnDestroy()
        {
            if (_stopOnDestroy) AudioSystem.StopBgm();
        }
    }
}
