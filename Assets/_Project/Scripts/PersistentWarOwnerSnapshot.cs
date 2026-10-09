using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum PersistentWarOwnerSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidWarOwner,
    InvalidParentComposition,
    InvalidOwnerSectionVector,
    InvalidSnapshot,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidWarIdentity,
    DuplicateWarIdentity,
    InvalidLifecycle,
    InvalidSide,
    DuplicateSide,
    InvalidBinding,
    DuplicateBinding,
    MissingConflict,
    MissingArmedForce,
    UnsupportedP17A,
    StageFailed
}

internal sealed class PersistentWarOwnerSnapshotFailure
{
    internal static readonly PersistentWarOwnerSnapshotFailure None =
        new PersistentWarOwnerSnapshotFailure(PersistentWarOwnerSnapshotFailureCode.None, string.Empty);

    internal PersistentWarOwnerSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private PersistentWarOwnerSnapshotFailure(PersistentWarOwnerSnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static PersistentWarOwnerSnapshotFailure Create(
        PersistentWarOwnerSnapshotFailureCode code,
        string message) => code == PersistentWarOwnerSnapshotFailureCode.None
            ? None
            : new PersistentWarOwnerSnapshotFailure(code, message);
}

/// <summary>Detached schema-v1 Daily-v1 value export for the persistent War owner.</summary>
internal sealed class PersistentWarOwnerSnapshot
{
    internal const string SectionId = PersistentWarCensusProvider.SectionId;
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<PersistentWarOwnerSnapshotRecord> Records { get; }

    internal PersistentWarOwnerSnapshot(
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<PersistentWarOwnerSnapshotRecord> records)
    {
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        Records = CopyRows(records, row => row?.Copy());
    }

