using System;
using System.Collections.Generic;

internal interface IFactualReader
{
    string CapabilityId { get; }
    int Version { get; }
    Type ValueType { get; }
    IFactualReadOutcome ReadUntyped(long logicalBoundary);
}

internal interface IFactualReader<T> : IFactualReader
{
    FactualReadOutcome<T> Read(long logicalBoundary);
}

/// <summary>
/// Synchronously materializes an explicit finite reader set under the shared
/// Faction/Person admission. No best-effort mode is provided by this slice.
/// </summary>
public sealed class FactualReadCoordinator
{
    private readonly FactualReadAdmission admission;
    private readonly Dictionary<string, IFactualReader> readersByCapability;

    internal FactualReadCoordinator(
        FactualReadAdmission admission,
        IEnumerable<IFactualReader> readers)
    {
        this.admission = admission ?? throw new ArgumentNullException(nameof(admission));
        if (readers == null)
            throw new ArgumentNullException(nameof(readers));

        readersByCapability = new Dictionary<string, IFactualReader>(StringComparer.Ordinal);
        foreach (IFactualReader reader in readers)
        {
            if (reader == null || string.IsNullOrWhiteSpace(reader.CapabilityId)
                || reader.Version <= 0 || reader.ValueType == null)
            {
                throw new ArgumentException("Every factual reader must declare a capability id, positive version, and value type.", nameof(readers));
            }
            if (readersByCapability.ContainsKey(reader.CapabilityId))
                throw new ArgumentException("Factual reader capability ids must be unique.", nameof(readers));
            readersByCapability.Add(reader.CapabilityId, reader);
        }
    }

    public bool TryCaptureCoherent(out FactualReadCapture capture, params string[] requestedCapabilityIds)
    {
        string[] requested;
        if (!TryCopyAndSortRequests(requestedCapabilityIds, out requested))
        {
            capture = CreateUnavailableCapture(new string[0]);
            return false;
        }

        FactualReadReadLease lease;
        if (!admission.TryBeginRead(out lease))
        {
            capture = CreateUnavailableCapture(requested);
            return false;
        }

        try
        {
            if (!admission.TryReadCurrentBoundary(out FactualReadBoundary before))
            {
                capture = CreateUnavailableCapture(requested);
                return false;
            }

            List<FactualReadResultEntry> results = new List<FactualReadResultEntry>(requested.Length);
            List<FactualReadSourceVersion> sourceVersions = new List<FactualReadSourceVersion>();
            foreach (string capabilityId in requested)
            {
                if (!readersByCapability.TryGetValue(capabilityId, out IFactualReader reader))
                {
                    results.Add(new FactualReadResultEntry(
                        capabilityId,
                        null,
                        FactReadResult<object>.Unsupported()));
                    continue;
                }

                IFactualReadOutcome outcome;
                try
                {
                    outcome = reader.ReadUntyped(before.LogicalBoundary);
                }
                catch (Exception)
                {
                    capture = CreateUnavailableCapture(
                        requested,
                        new[]
                        {
                            CreateDiagnostic(
                                capabilityId,
                                "reader-exception",
                                "A factual reader failed during coherent capture.")
                        });
                    return false;
                }

                IFactualReadResult result = outcome?.Result;
                if (result == null || result.ValueType != reader.ValueType)
                {
                    capture = CreateUnavailableCapture(
                        requested,
                        new[]
                        {
                            CreateDiagnostic(
                                capabilityId,
                                "reader-invalid-outcome",
                                "A factual reader returned an invalid outcome.")
                        });
                    return false;
                }

                if (result.Status == FactReadStatus.Unavailable)
                {
                    FactualReadDiagnosticData diagnostic = outcome.Diagnostic;
                    if (diagnostic == null)
                    {
                        diagnostic = new FactualReadDiagnosticData(
                            "reader-unavailable",
                            "The factual reader reported unavailable data.");
                    }
                    capture = CreateUnavailableCapture(
                        requested,
                        new[] { CreateDiagnostic(capabilityId, diagnostic.Code, diagnostic.Message) });
                    return false;
                }

                if (outcome.Diagnostic != null)
                {
                    capture = CreateUnavailableCapture(
                        requested,
                        new[]
                        {
                            CreateDiagnostic(
                                capabilityId,
                                "reader-invalid-outcome",
                                "A factual reader returned an invalid outcome.")
                        });
                    return false;
                }

                results.Add(new FactualReadResultEntry(capabilityId, reader.ValueType, result));
                sourceVersions.Add(new FactualReadSourceVersion(capabilityId, reader.Version));
            }

            if (!admission.TryReadCurrentBoundary(out FactualReadBoundary after)
                || !before.Matches(after))
            {
                capture = CreateUnavailableCapture(requested);
                return false;
            }

            capture = new FactualReadCapture(
                true,
                before.LogicalBoundary,
                before.FactionStoreRevision,
                before.PersonStoreRevision,
                results,
                sourceVersions);
            return true;
        }
        catch (Exception)
        {
            capture = CreateUnavailableCapture(requested);
            return false;
        }
        finally
        {
            lease.Dispose();
        }
    }

    private FactualReadCapture CreateUnavailableCapture(
        IReadOnlyList<string> requested,
        IEnumerable<FactualReadDiagnostic> diagnostics = null)
    {
        List<FactualReadResultEntry> results = new List<FactualReadResultEntry>();
        if (requested != null)
        {
            for (int i = 0; i < requested.Count; i++)
            {
                string capabilityId = requested[i];
                Type valueType = readersByCapability.TryGetValue(capabilityId, out IFactualReader reader)
                    ? reader.ValueType
                    : null;
                results.Add(new FactualReadResultEntry(
                    capabilityId,
                    valueType,
                    FactReadResult<object>.Unavailable()));
            }
        }

        return new FactualReadCapture(false, null, null, null, results, null, diagnostics);
    }

    private static FactualReadDiagnostic CreateDiagnostic(string capabilityId, string code, string message)
    {
        return new FactualReadDiagnostic(capabilityId, code, message);
    }

    private static bool TryCopyAndSortRequests(string[] source, out string[] requested)
    {
        requested = new string[0];
        if (source == null)
            return false;

        List<string> copy = new List<string>(source.Length);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < source.Length; i++)
        {
            string capabilityId = source[i];
            if (string.IsNullOrWhiteSpace(capabilityId))
                return false;
            if (seen.Add(capabilityId))
                copy.Add(capabilityId);
        }
        copy.Sort(StringComparer.Ordinal);
        requested = copy.ToArray();
        return true;
    }
}
