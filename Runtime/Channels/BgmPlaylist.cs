using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KidzDev.Unity.Audio
{
    /// <summary>
    /// Ordered set of BGM keys returned by <see cref="IAudioService.CreatePlaylist"/>. Build it,
    /// optionally <see cref="Shuffle"/> it, then <see cref="PlayAllAsync"/> to play through.
    /// </summary>
    public sealed class BgmPlaylist
    {
        readonly IAudioService _service;
        readonly List<string> _keys;

        internal BgmPlaylist(IAudioService service, IEnumerable<string> keys)
        {
            _service = service;
            _keys    = new List<string>(keys);
        }

        public int Count => _keys.Count;
        public IReadOnlyList<string> Keys => _keys;

        /// <summary>Shuffles the keys in place and returns this playlist so calls can be chained.</summary>
        public BgmPlaylist Shuffle()
        {
            var rng = new Random();
            for (int i = _keys.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_keys[i], _keys[j]) = (_keys[j], _keys[i]);
            }
            return this;
        }

        /// <summary>
        /// Plays each key in sequence via <see cref="IAudioService.PlayBgmAsync"/>. When
        /// <paramref name="durations"/> is supplied, waits that many seconds after a track starts
        /// before crossfading to the next — useful for non-looping BGM. Tracks with no matching
        /// entry (index past the array, or a zero/negative value) crossfade immediately.
        /// </summary>
        /// <param name="durations">Per-track hold time in seconds after the track starts. Omit or pass null to crossfade straight through every track.</param>
        /// <param name="ct">Checked before each track starts; cancelling throws <see cref="OperationCanceledException"/>.</param>
        public async UniTask PlayAllAsync(float[] durations = null, CancellationToken ct = default)
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await _service.PlayBgmAsync(_keys[i], ct);

                if (durations != null && i < durations.Length && durations[i] > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(durations[i]), ignoreTimeScale: true, cancellationToken: ct);
            }
        }
    }
}
