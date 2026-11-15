# OmmCMS

An open-source, ASP.NET Core content management system in development by [OmmLabs](https://github.com/OmmLabs). OmmCMS builds on the Smartstore platform, retaining its content, administration and extension foundations while progressively separating e-commerce.

**Status:** development preview, not a production release. OmmCMS is now CMS-only: the application enables the CMS profile automatically. Ten previously isolated commerce modules, 29 extracted commerce admin controllers, eight public commerce controllers and their dedicated views have been removed. Public account and identity no longer depend on order, checkout, delivery/payment preferences or rewards. Remaining commerce domain entities and the compatibility database schema remain during the transition; obsolete storefront presentation and detached core services have been removed. The repository is no longer a supported storefront build.

## What the CMS profile provides

- Website pages and articles (using Topics), menus, media uploads and SEO tools. A dedicated blog/news publishing system is planned.
- Account authentication, roles and permissions, localization, and theme infrastructure.
- Content-focused public layout and administration navigation.
- A starter website with Home, About and Contact pages; no sample shop catalog.
- Commerce route isolation and physical removal of ten bundled commerce modules and 29 admin controllers.

## Try a fresh installation

Prerequisites: .NET 10 SDK, a supported database (SQLite for local evaluation, SQL Server, PostgreSQL or MySQL), and a writable application data directory.

```bash
git clone https://github.com/OmmLabs/OmmCMS.git
cd OmmCMS
git switch cms-commerce-presentation-extraction
dotnet restore Smartstore.sln
dotnet build Smartstore.sln -c Release --no-restore
cd src/Smartstore.Web
ASPNETCORE_URLS=http://localhost:5080 dotnet run -c Release --no-build --no-launch-profile
```

On Windows PowerShell, set `$env:ASPNETCORE_URLS = "http://localhost:5080"` before running the same dotnet command.

1. Open http://localhost:5080/install.
2. Use a new database and create a strong administrator password. SQLite is recommended for the preview.
3. Complete installation and restart as directed by the installer. The CMS profile is automatically enabled.
4. Open the website and its administration area at `/admin`. Edit starter pages, menus and legal text before publishing.

Do not use an existing commerce database for this preview. This is not a database migration or a shop-to-CMS conversion tool.

## Build and test

```bash
dotnet test Smartstore.sln -c Release
```

CI checks removed modules and standalone commerce views are absent from source and publish output, verifies that extracted controller types are absent, runs the full test suite, and verifies a disposable SQLite fresh-install and authenticated editorial workflows. See [Phase 4](docs/ommcms/PHASE4_INSTALLATION.md) and [Phase 5](docs/ommcms/PHASE5_EDITORIAL_WORKFLOWS.md) for scope and limitations.

## Development roadmap

| Phase | Scope |
| --- | --- |
| 1 | [CMS public shell](docs/ommcms/PHASE1_CMS_SHELL.md) |
| 2 | [CMS administration navigation](docs/ommcms/PHASE2_ADMIN_NAVIGATION.md) |
| 3 | [Commerce isolation](docs/ommcms/PHASE3_COMMERCE_ISOLATION.md) |
| 4 | [CMS installation and runtime cleanup](docs/ommcms/PHASE4_INSTALLATION.md) |
| 5 | [Authenticated CMS editorial workflows](docs/ommcms/PHASE5_EDITORIAL_WORKFLOWS.md) |
| 6 | [Physical commerce module extraction](docs/ommcms/PHASE6_MODULE_EXTRACTION.md) |
| 7 | [Physical admin commerce controller extraction](docs/ommcms/PHASE7_CONTROLLER_EXTRACTION.md) |
| 8 | [Public commerce controller extraction](docs/ommcms/PHASE8_PUBLIC_COMMERCE_EXTRACTION.md) |
| 9 | [Public account and authentication dependency extraction](docs/ommcms/PHASE9_ACCOUNT_DEPENDENCY_EXTRACTION.md) |
| 10 | [Remaining commerce administration extraction](docs/ommcms/PHASE10_ADMIN_COMMERCE_EXTRACTION.md) |
| 11 | [Residual commerce actions and shared component extraction](docs/ommcms/PHASE11_RESIDUAL_COMMERCE_EXTRACTION.md) |
| 12 | [Commerce presentation and core leaf service extraction](docs/ommcms/PHASE12_COMMERCE_PRESENTATION_EXTRACTION.md) |
| Next | Extract remaining admin commerce models, legacy assets and coupled core commerce services before reducing schema; build dedicated article taxonomy and editorial tools. |

Source and project names retain their Smartstore names until the dependency extraction is complete.

## License and attribution

OmmCMS is derived from [Smartstore](https://github.com/smartstore/Smartstore). The upstream copyright, license and attribution are preserved; see [LICENSE](LICENSE) and the [original Smartstore README](README.Smartstore.md). The project is licensed under the GNU Affero General Public License v3 as provided in the repository. Third-party components retain their respective licenses.
