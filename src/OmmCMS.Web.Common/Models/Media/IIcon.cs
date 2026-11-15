#nullable enable

using OmmCMS.IconKit;

namespace OmmCMS.Web.Models.Media;

/// <summary>Exposes an icon address together with its presentation.</summary>
public interface IIcon
{
    /// <summary>Gets the icon expression; null represents no icon.</summary>
    IconExpression? Icon { get; }
}
