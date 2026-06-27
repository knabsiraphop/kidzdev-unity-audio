# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
