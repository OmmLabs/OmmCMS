using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Checkout.Shipping;

public class ShippingSettings : ISettings
{
    /// <summary>
    /// Specifies a custom formatting pattern for delivery time dates (e.g. "M").
    /// </summary>
    public string DeliveryTimesDateFormat { get; set; }

    /// <summary>
    /// Specifies the hour (value between 1 and 24) by which the order will be shipped the same day.
    /// </summary>
    public int? TodayShipmentHour { get; set; } = 14;

    /// <summary>
    /// Specifies whether delivery takes place only on workweek days.
    /// </summary>
    public bool DeliveryOnWorkweekDaysOnly { get; set; } = true;
}
