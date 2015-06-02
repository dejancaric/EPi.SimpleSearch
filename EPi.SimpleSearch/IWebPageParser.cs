using DC.EPi.SimpleSearch.Models;

namespace DC.EPi.SimpleSearch
{
    public interface IWebPageParser
    {
        EpiPage GetPage(string pageUrl);
    }
}