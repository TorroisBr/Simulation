using System;
using System.Collections.Generic;
using UnityEngine;

public enum CommercialKnowledgeSource
{
    DirectObservation,
    InitialScenarioKnowledge,
    SharedByNpc
}

[Serializable]
public sealed class CommercialMarketObservation
{
    [SerializeField] private string locationRuntimeId;
    [SerializeField] private string itemDefinitionId;
    [NonSerialized] private ItemData itemDefinition;
    [SerializeField] private float observedPrice;
    [SerializeField] private int observedStock;
    [SerializeField] private long observedDay;
    [SerializeField] private long receivedDay;
    [SerializeField] private CommercialKnowledgeSource source;
    [SerializeField] private string sourceRuntimeId;

    public string LocationRuntimeId => locationRuntimeId;
    public string ItemDefinitionId => itemDefinitionId;
    public ItemData ItemDefinition => itemDefinition;
    public float ObservedPrice => observedPrice;
    public int ObservedStock => observedStock;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public CommercialKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    public CommercialMarketObservation(
        string locationRuntimeId,
        ItemData itemDefinition,
        float observedPrice,
        int observedStock,
        long observedDay,
        long receivedDay,
        CommercialKnowledgeSource source)
        : this(
            locationRuntimeId,
            itemDefinition,
            observedPrice,
            observedStock,
            observedDay,
            receivedDay,
            source,
            null)
    {
    }

    public CommercialMarketObservation(
        string locationRuntimeId,
        ItemData itemDefinition,
        float observedPrice,
        int observedStock,
        long observedDay,
        long receivedDay,
        CommercialKnowledgeSource source,
        string sourceRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true)
        {
            throw new ArgumentException("A commercial observation requires a LocationRuntimeId.", nameof(locationRuntimeId));
        }

        if (itemDefinition == null || string.IsNullOrWhiteSpace(itemDefinition.DefinitionId) == true)
        {
            throw new ArgumentException("A commercial observation requires an item with a DefinitionId.", nameof(itemDefinition));
        }

        if (observedDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay), "ObservedDay cannot be negative.");
        }

        if (receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay), "ReceivedDay cannot be earlier than ObservedDay.");
        }

        if (source == CommercialKnowledgeSource.SharedByNpc
            && string.IsNullOrWhiteSpace(sourceRuntimeId) == true)
        {
            throw new ArgumentException("Shared commercial knowledge requires the source NPC RuntimeId.", nameof(sourceRuntimeId));
        }

        this.locationRuntimeId = locationRuntimeId;
        itemDefinitionId = itemDefinition.DefinitionId;
        this.itemDefinition = itemDefinition;
        this.observedPrice = Mathf.Max(0f, observedPrice);
        this.observedStock = Mathf.Max(0, observedStock);
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
        this.sourceRuntimeId = source == CommercialKnowledgeSource.SharedByNpc ? sourceRuntimeId : null;
    }
}

[Serializable]
public sealed class CommercialKnowledgeRuntime
{
    [SerializeField] private List<CommercialMarketObservation> observations = new List<CommercialMarketObservation>();

    public IReadOnlyList<CommercialMarketObservation> Observations => ObservationList;

    private List<CommercialMarketObservation> ObservationList => observations ?? (observations = new List<CommercialMarketObservation>());

    public bool RecordObservation(CommercialMarketObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.LocationRuntimeId, observation.ItemDefinitionId);

        if (existingIndex < 0)
        {
            ObservationList.Add(observation);
            return true;
        }

        CommercialMarketObservation existing = ObservationList[existingIndex];

        if (ShouldReplace(existing, observation) == false)
        {
            return false;
        }

        ObservationList[existingIndex] = observation;
        return true;
    }

    public bool CanImproveWith(CommercialMarketObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.LocationRuntimeId, observation.ItemDefinitionId);
        return existingIndex < 0 || ShouldReplace(ObservationList[existingIndex], observation);
    }

    public bool TryGetObservation(string locationRuntimeId, string itemDefinitionId, out CommercialMarketObservation observation)
    {
        int index = FindObservationIndex(locationRuntimeId, itemDefinitionId);

        if (index >= 0)
        {
            observation = ObservationList[index];
            return true;
        }

        observation = null;
        return false;
    }

    private int FindObservationIndex(string locationRuntimeId, string itemDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true || string.IsNullOrWhiteSpace(itemDefinitionId) == true)
        {
            return -1;
        }

        for (int i = 0; i < ObservationList.Count; i++)
        {
            CommercialMarketObservation candidate = ObservationList[i];

            if (candidate != null
                && string.Equals(candidate.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(candidate.ItemDefinitionId, itemDefinitionId, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ShouldReplace(CommercialMarketObservation existing, CommercialMarketObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        if (incoming.ObservedDay != existing.ObservedDay)
        {
            return incoming.ObservedDay > existing.ObservedDay;
        }

        int incomingPriority = GetSourcePriority(incoming.Source);
        int existingPriority = GetSourcePriority(existing.Source);

        // Same observation day and source priority preserves the existing snapshot.
        return incomingPriority > existingPriority;
    }

    public static int GetSourcePriority(CommercialKnowledgeSource source)
    {
        switch (source)
        {
            case CommercialKnowledgeSource.DirectObservation:
                return 3;
            case CommercialKnowledgeSource.SharedByNpc:
                return 2;
            case CommercialKnowledgeSource.InitialScenarioKnowledge:
                return 1;
            default:
                return 0;
        }
    }
}

[Serializable]
public sealed class CommercialKnowledgeSettings
{
    public int freshForDays = 7;
    public int maxUsefulAgeDays = 30;
    public int maxSharedObservationsPerInteraction = 2;
}

public sealed class CommercialKnowledgePolicy
{
    private const int DefaultFreshForDays = 7;
    private const int DefaultMaxUsefulAgeDays = 30;

    private readonly int freshForDays;
    private readonly int maxUsefulAgeDays;

    public int FreshForDays => freshForDays;
    public int MaxUsefulAgeDays => maxUsefulAgeDays;

    public CommercialKnowledgePolicy(CommercialKnowledgeSettings settings)
    {
        int configuredFreshDays = settings != null ? settings.freshForDays : DefaultFreshForDays;
        int configuredMaxAge = settings != null ? settings.maxUsefulAgeDays : DefaultMaxUsefulAgeDays;

        freshForDays = Math.Min(Math.Max(0, configuredFreshDays), int.MaxValue - 1);

        if (configuredMaxAge <= 0)
        {
            configuredMaxAge = DefaultMaxUsefulAgeDays;
        }

        maxUsefulAgeDays = configuredMaxAge > freshForDays
            ? configuredMaxAge
            : freshForDays + 1;
    }

    public long GetAgeDays(CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        if (observation == null || currentAbsoluteDay <= observation.ObservedDay)
        {
            return 0L;
        }

        return currentAbsoluteDay - observation.ObservedDay;
    }

    public float GetFreshness(CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        if (observation == null)
        {
            return 0f;
        }

        long ageDays = GetAgeDays(observation, Math.Max(0L, currentAbsoluteDay));

        if (ageDays <= freshForDays)
        {
            return 1f;
        }

        if (ageDays >= maxUsefulAgeDays)
        {
            return 0f;
        }

        float decayRange = maxUsefulAgeDays - freshForDays;
        return 1f - (ageDays - freshForDays) / decayRange;
    }
}
