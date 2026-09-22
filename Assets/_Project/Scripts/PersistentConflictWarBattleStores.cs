using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Authoritative persistent Conflict state. This store is deliberately
/// independent from the lower-level ConflictFoundation resolution primitive.
/// </summary>
public sealed class PersistentConflictStore
{
    private readonly ArmedForceStore armedForceStore;
    private readonly Dictionary<string, PersistentConflictRecord> recordsById =
        new Dictionary<string, PersistentConflictRecord>(StringComparer.Ordinal);
    private long revision;

    public PersistentConflictStore(ArmedForceStore armedForceStore)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public int Count => recordsById.Count;
    public long Revision => revision;

    public IReadOnlyList<PersistentConflictRecord> Records
    {
        get
        {
            List<PersistentConflictRecord> values = new List<PersistentConflictRecord>(recordsById.Values);
            values.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
            return new ReadOnlyCollection<PersistentConflictRecord>(values);
        }
    }

    public bool TryGet(ConflictId conflictId, out PersistentConflictRecord record)
    {
        record = null;
        return conflictId != null && recordsById.TryGetValue(conflictId.Value, out record);
    }

    public bool TryRegister(PersistentConflictRecord record, out PersistentStateFailure failure)
    {
        if (record == null || record.Id == null)
        {
            return Fail(PersistentStateFailureCode.InvalidRecord, "A Conflict requires a stable ConflictId.", out failure);
        }

        if (recordsById.ContainsKey(record.Id.Value))
        {
            return Fail(PersistentStateFailureCode.DuplicateIdentity, "The ConflictId is already registered.", out failure);
        }

        if (ValidateRecord(record, false, out failure) == false || CanAdvance(out failure) == false)
        {
            return false;
        }

        recordsById.Add(record.Id.Value, record);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryAddParticipantBinding(
        ConflictId conflictId,
        ConflictParticipantBinding binding,
        out PersistentStateFailure failure)
    {
        if (TryGet(conflictId, out PersistentConflictRecord current) == false)
        {
            return Fail(PersistentStateFailureCode.NotRegistered, "The ConflictId is not registered.", out failure);
        }

        if (current.IsActive == false)
        {
            return Fail(PersistentStateFailureCode.StateEnded, "An ended Conflict cannot receive a new participant.", out failure);
        }

        if (ValidateBinding(current, binding, true, out failure) == false || CanAdvance(out failure) == false)
        {
            return false;
        }

        recordsById[current.Id.Value] = current.WithParticipantBinding(binding);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryEnd(ConflictId conflictId, long endedAbsoluteDay, out PersistentStateFailure failure)
    {
        if (TryGet(conflictId, out PersistentConflictRecord current) == false)
        {
            return Fail(PersistentStateFailureCode.NotRegistered, "The ConflictId is not registered.", out failure);
        }

        if (current.IsActive == false)
        {
            return Fail(PersistentStateFailureCode.StateEnded, "The Conflict is already ended.", out failure);
        }

        if (endedAbsoluteDay < current.CreatedAbsoluteDay)
        {
            return Fail(PersistentStateFailureCode.InvalidDay, "A Conflict cannot end before it is created.", out failure);
        }

        if (CanAdvance(out failure) == false) return false;

        recordsById[current.Id.Value] = current.WithEnded(endedAbsoluteDay);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    internal PersistentConflictStore Clone(ArmedForceStore targetArmedForceStore)
    {
        if (targetArmedForceStore == null) throw new ArgumentNullException(nameof(targetArmedForceStore));
        PersistentConflictStore copy = new PersistentConflictStore(targetArmedForceStore);
        foreach (KeyValuePair<string, PersistentConflictRecord> entry in recordsById)
        {
            copy.recordsById.Add(entry.Key, entry.Value);
        }

        copy.revision = revision;
        PersistentStateInvariantReport report = copy.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException("The ConflictStore cannot be bound to the target ArmedForceStore.", nameof(targetArmedForceStore));
        }

        return copy;
    }

    public PersistentStateInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        foreach (PersistentConflictRecord record in Records)
        {
            if (ValidateRecordForDiagnostics(record, violations) == false) continue;
        }

        return new PersistentStateInvariantReport(violations);
    }

    private bool ValidateRecord(
        PersistentConflictRecord record,
        bool requireActiveForce,
        out PersistentStateFailure failure)
    {
        if (record.Sides == null || record.Sides.Count < 2)
        {
            return Fail(PersistentStateFailureCode.RequiresTwoSides, "A Conflict requires at least two domain-owned sides.", out failure);
        }

        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictStateSide side in record.Sides)
        {
            if (side == null || side.SideId == null)
            {
                return Fail(PersistentStateFailureCode.InvalidSide, "A Conflict side requires a stable side identity.", out failure);
            }

            if (side.ConflictId != record.Id)
            {
                return Fail(PersistentStateFailureCode.SideParentMismatch, "A Conflict side must reference its owning Conflict.", out failure);
            }

            if (sideIds.Add(side.SideId.Value) == false)
            {
                return Fail(PersistentStateFailureCode.DuplicateSide, "A Conflict cannot register the same side twice.", out failure);
            }
        }

        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictParticipantBinding binding in record.ParticipantBindings)
        {
            if (ValidateBindingIdentity(record, binding, sideIds, bindingIds, requireActiveForce, out failure) == false)
            {
                return false;
            }
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private bool ValidateBinding(
        PersistentConflictRecord record,
        ConflictParticipantBinding binding,
        bool requireActiveForce,
        out PersistentStateFailure failure)
    {
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictStateSide side in record.Sides)
        {
            if (side?.SideId != null) sideIds.Add(side.SideId.Value);
        }

        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictParticipantBinding current in record.ParticipantBindings)
        {
            if (current?.BindingId != null) bindingIds.Add(current.BindingId.Value);
        }

