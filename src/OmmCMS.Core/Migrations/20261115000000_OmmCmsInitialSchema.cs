using FluentMigrator;
using OmmCMS.Data.Migrations;

namespace OmmCMS.Core.Data.Migrations;

/// <summary>
/// Initial OmmCMS schema marker. Fresh installation creates the current entity model,
/// then records this migration as applied. Historical commerce upgrades are unsupported.
/// </summary>
[MigrationVersion("2026-11-15 00:00:00", "OmmCMS initial schema")]
internal sealed class OmmCmsInitialSchema : Migration, ILocaleResourcesProvider
{
    public override void Up()
        => throw new NotSupportedException("The initial OmmCMS schema must be created by the fresh installer.");

    public override void Down()
        => throw new NotSupportedException("The OmmCMS initial schema cannot be rolled back.");

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Enums.SwatchSize.XSmall", "XS", "XS");
        builder.AddOrUpdate("Enums.SwatchSize.Small", "S", "S");
        builder.AddOrUpdate("Enums.SwatchSize.Medium", "M", "M");
        builder.AddOrUpdate("Enums.SwatchSize.Large", "L", "L");
        builder.AddOrUpdate("Enums.SwatchSize.XLarge", "XL", "XL");
        builder.AddOrUpdate("Enums.SwatchSize.XXLarge", "XXL", "XXL");

