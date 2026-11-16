using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Common.Configuration;

public class AdminAreaSettings : ISettings
{
    public int GridPageSize { get; set; } = 25;
}
