using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

/// <summary>
/// Shared characterization for the deterministic demographic policy and its BCL identity input.
/// Sample vectors are recorded from the production Unity implementation before extraction.
/// </summary>
public sealed class DeterministicDemographyPolicyTests
{
    [Test]
    public void SampleVectorsMatchRecordedProductionBits()
    {
        DeterministicDemographicSampleProvider provider = new DeterministicDemographicSampleProvider();
        SampleVector[] vectors =
        {
            new SampleVector("person-ascii-day-zero", "person", "person-1", 0L, 0x3FDF60128CC9CC74L),
            new SampleVector("person-surrogate-pair-positive-day", "person", "person-😀", 42L, 0x3FBCA3AC1E0A24B0L),
            new SampleVector("person-null", "person", null, 0L, 0x3FE0000000000000L),
            new SampleVector("person-negative-day", "person", "person-1", -1L, 0x3FE0000000000000L),
            new SampleVector("person-long-max", "person", "person-1", long.MaxValue, 0x3FE4F04DF1F4E696L),
            new SampleVector("settlement-birth-zero", "birth", "settlement-1", 0L, 0x3FD9FA9DB4FF45DAL),
            new SampleVector("settlement-death-positive", "death", "settlement-1", 42L, 0x3FE1F14E570A7ADEL),
            new SampleVector("settlement-surrogate-pair", "birth", "city-😀", 7L, 0x3FD805461D3A02F2L),
            new SampleVector("settlement-empty", "birth", "", 0L, 0x3FE0000000000000L),
            new SampleVector("settlement-whitespace", "death", "\r\n", 0L, 0x3FE0000000000000L),
            new SampleVector("settlement-null", "death", null, 0L, 0x3FE0000000000000L),
            new SampleVector("settlement-negative-day", "birth", "settlement-1", -2L, 0x3FE0000000000000L),
            new SampleVector("settlement-long-max", "death", "settlement-1", long.MaxValue, 0x3FD6D839F9750656L),
        };

        foreach (SampleVector vector in vectors)
        {
            double actual = vector.EntityKind == "person"
                ? provider.GetSample(vector.Identity == null ? null : new PersonId(vector.Identity), vector.Day)
                : provider.GetSample(vector.Identity, vector.Day, vector.Outcome == "birth"
                    ? AggregateDemographicOutcomeKind.Birth
                    : AggregateDemographicOutcomeKind.Death);
            long bits = BitConverter.DoubleToInt64Bits(actual);
            TestContext.WriteLine("{0}={1:X16} ({2:R})", vector.Name, bits, actual);
            Assert.That(bits, Is.EqualTo(vector.ExpectedBits), vector.Name);
        }
    }

    [Test]
    public void PersonIdRetainsConstructionTryCreateAndOrdinalValueSemantics()
    {
        Assert.Throws<ArgumentException>(() => new PersonId(null));
        Assert.Throws<ArgumentException>(() => new PersonId(" \t"));
        Assert.That(PersonId.TryCreate(null, out PersonId missing), Is.False);
        Assert.That(missing, Is.Null);
        Assert.That(PersonId.TryCreate(" \t", out missing), Is.False);
        Assert.That(missing, Is.Null);
        Assert.That(PersonId.TryCreate("Alice", out PersonId alice), Is.True);
        Assert.That(alice.Value, Is.EqualTo("Alice"));
        Assert.That(alice.ToString(), Is.EqualTo("Alice"));
        Assert.That(alice, Is.EqualTo(new PersonId("Alice")));
        Assert.That(alice == new PersonId("Alice"), Is.True);
        Assert.That(alice != new PersonId("alice"), Is.True);
        Assert.That(alice.Equals((PersonId)null), Is.False);
        Assert.That((PersonId)null == null, Is.True);
        Assert.That((PersonId)null != alice, Is.True);
    }

