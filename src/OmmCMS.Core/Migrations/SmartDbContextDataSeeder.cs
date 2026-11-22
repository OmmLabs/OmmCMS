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

    }

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        MigrateThemeResources(builder);
        MigrateDashboardAndNavigationResources(builder);
        MigrateSettingsResources(builder);
        MigrateChoiceResources(builder);
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
        builder.AddOrUpdate("Admin.Dashboard.Labels.Customer", "Customer", "Kunde");

        builder.Delete("FixedValueOrPercent.Absolute", "FixedValueOrPercent.Percentage",
            "Common.AmountOrPercent.Absolute", "Common.AmountOrPercent.Percentage");
        builder.AddOrUpdate("Permissions.DisplayName.Dashboard", "Dashboard", "Dashboard");
        builder.AddOrUpdate("Permissions.DisplayName.Widgets", "Widgets", "Widgets");
        builder.AddOrUpdate("Permissions.DisplayName.Customers", "Customers", "Kunden");
        builder.AddOrUpdate("Permissions.DisplayName.RegisteredCustomers", "Customer registrations", "Kundenregistrierungen");
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

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerFormFields.Description",
            "Manage form fields that are displayed during registration. In order to ensure the address transfer from the registration form, it is necessary that the following fields are activated and filled in by the customer:<ul class=\"mb-0\"><li>First name</li><li>Last name</li><li>Email</li><li>and all fields that are selected as required in the tab <b>Addresses</b></li></ul>",
            "Sie können Formularfelder verwalten, die während der Registrierung angezeigt werden. Um die Adressübergabe aus dem Registrierungsformular zu gewährleisten, müssen folgende Felder aktiviert und vom Kunden ausgefüllt sein:<ul class=\"mb-0\"><li>Vorname</li><li>Nachname</li><li>E-Mail</li><li>und alle Felder die im Tab <b>Adressen</b> als erforderlich ausgewählt sind</li></ul>");

        builder.AddOrUpdate("Admin.Configuration.Settings.Icons", "Icons", "Icons");

        var prefix = "Admin.Configuration.Settings.Icons.";

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

    private static void MigrateChoiceResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Admin.Common.ColorPalette.Color",
            "Color",
            "Farbe",
            "Define up to four colors for this attribute value. The colors are only displayed when the attribute's control type is set to swatches.",
            "Legen Sie bis zu vier Farben für diesen Attributwert fest. Die Farben werden nur angezeigt, wenn das Attribut als Swatches (Farb- und Bildmuster) dargestellt wird.");
        builder.AddOrUpdate("Admin.Common.ColorPalette.AddColor", "Add color", "Farbe hinzufügen");
        builder.AddOrUpdate("Admin.Common.ColorPalette.RemoveColor", "Remove this color field", "Dieses Farbfeld entfernen");

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
            "A clean, versatile website theme with bright surfaces, clear typography and a content-focused layout. The flexible foundation for the Flex theme family.",
            "Ein klares, vielseitiges Website-Theme mit hellen Flächen, gut lesbarer Typografie und einem Layout, das Ihre Inhalte in den Mittelpunkt stellt. Die flexible Basis der Flex-Theme-Familie.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexBlack",
            "A distinctive boxed website theme with slim Barlow Semi Condensed typography, a black service bar and layered graphite navigation. Crisp white content and soft neutral panels create a restrained, contemporary contrast.",
            "Ein markantes Box-Layout mit schlanker Barlow-Semi-Condensed-Typografie, schwarzer Serviceleiste und abgestuften Graphittönen in der Navigation. Weiße Inhaltsflächen und sanfte Neutraltöne sorgen für einen zurückhaltenden, modernen Kontrast.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexBlue",
            "A signature all-blue header, crisp white content surfaces and warm orange actions. Soft blue details and a deep navy footer give this classic a clear, contemporary feel.",
            "Ein durchgehend blauer Header, klare weiße Inhaltsflächen und warme orangefarbene Akzente. Sanfte Blautöne und ein tiefblauer Footer verleihen diesem Klassiker einen zeitgemäßen Charakter.");

        builder.AddOrUpdate("Admin.Configuration.Themes.Option.KeepSassCompilerInMemory",
            "Keep Sass compiler in memory",
            "Sass-Compiler im Arbeitsspeicher halten",
            "Recommended while actively editing Sass files or changing theme variables in the admin area. Keeping the Dart Sass compiler running speeds up repeated compilations but uses additional memory. Otherwise, leave this disabled; an unused compiler is stopped after a short idle period.",
            "Empfohlen, wenn Sie gerade intensiv Sass-Dateien bearbeiten oder Theme-Variablen im Backend ändern. Der Dart-Sass-Compiler bleibt aktiv und beschleunigt wiederholte Kompilierungen, benötigt aber zusätzlichen Arbeitsspeicher. Ansonsten deaktiviert lassen; ein ungenutzter Compiler wird nach kurzer Zeit beendet.");

        builder.Delete(
            "Admin.Configuration.Settings.Performance.KeepSassCompilerInMemory",
            "Admin.Configuration.Settings.Performance.KeepSassCompilerInMemory.Hint");
    }

    private static void MigrateCommonResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Common.Email", "Email", "E-Mail");

        builder.AddOrUpdate("Admin.Common.Store.LimitedTo",
            "Limited to sites",
            "Auf Websites begrenzt",
            "Specifies whether the object is only available on certain sites.",
            "Legt fest, ob das Objekt nur für bestimmte Websites verfügbar ist.");

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
