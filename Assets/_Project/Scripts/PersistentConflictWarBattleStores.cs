using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>Authoritative persistent Battle state with optional parent references.</summary>
public sealed class PersistentBattleStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly ArmedForceStore armedForceStore;
    private readonly PersistentConflictStore conflictStore;
    private readonly PersistentWarStore warStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly LocalTopologyStore localTopologyStore;
    private readonly Dictionary<string, PersistentBattleRecord> recordsById =
        new Dictionary<string, PersistentBattleRecord>(StringComparer.Ordinal);
    private long revision;
    // Narrow deterministic fault seam for D7 transaction rollback verification.
    internal bool FailNextTerminalCommitForTests { get; set; }
    internal bool ThrowAfterTerminalWriteForTests { get; set; }

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

    internal bool TryPrepareTerminalWrite(
        PersistentBattleTerminalOutcome outcome,
        out PreparedBattleTerminalWrite prepared,
        out PersistentStateFailure failure)
    {
        prepared = null;
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (outcome == null || outcome.BattleId == null)
            return Fail(PersistentStateFailureCode.InvalidRecord, "A terminal Battle outcome is required.", out failure);
        if (!TryGet(outcome.BattleId, out PersistentBattleRecord current))
            return Fail(PersistentStateFailureCode.NotRegistered, "The BattleId is not registered.", out failure);
        if (current.LifecycleState != BattleLifecycleState.Active || current.TerminalOutcome != null)
            return Fail(PersistentStateFailureCode.InvalidLifecycle, "Only an Active Battle without an outcome can be resolved.", out failure);
        if (current.StartedAbsoluteDay == null
            || outcome.ResolvedAbsoluteDay < current.StartedAbsoluteDay.Value
            || outcome.BattleId != current.Id)
            return Fail(PersistentStateFailureCode.InvalidDay, "The terminal outcome does not identify a valid resolution day and Battle.", out failure);

        PersistentBattleRecord terminal = current.WithTerminalOutcome(outcome);
        if (!ValidateRecord(terminal, false, out failure) || !CanAdvance(out failure))
            return false;
        prepared = new PreparedBattleTerminalWrite(current, terminal, revision);
        failure = PersistentStateFailure.None;
        return true;
    }

    internal bool TryCommitTerminalWrite(
        PreparedBattleTerminalWrite prepared,
        out bool authoritativeWriteStarted,
        out PersistentStateFailure failure)
    {
        authoritativeWriteStarted = false;
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (prepared == null || prepared.ExpectedRecord == null || prepared.TerminalRecord == null)
            return Fail(PersistentStateFailureCode.InvalidRecord, "The prepared terminal Battle write is invalid.", out failure);
        if (revision != prepared.ExpectedStoreRevision
            || !recordsById.TryGetValue(prepared.ExpectedRecord.Id.Value, out PersistentBattleRecord current)
            || !ReferenceEquals(current, prepared.ExpectedRecord))
            return Fail(PersistentStateFailureCode.InvalidLifecycle, "The Battle changed after terminal-write preparation.", out failure);
        if (!CanAdvance(out failure)) return false;
        if (FailNextTerminalCommitForTests)
        {
            FailNextTerminalCommitForTests = false;
            return Fail(PersistentStateFailureCode.InvalidRecord, "Injected terminal Battle commit failure.", out failure);
        }
        authoritativeWriteStarted = true;
        recordsById[prepared.ExpectedRecord.Id.Value] = prepared.TerminalRecord;
        revision++;
        if (ThrowAfterTerminalWriteForTests)
        {
            ThrowAfterTerminalWriteForTests = false;
            throw new InvalidOperationException("Injected exception after terminal Battle assignment.");
        }
        failure = PersistentStateFailure.None;
        return true;
    }

    internal bool RestoreBattleTransactionSnapshot(PersistentBattleRecord record, long storeRevision)
    {
        if (record == null || record.Id == null || storeRevision < 0L
            || !recordsById.ContainsKey(record.Id.Value))
            return false;
        recordsById[record.Id.Value] = record;
        revision = storeRevision;
        return true;
    }

    public bool TryRegister(PersistentBattleRecord record, out PersistentStateFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

        if (record == null || record.Id == null) return Fail(PersistentStateFailureCode.InvalidRecord, "A Battle requires a stable BattleId.", out failure);
        if (recordsById.ContainsKey(record.Id.Value)) return Fail(PersistentStateFailureCode.DuplicateIdentity, "The BattleId is already registered.", out failure);
        if (record.LifecycleState == BattleLifecycleState.Resolved || record.TerminalOutcome != null)
            return Fail(PersistentStateFailureCode.BattleResolutionDeferred, "Normal Battle registration cannot create a resolved Battle or supply a terminal outcome.", out failure);
        if (ValidateRecord(record, false, out failure) == false || CanAdvance(out failure) == false) return false;
        recordsById.Add(record.Id.Value, record);
        revision++;
        failure = PersistentStateFailure.None;
        return true;
    }

    public bool TryAddParticipantBinding(BattleId battleId, BattleParticipantBinding binding, out PersistentStateFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

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
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersistentStateFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);

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

    internal static bool TryCreateFromOwnerSnapshot(
        PersistentBattleOwnerSnapshot snapshot,
        ArmedForceStore targetArmedForceStore,
        PersistentConflictStore targetConflictStore,
        PersistentWarStore targetWarStore,
        SpatialAuthorityStore targetSpatialAuthorityStore,
        LocalTopologyStore targetLocalTopologyStore,
        out PersistentBattleStore stagedStore,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        stagedStore = null;
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
            "A Battle owner snapshot and all staged parent authorities are required.");
        if (snapshot == null
            || targetArmedForceStore == null
            || targetConflictStore == null
            || targetWarStore == null
            || targetSpatialAuthorityStore == null)
            return false;
        if (targetLocalTopologyStore != null)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.LocalTopologyComposed,
                "Daily-v1 staged Battle owners require LocalTopologyStore to remain NOT_COMPOSED.");
            return false;
        }

        if (!snapshot.TryBuildRecords(
                targetArmedForceStore,
                targetConflictStore,
                targetWarStore,
                targetSpatialAuthorityStore,
                targetLocalTopologyStore,
                out IReadOnlyList<PersistentBattleRecord> stagedRecords,
                out failure))
            return false;

        PersistentBattleStore candidate = new PersistentBattleStore(
            targetArmedForceStore,
            targetConflictStore,
            targetWarStore,
            targetSpatialAuthorityStore,
            null);
        foreach (PersistentBattleRecord record in stagedRecords)
        {
            if (candidate.recordsById.ContainsKey(record.Id.Value))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.DuplicateBattleIdentity,
                    "The Battle owner snapshot contains a duplicate BattleId.");
                return false;
            }
            candidate.recordsById.Add(record.Id.Value, record);
        }
        candidate.revision = snapshot.Revision;
        if (candidate.recordsById.Count != snapshot.RecordCount || !candidate.ValidateInvariants().IsValid)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.StageFailed,
                "The privately staged Battle owner failed its existing invariant validation.");
            return false;
        }

        stagedStore = candidate;
        failure = PersistentBattleOwnerSnapshotFailure.None;
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
        if (record.LifecycleState == BattleLifecycleState.Pending
            && (record.StartedAbsoluteDay.HasValue || record.TerminalOutcome != null))
            return Fail(PersistentStateFailureCode.InvalidLifecycle, "A pending Battle cannot have a start day or terminal outcome.", out failure);
        if (record.LifecycleState == BattleLifecycleState.Active
            && (!record.StartedAbsoluteDay.HasValue || record.TerminalOutcome != null))
            return Fail(PersistentStateFailureCode.InvalidLifecycle, "An active Battle requires a start day and cannot have a terminal outcome.", out failure);
        if (record.LifecycleState == BattleLifecycleState.Resolved)
        {
            PersistentBattleTerminalOutcome outcome = record.TerminalOutcome;
            if (!record.StartedAbsoluteDay.HasValue || outcome == null)
                return Fail(PersistentStateFailureCode.InvalidLifecycle, "A resolved Battle requires a start day and exactly one terminal outcome.", out failure);
            if (outcome.BattleId != record.Id || outcome.ResolvedAbsoluteDay < record.StartedAbsoluteDay.Value)
                return Fail(PersistentStateFailureCode.InvalidDay, "A resolved Battle terminal outcome has an invalid identity or day.", out failure);
            if (outcome.OutcomeType == BattleOutcomeType.Victory
                && (outcome.WinningBattleSideId == null
                    || !ContainsSide(record.Sides, outcome.WinningBattleSideId)))
                return Fail(PersistentStateFailureCode.InvalidSide, "A Battle victory must name a registered winning side.", out failure);
            if (outcome.OutcomeType == BattleOutcomeType.Draw && outcome.WinningBattleSideId != null)
                return Fail(PersistentStateFailureCode.InvalidRecord, "A Battle draw cannot name a winning side.", out failure);
            if (!HasValidAcceptedProvenance(outcome.Provenance))
                return Fail(PersistentStateFailureCode.InvalidRecord, "A resolved Battle requires complete accepted D5/D6B2 provenance.", out failure);
        }
        else if (record.TerminalOutcome != null)
        {
            return Fail(PersistentStateFailureCode.InvalidLifecycle, "Only a resolved Battle may have a terminal outcome.", out failure);
        }
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

    private static bool ContainsSide(IReadOnlyList<BattleStateSide> sides, BattleSideId sideId)
    {
        if (sides == null || sideId == null) return false;
        foreach (BattleStateSide side in sides)
            if (side?.SideId == sideId) return true;
        return false;
    }

    private void ValidateRecordForDiagnostics(PersistentBattleRecord record, List<string> violations)
    {
        if (record == null || record.Id == null) { violations.Add("Battle record has no stable identity."); return; }
        if (record.LifecycleState == BattleLifecycleState.Resolved && record.TerminalOutcome == null) violations.Add("Resolved Battle " + record.Id.Value + " has no terminal outcome.");
        if (record.LifecycleState != BattleLifecycleState.Resolved && record.TerminalOutcome != null) violations.Add("Non-resolved Battle " + record.Id.Value + " has a terminal outcome.");
        if (record.ConflictId != null && conflictStore.TryGet(record.ConflictId, out _) == false) violations.Add("Battle " + record.Id.Value + " has a missing Conflict reference.");
        PersistentWarRecord war = null;
        if (record.WarId != null && warStore.TryGet(record.WarId, out war) == false) violations.Add("Battle " + record.Id.Value + " has a missing War reference.");
        if (record.ConflictId != null && record.WarId != null && war != null && war.ConflictId != null && war.ConflictId != record.ConflictId) violations.Add("Battle " + record.Id.Value + " has contradictory Conflict and War references.");
        if (record.Sides == null || record.Sides.Count < 2) violations.Add("Battle " + record.Id.Value + " has fewer than two sides.");
        if (record.LifecycleState == BattleLifecycleState.Pending && record.StartedAbsoluteDay.HasValue) violations.Add("Pending Battle " + record.Id.Value + " has a start day.");
        if ((record.LifecycleState == BattleLifecycleState.Active || record.LifecycleState == BattleLifecycleState.Resolved) && !record.StartedAbsoluteDay.HasValue) violations.Add("Battle " + record.Id.Value + " has no start day for its lifecycle.");
        if (record.LifecycleState == BattleLifecycleState.Active && record.LocationReference == null) violations.Add("Active Battle " + record.Id.Value + " has no physical location.");
        if (record.LifecycleState == BattleLifecycleState.Resolved && record.LocationReference == null) violations.Add("Resolved Battle " + record.Id.Value + " has no retained physical location.");
        if (record.TerminalOutcome != null)
        {
            PersistentBattleTerminalOutcome outcome = record.TerminalOutcome;
            if (outcome.BattleId != record.Id) violations.Add("Resolved Battle " + record.Id.Value + " has a mismatched outcome BattleId.");
            if (outcome.ResolvedAbsoluteDay < 0L || (record.StartedAbsoluteDay.HasValue && outcome.ResolvedAbsoluteDay < record.StartedAbsoluteDay.Value)) violations.Add("Resolved Battle " + record.Id.Value + " has an invalid outcome day.");
            if (outcome.OutcomeType == BattleOutcomeType.Victory && !ContainsSide(record.Sides, outcome.WinningBattleSideId)) violations.Add("Resolved Battle " + record.Id.Value + " has no registered winning side.");
            if (outcome.OutcomeType == BattleOutcomeType.Draw && outcome.WinningBattleSideId != null) violations.Add("Resolved Battle " + record.Id.Value + " draw has a winner.");
            if (!HasValidAcceptedProvenance(outcome.Provenance)) violations.Add("Resolved Battle " + record.Id.Value + " has incomplete accepted provenance.");
        }
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

    private static bool HasValidAcceptedProvenance(PersistentBattleOutcomeProvenance provenance)
    {
        BattleResolutionProvenance d5 = provenance?.D5Resolution;
        return d5 != null
            && !string.IsNullOrWhiteSpace(d5.PolicyFingerprint)
            && !string.IsNullOrWhiteSpace(d5.NumericExecutionProfileKey)
            && !string.IsNullOrWhiteSpace(d5.ProjectionVersion)
            && !string.IsNullOrWhiteSpace(d5.CausalResolutionFingerprint)
            && !string.IsNullOrWhiteSpace(d5.SourceContextFingerprint)
            && !string.IsNullOrWhiteSpace(d5.CapabilityRuleKey)
            && !string.IsNullOrWhiteSpace(d5.RandomAuthorityRuleKey)
            && !string.IsNullOrWhiteSpace(d5.ResolverSettingsIdentity)
            && !string.IsNullOrWhiteSpace(provenance.D6B2PolicyFingerprint)
            && !string.IsNullOrWhiteSpace(provenance.D6B2PlanSchemaVersion)
            && !string.IsNullOrWhiteSpace(provenance.D6B2CoverageVersion)
            && !string.IsNullOrWhiteSpace(provenance.D6B2PlanFingerprint);
    }

    private bool ValidateLocation(PersistentBattleRecord record, out PersistentStateFailure failure)
    {
        if (record.LocationReference == null)
        {
            if (record.LifecycleState == BattleLifecycleState.Active
                || record.LifecycleState == BattleLifecycleState.Resolved)
            {
                return Fail(PersistentStateFailureCode.BattleLocationRequired, "An active or resolved Battle requires its explicit physical SpatialReference.", out failure);
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

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
