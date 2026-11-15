# Phase 6 — Physical commerce module extraction

The OmmCMS host now uses its CMS-only profile automatically. This is a deliberate transition from a reversible preview to a reduced distribution. No deployment or database migration occurs in this phase.

## Removed

| Project | Reason |
| --- | --- |
| Smartstore.AmazonPay | Amazon checkout/payment integration |
| Smartstore.OfflinePayment | Offline shop payment methods |
| Smartstore.PayPal | PayPal checkout integration |
| Smartstore.Stripe | Stripe checkout integration |
| Smartstore.Shipping | Fixed-rate and order-total shipping |
| Smartstore.ShippingByWeight | Weight-based shipping |
| Smartstore.Tax | Shop tax providers |
| Smartstore.Google.MerchantCenter | Product feeds |
| Smartstore.Clickatell | Order notification SMS |
| Smartstore.WebApi | General OData surface exposing commerce entities |

The obsolete standalone Smartstore.WebApi.Client tool was also removed from Smartstore.Tools.sln. All eleven source directories are removed; their Git history remains available. The main solution's project configurations and nested-project records were cleaned. No retained project directly references these removed module projects.

## Retained

Apple, Facebook, Google, Microsoft and Twitter authentication, developer tools and Google Analytics remain. Analytics still has commerce-specific integrations; refactor it into content-only tracking before removing associated core services.

Shared CMS-compatible functionality, database providers and common platform services remain. The commerce database schema, routes (blocked), seed data, and core types have not been physically removed because they still have dependencies. A fresh database is required.

## Build guardrails

The host pins the CMS profile even if an old environment setting requests storefront mode. The legacy module-name denylist remains to reject stale binaries, and the publish project excludes the retired module directories from generated output. The CI smoke test checks the physical absence of retired source and published modules and presence of all retained ones, then installs and exercises the CMS with a legacy false flag.

## Next

Extract the core commerce dependency graph in buildable slices, beginning with commerce-only controllers and leaf services, and simplify analytics. Remove schema only after the entity relationships and migrations are proven independent. Do not remove shared content or identity behavior.
