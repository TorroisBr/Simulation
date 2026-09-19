using System;
using System.Collections.Generic;
using System.Text;

public static class WorldStateSnapshotFormatter
{
    public static string Format(WorldStateSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "WORLD <null>\n";
        }

        StringBuilder output = new StringBuilder();
        output.Append("WORLD DAY ").Append(WorldStateCanonicalWriter.Int64Value(snapshot.AbsoluteDay)).Append('\n');
        if (snapshot.Metadata.CalendarDate != null)
        {
            WorldStateCalendarSnapshot date = snapshot.Metadata.CalendarDate;
            output.Append("Calendar: Year ").Append(WorldStateCanonicalWriter.Int64Value(date.Year))
                .Append(" / Month ").Append(WorldStateCanonicalWriter.IntValue(date.Month))
                .Append(" / Day ").Append(WorldStateCanonicalWriter.IntValue(date.DayOfMonth)).Append('\n');
        }

        output.Append("Settlements: ").Append(WorldStateCanonicalWriter.IntValue(snapshot.SettlementCount))
            .Append(" | Known NPCs: ").Append(WorldStateCanonicalWriter.IntValue(snapshot.KnownNpcCount)).Append('\n');

        foreach (WorldStatePersonSnapshot person in snapshot.Persons)
        {
            if (person == null)
            {
                continue;
            }

            output.Append('\n').Append("PERSON ")
                .Append(DisplayOrId(null, person.PersonId))
                .Append(" [").Append(WorldStateCanonicalWriter.StringValue(person.PersonId)).Append("]\n");
            output.Append("Birth day: ")
                .Append(person.BirthAbsoluteDay.HasValue
                    ? WorldStateCanonicalWriter.Int64Value(person.BirthAbsoluteDay.Value)
                    : "unknown")
                .Append('\n');
            output.Append("Age in days: ")
                .Append(person.AgeInDays.HasValue
                    ? WorldStateCanonicalWriter.Int64Value(person.AgeInDays.Value)
                    : "unknown")
                .Append('\n');
            output.Append("Completed years: ")
                .Append(person.CompletedYears.HasValue
                    ? WorldStateCanonicalWriter.Int64Value(person.CompletedYears.Value)
                    : "unknown")
                .Append('\n');
            output.Append("Residence: ").Append(Value(person.ResidenceSettlementRuntimeId)).Append('\n');
            output.Append("Materialized NPC: ").Append(Value(person.MaterializedNpcRuntimeId)).Append('\n');
        }

        foreach (WorldStateCitySnapshot city in snapshot.Cities)
        {
            if (city == null)
            {
                continue;
            }

            output.Append('\n').Append("SETTLEMENT ").Append(DisplayOrId(city.CityName, city.RuntimeId))
                .Append(" [").Append(WorldStateCanonicalWriter.StringValue(city.RuntimeId)).Append("]\n");
            output.Append("Definition: ").Append(Value(city.DefinitionId)).Append('\n');
            output.Append("Population: ").Append(WorldStateCanonicalWriter.IntValue(city.CurrentPopulation)).Append('\n');
            output.Append("Revision: ").Append(WorldStateCanonicalWriter.Int64Value(city.PopulationRevision)).Append('\n');
            output.Append("Named residents: ").Append(WorldStateCanonicalWriter.IntValue(city.NamedResidentCount)).Append('\n');
            output.Append("Named present: ").Append(WorldStateCanonicalWriter.IntValue(city.NamedPresentCount)).Append('\n');
            output.Append("Market balance: ").Append(WorldStateCanonicalWriter.FloatValue(city.MarketBalance)).Append('\n');

            foreach (WorldStateMarketStackSnapshot stock in city.MarketStock)
            {
                if (stock != null)
                {
                    output.Append("Stock ").Append(Value(stock.ItemDefinitionId)).Append(": ")
                        .Append(WorldStateCanonicalWriter.IntValue(stock.Amount))
                        .Append(" / desired ").Append(WorldStateCanonicalWriter.IntValue(stock.DesiredAmount))
                        .Append(" / price ").Append(WorldStateCanonicalWriter.FloatValue(stock.CurrentPrice)).Append('\n');
                }
            }
        }

