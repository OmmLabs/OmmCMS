# OmmCMS

An open-source ASP.NET Core content management system developed by OmmLabs.

**Status:** development preview. The CMS baseline is validated; remaining commerce internals are being removed before a stable release.

## Features

- Website pages, articles, navigation menus, media management and SEO.
- Content administration, roles, permissions, authentication and localization.
- Theme infrastructure and a starter website with Home, About and Contact pages.
- SQLite, SQL Server, PostgreSQL and MySQL database providers.

## Getting started

Install the .NET 10 SDK, clone this repository and run:

```bash
dotnet restore Smartstore.sln
dotnet build Smartstore.sln -c Release --no-restore
cd src/Smartstore.Web
dotnet run -c Release --no-build --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/install. Use a fresh database (SQLite is recommended for local evaluation), complete the installer and visit `/admin` to manage content. Do not connect the preview to an existing commerce database.

## Development

Work takes place on `main`. Run `dotnet test Smartstore.sln -c Release` before changing the baseline.

CI publishes a disposable application and checks a fresh SQLite installation, editorial workflows and retired commerce endpoints. See the [roadmap](docs/ROADMAP.md) for completed work and next milestones. Internal project names are retained until dependency cleanup is complete.

## License

OmmCMS is licensed under [AGPL-3.0](LICENSE). See [NOTICE](NOTICE.md) for upstream and third-party attribution.
