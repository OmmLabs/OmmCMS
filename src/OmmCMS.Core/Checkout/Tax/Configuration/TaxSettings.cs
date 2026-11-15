using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Checkout.Tax;

public class TaxSettings : ISettings
{
    /// <summary>
    /// Tax display type
    /// </summary>
    public TaxDisplayType TaxDisplayType { get; set; } = TaxDisplayType.ExcludingTax;


    /// <summary>
    /// Gets or sets a value indicating whether customers are allowed to select tax display type
    /// </summary>
    public bool AllowCustomersToSelectTaxDisplayType { get; set; }



    /// <summary>
    /// Gets or sets a value indicating whether to show legal info in footer
    /// </summary>
    public bool ShowLegalHintsInFooter { get; set; } = true;

}
