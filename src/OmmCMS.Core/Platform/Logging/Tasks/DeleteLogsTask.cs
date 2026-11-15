using OmmCMS.Core.Common.Configuration;
using OmmCMS.Scheduling;

namespace OmmCMS.Core.Logging.Tasks;

/// <summary>
/// A task that periodically deletes log entries.
/// </summary>
public partial class DeleteLogsTask : ITask
{
    private readonly IDbLogService _dbLogService;
    private readonly CommonSettings _commonSettings;

    public DeleteLogsTask(IDbLogService dbLogService, CommonSettings commonSettings)
    {
        _dbLogService = dbLogService;
        _commonSettings = commonSettings;
    }

    public Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
    {
        var maxAge = DateTime.UtcNow.AddDays(-_commonSettings.MaxLogAgeInDays);

        return _dbLogService.ClearLogsAsync(maxAge, _commonSettings.MinLogLevelToRetain, cancelToken);
    }
}