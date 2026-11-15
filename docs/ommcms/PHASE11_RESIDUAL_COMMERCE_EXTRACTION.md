# Phase 11 — Residual commerce actions and shared component extraction

Base: validated Phase 10 (`cms-admin-commerce-extraction`).

Remove twelve commerce-only view components and their templates, obsolete leaf models, storefront home/carousel/cart/currency invocations, catalog picker query implementations and mobile brand navigation. Remove nine residual commerce actions from the admin Customer controller, along with order-report/reward views and dependencies. The ordinary account, address, customer-picker authorization, localized patch policies, menus and content publishing remain.

Retain common catalog helpers used by the Topic breadcrumb and cart mappings, reusable dashboard infrastructure (currently blocked), PDF and packaging policy, data model and migrations. No production or main branch changes.

Validation: Release build, full test suite, publish, retired source/publish absence, authenticated route and picker checks, disposable SQLite CMS installation and earlier editorial workflows. Results tracked in CI.
