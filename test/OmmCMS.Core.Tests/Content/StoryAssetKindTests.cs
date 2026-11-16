using System;
using OmmCMS.Core.Content.Blocks.Domain;
using NUnit.Framework;

namespace OmmCMS.Core.Tests.Content;

[TestFixture]
public class StoryAssetKindTests
{
    [Test]
    public void Only_Cms_Asset_Kinds_Remain_With_Stable_Identifiers()
    {
        Assert.That(Enum.GetNames<StoryAssetKind>(), Is.EqualTo(new[] { "Picture", "Video", "Link", "Audio" }));
        Assert.That((int)StoryAssetKind.Picture, Is.EqualTo(0));
        Assert.That((int)StoryAssetKind.Video, Is.EqualTo(1));
        Assert.That((int)StoryAssetKind.Link, Is.EqualTo(5));
        Assert.That((int)StoryAssetKind.Audio, Is.EqualTo(6));
    }
}
