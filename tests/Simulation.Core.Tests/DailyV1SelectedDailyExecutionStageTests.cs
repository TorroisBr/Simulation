using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1SelectedDailyExecutionStageTests
{
    [Test]
    public void AdvanceRunsSelectedDailyPhasesInTheEstablishedOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        FakeBoundary boundary = new FakeBoundary();

        DailyV1SelectedDailyExecutionStage.Advance(23L, definitions, boundary);

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "place-content", "logger:23", "demography", "begin-day",
            "directives:23", "economy", "knowledge", "expeditions",
            "autonomous-expeditions", "actor-turns", "travel-and-observe"
        }));
    }

    [Test]
    public void AdvanceOmitsEconomyWhenTheSelectedConfigurationDisablesIt()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithEconomyEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), false);
        FakeBoundary boundary = new FakeBoundary();

        DailyV1SelectedDailyExecutionStage.Advance(0L, definitions, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("economy"));
        Assert.That(boundary.Calls[4], Is.EqualTo("directives:0"));
        Assert.That(boundary.Calls[5], Is.EqualTo("knowledge"));
        Assert.That(boundary.Calls[^1], Is.EqualTo("travel-and-observe"));
    }

    [Test]
    public void AdvanceStopsAtTheFirstFailingDomainPhase()
    {
        FakeBoundary boundary = new FakeBoundary
        {
            ThrowOnDemography = true
        };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                9L, DailyV1CapturedSelectedContentFixture.CreateDefinitions(), boundary));

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "place-content", "logger:9", "demography"
        }));
    }

    [Test]
    public void AdvanceRequiresTheAdmittedDefinitionGraph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, null, new FakeBoundary()));
    }

    private static DailyV1EffectiveDefinitions CopyWithEconomyEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            new EffectiveEconomyConfiguration(enabled),
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            current.MerchantTrade,
            current.CommercialKnowledge);
        return new DailyV1EffectiveDefinitions(
            definitions.Configuration,
            configuration,
            definitions.Calendar,
            null,
            definitions.Cities.Values,
            definitions.Items.Values,
            definitions.Npcs.Values,
            definitions.Jobs.Values,
            definitions.Actions.Values,
            definitions.Statuses.Values,
            null,
            definitions.StageOrder,
            definitions.ProfileContractIdentity,
            definitions.GeographyProfileContractIdentity,
            definitions.GeographyStageIdentity,
            definitions.HexCoordinateConvention,
            definitions.HexCoordinateCanonicalOrder,
            null,
            definitions.Traits.Values,
            definitions.CapabilityAttributes.Values,
            definitions.ExplorableSites.Values);
    }

    private sealed class FakeBoundary : IDailyV1SelectedDailyExecutionBoundary
    {
        internal List<string> Calls { get; } = new List<string>();
        internal bool ThrowOnDemography { get; set; }
        public void AdvancePlaceContent() => Calls.Add("place-content");
        public void BeginLoggerDay(long absoluteDay) => Calls.Add("logger:" + absoluteDay);
        public void AdvanceSelectedDemography()
        {
            Calls.Add("demography");
            if (ThrowOnDemography) throw new InvalidOperationException("demography failed");
        }
        public void BeginSimulationDay() => Calls.Add("begin-day");
        public void PrepareScheduledDirectives(long absoluteDay) => Calls.Add("directives:" + absoluteDay);
        public void SimulateEconomyDay() => Calls.Add("economy");
        public void RefreshLocalKnowledgeAndShare() => Calls.Add("knowledge");
        public void AdvanceActiveExpeditions() => Calls.Add("expeditions");
        public void StartAutonomousExpeditions() => Calls.Add("autonomous-expeditions");
        public void AdvanceActorTurns() => Calls.Add("actor-turns");
        public void AdvanceTravelAndObserve() => Calls.Add("travel-and-observe");
    }
}
