namespace KidzDev.Unity.Audio
{
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
