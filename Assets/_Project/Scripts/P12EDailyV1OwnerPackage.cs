using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EDailyV1OwnerPackageFailure
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerEvidence,
    CaptureFailed,
    StaleBoundary,
    StageFailed,
    InvalidUnresolvedBinding
}

/// <summary>
/// Exact private roots and already-admitted transient inputs for one P12-E
/// staging attempt. This carries references only; it does not resolve or
/// substitute any owner or configuration value.
/// </summary>
internal sealed class P12EDailyV1OwnerStagingContext
{
    internal DailyCaptureStagingAttempt StagingAttempt { get; }
    internal P12CStagedContinuationRoot P12CRoots { get; }
    internal P12DDailyV1OwnerPackage P12DPackage { get; }
    internal SimulationTime StagedSimulationTime { get; }
    internal NpcStatusData FreeStatus { get; }
    internal NpcStatusData WantedStatus { get; }
    internal NpcStatusData ArrestedStatus { get; }
    internal NpcStatusData HiddenStatus { get; }
    internal DomainEventRecorder DomainEventRecorder { get; }
    internal SimulationLogger Logger { get; }
    internal DailyV1Application Application { get; }
    internal DailyV1ActiveRuntimeSession ExpectedSession { get; }
    internal DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation ReplacementOperation { get; }

    internal P12EDailyV1OwnerStagingContext(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CStagedContinuationRoot p12cRoots,
        P12DDailyV1OwnerPackage p12dPackage,
        SimulationTime stagedSimulationTime,
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        DailyV1Application application = null,
        DailyV1ActiveRuntimeSession expectedSession = null,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation = null)
    {
        StagingAttempt = stagingAttempt;
        P12CRoots = p12cRoots;
        P12DPackage = p12dPackage;
        StagedSimulationTime = stagedSimulationTime;
        FreeStatus = freeStatus;
        WantedStatus = wantedStatus;
        ArrestedStatus = arrestedStatus;
        HiddenStatus = hiddenStatus;
        DomainEventRecorder = domainEventRecorder;
        Logger = logger;
        Application = application;
        ExpectedSession = expectedSession;
        ReplacementOperation = replacementOperation;
    }
}

/// <summary>Typed E-to-F evidence that remains unresolved until P12-G.</summary>
internal sealed class P12EUnresolvedPoliticalKnowledgeBinding
{
    internal PoliticalDecisionId DecisionId { get; }
    internal IReadOnlyList<string> KnowledgeReferences { get; }
    internal long ExpectedKnowledgeRevision { get; }
    internal long ExpectedWorldRevision { get; }

    internal P12EUnresolvedPoliticalKnowledgeBinding(P12EPoliticalDecisionSnapshotRow row)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        DecisionId = new PoliticalDecisionId(row.DecisionId);
        KnowledgeReferences = new ReadOnlyCollection<string>(
            new List<string>(row.KnowledgeReferences ?? Array.Empty<string>()));
        ExpectedKnowledgeRevision = row.ExpectedKnowledgeRevision;
        ExpectedWorldRevision = row.ExpectedWorldRevision;
    }
}

/// <summary>
/// Private, unpublished composition of every current Daily-v1 P12-E owner.
/// P12-G owns cross-phase resolution and final publication.
/// </summary>
internal sealed class P12EDailyV1OwnerPackage
{
    internal DailyCaptureStagingAttempt StagingAttempt { get; }
    internal WorldId WorldId { get; }
    internal InstitutionStore Institutions { get; }
    internal OfficeStore Offices { get; }
    internal PropertyOwnershipStore PropertyOwnership { get; }
    internal EstateStore Estates { get; }
    internal FactionStore Factions { get; }
    internal PoliticalClaimStore PoliticalClaims { get; }
    internal PoliticalSupportStore PoliticalSupport { get; }
    internal PoliticalDecisionStore PoliticalDecisions { get; }
    internal ArmedForceStore ArmedForces { get; }
    internal ContingentManpowerStateStore ContingentManpower { get; }
    internal ArmedForceSpatialStateStore ArmedForcePositions { get; }
    internal PersistentConflictStore Conflicts { get; }
    internal PersistentWarStore Wars { get; }
    internal PersistentBattleStore Battles { get; }
    internal JusticeSystem Justice { get; }
    internal CrimeSocialAppraisalWorldState CrimeSocialAppraisal { get; }
    internal P12ACrimeReceiptOwnerSnapshot CrimeReceiptSnapshot { get; }
    internal P12AJusticeReceiptOwnerSnapshot JusticeReceiptSnapshot { get; }
    internal P12EInstitutionOfficeOwnerSnapshot InstitutionOfficeSnapshot { get; }
    internal PropertyEstateOwnerSnapshot PropertyEstateSnapshot { get; }
    internal P12EFactionOwnerSnapshot FactionSnapshot { get; }
    internal P12EPoliticalClaimOwnerSnapshot PoliticalClaimSnapshot { get; }
    internal P12EPoliticalSupportOwnerSnapshot PoliticalSupportSnapshot { get; }
    internal P12EPoliticalDecisionOwnerSnapshot PoliticalDecisionSnapshot { get; }
    internal P12EMilitaryOwnerSnapshot MilitarySnapshot { get; }
    internal PersistentConflictOwnerSnapshot ConflictSnapshot { get; }
    internal PersistentWarOwnerSnapshot WarSnapshot { get; }
    internal PersistentBattleOwnerSnapshot BattleSnapshot { get; }
    internal P12EJusticeRecordsOwnerSnapshot JusticeSnapshot { get; }
    internal P12ECrimeSocialAppraisalOwnerSnapshot CrimeSocialSnapshot { get; }
    internal IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> UnresolvedPoliticalKnowledgeBindings { get; }