        builder.AddOrUpdate("Enums.SwatchShape.Rounded", "Pill", "Abgerundet");
        builder.AddOrUpdate("Enums.SwatchShape.Rect", "Rectangular", "Rechteckig");
        builder.AddOrUpdate("Enums.SwatchShape.Circle", "Circle", "Rund");

        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.None", "None", "Keine");
        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.Adjustment", "Price adjustment", "Preisanpassung");
        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.FinalPrice",
            "Product, Comparison, and Base Price",
            "Produkt-, Vergleichs- und Grundpreis");

        builder.AddOrUpdate("Admin.Common.ResetToDefault",
            "Reset to default value.",
            "Auf den Standardwert zurücksetzen.");

        builder.AddOrUpdate("Admin.Common.ClickToSetDifferentValue",
            "Set a value that differs from the default by clicking.",
            "Klicken, um den Standardwert für diese Einstellung zu überschreiben.");

        builder.AddOrUpdate("Admin.Common.ClickToSetValue",
            "No value set (default). Click to set a value.",
            "Kein Wert gesetzt (Standard). Klicken, um einen Wert festzulegen.");

        builder.AddOrUpdate("Enums.AttributeControlType.Boxes",
            "Color and image swatches",
            "Farb- und Bildmuster");
        builder.AddOrUpdate("Enums.FacetTemplateHint.Custom",
            "Color and image swatches",
            "Farb- und Bildmuster");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Swatches",
            "Color and image swatches",
            "Swatches (Farb- und Bildmuster)");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.EditSwatches",
            "Edit color and image swatches",
            "Farb- und Bildmuster bearbeiten");


        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchSize",
            "Default size of swatches",
            "Standardgröße von Swatches (Farb- und Bildmuster)",
            "Sets the default swatch size. It can be overridden for product and checkout attributes and for individual products. Recommended size is M.",
            "Legt die Standardgröße der Muster fest. Sie kann bei Produkt- und Checkoutattributen sowie bei einzelnen Produkten überschrieben werden. Empfohlen wird Größe M.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchShape",
            "Default shape of swatches",
            "Standardform von Swatches (Farb- und Bildmuster)",
            "Sets the default swatch shape. It can be overridden for product and checkout attributes and for individual products. Default is \"Pill\".",
            "Legt die Standardform der Muster fest. Sie kann bei Produkt- und Checkoutattributen sowie bei einzelnen Produkten überschrieben werden. Standard ist \"Abgerundet\".");


        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchSize",
            "Swatch Size",
            "Mustergröße",
            "Sets the swatch size. Names and prices appear inside the swatch from size L when every option has a color or image.",
            "Legt die Mustergröße fest. Namen und Preise erscheinen ab Größe L im Muster, wenn jede Option eine Farbe oder ein Bild besitzt.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchAspectRatio",
            "Swatch Aspect Ratio",
            "Muster-Seitenverhältnis",
            "Sets the height-to-width ratio. Default is 1:1 (square). It only affects card-style swatches, used from size L when name or price display is enabled and every option has a color or image.",
            "Legt das Höhen-Breiten-Verhältnis fest. Standard ist 1:1 (quadratisch). Es wirkt nur im Kartenformat, das ab Größe L bei aktivierter Namens- oder Preisanzeige verwendet wird, wenn jede Option eine Farbe oder ein Bild besitzt.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchShape",
            "Swatch Shape",
            "Musterform",
            "Sets the swatch shape. Circle is replaced by Pill when names or prices are shown inside the swatch or an option has no color or image.",
            "Legt die Musterform fest. Rund wird durch Abgerundet ersetzt, wenn Namen oder Preise im Muster erscheinen oder eine Option keine Farbe und kein Bild besitzt.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.ShowValueNameInSwatch",
            "Show option name",
            "Optionsnamen anzeigen",
            "Shows the option name inside the swatch. The name is always shown with price information. Card layout requires size L or larger and a color or image for every option.",
            "Zeigt den Optionsnamen im Muster an. Bei eingeblendeten Preisinformationen wird der Name immer angezeigt. Das Kartenformat erfordert mindestens Größe L sowie eine Farbe oder ein Bild für jede Option.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchPriceDisplay",
            "Swatch Price Display",
            "Preisanzeige im Muster",
            "Shows available price information inside the swatch. This requires visible prices, size L or larger, and a color or image for every option.",
            "Zeigt verfügbare Preisinformationen im Muster an. Dafür müssen Preise sichtbar sein, mindestens Größe L eingestellt sein und jede Option eine Farbe oder ein Bild besitzen.");


        builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.SwatchInfo",
            "Use these settings to override the default attribute values for this product.",
            "Legen Sie hier abweichende Einstellungen fest, wenn Sie die Vorgaben des Attributs für dieses Produkt überschreiben möchten.");

        builder.AddOrUpdate("Admin.Common.Overwritten", "Overwritten: {0}.", "Abweichend: {0}.");

        builder.AddOrUpdate("Admin.Common.ColorPalette.TooManyColors",
            "A maximum of {0} colors can be specified.",
            "Es können maximal {0} Farben angegeben werden.");

        builder.AddOrUpdate("Admin.Common.ColorPalette.PrimaryColorRequired",
            "Select the primary color before adding more colors.",
            "Wählen Sie zuerst die Hauptfarbe aus, bevor Sie weitere Farben hinzufügen.");

        builder.AddOrUpdate("Common.CompletedOn", "Completed on", "Abgeschlossen am");

        builder.AddOrUpdate("Admin.Catalog.Products.Fields.AllowWithdrawalBeforeShipping",
            "Allow withdrawal before shipping",
            "Widerruf vor Versand erlauben",
            "Specifies whether this product can still be withdrawn before it has been shipped if it is otherwise excluded from withdrawal (withdrawal period 0)."
            + " This setting applies only to items eligible for shipping.",
            "Legt fest, ob dieser Artikel noch widerrufen werden kann, bevor er versendet wurde, obwohl er ansonsten vom Widerruf ausgeschlossen ist (Widerrufsfrist 0)."
            + " Diese Einstellung gilt ausschließlich für versandfähige Artikel.");

        builder.AddOrUpdate("Admin.Catalog.Categories.Fields.AllowWithdrawalBeforeShipping",
            "Allow withdrawal before shipping",
            "Widerruf vor Versand erlauben",
            "Specifies whether products in this category can still be withdrawn before they have been shipped if they are otherwise excluded from withdrawal (withdrawal period 0)."
            + " A product setting takes precedence. For multiple categories, \"No\" takes precedence over \"Yes\"."
            + " This setting applies only to items eligible for shipping.",
            "Legt fest, ob Artikel dieser Warengruppe noch widerrufen werden können, bevor sie versendet wurden, obwohl sie ansonsten vom Widerruf ausgeschlossen sind (Widerrufsfrist 0)."
            + " Eine Einstellung am Artikel hat Vorrang. Bei mehreren Warengruppen gilt \"Nein\" vor \"Ja\"."
            + " Diese Einstellung gilt ausschließlich für versandfähige Artikel.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.CheckoutAttributes.Fields.Requirements",
            "Requirements",
            "Voraussetzungen",
            "Select cart rule sets that determine when the checkout attribute is available. The attribute is available when at least one rule set matches.",
            "Wählen Sie Warenkorb-Regelsätze aus, die festlegen, wann das Checkout-Attribut verfügbar ist. Das Attribut ist verfügbar, sobald einer der ausgewählten Regelsätze zutrifft.");

        builder.AddOrUpdate("Admin.Orders.Products.WithdrawalWaiver",
            "Withdrawal waiver",
            "Widerrufsverzicht",
            "Indicates whether and when the customer accepted the withdrawal waiver for this order item.",
            "Zeigt an, ob und wann der Kunde den Widerrufsverzicht für diese Auftragsposition erklärt hat.");
    }
}
