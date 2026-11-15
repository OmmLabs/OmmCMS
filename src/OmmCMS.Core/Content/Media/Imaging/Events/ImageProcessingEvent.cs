using OmmCMS.Events;
using OmmCMS.Imaging;

namespace OmmCMS.Core.Content.Media.Imaging;

/// <summary>
/// Published before processing begins, but after the source has been loaded.
/// </summary>
public class ImageProcessingEvent : IEventMessage
{
    public ImageProcessingEvent(ProcessImageQuery query, IImage image)
    {
        Query = query;
        Image = image;
    }

    public ProcessImageQuery Query { get; private set; }
    public IImage Image { get; private set; }
}