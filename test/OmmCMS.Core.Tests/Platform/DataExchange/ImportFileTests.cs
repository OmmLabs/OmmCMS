using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OmmCMS.Core.DataExchange;
using OmmCMS.Core.DataExchange.Import;
using OmmCMS.IO;

namespace OmmCMS.Core.Tests.DataExchange;

[TestFixture]
public class ImportFileTests
{
    [TestCase("Customers", false)]
    [TestCase("NewsletterSubscription", false)]
    [TestCase("ProductVariantAttributeValue-backup", false)]
    [TestCase("legacy-ProductVariantAttributeValue", true)]
    [TestCase("legacy-productvariantattributecombination", true)]
    [TestCase("legacy-TIERPRICE", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void OnlyRetiredSidecarSuffixesAreExcluded(string name, bool expected)
    {
        Assert.That(ImportFile.IsRetiredSidecar(name), Is.EqualTo(expected));
    }

    [Test]
    public async Task ImportDirectoryReturnsOnlyPrimaryFilesInNameOrder()
    {
        var names = new[] { "z-customers.xlsx", "legacy-TierPrice.csv",
            "legacy-ProductVariantAttributeValue.csv", "A-customers.csv",
            "legacy-ProductVariantAttributeCombination.xlsx" };
        var files = names.Select(name =>
        {
            var file = new Mock<IFile>();
            file.SetupGet(x => x.Name).Returns(name);
            file.SetupGet(x => x.NameWithoutExtension).Returns(Path.GetFileNameWithoutExtension(name));
            return file.Object;
        }).ToArray();
        var directory = new Mock<IDirectory>();
        directory.SetupGet(x => x.Exists).Returns(true);
        directory.Setup(x => x.EnumerateFiles("*", false)).Returns(files);
        var service = new Mock<ImportProfileService>(null, null, null, null, null, null) { CallBase = true };
        var profile = new ImportProfile();
        service.Setup(x => x.GetImportDirectoryAsync(profile, "Content", false)).ReturnsAsync(directory.Object);

        var result = await service.Object.GetImportFilesAsync(profile);

        Assert.That(result.Select(x => x.File.Name), Is.EqualTo(new[] { "A-customers.csv", "z-customers.xlsx" }));
    }
}
