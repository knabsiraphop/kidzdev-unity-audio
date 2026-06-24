using System;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    [Serializable]
    public sealed class SoundEntry
    {
        [Tooltip("Addressable key or Resources path — resolved by ISoundClipLoader.")]
        public string Key;

        public SoundCategory Category = SoundCategory.SFX;

        [Range(0f, 1f)]   public float Volume = 1f;
        [Range(0.1f, 3f)] public float Pitch  = 1f;
        public bool Loop;

        [Header("Playback range (0 = clip default)")]
        [Tooltip("Playback start offset in seconds.")] public float BeginTime;
        [Tooltip("Playback end in seconds (0 = full clip).")]  public float EndTime;

        [Header("Fades")]
        [Tooltip("Fade-in duration in seconds.")]  public float FadeIn;
        [Tooltip("Fade-out duration in seconds.")] public float FadeOut;
    }
}
