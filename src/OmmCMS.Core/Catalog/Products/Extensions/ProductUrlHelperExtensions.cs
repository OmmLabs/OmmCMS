using System.Runtime.CompilerServices;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Seo;

namespace OmmCMS.Core.Catalog.Products;

/// <summary>
/// Extension methods for product URL helper.
/// </summary>
public static partial class ProductUrlHelperExtensions
{
    /// <summary>
    /// Creates a product URL including variant query string.
    /// </summary>
    /// <param name="helper">Product URL helper.</param>
    /// <param name="productId">Product identifier.</param>
    /// <param name="productSlug">Product URL slug.</param>
    /// <param name="bundleItemId">Bundle item identifier. 0 if it's not a bundle item.</param>
    /// <param name="variantValues">Variant values</param>
    /// <returns>Product URL</returns>
    public static string GetProductUrl(
        this ProductUrlHelper helper,
        int productId,
        string productSlug,
        int bundleItemId,
        params ProductVariantAttributeValue[] variantValues)
    {
        Guard.NotNull(helper);
        Guard.NotZero(productId);

        var query = new ProductVariantQuery();

        foreach (var value in variantValues)
        {
            var attribute = value.ProductVariantAttribute;

            query.AddVariant(new()
            {
                Value = value.Id.ToString(),
                ProductId = productId,
                BundleItemId = bundleItemId,
                AttributeId = attribute.ProductAttributeId,
                VariantAttributeId = attribute.Id,
                Alias = attribute.ProductAttribute.Alias,
                ValueAlias = value.Alias
            });
        }

        return helper.GetProductPath(productSlug, query);
    }

    /// <summary>
    /// Creates a product URL including variant query string.
    /// </summary>
    /// <param name="helper">Product URL helper</param>
    /// <param name="product">Product entity</param>
    /// <param name="variantValues">Variant values</param>
    /// <returns>Product URL</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetProductUrlAsync(this ProductUrlHelper helper, Product product, params ProductVariantAttributeValue[] variantValues)
    {
        Guard.NotNull(helper);
        Guard.NotNull(product);

        if (product.IsSystemProduct)
        {
            return null;
        }

        return helper.GetProductUrl(product.Id, await product.GetActiveSlugAsync(), 0, variantValues);
    }

}
