/// <summary>Selected Daily-v1 travel-provider planning over admitted plan values.</summary>
internal static class DailyV1TravelActionPlanning
{
    internal static bool TryApplyExistingTravelPlanUtility(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition actor,
        DailyV1ActionDefinition action,
        float travelPlanUtility,
        bool isTradeTravelReason,
        bool matchingMerchantTradePlan,
        int pendingTradeTravelDays,
        float currentUtility,
        out float resultingUtility)
    {
        resultingUtility = 0f;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, actor)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(definitions, action))
            return false;

        float planUtility = travelPlanUtility;
        if (isTradeTravelReason && matchingMerchantTradePlan)
            planUtility += UnityMathfCompatibility.Min(30f, pendingTradeTravelDays * 10f);

        resultingUtility = UnityMathfCompatibility.Max(
            currentUtility,
            UnityMathfCompatibility.Clamp(planUtility, 0f, 100f));
        return true;
    }
}
