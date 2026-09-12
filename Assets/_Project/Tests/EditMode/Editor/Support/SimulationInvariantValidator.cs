using System;
using System.Collections.Generic;
using NUnit.Framework;

public static class SimulationInvariantValidator
{
    public static void ValidateActionExecution(
        ActionExecutionContext context,
        ActionParticipationRequirements requirements)
    {
        Assert.That(ActionExecutionValidator.TryValidate(context, requirements, out string reason), Is.True, reason);
    }

    public static void ValidateTravelParty(
        TravelPartyRuntime party,
        RuntimeIdentityRegistry registry)
    {
        Assert.That(party, Is.Not.Null);
        Assert.That(party.TravelPartyId, Is.Not.Null.And.Not.Empty);
        Assert.That(party.OriginLocationRuntimeId, Is.Not.Null.And.Not.Empty);
        Assert.That(party.DestinationLocationRuntimeId, Is.Not.Null.And.Not.Empty);
        Assert.That(party.RouteRuntimeId, Is.Not.Null.And.Not.Empty);
        Assert.That(party.TravelDaysTotal, Is.GreaterThan(0));
        Assert.That(party.TravelerRuntimeIds, Is.Not.Null);
        Assert.That(party.TravelerRuntimeIds.Count, Is.GreaterThan(0));
        Assert.That(party.EscortRuntimeIds, Is.Not.Null);
        Assert.That(party.MemberRuntimeIds, Is.Not.Null);
        Assert.That(party.MemberCosts, Is.Not.Null);
        Assert.That(party.MemberCosts.Count, Is.EqualTo(party.MemberRuntimeIds.Count));

        Assert.That(registry, Is.Not.Null);
        Assert.That(registry.TryGetLocation(party.OriginLocationRuntimeId, out SpatialLocationRuntime origin), Is.True);
        Assert.That(registry.TryGetLocation(party.DestinationLocationRuntimeId, out SpatialLocationRuntime destination), Is.True);
        Assert.That(registry.TryGetRoute(party.RouteRuntimeId, out SpatialRouteRuntime route), Is.True);
        Assert.That(route.Origin, Is.SameAs(origin));
        Assert.That(route.Destination, Is.SameAs(destination));

        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        CityRuntime commonDestination = null;
        int? commonRemainingDays = null;
        foreach (string runtimeId in party.MemberRuntimeIds)
        {
            Assert.That(runtimeId, Is.Not.Null.And.Not.Empty);
            Assert.That(memberIds.Add(runtimeId), Is.True);
            Assert.That(registry.TryGetNpc(runtimeId, out NpcRuntime npc), Is.True);
            Assert.That(npc, Is.Not.Null);
            Assert.That(npc.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
            Assert.That(npc.IsTraveling, Is.True);
            Assert.That(npc.DestinationCity, Is.Not.Null);
            Assert.That(npc.DestinationCity.Location, Is.Not.Null);
            Assert.That(npc.DestinationCity.Location.RuntimeId, Is.EqualTo(party.DestinationLocationRuntimeId));
            Assert.That(npc.TravelDaysRemaining, Is.GreaterThan(0));
            Assert.That(npc.TravelOriginDecisionId, Is.EqualTo(party.OriginDecisionId));

            if (commonDestination == null)
            {
                commonDestination = npc.DestinationCity;
                commonRemainingDays = npc.TravelDaysRemaining;
            }
            else
            {
                Assert.That(npc.DestinationCity, Is.SameAs(commonDestination));
                Assert.That(npc.TravelDaysRemaining, Is.EqualTo(commonRemainingDays.Value));
            }
        }

        HashSet<string> costIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (TravelPartyMemberCost cost in party.MemberCosts)
        {
            Assert.That(cost, Is.Not.Null);
            Assert.That(memberIds.Contains(cost.RuntimeId), Is.True);
            Assert.That(costIds.Add(cost.RuntimeId), Is.True);
            Assert.That(cost.Amount, Is.GreaterThanOrEqualTo(0f));
        }

        if (string.IsNullOrWhiteSpace(party.OriginDecisionId) == false)
        {
            Assert.That(party.OriginDecisionId, Is.Not.Empty);
        }
    }

    public static void ValidateTravelParties(
        TravelPartyStore store,
        RuntimeIdentityRegistry registry,
        IEnumerable<NpcRuntime> npcs)
    {
        Assert.That(store, Is.Not.Null);
        Assert.That(store.ActiveParties, Is.Not.Null);
        HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (TravelPartyRuntime party in store.ActiveParties)
        {
            ValidateTravelParty(party, registry);
            Assert.That(partyIds.Add(party.TravelPartyId), Is.True);

            foreach (string runtimeId in party.MemberRuntimeIds)
            {
                Assert.That(store.TryGetPartyForNpc(runtimeId, out TravelPartyRuntime indexed), Is.True);
                Assert.That(indexed, Is.SameAs(party));
            }
        }

        if (npcs == null)
        {
            return;
        }

        foreach (NpcRuntime npc in npcs)
        {
            Assert.That(npc, Is.Not.Null);

            if (string.IsNullOrWhiteSpace(npc.ActiveTravelPartyId) == true)
            {
                continue;
            }

            TravelPartyRuntime party = store.GetById(npc.ActiveTravelPartyId);
            Assert.That(party, Is.Not.Null);
            Assert.That(ContainsRuntimeId(party.MemberRuntimeIds, npc.RuntimeId), Is.True);
            Assert.That(store.TryGetPartyForNpc(npc.RuntimeId, out TravelPartyRuntime indexed), Is.True);
            Assert.That(indexed, Is.SameAs(party));
        }
    }

    private static bool ContainsRuntimeId(IReadOnlyList<string> runtimeIds, string expected)
    {
        if (runtimeIds == null)
        {
            return false;
        }

        foreach (string runtimeId in runtimeIds)
        {
            if (string.Equals(runtimeId, expected, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    public static void ValidateCommercialObservation(CommercialMarketObservation observation)
    {
        Assert.That(observation, Is.Not.Null);
        Assert.That(observation.LocationRuntimeId, Is.Not.Null.And.Not.Empty);
        Assert.That(observation.ItemDefinitionId, Is.Not.Null.And.Not.Empty);
        Assert.That(observation.ObservedDay, Is.GreaterThanOrEqualTo(0L));
        Assert.That(observation.ReceivedDay, Is.GreaterThanOrEqualTo(observation.ObservedDay));

        if (observation.Source == CommercialKnowledgeSource.SharedByNpc)
        {
            Assert.That(observation.SourceRuntimeId, Is.Not.Null.And.Not.Empty);
        }
    }

    public static void ValidateSpatialKnowledge(
        SpatialKnowledgeRuntime knowledge,
        RuntimeIdentityRegistry registry)
    {
        Assert.That(knowledge, Is.Not.Null);
        Assert.That(knowledge.OwnerRuntimeId, Is.Not.Null.And.Not.Empty);

        foreach (string locationRuntimeId in knowledge.KnownLocationRuntimeIds)
        {
            Assert.That(registry.TryGetLocation(locationRuntimeId, out _), Is.True);
        }

        foreach (string routeRuntimeId in knowledge.KnownRouteRuntimeIds)
        {
            Assert.That(registry.TryGetRoute(routeRuntimeId, out _), Is.True);
        }
    }

    public static void ValidateDecision(
        NpcDecisionRecord decision,
        RuntimeIdentityRegistry registry = null)
    {
        Assert.That(decision, Is.Not.Null);
        Assert.That(decision.DecisionId, Is.Not.Null.And.Not.Empty);
        Assert.That(decision.RecordSequence, Is.GreaterThan(0L));
        Assert.That(decision.ActorRuntimeId, Is.Not.Null.And.Not.Empty);

        if (registry != null)
        {
            Assert.That(registry.TryGetNpc(decision.ActorRuntimeId, out _), Is.True);
        }

        HashSet<string> participantRoleKeys = new HashSet<string>(StringComparer.Ordinal);
        bool actorIsDecisionMaker = false;
        foreach (NpcDecisionParticipant participant in decision.DecisionParticipants)
        {
            Assert.That(participant, Is.Not.Null);
            Assert.That(participant.RuntimeId, Is.Not.Null.And.Not.Empty);
            string participantRoleKey = participant.RuntimeId + "\u001f" + (int)participant.Role;
            Assert.That(participantRoleKeys.Add(participantRoleKey), Is.True);

            if (registry != null)
            {
                Assert.That(registry.TryGetNpc(participant.RuntimeId, out _), Is.True);
            }

            if (participant.RuntimeId == decision.ActorRuntimeId
                && participant.Role == NpcDecisionParticipantRole.DecisionMaker)
            {
                actorIsDecisionMaker = true;
            }
        }

        Assert.That(actorIsDecisionMaker, Is.True);
        Assert.That(decision.TargetRuntimeIds, Is.Not.Null);
        Assert.That(new HashSet<string>(decision.TargetRuntimeIds, StringComparer.Ordinal).Count, Is.EqualTo(decision.TargetRuntimeIds.Count));

        if (registry != null)
        {
            foreach (string targetRuntimeId in decision.TargetRuntimeIds)
            {
                Assert.That(registry.TryGetNpc(targetRuntimeId, out _), Is.True);
            }
        }
    }

    public static void ValidateDecisions(
        IReadOnlyList<NpcDecisionRecord> decisions,
        RuntimeIdentityRegistry registry = null)
    {
        Assert.That(decisions, Is.Not.Null);
        HashSet<string> decisionIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcDecisionRecord decision in decisions)
        {
            ValidateDecision(decision, registry);
            Assert.That(decisionIds.Add(decision.DecisionId), Is.True);
        }
    }

    public static void ValidateDomainEvent(
        DomainEvent domainEvent,
        RuntimeIdentityRegistry registry = null,
        NpcDecisionStore decisionStore = null)
    {
        Assert.That(domainEvent, Is.Not.Null);
        Assert.That(domainEvent.EventId, Is.Not.Null.And.Not.Empty);
        Assert.That(domainEvent.RecordSequence, Is.GreaterThan(0L));

        HashSet<string> participantRoleKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (DomainEventParticipant participant in domainEvent.GetParticipants())
        {
            Assert.That(participant, Is.Not.Null);
            Assert.That(participant.RuntimeId, Is.Not.Null.And.Not.Empty);
            string participantRoleKey = participant.RuntimeId + "\u001f" + (int)participant.Role;
            Assert.That(participantRoleKeys.Add(participantRoleKey), Is.True);

            if (registry != null)
            {
                Assert.That(registry.TryGetNpc(participant.RuntimeId, out _), Is.True);
            }
        }

        if (domainEvent.OriginDecisionId != null)
        {
            Assert.That(domainEvent.OriginDecisionId, Is.Not.Empty);

            if (decisionStore != null)
            {
                Assert.That(decisionStore.TryGetDecision(domainEvent.OriginDecisionId, out _), Is.True);
            }
        }
    }

    public static void ValidateDomainEvents(
        IReadOnlyList<DomainEvent> events,
        RuntimeIdentityRegistry registry = null,
        NpcDecisionStore decisionStore = null)
    {
        Assert.That(events, Is.Not.Null);
        HashSet<string> eventIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (DomainEvent domainEvent in events)
        {
            ValidateDomainEvent(domainEvent, registry, decisionStore);
            Assert.That(eventIds.Add(domainEvent.EventId), Is.True);
        }
    }

    public static void ValidateChronicle(IReadOnlyList<NpcChronicleEntry> entries)
    {
        Assert.That(entries, Is.Not.Null);

        long previousSequence = 0L;
        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcChronicleEntry entry in entries)
        {
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.RecordSequence, Is.GreaterThan(previousSequence));
            previousSequence = entry.RecordSequence;

            string identity = entry.EntryType + ":" + entry.RecordSequence;
            Assert.That(identities.Add(identity), Is.True);
        }
    }
}
