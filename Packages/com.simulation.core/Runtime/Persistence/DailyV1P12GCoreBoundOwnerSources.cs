using System;
using System.Collections.Generic;

internal enum DailyV1P12GCoreBoundOwnerSourceCaptureFailure
{
    None,
    InvalidSaveOperation,
    MissingCoreOwnerBinding,
    OwnerCensusMismatch,
    OwnerCaptureFailed,
    StaleSourceBoundary
}
internal enum DailyV1P12GCoreBoundCausalRootCaptureFailure
{
    None,
    InvalidSaveOperation,
    MissingP12CRuntimeRoots,
    RootBindingMismatch,
    RootCaptureFailed,
    StaleSourceBoundary
}

/// <summary>
/// P12-C snapshots captured from the exact roots retained by the active Core
/// session, whether it was published from genesis or detached restoration. The
/// Unity coordinator may stage these immutable values, but does not select or
/// reconstruct their source authorities.
/// </summary>
internal sealed class DailyV1P12GCoreBoundCausalRootSources
{
    private readonly DailyV1P12GContinuationSaveOperation operation;
    private readonly DailyV1P12GSourceBoundaryWitness witness;

    private DailyV1P12GCoreBoundCausalRootSources(
        DailyV1P12GContinuationSaveOperation operation,
        P12ACausalRootImageSnapshots snapshots)
    {
        this.operation = operation;
        witness = operation.SourceWitness;
        Snapshots = snapshots;
    }

    internal P12ACausalRootImageSnapshots Snapshots { get; }

    internal bool IsFor(DailyV1P12GContinuationSaveOperation candidate) =>
        candidate != null
        && ReferenceEquals(operation, candidate)
        && candidate.IsActive
        && ReferenceEquals(witness, candidate.SourceWitness)
        && ReferenceEquals(witness.Token, candidate.SourceWitness.Token)
        && ReferenceEquals(witness.TokenOwnerSections, candidate.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out DailyV1P12GCoreBoundCausalRootSources sources,
        out DailyV1P12GCoreBoundCausalRootCaptureFailure failure,
        out string diagnostic)
    {
        sources = null;
        failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.InvalidSaveOperation;
        diagnostic = null;
        if (operation == null || !operation.IsActive || !operation.TryRevalidate(out _))
        {
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.StaleSourceBoundary;
            diagnostic = "A current active Core continuation-save witness is required.";
            return false;
        }

        DailyV1ActiveRuntimeSession session = operation.Session;
        DailyV1P12CRuntimeRoots roots = session?.P12CRoots;
        if (roots == null)
        {
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.MissingP12CRuntimeRoots;
            diagnostic = "The active Core session has no retained selected Daily-v1 P12-C roots.";
            return false;
        }

        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        if (!roots.IsFor(session)
            || !ReferenceEquals(state, roots.ExecutionBoundaryState)
            || graph == null
            || !ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
            || !state.HasExactSelectedRuntimeExecutionRoots(
                graph, graph.PersonStore,
                state.SelectedActorChoiceStore, state.SelectedRandomSource,
                state.SelectedRuntimeIdAllocator, state.SelectedRecordSequence,
                state.SelectedDecisionStore))
        {
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootBindingMismatch;
            diagnostic = "The retained P12-C roots do not match the exact active Core session and owner graph.";
            return false;
        }

        if (!(state.SelectedRandomSource is DeterministicRandomSource randomSource))
        {
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootCaptureFailed;
            diagnostic = "The selected Daily-v1 P12-C root requires the admitted deterministic random source.";
            return false;
        }

        try
        {
            if (!P12CWorldIdentitySnapshot.TryCapture(
                    state.WorldId, out P12CWorldIdentitySnapshot worldIdentity,
                    out diagnostic))
            {
                failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootCaptureFailed;
                return false;
            }

            if (!P12CSpatialAuthoritySnapshot.TryCapture(
                    session.AdmissionContext.Profile,
                    roots.Manifest.SelectedP9ContractIdentity,
                    roots.Manifest.SelectedP9SchemaVersion,
                    roots.SpatialAuthority,
                    out P12CSpatialAuthoritySnapshot spatialAuthority,
                    out P12CSpatialAuthoritySnapshotFailure spatialFailure))
            {
                failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootCaptureFailed;
                diagnostic = "P12-C Core spatial-authority capture failed: " + spatialFailure?.Message;
                return false;
            }

            RuntimeIdAllocatorSnapshot allocator = state.SelectedRuntimeIdAllocator.CaptureSnapshot();
            SimulationRecordSequenceSnapshot recordSequence = state.SelectedRecordSequence.CaptureSnapshot();
            DeterministicRandomRootSnapshot randomRoot = randomSource.CaptureSnapshot();
            if (!P12CSelectedCoreRootStager.TryStage(
                    worldIdentity, allocator, recordSequence, spatialAuthority,
                    roots.Manifest, randomRoot, false, null, out _, out diagnostic))
            {
                failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootCaptureFailed;
                diagnostic = "The captured P12-C roots failed Core cross-validation: " + diagnostic;
                return false;
            }

            if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure staleFailure))
            {
                failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.StaleSourceBoundary;
                diagnostic = "The Core source boundary changed while P12-C roots were captured: "
                    + staleFailure + ".";
                return false;
            }

