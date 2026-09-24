using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum StablePositionReferenceKind
{
    Hex = 0,
    Location = 1,
    Crossing = 2
}

/// <summary>A closed persistent-position reference over stable spatial identities.</summary>
public sealed class StablePositionReference : IEquatable<StablePositionReference>, IComparable<StablePositionReference>
{
    private StablePositionReference(StablePositionReferenceKind kind, HexId hexId, LocationId locationId, CrossingId crossingId)
    {
        Kind = kind;
        HexId = hexId;
        LocationId = locationId;
        CrossingId = crossingId;
    }

    public StablePositionReferenceKind Kind { get; }
    public HexId HexId { get; }
    public LocationId LocationId { get; }
    public CrossingId CrossingId { get; }
    public string StableKey => Kind == StablePositionReferenceKind.Hex ? "hex:" + HexId.Value
        : Kind == StablePositionReferenceKind.Location ? "location:" + LocationId.Value
        : "crossing:" + CrossingId.Value;

    public static StablePositionReference ForHex(HexId id) => new StablePositionReference(StablePositionReferenceKind.Hex, id ?? throw new ArgumentNullException(nameof(id)), null, null);
    public static StablePositionReference ForLocation(LocationId id) => new StablePositionReference(StablePositionReferenceKind.Location, null, id ?? throw new ArgumentNullException(nameof(id)), null);
    public static StablePositionReference ForCrossing(CrossingId id) => new StablePositionReference(StablePositionReferenceKind.Crossing, null, null, id ?? throw new ArgumentNullException(nameof(id)));
    internal SpatialReference ToSpatialReference() => Kind == StablePositionReferenceKind.Hex ? SpatialReference.ForHex(HexId)
        : Kind == StablePositionReferenceKind.Location ? SpatialReference.ForLocation(LocationId)
        : SpatialReference.ForCrossing(CrossingId);

    public bool Equals(StablePositionReference other) => other != null && Kind == other.Kind
        && HexId == other.HexId && LocationId == other.LocationId && CrossingId == other.CrossingId;
    public override bool Equals(object obj) => Equals(obj as StablePositionReference);
    public override int GetHashCode() => (int)Kind * 397 ^ (HexId?.GetHashCode() ?? 0) ^ (LocationId?.GetHashCode() ?? 0) ^ (CrossingId?.GetHashCode() ?? 0);
    public int CompareTo(StablePositionReference other) => other == null ? 1 : StringComparer.Ordinal.Compare(StableKey, other.StableKey);
}

/// <summary>Resolver boundary implemented by the passage authority at integration.</summary>
public interface ISpatialTraversalOptionResolver
{
    bool TryResolveTraversalOption(TraversalOptionRef option, HexBoundaryKey boundary, out string failure);
}

public enum PersonSpatialPositionFailureCode
{
    None = 0, RuntimeFaulted = 1, PersonNotRegistered = 2, InvalidReference = 3,
    SpatialReferenceNotRegistered = 4, InvalidTransit = 5, BoundaryNotRegistered = 6,
    TraversalOptionNotRegistered = 7, PositionMismatch = 8, NoTransit = 9,
    ProgressOutOfRange = 10, ArrivalNotReady = 11, RevisionOverflow = 12,
    PositionAlreadyRegistered = 13
}

public sealed class PersonSpatialPositionFailure
{
    private PersonSpatialPositionFailure(PersonSpatialPositionFailureCode code, string message)
    { Code = code; Message = message ?? string.Empty; }
    public static PersonSpatialPositionFailure None { get; } = new PersonSpatialPositionFailure(PersonSpatialPositionFailureCode.None, string.Empty);
    public PersonSpatialPositionFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PersonSpatialPositionFailureCode.None;
    internal static PersonSpatialPositionFailure Create(PersonSpatialPositionFailureCode code, string message) => new PersonSpatialPositionFailure(code, message);
    public override string ToString() => Code + (Message.Length == 0 ? string.Empty : ": " + Message);
}

