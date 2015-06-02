using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace DC.EPi.SimpleSearch.Initialization
{
    [ModuleDependency(typeof(InitializationModule))]
    public class ContentEventsHandler : IInitializableModule
    {
        private IContentEvents _contentEvents;

        public void Initialize(InitializationEngine context)
        {
            if (_contentEvents == null)
            {
                _contentEvents = ServiceLocator.Current.GetInstance<IContentEvents>();
            }

            _contentEvents.PublishedContent += contentEvents_PublishedContent;
        }

        private void contentEvents_PublishedContent(object sender, ContentEventArgs e)
        {
            if (e.Content is PageData || e.Content is BlockData)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                var searchService = ServiceLocator.Current.GetInstance<ISearchIndex>();

                // re-index affected pages
                var links = ServiceLocator.Current.GetInstance<ContentSoftLinkRepository>();
                var references = links.Load(e.Content.ContentLink, true)
                                      .Where(link => link.SoftLinkType == ReferenceType.PageLinkReference &&
                                                     !ContentReference.IsNullOrEmpty(link.OwnerContentLink))
                                      .Select(link => link.OwnerContentLink)
                                      .ToList();

                foreach (var reference in references)
                {
                    var affectedPage = contentLoader.Get<IContent>(reference) as PageData;
                    if (affectedPage != null)
                    {
                        searchService.IndexPage(affectedPage);
                    }
                }

                // re-index published page
                var page = e.Content as PageData;
                if (page != null)
                {
                    searchService.IndexPage(page);
                }

                searchService.Commit();
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            if (_contentEvents == null)
            {
                _contentEvents = ServiceLocator.Current.GetInstance<IContentEvents>();
            }

            _contentEvents.PublishedContent -= contentEvents_PublishedContent;
        }
    }
}