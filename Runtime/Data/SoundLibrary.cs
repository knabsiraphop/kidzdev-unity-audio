using System.Collections.Generic;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Authoring asset mapping playback keys to <see cref="SoundEntry"/> settings. Assign one to
    /// <see cref="AudioServiceSettings"/>; every key passed to the service is looked up here, and
    /// keys with no entry still play using default settings.
    /// </summary>
    [CreateAssetMenu(menuName = "KidzDev/Audio/Sound Library", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField] List<SoundEntry> _entries = new();

        Dictionary<string, SoundEntry> _map;

        public IReadOnlyList<SoundEntry> Entries => _entries;

        /// <summary>
        /// Rebuilds the key lookup. Called automatically on first <see cref="TryGet"/> and by
        /// <see cref="IAudioService.Configure"/>; call it yourself only after mutating entries at
        /// runtime. Entries with a null or empty key are skipped, and later duplicates of a key win.
        /// </summary>
        public void BuildMap()
        {
            _map = new Dictionary<string, SoundEntry>(_entries.Count);
            foreach (var e in _entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Key)) continue;
                _map[e.Key] = e;
            }
        }

        public bool TryGet(string key, out SoundEntry entry)
        {
            if (_map == null) BuildMap();
            return _map.TryGetValue(key, out entry);
        }

        /// <summary>Returns every entry in <paramref name="category"/> as a newly allocated list — avoid calling it per frame.</summary>
        public List<SoundEntry> GetByCategory(SoundCategory category)
        {
            var result = new List<SoundEntry>();
            foreach (var e in _entries)
                if (e != null && e.Category == category)
                    result.Add(e);
            return result;
        }

#if UNITY_EDITOR
        public List<SoundEntry> EditorEntries => _entries;
#endif
    }
}
