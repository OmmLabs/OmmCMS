using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmmCMS.Core.Identity;

namespace OmmCMS.Core.Catalog.Products;

internal class ProductReviewMap : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasOne(c => c.Product)
            .WithMany(c => c.ProductReviews)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Represents a product review.
/// </summary>
[Table("ProductReview")]
public partial class ProductReview : CustomerContent
{
    /// <summary>
    /// Gets or sets the product identifier.
    /// </summary>
    public int ProductId { get; set; }

    private Product _product;
    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public Product Product
    {
        get => _product ?? LazyLoader.Load(this, ref _product);
        set => _product = value;
    }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [StringLength(4000)]
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the review text.
    /// </summary>
    [MaxLength]
    public string ReviewText { get; set; }

    /// <summary>
    /// Gets or sets the review rating.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets a flag that defines whether the reviewed product was purchased by the customer.
    /// </summary>
    public bool? IsVerifiedPurchase { get; set; }
}
