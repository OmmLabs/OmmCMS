using OmmCMS.Core.Catalog;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Events;

namespace OmmCMS.Web.Infrastructure;

public class MainMenuShrinker : IConsumer
{
    public void HandleEvent(MenuBuiltEvent message,
        CatalogSettings catalogSettings)
    {
        if (message.Name != "Main" || catalogSettings.MaxItemsToDisplayInCatalogMenu.GetValueOrDefault() < 1)
        {
            return;
        }

        message.Root.Children
            .Where(x => x.Value.Id != "brand")
            .Skip(catalogSettings.MaxItemsToDisplayInCatalogMenu.Value)
            .Each(x => x.SetMetadata("spare", true));
    }
}