        return ValidateBindingIdentity(record, binding, sideIds, bindingIds, requireActiveForce, out failure);
    }

    private bool ValidateBindingIdentity(
        PersistentConflictRecord record,
        ConflictParticipantBinding binding,
        HashSet<string> sideIds,
        HashSet<string> bindingIds,
        bool requireActiveForce,
        out PersistentStateFailure failure)
    {
        if (binding == null || binding.BindingId == null || binding.ArmedForceId == null)
        {
            return Fail(PersistentStateFailureCode.InvalidBinding, "A Conflict participant requires binding and ArmedForce identities.", out failure);
        }

        if (binding.ConflictId != record.Id)
        {
            return Fail(PersistentStateFailureCode.BindingParentMismatch, "A Conflict participant must reference its owning Conflict.", out failure);
        }

        if (binding.SideId == null || sideIds.Contains(binding.SideId.Value) == false)
        {
            return Fail(PersistentStateFailureCode.SideNotRegistered, "A Conflict participant must reference a registered side.", out failure);
        }

        if (bindingIds.Add(binding.BindingId.Value) == false)
        {
            return Fail(PersistentStateFailureCode.DuplicateBinding, "A Conflict cannot register the same binding twice.", out failure);
        }

        if (armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force) == false)
        {
            return Fail(PersistentStateFailureCode.ForceNotRegistered, "The Conflict participant ArmedForceId is not registered.", out failure);
        }

        if (requireActiveForce && force.IsActive == false)
        {
            return Fail(PersistentStateFailureCode.ForceNotActive, "A new Conflict participant must reference an active ArmedForce.", out failure);
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private bool ValidateRecordForDiagnostics(PersistentConflictRecord record, List<string> violations)
    {
        if (record == null || record.Id == null)
        {
            violations.Add("Conflict record has no stable identity.");
            return false;
        }

        if (record.Sides == null || record.Sides.Count < 2)
        {
            violations.Add("Conflict " + record.Id.Value + " has fewer than two sides.");
        }

        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictStateSide side in record.Sides ?? new List<ConflictStateSide>())
        {
            if (side == null || side.SideId == null)
            {
                violations.Add("Conflict " + record.Id.Value + " has an invalid side.");
                continue;
            }

            if (side.ConflictId != record.Id)
            {
                violations.Add("Conflict " + record.Id.Value + " has a side with a mismatched parent.");
            }

            if (sideIds.Add(side.SideId.Value) == false)
            {
                violations.Add("Conflict " + record.Id.Value + " has a duplicate side.");
            }
        }

        if (record.LifecycleState == ConflictLifecycleState.Active && record.EndedAbsoluteDay.HasValue)
        {
            violations.Add("Active Conflict " + record.Id.Value + " has an end day.");
        }
        else if (record.LifecycleState == ConflictLifecycleState.Ended
            && (!record.EndedAbsoluteDay.HasValue || record.EndedAbsoluteDay.Value < record.CreatedAbsoluteDay))
        {
            violations.Add("Ended Conflict " + record.Id.Value + " has an invalid end day.");
        }

        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictParticipantBinding binding in record.ParticipantBindings ?? new List<ConflictParticipantBinding>())
        {
            if (binding == null || binding.BindingId == null || binding.ArmedForceId == null)
            {
                violations.Add("Conflict " + record.Id.Value + " has an invalid participant binding.");
                continue;
            }

            if (binding.ConflictId != record.Id) violations.Add("Conflict " + record.Id.Value + " has a participant with a mismatched parent.");
            if (binding.SideId == null || sideIds.Contains(binding.SideId.Value) == false) violations.Add("Conflict " + record.Id.Value + " has a participant with a missing side.");
            if (bindingIds.Add(binding.BindingId.Value) == false) violations.Add("Conflict " + record.Id.Value + " has a duplicate participant binding.");
            if (armedForceStore.TryGet(binding.ArmedForceId, out _) == false) violations.Add("Conflict " + record.Id.Value + " has a participant with a missing ArmedForce.");
        }

        return true;
    }

    private bool CanAdvance(out PersistentStateFailure failure)
    {
        if (revision == long.MaxValue) return Fail(PersistentStateFailureCode.RevisionOverflow, "The ConflictStore revision cannot advance further.", out failure);
        failure = PersistentStateFailure.None;
        return true;
    }

    private static bool Fail(PersistentStateFailureCode code, string message, out PersistentStateFailure failure)
    {
        failure = PersistentStateFailure.Create(code, message);
        return false;
    }
}

