using System;
using NUnit.Framework;

public sealed class P18DDailyBoundaryStepProviderTests
{
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
}
