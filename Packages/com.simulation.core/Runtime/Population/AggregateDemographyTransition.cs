using System;

/// <summary>
/// Immutable facts for one aggregate-only birth/death transition.
/// </summary>
public sealed class AggregateDemographyTransition : IEquatable<AggregateDemographyTransition>
{
    public string SettlementRuntimeId { get; }
    public long ExpectedPopulationRevision { get; }
    public int PopulationBefore { get; }
    public int RepresentedResidentFloor { get; }
    public int Births { get; }
    public int Deaths { get; }
    public long NetChange { get; }
    public int PopulationAfter { get; }

    internal AggregateDemographyTransition(
        string settlementRuntimeId,
        long expectedPopulationRevision,
        int populationBefore,
        int representedResidentFloor,
        AggregateDemographyChange change,
        int populationAfter)
    {
        SettlementRuntimeId = settlementRuntimeId;
        ExpectedPopulationRevision = expectedPopulationRevision;
        PopulationBefore = populationBefore;
        RepresentedResidentFloor = representedResidentFloor;
        Births = change.Births;
        Deaths = change.Deaths;
        NetChange = change.NetChange;
        PopulationAfter = populationAfter;
    }

    public bool Equals(AggregateDemographyTransition other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return string.Equals(SettlementRuntimeId, other.SettlementRuntimeId, StringComparison.Ordinal)
            && ExpectedPopulationRevision == other.ExpectedPopulationRevision
            && PopulationBefore == other.PopulationBefore
            && RepresentedResidentFloor == other.RepresentedResidentFloor
            && Births == other.Births
            && Deaths == other.Deaths
            && NetChange == other.NetChange
            && PopulationAfter == other.PopulationAfter;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as AggregateDemographyTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(SettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ ExpectedPopulationRevision.GetHashCode();
            hash = (hash * 397) ^ PopulationBefore;
            hash = (hash * 397) ^ RepresentedResidentFloor;
            hash = (hash * 397) ^ Births;
            hash = (hash * 397) ^ Deaths;
            hash = (hash * 397) ^ NetChange.GetHashCode();
            return (hash * 397) ^ PopulationAfter;
        }
    }

    public static bool operator ==(AggregateDemographyTransition left, AggregateDemographyTransition right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(AggregateDemographyTransition left, AggregateDemographyTransition right)
    {
        return !(left == right);
    }
}
