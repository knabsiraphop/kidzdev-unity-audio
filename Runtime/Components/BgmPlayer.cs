using UnityEngine;

namespace KidzDev.Unity.Audio
{
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

        public void Play() => AudioSystem.PlayBgm(_soundKey);

        void OnDestroy()
        {
            if (_stopOnDestroy) AudioSystem.StopBgm();
        }
    }
}
