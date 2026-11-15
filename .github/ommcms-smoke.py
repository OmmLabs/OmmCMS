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
    if "--presentation-trimmed" in sys.argv:
        for path in ("/pdf/receiptheader", "/pdf/receiptfooter"):
            assert request(path, client=admin)[0] == 404, f"Retired receipt endpoint reachable: {path}"
    if "--residual-commerce-trimmed" in sys.argv:
        for path in ("/currency-selected/1", "/admin/customer/reports", "/admin/customer/rewardpointssettings", "/admin/customer/rewardpointsforpurchasesinfo"):
            assert request(path, client=admin)[0] == 404, f"Residual commerce action reachable: {path}"
        for entity_type in ("product", "category", "manufacturer"):
            assert request("/entity/picker?entityType=" + entity_type, client=admin)[0] == 404
        get_page("/entity/picker?entityType=customer", admin, "CustomerSearchType")
        print("Residual commerce routes absent; authorized customer picker remains available.")
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
        get_page("/customer/addresses", admin, "add-address-button")
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


def verify_trimmed_distribution():
    removed = ["Smartstore.AmazonPay","Smartstore.OfflinePayment","Smartstore.PayPal","Smartstore.Stripe","Smartstore.Shipping","Smartstore.ShippingByWeight","Smartstore.Tax","Smartstore.Google.MerchantCenter","Smartstore.Clickatell","Smartstore.WebApi"]
    retained = ["Smartstore.Apple.Auth", "Smartstore.Facebook.Auth", "Smartstore.Google.Auth",
                "Smartstore.Microsoft.Auth", "Smartstore.Twitter.Auth", "Smartstore.DevTools",
                "Smartstore.Google.Analytics"]
    repo = Path(__file__).resolve().parent.parent
    module_sources = repo / "src" / "Smartstore.Modules"
    modules = root / "Modules"
    solution = (repo / "OmmCMS.sln").read_text(encoding="utf-8-sig")
    for name in removed:
        assert not (module_sources / name).exists(), f"Retired source remains: {name}"
        assert not (modules / name).exists(), f"Retired module is in publish output: {name}"
        assert f'{name}.csproj' not in solution, f"Retired project is in solution: {name}"
    assert not (repo / "tools" / "Smartstore.WebApi.Client").exists(), "Obsolete API client remains"
    for name in retained:
        assert (modules / name / "module.json").is_file(), f"Retained module is absent: {name}"
    print(f"Trimmed distribution passed: {len(removed)} retired modules absent; {len(retained)} retained modules present.")


def verify_trimmed_controllers():
    names = ["Affiliate","BarcodeTester","CheckoutAttribute","CollectionGroup","DeliveryTime","GiftCard","PriceLabel","ProductReview","QuantityUnit","RecurringPayment","ReturnCase","Shipment","Shipping","Tax","Payment","ShoppingCart","SpecificationAttribute","ProductAttribute"]
    source = Path(__file__).resolve().parent.parent / "src" / "Smartstore.Web" / "Areas" / "Admin"
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
    source = Path(__file__).resolve().parent.parent / "src" / "Smartstore.Web"
    for name in ["Catalog","Checkout","Download","Order","Product","ReturnCase","Search","ShoppingCart"]:
        assert not (source / "Controllers" / (name + "Controller.cs")).exists(), f"Retired public controller remains: {name}"
    for path in ["Views/Catalog/CategoryTemplate.ProductsInGridOrLines.cshtml","Views/Catalog/CompareProducts.cshtml","Views/Catalog/ManufacturerAll.cshtml","Views/Catalog/ManufacturerTemplate.ProductsInGridOrLines.cshtml","Views/Catalog/ProductTagsAll.cshtml","Views/Catalog/ProductsByTag.cshtml","Views/Catalog/RecentlyAddedProducts.cshtml","Views/Catalog/RecentlyViewedProducts.cshtml","Views/Checkout/BillingAddress.cshtml","Views/Checkout/Completed.cshtml","Views/Checkout/Confirm.cshtml","Views/Checkout/PaymentMethod.cshtml","Views/Checkout/ShippingAddress.cshtml","Views/Checkout/ShippingMethod.cshtml","Views/Checkout/Terminal.Confirm.cshtml","Views/Order/Details.cshtml","Views/Order/ShipmentDetails.cshtml","Views/Product/AskQuestion.cshtml","Views/Product/BackInStockSubscribePopup.cshtml","Views/Product/EmailAFriend.cshtml","Views/Product/Product.cshtml","Views/Product/Reviews.cshtml","Views/ReturnCase/ReturnCase.cshtml","Views/Search/Search.cshtml","Views/ShoppingCart/Cart.cshtml","Views/ShoppingCart/EmailWishlist.cshtml","Views/ShoppingCart/Wishlist.cshtml"]:
        assert not (source / path).exists(), f"Retired source view remains: {path}"
        assert not (root / path).exists(), f"Retired published view remains: {path}"
    print("Public commerce source and publish extraction passed.")


