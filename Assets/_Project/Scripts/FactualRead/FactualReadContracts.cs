using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum FactReadStatus
{
    Present = 0,
    Absent = 1,
    Unavailable = 2,
    Unsupported = 3,
    OutsideFactualScope = 4
}

public enum FactReadOutsideScopeReason
{
    DerivedDisplay = 0,
    ActorKnowledge = 1
}

public enum FactualReadCaptureMode
{
    Coherent = 0
}

internal interface IFactualReadResult
{
    FactReadStatus Status { get; }
    Type ValueType { get; }
    object UntypedValue { get; }
    FactReadOutsideScopeReason? OutsideScopeReason { get; }
    string SourceAuthority { get; }
}

/// <summary>A status-bearing result from one approved factual capability.</summary>
public sealed class FactReadResult<T> : IFactualReadResult
{
    private readonly T value;

    private FactReadResult(
        FactReadStatus status,
        T value,
        FactReadOutsideScopeReason? outsideScopeReason,
        string sourceAuthority)
    {
        Status = status;
        this.value = value;
        OutsideScopeReason = outsideScopeReason;
        SourceAuthority = sourceAuthority;
    }

    public FactReadStatus Status { get; }
    public FactReadOutsideScopeReason? OutsideScopeReason { get; }
    public string SourceAuthority { get; }

    /// <summary>Returns the value only for Present results.</summary>
    public T Value
    {
        get
        {
            if (Status != FactReadStatus.Present)
                throw new InvalidOperationException("Only a Present factual read result has a value.");
            return value;
        }
    }

    public bool TryGetValue(out T result)
    {
        if (Status == FactReadStatus.Present)
        {
            result = value;
            return true;
        }

        result = default(T);
        return false;
    }

    public static FactReadResult<T> Present(T result)
    {
        if (ReferenceEquals(result, null))
            throw new ArgumentNullException(nameof(result), "Present requires a nonnull immutable value.");
        return new FactReadResult<T>(FactReadStatus.Present, result, null, null);
    }

    /// <summary>Creates authoritative absence for an optional scalar or keyed lookup.</summary>
    public static FactReadResult<T> Absent()
    {
        if (IsCollectionType(typeof(T)))
            throw new InvalidOperationException("A collection cannot use Absent; a known-empty collection is Present([]).");
        return new FactReadResult<T>(FactReadStatus.Absent, default(T), null, null);
    }

    public static FactReadResult<T> Unavailable()
    {
        return new FactReadResult<T>(FactReadStatus.Unavailable, default(T), null, null);
    }

    public static FactReadResult<T> Unsupported()
    {
        return new FactReadResult<T>(FactReadStatus.Unsupported, default(T), null, null);
    }

    public static FactReadResult<T> OutsideFactualScope(
        FactReadOutsideScopeReason reason,
        string sourceAuthority)
    {
        if (!Enum.IsDefined(typeof(FactReadOutsideScopeReason), reason))
            throw new ArgumentOutOfRangeException(nameof(reason));
        if (string.IsNullOrWhiteSpace(sourceAuthority))
            throw new ArgumentException("Outside-scope results must identify their source authority.", nameof(sourceAuthority));
        return new FactReadResult<T>(FactReadStatus.OutsideFactualScope, default(T), reason, sourceAuthority);
    }

    internal static FactReadResult<T> CopyNonPresent(IFactualReadResult source)
    {
        if (source == null)
            return null;

        switch (source.Status)
        {
            case FactReadStatus.Absent:
                return IsCollectionType(typeof(T)) ? null : Absent();
            case FactReadStatus.Unavailable:
                return Unavailable();
            case FactReadStatus.Unsupported:
                return Unsupported();
            case FactReadStatus.OutsideFactualScope:
                return source.OutsideScopeReason.HasValue && !string.IsNullOrWhiteSpace(source.SourceAuthority)
                    ? OutsideFactualScope(source.OutsideScopeReason.Value, source.SourceAuthority)
                    : null;
            default:
                return null;
        }
    }

