using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Inert typed owner values decoded from one complete Daily-v1 image.</summary>
internal sealed class P12ADailyV1ImageOwnerSnapshots
{
    internal P12ACausalRootImageSnapshots C { get; }
    internal P12CRuntimeIdentityOwnerSnapshot RuntimeIdentities { get; }
    internal SpatialNetworkOwnerSnapshot SpatialNetwork { get; }
    internal PersonStoreOwnerSnapshot Persons { get; }
    internal GenealogyOwnerSnapshot Genealogy { get; }
    internal ExplorableSiteOwnerSnapshot Sites { get; }
    internal IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> Cities { get; }
    internal IReadOnlyList<P12DNpcDRow> NpcDRows { get; }
    internal IReadOnlyList<P12DNpcFRow> NpcFRows { get; }
    internal P12EInstitutionOfficeOwnerSnapshot InstitutionOffice { get; }
    internal PropertyEstateOwnerSnapshot PropertyEstate { get; }
    internal P12EFactionOwnerSnapshot Faction { get; }
    internal P12EPoliticalClaimOwnerSnapshot PoliticalClaims { get; }
    internal P12EPoliticalSupportOwnerSnapshot PoliticalSupport { get; }
    internal P12EPoliticalDecisionOwnerSnapshot PoliticalDecisions { get; }
    internal P12EMilitaryOwnerSnapshot Military { get; }
    internal PersistentConflictOwnerSnapshot Conflicts { get; }
    internal PersistentWarOwnerSnapshot Wars { get; }
    internal PersistentBattleOwnerSnapshot Battles { get; }
    internal P12EJusticeRecordsOwnerSnapshot Justice { get; }
    internal P12ECrimeSocialAppraisalOwnerSnapshot CrimeSocial { get; }
    internal P12ACrimeReceiptOwnerSnapshot CrimeReceipts { get; }
    internal P12AJusticeReceiptOwnerSnapshot JusticeReceipts { get; }
    internal P12FPoliticalKnowledgeOwnerSnapshot PoliticalKnowledge { get; }
    internal P12FScheduledDirectiveOwnerSnapshot Directives { get; }
    internal P12FActorChoiceSnapshot ActorChoices { get; }
    internal P12FTravelPartyOwnerSnapshot TravelParties { get; }
    internal P12FExpeditionOwnerSnapshot Expeditions { get; }

    internal P12ADailyV1ImageOwnerSnapshots(
        P12ACausalRootImageSnapshots c,
        P12CRuntimeIdentityOwnerSnapshot runtimeIdentities,
        SpatialNetworkOwnerSnapshot spatialNetwork,
        PersonStoreOwnerSnapshot persons,
        GenealogyOwnerSnapshot genealogy,
        ExplorableSiteOwnerSnapshot sites,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cities,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EMilitaryOwnerSnapshot military,
        PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars,
        PersistentBattleOwnerSnapshot battles,
        P12EJusticeRecordsOwnerSnapshot justice,
        P12ECrimeSocialAppraisalOwnerSnapshot crimeSocial,
        P12ACrimeReceiptOwnerSnapshot crimeReceipts,
        P12AJusticeReceiptOwnerSnapshot justiceReceipts,
        P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge,
        P12FScheduledDirectiveOwnerSnapshot directives,
        P12FActorChoiceSnapshot actorChoices,
        P12FTravelPartyOwnerSnapshot travelParties,
        P12FExpeditionOwnerSnapshot expeditions)
    {
        C = c; RuntimeIdentities = runtimeIdentities; SpatialNetwork = spatialNetwork;
        Persons = persons; Genealogy = genealogy; Sites = sites; Cities = cities;
        NpcDRows = npcDRows; NpcFRows = npcFRows; InstitutionOffice = institutionOffice;
        PropertyEstate = propertyEstate; Faction = faction; PoliticalClaims = politicalClaims;
        PoliticalSupport = politicalSupport; PoliticalDecisions = politicalDecisions;
        Military = military; Conflicts = conflicts; Wars = wars; Battles = battles;
        Justice = justice; CrimeSocial = crimeSocial; CrimeReceipts = crimeReceipts;
        JusticeReceipts = justiceReceipts; PoliticalKnowledge = politicalKnowledge;
        Directives = directives; ActorChoices = actorChoices; TravelParties = travelParties;
        Expeditions = expeditions;
    }
}

