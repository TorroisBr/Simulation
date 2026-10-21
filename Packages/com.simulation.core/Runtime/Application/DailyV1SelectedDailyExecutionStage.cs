using System;

/// <summary>
/// Operations which are still hosted outside Core while the selected
/// Daily-v1 runtime source closure is being integrated.
/// </summary>
internal interface IDailyV1SelectedDailyExecutionBoundary
{
    void AdvancePlaceContent();
    void BeginLoggerDay(long absoluteDay);
    void AdvanceSelectedDemography();
    void BeginSimulationDay();
    void PrepareScheduledDirectives(long absoluteDay);
    void SimulateEconomyDay();
    void RefreshLocalKnowledgeAndShare();
    void AdvanceActiveExpeditions();
    void StartAutonomousExpeditions();
    void AdvanceActorTurns();
    void AdvanceTravelAndObserve();
}

/// <summary>
/// Owns the order of the selected Daily-v1 runtime phases. Domain work that
/// has not yet crossed the shared source boundary is forwarded to the
/// existing Unity owners through the narrow boundary above.
/// </summary>
internal static class DailyV1SelectedDailyExecutionStage
{
    internal static void Advance(
        long absoluteDay,
        DailyV1EffectiveDefinitions definitions,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));

        boundary.AdvancePlaceContent();
        boundary.BeginLoggerDay(absoluteDay);
        boundary.AdvanceSelectedDemography();
        boundary.BeginSimulationDay();
        boundary.PrepareScheduledDirectives(absoluteDay);

        if (definitions.EffectiveConfiguration?.Economy?.Enabled == true)
            boundary.SimulateEconomyDay();

        boundary.RefreshLocalKnowledgeAndShare();
        boundary.AdvanceActiveExpeditions();
        boundary.StartAutonomousExpeditions();
        boundary.AdvanceActorTurns();
        boundary.AdvanceTravelAndObserve();
    }
}
