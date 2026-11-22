using System;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Widgets;

namespace OmmCMS.Web.Tests.Administration;

[TestFixture]
public class OmmCmsThemesAndMetadataTests
{
    private static readonly string[] ExpectedThemes =
    [
        "Flex",
        "FlexAfterglow",
        "FlexBlack",
        "FlexBlue",
        "FlexLumen",
        "FlexPorcelain",
        "FlexRiso",
        "FlexSorbet",
        "FlexTidal"
    ];

    private static DirectoryInfo FindRepoRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "OmmCMS.sln")))
        {
            root = root.Parent;
        }

        Assert.That(root, Is.Not.Null, "The test must run from a solution checkout.");
        return root!;
    }

    [Test]
    public void AllNineBundledThemesHaveValidCmsConfigAndPreviewImages()
    {
        var themesRoot = Path.Combine(FindRepoRoot().FullName, "src", "OmmCMS.Web", "Themes");
        var actualThemes = Directory.GetDirectories(themesRoot)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.That(actualThemes, Is.EquivalentTo(ExpectedThemes));

        foreach (var theme in ExpectedThemes)
        {
            var themeDir = Path.Combine(themesRoot, theme);
            var configPath = Path.Combine(themeDir, "theme.config");
            var previewPath = Path.Combine(themeDir, "wwwroot", "images", "preview.png");

            Assert.That(File.Exists(configPath), Is.True, $"Missing theme.config for {theme}.");
            Assert.That(File.Exists(previewPath), Is.True, $"Missing preview.png for {theme}.");
            Assert.That(new FileInfo(previewPath).Length, Is.GreaterThan(1024), $"Preview image too small for {theme}.");

            var xml = new XmlDocument();
            xml.Load(configPath);
            var root = xml.DocumentElement;
            Assert.That(root, Is.Not.Null, $"Invalid XML root in {theme}/theme.config.");
            Assert.That(root!.GetAttribute("author"), Is.EqualTo("OmmLabs"), $"Unexpected author in {theme}/theme.config.");

            var description = root.GetAttribute("description");
            Assert.That(description, Is.Not.Empty, $"Missing description in {theme}/theme.config.");
            Assert.That(description.ToLowerInvariant(), Does.Not.Contain("product"), $"Storefront wording in {theme} description.");
            Assert.That(description.ToLowerInvariant(), Does.Not.Contain("storefront"), $"Storefront wording in {theme} description.");

            if (theme != "Flex")
            {
                Assert.That(root.GetAttribute("baseTheme"), Is.EqualTo("Flex"), $"{theme} should inherit Flex.");
            }

            var radiusVar = root.SelectSingleNode("Vars/Var[@name='dropdown-border-radius']") as XmlElement;
            if (radiusVar != null)
            {
                Assert.That(radiusVar.GetAttribute("type"), Is.EqualTo("String"), $"dropdown-border-radius in {theme} must be a String.");
            }
        }
    }

    [Test]
    public void LayoutUsesThemedHeaderNavigationAndFooterComponents()
    {
        var layoutPath = Path.Combine(FindRepoRoot().FullName, "src", "OmmCMS.Web", "Views", "Shared", "Layouts", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        Assert.That(layout, Does.Contain("Component.InvokeAsync(\"TopBar\")"));
        Assert.That(layout, Does.Contain("Component.InvokeAsync(\"ShopBar\")"));
        Assert.That(layout, Does.Contain("megamenu-container"));
        Assert.That(layout, Does.Contain("Component.InvokeAsync(\"Footer\")"));
    }

    [Test]
    public void CombineDocumentTitleDoesNotDuplicateMatchingSiteTitle()
    {
        Assert.That(
            PageAssetBuilder.CombineDocumentTitle("OmmCMS", "OmmCMS", ". ", PageTitleSeoAdjustment.PagenameAfterStorename),
            Is.EqualTo("OmmCMS"));
        Assert.That(
            PageAssetBuilder.CombineDocumentTitle("ommcms", "OmmCMS", ". ", PageTitleSeoAdjustment.StorenameAfterPagename),
            Is.EqualTo("ommcms"));
        Assert.That(
            PageAssetBuilder.CombineDocumentTitle(string.Empty, "OmmCMS", ". ", PageTitleSeoAdjustment.PagenameAfterStorename),
            Is.EqualTo("OmmCMS"));
        Assert.That(
            PageAssetBuilder.CombineDocumentTitle("Blog", "OmmCMS", ". ", PageTitleSeoAdjustment.PagenameAfterStorename),
            Is.EqualTo("OmmCMS. Blog"));
        Assert.That(
            PageAssetBuilder.CombineDocumentTitle("Blog", "OmmCMS", ". ", PageTitleSeoAdjustment.StorenameAfterPagename),
            Is.EqualTo("Blog. OmmCMS"));
    }
}
