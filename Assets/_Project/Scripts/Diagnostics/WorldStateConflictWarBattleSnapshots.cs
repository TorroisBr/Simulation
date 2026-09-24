using System;

public sealed class WorldStateConflictSnapshot
{
    public string ConflictId { get; }
    public long CreatedAbsoluteDay { get; }
    public ConflictLifecycleState LifecycleState { get; }
    public long? EndedAbsoluteDay { get; }

    public WorldStateConflictSnapshot(string conflictId, long createdAbsoluteDay, ConflictLifecycleState lifecycleState, long? endedAbsoluteDay)
    {
        ConflictId = conflictId;
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        EndedAbsoluteDay = endedAbsoluteDay;
    }
}

public sealed class WorldStateConflictSideSnapshot
{
    public string ConflictId { get; }
    public string SideId { get; }
    public string DisplayName { get; }

    public WorldStateConflictSideSnapshot(string conflictId, string sideId, string displayName)
    {
        ConflictId = conflictId;
        SideId = sideId;
        DisplayName = displayName ?? string.Empty;
    }
}

public sealed class WorldStateConflictParticipantBindingSnapshot
{
    public string ConflictId { get; }
    public string BindingId { get; }
    public string SideId { get; }
    public string ArmedForceId { get; }

    public WorldStateConflictParticipantBindingSnapshot(string conflictId, string bindingId, string sideId, string armedForceId)
    {
        ConflictId = conflictId;
        BindingId = bindingId;
        SideId = sideId;
        ArmedForceId = armedForceId;
    }
}

public sealed class WorldStateWarSnapshot
{
    public string WarId { get; }
    public long CreatedAbsoluteDay { get; }
    public WarLifecycleState LifecycleState { get; }
    public long? EndedAbsoluteDay { get; }
    public string ConflictId { get; }

    public WorldStateWarSnapshot(string warId, long createdAbsoluteDay, WarLifecycleState lifecycleState, long? endedAbsoluteDay, string conflictId)
    {
        WarId = warId;
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        EndedAbsoluteDay = endedAbsoluteDay;
        ConflictId = conflictId;
    }
}

public sealed class WorldStateWarSideSnapshot
{
    public string WarId { get; }
    public string SideId { get; }
    public string DisplayName { get; }

    public WorldStateWarSideSnapshot(string warId, string sideId, string displayName)
    {
        WarId = warId;
        SideId = sideId;
        DisplayName = displayName ?? string.Empty;
    }
}

public sealed class WorldStateWarParticipantBindingSnapshot
{
    public string WarId { get; }
    public string BindingId { get; }
    public string SideId { get; }
    public string ArmedForceId { get; }

    public WorldStateWarParticipantBindingSnapshot(string warId, string bindingId, string sideId, string armedForceId)
    {
        WarId = warId;
        BindingId = bindingId;
        SideId = sideId;
        ArmedForceId = armedForceId;
    }
}

public sealed class WorldStateBattleSnapshot
{
    public string BattleId { get; }
    public long CreatedAbsoluteDay { get; }
    public long? StartedAbsoluteDay { get; }
    public BattleLifecycleState LifecycleState { get; }
    public string ConflictId { get; }
    public string WarId { get; }
    public SpatialReference LocationReference { get; }
    public string LocationReferenceKey { get; }
    public WorldStateBattleTerminalOutcomeSnapshot TerminalOutcome { get; }

    public WorldStateBattleSnapshot(
        string battleId,
        long createdAbsoluteDay,
        long? startedAbsoluteDay,
        BattleLifecycleState lifecycleState,
        string conflictId,
        string warId,
        SpatialReference locationReference = null,
        string locationReferenceKey = null,
        WorldStateBattleTerminalOutcomeSnapshot terminalOutcome = null)
    {
        BattleId = battleId;
        CreatedAbsoluteDay = createdAbsoluteDay;
        StartedAbsoluteDay = startedAbsoluteDay;
        LifecycleState = lifecycleState;
        ConflictId = conflictId;
        WarId = warId;
        LocationReference = locationReference;
        LocationReferenceKey = locationReference?.StableKey ?? locationReferenceKey;
        TerminalOutcome = terminalOutcome;
    }
}

/// <summary>Immutable diagnostic view of accepted Battle outcome and stable provenance.</summary>
public sealed class WorldStateBattleTerminalOutcomeSnapshot
{
    public string BattleId { get; }
    public BattleOutcomeType OutcomeType { get; }
    public string WinningBattleSideId { get; }
    public long ResolvedAbsoluteDay { get; }
    public string D5PolicyFingerprint { get; }
    public string D5NumericExecutionProfileKey { get; }
    public string D5ProjectionVersion { get; }
    public string D5CausalResolutionFingerprint { get; }
    public string D5SourceContextFingerprint { get; }
    public string D5CapabilityRuleKey { get; }
    public string D5RandomAuthorityRuleKey { get; }
    public string D5ResolverSettingsIdentity { get; }
    public string D6B2PolicyFingerprint { get; }
    public string D6B2PlanSchemaVersion { get; }
    public string D6B2CoverageVersion { get; }
    public string D6B2PlanFingerprint { get; }

    public WorldStateBattleTerminalOutcomeSnapshot(PersistentBattleTerminalOutcome outcome)
    {
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        BattleId = outcome.BattleId?.Value;
        OutcomeType = outcome.OutcomeType;
        WinningBattleSideId = outcome.WinningBattleSideId?.Value;
        ResolvedAbsoluteDay = outcome.ResolvedAbsoluteDay;
        BattleResolutionProvenance d5 = outcome.Provenance?.D5Resolution;
        D5PolicyFingerprint = d5?.PolicyFingerprint;
        D5NumericExecutionProfileKey = d5?.NumericExecutionProfileKey;
        D5ProjectionVersion = d5?.ProjectionVersion;
        D5CausalResolutionFingerprint = d5?.CausalResolutionFingerprint;
        D5SourceContextFingerprint = d5?.SourceContextFingerprint;
        D5CapabilityRuleKey = d5?.CapabilityRuleKey;
        D5RandomAuthorityRuleKey = d5?.RandomAuthorityRuleKey;
        D5ResolverSettingsIdentity = d5?.ResolverSettingsIdentity;
        D6B2PolicyFingerprint = outcome.Provenance?.D6B2PolicyFingerprint;
        D6B2PlanSchemaVersion = outcome.Provenance?.D6B2PlanSchemaVersion;
        D6B2CoverageVersion = outcome.Provenance?.D6B2CoverageVersion;
        D6B2PlanFingerprint = outcome.Provenance?.D6B2PlanFingerprint;
    }
}

public sealed class WorldStateBattleSideSnapshot
{
    public string BattleId { get; }
    public string SideId { get; }
    public string DisplayName { get; }

    public WorldStateBattleSideSnapshot(string battleId, string sideId, string displayName)
    {
        BattleId = battleId;
        SideId = sideId;
        DisplayName = displayName ?? string.Empty;
    }
}

public sealed class WorldStateBattleParticipantBindingSnapshot
{
    public string BattleId { get; }
    public string BindingId { get; }
    public string SideId { get; }
    public string ArmedForceId { get; }

    public WorldStateBattleParticipantBindingSnapshot(string battleId, string bindingId, string sideId, string armedForceId)
    {
        BattleId = battleId;
        BindingId = bindingId;
        SideId = sideId;
        ArmedForceId = armedForceId;
    }
}
