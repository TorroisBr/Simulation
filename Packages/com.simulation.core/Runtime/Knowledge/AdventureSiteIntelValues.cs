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
