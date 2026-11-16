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

Completed: CMS shell, administration, installation, content workflows and phases 1–605 of commerce extraction, including affiliates, stock subscriptions, related products, product reviews recurring payments, checkout workflow/session orchestration, order processing, order-total calculation, and gift cards (including product, attribute and coupon handling). Payment and shipping providers and method configuration have also been extracted. Shipment records, returns, order notes, delivery-date services, and auxiliary fee tax handling have been extracted. Cart quantity, product and variant, category, manufacturer, and delivery-time rule processors have also been extracted (phases 31–35). Cart and wishlist service and validator implementations, order-history and address cart rules, cart administration models and obsolete cart assets have been extracted (phases 36–45). Cart persistence, export paths, administration statistics, customer cleanup dependencies, checkout attribute services and cart-specific pricing and product helpers are extracted (phases 46–65). Checkout attribute entities, rule mappings, media and tax adapters, checkout permissions and cart configuration have been extracted (phases 66–85). Discount persistence, coupon attributes, discount rules and relationships, pricing calculators, admin models and templates, and seed/export dependencies have been extracted (phases 86–105). Tier pricing persistence, calculation, caching, export/import, samples and admin configuration, and remaining discount and bundle-discount settings have been extracted (phases 106–125). Special-offer prices, offer dates, promotional badges and countdowns, residual discount amounts, search and administration dependencies have been extracted (phases 126–145). Compare-price and retail-price fields, price-label persistence and administration, saving calculation, and corresponding export and seed dependencies have been extracted (phases 146–165). The remaining catalog pricing engine, calculator discovery, export-price projections, base-price fields, customer-entered pricing and price-specific swatch performance settings have been extracted (phases 166–205). Inventory adjustment service, public contract, result type, stock save hook, low-stock notification and order-item adjustment model have been extracted (phases 206–211). Stock and backorder fields, variant inventory, availability search and rules, menu filtering, delivery-time stock overrides, imports, exports, admin models and sample data have been extracted (phases 212–235). Order quantity controls, purchase buttons, preorders, required-product relationships, dependent restoration, seeding and validation have been extracted (phases 236–252). Shipping flags, shipping fees, role free-shipping settings, bundle shipping, pre-shipment withdrawal, shipping-cost exports, rules, admin and seed dependencies have been extracted (phases 253–275). Electronic-service VAT classification, stock-independent tax search and rule filters, customs tariff metadata, tax-address ESD overrides, admin projections and sample data have been extracted (phases 276–288). Product, customer and role tax exemptions, exemption service and rules, import/export, administrator fields and dependent tests have been extracted (phases 289–307). Unused tax calculator, tax-pair administration widget, script, tax-format adapter and EU consumer detection have been extracted (phases 308–315). VAT registration and profile submission, validation, public and administrator views, presentation models and mandatory VAT settings have been extracted (phases 316–326). External VAT validation, VIES integration, notification dispatch and rendering, administrative warning, related configuration and tests have been extracted (phases 327–340). VAT exemption resolution, customer VAT status and attributes, order metadata, rule processors, messaging, import/export and remaining VAT settings have been extracted (phases 341–359). Product tax-category associations, deletion hooks, product import/export, administrator models and seed references have been extracted (phases 360–370). Tax category persistence, installation and fixed-rate seeding, administrator category model and test fixtures have been extracted (phases 371–378). Tax provider discovery, rate calculation, address caching, provider administration and obsolete calculation fixtures have been extracted (phases 379–394). Obsolete tax-address settings, calculation settings, finance administration model and culture seed dependencies have been extracted (phases 395–401). Order messaging adapters, tax-only formatting, obsolete display settings and commerce-specific AI metadata have been extracted (phases 402–411). Role tax-display persistence, administration, export and cache hooks have been extracted (phases 412–418). Customer and order tax-display persistence, work-context resolution, currency rounding coupling, tax footer, targeting filters and remaining tax settings have been extracted (phases 419–449). Order restrictions, order settings, obsolete settings navigation and order-specific message conditions have been extracted (phases 450–459). Order-update events, order service, order-item hooks, address and attribute order notifications and unused report models have been extracted (phases 460–469). Sales, incomplete order, bestseller and top-customer dashboard widgets, views, report models and projections have been extracted (phases 470–497). Latest order widgets have been replaced by contact requests and store statistics now report CMS content, menus, media and users (phases 498–508). Order targeting descriptors, order PDF attachments, unused administrator and order URL models, order export query, projection, preview, post-processing and batch loader have been extracted (phases 509–536). Order and order-item persistence, purchase-dependent account rules, history links, download authorization, order permissions and bundle order serialization have been extracted (phases 537–566). Wallet and reward points persistence, user interface, newsletter rewards, GDPR and export couplings, and payment/shipping status enums have been extracted (phases 567–590). Catalogue-generated menus, item counts, catalogue link schemas, corresponding editor controls, and product-specific menu invalidation have been extracted (phases 591–605). Next: catalogue persistence, search, seeding, import/export and shared media dependencies.

