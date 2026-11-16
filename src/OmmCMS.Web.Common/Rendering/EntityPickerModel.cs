using OmmCMS.Web.Modelling;

namespace OmmCMS.Web.Rendering;

public class EntityPickerModel : ModelBase
{
    public string EntityType { get; set; }
    public bool HighlightSearchTerm { get; set; }
    public string DisableIf { get; set; }
    public string DisableIds { get; set; }
    public string SearchTerm { get; set; }
    public string ReturnField { get; set; }
    public int MaxItems { get; set; }
    public string Selected { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 96;

    public List<SearchResultModel> SearchResult { get; set; }

    #region Search properties

    [LocalizedDisplay("Admin.Common.Store.SearchFor")]
    public int StoreId { get; set; }

    [LocalizedDisplay("Common.Search")]
    public string CustomerSearchType { get; set; }

    #endregion

    public class SearchResultModel : EntityModelBase
    {
        public string ReturnValue { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string SummaryTitle { get; set; }
        public bool? Published { get; set; }
        public bool Disable { get; set; }
        public bool Selected { get; set; }
        public string ImageUrl { get; set; }
        public string LabelText { get; set; }
        public string LabelClassName { get; set; }
    }
}
