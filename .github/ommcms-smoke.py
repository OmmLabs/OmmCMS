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
assert (root / "OmmCMS.Web.dll").is_file(), "Use a published application"
database = root / "App_Data" / "Tenants" / "Default" / "ommcms-smoke.db"
assert not database.exists(), "Smoke test needs a new database"
base = "http://127.0.0.1:5087"
env = dict(os.environ, ASPNETCORE_URLS=base, ASPNETCORE_ENVIRONMENT="Production")
# An old false setting must not reactivate commerce in the trimmed distribution.
env["OmmCMS__PreviewShell"] = "false" if "--trimmed" in sys.argv else "true"
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


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def assert_permanent_redirect(source, target):
    opener = urllib.request.build_opener(NoRedirect())
    try:
        with opener.open(base + source, timeout=30) as response:
            raise AssertionError(f"{source}: expected HTTP 301, got {response.status}")
    except urllib.error.HTTPError as error:
        assert error.code == 301, f"{source}: expected HTTP 301, got {error.code}"
        location = error.headers["Location"]
        actual = urllib.parse.urlsplit(location).path
        assert actual.rstrip("/") == target.rstrip("/"), f"{source}: redirects to {actual}, expected {target}"
        # Follow the exact canonical URL emitted by the application, without allowing a second redirect.
        with opener.open(urllib.parse.urljoin(base, location), timeout=30) as response:
            assert response.status == 200, f"{source}: redirect destination returned {response.status}"


