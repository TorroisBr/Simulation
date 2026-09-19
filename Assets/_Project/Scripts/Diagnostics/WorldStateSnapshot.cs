using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class WorldStateSnapshotContext
{
    public SimulationTime SimulationTime { get; }
    public SimulationCalendar Calendar { get; }
    public IEnumerable<NpcRuntime> Npcs { get; }
    public IEnumerable<CityRuntime> Cities { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public ExplorableSiteStore ExplorableSiteStore { get; }
    public ExpeditionStore ExpeditionStore { get; }
    public PlaceContentStore PlaceContentStore { get; }
    public LocalTopologyStore LocalTopologyStore { get; }
    public PersonStore PersonStore { get; }
    public IEnumerable<ParentageRecord> Parentages { get; }
    public GenealogyStore GenealogyStore { get; }
    public PropertyOwnershipStore PropertyOwnershipStore { get; }
    public EstateStore EstateStore { get; }

    public WorldStateSnapshotContext(
        SimulationTime simulationTime = null,
        IEnumerable<NpcRuntime> npcs = null,
        IEnumerable<CityRuntime> cities = null,
        SpatialNetworkRuntime spatialNetwork = null,
        ExplorableSiteStore explorableSiteStore = null,
        ExpeditionStore expeditionStore = null,
        PlaceContentStore placeContentStore = null,
        LocalTopologyStore localTopologyStore = null,
        SimulationCalendar calendar = null,
        CalendarDefinition calendarDefinition = null,
        PersonStore personStore = null,
        IEnumerable<ParentageRecord> parentages = null,
        GenealogyStore genealogyStore = null,
        PropertyOwnershipStore propertyOwnershipStore = null,
        EstateStore estateStore = null)
    {
        SimulationTime = simulationTime;
        Calendar = calendar ?? (calendarDefinition != null ? new SimulationCalendar(calendarDefinition) : null);
        Npcs = npcs ?? Array.Empty<NpcRuntime>();
        Cities = cities ?? Array.Empty<CityRuntime>();
        SpatialNetwork = spatialNetwork;
        ExplorableSiteStore = explorableSiteStore;
        ExpeditionStore = expeditionStore;
        PlaceContentStore = placeContentStore;
        LocalTopologyStore = localTopologyStore;
        PersonStore = personStore;
        GenealogyStore = genealogyStore;
        PropertyOwnershipStore = propertyOwnershipStore;
        EstateStore = estateStore;
        Parentages = parentages
            ?? genealogyStore?.Records
            ?? Array.Empty<ParentageRecord>();
    }
}

public sealed class WorldStateSnapshot
{
    public WorldStateSnapshotMetadata Metadata { get; }
    public long AbsoluteDay => Metadata.AbsoluteDay;
    public WorldStateCalendarSnapshot Calendar => Metadata.CalendarDate;
    public IReadOnlyList<WorldStateNpcSnapshot> Npcs { get; }
    public IReadOnlyList<WorldStateCitySnapshot> Cities { get; }
    public IReadOnlyList<WorldStateCitySnapshot> Settlements => Cities;
    public int SettlementCount => Cities.Count;
    public int KnownNpcCount => Npcs.Count;
    public IReadOnlyList<WorldStatePersonSnapshot> Persons { get; }
    public int PersonCount => Persons.Count;
    public IReadOnlyList<WorldStateParentageSnapshot> Parentages { get; }
    public int ParentageCount => Parentages.Count;
    public IReadOnlyList<WorldStatePropertyOwnershipSnapshot> PropertyOwnerships { get; }
    public int PropertyOwnershipCount => PropertyOwnerships.Count;
    public IReadOnlyList<WorldStatePropertyTransferSnapshot> PropertyTransfers { get; }
    public int PropertyTransferCount => PropertyTransfers.Count;
    public IReadOnlyList<WorldStateEstateSnapshot> Estates { get; }
    public int EstateCount => Estates.Count;
    public WorldStateSpatialSnapshot Spatial { get; }
    public IReadOnlyList<WorldStateSiteSnapshot> Sites { get; }
    public IReadOnlyList<WorldStateExpeditionSnapshot> Expeditions { get; }
    public IReadOnlyList<WorldStatePlaceContentSnapshot> PlaceContents { get; }
    public IReadOnlyList<WorldStateNotableItemSnapshot> NotableItems { get; }
    public IReadOnlyList<WorldStateLocalTopologySnapshot> LocalTopologies { get; }

    public WorldStateSnapshot(
        long absoluteDay,
        IEnumerable<WorldStateNpcSnapshot> npcs = null,
        IEnumerable<WorldStateCitySnapshot> cities = null,
        WorldStateSpatialSnapshot spatial = null,
        IEnumerable<WorldStateSiteSnapshot> sites = null,
        IEnumerable<WorldStateExpeditionSnapshot> expeditions = null,
        IEnumerable<WorldStatePlaceContentSnapshot> placeContents = null,
        IEnumerable<WorldStateNotableItemSnapshot> notableItems = null,
        IEnumerable<WorldStateLocalTopologySnapshot> localTopologies = null,
        WorldStateCalendarSnapshot calendarDate = null,
        IEnumerable<WorldStatePersonSnapshot> persons = null,
        IEnumerable<WorldStateParentageSnapshot> parentages = null,
        IEnumerable<WorldStatePropertyOwnershipSnapshot> propertyOwnerships = null,
        IEnumerable<WorldStateEstateSnapshot> estates = null,
        IEnumerable<WorldStatePropertyTransferSnapshot> propertyTransfers = null)
    {
        Metadata = new WorldStateSnapshotMetadata(absoluteDay, calendarDate);
        Npcs = SnapshotCollections.CopySorted(npcs, npc => npc?.RuntimeId);
        Cities = SnapshotCollections.CopySorted(cities, city => city?.RuntimeId);
        Spatial = spatial ?? new WorldStateSpatialSnapshot();
        Sites = SnapshotCollections.CopySorted(sites, site => site?.RuntimeId);
        Expeditions = SnapshotCollections.CopySorted(expeditions, expedition => expedition?.ExpeditionId);
        PlaceContents = SnapshotCollections.CopySorted(placeContents, content => content?.StableKey);
        NotableItems = SnapshotCollections.CopySorted(notableItems, notable => notable?.RuntimeId);
        LocalTopologies = SnapshotCollections.CopySorted(localTopologies, topology => topology?.StableKey);
        Persons = SnapshotCollections.CopySorted(persons, person => person?.PersonId);
        Parentages = SortParentages(parentages);
        PropertyOwnerships = SnapshotCollections.CopySorted(
            propertyOwnerships,
            ownership => ownership?.PropertyId);
        PropertyTransfers = SnapshotCollections.CopySorted(
            propertyTransfers,
            transfer => transfer == null
                ? null
                : transfer.PropertyId + "\u001f"
                    + transfer.TransferAbsoluteDay.ToString(CultureInfo.InvariantCulture)
                    + "\u001f"
                    + transfer.PreviousOwnerPersonId
                    + "\u001f"
                    + transfer.NewOwnerPersonId);
        Estates = SnapshotCollections.CopySorted(estates, estate => estate?.EstateId);
    }

    private static IReadOnlyList<WorldStateParentageSnapshot> SortParentages(
        IEnumerable<WorldStateParentageSnapshot> source)
    {
        List<WorldStateParentageSnapshot> result = new List<WorldStateParentageSnapshot>();
        if (source != null)
        {
            foreach (WorldStateParentageSnapshot parentage in source)
            {
                if (parentage != null)
                {
                    result.Add(parentage);
                }
            }
        }

        result.Sort((left, right) =>
        {
            int parent = StringComparer.Ordinal.Compare(left.ParentPersonId, right.ParentPersonId);
            return parent != 0
                ? parent
                : StringComparer.Ordinal.Compare(left.ChildPersonId, right.ChildPersonId);
        });
        return result.AsReadOnly();
    }
}

public sealed class WorldStatePersonSnapshot
{
    public string PersonId { get; }
    public long? BirthAbsoluteDay { get; }
    public bool HasKnownBirthDay => BirthAbsoluteDay.HasValue;
    public long? DeathAbsoluteDay { get; }
    public long? AgeInDays { get; }
    public long? CompletedYears { get; }
    public string ResidenceSettlementRuntimeId { get; }
    public string MaterializedNpcRuntimeId { get; }
    public bool IsMaterialized => string.IsNullOrWhiteSpace(MaterializedNpcRuntimeId) == false;

    public WorldStatePersonSnapshot(
        string personId,
        string materializedNpcRuntimeId)
        : this(personId, null, null, null, null, materializedNpcRuntimeId, null)
    {
    }

    public WorldStatePersonSnapshot(
        string personId,
        long? birthAbsoluteDay,
        long? ageInDays,
        long? completedYears,
        string materializedNpcRuntimeId)
        : this(
            personId,
            birthAbsoluteDay,
            ageInDays,
            completedYears,
            null,
            materializedNpcRuntimeId,
            null)
    {
    }

    public WorldStatePersonSnapshot(
        string personId,
        long? birthAbsoluteDay,
        long? ageInDays,
        long? completedYears,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId)
        : this(
            personId,
            birthAbsoluteDay,
            ageInDays,
            completedYears,
            residenceSettlementRuntimeId,
            materializedNpcRuntimeId,
            null)
    {
    }

    public WorldStatePersonSnapshot(
        string personId,
        long? birthAbsoluteDay,
        long? ageInDays,
        long? completedYears,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId,
        long? deathAbsoluteDay)
    {
        PersonId = personId;
        BirthAbsoluteDay = birthAbsoluteDay;
        DeathAbsoluteDay = deathAbsoluteDay;
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        MaterializedNpcRuntimeId = materializedNpcRuntimeId;
    }
}

public sealed class WorldStateParentageSnapshot
{
    public string ParentPersonId { get; }
    public string ChildPersonId { get; }

    public WorldStateParentageSnapshot(string parentPersonId, string childPersonId)
    {
        ParentPersonId = parentPersonId;
        ChildPersonId = childPersonId;
    }
}

public sealed class WorldStatePropertyOwnershipSnapshot
{
    public string PropertyId { get; }
    public string OwnerPersonId { get; }

    public WorldStatePropertyOwnershipSnapshot(string propertyId, string ownerPersonId)
    {
        PropertyId = propertyId;
        OwnerPersonId = ownerPersonId;
    }
}

public sealed class WorldStatePropertyTransferSnapshot
{
    public string PropertyId { get; }
    public string PreviousOwnerPersonId { get; }
    public string NewOwnerPersonId { get; }
    public long TransferAbsoluteDay { get; }

    public WorldStatePropertyTransferSnapshot(
        string propertyId,
        string previousOwnerPersonId,
        string newOwnerPersonId,
        long transferAbsoluteDay)
    {
        PropertyId = propertyId;
        PreviousOwnerPersonId = previousOwnerPersonId;
        NewOwnerPersonId = newOwnerPersonId;
        TransferAbsoluteDay = transferAbsoluteDay;
    }
}

public sealed class WorldStateEstateSnapshot
{
    public string EstateId { get; }
    public string DeceasedPersonId { get; }
    public long OpenedAbsoluteDay { get; }

    public WorldStateEstateSnapshot(
        string estateId,
        string deceasedPersonId,
        long openedAbsoluteDay)
    {
        EstateId = estateId;
        DeceasedPersonId = deceasedPersonId;
        OpenedAbsoluteDay = openedAbsoluteDay;
    }
}

public sealed class WorldStateSnapshotMetadata
{
    public long AbsoluteDay { get; }
    public WorldStateCalendarSnapshot CalendarDate { get; }

    public WorldStateSnapshotMetadata(long absoluteDay, WorldStateCalendarSnapshot calendarDate = null)
    {
        AbsoluteDay = absoluteDay;
        CalendarDate = calendarDate;
    }
}

public sealed class WorldStateCalendarSnapshot
{
    public long AbsoluteDay { get; }
    public long Year { get; }
    public int Month { get; }
    public int WeekOfMonth { get; }
    public int DayOfMonth { get; }
    public int DayOfWeek { get; }
    public long DayOfYear { get; }
    public long DaysPerMonth { get; }
    public long DaysPerYear { get; }

    public WorldStateCalendarSnapshot(SimulationDate date)
    {
        AbsoluteDay = date.AbsoluteDay;
        Year = date.Year;
        Month = date.Month;
        WeekOfMonth = date.WeekOfMonth;
        DayOfMonth = date.DayOfMonth;
        DayOfWeek = date.DayOfWeek;
        DayOfYear = date.DayOfYear;
        DaysPerMonth = date.DaysPerMonth;
        DaysPerYear = date.DaysPerYear;
    }
}

public sealed class WorldStateNpcSnapshot
{
    public string RuntimeId { get; }
    public string PersonId { get; }
    public string DefinitionId { get; }
    public string Name { get; }
    public string NpcName => Name;
    public string ResidenceSettlementRuntimeId { get; }
    public NpcLifeState LifeState { get; }
    public NpcInjurySeverity InjurySeverity { get; }
    public string CurrentLocationRuntimeId { get; }
    public string CurrentCityRuntimeId { get; }
    public string DestinationLocationRuntimeId { get; }
    public string DestinationCityRuntimeId { get; }
    public bool IsTraveling { get; }
    public string TravelRouteRuntimeId { get; }
    public int RemainingTravelDays { get; }
    public int TravelDaysRemaining => RemainingTravelDays;
    public string ActiveTravelPartyId { get; }
    public float MoneyBalance { get; }
    public float Money => MoneyBalance;
    public string ActiveExpeditionId { get; }
    public IReadOnlyList<WorldStateInventoryStackSnapshot> Inventory { get; }
    public IReadOnlyList<string> StatusNames { get; }
    public IReadOnlyList<string> Statuses => StatusNames;
    public WorldStateActionSnapshot CurrentAction { get; }
    public WorldStateMerchantTradePlanSnapshot MerchantTradePlan { get; }
    public WorldStateMerchantTradePlanSnapshot TradePlan => MerchantTradePlan;

    public WorldStateNpcSnapshot(
        string runtimeId,
        string definitionId,
        string residenceSettlementRuntimeId,
        NpcLifeState lifeState,
        NpcInjurySeverity injurySeverity,
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        bool isTraveling,
        string travelRouteRuntimeId,
        int remainingTravelDays,
        string activeTravelPartyId,
        float moneyBalance,
        string activeExpeditionId,
        IEnumerable<WorldStateInventoryStackSnapshot> inventory)
        : this(
            runtimeId,
            definitionId,
            null,
            residenceSettlementRuntimeId,
            lifeState,
            injurySeverity,
            currentLocationRuntimeId,
            currentCityRuntimeId,
            destinationLocationRuntimeId,
            destinationCityRuntimeId,
            isTraveling,
            travelRouteRuntimeId,
            remainingTravelDays,
            activeTravelPartyId,
            moneyBalance,
            activeExpeditionId,
            inventory,
            null,
            null,
            null,
            null)
    {
    }

    public WorldStateNpcSnapshot(
        string runtimeId,
        string definitionId,
        string name,
        string residenceSettlementRuntimeId,
        NpcLifeState lifeState,
        NpcInjurySeverity injurySeverity,
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        bool isTraveling,
        string travelRouteRuntimeId,
        int remainingTravelDays,
        string activeTravelPartyId,
        float moneyBalance,
        string activeExpeditionId,
        IEnumerable<WorldStateInventoryStackSnapshot> inventory,
        IEnumerable<string> statusNames = null,
        WorldStateActionSnapshot currentAction = null,
        WorldStateMerchantTradePlanSnapshot merchantTradePlan = null,
        string personId = null)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        Name = name;
        PersonId = personId;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        LifeState = lifeState;
        InjurySeverity = injurySeverity;
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        CurrentCityRuntimeId = currentCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
        IsTraveling = isTraveling;
        TravelRouteRuntimeId = travelRouteRuntimeId;
        RemainingTravelDays = remainingTravelDays;
        ActiveTravelPartyId = activeTravelPartyId;
        MoneyBalance = moneyBalance;
        ActiveExpeditionId = activeExpeditionId;
        Inventory = SnapshotCollections.CopySorted(inventory, stack => stack?.ItemDefinitionId);
        StatusNames = SnapshotCollections.CopySorted(statusNames, status => status);
        CurrentAction = currentAction;
        MerchantTradePlan = merchantTradePlan;
    }
}

