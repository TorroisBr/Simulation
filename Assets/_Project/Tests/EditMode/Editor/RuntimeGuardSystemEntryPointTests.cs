using System;
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
