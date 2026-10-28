using System;
using System.Collections.Generic;

/// <summary>Core-owned selected NPC observations about explorable sites.</summary>
internal sealed class DailyV1ExplorableSiteKnowledgeOwner
{
    private readonly List<ExplorableSiteKnowledgeObservation> observations =
        new List<ExplorableSiteKnowledgeObservation>();
    private readonly IReadOnlyList<ExplorableSiteKnowledgeObservation> readOnlyObservations;

    internal string OwnerRuntimeId { get; }
    internal IReadOnlyList<ExplorableSiteKnowledgeObservation> Observations => readOnlyObservations;
    internal long Revision { get; private set; }

    internal DailyV1ExplorableSiteKnowledgeOwner(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId))
            throw new ArgumentException("Explorable site knowledge requires an owner RuntimeId.", nameof(ownerRuntimeId));
        OwnerRuntimeId = ownerRuntimeId;
        readOnlyObservations = observations.AsReadOnly();
    }

    internal bool CanCreateInitialSnapshot(
        IReadOnlyList<ExplorableSiteKnowledgeObservation> values,
        long exactRevision) =>
        values != null && Revision == 0L && observations.Count == 0
        && TryValidateSnapshot(values, exactRevision);

    internal bool TryCreateInitialSnapshot(
        IReadOnlyList<ExplorableSiteKnowledgeObservation> values,
        long exactRevision)
    {
        if (!CanCreateInitialSnapshot(values, exactRevision)) return false;
        foreach (ExplorableSiteKnowledgeObservation value in values)
            observations.Add(Copy(value));
        Revision = exactRevision;
        return true;
    }

    internal bool TryCopyOwnerSnapshot(
        out IReadOnlyList<ExplorableSiteKnowledgeObservation> values,
        out long exactRevision)
    {
        List<ExplorableSiteKnowledgeObservation> copy = new List<ExplorableSiteKnowledgeObservation>(observations.Count);
        foreach (ExplorableSiteKnowledgeObservation value in observations)
            copy.Add(Copy(value));
        values = copy.AsReadOnly();
        exactRevision = Revision;
        return true;
    }

    internal bool MatchesSnapshot(
        IReadOnlyList<ExplorableSiteKnowledgeObservation> values,
        long exactRevision)
    {
        if (values == null || Revision != exactRevision || observations.Count != values.Count) return false;
        for (int index = 0; index < observations.Count; index++)
        {
            ExplorableSiteKnowledgeObservation left = observations[index];
            ExplorableSiteKnowledgeObservation right = values[index];
            if (right == null || left.SiteRuntimeId != right.SiteRuntimeId
                || left.LocationRuntimeId != right.LocationRuntimeId
                || left.ObservedDay != right.ObservedDay || left.ReceivedDay != right.ReceivedDay
                || left.Source != right.Source) return false;
        }
        return true;
    }

    internal bool KnowsSite(string siteRuntimeId) => TryGetObservation(siteRuntimeId, out _);

    internal bool TryGetObservation(string siteRuntimeId, out ExplorableSiteKnowledgeObservation observation)
    {
        for (int index = 0; index < observations.Count; index++)
        {
            ExplorableSiteKnowledgeObservation candidate = observations[index];
            if (candidate != null && string.Equals(candidate.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal))
            {
                observation = candidate;
                return true;
            }
        }
        observation = null;
        return false;
    }

    internal bool RecordObservation(ExplorableSiteKnowledgeObservation observation)
    {
        if (observation == null) return false;
        int existingIndex = FindObservationIndex(observation.SiteRuntimeId);
        if (existingIndex < 0)
        {
            if (Revision == long.MaxValue) return false;
            observations.Add(observation);
            Revision++;
            return true;
        }
        if (!ShouldReplace(observations[existingIndex], observation) || Revision == long.MaxValue) return false;
        observations[existingIndex] = observation;
        Revision++;
        return true;
    }

    private int FindObservationIndex(string siteRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(siteRuntimeId)) return -1;
        for (int index = 0; index < observations.Count; index++)
            if (observations[index] != null
                && string.Equals(observations[index].SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal))
                return index;
        return -1;
    }

    private static bool ShouldReplace(
        ExplorableSiteKnowledgeObservation existing,
        ExplorableSiteKnowledgeObservation incoming)
    {
        if (existing == null || incoming.ObservedDay != existing.ObservedDay)
            return existing == null || incoming.ObservedDay > existing.ObservedDay;
        return GetSourcePriority(incoming.Source) > GetSourcePriority(existing.Source);
    }

    private static int GetSourcePriority(ExplorableSiteKnowledgeSource source) =>
        source == ExplorableSiteKnowledgeSource.DirectObservation ? 2 : 1;

    private static bool TryValidateSnapshot(
        IReadOnlyList<ExplorableSiteKnowledgeObservation> values,
        long exactRevision)
    {
        if (exactRevision < 0L) return false;
        HashSet<string> uniqueSites = new HashSet<string>(StringComparer.Ordinal);
        foreach (ExplorableSiteKnowledgeObservation value in values)
            if (value == null || !uniqueSites.Add(value.SiteRuntimeId)) return false;
        return true;
    }

    private static ExplorableSiteKnowledgeObservation Copy(ExplorableSiteKnowledgeObservation value) =>
        new ExplorableSiteKnowledgeObservation(value.SiteRuntimeId, value.LocationRuntimeId,
            value.ObservedDay, value.ReceivedDay, value.Source);
}
