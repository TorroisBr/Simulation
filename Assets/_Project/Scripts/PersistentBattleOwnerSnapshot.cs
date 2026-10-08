using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum PersistentBattleOwnerSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidBattleOwner,
    InvalidParentComposition,
    InvalidOwnerSectionVector,
    LocalTopologyComposed,
    InvalidSnapshot,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidBattleIdentity,
    DuplicateBattleIdentity,
    InvalidLifecycle,
    MissingConflict,
    MissingWar,
    ContradictoryParents,
    InvalidSide,
    DuplicateSide,
    InvalidBinding,
    DuplicateBinding,
    MissingForce,
    InvalidSpatialReference,
    UnsupportedSubLocation,
    InvalidTerminalOutcome,
    InvalidProvenance,
    StageFailed
}

internal sealed class PersistentBattleOwnerSnapshotFailure
{
    internal static readonly PersistentBattleOwnerSnapshotFailure None =
        new PersistentBattleOwnerSnapshotFailure(PersistentBattleOwnerSnapshotFailureCode.None, string.Empty);

    internal PersistentBattleOwnerSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private PersistentBattleOwnerSnapshotFailure(
        PersistentBattleOwnerSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static PersistentBattleOwnerSnapshotFailure Create(
        PersistentBattleOwnerSnapshotFailureCode code,
        string message)
    {
        return code == PersistentBattleOwnerSnapshotFailureCode.None
            ? None
            : new PersistentBattleOwnerSnapshotFailure(code, message);
    }
}

/// <summary>
/// Detached schema-v1 value export for the admitted Daily-v1 Battle owner.
/// Object identity and the ephemeral P12-B capture token are verified during
/// capture but are never retained by this value.
/// </summary>
internal sealed class PersistentBattleOwnerSnapshot
{
    internal const string SectionId = PersistentBattleCensusProvider.SectionId;
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<PersistentBattleOwnerSnapshotRecord> Records { get; }

    internal PersistentBattleOwnerSnapshot(
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<PersistentBattleOwnerSnapshotRecord> records)
    {
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        if (records == null)
        {
            Records = null;
            return;
        }

        List<PersistentBattleOwnerSnapshotRecord> copied = new List<PersistentBattleOwnerSnapshotRecord>();
        foreach (PersistentBattleOwnerSnapshotRecord record in records)
            copied.Add(record?.Copy());
        Records = new ReadOnlyCollection<PersistentBattleOwnerSnapshotRecord>(copied);
    }

