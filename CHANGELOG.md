# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
