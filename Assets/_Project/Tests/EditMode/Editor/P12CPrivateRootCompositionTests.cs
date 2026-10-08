using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12CPrivateRootCompositionTests
{
    private const string DailyConfigPath = "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset";
    private const string P9GeographyIdentity = "unity-authored-bootstrap/authored-geography-v1";
    private const int P9GeographySchema = 2;

    private P12CP9GenesisManifestSnapshot manifestSnapshot;
    private object spatialSnapshot;
    private RuntimeIdAllocatorSnapshot allocatorSnapshot;
    private SimulationRecordSequenceSnapshot recordSequenceSnapshot;
    private DeterministicRandomRootSnapshot randomRootSnapshot;
    private SpatialAuthorityStore sourceSpatialOwner;
    private SimulationGenesisManifest sourceManifestOwner;
    private long sourceSpatialRevision;
    private string sourceManifestFingerprint;

    [OneTimeSetUp]
    public void CreateSelectedDailyV1Snapshots()
    {
        SimulationTestFactory.CleanupDefinitions();
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(DailyConfigPath);
        Assert.That(config, Is.Not.Null);
        Assert.That(config.useAuthoredGeographyProfile, Is.True);
        Assert.That(config.authoredP10RuinSite, Is.Null);

        GameObject simulationObject = new GameObject("p12c-private-root-composition-test");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        Assert.That(simulation.Bootstrap, Is.Not.Null);
        SimulationGenesisManifest sourceManifest = simulation.Bootstrap.Manifest;
        SpatialAuthorityStore sourceSpatial = simulation.Bootstrap.SpatialAuthority;
        sourceManifestOwner = sourceManifest;
        sourceSpatialOwner = sourceSpatial;
        Assert.That(sourceManifest.SelectedP9ContractIdentity, Is.EqualTo(P9GeographyIdentity));
        Assert.That(sourceManifest.SelectedP9SchemaVersion, Is.EqualTo(P9GeographySchema));
        Assert.That(sourceSpatial, Is.Not.Null);
        sourceSpatialRevision = sourceSpatial.Revision;
        sourceManifestFingerprint = sourceManifest.Fingerprint;

        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            sourceManifest,
            out manifestSnapshot,
            out string manifestDiagnostic), Is.True, manifestDiagnostic);
        Assert.That(TryCaptureSpatial(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            sourceManifest.SelectedP9ContractIdentity,
            sourceManifest.SelectedP9SchemaVersion,
            sourceSpatial,
            out spatialSnapshot,
            out object spatialFailure), Is.True, FailureMessage(spatialFailure));

        allocatorSnapshot = CreateAllocatorSnapshotWithGaps();
        recordSequenceSnapshot = new SimulationRecordSequenceSnapshot(
            SimulationRecordSequenceSnapshot.CurrentSchemaId,
            SimulationRecordSequenceSnapshot.CurrentSchemaVersion,
            37L);
        randomRootSnapshot = new DeterministicRandomSource(sourceManifest.Seed).CaptureSnapshot();

        UnityEngine.Object.DestroyImmediate(simulationObject);
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void CoherentDailyV1OwnerSnapshotsStageTogetherWithoutLiveBootstrapOrAssetAccess()
    {
        Assert.That(manifestSnapshot.TryStageManifest(out SimulationGenesisManifest independentlyStagedManifest,
            out string manifestDiagnostic), Is.True, manifestDiagnostic);
        Assert.That(TryStageSpatial(spatialSnapshot, out SpatialAuthorityStore independentlyStagedSpatial,
            out object spatialFailure), Is.True, FailureMessage(spatialFailure));
        Assert.That(independentlyStagedSpatial.HexCount, Is.EqualTo(1));
        Assert.That(independentlyStagedManifest.Seed, Is.EqualTo(manifestSnapshot.Seed));

        Assert.That(TryStage(
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            manifestSnapshot,
            randomRootSnapshot,
            out object staged,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(staged, Is.Not.Null);
        RuntimeIdAllocator stagedAllocator = (RuntimeIdAllocator)Read(staged, "RuntimeIdAllocator");
        SimulationRecordSequence stagedSequence = (SimulationRecordSequence)Read(staged, "RecordSequence");
        Assert.That(stagedAllocator, Is.TypeOf<RuntimeIdAllocator>());
        Assert.That(stagedSequence, Is.TypeOf<SimulationRecordSequence>());
        Assert.That(stagedAllocator.CaptureSnapshot().Counters.Count, Is.EqualTo(allocatorSnapshot.Counters.Count));
        for (int i = 0; i < allocatorSnapshot.Counters.Count; i++)
        {
            Assert.That(stagedAllocator.CaptureSnapshot().Counters[i].FamilyId,
                Is.EqualTo(allocatorSnapshot.Counters[i].FamilyId));
            Assert.That(stagedAllocator.CaptureSnapshot().Counters[i].NextSequence,
                Is.EqualTo(allocatorSnapshot.Counters[i].NextSequence));
        }
        Assert.That(stagedSequence.CaptureSnapshot().NextSequence, Is.EqualTo(recordSequenceSnapshot.NextSequence));
        Assert.That(Read(staged, "SpatialAuthority"), Is.TypeOf<SpatialAuthorityStore>());
        Assert.That(Read(staged, "GenesisManifest"), Is.TypeOf<SimulationGenesisManifest>());
        Assert.That(Read(staged, "DeterministicRandom"), Is.TypeOf<DeterministicRandomSource>());
        Assert.That(((SimulationGenesisManifest)Read(staged, "GenesisManifest")).Fingerprint,
            Is.EqualTo(sourceManifestFingerprint));
        Assert.That(((DeterministicRandomSource)Read(staged, "DeterministicRandom")).Seed,
            Is.EqualTo(manifestSnapshot.Seed));
        Assert.That(((DeterministicRandomSource)Read(staged, "DeterministicRandom")).NextUnit("p12c-root-proof", 9L),
            Is.EqualTo(new DeterministicRandomSource(manifestSnapshot.Seed).NextUnit("p12c-root-proof", 9L)));
        Assert.That(((SpatialAuthorityStore)Read(staged, "SpatialAuthority")).Revision, Is.EqualTo(1L));
    }

    [TestCase("hex-id")]
    [TestCase("hex-q")]
    [TestCase("hex-r")]
    [TestCase("terrain-id")]
    [TestCase("terrain-revision")]
    [TestCase("location-id")]
    [TestCase("scale-convention")]
    [TestCase("scale-source")]
    [TestCase("scale-version")]
    [TestCase("scale-distance")]
    [TestCase("scale-unit")]
    public void RejectsIndividuallyValidP8FactsThatDisagreeWithP9Provenance(string changedFact)
    {
        object changedSpatial = ChangeSpatialFact(spatialSnapshot, changedFact);
        Assert.That(TryStageSpatial(changedSpatial, out SpatialAuthorityStore stagedSpatial,
            out object spatialFailure), Is.True, FailureMessage(spatialFailure),
            "The changed P8 snapshot must remain locally valid so this exercises the cross-owner check.");
        Assert.That(stagedSpatial, Is.Not.Null);

        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            changedSpatial,
            manifestSnapshot,
            randomRootSnapshot);
    }

    [TestCase("coordinate-convention")]
    [TestCase("coordinate-order")]
    [TestCase("p9-location-anchor")]
    public void RejectsP9GeographyProvenanceThatDisagreesWithTheValidP8Owner(string changedFact)
    {
        P12CP9GenesisManifestSnapshot changedManifest = ChangeP9GeographyFact(manifestSnapshot, spatialSnapshot, changedFact);
        Assert.That(changedManifest.TryStageManifest(out SimulationGenesisManifest stagedManifest,
            out string manifestDiagnostic), Is.True, manifestDiagnostic,
            "The changed P9 snapshot must remain locally valid so this exercises the cross-owner check.");
        Assert.That(stagedManifest, Is.Not.Null);

        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            changedManifest,
            randomRootSnapshot);
    }

    [TestCase("authored-hex")]
    [TestCase("authored-location")]
    [TestCase("authored-scale")]
    public void RejectsAContradictoryDuplicateP9GeographyRecordEvenWhenOneCopyMatchesP8(string tag)
    {
        P12CP9GenesisManifestSnapshot duplicatedManifest = DuplicateP9GeographyRecord(manifestSnapshot, spatialSnapshot, tag);
        Assert.That(duplicatedManifest.TryStageManifest(out SimulationGenesisManifest stagedManifest,
            out string manifestDiagnostic), Is.True, manifestDiagnostic,
            "The duplicate must survive owner-local validation so this exercises aggregate tag cardinality.");
        Assert.That(stagedManifest, Is.Not.Null);

        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            duplicatedManifest,
            randomRootSnapshot);
    }

    [Test]
    public void RejectsRandomSeedMismatchAndMalformedIdentityOrSequenceRootsWithoutReturningAPartialBundle()
    {
        DeterministicRandomRootSnapshot wrongRandom = new DeterministicRandomRootSnapshot(
            randomRootSnapshot.SchemaId,
            randomRootSnapshot.SchemaVersion,
            randomRootSnapshot.ProviderId,
            randomRootSnapshot.ProviderVersion,
            randomRootSnapshot.AlgorithmId,
            randomRootSnapshot.AlgorithmVersion,
            randomRootSnapshot.Seed + 1);
        AssertWholeRootRejected(allocatorSnapshot, recordSequenceSnapshot, spatialSnapshot, manifestSnapshot, wrongRandom);

        RuntimeIdAllocatorSnapshot duplicateAllocatorFamily = new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            new[]
            {
                new RuntimeIdAllocatorCounterSnapshot("npc", 1L),
                new RuntimeIdAllocatorCounterSnapshot("npc", 2L)
            });
        AssertWholeRootRejected(duplicateAllocatorFamily, recordSequenceSnapshot, spatialSnapshot, manifestSnapshot,
            randomRootSnapshot);

        SimulationRecordSequenceSnapshot invalidSequence = new SimulationRecordSequenceSnapshot(
            SimulationRecordSequenceSnapshot.CurrentSchemaId,
            SimulationRecordSequenceSnapshot.CurrentSchemaVersion,
            0L);
        AssertWholeRootRejected(allocatorSnapshot, invalidSequence, spatialSnapshot, manifestSnapshot, randomRootSnapshot);

        AssertWholeRootRejected(null, recordSequenceSnapshot, spatialSnapshot, manifestSnapshot, randomRootSnapshot);
    }

    [TestCase("spatial-profile")]
    [TestCase("manifest-profile")]
    [TestCase("p9-contract")]
    [TestCase("p9-schema")]
    public void RejectsAdmissionAndP8P9ContractMismatches(string mismatch)
    {
        object changedSpatial = spatialSnapshot;
        P12CP9GenesisManifestSnapshot changedManifest = manifestSnapshot;
        switch (mismatch)
        {
            case "spatial-profile":
                changedSpatial = BuildSpatialSnapshot(
                    spatialSnapshot,
                    (System.Collections.IEnumerable)Read(spatialSnapshot, "Hexes"),
                    (System.Collections.IEnumerable)Read(spatialSnapshot, "Locations"),
                    Read(spatialSnapshot, "ScaleContext"),
                    SimulationRuntimeAdmissionProfile.None);
                break;
            case "manifest-profile":
                changedManifest = CopyManifestForTest(
                    manifestSnapshot,
                    admissionProfile: SimulationRuntimeAdmissionProfile.None);
                break;
            case "p9-contract":
                changedSpatial = BuildSpatialSnapshot(
                    spatialSnapshot,
                    (System.Collections.IEnumerable)Read(spatialSnapshot, "Hexes"),
                    (System.Collections.IEnumerable)Read(spatialSnapshot, "Locations"),
                    Read(spatialSnapshot, "ScaleContext"),
                    p9ContractIdentity: "unrelated-p9-contract/v1");
                break;
            case "p9-schema":
                changedSpatial = BuildSpatialSnapshot(
                    spatialSnapshot,
                    (System.Collections.IEnumerable)Read(spatialSnapshot, "Hexes"),
                    (System.Collections.IEnumerable)Read(spatialSnapshot, "Locations"),
                    Read(spatialSnapshot, "ScaleContext"),
                    p9SchemaVersion: P9GeographySchema + 1);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mismatch), mismatch, "Unknown mismatch.");
        }

        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            changedSpatial,
            changedManifest,
            randomRootSnapshot);
    }

    [TestCase("missing")]
    [TestCase("duplicate")]
    public void RejectsMissingOrDuplicateP9SpatialOutputOwnerReference(string change)
    {
        List<string> outputOwners = new List<string>(manifestSnapshot.OutputOwners);
        if (change == "missing")
            outputOwners.Remove("SpatialAuthorityStore");
        else
            outputOwners.Add("SpatialAuthorityStore");

        P12CP9GenesisManifestSnapshot changedManifest = CopyManifestForTest(
            manifestSnapshot,
            outputOwners: outputOwners,
            replaceOutputOwners: true);
        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            changedManifest,
            randomRootSnapshot);
    }

    [TestCase("allocator")]
    [TestCase("sequence")]
    [TestCase("spatial")]
    [TestCase("manifest")]
    [TestCase("random")]
    public void RejectsNullOwnerSnapshotsWithoutReturningPartialRoots(string owner)
    {
        RuntimeIdAllocatorSnapshot allocator = allocatorSnapshot;
        SimulationRecordSequenceSnapshot sequence = recordSequenceSnapshot;
        object spatial = spatialSnapshot;
        P12CP9GenesisManifestSnapshot manifest = manifestSnapshot;
        DeterministicRandomRootSnapshot random = randomRootSnapshot;
        switch (owner)
        {
            case "allocator": allocator = null; break;
            case "sequence": sequence = null; break;
            case "spatial": spatial = null; break;
            case "manifest": manifest = null; break;
            case "random": random = null; break;
            default: throw new ArgumentOutOfRangeException(nameof(owner), owner, "Unknown owner.");
        }

        AssertWholeRootRejected(allocator, sequence, spatial, manifest, random);
    }

    [TestCase("unsupported-schema")]
    [TestCase("duplicate-family")]
    [TestCase("missing-family")]
    [TestCase("invalid-next")]
    [TestCase("exhausted-next")]
    public void RejectsMalformedAllocatorRootAsPartOfTheWholeComposition(string defect)
    {
        List<RuntimeIdAllocatorCounterSnapshot> counters = new List<RuntimeIdAllocatorCounterSnapshot>();
        for (int i = 0; i < allocatorSnapshot.Counters.Count; i++)
        {
            RuntimeIdAllocatorCounterSnapshot counter = allocatorSnapshot.Counters[i];
            counters.Add(new RuntimeIdAllocatorCounterSnapshot(counter.FamilyId, counter.NextSequence));
        }

        string schemaId = RuntimeIdAllocatorSnapshot.CurrentSchemaId;
        int schemaVersion = RuntimeIdAllocatorSnapshot.CurrentSchemaVersion;
        switch (defect)
        {
            case "unsupported-schema": schemaVersion++; break;
            case "duplicate-family": counters.Add(new RuntimeIdAllocatorCounterSnapshot("npc", 1L)); break;
            case "missing-family": counters.RemoveAt(counters.Count - 1); break;
            case "invalid-next": counters[0] = new RuntimeIdAllocatorCounterSnapshot(counters[0].FamilyId, 0L); break;
            case "exhausted-next": counters[0] = new RuntimeIdAllocatorCounterSnapshot(counters[0].FamilyId, long.MaxValue); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect), defect, "Unknown allocator defect.");
        }

        RuntimeIdAllocatorSnapshot changedAllocator = new RuntimeIdAllocatorSnapshot(schemaId, schemaVersion, counters);
        AssertWholeRootRejected(
            changedAllocator,
            recordSequenceSnapshot,
            spatialSnapshot,
            manifestSnapshot,
            randomRootSnapshot);
    }

    [TestCase("unsupported-schema")]
    [TestCase("invalid-next")]
    [TestCase("exhausted-next")]
    public void RejectsMalformedRecordSequenceRootAsPartOfTheWholeComposition(string defect)
    {
        string schemaId = SimulationRecordSequenceSnapshot.CurrentSchemaId;
        int schemaVersion = SimulationRecordSequenceSnapshot.CurrentSchemaVersion;
        long nextSequence = recordSequenceSnapshot.NextSequence;
        switch (defect)
        {
            case "unsupported-schema": schemaVersion++; break;
            case "invalid-next": nextSequence = 0L; break;
            case "exhausted-next": nextSequence = long.MaxValue; break;
            default: throw new ArgumentOutOfRangeException(nameof(defect), defect, "Unknown sequence defect.");
        }

        SimulationRecordSequenceSnapshot changedSequence = new SimulationRecordSequenceSnapshot(
            schemaId, schemaVersion, nextSequence);
        AssertWholeRootRejected(
            allocatorSnapshot,
            changedSequence,
            spatialSnapshot,
            manifestSnapshot,
            randomRootSnapshot);
    }

    [TestCase("multiple-hexes")]
    [TestCase("wrong-anchor")]
    public void RejectsMalformedSpatialCardinalityOrAnchorAsPartOfTheWholeComposition(string defect)
    {
        object hex = ((System.Collections.IList)Read(spatialSnapshot, "Hexes"))[0];
        object location = ((System.Collections.IList)Read(spatialSnapshot, "Locations"))[0];
        object scale = Read(spatialSnapshot, "ScaleContext");
        IEnumerable<object> hexes = new object[] { hex };
        IEnumerable<object> locations = new object[] { location };
        if (defect == "multiple-hexes")
            hexes = new object[] { hex, hex };
        else
            locations = new object[] { CreateLocation((string)Read(location, "Id"), "unanchored-hex") };

        object changedSpatial = BuildSpatialSnapshot(spatialSnapshot, hexes, locations, scale);
        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            changedSpatial,
            manifestSnapshot,
            randomRootSnapshot);
    }

    [TestCase("fingerprint")]
    [TestCase("lineage")]
    public void RejectsP9FingerprintOrLineageCorruptionAsPartOfTheWholeComposition(string defect)
    {
        P12CP9GenesisManifestSnapshot changedManifest;
        if (defect == "fingerprint")
        {
            changedManifest = CopyManifestForTest(
                manifestSnapshot,
                fingerprint: new string('0', 64));
        }
        else
        {
            List<string> changedDependencies = new List<string>(manifestSnapshot.StageDependencyRecords);
            changedDependencies[0] = "resolve-profile -> unrelated-stage";
            changedManifest = CopyManifestForTest(
                manifestSnapshot,
                stageDependencies: changedDependencies,
                replaceStageDependencies: true);
        }

        AssertWholeRootRejected(
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            changedManifest,
            randomRootSnapshot);
    }

    [Test]
    public void P8AndP9SourcesRemainUnchangedAfterAcceptedAndRejectedStaging()
    {
        AssertWholeRootAccepted(allocatorSnapshot, recordSequenceSnapshot, spatialSnapshot, manifestSnapshot,
            randomRootSnapshot);
        object changedSpatial = ChangeSpatialFact(spatialSnapshot, "hex-q");
        AssertWholeRootRejected(allocatorSnapshot, recordSequenceSnapshot, changedSpatial, manifestSnapshot,
            randomRootSnapshot);

        Assert.That(sourceSpatialOwner.Revision, Is.EqualTo(sourceSpatialRevision));
        Assert.That(sourceManifestOwner.Fingerprint, Is.EqualTo(sourceManifestFingerprint));
        Assert.That(manifestSnapshot.Seed, Is.EqualTo(randomRootSnapshot.Seed));
    }

    private static object ChangeSpatialFact(object source, string changedFact)
    {
        object oldHex = ((System.Collections.IList)Read(source, "Hexes"))[0];
        object oldLocation = ((System.Collections.IList)Read(source, "Locations"))[0];
        object oldScale = Read(source, "ScaleContext");
        string hexId = (string)Read(oldHex, "Id");
        int q = (int)Read(oldHex, "Q");
        int r = (int)Read(oldHex, "R");
        string terrainId = (string)Read(oldHex, "TerrainDefinitionId");
        string terrainRevision = (string)Read(oldHex, "AuthoredRevisionToken");
        string locationId = (string)Read(oldLocation, "Id");
        string locationAnchor = (string)Read(oldLocation, "AnchorHexId");
        string resolvedConvention = (string)Read(oldScale, "ResolvedConventionId");
        string scaleSource = (string)Read(oldScale, "SourceIdentity");
        string scaleVersion = (string)Read(oldScale, "SourceVersion");
        decimal scaleDistance = (decimal)Read(oldScale, "DistancePerNeighborStep");
        string scaleUnit = (string)Read(oldScale, "Unit");

        switch (changedFact)
        {
            case "hex-id":
                hexId = "hex/other-origin";
                if (locationAnchor == (string)Read(oldHex, "Id")) locationAnchor = hexId;
                break;
            case "hex-q": q++; break;
            case "hex-r": r--; break;
            case "terrain-id": terrainId = "terrain/other-plains"; break;
            case "terrain-revision": terrainRevision = "other-world-v2"; break;
            case "location-id": locationId = "location/other-origin"; break;
            case "scale-convention": resolvedConvention = "world-scale/other-profile/v1"; break;
            case "scale-source": scaleSource = "profile/other-profile"; break;
            case "scale-version": scaleVersion = "2"; break;
            case "scale-distance": scaleDistance += 1m; break;
            case "scale-unit": scaleUnit = "mi"; break;
            default: throw new ArgumentOutOfRangeException(nameof(changedFact), changedFact, "Unknown P8 fact.");
        }

        return BuildSpatialSnapshot(
            source,
            new object[] { CreateHex(hexId, q, r, terrainId, terrainRevision) },
            new object[] { CreateLocation(locationId, locationAnchor) },
            CreateScale(resolvedConvention, scaleSource, scaleVersion, scaleDistance, scaleUnit));
    }

    private static P12CP9GenesisManifestSnapshot ChangeP9GeographyFact(
        P12CP9GenesisManifestSnapshot source,
        object spatial,
        string changedFact)
    {
        object hex = ((System.Collections.IList)Read(spatial, "Hexes"))[0];
        object location = ((System.Collections.IList)Read(spatial, "Locations"))[0];
        object scale = Read(spatial, "ScaleContext");
        string changedRecord;
        string tag;
        List<string> authoredIds = new List<string>(source.AuthoredDefinitionIds);
        switch (changedFact)
        {
            case "coordinate-convention":
                tag = "authored-hex";
                changedRecord = EncodeTaggedRecord(tag,
                    Read(hex, "Id"), Read(hex, "Q"), Read(hex, "R"), "other-axial-v1",
                    Read(spatial, "CoordinateCanonicalOrder"), Read(hex, "TerrainDefinitionId"),
                    Read(hex, "AuthoredRevisionToken"));
                break;
            case "coordinate-order":
                tag = "authored-hex";
                changedRecord = EncodeTaggedRecord(tag,
                    Read(hex, "Id"), Read(hex, "Q"), Read(hex, "R"),
                    Read(spatial, "CoordinateConventionVersion"), "other-order-v1",
                    Read(hex, "TerrainDefinitionId"), Read(hex, "AuthoredRevisionToken"));
                break;
            case "p9-location-anchor":
                tag = "authored-location";
                changedRecord = EncodeTaggedRecord(tag, Read(location, "Id"), "other-anchor");
                authoredIds.Add("hex/other-anchor");
                break;
            default: throw new ArgumentOutOfRangeException(nameof(changedFact), changedFact, "Unknown P9 fact.");
        }

        authoredIds.Sort(StringComparer.Ordinal);
        return ReplaceP9Record(source, tag, changedRecord, authoredIds);
    }

    private static P12CP9GenesisManifestSnapshot DuplicateP9GeographyRecord(
        P12CP9GenesisManifestSnapshot source,
        object spatial,
        string tag)
    {
        object hex = ((System.Collections.IList)Read(spatial, "Hexes"))[0];
        object location = ((System.Collections.IList)Read(spatial, "Locations"))[0];
        object scale = Read(spatial, "ScaleContext");
        string duplicate;
        List<string> authoredIds = new List<string>(source.AuthoredDefinitionIds);
        switch (tag)
        {
            case "authored-hex":
                duplicate = EncodeTaggedRecord(tag,
                    Read(hex, "Id"), (int)Read(hex, "Q") + 1, Read(hex, "R"),
                    Read(spatial, "CoordinateConventionVersion"), Read(spatial, "CoordinateCanonicalOrder"),
                    Read(hex, "TerrainDefinitionId"), Read(hex, "AuthoredRevisionToken"));
                break;
            case "authored-location":
                duplicate = EncodeTaggedRecord(tag, Read(location, "Id"), "conflicting-anchor");
                authoredIds.Add("hex/conflicting-anchor");
                break;
            case "authored-scale":
                duplicate = EncodeTaggedRecord(tag,
                    Read(scale, "ResolvedConventionId"), Read(scale, "SourceIdentity"), Read(scale, "SourceVersion"),
                    (decimal)Read(scale, "DistancePerNeighborStep") + 1m, Read(scale, "Unit"));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown geography tag.");
        }

        authoredIds.Sort(StringComparer.Ordinal);
        List<string> records = new List<string>(source.CanonicalProvenanceRecords);
        records.Add(duplicate);
        return CopyManifestWithRecords(source, records, authoredIds);
    }

    private static P12CP9GenesisManifestSnapshot ReplaceP9Record(
        P12CP9GenesisManifestSnapshot source,
        string tag,
        string replacement,
        IReadOnlyList<string> authoredIds = null)
    {
        List<string> records = new List<string>(source.CanonicalProvenanceRecords);
        string prefix = tag + "|";
        int index = -1;
        for (int i = 0; i < records.Count; i++)
        {
            if (!records[i].StartsWith(prefix, StringComparison.Ordinal)) continue;
            Assert.That(index, Is.EqualTo(-1), "Expected exactly one source record for " + tag + ".");
            index = i;
        }

        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        records[index] = replacement;
        return CopyManifestWithRecords(source, records, authoredIds ?? source.AuthoredDefinitionIds);
    }

    private static P12CP9GenesisManifestSnapshot CopyManifestWithRecords(
        P12CP9GenesisManifestSnapshot source,
        IReadOnlyList<string> records,
        IReadOnlyList<string> authoredIds)
    {
        string fingerprint = SimulationGenesisPipeline.ComputeFingerprint(records);
        return new P12CP9GenesisManifestSnapshot(
            source.SnapshotContract,
            source.SnapshotSchemaVersion,
            source.P12AdmissionProfile,
            source.ContractIdentity,
            source.SchemaVersion,
            fingerprint,
            fingerprint,
            source.SelectedP9ContractIdentity,
            source.SelectedP9SchemaVersion,
            source.EffectiveConfiguration,
            records,
            authoredIds,
            source.OutputOwners,
            source.StageDependencyRecords,
            source.CalendarMonthsPerYear,
            source.CalendarWeeksPerMonth,
            source.CalendarDaysPerWeek,
            source.MonthLengths,
            source.Seed,
            source.SeedSource,
            source.StageOrder,
            source.FirstSimulatedBoundary);
    }

    private static bool TryCaptureSpatial(
        SimulationRuntimeAdmissionProfile admissionProfile,
        string p9Identity,
        int p9Schema,
        SpatialAuthorityStore source,
        out object snapshot,
        out object failure)
    {
        MethodInfo method = SpatialSnapshotType().GetMethod("TryCapture", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { admissionProfile, p9Identity, p9Schema, source, null, null };
        bool result = (bool)method.Invoke(null, arguments);
        snapshot = arguments[4];
        failure = arguments[5];
        return result;
    }

    private static RuntimeIdAllocatorSnapshot CreateAllocatorSnapshotWithGaps()
    {
        return new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            new[]
            {
                new RuntimeIdAllocatorCounterSnapshot("npc", 5L),
                new RuntimeIdAllocatorCounterSnapshot("city", 3L),
                new RuntimeIdAllocatorCounterSnapshot("location", 8L),
                new RuntimeIdAllocatorCounterSnapshot("route", 2L),
                new RuntimeIdAllocatorCounterSnapshot("event", 7L),
                new RuntimeIdAllocatorCounterSnapshot("directive", 4L),
                new RuntimeIdAllocatorCounterSnapshot("decision", 9L),
                new RuntimeIdAllocatorCounterSnapshot("travel-party", 6L),
                new RuntimeIdAllocatorCounterSnapshot("organization", 11L),
                new RuntimeIdAllocatorCounterSnapshot("site", 10L),
                new RuntimeIdAllocatorCounterSnapshot("expedition", 13L),
                new RuntimeIdAllocatorCounterSnapshot("local-place", 12L),
                new RuntimeIdAllocatorCounterSnapshot("local-connection", 15L),
                new RuntimeIdAllocatorCounterSnapshot("notable-item", 14L)
            });
    }

    private static bool TryStageSpatial(object snapshot, out SpatialAuthorityStore staged, out object failure)
    {
        MethodInfo method = SpatialSnapshotType().GetMethod(
            "TryCreateStagedFromSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { snapshot, null, null };
        bool result = (bool)method.Invoke(null, arguments);
        staged = arguments[1] as SpatialAuthorityStore;
        failure = arguments[2];
        return result;
    }

    private static bool TryStage(
        RuntimeIdAllocatorSnapshot allocator,
        SimulationRecordSequenceSnapshot sequence,
        object spatial,
        P12CP9GenesisManifestSnapshot manifest,
        DeterministicRandomRootSnapshot random,
        out object staged,
        out string diagnostic)
    {
        Type stagerType = typeof(SimulationBootstrapComposition).Assembly.GetType("P12CContinuationRootStager");
        Assert.That(stagerType, Is.Not.Null);
        MethodInfo method = stagerType.GetMethod("TryStage", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { allocator, sequence, spatial, manifest, random, null, null };
        bool result = (bool)method.Invoke(null, arguments);
        staged = arguments[5];
        diagnostic = arguments[6] as string;
        return result;
    }

    private static void AssertWholeRootAccepted(
        RuntimeIdAllocatorSnapshot allocator,
        SimulationRecordSequenceSnapshot sequence,
        object spatial,
        P12CP9GenesisManifestSnapshot manifest,
        DeterministicRandomRootSnapshot random)
    {
        Assert.That(TryStage(allocator, sequence, spatial, manifest, random, out object staged, out string diagnostic),
            Is.True, diagnostic);
        Assert.That(staged, Is.Not.Null);
    }

    private static void AssertWholeRootRejected(
        RuntimeIdAllocatorSnapshot allocator,
        SimulationRecordSequenceSnapshot sequence,
        object spatial,
        P12CP9GenesisManifestSnapshot manifest,
        DeterministicRandomRootSnapshot random)
    {
        Assert.That(TryStage(allocator, sequence, spatial, manifest, random, out object staged, out string diagnostic),
            Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(diagnostic, Is.Not.Empty);
    }

    private static object BuildSpatialSnapshot(
        object source,
        System.Collections.IEnumerable hexes,
        System.Collections.IEnumerable locations,
        object scale,
        SimulationRuntimeAdmissionProfile? admissionProfile = null,
        string p9ContractIdentity = null,
        int? p9SchemaVersion = null)
    {
        Type type = SpatialSnapshotType();
        Type hexType = InnerType("P12CSpatialHexSnapshot");
        Type locationType = InnerType("P12CSpatialLocationSnapshot");
        Type enumerableHex = typeof(IEnumerable<>).MakeGenericType(hexType);
        Type enumerableLocation = typeof(IEnumerable<>).MakeGenericType(locationType);
        Type[] parameterTypes =
        {
            typeof(string), typeof(int), typeof(SimulationRuntimeAdmissionProfile), typeof(string), typeof(int),
            typeof(long), typeof(string), typeof(string), enumerableHex, enumerableLocation, InnerType("P12CSpatialScaleSnapshot")
        };
        ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, parameterTypes, null);
        Assert.That(constructor, Is.Not.Null);
        return constructor.Invoke(new[]
        {
            Read(source, "SchemaId"),
            Read(source, "SchemaVersion"),
            admissionProfile ?? (SimulationRuntimeAdmissionProfile)Read(source, "AdmissionProfile"),
            p9ContractIdentity ?? (string)Read(source, "P9ProfileContractIdentity"),
            p9SchemaVersion ?? (int)Read(source, "P9ProfileSchemaVersion"),
            Read(source, "OwnerRevision"),
            Read(source, "CoordinateConventionVersion"),
            Read(source, "CoordinateCanonicalOrder"),
            MakeArray("P12CSpatialHexSnapshot", hexes),
            MakeArray("P12CSpatialLocationSnapshot", locations),
            scale
        });
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
        return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null);
    }

    private static Array MakeArray(string innerTypeName, System.Collections.IEnumerable items)
    {
        List<object> copy = new List<object>();
        foreach (object item in items) copy.Add(item);
        Array values = Array.CreateInstance(InnerType(innerTypeName), copy.Count);
        for (int i = 0; i < copy.Count; i++) values.SetValue(copy[i], i);
        return values;
    }

    private static P12CP9GenesisManifestSnapshot CopyManifestForTest(
        P12CP9GenesisManifestSnapshot source,
        SimulationRuntimeAdmissionProfile? admissionProfile = null,
        string fingerprint = null,
        string selectedFingerprint = null,
        string selectedP9ContractIdentity = null,
        int? selectedP9SchemaVersion = null,
        IReadOnlyList<string> canonicalRecords = null,
        bool replaceCanonicalRecords = false,
        IReadOnlyList<string> authoredIds = null,
        bool replaceAuthoredIds = false,
        IReadOnlyList<string> outputOwners = null,
        bool replaceOutputOwners = false,
        IReadOnlyList<string> stageDependencies = null,
        bool replaceStageDependencies = false)
    {
        return new P12CP9GenesisManifestSnapshot(
            source.SnapshotContract,
            source.SnapshotSchemaVersion,
            admissionProfile ?? source.P12AdmissionProfile,
            source.ContractIdentity,
            source.SchemaVersion,
            fingerprint ?? source.Fingerprint,
            selectedFingerprint ?? source.SelectedP9ProfileFingerprint,
            selectedP9ContractIdentity ?? source.SelectedP9ContractIdentity,
            selectedP9SchemaVersion ?? source.SelectedP9SchemaVersion,
            source.EffectiveConfiguration,
            replaceCanonicalRecords ? canonicalRecords : source.CanonicalProvenanceRecords,
            replaceAuthoredIds ? authoredIds : source.AuthoredDefinitionIds,
            replaceOutputOwners ? outputOwners : source.OutputOwners,
            replaceStageDependencies ? stageDependencies : source.StageDependencyRecords,
            source.CalendarMonthsPerYear,
            source.CalendarWeeksPerMonth,
            source.CalendarDaysPerWeek,
            source.MonthLengths,
            source.Seed,
            source.SeedSource,
            source.StageOrder,
            source.FirstSimulatedBoundary);
    }

    private static string EncodeTaggedRecord(string tag, params object[] values)
    {
        string record = tag;
        for (int i = 0; i < values.Length; i++)
        {
            string value = Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? string.Empty;
            record += "|" + value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
        }
        return record;
    }

    private static object Read(object instance, string propertyName)
    {
        Assert.That(instance, Is.Not.Null);
        PropertyInfo property = instance.GetType().GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + propertyName + " on " + instance.GetType().FullName);
        return property.GetValue(instance);
    }

    private static Type SpatialSnapshotType()
    {
        return InnerType("P12CSpatialAuthoritySnapshot");
    }

    private static Type InnerType(string name)
    {
        Type type = typeof(SimulationBootstrapComposition).Assembly.GetType(name);
        Assert.That(type, Is.Not.Null, "Missing production type " + name);
        return type;
    }

    private static string FailureMessage(object failure)
    {
        return failure == null ? "Missing spatial snapshot failure." : failure.ToString();
    }
}
