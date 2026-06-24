using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    public interface IAudioService
    {
        // ── Init ─────────────────────────────────────────────────────────────────────
        bool IsReady { get; }
        void Configure(AudioServiceSettings settings = null);
        UniTask InitializeAsync(CancellationToken ct = default);

        // ── BGM ──────────────────────────────────────────────────────────────────────
        string CurrentBgmKey { get; }
        void PlayBgm(string key);
        UniTask PlayBgmAsync(string key, CancellationToken ct = default);
        void PlayBgm(AudioClip clip, bool loop = true);
        void StopBgm();
        void PauseBgm();
        void ResumeBgm();

        // ── SFX ──────────────────────────────────────────────────────────────────────
        void PlaySfx(string key);
        void PlaySfx(AudioClip clip, float volume = 1f);
        void PlaySfx(string key, float startPitch, float endPitch, float duration);
        void PlaySfxAt(string key, Vector3 worldPos);
        void PlayLoopSfx(string key);
        void StopLoopSfx(string key);

        // ── Ambience ─────────────────────────────────────────────────────────────────
        void PlayAmbience(string key);
        void StopAmbience();

        // ── Playlist ─────────────────────────────────────────────────────────────────
        BgmPlaylist CreatePlaylist(params string[] keys);

        // ── Volume ───────────────────────────────────────────────────────────────────
        float MasterVolume    { get; }
        float BgmVolume       { get; }
        float SfxVolume       { get; }
        float AmbienceVolume  { get; }
        bool  IsMuted         { get; }
        void SetMasterVolume(float v01);
        void SetBgmVolume(float v01);
        void SetSfxVolume(float v01);
        void SetAmbienceVolume(float v01);
        void SetMute(bool mute);
        event Action OnVolumeChanged;

        // ── Memory ───────────────────────────────────────────────────────────────────
        void ReleaseCategory(SoundCategory category);
        void Release();
    }
}
