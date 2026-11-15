# Phase 8 — Public commerce controller extraction

Base: Phase 7 (`cms-controller-extraction`).

Remove eight previously blocked public commerce controllers: Catalog, Checkout, Download, Order, Product, ReturnCase, Search, ShoppingCart. Remove 27 dedicated top-level Razor views. Preserve shared partials, catalog menu components, order print views and the PDF controller for remaining admin/service consumers. Remove the Google Analytics checkout filter registration; preserve its general analytics module.

The existing route isolation denylist remains defense in depth. Shared helpers, route names, mixed customer actions, compatibility services and database schema are retained pending dependency extraction. No production, deployment, default branch or migration changes.

CI builds and tests the solution, asserts controller types and extracted views are absent, installs a disposable SQLite site and checks former endpoints return 404 alongside existing editorial workflows. Validation status is tracked by CI; no pass is presumed.
