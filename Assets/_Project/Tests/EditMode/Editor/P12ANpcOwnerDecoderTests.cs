using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcOwnerDecoderTests
{
    [Test]
    public void ReconstructsUnboundNpcRowsFromCompleteTypedSectionSet()
    {
        const string runtimeId = "npc-decoder-a";
        P12DNpcDRow sourceD = DRow(runtimeId, null, 7L);
        P12DNpcFRow sourceF = FRow(runtimeId);
        P12AContinuationImage image = EncodeImage(sourceD, sourceF, null);

        Assert.That(P12ANpcOwnerDecoder.TryDecode(image, runtimeId,
            out P12DNpcDRow decodedD, out P12DNpcFRow decodedF, out P12AResult error),
            Is.True, error?.SectionCode + ":" + error?.Code);

        Assert.That(decodedD.RuntimeId, Is.EqualTo(runtimeId));
        Assert.That(decodedD.PersonIdValue, Is.Null);
        Assert.That(decodedD.ResidenceSettlementRuntimeId, Is.EqualTo("city-home"));
        Assert.That(decodedD.ResidenceRevision, Is.EqualTo(7L));
        Assert.That(decodedD.NpcDefinitionId, Is.EqualTo(sourceD.NpcDefinitionId));
        Assert.That(decodedD.LifeStateRevision, Is.EqualTo(sourceD.LifeStateRevision));
        Assert.That(decodedD.CurrentStatusDefinitionIds, Is.EqualTo(sourceD.CurrentStatusDefinitionIds));
        Assert.That(decodedD.HiddenDaysRemaining, Is.EqualTo(sourceD.HiddenDaysRemaining));
        Assert.That(decodedD.CurrentCityRuntimeId, Is.EqualTo("city-home"));
        Assert.That(decodedD.CurrentLocationRuntimeId, Is.EqualTo("location-home"));
        Assert.That(decodedD.DestinationCityRuntimeId, Is.EqualTo("city-target"));
        Assert.That(decodedD.DestinationLocationRuntimeId, Is.EqualTo("location-target"));
        Assert.That(decodedD.InventoryRows.Select(row => row.ItemDefinitionId),
            Is.EqualTo(sourceD.InventoryRows.Select(row => row.ItemDefinitionId)));
        Assert.That(decodedD.InventoryRows[0].Amount, Is.EqualTo(4));
        Assert.That(decodedD.InventoryRevision, Is.EqualTo(2L));
        Assert.That(decodedD.MoneyBalance, Is.EqualTo(8.5f));
        Assert.That(decodedD.MoneyRevision, Is.EqualTo(3L));
        Assert.That(decodedD.CrimeJusticeRevision, Is.EqualTo(3L));
        Assert.That(decodedD.CurrentActionRevision, Is.EqualTo(4L));

        Assert.That(decodedF.RuntimeId, Is.EqualTo(runtimeId));
        Assert.That(decodedF.Action.ActionDefinitionId, Is.EqualTo("action-current"));
        Assert.That(decodedF.TravelStateRevision, Is.EqualTo(6L));
        Assert.That(decodedF.MerchantPlan.ItemDefinitionId, Is.EqualTo("item-iron"));
        Assert.That(decodedF.MerchantPlan.Revision, Is.EqualTo(6L));
        Assert.That(decodedF.TravelPlan.TargetLocationRuntimeId, Is.EqualTo("location-plan"));
        Assert.That(decodedF.TravelPlan.Revision, Is.EqualTo(8L));
        Assert.That(decodedF.SiteObservations, Has.Count.EqualTo(1));
        Assert.That(decodedF.SiteObservations[0].SiteRuntimeId, Is.EqualTo("site-known"));
        Assert.That(decodedF.SiteKnowledgeRevision, Is.EqualTo(2L));
        Assert.That(decodedF.KnownLocationRuntimeIds, Is.EqualTo(new[] { "known-location" }));
        Assert.That(decodedF.SpatialKnowledgeRevision, Is.EqualTo(1L));
    }

    [Test]
    public void ReconstructsBoundPersonIdentityAndChecksDuplicatedResidenceFacts()
    {
        const string runtimeId = "npc-decoder-bound";
        const string personId = "person-decoder-bound";
        P12DNpcDRow sourceD = DRow(runtimeId, personId, 7L);
        P12DNpcFRow sourceF = FRow(runtimeId);
        PersonStoreOwnerSnapshotRow person = new PersonStoreOwnerSnapshotRow(
            personId, 12L, null, "city-home", runtimeId, 5L);
        P12AContinuationImage image = EncodeImage(sourceD, sourceF, person);

        Assert.That(image.Sections.Any(section => section.Id ==
            DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(runtimeId)), Is.False);
        Assert.That(P12ANpcOwnerDecoder.TryDecode(image, runtimeId,
            out P12DNpcDRow decodedD, out P12DNpcFRow decodedF, out P12AResult error),
            Is.True, error?.SectionCode + ":" + error?.Code);

        Assert.That(decodedD.PersonIdValue, Is.EqualTo(personId));
        Assert.That(decodedD.ResidenceSettlementRuntimeId, Is.EqualTo("city-home"));
        Assert.That(decodedD.ResidenceRevision, Is.EqualTo(7L));
        Assert.That(decodedF.RuntimeId, Is.EqualTo(runtimeId));
    }

    [Test]
    public void RejectsMissingDuplicateAndCrossRuntimeSections()
    {
        const string runtimeId = "npc-decoder-invalid";
        P12AContinuationImage valid = EncodeImage(DRow(runtimeId, null, 1L), FRow(runtimeId), null);
        string requiredId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId);
        P12AOwnerSection action = valid.Sections.Single(section => section.Id == requiredId);

        List<P12AOwnerSection> missing = valid.Sections.Where(section => section.Id != requiredId).ToList();
        Assert.That(P12ANpcOwnerDecoder.TryDecode(Image(missing), runtimeId,
            out _, out _, out _), Is.False);

        List<P12AOwnerSection> duplicate = valid.Sections.ToList();
        duplicate.Add(action);
        Assert.That(P12ANpcOwnerDecoder.TryDecode(Image(duplicate), runtimeId,
            out _, out _, out _), Is.False);

        P12ANpcCurrentActionOwnerCodec.TryEncode(DRow("npc-decoder-other", null, 1L),
            FRow("npc-decoder-other"), out P12AOwnerSection otherAction, out P12AResult encodeError);
        Assert.That(otherAction, Is.Not.Null, encodeError?.Code.ToString());
        P12AOwnerSection crossRuntime = new P12AOwnerSection(action.Id, action.Major, action.Minor,
            action.Role, action.AliasGroup, action.Revision,
            new[] { new P12AOwnerRow(action.Rows[0].OwnerKey, action.Rows[0].Revision,
                otherAction.Rows[0].Payload) });
        List<P12AOwnerSection> mismatched = valid.Sections.Select(section =>
            section.Id == requiredId ? crossRuntime : section).ToList();
        Assert.That(P12ANpcOwnerDecoder.TryDecode(Image(mismatched), runtimeId,
            out _, out _, out _), Is.False);
    }

    [Test]
    public void BatchDecoderReturnsAllNpcsInOrdinalIdentityOrderAndPublishesNoPartialRows()
    {
        P12AContinuationImage first = EncodeImage(DRow("npc-z", null, 1L), FRow("npc-z"), null);
        P12AContinuationImage second = EncodeImage(DRow("npc-a", null, 1L), FRow("npc-a"), null);
        P12CRuntimeIdentityOwnerSnapshot identities = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, 8L,
            new[]
            {
                new KeyValuePair<string, string>("npc", "npc-z"),
                new KeyValuePair<string, string>("npc", "npc-a"),
                new KeyValuePair<string, string>("city", "city-home"),
                new KeyValuePair<string, string>("city", "city-target"),
                new KeyValuePair<string, string>("location", "location-home"),
                new KeyValuePair<string, string>("location", "location-target"),
                new KeyValuePair<string, string>("location", "location-site"),
                new KeyValuePair<string, string>("location", "location-plan")
            });
        Assert.That(P12ARuntimeIdentityOwnerCodec.TryEncode(identities,
            out IReadOnlyList<P12AOwnerSection> identitySections, out P12AResult identityError),
            Is.True, identityError?.SectionCode + ":" + identityError?.Code);

        List<P12AOwnerSection> sections = identitySections.ToList();
        sections.AddRange(first.Sections.Where(section =>
            section.Id == DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId
            || section.Id == DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId));
        sections.AddRange(first.Sections.Where(section =>
            section.Id != DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId
            && section.Id != DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId));
        sections.AddRange(second.Sections.Where(section =>
            section.Id != DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId
            && section.Id != DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId));
        P12AContinuationImage image = Image(sections);

        Assert.That(P12ADailyV1NpcOwnerBatchDecoder.TryDecode(image,
            out IReadOnlyList<P12DNpcDRow> dRows, out IReadOnlyList<P12DNpcFRow> fRows,
            out P12AResult error), Is.True, error?.SectionCode + ":" + error?.Code);
        Assert.That(dRows.Select(row => row.RuntimeId), Is.EqualTo(new[] { "npc-a", "npc-z" }));
        Assert.That(fRows.Select(row => row.RuntimeId), Is.EqualTo(new[] { "npc-a", "npc-z" }));

        string missingId = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-z";
        P12AContinuationImage incomplete = Image(sections.Where(section => section.Id != missingId));
        Assert.That(P12ADailyV1NpcOwnerBatchDecoder.TryDecode(incomplete,
            out IReadOnlyList<P12DNpcDRow> rejectedD, out IReadOnlyList<P12DNpcFRow> rejectedF,
            out P12AResult rejection), Is.False);
        Assert.That(rejection, Is.Not.Null);
        Assert.That(rejectedD, Is.Null);
        Assert.That(rejectedF, Is.Null);
    }

    private static P12AContinuationImage EncodeImage(P12DNpcDRow dRow,
        P12DNpcFRow fRow, PersonStoreOwnerSnapshotRow boundPerson)
    {
        Assert.That(P12ANpcOwnerCodec.TryEncode(dRow, fRow, Census(dRow.RuntimeId),
            out IReadOnlyList<P12AOwnerSection> npcSections, out P12AResult npcError),
            Is.True, npcError?.SectionCode + ":" + npcError?.Code);
        List<P12AOwnerSection> sections = npcSections.ToList();

        PersonStoreOwnerSnapshotRow[] people = boundPerson == null
            ? Array.Empty<PersonStoreOwnerSnapshotRow>()
            : new[] { boundPerson };
        int bindings = people.Count(person => person.MaterializedNpcRuntimeId != null);
        PersonStoreOwnerSnapshot personSnapshot = new PersonStoreOwnerSnapshot(
            PersonStoreOwnerSnapshot.CurrentSchemaId, PersonStoreOwnerSnapshot.CurrentSchemaVersion,
            people.Length + bindings, people.Length, bindings, people);
        Assert.That(P12APersonStoreOwnerCodec.TryEncode(personSnapshot,
            out P12AOwnerSection membership, out P12AOwnerSection binding, out P12AResult personError),
            Is.True, personError?.SectionCode + ":" + personError?.Code);
        sections.Add(membership);
        sections.Add(binding);
        if (boundPerson != null)
        {
            Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(boundPerson,
                out P12AOwnerSection lifeResidence, out P12AResult residenceError),
                Is.True, residenceError?.SectionCode + ":" + residenceError?.Code);
            sections.Add(lifeResidence);
        }

        return Image(sections);
    }

    private static P12AContinuationImage Image(IEnumerable<P12AOwnerSection> sections)
        => new P12AContinuationImage(
            new P12AContinuationHeader("world:00112233445566778899aabbccddeeff", 0L, 1L, -1L,
                "p9-authored", "p9-fingerprint", "p8-authored", "p8-fingerprint", 0L, null),
            new P12ACausalRootPayload(null, null), sections);

    private static IReadOnlyList<OwnerSectionCensusSnapshot> Census(string runtimeId)
    {
        object local = new object(), trade = new object(), topology = new object();
        return new[]
        {
            new OwnerSectionCensusSnapshot(DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsSectionIdFor(runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, OwnerSectionRole.Required, local, 0, 0L),
            new OwnerSectionCensusSnapshot(DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsSectionIdFor(runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, OwnerSectionRole.Required, trade, 0, 0L),
            new OwnerSectionCensusSnapshot(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(1, runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, OwnerSectionRole.Required, topology, 0, 0L),
            new OwnerSectionCensusSnapshot(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(2, runtimeId),
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, OwnerSectionRole.Required, topology, 0, 0L)
        };
    }

    private static P12DNpcDRow DRow(string runtimeId, string personId, long residenceRevision)
        => new P12DNpcDRow(runtimeId, "npc-definition", personId, "city-home",
            NpcLifeState.Alive, NpcInjurySeverity.None, new[] { "status-known" }, 2,
            "action-current", "city-home", "location-home", "city-target", "location-target",
            new[] { new P12DNpcInventoryRowValue("item-ore", 4, 2.5f) },
            2L, 8.5f, 3L, 2L, residenceRevision, 3L, 4L);

    private static P12DNpcFRow FRow(string runtimeId)
        => new P12DNpcFRow(runtimeId,
            new P12DNpcActionValue("action-current", null, null, null, 0, 0f, 1f,
                NpcTravelReason.None, 0f, null, null, null, null),
            0, 0, null, false, null, null, 6L,
            new P12DNpcMerchantPlanValue("item-iron", "city-home", "city-target",
                8, 3, 2.375f, 4, 7, "decision-merchant", 6L),
            new P12DNpcTravelPlanValue("location-plan", "city-target",
                NpcTravelReason.TradeReposition, 2.375f, 0.125f, "decision-travel", 8L),
            new[]
            {
                new ExplorableSiteKnowledgeObservation("site-known", "location-site",
                    1L, 2L, ExplorableSiteKnowledgeSource.DirectObservation)
            },
            2L, new[] { "known-location" }, Array.Empty<string>(), 1L,
            Array.Empty<AdventureOppositionObservation>(),
            Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(),
            Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(),
            Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);
}
