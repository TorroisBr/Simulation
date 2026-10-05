using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class P12CrimeJusticeInvalidationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void NpcStatusWitnessKeepsOneOwnerAndTracksVariableOrderedRowsAndHiddenState()
    {
        NpcStatusData first = SimulationTestFactory.CreateStatus("p12-cj-status-first");
        NpcStatusData second = SimulationTestFactory.CreateStatus("p12-cj-status-second");
        NpcRuntime npc = CreateNpc("p12-cj-status-npc", first, second);
        Assert.That(npc.TryBindP12CrimeJusticeMutationBoundary(_ => true, _ => { }), Is.True);
        P12CrimeJusticeCensusProvider.NpcStatusSectionProvider provider =
            new P12CrimeJusticeCensusProvider.NpcStatusSectionProvider(npc);

        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();
        Assert.That(initial.SectionId, Is.EqualTo("p12b.npc-status-crime-state/p12-cj-status-npc"));
        Assert.That(initial.SchemaVersion, Is.EqualTo(1));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(npc));
        Assert.That(initial.Cardinality, Is.EqualTo(1));
        Assert.That(initial.Revision, Is.Zero);
        Assert.That(npc.CurrentStatus, Is.EqualTo(new[] { first, second }));

        Assert.That(npc.CurrentStatus, Is.InstanceOf<IList<NpcStatusData>>());
        IList<NpcStatusData> statusView = (IList<NpcStatusData>)npc.CurrentStatus;
        Assert.That(statusView.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => statusView.Add(first));
        Assert.That(npc.CurrentStatus, Is.EqualTo(new[] { first, second }));

        npc.AddStatus(first);
        Assert.That(provider.GetCurrentCensus().Revision, Is.Zero, "a duplicate status is a semantic no-op");
        npc.AddStatus(SimulationTestFactory.CreateStatus("p12-cj-status-third"));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1));
        npc.HideForDays(3);
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(2));
        npc.HideForDays(2);
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(2), "a shorter hide duration is a no-op");
        Assert.That(npc.AdvanceHiddenDay(), Is.False, "one committed decrement remains while hidden");
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(3));
        npc.ClearHidden();
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(4));
        npc.RemoveStatus(first);
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(5));
        npc.RemoveStatus(first);
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(5), "removing an absent status is a no-op");
    }

    [Test]
    public void P12RosterReconciliationFollowsNpcStatusOwnerReplacementWithTheSameRuntimeId()
    {
        NpcRuntime original = CreateNpc("p12-cj-reused-runtime-id");
        SimulationRuntime runtime = CreateP12Runtime(null, new[] { original });
        string sectionId = P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor(original.RuntimeId);
        IOwnerSectionCensusProvider originalProvider = FindLifecycleProvider(runtime, sectionId);
        Assert.That(originalProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(original));
        AssertCensus(runtime);

        Assert.That(runtime.TryUnregisterNpc(original.RuntimeId, out WorldNpcRegistryFailure unregisterFailure),
            Is.True, unregisterFailure.ToString());
        Assert.That(FindLifecycleProviderOrNull(runtime, sectionId), Is.Null);

        NpcRuntime replacement = CreateNpc(original.RuntimeId);
        Assert.That(runtime.TryRegisterNpc(replacement, out WorldNpcRegistryFailure registerFailure),
            Is.True, registerFailure.ToString());
        IOwnerSectionCensusProvider replacementProvider = FindLifecycleProvider(runtime, sectionId);
        Assert.That(replacementProvider, Is.Not.SameAs(originalProvider));
        Assert.That(replacementProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(replacement));
        Assert.That(replacementProvider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure),
            Is.True, epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(2));
        AssertCensus(runtime);
    }

    [Test]
    public void P12BoundDirectNpcStatusWriteRejectsBeforeChangingRowsOrRevision()
    {
        NpcRuntime npc = CreateNpc("p12-cj-direct-status");
        SimulationRuntime runtime = CreateP12Runtime(null, new[] { npc });
        NpcStatusData status = SimulationTestFactory.CreateStatus("p12-cj-direct-status-value");

        Assert.Throws<InvalidOperationException>(() => npc.AddStatus(status));
        Assert.That(npc.CurrentStatus, Is.Empty);
        Assert.That(P12CrimeJusticeCensusProvider.CreateNpcStatusProviders(new[] { npc })[0]
            .GetCurrentCensus().Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void P12SuccessfulActionNotifiesActorAndTargetStatusLeavesImmediatelyAndNoOpsStayQuiet()
    {
        NpcRuntime actor = CreateNpc("p12-cj-action-actor");
        NpcRuntime target = CreateNpc("p12-cj-action-target");
        NpcStatusData actorStatus = SimulationTestFactory.CreateStatus("p12-cj-action-actor-status");
        NpcStatusData targetStatus = SimulationTestFactory.CreateStatus("p12-cj-action-target-status");
        NpcActionData action = SimulationTestFactory.CreateAction("p12-cj-action", NpcActionType.Normal);
        action.statusToAdd.Add(actorStatus);
        action.targetStatusToAdd.Add(targetStatus);
        actor.SetCurrentActionRuntime(new NpcActionRuntime(action, target));
        SimulationRuntime runtime = CreateP12Runtime(null, new[] { actor, target });
        MethodInfo execute = typeof(SimulationRuntime).GetMethod(
            "TryExecuteCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(execute, Is.Not.Null);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());

        NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { actor });

        Assert.That(result.Success, Is.True);
        Assert.That(actor.CurrentStatus, Has.Member(actorStatus));
        Assert.That(target.CurrentStatus, Has.Member(targetStatus));
        Assert.That(actor.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(target.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 2), "each successful leaf publishes immediately for its exact NPC owner");
        AssertCensus(runtime);

        result = (NpcActionResult)execute.Invoke(runtime, new object[] { actor });
        Assert.That(result.Success, Is.True);
        Assert.That(actor.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(target.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long afterNoOp, out ContinuationCensusFailure noOpFailure), Is.True, noOpFailure.ToString());
        Assert.That(afterNoOp, Is.EqualTo(after), "a repeated status is a semantic no-op");
        AssertCensus(runtime);
    }

    [Test]
    public void P12CrimeHideProviderAndSuccessStatusChangesUseSeparateImmediateOwnerNotifications()
    {
        CityRuntime city = CreateCity("p12-cj-hide-city");
        NpcRuntime actor = CreateNpc("p12-cj-hide-actor");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("p12-cj-hide-wanted");
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("p12-cj-hide-hidden");
        NpcStatusData afterHide = SimulationTestFactory.CreateStatus("p12-cj-hide-followup");
        city.AddImportantNpc(actor);
        JusticeSystem justice = CreateJustice("p12-cj-hide-justice");
        justice.CreateOrIncreaseWarrant(actor, city, 10f, 2);
        CrimeSystem crime = new CrimeSystem(justice, null, hidden);
        NpcDecisionSystem decisions = new NpcDecisionSystem(new List<INpcActionProvider> { crime });
        NpcActionData action = SimulationTestFactory.CreateAction("p12-cj-hide", NpcActionType.Hide, NpcActionCategory.Crime);
        action.statusToAdd.Add(hidden);
        action.statusToAdd.Add(afterHide);
        NpcRuntime target = CreateNpc("p12-cj-hide-target");
        action.targetStatusToAdd.Add(wanted);
        actor.SetCurrentActionRuntime(new NpcActionRuntime(action, target));
        SimulationRuntime runtime = CreateP12Runtime(
            new[] { city },
            new[] { actor, target },
            justice,
            crime,
            decisionSystem: decisions,
            configuration: CreateP12Configuration(crimeEnabled: true));
        MethodInfo execute = typeof(SimulationRuntime).GetMethod(
            "TryExecuteCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(execute, Is.Not.Null);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());

        NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { actor });

        Assert.That(result.Success, Is.True);
        Assert.That(actor.IsHidden, Is.True);
        Assert.That(actor.CurrentStatus, Does.Contain(hidden));
        Assert.That(actor.CurrentStatus, Does.Contain(afterHide));
        Assert.That(target.CurrentStatus, Does.Contain(wanted));
        Assert.That(actor.P12CrimeJusticeRevision, Is.EqualTo(3),
            "Hide commits its timer and hidden status before post-provider action status effects");
        Assert.That(target.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 4));
        AssertCensus(runtime);
    }

    [Test]
    public void P12FailedEscapeActionNotifiesTheJusticeOwnerBeforeReturningFailure()
    {
        CityRuntime city = CreateCity("p12-cj-failed-escape-city");
        NpcRuntime guard = CreateNpc("p12-cj-failed-escape-guard");
        NpcRuntime target = CreateNpc("p12-cj-failed-escape-target");
        JusticeSystem justice = CreateJustice("p12-cj-failed-escape-justice");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        PrisonSentenceRuntime sentence = ReadRows<PrisonSentenceRuntime>(justice, "prisonSentences")[0];
        CrimeSystem crime = new CrimeSystem(justice, null, null);
        FixedRandomSource randomSource = new FixedRandomSource(0.9f);
        NpcDecisionSystem decisions = new NpcDecisionSystem(
            new List<INpcActionProvider> { crime },
            randomSource);
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-cj-failed-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        action.canFail = true;
        action.baseSuccessChance = 0f;
        SimulationRuntime runtime = CreateP12Runtime(
            new[] { city },
            new[] { guard, target },
            justice,
            crime,
            decisionSystem: decisions,
            configuration: CreateP12Configuration(crimeEnabled: true),
            randomSource: randomSource);
        MethodInfo execute = typeof(SimulationRuntime).GetMethod(
            "TryExecuteCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(execute, Is.Not.Null);
        target.SetCurrentActionRuntime(new NpcActionRuntime(action));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());

        NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { target });

        Assert.That(result.Success, Is.False);
        Assert.That(sentence.FailedEscapeAttempts, Is.EqualTo(1));
        Assert.That(sentence.RemainingDays, Is.EqualTo(6));
        Assert.That(justice.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1));
        AssertCensus(runtime);
    }

    [Test]
    public void P12GuardArrestActionNotifiesJusticeAndTargetStatusOwners()
    {
        CityRuntime city = CreateCity("p12-cj-guard-arrest-city");
        NpcRuntime guard = new NpcRuntime(
            "p12-cj-guard-arrest-guard",
            SimulationTestFactory.CreateNpc("p12-cj-guard-arrest-guard-definition", NpcJobType.Guard));
        NpcRuntime target = CreateNpc("p12-cj-guard-arrest-target");
        city.AddImportantNpc(guard);
        city.AddImportantNpc(target);
        JusticeSystem justice = CreateJustice("p12-cj-guard-arrest-justice");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        GuardSystem guardSystem = new GuardSystem(justice, null);
        NpcDecisionSystem decisions = new NpcDecisionSystem(new List<INpcActionProvider> { guardSystem });
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-cj-guard-arrest", NpcActionType.Arrest, NpcActionCategory.Justice);
        guard.SetCurrentActionRuntime(new NpcActionRuntime(action, target));
        SimulationRuntime runtime = CreateP12Runtime(
            new[] { city },
            new[] { guard, target },
            justice,
            decisionSystem: decisions,
            configuration: CreateP12Configuration(guardCrimeEnabled: true));
        MethodInfo execute = typeof(SimulationRuntime).GetMethod(
            "TryExecuteCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(execute, Is.Not.Null);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());

        NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { guard });

        Assert.That(result.Success, Is.True);
        Assert.That(justice.IsArrested(target), Is.True);
        Assert.That(ReadRows<PrisonSentenceRuntime>(justice, "prisonSentences"), Has.Count.EqualTo(1));
        Assert.That(target.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(justice.P12CrimeJusticeRevision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 2));
        AssertCensus(runtime);
    }

    [Test]
    public void P12ForcedEscapeDirectiveScopesJusticeAndStatusWritesBeforeDirectiveSucceeds()
    {
        CityRuntime city = CreateCity("p12-cj-forced-escape-city");
        NpcRuntime actor = CreateNpc("p12-cj-forced-escape-actor");
        NpcRuntime guard = CreateNpc("p12-cj-forced-escape-guard");
        JusticeSystem justice = CreateJustice("p12-cj-forced-escape-justice");
        justice.CreateOrIncreaseWarrant(actor, city, 12f, 4);
        Assert.That(justice.Arrest(guard, actor, city), Is.True);
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-cj-forced-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        action.statusToRemove.Add(ReadStatus("arrestedStatus", justice));
        ScheduledDirective directive = new ScheduledDirective(
            "p12-cj-forced-escape-directive",
            1L,
            ScheduledDirectiveMode.ForceOutcome,
            ScheduledDirectiveOperation.EscapePrison,
            actor.RuntimeId,
            action);
        SimulationTime time = new SimulationTime();
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(time);
        Assert.That(store.Add(directive), Is.True);
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        Assert.That(registry.RegisterNpc(guard), Is.True);
        ScheduledDirectiveSystem directives = new ScheduledDirectiveSystem(store, registry);
        CrimeSystem crime = new CrimeSystem(justice, null, null);
        SimulationRuntime runtime = CreateP12Runtime(
            new[] { city },
            new[] { actor, guard },
            justice,
            crime,
            configuration: CreateP12Configuration(crimeEnabled: true),
            scheduledDirectiveSystem: directives,
            simulationTime: time);

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());

        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
        Assert.That(justice.IsArrested(actor), Is.False);
        Assert.That(actor.CurrentStatus, Does.Contain(ReadStatus("freeStatus", justice)));
        Assert.That(actor.P12CrimeJusticeRevision, Is.GreaterThan(0));
        Assert.That(justice.P12CrimeJusticeRevision, Is.GreaterThan(0));
        AssertCensus(runtime);
    }

    [Test]
    public void DailyCrimeHiddenExpiryCommitsOneNpcSectionAndPreservesCensus()
    {
        CityRuntime city = CreateCity("p12-cj-daily-city");
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("p12-cj-daily-hidden");
        NpcRuntime npc = CreateNpc("p12-cj-daily-npc");
        npc.HideForDays(0);
        npc.AddStatus(hidden);
        CrimeSystem crime = new CrimeSystem(null, null, hidden);
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { npc }, crime: crime);
        P12CrimeJusticeCensusProvider.NpcStatusSectionProvider provider =
            new P12CrimeJusticeCensusProvider.NpcStatusSectionProvider(npc);

        Assert.That(provider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(runtime.TryAdvanceDays(2, out int advanced, out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(advanced, Is.EqualTo(2));
        Assert.That(npc.IsHidden, Is.False);
        Assert.That(npc.HiddenDaysRemaining, Is.Zero);
        Assert.That(npc.CurrentStatus, Has.No.Member(hidden));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(3));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure),
            Is.True, epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(2), "each committed day uses one reserved owner batch");
        AssertCensus(runtime);
    }

    [Test]
    public void JusticeWitnessCountsExactRowsRejectsAliasesAndTracksDailyMutableRowCommits()
    {
        CityRuntime city = CreateCity("p12-cj-justice-city");
        NpcRuntime guard = CreateNpc("p12-cj-justice-guard");
        NpcRuntime target = CreateNpc("p12-cj-justice-target");
        JusticeSystem justice = CreateJustice("p12-cj-justice");
        WantedRecordRuntime warrant = justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(justice.CreateOrIncreaseWarrant(target, city, 3f, 1), Is.SameAs(warrant));
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        PrisonSentenceRuntime sentence = ReadRows<PrisonSentenceRuntime>(justice, "prisonSentences")[0];
        Assert.That(justice.GetActiveWarrants(target), Has.Member(warrant));

        P12CrimeJusticeCensusProvider.JusticeRecordsSectionProvider provider = CreateJusticeProvider(
            justice,
            npc => ReferenceEquals(npc, guard) || ReferenceEquals(npc, target),
            candidateCity => ReferenceEquals(candidateCity, city));
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();
        Assert.That(initial.SectionId, Is.EqualTo(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId));
        Assert.That(initial.SchemaVersion, Is.EqualTo(1));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(justice));
        Assert.That(initial.Cardinality, Is.EqualTo(2));
        Assert.That(initial.Revision, Is.Zero);

        List<WantedRecordRuntime> wantedRows = ReadRows<WantedRecordRuntime>(justice, "wantedRecords");
        wantedRows.Add(warrant);
        Assert.Throws<InvalidOperationException>(() => provider.GetCurrentCensus(),
            "duplicate aliases cannot masquerade as distinct wanted rows");
        wantedRows.RemoveAt(wantedRows.Count - 1);

        List<PrisonSentenceRuntime> sentenceRows = ReadRows<PrisonSentenceRuntime>(justice, "prisonSentences");
        sentenceRows.Add(sentence);
        Assert.Throws<InvalidOperationException>(() => provider.GetCurrentCensus(),
            "the exact same mutable sentence alias cannot be counted twice");
        sentenceRows.RemoveAt(sentenceRows.Count - 1);

        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { guard, target }, justice: justice);
        Assert.That(runtime.TryAdvanceDays(1, out int advanced, out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(advanced, Is.EqualTo(1));
        OwnerSectionCensusWitness afterDaily = provider.GetCurrentCensus();
        Assert.That(afterDaily.Cardinality, Is.EqualTo(2));
        Assert.That(afterDaily.Revision, Is.GreaterThan(initial.Revision));
        Assert.That(sentence.WasArrestedToday, Is.False);
        Assert.That(sentence.RemainingDays, Is.EqualTo(4), "the existing daily semantics preserve the full term on the arrest day");
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure),
            Is.True, epochFailure.ToString());
        Assert.That(epoch, Is.GreaterThan(0));
        AssertCensus(runtime);
    }

    [Test]
    public void P12BindsReturnedJusticeRowAliasesAndRejectsUnsupportedDirectMutation()
    {
        CityRuntime city = CreateCity("p12-cj-alias-city");
        NpcRuntime guard = CreateNpc("p12-cj-alias-guard");
        NpcRuntime target = CreateNpc("p12-cj-alias-target");
        JusticeSystem justice = CreateJustice("p12-cj-alias-justice");
        WantedRecordRuntime warrant = justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        PrisonSentenceRuntime sentence = ReadRows<PrisonSentenceRuntime>(justice, "prisonSentences")[0];
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { guard, target }, justice: justice);

        Assert.That(justice.GetActiveWarrants(target)[0], Is.SameAs(warrant));
        Assert.Throws<InvalidOperationException>(() => justice.GetActiveWarrants(target)[0].AddPenalty(5f, 2));
        Assert.Throws<InvalidOperationException>(() => sentence.RegisterFailedEscape(2));
        Assert.That(warrant.Bounty, Is.EqualTo(12f));
        Assert.That(warrant.SentenceDays, Is.EqualTo(4));
        Assert.That(sentence.FailedEscapeAttempts, Is.Zero);
        Assert.That(sentence.RemainingDays, Is.EqualTo(4));
        Assert.That(justice.P12CrimeJusticeRevision, Is.Zero);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out _, out ContinuationCensusFailure epochFailure),
            Is.False);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void P12DailyProfileKeepsCrimeAndJusticeP18ReceiptOwnersExplicitlyEmpty()
    {
        CityRuntime city = CreateCity("p12-cj-receipts-city");
        NpcRuntime npc = CreateNpc("p12-cj-receipts-npc");
        JusticeSystem justice = CreateJustice("p12-cj-receipts-justice");
        CrimeSystem crime = new CrimeSystem(justice, null, null);
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { npc }, justice, crime);
        OwnerSectionCensusWitness crimeBefore = new P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider(crime)
            .GetCurrentCensus();
        OwnerSectionCensusWitness justiceBefore = new P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider(justice)
            .GetCurrentCensus();
        AssertExplicitEmptyReceiptWitness(crimeBefore, P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId, crime);
        AssertExplicitEmptyReceiptWitness(justiceBefore, P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId, justice);

        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 1L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            operation, new List<NpcRuntime> { npc }, 0, out BoundaryContinuationStep crimeStep, out _), Is.True);
        BoundaryContinuationManifest crimeManifest = CreateManifest(operation, crimeStep);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            crimeManifest, crimeStep, new List<NpcRuntime> { npc }, out IBoundaryContinuationStepCommit crimeCommit, out _), Is.True);
        Assert.That(crimeCommit.TryCommit(out TimelineFailure crimeFailure), Is.False);
        Assert.That(crimeFailure, Is.EqualTo(TimelineFailure.UnsupportedProfile));

        DailyBoundaryOperation justiceOperation = new DailyBoundaryOperation("world", "intraday", 2L);
        Assert.That(justice.TryCreateBeginDayStep(justiceOperation, 0, out BoundaryContinuationStep justiceStep, out _), Is.True);
        BoundaryContinuationManifest justiceManifest = CreateManifest(justiceOperation, justiceStep);
        Assert.That(justice.TryPrepareBeginDayStep(
            justiceManifest, justiceStep, out IBoundaryContinuationStepCommit justiceCommit, out _), Is.True);
        Assert.That(justiceCommit.TryCommit(out TimelineFailure justiceFailure), Is.False);
        Assert.That(justiceFailure, Is.EqualTo(TimelineFailure.UnsupportedProfile));

        Assert.That(new P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider(crime)
            .GetCurrentCensus().Revision, Is.Zero);
        Assert.That(new P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider(justice)
            .GetCurrentCensus().Revision, Is.Zero);
        AssertCensus(runtime);
    }

    private static SimulationRuntime CreateP12Runtime(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        JusticeSystem justice = null,
        CrimeSystem crime = null,
        NpcDecisionSystem decisionSystem = null,
        EffectiveSimulationConfiguration configuration = null,
        IAuthoritativeRandomSource randomSource = null,
        ScheduledDirectiveSystem scheduledDirectiveSystem = null,
        SimulationTime simulationTime = null)
    {
        return new SimulationRuntime(
            simulationTime ?? new SimulationTime(),
            cities,
            npcs,
            scheduledDirectiveSystem: scheduledDirectiveSystem,
            justiceSystem: justice,
            crimeSystem: crime,
            npcDecisionSystem: decisionSystem,
            configuration: configuration ?? SimulationConfigurationDefaults.Create(),
            randomSource: randomSource,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static NpcStatusData ReadStatus(string fieldName, JusticeSystem justice)
    {
        FieldInfo field = typeof(JusticeSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (NpcStatusData)field.GetValue(justice);
    }

    private static EffectiveSimulationConfiguration CreateP12Configuration(
        bool crimeEnabled = false,
        bool guardCrimeEnabled = false)
    {
        EffectiveSimulationConfiguration defaults = SimulationConfigurationDefaults.Create();
        return new EffectiveSimulationConfiguration(
            defaults.Population,
            defaults.Economy,
            defaults.Travel,
            new EffectiveCrimeConfiguration(crimeEnabled, crimeEnabled),
            new EffectiveGuardCrimeConfiguration(guardCrimeEnabled),
            defaults.NaturalMortality,
            defaults.AggregateDemography,
            defaults.MerchantTrade,
            defaults.CommercialKnowledge);
    }

    private sealed class FixedRandomSource : IAuthoritativeRandomSource
    {
        private readonly float value;
        private readonly DeterministicRandomSource streamSource = new DeterministicRandomSource(17);

        public FixedRandomSource(float value) => this.value = value;

        public float NextUnit(string streamKey, long drawIndex = 0L) => value;

        public DeterministicRandomStream CreateStream(string streamKey) => streamSource.CreateStream(streamKey);
    }

    private static CityRuntime CreateCity(string runtimeId) =>
        new CityRuntime(
            runtimeId,
            SimulationTestFactory.CreateCityData("definition-" + runtimeId),
            new SpatialLocationRuntime("location-" + runtimeId));

    private static NpcRuntime CreateNpc(string runtimeId, params NpcStatusData[] statuses)
    {
        NpcData definition = SimulationTestFactory.CreateNpc("definition-" + runtimeId);
        if (statuses != null) definition.statusPadrao.AddRange(statuses);
        return new NpcRuntime(runtimeId, definition);
    }

    private static JusticeSystem CreateJustice(string prefix) => new JusticeSystem(
        SimulationTestFactory.CreateStatus(prefix + "-free"),
        SimulationTestFactory.CreateStatus(prefix + "-wanted"),
        SimulationTestFactory.CreateStatus(prefix + "-arrested"),
        null);

    private static P12CrimeJusticeCensusProvider.JusticeRecordsSectionProvider CreateJusticeProvider(
        JusticeSystem justice,
        Func<NpcRuntime, bool> isNpcInstalled,
        Func<CityRuntime, bool> isCityInstalled) =>
        new P12CrimeJusticeCensusProvider.JusticeRecordsSectionProvider(justice, isNpcInstalled, isCityInstalled);

    private static IOwnerSectionCensusProvider FindLifecycleProvider(SimulationRuntime runtime, string sectionId)
    {
        IOwnerSectionCensusProvider provider = FindLifecycleProviderOrNull(runtime, sectionId);
        Assert.That(provider, Is.Not.Null, "Expected lifecycle owner provider " + sectionId);
        return provider;
    }

    private static IOwnerSectionCensusProvider FindLifecycleProviderOrNull(SimulationRuntime runtime, string sectionId)
    {
        FieldInfo protocolField = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(protocolField, Is.Not.Null);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)protocolField.GetValue(runtime);
        foreach (IOwnerSectionCensusProvider provider in protocol.LifecycleOwnerFamilyProviders)
        {
            if (provider.GetCurrentCensus().SectionId == sectionId) return provider;
        }
        return null;
    }

    private static List<T> ReadRows<T>(JusticeSystem justice, string fieldName)
    {
        FieldInfo field = typeof(JusticeSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        List<T> rows = field.GetValue(justice) as List<T>;
        Assert.That(rows, Is.Not.Null);
        return rows;
    }

    private static void AssertExplicitEmptyReceiptWitness(
        OwnerSectionCensusWitness witness,
        string sectionId,
        object owner)
    {
        Assert.That(witness.SectionId, Is.EqualTo(sectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(1));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(1));
        Assert.That(witness.Revision, Is.Zero);
    }

    private static void AssertCensus(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True,
            failure.ToString());
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyBoundaryOperation operation,
        BoundaryContinuationStep step) =>
        new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "configuration",
            new[] { step },
            "content");
}
