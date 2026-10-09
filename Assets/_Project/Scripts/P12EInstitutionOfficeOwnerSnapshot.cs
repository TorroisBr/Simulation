using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EInstitutionOfficeSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    UnsupportedProfile,
    InvalidOwnerComposition,
    InvalidOwnerSectionVector,
    InvalidOwnerValues,
    InvalidSnapshot,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    DuplicateIdentity,
    InvalidRelation,
    InvalidTenure,
    StageFailed
}

internal sealed class P12EInstitutionOfficeSnapshotFailure
{
    internal static readonly P12EInstitutionOfficeSnapshotFailure None =
        new P12EInstitutionOfficeSnapshotFailure(P12EInstitutionOfficeSnapshotFailureCode.None, string.Empty);

    internal P12EInstitutionOfficeSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12EInstitutionOfficeSnapshotFailure(
        P12EInstitutionOfficeSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12EInstitutionOfficeSnapshotFailure Create(
        P12EInstitutionOfficeSnapshotFailureCode code,
        string message) => code == P12EInstitutionOfficeSnapshotFailureCode.None
            ? None
            : new P12EInstitutionOfficeSnapshotFailure(code, message);
}

/// <summary>One detached schema-v1 section. The source owner identity is intentionally absent.</summary>
internal sealed class P12EInstitutionOfficeSnapshotSection<T>
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<T> Records { get; }

    internal P12EInstitutionOfficeSnapshotSection(
        string sectionId,
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<T> records,
        Func<T, T> copy)
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
        List<T> copied = new List<T>();
        foreach (T record in records) copied.Add(copy == null ? record : copy(record));
        Records = new ReadOnlyCollection<T>(copied);
    }
}

internal sealed class P12EInstitutionSnapshotRecord
{
    internal string InstitutionIdValue { get; }
    internal string DisplayName { get; }

    internal P12EInstitutionSnapshotRecord(string institutionIdValue, string displayName)
    {
        InstitutionIdValue = institutionIdValue;
        DisplayName = displayName;
    }

    internal P12EInstitutionSnapshotRecord Copy() => new P12EInstitutionSnapshotRecord(InstitutionIdValue, DisplayName);
}

internal sealed class P12EOfficeSnapshotRecord
{
    internal string OfficeIdValue { get; }
    internal string InstitutionIdValue { get; }
    internal string DisplayName { get; }

    internal P12EOfficeSnapshotRecord(string officeIdValue, string institutionIdValue, string displayName)
    {
        OfficeIdValue = officeIdValue;
        InstitutionIdValue = institutionIdValue;
        DisplayName = displayName;
    }

    internal P12EOfficeSnapshotRecord Copy() =>
        new P12EOfficeSnapshotRecord(OfficeIdValue, InstitutionIdValue, DisplayName);
}

internal sealed class P12EOfficeIncumbencySnapshotRecord
{
    internal string OfficeIdValue { get; }
    internal string PersonIdValue { get; }
    internal long? StartAbsoluteDay { get; }

    internal P12EOfficeIncumbencySnapshotRecord(string officeIdValue, string personIdValue, long? startAbsoluteDay)
    {
        OfficeIdValue = officeIdValue;
        PersonIdValue = personIdValue;
        StartAbsoluteDay = startAbsoluteDay;
    }

    internal P12EOfficeIncumbencySnapshotRecord Copy() =>
        new P12EOfficeIncumbencySnapshotRecord(OfficeIdValue, PersonIdValue, StartAbsoluteDay);
}

internal sealed class P12EOfficeTenureSnapshotRecord
{
    internal string OfficeIdValue { get; }
    internal string PersonIdValue { get; }
    internal long? StartAbsoluteDay { get; }
    internal long? EndAbsoluteDay { get; }
    internal int? EndReason { get; }
    internal bool IsClosed { get; }

    internal P12EOfficeTenureSnapshotRecord(
        string officeIdValue,
        string personIdValue,
        long? startAbsoluteDay,
        long? endAbsoluteDay,
        int? endReason,
        bool isClosed)
    {
        OfficeIdValue = officeIdValue;
        PersonIdValue = personIdValue;
        StartAbsoluteDay = startAbsoluteDay;
        EndAbsoluteDay = endAbsoluteDay;
        EndReason = endReason;
        IsClosed = isClosed;
    }

    internal P12EOfficeTenureSnapshotRecord Copy() => new P12EOfficeTenureSnapshotRecord(
        OfficeIdValue, PersonIdValue, StartAbsoluteDay, EndAbsoluteDay, EndReason, IsClosed);
}

