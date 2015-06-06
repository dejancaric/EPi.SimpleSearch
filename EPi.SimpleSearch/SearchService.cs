using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using DC.EPi.SimpleSearch.Filters;
using DC.EPi.SimpleSearch.Helpers;
using DC.EPi.SimpleSearch.Models;
using EPiServer.Core;
using EPiServer.Filters;
using EPiServer.ServiceLocation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace DC.EPi.SimpleSearch
{
    [ServiceConfiguration(typeof(ISearchService))]
    public class SearchService : ISearchService
    {
        protected readonly FSDirectory _luceneDirectory;
        private readonly IWebPageParser _webPageParser;
        private readonly List<PageFilterBase> _pageFilters;

        public SearchService(IWebPageParser webPageParser)
        {
            var filePath = ConfigurationManager.AppSettings["simplesearch_filepath"];
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = HostingEnvironment.MapPath("~/App_Data/simple_search/");
            }

            _webPageParser = webPageParser;

            _luceneDirectory = FSDirectory.Open(filePath);
            using (var writer = GetIndexWriter())
            {
                writer.Optimize();
                writer.Commit();
            }

            _pageFilters = new List<PageFilterBase>
            {
                new FilterPublished(PagePublishedStatus.Published),
                new FilterPublicAccess(),
                new FilterTemplate()
            };
        }

        protected IndexWriter GetIndexWriter()
        {
            var writer = new IndexWriter(_luceneDirectory,
                                         new StandardAnalyzer(Version.LUCENE_30),
                                         IndexWriter.MaxFieldLength.UNLIMITED);

            return writer;
        }

        public int GetNumberOfDocuments()
        {
            using (var searcher = new IndexSearcher(_luceneDirectory))
            {
                int numberOfDocuments = searcher.IndexReader.NumDocs();
                return numberOfDocuments;
            }
        }

        public virtual void IndexPage(PageData pageData)
        {
            if (!ShouldIndexPage(pageData))
            {
                return;
            }
            
            var document = GetDefaultDocument(pageData);
            IndexDocumentWithoutCommit(document);
        }

        protected virtual Document GetDefaultDocument(PageData pageData)
        {
            var pageUrl = UrlUtils.GetExternalUrl(pageData.PageLink);

            var epiPage = _webPageParser.GetPage(pageUrl);
            epiPage.InheritedTypes = pageData.GetType().GetBaseTypes();
            epiPage.ContentLink = pageData.ContentLink;

            var document = new Document();

            document.Add(new Field("url", epiPage.PageUrl, Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("title", epiPage.Title ?? "", Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("text", epiPage.Text ?? "", Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("title_lowercased", (epiPage.Title ?? "").Trim().ToLowerInvariant(),
                                   Field.Store.YES, Field.Index.NOT_ANALYZED));
            foreach (var type in epiPage.InheritedTypes)
            {
                document.Add(new Field("type", type, Field.Store.YES, Field.Index.NOT_ANALYZED));
            }
            document.Add(new Field("page_id", epiPage.ContentLink.ID.ToString(), Field.Store.YES,
                                   Field.Index.NOT_ANALYZED));
            return document;
        }

        public virtual void IndexDocumentWithoutCommit(Document document)
        {
            using (var writer = GetIndexWriter())
            {
                writer.AddDocument(document);
                writer.UpdateDocument(new Term("url", document.Get("url")), document);
            }
        }

        public void Commit()
        {
            using (var writer = GetIndexWriter())
            {
                writer.Optimize();
                writer.Commit();
            }
        }

        public void DeleteAll()
        {
            using (var writer = GetIndexWriter())
            {
                writer.DeleteAll();
            }
        }

        public void Rollback()
        {
            using (var writer = GetIndexWriter())
            {
                writer.Rollback();
            }
        }

        public SearchResult Search(SearchRequest request)
        {
            using (var searcher = new IndexSearcher(_luceneDirectory))
            {
                var searchResult = new SearchResult { SearchText = request.Text };

                if (string.IsNullOrEmpty(request.Text))
                {
                    return new SearchResult();
                }

                // boosts: hits in title are more relevant than hits in main body
                var boosts = new Dictionary<string, float> { { "text", 1 }, { "title", 1.6f } };
                // find hits in both title and text field, and apply above boosts
                var parser = new MultiFieldQueryParser(Version.LUCENE_30, new[] { "title", "text" },
                                                       new StandardAnalyzer(Version.LUCENE_30), boosts);

                var q = parser.Parse(request.Text);

                Query mainQuery;

                if (request.FilteredTypes != null && request.FilteredTypes.Length > 0)
                {
                    var filterQuery = new BooleanQuery();
                    for (int i = 0; i < request.FilteredTypes.Length; i++)
                    {
                        filterQuery.Add(new TermQuery(new Term("type", request.FilteredTypes[i].FullName)), Occur.SHOULD);
                    }

                    mainQuery = new BooleanQuery();
                    ((BooleanQuery)mainQuery).Add(q, Occur.MUST);
                    ((BooleanQuery)mainQuery).Add(filterQuery, Occur.MUST);
                }
                else
                {
                    mainQuery = q;
                }

                var skipDoc = (request.PageNumber - 1) * request.PageSize;
                var topDocs = searcher.Search(mainQuery, skipDoc + request.PageSize);
                searchResult.TotalHits = topDocs.TotalHits;

                if (topDocs.TotalHits > 0)
                {
                    // code highlight
                    var formater = new SimpleHTMLFormatter("<em>", "</em>");
                    var fragmenter = new SimpleFragmenter(200);
                    var scorer = new QueryScorer(q);
                    var highlighter = new Highlighter(formater, scorer) { TextFragmenter = fragmenter };

                    var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                    searchResult.Hits = new List<SearchHit>(topDocs.ScoreDocs.Length);

                    for (int i = skipDoc; i < topDocs.ScoreDocs.Length; i++)
                    {
                        int luceneDocumentId = topDocs.ScoreDocs[i].Doc;
                        var score = topDocs.ScoreDocs[i].Score;

                        var luceneDocument = searcher.Doc(luceneDocumentId);

                        TokenStream stream = analyzer.TokenStream("", new StringReader(luceneDocument.Get("text")));
                        string highlightedText = highlighter.GetBestFragments(stream, luceneDocument.Get("text"), 1,
                                                                              "...");
                        
                        string pageId = luceneDocument.Get("page_id");

                        var searchHit = new SearchHit
                        {
                            Url = luceneDocument.Get("url"),
                            Title = luceneDocument.Get("title"),
                            Text = luceneDocument.Get("text"),
                            HighlightedText = highlightedText,
                            Score = score,
                            ContentLink = string.IsNullOrEmpty(pageId)
                                                ? ContentReference.EmptyReference
                                                : new ContentReference(int.Parse(pageId))
                        };

                        searchResult.Hits.Add(searchHit);
                    }

                    searchResult.HasPagination = searchResult.Hits.Count < searchResult.TotalHits;
                    searchResult.TotalPages = (int)Math.Ceiling((double)searchResult.TotalHits / request.PageSize);
                    searchResult.PageNumber = request.PageNumber > searchResult.TotalPages ? 1 : request.PageNumber;

                    searchResult.HasPreviousPage = searchResult.PageNumber > 1;
                    searchResult.HasNextPage = searchResult.TotalPages > 1 &&
                                               searchResult.PageNumber < searchResult.TotalPages;
                }

                return searchResult;
            }
        }

        public List<AutocompleteSearchHit> AutocompleteSearch(string text, int numberOfHits)
        {
            using (var searcher = new IndexSearcher(_luceneDirectory))
            {
                var result = new List<AutocompleteSearchHit>();

                string searchText = text.ToLowerInvariant().Trim().Replace("*", "");
                var query = new WildcardQuery(new Term("title_lowercased", searchText + "*"));
                var topDocs = searcher.Search(query, numberOfHits);

                for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
                {
                    int luceneDocumentId = topDocs.ScoreDocs[i].Doc;
                    var luceneDocument = searcher.Doc(luceneDocumentId);

                    var hit = new AutocompleteSearchHit
                    {
                        Title = luceneDocument.Get("title"),
                        Url = luceneDocument.Get("url")
                    };

                    if (!string.IsNullOrEmpty(hit.Title) && !string.IsNullOrEmpty(hit.Url))
                    {
                        result.Add(hit);
                    }
                }

                return result;
            }
        }

        protected bool ShouldIndexPage(PageData page)
        {
            var searchablePage = page as ISearchablePage;
            if (searchablePage == null || searchablePage.ExcludeFromSearch)
            {
                return false;
            }

            if (_pageFilters.Any(filter => filter.ShouldFilter(page)))
            {
                return false;
            }

            return true;
        }
    }
}