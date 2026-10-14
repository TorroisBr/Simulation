using NUnit.Framework;

public sealed class MarketPriceUnityBaselineTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void UnchangedP14MarketPriceExpressionMatchesApprovedBinary32Vectors()
    {
        MarketPriceParityVector[] vectors = MarketPriceParityVectors.All;
        for (int i = 0; i < vectors.Length; i++)
        {
            MarketPriceParityVector vector = vectors[i];
            ItemData item = SimulationTestFactory.CreateItem("corec-price-baseline-" + i);
            item.basePrice = MarketPriceParityVectors.ToSingle(vector.BasePriceBits);

            MarketItemRuntime row = new MarketItemRuntime(item, vector.Amount, vector.DesiredAmount);

            Assert.That(MarketPriceParityVectors.ToBits(row.CurrentPrice), Is.EqualTo(vector.ExpectedPriceBits),
                "Unity price vector " + i + " expected 0x" + vector.ExpectedPriceBits.ToString("X8")
                + " but received 0x" + MarketPriceParityVectors.ToBits(row.CurrentPrice).ToString("X8"));
            Assert.That(MarketPriceParityVectors.ToBits(MarketPriceCalculator.CalculateCurrentPrice(
                    item.basePrice, vector.DesiredAmount, vector.Amount)),
                Is.EqualTo(vector.ExpectedPriceBits), "Core price vector " + i);
        }
    }
}
