namespace OmmCMS.Core.Web;

/// <summary>
/// User Agent types
/// </summary>
public enum UserAgentType : byte
{
    /// <summary>
    /// Unkown / not mapped
    /// </summary>
    Unknown,
    /// <summary>
    /// Browser, e.g. "Chrome", "Firefox" etc.
    /// </summary>
    Browser,
    /// <summary>
    /// Bot, Crawler, Spider, Tool
    /// </summary>
    Bot,
    /// <summary>
    /// OmmCMS
    /// </summary>
    Application
}