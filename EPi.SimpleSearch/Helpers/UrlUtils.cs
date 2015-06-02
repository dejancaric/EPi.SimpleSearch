using System;
using System.Text;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace DC.EPi.SimpleSearch.Helpers
{
    public class UrlUtils
    {
        public static Uri ConvertToUri(string url)
        {
            if (!url.StartsWith("http://"))
            {
                url = "http://" + url;
            }

            var uri = new Uri(url);
            return uri;
        }

        public static string GetExternalUrl(ContentReference content)
        {
            var internalUrl = UrlResolver.Current.GetUrl(content);
            var url = new UrlBuilder(internalUrl);
            Global.UrlRewriteProvider.ConvertToExternal(url, null, Encoding.UTF8);

            string externalUrl = HttpContext.Current == null
                                     ? UriSupport.AbsoluteUrlBySettings(url.ToString())
                                     : HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + url;

            return externalUrl;
        }
    }
}