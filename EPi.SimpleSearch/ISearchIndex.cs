using System.Collections.Generic;
using DC.EPi.SimpleSearch.Models;
using EPiServer.Core;

namespace DC.EPi.SimpleSearch
{
    public interface ISearchIndex
    {
        int GetNumberOfDocuments();
        void IndexPage(PageData page);

        void Commit();
        void DeleteAll();
        void Rollback();

        SearchResult Search(SearchRequest request);
        List<AutocompleteSearchHit> AutocompleteSearch(string text, int numberOfHits);
    }
}