    private static bool IsCollectionType(Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    Type IFactualReadResult.ValueType => typeof(T);
    object IFactualReadResult.UntypedValue => Status == FactReadStatus.Present ? (object)value : null;
}

/// <summary>Factories that materialize immutable factual collection values.</summary>
internal static class FactReadResultFactory
{
    internal static FactReadResult<IReadOnlyList<T>> PresentList<T>(IEnumerable<T> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        List<T> copy = new List<T>(values);
        return FactReadResult<IReadOnlyList<T>>.Present(copy.AsReadOnly());
    }
}

/// <summary>Semantic version evidence for a capability included in a coherent cut.</summary>
public sealed class FactualReadSourceVersion
{
    internal FactualReadSourceVersion(string capabilityId, int version)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("A capability id is required.", nameof(capabilityId));
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version));
        CapabilityId = capabilityId;
        Version = version;
    }

    public string CapabilityId { get; }
    public int Version { get; }
}

internal sealed class FactualReadResultEntry
{
    internal FactualReadResultEntry(string capabilityId, Type valueType, IFactualReadResult result)
    {
        CapabilityId = capabilityId;
        ValueType = valueType;
        Result = result;
    }

    internal string CapabilityId { get; }
    internal Type ValueType { get; }
    internal IFactualReadResult Result { get; }
}

/// <summary>
/// Immutable output from one coherent synchronous capture. This is a bounded
/// Faction/Person boundary, not a whole-world snapshot.
/// </summary>
public sealed class FactualReadCapture
{
    private readonly Dictionary<string, FactualReadResultEntry> resultsByCapability;
    private readonly ReadOnlyCollection<string> requestedCapabilityIds;
    private readonly ReadOnlyCollection<FactualReadSourceVersion> sourceVersions;

    internal FactualReadCapture(
        bool isCoherent,
        long? logicalBoundary,
        long? factionStoreRevision,
        long? personStoreRevision,
        IEnumerable<FactualReadResultEntry> results,
        IEnumerable<FactualReadSourceVersion> sourceVersions)
    {
        IsCoherent = isCoherent;
        Mode = FactualReadCaptureMode.Coherent;
        LogicalBoundary = logicalBoundary;
        FactionStoreRevision = factionStoreRevision;
        PersonStoreRevision = personStoreRevision;

        List<FactualReadResultEntry> copiedResults = results == null
            ? new List<FactualReadResultEntry>()
            : new List<FactualReadResultEntry>(results);
        copiedResults.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.CapabilityId,
            right.CapabilityId));

        resultsByCapability = new Dictionary<string, FactualReadResultEntry>(StringComparer.Ordinal);
        List<string> requested = new List<string>(copiedResults.Count);
        foreach (FactualReadResultEntry entry in copiedResults)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.CapabilityId) || entry.Result == null)
                throw new ArgumentException("Capture result entries must be complete.", nameof(results));
            if (resultsByCapability.ContainsKey(entry.CapabilityId))
                throw new ArgumentException("Capture capability ids must be unique.", nameof(results));
            resultsByCapability.Add(entry.CapabilityId, entry);
            requested.Add(entry.CapabilityId);
        }
        requestedCapabilityIds = requested.AsReadOnly();

        List<FactualReadSourceVersion> copiedVersions = sourceVersions == null
            ? new List<FactualReadSourceVersion>()
            : new List<FactualReadSourceVersion>(sourceVersions);
        copiedVersions.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.CapabilityId,
            right.CapabilityId));
        this.sourceVersions = copiedVersions.AsReadOnly();
    }

    public FactualReadCaptureMode Mode { get; }
    public bool IsCoherent { get; }
    public long? LogicalBoundary { get; }
    public long? FactionStoreRevision { get; }
    public long? PersonStoreRevision { get; }
    public IReadOnlyList<string> RequestedCapabilityIds => requestedCapabilityIds;
    public IReadOnlyList<FactualReadSourceVersion> SourceVersions => sourceVersions;

    public bool TryGet<T>(string capabilityId, out FactReadResult<T> result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(capabilityId)
            || !resultsByCapability.TryGetValue(capabilityId, out FactualReadResultEntry entry))
        {
            return false;
        }

        if (entry.ValueType != null && entry.ValueType != typeof(T))
            return false;

        if (entry.Result.Status == FactReadStatus.Present)
        {
            result = entry.Result as FactReadResult<T>;
            return result != null;
        }

        result = FactReadResult<T>.CopyNonPresent(entry.Result);
        return result != null;
    }
}
