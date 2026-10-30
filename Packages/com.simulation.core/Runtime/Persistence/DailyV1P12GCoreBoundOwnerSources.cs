using System;
using System.Collections.Generic;

internal enum DailyV1P12GCoreBoundOwnerSourceCaptureFailure
{
    None,
    InvalidSaveOperation,
    MissingCoreOwnerBinding,
    OwnerCensusMismatch,
    OwnerCaptureFailed,
    StaleSourceBoundary,
    IncompleteSelectedNpcRoster
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

/// <summary>
/// Detached P12-D spatial values captured from the exact SpatialNetworkOwner
/// retained by the active Core session. Unity may project that owner, but does
/// not select or recapture the app-backed source authority.
/// </summary>
internal sealed class DailyV1P12GCoreBoundSpatialOwnerSources
{
    private readonly DailyV1P12GContinuationSaveOperation operation;
    private readonly DailyV1P12GSourceBoundaryWitness witness;

    private DailyV1P12GCoreBoundSpatialOwnerSources(
        DailyV1P12GContinuationSaveOperation operation,
        SpatialNetworkOwner owner,
        SpatialNetworkOwnerSnapshot snapshot)
    {
        this.operation = operation;
        witness = operation.SourceWitness;
        Owner = owner;
        Snapshot = snapshot;
    }

    internal SpatialNetworkOwner Owner { get; }
    internal SpatialNetworkOwnerSnapshot Snapshot { get; }

    internal bool IsFor(DailyV1P12GContinuationSaveOperation candidate) =>
        candidate != null
        && ReferenceEquals(operation, candidate)
        && candidate.IsActive
        && ReferenceEquals(witness, candidate.SourceWitness)
        && ReferenceEquals(witness.Token, candidate.SourceWitness.Token)
        && ReferenceEquals(witness.TokenOwnerSections, candidate.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out DailyV1P12GCoreBoundSpatialOwnerSources sources,
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
        SpatialNetworkOwner owner = graph?.SpatialNetwork;
        if (graph == null || state == null || owner == null
            || !ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.MissingCoreOwnerBinding;
            diagnostic = "The active Core session does not retain the exact selected SpatialNetwork owner.";
            return false;
        }

        try
        {
            SpatialNetworkOwnerSnapshot snapshot = owner.CaptureOwnerSnapshot();
            if (snapshot == null
                || snapshot.LocationRuntimeIds == null
                || snapshot.Routes == null
                || snapshot.LocationRuntimeIds.Count != owner.LocationCount
                || snapshot.Routes.Count != owner.RouteCount
                || snapshot.Revision != owner.Revision
                || !TryMatchSpatialSection(
                    operation.SourceWitness.TokenOwnerSections,
                    P12ASpatialNetworkOwnerCodec.LocationsSectionId,
                    snapshot.LocationRuntimeIds.Count,
                    snapshot.Revision,
                    out object locationsOwnerIdentity)
                || !TryMatchSpatialSection(
                    operation.SourceWitness.TokenOwnerSections,
                    P12ASpatialNetworkOwnerCodec.RoutesSectionId,
                    snapshot.Routes.Count,
                    snapshot.Revision,
                    out object routesOwnerIdentity)
                || !ReferenceEquals(locationsOwnerIdentity, routesOwnerIdentity))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCensusMismatch;
                diagnostic = "The exact Core SpatialNetwork snapshot does not match the operation's admitted Location/Route census vector.";
                return false;
            }

            if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure staleFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
                diagnostic = "The Core save witness changed during spatial owner capture: "
                    + staleFailure + ".";
                return false;
            }

