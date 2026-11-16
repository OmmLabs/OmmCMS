using Autofac;
using Microsoft.AspNetCore.Mvc;
using OmmCMS.Core.Logging;

namespace OmmCMS.Core.Search;

public abstract partial class SearchServiceBase
{
    /// <summary>
    /// Notifies the admin that indexing is required to use the advanced search.
    /// </summary>
    protected virtual void IndexingRequiredNotification(ICommonServices services, string scope = null)
    {
        if (services.WorkContext.CurrentCustomer.IsAdmin() && services.Container.TryResolve<IUrlHelper>(out var urlHelper))
        {
            var indexingUrl = urlHelper.Action("Indexing", "MegaSearch", new { area = "Admin", scope });
            var configureUrl = urlHelper.Action("ConfigureModule", "Module", new { area = "Admin", systemName = "OmmCMS.MegaSearch" });
            var notification = services.Localization.GetResource("Search.IndexingRequiredNotification").FormatInvariant(indexingUrl, configureUrl);

            services.Notifier.Information(notification);
        }
    }
}
