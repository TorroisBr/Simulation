using System;
using System.Collections.Generic;

public enum AdventureIntelSource
{
    InitialScenarioKnowledge,
    SharedByNpc,
    DirectObservation
}

public enum AdventureOppositionObservedState
{
    Active,
    Resolved
}

[Serializable]
public abstract class AdventureSiteIntelObservation
{
    private readonly string siteRuntimeId;
    private readonly string localPlaceRuntimeId;
    private readonly long observedDay;
    private readonly long receivedDay;
    private readonly AdventureIntelSource source;
    private readonly string sourceRuntimeId;

    public string SiteRuntimeId => siteRuntimeId;
    public string LocalPlaceRuntimeId => localPlaceRuntimeId;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public AdventureIntelSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    protected AdventureSiteIntelObservation(
        string siteRuntimeId,
        string localPlaceRuntimeId,
        long observedDay,
        long receivedDay,
        AdventureIntelSource source,
        string sourceRuntimeId = null)
    {
        if (string.IsNullOrWhiteSpace(siteRuntimeId) == true)
        {
            throw new ArgumentException("Adventure intel requires a SiteRuntimeId.", nameof(siteRuntimeId));
        }

        if (observedDay < 0L || receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay));
        }

        if (Enum.IsDefined(typeof(AdventureIntelSource), source) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        if (source == AdventureIntelSource.SharedByNpc && string.IsNullOrWhiteSpace(sourceRuntimeId) == true)
        {
            throw new ArgumentException("Shared adventure intel requires a source NPC RuntimeId.", nameof(sourceRuntimeId));
        }

        this.siteRuntimeId = siteRuntimeId;
        this.localPlaceRuntimeId = Normalize(localPlaceRuntimeId);
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
        this.sourceRuntimeId = Normalize(sourceRuntimeId);
    }

    protected bool HasSameScope(AdventureSiteIntelObservation other)
    {
        return other != null
            && string.Equals(siteRuntimeId, other.SiteRuntimeId, StringComparison.Ordinal)
            && string.Equals(localPlaceRuntimeId, other.LocalPlaceRuntimeId, StringComparison.Ordinal);
    }

    internal abstract bool HasSameIdentity(AdventureSiteIntelObservation other);

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

[Serializable]
public sealed class AdventureOppositionObservation : AdventureSiteIntelObservation
{
    private readonly string oppositionRuntimeId;
    private readonly AdventureOppositionObservedState observedState;

    public string OppositionRuntimeId => oppositionRuntimeId;
    public AdventureOppositionObservedState ObservedState => observedState;

    public AdventureOppositionObservation(
        string siteRuntimeId,
        string localPlaceRuntimeId,
        string oppositionRuntimeId,
        AdventureOppositionObservedState observedState,
        long observedDay,
        long receivedDay,
        AdventureIntelSource source,
        string sourceRuntimeId = null)
        : base(siteRuntimeId, localPlaceRuntimeId, observedDay, receivedDay, source, sourceRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(oppositionRuntimeId) == true)
        {
            throw new ArgumentException("Opposition intel requires an OppositionRuntimeId.", nameof(oppositionRuntimeId));
        }

        if (Enum.IsDefined(typeof(AdventureOppositionObservedState), observedState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(observedState));
        }

        this.oppositionRuntimeId = oppositionRuntimeId;
        this.observedState = observedState;
    }

    internal override bool HasSameIdentity(AdventureSiteIntelObservation other)
    {
        return other is AdventureOppositionObservation opposition
            && HasSameScope(opposition)
            && string.Equals(oppositionRuntimeId, opposition.OppositionRuntimeId, StringComparison.Ordinal);
    }
}

[Serializable]
public sealed class AdventureNotableItemObservation : AdventureSiteIntelObservation
{
    private readonly string notableItemRuntimeId;
    private readonly string itemDefinitionId;

    public string NotableItemRuntimeId => notableItemRuntimeId;
    public string ItemDefinitionId => itemDefinitionId;

    public AdventureNotableItemObservation(
        string siteRuntimeId,
        string localPlaceRuntimeId,
        string notableItemRuntimeId,
        string itemDefinitionId,
        long observedDay,
        long receivedDay,
        AdventureIntelSource source,
        string sourceRuntimeId = null)
        : base(siteRuntimeId, localPlaceRuntimeId, observedDay, receivedDay, source, sourceRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(notableItemRuntimeId) == true
            || string.IsNullOrWhiteSpace(itemDefinitionId) == true)
        {
            throw new ArgumentException("Notable-item intel requires runtime and definition IDs.");
        }

