using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EFactionSnapshotFailureCode
{
    None = 0, InvalidCaptureContext, InvalidOwner, InvalidOwnerSectionVector,
    InvalidSnapshot, UnsupportedSchema, InvalidRevision, InvalidCardinality,
    InvalidIdentity, DuplicateIdentity, InvalidReference, InvalidPolicy,
    InvalidTenure, StageFailed
}

internal sealed class P12EFactionSnapshotFailure
{
    internal static readonly P12EFactionSnapshotFailure None =
        new P12EFactionSnapshotFailure(P12EFactionSnapshotFailureCode.None, string.Empty);
    internal P12EFactionSnapshotFailureCode Code { get; }
    internal string Message { get; }
    private P12EFactionSnapshotFailure(P12EFactionSnapshotFailureCode code, string message)
    { Code = code; Message = message ?? string.Empty; }
    internal static P12EFactionSnapshotFailure Create(P12EFactionSnapshotFailureCode code, string message) =>
        code == P12EFactionSnapshotFailureCode.None ? None : new P12EFactionSnapshotFailure(code, message);
}

internal sealed class P12EFactionSnapshotSection<T>
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<T> Records { get; }
    internal P12EFactionSnapshotSection(string id, int schema, int count, long revision, IEnumerable<T> records, Func<T,T> copy)
    {
        SectionId = id; SchemaVersion = schema; RecordCount = count; Revision = revision;
        if (records == null) return;
        List<T> rows = new List<T>();
        foreach (T record in records) rows.Add(copy == null ? record : copy(record));
        Records = new ReadOnlyCollection<T>(rows);
    }
}

internal sealed class P12EFactionSnapshotRecord
{
    internal string Id { get; }
    internal string DisplayName { get; }
    internal long CreatedAbsoluteDay { get; }
    internal int MembershipPolicy { get; }
    internal bool ExpulsionAllowed { get; }
    internal P12EFactionSnapshotRecord(string id, string name, long day, int policy, bool expulsion)
    { Id = id; DisplayName = name; CreatedAbsoluteDay = day; MembershipPolicy = policy; ExpulsionAllowed = expulsion; }
    internal P12EFactionSnapshotRecord Copy() => new P12EFactionSnapshotRecord(Id, DisplayName, CreatedAbsoluteDay, MembershipPolicy, ExpulsionAllowed);
}

internal sealed class P12EFactionAffiliationSnapshotRecord
{
    internal string AffiliationId { get; }
    internal string FactionId { get; }
    internal string PersonId { get; }
    internal long JoinedAbsoluteDay { get; }
    internal long? EndedAbsoluteDay { get; }
    internal int? EndReason { get; }
    internal bool IsActive { get; }
    internal P12EFactionAffiliationSnapshotRecord(string aid, string fid, string pid, long joined, long? ended, int? reason, bool active)
    { AffiliationId = aid; FactionId = fid; PersonId = pid; JoinedAbsoluteDay = joined; EndedAbsoluteDay = ended; EndReason = reason; IsActive = active; }
    internal P12EFactionAffiliationSnapshotRecord Copy() => new P12EFactionAffiliationSnapshotRecord(AffiliationId, FactionId, PersonId, JoinedAbsoluteDay, EndedAbsoluteDay, EndReason, IsActive);
}

