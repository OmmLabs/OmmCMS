using OmmCMS.Collections;

namespace OmmCMS.Core.Content.Menus;

public abstract class AdminMenuProvider : IMenuProvider
{
    public void BuildMenu(TreeNode<MenuItem> rootNode)
    {
        var modulesNode = rootNode.SelectNodeById("modules");
        BuildMenuCore(modulesNode);
    }

    protected abstract void BuildMenuCore(TreeNode<MenuItem> modulesNode);

    public string MenuName => "admin";

    public virtual int Ordinal => 0;
}