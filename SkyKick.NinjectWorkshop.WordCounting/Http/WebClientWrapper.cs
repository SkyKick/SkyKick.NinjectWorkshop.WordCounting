using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Logging;

namespace SkyKick.NinjectWorkshop.WordCounting.Http
{
    public interface IWebClient
    {
        Task<string> GetHtmlAsync(string url, CancellationToken token);
    }

    internal class WebClientWrapper : IWebClient
    {
        private readonly ILogger _logger;

        public WebClientWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> GetHtmlAsync(string url, CancellationToken token)
        {
            _logger.Debug($"Downloading [{url}]");

            using (var client = new WebClient())
                return await client.DownloadStringTaskAsync(url);
        }
    }
}
