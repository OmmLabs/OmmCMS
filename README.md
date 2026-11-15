# OmmCMS

An open-source, ASP.NET Core content management system in development by [OmmLabs](https://github.com/OmmLabs). OmmCMS builds on the Smartstore platform, retaining its content, administration and extension foundations while progressively separating e-commerce.

**Status:** development preview, not a production release. The CMS profile is opt-in and the original Smartstore behavior remains the default. The commerce database schema and shared services remain during the transition.

## What the CMS profile provides

- Website pages (topics), blog, news, menus, media library and SEO tools.
- Account authentication, roles and permissions, localization, and theme infrastructure.
- Content-focused public layout and administration navigation.
- A starter website with Home, About and Contact pages; no sample shop catalog.
- Commerce route isolation and exclusion of selected bundled commerce modules.

## Try a fresh installation

Prerequisites: .NET 10 SDK, a supported database (SQLite for local evaluation, SQL Server, PostgreSQL or MySQL), and a writable application data directory.

```bash
git clone https://github.com/OmmLabs/OmmCMS.git
cd OmmCMS
git switch cms-installation
dotnet restore Smartstore.sln
dotnet build Smartstore.sln -c Release --no-restore
cd src/Smartstore.Web
OmmCMS__PreviewShell=true ASPNETCORE_URLS=http://localhost:5080 dotnet run -c Release --no-build --no-launch-profile
```

On Windows PowerShell, set `$env:OmmCMS__PreviewShell = "true"` and `$env:ASPNETCORE_URLS = "http://localhost:5080"` before running the same dotnet command.

1. Open http://localhost:5080/install.
2. Use a new database and create a strong administrator password. SQLite is recommended for the preview.
3. Complete installation and restart as directed by the installer. Keep the CMS environment variable enabled on every launch.
4. Open the website and its administration area at `/admin`. Edit starter pages, menus and legal text before publishing.

Do not use an existing commerce database for this preview. This is not a database migration or a shop-to-CMS conversion tool.

## Build and test

```bash
dotnet test Smartstore.sln -c Release
```

CI also defines a disposable SQLite fresh-install smoke test for the CMS installation branch. See [Phase 4](docs/ommcms/PHASE4_INSTALLATION.md) for its scope and current limitations.

## Development roadmap

| Phase | Scope |
| --- | --- |
| 1 | [CMS public shell](docs/ommcms/PHASE1_CMS_SHELL.md) |
| 2 | [CMS administration navigation](docs/ommcms/PHASE2_ADMIN_NAVIGATION.md) |
| 3 | [Commerce isolation](docs/ommcms/PHASE3_COMMERCE_ISOLATION.md) |
| 4 | [CMS installation and runtime cleanup](docs/ommcms/PHASE4_INSTALLATION.md) |
| Next | Validate CMS workflows, then remove commerce service and schema dependencies in small, tested steps. |

Source and project names retain their Smartstore names until the dependency extraction is complete.

## License and attribution

OmmCMS is derived from [Smartstore](https://github.com/smartstore/Smartstore). The upstream copyright, license and attribution are preserved; see [LICENSE](LICENSE) and the [original Smartstore README](README.Smartstore.md). The project is licensed under the GNU Affero General Public License v3 as provided in the repository. Third-party components retain their respective licenses.
