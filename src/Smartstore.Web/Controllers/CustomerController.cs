using System.Linq.Expressions;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Customers;
using Smartstore.Admin.Models.Orders;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Utilities;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Controllers;

public class CustomerController : PublicController
{
    private readonly SmartDbContext _db;
    private readonly INewsletterSubscriptionService _newsletterSubscriptionService;
    private readonly ITaxService _taxService;
    private readonly IMediaService _mediaService;
    private readonly IMessageFactory _messageFactory;
    private readonly UserManager<Customer> _userManager;
    private readonly SignInManager<Customer> _signInManager;
    private readonly IProviderManager _providerManager;
    private readonly ModuleManager _moduleManager;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly DateTimeSettings _dateTimeSettings;
    private readonly CustomerSettings _customerSettings;
    private readonly TaxSettings _taxSettings;
    private readonly LocalizationSettings _localizationSettings;

    public CustomerController(
        SmartDbContext db,
        INewsletterSubscriptionService newsletterSubscriptionService,
        ITaxService taxService,
        IMediaService mediaService,
        IMessageFactory messageFactory,
        UserManager<Customer> userManager,
        SignInManager<Customer> signInManager,
        IProviderManager providerManager,
        ModuleManager moduleManager,
        IDateTimeHelper dateTimeHelper,
        DateTimeSettings dateTimeSettings,
        CustomerSettings customerSettings,
        TaxSettings taxSettings,
        LocalizationSettings localizationSettings)
    {
        _db = db;
        _newsletterSubscriptionService = newsletterSubscriptionService;
        _taxService = taxService;
        _mediaService = mediaService;
        _messageFactory = messageFactory;
        _userManager = userManager;
        _signInManager = signInManager;
        _providerManager = providerManager;
        _moduleManager = moduleManager;
        _dateTimeHelper = dateTimeHelper;
        _dateTimeSettings = dateTimeSettings;
        _customerSettings = customerSettings;
        _taxSettings = taxSettings;
        _localizationSettings = localizationSettings;
    }

    public async Task<IActionResult> Info()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        var model = new CustomerInfoModel();
        await PrepareCustomerInfoModelAsync(model, customer, false);

