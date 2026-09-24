using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

public enum SpatialSubjectKind
{
    Hex = 0,
    Location = 1,
    Crossing = 2,
    TraversalOption = 3,
    RouteEstimate = 4
}

/// <summary>A directed, typed route segment built from actor-known structure.</summary>
public sealed class SpatialRouteSegment : IEquatable<SpatialRouteSegment>, IComparable<SpatialRouteSegment>
{
    public HexBoundaryKey Boundary { get; }
    public HexId FromHexId { get; }
    public HexId ToHexId { get; }
    public TraversalOptionRef Option { get; }
    public string StableKey { get; }

    public SpatialRouteSegment(HexBoundaryKey boundary, HexId fromHexId, HexId toHexId, TraversalOptionRef option)
    {
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        FromHexId = fromHexId ?? throw new ArgumentNullException(nameof(fromHexId));
        ToHexId = toHexId ?? throw new ArgumentNullException(nameof(toHexId));
        Option = option ?? throw new ArgumentNullException(nameof(option));
        if (FromHexId == ToHexId || !Boundary.Equals(new HexBoundaryKey(FromHexId, ToHexId)))
        {
            throw new ArgumentException("A route segment requires distinct directed endpoints matching its canonical boundary.");
        }

        StableKey = SpatialStableKey.Encode(
            "segment-v1", Boundary.FirstHexId.Value, Boundary.SecondHexId.Value,
            FromHexId.Value, ToHexId.Value, SpatialStableKey.OptionKey(Option));
    }

    public bool Equals(SpatialRouteSegment other) => other != null
        && Boundary.Equals(other.Boundary) && FromHexId == other.FromHexId
        && ToHexId == other.ToHexId && Option.Equals(other.Option);
    public override bool Equals(object obj) => Equals(obj as SpatialRouteSegment);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);
    public int CompareTo(SpatialRouteSegment other) => other == null ? 1 : StringComparer.Ordinal.Compare(StableKey, other.StableKey);
}

/// <summary>Stable semantic identity for a generated route candidate.</summary>
public sealed class SpatialRouteCandidateId : IEquatable<SpatialRouteCandidateId>, IComparable<SpatialRouteCandidateId>
{
    public string StableKey { get; }

    internal SpatialRouteCandidateId(string stableKey)
    {
        if (string.IsNullOrWhiteSpace(stableKey)) throw new ArgumentException("A route candidate requires a stable identity.", nameof(stableKey));
        StableKey = stableKey;
    }

    public bool Equals(SpatialRouteCandidateId other) => other != null
        && string.Equals(StableKey, other.StableKey, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SpatialRouteCandidateId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);
    public int CompareTo(SpatialRouteCandidateId other) => other == null ? 1 : StringComparer.Ordinal.Compare(StableKey, other.StableKey);
    public override string ToString() => StableKey;
}

/// <summary>
/// Closed typed identity for a subject of spatial Knowledge. Values contain
/// semantic IDs only; no RuntimeId or display name participates in identity.
/// </summary>
public sealed class SpatialSubject : IEquatable<SpatialSubject>, IComparable<SpatialSubject>
{
    public SpatialSubjectKind Kind { get; }
    public HexId HexId { get; }
    public LocationId LocationId { get; }
    public CrossingId CrossingId { get; }
    public SpatialRouteSegment RouteSegment { get; }
    public SpatialRouteCandidateId CandidateId { get; }
    public string EstimateMetricId { get; }
    public string StableKey { get; }

