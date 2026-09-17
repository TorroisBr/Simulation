using System;

public enum NpcResidenceMigrationFailure
{
    None = 0,
    InvalidNpc = 1,
    DeadNpc = 2,
    InvalidOrigin = 3,
    InvalidDestination = 4,
    SameSettlement = 5,
    ResidenceMismatch = 6,
    OriginUnderflow = 7,
    DestinationOverflow = 8,
    OriginStaleState = 9,
    DestinationStaleState = 10,
    OriginRevisionOverflow = 11,
    DestinationRevisionOverflow = 12,
    InvalidTransition = 13,
    AlreadyApplied = 14
}

/// <summary>
/// Immutable two-aggregate proposal for a named resident changing permanent residence.
/// This phase intentionally supports CityRuntime-to-CityRuntime only; unmodeled residence
/// immigration/emigration will be added with the later population lifecycle boundary.
/// </summary>
public sealed class NpcResidenceMigrationTransition : IEquatable<NpcResidenceMigrationTransition>
{
    public string NpcRuntimeId { get; }
    public string OriginSettlementRuntimeId { get; }
    public string DestinationSettlementRuntimeId { get; }
    public long OriginExpectedRevision { get; }
    public long DestinationExpectedRevision { get; }
    public int OriginPopulationBefore { get; }
    public int OriginPopulationAfter { get; }
    public int DestinationPopulationBefore { get; }
    public int DestinationPopulationAfter { get; }

    public NpcResidenceMigrationTransition(
        string npcRuntimeId,
        string originSettlementRuntimeId,
        string destinationSettlementRuntimeId,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int originPopulationAfter,
        int destinationPopulationBefore,
        int destinationPopulationAfter)
    {
        NpcRuntimeId = npcRuntimeId;
        OriginSettlementRuntimeId = originSettlementRuntimeId;
        DestinationSettlementRuntimeId = destinationSettlementRuntimeId;
        OriginExpectedRevision = originExpectedRevision;
        DestinationExpectedRevision = destinationExpectedRevision;
        OriginPopulationBefore = originPopulationBefore;
        OriginPopulationAfter = originPopulationAfter;
        DestinationPopulationBefore = destinationPopulationBefore;
        DestinationPopulationAfter = destinationPopulationAfter;
    }

