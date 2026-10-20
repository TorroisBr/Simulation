using System;
using System.Collections.Generic;

/// <summary>A read-only candidate projection in the existing City roster order.</summary>
internal sealed class DailyV1GuardArrestCandidate
{
    internal string RuntimeId { get; }
    internal float Bounty { get; }

    internal DailyV1GuardArrestCandidate(string runtimeId, float bounty)
    {
        RuntimeId = runtimeId;
        Bounty = bounty;
    }
}

/// <summary>Selected-profile arrest target selection shared by Core runtime and Unity projection.</summary>
internal static class DailyV1GuardActionPlanning
{
    /// <summary>
    /// Applies the existing selected GuardSystem target ordering and utility rule
    /// to candidates already admitted by the authoritative Justice/roster reads.
    /// Candidate order is retained; the returned index identifies that exact row.
    /// </summary>
    internal static bool TryPlanArrest(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition guard,
        DailyV1ActionDefinition action,
        IReadOnlyList<DailyV1GuardArrestCandidate> candidates,
        float currentUtility,
        out int selectedCandidateIndex,
        out float resultingUtility)
    {
        selectedCandidateIndex = -1;
        resultingUtility = 0f;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, guard)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(definitions, action)
            || candidates == null)
            return false;

        float bestBounty = 0f;
        string bestRuntimeId = null;
        for (int index = 0; index < candidates.Count; index++)
        {
            DailyV1GuardArrestCandidate candidate = candidates[index];
            if (candidate == null)
                continue;

            if (selectedCandidateIndex < 0
                || candidate.Bounty > bestBounty
                || (UnityMathfCompatibility.Approximately(candidate.Bounty, bestBounty)
                    && string.CompareOrdinal(candidate.RuntimeId, bestRuntimeId) < 0))
            {
                selectedCandidateIndex = index;
                bestBounty = candidate.Bounty;
                bestRuntimeId = candidate.RuntimeId;
            }
        }

        if (selectedCandidateIndex < 0)
            return true;

        resultingUtility = UnityMathfCompatibility.Max(currentUtility, 55f + bestBounty * 0.25f);
        return true;
    }

    /// <summary>
    /// Plans the selected Guard arrest against the exact City, NPC and Justice
    /// owners already composed by this runtime graph. The returned owner is
    /// the same instance that supplied the ordered presence and warrant facts.
    /// </summary>
    internal static bool TryPlanArrest(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners guardOwner,
        DailyV1ActionDefinition action,
        float currentUtility,
        out DailyV1NpcRuntimeOwners targetOwner,
        out float resultingUtility)
    {
        targetOwner = null;
        resultingUtility = 0f;
        if (graph == null || guardOwner == null || action == null
            || graph.JusticeRecordsOwner == null
            || !graph.ContainsExactNpcOwner(guardOwner)
            || !ReferenceEquals(guardOwner.Definitions, graph.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedNpc(graph.Definitions, guardOwner.Definition)
            || guardOwner.Lifecycle?.LifeState != NpcLifeState.Alive
            || !DailyV1NpcDefinitionRules.HasJobType(
                guardOwner.Definition, graph.Definitions, DailyV1NpcDefinitionRules.GuardJobTypeValue)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || DailyV1NpcDefinitionRules.GetActionTypeOrder(graph.Definitions, action)
                != DailyV1NpcDefinitionRules.ArrestActionTypeValue
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, guardOwner.Status?.Statuses))
            return false;

        DailyV1SimulationConfigData configuration = graph.Definitions.Configuration;
        if (!TryGetConfiguredStatus(graph.Definitions, configuration?.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus)
            || !TryGetConfiguredStatus(graph.Definitions, configuration?.hiddenStatus,
                out DailyV1StatusDefinition hiddenStatus)
            || IsHidden(guardOwner, hiddenStatus)
            || guardOwner.Travel == null
            || guardOwner.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(guardOwner.Travel.CurrentCityRuntimeId))
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidateCity in graph.Cities)
            if (candidateCity != null
                && string.Equals(candidateCity.RuntimeId,
                    guardOwner.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            {
                city = candidateCity;
                break;
            }

        if (city == null || !graph.ContainsExactCityOwner(city)
            || city.NpcPresence == null
            || !string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !city.NpcPresence.Contains(guardOwner.RuntimeId))
            return false;

        List<DailyV1GuardArrestCandidate> candidates = new List<DailyV1GuardArrestCandidate>();
        List<DailyV1NpcRuntimeOwners> candidateOwners = new List<DailyV1NpcRuntimeOwners>();
        foreach (string npcRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
        {
            if (!graph.TryGetNpcOwner(npcRuntimeId, out DailyV1NpcRuntimeOwners candidateOwner)
                || candidateOwner == null
                || ReferenceEquals(candidateOwner, guardOwner)
                || string.Equals(candidateOwner.RuntimeId, guardOwner.RuntimeId, StringComparison.Ordinal)
                || candidateOwner.Lifecycle?.LifeState != NpcLifeState.Alive
                || candidateOwner.Travel == null
                || candidateOwner.Travel.IsTraveling
                || !string.Equals(candidateOwner.Travel.CurrentCityRuntimeId,
                    city.RuntimeId, StringComparison.Ordinal)
                || (arrestedStatus != null && candidateOwner.Status?.Contains(arrestedStatus) == true)
                || IsHidden(candidateOwner, hiddenStatus))
                continue;

            DailyV1JusticeWantedRecordOwner warrant = graph.JusticeRecordsOwner
                .FindActiveWantedRecord(candidateOwner.RuntimeId, city.RuntimeId);
            if (warrant == null) continue;

            candidates.Add(new DailyV1GuardArrestCandidate(candidateOwner.RuntimeId, warrant.Bounty));
            candidateOwners.Add(candidateOwner);
        }

        if (!TryPlanArrest(graph.Definitions, guardOwner.Definition, action, candidates,
                currentUtility, out int selectedIndex, out resultingUtility)
            || selectedIndex < 0 || selectedIndex >= candidateOwners.Count)
        {
            resultingUtility = 0f;
            return false;
        }

        targetOwner = candidateOwners[selectedIndex];
        return targetOwner != null;
    }

    private static bool TryGetConfiguredStatus(
        DailyV1EffectiveDefinitions definitions,
        string statusId,
        out DailyV1StatusDefinition status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(statusId)) return true;
        return definitions != null
            && definitions.Statuses.TryGetValue(statusId, out status)
            && status != null;
    }

    private static bool IsHidden(
        DailyV1NpcRuntimeOwners owner,
        DailyV1StatusDefinition hiddenStatus) =>
        owner == null
        || owner.HiddenState == null
        || owner.HiddenState.RemainingDays > 0
        || (hiddenStatus != null && owner.Status?.Contains(hiddenStatus) == true);
}