    internal static bool TryCapture(
        PersistentBattleStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out PersistentBattleOwnerSnapshot snapshot,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        snapshot = null;
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidBattleOwner,
            "The Daily-v1 Battle owner is invalid.");
        if (owner == null
            || token == null
            || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidCaptureContext,
                "Battle capture requires the exact owner-section vector carried by the P12-B token.");
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.UnsupportedProfile,
                "Battle owner export requires a successful Daily-v1 completed-boundary token.");
            return false;
        }

        if (owner.LocalTopologyStore != null)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.LocalTopologyComposed,
                "Daily-v1 LocalTopology is NOT_COMPOSED; an injected owner rejects Battle capture.");
            return false;
        }
        if (!ReferenceEquals(owner.ConflictStore.ArmedForceStore, owner.ArmedForceStore)
            || !ReferenceEquals(owner.WarStore.ArmedForceStore, owner.ArmedForceStore)
            || !ReferenceEquals(owner.WarStore.ConflictStore, owner.ConflictStore))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidParentComposition,
                "The Battle owner must use the exact installed ArmedForce, Conflict, and War authorities.");
            return false;
        }

        IReadOnlyList<PersistentBattleRecord> sourceRecords = owner.Records;
        int sourceCount = owner.Count;
        long sourceRevision = owner.Revision;
        if (sourceRecords == null || sourceCount < 0 || sourceRevision < 0L || sourceRecords.Count != sourceCount)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidBattleOwner,
                "Battle owner count, revision, and retained record view are inconsistent.");
            return false;
        }

        if (!MatchesUniqueRequiredSection(
                sharedOwnerSectionVector,
                SectionId,
                PersistentBattleCensusProvider.SchemaVersion,
                owner,
                sourceCount,
                sourceRevision))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "The P12-B vector does not contain exactly one matching p12e.battles owner witness.");
            return false;
        }

        List<PersistentBattleOwnerSnapshotRecord> copiedRecords =
            new List<PersistentBattleOwnerSnapshotRecord>(sourceRecords.Count);
        foreach (PersistentBattleRecord source in sourceRecords)
        {
            if (!TryCopyRecord(source, out PersistentBattleOwnerSnapshotRecord copied))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidBattleOwner,
                    "A retained Battle row could not be copied as detached values.");
                return false;
            }
            copiedRecords.Add(copied);
        }

        if (owner.Count != sourceCount
            || owner.Revision != sourceRevision
            || !MatchesUniqueRequiredSection(
                sharedOwnerSectionVector,
                SectionId,
                PersistentBattleCensusProvider.SchemaVersion,
                owner,
                sourceCount,
                sourceRevision))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "Battle owner count or revision changed during detached value capture.");
            return false;
        }

        snapshot = new PersistentBattleOwnerSnapshot(
            CurrentSchemaVersion,
            sourceCount,
            sourceRevision,
            copiedRecords);
        failure = PersistentBattleOwnerSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        ArmedForceStore stagedArmedForceStore,
        PersistentConflictStore stagedConflictStore,
        PersistentWarStore stagedWarStore,
        SpatialAuthorityStore stagedSpatialAuthorityStore,
        LocalTopologyStore localTopologyStore,
        out PersistentBattleStore stagedBattleStore,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        return PersistentBattleStore.TryCreateFromOwnerSnapshot(
            this,
            stagedArmedForceStore,
            stagedConflictStore,
            stagedWarStore,
            stagedSpatialAuthorityStore,
            localTopologyStore,
            out stagedBattleStore,
            out failure);
    }

    internal bool TryBuildRecords(
        ArmedForceStore armedForceStore,
        PersistentConflictStore conflictStore,
        PersistentWarStore warStore,
        SpatialAuthorityStore spatialAuthorityStore,
        LocalTopologyStore localTopologyStore,
        out IReadOnlyList<PersistentBattleRecord> records,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        records = null;
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
            "The Battle owner snapshot is invalid.");
        if (SchemaVersion != CurrentSchemaVersion)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.UnsupportedSchema,
                "The Battle owner snapshot schema is not supported.");
            return false;
        }
        if (Revision < 0L)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidRevision,
                "The Battle owner revision cannot be negative.");
            return false;
        }
        if (RecordCount < 0 || Records == null || RecordCount != Records.Count)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidCardinality,
                "The Battle owner row count must exactly match the detached record list.");
            return false;
        }
        if (armedForceStore == null || conflictStore == null || warStore == null || spatialAuthorityStore == null)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidParentComposition,
                "All staged parent authorities are required to reconstruct Battle references.");
            return false;
        }
        if (!ReferenceEquals(conflictStore.ArmedForceStore, armedForceStore)
            || !ReferenceEquals(warStore.ArmedForceStore, armedForceStore)
            || !ReferenceEquals(warStore.ConflictStore, conflictStore))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidParentComposition,
                "Staged Battle parents must be the exact ArmedForce/Conflict/War authority graph.");
            return false;
        }
        if (localTopologyStore != null)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.LocalTopologyComposed,
                "Daily-v1 staging requires LocalTopologyStore to remain NOT_COMPOSED.");
            return false;
        }

        List<PersistentBattleRecord> stagedRecords = new List<PersistentBattleRecord>(Records.Count);
        HashSet<string> battleIds = new HashSet<string>(StringComparer.Ordinal);
        string previousBattleId = null;
        foreach (PersistentBattleOwnerSnapshotRecord row in Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.BattleIdValue))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidBattleIdentity,
                    "Every Battle row requires a non-empty stable BattleId.");
                return false;
            }
            if (!battleIds.Add(row.BattleIdValue))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.DuplicateBattleIdentity,
                    "The Battle owner snapshot contains a duplicate BattleId.");
                return false;
            }
            if (previousBattleId != null
                && StringComparer.Ordinal.Compare(previousBattleId, row.BattleIdValue) >= 0)
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
                    "Battle rows must be in strict ordinal BattleId order.");
                return false;
            }
            previousBattleId = row.BattleIdValue;

            if (!TryBuildRecord(
                    row,
                    armedForceStore,
                    conflictStore,
                    warStore,
                    spatialAuthorityStore,
                    out PersistentBattleRecord stagedRecord,
                    out failure))
                return false;
            stagedRecords.Add(stagedRecord);
        }

        records = new ReadOnlyCollection<PersistentBattleRecord>(stagedRecords);
        failure = PersistentBattleOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildRecord(
        PersistentBattleOwnerSnapshotRecord row,
        ArmedForceStore armedForceStore,
        PersistentConflictStore conflictStore,
        PersistentWarStore warStore,
        SpatialAuthorityStore spatialAuthorityStore,
        out PersistentBattleRecord record,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        record = null;
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
            "A Battle snapshot row failed its local contract.");
        if (!Enum.IsDefined(typeof(BattleLifecycleState), row.LifecycleState)
            || row.CreatedAbsoluteDay < 0L
            || (row.StartedAbsoluteDay.HasValue
                && (row.StartedAbsoluteDay.Value < 0L || row.StartedAbsoluteDay.Value < row.CreatedAbsoluteDay)))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidLifecycle,
                "The Battle lifecycle or day values are invalid.");
            return false;
        }
        if ((row.LifecycleState == BattleLifecycleState.Pending && row.StartedAbsoluteDay.HasValue)
            || ((row.LifecycleState == BattleLifecycleState.Active || row.LifecycleState == BattleLifecycleState.Resolved)
                && !row.StartedAbsoluteDay.HasValue)
            || ((row.LifecycleState == BattleLifecycleState.Resolved) != (row.TerminalOutcome != null)))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidLifecycle,
                "Pending, Active, and Resolved Battle rows require their exact existing lifecycle shape.");
            return false;
        }

        ConflictId conflictId = null;
        if (row.ConflictIdValue != null)
        {
            if (string.IsNullOrWhiteSpace(row.ConflictIdValue)) return InvalidParent("ConflictId", out failure);
            conflictId = new ConflictId(row.ConflictIdValue);
            if (!conflictStore.TryGet(conflictId, out _))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.MissingConflict,
                    "A Battle ConflictId does not resolve against staged PersistentConflictStore.");
                return false;
            }
        }

        WarId warId = null;
        if (row.WarIdValue != null)
        {
            if (string.IsNullOrWhiteSpace(row.WarIdValue)) return InvalidParent("WarId", out failure);
            warId = new WarId(row.WarIdValue);
            if (!warStore.TryGet(warId, out PersistentWarRecord stagedWar))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.MissingWar,
                    "A Battle WarId does not resolve against staged PersistentWarStore.");
                return false;
            }
            if (conflictId != null && stagedWar.ConflictId != null && stagedWar.ConflictId != conflictId)
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.ContradictoryParents,
                    "The Battle ConflictId contradicts the staged War's ConflictId.");
                return false;
            }
        }

        if (!TryBuildLocation(row.LocationReference, spatialAuthorityStore, out SpatialReference location, out failure))
            return false;
        if (row.Sides == null || row.Sides.Count < 2)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidSide,
                "Each Battle retains at least two state sides.");
            return false;
        }

        List<BattleStateSide> sides = new List<BattleStateSide>(row.Sides.Count);
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        string previousSideId = null;
        foreach (PersistentBattleOwnerSideSnapshot side in row.Sides)
        {
            if (side == null || string.IsNullOrWhiteSpace(side.SideIdValue)
                || side.BattleIdValue != row.BattleIdValue
                || side.DisplayName == null)
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidSide,
                    "A Battle side requires an identity, matching parent, and exact display value.");
                return false;
            }
            if (!sideIds.Add(side.SideIdValue))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.DuplicateSide,
                    "A Battle snapshot contains a duplicate side identity.");
                return false;
            }
            if (previousSideId != null && StringComparer.Ordinal.Compare(previousSideId, side.SideIdValue) >= 0)
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
                    "Battle sides must be in strict ordinal SideId order.");
                return false;
            }
            previousSideId = side.SideIdValue;
            sides.Add(new BattleStateSide(
                new BattleId(row.BattleIdValue),
                new BattleSideId(side.SideIdValue),
                side.DisplayName));
        }

        if (row.ParticipantBindings == null)
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidBinding,
                "The Battle binding list cannot be null.");
            return false;
        }
        List<BattleParticipantBinding> bindings = new List<BattleParticipantBinding>(row.ParticipantBindings.Count);
        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        string previousBindingId = null;
        foreach (PersistentBattleOwnerBindingSnapshot binding in row.ParticipantBindings)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.BindingIdValue)
                || string.IsNullOrWhiteSpace(binding.ArmedForceIdValue)
                || binding.BattleIdValue != row.BattleIdValue
                || string.IsNullOrWhiteSpace(binding.SideIdValue)
                || !sideIds.Contains(binding.SideIdValue))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidBinding,
                    "A Battle binding requires valid stable identities and a side in its containing Battle.");
                return false;
            }
            if (!bindingIds.Add(binding.BindingIdValue))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.DuplicateBinding,
                    "A Battle snapshot contains a duplicate participant-binding identity.");
                return false;
            }
            if (previousBindingId != null
                && StringComparer.Ordinal.Compare(previousBindingId, binding.BindingIdValue) >= 0)
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
                    "Battle participant bindings must be in strict ordinal BindingId order.");
                return false;
            }
            previousBindingId = binding.BindingIdValue;
            ArmedForceId forceId = new ArmedForceId(binding.ArmedForceIdValue);
            if (!armedForceStore.TryGet(forceId, out _))
            {
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.MissingForce,
                    "A Battle binding ArmedForceId does not resolve against staged ArmedForceStore.");
                return false;
            }
            bindings.Add(new BattleParticipantBinding(
                new BattleParticipantBindingId(binding.BindingIdValue),
                new BattleId(row.BattleIdValue),
                new BattleSideId(binding.SideIdValue),
                forceId));
        }

        PersistentBattleTerminalOutcome outcome = null;
        if (row.TerminalOutcome != null)
        {
            if (!TryBuildOutcome(row.TerminalOutcome, row.BattleIdValue, row.StartedAbsoluteDay,
                    sideIds, out outcome, out failure))
                return false;
        }

        record = new PersistentBattleRecord(
            new BattleId(row.BattleIdValue),
            row.CreatedAbsoluteDay,
            conflictId,
            warId,
            row.LifecycleState,
            row.StartedAbsoluteDay,
            sides,
            bindings,
            location,
            outcome);
        failure = PersistentBattleOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildOutcome(
        PersistentBattleOwnerOutcomeSnapshot row,
        string battleIdValue,
        long? startedAbsoluteDay,
        HashSet<string> sideIds,
        out PersistentBattleTerminalOutcome outcome,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        outcome = null;
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidTerminalOutcome,
            "The retained terminal Battle outcome is invalid.");
        if (row.BattleIdValue != battleIdValue
            || !Enum.IsDefined(typeof(BattleOutcomeType), row.OutcomeType)
            || row.ResolvedAbsoluteDay < 0L
            || !startedAbsoluteDay.HasValue
            || row.ResolvedAbsoluteDay < startedAbsoluteDay.Value
            || (row.OutcomeType == BattleOutcomeType.Victory
                && (string.IsNullOrWhiteSpace(row.WinningBattleSideIdValue)
                    || !sideIds.Contains(row.WinningBattleSideIdValue)))
            || (row.OutcomeType == BattleOutcomeType.Draw && row.WinningBattleSideIdValue != null))
            return false;

        PersistentBattleOwnerProvenanceSnapshot provenanceRow = row.Provenance;
        BattleResolutionProvenance d5;
        PersistentBattleOutcomeProvenance provenance;
        if (provenanceRow == null
            || !HasValue(provenanceRow.PolicyFingerprint)
            || !HasValue(provenanceRow.NumericExecutionProfileKey)
            || !HasValue(provenanceRow.ProjectionVersion)
            || !HasValue(provenanceRow.CausalResolutionFingerprint)
            || !HasValue(provenanceRow.SourceContextFingerprint)
            || !HasValue(provenanceRow.CapabilityRuleKey)
            || !HasValue(provenanceRow.RandomAuthorityRuleKey)
            || !HasValue(provenanceRow.ResolverSettingsIdentity)
            || !HasValue(provenanceRow.D6B2PolicyFingerprint)
            || !HasValue(provenanceRow.D6B2PlanSchemaVersion)
            || !HasValue(provenanceRow.D6B2CoverageVersion)
            || !HasValue(provenanceRow.D6B2PlanFingerprint))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidProvenance,
                "A terminal Battle requires complete accepted D5 and D6B2 provenance.");
            return false;
        }

        d5 = new BattleResolutionProvenance(
            provenanceRow.PolicyFingerprint,
            provenanceRow.NumericExecutionProfileKey,
            provenanceRow.ProjectionVersion,
            provenanceRow.CausalResolutionFingerprint,
            provenanceRow.SourceContextFingerprint,
            provenanceRow.CapabilityRuleKey,
            provenanceRow.RandomAuthorityRuleKey,
            provenanceRow.ResolverSettingsIdentity);
        provenance = new PersistentBattleOutcomeProvenance(
            d5,
            provenanceRow.D6B2PolicyFingerprint,
            provenanceRow.D6B2PlanSchemaVersion,
            provenanceRow.D6B2CoverageVersion,
            provenanceRow.D6B2PlanFingerprint);
        outcome = new PersistentBattleTerminalOutcome(
            new BattleId(row.BattleIdValue),
            row.OutcomeType,
            row.WinningBattleSideIdValue == null ? null : new BattleSideId(row.WinningBattleSideIdValue),
            row.ResolvedAbsoluteDay,
            provenance);
        failure = PersistentBattleOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildLocation(
        PersistentBattleOwnerSpatialReferenceSnapshot row,
        SpatialAuthorityStore spatialAuthorityStore,
        out SpatialReference location,
        out PersistentBattleOwnerSnapshotFailure failure)
    {
        location = null;
        failure = PersistentBattleOwnerSnapshotFailure.None;
        if (row == null) return true;

        switch (row.Kind)
        {
            case SpatialReferenceKind.Hex:
                if (!HasValue(row.HexIdValue) || row.LocationIdValue != null || row.CrossingIdValue != null
                    || row.TopologyOwnerKind.HasValue || row.TopologyOwnerRuntimeId != null || row.SubLocationRuntimeId != null)
                    return InvalidSpatial(out failure);
                location = SpatialReference.ForHex(new HexId(row.HexIdValue));
                break;
            case SpatialReferenceKind.Location:
                if (!HasValue(row.LocationIdValue) || row.HexIdValue != null || row.CrossingIdValue != null
                    || row.TopologyOwnerKind.HasValue || row.TopologyOwnerRuntimeId != null || row.SubLocationRuntimeId != null)
                    return InvalidSpatial(out failure);
                location = SpatialReference.ForLocation(new LocationId(row.LocationIdValue));
                break;
            case SpatialReferenceKind.Crossing:
                if (!HasValue(row.CrossingIdValue) || row.HexIdValue != null || row.LocationIdValue != null
                    || row.TopologyOwnerKind.HasValue || row.TopologyOwnerRuntimeId != null || row.SubLocationRuntimeId != null)
                    return InvalidSpatial(out failure);
                location = SpatialReference.ForCrossing(new CrossingId(row.CrossingIdValue));
                break;
            case SpatialReferenceKind.SubLocation:
                if (!row.TopologyOwnerKind.HasValue
                    || !Enum.IsDefined(typeof(LocalTopologyOwnerKind), row.TopologyOwnerKind.Value)
                    || !HasValue(row.TopologyOwnerRuntimeId)
                    || !HasValue(row.SubLocationRuntimeId)
                    || row.HexIdValue != null || row.LocationIdValue != null || row.CrossingIdValue != null)
                    return InvalidSpatial(out failure);
                failure = PersistentBattleOwnerSnapshotFailure.Create(
                    PersistentBattleOwnerSnapshotFailureCode.UnsupportedSubLocation,
                    "Daily-v1 cannot stage a SubLocation Battle reference while LocalTopology is NOT_COMPOSED.");
                return false;
            default:
                return InvalidSpatial(out failure);
        }

        if (!spatialAuthorityStore.TryResolve(location, null, out _, out _))
        {
            failure = PersistentBattleOwnerSnapshotFailure.Create(
                PersistentBattleOwnerSnapshotFailureCode.InvalidSpatialReference,
                "A Battle SpatialReference does not resolve against the staged P8/P12 spatial authority.");
            return false;
        }
        return true;
    }

    private static bool TryCopyRecord(
        PersistentBattleRecord source,
        out PersistentBattleOwnerSnapshotRecord copied)
    {
        copied = null;
        if (source == null || source.Id == null || source.Sides == null || source.ParticipantBindings == null)
            return false;

        List<PersistentBattleOwnerSideSnapshot> sides = new List<PersistentBattleOwnerSideSnapshot>(source.Sides.Count);
        foreach (BattleStateSide side in source.Sides)
        {
            if (side == null || side.BattleId == null || side.SideId == null || side.DisplayName == null)
                return false;
            sides.Add(new PersistentBattleOwnerSideSnapshot(side.BattleId.Value, side.SideId.Value, side.DisplayName));
        }

        List<PersistentBattleOwnerBindingSnapshot> bindings = new List<PersistentBattleOwnerBindingSnapshot>(source.ParticipantBindings.Count);
        foreach (BattleParticipantBinding binding in source.ParticipantBindings)
        {
            if (binding == null || binding.BindingId == null || binding.BattleId == null
                || binding.SideId == null || binding.ArmedForceId == null)
                return false;
            bindings.Add(new PersistentBattleOwnerBindingSnapshot(
                binding.BindingId.Value,
                binding.BattleId.Value,
                binding.SideId.Value,
                binding.ArmedForceId.Value));
        }

        copied = new PersistentBattleOwnerSnapshotRecord(
            source.Id.Value,
            source.CreatedAbsoluteDay,
            source.StartedAbsoluteDay,
            source.LifecycleState,
            source.ConflictId?.Value,
            source.WarId?.Value,
            CopyLocation(source.LocationReference),
            sides,
            bindings,
            CopyOutcome(source.TerminalOutcome));
        return true;
    }

    private static PersistentBattleOwnerSpatialReferenceSnapshot CopyLocation(SpatialReference source)
    {
        if (source == null) return null;
        return new PersistentBattleOwnerSpatialReferenceSnapshot(
            source.Kind,
            source.HexId?.Value,
            source.LocationId?.Value,
            source.CrossingId?.Value,
            source.TopologyOwnerKind,
            source.TopologyOwnerRuntimeId,
            source.SubLocationRuntimeId);
    }

    private static PersistentBattleOwnerOutcomeSnapshot CopyOutcome(PersistentBattleTerminalOutcome source)
    {
        if (source == null) return null;
        PersistentBattleOutcomeProvenance sourceProvenance = source.Provenance;
        BattleResolutionProvenance d5 = sourceProvenance?.D5Resolution;
        PersistentBattleOwnerProvenanceSnapshot provenance = d5 == null || sourceProvenance == null
            ? null
            : new PersistentBattleOwnerProvenanceSnapshot(
                d5.PolicyFingerprint,
                d5.NumericExecutionProfileKey,
                d5.ProjectionVersion,
                d5.CausalResolutionFingerprint,
                d5.SourceContextFingerprint,
                d5.CapabilityRuleKey,
                d5.RandomAuthorityRuleKey,
                d5.ResolverSettingsIdentity,
                sourceProvenance.D6B2PolicyFingerprint,
                sourceProvenance.D6B2PlanSchemaVersion,
                sourceProvenance.D6B2CoverageVersion,
                sourceProvenance.D6B2PlanFingerprint);
        return new PersistentBattleOwnerOutcomeSnapshot(
            source.BattleId?.Value,
            source.OutcomeType,
            source.WinningBattleSideId?.Value,
            source.ResolvedAbsoluteDay,
            provenance);
    }

    private static bool MatchesUniqueRequiredSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision)
    {
        int count = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            count++;
            if (section.SchemaVersion != schemaVersion
                || section.Role != OwnerSectionRole.Required
                || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != cardinality
                || section.Revision != revision)
                return false;
        }
        return count == 1;
    }

    private static bool InvalidParent(string parentName, out PersistentBattleOwnerSnapshotFailure failure)
    {
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidSnapshot,
            "A nullable " + parentName + " cannot be present with an empty identity.");
        return false;
    }

    private static bool InvalidSpatial(out PersistentBattleOwnerSnapshotFailure failure)
    {
        failure = PersistentBattleOwnerSnapshotFailure.Create(
            PersistentBattleOwnerSnapshotFailureCode.InvalidSpatialReference,
            "A tagged Battle SpatialReference contains missing or contradictory values.");
        return false;
    }

    private static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);
}

