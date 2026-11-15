using System.Runtime.CompilerServices;
using OmmCMS.Core.Common.Configuration;

namespace OmmCMS.Core.Common.Services;

public partial class RoundingHelper : IRoundingHelper
{
    private readonly IWorkContext _workContext;
    private readonly CurrencySettings _currencySettings;

    public RoundingHelper(IWorkContext workContext, CurrencySettings currencySettings)
    {
        _workContext = workContext;
        _currencySettings = currencySettings;
    }

    public virtual decimal Round(decimal amount, Currency currency = null)
    {
        currency ??= _workContext.WorkingCurrency;
        return Round(amount, currency.RoundNumDecimals, currency.MidpointRounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual decimal Round(decimal amount, int decimals, CurrencyMidpointRounding midpointRounding = CurrencyMidpointRounding.AwayFromZero)
    {
        return decimal.Round(amount, decimals, Convert(midpointRounding));
    }

    public virtual bool IsCurrencyRoundingEnabled(Currency currency = null)
    {
        currency ??= _workContext.WorkingCurrency;

        return currency.RoundOrderItemsEnabled ?? _currencySettings.RoundOrderItemsEnabled;
    }

    public virtual decimal RoundIfEnabledFor(decimal amount, Currency currency = null)
    {
        currency ??= _workContext.WorkingCurrency;

        if (amount == 0m || !IsCurrencyRoundingEnabled(currency))
        {
            return amount;
        }

        return Round(amount, currency.RoundNumDecimals, currency.MidpointRounding);
    }

    public virtual decimal ToNearest(decimal amount, out decimal toNearestRounding, Currency currency = null)
    {
        currency ??= _workContext.WorkingCurrency;

        var oldValue = amount;

        switch (currency.RoundOrderTotalRule)
        {
            case CurrencyRoundingRule.RoundMidpointUp:
                amount = ToNearest(amount, currency, CurrencyMidpointRounding.AwayFromZero, null);
                break;
            case CurrencyRoundingRule.AlwaysRoundDown:
                amount = ToNearest(amount, currency, null, false);
                break;
            case CurrencyRoundingRule.AlwaysRoundUp:
                amount = ToNearest(amount, currency, null, true);
                break;
            case CurrencyRoundingRule.RoundMidpointDown:
            default:
                amount = ToNearest(amount, currency, CurrencyMidpointRounding.ToEven, null);
                break;
        }

        toNearestRounding = amount - Round(oldValue, currency.RoundNumDecimals, currency.MidpointRounding);

        return amount;
    }

    protected virtual decimal ToNearest(decimal amount, Currency currency, CurrencyMidpointRounding? midpointRounding, bool? roundUp)
    {
        if (currency.RoundOrderTotalDenominator != 0m)
        {
            if (midpointRounding.HasValue)
            {
                return decimal.Round(amount / currency.RoundOrderTotalDenominator, 0, Convert(midpointRounding.Value)) * currency.RoundOrderTotalDenominator;
            }
            else if (roundUp.HasValue)
            {
                var roundedAmountBase = roundUp.Value
                    ? decimal.Ceiling(amount / currency.RoundOrderTotalDenominator)
                    : decimal.Floor(amount / currency.RoundOrderTotalDenominator);

                return Round(roundedAmountBase, 0, currency.MidpointRounding) * currency.RoundOrderTotalDenominator;
            }
        }

        return amount;
    }

    protected virtual MidpointRounding Convert(CurrencyMidpointRounding midpointRounding)
    {
        switch (midpointRounding)
        {
            case CurrencyMidpointRounding.ToEven:
                return MidpointRounding.ToEven;
            case CurrencyMidpointRounding.AwayFromZero:
            default:
                return MidpointRounding.AwayFromZero;
        }
    }
}