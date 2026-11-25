using System;
using NUnit.Framework;

[TestFixture]
public sealed class PersonNaturalMortalityCoreTests
{
    [Test]
    public void CustomCalendarAndExplicitSamplePreserveDailyProbabilitySemantics()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("mortality-core"), 0L);
        SimulationCalendar calendar = new SimulationCalendar(
            new CalendarDefinition(new[] { 2, 3 }, 1));
        const double annualProbability = 0.25d;
        double expectedDailyProbability = 1d - Math.Pow(0.75d, 1d / 5d);

        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            person,
            7L,
            calendar,
            annualProbability,
            0d,
            out PersonNaturalMortalityEvaluation dies,
            out PersonNaturalMortalityQueryFailure failure), Is.True, failure.ToString());
        Assert.That(dies.CompletedYears, Is.EqualTo(1L));
        Assert.That(dies.DailyProbability, Is.EqualTo(expectedDailyProbability).Within(1e-12d));
        Assert.That(dies.ShouldDie, Is.True);

        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            person,
            7L,
            calendar,
            annualProbability,
            0.99d,
            out PersonNaturalMortalityEvaluation survives,
            out _), Is.True);
        Assert.That(survives.ShouldDie, Is.False);
        Assert.That(person.DeathAbsoluteDay, Is.Null);
    }

    [Test]
    public void InvalidBirthProbabilityAndSampleRemainRejectedWithoutMutation()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(3, 2, 4));
        PersonRuntime unknownBirth = new PersonRuntime(new PersonId("mortality-unknown"));
        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            unknownBirth,
            10L,
            calendar,
            0.2d,
            0.1d,
            out PersonNaturalMortalityEvaluation unknownEvaluation,
            out PersonNaturalMortalityQueryFailure unknownFailure), Is.False);
        Assert.That(unknownEvaluation, Is.Null);
        Assert.That(unknownFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.BirthDateUnknown));

        PersonRuntime living = new PersonRuntime(new PersonId("mortality-invalid"), 0L);
        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            living,
            10L,
            calendar,
            -0.1d,
            0.1d,
            out PersonNaturalMortalityEvaluation invalidProbability,
            out PersonNaturalMortalityQueryFailure probabilityFailure), Is.False);
        Assert.That(invalidProbability, Is.Null);
        Assert.That(probabilityFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.InvalidAnnualProbability));

        Assert.That(PersonNaturalMortalityQuery.TryEvaluate(
            living,
            10L,
            calendar,
            0.2d,
            1d,
            out PersonNaturalMortalityEvaluation invalidSample,
            out PersonNaturalMortalityQueryFailure sampleFailure), Is.False);
        Assert.That(invalidSample, Is.Null);
        Assert.That(sampleFailure, Is.EqualTo(PersonNaturalMortalityQueryFailure.InvalidDeterministicSample));
        Assert.That(living.DeathAbsoluteDay, Is.Null);
    }
}
