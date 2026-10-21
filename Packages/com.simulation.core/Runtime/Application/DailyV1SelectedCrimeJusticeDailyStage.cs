using System;
using System.Collections.Generic;

/// <summary>The existing four independently admitted Daily-v1 start-of-day scopes.</summary>
internal enum DailyV1SelectedCrimeJusticeDailyPhase
{
    ClearArrestedToday,
    AdvanceHiddenStatuses,
    AdvanceSentences,
    SyncWantedStatuses
}

/// <summary>
/// Host ports for the existing P12 daily section scope, exact Unity
/// compatibility projections, and presentation output. Core owns all
/// selected Crime/Justice decisions and writes the shared owners directly.
/// </summary>
internal interface IDailyV1SelectedCrimeJusticeDailyBoundary
    : DailyV1SelectedActionExecutionStage.ISelectedNpcOwnerMutationBoundary
{
    void ExecuteWithinDailyMutation(
        DailyV1SelectedCrimeJusticeDailyPhase phase,
        Action mutation);

    bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence);
    void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence);
    void Log(SimulationLogCategory category, string message);
    void LogWarning(string message);
}

/// <summary>
/// Core-owned selected Daily-v1 Crime/Justice start-of-day execution. The
/// phase boundaries intentionally remain four separate P12 reservations in
/// the established Justice, Crime, Justice, Justice order. The runtime uses
/// this stage only for an exact selected roster; a mixed roster stays on the
/// established Unity daily lifecycle.
/// </summary>
internal static class DailyV1SelectedCrimeJusticeDailyStage
{
    internal static bool TryRun(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> orderedRoster,
        bool crimeSystemPresent,
        bool justiceSystemPresent,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        if (graph == null || orderedRoster == null || boundary == null
            || !TryResolveStatus(graph.Definitions, graph.Definitions.Configuration?.hiddenStatus,
                out DailyV1StatusDefinition hiddenStatus)
            || !TryResolveStatus(graph.Definitions, graph.Definitions.Configuration?.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus)
            || !TryResolveStatus(graph.Definitions, graph.Definitions.Configuration?.freeStatus,
                out DailyV1StatusDefinition freeStatus)
            || !TryResolveStatus(graph.Definitions, graph.Definitions.Configuration?.wantedStatus,
                out DailyV1StatusDefinition wantedStatus)
            || !TryValidateRoster(graph, orderedRoster)
            || (justiceSystemPresent && !TryValidateJusticeOwners(graph)))
            return false;

        if (justiceSystemPresent)
        {
            boundary.ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
                () => ClearArrestedToday(graph.JusticeRecordsOwner));
        }

