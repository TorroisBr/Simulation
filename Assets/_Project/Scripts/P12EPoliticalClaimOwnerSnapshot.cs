using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EPoliticalClaimSnapshotFailureCode
{
    None = 0, InvalidCaptureContext, InvalidOwnerSectionVector, InvalidOwnerValues,
    UnsupportedSchema, InvalidRevision, InvalidCardinality, InvalidIdentity,
    DuplicateIdentity, InvalidReference, InvalidClaim, InvalidRecognition, StageFailed
}

internal sealed class P12EPoliticalClaimSnapshotFailure
{
    internal static readonly P12EPoliticalClaimSnapshotFailure None =
        new P12EPoliticalClaimSnapshotFailure(P12EPoliticalClaimSnapshotFailureCode.None, string.Empty);
    internal P12EPoliticalClaimSnapshotFailureCode Code { get; }
    internal string Message { get; }
    private P12EPoliticalClaimSnapshotFailure(P12EPoliticalClaimSnapshotFailureCode code, string message)
    { Code = code; Message = message ?? string.Empty; }
    internal static P12EPoliticalClaimSnapshotFailure Create(P12EPoliticalClaimSnapshotFailureCode code, string message) =>
        code == P12EPoliticalClaimSnapshotFailureCode.None ? None : new P12EPoliticalClaimSnapshotFailure(code, message);
}

internal sealed class P12EPoliticalClaimSnapshotSection<T>
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<T> Records { get; }
    internal P12EPoliticalClaimSnapshotSection(string id, int schema, int count, long revision,
        IEnumerable<T> rows, Func<T, T> copy)
    {
        SectionId = id; SchemaVersion = schema; RecordCount = count; Revision = revision;
        if (rows == null) { Records = null; return; }
        List<T> detached = new List<T>();
        foreach (T row in rows) detached.Add(copy == null ? row : copy(row));
        Records = new ReadOnlyCollection<T>(detached);
    }
}

internal sealed class P12EPoliticalClaimRow
{
    internal string ClaimId { get; }
    internal string ClaimantPersonId { get; }
    internal int ClaimType { get; }
    internal int TargetKind { get; }
    internal string TargetId { get; }
    internal int Basis { get; }
    internal string BasisDescription { get; }
    internal long CreatedAbsoluteDay { get; }
    internal int Status { get; }
    internal long? ResolutionAbsoluteDay { get; }
    internal IReadOnlyList<string> EvidenceReferences { get; }
    internal P12EPoliticalClaimRow(string claimId, string claimant, int claimType, int targetKind, string targetId,
        int basis, string description, long created, int status, long? resolved, IEnumerable<string> evidence)
    {
        ClaimId = claimId; ClaimantPersonId = claimant; ClaimType = claimType; TargetKind = targetKind;
        TargetId = targetId; Basis = basis; BasisDescription = description; CreatedAbsoluteDay = created;
        Status = status; ResolutionAbsoluteDay = resolved;
        EvidenceReferences = evidence == null ? null : new ReadOnlyCollection<string>(new List<string>(evidence));
    }
    internal P12EPoliticalClaimRow Copy() => new P12EPoliticalClaimRow(ClaimId, ClaimantPersonId, ClaimType,
        TargetKind, TargetId, Basis, BasisDescription, CreatedAbsoluteDay, Status, ResolutionAbsoluteDay, EvidenceReferences);
}

internal sealed class P12EPoliticalClaimRecognitionHistoryRow
{
    internal int State { get; }
    internal long Day { get; }
    internal string Reason { get; }
    internal P12EPoliticalClaimRecognitionHistoryRow(int state, long day, string reason)
    { State = state; Day = day; Reason = reason; }
    internal P12EPoliticalClaimRecognitionHistoryRow Copy() => new P12EPoliticalClaimRecognitionHistoryRow(State, Day, Reason);
}

internal sealed class P12EPoliticalClaimRecognitionRow
{
    internal string ClaimId { get; }
    internal string InstitutionId { get; }
    internal string RecognitionId { get; }
    internal int State { get; }
    internal long RecognitionAbsoluteDay { get; }
    internal string Reason { get; }
    internal IReadOnlyList<P12EPoliticalClaimRecognitionHistoryRow> History { get; }
    internal P12EPoliticalClaimRecognitionRow(string claimId, string institutionId, string recognitionId,
        int state, long day, string reason, IEnumerable<P12EPoliticalClaimRecognitionHistoryRow> history)
    {
        ClaimId = claimId; InstitutionId = institutionId; RecognitionId = recognitionId; State = state;
        RecognitionAbsoluteDay = day; Reason = reason;
        History = history == null ? null : new ReadOnlyCollection<P12EPoliticalClaimRecognitionHistoryRow>(
            new List<P12EPoliticalClaimRecognitionHistoryRow>(history));
    }
    internal P12EPoliticalClaimRecognitionRow Copy()
    {
        List<P12EPoliticalClaimRecognitionHistoryRow> history = null;
        if (History != null) { history = new List<P12EPoliticalClaimRecognitionHistoryRow>(); foreach (var row in History) history.Add(row?.Copy()); }
        return new P12EPoliticalClaimRecognitionRow(ClaimId, InstitutionId, RecognitionId, State,
            RecognitionAbsoluteDay, Reason, history);
    }
}

