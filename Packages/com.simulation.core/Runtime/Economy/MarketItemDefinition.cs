/// <summary>The effective, stable item values used by the finite-source Market operation.</summary>
public sealed class MarketItemDefinition
{
    public string DefinitionId { get; }
    public float BasePrice { get; }

    public MarketItemDefinition(string definitionId, float basePrice)
    {
        DefinitionId = definitionId;
        BasePrice = basePrice;
    }
}
