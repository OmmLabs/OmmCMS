using OmmCMS.Core.Content.Topics;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Topic> Topics { get; set; }
}