using OmmCMS.Events;

namespace OmmCMS.Core.Theming;

public class ThemeSwitchedEvent : IEventMessage
{
    public string OldTheme { get; init; }
    public string NewTheme { get; init; }
}