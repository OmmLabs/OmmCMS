using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmmCMS.Core.Rules;
using OmmCMS.Core.Security;

namespace OmmCMS.Core.Identity;

internal class CustomerRoleMap : IEntityTypeConfiguration<CustomerRole>
{
    public void Configure(EntityTypeBuilder<CustomerRole> builder)
    {

        builder.HasMany(c => c.RuleSets)
            .WithMany(c => c.CustomerRoles)
            .UsingEntity<Dictionary<string, object>>(
                "RuleSet_CustomerRole_Mapping",
                c => c
                    .HasOne<RuleSetEntity>()
                    .WithMany()
                    .HasForeignKey("RuleSetEntity_Id")
                    .HasConstraintName("FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id")
                    .OnDelete(DeleteBehavior.Cascade),
                c => c
                    .HasOne<CustomerRole>()
                    .WithMany()
                    .HasForeignKey("CustomerRole_Id")
                    .HasConstraintName("FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRole_Id")
                    .OnDelete(DeleteBehavior.Cascade),
                c =>
                {
                    c.HasIndex("CustomerRole_Id");
                    c.HasKey("CustomerRole_Id", "RuleSetEntity_Id");
                });
    }
}

/// <summary>
/// Represents a customer role.
/// </summary>
[Index(nameof(Active), Name = "IX_Active")]
[Index(nameof(IsSystemRole), Name = "IX_IsSystemRole")]
[Index(nameof(SystemName), Name = "IX_SystemName")]
[Index(nameof(SystemName), nameof(IsSystemRole), Name = "IX_CustomerRole_SystemName_IsSystemRole")]
public partial class CustomerRole : EntityWithAttributes, IRulesContainer
{
    /// <summary>
    /// Gets or sets the customer role name.
    /// </summary>
    [Required, StringLength(255)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer role is active.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer role is system.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Gets or sets the customer role system name.
    /// </summary>
    [StringLength(255)]
    public string SystemName { get; set; }

    private ICollection<PermissionRoleMapping> _permissionRoleMappings;
    /// <summary>
    /// Gets or sets permission role mappings.
    /// </summary>
    [IgnoreDataMember]
    public ICollection<PermissionRoleMapping> PermissionRoleMappings
    {
        get => _permissionRoleMappings ?? LazyLoader.Load(this, ref _permissionRoleMappings) ?? (_permissionRoleMappings ??= new HashSet<PermissionRoleMapping>());
        protected set => _permissionRoleMappings = value;
    }

    private ICollection<RuleSetEntity> _ruleSets;
    /// <summary>
    /// Gets or sets assigned rule sets.
    /// </summary>
    [IgnoreDataMember]
    public ICollection<RuleSetEntity> RuleSets
    {
        get => _ruleSets ?? LazyLoader.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
        protected set => _ruleSets = value;
    }
}