        return View(model);
    }

    [HttpPost]
    [SaveChanges<SmartDbContext>(false)]
    public async Task<IActionResult> Info(CustomerInfoModel model)
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        if (model.Email.IsEmpty())
        {
            ModelState.AddModelError(string.Empty, T("Account.Register.Errors.EmailIsNotProvided"));
        }
        if (_customerSettings.CanEditUsernames() && model.Username.IsEmpty())
        {
            ModelState.AddModelError(string.Empty, T("Account.Register.Errors.UsernameIsNotProvided"));
        }

        // INFO: update email and username requires SaveChangesAttribute to be set to 'false'.
        var newEmail = model.Email.TrimSafe();
        var newUsername = model.Username.TrimSafe();

        // Email.
        if (ModelState.IsValid && !newEmail.Equals(customer.Email, StringComparison.InvariantCultureIgnoreCase))
        {
            var token = await _userManager.GenerateChangeEmailTokenAsync(customer, newEmail);
            var result = await _userManager.ChangeEmailAsync(customer, newEmail, token);
            if (result.Succeeded)
            {
                // Re-authenticate (if usernames are disabled).
                if (_customerSettings.CustomerLoginType == CustomerLoginType.Email)
                {
                    await _signInManager.SignInAsync(customer, true);
                }
            }
            else
            {
                result.Errors
                    .Select(x => x.Description)
                    .Distinct()
                    .Each(x => ModelState.AddModelError(nameof(model.Email), x));
            }
        }

        // Username.
        if (ModelState.IsValid
            && _customerSettings.CanEditUsernames()
            && !newUsername.EqualsNoCase(customer.Username))
        {
            var result = await _userManager.SetUserNameAsync(customer, newUsername);
            if (result.Succeeded)
            {
                // Re-authenticate.
                await _signInManager.SignInAsync(customer, true);
            }
            else
            {
                result.Errors
                    .Select(x => x.Description)
                    .Distinct()
                    .Each(x => ModelState.AddModelError(nameof(model.Username), x));
            }
        }

        try
        {
            if (ModelState.IsValid)
            {
                customer.FirstName = model.FirstName;
                customer.LastName = model.LastName;

                // VAT number.
                if (_taxSettings.EuVatEnabled)
                {
                    var prevVatNumber = customer.GenericAttributes.VatNumber;
                    customer.GenericAttributes.VatNumber = model.VatNumber;

                    if (prevVatNumber != model.VatNumber)
                    {
                        var vatCheckResult = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                        customer.VatNumberStatusId = (int)vatCheckResult.Status;

                        // Send VAT number admin notification.
                        if (model.VatNumber.HasValue() && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                        {
                            await _messageFactory.SendNewVatSubmittedStoreOwnerNotificationAsync(customer, model.VatNumber, vatCheckResult.Address, _localizationSettings.DefaultAdminLanguageId);
                        }
                    }
                }

                // Customer number.
                if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
                {
                    customer.CustomerNumber = model.CustomerNumber;
                }

                if (_customerSettings.DateOfBirthEnabled)
                {
                    customer.BirthDate = model.DateOfBirth;
                }

                if (_customerSettings.CompanyEnabled)
                {
                    customer.Company = model.Company;
                }
                if (_customerSettings.TitleEnabled)
                {
                    customer.Title = model.Title;
                }
                if (_customerSettings.GenderEnabled)
                {
                    customer.Gender = model.Gender;
                }
                if (_customerSettings.StreetAddressEnabled)
                {
                    customer.GenericAttributes.StreetAddress = model.StreetAddress;
                }
                if (_customerSettings.StreetAddress2Enabled)
                {
                    customer.GenericAttributes.StreetAddress2 = model.StreetAddress2;
                }
                if (_customerSettings.ZipPostalCodeEnabled)
                {
                    customer.GenericAttributes.ZipPostalCode = model.ZipPostalCode;
                }
                if (_customerSettings.CityEnabled)
                {
                    customer.GenericAttributes.City = model.City;
                }
                if (_customerSettings.CountryEnabled)
                {
                    customer.GenericAttributes.CountryId = model.CountryId;
                }
                if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                {
                    customer.GenericAttributes.StateProvinceId = model.StateProvinceId;
                }
                if (_customerSettings.PhoneEnabled)
                {
                    customer.GenericAttributes.Phone = model.Phone;
                }
                if (_customerSettings.FaxEnabled)
                {
                    customer.GenericAttributes.Fax = model.Fax;
                }
                if (_customerSettings.NewsletterEnabled)
                {
                    if (model.Newsletter)
                    {
                        await _newsletterSubscriptionService.SubscribeAsync(customer.Email, customer);
                    }
                    else
                    {
                        await _newsletterSubscriptionService.UnsubscribeAsync(customer.Email);
                    }
                }
                if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                {
                    customer.TimeZoneId = model.TimeZoneId;
                }

                var updateResult = await _userManager.UpdateAsync(customer);
                if (updateResult.Succeeded)
                {
                    await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, customer, Request.Form, Services.StoreContext.CurrentStoreIdIfMultiStoreMode));
                    return RedirectToAction(nameof(Info));
                }
                else
                {
                    updateResult.Errors
                        .Select(x => x.Description)
                        .Distinct()
                        .Each(x => ModelState.AddModelError(string.Empty, x));
                }
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        await PrepareCustomerInfoModelAsync(model, customer, false);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CheckUsernameAvailability(string username)
    {
        username = username.TrimSafe();

        var usernameAvailable = false;
        string statusText = null;

        if (_customerSettings.SupportsUsernames() && username.HasValue())
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (customer != null && customer.Username != null && customer.Username.EqualsNoCase(username))
            {
                statusText = T("Account.CheckUsernameAvailability.CurrentUsername");
            }
            else
            {
                var userExists = await _db.Customers
                    .IgnoreQueryFilters()
                    .ApplyIdentFilter(null, username, null, true)
                    .AnyAsync();

                if (!userExists)
                {
                    statusText = T("Account.CheckUsernameAvailability.Available");
                    usernameAvailable = true;
                }
            }
        }

        return Json(new
        {
            Available = usernameAvailable,
            Text = statusText.NullEmpty() ?? T("Account.CheckUsernameAvailability.NotAvailable")
        });
    }

    #region Addresses

    public async Task<IActionResult> Addresses()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        var models = await customer.Addresses.MapAsync(customer, false);

        return View(models);
    }

    public async Task<IActionResult> AddressDelete(int id)
    {
        if (id < 1)
        {
            return NotFound();
        }

        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        // Find address and ensure that it belongs to the current customer.
        var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
        if (address != null)
        {
            customer.RemoveAddress(address);
            // Now delete the address record.
            _db.Addresses.Remove(address);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Addresses));
    }

    public async Task<IActionResult> AddressAdd()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        var model = await new Address().MapAsync(customer, false);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddressAdd(AddressModel model)
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        var address = new Address();

        if (ModelState.IsValid)
        {
            await model.MapAsync(address, customer);
            customer.Addresses.Add(address);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Addresses));
        }

        await address.MapAsync(model, customer, false);

        return View(model);
    }

    public async Task<IActionResult> AddressEdit(int id)
    {
        if (id < 1)
        {
            return NotFound();
        }

        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        // Find address and ensure that it belongs to the current customer.
        var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
        if (address == null)
        {
            return RedirectToAction(nameof(Addresses));
        }

        var model = await address.MapAsync(customer, false);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddressEdit(AddressModel model, int id)
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        // Find address and ensure that it belongs to the current customer.
        var address = customer.Addresses.FirstOrDefault(x => x.Id == id);
        if (address == null)
        {
            return RedirectToAction(nameof(Addresses));
        }

        if (ModelState.IsValid)
        {
            await model.MapAsync(address, customer, false);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Addresses));
        }

        await address.MapAsync(model, customer, false);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        var address = customer.Addresses.FirstOrDefault(x => x.Id == id);
        if (address != null)
        {
            var billingAllowed = address.Country?.AllowsBilling ?? true;
            var shippingAllowed = address.Country?.AllowsShipping ?? true;

            if (billingAllowed && shippingAllowed)
            {
                customer.GenericAttributes.DefaultBillingAddressId = address.Id;
                customer.GenericAttributes.DefaultShippingAddressId = address.Id;
                await _db.SaveChangesAsync();
            }
            else
            {
                if (!billingAllowed)
                {
                    NotifyError(T("Order.CountryNotAllowedForBilling", address.Country?.GetLocalized(x => x.Name)));
                }
                if (!shippingAllowed)
                {
                    NotifyError(T("Order.CountryNotAllowedForShipping", address.Country?.GetLocalized(x => x.Name)));
                }
            }
        }

        return RedirectToAction(nameof(Addresses));
    }

    #endregion

    #region Avatar

    public async Task<IActionResult> Avatar()
    {
        if (!_customerSettings.AllowCustomersToUploadAvatars)
        {
            return RedirectToAction(nameof(Info));
        }

        var customer = Services.WorkContext.CurrentCustomer;
        if (!customer.IsRegistered())
        {
            return ChallengeOrForbid();
        }

        var model = new CustomerAvatarEditModel
        {
            Avatar = await customer.MapAsync(null, true, true),
            MaxFileSizeValue = _customerSettings.MaxAvatarFileSize,
            MaxFileSize = _customerSettings.MaxAvatarFileSize.Kilobytes().Humanize()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UploadAvatar()
    {
        long maxFileSize = _customerSettings.MaxAvatarFileSize * 1024;
        var customer = Services.WorkContext.CurrentCustomer;
        var success = false;
        string avatar = null;

        if (customer.IsRegistered() && _customerSettings.AllowCustomersToUploadAvatars)
        {
            var uploadedFile = Request.Form.Files[0];
            if (uploadedFile != null && uploadedFile.FileName.HasValue())
            {
                if (uploadedFile.Length <= maxFileSize)
                {
                    var oldAvatar = await _db.MediaFiles.FindByIdAsync(customer.GenericAttributes.AvatarPictureId ?? 0);
                    if (oldAvatar != null)
                    {
                        await _mediaService.DeleteFileAsync(oldAvatar, true);
                    }

                    var path = _mediaService.CombinePaths(SystemAlbumProvider.Customers, PathUtility.SanitizeFileName(uploadedFile.FileName));

                    using var stream = uploadedFile.OpenReadStream();
                    var newAvatar = await _mediaService.SaveFileAsync(path, stream, false, DuplicateFileHandling.Rename);

                    if (newAvatar != null)
                    {
                        customer.GenericAttributes.AvatarPictureId = newAvatar.Id;
                        await _db.SaveChangesAsync();

                        var model = await customer.MapAsync(null, true, true);
                        avatar = await InvokePartialViewAsync("Customer.Avatar", model);
                        success = true;

                        NotifyInfo(T("Account.Avatar.AvatarChanged"));
                    }
                }
                else
                {
                    NotifyError(T("Account.Avatar.MaximumUploadedFileSize", _customerSettings.MaxAvatarFileSize.Kilobytes().Humanize()));
                }
            }
        }

        return Json(new { success, avatar });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAvatar()
    {
        string avatar = null;
        var customer = Services.WorkContext.CurrentCustomer;
        if (customer.IsRegistered() && _customerSettings.AllowCustomersToUploadAvatars)
        {
            var file = await _db.MediaFiles.FindByIdAsync((int)customer.GenericAttributes.AvatarPictureId);
            if (file != null)
            {
                await _mediaService.DeleteFileAsync(file, true);
            }

            customer.GenericAttributes.AvatarPictureId = 0;
            customer.GenericAttributes.AvatarColor = null;

            await _db.SaveChangesAsync();

            var model = await customer.MapAsync(null, true, true);
            avatar = await InvokePartialViewAsync("Customer.Avatar", model);

            NotifyInfo(T("Account.Avatar.AvatarRemoved"));
        }

        return Json(new { success = true, avatar });
    }

    #endregion

    #region Utilities

    private async Task PrepareCustomerInfoModelAsync(CustomerInfoModel model, Customer customer, bool excludeProperties)
    {
        Guard.NotNull(model);
        Guard.NotNull(customer);

        var store = Services.StoreContext.CurrentStore;
        var language = Services.WorkContext.WorkingLanguage;

        model.Id = customer.Id;
        model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;

        ViewBag.AvailableTimeZones = _dateTimeHelper.GetSystemTimeZones()
            .ToSelectListItems(excludeProperties ? model.TimeZoneId : _dateTimeHelper.CurrentTimeZone.Id);

        if (!excludeProperties)
        {
            var newsletterSubscription = await _db.NewsletterSubscriptions
                .AsNoTracking()
                .ApplyMailAddressFilter(customer.Email, store.Id)
                .FirstOrDefaultAsync();

            model.Company = customer.Company;
            model.Title = customer.Title;
            model.FirstName = customer.FirstName;
            model.LastName = customer.LastName;
            model.Gender = customer.Gender;
            model.CustomerNumber = customer.CustomerNumber;
            model.Email = customer.Email;
            model.Username = customer.Username;
            model.DateOfBirth = customer.BirthDate;
            model.VatNumber = customer.GenericAttributes.VatNumber;
            model.StreetAddress = customer.GenericAttributes.StreetAddress;
            model.StreetAddress2 = customer.GenericAttributes.StreetAddress2;
            model.City = customer.GenericAttributes.City;
            model.ZipPostalCode = customer.GenericAttributes.ZipPostalCode;
            model.CountryId = Convert.ToInt32(customer.GenericAttributes.CountryId);
            model.StateProvinceId = Convert.ToInt32(customer.GenericAttributes.StateProvinceId);
            model.Phone = customer.GenericAttributes.Phone;
            model.Fax = customer.GenericAttributes.Fax;
            model.Newsletter = newsletterSubscription != null && newsletterSubscription.Active;
        }
        else
        {
            if (_customerSettings.SupportsUsernames() && !_customerSettings.CanEditUsernames())
            {
                model.Username = customer.Username;
            }
        }

        // Countries and state provinces.
        if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
        {
            var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync(model.CountryId);
            ViewBag.AvailableStates = stateProvinces.ToSelectListItems(model.StateProvinceId) ??
            [
                new() { Text = T("Address.OtherNonUS"), Value = "0" }
            ];
        }

        model.FirstNameRequired = _customerSettings.FirstNameRequired;
        model.LastNameRequired = _customerSettings.LastNameRequired;
        model.DisplayVatNumber = _taxSettings.EuVatEnabled;
        model.VatNumberStatusNote = ((VatNumberStatus)customer.VatNumberStatusId).GetLocalizedEnum(language.Id);
        model.GenderEnabled = _customerSettings.GenderEnabled;
        model.TitleEnabled = _customerSettings.TitleEnabled;
        model.DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled;
        model.CompanyEnabled = _customerSettings.CompanyEnabled;
        model.CompanyRequired = _customerSettings.CompanyRequired;
        model.StreetAddressEnabled = _customerSettings.StreetAddressEnabled;
        model.StreetAddressRequired = _customerSettings.StreetAddressRequired;
        model.StreetAddress2Enabled = _customerSettings.StreetAddress2Enabled;
        model.StreetAddress2Required = _customerSettings.StreetAddress2Required;
        model.ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled;
        model.ZipPostalCodeRequired = _customerSettings.ZipPostalCodeRequired;
        model.CityEnabled = _customerSettings.CityEnabled;
        model.CityRequired = _customerSettings.CityRequired;
        model.CountryEnabled = _customerSettings.CountryEnabled;
        model.StateProvinceEnabled = _customerSettings.StateProvinceEnabled;
        model.PhoneEnabled = _customerSettings.PhoneEnabled;
        model.PhoneRequired = _customerSettings.PhoneRequired;
        model.FaxEnabled = _customerSettings.FaxEnabled;
        model.FaxRequired = _customerSettings.FaxRequired;
        model.NewsletterEnabled = _customerSettings.NewsletterEnabled;
        model.UsernamesEnabled = _customerSettings.SupportsUsernames();
        model.AllowUsersToChangeUsernames = _customerSettings.CanEditUsernames();
        model.CheckUsernameAvailabilityEnabled = _customerSettings.CheckUsernameAvailabilityEnabled;
        model.DisplayCustomerNumber = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled
            && _customerSettings.CustomerNumberVisibility != CustomerNumberVisibility.None;

        model.CustomerNumberEnabled = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled
            && (_customerSettings.CustomerNumberVisibility == CustomerNumberVisibility.Editable
            || (_customerSettings.CustomerNumberVisibility == CustomerNumberVisibility.EditableIfEmpty && model.CustomerNumber.IsEmpty()));

        // External authentication.
        await _db.LoadCollectionAsync(customer, x => x.ExternalAuthenticationRecords);

        var authSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        var authProviders = _providerManager.GetAllProviders<IExternalAuthenticationMethod>()
            .ToDictionarySafe(x => x.Metadata.SystemName, x => x, StringComparer.OrdinalIgnoreCase);

        model.AssociatedExternalAuthRecords = customer.ExternalAuthenticationRecords
            .Select(x =>
            {
                var methodName = authProviders.TryGetValue(x.ProviderSystemName, out var provider)
                    ? _moduleManager.GetLocalizedFriendlyName(provider.Metadata).NullEmpty() ?? provider.Metadata.FriendlyName.NullEmpty()
                    : null;

                if (methodName == null)
                {
                    // Method has a system name mapping.
                    var authScheme = authSchemes.FirstOrDefault(scheme => x.ProviderSystemName.Contains(scheme.Name, StringComparison.OrdinalIgnoreCase));
                    methodName = authScheme?.Name;
                }

                return new CustomerInfoModel.AssociatedExternalAuthModel
                {
                    Id = x.Id,
                    Email = x.Email,
                    ExternalIdentifier = x.ExternalIdentifier,
                    AuthMethodName = methodName ?? x.ProviderSystemName
                };
            })
            .ToList();

    }

    #endregion
}