Phases 606–615: CMS-only installation is now unconditional; catalogue templates, product and category samples, sample customer and campaign initialization, shop installer controls, and associated seed descriptors are extracted. The SQLite installation regression tests now require CMS content regardless of the legacy sample request. Next: remaining catalogue export/import, search and persistence dependencies.

Phases 616–620: removed bundled storefront brand, category, product and downloadable demo media and the obsolete catalogue sample guide; the publish smoke test guards against their return.

Phases 621–627: extracted the product, category and manufacturer import engines, their container registrations, import entity choices, and profile field maps. Customer and newsletter imports remain supported with their existing persisted type identifiers; retired profiles are rejected before import.

Phases 628–645: extracted catalogue export queries, data segmenters, batch contexts, dynamic projections, XML output, provider options, filters and admin models; removed product, category and manufacturer media import adapters while preserving generic media import and customer avatars. Customer and newsletter export identifiers remain unchanged, and retired providers are rejected before running.

Phases 646–653: removed catalogue sitemap publishers, their SEO settings, administrator controls and seed values. The LLM discovery directory lists public, published CMS topics rather than catalogue categories and storefront links.

Phases 654–662: removed product messaging models, thumbnail selection, template test models, product sharing and question dispatch, wishlist sharing and obsolete order notification templates. Customer identity, contact and newsletter messages are retained.

Phases 663–670: detached automatic and batch embedded-image extraction from catalogue entities while retaining CMS topics; removed category tree and product attribute hash maintenance actions, warnings, and the unused attribute migration utility.

Phases 671–679: constrained the administration rule editor to customer groups, removed product/cart/attribute rule presentation and obsolete commerce CAPTCHA targets, and retained customer rule editing, grouping, preview and execution. Legacy rule persistence identifiers are unchanged.

Phases 680–690: removed checkout and catalogue rule implementations, scheduled product rule evaluation and obsolete tax permissions; retained customer device filters, authentication rule options and messaging permissions.

Phases 691–715: removed catalogue services, product and category hooks, product search and registration, legacy catalogue/search settings navigation and product overview mapping. Generic search, media tree infrastructure and catalogue persistence remain separate.

Phases 716–730: removed the catalogue admin models, permissions, export previews and settings; detached cache invalidation, media tracking, icon migration, quantity-unit maintenance and generic index writer context from catalogue records. CMS-specific settings now retain content collapsing and upload filtering.

Phases 731–750: removed the remaining catalogue entity model and database sets, including products, categories, manufacturers, mappings, bundles and attributes. Media folder tree support and reusable choice controls were relocated into the retained CMS framework. CMS topic routing and generic SEO/serialization/seeding tests were preserved; fresh-install checks require retired catalogue tables to be absent. Existing database upgrades from the historical commerce schema are unsupported.

Phases 751–765: removed unused commerce directory records and seed data for delivery times, quantity units, weight/dimension conversion and collection groups, including their administration models, rules and diagnostics. General measurement values, common customer/country/currency data and CMS permissions remain.

Phase 766: fixed core compilation after commerce extraction.

Phases 767–775: removed the product detail JavaScript bundle, unused catalogue editor and tax provider model, retired product and recurring-payment email templates, obsolete order email layout, and the product-specific DevTools example. Publish checks guard the retired assets; general developer tooling and CMS navigation remain.

Phases 776–784: removed unused storefront menu, store and topic seed files and their localized content. The CMS seed descriptors and common configuration remain authoritative. Corrected stale messaging permissions, export validation and installer provider selection exposed by compilation.

