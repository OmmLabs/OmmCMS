# Phase 12 — Commerce presentation and core leaf service extraction

Base: validated Phase 11 (`cms-residual-commerce-extraction`).

- Remove the retired CatalogHelper and OrderHelper (including partials, DI registration and related storefront models/mappers). Topic details now builds the original menu trail directly using IMenuService and IBreadcrumb.
- Remove commerce-specific cart, checkout, product, order, receipt PDF and search views/components, obsolete shared models and the associated demo filters. General PDF conversion, content menu templates, media and generic search infrastructure remain.
- Remove four disconnected catalog services (product comparison, recently viewed products, product cloning and product attribute administration) and their registrations. No schema/entity/migration changes.
- Remove catalog presentation cache invalidation branches while preserving Topic, language, geographical and store identity invalidation.
- Keep Google Analytics page tracking, script customization, consent and opt-out functionality. Remove product/cart/order events and checkout settings from the live configuration UI. Retain legacy settings and migrations for compatibility.
- Remove two tests specific to deleted helpers rather than maintaining dummy commerce code. Replace them with assembly absence checks and a CMS breadcrumb dependency test.

Validation: full Release build, tests, published artifact absence, retired receipt endpoint HTTP 404, fresh SQLite installation and authenticated editorial regressions. No main or production changes.
