using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ExplorableSiteKnowledgeRuntime
{
    private readonly string ownerRuntimeId;
    private readonly List<ExplorableSiteKnowledgeObservation> observations =
        new List<ExplorableSiteKnowledgeObservation>();
    private readonly IReadOnlyList<ExplorableSiteKnowledgeObservation> readOnlyObservations;
    [SerializeField] private long revision;

    public string OwnerRuntimeId => ownerRuntimeId;
    public IReadOnlyList<ExplorableSiteKnowledgeObservation> Observations => readOnlyObservations;
    public long Revision => revision;

    public ExplorableSiteKnowledgeRuntime(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Explorable site knowledge requires an owner RuntimeId.", nameof(ownerRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
        readOnlyObservations = observations.AsReadOnly();
    }

    /// <summary>Builds an unpublished owner from copied exact values without replaying observations.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        string ownerRuntimeId,
        IReadOnlyList<ExplorableSiteKnowledgeObservation> exactObservations,
        long exactRevision,
        out ExplorableSiteKnowledgeRuntime staged)
    {
        staged = null;
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) || exactObservations == null || exactRevision < 0L)
            return false;
        List<ExplorableSiteKnowledgeObservation> copy = new List<ExplorableSiteKnowledgeObservation>(exactObservations.Count);
        HashSet<string> uniqueSites = new HashSet<string>(StringComparer.Ordinal);
        foreach (ExplorableSiteKnowledgeObservation observation in exactObservations)
        {
            if (observation == null || !uniqueSites.Add(observation.SiteRuntimeId)) return false;
            copy.Add(new ExplorableSiteKnowledgeObservation(
                observation.SiteRuntimeId, observation.LocationRuntimeId, observation.ObservedDay,
                observation.ReceivedDay, observation.Source));
        }
        ExplorableSiteKnowledgeRuntime candidate = new ExplorableSiteKnowledgeRuntime(ownerRuntimeId);
        candidate.observations.AddRange(copy);
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    public bool KnowsSite(string siteRuntimeId)
    {
        return TryGetObservation(siteRuntimeId, out _);
    }

    public bool TryGetObservation(
        string siteRuntimeId,
        out ExplorableSiteKnowledgeObservation observation)
    {
        int index = FindObservationIndex(siteRuntimeId);

        if (index >= 0)
        {
            observation = observations[index];
            return true;
        }

        observation = null;
        return false;
    }

    public bool RecordObservation(ExplorableSiteKnowledgeObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.SiteRuntimeId);

        if (existingIndex < 0)
        {
            if (revision == long.MaxValue) return false;
            observations.Add(observation);
            revision++;
            return true;
        }

        if (ShouldReplace(observations[existingIndex], observation) == false)
        {
            return false;
        }

        if (revision == long.MaxValue) return false;
        observations[existingIndex] = observation;
        revision++;
        return true;
    }

    private int FindObservationIndex(string siteRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(siteRuntimeId) == true)
        {
            return -1;
        }

        for (int i = 0; i < observations.Count; i++)
        {
            ExplorableSiteKnowledgeObservation candidate = observations[i];

            if (candidate != null
                && string.Equals(candidate.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ShouldReplace(
        ExplorableSiteKnowledgeObservation existing,
        ExplorableSiteKnowledgeObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        if (incoming.ObservedDay != existing.ObservedDay)
        {
            return incoming.ObservedDay > existing.ObservedDay;
        }

        return GetSourcePriority(incoming.Source) > GetSourcePriority(existing.Source);
    }

    private static int GetSourcePriority(ExplorableSiteKnowledgeSource source)
    {
        return source == ExplorableSiteKnowledgeSource.DirectObservation ? 2 : 1;
    }
}

public sealed class ExplorableSiteKnowledgeSystem : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();

    public bool RecordInitialScenarioKnowledge(
        NpcRuntime npcRuntime,
        ExplorableSiteRuntime siteRuntime,
        long observedDay = 0L)
    {
        return Record(
            npcRuntime,
            siteRuntime,
            observedDay,
            ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
    }

    // The caller is responsible for ensuring that the NPC physically observed the site.
    public bool RecordDirectObservation(
        NpcRuntime npcRuntime,
        ExplorableSiteRuntime siteRuntime,
        long observedDay)
    {
        return Record(
            npcRuntime,
            siteRuntime,
            observedDay,
            ExplorableSiteKnowledgeSource.DirectObservation);
    }

    private bool Record(
        NpcRuntime npcRuntime,
        ExplorableSiteRuntime siteRuntime,
        long observedDay,
        ExplorableSiteKnowledgeSource source)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        if (npcRuntime == null || siteRuntime == null || siteRuntime.Location == null)
        {
            return false;
        }

        ExplorableSiteKnowledgeObservation observation = new ExplorableSiteKnowledgeObservation(
            siteRuntime.RuntimeId,
            siteRuntime.Location.RuntimeId,
            observedDay,
            observedDay,
            source);
        bool recorded = npcRuntime.ExplorableSiteKnowledge.RecordObservation(observation);

        if (npcRuntime.ExplorableSiteKnowledge.KnowsSite(siteRuntime.RuntimeId) == true)
        {
            npcRuntime.SpatialKnowledge.DiscoverLocation(siteRuntime.Location.RuntimeId);
        }

        return recorded;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }
}