Phases 785–790: make route isolation, entity patch restrictions, catalogue menu blocking and retired module exclusion unconditional; remove the obsolete storefront administration sitemap and always use the CMS navigation.

Phases 791–800: retire storefront layout and installer presentation fallbacks, make CMS settings navigation unconditional, remove the stale storefront search action from structured data, and remove unused CMS-preview installation seed state. Retain the existing CMS home and administrator presentation.

Phases 801–815: retire country billing/shipping and VAT restrictions from persistence, seed data, address selection and validation, administrative editors, exports and related test fixtures. Country directories and customer address management remain available.

Phases 816–830: remove bank account configuration, settings binding, payment-specific email model parts and footer markup while retaining company and contact information.

Phases 831–840: unify the preferred address selection, its per-site customer attribute, administrative and public address editors, and import defaults. Historical customer address relationships are handled separately.

Phases 841–845: enable the preferred-address picker, editing and ownership checks in the account and administration UI and add a fresh-install regression for selection and persistence.

Phases 846–860: replace the legacy billing/shipping customer relationships with a single contact address, update identity, import/export, country rules, messaging, GDPR and seed data, and preserve the address book and preferred-address selection.

Phases 861–875: retire remaining checkout language-seeder updates, commerce activity-log types and localized seeds, obsolete page identities and permission labels, and the shipment email partial. Shared address presentation and logging remain.

Phases 876–890: remove checkout/order/cart resource families and obsolete bank, country and default-address translations from both installation languages; retire the unused product and payment grid renderers while preserving general UI helpers.

Phases 891–900: remove price adjustments, price cards and pricing CSS from the reusable swatch controls. Retain color, image, shape, size, accessibility and selection behavior with non-commerce unit tests.

Phases 901–920: retire catalog, pricing and tax localization seeder updates and installation translations; move customer lookup, media statistics and expandable text to shared resources, simplify import metadata and remove retired commerce widget-zone groups. Preserve content and extension widgets.

Phases 921–940: generalize currency rounding, retire cart-only rounding options and payment card validators, remove commerce API group labels and residual order, reward, discount and checkout translation families, and move media gallery captions to common resources.

Phases 941–960: remove retired commerce settings and editors from media, identity, privacy, SEO and admin configuration. Keep generic preview and avatar thumbnail sizing, CMS page identity, contact privacy and SEO controls, with authenticated settings page checks.

Phases 961–980: retire product and checkout Sass, price-specific card styles, commerce theme variables and payment-specific module artwork across the theme sources. Retain reusable cards and responsive media galleries; verify compiled CSS for every published theme.

Phases 981–1000: retire the catalog product rotator, its theme settings and obsolete compiled CSS snapshots. Keep accessible menu navigation, dynamic themes and generic media gallery structured data; verify published assets.

Phases 1001–1020: remove commerce-only Google Analytics settings, item identifier migration and resources; retain site tracking and consent. Generalize search facets and entity-picker defaults, retain customer selection and CMS media albums, and test the published configuration.

Phases 1021–1040: retire product-linked choice thumbnails, product picker callbacks, unused product badge rendering and product-specific media assignment fields. Preserve reusable choices and uploads, remove retired administration, tax, pricing and export translations, and verify the retained resources and assets.

Phases 1041–1060: retire product-variant related-data import and export sidecars, associated profile fields and translations. Preserve customer and newsletter import profiles, provider-generated extra export files and the shared data-exchange infrastructure; exclude legacy sidecar files from primary imports.

Phases 1061–1080: remove unused storefront payment, invoice, packaging, return, tax, pricing, catalog and enum translations in both installation languages, along with obsolete sales dashboard permissions and catalog-specific recent selection attributes. Generalize email footer company labels and navbar overflow text while retaining CMS search and navigation resources.

Phases 1081–1100: remove unused storefront AI roles, product-specific prompt behavior, image-count import options and retired rule, permission and export labels. Preserve generic AI editing, media imports, SEO and customer rules; align seed documentation with CMS-only installation.

Phase 1101: retire the unused product attribute-combination performance setting, its administration editor, validation and seed resources, and commerce-specific story asset kinds. Preserve the numeric identifiers of media and link assets.

Report security issues privately to the maintainers; do not publish sensitive details.

## License

OmmCMS is licensed under [AGPL-3.0](LICENSE). See [NOTICE](NOTICE.md) for upstream and third-party attribution.
