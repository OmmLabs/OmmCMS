#nullable enable

using NUnit.Framework;
using OmmCMS.Json;

namespace OmmCMS.Tests.Json;

[TestFixture]
public class JsonLdBuilderTests
{
    [Test]
    public void Media_gallery_does_not_require_a_product_fragment()
    {
        var builder = new JsonLdBuilder();
        var gallery = builder.Fragment("ImageGallery");
        gallery.Arr("image", new[] { "https://example.test/image.jpg" });
        gallery.Arr("video", new[] { JsonLdFragment.Create("VideoObject").Prop("name", "Gallery video") });

        Assert.That(builder.TryGetFragment("Product", out _), Is.False);
        Assert.That(builder.Fragments, Has.Count.EqualTo(1));
        var data = gallery.AsJsonObject();
        Assert.That(data["@type"]!.GetValue<string>(), Is.EqualTo("ImageGallery"));
        Assert.That(data["image"]![0]!.GetValue<string>(), Is.EqualTo("https://example.test/image.jpg"));
        Assert.That(data["video"]![0]!["@type"]!.GetValue<string>(), Is.EqualTo("VideoObject"));
        Assert.That(builder.RenderScripts().ToString(), Does.Contain("fragment-type=\"ImageGallery\""));
    }
}