public sealed class WorldStateActionSnapshot
{
    public string DefinitionId { get; }
    public string ActionName { get; }
    public NpcActionCategory Category { get; }
    public NpcActionType Type { get; }
    public string TargetNpcRuntimeId { get; }
    public string TargetCityRuntimeId { get; }
    public string TargetItemDefinitionId { get; }
    public int Amount { get; }
    public float ExpectedUnitPrice { get; }
    public float SuccessChanceMultiplier { get; }
    public NpcTravelReason TravelReason { get; }
    public float ExpectedNetValue { get; }
    public string OriginDecisionId { get; }

    public WorldStateActionSnapshot(
        string definitionId,
        string actionName,
        NpcActionCategory category,
        NpcActionType type,
        string targetNpcRuntimeId,
        string targetCityRuntimeId,
        string targetItemDefinitionId,
        int amount,
        float expectedUnitPrice,
        float successChanceMultiplier,
        NpcTravelReason travelReason,
        float expectedNetValue,
        string originDecisionId)
    {
        DefinitionId = definitionId;
        ActionName = actionName;
        Category = category;
        Type = type;
        TargetNpcRuntimeId = targetNpcRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        TargetItemDefinitionId = targetItemDefinitionId;
        Amount = amount;
        ExpectedUnitPrice = expectedUnitPrice;
        SuccessChanceMultiplier = successChanceMultiplier;
        TravelReason = travelReason;
        ExpectedNetValue = expectedNetValue;
        OriginDecisionId = originDecisionId;
    }
}

