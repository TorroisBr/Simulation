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
    public void InventoryPreparedReplacementRejectsInvalidAmountsCostsAndOverflowWithoutMutation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1InventoryOwner owner = new DailyV1InventoryOwner(definitions);
        Assert.That(owner.TryAddItem(item, 3, 2f), Is.True);
        long revision = owner.Revision;
        int amount = owner.GetAmount(item);
        float cost = owner.GetAverageUnitCost(item);

        Assert.That(owner.PrepareReplacement(item, -1, 0f, false, 0), Is.Null, "Negative removal is not a valid prepared operation.");
        Assert.That(owner.PrepareReplacement(item, amount + 1, 0f, false, 0), Is.Null, "Over-removal must reject instead of silently deleting the row.");
        Assert.That(owner.PrepareReplacement(item, 0, float.NaN, false, 0), Is.Null);
        Assert.That(owner.PrepareReplacement(item, 0, float.PositiveInfinity, true, 1), Is.Null);
        Assert.That(owner.PrepareReplacement(item, 0, -1f, true, 1), Is.Null);
        Assert.That(owner.PrepareReplacement(item, 0, 1f, true, -1), Is.Null);
        Assert.That(owner.PrepareReplacement(item, 0, float.MaxValue, true, 2), Is.Null,
            "Overflow in weighted cost calculation must not produce an installable row.");
        Assert.That(owner.PrepareReplacement(item, 0, 1f, true, int.MaxValue), Is.Null,
            "Addition overflow must be rejected before an installable state is produced.");

        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.GetAmount(item), Is.EqualTo(amount));
        Assert.That(owner.GetAverageUnitCost(item), Is.EqualTo(cost));
    }

    [Test]
    public void InventoryInstallRevalidatesPreparedRowsAndLeavesOwnerUnchangedOnRejection()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1InventoryOwner owner = new DailyV1InventoryOwner(definitions);
        Assert.That(owner.TryAddItem(item, 3, 2f), Is.True);
        long revision = owner.Revision;
        int amount = owner.GetAmount(item);

        DailyV1InventoryPreparedState negativeAmount = owner.PrepareReplacement(item, 1, 0f, false, 0);
        negativeAmount.Rows[0] = new DailyV1InventoryItemState(item, -1, 2f);
        Assert.That(owner.TryInstall(revision, negativeAmount), Is.False);
        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.GetAmount(item), Is.EqualTo(amount));

        DailyV1InventoryPreparedState invalidCost = owner.PrepareReplacement(item, 1, 0f, false, 0);
        invalidCost.Rows[0] = new DailyV1InventoryItemState(item, amount - 1, float.NaN);
        Assert.That(owner.TryInstall(revision, invalidCost), Is.False);
        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.GetAmount(item), Is.EqualTo(amount));
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

    [Test]
    public void MarketRejectsChangedPreparedRowsWithoutRevisionIncrement()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions);
        Assert.That(owner.TryInitializeFromCity(definitions.Cities["CAMPO_VERDE"]), Is.True);
        DailyV1MarketItemState[] original = owner.Items.ToArray();
        long revision = owner.Revision;
        DailyV1MarketItemState[] changed = original.Select(row =>
        {
            Assert.That(DailyV1MarketItemState.TryCreate(row.Item, row.Amount + 1,
                row.DesiredAmount, row.CurrentPrice, out DailyV1MarketItemState created), Is.True);
            return created;
        }).ToArray();
        Assert.That(owner.TryCreatePreparedState(changed, out DailyV1MarketPreparedState prepared), Is.True);

        Assert.That(owner.TryInstall(revision, 0, prepared), Is.False,
            "Changed authoritative stock must not install without a local revision invalidation.");
        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.Items.Select(row => row.Amount), Is.EqualTo(original.Select(row => row.Amount)));
        Assert.That(owner.TryInstall(revision, 1, prepared), Is.True,
            "The same prepared transaction remains valid when paired with its required revision increment.");
        Assert.That(owner.Revision, Is.EqualTo(revision + 1));
    }

    [Test]
    public void MarketTypedPreparedOperationsRetainNodeIdentityAndRejectInvalidChangesAtomically()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions);
        Assert.That(owner.TryInitializeFromCity(definitions.Cities["CAMPO_VERDE"]), Is.True);
        DailyV1MarketItemState original = owner.Items[0];
        long revision = owner.Revision;

        Assert.That(owner.TryCreatePreparedSnapshot(out DailyV1MarketPreparedState snapshot), Is.True);
        Assert.That(owner.TryGetPreparedItem(snapshot, original.Item, out DailyV1MarketItemState copied), Is.True);
        Assert.That(copied.Item, Is.SameAs(original.Item));
        Assert.That(owner.TryCountPreparedItems(snapshot, original.Item, out int count), Is.True);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(owner.TryGetPreparedItem(snapshot, otherDefinitions.Items[original.Item.DefinitionId], out _), Is.False,
            "A same-ID node from another definitions graph cannot address this prepared owner state.");

        Assert.That(owner.TryApplyPreparedStockDelta(snapshot, original.Item, -original.Amount - 1,
            original.DesiredAmount, out _, out _), Is.False, "Over-removal must reject without changing the owner.");
        Assert.That(owner.TryApplyPreparedStockDelta(snapshot, original.Item, int.MaxValue,
            original.DesiredAmount, out _, out _), Is.False, "Overflow must reject without changing the owner.");
        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.Items[0].Amount, Is.EqualTo(original.Amount));

        Assert.That(owner.TryApplyPreparedStockDelta(snapshot, original.Item, 7,
            original.DesiredAmount, out DailyV1MarketPreparedState replacement,
            out DailyV1MarketItemState changed), Is.True);
        Assert.That(changed.Item, Is.SameAs(original.Item));
        Assert.That(changed.Amount, Is.EqualTo(original.Amount + 7));
        Assert.That(owner.Items[0].Amount, Is.EqualTo(original.Amount), "Preparation cannot mutate live owner state.");
        Assert.That(owner.TryInstall(revision, 1, replacement), Is.True);
        Assert.That(owner.Items[0].Item, Is.SameAs(original.Item));
        Assert.That(owner.Items[0].Amount, Is.EqualTo(original.Amount + 7));
        Assert.That(owner.Revision, Is.EqualTo(revision + 1));

        Assert.That(DailyV1MarketItemState.TryCreate(original.Item, original.Amount,
            original.DesiredAmount, 12345f, out DailyV1MarketItemState stalePrice), Is.True);
        Assert.That(owner.TryCreatePreparedState(new[] { stalePrice }, out DailyV1MarketPreparedState staleSnapshot), Is.True);
        Assert.That(owner.TryPreparePriceRefresh(staleSnapshot,
            out DailyV1MarketPreparedState refreshed, out bool changedPrice), Is.True);
        Assert.That(changedPrice, Is.True);
        Assert.That(owner.TryGetPreparedItem(refreshed, original.Item, out DailyV1MarketItemState refreshedRow), Is.True);
        Assert.That(refreshedRow.CurrentPrice, Is.EqualTo(MarketPriceCalculator.CalculateCurrentPrice(
            original.Item.Values.basePrice, original.DesiredAmount, original.Amount)));
        Assert.That(owner.Revision, Is.EqualTo(revision + 1), "Price preparation is detached until installation.");
    }
}
