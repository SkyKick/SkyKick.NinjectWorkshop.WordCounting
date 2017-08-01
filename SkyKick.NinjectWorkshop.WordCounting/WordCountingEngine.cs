using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Logging;
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

        private readonly ILogger _logger;

        public WordCountingEngine(IWebTextSource webTextSource, IWordCountingAlgorithm wordCountingAlgorithm, ILogger logger)
        {
            _webTextSource = webTextSource;
            _wordCountingAlgorithm = wordCountingAlgorithm;
            _logger = logger;
        }

        public async Task<int> CountWordsOnUrlAsync(string url, CancellationToken token)
        {
            _logger.Debug($"Counting Words on [{url}]");

            var text = await _webTextSource.GetTextFromUrlAsync(url, token);

            return _wordCountingAlgorithm.CountWordsInString(text);
        }
    }
}
