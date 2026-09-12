using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class DecisionEventChronicleTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DecisionRecord_RequiresActorDecisionIdAndPositiveSequence()
    {
        Assert.Throws<ArgumentException>(() => new NpcDecisionRecord(
            string.Empty, 0L, 1L, "npc-actor", NpcDecisionType.Action, NpcDecisionOrigin.Autonomous, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new NpcDecisionRecord(
            "decision-1", 0L, 1L, string.Empty, NpcDecisionType.Action, NpcDecisionOrigin.Autonomous, null, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NpcDecisionRecord(
            "decision-1", 0L, 0L, "npc-actor", NpcDecisionType.Action, NpcDecisionOrigin.Autonomous, null, null, null, null));
    }

    [Test]
    public void DecisionRecord_SnapshotsParticipantsAndDeduplicatesRolesAndTargets()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionParticipant support = new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support);
        NpcDecisionParticipant contributor = new NpcDecisionParticipant("npc-marta", NpcDecisionParticipantRole.Contributor);

        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-test",
            new[]
            {
                support,
                support,
                contributor,
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support)
            },
            new[] { "npc-jobson", "npc-joao", "npc-jobson" },
            null,
            null);

        Assert.That(decision.DecisionParticipants, Has.Count.EqualTo(3));
        Assert.That(decision.TargetRuntimeIds, Has.Count.EqualTo(2));
        Assert.That(decision.TargetRuntimeIds, Is.EqualTo(new[] { "npc-jobson", "npc-joao" }));
        Assert.That(decision.DecisionParticipants[0].Role, Is.EqualTo(NpcDecisionParticipantRole.DecisionMaker));
    }

    [Test]
    public void DecisionValidator_AllowsSameNpcInDifferentRolesButDeduplicatesExactPair()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-test",
            new[]
            {
                new NpcDecisionParticipant("npc-bruno", NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant("npc-bruno", NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant("npc-bruno", NpcDecisionParticipantRole.Contributor),
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support)
            },
            new[] { "npc-jobson" },
            null,
            null);

        SimulationInvariantValidator.ValidateDecision(decision);

        Assert.That(decision.DecisionParticipants, Has.Count.EqualTo(4));
        Assert.That(decision.DecisionParticipants[0].Role, Is.EqualTo(NpcDecisionParticipantRole.DecisionMaker));
        Assert.That(decision.DecisionParticipants[1].Role, Is.EqualTo(NpcDecisionParticipantRole.Support));
        Assert.That(decision.DecisionParticipants[2].Role, Is.EqualTo(NpcDecisionParticipantRole.Contributor));
    }

    [Test]
    public void DecisionStore_IndexesActorParticipantsAndIntendedTargetsSeparately()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-test",
            new[] { new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support) },
            new[] { "npc-jobson" },
            null,
            null);

        Assert.That(fixture.Decisions.GetDecisionsForActor("npc-bruno"), Has.Member(decision));
        Assert.That(fixture.Decisions.GetDecisionsForParticipant("npc-caio"), Has.Member(decision));
        Assert.That(fixture.Decisions.GetDecisionsTargeting("npc-jobson"), Has.Member(decision));
        Assert.That(fixture.Decisions.GetDecisionsForParticipant("npc-jobson"), Is.Empty);
    }

    [Test]
    public void DecisionParticipants_PreserveSupportAndContributorRoles()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-test",
            new[]
            {
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant("npc-marta", NpcDecisionParticipantRole.Contributor)
            },
            null,
            null,
            null);

        Assert.That(decision.DecisionParticipants[1].Role, Is.EqualTo(NpcDecisionParticipantRole.Support));
        Assert.That(decision.DecisionParticipants[2].Role, Is.EqualTo(NpcDecisionParticipantRole.Contributor));
    }

    [Test]
    public void DecisionEvidence_RemainsSnapshotAfterKnowledgeReplacement()
    {
        ItemData item = SimulationTestFactory.CreateItem("item-wine");
        CommercialKnowledgeRuntime knowledge = new CommercialKnowledgeRuntime();
        CommercialMarketObservation oldObservation = SimulationTestFactory.CreateObservation(
            "location-a", item, 50f, 10, 10, 20, CommercialKnowledgeSource.SharedByNpc, "npc-bruno");
        knowledge.RecordObservation(oldObservation);
        CommercialObservationEvidence evidence = CommercialObservationEvidence.Capture(oldObservation, 0.4f);
        knowledge.RecordObservation(SimulationTestFactory.CreateObservation(
            "location-a", item, 25f, 10, 11, 11));

        Assert.That(evidence.KnownPrice, Is.EqualTo(50f));
        Assert.That(evidence.ObservedDay, Is.EqualTo(10L));
        Assert.That(evidence.ReceivedDay, Is.EqualTo(20L));
        Assert.That(evidence.Source, Is.EqualTo(CommercialKnowledgeSource.SharedByNpc));
        Assert.That(evidence.SourceRuntimeId, Is.EqualTo("npc-bruno"));
        Assert.That(evidence.Freshness, Is.EqualTo(0.4f));
    }

    [Test]
    public void DomainEventRecorder_RecordsCurrentEventTypesWithCausalIdsAndRoles()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture(7L);
        fixture.EventRecorder.Record((id, day, sequence) => new NpcTravelStartedEvent(
            id, day, sequence, "npc-bruno", "location-a", "location-b", "route-a-b", "decision-1"));
        fixture.EventRecorder.Record((id, day, sequence) => new NpcArrivedEvent(
            id, day, sequence, "npc-bruno", "location-b", "decision-1"));
        fixture.EventRecorder.Record((id, day, sequence) => new NpcArrestedEvent(
            id, day, sequence, "npc-guard", "npc-bruno", "location-b", "decision-2"));
        fixture.EventRecorder.Record((id, day, sequence) => new NpcEscapedEvent(
            id, day, sequence, "npc-bruno", "location-b", "decision-3"));

        Assert.That(fixture.Events.Events, Has.Count.EqualTo(4));
        Assert.That(fixture.Events.Events[0].EventType, Is.EqualTo(DomainEventType.NpcTravelStarted));
        Assert.That(fixture.Events.Events[1].EventType, Is.EqualTo(DomainEventType.NpcArrived));
        Assert.That(fixture.Events.Events[2].EventType, Is.EqualTo(DomainEventType.NpcArrested));
        Assert.That(fixture.Events.Events[3].EventType, Is.EqualTo(DomainEventType.NpcEscaped));
        Assert.That(fixture.Events.Events[0].RecordSequence, Is.EqualTo(1L));
        Assert.That(fixture.Events.Events[3].RecordSequence, Is.EqualTo(4L));
        Assert.That(fixture.Events.Events[2].OriginDecisionId, Is.EqualTo("decision-2"));
        Assert.That(fixture.Events.Events[2].GetParticipants()[0].Role, Is.EqualTo(DomainEventParticipantRole.Actor));
        Assert.That(fixture.Events.Events[2].GetParticipants()[1].Role, Is.EqualTo(DomainEventParticipantRole.Target));
    }

    [Test]
    public void DomainEventStore_IndexesOneMultiParticipantEventForEveryParticipant()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        MultiParticipantTestEvent domainEvent = new MultiParticipantTestEvent(
            fixture.Allocator.AllocateEventId(),
            fixture.Time.AbsoluteDay,
            fixture.Sequence.Allocate(),
            new[]
            {
                new DomainEventParticipant("npc-a", DomainEventParticipantRole.Actor),
                new DomainEventParticipant("npc-a", DomainEventParticipantRole.Participant),
                new DomainEventParticipant("npc-b", DomainEventParticipantRole.Support),
                new DomainEventParticipant("npc-c", DomainEventParticipantRole.Participant),
                new DomainEventParticipant("npc-x", DomainEventParticipantRole.Target),
                new DomainEventParticipant("npc-y", DomainEventParticipantRole.Target)
            });

        SimulationInvariantValidator.ValidateDomainEvent(domainEvent);
        Assert.That(fixture.Events.Record(domainEvent), Is.True);
        Assert.That(fixture.Events.Events, Has.Count.EqualTo(1));
        foreach (string participantId in new[] { "npc-a", "npc-b", "npc-c", "npc-x", "npc-y" })
        {
            Assert.That(fixture.Events.GetEventsForParticipant(participantId), Has.Count.EqualTo(1));
            Assert.That(fixture.Events.GetEventsForParticipant(participantId)[0].EventId, Is.EqualTo(domainEvent.EventId));
        }

        Assert.That(domainEvent.GetParticipants(), Has.Count.EqualTo(6));
        Assert.That(domainEvent.GetParticipants()[0].Role, Is.EqualTo(DomainEventParticipantRole.Actor));
        Assert.That(domainEvent.GetParticipants()[1].Role, Is.EqualTo(DomainEventParticipantRole.Participant));
        Assert.That(fixture.Events.GetEventsForParticipant("npc-a"), Has.Count.EqualTo(1));
        Assert.That(fixture.Chronicle.GetChronicle("npc-a"), Has.Count.EqualTo(1));
        Assert.That(fixture.Chronicle.GetChronicle("npc-a")[0].DomainEvent, Is.SameAs(domainEvent));
        Assert.That(fixture.Chronicle.GetChronicle("npc-a")[0].Relation, Is.EqualTo(NpcChronicleRelation.SelfAction));
    }

    [Test]
    public void DomainEventValidator_RejectsDuplicateSameRolePair()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        MultiParticipantTestEvent domainEvent = new MultiParticipantTestEvent(
            fixture.Allocator.AllocateEventId(),
            fixture.Time.AbsoluteDay,
            fixture.Sequence.Allocate(),
            new[]
            {
                new DomainEventParticipant("npc-a", DomainEventParticipantRole.Actor),
                new DomainEventParticipant("npc-a", DomainEventParticipantRole.Actor)
            });

        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateDomainEvent(domainEvent));
    }

    [Test]
    public void Chronicle_IsStrictlyOrderedByRecordSequence()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime actor = new NpcRuntime("npc-bruno", SimulationTestFactory.CreateNpc("bruno"));
        NpcDecisionRecord firstDecision = fixture.DecisionRecorder.RecordChosenAction(
            actor,
            new NpcActionRuntime(SimulationTestFactory.CreateAction("action-one", NpcActionType.Normal)),
            NpcDecisionOrigin.Autonomous);
        fixture.EventRecorder.Record((id, day, sequence) => new NpcArrivedEvent(
            id, day, sequence, actor.RuntimeId, "location-b", firstDecision.DecisionId));
        fixture.DecisionRecorder.RecordChosenAction(
            actor,
            new NpcActionRuntime(SimulationTestFactory.CreateAction("action-two", NpcActionType.Normal)),
            NpcDecisionOrigin.Autonomous);
        fixture.EventRecorder.Record((id, day, sequence) => new NpcEscapedEvent(
            id, day, sequence, actor.RuntimeId, "location-b", null));

        IReadOnlyList<NpcChronicleEntry> chronicle = fixture.Chronicle.GetChronicle(actor.RuntimeId);

        SimulationInvariantValidator.ValidateChronicle(chronicle);
        Assert.That(chronicle, Has.Count.EqualTo(4));
        Assert.That(chronicle[0].EntryType, Is.EqualTo(NpcChronicleEntryType.Decision));
        Assert.That(chronicle[1].EntryType, Is.EqualTo(NpcChronicleEntryType.DomainEvent));
        Assert.That(chronicle[2].EntryType, Is.EqualTo(NpcChronicleEntryType.Decision));
        Assert.That(chronicle[3].EntryType, Is.EqualTo(NpcChronicleEntryType.DomainEvent));
    }

    [Test]
    public void Chronicle_IntendedDecisionTargetDoesNotReceiveDecisionEntry()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionRecord decision = fixture.DecisionRecorder.Record(
            "npc-jobson", NpcDecisionType.Action, NpcDecisionOrigin.Autonomous, "action-steal", "npc-bruno", null, null);

        Assert.That(fixture.Chronicle.GetChronicle("npc-bruno"), Is.Empty);
        Assert.That(fixture.Chronicle.GetChronicle("npc-jobson"), Has.Count.EqualTo(1));
        Assert.That(fixture.Chronicle.GetChronicle("npc-jobson")[0].Decision, Is.SameAs(decision));
    }

    [Test]
    public void Chronicle_EventTargetReceivesAffectedByOtherEntry()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        fixture.EventRecorder.Record((id, day, sequence) => new NpcArrestedEvent(
            id, day, sequence, "npc-guard", "npc-bruno", "location-a", null));

        IReadOnlyList<NpcChronicleEntry> chronicle = fixture.Chronicle.GetChronicle("npc-bruno");

        Assert.That(chronicle, Has.Count.EqualTo(1));
        Assert.That(chronicle[0].Relation, Is.EqualTo(NpcChronicleRelation.AffectedByOther));
    }

    [Test]
    public void Chronicle_SupportAndContributorUseExpectedRelations()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-group",
            new[]
            {
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant("npc-marta", NpcDecisionParticipantRole.Contributor)
            },
            null,
            null,
            null);

        Assert.That(fixture.Chronicle.GetChronicle("npc-bruno")[0].Relation, Is.EqualTo(NpcChronicleRelation.SelfDecision));
        Assert.That(fixture.Chronicle.GetChronicle("npc-caio")[0].Relation, Is.EqualTo(NpcChronicleRelation.Support));
        Assert.That(fixture.Chronicle.GetChronicle("npc-marta")[0].Relation, Is.EqualTo(NpcChronicleRelation.Participant));
    }

    [Test]
    public void Chronicle_DecisionMakerAndSupportForSameNpcUsesSelfDecisionOnce()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-group",
            new[]
            {
                new NpcDecisionParticipant("npc-bruno", NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Contributor)
            },
            null,
            null,
            null);

        IReadOnlyList<NpcChronicleEntry> chronicle = fixture.Chronicle.GetChronicle("npc-bruno");

        Assert.That(decision.DecisionParticipants, Has.Count.EqualTo(3));
        Assert.That(chronicle, Has.Count.EqualTo(1));
        Assert.That(chronicle[0].Relation, Is.EqualTo(NpcChronicleRelation.SelfDecision));
        Assert.That(chronicle[0].Decision.DecisionParticipants[1].Role, Is.EqualTo(NpcDecisionParticipantRole.Support));
    }

    [Test]
    public void Chronicle_SupportAndContributorForSameNpcUsesSupportOnceAndPreservesRoles()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordWithParticipants(
            "npc-bruno",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-group",
            new[]
            {
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant("npc-caio", NpcDecisionParticipantRole.Contributor)
            },
            null,
            null,
            null);

        IReadOnlyList<NpcChronicleEntry> chronicle = fixture.Chronicle.GetChronicle("npc-caio");

        Assert.That(decision.DecisionParticipants, Has.Count.EqualTo(3));
        Assert.That(chronicle, Has.Count.EqualTo(1));
        Assert.That(chronicle[0].Relation, Is.EqualTo(NpcChronicleRelation.Support));
        Assert.That(decision.DecisionParticipants[1].Role, Is.EqualTo(NpcDecisionParticipantRole.Support));
        Assert.That(decision.DecisionParticipants[2].Role, Is.EqualTo(NpcDecisionParticipantRole.Contributor));
    }

    private sealed class MultiParticipantTestEvent : DomainEvent
    {
        private readonly IReadOnlyList<DomainEventParticipant> participants;

        public override DomainEventType EventType => DomainEventType.NpcTravelStarted;

        public MultiParticipantTestEvent(
            string eventId,
            long absoluteDay,
            long recordSequence,
            IReadOnlyList<DomainEventParticipant> participants)
            : base(eventId, absoluteDay, recordSequence, null)
        {
            this.participants = participants;
        }

        public override IReadOnlyList<DomainEventParticipant> GetParticipants()
        {
            return participants;
        }
    }
}
