using OmmCMS.Core.Content.Blogs;

namespace OmmCMS.Web.Models.Blogs;

public class BlogPageModel
{
    public string Title { get; set; } = "Blog";
    public string Description { get; set; }
    public string Query { get; set; }
    public string Category { get; set; }
    public string Tag { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int Page { get; set; }
    public int Total { get; set; }
    public int PageSize { get; set; } = 12;
    public List<BlogPost> Posts { get; set; } = [];
    public List<BlogCategory> Categories { get; set; } = [];
    public List<BlogTag> Tags { get; set; } = [];
    public List<BlogArchiveMonth> Archive { get; set; } = [];
}

public record BlogArchiveMonth(int Year, int Month, int Count);

public class BlogDetailModel
{
    public BlogPost Post { get; set; }
    public List<BlogPost> Related { get; set; } = [];
}
