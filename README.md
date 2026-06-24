# KidzDev Unity Audio

Production-grade BGM/SFX/Ambience audio service for Unity. UniTask async, AudioMixer-backed with dB conversion, cancellable crossfades, pooled sources, plug-in clip loader (Resources default, optional Addressables adapter), and plug-in volume store.

## Installation

Add via **Package Manager → Add package from git URL**:

```
https://github.com/knabsiraphop/kidzdev-unity-audio.git#v1.0.0
```

Or directly in `Packages/manifest.json`:

```json
"com.kidzdev.unity.audio": "https://github.com/knabsiraphop/kidzdev-unity-audio.git#v1.0.0"
```

> **Required dependency:** UniTask (`com.cysharp.unitask`). Add the OpenUPM scoped registry if not already present:
> ```json
> "scopedRegistries": [{
>   "name": "OpenUPM",
>   "url": "https://package.openupm.com",
>   "scopes": ["com.cysharp"]
> }]
> ```

---

## Setup (5 minutes)

### Step 1 — Create the Settings asset

**Tools ▸ Audio ▸ Create Settings**

Creates `Assets/Resources/AudioServiceSettings.asset`. This is the single configuration point for the entire audio system.

### Step 2 — Create the Sound Library

**Tools ▸ Audio ▸ Create Sound Library**

Creates `Assets/Resources/SoundLibrary.asset`. Every audio clip in your game has one entry here.

### Step 3 — Add clips to the library

