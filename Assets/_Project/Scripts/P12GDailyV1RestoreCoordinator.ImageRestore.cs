using System;
using System.Collections.Generic;
using System.Linq;

internal static partial class P12GDailyV1RestoreCoordinator
{
    /// <summary>
    /// Builds a private Daily-v1 candidate solely from a fully decoded persisted
    /// image and the current authored host profile. No source session, token,
    /// owner vector, or capture attempt participates in this path.
    /// </summary>
    internal static bool TryCreateRestoredSessionFromImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor expectedCompatibility,
        SimulationConfigData configuration,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar,
        SimulationRuntimeAdmissionContext bootstrapAdmissionContext,
        out SimulationActiveSession restoredSession,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        restoredSession = null;
        failure = P12GDailyV1RestoreFailure.InvalidSourceSession;
        diagnostic = "A validated persisted Daily-v1 image and current host profile are required.";
        if (image == null || expectedCompatibility == null || configuration == null
            || effective == null || calendar == null || bootstrapAdmissionContext == null
            || bootstrapAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || bootstrapAdmissionContext.IsRestoredContinuation
            || !bootstrapAdmissionContext.IsOwnedByCurrentThread()
            || image.Header == null || !image.Header.IsValid
            || !expectedCompatibility.Matches(image.Header.Compatibility.CanonicalBytes))
            return false;

        try
        {
            if (!P12ADailyV1Census.TryCreateForImage(image,
                    out P12ADailyV1Census imageCensus, out P12AResult imageResult))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "The persisted image does not satisfy the current Daily-v1 owner manifest: "
                    + (imageResult?.SectionCode ?? imageResult?.Code.ToString() ?? "InvalidOwnerValue") + ".";
                return false;
            }

