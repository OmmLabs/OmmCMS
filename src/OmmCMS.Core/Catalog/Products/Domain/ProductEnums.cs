namespace OmmCMS.Core.Catalog.Products;

/// <summary>
/// Represents the product type.
/// </summary>
public enum ProductType
{
    /// <summary>
    /// Simple product.
    /// </summary>
    SimpleProduct = 5,

    /// <summary>
    /// Grouped product.
    /// </summary>
    GroupedProduct = 10,

    /// <summary>
    /// Bundled product.
    /// </summary>
    BundledProduct = 15
}

/// <summary>
/// Gradually restricts the visibility of products.
/// </summary>
public enum ProductVisibility
{
    /// <summary>
    /// Product is fully visible.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Product is visible in search results.
    /// </summary>
    SearchResults = 10,

    /// <summary>
    /// Product is not visible in lists but clickable on product pages.
    /// </summary>
    ProductPage = 20,

    /// <summary>
    /// Product is not visible but appears on grouped product pages.
    /// </summary>
    Hidden = 30
}

/// <summary>
/// Represents a product condition. <see href="https://schema.org/OfferItemCondition"/>.
/// </summary>
public enum ProductCondition
{
    New = 0,
    Refurbished = 10,
    Used = 20,
    Damaged = 30
}

/// <summary>
/// Represents a quantity control type.
/// </summary>
public enum QuantityControlType
{
    Spinner = 0,
    Dropdown = 1
}

/// <summary>
/// Represents the behaviour when selecting product attributes.
/// </summary>
public enum AttributeChoiceBehaviour
{
    /// <summary>
    /// Gray out unavailable attributes.
    /// </summary>
    GrayOutUnavailable = 0,

    /// <summary>
    /// No particular behaviour.
    /// </summary>
    None = 30
}

/// <summary>
/// Represents the download activation type.
/// </summary>
public enum DownloadActivationType
{
    /// <summary>
    /// Activate when order is paid.
    /// </summary>
    WhenOrderIsPaid = 1,

    /// <summary>
    /// Activate manually.
    /// </summary>
    Manually = 10
}

/// <summary>
/// Represents the product sorting.
/// </summary>
public enum ProductSortingEnum
{
    /// <summary>
    /// Initial state
    /// </summary>
    Initial = 0,

    /// <summary>
    /// Relevance
    /// </summary>
    Relevance = 1,

    /// <summary>
    /// Name: A to Z
    /// </summary>
    NameAsc = 5,

    /// <summary>
    /// Name: Z to A
    /// </summary>
    NameDesc = 6,

    /// <summary>
    /// Price: Low to High
    /// </summary>
    PriceAsc = 10,

    /// <summary>
    /// Price: High to Low
    /// </summary>
    PriceDesc = 11,

    /// <summary>
    /// Product creation date.
    /// </summary>
    /// <remarks>Actually "CreatedOnDesc" (but due to localization this remains as is).</remarks>
    CreatedOn = 15,

    /// <summary>
    /// Product creation date
    /// </summary>
    CreatedOnAsc = 16
}

/// <summary>
/// This represents the legal information about a product.
/// </summary>
[Flags]
public enum ProductLegalInfo
{
    None = 0,

    /// <summary>
    /// Indicates whether prices include or exclude tax and, if applicable, the tax rate.
    /// </summary>
    Tax = 1 << 0,

    /// <summary>
    /// Indicates whether to display the shipping page link and, if applicable, a shipping surcharge.
    /// </summary>
    Shipping = 1 << 1,

    All = Tax | Shipping
}