internal sealed class PersistentBattleOwnerSnapshotRecord
{
    internal string BattleIdValue { get; }
    internal long CreatedAbsoluteDay { get; }
    internal long? StartedAbsoluteDay { get; }
    internal BattleLifecycleState LifecycleState { get; }
    internal string ConflictIdValue { get; }
    internal string WarIdValue { get; }
    internal PersistentBattleOwnerSpatialReferenceSnapshot LocationReference { get; }
    internal IReadOnlyList<PersistentBattleOwnerSideSnapshot> Sides { get; }
    internal IReadOnlyList<PersistentBattleOwnerBindingSnapshot> ParticipantBindings { get; }
    internal PersistentBattleOwnerOutcomeSnapshot TerminalOutcome { get; }

    internal PersistentBattleOwnerSnapshotRecord(
        string battleIdValue,
        long createdAbsoluteDay,
        long? startedAbsoluteDay,
        BattleLifecycleState lifecycleState,
        string conflictIdValue,
        string warIdValue,
        PersistentBattleOwnerSpatialReferenceSnapshot locationReference,
        IEnumerable<PersistentBattleOwnerSideSnapshot> sides,
        IEnumerable<PersistentBattleOwnerBindingSnapshot> participantBindings,
        PersistentBattleOwnerOutcomeSnapshot terminalOutcome)
    {
        BattleIdValue = battleIdValue;
        CreatedAbsoluteDay = createdAbsoluteDay;
        StartedAbsoluteDay = startedAbsoluteDay;
        LifecycleState = lifecycleState;
        ConflictIdValue = conflictIdValue;
        WarIdValue = warIdValue;
        LocationReference = locationReference?.Copy();
        Sides = CopyRows(sides, row => row?.Copy());
        ParticipantBindings = CopyRows(participantBindings, row => row?.Copy());
        TerminalOutcome = terminalOutcome?.Copy();
    }

