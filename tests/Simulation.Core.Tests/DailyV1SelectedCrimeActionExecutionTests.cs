using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedCrimeActionExecutionTests
{
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
            "p12-begin", "sentence-remove", "status-remove", "status-add",
            "status-add", "escape-event", "p12-end" }));
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
            "p12-begin", "status-remove", "status-add", "hidden-clear",
            "status-remove", "status-remove", "escape-event", "p12-end" }));
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
        RecordingDecisionBoundary boundary = new RecordingDecisionBoundary();

        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryChoose(
            fixture.Graph, fixture.Actor, new[] { escape }, fixture.Actor.Status.Statuses,
            12L, new FixedActionRandom(0f), boundary, out DailyV1NpcActionCandidate selected), Is.True);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected.Action, Is.SameAs(escape));
        Assert.That(selected.Utility, Is.EqualTo(30f));
        Assert.That(selected.PreparedPayload.SuccessChanceMultiplier,
            Is.EqualTo(0.64f).Within(0.000001f));
        Assert.That(boundary.LegacyProviderPlanCalls, Is.Zero);
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
            DailyV1JusticeWantedRecordOwner warrant, int remainingDays, int failedAttempts)
        {
            DailyV1JusticeSentenceOwner sentence = new DailyV1JusticeSentenceOwner(
                Actor.RuntimeId, City.RuntimeId, warrant, remainingDays,
                failedAttempts, wasArrestedToday: false);
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
            out TravelPartyStore partyStore, out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        { partyStore = null; boundary = null; return false; }
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
        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public void CompleteCrossCityBuy(DailyV1NpcRuntimeOwners actor, DailyV1SelectedCrossCityBuyResult result) { }
    }

    private sealed class RecordingCrimeBoundary
        : DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary
    {
        private readonly CrimeFixture fixture;
        internal List<string> Order { get; } = new List<string>();
        internal int ActorMutationNotifications { get; private set; }
        public DailyV1JusticeRecordsOwner JusticeRecordsOwner => fixture.Justice;

        internal RecordingCrimeBoundary(CrimeFixture fixture) => this.fixture = fixture;

        public bool ExecuteWithinOperation(Func<bool> operation)
        {
            Order.Add("p12-begin");
            try { return operation != null && operation(); }
            finally { Order.Add("p12-end"); }
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

        public bool TryRemoveSentence(DailyV1JusticeSentenceOwner sentence)
        {
            bool removed = fixture.Justice.TryRemoveSentence(sentence);
            if (removed) Order.Add("sentence-remove");
            return removed;
        }

        public void PublishHidden(DailyV1NpcRuntimeOwners actor) => Order.Add("hidden-log");
        public void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty) =>
            Order.Add("failed-escape-log");
        public void PublishEscaped(DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners eventCity, bool hadActiveSentence,
            float bountyAfterPenalty, string originDecisionId) => Order.Add("escape-event");
    }

    private sealed class RecordingDecisionBoundary : IDailyV1SelectedNpcActionDecisionBoundary
    {
        internal int LegacyProviderPlanCalls { get; private set; }
        public bool HasTravelActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public TravelPartyStore GetTravelPartyStore(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => null;
        public bool TryProjectTravelAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool HasMerchantActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public bool HasEnabledCrimeActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => true;
        public bool TryProjectBuyAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectSellAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryPrepareAutonomousProviderAction(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate, out float utility)
        { LegacyProviderPlanCalls++; utility = 0f; return false; }
    }
}
