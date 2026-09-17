using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class WorldStateCanonicalWriter
{
    public static string Write(WorldStateSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return string.Empty;
        }

        StringBuilder output = new StringBuilder();
        AppendLine(output, "METADATA", "AbsoluteDay", Int64Value(snapshot.AbsoluteDay));

        foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
        {
            AppendLine(output, "NPC",
                npc.RuntimeId,
                npc.DefinitionId,
                npc.ResidenceSettlementRuntimeId,
                EnumValue(npc.LifeState),
                EnumValue(npc.InjurySeverity),
                npc.CurrentLocationRuntimeId,
                npc.CurrentCityRuntimeId,
                npc.DestinationLocationRuntimeId,
                npc.DestinationCityRuntimeId,
                BoolValue(npc.IsTraveling),
                npc.TravelRouteRuntimeId,
                IntValue(npc.RemainingTravelDays),
                npc.ActiveTravelPartyId,
                FloatValue(npc.MoneyBalance),
                npc.ActiveExpeditionId);

            foreach (WorldStateInventoryStackSnapshot stack in npc.Inventory)
            {
                AppendLine(output, "NPC_STACK",
                    npc.RuntimeId,
                    stack.ItemDefinitionId,
                    IntValue(stack.Amount),
                    FloatValue(stack.AverageUnitCost));
            }
        }

        foreach (WorldStateCitySnapshot city in snapshot.Cities)
        {
            AppendLine(output, "CITY",
                city.RuntimeId,
                city.DefinitionId,
                city.LocationRuntimeId,
                IntValue(city.CurrentPopulation),
                city.MarketCounterpartyRuntimeId,
                EnumValue(city.MarketLiquidityMode),
                FloatValue(city.MarketBalance));

            foreach (string residentNpcRuntimeId in city.ResidentNpcRuntimeIds)
            {
                AppendLine(output, "CITY_RESIDENT", city.RuntimeId, residentNpcRuntimeId);
            }

            foreach (WorldStateMarketStackSnapshot stock in city.MarketStock)
            {
                AppendLine(output, "CITY_STOCK",
                    city.RuntimeId,
                    stock.ItemDefinitionId,
                    IntValue(stock.Amount),
                    IntValue(stock.DesiredAmount),
                    FloatValue(stock.CurrentPrice));
            }
        }

        foreach (WorldStateLocationSnapshot location in snapshot.Spatial.Locations)
        {
            AppendLine(output, "LOCATION", location.RuntimeId);
        }

        foreach (WorldStateRouteSnapshot route in snapshot.Spatial.Routes)
        {
            AppendLine(output, "ROUTE",
                route.RuntimeId,
                route.OriginRuntimeId,
                route.DestinationRuntimeId,
                IntValue(route.TravelDays));
        }

        foreach (WorldStateSiteSnapshot site in snapshot.Sites)
        {
            AppendLine(output, "SITE",
                site.RuntimeId,
                site.DefinitionId,
                site.LocationRuntimeId,
                EnumValue(site.SiteKind));
        }

        foreach (WorldStateExpeditionSnapshot expedition in snapshot.Expeditions)
        {
            AppendLine(output, "EXPEDITION",
                expedition.ExpeditionId,
                EnumValue(expedition.State),
                expedition.TargetSiteRuntimeId,
                expedition.OriginLocationRuntimeId,
                expedition.TargetLocationRuntimeId,
                expedition.OutboundRouteRuntimeId,
                expedition.OriginDecisionId,
                expedition.TravelPartyId,
                expedition.CurrentLocalPlaceRuntimeId,
                IntValue(expedition.ExplorationProgress),
                IntValue(expedition.ExplorationProgressRequired));
            AppendLine(output, "EXPEDITION_OBJECTIVE",
                expedition.ExpeditionId,
                EnumValue(expedition.ObjectiveType),
                expedition.ObjectiveTargetItemDefinitionId,
                expedition.ObjectiveTargetNotableItemRuntimeId,
                expedition.ObjectiveTargetOppositionRuntimeId,
                BoolValue(expedition.ObjectiveCompleted),
                BoolValue(expedition.ObjectiveAllowsContinueAfterCompletion));
            AppendIds(output, "EXPEDITION_MEMBER", expedition.ExpeditionId, expedition.MemberRuntimeIds);
            AppendIds(output, "EXPEDITION_PERFORMER", expedition.ExpeditionId, expedition.PerformerRuntimeIds);
            AppendIds(output, "EXPEDITION_SUPPORT", expedition.ExpeditionId, expedition.SupportRuntimeIds);
            AppendIds(output, "EXPEDITION_VISITED", expedition.ExpeditionId, expedition.VisitedLocalPlaceRuntimeIds);
            AppendIds(output, "EXPEDITION_OBSERVED_CONNECTION", expedition.ExpeditionId, expedition.ObservedLocalConnectionRuntimeIds);
        }

        foreach (WorldStatePlaceContentSnapshot content in snapshot.PlaceContents)
        {
            AppendLine(output, "PLACE",
                content.StableKey,
                EnumValue(content.OwnerKind),
                content.OwnerRuntimeId,
                content.MacroLocationRuntimeId,
                content.TopologyOwnerRuntimeId,
                EnumValue(content.SiteState),
                EnumValue(content.AccessState),
                content.ControllerRuntimeId);

            foreach (WorldStatePlaceStackSnapshot stack in content.Stacks)
            {
                AppendLine(output, "PLACE_STACK",
                    content.StableKey,
                    stack.ItemDefinitionId,
                    IntValue(stack.Amount),
                    EnumValue(stack.PersistencePolicy),
                    IntValue(stack.DecayPerDay),
                    FloatValue(stack.AverageUnitCost));
            }

            foreach (WorldStatePlaceOppositionSnapshot opposition in content.Oppositions)
            {
                AppendLine(output, "PLACE_OPPOSITION",
                    content.StableKey,
                    opposition.RuntimeId,
                    BoolValue(opposition.IsActive),
                    BoolValue(opposition.IsResolved),
                    opposition.OppositionSideId);
                AppendIds(output, "PLACE_OPPOSITION_NAMED", content.StableKey + ":" + opposition.RuntimeId, opposition.NamedParticipantRuntimeIds);
                AppendIds(output, "PLACE_OPPOSITION_AGGREGATE", content.StableKey + ":" + opposition.RuntimeId, opposition.AggregateParticipantSourceIds);
            }
        }

        foreach (WorldStateNotableItemSnapshot notable in snapshot.NotableItems)
        {
            AppendLine(output, "NOTABLE",
                notable.RuntimeId,
                notable.DefinitionId,
                BoolValue(notable.IsPresent),
                EnumValueOrNull(notable.CustodyKind),
                notable.CustodyKey,
                EnumValueOrNull(notable.OwnerKind),
                notable.OwnerRuntimeId,
                notable.OwnerMacroLocationRuntimeId,
                notable.OwnerTopologyRuntimeId,
                notable.CustodianNpcRuntimeId);
        }

        foreach (WorldStateLocalTopologySnapshot topology in snapshot.LocalTopologies)
        {
            AppendLine(output, "LOCAL_TOPOLOGY",
                topology.StableKey,
                EnumValue(topology.OwnerKind),
                topology.OwnerRuntimeId,
                topology.MacroLocationRuntimeId,
                EnumValue(topology.PublicationState));

            foreach (WorldStateLocalPlaceSnapshot place in topology.Places)
            {
                AppendLine(output, "LOCAL_PLACE",
                    topology.StableKey,
                    place.RuntimeId,
                    place.DefinitionId,
                    place.ParentRuntimeId,
                    BoolValue(place.IsEntryPoint));
            }

            foreach (WorldStateLocalConnectionSnapshot connection in topology.Connections)
            {
                AppendLine(output, "LOCAL_CONNECTION",
                    topology.StableKey,
                    connection.RuntimeId,
                    connection.OriginRuntimeId,
                    connection.DestinationRuntimeId,
                    FloatValue(connection.TraversalCost),
                    connection.ConnectionTypeDefinitionId);
            }
        }

        return output.ToString();
    }

    public static string EscapeValue(string value)
    {
        if (value == null)
        {
            return "~";
        }

        StringBuilder escaped = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            switch (character)
            {
                case '\\': escaped.Append("\\\\"); break;
                case '|': escaped.Append("\\|"); break;
                case '\n': escaped.Append("\\n"); break;
                case '\r': escaped.Append("\\r"); break;
                case '\t': escaped.Append("\\t"); break;
                case '~': escaped.Append("\\~"); break;
                default: escaped.Append(character); break;
            }
        }

        return escaped.ToString();
    }

    public static string StringValue(string value)
    {
        return EscapeValue(value);
    }

    public static string IntValue(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string Int64Value(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string FloatValue(float value)
    {
        if (float.IsNaN(value)) return "NaN";
        if (float.IsPositiveInfinity(value)) return "+Infinity";
        if (float.IsNegativeInfinity(value)) return "-Infinity";
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    public static string BoolValue(bool value)
    {
        return value ? "true" : "false";
    }

    public static string EnumValue(Enum value)
    {
        if (value == null)
        {
            return "~";
        }

        string name = Enum.GetName(value.GetType(), value);
        return name ?? Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    public static string EnumValueOrNull<TEnum>(TEnum? value) where TEnum : struct
    {
        return value.HasValue ? EnumValue((Enum)(object)value.Value) : "~";
    }

    public static string StringListValue(IReadOnlyList<string> values)
    {
        if (values == null || values.Count == 0)
        {
            return "[]";
        }

        StringBuilder result = new StringBuilder("[");
        for (int index = 0; index < values.Count; index++)
        {
            if (index > 0) result.Append(',');
            result.Append(EscapeValue(values[index]));
        }

        result.Append(']');
        return result.ToString();
    }

    private static void AppendIds(
        StringBuilder output,
        string record,
        string ownerId,
        IReadOnlyList<string> ids)
    {
        foreach (string id in ids)
        {
            AppendLine(output, record, ownerId, id);
        }
    }

    private static void AppendLine(StringBuilder output, string record, params string[] fields)
    {
        output.Append(record);
        foreach (string field in fields)
        {
            output.Append('|');
            output.Append(EncodeField(field));
        }

        output.Append('\n');
    }

    private static string EncodeField(string field)
    {
        return field == null ? "~" : EscapeValue(field);
    }
}
