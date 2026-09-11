using System;
using System.Collections.Generic;

public sealed class SimulationRecordSequence
{
    private long nextSequence = 1L;

    public long Allocate()
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException("Simulation record sequence is exhausted.");
        }

        long allocated = nextSequence;
        nextSequence++;
        return allocated;
    }
}

public enum NpcDecisionType
{
    Action,
    TradePurchase,
    TradeSale,
    TradeReposition,
    TradeRedirect,
    Travel,
    Arrest,
    Escape
}

public enum NpcDecisionOrigin
{
    Autonomous,
    ScheduledDirective
}

public enum NpcDecisionParticipantRole
{
    DecisionMaker,
    Contributor,
    Support,
    Participant
}

[Serializable]
public sealed class NpcDecisionParticipant
{
    private readonly string runtimeId;
    private readonly NpcDecisionParticipantRole role;

    public string RuntimeId => runtimeId;
    public NpcDecisionParticipantRole Role => role;

    public NpcDecisionParticipant(string runtimeId, NpcDecisionParticipantRole role)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Decision participant requires a RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
        this.role = role;
    }
}

[Serializable]
public sealed class CommercialObservationEvidence
{
    private readonly string locationRuntimeId;
    private readonly float knownPrice;
    private readonly int knownStock;
    private readonly long observedDay;
    private readonly float freshness;

    public string LocationRuntimeId => locationRuntimeId;
    public float KnownPrice => knownPrice;
    public int KnownStock => knownStock;
    public long ObservedDay => observedDay;
    public float Freshness => freshness;

    public CommercialObservationEvidence(
        string locationRuntimeId,
        float knownPrice,
        int knownStock,
        long observedDay,
        float freshness)
    {
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
        this.knownPrice = Math.Max(0f, knownPrice);
        this.knownStock = Math.Max(0, knownStock);
        this.observedDay = Math.Max(0L, observedDay);
        this.freshness = Math.Max(0f, Math.Min(1f, freshness));
    }

    public static CommercialObservationEvidence Capture(CommercialMarketObservation observation, float freshness)
    {
        if (observation == null)
        {
            return null;
        }

        return new CommercialObservationEvidence(
            observation.LocationRuntimeId,
            observation.ObservedPrice,
            observation.ObservedStock,
            observation.ObservedDay,
            freshness);
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Commercial observation evidence requires stable IDs.", parameterName);
        }

        return value;
    }
}

[Serializable]
public sealed class CommercialDecisionEvidence
{
    private readonly string itemDefinitionId;
    private readonly string currentLocationRuntimeId;
    private readonly string tradeOriginLocationRuntimeId;
    private readonly string tradeDestinationLocationRuntimeId;
    private readonly string previousDestinationLocationRuntimeId;
    private readonly CommercialObservationEvidence originObservation;
    private readonly CommercialObservationEvidence destinationObservation;
    private readonly int expectedQuantity;
    private readonly float expectedPurchaseUnitPrice;
    private readonly float expectedSaleUnitPrice;
    private readonly float expectedTravelCost;
    private readonly float expectedGrossProfit;
    private readonly float expectedNetProfit;
    private readonly float expectedScore;

    public string ItemDefinitionId => itemDefinitionId;
    public string CurrentLocationRuntimeId => currentLocationRuntimeId;
    public string TradeOriginLocationRuntimeId => tradeOriginLocationRuntimeId;
    public string TradeDestinationLocationRuntimeId => tradeDestinationLocationRuntimeId;
    public string PreviousDestinationLocationRuntimeId => previousDestinationLocationRuntimeId;
    public CommercialObservationEvidence OriginObservation => originObservation;
    public CommercialObservationEvidence DestinationObservation => destinationObservation;
    public int ExpectedQuantity => expectedQuantity;
    public float ExpectedPurchaseUnitPrice => expectedPurchaseUnitPrice;
    public float ExpectedSaleUnitPrice => expectedSaleUnitPrice;
    public float ExpectedTravelCost => expectedTravelCost;
    public float ExpectedGrossProfit => expectedGrossProfit;
    public float ExpectedNetProfit => expectedNetProfit;
    public float ExpectedScore => expectedScore;

