
using System.Text.Json.Serialization;
using OmmCMS.Core.Rules;
using OmmCMS.Core.Rules.Rendering;

namespace OmmCMS.Admin.Models.Rules;

public class RuleCommand
{
    [JsonPropertyName("scope")]
    public RuleScope Scope { get; set; }

    /// <summary>
    /// Identifier of the related entity.
    /// </summary>
    [JsonPropertyName("entityId")]
    public int? EntityId { get; set; }

    [JsonPropertyName("ruleSetId")]
    public int RuleSetId { get; set; }

    [JsonPropertyName("ruleId")]
    public int RuleId { get; set; }

    [JsonPropertyName("ruleType")]
    public string RuleType { get; set; }

    [JsonPropertyName("op")]
    public string Op { get; set; }

    [JsonPropertyName("ruleData")]
    public RuleEditItem[] RuleData { get; set; }
}
