using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ECrimeSocialAppraisalOwnerCodecTests
{
    [Test]
    public void CrimeSocialCodecsRoundTripTypedFactsNullsAndOwnerRevisions()
    {
        P12ECrimeSocialAppraisalOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeOutcomes(source,
            out P12AOwnerSection outcomes, out P12AResult outcomeError), Is.True, outcomeError?.Code.ToString());
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeKnowledge(source,
            out P12AOwnerSection knowledge, out P12AResult knowledgeError), Is.True, knowledgeError?.Code.ToString());
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeReactions(source,
            out P12AOwnerSection reactions, out P12AResult reactionError), Is.True, reactionError?.Code.ToString());

        Assert.That(outcomes.Revision, Is.EqualTo(4L));
        Assert.That(knowledge.Revision, Is.EqualTo(7L));
        Assert.That(reactions.Revision, Is.EqualTo(9L));
        Assert.That(outcomes.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(outcomes.AliasGroup, Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(outcomes.Id)));

        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryDecodeOutcomes(outcomes, 8L,
            out P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> decodedOutcomes, out outcomeError), Is.True,
            outcomeError?.Code.ToString());
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryDecodeKnowledge(knowledge, 8L,
            out P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> decodedKnowledge, out knowledgeError), Is.True,
            knowledgeError?.Code.ToString());
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryDecodeReactions(reactions, 8L,
            out P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> decodedReactions, out reactionError), Is.True,
            reactionError?.Code.ToString());

        P12ECrimeOutcomeSnapshotRow outcome = decodedOutcomes.Records[0];
        Assert.That(outcome.OutcomeId, Is.EqualTo(source.Outcomes.Records[0].OutcomeId));
        Assert.That(outcome.OriginDecisionId, Is.Null);
        Assert.That(decodedOutcomes.Revision, Is.EqualTo(4L));
        P12ECrimeKnowledgeSnapshotRow fact = decodedKnowledge.Records[0];
        Assert.That(fact.AttributionKind, Is.EqualTo((int)SocialPerceivedAttributionKind.Unknown));
        Assert.That(fact.AttributionPersonId, Is.Null);
        Assert.That(fact.KnownInvestigatorInstitutionId, Is.EqualTo("investigator"));
        Assert.That(fact.CognitiveBasisReference, Is.EqualTo(string.Empty));
        Assert.That(fact.CognitiveBasisPersonId, Is.EqualTo("witness"));
        P12ESocialReactionSnapshotRow reaction = decodedReactions.Records[0];
        Assert.That(reaction.SupersedesReactionId, Is.Null);
        Assert.That(reaction.CognitiveBasisReference, Is.EqualTo(string.Empty));
        Assert.That(reaction.AttributionInstitutionId, Is.EqualTo("investigator"));

        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeOutcomes(Combine(decodedOutcomes),
            out P12AOwnerSection outcomesAgain, out _), Is.True);
        Assert.That(Payloads(outcomesAgain), Is.EqualTo(Payloads(outcomes)));
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeKnowledge(Combine(decodedKnowledge),
            out P12AOwnerSection knowledgeAgain, out _), Is.True);
        Assert.That(Payloads(knowledgeAgain), Is.EqualTo(Payloads(knowledge)));
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeReactions(Combine(decodedReactions),
            out P12AOwnerSection reactionsAgain, out _), Is.True);
        Assert.That(Payloads(reactionsAgain), Is.EqualTo(Payloads(reactions)));
    }

    [Test]
    public void CrimeSocialCodecsRejectWrongOrderingUnknownEnumsAndMalformedOwnerKeys()
    {
        P12ECrimeSocialAppraisalOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeOutcomes(source,
            out P12AOwnerSection outcomes, out _), Is.True);
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryDecodeOutcomes(
            Copy(outcomes, outcomes.Major + 1, outcomes.AliasGroup, outcomes.Rows), 8L, out _, out _), Is.False);

        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryEncodeKnowledge(source,
            out P12AOwnerSection knowledge, out _), Is.True);
        byte[] badEnum = (byte[])knowledge.Rows[0].Payload.Clone();
        // Contract string, kind, evaluator string, outcome string, then role.
        int roleOffset = StringFieldEnd(badEnum, 0);
        roleOffset += 1;
        roleOffset = StringFieldEnd(badEnum, roleOffset);
        roleOffset = StringFieldEnd(badEnum, roleOffset);
        badEnum[roleOffset] = 0xff;
        badEnum[roleOffset + 1] = 0xff;
        P12AOwnerRow invalidEnumRow = new P12AOwnerRow(knowledge.Rows[0].OwnerKey, 0L, badEnum);
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryDecodeKnowledge(
            Copy(knowledge, knowledge.Major, knowledge.AliasGroup, new[] { invalidEnumRow }), 8L,
            out _, out _), Is.False);

        P12AOwnerRow mismatchedKey = new P12AOwnerRow("knowledge/other", 0L, knowledge.Rows[0].Payload);
        Assert.That(P12ECrimeSocialAppraisalOwnerCodec.TryDecodeKnowledge(
            Copy(knowledge, knowledge.Major, knowledge.AliasGroup, new[] { mismatchedKey }), 8L,
            out _, out _), Is.False);
    }

    private static P12ECrimeSocialAppraisalOwnerSnapshot CreateSnapshot()
    {
        PersonId perpetrator = new PersonId("perp");
        PersonId victim = new PersonId("victim");
        TheftOutcomeId outcomeId = TheftOutcomeId.Create(perpetrator, victim, 5L, "occurrence");
        P12ECrimeOutcomeSnapshotRow outcome = new P12ECrimeOutcomeSnapshotRow(outcomeId.Value,
            perpetrator.Value, victim.Value, 20, 5L, "occurrence", null);

        P12ECrimeKnowledgeSnapshotRow knowledge = new P12ECrimeKnowledgeSnapshotRow(
            victim.Value, outcomeId.Value, (int)CrimeKnowledgeRole.Victim, true,
            (int)SocialPerceivedAttributionKind.Unknown, null, null,
            null, "investigator", (int)SocialCognitiveBasisKind.ReceivedInformation,
            string.Empty, "witness", null, 6L);

        SocialSourceReference source = new SocialSourceReference("crime", "report");
        SocialReactionTarget target = SocialReactionTarget.ForTheftOutcome(outcomeId.Value);
        SocialPerceivedAttribution attribution = SocialPerceivedAttribution.BelievedInstitution(new InstitutionId("investigator"));
        SocialCognitiveBasis basis = new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation,
            string.Empty, sourcePersonId: new PersonId("witness"));
        SocialReactionId reactionId = SocialReactionId.Create(victim, source, target, attribution, basis,
            SocialReactionValence.Negative, SocialReactionSalience.High, 7L);
        P12ESocialReactionSnapshotRow reaction = new P12ESocialReactionSnapshotRow(reactionId.Value,
            victim.Value, source.Domain, source.StableId, (int)target.Kind, target.StableId,
            (int)attribution.Kind, null, attribution.InstitutionId.Value,
            (int)SocialReactionValence.Negative, (int)SocialReactionSalience.High,
            (int)basis.Kind, basis.Reference, basis.SourcePersonId.Value, null, 7L, null);

        return new P12ECrimeSocialAppraisalOwnerSnapshot(8L,
            new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 1, 1, 4L, new[] { outcome }),
            new P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, 1, 1, 7L, new[] { knowledge }),
            new P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, 1, 1, 9L, new[] { reaction }));
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source, int major, string alias,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, major, source.Minor, source.Role, alias, source.Revision, rows);

    private static P12ECrimeSocialAppraisalOwnerSnapshot Combine(
        P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> outcomes = null,
        P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> knowledge = null,
        P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> reactions = null)
        => new P12ECrimeSocialAppraisalOwnerSnapshot(8L,
            outcomes ?? new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 1, 0, 0L, Array.Empty<P12ECrimeOutcomeSnapshotRow>()),
            knowledge ?? new P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, 1, 0, 0L, Array.Empty<P12ECrimeKnowledgeSnapshotRow>()),
            reactions ?? new P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, 1, 0, 0L, Array.Empty<P12ESocialReactionSnapshotRow>()));

    private static P12ECrimeSocialAppraisalOwnerSnapshot Combine(
        P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> outcomes)
        => Combine(outcomes, null, null);

    private static P12ECrimeSocialAppraisalOwnerSnapshot Combine(
        P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> knowledge)
        => Combine(null, knowledge, null);

    private static P12ECrimeSocialAppraisalOwnerSnapshot Combine(
        P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> reactions)
        => Combine(null, null, reactions);

    private static IEnumerable<string> Payloads(P12AOwnerSection section)
        => section.Rows.Select(row => Convert.ToBase64String(row.Payload));

    private static int StringFieldEnd(byte[] bytes, int offset)
    {
        int length = BitConverter.ToInt32(bytes, offset);
        return checked(offset + sizeof(int) + length);
    }
}
