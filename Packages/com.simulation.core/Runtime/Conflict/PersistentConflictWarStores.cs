using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Authoritative persistent Conflict state. This store is deliberately
/// independent from the lower-level ConflictFoundation resolution primitive.
/// </summary>
public sealed class PersistentConflictStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
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
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

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
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

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
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

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

    internal static bool TryCreateFromOwnerSnapshot(
        PersistentConflictOwnerSnapshot snapshot,
        ArmedForceStore targetArmedForceStore,
        out PersistentConflictStore stagedStore,
        out PersistentConflictOwnerSnapshotFailure failure)
    {
        stagedStore = null;
        failure = PersistentConflictOwnerSnapshotFailure.Create(
            PersistentConflictOwnerSnapshotFailureCode.InvalidSnapshot,
            "A Conflict owner snapshot and its staged ArmedForce authority are required.");
        if (snapshot == null || targetArmedForceStore == null)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidParentComposition,
                "A Conflict owner snapshot and its staged ArmedForce authority are required.");
            return false;
        }
        if (!snapshot.TryBuildRecords(
                targetArmedForceStore,
                out IReadOnlyList<PersistentConflictRecord> stagedRecords,
                out failure))
            return false;

        PersistentConflictStore candidate = new PersistentConflictStore(targetArmedForceStore);
        foreach (PersistentConflictRecord record in stagedRecords)
        {
            if (record == null || record.Id == null || candidate.recordsById.ContainsKey(record.Id.Value))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.DuplicateConflictIdentity,
                    "The staged Conflict rows contain an invalid or duplicate ConflictId.");
                return false;
            }
            candidate.recordsById.Add(record.Id.Value, record);
        }
        candidate.revision = snapshot.Revision;
        if (candidate.recordsById.Count != snapshot.RecordCount
            || !candidate.ValidateInvariants().IsValid)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.StageFailed,
                "The privately staged Conflict owner failed its existing invariant validation.");
            return false;
        }

        stagedStore = candidate;
        failure = PersistentConflictOwnerSnapshotFailure.None;
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

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

