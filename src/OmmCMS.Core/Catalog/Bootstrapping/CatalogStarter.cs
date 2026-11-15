using Autofac;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Attributes.Rules;
using OmmCMS.Core.Catalog.Brands;
using OmmCMS.Core.Catalog.Brands.Rules;
using OmmCMS.Core.Catalog.Categories;
using OmmCMS.Core.Catalog.Categories.Rules;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Catalog.Products.Rules;
using OmmCMS.Core.Catalog.Rules;
using OmmCMS.Core.Catalog.Search;
using OmmCMS.Core.Catalog.Search.Modelling;
using OmmCMS.Core.DataExchange;
using OmmCMS.Core.DataExchange.Import;
using OmmCMS.Core.Rules.Rendering;
using OmmCMS.Core.Search;
using OmmCMS.Core.Search.Facets;
using OmmCMS.Core.Seo;
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
            .As<IXmlSitemapPublisher>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ManufacturerService>()
            .As<IManufacturerService>()
            .As<IXmlSitemapPublisher>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ProductAttributeMaterializer>().As<IProductAttributeMaterializer>().InstancePerLifetimeScope();
        builder.RegisterType<ProductAttributeFormatter>().As<IProductAttributeFormatter>().InstancePerLifetimeScope();

        builder.RegisterType<ProductUrlHelper>().InstancePerLifetimeScope();
        builder.RegisterType<ProductImporter>().Keyed<IEntityImporter>(ImportEntityType.Product).InstancePerLifetimeScope();
        builder.RegisterType<CategoryImporter>().Keyed<IEntityImporter>(ImportEntityType.Category).InstancePerLifetimeScope();
        builder.RegisterType<ManufacturerImporter>().Keyed<IEntityImporter>(ImportEntityType.Manufacturer).InstancePerLifetimeScope();

        // Search.
        builder.RegisterType<CatalogSearchQueryVisitor>()
            .As<LinqSearchQueryVisitor<Product, CatalogSearchQuery, CatalogSearchQueryContext>>()
            .SingleInstance();

        builder.RegisterType<CatalogSearchService>().As<ICatalogSearchService>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
        builder.RegisterType<LinqCatalogSearchService>().Named<ICatalogSearchService>("linq").InstancePerLifetimeScope();
        builder.RegisterType<CatalogSearchQueryFactory>().As<ICatalogSearchQueryFactory>().InstancePerLifetimeScope();
        builder.RegisterType<CatalogSearchQueryAliasMapper>().As<ICatalogSearchQueryAliasMapper>().InstancePerLifetimeScope();
        builder.RegisterType<CatalogFacetUrlHelper>().As<IFacetUrlHelper>().InstancePerLifetimeScope();

        // Rules.
        builder.RegisterType<ProductRuleProvider>().As<IProductRuleProvider>().InstancePerLifetimeScope();
        builder.RegisterType<NullAttributeRuleProvider>().As<IAttributeRuleProvider>().InstancePerLifetimeScope();

        // Rule options provider.
        builder.RegisterType<ProductVariantAttributeValueRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        builder.RegisterType<SpecificationAttributeOptionRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        builder.RegisterType<ManufacturerRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        builder.RegisterType<CategoryRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        builder.RegisterType<ProductRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        builder.RegisterType<ProductTagRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
    }

}