        if (crimeSystemPresent)
        {
            boundary.ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses,
                () => AdvanceHiddenStatuses(graph, orderedRoster, hiddenStatus, boundary));
        }

        if (justiceSystemPresent)
        {
            boundary.ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
                () => AdvanceSentences(graph, orderedRoster, arrestedStatus,
                    freeStatus, hiddenStatus, wantedStatus, boundary));
            boundary.ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses,
                () => SyncWantedStatuses(graph, orderedRoster, wantedStatus, boundary));
        }

        return true;
    }

    private static void ClearArrestedToday(DailyV1JusticeRecordsOwner justice)
    {
        foreach (DailyV1JusticeSentenceOwner sentence in justice.Sentences)
        {
            if (sentence == null || !sentence.TryClearArrestedToday())
                throw new InvalidOperationException(
                    "The selected Core Justice owner rejected its daily arrest-marker reset.");
        }
    }

    private static void AdvanceHiddenStatuses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        DailyV1StatusDefinition hiddenStatus,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        foreach (DailyV1ActorTurnRosterEntry entry in roster)
        {
            DailyV1NpcRuntimeOwners actor = entry?.ActorOwner;
            if (actor == null) continue;

            int currentDays = actor.HiddenState.RemainingDays;
            if (currentDays <= 0)
            {
                if (hiddenStatus != null
                    && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                        actor, new[] { hiddenStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary))
                    throw new InvalidOperationException(
                        "The selected Core Crime stage could not reconcile an expired hidden-status projection.");
                continue;
            }

            if (hiddenStatus != null && !actor.Status.Contains(hiddenStatus)
                && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                    actor, Array.Empty<DailyV1StatusDefinition>(), new[] { hiddenStatus }, boundary))
                throw new InvalidOperationException(
                    "The selected Core Crime stage could not install its hidden-status projection.");

            boundary.EnsureMutationAllowed(actor);
            int nextDays = UnityMathfCompatibility.Max(0, currentDays - 1);
            if (!actor.HiddenState.TrySetRemainingDays(nextDays))
                throw new InvalidOperationException(
                    "The selected Core Crime stage could not advance the hidden-day owner.");
            boundary.NotifyHiddenMutationCommitted(actor);

            if (nextDays == 0)
            {
                if (hiddenStatus != null
                    && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                        actor, new[] { hiddenStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary))
                    throw new InvalidOperationException(
                        "The selected Core Crime stage could not remove an expired hidden-status projection.");
                boundary.Log(SimulationLogCategory.Crime,
                    actor.Definition.Values.name + " nao esta mais escondido.");
            }
        }
    }

    private static void AdvanceSentences(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        DailyV1StatusDefinition arrestedStatus,
        DailyV1StatusDefinition freeStatus,
        DailyV1StatusDefinition hiddenStatus,
        DailyV1StatusDefinition wantedStatus,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        DailyV1JusticeRecordsOwner justice = graph.JusticeRecordsOwner;
        for (int index = justice.Sentences.Count - 1; index >= 0; index--)
        {
            DailyV1JusticeSentenceOwner sentence = justice.Sentences[index];
            DailyV1NpcRuntimeOwners target = FindNpc(graph, sentence.TargetRuntimeId);
            DailyV1CityRuntimeOwners city = FindCity(graph, sentence.CityRuntimeId);
            if (sentence == null || target == null || city == null || sentence.Warrant == null)
                throw new InvalidOperationException(
                    "The selected Core Justice sentence lost its exact NPC, City, or warrant owner.");

            if (sentence.Warrant.IsResolved)
            {
                ReleasePrisoner(graph, target, arrestedStatus, freeStatus,
                    hiddenStatus, wantedStatus, boundary);
                RemoveSentence(justice, sentence, boundary);
                boundary.Log(SimulationLogCategory.Justice,
                    target.Definition.Values.name + " foi libertado porque seu mandado nao esta mais ativo.");
                continue;
            }

            if (!IsArrested(target, arrestedStatus))
            {
                RemoveSentence(justice, sentence, boundary);
                SyncWantedStatus(graph, target, wantedStatus, boundary);
                continue;
            }

            if (!sentence.TryAdvanceDay())
                throw new InvalidOperationException(
                    "The selected Core Justice sentence rejected its daily decrement.");

            if (sentence.RemainingDays > 0) continue;

            if (!sentence.Warrant.TryResolve())
                throw new InvalidOperationException(
                    "The selected Core Justice warrant rejected sentence expiry resolution.");
            // Preserve JusticeSystem.ResolveWarrant's existing writer order:
            // resolve and synchronize the wanted projection before prisoner
            // release performs its own final synchronization.
            SyncWantedStatus(graph, target, wantedStatus, boundary);
            ReleasePrisoner(graph, target, arrestedStatus, freeStatus,
                hiddenStatus, wantedStatus, boundary);
            RemoveSentence(justice, sentence, boundary);
            boundary.Log(SimulationLogCategory.Justice,
                target.Definition.Values.name + " cumpriu sua pena em "
                + city.Definition.Values.cityName + " e foi libertado.");
        }

        foreach (DailyV1ActorTurnRosterEntry entry in roster)
        {
            DailyV1NpcRuntimeOwners actor = entry?.ActorOwner;
            if (actor == null || !IsArrested(actor, arrestedStatus)
                || justice.FindActiveSentence(actor.RuntimeId) != null)
                continue;

            ReleasePrisoner(graph, actor, arrestedStatus, freeStatus,
                hiddenStatus, wantedStatus, boundary);
            boundary.LogWarning(actor.Definition.Values.name
                + " estava preso sem sentenca ativa e foi libertado.");
        }

        SyncWantedStatuses(graph, roster, wantedStatus, boundary);
    }

    private static void SyncWantedStatuses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster,
        DailyV1StatusDefinition wantedStatus,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        foreach (DailyV1ActorTurnRosterEntry entry in roster)
            if (entry?.ActorOwner != null)
                SyncWantedStatus(graph, entry.ActorOwner, wantedStatus, boundary);
    }

    private static void SyncWantedStatus(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1StatusDefinition wantedStatus,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        if (wantedStatus == null) return;
        bool wanted = graph.JusticeRecordsOwner.FindActiveWantedRecords(actor.RuntimeId).Count > 0;
        bool hasStatus = actor.Status.Contains(wantedStatus);
        if (wanted == hasStatus) return;

        bool changed = wanted
            ? DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, Array.Empty<DailyV1StatusDefinition>(), new[] { wantedStatus }, boundary)
            : DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, new[] { wantedStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary);
        if (!changed)
            throw new InvalidOperationException(
                "The selected Core Justice stage could not synchronize the wanted-status projection.");
    }

    private static void ReleasePrisoner(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1StatusDefinition arrestedStatus,
        DailyV1StatusDefinition freeStatus,
        DailyV1StatusDefinition hiddenStatus,
        DailyV1StatusDefinition wantedStatus,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        if (arrestedStatus != null
            && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, new[] { arrestedStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary))
            throw new InvalidOperationException("The selected Core Justice stage could not clear arrest status.");
        if (freeStatus != null
            && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, Array.Empty<DailyV1StatusDefinition>(), new[] { freeStatus }, boundary))
            throw new InvalidOperationException("The selected Core Justice stage could not add free status.");
        if (!DailyV1SelectedActionExecutionStage.TryClearSelectedNpcHiddenState(actor, boundary))
            throw new InvalidOperationException("The selected Core Justice stage could not clear hidden state.");
        if (hiddenStatus != null
            && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, new[] { hiddenStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary))
            throw new InvalidOperationException("The selected Core Justice stage could not clear hidden status.");
        SyncWantedStatus(graph, actor, wantedStatus, boundary);
    }

    private static void RemoveSentence(
        DailyV1JusticeRecordsOwner justice,
        DailyV1JusticeSentenceOwner sentence,
        IDailyV1SelectedCrimeJusticeDailyBoundary boundary)
    {
        if (!justice.ContainsSentence(sentence)
            || !boundary.CanRemoveSentenceProjection(sentence))
            throw new InvalidOperationException(
                "The selected Core Justice sentence has no exact removable host projection.");
        if (!justice.TryRemoveSentence(sentence))
            throw new InvalidOperationException(
                "The selected Core Justice owner rejected its exact sentence removal.");
        boundary.CommitSentenceProjectionRemoval(sentence);
    }

    private static bool IsArrested(
        DailyV1NpcRuntimeOwners actor,
        DailyV1StatusDefinition arrestedStatus) =>
        arrestedStatus != null && actor.Status.Contains(arrestedStatus);

    private static DailyV1NpcRuntimeOwners FindNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId)
    {
        foreach (DailyV1NpcRuntimeOwners candidate in graph.Npcs)
            if (candidate != null && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                return graph.ContainsExactNpcOwner(candidate) ? candidate : null;
        return null;
    }

    private static DailyV1CityRuntimeOwners FindCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId)
    {
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (candidate != null && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                return graph.ContainsExactCityOwner(candidate) ? candidate : null;
        return null;
    }

    private static bool TryValidateRoster(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster)
    {
        HashSet<DailyV1NpcRuntimeOwners> seen = new HashSet<DailyV1NpcRuntimeOwners>();
        for (int index = 0; index < roster.Count; index++)
        {
            DailyV1ActorTurnRosterEntry entry = roster[index];
            if (entry == null || entry.RosterOrdinal != index) return false;
            DailyV1NpcRuntimeOwners owner = entry.ActorOwner;
            if (owner != null && (!graph.ContainsExactNpcOwner(owner)
                || !ReferenceEquals(owner.Definitions, graph.Definitions)
                || !seen.Add(owner))) return false;
        }

        if (seen.Count != graph.Npcs.Count) return false;
        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
            if (owner == null || !seen.Contains(owner)) return false;
        return true;
    }

    private static bool TryValidateJusticeOwners(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        DailyV1JusticeRecordsOwner justice = graph.JusticeRecordsOwner;
        if (justice == null) return false;
        foreach (DailyV1JusticeWantedRecordOwner warrant in justice.WantedRecords)
            if (warrant == null || FindNpc(graph, warrant.TargetRuntimeId) == null
                || FindCity(graph, warrant.CityRuntimeId) == null) return false;
        foreach (DailyV1JusticeSentenceOwner sentence in justice.Sentences)
            if (sentence == null || FindNpc(graph, sentence.TargetRuntimeId) == null
                || FindCity(graph, sentence.CityRuntimeId) == null
                || sentence.Warrant == null || !justice.ContainsWantedRecord(sentence.Warrant)) return false;
        return true;
    }

    private static bool TryResolveStatus(
        DailyV1EffectiveDefinitions definitions,
        string statusId,
        out DailyV1StatusDefinition status)
    {
        status = null;
        return string.IsNullOrWhiteSpace(statusId)
            || definitions.Statuses.TryGetValue(statusId, out status);
    }
}
