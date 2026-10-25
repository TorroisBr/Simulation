using System;
using System.Linq;
using NUnit.Framework;

public sealed class P12AGenealogyOwnerCodecTests
{
    [Test]
    public void EncodeDecodePreservesParentageOrderAndExactRevision()
    {
        GenealogyOwnerSnapshot source = new GenealogyOwnerSnapshot(
            GenealogyOwnerSnapshot.CurrentSchemaVersion,
            7L,
            new[]
            {
                new ParentageRecord(new PersonId("b"), new PersonId("d")),
                new ParentageRecord(new PersonId("a"), new PersonId("c"))
            });

        Assert.That(P12AGenealogyOwnerCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True, encodeError?.SectionCode);
        Assert.That(encoded.Id, Is.EqualTo(GenealogyCensusProvider.SectionId));
        Assert.That(encoded.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(encoded.Revision, Is.EqualTo(7L));
        Assert.That(encoded.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "1:a1:c", "1:b1:d" }));
        Assert.That(encoded.Rows.All(row => row.Revision == 7L), Is.True);

        Assert.That(P12AGenealogyOwnerCodec.TryDecode(encoded,
            out GenealogyOwnerSnapshot decoded, out P12AResult decodeError), Is.True, decodeError?.SectionCode);
        Assert.That(decoded.SchemaVersion, Is.EqualTo(GenealogyOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(decoded.Revision, Is.EqualTo(7L));
        Assert.That(decoded.Records.Select(record => record.ParentId.Value),
            Is.EqualTo(new[] { "a", "b" }));
        Assert.That(decoded.Records.Select(record => record.ChildId.Value),
            Is.EqualTo(new[] { "c", "d" }));
    }

    [Test]
    public void EncodeRejectsCyclicParentageWithoutProducingASection()
    {
        GenealogyOwnerSnapshot source = new GenealogyOwnerSnapshot(
            GenealogyOwnerSnapshot.CurrentSchemaVersion,
            2L,
            new[]
            {
                new ParentageRecord(new PersonId("a"), new PersonId("b")),
                new ParentageRecord(new PersonId("b"), new PersonId("a"))
            });

        Assert.That(P12AGenealogyOwnerCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult result), Is.False);
        Assert.That(encoded, Is.Null);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void EncodeRejectsUnsupportedGenealogySchema()
    {
        GenealogyOwnerSnapshot source = new GenealogyOwnerSnapshot(
            GenealogyOwnerSnapshot.CurrentSchemaVersion + 1,
            0L,
            Array.Empty<ParentageRecord>());

        Assert.That(P12AGenealogyOwnerCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult result), Is.False);
        Assert.That(encoded, Is.Null);
        Assert.That(result, Is.Not.Null);
    }
}