    public CommercialDecisionEvidence(
        string itemDefinitionId,
        string currentLocationRuntimeId,
        string tradeOriginLocationRuntimeId,
        string tradeDestinationLocationRuntimeId,
        string previousDestinationLocationRuntimeId,
        CommercialObservationEvidence originObservation,
        CommercialObservationEvidence destinationObservation,
        int expectedQuantity,
        float expectedPurchaseUnitPrice,
        float expectedSaleUnitPrice,
        float expectedTravelCost,
        float expectedGrossProfit,
        float expectedNetProfit,
        float expectedScore)
    {
        this.itemDefinitionId = RequireId(itemDefinitionId, nameof(itemDefinitionId));
        this.currentLocationRuntimeId = RequireId(currentLocationRuntimeId, nameof(currentLocationRuntimeId));
        this.tradeOriginLocationRuntimeId = RequireId(tradeOriginLocationRuntimeId, nameof(tradeOriginLocationRuntimeId));
        this.tradeDestinationLocationRuntimeId = RequireId(tradeDestinationLocationRuntimeId, nameof(tradeDestinationLocationRuntimeId));
        this.previousDestinationLocationRuntimeId = NormalizeOptionalId(previousDestinationLocationRuntimeId);
        this.originObservation = originObservation;
        this.destinationObservation = destinationObservation;
        this.expectedQuantity = Math.Max(0, expectedQuantity);
        this.expectedPurchaseUnitPrice = Math.Max(0f, expectedPurchaseUnitPrice);
        this.expectedSaleUnitPrice = Math.Max(0f, expectedSaleUnitPrice);
        this.expectedTravelCost = Math.Max(0f, expectedTravelCost);
        this.expectedGrossProfit = expectedGrossProfit;
        this.expectedNetProfit = expectedNetProfit;
        this.expectedScore = expectedScore;
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Commercial decision evidence requires stable IDs.", parameterName);
        }

        return value;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

[Serializable]
public sealed class NpcDecisionRecord
{
    private readonly string decisionId;
    private readonly long absoluteDay;
    private readonly long recordSequence;
    private readonly string actorRuntimeId;
    private readonly NpcDecisionType decisionType;
    private readonly NpcDecisionOrigin origin;
    private readonly string actionDefinitionId;
    private readonly string targetRuntimeId;
    private readonly string targetLocationRuntimeId;
    private readonly CommercialDecisionEvidence commercialEvidence;
    private readonly IReadOnlyList<NpcDecisionParticipant> decisionParticipants;
    private readonly IReadOnlyList<string> targetRuntimeIds;

    public string DecisionId => decisionId;
    public long AbsoluteDay => absoluteDay;
    public long RecordSequence => recordSequence;
    // Compatibility/convenience projection for the primary decision maker.
    // Other decision participants are represented by DecisionParticipants.
    public string ActorRuntimeId => actorRuntimeId;
    public NpcDecisionType DecisionType => decisionType;
    public NpcDecisionOrigin Origin => origin;
    public string ActionDefinitionId => actionDefinitionId;
    // Compatibility/convenience projection for the first intended target.
    // Intended targets are not decision participants and the complete snapshot is in TargetRuntimeIds.
    public string TargetRuntimeId => targetRuntimeId;
    public string TargetLocationRuntimeId => targetLocationRuntimeId;
    public CommercialDecisionEvidence CommercialEvidence => commercialEvidence;
    public IReadOnlyList<NpcDecisionParticipant> DecisionParticipants => decisionParticipants;
    public IReadOnlyList<string> TargetRuntimeIds => targetRuntimeIds;

