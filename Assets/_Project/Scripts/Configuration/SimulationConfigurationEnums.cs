/// <summary>
/// States whether a simulation feature exists in the configured world.
/// </summary>
public enum SimulationFeatureAvailability
{
    Unavailable = 0,
    Available = 1
}

/// <summary>
/// States whether a configured domain may start behavior without an external command.
/// </summary>
public enum SimulationAutonomyMode
{
    ManualOnly = 0,
    Autonomous = 1
}

/// <summary>
/// Controls how much population identity is represented by the simulation.
/// </summary>
public enum PopulationRepresentationMode
{
    Aggregate = 0,
    Hybrid = 1,
    FullyIndividualized = 2
}

/// <summary>
/// Controls which materialized NPCs participate in detailed decision processing.
/// </summary>
public enum NpcDecisionSimulationScope
{
    RelevantOnly = 0,
    AllMaterialized = 1
}
