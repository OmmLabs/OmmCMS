using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmmCMS.Domain;
using OmmCMS.Core.Seo;

namespace OmmCMS.Core.Content.Blogs;

[Table("BlogPost")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(IsPublished), nameof(PublishedOnUtc))]
public class BlogPost : BaseEntity, ISlugSupported
{
    public string GetDisplayName() => Title;
    public string[] GetDisplayNameMemberNames() => [nameof(Title)];

    [Required, StringLength(250)]
    public string Title { get; set; }
    [Required, StringLength(180)]
    public string Slug { get; set; }
    [StringLength(1000)]
    public string Intro { get; set; }
    [MaxLength]
    public string Body { get; set; }
    [StringLength(150)]
    public string Author { get; set; }
    public int? PreviewImageId { get; set; }
    public int? CoverImageId { get; set; }
    public int StoreId { get; set; }
    public int LanguageId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedOnUtc { get; set; }
    public DateTime? ExpiresOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
    [StringLength(250)]
    public string MetaTitle { get; set; }
    [StringLength(500)]
    public string MetaDescription { get; set; }
    public bool IncludeInSitemap { get; set; } = true;
    public ICollection<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
    public ICollection<BlogTag> Tags { get; set; } = new List<BlogTag>();
}

[Table("BlogCategory")]
[Index(nameof(Slug), IsUnique = true)]
public class BlogCategory : BaseEntity, ISlugSupported
{
    public string GetDisplayName() => Name;
    public string[] GetDisplayNameMemberNames() => [nameof(Name)];

    [Required, StringLength(150)]
    public string Name { get; set; }
    [Required, StringLength(180)]
    public string Slug { get; set; }
    [StringLength(500)]
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
}

[Table("BlogTag")]
[Index(nameof(Slug), IsUnique = true)]
public class BlogTag : BaseEntity, ISlugSupported
{
    public string GetDisplayName() => Name;
    public string[] GetDisplayNameMemberNames() => [nameof(Name)];

    [Required, StringLength(150)]
    public string Name { get; set; }
    [Required, StringLength(180)]
    public string Slug { get; set; }
    [StringLength(500)]
    public string Description { get; set; }
}

internal class BlogPostMap : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasMany(x => x.Categories).WithMany()
            .UsingEntity<Dictionary<string, object>>("BlogPostCategory",
                x => x.HasOne<BlogCategory>().WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                x => x.HasOne<BlogPost>().WithMany().HasForeignKey("PostId").OnDelete(DeleteBehavior.Cascade),
                x => x.HasKey("PostId", "CategoryId"));
        builder.HasMany(x => x.Tags).WithMany()
            .UsingEntity<Dictionary<string, object>>("BlogPostTag",
                x => x.HasOne<BlogTag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                x => x.HasOne<BlogPost>().WithMany().HasForeignKey("PostId").OnDelete(DeleteBehavior.Cascade),
                x => x.HasKey("PostId", "TagId"));
    }
}