    private SpatialSubject(
        SpatialSubjectKind kind,
        HexId hexId,
        LocationId locationId,
        CrossingId crossingId,
        SpatialRouteSegment routeSegment,
        SpatialRouteCandidateId candidateId,
        string estimateMetricId)
    {
        Kind = kind;
        HexId = hexId;
        LocationId = locationId;
        CrossingId = crossingId;
        RouteSegment = routeSegment;
        CandidateId = candidateId;
        EstimateMetricId = estimateMetricId;
        switch (kind)
        {
            case SpatialSubjectKind.Hex:
                StableKey = SpatialStableKey.Encode("hex", hexId.Value);
                break;
            case SpatialSubjectKind.Location:
                StableKey = SpatialStableKey.Encode("location", locationId.Value);
                break;
            case SpatialSubjectKind.Crossing:
                StableKey = SpatialStableKey.Encode("crossing", crossingId.Value);
                break;
            case SpatialSubjectKind.TraversalOption:
                StableKey = SpatialStableKey.Encode("traversal-option", routeSegment.StableKey);
                break;
            case SpatialSubjectKind.RouteEstimate:
                StableKey = SpatialStableKey.Encode("route-estimate", candidateId.StableKey, estimateMetricId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public static SpatialSubject ForHex(HexId id) => new SpatialSubject(SpatialSubjectKind.Hex, id ?? throw new ArgumentNullException(nameof(id)), null, null, null, null, null);
    public static SpatialSubject ForLocation(LocationId id) => new SpatialSubject(SpatialSubjectKind.Location, null, id ?? throw new ArgumentNullException(nameof(id)), null, null, null, null);
    public static SpatialSubject ForCrossing(CrossingId id) => new SpatialSubject(SpatialSubjectKind.Crossing, null, null, id ?? throw new ArgumentNullException(nameof(id)), null, null, null);
    public static SpatialSubject ForTraversalOption(SpatialRouteSegment segment) => new SpatialSubject(SpatialSubjectKind.TraversalOption, null, null, null, segment ?? throw new ArgumentNullException(nameof(segment)), null, null);
    public static SpatialSubject ForRouteEstimate(SpatialRouteCandidateId candidateId, string metricId)
    {
        if (candidateId == null) throw new ArgumentNullException(nameof(candidateId));
        if (string.IsNullOrWhiteSpace(metricId)) throw new ArgumentException("An estimate subject requires a stable metric identity.", nameof(metricId));
        return new SpatialSubject(SpatialSubjectKind.RouteEstimate, null, null, null, null, candidateId, metricId);
    }

    public bool Equals(SpatialSubject other) => other != null
        && string.Equals(StableKey, other.StableKey, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SpatialSubject);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);
    public int CompareTo(SpatialSubject other) => other == null ? 1 : StringComparer.Ordinal.Compare(StableKey, other.StableKey);
    public override string ToString() => StableKey;
}

public enum SpatialObservationValueKind
{
    EntityPresence = 0,
    RouteOptionBelief = 1,
    RouteEstimate = 2
}

public enum SpatialEntityBelief
{
    KnownPresent = 0,
    KnownAbsent = 1
}

public enum SpatialRouteOptionBelief
{
    Unknown = 0,
    KnownAvailable = 1,
    KnownUnavailable = 2
}

/// <summary>Typed immutable value carried by one spatial observation.</summary>
public sealed class SpatialObservationValue : IEquatable<SpatialObservationValue>
{
    public SpatialObservationValueKind Kind { get; }
    public SpatialEntityBelief EntityBelief { get; }
    public SpatialRouteOptionBelief RouteOptionBelief { get; }
    public decimal Estimate { get; }
    public string EstimateUnit { get; }
    public string StableKey { get; }

    private SpatialObservationValue(
        SpatialObservationValueKind kind,
        SpatialEntityBelief entityBelief,
        SpatialRouteOptionBelief routeOptionBelief,
        decimal estimate,
        string estimateUnit)
    {
        Kind = kind;
        EntityBelief = entityBelief;
        RouteOptionBelief = routeOptionBelief;
        Estimate = estimate;
        EstimateUnit = estimateUnit;
        switch (kind)
        {
            case SpatialObservationValueKind.EntityPresence:
                StableKey = SpatialStableKey.Encode("entity", ((int)entityBelief).ToString(CultureInfo.InvariantCulture));
                break;
            case SpatialObservationValueKind.RouteOptionBelief:
                StableKey = SpatialStableKey.Encode("route-option", ((int)routeOptionBelief).ToString(CultureInfo.InvariantCulture));
                break;
            case SpatialObservationValueKind.RouteEstimate:
                StableKey = SpatialStableKey.Encode("estimate", estimate.ToString(CultureInfo.InvariantCulture), estimateUnit);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public static SpatialObservationValue ForEntityBelief(SpatialEntityBelief belief)
    {
        if (!Enum.IsDefined(typeof(SpatialEntityBelief), belief)) throw new ArgumentOutOfRangeException(nameof(belief));
        return new SpatialObservationValue(SpatialObservationValueKind.EntityPresence, belief, default(SpatialRouteOptionBelief), 0m, null);
    }

    public static SpatialObservationValue ForRouteOptionBelief(SpatialRouteOptionBelief belief)
    {
        if (!Enum.IsDefined(typeof(SpatialRouteOptionBelief), belief)) throw new ArgumentOutOfRangeException(nameof(belief));
        return new SpatialObservationValue(SpatialObservationValueKind.RouteOptionBelief, default(SpatialEntityBelief), belief, 0m, null);
    }

    public static SpatialObservationValue ForEstimate(decimal estimate, string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("An explicit route estimate requires a stable unit identity.", nameof(unit));
        return new SpatialObservationValue(SpatialObservationValueKind.RouteEstimate, default(SpatialEntityBelief), default(SpatialRouteOptionBelief), estimate, unit);
    }

    public bool Equals(SpatialObservationValue other) => other != null
        && Kind == other.Kind && EntityBelief == other.EntityBelief
        && RouteOptionBelief == other.RouteOptionBelief && Estimate == other.Estimate
        && string.Equals(EstimateUnit, other.EstimateUnit, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SpatialObservationValue);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);
}

public enum SpatialObservationSourceKind
{
    DirectObservation = 0,
    InitialScenarioKnowledge = 1,
    SharedByPerson = 2,
    SharedByInstitution = 3,
    ExternalReport = 4,
    ExecutionOutcome = 5
}

/// <summary>Stable source and origin identity for an observation, independent of receipt copies.</summary>
public sealed class SpatialObservationProvenance : IEquatable<SpatialObservationProvenance>
{
    public SpatialObservationSourceKind SourceKind { get; }
    public string SourceIdentity { get; }
    public string OriginIdentity { get; }
    public PersonId TransmittingPersonId { get; }
    public string StableKey { get; }

    public SpatialObservationProvenance(
        SpatialObservationSourceKind sourceKind,
        string sourceIdentity,
        string originIdentity,
        PersonId transmittingPersonId = null)
    {
        if (!Enum.IsDefined(typeof(SpatialObservationSourceKind), sourceKind)) throw new ArgumentOutOfRangeException(nameof(sourceKind));
        if (string.IsNullOrWhiteSpace(sourceIdentity)) throw new ArgumentException("Spatial observation source requires a stable identity.", nameof(sourceIdentity));
        if (string.IsNullOrWhiteSpace(originIdentity)) throw new ArgumentException("Spatial observation provenance requires a stable origin identity.", nameof(originIdentity));
        if (sourceKind == SpatialObservationSourceKind.SharedByPerson && transmittingPersonId == null)
            throw new ArgumentException("SharedByPerson provenance requires a stable transmitting PersonId.", nameof(transmittingPersonId));

        SourceKind = sourceKind;
        SourceIdentity = sourceIdentity;
        OriginIdentity = originIdentity;
        TransmittingPersonId = transmittingPersonId;
        StableKey = SpatialStableKey.Encode(
            ((int)sourceKind).ToString(CultureInfo.InvariantCulture), sourceIdentity,
            originIdentity, transmittingPersonId?.Value ?? string.Empty);
    }

    public bool Equals(SpatialObservationProvenance other) => other != null
        && SourceKind == other.SourceKind
        && string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal)
        && string.Equals(OriginIdentity, other.OriginIdentity, StringComparison.Ordinal)
        && TransmittingPersonId == other.TransmittingPersonId;
    public override bool Equals(object obj) => Equals(obj as SpatialObservationProvenance);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);
}

/// <summary>
/// One actor-owned report. Observation and receipt times, confidence, precision,
/// source, and origin remain explicit; it is never synchronized from World Truth.
/// </summary>
public sealed class SpatialObservation
{
    public SpatialSubject Subject { get; }
    public SpatialObservationValue Value { get; }
    public SpatialObservationProvenance Provenance { get; }
    public long ObservedDay { get; }
    public long ReceivedDay { get; }
    public int ConfidencePermille { get; }
    public string PrecisionIdentity { get; }
    public string StableIdentity { get; }

