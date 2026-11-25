using System;
using System.Collections.Generic;

/// <summary>
/// Binds the selected local-knowledge owners to the enclosing runtime's P12
/// mutation callbacks. This object owns no session, thread gate, operation
/// count, clock, or mutation epoch; the single active Core runtime supplies
/// those authorities and serializes calls to this binding.
/// </summary>
internal sealed class DailyV1SelectedLocalKnowledgeOwnerBinding
{
    private sealed class BoundNpcOwners
    {
        internal SpatialKnowledgeRuntime Spatial;
        internal DailyV1CommercialKnowledgeOwner Commercial;
        internal Func<bool> SpatialMutationAdmission;
        internal Action SpatialMutationCommitted;
        internal Func<bool> CommercialMutationAdmission;
        internal Action CommercialMutationCommitted;
        internal bool SpatialBound;
        internal bool CommercialBound;
    }

    private readonly List<BoundNpcOwners> boundOwners = new List<BoundNpcOwners>();
    private readonly Func<bool> isCurrentRuntimeOperation;
    private readonly Func<bool> mutationAdmission;
    private readonly Action mutationCommitted;
    private DailyV1ExecutionBoundaryState coreState;
    private bool released;

    internal DailyV1SelectedRuntimeOwnerGraph Graph { get; private set; }

    private DailyV1SelectedLocalKnowledgeOwnerBinding(
        DailyV1SelectedRuntimeOwnerGraph graph,
        Func<bool> isCurrentRuntimeOperation,
        Func<bool> mutationAdmission,
        Action mutationCommitted)
    {
        Graph = graph;
        this.isCurrentRuntimeOperation = isCurrentRuntimeOperation;
        this.mutationAdmission = mutationAdmission;
        this.mutationCommitted = mutationCommitted;
    }

