# OmmCMS

An open-source ASP.NET Core content management system developed by OmmLabs.

Website: https://ommlabs.ommrudraksha.com

**Status:** development preview. The CMS baseline is validated; remaining commerce internals are being removed before a stable release.

## Features

- Website pages, articles, navigation menus, media management and SEO.
- Content administration, roles, permissions, authentication and localization.
- Theme infrastructure and a starter website with Home, About and Contact pages.
- SQLite, SQL Server, PostgreSQL and MySQL database providers.

## Getting started

Install the .NET 10 SDK, clone this repository and run:

```bash
dotnet restore OmmCMS.sln
dotnet build OmmCMS.sln -c Release --no-restore
cd src/OmmCMS.Web
dotnet run -c Release --no-build --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/install. Use a fresh database (SQLite is recommended for local evaluation), complete the installer and visit `/admin` to manage content. The initial OmmCMS schema is created directly by the installer and recorded as one core migration. Older upstream and OmmCMS preview databases are not supported for in-place upgrade; use a fresh database.

## Development

Work takes place on `main`. Run `dotnet test OmmCMS.sln -c Release` before changing the baseline.

CI publishes a disposable application and checks a fresh SQLite installation, editorial workflows and retired commerce endpoints.

Completed: CMS shell, administration, installation, content workflows and phases 1–45 of commerce extraction, including affiliates, stock subscriptions, related products, product reviews recurring payments, checkout workflow/session orchestration, order processing, order-total calculation, and gift cards (including product, attribute and coupon handling). Payment and shipping providers and method configuration have also been extracted. Shipment records, returns, order notes, delivery-date services, and auxiliary fee tax handling have been extracted. Cart quantity, product and variant, category, manufacturer, and delivery-time rule processors have also been extracted (phases 31–35). Cart and wishlist service and validator implementations, order-history and address cart rules, cart administration models and obsolete cart assets have been extracted (phases 36–45). Next: remove remaining cart persistence, checkout attributes and catalog dependencies.

Report security issues privately to the maintainers; do not publish sensitive details.

## License

OmmCMS is licensed under [AGPL-3.0](LICENSE). See [NOTICE](NOTICE.md) for upstream and third-party attribution.
