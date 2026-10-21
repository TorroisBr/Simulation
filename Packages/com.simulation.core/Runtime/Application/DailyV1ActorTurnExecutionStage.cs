using System;
using System.Collections.Generic;

/// <summary>
/// One exact selected actor at its original SimulationRuntime roster position.
/// A null ActorOwner preserves a null Unity roster slot without compacting the
/// ordinals recorded by actor-choice dispositions.
/// </summary>
internal sealed class DailyV1ActorTurnRosterEntry
{
    internal int RosterOrdinal { get; }
    internal PersonId PersonId { get; }
    internal DailyV1NpcRuntimeOwners ActorOwner { get; }

    internal DailyV1ActorTurnRosterEntry(
        int rosterOrdinal,
        PersonId personId,
        DailyV1NpcRuntimeOwners actorOwner)
    {
        if (rosterOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(rosterOrdinal));
        RosterOrdinal = rosterOrdinal;
        PersonId = personId;
        ActorOwner = actorOwner;
    }
}

/// <summary>
/// Host queries and executes effects that remain Unity-owned while Core owns
/// selected Daily-v1 roster order and the precedence of each actor turn.
/// </summary>
internal interface IDailyV1ActorTurnExecutionBoundary
{
    bool IsInSpatialTransit(DailyV1NpcRuntimeOwners actor, PersonId personId);
    bool IsOnActiveExpedition(DailyV1NpcRuntimeOwners actor);
    bool IsReservedToday(DailyV1NpcRuntimeOwners actor);
    void EvaluateStatus(DailyV1NpcRuntimeOwners actor);
    void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive);
    bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal);
    void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor);
    void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor);
}

/// <summary>
/// Optional selected-profile ports for Core to own the complete autonomous
/// action sequence. The host supplies only Unity projections, existing P12
/// admission/effect adapters, and presentation publication.
/// </summary>
internal interface IDailyV1SelectedActorActionExecutionBoundary
{
    IAuthoritativeRandomSource RandomSource { get; }
    bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor);
    void LogSkippedDirective(ScheduledDirective directive, string reason);

    bool TryCreateDecisionBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary);

    bool TryGetConfiguredActionIds(
        DailyV1NpcRuntimeOwners actor,
        out IReadOnlyList<string> actionIds);

    bool HasRequestedActionProvider(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action);

    bool TryRecordAndInstallSelectedAction(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate selected,
        IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary,
        NpcDecisionOrigin origin);

    void ClearCurrentAction(DailyV1NpcRuntimeOwners actor);
    bool IsActorAtCurrentCityLocation(DailyV1NpcRuntimeOwners actor);
    void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor);
    bool HasExactCurrentActionProjection(DailyV1NpcRuntimeOwners actor);
    bool TryGetDispatchBoundary(
        DailyV1NpcRuntimeOwners actor,
        out IDailyV1SelectedActionDispatchBoundary dispatchBoundary);
    bool TryCompleteDispatch(
        DailyV1NpcRuntimeOwners actor,
        DailyV1SelectedActionDispatchResult dispatch,
        out bool actionSucceeded,
        out NpcActionResultType? resultType,
        out string resultMessage);
    bool TryGetActionStatusBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcRuntimeOwners target,
        bool reuseExistingOperation,
        out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary);
    bool TryInstallForcedOutcomeAction(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action);
    bool TryGetForcedEscapeBoundary(
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary);
}

