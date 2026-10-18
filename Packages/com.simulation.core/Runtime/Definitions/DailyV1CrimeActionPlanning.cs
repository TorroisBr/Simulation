/// <summary>Selected Daily-v1 crime-provider preparation rules over admitted action identity and owner facts.</summary>
internal static class DailyV1CrimeActionPlanning
{
    internal const float EscapeSuccessMultiplierPerFailure = 0.8f;

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
}