def verify_account_extraction():
    repo = Path(__file__).resolve().parent.parent
    for path in ["src/Smartstore.Modules/Smartstore.DevTools/Controllers/MyCheckoutController.cs","src/Smartstore.Modules/Smartstore.DevTools/Views/MyCheckout/MyBillingAddress.cshtml","src/Smartstore.Modules/Smartstore.Google.Analytics/Filters/CheckoutFilter.cs","src/Smartstore.Web/Models/Customers/CustomerDownloadableProductsModel.cs","src/Smartstore.Web/Models/Customers/CustomerOrderListModel.cs","src/Smartstore.Web/Models/Customers/CustomerReturnCasesModel.cs","src/Smartstore.Web/Models/Customers/CustomerRewardPointsModel.cs","src/Smartstore.Web/Models/Customers/CustomerStockSubscriptionsModel.cs","src/Smartstore.Web/Views/Customer/DownloadableProducts.cshtml","src/Smartstore.Web/Views/Customer/Orders.cshtml","src/Smartstore.Web/Views/Customer/Partials/RecurringPayments.cshtml","src/Smartstore.Web/Views/Customer/ReturnCases.cshtml","src/Smartstore.Web/Views/Customer/RewardPoints.cshtml","src/Smartstore.Web/Views/Customer/StockSubscriptions.cshtml","src/Smartstore.Web/Views/Customer/UserAgreement.cshtml","src/Smartstore.Modules/Smartstore.DevTools/Filters/Samples/SampleCheckoutFilter.cs"]:
        assert not (repo / path).exists(), f"Retired account source remains: {path}"
        if path.startswith("src/Smartstore.Web/Views/"):
            assert not (root / path.removeprefix("src/Smartstore.Web/")).exists(), f"Retired account view was published: {path}"
    print("Account dependency source and publish extraction passed.")