/// <summary>
/// Core-owned ordered selected Daily-v1 actor loop. Unity supplies only
/// current spatial/expedition facts and forwards the existing host operations;
/// the selected owner graph and choice store remain the authorities for actor
/// life/travel and queued choice lifecycle.
/// </summary>
internal static class DailyV1ActorTurnExecutionStage
{
    internal static bool TryRunOrdered(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        ScheduledDirectiveStore scheduledDirectiveStore,
        long absoluteDay,
        bool merchantTradeEnabled,
        IDailyV1ActorTurnExecutionBoundary boundary)
    {
        if (graph == null || roster == null || personStore == null
            || actorChoiceStore == null || boundary == null)
            throw new ArgumentNullException("Selected actor turns require their exact graph, roster, owners and host boundary.");
        if (!ReferenceEquals(actorChoiceStore.PersonStore, personStore))
            throw new InvalidOperationException("Selected actor turns must use the PersonStore owned by their actor-choice store.");
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));

        List<DailyV1NpcRuntimeOwners> rosterOwners = new List<DailyV1NpcRuntimeOwners>();
        for (int ordinal = 0; ordinal < roster.Count; ordinal++)
        {
            DailyV1ActorTurnRosterEntry entry = roster[ordinal];
            if (entry == null || entry.RosterOrdinal != ordinal)
                return false;
            DailyV1NpcRuntimeOwners actor = entry.ActorOwner;
            if (actor != null
                && (!graph.ContainsExactNpcOwner(actor)
                    || !ReferenceEquals(graph.Definitions, actor.Definitions)
                    || actor.Lifecycle == null
                    || actor.Travel == null))
                return false;
            if (actor != null) rosterOwners.Add(actor);
        }

        // The runtime roster is the actor-turn authority for this boundary.
        // Checking only that each roster actor belongs to the graph would allow
        // a graph-owned actor omitted from the host roster to miss its turn.
        if (!graph.MatchesExactOwnerInstances(graph.Cities, rosterOwners))
            return false;

        foreach (DailyV1ActorTurnRosterEntry entry in roster)
        {
            DailyV1NpcRuntimeOwners actor = entry.ActorOwner;
            if (actor == null) continue;

            int rosterOrdinal = entry.RosterOrdinal;
            PersonId personId = entry.PersonId;
            if (actor.Lifecycle.LifeState != NpcLifeState.Alive)
            {
                RejectActorChoicesForPerson(actorChoiceStore, personId, absoluteDay, rosterOrdinal);
                continue;
            }

            bool hasPendingChoice = actorChoiceStore.TryGetNextPendingForActor(personId, out _);
            if (hasPendingChoice
                && !IsCurrentMaterializedPersonActor(
                    personStore, personId, actor.RuntimeId, absoluteDay))
            {
                RejectActorChoicesForPerson(actorChoiceStore, personId, absoluteDay, rosterOrdinal);
                continue;
            }

            if (hasPendingChoice && boundary.IsInSpatialTransit(actor, personId))
            {
                TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary);
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.Traveling);
                continue;
            }

            if (actor.Travel.IsTraveling)
            {
                TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary);
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.Traveling);
                continue;
            }

            if (boundary.IsOnActiveExpedition(actor))
            {
                TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary);
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.ExpeditionParticipant);
                continue;
            }

            if (boundary.IsReservedToday(actor))
            {
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.ReservedExpeditionActivity);
                continue;
            }

            boundary.EvaluateStatus(actor);

            if (actorChoiceStore.TryGetNextPendingForActor(personId, out _))
            {
                if (TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary))
                {
                    DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                        rosterOrdinal, ActorChoiceDeferralReason.ScheduledDirective);
                    continue;
                }

                if (boundary.TryProcessActorChoice(actor, rosterOrdinal))
                    continue;
            }

            if (merchantTradeEnabled)
                boundary.AdvanceMerchantTradeState(actor);

            if (TryProcessScheduledDirective(graph, actor, scheduledDirectiveStore, absoluteDay, boundary))
            {
                DeferActorChoiceForPerson(actorChoiceStore, personId, absoluteDay,
                    rosterOrdinal, ActorChoiceDeferralReason.ScheduledDirective);
                continue;
            }

            if (boundary.TryProcessActorChoice(actor, rosterOrdinal))
                continue;

            if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary
                && selectedActionBoundary.UsesSelectedDailyV1ActionExecution(actor))
            {
                if (!DailyV1SelectedActorActionExecutionStage.TryExecuteAutonomousAction(
                        graph, actor, personStore, selectedActionBoundary.RandomSource,
                        absoluteDay, selectedActionBoundary))
                    throw new InvalidOperationException(
                        "The exact selected Daily-v1 actor could not complete its Core-owned action turn.");
            }
            else
            {
                boundary.EvaluateAndExecuteAction(actor);
            }
        }

        RejectActorChoicesWithoutMaterializedTurn(actorChoiceStore, roster, absoluteDay);
        return true;
    }

    private static bool TryProcessScheduledDirective(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirectiveStore directiveStore,
        long absoluteDay,
        IDailyV1ActorTurnExecutionBoundary boundary)
    {
        if (directiveStore == null
            || !directiveStore.TryTakeDirective(actor.RuntimeId, out ScheduledDirective directive))
            return false;

        if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedActionBoundary
            && selectedActionBoundary.UsesSelectedDailyV1ActionExecution(actor)
            && directive.Mode == ScheduledDirectiveMode.RequestAction)
        {
            DailyV1SelectedActorActionExecutionStage.ProcessScheduledRequestAction(
                graph, actor, directive, selectedActionBoundary.RandomSource,
                absoluteDay, selectedActionBoundary);
        }
        else if (boundary is IDailyV1SelectedActorActionExecutionBoundary selectedForceBoundary
            && selectedForceBoundary.UsesSelectedDailyV1ActionExecution(actor)
            && directive.Mode == ScheduledDirectiveMode.ForceOutcome)
        {
            DailyV1SelectedActorActionExecutionStage.ProcessScheduledForceOutcome(
                graph, actor, directive, absoluteDay, selectedForceBoundary);
        }
        else
        {
            boundary.ProcessScheduledDirective(actor, directive);
        }
        return true;
    }

    private static bool IsCurrentMaterializedPersonActor(
        PersonStore personStore,
        PersonId personId,
        string actorRuntimeId,
        long absoluteDay)
    {
        if (personId == null
            || !personStore.TryGet(personId, out PersonRuntime person)
            || person.IsDeadAt(absoluteDay)
            || !person.IsMaterialized
            || !string.Equals(person.MaterializedNpcRuntimeId, actorRuntimeId, StringComparison.Ordinal)
            || !personStore.TryGetByMaterializedNpcRuntimeId(actorRuntimeId, out PersonRuntime byNpcId)
            || !ReferenceEquals(person, byNpcId))
            return false;
        return true;
    }

    private static void RejectActorChoicesForPerson(
        ActorChoiceStore store,
        PersonId personId,
        long absoluteDay,
        int rosterOrdinal)
    {
        if (personId == null) return;
        foreach (ActorChoiceInput input in store.PendingInputs)
        {
            if (input.PersonId.Equals(personId))
                RejectActorChoice(store, input, absoluteDay, rosterOrdinal,
                    ActorChoiceFailure.ActorUnavailable);
        }
    }

    private static void DeferActorChoiceForPerson(
        ActorChoiceStore store,
        PersonId personId,
        long absoluteDay,
        int rosterOrdinal,
        ActorChoiceDeferralReason reason)
    {
        if (!store.TryGetNextPendingForActor(personId, out ActorChoiceInput input)) return;
        if (!store.TryDefer(input.InputId, absoluteDay, rosterOrdinal, reason,
                out ActorChoiceStoreFailureCode failure))
            throw new InvalidOperationException(
                "Actor choice could not record its required deferral: " + failure + ".");
    }

    private static void RejectActorChoicesWithoutMaterializedTurn(
        ActorChoiceStore store,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        long absoluteDay)
    {
        HashSet<PersonId> presentActors = new HashSet<PersonId>();
        foreach (DailyV1ActorTurnRosterEntry entry in roster)
        {
            if (entry.ActorOwner != null && entry.PersonId != null)
                presentActors.Add(entry.PersonId);
        }

        foreach (ActorChoiceInput input in store.PendingInputs)
        {
            if (!presentActors.Contains(input.PersonId))
                RejectActorChoice(store, input, absoluteDay, roster.Count,
                    ActorChoiceFailure.ActorUnavailable);
        }
    }

    private static void RejectActorChoice(
        ActorChoiceStore store,
        ActorChoiceInput input,
        long absoluteDay,
        int rosterOrdinal,
        ActorChoiceFailure reason)
    {
        if (input == null)
            throw new InvalidOperationException(
                "Actor choice could not record its required rejection: missing input.");
        if (!store.TryReject(input.InputId, absoluteDay, rosterOrdinal, reason,
                out ActorChoiceStoreFailureCode failure))
            throw new InvalidOperationException(
                "Actor choice could not record its required rejection: " + failure + ".");
    }
}
