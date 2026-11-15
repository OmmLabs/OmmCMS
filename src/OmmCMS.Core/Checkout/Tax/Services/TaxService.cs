using System.Runtime.CompilerServices;
using OmmCMS.Core.Common;
using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Checkout.Tax;

public partial class TaxService : ITaxService
{
    const string DefaultTaxFormat = "{0} *";

    private readonly IWorkContext _workContext;
    private readonly ILocalizationService _localizationService;
    private readonly TaxSettings _taxSettings;

    public TaxService(
        IWorkContext workContext,
        ILocalizationService localizationService,
        TaxSettings taxSettings)
    {
        _workContext = workContext;
        _localizationService = localizationService;
        _taxSettings = taxSettings;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual string FormatTaxRate(decimal taxRate)
        => taxRate.ToString("G29");

    public virtual string GetTaxFormat(
        bool? displayTaxSuffix = null,
        bool? priceIncludesTax = null,
        PricingTarget target = PricingTarget.Product,
        Language language = null)
    {
        displayTaxSuffix ??= _taxSettings.DisplayTaxSuffix;

        if (displayTaxSuffix == true)
        {
            // Show tax suffix.
            priceIncludesTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            language ??= _workContext.WorkingLanguage;

            string resource = _localizationService.GetResource(priceIncludesTax.Value ? "Products.InclTaxSuffix" : "Products.ExclTaxSuffix", language.Id, false);
            var postFormat = resource.NullEmpty() ?? DefaultTaxFormat;

            return postFormat;
        }
        else
        {
            return null;
        }
    }
}
