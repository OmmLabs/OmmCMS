using System.Text.Json;
using System.Text.Json.Serialization;
using OmmCMS.Core.AI.Metadata;

namespace OmmCMS.Core.AI.JsonConverters;

public sealed class AIOutputTypeConverter : JsonStringEnumConverter<AIOutputType>
{
    public AIOutputTypeConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
