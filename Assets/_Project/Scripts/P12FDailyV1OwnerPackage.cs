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
        Action<P12GDailyV1RestoreStage> stageObserver = null)
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
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _))
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

            if (!sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _))
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

    internal bool IsFor(DailyCaptureEligibilityToken candidateToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> candidateOwnerSections) =>
        ReferenceEquals(token, candidateToken) && ReferenceEquals(ownerSections, candidateOwnerSections);

    internal bool TryStage(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken exactCompletedToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        P12EDailyV1OwnerStagingContext context,
        P12EDailyV1OwnerPackage stagedE,
        IReadOnlyList<NpcActionData> admittedActionDefinitions,
        out P12FDailyV1OwnerPackage package,
        out P12FDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        package = null;
        failure = P12FDailyV1OwnerPackageFailure.InvalidStagingContext;
        if (sourceRuntime == null || exactCompletedToken == null || exactOwnerSectionVector == null
            || context == null || stagedE == null || admittedActionDefinitions == null
            || !IsFor(exactCompletedToken, exactOwnerSectionVector)
            || context.StagingAttempt == null
            || !context.StagingAttempt.IsCurrentFor(sourceRuntime, exactCompletedToken, exactOwnerSectionVector)
            || !ReferenceEquals(exactCompletedToken.OwnerSections, exactOwnerSectionVector)
            || context.P12CRoots == null || context.P12DPackage == null
            || !ReferenceEquals(context.P12CRoots.StagingAttempt, context.StagingAttempt)
            || !ReferenceEquals(context.P12DPackage.StagingAttempt, context.StagingAttempt)
            || !ReferenceEquals(stagedE.StagingAttempt, context.StagingAttempt)
            || context.P12DPackage.Npcs == null || context.P12DPackage.NpcFRows == null
            || context.P12DPackage.RuntimeIdentities == null
            || context.P12DPackage.Npcs.Count != context.P12DPackage.NpcFRows.Count
            || context.StagedSimulationTime == null
            || context.StagedSimulationTime.AbsoluteDay != exactCompletedToken.AbsoluteDay
            || stagedE.WorldId == null
            || !string.Equals(stagedE.WorldId.Value, exactCompletedToken.WorldId?.Value, StringComparison.Ordinal)
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(exactCompletedToken, out _))
            return false;

        if (!TryBuildActionMap(admittedActionDefinitions,
                out Dictionary<string, NpcActionData> actionsById))
        {
            failure = P12FDailyV1OwnerPackageFailure.InvalidActionDefinitions;
            return false;
        }

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

        if (!directives.TryStage(
                exactCompletedToken, exactOwnerSectionVector, context.StagedSimulationTime,
                BuildNpcMap(stagedD.Npcs), actionsById,
                out ScheduledDirectiveStore stagedDirectives, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FScheduledDirectivesStaged);

        if (!P12FActorChoiceSnapshot.TryStage(
                actorChoices, stagedD.Persons,
                out ActorChoiceStore stagedActorChoices, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FActorChoicesStaged);

        if (!P12FTravelPartyOwnerSnapshot.TryStage(
                travelParties, stagedD.Npcs, stagedD.NpcFRows,
                stagedD.RuntimeIdentities, out TravelPartyStore stagedTravelParties, out _))
        {
            failure = P12FDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.FTravelPartiesStaged);

        if (!P12FExpeditionOwnerSnapshot.TryStage(
                expeditions, stagedD.Npcs, stagedD.NpcFRows,
                stagedD.RuntimeIdentities, stagedTravelParties,
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

    private static Dictionary<string, NpcRuntime> BuildNpcMap(IReadOnlyList<NpcRuntime> npcs)
    {
        Dictionary<string, NpcRuntime> result = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || result.ContainsKey(npc.RuntimeId))
                return null;
            result.Add(npc.RuntimeId, npc);
        }
        return result;
    }

    private static bool TryBuildActionMap(IReadOnlyList<NpcActionData> definitions,
        out Dictionary<string, NpcActionData> actionsById)
    {
        actionsById = new Dictionary<string, NpcActionData>(StringComparer.Ordinal);
        foreach (NpcActionData definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId)
                || actionsById.ContainsKey(definition.DefinitionId))
            {
                actionsById = null;
                return false;
            }
            actionsById.Add(definition.DefinitionId, definition);
        }
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
        StagingAttempt = stagingAttempt ?? throw new ArgumentNullException(nameof(stagingAttempt));
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
}
