using UnityEngine;

namespace KidzDev.Unity.Audio
{
    [AddComponentMenu("KidzDev/Audio/SFX Player")]
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] string _soundKey;
        [SerializeField] bool _playOnEnable = true;
        [SerializeField] bool _loop;

        void OnEnable()
        {
            if (!_playOnEnable) return;
            if (_loop) AudioSystem.PlayLoopSfx(_soundKey);
            else AudioSystem.PlaySfx(_soundKey);
        }

        public void Play()
        {
            if (_loop) AudioSystem.PlayLoopSfx(_soundKey);
            else AudioSystem.PlaySfx(_soundKey);
        }

        void OnDisable()
        {
            if (_loop) AudioSystem.StopLoopSfx(_soundKey);
        }
    }
}
