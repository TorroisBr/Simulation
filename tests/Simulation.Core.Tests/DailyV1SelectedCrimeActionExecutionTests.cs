using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedCrimeActionExecutionTests
{
    [Test]
    public void CoreCrimeBoundaryOwnsExactP12ActorJusticeAndReceiptScope()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.HideActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, null, null, 0, 0f, 1f,
            NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);

        RecordingP12Protocol protocol = new RecordingP12Protocol();
        object crimeIdentity = new object();
        object justiceIdentity = new object();
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding =
            new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                protocol,
                crimeIdentity, crimeIdentity, graph.JusticeRecordsOwner,
                justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
                admissionEnabled: true);
        RecordingCrimeProjection projection = new RecordingCrimeProjection();
        RecordingCrimeMutationHostPort hostPort =
            new RecordingCrimeMutationHostPort(hostBinding);
        Assert.That(DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedCrimeActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, actor, action,
                projection, hostPort, out DailyV1SelectedCrimeActionExecutionStage
                    .ISelectedCrimeActionBoundary boundary), Is.True);
        Assert.That(composition.ExecutionBoundaryState
            .TryBindSelectedP12CrimeJusticeMutationProtocol(
                protocol, graph,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);

        bool operationRan = false;
        Assert.That(boundary.ExecuteWithinOperation(() =>
        {
            operationRan = true;
            Assert.That(coordinator.CanBeginDailyMutation(), Is.False,
                "The Core P12 operation remains active for the complete selected action.");
            return true;
        }), Is.True);

        Assert.That(operationRan, Is.True);
        Assert.That(coordinator.CanBeginDailyMutation(), Is.True,
            "Disposing the Core-owned action scope releases the shared coordinator.");
        Assert.That(protocol.ValidatedSections, Is.EqualTo(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(actor.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId
        }));
        Assert.That(protocol.Faulted, Is.False);
        Assert.That(hostPort.BindingRequests, Is.GreaterThan(1),
            "Core obtains the exact host binding and rechecks it at operation use.");
    }

    [Test]
    public void CoreCrimeBoundaryRejectsReplacedHostBindingBeforeOpeningP12Scope()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.HideActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, null, null, 0, 0f, 1f,
            NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);

        RecordingP12Protocol protocol = new RecordingP12Protocol();
        object crimeIdentity = new object();
        object justiceIdentity = new object();
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding =
            new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                protocol,
                crimeIdentity, crimeIdentity, graph.JusticeRecordsOwner,
                justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
                admissionEnabled: true);
        RecordingCrimeMutationHostPort hostPort =
            new RecordingCrimeMutationHostPort(binding);
        Assert.That(DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedCrimeActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, actor, action,
                new RecordingCrimeProjection(), hostPort,
                out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary), Is.True);

        RecordingP12Protocol replacementProtocol = new RecordingP12Protocol();
        hostPort.Binding = new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
            replacementProtocol,
            crimeIdentity, crimeIdentity, graph.JusticeRecordsOwner,
            justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
            admissionEnabled: true);

        Assert.Throws<InvalidOperationException>(() =>
            boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced host binding must not enter its P12 operation.");
                return true;
            }));
        Assert.That(protocol.ValidatedSections, Is.Null);
        Assert.That(protocol.Faulted, Is.True);
        Assert.That(replacementProtocol.Faulted, Is.True);
    }

    [Test]
    public void CoreCrimeBoundaryRejectsStaleActionBeforeOpeningP12Scope()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.HideActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, null, null, 0, 0f, 1f,
            NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);

        RecordingP12Protocol protocol = new RecordingP12Protocol();
        object justiceIdentity = new object();
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding =
            new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                protocol,
                null, null, null,
                justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
                admissionEnabled: true);
        Assert.That(composition.ExecutionBoundaryState
            .TryBindSelectedCrimeJusticeDailyMutationHostBinding(graph, hostBinding), Is.True);
        Assert.That(DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedCrimeActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, actor, action,
                new RecordingCrimeProjection(),
                new RecordingCrimeMutationHostPort(hostBinding),
                out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary), Is.True);

        DailyV1ActionDefinition replacementAction = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, replacementAction, null, null, null, 0, 0f, 1f,
            NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner replacementPayload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(
            actor, replacementAction, replacementPayload, null), Is.True);

        Assert.Throws<InvalidOperationException>(() =>
            boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A stale action must not enter its P12 operation.");
                return true;
            }));
        Assert.That(protocol.ValidatedSections, Is.Null,
            "A replaced Core action slot is rejected before census baselines are queried.");
        Assert.That(protocol.Faulted, Is.True,
            "Losing the exact admitted operation identity faults the bound census protocol.");
    }

    [Test]
    public void CoreFleeBoundaryOwnsExactP12ActorJusticeAndReceiptScope()
    {
        CoreFleeBoundaryFixture fixture = CreateCoreFleeBoundaryFixture();
        bool projectionPublished = false;
        bool operationRan = false;

        Assert.That(fixture.Boundary.ExecuteWithinOperation(() =>
        {
            operationRan = true;
            Assert.That(fixture.Coordinator.CanBeginDailyMutation(), Is.False,
                "The Core P12 operation remains active while FleeCity commits and projects its owner write.");
            Assert.That(fixture.Boundary.CanPrepareTravelPlanProjection(
                fixture.Actor, fixture.Destination, 3f, "decision/flee-city"), Is.True);
            Assert.That(fixture.Boundary.CanAdmitTravelPlanMutation(), Is.True);
            fixture.Boundary.PublishCommittedTravelPlan(
                fixture.Actor, fixture.Destination, 3f, "decision/flee-city");
            projectionPublished = true;
            return true;
        }), Is.True);

        Assert.That(operationRan, Is.True);
        Assert.That(projectionPublished, Is.True);
        Assert.That(fixture.Projection.PreflightCalls, Is.EqualTo(1));
        Assert.That(fixture.Projection.AdmissionCalls, Is.EqualTo(1));
        Assert.That(fixture.Projection.PublicationCalls, Is.EqualTo(1));
        Assert.That(fixture.Coordinator.CanBeginDailyMutation(), Is.True,
            "Disposing the Core-owned scope releases the selected P12 operation.");
        Assert.That(fixture.Protocol.ValidatedSections, Is.EqualTo(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(fixture.Actor.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                fixture.Actor.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId
        }));
        Assert.That(fixture.Protocol.Faulted, Is.False);
        Assert.That(fixture.HostPort.BindingRequests, Is.GreaterThan(1),
            "Core rechecks the host census binding at the exact operation boundary.");
    }

    [Test]
    public void CoreOnlyFleeCityDispatchCommitsTravelPlanAndExactP12Owner()
    {
        CoreOnlyFleeDispatchFixture fixture = CreateCoreOnlyFleeDispatchFixture();
        long travelPlanRevisionBefore = fixture.Actor.TravelPlan.Revision;
        long mutationEpochBefore = fixture.ExecutionState.MutationEpochState.CurrentEpoch;
        Assert.That(fixture.ExecutionState.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                fixture.Graph,
                out IDailyV1P12CrimeJusticeMutationProtocol protocol,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);
        string travelPlanSection = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
            fixture.Actor.RuntimeId);
        Assert.That(protocol.TryValidateUnchangedSections(new[] { travelPlanSection }), Is.True,
            "The exact TravelPlan row starts at its admitted baseline.");

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
                fixture.Graph, fixture.Actor, new FixedActionRandom(0f), 12L,
                fixture.Boundary, out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.FleeCity));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.FleeCity, Is.Not.Null);
        Assert.That(result.FleeCity.Succeeded, Is.True);
        Assert.That(result.FleeCity.DestinationCityRuntimeId,
            Is.EqualTo(fixture.Destination.RuntimeId));
        Assert.That(fixture.Actor.TravelPlan.Revision,
            Is.EqualTo(travelPlanRevisionBefore + 1L));
        Assert.That(fixture.Actor.TravelPlan.Matches(
                fixture.Destination.LocationRuntimeId,
                fixture.Destination.RuntimeId,
                NpcTravelReason.Flee,
                80f,
                result.FleeCity.TravelCost,
                "decision/flee-city"), Is.True);
        Assert.That(fixture.ExecutionState.MutationEpochState.CurrentEpoch,
            Is.EqualTo(mutationEpochBefore + 1L));
        Assert.That(protocol.TryValidateUnchangedSections(new[] { travelPlanSection }), Is.True,
            "The successful Core mutation refreshes the exact TravelPlan census baseline.");
        Assert.That(coordinator.CanBeginDailyMutation(), Is.True,
            "The immediate operation releases its P12 scope after publication.");
    }

    [Test]
    public void CoreOnlyStealDispatchCommitsExactMoneyAndCrimeOwnersThroughCoreP12()
    {
        CoreOnlyStealDispatchFixture fixture = CreateCoreOnlyStealDispatchFixture();
        float victimBalanceBefore = fixture.Victim.MoneyAccount.Balance;
        float perpetratorBalanceBefore = fixture.Perpetrator.MoneyAccount.Balance;
        long victimRevisionBefore = fixture.Victim.MoneyAccount.Revision;
        long perpetratorRevisionBefore = fixture.Perpetrator.MoneyAccount.Revision;
        long epochBefore = fixture.ExecutionState.MutationEpochState.CurrentEpoch;
        CrimeSocialAppraisalWorldState social = fixture.Graph.P12EOwners.CrimeSocialAppraisal;
        int outcomesBefore = social.TheftOutcomes.Count;
        long outcomesRevisionBefore = social.TheftOutcomes.P12CensusRevision;
        Assert.That(fixture.Perpetrator.Travel.IsTraveling, Is.False);
        Assert.That(fixture.Victim.Travel.IsTraveling, Is.False);
        Assert.That(fixture.Perpetrator.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(fixture.Victim.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(fixture.Victim.HiddenState.RemainingDays, Is.Zero);
        Assert.That(DailyV1NpcDefinitionRules.IsAdmittedAction(
            fixture.Graph.Definitions, fixture.Perpetrator.ActionSlot.CurrentAction), Is.True);
        Assert.That(DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
            fixture.Graph.Definitions, fixture.Perpetrator.ActionSlot.CurrentAction,
            fixture.Perpetrator.Status.Statuses), Is.True);
        Assert.That(social.PersonStore.TryGetByMaterializedNpcRuntimeId(
            fixture.Perpetrator.RuntimeId, out PersonRuntime perpetratorPerson), Is.True);
        Assert.That(social.PersonStore.TryGetByMaterializedNpcRuntimeId(
            fixture.Victim.RuntimeId, out PersonRuntime victimPerson), Is.True);
        TheftOutcomeId expectedOutcomeId = TheftOutcomeId.Create(
            perpetratorPerson.PersonId, victimPerson.PersonId,
            social.SimulationTime.AbsoluteDay, fixture.Perpetrator.ActionSlot.CurrentPayload.StableOccurrenceKey);
        TheftOutcome expectedOutcome = new TheftOutcome(
            expectedOutcomeId, perpetratorPerson.PersonId, victimPerson.PersonId,
            17, social.SimulationTime.AbsoluteDay,
            fixture.Perpetrator.ActionSlot.CurrentPayload.StableOccurrenceKey,
            fixture.Perpetrator.ActionSlot.CurrentPayload.OriginDecisionId);
        Assert.That(social.Integration.CanAcceptTheftOutcome(expectedOutcome), Is.True);
        Assert.That(DailyV1MoneyTransferOperation.Validate(
            fixture.Victim.MoneyAccount, fixture.Perpetrator.MoneyAccount, 17,
            fixture.Victim.RuntimeId, fixture.Perpetrator.RuntimeId), Is.Null);

        Assert.That(fixture.ExecutionState.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                fixture.Graph,
                out IDailyV1P12CrimeJusticeMutationProtocol protocol,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);
        Assert.That(fixture.Boundary.TryGetStealActionBoundary(
                fixture.Perpetrator, fixture.Perpetrator.ActionSlot.CurrentAction,
                out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary stealBoundary), Is.True);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
                fixture.Graph, fixture.Perpetrator, new FixedActionRandom(0f), 41L,
                fixture.Boundary, out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.Steal));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Steal, Is.Not.Null);
        Assert.That(result.Steal.TargetRuntimeId, Is.EqualTo(fixture.Victim.RuntimeId));
        Assert.That(result.Steal.Amount, Is.EqualTo(17));
        Assert.That(fixture.Victim.MoneyAccount.Balance, Is.EqualTo(victimBalanceBefore - 17f));
        Assert.That(fixture.Perpetrator.MoneyAccount.Balance, Is.EqualTo(perpetratorBalanceBefore + 17f));
        Assert.That(fixture.Victim.MoneyAccount.Revision, Is.EqualTo(victimRevisionBefore + 1L));
        Assert.That(fixture.Perpetrator.MoneyAccount.Revision, Is.EqualTo(perpetratorRevisionBefore + 1L));
        Assert.That(social.TheftOutcomes.Count, Is.EqualTo(outcomesBefore + 1));
        Assert.That(social.TheftOutcomes.P12CensusRevision, Is.EqualTo(outcomesRevisionBefore + 1L));
        Assert.That(fixture.ExecutionState.MutationEpochState.CurrentEpoch, Is.GreaterThan(epochBefore));
        Assert.That(protocol.TryValidateUnchangedSections(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
                + fixture.Perpetrator.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
                + fixture.Victim.RuntimeId,
            P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId
        }), Is.True,
            "Core refreshes exact account and theft-outcome census baselines after committed owner writes.");
        Assert.That(coordinator.CanBeginDailyMutation(), Is.True,
            "Both nested Core P12 operation scopes close after the selected action.");
        Assert.That(fixture.ExecutionState.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
    }

    [Test]
    public void CoreOnlyFleeCityDomainRejectionLeavesTravelPlanAndP12EpochUnchanged()
    {
        CoreOnlyFleeDispatchFixture fixture = CreateCoreOnlyFleeDispatchFixture(actorArrested: true);
        long travelPlanRevisionBefore = fixture.Actor.TravelPlan.Revision;
        long mutationEpochBefore = fixture.ExecutionState.MutationEpochState.CurrentEpoch;
        Assert.That(fixture.ExecutionState.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                fixture.Graph,
                out IDailyV1P12CrimeJusticeMutationProtocol protocol,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
                fixture.Graph, fixture.Actor, new FixedActionRandom(1f), 12L,
                fixture.Boundary, out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.FleeCity));
        Assert.That(result.ActionRollSucceeded, Is.True,
            "FleeCity cannot fail by its action roll; this case rejects its ineligible actor state.");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FleeCity.Succeeded, Is.False);
        Assert.That(fixture.Actor.TravelPlan.Revision, Is.EqualTo(travelPlanRevisionBefore));
        Assert.That(fixture.ExecutionState.MutationEpochState.CurrentEpoch,
            Is.EqualTo(mutationEpochBefore));
        Assert.That(coordinator.CanBeginDailyMutation(), Is.True,
            "A domain-rejected FleeCity action releases its Core P12 mutation scope.");
        Assert.That(protocol.TryValidateUnchangedSections(new[]
            {
                DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(
                    fixture.Actor.RuntimeId),
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                    fixture.Actor.RuntimeId)
            }), Is.True,
            "The domain rejection leaves both admitted actor and TravelPlan census rows unchanged.");
    }    [Test]
    public void CoreFleeBoundaryRejectsReplacedHostBindingBeforeOpeningP12Scope()
    {
        CoreFleeBoundaryFixture fixture = CreateCoreFleeBoundaryFixture();
        RecordingP12Protocol replacementProtocol = new RecordingP12Protocol();
        object replacementCrimeIdentity = new object();
        object replacementJusticeIdentity = new object();
        fixture.HostPort.Binding = new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
            replacementProtocol,
            replacementCrimeIdentity, replacementCrimeIdentity, fixture.Graph.JusticeRecordsOwner,
            replacementJusticeIdentity, replacementJusticeIdentity, fixture.Graph.JusticeRecordsOwner,
            admissionEnabled: true);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced host binding must not enter the selected FleeCity operation.");
                return true;
            }));
        Assert.That(fixture.Protocol.ValidatedSections, Is.Null);
        Assert.That(fixture.Protocol.Faulted, Is.True);
        Assert.That(replacementProtocol.Faulted, Is.True);
    }

    [Test]
    public void CoreFleeBoundaryRejectsStaleActionBeforeOpeningP12Scope()
    {
        CoreFleeBoundaryFixture fixture = CreateCoreFleeBoundaryFixture();
        DailyV1ActionDefinition replacementAction = ActionOfType(
            fixture.Actor.Definitions, DailyV1NpcDefinitionRules.HideActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            fixture.Actor.Definitions, replacementAction, null, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner replacementPayload), Is.True);
        Assert.That(fixture.Graph.TryCommitCurrentActionSlot(
            fixture.Actor, replacementAction, replacementPayload, null), Is.True);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced Core action slot must not enter the selected FleeCity operation.");
                return true;
            }));
        Assert.That(fixture.Protocol.ValidatedSections, Is.Null,
            "A stale FleeCity action is rejected before census baselines are queried.");
        Assert.That(fixture.Protocol.Faulted, Is.True);
    }

    [Test]
    public void CoreGuardArrestBoundaryOwnsExactParticipantsAndJusticeReceiptScope()
    {
        CoreGuardBoundaryFixture fixture = CreateCoreGuardBoundaryFixture();
        bool operationRan = false;

        Assert.That(fixture.Boundary.ExecuteWithinOperation(() =>
        {
            operationRan = true;
            Assert.That(fixture.Coordinator.CanBeginDailyMutation(), Is.False,
                "The Core P12 operation remains active across all selected Guard Arrest owner writes.");
            Assert.That(fixture.Boundary.JusticeRecordsOwner,
                Is.SameAs(fixture.Graph.JusticeRecordsOwner));
            Assert.That(fixture.Boundary.CanProjectStatusChanges(
                fixture.Guard, Array.Empty<DailyV1StatusDefinition>()), Is.True);
            Assert.That(fixture.Boundary.CanProjectStatusChanges(
                fixture.Target, Array.Empty<DailyV1StatusDefinition>()), Is.True);
            Assert.That(fixture.Boundary.CanProjectStatusChanges(
                fixture.Graph.Npcs[2], Array.Empty<DailyV1StatusDefinition>()), Is.False,
                "An unrelated owner in the same graph is outside this exact Guard Arrest operation.");
            return true;
        }), Is.True);

        Assert.That(operationRan, Is.True);
        Assert.That(fixture.Coordinator.CanBeginDailyMutation(), Is.True);
        Assert.That(fixture.Protocol.ValidatedSections, Is.EqualTo(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(fixture.Guard.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(fixture.Target.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId
        }));
        Assert.That(fixture.Protocol.Faulted, Is.False);
        Assert.That(fixture.HostPort.BindingRequests, Is.GreaterThan(1),
            "Core rechecks the host census binding at the exact operation boundary.");
    }

    [Test]
    public void CoreGuardArrestBoundaryRejectsReplacedHostBindingBeforeOpeningP12Scope()
    {
        CoreGuardBoundaryFixture fixture = CreateCoreGuardBoundaryFixture();
        RecordingP12Protocol replacementProtocol = new RecordingP12Protocol();
        object replacementCrimeIdentity = new object();
        object replacementJusticeIdentity = new object();
        fixture.HostPort.Binding = new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
            replacementProtocol,
            replacementCrimeIdentity, replacementCrimeIdentity, fixture.Graph.JusticeRecordsOwner,
            replacementJusticeIdentity, replacementJusticeIdentity, fixture.Graph.JusticeRecordsOwner,
            admissionEnabled: true);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced host binding must not enter the selected Guard Arrest operation.");
                return true;
            }));
        Assert.That(fixture.Protocol.ValidatedSections, Is.Null);
        Assert.That(fixture.Protocol.Faulted, Is.True);
        Assert.That(replacementProtocol.Faulted, Is.True);
    }

    [Test]
    public void CoreGuardArrestBoundaryRejectsStaleActionBeforeOpeningP12Scope()
    {
        CoreGuardBoundaryFixture fixture = CreateCoreGuardBoundaryFixture();
        DailyV1ActionDefinition replacementAction = ActionOfType(
            fixture.Guard.Definitions, DailyV1NpcDefinitionRules.HideActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            fixture.Guard.Definitions, replacementAction, null, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner replacementPayload), Is.True);
        Assert.That(fixture.Graph.TryCommitCurrentActionSlot(
            fixture.Guard, replacementAction, replacementPayload, null), Is.True);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced Guard action must not enter its P12 operation.");
                return true;
            }));
        Assert.That(fixture.Protocol.ValidatedSections, Is.Null,
            "A stale Guard action is rejected before census baselines are queried.");
        Assert.That(fixture.Protocol.Faulted, Is.True);
    }

    [Test]
    public void CoreStealBoundaryOwnsExactParticipantsAndJusticeReceiptScope()
    {
        CoreStealBoundaryFixture fixture = CreateCoreStealBoundaryFixture();
        bool operationRan = false;

        Assert.That(fixture.Boundary.ExecuteWithinOperation(() =>
        {
            operationRan = true;
            Assert.That(fixture.Coordinator.CanBeginDailyMutation(), Is.False,
                "The Core P12 operation remains active while the selected Steal writes its owners.");
            Assert.That(fixture.Boundary.JusticeRecordsOwner,
                Is.SameAs(fixture.Graph.JusticeRecordsOwner));
            Assert.That(fixture.Boundary.CanProjectStatusChanges(
                fixture.Perpetrator, Array.Empty<DailyV1StatusDefinition>()), Is.True);
            Assert.That(fixture.Boundary.CanProjectStatusChanges(
                fixture.Victim, Array.Empty<DailyV1StatusDefinition>()), Is.False,
                "The victim participates in admission but is not the selected status writer.");
            Assert.That(fixture.Boundary.CanProjectStatusChanges(
                fixture.Unrelated, Array.Empty<DailyV1StatusDefinition>()), Is.False,
                "An unrelated graph owner is outside this exact Steal operation.");

            EconomyTransactionResult nested = fixture.Boundary.ExecuteMoneyTransferWithinOperation(
                fixture.Perpetrator, fixture.Victim, () => null);
            Assert.That(nested, Is.Null);
            return true;
        }), Is.True);

        Assert.That(operationRan, Is.True);
        Assert.That(fixture.Coordinator.CanBeginDailyMutation(), Is.True);
        Assert.That(fixture.Protocol.ValidatedSections, Is.EqualTo(new[]
        {
            DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(fixture.Perpetrator.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(fixture.Victim.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId
        }));
        Assert.That(fixture.Protocol.Faulted, Is.False);
        Assert.That(fixture.HostPort.BindingRequests, Is.GreaterThan(1),
            "Core rechecks the exact host census binding at the operation boundary.");
        Assert.That(fixture.Projection.LegacyOperationCalls, Is.Zero,
            "The admitted path opens the mutation scope through Core.");
        Assert.That(fixture.Projection.TransferCalls, Is.EqualTo(1));
        Assert.That(fixture.Projection.TransferSource, Is.SameAs(fixture.Perpetrator));
        Assert.That(fixture.Projection.TransferDestination, Is.SameAs(fixture.Victim));
    }

    [Test]
    public void CoreStealBoundaryRejectsReplacedHostBindingBeforeOpeningP12Scope()
    {
        CoreStealBoundaryFixture fixture = CreateCoreStealBoundaryFixture();
        RecordingP12Protocol replacementProtocol = new RecordingP12Protocol();
        object replacementCrimeIdentity = new object();
        object replacementJusticeIdentity = new object();
        fixture.HostPort.Binding = new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
            replacementProtocol,
            replacementCrimeIdentity, replacementCrimeIdentity, fixture.Graph.JusticeRecordsOwner,
            replacementJusticeIdentity, replacementJusticeIdentity, fixture.Graph.JusticeRecordsOwner,
            admissionEnabled: true);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced host binding must not enter the selected Steal operation.");
                return true;
            }));
        Assert.That(fixture.Protocol.ValidatedSections, Is.Null);
        Assert.That(fixture.Protocol.Faulted, Is.True);
        Assert.That(replacementProtocol.Faulted, Is.True);
    }

    [Test]
    public void CoreStealBoundaryRejectsStaleActionBeforeOpeningP12Scope()
    {
        CoreStealBoundaryFixture fixture = CreateCoreStealBoundaryFixture();
        DailyV1ActionDefinition replacementAction = ActionOfType(
            fixture.Perpetrator.Definitions, DailyV1NpcDefinitionRules.HideActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            fixture.Perpetrator.Definitions, replacementAction, null, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner replacementPayload), Is.True);
        Assert.That(fixture.Graph.TryCommitCurrentActionSlot(
            fixture.Perpetrator, replacementAction, replacementPayload, null), Is.True);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Boundary.ExecuteWithinOperation(() =>
            {
                Assert.Fail("A replaced Steal action must not enter its P12 operation.");
                return true;
            }));
        Assert.That(fixture.Protocol.ValidatedSections, Is.Null,
            "A stale action is rejected before the P12 census baseline is queried.");
        Assert.That(fixture.Protocol.Faulted, Is.True);
    }

    [Test]
    public void SelectedHideUsesCoreWarrantAndKeepsHiddenTimerAndStatusWritesSeparate()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition action = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.HideActionTypeValue);
        fixture.AddWarrant(10f);
        CommitAction(fixture, action, 1f);
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);

        Assert.That(Dispatch(fixture, boundary, new FixedActionRandom(0f), out var result), Is.True);

        DailyV1StatusDefinition hidden = fixture.Definitions.Statuses[
            fixture.Definitions.Configuration.hiddenStatus];
        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.Crime));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(fixture.Actor.HiddenState.RemainingDays, Is.EqualTo(2),
            "Hide preserves the existing following-full-days plus current-day duration.");
        Assert.That(fixture.Actor.Status.Contains(hidden), Is.True);
        Assert.That(boundary.ActorMutationNotifications, Is.EqualTo(2),
            "the hidden timer and Hidden status remain two immediate owner writes");
        Assert.That(boundary.Order, Is.EqualTo(new[] {
            "p12-begin", "hidden-duration", "status-add", "hidden-log", "p12-end" }));
        Assert.That(fixture.Justice.Revision, Is.EqualTo(1),
            "Hide reads the warrant but does not write Justice owner state.");
    }

    [Test]
    public void CoreHiddenOwnerWritesKeepNoOpsOutsideP12AdmissionAndCommitEachLeafOnce()
    {
        CrimeFixture fixture = CreateFixture();
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);
        Assert.That(fixture.Actor.HiddenState.TrySetRemainingDays(2), Is.True);

        Assert.That(DailyV1SelectedActionExecutionStage.TryHideSelectedNpcForDays(
            fixture.Actor, 1, boundary), Is.True);
        Assert.That(fixture.Actor.HiddenState.RemainingDays, Is.EqualTo(2));
        Assert.That(boundary.MutationAdmissions, Is.Zero,
            "A duration request that does not change the Core owner is a no-op.");
        Assert.That(boundary.ActorMutationNotifications, Is.Zero);

        Assert.That(DailyV1SelectedActionExecutionStage.TryClearSelectedNpcHiddenState(
            fixture.Actor, boundary), Is.True);
        Assert.That(fixture.Actor.HiddenState.RemainingDays, Is.Zero);
        Assert.That(boundary.MutationAdmissions, Is.EqualTo(1));
        Assert.That(boundary.ActorMutationNotifications, Is.EqualTo(1));

        Assert.That(DailyV1SelectedActionExecutionStage.TryClearSelectedNpcHiddenState(
            fixture.Actor, boundary), Is.True);
        Assert.That(boundary.MutationAdmissions, Is.EqualTo(1),
            "Clearing an already-clear owner is a no-op and opens no extra P12 leaf admission.");
        Assert.That(boundary.ActorMutationNotifications, Is.EqualTo(1));
    }

    [Test]
    public void FailedSelectedEscapeRegistersSentencePenaltyOnCoreJusticeOwner()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition action = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        fixture.AddArrestedStatus();
        DailyV1JusticeWantedRecordOwner warrant = fixture.AddWarrant(12f, sentenceDays: 4);
        DailyV1JusticeSentenceOwner sentence = fixture.AddSentence(warrant, 4, failedAttempts: 0);
        long justiceRevisionBefore = fixture.Justice.Revision;
        CommitAction(fixture, action, DailyV1CrimeActionPlanning.GetEscapeSuccessChanceMultiplier(0));
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);

        Assert.That(Dispatch(fixture, boundary, new FixedActionRandom(1f), out var result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(sentence.FailedEscapeAttempts, Is.EqualTo(1));
        Assert.That(sentence.RemainingDays, Is.EqualTo(6));
        Assert.That(fixture.Justice.Revision, Is.EqualTo(justiceRevisionBefore + 1));
        Assert.That(boundary.Order, Is.EqualTo(new[] {
            "p12-begin", "failed-escape-log", "p12-end" }));
    }

    [Test]
    public void SuccessfulSelectedEscapeCommitsPenaltySentenceAndStatusOwnersInOrder()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition action = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        DailyV1StatusDefinition arrested = fixture.AddArrestedStatus();
        DailyV1StatusDefinition free = fixture.Definitions.Statuses[
            fixture.Definitions.Configuration.freeStatus];
        DailyV1StatusDefinition wanted = fixture.Definitions.Statuses[
            fixture.Definitions.Configuration.wantedStatus];
        fixture.Actor.Status.TryAdd(wanted);
        DailyV1JusticeWantedRecordOwner warrant = fixture.AddWarrant(12f, sentenceDays: 4);
        DailyV1JusticeSentenceOwner sentence = fixture.AddSentence(warrant, 4, failedAttempts: 0);
        long justiceRevisionBefore = fixture.Justice.Revision;
        CommitAction(fixture, action, DailyV1CrimeActionPlanning.GetEscapeSuccessChanceMultiplier(0));
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);

        Assert.That(Dispatch(fixture, boundary, new FixedActionRandom(0f), out var result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(fixture.Justice.ContainsSentence(sentence), Is.False);
        Assert.That(warrant.Bounty, Is.EqualTo(37f));
        Assert.That(fixture.Actor.Status.Contains(arrested), Is.False);
        Assert.That(fixture.Actor.Status.Contains(free), Is.True);
        Assert.That(fixture.Actor.Status.Contains(wanted), Is.True);
        Assert.That(fixture.Justice.Revision, Is.EqualTo(justiceRevisionBefore + 2),
            "warrant penalty and sentence removal each publish an immediate Justice owner change");
        Assert.That(boundary.ActorMutationNotifications, Is.EqualTo(1));
        Assert.That(boundary.Order, Is.EqualTo(new[] {
            "p12-begin", "sentence-remove", "status-remove", "escape-event", "p12-end" }),
            "Configured no-op status writes must not produce admission, revision, or notification events.");
    }

    [Test]
    public void ForcedEscapeUsesCoreOwnersWithoutRollAndAllowsSameDayArrestInsideOneOperation()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition action = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        DailyV1StatusDefinition arrested = fixture.AddArrestedStatus();
        DailyV1JusticeWantedRecordOwner warrant = fixture.AddWarrant(12f, sentenceDays: 4);
        DailyV1JusticeSentenceOwner sentence = fixture.AddSentence(
            warrant, 4, failedAttempts: 0, wasArrestedToday: true);
        CommitAction(fixture, action, 1f);
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);

        Assert.That(DailyV1SelectedCrimeActionExecutionStage.TryExecuteForcedEscapeOutcome(
            fixture.Graph, fixture.Actor, action, boundary, boundary,
            out bool escaped), Is.True);

        Assert.That(escaped, Is.True,
            "ForceOutcome retains the legacy same-day escape semantics rather than taking an ordinary action roll.");
        Assert.That(fixture.Justice.ContainsSentence(sentence), Is.False);
        Assert.That(fixture.Actor.Status.Contains(arrested), Is.False);
        Assert.That(boundary.Order.Count(value => value == "p12-begin"), Is.EqualTo(1));
        Assert.That(boundary.Order.Count(value => value == "p12-end"), Is.EqualTo(1));
        Assert.That(boundary.Order, Does.Contain("escape-event"));
        Assert.That(boundary.Order, Does.Contain("status-plan"));
        Assert.That(boundary.Order.IndexOf("status-plan"), Is.LessThan(boundary.Order.IndexOf("p12-end")),
            "configured action status effects remain inside the existing Crime/Justice scope.");
    }

    [Test]
    public void OrdinaryEscapeStillRejectsSameDayArrest()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition action = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        DailyV1StatusDefinition arrested = fixture.AddArrestedStatus();
        DailyV1JusticeWantedRecordOwner warrant = fixture.AddWarrant(12f, sentenceDays: 4);
        DailyV1JusticeSentenceOwner sentence = fixture.AddSentence(
            warrant, 4, failedAttempts: 0, wasArrestedToday: true);
        long justiceRevisionBefore = fixture.Justice.Revision;
        CommitAction(fixture, action, 1f);
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);

        Assert.That(Dispatch(fixture, boundary, new FixedActionRandom(0f), out var result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(fixture.Justice.ContainsSentence(sentence), Is.True);
        Assert.That(warrant.Bounty, Is.EqualTo(12f));
        Assert.That(fixture.Actor.Status.Contains(arrested), Is.True);
        Assert.That(fixture.Justice.Revision, Is.EqualTo(justiceRevisionBefore));
        Assert.That(boundary.Order, Is.EqualTo(new[] { "p12-begin", "p12-end" }));
    }

    [Test]
    public void SelectedEscapeWithoutLiveSentenceReleasesAndClearsHiddenState()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition action = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        DailyV1StatusDefinition arrested = fixture.AddArrestedStatus();
        DailyV1StatusDefinition hidden = fixture.Definitions.Statuses[
            fixture.Definitions.Configuration.hiddenStatus];
        DailyV1StatusDefinition free = fixture.Definitions.Statuses[
            fixture.Definitions.Configuration.freeStatus];
        fixture.Actor.Status.TryAdd(hidden);
        fixture.Actor.HiddenState.TrySetRemainingDays(3);
        CommitAction(fixture, action, 1f);
        RecordingCrimeBoundary boundary = new RecordingCrimeBoundary(fixture);

        Assert.That(Dispatch(fixture, boundary, new FixedActionRandom(0f), out var result), Is.True);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(fixture.Actor.Status.Contains(arrested), Is.False);
        Assert.That(fixture.Actor.Status.Contains(hidden), Is.False);
        Assert.That(fixture.Actor.Status.Contains(free), Is.True);
        Assert.That(fixture.Actor.HiddenState.RemainingDays, Is.Zero);
        Assert.That(boundary.Order, Is.EqualTo(new[] {
            "p12-begin", "status-remove", "hidden-clear",
            "status-remove", "escape-event", "p12-end" }),
            "Only committed status and hidden-state leaves publish immediate events.");
    }

    [Test]
    public void SelectedCrimePlanningUsesOwnersAndDoesNotAskUnityProviderToPlan()
    {
        CrimeFixture fixture = CreateFixture();
        DailyV1ActionDefinition escape = ActionOfType(fixture.Definitions,
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
        fixture.AddArrestedStatus();
        DailyV1JusticeWantedRecordOwner warrant = fixture.AddWarrant(12f, sentenceDays: 8);
        fixture.AddSentence(warrant, 8, failedAttempts: 2);
        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryChoose(
            fixture.Graph, fixture.Actor, new[] { escape }, fixture.Actor.Status.Statuses,
            12L, new FixedActionRandom(0f), out DailyV1NpcActionCandidate selected), Is.True);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected.Action, Is.SameAs(escape));
        Assert.That(selected.Utility, Is.EqualTo(30f));
        Assert.That(selected.PreparedPayload.SuccessChanceMultiplier,
            Is.EqualTo(0.64f).Within(0.000001f));
    }

    private static bool Dispatch(
        CrimeFixture fixture,
        RecordingCrimeBoundary crimeBoundary,
        IAuthoritativeRandomSource random,
        out DailyV1SelectedActionDispatchResult result) =>
        DailyV1SelectedActionDispatchStage.TryDispatch(
            fixture.Graph, fixture.Actor, random, 12L,
            new DispatchBoundary(crimeBoundary), out result);

    private static DailyV1ActionDefinition ActionOfType(
        DailyV1EffectiveDefinitions definitions, int actionType) =>
        definitions.Actions.Values.Single(value => value.Values.actionType?.Value == actionType);

    private static CoreFleeBoundaryFixture CreateCoreFleeBoundaryFixture()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1CityRuntimeOwners destination = graph.Cities.FirstOrDefault(candidate =>
            candidate != null
            && !string.Equals(candidate.RuntimeId, actor.Travel?.CurrentCityRuntimeId,
                StringComparison.Ordinal)
            && !string.Equals(candidate.LocationRuntimeId,
                actor.Travel?.CurrentLocationRuntimeId, StringComparison.Ordinal)
            && graph.TryPlanSelectedSoloTravel(actor,
                actor.Travel.CurrentLocationRuntimeId,
                candidate.LocationRuntimeId,
                candidate.RuntimeId,
                out SpatialRouteRuntime route,
                out float cost)
            && route != null
            && actor.MoneyAccount?.CanDebit(cost) == true
            && actor.TravelPlan?.CanCommit(
                candidate.LocationRuntimeId, candidate.RuntimeId,
                NpcTravelReason.Flee, 80f, cost, "decision/flee-city") == true);
        Assert.That(destination, Is.Not.Null,
            "The fixture must select a different reachable city with a valid FleeCity plan.");
        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, destination.RuntimeId, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/flee-city", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);

        RecordingP12Protocol protocol = new RecordingP12Protocol();
        object crimeIdentity = new object();
        object justiceIdentity = new object();
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding =
            new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                protocol,
                crimeIdentity, crimeIdentity, graph.JusticeRecordsOwner,
                justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
                admissionEnabled: true);
        RecordingCrimeMutationHostPort hostPort =
            new RecordingCrimeMutationHostPort(hostBinding);
        RecordingFleeProjection projection = new RecordingFleeProjection();
        Assert.That(DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedFleeCityActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, actor, action,
                projection, hostPort,
                out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary), Is.True);
        Assert.That(composition.ExecutionBoundaryState
            .TryBindSelectedP12CrimeJusticeMutationProtocol(
                protocol, graph,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);
        return new CoreFleeBoundaryFixture(
            composition.ExecutionBoundaryState, graph, actor, destination,
            protocol, hostPort, projection, boundary, coordinator);
    }

    private static CoreOnlyFleeDispatchFixture CreateCoreOnlyFleeDispatchFixture(bool actorArrested = false)
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1CityRuntimeOwners destination = graph.Cities.FirstOrDefault(candidate =>
            candidate != null
            && !string.Equals(candidate.RuntimeId, actor.Travel?.CurrentCityRuntimeId,
                StringComparison.Ordinal)
            && !string.Equals(candidate.LocationRuntimeId,
                actor.Travel?.CurrentLocationRuntimeId, StringComparison.Ordinal)
            && graph.TryPlanSelectedSoloTravel(actor,
                actor.Travel.CurrentLocationRuntimeId,
                candidate.LocationRuntimeId,
                candidate.RuntimeId,
                out SpatialRouteRuntime route,
                out float cost)
            && route != null
            && actor.MoneyAccount?.CanDebit(cost) == true
            && actor.TravelPlan?.CanCommit(
                candidate.LocationRuntimeId, candidate.RuntimeId,
                NpcTravelReason.Flee, 80f, cost, "decision/flee-city") == true);
        Assert.That(destination, Is.Not.Null,
            "The fixture must select a different reachable city with a valid FleeCity plan.");
        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, null, destination.RuntimeId, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/flee-city", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, action, payload, null), Is.True);
        if (actorArrested)
        {
            Assert.That(definitions.Statuses.TryGetValue(
                definitions.Configuration.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus), Is.True);
            Assert.That(actor.Status.TryAdd(arrestedStatus), Is.True,
                "The fixture admits the arrested status before sealing its P12 baseline.");
        }
        SealCoreOnlySelectedP12Census(composition);
        Assert.That(DailyV1CoreOnlyCrimeActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, actor,
                out IDailyV1SelectedActionDispatchBoundary boundary), Is.True);
        return new CoreOnlyFleeDispatchFixture(
            composition.ExecutionBoundaryState, graph, actor, destination, boundary);
    }

    private static CoreOnlyStealDispatchFixture CreateCoreOnlyStealDispatchFixture()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        string arrestedStatusId = definitions.Configuration?.arrestedStatus;
        DailyV1StatusDefinition arrestedStatus = null;
        if (!string.IsNullOrWhiteSpace(arrestedStatusId))
            definitions.Statuses.TryGetValue(arrestedStatusId, out arrestedStatus);
        DailyV1NpcRuntimeOwners perpetrator = graph.Npcs.FirstOrDefault(candidate =>
            candidate != null && candidate.Travel != null && !candidate.Travel.IsTraveling
            && candidate.Lifecycle?.LifeState == NpcLifeState.Alive
            && candidate.HiddenState?.RemainingDays <= 0
            && (arrestedStatus == null || candidate.Status?.Contains(arrestedStatus) != true));
        Assert.That(perpetrator, Is.Not.Null,
            "The authored selected profile must have an alive, present, non-arrested Steal actor.");
        DailyV1NpcRuntimeOwners victim = graph.Npcs.FirstOrDefault(candidate =>
            candidate != null && !ReferenceEquals(candidate, perpetrator)
            && string.Equals(candidate.Travel?.CurrentCityRuntimeId,
                perpetrator.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal)
            && !candidate.Travel.IsTraveling
            && candidate.Lifecycle?.LifeState == NpcLifeState.Alive
            && candidate.HiddenState?.RemainingDays <= 0
            && (arrestedStatus == null || candidate.Status?.Contains(arrestedStatus) != true)
            && candidate.MoneyAccount?.Balance >= 17f);
        Assert.That(victim, Is.Not.Null,
            "The authored selected profile must have a co-located victim with enough money for the fixture transfer.");

        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, victim.RuntimeId, null, null, 17, 0f, 1f,
            NpcTravelReason.None, 0f, "decision/core-steal", "theft/core-dispatch",
            null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(perpetrator, action, payload, null), Is.True);

        PersonId perpetratorPersonId = new PersonId("core-steal-perpetrator");
        PersonId victimPersonId = new PersonId("core-steal-victim");
        Assert.That(graph.PersonStore.TryRegister(
            new PersonRuntime(perpetratorPersonId), out _), Is.True);
        Assert.That(graph.PersonStore.TryRegister(
            new PersonRuntime(victimPersonId), out _), Is.True);
        Assert.That(graph.PersonStore.TryBindMaterializedNpc(
            perpetratorPersonId, perpetrator.RuntimeId, out _), Is.True);
        Assert.That(graph.PersonStore.TryBindMaterializedNpc(
            victimPersonId, victim.RuntimeId, out _), Is.True);

        SealCoreOnlySelectedP12Census(composition);
        Assert.That(DailyV1CoreOnlyCrimeActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, perpetrator,
                out IDailyV1SelectedActionDispatchBoundary boundary), Is.True);
        return new CoreOnlyStealDispatchFixture(
            composition.ExecutionBoundaryState, graph, perpetrator, victim, boundary);
    }

    private static void SealCoreOnlySelectedP12Census(
        DailyV1GenesisComposition composition)
    {
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
                composition, 0L, out DailyV1ActiveRuntimeSession session,
                out string diagnostic), Is.True, diagnostic);
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        OwnerSectionCensusSnapshot[] initialVector =
            DailyV1ApplicationSessionGateTests.CreateCoreBoundCompleteOwnerSectionVector(session);
        Dictionary<string, IOwnerSectionCensusProvider> liveProviders =
            DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph)
                .Concat(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs))
                .Concat(DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph))
                .ToDictionary(provider => provider.GetCurrentCensus().SectionId,
                    StringComparer.Ordinal);
        liveProviders[ScheduledDirectiveStore.SectionId] =
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives);
        liveProviders[DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId] =
            new TestWitnessOwnerSectionCensusProvider(
                () => graph.PersonStore.GetMembershipCensusWitness());
        liveProviders[DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId] =
            new TestWitnessOwnerSectionCensusProvider(
                () => graph.PersonStore.GetMaterializationBindingCensusWitness());
        foreach (PersonRuntime person in graph.PersonStore.Persons)
        {
            OwnerSectionCensusWitness witness = person.GetLifeResidenceCensusWitness();
            Assert.That(witness, Is.Not.Null);
            liveProviders[witness.SectionId] = new TestOwnerSectionCensusProvider(
                witness.SectionId, witness.SchemaVersion, person,
                () => 1,
                () => PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person));
        }
        OwnerSectionCensusSnapshot justiceBaseline = initialVector.Single(snapshot =>
            string.Equals(snapshot.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                StringComparison.Ordinal));
        liveProviders[DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId] =
            new TestOwnerSectionCensusProvider(
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                justiceBaseline.SchemaVersion, justiceBaseline.OwnerInstanceIdentity,
                () => graph.JusticeRecordsOwner.WantedRecords.Count
                    + graph.JusticeRecordsOwner.Sentences.Count,
                () => graph.JusticeRecordsOwner.Revision);

        CrimeSocialAppraisalWorldState crimeSocial = graph.P12EOwners?.CrimeSocialAppraisal;
        if (crimeSocial != null)
        {
            liveProviders[P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId] =
                new TestOwnerSectionCensusProvider(
                    P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId,
                    P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                    crimeSocial.TheftOutcomes,
                    () => crimeSocial.TheftOutcomes.Count,
                    () => crimeSocial.TheftOutcomes.P12CensusRevision);
            liveProviders[P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId] =
                new TestOwnerSectionCensusProvider(
                    P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId,
                    P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                    crimeSocial.CrimeKnowledge,
                    () => crimeSocial.CrimeKnowledge.Count,
                    () => crimeSocial.CrimeKnowledge.P12CensusRevision);
            liveProviders[P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId] =
                new TestOwnerSectionCensusProvider(
                    P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId,
                    P12CrimeSocialAppraisalOwnerContract.SchemaVersion,
                    crimeSocial.SocialReactions,
                    () => crimeSocial.SocialReactions.Count,
                    () => crimeSocial.SocialReactions.P12CensusRevision);
        }

        List<DailyV1OwnerSectionContractIdentity> contracts =
            new List<DailyV1OwnerSectionContractIdentity>(initialVector.Length);
        List<DailyV1OwnerSectionCensusProviderRegistration> registrations =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(initialVector.Length);
        foreach (OwnerSectionCensusSnapshot snapshot in initialVector)
        {
            IOwnerSectionCensusProvider provider;
            if (string.Equals(snapshot.SectionId,
                    RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal))
                provider = RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(
                    composition.RuntimeIdAllocator);
            else if (string.Equals(snapshot.SectionId,
                    RuntimeIdAllocatorCensusProvider.DecisionsSectionId, StringComparison.Ordinal))
                provider = RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(
                    composition.RuntimeIdAllocator);
            else if (string.Equals(snapshot.SectionId,
                    SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal))
                provider = new SimulationRecordSequenceCensusProvider(composition.RecordSequence);
            else if (!liveProviders.TryGetValue(snapshot.SectionId, out provider))
                provider = new BaselineOwnerSectionCensusProvider(snapshot);

            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
            registrations.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role, provider,
                snapshot.OwnerInstanceIdentity, snapshot.Cardinality, snapshot.Revision,
                hasBaseline: true));
        }

        Dictionary<string, DailyV1OwnerSectionContractIdentity> contractMap =
            contracts.ToDictionary(contract => contract.SectionId, StringComparer.Ordinal);
        Assert.That(registrations.Select(registration => registration.SectionId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(contracts.Count));
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in registrations)
        {
            Assert.That(contractMap.TryGetValue(registration.SectionId,
                out DailyV1OwnerSectionContractIdentity contract), Is.True, registration.SectionId);
            Assert.That(contract.SchemaVersion, Is.EqualTo(registration.SchemaVersion), registration.SectionId);
            Assert.That(contract.Role, Is.EqualTo(registration.Role), registration.SectionId);
            OwnerSectionCensusWitness witness = registration.Provider.GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(registration.SectionId), registration.SectionId);
            Assert.That(witness.SchemaVersion, Is.EqualTo(registration.SchemaVersion), registration.SectionId);
        }

        bool censusReplaced = state.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            contracts, registrations);
        Assert.That(censusReplaced, Is.True);

        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in registrations)
        {
            OwnerSectionCensusWitness witness = registration.Provider.GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(registration.SectionId), registration.SectionId);
            Assert.That(witness.SchemaVersion, Is.EqualTo(registration.SchemaVersion), registration.SectionId);
            Assert.That(witness.OwnerInstanceIdentity,
                Is.SameAs(registration.BaselineOwnerInstanceIdentity), registration.SectionId);
            Assert.That(witness.Cardinality, Is.EqualTo(registration.BaselineCardinality), registration.SectionId);
            Assert.That(witness.Revision, Is.EqualTo(registration.BaselineRevision), registration.SectionId);
        }
        OwnerSectionCensusSnapshot[] stagedSnapshots = registrations.Select(registration =>
            new OwnerSectionCensusSnapshot(
                registration.SectionId, registration.SchemaVersion, registration.Role,
                registration.BaselineOwnerInstanceIdentity,
                registration.BaselineCardinality, registration.BaselineRevision)).ToArray();
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
            stagedSnapshots), Is.True,
            "The Core P12 provider vector must preserve all 89 owner families and exact per-Person rows.");
        Assert.That(state.TrySealSelectedOwnerSectionCensusRegistry(registrations), Is.True);
        foreach (string operationContractId in
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: graph.TravelParties != null,
                hasTravelSystem: true))
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(
                operationContractId), Is.True, operationContractId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);
    }

    private static CoreGuardBoundaryFixture CreateCoreGuardBoundaryFixture()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        Assert.That(graph.Npcs.Count, Is.GreaterThanOrEqualTo(3));
        DailyV1NpcRuntimeOwners guard = graph.Npcs[0];
        DailyV1NpcRuntimeOwners target = graph.Npcs[1];
        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.ArrestActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, target.RuntimeId, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f,
            "decision/guard-arrest", null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(guard, action, payload, null), Is.True);

        RecordingP12Protocol protocol = new RecordingP12Protocol();
        object crimeIdentity = new object();
        object justiceIdentity = new object();
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding =
            new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                protocol,
                crimeIdentity, crimeIdentity, graph.JusticeRecordsOwner,
                justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
                admissionEnabled: true);
        RecordingCrimeMutationHostPort hostPort =
            new RecordingCrimeMutationHostPort(hostBinding);
        RecordingGuardProjection projection = new RecordingGuardProjection();
        Assert.That(DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedGuardArrestActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, guard, action, target,
                projection, hostPort,
                out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary), Is.True);
        Assert.That(composition.ExecutionBoundaryState
            .TryBindSelectedP12CrimeJusticeMutationProtocol(
                protocol, graph,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);
        return new CoreGuardBoundaryFixture(
            graph, guard, target, protocol, hostPort, boundary, coordinator);
    }

    private static CoreStealBoundaryFixture CreateCoreStealBoundaryFixture()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners perpetrator = graph.Npcs[0];
        string cityRuntimeId = perpetrator.Travel.CurrentCityRuntimeId;
        DailyV1CityRuntimeOwners city = graph.Cities.FirstOrDefault(
            candidate => string.Equals(candidate.RuntimeId, cityRuntimeId, StringComparison.Ordinal));
        Assert.That(city, Is.Not.Null);
        DailyV1NpcRuntimeOwners victim = graph.Npcs.FirstOrDefault(candidate =>
            !ReferenceEquals(candidate, perpetrator)
            && string.Equals(candidate.Travel?.CurrentCityRuntimeId,
                cityRuntimeId, StringComparison.Ordinal));
        Assert.That(victim, Is.Not.Null,
            "The selected fixture must contain an exact co-located victim owner.");

        DailyV1ActionDefinition action = ActionOfType(definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, action, victim.RuntimeId, null, null, 17, 1f, 1f,
            NpcTravelReason.None, 0f, "decision/core-steal", "theft/core-boundary",
            null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(perpetrator, action, payload, null), Is.True);

        RecordingP12Protocol protocol = new RecordingP12Protocol();
        object crimeIdentity = new object();
        object justiceIdentity = new object();
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding =
            new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                protocol,
                crimeIdentity, crimeIdentity, graph.JusticeRecordsOwner,
                justiceIdentity, justiceIdentity, graph.JusticeRecordsOwner,
                admissionEnabled: true);
        RecordingCrimeMutationHostPort hostPort =
            new RecordingCrimeMutationHostPort(hostBinding);
        PersonStore persons = new PersonStore();
        PersonId perpetratorPersonId = new PersonId("core-steal-perpetrator");
        PersonId victimPersonId = new PersonId("core-steal-victim");
        Assert.That(persons.TryRegister(new PersonRuntime(perpetratorPersonId), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(victimPersonId), out _), Is.True);
        Assert.That(persons.TryBindMaterializedNpc(perpetratorPersonId,
            perpetrator.RuntimeId, out _), Is.True);
        Assert.That(persons.TryBindMaterializedNpc(victimPersonId,
            victim.RuntimeId, out _), Is.True);
        CrimeSocialAppraisalWorldState socialWorld = new CrimeSocialAppraisalWorldState(
            persons, new InstitutionStore(), new SimulationTime(41L));
        RecordingStealProjection projection = new RecordingStealProjection(
            socialWorld, graph.JusticeRecordsOwner);

        Assert.That(DailyV1SelectedCrimeActionExecutionStage
            .DailyV1CoreSelectedStealActionBoundary.TryCreate(
                composition.ExecutionBoundaryState, graph, perpetrator, victim, city,
                action, projection, hostPort,
                out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary),
            Is.True);
        Assert.That(composition.ExecutionBoundaryState
            .TryBindSelectedP12CrimeJusticeMutationProtocol(
                protocol, graph,
                out DailyV1P12CrimeJusticeMutationCoordinator coordinator), Is.True);
        return new CoreStealBoundaryFixture(
            graph, perpetrator, victim, graph.Npcs.First(candidate =>
                !ReferenceEquals(candidate, perpetrator) && !ReferenceEquals(candidate, victim)),
            protocol, hostPort, projection, boundary, coordinator);
    }

    private sealed class CoreFleeBoundaryFixture
    {
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1ExecutionBoundaryState ExecutionState { get; }
        internal DailyV1NpcRuntimeOwners Actor { get; }
        internal DailyV1CityRuntimeOwners Destination { get; }
        internal RecordingP12Protocol Protocol { get; }
        internal RecordingCrimeMutationHostPort HostPort { get; }
        internal RecordingFleeProjection Projection { get; }
        internal DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary Boundary { get; }
        internal DailyV1P12CrimeJusticeMutationCoordinator Coordinator { get; }

        internal CoreFleeBoundaryFixture(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination,
            RecordingP12Protocol protocol,
            RecordingCrimeMutationHostPort hostPort,
            RecordingFleeProjection projection,
            DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary,
            DailyV1P12CrimeJusticeMutationCoordinator coordinator)
        {
            ExecutionState = executionState;
            Graph = graph;
            Actor = actor;
            Destination = destination;
            Protocol = protocol;
            HostPort = hostPort;
            Projection = projection;
            Boundary = boundary;
            Coordinator = coordinator;
        }
    }

    private sealed class CoreOnlyFleeDispatchFixture
    {
        internal DailyV1ExecutionBoundaryState ExecutionState { get; }
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1NpcRuntimeOwners Actor { get; }
        internal DailyV1CityRuntimeOwners Destination { get; }
        internal IDailyV1SelectedActionDispatchBoundary Boundary { get; }

        internal CoreOnlyFleeDispatchFixture(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination,
            IDailyV1SelectedActionDispatchBoundary boundary)
        {
            ExecutionState = executionState;
            Graph = graph;
            Actor = actor;
            Destination = destination;
            Boundary = boundary;
        }
    }

    private sealed class CoreOnlyStealDispatchFixture
    {
        internal DailyV1ExecutionBoundaryState ExecutionState { get; }
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1NpcRuntimeOwners Perpetrator { get; }
        internal DailyV1NpcRuntimeOwners Victim { get; }
        internal IDailyV1SelectedActionDispatchBoundary Boundary { get; }

        internal CoreOnlyStealDispatchFixture(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim,
            IDailyV1SelectedActionDispatchBoundary boundary)
        {
            ExecutionState = executionState;
            Graph = graph;
            Perpetrator = perpetrator;
            Victim = victim;
            Boundary = boundary;
        }
    }

    private sealed class TestOwnerSectionCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly string sectionId;
        private readonly int schemaVersion;
        private readonly object owner;
        private readonly Func<int> cardinality;
        private readonly Func<long> revision;

        internal TestOwnerSectionCensusProvider(
            string sectionId,
            int schemaVersion,
            object owner,
            Func<int> cardinality,
            Func<long> revision)
        {
            this.sectionId = sectionId;
            this.schemaVersion = schemaVersion;
            this.owner = owner;
            this.cardinality = cardinality;
            this.revision = revision;
        }

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(
                sectionId, schemaVersion, owner, cardinality(), revision());
    }

    private sealed class TestWitnessOwnerSectionCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly Func<OwnerSectionCensusWitness> getWitness;

        internal TestWitnessOwnerSectionCensusProvider(
            Func<OwnerSectionCensusWitness> getWitness) =>
            this.getWitness = getWitness;

        public OwnerSectionCensusWitness GetCurrentCensus() => getWitness();
    }

    private sealed class BaselineOwnerSectionCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly OwnerSectionCensusWitness witness;

        internal BaselineOwnerSectionCensusProvider(OwnerSectionCensusSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            witness = new OwnerSectionCensusWitness(
                snapshot.SectionId, snapshot.SchemaVersion,
                snapshot.OwnerInstanceIdentity, snapshot.Cardinality, snapshot.Revision);
        }

        public OwnerSectionCensusWitness GetCurrentCensus() => witness;
    }

    private sealed class CoreGuardBoundaryFixture
    {
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1NpcRuntimeOwners Guard { get; }
        internal DailyV1NpcRuntimeOwners Target { get; }
        internal RecordingP12Protocol Protocol { get; }
        internal RecordingCrimeMutationHostPort HostPort { get; }
        internal DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary Boundary { get; }
        internal DailyV1P12CrimeJusticeMutationCoordinator Coordinator { get; }

        internal CoreGuardBoundaryFixture(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners guard,
            DailyV1NpcRuntimeOwners target,
            RecordingP12Protocol protocol,
            RecordingCrimeMutationHostPort hostPort,
            DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary,
            DailyV1P12CrimeJusticeMutationCoordinator coordinator)
        {
            Graph = graph;
            Guard = guard;
            Target = target;
            Protocol = protocol;
            HostPort = hostPort;
            Boundary = boundary;
            Coordinator = coordinator;
        }
    }

    private sealed class CoreStealBoundaryFixture
    {
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1NpcRuntimeOwners Perpetrator { get; }
        internal DailyV1NpcRuntimeOwners Victim { get; }
        internal DailyV1NpcRuntimeOwners Unrelated { get; }
        internal RecordingP12Protocol Protocol { get; }
        internal RecordingCrimeMutationHostPort HostPort { get; }
        internal RecordingStealProjection Projection { get; }
        internal DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary Boundary { get; }
        internal DailyV1P12CrimeJusticeMutationCoordinator Coordinator { get; }

        internal CoreStealBoundaryFixture(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim,
            DailyV1NpcRuntimeOwners unrelated,
            RecordingP12Protocol protocol,
            RecordingCrimeMutationHostPort hostPort,
            RecordingStealProjection projection,
            DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary,
            DailyV1P12CrimeJusticeMutationCoordinator coordinator)
        {
            Graph = graph;
            Perpetrator = perpetrator;
            Victim = victim;
            Unrelated = unrelated;
            Protocol = protocol;
            HostPort = hostPort;
            Projection = projection;
            Boundary = boundary;
            Coordinator = coordinator;
        }
    }

    private static void CommitAction(
        CrimeFixture fixture,
        DailyV1ActionDefinition action,
        float multiplier)
    {
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            fixture.Definitions, action, null, null, null, 0, 0f,
            multiplier, NpcTravelReason.None, 0f, null, null, null, null,
            out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(fixture.Graph.TryCommitCurrentActionSlot(
            fixture.Actor, action, payload, null), Is.True);
    }

    private static CrimeFixture CreateFixture()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition cityDefinition = definitions.Cities.Values.First();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city/crime", "location/crime", definitions, cityDefinition, null,
            out DailyV1CityRuntimeOwners city), Is.True);
        DailyV1NpcDefinition actorDefinition = definitions.Npcs["MARCUS"];
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/crime", definitions, actorDefinition, 0f,
            out DailyV1NpcRuntimeOwners actor), Is.True);
        Assert.That(actor.Travel.TryInstallState(
            city.LocationRuntimeId, city.RuntimeId, null, null, 0, 0,
            null, false, null, null, 0L), Is.True);
        Assert.That(city.NpcPresence.TryAdd(actor.RuntimeId), Is.True);

        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        int justiceCommitCallbacks = 0;
        Assert.That(justice.TryBindP12MutationBoundary(
            () => true, () => justiceCommitCallbacks++), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { city }, new[] { actor }, null, justice,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return new CrimeFixture(definitions, graph, city, actor, justice,
            () => justiceCommitCallbacks);
    }

    private sealed class CrimeFixture
    {
        internal DailyV1EffectiveDefinitions Definitions { get; }
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1CityRuntimeOwners City { get; }
        internal DailyV1NpcRuntimeOwners Actor { get; }
        internal DailyV1JusticeRecordsOwner Justice { get; }
        internal Func<int> JusticeCallbacks { get; }

        internal CrimeFixture(DailyV1EffectiveDefinitions definitions,
            DailyV1SelectedRuntimeOwnerGraph graph, DailyV1CityRuntimeOwners city,
            DailyV1NpcRuntimeOwners actor, DailyV1JusticeRecordsOwner justice,
            Func<int> justiceCallbacks)
        {
            Definitions = definitions; Graph = graph; City = city; Actor = actor;
            Justice = justice; JusticeCallbacks = justiceCallbacks;
        }

        internal DailyV1JusticeWantedRecordOwner AddWarrant(float bounty, int sentenceDays = 4)
        {
            DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
                Actor.RuntimeId, City.RuntimeId, bounty, sentenceDays);
            Assert.That(Justice.TryAddWantedRecord(warrant), Is.True);
            Assert.That(Justice.NotifyMutationCommitted(), Is.True);
            return warrant;
        }

        internal DailyV1JusticeSentenceOwner AddSentence(
            DailyV1JusticeWantedRecordOwner warrant, int remainingDays, int failedAttempts,
            bool wasArrestedToday = false)
        {
            DailyV1JusticeSentenceOwner sentence = new DailyV1JusticeSentenceOwner(
                Actor.RuntimeId, City.RuntimeId, warrant, remainingDays,
                failedAttempts, wasArrestedToday);
            Assert.That(Justice.TryAddSentence(sentence), Is.True);
            Assert.That(Justice.NotifyMutationCommitted(), Is.True);
            return sentence;
        }

        internal DailyV1StatusDefinition AddArrestedStatus()
        {
            DailyV1StatusDefinition status = Definitions.Statuses[
                Definitions.Configuration.arrestedStatus];
            Actor.Status.TryAdd(status);
            return status;
        }
    }

    private sealed class FixedActionRandom : IAuthoritativeRandomSource
    {
        private readonly float value;
        internal FixedActionRandom(float value) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L) => value;
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class DispatchBoundary : IDailyV1SelectedActionDispatchBoundary
    {
        private readonly RecordingCrimeBoundary crime;
        internal DispatchBoundary(RecordingCrimeBoundary crime) => this.crime = crime;
        public bool TryGetSoloTravelBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetMarketBoundaries(DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
            out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
        { saleBoundary = null; purchaseBoundary = null; return false; }
        public bool TryGetNpcTradeBoundary(DailyV1NpcRuntimeOwners seller, DailyV1NpcRuntimeOwners buyer,
            out IDailyV1SelectedNpcTradeBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetFleeCityActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetGuardArrestActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetStealActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetCrimeActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = crime; return boundary != null; }
        public bool TryGetForcedEscapeBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = null; return false; }
        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public void CompleteCrossCityBuy(DailyV1NpcRuntimeOwners actor, DailyV1SelectedCrossCityBuyResult result) { }
    }

    private sealed class RecordingCrimeBoundary
        : DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary,
            DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary
    {
        private readonly CrimeFixture fixture;
        internal List<string> Order { get; } = new List<string>();
        internal int ActorMutationNotifications { get; private set; }
        internal int MutationAdmissions { get; private set; }
        public DailyV1JusticeRecordsOwner JusticeRecordsOwner => fixture.Justice;

        internal RecordingCrimeBoundary(CrimeFixture fixture) => this.fixture = fixture;

        public bool ExecuteWithinOperation(Func<bool> operation)
        {
            Order.Add("p12-begin");
            try { return operation != null && operation(); }
            finally { Order.Add("p12-end"); }
        }

        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners actor,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => actor?.Status != null;

        public void RefreshStatusProjection(DailyV1NpcRuntimeOwners actor) { }

        public void SyncHiddenStateProjection(DailyV1NpcRuntimeOwners actor) { }

        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners actor) => MutationAdmissions++;

        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners actor,
            DailyV1StatusDefinition status,
            bool added)
        {
            ActorMutationNotifications++;
            Order.Add(added ? "status-add" : "status-remove");
        }

        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners actor)
        {
            ActorMutationNotifications++;
            Order.Add(actor.HiddenState.RemainingDays > 0 ? "hidden-duration" : "hidden-clear");
        }

        public bool ApplyHideDuration(DailyV1NpcRuntimeOwners actor, int followingFullDays)
        {
            Order.Add("hidden-duration");
            int next = Math.Max(actor.HiddenState.RemainingDays, Math.Max(1, followingFullDays) + 1);
            if (next != actor.HiddenState.RemainingDays)
            {
                actor.HiddenState.TrySetRemainingDays(next);
                ActorMutationNotifications++;
            }
            return true;
        }

        public bool ClearHidden(DailyV1NpcRuntimeOwners actor)
        {
            Order.Add("hidden-clear");
            if (actor.HiddenState.RemainingDays != 0)
            {
                actor.HiddenState.TrySetRemainingDays(0);
                ActorMutationNotifications++;
            }
            return true;
        }

        public bool ApplyStatusChange(DailyV1NpcRuntimeOwners actor,
            DailyV1StatusDefinition status, bool add)
        {
            bool changed = add ? actor.Status.TryAdd(status) : actor.Status.TryRemove(status);
            if (changed) ActorMutationNotifications++;
            Order.Add(add ? "status-add" : "status-remove");
            return true;
        }

        public DailyV1SelectedActionStatusPlanResult ExecuteWithinOperation(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcRuntimeOwners target,
            Func<DailyV1SelectedActionStatusPlanResult> operation)
        {
            Order.Add("status-plan");
            return operation?.Invoke() ?? DailyV1SelectedActionStatusPlanResult.ActorRejected;
        }

        public bool ApplyActorStatusChanges(DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
        {
            if (!ReferenceEquals(owner, fixture.Actor)) return false;
            foreach (DailyV1StatusDefinition status in statusToRemove ?? Array.Empty<DailyV1StatusDefinition>())
                ApplyStatusChange(owner, status, add: false);
            foreach (DailyV1StatusDefinition status in statusToAdd ?? Array.Empty<DailyV1StatusDefinition>())
                ApplyStatusChange(owner, status, add: true);
            return true;
        }

        public bool ApplyTargetStatusChanges(DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => false;

        public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
            fixture.Justice.ContainsSentence(sentence);

        public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence)
        {
            Order.Add("sentence-remove");
        }

        public void PublishHidden(DailyV1NpcRuntimeOwners actor) => Order.Add("hidden-log");
        public void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty) =>
            Order.Add("failed-escape-log");
        public void PublishEscaped(DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners eventCity, bool hadActiveSentence,
            float bountyAfterPenalty, string originDecisionId) => Order.Add("escape-event");
    }

    private sealed class RecordingCrimeMutationHostPort
        : IDailyV1SelectedCrimeJusticeDailyMutationHostPort
    {
        internal DailyV1SelectedCrimeJusticeDailyMutationHostBinding Binding { get; set; }
        internal int BindingRequests { get; private set; }

        internal RecordingCrimeMutationHostPort(
            DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding) =>
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));

        public bool TryGetSelectedCrimeJusticeDailyMutationHostBinding(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1SelectedCrimeJusticeDailyMutationHostBinding result)
        {
            BindingRequests++;
            result = Binding;
            return Binding != null && Binding.HasExactCoreOwnerRelationships(graph);
        }
    }

    private sealed class RecordingCrimeProjection
        : DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionProjectionBoundary
    {
        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners actor,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => actor?.Status != null;
        public void RefreshStatusProjection(DailyV1NpcRuntimeOwners actor) { }
        public void SyncHiddenStateProjection(DailyV1NpcRuntimeOwners actor) { }
        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners actor) { }
        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners actor, DailyV1StatusDefinition status, bool added) { }
        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners actor) { }
        public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) => true;
        public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) { }
        public void PublishHidden(DailyV1NpcRuntimeOwners actor) { }
        public void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty) { }
        public void PublishEscaped(
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners eventCity,
            bool hadActiveSentence,
            float bountyAfterPenalty,
            string originDecisionId) { }
    }

    private sealed class RecordingFleeProjection
        : DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionProjectionBoundary
    {
        internal int PreflightCalls { get; private set; }
        internal int AdmissionCalls { get; private set; }
        internal int PublicationCalls { get; private set; }

        public bool CanPrepareTravelPlanProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination,
            float travelCost,
            string originDecisionId)
        {
            PreflightCalls++;
            return actor != null && destination != null && travelCost > 0f;
        }

        public bool CanAdmitTravelPlanMutation()
        {
            AdmissionCalls++;
            return true;
        }

        public void PublishCommittedTravelPlan(
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners destination,
            float travelCost,
            string originDecisionId) => PublicationCalls++;
    }

    private sealed class RecordingGuardProjection
        : DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionProjectionBoundary
    {
        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => owner != null;
        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner) { }
        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners owner, DailyV1StatusDefinition status, bool added) { }
        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner) { }
        public bool TryPrepareSentenceProjection(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner warrant,
            DailyV1JusticeSentenceOwner sentence) =>
            target != null && city != null && warrant != null && sentence != null;
        public void DiscardPreparedSentenceProjection(DailyV1JusticeSentenceOwner sentence) { }
        public bool CommitPreparedSentenceProjection(DailyV1JusticeSentenceOwner sentence) => sentence != null;
        public void PublishArrest(
            DailyV1NpcRuntimeOwners guard,
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            string originDecisionId) { }
    }

    private sealed class RecordingStealProjection
        : DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary
    {
        internal int LegacyOperationCalls { get; private set; }
        internal int TransferCalls { get; private set; }
        internal DailyV1NpcRuntimeOwners TransferSource { get; private set; }
        internal DailyV1NpcRuntimeOwners TransferDestination { get; private set; }
        public CrimeSocialAppraisalWorldState SocialAppraisalWorldState { get; }
        public ITheftOutcomeSink TheftOutcomeSink => SocialAppraisalWorldState.Integration;
        public DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }
        public string LastJusticeAdmissionRejection => null;

        internal RecordingStealProjection(
            CrimeSocialAppraisalWorldState socialWorld,
            DailyV1JusticeRecordsOwner justiceRecordsOwner)
        {
            SocialAppraisalWorldState = socialWorld;
            JusticeRecordsOwner = justiceRecordsOwner;
        }

        public bool ExecuteWithinOperation(Func<bool> operation)
        {
            LegacyOperationCalls++;
            return operation != null && operation();
        }

        public EconomyTransactionResult ExecuteMoneyTransferWithinOperation(
            DailyV1NpcRuntimeOwners source,
            DailyV1NpcRuntimeOwners destination,
            Func<EconomyTransactionResult> operation)
        {
            TransferCalls++;
            TransferSource = source;
            TransferDestination = destination;
            return operation?.Invoke();
        }

        public void RefreshStatusProjection(DailyV1NpcRuntimeOwners owner) { }

        public void SyncHiddenStateProjection(DailyV1NpcRuntimeOwners owner) { }

        public bool TryProjectWantedRecord(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner record) => record != null;

        public bool TryPrepareWantedRecordProjection(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner record) => record != null;

        public void DiscardPreparedWantedRecordProjection(
            DailyV1JusticeWantedRecordOwner record) { }

        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) => owner?.Status != null;

        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner) { }

        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners owner,
            DailyV1StatusDefinition status,
            bool added) { }

        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner) { }

        public void PublishTheft(
            DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim,
            int amount) { }

        public void LogCompensationFailure() { }

        public void PublishWantedRecord(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner record) { }
    }

    private sealed class RecordingP12Protocol : IDailyV1P12CrimeJusticeMutationProtocol
    {
        internal List<string[]> ValidationCalls { get; } = new List<string[]>();
        internal List<string[]> RefreshedSections { get; } = new List<string[]>();
        internal IReadOnlyList<string> ValidatedSections { get; private set; }
        internal bool Faulted { get; private set; }

        public void FaultClosed() => Faulted = true;
        public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds)
        {
            ValidatedSections = sectionIds?.ToArray();
            if (ValidatedSections != null)
                ValidationCalls.Add(ValidatedSections.ToArray());
            return sectionIds != null;
        }
        public bool TryRefreshCommittedMutationBaselines(IReadOnlyCollection<string> sectionIds)
        {
            if (sectionIds != null) RefreshedSections.Add(sectionIds.ToArray());
            return sectionIds != null;
        }

        public bool TryRefreshCommittedMutationBaselinesForReservation(
            object reservationToken,
            IReadOnlyCollection<string> sectionIds) =>
            reservationToken != null && sectionIds != null;

        public bool TryReserveMutationEpoch(out object reservation)
        { reservation = null; return false; }
        public bool TryNotifyReservedCommittedMutations(
            object reservation, IReadOnlyCollection<string> sectionIds) => false;
        public void ReleaseMutationEpoch(object reservation) { }
        public bool TryValidateMutationEpochCapacity() => true;
        public bool NotifyCommittedMutation(string sectionId) => true;
    }

    private sealed class RecordingDecisionBoundary : IDailyV1SelectedNpcActionDecisionBoundary
    {
        internal int LegacyProviderPlanCalls { get; private set; }
        public bool TryProjectSelectedCandidate(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate) => candidate?.Action != null;
        public bool HasTravelActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public TravelPartyStore GetTravelPartyStore(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => null;
        public bool TryProjectTravelAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool HasMerchantActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public bool HasEnabledCrimeActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => true;
        public bool TryProjectBuyAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectSellAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectStealAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectFleeCityAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryPrepareAutonomousProviderAction(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate, out float utility)
        { LegacyProviderPlanCalls++; utility = 0f; return false; }
    }
}
