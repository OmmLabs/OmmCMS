using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using OmmCMS.Core.Common;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Rules;
using OmmCMS.Core.Rules.Filters;
using OmmCMS.Core.Stores;

namespace OmmCMS.Core.Tests.Rules.Filters;

[TestFixture]
public abstract class FilterTestsBase
{
    protected sealed class FilterCustomer : Customer
    {
        public List<FilterOrder> Orders { get; set; } = new();
    }

    protected enum OrderStatus { Pending = 10, Processing = 20, Complete = 30, Cancelled = 40 }

    protected sealed class FilterOrder
    {
        public int StoreId { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public int OrderStatusId => (int)OrderStatus;
        public string PaymentMethodSystemName { get; set; }
        public string ShippingMethod { get; set; }
        public decimal OrderTotal { get; set; }
        public List<FilterOrderItem> OrderItems { get; set; } = new();
    }

    protected sealed class FilterOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceInclTax { get; set; }
    }

    private List<FilterCustomer> _customers;

    private CustomerRole _role1 = new() { Id = 1, Active = true };
    private CustomerRole _role2 = new() { Id = 2, Active = true };
    private CustomerRole _role3 = new() { Id = 3, Active = false };
    private CustomerRole _role4 = new() { Id = 4, Active = true };

    private Store _store1 = new() { Id = 1 };
    private Store _store2 = new() { Id = 2 };
    private Store _store3 = new() { Id = 3 };

    private string _pay1 = "Payment1";
    private string _pay2 = "Payment2";
    private string _pay3 = "Payment3";

    private string _ship1 = "Ship1";
    private string _ship2 = "Ship2";
    private string _ship3 = "Ship3";


    protected static class FilterDescriptors
    {
        public static FilterDescriptor Active = new FilterDescriptor<FilterCustomer, bool>(x => x.Active)
        {
            RuleType = RuleType.Boolean,
            Name = "Active"
        };
        public static FilterDescriptor BillingCountry = new FilterDescriptor<FilterCustomer, int?>(x => x.BillingAddress.CountryId)
        {
            RuleType = RuleType.NullableInt,
            Name = "BillingCountry"
        };
        public static FilterDescriptor ShippingCountry = new FilterDescriptor<FilterCustomer, int?>(x => x.ShippingAddress.CountryId)
        {
            RuleType = RuleType.NullableInt,
            Name = "ShippingCountry"
        };
        public static FilterDescriptor LastActivityDays = new FilterDescriptor<FilterCustomer, double>(x => (DateTime.UtcNow - x.LastActivityDateUtc).TotalDays /* DbFunctions.DiffDays(x.LastActivityDateUtc, DateTime.UtcNow)*/)
        {
            RuleType = RuleType.Int,
            Name = "LastActivityDays"
        };
        public static FilterDescriptor CompletedOrderCount = new FilterDescriptor<FilterCustomer, int>(x => x.Orders.Count(y => y.OrderStatusId == 30))
        {
            RuleType = RuleType.Int,
            Name = "CompletedOrderCount"
        };
        public static FilterDescriptor CancelledOrderCount = new FilterDescriptor<FilterCustomer, int>(x => x.Orders.Count(y => y.OrderStatusId == 40))
        {
            RuleType = RuleType.Int,
            Name = "CancelledOrderCount"
        };
        public static FilterDescriptor NewOrderCount = new FilterDescriptor<FilterCustomer, int>(x => x.Orders.Count(y => y.OrderStatusId >= 20))
        {
            RuleType = RuleType.Int,
            Name = "NewOrderCount"
        };
        public static FilterDescriptor Age = new FilterDescriptor<FilterCustomer, double>(x => DateTime.Now.Year - x.BirthDate.Value.Year)
        {
            RuleType = RuleType.Int,
            Name = "Age"
        };
        public static FilterDescriptor IsInAnyRole = new AnyFilterDescriptor<FilterCustomer, CustomerRoleMapping, int>(x => x.CustomerRoleMappings.Where(y => y.CustomerRole.Active), r => r.CustomerRole.Id)
        {
            RuleType = RuleType.IntArray,
            Name = "IsInAnyRole"
        };
        public static FilterDescriptor HasAllRoles = new AllFilterDescriptor<FilterCustomer, CustomerRoleMapping, int>(x => x.CustomerRoleMappings.Where(y => y.CustomerRole.Active), r => r.CustomerRole.Id)
        {
            RuleType = RuleType.IntArray,
            Name = "HasAllRoles"
        };
        public static FilterDescriptor HasPurchasedProduct = new AnyFilterDescriptor<FilterCustomer, FilterOrderItem, int>(x => x.Orders.SelectMany(o => o.OrderItems), oi => oi.ProductId)
        {
            RuleType = RuleType.IntArray,
            Name = "HasPurchasedProduct"
        };
        // TODO
        public static FilterDescriptor AverageOrderItemPrice = new FilterDescriptor<FilterCustomer, decimal>(x => x.Orders.SelectMany(o => o.OrderItems.Where(oi => true)).Average(y => y.PriceInclTax))
        {
            RuleType = RuleType.Money,
            Name = "AverageOrderItemPrice"
        };
        public static FilterDescriptor TotalPurchasedUnits = new FilterDescriptor<FilterCustomer, int>(x => x.Orders.SelectMany(o => o.OrderItems.Where(oi => true)).Sum(y => y.Quantity))
        {
            RuleType = RuleType.Int,
            Name = "TotalPurchasedUnits"
        };
    }

