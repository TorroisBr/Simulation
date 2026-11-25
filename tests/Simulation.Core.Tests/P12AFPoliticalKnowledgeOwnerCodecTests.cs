using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12AFPoliticalKnowledgeOwnerCodecTests
{
    [Test]
    public void RoundTripPreservesHolderIdentityAndRevision()
    {
        PoliticalKnowledgeRuntime runtime = new PoliticalKnowledgeRuntime(
            PoliticalKnowledgeHolder.ForPerson(new PersonId("person-1")));
        P12FPoliticalKnowledgeOwnerSnapshot source =
            new P12FPoliticalKnowledgeOwnerSnapshot(new[] { runtime }, 1L);

        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(
            source, out P12AOwnerSection encoded, out P12AResult encodeFailure), Is.True,
            encodeFailure?.Code.ToString());
        Assert.That(encoded.Id, Is.EqualTo(PoliticalKnowledgeStoreCensusProvider.SectionId));
        Assert.That(encoded.Revision, Is.EqualTo(1L));
        Assert.That(encoded.Rows.Count, Is.EqualTo(1));
        Assert.That(encoded.Rows[0].OwnerKey, Is.EqualTo("holder/Person:person-1"));

        Assert.That(P12AFPoliticalKnowledgeCodec.TryDecode(
            encoded, out P12FPoliticalKnowledgeOwnerSnapshot decoded, out P12AResult decodeFailure), Is.True,
            decodeFailure?.Code.ToString());
        Assert.That(decoded.Revision, Is.EqualTo(1L));
        IReadOnlyList<PoliticalKnowledgeRuntime> restored = decoded.CopyDetachedRuntimes();
        Assert.That(restored.Count, Is.EqualTo(1));
        Assert.That(restored[0].Holder.Kind, Is.EqualTo(PoliticalKnowledgeHolderKind.Person));
        Assert.That(restored[0].Holder.PersonId.Value, Is.EqualTo("person-1"));
        Assert.That(restored[0].Observations.Count, Is.Zero);

        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(
            decoded, out P12AOwnerSection reencoded, out P12AResult reencodeFailure), Is.True,
            reencodeFailure?.Code.ToString());
        CollectionAssert.AreEqual(encoded.Rows[0].Payload, reencoded.Rows[0].Payload);
    }

    [Test]
    public void RejectsDetachedNullKnowledgeRows()
    {
        P12FPoliticalKnowledgeOwnerSnapshot source =
            new P12FPoliticalKnowledgeOwnerSnapshot(new PoliticalKnowledgeRuntime[] { null }, 1L);

        Assert.That(source.CopyDetachedRuntimes(), Is.Null);
        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(
            source, out P12AOwnerSection encoded, out P12AResult failure), Is.False);
        Assert.That(encoded, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    [Test]
    public void RejectsRevisionThatCannotCoverEncodedOwnerRows()
    {
        PoliticalKnowledgeRuntime runtime = new PoliticalKnowledgeRuntime(
            PoliticalKnowledgeHolder.ForInstitution(new InstitutionId("institution-1")));
        P12FPoliticalKnowledgeOwnerSnapshot source =
            new P12FPoliticalKnowledgeOwnerSnapshot(new[] { runtime }, 0L);

        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(
            source, out P12AOwnerSection encoded, out P12AResult failure), Is.False);
        Assert.That(encoded, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    [Test]
    public void EmptyImageStagesAgainstFreshCoreOwnerRoots()
    {
        P12FPoliticalKnowledgeOwnerSnapshot source =
            new P12FPoliticalKnowledgeOwnerSnapshot(System.Array.Empty<PoliticalKnowledgeRuntime>(), 0L);
        Assert.That(P12AFPoliticalKnowledgeCodec.TryEncode(
            source, out P12AOwnerSection encoded, out P12AResult encodeFailure), Is.True,
            encodeFailure?.Code.ToString());
        Assert.That(P12AFPoliticalKnowledgeCodec.TryDecode(
            encoded, out P12FPoliticalKnowledgeOwnerSnapshot decoded, out P12AResult decodeFailure), Is.True,
            decodeFailure?.Code.ToString());

        PersonStore people = new PersonStore();
        InstitutionStore institutions = new InstitutionStore();
        PoliticalClaimStore claims = new PoliticalClaimStore();
        FactionStore factions = new FactionStore(people);
        OfficeStore offices = new OfficeStore(institutions);
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);

        Assert.That(decoded.TryStageForImage(
            people, institutions, claims, factions, offices, properties, 0L,
            out PoliticalKnowledgeStore staged, out string failure), Is.True, failure);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
    }
}
