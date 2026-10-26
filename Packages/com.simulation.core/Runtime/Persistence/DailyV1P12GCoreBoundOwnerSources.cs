using System;
using System.Collections.Generic;

internal enum DailyV1P12GCoreBoundOwnerSourceCaptureFailure
{
    None,
    InvalidSaveOperation,
    MissingCoreOwnerBinding,
    OwnerCaptureFailed,
    StaleSourceBoundary
}

/// <summary>
/// Detached snapshots for the P12-F owners already bound to the active Core
/// session: PoliticalKnowledge, actor choices, scheduled directives,
/// TravelParties, and expeditions.
/// </summary>
internal sealed class DailyV1P12GCoreBoundOwnerSources
{
    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections;

    private DailyV1P12GCoreBoundOwnerSources(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge,
        P12FActorChoiceSnapshot actorChoices,
        P12FScheduledDirectiveOwnerSnapshot directives,
        P12FTravelPartyOwnerSnapshot travelParties,
        P12FExpeditionOwnerSnapshot expeditions)
    {
        this.token = token;
        this.ownerSections = ownerSections;
        PoliticalKnowledge = politicalKnowledge;
        ActorChoices = actorChoices;
        Directives = directives;
        TravelParties = travelParties;
        Expeditions = expeditions;
    }

    internal P12FPoliticalKnowledgeOwnerSnapshot PoliticalKnowledge { get; }
    internal P12FActorChoiceSnapshot ActorChoices { get; }
    internal P12FScheduledDirectiveOwnerSnapshot Directives { get; }
    internal P12FTravelPartyOwnerSnapshot TravelParties { get; }
    internal P12FExpeditionOwnerSnapshot Expeditions { get; }

    internal static bool HasRequiredCoreOwnerBindings(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoices) =>
        graph != null
        && graph.PoliticalKnowledgeStore != null
        && actorChoices != null
        && ReferenceEquals(actorChoices.PersonStore, graph.PersonStore)
        && graph.TravelParties != null
        && graph.ScheduledDirectives != null
        && graph.Expeditions != null;

    internal bool IsFor(DailyV1P12GContinuationSaveOperation operation) =>
        operation != null
        && ReferenceEquals(token, operation.SourceWitness.Token)
        && ReferenceEquals(ownerSections, operation.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out DailyV1P12GCoreBoundOwnerSources sources,
        out DailyV1P12GCoreBoundOwnerSourceCaptureFailure failure,
        out string diagnostic)
    {
        sources = null;
        failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.InvalidSaveOperation;
        diagnostic = null;
        if (operation == null || !operation.IsActive || !operation.TryRevalidate(out _))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
            diagnostic = "A current active Core continuation-save witness is required.";
            return false;
        }

        DailyV1ActiveRuntimeSession session = operation.Session;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        ActorChoiceStore actorChoices = session?.ExecutionBoundaryState.SelectedActorChoiceStore;
        if (!HasRequiredCoreOwnerBindings(graph, actorChoices))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.MissingCoreOwnerBinding;
            diagnostic = "The Core session has not bound every included P12-F owner source.";
            return false;
        }

        DailyV1P12GSourceBoundaryWitness witness = operation.SourceWitness;
        DailyCaptureEligibilityToken token = witness.Token;
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections = witness.TokenOwnerSections;
        try
        {
            if (!P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(
                    graph.PoliticalKnowledgeStore, token, ownerSections,
                    out P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledgeSnapshot,
                    out string politicalKnowledgeFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
                diagnostic = "The Core PoliticalKnowledge source snapshot was rejected: "
                    + politicalKnowledgeFailure + ".";
                return false;
            }

            if (!P12FActorChoiceSnapshot.TryCapture(actorChoices, token, ownerSections,
                    out P12FActorChoiceSnapshot actorChoiceSnapshot,
                    out P12FActorChoiceSnapshotFailure actorChoiceFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
                diagnostic = "The Core ActorChoice source snapshot was rejected: "
                    + actorChoiceFailure + ".";
                return false;
            }

            if (!P12FScheduledDirectiveOwnerSnapshot.TryCapture(graph.ScheduledDirectives,
                    token, ownerSections, out P12FScheduledDirectiveOwnerSnapshot directiveSnapshot,
                    out string directiveFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
                diagnostic = "The Core ScheduledDirective source snapshot was rejected: "
                    + directiveFailure + ".";
                return false;
            }

            if (!P12FTravelPartyOwnerSnapshot.TryCapture(graph.TravelParties, token,
                    ownerSections, out P12FTravelPartyOwnerSnapshot travelPartySnapshot,
                    out P12FCommitmentSnapshotFailure travelPartyFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
                diagnostic = "The Core TravelParty source snapshot was rejected: "
                    + travelPartyFailure + ".";
                return false;
            }

            if (!P12FExpeditionOwnerSnapshot.TryCapture(graph.Expeditions, token,
                    ownerSections, out P12FExpeditionOwnerSnapshot expeditionSnapshot,
                    out P12FCommitmentSnapshotFailure expeditionFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
                diagnostic = "The Core Expedition source snapshot was rejected: "
                    + expeditionFailure + ".";
                return false;
            }

            if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure staleFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
                diagnostic = "The exact Core continuation-save witness changed during owner capture: "
                    + staleFailure + ".";
                return false;
            }

            sources = new DailyV1P12GCoreBoundOwnerSources(
                token, ownerSections, politicalKnowledgeSnapshot, actorChoiceSnapshot, directiveSnapshot,
                travelPartySnapshot, expeditionSnapshot);
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "A Core-bound P12-F owner source threw during capture: "
                + exception.GetType().Name + ".";
            return false;
        }
    }
}
