using System;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Per-clip playback settings authored in a <see cref="SoundLibrary"/> and looked up by
    /// <see cref="Key"/> whenever that key is played.
    /// </summary>
    [Serializable]
    public sealed class SoundEntry
    {
        [Tooltip("Addressable key or Resources path — resolved by ISoundClipLoader.")]
        public string Key;

        /// <summary>Groups the entry for preloading (see <see cref="WarmStrategy"/>) and bulk release via <see cref="IAudioService.ReleaseCategory"/>.</summary>
        public SoundCategory Category = SoundCategory.SFX;

        /// <summary>Per-clip multiplier applied on top of the category and master volumes — see <see cref="IAudioService.SetMasterVolume"/> for how the three compose.</summary>
        [Range(0f, 1f)]   public float Volume = 1f;

        /// <summary>
        /// Playback pitch. Honored by BGM and ambience only — SFX one-shots and loops always play at
        /// unity pitch, and <see cref="IAudioService.PlaySfx(string, float, float, float)"/> supplies
        /// its own ramp range instead.
        /// </summary>
        [Range(0.1f, 3f)] public float Pitch  = 1f;

        /// <summary>Applies to BGM only. Ambience always loops, and looping SFX is chosen per call via <see cref="IAudioService.PlayLoopSfx"/>.</summary>
        public bool Loop;

        /// <summary>Applies to BGM only — start offset in seconds, 0 to start at the beginning. Ignored by SFX and ambience.</summary>
        [Tooltip("Playback start offset in seconds (0 = clip start). BGM only.")]
        public float BeginTime;
    }
}
