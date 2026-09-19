using System;

public enum NpcPopulationLifecycleFailure
{
    None = 0,
    InvalidNpc = 1,
    DeadNpc = 2,
    InvalidSettlement = 3,
    AuthoritativeRosterRequired = 4,
    NpcNotInAuthoritativeRoster = 5,
    ResidenceAlreadyAssigned = 6,
    ResidenceMismatch = 7,
    AggregateCapacityExceeded = 8,
    PopulationUnderflow = 9,
    PopulationOverflow = 10,
    RevisionOverflow = 11,
    StaleState = 12,
    InvalidTransition = 13,
    InvalidInjury = 14,
    PersonDeathAuthorityRequired = 15,
    PersonAlreadyDead = 16,
    InvalidDeathDay = 17
}

/// <summary>
/// Explicit boundary for named-NPC immigration, emigration, and resident death.
/// Each operation validates the world snapshot and aggregate first, then proposes
/// and applies one exact aggregate change before applying the already-validated NPC
/// state change. No automatic demography or daily lifecycle is performed here.
/// </summary>
public static class NpcPopulationLifecycleSystem
{
    public static bool TryProposeImmigration(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        return TryPropose(
            npc,
            settlement,
            authoritativeRoster,
            NpcPopulationLifecycleOperation.Immigration,
            out transition,
            out failure);
    }

    public static bool TryProposeEmigration(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        return TryPropose(
            npc,
            settlement,
            authoritativeRoster,
            NpcPopulationLifecycleOperation.Emigration,
            out transition,
            out failure);
    }

    public static bool TryProposeResidentDeath(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        return TryPropose(
            npc,
            settlement,
            authoritativeRoster,
            NpcPopulationLifecycleOperation.ResidentDeath,
            out transition,
            out failure);
    }

    public static bool TryApplyImmigration(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (TryProposeImmigration(npc, settlement, authoritativeRoster, out transition, out failure) == false)
        {
            return false;
        }

        return TryApply(npc, settlement, authoritativeRoster, transition, out failure);
    }

    public static bool TryApplyEmigration(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (TryProposeEmigration(npc, settlement, authoritativeRoster, out transition, out failure) == false)
        {
            return false;
        }

        return TryApply(npc, settlement, authoritativeRoster, transition, out failure);
    }

    public static bool TryApplyResidentDeath(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (TryProposeResidentDeath(npc, settlement, authoritativeRoster, out transition, out failure) == false)
        {
            return false;
        }

        return TryApply(npc, settlement, authoritativeRoster, transition, out failure);
    }

    internal static bool TryApplyResidentPersonDeath(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        long deathAbsoluteDay,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (TryProposeResidentDeath(
                npc,
                settlement,
                authoritativeRoster,
                out transition,
                out failure) == false)
        {
            return false;
        }

        return TryApplyInternal(
            npc,
            settlement,
            authoritativeRoster,
            transition,
            false,
            NpcInjurySeverity.None,
            deathAbsoluteDay,
            out failure);
    }

    /// <summary>
    /// Applies a conflict injury and resident death as one owner-aware lifecycle
    /// operation. The injury is validated before the aggregate commit and is committed
    /// only by the same internal NPC transition that clears residence and marks death.
    /// </summary>
    public static bool TryApplyResidentDeathWithConflictInjury(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcInjurySeverity injurySeverity,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (NpcInjuryRules.IsValid(injurySeverity) == false)
        {
            transition = null;
            failure = NpcPopulationLifecycleFailure.InvalidInjury;
            return false;
        }

        if (TryProposeResidentDeath(
            npc,
            settlement,
            authoritativeRoster,
            out transition,
            out failure) == false)
        {
            return false;
        }

        return TryApplyResidentDeathWithConflictInjury(
            npc,
            settlement,
            authoritativeRoster,
            injurySeverity,
            transition,
            out failure);
    }

    public static bool TryApplyResidentDeathWithConflictInjury(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcInjurySeverity injurySeverity,
        NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (NpcInjuryRules.IsValid(injurySeverity) == false)
        {
            failure = NpcPopulationLifecycleFailure.InvalidInjury;
            return false;
        }

        return TryApplyInternal(
            npc,
            settlement,
            authoritativeRoster,
            transition,
            true,
            injurySeverity,
            null,
            out failure);
    }

    internal static bool TryApplyResidentPersonDeathWithConflictInjury(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcInjurySeverity injurySeverity,
        long deathAbsoluteDay,
        NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (NpcInjuryRules.IsValid(injurySeverity) == false)
        {
            failure = NpcPopulationLifecycleFailure.InvalidInjury;
            return false;
        }

        return TryApplyInternal(
            npc,
            settlement,
            authoritativeRoster,
            transition,
            true,
            injurySeverity,
            deathAbsoluteDay,
            out failure);
    }

