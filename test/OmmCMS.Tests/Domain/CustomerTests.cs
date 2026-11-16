using System.Linq;
using NUnit.Framework;
using OmmCMS.Core.Common;
using OmmCMS.Core.Identity;
using OmmCMS.Test.Common;

namespace OmmCMS.Tests.Domain;

[TestFixture]
public class CustomerTests
{
    [Test]
    public void New_customer_has_clear_password_type()
    {
        var customer = new Customer();
        customer.PasswordFormat.ShouldEqual(PasswordFormat.Clear);
    }

    [Test]
    public void Can_add_address()
    {
        var customer = new Customer();
        var address = new Address { Id = 1 };

        customer.Addresses.Add(address);

        customer.Addresses.Count.ShouldEqual(1);
        customer.Addresses.First().Id.ShouldEqual(1);
    }

    [Test]
    public void Can_remove_address_assigned_as_contact_address()
    {
        var customer = new Customer();
        var address = new Address { Id = 1 };

        customer.Addresses.Add(address);
        customer.ContactAddress = address;

        Assert.That(customer.ContactAddress, Is.SameAs(customer.Addresses.First()));

        customer.RemoveAddress(address);
        customer.Addresses.Count.ShouldEqual(0);
        customer.ContactAddress.ShouldBeNull();
    }
}
