using OmmCMS.Engine.Modularity;

namespace OmmCMS.Events;

public class EventConsumerMetadata
{
    public Type ContainerType { get; set; }
    public IModuleDescriptor ModuleDescriptor { get; set; }
}