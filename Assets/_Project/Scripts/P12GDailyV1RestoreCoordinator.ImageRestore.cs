using System;
using System.Collections.Generic;
using System.Linq;

internal static partial class P12GDailyV1RestoreCoordinator
{
    internal static bool TryCreateRestoredSessionFromImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor expectedCompatibility,
        SimulationConfigData configuration,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar,
        DailyV1EffectiveDefinitions selectedDefinitions,
        SimulationRuntimeAdmissionContext bootstrapAdmissionContext,
        out SimulationActiveSession restoredSession,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        DailyV1Application application = null,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation = null,
        Action<SimulationActiveSession> privateCandidateObserverForTest = null,
        DailyV1ApplicationStartAdmission initialStartAdmission = null)
        => TryCreateRestoredSessionFromImageCore(image, expectedCompatibility, null,
            configuration, effective, calendar, selectedDefinitions, bootstrapAdmissionContext,
            out restoredSession, out failure, out diagnostic, stageObserver,
            application, replacementOperation, privateCandidateObserverForTest, initialStartAdmission);

    internal static bool TryCreateRestoredSessionFromDotNetV2Image(
        P12AContinuationImage image,
        P12ACompatibilityDescriptorV2 expectedCompatibility,
        SimulationConfigData configuration,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar,
        DailyV1EffectiveDefinitions selectedDefinitions,
        SimulationRuntimeAdmissionContext bootstrapAdmissionContext,
        out SimulationActiveSession restoredSession,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        DailyV1Application application = null,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation = null,
        Action<SimulationActiveSession> privateCandidateObserverForTest = null,
        DailyV1ApplicationStartAdmission initialStartAdmission = null)
        => TryCreateRestoredSessionFromImageCore(image, null, expectedCompatibility,
            configuration, effective, calendar, selectedDefinitions, bootstrapAdmissionContext,
            out restoredSession, out failure, out diagnostic, stageObserver,
            application, replacementOperation, privateCandidateObserverForTest, initialStartAdmission);

    /// <summary>
    /// Builds a private Daily-v1 candidate solely from a fully decoded persisted
    /// image and the current authored host profile. No source session, token,
    /// owner vector, or capture attempt participates in this path.
    /// </summary>
    private static bool TryCreateRestoredSessionFromImageCore(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor expectedCompatibilityV1,
        P12ACompatibilityDescriptorV2 expectedCompatibilityV2,
        SimulationConfigData configuration,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar,
        DailyV1EffectiveDefinitions selectedDefinitions,
        SimulationRuntimeAdmissionContext bootstrapAdmissionContext,
        out SimulationActiveSession restoredSession,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        DailyV1Application application = null,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation = null,
        Action<SimulationActiveSession> privateCandidateObserverForTest = null,
        DailyV1ApplicationStartAdmission initialStartAdmission = null)
    {
        restoredSession = null;
        failure = P12GDailyV1RestoreFailure.InvalidSourceSession;
        diagnostic = "A validated persisted Daily-v1 image and current host profile are required.";
        if (image == null || (expectedCompatibilityV1 == null) == (expectedCompatibilityV2 == null)
            || configuration == null
            || effective == null || calendar == null || selectedDefinitions == null || bootstrapAdmissionContext == null
            || bootstrapAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || bootstrapAdmissionContext.IsRestoredContinuation
            || !bootstrapAdmissionContext.IsOwnedByCurrentThread())
            return false;

        try
        {
            P12ADailyV1Census imageCensus;
            P12ADailyV1ImageOwnerSnapshots decoded;
            P12AResult imageResult;
            P12CStagedContinuationRoot stagedC;
            DailyV1RestoredImageCoreOwnerStages stagedCoreOwnerStages = null;
            CoreApplicationRestoreProjection coreProjection = null;
            if (application != null)
            {
                if (!TryRunCoreApplicationRestoreTransaction(
                        image, expectedCompatibilityV1, expectedCompatibilityV2,
                        configuration, selectedDefinitions, bootstrapAdmissionContext,
                        application, replacementOperation, initialStartAdmission,
                        stageObserver, out coreProjection, out failure, out diagnostic))
                    return false;

                imageCensus = coreProjection.Census;
                decoded = coreProjection.Snapshots;
                imageResult = null;
                stagedC = coreProjection.UnityRoots;
                stagedCoreOwnerStages = coreProjection.Stages;
            }
            else
            {
                bool descriptorMatches = image.Header != null && image.Header.IsValid
                    && (expectedCompatibilityV1 != null
                        ? image.Header.Compatibility != null
                            && expectedCompatibilityV1.Matches(image.Header.Compatibility.CanonicalBytes)
                        : image.Header.CompatibilityV2 != null
                            && P12ACompatibilityAdmission.AdmitDotNetV2(
                                image.Header.CompatibilityV2.CanonicalBytes,
                                expectedCompatibilityV2, out _) == P12AResultCode.Decoded);
                if (!descriptorMatches) return false;
                if (!P12ADailyV1Census.TryCreateForImage(image, out imageCensus, out imageResult))
                {
                    failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                    diagnostic = "The persisted image does not satisfy the current Daily-v1 owner manifest: "
                        + (imageResult?.SectionCode ?? imageResult?.Code.ToString() ?? "InvalidOwnerValue") + ".";
                    return false;
                }
                if (!P12ADailyV1ImageOwnerDecoder.TryDecode(image, out decoded, out imageResult)
                    || decoded == null)
                {
                    failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                    diagnostic = "The persisted Daily-v1 typed owner image could not be decoded: "
                        + (imageResult?.SectionCode ?? imageResult?.Code.ToString() ?? "OwnerGraphRejected") + ".";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.SourceCaptured);
                if (!P12ACausalRootImageAdapter.TryStage(null,
                        null,
                        null,
                        image,
                        out stagedC, out imageResult, stageObserver)
                    || stagedC == null || !stagedC.IsDetached)
                {
                    failure = P12GDailyV1RestoreFailure.RootStageFailed;
                    diagnostic = "P12-C could not privately stage the persisted roots: "
                        + (imageResult?.SectionCode ?? imageResult?.Code.ToString() ?? "OwnerGraphRejected") + ".";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.RootsStaged);
            }

            List<CityData> cityDefinitions = coreProjection?.CityDefinitions;
            List<ItemData> itemDefinitions = coreProjection?.ItemDefinitions;
            List<NpcData> npcDefinitions = coreProjection?.NpcDefinitions;
            List<NpcStatusData> statusDefinitions = coreProjection?.StatusDefinitions;
            if (coreProjection == null && !TryBuildDailyV1Definitions(configuration,
                    out cityDefinitions, out itemDefinitions, out npcDefinitions,
                    out statusDefinitions, out string definitionDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "The current authored Daily-v1 definition catalog is incomplete or ambiguous: "
                    + definitionDiagnostic;
                return false;
            }

            DailyV1P12DCoreRestoreOwnerStage stagedCoreDOwners = coreProjection?.CoreDOwners;
            RuntimeIdentityRegistry stagedIdentities = coreProjection?.Identities
                ?? new RuntimeIdentityRegistry();
            P12DDailyV1OwnerPackage stagedD = coreProjection?.D;
            SimulationTime stagedTime = coreProjection?.Time
                ?? stagedCoreOwnerStages?.StagedTime ?? new SimulationTime(image.Header.Day);
            SimulationLogger logger = coreProjection?.Logger
                ?? UnitySimulationLoggerAdapter.Create(configuration.LogSettings);
            NpcDecisionStore decisions = coreProjection?.Decisions;
            DailyV1DomainEventAuthority eventAuthority = coreProjection?.EventAuthority;
            DomainEventRecorder eventRecorder = coreProjection?.EventRecorder;
            NpcDecisionRecorder decisionRecorder = coreProjection?.DecisionRecorder;
            EconomyTransactionService economyTransactions = coreProjection?.EconomyTransactions;
            P12EDailyV1OwnerStagingContext eContext = coreProjection?.EContext;
            P12EDailyV1OwnerPackage stagedE = coreProjection?.E;
            P12FDailyV1OwnerPackage stagedF = coreProjection?.F;
            List<string> stagedPartyIds = coreProjection?.TravelPartyIds
                ?? decoded.TravelParties.Parties.Select(party => party?.TravelPartyId).ToList();

            if (application != null)
            {
                if (coreProjection == null || stagedCoreOwnerStages == null
                    || !stagedCoreOwnerStages.IsComplete
                    || stagedCoreOwnerStages.CoreSession == null
                    || stagedCoreOwnerStages.CoreCalendar == null
                    || stagedD == null || stagedE == null || stagedF == null)
                {
                    failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                    diagnostic = "The Core application did not complete the exact private restored-image transaction.";
                    return false;
                }
            }
            else
            {
                if (!P12DDailyV1OwnerPackage.TryStageFromImage(
                        image.Sections, decoded.Cities, decoded.NpcDRows, decoded.NpcFRows,
                        decoded.SpatialNetwork, decoded.Persons, decoded.Genealogy, decoded.Sites,
                        stagedIdentities, stagedC.WorldIdentity, cityDefinitions, itemDefinitions,
                        npcDefinitions, configuration.Actions, statusDefinitions,
                        Array.Empty<DailyV1ExplorableSiteDefinition>(), stagedPartyIds,
                        out stagedD,
                        out P12DDailyV1OwnerPackageFailure dFailure,
                        selectedDefinitions,
                        null,
                        stagedC.CoreRoot,
                        replacementOperation?.ExpectedSession,
                        replacementOperation,
                        null)
                    || stagedD == null || stagedD.StagingAttempt != null)
                {
                    failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                    diagnostic = "P12-D could not hydrate the detached factual roots: " + dFailure + ".";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.DStaged);

                logger.BeginSimulation(configuration.simulationName, configuration.EnabledModules,
                    configuration.Cities.Count, configuration.Npcs.Count);
                decisions = new NpcDecisionStore(logger);
                eventAuthority = DailyV1DomainEventAuthority.Create(
                    stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, decisions, logger);
                eventRecorder = eventAuthority.Recorder;
                decisionRecorder = new NpcDecisionRecorder(
                    eventAuthority.DecisionOccurrenceReceipts, logger);
                economyTransactions = new EconomyTransactionService();
                eContext = new P12EDailyV1OwnerStagingContext(
                    null, stagedC, stagedD, stagedTime, configuration.freeStatus,
                    configuration.wantedStatus, configuration.arrestedStatus,
                    configuration.hiddenStatus, eventRecorder, logger,
                    null, replacementOperation?.ExpectedSession, replacementOperation,
                    null, null);

                if (!P12EDailyV1OwnerPackage.TryStageFromImage(image, decoded, eContext,
                        out stagedE,
                        out P12EDailyV1OwnerPackageFailure eFailure)
                    || stagedE == null || stagedE.StagingAttempt != null)
                {
                    failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                    diagnostic = "P12-E could not hydrate the detached included owners: " + eFailure + ".";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.EStaged);

                if (!P12FDailyV1OwnerPackage.TryStageFromImage(image, decoded, eContext,
                        stagedE, selectedDefinitions,
                        out stagedF,
                        out P12FDailyV1OwnerPackageFailure fFailure)
                    || stagedF == null || stagedF.StagingAttempt != null)
                {
                    failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                    diagnostic = "P12-F could not hydrate the detached knowledge and commitment owners: " + fFailure + ".";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.FStaged);
                stageObserver?.Invoke(P12GDailyV1RestoreStage.OwnersStaged);
            }

            if (decisions == null || eventAuthority == null || stagedD == null || stagedE == null || stagedF == null
                || eventRecorder == null || decisionRecorder == null || economyTransactions == null || eContext == null)
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "The ordered restored-image operation did not produce every required Core owner projection.";
                return false;
            }

            if (!TryBuildRestoredSession(configuration, effective, calendar, selectedDefinitions,
                    bootstrapAdmissionContext, image.Header.PoliticalWorldRevision,
                    image.Header.LastEconomySnapshotDay, stagedC, stagedD, stagedE, stagedF,
                    stagedTime, eventAuthority, decisions, decisionRecorder,
                    economyTransactions, logger, null,
                    application, replacementOperation,
                    image, stagedCoreOwnerStages,
                    out SimulationActiveSession candidate, out diagnostic)
                || candidate == null)
            {
                failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CandidateComposed);
            privateCandidateObserverForTest?.Invoke(candidate);

            SimulationRuntime targetRuntime = candidate.Runtime;
            if (!TryValidatePersonNpcGenealogyBindings(targetRuntime, out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetPersonNpcGenealogyBindingsValidated);

            IReadOnlyList<OwnerSectionCensusSnapshot> targetSections;
            long targetEpoch;
            if (application != null)
            {
                if (candidate.CoreRuntimeSession == null
                    || !application.TryCaptureAndValidateRestoredImageOwnerVector(
                        candidate.CoreRuntimeSession,
                        targetRuntime,
                        imageCensus,
                        replacementOperation?.ExpectedSession,
                        replacementOperation,
                        _ => stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorCaptured),
                        out targetSections,
                        out targetEpoch,
                        out _,
                        out diagnostic))
                {
                    failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                    return false;
                }
            }
            else
            {
                if (!targetRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                        out targetSections, out targetEpoch,
                        out ContinuationCensusFailure initialCensusFailure))
                {
                    failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                    diagnostic = "The private target did not produce a complete quiescent owner vector: "
                        + initialCensusFailure + ".";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorCaptured);

                if (!P12GDailyV1OwnerVector.TryValidateRestoredImageCandidate(
                        imageCensus, targetSections, out _, out diagnostic))
                {
                    failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                    return false;
                }
            }

            if ((application == null
                    && (!TryValidateDailyV1ExpeditionExactZero(candidate.Composition, targetSections,
                            out diagnostic)
                        || !TryValidateAllocatorHighWater(candidate, out diagnostic)))
                || !TryValidateTargetSentinels(candidate.Composition, targetSections, out diagnostic)
                || !TryValidatePoliticalKnowledgeBindings(
                    stagedE.UnresolvedPoliticalKnowledgeBindings, targetRuntime, out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorValidated);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetSentinelsValidated);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetPoliticalKnowledgeBindingsValidated);

            SimulationRuntimeSpatialInvariantReport spatialReport = targetRuntime.ValidateSpatialInvariants();
            if (spatialReport == null || !spatialReport.IsValid)
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                diagnostic = "The private target failed spatial invariant validation.";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetSpatialInvariantsValidated);

            if (!targetRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure)
                || !TryValidateTravelPartyBindings(candidate, out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = "The private target failed the NPC roster census: " + censusFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetNpcCensusValidated);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetTravelPartyBindingsValidated);

            if (application != null)
            {
                if (candidate.CoreRuntimeSession == null)
                {
                    failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
                    return false;
                }

                Action<DailyV1RestoredBoundaryFinalizationStage> finalizationObserver =
                    finalizationStage => stageObserver?.Invoke(
                        MapRestoreFinalizationStage(finalizationStage));
                if (initialStartAdmission != null && replacementOperation == null)
                {
                    if (stagedCoreOwnerStages == null
                        || !application.TryBindInitialRestoredImageCommitProof(
                            initialStartAdmission,
                            stagedCoreOwnerStages,
                            candidate.CoreRuntimeSession,
                            targetSections,
                            targetEpoch,
                            finalizationObserver,
                            out diagnostic))
                    {
                        failure = P12GDailyV1RestoreFailure.TargetAdmissionFailed;
                        if (string.IsNullOrWhiteSpace(diagnostic))
                            diagnostic = "The Core application could not bind the validated initial image target to its private restore transaction.";
                        return false;
                    }
                }
                else
                {
                    if (!application.TryFinalizeRestoredCandidateBoundary(
                            candidate.CoreRuntimeSession,
                            targetRuntime,
                            stagedC.WorldIdentity,
                            image.Header.Day,
                            image.Header.Sequence,
                            targetSections,
                            targetEpoch,
                            replacementOperation?.ExpectedSession,
                            replacementOperation,
                            finalizationObserver,
                            out IReadOnlyList<OwnerSectionCensusSnapshot> recapturedSections,
                            out long recapturedEpoch,
                            out DailyV1RestoredBoundaryFinalizationFailure finalizationFailure,
                            out diagnostic))
                    {
                        failure = MapRestoreFinalizationFailure(finalizationFailure);
                        return false;
                    }

                    targetSections = recapturedSections;
                    targetEpoch = recapturedEpoch;
                }
            }
            else
            {
                if (!targetRuntime.TryBindRestoredCandidateMutationGuard(out diagnostic))
                {
                    failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.CandidateGuardBound);

                bool targetOwnerVectorRecaptured = targetRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                    out IReadOnlyList<OwnerSectionCensusSnapshot> recapturedSections,
                    out long recapturedEpoch, out censusFailure);
                bool targetOwnerVectorStillCurrent = targetOwnerVectorRecaptured
                    && P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
                        targetSections, targetEpoch, recapturedSections, recapturedEpoch,
                        out _, out diagnostic);
                if (!targetOwnerVectorStillCurrent)
                {
                    failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                    if (string.IsNullOrWhiteSpace(diagnostic))
                        diagnostic = "The detached target owner vector changed while binding its mutation guard.";
                    return false;
                }
                targetSections = recapturedSections;
                targetEpoch = recapturedEpoch;
                stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured);

                if (!targetRuntime.TryAdmitRestoredDailyBoundary(stagedC.WorldIdentity,
                        image.Header.Day, image.Header.Sequence, targetSections, targetEpoch,
                        out DailyCaptureEligibilityFailure admissionFailure))
                {
                    failure = P12GDailyV1RestoreFailure.TargetAdmissionFailed;
                    diagnostic = "The detached target boundary admission failed: " + admissionFailure + ".";
                    return false;
                }
                if (!targetRuntime.HasValidRestoredDailyBoundaryAdmission()
                    || !targetRuntime.IsHealthyDailyOwnerThreadBoundary())
                {
                    failure = P12GDailyV1RestoreFailure.TargetAdmissionFailed;
                    diagnostic = "The restored target did not retain a healthy admitted completed boundary.";
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.BoundaryAdmitted);
            }

            restoredSession = candidate;
            failure = P12GDailyV1RestoreFailure.None;
            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            restoredSession = null;
            failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
            diagnostic = "Detached Daily-v1 restoration failed without publishing the candidate: "
                + exception.Message;
            return false;
        }
    }

    private static P12GDailyV1RestoreStage MapRestoreFinalizationStage(
        DailyV1RestoredBoundaryFinalizationStage stage)
    {
        switch (stage)
        {
            case DailyV1RestoredBoundaryFinalizationStage.MutationGuardBound:
                return P12GDailyV1RestoreStage.CandidateGuardBound;
            case DailyV1RestoredBoundaryFinalizationStage.OwnerVectorRecaptured:
                return P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured;
            case DailyV1RestoredBoundaryFinalizationStage.BoundaryAdmitted:
                return P12GDailyV1RestoreStage.BoundaryAdmitted;
            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage,
                    "Unknown Core restored-boundary finalization stage.");
        }
    }

    private static P12GDailyV1RestoreFailure MapRestoreFinalizationFailure(
        DailyV1RestoredBoundaryFinalizationFailure failure) =>
        failure == DailyV1RestoredBoundaryFinalizationFailure.MutationGuardBindingFailed
            ? P12GDailyV1RestoreFailure.BindingValidationFailed
            : failure == DailyV1RestoredBoundaryFinalizationFailure.OwnerVectorValidationFailed
                ? P12GDailyV1RestoreFailure.TargetOwnerVectorFailed
                : failure == DailyV1RestoredBoundaryFinalizationFailure.StagingAuthorityUnavailable
                    || failure == DailyV1RestoredBoundaryFinalizationFailure.CandidateHostBoundaryUnavailable
                    || failure == DailyV1RestoredBoundaryFinalizationFailure.BoundaryAdmissionFailed
                    ? P12GDailyV1RestoreFailure.TargetAdmissionFailed
                    : P12GDailyV1RestoreFailure.TargetAdmissionFailed;

    private sealed class CoreApplicationRestoreProjection
    {
        internal P12ADailyV1Census Census;
        internal P12ADailyV1ImageOwnerSnapshots Snapshots;
        internal DailyV1RestoredImageCoreOwnerStages Stages;
        internal P12CStagedContinuationRoot UnityRoots;
        internal DailyV1P12DCoreRestoreOwnerStage CoreDOwners;
        internal RuntimeIdentityRegistry Identities = new RuntimeIdentityRegistry();
        internal P12DDailyV1OwnerPackage D;
        internal SimulationTime Time;
        internal SimulationLogger Logger;
        internal NpcDecisionStore Decisions;
        internal DailyV1DomainEventAuthority EventAuthority;
        internal DomainEventRecorder EventRecorder;
        internal NpcDecisionRecorder DecisionRecorder;
        internal EconomyTransactionService EconomyTransactions;
        internal P12EDailyV1OwnerStagingContext EContext;
        internal P12EDailyV1OwnerPackage E;
        internal P12FDailyV1OwnerPackage F;
        internal List<CityData> CityDefinitions;
        internal List<ItemData> ItemDefinitions;
        internal List<NpcData> NpcDefinitions;
        internal List<NpcStatusData> StatusDefinitions;
        internal List<string> TravelPartyIds;
        internal P12GDailyV1RestoreFailure ProjectionFailure;
        internal string ProjectionDiagnostic;
        internal bool HostPreparationFailed;
        internal string HostPreparationDiagnostic;
    }

    private static bool TryRunCoreApplicationRestoreTransaction(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor expectedCompatibilityV1,
        P12ACompatibilityDescriptorV2 expectedCompatibilityV2,
        SimulationConfigData configuration,
        DailyV1EffectiveDefinitions selectedDefinitions,
        SimulationRuntimeAdmissionContext bootstrapAdmissionContext,
        DailyV1Application application,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        DailyV1ApplicationStartAdmission initialStartAdmission,
        Action<P12GDailyV1RestoreStage> stageObserver,
        out CoreApplicationRestoreProjection projection,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        CoreApplicationRestoreProjection current = new CoreApplicationRestoreProjection();
        projection = current;
        failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
        diagnostic = null;
        DailyV1RestoredImageCoreOwnerStageBoundary failedBoundary =
            DailyV1RestoredImageCoreOwnerStageBoundary.None;

        Action<string> coreStageObserver = stageName =>
        {
            if (!P12CContinuationRootStager.TryMapRestoreStageName(
                    stageName, out P12GDailyV1RestoreStage stage))
                throw new InvalidOperationException(
                    "Core P12-C staging emitted an unknown restore stage: " + stageName);
            stageObserver?.Invoke(stage);
        };

        string ProjectCoreRoots(P12CStagedCoreContinuationRoot roots)
        {
            if (!P12ACausalRootImageAdapter.TryProject(
                    roots, out current.UnityRoots, out P12AResult result)
                || current.UnityRoots == null || !current.UnityRoots.IsDetached)
                return "P12-C could not create the Unity compatibility projection for the already staged Core roots: "
                    + (result?.SectionCode ?? result?.Code.ToString() ?? "OwnerGraphRejected") + ".";

            stageObserver?.Invoke(P12GDailyV1RestoreStage.RootsStaged);
            return null;
        }

        string PrepareHostProjection(DailyV1RestoredImageCoreOwnerStages stages)
        {
            current.Stages = stages;
            current.Census = stages?.Census;
            current.Snapshots = stages?.Snapshots;
            current.Time = stages?.StagedTime;
            if (current.Snapshots == null || current.Time == null)
            {
                current.HostPreparationFailed = true;
                current.HostPreparationDiagnostic =
                    "The admitted Core restore operation did not retain its exact image-owner snapshots and time.";
                return current.HostPreparationDiagnostic;
            }

            if (!TryBuildDailyV1Definitions(configuration,
                    out current.CityDefinitions,
                    out current.ItemDefinitions,
                    out current.NpcDefinitions,
                    out current.StatusDefinitions,
                    out string definitionDiagnostic))
            {
                current.HostPreparationFailed = true;
                current.HostPreparationDiagnostic =
                    "The current authored Daily-v1 definition catalog is incomplete or ambiguous: "
                    + definitionDiagnostic;
                return current.HostPreparationDiagnostic;
            }

            current.TravelPartyIds = current.Snapshots.TravelParties.Parties
                .Select(party => party?.TravelPartyId).ToList();
            return null;
        }

        SimulationLogger CreateDiagnosticLogger()
        {
            current.Logger = UnitySimulationLoggerAdapter.Create(configuration.LogSettings);
            return current.Logger;
        }

        string ProjectCompletedCoreOwnerStage(
            DailyV1RestoredImageCoreOwnerStages stages,
            DailyV1RestoredImageCoreOwnerStageBoundary boundary)
        {
            switch (boundary)
            {
                case DailyV1RestoredImageCoreOwnerStageBoundary.P12DOwners:
                    current.CoreDOwners = stages.D;
                    if (current.CoreDOwners == null)
                    {
                        current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                        current.ProjectionDiagnostic = "The Core application completed P12-D without its owner-stage value.";
                        return current.ProjectionDiagnostic;
                    }
                    current.Identities = new RuntimeIdentityRegistry(null,
                        current.CoreDOwners.RuntimeIdentityIndex);
                    if (!P12DDailyV1OwnerPackage.TryStageFromImage(
                            image.Sections, current.Snapshots.Cities, current.Snapshots.NpcDRows,
                            current.Snapshots.NpcFRows, current.Snapshots.SpatialNetwork,
                            current.Snapshots.Persons, current.Snapshots.Genealogy,
                            current.Snapshots.Sites, current.Identities,
                            current.UnityRoots.WorldIdentity, current.CityDefinitions,
                            current.ItemDefinitions, current.NpcDefinitions, configuration.Actions,
                            current.StatusDefinitions, Array.Empty<DailyV1ExplorableSiteDefinition>(),
                            current.TravelPartyIds, out current.D,
                            out P12DDailyV1OwnerPackageFailure dFailure, selectedDefinitions,
                            application, current.UnityRoots.CoreRoot,
                            replacementOperation?.ExpectedSession, replacementOperation,
                            current.CoreDOwners)
                        || current.D == null || current.D.StagingAttempt != null)
                    {
                        current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                        current.ProjectionDiagnostic = "P12-D could not project the completed Core owners: "
                            + dFailure + ".";
                        return current.ProjectionDiagnostic;
                    }

                    stageObserver?.Invoke(P12GDailyV1RestoreStage.DStaged);
                    current.Logger.BeginSimulation(configuration.simulationName,
                        configuration.EnabledModules, configuration.Cities.Count,
                        configuration.Npcs.Count);
                    return null;

                case DailyV1RestoredImageCoreOwnerStageBoundary.P12EOwners:
                    current.Decisions = stages.DecisionStore;
                    current.EventAuthority = stages.EventAuthority;
                    if (!stages.HasRuntimeAuthorities(current.Decisions, current.EventAuthority))
                    {
                        current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                        current.ProjectionDiagnostic = "The Core application did not retain the exact restored record authorities.";
                        return current.ProjectionDiagnostic;
                    }
                    current.EventRecorder = current.EventAuthority.Recorder;
                    current.DecisionRecorder = new NpcDecisionRecorder(
                        current.EventAuthority.DecisionOccurrenceReceipts, current.Logger);
                    current.EconomyTransactions = new EconomyTransactionService();
                    current.EContext = new P12EDailyV1OwnerStagingContext(
                        null, current.UnityRoots, current.D, current.Time,
                        configuration.freeStatus, configuration.wantedStatus,
                        configuration.arrestedStatus, configuration.hiddenStatus,
                        current.EventRecorder, current.Logger, application,
                        replacementOperation?.ExpectedSession, replacementOperation,
                        null, stages);
                    if (!P12EDailyV1OwnerPackage.TryStageFromImage(image,
                            current.Snapshots, current.EContext,
                            out current.E, out P12EDailyV1OwnerPackageFailure eFailure)
                        || current.E == null || current.E.StagingAttempt != null)
                    {
                        current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                        current.ProjectionDiagnostic = "P12-E could not project the completed Core owners: "
                            + eFailure + ".";
                        return current.ProjectionDiagnostic;
                    }

                    stageObserver?.Invoke(P12GDailyV1RestoreStage.EStaged);
                    return null;

                case DailyV1RestoredImageCoreOwnerStageBoundary.P12FOwners:
                    if (current.EContext == null || current.E == null)
                    {
                        current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                        current.ProjectionDiagnostic = "P12-F projection requires the exact completed P12-E projection.";
                        return current.ProjectionDiagnostic;
                    }
                    if (!P12FDailyV1OwnerPackage.TryStageFromImage(image,
                            current.Snapshots, current.EContext, current.E,
                            selectedDefinitions, out current.F,
                            out P12FDailyV1OwnerPackageFailure fFailure)
                        || current.F == null || current.F.StagingAttempt != null)
                    {
                        current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                        current.ProjectionDiagnostic = "P12-F could not project the completed Core owners: "
                            + fFailure + ".";
                        return current.ProjectionDiagnostic;
                    }

                    stageObserver?.Invoke(P12GDailyV1RestoreStage.FStaged);
                    stageObserver?.Invoke(P12GDailyV1RestoreStage.OwnersStaged);
                    return null;

                default:
                    current.ProjectionFailure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                    current.ProjectionDiagnostic = "The Core application returned an unknown restored-image owner-stage boundary.";
                    return current.ProjectionDiagnostic;
            }
        }

        bool accepted = application.TryRunRestoredImageCoreOwnerTransaction(
            image, expectedCompatibilityV1, expectedCompatibilityV2, selectedDefinitions,
            bootstrapAdmissionContext, replacementOperation?.ExpectedSession,
            replacementOperation, initialStartAdmission, CreateDiagnosticLogger,
            image.Header.LastEconomySnapshotDay,
            () => stageObserver?.Invoke(P12GDailyV1RestoreStage.SourceCaptured),
            coreStageObserver, ProjectCoreRoots, PrepareHostProjection,
            ProjectCompletedCoreOwnerStage,
            step => P12EDailyV1OwnerPackage.ObserveCoreOwnerRestoreStep(stageObserver, step),
            step => P12FDailyV1OwnerPackage.ObserveP12FCoreOwnerRestoreStep(stageObserver, step),
            out bool admissionPassed, out bool rootsPrepared,
            out P12ADailyV1Census census,
            out P12ADailyV1ImageOwnerSnapshots snapshots,
            out DailyV1RestoredImageCoreOwnerStages stages,
            out failedBoundary, out P12AResult result, out string coreDiagnostic);

        current.Census = census;
        current.Snapshots = snapshots;
        current.Stages = stages;
        if (!accepted)
        {
            if (!admissionPassed)
            {
                bool hostIdentityRejected = result?.Code == P12AResultCode.WrongThread
                    || result?.Code == P12AResultCode.IncompatibleContentOrConfiguration
                    || result?.Code == P12AResultCode.IncompatibleBuildOrRuntime;
                failure = hostIdentityRejected
                    ? P12GDailyV1RestoreFailure.InvalidSourceSession
                    : result?.Code == P12AResultCode.Busy
                        ? P12GDailyV1RestoreFailure.RootStageFailed
                        : P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = result?.Code == P12AResultCode.OwnerGraphRejected
                    ? "The persisted image does not satisfy the current Daily-v1 owner manifest: "
                        + (result.SectionCode ?? result.Code.ToString()) + "."
                    : result?.Code == P12AResultCode.Busy
                        ? "The Core application no longer holds the exact restore staging authority."
                        : "The persisted Daily-v1 image could not pass Core host and census admission: "
                            + (result?.SectionCode ?? result?.Code.ToString() ?? "InvalidOwnerValue") + ".";
                return false;
            }

            if (current.HostPreparationFailed)
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = current.HostPreparationDiagnostic;
                return false;
            }
            if (current.ProjectionFailure != P12GDailyV1RestoreFailure.None)
            {
                failure = current.ProjectionFailure;
                diagnostic = current.ProjectionDiagnostic;
                return false;
            }
            if (!rootsPrepared || failedBoundary == DailyV1RestoredImageCoreOwnerStageBoundary.P12CRootProjection)
            {
                failure = P12GDailyV1RestoreFailure.RootStageFailed;
                diagnostic = coreDiagnostic;
                return false;
            }
            if (failedBoundary == DailyV1RestoredImageCoreOwnerStageBoundary.CoreSessionComposition)
            {
                failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
                diagnostic = coreDiagnostic;
                return false;
            }

            failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
            diagnostic = coreDiagnostic;
            return false;
        }

        current.CoreDOwners = stages.D;
        current.Time = stages.StagedTime;
        current.Decisions = stages.DecisionStore;
        current.EventAuthority = stages.EventAuthority;
        current.EventRecorder = stages.EventAuthority.Recorder;
        current.DecisionRecorder = new NpcDecisionRecorder(
            stages.EventAuthority.DecisionOccurrenceReceipts, current.Logger);
        current.EconomyTransactions = new EconomyTransactionService();
        current.EContext ??= new P12EDailyV1OwnerStagingContext(
            null, current.UnityRoots, current.D, current.Time,
            configuration.freeStatus, configuration.wantedStatus, configuration.arrestedStatus,
            configuration.hiddenStatus, current.EventRecorder, current.Logger,
            application, replacementOperation?.ExpectedSession, replacementOperation,
            null, stages);
        failure = P12GDailyV1RestoreFailure.None;
        diagnostic = null;
        return true;
    }

    private static bool TryCreateRestoredCoreExecutionBoundary(
        DailyV1EffectiveDefinitions selectedDefinitions,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendarDefinition,
        SimulationTime stagedTime,
        SimulationRuntimeAdmissionContext restoredAdmissionContext,
        P12CStagedContinuationRoot stagedC,
        P12DDailyV1OwnerPackage stagedD,
        P12EDailyV1OwnerPackage stagedE,
        P12FDailyV1OwnerPackage stagedF,
        NpcDecisionStore decisionStore,
        out DailyV1ExecutionBoundaryState executionBoundaryState,
        out SimulationCalendar selectedCalendar,
        out string diagnostic)
    {
        executionBoundaryState = null;
        selectedCalendar = null;
        diagnostic = null;
        if (selectedDefinitions == null || stagedC == null || stagedD == null
            || stagedE == null || stagedF == null || stagedD.SpatialNetwork == null
            || stagedD.RuntimeIdentities == null || stagedD.Persons == null
            || stagedE.Justice == null || stagedF.TravelParties == null
            || stagedF.ScheduledDirectives == null || stagedF.Expeditions == null
            || stagedF.ActorChoices == null)
        {
            diagnostic = "The P12-C/D/E/F staged roots required by the Core boundary are incomplete.";
            return false;
        }

        if (!DailyV1RestoredExecutionBoundaryComposer.TryCreate(
                selectedDefinitions, effectiveConfiguration, calendarDefinition, stagedTime,
                restoredAdmissionContext, stagedC.WorldIdentity,
                stagedD.SelectedCityOwners, stagedD.SelectedNpcOwners,
                stagedD.SpatialNetwork.SelectedOwner, stagedE.Justice.CoreOwner,
                stagedF.TravelParties, stagedD.RuntimeIdentities.CoreIdentityIndex,
                stagedD.Persons, stagedD.Genealogy,
                stagedF.ScheduledDirectives, stagedF.Expeditions,
                stagedC.RuntimeIdAllocator, stagedC.DeterministicRandom,
                stagedC.RecordSequence, decisionStore, stagedF.ActorChoices,
                out executionBoundaryState, out selectedCalendar, out diagnostic))
            return false;

        return true;
    }

    private static bool TryBuildDailyV1Definitions(
        SimulationConfigData configuration,
        out List<CityData> cityDefinitions,
        out List<ItemData> itemDefinitions,
        out List<NpcData> npcDefinitions,
        out List<NpcStatusData> statusDefinitions,
        out string diagnostic)
    {
        diagnostic = null;
        cityDefinitions = new List<CityData>();
        itemDefinitions = new List<ItemData>();
        npcDefinitions = new List<NpcData>();
        statusDefinitions = new List<NpcStatusData>();
        if (configuration == null) { diagnostic = "configuration is null"; return false; }

        cityDefinitions.AddRange(configuration.Cities);
        foreach (CityData city in cityDefinitions)
        {
            if (city == null) { diagnostic = "a configured City definition is null"; return false; }
            foreach (MarketItemConfig market in city.marketItems ?? new List<MarketItemConfig>())
                AddItemDefinition(itemDefinitions, market?.item);
            foreach (CityProductionConfig production in city.productionConfigs ?? new List<CityProductionConfig>())
                AddItemDefinition(itemDefinitions, production?.item);
        }

        foreach (NpcSimulationConfig npcConfig in configuration.Npcs)
        {
            if (npcConfig?.npc == null) { diagnostic = "a configured NPC definition is null"; return false; }
            npcDefinitions.Add(npcConfig.npc);
            foreach (NpcInitialInventoryItemConfig inventory in npcConfig.InitialInventory)
                AddItemDefinition(itemDefinitions, inventory?.item);
            foreach (TradeItemPreference preference in npcConfig.npc.job?.PreferredTradeItems
                ?? new List<TradeItemPreference>())
                AddItemDefinition(itemDefinitions, preference?.item);
            AddStatuses(statusDefinitions, npcConfig.npc.statusPadrao);
        }
        foreach (NpcJobData job in configuration.Jobs)
            foreach (TradeItemPreference preference in job?.PreferredTradeItems ?? new List<TradeItemPreference>())
                AddItemDefinition(itemDefinitions, preference?.item);
        foreach (NpcStatusData status in configuration.Statuses) AddStatusDefinition(statusDefinitions, status);
        AddStatusDefinition(statusDefinitions, configuration.freeStatus);
        AddStatusDefinition(statusDefinitions, configuration.wantedStatus);
        AddStatusDefinition(statusDefinitions, configuration.arrestedStatus);
        AddStatusDefinition(statusDefinitions, configuration.hiddenStatus);
        foreach (NpcActionData action in configuration.Actions)
        {
            if (action == null) { diagnostic = "a configured action definition is null"; return false; }
            AddStatuses(statusDefinitions, action.statusNecessariosParaFazerAcao);
            AddStatuses(statusDefinitions, action.statusToAdd);
            AddStatuses(statusDefinitions, action.statusToRemove);
            AddStatuses(statusDefinitions, action.targetStatusToAdd);
            AddStatuses(statusDefinitions, action.targetStatusToRemove);
            foreach (StatusWeightModifier weight in action.statusModifiers ?? new List<StatusWeightModifier>())
                AddStatusDefinition(statusDefinitions, weight?.status);
        }

        cityDefinitions.Sort((left, right) => StringComparer.Ordinal.Compare(left.DefinitionId, right.DefinitionId));
        npcDefinitions.Sort((left, right) => StringComparer.Ordinal.Compare(left.DefinitionId, right.DefinitionId));
        itemDefinitions.Sort((left, right) => StringComparer.Ordinal.Compare(left.DefinitionId, right.DefinitionId));
        statusDefinitions.Sort((left, right) => StringComparer.Ordinal.Compare(left.DefinitionId, right.DefinitionId));
        bool cityIdsValid = IsUnique(cityDefinitions.Select(value => value.DefinitionId));
        bool npcIdsValid = IsUnique(npcDefinitions.Select(value => value.DefinitionId));
        bool itemIdsValid = IsUnique(itemDefinitions.Select(value => value.DefinitionId));
        bool statusIdsValid = IsUnique(statusDefinitions.Select(value => value.DefinitionId));
        if (cityDefinitions.Count == 0 || npcDefinitions.Count == 0
            || !cityIdsValid || !npcIdsValid || !itemIdsValid || !statusIdsValid)
        {
            diagnostic = "counts(city,npc,item,status)=" + cityDefinitions.Count + ","
                + npcDefinitions.Count + "," + itemDefinitions.Count + "," + statusDefinitions.Count
                + "; unique(city,npc,item,status)=" + cityIdsValid + "," + npcIdsValid + ","
                + itemIdsValid + "," + statusIdsValid + ".";
            return false;
        }
        return true;
    }

    private static void AddItemDefinition(List<ItemData> values, ItemData candidate)
    {
        if (candidate == null) return;
        if (values.Any(value => string.Equals(value.DefinitionId, candidate.DefinitionId, StringComparison.Ordinal)))
            return;
        values.Add(candidate);
    }

    private static void AddStatusDefinition(List<NpcStatusData> values, NpcStatusData candidate)
    {
        if (candidate == null) return;
        if (values.Any(value => string.Equals(value.DefinitionId, candidate.DefinitionId, StringComparison.Ordinal)))
            return;
        values.Add(candidate);
    }

    private static void AddStatuses(List<NpcStatusData> values, IEnumerable<NpcStatusData> candidates)
    {
        if (candidates == null) return;
        foreach (NpcStatusData candidate in candidates) AddStatusDefinition(values, candidate);
    }

    private static bool IsUnique(IEnumerable<string> values)
    {
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string value in values)
            if (string.IsNullOrWhiteSpace(value) || !unique.Add(value)) return false;
        return true;
    }
}
