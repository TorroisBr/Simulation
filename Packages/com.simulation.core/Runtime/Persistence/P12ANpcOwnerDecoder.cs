using System;
using System.Collections.Generic;

/// <summary>
/// Reconstructs one detached NPC D/F row pair from the typed Daily-v1 owner
/// sections. Cross-owner references remain the responsibility of P12-D's
/// staged graph validator.
/// </summary>
internal static class P12ANpcOwnerDecoder
{
    internal static bool TryDecode(P12AContinuationImage image, string runtimeId,
        out P12DNpcDRow dRow, out P12DNpcFRow fRow, out P12AResult result)
    {
        dRow = null;
        fRow = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix, runtimeId);
        if (image == null || image.Sections == null || !IsRuntimeId(runtimeId)) return false;

        if (!TryGetUnique(image, DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                out P12AOwnerSection membership, out result)
            || !TryGetUnique(image, DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
                out P12AOwnerSection bindings, out result)
            || !P12APersonStoreOwnerCodec.TryDecode(membership, bindings,
                out PersonStoreOwnerSnapshot persons, out result))
            return false;

        return TryDecode(image, runtimeId, persons, out dRow, out fRow, out result);
    }

    internal static bool TryDecode(P12AContinuationImage image, string runtimeId,
        PersonStoreOwnerSnapshot persons,
        out P12DNpcDRow dRow, out P12DNpcFRow fRow, out P12AResult result)
    {
        dRow = null;
        fRow = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix, runtimeId);
        if (image == null || image.Sections == null || persons == null || !IsRuntimeId(runtimeId)) return false;

        PersonStoreOwnerSnapshotRow boundPerson = null;
        foreach (PersonStoreOwnerSnapshotRow person in persons.Rows)
        {
            if (person == null || person.MaterializedNpcRuntimeId != runtimeId) continue;
            if (boundPerson != null)
            {
                result = Invalid(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId), runtimeId);
                return false;
            }
            boundPerson = person;
        }

        List<string> expectedIds = FixedNpcSectionIds(runtimeId);
        Dictionary<string, P12AOwnerSection> sections = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (string id in expectedIds)
        {
            if (!TryGetUnique(image, id, out P12AOwnerSection section, out result)) return false;
            sections.Add(id, section);
        }

        string residenceSectionId = DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(runtimeId);
        bool hasNpcResidenceSection = TryGetOptionalUnique(image, residenceSectionId,
            out P12AOwnerSection npcResidenceSection, out result);
        if (result != null) return false;
        if ((boundPerson == null) == !hasNpcResidenceSection)
        {
            result = Invalid(residenceSectionId, runtimeId);
            return false;
        }

        if (!P12ANpcLifeStateOwnerCodec.TryDecode(
                sections[DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(21, runtimeId)],
                out P12ANpcLifeStateOwnerValue life, out result)
            || !SameRuntimeId(runtimeId, life?.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(21, runtimeId), out result)) return false;

        if (!P12ANpcStatusCrimeOwnerCodec.TryDecode(
                sections[DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(19, runtimeId)],
                out P12ANpcStatusCrimeOwnerValue status, out result)
            || !SameRuntimeId(runtimeId, status?.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(19, runtimeId), out result)) return false;

        if (!P12ANpcCurrentActionOwnerCodec.TryDecode(
                sections[DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId)],
                out string actionRuntimeId, out string actionDefinitionId,
                out P12DNpcActionValue action, out long currentActionRevision, out result)
            || !SameRuntimeId(runtimeId, actionRuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId), out result)
            || (action == null) != (actionDefinitionId == null)
            || (action != null && action.ActionDefinitionId != actionDefinitionId))
        {
            if (result == null) result = Invalid(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId), runtimeId);
            return false;
        }

        if (!P12ANpcTravelStateOwnerCodec.TryDecode(
                sections[DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, runtimeId)],
                out P12ANpcTravelStateOwnerValue travel, out result)
            || !SameRuntimeId(runtimeId, travel?.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, runtimeId), out result)
            || !HasConsistentPresenceBindings(travel))
        {
            if (result == null) result = Invalid(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, runtimeId), runtimeId);
            return false;
        }

        if (!P12ANpcInventoryOwnerCodec.TryDecode(
                sections[DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + runtimeId],
                out P12ANpcInventoryOwnerValue inventory, out result)
            || !SameRuntimeId(runtimeId, inventory?.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + runtimeId, out result)) return false;

        if (!P12ANpcMoneyAccountOwnerCodec.TryDecode(
                sections[DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + runtimeId],
                out P12ANpcMoneyAccountOwnerValue money, out result)
            || !SameRuntimeId(runtimeId, money?.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + runtimeId, out result)) return false;

        string merchantPlanId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind, runtimeId);
        if (!P12ANpcMerchantTradePlanOwnerCodec.TryDecode(sections[merchantPlanId],
                out P12DNpcMerchantPlanValue merchantPlan, out result)) return false;

        string travelPlanId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, runtimeId);
        if (!P12ANpcTravelPlanOwnerCodec.TryDecode(sections[travelPlanId],
                out P12DNpcTravelPlanValue travelPlan, out result)) return false;

        string siteId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(0, runtimeId);
        if (!P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(sections[siteId],
                out string siteRuntimeId, out IReadOnlyList<ExplorableSiteKnowledgeObservation> siteObservations,
                out long siteRevision, out result)
            || !SameRuntimeId(runtimeId, siteRuntimeId, siteId, out result)) return false;

        string topologyPlacesId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(1, runtimeId);
        string topologyConnectionsId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(2, runtimeId);
        if (!P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryDecode(
                sections[topologyPlacesId], sections[topologyConnectionsId],
                out string topologyRuntimeId, out result)
            || !SameRuntimeId(runtimeId, topologyRuntimeId, topologyPlacesId, out result)) return false;

        string oppositionId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(3, runtimeId);
        string notableId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(4, runtimeId);
        string resourcesId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(5, runtimeId);
        string accessId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(6, runtimeId);
        if (!P12ANpcAdventureIntelOwnerCodec.TryDecode(
                sections[oppositionId], sections[notableId], sections[resourcesId], sections[accessId],
                out P12ANpcAdventureIntelOwnerValue adventure, out result)
            || !SameRuntimeId(runtimeId, adventure?.RuntimeId, oppositionId, out result)) return false;

        string marketsId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(7, runtimeId);
        string liquidityId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(8, runtimeId);
        string shareReceiptsId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(9, runtimeId);
        if (!P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(
                sections[marketsId], sections[liquidityId], sections[shareReceiptsId],
                out P12ANpcCommercialKnowledgeOwnerValue commercial, out result)
            || !SameRuntimeId(runtimeId, commercial?.RuntimeId, marketsId, out result)) return false;

        string knownLocationsId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + runtimeId;
        string knownRoutesId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + runtimeId;
        if (!P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(
                sections[knownLocationsId], sections[knownRoutesId],
                out P12ANpcSpatialKnowledgeOwnerValue spatial, out result)
            || !SameRuntimeId(runtimeId, spatial?.RuntimeId, knownLocationsId, out result)) return false;

        string localReceiptsId = DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsSectionIdFor(runtimeId);
        string tradeReceiptsId = DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsSectionIdFor(runtimeId);
        if (!P12ANpcReceiptEmptyOwnerCodec.TryDecode(
                sections[localReceiptsId], sections[tradeReceiptsId],
                out string receiptRuntimeId, out result)
            || !SameRuntimeId(runtimeId, receiptRuntimeId, localReceiptsId, out result)) return false;

        string personId = null;
        string residenceId;
        if (boundPerson == null)
        {
            if (!P12ANpcResidenceOwnerCodec.TryDecode(npcResidenceSection,
                    out P12ANpcResidenceOwnerValue residence, out result)
                || !SameRuntimeId(runtimeId, residence?.RuntimeId, residenceSectionId, out result)
                || residence.Revision != travel.ResidenceRevision)
            {
                if (result == null) result = Invalid(residenceSectionId, runtimeId);
                return false;
            }
            residenceId = residence.ResidenceSettlementRuntimeId;
        }
        else
        {
            if (!PersonId.TryCreate(boundPerson.PersonIdValue, out PersonId typedPersonId))
            {
                result = Invalid(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId, runtimeId);
                return false;
            }
            string personResidenceId = DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(typedPersonId);
            if (!TryGetUnique(image, personResidenceId, out P12AOwnerSection personResidenceSection, out result)
                || !P12APersonLifeResidenceOwnerCodec.TryDecode(personResidenceSection,
                    out P12APersonLifeResidenceOwnerValue personResidence, out result)
                || !SamePersonResidence(boundPerson, personResidence, runtimeId, out result)) return false;
            personId = boundPerson.PersonIdValue;
            residenceId = boundPerson.ResidenceSettlementRuntimeId;
        }

        try
        {
            dRow = new P12DNpcDRow(runtimeId, life.NpcDefinitionId, personId, residenceId,
                life.LifeState, life.InjurySeverity, status.CurrentStatusDefinitionIds,
                status.HiddenDaysRemaining, actionDefinitionId,
                travel.CurrentCityRuntimeId, travel.CurrentLocationRuntimeId,
                travel.DestinationCityRuntimeId, travel.DestinationLocationRuntimeId,
                inventory.Rows, inventory.Revision, money.Balance, money.Revision,
                life.Revision, travel.ResidenceRevision, status.Revision, currentActionRevision);

            fRow = new P12DNpcFRow(runtimeId, action,
                travel.TravelDaysRemaining, travel.TravelDaysTotal, travel.TravelRouteRuntimeId,
                travel.TravelStartedToday, travel.TravelOriginDecisionId, travel.ActiveTravelPartyId,
                travel.Revision, merchantPlan, travelPlan,
                siteObservations, siteRevision, spatial.LocationIds, spatial.RouteIds, spatial.Revision,
                adventure.Opposition, adventure.NotableItems, adventure.Resources, adventure.Access,
                adventure.Revision, commercial.Markets, commercial.Liquidity, commercial.Receipts,
                commercial.Revision);
        }
        catch (ArgumentException)
        {
            dRow = null;
            fRow = null;
            result = Invalid(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId), runtimeId);
            return false;
        }

        result = null;
        return true;
    }

    private static List<string> FixedNpcSectionIds(string runtimeId)
    {
        List<string> ids = new List<string>(22);
        for (int kind = 0; kind < 10; kind++)
            ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(kind, runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + runtimeId);
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + runtimeId);
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + runtimeId);
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + runtimeId);
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind, runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsSectionIdFor(runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsSectionIdFor(runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(19, runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId));
        ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(21, runtimeId));
        return ids;
    }

    private static bool TryGetUnique(P12AContinuationImage image, string id,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
        int count = 0;
        foreach (P12AOwnerSection candidate in image.Sections)
        {
            if (candidate == null || candidate.Id != id) continue;
            section = candidate;
            count++;
        }
        if (count != 1)
        {
            section = null;
            return false;
        }
        result = null;
        return true;
    }

    private static bool TryGetOptionalUnique(P12AContinuationImage image, string id,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = null;
        int count = 0;
        foreach (P12AOwnerSection candidate in image.Sections)
        {
            if (candidate == null || candidate.Id != id) continue;
            section = candidate;
            count++;
        }
        if (count > 1)
        {
            section = null;
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
            return false;
        }
        return count == 1;
    }

    private static bool SameRuntimeId(string expected, string actual, string sectionId,
        out P12AResult result)
    {
        result = null;
        if (string.Equals(expected, actual, StringComparison.Ordinal)) return true;
        result = Invalid(sectionId, expected);
        return false;
    }

    private static bool HasConsistentPresenceBindings(P12ANpcTravelStateOwnerValue travel)
        => travel != null
            && (travel.CurrentCityRuntimeId == null) == (travel.CurrentLocationRuntimeId == null)
            && (travel.DestinationCityRuntimeId == null || travel.DestinationLocationRuntimeId != null)
            && (travel.TravelDaysRemaining == 0 || travel.DestinationLocationRuntimeId != null);

    private static bool SamePersonResidence(PersonStoreOwnerSnapshotRow person,
        P12APersonLifeResidenceOwnerValue lifeResidence, string runtimeId, out P12AResult result)
    {
        result = null;
        if (person != null && lifeResidence != null
            && person.PersonIdValue == lifeResidence.PersonIdValue
            && person.BirthAbsoluteDay == lifeResidence.BirthAbsoluteDay
            && person.DeathAbsoluteDay == lifeResidence.DeathAbsoluteDay
            && person.ResidenceSettlementRuntimeId == lifeResidence.ResidenceSettlementRuntimeId
            && person.LifeResidenceRevision == lifeResidence.Revision)
            return true;
        result = Invalid(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId, runtimeId);
        return false;
    }

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static P12AResult Invalid(string sectionId, string runtimeId)
        => new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
}