public sealed class WorldStateMerchantTradePlanSnapshot
{
    public bool HasData { get; }
    public bool IsActive { get; }
    public string ItemDefinitionId { get; }
    public string OriginCityRuntimeId { get; }
    public string TargetCityRuntimeId { get; }
    public int PlannedAmount { get; }
    public int RemainingAmount { get; }
    public float PurchasePricePerItem { get; }
    public int WaitDaysAtDestination { get; }
    public int PendingTravelDays { get; }
    public string OriginDecisionId { get; }

    public WorldStateMerchantTradePlanSnapshot(
        bool hasData,
        bool isActive,
        string itemDefinitionId,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        int remainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId)
    {
        HasData = hasData;
        IsActive = isActive;
        ItemDefinitionId = itemDefinitionId;
        OriginCityRuntimeId = originCityRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        PlannedAmount = plannedAmount;
        RemainingAmount = remainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
    }
}

public sealed class WorldStateInventoryStackSnapshot
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public float AverageUnitCost { get; }

    public WorldStateInventoryStackSnapshot(string itemDefinitionId, int amount, float averageUnitCost)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        AverageUnitCost = averageUnitCost;
    }
}

public sealed class WorldStateCitySnapshot
{
    public string RuntimeId { get; }
    public string SettlementRuntimeId => RuntimeId;
    public string DefinitionId { get; }
    public string CityName { get; }
    public string SettlementName => CityName;
    public string LocationRuntimeId { get; }
    public int CurrentPopulation { get; }
    public long PopulationRevision { get; }
    public long Revision => PopulationRevision;
    public int NamedResidentCount { get; }
    public int NamedLivingResidentCount => NamedResidentCount;
    public int NamedPresentCount { get; }
    public string MarketCounterpartyRuntimeId { get; }
    public MarketLiquidityMode MarketLiquidityMode { get; }
    public float MarketBalance { get; }
    public IReadOnlyList<string> ResidentNpcRuntimeIds { get; }
    public IReadOnlyList<WorldStateMarketStackSnapshot> MarketStock { get; }

    public WorldStateCitySnapshot(
        string runtimeId,
        string definitionId,
        string locationRuntimeId,
        int currentPopulation,
        string marketCounterpartyRuntimeId,
        MarketLiquidityMode marketLiquidityMode,
        float marketBalance,
        IEnumerable<string> residentNpcRuntimeIds,
        IEnumerable<WorldStateMarketStackSnapshot> marketStock,
        string cityName = null,
        long populationRevision = 0L,
        int namedResidentCount = 0,
        int namedPresentCount = 0)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        CityName = cityName;
        LocationRuntimeId = locationRuntimeId;
        CurrentPopulation = currentPopulation;
        PopulationRevision = populationRevision;
        NamedResidentCount = namedResidentCount;
        NamedPresentCount = namedPresentCount;
        MarketCounterpartyRuntimeId = marketCounterpartyRuntimeId;
        MarketLiquidityMode = marketLiquidityMode;
        MarketBalance = marketBalance;
        ResidentNpcRuntimeIds = SnapshotCollections.CopySorted(residentNpcRuntimeIds, resident => resident);
        MarketStock = SnapshotCollections.CopySorted(marketStock, stock => stock?.ItemDefinitionId);
    }
}

public sealed class WorldStateMarketStackSnapshot
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public int DesiredAmount { get; }
    public float CurrentPrice { get; }

    public WorldStateMarketStackSnapshot(string itemDefinitionId, int amount, int desiredAmount, float currentPrice)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        DesiredAmount = desiredAmount;
        CurrentPrice = currentPrice;
    }
}

public sealed class WorldStateSpatialSnapshot
{
    public IReadOnlyList<WorldStateLocationSnapshot> Locations { get; }
    public IReadOnlyList<WorldStateRouteSnapshot> Routes { get; }

    public WorldStateSpatialSnapshot(
        IEnumerable<WorldStateLocationSnapshot> locations = null,
        IEnumerable<WorldStateRouteSnapshot> routes = null)
    {
        Locations = SnapshotCollections.CopySorted(locations, location => location?.RuntimeId);
        Routes = SnapshotCollections.CopySorted(routes, route => route?.RuntimeId);
    }
}

public sealed class WorldStateLocationSnapshot
{
    public string RuntimeId { get; }

    public WorldStateLocationSnapshot(string runtimeId)
    {
        RuntimeId = runtimeId;
    }
}

public sealed class WorldStateRouteSnapshot
{
    public string RuntimeId { get; }
    public string OriginRuntimeId { get; }
    public string DestinationRuntimeId { get; }
    public int TravelDays { get; }

    public WorldStateRouteSnapshot(string runtimeId, string originRuntimeId, string destinationRuntimeId, int travelDays)
    {
        RuntimeId = runtimeId;
        OriginRuntimeId = originRuntimeId;
        DestinationRuntimeId = destinationRuntimeId;
        TravelDays = travelDays;
    }
}

public sealed class WorldStateSiteSnapshot
{
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public string LocationRuntimeId { get; }
    public ExplorableSiteKind SiteKind { get; }

    public WorldStateSiteSnapshot(string runtimeId, string definitionId, string locationRuntimeId, ExplorableSiteKind siteKind)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        LocationRuntimeId = locationRuntimeId;
        SiteKind = siteKind;
    }
}

