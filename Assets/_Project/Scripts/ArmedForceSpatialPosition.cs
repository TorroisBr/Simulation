using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ArmedForceSpatialFailureCode
{
    None = 0,
    ForceNotRegistered = 1,
    ForceTerminated = 2,
    InvalidSpatialReference = 3,
    SpatialReferenceNotRegistered = 4,
    RevisionOverflow = 5,
    InvalidInvariant = 6
}

public sealed class ArmedForceSpatialFailure : IEquatable<ArmedForceSpatialFailure>
{
    private static readonly ArmedForceSpatialFailure none =
        new ArmedForceSpatialFailure(ArmedForceSpatialFailureCode.None, string.Empty, null);

    private ArmedForceSpatialFailure(
        ArmedForceSpatialFailureCode code,
        string message,
        SpatialAuthorityFailure authorityFailure)
    {
        Code = code;
        Message = message ?? string.Empty;
        AuthorityFailure = authorityFailure;
    }

    public static ArmedForceSpatialFailure None => none;
    public ArmedForceSpatialFailureCode Code { get; }
    public string Message { get; }
    public SpatialAuthorityFailure AuthorityFailure { get; }
    public bool IsFailure => Code != ArmedForceSpatialFailureCode.None;

    internal static ArmedForceSpatialFailure Create(
        ArmedForceSpatialFailureCode code,
        string message,
        SpatialAuthorityFailure authorityFailure = null)
    {
        return code == ArmedForceSpatialFailureCode.None
            ? None
            : new ArmedForceSpatialFailure(code, message, authorityFailure);
    }

    public bool Equals(ArmedForceSpatialFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal)
            && Equals(AuthorityFailure, other.AuthorityFailure);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForceSpatialFailure);
    public override int GetHashCode() => ((int)Code * 397)
        ^ StringComparer.Ordinal.GetHashCode(Message)
        ^ (AuthorityFailure == null ? 0 : AuthorityFailure.GetHashCode());

    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

public sealed class ArmedForceSpatialPosition
{
    public ArmedForceId ForceId { get; }
    public SpatialReference Position { get; }

    public ArmedForceSpatialPosition(ArmedForceId forceId, SpatialReference position)
    {
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public string StableKey => ForceId.Value;
}

public sealed class ArmedForceSpatialInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;

    internal ArmedForceSpatialInvariantReport(IEnumerable<string> violations)
    {
        List<string> values = violations == null
            ? new List<string>()
            : new List<string>(violations);
        values.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(values);
    }
}

/// <summary>
/// Authoritative optional current physical position for ArmedForce identities.
/// It is deliberately separate from force identity, hierarchy, detachment,
/// composition, lifecycle, and the legacy OperationalLocationReference shim.
/// This store performs no movement, battle, war, or daily processing.
/// </summary>
public sealed class ArmedForceSpatialStateStore
{
    private readonly ArmedForceStore armedForceStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly LocalTopologyStore localTopologyStore;
    private readonly Dictionary<string, SpatialReference> positionsByForceId =
        new Dictionary<string, SpatialReference>(StringComparer.Ordinal);
    private long revision;