    internal static bool TryCapture(
        PersistentWarStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out PersistentWarOwnerSnapshot snapshot,
        out PersistentWarOwnerSnapshotFailure failure)
    {
        snapshot = null;
        failure = PersistentWarOwnerSnapshotFailure.Create(
            PersistentWarOwnerSnapshotFailureCode.InvalidWarOwner,
            "The Daily-v1 War owner is invalid.");
        if (owner == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidCaptureContext,
                "War capture requires the exact owner-section vector carried by the P12-B token.");
            return false;
        }
        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.UnsupportedProfile,
                "War export requires a successful Daily-v1 completed-boundary token.");
            return false;
        }
        if (owner.ArmedForceStore == null || owner.ConflictStore == null
            || !ReferenceEquals(owner.ConflictStore.ArmedForceStore, owner.ArmedForceStore))
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidParentComposition,
                "The live War owner must use the installed ArmedForce and Conflict authorities.");
            return false;
        }
        if (!owner.ValidateInvariants().IsValid)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidWarOwner,
                "The live War owner or one of its existing parent relationships is invalid.");
            return false;
        }

        IReadOnlyList<PersistentWarRecord> sourceRecords = owner.Records;
        int sourceCount = owner.Count;
        long sourceRevision = owner.Revision;
        if (sourceRecords == null || sourceCount < 0 || sourceRevision < 0L
            || sourceRecords.Count != sourceCount)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidWarOwner,
                "War owner count, revision, and retained record view are inconsistent.");
            return false;
        }
        if (!MatchesUniqueRequiredSection(sharedOwnerSectionVector, owner, sourceCount, sourceRevision))
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "The P12-B vector must contain exactly one matching Required p12e.wars witness.");
            return false;
        }

        List<PersistentWarOwnerSnapshotRecord> copied = new List<PersistentWarOwnerSnapshotRecord>(sourceCount);
        string previousWarId = null;
        foreach (PersistentWarRecord source in sourceRecords)
        {
            if (source?.P17A != null)
            {
                failure = PersistentWarOwnerSnapshotFailure.Create(
                    PersistentWarOwnerSnapshotFailureCode.UnsupportedP17A,
                    "Daily-v1 cannot omit a War carrying P17-A state.");
                return false;
            }
            if (!TryCopyRecord(source, out PersistentWarOwnerSnapshotRecord row)
                || (previousWarId != null
                    && StringComparer.Ordinal.Compare(previousWarId, row.WarIdValue) >= 0))
            {
                failure = PersistentWarOwnerSnapshotFailure.Create(
                    PersistentWarOwnerSnapshotFailureCode.InvalidWarOwner,
                    "War rows could not be copied as strictly ordered detached values.");
                return false;
            }
            previousWarId = row.WarIdValue;
            copied.Add(row);
        }

        if (owner.Count != sourceCount || owner.Revision != sourceRevision
            || !owner.ValidateInvariants().IsValid
            || !MatchesUniqueRequiredSection(sharedOwnerSectionVector, owner, sourceCount, sourceRevision))
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidOwnerSectionVector,
                "War owner values or the P12-B witness changed during detached capture.");
            return false;
        }

        snapshot = new PersistentWarOwnerSnapshot(CurrentSchemaVersion, sourceCount, sourceRevision, copied);
        failure = PersistentWarOwnerSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        ArmedForceStore stagedArmedForceStore,
        PersistentConflictStore stagedConflictStore,
        out PersistentWarStore stagedWarStore,
        out PersistentWarOwnerSnapshotFailure failure) =>
        PersistentWarStore.TryCreateFromOwnerSnapshot(
            this, stagedArmedForceStore, stagedConflictStore, out stagedWarStore, out failure);

    internal bool TryBuildRecords(
        ArmedForceStore armedForceStore,
        PersistentConflictStore conflictStore,
        out IReadOnlyList<PersistentWarRecord> records,
        out PersistentWarOwnerSnapshotFailure failure)
    {
        records = null;
        failure = PersistentWarOwnerSnapshotFailure.Create(
            PersistentWarOwnerSnapshotFailureCode.InvalidSnapshot,
            "The War owner snapshot is invalid.");
        if (SchemaVersion != CurrentSchemaVersion)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.UnsupportedSchema,
                "The War owner snapshot schema is not supported.");
            return false;
        }
        if (Revision < 0L)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidRevision,
                "The War owner revision cannot be negative.");
            return false;
        }
        if (RecordCount < 0 || Records == null || Records.Count != RecordCount)
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidCardinality,
                "The War record count must exactly match the detached record list.");
            return false;
        }
        if (armedForceStore == null || conflictStore == null
            || !ReferenceEquals(conflictStore.ArmedForceStore, armedForceStore))
        {
            failure = PersistentWarOwnerSnapshotFailure.Create(
                PersistentWarOwnerSnapshotFailureCode.InvalidParentComposition,
                "The exact staged ArmedForce and Conflict parent stores are required.");
            return false;
        }

        List<PersistentWarRecord> staged = new List<PersistentWarRecord>(RecordCount);
        HashSet<string> warIds = new HashSet<string>(StringComparer.Ordinal);
        string previousWarId = null;
        foreach (PersistentWarOwnerSnapshotRecord row in Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.WarIdValue))
                return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidWarIdentity, "Every War row requires a non-empty WarId.", out failure);
            if (!warIds.Add(row.WarIdValue))
                return Fail(PersistentWarOwnerSnapshotFailureCode.DuplicateWarIdentity, "The snapshot contains a duplicate WarId.", out failure);
            if (previousWarId != null && StringComparer.Ordinal.Compare(previousWarId, row.WarIdValue) >= 0)
                return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidSnapshot, "War rows must be in strict ordinal WarId order.", out failure);
            previousWarId = row.WarIdValue;

            if (row.ConflictIdValue != null && (string.IsNullOrWhiteSpace(row.ConflictIdValue)
                || !conflictStore.TryGet(new ConflictId(row.ConflictIdValue), out _)))
                return Fail(PersistentWarOwnerSnapshotFailureCode.MissingConflict, "A supplied ConflictId does not resolve in the exact staged Conflict store.", out failure);
            if (!TryBuildRecord(row, armedForceStore, out PersistentWarRecord record, out failure))
                return false;
            staged.Add(record);
        }
        records = new ReadOnlyCollection<PersistentWarRecord>(staged);
        failure = PersistentWarOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildRecord(
        PersistentWarOwnerSnapshotRecord row,
        ArmedForceStore armedForceStore,
        out PersistentWarRecord record,
        out PersistentWarOwnerSnapshotFailure failure)
    {
        record = null;
        failure = PersistentWarOwnerSnapshotFailure.Create(
            PersistentWarOwnerSnapshotFailureCode.InvalidSnapshot,
            "A War row failed its local contract.");
        if (row.CreatedAbsoluteDay < 0L || !Enum.IsDefined(typeof(WarLifecycleState), row.LifecycleState)
            || (row.LifecycleState == WarLifecycleState.Active && row.EndedAbsoluteDay.HasValue)
            || (row.LifecycleState == WarLifecycleState.Ended
                && (!row.EndedAbsoluteDay.HasValue || row.EndedAbsoluteDay.Value < row.CreatedAbsoluteDay)))
            return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidLifecycle, "The War lifecycle or day values are invalid.", out failure);
        if (row.Sides == null || row.Sides.Count < 2 || row.ParticipantBindings == null)
            return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidCardinality, "A War requires at least two sides and a complete binding list.", out failure);

        WarId warId = new WarId(row.WarIdValue);
        List<WarStateSide> sides = new List<WarStateSide>(row.Sides.Count);
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        string previousSideId = null;
        foreach (PersistentWarOwnerSideSnapshot side in row.Sides)
        {
            if (side == null || string.IsNullOrWhiteSpace(side.SideIdValue)
                || side.WarIdValue != row.WarIdValue || side.DisplayName == null)
                return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidSide, "Each War side requires a stable identity, matching parent, and display value.", out failure);
            if (!sideIds.Add(side.SideIdValue))
                return Fail(PersistentWarOwnerSnapshotFailureCode.DuplicateSide, "A War contains a duplicate side identity.", out failure);
            if (previousSideId != null && StringComparer.Ordinal.Compare(previousSideId, side.SideIdValue) >= 0)
                return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidSnapshot, "War sides must be in strict ordinal SideId order.", out failure);
            previousSideId = side.SideIdValue;
            sides.Add(new WarStateSide(warId, new WarSideId(side.SideIdValue), side.DisplayName));
        }

        List<WarParticipantBinding> bindings = new List<WarParticipantBinding>(row.ParticipantBindings.Count);
        HashSet<string> bindingIds = new HashSet<string>(StringComparer.Ordinal);
        string previousBindingId = null;
        foreach (PersistentWarOwnerBindingSnapshot binding in row.ParticipantBindings)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.BindingIdValue)
                || binding.WarIdValue != row.WarIdValue || string.IsNullOrWhiteSpace(binding.SideIdValue)
                || string.IsNullOrWhiteSpace(binding.ArmedForceIdValue) || !sideIds.Contains(binding.SideIdValue))
                return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidBinding, "A War binding requires matching parent, side, binding, and ArmedForce identities.", out failure);
            if (!bindingIds.Add(binding.BindingIdValue))
                return Fail(PersistentWarOwnerSnapshotFailureCode.DuplicateBinding, "A War contains a duplicate participant binding identity.", out failure);
            if (previousBindingId != null && StringComparer.Ordinal.Compare(previousBindingId, binding.BindingIdValue) >= 0)
                return Fail(PersistentWarOwnerSnapshotFailureCode.InvalidSnapshot, "War bindings must be in strict ordinal BindingId order.", out failure);
            previousBindingId = binding.BindingIdValue;
            ArmedForceId forceId = new ArmedForceId(binding.ArmedForceIdValue);
            if (!armedForceStore.TryGet(forceId, out _))
                return Fail(PersistentWarOwnerSnapshotFailureCode.MissingArmedForce, "A binding ArmedForceId does not resolve in the exact staged ArmedForce store.", out failure);
            bindings.Add(new WarParticipantBinding(new WarParticipantBindingId(binding.BindingIdValue), warId,
                new WarSideId(binding.SideIdValue), forceId));
        }

        ConflictId conflictId = row.ConflictIdValue == null ? null : new ConflictId(row.ConflictIdValue);
        record = new PersistentWarRecord(warId, row.CreatedAbsoluteDay, conflictId, row.LifecycleState,
            row.EndedAbsoluteDay, sides, bindings);
        failure = PersistentWarOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryCopyRecord(PersistentWarRecord source, out PersistentWarOwnerSnapshotRecord copied)
    {
        copied = null;
        if (source?.Id == null || source.Sides == null || source.ParticipantBindings == null || source.P17A != null)
            return false;
        List<PersistentWarOwnerSideSnapshot> sides = new List<PersistentWarOwnerSideSnapshot>(source.Sides.Count);
        string previousSideId = null;
        foreach (WarStateSide side in source.Sides)
        {
            if (side?.WarId != source.Id || side.SideId == null || side.DisplayName == null
                || (previousSideId != null && StringComparer.Ordinal.Compare(previousSideId, side.SideId.Value) >= 0))
                return false;
            previousSideId = side.SideId.Value;
            sides.Add(new PersistentWarOwnerSideSnapshot(side.WarId.Value, side.SideId.Value, side.DisplayName));
        }
        List<PersistentWarOwnerBindingSnapshot> bindings = new List<PersistentWarOwnerBindingSnapshot>(source.ParticipantBindings.Count);
        string previousBindingId = null;
        foreach (WarParticipantBinding binding in source.ParticipantBindings)
        {
            if (binding?.WarId != source.Id || binding.BindingId == null || binding.SideId == null || binding.ArmedForceId == null
                || (previousBindingId != null && StringComparer.Ordinal.Compare(previousBindingId, binding.BindingId.Value) >= 0))
                return false;
            previousBindingId = binding.BindingId.Value;
            bindings.Add(new PersistentWarOwnerBindingSnapshot(binding.BindingId.Value, binding.WarId.Value,
                binding.SideId.Value, binding.ArmedForceId.Value));
        }
        copied = new PersistentWarOwnerSnapshotRecord(source.Id.Value, source.CreatedAbsoluteDay, source.LifecycleState,
            source.EndedAbsoluteDay, source.ConflictId?.Value, sides, bindings);
        return true;
    }

    private static bool MatchesUniqueRequiredSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        object owner,
        int count,
        long revision)
    {
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !string.Equals(section.SectionId, SectionId, StringComparison.Ordinal))
                continue;
            matches++;
            if (section.SchemaVersion != PersistentWarCensusProvider.SchemaVersion
                || section.Role != OwnerSectionRole.Required
                || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != count || section.Revision != revision)
                return false;
        }
        return matches == 1;
    }

    private static bool Fail(
        PersistentWarOwnerSnapshotFailureCode code,
        string message,
        out PersistentWarOwnerSnapshotFailure failure)
    {
        failure = PersistentWarOwnerSnapshotFailure.Create(code, message);
        return false;
    }

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in rows) copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class PersistentWarOwnerSnapshotRecord
{
    internal string WarIdValue { get; }
    internal long CreatedAbsoluteDay { get; }
    internal WarLifecycleState LifecycleState { get; }
    internal long? EndedAbsoluteDay { get; }
    internal string ConflictIdValue { get; }
    internal IReadOnlyList<PersistentWarOwnerSideSnapshot> Sides { get; }
    internal IReadOnlyList<PersistentWarOwnerBindingSnapshot> ParticipantBindings { get; }

    internal PersistentWarOwnerSnapshotRecord(
        string warIdValue, long createdAbsoluteDay, WarLifecycleState lifecycleState, long? endedAbsoluteDay,
        string conflictIdValue, IEnumerable<PersistentWarOwnerSideSnapshot> sides,
        IEnumerable<PersistentWarOwnerBindingSnapshot> participantBindings)
    {
        WarIdValue = warIdValue;
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        EndedAbsoluteDay = endedAbsoluteDay;
        ConflictIdValue = conflictIdValue;
        Sides = CopyRows(sides, row => row?.Copy());
        ParticipantBindings = CopyRows(participantBindings, row => row?.Copy());
    }

    internal PersistentWarOwnerSnapshotRecord Copy() => new PersistentWarOwnerSnapshotRecord(
        WarIdValue, CreatedAbsoluteDay, LifecycleState, EndedAbsoluteDay, ConflictIdValue, Sides, ParticipantBindings);

    private static IReadOnlyList<T> CopyRows<T>(IEnumerable<T> rows, Func<T, T> copy)
    {
        if (rows == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in rows) copied.Add(copy(row));
        return new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class PersistentWarOwnerSideSnapshot
{
    internal string WarIdValue { get; }
    internal string SideIdValue { get; }
    internal string DisplayName { get; }
    internal PersistentWarOwnerSideSnapshot(string warIdValue, string sideIdValue, string displayName)
    { WarIdValue = warIdValue; SideIdValue = sideIdValue; DisplayName = displayName; }
    internal PersistentWarOwnerSideSnapshot Copy() => new PersistentWarOwnerSideSnapshot(WarIdValue, SideIdValue, DisplayName);
}

internal sealed class PersistentWarOwnerBindingSnapshot
{
    internal string BindingIdValue { get; }
    internal string WarIdValue { get; }
    internal string SideIdValue { get; }
    internal string ArmedForceIdValue { get; }
    internal PersistentWarOwnerBindingSnapshot(string bindingIdValue, string warIdValue, string sideIdValue, string armedForceIdValue)
    { BindingIdValue = bindingIdValue; WarIdValue = warIdValue; SideIdValue = sideIdValue; ArmedForceIdValue = armedForceIdValue; }
    internal PersistentWarOwnerBindingSnapshot Copy() => new PersistentWarOwnerBindingSnapshot(
        BindingIdValue, WarIdValue, SideIdValue, ArmedForceIdValue);
}
