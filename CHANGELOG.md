# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-07-26

### Added

- README: `## Authorship` section disclosing that the package was built with Claude Code under human direction

### Changed

- **Breaking:** `BgmChannel` now multiplies playback volume by `SoundEntry.Volume`, matching `SfxChannel`/`AmbienceChannel` (BGM previously ignored it). **If any BGM entries in your `SoundLibrary` have `Volume` set below `1.0`, that music will play quieter after upgrading — audit your BGM entries' `Volume` values before or immediately after upgrading.**
- `SetMasterVolume`/`SetBgmVolume`/`SetSfxVolume`/`SetAmbienceVolume` no longer audibly un-mute audio while `IsMuted` is `true`; the value is still recorded and persisted, and takes effect on the next `SetMute(false)`. Previously, calling any setter while muted made audio audible again even though `IsMuted` still reported `true`.
- `SoundEntry.Pitch` documentation corrected — SFX has never honored pitch (only BGM and ambience do). Doc-only, no behavior change.
- Public API now carries XML documentation throughout: `IAudioService` holds the prose, `AudioManager` inherits it via `<inheritdoc/>`, and `AudioSystem` via `<inheritdoc cref>`
- Demo sample: replaced the near-silent `amb_wind.wav` with an audible looping wind clip, wired up every unset `SerializeField` on `AudioDemoController`, added an ambience status label, clarified the SFX-loop button label, and fixed a stale-label race when switching BGM tracks

### Removed

- **Breaking:** `SoundEntry.EndTime`, `SoundEntry.FadeIn`, and `SoundEntry.FadeOut` — despite being advertised as shipped in the 1.0.0 changelog, no runtime code ever read them; they were Inspector-only fields with no effect on playback. **If your code references `entry.EndTime`, `entry.FadeIn`, or `entry.FadeOut`, search your project for those references — it will no longer compile.** Existing `SoundLibrary` assets still load; the stored values are dropped from the serialized data with no change to playback, since they never affected playback. BGM and ambience fade durations continue to come from `AudioServiceSettings`.

### Fixed

- `SetMasterVolume` was a no-op whenever no `AudioMixer` was assigned — the documented and common configuration. It only wrote a mixer-exposed parameter, which does nothing without a mixer, so the master control never affected anything. Volume is now composed as `master × category` per source, so master works with or without a mixer.
- `AudioSourcePool.Rent()` could hand back an already-playing source once the pool reached capacity, and `PlayLoop`/`PlayPitchRamp` then reassigned that source's clip — corrupting an unrelated loop, so stopping one loop could silently kill another's audio and leave it referencing a dead source. Rentals that assign a clip are now exclusive and fail with a warning instead of stealing a live source.
- `InitializeAsync` had no in-flight join: `AudioServiceRunner.Awake()` auto-calling it alongside an explicit `Configure`-then-`InitializeAsync` call ran two concurrent preloads, and the second `Configure()` could dispose the SFX pool mid-flight. Concurrent calls now join a single in-flight run and a failed run can be retried; `SfxChannel.Reconfigure` is a no-op when the pool sizes are unchanged.
- `ResourcesSoundClipLoader` now shares a single load across concurrent requests for the same key instead of issuing duplicate loads. One caller cancelling no longer aborts a load other callers are still waiting on.
- `BgmChannel`/`SfxChannel`/`AmbienceChannel`: a volume setter called mid-playback no longer clobbers the per-clip `SoundEntry.Volume` factor already applied to a playing source. Volume is recomposed as `master × category × entry`, and fade loops re-read the target every frame so a slider moved mid-fade is not lost.
- Addressables Loader sample: added the missing `Unity.ResourceManager` assembly reference (needed for `AsyncOperationHandle<T>`, not included transitively via `Unity.Addressables`) and worked around a `ToUniTask()` overload-resolution quirk that bound to the non-generic overload

## [1.0.2] - 2026-06-27

### Changed
- Addressables Loader sample: `AddressablesSoundClipLoader` now uses Unity's built-in `Addressables.LoadAssetAsync` / `Addressables.Release` directly — removes the dependency on `com.kidzdev.unity.addressables-toolkit`
- Addressables Loader sample: `.asmdef` now references `Unity.Addressables` instead of `KidzDev.Unity.AddressablesToolkit`
- Addressables Loader sample: fixed control-flow bug where `_inflight.Remove` and `tcs.TrySetResult` were only reached on the happy path; moved them inside the try block so concurrent callers are always unblocked
- README: updated "Using with Addressables" section to reflect pure-Addressables implementation (no extra KidzDev package required)

## [1.0.1] - 2026-06-27

### Added
- Demo sample: ready-to-run scene with BGM crossfade, SFX pool, 3D one-shot, loop SFX, ambience, volume sliders with persistence, and playlist (`AudioDemo.unity`)
- Addressables Loader sample: `AddressablesSoundClipLoader` backed by `IAssetLoader`/`AssetScope` from `com.kidzdev.unity.addressables-toolkit`

### Changed
- README: added full setup guide with step-by-step instructions, inspector reference tables for `AudioServiceSettings` and `SoundLibrary`, and comprehensive code examples
- README: added inspector screenshots via `Documentation~/images/`

## [1.0.0] - 2026-06-24

### Added
- `AudioSystem` static facade + `IAudioService` seam + `AudioManager` default impl
- `SoundLibrary` / `SoundEntry` data SO (key, category, volume, pitch, loop, begin/end time, fade in/out)
- `ISoundClipLoader` seam with `ResourcesSoundClipLoader` (Resources, zero-dep, cached)
- `IVolumeStore` seam with `PlayerPrefsVolumeStore` (default) and `DelegateVolumeStore` (callback injection)
- `AudioVolume` static helpers: `RatioToDB`, `DBToRatio`, `Clamp`
- `AudioServiceSettings` Resources-loaded SO: library ref, optional mixer, warm strategy, pool config, fade durations, volume keys
- BGM channel: two-source cancellable crossfade, dedupe by key, superseded-load guard, pause/resume
- SFX channel: round-robin `AudioSourcePool` (grow-to-cap, never cut off), one-shot, 3D `PlaySfxAt`, loop SFX tracked by key, pitch-ramp variant (UniTask, no DOTween)
- Ambience channel: single source, dedupe by key
- `BgmPlaylist`: shuffle, sequential `PlayAllAsync`
- `AudioServiceRunner` convenience MonoBehaviour: auto-configure/initialize, forwards `OnApplicationPause`/`Focus`
- `BgmPlayer` / `SfxPlayer` inspector-driven helper components
- Editor menu: **Tools ▸ Audio ▸ Create Settings** / **Create Sound Library** / **Validate Library**
- `SoundLibraryEditor`: unique-key validation, per-entry ping
- Demo sample: BGM crossfade, SFX, ambience, volume sliders with persistence
- Addressables Loader sample: `AddressablesSoundClipLoader` via `IAssetLoader`/`AssetScope`
