
namespace OmmCMS.Core.Common.Services;

/// <summary>
/// Represents rounding of currency amounts.
/// </summary>
public partial interface IRoundingHelper
{
    /// <summary>Rounds <paramref name="amount"/>.</summary>
    /// <param name="amount">Amount to round.</param>
    /// <param name="currency">
    /// Rounds <paramref name="amount"/> using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
    /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
    /// </param>
    /// <returns>Rounded amount.</returns>
    decimal Round(decimal amount, Currency currency = null);

    /// <summary>
    /// Rounds <paramref name="amount"/>.
    /// </summary>
    /// <param name="amount">Amount to round.</param>
    /// <param name="decimals">Number of decimal places (precision).</param>
    /// <param name="midpointRounding">The rounding strategy of the midway between two currency amounts.</param>
    /// <returns>Rounded amount.</returns>
    decimal Round(decimal amount, int decimals, CurrencyMidpointRounding midpointRounding = CurrencyMidpointRounding.AwayFromZero);

    /// <summary>
    /// Gets a value indicating whether currency rounding is enabled.
    /// </summary>
    /// <param name="currency">
    /// Currency. If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
    /// </param>
    /// <returns></returns>
    bool IsCurrencyRoundingEnabled(Currency currency = null);

    /// <summary>
    /// Rounds <paramref name="amount"/> if currency rounding is enabled.
    /// </summary>
    /// <param name="amount">Amount to round.</param>
    /// <param name="currency">
    /// Rounds <paramref name="amount"/> using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
    /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
    /// </param>
    /// <returns>Rounded amount if rounding is enabled for <paramref name="currency"/>, <paramref name="amount"/> otherwise.</returns>
    decimal RoundIfEnabledFor(decimal amount, Currency currency = null);

    /// <summary>
    /// Round amount up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
    /// </summary>
    /// <param name="amount">Amount to round.</param>
    /// <param name="toNearestRounding">Amount by which was rounded.</param>
    /// <param name="currency">
    /// Currency. <see cref="Currency.RoundOrderTotalEnabled"/> must be activated for this currency.
    /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
    /// </param>
    /// <returns>Rounded amount.</returns>
    /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
    /// <remarks>Usually this method is used to round the order total.</remarks>
    decimal ToNearest(decimal amount, out decimal toNearestRounding, Currency currency = null);
}