    internal static bool TryApplyResidentPersonDeathWithConflictInjury(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcInjurySeverity injurySeverity,
        long deathAbsoluteDay,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (TryProposeResidentDeath(
                npc,
                settlement,
                authoritativeRoster,
                out transition,
                out failure) == false)
        {
            return false;
        }

        return TryApplyResidentPersonDeathWithConflictInjury(
            npc,
            settlement,
            authoritativeRoster,
            injurySeverity,
            deathAbsoluteDay,
            transition,
            out failure);
    }

    public static bool TryApply(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        return TryApplyInternal(
            npc,
            settlement,
            authoritativeRoster,
            transition,
            false,
            NpcInjurySeverity.None,
            null,
            out failure);
    }

    private static bool TryApplyInternal(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcPopulationLifecycleTransition transition,
        bool applyConflictInjury,
        NpcInjurySeverity injurySeverity,
        long? personDeathAbsoluteDay,
        out NpcPopulationLifecycleFailure failure)
    {
        failure = NpcPopulationLifecycleFailure.None;

        if (transition == null)
        {
            failure = NpcPopulationLifecycleFailure.InvalidTransition;
            return false;
        }

        if (TryValidateCurrentState(
            npc,
            settlement,
            authoritativeRoster,
            transition.Operation,
            out SettlementPopulationPresenceSummary summary,
            out failure) == false)
        {
            return false;
        }

        if (ValidateTransitionFacts(npc, settlement, transition, summary, out failure) == false)
        {
            return false;
        }

        if (ValidatePersonDeathAuthority(
                npc,
                transition.Operation,
                personDeathAbsoluteDay,
                out failure) == false)
        {
            return false;
        }

        PopulationChangeSet changes = CreateChanges(transition.Operation);
        if (SettlementPopulationSystem.TryPropose(
            settlement.Population,
            changes,
            out SettlementPopulationTransition aggregateTransition,
            out PopulationTransitionFailure aggregateFailure) == false)
        {
            failure = MapAggregateFailure(aggregateFailure);
            return false;
        }

        if (aggregateTransition.ExpectedRevision != transition.ExpectedRevision
            || aggregateTransition.PopulationBefore != transition.PopulationBefore
            || aggregateTransition.PopulationAfter != transition.PopulationAfter)
        {
            failure = NpcPopulationLifecycleFailure.StaleState;
            return false;
        }

        if (SettlementPopulationSystem.TryApply(
            settlement.Population,
            aggregateTransition,
            out aggregateFailure) == false)
        {
            failure = MapAggregateFailure(aggregateFailure);
            return false;
        }

        if (transition.Operation == NpcPopulationLifecycleOperation.Immigration)
        {
            npc.SetResidenceSettlementRuntimeId(settlement.RuntimeId);
        }
        else if (transition.Operation == NpcPopulationLifecycleOperation.Emigration)
        {
            npc.SetResidenceSettlementRuntimeId(null);
        }
        else
        {
            npc.ApplyResidentDeathAfterPopulationValidation(
                applyConflictInjury ? injurySeverity : NpcInjurySeverity.None,
                personDeathAbsoluteDay);
        }

        return true;
    }

    private static bool ValidatePersonDeathAuthority(
        NpcRuntime npc,
        NpcPopulationLifecycleOperation operation,
        long? personDeathAbsoluteDay,
        out NpcPopulationLifecycleFailure failure)
    {
        failure = NpcPopulationLifecycleFailure.None;
        if (operation != NpcPopulationLifecycleOperation.ResidentDeath
            || npc.BoundPersonRuntime == null)
        {
            return true;
        }

        if (personDeathAbsoluteDay.HasValue == false)
        {
            failure = NpcPopulationLifecycleFailure.PersonDeathAuthorityRequired;
            return false;
        }

        if (npc.BoundPersonRuntime.DeathAbsoluteDay.HasValue)
        {
            failure = NpcPopulationLifecycleFailure.PersonAlreadyDead;
            return false;
        }

        if (npc.CanApplyPersonBackedDeath(personDeathAbsoluteDay.Value) == false)
        {
            failure = NpcPopulationLifecycleFailure.InvalidDeathDay;
            return false;
        }

        return true;
    }

