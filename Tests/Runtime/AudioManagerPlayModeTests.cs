using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KidzDev.Unity.Audio.Tests
{
    sealed class AudioManagerPlayModeTests
    {
        AudioManager _manager;
        FakeLoader   _loader;

        [SetUp]
        public void SetUp()
        {
            _loader  = new FakeLoader();
            _manager = new AudioManager(_loader, new DelegateVolumeStore(
                () => 1f, _ => { },
                () => 1f, _ => { },
                () => 1f, _ => { }));
        }

        [TearDown]
        public void TearDown() => _manager?.Dispose();

        // ── BGM lifecycle ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PlayBgm_SetsCurrentBgmKey() => UniTask.ToCoroutine(async () =>
        {
            _manager.PlayBgm("bgm_a");
            await UniTask.Yield(); // one frame: sync FakeLoader completes, _currentBgmKey is set
            Assert.AreEqual("bgm_a", _manager.CurrentBgmKey);
        });

        [UnityTest]
        public IEnumerator StopBgm_ClearsBgmKeyImmediately() => UniTask.ToCoroutine(async () =>
        {
            _manager.PlayBgm("bgm_a");
            await UniTask.Yield();
            _manager.StopBgm(); // key cleared synchronously
            Assert.IsNull(_manager.CurrentBgmKey);
        });

        [UnityTest]
        public IEnumerator PlayBgm_Superseded_LatestKeyWins() => UniTask.ToCoroutine(async () =>
        {
            var slow     = new SlowFakeLoader(frameDelay: 4);
            var manager2 = new AudioManager(slow, new DelegateVolumeStore(
                () => 1f, _ => { },
                () => 1f, _ => { },
                () => 1f, _ => { }));

            manager2.PlayBgm("bgm_a"); // starts loading, takes 4 frames
            await UniTask.Yield();      // 1 frame in
            manager2.PlayBgm("bgm_b"); // supersedes bgm_a

            // Wait for bgm_b's load to finish (it also uses SlowFakeLoader, 4 more frames)
            for (int i = 0; i < 6; i++) await UniTask.Yield();

            Assert.AreEqual("bgm_b", manager2.CurrentBgmKey);
            manager2.Dispose();
        });

        // ── SFX lifecycle ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PlayLoopSfx_ThenStop_NoException() => UniTask.ToCoroutine(async () =>
        {
            _manager.PlayLoopSfx("sfx_loop");
            await UniTask.Yield();
            Assert.DoesNotThrow(() => _manager.StopLoopSfx("sfx_loop"));
        });

        // ── Volume events ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SetSfxVolume_FiresOnVolumeChanged() => UniTask.ToCoroutine(async () =>
        {
            bool fired = false;
            _manager.OnVolumeChanged += () => fired = true;
            _manager.SetSfxVolume(0.5f);
            await UniTask.Yield();
            Assert.IsTrue(fired);
        });

        // ── BGM dedupe ────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PlayBgm_SameKey_DoesNotReload() => UniTask.ToCoroutine(async () =>
        {
            _manager.PlayBgm("bgm_a");
            await UniTask.Yield();
            int countAfterFirst = _loader.LoadCallCount;

            _manager.PlayBgm("bgm_a"); // deduped
            await UniTask.Yield();
            Assert.AreEqual(countAfterFirst, _loader.LoadCallCount);
        });

        // ── Playlist ─────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator CreatePlaylist_PlayAllAsync_IteratesKeys() => UniTask.ToCoroutine(async () =>
        {
            var playlist = _manager.CreatePlaylist("bgm_a", "bgm_b");
            var played   = new List<string>();
            _loader.OnLoad = k => played.Add(k);

            using var cts = new CancellationTokenSource();
            // Run playlist but cancel after 2 tracks load
            var task = playlist.PlayAllAsync(ct: cts.Token);
            await UniTask.Yield();
            await UniTask.Yield();
            cts.Cancel();

            Assert.IsTrue(played.Count >= 1, "Playlist should have played at least one key");
        });

        // ── Helpers ───────────────────────────────────────────────────────────────────

        sealed class FakeLoader : ISoundClipLoader
        {
            public int LoadCallCount;
            public System.Action<string> OnLoad;
            readonly AudioClip _clip;

            public FakeLoader() => _clip = AudioClip.Create("fake", 44100, 1, 44100, false);

            public UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
            {
                LoadCallCount++;
                OnLoad?.Invoke(key);
                return UniTask.FromResult(_clip);
            }

            public void Release(string key) { }
            public void ReleaseAll()        { }
        }

        sealed class SlowFakeLoader : ISoundClipLoader
        {
            readonly int _frames;
            readonly AudioClip _clip;

            public SlowFakeLoader(int frameDelay)
            {
                _frames = frameDelay;
                _clip   = AudioClip.Create("slow_fake", 44100, 1, 44100, false);
            }

            public async UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
            {
                for (int i = 0; i < _frames; i++)
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                return _clip;
            }

            public void Release(string key) { }
            public void ReleaseAll()        { }
        }
    }
}
