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

    public WorldStateBattleSnapshot(
        string battleId,
        long createdAbsoluteDay,
        long? startedAbsoluteDay,
        BattleLifecycleState lifecycleState,
        string conflictId,
        string warId,
        SpatialReference locationReference = null,
        string locationReferenceKey = null)
    {
        BattleId = battleId;
        CreatedAbsoluteDay = createdAbsoluteDay;
        StartedAbsoluteDay = startedAbsoluteDay;
        LifecycleState = lifecycleState;
        ConflictId = conflictId;
        WarId = warId;
        LocationReference = locationReference;
        LocationReferenceKey = locationReference?.StableKey ?? locationReferenceKey;
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
