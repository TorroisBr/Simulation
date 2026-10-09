using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EJusticeSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerSectionVector,
    InvalidOwnerValues,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    InvalidReference,
    InvalidRelation,
    InvalidOrdering,
    StageFailed
}

internal sealed class P12EJusticeSnapshotFailure
{
    internal static readonly P12EJusticeSnapshotFailure None =
        new P12EJusticeSnapshotFailure(P12EJusticeSnapshotFailureCode.None, string.Empty);

    internal P12EJusticeSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12EJusticeSnapshotFailure(P12EJusticeSnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12EJusticeSnapshotFailure Create(
        P12EJusticeSnapshotFailureCode code,
        string message) => code == P12EJusticeSnapshotFailureCode.None
            ? None
            : new P12EJusticeSnapshotFailure(code, message);
}

internal sealed class P12EJusticeWantedSnapshotRow
{
    internal int Ordinal { get; }
    internal string TargetRuntimeId { get; }
    internal string TargetPersonIdValue { get; }
    internal string CityRuntimeId { get; }
    internal float Bounty { get; }
    internal int SentenceDays { get; }
    internal bool IsResolved { get; }

    internal P12EJusticeWantedSnapshotRow(
        int ordinal,
        string targetRuntimeId,
        string targetPersonIdValue,
        string cityRuntimeId,
        float bounty,
        int sentenceDays,
        bool isResolved)
    {
        Ordinal = ordinal;
        TargetRuntimeId = targetRuntimeId;
        TargetPersonIdValue = targetPersonIdValue;
        CityRuntimeId = cityRuntimeId;
        Bounty = bounty;
        SentenceDays = sentenceDays;
        IsResolved = isResolved;
    }

    internal P12EJusticeWantedSnapshotRow Copy() => new P12EJusticeWantedSnapshotRow(
        Ordinal, TargetRuntimeId, TargetPersonIdValue, CityRuntimeId, Bounty, SentenceDays, IsResolved);
}

internal sealed class P12EJusticeSentenceSnapshotRow
{
    internal int Ordinal { get; }
    internal string TargetRuntimeId { get; }
    internal string TargetPersonIdValue { get; }
    internal string CityRuntimeId { get; }
    internal int WarrantOrdinal { get; }
    internal int RemainingDays { get; }
    internal int FailedEscapeAttempts { get; }
    internal bool WasArrestedToday { get; }

    internal P12EJusticeSentenceSnapshotRow(
        int ordinal,
        string targetRuntimeId,
        string targetPersonIdValue,
        string cityRuntimeId,
        int warrantOrdinal,
        int remainingDays,
        int failedEscapeAttempts,
        bool wasArrestedToday)
    {
        Ordinal = ordinal;
        TargetRuntimeId = targetRuntimeId;
        TargetPersonIdValue = targetPersonIdValue;
        CityRuntimeId = cityRuntimeId;
        WarrantOrdinal = warrantOrdinal;
        RemainingDays = remainingDays;
        FailedEscapeAttempts = failedEscapeAttempts;
        WasArrestedToday = wasArrestedToday;
    }

    internal P12EJusticeSentenceSnapshotRow Copy() => new P12EJusticeSentenceSnapshotRow(
        Ordinal, TargetRuntimeId, TargetPersonIdValue, CityRuntimeId, WarrantOrdinal,
        RemainingDays, FailedEscapeAttempts, WasArrestedToday);
}

internal sealed class P12EJusticeRecordsSnapshotSection
{
    internal const string CurrentSectionId = "p12.crime-justice.records";
    internal const int CurrentSchemaVersion = 1;

    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12EJusticeWantedSnapshotRow> WantedRows { get; }
    internal IReadOnlyList<P12EJusticeSentenceSnapshotRow> SentenceRows { get; }

    internal P12EJusticeRecordsSnapshotSection(
        string sectionId,
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<P12EJusticeWantedSnapshotRow> wantedRows,
        IEnumerable<P12EJusticeSentenceSnapshotRow> sentenceRows)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        WantedRows = Copy(wantedRows);
        SentenceRows = Copy(sentenceRows);
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) where T : class
    {
        if (source == null) return null;
        List<T> copied = new List<T>();
        foreach (T row in source)
        {
            if (row is P12EJusticeWantedSnapshotRow wanted)
                copied.Add((T)(object)wanted.Copy());
            else if (row is P12EJusticeSentenceSnapshotRow sentence)
                copied.Add((T)(object)sentence.Copy());
            else
                copied.Add(row);
        }
        return new ReadOnlyCollection<T>(copied);
    }
}

