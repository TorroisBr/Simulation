using NUnit.Framework;

[TestFixture]
public sealed class PersonChronologyCoreTests
{
    [Test]
    public void AgeCalculationPreservesUniformCalendarBirthdayBoundary()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("age-core"), 5L);
        CalendarDefinition definition = new CalendarDefinition(12, 1, 1);

        Assert.That(PersonChronologyOwnerBridge.TryCalculateAge(
            person,
            16L,
            definition,
            out PersonAgeSnapshot beforeBirthday,
            out PersonAgeQueryFailure beforeFailure), Is.True, beforeFailure.ToString());
        Assert.That(beforeBirthday.AgeInDays, Is.EqualTo(11L));
        Assert.That(beforeBirthday.CompletedYears, Is.Zero);

        Assert.That(PersonChronologyOwnerBridge.TryCalculateAge(
            person,
            17L,
            definition,
            out PersonAgeSnapshot birthday,
            out PersonAgeQueryFailure birthdayFailure), Is.True, birthdayFailure.ToString());
        Assert.That(birthday.BirthAbsoluteDay, Is.EqualTo(5L));
        Assert.That(birthday.AgeInDays, Is.EqualTo(12L));
        Assert.That(birthday.CompletedYears, Is.EqualTo(1L));
        Assert.That(birthday.BirthDate.Month, Is.EqualTo(6));
        Assert.That(birthday.CurrentDate.Year, Is.EqualTo(1L));
        Assert.That(birthday.CurrentDate.Month, Is.EqualTo(6));
    }

    [Test]
    public void AgeCalculationRejectsUnknownFutureAndInvalidInputsWithoutResult()
    {
        CalendarDefinition definition = new CalendarDefinition(12, 1, 1);
        Assert.That(PersonChronologyOwnerBridge.TryCalculateAge(
            new PersonRuntime(new PersonId("age-unknown-core")),
            10L,
            definition,
            out PersonAgeSnapshot unknown,
            out PersonAgeQueryFailure unknownFailure), Is.False);
        Assert.That(unknown, Is.Null);
        Assert.That(unknownFailure, Is.EqualTo(PersonAgeQueryFailure.BirthDateUnknown));

        Assert.That(PersonChronologyOwnerBridge.TryCalculateAge(
            new PersonRuntime(new PersonId("age-future-core"), 11L),
            10L,
            definition,
            out PersonAgeSnapshot future,
            out PersonAgeQueryFailure futureFailure), Is.False);
        Assert.That(future, Is.Null);
        Assert.That(futureFailure, Is.EqualTo(PersonAgeQueryFailure.BirthDateInFuture));

        Assert.That(PersonChronologyOwnerBridge.TryCalculateAge(
            new PersonRuntime(new PersonId("age-invalid-calendar-core"), 0L),
            10L,
            new CalendarDefinition(0, 1, 1),
            out PersonAgeSnapshot invalidCalendar,
            out PersonAgeQueryFailure calendarFailure), Is.False);
        Assert.That(invalidCalendar, Is.Null);
        Assert.That(calendarFailure, Is.EqualTo(PersonAgeQueryFailure.InvalidCalendar));
    }

    [Test]
    public void MaturityUsesEffectiveThresholdAndExactCompletedYearBoundary()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("maturity-core"), 0L);
        CalendarDefinition definition = new CalendarDefinition(12, 1, 1);
        EffectivePopulationConfiguration configuration = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly,
            1L);

        Assert.That(PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            11L,
            definition,
            configuration,
            out PersonMaturitySnapshot before,
            out PersonMaturityQueryFailure beforeFailure), Is.True, beforeFailure.ToString());
        Assert.That(before.CompletedYears, Is.Zero);
        Assert.That(before.MaturityAgeYears, Is.EqualTo(1L));
        Assert.That(before.IsMature, Is.False);

        Assert.That(PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            12L,
            definition,
            configuration,
            out PersonMaturitySnapshot atBoundary,
            out PersonMaturityQueryFailure boundaryFailure), Is.True, boundaryFailure.ToString());
        Assert.That(atBoundary.CompletedYears, Is.EqualTo(1L));
        Assert.That(atBoundary.IsMature, Is.True);
    }

    [Test]
    public void MaturityRejectsInvalidThresholdAndConfiguration()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("maturity-invalid-core"), 0L);
        CalendarDefinition definition = new CalendarDefinition(12, 1, 1);

        Assert.That(PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            0L,
            definition,
            -1L,
            out PersonMaturitySnapshot invalidThreshold,
            out PersonMaturityQueryFailure thresholdFailure), Is.False);
        Assert.That(invalidThreshold, Is.Null);
        Assert.That(thresholdFailure, Is.EqualTo(PersonMaturityQueryFailure.InvalidMaturityAge));

        Assert.That(PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            0L,
            definition,
            (EffectivePopulationConfiguration)null,
            out PersonMaturitySnapshot missingConfiguration,
            out PersonMaturityQueryFailure configurationFailure), Is.False);
        Assert.That(missingConfiguration, Is.Null);
        Assert.That(configurationFailure, Is.EqualTo(PersonMaturityQueryFailure.InvalidConfiguration));
    }
}
