using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedCrimeJusticeDailyStageTests
{
    [Test]
    public void AdvancePreservesFourScopesAndRunsHiddenAndSentenceTransitionsOnCoreOwners()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraphWithSentence(
            out DailyV1NpcRuntimeOwners actor,
            out DailyV1JusticeRecordsOwner justice,
            out DailyV1JusticeWantedRecordOwner warrant,
            out DailyV1JusticeSentenceOwner sentence);
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.freeStatus)), Is.False,
            "The helper models an arrested NPC without the captured fixture's initial Free status.");
        FakeBoundary boundary = new FakeBoundary(graph);
        IReadOnlyList<DailyV1ActorTurnRosterEntry> roster = CreateOrderedRoster(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, roster, crimeSystemPresent: true, justiceSystemPresent: true, boundary), Is.True);

        Assert.That(boundary.Phases, Is.EqualTo(new[]
        {
            DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
            DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses
        }));
        Assert.That(actor.HiddenState.RemainingDays, Is.Zero);
        Assert.That(actor.Status.Contains(graph.Definitions.Statuses[
            graph.Definitions.Configuration.arrestedStatus]), Is.False);
        Assert.That(actor.Status.Contains(graph.Definitions.Statuses[
            graph.Definitions.Configuration.hiddenStatus]), Is.False);
        Assert.That(actor.Status.Contains(graph.Definitions.Statuses[
            graph.Definitions.Configuration.freeStatus]), Is.True);
        Assert.That(actor.Status.Contains(graph.Definitions.Statuses[
            graph.Definitions.Configuration.wantedStatus]), Is.False);
        Assert.That(warrant.IsResolved, Is.True);
        Assert.That(justice.Sentences, Is.Empty);
        Assert.That(justice.Revision, Is.EqualTo(4));
        Assert.That(actor.CrimeJusticeRevision, Is.EqualTo(5));
        Assert.That(boundary.Logs, Is.EqualTo(new[]
        {
            (SimulationLogCategory.Crime, actor.Definition.Values.name + " nao esta mais escondido."),
            (SimulationLogCategory.Justice, actor.Definition.Values.name + " cumpriu sua pena em "
                + graph.Cities[0].Definition.Values.cityName + " e foi libertado.")
        }));
        Assert.That(sentence.RemainingDays, Is.Zero);
    }

    [Test]
    public void IncompleteSelectedRosterIsRejectedBeforeAnyDailyScope()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraphWithSentence(
            out _, out _, out _, out _);
        FakeBoundary boundary = new FakeBoundary(graph);
        IReadOnlyList<DailyV1ActorTurnRosterEntry> incomplete =
            Array.Empty<DailyV1ActorTurnRosterEntry>();

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, incomplete, crimeSystemPresent: true, justiceSystemPresent: true, boundary), Is.False);
        Assert.That(boundary.Phases, Is.Empty);
    }

    [Test]
    public void ActiveSentenceRemainsArrestedAndAdvancesExactlyOneDay()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1JusticeRecordsOwner justice,
            out DailyV1JusticeWantedRecordOwner warrant, out DailyV1JusticeSentenceOwner sentence,
            remainingDays: 3, hidden: false, wasArrestedToday: false);
        long actorRevision = actor.CrimeJusticeRevision;
        long justiceRevision = justice.Revision;
        FakeBoundary boundary = new FakeBoundary(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, CreateOrderedRoster(graph), true, true, boundary), Is.True);

        Assert.That(sentence.RemainingDays, Is.EqualTo(2));
        Assert.That(sentence.IsActive, Is.True);
        Assert.That(warrant.IsResolved, Is.False);
        Assert.That(justice.Sentences, Is.EqualTo(new[] { sentence }));
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.arrestedStatus)), Is.True);
        Assert.That(actor.CrimeJusticeRevision, Is.EqualTo(actorRevision));
        Assert.That(justice.Revision, Is.EqualTo(justiceRevision + 1),
            "Only the sentence countdown commits a Justice owner revision.");
    }

    [Test]
    public void ResolvedWarrantReleasesPrisonerAndRemovesSentence()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1JusticeRecordsOwner justice,
            out DailyV1JusticeWantedRecordOwner warrant, out DailyV1JusticeSentenceOwner sentence,
            remainingDays: 4, hidden: false, resolvedWarrant: true, wasArrestedToday: false);
        FakeBoundary boundary = new FakeBoundary(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, CreateOrderedRoster(graph), true, true, boundary), Is.True);

        Assert.That(warrant.IsResolved, Is.True);
        Assert.That(justice.Sentences, Is.Empty);
        Assert.That(sentence.RemainingDays, Is.EqualTo(4), "An inactive warrant is cleaned up without decrementing its sentence.");
        AssertReleased(actor, graph);
        Assert.That(boundary.Logs.Any(row => row.Message.Contains("mandado nao esta mais ativo")), Is.True);
    }

    [Test]
    public void SentenceForActorWhoIsNoLongerArrestedIsRemovedWithoutReleasingActor()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1JusticeRecordsOwner justice,
            out DailyV1JusticeWantedRecordOwner warrant, out DailyV1JusticeSentenceOwner sentence,
            remainingDays: 4, arrested: false, hidden: false, wasArrestedToday: false);
        FakeBoundary boundary = new FakeBoundary(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, CreateOrderedRoster(graph), true, true, boundary), Is.True);

        Assert.That(justice.Sentences, Is.Empty);
        Assert.That(sentence.RemainingDays, Is.EqualTo(4));
        Assert.That(warrant.IsResolved, Is.False);
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.arrestedStatus)), Is.False);
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.wantedStatus)), Is.True);
    }

    [Test]
    public void ArrestedActorWithoutActiveSentenceIsReleased()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1JusticeRecordsOwner justice,
            out _, out _, addSentence: false, hidden: false, wanted: false);
        FakeBoundary boundary = new FakeBoundary(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, CreateOrderedRoster(graph), true, true, boundary), Is.True);

        Assert.That(justice.Sentences, Is.Empty);
        AssertReleased(actor, graph);
        Assert.That(boundary.Logs.Any(row => row.Message.Contains("sem sentenca ativa")), Is.True);
    }

    [Test]
    public void CrimeOnlyJusticeOnlyAndDisabledSubsystemsKeepTheirOwnScopes()
    {
        DailyV1SelectedRuntimeOwnerGraph crimeGraph = CreateGraph(
            out DailyV1NpcRuntimeOwners crimeActor, out _, out _, out _,
            addSentence: false, hiddenDays: 3, hidden: true);
        FakeBoundary crimeBoundary = new FakeBoundary(crimeGraph);
        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            crimeGraph, CreateOrderedRoster(crimeGraph), true, false, crimeBoundary), Is.True);
        Assert.That(crimeBoundary.Phases, Is.EqualTo(new[]
            { DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses }));
        Assert.That(crimeActor.HiddenState.RemainingDays, Is.EqualTo(2));

        DailyV1SelectedRuntimeOwnerGraph justiceGraph = CreateGraph(
            out DailyV1NpcRuntimeOwners justiceActor, out _, out _, out DailyV1JusticeSentenceOwner activeSentence,
            remainingDays: 3, hiddenDays: 2, hidden: true, wasArrestedToday: false);
        FakeBoundary justiceBoundary = new FakeBoundary(justiceGraph);
        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            justiceGraph, CreateOrderedRoster(justiceGraph), false, true, justiceBoundary), Is.True);
        Assert.That(justiceBoundary.Phases, Is.EqualTo(new[]
        {
            DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
            DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses
        }));
        Assert.That(activeSentence.RemainingDays, Is.EqualTo(2));
        Assert.That(justiceActor.HiddenState.RemainingDays, Is.EqualTo(2));

        DailyV1SelectedRuntimeOwnerGraph disabledGraph = CreateGraph(
            out DailyV1NpcRuntimeOwners disabledActor, out _, out _, out _,
            addSentence: false, arrested: false, hidden: false, wanted: false);
        long actorRevision = disabledActor.CrimeJusticeRevision;
        FakeBoundary disabledBoundary = new FakeBoundary(disabledGraph);
        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            disabledGraph, CreateOrderedRoster(disabledGraph), false, false, disabledBoundary), Is.True);
        Assert.That(disabledBoundary.Phases, Is.Empty);
        Assert.That(disabledActor.CrimeJusticeRevision, Is.EqualTo(actorRevision));
    }

    [Test]
    public void NoOpDailyPassDoesNotAdvanceOwnerRevisions()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            out DailyV1NpcRuntimeOwners actor, out DailyV1JusticeRecordsOwner justice,
            out _, out _, addSentence: false, arrested: false, hidden: false, wanted: false);
        long actorRevision = actor.CrimeJusticeRevision;
        long justiceRevision = justice.Revision;
        FakeBoundary boundary = new FakeBoundary(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, CreateOrderedRoster(graph), true, true, boundary), Is.True);

        Assert.That(boundary.Phases, Has.Count.EqualTo(4));
        Assert.That(actor.CrimeJusticeRevision, Is.EqualTo(actorRevision));
        Assert.That(justice.Revision, Is.EqualTo(justiceRevision));
    }

    [Test]
    public void UnresolvedJusticeLinksRejectCoreStageBeforeMutation()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraphWithSentence(
            out DailyV1NpcRuntimeOwners actor, out DailyV1JusticeRecordsOwner justice, out _, out _);
        Assert.That(justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            "missing-daily-v1-npc", graph.Cities[0].RuntimeId, 1f, 1)), Is.True);
        Assert.That(justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            actor.RuntimeId, "missing-daily-v1-city", 1f, 1)), Is.True);
        long actorRevision = actor.CrimeJusticeRevision;
        long justiceRevision = justice.Revision;
        FakeBoundary boundary = new FakeBoundary(graph);

        Assert.That(DailyV1SelectedCrimeJusticeDailyStage.TryRun(
            graph, CreateOrderedRoster(graph), true, true, boundary), Is.False);

        Assert.That(boundary.Phases, Is.Empty);
        Assert.That(actor.CrimeJusticeRevision, Is.EqualTo(actorRevision));
        Assert.That(justice.Revision, Is.EqualTo(justiceRevision));
        Assert.That(justice.Sentences, Has.Count.EqualTo(1));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraphWithSentence(
        out DailyV1NpcRuntimeOwners actor,
        out DailyV1JusticeRecordsOwner justice,
        out DailyV1JusticeWantedRecordOwner warrant,
        out DailyV1JusticeSentenceOwner sentence) =>
        CreateGraph(out actor, out justice, out warrant, out sentence);

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        out DailyV1NpcRuntimeOwners actor,
        out DailyV1JusticeRecordsOwner justice,
        out DailyV1JusticeWantedRecordOwner warrant,
        out DailyV1JusticeSentenceOwner sentence,
        int remainingDays = 1,
        int hiddenDays = 1,
        bool arrested = true,
        bool hidden = true,
        bool wanted = true,
        bool addSentence = true,
        bool resolvedWarrant = false,
        bool wasArrestedToday = true)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph baseGraph), Is.True);

        actor = baseGraph.Npcs[0];
        DailyV1CityRuntimeOwners city = baseGraph.Cities[0];
        DailyV1StatusDefinition arrestedStatus = definitions.Statuses[definitions.Configuration.arrestedStatus];
        DailyV1StatusDefinition hiddenStatus = definitions.Statuses[definitions.Configuration.hiddenStatus];
        DailyV1StatusDefinition wantedStatus = definitions.Statuses[definitions.Configuration.wantedStatus];
        if (arrested) actor.Status.TryRemove(definitions.Statuses[definitions.Configuration.freeStatus]);
        if (arrested) Assert.That(actor.Status.TryAdd(arrestedStatus), Is.True);
        if (hidden) Assert.That(actor.Status.TryAdd(hiddenStatus), Is.True);
        if (wanted) Assert.That(actor.Status.TryAdd(wantedStatus), Is.True);
        Assert.That(actor.HiddenState.TrySetRemainingDays(hidden ? hiddenDays : 0), Is.True);

        justice = new DailyV1JusticeRecordsOwner();
        warrant = addSentence
            ? new DailyV1JusticeWantedRecordOwner(actor.RuntimeId, city.RuntimeId,
                bounty: 12f, sentenceDays: 1, resolved: resolvedWarrant)
            : null;
        sentence = addSentence
            ? new DailyV1JusticeSentenceOwner(actor.RuntimeId, city.RuntimeId,
                warrant, remainingDays, failedEscapeAttempts: 0, wasArrestedToday: wasArrestedToday)
            : null;
        if (warrant != null) Assert.That(justice.TryAddWantedRecord(warrant), Is.True);
        if (sentence != null) Assert.That(justice.TryAddSentence(sentence), Is.True);
        Assert.That(justice.TryBindP12MutationBoundary(() => true, () => { }), Is.True);

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, baseGraph.Cities, baseGraph.Npcs, baseGraph.SpatialNetwork,
            justice, baseGraph.TravelParties, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }

    private static DailyV1StatusDefinition Status(
        DailyV1SelectedRuntimeOwnerGraph graph, string statusId) => graph.Definitions.Statuses[statusId];

    private static void AssertReleased(DailyV1NpcRuntimeOwners actor, DailyV1SelectedRuntimeOwnerGraph graph)
    {
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.arrestedStatus)), Is.False);
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.freeStatus)), Is.True);
        Assert.That(actor.HiddenState.RemainingDays, Is.Zero);
        Assert.That(actor.Status.Contains(Status(graph, graph.Definitions.Configuration.wantedStatus)), Is.False);
    }

    private static IReadOnlyList<DailyV1ActorTurnRosterEntry> CreateOrderedRoster(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        List<DailyV1ActorTurnRosterEntry> roster = new List<DailyV1ActorTurnRosterEntry>();
        for (int index = 0; index < graph.Npcs.Count; index++)
            roster.Add(new DailyV1ActorTurnRosterEntry(index, null, graph.Npcs[index]));
        return roster;
    }

    private sealed class FakeBoundary : IDailyV1SelectedCrimeJusticeDailyBoundary
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;

        internal List<DailyV1SelectedCrimeJusticeDailyPhase> Phases { get; } =
            new List<DailyV1SelectedCrimeJusticeDailyPhase>();
        internal List<(SimulationLogCategory Category, string Message)> Logs { get; } =
            new List<(SimulationLogCategory Category, string Message)>();

        internal FakeBoundary(DailyV1SelectedRuntimeOwnerGraph graph) => this.graph = graph;

        public DailyV1JusticeRecordsOwner JusticeRecordsOwner => graph.JusticeRecordsOwner;

        public void ExecuteWithinDailyMutation(
            DailyV1SelectedCrimeJusticeDailyPhase phase,
            Action mutation)
        {
            Phases.Add(phase);
            mutation();
        }

        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
            graph.ContainsExactNpcOwner(owner);

        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner)
        {
            if (!graph.ContainsExactNpcOwner(owner)
                || !owner.CanAdvanceCrimeJusticeRevision(1))
                throw new InvalidOperationException("Selected NPC mutation was not admitted.");
        }

        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners owner,
            DailyV1StatusDefinition status,
            bool added) => owner.NotifyCrimeJusticeMutationCommitted();

        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner) =>
            owner.NotifyCrimeJusticeMutationCommitted();

        public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
            graph.JusticeRecordsOwner.ContainsSentence(sentence);

        public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) { }

        public void Log(SimulationLogCategory category, string message) => Logs.Add((category, message));
        public void LogWarning(string message) => Logs.Add((SimulationLogCategory.Justice, message));
    }
}
