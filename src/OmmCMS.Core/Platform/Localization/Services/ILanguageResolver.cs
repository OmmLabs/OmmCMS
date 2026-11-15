using Microsoft.AspNetCore.Http;
using OmmCMS.Core.Identity;

namespace OmmCMS.Core.Localization;

/// <summary>
/// Responsible for resolving the current customer's working language.
/// </summary>
public interface ILanguageResolver
{
    Language ResolveLanguage(Customer currentCustomer, HttpContext httpContext);
    Task<Language> ResolveLanguageAsync(Customer currentCustomer, HttpContext httpContext);
}