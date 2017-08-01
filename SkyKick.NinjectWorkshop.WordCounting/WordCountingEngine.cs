using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Logging;
using SkyKick.NinjectWorkshop.WordCounting.Cache;
using SkyKick.NinjectWorkshop.WordCounting.Http;

namespace SkyKick.NinjectWorkshop.WordCounting
{
    public interface IWordCountingEngine
    {
        Task<int> CountWordsFromTextSourceAsync(ITextSource source, CancellationToken token);
    }

    internal class WordCountingEngine : IWordCountingEngine
    {
        private readonly IWordCountingAlgorithm _wordCountingAlgorithm;
        private readonly IWordCountCache _wordCountCache;

        private readonly ILogger _logger;

        public WordCountingEngine(
            IWordCountingAlgorithm wordCountingAlgorithm, 
            ILogger logger, 
            IWordCountCache wordCountCache)
        {
            _wordCountingAlgorithm = wordCountingAlgorithm;
            _logger = logger;
            _wordCountCache = wordCountCache;
        }

        public async Task<int> CountWordsFromTextSourceAsync(
            ITextSource source, 
            CancellationToken token)
        {
            _logger.Debug($"Counting Words on [{source.TextSourceId}]");

            int wordCount;
            if (_wordCountCache.TryGet(source.TextSourceId, out wordCount))
                return wordCount;

            var text = await source.GetTextAsync(token);

            wordCount = _wordCountingAlgorithm.CountWordsInString(text);

            _wordCountCache.Add(source.TextSourceId, wordCount);

            return wordCount;
        }
    }
}
