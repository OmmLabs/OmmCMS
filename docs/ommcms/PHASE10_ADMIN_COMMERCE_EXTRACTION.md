# Phase 10 — Remaining commerce administration extraction

Base: Phase 9 (`cms-account-dependency-extraction`).

Remove the 11 remaining blocked commerce administration controller types: Campaign, Category, Currency, Discount, Export, Import, Manufacturer, Measure, Order, Product and Search. Product is split across six files. Remove their dedicated views, the three remaining shipment grid/import files (no longer referenced after Order removal), and the three commerce grids previously linked by the customer editor. Remove order/reward/cart/wishlist tabs from that editor while preserving account, permission, address, impersonation, messaging and GDPR facilities.

The existing `Smartstore.Controllers.PackagingController` is not a commerce controller: it implements theme/module package installation. Preserve its code and existing blocked policy until a dedicated CMS extension-upload security review. Newsletter subscriptions remain; commerce campaign UI is retired. General data exchange services and GDPR export are retained until remaining dependencies are fully reviewed.

The denylist remains defense in depth. Retain shared domain entities, models, core services, database schema and migrations; no production changes. This is a staged source extraction, not a migration.

Validation: full Release build and tests, assembly absence, source and publish absence, authenticated former admin routes HTTP 404, retained customer editor and all earlier fresh SQLite CMS workflows.
