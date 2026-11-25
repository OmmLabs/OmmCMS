using System;
using System.Linq;
using NUnit.Framework;
using OmmCMS.Core.Content.Blogs;

namespace OmmCMS.Core.Tests.Content.Blogs;

[TestFixture]
public class BlogQueriesTests
{
    [TestCase("Hello world!", "hello-world")]
    [TestCase("  Café & Tea  ", "cafe-tea")]
    [TestCase("///", "")]
    public void Slug_is_normalized(string value, string expected)
        => Assert.That(BlogQueries.NormalizeSlug(value), Is.EqualTo(expected));

    [Test]
    public void Only_visible_posts_are_returned()
    {
        var now = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
        var posts = new[]
        {
            new BlogPost { Id = 1, IsPublished = true, PublishedOnUtc = now.AddDays(-1) },
            new BlogPost { Id = 2, IsPublished = false, PublishedOnUtc = now.AddDays(-1) },
            new BlogPost { Id = 3, IsPublished = true, PublishedOnUtc = now.AddDays(1) },
            new BlogPost { Id = 4, IsPublished = true, PublishedOnUtc = now.AddDays(-1), ExpiresOnUtc = now },
            new BlogPost { Id = 5, IsPublished = true, PublishedOnUtc = now.AddDays(-1), StoreId = 8 },
            new BlogPost { Id = 6, IsPublished = true, PublishedOnUtc = now.AddDays(-1), LanguageId = 9 },
            new BlogPost { Id = 7, IsPublished = true, PublishedOnUtc = now.AddDays(-1), StoreId = 1, LanguageId = 2 }
        }.AsQueryable();
        Assert.That(posts.Visible(now, 1, 2).Select(x => x.Id), Is.EquivalentTo(new[] { 1, 7 }));
    }

    [Test]
    public void Web_source_links_survive_sanitization_but_unsafe_links_do_not()
    {
        var input = "<p><a href='https://example.org/source'>Source</a>"
            + "<a href='http://example.org/reference'>Reference</a>"
            + "<a href='/docs/start-here'>Internal</a>"
            + "<a href='javascript:alert(1)'>Unsafe</a>"
            + "<a href='data:text/html,unsafe'>Data</a>"
            + "<a href='vbscript:unsafe'>Script</a></p>";
        var html = BlogQueries.SanitizeBody(BlogQueries.SanitizeBody(input));
        Assert.That(html, Does.Contain("href=\"https://example.org/source\"")
            .And.Contain("href=\"http://example.org/reference\"")
            .And.Contain("href=\"/docs/start-here\"")
            .And.Not.Contain("javascript:")
            .And.Not.Contain("data:text")
            .And.Not.Contain("vbscript:"));
    }

    [Test]
    public void Rich_text_is_sanitized()
    {
        var html = BlogQueries.SanitizeBody("<p>Article</p><script>alert(1)</script><img src='/photo.jpg' onerror='alert(1)'>");
        Assert.That(html, Does.Contain("<p>Article</p>").And.Not.Contain("<script").And.Not.Contain("onerror"));
    }
}
