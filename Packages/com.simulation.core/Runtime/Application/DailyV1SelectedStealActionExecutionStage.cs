using System;

/// <summary>
/// Host ports for the selected Steal operation. Core owns action sequencing,
/// social outcome and Justice owner changes; the host supplies the existing
/// P12 admission, exact Unity projection, and presentation boundaries.
/// </summary>
internal static class DailyV1SelectedStealActionExecutionStage
{
    internal interface ISelectedStealActionBoundary
        : DailyV1SelectedActionExecutionStage.ISelectedNpcOwnerMutationBoundary
    {
        CrimeSocialAppraisalWorldState SocialAppraisalWorldState { get; }
        ITheftOutcomeSink TheftOutcomeSink { get; }
        DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }

        bool ExecuteWithinOperation(Func<bool> operation);
        EconomyTransactionResult TryTransferMoney(
            DailyV1NpcRuntimeOwners source,
            DailyV1NpcRuntimeOwners destination,
            int amount);
        bool TryProjectWantedRecord(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner record);
        bool TryPrepareWantedRecordProjection(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner record);
        void DiscardPreparedWantedRecordProjection(DailyV1JusticeWantedRecordOwner record);
        void PublishTheft(
            DailyV1NpcRuntimeOwners perpetrator,
            DailyV1NpcRuntimeOwners victim,
            int amount);
        void LogCompensationFailure();
        void PublishWantedRecord(
            DailyV1NpcRuntimeOwners target,
            DailyV1CityRuntimeOwners city,
            DailyV1JusticeWantedRecordOwner record);
    }

    internal sealed class Result
    {
        internal bool Succeeded { get; }
        internal string TargetRuntimeId { get; }
        internal string CityRuntimeId { get; }
        internal int Amount { get; }

        internal Result(bool succeeded, string targetRuntimeId, string cityRuntimeId, int amount)
        {
            Succeeded = succeeded;
            TargetRuntimeId = targetRuntimeId;
            CityRuntimeId = cityRuntimeId;
            Amount = amount;
        }
    }

    internal static bool TryExecuteSelectedStealActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners perpetrator,
        DailyV1ActionDefinition action,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        ISelectedStealActionBoundary boundary,
        out bool actionRollSucceeded,
        out Result result)
    {
        actionRollSucceeded = false;
        result = null;
        if (graph == null || perpetrator == null || action == null
            || randomSource == null || boundary == null || currentAbsoluteDay < 0L
            || !graph.ContainsExactNpcOwner(perpetrator)
            || !ReferenceEquals(graph.Definitions, perpetrator.Definitions)
            || !ReferenceEquals(perpetrator.ActionSlot?.CurrentAction, action)
            || !ReferenceEquals(perpetrator.ActionSlot?.CurrentPayload?.Action, action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.StealActionTypeValue)
            return false;

        DailyV1NpcActionPayloadOwner payload = perpetrator.ActionSlot.CurrentPayload;
        if (!DailyV1SelectedActionExecutionStage.TryRollCurrentActionSlot(
                graph, perpetrator, randomSource, currentAbsoluteDay, out actionRollSucceeded))
            return false;

        if (!actionRollSucceeded)
        {
            // The legacy Crime provider opened the Crime/Justice P12 boundary
            // for failed rolls even though Steal has no failure mutation.
            boundary.ExecuteWithinOperation(() => false);
            result = new Result(false, payload.TargetNpcRuntimeId,
                perpetrator.Travel?.CurrentCityRuntimeId, 0);
            return true;
        }

        bool succeeded = boundary.ExecuteWithinOperation(() => TryApply(
            graph, perpetrator, action, payload, boundary));
        result = new Result(succeeded, payload.TargetNpcRuntimeId,
            perpetrator.Travel?.CurrentCityRuntimeId,
            succeeded ? Math.Max(0, payload.Amount) : 0);
        return true;
    }

    private static bool TryApply(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners perpetrator,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        ISelectedStealActionBoundary boundary)
    {
        DailyV1EffectiveDefinitions definitions = graph.Definitions;
        DailyV1JusticeRecordsOwner justice = graph.JusticeRecordsOwner;
        CrimeSocialAppraisalWorldState socialWorld = boundary.SocialAppraisalWorldState;
        ITheftOutcomeSink theftOutcomeSink = boundary.TheftOutcomeSink;
        if (payload == null || payload.TargetItem != null || payload.TargetCityRuntimeId != null
            || string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(definitions, action)
            || !ReferenceEquals(justice, boundary.JusticeRecordsOwner)
            || !ReferenceEquals(justice, graph.JusticeRecordsOwner)
            || socialWorld == null
            || theftOutcomeSink == null
            || socialWorld.SimulationTime.AbsoluteDay < 0L
            || !TryFindExactNpc(graph, payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners victim)
            || ReferenceEquals(victim, perpetrator)
            || !TryFindExactCity(graph, perpetrator.Travel?.CurrentCityRuntimeId,
                out DailyV1CityRuntimeOwners city)
            || victim.Travel == null || victim.Travel.IsTraveling
            || !string.Equals(victim.Travel.CurrentCityRuntimeId, city.RuntimeId,
                StringComparison.Ordinal)
            || perpetrator.Travel == null || perpetrator.Travel.IsTraveling
            || perpetrator.Lifecycle?.LifeState != NpcLifeState.Alive
            || victim.Lifecycle?.LifeState != NpcLifeState.Alive
            || perpetrator.MoneyAccount == null || victim.MoneyAccount == null
            || victim.MoneyAccount.Balance <= 0f
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                definitions, action, perpetrator.Status?.Statuses))
            return false;

        string arrestedStatusId = definitions.Configuration?.arrestedStatus;
        if (!string.IsNullOrWhiteSpace(arrestedStatusId)
            && definitions.Statuses.TryGetValue(arrestedStatusId,
                out DailyV1StatusDefinition arrestedStatus)
            && (perpetrator.Status?.Contains(arrestedStatus) == true
                || victim.Status?.Contains(arrestedStatus) == true))
            return false;

        if (victim.HiddenState?.RemainingDays > 0) return false;

        int amount = UnityMathfCompatibility.Min(
            payload.Amount, UnityMathfCompatibility.FloorToInt(victim.MoneyAccount.Balance));
        TheftOutcome theftOutcome = null;
        if (theftOutcomeSink != null)
        {
            if (string.IsNullOrWhiteSpace(payload.StableOccurrenceKey)
                || !socialWorld.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    perpetrator.RuntimeId, out PersonRuntime perpetratorPerson)
                || perpetratorPerson == null
                || perpetratorPerson.PersonId == null
                || !string.Equals(perpetratorPerson.MaterializedNpcRuntimeId,
                    perpetrator.RuntimeId, StringComparison.Ordinal)
                || !socialWorld.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    victim.RuntimeId, out PersonRuntime victimPerson)
                || victimPerson == null || victimPerson.PersonId == null
                || !string.Equals(victimPerson.MaterializedNpcRuntimeId,
                    victim.RuntimeId, StringComparison.Ordinal))
                return false;

            if (amount > 0)
            {
                long absoluteDay = socialWorld.SimulationTime.AbsoluteDay;
                TheftOutcomeId outcomeId = TheftOutcomeId.Create(
                    perpetratorPerson.PersonId, victimPerson.PersonId,
                    absoluteDay, payload.StableOccurrenceKey);
                theftOutcome = new TheftOutcome(
                    outcomeId, perpetratorPerson.PersonId, victimPerson.PersonId,
                    amount, absoluteDay, payload.StableOccurrenceKey,
                    payload.OriginDecisionId);
                if (!theftOutcomeSink.CanAcceptTheftOutcome(theftOutcome)) return false;
            }
        }

        if (amount <= 0) return false;
        EconomyTransactionResult transfer = boundary.TryTransferMoney(victim, perpetrator, amount);
        if (transfer?.Success != true) return false;

        // Keep the legacy observable ordering: the theft log is emitted after
        // transfer but before social acceptance, including rejected outcomes.
        boundary.PublishTheft(perpetrator, victim, amount);
        if (theftOutcome != null && !theftOutcomeSink.TryAcceptTheftOutcome(theftOutcome))
        {
            EconomyTransactionResult rollback = boundary.TryTransferMoney(
                perpetrator, victim, amount);
            if (rollback?.Success != true) boundary.LogCompensationFailure();
            return false;
        }
        DailyV1CrimeActionSettings settings =
            DailyV1NpcDefinitionRules.GetCrimeActionSettings(definitions, action)
            ?? new DailyV1CrimeActionSettings(20, 50f, 3, 1, 25f, 2);
        // Legacy Steal makes the perpetrator wanted; the victim is only the
        // transfer and social-outcome counterparty.
        DailyV1JusticeWantedRecordOwner warrant = justice.FindActiveWantedRecord(
            perpetrator.RuntimeId, city.RuntimeId);
        if (warrant == null)
        {
            if (!justice.CanCommitMutation())
                throw new InvalidOperationException(
                    "The selected Steal Justice owner could not admit its warrant write.");
            warrant = new DailyV1JusticeWantedRecordOwner(
                perpetrator.RuntimeId, city.RuntimeId, settings.bounty, settings.sentenceDays);
            if (!boundary.TryPrepareWantedRecordProjection(perpetrator, city, warrant))
                return false;
            if (!justice.TryAddWantedRecord(warrant))
            {
                boundary.DiscardPreparedWantedRecordProjection(warrant);
                return false;
            }
            if (!justice.NotifyMutationCommitted())
                throw new InvalidOperationException(
                    "The selected Steal Justice owner rejected its committed warrant.");
        }
        else if (!warrant.TryAddPenalty(settings.bounty, settings.sentenceDays))
        {
            throw new InvalidOperationException(
                "The selected Steal Justice owner rejected its warrant update.");
        }
        if (!boundary.TryProjectWantedRecord(perpetrator, city, warrant))
            throw new InvalidOperationException(
                "The selected Steal warrant could not bind its exact Unity view.");

        string wantedStatusId = definitions.Configuration?.wantedStatus;
        if (!string.IsNullOrWhiteSpace(wantedStatusId))
        {
            if (!definitions.Statuses.TryGetValue(wantedStatusId,
                    out DailyV1StatusDefinition wantedStatus)
                || !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                    perpetrator,
                    Array.Empty<DailyV1StatusDefinition>(),
                    new[] { wantedStatus },
                    boundary))
                throw new InvalidOperationException(
                    "The selected Steal wanted-status projection could not be applied.");
        }

        boundary.PublishWantedRecord(perpetrator, city, warrant);
        return true;
    }

    private static bool TryFindExactNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId,
        out DailyV1NpcRuntimeOwners owner)
    {
        owner = null;
        if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1NpcRuntimeOwners candidate in graph.Npcs)
        {
            if (candidate == null
                || !string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                continue;
            if (owner != null || !graph.ContainsExactNpcOwner(candidate)) return false;
            owner = candidate;
        }
        return owner != null;
    }

    private static bool TryFindExactCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId,
        out DailyV1CityRuntimeOwners owner)
    {
        owner = null;
        if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (candidate == null
                || !string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                continue;
            if (owner != null || !graph.ContainsExactCityOwner(candidate)) return false;
            owner = candidate;
        }
        return owner != null;
    }
}
