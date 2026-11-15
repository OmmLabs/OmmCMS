using OmmCMS.Core.Checkout.Orders;
using OmmCMS.Utilities.Html;

namespace OmmCMS;

public static partial class OrderNoteExtensions
{
    /// <summary>
    /// Formats the order note text.
    /// </summary>
    /// <param name="orderNote">Order note.</param>
    /// <returns>Formatted text.</returns>
    public static string FormatOrderNoteText(this OrderNote orderNote)
    {
        Guard.NotNull(orderNote);

        if (orderNote.Note.IsEmpty())
        {
            return string.Empty;
        }

        return HtmlUtility.ConvertPlainTextToHtml(orderNote.Note);
    }
}