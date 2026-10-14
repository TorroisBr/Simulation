using NUnit.Framework;

public sealed class MarketPriceCalculatorParityTests
{
    [Test]
    public void CoreCalculatorMatchesPinnedUnityBinary32Vectors()
    {
        foreach (MarketPriceParityVector vector in MarketPriceParityVectors.All)
        {
            float basePrice = MarketPriceParityVectors.ToSingle(vector.BasePriceBits);
            float actual = MarketPriceCalculator.CalculateCurrentPrice(
                basePrice, vector.DesiredAmount, vector.Amount);
            Assert.That(MarketPriceParityVectors.ToBits(actual), Is.EqualTo(vector.ExpectedPriceBits),
                "Core price vector expected 0x" + vector.ExpectedPriceBits.ToString("X8")
                + " but received 0x" + MarketPriceParityVectors.ToBits(actual).ToString("X8"));
        }
    }
}