    [Test]
    public void SamplesAreIndependentOfCallOrderAndCulture()
    {
        DeterministicDemographicSampleProvider provider = new DeterministicDemographicSampleProvider();
        PersonId person = new PersonId("Culture-İ-😀");
        long expected = BitConverter.DoubleToInt64Bits(provider.GetSample(person, 987654321L));
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
            provider.GetSample("other-settlement", 12L, AggregateDemographicOutcomeKind.Death);
            Assert.That(BitConverter.DoubleToInt64Bits(provider.GetSample(person, 987654321L)), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Test]
    public void NonBirthOutcomeValuesRetainDeathFallbackAndSamplingHasNoCallOrderState()
    {
        DeterministicDemographicSampleProvider provider = new DeterministicDemographicSampleProvider();
        double death = provider.GetSample("settlement-1", 42L, AggregateDemographicOutcomeKind.Death);
        double unknownOutcome = provider.GetSample("settlement-1", 42L, (AggregateDemographicOutcomeKind)42);
        Assert.That(BitConverter.DoubleToInt64Bits(unknownOutcome), Is.EqualTo(BitConverter.DoubleToInt64Bits(death)));

        long personBits = BitConverter.DoubleToInt64Bits(provider.GetSample(new PersonId("person-1"), 123456L));
        provider.GetSample("other", long.MaxValue, AggregateDemographicOutcomeKind.Birth);
        provider.GetSample("settlement-1", 42L, AggregateDemographicOutcomeKind.Death);
        Assert.That(BitConverter.DoubleToInt64Bits(provider.GetSample(new PersonId("person-1"), 123456L)), Is.EqualTo(personBits));
    }

    [Test]
    public void AggregatePolicyPreservesThresholdRateValidationAndDeathCap()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        RecordingSamples samples = new RecordingSamples(0.5d, 0.75d);
        DeterministicAggregateDemographyProvider provider = new DeterministicAggregateDemographyProvider(calendar, 360d, 720d, samples);
        AggregateDemographyContext context = new AggregateDemographyContext("town", 4L, 9, 3, 7L);

        AggregateDemographyChange change = provider.GetChange(context);

        Assert.That(change, Is.EqualTo(new AggregateDemographyChange(1, 2)));
        Assert.That(samples.Calls, Is.EqualTo(new[] { "town|7|Birth", "town|7|Death" }));
        Assert.That(context, Is.EqualTo(new AggregateDemographyContext("town", 4L, 9, 3, 7L)));
        Assert.That(context.AggregateOnlyPopulation, Is.EqualTo(6));

        RecordingSamples deathCapSamples = new RecordingSamples(0d, 0d);
        DeterministicAggregateDemographyProvider deathCapProvider = new DeterministicAggregateDemographyProvider(calendar, 0d, 36000d, deathCapSamples);
        Assert.That(deathCapProvider.GetChange(context), Is.EqualTo(new AggregateDemographyChange(0, 6)));
    }

