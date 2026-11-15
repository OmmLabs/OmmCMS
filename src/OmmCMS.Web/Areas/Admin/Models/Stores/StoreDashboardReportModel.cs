namespace OmmCMS.Admin.Models.Store;

public class StoreDashboardReportModel : ModelBase
{
    public string TopicsCount { get; set; }
    public string MenusCount { get; set; }
    public string MediaCount { get; set; }
    public string MediaSize { get; set; }
    public string CustomersCount { get; set; }
    public string OnlineCustomersCount { get; set; }
}