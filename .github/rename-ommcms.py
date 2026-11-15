"""One-time repository-wide identity change for OmmCMS."""

import collections
import pathlib
import re
import subprocess


PROTECTED = re.compile(
    r"Smartstore\.(?:Licensing|EmbeddedSass\.Net\.Compiler|LightningCss\.Native)"
    r"|Smartstore\s+(?:AG|contributors|Community Edition)"
    r"|https?://github\.com/smartstore/Smartstore[^\s\"'<>]*"
    r"|https?://[^\s\"'<>]*smartstore\.com[^\s\"'<>]*",
    re.IGNORECASE,
)
NAME = re.compile(r"smartstore", re.IGNORECASE)


def replace_name(match):
    word = match.group()
    if word.isupper():
        return "OMMCMS"
    if word.islower():
        return "ommcms"
    return "OmmCMS"


def rename_text(text):
    # Keep exact external package identities and genuine upstream attribution.
    result = []
    start = 0
    for match in PROTECTED.finditer(text):
        result.append(NAME.sub(replace_name, text[start:match.start()]))
        result.append(match.group())
        start = match.end()
    result.append(NAME.sub(replace_name, text[start:]))
    return "".join(result)


def skip_content(path):
    name = path.name.lower()
    return (
        path.as_posix() in {"LICENSE", "NOTICE.md", ".github/rename-ommcms.py", ".github/workflows/ommcms-rename.yml"}
        or (path.suffix.lower() in {"", ".md", ".txt"} and name.startswith(("license", "licence", "notice", "copyright", "third-party", "thirdparty")))
    )


files = [pathlib.Path(p) for p in subprocess.check_output(["git", "ls-files", "-z"]).decode().split("\0") if p]
moves = {path: pathlib.Path(NAME.sub(replace_name, path.as_posix())) for path in files}
collisions = [name for name, count in collections.Counter(str(p).casefold() for p in moves.values()).items() if count > 1]
if collisions:
    raise RuntimeError(f"Case-insensitive rename collisions: {collisions}")

changed = 0
renamed = 0
for source, target in moves.items():
    if not source.is_file():
        raise RuntimeError(f"Missing tracked file: {source}")
    if not skip_content(source):
        raw = source.read_bytes()
        if b"\0" not in raw:
            encoding = "utf-8"
            try:
                original = raw.decode(encoding)
            except UnicodeDecodeError:
                if source.suffix.lower() in {".cs", ".cshtml", ".js", ".css", ".scss", ".xml", ".txt", ".md", ".json", ".props", ".targets", ".csproj", ".sln"}:
                    encoding = "latin-1"
                    original = raw.decode(encoding)
                else:
                    original = None
            if original is not None:
                updated = rename_text(original)
                if source.as_posix() == "src/Smartstore.Web/appsettings.json":
                    # The former profile and application sections must be one JSON object.
                    preview = '  "OmmCMS": {\n    // OmmCMS is CMS-only from Phase 6. The host pins this setting to true.\n    "PreviewShell": true\n  },\n'
                    normalized = updated.replace("\r\n", "\n")
                    if normalized.count(preview) != 1:
                        raise RuntimeError("Could not identify the legacy profile section")
                    normalized = normalized.replace(preview, "", 1)
                    normalized = normalized.replace('  "OmmCMS": {\n', '  "OmmCMS": {\n    "PreviewShell": true,\n', 1)
                    updated = normalized.replace("\n", "\r\n") if "\r\n" in original else normalized
                if source.as_posix() == "AGENTS.md":
                    updated = updated.replace(
                        "Internal assembly, namespace and project-file identities are retained for compatibility until the dedicated rename milestone.",
                        "All first-party projects, assemblies, namespaces and paths use the OmmCMS identity. Preserve third-party package names and upstream licence attribution.",
                    )
                if source.as_posix() == "README.md":
                    updated = updated.replace("Older OmmCMS and OmmCMS preview databases", "Older upstream and OmmCMS preview databases")
                    updated = updated.replace("then reduce the database schema and complete internal namespace and assembly renaming.", "then reduce the database schema and complete dependency removal.")
                if original != updated:
                    source.write_bytes(updated.encode(encoding))
                    changed += 1
    if target != source:
        if target.exists():
            raise RuntimeError(f"Destination already exists: {target}")
        target.parent.mkdir(parents=True, exist_ok=True)
        source.rename(target)
        renamed += 1

# Remove only now-empty directories belonging to renamed tracked paths.
old_directories = {parent for source, target in moves.items() if source != target for parent in source.parents if NAME.search(parent.as_posix())}
for directory in sorted(old_directories, key=lambda p: len(p.parts), reverse=True):
    try:
        directory.rmdir()
    except OSError:
        pass