/// <summary>Authoritative persistent War state with optional Conflict reference.</summary>
public sealed class PersistentWarStore
{
    private readonly ArmedForceStore armedForceStore;
    private readonly PersistentConflictStore conflictStore;
    private readonly Dictionary<string, PersistentWarRecord> recordsById =
        new Dictionary<string, PersistentWarRecord>(StringComparer.Ordinal);
    private long revision;

    public PersistentWarStore(ArmedForceStore armedForceStore, PersistentConflictStore conflictStore)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.conflictStore = conflictStore ?? throw new ArgumentNullException(nameof(conflictStore));
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public PersistentConflictStore ConflictStore => conflictStore;
    public int Count => recordsById.Count;
    public long Revision => revision;

    public IReadOnlyList<PersistentWarRecord> Records
    {
        get
        {
            List<PersistentWarRecord> values = new List<PersistentWarRecord>(recordsById.Values);
            values.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
            return new ReadOnlyCollection<PersistentWarRecord>(values);
        }
    }

    public bool TryGet(WarId warId, out PersistentWarRecord record)
    {
        record = null;
        return warId != null && recordsById.TryGetValue(warId.Value, out record);
    }

    public bool TryRegister(PersistentWarRecord record, out PersistentStateFailure failure)
    {
        if (record == null || record.Id == null) return Fail(PersistentStateFailureCode.InvalidRecord, "A War requires a stable WarId.", out failure);
        if (recordsById.ContainsKey(record.Id.Value)) return Fail(PersistentStateFailureCode.DuplicateIdentity, "The WarId is already registered.", out failure);
        if (ValidateRecord(record, false, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById.Add(record.Id.Value, record);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryAddParticipantBinding(WarId warId, WarParticipantBinding binding, out PersistentStateFailure failure)
    {
        if (TryGet(warId, out PersistentWarRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The WarId is not registered.", out failure);
        if (current.IsActive == false) return Fail(PersistentStateFailureCode.StateEnded, "An ended War cannot receive a new participant.", out failure);
        if (ValidateBinding(current, binding, true, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = current.WithParticipantBinding(binding);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryEnd(WarId warId, long endedAbsoluteDay, out PersistentStateFailure failure)
    {
        if (TryGet(warId, out PersistentWarRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The WarId is not registered.", out failure);
        if (current.IsActive == false) return Fail(PersistentStateFailureCode.StateEnded, "The War is already ended.", out failure);
        if (endedAbsoluteDay < current.CreatedAbsoluteDay) return Fail(PersistentStateFailureCode.InvalidDay, "A War cannot end before it is created.", out failure);
        if (CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = current.WithEnded(endedAbsoluteDay);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    internal PersistentWarStore Clone(ArmedForceStore targetArmedForceStore, PersistentConflictStore targetConflictStore)
    {
        if (targetArmedForceStore == null) throw new ArgumentNullException(nameof(targetArmedForceStore));
        if (targetConflictStore == null) throw new ArgumentNullException(nameof(targetConflictStore));
        PersistentWarStore copy = new PersistentWarStore(targetArmedForceStore, targetConflictStore);
        foreach (KeyValuePair<string, PersistentWarRecord> entry in recordsById) copy.recordsById.Add(entry.Key, entry.Value);
        copy.revision = revision;
        if (copy.ValidateInvariants().IsValid == false) throw new ArgumentException("The WarStore cannot be bound to the target stores.", nameof(targetArmedForceStore));
        return copy;
    }

    public PersistentStateInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        foreach (PersistentWarRecord record in Records) ValidateRecordForDiagnostics(record, violations);
        return new PersistentStateInvariantReport(violations);
    }

    private bool ValidateRecord(PersistentWarRecord record, bool requireActiveForce, out PersistentStateFailure failure)
    {
        if (record.ConflictId != null && conflictStore.TryGet(record.ConflictId, out _) == false) return Fail(PersistentStateFailureCode.ConflictNotRegistered, "The War ConflictId is not registered.", out failure);
        if (record.Sides == null || record.Sides.Count < 2) return Fail(PersistentStateFailureCode.RequiresTwoSides, "A War requires at least two domain-owned sides.", out failure);
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarStateSide side in record.Sides)
        {
            if (side == null || side.SideId == null) return Fail(PersistentStateFailureCode.InvalidSide, "A War side requires a stable side identity.", out failure);
            if (side.WarId != record.Id) return Fail(PersistentStateFailureCode.SideParentMismatch, "A War side must reference its owning War.", out failure);
            if (sideIds.Add(side.SideId.Value) == false) return Fail(PersistentStateFailureCode.DuplicateSide, "A War cannot register the same side twice.", out failure);
        }

        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarParticipantBinding binding in record.ParticipantBindings)
        {
            if (binding == null || binding.BindingId == null || binding.ArmedForceId == null) return Fail(PersistentStateFailureCode.InvalidBinding, "A War participant requires binding and ArmedForce identities.", out failure);
            if (binding.WarId != record.Id) return Fail(PersistentStateFailureCode.BindingParentMismatch, "A War participant must reference its owning War.", out failure);
            if (binding.SideId == null || sideIds.Contains(binding.SideId.Value) == false) return Fail(PersistentStateFailureCode.SideNotRegistered, "A War participant must reference a registered side.", out failure);
            if (bindingIds.Add(binding.BindingId.Value) == false) return Fail(PersistentStateFailureCode.DuplicateBinding, "A War cannot register the same binding twice.", out failure);
            if (armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force) == false) return Fail(PersistentStateFailureCode.ForceNotRegistered, "The War participant ArmedForceId is not registered.", out failure);
            if (requireActiveForce && force.IsActive == false) return Fail(PersistentStateFailureCode.ForceNotActive, "A new War participant must reference an active ArmedForce.", out failure);
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private bool ValidateBinding(PersistentWarRecord record, WarParticipantBinding binding, bool requireActiveForce, out PersistentStateFailure failure)
    {
        PersistentWarRecord candidate = record.WithParticipantBinding(binding);
        if (ValidateRecord(candidate, false, out failure) == false) return false;
        if (requireActiveForce)
        {
            if (binding == null || armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force) == false)
            {
                return Fail(PersistentStateFailureCode.ForceNotRegistered, "The War participant ArmedForceId is not registered.", out failure);
            }

            if (force.IsActive == false)
            {
                return Fail(PersistentStateFailureCode.ForceNotActive, "A new War participant must reference an active ArmedForce.", out failure);
            }
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private void ValidateRecordForDiagnostics(PersistentWarRecord record, List<string> violations)
    {
        if (record == null || record.Id == null) { violations.Add("War record has no stable identity."); return; }
        if (record.ConflictId != null && conflictStore.TryGet(record.ConflictId, out _) == false) violations.Add("War " + record.Id.Value + " has a missing Conflict reference.");
        if (record.Sides == null || record.Sides.Count < 2) violations.Add("War " + record.Id.Value + " has fewer than two sides.");
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarStateSide side in record.Sides ?? new List<WarStateSide>())
        {
            if (side == null || side.SideId == null) { violations.Add("War " + record.Id.Value + " has an invalid side."); continue; }
            if (side.WarId != record.Id) violations.Add("War " + record.Id.Value + " has a side with a mismatched parent.");
            if (sideIds.Add(side.SideId.Value) == false) violations.Add("War " + record.Id.Value + " has a duplicate side.");
        }
        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarParticipantBinding binding in record.ParticipantBindings ?? new List<WarParticipantBinding>())
        {
            if (binding == null || binding.BindingId == null || binding.ArmedForceId == null) { violations.Add("War " + record.Id.Value + " has an invalid participant binding."); continue; }
            if (binding.WarId != record.Id) violations.Add("War " + record.Id.Value + " has a participant with a mismatched parent.");
            if (binding.SideId == null || sideIds.Contains(binding.SideId.Value) == false) violations.Add("War " + record.Id.Value + " has a participant with a missing side.");
            if (bindingIds.Add(binding.BindingId.Value) == false) violations.Add("War " + record.Id.Value + " has a duplicate participant binding.");
            if (armedForceStore.TryGet(binding.ArmedForceId, out _) == false) violations.Add("War " + record.Id.Value + " has a participant with a missing ArmedForce.");
        }
    }

    private bool CanAdvance(out PersistentStateFailure failure)
    {
        if (revision == long.MaxValue) return Fail(PersistentStateFailureCode.RevisionOverflow, "The WarStore revision cannot advance further.", out failure);
        failure = PersistentStateFailure.None;
        return true;
    }

    private static bool Fail(PersistentStateFailureCode code, string message, out PersistentStateFailure failure)
    {
        failure = PersistentStateFailure.Create(code, message);
        return false;
    }
}

/// <summary>Authoritative persistent Battle state with optional parent references.</summary>
public sealed class PersistentBattleStore
{
    private readonly ArmedForceStore armedForceStore;
    private readonly PersistentConflictStore conflictStore;
    private readonly PersistentWarStore warStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly LocalTopologyStore localTopologyStore;
    private readonly Dictionary<string, PersistentBattleRecord> recordsById =
        new Dictionary<string, PersistentBattleRecord>(StringComparer.Ordinal);
    private long revision;

    public PersistentBattleStore(
        ArmedForceStore armedForceStore,
        PersistentConflictStore conflictStore,
        PersistentWarStore warStore,
        SpatialAuthorityStore spatialAuthorityStore = null,
        LocalTopologyStore localTopologyStore = null)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.conflictStore = conflictStore ?? throw new ArgumentNullException(nameof(conflictStore));
        this.warStore = warStore ?? throw new ArgumentNullException(nameof(warStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? new SpatialAuthorityStore();
        this.localTopologyStore = localTopologyStore;
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public PersistentConflictStore ConflictStore => conflictStore;
    public PersistentWarStore WarStore => warStore;
    public SpatialAuthorityStore SpatialAuthorityStore => spatialAuthorityStore;
    public LocalTopologyStore LocalTopologyStore => localTopologyStore;
    public int Count => recordsById.Count;
    public long Revision => revision;

    public IReadOnlyList<PersistentBattleRecord> Records
    {
        get
        {
            List<PersistentBattleRecord> values = new List<PersistentBattleRecord>(recordsById.Values);
            values.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
            return new ReadOnlyCollection<PersistentBattleRecord>(values);
        }
    }

    public bool TryGet(BattleId battleId, out PersistentBattleRecord record)
    {
        record = null;
        return battleId != null && recordsById.TryGetValue(battleId.Value, out record);
    }

    public bool TryRegister(PersistentBattleRecord record, out PersistentStateFailure failure)
    {
        if (record == null || record.Id == null) return Fail(PersistentStateFailureCode.InvalidRecord, "A Battle requires a stable BattleId.", out failure);
        if (recordsById.ContainsKey(record.Id.Value)) return Fail(PersistentStateFailureCode.DuplicateIdentity, "The BattleId is already registered.", out failure);
        if (record.LifecycleState == BattleLifecycleState.Resolved) return Fail(PersistentStateFailureCode.BattleResolutionDeferred, "Battle resolution is outside this checkpoint.", out failure);
        if (ValidateRecord(record, false, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById.Add(record.Id.Value, record);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryAddParticipantBinding(BattleId battleId, BattleParticipantBinding binding, out PersistentStateFailure failure)
    {
        if (TryGet(battleId, out PersistentBattleRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The BattleId is not registered.", out failure);
        if (current.LifecycleState == BattleLifecycleState.Resolved) return Fail(PersistentStateFailureCode.BattleResolutionDeferred, "Battle resolution is outside this checkpoint.", out failure);
        if (ValidateBinding(current, binding, true, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = current.WithParticipantBinding(binding);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryStart(BattleId battleId, long startedAbsoluteDay, out PersistentStateFailure failure)
    {
        return TryStart(battleId, startedAbsoluteDay, null, out failure);
    }

    public bool TryStart(
        BattleId battleId,
        long startedAbsoluteDay,
        SpatialReference locationReference,
        out PersistentStateFailure failure)
    {
        if (TryGet(battleId, out PersistentBattleRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The BattleId is not registered.", out failure);
        if (current.LifecycleState != BattleLifecycleState.Pending) return Fail(PersistentStateFailureCode.InvalidLifecycle, "Only a pending Battle can become active.", out failure);
        if (startedAbsoluteDay < current.CreatedAbsoluteDay) return Fail(PersistentStateFailureCode.InvalidDay, "A Battle cannot start before it is created.", out failure);
        if (current.LocationReference != null
            && locationReference != null
            && current.LocationReference.Equals(locationReference) == false)
        {
            return Fail(PersistentStateFailureCode.BattleLocationImmutable, "A pending Battle cannot replace its explicit location during start.", out failure);
        }

        SpatialReference resolvedLocation = locationReference ?? current.LocationReference;
        if (resolvedLocation == null)
        {
            return Fail(PersistentStateFailureCode.BattleLocationRequired, "An active Battle requires an explicit physical SpatialReference.", out failure);
        }

        PersistentBattleRecord candidate = current.WithStarted(startedAbsoluteDay, resolvedLocation);
        if (ValidateRecord(candidate, false, out failure) == false) return false;
        if (CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = candidate;
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    internal PersistentBattleStore Clone(
        ArmedForceStore targetArmedForceStore,
        PersistentConflictStore targetConflictStore,
        PersistentWarStore targetWarStore,
        SpatialAuthorityStore targetSpatialAuthorityStore)
    {
        if (targetArmedForceStore == null) throw new ArgumentNullException(nameof(targetArmedForceStore));
        if (targetConflictStore == null) throw new ArgumentNullException(nameof(targetConflictStore));
        if (targetWarStore == null) throw new ArgumentNullException(nameof(targetWarStore));
        if (targetSpatialAuthorityStore == null) throw new ArgumentNullException(nameof(targetSpatialAuthorityStore));
        PersistentBattleStore copy = new PersistentBattleStore(
            targetArmedForceStore,
            targetConflictStore,
            targetWarStore,
            targetSpatialAuthorityStore,
            localTopologyStore);
        foreach (KeyValuePair<string, PersistentBattleRecord> entry in recordsById) copy.recordsById.Add(entry.Key, entry.Value);
        copy.revision = revision;
        if (copy.ValidateInvariants().IsValid == false) throw new ArgumentException("The BattleStore cannot be bound to the target stores.", nameof(targetArmedForceStore));
        return copy;
    }

    public PersistentStateInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        foreach (PersistentBattleRecord record in Records) ValidateRecordForDiagnostics(record, violations);
        return new PersistentStateInvariantReport(violations);
    }

    private bool ValidateRecord(PersistentBattleRecord record, bool requireActiveForce, out PersistentStateFailure failure)
    {
        if (record.ConflictId != null && conflictStore.TryGet(record.ConflictId, out _) == false) return Fail(PersistentStateFailureCode.ConflictNotRegistered, "The Battle ConflictId is not registered.", out failure);
        PersistentWarRecord war = null;
        if (record.WarId != null)
        {
            if (warStore.TryGet(record.WarId, out war) == false) return Fail(PersistentStateFailureCode.WarNotRegistered, "The Battle WarId is not registered.", out failure);
            if (record.ConflictId != null && war.ConflictId != null && war.ConflictId != record.ConflictId) return Fail(PersistentStateFailureCode.ContradictoryReference, "The Battle ConflictId contradicts its War ConflictId.", out failure);
        }
        if (record.Sides == null || record.Sides.Count < 2) return Fail(PersistentStateFailureCode.RequiresTwoSides, "A Battle requires at least two domain-owned sides.", out failure);
        if (record.LifecycleState == BattleLifecycleState.Pending && record.StartedAbsoluteDay.HasValue) return Fail(PersistentStateFailureCode.InvalidLifecycle, "A pending Battle cannot have a start day.", out failure);
        if (record.LifecycleState == BattleLifecycleState.Active && !record.StartedAbsoluteDay.HasValue) return Fail(PersistentStateFailureCode.InvalidLifecycle, "An active Battle requires a start day.", out failure);
        if (ValidateLocation(record, out failure) == false) return false;

        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleStateSide side in record.Sides)
        {
            if (side == null || side.SideId == null) return Fail(PersistentStateFailureCode.InvalidSide, "A Battle side requires a stable side identity.", out failure);
            if (side.BattleId != record.Id) return Fail(PersistentStateFailureCode.SideParentMismatch, "A Battle side must reference its owning Battle.", out failure);
            if (sideIds.Add(side.SideId.Value) == false) return Fail(PersistentStateFailureCode.DuplicateSide, "A Battle cannot register the same side twice.", out failure);
        }

        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleParticipantBinding binding in record.ParticipantBindings)
        {
            if (binding == null || binding.BindingId == null || binding.ArmedForceId == null) return Fail(PersistentStateFailureCode.InvalidBinding, "A Battle participant requires binding and ArmedForce identities.", out failure);
            if (binding.BattleId != record.Id) return Fail(PersistentStateFailureCode.BindingParentMismatch, "A Battle participant must reference its owning Battle.", out failure);
            if (binding.SideId == null || sideIds.Contains(binding.SideId.Value) == false) return Fail(PersistentStateFailureCode.SideNotRegistered, "A Battle participant must reference a registered side.", out failure);
            if (bindingIds.Add(binding.BindingId.Value) == false) return Fail(PersistentStateFailureCode.DuplicateBinding, "A Battle cannot register the same binding twice.", out failure);
            if (armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force) == false) return Fail(PersistentStateFailureCode.ForceNotRegistered, "The Battle participant ArmedForceId is not registered.", out failure);
            if (requireActiveForce && force.IsActive == false) return Fail(PersistentStateFailureCode.ForceNotActive, "A new Battle participant must reference an active ArmedForce.", out failure);
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private bool ValidateBinding(PersistentBattleRecord record, BattleParticipantBinding binding, bool requireActiveForce, out PersistentStateFailure failure)
    {
        PersistentBattleRecord candidate = record.WithParticipantBinding(binding);
        if (ValidateRecord(candidate, false, out failure) == false) return false;
        if (requireActiveForce)
        {
            if (binding == null || armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force) == false)
            {
                return Fail(PersistentStateFailureCode.ForceNotRegistered, "The Battle participant ArmedForceId is not registered.", out failure);
            }

            if (force.IsActive == false)
            {
                return Fail(PersistentStateFailureCode.ForceNotActive, "A new Battle participant must reference an active ArmedForce.", out failure);
            }
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private void ValidateRecordForDiagnostics(PersistentBattleRecord record, List<string> violations)
    {
        if (record == null || record.Id == null) { violations.Add("Battle record has no stable identity."); return; }
        if (record.LifecycleState == BattleLifecycleState.Resolved) violations.Add("Battle " + record.Id.Value + " is resolved although resolution is deferred.");
        if (record.ConflictId != null && conflictStore.TryGet(record.ConflictId, out _) == false) violations.Add("Battle " + record.Id.Value + " has a missing Conflict reference.");
        PersistentWarRecord war = null;
        if (record.WarId != null && warStore.TryGet(record.WarId, out war) == false) violations.Add("Battle " + record.Id.Value + " has a missing War reference.");
        if (record.ConflictId != null && record.WarId != null && war != null && war.ConflictId != null && war.ConflictId != record.ConflictId) violations.Add("Battle " + record.Id.Value + " has contradictory Conflict and War references.");
        if (record.Sides == null || record.Sides.Count < 2) violations.Add("Battle " + record.Id.Value + " has fewer than two sides.");
        if (record.LifecycleState == BattleLifecycleState.Pending && record.StartedAbsoluteDay.HasValue) violations.Add("Pending Battle " + record.Id.Value + " has a start day.");
        if ((record.LifecycleState == BattleLifecycleState.Active || record.LifecycleState == BattleLifecycleState.Resolved) && !record.StartedAbsoluteDay.HasValue) violations.Add("Battle " + record.Id.Value + " has no start day for its lifecycle.");
        if (record.LifecycleState == BattleLifecycleState.Active && record.LocationReference == null) violations.Add("Active Battle " + record.Id.Value + " has no physical location.");
        if (record.LocationReference != null && spatialAuthorityStore.TryResolve(record.LocationReference, localTopologyStore, out _, out SpatialAuthorityFailure spatialFailure) == false)
        {
            violations.Add("Battle " + record.Id.Value + " has an invalid physical location: " + spatialFailure.Code + ".");
        }
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleStateSide side in record.Sides ?? new List<BattleStateSide>())
        {
            if (side == null || side.SideId == null) { violations.Add("Battle " + record.Id.Value + " has an invalid side."); continue; }
            if (side.BattleId != record.Id) violations.Add("Battle " + record.Id.Value + " has a side with a mismatched parent.");
            if (sideIds.Add(side.SideId.Value) == false) violations.Add("Battle " + record.Id.Value + " has a duplicate side.");
        }
        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleParticipantBinding binding in record.ParticipantBindings ?? new List<BattleParticipantBinding>())
        {
            if (binding == null || binding.BindingId == null || binding.ArmedForceId == null) { violations.Add("Battle " + record.Id.Value + " has an invalid participant binding."); continue; }
            if (binding.BattleId != record.Id) violations.Add("Battle " + record.Id.Value + " has a participant with a mismatched parent.");
            if (binding.SideId == null || sideIds.Contains(binding.SideId.Value) == false) violations.Add("Battle " + record.Id.Value + " has a participant with a missing side.");
            if (bindingIds.Add(binding.BindingId.Value) == false) violations.Add("Battle " + record.Id.Value + " has a duplicate participant binding.");
            if (armedForceStore.TryGet(binding.ArmedForceId, out _) == false) violations.Add("Battle " + record.Id.Value + " has a participant with a missing ArmedForce.");
        }
    }

    private bool CanAdvance(out PersistentStateFailure failure)
    {
        if (revision == long.MaxValue) return Fail(PersistentStateFailureCode.RevisionOverflow, "The BattleStore revision cannot advance further.", out failure);
        failure = PersistentStateFailure.None;
        return true;
    }

    private bool ValidateLocation(PersistentBattleRecord record, out PersistentStateFailure failure)
    {
        if (record.LocationReference == null)
        {
            if (record.LifecycleState == BattleLifecycleState.Active)
            {
                return Fail(PersistentStateFailureCode.BattleLocationRequired, "An active Battle requires an explicit physical SpatialReference.", out failure);
            }

            failure = PersistentStateFailure.None;
            return true;
        }

        if (spatialAuthorityStore.TryResolve(
            record.LocationReference,
            localTopologyStore,
            out _,
            out SpatialAuthorityFailure spatialFailure) == false)
        {
            return Fail(
                PersistentStateFailureCode.BattleLocationInvalid,
                "Battle SpatialReference is not valid in the bound spatial authority: " + spatialFailure.Code + ".",
                out failure);
        }

        failure = PersistentStateFailure.None;
        return true;
    }

    private static bool Fail(PersistentStateFailureCode code, string message, out PersistentStateFailure failure)
    {
        failure = PersistentStateFailure.Create(code, message);
        return false;
    }
}
