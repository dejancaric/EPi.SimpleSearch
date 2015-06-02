using EPiServer.Core;

namespace DC.EPi.SimpleSearch
{
    public interface ISearchablePage : IContent
    {
        bool ExcludeFromSearch { get; }
    }
}