def verify_remaining_admin_extraction():
    source = Path(__file__).resolve().parent.parent / "src" / "Smartstore.Web" / "Areas" / "Admin"
    for path in ["Controllers/CampaignController.cs","Controllers/CategoryController.cs","Controllers/CurrencyController.cs","Controllers/DiscountController.cs","Controllers/ExportController.cs","Controllers/ImportController.cs","Controllers/ManufacturerController.cs","Controllers/MeasureController.cs","Controllers/OrderController.cs","Controllers/ProductController.AssignableProducts.cs","Controllers/ProductController.Attributes.cs","Controllers/ProductController.Grid.cs","Controllers/ProductController.RecycleBin.cs","Controllers/ProductController.Settings.cs","Controllers/ProductController.cs","Controllers/SearchController.cs","Views/Campaign/Create.cshtml","Views/Campaign/Edit.cshtml","Views/Campaign/List.cshtml","Views/Campaign/_CreateOrUpdate.cshtml","Views/Campaign/_Grid.Campaigns.cshtml","Views/Category/Create.cshtml","Views/Category/Edit.cshtml","Views/Category/EditorTemplates/CategoryProduct.cshtml","Views/Category/List.cshtml","Views/Category/Tree.cshtml","Views/Category/_CreateOrUpdate.cshtml","Views/Category/_DeleteCategoryDialog.cshtml","Views/Category/_Grid.Categories.Search.cshtml","Views/Category/_Grid.Categories.cshtml","Views/Category/_Grid.ProductCategories.cshtml","Views/Category/_ViewImports.cshtml","Views/Currency/Create.cshtml","Views/Currency/Edit.cshtml","Views/Currency/List.cshtml","Views/Currency/_CreateOrUpdate.cshtml","Views/Currency/_Grid.Currencies.cshtml","Views/Currency/_Grid.LiveRates.cshtml","Views/Discount/Create.cshtml","Views/Discount/Edit.cshtml","Views/Discount/List.cshtml","Views/Discount/_CreateOrUpdate.cshtml","Views/Discount/_Grid.DiscountUsageHistory.cshtml","Views/Discount/_Grid.Discounts.Search.cshtml","Views/Discount/_Grid.Discounts.cshtml","Views/Discount/_ViewImports.cshtml","Views/Export/Create.cshtml","Views/Export/CreateDeployment.cshtml","Views/Export/Edit.cshtml","Views/Export/EditDeployment.cshtml","Views/Export/List.cshtml","Views/Export/Preview.cshtml","Views/Export/ProfileFileCount.cshtml","Views/Export/ProfileFileDetails.cshtml","Views/Export/_CreateOrUpdate.Deployment.cshtml","Views/Export/_Grid.Preview.Category.cshtml","Views/Export/_Grid.Preview.Customer.cshtml","Views/Export/_Grid.Preview.Manufacturer.cshtml","Views/Export/_Grid.Preview.NewsLetterSubscription.cshtml","Views/Export/_Grid.Preview.Order.cshtml","Views/Export/_Grid.Preview.Product.cshtml","Views/Export/_Grid.Preview.ShoppingCartItem.cshtml","Views/Export/_ProfileList.cshtml","Views/Export/_ResolveTokensExample.cshtml","Views/Export/_Tab.Deployment.cshtml","Views/Export/_Tab.Filter.cshtml","Views/Export/_Tab.Projection.cshtml","Views/Export/_ViewImports.cshtml","Views/Import/DataExchangeSettings.cshtml","Views/Import/Edit.cshtml","Views/Import/List.cshtml","Views/Import/ProfileImportResult.cshtml","Views/Import/_ColumnMappings.cshtml","Views/Import/_CsvConfiguration.cshtml","Views/Import/_Update.cshtml","Views/Import/_ViewImports.cshtml","Views/Manufacturer/Create.cshtml","Views/Manufacturer/Edit.cshtml","Views/Manufacturer/EditorTemplates/ManufacturerProduct.cshtml","Views/Manufacturer/List.cshtml","Views/Manufacturer/_CreateOrUpdate.cshtml","Views/Manufacturer/_Grid.Manufacturers.Search.cshtml","Views/Manufacturer/_Grid.Manufacturers.cshtml","Views/Manufacturer/_Grid.ProductManufacturers.cshtml","Views/Manufacturer/_ViewImports.cshtml","Views/Measure/CreateDimensionPopup.cshtml","Views/Measure/CreateWeightPopup.cshtml","Views/Measure/Dimensions.cshtml","Views/Measure/EditDimensionPopup.cshtml","Views/Measure/EditWeightPopup.cshtml","Views/Measure/Weights.cshtml","Views/Measure/_CreateOrUpdateDimension.cshtml","Views/Measure/_CreateOrUpdateWeight.cshtml","Views/Measure/_Grid.MeasureDimension.cshtml","Views/Measure/_Grid.MeasureWeight.cshtml","Views/Order/AddProductToOrder.cshtml","Views/Order/AddressEdit.cshtml","Views/Order/BestsellersReport.cshtml","Views/Order/Edit.cshtml","Views/Order/Grids/_Grid.Bestsellers.Search.cshtml","Views/Order/Grids/_Grid.Bestsellers.cshtml","Views/Order/Grids/_Grid.NeverSold.Search.cshtml","Views/Order/Grids/_Grid.NeverSold.cshtml","Views/Order/Grids/_Grid.OrderNotes.cshtml","Views/Order/Grids/_Grid.Orders.Search.cshtml","Views/Order/Grids/_Grid.Orders.cshtml","Views/Order/List.cshtml","Views/Order/NeverSoldReport.cshtml","Views/Order/OrderSettings.cshtml","Views/Order/PartiallyRefundOrderPopup.cshtml","Views/Order/UploadLicenseFilePopup.cshtml","Views/Order/_AddProductToOrder.Attributes.cshtml","Views/Order/_AddProductToOrder.GiftCardInfo.cshtml","Views/Order/_AddProductToOrder.Prices.cshtml","Views/Order/_Edit.BillingAndShipment.cshtml","Views/Order/_Edit.Info.cshtml","Views/Order/_Edit.OrderTotals.cshtml","Views/Order/_Edit.Payment.cshtml","Views/Order/_Edit.Products.cshtml","Views/Order/_UpdateOrderItemDialog.cshtml","Views/Order/_ViewImports.cshtml","Views/Product/AttributeCombinationCreatePopup.cshtml","Views/Product/AttributeCombinationEditPopup.cshtml","Views/Product/BundleItemEditPopup.cshtml","Views/Product/CatalogSettings.cshtml","Views/Product/Create.cshtml","Views/Product/DeletedProductDetails.cshtml","Views/Product/Edit.cshtml","Views/Product/EditAttributeValues.cshtml","Views/Product/EditGroupedProductConfiguration.cshtml","Views/Product/EditProductTag.cshtml","Views/Product/EditorTemplates/AttributeControlType.cshtml","Views/Product/EditorTemplates/ProductAttribute.cshtml","Views/Product/EditorTemplates/ProductCategory.cshtml","Views/Product/EditorTemplates/ProductManufacturer.cshtml","Views/Product/EditorTemplates/TierPriceCalculationMethod.cshtml","Views/Product/EditorTemplates/TierPriceCustomer.cshtml","Views/Product/EditorTemplates/TierPriceStore.cshtml","Views/Product/Grids/_Grid.AssociatedProducts.cshtml","Views/Product/Grids/_Grid.AttributeCombinations.cshtml","Views/Product/Grids/_Grid.AttributeValues.cshtml","Views/Product/Grids/_Grid.BundleItems.cshtml","Views/Product/Grids/_Grid.Categories.cshtml","Views/Product/Grids/_Grid.CrossSellProducts.cshtml","Views/Product/Grids/_Grid.DeletedProducts.Search.cshtml","Views/Product/Grids/_Grid.DeletedProducts.cshtml","Views/Product/Grids/_Grid.LowStockReport.cshtml","Views/Product/Grids/_Grid.Manufacturers.cshtml","Views/Product/Grids/_Grid.ProductTags.Search.cshtml","Views/Product/Grids/_Grid.ProductTags.cshtml","Views/Product/Grids/_Grid.ProductVariantAttribute.cshtml","Views/Product/Grids/_Grid.Products.Search.cshtml","Views/Product/Grids/_Grid.Products.cshtml","Views/Product/Grids/_Grid.RelatedProducts.cshtml","Views/Product/Grids/_Grid.SpecificationAttributes.cshtml","Views/Product/Grids/_Grid.TierPrices.cshtml","Views/Product/List.cshtml","Views/Product/LowStockReport.cshtml","Views/Product/Partials/GroupedProductSettings.cshtml","Views/Product/Partials/PriceSettings.cshtml","Views/Product/ProductAttributeSwatchEditPopup.cshtml","Views/Product/ProductAttributeValueCreatePopup.cshtml","Views/Product/ProductAttributeValueEditPopup.cshtml","Views/Product/ProductTags.cshtml","Views/Product/RecycleBin.cshtml","Views/Product/_CopyAttributeOptionsDialog.cshtml","Views/Product/_CopyProductDialog.cshtml","Views/Product/_Create.SaveFirst.cshtml","Views/Product/_CreateOrUpdate.AssociatedProducts.cshtml","Views/Product/_CreateOrUpdate.Attributes.cshtml","Views/Product/_CreateOrUpdate.BundleItems.cshtml","Views/Product/_CreateOrUpdate.Categories.cshtml","Views/Product/_CreateOrUpdate.Downloads.cshtml","Views/Product/_CreateOrUpdate.Info.cshtml","Views/Product/_CreateOrUpdate.Inventory.cshtml","Views/Product/_CreateOrUpdate.Manufacturers.cshtml","Views/Product/_CreateOrUpdate.Orders.cshtml","Views/Product/_CreateOrUpdate.Pictures.cshtml","Views/Product/_CreateOrUpdate.Price.cshtml","Views/Product/_CreateOrUpdate.Promotion.cshtml","Views/Product/_CreateOrUpdate.SEO.cshtml","Views/Product/_CreateOrUpdate.SpecificationAttributes.cshtml","Views/Product/_CreateOrUpdate.cshtml","Views/Product/_CreateOrUpdateAttributeCombinationPopup.cshtml","Views/Product/_CreateOrUpdateProductAttributeValue.cshtml","Views/Product/_CreateOrUpdateProductTag.cshtml","Views/Product/_ProductAttributes.cshtml","Views/Product/_RestoreProductDialog.cshtml","Views/Product/_ViewImports.cshtml","Views/Search/SearchCommonFacet.cshtml","Views/Search/SearchSettings.cshtml","Views/Shipment/_Grid.Shipments.Search.cshtml","Views/Shipment/_Grid.Shipments.cshtml","Views/Shipment/_ViewImports.cshtml","Views/Customer/Grids/_Grid.RewardPoints.cshtml","Views/Customer/Grids/_Grid.ShoppingCart.cshtml","Views/Customer/Grids/_Grid.Wishlist.cshtml"]:
        assert not (source / path).exists(), f"Retired admin source remains: {path}"
        if path.startswith("Views/"):
            assert not (root / "Areas" / "Admin" / path).exists(), f"Retired admin view was published: {path}"
    assert (source / "Controllers" / "PackagingController.cs").is_file(), "CMS package installer source missing"
    print("Remaining admin commerce source and published views extraction passed.")


