using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Composes the complete selected Daily-v1 owner-section set for one NPC.</summary>
internal static class P12ANpcOwnerCodec
{
    internal static bool TryEncode(P12DNpcDRow dRow, P12DNpcFRow fRow,
        IReadOnlyList<OwnerSectionCensusSnapshot> census,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            NpcCurrentActionCensusProvider.SectionPrefix, dRow?.RuntimeId);
        if (dRow == null || fRow == null || census == null
            || string.IsNullOrWhiteSpace(dRow.RuntimeId)
            || dRow.RuntimeId.IndexOf('/') >= 0 || dRow.RuntimeId.IndexOf('\0') >= 0
            || !string.Equals(dRow.RuntimeId, fRow.RuntimeId, StringComparison.Ordinal)) return false;

        string runtimeId = dRow.RuntimeId;
        string localId = P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(runtimeId);
        string tradeId = P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(runtimeId);
        string placesId = NpcKnowledgeCensusProvider.SectionIdFor(1, runtimeId);
        string connectionsId = NpcKnowledgeCensusProvider.SectionIdFor(2, runtimeId);
        if (!TryFindExact(census, localId, tradeId, placesId, connectionsId,
            out OwnerSectionCensusSnapshot local, out OwnerSectionCensusSnapshot trade,
            out OwnerSectionCensusSnapshot places, out OwnerSectionCensusSnapshot connections))
            return false;

        // Capture the conditional residence row from the immutable D snapshot
        // before encoding other sections. The final output is sorted below, so
        // this validation order has no effect on the canonical wire order.
        if (!P12ANpcResidenceOwnerCodec.TryEncode(dRow,
            out P12AOwnerSection residence, out result)) return false;

        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>(23);
        if (!Append(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(fRow,
                out P12AOwnerSection siteKnowledge, out result), siteKnowledge, encoded)
            || !Append(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode(runtimeId, places, connections,
                out P12AOwnerSection topologyPlaces, out P12AOwnerSection topologyConnections, out result),
                topologyPlaces, topologyConnections, encoded)
            || !Append(P12ANpcAdventureIntelOwnerCodec.TryEncode(fRow,
                out P12AOwnerSection opposition, out P12AOwnerSection notable, out P12AOwnerSection resources,
                out P12AOwnerSection access, out result), opposition, notable, resources, access, encoded)
            || !Append(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(fRow,
                out P12AOwnerSection markets, out P12AOwnerSection liquidity, out P12AOwnerSection receipts,
                out result), markets, liquidity, receipts, encoded)
            || !Append(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(fRow,
                out P12AOwnerSection knownLocations, out P12AOwnerSection knownRoutes, out result),
                knownLocations, knownRoutes, encoded)
            || !Append(P12ANpcTravelStateOwnerCodec.TryEncode(dRow, fRow,
                out P12AOwnerSection travelState, out result), travelState, encoded)
            || !Append(P12ANpcInventoryOwnerCodec.TryEncode(dRow,
                out P12AOwnerSection inventory, out result), inventory, encoded)
            || !Append(P12ANpcMoneyAccountOwnerCodec.TryEncode(dRow,
                out P12AOwnerSection money, out result), money, encoded)
            || !Append(P12ANpcMerchantTradePlanOwnerCodec.TryEncode(fRow,
                out P12AOwnerSection merchantPlan, out result), merchantPlan, encoded)
            || !Append(P12ANpcTravelPlanOwnerCodec.TryEncode(fRow,
                out P12AOwnerSection travelPlan, out result), travelPlan, encoded)
            || !Append(P12ANpcReceiptEmptyOwnerCodec.TryEncode(runtimeId, local, trade,
                out P12AOwnerSection localReceipts, out P12AOwnerSection tradeReceipts, out result),
                localReceipts, tradeReceipts, encoded)
            || !Append(P12ANpcStatusCrimeOwnerCodec.TryEncode(dRow,
                out P12AOwnerSection statusCrime, out result), statusCrime, encoded)
            || !Append(P12ANpcCurrentActionOwnerCodec.TryEncode(dRow, fRow,
                out P12AOwnerSection currentAction, out result), currentAction, encoded)
            || !Append(P12ANpcLifeStateOwnerCodec.TryEncode(dRow,
                out P12AOwnerSection lifeState, out result), lifeState, encoded)) return false;

