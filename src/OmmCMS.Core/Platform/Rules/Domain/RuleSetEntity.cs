using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OmmCMS.Core.Identity;
using OmmCMS.Data.Caching;

namespace OmmCMS.Core.Rules;

/// <summary>
/// Represents a set of rules.
/// </summary>
[Table("RuleSet")]
[Index(nameof(IsSubGroup), Name = "IX_IsSubGroup")]
[Index(nameof(IsActive), nameof(Scope), Name = "IX_RuleSetEntity_Scope")]
[CacheableEntity(MaxRows = 1, Expiry = 480)] // MaxRows = 1 caches only ById calls
public partial class RuleSetEntity : EntityWithAttributes, IAuditable
{
    /// <summary>
    /// Gets or sets the name of the rule set.
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [StringLength(400)]
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the rule set is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the scope of the rule set.
    /// </summary>
    [Required]
    public RuleScope Scope { get; set; } = RuleScope.Customer;

    /// <summary>
    /// <c>True</c> when this set is an internal composite container for rules within another rule set.
    /// </summary>
    public bool IsSubGroup { get; set; }

    /// <summary>
    /// Gets or sets the logical operator for the rules in this set.
    /// </summary>
    public LogicalRuleOperator LogicalOperator { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedOnUtc { get; set; }

    /// <inheritdoc/>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date when the set was processed.
    /// </summary>
    public DateTime? LastProcessedOnUtc { get; set; }

    private ICollection<RuleEntity> _rules;
    /// <summary>
    /// Gets rules
    /// </summary>
    public ICollection<RuleEntity> Rules
    {
        get => _rules ?? LazyLoader.Load(this, ref _rules) ?? (_rules ??= new HashSet<RuleEntity>());
        protected set => _rules = value;
    }

    private ICollection<CustomerRole> _customerRoles;
    /// <summary>
    /// Gets or sets assigned customer roles.
    /// </summary>
    public ICollection<CustomerRole> CustomerRoles
    {
        get => _customerRoles ?? LazyLoader.Load(this, ref _customerRoles) ?? (_customerRoles ??= new HashSet<CustomerRole>());
        protected set => _customerRoles = value;
    }


}
