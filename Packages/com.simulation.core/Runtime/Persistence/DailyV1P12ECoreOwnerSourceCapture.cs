using System;
using System.Collections.Generic;

internal enum DailyV1P12ECoreOwnerSourceCaptureFailure
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerEvidence,
    CaptureFailed,
    StaleBoundary
}

/// <summary>
/// Host-owned freshness proof for the live census boundary. Implementations
/// may validate a Core continuation-save operation or the legacy Unity P12
/// staging attempt; they do not choose owner instances or capture owner data.
/// </summary>
internal interface IDailyV1P12ECoreOwnerCaptureBoundary
{
    bool IsCaptureCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector);
}

/// <summary>
/// Detached snapshots for the P12-E owners whose authorities already live in
/// Core. Exact owners are selected from the token's census vector and captured
/// here; Unity remains responsible for the installed Justice projection
/// check and the CrimeSocial appraisal adapter.
/// </summary>
internal sealed class DailyV1P12ECoreOwnerSourceSnapshots
{
    private readonly DailyV1P12GContinuationSaveOperation operation;
    private readonly DailyV1P12GSourceBoundaryWitness witness;

    private DailyV1P12ECoreOwnerSourceSnapshots(
        DailyV1P12GContinuationSaveOperation operation,
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EJusticeRecordsOwnerSnapshot justiceRecords,
        P12EMilitaryOwnerSnapshot military,
        PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars,
        PersistentBattleOwnerSnapshot battles)
    {
        this.operation = operation;
        witness = operation?.SourceWitness;
        InstitutionOffice = institutionOffice;
        PropertyEstate = propertyEstate;
        Faction = faction;
        PoliticalClaims = politicalClaims;
        PoliticalSupport = politicalSupport;
        PoliticalDecisions = politicalDecisions;
        JusticeRecords = justiceRecords;
        Military = military;
        Conflicts = conflicts;
        Wars = wars;
        Battles = battles;
    }

    internal P12EInstitutionOfficeOwnerSnapshot InstitutionOffice { get; }
    internal PropertyEstateOwnerSnapshot PropertyEstate { get; }
    internal P12EFactionOwnerSnapshot Faction { get; }
    internal P12EPoliticalClaimOwnerSnapshot PoliticalClaims { get; }
    internal P12EPoliticalSupportOwnerSnapshot PoliticalSupport { get; }
    internal P12EPoliticalDecisionOwnerSnapshot PoliticalDecisions { get; }
    internal P12EJusticeRecordsOwnerSnapshot JusticeRecords { get; }
    internal P12EMilitaryOwnerSnapshot Military { get; }
    internal PersistentConflictOwnerSnapshot Conflicts { get; }
    internal PersistentWarOwnerSnapshot Wars { get; }
    internal PersistentBattleOwnerSnapshot Battles { get; }

    internal bool IsFor(DailyV1P12GContinuationSaveOperation candidate) =>
        candidate != null
        && ReferenceEquals(operation, candidate)
        && candidate.IsActive
        && ReferenceEquals(witness, candidate.SourceWitness)
        && ReferenceEquals(witness.Token, candidate.SourceWitness.Token)
        && ReferenceEquals(witness.TokenOwnerSections, candidate.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        DailyV1P12GContinuationSaveOperation continuationSaveOperation,
        IDailyV1P12ECoreOwnerCaptureBoundary legacyCaptureBoundary,
        out DailyV1P12ECoreOwnerSourceSnapshots snapshots,
        out DailyV1P12ECoreOwnerSourceCaptureFailure failure,
        out string diagnostic)
    {
        snapshots = null;
        failure = DailyV1P12ECoreOwnerSourceCaptureFailure.InvalidCaptureContext;
        diagnostic = null;
        if (token == null || ownerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, ownerSectionVector)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L
            || (continuationSaveOperation == null && legacyCaptureBoundary == null))
        {
            diagnostic = "P12-E Core owner capture requires the exact selected Daily-v1 token, vector, and current-boundary authority.";
            return false;
        }