def verify_residual_extraction():
    repo = Path(__file__).resolve().parent.parent
    for path in ["src/Smartstore.Web/Components/CheckoutProgressViewComponent.cs","src/Smartstore.Web/Components/CrossSellProductsViewComponent.cs","src/Smartstore.Web/Components/CurrencySelectorViewComponent.cs","src/Smartstore.Web/Components/HomeBestsellersViewComponent.cs","src/Smartstore.Web/Components/HomeBrandsViewComponent.cs","src/Smartstore.Web/Components/HomeCategoriesViewComponent.cs","src/Smartstore.Web/Components/HomeProductsViewComponent.cs","src/Smartstore.Web/Components/OffCanvasCartViewComponent.cs","src/Smartstore.Web/Components/OrderSummaryViewComponent.cs","src/Smartstore.Web/Components/OrderTotalsViewComponent.cs","src/Smartstore.Web/Components/PopularProductTagsViewComponent.cs","src/Smartstore.Web/Components/RecentlyViewedProductsViewComponent.cs","src/Smartstore.Web/Views/Shared/Components/CheckoutProgress/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/CrossSellProducts/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/CurrencySelector/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/HomeBestSellers/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/HomeBrands/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/HomeCategories/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/HomeProducts/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/OffCanvasCart/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/OrderSummary/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/OrderTotals/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/OrderTotals/OrderTotalOnly.cshtml","src/Smartstore.Web/Views/Shared/Components/PopularProductTags/Default.cshtml","src/Smartstore.Web/Views/Shared/Components/RecentlyViewedProducts/Default.cshtml","src/Smartstore.Web/Models/Checkout/CheckoutProgressModel.cs","src/Smartstore.Web/Models/ShoppingCart/OffCanvasCartModel.cs","src/Smartstore.Web/Models/ShoppingCart/OrderTotalsModel.cs","src/Smartstore.Web/Models/Catalog/PopularProductTagsModel.cs","src/Smartstore.Web/Views/Menu/OffCanvas.Brands.cshtml","src/Smartstore.Web/Views/Checkout/Partials/CheckoutOrderTotals.cshtml","src/Smartstore.Web/Views/Checkout/Partials/ConfirmDetails.cshtml","src/Smartstore.Web/Views/Checkout/Partials/Terminal.ConfirmDetails.cshtml","src/Smartstore.Modules/Smartstore.Google.Analytics/Events.cs","src/Smartstore.Web/Areas/Admin/Models/Customers/RewardPointsSettingsModel.cs","src/Smartstore.Web/Areas/Admin/Models/Customers/TopCustomersReportModel.cs","src/Smartstore.Web/Areas/Admin/Views/Customer/Grids/_Grid.BestCustomers.Search.cshtml","src/Smartstore.Web/Areas/Admin/Views/Customer/Grids/_Grid.BestCustomers.cshtml","src/Smartstore.Web/Areas/Admin/Views/Customer/Reports.cshtml","src/Smartstore.Web/Areas/Admin/Views/Customer/RewardPointsSettings.cshtml"]:
        assert not (repo / path).exists(), f"Retired source remains: {path}"
        if path.startswith("src/Smartstore.Web/") and path.endswith(".cshtml"):
            assert not (root / path.removeprefix("src/Smartstore.Web/")).exists(), f"Retired view published: {path}"
    print("Residual commerce source and publish extraction passed.")