/// <summary>
/// Detached four-section Institution/Office export for Daily-v1. It is a private
/// owner package, not a capture token, envelope, or publication mechanism.
/// </summary>
internal sealed class P12EInstitutionOfficeOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal P12EInstitutionOfficeSnapshotSection<P12EInstitutionSnapshotRecord> Institutions { get; }
    internal P12EInstitutionOfficeSnapshotSection<P12EOfficeSnapshotRecord> Offices { get; }
    internal P12EInstitutionOfficeSnapshotSection<P12EOfficeIncumbencySnapshotRecord> Incumbencies { get; }
    internal P12EInstitutionOfficeSnapshotSection<P12EOfficeTenureSnapshotRecord> Tenures { get; }

    internal P12EInstitutionOfficeOwnerSnapshot(
        P12EInstitutionOfficeSnapshotSection<P12EInstitutionSnapshotRecord> institutions,
        P12EInstitutionOfficeSnapshotSection<P12EOfficeSnapshotRecord> offices,
        P12EInstitutionOfficeSnapshotSection<P12EOfficeIncumbencySnapshotRecord> incumbencies,
        P12EInstitutionOfficeSnapshotSection<P12EOfficeTenureSnapshotRecord> tenures)
    {
        Institutions = institutions;
        Offices = offices;
        Incumbencies = incumbencies;
        Tenures = tenures;
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EInstitutionOfficeOwnerSnapshot snapshot,
        out P12EInstitutionOfficeSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EInstitutionOfficeSnapshotFailure.Create(
            P12EInstitutionOfficeSnapshotFailureCode.InvalidCaptureContext,
            "Institution/Office capture requires a live admitted Daily-v1 runtime.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken token,
                out DailyCaptureEligibilityFailure tokenFailure))
            return false;
        if (tokenFailure != DailyCaptureEligibilityFailure.None || token == null)
            return false;
        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12EInstitutionOfficeOwnerSnapshot snapshot,
        out P12EInstitutionOfficeSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EInstitutionOfficeSnapshotFailure.Create(
            P12EInstitutionOfficeSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)) return false;
        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L)
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.UnsupportedProfile,
                "A successful Daily-v1 completed-boundary token is required.", out failure);

        InstitutionStore institutionOwner = runtime.InstitutionStoreForWorldBoundary;
        OfficeStore officeOwner = runtime.OfficeStoreForWorldBoundary;
        PersonStore personOwner = runtime.PersonStore;
        if (institutionOwner == null || officeOwner == null || personOwner == null
            || !ReferenceEquals(officeOwner.InstitutionStoreForWorldBoundary, institutionOwner))
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerComposition,
                "The installed OfficeStore must reference the exact installed InstitutionStore.", out failure);

        IReadOnlyList<InstitutionRecord> institutionRows = institutionOwner.Institutions;
        IReadOnlyList<OfficeRecord> officeRows = officeOwner.Offices;
        IReadOnlyList<OfficeIncumbency> incumbencyRows = officeOwner.Incumbencies;
        IReadOnlyList<OfficeTenureRecord> tenureRows = officeOwner.TenureHistoryInMutationOrder;
        int institutionCount = institutionOwner.Count;
        int officeCount = officeOwner.Count;
        int incumbencyCount = officeOwner.IncumbencyCount;
        int tenureCount = officeOwner.TenureCount;
        long institutionRevision = institutionOwner.Revision;
        long officeRevision = officeOwner.Revision;
        if (institutionRows == null || officeRows == null || incumbencyRows == null || tenureRows == null
            || institutionCount < 0 || officeCount < 0 || incumbencyCount < 0 || tenureCount < 0
            || institutionRevision < 0L || officeRevision < 0L
            || institutionRows.Count != institutionCount || officeRows.Count != officeCount
            || incumbencyRows.Count != incumbencyCount || tenureRows.Count != tenureCount)
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerValues,
                "Owner counts, revisions, and defensive views are inconsistent.", out failure);

        if (!Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.InstitutionsSectionId,
                institutionOwner, institutionCount, institutionRevision)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.OfficesSectionId,
                officeOwner, officeCount, officeRevision)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.IncumbenciesSectionId,
                officeOwner, incumbencyCount, officeRevision)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.TenuresSectionId,
                officeOwner, tenureCount, officeRevision))
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector,
                "The token must contain one matching required witness for each of the four existing owner sections.", out failure);

        List<P12EInstitutionSnapshotRecord> institutions = new List<P12EInstitutionSnapshotRecord>(institutionCount);
        foreach (InstitutionRecord row in institutionRows)
        {
            if (row?.Id == null) return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerValues,
                "An institution row is incomplete.", out failure);
            institutions.Add(new P12EInstitutionSnapshotRecord(row.Id.Value, row.DisplayName));
        }
        List<P12EOfficeSnapshotRecord> offices = new List<P12EOfficeSnapshotRecord>(officeCount);
        foreach (OfficeRecord row in officeRows)
        {
            if (row?.Id == null || row.InstitutionId == null) return Fail(
                P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerValues, "An office row is incomplete.", out failure);
            offices.Add(new P12EOfficeSnapshotRecord(row.Id.Value, row.InstitutionId.Value, row.DisplayName));
        }
        List<P12EOfficeIncumbencySnapshotRecord> incumbencies =
            new List<P12EOfficeIncumbencySnapshotRecord>(incumbencyCount);
        foreach (OfficeIncumbency row in incumbencyRows)
        {
            if (row?.OfficeId == null || row.Incumbent == null) return Fail(
                P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerValues, "An incumbency row is incomplete.", out failure);
            incumbencies.Add(new P12EOfficeIncumbencySnapshotRecord(
                row.OfficeId.Value, row.Incumbent.Value, row.StartAbsoluteDay));
        }
        List<P12EOfficeTenureSnapshotRecord> tenures = new List<P12EOfficeTenureSnapshotRecord>(tenureCount);
        foreach (OfficeTenureRecord row in tenureRows)
        {
            if (row?.OfficeId == null || row.Incumbent == null) return Fail(
                P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerValues, "A tenure row is incomplete.", out failure);
            tenures.Add(new P12EOfficeTenureSnapshotRecord(
                row.OfficeId.Value, row.Incumbent.Value, row.StartAbsoluteDay, row.EndAbsoluteDay,
                row.EndReason.HasValue ? (int?)row.EndReason.Value : null, row.IsClosed));
        }

        P12EInstitutionOfficeOwnerSnapshot candidate = new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, institutionCount,
                institutionRevision, institutions, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, officeCount,
                officeRevision, offices, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, incumbencyCount,
                officeRevision, incumbencies, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, tenureCount,
                officeRevision, tenures, row => row?.Copy()));

        if (!candidate.TryValidate(personOwner, out failure)
            || institutionOwner.Count != institutionCount || institutionOwner.Revision != institutionRevision
            || officeOwner.Count != officeCount || officeOwner.IncumbencyCount != incumbencyCount
            || officeOwner.TenureCount != tenureCount || officeOwner.Revision != officeRevision
            || !ReferenceEquals(officeOwner.InstitutionStoreForWorldBoundary, institutionOwner)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.InstitutionsSectionId,
                institutionOwner, institutionCount, institutionRevision)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.OfficesSectionId,
                officeOwner, officeCount, officeRevision)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.IncumbenciesSectionId,
                officeOwner, incumbencyCount, officeRevision)
            || !Matches(sharedOwnerSectionVector, InstitutionOfficeCensusProvider.TenuresSectionId,
                officeOwner, tenureCount, officeRevision)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            snapshot = null;
            if (failure.Code == P12EInstitutionOfficeSnapshotFailureCode.None)
                failure = P12EInstitutionOfficeSnapshotFailure.Create(
                    P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector,
                    "Owner values changed or the completed-boundary token became stale during capture.");
            return false;
        }
        snapshot = candidate;
        failure = P12EInstitutionOfficeSnapshotFailure.None;
        return true;
    }

    internal bool TryCreateStagedOwners(
        PersonStore stagedPersons,
        out InstitutionStore stagedInstitutions,
        out OfficeStore stagedOffices,
        out P12EInstitutionOfficeSnapshotFailure failure)
    {
        stagedInstitutions = null;
        stagedOffices = null;
        if (!TryValidate(stagedPersons, out failure)) return false;
        try
        {
            List<InstitutionRecord> institutions = new List<InstitutionRecord>(Institutions.RecordCount);
            foreach (P12EInstitutionSnapshotRecord row in Institutions.Records)
                institutions.Add(new InstitutionRecord(new InstitutionId(row.InstitutionIdValue), row.DisplayName));
            if (!InstitutionStore.TryCreateFromP12EOwnerSnapshot(
                    institutions, Institutions.Revision, out InstitutionStore institutionCandidate))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.StageFailed,
                    "The private InstitutionStore factory rejected validated values.", out failure);

            List<OfficeRecord> offices = new List<OfficeRecord>(Offices.RecordCount);
            foreach (P12EOfficeSnapshotRecord row in Offices.Records)
                offices.Add(new OfficeRecord(new OfficeId(row.OfficeIdValue),
                    new InstitutionId(row.InstitutionIdValue), row.DisplayName));
            List<OfficeIncumbency> incumbencies = new List<OfficeIncumbency>(Incumbencies.RecordCount);
            foreach (P12EOfficeIncumbencySnapshotRecord row in Incumbencies.Records)
                incumbencies.Add(new OfficeIncumbency(new OfficeId(row.OfficeIdValue),
                    new PersonId(row.PersonIdValue), row.StartAbsoluteDay));
            List<OfficeTenureRecord> tenures = new List<OfficeTenureRecord>(Tenures.RecordCount);
            foreach (P12EOfficeTenureSnapshotRecord row in Tenures.Records)
                tenures.Add(new OfficeTenureRecord(new OfficeId(row.OfficeIdValue),
                    new PersonId(row.PersonIdValue), row.StartAbsoluteDay, row.EndAbsoluteDay,
                    row.EndReason.HasValue
                        ? (InstitutionalVacancyRecognitionReason?)row.EndReason.Value
                        : null, row.IsClosed));
            if (!OfficeStore.TryCreateFromP12EOwnerSnapshot(
                    institutionCandidate, offices, incumbencies, tenures, Offices.Revision,
                    out OfficeStore officeCandidate)
                || !ReferenceEquals(officeCandidate.InstitutionStoreForWorldBoundary, institutionCandidate))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.StageFailed,
                    "The private OfficeStore factory rejected the validated owner pair.", out failure);
            stagedInstitutions = institutionCandidate;
            stagedOffices = officeCandidate;
            failure = P12EInstitutionOfficeSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            stagedInstitutions = null;
            stagedOffices = null;
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.StageFailed,
                "Private owner staging rejected malformed values: " + exception.Message, out failure);
        }
    }

    private bool TryValidate(PersonStore persons, out P12EInstitutionOfficeSnapshotFailure failure)
    {
        failure = P12EInstitutionOfficeSnapshotFailure.Create(
            P12EInstitutionOfficeSnapshotFailureCode.InvalidSnapshot,
            "Institution/Office snapshot is malformed.");
        if (persons == null) return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation,
            "The staged PersonStore is required for relation validation.", out failure);
        if (!ValidSection(Institutions, InstitutionOfficeCensusProvider.InstitutionsSectionId)
            || !ValidSection(Offices, InstitutionOfficeCensusProvider.OfficesSectionId)
            || !ValidSection(Incumbencies, InstitutionOfficeCensusProvider.IncumbenciesSectionId)
            || !ValidSection(Tenures, InstitutionOfficeCensusProvider.TenuresSectionId))
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.UnsupportedSchema,
                "The snapshot must contain exactly the four existing schema-v1 sections.", out failure);
        if (Institutions.Revision < 0L || Offices.Revision < 0L || Incumbencies.Revision < 0L
            || Tenures.Revision < 0L || Incumbencies.Revision != Offices.Revision
            || Tenures.Revision != Offices.Revision)
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidRevision,
                "Owner revisions must be nonnegative and all Office sections must share one revision.", out failure);
        if (Institutions.Records == null || Offices.Records == null || Incumbencies.Records == null
            || Tenures.Records == null || Institutions.RecordCount < 0 || Offices.RecordCount < 0
            || Incumbencies.RecordCount < 0 || Tenures.RecordCount < 0
            || Institutions.Records.Count != Institutions.RecordCount || Offices.Records.Count != Offices.RecordCount
            || Incumbencies.Records.Count != Incumbencies.RecordCount || Tenures.Records.Count != Tenures.RecordCount)
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidCardinality,
                "Section cardinality must equal its exact detached row count.", out failure);

        HashSet<string> institutionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EInstitutionSnapshotRecord row in Institutions.Records)
            if (row == null || string.IsNullOrWhiteSpace(row.InstitutionIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidIdentity, "Institution ID is invalid.", out failure);
            else if (!institutionIds.Add(row.InstitutionIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.DuplicateIdentity, "Institution ID is duplicated.", out failure);

        HashSet<string> officeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EOfficeSnapshotRecord row in Offices.Records)
            if (row == null || string.IsNullOrWhiteSpace(row.OfficeIdValue)
                || string.IsNullOrWhiteSpace(row.InstitutionIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidIdentity, "Office identity or parent is invalid.", out failure);
            else if (!officeIds.Add(row.OfficeIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.DuplicateIdentity, "Office ID is duplicated.", out failure);
            else if (!institutionIds.Contains(row.InstitutionIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation, "Office references a missing Institution.", out failure);

        Dictionary<string, P12EOfficeIncumbencySnapshotRecord> active =
            new Dictionary<string, P12EOfficeIncumbencySnapshotRecord>(StringComparer.Ordinal);
        foreach (P12EOfficeIncumbencySnapshotRecord row in Incumbencies.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.OfficeIdValue)
                || string.IsNullOrWhiteSpace(row.PersonIdValue)
                || (row.StartAbsoluteDay.HasValue && row.StartAbsoluteDay.Value < 0L))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidIdentity, "Incumbency identity or date is invalid.", out failure);
            if (!officeIds.Contains(row.OfficeIdValue) || !persons.TryGet(new PersonId(row.PersonIdValue), out _))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation, "Incumbency references a missing Office or Person.", out failure);
            if (active.ContainsKey(row.OfficeIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.DuplicateIdentity, "Office has more than one active incumbency.", out failure);
            active.Add(row.OfficeIdValue, row);
        }

        HashSet<string> openOfficeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EOfficeTenureSnapshotRecord row in Tenures.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.OfficeIdValue) || string.IsNullOrWhiteSpace(row.PersonIdValue))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidIdentity, "Tenure identity is invalid.", out failure);
            if (!officeIds.Contains(row.OfficeIdValue) || !persons.TryGet(new PersonId(row.PersonIdValue), out _))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation, "Tenure references a missing Office or Person.", out failure);
            if ((row.StartAbsoluteDay.HasValue && row.StartAbsoluteDay.Value < 0L)
                || (row.EndAbsoluteDay.HasValue && row.EndAbsoluteDay.Value < 0L)
                || (row.StartAbsoluteDay.HasValue && row.EndAbsoluteDay.HasValue
                    && row.EndAbsoluteDay.Value < row.StartAbsoluteDay.Value))
                return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidTenure, "Tenure dates are invalid.", out failure);
            if (row.IsClosed)
            {
                if (!row.EndReason.HasValue
                    || !Enum.IsDefined(typeof(InstitutionalVacancyRecognitionReason), row.EndReason.Value))
                    return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidTenure, "Closed tenure reason is unsupported.", out failure);
            }
            else
            {
                if (row.EndAbsoluteDay.HasValue || row.EndReason.HasValue || !openOfficeIds.Add(row.OfficeIdValue)
                    || !active.TryGetValue(row.OfficeIdValue, out P12EOfficeIncumbencySnapshotRecord owner)
                    || !Equals(row.OfficeIdValue, owner.OfficeIdValue)
                    || !Equals(row.PersonIdValue, owner.PersonIdValue)
                    || row.StartAbsoluteDay != owner.StartAbsoluteDay)
                    return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidTenure,
                        "Each active incumbency must have exactly one matching open tenure, with no orphan or duplicate open row.", out failure);
            }
        }
        if (openOfficeIds.Count != active.Count)
            return Fail(P12EInstitutionOfficeSnapshotFailureCode.InvalidTenure,
                "An active incumbency is missing its matching open tenure.", out failure);
        failure = P12EInstitutionOfficeSnapshotFailure.None;
        return true;
    }

    private static bool ValidSection<T>(P12EInstitutionOfficeSnapshotSection<T> section, string expectedId) =>
        section != null && string.Equals(section.SectionId, expectedId, StringComparison.Ordinal)
        && section.SchemaVersion == CurrentSchemaVersion;

    private static P12EInstitutionOfficeSnapshotSection<T> Section<T>(
        string id, int count, long revision, IEnumerable<T> rows, Func<T, T> copy) =>
        new P12EInstitutionOfficeSnapshotSection<T>(id, CurrentSchemaVersion, count, revision, rows, copy);

    private static bool Matches(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId,
        object owner,
        int cardinality,
        long revision)
    {
        int matches = 0;
        if (vector == null) return false;
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            matches++;
            if (section.SchemaVersion != InstitutionOfficeCensusProvider.SchemaVersion
                || section.Role != OwnerSectionRole.Required
                || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != cardinality || section.Revision != revision) return false;
        }
        return matches == 1;
    }

    private static bool Fail(
        P12EInstitutionOfficeSnapshotFailureCode code,
        string message,
        out P12EInstitutionOfficeSnapshotFailure failure)
    {
        failure = P12EInstitutionOfficeSnapshotFailure.Create(code, message);
        return false;
    }
}
