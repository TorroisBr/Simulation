using System;
using System.Collections.Generic;

public enum WorldStateDifferenceChangeKind
{
    Added,
    Removed,
    Changed
}

public sealed class WorldStateDifference
{
    public string Section { get; }
    public string Identity { get; }
    public string Field { get; }
    public string BeforeValue { get; }
    public string AfterValue { get; }
    public WorldStateDifferenceChangeKind ChangeKind { get; }

    public WorldStateDifference(
        string section,
        string identity,
        string field,
        string beforeValue,
        string afterValue,
        WorldStateDifferenceChangeKind changeKind)
    {
        Section = section;
        Identity = identity;
        Field = field;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        ChangeKind = changeKind;
    }
}

public sealed class WorldStateDiff
{
    public IReadOnlyList<WorldStateDifference> Differences { get; }
    public bool IsEmpty => Differences.Count == 0;

    public WorldStateDiff(IEnumerable<WorldStateDifference> differences)
    {
        List<WorldStateDifference> copied = SnapshotCollections.Materialize(differences);
        SortDifferences(copied);
        Differences = copied.AsReadOnly();
    }

    public static WorldStateDiff Compare(WorldStateSnapshot before, WorldStateSnapshot after)
    {
        List<WorldStateDifference> differences = new List<WorldStateDifference>();
        if (before == null || after == null)
        {
            ComparePresence("Snapshot", "world", before, after, differences);
            return new WorldStateDiff(differences);
        }

        CompareValue("Metadata", "world", "AbsoluteDay",
            WorldStateCanonicalWriter.Int64Value(before.AbsoluteDay),
            WorldStateCanonicalWriter.Int64Value(after.AbsoluteDay),
            differences);
        CompareValue("Metadata", "world", "SettlementCount",
            WorldStateCanonicalWriter.IntValue(before.SettlementCount),
            WorldStateCanonicalWriter.IntValue(after.SettlementCount),
            differences);
        CompareValue("Metadata", "world", "KnownNpcCount",
            WorldStateCanonicalWriter.IntValue(before.KnownNpcCount),
            WorldStateCanonicalWriter.IntValue(after.KnownNpcCount),
            differences);
        CompareValue("Metadata", "world", "PoliticalClaimCount",
            WorldStateCanonicalWriter.IntValue(before.PoliticalClaimCount),
            WorldStateCanonicalWriter.IntValue(after.PoliticalClaimCount),
            differences);
        CompareValue("Metadata", "world", "FactionCount",
            WorldStateCanonicalWriter.IntValue(before.FactionCount),
            WorldStateCanonicalWriter.IntValue(after.FactionCount),
            differences);
        CompareValue("Metadata", "world", "FactionAffiliationCount",
            WorldStateCanonicalWriter.IntValue(before.FactionAffiliationCount),
            WorldStateCanonicalWriter.IntValue(after.FactionAffiliationCount),
            differences);
        CompareCalendar(before.Metadata.CalendarDate, after.Metadata.CalendarDate, differences);

        CompareEntities("NPC", before.Npcs, after.Npcs, npc => npc.RuntimeId,
            (identity, left, right) =>
            {
                CompareValue("NPC", identity, "DefinitionId", WorldStateCanonicalWriter.StringValue(left.DefinitionId), WorldStateCanonicalWriter.StringValue(right.DefinitionId), differences);
                CompareValue("NPC", identity, "Name", WorldStateCanonicalWriter.StringValue(left.Name), WorldStateCanonicalWriter.StringValue(right.Name), differences);
                CompareValue("NPC", identity, "PersonId", WorldStateCanonicalWriter.StringValue(left.PersonId), WorldStateCanonicalWriter.StringValue(right.PersonId), differences);
                CompareValue("NPC", identity, "ResidenceSettlementRuntimeId", WorldStateCanonicalWriter.StringValue(left.ResidenceSettlementRuntimeId), WorldStateCanonicalWriter.StringValue(right.ResidenceSettlementRuntimeId), differences);
                CompareValue("NPC", identity, "LifeState", WorldStateCanonicalWriter.EnumValue(left.LifeState), WorldStateCanonicalWriter.EnumValue(right.LifeState), differences);
                CompareValue("NPC", identity, "InjurySeverity", WorldStateCanonicalWriter.EnumValue(left.InjurySeverity), WorldStateCanonicalWriter.EnumValue(right.InjurySeverity), differences);
                CompareValue("NPC", identity, "CurrentLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.CurrentLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.CurrentLocationRuntimeId), differences);
                CompareValue("NPC", identity, "CurrentCityRuntimeId", WorldStateCanonicalWriter.StringValue(left.CurrentCityRuntimeId), WorldStateCanonicalWriter.StringValue(right.CurrentCityRuntimeId), differences);
                CompareValue("NPC", identity, "DestinationLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.DestinationLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.DestinationLocationRuntimeId), differences);
                CompareValue("NPC", identity, "DestinationCityRuntimeId", WorldStateCanonicalWriter.StringValue(left.DestinationCityRuntimeId), WorldStateCanonicalWriter.StringValue(right.DestinationCityRuntimeId), differences);
                CompareValue("NPC", identity, "IsTraveling", WorldStateCanonicalWriter.BoolValue(left.IsTraveling), WorldStateCanonicalWriter.BoolValue(right.IsTraveling), differences);
                CompareValue("NPC", identity, "TravelRouteRuntimeId", WorldStateCanonicalWriter.StringValue(left.TravelRouteRuntimeId), WorldStateCanonicalWriter.StringValue(right.TravelRouteRuntimeId), differences);
                CompareValue("NPC", identity, "RemainingTravelDays", WorldStateCanonicalWriter.IntValue(left.RemainingTravelDays), WorldStateCanonicalWriter.IntValue(right.RemainingTravelDays), differences);
                CompareValue("NPC", identity, "ActiveTravelPartyId", WorldStateCanonicalWriter.StringValue(left.ActiveTravelPartyId), WorldStateCanonicalWriter.StringValue(right.ActiveTravelPartyId), differences);
                CompareValue("NPC", identity, "MoneyBalance", WorldStateCanonicalWriter.FloatValue(left.MoneyBalance), WorldStateCanonicalWriter.FloatValue(right.MoneyBalance), differences);
                CompareValue("NPC", identity, "ActiveExpeditionId", WorldStateCanonicalWriter.StringValue(left.ActiveExpeditionId), WorldStateCanonicalWriter.StringValue(right.ActiveExpeditionId), differences);
                CompareValue("NPC", identity, "StatusNames", WorldStateCanonicalWriter.StringListValue(left.StatusNames), WorldStateCanonicalWriter.StringListValue(right.StatusNames), differences);
                CompareAction(identity, left.CurrentAction, right.CurrentAction, differences);
                CompareMerchantTradePlan(identity, left.MerchantTradePlan, right.MerchantTradePlan, differences);
                CompareInventory(identity, left.Inventory, right.Inventory, differences);
            }, differences);

        CompareEntities("Person", before.Persons, after.Persons, person => person.PersonId,
            (identity, left, right) =>
            {
                CompareValue("Person", identity, "BirthAbsoluteDay", WorldStateCanonicalWriter.NullableInt64Value(left.BirthAbsoluteDay), WorldStateCanonicalWriter.NullableInt64Value(right.BirthAbsoluteDay), differences);
                CompareValue("Person", identity, "DeathAbsoluteDay", WorldStateCanonicalWriter.NullableInt64Value(left.DeathAbsoluteDay), WorldStateCanonicalWriter.NullableInt64Value(right.DeathAbsoluteDay), differences);
                CompareValue("Person", identity, "MaterializedNpcRuntimeId", WorldStateCanonicalWriter.StringValue(left.MaterializedNpcRuntimeId), WorldStateCanonicalWriter.StringValue(right.MaterializedNpcRuntimeId), differences);
                CompareValue("Person", identity, "IsMaterialized", WorldStateCanonicalWriter.BoolValue(left.IsMaterialized), WorldStateCanonicalWriter.BoolValue(right.IsMaterialized), differences);
                CompareValue("Person", identity, "CompletedYears", WorldStateCanonicalWriter.NullableInt64Value(left.CompletedYears), WorldStateCanonicalWriter.NullableInt64Value(right.CompletedYears), differences);
                CompareValue("Person", identity, "ResidenceSettlementRuntimeId", WorldStateCanonicalWriter.StringValue(left.ResidenceSettlementRuntimeId), WorldStateCanonicalWriter.StringValue(right.ResidenceSettlementRuntimeId), differences);
            }, differences);

        CompareEntities("Parentage", before.Parentages, after.Parentages,
            parentage => ParentageIdentity(parentage),
            (identity, left, right) => { },
            differences);

        CompareEntities("PropertyOwnership", before.PropertyOwnerships, after.PropertyOwnerships,
            ownership => ownership.PropertyId,
            (identity, left, right) =>
            {
                CompareValue("PropertyOwnership", identity, "OwnerPersonId",
                    WorldStateCanonicalWriter.StringValue(left.OwnerPersonId),
                    WorldStateCanonicalWriter.StringValue(right.OwnerPersonId),
                    differences);
            },
            differences);

        CompareEntities("PropertyTransfer", before.PropertyTransfers, after.PropertyTransfers,
            transfer => PropertyTransferIdentity(transfer),
            (identity, left, right) =>
            {
                CompareValue("PropertyTransfer", identity, "PreviousOwnerPersonId",
                    WorldStateCanonicalWriter.StringValue(left.PreviousOwnerPersonId),
                    WorldStateCanonicalWriter.StringValue(right.PreviousOwnerPersonId),
                    differences);
                CompareValue("PropertyTransfer", identity, "NewOwnerPersonId",
                    WorldStateCanonicalWriter.StringValue(left.NewOwnerPersonId),
                    WorldStateCanonicalWriter.StringValue(right.NewOwnerPersonId),
                    differences);
                CompareValue("PropertyTransfer", identity, "TransferAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.TransferAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.TransferAbsoluteDay),
                    differences);
            },
            differences);

        CompareEntities("Estate", before.Estates, after.Estates,
            estate => estate.EstateId,
            (identity, left, right) =>
            {
                CompareValue("Estate", identity, "DeceasedPersonId",
                    WorldStateCanonicalWriter.StringValue(left.DeceasedPersonId),
                    WorldStateCanonicalWriter.StringValue(right.DeceasedPersonId),
                    differences);
                CompareValue("Estate", identity, "OpenedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.OpenedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.OpenedAbsoluteDay),
                    differences);
            },
            differences);

        CompareEntities("PoliticalClaim", before.PoliticalClaims, after.PoliticalClaims,
            claim => claim.ClaimId,
            (identity, left, right) =>
            {
                CompareValue("PoliticalClaim", identity, "ClaimantPersonId",
                    WorldStateCanonicalWriter.StringValue(left.ClaimantPersonId),
                    WorldStateCanonicalWriter.StringValue(right.ClaimantPersonId),
                    differences);
                CompareValue("PoliticalClaim", identity, "ClaimType",
                    WorldStateCanonicalWriter.EnumValue(left.ClaimType),
                    WorldStateCanonicalWriter.EnumValue(right.ClaimType),
                    differences);
                CompareValue("PoliticalClaim", identity, "TargetKind",
                    WorldStateCanonicalWriter.EnumValue(left.TargetKind),
                    WorldStateCanonicalWriter.EnumValue(right.TargetKind),
                    differences);
                CompareValue("PoliticalClaim", identity, "TargetId",
                    WorldStateCanonicalWriter.StringValue(left.TargetId),
                    WorldStateCanonicalWriter.StringValue(right.TargetId),
                    differences);
                CompareValue("PoliticalClaim", identity, "Basis",
                    WorldStateCanonicalWriter.EnumValue(left.Basis),
                    WorldStateCanonicalWriter.EnumValue(right.Basis),
                    differences);
                CompareValue("PoliticalClaim", identity, "BasisDescription",
                    WorldStateCanonicalWriter.StringValue(left.BasisDescription),
                    WorldStateCanonicalWriter.StringValue(right.BasisDescription),
                    differences);
                CompareValue("PoliticalClaim", identity, "CreatedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay),
                    differences);
                CompareValue("PoliticalClaim", identity, "Status",
                    WorldStateCanonicalWriter.EnumValue(left.Status),
                    WorldStateCanonicalWriter.EnumValue(right.Status),
                    differences);
                CompareValue("PoliticalClaim", identity, "ResolutionAbsoluteDay",
                    WorldStateCanonicalWriter.NullableInt64Value(left.ResolutionAbsoluteDay),
                    WorldStateCanonicalWriter.NullableInt64Value(right.ResolutionAbsoluteDay),
                    differences);
                CompareValue("PoliticalClaim", identity, "RecognitionState",
                    WorldStateCanonicalWriter.EnumValue(left.RecognitionState),
                    WorldStateCanonicalWriter.EnumValue(right.RecognitionState),
                    differences);
                CompareValue("PoliticalClaim", identity, "RecognizingInstitutionId",
                    WorldStateCanonicalWriter.StringValue(left.RecognizingInstitutionId),
                    WorldStateCanonicalWriter.StringValue(right.RecognizingInstitutionId),
                    differences);
                CompareValue("PoliticalClaim", identity, "RecognitionAbsoluteDay",
                    WorldStateCanonicalWriter.NullableInt64Value(left.RecognitionAbsoluteDay),
                    WorldStateCanonicalWriter.NullableInt64Value(right.RecognitionAbsoluteDay),
                    differences);
                CompareValue("PoliticalClaim", identity, "RecognitionReason",
                    WorldStateCanonicalWriter.StringValue(left.RecognitionReason),
                    WorldStateCanonicalWriter.StringValue(right.RecognitionReason),
                    differences);
                CompareValue("PoliticalClaim", identity, "EvidenceReferences",
                    WorldStateCanonicalWriter.StringListValue(left.EvidenceReferences),
                    WorldStateCanonicalWriter.StringListValue(right.EvidenceReferences),
                    differences);
            },
            differences);

        CompareEntities("Faction", before.Factions, after.Factions,
            faction => faction.FactionId,
            (identity, left, right) =>
            {
                CompareValue("Faction", identity, "DisplayName",
                    WorldStateCanonicalWriter.StringValue(left.DisplayName),
                    WorldStateCanonicalWriter.StringValue(right.DisplayName),
                    differences);
                CompareValue("Faction", identity, "CreatedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay),
                    differences);
            },
            differences);

        CompareEntities("FactionAffiliation", before.FactionAffiliations, after.FactionAffiliations,
            affiliation => affiliation.FactionId + "\u001f" + affiliation.PersonId,
            (identity, left, right) =>
            {
                CompareValue("FactionAffiliation", identity, "JoinedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.JoinedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.JoinedAbsoluteDay),
                    differences);
                CompareValue("FactionAffiliation", identity, "EndedAbsoluteDay",
                    WorldStateCanonicalWriter.NullableInt64Value(left.EndedAbsoluteDay),
                    WorldStateCanonicalWriter.NullableInt64Value(right.EndedAbsoluteDay),
                    differences);
            },
            differences);

        CompareEntities("City", before.Cities, after.Cities, city => city.RuntimeId,
            (identity, left, right) =>
            {
                CompareValue("City", identity, "DefinitionId", WorldStateCanonicalWriter.StringValue(left.DefinitionId), WorldStateCanonicalWriter.StringValue(right.DefinitionId), differences);
                CompareValue("City", identity, "CityName", WorldStateCanonicalWriter.StringValue(left.CityName), WorldStateCanonicalWriter.StringValue(right.CityName), differences);
                CompareValue("City", identity, "LocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.LocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.LocationRuntimeId), differences);
                CompareValue("City", identity, "CurrentPopulation", WorldStateCanonicalWriter.IntValue(left.CurrentPopulation), WorldStateCanonicalWriter.IntValue(right.CurrentPopulation), differences);
                CompareValue("City", identity, "PopulationRevision", WorldStateCanonicalWriter.Int64Value(left.PopulationRevision), WorldStateCanonicalWriter.Int64Value(right.PopulationRevision), differences);
                CompareValue("City", identity, "NamedResidentCount", WorldStateCanonicalWriter.IntValue(left.NamedResidentCount), WorldStateCanonicalWriter.IntValue(right.NamedResidentCount), differences);
                CompareValue("City", identity, "NamedPresentCount", WorldStateCanonicalWriter.IntValue(left.NamedPresentCount), WorldStateCanonicalWriter.IntValue(right.NamedPresentCount), differences);
                CompareValue("City", identity, "MarketCounterpartyRuntimeId", WorldStateCanonicalWriter.StringValue(left.MarketCounterpartyRuntimeId), WorldStateCanonicalWriter.StringValue(right.MarketCounterpartyRuntimeId), differences);
                CompareValue("City", identity, "MarketLiquidityMode", WorldStateCanonicalWriter.EnumValue(left.MarketLiquidityMode), WorldStateCanonicalWriter.EnumValue(right.MarketLiquidityMode), differences);
                CompareValue("City", identity, "MarketBalance", WorldStateCanonicalWriter.FloatValue(left.MarketBalance), WorldStateCanonicalWriter.FloatValue(right.MarketBalance), differences);
                CompareValue("City", identity, "ResidentNpcRuntimeIds", WorldStateCanonicalWriter.StringListValue(left.ResidentNpcRuntimeIds), WorldStateCanonicalWriter.StringListValue(right.ResidentNpcRuntimeIds), differences);
                CompareEntities("CityStock", left.MarketStock, right.MarketStock, stock => stock.ItemDefinitionId,
                    (stockIdentity, stockLeft, stockRight) =>
                    {
                        CompareValue("CityStock", identity + "/item:" + stockIdentity, "Amount", WorldStateCanonicalWriter.IntValue(stockLeft.Amount), WorldStateCanonicalWriter.IntValue(stockRight.Amount), differences);
                        CompareValue("CityStock", identity + "/item:" + stockIdentity, "DesiredAmount", WorldStateCanonicalWriter.IntValue(stockLeft.DesiredAmount), WorldStateCanonicalWriter.IntValue(stockRight.DesiredAmount), differences);
                        CompareValue("CityStock", identity + "/item:" + stockIdentity, "CurrentPrice", WorldStateCanonicalWriter.FloatValue(stockLeft.CurrentPrice), WorldStateCanonicalWriter.FloatValue(stockRight.CurrentPrice), differences);
                    }, differences, identity + "/item:");
            }, differences);

        CompareEntities("Location", before.Spatial.Locations, after.Spatial.Locations, location => location.RuntimeId,
            (identity, left, right) => { }, differences);
        CompareEntities("Route", before.Spatial.Routes, after.Spatial.Routes, route => route.RuntimeId,
            (identity, left, right) =>
            {
                CompareValue("Route", identity, "OriginRuntimeId", WorldStateCanonicalWriter.StringValue(left.OriginRuntimeId), WorldStateCanonicalWriter.StringValue(right.OriginRuntimeId), differences);
                CompareValue("Route", identity, "DestinationRuntimeId", WorldStateCanonicalWriter.StringValue(left.DestinationRuntimeId), WorldStateCanonicalWriter.StringValue(right.DestinationRuntimeId), differences);
                CompareValue("Route", identity, "TravelDays", WorldStateCanonicalWriter.IntValue(left.TravelDays), WorldStateCanonicalWriter.IntValue(right.TravelDays), differences);
            }, differences);

        CompareEntities("Site", before.Sites, after.Sites, site => site.RuntimeId,
            (identity, left, right) =>
            {
                CompareValue("Site", identity, "DefinitionId", WorldStateCanonicalWriter.StringValue(left.DefinitionId), WorldStateCanonicalWriter.StringValue(right.DefinitionId), differences);
                CompareValue("Site", identity, "LocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.LocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.LocationRuntimeId), differences);
                CompareValue("Site", identity, "SiteKind", WorldStateCanonicalWriter.EnumValue(left.SiteKind), WorldStateCanonicalWriter.EnumValue(right.SiteKind), differences);
            }, differences);

        CompareEntities("Expedition", before.Expeditions, after.Expeditions, expedition => expedition.ExpeditionId,
            (identity, left, right) =>
            {
                CompareValue("Expedition", identity, "State", WorldStateCanonicalWriter.EnumValue(left.State), WorldStateCanonicalWriter.EnumValue(right.State), differences);
                CompareValue("Expedition", identity, "TargetSiteRuntimeId", WorldStateCanonicalWriter.StringValue(left.TargetSiteRuntimeId), WorldStateCanonicalWriter.StringValue(right.TargetSiteRuntimeId), differences);
                CompareValue("Expedition", identity, "OriginLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.OriginLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.OriginLocationRuntimeId), differences);
                CompareValue("Expedition", identity, "TargetLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.TargetLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.TargetLocationRuntimeId), differences);
                CompareValue("Expedition", identity, "OutboundRouteRuntimeId", WorldStateCanonicalWriter.StringValue(left.OutboundRouteRuntimeId), WorldStateCanonicalWriter.StringValue(right.OutboundRouteRuntimeId), differences);
                CompareValue("Expedition", identity, "OriginDecisionId", WorldStateCanonicalWriter.StringValue(left.OriginDecisionId), WorldStateCanonicalWriter.StringValue(right.OriginDecisionId), differences);
                CompareValue("Expedition", identity, "TravelPartyId", WorldStateCanonicalWriter.StringValue(left.TravelPartyId), WorldStateCanonicalWriter.StringValue(right.TravelPartyId), differences);
                CompareValue("Expedition", identity, "CurrentLocalPlaceRuntimeId", WorldStateCanonicalWriter.StringValue(left.CurrentLocalPlaceRuntimeId), WorldStateCanonicalWriter.StringValue(right.CurrentLocalPlaceRuntimeId), differences);
                CompareValue("Expedition", identity, "ExplorationProgress", WorldStateCanonicalWriter.IntValue(left.ExplorationProgress), WorldStateCanonicalWriter.IntValue(right.ExplorationProgress), differences);
                CompareValue("Expedition", identity, "ExplorationProgressRequired", WorldStateCanonicalWriter.IntValue(left.ExplorationProgressRequired), WorldStateCanonicalWriter.IntValue(right.ExplorationProgressRequired), differences);
                CompareValue("Expedition", identity, "MemberRuntimeIds", WorldStateCanonicalWriter.StringListValue(left.MemberRuntimeIds), WorldStateCanonicalWriter.StringListValue(right.MemberRuntimeIds), differences);
                CompareValue("Expedition", identity, "PerformerRuntimeIds", WorldStateCanonicalWriter.StringListValue(left.PerformerRuntimeIds), WorldStateCanonicalWriter.StringListValue(right.PerformerRuntimeIds), differences);
                CompareValue("Expedition", identity, "SupportRuntimeIds", WorldStateCanonicalWriter.StringListValue(left.SupportRuntimeIds), WorldStateCanonicalWriter.StringListValue(right.SupportRuntimeIds), differences);
                CompareValue("Expedition", identity, "VisitedLocalPlaceRuntimeIds", WorldStateCanonicalWriter.StringListValue(left.VisitedLocalPlaceRuntimeIds), WorldStateCanonicalWriter.StringListValue(right.VisitedLocalPlaceRuntimeIds), differences);
                CompareValue("Expedition", identity, "ObservedLocalConnectionRuntimeIds", WorldStateCanonicalWriter.StringListValue(left.ObservedLocalConnectionRuntimeIds), WorldStateCanonicalWriter.StringListValue(right.ObservedLocalConnectionRuntimeIds), differences);
                CompareValue("Expedition", identity, "ObjectiveType", WorldStateCanonicalWriter.EnumValue(left.ObjectiveType), WorldStateCanonicalWriter.EnumValue(right.ObjectiveType), differences);
                CompareValue("Expedition", identity, "ObjectiveTargetItemDefinitionId", WorldStateCanonicalWriter.StringValue(left.ObjectiveTargetItemDefinitionId), WorldStateCanonicalWriter.StringValue(right.ObjectiveTargetItemDefinitionId), differences);
                CompareValue("Expedition", identity, "ObjectiveTargetNotableItemRuntimeId", WorldStateCanonicalWriter.StringValue(left.ObjectiveTargetNotableItemRuntimeId), WorldStateCanonicalWriter.StringValue(right.ObjectiveTargetNotableItemRuntimeId), differences);
                CompareValue("Expedition", identity, "ObjectiveTargetOppositionRuntimeId", WorldStateCanonicalWriter.StringValue(left.ObjectiveTargetOppositionRuntimeId), WorldStateCanonicalWriter.StringValue(right.ObjectiveTargetOppositionRuntimeId), differences);
                CompareValue("Expedition", identity, "ObjectiveCompleted", WorldStateCanonicalWriter.BoolValue(left.ObjectiveCompleted), WorldStateCanonicalWriter.BoolValue(right.ObjectiveCompleted), differences);
                CompareValue("Expedition", identity, "ObjectiveAllowsContinueAfterCompletion", WorldStateCanonicalWriter.BoolValue(left.ObjectiveAllowsContinueAfterCompletion), WorldStateCanonicalWriter.BoolValue(right.ObjectiveAllowsContinueAfterCompletion), differences);
            }, differences);

        CompareEntities("PlaceContent", before.PlaceContents, after.PlaceContents, content => content.StableKey,
            (identity, left, right) =>
            {
                CompareValue("PlaceContent", identity, "OwnerKind", WorldStateCanonicalWriter.EnumValue(left.OwnerKind), WorldStateCanonicalWriter.EnumValue(right.OwnerKind), differences);
                CompareValue("PlaceContent", identity, "OwnerRuntimeId", WorldStateCanonicalWriter.StringValue(left.OwnerRuntimeId), WorldStateCanonicalWriter.StringValue(right.OwnerRuntimeId), differences);
                CompareValue("PlaceContent", identity, "MacroLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.MacroLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.MacroLocationRuntimeId), differences);
                CompareValue("PlaceContent", identity, "TopologyOwnerRuntimeId", WorldStateCanonicalWriter.StringValue(left.TopologyOwnerRuntimeId), WorldStateCanonicalWriter.StringValue(right.TopologyOwnerRuntimeId), differences);
                CompareValue("PlaceContent", identity, "SiteState", WorldStateCanonicalWriter.EnumValue(left.SiteState), WorldStateCanonicalWriter.EnumValue(right.SiteState), differences);
                CompareValue("PlaceContent", identity, "AccessState", WorldStateCanonicalWriter.EnumValue(left.AccessState), WorldStateCanonicalWriter.EnumValue(right.AccessState), differences);
                CompareValue("PlaceContent", identity, "ControllerRuntimeId", WorldStateCanonicalWriter.StringValue(left.ControllerRuntimeId), WorldStateCanonicalWriter.StringValue(right.ControllerRuntimeId), differences);
                CompareEntities("PlaceStack", left.Stacks, right.Stacks, stack => stack.ItemDefinitionId,
                    (stackIdentity, stackLeft, stackRight) =>
                    {
                        string stackKey = identity + "/item:" + stackIdentity;
                        CompareValue("PlaceStack", stackKey, "Amount", WorldStateCanonicalWriter.IntValue(stackLeft.Amount), WorldStateCanonicalWriter.IntValue(stackRight.Amount), differences);
                        CompareValue("PlaceStack", stackKey, "PersistencePolicy", WorldStateCanonicalWriter.EnumValue(stackLeft.PersistencePolicy), WorldStateCanonicalWriter.EnumValue(stackRight.PersistencePolicy), differences);
                        CompareValue("PlaceStack", stackKey, "DecayPerDay", WorldStateCanonicalWriter.IntValue(stackLeft.DecayPerDay), WorldStateCanonicalWriter.IntValue(stackRight.DecayPerDay), differences);
                        CompareValue("PlaceStack", stackKey, "AverageUnitCost", WorldStateCanonicalWriter.FloatValue(stackLeft.AverageUnitCost), WorldStateCanonicalWriter.FloatValue(stackRight.AverageUnitCost), differences);
                    }, differences, identity + "/item:");
                CompareEntities("PlaceOpposition", left.Oppositions, right.Oppositions, opposition => opposition.RuntimeId,
                    (oppositionIdentity, oppositionLeft, oppositionRight) =>
                    {
                        string oppositionKey = identity + "/opposition:" + oppositionIdentity;
                        CompareValue("PlaceOpposition", oppositionKey, "IsActive", WorldStateCanonicalWriter.BoolValue(oppositionLeft.IsActive), WorldStateCanonicalWriter.BoolValue(oppositionRight.IsActive), differences);
                        CompareValue("PlaceOpposition", oppositionKey, "IsResolved", WorldStateCanonicalWriter.BoolValue(oppositionLeft.IsResolved), WorldStateCanonicalWriter.BoolValue(oppositionRight.IsResolved), differences);
                        CompareValue("PlaceOpposition", oppositionKey, "OppositionSideId", WorldStateCanonicalWriter.StringValue(oppositionLeft.OppositionSideId), WorldStateCanonicalWriter.StringValue(oppositionRight.OppositionSideId), differences);
                        CompareValue("PlaceOpposition", oppositionKey, "NamedParticipantRuntimeIds", WorldStateCanonicalWriter.StringListValue(oppositionLeft.NamedParticipantRuntimeIds), WorldStateCanonicalWriter.StringListValue(oppositionRight.NamedParticipantRuntimeIds), differences);
                        CompareValue("PlaceOpposition", oppositionKey, "AggregateParticipantSourceIds", WorldStateCanonicalWriter.StringListValue(oppositionLeft.AggregateParticipantSourceIds), WorldStateCanonicalWriter.StringListValue(oppositionRight.AggregateParticipantSourceIds), differences);
                    }, differences, identity + "/opposition:");
            }, differences);

        CompareEntities("NotableItem", before.NotableItems, after.NotableItems, notable => notable.RuntimeId,
            (identity, left, right) =>
            {
                CompareValue("NotableItem", identity, "DefinitionId", WorldStateCanonicalWriter.StringValue(left.DefinitionId), WorldStateCanonicalWriter.StringValue(right.DefinitionId), differences);
                CompareValue("NotableItem", identity, "IsPresent", WorldStateCanonicalWriter.BoolValue(left.IsPresent), WorldStateCanonicalWriter.BoolValue(right.IsPresent), differences);
                CompareValue("NotableItem", identity, "Custody", WorldStateCanonicalWriter.StringValue(left.CustodyKey), WorldStateCanonicalWriter.StringValue(right.CustodyKey), differences);
                CompareValue("NotableItem", identity, "CustodyKind", WorldStateCanonicalWriter.EnumValueOrNull(left.CustodyKind), WorldStateCanonicalWriter.EnumValueOrNull(right.CustodyKind), differences);
                CompareValue("NotableItem", identity, "OwnerKind", WorldStateCanonicalWriter.EnumValueOrNull(left.OwnerKind), WorldStateCanonicalWriter.EnumValueOrNull(right.OwnerKind), differences);
                CompareValue("NotableItem", identity, "OwnerRuntimeId", WorldStateCanonicalWriter.StringValue(left.OwnerRuntimeId), WorldStateCanonicalWriter.StringValue(right.OwnerRuntimeId), differences);
                CompareValue("NotableItem", identity, "OwnerMacroLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.OwnerMacroLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.OwnerMacroLocationRuntimeId), differences);
                CompareValue("NotableItem", identity, "OwnerTopologyRuntimeId", WorldStateCanonicalWriter.StringValue(left.OwnerTopologyRuntimeId), WorldStateCanonicalWriter.StringValue(right.OwnerTopologyRuntimeId), differences);
                CompareValue("NotableItem", identity, "CustodianNpcRuntimeId", WorldStateCanonicalWriter.StringValue(left.CustodianNpcRuntimeId), WorldStateCanonicalWriter.StringValue(right.CustodianNpcRuntimeId), differences);
            }, differences);

        CompareEntities("LocalTopology", before.LocalTopologies, after.LocalTopologies, topology => topology.StableKey,
            (identity, left, right) =>
            {
                CompareValue("LocalTopology", identity, "OwnerKind", WorldStateCanonicalWriter.EnumValue(left.OwnerKind), WorldStateCanonicalWriter.EnumValue(right.OwnerKind), differences);
                CompareValue("LocalTopology", identity, "OwnerRuntimeId", WorldStateCanonicalWriter.StringValue(left.OwnerRuntimeId), WorldStateCanonicalWriter.StringValue(right.OwnerRuntimeId), differences);
                CompareValue("LocalTopology", identity, "MacroLocationRuntimeId", WorldStateCanonicalWriter.StringValue(left.MacroLocationRuntimeId), WorldStateCanonicalWriter.StringValue(right.MacroLocationRuntimeId), differences);
                CompareValue("LocalTopology", identity, "PublicationState", WorldStateCanonicalWriter.EnumValue(left.PublicationState), WorldStateCanonicalWriter.EnumValue(right.PublicationState), differences);
                CompareEntities("LocalPlace", left.Places, right.Places, place => place.RuntimeId,
                    (placeIdentity, placeLeft, placeRight) =>
                    {
                        string placeKey = identity + "/place:" + placeIdentity;
                        CompareValue("LocalPlace", placeKey, "DefinitionId", WorldStateCanonicalWriter.StringValue(placeLeft.DefinitionId), WorldStateCanonicalWriter.StringValue(placeRight.DefinitionId), differences);
                        CompareValue("LocalPlace", placeKey, "ParentRuntimeId", WorldStateCanonicalWriter.StringValue(placeLeft.ParentRuntimeId), WorldStateCanonicalWriter.StringValue(placeRight.ParentRuntimeId), differences);
                        CompareValue("LocalPlace", placeKey, "IsEntryPoint", WorldStateCanonicalWriter.BoolValue(placeLeft.IsEntryPoint), WorldStateCanonicalWriter.BoolValue(placeRight.IsEntryPoint), differences);
                    }, differences, identity + "/place:");
                CompareEntities("LocalConnection", left.Connections, right.Connections, connection => connection.RuntimeId,
                    (connectionIdentity, connectionLeft, connectionRight) =>
                    {
                        string connectionKey = identity + "/connection:" + connectionIdentity;
                        CompareValue("LocalConnection", connectionKey, "OriginRuntimeId", WorldStateCanonicalWriter.StringValue(connectionLeft.OriginRuntimeId), WorldStateCanonicalWriter.StringValue(connectionRight.OriginRuntimeId), differences);
                        CompareValue("LocalConnection", connectionKey, "DestinationRuntimeId", WorldStateCanonicalWriter.StringValue(connectionLeft.DestinationRuntimeId), WorldStateCanonicalWriter.StringValue(connectionRight.DestinationRuntimeId), differences);
                        CompareValue("LocalConnection", connectionKey, "TraversalCost", WorldStateCanonicalWriter.FloatValue(connectionLeft.TraversalCost), WorldStateCanonicalWriter.FloatValue(connectionRight.TraversalCost), differences);
                        CompareValue("LocalConnection", connectionKey, "ConnectionTypeDefinitionId", WorldStateCanonicalWriter.StringValue(connectionLeft.ConnectionTypeDefinitionId), WorldStateCanonicalWriter.StringValue(connectionRight.ConnectionTypeDefinitionId), differences);
                    }, differences, identity + "/connection:");
            }, differences);

        return new WorldStateDiff(differences);
    }

    private static void CompareCalendar(
        WorldStateCalendarSnapshot before,
        WorldStateCalendarSnapshot after,
        List<WorldStateDifference> differences)
    {
        if (before == null || after == null)
        {
            ComparePresence("Calendar", "world", before, after, differences);
            return;
        }

        CompareValue("Calendar", "world", "Year", WorldStateCanonicalWriter.Int64Value(before.Year), WorldStateCanonicalWriter.Int64Value(after.Year), differences);
        CompareValue("Calendar", "world", "Month", WorldStateCanonicalWriter.IntValue(before.Month), WorldStateCanonicalWriter.IntValue(after.Month), differences);
        CompareValue("Calendar", "world", "WeekOfMonth", WorldStateCanonicalWriter.IntValue(before.WeekOfMonth), WorldStateCanonicalWriter.IntValue(after.WeekOfMonth), differences);
        CompareValue("Calendar", "world", "DayOfMonth", WorldStateCanonicalWriter.IntValue(before.DayOfMonth), WorldStateCanonicalWriter.IntValue(after.DayOfMonth), differences);
        CompareValue("Calendar", "world", "DayOfWeek", WorldStateCanonicalWriter.IntValue(before.DayOfWeek), WorldStateCanonicalWriter.IntValue(after.DayOfWeek), differences);
        CompareValue("Calendar", "world", "DayOfYear", WorldStateCanonicalWriter.Int64Value(before.DayOfYear), WorldStateCanonicalWriter.Int64Value(after.DayOfYear), differences);
        CompareValue("Calendar", "world", "DaysPerMonth", WorldStateCanonicalWriter.Int64Value(before.DaysPerMonth), WorldStateCanonicalWriter.Int64Value(after.DaysPerMonth), differences);
        CompareValue("Calendar", "world", "DaysPerYear", WorldStateCanonicalWriter.Int64Value(before.DaysPerYear), WorldStateCanonicalWriter.Int64Value(after.DaysPerYear), differences);
    }

    private static void CompareAction(
        string npcIdentity,
        WorldStateActionSnapshot before,
        WorldStateActionSnapshot after,
        List<WorldStateDifference> differences)
    {
        const string section = "NpcAction";
        string identity = npcIdentity + "/action";
        if (before == null || after == null)
        {
            ComparePresence(section, identity, before, after, differences);
            if (before == null && after != null)
            {
                CompareValue(section, identity, "DefinitionId", null, WorldStateCanonicalWriter.StringValue(after.DefinitionId), differences);
            }
            else if (before != null && after == null)
            {
                CompareValue(section, identity, "DefinitionId", WorldStateCanonicalWriter.StringValue(before.DefinitionId), null, differences);
            }

            return;
        }

        CompareValue(section, identity, "DefinitionId", WorldStateCanonicalWriter.StringValue(before.DefinitionId), WorldStateCanonicalWriter.StringValue(after.DefinitionId), differences);
        CompareValue(section, identity, "ActionName", WorldStateCanonicalWriter.StringValue(before.ActionName), WorldStateCanonicalWriter.StringValue(after.ActionName), differences);
        CompareValue(section, identity, "Category", WorldStateCanonicalWriter.EnumValue(before.Category), WorldStateCanonicalWriter.EnumValue(after.Category), differences);
        CompareValue(section, identity, "Type", WorldStateCanonicalWriter.EnumValue(before.Type), WorldStateCanonicalWriter.EnumValue(after.Type), differences);
        CompareValue(section, identity, "TargetNpcRuntimeId", WorldStateCanonicalWriter.StringValue(before.TargetNpcRuntimeId), WorldStateCanonicalWriter.StringValue(after.TargetNpcRuntimeId), differences);
        CompareValue(section, identity, "TargetCityRuntimeId", WorldStateCanonicalWriter.StringValue(before.TargetCityRuntimeId), WorldStateCanonicalWriter.StringValue(after.TargetCityRuntimeId), differences);
        CompareValue(section, identity, "TargetItemDefinitionId", WorldStateCanonicalWriter.StringValue(before.TargetItemDefinitionId), WorldStateCanonicalWriter.StringValue(after.TargetItemDefinitionId), differences);
        CompareValue(section, identity, "Amount", WorldStateCanonicalWriter.IntValue(before.Amount), WorldStateCanonicalWriter.IntValue(after.Amount), differences);
        CompareValue(section, identity, "ExpectedUnitPrice", WorldStateCanonicalWriter.FloatValue(before.ExpectedUnitPrice), WorldStateCanonicalWriter.FloatValue(after.ExpectedUnitPrice), differences);
        CompareValue(section, identity, "SuccessChanceMultiplier", WorldStateCanonicalWriter.FloatValue(before.SuccessChanceMultiplier), WorldStateCanonicalWriter.FloatValue(after.SuccessChanceMultiplier), differences);
        CompareValue(section, identity, "TravelReason", WorldStateCanonicalWriter.EnumValue(before.TravelReason), WorldStateCanonicalWriter.EnumValue(after.TravelReason), differences);
        CompareValue(section, identity, "ExpectedNetValue", WorldStateCanonicalWriter.FloatValue(before.ExpectedNetValue), WorldStateCanonicalWriter.FloatValue(after.ExpectedNetValue), differences);
        CompareValue(section, identity, "OriginDecisionId", WorldStateCanonicalWriter.StringValue(before.OriginDecisionId), WorldStateCanonicalWriter.StringValue(after.OriginDecisionId), differences);
    }

    private static void CompareMerchantTradePlan(
        string npcIdentity,
        WorldStateMerchantTradePlanSnapshot before,
        WorldStateMerchantTradePlanSnapshot after,
        List<WorldStateDifference> differences)
    {
        const string section = "MerchantTradePlan";
        string identity = npcIdentity + "/trade-plan";
        if (before == null || after == null)
        {
            ComparePresence(section, identity, before, after, differences);
            if (before == null && after != null)
            {
                CompareValue(section, identity, "ItemDefinitionId", null, WorldStateCanonicalWriter.StringValue(after.ItemDefinitionId), differences);
            }
            else if (before != null && after == null)
            {
                CompareValue(section, identity, "ItemDefinitionId", WorldStateCanonicalWriter.StringValue(before.ItemDefinitionId), null, differences);
            }

            return;
        }

        CompareValue(section, identity, "HasData", WorldStateCanonicalWriter.BoolValue(before.HasData), WorldStateCanonicalWriter.BoolValue(after.HasData), differences);
        CompareValue(section, identity, "IsActive", WorldStateCanonicalWriter.BoolValue(before.IsActive), WorldStateCanonicalWriter.BoolValue(after.IsActive), differences);
        CompareValue(section, identity, "ItemDefinitionId", WorldStateCanonicalWriter.StringValue(before.ItemDefinitionId), WorldStateCanonicalWriter.StringValue(after.ItemDefinitionId), differences);
        CompareValue(section, identity, "OriginCityRuntimeId", WorldStateCanonicalWriter.StringValue(before.OriginCityRuntimeId), WorldStateCanonicalWriter.StringValue(after.OriginCityRuntimeId), differences);
        CompareValue(section, identity, "TargetCityRuntimeId", WorldStateCanonicalWriter.StringValue(before.TargetCityRuntimeId), WorldStateCanonicalWriter.StringValue(after.TargetCityRuntimeId), differences);
        CompareValue(section, identity, "PlannedAmount", WorldStateCanonicalWriter.IntValue(before.PlannedAmount), WorldStateCanonicalWriter.IntValue(after.PlannedAmount), differences);
        CompareValue(section, identity, "RemainingAmount", WorldStateCanonicalWriter.IntValue(before.RemainingAmount), WorldStateCanonicalWriter.IntValue(after.RemainingAmount), differences);
        CompareValue(section, identity, "PurchasePricePerItem", WorldStateCanonicalWriter.FloatValue(before.PurchasePricePerItem), WorldStateCanonicalWriter.FloatValue(after.PurchasePricePerItem), differences);
        CompareValue(section, identity, "WaitDaysAtDestination", WorldStateCanonicalWriter.IntValue(before.WaitDaysAtDestination), WorldStateCanonicalWriter.IntValue(after.WaitDaysAtDestination), differences);
        CompareValue(section, identity, "PendingTravelDays", WorldStateCanonicalWriter.IntValue(before.PendingTravelDays), WorldStateCanonicalWriter.IntValue(after.PendingTravelDays), differences);
        CompareValue(section, identity, "OriginDecisionId", WorldStateCanonicalWriter.StringValue(before.OriginDecisionId), WorldStateCanonicalWriter.StringValue(after.OriginDecisionId), differences);
    }

    private static void CompareInventory(
        string npcIdentity,
        IReadOnlyList<WorldStateInventoryStackSnapshot> before,
        IReadOnlyList<WorldStateInventoryStackSnapshot> after,
        List<WorldStateDifference> differences)
    {
        CompareEntities("NpcInventory", before, after, stack => stack.ItemDefinitionId,
            (identity, left, right) =>
            {
                string stackKey = npcIdentity + "/item:" + identity;
                CompareValue("NpcInventory", stackKey, "Amount", WorldStateCanonicalWriter.IntValue(left.Amount), WorldStateCanonicalWriter.IntValue(right.Amount), differences);
                CompareValue("NpcInventory", stackKey, "AverageUnitCost", WorldStateCanonicalWriter.FloatValue(left.AverageUnitCost), WorldStateCanonicalWriter.FloatValue(right.AverageUnitCost), differences);
            }, differences, npcIdentity + "/item:");
    }

    private delegate void EntityFields<T>(string identity, T before, T after);

    private static void CompareEntities<T>(
        string section,
        IReadOnlyList<T> before,
        IReadOnlyList<T> after,
        Func<T, string> identitySelector,
        EntityFields<T> compareFields,
        List<WorldStateDifference> differences,
        string identityPrefix = "")
    {
        Dictionary<string, T> beforeById = Index(before, identitySelector);
        Dictionary<string, T> afterById = Index(after, identitySelector);
        SortedSet<string> identities = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string identity in beforeById.Keys) identities.Add(identity);
        foreach (string identity in afterById.Keys) identities.Add(identity);

        foreach (string identity in identities)
        {
            bool hasBefore = beforeById.TryGetValue(identity, out T beforeValue);
            bool hasAfter = afterById.TryGetValue(identity, out T afterValue);
            string fullIdentity = identityPrefix + identity;
            if (hasBefore == false)
            {
                differences.Add(new WorldStateDifference(section, fullIdentity, "Entity", null, "present", WorldStateDifferenceChangeKind.Added));
            }
            else if (hasAfter == false)
            {
                differences.Add(new WorldStateDifference(section, fullIdentity, "Entity", "present", null, WorldStateDifferenceChangeKind.Removed));
            }
            else
            {
                compareFields(identity, beforeValue, afterValue);
            }
        }
    }

    private static Dictionary<string, T> Index<T>(IReadOnlyList<T> values, Func<T, string> identitySelector)
    {
        Dictionary<string, T> result = new Dictionary<string, T>(StringComparer.Ordinal);
        if (values == null)
        {
            return result;
        }

        foreach (T value in values)
        {
            string identity = identitySelector(value);
            if (string.IsNullOrWhiteSpace(identity) == false)
            {
                result[identity] = value;
            }
        }

        return result;
    }

    private static void ComparePresence(
        string section,
        string identity,
        object before,
        object after,
        List<WorldStateDifference> differences)
    {
        if (before == null && after == null) return;
        differences.Add(new WorldStateDifference(
            section,
            identity,
            "Entity",
            before == null ? null : "present",
            after == null ? null : "present",
            before == null ? WorldStateDifferenceChangeKind.Added : WorldStateDifferenceChangeKind.Removed));
    }

    private static void CompareValue(
        string section,
        string identity,
        string field,
        string beforeValue,
        string afterValue,
        List<WorldStateDifference> differences)
    {
        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal) == false)
        {
            differences.Add(new WorldStateDifference(
                section,
                identity,
                field,
                beforeValue,
                afterValue,
                WorldStateDifferenceChangeKind.Changed));
        }
    }

    private static void SortDifferences(List<WorldStateDifference> differences)
    {
        differences.Sort((left, right) =>
        {
            int comparison = SectionOrder(left.Section).CompareTo(SectionOrder(right.Section));
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Section, right.Section);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Identity, right.Identity);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Field, right.Field);
            if (comparison != 0) return comparison;
            return left.ChangeKind.CompareTo(right.ChangeKind);
        });
    }

    private static int SectionOrder(string section)
    {
        switch (section)
        {
            case "NPC": return 0;
            case "NpcAction": return 1;
            case "MerchantTradePlan": return 2;
            case "NpcInventory": return 3;
            case "Parentage": return 4;
            case "Metadata": return 4;
            case "Calendar": return 5;
            case "City": return 6;
            case "CityStock": return 7;
            case "Location": return 8;
            case "Route": return 9;
            case "Site": return 10;
            case "Expedition": return 11;
            case "PlaceContent": return 12;
            case "PlaceStack": return 13;
            case "PlaceOpposition": return 14;
            case "NotableItem": return 15;
            case "LocalTopology": return 16;
            case "LocalPlace": return 17;
            case "LocalConnection": return 18;
            default: return 100;
        }
    }

    private static string ParentageIdentity(WorldStateParentageSnapshot parentage)
    {
        if (parentage == null)
        {
            return null;
        }

        return (parentage.ParentPersonId ?? string.Empty)
            + " -> "
            + (parentage.ChildPersonId ?? string.Empty);
    }

    private static string PropertyTransferIdentity(WorldStatePropertyTransferSnapshot transfer)
    {
        if (transfer == null)
        {
            return null;
        }

        return (transfer.PropertyId ?? string.Empty)
            + "@"
            + WorldStateCanonicalWriter.Int64Value(transfer.TransferAbsoluteDay)
            + ":"
            + (transfer.PreviousOwnerPersonId ?? string.Empty)
            + ":"
            + (transfer.NewOwnerPersonId ?? string.Empty);
    }
}
