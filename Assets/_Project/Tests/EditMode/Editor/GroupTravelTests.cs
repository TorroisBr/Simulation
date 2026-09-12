using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class GroupTravelTests
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
    public void ActionExecutionRequirements_EnforceParticipantRoleBounds()
    {
        ActionParticipationRequirements requirements = new ActionParticipationRequirements(
            minPerformers: 3,
            maxPerformers: 3,
            minSupports: 1,
            maxSupports: 1,
            minTargets: 2,
            maxTargets: 2);
        ActionExecutionContext context = new ActionExecutionContext(
            "cooperative-action",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-b", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-c", ActionExecutionParticipantRole.Support),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target),
                new ActionExecutionParticipant("npc-y", ActionExecutionParticipantRole.Target)
            });

        Assert.That(ActionExecutionValidator.TryValidate(context, requirements, out _), Is.False);

        ActionExecutionContext complete = new ActionExecutionContext(
            "cooperative-action",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-b", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-c", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-s", ActionExecutionParticipantRole.Support),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target),
                new ActionExecutionParticipant("npc-y", ActionExecutionParticipantRole.Target)
            });

        SimulationInvariantValidator.ValidateActionExecution(complete, requirements);
    }

    [Test]
    public void RuntimeIdentityAllocator_TravelPartyIdsUseIndependentStableNamespace()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        string first = allocator.AllocateTravelPartyId();
        string second = allocator.AllocateTravelPartyId();
        string npc = allocator.AllocateNpcId();

        Assert.That(first, Is.EqualTo("travel-party-000001"));
        Assert.That(second, Is.EqualTo("travel-party-000002"));
        Assert.That(first, Is.Not.EqualTo(npc));
    }

    [Test]
    public void ActionExecutionRequirements_UnlimitedPerformersAllowMany()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "many-performers",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-b", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-c", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-d", ActionExecutionParticipantRole.Performer)
            });

        SimulationInvariantValidator.ValidateActionExecution(
            context,
            new ActionParticipationRequirements(minPerformers: 1, maxPerformers: ActionParticipationRequirements.Unlimited));
    }

    [Test]
    public void ActionExecutionValidator_DuplicateExactRolePairIsRejected()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "duplicate-role",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer)
            });

        Assert.That(ActionExecutionValidator.TryValidate(
            context,
            new ActionParticipationRequirements(minPerformers: 1),
            out _), Is.False);
    }

    [Test]
    public void ActionExecutionValidator_SameRuntimeIdMayHaveDifferentRoles()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "multi-role",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Support)
            });

        SimulationInvariantValidator.ValidateActionExecution(
            context,
            new ActionParticipationRequirements(minPerformers: 1, minSupports: 1));
    }

    [Test]
    public void ActionExecutionTargets_TargetParticipantsAreCanonical()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "canonical-targets",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target),
                new ActionExecutionParticipant("npc-y", ActionExecutionParticipantRole.Target)
            });

        SimulationInvariantValidator.ValidateActionExecution(
            context,
            new ActionParticipationRequirements(minPerformers: 1, minTargets: 2, maxTargets: 2));
        Assert.That(context.Participants.Count, Is.EqualTo(3));
        Assert.That(context.Participants[1].RuntimeId, Is.EqualTo("npc-x"));
        Assert.That(context.Participants[1].Role, Is.EqualTo(ActionExecutionParticipantRole.Target));
        Assert.That(context.Participants[2].RuntimeId, Is.EqualTo("npc-y"));
        Assert.That(context.Participants[2].Role, Is.EqualTo(ActionExecutionParticipantRole.Target));
    }

    [Test]
    public void ActionExecutionTargets_TargetDoesNotCountAsPerformer()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "target-only",
            new[] { new ActionExecutionParticipant("npc-target", ActionExecutionParticipantRole.Target) });

        Assert.That(ActionExecutionValidator.TryValidate(
            context,
            new ActionParticipationRequirements(minPerformers: 1),
            out _), Is.False);
    }

    [Test]
    public void ActionExecutionTargets_MinTargetsUsesCanonicalTargets()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "minimum-targets",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target)
            });

        Assert.That(ActionExecutionValidator.TryValidate(
            context,
            new ActionParticipationRequirements(minPerformers: 1, minTargets: 2),
            out _), Is.False);
    }

    [Test]
    public void ActionExecutionTargets_MaxTargetsUsesCanonicalTargets()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "maximum-targets",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target),
                new ActionExecutionParticipant("npc-y", ActionExecutionParticipantRole.Target),
                new ActionExecutionParticipant("npc-z", ActionExecutionParticipantRole.Target)
            });

        Assert.That(ActionExecutionValidator.TryValidate(
            context,
            new ActionParticipationRequirements(minPerformers: 1, maxTargets: 2),
            out _), Is.False);
    }

    [Test]
    public void ActionExecutionTargets_DuplicateSameTargetRoleIsRejected()
    {
        ActionExecutionContext context = new ActionExecutionContext(
            "duplicate-target",
            new[]
            {
                new ActionExecutionParticipant("npc-a", ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target),
                new ActionExecutionParticipant("npc-x", ActionExecutionParticipantRole.Target)
            });

        Assert.That(ActionExecutionValidator.TryValidate(
            context,
            new ActionParticipationRequirements(minPerformers: 1, minTargets: 1),
            out _), Is.False);
    }

    [Test]
    public void TravelParty_StartsOneAtomicExecutionWithStableMembershipAndCosts()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        float brunoMoney = fixture.Bruno.Money;
        float caioMoney = fixture.Caio.Money;
        float martaMoney = fixture.Marta.Money;

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out TravelPartyRuntime party), Is.True);
        Assert.That(party.TravelPartyId, Is.EqualTo("travel-party-000001"));
        Assert.That(party.TravelerRuntimeIds, Is.EqualTo(new[] { fixture.Bruno.RuntimeId, fixture.Caio.RuntimeId }));
        Assert.That(party.EscortRuntimeIds, Is.EqualTo(new[] { fixture.Marta.RuntimeId }));
        Assert.That(party.MemberRuntimeIds.Count, Is.EqualTo(3));
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(1));
        Assert.That(fixture.Bruno.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
        Assert.That(fixture.Caio.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
        Assert.That(fixture.Marta.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
        Assert.That(fixture.Bruno.SpatialKnowledge.KnowsLocation(fixture.World.A.Location.RuntimeId), Is.True);
        Assert.That(fixture.Bruno.SpatialKnowledge.KnowsRoute(fixture.World.RouteAB.RuntimeId), Is.True);
        Assert.That(fixture.Bruno.SpatialKnowledge.KnowsLocation(fixture.World.B.Location.RuntimeId), Is.False);
        Assert.That(fixture.Bruno.Money, Is.EqualTo(brunoMoney - 3f));
        Assert.That(fixture.Caio.Money, Is.EqualTo(caioMoney - 3f));
        Assert.That(fixture.Marta.Money, Is.EqualTo(martaMoney - 3f));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(1));
        Assert.That(fixture.Records.Events.Events[0].EventType, Is.EqualTo(DomainEventType.TravelPartyStarted));
        Assert.That(fixture.Records.Events.Events[0].GetParticipants().Count, Is.EqualTo(3));
        SimulationInvariantValidator.ValidateTravelParties(fixture.Parties, fixture.World.IdentityRegistry, fixture.Members);
    }

    [Test]
    public void TravelPartyInvariant_ActiveTravelPartyIdWithoutExistingPartyIsInvalid()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        fixture.Bruno.SetActiveTravelPartyId("travel-party-missing");

        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            fixture.Members));
    }

    [Test]
    public void TravelPartyInvariant_ExistingPartyWithoutNpcMembershipIsInvalid()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        string partyId = "travel-party-manual";
        Assert.That(fixture.Caio.StartTravel(fixture.World.B, 3), Is.True);
        fixture.Caio.SetActiveTravelPartyId(partyId);
        TravelPartyRuntime party = CreatePartySnapshot(
            fixture,
            partyId,
            new[] { fixture.Caio },
            fixture.World.B,
            fixture.World.RouteAB,
            3,
            null);
        Assert.That(fixture.Parties.Add(party), Is.True);
        fixture.Bruno.SetActiveTravelPartyId(partyId);

        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            fixture.Members));
    }

    [Test]
    public void TravelPartyInvariant_ProgressDesynchronizationIsInvalid()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties();
        Assert.That(fixture.Bruno.AdvanceTravelDay(out _), Is.False);

        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            fixture.Members));
    }

    [Test]
    public void TravelPartyInvariant_DestinationDesynchronizationIsInvalid()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        string partyId = "travel-party-manual";
        Assert.That(fixture.Bruno.StartTravel(fixture.World.B, 3), Is.True);
        Assert.That(fixture.Caio.StartTravel(fixture.World.C, 3), Is.True);
        fixture.Bruno.SetActiveTravelPartyId(partyId);
        fixture.Caio.SetActiveTravelPartyId(partyId);
        TravelPartyRuntime party = CreatePartySnapshot(
            fixture,
            partyId,
            new[] { fixture.Bruno, fixture.Caio },
            fixture.World.B,
            fixture.World.RouteAB,
            3,
            null);
        Assert.That(fixture.Parties.Add(party), Is.True);

        Assert.That(fixture.System.AdvanceParties().Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Caio.TravelDaysRemaining, Is.EqualTo(3));
        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            new[] { fixture.Bruno, fixture.Caio }));
    }

    [Test]
    public void TravelPartyInvariant_OriginDecisionDesynchronizationIsInvalid()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        string partyId = "travel-party-manual";
        Assert.That(fixture.Bruno.StartTravel(fixture.World.B, 3, "decision-a"), Is.True);
        Assert.That(fixture.Caio.StartTravel(fixture.World.B, 3, "decision-b"), Is.True);
        fixture.Bruno.SetActiveTravelPartyId(partyId);
        fixture.Caio.SetActiveTravelPartyId(partyId);
        TravelPartyRuntime party = CreatePartySnapshot(
            fixture,
            partyId,
            new[] { fixture.Bruno, fixture.Caio },
            fixture.World.B,
            fixture.World.RouteAB,
            3,
            "decision-a");
        Assert.That(fixture.Parties.Add(party), Is.True);

        Assert.That(fixture.System.AdvanceParties().Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Caio.TravelDaysRemaining, Is.EqualTo(3));
        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            new[] { fixture.Bruno, fixture.Caio }));
    }

    [Test]
    public void TravelParty_InsufficientMemberFundsRollsBackAllMembersAtomically()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        fixture.Marta.TrySpendMoney(fixture.Marta.Money);
        float brunoMoney = fixture.Bruno.Money;
        float caioMoney = fixture.Caio.Money;

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.False);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.IsTraveling, Is.False);
        Assert.That(fixture.Caio.IsTraveling, Is.False);
        Assert.That(fixture.Marta.IsTraveling, Is.False);
        Assert.That(fixture.Bruno.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Caio.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Marta.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Bruno.SpatialKnowledge.KnowsRoute(fixture.World.RouteAB.RuntimeId), Is.False);
        Assert.That(fixture.Caio.SpatialKnowledge.KnowsRoute(fixture.World.RouteAB.RuntimeId), Is.False);
        Assert.That(fixture.Bruno.Money, Is.EqualTo(brunoMoney));
        Assert.That(fixture.Caio.Money, Is.EqualTo(caioMoney));
    }

    [Test]
    public void TravelParty_DifferentOriginsAreRejectedWithoutCreatingParty()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        fixture.World.B.AddImportantNpc(fixture.Caio);

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.False);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.IsTraveling, Is.False);
        Assert.That(fixture.Caio.CurrentCity, Is.SameAs(fixture.World.B));
    }

    [Test]
    public void TravelParty_AlreadyTravelingMemberIsRejected()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.Bruno.StartTravel(fixture.World.B, 3), Is.True);

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.False);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.IsTraveling, Is.True);
    }

    [Test]
    public void TravelParty_ActiveMemberCannotJoinSecondParty()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.False);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(1));
        Assert.That(fixture.Parties.TryGetPartyForNpc(fixture.Bruno.RuntimeId, out TravelPartyRuntime indexed), Is.True);
        Assert.That(indexed, Is.Not.Null);
    }

    [Test]
    public void TravelParty_RouteMustMatchDestinationWorldTruth()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        ActionExecutionContext context = CreateContext(fixture, fixture.World.RouteAC.RuntimeId);

        Assert.That(fixture.System.TryStartTravelParty(context, out _), Is.False);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(0));
    }

    [Test]
    public void TravelParty_KnownPlanningRequiresEveryMemberToKnowRoute()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.CanPlanKnownGroupTravel(CreateContext(fixture), out _), Is.False);

        foreach (NpcRuntime member in fixture.Members)
        {
            member.SpatialKnowledge.DiscoverLocation(fixture.World.A.Location.RuntimeId);
            member.SpatialKnowledge.DiscoverLocation(fixture.World.B.Location.RuntimeId);
            member.SpatialKnowledge.DiscoverRoute(fixture.World.RouteAB.RuntimeId);
        }

        Assert.That(fixture.System.CanPlanKnownGroupTravel(CreateContext(fixture), out _), Is.True);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.IsTraveling, Is.False);
    }

    [Test]
    public void TravelParty_SoloTravelSystemSkipsActivePartyMembers()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);

        IReadOnlyList<NpcRuntime> soloArrivals = fixture.Travel.AdvanceTravels(new List<NpcRuntime>(fixture.Members));

        Assert.That(soloArrivals.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(3));
    }

    [Test]
    public void TravelParty_AdvancesSynchronouslyAndArrivesWithOneEvent()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out TravelPartyRuntime party), Is.True);

        Assert.That(fixture.System.AdvanceParties().Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.System.AdvanceParties().Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(fixture.System.AdvanceParties().Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(1));
        IReadOnlyList<NpcRuntime> arrivals = fixture.System.AdvanceParties();

        Assert.That(arrivals.Count, Is.EqualTo(3));
        foreach (NpcRuntime member in fixture.Members)
        {
            Assert.That(member.CurrentCity, Is.SameAs(fixture.World.B));
            Assert.That(member.IsTraveling, Is.False);
            Assert.That(member.ActiveTravelPartyId, Is.Null);
            Assert.That(member.TravelOriginDecisionId, Is.Null);
        }

        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(2));
        Assert.That(fixture.Records.Events.Events[1].EventType, Is.EqualTo(DomainEventType.TravelPartyArrived));
        Assert.That(fixture.Records.Events.Events[1].EventId, Is.Not.EqualTo(fixture.Records.Events.Events[0].EventId));
        Assert.That(party.IsCompleted, Is.True);
    }

    [Test]
    public void TravelParty_StartWithoutOriginDecision_DoesNotCreateDecisionRecord()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(1);

        Assert.That(fixture.Records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        Assert.That(fixture.Records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(1));
        Assert.That(fixture.Records.Events.Events[0].EventType, Is.EqualTo(DomainEventType.TravelPartyStarted));
        Assert.That(fixture.Records.Events.Events[0].OriginDecisionId, Is.Null);
        fixture.System.AdvanceParties();
        fixture.System.AdvanceParties();

        Assert.That(fixture.Records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(2));
        Assert.That(fixture.Records.Events.Events[1].EventType, Is.EqualTo(DomainEventType.TravelPartyArrived));
        Assert.That(fixture.Records.Events.Events[1].OriginDecisionId, Is.Null);
    }

    [Test]
    public void TravelParty_DesynchronizedProgressDoesNotAdvanceAnyMember()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties();
        Assert.That(fixture.Bruno.AdvanceTravelDay(out _), Is.False);

        IReadOnlyList<NpcRuntime> arrivals = fixture.System.AdvanceParties();

        Assert.That(arrivals.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(fixture.Caio.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Marta.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(1));
    }

    [Test]
    public void TravelParty_DesynchronizedActivePartyIdDoesNotAdvanceAnyMember()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties();
        fixture.Bruno.SetActiveTravelPartyId("travel-party-other");

        IReadOnlyList<NpcRuntime> arrivals = fixture.System.AdvanceParties();

        Assert.That(arrivals.Count, Is.EqualTo(0));
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Caio.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Marta.TravelDaysRemaining, Is.EqualTo(3));
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(1));
        Assert.Throws<AssertionException>(() => SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            fixture.Members));
    }

    [Test]
    public void TravelParty_RecordsOneEventPerParticipantAndChronicleRelation()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(1);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties();
        fixture.System.AdvanceParties();

        DomainEvent started = fixture.Records.Events.Events[0];
        DomainEvent arrived = fixture.Records.Events.Events[1];
        Assert.That(fixture.Records.Events.GetEventsForParticipant(fixture.Bruno.RuntimeId).Count, Is.EqualTo(2));
        Assert.That(fixture.Records.Events.GetEventsForParticipant(fixture.Caio.RuntimeId).Count, Is.EqualTo(2));
        Assert.That(fixture.Records.Events.GetEventsForParticipant(fixture.Marta.RuntimeId).Count, Is.EqualTo(2));
        Assert.That(fixture.Records.Chronicle.GetChronicle(fixture.Bruno.RuntimeId).Count, Is.EqualTo(2));
        Assert.That(fixture.Records.Chronicle.GetChronicle(fixture.Bruno.RuntimeId)[0].Relation, Is.EqualTo(NpcChronicleRelation.SelfAction));
        Assert.That(fixture.Records.Chronicle.GetChronicle(fixture.Marta.RuntimeId)[0].Relation, Is.EqualTo(NpcChronicleRelation.Support));
        Assert.That(started.EventId, Is.Not.EqualTo(arrived.EventId));
        SimulationInvariantValidator.ValidateDomainEvents(fixture.Records.Events.Events, fixture.World.IdentityRegistry, fixture.Records.Decisions);
    }

    [Test]
    public void TravelParty_DecisionTravelArrivalPreservesCausalSequence()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(2);
        NpcDecisionRecord decision = fixture.Records.DecisionRecorder.RecordWithParticipants(
            fixture.Bruno.RuntimeId,
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "group-travel",
            new[]
            {
                new NpcDecisionParticipant(fixture.Caio.RuntimeId, NpcDecisionParticipantRole.Support),
                new NpcDecisionParticipant(fixture.Marta.RuntimeId, NpcDecisionParticipantRole.Contributor)
            },
            null,
            fixture.World.B.Location.RuntimeId,
            null,
            null);
        ActionExecutionContext context = CreateContext(fixture, fixture.World.RouteAB.RuntimeId, decision.DecisionId);

        Assert.That(fixture.System.TryStartTravelParty(context, out _), Is.True);
        fixture.System.AdvanceParties();
        fixture.System.AdvanceParties();
        fixture.System.AdvanceParties();

        DomainEvent started = fixture.Records.Events.Events[0];
        DomainEvent arrived = fixture.Records.Events.Events[1];
        Assert.That(decision.RecordSequence, Is.LessThan(started.RecordSequence));
        Assert.That(started.RecordSequence, Is.LessThan(arrived.RecordSequence));
        Assert.That(started.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(arrived.OriginDecisionId, Is.EqualTo(decision.DecisionId));

        foreach (NpcRuntime member in fixture.Members)
        {
            Assert.That(member.TravelOriginDecisionId, Is.Null);
        }

        SimulationInvariantValidator.ValidateChronicle(fixture.Records.Chronicle.GetChronicle(fixture.Bruno.RuntimeId));
    }

    [Test]
    public void TravelParty_ArrivalDiscoversDestinationAndMerchantCanObserveTruth()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(1);
        ItemData item = SimulationTestFactory.CreateItem("item-wine", 10f);
        fixture.World.B.Market.AddStock(item, 10, 10);
        MerchantSystem merchant = SimulationTestFactory.CreateMerchantSystem(
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.DecisionRecorder);

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties();
        fixture.System.AdvanceParties();
        merchant.ObserveCurrentMarket(fixture.Bruno);

        Assert.That(fixture.Bruno.SpatialKnowledge.KnowsLocation(fixture.World.B.Location.RuntimeId), Is.True);
        Assert.That(fixture.Bruno.CommercialKnowledge.TryGetObservation(
            fixture.World.B.Location.RuntimeId,
            item.DefinitionId,
            out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedDay, Is.EqualTo(fixture.Records.Time.AbsoluteDay));
        Assert.That(observation.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
    }

    private static ActionExecutionContext CreateContext(
        TravelPartyFixture fixture,
        string routeRuntimeId = null,
        string originDecisionId = null)
    {
        return new ActionExecutionContext(
            "group-travel",
            new[]
            {
                new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
            },
            fixture.World.B.Location.RuntimeId,
            routeRuntimeId ?? fixture.World.RouteAB.RuntimeId,
            originDecisionId);
    }

    private static TravelPartyRuntime CreatePartySnapshot(
        TravelPartyFixture fixture,
        string partyId,
        IEnumerable<NpcRuntime> travelers,
        CityRuntime destination,
        SpatialRouteRuntime route,
        int travelDays,
        string originDecisionId)
    {
        List<string> travelerIds = new List<string>();
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>();

        foreach (NpcRuntime traveler in travelers)
        {
            travelerIds.Add(traveler.RuntimeId);
            costs.Add(new TravelPartyMemberCost(traveler.RuntimeId, 0f));
        }

        return new TravelPartyRuntime(
            partyId,
            fixture.World.A.Location.RuntimeId,
            destination.Location.RuntimeId,
            route.RuntimeId,
            travelerIds,
            null,
            travelDays,
            originDecisionId,
            costs);
    }
}
