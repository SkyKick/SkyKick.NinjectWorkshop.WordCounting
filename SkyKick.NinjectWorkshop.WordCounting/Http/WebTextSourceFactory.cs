namespace SkyKick.NinjectWorkshop.WordCounting.Http
{
    public interface IWebTextSourceFactory
    {
        ITextSource CreateWebTextSource(string url);
    }

    internal class WebTextSourceFactory : IWebTextSourceFactory
    {
        private readonly IWebClient _webClient;
        private readonly WebTextSourceOptions _options;
        
        public WebTextSourceFactory(IWebClient webClient, WebTextSourceOptions options)
        {
            _webClient = webClient;
            _options = options;
        }

        public ITextSource CreateWebTextSource(string url)
        {
            return new WebTextSource(_webClient, _options, url);
        }
    }
}