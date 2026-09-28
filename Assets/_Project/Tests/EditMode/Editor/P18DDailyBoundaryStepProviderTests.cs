using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P18DDailyBoundaryStepProviderTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ExistingPlaceContentAndLoggerOwnersRunInTheirDeclaredOrderWithReceipts()
    {
        const string worldId = "p18d-provider-world";
        const string profileId = "p18d-provider-profile";
        PlaceContentStore placeContent = new PlaceContentStore();
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings { showDay = true });
        PlaceContentDailyBoundaryStepProvider placeProvider =
            new PlaceContentDailyBoundaryStepProvider(placeContent);
        LoggerDailyBoundaryStepProvider loggerProvider = new LoggerDailyBoundaryStepProvider(logger);
        P18DDailyBoundaryOwner boundaryOwner = new P18DDailyBoundaryOwner(
            worldId, profileId, "configuration/v1", "content/v1",
            new IP18DDailyBoundaryStepProvider[] { placeProvider, loggerProvider });
        DailyBoundaryOperation operation = new DailyBoundaryOperation(worldId, profileId, 1L);

        Assert.That(boundaryOwner.TryPrepareActivation(operation,
            out IBoundaryActivationCommit activation, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(activation.Manifest.Steps.Count, Is.EqualTo(2));
        Assert.That(activation.Manifest.Steps[0].Ordinal, Is.Zero);
        Assert.That(activation.Manifest.Steps[0].StepId, Is.EqualTo(PlaceContentStore.DayAdvanceStepId));
        Assert.That(activation.Manifest.Steps[1].Ordinal, Is.EqualTo(1));
        Assert.That(activation.Manifest.Steps[1].StepId, Is.EqualTo("logger-begin-day"));
        Assert.That(activation.TryCommit(out failure), Is.True);

        SimulationTimeline timeline = new SimulationTimeline(
            new SimulationCalendar(new CalendarDefinition(2, 2, 3)),
            new LogicalTick(0), boundaryOwner: boundaryOwner, worldId: worldId, profileId: profileId);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(timeline.TrySealInputsThrough(boundary, out failure), Is.True);
        Assert.That(timeline.TryAdvanceTo(boundary, out failure), Is.True, failure.ToString());

        Assert.That(placeContent.TryResolveDayAdvanceReceipt(activation.Manifest,
            activation.Manifest.Steps[0], out _, out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(logger.TryResolveBeginDayReceipt(activation.Manifest,
            activation.Manifest.Steps[1], out LoggerBeginDayReceipt loggerReceipt, out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(loggerReceipt.Disposition, Is.EqualTo("included"));
        Assert.That(logger.FullLog, Is.EqualTo(string.Join(Environment.NewLine,
            "====================", "DIA 1", "====================", string.Empty)));
    }

    [Test]
    public void LoggerProviderFreezesNoOutputDispositionAndRejectsForeignSteps()
    {
        LoggerDailyBoundaryStepProvider provider = new LoggerDailyBoundaryStepProvider(
            new SimulationLogger(new SimulationLogSettings { showDay = false }));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "profile", 2L);

        Assert.That(provider.TryCreateSteps(operation, 3,
            out System.Collections.Generic.IReadOnlyList<BoundaryContinuationStep> steps,
            out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(steps.Count, Is.EqualTo(1));
        Assert.That(steps[0].Ordinal, Is.EqualTo(3));
        Assert.That(steps[0].Disposition, Is.EqualTo("no-output"));
        Assert.That(provider.OwnsStep(steps[0]), Is.True);

        BoundaryContinuationStep foreign = new BoundaryContinuationStep(
            3, "other-step", "other-owner", "other.operation", "1", "revision", string.Empty);
        Assert.That(provider.OwnsStep(foreign), Is.False);
        Assert.That(provider.TryPrepareStep(null, foreign, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    [Test]
    public void JusticeCrimeAndMerchantProvidersExposeTheApprovedRelativeDailyOrder()
    {
        JusticeSystem justice = CreateJustice();
        List<NpcRuntime> roster = new List<NpcRuntime>();
        CrimeSystem crime = new CrimeSystem(justice, null,
            SimulationTestFactory.CreateStatus("p18d-provider-hidden"));
        MerchantSystem merchant = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 6L);

        JusticeBeginDayDailyBoundaryStepProvider beginDay =
            new JusticeBeginDayDailyBoundaryStepProvider(justice);
        CrimeHiddenStatusesDailyBoundaryStepProvider hiddenStatuses =
            new CrimeHiddenStatusesDailyBoundaryStepProvider(crime, roster);
        JusticeResolutionDailyBoundaryStepProvider justiceResolution =
            new JusticeResolutionDailyBoundaryStepProvider(justice, roster);
        MerchantPlanUrgencyDailyBoundaryStepProvider planUrgency =
            new MerchantPlanUrgencyDailyBoundaryStepProvider(merchant, roster);

        AssertStep(beginDay, operation, 0, "justice-begin-day");
        AssertStep(hiddenStatuses, operation, 1, "crime-hidden-statuses");
        Assert.That(justiceResolution.TryCreateSteps(operation, 2,
            out IReadOnlyList<BoundaryContinuationStep> justiceSteps, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(justiceSteps.Count, Is.EqualTo(2));
        Assert.That(justiceSteps[0].StepId, Is.EqualTo("justice-advance-sentences"));
        Assert.That(justiceSteps[0].Ordinal, Is.EqualTo(2));
        Assert.That(justiceSteps[1].StepId, Is.EqualTo("justice-sync-wanted-statuses"));
        Assert.That(justiceSteps[1].Ordinal, Is.EqualTo(3));
        AssertStep(planUrgency, operation, 4, "merchant-plan-urgency");

        // The absent demographic owner remains an earlier P18-D readiness blocker;
        // these adapters prove only their relative segment and are not a profile composition.
    }

    [Test]
    public void JusticeCrimeAndMerchantProvidersDelegatePreparationToTheirDomainOwners()
    {
        JusticeSystem justice = CreateJustice();
        List<NpcRuntime> roster = new List<NpcRuntime>();
        CrimeSystem crime = new CrimeSystem(justice, null,
            SimulationTestFactory.CreateStatus("p18d-provider-prepare-hidden"));
        MerchantSystem merchant = SimulationTestFactory.CreateMerchantSystem(null, new SimulationTime(1));

        JusticeBeginDayDailyBoundaryStepProvider beginDay =
            new JusticeBeginDayDailyBoundaryStepProvider(justice);
        IBoundaryContinuationStepCommit beginOperation = PrepareSingleStep(beginDay,
            new DailyBoundaryOperation("world", "intraday", 10L), out BoundaryContinuationManifest beginManifest,
            out BoundaryContinuationStep beginStep);
        Assert.That(beginOperation.TryCommit(out TimelineFailure failure), Is.True);
        Assert.That(justice.TryResolveBeginDayReceipt(beginManifest, beginStep, out _, out failure), Is.True);

        CrimeHiddenStatusesDailyBoundaryStepProvider hiddenStatuses =
            new CrimeHiddenStatusesDailyBoundaryStepProvider(crime, roster);
        IBoundaryContinuationStepCommit crimeOperation = PrepareSingleStep(hiddenStatuses,
            new DailyBoundaryOperation("world", "intraday", 11L), out BoundaryContinuationManifest crimeManifest,
            out BoundaryContinuationStep crimeStep);
        Assert.That(crimeOperation.TryCommit(out failure), Is.True);
        Assert.That(crime.TryResolveAdvanceHiddenStatusesReceipt(crimeManifest, crimeStep, out _, out failure), Is.True);

        JusticeResolutionDailyBoundaryStepProvider resolution =
            new JusticeResolutionDailyBoundaryStepProvider(justice, roster);
        DailyBoundaryOperation resolutionOperation = new DailyBoundaryOperation("world", "intraday", 12L);
        Assert.That(resolution.TryCreateSteps(resolutionOperation, 0,
            out IReadOnlyList<BoundaryContinuationStep> resolutionSteps, out failure), Is.True);
        BoundaryContinuationManifest resolutionManifest = new BoundaryContinuationManifest(
            resolutionOperation, "daily-boundary", "1", "configuration", resolutionSteps);
        Assert.That(resolution.TryPrepareStep(resolutionManifest, resolutionSteps[0],
            out IBoundaryContinuationStepCommit sentenceAdvance, out failure), Is.True);
        Assert.That(sentenceAdvance.TryCommit(out failure), Is.True);
        Assert.That(resolution.TryPrepareStep(resolutionManifest, resolutionSteps[1],
            out IBoundaryContinuationStepCommit wantedSync, out failure), Is.True);
        Assert.That(wantedSync.TryCommit(out failure), Is.True);
        Assert.That(justice.TryResolveAdvanceSentencesReceipt(resolutionManifest, resolutionSteps[0], out _, out failure), Is.True);
        Assert.That(justice.TryResolveSyncWantedStatusesReceipt(resolutionManifest, resolutionSteps[1], out _, out failure), Is.True);

        MerchantPlanUrgencyDailyBoundaryStepProvider planUrgency =
            new MerchantPlanUrgencyDailyBoundaryStepProvider(merchant, roster);
        IBoundaryContinuationStepCommit merchantOperation = PrepareSingleStep(planUrgency,
            new DailyBoundaryOperation("world", "intraday", 13L), out BoundaryContinuationManifest merchantManifest,
            out BoundaryContinuationStep merchantStep);
        Assert.That(merchantOperation.TryCommit(out failure), Is.True);
        Assert.That(merchant.TryResolvePlanUrgencyReceipt(merchantManifest, merchantStep, out _, out failure), Is.True);
    }

    [Test]
    public void CityEconomyProviderFreezesCityOrderAndUsesProductionThenPerCityConsumptionAndPrice()
    {
        CityRuntime first = SimulationTestFactory.CreateCity("p18d-economy-first", "p18d-economy-location-first");
        CityRuntime second = SimulationTestFactory.CreateCity("p18d-economy-second", "p18d-economy-location-second");
        List<CityRuntime> liveCities = new List<CityRuntime> { first, second };
        CityEconomyDailyBoundaryStepProvider provider =
            new CityEconomyDailyBoundaryStepProvider(() => liveCities);
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 20L);

        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(steps.Count, Is.EqualTo(6));
        Assert.That(steps[0].StepId, Is.EqualTo(CityStepId(CityDailyEconomyStepKind.Production, first)));
        Assert.That(steps[1].StepId, Is.EqualTo(CityStepId(CityDailyEconomyStepKind.Production, second)));
        Assert.That(steps[2].StepId, Is.EqualTo(CityStepId(CityDailyEconomyStepKind.Consumption, first)));
        Assert.That(steps[3].StepId, Is.EqualTo(CityStepId(CityDailyEconomyStepKind.PriceRefresh, first)));
        Assert.That(steps[4].StepId, Is.EqualTo(CityStepId(CityDailyEconomyStepKind.Consumption, second)));
        Assert.That(steps[5].StepId, Is.EqualTo(CityStepId(CityDailyEconomyStepKind.PriceRefresh, second)));
        Assert.That(steps[0].OwnerId, Is.EqualTo(first.RuntimeId));
        Assert.That(steps[1].OwnerId, Is.EqualTo(second.RuntimeId));

        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "economy-enabled", steps);
        liveCities.RemoveAt(1);
        Assert.That(provider.TryPrepareStep(manifest, steps[1],
            out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(prepared.TryCommit(out failure), Is.True);
        Assert.That(second.TryResolveDailyEconomyReceipt(manifest, steps[1],
            out CityDailyEconomyReceipt receipt, out failure), Is.True);
        Assert.That(receipt.ProductionResults, Is.Empty);
    }

    [Test]
    public void CityEconomyProviderRejectsAmbiguousRuntimeIdentityInsteadOfFiltering()
    {
        CityRuntime first = SimulationTestFactory.CreateCity("p18d-economy-duplicate", "p18d-economy-location-a");
        CityRuntime second = SimulationTestFactory.CreateCity("p18d-economy-duplicate", "p18d-economy-location-b");
        CityEconomyDailyBoundaryStepProvider provider = new CityEconomyDailyBoundaryStepProvider(
            () => new[] { first, second });
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world", "intraday", 21L);

        Assert.That(provider.TryCreateSteps(operation, 0, out IReadOnlyList<BoundaryContinuationStep> steps,
            out TimelineFailure failure), Is.False);
        Assert.That(steps, Is.Null);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    private static JusticeSystem CreateJustice()
    {
        return new JusticeSystem(
            SimulationTestFactory.CreateStatus("p18d-provider-free"),
            SimulationTestFactory.CreateStatus("p18d-provider-wanted"),
            SimulationTestFactory.CreateStatus("p18d-provider-arrested"),
            SimulationTestFactory.CreateStatus("p18d-provider-justice-hidden"));
    }

    private static string CityStepId(CityDailyEconomyStepKind kind, CityRuntime city) =>
        "city-economy-" + kind.ToString().ToLowerInvariant() + ":" + city.RuntimeId;

    private static void AssertStep(IP18DDailyBoundaryStepProvider provider,
        DailyBoundaryOperation operation, int ordinal, string stepId)
    {
        Assert.That(provider.TryCreateSteps(operation, ordinal,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(TimelineFailure.None));
        Assert.That(steps.Count, Is.EqualTo(1));
        Assert.That(steps[0].Ordinal, Is.EqualTo(ordinal));
        Assert.That(steps[0].StepId, Is.EqualTo(stepId));
        Assert.That(provider.OwnsStep(steps[0]), Is.True);
    }

    private static IBoundaryContinuationStepCommit PrepareSingleStep(
        IP18DDailyBoundaryStepProvider provider, DailyBoundaryOperation operation,
        out BoundaryContinuationManifest manifest, out BoundaryContinuationStep step)
    {
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure), Is.True);
        Assert.That(steps.Count, Is.EqualTo(1));
        step = steps[0];
        manifest = new BoundaryContinuationManifest(operation, "daily-boundary", "1",
            "configuration", steps);
        Assert.That(provider.TryPrepareStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared, out failure), Is.True);
        return prepared;
    }
}
