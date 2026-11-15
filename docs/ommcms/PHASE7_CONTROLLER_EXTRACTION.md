# Phase 7 — Physical admin commerce controller extraction

Base: Phase 6, `cms-module-extraction`. This phase removes 18 blocked commerce-only admin controllers and 110 Razor files (128 files, 529,580 source bytes).

## Removed controllers

- `AffiliateController` (admin area)
- `BarcodeTesterController` (admin area)
- `CheckoutAttributeController` (admin area)
- `CollectionGroupController` (admin area)
- `DeliveryTimeController` (admin area)
- `GiftCardController` (admin area)
- `PriceLabelController` (admin area)
- `ProductReviewController` (admin area)
- `QuantityUnitController` (admin area)
- `RecurringPaymentController` (admin area)
- `ReturnCaseController` (admin area)
- `ShipmentController` (admin area)
- `ShippingController` (admin area)
- `TaxController` (admin area)
- `PaymentController` (admin area)
- `ShoppingCartController` (admin area)
- `SpecificationAttributeController` (admin area)
- `ProductAttributeController` (admin area)

Seventeen matching view directories are removed. Four standalone shipment views are removed; the shared shipment grid, search partial and view imports remain because the retained Order view references the grid. The customer editor no longer includes the obsolete affiliate assignment action.

## Dependency boundary

- Retain the commerce isolation middleware and its denylist as defense in depth.
- Retain public commerce controllers and the remaining admin controllers (including Product, Order, Category, Import and Export) until their routing, partials, services and consumers can be severed.
- Do not drop database tables, migrations, domain entities, service interfaces, provider infrastructure or model classes based on controller removal alone. Core services remain coupled to public controllers, rules, order processing, customer workflows and data mappings.
- Retain all CMS content, navigation, media, authentication, permissions, localization, theme and installation infrastructure.
- Git history is the recovery path; this change does not touch any deployed application or database.

## Validation

The existing build, tests and disposable SQLite installation/editorial smoke workflow now run on `cms-controller-extraction`. New checks assert extracted controller types are absent from the compiled assembly, their standalone views are absent from source and publish output, and former admin routes return HTTP 404 for an authenticated administrator. The existing commerce isolation, module extraction and content workflow checks remain.

## Next

Extract remaining public commerce controllers and their routing/filter/analytics dependencies, then remove disconnected core services in dependency order. Database schema reduction follows only after shared dependencies have been removed and clean-install tests prove the smaller database.