public sealed class PersonSpatialPosition
{
    public PersonId PersonId { get; }
    public StablePositionReference Position { get; }
    public TraversalProgress Transit { get; }
    public bool IsInTransit => Transit != null;
    public string StableKey => PersonId.Value;
    internal PersonSpatialPosition(PersonId personId, StablePositionReference position, TraversalProgress transit)
    { PersonId = personId; Position = position; Transit = transit; }
}

/// <summary>Integer progress is an explicit state value; the store defines no speed or clock law.</summary>
public sealed class TraversalProgress
{
    public const int CompleteProgressTicks = 1000;
    public TraversalOptionRef Option { get; }
    public HexBoundaryKey Boundary { get; }
    public HexId FromHexId { get; }
    public HexId ToHexId { get; }
    public StablePositionReference LastFullyReachedReference { get; }
    public int ProgressTicks { get; }

    internal TraversalProgress(TraversalOptionRef option, HexBoundaryKey boundary, HexId from, HexId to, StablePositionReference lastReached, int ticks)
    { Option = option; Boundary = boundary; FromHexId = from; ToHexId = to; LastFullyReachedReference = lastReached; ProgressTicks = ticks; }
}

/// <summary>PersonId-owned factual spatial position independent of loaded NPC state.</summary>
public sealed class PersonSpatialPositionStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly ISpatialTraversalOptionResolver traversalResolver;
    private readonly Dictionary<string, PersonSpatialPosition> positions = new Dictionary<string, PersonSpatialPosition>(StringComparer.Ordinal);
    private long revision;

    public PersonSpatialPositionStore(PersonStore personStore, SpatialAuthorityStore spatialAuthorityStore, ISpatialTraversalOptionResolver traversalResolver)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore));
        this.traversalResolver = traversalResolver ?? throw new ArgumentNullException(nameof(traversalResolver));
    }

    public long Revision => revision;
    public int Count => positions.Count;
    public IReadOnlyList<PersonSpatialPosition> Positions
    {
        get
        {
            List<PersonSpatialPosition> result = new List<PersonSpatialPosition>(positions.Values);
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.StableKey, b.StableKey));
            return new ReadOnlyCollection<PersonSpatialPosition>(result);
        }
    }

    public bool TryGetPosition(PersonId id, out PersonSpatialPosition position)
    { position = null; return id != null && positions.TryGetValue(id.Value, out position); }

    public bool TrySetAt(PersonId id, StablePositionReference position, out PersonSpatialPositionFailure failure)
    {
        if (!CanMutate(out failure) || !TryPerson(id, out failure)) return false;
        if (positions.ContainsKey(id.Value))
            return Fail(PersonSpatialPositionFailureCode.PositionAlreadyRegistered, "At registration is initial-position-only; use validated transit and arrival transitions to change position.", out failure);
        if (!TryResolve(position, out _, out failure)) return false;
        PersonSpatialPosition next = new PersonSpatialPosition(id, position, null);
        if (!CanAdvanceRevision(out failure)) return false;
        positions[id.Value] = next; revision++; failure = PersonSpatialPositionFailure.None; return true;
    }

    /// <summary>Compatibility boundary that explicitly rejects runtime-only D0 SubLocation values.</summary>
    public bool TrySetAt(PersonId id, SpatialReference reference, out PersonSpatialPositionFailure failure)
    {
        if (reference == null || reference.Kind == SpatialReferenceKind.SubLocation)
            return Fail(PersonSpatialPositionFailureCode.InvalidReference, "Persistent Person position does not accept runtime-only SubLocation references.", out failure);
        StablePositionReference stable = reference.Kind == SpatialReferenceKind.Hex ? StablePositionReference.ForHex(reference.HexId)
            : reference.Kind == SpatialReferenceKind.Location ? StablePositionReference.ForLocation(reference.LocationId)
            : reference.Kind == SpatialReferenceKind.Crossing ? StablePositionReference.ForCrossing(reference.CrossingId) : null;
        return TrySetAt(id, stable, out failure);
    }

    public bool TryBeginTransit(PersonId id, TraversalOptionRef option, HexBoundaryKey boundary, HexId from, HexId to, out PersonSpatialPositionFailure failure)
    {
        if (!CanMutate(out failure) || !TryPerson(id, out failure)) return false;
        if (option == null || boundary == null || from == null || to == null || !boundary.Contains(from) || !boundary.Contains(to) || from == to)
            return Fail(PersonSpatialPositionFailureCode.InvalidTransit, "Transit requires a typed option, boundary, and distinct directed endpoints.", out failure);
        if (!spatialAuthorityStore.TryGetGeometricBoundary(from, to, out _, out SpatialAuthorityFailure boundaryFailure))
            return Fail(PersonSpatialPositionFailureCode.BoundaryNotRegistered, boundaryFailure?.Message ?? "Transit boundary is not a registered geometric boundary.", out failure);
        if (!spatialAuthorityStore.TryGetGeometricBoundary(boundary.FirstHexId, boundary.SecondHexId, out _, out _) || !boundary.Equals(new HexBoundaryKey(from, to)))
            return Fail(PersonSpatialPositionFailureCode.BoundaryNotRegistered, "The supplied boundary does not match the directed Hex endpoints.", out failure);
        if (!traversalResolver.TryResolveTraversalOption(option, boundary, out string resolverFailure))
            return Fail(PersonSpatialPositionFailureCode.TraversalOptionNotRegistered, resolverFailure, out failure);
        if (!TryGetPosition(id, out PersonSpatialPosition current) || current.IsInTransit)
            return Fail(PersonSpatialPositionFailureCode.PositionMismatch, "Person must have an At position before departure.", out failure);
        if (current.Position.Kind != StablePositionReferenceKind.Hex)
            return Fail(PersonSpatialPositionFailureCode.PositionMismatch, "Regional transit requires a Hex At position; Location and Crossing require an explicit local connector capability.", out failure);
        if (!TryResolve(current.Position, out SpatialResolution resolution, out failure)) return false;
        if (resolution.Hex == null || resolution.Hex.Id != from)
            return Fail(PersonSpatialPositionFailureCode.PositionMismatch, "Current position does not resolve to the directed origin Hex.", out failure);
        if (!CanAdvanceRevision(out failure)) return false;
        TraversalProgress transit = new TraversalProgress(option, boundary, from, to, current.Position, 0);
        positions[id.Value] = new PersonSpatialPosition(id, null, transit); revision++; failure = PersonSpatialPositionFailure.None; return true;
    }

    public bool TryAdvanceTransit(PersonId id, int progressTicks, out PersonSpatialPositionFailure failure)
    {
        if (!CanMutate(out failure) || !TryPerson(id, out failure)) return false;
        if (!TryGetPosition(id, out PersonSpatialPosition current) || !current.IsInTransit)
            return Fail(PersonSpatialPositionFailureCode.NoTransit, "Person has no active transit position.", out failure);
        if (progressTicks <= 0 || current.Transit.ProgressTicks > TraversalProgress.CompleteProgressTicks - progressTicks)
            return Fail(PersonSpatialPositionFailureCode.ProgressOutOfRange, "Explicit progress increment must be positive and remain within the supported fixed range.", out failure);
        if (!TryValidateTransit(current.Transit, out failure)) return false;
        if (!CanAdvanceRevision(out failure)) return false;
        TraversalProgress old = current.Transit;
        TraversalProgress advanced = new TraversalProgress(old.Option, old.Boundary, old.FromHexId, old.ToHexId, old.LastFullyReachedReference, old.ProgressTicks + progressTicks);
        positions[id.Value] = new PersonSpatialPosition(id, null, advanced); revision++; failure = PersonSpatialPositionFailure.None; return true;
    }

    public bool TryArrive(PersonId id, StablePositionReference destination, out PersonSpatialPositionFailure failure)
    {
        if (!CanMutate(out failure) || !TryPerson(id, out failure)) return false;
        if (!TryGetPosition(id, out PersonSpatialPosition current) || !current.IsInTransit)
            return Fail(PersonSpatialPositionFailureCode.NoTransit, "Person has no active transit position.", out failure);
        if (current.Transit.ProgressTicks != TraversalProgress.CompleteProgressTicks)
            return Fail(PersonSpatialPositionFailureCode.ArrivalNotReady, "Transit must reach its explicit completion tick before arrival.", out failure);
        if (destination == null || destination.Kind != StablePositionReferenceKind.Hex)
            return Fail(PersonSpatialPositionFailureCode.PositionMismatch, "Regional transit arrival requires a Hex reference; Location and Crossing require an explicit local connector capability.", out failure);
        if (!TryValidateTransit(current.Transit, out failure) || !TryResolve(destination, out SpatialResolution resolution, out failure)) return false;
        if (resolution.Hex == null || resolution.Hex.Id != current.Transit.ToHexId)
            return Fail(PersonSpatialPositionFailureCode.PositionMismatch, "Arrival reference does not resolve to the directed destination Hex.", out failure);
        if (!CanAdvanceRevision(out failure)) return false;
        positions[id.Value] = new PersonSpatialPosition(id, destination, null); revision++; failure = PersonSpatialPositionFailure.None; return true;
    }

    public PersonSpatialPositionInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        foreach (PersonSpatialPosition value in Positions)
        {
            if (!personStore.TryGet(value.PersonId, out _)) violations.Add("Person spatial position references an unregistered PersonId: " + value.PersonId.Value + ".");
            if (value.IsInTransit) { if (!TryValidateTransit(value.Transit, out PersonSpatialPositionFailure failure)) violations.Add("Transit position is invalid for " + value.PersonId.Value + ": " + failure + "."); }
            else if (!TryResolve(value.Position, out _, out PersonSpatialPositionFailure failure)) violations.Add("At position is invalid for " + value.PersonId.Value + ": " + failure + ".");
        }
        return new PersonSpatialPositionInvariantReport(violations);
    }

    internal PersonSpatialPositionStore Clone(PersonStore targetPersons, SpatialAuthorityStore targetSpatial, ISpatialTraversalOptionResolver targetResolver)
    {
        PersonSpatialPositionStore copy = new PersonSpatialPositionStore(targetPersons, targetSpatial, targetResolver);
        foreach (PersonSpatialPosition value in Positions)
        {
            if (!targetPersons.TryGet(value.PersonId, out _)) throw new ArgumentException("Position PersonId is absent from target PersonStore.", nameof(targetPersons));
            if (value.IsInTransit)
            {
                if (!copy.TryValidateTransit(value.Transit, out _)) throw new ArgumentException("Transit references are absent from target authorities.", nameof(targetSpatial));
                copy.positions.Add(value.PersonId.Value, new PersonSpatialPosition(new PersonId(value.PersonId.Value), null,
                    new TraversalProgress(value.Transit.Option, value.Transit.Boundary, new HexId(value.Transit.FromHexId.Value), new HexId(value.Transit.ToHexId.Value), copy.CloneReference(value.Transit.LastFullyReachedReference), value.Transit.ProgressTicks)));
            }
            else
            {
                if (!copy.TryResolve(value.Position, out _, out _)) throw new ArgumentException("Position reference is absent from target spatial authority.", nameof(targetSpatial));
                copy.positions.Add(value.PersonId.Value, new PersonSpatialPosition(new PersonId(value.PersonId.Value), copy.CloneReference(value.Position), null));
            }
        }
        copy.revision = revision; return copy;
    }

    private bool TryValidateTransit(TraversalProgress transit, out PersonSpatialPositionFailure failure)
    {
        if (transit == null || transit.ProgressTicks < 0 || transit.ProgressTicks > TraversalProgress.CompleteProgressTicks
            || transit.Option == null || transit.Boundary == null || transit.FromHexId == null || transit.ToHexId == null
            || transit.LastFullyReachedReference == null || transit.LastFullyReachedReference.Kind != StablePositionReferenceKind.Hex
            || !transit.Boundary.Equals(new HexBoundaryKey(transit.FromHexId, transit.ToHexId)))
            return Fail(PersonSpatialPositionFailureCode.InvalidTransit, "Transit state is structurally invalid.", out failure);
        if (!TryResolve(transit.LastFullyReachedReference, out SpatialResolution last, out failure)) return false;
        if (last.Hex == null || last.Hex.Id != transit.FromHexId) return Fail(PersonSpatialPositionFailureCode.PositionMismatch, "Last fully reached reference must resolve to the directed origin Hex.", out failure);
        if (!spatialAuthorityStore.TryGetGeometricBoundary(transit.FromHexId, transit.ToHexId, out _, out SpatialAuthorityFailure boundaryFailure))
            return Fail(PersonSpatialPositionFailureCode.BoundaryNotRegistered, boundaryFailure?.Message, out failure);
        if (!traversalResolver.TryResolveTraversalOption(transit.Option, transit.Boundary, out string message))
            return Fail(PersonSpatialPositionFailureCode.TraversalOptionNotRegistered, message, out failure);
        failure = PersonSpatialPositionFailure.None; return true;
    }

    private StablePositionReference CloneReference(StablePositionReference value) => value.Kind == StablePositionReferenceKind.Hex ? StablePositionReference.ForHex(new HexId(value.HexId.Value))
        : value.Kind == StablePositionReferenceKind.Location ? StablePositionReference.ForLocation(new LocationId(value.LocationId.Value)) : StablePositionReference.ForCrossing(new CrossingId(value.CrossingId.Value));
    private bool TryResolve(StablePositionReference reference, out SpatialResolution resolution, out PersonSpatialPositionFailure failure)
    {
        resolution = null;
        if (reference == null || !Enum.IsDefined(typeof(StablePositionReferenceKind), reference.Kind)) return Fail(PersonSpatialPositionFailureCode.InvalidReference, "Stable position reference is required.", out failure);
        if (!spatialAuthorityStore.TryResolve(reference.ToSpatialReference(), null, out resolution, out SpatialAuthorityFailure authorityFailure))
            return Fail(PersonSpatialPositionFailureCode.SpatialReferenceNotRegistered, authorityFailure?.Message, out failure);
        failure = PersonSpatialPositionFailure.None; return true;
    }
    private bool TryPerson(PersonId id, out PersonSpatialPositionFailure failure)
    { if (id == null || !personStore.TryGet(id, out _)) return Fail(PersonSpatialPositionFailureCode.PersonNotRegistered, "PersonId is not registered.", out failure); failure = PersonSpatialPositionFailure.None; return true; }
    private bool CanMutate(out PersonSpatialPositionFailure failure)
    { if (!mutationGuardBinding.CanMutate) return Fail(PersonSpatialPositionFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure); failure = PersonSpatialPositionFailure.None; return true; }
    private bool CanAdvanceRevision(out PersonSpatialPositionFailure failure)
    { if (revision == long.MaxValue) return Fail(PersonSpatialPositionFailureCode.RevisionOverflow, "Person spatial position revision cannot advance.", out failure); failure = PersonSpatialPositionFailure.None; return true; }
    private static bool Fail(PersonSpatialPositionFailureCode code, string message, out PersonSpatialPositionFailure failure)
    { failure = PersonSpatialPositionFailure.Create(code, message); return false; }
    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

public sealed class PersonSpatialPositionInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;
    internal PersonSpatialPositionInvariantReport(IEnumerable<string> values)
    { List<string> result = values == null ? new List<string>() : new List<string>(values); result.Sort(StringComparer.Ordinal); Violations = new ReadOnlyCollection<string>(result); }
}
