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
        internal bool SpatialBound;
        internal bool CommercialBound;
    }

    private readonly List<BoundNpcOwners> boundOwners = new List<BoundNpcOwners>();
    private readonly Func<bool> isCurrentRuntimeOperation;
    private readonly Func<bool> mutationAdmission;
    private readonly Action mutationCommitted;
    private bool released;

    internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }

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
                Commercial = npc.CommercialKnowledge
            };
            candidate.boundOwners.Add(owners);
            try
            {
                owners.Spatial.BindP12MutationBoundary(
                    candidate.mutationAdmission, candidate.mutationCommitted);
                owners.SpatialBound = true;
                owners.Commercial.BindMutationBoundary(
                    candidate.mutationAdmission, candidate.mutationCommitted);
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
                        mutationAdmission, mutationCommitted))
                    owners.CommercialBound = false;
                else
                    succeeded = false;
            }
            if (owners.SpatialBound)
            {
                if (owners.Spatial.UnbindP12MutationBoundary(
                        mutationAdmission, mutationCommitted))
                    owners.SpatialBound = false;
                else
                    succeeded = false;
            }
            if (!owners.CommercialBound && !owners.SpatialBound)
                boundOwners.RemoveAt(index);
        }
        return succeeded && boundOwners.Count == 0;
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
