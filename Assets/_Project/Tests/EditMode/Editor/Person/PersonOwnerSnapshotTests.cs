using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PersonOwnerSnapshotTests
{
    [Test]
    public void EmptyOwnerRoundTripsAndRemainsDistinctFromMissingSnapshot()
    {
        PersonStore source = new PersonStore();
        PersonStoreOwnerSnapshot snapshot = source.CaptureOwnerSnapshot();

        Assert.That(snapshot.SchemaVersion, Is.EqualTo(PersonStoreOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(snapshot.Revision, Is.Zero);
        Assert.That(snapshot.MembershipCount, Is.Zero);
        Assert.That(snapshot.BindingCount, Is.Zero);
        Assert.That(snapshot.Rows, Is.Empty);
        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            snapshot,
            out PersonStore staged,
            out PersonStoreOwnerSnapshotFailureCode failure), Is.True);
        Assert.That(failure, Is.EqualTo(PersonStoreOwnerSnapshotFailureCode.None));
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.Persons, Is.Empty);
        Assert.That(staged.Revision, Is.Zero);

        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            null,
            out staged,
            out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(PersonStoreOwnerSnapshotFailureCode.MissingSnapshot));
    }

    [Test]
    public void PopulatedOwnerPreservesExactValuesOrderAndDetachedIndexes()
    {
        PersonStore source = new PersonStore();
        PersonRuntime deceased = new PersonRuntime(new PersonId("person-z"), 2L);
        Assert.That(deceased.TrySetResidenceSettlementRuntimeId(" settlement-z "), Is.True);
        Register(source, deceased);
        Assert.That(deceased.TryRecordDeath(9L), Is.True);

        PersonRuntime unknownBirth = new PersonRuntime(new PersonId("Person-A"));
        Register(source, unknownBirth);
        Assert.That(source.TryBindMaterializedNpc(
            unknownBirth.PersonId,
            " npc-runtime-A ",
            out PersonStoreFailure bindFailure), Is.True, bindFailure.ToString());

        PersonRuntime living = new PersonRuntime(new PersonId("person-a"), 0L);
        Register(source, living);

        PersonStoreOwnerSnapshot snapshot = source.CaptureOwnerSnapshot();
        Assert.That(snapshot.MembershipCount, Is.EqualTo(3));
        Assert.That(snapshot.BindingCount, Is.EqualTo(1));
        Assert.That(snapshot.Revision, Is.EqualTo(4L));
        Assert.That(snapshot.Rows, Has.Count.EqualTo(3));
        AssertRow(snapshot.Rows[0], "person-z", 2L, 9L, " settlement-z ", null, 2L);
        AssertRow(snapshot.Rows[1], "Person-A", null, null, null, " npc-runtime-A ", 0L);
        AssertRow(snapshot.Rows[2], "person-a", 0L, null, null, null, 0L);

        IList<PersonStoreOwnerSnapshotRow> readOnlyRows = snapshot.Rows as IList<PersonStoreOwnerSnapshotRow>;
        Assert.That(readOnlyRows, Is.Not.Null);
        Assert.That(readOnlyRows.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => readOnlyRows[0] = null);

        List<PersonStoreOwnerSnapshotRow> suppliedRows = new List<PersonStoreOwnerSnapshotRow>
        {
            snapshot.Rows[0]
        };
        PersonStoreOwnerSnapshot copiedInput = new PersonStoreOwnerSnapshot(
            PersonStoreOwnerSnapshot.CurrentSchemaVersion,
            1L,
            1,
            0,
            suppliedRows);
        suppliedRows.Clear();
        Assert.That(copiedInput.Rows, Has.Count.EqualTo(1));
        AssertRow(copiedInput.Rows[0], "person-z", 2L, 9L, " settlement-z ", null, 2L);

        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            snapshot,
            out PersonStore staged,
            out PersonStoreOwnerSnapshotFailureCode failure), Is.True);
        Assert.That(failure, Is.EqualTo(PersonStoreOwnerSnapshotFailureCode.None));
        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.Revision, Is.EqualTo(source.Revision));
        Assert.That(staged.Persons, Has.Count.EqualTo(3));
        Assert.That(staged.Persons[0].PersonId.Value, Is.EqualTo("person-z"));
        Assert.That(staged.Persons[1].PersonId.Value, Is.EqualTo("Person-A"));
        Assert.That(staged.Persons[2].PersonId.Value, Is.EqualTo("person-a"));
        Assert.That(staged.TryGetByMaterializedNpcRuntimeId(
            " npc-runtime-A ", out PersonRuntime rebound), Is.True);
        Assert.That(rebound.PersonId.Value, Is.EqualTo("Person-A"));
        Assert.That(staged.TryGet(new PersonId("person-z"), out PersonRuntime stagedDeceased), Is.True);
        Assert.That(stagedDeceased.DeathAbsoluteDay, Is.EqualTo(9L));

        // Later writes to the source cannot affect either the detached value
        // or the already staged owner.
        Assert.That(living.TrySetResidenceSettlementRuntimeId("settlement-later"), Is.True);
        Assert.That(source.TryBindMaterializedNpc(
            living.PersonId,
            "npc-runtime-later",
            out bindFailure), Is.True, bindFailure.ToString());
        AssertRow(snapshot.Rows[2], "person-a", 0L, null, null, null, 0L);
        AssertRow(staged.CaptureOwnerSnapshot().Rows[2], "person-a", 0L, null, null, null, 0L);
        Assert.That(source.CaptureOwnerSnapshot().Revision, Is.EqualTo(5L));
        Assert.That(staged.Revision, Is.EqualTo(4L));
    }

    [Test]
    public void CaptureTracksRegistrationBindingCompensationAndPersonLocalCommitsExactly()
    {
        PersonStore source = new PersonStore();
        PersonRuntime initialResidence = new PersonRuntime(new PersonId("person-initial-residence"));
        Assert.That(initialResidence.TrySetResidenceSettlementRuntimeId("settlement-before-registration"), Is.True);
        Assert.That(initialResidence.LifeResidenceRevision, Is.EqualTo(1L));
        Register(source, initialResidence);
        AssertRow(source.CaptureOwnerSnapshot().Rows[0],
            "person-initial-residence", null, null, "settlement-before-registration", null, 1L);

        PersonRuntime compensated = new PersonRuntime(new PersonId("person-compensated"));
        Register(source, compensated);
        Assert.That(source.TryBindMaterializedNpc(
            compensated.PersonId,
            "npc-compensated",
            out PersonStoreFailure bindFailure), Is.True, bindFailure.ToString());
        Assert.That(source.Revision, Is.EqualTo(3L));
        Assert.That(source.MaterializedBindingCount, Is.EqualTo(1));
        Assert.That(source.TryRollbackMaterializedNpcBinding(
            compensated.PersonId,
            "npc-compensated"), Is.True);
        Assert.That(source.Revision, Is.EqualTo(4L));
        Assert.That(source.TryRollbackRegistration(compensated), Is.True);
        Assert.That(source.Revision, Is.EqualTo(5L));
        Assert.That(source.Persons, Has.Count.EqualTo(1));
        Assert.That(source.MaterializedBindingCount, Is.Zero);

        PersonRuntime residentDeceased = new PersonRuntime(new PersonId("person-resident-deceased"), 1L);
        Register(source, residentDeceased);
        Assert.That(residentDeceased.TrySetResidenceSettlementRuntimeId("settlement-resident"), Is.True);
        Assert.That(residentDeceased.LifeResidenceRevision, Is.EqualTo(1L));
        Assert.That(residentDeceased.TryRecordDeath(5L), Is.True);
        Assert.That(residentDeceased.LifeResidenceRevision, Is.EqualTo(2L));

        PersonStoreOwnerSnapshot snapshot = source.CaptureOwnerSnapshot();
        Assert.That(snapshot.MembershipCount, Is.EqualTo(2));
        Assert.That(snapshot.BindingCount, Is.Zero);
        Assert.That(snapshot.Revision, Is.EqualTo(6L));
        AssertRow(snapshot.Rows[0],
            "person-initial-residence", null, null, "settlement-before-registration", null, 1L);
        AssertRow(snapshot.Rows[1],
            "person-resident-deceased", 1L, 5L, "settlement-resident", null, 2L);
    }

    [Test]
    public void StagingRestoresGappedAndSaturatedRevisionsWithoutReplay()
    {
        PersonStoreOwnerSnapshot gapped = Snapshot(
            1,
            17L,
            2,
            1,
            Row("person-gap-z", 0L, null, null, "npc-gap", 11L),
            Row("person-gap-a", null, null, "settlement-gap", null, 4L));

        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            gapped,
            out PersonStore staged,
            out PersonStoreOwnerSnapshotFailureCode failure), Is.True);
        Assert.That(failure, Is.EqualTo(PersonStoreOwnerSnapshotFailureCode.None));
        Assert.That(staged.Revision, Is.EqualTo(17L));
        Assert.That(staged.Persons[0].LifeResidenceRevision, Is.EqualTo(11L));
        Assert.That(staged.Persons[1].LifeResidenceRevision, Is.EqualTo(4L));
        Assert.That(staged.TryRegister(
            new PersonRuntime(new PersonId("person-after-stage")),
            out PersonStoreFailure registerFailure), Is.True, registerFailure.ToString());
        Assert.That(staged.Revision, Is.EqualTo(18L));

        PersonStoreOwnerSnapshot saturatedStoreSnapshot = Snapshot(
            1,
            long.MaxValue,
            1,
            0,
            Row("person-saturated-store", null, null, null, null, 0L));
        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            saturatedStoreSnapshot,
            out PersonStore saturatedStore,
            out failure), Is.True);
        Assert.That(saturatedStore.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(saturatedStore.TryRegister(
            new PersonRuntime(new PersonId("person-overflow")),
            out registerFailure), Is.False);
        Assert.That(registerFailure, Is.EqualTo(PersonStoreFailure.RevisionOverflow));
        Assert.That(saturatedStore.Persons, Has.Count.EqualTo(1));
        Assert.That(saturatedStore.Revision, Is.EqualTo(long.MaxValue));

        PersonStoreOwnerSnapshot saturatedPersonSnapshot = Snapshot(
            1,
            0L,
            1,
            0,
            Row("person-saturated-life", null, null, null, null, long.MaxValue));
        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            saturatedPersonSnapshot,
            out PersonStore saturatedPersonStore,
            out failure), Is.True);
        PersonRuntime saturatedPerson = saturatedPersonStore.Persons[0];
        saturatedPerson.BindP12LifecycleMutationBoundary(_ => true, _ => { });
        Assert.That(saturatedPerson.TryRecordDeath(10L), Is.False);
        Assert.That(saturatedPerson.DeathAbsoluteDay, Is.Null);
        Assert.That(saturatedPerson.LifeResidenceRevision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void RejectsMalformedLocalValuesWithoutPublishingOrChangingSource()
    {
        PersonStore source = new PersonStore();
        Register(source, new PersonRuntime(new PersonId("stable-source"), 0L));
        PersonStoreOwnerSnapshot sourceBefore = source.CaptureOwnerSnapshot();

        AssertRejected(source, sourceBefore, Snapshot(2, 0L, 0, 0, new PersonStoreOwnerSnapshotRow[0]),
            PersonStoreOwnerSnapshotFailureCode.UnsupportedSchema);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 0, 0, null),
            PersonStoreOwnerSnapshotFailureCode.InvalidHeader);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, (PersonStoreOwnerSnapshotRow)null),
            PersonStoreOwnerSnapshotFailureCode.NullPersonRow);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row(null, null, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidPersonId);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row("  ", null, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidPersonId);
        AssertRejected(source, sourceBefore, Snapshot(1, 1L, 2, 0,
            Row("duplicate", null, null, null, null, 0L),
            Row("duplicate", null, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.DuplicatePersonId);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row("negative-birth", -1L, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidDates);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row("negative-death", null, -1L, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidDates);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row("death-before-birth", 5L, 4L, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidDates);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row("negative-local-revision", null, null, null, null, -1L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidPersonRevision);
        AssertRejected(source, sourceBefore, Snapshot(1, 0L, 1, 0, Row("blank-residence", null, null, " ", null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidResidence);
        AssertRejected(source, sourceBefore, Snapshot(1, 1L, 1, 1, Row("blank-link", null, null, null, "\t", 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidMaterializedNpcId);
        AssertRejected(source, sourceBefore, Snapshot(1, 3L, 2, 2,
            Row("link-one", null, null, null, "duplicate-npc", 0L),
            Row("link-two", null, null, null, "duplicate-npc", 0L)),
            PersonStoreOwnerSnapshotFailureCode.DuplicateMaterializedNpcId);
        AssertRejected(source, sourceBefore, Snapshot(1, 1L, 2, 0,
            Row("membership-mismatch", null, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidCardinality);
        AssertRejected(source, sourceBefore, Snapshot(1, 1L, 1, 1,
            Row("binding-mismatch", null, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.InvalidCardinality);
        AssertRejected(source, sourceBefore, Snapshot(1, -1L, 0, 0, new PersonStoreOwnerSnapshotRow[0]),
            PersonStoreOwnerSnapshotFailureCode.InvalidHeader);
        AssertRejected(source, sourceBefore, Snapshot(1, 1L, 2, 1,
            Row("impossible-revision-one", null, null, null, "npc-revision", 0L),
            Row("impossible-revision-two", null, null, null, null, 0L)),
            PersonStoreOwnerSnapshotFailureCode.ImpossibleRevision);
    }

    private static void Register(PersonStore store, PersonRuntime person)
    {
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
    }

    private static PersonStoreOwnerSnapshot Snapshot(
        int schemaVersion,
        long revision,
        int membershipCount,
        int bindingCount,
        params PersonStoreOwnerSnapshotRow[] rows)
    {
        return new PersonStoreOwnerSnapshot(
            schemaVersion,
            revision,
            membershipCount,
            bindingCount,
            rows);
    }

    private static PersonStoreOwnerSnapshotRow Row(
        string personId,
        long? birth,
        long? death,
        string residence,
        string npcId,
        long lifeResidenceRevision)
    {
        return new PersonStoreOwnerSnapshotRow(
            personId,
            birth,
            death,
            residence,
            npcId,
            lifeResidenceRevision);
    }

    private static void AssertRow(
        PersonStoreOwnerSnapshotRow row,
        string personId,
        long? birth,
        long? death,
        string residence,
        string npcId,
        long lifeResidenceRevision)
    {
        Assert.That(row, Is.Not.Null);
        Assert.That(row.PersonIdValue, Is.EqualTo(personId));
        Assert.That(row.BirthAbsoluteDay, Is.EqualTo(birth));
        Assert.That(row.DeathAbsoluteDay, Is.EqualTo(death));
        Assert.That(row.ResidenceSettlementRuntimeId, Is.EqualTo(residence));
        Assert.That(row.MaterializedNpcRuntimeId, Is.EqualTo(npcId));
        Assert.That(row.LifeResidenceRevision, Is.EqualTo(lifeResidenceRevision));
    }

    private static void AssertRejected(
        PersonStore source,
        PersonStoreOwnerSnapshot sourceBefore,
        PersonStoreOwnerSnapshot invalidSnapshot,
        PersonStoreOwnerSnapshotFailureCode expectedFailure)
    {
        Assert.That(PersonStore.TryCreateFromOwnerSnapshot(
            invalidSnapshot,
            out PersonStore staged,
            out PersonStoreOwnerSnapshotFailureCode failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(expectedFailure));
        AssertSnapshotsEqual(sourceBefore, source.CaptureOwnerSnapshot());
    }

    private static void AssertSnapshotsEqual(
        PersonStoreOwnerSnapshot expected,
        PersonStoreOwnerSnapshot actual)
    {
        Assert.That(actual.SchemaVersion, Is.EqualTo(expected.SchemaVersion));
        Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
        Assert.That(actual.MembershipCount, Is.EqualTo(expected.MembershipCount));
        Assert.That(actual.BindingCount, Is.EqualTo(expected.BindingCount));
        Assert.That(actual.Rows, Has.Count.EqualTo(expected.Rows.Count));
        for (int i = 0; i < expected.Rows.Count; i++)
        {
            PersonStoreOwnerSnapshotRow left = expected.Rows[i];
            PersonStoreOwnerSnapshotRow right = actual.Rows[i];
            Assert.That(right.PersonIdValue, Is.EqualTo(left.PersonIdValue));
            Assert.That(right.BirthAbsoluteDay, Is.EqualTo(left.BirthAbsoluteDay));
            Assert.That(right.DeathAbsoluteDay, Is.EqualTo(left.DeathAbsoluteDay));
            Assert.That(right.ResidenceSettlementRuntimeId, Is.EqualTo(left.ResidenceSettlementRuntimeId));
            Assert.That(right.MaterializedNpcRuntimeId, Is.EqualTo(left.MaterializedNpcRuntimeId));
            Assert.That(right.LifeResidenceRevision, Is.EqualTo(left.LifeResidenceRevision));
        }
    }
}
