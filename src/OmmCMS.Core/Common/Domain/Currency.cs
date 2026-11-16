using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Stores;
using OmmCMS.Data.Caching;

namespace OmmCMS.Core.Common;

internal class CurrencyMap : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.Property(c => c.Rate).HasPrecision(18, 8);
    }
}

/// <summary>
/// Represents a currency
/// </summary>
[Index(nameof(DisplayOrder), Name = "IX_Currency_DisplayOrder")]
[CacheableEntity]
[LocalizedEntity("Published")]
public partial class Currency : EntityWithAttributes, IAuditable, ILocalizedEntity, IStoreRestricted, IDisplayOrder, ICloneable<Currency>
{
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    [Required, StringLength(50)]
    [LocalizedProperty]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the ISO currency code
    /// </summary>
    [Required, StringLength(5)]
    public string CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the rate
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Gets or sets the display locale
    /// </summary>
    [StringLength(50)]
    public string DisplayLocale { get; set; }

    /// <summary>
    /// Gets or sets the custom formatting
    /// </summary>
    [StringLength(50)]
    public string CustomFormatting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is published
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time of instance creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of instance update
    /// </summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the (comma separated) list of domain endings (e.g. country code top-level domains) to which this currency is the default one
    /// </summary>
    [StringLength(1000)]
    public string DomainEndings { get; set; }

    #region Rounding

    /// <summary>
    /// Gets or sets the number of decimal places to round to
    /// </summary>
    public int RoundNumDecimals { get; set; } = 2;

    /// <summary>
    /// Gets or sets the rounding strategy of the midpoint between two currency values.
    /// </summary>
    public CurrencyMidpointRounding MidpointRounding { get; set; } = CurrencyMidpointRounding.AwayFromZero;

    /// <summary>
    /// Gets or sets a value indicating whether currency amount rounding is enabled.
    /// Will be obtained from <see cref="CurrencySettings.RoundingEnabled"/> if <c>null</c>.
    /// </summary>
    public bool? RoundingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to round to the nearest denomination.
    /// </summary>
    public bool RoundToNearestEnabled { get; set; }

    /// <summary>
    /// Gets or sets the smallest denomination. The amount is rounded to the nearest multiple of it.
    /// </summary>
    public decimal RoundingDenomination { get; set; }

    /// <summary>
    /// Gets or sets the denomination rounding rule.
    /// </summary>
    public CurrencyRoundingRule RoundingRule { get; set; }

    #endregion

    public Currency Clone()
    {
        return (Currency)this.MemberwiseClone();
    }

    object ICloneable.Clone()
    {
        return this.MemberwiseClone();
    }

    #region Utils

    private NumberFormatInfo _numberFormat;

    [NotMapped, IgnoreDataMember]
    public NumberFormatInfo NumberFormat
    {
        get
        {
            if (_numberFormat == null && !string.IsNullOrEmpty(DisplayLocale))
            {
                try
                {
                    _numberFormat = CultureInfo.CreateSpecificCulture(DisplayLocale).NumberFormat;
                }
                catch
                {
                }
            }

            if (_numberFormat == null)
            {
                _numberFormat = NumberFormatInfo.CurrentInfo;
            }

            return _numberFormat;
        }
    }

    #endregion
}