    private static bool TryPropose(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcPopulationLifecycleOperation operation,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        transition = null;
        failure = NpcPopulationLifecycleFailure.None;

        if (TryValidateCurrentState(
            npc,
            settlement,
            authoritativeRoster,
            operation,
            out SettlementPopulationPresenceSummary summary,
            out failure) == false)
        {
            return false;
        }

        int populationBefore = settlement.CurrentPopulation;
        int populationAfter;
        if (operation == NpcPopulationLifecycleOperation.Immigration)
        {
            if (populationBefore == int.MaxValue)
            {
                failure = NpcPopulationLifecycleFailure.PopulationOverflow;
                return false;
            }

            populationAfter = populationBefore + 1;
        }
        else
        {
            if (populationBefore <= 0)
            {
                failure = NpcPopulationLifecycleFailure.PopulationUnderflow;
                return false;
            }

            populationAfter = populationBefore - 1;
        }

        if (settlement.Population.Revision == long.MaxValue)
        {
            failure = NpcPopulationLifecycleFailure.RevisionOverflow;
            return false;
        }

        int resultingNamedResidentCount = summary.NamedResidentCount
            + (operation == NpcPopulationLifecycleOperation.Immigration ? 1 : -1);
        if (resultingNamedResidentCount > populationAfter)
        {
            failure = NpcPopulationLifecycleFailure.AggregateCapacityExceeded;
            return false;
        }

        transition = new NpcPopulationLifecycleTransition(
            npc.RuntimeId,
            settlement.RuntimeId,
            operation,
            settlement.Population.Revision,
            populationBefore,
            populationAfter,
            npc.ResidenceSettlementRuntimeId,
            operation == NpcPopulationLifecycleOperation.Immigration ? settlement.RuntimeId : null,
            NpcLifeState.Alive,
            operation == NpcPopulationLifecycleOperation.ResidentDeath ? NpcLifeState.Dead : NpcLifeState.Alive);
        return true;
    }

    private static bool TryValidateCurrentState(
        NpcRuntime npc,
        CityRuntime settlement,
        AuthoritativeNpcRoster authoritativeRoster,
        NpcPopulationLifecycleOperation operation,
        out SettlementPopulationPresenceSummary summary,
        out NpcPopulationLifecycleFailure failure)
    {
        summary = null;
        failure = NpcPopulationLifecycleFailure.None;

        if (npc == null)
        {
            failure = NpcPopulationLifecycleFailure.InvalidNpc;
            return false;
        }

        if (settlement == null || string.IsNullOrWhiteSpace(settlement.RuntimeId) == true)
        {
            failure = NpcPopulationLifecycleFailure.InvalidSettlement;
            return false;
        }

        if (npc.IsAlive == false)
        {
            failure = NpcPopulationLifecycleFailure.DeadNpc;
            return false;
        }

        if (authoritativeRoster == null)
        {
            failure = NpcPopulationLifecycleFailure.AuthoritativeRosterRequired;
            return false;
        }

        if (operation != NpcPopulationLifecycleOperation.Immigration
            && operation != NpcPopulationLifecycleOperation.Emigration
            && operation != NpcPopulationLifecycleOperation.ResidentDeath)
        {
            failure = NpcPopulationLifecycleFailure.InvalidTransition;
            return false;
        }

        if (ContainsNpc(authoritativeRoster, npc) == false)
        {
            failure = NpcPopulationLifecycleFailure.NpcNotInAuthoritativeRoster;
            return false;
        }

        bool hasResidence = string.IsNullOrWhiteSpace(npc.ResidenceSettlementRuntimeId) == false;
        if (operation == NpcPopulationLifecycleOperation.Immigration)
        {
            if (hasResidence == true)
            {
                failure = NpcPopulationLifecycleFailure.ResidenceAlreadyAssigned;
                return false;
            }
        }
        else if (string.Equals(npc.ResidenceSettlementRuntimeId, settlement.RuntimeId, StringComparison.Ordinal) == false)
        {
            failure = NpcPopulationLifecycleFailure.ResidenceMismatch;
            return false;
        }

        summary = SettlementPopulationPresenceQuery.BuildSummary(settlement, authoritativeRoster.Npcs);
        if (summary == null || summary.NamedResidentCount > summary.ResidentPopulation)
        {
            failure = NpcPopulationLifecycleFailure.AggregateCapacityExceeded;
            return false;
        }

        return true;
    }

