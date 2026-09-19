using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public sealed class PersonMaturityFoundationTests
{
    [Test]
    public void DefaultsExposeExplicitEighteenYearMaturityThreshold()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationDefaults.Create();

        Assert.That(SimulationConfigurationDefaults.DefaultMaturityAgeYears, Is.EqualTo(18L));
        Assert.That(configuration.Population.MaturityAgeYears, Is.EqualTo(18L));
    }

    [Test]
    public void MaturityAgeOverridesResolvePresetThenWorldThenContentAndPreserveZero()
    {
        SimulationConfigurationPreset preset = new SimulationConfigurationPreset(
            "preset",
            new SimulationConfigurationOverrides(
                population: new PopulationConfigurationOverrides(maturityAgeYears: 21L)));
        SimulationConfigurationOverrides world = new SimulationConfigurationOverrides(
            population: new PopulationConfigurationOverrides(maturityAgeYears: 19L));
        SimulationConfigurationOverrides content = new SimulationConfigurationOverrides(
            population: new PopulationConfigurationOverrides(maturityAgeYears: 0L));

        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(
            preset,
            world,
            content).Configuration;

        Assert.That(configuration.Population.MaturityAgeYears, Is.EqualTo(0L));
    }

    [Test]
    public void MaturityAgeOverrideCanBeChangedByWorldWhenContentIsAbsent()
    {
        SimulationConfigurationPreset preset = new SimulationConfigurationPreset(
            "preset",
            new SimulationConfigurationOverrides(
                population: new PopulationConfigurationOverrides(maturityAgeYears: 21L)));
        SimulationConfigurationOverrides world = new SimulationConfigurationOverrides(
            population: new PopulationConfigurationOverrides(maturityAgeYears: 19L));

        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(
            preset,
            world,
            null).Configuration;

        Assert.That(configuration.Population.MaturityAgeYears, Is.EqualTo(19L));
    }

    [Test]
    public void NegativeMaturityAgeIsRejectedByConfigurationValidation()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                population: new PopulationConfigurationOverrides(maturityAgeYears: -1L)),
            null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Configuration, Is.Null);
        Assert.That(ContainsError(result.Validation.Errors, "Maturity age"), Is.True);
    }

    [Test]
    public void PopulationAndSimulationConfigurationEqualityIncludeMaturityAge()
    {
        EffectivePopulationConfiguration firstPopulation = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly,
            18L);
        EffectivePopulationConfiguration secondPopulation = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly,
            19L);

        Assert.That(firstPopulation, Is.Not.EqualTo(secondPopulation));
        Assert.That(firstPopulation.GetHashCode(), Is.Not.EqualTo(secondPopulation.GetHashCode()));

        EffectiveSimulationConfiguration first = CreateSimulationConfiguration(firstPopulation);
        EffectiveSimulationConfiguration second = CreateSimulationConfiguration(secondPopulation);
        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void NewbornAtDefaultThresholdIsNotMature()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("newborn"), 0L);

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            0L,
            UniformYearCalendar(),
            SimulationConfigurationDefaults.DefaultMaturityAgeYears,
            out PersonMaturitySnapshot maturity,
            out PersonMaturityQueryFailure failure), Is.True, failure.ToString());
        Assert.That(maturity.AgeInDays, Is.EqualTo(0L));
        Assert.That(maturity.CompletedYears, Is.EqualTo(0L));
        Assert.That(maturity.MaturityAgeYears, Is.EqualTo(18L));
        Assert.That(maturity.IsMature, Is.False);
    }

    [Test]
    public void MaturityUsesExactBirthdayBoundaryBeforeAndAtThreshold()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("birthday"), 0L);
        CalendarDefinition calendar = UniformYearCalendar();

        Assert.That(Calculate(person, 215L, calendar, 18L).IsMature, Is.False);
        Assert.That(Calculate(person, 216L, calendar, 18L).IsMature, Is.True);
        Assert.That(Calculate(person, 240L, calendar, 18L).IsMature, Is.True);
    }

    [Test]
    public void MaturityRespectsCustomCalendarAnniversaryWithout365DayMath()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("custom-calendar"), 0L);
        CalendarDefinition calendar = new CalendarDefinition(new[] { 2, 3, 4 }, 1);

        Assert.That(Calculate(person, 17L, calendar, 2L).CompletedYears, Is.EqualTo(1L));
        Assert.That(Calculate(person, 17L, calendar, 2L).IsMature, Is.False);
        Assert.That(Calculate(person, 18L, calendar, 2L).CompletedYears, Is.EqualTo(2L));
        Assert.That(Calculate(person, 18L, calendar, 2L).IsMature, Is.True);
    }

    [Test]
    public void ZeroMaturityThresholdMakesKnownBirthMatureImmediately()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("zero-threshold"), 400L);

        Assert.That(Calculate(person, 400L, UniformYearCalendar(), 0L).IsMature, Is.True);
    }

    [Test]
    public void LongAbsoluteDaysRemainLongAndDoNotNarrowToInt()
    {
        long currentDay = 1_000_000_000_000L;
        PersonRuntime person = new PersonRuntime(new PersonId("long-maturity"), currentDay - 1_000L);

        PersonMaturitySnapshot maturity = Calculate(
            person,
            currentDay,
            UniformYearCalendar(),
            80L);

        Assert.That(maturity.AgeInDays, Is.EqualTo(1_000L));
        Assert.That(maturity.CompletedYears, Is.EqualTo(83L));
        Assert.That(maturity.IsMature, Is.True);
    }

    [Test]
    public void UnknownBirthReturnsStructuredFailure()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("unknown-birth"));

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            100L,
            UniformYearCalendar(),
            18L,
            out PersonMaturitySnapshot maturity,
            out PersonMaturityQueryFailure failure), Is.False);
        Assert.That(maturity, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonMaturityQueryFailure.BirthDateUnknown));
    }

    [Test]
    public void FutureBirthReturnsStructuredFailure()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("future-birth"), 101L);

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            100L,
            UniformYearCalendar(),
            18L,
            out PersonMaturitySnapshot maturity,
            out PersonMaturityQueryFailure failure), Is.False);
        Assert.That(maturity, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonMaturityQueryFailure.BirthDateInFuture));
    }

    [Test]
    public void InvalidInputsReturnStructuredFailures()
    {
        Assert.That(PersonMaturityQuery.TryCalculate(
            null,
            0L,
            UniformYearCalendar(),
            18L,
            out _,
            out PersonMaturityQueryFailure invalidPerson), Is.False);
        Assert.That(invalidPerson, Is.EqualTo(PersonMaturityQueryFailure.InvalidPerson));

        PersonRuntime person = new PersonRuntime(new PersonId("invalid-input"), 0L);
        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            -1L,
            UniformYearCalendar(),
            18L,
            out _,
            out PersonMaturityQueryFailure invalidDay), Is.False);
        Assert.That(invalidDay, Is.EqualTo(PersonMaturityQueryFailure.InvalidCurrentDay));

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            0L,
            UniformYearCalendar(),
            -1L,
            out _,
            out PersonMaturityQueryFailure invalidAge), Is.False);
        Assert.That(invalidAge, Is.EqualTo(PersonMaturityQueryFailure.InvalidMaturityAge));

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            0L,
            (CalendarDefinition)null,
            18L,
            out _,
            out PersonMaturityQueryFailure invalidCalendar), Is.False);
        Assert.That(invalidCalendar, Is.EqualTo(PersonMaturityQueryFailure.InvalidCalendar));

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            0L,
            UniformYearCalendar(),
            (EffectivePopulationConfiguration)null,
            out _,
            out PersonMaturityQueryFailure invalidConfiguration), Is.False);
        Assert.That(invalidConfiguration, Is.EqualTo(PersonMaturityQueryFailure.InvalidConfiguration));
    }

    [Test]
    public void ConfigurationOverloadUsesEffectiveMaturityAge()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("configuration-query"), 0L);
        EffectivePopulationConfiguration population = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly,
            2L);

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            24L,
            UniformYearCalendar(),
            population,
            out PersonMaturitySnapshot maturity,
            out PersonMaturityQueryFailure failure), Is.True, failure.ToString());
        Assert.That(maturity.MaturityAgeYears, Is.EqualTo(2L));
        Assert.That(maturity.IsMature, Is.True);
    }

    [Test]
    public void SimulationTimeOverloadAndUnmaterializedPersonAreSupported()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("unmaterialized"), 0L);
        SimulationTime time = new SimulationTime(24L);

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            time,
            UniformYearCalendar(),
            2L,
            out PersonMaturitySnapshot maturity,
            out PersonMaturityQueryFailure failure), Is.True, failure.ToString());
        Assert.That(person.IsMaterialized, Is.False);
        Assert.That(maturity.IsMature, Is.True);
        Assert.That(time.AbsoluteDay, Is.EqualTo(24L));
    }

    [Test]
    public void QueryDoesNotMutatePersonTimeCalendarOrConfiguration()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("pure-query"), 0L);
        SimulationTime time = new SimulationTime(24L);
        CalendarDefinition calendar = UniformYearCalendar();
        EffectivePopulationConfiguration configuration = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly,
            2L);
        long? birthBefore = person.BirthAbsoluteDay;
        long currentBefore = time.AbsoluteDay;
        long daysPerYearBefore = calendar.DaysPerYear;
        long maturityAgeBefore = configuration.MaturityAgeYears;

        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            time.AbsoluteDay,
            calendar,
            configuration,
            out _,
            out PersonMaturityQueryFailure failure), Is.True, failure.ToString());

        Assert.That(person.BirthAbsoluteDay, Is.EqualTo(birthBefore));
        Assert.That(time.AbsoluteDay, Is.EqualTo(currentBefore));
        Assert.That(calendar.DaysPerYear, Is.EqualTo(daysPerYearBefore));
        Assert.That(configuration.MaturityAgeYears, Is.EqualTo(maturityAgeBefore));
    }

    [Test]
    public void MaturityCoreDoesNotReferenceUnityOrWorldAdapters()
    {
        string file = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets",
            "_Project",
            "Scripts",
            "Person",
            "PersonMaturityQuery.cs");
        string source = File.ReadAllText(file);

        Assert.That(source, Does.Not.Contain("UnityEngine"));
        Assert.That(source, Does.Not.Contain("NpcRuntime"));
        Assert.That(source, Does.Not.Contain("SimulationRuntime"));
        Assert.That(source, Does.Not.Contain("GenealogyStore"));
        Assert.That(source, Does.Not.Contain("DateTime.Now"));
    }

    [Test]
    public void MaturitySnapshotIsImmutable()
    {
        foreach (PropertyInfo property in typeof(PersonMaturitySnapshot).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.That(property.CanWrite, Is.False, property.Name);
        }
    }

    private static PersonMaturitySnapshot Calculate(
        PersonRuntime person,
        long currentDay,
        CalendarDefinition calendar,
        long maturityAgeYears)
    {
        Assert.That(PersonMaturityQuery.TryCalculate(
            person,
            currentDay,
            calendar,
            maturityAgeYears,
            out PersonMaturitySnapshot maturity,
            out PersonMaturityQueryFailure failure), Is.True, failure.ToString());
        return maturity;
    }

    private static EffectiveSimulationConfiguration CreateSimulationConfiguration(
        EffectivePopulationConfiguration population)
    {
        return new EffectiveSimulationConfiguration(
            population,
            new EffectiveEconomyConfiguration(true),
            new EffectiveTravelConfiguration(0f),
            new EffectiveCrimeConfiguration(false, false),
            new EffectiveGuardCrimeConfiguration(false));
    }

    private static CalendarDefinition UniformYearCalendar()
    {
        return new CalendarDefinition(12, 1, 1);
    }

    private static bool ContainsError(
        System.Collections.Generic.IReadOnlyList<string> errors,
        string fragment)
    {
        foreach (string error in errors)
        {
            if (error != null && error.Contains(fragment))
            {
                return true;
            }
        }

        return false;
    }
}