def start():
    global process
    with log.open("a", encoding="utf-8") as output:
        process = subprocess.Popen(["dotnet", "OmmCMS.Web.dll"], cwd=root,
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
    media_settings = get_page("/admin/media/mediasettings", admin, "PreviewThumbnailSize")
    assert all(name not in media_settings for name in ("ProductThumbPictureSize", "ProductDetailsPictureSize", "CartThumbPictureSize", "ManufacturerThumbPictureSize")), "Retired media controls remain"
    customer_settings = get_page("/admin/customer/customerusersettings", admin, "FullNameOnContactUsRequired")
    assert all(name not in customer_settings for name in ("HideMyAccountOrders", "HideDownloadableProductsTab", "FullNameOnProductRequestRequired")), "Retired account controls remain"
    general_settings = get_page("/admin/setting/generalcommon", admin, "ConvertNonWesternChars")
    assert "ProductDescriptionPriority" not in general_settings, "Retired SEO control remains"
    performance_settings = get_page("/admin/maintenance/performancesettings", admin, "MediaDupeDetectorMaxCacheSize")
    assert "MaxUnavailableAttributeCombinations" not in performance_settings, "Retired product variant performance setting remains"
    print("CMS media, account and SEO settings render without commerce controls.")
    if "--presentation-trimmed" in sys.argv:
        for path in ("/pdf/receiptheader", "/pdf/receiptfooter"):
            assert request(path, client=admin)[0] == 404, f"Retired receipt endpoint reachable: {path}"
    if "--residual-commerce-trimmed" in sys.argv:
        for path in ("/currency-selected/1", "/admin/customer/reports", "/admin/customer/rewardpointssettings", "/admin/customer/rewardpointsforpurchasesinfo"):
            assert request(path, client=admin)[0] == 404, f"Residual commerce action reachable: {path}"
        for entity_type in ("product", "category", "manufacturer"):
            assert request("/entity/picker?entityType=" + entity_type, client=admin)[0] == 404
        picker = get_page("/entity/picker?entityType=customer", admin, "CustomerSearchType")
        assert 'name="DisableIf"' not in picker and 'name="DisableIds"' in picker
        picker_status, picker_js = request("/js/ommcms.entitypicker.js")
        assert picker_status == 200 and "entityType: 'customer'" in picker_js and "disableIf" not in picker_js
        print("Residual commerce routes absent; authorized customer picker remains available.")
    analytics_settings = {name.lower() for (name,) in read_rows("SELECT Name FROM Setting WHERE lower(Name) LIKE 'googleanalyticssettings.%'")}
    assert "googleanalyticssettings.googleid" in analytics_settings
    assert "googleanalyticssettings.trackingscript" in analytics_settings
    assert "googleanalyticssettings.renderwithuserconsentonly" in analytics_settings
    assert not any(name in analytics_settings for name in ("googleanalyticssettings.ecommercescript", "googleanalyticssettings.ecommercedetailscript", "googleanalyticssettings.rendercatalogscripts", "googleanalyticssettings.rendercheckoutscripts", "googleanalyticssettings.itemidentifier"))
    analytics = get_page("/admin/googleanalytics/configure", admin, "RenderWithUserConsentOnly")
    assert "TrackingScript" in analytics and "{ECOMMERCE}" not in analytics
    print("CMS Google Analytics configuration and consent controls passed without commerce settings.")
    resources = {name.lower() for (name,) in read_rows("SELECT ResourceName FROM LocaleStringResource")}
    assert all(name.lower() not in resources for name in ("Admin.Configuration.PriceLabels", "Admin.Configuration.Tax", "Admin.Customers.Customers.Orders", "Admin.Product.Picture.Added", "Admin.DataExchange.Export.Filter.PriceMaximum"))
    assert "admin.dataexchange.export.filter.isactivecustomer" in resources
    assert "admin.dataexchange.export.filter.isactivesubscriber" in resources
    assert not any(name.startswith(("admin.dataexchange.export.exportrelateddata", "admin.dataexchange.import.importrelateddata", "enums.relatedentitytype.")) for name in resources)
    for table, retained, retired in (("ImportProfile", "EntityTypeId", "ImportRelatedData"), ("ExportProfile", "ProviderSystemName", "ExportRelatedData")):
        columns = {row[1] for row in read_rows(f"PRAGMA table_info({table})")}
        assert {"Id", retained} <= columns and retired not in columns, f"Unexpected {table} schema: {columns}"
    assert not any(name.startswith(("payment.", "pdfinvoice.", "pdfpackagingslip.", "pdfproductcatalog.", "returnrequests.", "tax.", "shipment.", "shipping.", "enums.producttype.", "enums.shoppingcarttype.", "enums.orderstatus.")) for name in resources)
    assert {"common.navigation.more", "common.company.email", "common.company.phone", "common.company.fax", "common.company.taxnumber", "common.company.vatid", "common.company.commercialregister", "search.facet.customer", "search.facet.date", "enums.rulescope.customer"} <= resources
    assert "catalogmenu.morelink" not in resources
    assert {"ommcms.ai.prompts.role.htmleditor", "ommcms.ai.prompts.role.imageanalyzer", "ommcms.ai.prompts.role.seoexpert", "ommcms.ai.prompts.dontcreatetitle", "admin.rules.execute.matchcustomers", "admin.rules.filterdescriptor.isincustomerrole"} <= resources
    assert not {"ommcms.ai.prompts.role.productexpert", "ommcms.ai.prompts.role.salesperson", "admin.rules.execute.matchproducts", "admin.rules.filterdescriptor.ordertotal", "admin.dataexchange.import.numberofpictures"} & resources
    print("Fresh locale resources retain CMS export fields without retired commerce labels.")
    if "--admin-commerce-trimmed" in sys.argv:
        for controller, action in (("campaign","list"), ("category","list"), ("currency","list"), ("discount","list"), ("export","list"), ("import","list"), ("manufacturer","list"), ("measure","weights"), ("order","list"), ("product","list"), ("search","searchsettings")):
            path = f"/admin/{controller}/{action}"
            assert request(path, client=admin)[0] == 404, f"Retired admin commerce route reachable: {path}"
        admin_id = read_rows("SELECT Id FROM Customer WHERE Email = ?", ("admin@ommcms.test",))[0][0]
        get_page("/admin/customer/list", admin)
        customer = get_page(f"/admin/customer/edit/{admin_id}", admin, 'id="customer-form"')
        assert "/admin/order/orderlist" not in customer.lower(), "Removed order grid still linked"
        assert "/admin/customer/getcartlist" not in customer.lower(), "Removed cart grid still linked"
        print("Remaining commerce administration routes return HTTP 404; user administration still renders.")
    if "--account-trimmed" in sys.argv:
        profile = get_page("/customer/info", admin, "save-info-button")
        assert "PreferredShippingMethodId" not in profile and "PreferredPaymentMethod" not in profile
        addresses = get_page("/customer/addresses", admin, "default-address-button")
        admin_id = read_rows("SELECT Id FROM Customer WHERE Email = ?", ("admin@ommcms.test",))[0][0]
        address_id = read_rows("SELECT Address_Id FROM CustomerAddresses WHERE Customer_Id = ?", (admin_id,))[0][0]
        post_form(f"/customer/setdefaultaddress/{address_id}", admin, profile, {})
        assert read_rows("SELECT Value FROM GenericAttribute WHERE EntityId = ? AND KeyGroup = 'Customer' AND Key = 'DefaultAddressId'", (admin_id,)) == [(str(address_id),)], "Preferred address was not persisted"
        addresses = get_page("/customer/addresses", admin, f"id=\"caddr-hint{address_id}\"")
        assert "Is default address" in addresses, "Preferred address badge missing"
        edit = get_page(f"/customer/addressedit/{address_id}", admin, 'name="IsDefaultAddress"')
        assert "IsDefaultBillingAddress" not in edit and "IsDefaultShippingAddress" not in edit
        print("Preferred CMS address persists and renders without billing/shipping controls.")
        for action in ("orders", "cancelrecurringpayment", "returncases", "downloadableproducts", "useragreement", "rewardpoints", "stocksubscriptions", "stocksubscriptionspost"):
            path = "/customer/" + action
            assert request(path, client=admin)[0] == 404, f"Retired account action reachable: {path}"
        login = get_page("/login?checkoutAsGuest=true", anonymous, 'name="CustomerLoginType"')
        assert "checkout-as-guest-button" not in login
        assert request("/mycheckout/mybillingaddress", client=admin)[0] == 404
        print("Public account profile and address pages, retired account actions and guest checkout removal passed.")
    if "--controllers-trimmed" in sys.argv:
        for controller in ["Affiliate","BarcodeTester","CheckoutAttribute","CollectionGroup","DeliveryTime","GiftCard","PriceLabel","ProductReview","QuantityUnit","RecurringPayment","ReturnCase","Shipment","Shipping","Tax","Payment","ShoppingCart","SpecificationAttribute","ProductAttribute"]:
            action = "index" if controller == "BarcodeTester" else "list"
            path = f"/admin/{controller.lower()}/{action}"
            assert request(path, client=admin)[0] == 404, f"Removed controller reachable: {path}"
        print("Extracted admin commerce endpoints return HTTP 404.")

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

    # Independent blog content and reusable taxonomy.
    page = get_page("/admin/blog/term", admin, 'id="blog-term-form"')
    post_form("/admin/blog/term", admin, page, {"Id": "0", "IsTag": "false", "Name": "Smoke category", "Slug": "smoke-category", "DisplayOrder": "1"})
    category_id = read_rows("SELECT Id FROM BlogCategory WHERE Slug = 'smoke-category'")[0][0]
    page = get_page("/admin/blog/term?isTag=true", admin, 'id="blog-term-form"')
    post_form("/admin/blog/term", admin, page, {"Id": "0", "IsTag": "true", "Name": "Smoke tag", "Slug": "smoke-tag"})
    tag_id = read_rows("SELECT Id FROM BlogTag WHERE Slug = 'smoke-tag'")[0][0]
    page = get_page("/admin/blog/edit", admin, 'id="blog-post-form"')
    blog_fields = {"Id": "0", "Title": "Blog smoke article", "Slug": "smoke-blog", "Intro": "Blog smoke intro",
                   "Body": "<p>Blog smoke content</p><script>alert(1)</script>", "PublishedOnUtc": "2020-01-01T12:00",
                   "IsPublished": "false", "IncludeInSitemap": "true", "CategoryIds": str(category_id), "TagIds": str(tag_id)}
    post_form("/admin/blog/edit", admin, page, blog_fields)
    blog_id = read_rows("SELECT Id FROM BlogPost WHERE Slug = 'smoke-blog'")[0][0]
    assert request("/blog/smoke-blog", client=anonymous)[0] == 404, "Draft exposed"
    page = get_page(f"/admin/blog/edit/{blog_id}", admin, 'id="blog-post-form"')
    blog_fields.update(Id=str(blog_id), IsPublished="true")
    post_form("/admin/blog/edit", admin, page, blog_fields)
    get_page("/blog", anonymous, "Blog smoke article")
    detail = get_page("/blog/smoke-blog", anonymous, "Blog smoke content")
    assert "<script>alert(1)</script>" not in detail, "Unsafe article body"
    get_page("/blog/category/smoke-category", anonymous, "Blog smoke article")
    get_page("/blog/tag/smoke-tag", anonymous, "Blog smoke article")
    get_page("/blog/archive/2020/1", anonymous, "Blog smoke article")
    get_page("/blog/rss.xml", anonymous, "Blog smoke article")
    get_page("/sitemap.xml", anonymous, "/blog/smoke-blog")
    assert read_rows("SELECT PostId FROM BlogPostCategory WHERE CategoryId = ?", (category_id,)) == [(blog_id,)]
    assert read_rows("SELECT PostId FROM BlogPostTag WHERE TagId = ?", (tag_id,)) == [(blog_id,)]
    print("Native blog draft, publish, sanitized content, category/tag, archive and RSS passed.")

    # Simulate the immediately preceding schema on this disposable database only.
    # The article and taxonomy rows already exist, but their central URLs do not.
    stop()
    with sqlite3.connect(database) as connection:
        deleted = connection.execute("DELETE FROM UrlRecord WHERE EntityName IN ('BlogPost', 'BlogCategory', 'BlogTag')").rowcount
        assert deleted == 3, "Expected exactly three newly managed URLs"
        migration_marker_count = connection.execute("DELETE FROM __MigrationVersionInfo WHERE Description LIKE '%Central URL records for blog content%'").rowcount
        assert migration_marker_count == 1, "Blog central URL migration marker missing"
    start()
    wait_for("/")
    get_page("/blog/smoke-blog", anonymous, "Blog smoke content")
    get_page("/blog/category/smoke-category", anonymous, "Blog smoke article")
    get_page("/blog/tag/smoke-tag", anonymous, "Blog smoke article")
    assert read_rows("SELECT COUNT(*) FROM UrlRecord WHERE EntityName IN ('BlogPost','BlogCategory','BlogTag') AND IsActive = 1") == [(3,)]
    print("Existing blog URL migration passed without changing public paths.")

    # Rename all kinds of content and verify exact HTTP 301 responses.
    page = get_page(f"/admin/blog/edit/{blog_id}", admin, 'id="blog-post-form"')
    blog_fields["Slug"] = "smoke-blog-second"
    post_form(f"/admin/blog/edit/{blog_id}", admin, page, blog_fields)
    assert_permanent_redirect("/blog/smoke-blog", "/blog/smoke-blog-second")
    page = get_page(f"/admin/blog/edit/{blog_id}", admin, 'id="blog-post-form"')
    blog_fields["Slug"] = "smoke-blog-final"
    post_form(f"/admin/blog/edit/{blog_id}", admin, page, blog_fields)
    assert_permanent_redirect("/blog/smoke-blog", "/blog/smoke-blog-final")
    assert_permanent_redirect("/blog/smoke-blog-second", "/blog/smoke-blog-final")
    page = get_page(f"/admin/blog/term/{category_id}", admin, 'id="blog-term-form"')
    post_form(f"/admin/blog/term/{category_id}", admin, page, {"Id": str(category_id), "IsTag": "false", "Name": "Smoke category", "Slug": "smoke-blog-final", "DisplayOrder": "1"})
    page = get_page(f"/admin/blog/term/{tag_id}?isTag=true", admin, 'id="blog-term-form"')
    post_form(f"/admin/blog/term/{tag_id}?isTag=true", admin, page, {"Id": str(tag_id), "IsTag": "true", "Name": "Smoke tag", "Slug": "smoke-blog-final"})
    assert_permanent_redirect("/blog/category/smoke-category", "/blog/category/smoke-blog-final")
    assert_permanent_redirect("/blog/tag/smoke-tag", "/blog/tag/smoke-blog-final")
    get_page("/blog/category/smoke-blog-final", anonymous, "Blog smoke article")
    get_page("/blog/tag/smoke-blog-final", anonymous, "Blog smoke article")
    get_page("/blog/smoke-blog-final", anonymous, "Blog smoke content")
    get_page("/blog/rss.xml", anonymous, "/blog/smoke-blog-final")
    sitemap = get_page("/sitemap.xml", anonymous, "/blog/smoke-blog-final")
    assert all(old not in sitemap for old in ("/blog/smoke-blog<", "/blog/smoke-blog/<", "/blog/smoke-blog-second<", "/blog/smoke-blog-second/<"))

    # A historical URL cannot be stolen by a different post.
    page = get_page("/admin/blog/edit", admin, 'id="blog-post-form"')
    conflicting = dict(blog_fields, Id="0", Title="Conflicting blog post", Slug="smoke-blog", IsPublished="false")
    post_form("/admin/blog/edit", admin, page, conflicting)
    assert read_rows("SELECT COUNT(*) FROM BlogPost WHERE Title = 'Conflicting blog post'") == [(0,)]
    assert read_rows("SELECT Slug, IsActive FROM UrlRecord WHERE EntityName = 'BlogPost' AND EntityId = ? ORDER BY Slug", (blog_id,)) == [
        ("blog/smoke-blog", 0), ("blog/smoke-blog-final", 1), ("blog/smoke-blog-second", 0)]

    # Reclaiming an item's own historical URL is allowed, but drafts never redirect publicly.
    page = get_page(f"/admin/blog/edit/{blog_id}", admin, 'id="blog-post-form"')
    blog_fields.update(Slug="smoke-blog", IsPublished="true")
    post_form(f"/admin/blog/edit/{blog_id}", admin, page, blog_fields)
    assert_permanent_redirect("/blog/smoke-blog-final", "/blog/smoke-blog")
    page = get_page(f"/admin/blog/edit/{blog_id}", admin, 'id="blog-post-form"')
    blog_fields["IsPublished"] = "false"
    post_form(f"/admin/blog/edit/{blog_id}", admin, page, blog_fields)
    assert request("/blog/smoke-blog")[0] == 404
    assert request("/blog/smoke-blog-final")[0] == 404
    page = get_page(f"/admin/blog/edit/{blog_id}", admin, 'id="blog-post-form"')
    blog_fields["IsPublished"] = "true"
    post_form(f"/admin/blog/edit/{blog_id}", admin, page, blog_fields)
    print("Blog URL history, 301 redirects, namespace isolation, collision rejection and draft privacy passed.")



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
    # Fetch the actual compiled Sass bundle for every published theme.
    published_themes = sorted(path.name for path in (root / "Themes").iterdir() if (path / "theme.config").is_file())
    assert set(published_themes) >= {"Flex", "FlexAfterglow", "FlexBlack", "FlexBlue", "FlexLumen", "FlexPorcelain", "FlexRiso", "FlexSorbet", "FlexTidal"}, f"Missing bundled themes: {published_themes}"
    for theme in published_themes:
        css_status, css = request(f"/themes/{theme.lower()}/theme.css", timeout=120)
        assert css_status == 200, f"{theme} stylesheet returned HTTP {css_status}"
        assert ".media-gallery" in css and ".gal-box" in css, f"{theme} gallery styling missing"
        assert ".costep" not in css and ".pd-offer" not in css and ".art-finalprice" not in css, f"{theme} contains retired commerce styling"
        assert ".rotator-container" not in css, f"{theme} contains the retired product rotator"
        assert ".choice-item-linked-product-img" not in css and ".choice-item-img" in css, f"{theme} retains product-linked choice styling or lacks choice images"
    print(f"All {len(published_themes)} published CMS theme stylesheets passed.")
    rtl_status, rtl_css = request("/themes/flex/theme-rtl.css", timeout=120)
    assert rtl_status == 200 and ".media-gallery" in rtl_css, "RTL theme stylesheet missing gallery styling"
    menu_status, menu_js = request("/js/ommcms.megamenu.js")
    assert menu_status == 200 and "aria-expanded" in menu_js and "megaMenu" in menu_js
    assert "productRotatorAjaxUrl" not in menu_js, "Catalog rotator still exposed"
    editor_status, editor_js = request("/js/ommcms.editortemplates.js")
    assert editor_status == 200 and "onMediaSelected" in editor_js and "productPickerCallback" not in editor_js
    uploader_status, uploader_js = request("/js/ommcms.dropzoneWrapper.js")
    assert uploader_status == 200 and "EntityMediaId" in uploader_js and "ProductMediaFileId" not in uploader_js
    assert request("/bundle/css/site-common.css")[0] == 200, "Common CSS bundle missing"
    assert request("/css/compiled/theme.css")[0] == 404, "Obsolete compiled theme snapshot still exposed"
    assert request("/css/compiled/site-common.css")[0] == 404, "Obsolete compiled common snapshot still exposed"
    print("CMS navigation script, RTL styling and dynamic CSS bundles passed.")


def verify_trimmed_distribution():
    removed = ["OmmCMS.AmazonPay","OmmCMS.OfflinePayment","OmmCMS.PayPal","OmmCMS.Stripe","OmmCMS.Shipping","OmmCMS.ShippingByWeight","OmmCMS.Tax","OmmCMS.Google.MerchantCenter","OmmCMS.Clickatell","OmmCMS.WebApi"]
    retained = ["OmmCMS.Apple.Auth", "OmmCMS.Facebook.Auth", "OmmCMS.Google.Auth",
                "OmmCMS.Microsoft.Auth", "OmmCMS.Twitter.Auth", "OmmCMS.DevTools",
                "OmmCMS.Google.Analytics"]
    repo = Path(__file__).resolve().parent.parent
    module_sources = repo / "src" / "OmmCMS.Modules"
    modules = root / "Modules"
    solution = (repo / "OmmCMS.sln").read_text(encoding="utf-8-sig")
    for name in removed:
        assert not (module_sources / name).exists(), f"Retired source remains: {name}"
        assert not (modules / name).exists(), f"Retired module is in publish output: {name}"
        assert f'{name}.csproj' not in solution, f"Retired project is in solution: {name}"
    assert not (repo / "tools" / "OmmCMS.WebApi.Client").exists(), "Obsolete API client remains"
    for name in retained:
        assert (modules / name / "module.json").is_file(), f"Retained module is absent: {name}"
    print(f"Trimmed distribution passed: {len(removed)} retired modules absent; {len(retained)} retained modules present.")


def verify_trimmed_controllers():
    names = ["Affiliate","BarcodeTester","CheckoutAttribute","CollectionGroup","DeliveryTime","GiftCard","PriceLabel","ProductReview","QuantityUnit","RecurringPayment","ReturnCase","Shipment","Shipping","Tax","Payment","ShoppingCart","SpecificationAttribute","ProductAttribute"]
    source = Path(__file__).resolve().parent.parent / "src" / "OmmCMS.Web" / "Areas" / "Admin"
    for name in names:
        assert not (source / "Controllers" / (name + "Controller.cs")).exists(), f"Retired controller remains: {name}"
        if name != "Shipment":
            assert not (source / "Views" / name).exists(), f"Retired source views remain: {name}"
            assert not (root / "Areas" / "Admin" / "Views" / name).exists(), f"Retired published views remain: {name}"
    for name in ("Create.cshtml", "Edit.cshtml", "List.cshtml", "PdfPackagingSlips.Print.cshtml"):
        assert not (source / "Views" / "Shipment" / name).exists(), f"Retired shipment view remains: {name}"
        assert not (root / "Areas" / "Admin" / "Views" / "Shipment" / name).exists(), f"Retired published shipment view remains: {name}"
    if "--admin-commerce-trimmed" in sys.argv:
        assert not (source / "Views" / "Shipment").exists(), "Retired shipment grid remains"
    else:
        assert (source / "Views" / "Shipment" / "_Grid.Shipments.cshtml").is_file(), "Shared order shipment grid missing"
    print(f"Controller extraction passed: {len(names)} retired admin controllers and their standalone views absent.")


def verify_public_controllers():
    source = Path(__file__).resolve().parent.parent / "src" / "OmmCMS.Web"
    for name in ["Catalog","Checkout","Download","Order","Product","ReturnCase","Search","ShoppingCart"]:
        assert not (source / "Controllers" / (name + "Controller.cs")).exists(), f"Retired public controller remains: {name}"
    for path in ["Views/Catalog/CategoryTemplate.ProductsInGridOrLines.cshtml","Views/Catalog/CompareProducts.cshtml","Views/Catalog/ManufacturerAll.cshtml","Views/Catalog/ManufacturerTemplate.ProductsInGridOrLines.cshtml","Views/Catalog/ProductTagsAll.cshtml","Views/Catalog/ProductsByTag.cshtml","Views/Catalog/RecentlyAddedProducts.cshtml","Views/Catalog/RecentlyViewedProducts.cshtml","Views/Checkout/BillingAddress.cshtml","Views/Checkout/Completed.cshtml","Views/Checkout/Confirm.cshtml","Views/Checkout/PaymentMethod.cshtml","Views/Checkout/ShippingAddress.cshtml","Views/Checkout/ShippingMethod.cshtml","Views/Checkout/Terminal.Confirm.cshtml","Views/Order/Details.cshtml","Views/Order/ShipmentDetails.cshtml","Views/Product/AskQuestion.cshtml","Views/Product/BackInStockSubscribePopup.cshtml","Views/Product/EmailAFriend.cshtml","Views/Product/Product.cshtml","Views/Product/Reviews.cshtml","Views/ReturnCase/ReturnCase.cshtml","Views/Search/Search.cshtml","Views/ShoppingCart/Cart.cshtml","Views/ShoppingCart/EmailWishlist.cshtml","Views/ShoppingCart/Wishlist.cshtml"]:
        assert not (source / path).exists(), f"Retired source view remains: {path}"
        assert not (root / path).exists(), f"Retired published view remains: {path}"
    print("Public commerce source and publish extraction passed.")


def verify_account_extraction():
    repo = Path(__file__).resolve().parent.parent
    for path in ["src/OmmCMS.Modules/OmmCMS.DevTools/Controllers/MyCheckoutController.cs","src/OmmCMS.Modules/OmmCMS.DevTools/Views/MyCheckout/MyBillingAddress.cshtml","src/OmmCMS.Modules/OmmCMS.Google.Analytics/Filters/CheckoutFilter.cs","src/OmmCMS.Web/Models/Customers/CustomerDownloadableProductsModel.cs","src/OmmCMS.Web/Models/Customers/CustomerOrderListModel.cs","src/OmmCMS.Web/Models/Customers/CustomerReturnCasesModel.cs","src/OmmCMS.Web/Models/Customers/CustomerRewardPointsModel.cs","src/OmmCMS.Web/Models/Customers/CustomerStockSubscriptionsModel.cs","src/OmmCMS.Web/Views/Customer/DownloadableProducts.cshtml","src/OmmCMS.Web/Views/Customer/Orders.cshtml","src/OmmCMS.Web/Views/Customer/Partials/RecurringPayments.cshtml","src/OmmCMS.Web/Views/Customer/ReturnCases.cshtml","src/OmmCMS.Web/Views/Customer/RewardPoints.cshtml","src/OmmCMS.Web/Views/Customer/StockSubscriptions.cshtml","src/OmmCMS.Web/Views/Customer/UserAgreement.cshtml","src/OmmCMS.Modules/OmmCMS.DevTools/Filters/Samples/SampleCheckoutFilter.cs"]:
        assert not (repo / path).exists(), f"Retired account source remains: {path}"
        if path.startswith("src/OmmCMS.Web/Views/"):
            assert not (root / path.removeprefix("src/OmmCMS.Web/")).exists(), f"Retired account view was published: {path}"
    print("Account dependency source and publish extraction passed.")


def verify_remaining_admin_extraction():
    source = Path(__file__).resolve().parent.parent / "src" / "OmmCMS.Web" / "Areas" / "Admin"
    for path in ["Controllers/CampaignController.cs","Controllers/CategoryController.cs","Controllers/CurrencyController.cs","Controllers/DiscountController.cs","Controllers/ExportController.cs","Controllers/ImportController.cs","Controllers/ManufacturerController.cs","Controllers/MeasureController.cs","Controllers/OrderController.cs","Controllers/ProductController.AssignableProducts.cs","Controllers/ProductController.Attributes.cs","Controllers/ProductController.Grid.cs","Controllers/ProductController.RecycleBin.cs","Controllers/ProductController.Settings.cs","Controllers/ProductController.cs","Controllers/SearchController.cs","Views/Campaign/Create.cshtml","Views/Campaign/Edit.cshtml","Views/Campaign/List.cshtml","Views/Campaign/_CreateOrUpdate.cshtml","Views/Campaign/_Grid.Campaigns.cshtml","Views/Category/Create.cshtml","Views/Category/Edit.cshtml","Views/Category/EditorTemplates/CategoryProduct.cshtml","Views/Category/List.cshtml","Views/Category/Tree.cshtml","Views/Category/_CreateOrUpdate.cshtml","Views/Category/_DeleteCategoryDialog.cshtml","Views/Category/_Grid.Categories.Search.cshtml","Views/Category/_Grid.Categories.cshtml","Views/Category/_Grid.ProductCategories.cshtml","Views/Category/_ViewImports.cshtml","Views/Currency/Create.cshtml","Views/Currency/Edit.cshtml","Views/Currency/List.cshtml","Views/Currency/_CreateOrUpdate.cshtml","Views/Currency/_Grid.Currencies.cshtml","Views/Currency/_Grid.LiveRates.cshtml","Views/Discount/Create.cshtml","Views/Discount/Edit.cshtml","Views/Discount/List.cshtml","Views/Discount/_CreateOrUpdate.cshtml","Views/Discount/_Grid.DiscountUsageHistory.cshtml","Views/Discount/_Grid.Discounts.Search.cshtml","Views/Discount/_Grid.Discounts.cshtml","Views/Discount/_ViewImports.cshtml","Views/Export/Create.cshtml","Views/Export/CreateDeployment.cshtml","Views/Export/Edit.cshtml","Views/Export/EditDeployment.cshtml","Views/Export/List.cshtml","Views/Export/Preview.cshtml","Views/Export/ProfileFileCount.cshtml","Views/Export/ProfileFileDetails.cshtml","Views/Export/_CreateOrUpdate.Deployment.cshtml","Views/Export/_Grid.Preview.Category.cshtml","Views/Export/_Grid.Preview.Customer.cshtml","Views/Export/_Grid.Preview.Manufacturer.cshtml","Views/Export/_Grid.Preview.NewsLetterSubscription.cshtml","Views/Export/_Grid.Preview.Order.cshtml","Views/Export/_Grid.Preview.Product.cshtml","Views/Export/_Grid.Preview.ShoppingCartItem.cshtml","Views/Export/_ProfileList.cshtml","Views/Export/_ResolveTokensExample.cshtml","Views/Export/_Tab.Deployment.cshtml","Views/Export/_Tab.Filter.cshtml","Views/Export/_Tab.Projection.cshtml","Views/Export/_ViewImports.cshtml","Views/Import/DataExchangeSettings.cshtml","Views/Import/Edit.cshtml","Views/Import/List.cshtml","Views/Import/ProfileImportResult.cshtml","Views/Import/_ColumnMappings.cshtml","Views/Import/_CsvConfiguration.cshtml","Views/Import/_Update.cshtml","Views/Import/_ViewImports.cshtml","Views/Manufacturer/Create.cshtml","Views/Manufacturer/Edit.cshtml","Views/Manufacturer/EditorTemplates/ManufacturerProduct.cshtml","Views/Manufacturer/List.cshtml","Views/Manufacturer/_CreateOrUpdate.cshtml","Views/Manufacturer/_Grid.Manufacturers.Search.cshtml","Views/Manufacturer/_Grid.Manufacturers.cshtml","Views/Manufacturer/_Grid.ProductManufacturers.cshtml","Views/Manufacturer/_ViewImports.cshtml","Views/Measure/CreateDimensionPopup.cshtml","Views/Measure/CreateWeightPopup.cshtml","Views/Measure/Dimensions.cshtml","Views/Measure/EditDimensionPopup.cshtml","Views/Measure/EditWeightPopup.cshtml","Views/Measure/Weights.cshtml","Views/Measure/_CreateOrUpdateDimension.cshtml","Views/Measure/_CreateOrUpdateWeight.cshtml","Views/Measure/_Grid.MeasureDimension.cshtml","Views/Measure/_Grid.MeasureWeight.cshtml","Views/Order/AddProductToOrder.cshtml","Views/Order/AddressEdit.cshtml","Views/Order/BestsellersReport.cshtml","Views/Order/Edit.cshtml","Views/Order/Grids/_Grid.Bestsellers.Search.cshtml","Views/Order/Grids/_Grid.Bestsellers.cshtml","Views/Order/Grids/_Grid.NeverSold.Search.cshtml","Views/Order/Grids/_Grid.NeverSold.cshtml","Views/Order/Grids/_Grid.OrderNotes.cshtml","Views/Order/Grids/_Grid.Orders.Search.cshtml","Views/Order/Grids/_Grid.Orders.cshtml","Views/Order/List.cshtml","Views/Order/NeverSoldReport.cshtml","Views/Order/OrderSettings.cshtml","Views/Order/PartiallyRefundOrderPopup.cshtml","Views/Order/UploadLicenseFilePopup.cshtml","Views/Order/_AddProductToOrder.Attributes.cshtml","Views/Order/_AddProductToOrder.GiftCardInfo.cshtml","Views/Order/_AddProductToOrder.Prices.cshtml","Views/Order/_Edit.BillingAndShipment.cshtml","Views/Order/_Edit.Info.cshtml","Views/Order/_Edit.OrderTotals.cshtml","Views/Order/_Edit.Payment.cshtml","Views/Order/_Edit.Products.cshtml","Views/Order/_UpdateOrderItemDialog.cshtml","Views/Order/_ViewImports.cshtml","Views/Product/AttributeCombinationCreatePopup.cshtml","Views/Product/AttributeCombinationEditPopup.cshtml","Views/Product/BundleItemEditPopup.cshtml","Views/Product/CatalogSettings.cshtml","Views/Product/Create.cshtml","Views/Product/DeletedProductDetails.cshtml","Views/Product/Edit.cshtml","Views/Product/EditAttributeValues.cshtml","Views/Product/EditGroupedProductConfiguration.cshtml","Views/Product/EditProductTag.cshtml","Views/Product/EditorTemplates/AttributeControlType.cshtml","Views/Product/EditorTemplates/ProductAttribute.cshtml","Views/Product/EditorTemplates/ProductCategory.cshtml","Views/Product/EditorTemplates/ProductManufacturer.cshtml","Views/Product/EditorTemplates/TierPriceCalculationMethod.cshtml","Views/Product/EditorTemplates/TierPriceCustomer.cshtml","Views/Product/EditorTemplates/TierPriceStore.cshtml","Views/Product/Grids/_Grid.AssociatedProducts.cshtml","Views/Product/Grids/_Grid.AttributeCombinations.cshtml","Views/Product/Grids/_Grid.AttributeValues.cshtml","Views/Product/Grids/_Grid.BundleItems.cshtml","Views/Product/Grids/_Grid.Categories.cshtml","Views/Product/Grids/_Grid.CrossSellProducts.cshtml","Views/Product/Grids/_Grid.DeletedProducts.Search.cshtml","Views/Product/Grids/_Grid.DeletedProducts.cshtml","Views/Product/Grids/_Grid.LowStockReport.cshtml","Views/Product/Grids/_Grid.Manufacturers.cshtml","Views/Product/Grids/_Grid.ProductTags.Search.cshtml","Views/Product/Grids/_Grid.ProductTags.cshtml","Views/Product/Grids/_Grid.ProductVariantAttribute.cshtml","Views/Product/Grids/_Grid.Products.Search.cshtml","Views/Product/Grids/_Grid.Products.cshtml","Views/Product/Grids/_Grid.RelatedProducts.cshtml","Views/Product/Grids/_Grid.SpecificationAttributes.cshtml","Views/Product/Grids/_Grid.TierPrices.cshtml","Views/Product/List.cshtml","Views/Product/LowStockReport.cshtml","Views/Product/Partials/GroupedProductSettings.cshtml","Views/Product/Partials/PriceSettings.cshtml","Views/Product/ProductAttributeSwatchEditPopup.cshtml","Views/Product/ProductAttributeValueCreatePopup.cshtml","Views/Product/ProductAttributeValueEditPopup.cshtml","Views/Product/ProductTags.cshtml","Views/Product/RecycleBin.cshtml","Views/Product/_CopyAttributeOptionsDialog.cshtml","Views/Product/_CopyProductDialog.cshtml","Views/Product/_Create.SaveFirst.cshtml","Views/Product/_CreateOrUpdate.AssociatedProducts.cshtml","Views/Product/_CreateOrUpdate.Attributes.cshtml","Views/Product/_CreateOrUpdate.BundleItems.cshtml","Views/Product/_CreateOrUpdate.Categories.cshtml","Views/Product/_CreateOrUpdate.Downloads.cshtml","Views/Product/_CreateOrUpdate.Info.cshtml","Views/Product/_CreateOrUpdate.Inventory.cshtml","Views/Product/_CreateOrUpdate.Manufacturers.cshtml","Views/Product/_CreateOrUpdate.Orders.cshtml","Views/Product/_CreateOrUpdate.Pictures.cshtml","Views/Product/_CreateOrUpdate.Price.cshtml","Views/Product/_CreateOrUpdate.Promotion.cshtml","Views/Product/_CreateOrUpdate.SEO.cshtml","Views/Product/_CreateOrUpdate.SpecificationAttributes.cshtml","Views/Product/_CreateOrUpdate.cshtml","Views/Product/_CreateOrUpdateAttributeCombinationPopup.cshtml","Views/Product/_CreateOrUpdateProductAttributeValue.cshtml","Views/Product/_CreateOrUpdateProductTag.cshtml","Views/Product/_ProductAttributes.cshtml","Views/Product/_RestoreProductDialog.cshtml","Views/Product/_ViewImports.cshtml","Views/Search/SearchCommonFacet.cshtml","Views/Search/SearchSettings.cshtml","Views/Shipment/_Grid.Shipments.Search.cshtml","Views/Shipment/_Grid.Shipments.cshtml","Views/Shipment/_ViewImports.cshtml","Views/Customer/Grids/_Grid.RewardPoints.cshtml","Views/Customer/Grids/_Grid.ShoppingCart.cshtml","Views/Customer/Grids/_Grid.Wishlist.cshtml"]:
        assert not (source / path).exists(), f"Retired admin source remains: {path}"
        if path.startswith("Views/"):
            assert not (root / "Areas" / "Admin" / path).exists(), f"Retired admin view was published: {path}"
    assert (source / "Controllers" / "PackagingController.cs").is_file(), "CMS package installer source missing"
    print("Remaining admin commerce source and published views extraction passed.")


def verify_residual_extraction():
    repo = Path(__file__).resolve().parent.parent
    for path in ["src/OmmCMS.Web/Components/CheckoutProgressViewComponent.cs","src/OmmCMS.Web/Components/CrossSellProductsViewComponent.cs","src/OmmCMS.Web/Components/CurrencySelectorViewComponent.cs","src/OmmCMS.Web/Components/HomeBestsellersViewComponent.cs","src/OmmCMS.Web/Components/HomeBrandsViewComponent.cs","src/OmmCMS.Web/Components/HomeCategoriesViewComponent.cs","src/OmmCMS.Web/Components/HomeProductsViewComponent.cs","src/OmmCMS.Web/Components/OffCanvasCartViewComponent.cs","src/OmmCMS.Web/Components/OrderSummaryViewComponent.cs","src/OmmCMS.Web/Components/OrderTotalsViewComponent.cs","src/OmmCMS.Web/Components/PopularProductTagsViewComponent.cs","src/OmmCMS.Web/Components/RecentlyViewedProductsViewComponent.cs","src/OmmCMS.Web/Views/Shared/Components/CheckoutProgress/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/CrossSellProducts/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/CurrencySelector/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/HomeBestSellers/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/HomeBrands/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/HomeCategories/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/HomeProducts/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/OffCanvasCart/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/OrderSummary/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/OrderTotals/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/OrderTotals/OrderTotalOnly.cshtml","src/OmmCMS.Web/Views/Shared/Components/PopularProductTags/Default.cshtml","src/OmmCMS.Web/Views/Shared/Components/RecentlyViewedProducts/Default.cshtml","src/OmmCMS.Web/Models/Checkout/CheckoutProgressModel.cs","src/OmmCMS.Web/Models/ShoppingCart/OffCanvasCartModel.cs","src/OmmCMS.Web/Models/ShoppingCart/OrderTotalsModel.cs","src/OmmCMS.Web/Models/Catalog/PopularProductTagsModel.cs","src/OmmCMS.Web/Views/Menu/OffCanvas.Brands.cshtml","src/OmmCMS.Web/Views/Checkout/Partials/CheckoutOrderTotals.cshtml","src/OmmCMS.Web/Views/Checkout/Partials/ConfirmDetails.cshtml","src/OmmCMS.Web/Views/Checkout/Partials/Terminal.ConfirmDetails.cshtml","src/OmmCMS.Modules/OmmCMS.Google.Analytics/Events.cs","src/OmmCMS.Web/Areas/Admin/Models/Customers/RewardPointsSettingsModel.cs","src/OmmCMS.Web/Areas/Admin/Models/Customers/TopCustomersReportModel.cs","src/OmmCMS.Web/Areas/Admin/Views/Customer/Grids/_Grid.BestCustomers.Search.cshtml","src/OmmCMS.Web/Areas/Admin/Views/Customer/Grids/_Grid.BestCustomers.cshtml","src/OmmCMS.Web/Areas/Admin/Views/Customer/Reports.cshtml","src/OmmCMS.Web/Areas/Admin/Views/Customer/RewardPointsSettings.cshtml"]:
        assert not (repo / path).exists(), f"Retired source remains: {path}"
        if path.startswith("src/OmmCMS.Web/") and path.endswith(".cshtml"):
            assert not (root / path.removeprefix("src/OmmCMS.Web/")).exists(), f"Retired view published: {path}"
    print("Residual commerce source and publish extraction passed.")


def verify_presentation_extraction():
    repo = Path(__file__).resolve().parent.parent
    for path in [
        "src/OmmCMS.Web/Areas/Admin/Views/Rule/_Grid.Preview.Product.cshtml",
        "src/OmmCMS.Core/Catalog/Products/Services/IProductCloner.cs",
        "src/OmmCMS.Core/Catalog/Products/Services/IProductCompareService.cs",
        "src/OmmCMS.Core/Catalog/Products/Services/IRecentlyViewedProductsService.cs",
        "src/OmmCMS.Core/Catalog/Products/Services/ProductCloner.cs",
        "src/OmmCMS.Core/Catalog/Products/Services/ProductCompareService.cs",
        "src/OmmCMS.Core/Catalog/Products/Services/RecentlyViewedProductsService.cs",
        "src/OmmCMS.Modules/OmmCMS.DevTools/Filters/Samples/SampleProductDetailActionFilter.cs",
        "src/OmmCMS.Modules/OmmCMS.DevTools/Filters/Samples/SampleResultFilter.cs",
        "src/OmmCMS.Web.Common/Models/Pdf/Mappers/PdfReceiptMapper.cs",
        "src/OmmCMS.Web.Common/Models/Pdf/PdfReceiptSectionModel.cs",
        "src/OmmCMS.Web.Common/Models/Pdf/PdfSectionVariables.cs",
        "src/OmmCMS.Web/Components/FacetGroupViewComponent.cs",
        "src/OmmCMS.Web/Components/SearchBoxViewComponent.cs",
        "src/OmmCMS.Web/Controllers/CatalogHelper.Price.cs",
        "src/OmmCMS.Web/Controllers/CatalogHelper.Product.cs",
        "src/OmmCMS.Web/Controllers/CatalogHelper.ProductSummary.cs",
        "src/OmmCMS.Web/Controllers/CatalogHelper.cs",
        "src/OmmCMS.Web/Controllers/OrderHelper.cs",
        "src/OmmCMS.Web/Controllers/PdfController.cs",
        "src/OmmCMS.Web/Models/Catalog/BackInStockSubscribeModel.cs",
        "src/OmmCMS.Web/Models/Catalog/BrandModel.cs",
        "src/OmmCMS.Web/Models/Catalog/BrandNavigationModel.cs",
        "src/OmmCMS.Web/Models/Catalog/BrandOverviewModel.cs",
        "src/OmmCMS.Web/Models/Catalog/CategoryModel.cs",
        "src/OmmCMS.Web/Models/Catalog/CategorySummaryModel.cs",
        "src/OmmCMS.Web/Models/Catalog/DeliveryTimeModel.cs",
        "src/OmmCMS.Web/Models/Catalog/Extensions/JsonLdExtensions.cs",
        "src/OmmCMS.Web/Models/Catalog/GroupedProductModel.cs",
        "src/OmmCMS.Web/Models/Catalog/IQuantityInput.cs",
        "src/OmmCMS.Web/Models/Catalog/Mappers/BrandMetaPropertyMapper.cs",
        "src/OmmCMS.Web/Models/Catalog/Mappers/CategoryMetaPropertyMapper.cs",
        "src/OmmCMS.Web/Models/Catalog/Mappers/ProductMetaPropertyMapper.cs",
        "src/OmmCMS.Web/Models/Catalog/Mappers/ProductQuantityInputMapper.cs",
        "src/OmmCMS.Web/Models/Catalog/Mappers/QuantityInputMapperBase.cs",
        "src/OmmCMS.Web/Models/Catalog/PriceModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductAskQuestionModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductDetailsModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductDetailsModelContext.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductEmailAFriendModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductReviewsModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductSpecificationModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductSummaryItemModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductSummaryModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductTagModel.cs",
        "src/OmmCMS.Web/Models/Catalog/ProductsByTagModel.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutAddressModel.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutCompletedModel.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutConfirmModel.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutModelBase.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutPaymentMethodModel.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutRefreshModel.cs",
        "src/OmmCMS.Web/Models/Checkout/CheckoutShippingMethodModel.cs",
        "src/OmmCMS.Web/Models/Checkout/Mappers/CheckoutAddressMapper.cs",
        "src/OmmCMS.Web/Models/Checkout/Mappers/CheckoutConfirmMapper.cs",
        "src/OmmCMS.Web/Models/Checkout/Mappers/CheckoutPaymentMethodMapper.cs",
        "src/OmmCMS.Web/Models/Checkout/Mappers/CheckoutShippingMethodMapper.cs",
        "src/OmmCMS.Web/Models/Orders/AddReturnCaseModel.cs",
        "src/OmmCMS.Web/Models/Orders/Mappers/ReturnCaseItemsMapper.cs",
        "src/OmmCMS.Web/Models/Orders/OrderDetailsModel.cs",
        "src/OmmCMS.Web/Models/Orders/ReturnCaseItemsModel.cs",
        "src/OmmCMS.Web/Models/Orders/ReturnCaseModel.cs",
        "src/OmmCMS.Web/Models/Orders/ShipmentDetailsModel.cs",
        "src/OmmCMS.Web/Models/Search/ISearchResultModel.cs",
        "src/OmmCMS.Web/Models/Search/SearchResultModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/ButtonPaymentMethodModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/CartModelBase.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/EstimateShippingModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/CartItemImageMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/CartItemMapperBase.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/CartItemQuantityInputMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/CartItemSorter.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/CartMapperBase.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/MiniShoppingCartMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/ShoppingCartItemMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/ShoppingCartMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/WishlistItemMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/Mappers/WishlistMapper.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/MiniShoppingCartModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/ShoppingCartModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/UpdateCartItemModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/WishlistEmailAFriendModel.cs",
        "src/OmmCMS.Web/Models/ShoppingCart/WishlistModel.cs",
        "src/OmmCMS.Web/Views/Catalog/Partials/OffCanvasCompare.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/AddAddressButton.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/AddressScript.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/ConfirmScript.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/OrderReviewData.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/PaymentMethodList.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/ShippingAddressDiffers.cshtml",
        "src/OmmCMS.Web/Views/Checkout/Partials/ShippingMethodList.cshtml",
        "src/OmmCMS.Web/Views/Order/Details.Print.cshtml",
        "src/OmmCMS.Web/Views/Pdf/ReceiptFooter.cshtml",
        "src/OmmCMS.Web/Views/Pdf/ReceiptHeader.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.AlsoPurchased.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.AssociatedProduct.Header.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.AssociatedProduct.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.AssociatedProducts.Search.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.AssociatedProducts.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Attrs.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.AvailablePaymentMethods.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Bundle.Items.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Bundle.Price.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Bundle.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.CustomerPrice.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.GiftCard.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Info.Mobile.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Info.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Media.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Offer.Actions.Misc.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Offer.Actions.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Offer.Price.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Offer.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.RelatedProducts.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.ReviewSummary.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Reviews.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Specs.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.StockInfo.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Tabs.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Tags.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.TierPrices.cshtml",
        "src/OmmCMS.Web/Views/Product/Partials/Product.Variants.cshtml",
        "src/OmmCMS.Web/Views/Search/Partials/InstantSearch.HitGroup.cshtml",
        "src/OmmCMS.Web/Views/Search/Partials/InstantSearch.TopProducts.cshtml",
        "src/OmmCMS.Web/Views/Search/Partials/InstantSearch.cshtml",
        "src/OmmCMS.Web/Views/Shared/Components/FacetGroup/MultiSelect.cshtml",
        "src/OmmCMS.Web/Views/Shared/Components/FacetGroup/Price.cshtml",
        "src/OmmCMS.Web/Views/Shared/Components/FacetGroup/Rating.cshtml",
        "src/OmmCMS.Web/Views/Shared/Components/FacetGroup/SingleSelect.cshtml",
        "src/OmmCMS.Web/Views/Shared/Components/SearchBox/Default.cshtml",
        "src/OmmCMS.Web/Views/Shared/EditorTemplates/QtyInput.cshtml",
        "src/OmmCMS.Web/Views/Shared/Layouts/_Checkout.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/CartItem.BundleItems.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/CartItem.Details.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/CartItem.Price.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/CartItems.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/CartPaymentButtons.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/CartWarnings.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.FilterSort.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.Brand.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.Buttons.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.Delivery.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.Price.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.Rating.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.Variants.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Item.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.Pager.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.ViewMode.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Product.List.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/ReturnCase.Infos.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/ReturnCase.Items.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Search.ActiveFilters.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/Search.Filters.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/SearchResultHitsCount.cshtml",
        "src/OmmCMS.Web/Views/Shared/Partials/_CommentBox.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/EstimateShipping.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/OffCanvasShoppingCart.Items.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/OffCanvasShoppingCart.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/OffCanvasWishlist.Items.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/OffCanvasWishlist.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/WishlistItems.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/_CheckoutAttributes.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/_DiscountBox.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/_GiftCardBox.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/Partials/_RewardPointsBox.cshtml",
        "src/OmmCMS.Web/Views/ShoppingCart/_ViewImports.cshtml",
        "test/OmmCMS.Web.Tests/Web/CartItemSorterTests.cs",
        "test/OmmCMS.Web.Tests/Web/CatalogHelperPriceTests.cs",
        "src/OmmCMS.Web.Common/Models/Catalog/IListActions.cs",
        "src/OmmCMS.Web.Common/Models/Search/SearchBoxModel.cs",
        "src/OmmCMS.Modules/OmmCMS.DevTools/Components/CustomFacetViewComponent.cs",
        "src/OmmCMS.Modules/OmmCMS.DevTools/Services/CustomFacetTemplateSelector.cs",
        "src/OmmCMS.Modules/OmmCMS.DevTools/Views/Shared/Components/CustomFacet/Default.cshtml",
        "src/OmmCMS.Core/Catalog/Attributes/Services/IProductAttributeService.cs",
        "src/OmmCMS.Core/Catalog/Attributes/Services/ProductAttributeService.cs",
    ]:
        assert not (repo / path).exists(), f"Retired source remains: {path}"
        if path.startswith("src/OmmCMS.Web/") and path.endswith(".cshtml"):
            assert not (root / path.removeprefix("src/OmmCMS.Web/")).exists(), f"Retired view published: {path}"
    print("Commerce presentation and detached core services extraction passed.")


try:
    if "--core-commerce-trimmed" in sys.argv:
        repo = Path(__file__).resolve().parent.parent
        for path in (
            "Catalog/Products/Services/IStockSubscriptionService.cs",
            "Catalog/Products/Services/StockSubscriptionService.cs",
            "Catalog/Products/Extensions/BackInStockSubscriptionQueryExtensions.cs",
            "Catalog/Products/Domain/RelatedProduct.cs",
            "Catalog/Products/Domain/CrossSellProduct.cs",
            "Catalog/Products/Extensions/RelatedProductQueryExtensions.cs",
            "Catalog/Products/Extensions/CrossSellProductQueryExtensions.cs",
            "Catalog/Attributes/Modelling/IProductVariantQueryFactory.cs",
            "Catalog/Attributes/Modelling/ProductVariantQueryFactory.cs",
            "Catalog/Attributes/Modelling/ProductAttributeQueryModelBinder.cs",
            "Checkout/Orders/Services/CheckoutWorkflow.cs",
            "Checkout/Orders/Services/ICheckoutWorkflow.cs",
            "Checkout/Orders/Services/CheckoutFactory.cs",
            "Checkout/Orders/Services/ICheckoutFactory.cs",
            "Checkout/Orders/Services/CheckoutContext.cs",
            "Checkout/Orders/Services/CheckoutHandler.cs",
            "Checkout/Orders/Services/CheckoutHandlerBase.cs",
            "Checkout/Orders/Services/CheckoutHandlerMetadata.cs",
            "Checkout/Orders/Services/CheckoutResult.cs",
            "Checkout/Orders/Services/CheckoutStep.cs",
            "Checkout/Orders/Services/Handlers",
            "Checkout/Orders/Services/DefaultCheckoutStateAccessor.cs",
            "Checkout/Orders/Services/ICheckoutStateAccessor.cs",
            "Checkout/Orders/Domain/CheckoutState.cs",
            "Checkout/Bootstrapping/CheckoutBootstrappingExtensions.cs",
            "Checkout/Orders/Services/IOrderProcessingService.cs",
            "Checkout/Orders/Services/OrderProcessingService.cs",
            "Checkout/Orders/Services/OrderProcessingService.Payment.cs",
            "Checkout/Orders/Services/OrderProcessingService.PlaceOrder.cs",
            "Checkout/Orders/Services/OrderRecoveryData.cs",
            "Checkout/Orders/Domain/AutoUpdateOrderItemContext.cs",
            "Checkout/Orders/Domain/OrderPlacementResult.cs",
            "Checkout/Orders/Domain/OrderTotalValidationResult.cs",
            "Checkout/Cart/Consumers/ValidatingCartEventConsumer.cs",
            "Checkout/Cart/Events/ValidatingCartEvent.cs",
            "Checkout/Orders/Services/IOrderCalculationService.cs",
            "Checkout/Orders/Services/OrderCalculationService.cs",
            "Checkout/Orders/Extensions/IOrderCalculationServiceExtensions.cs",
            "Checkout/Rules/Impl/Cart/CartSubtotalRule.cs",
            "Checkout/Rules/Impl/Cart/CartTotalRule.cs",
            "Checkout/Cart/Domain/ShoppingCartTotal.cs",
            "Checkout/Cart/Domain/ShoppingCartSubTotal.cs",
            "Checkout/Cart/Domain/ShoppingCartShippingTotal.cs",
            "Checkout/Cart/Domain/ShoppingCartLineItem.cs",
            "Checkout/GiftCards/Domain/GiftCard.cs",
            "Checkout/GiftCards/Domain/GiftCardUsageHistory.cs",
            "Checkout/GiftCards/Domain/AppliedGiftCard.cs",
            "Checkout/GiftCards/Services/GiftCardService.cs",
            "Checkout/GiftCards/Services/IGiftCardService.cs",
            "Checkout/Rules/Impl/GiftCardRule.cs",
            "Checkout/GiftCards/Domain/GiftCardCouponCodeConverter.cs",
            "Checkout/GiftCards/Domain/GiftCardQueryItem.cs",
            "Checkout/GiftCards/Domain/GiftCardInfo.cs",
            "Checkout/GiftCards/Domain/GiftCardType.cs",
            "Checkout/Payment/Domain/PaymentMethod.cs",
            "Checkout/Payment/Service/IPaymentService.cs",
            "Checkout/Payment/Service/PaymentService.cs",
            "Checkout/Payment/Service/IPaymentMethod.cs",
            "Checkout/Payment/Service/PaymentMethodBase.cs",
            "Checkout/Payment/Service/IUcpPaymentHandler.cs",
            "Checkout/Payment/Configuration/PaymentSettings.cs",
            "Checkout/Rules/Impl/PaymentMethodRule.cs",
            "Checkout/Rules/Impl/PaidByRule.cs",
            "Checkout/Shipping/Domain/ShippingMethod.cs",
            "Checkout/Shipping/Domain/ShippingOption.cs",
            "Checkout/Shipping/Services/IShippingService.cs",
            "Checkout/Shipping/Services/ShippingService.cs",
            "Checkout/Shipping/Services/IShippingRateComputationMethod.cs",
            "Checkout/Shipping/Tracking/IShipmentTracker.cs",
            "Checkout/Rules/Impl/ShippingMethodRule.cs",
            "Checkout/Rules/Impl/Cart/CartWeightRule.cs",
            "Checkout/Shipping/Domain/Shipment.cs",
            "Checkout/Shipping/Domain/ShipmentItem.cs",
            "Checkout/Shipping/Hooks/ShipmentHook.cs",
            "Checkout/Shipping/Configuration/ShippingSettings.cs",
            "Checkout/Orders/Domain/ReturnCase.cs",
            "Checkout/Orders/Domain/OrderNote.cs",
            "Content/Menus/Services/MenuProviders/CatalogMenuItemProvider.cs",
            "Content/Menus/Services/MenuProviders/CatalogMenuProvider.cs",
            "Content/Menus/Consumers/CatalogMenuInvalidator.cs",
            "Checkout/Payment/Domain/PaymentEnums.cs",
            "Checkout/Shipping/Domain/ShippingStatus.cs",
            "Platform/Identity/Configuration/RewardPointsSettings.cs",
            "Platform/Identity/Domain/RewardPointsHistory.cs",
            "Platform/Identity/Domain/WalletHistory.cs",
            "Checkout/Orders/Domain/Order.cs",
            "Checkout/Orders/Domain/OrderItem.cs",
            "Checkout/Orders/Domain/OrderEnums.cs",
            "Checkout/SmartDbContext.Checkout.cs",
            "Catalog/Products/Domain/ProductBundleItemOrderData.cs",
            "Checkout/Orders/Events/CreateAttachmentsConsumer.cs",
            "Checkout/Orders/Extensions/OrderMessageFactoryExtensions.cs",
            "Platform/DataExchange/Export/Internal/OrderBatchContext.cs",
            "Checkout/Orders/Hooks/ReturnCaseHook.cs",
            "Checkout/Orders/Hooks/OrderNoteHook.cs",
            "Common/Services/DeliveryTimeService.cs",
            "Common/Services/IDeliveryTimeService.cs",
            "Checkout/Rules/Impl/Cart",
            "Checkout/Rules/Impl/Addresses",
            "Checkout/Rules/Impl/ProductOnWishlistRule.cs",
            "Checkout/Rules/Impl/RewardPointsBalanceRule.cs",
            "Checkout/Rules/Impl/OrderCountRule.cs",
            "Checkout/Rules/Impl/SpentAmountRule.cs",
            "Checkout/Rules/Impl/PurchasedProductRule.cs",
            "Checkout/Rules/Impl/PurchasedFromManufacturerRule.cs",
            "Checkout/Cart/Services",
            "Checkout/Cart/Extensions/IShoppingCartValidatorExtensions.cs",
            "Checkout/Cart/Domain/AddToCartContext.cs",
            "Checkout/Cart/Events/MigrateShoppingCartEvent.cs",
            "Checkout/Cart/Domain/ShoppingCartItem.cs",
            "Checkout/Cart/Domain/ShoppingCartType.cs",
            "Checkout/Cart/Domain/ShoppingCart.cs",
            "Checkout/Cart/Domain/OrganizedShoppingCartItem.cs",
            "Checkout/Cart/Extensions",
            "Checkout/Attributes/Services",
            "Checkout/Attributes",
            "Checkout/Cart",
            "Checkout",
            "Catalog",
            "Catalog/Rules",
            "Catalog/Search",
            "Catalog/Bootstrapping/CatalogStarter.cs",
            "Catalog/Products/Services",
            "Catalog/Categories/Services",
            "Catalog/Brands/Services",
            "Catalog/Attributes/Services",
            "Catalog/Permissions.Catalog.cs",
            "Catalog/Configuration/CatalogSettings.cs",
            "Checkout/Orders/Domain/CheckoutProcess.cs",
            "Catalog/Products/Hooks/ProductBundleItemHook.cs",
            "Platform/Installation/InstallationDataSeeder.Products.cs",
            "Catalog/Products/Import/ProductImporter.cs",
            "Catalog/Categories/Import/CategoryImporter.cs",
            "Catalog/Brands/Import/ManufacturerImporter.cs",
            "Platform/DataExchange/Export/DataExporter.DynamicProduct.cs",
            "Platform/DataExchange/Export/Internal/CategoryBatchContext.cs",
            "Platform/DataExchange/Export/Internal/ManufacturerBatchContext.cs",
            "Catalog/Products/Extensions/CatalogMessageFactoryExtensions.cs",
            "Data/Migrations/Builders/AttributesMigrator.cs",
        ):
            assert not (repo / "src/OmmCMS.Core" / path).exists(), f"Retired core source remains: {path}"
        seed = Path("App_Data/Seed")
        for seed_root in (repo / "src/OmmCMS.Web" / seed, root / seed):
            assert not list(seed_root.glob("**/samples/catalog")), "Retired catalogue seed directory remains"
            for name in ("brand", "category", "product", "download"):
                assert not (seed_root / "media" / name).exists(), f"Retired demo media remains: {name}"
            assert (seed_root / "base/core/cms/topics.json").exists(), "CMS starter topics missing"
            assert (seed_root / "base/core/cms/menus.json").exists(), "CMS starter menus missing"
            for path in ("base/core/menus.json", "base/core/stores.json", "base/core/topics.json", "base/core/topics", "cultures/de-DE/core/stores.json", "cultures/de-DE/core/topics.json", "cultures/de-DE/core/topics"):
                assert not (seed_root / path).exists(), f"Retired storefront seed remains: {path}"
        for language in ("en", "de"):
            for name in ("GiftCard.Notification.xml", "ShipmentSent.CustomerNotification.xml", "ShipmentDelivered.CustomerNotification.xml", "Customer.NewOrderNote.xml", "Product.ProductReview.xml", "RecurringPaymentCancelled.StoreOwnerNotification.xml"):
                template = Path("App_Data/EmailTemplates") / language / name
                assert not (repo / "src/OmmCMS.Web" / template).exists(), f"Retired template remains: {template}"
                assert not (root / template).exists(), f"Retired template was published: {template}"
        cart_template = Path("Areas/Admin/Views/Rule/ValueTemplates/CartItemQuantity.cshtml")
        for web_root in (repo / "src/OmmCMS.Web", root):
            assert not (web_root / "Areas/Admin/sitemap.xml").exists(), "Retired admin sitemap remains"
            assert (web_root / "Areas/Admin/ommcms-sitemap.xml").exists(), "CMS admin sitemap missing"
        assert not (repo / "src/OmmCMS.Web" / cart_template).exists()
        assert not (root / cart_template).exists()
        assert not (repo / "src/OmmCMS.Web/Areas/Admin/Models/ShoppingCart").exists()
        for path in ("wwwroot/js/public.refreshcart.js", "wwwroot/js/public.offcanvas-cart.js", "Themes/Flex/wwwroot/_cart.scss", "wwwroot/js/public.product.js", "Areas/Admin/Views/Menu/EditorTemplates/MenuItem.Catalog.cshtml", "Themes/Flex/Views/Shared/EmailTemplates/order.liquid", "Themes/Flex/Views/Shared/EmailTemplates/shipment.liquid"):
            assert not (repo / "src/OmmCMS.Web" / path).exists(), f"Retired cart asset remains: {path}"
            assert not (root / path).exists(), f"Retired cart asset published: {path}"
        print("Core commerce source extraction passed.")
    if "--presentation-trimmed" in sys.argv:
        verify_presentation_extraction()
    if "--residual-commerce-trimmed" in sys.argv:
        verify_residual_extraction()
    if "--admin-commerce-trimmed" in sys.argv:
        verify_remaining_admin_extraction()
    if "--account-trimmed" in sys.argv:
        verify_account_extraction()
    if "--public-controllers-trimmed" in sys.argv:
        verify_public_controllers()
    if "--controllers-trimmed" in sys.argv:
        verify_trimmed_controllers()
    if "--trimmed" in sys.argv:
        verify_trimmed_distribution()
    start()
    page = wait_for("/install?noAutoInstall=true")
    assert "OmmCMS" in page, "CMS installer branding missing"
    assert "https://ommlabs.ommrudraksha.com" in page, "CMS installer website missing"
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
        assert rows("SELECT COUNT(*) FROM __MigrationVersionInfo WHERE Description LIKE '%OmmCMS initial schema%'") == [1], "Initial schema migration missing"
        assert rows("SELECT COUNT(*) FROM __MigrationVersionInfo WHERE Description LIKE '%Core:%'") == [0], "Historical core migrations must not be applied"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName = 'Admin.Common.ResetToDefault'") == [1], "Baseline locale updates missing"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName LIKE 'ShoppingCart.%' OR ResourceName LIKE 'Checkout.%' OR ResourceName LIKE 'Order.%' OR ResourceName LIKE 'Admin.Orders.%'") == [0], "Retired checkout locale resources remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName LIKE 'Admin.Configuration.Settings.GeneralCommon.BankConnectionSettings%'") == [0], "Retired bank locale resources remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName LIKE 'Enums.SwatchPriceDisplayMode.%'") == [0], "Commerce swatch price resources remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName = 'Admin.Catalog' OR ResourceName LIKE 'Admin.Catalog.%' OR ResourceName LIKE 'Admin.Configuration.Settings.Catalog%' OR ResourceName LIKE 'Admin.Configuration.Settings.Price%' OR ResourceName LIKE 'Admin.Configuration.Settings.Tax%' OR ResourceName LIKE 'Admin.CatalogSettings%' OR ResourceName LIKE 'Products.%'") == [0], "Retired catalog locale resources remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName IN ('Common.ShowMore', 'Common.ShowLess', 'Common.Search', 'Admin.Plugins.KnownGroup.Media')") == [4], "Shared CMS labels missing"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName LIKE 'Admin.Configuration.Settings.ShoppingCart%' OR ResourceName LIKE 'Admin.Configuration.Settings.Order%' OR ResourceName LIKE 'Admin.Configuration.Settings.RewardPoints%' OR ResourceName LIKE 'Admin.Promotions.Discounts%' OR ResourceName LIKE 'Account.CustomerOrders%' OR ResourceName LIKE 'Messages.Order%' OR ResourceName LIKE 'Wishlist.%'") == [0], "Commerce-specific resource families remain"
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Currency') WHERE name IN ('RoundNetPrices', 'RoundUnitPrices', 'RoundOrderItemsEnabled', 'RoundOrderTotalEnabled', 'RoundOrderTotalDenominator', 'RoundOrderTotalRule')") == [0], "Commerce currency flags remain"
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Currency') WHERE name IN ('RoundingEnabled', 'RoundToNearestEnabled', 'RoundingDenomination', 'RoundingRule')") == [4], "Generic currency rounding missing"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName IN ('Common.Media.ThumbTitle', 'Common.Media.ThumbAlternateText', 'Admin.Configuration.Currencies.Fields.RoundingEnabled.Validation')") == [3], "CMS media or rounding resources missing"
        assert rows("SELECT COUNT(*) FROM Setting WHERE lower(Name) IN ('mediasettings.productthumbpicturesize', 'mediasettings.productdetailspicturesize', 'mediasettings.cartthumbpicturesize', 'customersettings.hidemyaccountorders', 'customersettings.hidedownloadableproductstab', 'privacysettings.fullnameonproductrequestrequired', 'seosettings.productdescriptionpriority', 'adminareasettings.displayproductpictures')") == [0], "Commerce settings remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName = 'Admin.Configuration.Settings.Media.PreviewThumbnailSize'") == [1], "CMS preview thumbnail label missing"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName LIKE 'Admin.Configuration.Settings.Performance.MaxUnavailableAttributeCombinations%'") == [0], "Retired product variant performance resources remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName LIKE 'Aria.Label.Cart%' OR ResourceName LIKE 'Aria.Label.Checkout%' OR ResourceName LIKE 'Admin.System.Maintenance.DeleteAbandonedCarts%' OR ResourceName LIKE 'Admin.CurrentWishlists%' OR ResourceName IN ('HomePage.Products', 'RSS.RecentlyAddedProducts', 'Providers.FriendlyName.Tax.Free')") == [0], "Final retired commerce resource families remain"
        assert rows("SELECT COUNT(*) FROM LocaleStringResource WHERE ResourceName IN ('Admin.Customers.Customers.Impersonate.Button', 'Search.NoResultsText', 'CookieManager.Dialog.Required.Intro')") == [3], "Retained CMS labels missing"
        retired_catalog_tables = ("product", "category", "manufacturer", "producttemplate", "categorytemplate",
            "manufacturertemplate", "product_category_mapping", "product_manufacturer_mapping",
            "productbundleitem", "productbundleitemattributefilter", "product_mediafile_mapping", "producttag",
            "productattribute", "product_productattribute_mapping", "productvariantattributevalue",
            "productvariantattributecombination", "productattributeoptionsset", "productattributeoption",
            "specificationattribute", "specificationattributeoption", "product_specificationattribute_mapping",
            "ruleset_category_mapping", "deliverytime", "quantityunit", "measuredimension",
            "measureweight", "collectiongroup", "collectiongroupmapping")
        tables = {name.lower() for name in rows("SELECT name FROM sqlite_master WHERE type='table'")}
        assert not tables.intersection(retired_catalog_tables), "Retired catalogue tables remain"
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Country') WHERE name IN ('AllowsBilling', 'AllowsShipping', 'SubjectToVat')") == [0], "Commerce country flags remain"
        assert rows("SELECT COUNT(*) FROM Country")[0] > 0, "Country directory missing"
        assert rows("SELECT COUNT(*) FROM ActivityLogType WHERE SystemKeyword IN ('AddNewProduct', 'EditOrder', 'PublicStore.AddToShoppingCart', 'PublicStore.AddToWishlist')") == [0], "Commerce activity logs remain"
        assert rows("SELECT COUNT(*) FROM ActivityLogType WHERE SystemKeyword='PublicStore.ContactUs'") == [1], "Contact activity logging missing"
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Customer') WHERE name IN ('BillingAddress_Id', 'ShippingAddress_Id')") == [0], "Legacy checkout addresses remain"
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Customer') WHERE name='ContactAddress_Id'") == [1], "Contact address relation missing"
        assert rows("SELECT COUNT(*) FROM Setting WHERE lower(Name) LIKE 'bankconnectionsettings.%'") == [0], "Bank payment settings remain"
        assert rows("SELECT COUNT(*) FROM pragma_table_info('RuleSet') WHERE name='ProductVariantAttributeId'") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='BackInStockSubscription'") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Affiliate'") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('relatedproduct', 'relatedproducts', 'crosssellproduct', 'crosssellproducts')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Customer') WHERE name='AffiliateId'") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name='AllowBackInStockSubscriptions'") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name)='productreviewhelpfulness'") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name)='productreview'") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name IN ('AllowCustomerReviews', 'ApprovedRatingSum', 'NotApprovedRatingSum', 'ApprovedTotalReviews', 'NotApprovedTotalReviews')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('recurringpayment', 'recurringpayments', 'recurringpaymenthistory', 'recurringpaymenthistories')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name IN ('IsRecurring', 'RecurringCycleLength', 'RecurringCyclePeriodId', 'RecurringTotalCycles')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('order', 'orderitem', 'rewardpointshistory', 'wallethistory')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('giftcard', 'giftcards', 'giftcardusagehistory', 'giftcardusagehistories')") == [0]
        assert rows("SELECT COUNT(*) FROM MessageTemplate WHERE Name = 'GiftCard.Notification'") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name IN ('IsGiftCard', 'GiftCardTypeId')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('paymentmethod', 'paymentmethods', 'ruleset_paymentmethod_mapping')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('shippingmethod', 'shippingmethods', 'ruleset_shippingmethod_mapping')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('shipment', 'shipments', 'shipmentitem', 'shipmentitems', 'returncase', 'returncases', 'ordernote', 'ordernotes')") == [0]
        assert rows("SELECT COUNT(*) FROM MessageTemplate WHERE Name IN ('ShipmentSent.CustomerNotification', 'ShipmentDelivered.CustomerNotification', 'Customer.NewOrderNote')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name)='newslettersubscription'") == [1]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('shoppingcartitem', 'shoppingcartitems')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('checkoutattribute', 'checkoutattributes', 'checkoutattributevalue', 'checkoutattributevalues', 'ruleset_checkoutattribute_mapping')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('tierprice', 'tierprices')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) IN ('pricelabel', 'pricelabels')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name IN ('OldPrice', 'ComparePrice', 'ComparePriceLabelId')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name IN ('BasePriceEnabled', 'BasePriceMeasureUnit', 'BasePriceAmount', 'BasePriceBaseAmount', 'CallForPrice', 'CustomerEntersPrice', 'MinimumCustomerEnteredPrice', 'MaximumCustomerEnteredPrice')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('ProductVariantAttributeCombination') WHERE name IN ('BasePriceAmount', 'BasePriceBaseAmount')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('ProductVariantAttributeCombination') WHERE name IN ('ComparePrice', 'OldPrice')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name IN ('SpecialPrice', 'SpecialPriceStartDateTimeUtc', 'SpecialPriceEndDateTimeUtc')") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('ProductBundleItem') WHERE name IN ('Discount', 'DiscountPercentage')") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND lower(name) LIKE '%discount%'") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Order') WHERE name IN ('CheckoutAttributeDescription', 'CheckoutAttributesXml')") == [0]
        topics = set(rows("SELECT SystemName FROM Topic"))
        assert {"HomePageText", "AboutUs", "ContactUs"} <= topics
        assert not topics.intersection({"PaymentInfo", "ShippingInfo", "CheckoutAsGuestOrRegister"})
        assert "catalog" not in rows("SELECT ProviderName FROM MenuItemRecord")
        tasks = set(rows("SELECT Type FROM ScheduleTask"))
        assert tasks == {"QueuedMessagesSendTask", "QueuedMessagesClearTask", "DeleteGuestsTask", "DeleteLogsTask",
                         "ClearCacheTask", "TransientMediaClearTask", "TempFileCleanupTask",
                         "RebuildXmlSitemapTask", "TargetGroupEvaluatorTask"}, "Unexpected scheduled jobs in fresh CMS"
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
    if "--public-controllers-trimmed" in sys.argv:
        for path in ("/catalog/manufacturerall", "/product/productdetails/1", "/checkout/billingaddress", "/shoppingcart/cart", "/order/details/1", "/returncase/returncase/1", "/download/getdownload/1", "/search/search"):
            assert request(path)[0] == 404, f"Former public commerce endpoint reachable: {path}"
        print("Extracted public commerce endpoints return HTTP 404.")
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