/// <summary>Detached owner export for the two existing schema-v1 PoliticalClaimStore sections.</summary>
internal sealed class P12EPoliticalClaimOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;
    internal P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow> Claims { get; }
    internal P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> Recognitions { get; }
    internal P12EPoliticalClaimOwnerSnapshot(
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow> claims,
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> recognitions)
    { Claims = claims; Recognitions = recognitions; }

    internal static bool TryCapture(SimulationRuntime runtime, out P12EPoliticalClaimOwnerSnapshot snapshot,
        out P12EPoliticalClaimSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalClaimSnapshotFailure.Create(P12EPoliticalClaimSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires an admitted completed Daily-v1 runtime.");
        if (runtime == null || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out _)
            || token == null) return false;
        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(SimulationRuntime runtime, DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12EPoliticalClaimOwnerSnapshot snapshot, out P12EPoliticalClaimSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalClaimSnapshotFailure.Create(P12EPoliticalClaimSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its owner-section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _)) return false;
        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L)
            return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidCaptureContext,
                "A successful completed Daily-v1 boundary token is required.", out failure);
        OwnerSectionCensusSnapshot claimWitness = Find(sharedOwnerSectionVector, PoliticalClaimStoreCensusProvider.ClaimsSectionId);
        OwnerSectionCensusSnapshot recognitionWitness = Find(sharedOwnerSectionVector, PoliticalClaimStoreCensusProvider.RecognitionsSectionId);
        PoliticalClaimStore owner = claimWitness?.OwnerInstanceIdentity as PoliticalClaimStore;
        if (owner == null || !ReferenceEquals(owner, recognitionWitness?.OwnerInstanceIdentity)
            || !Matches(claimWitness, PoliticalClaimStoreCensusProvider.ClaimsSectionId, owner, owner.Count, owner.Revision)
            || !Matches(recognitionWitness, PoliticalClaimStoreCensusProvider.RecognitionsSectionId, owner,
                owner.RecognitionCount, owner.Revision))
            return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidOwnerSectionVector,
                "Both required witnesses must identify the same exact store and shared revision.", out failure);

        IReadOnlyList<PoliticalClaimRecord> claims = owner.Records;
        IReadOnlyList<PoliticalClaimRecognitionRecord> recognitions = owner.RecognitionRecords;
        int claimCount = owner.Count, recognitionCount = owner.RecognitionCount;
        long revision = owner.Revision;
        if (claims == null || recognitions == null || claimCount < 0 || recognitionCount < 0 || revision < 0L
            || claims.Count != claimCount || recognitions.Count != recognitionCount)
            return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidOwnerValues, "Store counts, views, or revision are inconsistent.", out failure);
        List<P12EPoliticalClaimRow> claimRows = new List<P12EPoliticalClaimRow>(claimCount);
        foreach (PoliticalClaimRecord row in claims)
        {
            if (row?.ClaimId == null || row.ClaimantPersonId == null || row.Target == null || row.EvidenceReferences == null)
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidOwnerValues, "A claim row is incomplete.", out failure);
            claimRows.Add(new P12EPoliticalClaimRow(row.ClaimId.Value, row.ClaimantPersonId.Value, (int)row.ClaimType,
                (int)row.Target.Kind, row.Target.TargetId, (int)row.Basis, row.BasisDescription, row.CreatedAbsoluteDay,
                (int)row.Status, row.ResolutionAbsoluteDay, row.EvidenceReferences));
        }
        List<P12EPoliticalClaimRecognitionRow> recognitionRows = new List<P12EPoliticalClaimRecognitionRow>(recognitionCount);
        foreach (PoliticalClaimRecognitionRecord row in recognitions)
        {
            if (row?.ClaimId == null || row.InstitutionId == null || row.History == null)
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidOwnerValues, "A recognition row is incomplete.", out failure);
            List<P12EPoliticalClaimRecognitionHistoryRow> history = new List<P12EPoliticalClaimRecognitionHistoryRow>();
            foreach (PoliticalClaimRecognitionHistoryEntry item in row.History)
            {
                if (item == null) return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidOwnerValues, "Recognition history contains a null row.", out failure);
                history.Add(new P12EPoliticalClaimRecognitionHistoryRow((int)item.State, item.RecognitionAbsoluteDay, item.Reason));
            }
            recognitionRows.Add(new P12EPoliticalClaimRecognitionRow(row.ClaimId.Value, row.InstitutionId.Value,
                row.RecognitionId, (int)row.State, row.RecognitionAbsoluteDay, row.Reason, history));
        }
        P12EPoliticalClaimOwnerSnapshot candidate = new P12EPoliticalClaimOwnerSnapshot(
            Section(PoliticalClaimStoreCensusProvider.ClaimsSectionId, claimCount, revision, claimRows, x => x?.Copy()),
            Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, recognitionCount, revision, recognitionRows, x => x?.Copy()));
        if (!candidate.TryValidate(runtime.PersonStore, runtime.PropertyOwnershipStore,
                runtime.InstitutionStoreForWorldBoundary, runtime.OfficeStoreForWorldBoundary, token.AbsoluteDay, out failure)
            || owner.Count != claimCount || owner.RecognitionCount != recognitionCount || owner.Revision != revision
            || !Matches(claimWitness, PoliticalClaimStoreCensusProvider.ClaimsSectionId, owner, claimCount, revision)
            || !Matches(recognitionWitness, PoliticalClaimStoreCensusProvider.RecognitionsSectionId, owner, recognitionCount, revision)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            snapshot = null;
            if (failure.Code == P12EPoliticalClaimSnapshotFailureCode.None)
                failure = P12EPoliticalClaimSnapshotFailure.Create(P12EPoliticalClaimSnapshotFailureCode.InvalidOwnerSectionVector,
                    "The owner changed during capture or the completed-boundary token became stale.");
            return false;
        }
        snapshot = candidate;
        failure = P12EPoliticalClaimSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(PersonStore persons, PropertyOwnershipStore properties, InstitutionStore institutions,
        OfficeStore offices, long capturedDay, out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure)
    {
        staged = null;
        if (!TryValidate(persons, properties, institutions, offices, capturedDay, out failure)) return false;
        try
        {
            List<PoliticalClaimRecord> claimRecords = new List<PoliticalClaimRecord>(Claims.RecordCount);
            foreach (P12EPoliticalClaimRow row in Claims.Records)
            {
                PoliticalClaimTargetKind kind = (PoliticalClaimTargetKind)row.TargetKind;
                PoliticalClaimTarget target;
                switch (kind)
                {
                    case PoliticalClaimTargetKind.Office: target = PoliticalClaimTarget.ForOffice(new OfficeId(row.TargetId)); break;
                    case PoliticalClaimTargetKind.Property: target = PoliticalClaimTarget.ForProperty(new PropertyId(row.TargetId)); break;
                    case PoliticalClaimTargetKind.Institution: target = PoliticalClaimTarget.ForInstitution(new InstitutionId(row.TargetId)); break;
                    case PoliticalClaimTargetKind.Person: target = PoliticalClaimTarget.ForPerson(new PersonId(row.TargetId)); break;
                    default: throw new ArgumentException("Unsupported target kind.");
                }
                claimRecords.Add(new PoliticalClaimRecord(new PoliticalClaimId(row.ClaimId), new PersonId(row.ClaimantPersonId),
                    (PoliticalClaimType)row.ClaimType, target, (PoliticalClaimBasis)row.Basis, row.BasisDescription,
                    row.CreatedAbsoluteDay, row.EvidenceReferences, (PoliticalClaimStatus)row.Status,
                    resolutionAbsoluteDay: row.ResolutionAbsoluteDay));
            }
            List<PoliticalClaimRecognitionRecord> recognitionRecords = new List<PoliticalClaimRecognitionRecord>(Recognitions.RecordCount);
            foreach (P12EPoliticalClaimRecognitionRow row in Recognitions.Records)
            {
                List<PoliticalClaimRecognitionHistoryEntry> history = new List<PoliticalClaimRecognitionHistoryEntry>();
                foreach (P12EPoliticalClaimRecognitionHistoryRow item in row.History)
                    history.Add(new PoliticalClaimRecognitionHistoryEntry((PoliticalClaimRecognitionState)item.State, item.Day, item.Reason));
                recognitionRecords.Add(new PoliticalClaimRecognitionRecord(new PoliticalClaimId(row.ClaimId),
                    new InstitutionId(row.InstitutionId), (PoliticalClaimRecognitionState)row.State,
                    row.RecognitionAbsoluteDay, row.Reason, history));
            }
            if (!PoliticalClaimStore.TryCreateFromP12EOwnerSnapshot(claimRecords, recognitionRecords, Claims.Revision,
                    out PoliticalClaimStore candidate))
                return Fail(P12EPoliticalClaimSnapshotFailureCode.StageFailed, "Private owner factory rejected validated rows.", out failure);
            staged = candidate;
            failure = P12EPoliticalClaimSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is NullReferenceException)
        { return Fail(P12EPoliticalClaimSnapshotFailureCode.StageFailed, "Private staging rejected malformed rows: " + exception.Message, out failure); }
    }

    private bool TryValidate(PersonStore persons, PropertyOwnershipStore properties, InstitutionStore institutions,
        OfficeStore offices, long capturedDay, out P12EPoliticalClaimSnapshotFailure failure)
    {
        failure = P12EPoliticalClaimSnapshotFailure.Create(P12EPoliticalClaimSnapshotFailureCode.InvalidClaim,
            "Political claim owner snapshot is malformed.");
        if (persons == null || properties == null || institutions == null || offices == null || capturedDay < 0L
            || !ReferenceEquals(offices.InstitutionStoreForWorldBoundary, institutions))
            return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidReference, "All exact staged target roots are required.", out failure);
        if (!ValidSection(Claims, PoliticalClaimStoreCensusProvider.ClaimsSectionId)
            || !ValidSection(Recognitions, PoliticalClaimStoreCensusProvider.RecognitionsSectionId))
            return Fail(P12EPoliticalClaimSnapshotFailureCode.UnsupportedSchema, "Both existing schema-v1 sections are required.", out failure);
        if (Claims.Revision < 0L || Claims.Revision != Recognitions.Revision)
            return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidRevision, "Both sections must preserve one nonnegative local revision.", out failure);
        if (Claims.Records == null || Recognitions.Records == null || Claims.RecordCount < 0 || Recognitions.RecordCount < 0
            || Claims.Records.Count != Claims.RecordCount || Recognitions.Records.Count != Recognitions.RecordCount)
            return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidCardinality, "Section cardinality must equal detached row count.", out failure);
        HashSet<string> claimIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EPoliticalClaimRow row in Claims.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ClaimId) || string.IsNullOrWhiteSpace(row.ClaimantPersonId)
                || !Enum.IsDefined(typeof(PoliticalClaimType), row.ClaimType)
                || !Enum.IsDefined(typeof(PoliticalClaimTargetKind), row.TargetKind)
                || !Enum.IsDefined(typeof(PoliticalClaimBasis), row.Basis)
                || !Enum.IsDefined(typeof(PoliticalClaimStatus), row.Status)
                || string.IsNullOrWhiteSpace(row.TargetId) || row.BasisDescription == null || row.EvidenceReferences == null)
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidIdentity, "Claim identity, enum, target, or retained values are invalid.", out failure);
            if (!claimIds.Add(row.ClaimId)) return Fail(P12EPoliticalClaimSnapshotFailureCode.DuplicateIdentity, "Claim identity is duplicated.", out failure);
            var type = (PoliticalClaimType)row.ClaimType; var kind = (PoliticalClaimTargetKind)row.TargetKind;
            if (!PoliticalClaimRecord.IsTargetCompatible(type, kind) || row.CreatedAbsoluteDay < 0L || row.CreatedAbsoluteDay > capturedDay
                || (row.Status == (int)PoliticalClaimStatus.Active && row.ResolutionAbsoluteDay.HasValue)
                || (row.Status != (int)PoliticalClaimStatus.Active && (!row.ResolutionAbsoluteDay.HasValue
                    || row.ResolutionAbsoluteDay.Value < row.CreatedAbsoluteDay || row.ResolutionAbsoluteDay.Value > capturedDay)))
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidClaim, "Claim status, type, or dates are inconsistent.", out failure);
            if (!persons.TryGet(new PersonId(row.ClaimantPersonId), out _))
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidReference, "Claimant Person is missing.", out failure);
            bool targetExists = kind == PoliticalClaimTargetKind.Person
                ? persons.TryGet(new PersonId(row.TargetId), out _)
                : kind == PoliticalClaimTargetKind.Institution
                    ? institutions.TryGet(new InstitutionId(row.TargetId), out _)
                    : kind == PoliticalClaimTargetKind.Office
                        ? offices.TryGet(new OfficeId(row.TargetId), out _)
                        : properties.TryGet(new PropertyId(row.TargetId), out _);
            if (!targetExists) return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidReference, "Typed claim target is missing.", out failure);
            string previous = null;
            foreach (string evidence in row.EvidenceReferences)
            {
                if (string.IsNullOrWhiteSpace(evidence) || (previous != null && StringComparer.Ordinal.Compare(previous, evidence) >= 0))
                    return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidClaim, "Evidence references must be unique, nonblank, and ordinal-sorted.", out failure);
                previous = evidence;
            }
        }
        HashSet<string> relations = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EPoliticalClaimRecognitionRow row in Recognitions.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ClaimId) || string.IsNullOrWhiteSpace(row.InstitutionId)
                || !Enum.IsDefined(typeof(PoliticalClaimRecognitionState), row.State) || row.Reason == null || row.History == null)
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidIdentity, "Recognition identity or values are invalid.", out failure);
            string expected = PoliticalClaimRecognitionRecord.BuildRecognitionId(new PoliticalClaimId(row.ClaimId), new InstitutionId(row.InstitutionId));
            if (!string.Equals(row.RecognitionId, expected, StringComparison.Ordinal)
                || !relations.Add(expected)) return Fail(P12EPoliticalClaimSnapshotFailureCode.DuplicateIdentity, "Recognition identity is invalid or duplicated.", out failure);
            P12EPoliticalClaimRow claim = null;
            foreach (var candidate in Claims.Records) if (string.Equals(candidate.ClaimId, row.ClaimId, StringComparison.Ordinal)) { claim = candidate; break; }
            if (claim == null || !institutions.TryGet(new InstitutionId(row.InstitutionId), out _))
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidReference, "Recognition references a missing claim or institution.", out failure);
            if (row.RecognitionAbsoluteDay < claim.CreatedAbsoluteDay || row.RecognitionAbsoluteDay > capturedDay || row.History.Count == 0)
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidRecognition, "Recognition current date or history is invalid.", out failure);
            long lastDay = -1L;
            foreach (P12EPoliticalClaimRecognitionHistoryRow item in row.History)
            {
                if (item == null || !Enum.IsDefined(typeof(PoliticalClaimRecognitionState), item.State)
                    || item.Day < claim.CreatedAbsoluteDay || item.Day > capturedDay || item.Day < lastDay || item.Reason == null)
                    return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidRecognition, "Recognition history is invalid or nonchronological.", out failure);
                lastDay = item.Day;
            }
            var terminal = row.History[row.History.Count - 1];
            if (terminal.State != row.State || terminal.Day != row.RecognitionAbsoluteDay
                || !string.Equals(terminal.Reason, row.Reason, StringComparison.Ordinal))
                return Fail(P12EPoliticalClaimSnapshotFailureCode.InvalidRecognition, "Recognition history terminal entry differs from current values.", out failure);
        }
        failure = P12EPoliticalClaimSnapshotFailure.None;
        return true;
    }

    private static bool ValidSection<T>(P12EPoliticalClaimSnapshotSection<T> section, string id) =>
        section != null && string.Equals(section.SectionId, id, StringComparison.Ordinal) && section.SchemaVersion == CurrentSchemaVersion;
    private static P12EPoliticalClaimSnapshotSection<T> Section<T>(string id, int count, long rev, IEnumerable<T> rows, Func<T,T> copy) =>
        new P12EPoliticalClaimSnapshotSection<T>(id, CurrentSchemaVersion, count, rev, rows, copy);
    private static OwnerSectionCensusSnapshot Find(IReadOnlyList<OwnerSectionCensusSnapshot> vector, string id)
    {
        OwnerSectionCensusSnapshot result = null;
        if (vector != null) foreach (var item in vector) if (item != null && string.Equals(item.SectionId, id, StringComparison.Ordinal))
        { if (result != null) return null; result = item; }
        return result;
    }
    private static bool Matches(OwnerSectionCensusSnapshot row, string id, object owner, int count, long revision) =>
        row != null && string.Equals(row.SectionId, id, StringComparison.Ordinal)
        && row.SchemaVersion == PoliticalClaimStoreCensusProvider.SchemaVersion && row.Role == OwnerSectionRole.Required
        && ReferenceEquals(row.OwnerInstanceIdentity, owner) && row.Cardinality == count && row.Revision == revision;
    private static bool Fail(P12EPoliticalClaimSnapshotFailureCode code, string message, out P12EPoliticalClaimSnapshotFailure failure)
    { failure = P12EPoliticalClaimSnapshotFailure.Create(code, message); return false; }
}
