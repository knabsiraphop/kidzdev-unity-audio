using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Static convenience facade over the process-wide default <see cref="IAudioService"/>
    /// (an <see cref="AudioManager"/> hosted by <see cref="AudioServiceRunner"/>). Keeps the
    /// ergonomic <c>AudioSystem.PlaySfx(…)</c> entry point; for testability/DI, depend on
    /// <see cref="IAudioService"/> and inject <see cref="Default"/> (or your own implementation).
    /// </summary>
    public static class AudioSystem
    {
        static IAudioService _default;

        /// <summary>
        /// The process-wide default service. Reading this creates an
        /// <see cref="AudioServiceRunner"/> on demand if none exists; assign to substitute a fake
        /// in tests or to run a second manager.
        /// </summary>
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

        /// <inheritdoc cref="IAudioService.IsReady"/>
        public static bool IsReady => Default.IsReady;

        /// <inheritdoc cref="IAudioService.Configure"/>
        public static void Configure(AudioServiceSettings settings = null)
            => Default.Configure(settings);

        /// <inheritdoc cref="IAudioService.InitializeAsync"/>
        public static UniTask InitializeAsync(CancellationToken ct = default)
            => Default.InitializeAsync(ct);

        // ── BGM ──────────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="IAudioService.CurrentBgmKey"/>
        public static string CurrentBgmKey => Default.CurrentBgmKey;

        /// <inheritdoc cref="IAudioService.PlayBgm(string)"/>
        public static void PlayBgm(string key)                               => Default.PlayBgm(key);

        /// <inheritdoc cref="IAudioService.PlayBgmAsync"/>
        public static UniTask PlayBgmAsync(string key, CancellationToken ct = default) => Default.PlayBgmAsync(key, ct);

        /// <inheritdoc cref="IAudioService.PlayBgm(AudioClip, bool)"/>
        public static void PlayBgm(AudioClip clip, bool loop = true)         => Default.PlayBgm(clip, loop);

        /// <inheritdoc cref="IAudioService.StopBgm"/>
        public static void StopBgm()                                         => Default.StopBgm();

        /// <inheritdoc cref="IAudioService.PauseBgm"/>
        public static void PauseBgm()                                        => Default.PauseBgm();

        /// <inheritdoc cref="IAudioService.ResumeBgm"/>
        public static void ResumeBgm()                                       => Default.ResumeBgm();

        // ── SFX ──────────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="IAudioService.PlaySfx(string)"/>
        public static void PlaySfx(string key)                                                        => Default.PlaySfx(key);

        /// <inheritdoc cref="IAudioService.PlaySfx(AudioClip, float)"/>
        public static void PlaySfx(AudioClip clip, float volume = 1f)                                 => Default.PlaySfx(clip, volume);

        /// <inheritdoc cref="IAudioService.PlaySfx(string, float, float, float)"/>
        public static void PlaySfx(string key, float startPitch, float endPitch, float duration)      => Default.PlaySfx(key, startPitch, endPitch, duration);

        /// <inheritdoc cref="IAudioService.PlaySfxAt"/>
        public static void PlaySfxAt(string key, Vector3 worldPos)                                    => Default.PlaySfxAt(key, worldPos);

        /// <inheritdoc cref="IAudioService.PlayLoopSfx"/>
        public static void PlayLoopSfx(string key)                                                    => Default.PlayLoopSfx(key);

        /// <inheritdoc cref="IAudioService.StopLoopSfx"/>
        public static void StopLoopSfx(string key)                                                    => Default.StopLoopSfx(key);

        // ── Ambience ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="IAudioService.PlayAmbience"/>
        public static void PlayAmbience(string key) => Default.PlayAmbience(key);

        /// <inheritdoc cref="IAudioService.StopAmbience"/>
        public static void StopAmbience()           => Default.StopAmbience();

        // ── Playlist ─────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="IAudioService.CreatePlaylist"/>
        public static BgmPlaylist CreatePlaylist(params string[] keys) => Default.CreatePlaylist(keys);

        // ── Volume ───────────────────────────────────────────────────────────────────

        public static float MasterVolume   => Default.MasterVolume;
        public static float BgmVolume      => Default.BgmVolume;
        public static float SfxVolume      => Default.SfxVolume;
        public static float AmbienceVolume => Default.AmbienceVolume;
        public static bool  IsMuted        => Default.IsMuted;

        /// <inheritdoc cref="IAudioService.SetMasterVolume"/>
        public static void SetMasterVolume(float v01)   => Default.SetMasterVolume(v01);

        /// <inheritdoc cref="IAudioService.SetBgmVolume"/>
        public static void SetBgmVolume(float v01)      => Default.SetBgmVolume(v01);

        /// <inheritdoc cref="IAudioService.SetSfxVolume"/>
        public static void SetSfxVolume(float v01)      => Default.SetSfxVolume(v01);

        /// <inheritdoc cref="IAudioService.SetAmbienceVolume"/>
        public static void SetAmbienceVolume(float v01) => Default.SetAmbienceVolume(v01);

        /// <inheritdoc cref="IAudioService.SetMute"/>
        public static void SetMute(bool mute)           => Default.SetMute(mute);

        /// <inheritdoc cref="IAudioService.OnVolumeChanged"/>
        public static event Action OnVolumeChanged
        {
            add    => Default.OnVolumeChanged += value;
            remove => Default.OnVolumeChanged -= value;
        }

        // ── Memory ───────────────────────────────────────────────────────────────────

        /// <inheritdoc cref="IAudioService.ReleaseCategory"/>
        public static void ReleaseCategory(SoundCategory category) => Default.ReleaseCategory(category);

        /// <inheritdoc cref="IAudioService.Release"/>
        public static void Release()                                => Default.Release();
    }
}
