using System;

namespace KidzDev.Unity.Audio
{
    public sealed class DelegateVolumeStore : IVolumeStore
    {
        readonly Func<float>   _getMaster;
        readonly Func<float>   _getBgm;
        readonly Func<float>   _getSfx;
        readonly Func<float>   _getAmbience;
        readonly Action<float> _saveMaster;
        readonly Action<float> _saveBgm;
        readonly Action<float> _saveSfx;
        readonly Action<float> _saveAmbience;

        public DelegateVolumeStore(
            Func<float>   getMaster,    Action<float> saveMaster,
            Func<float>   getBgm,       Action<float> saveBgm,
            Func<float>   getSfx,       Action<float> saveSfx,
            Func<float>   getAmbience  = null, Action<float> saveAmbience  = null)
        {
            _getMaster    = getMaster;
            _getBgm       = getBgm;
            _getSfx       = getSfx;
            _getAmbience  = getAmbience;
            _saveMaster   = saveMaster;
            _saveBgm      = saveBgm;
            _saveSfx      = saveSfx;
            _saveAmbience = saveAmbience;
        }

        public float GetMasterVolume()   => _getMaster();
        public float GetBgmVolume()      => _getBgm();
        public float GetSfxVolume()      => _getSfx();
        public float GetAmbienceVolume() => _getAmbience?.Invoke() ?? 1f;

        public void SaveMasterVolume(float v01)   => _saveMaster(v01);
        public void SaveBgmVolume(float v01)      => _saveBgm(v01);
        public void SaveSfxVolume(float v01)      => _saveSfx(v01);
        public void SaveAmbienceVolume(float v01) => _saveAmbience?.Invoke(v01);
    }
}
