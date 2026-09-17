using System;

/// <summary>
/// Mutable aggregate population state owned by a future settlement runtime.
/// This ledger intentionally has no independent runtime identity and no named-NPC awareness.
/// </summary>
public sealed class SettlementPopulationRuntime
{
    private readonly string settlementRuntimeId;
    private int currentPopulation;
    private long revision;

    public string SettlementRuntimeId => settlementRuntimeId;
    public int CurrentPopulation => currentPopulation;
    public long Revision => revision;

    public SettlementPopulationRuntime(string settlementRuntimeId, int currentPopulation)
    {
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            throw new ArgumentException(
                "SettlementPopulationRuntime requires a non-empty settlement RuntimeId.",
                nameof(settlementRuntimeId));
        }

        if (currentPopulation < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentPopulation),
                "Settlement population cannot be negative.");
        }

        this.settlementRuntimeId = settlementRuntimeId;
        this.currentPopulation = currentPopulation;
        revision = 0L;
    }

    internal bool TryApplyTransition(
        SettlementPopulationTransition transition,
        out PopulationTransitionFailure failure)
    {
        failure = PopulationTransitionFailure.None;

        if (transition == null)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (string.Equals(SettlementRuntimeId, transition.SettlementRuntimeId, StringComparison.Ordinal) == false)
        {
            failure = PopulationTransitionFailure.InvalidSettlement;
            return false;
        }

        if (transition.PopulationBefore < 0 || transition.PopulationAfter < 0)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        long expectedNetChange = (long)transition.Births
            + transition.Immigrations
            - transition.Deaths
            - transition.Emigrations;
        if (transition.Births < 0
            || transition.Deaths < 0
            || transition.Immigrations < 0
            || transition.Emigrations < 0
            || transition.NetChange != expectedNetChange
            || (long)transition.PopulationBefore + transition.NetChange != transition.PopulationAfter)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (revision != transition.ExpectedRevision
            || currentPopulation != transition.PopulationBefore)
        {
            failure = PopulationTransitionFailure.StaleState;
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PopulationTransitionFailure.RevisionOverflow;
            return false;
        }

        currentPopulation = transition.PopulationAfter;
        revision++;
        return true;
    }

    internal static bool TryApplyPairedMigration(
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int destinationPopulationBefore,
        out PopulationTransitionFailure failure)
    {
        failure = PopulationTransitionFailure.None;

        if (origin == null || destination == null
            || string.IsNullOrWhiteSpace(origin.SettlementRuntimeId) == true
            || string.IsNullOrWhiteSpace(destination.SettlementRuntimeId) == true
            || ReferenceEquals(origin, destination)
            || string.Equals(origin.SettlementRuntimeId, destination.SettlementRuntimeId, StringComparison.Ordinal))
        {
            failure = PopulationTransitionFailure.InvalidSettlement;
            return false;
        }

        if (originExpectedRevision < 0L
            || destinationExpectedRevision < 0L
            || originPopulationBefore < 0
            || destinationPopulationBefore < 0)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (origin.Revision != originExpectedRevision
            || origin.CurrentPopulation != originPopulationBefore)
        {
            failure = PopulationTransitionFailure.StaleState;
            return false;
        }

        if (destination.Revision != destinationExpectedRevision
            || destination.CurrentPopulation != destinationPopulationBefore)
        {
            failure = PopulationTransitionFailure.StaleState;
            return false;
        }

        if (originPopulationBefore == 0)
        {
            failure = PopulationTransitionFailure.WouldUnderflow;
            return false;
        }

        if (destinationPopulationBefore == int.MaxValue)
        {
            failure = PopulationTransitionFailure.WouldOverflow;
            return false;
        }

        if (origin.Revision == long.MaxValue || destination.Revision == long.MaxValue)
        {
            failure = PopulationTransitionFailure.RevisionOverflow;
            return false;
        }

        // This boundary computes the only valid migration delta and commits both sides
        // only after every state, limit, and revision check has passed.
        origin.currentPopulation = originPopulationBefore - 1;
        origin.revision++;
        destination.currentPopulation = destinationPopulationBefore + 1;
        destination.revision++;
        return true;
    }
}
