using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Common.Configuration;

public class MeasureSettings : ISettings
{
    public int BaseDimensionId { get; set; }
    public int BaseWeightId { get; set; }
}