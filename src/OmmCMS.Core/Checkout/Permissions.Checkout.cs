namespace OmmCMS.Core.Security;

public static partial class Permissions
{
    public static class Order
    {
        public const string Self = "order";
        public const string Read = "order.read";
        public const string Update = "order.update";
        public const string Create = "order.create";
        public const string Delete = "order.delete";
        public const string EditItem = "order.editorderitem";

    }

    public static partial class Promotion
    {
        public const string Self = "promotion";

    }

    public static partial class Configuration
    {
        public static class Tax
        {
            public const string Self = "configuration.tax";
            public const string Read = "configuration.tax.read";
            public const string Update = "configuration.tax.update";
            public const string Create = "configuration.tax.create";
            public const string Delete = "configuration.tax.delete";
            public const string Activate = "configuration.tax.activate";
        }
    }

    public static class Cart
    {
        public const string Self = "cart";
        public const string Read = "cart.read";
        public const string AccessShoppingCart = "cart.accessshoppingcart";
        public const string AccessWishlist = "cart.accesswishlist";

        public static class CheckoutAttribute
        {
            public const string Self = "cart.checkoutattribute";
            public const string Read = "cart.checkoutattribute.read";
            public const string Update = "cart.checkoutattribute.update";
            public const string Create = "cart.checkoutattribute.create";
            public const string Delete = "cart.checkoutattribute.delete";
        }
    }
}
