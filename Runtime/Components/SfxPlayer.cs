using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Plays one SFX key from the Inspector — on enable and/or from a UI event via
    /// <see cref="Play"/>. When set to loop, the loop is stopped again on disable.
    /// </summary>
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

        /// <summary>Plays (or starts looping) the configured key. Safe to wire directly to a Button's onClick.</summary>
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
