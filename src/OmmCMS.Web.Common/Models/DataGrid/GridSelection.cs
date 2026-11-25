using System.Text.Json.Serialization;

namespace OmmCMS.Web.Models.DataGrid;

public class GridSelection
{
    [JsonPropertyName("selectedKeys")]
    public string[] SelectedKeys { get; set; }

    public IEnumerable<int> GetEntityIds()
    {
        return (SelectedKeys ?? Array.Empty<string>())
            .Select(x => x.ToInt())
            .Where(x => x > 0)
            .Distinct();
    }
}