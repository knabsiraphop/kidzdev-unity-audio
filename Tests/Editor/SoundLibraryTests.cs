using NUnit.Framework;
using UnityEngine;

namespace KidzDev.Unity.Audio.Tests
{
    sealed class SoundLibraryTests
    {
        SoundLibrary CreateLibrary(params (string key, SoundCategory cat)[] entries)
        {
            var lib = ScriptableObject.CreateInstance<SoundLibrary>();
            foreach (var (key, cat) in entries)
                lib.EditorEntries.Add(new SoundEntry { Key = key, Category = cat, Volume = 1f });
            lib.BuildMap();
            return lib;
        }

        [Test]
        public void TryGet_ReturnsEntry_WhenKeyExists()
        {
            var lib = CreateLibrary(("sfx_click", SoundCategory.SFX));
            Assert.IsTrue(lib.TryGet("sfx_click", out var entry));
            Assert.AreEqual("sfx_click", entry.Key);
        }

        [Test]
        public void TryGet_ReturnsFalse_WhenKeyMissing()
        {
            var lib = CreateLibrary(("sfx_click", SoundCategory.SFX));
            Assert.IsFalse(lib.TryGet("does_not_exist", out _));
        }

        [Test]
        public void TryGet_BuildsMapLazily()
        {
            var lib = ScriptableObject.CreateInstance<SoundLibrary>();
            lib.EditorEntries.Add(new SoundEntry { Key = "bgm_a", Category = SoundCategory.BGM });
            // No explicit BuildMap call — TryGet must trigger it.
            Assert.IsTrue(lib.TryGet("bgm_a", out _));
        }

        [Test]
        public void GetByCategory_ReturnsOnlyMatchingEntries()
        {
            var lib = CreateLibrary(
                ("sfx_a", SoundCategory.SFX),
                ("sfx_b", SoundCategory.SFX),
                ("bgm_a", SoundCategory.BGM));

            var sfx = lib.GetByCategory(SoundCategory.SFX);
            Assert.AreEqual(2, sfx.Count);
            foreach (var e in sfx)
                Assert.AreEqual(SoundCategory.SFX, e.Category);
        }

        [Test]
        public void BuildMap_LastEntryWins_OnDuplicateKey()
        {
            var lib = ScriptableObject.CreateInstance<SoundLibrary>();
            lib.EditorEntries.Add(new SoundEntry { Key = "dup", Volume = 0.5f });
            lib.EditorEntries.Add(new SoundEntry { Key = "dup", Volume = 0.9f });
            lib.BuildMap();
            lib.TryGet("dup", out var e);
            Assert.AreEqual(0.9f, e.Volume, 0.001f);
        }
    }
}