    private P12EDailyV1OwnerPackage(
        DailyCaptureStagingAttempt stagingAttempt,
        WorldId worldId,
        InstitutionStore institutions,
        OfficeStore offices,
        PropertyOwnershipStore propertyOwnership,
        EstateStore estates,
        FactionStore factions,
        PoliticalClaimStore politicalClaims,
        PoliticalSupportStore politicalSupport,
        PoliticalDecisionStore politicalDecisions,
        ArmedForceStore armedForces,
        ContingentManpowerStateStore contingentManpower,
        ArmedForceSpatialStateStore armedForcePositions,
        PersistentConflictStore conflicts,
        PersistentWarStore wars,
        PersistentBattleStore battles,
        JusticeSystem justice,
        CrimeSocialAppraisalWorldState crimeSocialAppraisal,
        P12ACrimeReceiptOwnerSnapshot crimeReceiptSnapshot,
        P12AJusticeReceiptOwnerSnapshot justiceReceiptSnapshot,
        P12EInstitutionOfficeOwnerSnapshot institutionOfficeSnapshot,
        PropertyEstateOwnerSnapshot propertyEstateSnapshot,
        P12EFactionOwnerSnapshot factionSnapshot,
        P12EPoliticalClaimOwnerSnapshot politicalClaimSnapshot,
        P12EPoliticalSupportOwnerSnapshot politicalSupportSnapshot,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisionSnapshot,
        P12EMilitaryOwnerSnapshot militarySnapshot,
        PersistentConflictOwnerSnapshot conflictSnapshot,
        PersistentWarOwnerSnapshot warSnapshot,
        PersistentBattleOwnerSnapshot battleSnapshot,
        P12EJusticeRecordsOwnerSnapshot justiceSnapshot,
        P12ECrimeSocialAppraisalOwnerSnapshot crimeSocialSnapshot,
        IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> unresolvedBindings)
    {
        StagingAttempt = stagingAttempt;
        WorldId = worldId;
        Institutions = institutions;
        Offices = offices;
        PropertyOwnership = propertyOwnership;
        Estates = estates;
        Factions = factions;
        PoliticalClaims = politicalClaims;
        PoliticalSupport = politicalSupport;
        PoliticalDecisions = politicalDecisions;
        ArmedForces = armedForces;
        ContingentManpower = contingentManpower;
        ArmedForcePositions = armedForcePositions;
        Conflicts = conflicts;
        Wars = wars;
        Battles = battles;
        Justice = justice;
        CrimeSocialAppraisal = crimeSocialAppraisal;
        CrimeReceiptSnapshot = crimeReceiptSnapshot;
        JusticeReceiptSnapshot = justiceReceiptSnapshot ?? throw new ArgumentNullException(nameof(justiceReceiptSnapshot));
        InstitutionOfficeSnapshot = institutionOfficeSnapshot ?? throw new ArgumentNullException(nameof(institutionOfficeSnapshot));
        PropertyEstateSnapshot = propertyEstateSnapshot ?? throw new ArgumentNullException(nameof(propertyEstateSnapshot));
        FactionSnapshot = factionSnapshot ?? throw new ArgumentNullException(nameof(factionSnapshot));
        PoliticalClaimSnapshot = politicalClaimSnapshot ?? throw new ArgumentNullException(nameof(politicalClaimSnapshot));
        PoliticalSupportSnapshot = politicalSupportSnapshot ?? throw new ArgumentNullException(nameof(politicalSupportSnapshot));
        PoliticalDecisionSnapshot = politicalDecisionSnapshot ?? throw new ArgumentNullException(nameof(politicalDecisionSnapshot));
        MilitarySnapshot = militarySnapshot ?? throw new ArgumentNullException(nameof(militarySnapshot));
        ConflictSnapshot = conflictSnapshot ?? throw new ArgumentNullException(nameof(conflictSnapshot));
        WarSnapshot = warSnapshot ?? throw new ArgumentNullException(nameof(warSnapshot));
        BattleSnapshot = battleSnapshot ?? throw new ArgumentNullException(nameof(battleSnapshot));
        JusticeSnapshot = justiceSnapshot ?? throw new ArgumentNullException(nameof(justiceSnapshot));
        CrimeSocialSnapshot = crimeSocialSnapshot ?? throw new ArgumentNullException(nameof(crimeSocialSnapshot));
        UnresolvedPoliticalKnowledgeBindings = new ReadOnlyCollection<P12EUnresolvedPoliticalKnowledgeBinding>(
            new List<P12EUnresolvedPoliticalKnowledgeBinding>(unresolvedBindings));
    }