    [Test]
    public void AggregatePolicyEvaluatesSamplesInBirthThenDeathOrderAndShortCircuits()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        List<string> calls = new List<string>();
        DeterministicAggregateDemographyProvider provider = new DeterministicAggregateDemographyProvider(
            calendar, double.NaN, 1d, new DelegateSamples((id, day, outcome) =>
            {
                calls.Add(outcome.ToString());
                return outcome == AggregateDemographicOutcomeKind.Birth ? 0d : 0d;
            }));

        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetChange(
            new AggregateDemographyContext("town", 0L, 10, 0)));
        Assert.That(calls, Is.EqualTo(new[] { "Birth" }));
    }

    [Test]
    public void AggregatePolicyPropagatesInjectedSampleExceptionsAtTheOriginalCallSite()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        List<string> calls = new List<string>();
        DeterministicAggregateDemographyProvider provider = new DeterministicAggregateDemographyProvider(
            calendar, 0d, 0d, new DelegateSamples((id, day, outcome) =>
            {
                calls.Add(outcome.ToString());
                if (outcome == AggregateDemographicOutcomeKind.Death) throw new InvalidOperationException("death sample failed");
                return 0d;
            }));

        Assert.That(() => provider.GetChange(new AggregateDemographyContext("town", 0L, 10, 0)),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("death sample failed"));
        Assert.That(calls, Is.EqualTo(new[] { "Birth", "Death" }));
    }

    [Test]
    public void AggregatePolicyUsesStrictFractionalThresholdAndPreservesIntegerAndZeroRates()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        AggregateDemographyContext context = new AggregateDemographyContext("town", 2L, 20, 0);

        Assert.That(Policy(calendar, 0d, 0d, 0.99d, 0.99d).GetChange(context),
            Is.EqualTo(new AggregateDemographyChange(0, 0)));
        Assert.That(Policy(calendar, 720d, 360d, 0.99d, 0.99d).GetChange(context),
            Is.EqualTo(new AggregateDemographyChange(2, 1)));
        Assert.That(Policy(calendar, 180d, 180d, 0.49999999999999994d, 0.49999999999999994d).GetChange(context),
            Is.EqualTo(new AggregateDemographyChange(1, 1)));
        Assert.That(Policy(calendar, 180d, 180d, 0.5d, 0.5d).GetChange(context),
            Is.EqualTo(new AggregateDemographyChange(0, 0)));
        Assert.That(Policy(calendar, 180d, 180d, 0.5000000000000001d, 0.5000000000000001d).GetChange(context),
            Is.EqualTo(new AggregateDemographyChange(0, 0)));
    }

    [Test]
    public void AggregatePolicySaturatesLargeEventCountsAndDoesNotNormalizeMalformedContext()
    {
        SimulationCalendar oneDayCalendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        AggregateDemographyContext context = new AggregateDemographyContext("town", 0L, 2, 3);
        AggregateDemographyChange change = Policy(oneDayCalendar, double.MaxValue, double.MaxValue, 0d, 0d).GetChange(context);

        Assert.That(change.Births, Is.EqualTo(int.MaxValue));
        Assert.That(change.Deaths, Is.EqualTo(-1));
        Assert.That(context.AggregateOnlyPopulation, Is.EqualTo(-1));
        Assert.That(new AggregateDemographyContext("town", 0L, int.MinValue, int.MaxValue)
            .AggregateOnlyPopulation, Is.EqualTo(1));
    }

    [Test]
    public void AggregatePolicyDefersRateValidationUntilAfterTheCorrespondingSample()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        List<string> calls = new List<string>();
        DeterministicAggregateDemographyProvider provider = new DeterministicAggregateDemographyProvider(
            calendar, double.NaN, double.PositiveInfinity, new DelegateSamples((id, day, outcome) =>
            {
                calls.Add(outcome.ToString());
                return 0d;
            }));

        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetChange(new AggregateDemographyContext("town", 0L, 10, 0)));
        Assert.That(calls, Is.EqualTo(new[] { "Birth" }));
    }

    [Test]
    public void AggregatePolicyRejectsInvalidRatesSamplesAndCalendarInExistingOrder()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        double[] invalidRates = { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
        foreach (double rate in invalidRates)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Policy(calendar, rate, 0d, 0d, 0d)
                .GetChange(new AggregateDemographyContext("town", 0L, 10, 0)), "rate=" + rate);
        }

        double[] invalidSamples = { -double.Epsilon, 1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
        foreach (double sample in invalidSamples)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Policy(calendar, 0d, 0d, sample, 0d)
                .GetChange(new AggregateDemographyContext("town", 0L, 10, 0)), "sample=" + sample);
        }

        Assert.Throws<ArgumentNullException>(() => new DeterministicAggregateDemographyProvider(null, 0d, 0d));
    }

    [Test]
    public void DefaultAndInjectedSamplesUseTheSamePolicyAndValueContractsRetainEquality()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));
        AggregateDemographyContext context = new AggregateDemographyContext("town", 5L, 17, 2, 13L);
        DeterministicAggregateDemographyProvider defaultProvider = new DeterministicAggregateDemographyProvider(calendar, 140d, 280d);
        DeterministicAggregateDemographyProvider explicitDefaultProvider = new DeterministicAggregateDemographyProvider(
            calendar, 140d, 280d, new DeterministicDemographicSampleProvider());

        Assert.That(defaultProvider.GetChange(context), Is.EqualTo(explicitDefaultProvider.GetChange(context)));
        Assert.That(context.Equals(new AggregateDemographyContext("town", 5L, 17, 2, 13L)), Is.True);
        Assert.That(context.GetHashCode(), Is.EqualTo(new AggregateDemographyContext("town", 5L, 17, 2, 13L).GetHashCode()));
        Assert.That(new AggregateDemographyChange(1, 2), Is.EqualTo(new AggregateDemographyChange(1, 2)));
        Assert.That(new AggregateDemographyChange(1, 2).GetHashCode(), Is.EqualTo(new AggregateDemographyChange(1, 2).GetHashCode()));
        Assert.That(typeof(PersonId).Assembly, Is.EqualTo(typeof(DeterministicDemographicSampleProvider).Assembly));
    }

    private static DeterministicAggregateDemographyProvider Policy(
        SimulationCalendar calendar, double births, double deaths, double birthSample, double deathSample)
    {
        return new DeterministicAggregateDemographyProvider(calendar, births, deaths,
            new RecordingSamples(birthSample, deathSample));
    }

    private sealed class SampleVector
    {
        public string Name { get; }
        public string EntityKind { get; }
        public string Outcome { get; }
        public string Identity { get; }
        public long Day { get; }
        public long ExpectedBits { get; }

        public SampleVector(string name, string outcome, string identity, long day, long expectedBits)
        {
            Name = name;
            EntityKind = outcome == "person" ? "person" : "settlement";
            Outcome = outcome;
            Identity = identity;
            Day = day;
            ExpectedBits = expectedBits;
        }
    }

    private sealed class RecordingSamples : IAggregateDemographySampleProvider
    {
        private readonly Queue<double> samples;
        public List<string> Calls { get; } = new List<string>();

        public RecordingSamples(params double[] values) { samples = new Queue<double>(values); }

        public double GetSample(string settlementRuntimeId, long currentAbsoluteDay, AggregateDemographicOutcomeKind outcomeKind)
        {
            Calls.Add(settlementRuntimeId + "|" + currentAbsoluteDay + "|" + outcomeKind);
            return samples.Dequeue();
        }
    }

    private sealed class DelegateSamples : IAggregateDemographySampleProvider
    {
        private readonly Func<string, long, AggregateDemographicOutcomeKind, double> callback;
        public DelegateSamples(Func<string, long, AggregateDemographicOutcomeKind, double> callback) { this.callback = callback; }
        public double GetSample(string settlementRuntimeId, long currentAbsoluteDay, AggregateDemographicOutcomeKind outcomeKind)
            => callback(settlementRuntimeId, currentAbsoluteDay, outcomeKind);
    }
}