    public SpatialObservation(
        SpatialSubject subject,
        SpatialObservationValue value,
        SpatialObservationProvenance provenance,
        long observedDay,
        long receivedDay,
        int confidencePermille,
        string precisionIdentity)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        if (observedDay < 0L) throw new ArgumentOutOfRangeException(nameof(observedDay));
        if (receivedDay < observedDay) throw new ArgumentOutOfRangeException(nameof(receivedDay), "Receipt day cannot precede observation day.");
        if (confidencePermille < 0 || confidencePermille > 1000) throw new ArgumentOutOfRangeException(nameof(confidencePermille));
        if (string.IsNullOrWhiteSpace(precisionIdentity)) throw new ArgumentException("Spatial observation precision requires a stable identity.", nameof(precisionIdentity));
        ValidateSubjectValue(subject, value);

        ObservedDay = observedDay;
        ReceivedDay = receivedDay;
        ConfidencePermille = confidencePermille;
        PrecisionIdentity = precisionIdentity;
        StableIdentity = SpatialStableKey.Encode(
            subject.StableKey, provenance.StableKey,
            observedDay.ToString(CultureInfo.InvariantCulture),
            receivedDay.ToString(CultureInfo.InvariantCulture), value.StableKey,
            confidencePermille.ToString(CultureInfo.InvariantCulture), precisionIdentity);
    }

