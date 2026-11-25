# OmmCMS

An open-source ASP.NET Core content management system developed by OmmLabs.

Website: https://ommlabs.ommrudraksha.com

**Status:** development preview. CMS-only extraction baseline. This is not a stable production release.

## Features

- Website pages, dedicated blog posts, reusable content categories and tags, navigation menus, media management and SEO.
- Blog administration under Content, with publishing schedules, cover/preview media, scoped visibility, related articles, archives, search, RSS and sitemap support. Public blog: `/blog`. Blog, category and tag URLs use central URL records with automatic permanent redirects from former paths.
- Content administration, roles, permissions, authentication and localization.
- Nine bundled CMS themes in the Flex family (`Flex`, `FlexAfterglow`, `FlexBlack`, `FlexBlue`, `FlexLumen`, `FlexPorcelain`, `FlexRiso`, `FlexSorbet`, `FlexTidal`) with live theme variable configuration, preview mode, LTR/RTL Sass compilation and a starter website with Home, About, Blog and Contact pages.
- SQLite, SQL Server, PostgreSQL and MySQL database providers.

## Getting started

Install the .NET 10 SDK, clone this repository and run:

```bash
dotnet restore OmmCMS.sln
dotnet build OmmCMS.sln -c Release --no-restore
cd src/OmmCMS.Web
dotnet run -c Release --no-build --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/install. Use a fresh database (SQLite is recommended for local evaluation), complete the installer and visit `/admin` to manage content. The current OmmCMS schema is created directly by the installer and all core migrations are recorded as applied. Older upstream and OmmCMS preview databases are not supported for in-place upgrade; use a fresh database.

## Development

Work takes place on `main`. Run `dotnet test OmmCMS.sln -c Release` before changing the baseline.

CI publishes a disposable application and checks a fresh SQLite installation, editorial workflows and retired commerce endpoints.

## CMS-only architecture

The Smartstore-derived storefront has been extracted in phases 1–1107 and the final cleanup pass. The phase history remains in the repository's commit history.

- Retained: CMS topics and content publishing, navigation, media, themes, SEO, localization, identity, permissions, contact forms, newsletters, scheduling, extension infrastructure and shared data utilities.
- Extracted: product catalogue, cart, checkout, orders, pricing, promotions, inventory, tax, payment, shipping, returns and reward points, including their persistence, seed data, controllers, modules and standalone views.
- Shared platform features such as currency formatting, customer identities, site records and generic search and rule infrastructure remain. The retired-route and entity guards deliberately retain commerce names as deny-list entries.
- The final cleanup removes obsolete commerce labels, corrects CMS-facing descriptions and relocates generic tests and dashboard models to their CMS areas.

### Regression gates

The CI workflow restores and builds the solution, runs unit and integration tests, publishes the application and runs a fresh SQLite installation smoke test. It verifies retired modules, schema and endpoints are absent and exercises authentication, article publishing, access restrictions, navigation, media and themes.

Fresh-install validation covers SQLite. The other database providers compile; separate deployment and provider-specific acceptance testing are required before a stable release.

### Remaining preview acceptance

- [ ] Pass the latest full CI workflow after administration review changes.
- [ ] Complete focused browser acceptance of administrator and restricted-role workflows, including save/delete confirmation, scheduling and maintenance controls.
- [ ] Verify backup and restoration on a disposable database for each provider that supports those operations.
- [ ] Select a tested engine revision and revalidate the separately pinned demo and official website before their deployment.

Do not treat the passing SQLite CI gate as production certification. Report security issues privately to the maintainers; do not publish sensitive details.

## License

OmmCMS is licensed under [AGPL-3.0](LICENSE). See [NOTICE](NOTICE.md) for upstream and third-party attribution.
