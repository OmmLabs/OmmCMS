namespace OmmCMS.Core.DataExchange;

/// <summary>
/// Represents export entity types.
/// </summary>
public enum ExportEntityType
{
    Customer = 3,
    NewsletterSubscription = 5
}

/// <summary>
/// Represents export deployment types.
/// </summary>
public enum ExportDeploymentType
{
    FileSystem = 0,
    Email,
    Http,
    Ftp,
    PublicFolder
}

/// <summary>
/// Represents export HTTP transmission types.
/// </summary>
public enum ExportHttpTransmissionType
{
    SimplePost = 0,
    MultipartFormDataPost
}

/// <summary>
/// Controls data processing and projection items supported by an export provider.
/// </summary>
[Flags]
public enum ExportFeatures
{
    None = 0,

    /// <summary>
    /// A value indicating whether to automatically create a file based public deployment when an export profile is created.
    /// </summary>
    CreatesInitialPublicDeployment = 1,

    /// <summary>
    /// A value indicating whether to not automatically send a completion email.
    /// </summary>
    CanOmitCompletionMail = 1 << 10
}