            if (!P12ADailyV1ImageOwnerDecoder.TryDecode(image,
                    out P12ADailyV1ImageOwnerSnapshots decoded, out imageResult)
                || decoded == null)
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "The persisted Daily-v1 typed owner image could not be decoded: "
                    + (imageResult?.SectionCode ?? imageResult?.Code.ToString() ?? "OwnerGraphRejected") + ".";
                return false;
            }

            stageObserver?.Invoke(P12GDailyV1RestoreStage.SourceCaptured);
            if (!P12ACausalRootImageAdapter.TryStage(image,
                    out P12CStagedContinuationRoot stagedC, out imageResult, stageObserver)
                || stagedC == null || !stagedC.IsDetached)
            {
                failure = P12GDailyV1RestoreFailure.RootStageFailed;
                diagnostic = "P12-C could not privately stage the persisted roots: "
                    + (imageResult?.SectionCode ?? imageResult?.Code.ToString() ?? "OwnerGraphRejected") + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.RootsStaged);

            if (!TryBuildDailyV1Definitions(configuration,
                    out List<CityData> cityDefinitions,
                    out List<ItemData> itemDefinitions,
                    out List<NpcData> npcDefinitions,
                    out List<NpcStatusData> statusDefinitions,
                    out string definitionDiagnostic))
            {
                failure = P12GDailyV1RestoreFailure.SourceCaptureFailed;
                diagnostic = "The current authored Daily-v1 definition catalog is incomplete or ambiguous: "
                    + definitionDiagnostic;
                return false;
            }

            RuntimeIdentityRegistry stagedIdentities = new RuntimeIdentityRegistry();
            List<string> stagedPartyIds = decoded.TravelParties.Parties
                .Select(party => party?.TravelPartyId).ToList();
            if (!P12DDailyV1OwnerPackage.TryStageFromImage(
                    image.Sections, decoded.Cities, decoded.NpcDRows, decoded.NpcFRows,
                    decoded.SpatialNetwork, decoded.Persons, decoded.Genealogy, decoded.Sites,
                    stagedIdentities, stagedC.WorldIdentity, cityDefinitions, itemDefinitions,
                    npcDefinitions, configuration.Actions, statusDefinitions,
                    Array.Empty<ExplorableSiteData>(), stagedPartyIds,
                    out P12DDailyV1OwnerPackage stagedD,
                    out P12DDailyV1OwnerPackageFailure dFailure)
                || stagedD == null || stagedD.StagingAttempt != null)
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "P12-D could not hydrate the detached factual roots: " + dFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.DStaged);

            SimulationTime stagedTime = new SimulationTime(image.Header.Day);
            SimulationLogger logger = new SimulationLogger(configuration.LogSettings);
            logger.BeginSimulation(configuration.simulationName, configuration.EnabledModules,
                configuration.Cities.Count, configuration.Npcs.Count);
            HistoryStore history = new HistoryStore();
            DomainEventStore eventStore = new DomainEventStore(history, new HistoryPolicy(), logger);
            NpcDecisionStore decisions = new NpcDecisionStore(logger);
            DomainEventRecorder eventRecorder = new DomainEventRecorder(
                stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, eventStore, logger);
            NpcDecisionRecorder decisionRecorder = new NpcDecisionRecorder(
                stagedC.RuntimeIdAllocator, stagedTime, stagedC.RecordSequence, decisions, logger);
            EconomyTransactionService economyTransactions = new EconomyTransactionService();
            P12EDailyV1OwnerStagingContext eContext = new P12EDailyV1OwnerStagingContext(
                null, stagedC, stagedD, stagedTime, configuration.freeStatus,
                configuration.wantedStatus, configuration.arrestedStatus,
                configuration.hiddenStatus, eventRecorder, logger);

            if (!P12EDailyV1OwnerPackage.TryStageFromImage(image, decoded, eContext,
                    out P12EDailyV1OwnerPackage stagedE,
                    out P12EDailyV1OwnerPackageFailure eFailure)
                || stagedE == null || stagedE.StagingAttempt != null)
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "P12-E could not hydrate the detached included owners: " + eFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.EStaged);

            if (!P12FDailyV1OwnerPackage.TryStageFromImage(image, decoded, eContext,
                    stagedE, configuration.Actions,
                    out P12FDailyV1OwnerPackage stagedF,
                    out P12FDailyV1OwnerPackageFailure fFailure)
                || stagedF == null || stagedF.StagingAttempt != null)
            {
                failure = P12GDailyV1RestoreFailure.OwnerStageFailed;
                diagnostic = "P12-F could not hydrate the detached knowledge and commitment owners: " + fFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.FStaged);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.OwnersStaged);

            if (!TryBuildRestoredSession(configuration, effective, calendar,
                    bootstrapAdmissionContext, image.Header.PoliticalWorldRevision,
                    image.Header.LastEconomySnapshotDay, stagedC, stagedD, stagedE, stagedF,
                    stagedTime, eventStore, history, decisions, decisionRecorder,
                    eventRecorder, economyTransactions, logger, null,
                    out SimulationActiveSession candidate, out diagnostic)
                || candidate == null)
            {
                failure = P12GDailyV1RestoreFailure.TargetCompositionFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CandidateComposed);

            SimulationRuntime targetRuntime = candidate.Runtime;
            if (!TryValidatePersonNpcGenealogyBindings(targetRuntime, out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetPersonNpcGenealogyBindingsValidated);

            if (!targetRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                    out IReadOnlyList<OwnerSectionCensusSnapshot> targetSections,
                    out long targetEpoch, out ContinuationCensusFailure censusFailure))
            {
                failure = P12GDailyV1RestoreFailure.TargetOwnerVectorFailed;
                diagnostic = "The private target did not produce a complete quiescent owner vector: "
                    + censusFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetOwnerVectorCaptured);

            if (!TryValidateDailyV1ExpeditionExactZero(candidate.Composition, targetSections,
                    out diagnostic)
                || !TryValidateAllocatorHighWater(candidate, out diagnostic)
                || !P12GDailyV1OwnerVector.TryValidateRestoredImageCandidate(
                    imageCensus, targetSections, out _, out diagnostic)
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

            if (!targetRuntime.TryAssessNpcRosterCensus(out censusFailure)
                || !TryValidateTravelPartyBindings(candidate, out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = "The private target failed the NPC roster census: " + censusFailure + ".";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetNpcCensusValidated);
            stageObserver?.Invoke(P12GDailyV1RestoreStage.TargetTravelPartyBindingsValidated);

            if (!targetRuntime.TryBindRestoredCandidateMutationGuard(out diagnostic))
            {
                failure = P12GDailyV1RestoreFailure.BindingValidationFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CandidateGuardBound);

            if (!targetRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                    out IReadOnlyList<OwnerSectionCensusSnapshot> recapturedSections,
                    out long recapturedEpoch, out censusFailure)
                || !P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
                    targetSections, targetEpoch, recapturedSections, recapturedEpoch,
                    out _, out diagnostic))
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
