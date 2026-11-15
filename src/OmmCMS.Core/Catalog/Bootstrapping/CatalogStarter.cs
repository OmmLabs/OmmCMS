using Autofac;
using OmmCMS.Core.Catalog.Attributes;
using OmmCMS.Core.Catalog.Attributes.Rules;
using OmmCMS.Core.Catalog.Brands;
using OmmCMS.Core.Catalog.Brands.Rules;
using OmmCMS.Core.Catalog.Categories;
using OmmCMS.Core.Catalog.Categories.Rules;
using OmmCMS.Core.Catalog.Discounts;
using OmmCMS.Core.Catalog.Pricing;
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

        builder.RegisterType<DiscountService>().As<IDiscountService>().InstancePerLifetimeScope();
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

        // Pricing
        builder.RegisterType<PriceCalculationService>().As<IPriceCalculationService>().InstancePerLifetimeScope();
        builder.RegisterType<PriceCalculatorFactory>().As<IPriceCalculatorFactory>().InstancePerLifetimeScope();
        builder.RegisterType<PriceLabelService>().As<IPriceLabelService>().InstancePerLifetimeScope();

        DiscoverCalculators(builder, appContext);

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

    private static void DiscoverCalculators(ContainerBuilder builder, IApplicationContext appContext)
    {
        var calculatorTypes = appContext.TypeScanner.FindTypes<IPriceCalculator>();

        foreach (var calculatorType in calculatorTypes)
        {
            var usageAttribute = calculatorType.GetAttribute<CalculatorUsageAttribute>(true);

            var registration = builder
                .RegisterType(calculatorType)
                .As<IPriceCalculator>()
                .Keyed<IPriceCalculator>(calculatorType)
                .InstancePerAttributedLifetime()
                .WithMetadata<PriceCalculatorMetadata>(m =>
                {
                    m.For(em => em.CalculatorType, calculatorType);
                    m.For(em => em.ValidTargets, usageAttribute?.ValidTargets ?? CalculatorTargets.All);
                    m.For(em => em.Order, usageAttribute?.Order ?? CalculatorOrdering.Default);
                });
        }

        // Register calculator resolve delegate
        builder.Register<Func<Type, IPriceCalculator>>(c =>
        {
            var cc = c.Resolve<IComponentContext>();
            return key => cc.ResolveKeyed<IPriceCalculator>(key);
        });
    }
}