using System.Collections.Generic;
using EPiServer.Core;

namespace DC.EPi.SimpleSearch.Models
{
    public class EpiPage
    {
        public string Title { get; set; }
        public string PageUrl { get; set; }
        public string Text { get; set; }
        public List<string> InheritedTypes { get; set; }
        public ContentReference ContentLink { get; set; }
    }
}