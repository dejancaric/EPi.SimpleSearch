using System;

namespace DC.EPi.SimpleSearch.Models
{
    public class SearchRequest
    {
        public string Text { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public Type[] FilteredTypes { get; set; } 
    }
}