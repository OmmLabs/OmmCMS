using Microsoft.AspNetCore.Razor.TagHelpers;
using OmmCMS.Web.TagHelpers.Shared;

namespace OmmCMS.Web.TagHelpers;

public static class TagHelperContextExtensions
{
    public static bool ShouldSuppressChildContent(this TagHelperContext context)
        => context.Items.TryGetValue(IfTagHelper.SuppressChildContentKey, out var value)
            && value is true;
}