    /// <summary>
    /// Binds every selected NPC's two local-knowledge owners to callbacks
    /// supplied by the one enclosing runtime. If any owner is invalid or
    /// aliased, all prior successful bindings are rolled back before failure.
    /// </summary>
    internal static bool TryCreate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        Func<bool> isCurrentRuntimeOperation,
        Func<bool> mutationAdmission,
        Action mutationCommitted,
        out DailyV1SelectedLocalKnowledgeOwnerBinding binding)
    {
        binding = null;
        if (graph == null || graph.Definitions == null || graph.Npcs == null
            || isCurrentRuntimeOperation == null || mutationAdmission == null
            || mutationCommitted == null)
            return false;

        DailyV1SelectedLocalKnowledgeOwnerBinding candidate =
            new DailyV1SelectedLocalKnowledgeOwnerBinding(
                graph, isCurrentRuntimeOperation, mutationAdmission, mutationCommitted);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || !ReferenceEquals(npc.Definitions, graph.Definitions)
                || npc.SpatialKnowledge == null || npc.CommercialKnowledge == null)
            {
                candidate.RollBackAfterFailedCreate(ref binding);
                return false;
            }
            foreach (BoundNpcOwners prior in candidate.boundOwners)
                if (ReferenceEquals(prior.Spatial, npc.SpatialKnowledge)
                    || ReferenceEquals(prior.Commercial, npc.CommercialKnowledge))
                {
                    candidate.RollBackAfterFailedCreate(ref binding);
                    return false;
                }

            BoundNpcOwners owners = new BoundNpcOwners
            {
                Spatial = npc.SpatialKnowledge,
                Commercial = npc.CommercialKnowledge,
                SpatialMutationAdmission = candidate.mutationAdmission,
                SpatialMutationCommitted = candidate.mutationCommitted,
                CommercialMutationAdmission = candidate.mutationAdmission,
                CommercialMutationCommitted = candidate.mutationCommitted
            };
            candidate.boundOwners.Add(owners);
            try
            {
                owners.Spatial.BindP12MutationBoundary(
                    owners.SpatialMutationAdmission, owners.SpatialMutationCommitted);
                owners.SpatialBound = true;
                owners.Commercial.BindMutationBoundary(
                    owners.CommercialMutationAdmission, owners.CommercialMutationCommitted);
                owners.CommercialBound = true;
            }
            catch
            {
                candidate.RollBackAfterFailedCreate(ref binding);
                return false;
            }
        }

        binding = candidate;
        return true;
    }

    /// <summary>
    /// Installs the long-lived Core P12 guard on every exact local-knowledge
    /// owner in the admitted graph. The callbacks route writes through either
    /// the active Merchant operation or the enclosing DailyAdvance owner
    /// mutation protocol; no Unity host port participates.
    /// </summary>
    internal static bool TryCreateCoreSelected(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1SelectedLocalKnowledgeOwnerBinding binding)
    {
        binding = null;
        if (state == null || !IsValidCoreGraph(graph)) return false;

        DailyV1SelectedLocalKnowledgeOwnerBinding candidate =
            new DailyV1SelectedLocalKnowledgeOwnerBinding(
                graph,
                () => state.IsSelectedDailyAdvanceCoreTurnActive,
                () => false,
                () => { })
            {
                coreState = state
            };

        if (!candidate.TryBindCoreGraph(graph))
        {
            candidate.RollBackAfterFailedCreate(ref binding);
            return false;
        }

        binding = candidate;
        return true;
    }

    /// <summary>
    /// Rebinds Core's exact owner callbacks as one idle-boundary transition.
    /// If the replacement cannot be completely bound, the old graph is
    /// restored; callers fail closed if that recovery cannot be completed.
    /// </summary>
    internal bool TryRebindCoreSelected(DailyV1SelectedRuntimeOwnerGraph replacement)
    {
        if (coreState == null || released || !IsValidCoreGraph(replacement)) return false;
        if (ReferenceEquals(Graph, replacement)) return true;
        if (isCurrentRuntimeOperation()) return false;

        DailyV1SelectedRuntimeOwnerGraph previous = Graph;
        if (!ReleaseBindings()) return false;
        Graph = replacement;
        if (TryBindCoreGraph(replacement)) return true;

        ReleaseBindings();
        Graph = previous;
        TryBindCoreGraph(previous);
        return false;
    }

    /// <summary>
    /// Runs the existing Core stage only while the enclosing runtime confirms
    /// its owner-thread operation is active. The caller owns epoch accounting,
    /// failure faulting, and synchronization around this call.
    /// </summary>
    internal bool TryRefreshLocalKnowledge(long absoluteDay)
    {
        if (released || absoluteDay < 0L || !isCurrentRuntimeOperation()) return false;
        return DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(Graph, absoluteDay);
    }

    /// <summary>Releases the owner callbacks after the enclosing runtime is idle.</summary>
    internal bool TryRelease()
    {
        if (released || isCurrentRuntimeOperation()) return false;
        if (!ReleaseBindings()) return false;
        released = true;
        return true;
    }

    private bool ReleaseBindings()
    {
        bool succeeded = true;
        for (int index = boundOwners.Count - 1; index >= 0; index--)
        {
            BoundNpcOwners owners = boundOwners[index];
            if (owners.CommercialBound)
            {
                if (owners.Commercial.UnbindMutationBoundary(
                        owners.CommercialMutationAdmission,
                        owners.CommercialMutationCommitted))
                    owners.CommercialBound = false;
                else
                    succeeded = false;
            }
            if (owners.SpatialBound)
            {
                if (owners.Spatial.UnbindP12MutationBoundary(
                        owners.SpatialMutationAdmission,
                        owners.SpatialMutationCommitted))
                    owners.SpatialBound = false;
                else
                    succeeded = false;
            }
            if (!owners.CommercialBound && !owners.SpatialBound)
                boundOwners.RemoveAt(index);
        }
        return succeeded && boundOwners.Count == 0;
    }

    private bool TryBindCoreGraph(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (coreState == null || !IsValidCoreGraph(graph) || boundOwners.Count != 0)
            return false;

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            string runtimeId = npc.RuntimeId;
            DailyV1ExecutionBoundaryState state = coreState;
            DailyV1SelectedRuntimeOwnerGraph exactGraph = graph;
            CreateCoreMutationCallbacks(state, exactGraph,
                CreateCoreSpatialSectionIds(runtimeId),
                out Func<bool> spatialAdmission, out Action spatialCommitted);
            CreateCoreMutationCallbacks(state, exactGraph,
                CreateCoreCommercialSectionIds(runtimeId),
                out Func<bool> commercialAdmission, out Action commercialCommitted);

            BoundNpcOwners owners = new BoundNpcOwners
            {
                Spatial = npc.SpatialKnowledge,
                Commercial = npc.CommercialKnowledge,
                SpatialMutationAdmission = spatialAdmission,
                SpatialMutationCommitted = spatialCommitted,
                CommercialMutationAdmission = commercialAdmission,
                CommercialMutationCommitted = commercialCommitted
            };
            boundOwners.Add(owners);
            try
            {
                owners.Spatial.BindP12MutationBoundary(
                    spatialAdmission, spatialCommitted);
                owners.SpatialBound = true;
                owners.Commercial.BindMutationBoundary(
                    commercialAdmission, commercialCommitted);
                owners.CommercialBound = true;
            }
            catch
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsValidCoreGraph(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph?.Definitions == null || graph.Npcs == null) return false;
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<SpatialKnowledgeRuntime> spatialOwners =
            new HashSet<SpatialKnowledgeRuntime>();
        HashSet<DailyV1CommercialKnowledgeOwner> commercialOwners =
            new HashSet<DailyV1CommercialKnowledgeOwner>();
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !runtimeIds.Add(npc.RuntimeId)
                || !graph.ContainsExactNpcOwner(npc)
                || !ReferenceEquals(npc.Definitions, graph.Definitions)
                || npc.SpatialKnowledge == null || npc.CommercialKnowledge == null
                || !ReferenceEquals(npc.CommercialKnowledge.Definitions, graph.Definitions)
                || !spatialOwners.Add(npc.SpatialKnowledge)
                || !commercialOwners.Add(npc.CommercialKnowledge))
                return false;
        }
        return true;
    }

    private void CreateCoreMutationCallbacks(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph exactGraph,
        string[] sections,
        out Func<bool> admission,
        out Action committed)
    {
        admission = () =>
        {
            if (ReferenceEquals(coreState, state)
                && ReferenceEquals(Graph, exactGraph)
                && state.TryCanCommitCoreSelectedLocalKnowledgeMutation(
                    exactGraph, sections))
                return true;

            // Owner APIs reach this callback only after accepting a valid,
            // changed candidate write. A refusal here must preserve the owner
            // facts and close P12 capture rather than leave a stale token usable.
            state.MarkSelectedRuntimeCensusProtocolFaulted();
            state.InvalidateCurrentCaptureToken();
            return false;
        };
        committed = () =>
        {
            if (ReferenceEquals(coreState, state)
                && ReferenceEquals(Graph, exactGraph)
                && state.TryNotifyCoreSelectedLocalKnowledgeMutation(
                    exactGraph, sections))
                return;

            state.MarkSelectedRuntimeCensusProtocolFaulted();
            state.InvalidateCurrentCaptureToken();
        };
    }

    private static string[] CreateCoreSpatialSectionIds(string runtimeId)
    {
        string[] sectionIds =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix
                + runtimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix
                + runtimeId
        };
        Array.Sort(sectionIds, StringComparer.Ordinal);
        return sectionIds;
    }

    private static string[] CreateCoreCommercialSectionIds(string runtimeId)
    {
        string[] sectionIds =
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix
                + runtimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix
                + runtimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix
                + runtimeId
        };
        Array.Sort(sectionIds, StringComparer.Ordinal);
        return sectionIds;
    }

    private void RollBackAfterFailedCreate(
        ref DailyV1SelectedLocalKnowledgeOwnerBinding rollbackHandle)
    {
        if (!ReleaseBindings())
        {
            // Keep a recovery handle when a foreign/existing binding prevents
            // complete rollback; callers can retry release while the shared
            // runtime remains closed to mutation.
            rollbackHandle = this;
        }
    }
}
