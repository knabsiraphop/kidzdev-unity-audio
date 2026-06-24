# KidzDev Unity Audio

Production-grade BGM/SFX/Ambience audio service for Unity. UniTask async, AudioMixer-backed with dB conversion, cancellable crossfades, pooled sources, plug-in clip loader (Resources default, optional Addressables adapter), and plug-in volume store.

## Installation

```
https://github.com/knabsiraphop/kidzdev-unity-audio.git#v1.0.0
```

Or in `Packages/manifest.json`:

```json
"com.kidzdev.unity.audio": "https://github.com/knabsiraphop/kidzdev-unity-audio.git#v1.0.0"
```

> UniTask (`com.cysharp.unitask`) is a required dependency. Add the OpenUPM scoped registry if it is not already present.

## Quick start

1. **Tools ▸ Audio ▸ Create Settings** — creates `Assets/Resources/AudioServiceSettings.asset`.
2. **Tools ▸ Audio ▸ Create Sound Library** — creates `Assets/Resources/SoundLibrary.asset`.
3. Add your clips to the `SoundLibrary` (Resources path or Addressable key).
4. Add `AudioServiceRunner` to a `DontDestroyOnLoad` GameObject (or rely on `AudioSystem.Default` to create one automatically).
5. Call `AudioSystem.PlayBgm("bgm_key")`, `AudioSystem.PlaySfx("sfx_key")`, etc.

## Overview

| Feature | Details |
| --- | --- |
| Async model | `UniTask` throughout; one `CancellationToken` per operation |
| BGM | Two-source crossfade, dedupe, superseded-load guard, pause/resume |
| SFX | Pooled `AudioSourcePool` (grow-to-cap), one-shot, 3D positional, loop, pitch ramp |
| Ambience | Single-source, dedupe |
| Volume | `SetMasterVolume / SetBgmVolume / SetSfxVolume / SetAmbienceVolume`, mute, AudioMixer or per-source |
| Persistence | `IVolumeStore` — `PlayerPrefsVolumeStore` (default) or `DelegateVolumeStore` |
| Clip loading | `ISoundClipLoader` — `ResourcesSoundClipLoader` (default) or `AddressablesSoundClipLoader` (sample) |

## Samples

- **Demo** — BGM crossfade, SFX pool, 3D one-shot, loop SFX, ambience, volume sliders with persistence.
- **Addressables Loader** — `AddressablesSoundClipLoader` via `IAssetLoader` / `AssetScope` from `com.kidzdev.unity.addressables-toolkit`.

## License

MIT — see [LICENSE.md](LICENSE.md).
