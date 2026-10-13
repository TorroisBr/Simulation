using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12APersonStoreOwnerCodecTests
{
    [Test]
    public void FixedPersonSectionsRoundTripExactValuesRevisionBindingsAndInsertionOrder()
    {
        PersonStoreOwnerSnapshot source = Snapshot(
            4L,
            Row("person/z", 2L, 9L, " settlement-z ", null, 2L),
            Row("Person-A", null, null, null, " npc-runtime-A ", 0L),
            Row("person-a", 0L, null, null, null, 0L));

        Assert.That(P12APersonStoreOwnerCodec.TryEncode(source,
            out P12AOwnerSection membership, out P12AOwnerSection bindings,
            out P12AResult encodeFailure), Is.True, encodeFailure?.Code.ToString());
        AssertRequiredSection(membership, PersonMembershipCensusProvider.SectionId, 3, 4L);
        AssertRequiredSection(bindings, PersonMaterializationBindingCensusProvider.SectionId, 1, 4L);
        Assert.That(membership.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(membership.Id)));
        Assert.That(bindings.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(bindings.Id)));
        AssertCanonicalRows(membership);
        AssertCanonicalRows(bindings);

        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, bindings,
            out PersonStoreOwnerSnapshot restored, out P12AResult decodeFailure),
            Is.True, decodeFailure?.Code.ToString());
        AssertSnapshotEqual(source, restored);
        Assert.That(restored.Rows[0].PersonIdValue, Is.EqualTo("person/z"));
        Assert.That(restored.Rows[1].PersonIdValue, Is.EqualTo("Person-A"));
        Assert.That(restored.Rows[1].BirthAbsoluteDay, Is.Null);
        Assert.That(restored.Rows[1].DeathAbsoluteDay, Is.Null);
        Assert.That(restored.Rows[1].ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(restored.Rows[1].MaterializedNpcRuntimeId, Is.EqualTo(" npc-runtime-A "));
        Assert.That(restored.Rows[2].BirthAbsoluteDay, Is.EqualTo(0L));
        Assert.That(restored.Rows[2].MaterializedNpcRuntimeId, Is.Null);
    }

    [Test]
    public void EmptyOwnerPreservesNonzeroPostCompensationRevision()
    {
        PersonStoreOwnerSnapshot source = Snapshot(3L);
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(source,
            out P12AOwnerSection membership, out P12AOwnerSection bindings, out _), Is.True);
        AssertRequiredSection(membership, PersonMembershipCensusProvider.SectionId, 0, 3L);
        AssertRequiredSection(bindings, PersonMaterializationBindingCensusProvider.SectionId, 0, 3L);
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, bindings,
            out PersonStoreOwnerSnapshot restored, out _), Is.True);
        AssertSnapshotEqual(source, restored);
    }

    [Test]
    public void RejectsMissingBindingRowsAndMismatchedPairedSectionContracts()
    {
        PersonStoreOwnerSnapshot source = Snapshot(
            3L,
            Row("person-one", null, null, null, "npc-one", 0L),
            Row("person-two", null, null, null, null, 0L));
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(source,
            out P12AOwnerSection membership, out P12AOwnerSection bindings, out _), Is.True);

        P12AOwnerSection missingBinding = Rebuild(bindings, Array.Empty<P12AOwnerRow>());
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, missingBinding,
            out PersonStoreOwnerSnapshot restored, out _), Is.False);
        Assert.That(restored, Is.Null);

        P12AOwnerSection wrongRevision = new P12AOwnerSection(bindings.Id, bindings.Major, bindings.Minor,
            bindings.Role, bindings.AliasGroup, bindings.Revision + 1, bindings.Rows);
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, wrongRevision,
            out restored, out _), Is.False);

        P12AOwnerSection wrongAlias = new P12AOwnerSection(bindings.Id, bindings.Major, bindings.Minor,
            bindings.Role, "unrelated-alias", bindings.Revision, bindings.Rows);
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, wrongAlias,
            out restored, out _), Is.False);
    }

    [Test]
    public void RejectsDuplicateOrDanglingBindingRowsAndNoncanonicalOrdering()
    {
        PersonStoreOwnerSnapshot source = Snapshot(
            4L,
            Row("person-one", null, null, null, "npc-one", 0L),
            Row("person-two", null, null, null, "npc-two", 0L));
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(source,
            out P12AOwnerSection membership, out P12AOwnerSection bindings, out _), Is.True);

        P12AOwnerRow first = bindings.Rows[0];
        P12AOwnerSection duplicate = Rebuild(bindings, new[] { first, first });
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, duplicate,
            out PersonStoreOwnerSnapshot restored, out _), Is.False);

        P12AOwnerSection reversed = Rebuild(membership, membership.Rows.Reverse());
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(reversed, bindings,
            out restored, out _), Is.False);

        P12AOwnerRow[] danglingRows = bindings.Rows.Select(row =>
            new P12AOwnerRow("person/11:not-present", row.Revision, row.Payload)).ToArray();
        P12AOwnerSection dangling = Rebuild(bindings, danglingRows);
        Assert.That(P12APersonStoreOwnerCodec.TryDecode(membership, dangling,
            out restored, out _), Is.False);
    }

    [Test]
    public void RejectsInvalidSnapshotValuesAndOversizedStableOwnerKeys()
    {
        PersonStoreOwnerSnapshot invalidEmptyBinding = Snapshot(
            1L, Row("person-one", null, null, null, "", 0L));
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(invalidEmptyBinding,
            out P12AOwnerSection membership, out P12AOwnerSection bindings, out _), Is.False);
        Assert.That(membership, Is.Null);
        Assert.That(bindings, Is.Null);

        PersonStoreOwnerSnapshot reversedDates = Snapshot(
            1L, Row("person-one", 5L, 4L, null, null, 0L));
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(reversedDates,
            out membership, out bindings, out _), Is.False);

        PersonStoreOwnerSnapshot oversizedId = Snapshot(
            1L, Row(new string('x', P12AContinuationCodec.MaxStringBytes), null, null, null, null, 0L));
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(oversizedId,
            out membership, out bindings, out P12AResult limitFailure), Is.False);
        Assert.That(limitFailure.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    private static void AssertRequiredSection(P12AOwnerSection section,
        string id, int cardinality, long revision)
    {
        Assert.That(section.Id, Is.EqualTo(id));
        Assert.That(section.Major, Is.EqualTo(1));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Has.Count.EqualTo(cardinality));
        Assert.That(section.Revision, Is.EqualTo(revision));
        Assert.That(section.Rows.All(row => row.Revision == 0L), Is.True);
    }

    private static void AssertCanonicalRows(P12AOwnerSection section)
    {
        string[] keys = section.Rows.Select(row => row.OwnerKey).ToArray();
        Assert.That(keys, Is.EqualTo(keys.OrderBy(key => key, StringComparer.Ordinal).ToArray()));
        Assert.That(keys.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(keys.Length));
    }

    private static void AssertSnapshotEqual(PersonStoreOwnerSnapshot expected,
        PersonStoreOwnerSnapshot actual)
    {
        Assert.That(actual.SchemaId, Is.EqualTo(expected.SchemaId));
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

    private static PersonStoreOwnerSnapshot Snapshot(long revision,
        params PersonStoreOwnerSnapshotRow[] rows)
    {
        int bindings = rows.Count(row => row?.MaterializedNpcRuntimeId != null);
        return new PersonStoreOwnerSnapshot(PersonStoreOwnerSnapshot.CurrentSchemaId,
            PersonStoreOwnerSnapshot.CurrentSchemaVersion, revision, rows.Length, bindings, rows);
    }

    private static PersonStoreOwnerSnapshotRow Row(string personId, long? birth, long? death,
        string residence, string materializedNpcId, long lifeResidenceRevision)
        => new PersonStoreOwnerSnapshotRow(personId, birth, death, residence,
            materializedNpcId, lifeResidenceRevision);

    private static P12AOwnerSection Rebuild(P12AOwnerSection source, IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, source.Revision, rows);
}
