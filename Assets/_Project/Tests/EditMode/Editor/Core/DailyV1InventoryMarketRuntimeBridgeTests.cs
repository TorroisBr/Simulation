using NUnit.Framework;

[TestFixture]
public sealed class DailyV1InventoryMarketRuntimeBridgeTests
{
    [Test]
    public void InventoryWrapperUsesTheExistingTypedOwnerWithoutAuthoringBindings()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1InventoryOwner owner = new DailyV1InventoryOwner(definitions);
        InventoryRuntime runtime = new InventoryRuntime(owner);
        int committed = 0;

        Assert.That(runtime.SelectedDailyV1Owner, Is.SameAs(owner));
        runtime.BindP12MutationBoundary(() => true, () => committed++);
        Assert.That(runtime.CanAddSelectedItem(item, 2, 4f), Is.True);
        Assert.That(runtime.TryAddSelectedItem(item, 2, 4f), Is.True);
        Assert.That(runtime.GetSelectedItem(item).Item, Is.SameAs(item));
        Assert.That(runtime.GetSelectedAmount(item), Is.EqualTo(2));
        Assert.That(runtime.GetSelectedAverageUnitCost(item), Is.EqualTo(4f));
        Assert.That(runtime.Revision, Is.EqualTo(1));
        Assert.That(committed, Is.EqualTo(1));

        PreparedInventoryState replacement = runtime.PrepareSelectedReplacement(item, 1, 0f, false, 0);
        Assert.That(replacement.SelectedState.Rows[0].Item, Is.SameAs(item));
        Assert.That(runtime.CanInstall(runtime.Revision), Is.True);
        runtime.InstallPrepared(runtime.Revision, replacement);
        Assert.That(runtime.GetSelectedAmount(item), Is.EqualTo(1));
        Assert.That(runtime.Revision, Is.EqualTo(2));
        Assert.That(committed, Is.EqualTo(1), "The prepared transaction caller owns its commit notification.");
    }

    [Test]
    public void MarketWrapperCanConsumeAnExistingOwnerAndBuildFromTypedCityWithoutBindings()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities["CAMPO_VERDE"];
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        MarketCounterpartyRuntime counterparty = MarketCounterpartyRuntime.CreateOpen();
        DailyV1MarketOwner owner = new DailyV1MarketOwner(definitions, counterparty);
        Assert.That(owner.TryInitializeFromCity(city), Is.True);
        MarketRuntime runtime = new MarketRuntime(owner);
        int committed = 0;

        Assert.That(runtime.SelectedDailyV1Owner, Is.SameAs(owner));
        Assert.That(runtime.Counterparty, Is.SameAs(counterparty));
        Assert.That(owner.Items[0].Item, Is.SameAs(definitions.Items[owner.Items[0].Item.DefinitionId]));
        Assert.DoesNotThrow(() => runtime.BindP12MutationBoundary(() => true, () => committed++));

        int before = owner.GetAmount(item);
        long revision = runtime.Revision;
        Assert.That(runtime.AddSelectedStock(item, 2), Is.EqualTo(2));
        Assert.That(runtime.GetSelectedAmount(item), Is.EqualTo(before + 2));
        Assert.That(runtime.Revision, Is.EqualTo(revision + 1));
        Assert.That(committed, Is.EqualTo(1));

        PreparedMarketState snapshot = runtime.CreatePreparedSnapshot();
        Assert.That(runtime.TryGetPreparedItem(snapshot, item, out DailyV1MarketItemState prepared), Is.True);
        Assert.That(prepared.Item, Is.SameAs(item));
        Assert.That(runtime.TryApplyPreparedStockDelta(snapshot, item, 1, prepared.DesiredAmount,
            out PreparedMarketState next, out DailyV1MarketItemState updated), Is.True);
        Assert.That(updated.Item, Is.SameAs(item));
        Assert.That(runtime.CanInstallMaterialFlow(runtime.Revision, 1), Is.True);
        runtime.InstallPrepared(runtime.Revision, 1, next);
        runtime.NotifyMaterialFlowInstalled();
        Assert.That(runtime.GetSelectedAmount(item), Is.EqualTo(before + 3));
        Assert.That(runtime.Revision, Is.EqualTo(revision + 2));
        Assert.That(committed, Is.EqualTo(2), "Prepared owner replacement notifies exactly once after installation.");

        MarketRuntime cityRuntime = new MarketRuntime(city, definitions, counterparty);
        Assert.That(cityRuntime.SelectedDailyV1Owner, Is.Not.Null);
        Assert.That(cityRuntime.SelectedDailyV1Owner.Definitions, Is.SameAs(definitions));
        Assert.That(cityRuntime.SelectedDailyV1Owner.Items[0].Item,
            Is.SameAs(definitions.Items[cityRuntime.SelectedDailyV1Owner.Items[0].Item.DefinitionId]));
    }
}
