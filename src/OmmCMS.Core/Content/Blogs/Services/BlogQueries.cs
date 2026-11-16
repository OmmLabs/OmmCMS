using System.Globalization;
using System.Text.RegularExpressions;
using OmmCMS.Utilities.Html;

namespace OmmCMS.Core.Content.Blogs;

public static class BlogQueries
{
    public static IQueryable<BlogPost> Visible(this IQueryable<BlogPost> posts, DateTime utcNow, int storeId, int languageId)
        => posts.Where(x => x.IsPublished && x.PublishedOnUtc <= utcNow
            && (x.ExpiresOnUtc == null || x.ExpiresOnUtc > utcNow)
            && (x.StoreId == 0 || x.StoreId == storeId)
            && (x.LanguageId == 0 || x.LanguageId == languageId));

    public static string NormalizeSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var text = string.Concat(value.Trim().Normalize(NormalizationForm.FormD)
            .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
        text = Regex.Replace(text, @"[^a-z0-9]+", "-").Trim('-');
        return text.Length > 180 ? text[..180].TrimEnd('-') : text;
    }

    public static string SanitizeBody(string html)
        => HtmlUtility.SanitizeHtml(html ?? "") ?? "";
}
