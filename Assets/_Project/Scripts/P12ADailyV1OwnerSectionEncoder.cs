using System;
using System.Collections.Generic;

/// <summary>Encodes the complete selected Daily-v1 C-F owner export.</summary>
internal static class P12ADailyV1OwnerSectionEncoder
{
    internal static bool TryEncode(IReadOnlyList<OwnerSectionCensusSnapshot> census,
        P12CRuntimeIdentityOwnerSnapshot runtimeIdentity,
        P12DDailyV1OwnerPackage d,
        P12EDailyV1OwnerPackage e,
        P12FDailyV1OwnerCapture f,
        long capturedDay,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        return TryEncode(census, runtimeIdentity, d, e, f, null, false, null, capturedDay,
            out sections, out result);
    }

    internal static bool TryEncodeWithCoreP12FSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> census,
        P12CRuntimeIdentityOwnerSnapshot runtimeIdentity,
        P12DDailyV1OwnerPackage d,
        P12EDailyV1OwnerPackage e,
        IReadOnlyList<P12AOwnerSection> coreP12FSections,
        long capturedDay,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        return TryEncode(census, runtimeIdentity, d, e, null, null, false, coreP12FSections,
            capturedDay, out sections, out result);
    }

    internal static bool TryEncodeWithCoreP12DOwnerSections(
        IReadOnlyList<OwnerSectionCensusSnapshot> census,
        P12CRuntimeIdentityOwnerSnapshot runtimeIdentity,
        P12DDailyV1OwnerPackage d,
        P12EDailyV1OwnerPackage e,
        IReadOnlyList<P12AOwnerSection> coreP12DSections,
        bool coreP12DIncludesNpcRows,
        IReadOnlyList<P12AOwnerSection> coreP12FSections,
        long capturedDay,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        return TryEncode(census, runtimeIdentity, d, e, null,
            coreP12DSections, coreP12DIncludesNpcRows, coreP12FSections,
            capturedDay, out sections, out result);
    }

    private static bool TryEncode(
        IReadOnlyList<OwnerSectionCensusSnapshot> census,
        P12CRuntimeIdentityOwnerSnapshot runtimeIdentity,
        P12DDailyV1OwnerPackage d,
        P12EDailyV1OwnerPackage e,
        P12FDailyV1OwnerCapture f,
        IReadOnlyList<P12AOwnerSection> coreP12DSections,
        bool coreP12DIncludesNpcRows,
        IReadOnlyList<P12AOwnerSection> coreP12FSections,
        long capturedDay,
        out IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (census == null || runtimeIdentity == null || d == null || e == null
            || ((f == null) == (coreP12FSections == null))
            || (coreP12DIncludesNpcRows && coreP12DSections == null)
            || capturedDay < 0L || d.CitySnapshots == null || d.NpcDRows == null
            || d.NpcFRows == null || d.NpcDRows.Count != d.NpcFRows.Count
            || d.PersonSnapshot == null || d.GenealogySnapshot == null
            || d.SpatialNetworkSnapshot == null) return false;

        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>();
        if (!P12ARuntimeIdentityOwnerCodec.TryEncode(runtimeIdentity,
                out IReadOnlyList<P12AOwnerSection> identitySections, out result)) return false;
        encoded.AddRange(identitySections);
        if (coreP12DSections != null)
        {
            encoded.AddRange(coreP12DSections);
        }
        else
        {
            if (!P12ASpatialNetworkOwnerCodec.TryEncode(d.SpatialNetworkSnapshot,
                    out P12AOwnerSection locations, out P12AOwnerSection routes, out result)
                || !P12APersonStoreOwnerCodec.TryEncode(d.PersonSnapshot,
                    out P12AOwnerSection membership, out P12AOwnerSection bindings, out result)
                || !P12AGenealogyOwnerCodec.TryEncode(d.GenealogySnapshot,
                    out P12AOwnerSection genealogy, out result)) return false;
            encoded.Add(locations);
            encoded.Add(routes);
            encoded.Add(membership);
            encoded.Add(bindings);
            encoded.Add(genealogy);

            foreach (PersonStoreOwnerSnapshotRow person in d.PersonSnapshot.Rows)
            {
                if (!P12APersonLifeResidenceOwnerCodec.TryEncode(person,
                        out P12AOwnerSection lifeResidence, out result)) return false;
                encoded.Add(lifeResidence);
            }

            if (!P12ACityOwnerSetCodec.TryEncode(d.CitySnapshots, census,
                    out IReadOnlyList<P12AOwnerSection> cities, out result)) return false;
            encoded.AddRange(cities);
        }

        if (coreP12DSections == null || !coreP12DIncludesNpcRows)
            for (int i = 0; i < d.NpcDRows.Count; i++)
            {
                if (!P12ANpcOwnerCodec.TryEncode(d.NpcDRows[i], d.NpcFRows[i], census,
                        out IReadOnlyList<P12AOwnerSection> npcSections, out result)) return false;
                encoded.AddRange(npcSections);
            }

        if (!P12EInstitutionOfficeOwnerCodec.TryEncode(e.InstitutionOfficeSnapshot,
                out P12AOwnerSection institutions, out P12AOwnerSection offices,
                out P12AOwnerSection incumbencies, out P12AOwnerSection tenures, out result)
            || !P12EPropertyEstateOwnerCodec.TryEncode(e.PropertyEstateSnapshot, capturedDay,
                out P12AOwnerSection ownership, out P12AOwnerSection transfers,
                out P12AOwnerSection estates, out result)
            || !P12EFactionOwnerCodec.TryEncode(e.FactionSnapshot, capturedDay,
                out P12AOwnerSection factions, out P12AOwnerSection affiliations, out result)
            || !P12EPoliticalClaimOwnerCodec.TryEncode(e.PoliticalClaimSnapshot, capturedDay,
                out P12AOwnerSection claims, out P12AOwnerSection recognitions, out result)
            || !P12EPoliticalSupportOwnerCodec.TryEncode(e.PoliticalSupportSnapshot,
                out P12AOwnerSection politicalSupport, out result)
            || !P12EPoliticalDecisionOwnerCodec.TryEncode(e.PoliticalDecisionSnapshot,
                out P12AOwnerSection politicalDecisions, out result)
            || !P12EMilitaryOwnerCodec.TryEncode(e.MilitarySnapshot,
                out IReadOnlyList<P12AOwnerSection> military, out result)
            || !P12EPersistentConflictWarBattleOwnerCodec.TryEncode(e.ConflictSnapshot,
                e.WarSnapshot, e.BattleSnapshot, out IReadOnlyList<P12AOwnerSection> conflicts,
                out result)
            || !P12EJusticeRecordsOwnerCodec.TryEncode(e.JusticeSnapshot,
                out P12AOwnerSection justice, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryEncodeOutcomes(e.CrimeSocialSnapshot,
                out P12AOwnerSection outcomes, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryEncodeKnowledge(e.CrimeSocialSnapshot,
                out P12AOwnerSection crimeKnowledge, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryEncodeReactions(e.CrimeSocialSnapshot,
                out P12AOwnerSection reactions, out result)
            || !P12AReceiptOwnerCodec.TryEncode(e.CrimeReceiptSnapshot, e.JusticeReceiptSnapshot,
                out IReadOnlyList<P12AOwnerSection> receipts, out result)) return false;

        encoded.Add(institutions); encoded.Add(offices); encoded.Add(incumbencies); encoded.Add(tenures);
        encoded.Add(ownership); encoded.Add(transfers); encoded.Add(estates);
        encoded.Add(factions); encoded.Add(affiliations); encoded.Add(claims); encoded.Add(recognitions);
        encoded.Add(politicalSupport); encoded.Add(politicalDecisions); encoded.AddRange(military);
        encoded.AddRange(conflicts); encoded.Add(justice);
        encoded.Add(outcomes); encoded.Add(crimeKnowledge); encoded.Add(reactions);
        encoded.AddRange(receipts);

        if (coreP12FSections != null)
        {
            encoded.AddRange(coreP12FSections);
        }
        else
        {
            if (!P12AFPoliticalKnowledgeCodec.TryEncode(f.PoliticalKnowledgeSnapshot,
                    out P12AOwnerSection politicalKnowledge, out result)
                || !P12AFScheduledDirectiveCodec.TryEncode(f.DirectiveSnapshot,
                    out P12AOwnerSection directives, out result)
                || !P12AActorChoiceCodec.TryEncode(f.ActorChoiceSnapshot,
                    out P12AOwnerSection actorChoices, out result)
                || !P12AFCommitmentOwnerCodec.TryEncodeTravelParties(f.TravelPartySnapshot,
                    out P12AOwnerSection travelParties, out result)
                || !P12AFCommitmentOwnerCodec.TryEncodeExpeditions(f.ExpeditionSnapshot,
                    out P12AOwnerSection expeditions, out result)) return false;
            encoded.Add(politicalKnowledge); encoded.Add(directives); encoded.Add(actorChoices);
            encoded.Add(travelParties); encoded.Add(expeditions);
        }

        encoded.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id, right?.Id));
        for (int i = 0; i < encoded.Count; i++)
        {
            if (encoded[i] == null || string.IsNullOrWhiteSpace(encoded[i].Id)
                || (i > 0 && encoded[i - 1].Id == encoded[i].Id))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, encoded[i]?.Id);
                return false;
            }
        }
        sections = encoded.AsReadOnly();
        result = null;
        return true;
    }
}