Open `SoundLibrary` and add entries (see [Library reference](#sound-library) below). Assign the same asset in the **Library** field of `AudioServiceSettings`.

### Step 4 — Initialize in code

```csharp
using Cysharp.Threading.Tasks;
using KidzDev.Unity.Audio;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    async void Start()
    {
        // Configure picks up AudioServiceSettings from Resources automatically.
        // Pass a custom settings asset here if you want a non-default location.
        AudioSystem.Configure();

        // InitializeAsync: loads the library, warms clips per strategy, restores saved volumes.
        // Pass destroyCancellationToken so everything cleans up when this GameObject is destroyed.
        await AudioSystem.InitializeAsync(destroyCancellationToken);

        // The system is now ready — IsReady == true.
        Debug.Log("Audio ready: " + AudioSystem.IsReady);
    }
}
```

> `Configure` + `InitializeAsync` are **idempotent and safe to call multiple times**. Concurrent calls join the same in-flight task.

---

## Audio Service Settings

Inspector reference for `AudioServiceSettings.asset`:

| Field | Description |
|---|---|
| **Addressable** | When enabled, clip keys are resolved via `AddressablesSoundClipLoader` instead of `ResourcesSoundClipLoader`. Requires the Addressables Loader sample to be imported. |
| **Library** | The `SoundLibrary` asset this service uses. |
| **Mixer** | *(Optional)* An `AudioMixer` asset. When set, volumes are applied via mixer parameters (dB-converted). Leave `None` to use per-source volume instead. |
| **Param Master / Bgm / Sfx / Ambience** | The exposed parameter names on the mixer (e.g. `MasterVolume`, `BgmVolume`). Must match the exposed parameters in your `AudioMixer`. |
| **Warm Strategy** | `None` — no pre-loading. `AllSfx` — warm every SFX entry at init. `ByCategory` — warm only the categories listed in Warm Categories. |
| **Warm Categories** | Active when strategy is `ByCategory`. List the `SoundCategory` values to warm (e.g. `SFX`, `UI`). |
| **Sfx Pool Size** | Initial number of `AudioSource` components in the SFX pool. |
| **Sfx Pool Cap** | Maximum the pool grows to under load. Sources above initial are created on demand. |
| **Bgm Fade Duration** | Crossfade time (seconds) between BGM tracks. |
| **Ambience Fade Duration** | Fade-in / fade-out time (seconds) for ambience transitions. |
| **Master / Bgm / Sfx / Ambience Volume Key** | `PlayerPrefs` keys used by the default volume store. Change these if you have key conflicts. |

---

## Sound Library

Inspector reference for `SoundLibrary.asset`. Each entry in **Sound Entries** maps a string key to a clip and its playback settings:

| Field | Description |
|---|---|
| **Key** | Unique string identifier used in all `AudioSystem.Play*` calls. For Resources loading, this is the path under `Resources/` without the extension (e.g. `Audio/bgm_a`). For Addressables, this is the Addressable address. |
| **Category** | `BGM`, `SFX`, `Ambience`, `UI`, or `Voice`. Used for warm-by-category and `ReleaseCategory`. |
| **Volume** | Per-entry volume multiplier (0–1). Combined with the channel volume at playback. |
| **Pitch** | Playback pitch (default 1). |
| **Loop** | When checked, the clip loops. Relevant for BGM and Ambience entries. |
| **Begin Time / End Time** | Playback range in seconds. `0` = use full clip. Lets you trim a clip without editing the asset. |
| **Fade In / Fade Out** | Per-entry fade override in seconds. Set to `0` to use the global fade from Settings. |

> **Validate your library** any time with **Tools ▸ Audio ▸ Validate Library** — it checks for empty keys, duplicate keys, and missing Resources clips.

---

## Usage Examples

### Initialize and wait for ready

```csharp
public class AudioBootstrap : MonoBehaviour
{
    async void Start()
    {
        AudioSystem.Configure();
        await AudioSystem.InitializeAsync(destroyCancellationToken);
        // Safe to call PlayBgm/PlaySfx after this point.
    }
}
```

### BGM — play, crossfade, stop

```csharp
// Play (crossfades if another track is already playing)
AudioSystem.PlayBgm("Audio/bgm_main");

// Switch track — old one fades out, new one fades in (duration from Settings)
AudioSystem.PlayBgm("Audio/bgm_boss");

// Stop with fade
AudioSystem.StopBgm();

// Pause / Resume (e.g. game paused)
AudioSystem.PauseBgm();
AudioSystem.ResumeBgm();

// Async version — await the crossfade to complete
await AudioSystem.PlayBgmAsync("Audio/bgm_main", cancellationToken);
```

### SFX — one-shot, loop, 3D, pitch ramp

```csharp
// One-shot (pooled, fire-and-forget)
AudioSystem.PlaySfx("Audio/sfx_click");

// Direct AudioClip (no library entry needed)
AudioSystem.PlaySfx(myClip, volume: 0.8f);

// 3D positional one-shot
AudioSystem.PlaySfxAt("Audio/sfx_explosion", transform.position);

// Looping SFX (e.g. engine hum, footsteps)
AudioSystem.PlayLoopSfx("Audio/sfx_engine");
AudioSystem.StopLoopSfx("Audio/sfx_engine");

// Pitch ramp (e.g. rewind effect) — UniTask, no DOTween needed
AudioSystem.PlaySfx("Audio/sfx_rewind", startPitch: 2f, endPitch: 0.5f, duration: 1f);
```

### Ambience — fade in/out

```csharp
// Play with fade-in (duration from Settings → Ambience Fade Duration)
AudioSystem.PlayAmbience("Audio/amb_forest");

// Switch ambience — fades out old, fades in new
AudioSystem.PlayAmbience("Audio/amb_cave");

// Stop with fade-out
AudioSystem.StopAmbience();
```

### Volume control

```csharp
// Set volumes (0–1). Persisted automatically via IVolumeStore.
AudioSystem.SetMasterVolume(0.8f);
AudioSystem.SetBgmVolume(0.6f);
AudioSystem.SetSfxVolume(1f);
AudioSystem.SetAmbienceVolume(0.5f);

// Read current volumes
float master = AudioSystem.MasterVolume;
float bgm    = AudioSystem.BgmVolume;

// Mute / unmute (saves pre-mute volume internally)
AudioSystem.SetMute(true);
AudioSystem.SetMute(false);

// React to volume changes (e.g. update a UI slider from code)
AudioSystem.OnVolumeChanged += () =>
{
    volumeSlider.value = AudioSystem.MasterVolume;
};
```

### BGM Playlist (sequential)

```csharp
var playlist = AudioSystem.CreatePlaylist(
    "Audio/bgm_a",
    "Audio/bgm_b",
    "Audio/bgm_c"
);

playlist.Shuffle(); // optional

// Plays each track in sequence; loops back to start when all finish.
await playlist.PlayAllAsync(destroyCancellationToken);
```

### Inspector-driven components

Attach these to GameObjects to drive audio without code:

- **`BgmPlayer`** — plays a BGM key on `Start`, stops on `OnDestroy`.
- **`SfxPlayer`** — plays a SFX key on `OnEnable`, stops any loop on `OnDisable`.

---

## Using with Addressables

The package ships a sample that integrates with [`com.kidzdev.unity.addressables-toolkit`](https://github.com/knabsiraphop/kidzdev-unity-addressables-toolkit).

### 1 — Import the sample

In **Package Manager**, find *KidzDev Unity Audio* and import the **Addressables Loader** sample. This adds `AddressablesSoundClipLoader.cs` (and its asmdef) to your project.

### 2 — Mark clips as Addressable

In the Unity Addressables window, add your audio clips and note their **Addressable address** (e.g. `Audio/bgm_main`). Use the same value as the **Key** in `SoundLibrary`.

### 3 — Enable in Settings

Check **Addressable** on your `AudioServiceSettings` asset. The system will automatically use `AddressablesSoundClipLoader` instead of the Resources loader.

### 4 — Initialize (same as usual)

```csharp
// The init flow is identical — the loader swap is transparent.
AudioSystem.Configure();
await AudioSystem.InitializeAsync(destroyCancellationToken);

// Now clips are loaded from Addressables, ref-counted via AssetScope.
AudioSystem.PlayBgm("Audio/bgm_main");
```

### How it works under the hood

`AddressablesSoundClipLoader` implements `ISoundClipLoader` using `IAssetLoader` from the Addressables Toolkit. Every clip is ref-counted via `AssetScope` — `Release` / `ReleaseCategory` correctly decrements the ref count and unloads the bundle when no other scope holds it.

```csharp
// Manual plug-in (if you're not using AudioServiceSettings.Addressable)
var loader = new AddressablesSoundClipLoader();
AudioSystem.Configure(settings, loader: loader);
await AudioSystem.InitializeAsync(destroyCancellationToken);
```

---

## Custom Volume Persistence

By default volumes are stored in `PlayerPrefs`. Swap in any backend by implementing `IVolumeStore`:

```csharp
// Option A — delegate callbacks (no class needed)
var store = new DelegateVolumeStore(
    getMaster:      () => SaveData.MasterVolume,
    saveMaster:     v  => SaveData.MasterVolume = v,
    getBgm:         () => SaveData.BgmVolume,
    saveBgm:        v  => SaveData.BgmVolume = v,
    getSfx:         () => SaveData.SfxVolume,
    saveSfx:        v  => SaveData.SfxVolume = v,
    getAmbience:    () => SaveData.AmbienceVolume,
    saveAmbience:   v  => SaveData.AmbienceVolume = v
);

AudioSystem.Configure(settings, volumeStore: store);

// Option B — full IVolumeStore implementation
public class MyVolumeStore : IVolumeStore
{
    public float GetMasterVolume()         => ...; 
    public void  SaveMasterVolume(float v) => ...;
    public float GetBgmVolume()            => ...;
    public void  SaveBgmVolume(float v)    => ...;
    public float GetSfxVolume()            => ...;
    public void  SaveSfxVolume(float v)    => ...;
    public float GetAmbienceVolume()       => ...;
    public void  SaveAmbienceVolume(float v) => ...;
}
```

---

## Memory Management

```csharp
// Release all cached clips for one category (good for unloading a level's assets)
AudioSystem.ReleaseCategory(SoundCategory.SFX);

// Release everything (called automatically on destroy)
AudioSystem.Release();
```

---

## Full Use-Case Flow (example game scene)

```csharp
public class LevelManager : MonoBehaviour
{
    async void Start()
    {
        // 1. Boot (idempotent — safe if already initialized from a previous scene)
        AudioSystem.Configure();
        await AudioSystem.InitializeAsync(destroyCancellationToken);

        // 2. Start level music
        AudioSystem.PlayBgm("Audio/bgm_level1");

        // 3. Start looping ambience
        AudioSystem.PlayAmbience("Audio/amb_wind");
    }

    // Called when player opens the pause menu
    public void OnPause()
    {
        AudioSystem.PauseBgm();
        AudioSystem.SetMasterVolume(AudioSystem.MasterVolume * 0.5f);
    }

    public void OnResume()
    {
        AudioSystem.ResumeBgm();
        AudioSystem.SetMasterVolume(AudioSystem.MasterVolume * 2f);
    }

    // Called when player picks up a coin
    public void OnCoinCollected()
    {
        AudioSystem.PlaySfx("Audio/sfx_coin");
    }

    // Called when player fires a weapon
    public void OnShoot(Vector3 muzzlePos)
    {
        AudioSystem.PlaySfxAt("Audio/sfx_gunshot", muzzlePos);
    }

    // Called when transitioning to the boss arena
    async void OnEnterBossArena()
    {
        // Crossfade BGM (non-blocking — fade happens in background)
        AudioSystem.PlayBgm("Audio/bgm_boss");

        // Switch ambience with fade
        AudioSystem.PlayAmbience("Audio/amb_cave");
    }

    void OnDestroy()
    {
        // ReleaseCategory frees Resources/Addressables memory for level-specific clips
        AudioSystem.ReleaseCategory(SoundCategory.SFX);
        AudioSystem.ReleaseCategory(SoundCategory.Ambience);
    }
}
```

---

## Samples

| Sample | Contents |
|---|---|
| **Demo** | Ready-to-run scene: BGM crossfade, SFX pool, 3D one-shot, loop SFX, ambience, volume sliders, mute, playlist |
| **Addressables Loader** | `AddressablesSoundClipLoader` — plug-in ISoundClipLoader backed by `IAssetLoader`/`AssetScope` from `com.kidzdev.unity.addressables-toolkit` |

---

## Editor Tools

| Menu item | What it does |
|---|---|
| **Tools ▸ Audio ▸ Create Settings** | Creates `Assets/Resources/AudioServiceSettings.asset` |
| **Tools ▸ Audio ▸ Create Sound Library** | Creates `Assets/Resources/SoundLibrary.asset` |
| **Tools ▸ Audio ▸ Validate Library** | Checks all `SoundLibrary` assets for empty keys, duplicate keys, and Resources-unresolvable clips |

---

## License

MIT — see [LICENSE.md](LICENSE.md).
