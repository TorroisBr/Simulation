using NUnit.Framework;

public sealed partial class DailyV1DefinitionIdentityTests
{
    // Verbatim data-only census groups copied from the P12 production source
    // at 8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f; the expected digest is
    // independently retained in the Unity baseline fixture.
    [Test]
    public void ProviderAndCalendarFingerprintsMatchUnityBaseline()
    {
        Assert.That(DailyV1DefinitionIdentity.CreateProviderFingerprint(
            FixedRequired, FixedEmpty, NpcPrefixes, CityPrefixes),
            Is.EqualTo("5de459e6a4dcb442baf91d6fc31cf3a5f416f20bc4365c780527ffa33b93a8ee"));
        Assert.That(DailyV1DefinitionIdentity.CreateCalendarFingerprint(
            new DailyV1CalendarDefinition(1, 1, 1, null)),
            Is.EqualTo("c4cbcdbb1fa3e79478b09fa602c9eb149edb56b3cf1f343cc82daff59650bd55"));
    }
    private static readonly string[] FixedRequired =
    {
        "p12d.person.membership",
        "p12d.person.materialization-binding",
        "p12c.runtime-identities.npcs",
        "p12c.runtime-identities.cities",
        "p12c.runtime-identities.locations",
        "p12c.runtime-identities.routes",
        "p12d.legacy-spatial-network.locations",
        "p12d.legacy-spatial-network.routes",
        "p8a.hexes",
        "p8a.locations",
        "p8a.scale-context",
        "p12c.runtime-id-allocator.events",
        "p12c.runtime-id-allocator.decisions",
        "p12c.runtime-id-allocator.travel-parties",
        "p12c.simulation-record-sequence",
        "p12d.genealogy.parentage",
        "p12f.political-knowledge.holders",
        "p12f.political-decisions.records",
        "p12.crime-social-appraisal.outcomes",
        "p12.crime-social-appraisal.knowledge",
        "p12.crime-social-appraisal.reactions",
        "p12b.justice-records",
        "p12b.crime-p18-receipts",
        "p12b.justice-p18-receipts",
        "p12e.armed-force.forces",
        "p12e.armed-force.contingents",
        "p12e.armed-force.relevant-person-references",
        "p12e.contingent-manpower.states",
        "p12e.armed-force-spatial.positions",
        "p12e.conflicts",
        "p12e.wars",
        "p12e.battles",
        "p12e.institution.records",
        "p12e.office.records",
        "p12e.office.incumbencies",
        "p12e.office.tenures",
        "p12e.faction.records",
        "p12e.faction.affiliations",
        "p12e.political-claim.records",
        "p12e.political-claim.recognitions",
        "p12e.political-support.relations",
        "p12e.property.ownership",
        "p12e.property.transfer-history",
        "p12e.estate.records",
        "p12f.actor-choice-inputs",
        "p12f.scheduled-directives",
        "p12f.travel-parties",
        "p12f.expeditions",
    };

    private static readonly string[] FixedEmpty =
    {
        "p12c.runtime-identities.explorable-sites",
        "p12c.runtime-identities.local-places",
        "p12c.runtime-identities.local-connections",
        "p12c.runtime-identities.notable-items",
        "p12d.explorable-sites",
        "p8b.passage-option-barrier-state",
        "p8b.crossings",
        "p8c.city-site-location-bindings",
        "p8c.person-positions",
        "p8d.spatial-route-observations",
        "p8d.person-route-plan-history",
        "p12f.npc-decision-occurrence-receipts",
        "p12e.economy-keyed-sale-receipts",
    };

    private static readonly string[] NpcPrefixes =
    {
        "p12f.explorable-site-knowledge/",
        "p12f.local-topology-knowledge.places/",
        "p12f.local-topology-knowledge.connections/",
        "p12f.adventure-intel.opposition/",
        "p12f.adventure-intel.notable-items/",
        "p12f.adventure-intel.common-resources/",
        "p12f.adventure-intel.access/",
        "p12f.commercial-knowledge.markets/",
        "p12f.commercial-knowledge.liquidity/",
        "p12f.commercial-knowledge.share-receipts/",
        "p12f.spatial-knowledge.locations/",
        "p12f.spatial-knowledge.routes/",
        "p12f.npc-travel-state/",
        "p12f.inventory/",
        "p12e.npc-money-account/",
        "p12b.merchant-trade-plan/",
        "p12b.npc-travel-plan/",
        "p12d.npc-local-observation-receipts/",
        "p12d.npc-merchant-trade-state-receipts/",
        "p12b.npc-status-crime-state/",
        "p12b.npc-current-action/",
        "p12b.npc-life-state/",
    };

    private static readonly string[] CityPrefixes =
    {
        "p12b.city.important-npcs/",
        "p12e.city-market-stock-rows/",
        "p12d.city-population.aggregate/",
        "p12d.city-population.operation-receipts/",
    };
}

