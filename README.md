# OmmCMS

An open-source ASP.NET Core content management system developed by OmmLabs.

Website: https://ommlabs.ommrudraksha.com

**Status:** development preview. The CMS baseline is validated; remaining commerce internals are being removed before a stable release.

## Features

- Website pages, articles, navigation menus, media management and SEO.
- Content administration, roles, permissions, authentication and localization.
- Theme infrastructure and a starter website with Home, About and Contact pages.
- SQLite, SQL Server, PostgreSQL and MySQL database providers.

## Getting started

Install the .NET 10 SDK, clone this repository and run:

```bash
dotnet restore OmmCMS.sln
dotnet build OmmCMS.sln -c Release --no-restore
cd src/OmmCMS.Web
dotnet run -c Release --no-build --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/install. Use a fresh database (SQLite is recommended for local evaluation), complete the installer and visit `/admin` to manage content. The initial OmmCMS schema is created directly by the installer and recorded as one core migration. Older upstream and OmmCMS preview databases are not supported for in-place upgrade; use a fresh database.

## Development

Work takes place on `main`. Run `dotnet test OmmCMS.sln -c Release` before changing the baseline.

CI publishes a disposable application and checks a fresh SQLite installation, editorial workflows and retired commerce endpoints.

Completed: CMS shell, administration, installation, content workflows and phases 1–326 of commerce extraction, including affiliates, stock subscriptions, related products, product reviews recurring payments, checkout workflow/session orchestration, order processing, order-total calculation, and gift cards (including product, attribute and coupon handling). Payment and shipping providers and method configuration have also been extracted. Shipment records, returns, order notes, delivery-date services, and auxiliary fee tax handling have been extracted. Cart quantity, product and variant, category, manufacturer, and delivery-time rule processors have also been extracted (phases 31–35). Cart and wishlist service and validator implementations, order-history and address cart rules, cart administration models and obsolete cart assets have been extracted (phases 36–45). Cart persistence, export paths, administration statistics, customer cleanup dependencies, checkout attribute services and cart-specific pricing and product helpers are extracted (phases 46–65). Checkout attribute entities, rule mappings, media and tax adapters, checkout permissions and cart configuration have been extracted (phases 66–85). Discount persistence, coupon attributes, discount rules and relationships, pricing calculators, admin models and templates, and seed/export dependencies have been extracted (phases 86–105). Tier pricing persistence, calculation, caching, export/import, samples and admin configuration, and remaining discount and bundle-discount settings have been extracted (phases 106–125). Special-offer prices, offer dates, promotional badges and countdowns, residual discount amounts, search and administration dependencies have been extracted (phases 126–145). Compare-price and retail-price fields, price-label persistence and administration, saving calculation, and corresponding export and seed dependencies have been extracted (phases 146–165). The remaining catalog pricing engine, calculator discovery, export-price projections, base-price fields, customer-entered pricing and price-specific swatch performance settings have been extracted (phases 166–205). Inventory adjustment service, public contract, result type, stock save hook, low-stock notification and order-item adjustment model have been extracted (phases 206–211). Stock and backorder fields, variant inventory, availability search and rules, menu filtering, delivery-time stock overrides, imports, exports, admin models and sample data have been extracted (phases 212–235). Order quantity controls, purchase buttons, preorders, required-product relationships, dependent restoration, seeding and validation have been extracted (phases 236–252). Shipping flags, shipping fees, role free-shipping settings, bundle shipping, pre-shipment withdrawal, shipping-cost exports, rules, admin and seed dependencies have been extracted (phases 253–275). Electronic-service VAT classification, stock-independent tax search and rule filters, customs tariff metadata, tax-address ESD overrides, admin projections and sample data have been extracted (phases 276–288). Product, customer and role tax exemptions, exemption service and rules, import/export, administrator fields and dependent tests have been extracted (phases 289–307). Unused tax calculator, tax-pair administration widget, script, tax-format adapter and EU consumer detection have been extracted (phases 308–315). VAT registration and profile submission, validation, public and administrator views, presentation models and mandatory VAT settings have been extracted (phases 316–326). Next: VAT service and tax infrastructure.

Report security issues privately to the maintainers; do not publish sensitive details.

## License

OmmCMS is licensed under [AGPL-3.0](LICENSE). See [NOTICE](NOTICE.md) for upstream and third-party attribution.
