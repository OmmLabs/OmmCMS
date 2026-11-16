using NUnit.Framework;
using OmmCMS.Web.Rendering.Choices;

namespace OmmCMS.Web.Tests.Common.Rendering.Choices;

[TestFixture]
public class ChoiceItemModelTests
{
    [Test]
    public void Can_generate_single_color_css()
    {
        var item = new TestChoiceItemModel { Color = "#123456" };

        Assert.That(item.GetSwatchColorCss(), Is.EqualTo("background-color: #123456;"));
    }

    [Test]
    public void Can_generate_multicolor_css()
    {
        var item = new TestChoiceItemModel
        {
            Color = "#ff0000",
            AdditionalColors = ["#00ff00", "#0000ff"]
        };

        Assert.That(item.GetSwatchColorCss(), Is.EqualTo(
            "--swatch-color-count: 3;--swatch-color-1: #ff0000;" +
            "--swatch-color-2: #00ff00;--swatch-color-3: #0000ff;"));
    }

    [Test]
    public void Ignores_empty_transparent_and_excess_additional_colors()
    {
        var item = new TestChoiceItemModel
        {
            Color = "#111111",
            AdditionalColors = [null, " transparent ", " ", "#222222", "#333333", "#444444", "#555555"]
        };

        var css = item.GetSwatchColorCss();

        Assert.That(css, Does.Contain("#444444"));
        Assert.That(css, Does.Not.Contain("#555555"));
        Assert.That(css, Does.Not.Contain("transparent"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("transparent")]
    [TestCase(" Transparent ")]
    [TestCase(" ")]
    public void Does_not_generate_css_without_primary_color(string color)
    {
        var item = new TestChoiceItemModel
        {
            Color = color,
            AdditionalColors = ["#123456"]
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.GetSwatchColorCss(), Is.Null);
            Assert.That(item.HasColor, Is.False);
            Assert.That(item.HasMultipleColors, Is.False);
        }
    }

    [Test]
    public void Builds_swatch_card_value_with_quantity()
    {
        var item = new TestChoiceItemModel
        {
            Name = "Red",
            QuantityInfo = 2
        };

        Assert.That(item.SwatchValueText, Is.EqualTo("2 x Red"));
    }

    [Test]
    public void Builds_accessible_swatch_card_label()
    {
        var item = new TestChoiceItemModel { Name = "Red", QuantityInfo = 2, Title = "Unavailable" };

        Assert.That(item.GetSwatchCardLabel(), Is.EqualTo("2 x Red"));
        item.IsUnavailable = true;
        Assert.That(item.GetSwatchCardLabel(), Is.EqualTo("2 x Red, Unavailable"));
    }

    [Test]
    public void Adds_unavailability_reason_to_swatch_display_name()
    {
        var item = new TestChoiceItemModel
        {
            Name = "Red",
            Title = "Out of stock"
        };

        Assert.That(item.GetSwatchDisplayName(), Is.EqualTo("Red"));
        Assert.That(item.UnavailableReason, Is.Null);

        item.IsUnavailable = true;

        Assert.That(item.GetSwatchDisplayName(), Is.EqualTo("Red — Out of stock"));
        Assert.That(item.UnavailableReason, Is.EqualTo("Out of stock"));
    }

    private sealed class TestChoiceItemModel : ChoiceItemModel
    {
        public override string GetItemLabel() => Name;
    }
}
