using EPiServer.Core;

namespace DC.EPi.SimpleSearch.Models
{
    public class SearchHit
    {
        public ContentReference ContentLink { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public float Score { get; set; }
        public string HighlightedText { get; set; }
    }
}