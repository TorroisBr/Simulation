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
/// here; Unity remains responsible for its Justice projections and
/// CrimeSocial appraisal adapter.
/// </summary>
internal sealed class DailyV1P12ECoreOwnerSourceSnapshots
{
    private DailyV1P12ECoreOwnerSourceSnapshots(
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EMilitaryOwnerSnapshot military,
        PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars,
        PersistentBattleOwnerSnapshot battles)
    {
        InstitutionOffice = institutionOffice;
        PropertyEstate = propertyEstate;
        Faction = faction;
        PoliticalClaims = politicalClaims;
        PoliticalSupport = politicalSupport;
        PoliticalDecisions = politicalDecisions;
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
    internal P12EMilitaryOwnerSnapshot Military { get; }
    internal PersistentConflictOwnerSnapshot Conflicts { get; }
    internal PersistentWarOwnerSnapshot Wars { get; }
    internal PersistentBattleOwnerSnapshot Battles { get; }

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
                institutionOffice, propertyEstate, faction, claims, support, decisions,
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
