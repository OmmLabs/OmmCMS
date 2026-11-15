
using OmmCMS.Core.Checkout.Attributes;
using OmmCMS.Core.Checkout.Cart;
using OmmCMS.Core.Checkout.Orders;
using OmmCMS.Core.Checkout.Shipping;
using OmmCMS.Core.Checkout.Tax;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderNote> OrderNotes { get; set; }
    public DbSet<TaxCategory> TaxCategories { get; set; }
    public DbSet<CheckoutAttribute> CheckoutAttributes { get; set; }
    public DbSet<CheckoutAttributeValue> CheckoutAttributeValues { get; set; }
    public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentItem> ShipmentItems { get; set; }
    public DbSet<ReturnCase> ReturnCases { get; set; }
}
