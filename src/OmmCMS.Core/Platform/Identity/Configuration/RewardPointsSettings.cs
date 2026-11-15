using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Identity;

public class RewardPointsSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether Reward Points Program is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value of Reward Points exchange rate.
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value whether to round down reward points.
    /// </summary>
    public bool RoundDownRewardPoints { get; set; }

    /// <summary>
    /// Gets or sets a number of points awarded for registration.
    /// </summary>
    public int PointsForRegistration { get; set; }

    /// <summary>
    /// Gets or sets the number of reward points awarded for subscribing to the newsletter.
    /// </summary>
    public int PointsForNewsletterSubscription { get; set; }
}
