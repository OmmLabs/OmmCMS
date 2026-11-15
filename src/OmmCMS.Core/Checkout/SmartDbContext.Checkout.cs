
using OmmCMS.Core.Checkout.Orders;
using OmmCMS.Core.Checkout.Shipping;
using OmmCMS.Core.Checkout.Tax;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<TaxCategory> TaxCategories { get; set; }
}
