using UnityEngine;
using UnityEngine.Audio;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Configuration for the audio service: clip library, optional mixer, preload strategy, SFX
    /// pool sizing, fade durations, and volume-persistence keys. Name the asset
    /// <c>AudioServiceSettings</c> and put it in a <c>Resources</c> folder for
    /// <see cref="IAudioService.Configure"/> to find it automatically.
    /// </summary>
    /// <remarks>
    /// The mixer is optional and changes how volumes compose — see
    /// <see cref="IAudioService.SetMasterVolume"/>. With one assigned, groups named
    /// <c>BGM</c>, <c>SFX</c>, and <c>Ambience</c> are matched by name and each channel routes to
    /// its group.
    /// </remarks>
    [CreateAssetMenu(menuName = "KidzDev/Audio/Settings", fileName = "AudioServiceSettings")]
    public sealed class AudioServiceSettings : ScriptableObject
    {
        [Header("Library")]
        [SerializeField] SoundLibrary _library;

        [Header("Mixer (optional — omit to use per-source volume)")]
        [SerializeField] AudioMixer _mixer;
        [SerializeField] string _paramMaster    = "MasterVolume";
        [SerializeField] string _paramBgm       = "BgmVolume";
        [SerializeField] string _paramSfx       = "SfxVolume";
        [SerializeField] string _paramAmbience  = "AmbienceVolume";

        [Header("Warming")]
        [SerializeField] WarmStrategy _warmStrategy = WarmStrategy.AllSfx;
        [SerializeField] SoundCategory[] _warmCategories = { SoundCategory.SFX, SoundCategory.UI };

        [Header("SFX Pool")]
        [SerializeField] int _sfxPoolSize = 5;
        [SerializeField] int _sfxPoolCap  = 12;

        [Header("Fades")]
        [SerializeField] float _bgmFadeDuration      = 1f;
        [SerializeField] float _ambienceFadeDuration = 0.5f;

        [Header("Volume persistence (PlayerPrefs keys)")]
        [SerializeField] string _masterVolumeKey    = "audio_master";
        [SerializeField] string _bgmVolumeKey       = "audio_bgm";
        [SerializeField] string _sfxVolumeKey       = "audio_sfx";
        [SerializeField] string _ambienceVolumeKey  = "audio_ambience";

        public SoundLibrary        Library             => _library;
        public AudioMixer          Mixer               => _mixer;
        public string              ParamMaster         => _paramMaster;
        public string              ParamBgm            => _paramBgm;
        public string              ParamSfx            => _paramSfx;
        public string              ParamAmbience       => _paramAmbience;
        public WarmStrategy        WarmStrategy        => _warmStrategy;
        public SoundCategory[]     WarmCategories      => _warmCategories;
        public int                 SfxPoolSize         => _sfxPoolSize;
        public int                 SfxPoolCap          => _sfxPoolCap;
        public float               BgmFadeDuration     => _bgmFadeDuration;
        public float               AmbienceFadeDuration => _ambienceFadeDuration;
        public string              MasterVolumeKey     => _masterVolumeKey;
        public string              BgmVolumeKey        => _bgmVolumeKey;
        public string              SfxVolumeKey        => _sfxVolumeKey;
        public string              AmbienceVolumeKey   => _ambienceVolumeKey;
    }
}
