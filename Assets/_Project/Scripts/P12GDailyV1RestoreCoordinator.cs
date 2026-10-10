using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

internal enum P12GDailyV1RestoreFailure
{
    None = 0,
    InvalidSourceSession,
    SourceBoundaryUnavailable,
    SourceCaptureFailed,
    RootStageFailed,
    OwnerStageFailed,
    BindingValidationFailed,
    TargetCompositionFailed,
    TargetOwnerVectorFailed,
    TargetAdmissionFailed,
    PublicationFailed
}

internal enum P12GDailyV1RestoreStage
{
    SourceCaptured = 1,
    RootsStaged,
    DStaged,
    EStaged,
    FStaged,
    OwnersStaged,
    CandidateComposed,
    TargetOwnerVectorCaptured,
    TargetChecksCompleted,
    CandidateGuardBound,
    TargetOwnerVectorRecaptured,
    BoundaryAdmitted,
    BeforePublication,
    CWorldIdentityStaged,
    CGenesisManifestStaged,
    CSpatialAuthorityStaged,
    CDeterministicRandomStaged,
    CRuntimeIdAllocatorStaged,
    CRecordSequenceStaged,
    DSnapshotsCaptured,
    DSpatialNetworkStaged,
    DPersonsStaged,
    DGenealogyStaged,
    DCitiesStaged,
    DRelationsValidated,
    DNpcsStaged,
    DFinalGraphValidated,
    EOwnerSnapshotsCaptured,
    EInstitutionOfficeOwnersStaged,
    EPropertyEstateOwnersStaged,
    EFactionOwnerStaged,
    EPoliticalClaimsOwnerStaged,
    EPoliticalSupportOwnerStaged,
    EPoliticalDecisionsOwnerStaged,
    EMilitaryOwnersStaged,
    EConflictOwnerStaged,
    EWarOwnerStaged,
    EBattleOwnerStaged,
    EJusticeOwnerStaged,
    ECrimeSocialOwnerStaged,
    EUnresolvedBindingsValidated,
    FOwnerSnapshotsCaptured,
    FPoliticalKnowledgeStaged,
    FScheduledDirectivesStaged,
    FActorChoicesStaged,
    FTravelPartiesStaged,
    FExpeditionsStaged,
    TargetPersonNpcGenealogyBindingsValidated,
    TargetOwnerVectorValidated,
    TargetSentinelsValidated,
    TargetPoliticalKnowledgeBindingsValidated,
    TargetSpatialInvariantsValidated,
    TargetNpcCensusValidated,
    TargetTravelPartyBindingsValidated
}

/// <summary>
/// Builds a fresh, unpublished Daily-v1 continuation from the already reviewed
/// B-F owner snapshots, validates the complete target census, admits the exact
/// restored boundary, and returns one candidate for TesteSimulacao's existing
/// single-reference exchange. This is an in-memory continuation path; it does
/// not define or parse a persistence envelope.
/// </summary>
internal static class P12GDailyV1RestoreCoordinator
{
    internal static bool TryCreateRestoredSession(
        SimulationActiveSession sourceSession,
        out SimulationActiveSession restoredSession,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        Action<SimulationActiveSession> privateCandidateObserver = null)
    {
        restoredSession = null;
        failure = P12GDailyV1RestoreFailure.InvalidSourceSession;
        diagnostic = "A supported active Daily-v1 session is required.";
        if (!IsSupportedSource(sourceSession))
            return false;

        SimulationRuntime sourceRuntime = sourceSession.Runtime;
        SimulationBootstrapComposition sourceComposition = sourceSession.Composition;
        if (!sourceRuntime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken sourceToken,
                out DailyCaptureEligibilityFailure tokenFailure)
            || sourceToken == null
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(sourceToken, out _))
        {
            failure = P12GDailyV1RestoreFailure.SourceBoundaryUnavailable;
            diagnostic = "The active source has no current successful completed Daily-v1 boundary: "
                + tokenFailure + ".";
            return false;
        }

        IReadOnlyList<OwnerSectionCensusSnapshot> sourceOwnerSections = sourceToken.OwnerSections;
        if (!TryValidateDailyV1ExpeditionExactZero(
                sourceComposition, sourceOwnerSections, out string sourceExpeditionDiagnostic))
        {
            failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
            diagnostic = "The selected Daily-v1 source must have an exact-empty Expedition owner: "
                + sourceExpeditionDiagnostic;
            return false;
        }

        if (!DailyCaptureStagingAttempt.TryBegin(
                sourceRuntime,
                sourceToken,
                sourceOwnerSections,
                out DailyCaptureStagingAttempt stagingAttempt))
        {
            failure = P12GDailyV1RestoreFailure.SourceBoundaryUnavailable;
            diagnostic = "The exact source completed-boundary owner vector could not begin a staging attempt.";
            return false;
        }

        try
        {
            // F checks the source ActorChoice temporal section before C allocates
            // any candidate root. Keep this capture for staging later in the
            // same attempt.
            if (!P12FDailyV1OwnerCapture.TryCapture(
                    sourceRuntime,
                    sourceComposition,
                    sourceToken,
                    sourceOwnerSections,
                    out P12FDailyV1OwnerCapture capturedF,
                    out P12FDailyV1OwnerPackageFailure fCaptureFailure,
                    stageObserver))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "P12-F source capture rejected the completed boundary: " + fCaptureFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.SourceCaptured);

