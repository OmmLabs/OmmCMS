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

        builder.AddOrUpdate("Admin.Common.Overwritten", "Overwritten: {0}.", "Abweichend: {0}.");

        builder.AddOrUpdate("Admin.Common.ColorPalette.TooManyColors",
            "A maximum of {0} colors can be specified.",
            "Es können maximal {0} Farben angegeben werden.");

        builder.AddOrUpdate("Admin.Common.ColorPalette.PrimaryColorRequired",
            "Select the primary color before adding more colors.",
            "Wählen Sie zuerst die Hauptfarbe aus, bevor Sie weitere Farben hinzufügen.");

        builder.AddOrUpdate("Common.CompletedOn", "Completed on", "Abgeschlossen am");

    }
}