/// <summary>Authoritative persistent War state with optional Conflict reference.</summary>
public sealed class PersistentWarStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly ArmedForceStore armedForceStore;
    private readonly PersistentConflictStore conflictStore;
    private readonly FactionStore factionStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly ArmedForceSpatialStateStore p16StateStore;
    private readonly Dictionary<string, PersistentWarRecord> recordsById =
        new Dictionary<string, PersistentWarRecord>(StringComparer.Ordinal);
    private long revision;

    public PersistentWarStore(ArmedForceStore armedForceStore, PersistentConflictStore conflictStore)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.conflictStore = conflictStore ?? throw new ArgumentNullException(nameof(conflictStore));
    }

    public PersistentWarStore(ArmedForceStore armedForceStore, PersistentConflictStore conflictStore,
        FactionStore factionStore, SpatialAuthorityStore spatialAuthorityStore, ArmedForceSpatialStateStore p16StateStore)
        : this(armedForceStore, conflictStore)
    {
        this.factionStore = factionStore ?? throw new ArgumentNullException(nameof(factionStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore));
        this.p16StateStore = p16StateStore ?? throw new ArgumentNullException(nameof(p16StateStore));
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public PersistentConflictStore ConflictStore => conflictStore;
    public int Count => recordsById.Count;
    public long Revision => revision;

    public PersistentWarStoreSnapshot CaptureState() => new PersistentWarStoreSnapshot(revision, Records);

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
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

        if (record == null || record.Id == null) return Fail(PersistentStateFailureCode.InvalidRecord, "A War requires a stable WarId.", out failure);
        if (record.P17A != null) return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A state must be installed through initial-only TryConfigureP17A on an existing War.", out failure);
        if (recordsById.ContainsKey(record.Id.Value)) return Fail(PersistentStateFailureCode.DuplicateIdentity, "The WarId is already registered.", out failure);
        if (ValidateRecord(record, false, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById.Add(record.Id.Value, record);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryAddParticipantBinding(WarId warId, WarParticipantBinding binding, out PersistentStateFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

        if (TryGet(warId, out PersistentWarRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The WarId is not registered.", out failure);
        if (current.IsActive == false) return Fail(PersistentStateFailureCode.StateEnded, "An ended War cannot receive a new participant.", out failure);
        if (current.P17A != null) return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A War force bindings are immutable after configuration.", out failure);
        if (ValidateBinding(current, binding, true, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = current.WithParticipantBinding(binding);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryEnd(WarId warId, long endedAbsoluteDay, out PersistentStateFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

        if (TryGet(warId, out PersistentWarRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The WarId is not registered.", out failure);
        if (current.IsActive == false) return Fail(PersistentStateFailureCode.StateEnded, "The War is already ended.", out failure);
        if (current.P17A != null) return Fail(PersistentStateFailureCode.InvalidRecord, "A P17-A War requires an explicit reasoned concession to end.", out failure);
        if (endedAbsoluteDay < current.CreatedAbsoluteDay) return Fail(PersistentStateFailureCode.InvalidDay, "A War cannot end before it is created.", out failure);
        if (CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = current.WithEnded(endedAbsoluteDay);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryConfigureP17A(WarId warId, P17AWarStrategicSection section, out PersistentStateFailure failure)
    {
        if (!mutationGuardBinding.CanMutate) return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (mutationGuardBinding.BoundGuard != null) return Fail(PersistentStateFailureCode.InvalidLifecycle, "P17-A configuration is initial-only and must precede runtime publication.", out failure);
        if (TryGet(warId, out PersistentWarRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The WarId is not registered.", out failure);
        if (section == null) return Fail(PersistentStateFailureCode.InvalidRecord, "A complete P17-A strategic section is required.", out failure);
        if (current.P17A != null || !current.IsActive) return Fail(PersistentStateFailureCode.InvalidLifecycle, "P17-A can be configured once on an active prepublication War.", out failure);
        PersistentWarRecord candidate = current.WithP17A(section);
        if (ValidateRecord(candidate, true, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById[current.Id.Value] = candidate;
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    internal bool TryConcedeP17A(WarId warId, long expectedRevision, long acceptedAbsoluteDay,
        WarTerminalConcession concession, out PersistentStateFailure failure)
    {
        if (!mutationGuardBinding.CanMutate) return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (TryGet(warId, out PersistentWarRecord current) == false) return Fail(PersistentStateFailureCode.NotRegistered, "The WarId is not registered.", out failure);
        if (current.P17A == null) return Fail(PersistentStateFailureCode.InvalidRecord, "The War is not configured for P17-A.", out failure);
        if (!current.IsActive) return Fail(PersistentStateFailureCode.StateEnded, "The War is already ended.", out failure);
        if (expectedRevision != revision) return Fail(PersistentStateFailureCode.InvalidRecord, "The WarStore revision changed before concession.", out failure);
        if (p16StateStore?.P16Profile == null)
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A concession requires its selected P16 profile.", out failure);
        if (!ContainsParticipant(current.P17A.Participants, concession?.ConcedingParticipantId, out WarStrategicParticipant participant))
            return Fail(PersistentStateFailureCode.InvalidRecord, "The concession participant is not registered in this War.", out failure);
        if (concession == null || concession.Reason != WarConcessionReason.Concession
            || concession.AuthorityId != current.P17A.ScenarioAuthorityId
            || (concession.AcceptedOrigin != WorldCommandOrigin.GM && concession.AcceptedOrigin != WorldCommandOrigin.Scenario)
            || concession.AcceptedAbsoluteDay != acceptedAbsoluteDay || concession.AcceptedOrder != 0L
            || acceptedAbsoluteDay <= p16StateStore.P16Profile.TargetBoundaryDay
            || acceptedAbsoluteDay < current.CreatedAbsoluteDay
            || participant.SideId == null)
            return Fail(PersistentStateFailureCode.InvalidRecord, "The concession does not match the configured authority, participant, day, or order.", out failure);
        if (CanAdvance(out failure) == false) return false;
        P17AWarStrategicSection updated = current.P17A.WithTerminalConcession(concession);
        PersistentWarRecord candidate = current.WithP17AEnded(acceptedAbsoluteDay, updated);
        if (ValidateP17A(candidate, out failure) == false) return false;
        recordsById[current.Id.Value] = candidate;
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    internal static bool TryCreateFromOwnerSnapshot(
        PersistentWarOwnerSnapshot snapshot,
        ArmedForceStore stagedArmedForceStore,
        PersistentConflictStore stagedConflictStore,
        out PersistentWarStore stagedWarStore,
        out PersistentWarOwnerSnapshotFailure failure)
    {
        stagedWarStore = null;
        failure = PersistentWarOwnerSnapshotFailure.Create(
            PersistentWarOwnerSnapshotFailureCode.InvalidSnapshot,
            "A War owner snapshot and its staged ArmedForce and Conflict authorities are required.");
        if (snapshot == null || stagedArmedForceStore == null || stagedConflictStore == null)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidParentComposition,
                "A War owner snapshot and its staged ArmedForce and Conflict authorities are required.");
            return false;
        }
        if (!ReferenceEquals(stagedConflictStore.ArmedForceStore, stagedArmedForceStore))
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidParentComposition,
                "The staged Conflict store must reference the exact staged ArmedForce store.");
            return false;
        }
        if (!snapshot.TryBuildRecords(
                stagedArmedForceStore,
                stagedConflictStore,
                out IReadOnlyList<PersistentWarRecord> stagedRecords,
                out failure))
            return false;

        PersistentWarStore candidate = new PersistentWarStore(stagedArmedForceStore, stagedConflictStore);
        foreach (PersistentWarRecord record in stagedRecords)
        {
            if (record == null || record.Id == null || candidate.recordsById.ContainsKey(record.Id.Value))
            {
                failure = PersistentWarOwnerSnapshotFailure.Create(
                    PersistentWarOwnerSnapshotFailureCode.DuplicateWarIdentity,
                    "The staged War rows contain an invalid or duplicate WarId.");
                return false;
            }
            candidate.recordsById.Add(record.Id.Value, record);
        }
        candidate.revision = snapshot.Revision;
        if (candidate.recordsById.Count != snapshot.RecordCount
            || !candidate.ValidateInvariants().IsValid)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.StageFailed,
                "The privately staged War owner failed its existing invariant validation.");
            return false;
        }

        stagedWarStore = candidate;
        failure = PersistentWarOwnerSnapshotFailure.None;
        return true;
    }

    internal PersistentWarStore Clone(ArmedForceStore targetArmedForceStore, PersistentConflictStore targetConflictStore)
    {
        if (targetArmedForceStore == null) throw new ArgumentNullException(nameof(targetArmedForceStore));
        if (targetConflictStore == null) throw new ArgumentNullException(nameof(targetConflictStore));
        if (recordsById.Values.Any(record => record?.P17A != null))
            throw new ArgumentException("Cloning P17-A War state requires cloned Faction, spatial and P16 owners.", nameof(targetArmedForceStore));
        PersistentWarStore copy = new PersistentWarStore(targetArmedForceStore, targetConflictStore);
        foreach (KeyValuePair<string, PersistentWarRecord> entry in recordsById) copy.recordsById.Add(entry.Key, entry.Value);
        copy.revision = revision;
        if (copy.ValidateInvariants().IsValid == false) throw new ArgumentException("The WarStore cannot be bound to the target stores.", nameof(targetArmedForceStore));
        return copy;
    }

    internal PersistentWarStore Clone(ArmedForceStore targetArmedForceStore, PersistentConflictStore targetConflictStore,
        FactionStore targetFactionStore, SpatialAuthorityStore targetSpatialAuthorityStore,
        ArmedForceSpatialStateStore targetP16StateStore)
    {
        PersistentWarStore copy = new PersistentWarStore(targetArmedForceStore, targetConflictStore,
            targetFactionStore, targetSpatialAuthorityStore, targetP16StateStore);
        foreach (KeyValuePair<string, PersistentWarRecord> entry in recordsById) copy.recordsById.Add(entry.Key, entry.Value);
        copy.revision = revision;
        if (!copy.ValidateInvariants().IsValid) throw new ArgumentException("The WarStore cannot be bound to the target stores.", nameof(targetArmedForceStore));
        return copy;
    }

    internal bool TryValidateSnapshotForHydration(PersistentWarStoreSnapshot snapshot, out string diagnostic)
    {
        diagnostic = string.Empty;
        if (snapshot == null || snapshot.Records == null || snapshot.Revision < 0L)
        { diagnostic = "The staged WarStore snapshot is incomplete."; return false; }
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (PersistentWarRecord record in snapshot.Records)
        {
            if (record?.Id == null || !ids.Add(record.Id.Value))
            { diagnostic = "The staged WarStore snapshot has a missing or duplicate WarId."; return false; }
            if (!ValidateRecord(record, false, out _))
            { diagnostic = "The staged War record is inconsistent with its P7/P17 owner references."; return false; }
        }
        return true;
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

        if (record.P17A != null && ValidateP17A(record, out failure) == false) return false;

        failure = PersistentStateFailure.None;
        return true;
    }

    private bool ValidateP17A(PersistentWarRecord record, out PersistentStateFailure failure)
    {
        P17AWarStrategicSection section = record?.P17A;
        if (section == null || factionStore == null || spatialAuthorityStore == null || p16StateStore == null)
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A War validation requires the Faction, spatial and P16 owners.", out failure);
        P16AStateSnapshot p16Snapshot = p16StateStore.CaptureP16AState();
        if (p16Snapshot == null || !p16Snapshot.RequiresP17AProvenance
            || p16Snapshot.TrustedAuthorityId != section.ScenarioAuthorityId)
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A requires matching retained P16 scenario authority provenance.", out failure);
        if (section.Participants == null || section.Participants.Count != 2 || record.Sides.Count != 2 || record.ParticipantBindings.Count != 2)
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A requires exactly two participants, sides and force bindings.", out failure);
        HashSet<string> strategicIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> participantSides = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarStrategicParticipant participant in section.Participants)
        {
            if (participant == null || participant.Id == null || participant.WarId != record.Id || participant.FactionId == null || participant.SideId == null
                || !strategicIds.Add(participant.Id.Value) || !factionIds.Add(participant.FactionId.Value)
                || !participantSides.Add(participant.SideId.Value)
                || !factionStore.TryGet(participant.FactionId, out FactionRecord faction)
                || faction.CreatedAbsoluteDay > record.CreatedAbsoluteDay)
                return Fail(PersistentStateFailureCode.InvalidRecord, "A P17-A participant has an invalid parent, duplicate identity, missing Faction or late Faction.", out failure);
        }
        if (participantSides.Count != 2 || !participantSides.SetEquals(record.Sides.Select(side => side?.SideId?.Value)))
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A participants must map exactly to the War's two sides.", out failure);
        HashSet<string> bindingSides = new HashSet<string>(StringComparer.Ordinal);
        foreach (WarParticipantBinding binding in record.ParticipantBindings)
        {
            if (binding?.SideId == null || !bindingSides.Add(binding.SideId.Value)
                || !armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force) || !force.IsActive)
                return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A requires one active force binding on each side.", out failure);
        }
        if (!bindingSides.SetEquals(participantSides))
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A force bindings must cover each side exactly once.", out failure);
        WarWithdrawalDemand goal = section.WithdrawalDemand;
        if (p16StateStore.P16Profile == null)
            return Fail(PersistentStateFailureCode.InvalidRecord, "P17-A requires a selected P16 profile.", out failure);
        if (goal == null || goal.WarId != record.Id || goal.SourceHexId == null || goal.ActivatedAbsoluteDay < record.CreatedAbsoluteDay
            || !spatialAuthorityStore.TryGet(goal.SourceHexId, out _))
            return Fail(PersistentStateFailureCode.InvalidRecord, "The P17-A withdrawal demand has invalid identities, timing or source Hex.", out failure);
        if (!TryGetParticipant(section.Participants, goal.OwnerParticipantId, out WarStrategicParticipant owner)
            || !TryGetParticipant(section.Participants, goal.TargetParticipantId, out WarStrategicParticipant target)
            || owner.Id == target.Id || owner.SideId == target.SideId
            || goal.ActivatedAbsoluteDay >= p16StateStore.P16Profile.TargetBoundaryDay)
            return Fail(PersistentStateFailureCode.InvalidRecord, "The P17-A withdrawal demand must name opposing registered participants before the P16 target day.", out failure);
        WarParticipantBinding targetBinding = null;
        foreach (WarParticipantBinding binding in record.ParticipantBindings)
            if (binding.BindingId == goal.TargetBindingId) targetBinding = binding;
        if (targetBinding == null || targetBinding.SideId != target.SideId || p16StateStore.P16Profile == null
            || targetBinding.ArmedForceId != p16StateStore.P16Profile.SelectedForceId
            || !InitialPositionMatchesGoal(goal, targetBinding.ArmedForceId))
            return Fail(PersistentStateFailureCode.InvalidRecord, "The target binding must resolve to the selected P16 force initially at the demanded Hex.", out failure);
        if (section.TerminalConcession == null)
        {
            if (record.LifecycleState != WarLifecycleState.Active || record.EndedAbsoluteDay.HasValue)
                return Fail(PersistentStateFailureCode.InvalidLifecycle, "A P17-A War without concession must remain Active.", out failure);
        }
        else
        {
            WarTerminalConcession concession = section.TerminalConcession;
            if (record.LifecycleState != WarLifecycleState.Ended || record.EndedAbsoluteDay != concession.AcceptedAbsoluteDay
                || concession.AcceptedAbsoluteDay <= p16StateStore.P16Profile.TargetBoundaryDay || concession.AcceptedOrder != 0L
                || concession.AuthorityId != section.ScenarioAuthorityId || concession.Reason != WarConcessionReason.Concession
                || (concession.AcceptedOrigin != WorldCommandOrigin.GM && concession.AcceptedOrigin != WorldCommandOrigin.Scenario)
                || concession.ConcedingParticipantId != goal.TargetParticipantId || string.IsNullOrWhiteSpace(concession.OperationId))
                return Fail(PersistentStateFailureCode.InvalidLifecycle, "The P17-A terminal state must be a valid reasoned concession by the target participant.", out failure);
        }
        failure = PersistentStateFailure.None;
        return true;
    }

    private bool InitialPositionMatchesGoal(WarWithdrawalDemand goal, ArmedForceId selectedForce)
    {
        if (p16StateStore.CaptureP16AState()?.Receipt is P16ACrossingReceipt receipt)
            return receipt.ForceId == selectedForce.Value && receipt.SourceHexId == goal.SourceHexId.Value;
        return p16StateStore.TryGetPosition(selectedForce, out SpatialReference position)
            && position?.Kind == SpatialReferenceKind.Hex && position.HexId == goal.SourceHexId;
    }

    private static bool TryGetParticipant(IReadOnlyList<WarStrategicParticipant> values, WarStrategicParticipantId id, out WarStrategicParticipant participant)
    {
        participant = null;
        if (values == null || id == null) return false;
        foreach (WarStrategicParticipant value in values) if (value?.Id == id) { participant = value; return true; }
        return false;
    }

    private static bool ContainsParticipant(IReadOnlyList<WarStrategicParticipant> values, WarStrategicParticipantId id, out WarStrategicParticipant participant) =>
        TryGetParticipant(values, id, out participant);

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
        if (record.P17A != null && !ValidateP17A(record, out _)) violations.Add("War " + record.Id.Value + " has invalid P17-A strategic state.");
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

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
