using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12FDailyV1OwnerPackageFailure
{
    None = 0,
    InvalidCaptureContext,
    CaptureFailed,
    StaleBoundary,
    InvalidStagingContext,
    InvalidActionDefinitions,
    StageFailed
}

/// <summary>
/// Detached F owner snapshots for one completed Daily-v1 boundary. It retains
/// only temporary token/vector identity needed to prevent mixing capture
/// attempts; it retains no live domain owner references.
/// </summary>
internal sealed class P12FDailyV1OwnerCapture
{
    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections;
    private readonly P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge;
    private readonly P12FScheduledDirectiveOwnerSnapshot directives;
    private readonly P12FActorChoiceSnapshot actorChoices;
    private readonly P12FTravelPartyOwnerSnapshot travelParties;
    private readonly P12FExpeditionOwnerSnapshot expeditions;

    internal P12FPoliticalKnowledgeOwnerSnapshot PoliticalKnowledgeSnapshot => politicalKnowledge;
    internal P12FScheduledDirectiveOwnerSnapshot DirectiveSnapshot => directives;
    internal P12FActorChoiceSnapshot ActorChoiceSnapshot => actorChoices;
    internal P12FTravelPartyOwnerSnapshot TravelPartySnapshot => travelParties;
    internal P12FExpeditionOwnerSnapshot ExpeditionSnapshot => expeditions;

    private P12FDailyV1OwnerCapture(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge,
        P12FScheduledDirectiveOwnerSnapshot directives,
        P12FActorChoiceSnapshot actorChoices,
        P12FTravelPartyOwnerSnapshot travelParties,
        P12FExpeditionOwnerSnapshot expeditions)
    {
        this.token = token;
        this.ownerSections = ownerSections;
        this.politicalKnowledge = politicalKnowledge;
        this.directives = directives;
        this.actorChoices = actorChoices;
        this.travelParties = travelParties;
        this.expeditions = expeditions;
    }

    /// <summary>Stable IDs used by D to restore reciprocal NPC TravelParty links.</summary>
    internal IReadOnlyList<string> TravelPartyIds
    {
        get
        {
            List<string> ids = new List<string>(travelParties.Parties.Count);
            foreach (P12FTravelPartyOwnerSnapshotRecord party in travelParties.Parties)
                ids.Add(party.TravelPartyId);
            return new ReadOnlyCollection<string>(ids);
        }
    }

    internal static bool TryCapture(
        SimulationRuntime sourceRuntime,
        SimulationBootstrapComposition sourceComposition,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        out P12FDailyV1OwnerCapture capture,
        out P12FDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation = null)
    {
        capture = null;
        failure = P12FDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (sourceRuntime == null || sourceComposition == null
            || !ReferenceEquals(sourceComposition.Runtime, sourceRuntime)
            || token == null || exactOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, exactOwnerSectionVector)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L
            || !ReferenceEquals(token.WorldId, sourceRuntime.WorldId)
            || !sourceRuntime.TryValidateCompletedDailyCaptureTokenForP12Capture(
                token, replacementOperation, out _))
            return false;