            sources = new DailyV1P12GCoreBoundSpatialOwnerSources(operation, owner, snapshot);
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "The Core SpatialNetwork owner failed while making its detached snapshot: "
                + exception.GetType().Name + ".";
            return false;
        }
    }

    private static bool TryMatchSpatialSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int cardinality,
        long revision,
        out object ownerIdentity)
    {
        ownerIdentity = null;
        if (sections == null || cardinality < 0 || revision < 0L) return false;
        OwnerSectionCensusSnapshot match = null;
        for (int index = 0; index < sections.Count; index++)
        {
            OwnerSectionCensusSnapshot section = sections[index];
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            if (match != null) return false;
            match = section;
        }

        if (match == null
            || match.SchemaVersion != P12ASpatialNetworkOwnerCodec.SchemaVersion
            || match.Role != OwnerSectionRole.Required
            || match.OwnerInstanceIdentity == null
            || match.Cardinality != cardinality
            || match.Revision != revision)
            return false;
        ownerIdentity = match.OwnerInstanceIdentity;
        return true;
    }
}

/// <summary>
/// P12-D/F NPC rows captured from the ordered Core-selected NPC owner graph
/// while its continuation-save operation is active. The Unity adapter may
/// validate that its projections correspond to this complete graph, but it
/// does not select the persisted rows.
/// </summary>
internal sealed class DailyV1P12GCoreBoundNpcOwnerSources
{
    private readonly DailyV1P12GContinuationSaveOperation operation;
    private readonly DailyV1P12GSourceBoundaryWitness witness;
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<DailyV1NpcRuntimeOwners> owners;
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<P12DNpcDRow> dRows;
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<P12DNpcFRow> fRows;

    private DailyV1P12GCoreBoundNpcOwnerSources(
        DailyV1P12GContinuationSaveOperation operation,
        IList<DailyV1NpcRuntimeOwners> owners,
        IList<P12DNpcDRow> dRows,
        IList<P12DNpcFRow> fRows)
    {
        this.operation = operation;
        witness = operation.SourceWitness;
        this.owners = new System.Collections.ObjectModel.ReadOnlyCollection<DailyV1NpcRuntimeOwners>(
            new List<DailyV1NpcRuntimeOwners>(owners));
        this.dRows = new System.Collections.ObjectModel.ReadOnlyCollection<P12DNpcDRow>(
            new List<P12DNpcDRow>(dRows));
        this.fRows = new System.Collections.ObjectModel.ReadOnlyCollection<P12DNpcFRow>(
            new List<P12DNpcFRow>(fRows));
    }

    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Owners => owners;
    internal IReadOnlyList<P12DNpcDRow> DRows => dRows;
    internal IReadOnlyList<P12DNpcFRow> FRows => fRows;

    internal bool IsFor(DailyV1P12GContinuationSaveOperation candidate) =>
        candidate != null
        && ReferenceEquals(operation, candidate)
        && candidate.IsActive
        && ReferenceEquals(witness, candidate.SourceWitness)
        && ReferenceEquals(witness.Token, candidate.SourceWitness.Token)
        && ReferenceEquals(witness.TokenOwnerSections, candidate.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out DailyV1P12GCoreBoundNpcOwnerSources sources,
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

        DailyV1SelectedRuntimeOwnerGraph graph = operation.Session?.OwnerGraph;
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = operation.SourceWitness.TokenOwnerSections;
        if (graph == null || graph.Npcs == null || graph.PersonStore == null
            || !ReferenceEquals(graph, operation.Session.ExecutionBoundaryState.SelectedRuntimeOwnerGraph)
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(sections))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.MissingCoreOwnerBinding;
            diagnostic = "The active Core session does not retain a complete selected NPC graph and census vector.";
            return false;
        }