/// <summary>
/// Decodes every selected Daily-v1 owner section into detached DTOs before any
/// live owner factory is called. Section identity and dynamic cardinality are
/// checked by the census, then each family is decoded by its owner codec.
/// </summary>
internal static class P12ADailyV1ImageOwnerDecoder
{
    internal static bool TryDecode(P12AContinuationImage image,
        out P12ADailyV1ImageOwnerSnapshots snapshots, out P12AResult result)
    {
        snapshots = null;
        result = new P12AResult(P12AResultCode.OwnerGraphRejected);
        if (image == null || !P12ADailyV1Census.TryCreateForImage(image, out _, out result)
            || !P12ACausalRootImageCodec.TryDecode(image,
                out P12ACausalRootImageSnapshots c, out result)) return false;

        P12AOwnerSection Section(string id) => Unique(image, id, out P12AResult sectionError)
            ? Find(image, id) : throw new InvalidOperationException(sectionError?.SectionCode ?? id);

        try
        {
            if (!P12ARuntimeIdentityOwnerCodec.TryDecode(new[]
                {
                    Section(DailyV1SelectedOwnerSectionCensusContract.RuntimeIdentityNpcsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.RuntimeIdentityCitiesSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.RuntimeIdentityLocationsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.RuntimeIdentityRoutesSectionId)
                }, out P12CRuntimeIdentityOwnerSnapshot identities, out result)
                || !P12ASpatialNetworkOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.LegacySpatialNetworkLocationsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.LegacySpatialNetworkRoutesSectionId),
                    out SpatialNetworkOwnerSnapshot spatial, out result)
                || !P12APersonStoreOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId),
                    out PersonStoreOwnerSnapshot persons, out result)
                || !P12AGenealogyOwnerCodec.TryDecode(Section(DailyV1SelectedOwnerSectionCensusContract.GenealogySectionId),
                    out GenealogyOwnerSnapshot genealogy, out result)) return false;

            List<P12AOwnerSection> citySections = image.Sections.Where(section =>
                P12ADailyV1Census.CitySectionPrefixes.Any(prefix =>
                    section.Id.StartsWith(prefix, StringComparison.Ordinal))).ToList();
            if (!P12DCityOwnerSetImageDecoder.TryDecode(citySections,
                    out IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cities, out result)) return false;

            P12AOwnerSection[] emptySections = P12ADailyV1Census.FixedEmptySectionIds
                .Select(Section).ToArray();
            if (!TryValidateExactEmptySections(emptySections, out result)) return false;
            P12AOwnerSection siteSection = emptySections.Single(section =>
                section.Id == DailyV1SelectedOwnerSectionCensusContract.ExplorableSitesSectionId);
            ExplorableSiteOwnerSnapshot sites = new ExplorableSiteOwnerSnapshot(
                ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, siteSection.Revision,
                Array.Empty<ExplorableSiteOwnerSnapshotRecord>());

            if (!P12ADailyV1NpcOwnerBatchDecoder.TryDecode(image, identities, persons,
                    out IReadOnlyList<P12DNpcDRow> npcDRows,
                    out IReadOnlyList<P12DNpcFRow> npcFRows, out result)) return false;

            long day = image.Header.Day;
            if (!P12EInstitutionOfficeOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.InstitutionRecordsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.OfficeRecordsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.OfficeIncumbenciesSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.OfficeTenuresSectionId),
                    out P12EInstitutionOfficeOwnerSnapshot institutionOffice, out result)
                || !P12EPropertyEstateOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.PropertyOwnershipSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.PropertyTransferHistorySectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.EstateRecordsSectionId), day,
                    out PropertyEstateOwnerSnapshot propertyEstate, out result)
                || !P12EFactionOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.FactionRecordsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.FactionAffiliationsSectionId), day,
                    out P12EFactionOwnerSnapshot faction, out result)
                || !P12EPoliticalClaimOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.PoliticalClaimRecordsSectionId),
                    Section(DailyV1SelectedOwnerSectionCensusContract.PoliticalClaimRecognitionsSectionId), day,
                    out P12EPoliticalClaimOwnerSnapshot claims, out result)
                || !P12EPoliticalSupportOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.PoliticalSupportRelationsSectionId), day,
                    out P12EPoliticalSupportOwnerSnapshot support, out result)
                || !P12EPoliticalDecisionOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.PoliticalDecisionRecordsSectionId), day,
                    out P12EPoliticalDecisionOwnerSnapshot decisions, out result)
                || !P12EMilitaryOwnerCodec.TryDecode(image.Sections.Where(section =>
                    section.Id.StartsWith("p12e.armed-force.", StringComparison.Ordinal)
                    || section.Id == DailyV1SelectedOwnerSectionCensusContract.ContingentManpowerStatesSectionId
                    || section.Id == DailyV1SelectedOwnerSectionCensusContract.ArmedForceSpatialPositionsSectionId).ToArray(),
                    out P12EMilitaryOwnerSnapshot military, out result)
                || !P12EPersistentConflictWarBattleOwnerCodec.TryDecode(image.Sections.Where(section =>
                    section.Id == DailyV1SelectedOwnerSectionCensusContract.ConflictsSectionId
                    || section.Id == DailyV1SelectedOwnerSectionCensusContract.WarsSectionId
                    || section.Id == DailyV1SelectedOwnerSectionCensusContract.BattlesSectionId).ToArray(),
                    out PersistentConflictOwnerSnapshot conflicts,
                    out PersistentWarOwnerSnapshot wars,
                    out PersistentBattleOwnerSnapshot battles, out result)
                || !P12EJusticeRecordsOwnerCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId), day,
                    out P12EJusticeRecordsOwnerSnapshot justice, out result)
                || !TryDecodeCrimeSocial(image, day,
                    out P12ECrimeSocialAppraisalOwnerSnapshot crimeSocial, out result)
                || !P12AReceiptOwnerCodec.TryDecode(new[]
                    {
                        Section(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId),
                        Section(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId)
                    }, out P12ACrimeReceiptOwnerSnapshot crimeReceipts,
                    out P12AJusticeReceiptOwnerSnapshot justiceReceipts, out result)
                || !P12AFPoliticalKnowledgeCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.PoliticalKnowledgeSectionId),
                    out P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge, out result)
                || !P12AFScheduledDirectiveCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.ScheduledDirectivesSectionId),
                    out P12FScheduledDirectiveOwnerSnapshot directives, out result)
                || !P12AActorChoiceCodec.TryDecode(
                    Section(DailyV1SelectedOwnerSectionCensusContract.ActorChoiceInputsSectionId),
                    out P12FActorChoiceSnapshot actorChoices, out result)
                || !P12AFCommitmentOwnerCodec.TryDecodeTravelParties(
                    Section(DailyV1SelectedOwnerSectionCensusContract.TravelPartiesSectionId),
                    out P12FTravelPartyOwnerSnapshot travelParties, out result)
                || !P12AFCommitmentOwnerCodec.TryDecodeExpeditions(
                    Section(DailyV1SelectedOwnerSectionCensusContract.ExpeditionsSectionId),
                    out P12FExpeditionOwnerSnapshot expeditions, out result)) return false;

            snapshots = new P12ADailyV1ImageOwnerSnapshots(c, identities, spatial, persons,
                genealogy, sites, cities, npcDRows, npcFRows,
                institutionOffice, propertyEstate, faction, claims, support, decisions,
                military, conflicts, wars, battles, justice, crimeSocial, crimeReceipts,
                justiceReceipts, politicalKnowledge, directives, actorChoices,
                travelParties, expeditions);
            result = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            snapshots = null;
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, exception.Message);
            return false;
        }
        catch (ArgumentException)
        {
            snapshots = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }
    }

    private static bool TryDecodeCrimeSocial(P12AContinuationImage image, long day,
        out P12ECrimeSocialAppraisalOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        if (!P12ECrimeSocialAppraisalOwnerCodec.TryDecodeOutcomes(
                Find(image, DailyV1SelectedOwnerSectionCensusContract.CrimeSocialOutcomesSectionId), day,
                out P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> outcomes, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryDecodeKnowledge(
                Find(image, DailyV1SelectedOwnerSectionCensusContract.CrimeSocialKnowledgeSectionId), day,
                out P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> knowledge, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryDecodeReactions(
                Find(image, DailyV1SelectedOwnerSectionCensusContract.CrimeSocialReactionsSectionId), day,
                out P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> reactions, out result))
            return false;
        snapshot = new P12ECrimeSocialAppraisalOwnerSnapshot(day, outcomes, knowledge, reactions);
        result = null;
        return true;
    }

    private static bool TryValidateExactEmptySections(IReadOnlyList<P12AOwnerSection> sections,
        out P12AResult result)
    {
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (sections == null || sections.Count != P12ADailyV1Census.FixedEmptySectionIds.Count)
            return false;
        Dictionary<string, P12AOwnerSection> byId = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in sections)
        {
            if (section == null || !byId.TryAdd(section.Id, section)
                || section.Role != OwnerSectionRole.ExplicitlyEmpty || section.Rows.Count != 0
                || section.Revision < 0L
                || section.AliasGroup != P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id);
                return false;
            }
        }
        if (!new HashSet<string>(byId.Keys, StringComparer.Ordinal)
            .SetEquals(P12ADailyV1Census.FixedEmptySectionIds))
        {
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
            return false;
        }
        result = null;
        return true;
    }

    private static bool Unique(P12AContinuationImage image, string id, out P12AResult result)
    {
        int count = 0;
        foreach (P12AOwnerSection section in image.Sections)
            if (section != null && section.Id == id) count++;
        result = count == 1 ? null : new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
        return count == 1;
    }

    private static P12AOwnerSection Find(P12AContinuationImage image, string id)
    {
        foreach (P12AOwnerSection section in image.Sections)
            if (section != null && section.Id == id) return section;
        return null;
    }
}