    public ArmedForceSpatialStateStore(
        ArmedForceStore armedForceStore,
        SpatialAuthorityStore spatialAuthorityStore,
        LocalTopologyStore localTopologyStore = null)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore));
        this.localTopologyStore = localTopologyStore;
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public SpatialAuthorityStore SpatialAuthorityStore => spatialAuthorityStore;
    public LocalTopologyStore LocalTopologyStore => localTopologyStore;
    public long Revision => revision;
    public int Count => positionsByForceId.Count;

    public IReadOnlyList<ArmedForceSpatialPosition> Positions
    {
        get
        {
            List<ArmedForceSpatialPosition> result = new List<ArmedForceSpatialPosition>();
            foreach (KeyValuePair<string, SpatialReference> entry in positionsByForceId)
            {
                result.Add(new ArmedForceSpatialPosition(new ArmedForceId(entry.Key), entry.Value));
            }

            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.StableKey, right.StableKey));
            return new ReadOnlyCollection<ArmedForceSpatialPosition>(result);
        }
    }

    public bool TryGetPosition(ArmedForceId forceId, out SpatialReference position)
    {
        position = null;
        return forceId != null && positionsByForceId.TryGetValue(forceId.Value, out position);
    }

    public bool TrySetPosition(
        ArmedForceId forceId,
        SpatialReference position,
        out ArmedForceSpatialFailure failure)
    {
        if (TryResolveActiveForce(forceId, out failure) == false)
        {
            return false;
        }

        if (TryResolvePosition(position, out failure) == false)
        {
            return false;
        }

        if (positionsByForceId.TryGetValue(forceId.Value, out SpatialReference current)
            && current.Equals(position))
        {
            failure = ArmedForceSpatialFailure.None;
            return true;
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        positionsByForceId[forceId.Value] = position;
        revision++;
        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    public bool TryClearPosition(
        ArmedForceId forceId,
        out ArmedForceSpatialFailure failure)
    {
        if (TryResolveActiveForce(forceId, out failure) == false)
        {
            return false;
        }

        if (positionsByForceId.ContainsKey(forceId.Value) == false)
        {
            failure = ArmedForceSpatialFailure.None;
            return true;
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        positionsByForceId.Remove(forceId.Value);
        revision++;
        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    /// <summary>
    /// Checks whether the force's current position proves presence in the
    /// required typed area. A valid query may return compatible=false when no
    /// current position exists or the references are incompatible. Invalid or
    /// unresolved references return false with a failure.
    /// </summary>
    public bool TryCheckCompatibility(
        ArmedForceId forceId,
        SpatialReference requiredArea,
        out bool compatible,
        out ArmedForceSpatialFailure failure)
    {
        compatible = false;
        if (TryResolveActiveForce(forceId, out failure) == false)
        {
            return false;
        }

        if (TryResolvePosition(requiredArea, out failure, "Required spatial area") == false)
        {
            return false;
        }

        if (positionsByForceId.TryGetValue(forceId.Value, out SpatialReference currentPosition) == false)
        {
            failure = ArmedForceSpatialFailure.None;
            return true;
        }

        if (TryResolvePosition(currentPosition, out failure, "Current spatial position") == false)
        {
            return false;
        }

        SpatialResolution requiredResolution;
        SpatialAuthorityFailure requiredFailure;
        spatialAuthorityStore.TryResolve(
            requiredArea,
            localTopologyStore,
            out requiredResolution,
            out requiredFailure);
        SpatialResolution currentResolution;
        SpatialAuthorityFailure currentFailure;
        spatialAuthorityStore.TryResolve(
            currentPosition,
            localTopologyStore,
            out currentResolution,
            out currentFailure);

        switch (requiredArea.Kind)
        {
            case SpatialReferenceKind.Hex:
                compatible = currentResolution.Hex != null
                    && requiredResolution.Hex != null
                    && currentResolution.Hex.Id == requiredResolution.Hex.Id;
                break;
            case SpatialReferenceKind.Location:
                compatible = currentResolution.Location != null
                    && requiredResolution.Location != null
                    && currentResolution.Location.Id == requiredResolution.Location.Id;
                break;
            case SpatialReferenceKind.SubLocation:
                compatible = currentPosition.Equals(requiredArea);
                break;
            default:
                return Fail(
                    ArmedForceSpatialFailureCode.InvalidSpatialReference,
                    "SpatialReference kind is invalid.",
                    out failure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    public ArmedForceSpatialInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        foreach (KeyValuePair<string, SpatialReference> entry in positionsByForceId)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                violations.Add("ArmedForce spatial position has an empty force identity.");
                continue;
            }

            ArmedForceId forceId = new ArmedForceId(entry.Key);
            if (armedForceStore.TryGet(forceId, out _) == false)
            {
                violations.Add("ArmedForce spatial position references a missing force: " + entry.Key + ".");
            }

            if (TryResolvePosition(entry.Value, out ArmedForceSpatialFailure failure) == false)
            {
                violations.Add("ArmedForce spatial position is unresolved for " + entry.Key + ": " + failure + ".");
            }
        }

        return new ArmedForceSpatialInvariantReport(violations);
    }

    internal ArmedForceSpatialStateStore Clone(
        ArmedForceStore targetArmedForceStore,
        SpatialAuthorityStore targetSpatialAuthorityStore,
        LocalTopologyStore targetLocalTopologyStore)
    {
        if (targetArmedForceStore == null) throw new ArgumentNullException(nameof(targetArmedForceStore));
        if (targetSpatialAuthorityStore == null) throw new ArgumentNullException(nameof(targetSpatialAuthorityStore));

        ArmedForceSpatialStateStore copy = new ArmedForceSpatialStateStore(
            targetArmedForceStore,
            targetSpatialAuthorityStore,
            targetLocalTopologyStore);
        foreach (KeyValuePair<string, SpatialReference> entry in positionsByForceId)
        {
            if (targetArmedForceStore.TryGet(new ArmedForceId(entry.Key), out _) == false)
            {
                throw new ArgumentException(
                    "The ArmedForce spatial state references a force absent from the target ArmedForceStore.",
                    nameof(targetArmedForceStore));
            }

            if (targetSpatialAuthorityStore.TryResolve(
                    entry.Value,
                    targetLocalTopologyStore,
                    out _,
                    out SpatialAuthorityFailure failure) == false)
            {
                throw new ArgumentException(
                    "The ArmedForce spatial state references an area absent from the target spatial authority: " + failure,
                    nameof(targetSpatialAuthorityStore));
            }

            copy.positionsByForceId.Add(entry.Key, entry.Value);
        }

        copy.revision = revision;
        return copy;
    }

    private bool TryResolveActiveForce(
        ArmedForceId forceId,
        out ArmedForceSpatialFailure failure)
    {
        if (forceId == null || armedForceStore.TryGet(forceId, out ArmedForceRecord force) == false)
        {
            return Fail(
                ArmedForceSpatialFailureCode.ForceNotRegistered,
                "The ArmedForceId is not registered.",
                out failure);
        }

        if (force.IsActive == false)
        {
            return Fail(
                ArmedForceSpatialFailureCode.ForceTerminated,
                "A terminated ArmedForce cannot receive a current physical position update or query.",
                out failure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    private bool TryResolvePosition(
        SpatialReference position,
        out ArmedForceSpatialFailure failure,
        string label = "SpatialReference")
    {
        if (position == null)
        {
            return Fail(
                ArmedForceSpatialFailureCode.InvalidSpatialReference,
                label + " is required.",
                out failure);
        }

        if (spatialAuthorityStore.TryResolve(
                position,
                localTopologyStore,
                out _,
                out SpatialAuthorityFailure authorityFailure) == false)
        {
            return Fail(
                ArmedForceSpatialFailureCode.SpatialReferenceNotRegistered,
                label + " is not registered in the supplied SpatialAuthorityStore.",
                out failure,
                authorityFailure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    private bool CanAdvanceRevision(out ArmedForceSpatialFailure failure)
    {
        if (revision == long.MaxValue)
        {
            return Fail(
                ArmedForceSpatialFailureCode.RevisionOverflow,
                "ArmedForce spatial revision cannot advance further.",
                out failure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    private static bool Fail(
        ArmedForceSpatialFailureCode code,
        string message,
        out ArmedForceSpatialFailure failure,
        SpatialAuthorityFailure authorityFailure = null)
    {
        failure = ArmedForceSpatialFailure.Create(code, message, authorityFailure);
        return false;
    }
}
