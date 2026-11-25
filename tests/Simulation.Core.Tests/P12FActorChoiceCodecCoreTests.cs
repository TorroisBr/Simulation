using System;
using NUnit.Framework;

public sealed class P12FActorChoiceCodecCoreTests
{
    [Test]
    public void TerminalHistoryRoundTripsWithStableInputIdentityAndSequence()
    {
        ActorChoiceInput rejected = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-core-000001"), "command-core-1", 1,
            new PersonId("person-core-1"), "action-core-1", WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request, 7, ActorChoiceInputStatus.Rejected,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.Rejected, 7, 0,
                    failure: ActorChoiceFailure.ActionUnavailable)
            });
        P12FActorChoiceSnapshot source = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { rejected }, 2, 1);

        Assert.That(P12AActorChoiceCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeFailure), Is.True, encodeFailure?.Code.ToString());
        Assert.That(encoded.Id, Is.EqualTo(ActorChoiceP11CensusProvider.SectionId));
        Assert.That(encoded.Rows, Has.Count.EqualTo(1));
        Assert.That(encoded.Rows[0].OwnerKey, Is.EqualTo("input/actor-choice-core-000001"));
        Assert.That(P12AActorChoiceCodec.TryDecode(encoded,
            out P12FActorChoiceSnapshot decoded, out P12AResult decodeFailure), Is.True, decodeFailure?.Code.ToString());
        Assert.That(decoded.NextInputSequence, Is.EqualTo(2));
        Assert.That(decoded.CensusRevision, Is.EqualTo(1));
        Assert.That(decoded.Inputs[0].InputId.Value, Is.EqualTo("actor-choice-core-000001"));
        Assert.That(decoded.Inputs[0].Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(decoded.Inputs[0].Dispositions[0].Failure, Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
        Assert.That(P12AActorChoiceCodec.TryEncode(decoded,
            out P12AOwnerSection reencoded, out _), Is.True);
        CollectionAssert.AreEqual(encoded.Rows[0].Payload, reencoded.Rows[0].Payload);
    }

    [Test]
    public void ExactEmptyInitialOwnerRoundTrips()
    {
        P12FActorChoiceSnapshot source = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, Array.Empty<ActorChoiceInput>(), 1, 0);

        Assert.That(P12AActorChoiceCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeFailure), Is.True, encodeFailure?.Code.ToString());
        Assert.That(encoded.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(encoded.Rows, Is.Empty);
        Assert.That(P12AActorChoiceCodec.TryDecode(encoded,
            out P12FActorChoiceSnapshot decoded, out P12AResult decodeFailure), Is.True, decodeFailure?.Code.ToString());
        Assert.That(decoded.NextInputSequence, Is.EqualTo(1));
        Assert.That(decoded.CensusRevision, Is.Zero);
    }

    [Test]
    public void PendingAndTamperedRowsAreRejected()
    {
        ActorChoiceInput pending = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-core-000002"), "command-core-2", 1,
            new PersonId("person-core-2"), "action-core-2", WorldCommandOrigin.GM,
            WorldCommandAuthorityMode.Declare, 8, ActorChoiceInputStatus.Pending,
            Array.Empty<ActorChoiceDisposition>());
        P12FActorChoiceSnapshot pendingSnapshot = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { pending }, 2, 1);
        Assert.That(P12AActorChoiceCodec.TryEncode(pendingSnapshot, out _, out _), Is.False);

        ActorChoiceInput terminal = new ActorChoiceInput(
            new ActorChoiceInputId("actor-choice-core-000003"), "command-core-3", 1,
            new PersonId("person-core-3"), "action-core-3", WorldCommandOrigin.GM,
            WorldCommandAuthorityMode.Declare, 9, ActorChoiceInputStatus.Rejected,
            new[]
            {
                new ActorChoiceDisposition(1, ActorChoiceDispositionKind.Rejected, 9, 0,
                    failure: ActorChoiceFailure.ActorUnavailable)
            });
        P12FActorChoiceSnapshot terminalSnapshot = new P12FActorChoiceSnapshot(
            P12FActorChoiceSnapshot.CurrentSchemaVersion, new[] { terminal }, 2, 1);
        Assert.That(P12AActorChoiceCodec.TryEncode(terminalSnapshot,
            out P12AOwnerSection encoded, out _), Is.True);
        P12AOwnerSection tampered = new P12AOwnerSection(encoded.Id, encoded.Major, encoded.Minor,
            encoded.Role, encoded.AliasGroup, encoded.Revision,
            new[] { new P12AOwnerRow("input/actor-choice-other", encoded.Rows[0].Revision,
                encoded.Rows[0].Payload) });
        Assert.That(P12AActorChoiceCodec.TryDecode(tampered, out _, out _), Is.False);
    }
}
