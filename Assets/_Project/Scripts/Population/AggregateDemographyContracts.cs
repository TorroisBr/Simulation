using System;

/// <summary>
/// Structured reasons why an aggregate demographic proposal or application was rejected.
/// </summary>
public enum AggregateDemographyFailure
{
    None = 0,
    InvalidSettlement = 1,
    InvalidProvider = 2,
    InvalidRepresentedResidentFloor = 3,
    NegativeChange = 4,
    WouldUnderflow = 5,
    WouldOverflow = 6,
    WouldViolateRepresentedResidentFloor = 7,
    RevisionOverflow = 8,
    StaleState = 9,
    InvalidTransition = 10,
    RuntimeFaulted = 11
}

/// <summary>
/// Aggregate-only births and deaths proposed for one settlement transition.
/// Negative values are retained so the system boundary can reject malformed provider output.
/// </summary>
public readonly struct AggregateDemographyChange : IEquatable<AggregateDemographyChange>
{
    public int Births { get; }
    public int Deaths { get; }
    public long NetChange => (long)Births - Deaths;
    public bool HasNegativeChange => Births < 0 || Deaths < 0;

    public AggregateDemographyChange(int births, int deaths)
    {
        Births = births;
        Deaths = deaths;
    }

    public bool Equals(AggregateDemographyChange other)
    {
        return Births == other.Births && Deaths == other.Deaths;
    }

    public override bool Equals(object obj)
    {
        return obj is AggregateDemographyChange other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Births * 397) ^ Deaths;
        }
    }

    public static bool operator ==(AggregateDemographyChange left, AggregateDemographyChange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AggregateDemographyChange left, AggregateDemographyChange right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Immutable aggregate snapshot supplied to demographic policy. The represented-resident
/// floor is provided by the caller; this contract never discovers Persons or NPCs itself.
/// </summary>
public readonly struct AggregateDemographyContext : IEquatable<AggregateDemographyContext>
{
    public string SettlementRuntimeId { get; }
    public long CurrentAbsoluteDay { get; }
    public long PopulationRevision { get; }
    public int CurrentPopulation { get; }
    public int RepresentedResidentFloor { get; }
    public int AggregateOnlyPopulation => CurrentPopulation - RepresentedResidentFloor;

    internal AggregateDemographyContext(
        string settlementRuntimeId,
        long populationRevision,
        int currentPopulation,
        int representedResidentFloor,
        long currentAbsoluteDay = 0L)
    {
        SettlementRuntimeId = settlementRuntimeId;
        CurrentAbsoluteDay = currentAbsoluteDay;
        PopulationRevision = populationRevision;
        CurrentPopulation = currentPopulation;
        RepresentedResidentFloor = representedResidentFloor;
    }

    public bool Equals(AggregateDemographyContext other)
    {
        return string.Equals(SettlementRuntimeId, other.SettlementRuntimeId, StringComparison.Ordinal)
            && CurrentAbsoluteDay == other.CurrentAbsoluteDay
            && PopulationRevision == other.PopulationRevision
            && CurrentPopulation == other.CurrentPopulation
            && RepresentedResidentFloor == other.RepresentedResidentFloor;
    }

    public override bool Equals(object obj)
    {
        return obj is AggregateDemographyContext other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(SettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ CurrentAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ PopulationRevision.GetHashCode();
            hash = (hash * 397) ^ CurrentPopulation;
            return (hash * 397) ^ RepresentedResidentFloor;
        }
    }

    public static bool operator ==(AggregateDemographyContext left, AggregateDemographyContext right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AggregateDemographyContext left, AggregateDemographyContext right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Explicit policy boundary for aggregate demographic change. Implementations must return
/// the same change for the same context unless they deliberately own an injected random source.
/// </summary>
public interface IAggregateDemographyProvider
{
    AggregateDemographyChange GetChange(AggregateDemographyContext context);
}

/// <summary>
/// Explicit randomness boundary for stochastic aggregate-demography providers. Providers,
/// rather than the aggregate state system, own and consume this dependency.
/// </summary>
public interface IAggregateDemographyRandomSource
{
    double NextUnitInterval();
}
