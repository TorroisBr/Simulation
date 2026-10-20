using System;
using System.Collections.Generic;

/// <summary>
/// Ephemeral owner-issued cardinality evidence. This witness is not owner state
/// export and its owner identity is valid only for the live runtime instance.
/// Reading the owner is not synchronized; P12-B may consume this witness only
/// on the bound simulation thread while the registered operation set is idle.
/// </summary>
public sealed class OwnerSectionCensusWitness
{
    internal OwnerSectionCensusWitness(
        string sectionId,
        int schemaVersion,
        object ownerInstanceIdentity,
        int cardinality,
        long revision)
    {
        if (string.IsNullOrWhiteSpace(sectionId)) throw new ArgumentException("Section identity is required.", nameof(sectionId));
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        OwnerInstanceIdentity = ownerInstanceIdentity ?? throw new ArgumentNullException(nameof(ownerInstanceIdentity));
        if (cardinality < 0) throw new ArgumentOutOfRangeException(nameof(cardinality));
        if (revision < 0L) throw new ArgumentOutOfRangeException(nameof(revision));

        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        Cardinality = cardinality;
        Revision = revision;
    }

    public string SectionId { get; }
    public int SchemaVersion { get; }
    /// <summary>Opaque reference-identity token; it is not serialized or persisted.</summary>
    public object OwnerInstanceIdentity { get; }
    public int Cardinality { get; }
    public long Revision { get; }
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
    Escape,
    CommercialScout,
    ExpeditionStart,
    ExpeditionExplore,
    ExpeditionTraverse,
    ExpeditionResolveOpposition,
    ExpeditionRetrieve,
    ExpeditionReturn
}

public enum NpcDecisionOrigin
{
    Autonomous,
    ScheduledDirective,
    ActorChoice
}

public enum NpcDecisionParticipantRole
{
    DecisionMaker,
    Performer,
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
    private readonly CommercialScoutingEvidence commercialScoutingEvidence;
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
    public CommercialScoutingEvidence CommercialScoutingEvidence => commercialScoutingEvidence;
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
            commercialEvidence,
            null)
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
        : this(
            decisionId,
            absoluteDay,
            recordSequence,
            actorRuntimeId,
            decisionType,
            origin,
            actionDefinitionId,
            decisionParticipants,
            targetRuntimeIds,
            targetLocationRuntimeId,
            commercialEvidence,
            null)
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
        CommercialDecisionEvidence commercialEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence)
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
        this.commercialScoutingEvidence = commercialScoutingEvidence;
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
        this.logger = logger;
        readOnlyDecisions = decisions.AsReadOnly();
    }

    public bool Record(NpcDecisionRecord decision)
    {
        if (decision == null || string.IsNullOrWhiteSpace(decision.DecisionId) == true)
        {
            logger?.LogError("Cannot record NPC decision: record or DecisionId is null.");
            return false;
        }

        if (decisionsById.ContainsKey(decision.DecisionId) == true)
        {
            logger?.LogError($"Cannot record duplicate DecisionId '{decision.DecisionId}'.");
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
