using System.Security.Principal;
using EPiServer.Core;
using EPiServer.Filters;
using EPiServer.Security;

namespace DC.EPi.SimpleSearch.Filters
{
    public class FilterPublicAccess : PageFilterBase
    {
        private readonly IPrincipal _principal;

        public FilterPublicAccess()
        {
            _principal = new GenericPrincipal(new GenericIdentity("visitor"), new[] { "Everyone" });
        }

        public override bool ShouldFilter(PageData page)
        {
            return !page.ACL.QueryDistinctAccess(_principal, AccessLevel.Read);
        }

        public override bool ShouldFilter(IContent content)
        {
            var page = content as PageData;
            if (page == null)
            {
                return false;
            }

            return ShouldFilter(page);
        }
    }
}