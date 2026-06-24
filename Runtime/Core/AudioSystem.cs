using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Static facade over <see cref="AudioManager.Default"/>.
    /// Swap <see cref="Default"/> for testing or multi-manager setups.
    /// </summary>
    public static class AudioSystem
    {
        static IAudioService _default;

        public static IAudioService Default
        {
            get => _default ??= AudioServiceRunner.GetOrCreate();
            set => _default = value;
        }

        // Used by AudioServiceRunner.OnDestroy to clear without triggering GetOrCreate.
        internal static void ClearDefault(IAudioService service)
        {
            if (_default == service) _default = null;
        }

        // ── Init ─────────────────────────────────────────────────────────────────────

        public static bool IsReady => Default.IsReady;

        public static void Configure(AudioServiceSettings settings = null)
            => Default.Configure(settings);

        public static UniTask InitializeAsync(CancellationToken ct = default)
            => Default.InitializeAsync(ct);

        // ── BGM ──────────────────────────────────────────────────────────────────────

        public static string CurrentBgmKey => Default.CurrentBgmKey;

        public static void PlayBgm(string key)                               => Default.PlayBgm(key);
        public static UniTask PlayBgmAsync(string key, CancellationToken ct = default) => Default.PlayBgmAsync(key, ct);
        public static void PlayBgm(AudioClip clip, bool loop = true)         => Default.PlayBgm(clip, loop);
        public static void StopBgm()                                         => Default.StopBgm();
        public static void PauseBgm()                                        => Default.PauseBgm();
        public static void ResumeBgm()                                       => Default.ResumeBgm();

        // ── SFX ──────────────────────────────────────────────────────────────────────

        public static void PlaySfx(string key)                                                        => Default.PlaySfx(key);
        public static void PlaySfx(AudioClip clip, float volume = 1f)                                 => Default.PlaySfx(clip, volume);
        public static void PlaySfx(string key, float startPitch, float endPitch, float duration)      => Default.PlaySfx(key, startPitch, endPitch, duration);
        public static void PlaySfxAt(string key, Vector3 worldPos)                                    => Default.PlaySfxAt(key, worldPos);
        public static void PlayLoopSfx(string key)                                                    => Default.PlayLoopSfx(key);
        public static void StopLoopSfx(string key)                                                    => Default.StopLoopSfx(key);

        // ── Ambience ─────────────────────────────────────────────────────────────────

        public static void PlayAmbience(string key) => Default.PlayAmbience(key);
        public static void StopAmbience()           => Default.StopAmbience();

        // ── Playlist ─────────────────────────────────────────────────────────────────

        public static BgmPlaylist CreatePlaylist(params string[] keys) => Default.CreatePlaylist(keys);

        // ── Volume ───────────────────────────────────────────────────────────────────

        public static float MasterVolume   => Default.MasterVolume;
        public static float BgmVolume      => Default.BgmVolume;
        public static float SfxVolume      => Default.SfxVolume;
        public static float AmbienceVolume => Default.AmbienceVolume;
        public static bool  IsMuted        => Default.IsMuted;

        public static void SetMasterVolume(float v01)   => Default.SetMasterVolume(v01);
        public static void SetBgmVolume(float v01)      => Default.SetBgmVolume(v01);
        public static void SetSfxVolume(float v01)      => Default.SetSfxVolume(v01);
        public static void SetAmbienceVolume(float v01) => Default.SetAmbienceVolume(v01);
        public static void SetMute(bool mute)           => Default.SetMute(mute);

        public static event Action OnVolumeChanged
        {
            add    => Default.OnVolumeChanged += value;
            remove => Default.OnVolumeChanged -= value;
        }

        // ── Memory ───────────────────────────────────────────────────────────────────

        public static void ReleaseCategory(SoundCategory category) => Default.ReleaseCategory(category);
        public static void Release()                                => Default.Release();
    }
}
