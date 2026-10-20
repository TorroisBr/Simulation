using System;

/// <summary>Selected Daily-v1 crime-provider preparation rules over admitted action identity and owner facts.</summary>
internal static class DailyV1CrimeActionPlanning
{
    internal const float EscapeSuccessMultiplierPerFailure = 0.8f;

    /// <summary>Plans selected Hide from the admitted NPC, city, status and Justice owners.</summary>
    internal static bool TryPrepareSelectedHide(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.HideActionTypeValue
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, candidate.Action))
            return false;

        candidate.PreparedPayload = null;
        DailyV1JusticeRecordsOwner justice = graph.JusticeRecordsOwner;
        DailyV1NpcTravelOwner travel = actor.Travel;
        if (justice == null || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || travel == null || travel.IsTraveling
            || actor.HiddenState == null || actor.HiddenState.RemainingDays > 0
            || !string.IsNullOrWhiteSpace(actor.TravelPlan?.TargetLocationRuntimeId)
            || string.IsNullOrWhiteSpace(travel.CurrentCityRuntimeId)
            || !TryGetExactCity(graph, travel.CurrentCityRuntimeId, out _))
        {
            candidate.Utility = 0f;
            return true;
        }

        DailyV1EffectiveDefinitions definitions = graph.Definitions;
        string arrestedStatusId = definitions.Configuration?.arrestedStatus;
        if (!string.IsNullOrWhiteSpace(arrestedStatusId))
        {
            if (!definitions.Statuses.TryGetValue(arrestedStatusId, out DailyV1StatusDefinition arrestedStatus))
                return false;
            if (actor.Status?.Contains(arrestedStatus) == true)
            {
                candidate.Utility = 0f;
                return true;
            }
        }

        DailyV1JusticeWantedRecordOwner warrant = justice.FindActiveWantedRecord(
            actor.RuntimeId, travel.CurrentCityRuntimeId);
        if (warrant == null)
        {
            candidate.Utility = 0f;
            return true;
        }

        candidate.Utility = UnityMathfCompatibility.Max(
            candidate.Utility, 20f + warrant.Bounty * 0.15f);
        return true;
    }

    /// <summary>Plans selected EscapePrison and installs the exact failure multiplier into its owner payload.</summary>
    internal static bool TryPrepareSelectedEscapePrison(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, candidate.Action))
            return false;

        candidate.PreparedPayload = null;
        string arrestedStatusId = graph.Definitions.Configuration?.arrestedStatus;
        if (string.IsNullOrWhiteSpace(arrestedStatusId)
            || !graph.Definitions.Statuses.TryGetValue(arrestedStatusId,
                out DailyV1StatusDefinition arrestedStatus))
            return false;

        DailyV1JusticeSentenceOwner sentence = graph.JusticeRecordsOwner?.FindActiveSentence(actor.RuntimeId);
        bool isArrested = actor.Status?.Contains(arrestedStatus) == true;
        bool wasArrestedToday = sentence?.WasArrestedToday == true;
        int remainingDays = sentence?.RemainingDays ?? 0;
        if (!TryPlanEscapePrison(graph.Definitions, actor.Definition, candidate.Action,
                isArrested, wasArrestedToday, remainingDays,
                out bool canAttemptEscape, out float utility))
            return false;

        if (!canAttemptEscape)
        {
            candidate.Utility = 0f;
            return true;
        }

        float multiplier = GetEscapeSuccessChanceMultiplier(sentence?.FailedEscapeAttempts ?? 0);
        if (!DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, candidate.Action, null, null, null, 0, 0f,
                multiplier, NpcTravelReason.None, 0f, null, null, null, null,
                out DailyV1NpcActionPayloadOwner payload))
            return false;

        candidate.Utility = utility;
        candidate.PreparedPayload = payload;
        return true;
    }

    internal static bool TryPlanEscapePrison(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition actor,
        DailyV1ActionDefinition action,
        bool isArrested,
        bool wasArrestedToday,
        int remainingSentenceDays,
        out bool canAttemptEscape,
        out float utility)
    {
        canAttemptEscape = false;
        utility = 0f;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, actor)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(definitions, action))
            return false;

        if (!isArrested || wasArrestedToday || remainingSentenceDays <= 1)
            return true;

        float remainingSentenceUrgency = UnityMathfCompatibility.Clamp01(
            (remainingSentenceDays - 1f) / 7f);
        utility = UnityMathfCompatibility.Clamp(
            10f + remainingSentenceUrgency * 20f,
            10f,
            30f);
        canAttemptEscape = true;
        return true;
    }

    internal static float GetEscapeSuccessChanceMultiplier(int failedEscapeAttempts)
    {
        return UnityMathfCompatibility.Pow(
            EscapeSuccessMultiplierPerFailure,
            failedEscapeAttempts);
    }

    private static bool TryGetExactCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (candidate != null
                && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (city != null || !graph.ContainsExactCityOwner(candidate)) return false;
                city = candidate;
            }
        return city != null;
    }
}
