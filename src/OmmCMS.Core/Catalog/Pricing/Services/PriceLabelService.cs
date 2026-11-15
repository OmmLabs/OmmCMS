using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Data;

namespace OmmCMS.Core.Catalog.Pricing;

public class PriceLabelService : IPriceLabelService
{
    private readonly SmartDbContext _db;
    private readonly PriceSettings _priceSettings;

    private Dictionary<int, PriceLabel> _allPriceLabels;
    private PriceLabel _defaultComparePriceLabel;
    private PriceLabel _defaultRegularPriceLabel;

    public PriceLabelService(SmartDbContext db, PriceSettings priceSettings)
    {
        _db = db;
        _priceSettings = priceSettings;
    }


    public virtual PriceLabel GetDefaultComparePriceLabel()
    {
        return _defaultComparePriceLabel ??= GetPriceLabel(_priceSettings.DefaultComparePriceLabelId, true);
    }

    public virtual PriceLabel GetDefaultRegularPriceLabel()
    {
        return _defaultRegularPriceLabel ??= GetPriceLabel(_priceSettings.DefaultRegularPriceLabelId, false);
    }

    private PriceLabel GetPriceLabel(int? id, bool forComparePrice)
    {
        var allLabels = GetAllPriceLabels();

        if (id > 0 && allLabels.TryGetValue(id.Value, out var label))
        {
            return label;
        }

        label = forComparePrice
            ? allLabels.Values.FirstOrDefault(x => x.IsRetailPrice)
            : allLabels.Values.FirstOrDefault(x => !x.IsRetailPrice);

        return label ?? allLabels.Values.FirstOrDefault();
    }

    private Dictionary<int, PriceLabel> GetAllPriceLabels()
    {
        if (_allPriceLabels == null)
        {
            _allPriceLabels = _db.PriceLabels
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToDictionary(x => x.Id);
        }

        return _allPriceLabels;
    }

    public virtual PriceLabel GetComparePriceLabel(Product product)
    {
        Guard.NotNull(product);

        var labelId = product.ComparePriceLabelId.GetValueOrDefault();
        if (labelId == 0)
        {
            return GetDefaultComparePriceLabel();
        }

        if (_db.IsReferenceLoaded(product, x => x.ComparePriceLabel, out var entry))
        {
            return entry.CurrentValue;
        }

        return GetPriceLabel(labelId, true);
    }

    public virtual PriceLabel GetRegularPriceLabel(Product product)
    {
        return GetDefaultRegularPriceLabel();
    }

}
