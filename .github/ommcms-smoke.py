"""Fresh CMS installation against a disposable published application."""
import base64
import http.cookiejar
from html.parser import HTMLParser
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

root = Path(sys.argv[1]).resolve()
assert (root / "Smartstore.Web.dll").is_file(), "Use a published application"
database = root / "App_Data" / "Tenants" / "Default" / "ommcms-smoke.db"
assert not database.exists(), "Smoke test needs a new database"
base = "http://127.0.0.1:5087"
env = dict(os.environ, ASPNETCORE_URLS=base, ASPNETCORE_ENVIRONMENT="Production",
           OmmCMS__PreviewShell="true")
log = root / "ommcms-smoke.log"
process = None


def browser():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


anonymous = browser()


def request(path, data=None, timeout=30, client=anonymous, headers=None):
    payload = data if isinstance(data, bytes) else (urllib.parse.urlencode(data, doseq=True).encode() if data is not None else None)
    req = urllib.request.Request(base + path, data=payload, headers=headers or {})
    try:
        with client.open(req, timeout=timeout) as response:
            return response.status, response.read().decode(errors="replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode(errors="replace")


def start():
    global process
    with log.open("a", encoding="utf-8") as output:
        process = subprocess.Popen(["dotnet", "Smartstore.Web.dll"], cwd=root,
                                   env=env, stdout=output, stderr=subprocess.STDOUT)


def stop():
    global process
    if process is not None:
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=30)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=10)
        process = None


def wait_for(path, timeout=180):
    until = time.monotonic() + timeout
    while time.monotonic() < until:
        if process.poll() is not None:
            raise RuntimeError(f"Application exited: {process.returncode}")
        try:
            status, body = request(path, timeout=10)
            if status == 200:
                return body
        except (OSError, TimeoutError):
            pass
        time.sleep(2)
    raise RuntimeError(f"Application did not become ready: {path}")



class Inputs(HTMLParser):
    def __init__(self, html):
        super().__init__()
        self.values = {}
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        if tag == "input":
            attrs = dict(attrs)
            if attrs.get("name") and attrs.get("type") == "hidden":
                self.values.setdefault(attrs["name"], attrs.get("value", ""))


def read_rows(sql, args=()):
    with sqlite3.connect(database.as_uri() + "?mode=ro", uri=True) as connection:
        return connection.execute(sql, args).fetchall()


def get_page(path, client, marker=None):
    status, body = request(path, client=client)
    assert status == 200, f"{path}: HTTP {status}"
    if marker:
        assert marker in body, f"{path}: missing expected page marker {marker}"
    return body


def post_form(path, client, page, data):
    token = Inputs(page).values.get("__RequestVerificationToken")
    assert token, f"{path}: antiforgery token missing"
    status, body = request(path, dict(data, __RequestVerificationToken=token), client=client)
    assert status == 200, f"{path}: HTTP {status}"
    return body


