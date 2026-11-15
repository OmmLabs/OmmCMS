using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;
using OmmCMS.Data.Hooks;

namespace OmmCMS.Core.Common.Hooks;

internal class MeasureDimensionHook : AsyncDbSaveHook<MeasureDimension>
{
    private readonly MeasureSettings _measureSettings;
    private string _hookErrorMessage;

    public MeasureDimensionHook(MeasureSettings measureSettings)
    {
        _measureSettings = measureSettings;
    }

    public Localizer T { get; set; } = NullLocalizer.Instance;

    protected override Task<HookResult> OnDeletingAsync(MeasureDimension entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.Id == _measureSettings.BaseDimensionId)
        {
            entry.ResetState();
            _hookErrorMessage = T("Admin.Configuration.Measures.Dimensions.CantDeletePrimary");
        }

        return Task.FromResult(HookResult.Ok);
    }

    public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        if (_hookErrorMessage.HasValue())
        {
            var message = new string(_hookErrorMessage);
            _hookErrorMessage = null;

            throw new HookException(message);
        }

        return Task.CompletedTask;
    }
}