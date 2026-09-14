using System;
using NUnit.Framework;

public sealed class EconomyResultSemanticsTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void CityProductionResultRejectsNonPositiveQuantity(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CityProductionResult("city", "item", quantity, "city"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void CityProductionResultRejectsEmptySettlementRuntimeId(string settlementRuntimeId)
    {
        Assert.Throws<ArgumentException>(() =>
            new CityProductionResult(settlementRuntimeId, "item", 1, "city"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void CityProductionResultRejectsEmptyStockOwnerRuntimeId(string stockOwnerRuntimeId)
    {
        Assert.Throws<ArgumentException>(() =>
            new CityProductionResult("city", "item", 1, stockOwnerRuntimeId));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void CityProductionResultRejectsEmptyItemDefinitionId(string itemDefinitionId)
    {
        Assert.Throws<ArgumentException>(() =>
            new CityProductionResult("city", itemDefinitionId, 1, "city"));
    }
}