def editorial_workflows(password):
    # Never reuse a real account or database: everything below is the fresh smoke fixture.
    admin = browser()
    page = get_page("/admin/topic/create", anonymous)
    assert 'name="CustomerLoginType"' in page, "Anonymous admin access did not require sign-in"
    page = get_page("/login?returnUrl=%2Fadmin%2Ftopic%2Flist", admin, 'name="CustomerLoginType"')
    login_type = Inputs(page).values["CustomerLoginType"]
    post_form("/login?returnUrl=%2Fadmin%2Ftopic%2Flist", admin, page, {
        "CustomerLoginType": login_type,
        "UsernameOrEmail": "admin@ommcms.test",
        "Email": "admin@ommcms.test",
        "Username": "admin@ommcms.test",
        "Password": password,
        "RememberMe": "false",
    })
    page = get_page("/admin/topic/create", admin, 'id="topic-form"')
    print("Authenticated admin access passed.")

    key = "Phase5SmokeArticle"
    slug = "phase5-smoke-article"
    title = "Phase 5 smoke article"
    marker = "OmmCMS editorial draft verifiable text"
    fields = {
        "SystemName": key, "Title": title, "SeName": slug,
        "Body": "<p>" + marker + "</p>", "MetaTitle": title,
        "MetaDescription": "Disposable editorial regression fixture.",
        "IsPublished": "false", "RenderAsWidget": "false",
        "IncludeInSitemap": "true", "TitleTag": "h1", "save": "save",
    }
    # The live endpoint must enforce antiforgery, even for an authenticated editor.
    status, _ = request("/admin/topic/create", fields, client=admin)
    assert status in (400, 403), f"Missing CSRF token accepted: HTTP {status}"
    assert not read_rows("SELECT Id FROM Topic WHERE SystemName = ?", (key,))
    post_form("/admin/topic/create", admin, page, fields)
    rows = read_rows("SELECT Id, IsPublished FROM Topic WHERE SystemName = ?", (key,))
    assert len(rows) == 1 and rows[0][1] == 0, "Draft not persisted"
    topic_id = rows[0][0]
    assert request("/" + slug, client=anonymous)[0] == 404, "Draft is publicly visible"
    page = get_page(f"/admin/topic/edit/{topic_id}", admin, 'id="topic-form"')
    fields.update(Id=str(topic_id), IsPublished="true", Body="<p>" + marker + " published</p>")
    post_form(f"/admin/topic/edit/{topic_id}", admin, page, fields)
    assert read_rows("SELECT IsPublished FROM Topic WHERE Id = ?", (topic_id,)) == [(1,)]
    get_page("/" + slug, anonymous, marker + " published")
    print("Article draft, publication, public slug and CSRF protection passed.")

    # Verify access controls on the same article using the seeded Administrators role.
    role = read_rows("SELECT Id FROM CustomerRole WHERE SystemName = 'Administrators'")
    assert len(role) == 1, "Administrator role missing"
    page = get_page(f"/admin/topic/edit/{topic_id}", admin, 'id="topic-form"')
    restricted = dict(fields, SubjectToAcl="true", SelectedCustomerRoleIds=str(role[0][0]))
    post_form(f"/admin/topic/edit/{topic_id}", admin, page, restricted)
    assert request("/" + slug, client=anonymous)[0] == 404, "Restricted article visible to guest"
    get_page("/" + slug, admin, marker + " published")
    page = get_page(f"/admin/topic/edit/{topic_id}", admin, 'id="topic-form"')
    post_form(f"/admin/topic/edit/{topic_id}", admin, page, fields)
    get_page("/" + slug, anonymous, marker + " published")
    print("Article role restriction and restoration passed.")

    # Create a real navigation item on the starter Main menu and test live rendering.
    menu_id = read_rows("SELECT Id FROM MenuRecord WHERE SystemName = 'Main'")[0][0]
    assert "createitem" in get_page(f"/admin/menu/edit/{menu_id}", admin).lower(), "Menu item creation link missing"
    blocked = f"/admin/menu/createitem?providerName=catalog&menuId={menu_id}&parentItemId=0"
    assert request(blocked, client=admin)[0] == 404, "Catalog menu editor exposed"
    create_item = f"/admin/menu/createitem?providerName=entity&menuId={menu_id}&parentItemId=0"
    page = get_page(create_item, admin)
    item_title = "Editorial smoke navigation"
    item_fields = {
        "MenuId": menu_id, "ParentItemId": 0, "ProviderName": "entity",
        "Model": "topic:" + key, "Title": item_title, "Published": "true",
        "DisplayOrder": 20, "save": "save",
    }
    post_form("/admin/menu/createitem", admin, page, item_fields)
    items = read_rows("SELECT Id FROM MenuItemRecord WHERE MenuId = ? AND Title = ?", (menu_id, item_title))
    assert len(items) == 1, "Menu item not persisted"
    get_page("/", anonymous, item_title)
    item_id = items[0][0]
    page = get_page(f"/admin/menu/edititem/{item_id}", admin)
    item_fields.update(Id=item_id, Title=item_title + " updated")
    post_form(f"/admin/menu/edititem/{item_id}", admin, page, item_fields)
    assert read_rows("SELECT Title FROM MenuItemRecord WHERE Id = ?", (item_id,)) == [(item_title + " updated",)]
    get_page("/", anonymous, item_title + " updated")
    print("Content navigation create, edit and catalog provider isolation passed.")

    # Upload a real tiny PNG to the standard file album using the normal media endpoint.
    page = get_page(f"/admin/topic/edit/{topic_id}", admin)
    token = Inputs(page).values["__RequestVerificationToken"]
    name = "ommcms-editorial-smoke.png"
    png = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAIAAABLbSncAAAAFElEQVR4nGO0adrCgA0wYRUdtBIAJMsBgh5OrpoAAAAASUVORK5CYII=")
    boundary = "----OmmCmsSmoke" + uuid.uuid4().hex
    parts = []
    for field, value in {"path": "file", "__RequestVerificationToken": token}.items():
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"\r\n\r\n{value}\r\n'.encode())
    parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{name}"\r\nContent-Type: image/png\r\n\r\n'.encode() + png + b"\r\n")
    parts.append(f"--{boundary}--\r\n".encode())
    status, result = request("/admin/media/upload", b"".join(parts), client=admin,
                             headers={"Content-Type": "multipart/form-data; boundary=" + boundary})
    assert status == 200, f"Media upload returned HTTP {status}"
    media = json.loads(result)
    assert isinstance(media, dict) and media.get("success"), "Media upload did not succeed"
    assert read_rows("SELECT Id FROM MediaFile WHERE Name = ? AND Deleted = 0", (name,)), "Media metadata missing"
    media_url = urllib.parse.urljoin(base + "/", media["url"])
    assert urllib.parse.urlsplit(media_url).netloc == urllib.parse.urlsplit(base).netloc
    with anonymous.open(media_url, timeout=30) as response:
        assert response.status == 200 and response.read().startswith(png[:8]), "Uploaded image not publicly readable"
    print("Authenticated media upload and public retrieval passed.")

    # Check actual theme settings and configuration UI, then persist a different bundled theme.
    store_id = read_rows("SELECT Id FROM Store")[0][0]
    page = get_page("/admin/theme/list", admin, 'name="DefaultTheme"')
    original_theme = Inputs(page).values["DefaultTheme"]
    replacement = "FlexBlue" if original_theme.lower() != "flexblue" else "Flex"
    get_page(f"/admin/theme/configure?theme={original_theme}&storeId={store_id}", admin, 'name="theme"')
    post_form("/admin/theme/list", admin, page, {
        "StoreId": store_id, "DefaultTheme": replacement,
        "BundleOptimizationEnabled": 0, "AssetCachingEnabled": 0,
        "AllowCustomerToSelectTheme": "false", "SaveThemeChoiceInCookie": "false",
        "KeepSassCompilerInMemory": "false", "save": "save",
    })
    assert Inputs(get_page("/admin/theme/list", admin)).values["DefaultTheme"] == replacement
    get_page("/", anonymous, "Welcome to OmmCMS")
    print("Theme configuration and theme switch passed.")


