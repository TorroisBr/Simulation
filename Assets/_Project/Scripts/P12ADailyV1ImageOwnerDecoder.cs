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
    internal IReadOnlyList<P12DCityRootOwnerSnapshot> Cities { get; }
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
        IReadOnlyList<P12DCityRootOwnerSnapshot> cities,
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
                    Section(RuntimeIdentityRegistryCensusProvider.NpcsSectionId),
                    Section(RuntimeIdentityRegistryCensusProvider.CitiesSectionId),
                    Section(RuntimeIdentityRegistryCensusProvider.LocationsSectionId),
                    Section(RuntimeIdentityRegistryCensusProvider.RoutesSectionId)
                }, out P12CRuntimeIdentityOwnerSnapshot identities, out result)
                || !P12ASpatialNetworkOwnerCodec.TryDecode(
                    Section(SpatialNetworkCensusProvider.LocationsSectionId),
                    Section(SpatialNetworkCensusProvider.RoutesSectionId),
                    out SpatialNetworkOwnerSnapshot spatial, out result)
                || !P12APersonStoreOwnerCodec.TryDecode(
                    Section(PersonMembershipCensusProvider.SectionId),
                    Section(PersonMaterializationBindingCensusProvider.SectionId),
                    out PersonStoreOwnerSnapshot persons, out result)
                || !P12AGenealogyOwnerCodec.TryDecode(Section(GenealogyCensusProvider.SectionId),
                    out GenealogyOwnerSnapshot genealogy, out result)) return false;

            List<P12AOwnerSection> citySections = image.Sections.Where(section =>
                P12ADailyV1Census.CitySectionPrefixes.Any(prefix =>
                    section.Id.StartsWith(prefix, StringComparison.Ordinal))).ToList();
            if (!P12ACityOwnerSetCodec.TryDecode(citySections,
                    out IReadOnlyList<P12DCityRootOwnerSnapshot> cities, out result)) return false;

            P12AOwnerSection[] emptySections = P12ADailyV1Census.FixedEmptySectionIds
                .Select(Section).ToArray();
            if (!TryValidateExactEmptySections(emptySections, out result)) return false;
            P12AOwnerSection siteSection = emptySections.Single(section =>
                section.Id == ExplorableSiteCensusProvider.SectionId);
            ExplorableSiteOwnerSnapshot sites = new ExplorableSiteOwnerSnapshot(
                ExplorableSiteOwnerSnapshot.CurrentSchemaVersion, siteSection.Revision,
                Array.Empty<ExplorableSiteOwnerSnapshotRecord>());

            if (!P12ADailyV1NpcOwnerBatchDecoder.TryDecode(image, identities, persons,
                    out IReadOnlyList<P12DNpcDRow> npcDRows,
                    out IReadOnlyList<P12DNpcFRow> npcFRows, out result)) return false;

            long day = image.Header.Day;
            if (!P12EInstitutionOfficeOwnerCodec.TryDecode(
                    Section(InstitutionOfficeCensusProvider.InstitutionsSectionId),
                    Section(InstitutionOfficeCensusProvider.OfficesSectionId),
                    Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId),
                    Section(InstitutionOfficeCensusProvider.TenuresSectionId),
                    out P12EInstitutionOfficeOwnerSnapshot institutionOffice, out result)
                || !P12EPropertyEstateOwnerCodec.TryDecode(
                    Section(PropertyOwnershipCensusProvider.OwnershipSectionId),
                    Section(PropertyOwnershipCensusProvider.TransferHistorySectionId),
                    Section(EstateCensusProvider.SectionId), day,
                    out PropertyEstateOwnerSnapshot propertyEstate, out result)
                || !P12EFactionOwnerCodec.TryDecode(
                    Section(FactionStoreCensusProvider.FactionsSectionId),
                    Section(FactionStoreCensusProvider.AffiliationsSectionId), day,
                    out P12EFactionOwnerSnapshot faction, out result)
                || !P12EPoliticalClaimOwnerCodec.TryDecode(
                    Section(PoliticalClaimStoreCensusProvider.ClaimsSectionId),
                    Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId), day,
                    out P12EPoliticalClaimOwnerSnapshot claims, out result)
                || !P12EPoliticalSupportOwnerCodec.TryDecode(
                    Section(PoliticalSupportStoreCensusProvider.RelationsSectionId), day,
                    out P12EPoliticalSupportOwnerSnapshot support, out result)
                || !P12EPoliticalDecisionOwnerCodec.TryDecode(
                    Section(PoliticalDecisionStoreCensusProvider.SectionId), day,
                    out P12EPoliticalDecisionOwnerSnapshot decisions, out result)
                || !P12EMilitaryOwnerCodec.TryDecode(image.Sections.Where(section =>
                    section.Id.StartsWith("p12e.armed-force.", StringComparison.Ordinal)
                    || section.Id == ContingentManpowerCensusProvider.SectionId
                    || section.Id == ArmedForceSpatialCensusProvider.SectionId).ToArray(),
                    out P12EMilitaryOwnerSnapshot military, out result)
                || !P12EPersistentConflictWarBattleOwnerCodec.TryDecode(image.Sections.Where(section =>
                    section.Id == PersistentConflictCensusProvider.SectionId
                    || section.Id == PersistentWarCensusProvider.SectionId
                    || section.Id == PersistentBattleCensusProvider.SectionId).ToArray(),
                    out PersistentConflictOwnerSnapshot conflicts,
                    out PersistentWarOwnerSnapshot wars,
                    out PersistentBattleOwnerSnapshot battles, out result)
                || !P12EJusticeRecordsOwnerCodec.TryDecode(
                    Section(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId), day,
                    out P12EJusticeRecordsOwnerSnapshot justice, out result)
                || !TryDecodeCrimeSocial(image, day,
                    out P12ECrimeSocialAppraisalOwnerSnapshot crimeSocial, out result)
                || !P12AReceiptOwnerCodec.TryDecode(new[]
                    {
                        Section(P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId),
                        Section(P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId)
                    }, out P12ACrimeReceiptOwnerSnapshot crimeReceipts,
                    out P12AJusticeReceiptOwnerSnapshot justiceReceipts, out result)
                || !P12AFPoliticalKnowledgeCodec.TryDecode(
                    Section(PoliticalKnowledgeStoreCensusProvider.SectionId),
                    out P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge, out result)
                || !P12AFScheduledDirectiveCodec.TryDecode(
                    Section(ScheduledDirectiveCensusProvider.SectionId),
                    out P12FScheduledDirectiveOwnerSnapshot directives, out result)
                || !P12AActorChoiceCodec.TryDecode(
                    Section(ActorChoiceP11CensusProvider.SectionId),
                    out P12FActorChoiceSnapshot actorChoices, out result)
                || !P12AFCommitmentOwnerCodec.TryDecodeTravelParties(
                    Section(TravelPartyCensusProvider.SectionId),
                    out P12FTravelPartyOwnerSnapshot travelParties, out result)
                || !P12AFCommitmentOwnerCodec.TryDecodeExpeditions(
                    Section(ExpeditionCensusProvider.SectionId),
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
                Find(image, P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId), day,
                out P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> outcomes, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryDecodeKnowledge(
                Find(image, P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId), day,
                out P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> knowledge, out result)
            || !P12ECrimeSocialAppraisalOwnerCodec.TryDecodeReactions(
                Find(image, P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId), day,
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