        if (residence != null) encoded.Add(residence);

        encoded.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
        HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in encoded)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.Id) || !sectionIds.Add(section.Id)
                || section.Role != OwnerSectionRole.Required
                || (!P12ADailyV1Census.NpcSectionPrefixes.Any(prefix =>
                        section.Id.StartsWith(prefix, StringComparison.Ordinal))
                    && !section.Id.StartsWith(NpcLifecycleCensusProvider.ResidenceSectionPrefix,
                        StringComparison.Ordinal)))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    section?.Id ?? NpcCurrentActionCensusProvider.SectionPrefix, runtimeId);
                return false;
            }
        }

        int expectedCount = dRow.PersonIdValue == null ? 23 : 22;
        if (encoded.Count != expectedCount)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                NpcCurrentActionCensusProvider.SectionIdFor(runtimeId), runtimeId);
            return false;
        }
        sections = encoded.AsReadOnly();
        result = null;
        return true;
    }

    private static bool TryFindExact(IReadOnlyList<OwnerSectionCensusSnapshot> census,
        string localId, string tradeId, string placesId, string connectionsId,
        out OwnerSectionCensusSnapshot local, out OwnerSectionCensusSnapshot trade,
        out OwnerSectionCensusSnapshot places, out OwnerSectionCensusSnapshot connections)
    {
        local = trade = places = connections = null;
        int localCount = 0, tradeCount = 0, placesCount = 0, connectionCount = 0;
        foreach (OwnerSectionCensusSnapshot snapshot in census)
        {
            if (snapshot == null) return false;
            if (snapshot.SectionId == localId) { local = snapshot; localCount++; }
            else if (snapshot.SectionId == tradeId) { trade = snapshot; tradeCount++; }
            else if (snapshot.SectionId == placesId) { places = snapshot; placesCount++; }
            else if (snapshot.SectionId == connectionsId) { connections = snapshot; connectionCount++; }
        }
        return localCount == 1 && tradeCount == 1 && placesCount == 1 && connectionCount == 1
            && local.OwnerInstanceIdentity != null && trade.OwnerInstanceIdentity != null
            && places.OwnerInstanceIdentity != null && ReferenceEquals(places.OwnerInstanceIdentity,
                connections.OwnerInstanceIdentity)
            && !ReferenceEquals(local.OwnerInstanceIdentity, trade.OwnerInstanceIdentity)
            && !ReferenceEquals(local.OwnerInstanceIdentity, places.OwnerInstanceIdentity)
            && !ReferenceEquals(trade.OwnerInstanceIdentity, places.OwnerInstanceIdentity);
    }

    private static bool Append(bool success, P12AOwnerSection section,
        List<P12AOwnerSection> destination)
    {
        if (!success || section == null) return false;
        destination.Add(section);
        return true;
    }

    private static bool Append(bool success, P12AOwnerSection first, P12AOwnerSection second,
        List<P12AOwnerSection> destination)
    {
        if (!success || first == null || second == null) return false;
        destination.Add(first); destination.Add(second);
        return true;
    }

    private static bool Append(bool success, P12AOwnerSection first, P12AOwnerSection second,
        P12AOwnerSection third, List<P12AOwnerSection> destination)
    {
        if (!success || first == null || second == null || third == null) return false;
        destination.Add(first); destination.Add(second); destination.Add(third);
        return true;
    }

    private static bool Append(bool success, P12AOwnerSection first, P12AOwnerSection second,
        P12AOwnerSection third, P12AOwnerSection fourth, List<P12AOwnerSection> destination)
    {
        if (!success || first == null || second == null || third == null || fourth == null) return false;
        destination.Add(first); destination.Add(second); destination.Add(third); destination.Add(fourth);
        return true;
    }
}
