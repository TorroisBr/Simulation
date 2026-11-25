using System;
using System.Collections.Generic;

public sealed class ConflictResolvedEvent : DomainEvent
{
    private readonly string conflictId;
    private readonly string locationRuntimeId;
    private readonly ConflictResolutionResult resolution;

    public override DomainEventType EventType => DomainEventType.ConflictResolved;
    public string ConflictId => conflictId;
    public string LocationRuntimeId => locationRuntimeId;
    public ConflictResolutionResult Resolution => resolution;
    public string WinningSideId => resolution.WinningSideId;
    public ConflictOutcomeType Outcome => resolution.Outcome;
    public ConflictOutcomeSource OutcomeSource => resolution.OutcomeSource;
    public bool ConsequencesWereExternallyConstrained => resolution.ConsequencesWereExternallyConstrained;
    public IReadOnlyList<ConflictSideResolutionResult> SideResults => resolution.SideResults;
    public IReadOnlyList<ConflictNpcConsequence> NpcConsequences => resolution.NpcConsequences;
    public IReadOnlyList<ConflictAggregateConsequence> AggregateConsequences => resolution.AggregateConsequences;

    public ConflictResolvedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        Conflict conflict,
        ConflictResolutionResult resolution)
        : base(
            eventId,
            absoluteDay,
            recordSequence,
            conflict != null ? conflict.OriginDecisionId : null)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        if (resolution == null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        ConflictResultValidator.Validate(conflict, resolution);
        conflictId = conflict.ConflictId;
        locationRuntimeId = conflict.LocationRuntimeId;
        this.resolution = resolution;
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        List<DomainEventParticipant> participants = new List<DomainEventParticipant>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (ConflictSideResolutionResult sideResult in resolution.SideResults)
        {
            foreach (ConflictParticipantCapabilityResult participant in sideResult.ParticipantContributions)
            {
                if (participant.Kind != ConflictParticipantKind.Npc
                    || runtimeIds.Add(participant.SourceId) == false)
                {
                    continue;
                }

                participants.Add(new DomainEventParticipant(
                    participant.SourceId,
                    DomainEventParticipantRole.Participant));
            }
        }

        return participants.AsReadOnly();
    }
}

[Serializable]
public sealed class BattleResolvedEvent : DomainEvent
{
    public override DomainEventType EventType => DomainEventType.BattleResolved;
    public BattleId BattleId { get; }
    public BattleOutcomeType OutcomeType { get; }
    public BattleSideId WinningBattleSideId { get; }
    public string D5PolicyFingerprint { get; }
    public string D5CausalResolutionFingerprint { get; }
    public string D6B2PolicyFingerprint { get; }
    public string D6B2PlanFingerprint { get; }

    public BattleResolvedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        BattleId battleId,
        BattleOutcomeType outcomeType,
        BattleSideId winningBattleSideId,
        string d5PolicyFingerprint,
        string d5CausalResolutionFingerprint,
        string d6b2PolicyFingerprint,
        string d6b2PlanFingerprint)
        : base(eventId, absoluteDay, recordSequence, null)
    {
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
        if (!Enum.IsDefined(typeof(BattleOutcomeType), outcomeType))
            throw new ArgumentOutOfRangeException(nameof(outcomeType));
        if (outcomeType == BattleOutcomeType.Victory && winningBattleSideId == null)
            throw new ArgumentException("A BattleResolved victory requires a winning side.", nameof(winningBattleSideId));
        if (outcomeType == BattleOutcomeType.Draw && winningBattleSideId != null)
            throw new ArgumentException("A BattleResolved draw cannot have a winning side.", nameof(winningBattleSideId));
        OutcomeType = outcomeType;
        WinningBattleSideId = winningBattleSideId;
        D5PolicyFingerprint = RequireId(d5PolicyFingerprint, nameof(d5PolicyFingerprint));
        D5CausalResolutionFingerprint = RequireId(d5CausalResolutionFingerprint, nameof(d5CausalResolutionFingerprint));
        D6B2PolicyFingerprint = RequireId(d6b2PolicyFingerprint, nameof(d6b2PolicyFingerprint));
        D6B2PlanFingerprint = RequireId(d6b2PlanFingerprint, nameof(d6b2PlanFingerprint));
    }

    // ArmedForceId and BattleSideId are not NpcRuntimeIds; do not misrepresent
    // military participants through the generic runtime-participant index.
    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
        => Array.Empty<DomainEventParticipant>();
}