icon = pathlib.Path("src/OmmCMS.Web/App_Data/Icons/system/fill/icons/ommcms.svg")
icon.write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><circle cx="32" cy="32" r="20" fill="none" stroke="currentColor" stroke-width="8"/></svg>\n', encoding="utf-8")

# OmmCMS does not support proprietary Smartstore modules or their activation service.
# Remove the upstream commercial-module UI/API rather than emulating license checks.
admin = pathlib.Path("src/OmmCMS.Web/Areas/Admin")
controller = admin / "Controllers/ModuleController.cs"
source = controller.read_text(encoding="utf-8-sig")
source = source.replace("using Smartstore.Licensing;\n", "").replace("using OmmCMS.Utilities.Html;\n", "")
source = source.replace("            // License label\n            await PrepareLicenseLabelModelAsync(model.LicenseLabel, descriptor);\n", "")
assert source.count("    #region Licensing") == 1
source = source.split("    #region Licensing")[0].rstrip() + "\n}\n"
controller.write_text(source, encoding="utf-8-sig")
model = admin / "Models/Modularity/ModuleModel.cs"
model.write_text(model.read_text(encoding="utf-8-sig").replace("    public LicenseLabelModel LicenseLabel { get; set; } = new();\n", ""), encoding="utf-8-sig")
view = admin / "Views/Module/List.cshtml"
source = view.read_text(encoding="utf-8-sig")
patterns = [
    r'<modal id="module-licence-modal".*?</modal>\s*',
    r'(?ms)^\s*// Licence module.*?(?=^\s*\$\(\x27#btnRefresh\x27\)\.click)',
    r'<!-- License badge -->\s*<div class="license-label">.*?</div>\s*',
    r'<a sm-if="module\.LicenseLabel\.LicenseUrl\.HasValue\(\)".*?</a>\s*',
]
for pattern in patterns:
    source, count = re.subn(pattern, "", source, flags=re.DOTALL)
    assert count == 1, (pattern, count)
view.write_text(source, encoding="utf-8-sig")
project = pathlib.Path("src/OmmCMS.Web.Common/OmmCMS.Web.Common.csproj")
source = project.read_text(encoding="utf-8-sig")
source, count = re.subn(r'(?m)^\s*<PackageReference Include="Smartstore\.Licensing"[^\n]*\n', "\n", source)
assert count == 1
project.write_text(source, encoding="utf-8-sig")
removed = [
    admin / "Models/Modularity/LicenseModuleModel.cs",
    admin / "Views/Module/LicenseModule.cshtml",
    admin / "Views/Module/Partials/LicenseLabel.cshtml",
]
for path in removed:
    path.unlink()
license_path = pathlib.Path("src/OmmCMS/sixlabors.lic")
license_path.write_bytes(pathlib.Path(".github/ommcms-sixlabors.lic").read_bytes())

# Keep the search fixture's partial-term matches consistent with the renamed data.
fixture = pathlib.Path("test/OmmCMS.Core.Tests/Catalog/Search/LinqCatalogSearchServiceTests.cs")
source = fixture.read_text(encoding="utf-8-sig")
source = source.replace('[TestCase(new int[] { 1, 2, 4 }, "Smart")]', '[TestCase(new int[] { 1, 2, 4 }, "Omm")]')
source = source.replace('[TestCase(new int[] { 1, 2, 4, 5 }, "Smart", SearchMode.Contains, 1)]', '[TestCase(new int[] { 1, 2, 4, 5 }, "Omm", SearchMode.Contains, 1)]')
source = source.replace('new SearchProduct(2) { Name = "Apple iPhone Smartphone 6" },', 'new SearchProduct(2) { Name = "Apple iPhone Smartphone 6 for OmmCMS" },')
fixture.write_text(source, encoding="utf-8-sig")

# Force-add exactly the renamed tracked paths, even if they live under build ignore rules.
subprocess.run(["git", "add", "-u"], check=True)
destinations = [str(target) for source, target in moves.items() if source != target and target.is_file()]
for offset in range(0, len(destinations), 100):
    subprocess.run(["git", "add", "-f", "--", *destinations[offset:offset + 100]], check=True)
print(f"OmmCMS identity migration: {changed} text files updated, {renamed} tracked files renamed")
remaining = [p for p in subprocess.check_output(["git", "ls-files", "-z"]).decode().split("\0") if NAME.search(p)]
if remaining:
    raise RuntimeError(f"Old-name tracked paths remain: {remaining[:20]}")
