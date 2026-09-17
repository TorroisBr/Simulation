using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class SimulationConfigurationFoundationTests
{
    [Test]
    public void DomainPolicyAvailableManualIsValid()
    {
        SimulationDomainPolicy policy = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.ManualOnly);

        Assert.That(SimulationConfigurationValidator.ValidateDomainPolicy(policy).IsValid, Is.True);
    }

    [Test]
    public void DomainPolicyAvailableAutonomousIsValid()
    {
        SimulationDomainPolicy policy = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.Autonomous);

        Assert.That(SimulationConfigurationValidator.ValidateDomainPolicy(policy).IsValid, Is.True);
    }

    [Test]
    public void DomainPolicyUnavailableManualIsValid()
    {
        SimulationDomainPolicy policy = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Unavailable,
            SimulationAutonomyMode.ManualOnly);

        Assert.That(SimulationConfigurationValidator.ValidateDomainPolicy(policy).IsValid, Is.True);
    }

    [Test]
    public void DomainPolicyUnavailableAutonomousIsRejected()
    {
        SimulationDomainPolicy policy = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Unavailable,
            SimulationAutonomyMode.Autonomous);

        Assert.That(policy.IsValid, Is.False);
        Assert.That(SimulationConfigurationValidator.ValidateDomainPolicy(policy).IsValid, Is.False);
    }

    [Test]
    public void DomainPolicyEqualityIsValueBased()
    {
        SimulationDomainPolicy first = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.ManualOnly);
        SimulationDomainPolicy second = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.ManualOnly);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first == second, Is.True);
    }

    [Test]
    public void DomainPolicyHashCodeIsStableForEqualValues()
    {
        SimulationDomainPolicy first = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.Autonomous);
        SimulationDomainPolicy second = new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.Autonomous);

        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        Assert.That(first.GetHashCode(), Is.EqualTo(first.GetHashCode()));
    }

    [Test]
    public void PopulationAggregateIsValid()
    {
        Assert.That(SimulationConfigurationValidator.ValidatePopulation(
            new EffectivePopulationConfiguration(
                PopulationRepresentationMode.Aggregate,
                NpcDecisionSimulationScope.RelevantOnly)).IsValid, Is.True);
    }

    [Test]
    public void PopulationHybridIsValid()
    {
        Assert.That(SimulationConfigurationValidator.ValidatePopulation(
            new EffectivePopulationConfiguration(
                PopulationRepresentationMode.Hybrid,
                NpcDecisionSimulationScope.RelevantOnly)).IsValid, Is.True);
    }

    [Test]
    public void PopulationFullyIndividualizedIsValid()
    {
        Assert.That(SimulationConfigurationValidator.ValidatePopulation(
            new EffectivePopulationConfiguration(
                PopulationRepresentationMode.FullyIndividualized,
                NpcDecisionSimulationScope.RelevantOnly)).IsValid, Is.True);
    }

    [Test]
    public void FullyIndividualizedDoesNotImplyAllMaterializedDecisionSimulation()
    {
        EffectivePopulationConfiguration population = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.FullyIndividualized,
            NpcDecisionSimulationScope.RelevantOnly);

        Assert.That(population.RepresentationMode, Is.EqualTo(PopulationRepresentationMode.FullyIndividualized));
        Assert.That(population.DecisionScope, Is.EqualTo(NpcDecisionSimulationScope.RelevantOnly));
        Assert.That(population.IsValid, Is.True);
    }

    [Test]
    public void HybridCanUseRelevantOnlyDecisionScope()
    {
        EffectivePopulationConfiguration population = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Hybrid,
            NpcDecisionSimulationScope.RelevantOnly);

        Assert.That(population.IsValid, Is.True);
    }

    [Test]
    public void AggregateCanUseRelevantOnlyDecisionScope()
    {
        EffectivePopulationConfiguration population = new EffectivePopulationConfiguration(
            PopulationRepresentationMode.Aggregate,
            NpcDecisionSimulationScope.RelevantOnly);

        Assert.That(population.IsValid, Is.True);
    }

    [Test]
    public void EffectiveConfigurationIsImmutable()
    {
        PropertyInfo[] writableProperties = typeof(EffectiveSimulationConfiguration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite)
            .ToArray();

        Assert.That(writableProperties, Is.Empty);
        Assert.That(typeof(EffectivePopulationConfiguration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.CanWrite), Is.False);
        Assert.That(typeof(SimulationDomainPolicy)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.CanWrite), Is.False);
    }

    [Test]
    public void EffectiveConfigurationDefaultIsValid()
    {
        EffectiveSimulationConfiguration configuration = EffectiveSimulationConfiguration.CreateDefault();

        Assert.That(SimulationConfigurationValidator.Validate(configuration).IsValid, Is.True);
    }

    [Test]
    public void EffectiveConfigurationEqualityIfImplementedIsDeterministic()
    {
        EffectiveSimulationConfiguration first = EffectiveSimulationConfiguration.CreateDefault();
        EffectiveSimulationConfiguration second = EffectiveSimulationConfiguration.CreateDefault();

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void ValidatorDoesNotMutateConfiguration()
    {
        EffectiveSimulationConfiguration configuration = EffectiveSimulationConfiguration.CreateDefault();
        EffectivePopulationConfiguration populationBefore = configuration.Population;
        SimulationDomainPolicy crimeBefore = configuration.CrimePolicy;

        SimulationConfigurationValidator.Validate(configuration);

        Assert.That(configuration.Population, Is.SameAs(populationBefore));
        Assert.That(configuration.CrimePolicy, Is.SameAs(crimeBefore));
    }

    [Test]
    public void ValidatorIsDeterministic()
    {
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(
                (PopulationRepresentationMode)999,
                NpcDecisionSimulationScope.RelevantOnly),
            new SimulationDomainPolicy(
                SimulationFeatureAvailability.Unavailable,
                SimulationAutonomyMode.Autonomous),
            new SimulationDomainPolicy(
                SimulationFeatureAvailability.Available,
                SimulationAutonomyMode.ManualOnly),
            new SimulationDomainPolicy(
                SimulationFeatureAvailability.Available,
                SimulationAutonomyMode.ManualOnly),
            new SimulationDomainPolicy(
                SimulationFeatureAvailability.Available,
                SimulationAutonomyMode.ManualOnly));

        SimulationConfigurationValidationResult first = SimulationConfigurationValidator.Validate(configuration);
        SimulationConfigurationValidationResult second = SimulationConfigurationValidator.Validate(configuration);

        Assert.That(first.IsValid, Is.False);
        Assert.That(second.IsValid, Is.False);
        Assert.That(first.Errors, Is.EqualTo(second.Errors));
    }

    [Test]
    public void ValidatorRejectsInvalidDomainPolicy()
    {
        EffectiveSimulationConfiguration configuration = CreateConfiguration(
            new SimulationDomainPolicy(
                SimulationFeatureAvailability.Unavailable,
                SimulationAutonomyMode.Autonomous));

        SimulationConfigurationValidationResult result = SimulationConfigurationValidator.Validate(configuration);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(error => error.Contains("cannot be autonomous")), Is.True);
    }

    [Test]
    public void ValidatorRejectsUnknownEnumValues()
    {
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(
                (PopulationRepresentationMode)999,
                (NpcDecisionSimulationScope)999),
            new SimulationDomainPolicy(
                (SimulationFeatureAvailability)999,
                (SimulationAutonomyMode)999),
            ValidPolicy(),
            ValidPolicy(),
            ValidPolicy());

        SimulationConfigurationValidationResult result = SimulationConfigurationValidator.Validate(configuration);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void ValidatorRejectsNullPopulationSection()
    {
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            null,
            ValidPolicy(),
            ValidPolicy(),
            ValidPolicy(),
            ValidPolicy());

        SimulationConfigurationValidationResult result = SimulationConfigurationValidator.Validate(configuration);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(error => error.Contains("Population is required")), Is.True);
    }

    [Test]
    public void ProductionConfigurationFilesDoNotReferenceUnityEngine()
    {
        AssertProductionConfigurationFilesDoNotContain("UnityEngine");
    }

    [Test]
    public void ConfigurationFoundationDoesNotReferenceNpcRuntime()
    {
        AssertProductionConfigurationFilesDoNotContain("NpcRuntime");
    }

    [Test]
    public void ConfigurationFoundationDoesNotReferenceCityRuntime()
    {
        AssertProductionConfigurationFilesDoNotContain("CityRuntime");
    }

    [Test]
    public void ConfigurationFoundationDoesNotReferenceSettlementPopulationRuntime()
    {
        AssertProductionConfigurationFilesDoNotContain("SettlementPopulationRuntime");
    }

    [Test]
    public void PresetConceptDoesNotRequireAlternateRuntimePath()
    {
        string documentation = File.ReadAllText(GetDocumentationPath());

        Assert.That(documentation, Does.Contain("Preset = a set of values"));
        Assert.That(documentation, Does.Contain("does not require an alternate runtime path"));
    }

    private static EffectiveSimulationConfiguration CreateConfiguration(SimulationDomainPolicy mobilityPolicy)
    {
        return new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(
                PopulationRepresentationMode.Aggregate,
                NpcDecisionSimulationScope.RelevantOnly),
            mobilityPolicy,
            ValidPolicy(),
            ValidPolicy(),
            ValidPolicy());
    }

    private static SimulationDomainPolicy ValidPolicy()
    {
        return new SimulationDomainPolicy(
            SimulationFeatureAvailability.Available,
            SimulationAutonomyMode.ManualOnly);
    }

    private static void AssertProductionConfigurationFilesDoNotContain(string text)
    {
        string directory = GetConfigurationDirectory();
        string[] files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);

        Assert.That(files, Is.Not.Empty);
        foreach (string file in files)
        {
            Assert.That(File.ReadAllText(file), Does.Not.Contain(text), file);
        }
    }

    private static string GetConfigurationDirectory()
    {
        return Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets",
            "_Project",
            "Scripts",
            "Configuration");
    }

    private static string GetDocumentationPath()
    {
        return Path.Combine(
            Directory.GetCurrentDirectory(),
            "Docs",
            "Architecture",
            "SimulationConfiguration.md");
    }
}
