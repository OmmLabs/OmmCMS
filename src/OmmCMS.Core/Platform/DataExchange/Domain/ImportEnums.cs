namespace OmmCMS.Core.DataExchange;

/// <summary>
/// Supported entity types
/// </summary>
public enum ImportEntityType
{
    Customer = 2,
    NewsletterSubscription = 3
}

public enum ImportFileType
{
    Csv = 0,
    Xlsx
}

[Flags]
public enum ImportModeFlags
{
    Insert = 1,
    Update = 2
}