/// <summary>Detached export and private staged reconstruction for the current Justice owner rows.</summary>
internal sealed class P12EJusticeRecordsOwnerSnapshot
{
    internal const int CurrentSchemaVersion = P12EJusticeRecordsSnapshotSection.CurrentSchemaVersion;

    internal long CapturedAbsoluteDay { get; }
    internal P12EJusticeRecordsSnapshotSection Records { get; }

    internal P12EJusticeRecordsOwnerSnapshot(
        long capturedAbsoluteDay,
        P12EJusticeRecordsSnapshotSection records)
    {
        CapturedAbsoluteDay = capturedAbsoluteDay;
        Records = records;
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EJusticeRecordsOwnerSnapshot snapshot,
        out P12EJusticeSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EJusticeSnapshotFailure.Create(
            P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires an admitted completed Daily-v1 runtime.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out _)
            || token == null)
            return false;
        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12EJusticeRecordsOwnerSnapshot snapshot,
        out P12EJusticeSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EJusticeSnapshotFailure.Create(
            P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its owner-section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
            return false;
        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
                "A successful completed Daily-v1 boundary token is required.", out failure);

        OwnerSectionCensusSnapshot justiceWitness = Find(sharedOwnerSectionVector,
            P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
        JusticeSystem owner = justiceWitness?.OwnerInstanceIdentity as JusticeSystem;
        if (owner == null || justiceWitness.SchemaVersion != P12CrimeJusticeCensusProvider.SchemaVersion
            || justiceWitness.Role != OwnerSectionRole.Required)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                "The Required Justice records witness must identify the exact registered owner.", out failure);

