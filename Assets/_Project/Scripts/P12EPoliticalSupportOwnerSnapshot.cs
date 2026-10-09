using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EPoliticalSupportSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerSectionVector,
    InvalidOwnerValues,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    DuplicateIdentity,
    InvalidReference,
    InvalidRelation,
    InvalidTimeline,
    InvalidOrdering,
    StageFailed
}

internal sealed class P12EPoliticalSupportSnapshotFailure
{
    internal static readonly P12EPoliticalSupportSnapshotFailure None =
        new P12EPoliticalSupportSnapshotFailure(P12EPoliticalSupportSnapshotFailureCode.None, string.Empty);

    internal P12EPoliticalSupportSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12EPoliticalSupportSnapshotFailure(
        P12EPoliticalSupportSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12EPoliticalSupportSnapshotFailure Create(
        P12EPoliticalSupportSnapshotFailureCode code,
        string message)
    {
        return code == P12EPoliticalSupportSnapshotFailureCode.None
            ? None
            : new P12EPoliticalSupportSnapshotFailure(code, message);
    }
}

internal sealed class P12EPoliticalSupportSnapshotRow
{
    internal string RelationId { get; }
    internal int SourceKind { get; }
    internal string SourceId { get; }
    internal int TargetKind { get; }
    internal string TargetId { get; }
    internal int Disposition { get; }
    internal long StartedAbsoluteDay { get; }
    internal long? EndedAbsoluteDay { get; }

    internal P12EPoliticalSupportSnapshotRow(
        string relationId,
        int sourceKind,
        string sourceId,
        int targetKind,
        string targetId,
        int disposition,
        long startedAbsoluteDay,
        long? endedAbsoluteDay)
    {
        RelationId = relationId;
        SourceKind = sourceKind;
        SourceId = sourceId;
        TargetKind = targetKind;
        TargetId = targetId;
        Disposition = disposition;
        StartedAbsoluteDay = startedAbsoluteDay;
        EndedAbsoluteDay = endedAbsoluteDay;
    }

    internal P12EPoliticalSupportSnapshotRow Copy()
    {
        return new P12EPoliticalSupportSnapshotRow(RelationId, SourceKind, SourceId,
            TargetKind, TargetId, Disposition, StartedAbsoluteDay, EndedAbsoluteDay);
    }
}

internal sealed class P12EPoliticalSupportSnapshotSection
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12EPoliticalSupportSnapshotRow> Records { get; }

    internal P12EPoliticalSupportSnapshotSection(
        string sectionId,
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<P12EPoliticalSupportSnapshotRow> records)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        if (records == null)
        {
            Records = null;
            return;
        }

        List<P12EPoliticalSupportSnapshotRow> detached = new List<P12EPoliticalSupportSnapshotRow>();
        foreach (P12EPoliticalSupportSnapshotRow row in records)
        {
            detached.Add(row?.Copy());
        }
        Records = new ReadOnlyCollection<P12EPoliticalSupportSnapshotRow>(detached);
    }
}

/// <summary>Detached export and private staged reconstruction for the existing PoliticalSupport census section.</summary>
internal sealed class P12EPoliticalSupportOwnerSnapshot
{
    internal const int CurrentSchemaVersion = PoliticalSupportStoreCensusProvider.SchemaVersion;

    internal long CapturedAbsoluteDay { get; }
    internal P12EPoliticalSupportSnapshotSection Relations { get; }

