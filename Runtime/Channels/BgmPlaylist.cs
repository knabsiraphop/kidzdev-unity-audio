using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KidzDev.Unity.Audio
{
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

        // Plays each key in sequence. If durations is provided, waits that many seconds after each
        // track fades in before crossfading to the next (useful for non-looping BGM).
        // Pass no durations (or null) to crossfade immediately between tracks.
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
