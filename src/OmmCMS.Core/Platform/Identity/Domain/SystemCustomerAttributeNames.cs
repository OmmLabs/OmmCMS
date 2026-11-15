namespace OmmCMS.Core.Identity;

public static partial class SystemCustomerAttributeNames
{
    // Form fields
    public static string StreetAddress => "StreetAddress";
    public static string StreetAddress2 => "StreetAddress2";
    public static string ZipPostalCode => "ZipPostalCode";
    public static string City => "City";
    public static string StateProvinceId => "StateProvinceId";
    public static string CountryId => "CountryId";
    public static string Phone => "Phone";
    public static string Fax => "Fax";

    // Other attributes
    public static string AvatarPictureId => "AvatarPictureId";
    public static string AvatarColor => "AvatarColor";
    public static string PasswordRecoveryToken => "PasswordRecoveryToken";
    public static string AccountActivationToken => "AccountActivationToken";
    public static string ImpersonatedCustomerId => "ImpersonatedCustomerId";
    public static string AdminAreaStoreScopeConfiguration => "AdminAreaStoreScopeConfiguration";
    public static string MostRecentlyUsedCategories => "MostRecentlyUsedCategories";
    public static string MostRecentlyUsedManufacturers => "MostRecentlyUsedManufacturers";
    public static string HasConsentedToGdpr => "HasConsentedToGdpr";
    public static string CookieConsent => "CookieConsent";
    public static string HasEarnedNewsletterRewardPoints => "HasEarnedNewsletterRewardPoints";

    // Checkout
    public static string DefaultBillingAddressId => "DefaultBillingAddressId";
    public static string DefaultShippingAddressId => "DefaultShippingAddressId";


    // Depends on store
    public static string CurrencyId => "CurrencyId";
    public static string LanguageId => "LanguageId";
    public static string LastContinueShoppingPage => "LastContinueShoppingPage";
    public static string WorkingThemeName => "WorkingThemeName";
}
