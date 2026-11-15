using OmmCMS.Core.Catalog;
using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Services;

namespace OmmCMS;

public static class IDeliveryTimeServiceExtensions
{
    /// <summary>
    /// Gets the product delivery time according to stock.
    /// </summary>
    public static Task<DeliveryTime> GetDeliveryTimeAsync(this IDeliveryTimeService service, Product product, CatalogSettings catalogSettings)
    {
        var deliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(catalogSettings);
        return service.GetDeliveryTimeAsync(deliveryTimeId, false);
    }
}