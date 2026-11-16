using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Common.Configuration;

/// <summary>
/// Global currency settings
/// </summary>
public class CurrencySettings : ISettings
{
    /// <summary>
    /// Primary currency identifier
    /// </summary>
    public int PrimaryCurrencyId { get; set; }

    /// <summary>
    ///  Primary exchange rate currency identifier
    /// </summary>
    public int PrimaryExchangeCurrencyId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically update currency rates.
    /// </summary>
    public bool AutoUpdateEnabled { get; set; }

    /// <summary>
    /// Active exchange rate provider system name
    /// </summary>
    public string ActiveExchangeRateProviderSystemName { get; set; }

    #region Rounding

    /// <summary>
    /// Gets or sets a value indicating whether currency amount rounding is enabled.
    /// </summary>
    public bool RoundingEnabled { get; set; }

    #endregion
}
