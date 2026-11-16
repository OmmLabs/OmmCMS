using System.Globalization;
using System.Text;
using System.Xml;
using OmmCMS.Core.Common;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Identity;

namespace OmmCMS.Core.DataExchange.Export;

/// <summary>
/// Writes XML formatted entity data using <see cref="XmlWriter"/>.
/// </summary>
/// <remarks>
/// Uses synchronous XmlWriter methods to avoid many atomic asynchronous write statements for each small text portion.
/// https://stackoverflow.com/questions/16641074/xmlwriter-async-methods/37391267
/// </remarks>
public class ExportXmlHelper : Disposable
{
    protected XmlWriter _writer;
    protected CultureInfo _culture;
    protected bool _doNotDispose;

    public ExportXmlHelper(XmlWriter writer, bool doNotDispose = false, CultureInfo culture = null)
    {
        _writer = writer;
        _doNotDispose = doNotDispose;
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    public ExportXmlHelper(Stream stream, XmlWriterSettings settings = null, CultureInfo culture = null)
    {
        _writer = XmlWriter.Create(stream, settings ?? DefaultSettings);
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    public static XmlWriterSettings DefaultSettings => new()
    {
        Encoding = Encoding.UTF8,
        CheckCharacters = false,
        Indent = false,
        NewLineHandling = NewLineHandling.None
    };

    public XmlWriter Writer => _writer;

    public void WriteLocalized(dynamic parentNode)
    {
        if (parentNode == null || parentNode._Localized == null)
            return;

        _writer.WriteStartElement("Localized");
        foreach (dynamic item in parentNode._Localized)
        {
            _writer.WriteStartElement((string)item.LocaleKey);
            _writer.WriteAttributeString("culture", (string)item.Culture);
            _writer.WriteString(((string)item.LocaleValue).RemoveInvalidXmlChars());
            _writer.WriteEndElement();
        }
        _writer.WriteEndElement();
    }

    public void WriteGenericAttributes(dynamic parentNode)
    {
        if (parentNode == null || parentNode._GenericAttributes == null)
            return;

        _writer.WriteStartElement("GenericAttributes");
        foreach (dynamic genericAttribute in parentNode._GenericAttributes)
        {
            GenericAttribute entity = genericAttribute.Entity;

            _writer.WriteStartElement("GenericAttribute");
            _writer.WriteElementString(nameof(GenericAttribute.Id), entity.Id.ToString());
            _writer.WriteElementString(nameof(GenericAttribute.EntityId), entity.EntityId.ToString());
            _writer.WriteElementString(nameof(GenericAttribute.KeyGroup), entity.KeyGroup);
            _writer.WriteElementString(nameof(GenericAttribute.Key), entity.Key);
            _writer.WriteElementString(nameof(GenericAttribute.Value), (string)genericAttribute.Value);
            _writer.WriteElementString(nameof(GenericAttribute.StoreId), entity.StoreId.ToString());
            _writer.WriteEndElement();
        }
        _writer.WriteEndElement();
    }

    public void WriteAddress(dynamic address, string node)
    {
        if (address == null)
            return;

        Address entity = address.Entity;

        if (node.HasValue())
        {
            _writer.WriteStartElement(node);
        }

        _writer.WriteElementString(nameof(Address.Id), entity.Id.ToString());
        _writer.WriteElementString(nameof(Address.Salutation), entity.Salutation);
        _writer.WriteElementString(nameof(Address.Title), entity.Title);
        _writer.WriteElementString(nameof(Address.FirstName), entity.FirstName);
        _writer.WriteElementString(nameof(Address.LastName), entity.LastName);
        _writer.WriteElementString(nameof(Address.Email), entity.Email);
        _writer.WriteElementString(nameof(Address.Company), entity.Company);
        _writer.WriteElementString(nameof(Address.CountryId), entity.CountryId?.ToString() ?? string.Empty);
        _writer.WriteElementString(nameof(Address.StateProvinceId), entity.StateProvinceId?.ToString() ?? string.Empty);
        _writer.WriteElementString(nameof(Address.City), entity.City);
        _writer.WriteElementString(nameof(Address.Address1), entity.Address1);
        _writer.WriteElementString(nameof(Address.Address2), entity.Address2);
        _writer.WriteElementString(nameof(Address.ZipPostalCode), entity.ZipPostalCode);
        _writer.WriteElementString(nameof(Address.PhoneNumber), entity.PhoneNumber);
        _writer.WriteElementString(nameof(Address.FaxNumber), entity.FaxNumber);
        _writer.WriteElementString(nameof(Address.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));

        if (address.Country != null)
        {
            dynamic country = address.Country;
            Country entityCountry = address.Country.Entity;

            _writer.WriteStartElement("Country");
            _writer.WriteElementString(nameof(Country.Id), entityCountry.Id.ToString());
            _writer.WriteElementString(nameof(Country.Name), (string)country.Name);
            _writer.WriteElementString(nameof(Country.TwoLetterIsoCode), entityCountry.TwoLetterIsoCode);
            _writer.WriteElementString(nameof(Country.ThreeLetterIsoCode), entityCountry.ThreeLetterIsoCode);
            _writer.WriteElementString(nameof(Country.NumericIsoCode), entityCountry.NumericIsoCode.ToString());
            _writer.WriteElementString(nameof(Country.Published), entityCountry.Published.ToString());
            _writer.WriteElementString(nameof(Country.DisplayOrder), entityCountry.DisplayOrder.ToString());
            _writer.WriteElementString(nameof(Country.LimitedToStores), entityCountry.LimitedToStores.ToString());

            WriteLocalized(country);
            _writer.WriteEndElement();
        }

        if (address.StateProvince != null)
        {
            dynamic stateProvince = address.StateProvince;
            StateProvince entityStateProvince = address.StateProvince.Entity;

            _writer.WriteStartElement("StateProvince");
            _writer.WriteElementString(nameof(StateProvince.Id), entityStateProvince.Id.ToString());
            _writer.WriteElementString(nameof(StateProvince.CountryId), entityStateProvince.CountryId.ToString());
            _writer.WriteElementString(nameof(StateProvince.Name), (string)stateProvince.Name);
            _writer.WriteElementString(nameof(StateProvince.Abbreviation), (string)stateProvince.Abbreviation);
            _writer.WriteElementString(nameof(StateProvince.Published), entityStateProvince.Published.ToString());
            _writer.WriteElementString(nameof(StateProvince.DisplayOrder), entityStateProvince.DisplayOrder.ToString());

            WriteLocalized(stateProvince);
            _writer.WriteEndElement();
        }

        if (node.HasValue())
        {
            _writer.WriteEndElement();
        }
    }

    public void WriteCurrency(dynamic currency, string node)
    {
        if (currency == null)
            return;

        Currency entity = currency.Entity;

        if (node.HasValue())
        {
            _writer.WriteStartElement(node);
        }

        _writer.WriteElementString(nameof(Currency.Id), entity.Id.ToString());
        _writer.WriteElementString(nameof(Currency.Name), (string)currency.Name);
        _writer.WriteElementString(nameof(Currency.CurrencyCode), entity.CurrencyCode);
        _writer.WriteElementString(nameof(Currency.Rate), entity.Rate.ToString(_culture));
        _writer.WriteElementString(nameof(Currency.DisplayLocale), entity.DisplayLocale);
        _writer.WriteElementString(nameof(Currency.CustomFormatting), entity.CustomFormatting);
        _writer.WriteElementString(nameof(Currency.LimitedToStores), entity.LimitedToStores.ToString());
        _writer.WriteElementString(nameof(Currency.Published), entity.Published.ToString());
        _writer.WriteElementString(nameof(Currency.DisplayOrder), entity.DisplayOrder.ToString());
        _writer.WriteElementString(nameof(Currency.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
        _writer.WriteElementString(nameof(Currency.UpdatedOnUtc), entity.UpdatedOnUtc.ToString(_culture));
        _writer.WriteElementString(nameof(Currency.DomainEndings), entity.DomainEndings);
        _writer.WriteElementString(nameof(Currency.RoundNumDecimals), entity.RoundNumDecimals.ToString());
        _writer.WriteElementString(nameof(Currency.MidpointRounding), ((int)entity.MidpointRounding).ToString());
        _writer.WriteElementString(nameof(Currency.RoundOrderItemsEnabled), entity.RoundOrderItemsEnabled.ToString());
        _writer.WriteElementString(nameof(Currency.RoundNetPrices), entity.RoundNetPrices.ToString());
        _writer.WriteElementString(nameof(Currency.RoundUnitPrices), entity.RoundUnitPrices.ToString());
        _writer.WriteElementString(nameof(Currency.RoundOrderTotalEnabled), entity.RoundOrderTotalEnabled.ToString());
        _writer.WriteElementString(nameof(Currency.RoundOrderTotalDenominator), entity.RoundOrderTotalDenominator.ToString(_culture));
        _writer.WriteElementString(nameof(Currency.RoundOrderTotalRule), ((int)entity.RoundOrderTotalRule).ToString());

        WriteLocalized(currency);

        if (node.HasValue())
        {
            _writer.WriteEndElement();
        }
    }

    public void WriteCountry(dynamic country, string node)
    {
        if (country == null)
            return;

        Country entity = country.Entity;

        if (node.HasValue())
        {
            _writer.WriteStartElement(node);
        }

        _writer.WriteElementString(nameof(Country.Id), entity.Id.ToString());
        _writer.WriteElementString(nameof(Country.Name), entity.Name);
        _writer.WriteElementString(nameof(Country.TwoLetterIsoCode), entity.TwoLetterIsoCode);
        _writer.WriteElementString(nameof(Country.ThreeLetterIsoCode), entity.ThreeLetterIsoCode);
        _writer.WriteElementString(nameof(Country.NumericIsoCode), entity.NumericIsoCode.ToString());
        _writer.WriteElementString(nameof(Country.Published), entity.Published.ToString());
        _writer.WriteElementString(nameof(Country.DisplayOrder), entity.DisplayOrder.ToString());
        _writer.WriteElementString(nameof(Country.LimitedToStores), entity.LimitedToStores.ToString());

        WriteLocalized(country);

        if (node.HasValue())
        {
            _writer.WriteEndElement();
        }
    }

    public void WriteMediaFile(dynamic file, string node)
    {
        if (file == null)
            return;

        MediaFile entity = file.Entity;

        if (node.HasValue())
        {
            _writer.WriteStartElement(node);
        }

        var seoName = (string)file.Name;
        seoName = Path.GetFileNameWithoutExtension(seoName.EmptyNull());

        _writer.WriteElementString(nameof(MediaFile.Id), entity.Id.ToString());
        _writer.WriteElementString("SeoFilename", seoName);
        _writer.WriteElementString(nameof(MediaFile.MimeType), (string)file.MimeType);
        _writer.WriteElementString("ThumbImageUrl", (string)file._ThumbImageUrl);
        _writer.WriteElementString("ImageUrl", (string)file._ImageUrl);
        _writer.WriteElementString("FullSizeImageUrl", (string)file._FullSizeImageUrl);
        _writer.WriteElementString("FileName", (string)file._FileName);

        if (node.HasValue())
        {
            _writer.WriteEndElement();
        }
    }

    public void WriteCustomer(dynamic customer, string node)
    {
        if (customer == null)
            return;

        Customer entity = customer.Entity;

        if (node.HasValue())
        {
            _writer.WriteStartElement(node);
        }

        _writer.WriteElementString(nameof(Customer.Id), entity.Id.ToString());
        _writer.WriteElementString(nameof(Customer.CustomerGuid), entity.CustomerGuid.ToString());
        _writer.WriteElementString(nameof(Customer.Username), entity.Username);
        _writer.WriteElementString(nameof(Customer.Email), entity.Email);
        _writer.WriteElementString(nameof(Customer.AdminComment), entity.AdminComment);
        _writer.WriteElementString(nameof(Customer.Active), entity.Active.ToString());
        _writer.WriteElementString(nameof(Customer.Deleted), entity.Deleted.ToString());
        _writer.WriteElementString(nameof(Customer.IsSystemAccount), entity.IsSystemAccount.ToString());
        _writer.WriteElementString(nameof(Customer.SystemName), entity.SystemName);
        _writer.WriteElementString(nameof(Customer.LastIpAddress), entity.LastIpAddress);
        _writer.WriteElementString(nameof(Customer.CreatedOnUtc), entity.CreatedOnUtc.ToString(_culture));
        _writer.WriteElementString(nameof(Customer.LastLoginDateUtc), entity.LastLoginDateUtc?.ToString(_culture) ?? string.Empty);
        _writer.WriteElementString(nameof(Customer.LastActivityDateUtc), entity.LastActivityDateUtc.ToString(_culture));
        _writer.WriteElementString(nameof(Customer.LimitedToStores), entity.LimitedToStores.ToString());
        _writer.WriteElementString(nameof(Customer.Salutation), entity.Salutation);
        _writer.WriteElementString(nameof(Customer.Title), entity.Title);
        _writer.WriteElementString(nameof(Customer.FirstName), entity.FirstName);
        _writer.WriteElementString(nameof(Customer.LastName), entity.LastName);
        _writer.WriteElementString(nameof(Customer.FullName), entity.FullName);
        _writer.WriteElementString(nameof(Customer.Company), entity.Company);
        _writer.WriteElementString(nameof(Customer.CustomerNumber), entity.CustomerNumber);
        _writer.WriteElementString(nameof(Customer.BirthDate), entity.BirthDate?.ToString(_culture) ?? string.Empty);

        if (customer.CustomerRoles != null)
        {
            _writer.WriteStartElement("CustomerRoles");
            foreach (dynamic role in customer.CustomerRoles)
            {
                CustomerRole entityRole = role.Entity;

                _writer.WriteStartElement("CustomerRole");
                _writer.WriteElementString(nameof(CustomerRole.Id), entityRole.Id.ToString());
                _writer.WriteElementString(nameof(CustomerRole.Name), (string)role.Name);
                _writer.WriteElementString(nameof(CustomerRole.Active), entityRole.Active.ToString());
                _writer.WriteElementString(nameof(CustomerRole.IsSystemRole), entityRole.IsSystemRole.ToString());
                _writer.WriteElementString(nameof(CustomerRole.SystemName), entityRole.SystemName);
                _writer.WriteEndElement();
            }
            _writer.WriteEndElement();
        }

        WriteAddress(customer.ContactAddress, nameof(Customer.ContactAddress));

        if (customer.Addresses != null)
        {
            _writer.WriteStartElement("Addresses");
            foreach (dynamic address in customer.Addresses)
            {
                WriteAddress(address, "Address");
            }
            _writer.WriteEndElement();
        }

        WriteGenericAttributes(customer);

        if (node.HasValue())
        {
            _writer.WriteEndElement();
        }
    }


    protected override void OnDispose(bool disposing)
    {
        if (_writer != null && !_doNotDispose)
        {
            _writer.Dispose();
        }
    }

    protected override async ValueTask OnDisposeAsync(bool disposing)
    {
        if (_writer != null && !_doNotDispose)
        {
            await _writer.DisposeAsync();
        }
    }
}
