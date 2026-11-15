using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Content.Menus;

public partial class DefaultLinkProvider : ILinkProvider
{
    public const string SchemaTopic = "topic";

    private static readonly string[] _supportedSchemas =
        new[] { SchemaTopic };

    private readonly SmartDbContext _db;

    public DefaultLinkProvider(SmartDbContext db)
    {
        _db = db;
    }

    public int Order { get; }

    public virtual IEnumerable<LinkBuilderMetadata> GetBuilderMetadata()
    {
        return new[]
        {
            new LinkBuilderMetadata { Schema = SchemaTopic, Icon = "file-pen", ResKey = "Common.Entity.Topic" },
        };
    }

    public virtual async Task<LinkTranslationResult> TranslateAsync(LinkExpression expression, int storeId, int languageId)
    {
        if (!_supportedSchemas.Contains(expression.Schema))
        {
            return null;
        }

        Type entityType = null;
        string entityName = null;
        LinkTranslatorEntitySummary summary = null;

        switch (expression.Schema)
        {
            case SchemaTopic:
                entityType = typeof(Topic);
                entityName = nameof(Topic);
                summary = await GetEntityDataAsync(expression, storeId);
                break;
        }

        return new LinkTranslationResult
        {
            EntitySummary = summary,
            EntityName = entityName,
            EntityType = entityType,
            Status = summary == null || summary.Deleted
                ? LinkStatus.NotFound
                : summary.Published ? LinkStatus.Ok : LinkStatus.Hidden
        };
    }

    private async Task<LinkTranslatorEntitySummary> GetEntityDataAsync(LinkExpression expression, int storeId)
    {
        LinkTranslatorEntitySummary summary = null;
        string systemName = null;

        if (!int.TryParse(expression.Target, out var entityId))
        {
            systemName = expression.Target;
        }

        if (expression.Schema == "topic")
        {
            Topic topic = null;

            if (string.IsNullOrEmpty(systemName))
            {
                topic = await _db.Topics.FindByIdAsync(entityId, false);
            }
            else
            {
                topic = await _db.Topics
                    .AsNoTracking()
                    .ApplyStandardFilter(true, null, storeId)
                    .FirstOrDefaultAsync(x => x.SystemName == systemName);
            }

            if (topic != null)
            {
                summary = new LinkTranslatorEntitySummary
                {
                    Id = topic.Id,
                    Name = topic.SystemName,
                    Title = topic.Title,
                    ShortTitle = topic.ShortTitle,
                    Published = topic.IsPublished,
                    SubjectToAcl = topic.SubjectToAcl,
                    LimitedToStores = topic.LimitedToStores,
                    // INFO: 'ShortTitle' is intended for links. 'Title' can be very long.
                    LocalizedPropertyNames = new[] { nameof(Topic.ShortTitle), nameof(Topic.Title) }
                };
            }
        }

        return summary;
    }
}