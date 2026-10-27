using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1RuntimeProtocolOperationContractsTests
{
    [TestCase(false, false, false, 1)]
    [TestCase(false, true, false, 1)]
    [TestCase(false, false, true, 1)]
    [TestCase(false, true, true, 1)]
    [TestCase(true, false, false, 21)]
    [TestCase(true, true, false, 23)]
    [TestCase(true, false, true, 22)]
    [TestCase(true, true, true, 24)]
    public void ExpectedOperationCatalogueHasStableOptionalCardinality(
        bool hasRuntimeAdmission,
        bool hasTravelPartySystem,
        bool hasTravelSystem,
        int expectedCount)
    {
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission,
                hasTravelPartySystem,
                hasTravelSystem);

        Assert.That(operationContractIds, Has.Count.EqualTo(expectedCount));
        var uniqueOperationContractIds = new HashSet<string>(operationContractIds);
        Assert.That(uniqueOperationContractIds, Has.Count.EqualTo(expectedCount),
            "Every enabled protocol operation must have one unique stable identity.");
    }

    [Test]
    public void SelectedRuntimeCataloguePreservesCanonicalIdentityAndRegistrationOrder()
    {
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: true);

        CollectionAssert.AreEqual(new[]
        {
            "runtime.npc-membership",
            "runtime.bootstrap-publication",
            "runtime.advance-day",
            "runtime.person.parentage",
            "runtime.travel-party.start",
            "runtime.travel-party.advance",
            "runtime.travel.start",
            "runtime.economy.npc-trade",
            "runtime.economy.money-transfer",
            "runtime.economy.market-purchase",
            "runtime.economy.market-sale",
            "runtime.merchant.advance-npc-trade-state",
            "p12.institution-office.owner-commit",
            "p12.faction.owner-commit",
            "p12.political-claim.owner-commit",
            "p12.political-support.owner-commit",
            "p12.property.owner-commit",
            "p12.estate.owner-commit",
            "runtime.population.immigration",
            "runtime.population.emigration",
            "runtime.population.resident-death",
            "runtime.population.residence-migration",
            "runtime.person.death",
            "runtime.person.residence-bind"
        }, operationContractIds);
    }

    [Test]
    public void RuntimeWithoutAdmissionKeepsOnlyNpcMembershipProtocol()
    {
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: false,
                hasTravelPartySystem: true,
                hasTravelSystem: true);

        CollectionAssert.AreEqual(
            new[] { "runtime.npc-membership" }, operationContractIds);
    }
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void CoreExecutionBoundarySealsAndEnforcesItsCompleteOperationInventory(
        bool hasTravelPartySystem,
        bool hasTravelSystem)
    {
        DailyV1ExecutionBoundaryState state = CreateAdmittedExecutionState();
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                true, hasTravelPartySystem, hasTravelSystem);

        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True,
                "Core must accept each unique operation from its selected catalogue.");

        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
        Assert.That(state.HasRuntimeProtocolOperationInventory, Is.True);
        Assert.That(state.HasRuntimeProtocolOperationContract(operationContractIds[0]), Is.True);
        Assert.That(state.HasRuntimeProtocolOperationContract("host.invented-operation"), Is.False);
        Assert.That(state.TryBeginRuntimeProtocolOperation("host.invented-operation"), Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);

        Assert.That(state.TryBeginRuntimeProtocolOperation(operationContractIds[0]), Is.True);
        Assert.That(state.ActiveRuntimeOperationCount, Is.EqualTo(1));
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.False,
            "A Core-active operation must prevent a capture from appearing quiescent.");
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
    }

    [Test]
    public void CoreOperationInventoryRejectsDuplicatesAndIncompleteOrMixedSets()
    {
        DailyV1ExecutionBoundaryState duplicateState = CreateAdmittedExecutionState();
        string first = DailyV1RuntimeProtocolOperationContracts.NpcMembership;
        Assert.That(duplicateState.TryRegisterRuntimeProtocolOperationContract(first), Is.True);
        Assert.That(duplicateState.TryRegisterRuntimeProtocolOperationContract(first), Is.False);
        Assert.That(duplicateState.TrySealRuntimeProtocolOperationInventory(), Is.False);

        DailyV1ExecutionBoundaryState incompleteState = CreateAdmittedExecutionState();
        IReadOnlyList<string> baseIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                true, false, false);
        foreach (string operationContractId in baseIds)
            Assert.That(incompleteState.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True);
        Assert.That(incompleteState.TryRegisterRuntimeProtocolOperationContract(
            DailyV1RuntimeProtocolOperationContracts.TravelPartyStart), Is.True);
        Assert.That(incompleteState.TrySealRuntimeProtocolOperationInventory(), Is.False,
            "A partial optional family must not seal as a supported selected operation set.");
        Assert.That(incompleteState.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.False,
            "An unsealed operation inventory cannot admit a runtime operation.");
    }

    [Test]
    public void CoreOwnerSectionInventoryIsSortedSealedAndFailClosed()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();

        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.False,
            "An empty owner-section declaration cannot be sealed.");
        Assert.That(state.TryRegisterExpectedOwnerSectionContract(
            "owner.z", 2, OwnerSectionRole.Required), Is.True);
        Assert.That(state.TryRegisterExpectedOwnerSectionContract(
            "owner.a", 1, OwnerSectionRole.ExplicitlyEmpty), Is.True);
        Assert.That(state.TryRegisterExpectedOwnerSectionContract(
            "owner.a", 1, OwnerSectionRole.ExplicitlyEmpty), Is.False);

        IReadOnlyList<DailyV1OwnerSectionContractIdentity> contracts =
            state.GetExpectedOwnerSectionContractInventory(out long revision);
        Assert.That(contracts, Has.Count.EqualTo(2));
        Assert.That(contracts[0].SectionId, Is.EqualTo("owner.a"));
        Assert.That(contracts[1].SectionId, Is.EqualTo("owner.z"));
        Assert.That(contracts[0].SchemaVersion, Is.EqualTo(1));
        Assert.That(contracts[0].Role, Is.EqualTo(OwnerSectionRole.ExplicitlyEmpty));

        Assert.That(state.TryReplaceExpectedOwnerSectionContractInventoryBeforeSeal(
            new[]
            {
                new DailyV1OwnerSectionContractIdentity("owner.replacement", 1, OwnerSectionRole.Required),
                new DailyV1OwnerSectionContractIdentity("owner.replacement", 1, OwnerSectionRole.Required)
            }), Is.False, "Duplicate staged identities must reject without replacing the prior inventory.");
        Assert.That(state.ExpectedOwnerSectionContractInventoryRevision, Is.EqualTo(revision));
        IReadOnlyList<DailyV1OwnerSectionContractIdentity> unchanged =
            state.GetExpectedOwnerSectionContractInventory(out _);
        Assert.That(unchanged, Has.Count.EqualTo(2));
        Assert.That(unchanged[0].SectionId, Is.EqualTo("owner.a"));
        Assert.That(unchanged[1].SectionId, Is.EqualTo("owner.z"));

        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        Assert.That(state.ExpectedOwnerSectionContractInventorySealed, Is.True);
        Assert.That(state.TryRegisterExpectedOwnerSectionContract(
            "owner.late", 1, OwnerSectionRole.Required), Is.False);
        Assert.That(state.TryReplaceExpectedOwnerSectionContractInventoryBeforeSeal(
            new[] { new DailyV1OwnerSectionContractIdentity("owner.late", 1, OwnerSectionRole.Required) }),
            Is.False);
    }

    [Test]
    public void CoreSealedOwnerSectionInventoryChangesOnlyInsideActiveRuntimeOperation()
    {
        DailyV1ExecutionBoundaryState state = CreateAdmittedExecutionState();
        IReadOnlyList<string> operationIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true, hasTravelPartySystem: false, hasTravelSystem: false);
        foreach (string operationId in operationIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationId), Is.True);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);

        Assert.That(state.TryRegisterExpectedOwnerSectionContract(
            "owner.initial", 1, OwnerSectionRole.Required), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        DailyV1OwnerSectionContractIdentity[] transitioned =
        {
            new DailyV1OwnerSectionContractIdentity("owner.initial", 1, OwnerSectionRole.Required),
            new DailyV1OwnerSectionContractIdentity("npc.knowledge.dynamic", 1, OwnerSectionRole.Required)
        };

        Assert.That(state.TryReplaceExpectedOwnerSectionContractInventoryDuringRuntimeOperation(
            transitioned), Is.False, "A roster census cannot change its section identities while idle.");
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.TryReplaceExpectedOwnerSectionContractInventoryDuringRuntimeOperation(
            transitioned), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        IReadOnlyList<DailyV1OwnerSectionContractIdentity> contracts =
            state.GetExpectedOwnerSectionContractInventory(out _);
        Assert.That(contracts, Has.Count.EqualTo(2));
        Assert.That(contracts[0].SectionId, Is.EqualTo("npc.knowledge.dynamic"));
        Assert.That(state.ExpectedOwnerSectionContractInventorySealed, Is.True);
    }
    private static DailyV1ExecutionBoundaryState CreateAdmittedExecutionState()
    {
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            new SimulationTime(0L), admission,
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True);
        return state;
    }
}