            sources = new DailyV1P12GCoreBoundCausalRootSources(operation,
                new P12ACausalRootImageSnapshots(worldIdentity, allocator, recordSequence,
                    spatialAuthority, roots.Manifest, randomRoot));
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.None;
            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12GCoreBoundCausalRootCaptureFailure.RootCaptureFailed;
            diagnostic = "A Core-bound P12-C root threw during capture: "
                + exception.GetType().Name + ".";
            return false;
        }
    }
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

/// <summary>
/// Immutable P12-D demographic snapshots captured from the exact Person and
/// Genealogy owners retained by the active Core session. The Unity adapter
/// consumes these values for image staging; it does not choose or recapture
/// their source authorities on the app-backed path.
/// </summary>
internal sealed class DailyV1P12GCoreBoundDemographicOwnerSources
{
    private readonly DailyV1P12GContinuationSaveOperation operation;
    private readonly DailyV1P12GSourceBoundaryWitness witness;

    private DailyV1P12GCoreBoundDemographicOwnerSources(
        DailyV1P12GContinuationSaveOperation operation,
        PersonStore persons,
        GenealogyStore genealogy,
        PersonStoreOwnerSnapshot personSnapshot,
        GenealogyOwnerSnapshot genealogySnapshot)
    {
        this.operation = operation;
        witness = operation.SourceWitness;
        Persons = persons;
        Genealogy = genealogy;
        PersonSnapshot = personSnapshot;
        GenealogySnapshot = genealogySnapshot;
    }

    internal PersonStore Persons { get; }
    internal GenealogyStore Genealogy { get; }
    internal PersonStoreOwnerSnapshot PersonSnapshot { get; }
    internal GenealogyOwnerSnapshot GenealogySnapshot { get; }

    internal bool IsFor(DailyV1P12GContinuationSaveOperation candidate) =>
        candidate != null
        && ReferenceEquals(operation, candidate)
        && candidate.IsActive
        && ReferenceEquals(witness, candidate.SourceWitness)
        && ReferenceEquals(witness.Token, candidate.SourceWitness.Token)
        && ReferenceEquals(witness.TokenOwnerSections, candidate.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out DailyV1P12GCoreBoundDemographicOwnerSources sources,
        out DailyV1P12GCoreBoundOwnerSourceCaptureFailure failure,
        out string diagnostic)
    {
        sources = null;
        failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.InvalidSaveOperation;
        diagnostic = null;
        if (operation == null || !operation.IsActive)
        {
            diagnostic = "A current active Core continuation-save operation is required.";
            return false;
        }
        if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure initialFailure))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
            diagnostic = "The Core continuation-save witness is stale: " + initialFailure + ".";
            return false;
        }

        DailyV1ActiveRuntimeSession session = operation.Session;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        DailyV1ExecutionBoundaryState state = session?.ExecutionBoundaryState;
        PersonStore persons = graph?.PersonStore;
        GenealogyStore genealogy = graph?.GenealogyStore;
        if (graph == null || state == null || persons == null || genealogy == null
            || !ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.MissingCoreOwnerBinding;
            diagnostic = "The active Core session does not retain the exact selected Person and Genealogy owners.";
            return false;
        }

        try
        {
            PersonStoreOwnerSnapshot personSnapshot = persons.CaptureOwnerSnapshot();
            GenealogyOwnerSnapshot genealogySnapshot = new GenealogyOwnerSnapshot(
                GenealogyOwnerBridge.CaptureOwnerSnapshot(genealogy));
            if (!MatchesOwnerSection(operation.SourceWitness.TokenOwnerSections,
                    DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    persons, personSnapshot.MembershipCount, personSnapshot.Revision)
                || !MatchesOwnerSection(operation.SourceWitness.TokenOwnerSections,
                    DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    persons, personSnapshot.BindingCount, personSnapshot.Revision)
                || !MatchesOwnerSection(operation.SourceWitness.TokenOwnerSections,
                    "p12d.genealogy.parentage",
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    genealogy, genealogySnapshot.Records?.Count ?? -1, genealogySnapshot.Revision))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCensusMismatch;
                diagnostic = "The exact Core Person/Genealogy snapshots do not match the operation's admitted census vector.";
                return false;
            }

            if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure staleFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
                diagnostic = "The Core save witness changed during demographic owner capture: "
                    + staleFailure + ".";
                return false;
            }

            sources = new DailyV1P12GCoreBoundDemographicOwnerSources(
                operation, persons, genealogy, personSnapshot, genealogySnapshot);
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "A Core demographic owner failed while making its detached snapshot: "
                + exception.GetType().Name + ".";
            return false;
        }
    }

    private static bool MatchesOwnerSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision)
    {
        if (sections == null || owner == null || cardinality < 0 || revision < 0L) return false;
        OwnerSectionCensusSnapshot match = null;
        for (int index = 0; index < sections.Count; index++)
        {
            OwnerSectionCensusSnapshot section = sections[index];
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            if (match != null) return false;
            match = section;
        }
        return match != null
            && match.SchemaVersion == schemaVersion
            && match.Role == OwnerSectionRole.Required
            && ReferenceEquals(match.OwnerInstanceIdentity, owner)
            && match.Cardinality == cardinality
            && match.Revision == revision;
    }
}
