using Autofac;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Brands;
using OmmCMS.Core.Catalog.Categories;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Catalog.Search;
using OmmCMS.Core.Catalog.Search.Modelling;
using OmmCMS.Core.Search;
using OmmCMS.Core.Search.Facets;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Core.Bootstrapping;

internal class CatalogStarter : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<ProductService>().As<IProductService>().InstancePerLifetimeScope();
        builder.RegisterType<ProductTagService>().As<IProductTagService>().InstancePerLifetimeScope();

        builder.RegisterType<CategoryService>()
            .As<ICategoryService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ManufacturerService>()
            .As<IManufacturerService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ProductAttributeMaterializer>().As<IProductAttributeMaterializer>().InstancePerLifetimeScope();
        builder.RegisterType<ProductAttributeFormatter>().As<IProductAttributeFormatter>().InstancePerLifetimeScope();

        builder.RegisterType<ProductUrlHelper>().InstancePerLifetimeScope();

        // Search.
        builder.RegisterType<CatalogSearchQueryVisitor>()
            .As<LinqSearchQueryVisitor<Product, CatalogSearchQuery, CatalogSearchQueryContext>>()
            .SingleInstance();

        builder.RegisterType<CatalogSearchService>().As<ICatalogSearchService>().InstancePerLifetimeScope();
        builder.RegisterType<LinqCatalogSearchService>().Named<ICatalogSearchService>("linq").InstancePerLifetimeScope();
        builder.RegisterType<CatalogSearchQueryFactory>().As<ICatalogSearchQueryFactory>().InstancePerLifetimeScope();
        builder.RegisterType<CatalogSearchQueryAliasMapper>().As<ICatalogSearchQueryAliasMapper>().InstancePerLifetimeScope();
        builder.RegisterType<CatalogFacetUrlHelper>().As<IFacetUrlHelper>().InstancePerLifetimeScope();

    }

}
