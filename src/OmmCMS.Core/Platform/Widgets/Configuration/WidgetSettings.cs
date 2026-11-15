using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Widgets;

public class WidgetSettings : ISettings
{
    /// <summary>
    /// Gets or sets a system names of active widgets.
    /// </summary>
    public List<string> ActiveWidgetSystemNames { get; set; } = new();
}