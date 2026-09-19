using System;
using NUnit.Framework;

public sealed class PersonDemographicAgeTests
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
    public void KnownBirthCalculatesDaysAndCompletedYearsForUnmaterializedPerson()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("age-known"), 5L);

        Assert.That(PersonAgeQuery.TryCalculate(
            person,
            new SimulationTime(17L),
            UniformCalendar(),
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure failure), Is.True, failure.ToString());
        Assert.That(age.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(age.AgeInDays, Is.EqualTo(12L));
        Assert.That(age.CompletedYears, Is.EqualTo(1L));
        Assert.That(age.BirthDate.Month, Is.EqualTo(6));
        Assert.That(age.CurrentDate.Year, Is.EqualTo(1L));
        Assert.That(age.CurrentDate.Month, Is.EqualTo(6));
    }

    [Test]
    public void UnknownBirthRemainsValidButAgeIsUnknown()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("age-unknown"));

        Assert.That(PersonAgeQuery.TryCalculate(
            person,
            new SimulationTime(10L),
            UniformCalendar(),
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure failure), Is.False);
        Assert.That(age, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonAgeQueryFailure.BirthDateUnknown));
    }

    [Test]
    public void CompletedYearsChangesOnExactBirthdayNotTheDayBeforeOrAfter()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("age-birthday"), 5L);
        CalendarDefinition calendar = UniformCalendar();

        Assert.That(GetCompletedYears(person, 16L, calendar), Is.EqualTo(0L));
        Assert.That(GetCompletedYears(person, 17L, calendar), Is.EqualTo(1L));
        Assert.That(GetCompletedYears(person, 18L, calendar), Is.EqualTo(1L));
    }

    [Test]
    public void CustomMonthLengthsDefineAnniversaryBoundaries()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("age-custom"), 4L);
        CalendarDefinition calendar = new CalendarDefinition(new[] { 2, 3, 4 }, 1);

        Assert.That(GetCompletedYears(person, 12L, calendar), Is.EqualTo(0L));
        Assert.That(GetCompletedYears(person, 13L, calendar), Is.EqualTo(1L));
    }

    [Test]
    public void BirthTodayHasZeroDaysAndZeroCompletedYears()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("age-today"), 12L);

        Assert.That(PersonAgeQuery.TryCalculate(
            person,
            12L,
            UniformCalendar(),
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure failure), Is.True, failure.ToString());
        Assert.That(age.AgeInDays, Is.EqualTo(0L));
        Assert.That(age.CompletedYears, Is.EqualTo(0L));
    }

    [Test]
    public void FutureBirthFailsPureQueryAndNegativeBirthIsRejectedLocally()
    {
        PersonRuntime future = new PersonRuntime(new PersonId("age-future"), 11L);

        Assert.That(PersonAgeQuery.TryCalculate(
            future,
            10L,
            UniformCalendar(),
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure failure), Is.False);
        Assert.That(age, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonAgeQueryFailure.BirthDateInFuture));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersonRuntime(new PersonId("age-negative"), -1L));
    }

    [Test]
    public void FutureBirthIsRejectedAtWorldRegistrationWithoutMutation()
    {
        SimulationTime time = new SimulationTime(100L);
        CityRuntime city = SimulationTestFactory.CreateCity("age-city", "age-location");
        SimulationRuntime world = new SimulationRuntime(time, new[] { city }, null);
        PersonRuntime future = new PersonRuntime(new PersonId("future-person"), 101L);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryRegisterPerson(future, out PersonStoreFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PersonStoreFailure.BirthAbsoluteDayInFuture));
        Assert.That(world.PersonStore.Persons, Is.Empty);
        Assert.That(world.NpcRuntimes, Is.Empty);
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        Assert.That(time.AbsoluteDay, Is.EqualTo(100L));
    }

    [Test]
    public void HistoricalPersonCanBeRegisteredWithoutPopulationMutation()
    {
        SimulationTime time = new SimulationTime(5000L);
        CityRuntime city = SimulationTestFactory.CreateCity("historical-city", "historical-location");
        SimulationRuntime world = new SimulationRuntime(time, new[] { city }, null);
        PersonRuntime person = new PersonRuntime(new PersonId("historical-person"), 100L);
        int populationBefore = city.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        Assert.That(world.TryRegisterPerson(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        Assert.That(city.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        Assert.That(GetCompletedYears(person, time.AbsoluteDay, UniformCalendar()), Is.EqualTo(4900L / 12L));
    }

    [Test]
    public void MaterializationDoesNotMoveBirthToNpcAndAgeStillUsesPerson()
    {
        SimulationTime time = new SimulationTime(17L);
        SimulationRuntime world = new SimulationRuntime(time, null, null);
        PersonId personId = new PersonId("materialized-age");
        PersonRuntime person = new PersonRuntime(personId, 5L);
        Assert.That(world.TryRegisterPerson(person, out _), Is.True);

        Assert.That(world.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("materialized-age-definition"),
            "npc-materialized-age",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure materializationFailure), Is.True, materializationFailure.ToString());

        Assert.That(person.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(npc.PersonId, Is.EqualTo(personId));
        Assert.That(typeof(NpcRuntime).GetProperty("BirthAbsoluteDay"), Is.Null);
        Assert.That(PersonAgeQuery.TryCalculate(person, time, UniformCalendar(), out PersonAgeSnapshot age, out _), Is.True);
        Assert.That(age.CompletedYears, Is.EqualTo(1L));
    }

    [Test]
    public void ExplicitLegacyNpcAdoptionKeepsBirthOnPersonOnly()
    {
        NpcRuntime legacyNpc = new NpcRuntime("legacy-age-npc", SimulationTestFactory.CreateNpc("legacy-age-definition"));
        SimulationRuntime world = new SimulationRuntime(new SimulationTime(20L), null, new[] { legacyNpc });
        PersonId personId = new PersonId("legacy-age-person");
        PersonRuntime person = new PersonRuntime(personId, 5L);
        Assert.That(world.TryRegisterPerson(person, out _), Is.True);

        Assert.That(world.TryBindExistingNpcToPerson(personId, legacyNpc.RuntimeId, out PersonMaterializationFailure failure), Is.True, failure.ToString());
        Assert.That(legacyNpc.PersonId, Is.EqualTo(personId));
        Assert.That(typeof(NpcRuntime).GetProperty("BirthAbsoluteDay"), Is.Null);
        Assert.That(GetCompletedYears(person, 20L, UniformCalendar()), Is.EqualTo(1L));
    }

    [Test]
    public void DeadMaterializedNpcDoesNotStopChronologicalAgeQuery()
    {
        SimulationRuntime world = new SimulationRuntime(new SimulationTime(12L), null, null);
        PersonId personId = new PersonId("dead-age-person");
        Assert.That(world.TryRegisterPerson(new PersonRuntime(personId, 0L), out _), Is.True);
        Assert.That(world.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("dead-age-definition"),
            "dead-age-npc",
            null,
            0f,
            out NpcRuntime npc,
            out _), Is.True);
        Assert.That(world.TryApplyPersonDeath(personId, out _, out _), Is.True);

        Assert.That(world.PersonStore.TryGet(personId, out PersonRuntime person), Is.True);
        Assert.That(PersonAgeQuery.TryCalculate(person, world.SimulationTime, UniformCalendar(), out PersonAgeSnapshot age, out PersonAgeQueryFailure failure), Is.True, failure.ToString());
        Assert.That(npc.IsDead, Is.True);
        Assert.That(age.CompletedYears, Is.EqualTo(1L));
        Assert.That(person.IsMaterialized, Is.True);
    }

    [Test]
    public void AgeQueryIsPureAndSupportsLargeAbsoluteDays()
    {
        long currentDay = 1_000_000_000_000L;
        long birthDay = currentDay - 1_000L;
        PersonRuntime person = new PersonRuntime(new PersonId("long-age"), birthDay);
        SimulationTime time = new SimulationTime(currentDay);
        long? birthBefore = person.BirthAbsoluteDay;

        Assert.That(PersonAgeQuery.TryCalculate(person, time, UniformCalendar(), out PersonAgeSnapshot age, out PersonAgeQueryFailure failure), Is.True, failure.ToString());
        Assert.That(age.AgeInDays, Is.EqualTo(1_000L));
        Assert.That(age.BirthAbsoluteDay, Is.EqualTo(birthDay));
        Assert.That(person.BirthAbsoluteDay, Is.EqualTo(birthBefore));
        Assert.That(time.AbsoluteDay, Is.EqualTo(currentDay));
    }

    [Test]
    public void DiagnosticsRepresentKnownAndUnknownBirthWithoutNpcDuplication()
    {
        SimulationTime time = new SimulationTime(17L);
        PersonStore store = new PersonStore();
        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("person-known"), 5L), out _), Is.True);
        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("person-unknown")), out _), Is.True);
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: time,
            calendar: new SimulationCalendar(UniformCalendar()),
            personStore: store));

        WorldStatePersonSnapshot known = FindPerson(snapshot, "person-known");
        WorldStatePersonSnapshot unknown = FindPerson(snapshot, "person-unknown");
        Assert.That(known.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(known.AgeInDays, Is.EqualTo(12L));
        Assert.That(known.CompletedYears, Is.EqualTo(1L));
        Assert.That(unknown.BirthAbsoluteDay, Is.Null);
        Assert.That(unknown.AgeInDays, Is.Null);
        Assert.That(unknown.CompletedYears, Is.Null);
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("PERSON|person-known|~|false|5|1"));
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("PERSON|person-unknown|~|false|~|~"));
        Assert.That(WorldStateSnapshotFormatter.Format(snapshot), Does.Contain("Completed years: 1"));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);
    }

    [Test]
    public void DiagnosticsDiffReportsBirthdayButNotDailyAgeInDaysNoise()
    {
        PersonStore store = new PersonStore();
        Assert.That(store.TryRegister(new PersonRuntime(new PersonId("diff-age"), 5L), out _), Is.True);
        WorldStateSnapshot before = BuildPersonSnapshot(store, 16L);
        WorldStateSnapshot after = BuildPersonSnapshot(store, 17L);
        WorldStateDiff diff = WorldStateDiff.Compare(before, after);
        bool completedYearsChanged = false;

        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == "Person" && difference.Field == "CompletedYears")
            {
                completedYearsChanged = true;
            }

            Assert.That(difference.Field, Is.Not.EqualTo("AgeInDays"));
        }

        Assert.That(completedYearsChanged, Is.True);
    }

    [Test]
    public void DiagnosticsValidatorRejectsNegativeAndFutureBirths()
    {
        WorldStateSnapshot snapshot = new WorldStateSnapshot(
            100L,
            persons: new[]
            {
                new WorldStatePersonSnapshot("negative-birth", -1L, null, null, null),
                new WorldStatePersonSnapshot("future-birth", 101L, null, null, null)
            });

        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(snapshot);
        Assert.That(ContainsIssue(report, "NegativePersonBirthAbsoluteDay"), Is.True);
        Assert.That(ContainsIssue(report, "FuturePersonBirthAbsoluteDay"), Is.True);
    }

    [Test]
    public void LegacyDiagnosticsContextWithoutPersonStoreRemainsValid()
    {
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext());

        Assert.That(snapshot.Persons, Is.Empty);
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);
    }

    private static WorldStateSnapshot BuildPersonSnapshot(PersonStore store, long absoluteDay)
    {
        return WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(absoluteDay),
            calendar: new SimulationCalendar(UniformCalendar()),
            personStore: store));
    }

    private static WorldStatePersonSnapshot FindPerson(WorldStateSnapshot snapshot, string personId)
    {
        foreach (WorldStatePersonSnapshot person in snapshot.Persons)
        {
            if (person != null && person.PersonId == personId)
            {
                return person;
            }
        }

        Assert.Fail("Person was not found: " + personId);
        return null;
    }

    private static long GetCompletedYears(PersonRuntime person, long currentDay, CalendarDefinition calendar)
    {
        Assert.That(PersonAgeQuery.TryCalculate(person, currentDay, calendar, out PersonAgeSnapshot age, out PersonAgeQueryFailure failure), Is.True, failure.ToString());
        return age.CompletedYears;
    }

    private static CalendarDefinition UniformCalendar()
    {
        return new CalendarDefinition(12, 1, 1);
    }

    private static bool ContainsIssue(WorldStateInvariantReport report, string code)
    {
        foreach (WorldStateInvariantIssue issue in report.Issues)
        {
            if (issue != null && issue.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
