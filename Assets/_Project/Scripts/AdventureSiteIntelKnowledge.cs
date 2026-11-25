using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AdventureSiteIntelKnowledgeRuntime : ISerializationCallbackReceiver
{
    private readonly string ownerRuntimeId;
    [SerializeField] private long revision;
    [NonSerialized] private DailyV1AdventureSiteIntelKnowledgeOwner selectedOwner;
    private readonly List<AdventureOppositionObservation> oppositionObservations = new List<AdventureOppositionObservation>();
    private readonly List<AdventureNotableItemObservation> notableItemObservations = new List<AdventureNotableItemObservation>();
    private readonly List<AdventureCommonResourceObservation> commonResourceObservations = new List<AdventureCommonResourceObservation>();
    private readonly List<AdventureAccessObservation> accessObservations = new List<AdventureAccessObservation>();

    public string OwnerRuntimeId => ownerRuntimeId;
    public long Revision => selectedOwner?.Revision ?? revision;
    public IReadOnlyList<AdventureOppositionObservation> OppositionObservations => selectedOwner?.OppositionObservations ?? oppositionObservations.AsReadOnly();
    public IReadOnlyList<AdventureNotableItemObservation> NotableItemObservations => selectedOwner?.NotableItemObservations ?? notableItemObservations.AsReadOnly();
    public IReadOnlyList<AdventureCommonResourceObservation> CommonResourceObservations => selectedOwner?.CommonResourceObservations ?? commonResourceObservations.AsReadOnly();
    public IReadOnlyList<AdventureAccessObservation> AccessObservations => selectedOwner?.AccessObservations ?? accessObservations.AsReadOnly();
    internal DailyV1AdventureSiteIntelKnowledgeOwner SelectedOwner => selectedOwner;

    public AdventureSiteIntelKnowledgeRuntime(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Adventure intel requires an owner RuntimeId.", nameof(ownerRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
    }

    internal bool CanBindSelectedOwner(DailyV1AdventureSiteIntelKnowledgeOwner owner)
    {
        if (owner == null || !string.Equals(owner.OwnerRuntimeId, ownerRuntimeId, StringComparison.Ordinal)
            || (selectedOwner != null && !ReferenceEquals(selectedOwner, owner))) return false;
        if (selectedOwner != null) return true;

        bool hasProjection = revision != 0L || oppositionObservations.Count != 0
            || notableItemObservations.Count != 0 || commonResourceObservations.Count != 0
            || accessObservations.Count != 0;
        if (!hasProjection) return true;
        if (owner.Revision == 0L && owner.OppositionObservations.Count == 0
            && owner.NotableItemObservations.Count == 0 && owner.CommonResourceObservations.Count == 0
            && owner.AccessObservations.Count == 0)
            return owner.CanCreateInitialSnapshot(oppositionObservations, notableItemObservations,
                commonResourceObservations, accessObservations, revision);
        return owner.MatchesSnapshot(oppositionObservations, notableItemObservations,
            commonResourceObservations, accessObservations, revision);
    }

    internal bool TryBindSelectedOwner(DailyV1AdventureSiteIntelKnowledgeOwner owner)
    {
        if (!CanBindSelectedOwner(owner)) return false;
        bool hasProjection = revision != 0L || oppositionObservations.Count != 0
            || notableItemObservations.Count != 0 || commonResourceObservations.Count != 0
            || accessObservations.Count != 0;
        if (selectedOwner == null && hasProjection && owner.Revision == 0L
            && owner.OppositionObservations.Count == 0 && owner.NotableItemObservations.Count == 0
            && owner.CommonResourceObservations.Count == 0 && owner.AccessObservations.Count == 0
            && !owner.TryCreateInitialSnapshot(oppositionObservations, notableItemObservations,
                commonResourceObservations, accessObservations, revision)) return false;
        selectedOwner = owner;
        CopySelectedOwnerToSerializationProjection();
        return true;
    }

    internal bool TryCreateSelectedOwnerSnapshot(
        out DailyV1AdventureSiteIntelKnowledgeOwner owner)
    {
        if (selectedOwner != null)
        {
            owner = selectedOwner;
            return true;
        }
        DailyV1AdventureSiteIntelKnowledgeOwner candidate =
            new DailyV1AdventureSiteIntelKnowledgeOwner(ownerRuntimeId);
        if (!candidate.TryCreateInitialSnapshot(oppositionObservations, notableItemObservations,
            commonResourceObservations, accessObservations, revision))
        {
            owner = null;
            return false;
        }
        owner = candidate;
        return true;
    }

    public void OnBeforeSerialize() => CopySelectedOwnerToSerializationProjection();
    public void OnAfterDeserialize() => selectedOwner = null;

    private void CopySelectedOwnerToSerializationProjection()
    {
        if (selectedOwner == null) return;
        oppositionObservations.Clear();
        oppositionObservations.AddRange(selectedOwner.OppositionObservations);
        notableItemObservations.Clear();
        notableItemObservations.AddRange(selectedOwner.NotableItemObservations);
        commonResourceObservations.Clear();
        commonResourceObservations.AddRange(selectedOwner.CommonResourceObservations);
        accessObservations.Clear();
        accessObservations.AddRange(selectedOwner.AccessObservations);
        revision = selectedOwner.Revision;
    }

    /// <summary>Builds an unpublished owner from copied exact values without replaying observations.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        string ownerRuntimeId,
        IReadOnlyList<AdventureOppositionObservation> opposition,
        IReadOnlyList<AdventureNotableItemObservation> notableItems,
        IReadOnlyList<AdventureCommonResourceObservation> resources,
        IReadOnlyList<AdventureAccessObservation> access,
        long exactRevision,
        out AdventureSiteIntelKnowledgeRuntime staged)
    {
        staged = null;
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) || opposition == null || notableItems == null
            || resources == null || access == null || exactRevision < 0L)
            return false;
        AdventureSiteIntelKnowledgeRuntime candidate = new AdventureSiteIntelKnowledgeRuntime(ownerRuntimeId);
        if (!CopyUnique(opposition, candidate.oppositionObservations)
            || !CopyUnique(notableItems, candidate.notableItemObservations)
            || !CopyUnique(resources, candidate.commonResourceObservations)
            || !CopyUnique(access, candidate.accessObservations))
            return false;
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    private static bool CopyUnique<T>(IReadOnlyList<T> source, List<T> destination)
        where T : AdventureSiteIntelObservation
    {
        for (int i = 0; i < source.Count; i++)
        {
            T value = source[i];
            if (value == null) return false;
            for (int j = 0; j < destination.Count; j++)
                if (destination[j].HasSameIdentity(value)) return false;
            destination.Add(value);
        }
        return true;
    }

    public bool RecordObservation(AdventureOppositionObservation observation)
    {
        return Record(oppositionObservations, observation);
    }

    public bool RecordObservation(AdventureNotableItemObservation observation)
    {
        return Record(notableItemObservations, observation);
    }

    public bool RecordObservation(AdventureCommonResourceObservation observation)
    {
        return Record(commonResourceObservations, observation);
    }

    public bool RecordObservation(AdventureAccessObservation observation)
    {
        return Record(accessObservations, observation);
    }

    public bool KnowsOpposition(string siteRuntimeId, string oppositionRuntimeId)
    {
        return TryGetOpposition(siteRuntimeId, oppositionRuntimeId, out AdventureOppositionObservation observation)
            && observation.ObservedState == AdventureOppositionObservedState.Active;
    }

    public bool TryGetOpposition(
        string siteRuntimeId,
        string oppositionRuntimeId,
        out AdventureOppositionObservation observation)
    {
        foreach (AdventureOppositionObservation candidate in OppositionObservations)
        {
            if (candidate != null
                && string.Equals(candidate.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal)
                && string.Equals(candidate.OppositionRuntimeId, oppositionRuntimeId, StringComparison.Ordinal))
            {
                observation = candidate;
                return true;
            }
        }

        observation = null;
        return false;
    }

    public bool KnowsNotableItem(string siteRuntimeId, string notableItemRuntimeId)
    {
        foreach (AdventureNotableItemObservation observation in NotableItemObservations)
        {
            if (observation != null
                && string.Equals(observation.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal)
                && string.Equals(observation.NotableItemRuntimeId, notableItemRuntimeId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool KnowsCommonResource(string siteRuntimeId, string itemDefinitionId)
    {
        foreach (AdventureCommonResourceObservation observation in CommonResourceObservations)
        {
            if (observation != null
                && observation.ObservedAmount > 0
                && string.Equals(observation.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal)
                && string.Equals(observation.ItemDefinitionId, itemDefinitionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool Record<T>(List<T> observations, T incoming) where T : AdventureSiteIntelObservation
    {
        if (selectedOwner != null)
        {
            bool committed = incoming is AdventureOppositionObservation opposition
                ? selectedOwner.RecordObservation(opposition)
                : incoming is AdventureNotableItemObservation notable
                    ? selectedOwner.RecordObservation(notable)
                    : incoming is AdventureCommonResourceObservation resource
                        ? selectedOwner.RecordObservation(resource)
                        : incoming is AdventureAccessObservation access
                            && selectedOwner.RecordObservation(access);
            if (committed) CopySelectedOwnerToSerializationProjection();
            return committed;
        }

        if (incoming == null)
        {
            return false;
        }

        for (int i = 0; i < observations.Count; i++)
        {
            T existing = observations[i];
            if (existing == null || existing.HasSameIdentity(incoming) == false)
            {
                continue;
            }

            if (ShouldReplace(existing, incoming) == false)
            {
                return false;
            }

            if (revision == long.MaxValue) return false;
            observations[i] = incoming;
            revision++;
            return true;
        }

        if (revision == long.MaxValue) return false;
        observations.Add(incoming);
        revision++;
        return true;
    }

    private static bool ShouldReplace(AdventureSiteIntelObservation existing, AdventureSiteIntelObservation incoming)
    {
        if (incoming.ObservedDay != existing.ObservedDay)
        {
            return incoming.ObservedDay > existing.ObservedDay;
        }

        int sourceComparison = GetSourcePriority(incoming.Source).CompareTo(GetSourcePriority(existing.Source));
        return sourceComparison > 0 || (sourceComparison == 0 && incoming.ReceivedDay > existing.ReceivedDay);
    }

    private static int GetSourcePriority(AdventureIntelSource source)
    {
        if (source == AdventureIntelSource.DirectObservation)
        {
            return 3;
        }

        return source == AdventureIntelSource.SharedByNpc ? 2 : 1;
    }
}

public sealed class AdventureSiteIntelKnowledgeSystem
{
    private readonly LocalTopologyKnowledgeSystem localTopologyKnowledgeSystem = new LocalTopologyKnowledgeSystem();

    public bool RecordDirectObservation(
        NpcRuntime npcRuntime,
        ExplorableSiteRuntime siteRuntime,
        LocalPlaceRuntime localPlace,
        LocalTopologyRuntime topology,
        PlaceContentStore contentStore,
        long observedDay)
    {
        if (npcRuntime == null || siteRuntime == null || contentStore == null || observedDay < 0L)
        {
            return false;
        }

        PlaceContentOwnerReference owner;
        if (localPlace != null)
        {
            if (topology == null
                || topology.ContainsPlace(localPlace) == false
                || string.Equals(topology.Owner.OwnerRuntimeId, siteRuntime.RuntimeId, StringComparison.Ordinal) == false)
            {
                return false;
            }

            localTopologyKnowledgeSystem.RecordDirectObservation(npcRuntime, topology, localPlace, observedDay);
            foreach (LocalTopologyConnectionRuntime connection in topology.GetOutgoingConnections(localPlace))
            {
                localTopologyKnowledgeSystem.RecordDirectObservation(npcRuntime, topology, connection, observedDay);
            }

            owner = PlaceContentOwnerReference.ForLocalPlace(localPlace);
        }
        else
        {
            owner = PlaceContentOwnerReference.ForExplorableSite(siteRuntime);
        }

        if (contentStore.TryGet(owner, out PlaceContentRuntime content) == false)
        {
            return true;
        }

        AdventureSiteIntelKnowledgeRuntime knowledge = npcRuntime.AdventureSiteIntelKnowledge;
        knowledge.RecordObservation(new AdventureAccessObservation(
            siteRuntime.RuntimeId,
            localPlace?.RuntimeId,
            content.AccessState,
            observedDay,
            observedDay,
            AdventureIntelSource.DirectObservation));

        foreach (PlaceOppositionRuntime opposition in content.Oppositions)
        {
            if (opposition == null)
            {
                continue;
            }

            knowledge.RecordObservation(new AdventureOppositionObservation(
                siteRuntime.RuntimeId,
                localPlace?.RuntimeId,
                opposition.RuntimeId,
                opposition.IsActive ? AdventureOppositionObservedState.Active : AdventureOppositionObservedState.Resolved,
                observedDay,
                observedDay,
                AdventureIntelSource.DirectObservation));
        }

        foreach (NotableItemRuntime notable in content.NotableContent)
        {
            if (notable != null)
            {
                knowledge.RecordObservation(new AdventureNotableItemObservation(
                    siteRuntime.RuntimeId,
                    localPlace?.RuntimeId,
                    notable.RuntimeId,
                    notable.DefinitionId,
                    observedDay,
                    observedDay,
                    AdventureIntelSource.DirectObservation));
            }
        }

        foreach (PlaceContentStackRuntime stack in content.StackedContent)
        {
            if (stack != null)
            {
                knowledge.RecordObservation(new AdventureCommonResourceObservation(
                    siteRuntime.RuntimeId,
                    localPlace?.RuntimeId,
                    stack.ItemDefinitionId,
                    stack.Amount,
                    null,
                    observedDay,
                    observedDay,
                    AdventureIntelSource.DirectObservation));
            }
        }

        return true;
    }
}