        // A selected Core graph is the whole-roster authority only when its
        // exact RuntimeId set equals the 22 dynamic NPC census families. A
        // smaller graph is a supported mixed-roster host case: leave row
        // capture to the documented whole-roster Unity compatibility path.
        if (!HasExactSelectedNpcRoster(graph.Npcs, sections))
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.IncompleteSelectedNpcRoster;
            diagnostic = "The selected Core NPC graph does not cover the complete admitted NPC roster.";
            return false;
        }

        try
        {
            List<P12DNpcDRow> dRows = new List<P12DNpcDRow>(graph.Npcs.Count);
            List<P12DNpcFRow> fRows = new List<P12DNpcFRow>(graph.Npcs.Count);
            for (int index = 0; index < graph.Npcs.Count; index++)
            {
                DailyV1NpcRuntimeOwners owner = graph.Npcs[index];
                PersonRuntime person = null;
                if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(owner.RuntimeId,
                        out PersonRuntime boundPerson))
                    person = boundPerson;

                if (!DailyV1P12DNpcCoreOwnerExporter.TryCreate(
                        graph, owner, person,
                        out P12DNpcDRow dRow,
                        out P12DNpcFRow fRow,
                        out DailyV1P12DNpcCoreOwnerExportFailure exportFailure)
                    || dRow == null || fRow == null
                    || !string.Equals(dRow.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(fRow.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                    || !MatchesOwnerSections(owner, person, sections))
                {
                    failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCensusMismatch;
                    diagnostic = "The exact Core NPC owner or its admitted 22-family census vector is invalid: "
                        + exportFailure + ".";
                    return false;
                }

                dRows.Add(dRow);
                fRows.Add(fRow);
            }

            if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure staleFailure))
            {
                failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary;
                diagnostic = "The Core save witness changed during NPC owner capture: "
                    + staleFailure + ".";
                return false;
            }

            sources = new DailyV1P12GCoreBoundNpcOwnerSources(
                operation, new List<DailyV1NpcRuntimeOwners>(graph.Npcs), dRows, fRows);
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12GCoreBoundOwnerSourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "A Core NPC owner failed while making its detached P12-D/F snapshot: "
                + exception.GetType().Name + ".";
            return false;
        }
    }

    private static bool HasExactSelectedNpcRoster(
        IReadOnlyList<DailyV1NpcRuntimeOwners> owners,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections)
    {
        if (owners == null || sections == null || owners.Count == 0) return false;
        HashSet<string> ownerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners owner in owners)
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId) || !ownerIds.Add(owner.RuntimeId))
                return false;

        string prefix = DailyV1SelectedOwnerSectionCensusContract.NpcSectionPrefixFor(0);
        HashSet<string> admittedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !section.SectionId.StartsWith(prefix, StringComparison.Ordinal)) continue;
            string runtimeId = section.SectionId.Substring(prefix.Length);
            if (string.IsNullOrWhiteSpace(runtimeId)
                || !string.Equals(section.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(0, runtimeId),
                    StringComparison.Ordinal)
                || !admittedIds.Add(runtimeId)) return false;
        }
        return admittedIds.Count == ownerIds.Count && admittedIds.SetEquals(ownerIds);
    }

    private static bool MatchesOwnerSections(
        DailyV1NpcRuntimeOwners owner,
        PersonRuntime person,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections)
    {
        if (owner == null || sections == null) return false;
        if (!owner.Inventory.TryGetCensusCardinality(out int inventoryCardinality)
            || !owner.CommercialKnowledge.TryReadCensus(
                out int commercialMarkets, out int commercialLiquidity,
                out int commercialReceipts, out long commercialRevision))
            return false;

        return Match(0, owner.ExplorableSiteKnowledge,
                owner.ExplorableSiteKnowledge.Observations.Count, owner.ExplorableSiteKnowledge.Revision)
            && Match(1, owner.LocalTopologyKnowledgeEmptyOwner,
                owner.LocalTopologyKnowledgeEmptyOwner.PlaceObservationCount,
                owner.LocalTopologyKnowledgeEmptyOwner.Revision)
            && Match(2, owner.LocalTopologyKnowledgeEmptyOwner,
                owner.LocalTopologyKnowledgeEmptyOwner.ConnectionObservationCount,
                owner.LocalTopologyKnowledgeEmptyOwner.Revision)
            && Match(3, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.OppositionObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision)
            && Match(4, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.NotableItemObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision)
            && Match(5, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.CommonResourceObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision)
            && Match(6, owner.AdventureSiteIntelKnowledge,
                owner.AdventureSiteIntelKnowledge.AccessObservations.Count,
                owner.AdventureSiteIntelKnowledge.Revision)
            && Match(7, null, commercialMarkets, commercialRevision)
            && Match(8, null, commercialLiquidity, commercialRevision)
            && Match(9, null, commercialReceipts, commercialRevision)
            && Match(10, owner.SpatialKnowledge,
                owner.SpatialKnowledge.KnownLocationCount, owner.SpatialKnowledge.Revision)
            && Match(11, owner.SpatialKnowledge,
                owner.SpatialKnowledge.KnownRouteCount, owner.SpatialKnowledge.Revision)
            && Match(12, null, 1, owner.Travel.Revision)
            && Match(13, owner.Inventory, inventoryCardinality, owner.Inventory.Revision)
            && Match(14, owner.MoneyAccount, 1, owner.MoneyAccount.Revision)
            && Match(15, owner.MerchantTradePlan, 1, owner.MerchantTradePlan.Revision)
            && Match(16, null, 1, owner.TravelPlan.Revision)
            && Match(17, null, 0, 0L)
            && Match(18, null, 0, 0L)
            && Match(19, null, 1, owner.CrimeJusticeRevision)
            && Match(20, null, owner.ActionSlot.CurrentAction == null ? 0 : 1,
                owner.ActionSlot.Revision)
            && Match(21, null, 1, owner.Lifecycle.LifeStateRevision)
            && (person != null
                ? MatchSection(
                    DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(person.PersonId),
                    person, 1, PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person))
                : MatchSection(
                    DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(owner.RuntimeId),
                    null, 1, owner.Residence.Revision));

        bool Match(int family, object ownerIdentity, int cardinality, long revision) =>
            MatchSection(
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(family, owner.RuntimeId),
                ownerIdentity, cardinality, revision);

        bool MatchSection(string sectionId, object ownerIdentity, int cardinality, long revision)
        {
            OwnerSectionCensusSnapshot match = null;
            foreach (OwnerSectionCensusSnapshot section in sections)
            {
                if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
                if (match != null) return false;
                match = section;
            }

            return match != null
                && match.SchemaVersion == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                && match.Role == OwnerSectionRole.Required
                && match.OwnerInstanceIdentity != null
                && (ownerIdentity == null || ReferenceEquals(match.OwnerInstanceIdentity, ownerIdentity))
                && match.Cardinality == cardinality
                && match.Revision == revision;
        }
    }
}