        if (!IsCurrent(token, ownerSectionVector, continuationSaveOperation, legacyCaptureBoundary))
        {
            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.StaleBoundary;
            diagnostic = "The exact P12-E owner vector is no longer current for its capture attempt.";
            return false;
        }

        if (!TryFindRequiredOwner(ownerSectionVector,
                DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                1, out PersonStore persons)
            || !TryFindRequiredOwner(ownerSectionVector, InstitutionOfficeCensusProvider.InstitutionsSectionId,
                InstitutionOfficeCensusProvider.SchemaVersion, out InstitutionStore institutions)
            || !TryFindRequiredOwner(ownerSectionVector, InstitutionOfficeCensusProvider.OfficesSectionId,
                InstitutionOfficeCensusProvider.SchemaVersion, out OfficeStore offices)
            || !TryFindRequiredOwner(ownerSectionVector, PropertyOwnershipCensusProvider.OwnershipSectionId,
                PropertyOwnershipCensusProvider.SchemaVersion, out PropertyOwnershipStore properties)
            || !TryFindRequiredOwner(ownerSectionVector, EstateCensusProvider.SectionId,
                EstateCensusProvider.SchemaVersion, out EstateStore estates)
            || !TryFindRequiredOwner(ownerSectionVector, FactionStoreCensusProvider.FactionsSectionId,
                FactionStoreCensusProvider.SchemaVersion, out FactionStore factions)
            || !TryFindRequiredOwner(ownerSectionVector, ArmedForceStoreCensusProvider.ForcesSectionId,
                ArmedForceStoreCensusProvider.SchemaVersion, out ArmedForceStore armedForces)
            || !TryFindRequiredOwner(ownerSectionVector, ContingentManpowerCensusProvider.SectionId,
                ContingentManpowerCensusProvider.SchemaVersion, out ContingentManpowerStateStore manpower)
            || !TryFindRequiredOwner(ownerSectionVector, ArmedForceSpatialCensusProvider.SectionId,
                ArmedForceSpatialCensusProvider.SchemaVersion, out ArmedForceSpatialStateStore positions)
            || !TryFindRequiredOwner(ownerSectionVector, PersistentConflictCensusProvider.SectionId,
                PersistentConflictCensusProvider.SchemaVersion, out PersistentConflictStore conflicts)
            || !TryFindRequiredOwner(ownerSectionVector, PersistentWarCensusProvider.SectionId,
                PersistentWarCensusProvider.SchemaVersion, out PersistentWarStore wars)
            || !TryFindRequiredOwner(ownerSectionVector, PersistentBattleCensusProvider.SectionId,
                PersistentBattleCensusProvider.SchemaVersion, out PersistentBattleStore battles)
            || !TryFindRequiredOwner(ownerSectionVector, PoliticalClaimStoreCensusProvider.ClaimsSectionId,
                PoliticalClaimStoreCensusProvider.SchemaVersion, out PoliticalClaimStore politicalClaims))
        {
            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.InvalidOwnerEvidence;
            diagnostic = "A required P12-E Core owner is absent, duplicated, or differs from its exact census section identity.";
            return false;
        }

