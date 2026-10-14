/// <summary>The single engine-free implementation of the current Market row price formula.</summary>
public static class MarketPriceCalculator
{
    public static float CalculateCurrentPrice(float basePrice, int desiredAmount, int amount)
    {
        // Preserve Mathf.Max(0.01f, basePrice)'s operand order, including its NaN behavior.
        float effectiveBasePrice = 0.01f > basePrice ? 0.01f : basePrice;
        int stockForRatio = amount > 1 ? amount : 1;
        float multiplier = desiredAmount / (float)stockForRatio;
        if (multiplier < 0.5f) multiplier = 0.5f;
        else if (multiplier > 3f) multiplier = 3f;
        return effectiveBasePrice * multiplier;
    }
}
