#nullable enable

using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Checkout.Tax;
using OmmCMS.Core.Common;

namespace OmmCMS.Core.Catalog.Pricing;

/// <summary>
/// Represents the result of a price calculation process for a single product. All monetary amounts
/// are in the target currency and have been exchanged and converted according to input options.
/// </summary>
public class CalculatedPrice : ICloneable<CalculatedPrice>
{
    public CalculatedPrice(CalculatorContext context)
    {
        Guard.NotNull(context);

        Product = context.Product;
        PricingType = context.PricingType;
        HasPriceRange = context.HasPriceRange;
        AttributePriceAdjustments = context.AttributePriceAdjustments;
    }

    /// <summary>
    /// For testing or skipped calculations.
    /// </summary>
    public CalculatedPrice(Product product, PricingType pricingType = PricingType.Calculated)
    {
        Guard.NotNull(product);

        Product = product;
        PricingType = pricingType;
        AttributePriceAdjustments = new List<CalculatedPriceAdjustment>();
    }

    /// <summary>
    /// The product for which a price was calculated. Not necessarily the input product,
    /// can also be a child of a grouped product, if the lowest price should be calculated.
    /// In that case this property refers to the lowest price child product.
    /// </summary>
    public Product Product { get; init; }

    public PricingType PricingType { get; }

    /// <summary>
    /// The final unit price of the input <see cref="Product"/>.
    /// </summary>
    public Money FinalPrice { get; set; }

    /// <summary>
    /// The regular unit price of the input <see cref="Product"/> in the target currency, 
    /// usually <see cref="Product.Price"/> or <see cref="Product.ComparePrice"/>.
    /// </summary>
    public Money? RegularPrice { get; set; }

    /// <summary>
    /// The regular price label.
    /// </summary>
    public PriceLabel? RegularPriceLabel { get; set; }

    /// <summary>
    /// The retail unit price (MSRP) of the input <see cref="Product"/>.
    /// A retail price is given if <see cref="Product.ComparePrice"/> is not 0
    /// and <see cref="Product.ComparePriceLabelId"/> referes to an MSRP label.
    /// </summary>
    public Money? RetailPrice { get; set; }

    /// <summary>
    /// The retail price label if the <see cref="Product.ComparePrice"/> is not 0
    /// and <see cref="Product.ComparePriceLabelId"/> referes to an MSRP label.
    /// </summary>
    public PriceLabel? RetailPriceLabel { get; set; }

    /// <summary>
    /// A value indicating whether the price has a range, which is mostly the case if the lowest price
    /// was determined.
    /// </summary>
    public bool HasPriceRange { get; set; }

    /// <summary>
    /// The price that is initially displayed on the product detail page, if any.
    /// Includes price adjustments of preselected attributes and prices of attribute combinations.
    /// </summary>
    public Money? PreselectedPrice { get; set; }

    /// <summary>
    /// The lowest possible price of a product, if any.
    /// Includes prices of attribute combinations. Ignores price adjustments of attributes.
    /// </summary>
    public Money? LowestPrice { get; set; }

    /// <summary>
    /// Gets a list of calculated attribute price adjustments, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
    /// Only filled if <see cref="PriceCalculationOptions.DeterminePriceAdjustments"/> is activated.
    /// </summary>
    public ICollection<CalculatedPriceAdjustment> AttributePriceAdjustments { get; init; }

    /// <summary>
    /// Tax for <see cref="FinalPrice"/>.
    /// </summary>
    public Tax? Tax { get; set; }

    /// <summary>
    /// Gets or sets a price saving in relation to <see cref="FinalPrice"/>.
    /// The saving results from the applied discounts, if any, otherwise from the difference to the <see cref="Product.ComparePrice"/>.
    /// </summary>
    public PriceSaving Saving { get; set; }

    /// <inheritdoc/>
    public CalculatedPrice Clone()
        => (CalculatedPrice)MemberwiseClone();

    /// <inheritdoc/>
    object ICloneable.Clone()
        => MemberwiseClone();
}