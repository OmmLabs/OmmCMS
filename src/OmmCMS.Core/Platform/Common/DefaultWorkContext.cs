using Microsoft.AspNetCore.Http;
using OmmCMS.Core.Common;
using OmmCMS.Core.Data;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Stores;

namespace OmmCMS.Core;

public partial class DefaultWorkContext : IWorkContext
{
    private readonly SmartDbContext _db;
    private readonly IWorkContextSource _source;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStoreContext _storeContext;


    private Customer _customer;
    private Language _language;
    private Currency _currency;
    private Customer _impersonator;
    private bool? _isAdminArea;

    public DefaultWorkContext(
        SmartDbContext db,
        IWorkContextSource source,
        IHttpContextAccessor httpContextAccessor,
        IStoreContext storeContext)
    {
        _db = db;
        _source = source;
        _httpContextAccessor = httpContextAccessor;
        _storeContext = storeContext;
    }

    public async Task InitializeAsync()
    {
        if (_customer == null)
        {
            (_customer, _impersonator) = await _source.ResolveCurrentCustomerAsync();
        }

        if (_language == null)
        {
            _language = await _source.ResolveWorkingLanguageAsync(_customer);
        }

        if (_currency == null)
        {
            _currency = await _source.ResolveWorkingCurrencyAsync(_customer, IsAdminArea);
        }

    }

    public bool IsInitialized
    {
        get => _customer != null && _language != null && _currency != null;
    }

    public Customer CurrentCustomer
    {
        get
        {
            if (_customer == null)
            {
                InitializeAsync().Await();
            }

            return _customer;
        }
        set
        {
            if (value != _customer)
            {
                _customer = value;

                // Reset all properties that depend on "_customer".
                _language = null;
                _currency = null;
            }
        }
    }

    public Customer CurrentImpersonator => _impersonator;

    public Language WorkingLanguage
    {
        get
        {
            if (_language == null)
            {
                InitializeAsync().Await();
            }

            return _language;
        }
        set
        {
            if (value?.Id != _language?.Id)
            {
                _source.SaveCustomerAttribute(CurrentCustomer, SystemCustomerAttributeNames.LanguageId, value?.Id, false).Await();
                _language = value;
            }
        }
    }

    public Currency WorkingCurrency
    {
        get
        {
            if (_currency == null)
            {
                InitializeAsync().Await();
            }

            return _currency;
        }
        set
        {
            if (value?.Id != _currency?.Id)
            {
                _source.SaveCustomerAttribute(CurrentCustomer, SystemCustomerAttributeNames.CurrencyId, value?.Id, false).Await();
                _currency = value;
            }
        }
    }

    public bool IsAdminArea
    {
        get
        {
            if (_isAdminArea.HasValue)
            {
                return _isAdminArea.Value;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            _isAdminArea = httpContext?.Request?.IsAdminArea() == true;

            return _isAdminArea.Value;
        }
        set => _isAdminArea = value;
    }
}