global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using OmmCMS.Domain;
global using OmmCMS.Engine;
global using EfState = Microsoft.EntityFrameworkCore.EntityState;
global using EntityState = OmmCMS.Data.EntityState;
global using LogLevel = OmmCMS.Core.Logging.LogLevel;
using System.Text;
using Autofac;
using OmmCMS.Bootstrapping;
using OmmCMS.ComponentModel;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Data;
using OmmCMS.Core.Data.Migrations;
using OmmCMS.Core.Data.Migrations.Icons;
using OmmCMS.Data;
using OmmCMS.Data.Caching;
using OmmCMS.Data.Providers;
using OmmCMS.Engine.Builders;
using OmmCMS.Templating;
using OmmCMS.Templating.Liquid;

namespace OmmCMS.Core.Bootstrapping;

internal class CoreStarter : StarterBase
{
    public override int Order => (int)StarterOrdering.Early;

    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        var config = appContext.AppConfiguration;

        // Type converters
        RegisterTypeConverters();

        // CodePages dependency required by ExcelDataReader to avoid NotSupportedException "No data is available for encoding 1252."
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddDbMigrator(appContext);
        services.AddDisplayControl();
        services.AddWkHtmlToPdf();
        services.AddPdfHttpClient();

        if (appContext.IsInstalled && config.UsePooledDbContextFactory)
        {
            // Application DbContext as pooled factory
            services.AddPooledDbContextFactory<SmartDbContext>(DbContextAction, config.DbContextPoolSize);
        }
        else
        {
            // No pooling allowed or desired.
            services.AddDbContextFactory<SmartDbContext>(DbContextAction);
        }

        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<SmartDbContext>>().CreateDbContext());

        void DbContextAction(IServiceProvider c, DbContextOptionsBuilder builder)
        {
            if (appContext.IsInstalled)
            {
                if (config.UseDbCache)
                {
                    builder.UseSecondLevelCache();
                }

                if (config.UseSequentialDbDataReader && DataSettings.Instance.DbFactory.DbSystem == DbSystemType.SqlServer)
                {
                    // Fixes large binary or text async read performance issue.
                    // See: https://github.com/dotnet/SqlClient/issues/593
                    builder.AddInterceptors(new SequentialDbCommandInterceptor());
                }
            }

            builder
                .UseDbFactory(factoryBuilder =>
                {
                    factoryBuilder
                        //.QuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                        .AddModelAssemblies(new[]
                        { 
                            // Add all core models from OmmCMS.Core assembly
                            typeof(SmartDbContext).Assembly,
                            // Add provider specific entity configurations
                            DataSettings.Instance.DbFactory.GetType().Assembly
                        });

                    if (appContext.IsInstalled)
                    {
                        factoryBuilder.AddDataSeeder<SmartDbContext, SmartDbContextDataSeeder>();
                    }
                });

            var configurers = c.GetServices<IDbContextConfigurationSource<SmartDbContext>>();
            foreach (var configurer in configurers)
            {
                configurer.Configure(c, builder);
            }
        }
    }

    internal static void RegisterTypeConverters()
    {
    }

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<IconMigrator>().As<IIconMigrator>().SingleInstance();

        builder.RegisterType<LiquidTemplateEngine>().As<ITemplateEngine>().SingleInstance();

        builder.RegisterModule(new LoggingModule());
        builder.RegisterModule(new PackagingModule());
        builder.RegisterModule(new CommonServicesModule());
        builder.RegisterModule(new DbHooksModule(appContext));
        builder.RegisterModule(new DbQuerySettingsModule());
        builder.RegisterModule(new StoresModule());
    }
}
