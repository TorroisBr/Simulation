using System;
using System.Collections.Generic;

/// <summary>
/// Creates selected-profile Inventory, MoneyAccount, CommercialKnowledge, and
/// SpatialKnowledge census witnesses directly from the exact Core NPC owners.
/// Unity projections may alias these objects, but they do not select or read
/// persisted owners.
/// </summary>
internal static class DailyV1NpcOwnerCensusProviders
{
    internal interface IInventoryProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        DailyV1NpcRuntimeOwners NpcOwner { get; }
        DailyV1InventoryOwner InventoryOwner { get; }
    }

    internal interface IMoneyAccountProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        DailyV1NpcRuntimeOwners NpcOwner { get; }
        MoneyAccountRuntime MoneyAccountOwner { get; }
    }

    internal interface ISpatialKnowledgeProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        DailyV1NpcRuntimeOwners NpcOwner { get; }
        SpatialKnowledgeRuntime SpatialKnowledgeOwner { get; }
    }

    private enum SpatialKnowledgeSection
    {
        Locations,
        Routes
    }

    private sealed class SpatialKnowledgeSectionProvider : ISpatialKnowledgeProvider
    {
        private readonly SpatialKnowledgeSection section;
        private readonly string sectionId;

        internal SpatialKnowledgeSectionProvider(
            DailyV1NpcRuntimeOwners npcOwner,
            SpatialKnowledgeRuntime owner,
            SpatialKnowledgeSection section,
            string sectionId)
        {
            NpcOwner = npcOwner;
            SpatialKnowledgeOwner = owner;
            this.section = section;
            this.sectionId = sectionId;
        }

        public string RuntimeId => NpcOwner.RuntimeId;
        public DailyV1NpcRuntimeOwners NpcOwner { get; }
        public SpatialKnowledgeRuntime SpatialKnowledgeOwner { get; }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == SpatialKnowledgeSection.Locations
                ? SpatialKnowledgeOwner.KnownLocationCount
                : SpatialKnowledgeOwner.KnownRouteCount;
            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                SpatialKnowledgeOwner,
                cardinality,
                SpatialKnowledgeOwner.Revision);
        }
    }

    private sealed class InventorySectionProvider : IInventoryProvider
    {
        private readonly string sectionId;

        internal InventorySectionProvider(DailyV1NpcRuntimeOwners owner)
        {
            NpcOwner = owner;
            sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + owner.RuntimeId;
        }

        public string RuntimeId => NpcOwner.RuntimeId;
        public DailyV1NpcRuntimeOwners NpcOwner { get; }
        public DailyV1InventoryOwner InventoryOwner => NpcOwner.Inventory;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (!InventoryOwner.TryGetCensusCardinality(out int cardinality))
                throw new InvalidOperationException("The Core Inventory owner cannot provide its census cardinality.");
            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                InventoryOwner,
                cardinality,
                InventoryOwner.Revision);
        }
    }

    private sealed class MoneyAccountSectionProvider : IMoneyAccountProvider
    {
        private readonly string sectionId;

        internal MoneyAccountSectionProvider(DailyV1NpcRuntimeOwners owner)
        {
            NpcOwner = owner;
            sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + owner.RuntimeId;
        }

        public string RuntimeId => NpcOwner.RuntimeId;
        public DailyV1NpcRuntimeOwners NpcOwner { get; }
        public MoneyAccountRuntime MoneyAccountOwner => NpcOwner.MoneyAccount;

        public OwnerSectionCensusWitness GetCurrentCensus() => new OwnerSectionCensusWitness(
            sectionId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            MoneyAccountOwner,
            1,
            MoneyAccountOwner.Revision);
    }

    private enum CommercialKnowledgeSection
    {
        Markets,
        Liquidity,
        ShareReceipts
    }

    private sealed class CommercialKnowledgeSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1CommercialKnowledgeOwner owner;
        private readonly CommercialKnowledgeSection section;
        private readonly string sectionId;

        internal CommercialKnowledgeSectionProvider(
            DailyV1NpcRuntimeOwners npcOwner,
            DailyV1CommercialKnowledgeOwner owner,
            CommercialKnowledgeSection section)
        {
            NpcOwner = npcOwner;
            this.owner = owner;
            this.section = section;
            sectionId = SectionPrefix(section) + npcOwner.RuntimeId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (!owner.TryReadCensus(
                    out int markets,
                    out int liquidity,
                    out int receipts,
                    out long revision))
                throw new InvalidOperationException(
                    "The Core CommercialKnowledge owner cannot provide its census.");

            int cardinality = section switch
            {
                CommercialKnowledgeSection.Markets => markets,
                CommercialKnowledgeSection.Liquidity => liquidity,
                CommercialKnowledgeSection.ShareReceipts => receipts,
                _ => throw new InvalidOperationException(
                    "The CommercialKnowledge census section is invalid.")
            };
            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                owner,
                cardinality,
                revision);
        }

        private static string SectionPrefix(CommercialKnowledgeSection section) => section switch
        {
            CommercialKnowledgeSection.Markets =>
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix,
            CommercialKnowledgeSection.Liquidity =>
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix,
            CommercialKnowledgeSection.ShareReceipts =>
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix,
            _ => throw new ArgumentOutOfRangeException(nameof(section))
        };

        internal DailyV1NpcRuntimeOwners NpcOwner { get; }
    }

    private enum KnowledgePlanSection
    {
        ExplorableSite,
        LocalTopologyPlaces,
        LocalTopologyConnections,
        AdventureOpposition,
        AdventureNotableItems,
        AdventureCommonResources,
        AdventureAccess,
        MerchantTradePlan,
        TravelPlan
    }

    private sealed class KnowledgePlanSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1NpcRuntimeOwners npcOwner;
        private readonly KnowledgePlanSection section;
        private readonly string sectionId;

        internal KnowledgePlanSectionProvider(
            DailyV1NpcRuntimeOwners npcOwner,
            KnowledgePlanSection section)
        {
            this.npcOwner = npcOwner ?? throw new ArgumentNullException(nameof(npcOwner));
            this.section = section;
            sectionId = SectionIdFor(section, npcOwner.RuntimeId);
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            object owner;
            int cardinality;
            long revision;
            switch (section)
            {
                case KnowledgePlanSection.ExplorableSite:
                    DailyV1ExplorableSiteKnowledgeOwner explorable = npcOwner.ExplorableSiteKnowledge;
                    if (explorable == null
                        || !string.Equals(explorable.OwnerRuntimeId, npcOwner.RuntimeId, StringComparison.Ordinal)
                        || explorable.Observations == null
                        || explorable.Revision < 0L)
                        throw new InvalidOperationException(
                            "The Core ExplorableSiteKnowledge owner cannot provide a valid census witness.");
                    owner = explorable;
                    cardinality = explorable.Observations.Count;
                    revision = explorable.Revision;
                    break;

                case KnowledgePlanSection.LocalTopologyPlaces:
                case KnowledgePlanSection.LocalTopologyConnections:
                    DailyV1NpcLocalTopologyKnowledgeEmptyOwner topology =
                        npcOwner.LocalTopologyKnowledgeEmptyOwner;
                    if (topology == null || !topology.MatchesRuntimeId(npcOwner.RuntimeId))
                        throw new InvalidOperationException(
                            "The Core exact-empty LocalTopology owner cannot provide a valid census witness.");
                    owner = topology;
                    cardinality = section == KnowledgePlanSection.LocalTopologyPlaces
                        ? topology.PlaceObservationCount
                        : topology.ConnectionObservationCount;
                    revision = topology.Revision;
                    break;

                case KnowledgePlanSection.AdventureOpposition:
                case KnowledgePlanSection.AdventureNotableItems:
                case KnowledgePlanSection.AdventureCommonResources:
                case KnowledgePlanSection.AdventureAccess:
                    DailyV1AdventureSiteIntelKnowledgeOwner adventure =
                        npcOwner.AdventureSiteIntelKnowledge;
                    if (adventure == null
                        || !string.Equals(adventure.OwnerRuntimeId, npcOwner.RuntimeId, StringComparison.Ordinal)
                        || adventure.Revision < 0L)
                        throw new InvalidOperationException(
                            "The Core AdventureSiteIntel owner cannot provide a valid census witness.");
                    owner = adventure;
                    cardinality = section switch
                    {
                        KnowledgePlanSection.AdventureOpposition => adventure.OppositionObservations.Count,
                        KnowledgePlanSection.AdventureNotableItems => adventure.NotableItemObservations.Count,
                        KnowledgePlanSection.AdventureCommonResources => adventure.CommonResourceObservations.Count,
                        KnowledgePlanSection.AdventureAccess => adventure.AccessObservations.Count,
                        _ => throw new InvalidOperationException("The Core AdventureSiteIntel census section is invalid.")
                    };
                    revision = adventure.Revision;
                    break;

                case KnowledgePlanSection.MerchantTradePlan:
                    DailyV1NpcMerchantTradePlanOwner merchantPlan = npcOwner.MerchantTradePlan;
                    if (merchantPlan == null
                        || !ReferenceEquals(merchantPlan.Definitions, npcOwner.Definitions)
                        || merchantPlan.Revision < 0L)
                        throw new InvalidOperationException(
                            "The Core MerchantTradePlan owner cannot provide a valid census witness.");
                    owner = merchantPlan;
                    cardinality = 1;
                    revision = merchantPlan.Revision;
                    break;

                case KnowledgePlanSection.TravelPlan:
                    DailyV1NpcTravelPlanOwner travelPlan = npcOwner.TravelPlan;
                    if (travelPlan == null || travelPlan.Revision < 0L)
                        throw new InvalidOperationException(
                            "The Core TravelPlan owner cannot provide a valid census witness.");
                    owner = travelPlan;
                    cardinality = 1;
                    revision = travelPlan.Revision;
                    break;

                default:
                    throw new InvalidOperationException("The Core Knowledge and Plan census section is unsupported.");
            }

            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                owner,
                cardinality,
                revision);
        }

        private static string SectionIdFor(KnowledgePlanSection section, string runtimeId) => section switch
        {
            KnowledgePlanSection.ExplorableSite =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(0, runtimeId),
            KnowledgePlanSection.LocalTopologyPlaces =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(1, runtimeId),
            KnowledgePlanSection.LocalTopologyConnections =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(2, runtimeId),
            KnowledgePlanSection.AdventureOpposition =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(3, runtimeId),
            KnowledgePlanSection.AdventureNotableItems =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(4, runtimeId),
            KnowledgePlanSection.AdventureCommonResources =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(5, runtimeId),
            KnowledgePlanSection.AdventureAccess =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(6, runtimeId),
            KnowledgePlanSection.MerchantTradePlan =>
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind, runtimeId),
            KnowledgePlanSection.TravelPlan =>
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, runtimeId),
            _ => throw new ArgumentOutOfRangeException(nameof(section))
        };
    }
    private enum RuntimeAuthoritySection
    {
        CurrentAction,
        LifeState,
        Residence,
        StatusCrime,
        TravelState
    }

    private sealed class RuntimeAuthoritySectionProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1NpcRuntimeOwners owner;
        private readonly RuntimeAuthoritySection section;
        private readonly string sectionId;

        internal RuntimeAuthoritySectionProvider(
            DailyV1NpcRuntimeOwners owner,
            RuntimeAuthoritySection section)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            sectionId = SectionIdFor(section, owner.RuntimeId);
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality;
            long revision;
            switch (section)
            {
                case RuntimeAuthoritySection.CurrentAction:
                    if (owner.ActionSlot == null
                        || owner.ActionSlot.Revision < 0L
                        || !owner.ActionSlot.TryCapturePayload(out _))
                        throw new InvalidOperationException(
                            "The Core current-action owner cannot provide a valid census witness.");
                    cardinality = owner.ActionSlot.CurrentAction == null ? 0 : 1;
                    revision = owner.ActionSlot.Revision;
                    break;

                case RuntimeAuthoritySection.LifeState:
                    if (owner.Lifecycle == null
                        || !NpcInjuryRules.IsValid(owner.Lifecycle.LifeState)
                        || !NpcInjuryRules.IsValid(owner.Lifecycle.InjurySeverity)
                        || owner.Lifecycle.LifeStateRevision < 0L)
                        throw new InvalidOperationException(
                            "The Core lifecycle owner cannot provide a valid census witness.");
                    cardinality = 1;
                    revision = owner.Lifecycle.LifeStateRevision;
                    break;

                case RuntimeAuthoritySection.Residence:
                    if (owner.Residence == null
                        || !string.Equals(owner.Residence.OwnerRuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                        || owner.Residence.Revision < 0L)
                        throw new InvalidOperationException(
                            "The Core residence owner cannot provide a valid census witness.");
                    cardinality = 1;
                    revision = owner.Residence.Revision;
                    break;

                case RuntimeAuthoritySection.StatusCrime:
                    if (owner.Status == null || owner.CrimeJusticeRevision < 0L)
                        throw new InvalidOperationException(
                            "The Core status owner cannot provide a valid census witness.");
                    cardinality = 1;
                    revision = owner.CrimeJusticeRevision;
                    break;

                case RuntimeAuthoritySection.TravelState:
                    if (owner.Travel == null
                        || !string.Equals(owner.Travel.NpcRuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                        || owner.Travel.Revision < 0L)
                        throw new InvalidOperationException(
                            "The Core travel owner cannot provide a valid census witness.");
                    cardinality = 1;
                    revision = owner.Travel.Revision;
                    break;

                default:
                    throw new InvalidOperationException("The Core NPC census section is unsupported.");
            }

            return new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                owner,
                cardinality,
                revision);
        }

        private static string SectionIdFor(RuntimeAuthoritySection section, string runtimeId) => section switch
        {
            RuntimeAuthoritySection.CurrentAction =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId),
            RuntimeAuthoritySection.LifeState =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(21, runtimeId),
            RuntimeAuthoritySection.Residence =>
                DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(runtimeId),
            RuntimeAuthoritySection.StatusCrime =>
                DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(runtimeId),
            RuntimeAuthoritySection.TravelState =>
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, runtimeId),
            _ => throw new ArgumentOutOfRangeException(nameof(section))
        };
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateKnowledgeAndPlanProviders(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null || graph.Definitions == null)
            throw new ArgumentNullException(nameof(graph));
        ValidateRoster(graph.Npcs);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(graph.Npcs);
        HashSet<DailyV1ExplorableSiteKnowledgeOwner> explorableOwners =
            new HashSet<DailyV1ExplorableSiteKnowledgeOwner>();
        HashSet<DailyV1NpcLocalTopologyKnowledgeEmptyOwner> topologyOwners =
            new HashSet<DailyV1NpcLocalTopologyKnowledgeEmptyOwner>();
        HashSet<DailyV1AdventureSiteIntelKnowledgeOwner> adventureOwners =
            new HashSet<DailyV1AdventureSiteIntelKnowledgeOwner>();
        HashSet<DailyV1NpcMerchantTradePlanOwner> merchantPlanOwners =
            new HashSet<DailyV1NpcMerchantTradePlanOwner>();
        HashSet<DailyV1NpcTravelPlanOwner> travelPlanOwners =
            new HashSet<DailyV1NpcTravelPlanOwner>();
        IOwnerSectionCensusProvider[] providers =
            new IOwnerSectionCensusProvider[ordered.Count * 9];
        int providerIndex = 0;
        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            if (!ReferenceEquals(npc.Definitions, graph.Definitions)
                || npc.ExplorableSiteKnowledge == null
                || !string.Equals(npc.ExplorableSiteKnowledge.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !explorableOwners.Add(npc.ExplorableSiteKnowledge)
                || npc.LocalTopologyKnowledgeEmptyOwner == null
                || !npc.LocalTopologyKnowledgeEmptyOwner.MatchesRuntimeId(npc.RuntimeId)
                || !topologyOwners.Add(npc.LocalTopologyKnowledgeEmptyOwner)
                || npc.AdventureSiteIntelKnowledge == null
                || !string.Equals(npc.AdventureSiteIntelKnowledge.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !adventureOwners.Add(npc.AdventureSiteIntelKnowledge)
                || npc.MerchantTradePlan == null
                || !ReferenceEquals(npc.MerchantTradePlan.Definitions, graph.Definitions)
                || !merchantPlanOwners.Add(npc.MerchantTradePlan)
                || npc.TravelPlan == null
                || !travelPlanOwners.Add(npc.TravelPlan))
                throw new InvalidOperationException(
                    "Selected NPC Knowledge and Plan census requires distinct exact owners bound to the admitted runtime graph.");

            for (int familyIndex = 0; familyIndex <= 6; familyIndex++)
            {
                providers[providerIndex++] = new KnowledgePlanSectionProvider(
                    npc,
                    (KnowledgePlanSection)familyIndex);
            }
            providers[providerIndex++] = new KnowledgePlanSectionProvider(
                npc, KnowledgePlanSection.MerchantTradePlan);
            providers[providerIndex++] = new KnowledgePlanSectionProvider(
                npc, KnowledgePlanSection.TravelPlan);
        }
        return Array.AsReadOnly(providers);
    }
    /// <summary>
    /// Creates the selected runtime-state witnesses from the exact Core owner
    /// graph. The five P12 alias-family sections share the aggregate identity;
    /// Person-backed NPC residence remains solely in PersonStore.
    /// </summary>
    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateRuntimeAuthorityProviders(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (graph.PersonStore == null)
            throw new InvalidOperationException(
                "Selected NPC runtime-state census requires the bound Core PersonStore.");

        ValidateRoster(graph.Npcs);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(graph.Npcs);
        HashSet<DailyV1NpcRuntimeOwners> ownerIdentities =
            new HashSet<DailyV1NpcRuntimeOwners>();
        List<IOwnerSectionCensusProvider> providers =
            new List<IOwnerSectionCensusProvider>(ordered.Count * 5);
        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            if (!ReferenceEquals(npc.Definitions, graph.Definitions)
                || !ownerIdentities.Add(npc))
                throw new InvalidOperationException(
                    "Selected NPC runtime-state census requires exact, distinct owners from the admitted definition graph.");

            providers.Add(new RuntimeAuthoritySectionProvider(
                npc, RuntimeAuthoritySection.CurrentAction));
            providers.Add(new RuntimeAuthoritySectionProvider(
                npc, RuntimeAuthoritySection.LifeState));
            providers.Add(new RuntimeAuthoritySectionProvider(
                npc, RuntimeAuthoritySection.StatusCrime));
            providers.Add(new RuntimeAuthoritySectionProvider(
                npc, RuntimeAuthoritySection.TravelState));

            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime person))
            {
                if (person == null
                    || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The Core PersonStore returned an inconsistent NPC materialization binding.");
            }
            else
            {
                providers.Add(new RuntimeAuthoritySectionProvider(
                    npc, RuntimeAuthoritySection.Residence));
            }
        }

        return providers.AsReadOnly();
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateInventoryProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<DailyV1InventoryOwner> owners = new HashSet<DailyV1InventoryOwner>();
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            DailyV1NpcRuntimeOwners npc = ordered[i];
            if (npc.Inventory == null || !ReferenceEquals(npc.Inventory.Definitions, npc.Definitions)
                || !owners.Add(npc.Inventory))
                throw new InvalidOperationException("Selected Inventory census requires one distinct admitted Core owner per NPC.");
            providers[i] = new InventorySectionProvider(npc);
        }
        return Array.AsReadOnly(providers);
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateMoneyAccountProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<MoneyAccountRuntime> owners = new HashSet<MoneyAccountRuntime>();
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            DailyV1NpcRuntimeOwners npc = ordered[i];
            if (npc.MoneyAccount == null || !owners.Add(npc.MoneyAccount))
                throw new InvalidOperationException("Selected MoneyAccount census requires one distinct Core owner per NPC.");
            providers[i] = new MoneyAccountSectionProvider(npc);
        }
        return Array.AsReadOnly(providers);
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateCommercialKnowledgeProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<DailyV1CommercialKnowledgeOwner> owners =
            new HashSet<DailyV1CommercialKnowledgeOwner>();
        IOwnerSectionCensusProvider[] providers =
            new IOwnerSectionCensusProvider[ordered.Count * 3];
        int providerIndex = 0;
        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            DailyV1CommercialKnowledgeOwner owner = npc.CommercialKnowledge;
            if (owner == null
                || !ReferenceEquals(owner.Definitions, npc.Definitions)
                || !owners.Add(owner))
                throw new InvalidOperationException(
                    "Selected CommercialKnowledge census requires one distinct admitted Core owner per NPC.");

            providers[providerIndex++] = new CommercialKnowledgeSectionProvider(
                npc, owner, CommercialKnowledgeSection.Markets);
            providers[providerIndex++] = new CommercialKnowledgeSectionProvider(
                npc, owner, CommercialKnowledgeSection.Liquidity);
            providers[providerIndex++] = new CommercialKnowledgeSectionProvider(
                npc, owner, CommercialKnowledgeSection.ShareReceipts);
        }
        return Array.AsReadOnly(providers);
    }

    internal static IReadOnlyList<IOwnerSectionCensusProvider> CreateSpatialKnowledgeProviders(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        ValidateRoster(roster);
        List<DailyV1NpcRuntimeOwners> ordered = OrderByRuntimeId(roster);
        HashSet<SpatialKnowledgeRuntime> owners = new HashSet<SpatialKnowledgeRuntime>();
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count * 2];
        int providerIndex = 0;
        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            SpatialKnowledgeRuntime owner = npc.SpatialKnowledge;
            if (owner == null
                || !string.Equals(owner.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !owners.Add(owner))
                throw new InvalidOperationException(
                    "Selected SpatialKnowledge census requires one distinct owner bound to each NPC RuntimeId.");

            providers[providerIndex++] = new SpatialKnowledgeSectionProvider(
                npc,
                owner,
                SpatialKnowledgeSection.Locations,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + npc.RuntimeId);
            providers[providerIndex++] = new SpatialKnowledgeSectionProvider(
                npc,
                owner,
                SpatialKnowledgeSection.Routes,
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + npc.RuntimeId);
        }
        return Array.AsReadOnly(providers);
    }

    private static void ValidateRoster(IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in roster)
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !runtimeIds.Add(npc.RuntimeId))
                throw new InvalidOperationException("Selected owner census requires unique NPC runtime identities.");
    }

    private static List<DailyV1NpcRuntimeOwners> OrderByRuntimeId(
        IReadOnlyList<DailyV1NpcRuntimeOwners> roster)
    {
        List<DailyV1NpcRuntimeOwners> ordered = new List<DailyV1NpcRuntimeOwners>(roster);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        return ordered;
    }
}
