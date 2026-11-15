# OmmCMS — Phase 1: optional CMS shell preview

**Scope:** UI foundation only. This is not a standalone CMS release or a safe public deployment profile.

## Implemented on `cms-shell`
- New app configuration setting `OmmCMS:PreviewShell` (default `false`).
- When enabled, the homepage renders content zones and the existing `HomePageText` CMS Topic without storefront product, category, best-seller or recent-product widgets.
- Shared layout uses a simplified site-name header with existing configurable `Main` menu, and a neutral footer.
- When disabled, all original Smartstore frontend layout, homepage and commerce behaviour remain in place.
- No databases, migrations, controllers, commerce services or routes are changed.

## Local developer testing
1. Build `Smartstore.sln` and run the existing tests.
2. Use a **disposable development database**. Do not point preview at the live Smartstore store.
3. Set `OmmCMS:PreviewShell=true` in a local overridden configuration or environmental variable (`OmmCMS__PreviewShell=true`), not in a checked-in secret file.
4. Create or edit the CMS Topic with system name `HomePageText`, and configure the `Main` menu to contain only intended content links.
5. Check home, menus, page/topics, admin and responsive layout at phone and desktop widths.
6. Return to `false`; verify the original store homepage and header still appear.

## Known limitations — must be addressed before any public CMS launch
- **Commerce controllers and URLs are still available.** Hiding their homepage widgets is not removal or access restriction.
- **Smartstore's shop-oriented database is unchanged.**
- **The existing Main menu may contain product/shop links** until CMS configuration is added.
- **Admin sitemap is unchanged** and still includes sales/catalog management.
- **Blog implementation and a complete visual Page Builder require separate source verification**, as identified in `/ommlabs/smartstore/audit/03_SOURCE_LEVEL_CMS_AUDIT.md`.
- We have not performed local browser tests or deployed the preview.

## Next stage
1. CMS-neutral menu/header/footer components and admin sitemap, with dedicated tests.
2. Disable commerce routes and modules in CMS edition without breaking shared services.
3. Implement articles, taxonomy, publishing and blocks after verifying existing code rights.
4. Clean CMS-only schema and installer, then performance, security and accessibility reviews.