    internal PersistentBattleOwnerSnapshotRecord Copy() => new PersistentBattleOwnerSnapshotRecord(
        BattleIdValue, CreatedAbsoluteDay, StartedAbsoluteDay, LifecycleState,
        ConflictIdValue, WarIdValue, LocationReference, Sides, ParticipantBindings, TerminalOutcome);

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null) return null;
        List<T> values = new List<T>();
        foreach (T row in rows) values.Add(copy(row));
        return new ReadOnlyCollection<T>(values);
    }
}

internal sealed class PersistentBattleOwnerSideSnapshot
{
    internal string BattleIdValue { get; }
    internal string SideIdValue { get; }
    internal string DisplayName { get; }

    internal PersistentBattleOwnerSideSnapshot(string battleIdValue, string sideIdValue, string displayName)
    {
        BattleIdValue = battleIdValue;
        SideIdValue = sideIdValue;
        DisplayName = displayName;
    }

    internal PersistentBattleOwnerSideSnapshot Copy() =>
        new PersistentBattleOwnerSideSnapshot(BattleIdValue, SideIdValue, DisplayName);
}

internal sealed class PersistentBattleOwnerBindingSnapshot
{
    internal string BindingIdValue { get; }
    internal string BattleIdValue { get; }
    internal string SideIdValue { get; }
    internal string ArmedForceIdValue { get; }

