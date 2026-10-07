using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using Simulation.WorldExchangeProducer;
using UnityEngine;

public sealed class WorldExchangeV2ProducerTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        for (int index = simulationObjects.Count - 1; index >= 0; index--)
        {
            if (simulationObjects[index] != null)
                UnityEngine.Object.DestroyImmediate(simulationObjects[index]);
        }
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void ArtifactUsesCanonicalIdentityCompleteFactionCoverageAndUnsupportedOtherCollections()
    {
        List<FactionProjectionInput> facts = new List<FactionProjectionInput>
        {
            new FactionProjectionInput("zeta", "Zeta"),
            new FactionProjectionInput("alpha", "  ")
        };

        Assert.That(WorldExchangeV2Artifact.TryCreate(
            "world:12345678123456789abcdef012345678",
            facts,
            out WorldExchangeV2Artifact artifact,
            out string failureCode,
            out string failureMessage), Is.True, failureCode + ": " + failureMessage);

        Assert.That(artifact.SchemaVersion, Is.EqualTo(2));
        Assert.That(artifact.WorldId, Is.EqualTo("world:12345678123456789abcdef012345678"));
        Assert.That(artifact.Factions, Has.Count.EqualTo(2));
        Assert.That(artifact.Factions[0].Id, Is.EqualTo(WorldExchangeV2Artifact.MapFactionId("alpha")));
        Assert.That(artifact.Factions[0].Name, Is.Null);
        Assert.That(artifact.Factions[1].Id, Is.EqualTo(WorldExchangeV2Artifact.MapFactionId("zeta")));
        Assert.That(artifact.Factions[1].Name, Is.EqualTo("Zeta"));
        Assert.That(artifact.CollectionCoverage, Has.Count.EqualTo(9));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Factions), Is.EqualTo(WorldExchangeCoverageStatus.Included));
        AssertUnsupportedCollections(artifact);

        Assert.That(WorldExchangeV2JsonWriter.TrySerialize(artifact, out byte[] json, out failureCode, out failureMessage),
            Is.True, failureCode + ": " + failureMessage);
        string document = Encoding.UTF8.GetString(json);
        Assert.That(document, Does.Contain("\"schemaVersion\":2"));
        Assert.That(document, Does.Contain("\"collectionCoverage\":"));
        Assert.That(document, Does.Contain("\"world\":{\"id\":"));
        Assert.That(document, Does.Not.Contain("\"name\":\"world"));
        Assert.That(document, Does.Not.Contain("\"memberIds\""));
        Assert.That(document, Does.Not.Contain("\"relationships\":[{"));
        Assert.That(document, Does.Not.Contain("\"ideology\""));
        Assert.That(document, Does.Not.Contain("Knowledge"));
        Assert.That(document, Does.Not.Contain("support"));
        Assert.That(document.IndexOf(WorldExchangeV2Artifact.MapFactionId("alpha"), StringComparison.Ordinal),
            Is.LessThan(document.IndexOf(WorldExchangeV2Artifact.MapFactionId("zeta"), StringComparison.Ordinal)));
        Assert.That(document, Does.Not.Contain("NOT_INCLUDED"));
        Assert.That(document.StartsWith("{\"cities\":", StringComparison.Ordinal), Is.True);
        Assert.That(document.EndsWith("}", StringComparison.Ordinal), Is.True);
    }

    [Test]
    public void EmptyFactionOwnerIsKnownEmptyAndNeverConfusedWithUnsupportedCollections()
    {
        Assert.That(WorldExchangeV2Artifact.TryCreate(
            "world:12345678123456789abcdef012345678",
            new FactionProjectionInput[0],
            out WorldExchangeV2Artifact artifact,
            out string failureCode,
            out string failureMessage), Is.True, failureCode + ": " + failureMessage);

        Assert.That(artifact.Factions, Is.Empty);
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Factions), Is.EqualTo(WorldExchangeCoverageStatus.KnownEmpty));
        AssertUnsupportedCollections(artifact);
        Assert.That(WorldExchangeV2JsonWriter.TrySerialize(artifact, out byte[] json, out failureCode, out failureMessage),
            Is.True, failureCode + ": " + failureMessage);
        string document = Encoding.UTF8.GetString(json);
        Assert.That(document, Does.Contain("\"factions\":\"KNOWN_EMPTY\""));
        Assert.That(document, Does.Contain("\"factions\":[]"));
        Assert.That(document, Does.Contain("\"people\":\"UNSUPPORTED\""));
        Assert.That(document, Does.Contain("\"people\":[]"));
        Assert.That(document, Does.Not.Contain("\"factions\":\"INCLUDED\""));
    }

    [Test]
    public void FactionIdentityMappingIsStrictUtf8ReversibleAndStable()
    {
        Assert.That(WorldExchangeV2Artifact.MapFactionId("id/α"), Is.EqualTo("sim:faction:aWQvzrE"));
        Assert.That(WorldExchangeV2Artifact.MapFactionId("id/α"), Is.EqualTo(WorldExchangeV2Artifact.MapFactionId("id/α")));
        Assert.That(WorldExchangeV2Artifact.TryCreate(
            "world:12345678123456789abcdef012345678",
            new[] { new FactionProjectionInput("bad\ud800", "Invalid") },
            out _, out string failureCode, out _), Is.False);
        Assert.That(failureCode, Is.EqualTo("faction-text.invalid-utf8"));
        Assert.That(WorldExchangeV2Artifact.TryCreate(
            "world:12345678123456789abcdef012345678",
            new[] { new FactionProjectionInput("same", "A"), new FactionProjectionInput("same", "B") },
            out _, out failureCode, out _), Is.False);
        Assert.That(failureCode, Is.EqualTo("faction-id.duplicate"));
        Assert.That(WorldExchangeV2Artifact.TryCreate(
            "world:12345678123456789ABCDEF012345678",
            new FactionProjectionInput[0],
            out _, out failureCode, out _), Is.False);
        Assert.That(failureCode, Is.EqualTo("world-id.invalid"));
    }

    [Test]
    public void SerializationIsByteDeterministicAndEscapesNamesWithoutAddingClaims()
    {
        FactionProjectionInput[] first =
        {
            new FactionProjectionInput("b", "Quote \" and line\n"),
            new FactionProjectionInput("a", "Alpha")
        };
        FactionProjectionInput[] second =
        {
            new FactionProjectionInput("a", "Alpha"),
            new FactionProjectionInput("b", "Quote \" and line\n")
        };
        Assert.That(WorldExchangeV2Artifact.TryCreate("world:12345678123456789abcdef012345678", first, out var left, out _, out _), Is.True);
        Assert.That(WorldExchangeV2Artifact.TryCreate("world:12345678123456789abcdef012345678", second, out var right, out _, out _), Is.True);
        Assert.That(WorldExchangeV2JsonWriter.TrySerialize(left, out byte[] leftBytes, out _, out _), Is.True);
        Assert.That(WorldExchangeV2JsonWriter.TrySerialize(right, out byte[] rightBytes, out _, out _), Is.True);
        Assert.That(leftBytes, Is.EqualTo(rightBytes));
        Assert.That(Encoding.UTF8.GetString(leftBytes), Does.Contain("Quote \\\" and line\\n"));
    }

    [Test]
    public void AtomicPublisherWritesAndReplacesOnlyValidatedWorldArtifacts()
    {
        Assert.That(WorldExchangeV2Artifact.TryCreate(
            "world:12345678123456789abcdef012345678",
            new[] { new FactionProjectionInput("one", "One") },
            out WorldExchangeV2Artifact artifact,
            out _, out _), Is.True);

        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Library", "WXD-Publisher-Test-" + Guid.NewGuid().ToString("N"));
        string destination = Path.Combine(directory, "fixture.world.json");
        try
        {
            WorldExchangePublishResult first = WorldExchangeV2FilePublisher.WriteAtomically(artifact, destination);
            Assert.That(first.Succeeded, Is.True, first.FailureCode + ": " + first.FailureMessage);
            byte[] initial = File.ReadAllBytes(destination);
            WorldExchangePublishResult invalid = WorldExchangeV2FilePublisher.WriteAtomically(null, destination);
            Assert.That(invalid.Succeeded, Is.False);
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(initial), "invalid publication must preserve the existing final artifact");
            WorldExchangePublishResult replacement = WorldExchangeV2FilePublisher.WriteAtomically(artifact, destination);
            Assert.That(replacement.Succeeded, Is.True, replacement.FailureCode + ": " + replacement.FailureMessage);
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(initial));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublishedBootstrapExportsOneCoherentFactionProjectionWithoutMembershipOrWorldName()
    {
        TesteSimulacao simulation = CreateSimulation(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1, true);
        SimulationBootstrapComposition composition = simulation.Bootstrap;
        FactionStore factions = GetRuntimeOwner<FactionStore>(simulation.Runtime, "factionStore");

        SimulationWorldExchangeExportResult initiallyEmpty = SimulationWorldExchangeExporter.TryCreateArtifact(composition);
        Assert.That(initiallyEmpty.Succeeded, Is.True, initiallyEmpty.FailureCode + ": " + initiallyEmpty.FailureMessage);
        Assert.That(initiallyEmpty.Artifact.Factions, Is.Empty);
        Assert.That(initiallyEmpty.Artifact.GetCoverage(WorldExchangeCollection.Factions), Is.EqualTo(WorldExchangeCoverageStatus.KnownEmpty));

        PersonRuntime person = new PersonRuntime(new PersonId("person-export-test"));
        Assert.That(simulation.Runtime.PersonStore.TryRegister(person, out PersonStoreFailure personFailure), Is.True, personFailure.ToString());
        FactionRecord zeta = new FactionRecord(new FactionId("zeta/export"), "Zeta", 0L);
        FactionRecord alpha = new FactionRecord(new FactionId("alpha"), "  ", 0L);
        Assert.That(factions.TryRegister(zeta, out FactionFoundationFailure zetaFailure), Is.True, zetaFailure.ToString());
        Assert.That(factions.TryRegister(alpha, out FactionFoundationFailure alphaFailure), Is.True, alphaFailure.ToString());
        Assert.That(factions.TryRegisterAffiliation(
            new FactionAffiliationRecord(zeta.Id, person.PersonId, 0L, affiliationId: new FactionAffiliationId("affiliation-export-test")),
            out FactionFoundationFailure affiliationFailure), Is.True, affiliationFailure.ToString());

        SimulationWorldExchangeExportResult exported = SimulationWorldExchangeExporter.TryCreateArtifact(composition);
        Assert.That(exported.Succeeded, Is.True, exported.FailureCode + ": " + exported.FailureMessage);
        Assert.That(exported.Artifact.WorldId, Is.EqualTo(composition.WorldId.Value));
        Assert.That(exported.Artifact.Factions, Has.Count.EqualTo(2));
        Assert.That(exported.Artifact.Factions[0].Id, Is.EqualTo(WorldExchangeV2Artifact.MapFactionId("alpha")));
        Assert.That(exported.Artifact.Factions[0].Name, Is.Null);
        Assert.That(exported.Artifact.Factions[1].Id, Is.EqualTo(WorldExchangeV2Artifact.MapFactionId("zeta/export")));
        Assert.That(exported.Artifact.GetCoverage(WorldExchangeCollection.Factions), Is.EqualTo(WorldExchangeCoverageStatus.Included));
        AssertUnsupportedCollections(exported.Artifact);

        Assert.That(WorldExchangeV2JsonWriter.TrySerialize(exported.Artifact, out byte[] bytes, out _, out _), Is.True);
        string document = Encoding.UTF8.GetString(bytes);
        Assert.That(document, Does.Not.Contain("\"name\":\"World"));
        Assert.That(document, Does.Not.Contain("memberIds"));
        Assert.That(document, Does.Not.Contain("person-export-test"));
        Assert.That(document, Does.Not.Contain("affiliation-export-test"));
        Assert.That(document, Does.Not.Contain("ideology"));
        Assert.That(document, Does.Not.Contain("support"));
        Assert.That(document, Does.Not.Contain("Knowledge"));

        string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        string fixtureDirectory = Path.Combine(projectDirectory, "Library", "ValidationResults", "WXD-v2-conformance");
        string fixturePath = Path.Combine(fixtureDirectory, "first-faction.world.json");
        SimulationWorldExchangeExportResult published = SimulationWorldExchangeExporter.TryExportToFile(composition, fixturePath);
        Assert.That(published.Succeeded, Is.True, published.FailureCode + ": " + published.FailureMessage);
        Assert.That(File.Exists(fixturePath), Is.True);
    }

    [Test]
    public void UnavailableOrInvalidFactualReadBlocksExportAndPreservesReviewedDiagnostic()
    {
        TesteSimulacao unsupported = CreateSimulation(SimulationRuntimeAdmissionProfile.None, false);
        SimulationWorldExchangeExportResult unsupportedResult = SimulationWorldExchangeExporter.TryCreateArtifact(unsupported.Bootstrap);
        Assert.That(unsupportedResult.Succeeded, Is.False);
        Assert.That(unsupportedResult.Artifact, Is.Null);

        TesteSimulacao selected = CreateSimulation(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1, true);
        FactionStore factions = GetRuntimeOwner<FactionStore>(selected.Runtime, "factionStore");
        FactionRecord faction = new FactionRecord(new FactionId("faction-orphan-test"), "Faction", 0L);
        Assert.That(factions.TryRegister(faction, out FactionFoundationFailure failure), Is.True, failure.ToString());
        Dictionary<string, FactionAffiliationRecord> affiliationRows =
            (Dictionary<string, FactionAffiliationRecord>)typeof(FactionStore)
                .GetField("affiliationsById", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(factions);
        FactionAffiliationRecord invalid = new FactionAffiliationRecord(
            faction.Id,
            new PersonId("missing-person"),
            0L,
            affiliationId: new FactionAffiliationId("affiliation-orphan-test"));
        affiliationRows.Add(invalid.AffiliationId.Value, invalid);

        SimulationWorldExchangeExportResult result = SimulationWorldExchangeExporter.TryCreateArtifact(selected.Bootstrap);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Artifact, Is.Null);
        Assert.That(result.FailureCode, Is.EqualTo("faction.active-affiliation.person-endpoint-missing"));
        Assert.That(result.FailureMessage, Is.EqualTo("An active affiliation references an unavailable Person endpoint."));
        Assert.That(result.DiagnosticCapabilityId, Is.EqualTo(SimulationWorldExchangeExporter.FactionTruthCapabilityId));
    }

    private static void AssertUnsupportedCollections(WorldExchangeV2Artifact artifact)
    {
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.People), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Cities), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Locations), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Organizations), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Institutions), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Items), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.HistoricalEvents), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
        Assert.That(artifact.GetCoverage(WorldExchangeCollection.Relationships), Is.EqualTo(WorldExchangeCoverageStatus.Unsupported));
    }

    private TesteSimulacao CreateSimulation(SimulationRuntimeAdmissionProfile profile, bool authoredGeography)
    {
        SimulationConfigData config;
        if (profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
                "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
            Assert.That(config, Is.Not.Null);
            Assert.That(config.useAuthoredGeographyProfile, Is.EqualTo(authoredGeography));
        }
        else
        {
            config = SimulationTestFactory.CreateSimulationConfig();
            if (authoredGeography) ConfigureGeography(config);
        }
        GameObject simulationObject = new GameObject("wxd-export-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, profile);
        simulation.Start();
        Assert.That(simulation.Bootstrap, Is.Not.Null);
        return simulation;
    }

    private static T GetRuntimeOwner<T>(SimulationRuntime runtime, string fieldName) where T : class
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        T owner = field.GetValue(runtime) as T;
        Assert.That(owner, Is.Not.Null);
        return owner;
    }

    private static void ConfigureGeography(SimulationConfigData config)
    {
        config.useAuthoredGeographyProfile = true;
        config.authoredHexId = "authored-hex-one";
        config.authoredHexQ = 4;
        config.authoredHexR = -2;
        config.authoredTerrainDefinitionId = "terrain/authored-fixture";
        config.authoredTerrainRevisionToken = "rev-7";
        config.authoredLocationId = "authored-location-one";
        config.authoredScaleConventionId = "world-scale/authored-fixture";
        config.authoredScaleSourceIdentity = "profile/authored-fixture";
        config.authoredScaleSourceVersion = "1";
        config.authoredDistancePerNeighborStep = "3.5";
        config.authoredScaleUnit = "league";
    }
}