    public bool Equals(NpcResidenceMigrationTransition other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return string.Equals(NpcRuntimeId, other.NpcRuntimeId, StringComparison.Ordinal)
            && string.Equals(OriginSettlementRuntimeId, other.OriginSettlementRuntimeId, StringComparison.Ordinal)
            && string.Equals(DestinationSettlementRuntimeId, other.DestinationSettlementRuntimeId, StringComparison.Ordinal)
            && OriginExpectedRevision == other.OriginExpectedRevision
            && DestinationExpectedRevision == other.DestinationExpectedRevision
            && OriginPopulationBefore == other.OriginPopulationBefore
            && OriginPopulationAfter == other.OriginPopulationAfter
            && DestinationPopulationBefore == other.DestinationPopulationBefore
            && DestinationPopulationAfter == other.DestinationPopulationAfter;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as NpcResidenceMigrationTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.Ordinal.GetHashCode(NpcRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(OriginSettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DestinationSettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ OriginExpectedRevision.GetHashCode();
            hash = (hash * 397) ^ DestinationExpectedRevision.GetHashCode();
            hash = (hash * 397) ^ OriginPopulationBefore;
            hash = (hash * 397) ^ OriginPopulationAfter;
            hash = (hash * 397) ^ DestinationPopulationBefore;
            hash = (hash * 397) ^ DestinationPopulationAfter;
            return hash;
        }
    }

    public static bool operator ==(NpcResidenceMigrationTransition left, NpcResidenceMigrationTransition right)
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

    public static bool operator !=(NpcResidenceMigrationTransition left, NpcResidenceMigrationTransition right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Pure proposal and atomic apply boundary for permanent named-resident migration.
/// It does not start travel, alter physical presence, advance time, use RNG, or allocate IDs.
/// </summary>
public static class NpcResidenceMigrationSystem
{
    public static bool TryPropose(
        NpcRuntime npc,
        CityRuntime origin,
        CityRuntime destination,
        out NpcResidenceMigrationTransition transition,
        out NpcResidenceMigrationFailure failure)
    {
        if (origin == null)
        {
            transition = null;
            failure = NpcResidenceMigrationFailure.InvalidOrigin;
            return false;
        }

        if (destination == null)
        {
            transition = null;
            failure = NpcResidenceMigrationFailure.InvalidDestination;
            return false;
        }

        return TryPropose(
            npc,
            origin.Population,
            destination.Population,
            out transition,
            out failure);
    }

    public static bool TryPropose(
        NpcRuntime npc,
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        out NpcResidenceMigrationTransition transition,
        out NpcResidenceMigrationFailure failure)
    {
        transition = null;
        failure = NpcResidenceMigrationFailure.None;

        if (npc == null)
        {
            failure = NpcResidenceMigrationFailure.InvalidNpc;
            return false;
        }

        if (npc.IsAlive == false)
        {
            failure = NpcResidenceMigrationFailure.DeadNpc;
            return false;
        }

        if (origin == null || string.IsNullOrWhiteSpace(origin.SettlementRuntimeId) == true)
        {
            failure = NpcResidenceMigrationFailure.InvalidOrigin;
            return false;
        }

        if (destination == null || string.IsNullOrWhiteSpace(destination.SettlementRuntimeId) == true)
        {
            failure = NpcResidenceMigrationFailure.InvalidDestination;
            return false;
        }

        if (ReferenceEquals(origin, destination)
            || string.Equals(origin.SettlementRuntimeId, destination.SettlementRuntimeId, StringComparison.Ordinal))
        {
            failure = NpcResidenceMigrationFailure.SameSettlement;
            return false;
        }

        if (string.Equals(npc.ResidenceSettlementRuntimeId, origin.SettlementRuntimeId, StringComparison.Ordinal) == false)
        {
            failure = NpcResidenceMigrationFailure.ResidenceMismatch;
            return false;
        }

        if (origin.CurrentPopulation <= 0)
        {
            failure = NpcResidenceMigrationFailure.OriginUnderflow;
            return false;
        }

        if (destination.CurrentPopulation == int.MaxValue)
        {
            failure = NpcResidenceMigrationFailure.DestinationOverflow;
            return false;
        }

        if (origin.Revision == long.MaxValue)
        {
            failure = NpcResidenceMigrationFailure.OriginRevisionOverflow;
            return false;
        }

        if (destination.Revision == long.MaxValue)
        {
            failure = NpcResidenceMigrationFailure.DestinationRevisionOverflow;
            return false;
        }

        transition = new NpcResidenceMigrationTransition(
            npc.RuntimeId,
            origin.SettlementRuntimeId,
            destination.SettlementRuntimeId,
            origin.Revision,
            destination.Revision,
            origin.CurrentPopulation,
            origin.CurrentPopulation - 1,
            destination.CurrentPopulation,
            destination.CurrentPopulation + 1);
        return true;
    }

    public static bool TryApply(
        NpcRuntime npc,
        CityRuntime origin,
        CityRuntime destination,
        NpcResidenceMigrationTransition transition,
        out NpcResidenceMigrationFailure failure)
    {
        if (origin == null)
        {
            failure = NpcResidenceMigrationFailure.InvalidOrigin;
            return false;
        }

        if (destination == null)
        {
            failure = NpcResidenceMigrationFailure.InvalidDestination;
            return false;
        }

        return TryApply(npc, origin.Population, destination.Population, transition, out failure);
    }

    public static bool TryApply(
        NpcRuntime npc,
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        NpcResidenceMigrationTransition transition,
        out NpcResidenceMigrationFailure failure)
    {
        failure = NpcResidenceMigrationFailure.None;

        if (npc == null)
        {
            failure = NpcResidenceMigrationFailure.InvalidNpc;
            return false;
        }

        if (npc.IsAlive == false)
        {
            failure = NpcResidenceMigrationFailure.DeadNpc;
            return false;
        }

        if (origin == null || string.IsNullOrWhiteSpace(origin.SettlementRuntimeId) == true)
        {
            failure = NpcResidenceMigrationFailure.InvalidOrigin;
            return false;
        }

        if (destination == null || string.IsNullOrWhiteSpace(destination.SettlementRuntimeId) == true)
        {
            failure = NpcResidenceMigrationFailure.InvalidDestination;
            return false;
        }

        if (ReferenceEquals(origin, destination)
            || string.Equals(origin.SettlementRuntimeId, destination.SettlementRuntimeId, StringComparison.Ordinal))
        {
            failure = NpcResidenceMigrationFailure.SameSettlement;
            return false;
        }

        if (transition == null)
        {
            failure = NpcResidenceMigrationFailure.InvalidTransition;
            return false;
        }

        if (string.Equals(transition.NpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal) == false
            || string.Equals(transition.OriginSettlementRuntimeId, origin.SettlementRuntimeId, StringComparison.Ordinal) == false
            || string.Equals(transition.DestinationSettlementRuntimeId, destination.SettlementRuntimeId, StringComparison.Ordinal) == false
            || transition.OriginExpectedRevision < 0L
            || transition.DestinationExpectedRevision < 0L
            || transition.OriginPopulationBefore <= 0
            || transition.OriginPopulationAfter != transition.OriginPopulationBefore - 1
            || transition.DestinationPopulationBefore < 0
            || transition.DestinationPopulationAfter != transition.DestinationPopulationBefore + 1)
        {
            failure = NpcResidenceMigrationFailure.InvalidTransition;
            return false;
        }

        if (origin.CurrentPopulation != transition.OriginPopulationBefore
            || origin.Revision != transition.OriginExpectedRevision)
        {
            failure = origin.Revision != transition.OriginExpectedRevision
                ? NpcResidenceMigrationFailure.OriginStaleState
                : NpcResidenceMigrationFailure.InvalidTransition;
            return false;
        }

        if (destination.CurrentPopulation != transition.DestinationPopulationBefore
            || destination.Revision != transition.DestinationExpectedRevision)
        {
            failure = destination.Revision != transition.DestinationExpectedRevision
                ? NpcResidenceMigrationFailure.DestinationStaleState
                : NpcResidenceMigrationFailure.InvalidTransition;
            return false;
        }

        if (string.Equals(npc.ResidenceSettlementRuntimeId, origin.SettlementRuntimeId, StringComparison.Ordinal) == false)
        {
            failure = transition.OriginExpectedRevision < origin.Revision
                || transition.DestinationExpectedRevision < destination.Revision
                ? NpcResidenceMigrationFailure.AlreadyApplied
                : NpcResidenceMigrationFailure.ResidenceMismatch;
            return false;
        }

        if (origin.CurrentPopulation <= 0)
        {
            failure = NpcResidenceMigrationFailure.OriginUnderflow;
            return false;
        }

        if (destination.CurrentPopulation == int.MaxValue)
        {
            failure = NpcResidenceMigrationFailure.DestinationOverflow;
            return false;
        }

        if (origin.Revision == long.MaxValue)
        {
            failure = NpcResidenceMigrationFailure.OriginRevisionOverflow;
            return false;
        }

        if (destination.Revision == long.MaxValue)
        {
            failure = NpcResidenceMigrationFailure.DestinationRevisionOverflow;
            return false;
        }

        // Every failure condition has been checked for both aggregates before this point.
        // These commits cannot fail and therefore cannot leave only one population changed.
        origin.CommitValidatedTransition(transition.OriginPopulationAfter);
        destination.CommitValidatedTransition(transition.DestinationPopulationAfter);
        npc.SetResidenceSettlementRuntimeId(destination.SettlementRuntimeId);
        return true;
    }
}
