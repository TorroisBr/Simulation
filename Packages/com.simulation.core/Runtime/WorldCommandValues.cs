/// <summary>Origin labels retained by the shared typed world-command boundary.</summary>
public enum WorldCommandOrigin
{
    GM,
    Table,
    Scenario,
    ExternalImport,
    System,
    Internal,
    LocalPlayer
}

/// <summary>Requested authority mode for a world command.</summary>
public enum WorldCommandAuthorityMode
{
    Suggest,
    Request,
    Declare,
    ForceOutcome
}