public sealed class WorldStateExpeditionSnapshot
{
    public string ExpeditionId { get; }
    public ExpeditionState State { get; }
    public string TargetSiteRuntimeId { get; }
    public string OriginLocationRuntimeId { get; }
    public string TargetLocationRuntimeId { get; }
    public string OutboundRouteRuntimeId { get; }
    public string OriginDecisionId { get; }
    public string TravelPartyId { get; }
    public string CurrentLocalPlaceRuntimeId { get; }
    public int ExplorationProgress { get; }
    public int ExplorationProgressRequired { get; }
    public IReadOnlyList<string> MemberRuntimeIds { get; }
    public IReadOnlyList<string> PerformerRuntimeIds { get; }
    public IReadOnlyList<string> SupportRuntimeIds { get; }
    public IReadOnlyList<string> VisitedLocalPlaceRuntimeIds { get; }
    public IReadOnlyList<string> ObservedLocalConnectionRuntimeIds { get; }
    public ExpeditionObjectiveType ObjectiveType { get; }
    public string ObjectiveTargetItemDefinitionId { get; }
    public string ObjectiveTargetNotableItemRuntimeId { get; }
    public string ObjectiveTargetOppositionRuntimeId { get; }
    public bool ObjectiveCompleted { get; }
    public bool ObjectiveAllowsContinueAfterCompletion { get; }

    public WorldStateExpeditionSnapshot(
        string expeditionId,
        ExpeditionState state,
        string targetSiteRuntimeId,
        string originLocationRuntimeId,
        string targetLocationRuntimeId,
        string outboundRouteRuntimeId,
        string originDecisionId,
        string travelPartyId,
        string currentLocalPlaceRuntimeId,
        int explorationProgress,
        int explorationProgressRequired,
        IEnumerable<string> memberRuntimeIds,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds,
        IEnumerable<string> visitedLocalPlaceRuntimeIds,
        IEnumerable<string> observedLocalConnectionRuntimeIds,
        ExpeditionObjectiveType objectiveType,
        string objectiveTargetItemDefinitionId,
        string objectiveTargetNotableItemRuntimeId,
        string objectiveTargetOppositionRuntimeId,
        bool objectiveCompleted,
        bool objectiveAllowsContinueAfterCompletion)
    {
        ExpeditionId = expeditionId;
        State = state;
        TargetSiteRuntimeId = targetSiteRuntimeId;
        OriginLocationRuntimeId = originLocationRuntimeId;
        TargetLocationRuntimeId = targetLocationRuntimeId;
        OutboundRouteRuntimeId = outboundRouteRuntimeId;
        OriginDecisionId = originDecisionId;
        TravelPartyId = travelPartyId;
        CurrentLocalPlaceRuntimeId = currentLocalPlaceRuntimeId;
        ExplorationProgress = explorationProgress;
        ExplorationProgressRequired = explorationProgressRequired;
        MemberRuntimeIds = SnapshotCollections.CopySorted(memberRuntimeIds, value => value);
        PerformerRuntimeIds = SnapshotCollections.CopySorted(performerRuntimeIds, value => value);
        SupportRuntimeIds = SnapshotCollections.CopySorted(supportRuntimeIds, value => value);
        VisitedLocalPlaceRuntimeIds = SnapshotCollections.CopySorted(visitedLocalPlaceRuntimeIds, value => value);
        ObservedLocalConnectionRuntimeIds = SnapshotCollections.CopySorted(observedLocalConnectionRuntimeIds, value => value);
        ObjectiveType = objectiveType;
        ObjectiveTargetItemDefinitionId = objectiveTargetItemDefinitionId;
        ObjectiveTargetNotableItemRuntimeId = objectiveTargetNotableItemRuntimeId;
        ObjectiveTargetOppositionRuntimeId = objectiveTargetOppositionRuntimeId;
        ObjectiveCompleted = objectiveCompleted;
        ObjectiveAllowsContinueAfterCompletion = objectiveAllowsContinueAfterCompletion;
    }
}

public sealed class WorldStatePlaceContentSnapshot
{
    public PlaceContentOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string MacroLocationRuntimeId { get; }
    public string TopologyOwnerRuntimeId { get; }
    public PlaceSiteState SiteState { get; }
    public PlaceAccessState AccessState { get; }
    public string ControllerRuntimeId { get; }
    public IReadOnlyList<WorldStatePlaceStackSnapshot> Stacks { get; }
    public IReadOnlyList<WorldStatePlaceOppositionSnapshot> Oppositions { get; }

    public string StableKey => WorldStateSnapshotValue.OwnerKey(OwnerKind, OwnerRuntimeId);

    public WorldStatePlaceContentSnapshot(
        PlaceContentOwnerKind ownerKind,
        string ownerRuntimeId,
        string macroLocationRuntimeId,
        string topologyOwnerRuntimeId,
        PlaceSiteState siteState,
        PlaceAccessState accessState,
        string controllerRuntimeId,
        IEnumerable<WorldStatePlaceStackSnapshot> stacks,
        IEnumerable<WorldStatePlaceOppositionSnapshot> oppositions)
    {
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        MacroLocationRuntimeId = macroLocationRuntimeId;
        TopologyOwnerRuntimeId = topologyOwnerRuntimeId;
        SiteState = siteState;
        AccessState = accessState;
        ControllerRuntimeId = controllerRuntimeId;
        Stacks = SnapshotCollections.CopySorted(stacks, stack => stack?.ItemDefinitionId);
        Oppositions = SnapshotCollections.CopySorted(oppositions, opposition => opposition?.RuntimeId);
    }
}

public sealed class WorldStatePlaceStackSnapshot
{
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public PlaceContentPersistencePolicy PersistencePolicy { get; }
    public int DecayPerDay { get; }
    public float AverageUnitCost { get; }

    public WorldStatePlaceStackSnapshot(
        string itemDefinitionId,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        int decayPerDay,
        float averageUnitCost)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        PersistencePolicy = persistencePolicy;
        DecayPerDay = decayPerDay;
        AverageUnitCost = averageUnitCost;
    }
}

public sealed class WorldStatePlaceOppositionSnapshot
{
    public string RuntimeId { get; }
    public bool IsActive { get; }
    public bool IsResolved { get; }
    public string OppositionSideId { get; }
    public IReadOnlyList<string> NamedParticipantRuntimeIds { get; }
    public IReadOnlyList<string> AggregateParticipantSourceIds { get; }

    public WorldStatePlaceOppositionSnapshot(
        string runtimeId,
        bool isActive,
        bool isResolved,
        string oppositionSideId,
        IEnumerable<string> namedParticipantRuntimeIds,
        IEnumerable<string> aggregateParticipantSourceIds)
    {
        RuntimeId = runtimeId;
        IsActive = isActive;
        IsResolved = isResolved;
        OppositionSideId = oppositionSideId;
        NamedParticipantRuntimeIds = SnapshotCollections.CopySorted(namedParticipantRuntimeIds, value => value);
        AggregateParticipantSourceIds = SnapshotCollections.CopySorted(aggregateParticipantSourceIds, value => value);
    }
}

public sealed class WorldStateNotableItemSnapshot
{
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public bool IsPresent { get; }
    public NotableItemCustodyKind? CustodyKind { get; }
    public PlaceContentOwnerKind? OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string OwnerMacroLocationRuntimeId { get; }
    public string OwnerTopologyRuntimeId { get; }
    public string CustodianNpcRuntimeId { get; }

    public string CustodyKey
    {
        get
        {
            if (IsPresent == false || CustodyKind.HasValue == false)
            {
                return null;
            }

            if (CustodyKind.Value == NotableItemCustodyKind.Npc)
            {
                return "Npc:" + CustodianNpcRuntimeId;
            }

            return "Place:" + WorldStateSnapshotValue.OwnerKey(OwnerKind.Value, OwnerRuntimeId);
        }
    }

