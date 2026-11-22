using System.IO;
using System.Xml;
using NUnit.Framework;

namespace OmmCMS.Web.Tests.Administration;

[TestFixture]
public class OmmCmsAdminSitemapTests
{
    private static XmlDocument LoadCmsSitemap()
    {
        // Find the solution root regardless of the clone folder name (OmmCMS, OmmCMS, etc.).
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "OmmCMS.sln")))
        {
            root = root.Parent;
        }

        Assert.That(root, Is.Not.Null, "The test must run from a solution checkout.");
        var xml = new XmlDocument();
        xml.Load(Path.Combine(root!.FullName, "src", "OmmCMS.Web", "Areas", "Admin", "ommcms-sitemap.xml"));
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
        Assert.That(root.SelectSingleNode("siteMapNode[@id='cms']/siteMapNode[@id='newsletter-subscribers']"), Is.Not.Null);
        Assert.That(root.SelectSingleNode("siteMapNode[@id='system']/siteMapNode[@id='system-info']"), Is.Not.Null);
        Assert.That(root.SelectSingleNode("siteMapNode[@id='system']/siteMapNode[@id='system-warnings']"), Is.Not.Null);
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
        var project = File.ReadAllText(Path.Combine(root!.FullName, "src", "OmmCMS.Web", "OmmCMS.Web.csproj"));
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

    [Test]
    public void AdminSidebarBrandAndCmsViewsOmitRetiredControls()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "OmmCMS.sln")))
        {
            root = root.Parent;
        }

        Assert.That(root, Is.Not.Null);
        var webRoot = Path.Combine(root!.FullName, "src", "OmmCMS.Web");

        var navbar = File.ReadAllText(Path.Combine(webRoot, "Areas", "Admin", "Views", "Shared", "Components", "AdminNavbar", "Default.cshtml"));
        Assert.That(navbar, Does.Contain("aria-label=\"OmmCMS\""));
        Assert.That(navbar, Does.Contain("<svg"));
        Assert.That(navbar, Does.Contain("navbar-brand-wordmark"));

        var layoutScss = File.ReadAllText(Path.Combine(webRoot, "wwwroot", "admin", "_layout.scss"));
        Assert.That(layoutScss, Does.Contain("--sidebar-brand-symbol-size"));
        Assert.That(layoutScss, Does.Not.Contain("--sidebar-brand-width"));

        var generalCommon = File.ReadAllText(Path.Combine(webRoot, "Areas", "Admin", "Views", "Setting", "GeneralCommon.cshtml"));
        Assert.That(generalCommon, Does.Not.Contain("XmlSitemapIncludesNews"));
        Assert.That(generalCommon, Does.Not.Contain("XmlSitemapIncludesForum"));

        var maintenance = File.ReadAllText(Path.Combine(webRoot, "Areas", "Admin", "Views", "Maintenance", "Index.cshtml"));
        Assert.That(maintenance, Does.Not.Contain("delete-export-files"));
        Assert.That(maintenance, Does.Not.Contain("migrate-icons"));
    }
}
