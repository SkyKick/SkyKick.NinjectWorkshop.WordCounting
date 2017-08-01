using System;
using System.Collections.Generic;
using SkyKick.Bcl.Logging;
using SkyKick.NinjectWorkshop.WordCounting.Threading;

namespace SkyKick.NinjectWorkshop.WordCounting.Cache
{
    public interface IWordCountCache
    {
        bool TryGet(string key, out int value);
        void Add(string key, int value);
    }

    internal class WordCountCache : IWordCountCache
    {
        private readonly Dictionary<string, int> _cache = new Dictionary<string, int>();

        private readonly ILogger _logger;
        private readonly IThreadSleeper _threadSleeper;

        public WordCountCache(ILogger logger, IThreadSleeper threadSleeper)
        {
            _logger = logger;
            _threadSleeper = threadSleeper;
        }

        public bool TryGet(string key, out int value)
        {
            EnsureInitialized();

            var cacheHit =  _cache.TryGetValue(key, out value);

            _logger.Info( (cacheHit ? "Cache Hit" : "Cache Miss") + $": {key}");

            return cacheHit;
        }

        public void Add(string key, int value)
        {
            EnsureInitialized();

            _cache[key] = value;
        }

        private bool _isInitialized;

        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            _logger.Warn("Initializing Cache");

            _threadSleeper.Sleep(TimeSpan.FromSeconds(3));

            _isInitialized = true;
        }
    }
}