    public WorldStateNotableItemSnapshot(
        string runtimeId,
        string definitionId,
        bool isPresent,
        NotableItemCustodyKind? custodyKind,
        PlaceContentOwnerKind? ownerKind,
        string ownerRuntimeId,
        string ownerMacroLocationRuntimeId,
        string ownerTopologyRuntimeId,
        string custodianNpcRuntimeId)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        IsPresent = isPresent;
        CustodyKind = custodyKind;
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        OwnerMacroLocationRuntimeId = ownerMacroLocationRuntimeId;
        OwnerTopologyRuntimeId = ownerTopologyRuntimeId;
        CustodianNpcRuntimeId = custodianNpcRuntimeId;
    }
}

public sealed class WorldStateLocalTopologySnapshot
{
    public LocalTopologyOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string MacroLocationRuntimeId { get; }
    public LocalTopologyPublicationState PublicationState { get; }
    public IReadOnlyList<WorldStateLocalPlaceSnapshot> Places { get; }
    public IReadOnlyList<WorldStateLocalConnectionSnapshot> Connections { get; }

    public string StableKey => WorldStateSnapshotValue.OwnerKey(OwnerKind, OwnerRuntimeId);

    public WorldStateLocalTopologySnapshot(
        LocalTopologyOwnerKind ownerKind,
        string ownerRuntimeId,
        string macroLocationRuntimeId,
        LocalTopologyPublicationState publicationState,
        IEnumerable<WorldStateLocalPlaceSnapshot> places,
        IEnumerable<WorldStateLocalConnectionSnapshot> connections)
    {
        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        MacroLocationRuntimeId = macroLocationRuntimeId;
        PublicationState = publicationState;
        Places = SnapshotCollections.CopySorted(places, place => place?.RuntimeId);
        Connections = SnapshotCollections.CopySorted(connections, connection => connection?.RuntimeId);
    }
}

public sealed class WorldStateLocalPlaceSnapshot
{
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public string ParentRuntimeId { get; }
    public bool IsEntryPoint { get; }

    public WorldStateLocalPlaceSnapshot(string runtimeId, string definitionId, string parentRuntimeId, bool isEntryPoint)
    {
        RuntimeId = runtimeId;
        DefinitionId = definitionId;
        ParentRuntimeId = parentRuntimeId;
        IsEntryPoint = isEntryPoint;
    }
}

public sealed class WorldStateLocalConnectionSnapshot
{
    public string RuntimeId { get; }
    public string OriginRuntimeId { get; }
    public string DestinationRuntimeId { get; }
    public float TraversalCost { get; }
    public string ConnectionTypeDefinitionId { get; }

    public WorldStateLocalConnectionSnapshot(
        string runtimeId,
        string originRuntimeId,
        string destinationRuntimeId,
        float traversalCost,
        string connectionTypeDefinitionId)
    {
        RuntimeId = runtimeId;
        OriginRuntimeId = originRuntimeId;
        DestinationRuntimeId = destinationRuntimeId;
        TraversalCost = traversalCost;
        ConnectionTypeDefinitionId = connectionTypeDefinitionId;
    }
}

public static class WorldStateSnapshotBuilder
{
    public static WorldStateSnapshot Capture(WorldStateSnapshotContext context)
    {
        return BuildSnapshot(context);
    }

    public static WorldStateSnapshot BuildSnapshot(WorldStateSnapshotContext context)
    {
        context = context ?? new WorldStateSnapshotContext();

        List<NpcRuntime> knownNpcs = SnapshotCollections.Materialize(context.Npcs);
        List<WorldStatePersonSnapshot> persons = BuildPersonSnapshots(
            context.PersonStore,
            context.SimulationTime,
            context.Calendar);
        List<WorldStateParentageSnapshot> parentages = BuildParentageSnapshots(context.Parentages);
        List<WorldStateExpeditionSnapshot> expeditions = BuildExpeditionSnapshots(context.ExpeditionStore);
        WorldStateCalendarSnapshot calendarDate = null;
        if (context.Calendar != null && context.SimulationTime != null)
        {
            calendarDate = new WorldStateCalendarSnapshot(context.Calendar.GetDate(context.SimulationTime.AbsoluteDay));
        }

        return new WorldStateSnapshot(
            context.SimulationTime != null ? context.SimulationTime.AbsoluteDay : 0L,
            BuildNpcSnapshots(knownNpcs, expeditions),
            BuildCitySnapshots(context.Cities, knownNpcs, context.PersonStore?.Persons),
            BuildSpatialSnapshot(context.SpatialNetwork),
            BuildSiteSnapshots(context.ExplorableSiteStore),
            expeditions,
            BuildPlaceContentSnapshots(context.PlaceContentStore),
            BuildNotableItemSnapshots(context.PlaceContentStore),
            BuildLocalTopologySnapshots(context.LocalTopologyStore),
            calendarDate,
            persons,
            parentages,
            BuildPropertyOwnershipSnapshots(context.PropertyOwnershipStore),
            BuildEstateSnapshots(context.EstateStore),
            BuildPropertyTransferSnapshots(context.PropertyOwnershipStore));
    }

    private static List<WorldStateNpcSnapshot> BuildNpcSnapshots(
        IEnumerable<NpcRuntime> source,
        IReadOnlyList<WorldStateExpeditionSnapshot> expeditions)
    {
        List<NpcRuntime> npcs = SnapshotCollections.Materialize(source);
        npcs.RemoveAll(npc => npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId));
        npcs.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateNpcSnapshot> result = new List<WorldStateNpcSnapshot>();
        foreach (NpcRuntime npc in npcs)
        {
            result.Add(new WorldStateNpcSnapshot(
                npc.RuntimeId,
                npc.DefinitionId,
                npc.NpcName,
                npc.ResidenceSettlementRuntimeId,
                npc.LifeState,
                npc.InjurySeverity,
                npc.CurrentLocation?.RuntimeId,
                npc.CurrentCity?.RuntimeId,
                npc.DestinationLocation?.RuntimeId,
                npc.DestinationCity?.RuntimeId,
                npc.IsTraveling,
                npc.TravelRouteRuntimeId,
                npc.TravelDaysRemaining,
                npc.ActiveTravelPartyId,
                npc.MoneyAccount?.Balance ?? 0f,
                FindActiveExpeditionId(npc.RuntimeId, expeditions),
                BuildInventorySnapshots(npc),
                BuildStatusSnapshots(npc),
                BuildActionSnapshot(npc),
                BuildMerchantTradePlanSnapshot(npc),
                npc.PersonId?.Value));
        }

