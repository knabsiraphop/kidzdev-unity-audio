using NUnit.Framework;

namespace KidzDev.Unity.Audio.Tests
{
    sealed class VolumeStoreTests
    {
        [Test]
        public void DelegateVolumeStore_GetSet_RoundTrip()
        {
            float master = 1f, bgm = 1f, sfx = 1f;
            var store = new DelegateVolumeStore(
                () => master, v => master = v,
                () => bgm,    v => bgm    = v,
                () => sfx,    v => sfx    = v);

            store.SaveMasterVolume(0.5f);
            store.SaveBgmVolume(0.7f);
            store.SaveSfxVolume(0.3f);

            Assert.AreEqual(0.5f, store.GetMasterVolume(), 0.001f);
            Assert.AreEqual(0.7f, store.GetBgmVolume(),    0.001f);
            Assert.AreEqual(0.3f, store.GetSfxVolume(),    0.001f);
        }

        [Test]
        public void PlayerPrefsVolumeStore_DefaultsToOne()
        {
            // Use unique keys to avoid leaking state between test runs.
            var store = new PlayerPrefsVolumeStore("_test_m", "_test_b", "_test_s");
            UnityEngine.PlayerPrefs.DeleteKey("_test_m");
            UnityEngine.PlayerPrefs.DeleteKey("_test_b");
            UnityEngine.PlayerPrefs.DeleteKey("_test_s");

            Assert.AreEqual(1f, store.GetMasterVolume(), 0.001f);
            Assert.AreEqual(1f, store.GetBgmVolume(),    0.001f);
            Assert.AreEqual(1f, store.GetSfxVolume(),    0.001f);
        }

        [Test]
        public void PlayerPrefsVolumeStore_SaveAndRestore()
        {
            var store = new PlayerPrefsVolumeStore("_test_m2", "_test_b2", "_test_s2");
            store.SaveMasterVolume(0.4f);
            store.SaveBgmVolume(0.6f);
            store.SaveSfxVolume(0.8f);

            // Re-create store to simulate app restart.
            var store2 = new PlayerPrefsVolumeStore("_test_m2", "_test_b2", "_test_s2");
            Assert.AreEqual(0.4f, store2.GetMasterVolume(), 0.001f);
            Assert.AreEqual(0.6f, store2.GetBgmVolume(),    0.001f);
            Assert.AreEqual(0.8f, store2.GetSfxVolume(),    0.001f);

            // Clean up.
            UnityEngine.PlayerPrefs.DeleteKey("_test_m2");
            UnityEngine.PlayerPrefs.DeleteKey("_test_b2");
            UnityEngine.PlayerPrefs.DeleteKey("_test_s2");
        }
    }
}
