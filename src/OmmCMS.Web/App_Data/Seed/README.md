# Installation seed data

This folder holds the data that the installer writes into a new CMS database:
mandatory data (currencies, countries, settings, …) and CMS starter pages and menus.
The generic seed loader supports culture overlays; storefront sample data is not installed.

It replaces the seed data that used to live in C# (`InvariantSeedData`, `EnUSSeedData`,
`DeDESeedData` in `src/OmmCMS.Core/Platform/Installation`). C# only orchestrates the
installation (`InstallationDataSeeder`) and wires up database IDs; all data lives here.

## Folder layout

```
App_Data/Seed/
├── base/                         Base data, English, valid for every language
│   └── core/                     Mandatory data and CMS starter content
├── cultures/
│   └── {culture}/                e.g. de-DE
│       ├── culture.json          Makes the culture installable (see below)
│       ├── installation.xml      UI texts of the installer (optional)
│       ├── resources/            Locale resources (*.smres.xml, head.txt), see below
│       └── core/…                Overlays: same relative paths as in base/
└── media/                        Optional installation media
```

`base/core/settings.json` is the only object file: `{ "SeoSettings": { "MetaTitle": "Example" } }`.
It lists only values that differ from the defaults of the settings class. Values that hold
the ID of a seed record are written as `"$ref:{EntityType}:{key}"`, e.g. `"$ref:Currency:EUR"`.
Its overlay has the same shape and is merged field by field.

Settings that do not belong to a settings class, e.g. because their key contains an entity ID, go into the
special object `"SettingKeys"`. Placeholders `{$ref:{EntityType}:{key}}` in a key are replaced by the ID,
for example `{$ref:Currency:EUR}`.

- **Base files** are JSON **arrays** of records.
- **Overlay files** are JSON **objects** and live under the same relative path as their
  base file, e.g. `base/core/currencies.json` ↔ `cultures/de-DE/core/currencies.json`.
  An overlay without a base file is an error.
- Files starting with `_` (e.g. `_defaults.json`) are never read as record files.
- JSON may contain `//` comments and trailing commas.

## Keys

Every record has a **stable, language-independent key**. Keys are never translated and are
compared case-insensitively.

- Where the entity has a natural key, that field **is** the key
  (e.g. `twoLetterIsoCode`, `currencyCode`, `systemName`, `systemKeyword`).
- Otherwise the record has a synthetic `key` field in kebab-case ASCII,
  e.g. `"key": "default"`. It is not stored in the database.
- Which field is the key of an entity type is declared by its `SeedEntityDescriptor` in C#.
- Child records (e.g. the states of a country) are keyed within their parent and can be
  referenced as `{parentKey}/{childKey}`, e.g. `DE/BW`.

## References

Records refer to each other **by key only**, never by name or database ID. Reference fields
are named after the target without an `Id` suffix:

```jsonc
"country": "DE",
"stateProvince": "DE/BW"
```

The descriptor declares which fields are references. An unknown key fails the installation
with the file, record and field in the message.

## Overlay rules

An overlay object contains record keys (partial records) and directives:

| Entry | Effect |
|---|---|
| `"{key}": { … }` | Merged into the record with that key. An unknown key is an **error** (use `$add`). |
| `"$all": { … }` | Merged into every record, before the keyed patches. |
| `"$remove": [ "key", … ]` | Removes records. |
| `"$add": [ { … }, … ]` | Appends complete new records (after all other directives). |
| `"$mode": "replace"` + `"$items": [ … ]` | Drops all base records and uses `$items` instead. No keyed patches allowed. |
| `"$aliases": { "old": "new" }` | Lets references to `old` resolve to `new` (file level only, single hop). |

Merging inside a record:

- Nested objects are merged field by field.
- Declared **child collections** (e.g. `stateProvinces`) accept either an overlay object
  (merged by child key, same directives except `$aliases`) or an array (replaces the collection).
- All other values, including arrays of keys, are **replaced**.
- `null` sets a value to null.

Precedence, lowest first:

```
_defaults.json  →  base record  →  overlay $all  →  overlay keyed patch
```

A descriptor may declare a defaults file (e.g. `core/cms/_defaults.json`).
A culture can overlay the defaults file under the same path.

Example — an overlay translates the starter About page:

```jsonc
// cultures/de-DE/core/cms/topics.json
{
  "AboutUs": { "title": "Über uns" }
}
```

## Core and starter content

Every installation uses the core files and the selected culture overlay. The CMS installer
does not load legacy sample overlays or expose sample-data selection. The lower-level seed
loader's optional overlay mechanism remains available for extensions and tests.

## Long texts (`@file`)

A **top-level** string field with the value `"@file"` is replaced by the content of
`{jsonFileName}/{key}.{field}.html` next to the JSON file that sets the value:

```
base/core/cms/topics.json              "body": "@file"
base/core/cms/topics/AboutUs.body.html
```

- `"@file:relative/path.html"` names the file explicitly (relative to the JSON file).
- In an overlay, the file is looked up next to the overlay file. If the overlay does not set
  the field, the base text is used.
- Characters that are invalid in file names (`< > : " / \ | ? *`) are replaced by `_` in the key.
- `@file` in nested fields is an error.

Rule of thumb: HTML longer than one line or about 200 characters goes into its own file.
Format these files so that people can edit them: every block element (`<p>`, `<ul>`, `<li>`, ...) on its own line,
content indented by two spaces, one sentence per line. No one-line HTML.
Links between starter pages should use their registered slugs, e.g.
`<a href="/about">About us</a>`. Topic descriptors accept the `slug` override.