        var captureSource = new CoreCaptureSource(
            institutions, offices, persons,
            token, ownerSectionVector, continuationSaveOperation, legacyCaptureBoundary);
        try
        {
            P12EJusticeRecordsOwnerSnapshot justiceRecords = null;
            if (continuationSaveOperation != null
                && !TryCaptureCoreJusticeRecords(
                    continuationSaveOperation.Session?.OwnerGraph,
                    persons,
                    token,
                    ownerSectionVector,
                    out justiceRecords,
                    out P12EJusticeSnapshotFailure justiceFailure))
            {
                failure = DailyV1P12ECoreOwnerSourceCaptureFailure.CaptureFailed;
                diagnostic = "The selected Core Justice owner did not match its exact P12-E census snapshot contract: "
                    + (justiceFailure?.Code.ToString() ?? P12EJusticeSnapshotFailureCode.InvalidOwnerValues.ToString()) + ".";
                return false;
            }

            if (!P12EInstitutionOfficeOwnerSnapshot.TryCapture(captureSource, token, ownerSectionVector,
                    out P12EInstitutionOfficeOwnerSnapshot institutionOffice, out _)
                || !PropertyEstateOwnerSnapshot.TryCapture(properties, estates, token, ownerSectionVector,
                    out PropertyEstateOwnerSnapshot propertyEstate, out _)
                || !P12EFactionOwnerSnapshot.TryCapture(captureSource, factions, token, ownerSectionVector,
                    out P12EFactionOwnerSnapshot faction, out _)
                || !P12EPoliticalClaimOwnerSnapshot.TryCapture(captureSource, token, ownerSectionVector,
                    persons, properties, institutions, offices,
                    out P12EPoliticalClaimOwnerSnapshot claims, out _)
                || !P12EPoliticalSupportOwnerSnapshot.TryCapture(captureSource,
                    token, ownerSectionVector, out P12EPoliticalSupportOwnerSnapshot support, out _)
                || !P12EPoliticalDecisionOwnerSnapshot.TryCapture(captureSource,
                    token, ownerSectionVector, out P12EPoliticalDecisionOwnerSnapshot decisions, out _)
                || !P12EMilitaryOwnerSnapshot.TryCapture(armedForces, manpower, positions, token,
                    ownerSectionVector, out P12EMilitaryOwnerSnapshot military, out _)
                || !PersistentConflictOwnerSnapshot.TryCapture(conflicts, token, ownerSectionVector,
                    out PersistentConflictOwnerSnapshot conflictSnapshot, out _)
                || !PersistentWarOwnerSnapshot.TryCapture(wars, token, ownerSectionVector,
                    out PersistentWarOwnerSnapshot warSnapshot, out _)
                || !PersistentBattleOwnerSnapshot.TryCapture(battles, token, ownerSectionVector,
                    out PersistentBattleOwnerSnapshot battleSnapshot, out _))
            {
                failure = DailyV1P12ECoreOwnerSourceCaptureFailure.CaptureFailed;
                diagnostic = "A Core-owned P12-E snapshot rejected its exact source owner or census witness.";
                return false;
            }

            if (!IsCurrent(token, ownerSectionVector, continuationSaveOperation, legacyCaptureBoundary))
            {
                failure = DailyV1P12ECoreOwnerSourceCaptureFailure.StaleBoundary;
                diagnostic = "The P12-E Core owner vector changed while its detached snapshots were captured.";
                return false;
            }

            snapshots = new DailyV1P12ECoreOwnerSourceSnapshots(
                continuationSaveOperation,
                institutionOffice, propertyEstate, faction, claims, support, decisions, justiceRecords,
                military, conflictSnapshot, warSnapshot, battleSnapshot);
            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12ECoreOwnerSourceCaptureFailure.CaptureFailed;
            diagnostic = "A Core-owned P12-E source threw during snapshot capture: "
                + exception.GetType().Name + ".";
            return false;
        }
    }

    private static bool IsCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        DailyV1P12GContinuationSaveOperation continuationSaveOperation,
        IDailyV1P12ECoreOwnerCaptureBoundary legacyCaptureBoundary)
    {
        if (continuationSaveOperation != null)
            return continuationSaveOperation.IsActive
                && ReferenceEquals(continuationSaveOperation.SourceWitness?.Token, token)
                && ReferenceEquals(continuationSaveOperation.SourceWitness?.TokenOwnerSections, ownerSectionVector)
                && continuationSaveOperation.TryRevalidate(out _);

        return legacyCaptureBoundary != null
            && legacyCaptureBoundary.IsCaptureCurrent(token, ownerSectionVector);
    }

    private static bool TryFindRequiredOwner<TOwner>(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        out TOwner owner)
        where TOwner : class
    {
        owner = null;
        if (sections == null || string.IsNullOrWhiteSpace(sectionId) || schemaVersion <= 0) return false;
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (!string.Equals(section?.SectionId, sectionId, StringComparison.Ordinal)) continue;
            matches++;
            if (section.SchemaVersion != schemaVersion
                || section.Role != OwnerSectionRole.Required
                || section.Cardinality < 0
                || section.Revision < 0L
                || !(section.OwnerInstanceIdentity is TOwner typedOwner))
                return false;
            owner = typedOwner;
        }
        if (matches != 1)
        {
            owner = null;
            return false;
        }
        return true;
    }

    private static bool TryCaptureCoreJusticeRecords(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore persons,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        out P12EJusticeRecordsOwnerSnapshot snapshot,
        out P12EJusticeSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EJusticeSnapshotFailure.Create(
            P12EJusticeSnapshotFailureCode.InvalidCaptureContext,
            "An exact Core Justice owner graph and completed capture token are required.");
        DailyV1JusticeRecordsOwner owner = graph?.JusticeRecordsOwner;
        if (owner == null || persons == null || token == null || ownerSectionVector == null
            || !ReferenceEquals(graph.PersonStore, persons)
            || !ReferenceEquals(token.OwnerSections, ownerSectionVector)
            || token.AbsoluteDay < 0L || token.CompletedCoreSequence <= 0L
            || token.MutationEpoch < 0L)
            return false;

        if (!TryFindUniqueSection(ownerSectionVector,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                out OwnerSectionCensusSnapshot justiceWitness)
            || !TryFindUniqueSection(ownerSectionVector,
                DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
                out OwnerSectionCensusSnapshot receiptWitness)
            || justiceWitness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
            || justiceWitness.Role != OwnerSectionRole.Required
            || justiceWitness.Cardinality < 0 || justiceWitness.Revision < 0L
            || receiptWitness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
            || receiptWitness.Role != OwnerSectionRole.Required
            || receiptWitness.Cardinality != 1 || receiptWitness.Revision != 0L
            || !ReferenceEquals(receiptWitness.OwnerInstanceIdentity, justiceWitness.OwnerInstanceIdentity))
        {
            failure = P12EJusticeSnapshotFailure.Create(
                P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                "The exact Justice owner and its required exact-zero receipt witness are required.");
            return false;
        }

        IReadOnlyList<DailyV1JusticeWantedRecordOwner> wanted = owner.WantedRecords;
        IReadOnlyList<DailyV1JusticeSentenceOwner> sentences = owner.Sentences;
        long capturedRevision = owner.Revision;
        if (wanted == null || sentences == null || capturedRevision < 0L)
        {
            failure = P12EJusticeSnapshotFailure.Create(
                P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
                "The exact Core Justice rows and nonnegative local revision are required.");
            return false;
        }

        int recordCount;
        try { recordCount = checked(wanted.Count + sentences.Count); }
        catch (OverflowException)
        {
            failure = P12EJusticeSnapshotFailure.Create(
                P12EJusticeSnapshotFailureCode.InvalidCardinality,
                "Combined Justice row cardinality overflowed.");
            return false;
        }
        if (justiceWitness.Cardinality != recordCount || justiceWitness.Revision != capturedRevision)
        {
            failure = P12EJusticeSnapshotFailure.Create(
                P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                "The P12-B Justice witness does not match the exact Core owner cardinality and revision.");
            return false;
        }

        try
        {
            List<P12EJusticeWantedSnapshotRow> wantedRows =
                new List<P12EJusticeWantedSnapshotRow>(wanted.Count);
            HashSet<DailyV1JusticeWantedRecordOwner> distinctWanted =
                new HashSet<DailyV1JusticeWantedRecordOwner>();
            for (int index = 0; index < wanted.Count; index++)
            {
                DailyV1JusticeWantedRecordOwner row = wanted[index];
                if (row == null || !distinctWanted.Add(row)
                    || !TryResolveCoreJusticeRoots(graph, persons, row.TargetRuntimeId,
                        row.CityRuntimeId, out string personIdValue))
                {
                    failure = P12EJusticeSnapshotFailure.Create(
                        P12EJusticeSnapshotFailureCode.InvalidReference,
                        "A Core Justice wanted row has a duplicate owner or unresolved exact NPC, Person, or City binding.");
                    return false;
                }

                wantedRows.Add(new P12EJusticeWantedSnapshotRow(
                    index, row.TargetRuntimeId, personIdValue, row.CityRuntimeId,
                    row.Bounty, row.SentenceDays, row.IsResolved));
            }

            List<P12EJusticeSentenceSnapshotRow> sentenceRows =
                new List<P12EJusticeSentenceSnapshotRow>(sentences.Count);
            HashSet<DailyV1JusticeSentenceOwner> distinctSentences =
                new HashSet<DailyV1JusticeSentenceOwner>();
            for (int index = 0; index < sentences.Count; index++)
            {
                DailyV1JusticeSentenceOwner row = sentences[index];
                int warrantOrdinal = -1;
                for (int wantedIndex = 0; wantedIndex < wanted.Count; wantedIndex++)
                    if (ReferenceEquals(wanted[wantedIndex], row?.Warrant))
                    {
                        warrantOrdinal = wantedIndex;
                        break;
                    }

                if (row == null || !distinctSentences.Add(row) || warrantOrdinal < 0
                    || !TryResolveCoreJusticeRoots(graph, persons, row.TargetRuntimeId,
                        row.CityRuntimeId, out string personIdValue))
                {
                    failure = P12EJusticeSnapshotFailure.Create(
                        P12EJusticeSnapshotFailureCode.InvalidRelation,
                        "A Core Justice sentence row has a duplicate owner, unresolved root binding, or non-exact warrant owner.");
                    return false;
                }

                sentenceRows.Add(new P12EJusticeSentenceSnapshotRow(
                    index, row.TargetRuntimeId, personIdValue, row.CityRuntimeId,
                    warrantOrdinal, row.RemainingDays, row.FailedEscapeAttempts, row.WasArrestedToday));
            }

            P12EJusticeRecordsOwnerSnapshot candidate = new P12EJusticeRecordsOwnerSnapshot(
                token.AbsoluteDay,
                new P12EJusticeRecordsSnapshotSection(
                    P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                    P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion,
                    recordCount,
                    capturedRevision,
                    wantedRows,
                    sentenceRows));
            if (!candidate.TryValidate(out failure)
                || owner.Revision != capturedRevision
                || !TryFindUniqueSection(ownerSectionVector,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                    out OwnerSectionCensusSnapshot finalJusticeWitness)
                || !MatchesJusticeWitness(finalJusticeWitness, justiceWitness.OwnerInstanceIdentity,
                    recordCount, capturedRevision)
                || !TryFindUniqueSection(ownerSectionVector,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
                    out OwnerSectionCensusSnapshot finalReceiptWitness)
                || !MatchesExactZeroReceiptWitness(finalReceiptWitness, justiceWitness.OwnerInstanceIdentity))
            {
                snapshot = null;
                if (failure?.Code == P12EJusticeSnapshotFailureCode.None)
                    failure = P12EJusticeSnapshotFailure.Create(
                        P12EJusticeSnapshotFailureCode.InvalidOwnerSectionVector,
                        "Justice owner values or the exact-zero receipt witness changed during capture.");
                return false;
            }

            snapshot = candidate;
            failure = P12EJusticeSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            snapshot = null;
            failure = P12EJusticeSnapshotFailure.Create(
                P12EJusticeSnapshotFailureCode.InvalidOwnerValues,
                "Core Justice snapshot capture failed: " + exception.GetType().Name + ".");
            return false;
        }
    }

    private static bool TryResolveCoreJusticeRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore persons,
        string npcRuntimeId,
        string cityRuntimeId,
        out string personIdValue)
    {
        personIdValue = null;
        if (graph == null || persons == null
            || string.IsNullOrWhiteSpace(npcRuntimeId)
            || string.IsNullOrWhiteSpace(cityRuntimeId))
            return false;

        DailyV1NpcRuntimeOwners matchedNpc = null;
        int npcMatches = 0;
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
            if (npc != null && string.Equals(npc.RuntimeId, npcRuntimeId, StringComparison.Ordinal))
            {
                matchedNpc = npc;
                npcMatches++;
            }
        if (npcMatches != 1 || !graph.ContainsExactNpcOwner(matchedNpc)) return false;

        int cityMatches = 0;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city != null && string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal)
                && graph.ContainsExactCityOwner(city))
                cityMatches++;
        if (cityMatches != 1) return false;

        if (!persons.TryGetByMaterializedNpcRuntimeId(npcRuntimeId, out PersonRuntime person))
            return true;
        if (person?.PersonId == null || !person.IsMaterialized
            || !string.Equals(person.MaterializedNpcRuntimeId, npcRuntimeId, StringComparison.Ordinal)
            || !persons.TryGet(person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, person))
            return false;

        personIdValue = person.PersonId.Value;
        return !string.IsNullOrWhiteSpace(personIdValue);
    }

    private static bool TryFindUniqueSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        out OwnerSectionCensusSnapshot match)
    {
        match = null;
        if (sections == null || string.IsNullOrWhiteSpace(sectionId)) return false;
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
            if (string.Equals(section?.SectionId, sectionId, StringComparison.Ordinal))
            {
                match = section;
                matches++;
            }
        return matches == 1;
    }

    private static bool MatchesJusticeWitness(
        OwnerSectionCensusSnapshot witness,
        object exactOwnerIdentity,
        int cardinality,
        long revision) => witness != null
        && string.Equals(witness.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, StringComparison.Ordinal)
        && witness.SchemaVersion == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
        && witness.Role == OwnerSectionRole.Required
        && ReferenceEquals(witness.OwnerInstanceIdentity, exactOwnerIdentity)
        && witness.Cardinality == cardinality
        && witness.Revision == revision;

    private static bool MatchesExactZeroReceiptWitness(
        OwnerSectionCensusSnapshot witness,
        object exactOwnerIdentity) => witness != null
        && string.Equals(witness.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId, StringComparison.Ordinal)
        && witness.SchemaVersion == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
        && witness.Role == OwnerSectionRole.Required
        && ReferenceEquals(witness.OwnerInstanceIdentity, exactOwnerIdentity)
        && witness.Cardinality == 1
        && witness.Revision == 0L;

    private sealed class CoreCaptureSource :
        IP12EInstitutionOfficeCaptureSource,
        IP12EFactionCaptureSource,
        IP12EPoliticalClaimCaptureSource,
        IP12EPoliticalSupportCaptureSource,
        IP12EPoliticalDecisionCaptureSource
    {
        private readonly DailyCaptureEligibilityToken token;
        private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector;
        private readonly DailyV1P12GContinuationSaveOperation continuationSaveOperation;
        private readonly IDailyV1P12ECoreOwnerCaptureBoundary legacyCaptureBoundary;

        internal CoreCaptureSource(
            InstitutionStore institutions,
            OfficeStore offices,
            PersonStore persons,
            DailyCaptureEligibilityToken token,
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
            DailyV1P12GContinuationSaveOperation continuationSaveOperation,
            IDailyV1P12ECoreOwnerCaptureBoundary legacyCaptureBoundary)
        {
            InstitutionOwner = institutions;
            OfficeOwner = offices;
            PersonOwner = persons;
            this.token = token;
            this.ownerSectionVector = ownerSectionVector;
            this.continuationSaveOperation = continuationSaveOperation;
            this.legacyCaptureBoundary = legacyCaptureBoundary;
        }

        public InstitutionStore InstitutionOwner { get; }
        public OfficeStore OfficeOwner { get; }
        public PersonStore PersonOwner { get; }

        public bool IsCaptureCurrent(
            DailyCaptureEligibilityToken candidateToken,
            IReadOnlyList<OwnerSectionCensusSnapshot> candidateOwnerSectionVector) =>
            ReferenceEquals(token, candidateToken)
            && ReferenceEquals(ownerSectionVector, candidateOwnerSectionVector)
            && IsCurrent(token, ownerSectionVector,
                continuationSaveOperation, legacyCaptureBoundary);
    }
}

