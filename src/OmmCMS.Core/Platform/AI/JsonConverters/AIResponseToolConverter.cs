using OmmCMS.Core.AI.Metadata;
using OmmCMS.Json.Converters;

namespace OmmCMS.Core.AI.JsonConverters;

public sealed class AIResponseToolConverter : ArrayEnumFlagConverter<AIResponseTool>
{
    protected override IReadOnlyDictionary<string, AIResponseTool> GetMapping()
    {
        return new Dictionary<string, AIResponseTool>(StringComparer.OrdinalIgnoreCase)
        {
            ["WebSearch"] = AIResponseTool.WebSearch,
            ["ImageGeneration"] = AIResponseTool.ImageGeneration,
            ["CodeAnalysis"] = AIResponseTool.CodeAnalysis,
            ["FileSearch"] = AIResponseTool.FileSearch
        };
    }
}