namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Persists the four volume levels across sessions. <see cref="PlayerPrefsVolumeStore"/> is the
    /// default; implement this (or use <see cref="DelegateVolumeStore"/>) to route volumes into
    /// your own save system instead.
    /// </summary>
    public interface IVolumeStore
    {
        float GetMasterVolume();
        float GetBgmVolume();
        float GetSfxVolume();
        float GetAmbienceVolume();
        void SaveMasterVolume(float v01);
        void SaveBgmVolume(float v01);
        void SaveSfxVolume(float v01);
        void SaveAmbienceVolume(float v01);
    }
}
