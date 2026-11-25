using System;
using System.Collections.Generic;

public enum ExplorableSiteKnowledgeSource
{
    InitialScenarioKnowledge,
    DirectObservation
}

[Serializable]
public sealed class ExplorableSiteKnowledgeObservation
{
    private readonly string siteRuntimeId;
    private readonly string locationRuntimeId;
    private readonly long observedDay;
    private readonly long receivedDay;
    private readonly ExplorableSiteKnowledgeSource source;

    public string SiteRuntimeId => siteRuntimeId;
    public string LocationRuntimeId => locationRuntimeId;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public ExplorableSiteKnowledgeSource Source => source;

    public ExplorableSiteKnowledgeObservation(
        string siteRuntimeId,
        string locationRuntimeId,
        long observedDay,
        long receivedDay,
        ExplorableSiteKnowledgeSource source)
    {
        if (string.IsNullOrWhiteSpace(siteRuntimeId) == true)
        {
            throw new ArgumentException("Explorable site knowledge requires a non-empty SiteRuntimeId.", nameof(siteRuntimeId));
        }

        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true)
        {
            throw new ArgumentException("Explorable site knowledge requires a non-empty LocationRuntimeId.", nameof(locationRuntimeId));
        }

        if (observedDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay));
        }

        if (receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay));
        }

        if (Enum.IsDefined(typeof(ExplorableSiteKnowledgeSource), source) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        this.siteRuntimeId = siteRuntimeId;
        this.locationRuntimeId = locationRuntimeId;
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
    }
}
