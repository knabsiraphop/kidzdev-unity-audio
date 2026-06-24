using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace KidzDev.Unity.Audio
{
    internal sealed class AudioSourcePool
    {
        readonly Transform _root;
        readonly string _sourceName;
        readonly int _cap;
        readonly List<AudioSource> _sources = new();
        int _cursor;

        internal AudioSourcePool(Transform root, string sourceName, int initial, int cap)
        {
            _root       = root;
            _sourceName = sourceName;
            _cap        = cap;
            for (int i = 0; i < initial; i++)
                _sources.Add(CreateSource(i));
        }

        // Round-robin: find idle source → grow to cap → advance cursor (never cut off a playing source).
        internal AudioSource Rent()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                int idx = (_cursor + i) % _sources.Count;
                if (!_sources[idx].isPlaying)
                {
                    _cursor = (idx + 1) % _sources.Count;
                    return _sources[idx];
                }
            }

            if (_sources.Count < _cap)
            {
                var grown = CreateSource(_sources.Count);
                _sources.Add(grown);
                // Inherit mixer group from first source if set
                if (_sources.Count > 1)
                    grown.outputAudioMixerGroup = _sources[0].outputAudioMixerGroup;
                _cursor = 0;
                return grown;
            }

            // All busy and at cap: return cursor source (caller uses PlayOneShot, so no clip stomping).
            var next = _sources[_cursor];
            _cursor = (_cursor + 1) % _sources.Count;
            return next;
        }

        internal void SetMixerGroup(AudioMixerGroup group)
        {
            foreach (var src in _sources)
                src.outputAudioMixerGroup = group;
        }

        internal void Dispose()
        {
            foreach (var src in _sources)
            {
                if (src == null) continue;
                if (Application.isPlaying) Object.Destroy(src.gameObject);
                else Object.DestroyImmediate(src.gameObject);
            }
            _sources.Clear();
        }

        AudioSource CreateSource(int index)
        {
            var go = new GameObject($"{_sourceName}_{index}");
            go.transform.SetParent(_root, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 0f;
            return src;
        }
    }
}
