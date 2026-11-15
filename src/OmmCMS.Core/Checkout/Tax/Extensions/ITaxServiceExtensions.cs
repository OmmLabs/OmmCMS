using OmmCMS.Core;
using OmmCMS.Core.Checkout.Tax;
using OmmCMS.Core.Common;
using OmmCMS.Core.Localization;

namespace OmmCMS;

public static class ITaxServiceExtensions
{
    /// <summary>
    ///     Applies a tax formatting pattern to given <c>product</c> money <paramref name="source"/>,
    ///     e.g. "{0} *", "{0} incl. tax"
    /// </summary>
    /// <param name="source">
    ///     The source <see cref="Money"/> to apply formatting to.
    /// </param>
    /// <param name="displayTaxSuffix">
    ///     A value indicating whether to display the tax suffix.
    ///     If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/>.
    /// </param>
    /// <param name="priceIncludesTax">
    ///     A value indicating whether given price includes tax already.
    ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
    /// </param>
    /// <param name="language">
    ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
    /// </param>
    /// <returns>Money</returns>
    public static Money ApplyTaxFormat(this ITaxService taxService,
        Money source,
        bool? displayTaxSuffix = null,
        bool? priceIncludesTax = null,
        Language language = null)
    {
        if (source == 0)
            return source;

        var format = taxService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.Product, language);
        return source.WithPostFormat(format);
    }

}
