using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SpatialAuthorityContinuationSnapshotTests
{
    private const string DailyConfigPath = "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset";
    private const string P9GeographyIdentity = "unity-authored-bootstrap/authored-geography-v1";
    private const int P9GeographySchemaVersion = 2;

    private GameObject simulationObject;
    private TesteSimulacao simulation;
    private SpatialAuthorityStore source;

    [SetUp]
    public void SetUp()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(DailyConfigPath);
        Assert.That(config, Is.Not.Null, "The selected Daily-v1 authored profile must be present.");
        Assert.That(config.useAuthoredGeographyProfile, Is.True);
        Assert.That(config.authoredP10RuinSite, Is.Null);

        simulationObject = new GameObject("p12c-spatial-authority-snapshot-test");
        simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        Assert.That(simulation.Bootstrap, Is.Not.Null);
        source = simulation.Bootstrap.SpatialAuthority;
        Assert.That(source, Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        if (simulationObject != null)
        {
            UnityEngine.Object.DestroyImmediate(simulationObject);
        }
    }

    [Test]
    public void SelectedDailyV1Geography_CapturesExactImmutableFactsAndStagesAnEquivalentPrivateOwner()
    {
        Assert.That(simulation.Bootstrap.Manifest.SelectedP9ContractIdentity, Is.EqualTo(P9GeographyIdentity));
        Assert.That(simulation.Bootstrap.Manifest.SelectedP9SchemaVersion, Is.EqualTo(P9GeographySchemaVersion));
        Assert.That(source.Revision, Is.EqualTo(1L));
        Assert.That(source.HexCount, Is.EqualTo(1));
        Assert.That(source.LocationCount, Is.EqualTo(1));
        Assert.That(source.CrossingCount, Is.EqualTo(0));
        Assert.That(source.LocalTopologyBindingCount, Is.EqualTo(0));
        Assert.That(source.PassageAuthority.Options, Is.Empty);
        Assert.That(source.PassageAuthority.Barriers, Is.Empty);
        Assert.That(source.PassageAuthority.OptionStates, Is.Empty);
        Assert.That(source.PassageAuthority.BarrierStates, Is.Empty);

        long sourceRevision = source.Revision;
        HexRecord sourceHex = source.Hexes[0];
        LocationRecord sourceLocation = source.Locations[0];

        Assert.That(TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            simulation.Bootstrap.Manifest.SelectedP9ContractIdentity,
            simulation.Bootstrap.Manifest.SelectedP9SchemaVersion,
            source,
            out object snapshot,
            out object captureFailure), Is.True, FailureMessage(captureFailure));
        Assert.That(captureFailure, Is.Not.Null);
        Assert.That(Read(captureFailure, "IsFailure"), Is.False);
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(Read(snapshot, "SchemaId"), Is.EqualTo("p12c-spatial-authority-geography"));
        Assert.That(Read(snapshot, "SchemaVersion"), Is.EqualTo(1));
        Assert.That(Read(snapshot, "AdmissionProfile"), Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(Read(snapshot, "P9ProfileContractIdentity"), Is.EqualTo(P9GeographyIdentity));
        Assert.That(Read(snapshot, "P9ProfileSchemaVersion"), Is.EqualTo(P9GeographySchemaVersion));
        Assert.That(Read(snapshot, "OwnerRevision"), Is.EqualTo(1L));
        Assert.That(Read(snapshot, "CoordinateConventionVersion"), Is.EqualTo(HexCoordinate.ConventionVersion));
        Assert.That(Read(snapshot, "CoordinateCanonicalOrder"), Is.EqualTo(HexCoordinate.CanonicalOrder));

        IList hexes = (IList)Read(snapshot, "Hexes");
        IList locations = (IList)Read(snapshot, "Locations");
        Assert.That(hexes.Count, Is.EqualTo(1));
        Assert.That(locations.Count, Is.EqualTo(1));
        Assert.That(hexes.IsReadOnly, Is.True);
        Assert.That(locations.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => hexes[0] = null);
        Assert.Throws<NotSupportedException>(() => locations[0] = null);

        object hex = hexes[0];
        object location = locations[0];
        Assert.That(Read(hex, "Id"), Is.EqualTo("hex/sample-origin"));
        Assert.That(Read(hex, "Q"), Is.EqualTo(0));
        Assert.That(Read(hex, "R"), Is.EqualTo(0));
        Assert.That(Read(hex, "TerrainDefinitionId"), Is.EqualTo("terrain/sample-plains"));
        Assert.That(Read(hex, "AuthoredRevisionToken"), Is.EqualTo("sample-world-v1"));
        Assert.That(Read(location, "Id"), Is.EqualTo("location/sample-origin"));
        Assert.That(Read(location, "AnchorHexId"), Is.EqualTo("hex/sample-origin"));

        object scale = Read(snapshot, "ScaleContext");
        Assert.That(Read(scale, "ResolvedConventionId"), Is.EqualTo("world-scale/Simulation-DailyV1/v1"));
        Assert.That(Read(scale, "SourceIdentity"), Is.EqualTo("profile/Simulation-DailyV1"));
        Assert.That(Read(scale, "SourceVersion"), Is.EqualTo("1"));
        Assert.That(Read(scale, "DistancePerNeighborStep"), Is.EqualTo(1m));
        Assert.That(Read(scale, "Unit"), Is.EqualTo("km"));
        PropertyInfo hexIdProperty = hex.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(hexIdProperty, Is.Not.Null);
        Assert.That(hexIdProperty.CanWrite, Is.False);

        Assert.That(TryStage(snapshot, out SpatialAuthorityStore staged, out object stageFailure),
            Is.True, FailureMessage(stageFailure));
        Assert.That(stageFailure, Is.Not.Null);
        Assert.That(Read(stageFailure, "IsFailure"), Is.False);
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.ValidateInvariants().IsValid, Is.True);
        Assert.That(staged.Revision, Is.EqualTo(sourceRevision));
        Assert.That(staged.HexCount, Is.EqualTo(source.HexCount));
        Assert.That(staged.LocationCount, Is.EqualTo(1));
        Assert.That(staged.CrossingCount, Is.EqualTo(0));
        Assert.That(staged.LocalTopologyBindingCount, Is.EqualTo(0));
        Assert.That(staged.PassageAuthority.Options, Is.Empty);
        Assert.That(staged.PassageAuthority.Barriers, Is.Empty);
        Assert.That(staged.PassageAuthority.OptionStates, Is.Empty);
        Assert.That(staged.PassageAuthority.BarrierStates, Is.Empty);
        Assert.That(staged.Hexes[0].Id.Value, Is.EqualTo(sourceHex.Id.Value));
        Assert.That(staged.Hexes[0].Coordinate, Is.EqualTo(sourceHex.Coordinate));
        Assert.That(staged.Hexes[0].TerrainDefinitionId.Value, Is.EqualTo(sourceHex.TerrainDefinitionId.Value));
        Assert.That(staged.Hexes[0].AuthoredRevisionToken, Is.EqualTo(sourceHex.AuthoredRevisionToken));
        Assert.That(staged.Locations[0].Id.Value, Is.EqualTo(sourceLocation.Id.Value));
        Assert.That(staged.Locations[0].AnchorHexId.Value, Is.EqualTo(sourceLocation.AnchorHexId.Value));
        Assert.That(staged.CoordinateConventionVersion, Is.EqualTo(source.CoordinateConventionVersion));
        Assert.That(staged.CoordinateCanonicalOrder, Is.EqualTo(source.CoordinateCanonicalOrder));
        Assert.That(staged.ScaleContext, Is.EqualTo(source.ScaleContext));

        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(source.LocationCount, Is.EqualTo(1));
        Assert.That(source.Locations[0], Is.SameAs(sourceLocation));
        Assert.That(locations.Count, Is.EqualTo(1));
        Assert.That(source.Hexes[0], Is.SameAs(sourceHex));
    }

    [Test]
    public void CapturedSnapshotRemainsDetachedWhenItsSourceOwnerChanges()
    {
        SpatialAuthorityStore mutableOwner = CreateDailyV1Owner();
        IList originalLocationView = (IList)mutableOwner.Locations;
        Assert.That(TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            P9GeographyIdentity,
            P9GeographySchemaVersion,
            mutableOwner,
            out object snapshot,
            out object captureFailure), Is.True, FailureMessage(captureFailure));

        IList snapshotLocations = (IList)Read(snapshot, "Locations");
        Assert.That(snapshotLocations.Count, Is.EqualTo(1));
        Assert.That(mutableOwner.TryRegisterLocation(
            new LocationRecord(new LocationId("location/z-after-capture"), new HexId("hex/sample-origin")),
            out SpatialAuthorityFailure mutationFailure), Is.True, mutationFailure.ToString());
        IList updatedLocationView = (IList)mutableOwner.Locations;

        Assert.That(mutableOwner.ValidateInvariants().IsValid, Is.True);
        Assert.That(mutableOwner.Revision, Is.EqualTo(2L));
        Assert.That(mutableOwner.LocationCount, Is.EqualTo(2));
        Assert.That(originalLocationView.Count, Is.EqualTo(1));
        Assert.That(updatedLocationView.Count, Is.EqualTo(2));
        Assert.That(updatedLocationView, Is.Not.SameAs(originalLocationView));
        Assert.That(snapshotLocations.Count, Is.EqualTo(1));
        Assert.That(Read(snapshotLocations[0], "Id"), Is.EqualTo("location/sample-origin"));

        Assert.That(TryStage(snapshot, out SpatialAuthorityStore staged, out object stageFailure),
            Is.True, FailureMessage(stageFailure));
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged.Revision, Is.EqualTo(1L));
        Assert.That(staged.LocationCount, Is.EqualTo(1));
        Assert.That(staged.Locations[0].Id.Value, Is.EqualTo("location/sample-origin"));
        Assert.That(mutableOwner.Revision, Is.EqualTo(2L));
        Assert.That(mutableOwner.LocationCount, Is.EqualTo(2));
    }

    [Test]
    public void CaptureRejectsWrongAdmissionAndGenesisIdentitiesIndependently()
    {
        AssertCaptureRejected(
            (SimulationRuntimeAdmissionProfile)(-1),
            P9GeographyIdentity,
            P9GeographySchemaVersion,
            source,
            "UnsupportedAdmissionProfile");
        AssertCaptureRejected(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            "wrong-p9-contract",
            P9GeographySchemaVersion,
            source,
            "UnsupportedGenesisProfile");
        AssertCaptureRejected(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            P9GeographyIdentity,
            P9GeographySchemaVersion + 1,
            source,
            "UnsupportedGenesisProfile");
    }

    [Test]
    public void StagingRejectsWrongSchemaAdmissionAndGenesisIdentitiesIndependently()
    {
        object valid = CaptureValidSnapshot();
        object[] args = SnapshotArguments(valid);

        object[] badSchemaId = (object[])args.Clone();
        badSchemaId[0] = "other-schema";
        AssertStageRejected(BuildSnapshot(badSchemaId), "UnsupportedSnapshotSchema");

        object[] badSchemaVersion = (object[])args.Clone();
        badSchemaVersion[1] = 2;
        AssertStageRejected(BuildSnapshot(badSchemaVersion), "UnsupportedSnapshotSchema");

        object[] badAdmission = (object[])args.Clone();
        badAdmission[2] = (SimulationRuntimeAdmissionProfile)(-1);
        AssertStageRejected(BuildSnapshot(badAdmission), "UnsupportedAdmissionProfile");

        object[] badP9Identity = (object[])args.Clone();
        badP9Identity[3] = "wrong-p9-contract";
        AssertStageRejected(BuildSnapshot(badP9Identity), "UnsupportedGenesisProfile");

        object[] badP9Schema = (object[])args.Clone();
        badP9Schema[4] = P9GeographySchemaVersion + 1;
        AssertStageRejected(BuildSnapshot(badP9Schema), "UnsupportedGenesisProfile");
    }

    [Test]
    public void StagingRejectsMalformedCardinalityIdentityAnchorConventionAndScaleFacts()
    {
        object valid = CaptureValidSnapshot();
        object[] original = SnapshotArguments(valid);
        object validHex = ((IList)original[8])[0];
        object validLocation = ((IList)original[9])[0];
        object validScale = original[10];

        AssertMalformed(original, args => args[5] = 2L);
        AssertMalformed(original, args => args[6] = "unknown-coordinate-convention");
        AssertMalformed(original, args => args[7] = "r-then-q");
        AssertMalformed(original, args => args[8] = null);
        AssertMalformed(original, args => args[8] = MakeArray("P12CSpatialHexSnapshot"));
        AssertMalformed(original, args => args[8] = MakeArray("P12CSpatialHexSnapshot", validHex, validHex));
        AssertMalformed(original, args => args[8] = MakeArray("P12CSpatialHexSnapshot", CreateHex("", 0, 0, "terrain/x", "rev-x")));
        AssertMalformed(original, args => args[8] = MakeArray("P12CSpatialHexSnapshot", CreateHex("hex/x", 0, 0, "", "rev-x")));
        AssertMalformed(original, args => args[8] = MakeArray("P12CSpatialHexSnapshot", CreateHex("hex/x", 0, 0, "terrain/x", "")));
        AssertMalformed(original, args => args[8] = MakeArray("P12CSpatialHexSnapshot", (object)null));
        AssertMalformed(original, args => args[9] = null);
        AssertMalformed(original, args => args[9] = MakeArray("P12CSpatialLocationSnapshot"));
        AssertMalformed(original, args => args[9] = MakeArray("P12CSpatialLocationSnapshot", validLocation, validLocation));
        AssertMalformed(original, args => args[9] = MakeArray("P12CSpatialLocationSnapshot", CreateLocation("", "hex/sample-origin")));
        AssertMalformed(original, args => args[9] = MakeArray("P12CSpatialLocationSnapshot", CreateLocation("location/x", "hex/missing")));
        AssertMalformed(original, args => args[9] = MakeArray("P12CSpatialLocationSnapshot", (object)null));
        AssertMalformed(original, args => args[10] = null);
        AssertMalformed(original, args => args[10] = CreateScale("", "profile/x", "1", 1m, "km"));
        AssertMalformed(original, args => args[10] = CreateScale("scale/x", "", "1", 1m, "km"));
        AssertMalformed(original, args => args[10] = CreateScale("scale/x", "profile/x", "", 1m, "km"));
        AssertMalformed(original, args => args[10] = CreateScale("scale/x", "profile/x", "1", 0m, "km"));
        AssertMalformed(original, args => args[10] = CreateScale("scale/x", "profile/x", "1", 1m, ""));
    }

    [Test]
    public void CaptureRejectsInvalidOwnerInvariantsWithoutChangingThatOwner()
    {
        object valid = CaptureValidSnapshot();
        Assert.That(TryStage(valid, out SpatialAuthorityStore malformedOwner, out object stageFailure),
            Is.True, FailureMessage(stageFailure));

        FieldInfo field = typeof(SpatialAuthorityStore).GetField("hexesById", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        IDictionary hexes = (IDictionary)field.GetValue(malformedOwner);
        hexes.Add("corrupt-key", new HexRecord(
            new HexId("hex/corrupt"),
            new HexCoordinate(20, 20),
            new TerrainReference(new TerrainDefinitionId("terrain/corrupt"), "corrupt-v1")));
        int cardinalityBeforeCapture = hexes.Count;
        long revisionBeforeCapture = malformedOwner.Revision;
        Assert.That(malformedOwner.ValidateInvariants().IsValid, Is.False);

        AssertCaptureRejected(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            P9GeographyIdentity,
            P9GeographySchemaVersion,
            malformedOwner,
            "InvalidOwner");

        Assert.That(hexes.Count, Is.EqualTo(cardinalityBeforeCapture));
        Assert.That(malformedOwner.Revision, Is.EqualTo(revisionBeforeCapture));
    }

    [Test]
    public void CaptureRejectsCrossingsPassageBarriersAndLocalTopologyBindings()
    {
        SpatialAuthorityStore crossingOwner = CreateTwoHexOwner();
        HexBoundaryKey boundary = Boundary();
        Assert.That(crossingOwner.TryRegisterCrossing(
            new CrossingRecord(
                new CrossingId("crossing.snapshot-test"),
                boundary,
                new HexId("hex.a"),
                "content.bridge",
                "bridge-v1",
                null),
            out SpatialAuthorityFailure crossingFailure), Is.True, crossingFailure.ToString());
        AssertOwnerRejected(crossingOwner, "ExcludedFactsPresent");

        SpatialAuthorityStore passageOwner = CreateTwoHexOwner();
        Assert.That(passageOwner.PassageAuthority.TryRegisterWildernessRule(
            new PassageOptionRecord(
                TraversalOptionRef.ForWildernessRule("rule.snapshot-test", "v1"),
                boundary,
                "content.wilderness",
                "wilderness-v1"),
            PassageCondition.Available,
            out SpatialAuthorityFailure passageFailure), Is.True, passageFailure.ToString());
        AssertOwnerRejected(passageOwner, "ExcludedFactsPresent");

        SpatialAuthorityStore barrierOwner = CreateTwoHexOwner();
        Assert.That(barrierOwner.PassageAuthority.TryRegisterBarrier(
            new BarrierRecord(
                new BarrierId("barrier.snapshot-test"),
                "content.barrier",
                "barrier-v1",
                new[] { boundary }),
            BarrierCondition.Active,
            out SpatialAuthorityFailure barrierFailure), Is.True, barrierFailure.ToString());
        AssertOwnerRejected(barrierOwner, "ExcludedFactsPresent");

        SpatialAuthorityStore topologyOwner = CreateTwoHexOwner();
        var binding = new SpatialLocalTopologyBinding(
            LocalTopologyOwnerKind.City,
            "city.snapshot-test",
            new LocationId("location.a"));
        IDictionary topologyBindings = (IDictionary)typeof(SpatialAuthorityStore)
            .GetField("topologyBindingsByKey", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(topologyOwner);
        topologyBindings.Add(binding.StableKey, binding);
        AssertOwnerRejected(topologyOwner, "ExcludedFactsPresent");

        SpatialAuthorityStore optionStateOwner = CreateTwoHexOwner();
        Assert.That(optionStateOwner.PassageAuthority.TryRegisterWildernessRule(
            new PassageOptionRecord(
                TraversalOptionRef.ForWildernessRule("rule.state-test", "v1"),
                boundary,
                "content.wilderness",
                "wilderness-v1"),
            PassageCondition.Available,
            out SpatialAuthorityFailure optionStateFailure), Is.True, optionStateFailure.ToString());
        IDictionary options = (IDictionary)typeof(SpatialPassageAuthority)
            .GetField("options", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(optionStateOwner.PassageAuthority);
        options.Clear();
        AssertOwnerRejected(optionStateOwner, "ExcludedFactsPresent");

        SpatialAuthorityStore barrierStateOwner = CreateTwoHexOwner();
        Assert.That(barrierStateOwner.PassageAuthority.TryRegisterBarrier(
            new BarrierRecord(
                new BarrierId("barrier.state-test"),
                "content.barrier",
                "barrier-v1",
                new[] { boundary }),
            BarrierCondition.Active,
            out SpatialAuthorityFailure barrierStateFailure), Is.True, barrierStateFailure.ToString());
        IDictionary barriers = (IDictionary)typeof(SpatialPassageAuthority)
            .GetField("barriers", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(barrierStateOwner.PassageAuthority);
        barriers.Clear();
        AssertOwnerRejected(barrierStateOwner, "ExcludedFactsPresent");
    }

    private void AssertOwnerRejected(SpatialAuthorityStore owner, string expectedCode)
    {
        long revision = owner.Revision;
        int hexCount = owner.HexCount;
        int locationCount = owner.LocationCount;
        AssertCaptureRejected(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            P9GeographyIdentity,
            P9GeographySchemaVersion,
            owner,
            expectedCode);
        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.HexCount, Is.EqualTo(hexCount));
        Assert.That(owner.LocationCount, Is.EqualTo(locationCount));
    }

    private static SpatialAuthorityStore CreateDailyV1Owner()
    {
        var owner = new SpatialAuthorityStore();
        var scale = new SpatialWorldScaleContext(
            "world-scale/Simulation-DailyV1/v1",
            "profile/Simulation-DailyV1",
            "1",
            1m,
            "km");
        var geography = new SpatialGeographyDefinition(
            scale,
            new[]
            {
                new HexRecord(
                    new HexId("hex/sample-origin"),
                    new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain/sample-plains"), "sample-world-v1"))
            },
            new[]
            {
                new LocationRecord(new LocationId("location/sample-origin"), new HexId("hex/sample-origin"))
            });
        Assert.That(owner.TryComposeGeography(geography, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        return owner;
    }

    private static SpatialAuthorityStore CreateTwoHexOwner()
    {
        var owner = new SpatialAuthorityStore();
        var scale = new SpatialWorldScaleContext("scale/test/v1", "profile/test", "1", 1m, "km");
        var geography = new SpatialGeographyDefinition(
            scale,
            new[]
            {
                new HexRecord(new HexId("hex.a"), new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain/a"), "terrain-v1")),
                new HexRecord(new HexId("hex.b"), new HexCoordinate(1, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain/b"), "terrain-v1"))
            },
            new[] { new LocationRecord(new LocationId("location.a"), new HexId("hex.a")) });
        Assert.That(owner.TryComposeGeography(geography, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        return owner;
    }

    private static HexBoundaryKey Boundary()
    {
        return new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b"));
    }

    private object CaptureValidSnapshot()
    {
        Assert.That(TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            simulation.Bootstrap.Manifest.SelectedP9ContractIdentity,
            simulation.Bootstrap.Manifest.SelectedP9SchemaVersion,
            source,
            out object snapshot,
            out object failure), Is.True, FailureMessage(failure));
        return snapshot;
    }

    private static void AssertCaptureRejected(
        SimulationRuntimeAdmissionProfile admissionProfile,
        string p9Identity,
        int p9Schema,
        SpatialAuthorityStore owner,
        string expectedCode)
    {
        bool result = TryCapture(admissionProfile, p9Identity, p9Schema, owner, out object snapshot, out object failure);
        Assert.That(result, Is.False);
        Assert.That(snapshot, Is.Null);
        AssertFailureCode(failure, expectedCode);
    }

    private static bool TryCapture(
        SimulationRuntimeAdmissionProfile admissionProfile,
        string p9Identity,
        int p9Schema,
        SpatialAuthorityStore owner,
        out object snapshot,
        out object failure)
    {
        MethodInfo method = SnapshotType().GetMethod("TryCapture", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { admissionProfile, p9Identity, p9Schema, owner, null, null };
        bool result = (bool)method.Invoke(null, arguments);
        snapshot = arguments[4];
        failure = arguments[5];
        return result;
    }

    private static bool TryStage(object snapshot, out SpatialAuthorityStore staged, out object failure)
    {
        MethodInfo method = SnapshotType().GetMethod("TryCreateStagedFromSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { snapshot, null, null };
        bool result = (bool)method.Invoke(null, arguments);
        staged = arguments[1] as SpatialAuthorityStore;
        failure = arguments[2];
        return result;
    }

    private static void AssertStageRejected(object snapshot, string expectedCode)
    {
        bool result = TryStage(snapshot, out SpatialAuthorityStore staged, out object failure);
        Assert.That(result, Is.False);
        Assert.That(staged, Is.Null);
        AssertFailureCode(failure, expectedCode);
    }

    private static void AssertFailureCode(object failure, string expectedCode)
    {
        Assert.That(failure, Is.Not.Null);
        Assert.That(Read(failure, "IsFailure"), Is.True);
        Assert.That(Read(failure, "Code").ToString(), Is.EqualTo(expectedCode));
        Assert.That(Read(failure, "Message"), Is.Not.Empty);
    }

    private static string FailureMessage(object failure)
    {
        return failure == null ? "Missing typed failure." : failure.ToString();
    }

    private static void AssertMalformed(object[] original, Action<object[]> mutate)
    {
        object[] args = (object[])original.Clone();
        mutate(args);
        AssertStageRejected(BuildSnapshot(args), "InvalidSnapshot");
    }

    private static object[] SnapshotArguments(object snapshot)
    {
        return new[]
        {
            Read(snapshot, "SchemaId"),
            Read(snapshot, "SchemaVersion"),
            Read(snapshot, "AdmissionProfile"),
            Read(snapshot, "P9ProfileContractIdentity"),
            Read(snapshot, "P9ProfileSchemaVersion"),
            Read(snapshot, "OwnerRevision"),
            Read(snapshot, "CoordinateConventionVersion"),
            Read(snapshot, "CoordinateCanonicalOrder"),
            Read(snapshot, "Hexes"),
            Read(snapshot, "Locations"),
            Read(snapshot, "ScaleContext")
        };
    }

    private static object BuildSnapshot(object[] arguments)
    {
        Type type = SnapshotType();
        Type hexType = InnerType("P12CSpatialHexSnapshot");
        Type locationType = InnerType("P12CSpatialLocationSnapshot");
        Type scaleType = InnerType("P12CSpatialScaleSnapshot");
        Type enumerableHex = typeof(IEnumerable<>).MakeGenericType(hexType);
        Type enumerableLocation = typeof(IEnumerable<>).MakeGenericType(locationType);
        Type[] parameterTypes =
        {
            typeof(string), typeof(int), typeof(SimulationRuntimeAdmissionProfile), typeof(string), typeof(int),
            typeof(long), typeof(string), typeof(string), enumerableHex, enumerableLocation, scaleType
        };
        ConstructorInfo constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            parameterTypes,
            null);
        Assert.That(constructor, Is.Not.Null);
        return constructor.Invoke(arguments);
    }

    private static object CreateHex(string id, int q, int r, string terrainId, string terrainRevision)
    {
        return Construct(InnerType("P12CSpatialHexSnapshot"), id, q, r, terrainId, terrainRevision);
    }

    private static object CreateLocation(string id, string anchorHexId)
    {
        return Construct(InnerType("P12CSpatialLocationSnapshot"), id, anchorHexId);
    }

    private static object CreateScale(
        string conventionId,
        string sourceIdentity,
        string sourceVersion,
        decimal distance,
        string unit)
    {
        return Construct(InnerType("P12CSpatialScaleSnapshot"),
            conventionId, sourceIdentity, sourceVersion, distance, unit);
    }

    private static object Construct(Type type, params object[] arguments)
    {
        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            arguments,
            null);
    }

    private static Array MakeArray(string innerTypeName, params object[] items)
    {
        Type elementType = InnerType(innerTypeName);
        Array values = Array.CreateInstance(elementType, items.Length);
        for (int index = 0; index < items.Length; index++)
        {
            values.SetValue(items[index], index);
        }
        return values;
    }

    private static object Read(object instance, string propertyName)
    {
        Assert.That(instance, Is.Not.Null);
        PropertyInfo property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + propertyName + " on " + instance.GetType().FullName);
        return property.GetValue(instance);
    }


    private static Type SnapshotType()
    {
        return InnerType("P12CSpatialAuthoritySnapshot");
    }

    private static Type InnerType(string name)
    {
        Type result = typeof(SpatialAuthorityStore).Assembly.GetType(name, false);
        Assert.That(result, Is.Not.Null, "Missing internal P12-C geography type " + name);
        return result;
    }
}