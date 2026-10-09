using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum PersistentConflictOwnerSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidConflictOwner,
    InvalidOwnerSectionVector,
    InvalidSnapshot,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidConflictIdentity,
    DuplicateConflictIdentity,
    InvalidLifecycle,
    InvalidSide,
    DuplicateSide,
    InvalidBinding,
    DuplicateBinding,
    MissingArmedForce,
    InvalidParentComposition,
    StageFailed
}

internal sealed class PersistentConflictOwnerSnapshotFailure
{
    internal static readonly PersistentConflictOwnerSnapshotFailure None =
        new PersistentConflictOwnerSnapshotFailure(PersistentConflictOwnerSnapshotFailureCode.None, string.Empty);

    internal PersistentConflictOwnerSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private PersistentConflictOwnerSnapshotFailure(
        PersistentConflictOwnerSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static PersistentConflictOwnerSnapshotFailure Create(
        PersistentConflictOwnerSnapshotFailureCode code,
        string message)
    {
        return code == PersistentConflictOwnerSnapshotFailureCode.None
            ? None
            : new PersistentConflictOwnerSnapshotFailure(code, message);
    }
}

/// <summary>
/// Detached schema-v1 value export for the admitted Daily-v1 Conflict owner.
/// The exact live owner identity and P12-B boundary token are checked during
/// capture, but are not retained in this value.
/// </summary>
internal sealed class PersistentConflictOwnerSnapshot
{
    internal const string SectionId = PersistentConflictCensusProvider.SectionId;
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<PersistentConflictOwnerSnapshotRecord> Records { get; }

    internal PersistentConflictOwnerSnapshot(
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<PersistentConflictOwnerSnapshotRecord> records)
    {
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        Records = CopyRows(records, row => row?.Copy());
    }

    internal static bool TryCapture(
        PersistentConflictStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out PersistentConflictOwnerSnapshot snapshot,
        out PersistentConflictOwnerSnapshotFailure failure)
    {
        snapshot = null;
        failure = PersistentConflictOwnerSnapshotFailure.Create(
            PersistentConflictOwnerSnapshotFailureCode.InvalidConflictOwner,
            "The Daily-v1 Conflict owner is invalid.");
        if (owner == null
            || token == null
            || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidCaptureContext,
                "Conflict capture requires the exact owner-section vector carried by the P12-B token.");
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.UnsupportedProfile,
                "Conflict owner export requires a successful Daily-v1 completed-boundary token.");
            return false;
        }

