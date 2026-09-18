using System;
using NUnit.Framework;

public sealed class NpcAgeFoundationTests
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
    public void LegacyNpcWithoutBirthRemainsRegisterableAndAgeIsUnknown()
    {
        NpcRuntime npc = CreateNpc("age-unknown");
        SimulationTime time = new SimulationTime(10L);
        SimulationRuntime world = CreateWorld(time);

        Assert.That(npc.HasKnownBirthDay, Is.False);
        Assert.That(world.TryRegisterNpc(npc, out WorldNpcRegistryFailure registrationFailure), Is.True, registrationFailure.ToString());
        Assert.That(
            NpcAgeQuery.TryCalculate(
                npc,
                time,
                UniformCalendar(),
                out NpcAgeSnapshot age,
                out NpcAgeQueryFailure failure),
            Is.False);
        Assert.That(age, Is.Null);
        Assert.That(failure, Is.EqualTo(NpcAgeQueryFailure.BirthDateUnknown));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(1));
    }

    [Test]
    public void KnownBirthProducesAgeInDaysAndCompletedYears()
    {
        NpcRuntime npc = CreateNpc("age-known", 5L);
        SimulationTime time = new SimulationTime(17L);

        Assert.That(
            NpcAgeQuery.TryCalculate(
                npc,
                time,
                UniformCalendar(),
                out NpcAgeSnapshot age,
                out NpcAgeQueryFailure failure),
            Is.True,
            failure.ToString());
        Assert.That(age.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(age.AgeInDays, Is.EqualTo(12L));
        Assert.That(age.CompletedYears, Is.EqualTo(1L));
        Assert.That(age.BirthDate.Month, Is.EqualTo(6));
        Assert.That(age.CurrentDate.Year, Is.EqualTo(1L));
        Assert.That(age.CurrentDate.Month, Is.EqualTo(6));
    }

    [Test]
    public void CompletedYearsChangesOnExactBirthdayNotTheDayBefore()
    {
        NpcRuntime npc = CreateNpc("age-birthday", 5L);
        CalendarDefinition calendar = UniformCalendar();

        Assert.That(GetCompletedYears(npc, 16L, calendar), Is.EqualTo(0L));
        Assert.That(GetCompletedYears(npc, 17L, calendar), Is.EqualTo(1L));
    }

    [Test]
    public void CustomMonthLengthsAreUsedForCompletedYears()
    {
        NpcRuntime npc = CreateNpc("age-custom-calendar", 4L);
        CalendarDefinition calendar = new CalendarDefinition(new[] { 2, 3, 4 }, 1);

        Assert.That(GetCompletedYears(npc, 12L, calendar), Is.EqualTo(0L));
        Assert.That(GetCompletedYears(npc, 13L, calendar), Is.EqualTo(1L));
    }

    [Test]
    public void BirthTodayHasZeroAgeAndYearBoundaryUsesCalendar()
    {
        NpcRuntime today = CreateNpc("age-today", 12L);
        NpcRuntime boundary = CreateNpc("age-boundary", 11L);
        CalendarDefinition calendar = UniformCalendar();

        Assert.That(GetAgeDays(today, 12L, calendar), Is.EqualTo(0L));
        Assert.That(GetCompletedYears(today, 12L, calendar), Is.EqualTo(0L));
        Assert.That(GetCompletedYears(boundary, 23L, calendar), Is.EqualTo(1L));
        Assert.That(GetCompletedYears(boundary, 22L, calendar), Is.EqualTo(0L));
    }

    [Test]
    public void FutureBirthIsRejectedByQueryAndWorldRegistrationWithoutMutation()
    {
        NpcRuntime future = CreateNpc("age-future", 11L);
        SimulationTime time = new SimulationTime(10L);
        SimulationRuntime world = CreateWorld(time);

        Assert.That(
            NpcAgeQuery.TryCalculate(
                future,
                time,
                UniformCalendar(),
                out _,
                out NpcAgeQueryFailure queryFailure),
            Is.False);
        Assert.That(queryFailure, Is.EqualTo(NpcAgeQueryFailure.BirthDateInFuture));
        Assert.That(world.TryRegisterNpc(future, out WorldNpcRegistryFailure registrationFailure), Is.False);
        Assert.That(registrationFailure, Is.EqualTo(WorldNpcRegistryFailure.BirthDayInFuture));
        Assert.That(world.NpcRuntimes, Is.Empty);
        Assert.That(time.AbsoluteDay, Is.EqualTo(10L));
    }

    [Test]
    public void InvalidCalendarAndNegativeBirthFailExplicitly()
    {
        NpcRuntime npc = CreateNpc("age-invalid-calendar", 0L);
        CalendarDefinition invalid = new CalendarDefinition(0, 1, 1);

        Assert.That(
            NpcAgeQuery.TryCalculate(
                npc,
                new SimulationTime(1L),
                invalid,
                out _,
                out NpcAgeQueryFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(NpcAgeQueryFailure.InvalidCalendar));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateNpc("age-negative", -1L));
    }

    [Test]
    public void AgeQueryDoesNotMutateNpcTimePopulationOrRoster()
    {
        NpcRuntime npc = CreateNpc("age-no-mutation", 5L);
        SimulationTime time = new SimulationTime(17L);
        SimulationRuntime world = CreateWorld(time, npc);
        long? birthBefore = npc.BirthAbsoluteDay;
        NpcLifeState lifeBefore = npc.LifeState;
        string residenceBefore = npc.ResidenceSettlementRuntimeId;
        int rosterCountBefore = world.NpcRuntimes.Count;

        Assert.That(NpcAgeQuery.TryCalculate(npc, time, UniformCalendar(), out _, out _), Is.True);

        Assert.That(npc.BirthAbsoluteDay, Is.EqualTo(birthBefore));
        Assert.That(npc.LifeState, Is.EqualTo(lifeBefore));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(residenceBefore));
        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(rosterCountBefore));
    }

    [Test]
    public void AdvanceDayChangesDerivedAgeOnlyWhenCalendarDateReachesBirthday()
    {
        NpcRuntime npc = CreateNpc("age-advance-day", 5L);
        SimulationTime time = new SimulationTime(16L);
        SimulationRuntime world = CreateWorld(time, npc);
        CalendarDefinition calendar = UniformCalendar();

        Assert.That(GetCompletedYears(npc, time.AbsoluteDay, calendar), Is.EqualTo(0L));
        world.AdvanceDay();
        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
        Assert.That(GetCompletedYears(npc, time.AbsoluteDay, calendar), Is.EqualTo(1L));
        Assert.That(npc.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(world.NpcRuntimes, Has.Count.EqualTo(1));
    }

    [Test]
    public void LongAbsoluteDaysRemainLongWithoutNarrowing()
    {
        long currentDay = 1_000_000_000_000L;
        long birthDay = currentDay - 1_000L;
        NpcRuntime npc = CreateNpc("age-long-day", birthDay);

        Assert.That(
            NpcAgeQuery.TryCalculate(
                npc,
                currentDay,
                UniformCalendar(),
                out NpcAgeSnapshot age,
                out NpcAgeQueryFailure failure),
            Is.True,
            failure.ToString());
        Assert.That(age.AgeInDays, Is.EqualTo(1_000L));
        Assert.That(age.BirthAbsoluteDay, Is.EqualTo(birthDay));
    }

    [Test]
    public void DeadNpcKeepsBirthIdentityAndChronologicalAge()
    {
        NpcRuntime npc = CreateNpc("age-dead", 0L);
        Assert.That(npc.TryApplyDeath(), Is.True);

        Assert.That(
            NpcAgeQuery.TryCalculate(
                npc,
                new SimulationTime(12L),
                UniformCalendar(),
                out NpcAgeSnapshot age,
                out NpcAgeQueryFailure failure),
            Is.True,
            failure.ToString());
        Assert.That(npc.IsDead, Is.True);
        Assert.That(age.CompletedYears, Is.EqualTo(1L));
    }

    private static long GetCompletedYears(NpcRuntime npc, long currentDay, CalendarDefinition calendar)
    {
        Assert.That(
            NpcAgeQuery.TryCalculate(npc, currentDay, calendar, out NpcAgeSnapshot age, out NpcAgeQueryFailure failure),
            Is.True,
            failure.ToString());
        return age.CompletedYears;
    }

    private static long GetAgeDays(NpcRuntime npc, long currentDay, CalendarDefinition calendar)
    {
        Assert.That(
            NpcAgeQuery.TryCalculate(npc, currentDay, calendar, out NpcAgeSnapshot age, out NpcAgeQueryFailure failure),
            Is.True,
            failure.ToString());
        return age.AgeInDays;
    }

    private static SimulationRuntime CreateWorld(SimulationTime time, params NpcRuntime[] npcs)
    {
        return new SimulationRuntime(time, Array.Empty<CityRuntime>(), npcs, economyEnabled: false);
    }

    private static NpcRuntime CreateNpc(string runtimeId, long? birthAbsoluteDay = null)
    {
        return new NpcRuntime(
            runtimeId,
            SimulationTestFactory.CreateNpc(runtimeId),
            null,
            0f,
            birthAbsoluteDay);
    }

    private static CalendarDefinition UniformCalendar()
    {
        return new CalendarDefinition(12, 1, 1);
    }
}