    public NpcDecisionRecord(
        string decisionId,
        long absoluteDay,
        long recordSequence,
        string actorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        string targetRuntimeId,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence)
        : this(
            decisionId,
            absoluteDay,
            recordSequence,
            actorRuntimeId,
            decisionType,
            origin,
            actionDefinitionId,
            null,
            string.IsNullOrWhiteSpace(targetRuntimeId) == true ? null : new[] { targetRuntimeId },
            targetLocationRuntimeId,
            commercialEvidence)
    {
    }

    public NpcDecisionRecord(
        string decisionId,
        long absoluteDay,
        long recordSequence,
        string actorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        IEnumerable<NpcDecisionParticipant> decisionParticipants,
        IEnumerable<string> targetRuntimeIds,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence)
    {
        this.decisionId = RequireId(decisionId, nameof(decisionId));
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        }

        if (recordSequence <= 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(recordSequence));
        }

        this.absoluteDay = absoluteDay;
        this.recordSequence = recordSequence;
        this.decisionType = decisionType;
        this.origin = origin;
        this.actionDefinitionId = NormalizeOptionalId(actionDefinitionId);
        this.decisionParticipants = CaptureDecisionParticipants(actorRuntimeId, decisionParticipants);
        this.targetRuntimeIds = CaptureTargetRuntimeIds(targetRuntimeIds);
        this.targetRuntimeId = this.targetRuntimeIds.Count > 0 ? this.targetRuntimeIds[0] : null;
        this.targetLocationRuntimeId = NormalizeOptionalId(targetLocationRuntimeId);
        this.commercialEvidence = commercialEvidence;
    }

    private static IReadOnlyList<NpcDecisionParticipant> CaptureDecisionParticipants(
        string primaryActorRuntimeId,
        IEnumerable<NpcDecisionParticipant> participants)
    {
        List<NpcDecisionParticipant> snapshot = new List<NpcDecisionParticipant>();
        HashSet<string> participantRoles = new HashSet<string>(StringComparer.Ordinal);
        AddDecisionParticipant(
            snapshot,
            participantRoles,
            new NpcDecisionParticipant(primaryActorRuntimeId, NpcDecisionParticipantRole.DecisionMaker));

        if (participants != null)
        {
            foreach (NpcDecisionParticipant participant in participants)
            {
                AddDecisionParticipant(snapshot, participantRoles, participant);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static void AddDecisionParticipant(
        List<NpcDecisionParticipant> snapshot,
        HashSet<string> participantRoles,
        NpcDecisionParticipant participant)
    {
        if (participant == null || string.IsNullOrWhiteSpace(participant.RuntimeId) == true)
        {
            return;
        }

        string participantRoleKey = participant.RuntimeId + "\u001f" + (int)participant.Role;

        if (participantRoles.Add(participantRoleKey) == true)
        {
            snapshot.Add(new NpcDecisionParticipant(participant.RuntimeId, participant.Role));
        }
    }

    private static IReadOnlyList<string> CaptureTargetRuntimeIds(IEnumerable<string> targetIds)
    {
        List<string> snapshot = new List<string>();
        HashSet<string> uniqueTargetIds = new HashSet<string>(StringComparer.Ordinal);

        if (targetIds != null)
        {
            foreach (string targetId in targetIds)
            {
                string normalizedTargetId = NormalizeOptionalId(targetId);

                if (normalizedTargetId != null && uniqueTargetIds.Add(normalizedTargetId) == true)
                {
                    snapshot.Add(normalizedTargetId);
                }
            }
        }

        return snapshot.AsReadOnly();
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("NpcDecisionRecord requires stable IDs.", parameterName);
        }

        return value;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

public sealed class NpcDecisionStore
{
    private readonly List<NpcDecisionRecord> decisions = new List<NpcDecisionRecord>();
    private readonly IReadOnlyList<NpcDecisionRecord> readOnlyDecisions;
    private readonly Dictionary<string, NpcDecisionRecord> decisionsById = new Dictionary<string, NpcDecisionRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<NpcDecisionRecord>> decisionsByActor = new Dictionary<string, List<NpcDecisionRecord>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<NpcDecisionRecord>> decisionsByParticipant = new Dictionary<string, List<NpcDecisionRecord>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<NpcDecisionRecord>> decisionsByTarget = new Dictionary<string, List<NpcDecisionRecord>>(StringComparer.Ordinal);
    private readonly SimulationLogger logger;

    public IReadOnlyList<NpcDecisionRecord> Decisions => readOnlyDecisions;

    public NpcDecisionStore(SimulationLogger logger = null)
    {
        this.logger = logger ?? new SimulationLogger(null);
        readOnlyDecisions = decisions.AsReadOnly();
    }

    public bool Record(NpcDecisionRecord decision)
    {
        if (decision == null || string.IsNullOrWhiteSpace(decision.DecisionId) == true)
        {
            logger.LogError("Cannot record NPC decision: record or DecisionId is null.");
            return false;
        }

        if (decisionsById.ContainsKey(decision.DecisionId) == true)
        {
            logger.LogError($"Cannot record duplicate DecisionId '{decision.DecisionId}'.");
            return false;
        }

        decisionsById.Add(decision.DecisionId, decision);
        decisions.Add(decision);

        if (decisionsByActor.TryGetValue(decision.ActorRuntimeId, out List<NpcDecisionRecord> actorDecisions) == false)
        {
            actorDecisions = new List<NpcDecisionRecord>();
            decisionsByActor.Add(decision.ActorRuntimeId, actorDecisions);
        }

        actorDecisions.Add(decision);
        IndexDecisionParticipants(decision);
        IndexDecisionTargets(decision);
        return true;
    }

    public bool TryGetDecision(string decisionId, out NpcDecisionRecord decision)
    {
        return decisionsById.TryGetValue(decisionId ?? string.Empty, out decision);
    }

    public IReadOnlyList<NpcDecisionRecord> GetDecisionsForActor(string actorRuntimeId)
    {
        return decisionsByActor.TryGetValue(actorRuntimeId ?? string.Empty, out List<NpcDecisionRecord> actorDecisions) == true
            ? actorDecisions.AsReadOnly()
            : Array.Empty<NpcDecisionRecord>();
    }

    public IReadOnlyList<NpcDecisionRecord> GetDecisionsForParticipant(string participantRuntimeId)
    {
        return decisionsByParticipant.TryGetValue(participantRuntimeId ?? string.Empty, out List<NpcDecisionRecord> participantDecisions) == true
            ? participantDecisions.AsReadOnly()
            : Array.Empty<NpcDecisionRecord>();
    }

    public IReadOnlyList<NpcDecisionRecord> GetDecisionsTargeting(string targetRuntimeId)
    {
        return decisionsByTarget.TryGetValue(targetRuntimeId ?? string.Empty, out List<NpcDecisionRecord> targetingDecisions) == true
            ? targetingDecisions.AsReadOnly()
            : Array.Empty<NpcDecisionRecord>();
    }

    private void IndexDecisionParticipants(NpcDecisionRecord decision)
    {
        HashSet<string> indexedRuntimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcDecisionParticipant participant in decision.DecisionParticipants)
        {
            if (participant == null
                || string.IsNullOrWhiteSpace(participant.RuntimeId) == true
                || indexedRuntimeIds.Add(participant.RuntimeId) == false)
            {
                continue;
            }

            AddToIndex(decisionsByParticipant, participant.RuntimeId, decision);
        }
    }

    private void IndexDecisionTargets(NpcDecisionRecord decision)
    {
        foreach (string targetRuntimeId in decision.TargetRuntimeIds)
        {
            AddToIndex(decisionsByTarget, targetRuntimeId, decision);
        }
    }

    private static void AddToIndex(
        Dictionary<string, List<NpcDecisionRecord>> index,
        string runtimeId,
        NpcDecisionRecord decision)
    {
        if (index.TryGetValue(runtimeId, out List<NpcDecisionRecord> indexedDecisions) == false)
        {
            indexedDecisions = new List<NpcDecisionRecord>();
            index.Add(runtimeId, indexedDecisions);
        }

        indexedDecisions.Add(decision);
    }
}

public sealed class NpcDecisionRecorder
{
    private readonly RuntimeIdAllocator idAllocator;
    private readonly SimulationTime simulationTime;
    private readonly SimulationRecordSequence recordSequence;
    private readonly NpcDecisionStore decisionStore;
    private readonly SimulationLogger logger;

    public NpcDecisionRecorder(
        RuntimeIdAllocator idAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        SimulationLogger logger = null)
    {
        this.idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.recordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
        this.decisionStore = decisionStore ?? throw new ArgumentNullException(nameof(decisionStore));
        this.logger = logger ?? new SimulationLogger(null);
    }

    public NpcDecisionRecord RecordChosenAction(NpcRuntime actor, NpcActionRuntime actionRuntime, NpcDecisionOrigin origin)
    {
        if (actor == null || actionRuntime == null || actionRuntime.Action == null)
        {
            return null;
        }

        if (actionRuntime.Action.actionType == NpcActionType.Travel
            && string.IsNullOrWhiteSpace(actionRuntime.OriginDecisionId) == false)
        {
            return null;
        }

        string actionDefinitionId = actionRuntime.Action.DefinitionId;

        if (string.IsNullOrWhiteSpace(actionDefinitionId) == true)
        {
            logger.LogError("Cannot record chosen NPC action: ActionDefinitionId is empty.");
            return null;
        }

        NpcDecisionRecord decision = Record(
            actor.RuntimeId,
            GetDecisionType(actionRuntime),
            origin,
            actionDefinitionId,
            actionRuntime.TargetNpc?.RuntimeId,
            actionRuntime.TargetCity?.Location?.RuntimeId,
            actionRuntime.CommercialDecisionEvidence);

        if (decision != null)
        {
            actionRuntime.SetOriginDecisionId(decision.DecisionId);
        }

        return decision;
    }

    public NpcDecisionRecord Record(
        string actorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        string targetRuntimeId,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence)
    {
        return RecordWithParticipants(
            actorRuntimeId,
            decisionType,
            origin,
            actionDefinitionId,
            null,
            string.IsNullOrWhiteSpace(targetRuntimeId) == true ? null : new[] { targetRuntimeId },
            targetLocationRuntimeId,
            commercialEvidence);
    }

    public NpcDecisionRecord RecordWithParticipants(
        string primaryActorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        IEnumerable<NpcDecisionParticipant> decisionParticipants,
        IEnumerable<string> targetRuntimeIds,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence)
    {
        try
        {
            NpcDecisionRecord decision = new NpcDecisionRecord(
                idAllocator.AllocateDecisionId(),
                simulationTime.AbsoluteDay,
                recordSequence.Allocate(),
                primaryActorRuntimeId,
                decisionType,
                origin,
                actionDefinitionId,
                decisionParticipants,
                targetRuntimeIds,
                targetLocationRuntimeId,
                commercialEvidence);

            return decisionStore.Record(decision) == true ? decision : null;
        }
        catch (ArgumentException exception)
        {
            logger.LogError("Cannot create NPC decision record: " + exception.Message);
            return null;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError("Cannot allocate NPC decision record identity: " + exception.Message);
            return null;
        }
    }

    private static NpcDecisionType GetDecisionType(NpcActionRuntime actionRuntime)
    {
        switch (actionRuntime.Action.actionType)
        {
            case NpcActionType.BuyGoods:
                return NpcDecisionType.TradePurchase;
            case NpcActionType.SellGoods:
                return NpcDecisionType.TradeSale;
            case NpcActionType.Travel:
                return actionRuntime.TravelReason == NpcTravelReason.TradeReposition
                    ? NpcDecisionType.TradeReposition
                    : NpcDecisionType.Travel;
            case NpcActionType.FleeCity:
                return NpcDecisionType.Travel;
            case NpcActionType.Arrest:
                return NpcDecisionType.Arrest;
            case NpcActionType.EscapePrison:
                return NpcDecisionType.Escape;
            default:
                return NpcDecisionType.Action;
        }
    }
}