            if (!(sourceSession.RandomSource is DeterministicRandomSource sourceRandom)
                || sourceComposition.RuntimeIdAllocator == null
                || sourceComposition.RecordSequence == null)
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "The deterministic random source or P12-C identity roots are unsupported.";
                return false;
            }

            if (!P12CWorldIdentitySnapshot.TryCapture(
                    sourceComposition.WorldId,
                    out P12CWorldIdentitySnapshot worldIdentitySnapshot,
                    out string worldIdentityDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "P12-C WorldId capture failed: " + worldIdentityDiagnostic;
                return false;
            }

            if (!P12CP9GenesisManifestSnapshot.TryCapture(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    sourceComposition.Manifest,
                    out P12CP9GenesisManifestSnapshot manifestSnapshot,
                    out string manifestDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "P12-C P9 manifest capture failed: " + manifestDiagnostic;
                return false;
            }

            if (!P12CSpatialAuthoritySnapshot.TryCapture(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    sourceComposition.Manifest.SelectedP9ContractIdentity,
                    sourceComposition.Manifest.SelectedP9SchemaVersion,
                    sourceComposition.SpatialAuthority,
                    out P12CSpatialAuthoritySnapshot spatialSnapshot,
                    out P12CSpatialAuthoritySnapshotFailure spatialFailure))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "P12-C spatial authority capture failed: " + spatialFailure?.Message;
                return false;
            }

            RuntimeIdAllocatorSnapshot allocatorSnapshot = sourceComposition.RuntimeIdAllocator.CaptureSnapshot();
            SimulationRecordSequenceSnapshot recordSequenceSnapshot = sourceComposition.RecordSequence.CaptureSnapshot();
            DeterministicRandomRootSnapshot randomSnapshot = sourceRandom.CaptureSnapshot();
            if (!P12CContinuationRootStager.TryStageForRestore(
                    stagingAttempt,
                    worldIdentitySnapshot,
                    allocatorSnapshot,
                    recordSequenceSnapshot,
                    spatialSnapshot,
                    manifestSnapshot,
                    randomSnapshot,
                    out P12CStagedContinuationRoot stagedC,
                    out string cDiagnostic,
                    stageObserver))
            {
                failure = P12GDailyV1RestoreFailure.RootStageFailed;
                diagnostic = "P12-C could not stage the captured roots: " + cDiagnostic;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.RootsStaged);

            if (!TryBuildDailyV1Definitions(
                    sourceRuntime,
                    out List<CityData> cityDefinitions,
                    out List<ItemData> itemDefinitions,
                    out List<NpcData> npcDefinitions))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "The active Daily-v1 runtime has an incomplete authored definition set.";
                return false;
            }

            RuntimeIdentityRegistry sourceIdentityRegistry = sourceSession.IdentityRegistry;
            ExplorableSiteStore sourceSites = sourceComposition.ExplorableSites;
            if (!P12DDailyV1OwnerPackage.TryCaptureAndStage(
                    sourceRuntime,
                    sourceToken,
                    stagingAttempt,
                    sourceOwnerSections,
                    sourceIdentityRegistry,
                    sourceSession.SpatialNetwork,
                    sourceSites,
                    new RuntimeIdentityRegistry(),
                    stagedC.WorldIdentity,
                    cityDefinitions,
                    itemDefinitions,
                    npcDefinitions,
                    sourceSession.Configuration.Actions,
                    sourceSession.Configuration.Statuses,
                    Array.Empty<ExplorableSiteData>(),
                    capturedF.TravelPartyIds,
                    out P12DDailyV1OwnerPackage stagedD,
                    out P12DDailyV1OwnerPackageFailure dFailure,
                    stageObserver))
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "P12-D could not stage the factual roots: " + dFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.DStaged);

            SimulationTime stagedTime = new SimulationTime(sourceToken.AbsoluteDay);
            SimulationLogger logger = new SimulationLogger(sourceSession.Configuration.LogSettings);
            HistoryStore history = new HistoryStore();
            DomainEventStore eventStore = new DomainEventStore(history, new HistoryPolicy(), logger);
            NpcDecisionStore decisions = new NpcDecisionStore();
            DomainEventRecorder eventRecorder = new DomainEventRecorder(
                stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, eventStore, logger);
            NpcDecisionRecorder decisionRecorder = new NpcDecisionRecorder(
                stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, decisions, logger);
            EconomyTransactionService economyTransactions = new EconomyTransactionService();

            P12EDailyV1OwnerStagingContext eContext = new P12EDailyV1OwnerStagingContext(
                stagingAttempt,
                stagedC,
                stagedD,
                stagedTime,
                sourceSession.Configuration.freeStatus,
                sourceSession.Configuration.wantedStatus,
                sourceSession.Configuration.arrestedStatus,
                sourceSession.Configuration.hiddenStatus,
                eventRecorder,
                logger);
            if (!P12EDailyV1OwnerPackage.TryCaptureAndStage(
                    sourceRuntime,
                    sourceToken,
                    sourceOwnerSections,
                    eContext,
                    out P12EDailyV1OwnerPackage stagedE,
                    out P12EDailyV1OwnerPackageFailure eFailure,
                    stageObserver))
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "P12-E could not stage the configured domain owners: " + eFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EStaged);

            if (!capturedF.TryStage(
                    sourceRuntime,
                    sourceToken,
                    sourceOwnerSections,
                    eContext,
                    stagedE,
                    sourceSession.Configuration.Actions,
                    out P12FDailyV1OwnerPackage stagedF,
                    out P12FDailyV1OwnerPackageFailure fStageFailure,
                    stageObserver))
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "P12-F could not stage the retained owner capture: " + fStageFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.FStaged);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.OwnersStaged);

            if (!stagingAttempt.IsCurrentFor(sourceRuntime, sourceToken, sourceOwnerSections)
                || !sourceRuntime.TryValidateCompletedDailyCaptureToken(sourceToken, out _))
            {
                failure = P12GDailyV1RestoreFailure.SourceBoundaryUnavailable;
                diagnostic = "The source completed boundary changed during private staging.";
                return false;
            }

            if (!TryBuildRestoredSession(
                    sourceSession,
                    sourceToken,
                    stagedC,
                    stagedD,
                    stagedE,
                    stagedF,
                    stagedTime,
                    eventStore,
                    history,
                    decisions,
                    decisionRecorder,
                    eventRecorder,
                    economyTransactions,
                    logger,
                    out SimulationActiveSession candidate,
                    out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CandidateComposed);
            // Tests can corrupt only the still-private candidate to verify the
            // integrated graph validator. Production restore calls pass null.
            privateCandidateObserver?.Invoke(candidate);

            SimulationRuntime targetRuntime = candidate.Runtime;
            if (!TryValidatePersonNpcGenealogyBindings(targetRuntime, out string personBindingDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = personBindingDiagnostic;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetPersonNpcGenealogyBindingsValidated);

            if (!targetRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                    out IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
                    out long targetMutationEpoch,
                    out ContinuationCensusFailure targetCensusFailure))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = "The private target did not produce a quiescent, complete Daily-v1 owner vector: "
                    + targetCensusFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorCaptured);

            if (!TryValidateDailyV1ExpeditionExactZero(
                    candidate.Composition, targetOwnerSections, out string targetExpeditionDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = "The selected Daily-v1 target must have an exact-empty Expedition owner: "
                    + targetExpeditionDiagnostic;
                return false;
            }

            if (!TryValidateAllocatorHighWater(candidate, out string allocatorDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = allocatorDiagnostic;
                return false;
            }

            if (!P12GDailyV1OwnerVector.TryValidateRestoredCandidate(
                    sourceOwnerSections,
                    targetOwnerSections,
                    out P12GDailyV1OwnerVectorFailure vectorFailure,
                    out string vectorDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = vectorDiagnostic ?? vectorFailure.ToString();
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorValidated);

            if (!TryValidateTargetSentinels(candidate.Composition, targetOwnerSections, out string sentinelDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = sentinelDiagnostic;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetSentinelsValidated);

            if (!TryValidatePoliticalKnowledgeBindings(
                    stagedE.UnresolvedPoliticalKnowledgeBindings,
                    targetRuntime,
                    out string bindingDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = bindingDiagnostic;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetPoliticalKnowledgeBindingsValidated);

            SimulationRuntimeSpatialInvariantReport spatialReport = targetRuntime.ValidateSpatialInvariants();
            if (spatialReport == null || !spatialReport.IsValid)
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = "The private target failed spatial invariant validation.";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetSpatialInvariantsValidated);

            if (!targetRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure targetAssessFailure))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = "The private target failed NPC roster census validation: " + targetAssessFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetNpcCensusValidated);

            if (!TryValidateTravelPartyBindings(candidate, out string travelPartyDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = travelPartyDiagnostic;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetTravelPartyBindingsValidated);

            if (!stagingAttempt.IsCurrentFor(sourceRuntime, sourceToken, sourceOwnerSections)
                || !sourceRuntime.TryValidateCompletedDailyCaptureToken(sourceToken, out _))
            {
                failure = P12GDailyV1RestoreFailure.SourceBoundaryUnavailable;
                diagnostic = "The source completed boundary changed during final target graph validation.";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetChecksCompleted);

            if (!targetRuntime.TryBindRestoredCandidateMutationGuard(out string mutationGuardDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = mutationGuardDiagnostic;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CandidateGuardBound);

            if (!targetRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                    out IReadOnlyList<OwnerSectionCensusSnapshot> postBindingOwnerSections,
                    out long postBindingMutationEpoch,
                    out ContinuationCensusFailure postBindingCensusFailure))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = "The private target could not recapture its owner vector after mutation-guard binding: "
                    + postBindingCensusFailure + ".";
                return false;
            }

            if (!P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
                    targetOwnerSections,
                    targetMutationEpoch,
                    postBindingOwnerSections,
                    postBindingMutationEpoch,
                    out P12GDailyV1OwnerVectorFailure postBindingVectorFailure,
                    out string postBindingVectorDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = "The private target owner vector changed during mutation-guard binding: "
                    + (postBindingVectorDiagnostic ?? postBindingVectorFailure.ToString());
                return false;
            }
            targetOwnerSections = postBindingOwnerSections;
            targetMutationEpoch = postBindingMutationEpoch;
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured);

            if (!targetRuntime.TryAdmitRestoredDailyBoundary(
                    stagedC.WorldIdentity,
                    sourceToken.AbsoluteDay,
                    sourceToken.CompletedCoreSequence,
                    targetOwnerSections,
                    targetMutationEpoch,
                    out DailyCaptureEligibilityFailure admissionFailure))
            {
                failure = P12GDailyV1RestoreFailure.TargetAdmissionFailed;
                diagnostic = "The target-bound completed-boundary admission failed: " + admissionFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.BoundaryAdmitted);

            if (!targetRuntime.HasValidRestoredDailyBoundaryAdmission()
                || !targetRuntime.IsHealthyDailyOwnerThreadBoundary()
                || !sourceRuntime.TryValidateCompletedDailyCaptureToken(sourceToken, out _))
            {
                failure = P12GDailyV1RestoreFailure.TargetAdmissionFailed;
                diagnostic = "The admitted target or source boundary changed before the publication handoff.";
                return false;
            }

            restoredSession = candidate;
            failure = P12GDailyV1RestoreFailure.None;
            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is NullReferenceException)
        {
            restoredSession = null;
            failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
            diagnostic = "Private Daily-v1 restoration failed without publishing the staged candidate: "
                + exception.Message;
            return false;
        }
    }

    private static bool IsSupportedSource(SimulationActiveSession session)
    {
        return session != null
            && session.Composition != null
            && session.Runtime != null
            && session.Configuration != null
            && session.AdmissionContext != null
            && session.AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && session.AdmissionContext.IsOwnedByCurrentThread()
            && session.RandomSource is DeterministicRandomSource
            && session.Composition.Manifest != null
            && session.Composition.ProfileContractIdentity == session.Composition.Manifest.ContractIdentity
            && session.Composition.ProfileFingerprint == session.Composition.Manifest.Fingerprint
            && session.Runtime.IsHealthyDailyOwnerThreadBoundary()
            && session.Composition.Runtime.HasSameP12RuntimeIdentitySpatialOwners(
                session.IdentityRegistry, session.SpatialNetwork, session.ExplorableSites);
    }

    private static bool TryValidateTravelPartyBindings(
        SimulationActiveSession session,
        out string diagnostic)
    {
        diagnostic = null;
        TravelPartyStore parties = session?.Composition?.TravelParties;
        SimulationRuntime runtime = session?.Runtime;
        RuntimeIdentityRegistry identities = session?.IdentityRegistry;
        SpatialNetworkRuntime spatialNetwork = session?.SpatialNetwork;
        if (parties == null || runtime == null || identities == null || spatialNetwork == null
            || !parties.ValidateCensus(out int partyCount, out _)
            || partyCount != parties.ActiveParties.Count)
        {
            diagnostic = "The private target's TravelParty owner census is unavailable or inconsistent.";
            return false;
        }

        Dictionary<string, NpcRuntime> npcsById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in runtime.NpcRuntimes)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcsById.TryAdd(npc.RuntimeId, npc))
            {
                diagnostic = "The private target's NPC roster has a missing or duplicate RuntimeId.";
                return false;
            }
        }

        Dictionary<string, string> partyByMemberId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (TravelPartyRuntime party in parties.ActiveParties)
        {
            if (party == null || !party.IsActive
                || !identities.TryGetLocation(party.OriginLocationRuntimeId, out SpatialLocationRuntime origin)
                || !identities.TryGetLocation(party.DestinationLocationRuntimeId, out SpatialLocationRuntime destination)
                || !identities.TryGetRoute(party.RouteRuntimeId, out SpatialRouteRuntime route)
                || !spatialNetwork.TryGetRoute(party.RouteRuntimeId, out SpatialRouteRuntime networkRoute)
                || !ReferenceEquals(route, networkRoute)
                || !ReferenceEquals(route.Origin, origin)
                || !ReferenceEquals(route.Destination, destination)
                || route.TravelDays != party.TravelDaysTotal)
            {
                diagnostic = "An active TravelParty has an unresolved or inconsistent Location/Route binding.";
                return false;
            }

            int sharedRemainingDays = -1;
            bool? sharedStartedToday = null;
            foreach (string memberId in party.MemberRuntimeIds)
            {
                if (string.IsNullOrWhiteSpace(memberId)
                    || !partyByMemberId.TryAdd(memberId, party.TravelPartyId)
                    || !npcsById.TryGetValue(memberId, out NpcRuntime member)
                    || !identities.TryGetNpc(memberId, out NpcRuntime registeredMember)
                    || !ReferenceEquals(member, registeredMember)
                    || !member.IsAlive
                    || !member.IsTraveling
                    || member.CurrentLocation != null
                    || member.CurrentCity != null
                    || !ReferenceEquals(member.DestinationLocation, destination)
                    || !string.Equals(member.TravelRouteRuntimeId, party.RouteRuntimeId, StringComparison.Ordinal)
                    || member.TravelDaysTotal != party.TravelDaysTotal
                    || member.TravelDaysRemaining <= 0
                    || member.TravelDaysRemaining > party.TravelDaysTotal
                    || !string.Equals(member.TravelOriginDecisionId, party.OriginDecisionId, StringComparison.Ordinal))
                {
                    diagnostic = "An active TravelParty member does not resolve to the exact traveling NPC state.";
                    return false;
                }
                if (!string.Equals(member.ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal))
                {
                    diagnostic = "An NPC ActiveTravelPartyId does not reciprocally identify its staged owner party.";
                    return false;
                }

                CityRuntime expectedDestinationCity = null;
                session.TryGetCityByLocation(destination, out expectedDestinationCity);
                if (!ReferenceEquals(member.DestinationCity, expectedDestinationCity))
                {
                    diagnostic = "An active TravelParty member's destination City projection is inconsistent.";
                    return false;
                }

                if (sharedRemainingDays < 0)
                {
                    sharedRemainingDays = member.TravelDaysRemaining;
                    sharedStartedToday = member.TravelStartedToday;
                }
                else if (sharedRemainingDays != member.TravelDaysRemaining
                    || sharedStartedToday != member.TravelStartedToday)
                {
                    diagnostic = "Active TravelParty members do not share the same remaining-day/start boundary.";
                    return false;
                }
            }
        }

        foreach (NpcRuntime npc in runtime.NpcRuntimes)
        {
            if (string.IsNullOrWhiteSpace(npc.ActiveTravelPartyId)) continue;
            if (!partyByMemberId.TryGetValue(npc.RuntimeId, out string partyId)
                || !string.Equals(partyId, npc.ActiveTravelPartyId, StringComparison.Ordinal))
            {
                diagnostic = "An NPC's ActiveTravelPartyId has no reciprocal active-party member binding.";
                return false;
            }
        }

        return true;
    }

    private static bool TryValidatePersonNpcGenealogyBindings(
        SimulationRuntime runtime,
        out string diagnostic)
    {
        diagnostic = null;
        PersonStore people = runtime?.PersonStore;
        IReadOnlyList<NpcRuntime> npcs = runtime?.NpcRuntimes;
        IReadOnlyList<ParentageRecord> genealogy = runtime?.GenealogyRecords;
        if (people == null || npcs == null || genealogy == null)
        {
            diagnostic = "The private target's Person/NPC/Genealogy graph is unavailable.";
            return false;
        }

        Dictionary<string, PersonRuntime> personsById = new Dictionary<string, PersonRuntime>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> npcsById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        foreach (PersonRuntime person in people.Persons)
        {
            if (person?.PersonId == null
                || string.IsNullOrWhiteSpace(person.PersonId.Value)
                || !personsById.TryAdd(person.PersonId.Value, person))
            {
                diagnostic = "The private target's PersonStore has a missing or duplicate PersonId.";
                return false;
            }
        }

        foreach (NpcRuntime npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcsById.TryAdd(npc.RuntimeId, npc))
            {
                diagnostic = "The private target's NPC roster has a missing or duplicate RuntimeId.";
                return false;
            }

            if (npc.PersonId == null)
            {
                if (npc.BoundPersonRuntime != null)
                {
                    diagnostic = "An unbound NPC retains a private Person/NPC materialization reference.";
                    return false;
                }
                continue;
            }

            if (!personsById.TryGetValue(npc.PersonId.Value, out PersonRuntime person)
                || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !ReferenceEquals(person, npc.BoundPersonRuntime)
                || !people.TryGetByMaterializedNpcRuntimeId(npc.RuntimeId, out PersonRuntime reversePerson)
                || !ReferenceEquals(person, reversePerson))
            {
                diagnostic = "An NPC PersonId does not resolve to the exact reciprocal Person/NPC materialization binding.";
                return false;
            }
        }

        foreach (PersonRuntime person in people.Persons)
        {
            if (string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId))
                continue;
            if (!npcsById.TryGetValue(person.MaterializedNpcRuntimeId, out NpcRuntime npc)
                || npc.PersonId != person.PersonId
                || !ReferenceEquals(npc.BoundPersonRuntime, person)
                || !people.TryGetByMaterializedNpcRuntimeId(person.MaterializedNpcRuntimeId, out PersonRuntime indexedPerson)
                || !ReferenceEquals(person, indexedPerson))
            {
                diagnostic = "A Person materialized-NPC id does not resolve to the exact reciprocal NPC/Person binding.";
                return false;
            }
        }

        foreach (ParentageRecord edge in genealogy)
        {
            if (edge?.ParentId == null || edge.ChildId == null
                || !personsById.ContainsKey(edge.ParentId.Value)
                || !personsById.ContainsKey(edge.ChildId.Value))
            {
                diagnostic = "A Genealogy edge endpoint does not resolve to a Person in the private target.";
                return false;
            }
        }

        return true;
    }

    private static bool TryBuildDailyV1Definitions(
        SimulationRuntime runtime,
        out List<CityData> cityDefinitions,
        out List<ItemData> itemDefinitions,
        out List<NpcData> npcDefinitions)
    {
        cityDefinitions = new List<CityData>();
        itemDefinitions = new List<ItemData>();
        npcDefinitions = new List<NpcData>();
        if (runtime?.Cities == null || runtime.NpcRuntimes == null)
            return false;

        foreach (CityRuntime city in runtime.Cities)
        {
            if (city == null || city.CityData == null || city.Market == null)
                return false;
            cityDefinitions.Add(city.CityData);
            foreach (MarketItemRuntime row in city.Market.Items)
                AddUniqueDefinitionReference(itemDefinitions, row?.Item);
        }

        foreach (NpcRuntime npc in runtime.NpcRuntimes)
        {
            if (npc == null || npc.NpcData == null)
                return false;
            npcDefinitions.Add(npc.NpcData);
            InventoryRuntime inventory = npc.ExistingInventory;
            if (inventory == null)
                continue;
            foreach (InventoryItemRuntime row in inventory.Items)
                AddUniqueDefinitionReference(itemDefinitions, row?.Item);
        }

        return cityDefinitions.Count > 0;
    }

    private static void AddUniqueDefinitionReference(List<ItemData> values, ItemData candidate)
    {
        if (candidate == null) return;
        foreach (ItemData value in values)
            if (ReferenceEquals(value, candidate)) return;
        values.Add(candidate);
    }

    private static bool TryBuildRestoredSession(
        SimulationActiveSession sourceSession,
        DailyCaptureEligibilityToken sourceToken,
        P12CStagedContinuationRoot stagedC,
        P12DDailyV1OwnerPackage stagedD,
        P12EDailyV1OwnerPackage stagedE,
        P12FDailyV1OwnerPackage stagedF,
        SimulationTime time,
        DomainEventStore eventStore,
        HistoryStore history,
        NpcDecisionStore decisions,
        NpcDecisionRecorder decisionRecorder,
        DomainEventRecorder eventRecorder,
        EconomyTransactionService economyTransactions,
        SimulationLogger logger,
        out SimulationActiveSession candidate,
        out string diagnostic)
    {
        candidate = null;
        diagnostic = null;
        SimulationConfigData config = sourceSession.Configuration;
        EffectiveSimulationConfiguration effective = sourceSession.Runtime.Configuration;
        Dictionary<SpatialLocationRuntime, CityRuntime> cityByLocation =
            new Dictionary<SpatialLocationRuntime, CityRuntime>();
        foreach (CityRuntime city in stagedD.Cities)
        {
            if (city?.Location == null || cityByLocation.ContainsKey(city.Location))
            {
                diagnostic = "The staged City-to-legacy-Location relation is missing or duplicated.";
                return false;
            }
            cityByLocation.Add(city.Location, city);
        }

        TravelSystem travel = new TravelSystem(
            stagedD.SpatialNetwork,
            location => location != null && cityByLocation.TryGetValue(location, out CityRuntime city)
                ? city
                : null,
            effective.Travel,
            eventRecorder,
            logger,
            economyTransactions);
        ScheduledDirectiveSystem directives = new ScheduledDirectiveSystem(
            stagedF.ScheduledDirectives, stagedD.RuntimeIdentities, logger);
        TravelPartySystem travelParties = new TravelPartySystem(
            stagedF.TravelParties,
            stagedC.RuntimeIdAllocator,
            stagedD.RuntimeIdentities,
            travel,
            time,
            stagedC.RecordSequence,
            eventRecorder,
            logger,
            economyTransactions);
        ExplorableSiteKnowledgeSystem siteKnowledge = new ExplorableSiteKnowledgeSystem();
        ExpeditionSystem expeditions = new ExpeditionSystem(
            stagedF.Expeditions,
            stagedC.RuntimeIdAllocator,
            stagedD.RuntimeIdentities,
            stagedD.EmptyExplorableSites,
            travelParties,
            stagedF.TravelParties,
            siteKnowledge,
            time,
            eventRecorder,
            logger);

        MerchantSystem merchant = null;
        CommercialKnowledgeSharingSystem commercialKnowledge = null;
        if (effective.MerchantTrade.Enabled == true)
        {
            merchant = new MerchantSystem(
                effective.MerchantTrade,
                effective.CommercialKnowledge,
                travel,
                time,
                decisionRecorder,
                logger,
                economyTransactions);
            commercialKnowledge = new CommercialKnowledgeSharingSystem(time, effective.CommercialKnowledge);
        }

        List<INpcActionProvider> actionProviders = new List<INpcActionProvider>
        {
            new TravelActionProvider(travel, merchant)
        };
        if (merchant != null) actionProviders.Add(merchant);

        CrimeSystem crime = new CrimeSystem(
            stagedE.Justice,
            travel,
            config.hiddenStatus,
            logger,
            economyTransactions,
            effective.Crime,
            stagedC.DeterministicRandom,
            time);
        if (effective.Crime.Enabled == true) actionProviders.Add(crime);
        if (effective.GuardCrime.Enabled == true)
            actionProviders.Add(new GuardSystem(stagedE.Justice, config.hiddenStatus, effective.GuardCrime));
        NpcDecisionSystem decisionsSystem = new NpcDecisionSystem(actionProviders, stagedC.DeterministicRandom);
        SimulationRuntimeAdmissionContext restoredAdmissionContext =
            sourceSession.AdmissionContext.CreateRestoredContinuationContext();

        SimulationRuntime runtime = new SimulationRuntime(
            simulationTime: time,
            cities: stagedD.Cities,
            npcRuntimes: stagedD.Npcs,
            configuredActions: config.Actions,
            scheduledDirectiveSystem: directives,
            justiceSystem: stagedE.Justice,
            crimeSystem: crime,
            npcDecisionSystem: decisionsSystem,
            travelSystem: travel,
            travelPartySystem: travelParties,
            merchantSystem: merchant,
            commercialKnowledgeSharingSystem: commercialKnowledge,
            decisionRecorder: decisionRecorder,
            logger: logger,
            explorableSiteStore: stagedD.EmptyExplorableSites,
            explorableSiteKnowledgeSystem: siteKnowledge,
            expeditionSystem: expeditions,
            configuration: effective,
            randomSource: stagedC.DeterministicRandom,
            personStore: stagedD.Persons,
            genealogyStore: stagedD.Genealogy,
            institutionStore: stagedE.Institutions,
            officeStore: stagedE.Offices,
            calendarDefinition: sourceSession.Composition.Calendar,
            propertyOwnershipStore: stagedE.PropertyOwnership,
            estateStore: stagedE.Estates,
            politicalClaimStore: stagedE.PoliticalClaims,
            factionStore: stagedE.Factions,
            politicalSupportStore: stagedE.PoliticalSupport,
            politicalKnowledgeStore: stagedF.PoliticalKnowledge,
            politicalDecisionStore: stagedE.PoliticalDecisions,
            politicalWorldRevision: sourceSession.Runtime.PoliticalWorldRevision,
            armedForceStore: stagedE.ArmedForces,
            conflictStore: stagedE.Conflicts,
            warStore: stagedE.Wars,
            battleStore: stagedE.Battles,
            spatialAuthorityStore: stagedC.SpatialAuthority,
            armedForceSpatialStateStore: stagedE.ArmedForcePositions,
            contingentManpowerStateStore: stagedE.ContingentManpower,
            actorChoiceStore: stagedF.ActorChoices,
            runtimeAdmissionContext: restoredAdmissionContext,
            recordSequence: stagedC.RecordSequence,
            worldId: stagedC.WorldIdentity,
            runtimeIdAllocator: stagedC.RuntimeIdAllocator,
            economyTransactionService: economyTransactions,
            requireP12ReceiptCensusOwners: true,
            runtimeIdentityRegistry: stagedD.RuntimeIdentities,
            spatialNetworkRuntime: stagedD.SpatialNetwork,
            requireP12RuntimeIdentitySpatialCensusOwners: true,
            p12CrimeSocialAppraisalWorldState: stagedE.CrimeSocialAppraisal);

        NpcChronicleService chronicles = new NpcChronicleService(decisions, eventStore);
        NpcChronicleFormatter formatter = TesteSimulacao.CreateNpcChronicleFormatter(
            stagedD.RuntimeIdentities,
            stagedD.SpatialNetwork,
            cityByLocation,
            stagedD.EmptyExplorableSites,
            stagedD.Cities,
            config);
        SimulationBootstrapComposition composition = new SimulationBootstrapComposition(
            stagedC.WorldIdentity,
            stagedC.GenesisManifest,
            time,
            sourceSession.Composition.Calendar,
            stagedD.SpatialNetwork,
            eventStore,
            history,
            stagedF.ScheduledDirectives,
            decisions,
            decisionRecorder,
            stagedC.RecordSequence,
            economyTransactions,
            chronicles,
            formatter,
            stagedF.TravelParties,
            travelParties,
            runtime,
            stagedD.RuntimeIdentities,
            stagedC.RuntimeIdAllocator,
            stagedD.EmptyExplorableSites,
            stagedF.Expeditions,
            expeditions);

        candidate = new SimulationActiveSession(
            composition,
            logger,
            stagedE.Justice,
            stagedD.RuntimeIdentities,
            stagedD.SpatialNetwork,
            cityByLocation,
            config,
            restoredAdmissionContext,
            stagedC.DeterministicRandom,
            new SimulationSessionReportState(sourceSession.ReportState.LastEconomySnapshotDay));
        return true;
    }

    private static bool TryValidateTargetSentinels(
        SimulationBootstrapComposition composition,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        out string diagnostic)
    {
        diagnostic = null;
        if (!TryValidateCurrentRevisionWitness(
                sections,
                new LegacySpatialAnchorBindingCensusProvider(
                    composition.Runtime.LegacySpatialAnchorBindingStore).GetCurrentCensus(),
                0,
                out diagnostic)
            || !TryValidateCurrentRevisionWitness(
                sections,
                new PersonSpatialPositionCensusProvider(
                    composition.Runtime.PersonSpatialPositionStore).GetCurrentCensus(),
                0,
                out diagnostic)
            || !TryValidateWitness(
                sections,
                new SpatialRouteObservationCensusProvider(
                    composition.Runtime.SpatialRouteKnowledgeStore).GetCurrentCensus(),
                0,
                0L,
                out diagnostic)
            || !TryValidateWitness(
                sections,
                new PersonRoutePlanHistoryCensusProvider(
                    composition.Runtime.PersonRoutePlanStore).GetCurrentCensus(),
                0,
                0L,
                out diagnostic)
            || !TryValidateWitness(
                sections,
                composition.GetNpcDecisionOccurrenceReceiptCensus(),
                0,
                0L,
                out diagnostic)
            || !TryValidateWitness(
                sections,
                composition.GetEconomyKeyedSaleReceiptCensus(),
                0,
                0L,
                out diagnostic)
            || !TryValidateActorChoiceTemporalZeroWitness(composition, sections, out diagnostic)
            || !TryValidateWitness(
                sections,
                new P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider(
                    composition.Runtime.CrimeSystemForWorldBoundary).GetCurrentCensus(),
                1,
                0L,
                out diagnostic)
            || !TryValidateWitness(
                sections,
                new P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider(
                    composition.Runtime.JusticeSystemForWorldBoundary).GetCurrentCensus(),
                1,
                0L,
                out diagnostic))
            return false;

        foreach (IOwnerSectionCensusProvider provider in composition.NpcReceiptOwnerCensusProviders)
        {
            if (provider == null || !TryValidateWitness(
                    sections, provider.GetCurrentCensus(), 0, 0L, out diagnostic))
                return false;
        }
        return true;
    }

    private static bool TryValidateCurrentRevisionWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        OwnerSectionCensusWitness witness,
        int expectedCardinality,
        out string diagnostic)
    {
        diagnostic = null;
        if (witness == null || witness.Revision < 0L)
        {
            diagnostic = "A current target census witness with a non-negative local revision is required.";
            return false;
        }

        return TryValidateWitness(
            sections,
            witness,
            expectedCardinality,
            witness.Revision,
            out diagnostic);
    }

    internal static bool TryValidateDailyV1ExpeditionExactZero(
        SimulationBootstrapComposition composition,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        out string diagnostic)
    {
        diagnostic = null;
        if (composition?.Expeditions == null || sections == null)
        {
            diagnostic = "The selected profile's Expedition owner or census is unavailable.";
            return false;
        }

        OwnerSectionCensusWitness witness;
        try
        {
            witness = new ExpeditionCensusProvider(composition.Expeditions).GetCurrentCensus();
        }
        catch (InvalidOperationException exception)
        {
            diagnostic = "The selected profile's Expedition census is inconsistent: " + exception.Message;
            return false;
        }

        OwnerSectionCensusSnapshot match = null;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (!string.Equals(section?.SectionId, ExpeditionCensusProvider.SectionId, StringComparison.Ordinal))
                continue;
            if (match != null)
            {
                diagnostic = "The owner vector duplicates the selected profile's Expedition section.";
                return false;
            }
            match = section;
        }

        if (witness == null
            || witness.SchemaVersion != ExpeditionCensusProvider.SchemaVersion
            || witness.Cardinality != 0
            || witness.Revision < 0L
            || !ReferenceEquals(witness.OwnerInstanceIdentity, composition.Expeditions)
            || match == null
            || match.Role != OwnerSectionRole.Required
            || match.SchemaVersion != witness.SchemaVersion
            || match.Cardinality != 0
            || match.Revision < 0L
            || match.Revision != witness.Revision
            || !ReferenceEquals(match.OwnerInstanceIdentity, witness.OwnerInstanceIdentity))
        {
            diagnostic = "The required p12f.expeditions row must identify the exact live ExpeditionStore at a matching revision and cardinality zero.";
            return false;
        }

        return true;
    }

    internal static bool TryValidateAllocatorHighWater(
        SimulationActiveSession candidate,
        out string diagnostic)
    {
        diagnostic = null;
        if (candidate?.Composition == null || candidate.Runtime == null
            || candidate.Composition.RuntimeIdAllocator == null
            || candidate.IdentityRegistry == null
            || candidate.Composition.SpatialNetwork == null
            || candidate.Composition.ExplorableSites == null
            || candidate.Composition.ScheduledDirectives == null
            || candidate.Composition.DomainEventStore == null
            || candidate.Composition.Decisions == null
            || candidate.Runtime.ActorChoiceStore == null
            || candidate.Composition.TravelParties == null
            || candidate.Composition.Expeditions == null)
        {
            diagnostic = "A complete staged composition is required for allocator high-water validation.";
            return false;
        }

        RuntimeIdAllocatorSnapshot allocator = candidate.Composition.RuntimeIdAllocator.CaptureSnapshot();
        List<KeyValuePair<string, string>> allocatedIdentityValues = new List<KeyValuePair<string, string>>();
        allocatedIdentityValues.AddRange(candidate.IdentityRegistry.CaptureRegisteredRuntimeIdentityValues());
        foreach (ScheduledDirective directive in candidate.Composition.ScheduledDirectives.Directives)
            allocatedIdentityValues.Add(new KeyValuePair<string, string>("directive", directive?.DirectiveId));
        foreach (DomainEvent domainEvent in candidate.Composition.DomainEventStore.Events)
            allocatedIdentityValues.Add(new KeyValuePair<string, string>("event", domainEvent?.EventId));
        foreach (NpcDecisionRecord decision in candidate.Composition.Decisions.Decisions)
            allocatedIdentityValues.Add(new KeyValuePair<string, string>("decision", decision?.DecisionId));
        foreach (TravelPartyRuntime party in candidate.Composition.TravelParties.ActiveParties)
            allocatedIdentityValues.Add(new KeyValuePair<string, string>("travel-party", party?.TravelPartyId));
        foreach (ExpeditionRuntime expedition in candidate.Composition.Expeditions.ActiveExpeditions)
            allocatedIdentityValues.Add(new KeyValuePair<string, string>("expedition", expedition?.ExpeditionId));

        List<string> retainedDecisionReferences = new List<string>();
        foreach (ActorChoiceInput input in candidate.Runtime.ActorChoiceStore.Inputs)
        {
            if (input?.Dispositions == null) continue;
            foreach (ActorChoiceDisposition disposition in input.Dispositions)
                if (disposition != null)
                    retainedDecisionReferences.Add(disposition.DecisionRecordId);
        }
        foreach (TravelPartyRuntime party in candidate.Composition.TravelParties.ActiveParties)
            retainedDecisionReferences.Add(party?.OriginDecisionId);
        foreach (ExpeditionRuntime expedition in candidate.Composition.Expeditions.ActiveExpeditions)
            retainedDecisionReferences.Add(expedition?.OriginDecisionId);

        return TryValidateAllocatorHighWater(
            allocator, allocatedIdentityValues, retainedDecisionReferences, out diagnostic);
    }

    internal static bool TryValidateAllocatorHighWater(
        RuntimeIdAllocatorSnapshot allocator,
        IReadOnlyList<KeyValuePair<string, string>> allocatedIdentityValues,
        out string diagnostic)
    {
        return TryValidateAllocatorHighWater(
            allocator, allocatedIdentityValues, Array.Empty<string>(), out diagnostic);
    }

    internal static bool TryValidateAllocatorHighWater(
        RuntimeIdAllocatorSnapshot allocator,
        IReadOnlyList<KeyValuePair<string, string>> allocatedIdentityValues,
        IReadOnlyList<string> retainedDecisionReferences,
        out string diagnostic)
    {
        diagnostic = null;
        if (allocator?.Counters == null || allocatedIdentityValues == null || retainedDecisionReferences == null)
        {
            diagnostic = "A staged RuntimeIdAllocator and retained identity inventory are required.";
            return false;
        }
        Dictionary<string, long> nextSequenceByFamily = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (RuntimeIdAllocatorCounterSnapshot counter in allocator.Counters)
        {
            if (counter == null || string.IsNullOrWhiteSpace(counter.FamilyId)
                || counter.NextSequence <= 0L
                || !nextSequenceByFamily.TryAdd(counter.FamilyId, counter.NextSequence))
            {
                diagnostic = "The staged RuntimeIdAllocator continuation counters contain an invalid or duplicate family.";
                return false;
            }
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> identity in allocatedIdentityValues)
        {
            string expectedPrefix = identity.Key + "-";
            if (string.IsNullOrWhiteSpace(identity.Value)
                || !nextSequenceByFamily.TryGetValue(identity.Key, out long nextSequence)
                || !identity.Value.StartsWith(expectedPrefix, StringComparison.Ordinal)
                || !long.TryParse(identity.Value.Substring(expectedPrefix.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out long allocatedSequence)
                || allocatedSequence <= 0L
                || allocatedSequence >= nextSequence
                || !seen.Add(identity.Key + ":" + identity.Value))
            {
                diagnostic = "The staged RuntimeIdAllocator next/high-water mark does not continue past every retained "
                    + identity.Key + " identity ('" + identity.Value + "').";
                return false;
            }
        }

        const string decisionPrefix = "decision-";
        foreach (string decisionReference in retainedDecisionReferences)
        {
            // These are retained opaque facts, not lookup keys. Only exact values the
            // allocator can emit contribute a high-water requirement; do not resolve
            // them through the intentionally omitted NpcDecisionStore.
            if (string.IsNullOrWhiteSpace(decisionReference)
                || !decisionReference.StartsWith(decisionPrefix, StringComparison.Ordinal)
                || !long.TryParse(
                    decisionReference.Substring(decisionPrefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long allocatedSequence)
                || allocatedSequence <= 0L
                || !string.Equals(
                    decisionReference,
                    decisionPrefix + allocatedSequence.ToString("D6", CultureInfo.InvariantCulture),
                    StringComparison.Ordinal))
                continue;

            if (!nextSequenceByFamily.TryGetValue("decision", out long nextSequence)
                || allocatedSequence >= nextSequence)
            {
                diagnostic = "The staged RuntimeIdAllocator next/high-water mark does not continue past a retained opaque decision reference ('"
                    + decisionReference + "').";
                return false;
            }
        }

        return true;
    }

    private static bool TryValidateActorChoiceTemporalZeroWitness(
        SimulationBootstrapComposition composition,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        out string diagnostic)
    {
        diagnostic = null;
        if (composition == null || composition.Runtime?.ActorChoiceStore == null || sections == null)
        {
            diagnostic = "The selected profile's excluded ActorChoice temporal owner is unavailable.";
            return false;
        }

        OwnerSectionCensusWitness temporal = composition.ActorChoiceTemporalCensusProvider?.GetCurrentCensus();
        OwnerSectionCensusSnapshot p11Section = null;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (string.Equals(section?.SectionId, ActorChoiceP11CensusProvider.SectionId, StringComparison.Ordinal))
            {
                p11Section = section;
                break;
            }
        }

        if (temporal == null
            || !string.Equals(temporal.SectionId, ActorChoiceTemporalCensusProvider.SectionId, StringComparison.Ordinal)
            || temporal.SchemaVersion != ActorChoiceTemporalCensusProvider.SchemaVersion
            || temporal.Cardinality != 0
            || !ReferenceEquals(temporal.OwnerInstanceIdentity, composition.Runtime.ActorChoiceStore.CensusOwnerIdentity)
            || p11Section == null
            || p11Section.Role != OwnerSectionRole.Required
            || temporal.Revision != p11Section.Revision
            || !ReferenceEquals(temporal.OwnerInstanceIdentity, p11Section.OwnerInstanceIdentity))
        {
            diagnostic = "The non-serialized ActorChoice temporal input count must be zero on the required P11 owner at its captured census revision.";
            return false;
        }

        return true;
    }

    private static bool TryValidateWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        OwnerSectionCensusWitness witness,
        int expectedCardinality,
        long expectedRevision,
        out string diagnostic)
    {
        diagnostic = null;
        if (witness == null || sections == null)
        {
            diagnostic = "An exact target census witness or owner vector is missing.";
            return false;
        }

        OwnerSectionCensusSnapshot match = null;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (!string.Equals(section.SectionId, witness.SectionId, StringComparison.Ordinal)) continue;
            if (match != null)
            {
                diagnostic = "The target owner vector duplicates section '" + witness.SectionId + "'.";
                return false;
            }
            match = section;
        }

        if (match == null
            || match.SchemaVersion != witness.SchemaVersion
            || !ReferenceEquals(match.OwnerInstanceIdentity, witness.OwnerInstanceIdentity)
            || match.Cardinality != expectedCardinality
            || match.Revision != expectedRevision
            || witness.Cardinality != expectedCardinality
            || witness.Revision != expectedRevision)
        {
            diagnostic = "The target owner vector does not identify the exact empty/sentinel owner for '"
                + witness.SectionId + "' (match="
                + (match == null ? "missing" : match.SchemaVersion + "/" + match.Role + "/" + match.Cardinality + "/" + match.Revision)
                + ", witness=" + witness.SchemaVersion + "/" + witness.Cardinality + "/" + witness.Revision
                + ", expected=" + expectedCardinality + "/" + expectedRevision
                + ", ownerSame=" + (match != null && ReferenceEquals(match.OwnerInstanceIdentity, witness.OwnerInstanceIdentity))
                + ").";
            return false;
        }
        return true;
    }

    private static bool TryValidatePoliticalKnowledgeBindings(
        IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> bindings,
        SimulationRuntime runtime,
        out string diagnostic)
    {
        diagnostic = null;
        if (bindings == null || runtime == null)
        {
            diagnostic = "P12-E typed PoliticalKnowledge bindings and a staged runtime are required.";
            return false;
        }

        Dictionary<string, P12EUnresolvedPoliticalKnowledgeBinding> byDecisionId =
            new Dictionary<string, P12EUnresolvedPoliticalKnowledgeBinding>(StringComparer.Ordinal);
        foreach (P12EUnresolvedPoliticalKnowledgeBinding binding in bindings)
        {
            string id = binding?.DecisionId?.Value;
            if (string.IsNullOrWhiteSpace(id) || binding.KnowledgeReferences == null
                || binding.KnowledgeReferences.Count == 0
                || !byDecisionId.TryAdd(id, binding))
            {
                diagnostic = "P12-E produced a missing, empty, or duplicate typed Knowledge binding.";
                return false;
            }
        }

        HashSet<string> matched = new HashSet<string>(StringComparer.Ordinal);
        long worldRevision = runtime.PoliticalWorldRevision;
        long knowledgeRevision = runtime.PoliticalKnowledgeRevision;
        foreach (PoliticalDecisionRecord decision in runtime.PoliticalDecisionRecords)
        {
            if (decision == null || decision.DecisionId == null || decision.KnowledgeReferences == null)
            {
                diagnostic = "The staged PoliticalDecision owner contains a malformed record.";
                return false;
            }

            string id = decision.DecisionId.Value;
            bool hasReferences = decision.KnowledgeReferences.Count > 0;
            if (!hasReferences)
            {
                if (byDecisionId.ContainsKey(id))
                {
                    diagnostic = "P12-E retained a Knowledge binding for a decision with no Knowledge references.";
                    return false;
                }
                continue;
            }

            if (!byDecisionId.TryGetValue(id, out P12EUnresolvedPoliticalKnowledgeBinding binding)
                || !matched.Add(id)
                || binding.ExpectedKnowledgeRevision != decision.ExpectedKnowledgeRevision
                || binding.ExpectedWorldRevision != decision.ExpectedWorldRevision
                || decision.ExpectedKnowledgeRevision > knowledgeRevision
                || decision.ExpectedWorldRevision > worldRevision
                || !runtime.TryGetPoliticalKnowledge(decision.Decider, out _)
                || !AreEqualOrdinal(binding.KnowledgeReferences, decision.KnowledgeReferences))
            {
                diagnostic = "A staged PoliticalDecision's typed P12-F Knowledge binding, holder, or revision does not resolve. ";
                return false;
            }
        }

        if (matched.Count != byDecisionId.Count)
        {
            diagnostic = "P12-E contains a Knowledge binding without exactly one staged PoliticalDecision record.";
            return false;
        }
        return true;
    }

    private static bool AreEqualOrdinal(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal)) return false;
        return true;
    }
}
