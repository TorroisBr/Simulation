using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1InventoryMarketOwnerTests
{
    [Test]
    public void InventoryRetainsExactGraphNodesAndPublishesSuccessfulMutations()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1InventoryOwner owner = new DailyV1InventoryOwner(definitions);
        int committed = 0;

        Assert.That(owner.TryBindMutationBoundary(() => true, () => committed++), Is.True);
        Assert.That(owner.TryAddItem(item, 4, 3f), Is.True);
        Assert.That(owner.Items, Has.Count.EqualTo(1));
        Assert.That(owner.Items[0].Item, Is.SameAs(item));
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(committed, Is.EqualTo(1));
        Assert.That(owner.TryAddItem(otherDefinitions.Items["FERRO"], 1), Is.False,
            "Equal definition IDs from another effective graph are not interchangeable owner identities.");

        DailyV1InventoryPreparedState prepared = owner.PrepareReplacement(item, 1, 5f, true, 2);
        Assert.That(prepared, Is.Not.Null);
        Assert.That(owner.TryInstall(1, prepared), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(2));
        Assert.That(owner.Items[0].Item, Is.SameAs(item));
        Assert.That(owner.Items[0].Amount, Is.EqualTo(5));
        Assert.That(owner.Items[0].AverageUnitCost, Is.EqualTo(4.4f).Within(0.0001f));
        Assert.That(committed, Is.EqualTo(1), "Prepared installs preserve the caller-owned notification boundary.");
    }

    [Test]
    public void MarketInitializationAndPreparedInstallRetainCanonicalItemIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities["CAMPO_VERDE"];
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions);
        int committed = 0;

        Assert.That(owner.TryInitializeFromCity(city), Is.True);
        Assert.That(owner.Items, Has.Count.EqualTo(city.Values.marketItems.Count));
        Assert.That(owner.Items.Single(row => row.Item.DefinitionId == item.DefinitionId).Item, Is.SameAs(item));
        Assert.That(owner.TryBindMutationBoundary(() => true, () => committed++), Is.True);

        int initialAmount = owner.GetAmount(item);
        long initialRevision = owner.Revision;
        Assert.That(owner.AddStock(item, 3), Is.EqualTo(3));
        Assert.That(owner.GetAmount(item), Is.EqualTo(initialAmount + 3));
        Assert.That(owner.Revision, Is.EqualTo(initialRevision + 1));
        Assert.That(committed, Is.EqualTo(1));

        DailyV1MarketPreparedState prepared = owner.PrepareReplacement(item, 2, 180);
        Assert.That(prepared, Is.Not.Null);
        Assert.That(owner.TryInstall(owner.Revision, 1, prepared), Is.True);
        Assert.That(owner.Items.Single(row => row.Item.DefinitionId == item.DefinitionId).Item, Is.SameAs(item));
        Assert.That(owner.Revision, Is.EqualTo(initialRevision + 2));
        Assert.That(committed, Is.EqualTo(1), "Prepared material-flow installation notifies only after its caller commits.");
    }

    [Test]
    public void MarketPreparedSnapshotPreservesTypedRowsOrderAndExactGraphIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions);
        Assert.That(owner.TryInitializeFromCity(definitions.Cities["CAMPO_VERDE"]), Is.True);
        DailyV1MarketItemState[] originalRows = owner.Items.ToArray();
        DailyV1MarketItemState[] replacementRows = originalRows.Select((row, index) =>
        {
            Assert.That(DailyV1MarketItemState.TryCreate(row.Item, row.Amount + index + 1,
                row.DesiredAmount, row.CurrentPrice, out DailyV1MarketItemState created), Is.True);
            return created;
        }).ToArray();
        long revision = owner.Revision;

        Assert.That(owner.TryCreatePreparedState(replacementRows, out DailyV1MarketPreparedState prepared), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(revision), "Preparing a replacement cannot mutate the authoritative owner.");
        Assert.That(owner.TryInstall(revision, 1, prepared), Is.True);
        Assert.That(owner.Items.Select(row => row.Item.DefinitionId),
            Is.EqualTo(originalRows.Select(row => row.Item.DefinitionId)),
            "Prepared installation must preserve the canonical selected row order.");
        for (int i = 0; i < replacementRows.Length; i++)
        {
            Assert.That(owner.Items[i].Item, Is.SameAs(originalRows[i].Item));
            Assert.That(owner.Items[i].Amount, Is.EqualTo(replacementRows[i].Amount));
        }
        Assert.That(owner.Revision, Is.EqualTo(revision + 1));

        DailyV1MarketItemState foreignRow = replacementRows[0];
        Assert.That(DailyV1MarketItemState.TryCreate(
            otherDefinitions.Items[foreignRow.Item.DefinitionId], foreignRow.Amount,
            foreignRow.DesiredAmount, foreignRow.CurrentPrice, out DailyV1MarketItemState foreignCandidate), Is.True);
        Assert.That(owner.TryCreatePreparedState(new[] { foreignCandidate }, out _), Is.False,
            "A foreign graph node with the same stable ID cannot be installed into this owner.");
        Assert.That(owner.Revision, Is.EqualTo(revision + 1));
    }
}
