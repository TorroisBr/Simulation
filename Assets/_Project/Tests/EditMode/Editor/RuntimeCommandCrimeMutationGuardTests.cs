using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RuntimeGuardSystemEntryPointTests
{
    private SimulationConfigData configToCleanup;

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        if (configToCleanup != null)
        {
            UnityEngine.Object.DestroyImmediate(configToCleanup);
            configToCleanup = null;
        }

        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void RuntimeOwnedSystemsAndServicesBindToOneGuardOnly()
    {
        JusticeSystem justice = new JusticeSystem(null, null, null, null);
        CrimeSystem crime = new CrimeSystem(justice, null, null);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveSystem directives = new ScheduledDirectiveSystem(directiveStore, new RuntimeIdentityRegistry());
        NpcDecisionSystem decisions = new NpcDecisionSystem(null);
        WorldCommandService commands = new WorldCommandService();
        object guard = CreateGuard(false);
        object otherGuard = CreateGuard(false);

        object[] authorities = { justice, crime, directiveStore, directives, decisions, commands };
        foreach (object authority in authorities)
        {
            Assert.That(TryBind(authority, guard), Is.True, authority.GetType().Name);
            Assert.That(CanBind(authority, guard), Is.True, authority.GetType().Name);
            Assert.That(TryBind(authority, guard), Is.True, authority.GetType().Name);
            Assert.That(CanBind(authority, otherGuard), Is.False, authority.GetType().Name);
            Assert.That(TryBind(authority, otherGuard), Is.False, authority.GetType().Name);
        }
    }

    [Test]
    public void FaultedCrimeJusticeAndDecisionPathsRejectBeforeEffects()
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("guard-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("guard-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("guard-arrested");
        JusticeSystem justice = new JusticeSystem(free, wanted, arrested, null);
        CityRuntime city = SimulationTestFactory.CreateCity("guard-justice-city", "guard-justice-location");
        NpcRuntime target = new NpcRuntime("guard-justice-npc", SimulationTestFactory.CreateNpc("guard-justice-definition"));
        WantedRecordRuntime warrant = justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(warrant, Is.Not.Null);

        CountingRandomSource random = new CountingRandomSource();
        CrimeSystem crime = new CrimeSystem(justice, null, null, randomSource: random);
        object faultedGuard = CreateGuard(true);
        Assert.That(TryBind(crime, faultedGuard), Is.True);

        NpcActionData action = SimulationTestFactory.CreateAction("guard-crime-action", NpcActionType.Steal, NpcActionCategory.Crime);
        float utility = 17f;
        Assert.That(crime.CreateAction(target, action, ref utility), Is.Null);
        Assert.That(utility, Is.Zero);
        Assert.That(random.DrawCount, Is.Zero);
        NpcActionResult crimeResult = crime.TryExecuteAction(target, null);
        Assert.That(crimeResult.Success, Is.False);
        StringAssert.Contains("faulted", crimeResult.Message);
        Assert.That(crime.HandleActionFailure(target, null), Is.Null);
        Assert.Throws<InvalidOperationException>(() => crime.AdvanceHiddenStatuses(null));

        configToCleanup = ScriptableObject.CreateInstance<SimulationConfigData>();
        configToCleanup.initialWarrants.Add(new InitialWantedRecordConfig
        {
            target = SimulationTestFactory.CreateNpc("guard-initial-warrant-target"),
            city = SimulationTestFactory.CreateCityData("guard-initial-warrant-city")
        });
        int callbackCount = 0;
        Assert.Throws<InvalidOperationException>(() => justice.CreateInitialWarrants(
            configToCleanup,
            _ => { callbackCount++; return target; },
            _ => { callbackCount++; return city; }));
        Assert.That(callbackCount, Is.Zero);

        Assert.That(justice.CreateOrIncreaseWarrant(target, city, 30f, 9), Is.Null);
        Assert.That(justice.Arrest(target, target, city), Is.False);
        Assert.That(justice.EscapePrison(target, 10f), Is.False);
        Assert.That(justice.ApplyEscapeSuccess(target, 10f), Is.False);
        Assert.That(justice.RegisterFailedEscape(target, 3), Is.False);
        Assert.Throws<InvalidOperationException>(() => justice.BeginDay());
        Assert.Throws<InvalidOperationException>(() => justice.AdvanceSentences(null));
        Assert.Throws<InvalidOperationException>(() => justice.SyncWantedStatus(target));

        Assert.Throws<InvalidOperationException>(() => warrant.AddPenalty(50f, 20));
        Assert.That(warrant.Bounty, Is.EqualTo(12f));
        Assert.Throws<InvalidOperationException>(() => warrant.Resolve());
        Assert.That(warrant.IsActive, Is.True);

        PrisonSentenceRuntime sentence = new PrisonSentenceRuntime(target, city, warrant, 5);
        Assert.That(TryBind(sentence, faultedGuard), Is.True);
        Assert.Throws<InvalidOperationException>(() => sentence.AdvanceDay());
        Assert.Throws<InvalidOperationException>(() => sentence.RegisterFailedEscape(7));
        Assert.Throws<InvalidOperationException>(() => sentence.ClearArrestedToday());
        Assert.That(sentence.RemainingDays, Is.EqualTo(5));
        Assert.That(sentence.FailedEscapeAttempts, Is.Zero);
        Assert.That(sentence.WasArrestedToday, Is.True);

        CountingActionProvider provider = new CountingActionProvider();
        NpcDecisionSystem decisions = new NpcDecisionSystem(new System.Collections.Generic.List<INpcActionProvider> { provider }, random);
        Assert.That(TryBind(decisions, faultedGuard), Is.True);
        Assert.That(decisions.GetProviderForAction(action), Is.Null);
        Assert.That(decisions.ChooseAction(target, new System.Collections.Generic.List<NpcActionData> { action }), Is.Null);
        Assert.That(decisions.CreateRequestedAction(target, action), Is.Null);
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(random.DrawCount, Is.Zero);
    }

    [Test]
    public void FaultedScheduledDirectivePathsPreservePendingState()
    {
        SimulationTime time = new SimulationTime();
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(time);
        NpcActionData escape = SimulationTestFactory.CreateAction("guard-directive-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective existing = new ScheduledDirective(
            "guard-directive-existing",
            1L,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            "guard-directive-actor",
            escape);
        Assert.That(store.Add(existing), Is.True);
        ScheduledDirectiveSystem system = new ScheduledDirectiveSystem(store, new RuntimeIdentityRegistry());
        object faultedGuard = CreateGuard(true);
        Assert.That(TryBind(system, faultedGuard), Is.True);

        ScheduledDirective rejected = new ScheduledDirective(
            "guard-directive-rejected",
            2L,
            ScheduledDirectiveMode.ForceOutcome,
            ScheduledDirectiveOperation.EscapePrison,
            "guard-directive-actor",
            escape);
        Assert.That(store.Add(rejected), Is.False);
        Assert.That(existing.MarkSucceeded(1L), Is.False);
        Assert.Throws<InvalidOperationException>(() => system.PrepareDay(1L));
        Assert.That(system.TryTakeDirective(null, out ScheduledDirective taken), Is.False);
        Assert.That(taken, Is.Null);
        Assert.That(existing.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(rejected.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(store.GetPendingForDay(1L), Has.Count.EqualTo(1));
    }

    [Test]
    public void FaultedCommandServiceRejectsBeforeIdAndDirectCoreHandlerMutation()
    {
        SimulationRuntime world = new SimulationRuntime(new SimulationTime(), null, null);
        CoreWorldCommandDependencies dependencies = new CoreWorldCommandDependencies(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            world);
        RelocateNpcWorldCommandHandler handler = new RelocateNpcWorldCommandHandler(dependencies);
        WorldCommandIdAllocator ids = new WorldCommandIdAllocator();
        WorldCommandService service = new WorldCommandService(commandIdAllocator: ids);
        Assert.That(service.RegisterHandler(handler), Is.True);
        MarkRuntimeFaulted(world);
        Assert.That(service.RegisterHandler(new DeclareStackResourceWorldCommandHandler(dependencies)), Is.False);

        WorldCommand command = new WorldCommand(
            WorldCommandKind.RelocateNpc,
            WorldCommandOrigin.GM,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload("guard-npc", "guard-location"));
        WorldCommandResult result = service.Execute(command);
        Assert.That(result.Success, Is.False);
        Assert.That(result.WorldCommandId, Is.Null);
        Assert.That(result.Record, Is.Null);
        StringAssert.Contains("faulted", result.Diagnostic);
        Assert.That(service.RecordStore.Records, Is.Empty);
        Assert.That(service.RecordStore.Add(new WorldCommandRecord(
            "direct-world-command-record",
            0L,
            WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Declare,
            WorldCommandKind.RelocateNpc,
            true)), Is.False);
        Assert.That(service.RecordStore.Records, Is.Empty);
        Assert.That(ids.Allocate(), Is.EqualTo("world-command-000001"));

        IWorldCommandHandler[] coreHandlers =
        {
            handler,
            new DeclareStackResourceWorldCommandHandler(dependencies),
            new DeclareNotableItemWorldCommandHandler(dependencies),
            new AddLocalPlaceWorldCommandHandler(dependencies),
            new AddLocalConnectionWorldCommandHandler(dependencies),
            new GrantSiteKnowledgeWorldCommandHandler(dependencies),
            new GrantAdventureIntelWorldCommandHandler(dependencies),
            new ResolveConflictWorldCommandHandler(WorldCommandKind.ResolveConflict, dependencies),
            new ResolveConflictWorldCommandHandler(WorldCommandKind.PlaceOpposition, dependencies)
        };
        RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
        foreach (IWorldCommandHandler coreHandler in coreHandlers)
        {
            WorldCommandHandlerResult direct = coreHandler.Execute(
                null,
                new WorldCommandExecutionContext("direct-command-id", runtimeIds));
            Assert.That(direct.Success, Is.False, coreHandler.Kind.ToString());
            StringAssert.Contains("faulted", direct.Diagnostic);
        }

        Assert.That(runtimeIds.AllocateNpcId(), Is.EqualTo("npc-000001"));
    }

    private static object CreateGuard(bool faulted)
    {
        Type guardType = typeof(SimulationRuntime).Assembly.GetType("AuthoritativeMutationGuard", true);
        object guard = Activator.CreateInstance(guardType, true);
        if (faulted)
        {
            Type reasonType = guardType.Assembly.GetType("AuthoritativeMutationFaultReason", true);
            object reason = Enum.Parse(reasonType, "RollbackRestoreFailed");
            guardType.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(guard, new[] { reason });
        }

        return guard;
    }

    private static bool CanBind(object authority, object guard)
    {
        MethodInfo method = GetBindableInterface(guard).GetMethod("CanBindMutationGuard");
        return (bool)method.Invoke(authority, new[] { guard });
    }

    private static bool TryBind(object authority, object guard)
    {
        MethodInfo method = GetBindableInterface(guard).GetMethod("TryBindMutationGuard");
        return (bool)method.Invoke(authority, new[] { guard });
    }

    private static Type GetBindableInterface(object guard)
    {
        return guard.GetType().Assembly.GetType("IAuthoritativeMutationGuardBindable", true);
    }

    private static void MarkRuntimeFaulted(SimulationRuntime world)
    {
        Type reasonType = typeof(SimulationRuntime).Assembly.GetType("AuthoritativeMutationFaultReason", true);
        object reason = Enum.Parse(reasonType, "RollbackRestoreFailed");
        typeof(SimulationRuntime).GetMethod("MarkAuthoritativeMutationFaulted", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(world, new[] { reason });
    }

    private sealed class CountingRandomSource : IAuthoritativeRandomSource
    {
        public int DrawCount { get; private set; }

        public float NextUnit(string streamKey, long drawIndex = 0L)
        {
            DrawCount++;
            return 0.5f;
        }

        public DeterministicRandomStream CreateStream(string streamKey)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CountingActionProvider : INpcActionProvider
    {
        public int CallCount { get; private set; }

        public bool HandlesAction(NpcActionData action)
        {
            CallCount++;
            return true;
        }

        public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
        {
            CallCount++;
            return new NpcActionRuntime(action);
        }

        public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
        {
            CallCount++;
            return NpcActionResult.Succeeded();
        }
    }
}

public sealed class JusticeBeginDayBoundaryOwnerTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void BeginDayBoundaryStepCommitsClearAndReceiptWithExactReplay()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-step-guard");
        NpcRuntime target = CreateActor("justice-step-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        Assert.That(justice.WasArrestedToday(target), Is.True);

        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1L);
        Assert.That(justice.TryCreateBeginDayStep(operation, 0, out BoundaryContinuationStep step, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);

        Assert.That(justice.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        Assert.That(prepared.RetainedTimelineFacts, Is.Empty);
        Assert.That(prepared.RetainedSourceSignals, Is.Empty);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(justice.WasArrestedToday(target), Is.False);
        Assert.That(justice.TryResolveBeginDayReceipt(manifest, step, out JusticeBeginDayReceipt receipt, out failure), Is.True);
        Assert.That(receipt.OwnerRevisionBefore, Is.Zero);
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1L));

        Assert.That(justice.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(justice.WasArrestedToday(target), Is.False);
    }

    [Test]
    public void ExactReplayDoesNotClearArrestMadeAfterOriginalCommit()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime firstGuard = CreateActor("justice-replay-first-guard");
        NpcRuntime firstTarget = CreateActor("justice-replay-first-target");
        justice.CreateOrIncreaseWarrant(firstTarget, city, 12f, 4);
        Assert.That(justice.Arrest(firstGuard, firstTarget, city), Is.True);

        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 1L);
        Assert.That(justice.TryCreateBeginDayStep(operation, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit firstCommit, out _), Is.True);
        Assert.That(firstCommit.TryCommit(out _), Is.True);

        NpcRuntime secondGuard = CreateActor("justice-replay-second-guard");
        NpcRuntime secondTarget = CreateActor("justice-replay-second-target");
        justice.CreateOrIncreaseWarrant(secondTarget, city, 10f, 3);
        Assert.That(justice.Arrest(secondGuard, secondTarget, city), Is.True);

        Assert.That(justice.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit replay, out _), Is.True);
        Assert.That(replay.TryCommit(out _), Is.True);
        Assert.That(justice.WasArrestedToday(secondTarget), Is.True);
    }

    [Test]
    public void BeginDayBoundaryStepRejectsChangedDescriptorAndStaleOwnerSnapshot()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-stale-guard");
        NpcRuntime target = CreateActor("justice-stale-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(justice.Arrest(guard, target, city), Is.True);

        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 2L);
        Assert.That(justice.TryCreateBeginDayStep(operation, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out _), Is.True);

        justice.BeginDay();
        Assert.That(prepared.TryCommit(out TimelineFailure staleFailure), Is.False);
        Assert.That(staleFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        BoundaryContinuationStep changedStep = new BoundaryContinuationStep(
            0,
            step.StepId,
            step.OwnerId,
            step.OperationKind,
            step.OperationVersion,
            step.OwnerRevision + "changed",
            step.Payload);
        BoundaryContinuationManifest changedManifest = CreateManifest(operation, changedStep);
        Assert.That(justice.TryPrepareBeginDayStep(changedManifest, changedStep, out _, out TimelineFailure conflictFailure), Is.False);
        Assert.That(conflictFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void BeginDayReceiptUsesSemanticStepIdentityAndRejectsOrdinalDrift()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-identity-guard");
        NpcRuntime target = CreateActor("justice-identity-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 4);
        Assert.That(justice.Arrest(guard, target, city), Is.True);

        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 3L);
        Assert.That(justice.TryCreateBeginDayStep(operation, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareBeginDayStep(manifest, step, out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.True);
        Assert.That(justice.TryResolveBeginDayReceipt(
            manifest, step, out JusticeBeginDayReceipt receipt, out TimelineFailure failure), Is.True);
        Assert.That(receipt.ExecutionStepIdentity, Is.Not.EqualTo(manifest.GetExecutionStepIdentity(step)));

        BoundaryContinuationStep preceding = new BoundaryContinuationStep(
            0, "preceding-step", "test-owner", "test.operation", "1", "test-revision", string.Empty);
        BoundaryContinuationStep shifted = new BoundaryContinuationStep(
            1, step.StepId, step.OwnerId, step.OperationKind, step.OperationVersion,
            step.OwnerRevision, step.Payload, step.PersonId, step.Disposition);
        BoundaryContinuationManifest shiftedManifest = new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "configuration",
            new List<BoundaryContinuationStep> { preceding, shifted },
            "content");

        Assert.That(justice.TryResolveBeginDayReceipt(
            shiftedManifest, shifted, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        BoundaryContinuationManifest changedConfigurationManifest = new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "changed-configuration",
            new List<BoundaryContinuationStep> { step },
            "content");
        Assert.That(justice.TryResolveBeginDayReceipt(
            changedConfigurationManifest, step, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        BoundaryContinuationManifest changedContentManifest = new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "configuration",
            new List<BoundaryContinuationStep> { step },
            "changed-content");
        Assert.That(justice.TryResolveBeginDayReceipt(
            changedContentManifest, step, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    private static JusticeSystem CreateJustice(out CityRuntime city)
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("justice-step-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("justice-step-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("justice-step-arrested");
        city = SimulationTestFactory.CreateCity("justice-step-city", "justice-step-location");
        return new JusticeSystem(free, wanted, arrested, null);
    }

    private static NpcRuntime CreateActor(string identity)
    {
        return new NpcRuntime(identity, SimulationTestFactory.CreateNpc(identity + "-definition"));
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyBoundaryOperation operation,
        BoundaryContinuationStep step)
    {
        return new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "configuration",
            new List<BoundaryContinuationStep> { step },
            "content");
    }
}

public sealed class JusticeAdvanceSentencesBoundaryOwnerTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SentenceStepPreservesLegacyEffectsAndExactReplayDoesNotReapply()
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("justice-advance-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("justice-advance-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("justice-advance-arrested");
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("justice-advance-hidden");
        CityRuntime city = SimulationTestFactory.CreateCity("justice-advance-city", "justice-advance-location");
        JusticeSystem justice = new JusticeSystem(free, wanted, arrested, hidden);
        NpcRuntime guard = CreateActor("justice-advance-guard");
        NpcRuntime target = CreateActor("justice-advance-target");
        WantedRecordRuntime warrant = justice.CreateOrIncreaseWarrant(target, city, 12f, 1);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        target.HideForDays(3);
        target.AddStatus(hidden);
        NpcStatusTestRows.AppendStatusRows(target, wanted, 4);

        NpcRuntime orphan = CreateActor("justice-advance-orphan");
        orphan.AddStatus(arrested);
        orphan.HideForDays(2);
        orphan.AddStatus(hidden);
        List<NpcRuntime> roster = new List<NpcRuntime> { target, null, orphan, target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 4L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);

        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        Assert.That(prepared.RetainedTimelineFacts, Is.Empty);
        Assert.That(prepared.RetainedSourceSignals, Is.Empty);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(warrant.IsActive, Is.False);
        Assert.That(justice.GetRemainingSentenceDays(target), Is.Zero);
        Assert.That(justice.IsArrested(target), Is.False);
        Assert.That(justice.IsArrested(orphan), Is.False);
        Assert.That(target.HiddenDaysRemaining, Is.Zero);
        Assert.That(orphan.HiddenDaysRemaining, Is.Zero);
        Assert.That(CountStatusReference(target, free), Is.EqualTo(1));
        Assert.That(CountStatusReference(target, wanted), Is.EqualTo(1));
        Assert.That(CountStatusReference(target, hidden), Is.Zero);
        Assert.That(CountStatusReference(orphan, free), Is.EqualTo(1));
        Assert.That(CountStatusReference(orphan, hidden), Is.Zero);
        Assert.That(justice.TryResolveAdvanceSentencesReceipt(
            manifest, step, out JusticeAdvanceSentencesReceipt receipt, out failure), Is.True);
        Assert.That(receipt.ExecutionStepIdentity, Is.Not.EqualTo(manifest.GetExecutionStepIdentity(step)));
        Assert.That(receipt.OwnerRevisionBefore, Is.Zero);
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1L));

        // One duplicate marker remains after ResolveWarrant, ReleasePrisoner, and both roster
        // occurrences each remove one marker. The separate legacy pass remains distinct.
        Assert.That(CountStatusReference(target, wanted), Is.EqualTo(1));
        justice.SyncWantedStatuses(roster);
        Assert.That(CountStatusReference(target, wanted), Is.Zero);

        NpcRuntime laterTarget = CreateActor("justice-advance-later-target");
        WantedRecordRuntime laterWarrant = justice.CreateOrIncreaseWarrant(laterTarget, city, 8f, 3);
        Assert.That(justice.Arrest(guard, laterTarget, city), Is.True);
        int laterSentenceDays = justice.GetRemainingSentenceDays(laterTarget);
        Assert.That(laterWarrant.IsActive, Is.True);

        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(justice.GetRemainingSentenceDays(laterTarget), Is.EqualTo(laterSentenceDays));
        Assert.That(laterWarrant.IsActive, Is.True);
    }

    [Test]
    public void SentenceStepRejectsSentenceCounterDriftBeforeApplyingEffects()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-advance-stale-guard");
        NpcRuntime target = CreateActor("justice-advance-stale-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 3);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        List<NpcRuntime> roster = new List<NpcRuntime>();
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 5L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out _), Is.True);

        List<PrisonSentenceRuntime> sentences = (List<PrisonSentenceRuntime>)typeof(JusticeSystem)
            .GetField("prisonSentences", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(justice);
        sentences[0].RegisterFailedEscape(1);
        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(justice.GetRemainingSentenceDays(target), Is.EqualTo(4));
        Assert.That(justice.IsArrested(target), Is.True);
        Assert.That(justice.TryResolveAdvanceSentencesReceipt(manifest, step, out _, out _), Is.False);
    }

    [Test]
    public void SentenceStepRejectsMissingExpiryCityBeforeApplyingEffects()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-advance-missing-city-guard");
        NpcRuntime target = CreateActor("justice-advance-missing-city-target");
        WantedRecordRuntime warrant = justice.CreateOrIncreaseWarrant(target, city, 12f, 1);
        Assert.That(justice.Arrest(guard, target, city), Is.True);

        List<PrisonSentenceRuntime> sentences = (List<PrisonSentenceRuntime>)typeof(JusticeSystem)
            .GetField("prisonSentences", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(justice);
        Assert.That(sentences, Has.Count.EqualTo(1));
        typeof(PrisonSentenceRuntime)
            .GetField("city", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(sentences[0], null);

        List<NpcRuntime> roster = new List<NpcRuntime> { target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 9L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out failure), Is.True);

        Assert.That(prepared.TryCommit(out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(warrant.IsActive, Is.True);
        Assert.That(justice.IsArrested(target), Is.True);
        Assert.That((int)typeof(PrisonSentenceRuntime)
            .GetField("remainingDays", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(sentences[0]), Is.EqualTo(1));
        Assert.That(justice.TryResolveAdvanceSentencesReceipt(manifest, step, out _, out _), Is.False);
    }

    [Test]
    public void SentenceStepCapturesCrimeHiddenStateAfterItsPredecessor()
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("justice-advance-hidden-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("justice-advance-hidden-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("justice-advance-hidden-arrested");
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("justice-advance-hidden-marker");
        CityRuntime city = SimulationTestFactory.CreateCity("justice-advance-hidden-city", "justice-advance-hidden-location");
        JusticeSystem justice = new JusticeSystem(free, wanted, arrested, hidden);
        NpcRuntime guard = CreateActor("justice-advance-hidden-guard");
        NpcRuntime target = CreateActor("justice-advance-hidden-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 3);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        List<NpcRuntime> roster = new List<NpcRuntime> { target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 8L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);

        // The frozen topology is unchanged; the declared Crime predecessor may update this mutable value.
        target.HideForDays(2);
        target.AddStatus(hidden);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(justice.GetRemainingSentenceDays(target), Is.EqualTo(2));
        Assert.That(target.HiddenDaysRemaining, Is.EqualTo(3));
        Assert.That(CountStatusReference(target, hidden), Is.EqualTo(1));
    }

    [Test]
    public void SentenceStepFreezesOrderedRosterAndJusticeTargetCardinality()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-advance-topology-guard");
        NpcRuntime target = CreateActor("justice-advance-topology-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 3);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        List<NpcRuntime> roster = new List<NpcRuntime> { target, null, target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 6L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);

        roster.RemoveAt(1);
        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out _, out TimelineFailure rosterFailure), Is.False);
        Assert.That(rosterFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        roster.Insert(1, null);
        justice.CreateOrIncreaseWarrant(CreateActor("justice-advance-topology-extra"), city, 4f, 2);
        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out _, out TimelineFailure targetFailure), Is.False);
        Assert.That(targetFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void SentenceStepReceiptRejectsOrdinalAndConfigurationDrift()
    {
        JusticeSystem justice = CreateJustice(out CityRuntime city);
        NpcRuntime guard = CreateActor("justice-advance-identity-guard");
        NpcRuntime target = CreateActor("justice-advance-identity-target");
        justice.CreateOrIncreaseWarrant(target, city, 12f, 3);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        List<NpcRuntime> roster = new List<NpcRuntime> { target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 7L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(justice.TryPrepareAdvanceSentencesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.True);

        Assert.That(justice.TryResolveAdvanceSentencesReceipt(
            manifest, step, out JusticeAdvanceSentencesReceipt receipt, out TimelineFailure failure), Is.True);
        Assert.That(receipt.ExecutionStepIdentity, Is.Not.EqualTo(manifest.GetExecutionStepIdentity(step)));

        BoundaryContinuationStep preceding = new BoundaryContinuationStep(
            0, "preceding-step", "test-owner", "test.operation", "1", "test-revision", string.Empty);
        BoundaryContinuationStep shifted = new BoundaryContinuationStep(
            1, step.StepId, step.OwnerId, step.OperationKind, step.OperationVersion,
            step.OwnerRevision, step.Payload, step.PersonId, step.Disposition);
        BoundaryContinuationManifest shiftedManifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "v1", "configuration",
            new List<BoundaryContinuationStep> { preceding, shifted }, "content");
        Assert.That(justice.TryResolveAdvanceSentencesReceipt(
            shiftedManifest, shifted, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        BoundaryContinuationManifest changedConfigurationManifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "v1", "changed-configuration",
            new List<BoundaryContinuationStep> { step }, "content");
        Assert.That(justice.TryResolveAdvanceSentencesReceipt(
            changedConfigurationManifest, step, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    private static JusticeSystem CreateJustice(out CityRuntime city)
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("justice-advance-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("justice-advance-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("justice-advance-arrested");
        city = SimulationTestFactory.CreateCity("justice-advance-city", "justice-advance-location");
        return new JusticeSystem(free, wanted, arrested, null);
    }

    private static NpcRuntime CreateActor(string identity)
    {
        return new NpcRuntime(identity, SimulationTestFactory.CreateNpc(identity + "-definition"));
    }

    private static int CountStatusReference(NpcRuntime npc, NpcStatusData status)
    {
        int count = 0;
        foreach (NpcStatusData current in npc.CurrentStatus)
        {
            if (ReferenceEquals(current, status))
            {
                count++;
            }
        }

        return count;
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyBoundaryOperation operation,
        BoundaryContinuationStep step)
    {
        return new BoundaryContinuationManifest(
            operation, "daily-boundary", "v1", "configuration",
            new List<BoundaryContinuationStep> { step }, "content");
    }
}

public sealed class JusticeSyncWantedStatusesBoundaryOwnerTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void SeparateWantedPassObservesSentencePredecessorAndExactReplayDoesNotRepeatEffects()
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("justice-sync-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("justice-sync-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("justice-sync-arrested");
        CityRuntime city = SimulationTestFactory.CreateCity("justice-sync-city", "justice-sync-location");
        JusticeSystem justice = new JusticeSystem(free, wanted, arrested, null);
        NpcRuntime guard = CreateActor("justice-sync-guard");
        NpcRuntime target = CreateActor("justice-sync-target");
        WantedRecordRuntime record = justice.CreateOrIncreaseWarrant(target, city, 5f, 1);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        NpcStatusTestRows.AppendStatusRows(target, wanted, 8);
        List<NpcRuntime> roster = new List<NpcRuntime> { target, null, target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 31L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(operation, roster, 0, out BoundaryContinuationStep sentenceStep, out _), Is.True);
        Assert.That(justice.TryCreateSyncWantedStatusesStep(operation, roster, 1, out BoundaryContinuationStep syncStep, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, sentenceStep, syncStep);

        Assert.That(justice.TryPrepareAdvanceSentencesStep(manifest, sentenceStep, roster, out IBoundaryContinuationStepCommit sentence, out _), Is.True);
        Assert.That(sentence.TryCommit(out _), Is.True);
        Assert.That(record.IsActive, Is.False);
        int afterEmbeddedPasses = CountStatusReference(target, wanted);
        Assert.That(afterEmbeddedPasses, Is.GreaterThan(0));

        Assert.That(justice.TryPrepareSyncWantedStatusesStep(manifest, syncStep, roster, out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True);
        Assert.That(prepared.RetainedTimelineFacts, Is.Empty);
        Assert.That(prepared.RetainedSourceSignals, Is.Empty);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(CountStatusReference(target, wanted), Is.EqualTo(afterEmbeddedPasses - 2));
        Assert.That(justice.TryResolveSyncWantedStatusesReceipt(manifest, syncStep, out JusticeSyncWantedStatusesReceipt receipt, out failure), Is.True);
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(receipt.OwnerRevisionBefore + 1));

        target.AddStatus(wanted);
        int markerCountBeforeReplay = CountStatusReference(target, wanted);
        Assert.That(justice.TryPrepareSyncWantedStatusesStep(manifest, syncStep, roster, out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(CountStatusReference(target, wanted), Is.EqualTo(markerCountBeforeReplay));
    }

    [Test]
    public void TopologyFingerprintRejectsOrdinalConfigurationAndRosterCardinalityDrift()
    {
        JusticeSystem justice = CreateJustice(out _);
        NpcRuntime target = CreateActor("justice-sync-topology-target");
        List<NpcRuntime> roster = new List<NpcRuntime> { target, null, target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 32L);
        Assert.That(justice.TryCreateSyncWantedStatusesStep(operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);

        roster.RemoveAt(1);
        Assert.That(justice.TryPrepareSyncWantedStatusesStep(manifest, step, roster, out _, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        roster.Insert(1, null);

        Assert.That(justice.TryPrepareSyncWantedStatusesStep(manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True);

        BoundaryContinuationStep preceding = new BoundaryContinuationStep(0, "preceding", "test", "test", "1", "r", string.Empty);
        BoundaryContinuationStep shifted = new BoundaryContinuationStep(1, step.StepId, step.OwnerId, step.OperationKind,
            step.OperationVersion, step.OwnerRevision, step.Payload, step.PersonId, step.Disposition);
        BoundaryContinuationManifest shiftedManifest = CreateManifest(operation, preceding, shifted);
        Assert.That(justice.TryResolveSyncWantedStatusesReceipt(shiftedManifest, shifted, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        BoundaryContinuationManifest changedConfiguration = new BoundaryContinuationManifest(
            operation, "daily-boundary", "v1", "changed", new List<BoundaryContinuationStep> { step }, "content");
        Assert.That(justice.TryResolveSyncWantedStatusesReceipt(changedConfiguration, step, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        DailyBoundaryOperation recordOperation = new DailyBoundaryOperation("world", "intraday", 34L);
        Assert.That(justice.TryCreateSyncWantedStatusesStep(recordOperation, roster, 0, out BoundaryContinuationStep recordStep, out _), Is.True);
        BoundaryContinuationManifest recordManifest = CreateManifest(recordOperation, recordStep);
        justice.CreateOrIncreaseWarrant(CreateActor("justice-sync-new-record-target"),
            SimulationTestFactory.CreateCity("justice-sync-new-record-city", "justice-sync-new-record-location"), 3f, 2);
        Assert.That(justice.TryPrepareSyncWantedStatusesStep(recordManifest, recordStep, roster, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void MutableWantedAndMarkerValuesAreCapturedAfterPredecessorEffects()
    {
        NpcStatusData free = SimulationTestFactory.CreateStatus("justice-sync-late-free");
        NpcStatusData wanted = SimulationTestFactory.CreateStatus("justice-sync-late-wanted");
        NpcStatusData arrested = SimulationTestFactory.CreateStatus("justice-sync-late-arrested");
        CityRuntime city = SimulationTestFactory.CreateCity("justice-sync-late-city", "justice-sync-late-location");
        JusticeSystem justice = new JusticeSystem(free, wanted, arrested, null);
        NpcRuntime guard = CreateActor("justice-sync-late-guard");
        NpcRuntime target = CreateActor("justice-sync-late-target");
        justice.CreateOrIncreaseWarrant(target, city, 4f, 1);
        Assert.That(justice.Arrest(guard, target, city), Is.True);
        target.AddStatus(wanted);
        List<NpcRuntime> roster = new List<NpcRuntime> { target };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 33L);
        Assert.That(justice.TryCreateAdvanceSentencesStep(operation, roster, 0, out BoundaryContinuationStep sentenceStep, out _), Is.True);
        Assert.That(justice.TryCreateSyncWantedStatusesStep(operation, roster, 1, out BoundaryContinuationStep syncStep, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, sentenceStep, syncStep);

        Assert.That(justice.TryPrepareAdvanceSentencesStep(manifest, sentenceStep, roster, out IBoundaryContinuationStepCommit sentence, out _), Is.True);
        Assert.That(sentence.TryCommit(out _), Is.True);
        Assert.That(justice.TryPrepareSyncWantedStatusesStep(manifest, syncStep, roster, out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(CountStatusReference(target, wanted), Is.EqualTo(0));
    }

    private static JusticeSystem CreateJustice(out CityRuntime city)
    {
        city = SimulationTestFactory.CreateCity("justice-sync-helper-city", "justice-sync-helper-location");
        return new JusticeSystem(
            SimulationTestFactory.CreateStatus("justice-sync-helper-free"),
            SimulationTestFactory.CreateStatus("justice-sync-helper-wanted"),
            SimulationTestFactory.CreateStatus("justice-sync-helper-arrested"), null);
    }

    private static NpcRuntime CreateActor(string identity) =>
        new NpcRuntime(identity, SimulationTestFactory.CreateNpc(identity + "-definition"));

    private static int CountStatusReference(NpcRuntime npc, NpcStatusData status)
    {
        int count = 0;
        foreach (NpcStatusData candidate in npc.CurrentStatus)
        {
            if (ReferenceEquals(candidate, status)) count++;
        }
        return count;
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyBoundaryOperation operation,
        params BoundaryContinuationStep[] steps) =>
        new BoundaryContinuationManifest(operation, "daily-boundary", "v1", "configuration",
            new List<BoundaryContinuationStep>(steps), "content");
}

public sealed class CrimeHiddenStatusBoundaryOwnerTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void HiddenStatusBoundaryStepCommitsReceiptAndExactReplayDoesNotReapply()
    {
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("crime-boundary-hidden");
        CrimeSystem crime = new CrimeSystem(null, null, hidden);
        NpcRuntime npc = CreateActor("crime-boundary-replay");
        npc.HideForDays(1);
        npc.AddStatus(hidden);
        List<NpcRuntime> roster = new List<NpcRuntime> { npc };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 1L);

        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out TimelineFailure failure), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        Assert.That(prepared.RetainedTimelineFacts, Is.Empty);
        Assert.That(prepared.RetainedSourceSignals, Is.Empty);
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(1));
        Assert.That(CountStatusReference(npc, hidden), Is.EqualTo(1));
        Assert.That(crime.TryResolveAdvanceHiddenStatusesReceipt(
            manifest, step, out CrimeHiddenStatusReceipt receipt, out failure), Is.True);
        Assert.That(receipt.ExecutionStepIdentity, Is.Not.EqualTo(manifest.GetExecutionStepIdentity(step)));
        Assert.That(receipt.OwnerRevisionBefore, Is.Zero);
        Assert.That(receipt.OwnerRevisionAfter, Is.EqualTo(1L));

        npc.HideForDays(4);
        int currentDays = npc.HiddenDaysRemaining;
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit replay, out failure), Is.True);
        Assert.That(replay.TryCommit(out failure), Is.True);
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(currentDays));
        Assert.That(CountStatusReference(npc, hidden), Is.EqualTo(1));
    }

    [Test]
    public void ReceiptIdentityExcludesStepOrdinalWhileDescriptorRejectsManifestDrift()
    {
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("crime-boundary-identity-hidden");
        CrimeSystem crime = new CrimeSystem(null, null, hidden);
        NpcRuntime npc = CreateActor("crime-boundary-identity");
        npc.HideForDays(1);
        npc.AddStatus(hidden);
        List<NpcRuntime> roster = new List<NpcRuntime> { npc };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 1L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out _), Is.True);

        BoundaryContinuationStep preceding = new BoundaryContinuationStep(
            0, "preceding-step", "test-owner", "test.operation", "1", "test-revision", string.Empty);
        BoundaryContinuationStep shifted = new BoundaryContinuationStep(
            1, step.StepId, step.OwnerId, step.OperationKind, step.OperationVersion,
            step.OwnerRevision, step.Payload, step.PersonId, step.Disposition);
        BoundaryContinuationManifest shiftedManifest = CreateManifest(
            operation,
            new List<BoundaryContinuationStep> { preceding, shifted });

        Assert.That(crime.TryResolveAdvanceHiddenStatusesReceipt(
            shiftedManifest, shifted, out _, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));

        BoundaryContinuationManifest changedContentManifest = new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "changed-configuration",
            new List<BoundaryContinuationStep> { step },
            "changed-content");
        Assert.That(crime.TryResolveAdvanceHiddenStatusesReceipt(
            changedContentManifest, step, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void HiddenStatusSnapshotUsesPersonIdWhenPresentAndRuntimeIdFallbackWhenAbsent()
    {
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("crime-boundary-person-hidden");
        CrimeSystem crime = new CrimeSystem(null, null, hidden);
        NpcRuntime npc = CreateActor("crime-boundary-person-fallback");
        npc.HideForDays(1);
        List<NpcRuntime> roster = new List<NpcRuntime> { npc };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 1L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            manifest, step, roster, out IBoundaryContinuationStepCommit prepared, out _), Is.True);

        PersonId personId = new PersonId("crime-boundary-person");
        MethodInfo assignPersonId = typeof(NpcRuntime).GetMethod("TryAssignPersonId", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(assignPersonId, Is.Not.Null);
        Assert.That((bool)assignPersonId.Invoke(npc, new object[] { personId }), Is.True);

        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(2));
    }

    [Test]
    public void HiddenStatusBoundaryStepRejectsFaultedOwnerBeforeEffects()
    {
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("crime-boundary-faulted-hidden");
        CrimeSystem crime = new CrimeSystem(null, null, hidden);
        NpcRuntime npc = CreateActor("crime-boundary-faulted");
        npc.HideForDays(1);
        List<NpcRuntime> roster = new List<NpcRuntime> { npc };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 1L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            operation, roster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        object guard = CreateFaultedGuard();
        MethodInfo bind = typeof(CrimeSystem).GetMethod("TryBindMutationGuard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(bind, Is.Not.Null);
        Assert.That((bool)bind.Invoke(crime, new[] { guard }), Is.True);

        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            manifest, step, roster, out _, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(2));
    }

    [Test]
    public void HiddenStatusBoundaryStepRejectsChangedOrderCardinalityAndMarkerSnapshot()
    {
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("crime-boundary-stale-hidden");
        CrimeSystem crime = new CrimeSystem(null, null, hidden);
        NpcRuntime first = CreateActor("crime-boundary-stale-first");
        NpcRuntime second = CreateActor("crime-boundary-stale-second");
        first.HideForDays(2);
        second.HideForDays(2);
        List<NpcRuntime> roster = new List<NpcRuntime> { first, second };

        DailyBoundaryOperation orderOperation = new DailyBoundaryOperation("world", "intraday", 1L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            orderOperation, roster, 0, out BoundaryContinuationStep orderStep, out _), Is.True);
        BoundaryContinuationManifest orderManifest = CreateManifest(orderOperation, orderStep);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            orderManifest, orderStep, roster, out IBoundaryContinuationStepCommit orderPrepared, out _), Is.True);
        roster.Reverse();
        Assert.That(orderPrepared.TryCommit(out TimelineFailure orderFailure), Is.False);
        Assert.That(orderFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(first.HiddenDaysRemaining, Is.EqualTo(3));
        Assert.That(second.HiddenDaysRemaining, Is.EqualTo(3));

        DailyBoundaryOperation cardinalityOperation = new DailyBoundaryOperation("world", "intraday", 2L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            cardinalityOperation, roster, 0, out BoundaryContinuationStep cardinalityStep, out _), Is.True);
        BoundaryContinuationManifest cardinalityManifest = CreateManifest(cardinalityOperation, cardinalityStep);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            cardinalityManifest, cardinalityStep, roster, out IBoundaryContinuationStepCommit cardinalityPrepared, out _), Is.True);
        roster.RemoveAt(1);
        Assert.That(cardinalityPrepared.TryCommit(out TimelineFailure cardinalityFailure), Is.False);
        Assert.That(cardinalityFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(first.HiddenDaysRemaining, Is.EqualTo(3));
        Assert.That(second.HiddenDaysRemaining, Is.EqualTo(3));

        DailyBoundaryOperation markerOperation = new DailyBoundaryOperation("world", "intraday", 3L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            markerOperation, roster, 0, out BoundaryContinuationStep markerStep, out _), Is.True);
        BoundaryContinuationManifest markerManifest = CreateManifest(markerOperation, markerStep);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            markerManifest, markerStep, roster, out IBoundaryContinuationStepCommit markerPrepared, out _), Is.True);
        second.AddStatus(hidden);
        Assert.That(markerPrepared.TryCommit(out TimelineFailure markerFailure), Is.False);
        Assert.That(markerFailure, Is.EqualTo(TimelineFailure.ContinuationFailed));
        Assert.That(first.HiddenDaysRemaining, Is.EqualTo(3));
        Assert.That(CountStatusReference(second, hidden), Is.EqualTo(1));
    }

    [Test]
    public void HiddenStatusBoundaryStepPreservesRepeatedRosterOccurrencesAndSingleMarkerRemoval()
    {
        NpcStatusData hidden = SimulationTestFactory.CreateStatus("crime-boundary-duplicate-hidden");
        SimulationLogger logger = new SimulationLogger(null);
        CrimeSystem crime = new CrimeSystem(null, null, hidden, logger: logger);
        NpcRuntime hiddenNpc = CreateActor("crime-boundary-duplicate-roster");
        hiddenNpc.HideForDays(1);
        hiddenNpc.AddStatus(hidden);
        List<NpcRuntime> repeatedRoster = new List<NpcRuntime> { hiddenNpc, hiddenNpc };
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 1L);

        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            operation, repeatedRoster, 0, out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = CreateManifest(operation, step);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            manifest, step, repeatedRoster, out IBoundaryContinuationStepCommit prepared, out _), Is.True);
        Assert.That(prepared.TryCommit(out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(hiddenNpc.HiddenDaysRemaining, Is.Zero);
        Assert.That(CountStatusReference(hiddenNpc, hidden), Is.Zero);

        NpcRuntime visibleNpc = CreateActor("crime-boundary-duplicate-marker");
        NpcStatusTestRows.AppendStatusRows(visibleNpc, hidden, 2);
        List<NpcRuntime> visibleRoster = new List<NpcRuntime> { visibleNpc };
        DailyBoundaryOperation nextOperation = new DailyBoundaryOperation("world", "intraday", 2L);
        Assert.That(crime.TryCreateAdvanceHiddenStatusesStep(
            nextOperation, visibleRoster, 0, out BoundaryContinuationStep nextStep, out _), Is.True);
        BoundaryContinuationManifest nextManifest = CreateManifest(nextOperation, nextStep);
        Assert.That(crime.TryPrepareAdvanceHiddenStatusesStep(
            nextManifest, nextStep, visibleRoster, out IBoundaryContinuationStepCommit nextPrepared, out _), Is.True);
        Assert.That(nextPrepared.TryCommit(out failure), Is.True);
        Assert.That(CountStatusReference(visibleNpc, hidden), Is.EqualTo(1));

        string expiryNotice = hiddenNpc.NpcName + " nao esta mais escondido.";
        StringAssert.Contains(expiryNotice, logger.FullLog);
        Assert.That(logger.FullLog.IndexOf(expiryNotice, StringComparison.Ordinal),
            Is.EqualTo(logger.FullLog.LastIndexOf(expiryNotice, StringComparison.Ordinal)));
    }

    private static NpcRuntime CreateActor(string identity)
    {
        return new NpcRuntime(identity, SimulationTestFactory.CreateNpc(identity + "-definition"));
    }

    private static object CreateFaultedGuard()
    {
        Type guardType = typeof(SimulationRuntime).Assembly.GetType("AuthoritativeMutationGuard", true);
        object guard = Activator.CreateInstance(guardType, true);
        Type reasonType = guardType.Assembly.GetType("AuthoritativeMutationFaultReason", true);
        object reason = Enum.Parse(reasonType, "RollbackRestoreFailed");
        guardType.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(guard, new[] { reason });
        return guard;
    }

    private static int CountStatusReference(NpcRuntime npc, NpcStatusData status)
    {
        int count = 0;
        foreach (NpcStatusData candidate in npc.CurrentStatus)
        {
            if (ReferenceEquals(candidate, status))
            {
                count++;
            }
        }

        return count;
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyBoundaryOperation operation,
        BoundaryContinuationStep step)
    {
        return CreateManifest(operation, new List<BoundaryContinuationStep> { step });
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyBoundaryOperation operation,
        IReadOnlyList<BoundaryContinuationStep> steps)
    {
        return new BoundaryContinuationManifest(
            operation,
            "daily-boundary",
            "v1",
            "configuration",
            steps,
            "content");
    }
}

internal static class NpcStatusTestRows
{
    public static void AppendStatusRows(NpcRuntime npc, NpcStatusData status, int count)
    {
        List<NpcStatusData> rows = (List<NpcStatusData>)typeof(NpcRuntime)
            .GetField("currentStatus", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(npc);
        for (int i = 0; i < count; i++) rows.Add(status);
    }
}
