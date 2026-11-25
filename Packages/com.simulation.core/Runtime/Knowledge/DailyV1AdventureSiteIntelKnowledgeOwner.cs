using System;
using System.Collections.Generic;

/// <summary>Core-owned selected NPC adventure intelligence observations.</summary>
internal sealed class DailyV1AdventureSiteIntelKnowledgeOwner
{
    private readonly List<AdventureOppositionObservation> opposition = new List<AdventureOppositionObservation>();
    private readonly List<AdventureNotableItemObservation> notableItems = new List<AdventureNotableItemObservation>();
    private readonly List<AdventureCommonResourceObservation> resources = new List<AdventureCommonResourceObservation>();
    private readonly List<AdventureAccessObservation> access = new List<AdventureAccessObservation>();
    private readonly IReadOnlyList<AdventureOppositionObservation> readOnlyOpposition;
    private readonly IReadOnlyList<AdventureNotableItemObservation> readOnlyNotableItems;
    private readonly IReadOnlyList<AdventureCommonResourceObservation> readOnlyResources;
    private readonly IReadOnlyList<AdventureAccessObservation> readOnlyAccess;

    internal string OwnerRuntimeId { get; }
    internal long Revision { get; private set; }
    internal IReadOnlyList<AdventureOppositionObservation> OppositionObservations => readOnlyOpposition;
    internal IReadOnlyList<AdventureNotableItemObservation> NotableItemObservations => readOnlyNotableItems;
    internal IReadOnlyList<AdventureCommonResourceObservation> CommonResourceObservations => readOnlyResources;
    internal IReadOnlyList<AdventureAccessObservation> AccessObservations => readOnlyAccess;

