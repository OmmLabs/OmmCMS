using System;
using NUnit.Framework;
using OmmCMS.Core.Content.Blogs;

namespace OmmCMS.Core.Tests.Content.Blogs;

[TestFixture]
public class BlogSlugPathsTests
{
    [TestCase(nameof(BlogPost), "blog/a-post")]
    [TestCase(nameof(BlogCategory), "blog/category/a-post")]
    [TestCase(nameof(BlogTag), "blog/tag/a-post")]
    public void Full_paths_preserve_namespaces(string entityName, string path)
    {
        Assert.That(BlogSlugPaths.GetPath(entityName, "a-post"), Is.EqualTo(path));
        Assert.That(BlogSlugPaths.GetSlug(entityName, path), Is.EqualTo("a-post"));
    }

    [Test]
    public void Invalid_or_foreign_paths_are_rejected()
    {
        Assert.That(BlogSlugPaths.GetSlug(nameof(BlogPost), "blog/category/example"), Is.Null);
        Assert.That(BlogSlugPaths.GetSlug(nameof(BlogTag), "blog/example"), Is.Null);
        Assert.That(BlogSlugPaths.IsManaged("Topic"), Is.False);
        Assert.Throws<ArgumentException>(() => BlogSlugPaths.GetPath(nameof(BlogPost), "../example"));
    }
}
