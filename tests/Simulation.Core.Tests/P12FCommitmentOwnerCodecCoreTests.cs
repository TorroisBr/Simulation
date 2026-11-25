using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12FCommitmentOwnerCodecCoreTests
{
    [Test]
    public void TravelPartyRoundTripPreservesOwnerRowsOrderAndFloatBits()
    {
        P12FTravelPartyOwnerSnapshot source = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion,
            4,
            new[]
            {
                new P12FTravelPartyOwnerSnapshotRecord(
                    "party-1", "location-a", "location-b", "route-a-b",
                    new[] { "npc-traveler" }, new[] { "npc-escort" },
                    new[] { "npc-traveler", "npc-escort" },
                    new[]
                    {
                        new TravelPartyMemberCost("npc-traveler", -0.0f),
                        new TravelPartyMemberCost("npc-escort", 12.25f)
                    },
                    3,
                    "decision-origin")
            });

        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeTravelParties(source,
            out P12AOwnerSection encoded, out P12AResult encodeFailure), Is.True, encodeFailure?.Code.ToString());
        Assert.That(encoded.Id, Is.EqualTo(TravelPartyCensusProvider.SectionId));
        Assert.That(encoded.Revision, Is.EqualTo(4));
        Assert.That(encoded.Rows, Has.Count.EqualTo(1));
        Assert.That(encoded.Rows[0].OwnerKey, Is.EqualTo("travel-party/party-1"));
        Assert.That(encoded.Rows[0].Revision, Is.Zero);

        Assert.That(P12AFCommitmentOwnerCodec.TryDecodeTravelParties(encoded,
            out P12FTravelPartyOwnerSnapshot decoded, out P12AResult decodeFailure), Is.True, decodeFailure?.Code.ToString());
        Assert.That(decoded.Parties[0].TravelerRuntimeIds, Is.EqualTo(new[] { "npc-traveler" }));
        Assert.That(decoded.Parties[0].EscortRuntimeIds, Is.EqualTo(new[] { "npc-escort" }));
        Assert.That(decoded.Parties[0].MemberRuntimeIds, Is.EqualTo(new[] { "npc-traveler", "npc-escort" }));
        Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(decoded.Parties[0].MemberCosts[0].Amount), 0),
            Is.EqualTo(BitConverter.ToInt32(BitConverter.GetBytes(-0.0f), 0)));
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeTravelParties(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(Payloads(encodedAgain), Is.EqualTo(Payloads(encoded)));

        byte[] trailing = encoded.Rows[0].Payload.Concat(new byte[] { 0x7f }).ToArray();
        P12AOwnerSection withTrailingByte = new P12AOwnerSection(encoded.Id, encoded.Major, encoded.Minor,
            encoded.Role, encoded.AliasGroup, encoded.Revision,
            new[] { new P12AOwnerRow(encoded.Rows[0].OwnerKey, encoded.Rows[0].Revision, trailing) });
        Assert.That(P12AFCommitmentOwnerCodec.TryDecodeTravelParties(withTrailingByte, out _, out _), Is.False);
    }

    [Test]
    public void ExpeditionRoundTripPreservesOrderedProgressAndGraphReferences()
    {
        P12FExpeditionOwnerSnapshot source = new P12FExpeditionOwnerSnapshot(
            P12FExpeditionOwnerSnapshot.CurrentSchemaVersion,
            2,
            new[]
            {
                new P12FExpeditionOwnerSnapshotRecord(
                    "expedition-1", "site-1", "location-a", "location-b", "route-a-b",
                    "party-1", "decision-origin", new[] { "npc-a", "npc-b" },
                    new[] { "npc-a" }, new[] { "npc-b" }, ExpeditionState.TravelingToSite,
                    null, new[] { "place-first", "place-second" }, new[] { "connection-first" },
                    ExpeditionObjectiveType.Explore, null, null, null, 3, 2, false, true)
            });

        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeExpeditions(source,
            out P12AOwnerSection encoded, out P12AResult encodeFailure), Is.True, encodeFailure?.Code.ToString());
        Assert.That(encoded.Id, Is.EqualTo(ExpeditionCensusProvider.SectionId));
        Assert.That(encoded.Revision, Is.EqualTo(2));
        Assert.That(encoded.Rows[0].OwnerKey, Is.EqualTo("expedition/expedition-1"));
        Assert.That(encoded.Rows[0].Revision, Is.Zero);

        Assert.That(P12AFCommitmentOwnerCodec.TryDecodeExpeditions(encoded,
            out P12FExpeditionOwnerSnapshot decoded, out P12AResult decodeFailure), Is.True, decodeFailure?.Code.ToString());
        P12FExpeditionOwnerSnapshotRecord row = decoded.Expeditions[0];
        Assert.That(row.MemberRuntimeIds, Is.EqualTo(new[] { "npc-a", "npc-b" }));
        Assert.That(row.VisitedLocalPlaceRuntimeIds, Is.EqualTo(new[] { "place-first", "place-second" }));
        Assert.That(row.Progress, Is.EqualTo(2));
        Assert.That(row.ObjectiveCompleted, Is.False);
        Assert.That(row.AllowContinueAfterCompletion, Is.True);
        Assert.That(P12AFCommitmentOwnerCodec.TryEncodeExpeditions(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(Payloads(encodedAgain), Is.EqualTo(Payloads(encoded)));
    }

    private static byte[][] Payloads(P12AOwnerSection section)
    {
        List<byte[]> result = new List<byte[]>();
        foreach (P12AOwnerRow row in section.Rows)
            result.Add(row.Payload);
        return result.ToArray();
    }
}
