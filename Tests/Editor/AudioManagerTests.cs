using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace KidzDev.Unity.Audio.Tests
{
    sealed class AudioManagerTests
    {
        AudioManager _manager;
        FakeLoader   _loader;
        SoundLibrary _lib;

        [SetUp]
        public void SetUp()
        {
            _loader  = new FakeLoader();
            _manager = new AudioManager(_loader, new DelegateVolumeStore(
                () => 1f, _ => { },
                () => 1f, _ => { },
                () => 1f, _ => { }));

            _lib = ScriptableObject.CreateInstance<SoundLibrary>();
            _lib.EditorEntries.Add(new SoundEntry { Key = "bgm_a",  Category = SoundCategory.BGM });
            _lib.EditorEntries.Add(new SoundEntry { Key = "sfx_a",  Category = SoundCategory.SFX });
            _lib.EditorEntries.Add(new SoundEntry { Key = "sfx_b",  Category = SoundCategory.SFX });
            _lib.EditorEntries.Add(new SoundEntry { Key = "amb_a",  Category = SoundCategory.Ambience });
            _lib.BuildMap();

            ConfigureZeroFade();
        }

        [TearDown]
        public void TearDown()
        {
            _manager.Dispose();
            Object.DestroyImmediate(_lib);
        }

        // ── BGM state ─────────────────────────────────────────────────────────────────

        [Test]
        public void PlayBgm_SetsCurrentBgmKey()
        {
            _manager.PlayBgm("bgm_a");
            Assert.AreEqual("bgm_a", _manager.CurrentBgmKey);
        }

        [Test]
        public void StopBgm_ClearsBgmKey()
        {
            _manager.PlayBgm("bgm_a");
            _manager.StopBgm();
            Assert.IsNull(_manager.CurrentBgmKey);
        }

        [Test]
        public void Release_ClearsBgmKey()
        {
            _manager.PlayBgm("bgm_a");
            _manager.Release();
            Assert.IsNull(_manager.CurrentBgmKey);
        }

        [Test]
        public void PlayBgm_SameKey_Deduped_NoSecondLoad()
        {
            _manager.PlayBgm("bgm_a");
            int loadsBefore = _loader.LoadCallCount;
            _manager.PlayBgm("bgm_a");
            Assert.AreEqual(loadsBefore, _loader.LoadCallCount);
        }

        // ── Volume events ────────────────────────────────────────────────────────────

        [Test]
        public void SetMasterVolume_FiresOnVolumeChanged()
        {
            bool fired = false;
            _manager.OnVolumeChanged += () => fired = true;
            _manager.SetMasterVolume(0.5f);
            Assert.IsTrue(fired);
        }

        [Test]
        public void SetMasterVolume_ClampedToZeroOne()
        {
            _manager.SetMasterVolume(5f);
            Assert.AreEqual(1f, _manager.MasterVolume, 0.001f);

            _manager.SetMasterVolume(-1f);
            Assert.AreEqual(0f, _manager.MasterVolume, 0.001f);
        }

        // ── ReleaseCategory ───────────────────────────────────────────────────────────

        [Test]
        public void ReleaseCategory_SFX_ReleasesOnlySfxKeys()
        {
            _manager.ReleaseCategory(SoundCategory.SFX);

            Assert.IsTrue(_loader.ReleasedKeys.Contains("sfx_a"),  "sfx_a should be released");
            Assert.IsTrue(_loader.ReleasedKeys.Contains("sfx_b"),  "sfx_b should be released");
            Assert.IsFalse(_loader.ReleasedKeys.Contains("bgm_a"), "bgm_a must not be released");
            Assert.IsFalse(_loader.ReleasedKeys.Contains("amb_a"), "amb_a must not be released");
        }

        [Test]
        public void ReleaseCategory_BGM_ClearsBgmKey()
        {
            _manager.PlayBgm("bgm_a");
            _manager.ReleaseCategory(SoundCategory.BGM);
            Assert.IsNull(_manager.CurrentBgmKey);
        }

        [Test]
        public void ReleaseCategory_Ambience_ReleasesAmbienceKeys()
        {
            _manager.ReleaseCategory(SoundCategory.Ambience);
            Assert.IsTrue(_loader.ReleasedKeys.Contains("amb_a"));
        }

        // ── Release all ───────────────────────────────────────────────────────────────

        [Test]
        public void Release_CallsLoaderReleaseAll()
        {
            _manager.Release();
            Assert.IsTrue(_loader.ReleaseAllCalled);
        }

        // ── Loop SFX source ownership ─────────────────────────────────────────────────

        [Test]
        public void PlayLoopSfx_PoolExhausted_DoesNotStealAnActiveLoop()
        {
            ConfigureTinyPool();

            _manager.PlayLoopSfx("sfx_a");
            var src = SingleSfxSource();
            Assert.IsNotNull(src.clip, "loop sfx_a should own the pool's only source");

            LogAssert.Expect(LogType.Warning, new Regex("SFX pool exhausted"));
            _manager.PlayLoopSfx("sfx_b");

            _manager.StopLoopSfx("sfx_b");
            Assert.IsNotNull(
                src.clip,
                "sfx_b was rejected so it must never have been registered — stopping it must not tear down sfx_a's source");
        }

        [Test]
        public void StopLoopSfx_ReturnsSourceToPool_SoAnotherLoopCanTakeIt()
        {
            ConfigureTinyPool();

            _manager.PlayLoopSfx("sfx_a");
            _manager.StopLoopSfx("sfx_a");

            _manager.PlayLoopSfx("sfx_b");
            Assert.IsNotNull(
                SingleSfxSource().clip,
                "stopping a loop must un-reserve its source, otherwise the pool leaks and later loops starve");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        void ConfigureTinyPool()
        {
            var settings = ScriptableObject.CreateInstance<AudioServiceSettings>();
            var so = new SerializedObject(settings);
            so.FindProperty("_library").objectReferenceValue    = _lib;
            so.FindProperty("_bgmFadeDuration").floatValue      = 0f;
            so.FindProperty("_ambienceFadeDuration").floatValue = 0f;
            so.FindProperty("_sfxPoolSize").intValue            = 1;
            so.FindProperty("_sfxPoolCap").intValue             = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            _manager.Configure(settings);
            Object.DestroyImmediate(settings);
        }

        static AudioSource SingleSfxSource()
        {
            var root = GameObject.Find("[AudioManager]");
            Assert.IsNotNull(root, "[AudioManager] host object should exist while the manager is alive");

            var sfx = new List<AudioSource>();
            foreach (var src in root.GetComponentsInChildren<AudioSource>(true))
                if (src.gameObject.name.StartsWith("SfxSource"))
                    sfx.Add(src);

            Assert.AreEqual(1, sfx.Count, "a cap-1 pool should expose exactly one SFX source");
            return sfx[0];
        }

        void ConfigureZeroFade()
        {
            var settings = ScriptableObject.CreateInstance<AudioServiceSettings>();
            var so = new SerializedObject(settings);
            so.FindProperty("_library").objectReferenceValue          = _lib;
            so.FindProperty("_bgmFadeDuration").floatValue            = 0f;
            so.FindProperty("_ambienceFadeDuration").floatValue       = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            _manager.Configure(settings);
            Object.DestroyImmediate(settings);
        }

        sealed class FakeLoader : ISoundClipLoader
        {
            public int LoadCallCount;
            public bool ReleaseAllCalled;
            public readonly List<string> ReleasedKeys = new();
            readonly AudioClip _clip;

            public FakeLoader()
            {
                _clip = AudioClip.Create("fake", 44100, 1, 44100, false);
            }

            public UniTask<AudioClip> LoadAsync(string key, CancellationToken ct = default)
            {
                LoadCallCount++;
                return UniTask.FromResult(_clip);
            }

            public void Release(string key)     => ReleasedKeys.Add(key);

            public void ReleaseAll()
            {
                ReleaseAllCalled = true;
                ReleasedKeys.Clear();
            }
        }
    }
}
