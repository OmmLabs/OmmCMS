using System.Runtime.CompilerServices;

using OmmCMS.Collections;
using OmmCMS.Core.Web;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Rules;
using OmmCMS.Core.Rules.Filters;

namespace OmmCMS.Core.Identity.Rules;

public partial class TargetGroupService : RuleProviderBase, ITargetGroupService
{
    private readonly SmartDbContext _db;
    private readonly IRuleService _ruleService;
    private readonly IUserAgentParser _userAgentParser;

    public TargetGroupService(
        SmartDbContext db,
        IRuleService ruleService,
        IUserAgentParser userAgentParser)
        : base(RuleScope.Customer)
    {
        _db = db;
        _ruleService = ruleService;
        _userAgentParser = userAgentParser;
    }

    public Localizer T { get; set; } = NullLocalizer.Instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<FilterExpressionGroup> CreateExpressionGroupAsync(int ruleSetId)
    {
        return await _ruleService.CreateExpressionGroupAsync(ruleSetId, this) as FilterExpressionGroup;
    }

    public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
    {
        var expression = new FilterExpression();
        await base.ConvertRuleAsync(rule, expression);
        expression.Descriptor = ((RuleExpression)expression).Descriptor as FilterDescriptor;
        return expression;
    }

    public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
    {
        var group = new FilterExpressionGroup(typeof(Customer))
        {
            Id = ruleSet.Id,
            LogicalOperator = ruleSet.LogicalOperator,
            IsSubGroup = ruleSet.IsSubGroup,
            Value = ruleSet.Id,
            RawValue = ruleSet.Id.ToString(),
            Provider = this
            // INFO: filter group does NOT access any descriptor
        };

        return group;
    }

    public async Task<IPagedList<Customer>> ProcessFilterAsync(
        int[] ruleSetIds,
        LogicalRuleOperator logicalOperator,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        Guard.NotNull(ruleSetIds, nameof(ruleSetIds));

        var filters = await ruleSetIds
            .SelectAwait(id => _ruleService.CreateExpressionGroupAsync(id, this))
            .Where(x => x != null)
            .Cast<FilterExpression>()
            .ToArrayAsync();

        return ProcessFilter(filters, logicalOperator, pageIndex, pageSize);
    }

    public IPagedList<Customer> ProcessFilter(
        FilterExpression[] filters,
        LogicalRuleOperator logicalOperator,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        Guard.NotNull(filters, nameof(filters));

        if (filters.Length == 0)
        {
            return Array.Empty<Customer>().ToPagedList(0, int.MaxValue);
        }

        var query = _db.Customers.AsNoTracking().Where(x => !x.IsSystemAccount);

        FilterExpressionGroup group = null;

        if (filters.Length == 1 && filters[0] is FilterExpressionGroup group2)
        {
            group = group2;
        }
        else
        {
            group = new FilterExpressionGroup(typeof(Customer)) { LogicalOperator = logicalOperator };
            group.AddExpressions(filters);
        }

        // Create lambda predicate
        var predicate = group.ToPredicate(query.Provider);

        // Apply predicate to query
        query = query
            .Where(predicate)
            .Cast<Customer>()
            .OrderByDescending(c => c.CreatedOnUtc);

        return query.ToPagedList(pageIndex, pageSize);
    }

    protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
    {
        var descriptors = new List<FilterDescriptor>
        {
            new FilterDescriptor<Customer, bool>(x => x.Active)
            {
                Name = "Active",
                DisplayName = T("Admin.Rules.FilterDescriptor.Active"),
                RuleType = RuleType.Boolean
            },
            new FilterDescriptor<Customer, string>(x => x.Salutation)
            {
                Name = "Salutation",
                DisplayName = T("Admin.Rules.FilterDescriptor.Salutation"),
                RuleType = RuleType.String
            },
            new FilterDescriptor<Customer, string>(x => x.Title)
            {
                Name = "Title",
                DisplayName = T("Admin.Rules.FilterDescriptor.Title"),
                RuleType = RuleType.String
            },
            new FilterDescriptor<Customer, string>(x => x.Company)
            {
                Name = "Company",
                DisplayName = T("Admin.Rules.FilterDescriptor.Company"),
                RuleType = RuleType.String
            },
            new FilterDescriptor<Customer, string>(x => x.Gender)
            {
                Name = "Gender",
                DisplayName = T("Admin.Rules.FilterDescriptor.Gender"),
                RuleType = RuleType.String
            },
            new FilterDescriptor<Customer, string>(x => x.CustomerNumber)
            {
                Name = "CustomerNumber",
                DisplayName = T("Admin.Rules.FilterDescriptor.CustomerNumber"),
                RuleType = RuleType.String
            },
            new AnyFilterDescriptor<Customer, CustomerRoleMapping, int>(x => x.CustomerRoleMappings, rm => rm.CustomerRoleId)
            {
                Name = "IsInCustomerRole",
                DisplayName = T("Admin.Rules.FilterDescriptor.IsInCustomerRole"),
                RuleType = RuleType.IntArray,
                SelectList = new RemoteRuleValueSelectList("CustomerRole") { Multiple = true }
            },
            new FilterDescriptor<Customer, string>(x => x.TimeZoneId)
            {
                Name = "TimeZone",
                DisplayName = T("Admin.Rules.FilterDescriptor.TimeZone"),
                RuleType = RuleType.String
            },
            new FilterDescriptor<Customer, string>(x => x.LastUserAgent)
            {
                Name = "LastUserAgent",
                DisplayName = T("Admin.Rules.FilterDescriptor.LastUserAgent"),
                RuleType = RuleType.String
            },
            new FilterDescriptor<Customer, string>(x => x.LastUserDeviceType)
            {
                Name = "LastDeviceFamily",
                DisplayName = T("Admin.Rules.FilterDescriptor.LastDeviceFamily"),
                RuleType = RuleType.StringArray,
                SelectList = new LocalRuleValueSelectList(_userAgentParser.GetDetectableDevices()
                    .Select(x => new RuleValueSelectListOption { Value = x, Text = x }).ToArray()) { Multiple = true }
            },
            new FilterDescriptor<Customer, int?>(x => x.BillingAddress != null ? x.BillingAddress.CountryId : 0)
            {
                Name = "BillingCountry",
                DisplayName = T("Admin.Rules.FilterDescriptor.BillingCountry"),
                RuleType = RuleType.IntArray,
                SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
            },
            new FilterDescriptor<Customer, int?>(x => x.ShippingAddress != null ? x.ShippingAddress.CountryId : 0)
            {
                Name = "ShippingCountry",
                DisplayName = T("Admin.Rules.FilterDescriptor.ShippingCountry"),
                RuleType = RuleType.IntArray,
                SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
            },
            new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.LastActivityDateUtc, DateTime.UtcNow))
            {
                Name = "LastActivityDays",
                DisplayName = T("Admin.Rules.FilterDescriptor.LastActivityDays"),
                RuleType = RuleType.NullableInt
            },
            new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.LastLoginDateUtc, DateTime.UtcNow))
            {
                Name = "LastLoginDays",
                DisplayName = T("Admin.Rules.FilterDescriptor.LastLoginDays"),
                RuleType = RuleType.NullableInt
            },
            new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.LastForumVisit, DateTime.UtcNow))
            {
                Name = "LastForumVisitDays",
                DisplayName = T("Admin.Rules.FilterDescriptor.LastForumVisit"),
                RuleType = RuleType.NullableInt
            },
            new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.CreatedOnUtc, DateTime.UtcNow))
            {
                Name = "CreatedDays",
                DisplayName = T("Admin.Rules.FilterDescriptor.CreatedDays"),
                RuleType = RuleType.NullableInt
            },
            new FilterDescriptor<Customer, int?>(x => EF.Functions.DateDiffDay(x.BirthDate, DateTime.UtcNow))
            {
                Name = "BirthDateDays",
                DisplayName = T("Admin.Rules.FilterDescriptor.BirthDate"),
                RuleType = RuleType.NullableInt
            },
            new TargetGroupFilterDescriptor(_ruleService, this)
            {
                Name = "RuleSet",
                DisplayName = T("Admin.Rules.FilterDescriptor.RuleSet"),
                RuleType = RuleType.Int,
                Operators = new[] { RuleOperator.IsEqualTo, RuleOperator.IsNotEqualTo },
                SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.TargetGroup)
            },

        };

        return Task.FromResult(descriptors.Cast<RuleDescriptor>());
    }
}
