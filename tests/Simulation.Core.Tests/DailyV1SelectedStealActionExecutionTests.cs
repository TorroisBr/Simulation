using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedStealActionExecutionTests
{
    [Test]
    public void SelectedStealUsesCoreMoneySocialAndJusticeOwnersInLegacyOrder()
    {
        StealFixture fixture = CreateFixture();
        RecordingStealBoundary operation = new RecordingStealBoundary(fixture.SocialWorld, fixture.Justice);
        DispatchBoundary dispatchBoundary = new DispatchBoundary(operation);
        float perpetratorBefore = fixture.Perpetrator.MoneyAccount.Balance;
        float victimBefore = fixture.Victim.MoneyAccount.Balance;

        Assert.That(DailyV1SelectedActionDispatchStage.TryDispatch(
            fixture.Graph, fixture.Perpetrator, new FixedActionRandom(0f),
            fixture.SocialWorld.SimulationTime.AbsoluteDay, dispatchBoundary,
            out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Kind, Is.EqualTo(DailyV1SelectedActionDispatchKind.Steal));
        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(operation.Order, Is.EqualTo(new[] {
            "p12-begin", "transfer", "theft-log", "wanted-status", "warrant-log", "p12-end" }));
        Assert.That(fixture.Perpetrator.MoneyAccount.Balance, Is.GreaterThan(perpetratorBefore));
        Assert.That(fixture.Victim.MoneyAccount.Balance, Is.LessThan(victimBefore));
        Assert.That(fixture.Justice.WantedRecords, Has.Count.EqualTo(1));
        Assert.That(fixture.Justice.WantedRecords[0].TargetRuntimeId,
            Is.EqualTo(fixture.Perpetrator.RuntimeId));
        DailyV1StatusDefinition wantedStatus = fixture.Definitions.Statuses[
            fixture.Definitions.Configuration.wantedStatus];
        Assert.That(fixture.Perpetrator.Status.Statuses, Does.Contain(wantedStatus));
        Assert.That(fixture.Victim.Status.Statuses, Does.Not.Contain(wantedStatus));
        Assert.That(fixture.Justice.Revision, Is.Zero,
            "This Core-only fixture has no P12 Justice revision callback bound.");
        Assert.That(fixture.SocialWorld.TheftOutcomes.Count, Is.EqualTo(1));
        Assert.That(operation.ProjectedWantedRecord, Is.SameAs(fixture.Justice.WantedRecords[0]));
    }

    [Test]
    public void SelectedStealRejectsSocialPreflightBeforeTransferOrLogging()
    {
        StealFixture fixture = CreateFixture();
        TheftOutcome duplicate = fixture.CreateOutcome();
        Assert.That(fixture.SocialWorld.TheftOutcomes.TryRecord(
            duplicate, out CrimeOutcomeStoreFailure preseedFailure), Is.True,
            preseedFailure.ToString());
        RecordingStealBoundary operation = new RecordingStealBoundary(fixture.SocialWorld, fixture.Justice);

        Assert.That(Dispatch(fixture, operation, out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(operation.TransferCalls, Is.Zero);
        Assert.That(operation.Order, Is.EqualTo(new[] { "p12-begin", "p12-end" }));
        Assert.That(fixture.Justice.WantedRecords, Is.Empty);
    }

    [Test]
    public void SelectedStealUsesTheBoundaryTheftSinkForPreflightAndCommit()
    {
        StealFixture fixture = CreateFixture();
        RecordingStealBoundary operation = new RecordingStealBoundary(fixture.SocialWorld, fixture.Justice);
        CountingTheftOutcomeSink sink = new CountingTheftOutcomeSink(fixture.SocialWorld.Integration);
        operation.TheftOutcomeSink = sink;

        Assert.That(Dispatch(fixture, operation, out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(sink.CanAcceptCalls, Is.EqualTo(1));
        Assert.That(sink.AcceptCalls, Is.EqualTo(1));
        Assert.That(fixture.SocialWorld.TheftOutcomes.Count, Is.EqualTo(1));
    }

    [Test]
    public void SelectedStealCompensatesRejectedSocialCommitAndWarnsOnFailedRollback()
    {
        StealFixture fixture = CreateFixture();
        RecordingStealBoundary operation = new RecordingStealBoundary(fixture.SocialWorld, fixture.Justice)
        {
            InjectDuplicateAfterFirstTransfer = true,
            FailRollback = true
        };
        float perpetratorBefore = fixture.Perpetrator.MoneyAccount.Balance;
        float victimBefore = fixture.Victim.MoneyAccount.Balance;

        Assert.That(Dispatch(fixture, operation, out DailyV1SelectedActionDispatchResult result), Is.True);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(operation.Order, Is.EqualTo(new[] {
            "p12-begin", "transfer", "theft-log", "rollback", "compensation-warning", "p12-end" }));
        Assert.That(fixture.Perpetrator.MoneyAccount.Balance, Is.GreaterThan(perpetratorBefore));
        Assert.That(fixture.Victim.MoneyAccount.Balance, Is.LessThan(victimBefore));
        Assert.That(fixture.Justice.WantedRecords, Is.Empty);
    }

    private static bool Dispatch(
        StealFixture fixture,
        RecordingStealBoundary operation,
        out DailyV1SelectedActionDispatchResult result) =>
        DailyV1SelectedActionDispatchStage.TryDispatch(
            fixture.Graph, fixture.Perpetrator, new FixedActionRandom(0f),
            fixture.SocialWorld.SimulationTime.AbsoluteDay,
            new DispatchBoundary(operation), out result);

    private static StealFixture CreateFixture()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition cityDefinition = definitions.Cities.Values.First();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city/steal", "location/steal", definitions, cityDefinition, null,
            out DailyV1CityRuntimeOwners city), Is.True);
        DailyV1NpcDefinition perpetratorDefinition = definitions.Npcs.Values.First();
        DailyV1NpcDefinition victimDefinition = definitions.Npcs.Values.Skip(1).First();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/perpetrator", definitions, perpetratorDefinition, 0f,
            out DailyV1NpcRuntimeOwners perpetrator), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/victim", definitions, victimDefinition, 100f,
            out DailyV1NpcRuntimeOwners victim), Is.True);
        Assert.That(perpetrator.Travel.TryInstallState(
            city.LocationRuntimeId, city.RuntimeId, null, null, 0, 0,
            null, false, null, null, 0L), Is.True);
        Assert.That(victim.Travel.TryInstallState(
            city.LocationRuntimeId, city.RuntimeId, null, null, 0, 0,
            null, false, null, null, 0L), Is.True);
        Assert.That(city.NpcPresence.TryAdd(perpetrator.RuntimeId), Is.True);
        Assert.That(city.NpcPresence.TryAdd(victim.RuntimeId), Is.True);

        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { city }, new[] { perpetrator, victim }, null, justice,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1ActionDefinition steal = definitions.Actions.Values.First(
            value => value.Values.actionType?.Value == DailyV1NpcDefinitionRules.StealActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            definitions, steal, victim.RuntimeId, null, null, 17, 1f, 1f,
            NpcTravelReason.None, 0f, "decision/steal", "theft/selected-slot",
            null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(perpetrator, steal, payload, null), Is.True);

        PersonStore persons = new PersonStore();
        PersonId perpetratorPersonId = new PersonId("person/perpetrator");
        PersonId victimPersonId = new PersonId("person/victim");
        Assert.That(persons.TryRegister(new PersonRuntime(perpetratorPersonId), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(victimPersonId), out _), Is.True);
        Assert.That(persons.TryBindMaterializedNpc(perpetratorPersonId,
            perpetrator.RuntimeId, out _), Is.True);
        Assert.That(persons.TryBindMaterializedNpc(victimPersonId, victim.RuntimeId, out _), Is.True);
        CrimeSocialAppraisalWorldState socialWorld = new CrimeSocialAppraisalWorldState(
            persons, new InstitutionStore(), new SimulationTime(41L));
        return new StealFixture(definitions, graph, justice, perpetrator,
            victim, socialWorld, payload);
    }

    private sealed class StealFixture
    {
        internal DailyV1EffectiveDefinitions Definitions { get; }
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1JusticeRecordsOwner Justice { get; }
        internal DailyV1NpcRuntimeOwners Perpetrator { get; }
        internal DailyV1NpcRuntimeOwners Victim { get; }
        internal CrimeSocialAppraisalWorldState SocialWorld { get; }
        internal DailyV1NpcActionPayloadOwner Payload { get; }

        internal StealFixture(
            DailyV1EffectiveDefinitions definitions,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1JusticeRecordsOwner justice,
            DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim,
            CrimeSocialAppraisalWorldState socialWorld,
            DailyV1NpcActionPayloadOwner payload)
        {
            Definitions = definitions;
            Graph = graph;
            Justice = justice;
            Perpetrator = perpetrator;
            Victim = victim;
            SocialWorld = socialWorld;
            Payload = payload;
        }

        internal TheftOutcome CreateOutcome()
        {
            Assert.That(SocialWorld.PersonStore.TryGetByMaterializedNpcRuntimeId(
                Perpetrator.RuntimeId, out PersonRuntime perpetrator), Is.True);
            Assert.That(SocialWorld.PersonStore.TryGetByMaterializedNpcRuntimeId(
                Victim.RuntimeId, out PersonRuntime victim), Is.True);
            long day = SocialWorld.SimulationTime.AbsoluteDay;
            TheftOutcomeId id = TheftOutcomeId.Create(
                perpetrator.PersonId, victim.PersonId, day, Payload.StableOccurrenceKey);
            return new TheftOutcome(id, perpetrator.PersonId, victim.PersonId,
                Payload.Amount, day, Payload.StableOccurrenceKey, Payload.OriginDecisionId);
        }
    }

    private sealed class FixedActionRandom : IAuthoritativeRandomSource
    {
        private readonly float value;
        internal FixedActionRandom(float value) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L) => value;
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class CountingTheftOutcomeSink : ITheftOutcomeSink
    {
        private readonly ITheftOutcomeSink inner;
        internal int CanAcceptCalls { get; private set; }
        internal int AcceptCalls { get; private set; }

        internal CountingTheftOutcomeSink(ITheftOutcomeSink inner) => this.inner = inner;
        public bool CanAcceptTheftOutcome(TheftOutcome outcome)
        {
            CanAcceptCalls++;
            return inner.CanAcceptTheftOutcome(outcome);
        }
        public bool TryAcceptTheftOutcome(TheftOutcome outcome)
        {
            AcceptCalls++;
            return inner.TryAcceptTheftOutcome(outcome);
        }
    }

    private sealed class DispatchBoundary : IDailyV1SelectedActionDispatchBoundary
    {
        private readonly RecordingStealBoundary stealBoundary;
        internal DispatchBoundary(RecordingStealBoundary stealBoundary) => this.stealBoundary = stealBoundary;
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
        { boundary = stealBoundary; return boundary != null; }
        public bool TryGetCrimeActionBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = null; return false; }
        public bool TryGetForcedEscapeBoundary(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        { boundary = null; return false; }
        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor) { }
        public void CompleteCrossCityBuy(DailyV1NpcRuntimeOwners actor, DailyV1SelectedCrossCityBuyResult result) { }
    }

    private sealed class RecordingStealBoundary
        : DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary
    {
        private readonly List<string> order = new List<string>();
        internal IReadOnlyList<string> Order => order;
        internal CrimeSocialAppraisalWorldState SocialWorld { get; }
        public DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }
        internal int TransferCalls { get; private set; }
        internal bool InjectDuplicateAfterFirstTransfer { get; set; }
        internal bool FailRollback { get; set; }
        internal DailyV1JusticeWantedRecordOwner ProjectedWantedRecord { get; private set; }

        public CrimeSocialAppraisalWorldState SocialAppraisalWorldState => SocialWorld;
        public ITheftOutcomeSink TheftOutcomeSink { get; set; }
        internal RecordingStealBoundary(
            CrimeSocialAppraisalWorldState socialWorld,
            DailyV1JusticeRecordsOwner justiceRecordsOwner)
        {
            SocialWorld = socialWorld;
            JusticeRecordsOwner = justiceRecordsOwner;
            TheftOutcomeSink = socialWorld.Integration;
        }

        public bool ExecuteWithinOperation(Func<bool> operation)
        {
            order.Add("p12-begin");
            try { return operation != null && operation(); }
            finally { order.Add("p12-end"); }
        }


        public EconomyTransactionResult TryTransferMoney(
            DailyV1NpcRuntimeOwners source,
            DailyV1NpcRuntimeOwners destination,
            int amount)
        {
            TransferCalls++;
            order.Add(TransferCalls == 1 ? "transfer" : "rollback");
            if (TransferCalls > 1 && FailRollback)
                return EconomyTransactionResult.CreateFailure(
                    EconomyTransactionType.MoneyTransfer, MoneyEffect.Transfer,
                    EconomyTransactionFailureReason.TransactionCommitFailed,
                    source?.RuntimeId, destination?.RuntimeId);
            EconomyTransactionResult result = DailyV1MoneyTransferOperation.TryExecute(
                source?.MoneyAccount, destination?.MoneyAccount, amount,
                source?.RuntimeId, destination?.RuntimeId);
            if (TransferCalls == 1 && InjectDuplicateAfterFirstTransfer && result.Success)
            {
                TheftOutcome duplicate = CreateOutcome(destination, source, amount);
                Assert.That(SocialWorld.TheftOutcomes.TryRecord(
                    duplicate, out CrimeOutcomeStoreFailure failure), Is.True, failure.ToString());
            }
            return result;
        }

        public bool TryProjectWantedRecord(DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city, DailyV1JusticeWantedRecordOwner record)
        {
            Assert.That(target.RuntimeId, Is.EqualTo("npc/perpetrator"));
            Assert.That(record?.TargetRuntimeId, Is.EqualTo(target.RuntimeId));
            ProjectedWantedRecord = record;
            return record != null;
        }

        public bool TryPrepareWantedRecordProjection(DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city, DailyV1JusticeWantedRecordOwner record)
        {
            Assert.That(target.RuntimeId, Is.EqualTo("npc/perpetrator"));
            Assert.That(record?.TargetRuntimeId, Is.EqualTo(target.RuntimeId));
            return record != null;
        }

        public void DiscardPreparedWantedRecordProjection(DailyV1JusticeWantedRecordOwner record) { }

        public bool ApplyStatusChange(DailyV1NpcRuntimeOwners target,
            DailyV1StatusDefinition status, bool add)
        {
            order.Add("wanted-status");
            Assert.That(target.RuntimeId, Is.EqualTo("npc/perpetrator"));
            return status != null && (add ? target.Status.TryAdd(status) : target.Status.TryRemove(status));
        }

        public void PublishTheft(DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim, int amount) => order.Add("theft-log");
        public void LogCompensationFailure() => order.Add("compensation-warning");
        public void PublishWantedRecord(DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city, DailyV1JusticeWantedRecordOwner record)
        {
            Assert.That(target.RuntimeId, Is.EqualTo("npc/perpetrator"));
            Assert.That(record?.TargetRuntimeId, Is.EqualTo(target.RuntimeId));
            order.Add("warrant-log");
        }

        private TheftOutcome CreateOutcome(
            DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim,
            int amount)
        {
            Assert.That(SocialWorld.PersonStore.TryGetByMaterializedNpcRuntimeId(
                perpetrator.RuntimeId, out PersonRuntime perpetratorPerson), Is.True);
            Assert.That(SocialWorld.PersonStore.TryGetByMaterializedNpcRuntimeId(
                victim.RuntimeId, out PersonRuntime victimPerson), Is.True);
            long day = SocialWorld.SimulationTime.AbsoluteDay;
            string key = "theft/selected-slot";
            TheftOutcomeId id = TheftOutcomeId.Create(
                perpetratorPerson.PersonId, victimPerson.PersonId, day, key);
            return new TheftOutcome(id, perpetratorPerson.PersonId, victimPerson.PersonId,
                amount, day, key, "decision/steal");
        }
    }
}
