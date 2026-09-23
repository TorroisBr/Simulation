using System;

/// <summary>
/// Structured reasons why a population proposal or application was rejected.
/// </summary>
public enum PopulationTransitionFailure
{
    None = 0,
    InvalidSettlement = 1,
    NegativeChange = 2,
    WouldUnderflow = 3,
    WouldOverflow = 4,
    StaleState = 5,
    InvalidTransition = 6,
    RevisionOverflow = 7,
    RuntimeFaulted = 8,
    RuntimeOwnershipMismatch = 9
}

/// <summary>
/// Immutable, value-based snapshot of one proposed aggregate population transition.
/// </summary>
public sealed class SettlementPopulationTransition : IEquatable<SettlementPopulationTransition>
{
    public string SettlementRuntimeId { get; }
    public long ExpectedRevision { get; }
    public int PopulationBefore { get; }
    public int Births { get; }
    public int Deaths { get; }
    public int Immigrations { get; }
    public int Emigrations { get; }
    public long NetChange { get; }
    public int PopulationAfter { get; }

    internal SettlementPopulationTransition(
        string settlementRuntimeId,
        long expectedRevision,
        int populationBefore,
        PopulationChangeSet changes,
        long netChange,
        int populationAfter)
    {
        SettlementRuntimeId = settlementRuntimeId;
        ExpectedRevision = expectedRevision;
        PopulationBefore = populationBefore;
        Births = changes.Births;
        Deaths = changes.Deaths;
        Immigrations = changes.Immigrations;
        Emigrations = changes.Emigrations;
        NetChange = netChange;
        PopulationAfter = populationAfter;
    }

    public bool Equals(SettlementPopulationTransition other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return string.Equals(SettlementRuntimeId, other.SettlementRuntimeId, StringComparison.Ordinal)
            && ExpectedRevision == other.ExpectedRevision
            && PopulationBefore == other.PopulationBefore
            && Births == other.Births
            && Deaths == other.Deaths
            && Immigrations == other.Immigrations
            && Emigrations == other.Emigrations
            && NetChange == other.NetChange
            && PopulationAfter == other.PopulationAfter;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SettlementPopulationTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(SettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ ExpectedRevision.GetHashCode();
            hash = (hash * 397) ^ PopulationBefore;
            hash = (hash * 397) ^ Births;
            hash = (hash * 397) ^ Deaths;
            hash = (hash * 397) ^ Immigrations;
            hash = (hash * 397) ^ Emigrations;
            hash = (hash * 397) ^ NetChange.GetHashCode();
            hash = (hash * 397) ^ PopulationAfter;
            return hash;
        }
    }

    public static bool operator ==(SettlementPopulationTransition left, SettlementPopulationTransition right)
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

    public static bool operator !=(SettlementPopulationTransition left, SettlementPopulationTransition right)
    {
        return !(left == right);
    }
}
