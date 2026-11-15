using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmmCMS.Core.Localization;
using OmmCMS.Data.Caching;

namespace OmmCMS.Core.Common;

internal class MeasureWeightMap : IEntityTypeConfiguration<MeasureWeight>
{
    public void Configure(EntityTypeBuilder<MeasureWeight> builder)
    {
        builder.Property(c => c.Ratio).HasPrecision(18, 8);
    }
}

/// <summary>
/// Represents a measure weight
/// </summary>
[CacheableEntity]
public partial class MeasureWeight : EntityWithAttributes, ILocalizedEntity, IDisplayOrder
{
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    [LocalizedProperty]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the system keyword
    /// </summary>
    public string SystemKeyword { get; set; }

    /// <summary>
    /// Gets or sets the ratio
    /// </summary>        
    public decimal Ratio { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}