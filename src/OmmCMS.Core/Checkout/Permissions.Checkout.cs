namespace OmmCMS.Core.Security;

public static partial class Permissions
{
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

}