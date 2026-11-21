using System.Linq;
using NUnit.Framework;
using OmmCMS.Core.Content.Blogs;
using OmmCMS.Core.Content.Media;

namespace OmmCMS.Core.Tests.Content.Media;

[TestFixture]
public class MediaPathTests
{
    private static MediaService CreateMediaService()
        => new(null, null, null, null, null, null, null, null, null, null, null, null, null, () => null, null);

    [Test]
    public void CombinePaths_handles_null_and_empty_segments()
    {
        var service = CreateMediaService();

        Assert.That(service.CombinePaths(null), Is.EqualTo(string.Empty));
        Assert.That(service.CombinePaths(), Is.EqualTo(string.Empty));
        Assert.That(service.CombinePaths(null, null), Is.EqualTo(string.Empty));
        Assert.That(service.CombinePaths(null, "image.png"), Is.EqualTo("image.png"));
        Assert.That(service.CombinePaths("", "image.png"), Is.EqualTo("image.png"));
        Assert.That(service.CombinePaths("content", null, "image.png"), Is.EqualTo("content/image.png"));
        Assert.That(service.CombinePaths("content", "/sub/image.png"), Is.EqualTo("content/sub/image.png"));
        Assert.That(service.CombinePaths(@"content\sub\", @"\image.png"), Is.EqualTo("content/sub/image.png"));
    }

    [TestCase(null, true, "")]
    [TestCase("", true, "")]
    [TestCase("/Content/Sub/", true, "content/sub")]
    [TestCase(@"\Content\Sub\", false, "Content/Sub")]
    public void NormalizePath_handles_empty_and_slashes(string input, bool forQuery, string expected)
    {
        Assert.That(FolderService.NormalizePath(input, forQuery), Is.EqualTo(expected));
    }

    [Test]
    public void SystemAlbumProvider_registers_blog_image_tracks_in_content_album()
    {
        var provider = new SystemAlbumProvider(null);
        var table = new TrackedMediaPropertyTable(SystemAlbumProvider.Content);

        provider.ConfigureTracks(SystemAlbumProvider.Content, table);
        var props = table.GetProperties();

        Assert.That(props.Any(x => x.EntityType == typeof(BlogPost) && x.Name == nameof(BlogPost.PreviewImageId)), Is.True);
        Assert.That(props.Any(x => x.EntityType == typeof(BlogPost) && x.Name == nameof(BlogPost.CoverImageId)), Is.True);
    }
}
