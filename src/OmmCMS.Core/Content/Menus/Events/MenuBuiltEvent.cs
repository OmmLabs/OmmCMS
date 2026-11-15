using OmmCMS.Collections;
using OmmCMS.Events;

namespace OmmCMS.Core.Content.Menus;

public class MenuBuiltEvent : IEventMessage
{
    public MenuBuiltEvent(string name, TreeNode<MenuItem> root)
    {
        Name = name;
        Root = root;
    }

    public string Name { get; }
    public TreeNode<MenuItem> Root { get; }
}