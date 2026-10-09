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
        SimulationLogger logger)
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
    internal IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> UnresolvedPoliticalKnowledgeBindings { get; }

    private P12EDailyV1OwnerPackage(
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
        IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> unresolvedBindings)
    {
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
        UnresolvedPoliticalKnowledgeBindings = new ReadOnlyCollection<P12EUnresolvedPoliticalKnowledgeBinding>(
            new List<P12EUnresolvedPoliticalKnowledgeBinding>(unresolvedBindings));
    }

    internal static bool TryCaptureAndStage(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken exactCompletedToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactTokenOwnerSections,
        P12EDailyV1OwnerStagingContext context,
        out P12EDailyV1OwnerPackage package,
        out P12EDailyV1OwnerPackageFailure failure)
    {
        package = null;
        failure = P12EDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (!TryValidateContext(sourceRuntime, exactCompletedToken, exactTokenOwnerSections, context))
            return false;

        try
        {
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
                    out PersistentBattleStore battleOwner))
            {
                failure = P12EDailyV1OwnerPackageFailure.InvalidOwnerEvidence;
                return false;
            }

            if (!P12EInstitutionOfficeOwnerSnapshot.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EInstitutionOfficeOwnerSnapshot institutionOfficeSnapshot, out _)
                || !PropertyEstateOwnerSnapshot.TryCapture(
                    propertyOwner, estateOwner, exactCompletedToken, exactTokenOwnerSections,
                    out PropertyEstateOwnerSnapshot propertyEstateSnapshot, out _)
                || !P12EFactionOwnerSnapshot.TryCapture(
                    sourceRuntime, factionOwner, exactCompletedToken, exactTokenOwnerSections,
                    out P12EFactionOwnerSnapshot factionSnapshot, out _)
                || !P12EPoliticalClaimOwnerSnapshot.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EPoliticalClaimOwnerSnapshot claimSnapshot, out _)
                || !P12EPoliticalSupportOwnerSnapshot.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EPoliticalSupportOwnerSnapshot supportSnapshot, out _)
                || !P12EPoliticalDecisionOwnerSnapshot.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EPoliticalDecisionOwnerSnapshot decisionSnapshot, out _)
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
                || !P12EJusticeRecordsOwnerSnapshot.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12EJusticeRecordsOwnerSnapshot justiceSnapshot, out _)
                || !P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections,
                    out P12ECrimeSocialAppraisalOwnerSnapshot crimeSocialSnapshot, out _))
            {
                failure = P12EDailyV1OwnerPackageFailure.CaptureFailed;
                return false;
            }

            if (!context.StagingAttempt.IsCurrentFor(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections))
            {
                failure = P12EDailyV1OwnerPackageFailure.StaleBoundary;
                return false;
            }

            P12DDailyV1OwnerPackage stagedD = context.P12DPackage;
            P12CStagedContinuationRoot stagedC = context.P12CRoots;
            if (!institutionOfficeSnapshot.TryCreateStagedOwners(
                    stagedD.Persons, out InstitutionStore stagedInstitutions,
                    out OfficeStore stagedOffices, out _)
                || !propertyEstateSnapshot.TryStage(
                    stagedD.Persons, exactCompletedToken.AbsoluteDay,
                    out PropertyOwnershipStore stagedPropertyOwnership,
                    out EstateStore stagedEstates, out _)
                || !factionSnapshot.TryCreateStagedOwner(
                    stagedD.Persons, exactCompletedToken.AbsoluteDay,
                    out FactionStore stagedFactions, out _)
                || !claimSnapshot.TryStage(
                    stagedD.Persons, stagedPropertyOwnership, stagedInstitutions, stagedOffices,
                    exactCompletedToken.AbsoluteDay, out PoliticalClaimStore stagedClaims, out _)
                || !supportSnapshot.TryStage(
                    stagedD.Persons, stagedFactions, stagedClaims,
                    out PoliticalSupportStore stagedSupport, out _)
                || !decisionSnapshot.TryStage(
                    stagedD.Persons, stagedInstitutions, stagedOffices, stagedClaims,
                    out PoliticalDecisionStore stagedDecisions, out _)
                || !militarySnapshot.TryStage(
                    stagedD.Persons, stagedC.SpatialAuthority, null,
                    out ArmedForceStore stagedArmedForces,
                    out ContingentManpowerStateStore stagedManpower,
                    out ArmedForceSpatialStateStore stagedPositions, out _)
                || !conflictSnapshot.TryStage(
                    stagedArmedForces, out PersistentConflictStore stagedConflicts, out _)
                || !warSnapshot.TryStage(
                    stagedArmedForces, stagedConflicts, out PersistentWarStore stagedWars, out _)
                || !battleSnapshot.TryStage(
                    stagedArmedForces, stagedConflicts, stagedWars,
                    stagedC.SpatialAuthority, null,
                    out PersistentBattleStore stagedBattles, out _)
                || !justiceSnapshot.TryStage(
                    stagedD.Cities, stagedD.Npcs, stagedD.Persons,
                    context.FreeStatus, context.WantedStatus,
                    context.ArrestedStatus, context.HiddenStatus,
                    context.DomainEventRecorder, context.Logger,
                    out JusticeSystem stagedJustice, out _)
                || !crimeSocialSnapshot.TryStage(
                    stagedD.Persons, stagedInstitutions, context.StagedSimulationTime,
                    out CrimeSocialAppraisalWorldState stagedCrimeSocial, out _))
            {
                failure = P12EDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }

            if (stagedInstitutions == null || stagedOffices == null
                || stagedPropertyOwnership == null || stagedEstates == null || stagedFactions == null
                || stagedClaims == null || stagedSupport == null || stagedDecisions == null
                || stagedArmedForces == null || stagedManpower == null || stagedPositions == null
                || stagedConflicts == null || stagedWars == null || stagedBattles == null
                || stagedJustice == null || stagedCrimeSocial == null)
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

            if (!context.StagingAttempt.IsCurrentFor(
                    sourceRuntime, exactCompletedToken, exactTokenOwnerSections))
            {
                failure = P12EDailyV1OwnerPackageFailure.StaleBoundary;
                return false;
            }

            package = new P12EDailyV1OwnerPackage(
                stagedC.WorldIdentity,
                stagedInstitutions, stagedOffices,
                stagedPropertyOwnership, stagedEstates, stagedFactions,
                stagedClaims, stagedSupport, stagedDecisions,
                stagedArmedForces, stagedManpower, stagedPositions,
                stagedConflicts, stagedWars, stagedBattles,
                stagedJustice, stagedCrimeSocial, unresolvedBindings);
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
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _))
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
