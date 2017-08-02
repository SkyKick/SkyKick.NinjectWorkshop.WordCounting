using System;
using System.Net;
using SkyKick.Bcl.Extensions.Reflection;

namespace SkyKick.NinjectWorkshop.WordCounting.Tests.Helpers
{
    public static class WebExceptionHelper
    {
        /// <summary>
        /// Have to use reflection to build <see cref="WebException"/>
        /// because Microsoft doesn't provide public constructors / setters
        /// <para />
        /// This leverages tools from <see cref="SkyKick.Bcl.Extensions.Reflection"/>
        /// to make it a bit easier.
        /// </summary>
        public static WebException CreateWebExceptionWithStatusCode(HttpStatusCode status)
        {
            var httpWebResponse = 
                (HttpWebResponse)
                Activator.CreateInstance(
                    typeof(HttpWebResponse), 
                    false);

            typeof(HttpWebResponse)
                .CreateFieldAccessor<HttpStatusCode>("m_StatusCode")
                .Set(httpWebResponse, status);

            var webException = new WebException("");

            typeof(WebException)
                .CreateFieldAccessor<WebResponse>("m_Response")
                .Set(webException, httpWebResponse);

            return webException;
        }
    }
}