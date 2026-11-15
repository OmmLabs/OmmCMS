namespace OmmCMS.Core.Catalog.Attributes;

/// <summary>
/// Represents a product variant attribute selection.
/// </summary>
/// <remarks>This class can parse strings of XML or JSON format.</remarks>
public class ProductVariantAttributeSelection : AttributeSelection
{
    /// <summary>
    /// Creates product variant attribute selection from string. 
    /// Use <see cref="AttributeSelection.AttributesMap"/> to access parsed attributes afterwards.
    /// </summary>
    /// <remarks>Automatically differentiates between XML and JSON.</remarks>
    /// <param name="rawAttributes">XML or JSON attributes string.</param>  
    public ProductVariantAttributeSelection(string rawAttributes)
        : base(rawAttributes, "ProductVariantAttribute")
    {
    }

}
