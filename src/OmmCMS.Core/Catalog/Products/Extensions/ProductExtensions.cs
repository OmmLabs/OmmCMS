using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Catalog.Products;

public static partial class ProductExtensions
{
    /// <summary>
    /// Applies data of an attribute combination to the product.
    /// </summary>
    /// <param name="product">Target product entity.</param>
    /// <param name="combination">Source attribute combination.</param>
    public static void MergeWithCombination(this Product product, ProductVariantAttributeCombination combination)
    {
        Guard.NotNull(product);

        var values = product.MergedDataValues;

        values?.Clear();

        if (combination == null)
            return;

        if (values == null)
            product.MergedDataValues = values = [];

        if (combination.Sku.HasValue())
            values.Add(nameof(Product.Sku), combination.Sku);
        if (combination.Gtin.HasValue())
            values.Add(nameof(Product.Gtin), combination.Gtin);
        if (combination.ManufacturerPartNumber.HasValue())
            values.Add(nameof(Product.ManufacturerPartNumber), combination.ManufacturerPartNumber);

        if (combination.Price.HasValue)
            values.Add(nameof(Product.Price), combination.Price.Value);

        if (combination.DeliveryTimeId.HasValue && combination.DeliveryTimeId.Value > 0)
            values.Add(nameof(Product.DeliveryTimeId), combination.DeliveryTimeId);

        if (combination.QuantityUnitId.HasValue && combination.QuantityUnitId.Value > 0)
            values.Add(nameof(Product.QuantityUnitId), combination.QuantityUnitId);

        if (combination.Weight.HasValue)
            values.Add(nameof(Product.Weight), combination.Weight.Value);
        if (combination.Length.HasValue)
            values.Add(nameof(Product.Length), combination.Length.Value);
        if (combination.Width.HasValue)
            values.Add(nameof(Product.Width), combination.Width.Value);
        if (combination.Height.HasValue)
            values.Add(nameof(Product.Height), combination.Height.Value);

    }

    /// <summary>
    /// Gets a value indicating whether the product is labeled as NEW.
    /// </summary>
    /// <param name="product">Product entity.</param>
    /// <param name="catalogSettings">Catalog settings.</param>
    /// <returns>A alue indicating whether the product is labeled as NEW.</returns>
    public static bool IsNew(this Product product, CatalogSettings catalogSettings)
    {
        if (catalogSettings.LabelAsNewForMaxDays != null)
        {
            var dt = catalogSettings.LabelAsNewByAvailableDate ? product.AvailableStartDateTimeUtc : product.CreatedOnUtc;
            if (dt != null)
            {
                return (DateTime.UtcNow - dt.Value).Days <= catalogSettings.LabelAsNewForMaxDays.Value;
            }
        }

        return false;
    }

    public static string GetProductTypeLabel(this Product product, ILocalizationService localizationService)
    {
        if (product != null && product.ProductTypeId > (int)ProductType.SimpleProduct)
        {
            var key = "Admin.Catalog.Products.ProductType.{0}.Label".FormatInvariant(product.ProductType.ToStringInvariant());
            return localizationService.GetResource(key);
        }

        return string.Empty;
    }

    public static bool CanBeBundleItem(this Product product)
    {
        return product != null && product.ProductType == ProductType.SimpleProduct && !product.IsDownload;
    }
}