    internal P12EPoliticalSupportOwnerSnapshot(
        long capturedAbsoluteDay,
        P12EPoliticalSupportSnapshotSection relations)
    {
        CapturedAbsoluteDay = capturedAbsoluteDay;
        Relations = relations;
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EPoliticalSupportOwnerSnapshot snapshot,
        out P12EPoliticalSupportSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalSupportSnapshotFailure.Create(
            P12EPoliticalSupportSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires an admitted completed Daily-v1 runtime.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out _)
            || token == null)
        {
            return false;
        }

        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12EPoliticalSupportOwnerSnapshot snapshot,
        out P12EPoliticalSupportSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalSupportSnapshotFailure.Create(
            P12EPoliticalSupportSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its owner-section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidCaptureContext,
                "A successful completed Daily-v1 boundary token is required.", out failure);
        }

        OwnerSectionCensusSnapshot witness = Find(sharedOwnerSectionVector,
            PoliticalSupportStoreCensusProvider.RelationsSectionId);
        PoliticalSupportStore owner = witness?.OwnerInstanceIdentity as PoliticalSupportStore;
        if (owner == null || !Matches(witness, owner, owner.Count, owner.Revision))
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidOwnerSectionVector,
                "The Required relation witness must identify the exact installed PoliticalSupportStore.", out failure);
        }

        IReadOnlyList<PoliticalSupportRelationRecord> ownerRows = owner.Records;
        int count = owner.Count;
        long revision = owner.Revision;
        if (ownerRows == null || count < 0 || revision < 0L || ownerRows.Count != count)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidOwnerValues,
                "The store row view, cardinality, or local revision is inconsistent.", out failure);
        }

        List<P12EPoliticalSupportSnapshotRow> rows = new List<P12EPoliticalSupportSnapshotRow>(count);
        foreach (PoliticalSupportRelationRecord row in ownerRows)
        {
            if (row == null || row.RelationId == null || row.Source == null || row.Target == null
                || !Enum.IsDefined(typeof(PoliticalSupportSourceKind), row.Source.Kind)
                || !Enum.IsDefined(typeof(PoliticalSupportTargetKind), row.Target.Kind)
                || !Enum.IsDefined(typeof(PoliticalSupportDisposition), row.Disposition)
                || string.IsNullOrWhiteSpace(row.RelationId.Value)
                || string.IsNullOrWhiteSpace(row.Source.Value)
                || string.IsNullOrWhiteSpace(row.Target.Value)
                || row.StartedAbsoluteDay < 0L || row.StartedAbsoluteDay > token.AbsoluteDay
                || (row.EndedAbsoluteDay.HasValue
                    && (row.EndedAbsoluteDay.Value < row.StartedAbsoluteDay
                        || row.EndedAbsoluteDay.Value > token.AbsoluteDay))
                || !owner.HasValidEndpointsForP12EOwnerSnapshot(row))
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidOwnerValues,
                    "A relation row is malformed, outside the captured day, or references an unavailable endpoint.", out failure);
            }

            rows.Add(new P12EPoliticalSupportSnapshotRow(
                row.RelationId.Value,
                (int)row.Source.Kind,
                row.Source.Value,
                (int)row.Target.Kind,
                row.Target.Value,
                (int)row.Disposition,
                row.StartedAbsoluteDay,
                row.EndedAbsoluteDay));
        }

        P12EPoliticalSupportOwnerSnapshot candidate = new P12EPoliticalSupportOwnerSnapshot(
            token.AbsoluteDay,
            new P12EPoliticalSupportSnapshotSection(
                PoliticalSupportStoreCensusProvider.RelationsSectionId,
                CurrentSchemaVersion,
                count,
                revision,
                rows));

        if (!candidate.TryValidate(out failure)
            || owner.Count != count
            || owner.Revision != revision
            || !Matches(witness, owner, count, revision)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            snapshot = null;
            if (failure.Code == P12EPoliticalSupportSnapshotFailureCode.None)
            {
                failure = P12EPoliticalSupportSnapshotFailure.Create(
                    P12EPoliticalSupportSnapshotFailureCode.InvalidOwnerSectionVector,
                    "The PoliticalSupport owner or completed-boundary token changed during capture.");
            }
            return false;
        }

        snapshot = candidate;
        failure = P12EPoliticalSupportSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        PersonStore stagedPersons,
        FactionStore stagedFactions,
        PoliticalClaimStore stagedPoliticalClaims,
        out PoliticalSupportStore staged,
        out P12EPoliticalSupportSnapshotFailure failure)
    {
        staged = null;
        if (!TryValidate(out failure)) return false;
        if (stagedPersons == null || stagedFactions == null || stagedPoliticalClaims == null)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidReference,
                "Exact staged Person, Faction, and PoliticalClaim roots are required.", out failure);
        }

        try
        {
            List<PoliticalSupportRelationRecord> records = new List<PoliticalSupportRelationRecord>(Relations.RecordCount);
            foreach (P12EPoliticalSupportSnapshotRow row in Relations.Records)
            {
                PoliticalSupportSource source = row.SourceKind == (int)PoliticalSupportSourceKind.Person
                    ? PoliticalSupportSource.ForPerson(new PersonId(row.SourceId))
                    : PoliticalSupportSource.ForFaction(new FactionId(row.SourceId));
                PoliticalSupportTarget target = row.TargetKind == (int)PoliticalSupportTargetKind.PoliticalClaim
                    ? PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId(row.TargetId))
                    : PoliticalSupportTarget.ForSuccessionCandidate(new PersonId(row.TargetId));

                bool sourceExists = source.Kind == PoliticalSupportSourceKind.Person
                    ? stagedPersons.TryGet(source.PersonId, out _)
                    : stagedFactions.TryGet(source.FactionId, out _);
                bool targetExists = target.Kind == PoliticalSupportTargetKind.PoliticalClaim
                    ? stagedPoliticalClaims.TryGet(target.PoliticalClaimId, out _)
                    : stagedPersons.TryGet(target.SuccessionCandidatePersonId, out _);
                if (!sourceExists || !targetExists)
                {
                    return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidReference,
                        "A relation references an endpoint absent from the exact staged roots.", out failure);
                }

                records.Add(new PoliticalSupportRelationRecord(
                    new PoliticalSupportRelationId(row.RelationId), source, target,
                    (PoliticalSupportDisposition)row.Disposition,
                    row.StartedAbsoluteDay, row.EndedAbsoluteDay));
            }

            if (!PoliticalSupportStore.TryCreateFromP12EOwnerSnapshot(
                    stagedPersons, stagedFactions, stagedPoliticalClaims, records,
                    Relations.Revision, out PoliticalSupportStore candidate))
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.StageFailed,
                    "The private PoliticalSupportStore factory rejected the validated rows.", out failure);
            }

            staged = candidate;
            failure = P12EPoliticalSupportSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is NullReferenceException)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.StageFailed,
                "Private staging rejected malformed PoliticalSupport values: " + exception.Message, out failure);
        }
    }

    private bool TryValidate(out P12EPoliticalSupportSnapshotFailure failure)
    {
        failure = P12EPoliticalSupportSnapshotFailure.Create(
            P12EPoliticalSupportSnapshotFailureCode.InvalidRelation,
            "PoliticalSupport owner snapshot is malformed.");
        if (CapturedAbsoluteDay < 0L)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidTimeline,
                "Captured day must be nonnegative.", out failure);
        }
        if (Relations == null
            || !string.Equals(Relations.SectionId, PoliticalSupportStoreCensusProvider.RelationsSectionId, StringComparison.Ordinal)
            || Relations.SchemaVersion != CurrentSchemaVersion)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.UnsupportedSchema,
                "The existing Required schema-v1 PoliticalSupport section is required.", out failure);
        }
        if (Relations.Revision < 0L)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidRevision,
                "The exact local revision must be nonnegative.", out failure);
        }
        if (Relations.Records == null || Relations.RecordCount < 0
            || Relations.Records.Count != Relations.RecordCount)
        {
            return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidCardinality,
                "Declared relation cardinality must equal the detached row count.", out failure);
        }

        HashSet<string> relationIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> activePairs = new HashSet<string>(StringComparer.Ordinal);
        P12EPoliticalSupportSnapshotRow previous = null;
        foreach (P12EPoliticalSupportSnapshotRow row in Relations.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.RelationId)
                || string.IsNullOrWhiteSpace(row.SourceId) || string.IsNullOrWhiteSpace(row.TargetId)
                || !Enum.IsDefined(typeof(PoliticalSupportSourceKind), row.SourceKind)
                || !Enum.IsDefined(typeof(PoliticalSupportTargetKind), row.TargetKind)
                || !Enum.IsDefined(typeof(PoliticalSupportDisposition), row.Disposition))
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidIdentity,
                    "Relation identity, typed endpoints, or disposition is invalid.", out failure);
            }
            if (!relationIds.Add(row.RelationId))
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.DuplicateIdentity,
                    "Relation IDs must be unique under ordinal comparison.", out failure);
            }
            if (row.StartedAbsoluteDay < 0L || row.StartedAbsoluteDay > CapturedAbsoluteDay
                || (row.EndedAbsoluteDay.HasValue
                    && (row.EndedAbsoluteDay.Value < row.StartedAbsoluteDay
                        || row.EndedAbsoluteDay.Value > CapturedAbsoluteDay)))
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidTimeline,
                    "Relation dates must fall within the retained captured-day boundary.", out failure);
            }

            PoliticalSupportSource source = row.SourceKind == (int)PoliticalSupportSourceKind.Person
                ? PoliticalSupportSource.ForPerson(new PersonId(row.SourceId))
                : PoliticalSupportSource.ForFaction(new FactionId(row.SourceId));
            PoliticalSupportTarget target = row.TargetKind == (int)PoliticalSupportTargetKind.PoliticalClaim
                ? PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId(row.TargetId))
                : PoliticalSupportTarget.ForSuccessionCandidate(new PersonId(row.TargetId));
            if (!row.EndedAbsoluteDay.HasValue
                && !activePairs.Add(PoliticalSupportStore.PairKeyForP12EOwnerSnapshot(source, target)))
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.DuplicateIdentity,
                    "At most one active relation may exist for a typed source-target pair.", out failure);
            }
            if (previous != null && CompareRows(previous, row) > 0)
            {
                return Fail(P12EPoliticalSupportSnapshotFailureCode.InvalidOrdering,
                    "Relation rows must preserve the store's deterministic CompareRecords order.", out failure);
            }
            previous = row;
        }

        failure = P12EPoliticalSupportSnapshotFailure.None;
        return true;
    }

    private static int CompareRows(P12EPoliticalSupportSnapshotRow left, P12EPoliticalSupportSnapshotRow right)
    {
        int result = left.SourceKind.CompareTo(right.SourceKind);
        if (result != 0) return result;
        result = StringComparer.Ordinal.Compare(left.SourceId, right.SourceId);
        if (result != 0) return result;
        result = left.TargetKind.CompareTo(right.TargetKind);
        if (result != 0) return result;
        result = StringComparer.Ordinal.Compare(left.TargetId, right.TargetId);
        if (result != 0) return result;
        result = left.Disposition.CompareTo(right.Disposition);
        if (result != 0) return result;
        result = left.StartedAbsoluteDay.CompareTo(right.StartedAbsoluteDay);
        if (result != 0) return result;
        return StringComparer.Ordinal.Compare(left.RelationId, right.RelationId);
    }

    private static OwnerSectionCensusSnapshot Find(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId)
    {
        OwnerSectionCensusSnapshot result = null;
        if (vector == null) return null;
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            if (result != null) return null;
            result = section;
        }
        return result;
    }

    private static bool Matches(
        OwnerSectionCensusSnapshot witness,
        PoliticalSupportStore owner,
        int count,
        long revision)
    {
        return witness != null
            && string.Equals(witness.SectionId, PoliticalSupportStoreCensusProvider.RelationsSectionId, StringComparison.Ordinal)
            && witness.SchemaVersion == PoliticalSupportStoreCensusProvider.SchemaVersion
            && witness.Role == OwnerSectionRole.Required
            && ReferenceEquals(witness.OwnerInstanceIdentity, owner)
            && witness.Cardinality == count
            && witness.Revision == revision;
    }

    private static bool Fail(
        P12EPoliticalSupportSnapshotFailureCode code,
        string message,
        out P12EPoliticalSupportSnapshotFailure failure)
    {
        failure = P12EPoliticalSupportSnapshotFailure.Create(code, message);
        return false;
    }
}