    protected List<FilterCustomer> Customers => _customers;

    protected void AssertEquality(List<FilterCustomer> expected, List<FilterCustomer> actual)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count), "ResultCount");
        //Console.WriteLine("FilterCustomer filter result - expected: {0}".FormatInvariant(expected.Count));

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.That(actual[i].Id, Is.EqualTo(expected[i].Id), "Item " + i);
        }
    }

    [OneTimeSetUp]
    public virtual void SetUp()
    {
        SetUpEntities();
    }

    protected virtual void SetUpEntities()
    {
        _customers = new List<FilterCustomer>();

        var customer1 = new FilterCustomer
        {
            Id = 1,
            Username = "sally",
            BillingAddress = new Address { CountryId = 1 },
            ShippingAddress = new Address { CountryId = 1 },
            BirthDate = new DateTime(1980, 1, 1),
            Active = false,
            LastActivityDateUtc = DateTime.Now.AddDays(-5),
            Orders = new List<FilterOrder>
            {
                new FilterOrder
                {
                    StoreId = _store1.Id,
                    OrderStatus = OrderStatus.Complete,
                    PaymentMethodSystemName = _pay1,
                    ShippingMethod = _ship1,
                    OrderTotal = 200,
                    OrderItems = new List<FilterOrderItem>
                    {
                        new FilterOrderItem { ProductId = 1, Quantity = 1, PriceInclTax = 10 },
                        new FilterOrderItem { ProductId = 2, Quantity = 1, PriceInclTax = 50 },
                        new FilterOrderItem { ProductId = 3, Quantity = 2, PriceInclTax = 100 },
                        new FilterOrderItem { ProductId = 4, Quantity = 1, PriceInclTax = 40 }
                    }
                }
            }
        };
        customer1.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 1, CustomerRoleId = _role1.Id, CustomerRole = _role1 });
        customer1.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 1, CustomerRoleId = _role2.Id, CustomerRole = _role2 });
        _customers.Add(customer1);

        var customer2 = new FilterCustomer
        {
            Id = 2,
            Username = "john",
            BillingAddress = new Address { CountryId = 2 },
            ShippingAddress = new Address { CountryId = 1 },
            BirthDate = new DateTime(1999, 12, 24),
            Active = true,
            LastActivityDateUtc = DateTime.Now.AddDays(-1),
            Orders = new List<FilterOrder>
            {
                new FilterOrder
                {
                    StoreId = _store2.Id,
                    OrderStatus = OrderStatus.Processing,
                    PaymentMethodSystemName = _pay2,
                    ShippingMethod = _ship2,
                    OrderTotal = 999,
                    OrderItems = new List<FilterOrderItem>
                    {
                        new FilterOrderItem { ProductId = 1, Quantity = 3, PriceInclTax = 100 },
                        new FilterOrderItem { ProductId = 2, Quantity = 1, PriceInclTax = 50 },
                        new FilterOrderItem { ProductId = 5, Quantity = 1, PriceInclTax = 150 },
                        new FilterOrderItem { ProductId = 6, Quantity = 1, PriceInclTax = 10 },
                        new FilterOrderItem { ProductId = 7, Quantity = 2, PriceInclTax = 300 },
                        new FilterOrderItem { ProductId = 8, Quantity = 4, PriceInclTax = 300 }
                    }
                }
            }
        };
        customer2.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 2, CustomerRoleId = _role2.Id, CustomerRole = _role2 });
        customer2.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 2, CustomerRoleId = _role3.Id, CustomerRole = _role3 });
        customer2.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 2, CustomerRoleId = _role4.Id, CustomerRole = _role4 });
        _customers.Add(customer2);

        var customer3 = new FilterCustomer
        {
            Id = 3,
            Username = "edward",
            BillingAddress = new Address { CountryId = 3 },
            ShippingAddress = new Address { CountryId = 3 },
            BirthDate = new DateTime(1960, 10, 10),
            Active = false,
            LastActivityDateUtc = DateTime.Now.AddDays(-100),
            Orders = new List<FilterOrder>
            {
                new FilterOrder
                {
                    StoreId = _store1.Id,
                    OrderStatus = OrderStatus.Cancelled,
                    PaymentMethodSystemName = _pay3,
                    ShippingMethod = _ship3,
                    OrderTotal = 50,
                    OrderItems = new List<FilterOrderItem>
                    {
                        new FilterOrderItem { ProductId = 2, Quantity = 1, PriceInclTax = 10 },
                        new FilterOrderItem { ProductId = 3, Quantity = 1, PriceInclTax = 20 },
                        new FilterOrderItem { ProductId = 5, Quantity = 1, PriceInclTax = 5 },
                        new FilterOrderItem { ProductId = 8, Quantity = 1, PriceInclTax = 5 },
                        new FilterOrderItem { ProductId = 9, Quantity = 1, PriceInclTax = 10 }
                    }
                }
            }
        };
        customer3.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 3, CustomerRoleId = _role1.Id, CustomerRole = _role1 });
        customer3.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 3, CustomerRoleId = _role4.Id, CustomerRole = _role4 });
        _customers.Add(customer3);

        var customer4 = new FilterCustomer
        {
            Id = 4,
            Username = "snowden",
            BillingAddress = new Address { CountryId = 1 },
            ShippingAddress = new Address { CountryId = 1 },
            BirthDate = DateTime.Now.AddYears(-32),
            Active = true,
            LastActivityDateUtc = DateTime.Now.AddDays(0),
            Orders = new List<FilterOrder>
            {
                new FilterOrder
                {
                    StoreId = _store2.Id,
                    OrderStatus = OrderStatus.Complete,
                    PaymentMethodSystemName = _pay2,
                    ShippingMethod = _ship1,
                    OrderTotal = 500,
                    OrderItems = new List<FilterOrderItem>
                    {
                        new FilterOrderItem { ProductId = 1, Quantity = 1, PriceInclTax = 250 },
                        new FilterOrderItem { ProductId = 5, Quantity = 1, PriceInclTax = 250 }
                    }
                }
            }
        };
        customer4.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 4, CustomerRoleId = _role2.Id, CustomerRole = _role2 });
        customer4.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 4, CustomerRoleId = _role3.Id, CustomerRole = _role3 });
        _customers.Add(customer4);

        var customer5 = new FilterCustomer
        {
            Id = 5,
            BillingAddress = new Address { CountryId = 2 },
            ShippingAddress = new Address { CountryId = 2 },
            LastActivityDateUtc = DateTime.Now.AddDays(-2),
            Orders = new List<FilterOrder>
            {
                new FilterOrder
                {
                    OrderItems = new List<FilterOrderItem>()
                }
            }
        };
        customer5.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 5, CustomerRoleId = _role1.Id, CustomerRole = _role1 });
        _customers.Add(customer5);

        var customer6 = new FilterCustomer
        {
            Id = 6,
            BillingAddress = new Address { CountryId = 1 },
            ShippingAddress = new Address { CountryId = 1 },
            LastActivityDateUtc = DateTime.Now.AddDays(-10),
            Orders = new List<FilterOrder>
            {
                new FilterOrder
                {
                    OrderItems = new List<FilterOrderItem>()
                }
            }
        };
        customer6.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = 6, CustomerRoleId = _role2.Id, CustomerRole = _role2 });
        _customers.Add(customer6);
    }

    protected virtual List<FilterCustomer> ExecuteQuery<TValue>(RuleOperator op, Expression<Func<FilterCustomer, TValue>> left, object right)
    {
        var predicate = ExpressionHelper.CreateLambdaExpression(left, op, right);
        var result = _customers.AsQueryable().Where(predicate).Cast<FilterCustomer>().ToList();
        return result;
    }

    protected virtual List<FilterCustomer> ExecuteQuery(LogicalRuleOperator logicalOperator, params FilterExpression[] expressions)
    {
        var compositeFilter = new FilterExpressionGroup(typeof(FilterCustomer)) { LogicalOperator = logicalOperator };
        compositeFilter.AddExpressions(expressions);

        var predicate = compositeFilter.ToPredicate(_customers.AsQueryable().Provider);
        var result = _customers.AsQueryable().Where(predicate).Cast<FilterCustomer>().ToList();

        return result;
    }
}