        this.notableItemRuntimeId = notableItemRuntimeId;
        this.itemDefinitionId = itemDefinitionId;
    }

    internal override bool HasSameIdentity(AdventureSiteIntelObservation other)
    {
        return other is AdventureNotableItemObservation notable
            && HasSameScope(notable)
            && string.Equals(notableItemRuntimeId, notable.NotableItemRuntimeId, StringComparison.Ordinal);
    }
}

[Serializable]
public sealed class AdventureCommonResourceObservation : AdventureSiteIntelObservation
{
    private readonly string itemDefinitionId;
    private readonly int observedAmount;
    private readonly string resourceCategory;

    public string ItemDefinitionId => itemDefinitionId;
    public int ObservedAmount => observedAmount;
    public string ResourceCategory => resourceCategory;

    public AdventureCommonResourceObservation(
        string siteRuntimeId,
        string localPlaceRuntimeId,
        string itemDefinitionId,
        int observedAmount,
        string resourceCategory,
        long observedDay,
        long receivedDay,
        AdventureIntelSource source,
        string sourceRuntimeId = null)
        : base(siteRuntimeId, localPlaceRuntimeId, observedDay, receivedDay, source, sourceRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(itemDefinitionId) == true || observedAmount < 0)
        {
            throw new ArgumentException("Common-resource intel requires a definition ID and non-negative amount.");
        }

        this.itemDefinitionId = itemDefinitionId;
        this.observedAmount = observedAmount;
        this.resourceCategory = string.IsNullOrWhiteSpace(resourceCategory) == true ? null : resourceCategory;
    }

    internal override bool HasSameIdentity(AdventureSiteIntelObservation other)
    {
        return other is AdventureCommonResourceObservation resource
            && HasSameScope(resource)
            && string.Equals(itemDefinitionId, resource.ItemDefinitionId, StringComparison.Ordinal);
    }
}

[Serializable]
public sealed class AdventureAccessObservation : AdventureSiteIntelObservation
{
    private readonly PlaceAccessState observedAccessState;

    public PlaceAccessState ObservedAccessState => observedAccessState;

    public AdventureAccessObservation(
        string siteRuntimeId,
        string localPlaceRuntimeId,
        PlaceAccessState observedAccessState,
        long observedDay,
        long receivedDay,
        AdventureIntelSource source,
        string sourceRuntimeId = null)
        : base(siteRuntimeId, localPlaceRuntimeId, observedDay, receivedDay, source, sourceRuntimeId)
    {
        if (Enum.IsDefined(typeof(PlaceAccessState), observedAccessState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(observedAccessState));
        }

        this.observedAccessState = observedAccessState;
    }

    internal override bool HasSameIdentity(AdventureSiteIntelObservation other)
    {
        return other is AdventureAccessObservation access && HasSameScope(access);
    }
}

[Serializable]
public sealed class AdventureSiteIntelKnowledgeRuntime
{
    private readonly string ownerRuntimeId;
    private readonly List<AdventureOppositionObservation> oppositionObservations = new List<AdventureOppositionObservation>();
    private readonly List<AdventureNotableItemObservation> notableItemObservations = new List<AdventureNotableItemObservation>();
    private readonly List<AdventureCommonResourceObservation> commonResourceObservations = new List<AdventureCommonResourceObservation>();
    private readonly List<AdventureAccessObservation> accessObservations = new List<AdventureAccessObservation>();

    public string OwnerRuntimeId => ownerRuntimeId;
    public IReadOnlyList<AdventureOppositionObservation> OppositionObservations => oppositionObservations.AsReadOnly();
    public IReadOnlyList<AdventureNotableItemObservation> NotableItemObservations => notableItemObservations.AsReadOnly();
    public IReadOnlyList<AdventureCommonResourceObservation> CommonResourceObservations => commonResourceObservations.AsReadOnly();
    public IReadOnlyList<AdventureAccessObservation> AccessObservations => accessObservations.AsReadOnly();

    public AdventureSiteIntelKnowledgeRuntime(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Adventure intel requires an owner RuntimeId.", nameof(ownerRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
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
        foreach (AdventureOppositionObservation candidate in oppositionObservations)
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
        foreach (AdventureNotableItemObservation observation in notableItemObservations)
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
        foreach (AdventureCommonResourceObservation observation in commonResourceObservations)
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

    private static bool Record<T>(List<T> observations, T incoming) where T : AdventureSiteIntelObservation
    {
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

            observations[i] = incoming;
            return true;
        }

        observations.Add(incoming);
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