        try
        {
            PoliticalKnowledgeStore politicalOwner = sourceRuntime.PoliticalKnowledgeStoreForWorldBoundary;
            ScheduledDirectiveStore directiveOwner = sourceComposition.ScheduledDirectives;
            ActorChoiceStore actorChoiceOwner = sourceRuntime.ActorChoiceStore;
            TravelPartyStore travelPartyOwner = sourceComposition.TravelParties;
            ExpeditionStore expeditionOwner = sourceComposition.Expeditions;
            if (politicalOwner == null || directiveOwner == null || actorChoiceOwner == null
                || travelPartyOwner == null || expeditionOwner == null)
                return false;

            if (!P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(
                    politicalOwner, token, exactOwnerSectionVector,
                    out P12FPoliticalKnowledgeOwnerSnapshot politicalSnapshot, out _)
                || !P12FScheduledDirectiveOwnerSnapshot.TryCapture(
                    directiveOwner, token, exactOwnerSectionVector,
                    out P12FScheduledDirectiveOwnerSnapshot directiveSnapshot, out _)
                || !P12FActorChoiceSnapshot.TryCapture(
                    actorChoiceOwner, token, exactOwnerSectionVector,
                    out P12FActorChoiceSnapshot actorChoiceSnapshot, out _)
                || !P12FTravelPartyOwnerSnapshot.TryCapture(
                    travelPartyOwner, token, exactOwnerSectionVector,
                    out P12FTravelPartyOwnerSnapshot travelPartySnapshot, out _)
                || !P12FExpeditionOwnerSnapshot.TryCapture(
                    expeditionOwner, token, exactOwnerSectionVector,
                    out P12FExpeditionOwnerSnapshot expeditionSnapshot, out _))
            {
                failure = P12FDailyV1OwnerPackageFailure.CaptureFailed;
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.FOwnerSnapshotsCaptured);

            if (!sourceRuntime.TryValidateCompletedDailyCaptureTokenForP12Capture(
                token, replacementOperation, out _))
            {
                failure = P12FDailyV1OwnerPackageFailure.StaleBoundary;
                return false;
            }

            capture = new P12FDailyV1OwnerCapture(
                token, exactOwnerSectionVector, politicalSnapshot, directiveSnapshot,
                actorChoiceSnapshot, travelPartySnapshot, expeditionSnapshot);
            failure = P12FDailyV1OwnerPackageFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            capture = null;
            failure = P12FDailyV1OwnerPackageFailure.CaptureFailed;
            return false;
        }
    }

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out P12FDailyV1OwnerCapture capture,
        out P12FDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        capture = null;
        failure = P12FDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (operation == null || !operation.IsActive
            || !operation.TryRevalidate(out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StaleBoundary;
            return false;
        }

        if (!operation.TryCaptureCoreBoundP12FOwnerSources(
                out DailyV1P12GCoreBoundOwnerSources sources,
                out DailyV1P12GCoreBoundOwnerSourceCaptureFailure sourceFailure,
                out _))
        {
            failure = sourceFailure == DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary
                ? P12FDailyV1OwnerPackageFailure.StaleBoundary
                : sourceFailure == DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed
                    ? P12FDailyV1OwnerPackageFailure.CaptureFailed
                    : P12FDailyV1OwnerPackageFailure.InvalidCaptureContext;
            return false;
        }

        DailyV1P12GSourceBoundaryWitness witness = operation.SourceWitness;
        if (sources == null || !sources.IsFor(operation)
            || !operation.TryRevalidate(out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StaleBoundary;
            return false;
        }

        stageObserver?.Invoke(P12GDailyV1RestoreStage.FOwnerSnapshotsCaptured);
        if (!operation.TryRevalidate(out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StaleBoundary;
            return false;
        }

        capture = new P12FDailyV1OwnerCapture(
            witness.Token,
            witness.TokenOwnerSections,
            sources.PoliticalKnowledge,
            sources.Directives,
            sources.ActorChoices,
            sources.TravelParties,
            sources.Expeditions);
        failure = P12FDailyV1OwnerPackageFailure.None;
        return true;
    }

    internal bool IsFor(DailyCaptureEligibilityToken candidateToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> candidateOwnerSections) =>
        ReferenceEquals(token, candidateToken) && ReferenceEquals(ownerSections, candidateOwnerSections);

    internal bool TryStage(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken exactCompletedToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        P12EDailyV1OwnerStagingContext context,
        P12EDailyV1OwnerPackage stagedE,
        DailyV1EffectiveDefinitions selectedDefinitions,
        out P12FDailyV1OwnerPackage package,
        out P12FDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        Func<P12FActorChoiceSnapshot, P12FActorChoiceSnapshot> actorChoiceSnapshotOverrideForTest = null)
    {
        package = null;
        failure = P12FDailyV1OwnerPackageFailure.InvalidStagingContext;
        if (sourceRuntime == null || exactCompletedToken == null || exactOwnerSectionVector == null
            || context == null || stagedE == null || selectedDefinitions == null
            || !IsFor(exactCompletedToken, exactOwnerSectionVector)
            || context.StagingAttempt == null
            || !context.StagingAttempt.IsCurrentFor(sourceRuntime, exactCompletedToken, exactOwnerSectionVector)
            || !ReferenceEquals(exactCompletedToken.OwnerSections, exactOwnerSectionVector)
            || context.P12CRoots == null || context.P12DPackage == null
            || !ReferenceEquals(context.P12CRoots.StagingAttempt, context.StagingAttempt)
            || !ReferenceEquals(context.P12DPackage.StagingAttempt, context.StagingAttempt)
            || !ReferenceEquals(stagedE.StagingAttempt, context.StagingAttempt)
            || context.P12DPackage.Npcs == null || context.P12DPackage.NpcFRows == null
            || context.P12DPackage.SelectedNpcOwners == null
            || context.P12DPackage.SpatialNetwork == null
            || context.P12DPackage.RuntimeIdentities == null
            || context.P12DPackage.Npcs.Count != context.P12DPackage.NpcFRows.Count
            || context.StagedSimulationTime == null
            || context.StagedSimulationTime.AbsoluteDay != exactCompletedToken.AbsoluteDay
            || stagedE.WorldId == null
            || !string.Equals(stagedE.WorldId.Value, exactCompletedToken.WorldId?.Value, StringComparison.Ordinal)
            || !context.StagingAttempt.IsCurrentFor(
                sourceRuntime, exactCompletedToken, exactOwnerSectionVector))
            return false;

        for (int i = 0; i < context.P12DPackage.Npcs.Count; i++)
        {
            NpcRuntime npc = context.P12DPackage.Npcs[i];
            P12DNpcFRow row = context.P12DPackage.NpcFRows[i];
            if (npc == null || row == null
                || !string.Equals(npc.RuntimeId, row.RuntimeId, StringComparison.Ordinal))
            {
                failure = P12FDailyV1OwnerPackageFailure.InvalidStagingContext;
                return false;
            }
        }

        P12DDailyV1OwnerPackage stagedD = context.P12DPackage;
        P12CStagedContinuationRoot stagedC = context.P12CRoots;
        if (!politicalKnowledge.TryStage(
                exactCompletedToken, exactOwnerSectionVector,
                stagedD.Persons, stagedE.Institutions, stagedE.PoliticalClaims,
                stagedE.Factions, stagedE.Offices, stagedE.PropertyOwnership,
                exactCompletedToken.AbsoluteDay,
                out PoliticalKnowledgeStore stagedPoliticalKnowledge, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FPoliticalKnowledgeStaged);

        if (!TryGetNpcRuntimeIds(stagedD.Npcs, out IReadOnlyList<string> stagedNpcRuntimeIds))
        {
            failure = P12FDailyV1OwnerPackageFailure.InvalidStagingContext;
            return false;
        }
        if (!directives.TryStage(
                exactCompletedToken, exactOwnerSectionVector, context.StagedSimulationTime,
                selectedDefinitions,
                out ScheduledDirectiveStore stagedDirectives, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FScheduledDirectivesStaged);

        P12FActorChoiceSnapshot actorChoiceSnapshot = actorChoiceSnapshotOverrideForTest == null
            ? actorChoices
            : actorChoiceSnapshotOverrideForTest(actorChoices);
        if (!P12FActorChoiceSnapshot.TryStage(
                actorChoiceSnapshot, stagedD.Persons,
                out ActorChoiceStore stagedActorChoices, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FActorChoicesStaged);

        if (!DailyV1P12FTravelPartyOwnerStager.TryStage(
                travelParties, stagedD.SelectedNpcOwners, stagedD.NpcFRows,
                stagedD.SpatialNetwork.SelectedOwner, out TravelPartyStore stagedTravelParties, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FTravelPartiesStaged);

        if (!P12FExpeditionOwnerSnapshotStager.TryStage(
                expeditions, stagedNpcRuntimeIds, stagedD.NpcFRows,
                stagedD.SelectedNpcOwners, stagedD.RuntimeIdentities.CoreIdentityIndex,
                stagedD.SpatialNetwork.SelectedOwner, stagedD.EmptyExplorableSites, stagedTravelParties,
                out ExpeditionStore stagedExpeditions, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FExpeditionsStaged);

        if (!context.StagingAttempt.IsCurrentFor(sourceRuntime, exactCompletedToken, exactOwnerSectionVector))
        {
            failure = P12FDailyV1OwnerPackageFailure.StaleBoundary;
            return false;
        }

        package = new P12FDailyV1OwnerPackage(
            context.StagingAttempt, stagedC.WorldIdentity,
            stagedPoliticalKnowledge, stagedDirectives, stagedActorChoices,
            stagedTravelParties, stagedExpeditions, stagedD.NpcFRows,
            stagedE.UnresolvedPoliticalKnowledgeBindings);
        failure = P12FDailyV1OwnerPackageFailure.None;
        return true;
    }

    private static bool TryGetNpcRuntimeIds(
        IReadOnlyList<NpcRuntime> npcs,
        out IReadOnlyList<string> runtimeIds)
    {
        runtimeIds = null;
        if (npcs == null) return false;
        List<string> ids = new List<string>(npcs.Count);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !seen.Add(npc.RuntimeId))
                return false;
            ids.Add(npc.RuntimeId);
        }

        runtimeIds = new ReadOnlyCollection<string>(ids);
        return true;
    }

}

/// <summary>Private unpublished F candidates consumed later by P12-G.</summary>
internal sealed class P12FDailyV1OwnerPackage
{
    internal DailyCaptureStagingAttempt StagingAttempt { get; }
    internal WorldId WorldId { get; }
    internal PoliticalKnowledgeStore PoliticalKnowledge { get; }
    internal ScheduledDirectiveStore ScheduledDirectives { get; }
    internal ActorChoiceStore ActorChoices { get; }
    internal TravelPartyStore TravelParties { get; }
    internal ExpeditionStore Expeditions { get; }
    internal IReadOnlyList<P12DNpcFRow> DetachedNpcRows { get; }
    internal IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> UnresolvedPoliticalKnowledgeBindings { get; }

    internal P12FDailyV1OwnerPackage(
        DailyCaptureStagingAttempt stagingAttempt,
        WorldId worldId,
        PoliticalKnowledgeStore politicalKnowledge,
        ScheduledDirectiveStore scheduledDirectives,
        ActorChoiceStore actorChoices,
        TravelPartyStore travelParties,
        ExpeditionStore expeditions,
        IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        IReadOnlyList<P12EUnresolvedPoliticalKnowledgeBinding> unresolvedPoliticalKnowledgeBindings)
    {
        StagingAttempt = stagingAttempt;
        WorldId = worldId ?? throw new ArgumentNullException(nameof(worldId));
        PoliticalKnowledge = politicalKnowledge ?? throw new ArgumentNullException(nameof(politicalKnowledge));
        ScheduledDirectives = scheduledDirectives ?? throw new ArgumentNullException(nameof(scheduledDirectives));
        ActorChoices = actorChoices ?? throw new ArgumentNullException(nameof(actorChoices));
        TravelParties = travelParties ?? throw new ArgumentNullException(nameof(travelParties));
        Expeditions = expeditions ?? throw new ArgumentNullException(nameof(expeditions));
        DetachedNpcRows = new ReadOnlyCollection<P12DNpcFRow>(
            new List<P12DNpcFRow>(detachedNpcRows ?? throw new ArgumentNullException(nameof(detachedNpcRows))));
        UnresolvedPoliticalKnowledgeBindings = new ReadOnlyCollection<P12EUnresolvedPoliticalKnowledgeBinding>(
            new List<P12EUnresolvedPoliticalKnowledgeBinding>(
                unresolvedPoliticalKnowledgeBindings ?? throw new ArgumentNullException(nameof(unresolvedPoliticalKnowledgeBindings))));
    }

    /// <summary>Stages decoded F values against detached roots without a source token or owner vector.</summary>
    internal static bool TryStageFromImage(
        P12AContinuationImage image,
        P12ADailyV1ImageOwnerSnapshots decoded,
        P12EDailyV1OwnerStagingContext context,
        P12EDailyV1OwnerPackage stagedE,
        DailyV1EffectiveDefinitions selectedDefinitions,
        out P12FDailyV1OwnerPackage package,
        out P12FDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        package = null;
        failure = P12FDailyV1OwnerPackageFailure.InvalidStagingContext;
        if (image == null || decoded == null || context == null || stagedE == null
            || context.StagingAttempt != null || context.P12CRoots?.StagingAttempt != null
            || context.P12DPackage?.StagingAttempt != null || stagedE.StagingAttempt != null
            || context.P12DPackage?.Npcs == null || context.P12DPackage.NpcFRows == null
            || context.P12DPackage.SelectedNpcOwners == null
            || context.P12DPackage.SpatialNetwork == null
            || context.P12DPackage.RuntimeIdentities == null
            || context.P12DPackage.Npcs.Count != context.P12DPackage.NpcFRows.Count
            || context.StagedSimulationTime == null || context.StagedSimulationTime.AbsoluteDay != image.Header.Day
            || stagedE.WorldId == null || stagedE.WorldId.Value != image.Header.WorldId
            || context.P12DPackage.WorldId == null || context.P12DPackage.WorldId.Value != image.Header.WorldId
            || selectedDefinitions == null || decoded.PoliticalKnowledge == null
            || decoded.Directives == null || decoded.ActorChoices == null
            || decoded.TravelParties == null || decoded.Expeditions == null)
            return false;

        if (!TryGetNpcRuntimeIds(context.P12DPackage.Npcs, out IReadOnlyList<string> stagedNpcRuntimeIds))
        {
            failure = P12FDailyV1OwnerPackageFailure.InvalidStagingContext;
            return false;
        }

        try
        {
            long day = image.Header.Day;
            DailyV1P12FCoreOwnerRestoreStage coreOwnerStage;
            if (context.Application != null)
            {
                if (!context.Application.TryStageRestoredP12FOwners(
                        context.P12CRoots.CoreRoot, decoded.PoliticalKnowledge,
                        decoded.Directives, decoded.ActorChoices, decoded.TravelParties,
                        decoded.Expeditions, context.P12DPackage.Persons,
                        stagedE.Institutions, stagedE.PoliticalClaims, stagedE.Factions,
                        stagedE.Offices, stagedE.PropertyOwnership, day,
                        context.StagedSimulationTime, selectedDefinitions,
                        context.P12DPackage.NpcFRows, context.P12DPackage.SelectedNpcOwners,
                        context.P12DPackage.RuntimeIdentities.CoreIdentityIndex,
                        context.P12DPackage.SpatialNetwork.SelectedOwner,
                        context.P12DPackage.EmptyExplorableSites,
                        context.ExpectedSession, context.ReplacementOperation,
                        out coreOwnerStage, out _,
                        step => ObserveP12FCoreOwnerRestoreStep(stageObserver, step))
                    || coreOwnerStage == null)
                {
                    failure = P12FDailyV1OwnerPackageFailure.StageFailed;
                    return false;
                }
            }
            else
            {
                if (!decoded.PoliticalKnowledge.TryStageForImage(context.P12DPackage.Persons,
                        stagedE.Institutions, stagedE.PoliticalClaims, stagedE.Factions,
                        stagedE.Offices, stagedE.PropertyOwnership, day,
                        out PoliticalKnowledgeStore politicalKnowledge, out _))
                {
                    failure = P12FDailyV1OwnerPackageFailure.StageFailed;
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.FPoliticalKnowledgeStaged);

                if (!decoded.Directives.TryStageForImage(context.StagedSimulationTime,
                        selectedDefinitions, out ScheduledDirectiveStore directives, out _))
                {
                    failure = P12FDailyV1OwnerPackageFailure.StageFailed;
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.FScheduledDirectivesStaged);

                if (!P12FActorChoiceSnapshot.TryStage(decoded.ActorChoices, context.P12DPackage.Persons,
                        out ActorChoiceStore actorChoices, out _))
                {
                    failure = P12FDailyV1OwnerPackageFailure.StageFailed;
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.FActorChoicesStaged);

                if (!DailyV1P12FTravelPartyOwnerStager.TryStage(decoded.TravelParties,
                        context.P12DPackage.SelectedNpcOwners, context.P12DPackage.NpcFRows,
                        context.P12DPackage.SpatialNetwork.SelectedOwner,
                        out TravelPartyStore travelParties, out _))
                {
                    failure = P12FDailyV1OwnerPackageFailure.StageFailed;
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.FTravelPartiesStaged);

                if (!P12FExpeditionOwnerSnapshotStager.TryStage(
                        decoded.Expeditions, stagedNpcRuntimeIds, context.P12DPackage.NpcFRows,
                        context.P12DPackage.SelectedNpcOwners, context.P12DPackage.RuntimeIdentities.CoreIdentityIndex,
                        context.P12DPackage.SpatialNetwork.SelectedOwner, context.P12DPackage.EmptyExplorableSites, travelParties,
                        out ExpeditionStore expeditions, out _))
                {
                    failure = P12FDailyV1OwnerPackageFailure.StageFailed;
                    return false;
                }
                stageObserver?.Invoke(P12GDailyV1RestoreStage.FExpeditionsStaged);
                coreOwnerStage = new DailyV1P12FCoreOwnerRestoreStage(
                    politicalKnowledge, directives, actorChoices, travelParties, expeditions);
            }

            package = new P12FDailyV1OwnerPackage(null, context.P12CRoots.WorldIdentity,
                coreOwnerStage.PoliticalKnowledge, coreOwnerStage.ScheduledDirectives,
                coreOwnerStage.ActorChoices, coreOwnerStage.TravelParties,
                coreOwnerStage.Expeditions,
                context.P12DPackage.NpcFRows, stagedE.UnresolvedPoliticalKnowledgeBindings);
            failure = P12FDailyV1OwnerPackageFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            package = null;
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
    }

    private static bool TryGetNpcRuntimeIds(
        IReadOnlyList<NpcRuntime> npcs,
        out IReadOnlyList<string> runtimeIds)
    {
        runtimeIds = null;
        if (npcs == null) return false;
        List<string> ids = new List<string>(npcs.Count);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !seen.Add(npc.RuntimeId))
                return false;
            ids.Add(npc.RuntimeId);
        }

        runtimeIds = new ReadOnlyCollection<string>(ids);
        return true;
    }

    private static void ObserveP12FCoreOwnerRestoreStep(
        Action<P12GDailyV1RestoreStage> stageObserver,
        DailyV1P12FCoreOwnerRestoreStep step)
    {
        if (stageObserver == null) return;
        switch (step)
        {
            case DailyV1P12FCoreOwnerRestoreStep.PoliticalKnowledge:
                stageObserver(P12GDailyV1RestoreStage.FPoliticalKnowledgeStaged);
                break;
            case DailyV1P12FCoreOwnerRestoreStep.ScheduledDirectives:
                stageObserver(P12GDailyV1RestoreStage.FScheduledDirectivesStaged);
                break;
            case DailyV1P12FCoreOwnerRestoreStep.ActorChoices:
                stageObserver(P12GDailyV1RestoreStage.FActorChoicesStaged);
                break;
            case DailyV1P12FCoreOwnerRestoreStep.TravelParties:
                stageObserver(P12GDailyV1RestoreStage.FTravelPartiesStaged);
                break;
            case DailyV1P12FCoreOwnerRestoreStep.Expeditions:
                stageObserver(P12GDailyV1RestoreStage.FExpeditionsStaged);
                break;
        }
    }
}