    private static bool ValidateTransitionFacts(
        NpcRuntime npc,
        CityRuntime settlement,
        NpcPopulationLifecycleTransition transition,
        SettlementPopulationPresenceSummary summary,
        out NpcPopulationLifecycleFailure failure)
    {
        failure = NpcPopulationLifecycleFailure.None;

        if (string.Equals(transition.NpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal) == false
            || string.Equals(transition.SettlementRuntimeId, settlement.RuntimeId, StringComparison.Ordinal) == false
            || transition.ExpectedRevision < 0L
            || transition.PopulationBefore < 0
            || transition.PopulationAfter < 0
            || transition.LifeStateBefore != NpcLifeState.Alive
            || !string.Equals(transition.ResidenceBefore, npc.ResidenceSettlementRuntimeId, StringComparison.Ordinal))
        {
            failure = NpcPopulationLifecycleFailure.InvalidTransition;
            return false;
        }

        if (settlement.Population.Revision != transition.ExpectedRevision
            || settlement.CurrentPopulation != transition.PopulationBefore)
        {
            failure = NpcPopulationLifecycleFailure.StaleState;
            return false;
        }

        if (transition.Operation == NpcPopulationLifecycleOperation.Immigration)
        {
            if (transition.PopulationBefore == int.MaxValue
                || transition.PopulationAfter != transition.PopulationBefore + 1
                || transition.ResidenceAfter != settlement.RuntimeId
                || transition.LifeStateAfter != NpcLifeState.Alive)
            {
                failure = NpcPopulationLifecycleFailure.InvalidTransition;
                return false;
            }
        }
        else if (transition.Operation == NpcPopulationLifecycleOperation.Emigration)
        {
            if (transition.PopulationBefore <= 0
                || transition.PopulationAfter != transition.PopulationBefore - 1
                || transition.ResidenceBefore != settlement.RuntimeId
                || !string.IsNullOrWhiteSpace(transition.ResidenceAfter)
                || transition.LifeStateAfter != NpcLifeState.Alive)
            {
                failure = NpcPopulationLifecycleFailure.InvalidTransition;
                return false;
            }
        }
        else if (transition.Operation == NpcPopulationLifecycleOperation.ResidentDeath)
        {
            if (transition.PopulationBefore <= 0
                || transition.PopulationAfter != transition.PopulationBefore - 1
                || transition.ResidenceBefore != settlement.RuntimeId
                || !string.IsNullOrWhiteSpace(transition.ResidenceAfter)
                || transition.LifeStateAfter != NpcLifeState.Dead)
            {
                failure = NpcPopulationLifecycleFailure.InvalidTransition;
                return false;
            }
        }
        else
        {
            failure = NpcPopulationLifecycleFailure.InvalidTransition;
            return false;
        }

        int resultingNamedResidentCount = summary.NamedResidentCount
            + (transition.Operation == NpcPopulationLifecycleOperation.Immigration ? 1 : -1);
        if (resultingNamedResidentCount > transition.PopulationAfter)
        {
            failure = NpcPopulationLifecycleFailure.AggregateCapacityExceeded;
            return false;
        }

        if (settlement.Population.Revision == long.MaxValue)
        {
            failure = NpcPopulationLifecycleFailure.RevisionOverflow;
            return false;
        }

        return true;
    }

    private static PopulationChangeSet CreateChanges(NpcPopulationLifecycleOperation operation)
    {
        if (operation == NpcPopulationLifecycleOperation.Immigration)
        {
            return new PopulationChangeSet(0, 0, 1, 0);
        }

        if (operation == NpcPopulationLifecycleOperation.Emigration)
        {
            return new PopulationChangeSet(0, 0, 0, 1);
        }

        return new PopulationChangeSet(0, 1, 0, 0);
    }

    private static NpcPopulationLifecycleFailure MapAggregateFailure(PopulationTransitionFailure failure)
    {
        if (failure == PopulationTransitionFailure.WouldUnderflow)
        {
            return NpcPopulationLifecycleFailure.PopulationUnderflow;
        }

        if (failure == PopulationTransitionFailure.WouldOverflow)
        {
            return NpcPopulationLifecycleFailure.PopulationOverflow;
        }

        if (failure == PopulationTransitionFailure.RevisionOverflow)
        {
            return NpcPopulationLifecycleFailure.RevisionOverflow;
        }

        if (failure == PopulationTransitionFailure.StaleState)
        {
            return NpcPopulationLifecycleFailure.StaleState;
        }

        return NpcPopulationLifecycleFailure.InvalidTransition;
    }

    private static bool ContainsNpc(AuthoritativeNpcRoster authoritativeRoster, NpcRuntime npc)
    {
        foreach (NpcRuntime rosterNpc in authoritativeRoster.Npcs)
        {
            if (rosterNpc != null
                && string.Equals(rosterNpc.RuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
