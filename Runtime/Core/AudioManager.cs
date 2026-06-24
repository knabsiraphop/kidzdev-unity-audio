using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace KidzDev.Unity.Audio
{
    public sealed class AudioManager : IAudioService, IDisposable
    {
        // ── Collaborators (injectable) ────────────────────────────────────────────────
        ISoundClipLoader _loader;
        IVolumeStore _store;

        // ── Settings ──────────────────────────────────────────────────────────────────
        AudioServiceSettings _settings;
        SoundLibrary _library;
        AudioMixer _mixer;
        string _paramMaster   = "MasterVolume";
        string _paramBgm      = "BgmVolume";
        string _paramSfx      = "SfxVolume";
        string _paramAmbience = "AmbienceVolume";
        float _bgmFade        = 1f;
        float _ambienceFade   = 0.5f;

        // ── Channels ──────────────────────────────────────────────────────────────────
        readonly BgmChannel _bgm;
        readonly SfxChannel _sfx;
        readonly AmbienceChannel _ambience;

        // ── Lifetime ──────────────────────────────────────────────────────────────────
        readonly GameObject _root;
        CancellationToken _lifetimeCt;

        // ── BGM state (superseded-load guard) ────────────────────────────────────────
        string _currentBgmKey;
        string _pendingBgmKey;

        // ── Loop SFX in-flight cancel (fixes stop-before-load-completes race) ────────
        readonly Dictionary<string, CancellationTokenSource> _pendingLoopCts = new();

        // ── State ─────────────────────────────────────────────────────────────────────
        bool _isReady;

        // ── Volume ───────────────────────────────────────────────────────────────────
        float _masterVolume   = 1f;
        float _bgmVolume      = 1f;
        float _sfxVolume      = 1f;
        float _ambienceVolume = 1f;
        bool  _isMuted;
        // Pre-mute master saved so we can restore the exact value on unmute.
        float _preMuteMaster;

        public event Action OnVolumeChanged;

        public bool  IsReady       => _isReady;
        public float MasterVolume  => _masterVolume;
        public float BgmVolume     => _bgmVolume;
        public float SfxVolume     => _sfxVolume;
        public float AmbienceVolume => _ambienceVolume;
        public bool  IsMuted        => _isMuted;
        public string CurrentBgmKey => _currentBgmKey;

        // ── Constructor ───────────────────────────────────────────────────────────────

        public AudioManager(ISoundClipLoader loader = null, IVolumeStore store = null)
        {
            _loader = loader ?? new ResourcesSoundClipLoader();
            _store  = store  ?? new PlayerPrefsVolumeStore();

            _root = new GameObject("[AudioManager]");
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(_root);
            var audioRoot = _root.transform;

            _bgm      = new BgmChannel(audioRoot);
            _sfx      = new SfxChannel(audioRoot, 5, 12);
            _ambience = new AmbienceChannel(audioRoot);
        }

        // Set by AudioServiceRunner so fades/operations cancel when the scene owner is destroyed.
        public void SetLifetimeCancellationToken(CancellationToken ct) => _lifetimeCt = ct;

        // ── Init ─────────────────────────────────────────────────────────────────────

        public void Configure(AudioServiceSettings settings = null)
        {
            if (settings == null)
                settings = Resources.Load<AudioServiceSettings>("AudioServiceSettings");
            if (settings == null) return;

            _settings      = settings;
            _library       = settings.Library;
            _bgmFade       = settings.BgmFadeDuration;
            _ambienceFade  = settings.AmbienceFadeDuration;
            _paramMaster   = settings.ParamMaster;
            _paramBgm      = settings.ParamBgm;
            _paramSfx      = settings.ParamSfx;
            _paramAmbience = settings.ParamAmbience;

            _sfx.Reconfigure(settings.SfxPoolSize, settings.SfxPoolCap);
            SetMixer(settings.Mixer);

            // Re-key store to match settings keys
            if (_store is PlayerPrefsVolumeStore)
                _store = new PlayerPrefsVolumeStore(settings.MasterVolumeKey, settings.BgmVolumeKey, settings.SfxVolumeKey, settings.AmbienceVolumeKey);

            _library?.BuildMap();
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            RestoreVolumes();
            ApplyAllVolumes();

            if (_library != null)
                await WarmAsync(ct);

            _isReady = true;
        }

        // ── BGM ──────────────────────────────────────────────────────────────────────

        public void PlayBgm(string key) => PlayBgmAsync(key, _lifetimeCt).Forget();

        public async UniTask PlayBgmAsync(string key, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (key == _currentBgmKey) return; // dedupe

            _pendingBgmKey = key;

            var entry = GetEntry(key);

            var clip = await _loader.LoadAsync(key, ct);

            if (ct.IsCancellationRequested || _pendingBgmKey != key) return; // superseded

            _currentBgmKey = key;
            await _bgm.PlayAsync(clip, entry, _bgmFade, _lifetimeCt);
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            _currentBgmKey = null;
            _pendingBgmKey = null;
            _bgm.PlayDirect(clip, loop);
        }

        public void StopBgm()
        {
            _currentBgmKey = null;
            _pendingBgmKey = null;
            _bgm.StopAsync(_bgmFade, _lifetimeCt).Forget();
        }

        public void PauseBgm()  => _bgm.Pause();
        public void ResumeBgm() => _bgm.Resume();

        // ── SFX ──────────────────────────────────────────────────────────────────────

        public void PlaySfx(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            LoadAndPlaySfx(key, GetEntry(key)).Forget();
        }

        public void PlaySfx(AudioClip clip, float volume = 1f) => _sfx.PlayDirect(clip, volume);

        public void PlaySfx(string key, float startPitch, float endPitch, float duration)
        {
            if (string.IsNullOrEmpty(key)) return;
            LoadAndPitchRamp(key, GetEntry(key), startPitch, endPitch, duration).Forget();
        }

        public void PlaySfxAt(string key, Vector3 worldPos)
        {
            if (string.IsNullOrEmpty(key)) return;
            LoadAndPlayAt(key, GetEntry(key), worldPos).Forget();
        }

        public void PlayLoopSfx(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            // Cancel any in-flight load for this key so double-press doesn't duplicate.
            CancelPendingLoop(key);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCt);
            _pendingLoopCts[key] = cts;
            LoadAndPlayLoop(key, GetEntry(key), cts.Token).Forget();
        }

        public void StopLoopSfx(string key)
        {
            // Cancel in-flight load so stop wins even before the clip finishes loading.
            CancelPendingLoop(key);
            _sfx.StopLoop(key);
        }

        // ── Ambience ─────────────────────────────────────────────────────────────────

        public void PlayAmbience(string key)
        {
            if (string.IsNullOrEmpty(key) || key == _ambience.CurrentKey) return;
            LoadAndPlayAmbience(key, GetEntry(key)).Forget();
        }

        public void StopAmbience() => _ambience.StopAsync(_ambienceFade, _lifetimeCt).Forget();

        // ── Playlist ─────────────────────────────────────────────────────────────────

        public BgmPlaylist CreatePlaylist(params string[] keys) => new BgmPlaylist(this, keys);

        // ── Volume ───────────────────────────────────────────────────────────────────

        public void SetMasterVolume(float v01)
        {
            _masterVolume = AudioVolume.Clamp(v01);
            ApplyMixerVolume(_paramMaster, _masterVolume);
            _store.SaveMasterVolume(_masterVolume);
            OnVolumeChanged?.Invoke();
        }

        public void SetBgmVolume(float v01)
        {
            _bgmVolume = AudioVolume.Clamp(v01);
            ApplyMixerVolume(_paramBgm, _bgmVolume);
            _bgm.SetVolume(_mixer != null ? 1f : _bgmVolume);
            _store.SaveBgmVolume(_bgmVolume);
            OnVolumeChanged?.Invoke();
        }

        public void SetSfxVolume(float v01)
        {
            _sfxVolume = AudioVolume.Clamp(v01);
            ApplyMixerVolume(_paramSfx, _sfxVolume);
            _sfx.SetVolume(_mixer != null ? 1f : _sfxVolume);
            _store.SaveSfxVolume(_sfxVolume);
            OnVolumeChanged?.Invoke();
        }

        public void SetAmbienceVolume(float v01)
        {
            _ambienceVolume = AudioVolume.Clamp(v01);
            ApplyMixerVolume(_paramAmbience, _ambienceVolume);
            _ambience.SetVolume(_mixer != null ? 1f : _ambienceVolume);
            _store.SaveAmbienceVolume(_ambienceVolume);
            OnVolumeChanged?.Invoke();
        }

        public void SetMute(bool mute)
        {
            if (_isMuted == mute) return;
            _isMuted = mute;

            if (mute)
            {
                _preMuteMaster = _masterVolume;
                ApplyMixerVolume(_paramMaster, 0f);
                if (_mixer == null)
                {
                    _bgm.SetVolume(0f);
                    _sfx.SetVolume(0f);
                    _ambience.SetVolume(0f);
                }
            }
            else
            {
                ApplyMixerVolume(_paramMaster, _masterVolume);
                if (_mixer == null)
                {
                    _bgm.SetVolume(_bgmVolume);
                    _sfx.SetVolume(_sfxVolume);
                    _ambience.SetVolume(_ambienceVolume);
                }
            }

            OnVolumeChanged?.Invoke();
        }

        // ── Memory ───────────────────────────────────────────────────────────────────

        public void ReleaseCategory(SoundCategory category)
        {
            if (_library == null) return;

            // Stop active playback in this category first so sources don't hold freed handles.
            // Use immediate (0-fade) stops here — we're evicting clips, not UX-transitioning.
            switch (category)
            {
                case SoundCategory.BGM:
                    _currentBgmKey = null;
                    _pendingBgmKey = null;
                    _bgm.StopAsync(0f, _lifetimeCt).Forget();
                    break;
                case SoundCategory.Ambience:
                    _ambience.StopAsync(0f, _lifetimeCt).Forget();
                    break;
            }

            foreach (var e in _library.GetByCategory(category))
            {
                if (category == SoundCategory.SFX)
                {
                    CancelPendingLoop(e.Key);
                    _sfx.StopLoop(e.Key);
                }
                _loader.Release(e.Key);
            }
        }

        public void Release()
        {
            // Stop all active playback before evicting clips so AudioSources don't hold freed handles.
            _bgm.StopAsync(0f, _lifetimeCt).Forget();      // immediate stop (0-duration → no yield)
            _ambience.StopAsync(0f, _lifetimeCt).Forget();  // immediate stop
            CancelAllPendingLoops();
            _sfx.StopAllLoops();
            _currentBgmKey = null;
            _pendingBgmKey = null;
            _loader.ReleaseAll();
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────────

        public void OnApplicationPause(bool paused)
        {
            if (paused) PauseBgm();
            else ResumeBgm();
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        void CancelPendingLoop(string key)
        {
            if (!_pendingLoopCts.TryGetValue(key, out var cts)) return;
            cts.Cancel();
            cts.Dispose();
            _pendingLoopCts.Remove(key);
        }

        void CancelAllPendingLoops()
        {
            foreach (var cts in _pendingLoopCts.Values) { cts.Cancel(); cts.Dispose(); }
            _pendingLoopCts.Clear();
        }

        SoundEntry GetEntry(string key)
        {
            if (_library != null && _library.TryGet(key, out var e)) return e;
            return null;
        }

        void SetMixer(AudioMixer mixer)
        {
            _mixer = mixer;
            if (mixer == null) return;

            var bgmGroup      = FindGroup("BGM");
            var sfxGroup      = FindGroup("SFX");
            var ambienceGroup = FindGroup("Ambience");

            _bgm.SetMixerGroup(bgmGroup);
            _sfx.SetMixerGroup(sfxGroup);
            _ambience.SetMixerGroup(ambienceGroup);
        }

        AudioMixerGroup FindGroup(string name)
        {
            if (_mixer == null) return null;
            var groups = _mixer.FindMatchingGroups(name);
            return groups != null && groups.Length > 0 ? groups[0] : null;
        }

        void RestoreVolumes()
        {
            _masterVolume   = AudioVolume.Clamp(_store.GetMasterVolume());
            _bgmVolume      = AudioVolume.Clamp(_store.GetBgmVolume());
            _sfxVolume      = AudioVolume.Clamp(_store.GetSfxVolume());
            _ambienceVolume = AudioVolume.Clamp(_store.GetAmbienceVolume());
        }

        void ApplyAllVolumes()
        {
            ApplyMixerVolume(_paramMaster,   _masterVolume);
            ApplyMixerVolume(_paramBgm,      _bgmVolume);
            ApplyMixerVolume(_paramAmbience, _ambienceVolume);
            ApplyMixerVolume(_paramSfx,      _sfxVolume);

            if (_mixer == null)
            {
                _bgm.SetVolume(_bgmVolume);
                _sfx.SetVolume(_sfxVolume);
                _ambience.SetVolume(_ambienceVolume);
            }
        }

        void ApplyMixerVolume(string param, float v01)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(param, AudioVolume.RatioToDB(v01));
        }

        async UniTask WarmAsync(CancellationToken ct)
        {
            if (_settings == null || _library == null) return;

            List<SoundEntry> toWarm;
            switch (_settings.WarmStrategy)
            {
                case WarmStrategy.AllSfx:
                    toWarm = _library.GetByCategory(SoundCategory.SFX);
                    break;
                case WarmStrategy.ByCategory:
                    toWarm = new List<SoundEntry>();
                    foreach (var cat in _settings.WarmCategories)
                        toWarm.AddRange(_library.GetByCategory(cat));
                    break;
                default:
                    return;
            }

            var tasks = new UniTask[toWarm.Count];
            for (int i = 0; i < toWarm.Count; i++)
            {
                var key = toWarm[i].Key;
                tasks[i] = _loader.LoadAsync(key, ct).AsUniTask();
            }
            await UniTask.WhenAll(tasks);
        }

        async UniTaskVoid LoadAndPlaySfx(string key, SoundEntry entry)
        {
            var clip = await _loader.LoadAsync(key, _lifetimeCt);
            if (clip != null) _sfx.PlayOneShot(clip, entry?.Volume ?? 1f);
        }

        async UniTaskVoid LoadAndPitchRamp(string key, SoundEntry entry, float startPitch, float endPitch, float duration)
        {
            var clip = await _loader.LoadAsync(key, _lifetimeCt);
            if (clip != null) _sfx.PlayPitchRamp(clip, entry?.Volume ?? 1f, startPitch, endPitch, duration, _lifetimeCt).Forget();
        }

        async UniTaskVoid LoadAndPlayAt(string key, SoundEntry entry, Vector3 worldPos)
        {
            var clip = await _loader.LoadAsync(key, _lifetimeCt);
            if (clip != null) _sfx.PlayAt(clip, entry?.Volume ?? 1f, worldPos, _lifetimeCt);
        }

        async UniTaskVoid LoadAndPlayLoop(string key, SoundEntry entry, CancellationToken ct)
        {
            try
            {
                var clip = await _loader.LoadAsync(key, ct);
                if (!ct.IsCancellationRequested && clip != null)
                    _sfx.PlayLoop(key, clip, entry?.Volume ?? 1f);
            }
            catch (System.OperationCanceledException) { }
            finally { _pendingLoopCts.Remove(key); }
        }

        async UniTaskVoid LoadAndPlayAmbience(string key, SoundEntry entry)
        {
            var clip = await _loader.LoadAsync(key, _lifetimeCt);
            if (clip != null) await _ambience.PlayAsync(key, clip, entry, _ambienceFade, _lifetimeCt);
        }

        public void Dispose()
        {
            Release();
            // In EditMode (tests), destroy root first — children become fake-null so channel
            // Dispose calls skip their own Destroy checks without logging errors.
            if (_root != null && !Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(_root);
            _bgm.Dispose();
            _sfx.Dispose();
            _ambience.Dispose();
            if (_root != null && Application.isPlaying)
                UnityEngine.Object.Destroy(_root);
        }
    }
}