def verify_presentation_extraction():
    repo = Path(__file__).resolve().parent.parent
    for path in [
        "src/Smartstore.Core/Catalog/Products/Services/IProductCloner.cs",
        "src/Smartstore.Core/Catalog/Products/Services/IProductCompareService.cs",
        "src/Smartstore.Core/Catalog/Products/Services/IRecentlyViewedProductsService.cs",
        "src/Smartstore.Core/Catalog/Products/Services/ProductCloner.cs",
        "src/Smartstore.Core/Catalog/Products/Services/ProductCompareService.cs",
        "src/Smartstore.Core/Catalog/Products/Services/RecentlyViewedProductsService.cs",
        "src/Smartstore.Modules/Smartstore.DevTools/Filters/Samples/SampleProductDetailActionFilter.cs",
        "src/Smartstore.Modules/Smartstore.DevTools/Filters/Samples/SampleResultFilter.cs",
        "src/Smartstore.Web.Common/Models/Pdf/Mappers/PdfReceiptMapper.cs",
        "src/Smartstore.Web.Common/Models/Pdf/PdfReceiptSectionModel.cs",
        "src/Smartstore.Web.Common/Models/Pdf/PdfSectionVariables.cs",
        "src/Smartstore.Web/Components/FacetGroupViewComponent.cs",
        "src/Smartstore.Web/Components/SearchBoxViewComponent.cs",
        "src/Smartstore.Web/Controllers/CatalogHelper.Price.cs",
        "src/Smartstore.Web/Controllers/CatalogHelper.Product.cs",
        "src/Smartstore.Web/Controllers/CatalogHelper.ProductSummary.cs",
        "src/Smartstore.Web/Controllers/CatalogHelper.cs",
        "src/Smartstore.Web/Controllers/OrderHelper.cs",
        "src/Smartstore.Web/Controllers/PdfController.cs",
        "src/Smartstore.Web/Models/Catalog/BackInStockSubscribeModel.cs",
        "src/Smartstore.Web/Models/Catalog/BrandModel.cs",
        "src/Smartstore.Web/Models/Catalog/BrandNavigationModel.cs",
        "src/Smartstore.Web/Models/Catalog/BrandOverviewModel.cs",
        "src/Smartstore.Web/Models/Catalog/CategoryModel.cs",
        "src/Smartstore.Web/Models/Catalog/CategorySummaryModel.cs",
        "src/Smartstore.Web/Models/Catalog/DeliveryTimeModel.cs",
        "src/Smartstore.Web/Models/Catalog/Extensions/JsonLdExtensions.cs",
        "src/Smartstore.Web/Models/Catalog/GroupedProductModel.cs",
        "src/Smartstore.Web/Models/Catalog/IQuantityInput.cs",
        "src/Smartstore.Web/Models/Catalog/Mappers/BrandMetaPropertyMapper.cs",
        "src/Smartstore.Web/Models/Catalog/Mappers/CategoryMetaPropertyMapper.cs",
        "src/Smartstore.Web/Models/Catalog/Mappers/ProductMetaPropertyMapper.cs",
        "src/Smartstore.Web/Models/Catalog/Mappers/ProductQuantityInputMapper.cs",
        "src/Smartstore.Web/Models/Catalog/Mappers/QuantityInputMapperBase.cs",
        "src/Smartstore.Web/Models/Catalog/PriceModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductAskQuestionModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductDetailsModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductDetailsModelContext.cs",
        "src/Smartstore.Web/Models/Catalog/ProductEmailAFriendModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductReviewsModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductSpecificationModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductSummaryItemModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductSummaryModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductTagModel.cs",
        "src/Smartstore.Web/Models/Catalog/ProductsByTagModel.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutAddressModel.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutCompletedModel.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutConfirmModel.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutModelBase.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutPaymentMethodModel.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutRefreshModel.cs",
        "src/Smartstore.Web/Models/Checkout/CheckoutShippingMethodModel.cs",
        "src/Smartstore.Web/Models/Checkout/Mappers/CheckoutAddressMapper.cs",
        "src/Smartstore.Web/Models/Checkout/Mappers/CheckoutConfirmMapper.cs",
        "src/Smartstore.Web/Models/Checkout/Mappers/CheckoutPaymentMethodMapper.cs",
        "src/Smartstore.Web/Models/Checkout/Mappers/CheckoutShippingMethodMapper.cs",
        "src/Smartstore.Web/Models/Orders/AddReturnCaseModel.cs",
        "src/Smartstore.Web/Models/Orders/Mappers/ReturnCaseItemsMapper.cs",
        "src/Smartstore.Web/Models/Orders/OrderDetailsModel.cs",
        "src/Smartstore.Web/Models/Orders/ReturnCaseItemsModel.cs",
        "src/Smartstore.Web/Models/Orders/ReturnCaseModel.cs",
        "src/Smartstore.Web/Models/Orders/ShipmentDetailsModel.cs",
        "src/Smartstore.Web/Models/Search/ISearchResultModel.cs",
        "src/Smartstore.Web/Models/Search/SearchResultModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/ButtonPaymentMethodModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/CartModelBase.cs",
        "src/Smartstore.Web/Models/ShoppingCart/EstimateShippingModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/CartItemImageMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/CartItemMapperBase.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/CartItemQuantityInputMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/CartItemSorter.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/CartMapperBase.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/MiniShoppingCartMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/ShoppingCartItemMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/ShoppingCartMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/WishlistItemMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/Mappers/WishlistMapper.cs",
        "src/Smartstore.Web/Models/ShoppingCart/MiniShoppingCartModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/ShoppingCartModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/UpdateCartItemModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/WishlistEmailAFriendModel.cs",
        "src/Smartstore.Web/Models/ShoppingCart/WishlistModel.cs",
        "src/Smartstore.Web/Views/Catalog/Partials/OffCanvasCompare.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/AddAddressButton.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/AddressScript.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/ConfirmScript.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/OrderReviewData.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/PaymentMethodList.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/ShippingAddressDiffers.cshtml",
        "src/Smartstore.Web/Views/Checkout/Partials/ShippingMethodList.cshtml",
        "src/Smartstore.Web/Views/Order/Details.Print.cshtml",
        "src/Smartstore.Web/Views/Pdf/ReceiptFooter.cshtml",
        "src/Smartstore.Web/Views/Pdf/ReceiptHeader.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.AlsoPurchased.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.AssociatedProduct.Header.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.AssociatedProduct.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.AssociatedProducts.Search.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.AssociatedProducts.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Attrs.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.AvailablePaymentMethods.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Bundle.Items.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Bundle.Price.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Bundle.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.CustomerPrice.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.GiftCard.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Info.Mobile.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Info.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Media.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Offer.Actions.Misc.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Offer.Actions.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Offer.Price.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Offer.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.RelatedProducts.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.ReviewSummary.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Reviews.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Specs.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.StockInfo.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Tabs.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Tags.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.TierPrices.cshtml",
        "src/Smartstore.Web/Views/Product/Partials/Product.Variants.cshtml",
        "src/Smartstore.Web/Views/Search/Partials/InstantSearch.HitGroup.cshtml",
        "src/Smartstore.Web/Views/Search/Partials/InstantSearch.TopProducts.cshtml",
        "src/Smartstore.Web/Views/Search/Partials/InstantSearch.cshtml",
        "src/Smartstore.Web/Views/Shared/Components/FacetGroup/MultiSelect.cshtml",
        "src/Smartstore.Web/Views/Shared/Components/FacetGroup/Price.cshtml",
        "src/Smartstore.Web/Views/Shared/Components/FacetGroup/Rating.cshtml",
        "src/Smartstore.Web/Views/Shared/Components/FacetGroup/SingleSelect.cshtml",
        "src/Smartstore.Web/Views/Shared/Components/SearchBox/Default.cshtml",
        "src/Smartstore.Web/Views/Shared/EditorTemplates/QtyInput.cshtml",
        "src/Smartstore.Web/Views/Shared/Layouts/_Checkout.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/CartItem.BundleItems.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/CartItem.Details.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/CartItem.Price.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/CartItems.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/CartPaymentButtons.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/CartWarnings.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.FilterSort.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.Brand.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.Buttons.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.Delivery.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.Price.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.Rating.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.Variants.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Item.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.Pager.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.ViewMode.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Product.List.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/ReturnCase.Infos.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/ReturnCase.Items.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Search.ActiveFilters.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/Search.Filters.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/SearchResultHitsCount.cshtml",
        "src/Smartstore.Web/Views/Shared/Partials/_CommentBox.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/EstimateShipping.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/OffCanvasShoppingCart.Items.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/OffCanvasShoppingCart.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/OffCanvasWishlist.Items.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/OffCanvasWishlist.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/WishlistItems.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/_CheckoutAttributes.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/_DiscountBox.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/_GiftCardBox.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/Partials/_RewardPointsBox.cshtml",
        "src/Smartstore.Web/Views/ShoppingCart/_ViewImports.cshtml",
        "test/Smartstore.Web.Tests/Web/CartItemSorterTests.cs",
        "test/Smartstore.Web.Tests/Web/CatalogHelperPriceTests.cs",
        "src/Smartstore.Web.Common/Models/Catalog/IListActions.cs",
        "src/Smartstore.Web.Common/Models/Search/SearchBoxModel.cs",
        "src/Smartstore.Modules/Smartstore.DevTools/Components/CustomFacetViewComponent.cs",
        "src/Smartstore.Modules/Smartstore.DevTools/Services/CustomFacetTemplateSelector.cs",
        "src/Smartstore.Modules/Smartstore.DevTools/Views/Shared/Components/CustomFacet/Default.cshtml",
        "src/Smartstore.Core/Catalog/Attributes/Services/IProductAttributeService.cs",
        "src/Smartstore.Core/Catalog/Attributes/Services/ProductAttributeService.cs",
    ]:
        assert not (repo / path).exists(), f"Retired source remains: {path}"
        if path.startswith("src/Smartstore.Web/") and path.endswith(".cshtml"):
            assert not (root / path.removeprefix("src/Smartstore.Web/")).exists(), f"Retired view published: {path}"
    print("Commerce presentation and detached core services extraction passed.")


try:
    if "--core-commerce-trimmed" in sys.argv:
        repo = Path(__file__).resolve().parent.parent
        for path in (
            "Catalog/Products/Services/IStockSubscriptionService.cs",
            "Catalog/Products/Services/StockSubscriptionService.cs",
            "Catalog/Products/Extensions/BackInStockSubscriptionQueryExtensions.cs",
            "Catalog/Attributes/Modelling/IProductVariantQueryFactory.cs",
            "Catalog/Attributes/Modelling/ProductVariantQueryFactory.cs",
            "Catalog/Attributes/Modelling/ProductAttributeQueryModelBinder.cs",
        ):
            assert not (repo / "src/Smartstore.Core" / path).exists(), f"Retired core source remains: {path}"
        print("Core stock notification and storefront variant binding extraction passed.")
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
        assert rows("SELECT COUNT(*) FROM Product") == [0]
        assert rows("SELECT COUNT(*) FROM Category") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='BackInStockSubscription'") == [0]
        assert rows("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Affiliate'") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Customer') WHERE name='AffiliateId'") == [0]
        assert rows("SELECT COUNT(*) FROM pragma_table_info('Product') WHERE name='AllowBackInStockSubscriptions'") == [0]
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