        OwnerSectionCensusSnapshot receiptWitness = Find(sharedOwnerSectionVector,
            P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId);
        if (!TryMatchesReceiptWitness(receiptWitness, owner))
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                "Daily-v1 requires the exact Justice P18 receipt witness at cardinality one and revision zero.",
                out failure);

        if (!TryBuildInstalledMaps(runtime, out HashSet<NpcRuntime> installedNpcs,
                out HashSet<CityRuntime> installedCities,
                out Dictionary<string, NpcRuntime> installedNpcsById))
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
                "The admitted runtime NPC or City roots have invalid or ambiguous identities.", out failure);

        if (!owner.TryReadP12CrimeJusticeCensus(installedNpcs.Contains, installedCities.Contains,
                out int wantedCount, out int sentenceCount, out long revision))
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
                "Justice rows do not resolve to the exact admitted NPC and City roots.", out failure);

        int recordCount;
        try { recordCount = checked(wantedCount + sentenceCount); }
        catch (OverflowException)
        {
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Combined Justice row cardinality overflowed.", out failure);
        }
        if (!Matches(justiceWitness, owner, recordCount, revision))
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                "The Justice witness does not match its exact current owner, count, and revision.", out failure);

        if (!owner.TryCaptureP12EOwnerSnapshotRows(
                out IReadOnlyList<P12EJusticeWantedSnapshotRow> wantedRows,
                out IReadOnlyList<P12EJusticeSentenceSnapshotRow> sentenceRows)
            || wantedRows == null || sentenceRows == null
            || wantedRows.Count != wantedCount || sentenceRows.Count != sentenceCount)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
                "Justice rows could not be copied as a stable owner value snapshot.", out failure);
        foreach (P12EJusticeWantedSnapshotRow row in wantedRows)
        {
            if (!installedNpcsById.TryGetValue(row.TargetRuntimeId, out NpcRuntime target)
                || !TryValidateTargetPersonBinding(row.TargetRuntimeId,
                    row.TargetPersonIdValue, target, runtime.PersonStore))
                return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                    "A wanted-record target has a dangling or non-reciprocal PersonStore binding.", out failure);
        }
        foreach (P12EJusticeSentenceSnapshotRow row in sentenceRows)
        {
            if (!installedNpcsById.TryGetValue(row.TargetRuntimeId, out NpcRuntime target)
                || !TryValidateTargetPersonBinding(row.TargetRuntimeId,
                    row.TargetPersonIdValue, target, runtime.PersonStore))
                return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                    "A sentence target has a dangling or non-reciprocal PersonStore binding.", out failure);
        }

        P12EJusticeRecordsOwnerSnapshot candidate = new P12EJusticeRecordsOwnerSnapshot(
            token.AbsoluteDay,
            new P12EJusticeRecordsSnapshotSection(
                P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                CurrentSchemaVersion,
                recordCount,
                revision,
                wantedRows,
                sentenceRows));
        if (!candidate.TryValidate(out failure)
            || !owner.TryReadP12CrimeJusticeCensus(installedNpcs.Contains, installedCities.Contains,
                out int finalWantedCount, out int finalSentenceCount, out long finalRevision)
            || finalWantedCount != wantedCount || finalSentenceCount != sentenceCount || finalRevision != revision
            || !Matches(justiceWitness, owner, recordCount, revision)
            || !TryMatchesReceiptWitness(receiptWitness, owner)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            snapshot = null;
            if (failure.Code == P12EJusticeSnapshotFailureCode.None)
                failure = P12EJusticeSnapshotFailure.Create(
                    P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                    "Justice, its P18 receipt witness, or the completed-boundary token changed during capture.");
            return false;
        }

        snapshot = candidate;
        failure = P12EJusticeSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        IReadOnlyList<CityRuntime> stagedCities,
        IReadOnlyList<NpcRuntime> stagedNpcs,
        PersonStore stagedPersons,
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        out JusticeSystem staged,
        out P12EJusticeSnapshotFailure failure)
    {
        staged = null;
        if (!TryValidate(out failure)) return false;
        if (stagedCities == null || stagedNpcs == null || stagedPersons == null
            || freeStatus == null || wantedStatus == null || arrestedStatus == null || hiddenStatus == null)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                "Exact staged City, NPC, PersonStore, and Justice configuration roots are required.", out failure);

        if (!TryBuildRootMaps(stagedCities, stagedNpcs,
                out Dictionary<string, CityRuntime> citiesById,
                out Dictionary<string, NpcRuntime> npcsById))
            return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                "Staged root identities are missing or ambiguous.", out failure);

        try
        {
            List<WantedRecordRuntime> wanted = new List<WantedRecordRuntime>(Records.WantedRows.Count);
            foreach (P12EJusticeWantedSnapshotRow row in Records.WantedRows)
            {
                if (!npcsById.TryGetValue(row.TargetRuntimeId, out NpcRuntime target)
                    || !citiesById.TryGetValue(row.CityRuntimeId, out CityRuntime city)
                    || !TryValidateTargetPersonBinding(row.TargetRuntimeId, row.TargetPersonIdValue,
                        target, stagedPersons))
                    return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                        "A wanted record target or City does not resolve to the exact staged roots.", out failure);

                if (!WantedRecordRuntime.TryCreateFromP12EOwnerSnapshot(
                        target, city, row.Bounty, row.SentenceDays, row.IsResolved,
                        out WantedRecordRuntime warrant))
                    return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                        "The private wanted-record factory rejected a validated row.", out failure);
                wanted.Add(warrant);
            }

            List<PrisonSentenceRuntime> sentences =
                new List<PrisonSentenceRuntime>(Records.SentenceRows.Count);
            foreach (P12EJusticeSentenceSnapshotRow row in Records.SentenceRows)
            {
                if (!npcsById.TryGetValue(row.TargetRuntimeId, out NpcRuntime target)
                    || !citiesById.TryGetValue(row.CityRuntimeId, out CityRuntime city)
                    || !TryValidateTargetPersonBinding(row.TargetRuntimeId, row.TargetPersonIdValue,
                        target, stagedPersons)
                    || row.WarrantOrdinal < 0 || row.WarrantOrdinal >= wanted.Count)
                    return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                        "A sentence target, City, Person binding, or warrant ordinal is invalid.", out failure);

                WantedRecordRuntime warrant = wanted[row.WarrantOrdinal];
                if (!ReferenceEquals(target, warrant.Target) || !ReferenceEquals(city, warrant.City))
                    return Fail(P12EJusticeSnapshotFailureCode.InvalidRelation,
                        "A sentence must reference the exact target and City objects of its warrant.", out failure);
                if (!PrisonSentenceRuntime.TryCreateFromP12EOwnerSnapshot(
                        target, city, warrant, row.RemainingDays, row.FailedEscapeAttempts,
                        row.WasArrestedToday, out PrisonSentenceRuntime sentence))
                    return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                        "The private prison-sentence factory rejected a validated row.", out failure);
                sentences.Add(sentence);
            }

            if (!JusticeSystem.TryCreateFromP12EOwnerSnapshot(
                    freeStatus, wantedStatus, arrestedStatus, hiddenStatus,
                    domainEventRecorder, logger, wanted, sentences, Records.Revision,
                    out JusticeSystem candidate))
                return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                    "The private JusticeSystem factory rejected the staged rows.", out failure);

            staged = candidate;
            failure = P12EJusticeSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            staged = null;
            return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                "Private staging rejected malformed Justice values: " + exception.Message, out failure);
        }
    }

    private bool TryValidate(out P12EJusticeSnapshotFailure failure)
    {
        failure = P12EJusticeSnapshotFailure.Create(
            P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
            "Justice owner snapshot is malformed.");
        if (CapturedAbsoluteDay < 0L)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
                "Captured day must be nonnegative.", out failure);
        if (Records == null
            || !string.Equals(Records.SectionId, P12EJusticeRecordsSnapshotSection.CurrentSectionId, StringComparison.Ordinal)
            || Records.SchemaVersion != CurrentSchemaVersion)
            return Fail(P12EJusticeSnapshotFailureCode.UnsupportedSchema,
                "The current Justice owner section and schema are required.", out failure);
        if (Records.Revision < 0L)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidRevision,
                "The exact Justice local revision must be nonnegative.", out failure);
        if (Records.WantedRows == null || Records.SentenceRows == null
            || Records.RecordCount < 0)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Both detached row lists and a nonnegative combined cardinality are required.", out failure);

        int combinedCount;
        try { combinedCount = checked(Records.WantedRows.Count + Records.SentenceRows.Count); }
        catch (OverflowException)
        {
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Combined Justice row cardinality overflowed.", out failure);
        }
        if (Records.RecordCount != combinedCount)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Declared combined cardinality must equal the two detached row counts.", out failure);

        for (int i = 0; i < Records.WantedRows.Count; i++)
        {
            P12EJusticeWantedSnapshotRow row = Records.WantedRows[i];
            if (row == null || row.Ordinal != i || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
                || string.IsNullOrWhiteSpace(row.CityRuntimeId)
                || (row.TargetPersonIdValue != null
                    && (!PersonId.TryCreate(row.TargetPersonIdValue, out _)
                        || string.IsNullOrWhiteSpace(row.TargetPersonIdValue)))
                || float.IsNaN(row.Bounty) || float.IsInfinity(row.Bounty) || row.Bounty < 0f
                || row.SentenceDays < 1)
                return Fail(P12EJusticeSnapshotFailureCode.InvalidIdentity,
                    "A wanted row has an invalid ordinal, endpoint identity, or scalar value.", out failure);
        }

        for (int i = 0; i < Records.SentenceRows.Count; i++)
        {
            P12EJusticeSentenceSnapshotRow row = Records.SentenceRows[i];
            if (row == null || row.Ordinal != i || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
                || string.IsNullOrWhiteSpace(row.CityRuntimeId)
                || (row.TargetPersonIdValue != null
                    && (!PersonId.TryCreate(row.TargetPersonIdValue, out _)
                        || string.IsNullOrWhiteSpace(row.TargetPersonIdValue)))
                || row.WarrantOrdinal < 0 || row.WarrantOrdinal >= Records.WantedRows.Count
                || row.RemainingDays < 0 || row.FailedEscapeAttempts < 0)
                return Fail(P12EJusticeSnapshotFailureCode.InvalidIdentity,
                    "A sentence row has an invalid ordinal, endpoint identity, scalar, or warrant ordinal.", out failure);

            P12EJusticeWantedSnapshotRow warrant = Records.WantedRows[row.WarrantOrdinal];
            if (!string.Equals(row.TargetRuntimeId, warrant.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(row.TargetPersonIdValue, warrant.TargetPersonIdValue, StringComparison.Ordinal)
                || !string.Equals(row.CityRuntimeId, warrant.CityRuntimeId, StringComparison.Ordinal))
                return Fail(P12EJusticeSnapshotFailureCode.InvalidRelation,
                    "A sentence row must retain the exact target/City relation of its warrant.", out failure);
        }

        failure = P12EJusticeSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildInstalledMaps(
        SimulationRuntime runtime,
        out HashSet<NpcRuntime> npcs,
        out HashSet<CityRuntime> cities,
        out Dictionary<string, NpcRuntime> npcsById)
    {
        npcs = new HashSet<NpcRuntime>();
        cities = new HashSet<CityRuntime>();
        npcsById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        if (runtime.NpcRuntimes == null || runtime.Cities == null) return false;
        foreach (NpcRuntime npc in runtime.NpcRuntimes)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcs.Add(npc) || !npcIds.Add(npc.RuntimeId)) return false;
            npcsById.Add(npc.RuntimeId, npc);
        }
        foreach (CityRuntime city in runtime.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || !cities.Add(city) || !cityIds.Add(city.RuntimeId)) return false;
        }
        return true;
    }

    private static bool TryBuildRootMaps(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        out Dictionary<string, CityRuntime> citiesById,
        out Dictionary<string, NpcRuntime> npcsById)
    {
        citiesById = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
        npcsById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        HashSet<CityRuntime> cityObjects = new HashSet<CityRuntime>();
        HashSet<NpcRuntime> npcObjects = new HashSet<NpcRuntime>();
        foreach (CityRuntime city in cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || !cityObjects.Add(city) || citiesById.ContainsKey(city.RuntimeId)) return false;
            citiesById.Add(city.RuntimeId, city);
        }
        foreach (NpcRuntime npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcObjects.Add(npc) || npcsById.ContainsKey(npc.RuntimeId)) return false;
            npcsById.Add(npc.RuntimeId, npc);
        }
        return true;
    }

    private static bool TryValidateTargetPersonBinding(
        string runtimeId,
        string personIdValue,
        NpcRuntime npc,
        PersonStore persons)
    {
        if (npc == null || persons == null || !string.Equals(npc.RuntimeId, runtimeId, StringComparison.Ordinal))
            return false;
        if (personIdValue == null)
            return npc.PersonId == null && npc.BoundPersonRuntime == null
                && !persons.TryGetByMaterializedNpcRuntimeId(runtimeId, out _);
        if (!PersonId.TryCreate(personIdValue, out PersonId personId)
            || npc.PersonId == null || !string.Equals(npc.PersonId.Value, personIdValue, StringComparison.Ordinal)
            || !persons.TryGet(personId, out PersonRuntime person) || person == null
            || !string.Equals(person.MaterializedNpcRuntimeId, runtimeId, StringComparison.Ordinal)
            || !persons.TryGetByMaterializedNpcRuntimeId(runtimeId, out PersonRuntime indexed)
            || !ReferenceEquals(indexed, person)
            || !ReferenceEquals(npc.BoundPersonRuntime, person))
            return false;
        return true;
    }

    private static OwnerSectionCensusSnapshot Find(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId)
    {
        OwnerSectionCensusSnapshot result = null;
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
        JusticeSystem owner,
        int count,
        long revision) => witness != null
        && string.Equals(witness.SectionId, P12CrimeJusticeCensusProvider.JusticeRecordsSectionId, StringComparison.Ordinal)
        && witness.SchemaVersion == P12CrimeJusticeCensusProvider.SchemaVersion
        && witness.Role == OwnerSectionRole.Required
        && ReferenceEquals(witness.OwnerInstanceIdentity, owner)
        && witness.Cardinality == count
        && witness.Revision == revision;

    private static bool TryMatchesReceiptWitness(OwnerSectionCensusSnapshot witness, JusticeSystem owner)
    {
        try
        {
            return witness != null
                && string.Equals(witness.SectionId, P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId,
                    StringComparison.Ordinal)
                && witness.SchemaVersion == P12CrimeJusticeCensusProvider.SchemaVersion
                && witness.Role == OwnerSectionRole.Required
                && ReferenceEquals(witness.OwnerInstanceIdentity, owner)
                && witness.Cardinality == 1 && witness.Revision == 0L
                && owner.P12P18ReceiptCensusRevision == 0L;
        }
        catch (Exception exception) when (exception is InvalidOperationException || exception is OverflowException)
        {
            return false;
        }
    }

    private static bool Fail(
        P12EJusticeSnapshotFailureCode code,
        string message,
        out P12EJusticeSnapshotFailure failure)
    {
        failure = P12EJusticeSnapshotFailure.Create(code, message);
        return false;
    }
}
