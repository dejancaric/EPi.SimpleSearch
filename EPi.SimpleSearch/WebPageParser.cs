using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using DC.EPi.SimpleSearch.Helpers;
using DC.EPi.SimpleSearch.Models;
using EPiServer.ServiceLocation;
using HtmlAgilityPack;

namespace DC.EPi.SimpleSearch
{
    [ServiceConfiguration(typeof(IWebPageParser))]
    public class WebPageParser : IWebPageParser
    {
        public EpiPage GetPage(string pageUrl)
        {
            using (var client = new HttpClient())
            {
                var uri = UrlUtils.ConvertToUri(pageUrl);
                string html = client.GetStringAsync(uri).Result;

                return ParseHtml(html, uri);
            }
        }

        private static EpiPage ParseHtml(string html, Uri uri)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // remove inline scripts
            foreach (var node in document.DocumentNode.Descendants("script").ToArray())
            {
                node.Remove();
            }

            // remove inline styles
            foreach (var node in document.DocumentNode.Descendants("style").ToArray())
            {
                node.Remove();
            }

            // remove all elements that contain data-nosearch attribute
            var nosearchNodes = document.DocumentNode.SelectNodes("//*[@data-nosearch]");
            if (nosearchNodes != null)
            {
                foreach (var node in nosearchNodes)
                {
                    node.Remove();
                }
            }

            var webPage = new EpiPage { PageUrl = uri.AbsoluteUri };

            // fetch page title
            var titleNode = document.DocumentNode.Descendants("title").SingleOrDefault();
            if (titleNode != null)
            {
                webPage.Title = StringUtils.DecodeAndRemoveSpaces(titleNode.InnerText ?? "");
            }

            // fetch text inside Body
            string innerText = (document.DocumentNode.SelectSingleNode("//body").InnerText ?? "").Trim().Replace("</form>", "");

            // prettify body text
            var sb = new StringBuilder();
            var lines = innerText.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string trimmed = StringUtils.DecodeAndRemoveSpaces(line);
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(trimmed);
                }
            }

            webPage.Text = sb.ToString();

            return webPage;
        }
    }
}