    internal PersistentBattleOwnerBindingSnapshot(
        string bindingIdValue,
        string battleIdValue,
        string sideIdValue,
        string armedForceIdValue)
    {
        BindingIdValue = bindingIdValue;
        BattleIdValue = battleIdValue;
        SideIdValue = sideIdValue;
        ArmedForceIdValue = armedForceIdValue;
    }

    internal PersistentBattleOwnerBindingSnapshot Copy() =>
        new PersistentBattleOwnerBindingSnapshot(BindingIdValue, BattleIdValue, SideIdValue, ArmedForceIdValue);
}

internal sealed class PersistentBattleOwnerSpatialReferenceSnapshot
{
    internal SpatialReferenceKind Kind { get; }
    internal string HexIdValue { get; }
    internal string LocationIdValue { get; }
    internal string CrossingIdValue { get; }
    internal LocalTopologyOwnerKind? TopologyOwnerKind { get; }
    internal string TopologyOwnerRuntimeId { get; }
    internal string SubLocationRuntimeId { get; }

    internal PersistentBattleOwnerSpatialReferenceSnapshot(
        SpatialReferenceKind kind,
        string hexIdValue,
        string locationIdValue,
        string crossingIdValue,
        LocalTopologyOwnerKind? topologyOwnerKind,
        string topologyOwnerRuntimeId,
        string subLocationRuntimeId)
    {
        Kind = kind;
        HexIdValue = hexIdValue;
        LocationIdValue = locationIdValue;
        CrossingIdValue = crossingIdValue;
        TopologyOwnerKind = topologyOwnerKind;
        TopologyOwnerRuntimeId = topologyOwnerRuntimeId;
        SubLocationRuntimeId = subLocationRuntimeId;
    }