    internal bool HasSameOriginEvidence(SpatialObservation other) => other != null
        && Subject.Equals(other.Subject)
        && Provenance.Equals(other.Provenance)
        && ObservedDay == other.ObservedDay
        && ConfidencePermille == other.ConfidencePermille
        && string.Equals(PrecisionIdentity, other.PrecisionIdentity, StringComparison.Ordinal)
        && Value.Equals(other.Value);

    internal SpatialObservation Clone()
    {
        SpatialSubject subject = CloneSubject(Subject);
        SpatialObservationProvenance provenance = new SpatialObservationProvenance(
            Provenance.SourceKind, Provenance.SourceIdentity, Provenance.OriginIdentity,
            Provenance.TransmittingPersonId == null ? null : new PersonId(Provenance.TransmittingPersonId.Value));
        SpatialObservationValue value = Value.Kind == SpatialObservationValueKind.RouteEstimate
            ? SpatialObservationValue.ForEstimate(Value.Estimate, Value.EstimateUnit)
            : Value.Kind == SpatialObservationValueKind.RouteOptionBelief
                ? SpatialObservationValue.ForRouteOptionBelief(Value.RouteOptionBelief)
                : SpatialObservationValue.ForEntityBelief(Value.EntityBelief);
        return new SpatialObservation(subject, value, provenance, ObservedDay, ReceivedDay, ConfidencePermille, PrecisionIdentity);
    }

    private static void ValidateSubjectValue(SpatialSubject subject, SpatialObservationValue value)
    {
        bool valid = subject.Kind == SpatialSubjectKind.TraversalOption
            ? value.Kind == SpatialObservationValueKind.RouteOptionBelief
            : subject.Kind == SpatialSubjectKind.RouteEstimate
                ? value.Kind == SpatialObservationValueKind.RouteEstimate
                : value.Kind == SpatialObservationValueKind.EntityPresence;
        if (!valid) throw new ArgumentException("Spatial observation value kind does not match its typed subject.", nameof(value));
    }

