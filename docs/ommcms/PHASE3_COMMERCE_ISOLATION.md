# OmmCMS — Phase 3: commerce isolation

This is a development preview, not a standalone CMS release.

## Implemented

- The existing opt-in `OmmCMS:PreviewShell` setting now enables a backend boundary. It defaults to false. Restart after changing it.
- Middleware after endpoint routing rejects the reviewed storefront and admin commerce MVC controllers and specific commerce actions on mixed controllers with HTTP 404 before invoking the action. It uses resolved controller/action metadata instead of URL prefix matching, including slug and localized routes.
- Catalog search, product/catalog, cart, checkout, sales, payment, shipping, tax, returns, promotional administration and legacy import/export controllers are blocked.
- Mixed customer actions (orders, returns, downloadable products, rewards, stock subscriptions and related admin actions) are blocked. Account navigation and search settings shortcuts are updated.
- The admin home redirects to the topic list in CMS preview; commerce dashboard widget endpoints are blocked.
- Generic entity picker requests for product/category/manufacturer are rejected; generic patch and localized patch requests accept only reviewed CMS/platform entity types, keeping existing authorization.
- Ten bundled module system names are excluded from discovery before assemblies are loaded or registered, including payment/shipping/tax providers, Merchant Center, order SMS and the commerce-capable OData WebApi. Authentication, analytics and developer modules remain eligible. The original files remain available in stock mode.
- No application schema deletion, database migration, source removal or production deployment is part of this change.

## Run and validate

1. Use a disposable database and set `OmmCMS__PreviewShell=true` **before starting**. Do not use OmmRudraksha production.
2. Verify installation, home, topic details/slug, contact, sign-in, profile, admin pages/media/menus/themes and permission checks.
3. Verify direct GET and POST to cart, checkout, product, category, order and corresponding administration endpoints; they must not execute. Verify that excluded modules are absent from the loaded module catalog including WebApi.
4. Restart with `OmmCMS__PreviewShell=false` against a disposable stock installation and verify the original storefront, administration and modules.
5. Run the solution build and tests. The new tests exercise controller metadata classification, mixed actions/aliases, middleware short-circuiting and pass-through, opt-in behavior, module policy and entity picker/patch policy.

## Remaining work

- The middleware tests use prepared endpoint metadata; an end-to-end installed-host test (including actual localized routing, OData absence, sign-in and editor operations) is still required.
- CMS mode does not remove commerce service registrations, entities, seed data, scheduled tasks or hooks. Existing installed module state is not explicitly uninstalled; a fresh CMS preview installation will omit excluded modules, so switching modes is not a database conversion.
- Unknown third-party modules are not allowlisted or security-reviewed. Generic platform features, admin user editing, data retained in the database, widgets, configured navigation, domain services, sitemap content, and background tasks still need a CMS-specific audit. Do not consider this a security boundary for untrusted administrators or publish it as a commerce-free production CMS.
- Next: CMS-safe runtime/installation, implement independent article/taxonomy/publishing and then clean schema and branding.
