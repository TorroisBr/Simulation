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
}
