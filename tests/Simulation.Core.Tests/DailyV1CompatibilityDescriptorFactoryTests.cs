using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1CompatibilityDescriptorFactoryTests
{
    private static DailyV1HostEnvironmentIdentity HostIdentity() =>
        new DailyV1HostEnvironmentIdentity(1, ".NET 8.0", "8.0.1.0", "Windows",
            "win-x64", "x64", "net8.0",
            new[] { new P12AAssemblyIdentityV2("Simulation.Core", "1.0.0.0",
                "00000000-0000-0000-0000-000000000001") }, "ieee754-binary64", 1);

    [Test]
    public void BuildsDotNetDescriptorFromTheExactSelectedDailyDefinitions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();

        Assert.That(DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
            definitions, HostIdentity(), out P12ACompatibilityDescriptorV2 descriptor,
            out string diagnostic), Is.True, diagnostic);

        string p9 = DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out _);
        Assert.That(descriptor.HostKind, Is.EqualTo(1));
        Assert.That(descriptor.ProfileId, Is.EqualTo("UnityBootstrap-Daily-v1"));
        Assert.That(descriptor.ConfigurationFingerprint, Is.EqualTo(p9));
        Assert.That(descriptor.P9Fingerprint, Is.EqualTo(p9));
        Assert.That(descriptor.ContentFingerprint, Is.EqualTo(definitions.ContentFingerprint));
        Assert.That(descriptor.CalendarFingerprint,
            Is.EqualTo(DailyV1DefinitionIdentity.CreateCalendarFingerprint(definitions.Calendar)));
        Assert.That(descriptor.P8Fingerprint, Has.Length.EqualTo(64));
    }

    [Test]
    public void RejectsDefinitionsOutsideTheAdmittedDailyProfileBeforeCreatingIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions invalidStageOrder = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            definitions.Configuration, definitions.EffectiveConfiguration, definitions.Calendar,
            definitions.Cities.Values.Select(value => value.Values),
            definitions.Items.Values.Select(value => value.Values),
            definitions.Npcs.Values.Select(value => value.Values),
            definitions.Jobs.Values.Select(value => value.Values),
            definitions.Actions.Values.Select(value => value.Values),
            definitions.Statuses.Values.Select(value => value.Values),
            new[]
            {
                "p9.genesis.resolve-profile/v1",
                "p9.genesis.authored-world/v1",
                "p9.genesis.authored-actors/v1",
                "p9.genesis.validate-profile/v1",
                "p9.genesis.publish/v1"
            }, definitions.ProfileContractIdentity,
            definitions.GeographyProfileContractIdentity, definitions.GeographyStageIdentity,
            definitions.HexCoordinateConvention, definitions.HexCoordinateCanonicalOrder)
            .WithContentProjection(definitions.ContentProjection);

        Assert.That(DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
            invalidStageOrder, HostIdentity(), out P12ACompatibilityDescriptorV2 descriptor,
            out string diagnostic), Is.False);
        Assert.That(descriptor, Is.Null);
        Assert.That(diagnostic, Is.Not.Empty);
    }

    [Test]
    public void HostKindAndContentAreBoundToCanonicalDescriptorBytes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
            definitions, HostIdentity(), out P12ACompatibilityDescriptorV2 first, out _), Is.True);
        Assert.That(DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
            definitions, HostIdentity(), out P12ACompatibilityDescriptorV2 second, out _), Is.True);
        Assert.That(ContinuationWire.BytesEqual(first.CanonicalBytes, second.CanonicalBytes), Is.True);
        Assert.That(P12ACompatibilityDescriptorV2.TryReadCanonical(
            first.CanonicalBytes, out P12ACompatibilityDescriptorV2 parsed,
            out P12AResultCode result), Is.True);
        Assert.That(result, Is.EqualTo(P12AResultCode.Decoded));
        Assert.That(parsed.MismatchCode(first), Is.EqualTo(P12AResultCode.Decoded));
    }

    [TestCase("0")]
    [TestCase("-1")]
    public void RejectsNonPositiveAuthoredGeographyScale(string distance)
    {
        DailyV1EffectiveDefinitions definitions = WithAuthoredDistance(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), distance);

        Assert.That(DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
            definitions, HostIdentity(), out P12ACompatibilityDescriptorV2 descriptor,
            out string diagnostic), Is.False);
        Assert.That(descriptor, Is.Null);
        Assert.That(diagnostic, Is.Not.Empty);
    }

    private static DailyV1EffectiveDefinitions WithAuthoredDistance(
        DailyV1EffectiveDefinitions definitions, string distance)
    {
        DailyV1SimulationConfigData value = definitions.Configuration;
        DailyV1SimulationConfigData configuration = new DailyV1SimulationConfigData(
            value.simulationName, value.calendar, value.enabledModules, value.cities,
            value.explorableSites, value.npcs, value.actions, value.statuses, value.jobs,
            value.initialWarrants, value.scheduledDirectives, value.useAuthoredGeographyProfile,
            value.authoredHexId, value.authoredHexQ, value.authoredHexR,
            value.authoredTerrainDefinitionId, value.authoredTerrainRevisionToken,
            value.authoredLocationId, value.authoredScaleConventionId,
            value.authoredScaleSourceIdentity, value.authoredScaleSourceVersion, distance,
            value.authoredScaleUnit, value.authoredP10RuinSite,
            value.genesisProfileContractIdentity, value.p10bStableSiteKey, value.travelCostPerDay,
            value.allowMerchantTradeRepositioning, value.maxMerchantTradeAmount,
            value.localMerchantWholesalePriceMultiplier, value.localMerchantReserveRatio,
            value.maxUnprofitablePlanWaitDays, value.crimeAutonomousEnabled,
            value.commercialKnowledge, value.useFixedSimulationSeed, value.simulationSeed,
            value.includeEconomySnapshots, value.economySnapshotIntervalDays, value.logSettings,
            value.naturalMortalityEnabled, value.naturalMortalityAnnualProbability,
            value.aggregateDemographyEnabled, value.aggregateAnnualBirthRate,
            value.aggregateAnnualDeathRate, value.freeStatus, value.wantedStatus,
            value.arrestedStatus, value.hiddenStatus);

        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            configuration, definitions.EffectiveConfiguration, definitions.Calendar,
            definitions.Cities.Values.Select(node => node.Values),
            definitions.Items.Values.Select(node => node.Values),
            definitions.Npcs.Values.Select(node => node.Values),
            definitions.Jobs.Values.Select(node => node.Values),
            definitions.Actions.Values.Select(node => node.Values),
            definitions.Statuses.Values.Select(node => node.Values),
            definitions.StageOrder, definitions.ProfileContractIdentity,
            definitions.GeographyProfileContractIdentity, definitions.GeographyStageIdentity,
            definitions.HexCoordinateConvention, definitions.HexCoordinateCanonicalOrder,
            definitions.Traits.Select(node => new KeyValuePair<string, string>(
                node.Key, node.Value.DisplayName)),
            definitions.CapabilityAttributes.Select(node => new KeyValuePair<string, string>(
                node.Key, node.Value.DisplayName)),
            definitions.ExplorableSites.Values.Select(node => node.Values))
            .WithContentProjection(definitions.ContentProjection);
    }
}
