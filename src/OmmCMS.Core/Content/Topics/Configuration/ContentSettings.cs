using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Content.Topics;

public class ContentSettings : ISettings
{
    public bool EnableHtmlTextCollapser { get; set; } = true;
    public int HtmlTextCollapsedHeight { get; set; } = 230;
    public List<string> FileUploadAllowedExtensions { get; set; } = [];
}
