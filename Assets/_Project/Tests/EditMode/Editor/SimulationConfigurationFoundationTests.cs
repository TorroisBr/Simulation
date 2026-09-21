using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class SimulationConfigurationFoundationTests
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
    public void DefaultsOnlyResolveToCanonicalDefaults()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Configuration, Is.EqualTo(SimulationConfigurationDefaults.Create()));
    }

    [Test]
    public void PresetOverridesDefaultsAndLeavesUnspecifiedValuesIntact()
    {
        SimulationConfigurationPreset preset = new SimulationConfigurationPreset(
            "hybrid",
            new SimulationConfigurationOverrides(
                population: new PopulationConfigurationOverrides(
                    PopulationRepresentationMode.Hybrid)));

        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(preset).Configuration;

        Assert.That(configuration.Population.RepresentationMode, Is.EqualTo(PopulationRepresentationMode.Hybrid));
        Assert.That(configuration.Population.DecisionScope, Is.EqualTo(NpcDecisionSimulationScope.RelevantOnly));
        Assert.That(configuration.Economy.Enabled, Is.True);
    }

    [Test]
    public void WorldOverridesPreset()
    {
        SimulationConfigurationPreset preset = new SimulationConfigurationPreset(
            "economy-on",
            new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(true)));
        SimulationConfigurationOverrides world = new SimulationConfigurationOverrides(
            economy: new EconomyConfigurationOverrides(false));

        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(
            preset,
            world,
            null).Configuration;

        Assert.That(configuration.Economy.Enabled, Is.False);
    }

    [Test]
    public void ContentOverridesWorld()
    {
        SimulationConfigurationOverrides world = new SimulationConfigurationOverrides(
            travel: new TravelConfigurationOverrides(4f));
        SimulationConfigurationOverrides content = new SimulationConfigurationOverrides(
            travel: new TravelConfigurationOverrides(7f));

        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(
            null,
            world,
            content).Configuration;

        Assert.That(configuration.Travel.TravelCostPerDay, Is.EqualTo(7f));
    }

    [Test]
    public void ExplicitFalseOverridesTrue()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            new SimulationConfigurationPreset(
                "enabled",
                new SimulationConfigurationOverrides(
                    economy: new EconomyConfigurationOverrides(true))),
            new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false)),
            null);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Configuration.Economy.Enabled, Is.False);
    }

    [Test]
    public void ExplicitZeroOverridesPreviousTravelCost()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                travel: new TravelConfigurationOverrides(3f)),
            new SimulationConfigurationOverrides(
                travel: new TravelConfigurationOverrides(0f))).Configuration;

        Assert.That(configuration.Travel.TravelCostPerDay, Is.EqualTo(0f));
    }

    [Test]
    public void PartialSectionsPreserveOtherFields()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                population: new PopulationConfigurationOverrides(
                    decisionScope: NpcDecisionSimulationScope.AllMaterialized)),
            null).Configuration;

        Assert.That(configuration.Population.RepresentationMode, Is.EqualTo(PopulationRepresentationMode.Aggregate));
        Assert.That(configuration.Population.DecisionScope, Is.EqualTo(NpcDecisionSimulationScope.AllMaterialized));
    }

    [Test]
    public void PresetNameDoesNotAffectEffectiveConfiguration()
    {
        SimulationConfigurationOverrides values = new SimulationConfigurationOverrides(
            crime: new CrimeConfigurationOverrides(true, false));

        EffectiveSimulationConfiguration first = SimulationConfigurationResolver.Resolve(
            new SimulationConfigurationPreset("first", values)).Configuration;
        EffectiveSimulationConfiguration second = SimulationConfigurationResolver.Resolve(
            new SimulationConfigurationPreset("second", values)).Configuration;

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.Crime.Enabled, Is.True);
        Assert.That(first.Crime.AutonomousEnabled, Is.False);
    }

    [Test]
    public void InvalidNaNTravelCostIsRejected()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                travel: new TravelConfigurationOverrides(float.NaN)),
            null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Configuration, Is.Null);
        Assert.That(result.Validation.Errors.Any(error => error.Contains("finite")), Is.True);
    }

    [Test]
    public void InvalidInfinityTravelCostIsRejected()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                travel: new TravelConfigurationOverrides(float.PositiveInfinity)),
            null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Validation.Errors.Any(error => error.Contains("finite")), Is.True);
    }

    [Test]
    public void NegativeTravelCostIsRejected()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                travel: new TravelConfigurationOverrides(-1f)),
            null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Validation.Errors.Any(error => error.Contains("negative")), Is.True);
    }

    [Test]
    public void InvalidEnumsAreRejected()
    {
        EffectiveSimulationConfiguration invalid = new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(
                (PopulationRepresentationMode)999,
                (NpcDecisionSimulationScope)999),
            new EffectiveEconomyConfiguration(true),
            new EffectiveTravelConfiguration(0f),
            new EffectiveCrimeConfiguration(false, false),
            new EffectiveGuardCrimeConfiguration(false));

        SimulationConfigurationValidationResult validation =
            SimulationConfigurationValidator.Validate(invalid);

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.Errors.Count, Is.EqualTo(2));
    }

    [Test]
    public void AutonomousCrimeRequiresEnabledCrime()
    {
        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                crime: new CrimeConfigurationOverrides(false, true)),
            null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Validation.Errors.Any(error => error.Contains("autonomous")), Is.True);
    }

    [Test]
    public void ResolverIsPureAndDeterministic()
    {
        SimulationConfigurationPreset preset = new SimulationConfigurationPreset(
            "stable",
            new SimulationConfigurationOverrides(
                population: new PopulationConfigurationOverrides(
                    PopulationRepresentationMode.Hybrid,
                    NpcDecisionSimulationScope.AllMaterialized),
                travel: new TravelConfigurationOverrides(0f)));
        SimulationConfigurationOverrides world = new SimulationConfigurationOverrides(
            economy: new EconomyConfigurationOverrides(false));
        SimulationConfigurationOverrides content = new SimulationConfigurationOverrides(
            crime: new CrimeConfigurationOverrides(true, false));

        EffectiveSimulationConfiguration first = SimulationConfigurationResolver.Resolve(
            preset,
            world,
            content).Configuration;
        EffectiveSimulationConfiguration second = SimulationConfigurationResolver.Resolve(
            preset,
            world,
            content).Configuration;

        Assert.That(first, Is.EqualTo(second));
        Assert.That(preset.Overrides.Population.RepresentationMode, Is.EqualTo(PopulationRepresentationMode.Hybrid));
        Assert.That(world.Economy.Enabled, Is.False);
        Assert.That(content.Crime.Enabled, Is.True);
        Assert.That(SimulationConfigurationDefaults.Create(), Is.EqualTo(SimulationConfigurationDefaults.Create()));
    }

    [Test]
    public void EffectiveConfigurationIsReadOnly()
    {
        Type[] types =
        {
            typeof(EffectiveSimulationConfiguration),
            typeof(EffectivePopulationConfiguration),
            typeof(EffectiveEconomyConfiguration),
            typeof(EffectiveTravelConfiguration),
            typeof(EffectiveCrimeConfiguration),
            typeof(EffectiveGuardCrimeConfiguration),
            typeof(EffectiveMerchantTradeConfiguration),
            typeof(EffectiveCommercialKnowledgeConfiguration)
        };

        foreach (Type type in types)
        {
            Assert.That(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(property => property.CanWrite),
                Is.False,
                type.FullName);
        }
    }

    [Test]
    public void ConfigurationCoreDoesNotDependOnUnityOrRuntimeState()
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets",
            "_Project",
            "Scripts",
            "Configuration");

        string[] files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
        Assert.That(files, Is.Not.Empty);

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            Assert.That(source, Does.Not.Contain("UnityEngine"), file);
            Assert.That(source, Does.Not.Contain("NpcRuntime"), file);
            Assert.That(source, Does.Not.Contain("DateTime.Now"), file);
        }
    }

    [Test]
    public void SimulationRuntimeLegacyDefaultsExposeEffectiveConfiguration()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);

        Assert.That(runtime.Configuration, Is.EqualTo(SimulationConfigurationDefaults.Create()));
        Assert.That(runtime.Configuration.Economy.Enabled, Is.True);
        Assert.That(runtime.Configuration.GuardCrime.Enabled, Is.False);
    }

    [Test]
    public void InvalidConfigurationIsRejectedBeforeRuntimeCreation()
    {
        EffectiveSimulationConfiguration invalid = new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(
                PopulationRepresentationMode.Aggregate,
                NpcDecisionSimulationScope.RelevantOnly),
            new EffectiveEconomyConfiguration(true),
            new EffectiveTravelConfiguration(float.NaN),
            new EffectiveCrimeConfiguration(false, false),
            new EffectiveGuardCrimeConfiguration(false));

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            configuration: invalid));
    }

    [Test]
    public void EffectiveConfigurationControlsEconomyForIndependentWorlds()
    {
        ItemData item = SimulationTestFactory.CreateItem("config-food");
        CityRuntime economyWorldCity = CreateProducingCity("economy-on", item);
        CityRuntime noEconomyWorldCity = CreateProducingCity("economy-off", item);
        SimulationRuntime economyWorld = new SimulationRuntime(
            new SimulationTime(),
            new[] { economyWorldCity },
            new NpcRuntime[0],
            configuration: CreateRuntimeConfiguration(true));
        SimulationRuntime noEconomyWorld = new SimulationRuntime(
            new SimulationTime(),
            new[] { noEconomyWorldCity },
            new NpcRuntime[0],
            configuration: CreateRuntimeConfiguration(false));

        economyWorld.AdvanceDay();
        noEconomyWorld.AdvanceDay();

        Assert.That(economyWorldCity.Market.GetAmount(item), Is.EqualTo(1));
        Assert.That(noEconomyWorldCity.Market.GetAmount(item), Is.EqualTo(0));
        Assert.That(economyWorld.Configuration, Is.Not.EqualTo(noEconomyWorld.Configuration));
    }

    [Test]
    public void EffectiveMerchantAndCommercialConfigurationUsesResolvedValuesOverAuthoringValues()
    {
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.enabledModules.Add(SimulationModule.Merchant);
        authoring.maxMerchantTradeAmount = 11;
        authoring.allowMerchantTradeRepositioning = true;
        authoring.commercialKnowledge.freshForDays = 4;
        authoring.commercialKnowledge.maxUsefulAgeDays = 18;
        authoring.commercialKnowledge.maxSharedObservationsPerInteraction = 3;

        SimulationConfigurationResolutionResult result = SimulationConfigurationResolver.Resolve(
            null,
            authoring.CreateConfigurationOverrides(),
            new SimulationConfigurationOverrides(
                merchantTrade: new MerchantTradeConfigurationOverrides(maxTradeAmount: 2),
                commercialKnowledge: new CommercialKnowledgeConfigurationOverrides(freshForDays: 9)));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Configuration.MerchantTrade.Enabled, Is.True);
        Assert.That(result.Configuration.MerchantTrade.MaxTradeAmount, Is.EqualTo(2));
        Assert.That(result.Configuration.MerchantTrade.AllowAutonomousTradeRepositioning, Is.True);
        Assert.That(result.Configuration.CommercialKnowledge.FreshForDays, Is.EqualTo(9));
        Assert.That(result.Configuration.CommercialKnowledge.MaxUsefulAgeDays, Is.EqualTo(18));
        Assert.That(result.Configuration.CommercialKnowledge.MaxSharedObservationsPerInteraction, Is.EqualTo(3));
    }

    [Test]
    public void EnabledCapabilityWithoutImplementationFailsCompositionExplicitly()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                merchantTrade: new MerchantTradeConfigurationOverrides(enabled: true),
                crime: new CrimeConfigurationOverrides(enabled: true),
                guardCrime: new GuardCrimeConfigurationOverrides(enabled: true)));

        IReadOnlyList<string> errors = SimulationCompositionValidator.Validate(
            configuration,
            new SimulationCompositionCapabilities(false, false, false));

        Assert.That(errors, Has.Count.EqualTo(3));
        Assert.That(errors, Has.Some.Contains("Merchant trade"));
        Assert.That(errors, Has.Some.Contains("Crime"));
        Assert.That(errors, Has.Some.Contains("Guard crime"));
    }

    [Test]
    public void DisabledCapabilityDoesNotBecomeEnabledFromProviderAvailability()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationDefaults.Create();

        IReadOnlyList<string> errors = SimulationCompositionValidator.Validate(
            configuration,
            new SimulationCompositionCapabilities(true, true, true));

        Assert.That(errors, Is.Empty);
        Assert.That(configuration.MerchantTrade.Enabled, Is.False);
        Assert.That(configuration.Crime.Enabled, Is.False);
        Assert.That(configuration.GuardCrime.Enabled, Is.False);
    }

    [Test]
    public void SimulationModuleSetDoesNotSilentlyRemoveMerchantWhenEconomyIsDisabled()
    {
        SimulationConfigData authoring = SimulationTestFactory.CreateSimulationConfig();
        authoring.enabledModules.Add(SimulationModule.Merchant);

        SimulationModuleSet modules = new SimulationModuleSet(authoring);

        Assert.That(modules.IsEnabled(SimulationModule.Merchant), Is.True);
        Assert.That(modules.IsEnabled(SimulationModule.Economy), Is.False);
    }

    private static EffectiveSimulationConfiguration CreateRuntimeConfiguration(bool economyEnabled)
    {
        return SimulationConfigurationResolver.Resolve(
            null,
            new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(economyEnabled)),
            null).Configuration;
    }

    private static CityRuntime CreateProducingCity(string runtimeId, ItemData item)
    {
        CityData cityData = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        cityData.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 1
        });
        return new CityRuntime(
            runtimeId,
            cityData,
            new SpatialLocationRuntime("location-" + runtimeId));
    }
}
