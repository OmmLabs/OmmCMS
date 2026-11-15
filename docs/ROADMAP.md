# OmmCMS development roadmap

## Completed: CMS baseline (phases 1–13)

- CMS-only public shell, administration, installation and starter content.
- Authenticated article, publishing, SEO, access control, navigation, media and theme workflows.
- Physical extraction of ten commerce modules, 29 commerce admin controllers, eight storefront controllers, account commerce actions, residual view components and commerce presentation helpers.
- Core leaf service extraction, stock subscription and storefront variant binding removal. Legacy GDPR handling remains intact.
- Last verified baseline: release build with zero errors and warnings, 2,104 tests passed, one skipped, zero failures; fresh SQLite and editorial smoke passed.

## Next

1. Remove remaining commerce-only admin models, assets and coupled product, checkout, order, rule, messaging and export services in dependency order.
2. Reduce compatibility database entities and schema using incremental, tested migrations.
3. Implement dedicated article taxonomy, editorial tools, publishing and CMS-specific search.
4. Complete project/application branding, package and namespace renaming once dependency cleanup is stable.
5. Prepare installation guidance, security review, release packaging and deployment documentation.

## Working rules

- Develop on `main` only; no phase branches for routine work.
- Keep all retained CMS features working and run the full CI suite after every coherent change.
- Preserve required license and copyright notices. Do not make production database changes as part of extraction.