/// <summary>
/// Encodes P12-F sections from the exact immutable Core owner snapshots bound
/// to one active continuation-save operation.
/// </summary>
internal static class DailyV1P12FCoreOwnerSectionEncoder
{
    internal static bool TryEncode(
        DailyV1P12GCoreBoundOwnerSources sources,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (sources == null
            || sources.PoliticalKnowledge == null
            || sources.ActorChoices == null
            || sources.Directives == null
            || sources.TravelParties == null
            || sources.Expeditions == null)
            return false;

        if (!P12AFPoliticalKnowledgeCodec.TryEncode(sources.PoliticalKnowledge,
                out P12AOwnerSection politicalKnowledge, out result)
            || !P12AFScheduledDirectiveCodec.TryEncode(sources.Directives,
                out P12AOwnerSection directives, out result)
            || !P12AActorChoiceCodec.TryEncode(sources.ActorChoices,
                out P12AOwnerSection actorChoices, out result)
            || !P12AFCommitmentOwnerCodec.TryEncodeTravelParties(sources.TravelParties,
                out P12AOwnerSection travelParties, out result)
            || !P12AFCommitmentOwnerCodec.TryEncodeExpeditions(sources.Expeditions,
                out P12AOwnerSection expeditions, out result))
            return false;

        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>
        {
            politicalKnowledge,
            directives,
            actorChoices,
            travelParties,
            expeditions
        };
        encoded.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left?.Id, right?.Id));
        for (int index = 0; index < encoded.Count; index++)
        {
            P12AOwnerSection section = encoded[index];
            if (section == null || string.IsNullOrWhiteSpace(section.Id)
                || section.Role != OwnerSectionRole.Required
                || (index > 0 && string.Equals(encoded[index - 1].Id,
                    section.Id, StringComparison.Ordinal)))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection,
                    section?.Id);
                return false;
            }
        }

        sections = encoded.AsReadOnly();
        result = null;
        return true;
    }
}
