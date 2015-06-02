using System.Collections.Generic;

namespace DC.EPi.SimpleSearch.Models
{
    public class SearchResult
    {
        public string SearchText { get; set; }
        public int TotalHits { get; set; }
        public List<SearchHit> Hits { get; set; }

        public bool HasHits
        {
            get { return Hits != null && Hits.Count > 0; }
        }

        public bool HasPagination { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
    }
}