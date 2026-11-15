using Microsoft.AspNetCore.Http;
using OmmCMS.Core.Seo.Routing;

namespace OmmCMS.Core.Seo;

/// <summary>
/// Responsible for applying URL rules to current request url (HTTPS, canonical host name, culture code etc.)
/// </summary>
public interface IUrlFilter
{
    void Apply(UrlPolicy policy, HttpContext httpContext);
}