namespace OmmCMS.Admin.Models.Common;

[LocalizedDisplay("Admin.Configuration.Countries.Fields.")]
public class CountrySearchListModel : EntityModelBase
{
    [LocalizedDisplay("*Name")]
    public string SearchName { get; set; }

    [LocalizedDisplay("*TwoLetterIsoCode")]
    public string SearchTwoLetterIsoCode { get; set; }

    [LocalizedDisplay("*Published")]
    public bool? SearchPublished { get; set; }
}