        if (owner.ArmedForceStore == null || !owner.ValidateInvariants().IsValid)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidConflictOwner,
                "The Conflict owner or one of its ArmedForce relationships is invalid.");
            return false;
        }

        IReadOnlyList<PersistentConflictRecord> sourceRecords = owner.Records;
        int sourceCount = owner.Count;
        long sourceRevision = owner.Revision;
        if (sourceRecords == null
            || sourceCount < 0
            || sourceRevision < 0L
            || sourceRecords.Count != sourceCount)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidConflictOwner,
                "Conflict owner count, revision, and retained record view are inconsistent.");
            return false;
        }

        if (!MatchesUniqueRequiredSection(
                sharedOwnerSectionVector,
                owner,
                sourceCount,
                sourceRevision))
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "The P12-B vector does not contain exactly one matching Required p12e.conflicts witness.");
            return false;
        }

        List<PersistentConflictOwnerSnapshotRecord> copiedRecords =
            new List<PersistentConflictOwnerSnapshotRecord>(sourceRecords.Count);
        string previousConflictId = null;
        foreach (PersistentConflictRecord source in sourceRecords)
        {
            if (!TryCopyRecord(source, out PersistentConflictOwnerSnapshotRecord copied)
                || (previousConflictId != null
                    && StringComparer.Ordinal.Compare(previousConflictId, copied.ConflictIdValue) >= 0))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidConflictOwner,
                    "Conflict rows could not be copied as strictly ordered detached values.");
                return false;
            }

            previousConflictId = copied.ConflictIdValue;
            copiedRecords.Add(copied);
        }

        if (owner.Count != sourceCount
            || owner.Revision != sourceRevision
            || !owner.ValidateInvariants().IsValid
            || !MatchesUniqueRequiredSection(
                sharedOwnerSectionVector,
                owner,
                sourceCount,
                sourceRevision))
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "Conflict owner values or their P12-B witness changed during detached capture.");
            return false;
        }

        snapshot = new PersistentConflictOwnerSnapshot(
            CurrentSchemaVersion,
            sourceCount,
            sourceRevision,
            copiedRecords);
        failure = PersistentConflictOwnerSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        ArmedForceStore stagedArmedForceStore,
        out PersistentConflictStore stagedConflictStore,
        out PersistentConflictOwnerSnapshotFailure failure)
    {
        return PersistentConflictStore.TryCreateFromOwnerSnapshot(
            this,
            stagedArmedForceStore,
            out stagedConflictStore,
            out failure);
    }

    internal bool TryBuildRecords(
        ArmedForceStore armedForceStore,
        out IReadOnlyList<PersistentConflictRecord> records,
        out PersistentConflictOwnerSnapshotFailure failure)
    {
        records = null;
        failure = PersistentConflictOwnerSnapshotFailure.Create(
            PersistentConflictOwnerSnapshotFailureCode.InvalidSnapshot,
            "The Conflict owner snapshot is invalid.");
        if (SchemaVersion != CurrentSchemaVersion)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.UnsupportedSchema,
                "The Conflict owner snapshot schema is not supported.");
            return false;
        }
        if (Revision < 0L)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidRevision,
                "The Conflict owner revision cannot be negative.");
            return false;
        }
        if (RecordCount < 0 || Records == null || RecordCount != Records.Count)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidCardinality,
                "The Conflict row count must exactly match the detached record list.");
            return false;
        }
        if (armedForceStore == null)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidParentComposition,
                "The staged ArmedForceStore is required to reconstruct Conflict bindings.");
            return false;
        }

        List<PersistentConflictRecord> stagedRecords = new List<PersistentConflictRecord>(Records.Count);
        HashSet<string> conflictIds = new HashSet<string>(StringComparer.Ordinal);
        string previousConflictId = null;
        foreach (PersistentConflictOwnerSnapshotRecord row in Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ConflictIdValue))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidConflictIdentity,
                    "Every Conflict row requires a non-empty stable ConflictId.");
                return false;
            }
            if (!conflictIds.Add(row.ConflictIdValue))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.DuplicateConflictIdentity,
                    "The Conflict owner snapshot contains a duplicate ConflictId.");
                return false;
            }
            if (previousConflictId != null
                && StringComparer.Ordinal.Compare(previousConflictId, row.ConflictIdValue) >= 0)
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidSnapshot,
                    "Conflict rows must be in strict ordinal ConflictId order.");
                return false;
            }
            previousConflictId = row.ConflictIdValue;

            if (!TryBuildRecord(row, armedForceStore, out PersistentConflictRecord stagedRecord, out failure))
                return false;
            stagedRecords.Add(stagedRecord);
        }

        records = new ReadOnlyCollection<PersistentConflictRecord>(stagedRecords);
        failure = PersistentConflictOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildRecord(
        PersistentConflictOwnerSnapshotRecord row,
        ArmedForceStore armedForceStore,
        out PersistentConflictRecord record,
        out PersistentConflictOwnerSnapshotFailure failure)
    {
        record = null;
        failure = PersistentConflictOwnerSnapshotFailure.Create(
            PersistentConflictOwnerSnapshotFailureCode.InvalidSnapshot,
            "A Conflict snapshot row failed its local contract.");
        if (row.CreatedAbsoluteDay < 0L
            || !Enum.IsDefined(typeof(ConflictLifecycleState), row.LifecycleState)
            || (row.LifecycleState == ConflictLifecycleState.Active && row.EndedAbsoluteDay.HasValue)
            || (row.LifecycleState == ConflictLifecycleState.Ended
                && (!row.EndedAbsoluteDay.HasValue || row.EndedAbsoluteDay.Value < row.CreatedAbsoluteDay)))
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidLifecycle,
                "The Conflict lifecycle or day values are invalid.");
            return false;
        }
        if (row.Sides == null || row.Sides.Count < 2)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidSide,
                "Each Conflict row requires at least two retained sides.");
            return false;
        }
        if (row.ParticipantBindings == null)
        {
            failure = PersistentConflictOwnerSnapshotFailure.Create(
                PersistentConflictOwnerSnapshotFailureCode.InvalidBinding,
                "A Conflict participant binding list is required, including when empty.");
            return false;
        }

        ConflictId conflictId = new ConflictId(row.ConflictIdValue);
        List<ConflictStateSide> sides = new List<ConflictStateSide>(row.Sides.Count);
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        string previousSideId = null;
        foreach (PersistentConflictOwnerSideSnapshot side in row.Sides)
        {
            if (side == null
                || string.IsNullOrWhiteSpace(side.SideIdValue)
                || side.ConflictIdValue != row.ConflictIdValue
                || side.DisplayName == null)
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidSide,
                    "Each Conflict side requires a stable identity, matching parent, and exact display value.");
                return false;
            }
            if (!sideIds.Add(side.SideIdValue))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.DuplicateSide,
                    "A Conflict snapshot contains a duplicate side identity.");
                return false;
            }
            if (previousSideId != null
                && StringComparer.Ordinal.Compare(previousSideId, side.SideIdValue) >= 0)
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidSnapshot,
                    "Conflict sides must be in strict ordinal SideId order.");
                return false;
            }
            previousSideId = side.SideIdValue;
            sides.Add(new ConflictStateSide(
                conflictId,
                new ConflictSideId(side.SideIdValue),
                side.DisplayName));
        }

        List<ConflictParticipantBinding> bindings =
            new List<ConflictParticipantBinding>(row.ParticipantBindings.Count);
        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        string previousBindingId = null;
        foreach (PersistentConflictOwnerBindingSnapshot binding in row.ParticipantBindings)
        {
            if (binding == null
                || string.IsNullOrWhiteSpace(binding.BindingIdValue)
                || string.IsNullOrWhiteSpace(binding.ConflictIdValue)
                || string.IsNullOrWhiteSpace(binding.SideIdValue)
                || string.IsNullOrWhiteSpace(binding.ArmedForceIdValue)
                || binding.ConflictIdValue != row.ConflictIdValue
                || !sideIds.Contains(binding.SideIdValue))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidBinding,
                    "A Conflict participant requires matching parent, side, binding, and ArmedForce identities.");
                return false;
            }
            if (!bindingIds.Add(binding.BindingIdValue))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.DuplicateBinding,
                    "A Conflict snapshot contains a duplicate participant binding identity.");
                return false;
            }
            if (previousBindingId != null
                && StringComparer.Ordinal.Compare(previousBindingId, binding.BindingIdValue) >= 0)
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.InvalidSnapshot,
                    "Conflict participant bindings must be in strict ordinal BindingId order.");
                return false;
            }
            previousBindingId = binding.BindingIdValue;

            ArmedForceId forceId = new ArmedForceId(binding.ArmedForceIdValue);
            if (!armedForceStore.TryGet(forceId, out _))
            {
                failure = PersistentConflictOwnerSnapshotFailure.Create(
                    PersistentConflictOwnerSnapshotFailureCode.MissingArmedForce,
                    "A Conflict participant ArmedForceId does not resolve in the staged ArmedForceStore.");
                return false;
            }
            bindings.Add(new ConflictParticipantBinding(
                new ConflictParticipantBindingId(binding.BindingIdValue),
                conflictId,
                new ConflictSideId(binding.SideIdValue),
                forceId));
        }

        record = new PersistentConflictRecord(
            conflictId,
            row.CreatedAbsoluteDay,
            row.LifecycleState,
            row.EndedAbsoluteDay,
            sides,
            bindings);
        failure = PersistentConflictOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryCopyRecord(
        PersistentConflictRecord source,
        out PersistentConflictOwnerSnapshotRecord copied)
    {
        copied = null;
        if (source == null
            || source.Id == null
            || source.Sides == null
            || source.ParticipantBindings == null)
            return false;

        List<PersistentConflictOwnerSideSnapshot> sides =
            new List<PersistentConflictOwnerSideSnapshot>(source.Sides.Count);
        string previousSideId = null;
        foreach (ConflictStateSide side in source.Sides)
        {
            if (side == null || side.ConflictId == null || side.SideId == null
                || side.ConflictId != source.Id || side.DisplayName == null
                || (previousSideId != null
                    && StringComparer.Ordinal.Compare(previousSideId, side.SideId.Value) >= 0))
                return false;
            previousSideId = side.SideId.Value;
            sides.Add(new PersistentConflictOwnerSideSnapshot(
                side.ConflictId.Value,
                side.SideId.Value,
                side.DisplayName));
        }

        List<PersistentConflictOwnerBindingSnapshot> bindings =
            new List<PersistentConflictOwnerBindingSnapshot>(source.ParticipantBindings.Count);
        string previousBindingId = null;
        foreach (ConflictParticipantBinding binding in source.ParticipantBindings)
        {
            if (binding == null || binding.BindingId == null || binding.ConflictId == null
                || binding.SideId == null || binding.ArmedForceId == null
                || binding.ConflictId != source.Id
                || (previousBindingId != null
                    && StringComparer.Ordinal.Compare(previousBindingId, binding.BindingId.Value) >= 0))
                return false;
            previousBindingId = binding.BindingId.Value;
            bindings.Add(new PersistentConflictOwnerBindingSnapshot(
                binding.BindingId.Value,
                binding.ConflictId.Value,
                binding.SideId.Value,
                binding.ArmedForceId.Value));
        }

        copied = new PersistentConflictOwnerSnapshotRecord(
            source.Id.Value,
            source.CreatedAbsoluteDay,
            source.LifecycleState,
            source.EndedAbsoluteDay,
            sides,
            bindings);
        return true;
    }

    private static bool MatchesUniqueRequiredSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        object owner,
        int cardinality,
        long revision)
    {
        int count = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !string.Equals(section.SectionId, SectionId, StringComparison.Ordinal))
                continue;
            count++;
            if (section.SchemaVersion != PersistentConflictCensusProvider.SchemaVersion
                || section.Role != OwnerSectionRole.Required
                || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != cardinality
                || section.Revision != revision)
                return false;
        }
        return count == 1;
    }

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null)
            return null;
        List<T> copied = new List<T>();
        foreach (T row in rows)
            copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class PersistentConflictOwnerSnapshotRecord
{
    internal string ConflictIdValue { get; }
    internal long CreatedAbsoluteDay { get; }
    internal ConflictLifecycleState LifecycleState { get; }
    internal long? EndedAbsoluteDay { get; }
    internal IReadOnlyList<PersistentConflictOwnerSideSnapshot> Sides { get; }
    internal IReadOnlyList<PersistentConflictOwnerBindingSnapshot> ParticipantBindings { get; }

    internal PersistentConflictOwnerSnapshotRecord(
        string conflictIdValue,
        long createdAbsoluteDay,
        ConflictLifecycleState lifecycleState,
        long? endedAbsoluteDay,
        IEnumerable<PersistentConflictOwnerSideSnapshot> sides,
        IEnumerable<PersistentConflictOwnerBindingSnapshot> participantBindings)
    {
        ConflictIdValue = conflictIdValue;
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        EndedAbsoluteDay = endedAbsoluteDay;
        Sides = CopyRows(sides, row => row?.Copy());
        ParticipantBindings = CopyRows(participantBindings, row => row?.Copy());
    }

    internal PersistentConflictOwnerSnapshotRecord Copy() => new PersistentConflictOwnerSnapshotRecord(
        ConflictIdValue,
        CreatedAbsoluteDay,
        LifecycleState,
        EndedAbsoluteDay,
        Sides,
        ParticipantBindings);

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null)
            return null;
        List<T> copied = new List<T>();
        foreach (T row in rows)
            copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class PersistentConflictOwnerSideSnapshot
{
    internal string ConflictIdValue { get; }
    internal string SideIdValue { get; }
    internal string DisplayName { get; }

    internal PersistentConflictOwnerSideSnapshot(string conflictIdValue, string sideIdValue, string displayName)
    {
        ConflictIdValue = conflictIdValue;
        SideIdValue = sideIdValue;
        DisplayName = displayName;
    }

    internal PersistentConflictOwnerSideSnapshot Copy() =>
        new PersistentConflictOwnerSideSnapshot(ConflictIdValue, SideIdValue, DisplayName);
}

internal sealed class PersistentConflictOwnerBindingSnapshot
{
    internal string BindingIdValue { get; }
    internal string ConflictIdValue { get; }
    internal string SideIdValue { get; }
    internal string ArmedForceIdValue { get; }

    internal PersistentConflictOwnerBindingSnapshot(
        string bindingIdValue,
        string conflictIdValue,
        string sideIdValue,
        string armedForceIdValue)
    {
        BindingIdValue = bindingIdValue;
        ConflictIdValue = conflictIdValue;
        SideIdValue = sideIdValue;
        ArmedForceIdValue = armedForceIdValue;
    }

    internal PersistentConflictOwnerBindingSnapshot Copy() => new PersistentConflictOwnerBindingSnapshot(
        BindingIdValue,
        ConflictIdValue,
        SideIdValue,
        ArmedForceIdValue);
}