        return result;
    }

    private static List<WorldStatePersonSnapshot> BuildPersonSnapshots(
        PersonStore personStore,
        SimulationTime simulationTime,
        SimulationCalendar calendar)
    {
        List<WorldStatePersonSnapshot> result = new List<WorldStatePersonSnapshot>();
        if (personStore == null)
        {
            return result;
        }

        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null || person.PersonId == null)
            {
                continue;
            }

            PersonAgeSnapshot age = null;
            if (simulationTime != null && calendar != null)
            {
                PersonAgeQuery.TryCalculate(
                    person,
                    simulationTime,
                    calendar,
                    out age,
                    out _);
            }

            result.Add(new WorldStatePersonSnapshot(
                person.PersonId.Value,
                person.BirthAbsoluteDay,
                age?.AgeInDays,
                age?.CompletedYears,
                person.ResidenceSettlementRuntimeId,
                person.MaterializedNpcRuntimeId,
                person.DeathAbsoluteDay));
        }

        return result;
    }

    private static List<WorldStateParentageSnapshot> BuildParentageSnapshots(
        IEnumerable<ParentageRecord> source)
    {
        List<WorldStateParentageSnapshot> result = new List<WorldStateParentageSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (ParentageRecord parentage in source)
        {
            if (parentage != null)
            {
                result.Add(new WorldStateParentageSnapshot(
                    parentage.ParentId?.Value,
                    parentage.ChildId?.Value));
            }
        }

        result.Sort((left, right) =>
        {
            int parent = StringComparer.Ordinal.Compare(left.ParentPersonId, right.ParentPersonId);
            return parent != 0
                ? parent
                : StringComparer.Ordinal.Compare(left.ChildPersonId, right.ChildPersonId);
        });
        return result;
    }

    private static List<WorldStatePropertyOwnershipSnapshot> BuildPropertyOwnershipSnapshots(
        PropertyOwnershipStore source)
    {
        List<WorldStatePropertyOwnershipSnapshot> result =
            new List<WorldStatePropertyOwnershipSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PropertyOwnershipRecord ownership in source.Records)
        {
            if (ownership?.PropertyId != null && ownership.OwnerPersonId != null)
            {
                result.Add(new WorldStatePropertyOwnershipSnapshot(
                    ownership.PropertyId.Value,
                    ownership.OwnerPersonId.Value));
            }
        }

        result.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.PropertyId,
            right.PropertyId));
        return result;
    }

    private static List<WorldStateEstateSnapshot> BuildEstateSnapshots(EstateStore source)
    {
        List<WorldStateEstateSnapshot> result = new List<WorldStateEstateSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (EstateRecord estate in source.Records)
        {
            if (estate?.EstateId != null && estate.DeceasedPersonId != null)
            {
                result.Add(new WorldStateEstateSnapshot(
                    estate.EstateId.Value,
                    estate.DeceasedPersonId.Value,
                    estate.OpenedAbsoluteDay));
            }
        }

        result.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.EstateId,
            right.EstateId));
        return result;
    }

    private static List<WorldStatePropertyTransferSnapshot> BuildPropertyTransferSnapshots(
        PropertyOwnershipStore source)
    {
        List<WorldStatePropertyTransferSnapshot> result =
            new List<WorldStatePropertyTransferSnapshot>();
        if (source == null)
        {
            return result;
        }

        foreach (PropertyOwnershipTransferHistoryRecord transfer in source.TransferHistory)
        {
            if (transfer?.PropertyId != null
                && transfer.PreviousOwnerPersonId != null
                && transfer.NewOwnerPersonId != null)
            {
                result.Add(new WorldStatePropertyTransferSnapshot(
                    transfer.PropertyId.Value,
                    transfer.PreviousOwnerPersonId.Value,
                    transfer.NewOwnerPersonId.Value,
                    transfer.TransferAbsoluteDay));
            }
        }

        result.Sort((left, right) =>
        {
            int property = StringComparer.Ordinal.Compare(left.PropertyId, right.PropertyId);
            if (property != 0)
            {
                return property;
            }

            int day = left.TransferAbsoluteDay.CompareTo(right.TransferAbsoluteDay);
            if (day != 0)
            {
                return day;
            }

            int previous = StringComparer.Ordinal.Compare(
                left.PreviousOwnerPersonId,
                right.PreviousOwnerPersonId);
            return previous != 0
                ? previous
                : StringComparer.Ordinal.Compare(
                left.NewOwnerPersonId,
                right.NewOwnerPersonId);
        });
        return result;
    }

    private static List<string> BuildStatusSnapshots(NpcRuntime npc)
    {
        List<string> result = new List<string>();
        if (npc?.CurrentStatus != null)
        {
            foreach (NpcStatusData status in npc.CurrentStatus)
            {
                if (status != null && string.IsNullOrWhiteSpace(status.statusName) == false)
                {
                    result.Add(status.statusName);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static WorldStateActionSnapshot BuildActionSnapshot(NpcRuntime npc)
    {
        if (npc == null)
        {
            return null;
        }

        NpcActionRuntime actionRuntime = npc.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npc.CurrentAction;
        if (action == null)
        {
            return null;
        }

        return new WorldStateActionSnapshot(
            action.DefinitionId,
            action.actionName,
            action.actionCategory,
            action.actionType,
            actionRuntime?.TargetNpc?.RuntimeId,
            actionRuntime?.TargetCity?.RuntimeId,
            actionRuntime?.TargetItem?.DefinitionId,
            actionRuntime?.Amount ?? 0,
            actionRuntime?.ExpectedUnitPrice ?? 0f,
            actionRuntime?.SuccessChanceMultiplier ?? 1f,
            actionRuntime?.TravelReason ?? NpcTravelReason.None,
            actionRuntime?.ExpectedNetValue ?? 0f,
            actionRuntime?.OriginDecisionId);
    }

    private static WorldStateMerchantTradePlanSnapshot BuildMerchantTradePlanSnapshot(NpcRuntime npc)
    {
        if (npc == null)
        {
            return null;
        }

        MerchantTradePlanRuntime plan = npc.MerchantTradePlan;
        if (plan == null || plan.HasData == false)
        {
            return null;
        }

        return new WorldStateMerchantTradePlanSnapshot(
            plan.HasData,
            plan.IsActive,
            plan.Item?.DefinitionId,
            plan.OriginCity?.RuntimeId,
            plan.TargetCity?.RuntimeId,
            plan.PlannedAmount,
            plan.RemainingAmount,
            plan.PurchasePricePerItem,
            plan.WaitDaysAtDestination,
            plan.PendingTravelDays,
            plan.OriginDecisionId);
    }

    private static string FindActiveExpeditionId(
        string npcRuntimeId,
        IReadOnlyList<WorldStateExpeditionSnapshot> expeditions)
    {
        foreach (WorldStateExpeditionSnapshot expedition in expeditions)
        {
            if (ContainsId(expedition.MemberRuntimeIds, npcRuntimeId))
            {
                return expedition.ExpeditionId;
            }
        }

        return null;
    }

    private static bool ContainsId(IReadOnlyList<string> values, string expected)
    {
        if (values == null)
        {
            return false;
        }

        foreach (string value in values)
        {
            if (string.Equals(value, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static List<WorldStateInventoryStackSnapshot> BuildInventorySnapshots(NpcRuntime npc)
    {
        Dictionary<string, InventoryAggregate> aggregates = new Dictionary<string, InventoryAggregate>(StringComparer.Ordinal);
        if (npc?.Inventory?.Items != null)
        {
            foreach (InventoryItemRuntime item in npc.Inventory.Items)
            {
                string definitionId = item?.Item?.DefinitionId;
                if (string.IsNullOrWhiteSpace(definitionId) == true)
                {
                    continue;
                }

                if (aggregates.TryGetValue(definitionId, out InventoryAggregate aggregate) == false)
                {
                    aggregate = new InventoryAggregate();
                    aggregates.Add(definitionId, aggregate);
                }

                aggregate.Add(item.Amount, item.AverageUnitCost);
            }
        }

        List<string> definitionIds = new List<string>(aggregates.Keys);
        definitionIds.Sort(StringComparer.Ordinal);
        List<WorldStateInventoryStackSnapshot> result = new List<WorldStateInventoryStackSnapshot>();
        foreach (string definitionId in definitionIds)
        {
            InventoryAggregate aggregate = aggregates[definitionId];
            result.Add(new WorldStateInventoryStackSnapshot(
                definitionId,
                aggregate.Amount,
                aggregate.GetAverageUnitCost()));
        }

        return result;
    }

    private static List<WorldStateCitySnapshot> BuildCitySnapshots(
        IEnumerable<CityRuntime> source,
        IEnumerable<NpcRuntime> knownNpcs,
        IEnumerable<PersonRuntime> persons)
    {
        List<CityRuntime> cities = SnapshotCollections.Materialize(source);
        cities.RemoveAll(city => city == null || string.IsNullOrWhiteSpace(city.RuntimeId));
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateCitySnapshot> result = new List<WorldStateCitySnapshot>();
        foreach (CityRuntime city in cities)
        {
            MarketRuntime market = city.Market;
            MarketCounterpartyRuntime counterparty = market?.Counterparty;
            List<string> residents = new List<string>();
            foreach (NpcRuntime npc in city.ImportantNpcs)
            {
                if (npc != null && string.IsNullOrWhiteSpace(npc.RuntimeId) == false)
                {
                    residents.Add(npc.RuntimeId);
                }
            }

            residents.Sort(StringComparer.Ordinal);
            SettlementPopulationPresenceSummary presence = SettlementPopulationPresenceQuery.BuildSummary(city, knownNpcs, persons);
            result.Add(new WorldStateCitySnapshot(
                city.RuntimeId,
                city.DefinitionId,
                city.Location?.RuntimeId,
                city.CurrentPopulation,
                counterparty?.CounterpartyRuntimeId,
                counterparty != null ? counterparty.LiquidityMode : MarketLiquidityMode.Open,
                counterparty?.MoneyAccount?.Balance ?? 0f,
                residents,
                BuildMarketStockSnapshots(market),
                city.CityName,
                city.Population.Revision,
                presence?.NamedResidentCount ?? 0,
                presence?.NamedPresentCount ?? 0));
        }

        return result;
    }

    private static List<WorldStateMarketStackSnapshot> BuildMarketStockSnapshots(MarketRuntime market)
    {
        List<MarketItemRuntime> items = market == null ? new List<MarketItemRuntime>() : new List<MarketItemRuntime>(market.Items);
        items.RemoveAll(item => item == null || string.IsNullOrWhiteSpace(item.Item?.DefinitionId));
        items.Sort((left, right) =>
        {
            int comparison = StringComparer.Ordinal.Compare(left.Item.DefinitionId, right.Item.DefinitionId);
            if (comparison != 0) return comparison;
            comparison = left.Amount.CompareTo(right.Amount);
            if (comparison != 0) return comparison;
            comparison = left.DesiredAmount.CompareTo(right.DesiredAmount);
            if (comparison != 0) return comparison;
            return left.CurrentPrice.CompareTo(right.CurrentPrice);
        });

        List<WorldStateMarketStackSnapshot> result = new List<WorldStateMarketStackSnapshot>();
        foreach (MarketItemRuntime item in items)
        {
            result.Add(new WorldStateMarketStackSnapshot(
                item.Item.DefinitionId,
                item.Amount,
                item.DesiredAmount,
                item.CurrentPrice));
        }

        return result;
    }

    private static WorldStateSpatialSnapshot BuildSpatialSnapshot(SpatialNetworkRuntime network)
    {
        if (network == null)
        {
            return new WorldStateSpatialSnapshot();
        }

        List<SpatialLocationRuntime> locations = new List<SpatialLocationRuntime>(network.Locations);
        locations.RemoveAll(location => location == null || string.IsNullOrWhiteSpace(location.RuntimeId));
        locations.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<WorldStateLocationSnapshot> locationSnapshots = new List<WorldStateLocationSnapshot>();
        foreach (SpatialLocationRuntime location in locations)
        {
            locationSnapshots.Add(new WorldStateLocationSnapshot(location.RuntimeId));
        }

        List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>(network.Routes);
        routes.RemoveAll(route => route == null || string.IsNullOrWhiteSpace(route.RuntimeId));
        routes.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<WorldStateRouteSnapshot> routeSnapshots = new List<WorldStateRouteSnapshot>();
        foreach (SpatialRouteRuntime route in routes)
        {
            routeSnapshots.Add(new WorldStateRouteSnapshot(
                route.RuntimeId,
                route.Origin?.RuntimeId,
                route.Destination?.RuntimeId,
                route.TravelDays));
        }

        return new WorldStateSpatialSnapshot(locationSnapshots, routeSnapshots);
    }

    private static List<WorldStateSiteSnapshot> BuildSiteSnapshots(ExplorableSiteStore store)
    {
        List<ExplorableSiteRuntime> sites = store == null
            ? new List<ExplorableSiteRuntime>()
            : new List<ExplorableSiteRuntime>(store.Sites);
        sites.RemoveAll(site => site == null || string.IsNullOrWhiteSpace(site.RuntimeId));
        sites.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateSiteSnapshot> result = new List<WorldStateSiteSnapshot>();
        foreach (ExplorableSiteRuntime site in sites)
        {
            result.Add(new WorldStateSiteSnapshot(
                site.RuntimeId,
                site.DefinitionId,
                site.Location?.RuntimeId,
                site.Definition != null ? site.Definition.kind : ExplorableSiteKind.Generic));
        }

        return result;
    }

    private static List<WorldStateExpeditionSnapshot> BuildExpeditionSnapshots(ExpeditionStore store)
    {
        List<ExpeditionRuntime> expeditions = store == null
            ? new List<ExpeditionRuntime>()
            : new List<ExpeditionRuntime>(store.ActiveExpeditions);
        expeditions.RemoveAll(expedition => expedition == null || string.IsNullOrWhiteSpace(expedition.ExpeditionId));
        expeditions.Sort((left, right) => StringComparer.Ordinal.Compare(left.ExpeditionId, right.ExpeditionId));

        List<WorldStateExpeditionSnapshot> result = new List<WorldStateExpeditionSnapshot>();
        foreach (ExpeditionRuntime expedition in expeditions)
        {
            ExpeditionObjectiveRuntime objective = expedition.Objective;
            result.Add(new WorldStateExpeditionSnapshot(
                expedition.ExpeditionId,
                expedition.State,
                expedition.TargetSiteRuntimeId,
                expedition.OriginLocationRuntimeId,
                expedition.TargetLocationRuntimeId,
                expedition.OutboundRouteRuntimeId,
                expedition.OriginDecisionId,
                expedition.TravelPartyId,
                expedition.CurrentLocalPlaceRuntimeId,
                expedition.ExplorationProgress,
                expedition.ExplorationProgressRequired,
                SortStrings(expedition.MemberRuntimeIds),
                SortStrings(expedition.PerformerRuntimeIds),
                SortStrings(expedition.SupportRuntimeIds),
                SortStrings(expedition.VisitedLocalPlaceRuntimeIds),
                SortStrings(expedition.ObservedLocalConnectionRuntimeIds),
                objective != null ? objective.ObjectiveType : ExpeditionObjectiveType.Explore,
                objective?.TargetItemDefinitionId,
                objective?.TargetNotableItemRuntimeId,
                objective?.TargetOppositionRuntimeId,
                objective?.IsCompleted ?? false,
                objective?.AllowContinueAfterCompletion ?? false));
        }

        return result;
    }

    private static List<WorldStatePlaceContentSnapshot> BuildPlaceContentSnapshots(PlaceContentStore store)
    {
        List<PlaceContentRuntime> contents = store == null
            ? new List<PlaceContentRuntime>()
            : new List<PlaceContentRuntime>(store.Places);
        contents.RemoveAll(content => content == null || content.Owner == null);
        contents.Sort((left, right) => StringComparer.Ordinal.Compare(left.Owner.StableKey, right.Owner.StableKey));

        List<WorldStatePlaceContentSnapshot> result = new List<WorldStatePlaceContentSnapshot>();
        foreach (PlaceContentRuntime content in contents)
        {
            PlaceContentOwnerReference owner = content.Owner;
            result.Add(new WorldStatePlaceContentSnapshot(
                owner.OwnerKind,
                owner.OwnerRuntimeId,
                owner.MacroLocationRuntimeId,
                owner.TopologyOwnerRuntimeId,
                content.SiteState,
                content.AccessState,
                content.ControllerRuntimeId,
                BuildPlaceStackSnapshots(content.StackedContent),
                BuildOppositionSnapshots(content.Oppositions)));
        }

        return result;
    }

    private static List<WorldStatePlaceStackSnapshot> BuildPlaceStackSnapshots(
        IEnumerable<PlaceContentStackRuntime> source)
    {
        List<PlaceContentStackRuntime> stacks = SnapshotCollections.Materialize(source);
        stacks.RemoveAll(stack => stack == null || string.IsNullOrWhiteSpace(stack.ItemDefinitionId));
        stacks.Sort((left, right) => StringComparer.Ordinal.Compare(left.ItemDefinitionId, right.ItemDefinitionId));
        List<WorldStatePlaceStackSnapshot> result = new List<WorldStatePlaceStackSnapshot>();
        foreach (PlaceContentStackRuntime stack in stacks)
        {
            result.Add(new WorldStatePlaceStackSnapshot(
                stack.ItemDefinitionId,
                stack.Amount,
                stack.PersistencePolicy,
                stack.DecayPerDay,
                stack.AverageUnitCost));
        }

        return result;
    }

    private static List<WorldStatePlaceOppositionSnapshot> BuildOppositionSnapshots(
        IEnumerable<PlaceOppositionRuntime> source)
    {
        List<PlaceOppositionRuntime> oppositions = SnapshotCollections.Materialize(source);
        oppositions.RemoveAll(opposition => opposition == null || string.IsNullOrWhiteSpace(opposition.RuntimeId));
        oppositions.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<WorldStatePlaceOppositionSnapshot> result = new List<WorldStatePlaceOppositionSnapshot>();
        foreach (PlaceOppositionRuntime opposition in oppositions)
        {
            List<string> named = new List<string>();
            foreach (NpcRuntime npc in opposition.NamedParticipants)
            {
                if (npc != null && string.IsNullOrWhiteSpace(npc.RuntimeId) == false)
                {
                    named.Add(npc.RuntimeId);
                }
            }

            List<string> aggregate = new List<string>();
            foreach (AggregateParticipantSnapshot participant in opposition.AggregateParticipants)
            {
                if (participant != null && string.IsNullOrWhiteSpace(participant.SourceId) == false)
                {
                    aggregate.Add(participant.SourceId);
                }
            }

            named.Sort(StringComparer.Ordinal);
            aggregate.Sort(StringComparer.Ordinal);
            result.Add(new WorldStatePlaceOppositionSnapshot(
                opposition.RuntimeId,
                opposition.IsActive,
                opposition.IsResolved,
                opposition.OppositionSideId,
                named,
                aggregate));
        }

        return result;
    }

    private static List<WorldStateNotableItemSnapshot> BuildNotableItemSnapshots(PlaceContentStore store)
    {
        List<NotableItemRuntime> notables = store == null
            ? new List<NotableItemRuntime>()
            : new List<NotableItemRuntime>(store.NotableItems);
        notables.RemoveAll(notable => notable == null || string.IsNullOrWhiteSpace(notable.RuntimeId));
        notables.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        List<WorldStateNotableItemSnapshot> result = new List<WorldStateNotableItemSnapshot>();
        foreach (NotableItemRuntime notable in notables)
        {
            NotableItemCustodyReference custody = notable.Custody;
            PlaceContentOwnerReference owner = custody?.PlaceOwner;
            result.Add(new WorldStateNotableItemSnapshot(
                notable.RuntimeId,
                notable.DefinitionId,
                notable.IsPresent,
                custody?.CustodyKind,
                owner?.OwnerKind,
                owner?.OwnerRuntimeId,
                owner?.MacroLocationRuntimeId,
                owner?.TopologyOwnerRuntimeId,
                custody?.NpcRuntimeId));
        }

        return result;
    }

    private static List<WorldStateLocalTopologySnapshot> BuildLocalTopologySnapshots(LocalTopologyStore store)
    {
        List<LocalTopologyRuntime> topologies = store == null
            ? new List<LocalTopologyRuntime>()
            : new List<LocalTopologyRuntime>(store.Topologies);
        topologies.RemoveAll(topology => topology == null || topology.Owner == null);
        topologies.Sort((left, right) => StringComparer.Ordinal.Compare(
            WorldStateSnapshotValue.OwnerKey(left.Owner.OwnerKind, left.Owner.OwnerRuntimeId),
            WorldStateSnapshotValue.OwnerKey(right.Owner.OwnerKind, right.Owner.OwnerRuntimeId)));

        List<WorldStateLocalTopologySnapshot> result = new List<WorldStateLocalTopologySnapshot>();
        foreach (LocalTopologyRuntime topology in topologies)
        {
            LocalTopologyOwnerReference owner = topology.Owner;
            List<LocalPlaceRuntime> places = new List<LocalPlaceRuntime>(topology.Places);
            places.RemoveAll(place => place == null || string.IsNullOrWhiteSpace(place.RuntimeId));
            places.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
            List<WorldStateLocalPlaceSnapshot> placeSnapshots = new List<WorldStateLocalPlaceSnapshot>();
            foreach (LocalPlaceRuntime place in places)
            {
                placeSnapshots.Add(new WorldStateLocalPlaceSnapshot(
                    place.RuntimeId,
                    place.TypeDefinitionId,
                    place.Parent?.RuntimeId,
                    topology.IsEntryPoint(place)));
            }

            List<LocalTopologyConnectionRuntime> connections = new List<LocalTopologyConnectionRuntime>(topology.Connections);
            connections.RemoveAll(connection => connection == null || string.IsNullOrWhiteSpace(connection.RuntimeId));
            connections.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
            List<WorldStateLocalConnectionSnapshot> connectionSnapshots = new List<WorldStateLocalConnectionSnapshot>();
            foreach (LocalTopologyConnectionRuntime connection in connections)
            {
                connectionSnapshots.Add(new WorldStateLocalConnectionSnapshot(
                    connection.RuntimeId,
                    connection.Origin?.RuntimeId,
                    connection.Destination?.RuntimeId,
                    connection.TraversalCost,
                    connection.TypeDefinitionId));
            }

            result.Add(new WorldStateLocalTopologySnapshot(
                owner.OwnerKind,
                owner.OwnerRuntimeId,
                owner.MacroLocationRuntimeId,
                topology.PublicationState,
                placeSnapshots,
                connectionSnapshots));
        }

        return result;
    }

    private static List<string> SortStrings(IEnumerable<string> source)
    {
        List<string> result = new List<string>();
        if (source != null)
        {
            foreach (string value in source)
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    result.Add(value);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private sealed class InventoryAggregate
    {
        public int Amount { get; private set; }
        private double totalCost;

        public void Add(int amount, float averageUnitCost)
        {
            Amount += amount;
            totalCost += (double)amount * averageUnitCost;
        }

        public float GetAverageUnitCost()
        {
            return Amount > 0 ? (float)(totalCost / Amount) : 0f;
        }
    }
}

internal static class SnapshotCollections
{
    public static List<T> Materialize<T>(IEnumerable<T> source)
    {
        return source == null ? new List<T>() : new List<T>(source);
    }

    public static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
    {
        return Materialize(source).AsReadOnly();
    }

    public static IReadOnlyList<T> CopySorted<T>(IEnumerable<T> source, Func<T, string> keySelector)
    {
        List<T> values = Materialize(source);
        if (keySelector != null)
        {
            values.Sort((left, right) => StringComparer.Ordinal.Compare(keySelector(left) ?? string.Empty, keySelector(right) ?? string.Empty));
        }

        return values.AsReadOnly();
    }
}

internal static class WorldStateSnapshotValue
{
    public static string OwnerKey(PlaceContentOwnerKind kind, string runtimeId)
    {
        return Enum.GetName(typeof(PlaceContentOwnerKind), kind) + ":" + runtimeId;
    }

    public static string OwnerKey(LocalTopologyOwnerKind kind, string runtimeId)
    {
        return Enum.GetName(typeof(LocalTopologyOwnerKind), kind) + ":" + runtimeId;
    }
}
