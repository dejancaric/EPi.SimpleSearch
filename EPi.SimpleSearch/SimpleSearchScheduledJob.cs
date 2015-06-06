using System;
using EPiServer;
using EPiServer.BaseLibrary.Scheduling;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;

namespace DC.EPi.SimpleSearch
{
    [ScheduledPlugIn(DisplayName = "SimpleSearch Scheduled Job")]
    public class SimpleSearchScheduledJob : JobBase
    {
        private readonly ISearchService _search;
        private readonly IContentRepository _contentRepository;

        private bool _stopSignaled;
       

        public SimpleSearchScheduledJob()
        {
            IsStoppable = true;

            _search = ServiceLocator.Current.GetInstance<ISearchService>();
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        }

        /// <summary>
        /// Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log
        /// and visible from admin mode</returns>
        public override string Execute()
        {
            //Call OnStatusChanged to periodically notify progress of job for manually started jobs
            OnStatusChanged(String.Format("Starting execution of {0}", GetType()));

            try
            {
                // delete all documents from index
                _search.DeleteAll();

                // index start page
                var startPage = _contentRepository.Get<PageData>(ContentReference.StartPage);
                _search.IndexPage(startPage);

                // index all child pages
                IndexChildrenRecursively(startPage);

                //For long running jobs periodically check if stop is signalled and if so stop execution
                if (_stopSignaled)
                {
                    _search.Rollback();
                    return "Stop of job was called";
                }

                _search.Commit();
                return string.Format("Successfully indexed {0} documents.", _search.GetNumberOfDocuments());
            }
            catch
            {
                _search.Rollback();
                throw;
            }
        }

        private void IndexChildrenRecursively(PageData page)
        {
            if (_stopSignaled)
            {
                return;
            }

            var children = _contentRepository.GetChildren<PageData>(page.PageLink);
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (_stopSignaled)
                    {
                        return;
                    }
                    _search.IndexPage(child);
                    IndexChildrenRecursively(child);
                }
            }
        }

        /// <summary>
        /// Called when a user clicks on Stop for a manually started job,
        /// or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}