# Phase 13 — Core stock subscription and variant binding extraction

Base: Phase 12 (`cms-commerce-presentation-extraction`), validated by GitHub Actions run 43692016739: 2,099 passed, one skipped, zero failures; release build and editorial smoke passed.

- Remove the back-in-stock subscription service, its interface and detached query helper. Disconnect its notification side effect from the product save hook while retaining inventory and legacy entity integrity processing.
- Remove the storefront product variant HTTP query factory and MVC model binder. Retain the variant query domain model used by compatibility services.
- Remove the corresponding DI registrations and add assembly/source absence regression checks. Carry forward all previous CMS smoke assertions.
- Retain schema, migrations, CMS messaging, identity, content, media, menus, themes and generic infrastructure. No production changes.

Validation: pending CI on the phase branch. Subsequent extraction must separate remaining product, checkout, commerce rules, export and message model dependencies before schema reduction.
