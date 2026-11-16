using System.IO.Compression;
using System.Net.Http;
using System.Security;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using OmmCMS.Collections;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Content.Media.Storage;
using OmmCMS.Core.Data;
using OmmCMS.Core.DataExchange.Export.Deployment;
using OmmCMS.Core.DataExchange.Export.Internal;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Logging;
using OmmCMS.Core.Messaging;
using OmmCMS.Core.Security;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Stores;
using OmmCMS.Data;
using OmmCMS.Data.Caching;
using OmmCMS.Engine.Modularity;
using OmmCMS.IO;
using OmmCMS.Net.Mail;
using OmmCMS.Threading;
using OmmCMS.Utilities;

namespace OmmCMS.Core.DataExchange.Export;

public partial class DataExporter : IDataExporter
{
    private readonly SmartDbContext _db;
    private readonly ICommonServices _services;
    private readonly IWorkContext _workContext;
    private readonly IExportProfileService _exportProfileService;
    private readonly IMediaService _mediaService;
    private readonly ILanguageService _languageService;
    private readonly ILocalizedEntityService _localizedEntityService;
    private readonly IMailService _mailService;
    private readonly IQueuedEmailService _queuedEmailService;
    private readonly Lazy<IUrlHelper> _urlHelper;
    private readonly IProviderManager _providerManager;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly MediaSettings _mediaSettings;
    private readonly CustomerSettings _customerSettings;
    private readonly ContactDataSettings _contactDataSettings;

    public DataExporter(
        SmartDbContext db,
        ICommonServices services,
        IWorkContext workContext,
        IExportProfileService exportProfileService,
        IMediaService mediaService,
        ILanguageService languageService,
        ILocalizedEntityService localizedEntityService,
        IMailService mailService,
        IQueuedEmailService queuedEmailService,
        Lazy<IUrlHelper> urlHelper,
        IProviderManager providerManager,
        IHttpClientFactory httpClientFactory,
        MediaSettings mediaSettings,
        CustomerSettings customerSettings,
        ContactDataSettings contactDataSettings)
    {
        _db = db;
        _services = services;
        _workContext = workContext;
        _exportProfileService = exportProfileService;
        _mediaService = mediaService;
        _languageService = languageService;
        _localizedEntityService = localizedEntityService;
        _mailService = mailService;
        _queuedEmailService = queuedEmailService;
        _urlHelper = urlHelper;
        _providerManager = providerManager;
        _httpClientFactory = httpClientFactory;

        _mediaSettings = mediaSettings;
        _customerSettings = customerSettings;
        _contactDataSettings = contactDataSettings;
    }

    public Localizer T { get; set; } = NullLocalizer.Instance;

    /// <summary>
    /// The page size for loading data from database during export.
    /// </summary>
    public static int PageSize => 100;

