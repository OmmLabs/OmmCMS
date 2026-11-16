using System.Text.Json;
using OmmCMS.Core.Common;

namespace OmmCMS.Core.Identity;

/// <summary>
/// A collection of customer-related data that is stored in addition to the properties of the <see cref="Customer"/> entity.
/// </summary>
public class CustomerAttributeCollection : GenericAttributeCollection<Customer>
{
    public CustomerAttributeCollection(GenericAttributeCollection collection)
        : base(collection)
    {
    }

    #region Form fields

    // INFO: this address data is for information purposes only (e.g. on the customer profile page).
    // The customer's address book is maintained separately.

    public string StreetAddress
    {
        get => Get<string>(SystemCustomerAttributeNames.StreetAddress);
        set => Set(SystemCustomerAttributeNames.StreetAddress, value);
    }

    public string StreetAddress2
    {
        get => Get<string>(SystemCustomerAttributeNames.StreetAddress2);
        set => Set(SystemCustomerAttributeNames.StreetAddress2, value);
    }

    public string ZipPostalCode
    {
        get => Get<string>(SystemCustomerAttributeNames.ZipPostalCode);
        set => Set(SystemCustomerAttributeNames.ZipPostalCode, value);
    }

    public string City
    {
        get => Get<string>(SystemCustomerAttributeNames.City);
        set => Set(SystemCustomerAttributeNames.City, value);
    }

    public int? StateProvinceId
    {
        get => Get<int?>(SystemCustomerAttributeNames.StateProvinceId);
        set => Set(SystemCustomerAttributeNames.StateProvinceId, value);
    }

    public int? CountryId
    {
        get => Get<int?>(SystemCustomerAttributeNames.CountryId);
        set => Set(SystemCustomerAttributeNames.CountryId, value);
    }

    public string Phone
    {
        get => Get<string>(SystemCustomerAttributeNames.Phone);
        set => Set(SystemCustomerAttributeNames.Phone, value);
    }

    public string Fax
    {
        get => Get<string>(SystemCustomerAttributeNames.Fax);
        set => Set(SystemCustomerAttributeNames.Fax, value);
    }

    #endregion

    #region Other attributes


    /// <summary>
    /// Gets or sets the file IDcolor of the customer's avatar picture.
    /// </summary>
    public int? AvatarPictureId
    {
        get => Get<int?>(SystemCustomerAttributeNames.AvatarPictureId);
        set => Set(SystemCustomerAttributeNames.AvatarPictureId, value);
    }

    /// <summary>
    /// Gets or sets the color of the customer's avatar if no picture has been uploaded.
    /// </summary>
    /// <example>light</example>
    public string AvatarColor
    {
        get => Get<string>(SystemCustomerAttributeNames.AvatarColor);
        set => Set(SystemCustomerAttributeNames.AvatarColor, value);
    }

    /// <summary>
    /// Gets or sets the token to recover  the account password.
    /// </summary>
    public string PasswordRecoveryToken
    {
        get => Get<string>(SystemCustomerAttributeNames.PasswordRecoveryToken);
        set => Set(SystemCustomerAttributeNames.PasswordRecoveryToken, value);
    }

    /// <summary>
    /// Gets or sets the token to confirm the account registration via e-mail.
    /// </summary>
    public string AccountActivationToken
    {
        get => Get<string>(SystemCustomerAttributeNames.AccountActivationToken);
        set => Set(SystemCustomerAttributeNames.AccountActivationToken, value);
    }

    /// <summary>
    /// Gets or sets the ID of the customer currently impersonated by the admin.
    /// </summary>
    public int? ImpersonatedCustomerId
    {
        get => Get<int?>(SystemCustomerAttributeNames.ImpersonatedCustomerId);
        set => Set(SystemCustomerAttributeNames.ImpersonatedCustomerId, value);
    }

    /// <summary>
    /// Gets or sets the ID of the store whose settings are currently loaded and edited in the backend.
    /// </summary>
    public int AdminAreaStoreScopeConfiguration
    {
        get => Get<int>(SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration);
        set => Set(SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration, value);
    }

    /// <summary>
    /// Gets or sets the categories most recently used by an admin in the backend.
    /// </summary>
    public string MostRecentlyUsedCategories
    {
        get => Get<string>(SystemCustomerAttributeNames.MostRecentlyUsedCategories);
        set => Set(SystemCustomerAttributeNames.MostRecentlyUsedCategories, value);
    }

    /// <summary>
    /// Gets or sets the manufacturers most recently used by an admin in the backend.
    /// </summary>
    public string MostRecentlyUsedManufacturers
    {
        get => Get<string>(SystemCustomerAttributeNames.MostRecentlyUsedManufacturers);
        set => Set(SystemCustomerAttributeNames.MostRecentlyUsedManufacturers, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the customer has consented to the General Data Protection Regulation (GDPR).
    /// </summary>
    public bool HasConsentedToGdpr
    {
        get => Get<bool>(SystemCustomerAttributeNames.HasConsentedToGdpr);
        set => Set(SystemCustomerAttributeNames.HasConsentedToGdpr, value);
    }

    /// <summary>
    /// Gets or sets the customer's consent to the use of cookies.
    /// </summary>
    public ConsentCookie CookieConsent
    {
        get
        {
            var json = Get<string>(SystemCustomerAttributeNames.CookieConsent);
            return json.HasValue() ? JsonSerializer.Deserialize<ConsentCookie>(json) : null;
        }
        set
        {
            var json = value != null ? JsonSerializer.Serialize(value) : null;
            Set(SystemCustomerAttributeNames.CookieConsent, json);
        }
    }

    /// <summary>
    /// Gets or sets the ID of the selected currency.
    /// </summary>
    public int? CurrencyId
    {
        get => Get<int?>(SystemCustomerAttributeNames.CurrencyId, CurrentStoreId);
        set => Set(SystemCustomerAttributeNames.CurrencyId, value, CurrentStoreId);
    }

    /// <summary>
    /// Gets or sets the ID of the selected language.
    /// </summary>
    public int? LanguageId
    {
        get => Get<int?>(SystemCustomerAttributeNames.LanguageId, CurrentStoreId);
        set => Set(SystemCustomerAttributeNames.LanguageId, value, CurrentStoreId);
    }

    /// <summary>
    /// Gets or sets the URL to continue shopping.
    /// </summary>
    public string LastContinueShoppingPage
    {
        get => Get<string>(SystemCustomerAttributeNames.LastContinueShoppingPage, CurrentStoreId);
        set => Set(SystemCustomerAttributeNames.LastContinueShoppingPage, value, CurrentStoreId);
    }

    /// <summary>
    /// Gets or sets the name of the theme chosen by customer.
    /// </summary>
    public string WorkingThemeName
    {
        get => Get<string>(SystemCustomerAttributeNames.WorkingThemeName, CurrentStoreId);
        set => Set(SystemCustomerAttributeNames.WorkingThemeName, value, CurrentStoreId);
    }

    #endregion

    #region Addresses

    /// <summary>
    /// Gets or sets the ID of the customer's preferred address for the current site.
    /// </summary>
    public int? DefaultAddressId
    {
        get => Get<int?>(SystemCustomerAttributeNames.DefaultAddressId, CurrentStoreId);
        set => Set(SystemCustomerAttributeNames.DefaultAddressId, value, CurrentStoreId);
    }

    #endregion
}
