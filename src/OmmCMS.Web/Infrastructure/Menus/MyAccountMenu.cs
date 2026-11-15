using OmmCMS.Collections;
using OmmCMS.Core.Configuration;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Stores;
using OmmCMS.Events;

namespace OmmCMS.Web.Infrastructure;

public partial class MyAccountMenu : IMenu
{
    private readonly IEventPublisher _eventPublisher;
    private readonly CustomerSettings _customerSettings;

    protected TreeNode<MenuItem> _root;
    private TreeNode<MenuItem> _currentNode;
    private bool _currentNodeResolved;

    public MyAccountMenu(IEventPublisher eventPublisher, CustomerSettings customerSettings)
    {
        _eventPublisher = eventPublisher;
        _customerSettings = customerSettings;
        T = NullLocalizer.Instance;
    }

    public Localizer T { get; set; } = NullLocalizer.Instance;

    public string Name => "MyAccount";

    public bool ApplyPermissions => true;

    public virtual async Task<TreeNode<MenuItem>> GetRootNodeAsync()
    {
        if (_root == null)
        {
            _root = await BuildAsync();
            await _eventPublisher.PublishAsync(new MenuBuiltEvent(Name, _root));
        }

        return _root;
    }

    public virtual Task ResolveElementCountAsync(TreeNode<MenuItem> curNode, bool deep = false)
    {
        return Task.CompletedTask;
    }

    public virtual Task<TreeNode<MenuItem>> ResolveCurrentNodeAsync(ActionContext actionContext)
    {
        if (!_currentNodeResolved)
        {
            _currentNode = _root.SelectNode(x => x.Value.IsCurrent(actionContext), true);
            _currentNodeResolved = true;
        }

        return Task.FromResult(_currentNode);
    }

    public IDictionary<string, TreeNode<MenuItem>> GetAllCachedMenus()
    {
        // No caching.
        return new Dictionary<string, TreeNode<MenuItem>>();
    }

    public Task ClearCacheAsync()
    {
        // No caching.
        return Task.CompletedTask;
    }

    protected virtual Task<TreeNode<MenuItem>> BuildAsync()
    {
        var root = new TreeNode<MenuItem>(new())
        {
            Id = Name
        };

        root.AppendRange(
        [
            new MenuItem
            {
                Id = "info",
                Text = T("Account.CustomerInfo"),
                Icon = "user",
                ActionName = "Info",
                ControllerName = "Customer"
            },
            new MenuItem
            {
                Id = "addresses",
                Text = T("Account.CustomerAddresses"),
                Icon = "address-book",
                ActionName = "Addresses",
                ControllerName = "Customer"
            }
        ]);

        root.Append(new MenuItem
        {
            Id = "changepassword",
            Text = T("Account.ChangePassword"),
            Icon = "unlock",
            ActionName = "ChangePassword",
            ControllerName = "Identity"
        });

        if (_customerSettings.AllowCustomersToUploadAvatars)
        {
            root.Append(new MenuItem
            {
                Id = "avatar",
                Text = T("Account.Avatar"),
                Icon = "avatar",
                ActionName = "Avatar",
                ControllerName = "Customer"
            });
        }

        // Add area = "" to all items in one go.
        foreach (var item in root.Children)
        {
            item.Value.RouteValues["area"] = string.Empty;
        }

        return Task.FromResult(root);
    }
}