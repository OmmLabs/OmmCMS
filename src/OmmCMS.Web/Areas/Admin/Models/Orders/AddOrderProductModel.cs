using System.ComponentModel.DataAnnotations;
using FluentValidation;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Checkout.Orders;
using OmmCMS.Web.Rendering.Choices;

namespace OmmCMS.Admin.Models.Orders;

[LocalizedDisplay("Admin.Orders.Products.AddNew.")]
public partial class AddOrderProductModel : ModelBase
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }
    public ProductType ProductType { get; set; }
    public string Name { get; set; }

    [LocalizedDisplay("*UnitPriceInclTax")]
    public decimal UnitPriceInclTax { get; set; }
    [LocalizedDisplay("*UnitPriceExclTax")]
    public decimal UnitPriceExclTax { get; set; }

    [LocalizedDisplay("*SubTotalInclTax")]
    public decimal PriceInclTax { get; set; }
    [LocalizedDisplay("*SubTotalExclTax")]
    public decimal PriceExclTax { get; set; }

    [LocalizedDisplay("*TaxRate")]
    [AdditionalMetadata("min", 0)]
    public decimal TaxRate { get; set; }

    [AdditionalMetadata("min", 1)]
    [LocalizedDisplay("*Quantity")]
    public int Quantity { get; set; } = 1;

    [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.AdjustInventory")]
    public bool AdjustInventory { get; set; } = true;

    [LocalizedDisplay("Admin.Orders.OrderItem.AutoUpdate.UpdateTotals")]
    public bool UpdateTotals { get; set; }
    public bool ShowUpdateTotals { get; set; }

    public List<ProductVariantAttributeModel> ProductVariantAttributes { get; set; } = [];

    public partial class ProductVariantAttributeModel : ChoiceModel
    {
        public int ProductId { get; set; }
        public int BundleItemId { get; set; }
        public int ProductAttributeId { get; set; }

        public override string BuildControlId()
        {
            return ProductVariantQueryItem.CreateKey(ProductId, BundleItemId, ProductAttributeId, Id);
        }
    }

    public partial class ProductVariantAttributeValueModel : ChoiceItemModel
    {
        public override string GetItemLabel()
        {
            var label = QuantityInfo > 1
                ? $"{QuantityInfo} x {Name}"
                : Name;

            var priceAdjustment = GetPriceAdjustmentText();
            if (priceAdjustment.HasValue())
            {
                label += $" ({priceAdjustment})";
            }

            return label;
        }
    }

}

record class AddOrderProductData
{
    public Order Order { get; set; }
    public Product Product { get; set; }
    public ProductVariantAttributeSelection Selection { get; set; }
}

public partial class AddOrderProductModelValidator : SmartValidator<AddOrderProductModel>
{
    public AddOrderProductModelValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TaxRate).GreaterThanOrEqualTo(0);
    }
}