    public virtual async Task<DataExportResult> ExportAsync(DataExportRequest request, CancellationToken cancelToken = default)
    {
        Guard.NotNull(request);
        Guard.NotNull(request.Profile);
        Guard.NotNull(request.Provider?.Value);

        var ctx = CreateExporterContext(request, false, cancelToken);
        var profile = request.Profile;

        if (!profile.Enabled)
        {
            return ctx.Result;
        }

        var lockKey = $"dataexporter:profile:{profile.Id}";
        if (AsyncLock.IsLockHeld(lockKey))
        {
            ctx.Result.LastError = $"The execution of the profile \"{profile.Name.NaIfEmpty()}\" (ID {profile.Id}) is locked.";
            return ctx.Result;
        }

        // The export directory is the "Content" subfolder. ZIP and LOG file are in the parent folder.
        var dir = await _exportProfileService.GetExportDirectoryAsync(profile, "Content", true);
        var logFile = await dir.Parent.GetFileAsync("log.txt");
        var zipName = PathUtility.SanitizeFileName(dir.Parent.Name) + ".zip";
        var zipFile = await dir.Parent.GetFileAsync(zipName);

        await dir.FileSystem.TryDeleteFileAsync(logFile);
        await dir.FileSystem.TryDeleteFileAsync(zipFile);
        dir.FileSystem.ClearDirectory(dir, false, TimeSpan.Zero);

        using (await AsyncLock.KeyedAsync(lockKey, null, cancelToken))
        using (var logger = new TraceLogger(logFile, false))
        {
            try
            {
                ctx.ExportDirectory = ctx.ExecuteContext.ExportDirectory = dir;
                ctx.Log = ctx.ExecuteContext.Log = logger;
                ctx.ZipFile = zipFile;

                if (request?.Provider?.Value?.FileExtension?.HasValue() ?? false)
                {
                    ctx.Result.ExportDirectory = dir;
                }

                using (var scope = new DbContextScope(_db, autoDetectChanges: false, forceNoTracking: true))
                {
                    var stores = await Init(ctx);
                    await CheckPermission(ctx);

                    ctx.ExecuteContext.Profile = CreateDynamic(profile);
                    ctx.ExecuteContext.Language = CreateDynamic(_workContext.WorkingLanguage);
                    ctx.ExecuteContext.Customer = ToDynamic(_workContext.CurrentCustomer);
                    ctx.ExecuteContext.Currency = ToDynamic(_workContext.WorkingCurrency, ctx);

                    foreach (var store in stores)
                    {
                        if (ctx.ExecuteContext.Abort != DataExchangeAbortion.None)
                            break;

                        ctx.Store = store;
                        ctx.LastId = 0;

                        await InternalExport(ctx);
                    }
                }

                if (!ctx.IsPreview && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
                {
                    if (ctx.IsFileBasedExport)
                    {
                        if (profile.CreateZipArchive)
                        {
                            ZipFile.CreateFromDirectory(ctx.ExportDirectory.PhysicalPath, ctx.ZipFile.PhysicalPath, CompressionLevel.Fastest, false);

                            // INFO: We need to update the file status. Otherwise 'IFile.Exist' sometimes returns "false" even though the file exists.
                            ctx.ZipFile = await dir.Parent.GetFileAsync(zipName);
                        }

                        if (profile.Deployments.Any(x => x.Enabled))
                        {
                            await SetProgress(T("Common.Publishing"), ctx);

                            var allDeploymentsSucceeded = await Deploy(ctx);
                            if (allDeploymentsSucceeded && profile.Cleanup)
                            {
                                ctx.Log.Info("Cleaning up export folder.");
                                dir.FileSystem.ClearDirectory(dir, false, TimeSpan.Zero);
                            }
                        }
                    }

                    if (profile.EmailAccountId != 0 && !ctx.Supports(ExportFeatures.CanOmitCompletionMail))
                    {
                        await SendCompletionEmail(ctx);
                    }
                }
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
                ctx.Result.LastError = ex.ToAllMessages(true);
            }
            finally
            {
                await Finalize(ctx);
            }

        }

        cancelToken.ThrowIfCancellationRequested();

        return ctx.Result;
    }

    public virtual async Task<DataExportPreviewResult> PreviewAsync(DataExportRequest request, int pageIndex, int pageSize)
    {
        Guard.NotNull(request);
        Guard.NotNull(request.Profile);
        Guard.NotNegative(pageIndex);
        Guard.NotNegative(pageSize);

        var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5.0));
        var ctx = CreateExporterContext(request, true, cancellation.Token);

        var limit = Math.Max(request.Profile.Limit, 0);
        var take = limit > 0 && limit < pageSize ? limit : pageSize;
        var skip = Math.Max(ctx.Request.Profile.Offset, 0) + (pageIndex * take);

        var _ = await Init(ctx);
        await CheckPermission(ctx);

        var result = new DataExportPreviewResult
        {
            TotalRecords = ctx.ShopMetadata.First().Value.TotalRecords
        };

        var query = await GetEntitiesQuery(ctx);
        query = ApplyPaging(query, skip, take, ctx);
        var data = await query.ToListAsync(cancellation.Token);

        switch (ctx.Request.Provider.Value.EntityType)
        {
            case ExportEntityType.Customer:
                data.Cast<Customer>().Each(x => result.Data.Add(ToDynamic(x)));
                break;
            case ExportEntityType.NewsletterSubscription:
                data.Cast<NewsletterSubscription>().Each(x => result.Data.Add(DataExporter.ToDynamic(x, ctx)));
                break;
        }