    internal static bool TryCaptureAndStage(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken exactCompletedToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactTokenOwnerSections,
        P12EDailyV1OwnerStagingContext context,
        out P12EDailyV1OwnerPackage package,
        out P12EDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        Func<PropertyEstateOwnerSnapshot, PropertyEstateOwnerSnapshot> propertyEstateSnapshotOverrideForTest = null,
        Func<P12EPoliticalClaimOwnerSnapshot, P12EPoliticalClaimOwnerSnapshot> politicalClaimSnapshotOverrideForTest = null,
        Func<P12EPoliticalSupportOwnerSnapshot, P12EPoliticalSupportOwnerSnapshot> politicalSupportSnapshotOverrideForTest = null,
        Func<P12EFactionOwnerSnapshot, P12EFactionOwnerSnapshot> factionSnapshotOverrideForTest = null)
    {
        package = null;
        failure = P12EDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (!TryValidateContext(sourceRuntime, exactCompletedToken, exactTokenOwnerSections, context))
            return false;

        try
        {
            if (!TryFindOptionalOwner(exactTokenOwnerSections,
                    P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId,
                    P12CrimeJusticeCensusProvider.SchemaVersion,
                    out CrimeSystem crimeReceiptOwner,
                    out long crimeReceiptRevision))
            {
                failure = P12EDailyV1OwnerPackageFailure.InvalidOwnerEvidence;
                return false;
            }

            if (!TryFindRequiredOwner(exactTokenOwnerSections,
                    PropertyOwnershipCensusProvider.OwnershipSectionId,
                    PropertyOwnershipCensusProvider.SchemaVersion,
                    out PropertyOwnershipStore propertyOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    EstateCensusProvider.SectionId,
                    EstateCensusProvider.SchemaVersion,
                    out EstateStore estateOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    FactionStoreCensusProvider.FactionsSectionId,
                    FactionStoreCensusProvider.SchemaVersion,
                    out FactionStore factionOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    ArmedForceStoreCensusProvider.ForcesSectionId,
                    ArmedForceStoreCensusProvider.SchemaVersion,
                    out ArmedForceStore armedForceOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    ContingentManpowerCensusProvider.SectionId,
                    ContingentManpowerCensusProvider.SchemaVersion,
                    out ContingentManpowerStateStore manpowerOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    ArmedForceSpatialCensusProvider.SectionId,
                    ArmedForceSpatialCensusProvider.SchemaVersion,
                    out ArmedForceSpatialStateStore positionOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    PersistentConflictCensusProvider.SectionId,
                    PersistentConflictCensusProvider.SchemaVersion,
                    out PersistentConflictStore conflictOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    PersistentWarCensusProvider.SectionId,
                    PersistentWarCensusProvider.SchemaVersion,
                    out PersistentWarStore warOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    PersistentBattleCensusProvider.SectionId,
                    PersistentBattleCensusProvider.SchemaVersion,
                    out PersistentBattleStore battleOwner)
                || !TryFindRequiredOwner(exactTokenOwnerSections,
                    P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId,
                    P12CrimeJusticeCensusProvider.SchemaVersion,
                    out JusticeSystem justiceOwner))
            {
                failure = P12EDailyV1OwnerPackageFailure.InvalidOwnerEvidence;
                return false;
            }

            P12ACrimeReceiptOwnerSnapshot crimeReceiptSnapshot = null;
            if (crimeReceiptOwner != null
                && (!crimeReceiptOwner.TryCaptureP12AReceiptOwnerSnapshot(
                        out crimeReceiptSnapshot)
                    || crimeReceiptSnapshot.CensusRevision != crimeReceiptRevision))
            {
                failure = P12EDailyV1OwnerPackageFailure.CaptureFailed;
                return false;
            }

            if (!P12EInstitutionOfficeCaptureAdapter.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EInstitutionOfficeOwnerSnapshot institutionOfficeSnapshot, out _, context.StagingAttempt)
                || !PropertyEstateOwnerSnapshot.TryCapture(
                    propertyOwner, estateOwner, exactCompletedToken, exactTokenOwnerSections,
                    out PropertyEstateOwnerSnapshot propertyEstateSnapshot, out _)
                || !P12EFactionCaptureAdapter.TryCapture(
                    sourceRuntime, factionOwner, exactCompletedToken, exactTokenOwnerSections,
                    out P12EFactionOwnerSnapshot factionSnapshot, out _, context.StagingAttempt)
                || !P12EPoliticalClaimCaptureAdapter.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EPoliticalClaimOwnerSnapshot claimSnapshot, out _, context.StagingAttempt)
                || !P12EPoliticalSupportCaptureAdapter.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EPoliticalSupportOwnerSnapshot supportSnapshot, out _, context.StagingAttempt)
                || !P12EPoliticalDecisionCaptureAdapter.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EPoliticalDecisionOwnerSnapshot decisionSnapshot, out _, context.StagingAttempt)
                || !P12EMilitaryOwnerSnapshot.TryCapture(
                    armedForceOwner, manpowerOwner, positionOwner,
                    exactCompletedToken, exactTokenOwnerSections,
                    out P12EMilitaryOwnerSnapshot militarySnapshot, out _)
                || !PersistentConflictOwnerSnapshot.TryCapture(
                    conflictOwner, exactCompletedToken, exactTokenOwnerSections,
                    out PersistentConflictOwnerSnapshot conflictSnapshot, out _)
                || !PersistentWarOwnerSnapshot.TryCapture(
                    warOwner, exactCompletedToken, exactTokenOwnerSections,
                    out PersistentWarOwnerSnapshot warSnapshot, out _)
                || !PersistentBattleOwnerSnapshot.TryCapture(
                    battleOwner, exactCompletedToken, exactTokenOwnerSections,
                    out PersistentBattleOwnerSnapshot battleSnapshot, out _)
                || !P12EJusticeRecordsSnapshotAdapter.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EJusticeRecordsOwnerSnapshot justiceSnapshot, out _, context.StagingAttempt)
                || !justiceOwner.TryCaptureP12AReceiptOwnerSnapshot(
                    out P12AJusticeReceiptOwnerSnapshot justiceReceiptSnapshot)
                || !P12ECrimeSocialAppraisalCaptureAdapter.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12ECrimeSocialAppraisalOwnerSnapshot crimeSocialSnapshot, out _, context.StagingAttempt))
            {
                failure = P12EDailyV1OwnerPackageFailure.CaptureFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured);

            if (!context.StagingAttempt.IsCurrentFor(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections))
            {
                failure = P12EDailyV1OwnerPackageFailure.StaleBoundary;
                return false;
            }

            P12DDailyV1OwnerPackage stagedD = context.P12DPackage;
            P12CStagedContinuationRoot stagedC = context.P12CRoots;
            if (!TryStageCoreOwnerRestore(institutionOfficeSnapshot, propertyEstateSnapshot,
                    factionSnapshot, claimSnapshot, supportSnapshot, decisionSnapshot, justiceSnapshot,
                    militarySnapshot, stagedD.Persons, stagedC.SpatialAuthority,
                    stagedD.SelectedCityOwners, stagedD.SelectedNpcOwners,
                    exactCompletedToken.AbsoluteDay, stageObserver,
                    out P12ECoreOwnerRestoreStage coreOwnerStage,
                    propertyEstateSnapshotOverrideForTest, factionSnapshotOverrideForTest,
                    politicalClaimSnapshotOverrideForTest,
                    politicalSupportSnapshotOverrideForTest))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            InstitutionStore stagedInstitutions = coreOwnerStage.Institutions;
            OfficeStore stagedOffices = coreOwnerStage.Offices;
            PropertyOwnershipStore stagedPropertyOwnership = coreOwnerStage.PropertyOwnership;
            EstateStore stagedEstates = coreOwnerStage.Estates;
            FactionStore stagedFactions = coreOwnerStage.Factions;
            PoliticalClaimStore stagedClaims = coreOwnerStage.PoliticalClaims;
            PoliticalSupportStore stagedSupport = coreOwnerStage.PoliticalSupport;
            PoliticalDecisionStore stagedDecisions = coreOwnerStage.PoliticalDecisions;
            ArmedForceStore stagedArmedForces = coreOwnerStage.ArmedForces;
            ContingentManpowerStateStore stagedManpower = coreOwnerStage.Manpower;
            ArmedForceSpatialStateStore stagedPositions = coreOwnerStage.Positions;

            if (!conflictSnapshot.TryStage(
                    stagedArmedForces, out PersistentConflictStore stagedConflicts, out _))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EConflictOwnerStaged);

            if (!warSnapshot.TryStage(
                    stagedArmedForces, stagedConflicts, out PersistentWarStore stagedWars, out _))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EWarOwnerStaged);

            if (!battleSnapshot.TryStage(
                    stagedArmedForces, stagedConflicts, stagedWars,
                    stagedC.SpatialAuthority, null,
                    out PersistentBattleStore stagedBattles, out _))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EBattleOwnerStaged);

            if (!P12EJusticeRecordsSnapshotAdapter.TryStage(justiceSnapshot,
                    stagedD.Cities, stagedD.Npcs, stagedD.Persons, coreOwnerStage.JusticeRecords,
                    context.FreeStatus, context.WantedStatus,
                    context.ArrestedStatus, context.HiddenStatus,
                    context.DomainEventRecorder, context.Logger,
                    out JusticeSystem stagedJustice, out _, justiceReceiptSnapshot))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            if (!crimeSocialSnapshot.TryStage(
                    stagedD.Persons, stagedInstitutions, context.StagedSimulationTime,
                    out CrimeSocialAppraisalWorldState stagedCrimeSocial, out _))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.ECrimeSocialOwnerStaged);

            if (stagedInstitutions == null || stagedOffices == null
                || stagedPropertyOwnership == null || stagedEstates == null || stagedFactions == null
                || stagedClaims == null || stagedSupport == null || stagedDecisions == null
                || stagedArmedForces == null || stagedManpower == null || stagedPositions == null
                || stagedConflicts == null || stagedWars == null || stagedBattles == null
                || stagedJustice == null || stagedCrimeSocial == null
                || !ReferenceEquals(stagedJustice.CoreOwner, coreOwnerStage.JusticeRecords))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }

            if (!ReferenceEquals(stagedCrimeSocial.SimulationTime, context.StagedSimulationTime)
                || stagedCrimeSocial.SimulationTime.AbsoluteDay != exactCompletedToken.AbsoluteDay
                || !TryBuildUnresolvedBindings(decisionSnapshot, out IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> unresolvedBindings))
            {
                failure = P12EDailyV1OwnerPackageFailure.InvalidUnresolvedBinding;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EUnresolvedBindingsValidated);

            if (!context.StagingAttempt.IsCurrentFor(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections))
            {
                failure = P12EDailyV1OwnerPackageFailure.StaleBoundary;
                return false;
            }

            package = new P12EDailyV1OwnerPackage(
                context.StagingAttempt,
                stagedC.WorldIdentity,
                stagedInstitutions, stagedOffices,
                stagedPropertyOwnership, stagedEstates, stagedFactions,
                stagedClaims, stagedSupport, stagedDecisions,
                stagedArmedForces, stagedManpower, stagedPositions,
                stagedConflicts, stagedWars, stagedBattles,
                stagedJustice, stagedCrimeSocial,
                crimeReceiptSnapshot, justiceReceiptSnapshot,
                institutionOfficeSnapshot, propertyEstateSnapshot, factionSnapshot,
                claimSnapshot, supportSnapshot, decisionSnapshot, militarySnapshot,
                conflictSnapshot, warSnapshot, battleSnapshot, justiceSnapshot, crimeSocialSnapshot,
                unresolvedBindings);
            failure = P12EDailyV1OwnerPackageFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            package = null;
            failure = P12EDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
    }

    /// <summary>Stages decoded E owner values against detached C/D roots without a capture token or census vector.</summary>
    internal static bool TryStageFromImage(
        P12AContinuationImage image,
        P12ADailyV1ImageOwnerSnapshots decoded,
        P12EDailyV1OwnerStagingContext context,
        out P12EDailyV1OwnerPackage package,
        out P12EDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        package = null;
        failure = P12EDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (image == null || decoded == null || context == null
            || context.StagingAttempt != null || context.P12CRoots?.StagingAttempt != null
            || context.P12DPackage?.StagingAttempt != null
            || context.P12CRoots.WorldIdentity == null || context.P12DPackage.WorldId == null
            || context.P12DPackage.Persons == null || context.P12DPackage.Cities == null
            || context.P12DPackage.Npcs == null || context.P12DPackage.Genealogy == null
            || context.P12DPackage.RuntimeIdentities == null || context.P12DPackage.SpatialNetwork == null
            || context.StagedSimulationTime == null || context.Logger == null
            || context.FreeStatus == null || context.WantedStatus == null
            || context.ArrestedStatus == null || context.HiddenStatus == null
            || context.StagedSimulationTime.AbsoluteDay != image.Header.Day
            || context.P12CRoots.WorldIdentity.Value != image.Header.WorldId
            || context.P12DPackage.WorldId.Value != image.Header.WorldId
            || decoded.InstitutionOffice == null || decoded.PropertyEstate == null
            || decoded.Faction == null || decoded.PoliticalClaims == null
            || decoded.PoliticalSupport == null || decoded.PoliticalDecisions == null
            || decoded.Military == null || decoded.Conflicts == null || decoded.Wars == null
            || decoded.Battles == null || decoded.Justice == null || decoded.CrimeSocial == null
            || decoded.CrimeReceipts == null || decoded.JusticeReceipts == null)
            return false;

        try
        {
            P12DDailyV1OwnerPackage stagedD = context.P12DPackage;
            P12CStagedContinuationRoot stagedC = context.P12CRoots;
            long day = image.Header.Day;
            P12EInstitutionOfficeOwnerSnapshot institutionOfficeSnapshot = decoded.InstitutionOffice;
            PropertyEstateOwnerSnapshot propertyEstateSnapshot = decoded.PropertyEstate;
            P12EFactionOwnerSnapshot factionSnapshot = decoded.Faction;
            P12EPoliticalClaimOwnerSnapshot claimSnapshot = decoded.PoliticalClaims;
            P12EPoliticalSupportOwnerSnapshot supportSnapshot = decoded.PoliticalSupport;
            P12EPoliticalDecisionOwnerSnapshot decisionSnapshot = decoded.PoliticalDecisions;
            P12EMilitaryOwnerSnapshot militarySnapshot = decoded.Military;
            PersistentConflictOwnerSnapshot conflictSnapshot = decoded.Conflicts;
            PersistentWarOwnerSnapshot warSnapshot = decoded.Wars;
            PersistentBattleOwnerSnapshot battleSnapshot = decoded.Battles;
            P12EJusticeRecordsOwnerSnapshot justiceSnapshot = decoded.Justice;
            P12ECrimeSocialAppraisalOwnerSnapshot crimeSocialSnapshot = decoded.CrimeSocial;
            P12ACrimeReceiptOwnerSnapshot crimeReceiptSnapshot = decoded.CrimeReceipts;
            P12AJusticeReceiptOwnerSnapshot justiceReceiptSnapshot = decoded.JusticeReceipts;

            bool coreOwnerStageAccepted;
            P12ECoreOwnerRestoreStage coreOwnerStage;
            if (context.Application != null)
            {
                coreOwnerStageAccepted = context.Application.TryStageRestoredP12EOwners(
                    stagedC.CoreRoot, institutionOfficeSnapshot, propertyEstateSnapshot,
                    factionSnapshot, claimSnapshot, supportSnapshot, decisionSnapshot,
                    justiceSnapshot, militarySnapshot, stagedD.Persons, stagedD.SelectedCityOwners,
                    stagedD.SelectedNpcOwners, day, context.ExpectedSession,
                    context.ReplacementOperation,
                    out coreOwnerStage, out _,
                    step => ObserveCoreOwnerRestoreStep(stageObserver, step));
            }
            else
            {
                // Lower-level restore coordinator tests intentionally exercise
                // detached staging without an application publication gate.
                // The same authoritative Core stager still creates these owners.
                coreOwnerStageAccepted = DailyV1P12ECoreOwnerRestoreStager.TryStage(
                    institutionOfficeSnapshot, propertyEstateSnapshot, factionSnapshot,
                    claimSnapshot, supportSnapshot, decisionSnapshot, justiceSnapshot, militarySnapshot,
                    stagedD.Persons, stagedC.SpatialAuthority,
                    stagedD.SelectedCityOwners, stagedD.SelectedNpcOwners,
                    day, step => ObserveCoreOwnerRestoreStep(stageObserver, step),
                    out coreOwnerStage, out _);
            }
            if (!coreOwnerStageAccepted || coreOwnerStage == null)
                return StageFailed(out failure);
            InstitutionStore stagedInstitutions = coreOwnerStage.Institutions;
            OfficeStore stagedOffices = coreOwnerStage.Offices;
            PropertyOwnershipStore stagedPropertyOwnership = coreOwnerStage.PropertyOwnership;
            EstateStore stagedEstates = coreOwnerStage.Estates;
            FactionStore stagedFactions = coreOwnerStage.Factions;
            PoliticalClaimStore stagedClaims = coreOwnerStage.PoliticalClaims;
            PoliticalSupportStore stagedSupport = coreOwnerStage.PoliticalSupport;
            PoliticalDecisionStore stagedDecisions = coreOwnerStage.PoliticalDecisions;
            ArmedForceStore stagedArmedForces = coreOwnerStage.ArmedForces;
            ContingentManpowerStateStore stagedManpower = coreOwnerStage.Manpower;
            ArmedForceSpatialStateStore stagedPositions = coreOwnerStage.Positions;

            if (!conflictSnapshot.TryStage(stagedArmedForces,
                    out PersistentConflictStore stagedConflicts, out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EConflictOwnerStaged);

            if (!warSnapshot.TryStage(stagedArmedForces, stagedConflicts,
                    out PersistentWarStore stagedWars, out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EWarOwnerStaged);

            if (!battleSnapshot.TryStage(stagedArmedForces, stagedConflicts, stagedWars,
                    stagedC.SpatialAuthority, null, out PersistentBattleStore stagedBattles, out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EBattleOwnerStaged);

            if (!P12EJusticeRecordsSnapshotAdapter.TryStage(justiceSnapshot, stagedD.Cities, stagedD.Npcs,
                    stagedD.Persons, coreOwnerStage.JusticeRecords,
                    context.FreeStatus, context.WantedStatus, context.ArrestedStatus, context.HiddenStatus,
                    context.DomainEventRecorder, context.Logger,
                    out JusticeSystem stagedJustice, out _, justiceReceiptSnapshot))
                return StageFailed(out failure);

            if (!crimeSocialSnapshot.TryStage(stagedD.Persons, stagedInstitutions,
                    context.StagedSimulationTime, out CrimeSocialAppraisalWorldState stagedCrimeSocial, out _))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.ECrimeSocialOwnerStaged);

            if (stagedInstitutions == null || stagedOffices == null || stagedPropertyOwnership == null
                || stagedEstates == null || stagedFactions == null || stagedClaims == null
                || stagedSupport == null || stagedDecisions == null || stagedArmedForces == null
                || stagedManpower == null || stagedPositions == null || stagedConflicts == null
                || stagedWars == null || stagedBattles == null || stagedJustice == null
                || !ReferenceEquals(stagedJustice.CoreOwner, coreOwnerStage.JusticeRecords)
                || stagedCrimeSocial == null
                || !ReferenceEquals(stagedCrimeSocial.SimulationTime, context.StagedSimulationTime)
                || stagedCrimeSocial.SimulationTime.AbsoluteDay != day
                || !TryBuildUnresolvedBindings(decisionSnapshot,
                    out IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> unresolvedBindings))
                return StageFailed(out failure);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EUnresolvedBindingsValidated);

            package = new P12EDailyV1OwnerPackage(null, stagedC.WorldIdentity,
                stagedInstitutions, stagedOffices, stagedPropertyOwnership, stagedEstates,
                stagedFactions, stagedClaims, stagedSupport, stagedDecisions, stagedArmedForces,
                stagedManpower, stagedPositions, stagedConflicts, stagedWars, stagedBattles,
                stagedJustice, stagedCrimeSocial, crimeReceiptSnapshot, justiceReceiptSnapshot,
                institutionOfficeSnapshot, propertyEstateSnapshot, factionSnapshot, claimSnapshot,
                supportSnapshot, decisionSnapshot, militarySnapshot, conflictSnapshot, warSnapshot,
                battleSnapshot, justiceSnapshot, crimeSocialSnapshot, unresolvedBindings);
            failure = P12EDailyV1OwnerPackageFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            package = null;
            failure = P12EDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
    }

    private static bool TryStageCoreOwnerRestore(
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EJusticeRecordsOwnerSnapshot justiceRecords,
        P12EMilitaryOwnerSnapshot military,
        PersonStore persons,
        SpatialAuthorityStore stagedSpatialAuthority,
        IReadOnlyList<DailyV1CityRuntimeOwners> stagedCities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedNpcs,
        long capturedDay,
        Action<P12GDailyV1RestoreStage> stageObserver,
        out P12ECoreOwnerRestoreStage staged,
        Func<PropertyEstateOwnerSnapshot, PropertyEstateOwnerSnapshot> propertyEstateOverride = null,
        Func<P12EFactionOwnerSnapshot, P12EFactionOwnerSnapshot> factionOverride = null,
        Func<P12EPoliticalClaimOwnerSnapshot, P12EPoliticalClaimOwnerSnapshot> politicalClaimOverride = null,
        Func<P12EPoliticalSupportOwnerSnapshot, P12EPoliticalSupportOwnerSnapshot> politicalSupportOverride = null)
    {
        staged = null;
        if (!DailyV1P12ECoreOwnerRestoreBuilder.TryCreate(
                persons, stagedSpatialAuthority, capturedDay,
                out DailyV1P12ECoreOwnerRestoreBuilder builder, out _))
            return false;
        if (!builder.TryStageInstitutionOffice(institutionOffice, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.InstitutionOffice);

        PropertyEstateOwnerSnapshot effectivePropertyEstate = propertyEstateOverride != null
            ? propertyEstateOverride(propertyEstate)
            : propertyEstate;
        if (!builder.TryStagePropertyEstate(effectivePropertyEstate, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.PropertyEstate);

        P12EFactionOwnerSnapshot effectiveFaction = factionOverride != null
            ? factionOverride(faction)
            : faction;
        if (!builder.TryStageFaction(effectiveFaction, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.Faction);

        P12EPoliticalClaimOwnerSnapshot effectivePoliticalClaims = politicalClaimOverride != null
            ? politicalClaimOverride(politicalClaims)
            : politicalClaims;
        if (!builder.TryStagePoliticalClaims(effectivePoliticalClaims, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.PoliticalClaim);

        P12EPoliticalSupportOwnerSnapshot effectivePoliticalSupport = politicalSupportOverride != null
            ? politicalSupportOverride(politicalSupport)
            : politicalSupport;
        if (!builder.TryStagePoliticalSupport(effectivePoliticalSupport, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.PoliticalSupport);

        if (!builder.TryStagePoliticalDecisions(politicalDecisions, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.PoliticalDecision);
        if (!builder.TryStageJusticeRecords(justiceRecords, stagedCities, stagedNpcs, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.JusticeRecords);
        if (!builder.TryStageMilitary(military, out _))
            return false;
        ObserveCoreOwnerRestoreStep(stageObserver, P12ECoreOwnerRestoreStep.Military);
        return builder.TryBuild(out staged, out _);
    }

    private static void ObserveCoreOwnerRestoreStep(
        Action<P12GDailyV1RestoreStage> stageObserver,
        P12ECoreOwnerRestoreStep step)
    {
        if (stageObserver == null) return;
        switch (step)
        {
            case P12ECoreOwnerRestoreStep.InstitutionOffice:
                stageObserver(P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged);
                break;
            case P12ECoreOwnerRestoreStep.PropertyEstate:
                stageObserver(P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged);
                break;
            case P12ECoreOwnerRestoreStep.Faction:
                stageObserver(P12GDailyV1RestoreStage.EFactionOwnerStaged);
                break;
            case P12ECoreOwnerRestoreStep.PoliticalClaim:
                stageObserver(P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged);
                break;
            case P12ECoreOwnerRestoreStep.PoliticalSupport:
                stageObserver(P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged);
                break;
            case P12ECoreOwnerRestoreStep.PoliticalDecision:
                stageObserver(P12GDailyV1RestoreStage.EPoliticalDecisionsOwnerStaged);
                break;
            case P12ECoreOwnerRestoreStep.JusticeRecords:
                stageObserver(P12GDailyV1RestoreStage.EJusticeOwnerStaged);
                break;
            case P12ECoreOwnerRestoreStep.Military:
                stageObserver(P12GDailyV1RestoreStage.EMilitaryOwnersStaged);
                break;
        }
    }
    private static bool StageFailed(out P12EDailyV1OwnerPackageFailure failure)
    {
        failure = P12EDailyV1OwnerPackageFailure.StageFailed;
        return false;
    }

    private static bool TryValidateContext(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        P12EDailyV1OwnerStagingContext context)
    {
        if (sourceRuntime == null || token == null || ownerSections == null || context == null
            || context.StagingAttempt == null
            || !context.StagingAttempt.IsCurrentFor(sourceRuntime, token, ownerSections)
            || !ReferenceEquals(token.OwnerSections, ownerSections)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L
            || context.P12CRoots == null || context.P12DPackage == null
            || !ReferenceEquals(context.P12CRoots.StagingAttempt, context.StagingAttempt)
            || !ReferenceEquals(context.P12DPackage.StagingAttempt, context.StagingAttempt)
            || context.StagedSimulationTime == null
            || context.FreeStatus == null || context.WantedStatus == null
            || context.ArrestedStatus == null || context.HiddenStatus == null || context.Logger == null
            || context.P12CRoots.WorldIdentity == null || context.P12CRoots.SpatialAuthority == null
            || context.P12DPackage.WorldId == null || context.P12DPackage.Persons == null
            || context.P12DPackage.Genealogy == null || context.P12DPackage.RuntimeIdentities == null
            || context.P12DPackage.SpatialNetwork == null || context.P12DPackage.Cities == null
            || context.P12DPackage.Npcs == null || context.P12DPackage.EmptyExplorableSites == null
            || sourceRuntime.WorldId == null
            || !string.Equals(sourceRuntime.WorldId.Value, token.WorldId?.Value, StringComparison.Ordinal)
            || !string.Equals(context.P12CRoots.WorldIdentity.Value, token.WorldId?.Value, StringComparison.Ordinal)
            || !string.Equals(context.P12DPackage.WorldId.Value, token.WorldId?.Value, StringComparison.Ordinal)
            || context.StagedSimulationTime.AbsoluteDay != token.AbsoluteDay
            || sourceRuntime.LocalTopologyStore != null
            || !context.StagingAttempt.IsCurrentFor(sourceRuntime, token, ownerSections))
            return false;

        return true;
    }

    private static bool TryFindRequiredOwner<TOwner>(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        out TOwner owner)
        where TOwner : class
    {
        owner = null;
        if (sections == null || string.IsNullOrWhiteSpace(sectionId) || schemaVersion <= 0)
            return false;

        OwnerSectionCensusSnapshot match = null;
        for (int i = 0; i < sections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = sections[i];
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            if (match != null) return false;
            match = section;
        }

        if (match == null || match.SchemaVersion != schemaVersion
            || match.Role != OwnerSectionRole.Required
            || match.Cardinality < 0 || match.Revision < 0L
            || !(match.OwnerInstanceIdentity is TOwner typedOwner))
            return false;

        owner = typedOwner;
        return true;
    }

    private static bool TryFindOptionalOwner<TOwner>(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        out TOwner owner,
        out long revision)
        where TOwner : class
    {
        owner = null;
        revision = -1L;
        if (sections == null || string.IsNullOrWhiteSpace(sectionId) || schemaVersion <= 0)
            return false;

        OwnerSectionCensusSnapshot match = null;
        for (int i = 0; i < sections.Count; i++)
        {
            OwnerSectionCensusSnapshot section = sections[i];
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            if (match != null) return false;
            match = section;
        }

        if (match == null) return true;
        if (match.SchemaVersion != schemaVersion || match.Role != OwnerSectionRole.Required
            || match.Cardinality < 0 || match.Revision < 0L
            || !(match.OwnerInstanceIdentity is TOwner typedOwner))
            return false;

        owner = typedOwner;
        revision = match.Revision;
        return true;
    }

    private static bool TryBuildUnresolvedBindings(
        P12EPoliticalDecisionOwnerSnapshot snapshot,
        out IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> bindings)
    {
        bindings = null;
        if (snapshot == null || snapshot.Decisions == null || snapshot.Decisions.Records == null)
            return false;

        List<P12EUnresolvedPoliticalKnowledgeBinding> result =
            new List<P12EUnresolvedPoliticalKnowledgeBinding>();
        foreach (P12EPoliticalDecisionSnapshotRow row in snapshot.Decisions.Records)
        {
            if (row == null || row.KnowledgeReferences == null
                || row.ExpectedKnowledgeRevision < 0L || row.ExpectedWorldRevision < 0L)
                return false;
            if (row.KnowledgeReferences.Count > 0)
                result.Add(new P12EUnresolvedPoliticalKnowledgeBinding(row));
        }

        bindings = new ReadOnlyCollection<P12EUnresolvedPoliticalKnowledgeBinding>(result);
        return true;
    }
}
