using System.IO;
using System.Xml;
using NUnit.Framework;

namespace Smartstore.Web.Tests.Administration;

[TestFixture]
public class OmmCmsAdminSitemapTests
{
    private static XmlDocument LoadCmsSitemap()
    {
        // Find the solution root regardless of the clone folder name (OmmCMS, Smartstore, etc.).
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "OmmCMS.sln")))
        {
            root = root.Parent;
        }

        Assert.That(root, Is.Not.Null, "The test must run from a solution checkout.");
        var xml = new XmlDocument();
        xml.Load(Path.Combine(root!.FullName, "src", "Smartstore.Web", "Areas", "Admin", "ommcms-sitemap.xml"));
        return xml;
    }

    [Test]
    public void ContainsCoreContentAndOperationsNavigation()
    {
        var xml = LoadCmsSitemap();
        var root = xml.SelectSingleNode("/siteMap/siteMapNode") as XmlElement;

        Assert.That(root, Is.Not.Null);
        Assert.That(root!.GetAttribute("id"), Is.EqualTo("home"));

        foreach (var id in new[] { "cms", "design", "users", "configuration", "system", "modules" })
        {
            Assert.That(root.SelectSingleNode($"siteMapNode[@id='{id}']"), Is.Not.Null, $"Missing {id} section.");
        }

        Assert.That(root.SelectSingleNode("siteMapNode[@id='cms']/siteMapNode[@id='topics']"), Is.Not.Null);
        Assert.That(root.SelectSingleNode("siteMapNode[@id='cms']/siteMapNode[@id='menus']"), Is.Not.Null);
        Assert.That(root.SelectSingleNode("siteMapNode[@id='modules']/siteMapNode[@id='modules-manage']"), Is.Not.Null);
    }

    [Test]
    public void DoesNotAdvertiseCommerceNavigation()
    {
        var xml = LoadCmsSitemap();
        foreach (var id in new[] { "catalog", "sales", "promotions", "orders", "payment-methods", "shopping-carts", "discounts", "shipments" })
        {
            Assert.That(xml.SelectSingleNode($"//siteMapNode[@id='{id}']"), Is.Null, $"Commerce node {id} should not appear.");
        }
    }

    [Test]
    public void CmsSitemapIsIncludedInPublishOutput()
    {
        var xml = LoadCmsSitemap();
        Assert.That(xml.DocumentElement, Is.Not.Null);

        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "OmmCMS.sln")))
        {
            root = root.Parent;
        }

        Assert.That(root, Is.Not.Null);
        var project = File.ReadAllText(Path.Combine(root!.FullName, "src", "Smartstore.Web", "Smartstore.Web.csproj"));
        Assert.That(project, Does.Contain("Areas\\Admin\\ommcms-sitemap.xml"), "The CMS sitemap must be published with the web application.");
    }

    [Test]
    public void EveryLinkedMenuItemRetainsItsPermissionAndRoute()
    {
        var xml = LoadCmsSitemap();
        var nodes = xml.SelectNodes("//siteMapNode[@controller][@action]");
        Assert.That(nodes, Is.Not.Null);
        Assert.That(nodes!.Count, Is.GreaterThan(10));
        foreach (XmlElement node in nodes)
        {
            if (node.GetAttribute("id") == "home")
            {
                continue;
            }

            Assert.That(node.GetAttribute("area"), Is.EqualTo("Admin"), $"Unexpected area for {node.GetAttribute("id")}.");
            Assert.That(node.GetAttribute("permissionNames"), Is.Not.Empty, $"Missing permission guard for {node.GetAttribute("id")}.");
        }
    }
}
