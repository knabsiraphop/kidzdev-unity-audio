using UnityEngine;

namespace KidzDev.Unity.Audio
{
    public sealed class PlayerPrefsVolumeStore : IVolumeStore
    {
        readonly string _masterKey;
        readonly string _bgmKey;
        readonly string _sfxKey;
        readonly string _ambienceKey;

        public PlayerPrefsVolumeStore(
            string masterKey    = "audio_master",
            string bgmKey       = "audio_bgm",
            string sfxKey       = "audio_sfx",
            string ambienceKey  = "audio_ambience")
        {
            _masterKey   = masterKey;
            _bgmKey      = bgmKey;
            _sfxKey      = sfxKey;
            _ambienceKey = ambienceKey;
        }

        public float GetMasterVolume()   => PlayerPrefs.GetFloat(_masterKey,   1f);
        public float GetBgmVolume()      => PlayerPrefs.GetFloat(_bgmKey,      1f);
        public float GetSfxVolume()      => PlayerPrefs.GetFloat(_sfxKey,      1f);
        public float GetAmbienceVolume() => PlayerPrefs.GetFloat(_ambienceKey, 1f);

        public void SaveMasterVolume(float v01)   => PlayerPrefs.SetFloat(_masterKey,   v01);
        public void SaveBgmVolume(float v01)      => PlayerPrefs.SetFloat(_bgmKey,      v01);
        public void SaveSfxVolume(float v01)      => PlayerPrefs.SetFloat(_sfxKey,      v01);
        public void SaveAmbienceVolume(float v01) => PlayerPrefs.SetFloat(_ambienceKey, v01);
    }
}
