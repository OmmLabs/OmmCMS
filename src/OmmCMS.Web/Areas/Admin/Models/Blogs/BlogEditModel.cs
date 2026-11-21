using System.ComponentModel.DataAnnotations;
using OmmCMS.Core.Content.Blogs;

namespace OmmCMS.Admin.Models.Blogs;

public class BlogEditModel
{
    public int Id { get; set; }
    [Required, StringLength(250)]
    public string Title { get; set; }
    [StringLength(180)]
    public string Slug { get; set; }
    [StringLength(1000)]
    public string Intro { get; set; }
    public string Body { get; set; }
    [StringLength(150)]
    public string Author { get; set; }
    [UIHint("Media")]
    [AdditionalMetadata("album", "content")]
    [AdditionalMetadata("transientUpload", true)]
    [AdditionalMetadata("entityType", "BlogPost")]
    public int? PreviewImageId { get; set; }
    [UIHint("Media")]
    [AdditionalMetadata("album", "content")]
    [AdditionalMetadata("transientUpload", true)]
    [AdditionalMetadata("entityType", "BlogPost")]
    public int? CoverImageId { get; set; }
    public int StoreId { get; set; }
    public int LanguageId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresOnUtc { get; set; }
    [StringLength(250)]
    public string MetaTitle { get; set; }
    [StringLength(500)]
    public string MetaDescription { get; set; }
    public bool IncludeInSitemap { get; set; } = true;
    public int[] CategoryIds { get; set; } = [];
    public int[] TagIds { get; set; } = [];
}

public class BlogTermEditModel
{
    public int Id { get; set; }
    public bool IsTag { get; set; }
    [Required, StringLength(150)]
    public string Name { get; set; }
    [StringLength(180)]
    public string Slug { get; set; }
    [StringLength(500)]
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
}
