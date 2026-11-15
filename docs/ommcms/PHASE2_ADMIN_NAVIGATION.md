# OmmCMS — Phase 2: CMS-focused administration navigation

Status: implementation in review, not deployed.

## Scope
- Introduces a separate `Areas/Admin/ommcms-sitemap.xml` for the CMS preview.
- `AdminMenu` selects the new sitemap only when `OmmCMS:PreviewShell=true`; otherwise it loads the original `sitemap.xml`.
- Admin menu cache keys include CMS/store mode to avoid cross-profile cache collisions.
- `SettingsMenu` hides commerce-oriented shortcut links (catalog, cart, orders, payments, finance, shipping, reward points, data exchange and all-settings) during preview.
- Original Smartstore menus and settings remain unchanged with preview disabled.
- `OmmCmsAdminSitemapTests` guard the intended navigation structure and commerce-free menu entries.

## OmmCMS admin sections (preview only)
1. Content: Pages & Topics, Menus, Widgets, Email templates.
2. Design & SEO: Themes, search-friendly URL management, media settings.
3. Users & access: users, roles, external sign-in and activity log.
4. Site settings: general settings, sites, languages and email accounts.
5. Maintenance: logs, queued mail, scheduled tasks, maintenance.
6. Extensions: existing module manager.

## Security and feature limitations
- **Do not deploy as a standalone CMS.** This phase changes navigation, not controller access, route registration, module loading or database schema.
- Commerce URLs and APIs still exist; hiding navigation does not restrict access.
- Third-party module admin-menu contributions can still be added under Extensions.
- General settings, administration controllers and widget providers may contain shop-specific features.
- CMS topics and menus are present; the Blog engine and full Page Builder may require separate work.
- Do not use an existing OmmRudraksha production database for preview testing.

## Validation
- Run `dotnet restore Smartstore.sln`, `dotnet build Smartstore.sln -c Release --no-restore`, `dotnet test Smartstore.sln -c Release --no-build --no-restore`.
- Start with `OmmCMS__PreviewShell=true` against a disposable development installation.
- Test: CMS admin menu sections, links, permissions with different roles, media settings, pages, menus, themes, user administration, mobile admin layout.
- Disable preview; confirm stock Admin menu and settings shortcuts return.
- Verify that direct commerce URLs remain reachable (known limitation, to be addressed in a separate explicit route security milestone).

## Next (Phase 3)
- Isolate commerce controllers/routes and commerce modules behind a CMS edition boundary (not merely hide links).
- Remove commerce-specific hooks and checkout/catalog runtime dependencies incrementally.
- Implement independent CMS content/blocks and clean CMS migrations after dependency review.

No changes to `main` or to production infrastructure.
