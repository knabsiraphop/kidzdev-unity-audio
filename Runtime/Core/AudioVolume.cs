using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Converts between the 0–1 volume ratios used across the public API and the decibel values
    /// an <c>AudioMixer</c> exposed parameter expects. The mapping is logarithmic, so a 0.5 ratio
    /// is roughly -6 dB rather than half of the dB range.
    /// </summary>
    public static class AudioVolume
    {
        const float MinDb     = -80f;
        const float MinLinear = 0.0001f;

        /// <summary>Converts a 0–1 ratio to mixer decibels. Zero (or below) maps to -80 dB, treated as silence.</summary>
        public static float RatioToDB(float v01)
            => v01 <= 0f ? MinDb : Mathf.Log10(Mathf.Clamp(v01, MinLinear, 1f)) * 20f;

        /// <summary>Converts mixer decibels back to a 0–1 ratio. -80 dB or below maps to exactly zero.</summary>
        public static float DBToRatio(float db)
            => db <= MinDb ? 0f : Mathf.Pow(10f, db / 20f);

        public static float Clamp(float v01) => Mathf.Clamp01(v01);
    }
}
