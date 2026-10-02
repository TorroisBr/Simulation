using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class NpcOwnerCommitInvalidationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void MoneyAccountBoundaryAdmitsOnlyCommittedPositiveWrites()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(10f);
        bool admit = true;
        int admissionCalls = 0;
        int commitCalls = 0;
        Func<bool> admission = () => { admissionCalls++; return admit; };
        Action committed = () => commitCalls++;
        BindBoundary(account, admission, committed);

        Assert.That(account.TryCredit(0f), Is.True);
        Assert.That(account.TryDebit(0f), Is.True);
        Assert.That(account.TryDebit(11f), Is.False);
        Assert.That(account.Revision, Is.Zero);
        Assert.That(admissionCalls, Is.Zero);
        Assert.That(commitCalls, Is.Zero);

        admit = false;
        Assert.That(account.TryDebit(2f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(10f));
        Assert.That(account.Revision, Is.Zero);
        Assert.That(commitCalls, Is.Zero);

        admit = true;
        Assert.That(account.TryDebit(2f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(8f));
        Assert.That(account.Revision, Is.EqualTo(1));
        Assert.That(commitCalls, Is.EqualTo(1));
        Assert.That(UnbindBoundary(account, () => true, committed), Is.False);
        Assert.That(UnbindBoundary(account, admission, committed), Is.True);
    }

    [Test]
    public void InventoryTryAddReportsAdmissionAndKeepsLegacyWrapper()
    {
        InventoryRuntime inventory = new InventoryRuntime();
        ItemData first = SimulationTestFactory.CreateItem("owner-write-first");
        ItemData second = SimulationTestFactory.CreateItem("owner-write-second");
        bool admit = true;
        int admissionCalls = 0;
        int commitCalls = 0;
        Func<bool> admission = () => { admissionCalls++; return admit; };
        Action committed = () => commitCalls++;
        BindBoundary(inventory, admission, committed);

        Assert.That(inventory.TryAddItem(first, 1, 2f), Is.True);
        Assert.That(inventory.Revision, Is.EqualTo(1));
        Assert.That(inventory.GetAmount(first), Is.EqualTo(1));
        Assert.That(commitCalls, Is.EqualTo(1));

        admit = false;
        Assert.That(inventory.TryAddItem(second, 1), Is.False);
        Assert.That(inventory.TryAddItem(null, 1), Is.False);
        Assert.That(inventory.TryAddItem(second, 0), Is.False);
        Assert.That(inventory.RemoveItem(first, 2), Is.False);
        Assert.That(inventory.Revision, Is.EqualTo(1));
        Assert.That(inventory.GetAmount(second), Is.Zero);
        Assert.That(commitCalls, Is.EqualTo(1));

        admit = true;
        inventory.AddItem(first, 2, 4f);
        Assert.That(inventory.Revision, Is.EqualTo(2));
        Assert.That(inventory.GetAmount(first), Is.EqualTo(3));
        Assert.That(commitCalls, Is.EqualTo(2));

        admit = false;
        Assert.That(inventory.RemoveItem(first, 1), Is.False);
        Assert.That(inventory.GetAmount(first), Is.EqualTo(3));
        Assert.That(inventory.Revision, Is.EqualTo(2));
        Assert.That(commitCalls, Is.EqualTo(2));
        Assert.That(admissionCalls, Is.EqualTo(4), "Only otherwise valid owner writes reach pre-write admission.");
    }

    [Test]
    public void DirectNpcOwnerCommitsRefreshExactSectionsAndAdvanceEpoch()
    {
        NpcRuntime npc = new NpcRuntime("owner-commit-protocol", null, null, 5f);
        ItemData item = SimulationTestFactory.CreateItem("owner-commit-protocol-item");
        IOwnerSectionCensusProvider accountProvider = NpcMoneyAccountCensusProvider.CreateProviders(new[] { npc })[0];
        IOwnerSectionCensusProvider inventoryProvider = NpcInventoryCensusProvider.CreateProviders(new[] { npc })[0];
        ContinuationCensusProtocol protocol = CreateProtocol(accountProvider, inventoryProvider);
        string accountSection = accountProvider.GetCurrentCensus().SectionId;
        string inventorySection = inventoryProvider.GetCurrentCensus().SectionId;

        BindBoundary(npc.MoneyAccount,
            () => ValidateUnchanged(protocol, accountSection),
            () => NotifyOne(protocol, accountSection));
        BindBoundary(npc.Inventory,
            () => ValidateUnchanged(protocol, inventorySection),
            () => NotifyOne(protocol, inventorySection));

        Assert.That(npc.MoneyAccount.TryCredit(3f), Is.True);
        Assert.That(npc.Inventory.TryAddItem(item, 2, 4f), Is.True);
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.True,
            epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(2));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void OneInventoryWriteNotifiesEveryAliasedSectionInOneEpoch()
    {
        InventoryRuntime inventory = new InventoryRuntime();
        NpcRuntime firstNpc = new NpcRuntime("inventory-alias-first", null);
        NpcRuntime secondNpc = new NpcRuntime("inventory-alias-second", null);
        FieldInfo inventoryField = typeof(NpcRuntime).GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(inventoryField, Is.Not.Null);
        inventoryField.SetValue(firstNpc, inventory);
        inventoryField.SetValue(secondNpc, inventory);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcInventoryCensusProvider.CreateProviders(new[] { firstNpc, secondNpc });
        string firstSection = providers[0].GetCurrentCensus().SectionId;
        string secondSection = providers[1].GetCurrentCensus().SectionId;
        ContinuationCensusProtocol protocol = CreateProtocol(providers[0], providers[1]);
        string[] aliases = { firstSection, secondSection };
        BindBoundary(inventory,
            () => ValidateUnchanged(protocol, aliases),
            () =>
            {
                protocol.NotifyCommittedMutations(aliases, out _);
            });

        Assert.That(inventory.TryAddItem(SimulationTestFactory.CreateItem("alias-item"), 1), Is.True);
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.True,
            epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(1), "One physical Inventory commit advances one shared epoch despite two aliases.");
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void RuntimeBindingReconcilesInventoryAliasesAcrossRosterChanges()
    {
        NpcRuntime firstNpc = new NpcRuntime("runtime-inventory-alias-first", null);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, new[] { firstNpc }, economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        InventoryRuntime sharedInventory = firstNpc.Inventory;
        Assert.That(sharedInventory.TryAddItem(SimulationTestFactory.CreateItem("runtime-alias-before"), 1), Is.True);
        AssertRuntimeEpoch(runtime, 1);

        NpcRuntime secondNpc = new NpcRuntime("runtime-inventory-alias-second", null);
        typeof(NpcRuntime).GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(secondNpc, sharedInventory);
        Assert.That(runtime.TryRegisterNpc(secondNpc, out _), Is.True);
        AssertRuntimeEpoch(runtime, 2);

        Assert.That(sharedInventory.TryAddItem(SimulationTestFactory.CreateItem("runtime-alias-two-sections"), 1), Is.True);
        AssertRuntimeEpoch(runtime, 3);
        Assert.That(runtime.TryUnregisterNpc(secondNpc.RuntimeId, out _), Is.True);
        AssertRuntimeEpoch(runtime, 4);

        Assert.That(sharedInventory.TryAddItem(SimulationTestFactory.CreateItem("runtime-alias-one-section"), 1), Is.True);
        AssertRuntimeEpoch(runtime, 5);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void RuntimeBoundNpcTradeNotifiesEachLeafCommitOnce()
    {
        ItemData item = SimulationTestFactory.CreateItem("runtime-trade-commit", 10f);
        NpcRuntime buyer = new NpcRuntime("runtime-trade-buyer", SimulationTestFactory.CreateNpc("runtime-trade-buyer"), null, 20f);
        NpcRuntime seller = new NpcRuntime("runtime-trade-seller", SimulationTestFactory.CreateNpc("runtime-trade-seller"), null, 0f);
        seller.Inventory.AddItem(item, 1, 3f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, new[] { buyer, seller }, economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindRuntimeEconomyService(runtime, service);

        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 1, 10f);

        Assert.That(result.Success, Is.True, result.FailureReason.ToString());
        Assert.That(buyer.Money, Is.EqualTo(10f));
        Assert.That(seller.Money, Is.EqualTo(10f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(seller.Inventory.GetAmount(item), Is.Zero);
        AssertRuntimeEpoch(runtime, 4);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void RuntimeBoundOpenMarketPurchaseNotifiesNpcAndMarketOwnersOnce()
    {
        ItemData item = SimulationTestFactory.CreateItem("runtime-market-purchase", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "runtime-market-purchase-city",
            "runtime-market-purchase-location",
            SimulationTestFactory.CreateMarketItem(item, 3, 3));
        NpcRuntime buyer = new NpcRuntime("runtime-market-purchase-buyer", SimulationTestFactory.CreateNpc("runtime-market-purchase-buyer"), null, 20f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), new[] { city }, new[] { buyer }, economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindRuntimeEconomyService(runtime, service);

        EconomyTransactionResult result = service.TryExecuteMarketPurchase(buyer, city.Market, item, 1);

        Assert.That(result.Success, Is.True, result.FailureReason.ToString());
        Assert.That(buyer.Money, Is.EqualTo(10f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(2));
        AssertRuntimeEpoch(runtime, 3);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void RuntimeBoundOwnerRejectsWrongThreadBeforeCommit()
    {
        NpcRuntime npc = new NpcRuntime("runtime-owner-thread-npc", null, null, 5f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, new[] { npc }, economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        bool result = true;
        Thread thread = new Thread(() => result = npc.MoneyAccount.TryCredit(2f));

        thread.Start();
        Assert.That(thread.Join(TimeSpan.FromSeconds(5)), Is.True, "Wrong-thread owner write did not finish.");

        Assert.That(result, Is.False);
        Assert.That(npc.Money, Is.EqualTo(5f));
        Assert.That(npc.MoneyAccount.Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void AccountCommitAtEpochSaturationRemainsSuccessfulAndFaultsCensus()
    {
        NpcRuntime npc = new NpcRuntime("saturated-account-npc", null, null, 5f);
        MoneyAccountRuntime account = npc.MoneyAccount;
        IOwnerSectionCensusProvider provider = NpcMoneyAccountCensusProvider.CreateProviders(new[] { npc })[0];
        string section = provider.GetCurrentCensus().SectionId;
        ContinuationCensusProtocol protocol = CreateProtocol(provider);
        BindAccount(protocol, section, account);
        SetMutationEpoch(protocol, long.MaxValue);

        Assert.That(account.TryCredit(2f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(7f));
        Assert.That(account.Revision, Is.EqualTo(1));
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(long.MaxValue));
        AssertProtocolFaulted(protocol);
    }

    [Test]
    public void InventoryCommitAtEpochSaturationRemainsSuccessfulAndFaultsCensus()
    {
        NpcRuntime npc = new NpcRuntime("saturated-inventory-npc", null);
        InventoryRuntime inventory = npc.Inventory;
        IOwnerSectionCensusProvider provider = NpcInventoryCensusProvider.CreateProviders(new[] { npc })[0];
        string section = provider.GetCurrentCensus().SectionId;
        ContinuationCensusProtocol protocol = CreateProtocol(provider);
        BindInventory(protocol, section, inventory);
        SetMutationEpoch(protocol, long.MaxValue);
        ItemData item = SimulationTestFactory.CreateItem("saturated-inventory-item");

        Assert.That(inventory.TryAddItem(item, 1), Is.True);
        Assert.That(inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(inventory.Revision, Is.EqualTo(1));
        Assert.That(ReadMutationEpoch(protocol), Is.EqualTo(long.MaxValue));
        AssertProtocolFaulted(protocol);
    }

    [Test]
    public void NpcTradeRejectsFailedDestinationAddAfterEarlierWritesCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("trade-rejected-add", 10f);
        NpcRuntime buyer = new NpcRuntime("trade-rejected-add-buyer", SimulationTestFactory.CreateNpc("trade-rejected-add-buyer"), null, 20f);
        NpcRuntime seller = new NpcRuntime("trade-rejected-add-seller", SimulationTestFactory.CreateNpc("trade-rejected-add-seller"), null, 0f);
        seller.Inventory.AddItem(item, 1, 3f);
        BindBoundary(buyer.Inventory, () => false, () => { });

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteNpcTrade(buyer, seller, item, 1, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(10f));
        Assert.That(seller.Money, Is.EqualTo(10f));
        Assert.That(seller.Inventory.GetAmount(item), Is.Zero);
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void OpenMarketPurchaseRejectsFailedDestinationAddAfterMoneyAndStockCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("market-rejected-add", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "market-rejected-add-city",
            "market-rejected-add-location",
            SimulationTestFactory.CreateMarketItem(item, 3, 3));
        NpcRuntime buyer = new NpcRuntime("market-rejected-add-buyer", SimulationTestFactory.CreateNpc("market-rejected-add-buyer"), null, 20f);
        BindBoundary(buyer.Inventory, () => false, () => { });

        EconomyTransactionResult result = new EconomyTransactionService().TryExecuteMarketPurchase(buyer, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(10f));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(2));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void ExpeditionRetrievalRejectsFailedInventoryAddAfterPlaceStackRemoval()
    {
        SpatialTravelFixture world = new SpatialTravelFixture();
        ItemData item = SimulationTestFactory.CreateItem("expedition-rejected-add");
        NpcRuntime performer = world.CreateNpc("expedition-rejected-add-performer", world.CityA, 20f);
        performer.SetCurrentPresence(world.Site.Location);
        PlaceContentStore content = new PlaceContentStore(world.Records.Allocator, world.IdentityRegistry);
        PlaceContentOwnerReference owner = PlaceContentOwnerReference.ForExplorableSite(world.Site);
        Assert.That(content.TryAddStack(owner, item, 1, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        ExpeditionStore expeditions = new ExpeditionStore();
        TravelPartyStore parties = new TravelPartyStore();
        TravelPartySystem travelPartySystem = new TravelPartySystem(
            parties,
            world.Records.Allocator,
            world.IdentityRegistry,
            world.Travel,
            world.Records.Time,
            world.Records.Sequence,
            world.Records.EventRecorder);
        ExpeditionSystem system = new ExpeditionSystem(
            expeditions,
            world.Records.Allocator,
            world.IdentityRegistry,
            world.Sites,
            travelPartySystem,
            parties,
            world.Knowledge,
            world.Records.Time,
            world.Records.EventRecorder,
            placeContentStore: content);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-rejected-add",
            world.Site.RuntimeId,
            world.CityA.Location.RuntimeId,
            world.Site.Location.RuntimeId,
            world.SiteRoute.RuntimeId,
            "completed-travel-party",
            null,
            new[] { performer.RuntimeId },
            new[] { performer.RuntimeId },
            Array.Empty<string>(),
            ExpeditionState.Exploring,
            ExpeditionObjectiveRuntime.Retrieve(item.DefinitionId));
        Assert.That(expeditions.Add(expedition), Is.True);
        BindBoundary(performer.Inventory, () => false, () => { });

        Assert.That(system.TryRetrieveTargetResource(expedition, owner, item, 1, out string reason), Is.False);
        Assert.That(reason, Is.Not.Null.And.Not.Empty);
        Assert.That(content.TryGet(owner, out PlaceContentRuntime place), Is.True);
        Assert.That(place.GetAmount(item), Is.Zero, "The place-stack take already committed; no restoration is promised.");
        Assert.That(performer.Inventory.GetAmount(item), Is.Zero);
        Assert.That(expedition.IsObjectiveComplete, Is.False);
    }

    private static void BindAccount(ContinuationCensusProtocol protocol, string section, MoneyAccountRuntime account)
    {
        BindBoundary(account,
            () => ValidateUnchanged(protocol, section),
            () => NotifyOne(protocol, section));
    }

    private static void BindInventory(ContinuationCensusProtocol protocol, string section, InventoryRuntime inventory)
    {
        BindBoundary(inventory,
            () => ValidateUnchanged(protocol, section),
            () => NotifyOne(protocol, section));
    }

    private static bool ValidateUnchanged(ContinuationCensusProtocol protocol, params string[] sections)
    {
        MethodInfo method = typeof(ContinuationCensusProtocol).GetMethod(
            "TryValidateUnchangedSections", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { sections, null };
        return (bool)method.Invoke(protocol, arguments);
    }

    private static void NotifyOne(ContinuationCensusProtocol protocol, string section)
    {
        protocol.NotifyCommittedMutation(section, out _);
    }

    private static void BindBoundary(object owner, Func<bool> admission, Action committed)
    {
        MethodInfo method = owner.GetType().GetMethod("BindP12MutationBoundary", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, owner.GetType().Name);
        method.Invoke(owner, new object[] { admission, committed });
    }

    private static bool UnbindBoundary(object owner, Func<bool> admission, Action committed)
    {
        MethodInfo method = owner.GetType().GetMethod("UnbindP12MutationBoundary", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, owner.GetType().Name);
        return (bool)method.Invoke(owner, new object[] { admission, committed });
    }

    private static void BindRuntimeEconomyService(SimulationRuntime runtime, EconomyTransactionService service)
    {
        MethodInfo method = typeof(SimulationRuntime).GetMethod(
            "BindP12EconomyTransactionService", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(runtime, new object[] { service });
    }

    private static ContinuationCensusProtocol CreateProtocol(params IOwnerSectionCensusProvider[] providers)
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        for (int i = 0; i < providers.Length; i++)
        {
            OwnerSectionCensusWitness witness = providers[i].GetCurrentCensus();
            Assert.That(protocol.RegisterExpectedSection(
                new OwnerSectionContract(witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required), out _), Is.True);
            Assert.That(protocol.RegisterCensusProvider(witness.SectionId, providers[i], out _), Is.True);
        }

        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("test.owner-write", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure initial), Is.True,
            initial.ToString());
        return protocol;
    }

    private static void SetMutationEpoch(ContinuationCensusProtocol protocol, long value)
    {
        FieldInfo field = typeof(ContinuationCensusProtocol).GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(protocol, value);
    }

    private static long ReadMutationEpoch(ContinuationCensusProtocol protocol)
    {
        FieldInfo field = typeof(ContinuationCensusProtocol).GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (long)field.GetValue(protocol);
    }

    private static void AssertProtocolFaulted(ContinuationCensusProtocol protocol)
    {
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static void AssertRuntimeEpoch(SimulationRuntime runtime, long expectedEpoch)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.True,
            epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(expectedEpoch));
    }

}
