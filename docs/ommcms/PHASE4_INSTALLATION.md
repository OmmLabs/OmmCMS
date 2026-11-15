# Phase 4 — CMS installation and runtime cleanup

## Scope

The opt-in `OmmCMS:PreviewShell` profile now selects dedicated starter store, topic, menu and settings seeds. The server suppresses sample commerce data even if requested by an installation client. The installer defaults to SQLite in CMS mode and displays CMS branding.

- Retain required common platform schema, currencies, countries, roles, email, media, and other compatibility records.
- Do not seed shop-only topics, catalog menu providers, active payments, product rule evaluation, or exchange-rate jobs in CMS mode.
- Keep mail, maintenance, customer role evaluation and sitemap jobs.
- Skip checkout-state middleware and the unavailable commerce search structured-data action.
- Brand the public generator, admin header and installer while preserving upstream attribution.
- Preserve legacy installation and runtime behavior when the profile is off.
- No schema migration, physical deletion of commerce code or existing database conversion.

## Validation

The real installation seeder runs in isolated in-memory SQLite for every supported culture. CMS-specific assertions cover the content-only profile even when sample data is requested. Existing stock seeding tests remain.

CI publishes a disposable local application and invokes the installer against a new SQLite file. The smoke test checks the branded installation page, database contents, application restart, public content routes and blocked commerce endpoints. No production database or credentials are used.

## Next phase

Confirm authenticated editorial operations, content publishing, media upload, menu editing, themes, localization and permissions. Map commerce dependencies before deleting services, projects, schema, or migrations. Keep all existing sites and the default branch untouched.