    internal PersistentBattleOwnerSpatialReferenceSnapshot Copy() => new PersistentBattleOwnerSpatialReferenceSnapshot(
        Kind, HexIdValue, LocationIdValue, CrossingIdValue,
        TopologyOwnerKind, TopologyOwnerRuntimeId, SubLocationRuntimeId);
}

internal sealed class PersistentBattleOwnerOutcomeSnapshot
{
    internal string BattleIdValue { get; }
    internal BattleOutcomeType OutcomeType { get; }
    internal string WinningBattleSideIdValue { get; }
    internal long ResolvedAbsoluteDay { get; }
    internal PersistentBattleOwnerProvenanceSnapshot Provenance { get; }

    internal PersistentBattleOwnerOutcomeSnapshot(
        string battleIdValue,
        BattleOutcomeType outcomeType,
        string winningBattleSideIdValue,
        long resolvedAbsoluteDay,
        PersistentBattleOwnerProvenanceSnapshot provenance)
    {
        BattleIdValue = battleIdValue;
        OutcomeType = outcomeType;
        WinningBattleSideIdValue = winningBattleSideIdValue;
        ResolvedAbsoluteDay = resolvedAbsoluteDay;
        Provenance = provenance?.Copy();
    }

    internal PersistentBattleOwnerOutcomeSnapshot Copy() => new PersistentBattleOwnerOutcomeSnapshot(
        BattleIdValue, OutcomeType, WinningBattleSideIdValue, ResolvedAbsoluteDay, Provenance);
}

