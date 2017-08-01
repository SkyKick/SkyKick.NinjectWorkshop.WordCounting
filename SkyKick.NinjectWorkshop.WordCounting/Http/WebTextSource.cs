using System.Threading;
using System.Threading.Tasks;

namespace SkyKick.NinjectWorkshop.WordCounting.Http
{
    public interface IWebTextSource
    {
        Task<string> GetTextFromUrlAsync(string url, CancellationToken token);
    }

    internal class WebTextSource : IWebTextSource
    {
        private readonly IWebClient _webClient;

        public WebTextSource(IWebClient webClient)
        {
            _webClient = webClient;
        }

        public async Task<string> GetTextFromUrlAsync(string url, CancellationToken token)
        {
            var html = await _webClient.GetHtmlAsync(url, token);

            return new CsQuery.CQ(html).Text();
        }
    }
}
