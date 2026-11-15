# OmmCMS development instructions

- Work directly on `main`; keep changes focused and validated.
- OmmCMS is a CMS, not a storefront. Preserve content publishing, SEO, menus, themes, media, identity and extension infrastructure.
- The solution is `OmmCMS.sln`. Internal assembly, namespace and project-file identities are retained for compatibility until the dedicated rename milestone.
- Use .NET 10. Run restore, release build, tests and the disposable SQLite smoke test defined in `.github/workflows/ommcms-ci.yml`.
- Never modify generated module output under `src/Smartstore.Web/Modules/`; source is under `src/Smartstore.Modules/`.
- Preserve existing licence notices, copyright, XML documentation and source formatting; use English in code and comments.
- Use constructor injection, preserve lazy initialization, guard arguments, and use asynchronous IO.
- Database changes use FluentMigrator, never EF Core migrations. Do not change production databases as part of extraction.
- Keep assets and locale resources in their existing source locations. Preserve common infrastructure, mandatory build configuration, and component-specific notices.
- No Docker, Nuke, upstream release automation or extra documentation trees. Keep the README short and update its current status as development advances.