        foreach (WorldStateNpcSnapshot npc in snapshot.Npcs)
        {
            if (npc == null)
            {
                continue;
            }

            output.Append('\n').Append("NPC ").Append(DisplayOrId(npc.Name, npc.RuntimeId))
                .Append(" [").Append(WorldStateCanonicalWriter.StringValue(npc.RuntimeId)).Append("]\n");
            output.Append("Life: ").Append(WorldStateCanonicalWriter.EnumValue(npc.LifeState)).Append('\n');
            output.Append("Injury: ").Append(WorldStateCanonicalWriter.EnumValue(npc.InjurySeverity)).Append('\n');
            output.Append("Residence: ").Append(Value(npc.ResidenceSettlementRuntimeId)).Append('\n');
            output.Append("Presence: ").Append(Value(npc.CurrentCityRuntimeId ?? npc.CurrentLocationRuntimeId)).Append('\n');
            output.Append("Destination: ").Append(Value(npc.DestinationCityRuntimeId ?? npc.DestinationLocationRuntimeId)).Append('\n');
            output.Append("Traveling: ").Append(WorldStateCanonicalWriter.BoolValue(npc.IsTraveling)).Append('\n');
            output.Append("Travel remaining: ").Append(WorldStateCanonicalWriter.IntValue(npc.RemainingTravelDays)).Append('\n');
            output.Append("Money: ").Append(WorldStateCanonicalWriter.FloatValue(npc.MoneyBalance)).Append('\n');

            if (npc.StatusNames.Count > 0)
            {
                output.Append("Statuses: ").Append(WorldStateCanonicalWriter.StringListValue(npc.StatusNames)).Append('\n');
            }

            if (npc.CurrentAction != null)
            {
                WorldStateActionSnapshot action = npc.CurrentAction;
                output.Append("Action: ").Append(Value(action.DefinitionId)).Append(" (")
                    .Append(WorldStateCanonicalWriter.EnumValue(action.Type)).Append(")\n");
                output.Append("Action target NPC: ").Append(Value(action.TargetNpcRuntimeId)).Append('\n');
                output.Append("Action target settlement: ").Append(Value(action.TargetCityRuntimeId)).Append('\n');
                output.Append("Action target item: ").Append(Value(action.TargetItemDefinitionId)).Append('\n');
            }

            if (npc.MerchantTradePlan != null)
            {
                WorldStateMerchantTradePlanSnapshot plan = npc.MerchantTradePlan;
                output.Append("Trade plan: ").Append(Value(plan.ItemDefinitionId))
                    .Append(" ").Append(WorldStateCanonicalWriter.IntValue(plan.RemainingAmount))
                    .Append(" remaining, target ").Append(Value(plan.TargetCityRuntimeId)).Append('\n');
            }

            foreach (WorldStateInventoryStackSnapshot stack in npc.Inventory)
            {
                if (stack != null)
                {
                    output.Append("Inventory ").Append(Value(stack.ItemDefinitionId)).Append(": ")
                        .Append(WorldStateCanonicalWriter.IntValue(stack.Amount))
                        .Append(" @ ").Append(WorldStateCanonicalWriter.FloatValue(stack.AverageUnitCost)).Append('\n');
                }
            }
        }

        return output.ToString();
    }

    private static string DisplayOrId(string displayName, string runtimeId)
    {
        return string.IsNullOrWhiteSpace(displayName) ? Value(runtimeId) : displayName;
    }

    private static string Value(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}

public static class WorldStateDiffFormatter
{
    public static string Format(WorldStateDiff diff)
    {
        if (diff == null)
        {
            return "WORLD DIFF <null>\n";
        }

        StringBuilder output = new StringBuilder("WORLD DIFF\n");
        if (diff.IsEmpty)
        {
            output.Append("No changes.\n");
            return output.ToString();
        }

        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference == null)
            {
                continue;
            }

            output.Append(difference.Section).Append(' ')
                .Append(difference.Identity).Append(' ')
                .Append(difference.Field).Append(": ")
                .Append(DisplayValue(difference.BeforeValue)).Append(" -> ")
                .Append(DisplayValue(difference.AfterValue)).Append('\n');
        }

        return output.ToString();
    }

    private static string DisplayValue(string value)
    {
        return value == null || value == "~" ? "none" : value;
    }
}
