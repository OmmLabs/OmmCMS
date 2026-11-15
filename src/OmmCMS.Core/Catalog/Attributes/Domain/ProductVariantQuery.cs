using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OmmCMS.Core.Checkout.Attributes;

namespace OmmCMS.Core.Catalog.Attributes;

[ValidateNever]
public class ProductVariantQuery
{
    private readonly List<ProductVariantQueryItem> _variants = new();
    private readonly List<CheckoutAttributeQueryItem> _checkoutAttributes = new();

    public int VariantCombinationId { get; set; }

    public IReadOnlyList<ProductVariantQueryItem> Variants => _variants;

    public IReadOnlyList<CheckoutAttributeQueryItem> CheckoutAttributes => _checkoutAttributes;

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

    public void AddCheckoutAttribute(CheckoutAttributeQueryItem item)
    {
        _checkoutAttributes.Add(item);
    }

    public override string ToString()
    {
        var groups = new string[]
        {
            string.Join('&', Variants.Select(x => x.ToString())),
            string.Join('&', CheckoutAttributes.Select(x => x.ToString()))
        };

        return string.Join('&', groups.Where(x => x.HasValue()));
    }
}
