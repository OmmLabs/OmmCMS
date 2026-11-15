using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Seo;

namespace OmmCMS.Core.Catalog.Products;

public static partial class ProductBundleItemExtensions
{
    /// <summary>
    /// Gets a value indicating whether an attribute value is filtered out.
    /// </summary>
    /// <param name="bundleItem">Bundle item.</param>
    /// <param name="value">Variant attribute value.</param>
    /// <param name="filter">Matched filter.</param>
    /// <returns>A value indicating whether an attribute value is filtered out.</returns>
    public static bool IsFilteredOut(this ProductBundleItem bundleItem, ProductVariantAttributeValue value, out ProductBundleItemAttributeFilter filter)
    {
        if (bundleItem != null && value != null && bundleItem.FilterAttributes)
        {
            filter = bundleItem.AttributeFilters.FirstOrDefault(x => x.AttributeId == value.ProductVariantAttributeId && x.AttributeValueId == value.Id);
            return filter == null;
        }

        filter = null;
        return false;
    }

    /// <summary>
    /// Gets the localized name of a bundle item. Falls back to the localized product name if empty.
    /// </summary>
    /// <param name="bundleItem">Bundle item.</param>
    /// <returns>The localized name of the bundle item.</returns>
    public static string GetLocalizedName(this ProductBundleItem bundleItem)
    {
        if (bundleItem != null)
        {
            string name = bundleItem.GetLocalized(x => x.Name);
            return name.HasValue() ? name : bundleItem.Product.GetLocalized(x => x.Name);
        }

        return null;
    }
}
