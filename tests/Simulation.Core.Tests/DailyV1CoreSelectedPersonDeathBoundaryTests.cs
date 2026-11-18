using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1CoreSelectedPersonDeathBoundaryTests
{
    private sealed class CensusProvider : IOwnerSectionCensusProvider
    {
        private readonly Func<long> revision;
        private readonly Func<int> cardinality;

        internal CensusProvider(OwnerSectionCensusSnapshot snapshot)
            : this(snapshot, () => snapshot.Revision, () => snapshot.Cardinality) { }

        internal CensusProvider(
            OwnerSectionCensusSnapshot snapshot,
            Func<long> revision,
            Func<int> cardinality)
        {
            SectionId = snapshot.SectionId;
            SchemaVersion = snapshot.SchemaVersion;
            OwnerInstanceIdentity = snapshot.OwnerInstanceIdentity;
            this.revision = revision;
            this.cardinality = cardinality;
        }

        internal string SectionId { get; }
        internal int SchemaVersion { get; }
        internal object OwnerInstanceIdentity { get; }

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(
                SectionId, SchemaVersion, OwnerInstanceIdentity,
                cardinality(), revision());
    }

    private sealed class PersonDeathProjection : IDailyV1SelectedPersonDeathProjectionBoundary
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        internal int PublishedDeaths;
        internal int Faults;
        internal int WorldRevisionAdvances;

        internal PersonDeathProjection(DailyV1SelectedRuntimeOwnerGraph graph) =>
            this.graph = graph;

        public bool TryValidatePersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            out bool currentActionChanged,
            out string failure)
        {
            currentActionChanged = false;
            failure = "Projection owner mismatch.";
            if (plan?.MaterializedNpc != null
                && (!graph.ContainsExactNpcOwner(plan.MaterializedNpc)
                    || !graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                        plan.MaterializedNpc.RuntimeId, out PersonRuntime person)
                    || !ReferenceEquals(person, plan.Person)))
                return false;
            currentActionChanged = plan?.MaterializedNpc?.ActionSlot?.CurrentAction != null;
            failure = null;
            return plan != null
                && graph.PersonStore.TryGet(plan.Person.PersonId, out PersonRuntime registered)
                && ReferenceEquals(registered, plan.Person)
                && currentActionChanged == (plan.ExpectedAction != null);
        }

        public bool TryPublishPersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged)
        {
            if (plan?.MaterializedNpc != null
                && (plan.MaterializedNpc.Lifecycle.LifeState != NpcLifeState.Dead
                    || plan.MaterializedNpc.ActionSlot.CurrentAction != null
                    || plan.MaterializedNpc.ActionSlot.CurrentPayload != null))
                return false;
            PublishedDeaths++;
            return true;
        }

        public void FaultPersonDeathRuntime() => Faults++;
        public void AdvancePersonDeathWorldRevision() => WorldRevisionAdvances++;
    }

    [Test]
    public void SelectedPersonDeathUsesCoreOperationAdmissionAndPublishesOneEpoch()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, admission,
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition.ExecutionBoundaryState, definitions, 0L,
            out DailyV1ActiveRuntimeSession session, out diagnostic), Is.True, diagnostic);

        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        PersonRuntime person = new PersonRuntime(
            new PersonId("core-owned-person-death-boundary"), state.SimulationTime.AbsoluteDay);
        Assert.That(graph.PersonStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, graph.PersonStore, person, state.SimulationTime.AbsoluteDay,
            out DailyV1SelectedPersonDeathPlan plan, out diagnostic), Is.True, diagnostic);
        Assert.That(plan.MaterializedNpc, Is.Null,
            "This regression covers the Core-owned Person row without a Unity NPC projection.");

        BindCompleteCoreCensus(state, session, plan);
        PersonDeathProjection projection = new PersonDeathProjection(graph);
        DailyV1CoreSelectedPersonDeathBoundary boundary =
            new DailyV1CoreSelectedPersonDeathBoundary(state, graph, projection);
        long priorEpoch = state.MutationEpochState.CurrentEpoch;

        Assert.That(boundary.TryValidatePersonDeathProjection(
            plan, out bool actionChanged, out diagnostic), Is.True, diagnostic);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryCreateP12SectionIds(
            plan, out IReadOnlyList<string> expectedSections), Is.True);
        Assert.That(state.HasAdmittedSelectedDailyExecution, Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);
        DailyV1CoreSelectedOwnerSectionMutationProtocol coreProtocol =
            new DailyV1CoreSelectedOwnerSectionMutationProtocol(state, graph);
        Assert.That(coreProtocol.IsBoundTo(state, graph), Is.True);
        Assert.That(coreProtocol.TryValidateUnchangedSections(expectedSections), Is.True,
            "The exact Person-death rows must match the sealed Core census before operation admission.");
        Assert.That(boundary.TryBeginPersonDeathOperation(
            plan, actionChanged, DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            expectedSections, () => true,
            out IDailyV1SelectedPersonDeathP12Scope preflightScope), Is.True,
            "Core P12 admission failed; protocolFault=" + state.IsSelectedRuntimeCensusProtocolFaulted);
        preflightScope.Complete(false);
        preflightScope.Dispose();

        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan, boundary, out diagnostic), Is.True, diagnostic);

        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(plan.AbsoluteDay));
        Assert.That(projection.PublishedDeaths, Is.Zero,
            "An unmaterialized Core Person has no Unity NPC projection to publish.");
        Assert.That(projection.WorldRevisionAdvances, Is.EqualTo(1));
        Assert.That(projection.Faults, Is.Zero);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch + 1L));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long capturedEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(capturedEpoch, Is.EqualTo(priorEpoch + 1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    private static void BindCompleteCoreCensus(
        DailyV1ExecutionBoundaryState state,
        DailyV1ActiveRuntimeSession session,
        DailyV1SelectedPersonDeathPlan plan)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        foreach (string operationId in DailyV1RuntimeProtocolOperationContracts
                     .GetExpectedOperationContractIds(
                         hasRuntimeAdmission: true,
                         hasTravelPartySystem: false,
                         hasTravelSystem: false))
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationId), Is.True, operationId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);

        OwnerSectionCensusSnapshot[] snapshots =
            DailyV1ApplicationSessionGateTests.CreateCoreBoundCompleteOwnerSectionVector(session);
        List<OwnerSectionCensusSnapshot> composed = new List<OwnerSectionCensusSnapshot>(snapshots);
        HashSet<string> personSectionIds = new HashSet<string>(
            snapshots.Where(section => section.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix,
                    StringComparison.Ordinal))
                .Select(section => section.SectionId), StringComparer.Ordinal);
        foreach (PersonRuntime corePerson in graph.PersonStore.Persons)
        {
            OwnerSectionCensusWitness personWitness = corePerson.GetLifeResidenceCensusWitness();
            Assert.That(personWitness, Is.Not.Null);
            if (!personSectionIds.Add(personWitness.SectionId)) continue;
            composed.Add(new OwnerSectionCensusSnapshot(
                personWitness.SectionId, personWitness.SchemaVersion,
                OwnerSectionRole.Required, personWitness.OwnerInstanceIdentity,
                personWitness.Cardinality, personWitness.Revision));
        }
        snapshots = composed.ToArray();
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCorePersonLifeResidenceSections(
            graph, snapshots, out IReadOnlyList<OwnerSectionCensusSnapshot> reboundPersonSections,
            out string censusDiagnostic), Is.True, censusDiagnostic);
        snapshots = reboundPersonSections.ToArray();
        HashSet<string> dynamicSections = new HashSet<string>(StringComparer.Ordinal)
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(plan.Person.PersonId)
        };
        if (plan.MaterializedNpc != null)
        {
            dynamicSections.Add(DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix
                + plan.MaterializedNpc.RuntimeId);
            if (plan.ExpectedAction != null)
                dynamicSections.Add(DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
                    + plan.MaterializedNpc.RuntimeId);
        }
        if (plan.IsResident)
            dynamicSections.Add(DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(plan.ResidenceCity.RuntimeId));

        List<DailyV1OwnerSectionContractIdentity> contracts = new List<DailyV1OwnerSectionContractIdentity>();
        List<DailyV1OwnerSectionCensusProviderRegistration> registrations =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(snapshots.Length);
        foreach (OwnerSectionCensusSnapshot snapshot in snapshots)
        {
            CensusProvider provider = CreateProvider(snapshot, plan, dynamicSections);
            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
            registrations.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role, provider,
                snapshot.OwnerInstanceIdentity, snapshot.Cardinality, snapshot.Revision,
                hasBaseline: true));
        }

        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            contracts, registrations), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        Assert.That(state.TrySealSelectedOwnerSectionCensusRegistry(registrations), Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);
    }

    private static CensusProvider CreateProvider(
        OwnerSectionCensusSnapshot snapshot,
        DailyV1SelectedPersonDeathPlan plan,
        HashSet<string> dynamicSections)
    {
        if (!dynamicSections.Contains(snapshot.SectionId))
            return new CensusProvider(snapshot);

        if (string.Equals(snapshot.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(
                    plan.Person.PersonId), StringComparison.Ordinal))
            return new CensusProvider(snapshot,
                () => PersonRuntimeOwnerBridge.GetLifeResidenceRevision(plan.Person), () => 1);

        if (plan.MaterializedNpc != null
            && string.Equals(snapshot.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix
                    + plan.MaterializedNpc.RuntimeId, StringComparison.Ordinal))
            return new CensusProvider(snapshot,
                () => plan.MaterializedNpc.Lifecycle.LifeStateRevision, () => 1);

        if (plan.MaterializedNpc != null
            && plan.ExpectedAction != null
            && string.Equals(snapshot.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
                    + plan.MaterializedNpc.RuntimeId, StringComparison.Ordinal))
            return new CensusProvider(snapshot,
                () => plan.MaterializedNpc.ActionSlot.Revision,
                () => plan.MaterializedNpc.ActionSlot.CurrentAction == null ? 0 : 1);

        if (plan.IsResident
            && string.Equals(snapshot.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                    + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(plan.ResidenceCity.RuntimeId),
                StringComparison.Ordinal))
            return new CensusProvider(snapshot,
                () => plan.ResidenceCity.Population.Revision, () => 1);

        throw new InvalidOperationException(
            "The selected Person-death regression did not supply a live provider for " + snapshot.SectionId);
    }
}
