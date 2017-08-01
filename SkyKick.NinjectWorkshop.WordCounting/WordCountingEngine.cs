using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Logging;
using SkyKick.NinjectWorkshop.WordCounting.Cache;
using SkyKick.NinjectWorkshop.WordCounting.Http;

namespace SkyKick.NinjectWorkshop.WordCounting
{
    public interface IWordCountingEngine
    {
        Task<int> CountWordsOnUrlAsync(string url, CancellationToken token);
    }

    internal class WordCountingEngine : IWordCountingEngine
    {
        private readonly IWebTextSource _webTextSource;
        private readonly IWordCountingAlgorithm _wordCountingAlgorithm;
        private readonly IWordCountCache _wordCountCache;

        private readonly ILogger _logger;

        public WordCountingEngine(IWebTextSource webTextSource, IWordCountingAlgorithm wordCountingAlgorithm, ILogger logger, IWordCountCache wordCountCache)
        {
            _webTextSource = webTextSource;
            _wordCountingAlgorithm = wordCountingAlgorithm;
            _logger = logger;
            _wordCountCache = wordCountCache;
        }

        public async Task<int> CountWordsOnUrlAsync(string url, CancellationToken token)
        {
            _logger.Debug($"Counting Words on [{url}]");

            int wordCount;
            if (_wordCountCache.TryGet(url, out wordCount))
                return wordCount;

            var text = await _webTextSource.GetTextFromUrlAsync(url, token);

            wordCount = _wordCountingAlgorithm.CountWordsInString(text);

            _wordCountCache.Add(url, wordCount);

            return wordCount;
        }
    }
}