/// <summary>
/// Encodes sections for the P12-E owners captured from the exact Core owners
/// retained by the active continuation-save operation.
/// </summary>
internal static class DailyV1P12ECoreOwnerSectionEncoder
{
    internal static bool TryEncode(
        DailyV1P12ECoreOwnerSourceSnapshots snapshots,
        long capturedAbsoluteDay,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (snapshots == null || capturedAbsoluteDay < 0L)
            return false;

        if (!P12EInstitutionOfficeOwnerCodec.TryEncode(snapshots.InstitutionOffice,
                out P12AOwnerSection institutions, out P12AOwnerSection offices,
                out P12AOwnerSection incumbencies, out P12AOwnerSection tenures, out result)
            || !P12EPropertyEstateOwnerCodec.TryEncode(snapshots.PropertyEstate,
                capturedAbsoluteDay, out P12AOwnerSection ownership,
                out P12AOwnerSection transfers, out P12AOwnerSection estates, out result)
            || !P12EFactionOwnerCodec.TryEncode(snapshots.Faction, capturedAbsoluteDay,
                out P12AOwnerSection factions, out P12AOwnerSection affiliations, out result)
            || !P12EPoliticalClaimOwnerCodec.TryEncode(snapshots.PoliticalClaims,
                capturedAbsoluteDay, out P12AOwnerSection claims,
                out P12AOwnerSection recognitions, out result)
            || !P12EPoliticalSupportOwnerCodec.TryEncode(snapshots.PoliticalSupport,
                out P12AOwnerSection politicalSupport, out result)
            || !P12EPoliticalDecisionOwnerCodec.TryEncode(snapshots.PoliticalDecisions,
                out P12AOwnerSection politicalDecisions, out result)
            || !P12EMilitaryOwnerCodec.TryEncode(snapshots.Military,
                out IReadOnlyList<P12AOwnerSection> military, out result)
            || !P12EPersistentConflictWarBattleOwnerCodec.TryEncode(
                snapshots.Conflicts, snapshots.Wars, snapshots.Battles,
                out IReadOnlyList<P12AOwnerSection> conflicts, out result))
            return false;

        P12AOwnerSection justice = null;
        if (snapshots.JusticeRecords != null
            && !P12EJusticeRecordsOwnerCodec.TryEncode(
                snapshots.JusticeRecords, out justice, out result))
            return false;

        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>
        {
            institutions, offices, incumbencies, tenures,
            ownership, transfers, estates,
            factions, affiliations,
            claims, recognitions,
            politicalSupport, politicalDecisions
        };
        encoded.AddRange(military);
        encoded.AddRange(conflicts);
        if (justice != null) encoded.Add(justice);
        encoded.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left?.Id, right?.Id));

        for (int index = 0; index < encoded.Count; index++)
        {
            P12AOwnerSection section = encoded[index];
            if (section == null || string.IsNullOrWhiteSpace(section.Id)
                || section.Role != OwnerSectionRole.Required
                || (index > 0 && string.Equals(encoded[index - 1].Id,
                    section.Id, StringComparison.Ordinal)))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection,
                    section?.Id);
                return false;
            }
        }

        sections = encoded.AsReadOnly();
        result = null;
        return true;
    }
}
