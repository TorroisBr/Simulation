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

[Serializable]
public sealed class ExplorableSiteKnowledgeRuntime
{
    private readonly string ownerRuntimeId;
    private readonly List<ExplorableSiteKnowledgeObservation> observations =
        new List<ExplorableSiteKnowledgeObservation>();
    private readonly IReadOnlyList<ExplorableSiteKnowledgeObservation> readOnlyObservations;

    public string OwnerRuntimeId => ownerRuntimeId;
    public IReadOnlyList<ExplorableSiteKnowledgeObservation> Observations => readOnlyObservations;

    public ExplorableSiteKnowledgeRuntime(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Explorable site knowledge requires an owner RuntimeId.", nameof(ownerRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
        readOnlyObservations = observations.AsReadOnly();
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
            observations.Add(observation);
            return true;
        }

        if (ShouldReplace(observations[existingIndex], observation) == false)
        {
            return false;
        }

        observations[existingIndex] = observation;
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

public sealed class ExplorableSiteKnowledgeSystem
{
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
    // NpcRuntime remains city-centric until the later generalized travel phase.
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
}
