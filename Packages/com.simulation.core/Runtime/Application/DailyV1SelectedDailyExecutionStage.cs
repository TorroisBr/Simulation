using System;
using System.Collections.Generic;

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
    void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
    void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message);
    void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
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
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));

        boundary.AdvancePlaceContent();
        boundary.BeginLoggerDay(absoluteDay);
        boundary.AdvanceSelectedDemography();
        boundary.BeginSimulationDay();
        boundary.PrepareScheduledDirectives(absoluteDay);

        if (graph.Definitions.EffectiveConfiguration?.Economy?.Enabled == true)
        {
            boundary.EnsureSelectedEconomyCanRun(graph);
            if (!graph.TrySimulateDailyEconomyDay(
                    graph.Cities,
                    absoluteDay,
                    GetCalendarSemanticVersion(graph.Definitions.Calendar),
                    boundary.LogSelectedDailyEconomy))
                throw new InvalidOperationException(
                    "The selected Core economy stage could not complete against its exact runtime owner graph.");
        }

        boundary.EnsureSelectedLocalKnowledgeCanRun(graph);
        if (!DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(graph, absoluteDay))
            throw new InvalidOperationException(
                "The selected Core local-knowledge stage could not complete against its exact runtime owner graph.");

        boundary.AdvanceActiveExpeditions();
        boundary.StartAutonomousExpeditions();
        boundary.AdvanceActorTurns();
        boundary.AdvanceTravelAndObserve();
    }

    private static string GetCalendarSemanticVersion(DailyV1CalendarDefinition calendar)
    {
        if (calendar == null) throw new InvalidOperationException(
            "The selected Core economy stage requires an admitted calendar definition.");

        CalendarDefinition definition = calendar.monthLengths != null && calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(calendar.monthLengths), calendar.daysPerWeek)
            : new CalendarDefinition(calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek);
        return new SimulationCalendar(definition).SemanticVersion;
    }
}
