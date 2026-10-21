using System;
using System.Collections.Generic;

/// <summary>Selected Daily-v1 crime-provider preparation rules over admitted action identity and owner facts.</summary>
internal static class DailyV1CrimeActionPlanning
{
    internal const float EscapeSuccessMultiplierPerFailure = 0.8f;

    /// <summary>
    /// Plans the selected autonomous Steal candidate from the exact admitted
    /// City/NPC owners. Person identity is resolved through the same canonical
    /// PersonStore used by the Daily-v1 actor-turn and social boundaries; an
    /// absent or mismatched identity fails closed instead of fabricating a
    /// P12 occurrence key.
    /// </summary>
    internal static bool TryPrepareSelectedSteal(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate,
        PersonStore personStore,
        long currentAbsoluteDay,
        IAuthoritativeRandomSource randomSource) =>
        TryPrepareSelectedSteal(
            graph, actor, candidate, personStore, currentAbsoluteDay,
            randomSource, null);

    internal static bool TryPrepareSelectedSteal(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate,
        PersonStore personStore,
        long currentAbsoluteDay,
        IAuthoritativeRandomSource randomSource,
        Func<DailyV1NpcRuntimeOwners, bool> canProjectTarget)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.StealActionTypeValue
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, candidate.Action)
            || randomSource == null)
            return false;

        candidate.PreparedPayload = null;
        if (!CanActInCity(graph, actor, out DailyV1CityRuntimeOwners city)
            || actor.TravelPlan == null
            || !string.IsNullOrWhiteSpace(actor.TravelPlan.TargetLocationRuntimeId)
            || !DailyV1NpcDefinitionRules.HasConfiguredDefaultAction(
                actor.Definition, graph.Definitions, candidate.Action)
            || !TryResolveMaterializedPerson(personStore, actor.RuntimeId, out PersonRuntime actorPerson))
        {
            candidate.Utility = 0f;
            return true;
        }

        List<DailyV1NpcRuntimeOwners> targets = new List<DailyV1NpcRuntimeOwners>();
        float totalWeight = 0f;
        foreach (string targetRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
        {
            if (!graph.TryGetNpcOwner(targetRuntimeId, out DailyV1NpcRuntimeOwners target)
                || !graph.ContainsExactNpcOwner(target))
                return false;
            if (!IsValidStealTarget(graph, actor, city, target)) continue;
            // In the selected Unity composition, target projection is a
            // compatibility preflight over the complete eligible set. A
            // missing exact target projection must disable Steal before its
            // legacy auxiliary RNG draw, matching provider-before-plan order.
            if (canProjectTarget != null && !canProjectTarget(target))
            {
                candidate.Utility = 0f;
                return true;
            }
            totalWeight += UnityMathfCompatibility.Max(1f, target.MoneyAccount.Balance);
            targets.Add(target);
        }

        if (totalWeight <= 0f)
        {
            candidate.Utility = 0f;
            return true;
        }

        targets.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        float randomValue = randomSource.NextUnit(
            "crime-steal|" + actor.RuntimeId + "|" + currentAbsoluteDay) * totalWeight;
        float currentWeight = 0f;
        DailyV1NpcRuntimeOwners selectedTarget = null;
        foreach (DailyV1NpcRuntimeOwners target in targets)
        {
            currentWeight += UnityMathfCompatibility.Max(1f, target.MoneyAccount.Balance);
            if (randomValue <= currentWeight)
            {
                selectedTarget = target;
                break;
            }
        }

        DailyV1CrimeActionSettings settings = candidate.Action.Values.crimeSettings;
        int amount = selectedTarget == null || settings == null
            ? 0
            : UnityMathfCompatibility.Min(
                UnityMathfCompatibility.Max(0, settings.amount),
                UnityMathfCompatibility.FloorToInt(selectedTarget.MoneyAccount.Balance));
        if (amount <= 0
            || !TryResolveMaterializedPerson(
                personStore, selectedTarget?.RuntimeId, out PersonRuntime targetPerson))
        {
            candidate.Utility = 0f;
            return true;
        }

        string occurrenceKey = "autonomous|day|"
            + currentAbsoluteDay.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "|perp|" + actorPerson.PersonId.Value
            + "|victim|" + targetPerson.PersonId.Value
            + "|amount|" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "|action|" + (candidate.Action.DefinitionId ?? string.Empty);
        if (!DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, candidate.Action,
                selectedTarget.RuntimeId, null, null, amount, 0f, 1f,
                NpcTravelReason.None, 0f, null, occurrenceKey, null, null,
                out DailyV1NpcActionPayloadOwner payload))
        {
            candidate.Utility = 0f;
            return true;
        }

        candidate.Utility = UnityMathfCompatibility.Max(0f, candidate.Utility)
            + UnityMathfCompatibility.Clamp(
                (50f - actor.MoneyAccount.Balance) * 0.35f, 0f, 18f)
            + UnityMathfCompatibility.Min(8f, amount * 0.2f);
        candidate.PreparedPayload = payload;
        return true;
    }

    /// <summary>Plans selected FleeCity from exact Justice, spatial, and NPC owner facts.</summary>
    internal static bool TryPrepareSelectedFleeCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value != DailyV1NpcDefinitionRules.FleeCityActionTypeValue
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, candidate.Action))
            return false;

        candidate.PreparedPayload = null;
        if (!CanActInCity(graph, actor, out DailyV1CityRuntimeOwners originCity)
            || actor.HiddenState == null || actor.HiddenState.RemainingDays > 0
            || actor.TravelPlan == null
            || !string.IsNullOrWhiteSpace(actor.TravelPlan.TargetLocationRuntimeId)
            || graph.SpatialNetwork == null
            || actor.SpatialKnowledge == null
            || !actor.SpatialKnowledge.KnowsLocation(originCity.LocationRuntimeId))
        {
            candidate.Utility = 0f;
            return true;
        }

        DailyV1JusticeWantedRecordOwner currentWarrant =
            graph.JusticeRecordsOwner.FindActiveWantedRecord(actor.RuntimeId, originCity.RuntimeId);
        if (currentWarrant == null)
        {
            candidate.Utility = 0f;
            return true;
        }

        float currentBounty = currentWarrant.Bounty;
        DailyV1CityRuntimeOwners bestDestination = null;
        float bestTravelCost = 0f;
        float bestUtility = 0f;
        if (!graph.SpatialNetwork.TryGetLocation(
                originCity.LocationRuntimeId, out SpatialLocationRuntime originLocation))
        {
            candidate.Utility = 0f;
            return true;
        }

        List<SpatialRouteRuntime> knownDirectRoutes = new List<SpatialRouteRuntime>();
        foreach (SpatialRouteRuntime route in graph.SpatialNetwork.GetOutgoingRoutes(originLocation))
        {
            if (route == null || route.Destination == null
                || !actor.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                || !actor.SpatialKnowledge.KnowsLocation(route.Destination.RuntimeId)
                || !graph.SpatialNetwork.TryGetSingleDirectRoute(
                    originLocation, route.Destination,
                    out SpatialRouteRuntime directRoute, out _)
                || !ReferenceEquals(directRoute, route))
                continue;
            knownDirectRoutes.Add(route);
        }
        knownDirectRoutes.Sort((left, right) => string.CompareOrdinal(
            left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));

        foreach (SpatialRouteRuntime route in knownDirectRoutes)
        {
            if (!TryGetExactCityAtLocation(
                    graph, route.Destination.RuntimeId,
                    out DailyV1CityRuntimeOwners destination)
                || ReferenceEquals(destination, originCity)
                || !graph.TryPlanSelectedSoloTravel(
                    actor, originCity.LocationRuntimeId, destination.LocationRuntimeId,
                    destination.RuntimeId, out SpatialRouteRuntime plannedRoute,
                    out float travelCost)
                || !ReferenceEquals(plannedRoute, route))
                continue;

            DailyV1JusticeWantedRecordOwner destinationWarrant =
                graph.JusticeRecordsOwner.FindActiveWantedRecord(actor.RuntimeId, destination.RuntimeId);
            float destinationBounty = destinationWarrant?.Bounty ?? 0f;
            float safetyGain = currentBounty - destinationBounty;
            if (actor.MoneyAccount == null || actor.MoneyAccount.Balance < travelCost
                || safetyGain <= 0f)
                continue;

            int travelDays = UnityMathfCompatibility.Max(1, route.TravelDays);
            float score = (safetyGain - travelCost) / travelDays;
            if (score <= 0f) continue;
            float utility = 30f + score;
            if (bestDestination == null
                || utility > bestUtility
                || (UnityMathfCompatibility.Approximately(utility, bestUtility)
                    && string.CompareOrdinal(destination.RuntimeId, bestDestination.RuntimeId) < 0))
            {
                bestDestination = destination;
                bestTravelCost = travelCost;
                bestUtility = utility;
            }
        }

        if (bestDestination == null
            || !DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, candidate.Action, null, bestDestination.RuntimeId, null,
                0, bestTravelCost, 1f, NpcTravelReason.Flee, 0f,
                null, null, null, null, out DailyV1NpcActionPayloadOwner payload))
        {
            candidate.Utility = 0f;
            return true;
        }

        candidate.Utility = UnityMathfCompatibility.Max(candidate.Utility, bestUtility);
        candidate.PreparedPayload = payload;
        return true;
    }

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

    private static bool TryGetExactCityAtLocation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string locationRuntimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph == null || string.IsNullOrWhiteSpace(locationRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (candidate == null
                || !string.Equals(candidate.LocationRuntimeId,
                    locationRuntimeId, StringComparison.Ordinal))
                continue;
            if (city != null || !graph.ContainsExactCityOwner(candidate)) return false;
            city = candidate;
        }
        return city != null;
    }

    private static bool CanActInCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph?.JusticeRecordsOwner == null || actor?.Lifecycle?.LifeState != NpcLifeState.Alive
            || actor.Travel == null || actor.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(actor.Travel.CurrentCityRuntimeId)
            || !TryGetExactCity(graph, actor.Travel.CurrentCityRuntimeId, out city)
            || !city.NpcPresence.Contains(actor.RuntimeId)
            || !TryGetArrestedStatus(graph.Definitions, actor, out bool isArrested)
            || isArrested)
        {
            city = null;
            return false;
        }
        return true;
    }

    private static bool TryGetArrestedStatus(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcRuntimeOwners actor,
        out bool isArrested)
    {
        isArrested = false;
        string arrestedStatusId = definitions?.Configuration?.arrestedStatus;
        if (string.IsNullOrWhiteSpace(arrestedStatusId)) return definitions != null && actor != null;
        if (!definitions.Statuses.TryGetValue(arrestedStatusId,
                out DailyV1StatusDefinition arrestedStatus))
            return false;
        isArrested = actor?.Status?.Contains(arrestedStatus) == true;
        return true;
    }

    private static bool IsValidStealTarget(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1CityRuntimeOwners city,
        DailyV1NpcRuntimeOwners target)
    {
        return target != null && !ReferenceEquals(target, actor)
            && target.Lifecycle?.LifeState == NpcLifeState.Alive
            && target.Travel != null && !target.Travel.IsTraveling
            && string.Equals(target.Travel.CurrentCityRuntimeId, city.RuntimeId,
                StringComparison.Ordinal)
            && target.HiddenState?.RemainingDays <= 0
            && target.MoneyAccount != null && target.MoneyAccount.Balance > 0f
            && TryGetArrestedStatus(graph.Definitions, target, out bool arrested)
            && !arrested;
    }

    private static bool TryResolveMaterializedPerson(
        PersonStore personStore,
        string npcRuntimeId,
        out PersonRuntime person)
    {
        person = null;
        return personStore != null
            && !string.IsNullOrWhiteSpace(npcRuntimeId)
            && personStore.TryGetByMaterializedNpcRuntimeId(npcRuntimeId, out person)
            && person != null && person.PersonId != null && person.IsMaterialized
            && string.Equals(person.MaterializedNpcRuntimeId, npcRuntimeId, StringComparison.Ordinal)
            && personStore.TryGet(person.PersonId, out PersonRuntime registered)
            && ReferenceEquals(person, registered);
    }
}
