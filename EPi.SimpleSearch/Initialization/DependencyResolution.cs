using System.Web.Hosting;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace DC.EPi.SimpleSearch.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class DependencyResolution : IConfigurableModule
    {
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
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}