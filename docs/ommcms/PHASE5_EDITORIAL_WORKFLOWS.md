# Phase 5 — Editorial workflows

This phase builds on the opt-in CMS installation profile. The disposable SQLite CI application exercises the actual HTTP controllers, cookies and anti-forgery tokens. Test content and credentials are created only in the disposable runner.

## Scope

- Administrator login; anonymous access to administration must require authentication.
- Article/page authoring through Topics: draft, publish, SEO slug, public content and role-restricted visibility.
- Content navigation: create and edit a topic menu item; suppress the shop catalog menu provider in CMS mode.
- Upload an image to the standard media file album.
- Load theme configuration, select a different bundled theme and ensure the public home page remains functional.
- Skip commerce cart migration and registration reward points in CMS identity workflows.
- Preserve upstream behavior when CMS mode is disabled.

## Limitations

Topics are the currently implemented article/page editor. A dedicated blog/news editor, taxonomy, scheduled publishing, revisions and advanced media management remain later work. No database tables, migrations, shared services or upstream source are deleted in this phase.

The smoke test verifies live HTTP workflows and persistence using read-only SQLite assertions. It is not a browser-interaction or visual-layout regression suite, nor a substitute for testing with other database providers.

## Next

Audit and extract the commerce service dependency graph in small buildable steps. Implement first-class article collections and publishing capabilities separately from commerce removal.
