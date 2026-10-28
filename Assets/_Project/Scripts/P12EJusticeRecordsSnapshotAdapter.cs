using System;
using System.Collections.Generic;

/// <summary>Unity projection and staged-owner boundary for the Core-owned Justice value snapshot.</summary>
internal static class P12EJusticeRecordsSnapshotAdapter
{
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
        out P12EJusticeSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        snapshot = null;
        failure = P12EJusticeSnapshotFailure.Create(
            P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its owner-section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)
            || !DailyCaptureStagingAttempt.IsCaptureCurrent(
                runtime, token, sharedOwnerSectionVector, stagingAttempt))
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
                "The exact Justice P18 receipt witness must match the current owner and its current revision.",
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
                P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion,
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
            || !DailyCaptureStagingAttempt.IsCaptureCurrent(
                runtime, token, sharedOwnerSectionVector, stagingAttempt))
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

    internal static bool TryStage(
        P12EJusticeRecordsOwnerSnapshot snapshot,
        IReadOnlyList<CityRuntime> stagedCities,
        IReadOnlyList<NpcRuntime> stagedNpcs,
        PersonStore stagedPersons,
        DailyV1JusticeRecordsOwner selectedCoreOwner,
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        out JusticeSystem staged,
        out P12EJusticeSnapshotFailure failure,
        P12AJusticeReceiptOwnerSnapshot p12aReceiptSnapshot = null)
    {
        staged = null;
        if (snapshot == null)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
                "A Justice owner snapshot is required.", out failure);
        if (!snapshot.TryValidate(out failure)) return false;
        if (stagedCities == null || stagedNpcs == null || stagedPersons == null
            || freeStatus == null || wantedStatus == null || arrestedStatus == null || hiddenStatus == null)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                "Exact staged City, NPC, PersonStore, and Justice configuration roots are required.", out failure);

        if (!TryBuildRootMaps(stagedCities, stagedNpcs,
                out Dictionary<string, CityRuntime> citiesById,
                out Dictionary<string, NpcRuntime> npcsById))
            return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                "Staged root identities are missing or ambiguous.", out failure);
        if (selectedCoreOwner == null
            || selectedCoreOwner.WantedRecords == null || selectedCoreOwner.Sentences == null
            || selectedCoreOwner.WantedRecords.Count != snapshot.Records.WantedRows.Count
            || selectedCoreOwner.Sentences.Count != snapshot.Records.SentenceRows.Count)
            return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                "The exact Core Justice owner staged from this snapshot is required.", out failure);

        try
        {
            List<WantedRecordRuntime> wanted = new List<WantedRecordRuntime>(snapshot.Records.WantedRows.Count);
            for (int i = 0; i < snapshot.Records.WantedRows.Count; i++)
            {
                P12EJusticeWantedSnapshotRow row = snapshot.Records.WantedRows[i];
                DailyV1JusticeWantedRecordOwner coreRow = selectedCoreOwner.WantedRecords[i];
                if (!npcsById.TryGetValue(row.TargetRuntimeId, out NpcRuntime target)
                    || !citiesById.TryGetValue(row.CityRuntimeId, out CityRuntime city)
                    || !TryValidateTargetPersonBinding(row.TargetRuntimeId, row.TargetPersonIdValue,
                        target, stagedPersons)
                    || !Matches(row, coreRow))
                    return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                        "A wanted record target or City does not resolve to the exact staged roots.", out failure);

                if (!WantedRecordRuntime.TryCreateFromCoreOwner(target, city, coreRow,
                        out WantedRecordRuntime warrant))
                    return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                        "The private wanted-record factory rejected a validated row.", out failure);
                wanted.Add(warrant);
            }

            List<PrisonSentenceRuntime> sentences =
                new List<PrisonSentenceRuntime>(snapshot.Records.SentenceRows.Count);
            for (int i = 0; i < snapshot.Records.SentenceRows.Count; i++)
            {
                P12EJusticeSentenceSnapshotRow row = snapshot.Records.SentenceRows[i];
                DailyV1JusticeSentenceOwner coreRow = selectedCoreOwner.Sentences[i];
                if (!npcsById.TryGetValue(row.TargetRuntimeId, out NpcRuntime target)
                    || !citiesById.TryGetValue(row.CityRuntimeId, out CityRuntime city)
                    || !TryValidateTargetPersonBinding(row.TargetRuntimeId, row.TargetPersonIdValue,
                        target, stagedPersons)
                    || row.WarrantOrdinal < 0 || row.WarrantOrdinal >= wanted.Count
                    || !Matches(row, coreRow))
                    return Fail(P12EJusticeSnapshotFailureCode.InvalidReference,
                        "A sentence target, City, Person binding, or warrant ordinal is invalid.", out failure);

                WantedRecordRuntime warrant = wanted[row.WarrantOrdinal];
                if (!ReferenceEquals(target, warrant.Target) || !ReferenceEquals(city, warrant.City)
                    || !ReferenceEquals(coreRow.Warrant, warrant.CoreOwner))
                    return Fail(P12EJusticeSnapshotFailureCode.InvalidRelation,
                        "A sentence must reference the exact target and City objects of its warrant.", out failure);
                if (!PrisonSentenceRuntime.TryCreateSelectedProjection(
                        target, city, warrant, coreRow, out PrisonSentenceRuntime sentence))
                    return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                        "The private prison-sentence factory rejected a validated row.", out failure);
                sentences.Add(sentence);
            }

            if (!JusticeSystem.TryCreateFromP12ECoreOwner(
                    freeStatus, wantedStatus, arrestedStatus, hiddenStatus,
                    domainEventRecorder, logger, selectedCoreOwner, wanted, sentences,
                    out JusticeSystem candidate, p12aReceiptSnapshot))
                return Fail(P12EJusticeSnapshotFailureCode.StageFailed,
                    "The JusticeSystem projection rejected the exact staged Core owner rows.", out failure);

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

    private static bool Matches(
        P12EJusticeWantedSnapshotRow snapshot,
        DailyV1JusticeWantedRecordOwner owner) => owner != null
        && string.Equals(snapshot.TargetRuntimeId, owner.TargetRuntimeId, StringComparison.Ordinal)
        && string.Equals(snapshot.CityRuntimeId, owner.CityRuntimeId, StringComparison.Ordinal)
        && snapshot.Bounty.Equals(owner.Bounty)
        && snapshot.SentenceDays == owner.SentenceDays
        && snapshot.IsResolved == owner.IsResolved;

    private static bool Matches(
        P12EJusticeSentenceSnapshotRow snapshot,
        DailyV1JusticeSentenceOwner owner) => owner != null
        && string.Equals(snapshot.TargetRuntimeId, owner.TargetRuntimeId, StringComparison.Ordinal)
        && string.Equals(snapshot.CityRuntimeId, owner.CityRuntimeId, StringComparison.Ordinal)
        && snapshot.RemainingDays == owner.RemainingDays
        && snapshot.FailedEscapeAttempts == owner.FailedEscapeAttempts
        && snapshot.WasArrestedToday == owner.WasArrestedToday;

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
                && witness.Cardinality == 1
                && witness.Revision == 0L
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
