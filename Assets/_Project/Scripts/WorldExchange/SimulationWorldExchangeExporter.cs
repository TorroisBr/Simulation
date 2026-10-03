using System;
using System.Collections.Generic;
using Simulation.WorldExchangeProducer;

/// <summary>
/// Narrow host adapter. It reads only one promoted FR-C coherent capture from
/// a published bootstrap and hands copied values to the portable producer.
/// </summary>
public static class SimulationWorldExchangeExporter
{
    public const string FactionTruthCapabilityId = "simulation.faction-truth/v1";

    public static SimulationWorldExchangeExportResult TryCreateArtifact(SimulationBootstrapComposition composition)
    {
        if (composition == null)
            return Failed("composition.missing", "A published Simulation bootstrap composition is required.", null);
        if (composition.WorldId == null || !WorldId.TryParse(composition.WorldId.Value, out WorldId parsedWorldId)
            || !string.Equals(parsedWorldId.Value, composition.WorldId.Value, StringComparison.Ordinal))
            return Failed("world-id.invalid", "The published WorldId is not canonical.", null);

        FactualReadCapture capture = null;
        bool captured;
        try
        {
            captured = composition.FactualReads != null
                && composition.FactualReads.TryCaptureCoherent(out capture, FactionTruthCapabilityId);
        }
        catch (Exception)
        {
            return Failed("factual-read.exception", "The coherent Faction factual read could not be completed.", null);
        }

        if (!captured)
        {
            FactualReadDiagnostic diagnostic = FirstDiagnostic(capture);
            return Failed(
                diagnostic?.Code ?? "factual-read.unavailable",
                diagnostic?.Message ?? "A coherent Faction factual read is unavailable.",
                diagnostic?.CapabilityId ?? FactionTruthCapabilityId);
        }

        if (capture == null || !capture.IsCoherent || capture.Mode != FactualReadCaptureMode.Coherent
            || !capture.LogicalBoundary.HasValue || capture.LogicalBoundary.Value < 0L
            || !capture.FactionStoreRevision.HasValue || capture.FactionStoreRevision.Value < 0L
            || !capture.PersonStoreRevision.HasValue || capture.PersonStoreRevision.Value < 0L
            || capture.RequestedCapabilityIds == null || capture.RequestedCapabilityIds.Count != 1
            || !string.Equals(capture.RequestedCapabilityIds[0], FactionTruthCapabilityId, StringComparison.Ordinal)
            || capture.SourceVersions == null || capture.SourceVersions.Count != 1
            || !string.Equals(capture.SourceVersions[0].CapabilityId, FactionTruthCapabilityId, StringComparison.Ordinal)
            || capture.SourceVersions[0].Version != 1)
        {
            return Failed("factual-read.evidence-invalid", "The capture lacks the required coherent boundary, revisions, or FR-C source version.", FactionTruthCapabilityId);
        }

        if (!capture.TryGet(FactionTruthCapabilityId, out FactReadResult<FactionTruthFacts> result)
            || result == null || result.Status != FactReadStatus.Present || result.Value == null)
        {
            FactualReadDiagnostic diagnostic = FirstDiagnostic(capture);
            return Failed(
                diagnostic?.Code ?? "faction-truth.not-present",
                diagnostic?.Message ?? "FR-C did not return a Present complete Faction projection.",
                diagnostic?.CapabilityId ?? FactionTruthCapabilityId);
        }

        List<FactionProjectionInput> copiedFacts = new List<FactionProjectionInput>(result.Value.Factions.Count);
        foreach (FactionFact fact in result.Value.Factions)
        {
            if (fact == null || fact.FactionId == null)
                return Failed("faction-truth.invalid-fact", "FR-C returned an invalid Faction fact.", FactionTruthCapabilityId);
            copiedFacts.Add(new FactionProjectionInput(fact.FactionId.Value, fact.DisplayName));
        }

        if (!WorldExchangeV2Artifact.TryCreate(
            composition.WorldId.Value,
            copiedFacts,
            out WorldExchangeV2Artifact artifact,
            out string failureCode,
            out string failureMessage))
        {
            return Failed(failureCode, failureMessage, FactionTruthCapabilityId);
        }
        return new SimulationWorldExchangeExportResult(artifact, null, null, null);
    }

    public static SimulationWorldExchangeExportResult TryExportToFile(
        SimulationBootstrapComposition composition,
        string destinationPath)
    {
        SimulationWorldExchangeExportResult result = TryCreateArtifact(composition);
        if (!result.Succeeded)
            return result;

        WorldExchangePublishResult published = WorldExchangeV2FilePublisher.WriteAtomically(
            result.Artifact,
            destinationPath);
        if (!published.Succeeded)
            return Failed(published.FailureCode, published.FailureMessage, null);
        return result;
    }

    private static FactualReadDiagnostic FirstDiagnostic(FactualReadCapture capture)
    {
        return capture?.Diagnostics != null && capture.Diagnostics.Count > 0
            ? capture.Diagnostics[0]
            : null;
    }

    private static SimulationWorldExchangeExportResult Failed(string code, string message, string capabilityId)
    {
        return new SimulationWorldExchangeExportResult(null, code, message, capabilityId);
    }
}
