using System.Collections.Generic;
using UnityEngine;

namespace KidzDev.Unity.Audio
{
    [CreateAssetMenu(menuName = "KidzDev/Audio/Sound Library", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField] List<SoundEntry> _entries = new();

        Dictionary<string, SoundEntry> _map;

        public IReadOnlyList<SoundEntry> Entries => _entries;

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