internal sealed class PersistentBattleOwnerProvenanceSnapshot
{
    internal string PolicyFingerprint { get; }
    internal string NumericExecutionProfileKey { get; }
    internal string ProjectionVersion { get; }
    internal string CausalResolutionFingerprint { get; }
    internal string SourceContextFingerprint { get; }
    internal string CapabilityRuleKey { get; }
    internal string RandomAuthorityRuleKey { get; }
    internal string ResolverSettingsIdentity { get; }
    internal string D6B2PolicyFingerprint { get; }
    internal string D6B2PlanSchemaVersion { get; }
    internal string D6B2CoverageVersion { get; }
    internal string D6B2PlanFingerprint { get; }

    internal PersistentBattleOwnerProvenanceSnapshot(
        string policyFingerprint,
        string numericExecutionProfileKey,
        string projectionVersion,
        string causalResolutionFingerprint,
        string sourceContextFingerprint,
        string capabilityRuleKey,
        string randomAuthorityRuleKey,
        string resolverSettingsIdentity,
        string d6b2PolicyFingerprint,
        string d6b2PlanSchemaVersion,
        string d6b2CoverageVersion,
        string d6b2PlanFingerprint)
    {
        PolicyFingerprint = policyFingerprint;
        NumericExecutionProfileKey = numericExecutionProfileKey;
        ProjectionVersion = projectionVersion;
        CausalResolutionFingerprint = causalResolutionFingerprint;
        SourceContextFingerprint = sourceContextFingerprint;
        CapabilityRuleKey = capabilityRuleKey;
        RandomAuthorityRuleKey = randomAuthorityRuleKey;
        ResolverSettingsIdentity = resolverSettingsIdentity;
        D6B2PolicyFingerprint = d6b2PolicyFingerprint;
        D6B2PlanSchemaVersion = d6b2PlanSchemaVersion;
        D6B2CoverageVersion = d6b2CoverageVersion;
        D6B2PlanFingerprint = d6b2PlanFingerprint;
    }

    internal PersistentBattleOwnerProvenanceSnapshot Copy() => new PersistentBattleOwnerProvenanceSnapshot(
        PolicyFingerprint, NumericExecutionProfileKey, ProjectionVersion, CausalResolutionFingerprint,
        SourceContextFingerprint, CapabilityRuleKey, RandomAuthorityRuleKey, ResolverSettingsIdentity,
        D6B2PolicyFingerprint, D6B2PlanSchemaVersion, D6B2CoverageVersion, D6B2PlanFingerprint);
}
