using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KidzDev.Unity.Audio.Demo
{
    public sealed class AudioDemoController : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] TMP_Text _statusLabel;

        [Header("BGM")]
        [SerializeField] string _bgmKeyA = "Audio/bgm_a";
        [SerializeField] string _bgmKeyB = "Audio/bgm_b";
        [SerializeField] TMP_Text _bgmLabel;

        [Header("SFX")]
        [SerializeField] string _sfxClickKey = "Audio/sfx_click";
        [SerializeField] string _sfx3dKey    = "Audio/sfx_3d";
        [SerializeField] string _sfxLoopKey  = "Audio/sfx_loop";
        [SerializeField] Transform _sfx3dTarget;

        [Header("Ambience")]
        [SerializeField] string _ambienceKey = "Audio/amb_wind";
        [SerializeField] TMP_Text _ambienceLabel;

        [Header("Volume Sliders")]
        [SerializeField] Slider _masterSlider;
        [SerializeField] Slider _bgmSlider;
        [SerializeField] Slider _sfxSlider;
        [SerializeField] TMP_Text _masterLabel;
        [SerializeField] TMP_Text _bgmVolLabel;
        [SerializeField] TMP_Text _sfxVolLabel;

        bool _loopPlaying;
        bool _ambiencePlaying;
        int _bgmRequest;

        async void Start()
        {
            SetStatus("Initializing…");

            AudioSystem.Configure();
            await AudioSystem.InitializeAsync(destroyCancellationToken);

            SetStatus("Ready");

            // Restore slider positions from persisted volumes
            if (_masterSlider) _masterSlider.SetValueWithoutNotify(AudioSystem.MasterVolume);
            if (_bgmSlider)    _bgmSlider.SetValueWithoutNotify(AudioSystem.BgmVolume);
            if (_sfxSlider)    _sfxSlider.SetValueWithoutNotify(AudioSystem.SfxVolume);
            UpdateVolumeLabels();

            AudioSystem.OnVolumeChanged += UpdateVolumeLabels;
        }

        void OnDestroy() => AudioSystem.OnVolumeChanged -= UpdateVolumeLabels;

        // ── BGM ──────────────────────────────────────────────────────────────────────

        public void OnPlayBgmA() => PlayBgmThenLabel(_bgmKeyA).Forget();
        public void OnPlayBgmB() => PlayBgmThenLabel(_bgmKeyB).Forget();

        /// <summary>
        /// Plays a track and repaints the label, but only if this is still the newest request —
        /// a superseded PlayBgmAsync returns normally rather than throwing, so without the id check
        /// the loser's continuation would paint a key the winner hasn't set yet.
        /// </summary>
        async UniTask PlayBgmThenLabel(string key)
        {
            int id = ++_bgmRequest;
            try
            {
                await AudioSystem.PlayBgmAsync(key, destroyCancellationToken);
                if (id == _bgmRequest) UpdateBgmLabel();
            }
            catch (System.OperationCanceledException) { }
        }

        public void OnStopBgm()
        {
            AudioSystem.StopBgm();
            UpdateBgmLabel();
        }

        public void OnPauseBgm()  => AudioSystem.PauseBgm();
        public void OnResumeBgm() => AudioSystem.ResumeBgm();

        // ── SFX ──────────────────────────────────────────────────────────────────────

        public void OnPlaySfxClick() => AudioSystem.PlaySfx(_sfxClickKey);

        public void OnPlaySfx3D()
        {
            var pos = _sfx3dTarget != null ? _sfx3dTarget.position : Vector3.zero;
            AudioSystem.PlaySfxAt(_sfx3dKey, pos);
        }

        public void OnToggleLoopSfx()
        {
            if (_loopPlaying) AudioSystem.StopLoopSfx(_sfxLoopKey);
            else              AudioSystem.PlayLoopSfx(_sfxLoopKey);
            _loopPlaying = !_loopPlaying;
        }

        // ── Ambience ─────────────────────────────────────────────────────────────────

        public void OnToggleAmbience()
        {
            if (_ambiencePlaying) AudioSystem.StopAmbience();
            else                  AudioSystem.PlayAmbience(_ambienceKey);
            _ambiencePlaying = !_ambiencePlaying;
            UpdateAmbienceLabel();
        }

        // ── Volume ───────────────────────────────────────────────────────────────────

        public void OnMasterSliderChanged(float v) => AudioSystem.SetMasterVolume(v);
        public void OnBgmSliderChanged(float v)    => AudioSystem.SetBgmVolume(v);
        public void OnSfxSliderChanged(float v)    => AudioSystem.SetSfxVolume(v);

        void UpdateVolumeLabels()
        {
            if (_masterLabel) _masterLabel.text = $"Master: {AudioSystem.MasterVolume:P0}";
            if (_bgmVolLabel) _bgmVolLabel.text = $"BGM: {AudioSystem.BgmVolume:P0}";
            if (_sfxVolLabel) _sfxVolLabel.text = $"SFX: {AudioSystem.SfxVolume:P0}";
        }

        void SetStatus(string msg)
        {
            if (_statusLabel) _statusLabel.text = msg;
        }

        void UpdateBgmLabel()
        {
            if (_bgmLabel) _bgmLabel.text = $"BGM: {(AudioSystem.CurrentBgmKey ?? "—")}";
        }

        void UpdateAmbienceLabel()
        {
            if (_ambienceLabel) _ambienceLabel.text = _ambiencePlaying ? $"Ambience: {_ambienceKey}" : "Ambience: —";
        }
    }
}
