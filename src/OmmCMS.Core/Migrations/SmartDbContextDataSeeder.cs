using OmmCMS.Core.Catalog;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Configuration;
using OmmCMS.Core.Theming;
using OmmCMS.Data.Migrations;
using OmmCMS.Utilities;

namespace OmmCMS.Core.Data.Migrations;

public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
{
    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;

    public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        await MigrateSettingsAsync(context, cancelToken);
    }

    public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        await context.MigrateSettingsAsync(builder =>
        {
            builder.Add(TypeHelper.NameOf<ThemeSettings>(x => x.KeepSassCompilerInMemory, true), "False");
        });

        var settings = context.Set<Setting>();
        const string oldName1 = "TaxSettings.ShowLegalHintsInProductDetails";
        const string oldName2 = "TaxSettings.ShowLegalHintsInProductList";

        var oldSettings = await settings
            .Where(x => x.Name == oldName1 || x.Name == oldName2)
            .ToListAsync(cancelToken);
        var oldSettings1 = oldSettings.Where(x => x.Name == oldName1).ToList();
        var oldSettings2 = oldSettings.Where(x => x.Name == oldName2).ToList();

        await MigrateLegalInfo(oldSettings1, TypeHelper.NameOf<CatalogSettings>(x => x.LegalInfoInProductDetail, true));
        await MigrateLegalInfo(oldSettings2, TypeHelper.NameOf<CatalogSettings>(x => x.LegalInfoInLists, true));

        if (oldSettings1.Count > 0 || oldSettings2.Count > 0)
        {
            await context.SaveChangesAsync(cancelToken);
        }

        async Task MigrateLegalInfo(List<Setting> oldSettings, string newName)
        {
            if (oldSettings.Count == 0)
            {
                return;
            }

            var storeIds = oldSettings.ToDistinctArray(x => x.StoreId);
            var existingNewSettings = await settings
                .Where(x => x.Name == newName && storeIds.Contains(x.StoreId))
                .Select(x => x.StoreId)
                .ToListAsync(cancelToken);

            foreach (var oldSetting in oldSettings.Where(x => !existingNewSettings.Contains(x.StoreId)))
            {
                settings.Add(new()
                {
                    Name = newName,
                    StoreId = oldSetting.StoreId,
                    Value = (oldSetting.Value.Convert<bool>() ? ProductLegalInfo.All : ProductLegalInfo.None).Convert(string.Empty)
                });
            }

            settings.RemoveRange(oldSettings);
        }
    }

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        MigrateThemeResources(builder);
        MigrateDashboardAndNavigationResources(builder);
        MigrateSettingsResources(builder);
        MigrateCatalogResources(builder);
        MigrateCheckoutResources(builder);
        MigrateCommonResources(builder);
    }

    private static void MigrateDashboardAndNavigationResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Permissions.DisplayName.CookieConsents", "Cookie consent", "Cookie-Zustimmungen");
        builder.AddOrUpdate("Admin.Dashboard.RefreshFailed", "The widget could not be updated. Please try again.", "Das Widget konnte nicht aktualisiert werden. Bitte versuche es erneut.");
        builder.AddOrUpdate("Admin.Dashboard.CookieConsents", "Cookie consent", "Cookie-Zustimmungen");

        string prefix = "Admin.Dashboard.CookieConsents.";

        builder.AddOrUpdate(prefix + "Description", "Saved consent status by cookie category.", "Gespeicherter Zustimmungsstatus je Cookie-Kategorie.");
        builder.AddOrUpdate(prefix + "Skipped", "{0} unreadable or incomplete records were excluded.", "{0} nicht lesbare oder unvollständige Einträge wurden nicht berücksichtigt.");
        builder.AddOrUpdate(prefix + "Category", "Category", "Kategorie");
        builder.AddOrUpdate(prefix + "Accepted", "Accepted", "Zugestimmt");
        builder.AddOrUpdate(prefix + "Declined", "Not consented", "Nicht zugestimmt");

        builder.AddOrUpdate("Permissions.DisplayName.ContactRequests", "Contact requests", "Kontaktanfragen");
        builder.AddOrUpdate("Admin.Dashboard.ContactRequests", "Contact enquiries", "Kontaktanfragen");
        builder.AddOrUpdate("Admin.Dashboard.ContactRequests.Description", "The latest newly recorded contact enquiries with links to their messages.", "Die neuesten ab jetzt erfassten Kontaktanfragen mit Verweisen auf die Nachrichten.");
        builder.AddOrUpdate("Admin.Dashboard.ContactRequests.Empty", "Future contact enquiries will appear here. Existing enquiries are not added retroactively.", "Hier erscheinen zukünftige Kontaktanfragen. Bereits vorhandene Anfragen werden nicht nachträglich übernommen.");

        builder.AddOrUpdate("Admin.Dashboard.Labels.ByQuantity", "By quantity", "Nach Anzahl");
        builder.AddOrUpdate("Admin.Dashboard.Labels.ByAmount", "By amount", "Nach Betrag");
        builder.AddOrUpdate("Admin.Dashboard.Labels.OrderCount", "Orders", "Bestellungen");
        builder.AddOrUpdate("Admin.Dashboard.Labels.Customer", "Customer", "Kunde");
        builder.Delete(
            "Admin.SalesReport.ByQuantity",
            "Admin.SalesReport.ByAmount",
            "Admin.Customers.Reports.BestBy.Fields.Customer");

        builder.Delete("FixedValueOrPercent.Absolute", "FixedValueOrPercent.Percentage",
            "Common.AmountOrPercent.Absolute", "Common.AmountOrPercent.Percentage");
        builder.AddOrUpdate("Permissions.DisplayName.Dashboard", "Dashboard", "Dashboard");
        builder.AddOrUpdate("Permissions.DisplayName.Widgets", "Widgets", "Widgets");
        builder.AddOrUpdate("Permissions.DisplayName.Sales", "Sales", "Verkäufe");
        builder.AddOrUpdate("Permissions.DisplayName.Customers", "Customers", "Kunden");
        builder.AddOrUpdate("Permissions.DisplayName.IncompleteOrders", "Incomplete orders", "Unvollständige Bestellungen");
        builder.AddOrUpdate("Permissions.DisplayName.Orders", "Order statistics", "Bestellstatistik");
        builder.AddOrUpdate("Permissions.DisplayName.Bestsellers", "Bestsellers", "Bestseller");
        builder.AddOrUpdate("Permissions.DisplayName.LatestOrders", "Latest orders", "Neueste Bestellungen");
        builder.AddOrUpdate("Permissions.DisplayName.RegisteredCustomers", "Customer registrations", "Kundenregistrierungen");
        builder.AddOrUpdate("Permissions.DisplayName.TopCustomers", "Top customers", "Top Kunden");
        builder.AddOrUpdate("Permissions.DisplayName.StoreReport", "Store statistics", "Shop-Statistiken");
        builder.AddOrUpdate("Permissions.DisplayName.NewsFeed", "News", "News");

        builder.AddOrUpdate("Admin.Dashboard", "Dashboard", "Dashboard");
        builder.AddOrUpdate("Admin.Dashboard.Customize", "Customize dashboard", "Dashboard anpassen");
        builder.AddOrUpdate("Admin.Header.Account", "Account", "Konto");
        builder.AddOrUpdate("Admin.Header.ExpandNavigation", "Expand navigation", "Navigation ausklappen");
        builder.AddOrUpdate("Admin.Header.CollapseNavigation", "Collapse navigation", "Navigation einklappen");
        builder.AddOrUpdate("Admin.Header.ExpandNavigation.Short", "Expand", "Ausklappen");
        builder.AddOrUpdate("Admin.Header.CollapseNavigation.Short", "Collapse", "Einklappen");
    }

    private static void MigrateSettingsResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Admin.Configuration.Settings.StoreScope.OwnValues",
            "Break inheritance",
            "Vererbung aufheben",
            "Use separate values for all settings on this page in this store.",
            "Alle Einstellungen dieser Seite für diesen Shop separat festlegen.");

        string prefix = "Admin.Configuration.Settings.Catalog.";

        builder.AddOrUpdate(prefix + "ShowManufacturersOnHomepage", "Show manufacturers on the homepage", "Hersteller auf der Startseite anzeigen");
        builder.AddOrUpdate(prefix + "ManufacturerItemsToDisplayOnHomepage",
            "Maximum number of manufacturers on the homepage",
            "Maximale Herstelleranzahl auf der Startseite",
            "Specifies the maximum number of manufacturers displayed on the homepage.",
            "Legt fest, wie viele Hersteller höchstens auf der Startseite angezeigt werden.");
        builder.AddOrUpdate(prefix + "ShowManufacturersInOffCanvas",
            "Show manufacturers in the OffCanvas menu",
            "Hersteller im OffCanvas-Menü anzeigen",
            "Specifies whether manufacturers are displayed in the OffCanvas menu. This is the navigation menu that slides in from the side and that customers open using the menu button on mobile devices.",
            "Legt fest, ob Hersteller im OffCanvas-Menü angezeigt werden. Das ist das seitlich eingeblendete Navigationsmenü, das Kunden über die Menü-Schaltfläche auf mobilen Endgeräten öffnen.");
        builder.AddOrUpdate(prefix + "ManufacturerItemsToDisplayInOffcanvasMenu",
            "Maximum number of manufacturers in the OffCanvas menu",
            "Maximale Herstelleranzahl im OffCanvas-Menü",
            "Specifies the maximum number of manufacturers displayed in the OffCanvas menu.",
            "Legt fest, wie viele Hersteller höchstens im OffCanvas-Menü angezeigt werden.");
        builder.AddOrUpdate(prefix + "ShowManufacturerPictures.Hint",
            "Specifies whether manufacturer images are displayed in manufacturer lists on the homepage and in the OffCanvas menu.",
            "Legt fest, ob Herstellerbilder in den Herstellerlisten auf der Startseite und im OffCanvas-Menü angezeigt werden.");
        builder.AddOrUpdate(prefix + "HideManufacturerDefaultPictures", "Hide manufacturer placeholder images", "Hersteller-Platzhalterbilder ausblenden");
        builder.AddOrUpdate(prefix + "SortManufacturersAlphabetically", "Sort manufacturer overview alphabetically", "Herstellerübersicht alphabetisch sortieren");

        builder.AddOrUpdate(prefix + "AllowDifferingEmailAddressForEmailAFriend",
            "Allow a different email address",
            "Abweichende E-Mail-Adresse erlauben",
            "Specifies whether customers can change their own email address in the product recommendation form. Otherwise, the address from their customer account is used and cannot be edited. Enable this option also if guests are allowed to recommend products by email.",
            "Legt fest, ob Kunden ihre eigene E-Mail-Adresse im Weiterempfehlungsformular ändern dürfen. Andernfalls wird die Adresse aus dem Kundenkonto verwendet und kann nicht bearbeitet werden. Aktivieren Sie diese Option auch, wenn Gäste Produkte per E-Mail weiterempfehlen dürfen.");

        builder.AddOrUpdate(prefix + "ProductContactGroup", "Product inquiries & recommendations", "Produktanfragen & Weiterempfehlungen");
        builder.AddOrUpdate(prefix + "EmailAFriendEnabled",
            "Enable product recommendations by email",
            "Weiterempfehlung per E-Mail aktivieren",
            "Specifies whether customers can recommend a product by email.",
            "Legt fest, ob Kunden ein Produkt per E-Mail weiterempfehlen können.");

        prefix = "Admin.Configuration.Settings.Price.";


        builder.AddOrUpdate(prefix + "CheckListPrices", "Check when displaying list prices…", "Bei Listenpreisen prüfen…");

        builder.AddOrUpdate(prefix + "ShowLoginForPriceNote.Hint",
            "Specifies whether the product detail page displays the message 'Prices are displayed after login.' when the visitor is not allowed to see prices. To display prices only to logged-in customers, revoke the price display permission for the 'Guests' customer group under 'Access control' and grant it to registered customers.",
            "Legt fest, ob auf der Produktdetailseite der Hinweis „Preise werden nach Anmeldung angezeigt.“ erscheint, wenn der Besucher keine Preise sehen darf. Um Preise nur angemeldeten Kunden anzuzeigen, entziehen Sie der Kundengruppe „Gäste“ unter „Zugriffsrechte“ das Recht zur Preisanzeige und erlauben es für registrierte Kunden.");

        builder.AddOrUpdate(prefix + "ShowVariantCombinationPriceAdjustment",
            "Show surcharges and reductions for variants",
            "Auf- und Abschläge bei Varianten anzeigen",
            "Specifies whether price surcharges and reductions are displayed for variant options, for example 'Extra large (+ €5.00)'. This setting only affects the display; surcharges and reductions are still included in the product price.",
            "Legt fest, ob Preisaufschläge und Preisabschläge bei Variantenoptionen angezeigt werden, beispielsweise „Extra groß (+ 5,00 €)“. Die Einstellung betrifft nur die Anzeige; die Auf- und Abschläge werden weiterhin im Produktpreis berücksichtigt.");

        prefix = "Admin.Configuration.Settings.Catalog.";

        builder.AddOrUpdate(prefix + "ProductsAlsoPurchasedEnabled",
            "Enable 'Also purchased'",
            "„Auch gekauft“ aktivieren",
            "Specifies whether products ordered together with this product are displayed on the product detail page.",
            "Legt fest, ob auf der Produktdetailseite Produkte angezeigt werden, die zusammen mit diesem Produkt bestellt wurden.");
        builder.AddOrUpdate(prefix + "ProductsAlsoPurchasedNumber",
            "Maximum number of products",
            "Maximale Produktanzahl",
            "Specifies the maximum number of products displayed in the 'Also purchased' section.",
            "Legt fest, wie viele Produkte im Bereich „Auch gekauft“ höchstens angezeigt werden.");
        builder.AddOrUpdate(prefix + "RecentlyViewedProductsEnabled",
            "Enable 'Recently viewed'",
            "„Zuletzt angesehen“ aktivieren",
            "Specifies whether customers can see their recently viewed products on a dedicated overview page and in additional product overviews throughout the store.",
            "Legt fest, ob Kunden ihre zuletzt angesehenen Produkte auf einer eigenen Übersichtsseite und in ergänzenden Produktübersichten im Shop sehen können.");
        builder.AddOrUpdate(prefix + "RecentlyViewedProductsNumber",
            "Maximum number of products",
            "Maximale Produktanzahl",
            "Specifies the maximum number of products displayed in the 'Recently viewed' section.",
            "Legt fest, wie viele Produkte im Bereich „Zuletzt angesehen“ höchstens angezeigt werden.");
        builder.AddOrUpdate(prefix + "RecentlyAddedProductsEnabled",
            "Enable 'New arrivals'",
            "„Neu eingetroffen“ aktivieren",
            "Specifies whether customers can view the most recently added products on the 'New arrivals' page.",
            "Legt fest, ob Kunden die zuletzt hinzugefügten Produkte auf der Seite „Neu eingetroffen“ ansehen können.");
        builder.AddOrUpdate(prefix + "RecentlyAddedProductsNumber",
            "Maximum number of products",
            "Maximale Produktanzahl",
            "Specifies the maximum number of products displayed in the 'New arrivals' section.",
            "Legt fest, wie viele Produkte im Bereich „Neu eingetroffen“ höchstens angezeigt werden.");

        builder.AddOrUpdate(prefix + "ShowLinkedAttributeValueQuantity",
            "Show product quantity in variant selection",
            "Produktmenge in der Variantenauswahl anzeigen",
            "Specifies whether variant options linked to a product display the configured quantity before the name, for example '2 x spare battery'. The quantity is only displayed when it is greater than 1 and the customer is allowed to see prices.",
            "Legt fest, ob bei Variantenoptionen mit Produktverknüpfung die hinterlegte Stückzahl vor dem Namen angezeigt wird, beispielsweise „2 x Ersatzakku“. Die Angabe erscheint nur bei Mengen größer als 1 und wenn der Kunde Preise sehen darf.");
        builder.AddOrUpdate(prefix + "ShowLinkedAttributeValueImage",
            "Show product image in variant selection",
            "Produktbild in der Variantenauswahl anzeigen",
            "Specifies whether variant options linked to a product use the linked product's first image as a thumbnail. The display depends on the variant attribute's control type.",
            "Legt fest, ob bei Variantenoptionen mit Produktverknüpfung das erste Bild des verknüpften Produkts als Vorschaubild verwendet wird. Die Darstellung hängt vom Auswahltyp des Variantenattributs ab.");

        builder.AddOrUpdate(prefix + "AdditionalProductOverviews", "Additional product overviews", "Weitere Produktübersichten");
        builder.AddOrUpdate(prefix + "ShowProductCondition", "Item condition", "Artikelzustand");
        builder.AddOrUpdate(prefix + "ShowProductTags", "Tags", "Tags");
        builder.AddOrUpdate(prefix + "ShowManufacturerInProductDetail", "Manufacturer/brand", "Hersteller/Marke");
        builder.AddOrUpdate(prefix + "ShowManufacturerPicturesInProductDetail", "Brand logo", "Markenlogo");

        builder.AddOrUpdate(prefix + "ShowShortDescriptionInGridStyleLists", "Short description", "Kurzbeschreibung");
        builder.AddOrUpdate(prefix + "ShowManufacturerInGridStyleLists", "Manufacturer/brand", "Hersteller/Marke");
        builder.AddOrUpdate(prefix + "ShowProductOptionsInLists", "Variant names", "Variantnamen");
        builder.AddOrUpdate(prefix + "ShowColorSquaresInLists", "Color variants", "Farbvarianten");
        builder.AddOrUpdate(prefix + "ShowManufacturerLogoInLists", "Brand logo instead of brand name", "Markenlogo statt Markenname");
        builder.AddOrUpdate(prefix + "LinkManufacturerLogoInLists", "Link brand logo", "Markenlogo verlinken");

        builder.AddOrUpdate(prefix + "IgnoreFeaturedProducts",
            "Hide featured products block",
            "Top-Produkte-Block ausblenden",
            "Disables the separate featured products block on category and manufacturer pages. Whether featured products appear in the regular product list is controlled by 'Show featured products in lists'.",
            "Deaktiviert den separaten Top-Produkte-Block auf Warengruppen- und Markenseiten. Ob Top-Produkte in der normalen Produktliste erscheinen, wird durch „Top-Produkte in Listen anzeigen“ festgelegt.");

        builder.AddOrUpdate(prefix + "IncludeInCompareProducts", "Include in product comparisons…", "Beim Produktvergleich einbeziehen…");
        builder.AddOrUpdate(prefix + "IncludeShortDescriptionInCompareProducts", "Short description", "Kurzbeschreibung");
        builder.AddOrUpdate(prefix + "IncludeFullDescriptionInCompareProducts", "Full description", "Komplette Beschreibung");

        builder.AddOrUpdate(prefix + "ShowProductInformation", "Show product information…", "Produktinformationen anzeigen…");
        builder.AddOrUpdate(prefix + "ShowProductSku",
            "SKU",
            "Artikelnummer",
            "Specifies whether SKUs are displayed on product detail pages, in the list view of product lists, in product comparisons, in the shopping cart, on the wishlist and during checkout. Also applies to order and shipment details and printed orders and packing slips.",
            "Legt fest, ob Artikelnummern auf Produktdetailseiten, in der Listenansicht von Produktlisten, im Produktvergleich, im Warenkorb, auf der Merkliste und im Bestellvorgang angezeigt werden. Die Einstellung gilt auch für Bestell- und Versanddetails sowie die Druckansicht von Bestellungen und Lieferscheinen.");
        builder.AddOrUpdate(prefix + "ShowManufacturerPartNumber",
            "Manufacturer part number",
            "Herstellerartikelnummer",
            "Check the box to show manufacturer part numbers in public store.",
            "Legt fest, ob die Hersteller-Produktnummer im Shop angezeigt werden soll.");
        builder.AddOrUpdate(prefix + "ShowGtin",
            "GTIN",
            "GTIN",
            "Check the box to show GTIN in public store.",
            "Legt fest, ob die EAN Nummer im Shop angezeigt werden soll.");
        builder.AddOrUpdate(prefix + "ShowWeight",
            "Weight",
            "Gewicht",
            "Specifies whether the product weight is displayed on the product detail page, in the list view of product lists and in product comparisons. Weight display in the shopping cart is configured separately in the shopping cart settings.",
            "Legt fest, ob das Produktgewicht auf der Produktdetailseite, in der Listenansicht von Produktlisten und im Produktvergleich angezeigt wird. Die Gewichtsanzeige im Warenkorb wird separat in den Warenkorb-Einstellungen festgelegt.");
        builder.AddOrUpdate(prefix + "ShowDimensions",
            "Dimensions",
            "Abmessungen",
            "Specifies whether width, height and length are displayed on the product detail page, in the list view of product lists and in product comparisons.",
            "Legt fest, ob Breite, Höhe und Länge auf der Produktdetailseite, in der Listenansicht von Produktlisten und im Produktvergleich angezeigt werden.");
        builder.AddOrUpdate(prefix + "ShowDefaultQuantityUnit",
            "Use default quantity unit",
            "Standard-Verpackungseinheit verwenden",
            "Specifies whether the default quantity unit is used when no quantity unit is assigned to the product.",
            "Legt fest, ob die Standard-Verpackungseinheit verwendet wird, wenn dem Produkt keine Verpackungseinheit zugewiesen ist.");
        builder.AddOrUpdate(prefix + "ShowDefaultDeliveryTime",
            "Use default delivery time",
            "Standard-Lieferzeit verwenden",
            "Specifies whether the default delivery time is used when no delivery time is assigned to the product.",
            "Legt fest, ob die Standard-Lieferzeit verwendet wird, wenn dem Produkt keine Lieferzeit zugewiesen ist.");

        builder.AddOrUpdate("Admin.CatalogSettings.ProductDisplay", "Shared product settings", "Übergreifende Produkteinstellungen");
        builder.AddOrUpdate("Admin.CatalogSettings.ProductDisplay.Info",
            "These settings are used in multiple places in the store. For example, the SKU display setting also controls its display on orders; the placeholder image setting applies to products and the shopping cart, among other places.",
            "Diese Einstellungen werden an mehreren Stellen im Shop verwendet. Beispielsweise steuert die Artikelnummer-Anzeige auch die Darstellung bei Bestellungen; die Einstellung für Platzhalterbilder wirkt unter anderem bei Produkten und im Warenkorb.");

        builder.AddOrUpdate(prefix + "DefaultProductListPageSize",
            "Products per page",
            "Produkte pro Seite",
            "Specifies how many products are displayed on a product list page.",
            "Legt fest, wie viele Produkte auf einer Produktlistenseite angezeigt werden.");

        builder.AddOrUpdate(prefix + "ProductTileContent", "Product tile content", "Inhalt der Produktkacheln");

        builder.AddOrUpdate(prefix + "AllowProductViewModeChanging.Hint",
            "Displays a switch between grid and list views in product lists for customers. The initial view is set with 'Default view mode'.",
            "Zeigt Kunden einen Umschalter zwischen Raster- und Listenansicht in Produktlisten an. Die anfängliche Darstellung legen Sie mit „Standardansicht“ fest.");

        builder.AddOrUpdate(prefix + "KeepOnSubPages",
            "Keep when paging and filtering…",
            "Beim Blättern und Filtern beibehalten…",
            "Specifies which content remains visible from page 2 of the product list onwards and when filters are active.",
            "Legt fest, welche Inhalte ab Seite 2 der Produktliste und bei aktiven Filtern weiterhin angezeigt werden.");
        builder.AddOrUpdate(prefix + "IncludeFeaturedProductsInSubPages", "Featured products", "Top-Produkte");
        builder.AddOrUpdate(prefix + "ShowSubCategoriesInSubPages",
            "Subcategories",
            "Unterwarengruppen",
            "Keeps subcategories visible from page 2 onwards and when filters are active. Only applies when subcategories are displayed above the product list.",
            "Zeigt Unterwarengruppen auch ab Seite 2 der Produktliste und bei aktiven Filtern an. Gilt nur für die Anzeige oberhalb der Produktliste.");
        builder.AddOrUpdate(prefix + "ShowDescriptionInSubPages",
            "Page descriptions",
            "Seitenbeschreibungen",
            "Keeps category and manufacturer descriptions visible from page 2 onwards and when filters are active.",
            "Zeigt Beschreibungen von Warengruppen und Marken auch ab Seite 2 der Produktliste und bei aktiven Filtern an.");

        builder.AddOrUpdate(prefix + "DefaultPageSizeOptions",
            "Page size options",
            "Seitengrößen",
            "Specify the page sizes customers can select in product lists.",
            "Legt die auswählbaren Seitengrößen in Produktlisten fest.");

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Salutations",
            "Salutations",
            "Anreden",
            "Specify the entries available for salutation selection when entering an address.",
            "Bestimmen Sie hier die Einträge, die bei der Adresserfassung als Anrede ausgewählt werden können.");

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameAllowedCharacters",
            "Additional username characters",
            "Zusätzliche Zeichen für Benutzernamen",
            "When this field contains a value, usernames may contain the default characters a-z, A-Z, 0-9, space, -, ., _, @ and +, as well as the characters entered here."
            + " Leave empty to allow all characters.",
            "Wenn dieses Feld einen Wert enthält, dürfen Benutzernamen die Standardzeichen a-z, A-Z, 0-9, Leerzeichen, -, ., _, @ und + sowie die hier eingegebenen Zeichen enthalten."
            + " Leer lassen, um alle Zeichen zu erlauben.");

        const string defaultCharsStr = "<code>a-z</code>, <code>A-Z</code>, <code>0-9</code>, <code>-</code>, <code>.</code>, <code>_</code>, <code>@</code>, <code>+</code>";
        const string additionalCharsStr = "<code id=\"username-additional-characters\"></code>";

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameAllowedCharacters.AdminHint",
            $"Allowed default characters: {defaultCharsStr}, including spaces. Additionally allowed: {additionalCharsStr}.",
            $"Erlaubte Standardzeichen: {defaultCharsStr}, einschließlich Leerzeichen. Zusätzlich erlaubt: {additionalCharsStr}.");

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameAllowedCharacters.AllCharactersHint",
            "All characters are allowed.",
            "Alle Zeichen sind erlaubt.");

        builder.AddOrUpdate("Common.Variants", "Variants", "Varianten");

        builder.AddOrUpdate(prefix + "DisplayAllImagesNumber",
            "Show all product images up to",
            "Alle Produktbilder anzeigen bis",
            "Specifies the total number of product images up to which all images are displayed. When there are more images, the gallery only shows images of the selected variant and images not assigned to any variant. The limit can be set individually for each product.",
            "Legt fest, bis zu welcher Gesamtzahl alle Produktbilder angezeigt werden. Bei mehr Bildern zeigt die Galerie nur Bilder der gewählten Variante und Bilder ohne Variantenzuordnung. Die Grenze kann für jedes Produkt individuell festgelegt werden.");

        prefix = "Admin.Configuration.Settings.Price.";

        builder.AddOrUpdate(prefix + "RulesCalculateIncludingTax",
            "Cart rules calculate amounts including tax",
            "Warenkorbregeln berechnen Beträge inklusive Steuer",
            "Specifies whether shopping cart rules calculate amounts as inclusive of or exclusive of tax. If not specified (default), the tax settings for the respective shopping cart or customer apply.",
            "Legt fest, ob Warenkorbregeln die Beträge inklusive oder exklusive Umsatzsteuer berechnen. Falls nicht festgelegt (Standard), gelten die Steuereinstellungen des jeweiligen Warenkorbs bzw. Kunden.");

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerFormFields.Description",
            "Manage form fields that are displayed during registration. In order to ensure the address transfer from the registration form, it is necessary that the following fields are activated and filled in by the customer:<ul class=\"mb-0\"><li>First name</li><li>Last name</li><li>Email</li><li>and all fields that are selected as required in the tab <b>Addresses</b></li></ul>",
            "Sie können Formularfelder verwalten, die während der Registrierung angezeigt werden. Um die Adressübergabe aus dem Registrierungsformular zu gewährleisten, müssen folgende Felder aktiviert und vom Kunden ausgefüllt sein:<ul class=\"mb-0\"><li>Vorname</li><li>Nachname</li><li>E-Mail</li><li>und alle Felder die im Tab <b>Adressen</b> als erforderlich ausgewählt sind</li></ul>");

        builder.AddOrUpdate("Admin.Configuration.Settings.Icons", "Icons", "Icons");

        prefix = "Admin.Configuration.Settings.Icons.";

        builder.AddOrUpdate(prefix + "Description",
            "Choose which menu areas display icons and set their size and optional fallback icon. In My Account, icons are always displayed and only their size can be changed.",
            "Legen Sie fest, in welchen Menübereichen Icons angezeigt werden, und bestimmen Sie Größe und optionales Ersatzicon. Im Bereich Mein Konto werden Icons immer angezeigt; dort lässt sich nur ihre Größe ändern.");

        prefix += "Fields.";

        builder.AddOrUpdate(prefix + "MainMenu",
            "Main menu",
            "Hauptmenü",
            "Icon defaults for the top navigation. A store override applies to all options in this menu area.",
            "Icon-Vorgaben für die obere Hauptnavigation. Eine Shop-Überschreibung gilt für alle Optionen dieses Menübereichs.");

        builder.AddOrUpdate(prefix + "MainMenuDropdown",
            "Main menu dropdowns",
            "Hauptmenü-Dropdowns",
            "Icon defaults for the standard main menu dropdowns. A store override applies to all options in this menu area.",
            "Icon-Vorgaben für die normalen Hauptmenü-Dropdowns. Eine Shop-Überschreibung gilt für alle Optionen dieses Menübereichs.");

        builder.AddOrUpdate(prefix + "OffCanvasMenu",
            "Off-canvas menu",
            "Offcanvas-Menü",
            "Icon defaults for the off-canvas menu and all its submenu levels. A store override applies to this entire menu area.",
            "Icon-Vorgaben für das Offcanvas-Menü und alle nachgeladenen Untermenüebenen. Eine Shop-Überschreibung gilt für diesen gesamten Menübereich.");

        builder.AddOrUpdate(prefix + "CategoryMenu",
            "Category menu",
            "Kategoriemenü",
            "Icon defaults for the category navigation in the sidebar. A store override applies to all options in this menu area.",
            "Icon-Vorgaben für die Kategorienavigation in der Seitenleiste. Eine Shop-Überschreibung gilt für alle Optionen dieses Menübereichs.");

        builder.AddOrUpdate(prefix + "MyAccountIconSize",
            "My Account icon size",
            "Icon-Größe für Mein Konto",
            "Sets the icon size in My Account. This menu always displays its own icons.",
            "Legt die Icon-Größe im Bereich Mein Konto fest. Dieses Menü zeigt seine eigenen Icons immer an.");

        builder.AddOrUpdate(prefix + "DisplayIcons",
            "Display icons",
            "Icons anzeigen",
            "Shows entry icons in this menu area. Navigation controls remain visible.",
            "Zeigt Eintragsicons in diesem Menübereich an. Navigationssymbole bleiben sichtbar.");

        builder.AddOrUpdate(prefix + "Size",
            "Icon size",
            "Icon-Größe",
            "Default size for menu icons: 16px, 20px or 24px. A size configured on an individual icon takes precedence.",
            "Standardgröße für Menüicons: 16px, 20px oder 24px. Eine individuell am Icon konfigurierte Größe hat Vorrang.");

        builder.AddOrUpdate(prefix + "FallbackIcon",
            "Fallback icon",
            "Ersatzicon",
            "Fills missing icons only in menu blocks that already contain at least one entry icon.",
            "Füllt fehlende Icons nur in Menüblöcken auf, die bereits mindestens ein Eintragsicon enthalten.");
    }

    private static void MigrateCatalogResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Admin.Common.ColorPalette.Color",
            "Color",
            "Farbe",
            "Define up to four colors for this attribute value. The colors are only displayed when the attribute's control type is set to swatches.",
            "Legen Sie bis zu vier Farben für diesen Attributwert fest. Die Farben werden nur angezeigt, wenn das Attribut als Swatches (Farb- und Bildmuster) dargestellt wird.");
        builder.Delete(
            "Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.Fields.ColorSquaresRgb",
            "Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.Fields.ColorSquaresRgb.Hint",
            "Admin.Catalog.Attributes.CheckoutAttributes.Values.Fields.Color",
            "Admin.Catalog.Attributes.CheckoutAttributes.Values.Fields.Color.Hint");
        builder.AddOrUpdate("Admin.Common.ColorPalette.AddColor", "Add color", "Farbe hinzufügen");
        builder.AddOrUpdate("Admin.Common.ColorPalette.RemoveColor", "Remove this color field", "Dieses Farbfeld entfernen");

        string prefix = "Admin.Catalog.Products.";

        builder.Delete(
            prefix + "Fields.Published.Hint",
            prefix + "Fields.QuantityUnit.Hint",
            prefix + "Fields.Weight.Hint",
            prefix + "Fields.Length.Hint",
            prefix + "Fields.Width.Hint",
            prefix + "Fields.Height.Hint",
            prefix + "Groups.RequiredProducts",
            prefix + "Groups.Subscription",
            prefix + "Availability.From",
            prefix + "Availability.To");

        builder.AddOrUpdate(prefix + "Fields.HomePageDisplayOrder", "Homepage display order", "Reihenfolge auf der Homepage");
        builder.AddOrUpdate(prefix + "Fields.DisplayOrder",
            "General sort priority",
            "Allgemeine Sortierpriorität",
            "Specifies the order in which associated products of a grouped product are displayed. In addition, this setting determines the order of hits in the search, if sort by recommendation is enabled in the search settings. Lower values are displayed first.",
            "Legt die Reihenfolge fest, in der verknüpfte Produkte eines Gruppenproduktes angezeigt werden. Zusätzlich legt diese Einstellung die Reihenfolge der Treffer bei der Suche fest, sofern in den Sucheinstellungen die Sortierung nach Empfehlung aktiviert ist. Kleinere Werte werden zuerst angezeigt.");
        builder.AddOrUpdate(prefix + "Groups.Restrictions", "Publication and availability", "Veröffentlichung und Verfügbarkeit");
        builder.AddOrUpdate(prefix + "Fields.Availability",
            "Availability",
            "Verfügbarkeit",
            "Limits product availability to this period. Leave either field empty to leave that boundary unrestricted.",
            "Begrenzt die Verfügbarkeit des Produkts auf diesen Zeitraum. Lassen Sie ein Feld leer, um die jeweilige Grenze offen zu lassen.");
        builder.AddOrUpdate(prefix + "Groups.Taxes", "Taxes", "Steuern");
        builder.AddOrUpdate(prefix + "Groups.SalesOptions", "Sales options", "Verkaufsoptionen");
        builder.AddOrUpdate(prefix + "Groups.SalesOptions.Info",
            "Specify whether additional items must be ordered with this product. Enable the option to show its settings.",
            "Hier legen Sie fest, ob zum Produkt weitere Artikel mitbestellt werden müssen. Aktivieren Sie die Option, um die zugehörigen Einstellungen einzublenden.");
        builder.AddOrUpdate(prefix + "Groups.Shipping", "Shipping, dimensions and units", "Versand, Maße und Einheiten");
        builder.AddOrUpdate(prefix + "Groups.Shipping.Info",
            "The quantity unit specifies how product quantities are labeled in the shop, for example as pieces, packs or pairs. It appears alongside the quantity when ordering and in the cart. Weight and dimensions can affect shipping costs, depending on the shipping provider, and are displayed on the product page when the corresponding catalog settings are enabled.",
            "Die Verpackungseinheit legt fest, wie Produktmengen im Shop bezeichnet werden, zum Beispiel als Stück, Packung oder Paar. Sie erscheint bei der Mengenauswahl und im Warenkorb neben der Anzahl. Gewicht und Maße können je nach Versandanbieter die Versandkosten beeinflussen und werden bei aktivierten Katalogeinstellungen auf der Produktseite angezeigt.");

        builder.AddOrUpdate(prefix + "Fields.Condition",
            "Item condition",
            "Artikelzustand",
            "Condition of the goods offered, e.g. new, used or refurbished. Used for structured product data and supported product exports. Display in the shop is controlled by the catalog settings.",
            "Zustand der angebotenen Ware, z. B. neu, gebraucht oder generalüberholt. Wird für strukturierte Produktdaten und unterstützte Produktexporte verwendet. Die Anzeige im Shop wird über die Katalogeinstellungen gesteuert.");
        builder.AddOrUpdate(prefix + "Fields.CountryOfOriginId",
            "Country of origin",
            "Herkunftsland",
            "Country of origin of the goods. This information is available to integrations and exports. It does not determine tax or shipping costs in the standard implementation.",
            "Ursprungsland der Ware. Die Angabe steht für Schnittstellen und Exporte zur Verfügung. Sie bestimmt im Standard keine Steuer- oder Versandkosten.");
        builder.AddOrUpdate(prefix + "Fields.IsShipEnabled",
            "Shipping required",
            "Versand erforderlich",
            "Enable if this product must be shipped. The product is included in shipping calculations. When disabled, delivery times are hidden in the shop. The quantity unit is independent of this setting.",
            "Aktivieren, wenn dieses Produkt versendet werden muss. Es wird bei der Versandberechnung berücksichtigt. Bei deaktivierter Option werden Lieferzeiten im Shop ausgeblendet. Die Verpackungseinheit ist davon unabhängig.");

        builder.AddOrUpdate("Admin.Catalog.Categories.Fields.PageSizeOptions",
            "Page size options",
            "Seitengrößen",
            "Specify the page sizes customers can select in this category's product list. Leave empty to use the page sizes from the catalog settings.",
            "Legt die auswählbaren Seitengrößen der Produktliste dieser Warengruppe fest. Lassen Sie das Feld leer, um die Seitengrößen der Katalog-Einstellungen zu verwenden.");

        builder.AddOrUpdate("Admin.Catalog.Manufacturers.Fields.PageSizeOptions",
            "Page size options",
            "Seitengrößen",
            "Specify the page sizes customers can select in this manufacturer's product list. Leave empty to use the page sizes from the catalog settings.",
            "Legt die auswählbaren Seitengrößen der Produktliste dieses Herstellers fest. Lassen Sie das Feld leer, um die Seitengrößen der Katalog-Einstellungen zu verwenden.");


        builder.AddOrUpdate(prefix + "Deleted",
            "The product was moved to the recycle bin.",
            "Das Produkt wurde in den Papierkorb verschoben.");

        builder.AddOrUpdate("ActivityLog.DeleteProduct",
            "Product ('{0}') has been moved to the recycle bin",
            "Produkt ('{0}') in den Papierkorb verschoben");

        #region product legal info

        builder.AddOrUpdate("Products.ShippingInfo",
            "plus shipping",
            "zzgl. Versandkosten");

        builder.AddOrUpdate("Products.ShippingInfoUrl",
            "plus <a href=\"{0}\">shipping</a>",
            "zzgl. <a href=\"{0}\">Versandkosten</a>");

        builder.AddOrUpdate("Products.ShippingInfoWithSurcharge",
            "plus shipping and a <b>{0}</b> shipping surcharge",
            "zzgl. Versandkosten und <b>{0}</b> Versandaufschlag");

        builder.AddOrUpdate("Products.ShippingInfoUrlWithSurcharge",
            "plus <a href=\"{0}\">shipping</a> and a <b>{1}</b> shipping surcharge",
            "zzgl. <a href=\"{0}\">Versandkosten</a> und <b>{1}</b> Versandaufschlag");

        builder.AddOrUpdate("Products.ShippingSurchargeInfo",
            "plus <b>{0}</b> shipping surcharge",
            "zzgl. <b>{0}</b> Versandaufschlag");

        builder.AddOrUpdate("Products.FreeShippingInfo",
            "free shipping",
            "versandkostenfrei");

        builder.AddOrUpdate("Products.TaxLegalInfo", "Prices {0}", "Preise {0}");

        builder.AddOrUpdate("Common.AdditionalShippingSurcharge",
            "Plus <b>{0}</b> shipping surcharge",
            "zzgl. <b>{0}</b> Versandaufschlag");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LegalInfoInProductDetail",
            "Legal information on product page",
            "Rechtliche Hinweise auf der Produktseite",
            "Specifies which tax and shipping cost notes are displayed on the product page. If nothing is selected, no note is displayed. An additional shipping charge is always displayed.",
            "Legt fest, welche Hinweise zu Steuer und Versandkosten auf der Produktseite angezeigt werden. Ohne Auswahl wird kein Hinweis angezeigt. Ein Transportzuschlag wird immer angezeigt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LegalInfoInLists",
            "Legal information in product lists",
            "Rechtliche Hinweise in Produktlisten",
            "Specifies which tax and shipping cost notes are displayed in the list view and in the product comparison. If nothing is selected, no note is displayed.",
            "Legt fest, welche Hinweise zu Steuer und Versandkosten in der Listenansicht und im Produktvergleich angezeigt werden. Ohne Auswahl wird kein Hinweis angezeigt.");

        builder.AddOrUpdate("Enums.ProductLegalInfo.Tax", "Tax note (incl./excl. VAT)", "Steuerhinweis (inkl./zzgl. MwSt.)");
        builder.AddOrUpdate("Enums.ProductLegalInfo.Shipping", "Shipping cost note", "Versandkostenhinweis");

        prefix = "Admin.Configuration.Settings.Tax.";

        builder.Delete(
            "Tax.LegalInfoProductDetail",
            "Tax.LegalInfoProductDetail2",
            "Tax.LegalInfoShort",
            "Tax.LegalInfoShort2",
            "Tax.LegalInfoShort3",
            prefix + "ShowLegalHintsInProductDetails",
            prefix + "ShowLegalHintsInProductDetails.Hint",
            prefix + "ShowLegalHintsInProductList",
            prefix + "ShowLegalHintsInProductList.Hint",
            prefix + "ShowLegalHintsInProductGrid",
            prefix + "ShowLegalHintsInProductGrid.Hint");

        #endregion

        builder.AddOrUpdate("Products.EmailAFriend.LoginNote",
            "Please log in to use this function. <a href=\"{0}\" rel=\"nofollow\">Login now</a>",
            "Bitte melden Sie sich an, um diese Funktion nutzen zu können. <a href=\"{0}\" rel=\"nofollow\">Jetzt anmelden</a>");

        prefix = "Admin.Catalog.Products.";

        builder.AddOrUpdate(prefix + "ProductVariantAttributes.AttributeCombinations.SelectAttributes",
            "Set the attributes for the combination.",
            "Legen Sie die Attribute für die Kombination fest");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.ExportMappings.Note",
            "Define the mapping of attribute values to export fields according to the following pattern: <b>&lt;Format prefix&gt;:&lt;Export field name&gt;</b>."
            + " For example, <b>gmc:color</b> exports the attribute values for colors to the <b>color</b> field during"
            + " the Google Merchant Center export and <b>channable:color</b> exports them to the Channable export."
            + " Theses mappings are only apply when exporting attribute combinations.",
            "Legen Sie Zuordnungen von Attributwerten zu Exportfeldern nach dem folgenden Muster fest: <b>&lt;Formatpräfix&gt;:&lt;Export-Feldname&gt;</b>"
            + " fest. Beispiel: <b>gmc:color</b> exportiert beim Google-Merchant-Center-Export die Attributwerte für Farben in das Feld"
            + " <b>color</b> und <b>channable:color</b> beim Channable-Export. Die Zuordnungen sind nur beim Export von Attributkombinationen wirksam.");
    }

    private static void MigrateThemeResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Admin.Common.Icons", "Icons", "Icons");

        string prefix = "Admin.System.Maintenance.Icons.";

        builder.AddOrUpdate(prefix + "Title", "Migrate icons", "Icons migrieren");
        builder.AddOrUpdate(prefix + "Start", "Start migration", "Migration starten");
        builder.Delete("Admin.Dashboard.Editor.Layer.System", "Admin.Dashboard.Editor.Layer.Global", "Admin.Dashboard.Editor.Layer.User",
            "Admin.Dashboard.Editor.Using", "Admin.Dashboard.Editor.SharedHint");

        builder.Delete("Admin.Dashboard.Editor.Dashboard.System");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Dashboard.Global", "Dashboard for everyone", "Dashboard für alle");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Dashboard.User", "My dashboard", "Mein Dashboard");
        builder.Delete("Admin.Dashboard.Editor.Status.Unsaved");
        builder.Delete("Admin.Dashboard.Editor.TemplateNotice");
        builder.Delete("Admin.Dashboard.Editor.Status.Reset");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Customize", "Customize dashboard", "Dashboard anpassen");
        builder.AddOrUpdate("Admin.Dashboard.Editor.View", "Dashboard view", "Dashboard-Ansicht");
        builder.Delete("Admin.Dashboard.Editor.Editing");
        builder.AddOrUpdate("Admin.Dashboard.Editor.RemoveWidgetConfirm", "Really remove this widget?", "Dieses Widget wirklich entfernen?");
        builder.AddOrUpdate("Admin.Dashboard.Editor.ResetConfirm",
            "Remove the customization immediately and return to the default dashboard? Unsaved changes will be discarded.",
            "Die Anpassung jetzt entfernen und zur Dashboard-Vorgabe zurückkehren? Ungespeicherte Änderungen werden verworfen.");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Widgets", "Add widget", "Widget hinzufügen");
        builder.Delete("Admin.Dashboard.Editor.AddWidget");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Search", "Search widgets", "Widgets suchen");
        builder.AddOrUpdate("Admin.Dashboard.Editor.ResetPersonal", "Reset to the shared dashboard", "Auf gemeinsame Vorgabe zurücksetzen");
        builder.AddOrUpdate("Admin.Dashboard.Editor.ResetGlobal", "Reset to the default dashboard", "Auf Standard zurücksetzen");
        builder.Delete("Admin.Dashboard.Editor.SaveUser");
        builder.AddOrUpdate("Admin.Dashboard.Editor.SaveGlobal", "Save for everyone", "Für alle speichern");
        builder.AddOrUpdate("Admin.Dashboard.Editor.SaveGlobalConfirm",
            "This dashboard will be saved as the shared default for all users. Personal dashboards will be retained. Do you want to continue?",
            "Dieses Dashboard wird als gemeinsame Vorgabe für alle Benutzer gespeichert. Persönliche Dashboards bleiben erhalten. Möchten Sie fortfahren?");
        builder.Delete("Admin.Dashboard.Editor.SaveTarget");
        builder.Delete("Admin.Dashboard.Editor.ResetPending");
        builder.AddOrUpdate("Admin.Dashboard.Editor.ResetFailed",
            "The dashboard could not be reset. Please try again.",
            "Das Dashboard konnte nicht zurückgesetzt werden. Bitte versuche es erneut.");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Saved", "Dashboard saved.", "Dashboard gespeichert.");
        builder.AddOrUpdate("Admin.Dashboard.Editor.SelectionFailed", "The dashboard was saved, but the view selection could not be saved.", "Das Dashboard wurde gespeichert, die Ansichtsauswahl konnte jedoch nicht gespeichert werden.");
        builder.AddOrUpdate("Admin.Dashboard.Editor.SaveFailed", "Could not save the dashboard. Your draft is still available.", "Dashboard konnte nicht gespeichert werden. Ihr Entwurf bleibt erhalten.");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Invalid", "The dashboard changes are invalid or no longer permitted.", "Die Dashboard-Änderungen sind ungültig oder nicht mehr erlaubt.");
        builder.AddOrUpdate("Admin.Dashboard.Editor.DiscardConfirm",
            "Discard your unsaved changes?",
            "Deine ungespeicherten Änderungen wirklich verwerfen?");
        builder.Delete("Admin.Dashboard.Editor.Unsaved");
        builder.Delete("Admin.Dashboard.Editor.UnsavedHint");
        builder.Delete("Admin.Dashboard.Editor.KeepEditing");
        builder.Delete("Admin.Dashboard.Editor.Discard");
        builder.Delete("Admin.Dashboard.Editor.SaveAndSwitch");
        builder.Delete("Admin.Dashboard.Editor.TargetAbove", "Admin.Dashboard.Editor.TargetBelow");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Move", "Move widget", "Widget verschieben");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Undo", "Undo last change", "Letzte Änderung rückgängig machen");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Redo", "Redo last change", "Letzte Änderung wiederholen");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Configure", "Configure widget", "Widget konfigurieren");
        builder.AddOrUpdate("Admin.Dashboard.Widget.Count",
            "Number of displayed items",
            "Anzahl angezeigter Elemente",
            "Show between 1 and 50 items.",
            "Zeigt zwischen 1 und 50 Elemente an.");
        builder.Delete("Admin.Dashboard.LatestOrders.Fields.Count", "Admin.Dashboard.LatestOrders.Fields.Count.Hint");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Width", "Change widget width", "Widget-Breite ändern");
        builder.AddOrUpdate("Admin.Dashboard.Editor.Height", "Change widget row span (Home: one row)", "Widget-Zeilenspanne ändern (Pos1: eine Zeile)");
        builder.Delete("Permissions.DisplayName.dashboard.personalize", "Permissions.DisplayName.dashboard.manageglobal");
        builder.AddOrUpdate("Permissions.DisplayName.Personalize", "Customize own dashboard", "Eigenes Dashboard anpassen");
        builder.AddOrUpdate("Permissions.DisplayName.ManageGlobal", "Manage shared dashboard", "Gemeinsames Dashboard verwalten");

        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Title", "Migrate icons", "Icons migrieren");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Start", "Start migration", "Migration starten");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Hint",
            "Converts legacy Font Awesome icons in menus and HTML content to the new icon format. Already migrated icons remain unchanged. Migration usually takes just a few seconds, but may take several minutes for large datasets. Keep this page open until the migration finishes. Logs of automatic and manual migrations are available in App_Data/Logs (icon-migration-*).",
            "Überführt bisherige Font-Awesome-Icons in Menüs und HTML-Inhalten in das neue Icon-Format. Bereits migrierte Icons bleiben unverändert. Die Migration dauert meist nur wenige Sekunden, bei großen Datenbeständen auch mehrere Minuten. Lassen Sie diese Seite bis zum Abschluss geöffnet. Protokolle automatischer und manueller Migrationen finden Sie unter App_Data/Logs (icon-migration-*).");
        builder.AddOrUpdate(prefix + "Started", "Icon migration is running…", "Die Icon-Migration läuft…");
        builder.AddOrUpdate(prefix + "Completed",
            "Completed: {0} records checked, {1} icons migrated, {2} icons not migrated.",
            "Abgeschlossen: {0} Datensätze geprüft, {1} Icons migriert, {2} Icons nicht migriert.");
        builder.AddOrUpdate(prefix + "Failed",
            "The migration could not be started or the connection was interrupted. You can start it again.",
            "Die Migration konnte nicht gestartet werden oder die Verbindung wurde unterbrochen. Sie können sie erneut starten.");
        builder.AddOrUpdate(prefix + "Migrated", "Migrated", "Migriert");
        builder.AddOrUpdate(prefix + "Warning", "Warning", "Hinweis");
        builder.AddOrUpdate(prefix + "NotMigrated", "Not migrated", "Nicht migriert");
        builder.AddOrUpdate(prefix + "Columns.Entity", "Entity", "Entität");
        builder.AddOrUpdate(prefix + "Columns.RecordId", "Record ID", "Datensatz-ID");
        builder.AddOrUpdate(prefix + "Columns.Field", "Field", "Feld");
        builder.AddOrUpdate(prefix + "Columns.Message", "Message", "Meldung");
        builder.AddOrUpdate(prefix + "Columns.NewValue", "New icon value", "Neuer Icon-Wert");

        builder.AddOrUpdate("Enums.QuantizationMethod.Octree", "Hexadecatree (formerly Octree)", "Hexadecatree (vormals Octree)");
        builder.AddOrUpdate("Enums.JpegColorType.YCbCrRatio422", "YCbCr 4:2:2", "YCbCr 4:2:2");
        builder.AddOrUpdate("Enums.JpegColorType.YCbCrRatio411", "YCbCr 4:1:1", "YCbCr 4:1:1");
        builder.AddOrUpdate("Enums.JpegColorType.YCbCrRatio410", "YCbCr 4:1:0", "YCbCr 4:1:0");
        builder.AddOrUpdate("Admin.Configuration.Settings.Media.GifQuantizationMethod", "GIF palette quantization", "GIF-Palettenquantisierung");

        builder.AddOrUpdate("Admin.Configuration.Settings.Media.JpegColorType",
            "JPEG color type",
            "JPEG-Farbtyp",
            "Leave empty for automatic selection.",
            "Leer lassen für automatische Auswahl.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Media.PngQuantizationMethod",
            "PNG palette quantization",
            "PNG-Palettenquantisierung",
            "Only affects palette-based PNG images. Has no effect on RGB or RGBA images.",
            "Wirkt nur auf PNG-Bilder mit Farbpalette. Hat bei RGB- und RGBA-Bildern keine Wirkung.");

        builder.AddOrUpdate("Admin.IconBrowser.SelectSource", "Select source", "Quelle wählen",
            "Select an icon source", "Icon-Quelle wählen");
        builder.AddOrUpdate("Admin.IconBrowser.Kits", "Kits", "Kits");
        builder.AddOrUpdate("Admin.IconBrowser.Libraries", "Libraries", "Bibliotheken");
        builder.AddOrUpdate("Admin.IconBrowser.KitTitle", "Kit: {0}", "Kit: {0}");
        builder.AddOrUpdate("Admin.IconBrowser.LibraryTitle", "Library: {0}", "Bibliothek: {0}");
        builder.AddOrUpdate("Admin.IconBrowser.Icon", "Icon", "Icon");
        builder.AddOrUpdate("Admin.IconBrowser.Search", "Search icons", "Icons suchen");
        builder.AddOrUpdate("Admin.IconBrowser.Size", "Size", "Größe");
        builder.Delete("Admin.IconBrowser.Color", "Admin.IconBrowser.FlipHorizontal", "Admin.IconBrowser.FlipVertical", "Admin.IconBrowser.Rotation", "Admin.IconBrowser.Animation", "Admin.IconBrowser.StrokeWidth", "Admin.IconBrowser.NoAnimation");

        builder.AddOrUpdate("Admin.Catalog.Categories.Fields.Icon",
            "Icon",
            "Icon",
            "The icon appears in the category navigation, in the main menu including dropdowns, in the mobile menu and in the category tree in the administration.",
            "Das Icon erscheint in der Kategorienavigation, im Hauptmenü einschließlich Dropdowns, im mobilen Menü sowie im Kategoriebaum der Administration.");

        builder.AddOrUpdate("Admin.Design.Color", "Color", "Farbe");
        builder.AddOrUpdate("Admin.Design.Opacity", "Opacity", "Deckkraft");
        builder.AddOrUpdate("Admin.Design.FlipHorizontal", "Flip horizontally", "Horizontal spiegeln");
        builder.AddOrUpdate("Admin.Design.FlipVertical", "Flip vertically", "Vertikal spiegeln");
        builder.AddOrUpdate("Admin.Design.Rotation", "Rotation", "Rotation");
        builder.AddOrUpdate("Admin.Design.Animation", "Animation", "Animation");
        builder.AddOrUpdate("Admin.Design.StrokeWidth", "Stroke width", "Strichstärke");
        builder.AddOrUpdate("Admin.Design.NoAnimation", "No animation", "Keine Animation");
        builder.AddOrUpdate("Admin.IconBrowser.Options", "Icon options", "Icon-Optionen");
        builder.AddOrUpdate("Admin.IconBrowser.External", "provided by a module", "von einem Modul bereitgestellt");

        builder.AddOrUpdate("Admin.Themes.Info.Flex",
            "A clean, versatile storefront with bright surfaces, clear typography and a product-focused layout. The flexible foundation for the Flex theme family.",
            "Ein klarer, vielseitiger Shop mit hellen Flächen, gut lesbarer Typografie und einem Layout, das Ihre Produkte in den Mittelpunkt stellt. Die flexible Basis der Flex-Theme-Familie.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexBlack",
            "A distinctive boxed storefront with slim Barlow Semi Condensed typography, a black service bar and layered graphite navigation. Crisp white content and soft neutral panels create a restrained, contemporary contrast.",
            "Ein markantes Box-Layout mit schlanker Barlow-Semi-Condensed-Typografie, schwarzer Serviceleiste und abgestuften Graphittönen in der Navigation. Weiße Inhaltsflächen und sanfte Neutraltöne sorgen für einen zurückhaltenden, modernen Kontrast.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexBlue",
            "A signature all-blue header, crisp white product surfaces and warm orange actions. Soft blue details and a deep navy footer give this classic a clear, contemporary feel.",
            "Ein durchgehend blauer Header, klare weiße Produktflächen und warme orangefarbene Akzente. Sanfte Blautöne und ein tiefblauer Footer verleihen diesem Klassiker einen zeitgemäßen Charakter.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexSorbet",
            "A fresh editorial palette of aubergine, lime and mint on warm porcelain, with expressive serif headings.",
            "Eine frische Kombination aus Aubergine, Limette und Mint auf warmem Porzellanweiß. Ausdrucksstarke Serifenschriften setzen die Überschriften in Szene.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexTidal",
            "Atlantic ink, vivid coral and glacial blue. Crisp typography, clean geometry and bright product surfaces.",
            "Atlantikblau, lebendiges Korallrot und kühles Eisblau. Klare Typografie, geradlinige Formen und helle Produktflächen.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexAfterglow",
            "Graphite nights with electric mint, soft orchid and apricot accents. A calm dark canvas with luminous details.",
            "Dunkle Graphittöne mit leuchtendem Mint, sanftem Orchideenlila und Apricot. Ein ruhiges Dark Theme mit strahlenden Akzenten.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexLumen",
            "A bright white canvas with iris-blue actions, soft lavender surfaces and petrol accents. Familiar, airy and quietly modern.",
            "Strahlendes Weiß mit irisblauen Akzenten, sanften Lavendelflächen und Petrol. Vertraut, luftig und dezent modern.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexRiso",
            "Inspired by risograph prints: cobalt ink, fluorescent pink and sunflower yellow on bright white paper. Condensed DIN-style headings and playful off-register accents.",
            "Inspiriert vom Risographie-Druck: Kobaltblau, Neonpink und Sonnengelb auf strahlend weißem Papier. Kompakte Überschriften im DIN-Stil und verspielte, leicht versetzte Farbakzente.");

        builder.AddOrUpdate("Admin.Configuration.Themes.Option.KeepSassCompilerInMemory",
            "Keep Sass compiler in memory",
            "Sass-Compiler im Arbeitsspeicher halten",
            "Recommended while actively editing Sass files or changing theme variables in the admin area. Keeping the Dart Sass compiler running speeds up repeated compilations but uses additional memory. Otherwise, leave this disabled; an unused compiler is stopped after a short idle period.",
            "Empfohlen, wenn Sie gerade intensiv Sass-Dateien bearbeiten oder Theme-Variablen im Backend ändern. Der Dart-Sass-Compiler bleibt aktiv und beschleunigt wiederholte Kompilierungen, benötigt aber zusätzlichen Arbeitsspeicher. Ansonsten deaktiviert lassen; ein ungenutzter Compiler wird nach kurzer Zeit beendet.");

        builder.Delete(
            "Admin.Configuration.Settings.Performance.KeepSassCompilerInMemory",
            "Admin.Configuration.Settings.Performance.KeepSassCompilerInMemory.Hint");
    }

    private static void MigrateCheckoutResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("ShoppingCart.Mini.EmptyCart.Info",
            "You have not added any product to your cart yet. Use the {0} icon to add a product to your cart.",
            "Sie haben noch keine Produkte in Ihren Warenkorb gelegt.<br /> Benutzen Sie das {0} Symbol, um ein Produkt in den Warenkorb zu legen.");
        builder.AddOrUpdate("ShoppingCart.Mini.EmptyWishlist.Info",
            "You have not added any product to your wishlist yet. Use the {0} icon to add a product to your wishlist.",
            "Sie haben noch keine Produkte auf Ihrer Wunschliste vermerkt.<br /> Benutzen Sie das {0} Symbol, um ein Produkt in Ihrer Wunschliste zu vermerken.");
        builder.AddOrUpdate("ShoppingCart.Mini.EmptyCompare.Info",
            "You have not added any product to your compare list yet. Use the {0} icon to add a product to your compare list.",
            "Sie haben noch keine Produkte in Ihrer Vergleichsliste.<br /> Benutzen Sie das {0} Symbol, um ein Produkt in die Vergleichsliste aufzunehmen.");

        builder.Delete(
            "Admin.Orders.Products.AddNew.UnitPriceInclTax.Hint",
            "Admin.Orders.Products.AddNew.UnitPriceExclTax.Hint",
            "Admin.Orders.Products.AddNew.SubTotalInclTax.Hint",
            "Admin.Orders.Products.AddNew.SubTotalExclTax.Hint",
            "Admin.Orders.Products.Edit",
            "Admin.Orders.Products.Edit.InclTax",
            "Admin.Orders.Products.Edit.ExclTax",
            "Admin.Orders.Fields.OrderShippingInclTax.Hint",
            "Admin.Orders.Fields.OrderSubTotalDiscountInclTax.Hint",
            "Admin.Orders.Fields.OrderSubtotalInclTax.Hint",
            "Admin.Orders.Fields.OrderShippingExclTax.Hint",
            "Admin.Orders.Fields.OrderSubTotalDiscountExclTax.Hint",
            "Admin.Orders.Fields.OrderSubtotalExclTax.Hint");

        builder.AddOrUpdate("Admin.Orders.Products.AddNew.UnitPriceInclTax", "Unit price", "Einzelpreis");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.UnitPriceExclTax", "Unit price", "Einzelpreis");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.SubTotalInclTax", "Total", "Gesamt");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.SubTotalExclTax", "Total", "Gesamt");

        builder.AddOrUpdate("Admin.Orders.Fields.Edit.InclTax", "{0} gross", "{0} brutto");
        builder.AddOrUpdate("Admin.Orders.Fields.Edit.ExclTax", "{0} net", "{0} netto");
        builder.AddOrUpdate("Admin.Common.TaxPercent", "tax %", "Steuer %");

        builder.AddOrUpdate("Admin.Orders.Fields.OrderShippingInclTax", "Order shipping (gross)", "Versandkosten (brutto)");
        builder.AddOrUpdate("Admin.Orders.Fields.OrderShippingExclTax", "Order shipping (net)", "Versandkosten (netto)");

        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubTotalDiscountInclTax", "Order subtotal discount (gross)", "Rabatt für Auftragszwischensumme (brutto)");
        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubTotalDiscountExclTax", "Order subtotal discount (net)", "Rabatt für Auftragszwischensumme (netto)");

        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubtotalInclTax", "Order subtotal (gross)", "Auftragszwischensumme (brutto)");
        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubtotalExclTax", "Order subtotal (net)", "Auftragszwischensumme (netto)");

        builder.AddOrUpdate("Admin.Orders.Products.Total")
            .Value("de", "Gesamt");

        builder.AddOrUpdate("Order.PlaceOrderError",
            "The order could not be completed due to an unexpected error. Customer ID: {0}.",
            "Die Bestellung konnte aufgrund eines unerwarteten Fehlers nicht abgeschlossen werden. Kunden-ID: {0}.");

        builder.AddOrUpdate("Order.AlreadyExists",
            "The order has already been created. Customer ID: {0}.",
            "Die Bestellung wurde bereits angelegt. Kunden-ID: {0}.");


    }

    private static void MigrateCommonResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Common.Email", "Email", "E-Mail");

        builder.AddOrUpdate("Admin.Common.Store.LimitedTo",
            "Limited to stores",
            "Auf Shops begrenzt",
            "Specifies whether the object is only available in certain stores.",
            "Legt fest, ob das Objekt nur für bestimmte Shops verfügbar ist.");

        builder.AddOrUpdate("OmmCMS.AI.Prompts.UseImagePlaceholders",
            "If an image is to be inserted, output this HTML placeholder: <div class=\"mb-3\"><sm-icon name=\"file-image\" size=\"xl\" class=\"ai-preview-file\" title=\"SECTION_HEADING\"></sm-icon></div>. Replace SECTION_HEADING with the associated section heading, escaped for an HTML attribute. Keep the element names and all other attributes exactly as shown. The sm-icon element must remain empty and have an explicit closing tag; do not replace it with an i, img or svg element or add Font Awesome classes.",
            "Wenn ein Bild einzufügen ist, gib diesen HTML-Platzhalter aus: <div class=\"mb-3\"><sm-icon name=\"file-image\" size=\"xl\" class=\"ai-preview-file\" title=\"ABSCHNITTSÜBERSCHRIFT\"></sm-icon></div>. Ersetze ABSCHNITTSÜBERSCHRIFT durch die zugehörige Abschnittsüberschrift, korrekt für ein HTML-Attribut maskiert. Behalte die Elementnamen und alle anderen Attribute exakt bei. Das sm-icon-Element muss leer bleiben und ein explizites schließendes Tag haben; ersetze es nicht durch ein i-, img- oder svg-Element und ergänze keine Font-Awesome-Klassen.");

        builder.AddOrUpdate("Common.Select.AddPlaceholder", "＋ Add…", "＋ Hinzufügen…");

        builder.AddOrUpdate("Common.MoreOptions", "More options", "Weitere Optionen");

        builder.AddOrUpdate("Admin.DataExchange.Export.Filter.IsPublished.Hint",
            "Specifies whether only published objects are exported, provided the object has a publishing setting.",
            "Legt fest, ob nur Objekte, die veröffentlicht wurden, exportiert werden, sofern das Objekt eine Einstellung zur Veröffentlichung besitzt.");

        builder.AddOrUpdate("Footer.Info", "Information", "Informationen");

        builder.AddOrUpdate("OmmCMS.AI.Prompts.DontUseHtml",
            "Return plain text only. Do not use HTML tags.",
            "Gib ausschließlich Klartext zurück. Verwende keine HTML-Tags.");

        builder.AddOrUpdate("Admin.Common.AddEntry", "Add entry…", "Eintrag hinzufügen…");
        builder.AddOrUpdate("Admin.Common.Reorder", "Reorder (Up/Down arrow keys)", "Reihenfolge ändern (Pfeiltasten hoch/runter)");

        builder.AddOrUpdate("Admin.Common.InvalidFileName", "Invalid file name \"{0}\".", "Ungültiger Dateiname \"{0}\".");
    }
}
