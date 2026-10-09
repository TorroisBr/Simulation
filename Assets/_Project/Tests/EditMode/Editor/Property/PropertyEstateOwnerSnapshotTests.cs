using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PropertyEstateOwnerSnapshotTests
{
    [Test]
    public void EmptyPair_CapturesAndStagesThreeExplicitZeroSections()
    {
        OwnerWorld world = CreateWorld();
        Assert.That(PropertyEstateOwnerSnapshot.TryCapture(
            world.Properties, world.Estates, world.Token, world.Vector,
            out PropertyEstateOwnerSnapshot snapshot, out PropertyEstateOwnerSnapshotFailure failure),
            Is.True, failure?.Message);

        Assert.That(snapshot.OwnershipSchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.TransferHistorySchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.EstateSchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.OwnershipRecordCount, Is.Zero);
        Assert.That(snapshot.TransferHistoryRecordCount, Is.Zero);
        Assert.That(snapshot.EstateRecordCount, Is.Zero);
        Assert.That(snapshot.PropertyRevision, Is.Zero);
        Assert.That(snapshot.TransferHistoryRevision, Is.Zero);
        Assert.That(snapshot.EstateRevision, Is.Zero);
        Assert.That(snapshot.OwnershipRows, Is.Empty);
        Assert.That(snapshot.TransferHistoryRows, Is.Empty);
        Assert.That(snapshot.EstateRows, Is.Empty);

        Assert.That(snapshot.TryStage(world.People, 12L,
            out PropertyOwnershipStore stagedProperties,
            out EstateStore stagedEstates,
            out PropertyEstateOwnerSnapshotFailure stageFailure), Is.True, stageFailure?.Message);
        Assert.That(stagedProperties.Count, Is.Zero);
        Assert.That(stagedProperties.TransferHistory, Is.Empty);
        Assert.That(stagedProperties.Revision, Is.Zero);
        Assert.That(stagedEstates.Count, Is.Zero);
        Assert.That(stagedEstates.Revision, Is.Zero);
        Assert.That(stagedProperties, Is.Not.SameAs(world.Properties));
        Assert.That(stagedEstates, Is.Not.SameAs(world.Estates));
    }

    [Test]
    public void PopulatedPair_PreservesDetachedOwnershipFullHistoryEstateRowsAndExactRevisions()
    {
        OwnerWorld world = CreateWorld(populated: true);
        Assert.That(PropertyEstateOwnerSnapshot.TryCapture(
            world.Properties, world.Estates, world.Token, world.Vector,
            out PropertyEstateOwnerSnapshot snapshot, out PropertyEstateOwnerSnapshotFailure failure),
            Is.True, failure?.Message);

        Assert.That(snapshot.OwnershipRecordCount, Is.EqualTo(2));
        Assert.That(snapshot.TransferHistoryRecordCount, Is.EqualTo(2));
        Assert.That(snapshot.EstateRecordCount, Is.EqualTo(1));
        Assert.That(snapshot.PropertyRevision, Is.EqualTo(4L));
        Assert.That(snapshot.TransferHistoryRevision, Is.EqualTo(4L));
        Assert.That(snapshot.EstateRevision, Is.EqualTo(1L));

        Assert.That(snapshot.TryStage(world.People, 12L,
            out PropertyOwnershipStore stagedProperties,
            out EstateStore stagedEstates,
            out PropertyEstateOwnerSnapshotFailure stageFailure), Is.True, stageFailure?.Message);
        Assert.That(stagedProperties.Revision, Is.EqualTo(world.Properties.Revision));
        Assert.That(stagedProperties.OwnershipRecords, Is.EqualTo(world.Properties.OwnershipRecords));
        Assert.That(stagedProperties.OwnershipRecords[0].PropertyId,
            Is.Not.SameAs(world.Properties.OwnershipRecords[0].PropertyId));
        Assert.That(stagedProperties.OwnershipRecords[0].OwnerPersonId,
            Is.Not.SameAs(world.Properties.OwnershipRecords[0].OwnerPersonId));
        Assert.That(stagedProperties.TransferHistory, Has.Count.EqualTo(world.Properties.TransferHistory.Count));
        AssertHistoryEqual(world.Properties.TransferHistory, stagedProperties.TransferHistory);
        Assert.That(stagedEstates.Revision, Is.EqualTo(world.Estates.Revision));
        Assert.That(stagedEstates.Estates, Is.EqualTo(world.Estates.Estates));
        Assert.That(stagedEstates.TryGetByDeceasedPerson(world.Deceased.PersonId, out EstateRecord indexed), Is.True);
        Assert.That(indexed.EstateId, Is.EqualTo(new EstateId("estate-deceased")));

        Assert.That(stagedProperties.PersonStoreForWorldBoundary, Is.SameAs(world.People));
        Assert.That(stagedEstates.PersonStoreForWorldBoundary, Is.SameAs(world.People));
        Assert.That(world.Properties.Revision, Is.EqualTo(4L));
        Assert.That(world.Estates.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void CaptureRejectsMismatchedOrMissingRequiredOwnerVectorSection()
    {
        OwnerWorld world = CreateWorld();
        List<OwnerSectionCensusSnapshot> missing = new List<OwnerSectionCensusSnapshot>(world.Vector);
        missing.RemoveAt(2);
        DailyCaptureEligibilityToken tokenWithMissing = CreateToken(missing, 12L);
        Assert.That(PropertyEstateOwnerSnapshot.TryCapture(
            world.Properties, world.Estates, tokenWithMissing, missing,
            out PropertyEstateOwnerSnapshot snapshot, out PropertyEstateOwnerSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidOwnerSectionVector));

        OwnerWorld other = CreateWorld();
        Assert.That(PropertyEstateOwnerSnapshot.TryCapture(
            world.Properties, world.Estates, other.Token, other.Vector,
            out snapshot, out failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidOwnerSectionVector));
    }

    [Test]
    public void StageRejectsDuplicateDeceasedPersonBeforeReturningEitherOwner()
    {
        OwnerWorld world = CreateWorld(populated: true);
        PropertyEstateOwnerSnapshot duplicateDeceased = new PropertyEstateOwnerSnapshot(
            1, 2, 3L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-a", "property-owner"),
                new PropertyEstateOwnershipRow("property-z", "deceased")
            },
            1, 1, 3L,
            new[] { new PropertyEstateTransferHistoryRow("property-a", "property-owner", "successor", 8L) },
            1, 2, 2L,
            new[]
            {
                new PropertyEstateEstateRow("estate-a", "deceased", 10L),
                new PropertyEstateEstateRow("estate-b", "deceased", 11L)
            });

        Assert.That(duplicateDeceased.TryStage(world.People, 12L,
            out PropertyOwnershipStore stagedProperties,
            out EstateStore stagedEstates,
            out PropertyEstateOwnerSnapshotFailure failure), Is.False);
        Assert.That(stagedProperties, Is.Null);
        Assert.That(stagedEstates, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.DuplicateIdentity));
        Assert.That(world.Properties.Count, Is.EqualTo(2));
        Assert.That(world.Estates.Count, Is.EqualTo(1));
    }

    [Test]
    public void StageRejectsMissingTypedReferencesAndFutureHistoryWithoutPartialPair()
    {
        OwnerWorld world = CreateWorld(populated: true);
        PropertyEstateOwnerSnapshot badHistory = new PropertyEstateOwnerSnapshot(
            1, 2, 3L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-a", "property-owner"),
                new PropertyEstateOwnershipRow("property-z", "deceased")
            },
            1, 1, 3L,
            new[] { new PropertyEstateTransferHistoryRow("missing-property", "property-owner", "successor", 8L) },
            1, 1, 1L,
            new[] { new PropertyEstateEstateRow("estate-deceased", "deceased", 10L) });

        Assert.That(badHistory.TryStage(world.People, 12L,
            out PropertyOwnershipStore stagedProperties,
            out EstateStore stagedEstates,
            out PropertyEstateOwnerSnapshotFailure failure), Is.False);
        Assert.That(stagedProperties, Is.Null);
        Assert.That(stagedEstates, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidReference));

        PropertyEstateOwnerSnapshot futureHistory = new PropertyEstateOwnerSnapshot(
            1, 2, 3L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-a", "property-owner"),
                new PropertyEstateOwnershipRow("property-z", "deceased")
            },
            1, 1, 3L,
            new[] { new PropertyEstateTransferHistoryRow("property-a", "property-owner", "successor", 13L) },
            1, 1, 1L,
            new[] { new PropertyEstateEstateRow("estate-deceased", "deceased", 10L) });
        Assert.That(futureHistory.TryStage(world.People, 12L,
            out stagedProperties, out stagedEstates, out failure), Is.False);
        Assert.That(stagedProperties, Is.Null);
        Assert.That(stagedEstates, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidDay));
    }

    [Test]
    public void StageRejectsRevisionCardinalityAndUnorderedHistory()
    {
        OwnerWorld world = CreateWorld(populated: true);
        PropertyEstateOwnerSnapshot badRevision = new PropertyEstateOwnerSnapshot(
            1, 2, 4L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-a", "property-owner"),
                new PropertyEstateOwnershipRow("property-z", "deceased")
            },
            1, 1, 4L,
            new[] { new PropertyEstateTransferHistoryRow("property-a", "property-owner", "successor", 8L) },
            1, 1, 1L,
            new[] { new PropertyEstateEstateRow("estate-deceased", "deceased", 10L) });
        Assert.That(badRevision.TryStage(world.People, 12L, out _, out _, out PropertyEstateOwnerSnapshotFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidCardinality));

        PropertyEstateOwnerSnapshot unordered = new PropertyEstateOwnerSnapshot(
            1, 2, 4L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-a", "property-owner"),
                new PropertyEstateOwnershipRow("property-z", "deceased")
            },
            1, 2, 4L,
            new[]
            {
                new PropertyEstateTransferHistoryRow("property-z", "deceased", "successor", 9L),
                new PropertyEstateTransferHistoryRow("property-a", "property-owner", "successor", 8L)
            },
            1, 1, 1L,
            new[] { new PropertyEstateEstateRow("estate-deceased", "deceased", 10L) });
        Assert.That(unordered.TryStage(world.People, 12L, out _, out _, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidOrdering));
    }

    private static OwnerWorld CreateWorld(bool populated = false)
    {
        PersonStore people = new PersonStore();
        PersonRuntime propertyOwner = Register(people, "property-owner");
        PersonRuntime successor = Register(people, "successor");
        PersonRuntime deceased = Register(people, "deceased", 10L);
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        EstateStore estates = new EstateStore(people);
        if (populated)
        {
            Assert.That(properties.TryRegister(
                new PropertyOwnershipRecord(new PropertyId("property-z"), deceased.PersonId), out _), Is.True);
            Assert.That(properties.TryRegister(
                new PropertyOwnershipRecord(new PropertyId("property-a"), propertyOwner.PersonId), out _), Is.True);
            Assert.That(PropertyTransferSystem.TryTransfer(
                people, properties, new PropertyId("property-a"), successor.PersonId, 8L,
                out _, out PropertyTransferFailure transferFailure), Is.True, transferFailure.ToString());
            Assert.That(PropertyTransferSystem.TryTransfer(
                people, properties, new PropertyId("property-z"), successor.PersonId, 9L,
                out _, out transferFailure), Is.True, transferFailure.ToString());
            Assert.That(EstateOpeningSystem.TryOpenEstate(
                people, estates, new EstateId("estate-deceased"), deceased.PersonId, 10L,
                out _, out EstateFoundationFailure estateFailure), Is.True, estateFailure.ToString());
        }

        IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders =
            PropertyOwnershipCensusProvider.CreateProviders(properties);
        List<OwnerSectionCensusSnapshot> vector = new List<OwnerSectionCensusSnapshot>();
        AddWitness(vector, propertyProviders[0].GetCurrentCensus());
        AddWitness(vector, propertyProviders[1].GetCurrentCensus());
        AddWitness(vector, new EstateCensusProvider(estates).GetCurrentCensus());
        return new OwnerWorld(people, properties, estates, deceased, vector, CreateToken(vector, 12L));
    }

    private static PersonRuntime Register(PersonStore people, string id, long? deathDay = null)
    {
        PersonRuntime person = deathDay.HasValue
            ? new PersonRuntime(new PersonId(id), 0L, deathDay.Value)
            : new PersonRuntime(new PersonId(id), 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static void AddWitness(List<OwnerSectionCensusSnapshot> vector, OwnerSectionCensusWitness witness)
    {
        vector.Add(new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity,
            witness.Cardinality,
            witness.Revision));
    }

    private static DailyCaptureEligibilityToken CreateToken(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        long absoluteDay)
    {
        return new DailyCaptureEligibilityToken(
            new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()),
            absoluteDay,
            1L,
            0L,
            vector);
    }

    private static void AssertHistoryEqual(
        IReadOnlyList<PropertyOwnershipTransferHistoryRecord> expected,
        IReadOnlyList<PropertyOwnershipTransferHistoryRecord> actual)
    {
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.That(actual[i].PropertyId, Is.EqualTo(expected[i].PropertyId));
            Assert.That(actual[i].PreviousOwnerPersonId, Is.EqualTo(expected[i].PreviousOwnerPersonId));
            Assert.That(actual[i].NewOwnerPersonId, Is.EqualTo(expected[i].NewOwnerPersonId));
            Assert.That(actual[i].TransferAbsoluteDay, Is.EqualTo(expected[i].TransferAbsoluteDay));
        }
    }

    private sealed class OwnerWorld
    {
        public PersonStore People { get; }
        public PropertyOwnershipStore Properties { get; }
        public EstateStore Estates { get; }
        public PersonRuntime Deceased { get; }
        public IReadOnlyList<OwnerSectionCensusSnapshot> Vector { get; }
        public DailyCaptureEligibilityToken Token { get; }
        public OwnerWorld(PersonStore people, PropertyOwnershipStore properties, EstateStore estates,
            PersonRuntime deceased, IReadOnlyList<OwnerSectionCensusSnapshot> vector,
            DailyCaptureEligibilityToken token)
        { People = people; Properties = properties; Estates = estates; Deceased = deceased; Vector = vector; Token = token; }
    }
}
