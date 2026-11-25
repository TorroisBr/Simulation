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
        internal bool RejectNextRead;
        internal int RejectOnReadNumber;
        internal int ReadCount;

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

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            ReadCount++;
            if (RejectNextRead || ReadCount == RejectOnReadNumber)
            {
                RejectNextRead = false;
                RejectOnReadNumber = 0;
                throw new InvalidOperationException("Injected census provider failure.");
            }
            return new OwnerSectionCensusWitness(
                SectionId, SchemaVersion, OwnerInstanceIdentity,
                cardinality(), revision());
        }
    }

    private sealed class PersonDeathProjection : IDailyV1SelectedPersonDeathProjectionBoundary
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        internal int PublishedDeaths;
        internal int Faults;
        internal int WorldRevisionAdvances;
        internal int BeginProjectionMutationCalls;
        internal int CompleteProjectionMutationCalls;
        internal bool LastProjectionMutationSucceeded;
        internal int WorldRevision;
        internal bool AllowProjectionMutationBegin = true;
        internal bool ThrowOnProjectionMutationBegin;
        internal bool ThrowOnProjectionPublication;
        internal bool RejectNextSuccessfulCompletion;
        internal Action OnSuccessfulCompatibilityBaselineStage;
        internal long CompatibilityBaselineRevision;
        private long compatibilityBaselineRevisionAtBegin;
        private int publishedDeathsAtBegin;
        private int worldRevisionAtBegin;

        internal PersonDeathProjection(DailyV1SelectedRuntimeOwnerGraph graph) =>
            this.graph = graph;

        public bool TryBeginPersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds)
        {
            BeginProjectionMutationCalls++;
            if (ThrowOnProjectionMutationBegin)
                throw new InvalidOperationException("Injected projection begin failure.");
            bool admitted = AllowProjectionMutationBegin
                && plan != null && sectionIds != null && sectionIds.Count > 0;
            if (admitted)
            {
                publishedDeathsAtBegin = PublishedDeaths;
                worldRevisionAtBegin = WorldRevision;
                compatibilityBaselineRevisionAtBegin = CompatibilityBaselineRevision;
            }
            return admitted;
        }

        public bool TryCompletePersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds,
            bool operationSucceeded)
        {
            CompleteProjectionMutationCalls++;
            LastProjectionMutationSucceeded = operationSucceeded;
            if (operationSucceeded && RejectNextSuccessfulCompletion)
            {
                RejectNextSuccessfulCompletion = false;
                return false;
            }
            if (operationSucceeded && OnSuccessfulCompatibilityBaselineStage != null)
            {
                CompatibilityBaselineRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(
                    plan.Person);
                Action afterStage = OnSuccessfulCompatibilityBaselineStage;
                OnSuccessfulCompatibilityBaselineStage = null;
                afterStage();
            }
            if (!operationSucceeded)
            {
                PublishedDeaths = publishedDeathsAtBegin;
                WorldRevision = worldRevisionAtBegin;
                CompatibilityBaselineRevision = compatibilityBaselineRevisionAtBegin;
            }
            return plan != null && sectionIds != null && sectionIds.Count > 0;
        }

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
            if (ThrowOnProjectionPublication)
                throw new InvalidOperationException("Injected projection publication failure after Core owner writes.");
            PublishedDeaths++;
            return true;
        }

        public void FaultPersonDeathRuntime() => Faults++;
        public void AdvancePersonDeathWorldRevision()
        {
            WorldRevisionAdvances++;
            WorldRevision++;
        }
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

        BindCompleteCoreCensus(state, session, plan, out _);
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

        projection.AllowProjectionMutationBegin = false;
        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan, boundary, out diagnostic), Is.False,
            "A rejected host writer permit must abort before the first owner write.");
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);

        projection.AllowProjectionMutationBegin = true;
        projection.ThrowOnProjectionMutationBegin = true;
        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan, boundary, out diagnostic), Is.False,
            "An exception while establishing the host permit must fail closed before owner writes.");
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);

        projection.ThrowOnProjectionMutationBegin = false;
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
        Assert.That(projection.BeginProjectionMutationCalls, Is.EqualTo(4));
        Assert.That(projection.CompleteProjectionMutationCalls, Is.EqualTo(3),
            "Success stages the host baseline before Core epoch publication, then revokes the permit after commit.");
        Assert.That(projection.LastProjectionMutationSucceeded, Is.True);
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

    [Test]
    public void SelectedPersonDeathPlanCanBePreparedWhileItsP12WriterIsClosed()
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
            new PersonId("person-death-plan-with-closed-writer"), state.SimulationTime.AbsoluteDay);
        Assert.That(graph.PersonStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        bool writerPermitOpen = false;
        PersonRuntimeOwnerBridge.BindP12LifecycleMutationBoundary(
            person, _ => writerPermitOpen, _ => { });

        Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(person), Is.False,
            "The compatibility writer is closed until the Core operation issues its exact P12 scope.");
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, graph.PersonStore, person, state.SimulationTime.AbsoluteDay,
            out DailyV1SelectedPersonDeathPlan plan, out diagnostic), Is.True, diagnostic,
            "Preparation validates expected state and revision capacity; it must not require a writer permit before operation admission.");
        Assert.That(plan.Person, Is.SameAs(person));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero);
        Assert.That(PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(person), Is.False,
            "Preparing the plan cannot open or retain the P12 writer permission.");
    }

    [Test]
    public void SelectedResidentDeathCanCommitThroughCoreWithoutUnityProjection()
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
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = graph.Cities.FirstOrDefault(candidate =>
            candidate?.Population?.CurrentPopulation > 0);
        Assert.That(city, Is.Not.Null);

        PersonRuntime person = new PersonRuntime(
            new PersonId("detached-core-resident-death"), state.SimulationTime.AbsoluteDay);
        Assert.That(graph.PersonStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(graph.PersonStore.TryBindMaterializedNpc(
            person.PersonId, npc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);

        DailyV1ActionDefinition action = definitions.Actions["DESCANSAR"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, "detached-core-death/action",
            null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(npc, action, payload, null), Is.True);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, graph.PersonStore, person, state.SimulationTime.AbsoluteDay,
            out DailyV1SelectedPersonDeathPlan plan, out diagnostic), Is.True, diagnostic);
        Assert.That(plan.MaterializedNpc, Is.SameAs(npc));
        Assert.That(plan.ResidenceCity, Is.SameAs(city));

        BindCompleteCoreCensus(state, session, plan, out _);
        var boundary = new DailyV1CoreSelectedPersonDeathBoundary(
            state, graph, projection: null);
        long priorEpoch = state.MutationEpochState.CurrentEpoch;
        long personRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        long npcLifeRevision = npc.Lifecycle.LifeStateRevision;
        long actionRevision = npc.ActionSlot.Revision;
        long populationRevision = city.Population.Revision;
        int populationBefore = city.Population.CurrentPopulation;

        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            boundary, out diagnostic), Is.True, diagnostic);

        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(plan.AbsoluteDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person),
            Is.EqualTo(personRevision + 2L));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision + 1L));
        Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
        Assert.That(npc.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision + 1L));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision + 1L));
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

    [Test]
    public void SelectedResidentDeathWithoutUnityProjectionRollsBackWhenCoreCommitRejects()
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
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = graph.Cities.FirstOrDefault(candidate =>
            candidate?.Population?.CurrentPopulation > 0);
        Assert.That(city, Is.Not.Null);

        PersonRuntime person = new PersonRuntime(
            new PersonId("detached-core-resident-death-rollback"), state.SimulationTime.AbsoluteDay);
        Assert.That(graph.PersonStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(graph.PersonStore.TryBindMaterializedNpc(
            person.PersonId, npc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);

        DailyV1ActionDefinition action = definitions.Actions["DESCANSAR"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, "detached-core-death-rollback/action",
            null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(npc, action, payload, null), Is.True);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, graph.PersonStore, person, state.SimulationTime.AbsoluteDay,
            out DailyV1SelectedPersonDeathPlan plan, out diagnostic), Is.True, diagnostic);

        BindCompleteCoreCensus(state, session, plan, out CensusProvider personProvider);
        var boundary = new DailyV1CoreSelectedPersonDeathBoundary(
            state, graph, projection: null);
        long priorEpoch = state.MutationEpochState.CurrentEpoch;
        long personRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        long npcLifeRevision = npc.Lifecycle.LifeStateRevision;
        long actionRevision = npc.ActionSlot.Revision;
        long populationRevision = city.Population.Revision;
        int populationBefore = city.Population.CurrentPopulation;
        int failOnRead = personProvider.ReadCount + 2;
        personProvider.RejectOnReadNumber = failOnRead;

        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            boundary, out diagnostic), Is.False,
            "A rejected exact Core owner-vector commit must roll back without relying on a Unity projection.");
        Assert.That(personProvider.ReadCount, Is.GreaterThanOrEqualTo(failOnRead));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(personRevision));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision));
        Assert.That(npc.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(npc.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision));
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long capturedEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(capturedEpoch, Is.EqualTo(priorEpoch));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    [Test]
    public void SelectedResidentPersonDeathUsesExactNpcActionAndCitySections()
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
        Assert.That(graph.Npcs, Is.Not.Empty);
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = graph.Cities.FirstOrDefault(candidate =>
            candidate?.Population?.CurrentPopulation > 0);
        Assert.That(city, Is.Not.Null);
        Assert.That(city.Population.CurrentPopulation, Is.GreaterThan(0));

        PersonRuntime person = new PersonRuntime(
            new PersonId("core-owned-resident-death-boundary"), state.SimulationTime.AbsoluteDay);
        Assert.That(graph.PersonStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(graph.PersonStore.TryBindMaterializedNpc(
            person.PersonId, npc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);

        DailyV1ActionDefinition action = definitions.Actions["DESCANSAR"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, "person-death/resident-action",
            null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(npc, action, payload, null), Is.True);

        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, graph.PersonStore, person, state.SimulationTime.AbsoluteDay,
            out DailyV1SelectedPersonDeathPlan plan, out diagnostic), Is.True, diagnostic);
        Assert.That(plan.IsResident, Is.True);
        Assert.That(plan.MaterializedNpc, Is.SameAs(npc));
        Assert.That(plan.ExpectedAction, Is.SameAs(action));
        Assert.That(plan.ResidenceCity, Is.SameAs(city));

        Assert.That(DailyV1SelectedPersonDeathOperation.TryCreateP12SectionIds(
            plan, out IReadOnlyList<string> expectedSections), Is.True);
        Assert.That(expectedSections, Is.EqualTo(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(person.PersonId),
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + npc.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId)
        }));

        BindCompleteCoreCensus(state, session, plan, out CensusProvider personProvider);
        PersonDeathProjection projection = new PersonDeathProjection(graph);
        DailyV1CoreSelectedPersonDeathBoundary boundary =
            new DailyV1CoreSelectedPersonDeathBoundary(state, graph, projection);

        long priorEpoch = state.MutationEpochState.CurrentEpoch;
        long personRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        long npcLifeRevision = npc.Lifecycle.LifeStateRevision;
        long actionRevision = npc.ActionSlot.Revision;
        long populationRevision = city.Population.Revision;
        int populationBefore = city.Population.CurrentPopulation;

        List<string> mismatchedSections = expectedSections.ToList();
        mismatchedSections[1] += "/wrong-owner";
        Assert.That(boundary.TryBeginPersonDeathOperation(
            plan, currentActionChanged: true,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            mismatchedSections, () => true,
            out IDailyV1SelectedPersonDeathP12Scope rejectedScope), Is.False);
        Assert.That(rejectedScope, Is.Null);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));

        projection.ThrowOnProjectionPublication = true;
        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            boundary, out diagnostic), Is.False,
            "A projection exception after Core has written every owner must roll back the full transaction.");

        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(personRevision));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision));
        Assert.That(npc.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(npc.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision));
        Assert.That(projection.WorldRevisionAdvances, Is.Zero);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(projection.Faults, Is.Zero);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> rolledBackCensus,
            out long rolledBackEpoch,
            out DailyCaptureEligibilityFailure rollbackFailure), Is.True, rollbackFailure.ToString());
        Assert.That(rolledBackEpoch, Is.EqualTo(priorEpoch));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(rolledBackCensus), Is.True);

        projection.ThrowOnProjectionPublication = false;
        projection.RejectNextSuccessfulCompletion = true;
        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            boundary, out diagnostic), Is.False,
            "A host compatibility-baseline rejection before Core epoch publication must roll back every owner.");

        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(personRevision));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision));
        Assert.That(npc.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(npc.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision));
        Assert.That(projection.WorldRevision, Is.Zero,
            "A rejected Core epoch commit must restore the compatibility world revision.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> rejectedCommitCensus,
            out long rejectedCommitEpoch,
            out DailyCaptureEligibilityFailure rejectedCommitFailure), Is.True,
            rejectedCommitFailure.ToString());
        Assert.That(rejectedCommitEpoch, Is.EqualTo(priorEpoch));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(rejectedCommitCensus), Is.True);

        projection.CompatibilityBaselineRevision = personRevision;
        projection.OnSuccessfulCompatibilityBaselineStage = () => personProvider.RejectNextRead = true;
        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            boundary, out diagnostic), Is.False,
            "A Core census commit rejection after host baseline staging must restore both owner state and host baselines.");
        Assert.That(diagnostic, Does.Contain("CoreCensusOrEpochCommitRejected:ProviderReadFailed"));
        Assert.That(personProvider.RejectNextRead, Is.False,
            "The injected Core provider rejection must occur during final Core census publication.");
        Assert.That(projection.CompatibilityBaselineRevision, Is.EqualTo(personRevision),
            "The host compatibility baseline staged before Core publication must be restored on rejection.");
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(personRevision));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision));
        Assert.That(npc.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(npc.ActionSlot.CurrentPayload, Is.SameAs(payload));
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision));
        Assert.That(projection.WorldRevision, Is.Zero,
            "The host world revision must be restored if Core publication rejects after host baseline staging.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> afterCoreRejection,
            out long afterCoreRejectionEpoch,
            out DailyCaptureEligibilityFailure afterCoreRejectionFailure), Is.True,
            afterCoreRejectionFailure.ToString());
        Assert.That(afterCoreRejectionEpoch, Is.EqualTo(priorEpoch));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(afterCoreRejection), Is.True);

        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            boundary, out diagnostic), Is.True, diagnostic);

        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(plan.AbsoluteDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person),
            Is.EqualTo(personRevision + 2L));
        Assert.That(person.MaterializedNpcRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision + 1L));
        Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
        Assert.That(npc.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision + 1L));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision + 1L));
        Assert.That(projection.PublishedDeaths, Is.EqualTo(1));
        Assert.That(projection.Faults, Is.Zero,
            "Core census refresh rejection: " + state.LastSelectedOwnerSectionCensusRefreshRejection);
        Assert.That(projection.WorldRevisionAdvances, Is.EqualTo(3),
            "The two post-projection commit rejections each advance then restore the projection revision; only the successful attempt remains effective.");
        Assert.That(projection.WorldRevision, Is.EqualTo(1));
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch + 1L));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long capturedEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(capturedEpoch, Is.EqualTo(priorEpoch + 1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    internal static void BindCompleteCoreCensus(
        DailyV1ExecutionBoundaryState state,
        DailyV1ActiveRuntimeSession session,
        DailyV1SelectedPersonDeathPlan plan) =>
        BindCompleteCoreCensus(state, session, plan, out _);

    private static void BindCompleteCoreCensus(
        DailyV1ExecutionBoundaryState state,
        DailyV1ActiveRuntimeSession session,
        DailyV1SelectedPersonDeathPlan plan,
        out CensusProvider personProvider)
    {
        personProvider = null;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        if (!state.HasRuntimeProtocolOperationInventory)
        {
            foreach (string operationId in DailyV1RuntimeProtocolOperationContracts
                         .GetExpectedOperationContractIds(
                             hasRuntimeAdmission: true,
                             hasTravelPartySystem: false,
                             hasTravelSystem: false))
                Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationId), Is.True, operationId);
            Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
        }

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
            if (string.Equals(snapshot.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(
                        plan.Person.PersonId), StringComparison.Ordinal))
                personProvider = provider;
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
