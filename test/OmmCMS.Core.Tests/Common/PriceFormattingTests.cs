using System;
using System.Net.Http;
using NUnit.Framework;
using OmmCMS.Core.Common;
using OmmCMS.Core.Localization;
using OmmCMS.Test.Common;

namespace OmmCMS.Core.Tests.Common;

[TestFixture]
public class PriceFormattingTests : ServiceTestBase
{
    Currency _currencyEUR;
    Currency _currencyUSD;

    [SetUp]
    public new void SetUp()
    {
        _currencyEUR = new Currency
        {
            Id = 1,
            Name = "Euro",
            CurrencyCode = "EUR",
            DisplayLocale = "",
            CustomFormatting = "€0.00",
            DisplayOrder = 1,
            Published = true,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        _currencyUSD = new Currency
        {
            Id = 1,
            Name = "US Dollar",
            CurrencyCode = "USD",
            DisplayLocale = "en-US",
            CustomFormatting = "",
            DisplayOrder = 2,
            Published = true,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

    }

    [Test]
    public void Can_formatPrice_with_custom_currencyFormatting()
    {
        using (CultureHelper.Use("en-US"))
        {
            new Money(1234.5M, _currencyEUR).ToString().ShouldEqual("€1234.50");
            _currencyEUR.CustomFormatting = "€ 0.00";
            new Money(1234.5M, _currencyEUR).ToString().ShouldEqual("€ 1234.50");
        }
    }

    [Test]
    public void Can_formatPrice_with_distinct_currencyDisplayLocale()
    {
        new Money(1234.5M, _currencyUSD).ToString().ShouldEqual("$1,234.50");

        using (CultureHelper.Use("de-DE"))
        {
            _currencyEUR.CustomFormatting = string.Empty;
            new Money(1234.5M, _currencyEUR).ToString().ShouldEqual("1.234,50 €");
        }
    }

    [Test]
    public void Can_formatPrice_with_showCurrencyCode()
    {
        new Money(1234.5M, _currencyUSD, false).ToString().ShouldEqual("$1,234.50");
        new Money(1234.5M, _currencyUSD, true).ToString().ShouldEqual("1,234.50");
    }
}