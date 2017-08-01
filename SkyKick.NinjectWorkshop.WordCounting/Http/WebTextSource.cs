using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace SkyKick.NinjectWorkshop.WordCounting.Http
{
    public interface IWebTextSource
    {
        Task<string> GetTextFromUrlAsync(string url, CancellationToken token);
    }

    internal class WebTextSource : IWebTextSource
    {
        private readonly IWebClient _webClient;
        private readonly WebTextSourceOptions _options;

        public WebTextSource(IWebClient webClient, WebTextSourceOptions options)
        {
            _webClient = webClient;
            _options = options;
        }

        public async Task<string> GetTextFromUrlAsync(string url, CancellationToken token)
        {
            var policy =
                Polly.Policy
                    .Handle<WebException>(webException => 
                        (webException.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.InternalServerError)
                    .Or<Exception>(ex => !(ex is WebException))
                    .WaitAndRetryAsync(_options.RetryTimes);

            var html = await policy.ExecuteAsync( _ => _webClient.GetHtmlAsync(url, token), token);

            return new CsQuery.CQ(html).Text();
        }
    }
}
