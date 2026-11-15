using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OmmCMS.Core.Catalog.Attributes;

[ValidateNever]
public class ProductVariantQuery
{
    private readonly List<ProductVariantQueryItem> _variants = new();
    public int VariantCombinationId { get; set; }

    public IReadOnlyList<ProductVariantQueryItem> Variants => _variants;

    public void AddVariant(ProductVariantQueryItem item)
    {
        var exists = _variants.Any(x =>
            x.ProductId == item.ProductId &&
            x.BundleItemId == item.BundleItemId &&
            x.AttributeId == item.AttributeId &&
            x.VariantAttributeId == item.VariantAttributeId &&
            x.Value == item.Value
        );

        if (!exists)
        {
            _variants.Add(item);
        }
    }

    public override string ToString()
    {
        return string.Join('&', Variants.Select(x => x.ToString()).Where(x => x.HasValue()));
    }
}
