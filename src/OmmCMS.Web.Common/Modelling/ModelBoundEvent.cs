using Microsoft.AspNetCore.Http;
using OmmCMS.Core.Stores;
using OmmCMS.Events;
using OmmCMS.Web.Controllers;

namespace OmmCMS.Web.Modelling;

public class ModelBoundEvent : IEventMessage, IStoreScoped
{
    public ModelBoundEvent(TabbableModel boundModel, object entityModel, ManageController controller)
        : this(boundModel, entityModel, controller.Request.Form, controller.GetActiveStoreScopeConfiguration())
    {
    }

    public ModelBoundEvent(TabbableModel boundModel, object entityModel, IFormCollection form, int storeScope = 0)
    {
        BoundModel = boundModel;
        EntityModel = entityModel;
        Form = form;
        StoreScope = storeScope;
    }

    public TabbableModel BoundModel { get; }
    public object EntityModel { get; }
    public IFormCollection Form { get; }
    public int StoreScope { get; }
}