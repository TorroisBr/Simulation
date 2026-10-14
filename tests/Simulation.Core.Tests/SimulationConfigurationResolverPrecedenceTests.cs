using NUnit.Framework;

public sealed class SimulationConfigurationResolverPrecedenceTests
{
    [Test]
    public void DefaultsAreResolvedBeforePresetWorldAndContentOverrides()
    {
        SimulationConfigurationPreset preset = new SimulationConfigurationPreset("test",
            new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false),
                travel: new TravelConfigurationOverrides(1f)));
        SimulationConfigurationOverrides world = new SimulationConfigurationOverrides(
            economy: new EconomyConfigurationOverrides(true),
            travel: new TravelConfigurationOverrides(2f));
        SimulationConfigurationOverrides content = new SimulationConfigurationOverrides(
            travel: new TravelConfigurationOverrides(3f));

        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(preset, world, content);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Configuration.Economy.Enabled, Is.True);
        Assert.That(result.Configuration.Travel.TravelCostPerDay, Is.EqualTo(3f));
        Assert.That(result.Configuration.Population.RepresentationMode, Is.EqualTo(PopulationRepresentationMode.Aggregate));
        Assert.That(result.Configuration.Population.DecisionScope, Is.EqualTo(NpcDecisionSimulationScope.RelevantOnly));
        Assert.That(result.Configuration.Population.MaturityAgeYears, Is.EqualTo(18L));
        Assert.That(result.Configuration.CommercialKnowledge.FreshForDays, Is.EqualTo(7));
    }

    [Test]
    public void InvalidResolvedOverridesReturnValidationErrorsWithoutConfiguration()
    {
        SimulationConfigurationOverrides content = new SimulationConfigurationOverrides(
            travel: new TravelConfigurationOverrides(-1f));

        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            contentOverrides: content);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Configuration, Is.Null);
        Assert.That(result.Validation.Errors, Does.Contain("Travel cost per day cannot be negative."));
    }
}
