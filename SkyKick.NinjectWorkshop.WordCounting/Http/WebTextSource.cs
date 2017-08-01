using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace SkyKick.NinjectWorkshop.WordCounting.Http
{
    /// <summary>
    /// Don't build / bind directly, use <see cref="IWebTextSourceFactory"/>
    /// </summary>
    internal class WebTextSource : ITextSource
    {
        private readonly IWebClient _webClient;
        private readonly WebTextSourceOptions _options;
        private readonly string _url;

        public WebTextSource(IWebClient webClient, WebTextSourceOptions options, string url)
        {
            _webClient = webClient;
            _options = options;
            _url = url;
        }

        public string TextSourceId => _url;

        public async Task<string> GetTextAsync(CancellationToken token)
        {
            var policy =
                Polly.Policy
                    .Handle<WebException>(webException => 
                        (webException.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.InternalServerError)
                    .Or<Exception>(ex => !(ex is WebException))
                    .WaitAndRetryAsync(_options.RetryTimes);

            var html = await policy.ExecuteAsync( _ => _webClient.GetHtmlAsync(_url, token), token);

            return new CsQuery.CQ(html).Text();
        }
    }
}
