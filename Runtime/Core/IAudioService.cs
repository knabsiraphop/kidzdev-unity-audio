using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Contract for the audio service — BGM (crossfaded, single track), SFX (pooled one-shots plus
    /// keyed loops), and Ambience (single looping bed). Keys are <see cref="SoundLibrary"/> entries
    /// resolved through an <see cref="ISoundClipLoader"/>, so the same call works for Resources or
    /// Addressables. Depend on this abstraction rather than the static <see cref="AudioSystem"/>
    /// facade to keep consumers testable and substitutable.
    /// </summary>
    public interface IAudioService
    {
        // ── Init ─────────────────────────────────────────────────────────────────────

        /// <summary>True once <see cref="InitializeAsync"/> has finished, including any clip preloading.</summary>
        bool IsReady { get; }

        /// <summary>
        /// Applies settings and wires the mixer, clip library, and volume-store keys. Pass null to
        /// load <c>AudioServiceSettings</c> from Resources. Call before
        /// <see cref="InitializeAsync"/>; a no-op if no settings can be resolved.
        /// </summary>
        void Configure(AudioServiceSettings settings = null);

        /// <summary>
        /// Restores persisted volumes, applies them, and preloads clips per the configured
        /// <see cref="WarmStrategy"/>. Sets <see cref="IsReady"/> on completion. Safe to call more
        /// than once: concurrent callers join the single in-flight run instead of preloading twice,
        /// and once ready it completes immediately. A failed or cancelled run can be retried.
        /// </summary>
        UniTask InitializeAsync(CancellationToken ct = default);

        // ── BGM ──────────────────────────────────────────────────────────────────────

        /// <summary>Key of the playing track, or null when stopped or when playback was started from a raw <see cref="AudioClip"/>.</summary>
        string CurrentBgmKey { get; }

        /// <summary>
        /// Fire-and-forget: loads and crossfades to <paramref name="key"/>. A newer call in flight
        /// supersedes an older one — the older load still completes but is discarded rather than
        /// clobbering playback. No-ops if <paramref name="key"/> already equals
        /// <see cref="CurrentBgmKey"/>.
        /// </summary>
        void PlayBgm(string key);

        /// <summary>
        /// Awaitable form of <see cref="PlayBgm(string)"/> — completes once the crossfade has
        /// finished. Returns early without throwing when the call is superseded by a newer one or
        /// when <paramref name="key"/> is already playing, so completion does not by itself prove
        /// this key is the current track; check <see cref="CurrentBgmKey"/> if that matters.
        /// </summary>
        /// <param name="key">Library key to crossfade to.</param>
        /// <param name="ct">Cancels the load and abandons the crossfade.</param>
        UniTask PlayBgmAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Plays an already-loaded clip immediately, bypassing the library and loader. Clears
        /// <see cref="CurrentBgmKey"/> (there is no key to report) and does not crossfade.
        /// </summary>
        void PlayBgm(AudioClip clip, bool loop = true);

        /// <summary>Fades out the current track over the configured BGM fade duration and clears <see cref="CurrentBgmKey"/>.</summary>
        void StopBgm();

        /// <summary>Pauses the current track in place, keeping its clip and playback position.</summary>
        void PauseBgm();

        /// <summary>Resumes a track paused by <see cref="PauseBgm"/> from its previous position.</summary>
        void ResumeBgm();

        // ── SFX ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a one-shot on a pooled source. Overlapping calls layer rather than cutting each
        /// other off; when the pool is at capacity the least-recently-used source is reused
        /// without interrupting what it is already playing.
        /// </summary>
        void PlaySfx(string key);

        /// <summary>Plays an already-loaded clip as a one-shot, bypassing the library and loader.</summary>
        void PlaySfx(AudioClip clip, float volume = 1f);

        /// <summary>
        /// Plays a one-shot whose pitch ramps from <paramref name="startPitch"/> to
        /// <paramref name="endPitch"/> over <paramref name="duration"/> seconds (unscaled time).
        /// The source's pitch is reset when the ramp ends.
        /// </summary>
        void PlaySfx(string key, float startPitch, float endPitch, float duration);

        /// <summary>
        /// Plays a one-shot positionally at <paramref name="worldPos"/> (3D blend). The borrowed
        /// source reverts to 2D once the clip finishes, so it stays reusable for flat SFX.
        /// </summary>
        void PlaySfxAt(string key, Vector3 worldPos);

        /// <summary>
        /// Starts a looping SFX tracked under <paramref name="key"/>. Cancels any in-flight load
        /// for that key first, so a same-frame double-press cannot start two instances and a
        /// <see cref="StopLoopSfx"/> issued before the clip finishes loading still wins.
        /// No-ops if the key is already looping.
        /// </summary>
        void PlayLoopSfx(string key);

        /// <summary>Stops the loop started for <paramref name="key"/>, abandoning its load if one is still in flight.</summary>
        void StopLoopSfx(string key);

        // ── Ambience ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crossfades the single ambience bed to <paramref name="key"/> — only one ambience track
        /// plays at a time, unlike layered SFX. No-ops if that key is already the current bed.
        /// </summary>
        void PlayAmbience(string key);

        /// <summary>Fades out the current ambience bed over the configured ambience fade duration.</summary>
        void StopAmbience();

        // ── Playlist ─────────────────────────────────────────────────────────────────

        /// <summary>Builds a new <see cref="BgmPlaylist"/> over <paramref name="keys"/>; each call allocates its own independent playlist.</summary>
        BgmPlaylist CreatePlaylist(params string[] keys);

        // ── Volume ───────────────────────────────────────────────────────────────────
        float MasterVolume    { get; }
        float BgmVolume       { get; }
        float SfxVolume       { get; }
        float AmbienceVolume  { get; }
        bool  IsMuted         { get; }

        /// <summary>
        /// Sets master volume (0–1). With an <see cref="AudioServiceSettings.Mixer"/> assigned this
        /// writes the master exposed parameter and Unity's bus hierarchy composes it with each
        /// category bus — the channels themselves are always fed <c>1f</c>. With no mixer there is
        /// no bus to compose for you, so each channel's source volume becomes
        /// <c>master * category * SoundEntry.Volume</c> directly. The same composition rule applies
        /// to <see cref="SetBgmVolume"/>, <see cref="SetSfxVolume"/>, and
        /// <see cref="SetAmbienceVolume"/>. While <see cref="IsMuted"/> the value is recorded and
        /// persisted but does not become audible — see <see cref="SetMute"/>.
        /// </summary>
        void SetMasterVolume(float v01);

        /// <summary>Sets BGM category volume (0–1) — composes with master exactly as described on <see cref="SetMasterVolume"/>.</summary>
        void SetBgmVolume(float v01);

        /// <summary>Sets SFX category volume (0–1) — composes with master exactly as described on <see cref="SetMasterVolume"/>. Applies to active loops as well as future one-shots.</summary>
        void SetSfxVolume(float v01);

        /// <summary>Sets ambience category volume (0–1) — composes with master exactly as described on <see cref="SetMasterVolume"/>.</summary>
        void SetAmbienceVolume(float v01);

        /// <summary>
        /// Silences or restores all output without discarding stored volume values. Setters called
        /// while muted still record and persist their input; unmuting re-applies whatever was last
        /// set rather than a snapshot taken at mute time.
        /// </summary>
        void SetMute(bool mute);

        /// <summary>Raised after any volume or mute change, including changes made while muted. Useful for driving volume UI.</summary>
        event Action OnVolumeChanged;

        // ── Memory ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Releases every loaded clip in <paramref name="category"/>, stopping that category's
        /// active playback first so no <c>AudioSource</c> is left holding a freed clip handle.
        /// </summary>
        void ReleaseCategory(SoundCategory category);

        /// <summary>
        /// Stops all playback and releases every loaded clip. Volume settings and configuration
        /// survive, so the service can be reused without calling <see cref="Configure"/> again.
        /// </summary>
        void Release();
    }
}
