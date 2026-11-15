using Microsoft.EntityFrameworkCore;

namespace OmmCMS.Data;

public interface IDbContextConfigurationSource<TContext>
    where TContext : DbContext
{
    void Configure(IServiceProvider services, DbContextOptionsBuilder builder);
}