    internal DailyV1AdventureSiteIntelKnowledgeOwner(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId))
            throw new ArgumentException("Adventure intel requires an owner RuntimeId.", nameof(ownerRuntimeId));
        OwnerRuntimeId = ownerRuntimeId;
        readOnlyOpposition = opposition.AsReadOnly();
        readOnlyNotableItems = notableItems.AsReadOnly();
        readOnlyResources = resources.AsReadOnly();
        readOnlyAccess = access.AsReadOnly();
    }

    internal bool CanCreateInitialSnapshot(
        IReadOnlyList<AdventureOppositionObservation> oppositionValues,
        IReadOnlyList<AdventureNotableItemObservation> notableValues,
        IReadOnlyList<AdventureCommonResourceObservation> resourceValues,
        IReadOnlyList<AdventureAccessObservation> accessValues,
        long exactRevision) =>
        oppositionValues != null && notableValues != null && resourceValues != null && accessValues != null
        && Revision == 0L && opposition.Count == 0 && notableItems.Count == 0
        && resources.Count == 0 && access.Count == 0
        && TryValidateUnique(oppositionValues) && TryValidateUnique(notableValues)
        && TryValidateUnique(resourceValues) && TryValidateUnique(accessValues)
        && exactRevision >= 0L;

    internal bool TryCreateInitialSnapshot(
        IReadOnlyList<AdventureOppositionObservation> oppositionValues,
        IReadOnlyList<AdventureNotableItemObservation> notableValues,
        IReadOnlyList<AdventureCommonResourceObservation> resourceValues,
        IReadOnlyList<AdventureAccessObservation> accessValues,
        long exactRevision)
    {
        if (!CanCreateInitialSnapshot(oppositionValues, notableValues, resourceValues, accessValues, exactRevision))
            return false;
        opposition.AddRange(oppositionValues);
        notableItems.AddRange(notableValues);
        resources.AddRange(resourceValues);
        access.AddRange(accessValues);
        Revision = exactRevision;
        return true;
    }

    internal bool TryCopyOwnerSnapshot(
        out IReadOnlyList<AdventureOppositionObservation> oppositionValues,
        out IReadOnlyList<AdventureNotableItemObservation> notableValues,
        out IReadOnlyList<AdventureCommonResourceObservation> resourceValues,
        out IReadOnlyList<AdventureAccessObservation> accessValues,
        out long exactRevision)
    {
        oppositionValues = new List<AdventureOppositionObservation>(opposition).AsReadOnly();
        notableValues = new List<AdventureNotableItemObservation>(notableItems).AsReadOnly();
        resourceValues = new List<AdventureCommonResourceObservation>(resources).AsReadOnly();
        accessValues = new List<AdventureAccessObservation>(access).AsReadOnly();
        exactRevision = Revision;
        return true;
    }

    internal bool MatchesSnapshot(
        IReadOnlyList<AdventureOppositionObservation> oppositionValues,
        IReadOnlyList<AdventureNotableItemObservation> notableValues,
        IReadOnlyList<AdventureCommonResourceObservation> resourceValues,
        IReadOnlyList<AdventureAccessObservation> accessValues,
        long exactRevision) => exactRevision == Revision
            && SameSequence(opposition, oppositionValues)
            && SameSequence(notableItems, notableValues)
            && SameSequence(resources, resourceValues)
            && SameSequence(access, accessValues);

    internal bool RecordObservation(AdventureOppositionObservation value) => Record(opposition, value);
    internal bool RecordObservation(AdventureNotableItemObservation value) => Record(notableItems, value);
    internal bool RecordObservation(AdventureCommonResourceObservation value) => Record(resources, value);
    internal bool RecordObservation(AdventureAccessObservation value) => Record(access, value);

    internal bool KnowsOpposition(string siteRuntimeId, string oppositionRuntimeId) =>
        TryGetOpposition(siteRuntimeId, oppositionRuntimeId, out AdventureOppositionObservation observation)
        && observation.ObservedState == AdventureOppositionObservedState.Active;

    internal bool TryGetOpposition(string siteRuntimeId, string oppositionRuntimeId,
        out AdventureOppositionObservation observation)
    {
        foreach (AdventureOppositionObservation candidate in opposition)
            if (candidate != null && string.Equals(candidate.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal)
                && string.Equals(candidate.OppositionRuntimeId, oppositionRuntimeId, StringComparison.Ordinal))
            { observation = candidate; return true; }
        observation = null;
        return false;
    }

    internal bool KnowsNotableItem(string siteRuntimeId, string notableItemRuntimeId)
    {
        foreach (AdventureNotableItemObservation value in notableItems)
            if (value != null && string.Equals(value.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal)
                && string.Equals(value.NotableItemRuntimeId, notableItemRuntimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    internal bool KnowsCommonResource(string siteRuntimeId, string itemDefinitionId)
    {
        foreach (AdventureCommonResourceObservation value in resources)
            if (value != null && value.ObservedAmount > 0
                && string.Equals(value.SiteRuntimeId, siteRuntimeId, StringComparison.Ordinal)
                && string.Equals(value.ItemDefinitionId, itemDefinitionId, StringComparison.Ordinal)) return true;
        return false;
    }

    private bool Record<T>(List<T> destination, T incoming) where T : AdventureSiteIntelObservation
    {
        if (incoming == null) return false;
        for (int index = 0; index < destination.Count; index++)
        {
            T existing = destination[index];
            if (existing == null || !existing.HasSameIdentity(incoming)) continue;
            if (!ShouldReplace(existing, incoming) || Revision == long.MaxValue) return false;
            destination[index] = incoming;
            Revision++;
            return true;
        }
        if (Revision == long.MaxValue) return false;
        destination.Add(incoming);
        Revision++;
        return true;
    }

    private static bool ShouldReplace(AdventureSiteIntelObservation existing, AdventureSiteIntelObservation incoming)
    {
        if (incoming.ObservedDay != existing.ObservedDay) return incoming.ObservedDay > existing.ObservedDay;
        int incomingPriority = GetSourcePriority(incoming.Source);
        int existingPriority = GetSourcePriority(existing.Source);
        return incomingPriority > existingPriority
            || (incomingPriority == existingPriority && incoming.ReceivedDay > existing.ReceivedDay);
    }

    private static int GetSourcePriority(AdventureIntelSource source) => source switch
    {
        AdventureIntelSource.DirectObservation => 3,
        AdventureIntelSource.SharedByNpc => 2,
        _ => 1
    };

    private static bool TryValidateUnique<T>(IReadOnlyList<T> values) where T : AdventureSiteIntelObservation
    {
        for (int index = 0; index < values.Count; index++)
        {
            T value = values[index];
            if (value == null) return false;
            for (int prior = 0; prior < index; prior++)
                if (values[prior].HasSameIdentity(value)) return false;
        }
        return true;
    }

    private static bool SameSequence<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        where T : AdventureSiteIntelObservation
    {
        if (right == null || left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
            if (!SameObservation(left[index], right[index])) return false;
        return true;
    }

    private static bool SameObservation(
        AdventureSiteIntelObservation left,
        AdventureSiteIntelObservation right)
    {
        if (left == null || right == null || !left.HasSameIdentity(right)
            || !string.Equals(left.LocalPlaceRuntimeId, right.LocalPlaceRuntimeId, StringComparison.Ordinal)
            || left.ObservedDay != right.ObservedDay || left.ReceivedDay != right.ReceivedDay
            || left.Source != right.Source
            || !string.Equals(left.SourceRuntimeId, right.SourceRuntimeId, StringComparison.Ordinal))
            return false;

        if (left is AdventureOppositionObservation leftOpposition
            && right is AdventureOppositionObservation rightOpposition)
            return string.Equals(leftOpposition.OppositionRuntimeId, rightOpposition.OppositionRuntimeId, StringComparison.Ordinal)
                && leftOpposition.ObservedState == rightOpposition.ObservedState;
        if (left is AdventureNotableItemObservation leftNotable
            && right is AdventureNotableItemObservation rightNotable)
            return string.Equals(leftNotable.NotableItemRuntimeId, rightNotable.NotableItemRuntimeId, StringComparison.Ordinal)
                && string.Equals(leftNotable.ItemDefinitionId, rightNotable.ItemDefinitionId, StringComparison.Ordinal);
        if (left is AdventureCommonResourceObservation leftResource
            && right is AdventureCommonResourceObservation rightResource)
            return string.Equals(leftResource.ItemDefinitionId, rightResource.ItemDefinitionId, StringComparison.Ordinal)
                && leftResource.ObservedAmount == rightResource.ObservedAmount
                && string.Equals(leftResource.ResourceCategory, rightResource.ResourceCategory, StringComparison.Ordinal);
        if (left is AdventureAccessObservation leftAccess
            && right is AdventureAccessObservation rightAccess)
            return leftAccess.ObservedAccessState == rightAccess.ObservedAccessState;
        return false;
    }
}
