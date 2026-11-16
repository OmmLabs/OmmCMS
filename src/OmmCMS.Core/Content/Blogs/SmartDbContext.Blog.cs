using Microsoft.EntityFrameworkCore;
using OmmCMS.Core.Content.Blogs;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogCategory> BlogCategories { get; set; }
    public DbSet<BlogTag> BlogTags { get; set; }
}
