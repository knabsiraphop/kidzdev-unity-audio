using UnityEngine;

namespace KidzDev.Unity.Audio
{
    public static class AudioVolume
    {
        const float MinDb     = -80f;
        const float MinLinear = 0.0001f;

        public static float RatioToDB(float v01)
            => v01 <= 0f ? MinDb : Mathf.Log10(Mathf.Clamp(v01, MinLinear, 1f)) * 20f;

        public static float DBToRatio(float db)
            => db <= MinDb ? 0f : Mathf.Pow(10f, db / 20f);

        public static float Clamp(float v01) => Mathf.Clamp01(v01);
    }
}
