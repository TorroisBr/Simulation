using Simulation.WorldExchangeProducer;

/// <summary>Result of attempting one coherent Simulation-to-World-Exchange export.</summary>
public sealed class SimulationWorldExchangeExportResult
{
    internal SimulationWorldExchangeExportResult(
        WorldExchangeV2Artifact artifact,
        string failureCode,
        string failureMessage,
        string diagnosticCapabilityId)
    {
        Artifact = artifact;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        DiagnosticCapabilityId = diagnosticCapabilityId;
    }

    public bool Succeeded => Artifact != null;
    public WorldExchangeV2Artifact Artifact { get; }
    public string FailureCode { get; }
    public string FailureMessage { get; }
    public string DiagnosticCapabilityId { get; }
}
