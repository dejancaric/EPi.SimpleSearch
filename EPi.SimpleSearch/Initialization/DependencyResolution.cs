using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace DC.EPi.SimpleSearch.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class DependencyResolution : IConfigurableModule
    {
        private IContentEvents _contentEvents;

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            var indexPath = HostingEnvironment.MapPath("~/App_Data/custom_search/");
            context.Container.Configure(x =>
            {
                x.For<ISearchIndex>().Singleton()
                 .Use<SearchIndex>()
                 .Ctor<string>("filePath").Is(indexPath);

                x.For<IWebPageParser>().Singleton().Use<WebPageParser>();
            });
        }

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
                new Task(() => { ParsePage(e.Content); }).Start();
            }
        }

        private void ParsePage(IContent content)
        {
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var searchService = ServiceLocator.Current.GetInstance<ISearchIndex>();

            // re-index affected pages
            var links = ServiceLocator.Current.GetInstance<ContentSoftLinkRepository>();
            var references = links.Load(content.ContentLink, true)
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
            var page = content as PageData;
            if (page != null)
            {
                searchService.IndexPage(page);
            }

            searchService.Commit();
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