using System;

public enum NpcPopulationLifecycleOperation
{
    Immigration = 1,
    Emigration = 2,
    ResidentDeath = 3
}

/// <summary>
/// Immutable facts for one named-NPC population lifecycle transition.
/// It contains no mutable runtime references and does not apply itself.
/// </summary>
public sealed class NpcPopulationLifecycleTransition : IEquatable<NpcPopulationLifecycleTransition>
{
    public string NpcRuntimeId { get; }
    public string SettlementRuntimeId { get; }
    public NpcPopulationLifecycleOperation Operation { get; }
    public long ExpectedRevision { get; }
    public int PopulationBefore { get; }
    public int PopulationAfter { get; }
    public string ResidenceBefore { get; }
    public string ResidenceAfter { get; }
    public NpcLifeState LifeStateBefore { get; }
    public NpcLifeState LifeStateAfter { get; }

    public NpcPopulationLifecycleTransition(
        string npcRuntimeId,
        string settlementRuntimeId,
        NpcPopulationLifecycleOperation operation,
        long expectedRevision,
        int populationBefore,
        int populationAfter,
        string residenceBefore,
        string residenceAfter,
        NpcLifeState lifeStateBefore,
        NpcLifeState lifeStateAfter)
    {
        NpcRuntimeId = npcRuntimeId;
        SettlementRuntimeId = settlementRuntimeId;
        Operation = operation;
        ExpectedRevision = expectedRevision;
        PopulationBefore = populationBefore;
        PopulationAfter = populationAfter;
        ResidenceBefore = residenceBefore;
        ResidenceAfter = residenceAfter;
        LifeStateBefore = lifeStateBefore;
        LifeStateAfter = lifeStateAfter;
    }

    public bool Equals(NpcPopulationLifecycleTransition other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return string.Equals(NpcRuntimeId, other.NpcRuntimeId, StringComparison.Ordinal)
            && string.Equals(SettlementRuntimeId, other.SettlementRuntimeId, StringComparison.Ordinal)
            && Operation == other.Operation
            && ExpectedRevision == other.ExpectedRevision
            && PopulationBefore == other.PopulationBefore
            && PopulationAfter == other.PopulationAfter
            && string.Equals(ResidenceBefore, other.ResidenceBefore, StringComparison.Ordinal)
            && string.Equals(ResidenceAfter, other.ResidenceAfter, StringComparison.Ordinal)
            && LifeStateBefore == other.LifeStateBefore
            && LifeStateAfter == other.LifeStateAfter;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as NpcPopulationLifecycleTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(NpcRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ (int)Operation;
            hash = (hash * 397) ^ ExpectedRevision.GetHashCode();
            hash = (hash * 397) ^ PopulationBefore;
            hash = (hash * 397) ^ PopulationAfter;
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ResidenceBefore ?? string.Empty);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ResidenceAfter ?? string.Empty);
            hash = (hash * 397) ^ (int)LifeStateBefore;
            hash = (hash * 397) ^ (int)LifeStateAfter;
            return hash;
        }
    }

    public static bool operator ==(NpcPopulationLifecycleTransition left, NpcPopulationLifecycleTransition right)
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

    public static bool operator !=(NpcPopulationLifecycleTransition left, NpcPopulationLifecycleTransition right)
    {
        return !(left == right);
    }
}
