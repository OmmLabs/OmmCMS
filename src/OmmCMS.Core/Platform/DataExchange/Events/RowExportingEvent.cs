using OmmCMS.Core.Identity;
using OmmCMS.Events;

namespace OmmCMS.Core.DataExchange.Export.Events;

/// <summary>
/// An event that is fired before an entity is exported.
/// </summary>
public class RowExportingEvent : IEventMessage
{
    /// <summary>
    /// A dynamic object which wraps and extends the actual entity, e.g. <see cref="Customer"/>.
    /// </summary>
    public dynamic Row { get; init; }

    /// <summary>
    /// The entity type to be exported.
    /// </summary>
    public ExportEntityType EntityType { get; init; }

    /// <summary>
    /// Request of the export.
    /// </summary>
    public DataExportRequest ExportRequest { get; init; }

    /// <summary>
    /// Context of the export.
    /// </summary>
    public ExportExecuteContext ExecuteContext { get; init; }
}