## Locale resources

`cultures/{culture}/resources/` holds the complete UI resources of a language: one or more
`*.smres.xml` files and `head.txt`. `head.txt` contains the version of the newest migration whose
resources are included; resources of newer migrations are seeded on top during installation.

The installer imports these files **directly after creating the language** and before any
other data, so that seed records can reference resources (see below).

> **Release process:** the resource files are updated for each release. They used to live in
> `App_Data/Localization/App/{de|en}` and now live here.

`cultures/{culture}/installation.xml` holds the texts of the installation wizard itself. A culture
without it can still be installed, but the wizard is not offered in that language. The language
code of the wizard is `uniqueSeoCode` from `culture.json`. These texts used to live in
`App_Data/Localization/Installation/installation.{code}.xml`.

## Resource references

A string value can reference a locale resource of the installed language:

```jsonc
"title": "@resource:ContactUs",             // whole value
"title": "Footer - {@resource:Footer.Info}"       // embedded, any number of times
```

References are resolved when the record is created, in any field and in child records. An
unknown key fails the installation. Resolving fails as well if the resources have not been
seeded yet; the installer guarantees the order.

## Further value conventions

| Convention | Example | Meaning |
|---|---|---|
| Relative date | `"createdOnUtc": "@now-1d"` | Installation time plus/minus months (`M`) or days (`d`) for supported date fields. |
| Media reference | `"logo.png"` or `{ "file": "logo.png", "name": "website-logo" }` | Path below `media/`, used by descriptor-bound media fields such as `logoMediaFile`. |
| Topic slug | `"slug": "about"` | Stable public URL for a starter topic. |
| Menu item | `"providerName": "entity", "model": "topic:AboutUs"` | References an installed CMS topic from a navigation menu. |
| Icon | `"icon": "home"` | Concept name from the public icon kit (`App_Data/Icons/kits.json`), so that the icon comes from the shared sprite. |

## Languages

A culture is installable when `cultures/{culture}/culture.json` exists:

```json
{ "name": "Deutsch", "uniqueSeoCode": "de", "flagImageFileName": "de.png" }
```

The culture code is the folder name. Only one language is installed initially per website: the overlays
of that culture are applied on top of the base data.

### Adding a language

1. Create `cultures/{culture}/culture.json`.
2. Put the locale resources into `cultures/{culture}/resources/` (`*.smres.xml` and `head.txt`).
   Optionally add `cultures/{culture}/installation.xml` to offer the installation wizard in that language.
3. Add overlays for the files that need translation, mirroring the paths in `base/`.
   Only list the fields that differ.
4. Run the seed tests (see below).

### Adding or changing a record

1. Edit the base file. Give a new record a key following the rules above.
2. Add translations to the overlays of every culture that translates that file.
3. Run the seed tests.

## Validation and tests

The loader fails loudly on unknown fields, unknown overlay keys, duplicate keys,
unresolvable references and missing `@file` files. Messages name
`file → record → field`.

Tests in `test/OmmCMS.Core.Tests/Platform/Installation`:

| Test | Purpose |
|---|---|
| `Seeding/*Tests` | Unit tests of merger, data source, factory and resolver. |
| `InstallationSeedingTests` | Runs the real installation seeder for every culture against SQLite and verifies CMS-only starter content, even when the legacy sample request is supplied. |
| `InstallationServiceLanguageTests` | Reads the installer and app languages from this folder. |

The tests do not know concrete values or counts, so you can change seed data without changing a test.
They fail only if something is broken. Whether the data itself is right is a matter of reviewing the diff of these files.

## Data files

In installation order. OmmCMS installs only core and CMS starter data; storefront samples are no longer installed.

| Area | Files |
|---|---|
| Locale resources | `cultures/{culture}/resources/*` |
| Currencies, store | `core/currencies.json`, `core/cms/stores.json` |
| Countries and states | `core/countries.json` |
| Customer roles, admin address, system customers | `core/customer-roles.json`, `core/admin-address.json`, `core/system-customers.json` |
| Email accounts, topics (with `slug` overrides) | `core/email-accounts.json`, `core/cms/topics.json` |
| Settings (`$ref` values) | `core/settings.json`, `core/cms/settings.json` |
| Activity log types, scheduled tasks | `core/activity-log-types.json`, `core/tasks.json` |
| Menus (`@resource` titles) | `core/cms/menus.json` |

Message templates are not part of this folder; they are imported from `App_Data/EmailTemplates`.
Descriptors (files, keys, references) are defined in
`src/OmmCMS.Core/Platform/Installation/Seeding/InstallationSeedDescriptors.cs`.

## Rules for AI agents

- Add CMS starter content to the `core/cms/` files; do not reintroduce catalogue samples.
- Never translate or rename keys. They are referenced from other files and cultures.
- Put translations only into `cultures/{culture}/…`. Base files are English.
- Do not add a field to an overlay that is not in the base record unless the entity really
  has that property; unknown fields fail the installation.
- Prefer `$all`, `$add`, `$remove` over copying whole files with `$mode: "replace"`.
- After every change, run
  `dotnet test test/OmmCMS.Core.Tests --filter "FullyQualifiedName~Installation"`.
- Do not add tests that assert concrete seed values or record counts.
- Sample images and downloads live in `media/`. Reference them by their path below `media/`.
  Every media reference creates its own media file.
