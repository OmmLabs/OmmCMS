using OmmCMS.IO;

namespace OmmCMS.Core.DataExchange.Import;

public partial class ImportFile
{
    public ImportFile(IFile file)
    {
        Guard.NotNull(file, nameof(file));

        File = file;

    }

    public IFile File { get; private set; }

    /// <summary>
    /// Recognizes retired storefront sidecar files so they cannot be mistaken for primary CMS data.
    /// </summary>
    internal static bool IsRetiredSidecar(string fileNameWithoutExtension)
        => fileNameWithoutExtension != null
            && (fileNameWithoutExtension.EndsWith("ProductVariantAttributeValue", StringComparison.OrdinalIgnoreCase)
                || fileNameWithoutExtension.EndsWith("ProductVariantAttributeCombination", StringComparison.OrdinalIgnoreCase)
                || fileNameWithoutExtension.EndsWith("TierPrice", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// File label text.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets a value indicating whether the file has an CSV file extension.
    /// </summary>
    internal bool IsCsv => (new string[] { ".csv", ".txt", ".tab" }).Contains(File.Extension, StringComparer.OrdinalIgnoreCase);
}