    private static SpatialSubject CloneSubject(SpatialSubject value)
    {
        switch (value.Kind)
        {
            case SpatialSubjectKind.Hex: return SpatialSubject.ForHex(new HexId(value.HexId.Value));
            case SpatialSubjectKind.Location: return SpatialSubject.ForLocation(new LocationId(value.LocationId.Value));
            case SpatialSubjectKind.Crossing: return SpatialSubject.ForCrossing(new CrossingId(value.CrossingId.Value));
            case SpatialSubjectKind.TraversalOption:
                return SpatialSubject.ForTraversalOption(new SpatialRouteSegment(
                    new HexBoundaryKey(new HexId(value.RouteSegment.Boundary.FirstHexId.Value), new HexId(value.RouteSegment.Boundary.SecondHexId.Value)),
                    new HexId(value.RouteSegment.FromHexId.Value), new HexId(value.RouteSegment.ToHexId.Value), value.RouteSegment.Option));
            case SpatialSubjectKind.RouteEstimate:
                return SpatialSubject.ForRouteEstimate(new SpatialRouteCandidateId(value.CandidateId.StableKey), value.EstimateMetricId);
            default: throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}

public sealed class SpatialResolvedObservation
{
    public SpatialObservation Observation { get; }
    public bool IsKnown { get; }
    public bool IsStale { get; }
    internal SpatialResolvedObservation(SpatialObservation observation, bool isKnown, bool isStale)
    { Observation = observation; IsKnown = isKnown; IsStale = isStale; }
}

public sealed class SpatialKnowledgeBasis
{
    private readonly ReadOnlyCollection<string> observationIdentities;
    public PersonId ActorPersonId { get; }
    public long ActorKnowledgeRevision { get; }
    public IReadOnlyList<string> ObservationIdentities => observationIdentities;
    public string Fingerprint { get; }

    internal SpatialKnowledgeBasis(PersonId actor, long actorRevision, IEnumerable<string> identities)
    {
        ActorPersonId = actor ?? throw new ArgumentNullException(nameof(actor));
        if (actorRevision < 0L) throw new ArgumentOutOfRangeException(nameof(actorRevision));
        ActorKnowledgeRevision = actorRevision;
        List<string> ordered = identities == null ? new List<string>() : new List<string>(identities);
        ordered.Sort(StringComparer.Ordinal);
        for (int i = ordered.Count - 1; i > 0; i--)
            if (string.Equals(ordered[i], ordered[i - 1], StringComparison.Ordinal)) ordered.RemoveAt(i);
        observationIdentities = new ReadOnlyCollection<string>(ordered);
        Fingerprint = SpatialStableKey.Encode(ordered.ToArray());
    }
}

internal static class SpatialStableKey
{
    internal static string Encode(params string[] parts)
    {
        StringBuilder builder = new StringBuilder();
        foreach (string part in parts ?? Array.Empty<string>())
        {
            string value = part ?? string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }
        return builder.ToString();
    }

    internal static string OptionKey(TraversalOptionRef option)
    {
        switch (option.Kind)
        {
            case TraversalOptionKind.Connection: return Encode("connection", option.ConnectionId.Value);
            case TraversalOptionKind.Crossing: return Encode("crossing", option.CrossingId.Value);
            case TraversalOptionKind.WildernessRule: return Encode("wilderness", option.RuleIdentity, option.RuleVersion);
            default: throw new ArgumentOutOfRangeException(nameof(option));
        }
    }
}

public enum SpatialKnowledgeFailureCode
{
    None = 0,
    RuntimeFaulted = 1,
    ActorNotRegistered = 2,
    SourcePersonNotRegistered = 3,
    InvalidObservation = 4,
    FutureObservation = 5,
    ConflictingProvenance = 6,
    RevisionOverflow = 7
}

public sealed class SpatialKnowledgeFailure
{
    private SpatialKnowledgeFailure(SpatialKnowledgeFailureCode code, string message)
    { Code = code; Message = message ?? string.Empty; }
    public static SpatialKnowledgeFailure None { get; } = new SpatialKnowledgeFailure(SpatialKnowledgeFailureCode.None, string.Empty);
    public SpatialKnowledgeFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != SpatialKnowledgeFailureCode.None;
    internal static SpatialKnowledgeFailure Create(SpatialKnowledgeFailureCode code, string message) => new SpatialKnowledgeFailure(code, message);
    public override string ToString() => Code + (Message.Length == 0 ? string.Empty : ": " + Message);
}