        return result;
    }

    private async Task<List<Store>> Init(DataExporterContext ctx)
    {
        List<Store> result = null;
        var ct = ctx.CancelToken;
        var provider = ctx.Request.Provider.Value;
        var currency = await _db.Currencies.FindByIdAsync(ctx.Projection.CurrencyId ?? 0, false, ct);
        var language = await _db.Languages.FindByIdAsync(ctx.Projection.LanguageId ?? 0, false, ct);
        var customer = await _db.Customers
            .IncludeCustomerRoles()
            .FindByIdAsync(ctx.Projection.CustomerId ?? 0, true, ct);

        if (currency != null)
        {
            _workContext.WorkingCurrency = currency;
        }
        if (language != null)
        {
            _workContext.WorkingLanguage = language;
        }
        if (customer != null)
        {
            // Do not check the permissions of projected customers. This complicates the configuration for the admin,
            // e.g. if prices are to be exported as they are seen by guests.
            ctx.Request.HasPermission = true;
            _workContext.CurrentCustomer = customer;
        }

        ctx.Stores = _services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
        ctx.Languages = await _db.Languages.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);

        if (!ctx.IsPreview)
        {

            if (provider.EntityType == ExportEntityType.Customer)
            {
                ctx.Countries = await _db.Countries.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
            }
            if (provider.EntityType == ExportEntityType.Customer)
            {
                ctx.StateProvinces = await _db.StateProvinces.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
            }
            if (provider.EntityType == ExportEntityType.Customer)
            {
                var subscriptionEmails = await _db.NewsletterSubscriptions
                    .AsNoTracking()
                    .Where(x => x.Active)
                    .Select(x => x.Email)
                    .Distinct()
                    .ToListAsync(ct);

                ctx.NewsletterSubscriptions = new HashSet<string>(subscriptionEmails, StringComparer.OrdinalIgnoreCase);
            }

            // Get all translations and slugs for global entities in one go.
            ctx.Translations[nameof(Currency)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Currency), null);
            ctx.Translations[nameof(Country)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Country), null);
            ctx.Translations[nameof(StateProvince)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(StateProvince), null);

        }

        if (!ctx.IsPreview && ctx.Request.Profile.PerStore)
        {
            result = [.. ctx.Stores.Values.Where(x => x.Id == ctx.Filter.StoreId || ctx.Filter.StoreId == 0)];
        }
        else
        {
            int? storeId = ctx.Filter.StoreId == 0 ? ctx.Projection.StoreId : ctx.Filter.StoreId;
            ctx.Store = ctx.Stores.Values.FirstOrDefault(x => x.Id == (storeId ?? _services.StoreContext.CurrentStore.Id));

            result = [ctx.Store];
        }

        // Get some metadata for each store.
        foreach (var store in result)
        {
            ctx.Store = store;

            var query = await GetEntitiesQuery(ctx);
            query = ApplyPaging(query, ctx.Request.Profile.Offset, int.MaxValue, ctx);

            ctx.ShopMetadata[store.Id] = new ShopMetadata
            {
                TotalRecords = query.Count(),
                MaxId = !ctx.IsPreview ? (query.Max(x => (int?)x.Id) ?? 0) : 0,
                MasterLanguageId = await _languageService.GetMasterLanguageIdAsync(store.Id)
            };
        }

        return result;
    }

    private async Task InternalExport(DataExporterContext ctx)
    {
        var context = ctx.ExecuteContext;
        var dir = context.ExportDirectory;
        var profile = ctx.Request.Profile;
        var provider = ctx.Request.Provider.Value;
        var dataExchangeSettings = await _services.SettingFactory.LoadSettingsAsync<DataExchangeSettings>(ctx.Store.Id);
        var publicDeployment = profile.Deployments.FirstOrDefault(x => x.DeploymentType == ExportDeploymentType.PublicFolder);
        var fileExtension = provider.FileExtension.NullEmpty()?.ToLower()?.EnsureStartsWith(".") ?? string.Empty;

        context.FileIndex = 0;
        context.Store = DataExporter.ToDynamic(ctx.Store);
        context.MaxFileNameLength = dataExchangeSettings.MaxFileNameLength;
        context.HasPublicDeployment = publicDeployment != null;
        context.PublicDirectory = await _exportProfileService.GetDeploymentDirectoryAsync(publicDeployment, true);
        context.PublicDirectoryUrl = await _exportProfileService.GetDeploymentDirectoryUrlAsync(publicDeployment, ctx.Store);

        ctx.Log.Info(CreateLogHeader(ctx));

        using var segmenter = CreateSegmenter(ctx) ?? throw new NotSupportedException($"Unsupported entity type '{provider.EntityType}'.");
        if (segmenter.TotalRecords <= 0)
        {
            ctx.Log.Info("There are no records to export.");
        }

        while (context.Abort == DataExchangeAbortion.None && segmenter.HasData)
        {
            IFile file = null;

            segmenter.RecordPerSegmentCount = 0;
            context.RecordsSucceeded = 0;

            if (ctx.IsFileBasedExport)
            {
                context.FileIndex += 1;
                context.FileName = _exportProfileService.ResolveTokens(profile, profile.FileNamePattern, context.FileIndex, context.MaxFileNameLength, ctx.Store) + fileExtension;
                file = await dir.GetFileAsync(context.FileName);

            }

            if (await CallExportProvider("Execute", file, ctx))
            {
                if (ctx.IsFileBasedExport && file.Exists)
                {
                    ctx.Result.Files.Add(new DataExportResult.ExportFileInfo
                    {
                        StoreId = ctx.Store.Id,
                        FileName = context.FileName
                    });
                }
            }

            if (context.IsMaxFailures)
            {
                ctx.Log.Warn("Export aborted. The maximum number of failures has been reached.");
            }
            if (ctx.CancelToken.IsCancellationRequested)
            {
                ctx.Log.Warn("Export aborted. A cancellation has been requested.");
            }
        }

        if (context.Abort != DataExchangeAbortion.Hard)
        {
            var calledExecuted = false;

            foreach (var dataUnit in context.ExtraDataUnits)
            {
                context.DataStreamId = dataUnit.Id;

                var success = true;
                var file = dataUnit.FileName.HasValue()
                    ? await dir.GetFileAsync(dataUnit.FileName)
                    : null;

                if (!dataUnit.RelatedType.HasValue)
                {
                    // Data unit added by provider.
                    calledExecuted = true;
                    success = await CallExportProvider("OnExecuted", file, ctx);
                }

                if (success && ctx.IsFileBasedExport && dataUnit.DisplayInFileDialog && file.Exists)
                {
                    // Save info about extra file.
                    ctx.Result.Files.Add(new DataExportResult.ExportFileInfo
                    {
                        StoreId = ctx.Store.Id,
                        FileName = dataUnit.FileName,
                        Label = dataUnit.Label,
                        RelatedType = dataUnit.RelatedType
                    });
                }
            }

            if (!calledExecuted)
            {
                // Always call OnExecuted.
                await CallExportProvider("OnExecuted", null, ctx);
            }
        }

        context.ExtraDataUnits.Clear();
    }

    private async Task Finalize(DataExporterContext ctx)
    {
        var profile = ctx.Request.Profile;

        try
        {
            if (!ctx.IsPreview && profile.Id != 0)
            {
                ctx.Result.Files = ctx.Result.Files.OrderBy(x => x.RelatedType).ToList();
                profile.ResultInfo = XmlHelper.Serialize(ctx.Result);

                await _db.SaveChangesAsync(ctx.CancelToken);
            }
        }
        catch (Exception ex)
        {
            ctx.Log.ErrorsAll(ex);
        }

        DetachAllEntitiesAndClear(ctx);

        try
        {
            ctx.NewsletterSubscriptions.Clear();
            ctx.Countries.Clear();
            ctx.StateProvinces.Clear();
            ctx.Languages.Clear();
            ctx.Stores.Clear();
            ctx.Translations.Clear();
            ctx.UrlRecords.Clear();

            ctx.TranslationsPerPage?.Clear();
            ctx.UrlRecordsPerPage?.Clear();

            ctx.Request.CustomData.Clear();
            ctx.ExecuteContext.CustomProperties.Clear();

            ctx.ExecuteContext.Log = null;
            ctx.Log = null;
        }
        catch (Exception ex)
        {
            ctx?.Log?.ErrorsAll(ex);
        }
    }

    #region Entities

    private async Task<IEnumerable<TEntity>> LoadEntities<TEntity>(DataExporterContext ctx)
    {
        DetachAllEntitiesAndClear(ctx);

        if (ctx.LastId >= ctx.ShopMetadata[ctx.Store.Id].MaxId)
        {
            // End of data reached.
            return null;
        }

        var query = await GetEntitiesQuery(ctx);
        query = ApplyPaging(query, null, PageSize, ctx);
        var entities = await query.ToListAsync(ctx.CancelToken);

        if (entities.Count == 0)
        {
            return null;
        }

        ctx.LastId = entities.Last().Id;
        await SetProgress(entities.Count, ctx);

        return entities.Cast<TEntity>();
    }

    private async Task<IQueryable<BaseEntity>> GetEntitiesQuery(DataExporterContext ctx)
    {
        var f = ctx.Filter;
        var entityType = ctx.Request.Provider.Value.EntityType;
        var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : f.StoreId;
        IQueryable<int> customerIdsByRolesQuery = null;
        IQueryable<BaseEntity> result = null;

        if (!f.CustomerRoleIds.IsNullOrEmpty())
        {
            customerIdsByRolesQuery = _db.CustomerRoleMappings
                .AsNoTracking()
                .Where(x => f.CustomerRoleIds.Contains(x.CustomerRoleId))
                .Select(x => x.CustomerId);
        }

        if (entityType == ExportEntityType.Customer)
        {
            var query = _db.Customers
                .IncludeCustomerRoles()
                .Include(x => x.ContactAddress)
                .Include(x => x.Addresses)
                .AsSplitQuery()
                .AsNoTrackingWithIdentityResolution()
                .AsNoCaching();

            if (f.IsActiveCustomer.HasValue)
                query = query.Where(x => x.Active == f.IsActiveCustomer.Value);


            if (!f.ContactCountryIds.IsNullOrEmpty())
                query = query.Where(x => x.ContactAddress != null && f.ContactCountryIds.Contains(x.ContactAddress.CountryId ?? 0));

            if (f.LastActivityFrom.HasValue)
                query = query.Where(x => f.LastActivityFrom.Value <= x.LastActivityDateUtc);

            if (f.LastActivityTo.HasValue)
                query = query.Where(x => f.LastActivityTo.Value >= x.LastActivityDateUtc);

            if (f.CreatedFrom.HasValue)
                query = query.Where(x => f.CreatedFrom.Value <= x.CreatedOnUtc);

            if (f.CreatedTo.HasValue)
                query = query.Where(x => f.CreatedTo.Value >= x.CreatedOnUtc);

            if (customerIdsByRolesQuery != null)
                query = query.Where(x => customerIdsByRolesQuery.Contains(x.Id));

            result = query;
        }
        else if (entityType == ExportEntityType.NewsletterSubscription)
        {
            var query =
                from ns in _db.NewsletterSubscriptions.AsNoTracking()
                join c in _db.Customers.AsNoTracking() on ns.Email equals c.Email into customers
                from c in customers.DefaultIfEmpty()
                select new NewsletterSubscriber
                {
                    Subscription = ns,
                    Customer = c
                };

            if (storeId > 0)
                query = query.Where(x => x.Subscription.StoreId == storeId);

            if (f.IsActiveSubscriber.HasValue)
                query = query.Where(x => x.Subscription.Active == f.IsActiveSubscriber.Value);

            if (f.WorkingLanguageId.GetValueOrDefault() > 0)
            {
                var masterLanguageId = ctx.ShopMetadata.Get(storeId)?.MasterLanguageId ?? 0;

                query = f.WorkingLanguageId == masterLanguageId
                    ? query.Where(x => x.Subscription.WorkingLanguageId == 0 || x.Subscription.WorkingLanguageId == f.WorkingLanguageId)
                    : query.Where(x => x.Subscription.WorkingLanguageId == f.WorkingLanguageId);
            }

            if (f.CreatedFrom.HasValue)
                query = query.Where(x => f.CreatedFrom.Value <= x.Subscription.CreatedOnUtc);

            if (f.CreatedTo.HasValue)
                query = query.Where(x => f.CreatedTo.Value >= x.Subscription.CreatedOnUtc);

            if (customerIdsByRolesQuery != null)
                query = query.Where(x => customerIdsByRolesQuery.Contains(x.Customer.Id));

            result = query.Select(x => x.Subscription);
        }
        else
        {
            throw new NotSupportedException($"Unsupported entity type '{entityType}'.");
        }

        if (ctx.Request.EntitiesToExport.Count > 0)
        {
            result = result.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
        }

        return result;
    }

    private static IQueryable<BaseEntity> ApplyPaging(IQueryable<BaseEntity> query, int? skip, int take, DataExporterContext ctx)
    {
        // Skip used for data preview grid (classic paging) or initial query (offset profile setting).
        var skipValue = skip.GetValueOrDefault();
        if (skipValue == 0 && ctx.LastId == 0)
        {
            skipValue = Math.Max(ctx.Request.Profile.Offset, 0);
        }

        query = query.OrderBy(x => x.Id);

        if (skipValue > 0)
        {
            query = query.Skip(skipValue);
        }
        else if (ctx.LastId > 0)
        {
            query = query.Where(x => x.Id > ctx.LastId);
        }

        if (take != int.MaxValue)
        {
            query = query.Take(take);
        }

        return query;
    }

    private void DetachAllEntitiesAndClear(DataExporterContext ctx)
    {
        try
        {
            if (ctx.CustomerBatchContext != null)
            {
                Detach(x => x is Customer || x is GenericAttribute || x is CustomerContent);

                ctx.CustomerBatchContext.Clear();
            }

            switch (ctx.Request.Provider.Value.EntityType)
            {
                case ExportEntityType.NewsletterSubscription:
                    Detach(x => x is NewsletterSubscription || x is Customer);
                    break;
            }
        }
        catch (Exception ex)
        {
            ctx.Log.Warn(ex, "Detaching entities failed.");
        }

        int Detach(Func<BaseEntity, bool> predicate)
        {
            return _db.DetachEntities(predicate);
        }
    }

    #endregion

    #region Utilities

    private IExportDataSegmenterProvider CreateSegmenter(DataExporterContext ctx)
    {
        var entityType = ctx.Request.Provider.Value.EntityType;
        var offset = Math.Max(ctx.Request.Profile.Offset, 0);
        var limit = Math.Max(ctx.Request.Profile.Limit, 0);
        var recordsPerSegment = ctx.IsPreview ? 0 : Math.Max(ctx.Request.Profile.BatchSize, 0);
        var totalRecords = offset + ctx.ShopMetadata[ctx.Store.Id].TotalRecords;

        if (entityType == ExportEntityType.Customer)
        {
            Task dataLoaded(ICollection<Customer> entities)
            {
                ctx.CustomerBatchContext = new CustomerBatchContext(entities, _services);
                return Task.CompletedTask;
            };

            ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<Customer>
            (
                () => LoadEntities<Customer>(ctx),
                dataLoaded,
                entity => Convert(entity, ctx),
                offset, PageSize, limit, recordsPerSegment, totalRecords
            );
        }
        else if (entityType == ExportEntityType.NewsletterSubscription)
        {
            ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<NewsletterSubscription>
            (
                () => LoadEntities<NewsletterSubscription>(ctx),
                null,
                entity => Convert(entity, ctx),
                offset, PageSize, limit, recordsPerSegment, totalRecords
            );
        }
        else
        {
            ctx.ExecuteContext.DataSegmenter = null;
        }

        return ctx.ExecuteContext.DataSegmenter as IExportDataSegmenterProvider;
    }

    private static async Task<bool> CallExportProvider(string method, IFile file, DataExporterContext ctx)
    {
        var context = ctx.ExecuteContext;
        var dir = context.ExportDirectory;
        var provider = ctx.Request.Provider.Value;

        if (method != "Execute" && method != "OnExecuted")
        {
            throw new NotSupportedException($"Unknown export method {method.NaIfEmpty()}.");
        }

        try
        {
            if (ctx.IsFileBasedExport && file != null)
            {
                await file.CreateAsync(null, true, ctx.CancelToken);
                context.DataStream = new ExportFileStream(await file.OpenWriteAsync(null, ctx.CancelToken));
            }
            else
            {
                context.DataStream = new ExportFileStream(new MemoryStream());
            }

            if (method == "Execute")
            {
                await provider.ExecuteAsync(context, ctx.CancelToken);
            }
            else if (method == "OnExecuted")
            {
                await provider.OnExecutedAsync(context, ctx.CancelToken);
            }
        }
        catch (Exception ex)
        {
            context.Abort = DataExchangeAbortion.Hard;
            ctx.Log.Error(ex, $"The provider failed at the {method.NaIfEmpty()} method.");
            ctx.Result.LastError = ex.ToAllMessages(true);
        }
        finally
        {
            context.DataStream?.Dispose();
            context.DataStream = null;

            if (context.Abort == DataExchangeAbortion.Hard && ctx.IsFileBasedExport && file.Exists)
            {
                await file.DeleteAsync(ctx.CancelToken);
            }

            if (method == "Execute")
            {
                using var psb = StringBuilderPool.Instance.Get(out var unitFileInfos);

                var relatedDataUnits = context.ExtraDataUnits
                    .Where(x => x.RelatedType.HasValue && x.FileName.HasValue() && x.DataStream != null)
                    .ToList();

                foreach (var unit in relatedDataUnits)
                {
                    // Set unit.DataStream to null so that providers know that this unit should no longer be written to.
                    // We need these units later for ExportProfile.ResultInfo.
                    unit.DataStream?.Dispose();
                    unit.DataStream = null;

                    var unitFile = unit.FileName.HasValue()
                        ? await dir.GetFileAsync(unit.FileName)
                        : null;

                    if (context.Abort == DataExchangeAbortion.Hard)
                    {
                        if (ctx.IsFileBasedExport && unitFile.Exists)
                        {
                            await unitFile.DeleteAsync(ctx.CancelToken);
                        }
                    }
                    else
                    {
                        unitFileInfos.AppendLine();
                        unitFileInfos.Append($"Provider reports {unit.RecordsSucceeded:N0} successfully exported record(s) of type {unit.RelatedType.Value} to {unitFile?.PhysicalPath?.NaIfEmpty()}.");
                    }
                }

                if (context.Abort != DataExchangeAbortion.Hard)
                {
                    ctx.Log.Info($"Provider reports {context.RecordsSucceeded:N0} successfully exported record(s) of type {provider.EntityType} to {file?.PhysicalPath?.NaIfEmpty()}.");
                }

                ctx.Log.Info(unitFileInfos.ToString());
            }
        }

        return context.Abort != DataExchangeAbortion.Hard;
    }

    private async Task<bool> Deploy(DataExporterContext ctx)
    {
        var allSucceeded = true;
        var deployments = ctx.Request.Profile.Deployments
            .Where(x => x.Enabled)
            .OrderBy(x => x.Id)
            .ToArray();

        if (deployments.Length == 0)
        {
            return false;
        }

        var context = new ExportDeploymentContext
        {
            T = T,
            Log = ctx.Log,
            ExportProfileService = _exportProfileService,
            ExportDirectory = ctx.ExportDirectory,
            ZipFile = ctx.ZipFile,
            CreateZipArchive = ctx.Request.Profile.CreateZipArchive
        };

        foreach (var deployment in deployments)
        {
            IFilePublisher publisher = null;

            context.Result = new()
            {
                LastExecutionUtc = DateTime.UtcNow
            };

            try
            {
                switch (deployment.DeploymentType)
                {
                    case ExportDeploymentType.Email:
                        publisher = new EmailFilePublisher(_db,
                            (DatabaseMediaStorageProvider)_providerManager.GetProvider<IMediaStorageProvider>(DatabaseMediaStorageProvider.SystemName).Value,
                            _exportProfileService,
                            _queuedEmailService);
                        break;
                    case ExportDeploymentType.FileSystem:
                        publisher = new FileSystemFilePublisher(_services.ApplicationContext);
                        break;
                    case ExportDeploymentType.Ftp:
                        publisher = new FtpFilePublisher();
                        break;
                    case ExportDeploymentType.Http:
                        publisher = new HttpFilePublisher(_httpClientFactory);
                        break;
                    case ExportDeploymentType.PublicFolder:
                        publisher = new PublicFolderPublisher(_services.ApplicationContext);
                        break;
                }

                if (publisher != null)
                {
                    await publisher.PublishAsync(deployment, context, ctx.CancelToken);

                    if (!context.Result.Succeeded)
                    {
                        allSucceeded = false;
                    }
                }
            }
            catch (Exception ex)
            {
                allSucceeded = false;

                context.Result?.LastError = ex.ToAllMessages(true);

                ctx.Log.Error(ex, $"Deployment \"{deployment.Name}\" of type {deployment.DeploymentType} failed.");
            }

            deployment.ResultInfo = XmlHelper.Serialize(context.Result);
        }

        await _db.SaveChangesAsync(ctx.CancelToken);

        return allSucceeded;
    }

    private async Task SendCompletionEmail(DataExporterContext ctx)
    {
        var profile = ctx.Request.Profile;
        var emailAccount = await _db.EmailAccounts.FindByIdAsync(profile.EmailAccountId, false, ctx.CancelToken);
        if (emailAccount == null || emailAccount.Host.IsEmpty())
        {
            return;
        }

        var languageId = ctx.Projection.LanguageId ?? 0;
        var uri = ctx.Store.GetBaseUri();
        var storeInfo = $"{ctx.Store.Name} ({uri})";
        var intro = _services.Localization.GetResource("Admin.DataExchange.Export.CompletedEmail.Body", languageId).FormatInvariant(storeInfo);

        using var psb = StringBuilderPool.Instance.Get(out var body);
        body.Append(intro);

        if (ctx.Result.LastError.HasValue())
        {
            body.AppendFormat("<p style=\"color: #B94A48;\">{0}</p>", ctx.Result.LastError);
        }

        if (ctx.IsFileBasedExport && ctx.ZipFile.Exists)
        {
            var downloadUrl = _urlHelper.Value.Action("DownloadExportFile", "Export", new { area = "Admin", id = profile.Id, ctx.ZipFile.Name }, uri.Scheme, uri.Authority);
            body.AppendFormat("<p><a href='{0}' download>{1}</a></p>", downloadUrl, ctx.ZipFile.Name);
        }

        if (ctx.IsFileBasedExport && ctx.Result.Files.Count > 0)
        {
            body.Append("<p>");
            foreach (var file in ctx.Result.Files)
            {
                var downloadUrl = _urlHelper.Value.Action("DownloadExportFile", "Export", new { area = "Admin", id = profile.Id, name = file.FileName }, uri.Scheme, uri.Authority);
                body.AppendFormat("<div><a href='{0}' download>{1}</a></div>", downloadUrl, file.FileName);
            }
            body.Append("</p>");
        }

        using var message = new MailMessage
        {
            From = new(emailAccount.Email, emailAccount.DisplayName),
            Subject = _services.Localization.GetResource("Admin.DataExchange.Export.CompletedEmail.Subject", languageId).FormatInvariant(ctx.Request.Profile.Name),
            Body = body.ToString()
        };

        if (profile.CompletedEmailAddresses.HasValue())
        {
            var addresses = profile.CompletedEmailAddresses
                .SplitSafe(',')
                .Where(x => x.IsEmail())
                .Select(x => new MailAddress(x));

            message.To.AddRange(addresses);
        }

        if (message.To.Count == 0 && _contactDataSettings.CompanyEmailAddress.HasValue())
        {
            message.To.Add(new(_contactDataSettings.CompanyEmailAddress));
        }

        if (message.To.Count == 0)
        {
            message.To.Add(new(emailAccount.Email, emailAccount.DisplayName));
        }

        await using var client = await _mailService.ConnectAsync(emailAccount);
        await client.SendAsync(message, ctx.CancelToken);

        //_db.QueuedEmails.Add(new QueuedEmail
        //{
        //    From = emailAccount.Email,
        //    To = message.To.First().Address,
        //    Subject = message.Subject,
        //    Body = message.Body,
        //    CreatedOnUtc = DateTime.UtcNow,
        //    EmailAccountId = emailAccount.Id,
        //    SendManually = true
        //});
        //await _db.SaveChangesAsync();
    }

    private DataExporterContext CreateExporterContext(DataExportRequest request, bool isPreview, CancellationToken cancelToken)
    {
        var profile = request.Profile;
        var provider = request.Provider.Value;
        if (!Enum.IsDefined(provider.EntityType))
            throw new NotSupportedException($"Export profile {profile.Id} targets a retired entity type.");

        var context = new DataExporterContext
        {
            IsPreview = isPreview,
            Request = request,
            CancelToken = cancelToken,
            Filter = XmlHelper.Deserialize<ExportFilter>(profile.Filtering),
            Projection = XmlHelper.Deserialize<ExportProjection>(profile.Projection),
            ProgressInfo = T("Admin.DataExchange.Export.ProgressInfo")
        };

        context.ExecuteContext = new ExportExecuteContext(context.Result, cancelToken)
        {
            Filter = context.Filter,
            Projection = context.Projection,
            ProfileId = profile.Id,
            ProgressCallback = request.ProgressCallback
        };

        if (!isPreview && profile.ProviderConfigData.HasValue() && provider.ConfigurationInfo != null)
        {
            context.ExecuteContext.ConfigurationData = XmlHelper.Deserialize(profile.ProviderConfigData, provider.ConfigurationInfo.ModelType);
        }

        request.CustomData.Each(x => context.ExecuteContext.CustomProperties.Add(x.Key, x.Value));

        return context;
    }

    private string CreateLogHeader(DataExporterContext ctx)
    {
        var customer = _workContext.CurrentCustomer;
        var profile = ctx.Request.Profile;
        var provider = ctx.Request.Provider;
        var module = provider.Metadata.ModuleDescriptor;
        var providerName = provider.Metadata.FriendlyName.HasValue()
            ? $"{provider.Metadata.FriendlyName} ({profile.ProviderSystemName})"
            : profile.ProviderSystemName;

        using var psb = StringBuilderPool.Instance.Get(out var sb);

        sb.AppendLine();
        sb.AppendLine(new string('-', 40));
        sb.AppendLine("OmmCMS: v." + OmmCMSVersion.CurrentFullVersion);
        sb.Append("Export profile: " + profile.Name);
        sb.AppendLine(profile.Id == 0 ? " (transient)" : $" (ID {profile.Id})");
        sb.AppendLine("Export provider: " + providerName);

        sb.Append("Module: ");
        if (module != null)
        {
            var incompatible = module.Incompatible ? " INCOMPATIBLE" : string.Empty;
            sb.AppendLine($"{module.FriendlyName} ({module.SystemName}){incompatible}");
        }
        else
        {
            sb.AppendLine(string.Empty.NaIfEmpty());
        }

        sb.AppendLine("Entity: " + provider.Value.EntityType.ToString());

        try
        {
            sb.AppendLine($"Store: {ctx.Store.GetBaseUri().DnsSafeHost.NaIfEmpty()} (ID {ctx.Store.Id})");
        }
        catch
        {
        }

        sb.Append("Executed by: " + (customer.Email.HasValue() ? customer.Email : customer.SystemName));

        return sb.ToString();
    }

    private async Task CheckPermission(DataExporterContext ctx)
    {
        if (ctx.Request.HasPermission)
        {
            return;
        }

        var customer = _workContext.CurrentCustomer;

        if (customer.IsBackgroundTaskAccount())
        {
            return;
        }

        if (!await _services.Permissions.AuthorizeAsync(Permissions.Configuration.Export.Execute, customer))
        {
            throw new SecurityException(await _services.Permissions.GetUnauthorizedMessageAsync(Permissions.Configuration.Export.Execute));
        }
    }

    private static async Task SetProgress(string message, DataExporterContext ctx)
    {
        if (!ctx.IsPreview && message.HasValue())
        {
            try
            {
                await ctx.Request.ProgressCallback.Invoke(0, 0, message);
            }
            catch
            {
            }
        }
    }

    private static async Task SetProgress(int loadedRecords, DataExporterContext ctx)
    {
        if (!ctx.IsPreview && loadedRecords > 0)
        {
            try
            {
                var totalRecords = ctx.ShopMetadata.Sum(x => x.Value.TotalRecords);

                if (ctx.Request.Profile.Limit > 0 && totalRecords > ctx.Request.Profile.Limit)
                {
                    totalRecords = ctx.Request.Profile.Limit;
                }

                ctx.RecordCount = Math.Min(ctx.RecordCount + loadedRecords, totalRecords);
                var msg = ctx.ProgressInfo.FormatInvariant(ctx.RecordCount.ToString("N0"), totalRecords.ToString("N0"));
                await ctx.Request.ProgressCallback.Invoke(ctx.RecordCount, totalRecords, msg);
            }
            catch
            {
            }
        }
    }

    #endregion
}