try:
    start()
    page = wait_for("/install?noAutoInstall=true")
    assert "OmmCMS" in page, "CMS installer branding missing"
    assert 'name="InstallSampleData"' not in page, "Shop sample switch displayed"
    password = "Smoke-" + uuid.uuid4().hex + "!9"
    status, body = request("/install/Install", {
        "AdminEmail": "admin@ommcms.test",
        "AdminPassword": password,
        "ConfirmPassword": password,
        "DataProvider": "sqlite",
        "PrimaryLanguage": "en-US",
        "MediaStorage": "fs",
        "CreateDatabase": "true",
        "DbName": "ommcms-smoke",
        "InstallSampleData": "true",
    }, timeout=300)
    assert status == 200, f"Installer HTTP {status}"
    result = {key.lower(): value for key, value in json.loads(body).items()}
    assert result.get("success"), f"Installation failed: {result.get('errors')}"
    stop()
    assert database.is_file(), "SQLite database not found"
    with sqlite3.connect(database.as_uri() + "?mode=ro", uri=True) as connection:
        def rows(sql):
            return [row[0] for row in connection.execute(sql)]

        assert rows("SELECT Name FROM Store") == ["OmmCMS"]
        assert rows("SELECT COUNT(*) FROM Product") == [0]
        assert rows("SELECT COUNT(*) FROM Category") == [0]
        topics = set(rows("SELECT SystemName FROM Topic"))
        assert {"HomePageText", "AboutUs", "ContactUs"} <= topics
        assert not topics.intersection({"PaymentInfo", "ShippingInfo", "CheckoutAsGuestOrRegister"})
        assert "catalog" not in rows("SELECT ProviderName FROM MenuItemRecord")
        tasks = set(rows("SELECT Type FROM ScheduleTask"))
        assert "QueuedMessagesSendTask" in tasks
        assert not tasks.intersection({"ProductRuleEvaluatorTask", "UpdateExchangeRateTask"})
    print("CMS installer and SQLite starter content passed.")
    start()
    page = wait_for("/")
    assert "Welcome to OmmCMS" in page, "CMS home page missing"
    for path in ("/about", "/contactus", "/login"):
        status, _ = request(path)
        assert status == 200, f"{path}: expected HTTP 200, got {status}"
        print(f"{path}: {status}")
    for path in ("/cart", "/checkout", "/admin/product/list", "/odata/Products"):
        status, _ = request(path)
        assert status == 404, f"{path}: expected HTTP 404, got {status}"
        print(f"{path}: {status}")
    print("CMS fresh-install HTTP smoke test passed.")
    if "--editorial" in sys.argv:
        editorial_workflows(password)
        print("CMS authenticated editorial workflow smoke test passed.")
except Exception:
    if log.exists():
        print(log.read_text(errors="replace")[-25000:], file=sys.stderr)
    raise
finally:
    stop()
