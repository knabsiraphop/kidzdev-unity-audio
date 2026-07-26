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

        /// <summary>
        /// Sources handed out by <see cref="TryRentExclusive"/>. They own their clip until returned,
        /// so neither <see cref="Rent"/> nor a second exclusive rental may hand them out again —
        /// otherwise a loop's source gets its clip reassigned while the loop still references it.
        /// </summary>
        readonly HashSet<AudioSource> _reserved = new();

        int _cursor;

        internal AudioSourcePool(Transform root, string sourceName, int initial, int cap)
        {
            _root       = root;
            _sourceName = sourceName;
            _cap        = cap;
            for (int i = 0; i < initial; i++)
                _sources.Add(CreateSource(i));
        }

        internal void SetMixerGroup(AudioMixerGroup group)
        {
            foreach (var src in _sources)
                src.outputAudioMixerGroup = group;
        }

        /// <summary>
        /// Shared rental for <c>PlayOneShot</c> callers, which never assign <c>.clip</c> — layering
        /// onto a source that is already playing is harmless, so at capacity this reuses the next
        /// unreserved source rather than cutting anything off. Returns null only when every source
        /// is currently reserved.
        /// </summary>
        internal AudioSource Rent()
        {
            var idle = FindIdleUnreserved();
            if (idle != null) return idle;

            var grown = Grow();
            if (grown != null) return grown;

            for (int i = 0; i < _sources.Count; i++)
            {
                int idx = (_cursor + i) % _sources.Count;
                if (_reserved.Contains(_sources[idx])) continue;
                _cursor = (idx + 1) % _sources.Count;
                return _sources[idx];
            }
            return null;
        }

        /// <summary>
        /// Exclusive rental for callers that assign <c>.clip</c> (keyed loops, pitch ramps). Fails
        /// instead of stealing a busy source, since stealing would silently kill the current
        /// borrower and leave it holding a reference to a reassigned source. Pair with
        /// <see cref="ReturnExclusive"/>.
        /// </summary>
        internal bool TryRentExclusive(out AudioSource source)
        {
            source = FindIdleUnreserved() ?? Grow();
            if (source == null) return false;
            _reserved.Add(source);
            return true;
        }

        /// <summary>Returns an exclusively rented source to the shared pool. Safe to call with null.</summary>
        internal void ReturnExclusive(AudioSource source)
        {
            if (source != null) _reserved.Remove(source);
        }

        AudioSource FindIdleUnreserved()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                int idx = (_cursor + i) % _sources.Count;
                var src = _sources[idx];
                if (src.isPlaying || _reserved.Contains(src)) continue;
                _cursor = (idx + 1) % _sources.Count;
                return src;
            }
            return null;
        }

        /// <summary>Adds one source if the pool is below its cap, otherwise returns null.</summary>
        AudioSource Grow()
        {
            if (_sources.Count >= _cap) return null;

            var grown = CreateSource(_sources.Count);
            _sources.Add(grown);
            if (_sources.Count > 1)
                grown.outputAudioMixerGroup = _sources[0].outputAudioMixerGroup;
            _cursor = 0;
            return grown;
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
            _reserved.Clear();
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