/// <summary>Detached FactionStore facts for schema-v1 P12-E; contains no live owner machinery.</summary>
internal sealed class P12EFactionOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;
    internal P12EFactionSnapshotSection<P12EFactionSnapshotRecord> Factions { get; }
    internal P12EFactionSnapshotSection<P12EFactionAffiliationSnapshotRecord> Affiliations { get; }
    internal P12EFactionOwnerSnapshot(P12EFactionSnapshotSection<P12EFactionSnapshotRecord> factions,
        P12EFactionSnapshotSection<P12EFactionAffiliationSnapshotRecord> affiliations)
    { Factions = factions; Affiliations = affiliations; }

    internal static bool TryCapture(SimulationRuntime runtime, FactionStore owner,
        out P12EFactionOwnerSnapshot snapshot, out P12EFactionSnapshotFailure failure)
    {
        snapshot = null; failure = P12EFactionSnapshotFailure.Create(P12EFactionSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the live admitted Daily-v1 runtime and its section vector.");
        if (runtime == null || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out DailyCaptureEligibilityFailure tokenFailure)
            || tokenFailure != DailyCaptureEligibilityFailure.None || token == null) return false;
        return TryCapture(runtime, owner, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(SimulationRuntime runtime, FactionStore owner, DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector, out P12EFactionOwnerSnapshot snapshot,
        out P12EFactionSnapshotFailure failure)
    {
        snapshot = null; failure = P12EFactionSnapshotFailure.Create(P12EFactionSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact completed-boundary token and section vector.");
        if (runtime == null || owner == null || token == null || vector == null || !ReferenceEquals(token.OwnerSections, vector)
            || token.AdmissionContext == null || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L) return false;
        IReadOnlyList<FactionRecord> factions = owner.Factions;
        IReadOnlyList<FactionAffiliationRecord> affiliations = owner.Affiliations;
        int factionCount = owner.Count, affiliationCount = owner.AffiliationCount;
        long revision = owner.Revision;
        if (factions == null || affiliations == null || factionCount < 0 || affiliationCount < 0 || revision < 0L
            || factions.Count != factionCount || affiliations.Count != affiliationCount
            || !Matches(vector, FactionStoreCensusProvider.FactionsSectionId, owner, factionCount, revision)
            || !Matches(vector, FactionStoreCensusProvider.AffiliationsSectionId, owner, affiliationCount, revision))
            return Fail(P12EFactionSnapshotFailureCode.InvalidOwnerSectionVector, "Owner values and required census witnesses disagree.", out failure);
        List<P12EFactionSnapshotRecord> factionRows = new List<P12EFactionSnapshotRecord>(factionCount);
        foreach (FactionRecord row in factions)
        {
            if (row?.Id == null) return Fail(P12EFactionSnapshotFailureCode.InvalidOwner, "Faction row is incomplete.", out failure);
            factionRows.Add(new P12EFactionSnapshotRecord(row.Id.Value, row.DisplayName, row.CreatedAbsoluteDay, (int)row.MembershipPolicy, row.ExpulsionAllowed));
        }
        List<P12EFactionAffiliationSnapshotRecord> affiliationRows = new List<P12EFactionAffiliationSnapshotRecord>(affiliationCount);
        foreach (FactionAffiliationRecord row in affiliations)
        {
            if (row?.AffiliationId == null || row.FactionId == null || row.PersonId == null)
                return Fail(P12EFactionSnapshotFailureCode.InvalidOwner, "Affiliation row is incomplete.", out failure);
            if (!owner.HasPersonForP12EOwnerSnapshot(row.PersonId))
                return Fail(P12EFactionSnapshotFailureCode.InvalidOwner, "Affiliation references a person outside its exact owner root.", out failure);
            affiliationRows.Add(new P12EFactionAffiliationSnapshotRecord(row.AffiliationId.Value, row.FactionId.Value,
                row.PersonId.Value, row.JoinedAbsoluteDay, row.EndedAbsoluteDay,
                row.EndReason.HasValue ? (int?)row.EndReason.Value : null, row.IsActive));
        }
        P12EFactionOwnerSnapshot candidate = new P12EFactionOwnerSnapshot(
            new P12EFactionSnapshotSection<P12EFactionSnapshotRecord>(FactionStoreCensusProvider.FactionsSectionId, 1, factionCount, revision, factionRows, r => r?.Copy()),
            new P12EFactionSnapshotSection<P12EFactionAffiliationSnapshotRecord>(FactionStoreCensusProvider.AffiliationsSectionId, 1, affiliationCount, revision, affiliationRows, r => r?.Copy()));
        if (!candidate.TryValidate(null, token.AbsoluteDay, false, out failure)
            || owner.Revision != revision || owner.Count != factionCount || owner.AffiliationCount != affiliationCount
            || !Matches(vector, FactionStoreCensusProvider.FactionsSectionId, owner, factionCount, revision)
            || !Matches(vector, FactionStoreCensusProvider.AffiliationsSectionId, owner, affiliationCount, revision)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            snapshot = null;
            if (failure.Code == P12EFactionSnapshotFailureCode.None) failure = P12EFactionSnapshotFailure.Create(
                P12EFactionSnapshotFailureCode.InvalidOwnerSectionVector, "Owner values or capture token changed during export.");
            return false;
        }
        snapshot = candidate; failure = P12EFactionSnapshotFailure.None; return true;
    }

    internal bool TryCreateStagedOwner(PersonStore stagedPersons, long currentDay, out FactionStore staged,
        out P12EFactionSnapshotFailure failure)
    {
        staged = null;
        if (!TryValidate(stagedPersons, currentDay, true, out failure)) return false;
        try
        {
            List<FactionRecord> factions = new List<FactionRecord>(Factions.RecordCount);
            foreach (P12EFactionSnapshotRecord row in Factions.Records)
                factions.Add(new FactionRecord(new FactionId(row.Id), row.DisplayName, row.CreatedAbsoluteDay,
                    (FactionMembershipPolicy)row.MembershipPolicy, row.ExpulsionAllowed));
            List<FactionAffiliationRecord> affiliations = new List<FactionAffiliationRecord>(Affiliations.RecordCount);
            foreach (P12EFactionAffiliationSnapshotRecord row in Affiliations.Records)
                affiliations.Add(new FactionAffiliationRecord(new FactionId(row.FactionId), new PersonId(row.PersonId),
                    row.JoinedAbsoluteDay, row.EndedAbsoluteDay, new FactionAffiliationId(row.AffiliationId),
                    row.EndReason.HasValue ? (FactionAffiliationEndReason?)row.EndReason.Value : null));
            if (!FactionStore.TryCreateFromP12EOwnerSnapshot(stagedPersons, factions, affiliations, Factions.Revision, out staged))
                return Fail(P12EFactionSnapshotFailureCode.StageFailed, "Private FactionStore factory rejected validated values.", out failure);
            failure = P12EFactionSnapshotFailure.None; return true;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException)
        { staged = null; return Fail(P12EFactionSnapshotFailureCode.StageFailed, "Malformed Faction snapshot: " + ex.Message, out failure); }
    }

    private bool TryValidate(PersonStore persons, long currentDay, bool requirePersons, out P12EFactionSnapshotFailure failure)
    {
        failure = P12EFactionSnapshotFailure.Create(P12EFactionSnapshotFailureCode.InvalidSnapshot, "Faction snapshot is malformed.");
        if (currentDay < 0L || (requirePersons && persons == null)) return Fail(P12EFactionSnapshotFailureCode.InvalidReference, "A valid staged PersonStore and current day are required.", out failure);
        if (!Valid(Factions, FactionStoreCensusProvider.FactionsSectionId) || !Valid(Affiliations, FactionStoreCensusProvider.AffiliationsSectionId))
            return Fail(P12EFactionSnapshotFailureCode.UnsupportedSchema, "Both required schema-v1 sections must be present.", out failure);
        if (Factions.Revision < 0L || Affiliations.Revision != Factions.Revision)
            return Fail(P12EFactionSnapshotFailureCode.InvalidRevision, "Sections must carry one nonnegative shared owner revision.", out failure);
        if (Factions.Records == null || Affiliations.Records == null || Factions.RecordCount < 0 || Affiliations.RecordCount < 0
            || Factions.Records.Count != Factions.RecordCount || Affiliations.Records.Count != Affiliations.RecordCount)
            return Fail(P12EFactionSnapshotFailureCode.InvalidCardinality, "Section row counts must match their declared cardinalities.", out failure);
        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EFactionSnapshotRecord row in Factions.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id)) return Fail(P12EFactionSnapshotFailureCode.InvalidIdentity, "Faction ID is missing.", out failure);
            if (!factionIds.Add(row.Id)) return Fail(P12EFactionSnapshotFailureCode.DuplicateIdentity, "Faction ID is duplicated.", out failure);
            if (!Enum.IsDefined(typeof(FactionMembershipPolicy), row.MembershipPolicy)) return Fail(P12EFactionSnapshotFailureCode.InvalidPolicy, "Faction membership policy is undefined.", out failure);
            if (row.CreatedAbsoluteDay < 0L || row.CreatedAbsoluteDay > currentDay) return Fail(P12EFactionSnapshotFailureCode.InvalidTenure, "Faction creation day is outside captured time.", out failure);
        }
        HashSet<string> affiliationIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> activeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12EFactionAffiliationSnapshotRecord row in Affiliations.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.AffiliationId) || string.IsNullOrWhiteSpace(row.FactionId) || string.IsNullOrWhiteSpace(row.PersonId))
                return Fail(P12EFactionSnapshotFailureCode.InvalidIdentity, "Affiliation identity is incomplete.", out failure);
            if (!affiliationIds.Add(row.AffiliationId)) return Fail(P12EFactionSnapshotFailureCode.DuplicateIdentity, "Affiliation ID is duplicated.", out failure);
            if (!factionIds.Contains(row.FactionId) || (requirePersons && !persons.TryGet(new PersonId(row.PersonId), out _)))
                return Fail(P12EFactionSnapshotFailureCode.InvalidReference, "Affiliation references a missing faction or person.", out failure);
            P12EFactionSnapshotRecord faction = null;
            foreach (P12EFactionSnapshotRecord candidate in Factions.Records) if (candidate.Id == row.FactionId) { faction = candidate; break; }
            if (row.JoinedAbsoluteDay < 0L || row.JoinedAbsoluteDay > currentDay || row.JoinedAbsoluteDay < faction.CreatedAbsoluteDay
                || (row.EndedAbsoluteDay.HasValue && (row.EndedAbsoluteDay.Value < row.JoinedAbsoluteDay || row.EndedAbsoluteDay.Value > currentDay))
                || (row.EndReason.HasValue && (!row.EndedAbsoluteDay.HasValue || !Enum.IsDefined(typeof(FactionAffiliationEndReason), row.EndReason.Value)))
                || row.IsActive != !row.EndedAbsoluteDay.HasValue)
                return Fail(P12EFactionSnapshotFailureCode.InvalidTenure, "Affiliation dates, end reason, or derived active flag are invalid.", out failure);
            if (row.IsActive && !activeKeys.Add(row.FactionId + "\u001f" + row.PersonId))
                return Fail(P12EFactionSnapshotFailureCode.DuplicateIdentity, "Active affiliation key is duplicated or aliases another typed pair.", out failure);
        }
        failure = P12EFactionSnapshotFailure.None; return true;
    }

    private static bool Valid<T>(P12EFactionSnapshotSection<T> section, string id) => section != null
        && section.SchemaVersion == CurrentSchemaVersion && string.Equals(section.SectionId, id, StringComparison.Ordinal);
    private static bool Matches(IReadOnlyList<OwnerSectionCensusSnapshot> vector, string id, object owner, int count, long revision)
    {
        int found = 0; if (vector == null) return false;
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section == null || !string.Equals(section.SectionId, id, StringComparison.Ordinal)) continue;
            found++;
            if (section.SchemaVersion != 1 || section.Role != OwnerSectionRole.Required || !ReferenceEquals(section.OwnerInstanceIdentity, owner)
                || section.Cardinality != count || section.Revision != revision) return false;
        }
        return found == 1;
    }
    private static bool Fail(P12EFactionSnapshotFailureCode code, string message, out P12EFactionSnapshotFailure failure)
    { failure = P12EFactionSnapshotFailure.Create(code, message); return false; }
}
