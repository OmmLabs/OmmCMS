namespace OmmCMS.Core.DataExchange.Import.Internal;

internal class DataImporterContext
{
    public DataImportRequest Request { get; init; }
    public CancellationToken CancelToken { get; init; }
    public ILogger Log { get; set; }
    public ImportExecuteContext ExecuteContext { get; init; }
    public ColumnMap ColumnMap { get; init; }
}
