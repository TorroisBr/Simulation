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
            .Append(" | Known NPCs: ").Append(WorldStateCanonicalWriter.IntValue(snapshot.KnownNpcCount))
            .Append(" | Political claims: ").Append(WorldStateCanonicalWriter.IntValue(snapshot.PoliticalClaimCount))
            .Append(" | Factions: ").Append(WorldStateCanonicalWriter.IntValue(snapshot.FactionCount))
            .Append(" | Faction affiliations: ").Append(WorldStateCanonicalWriter.IntValue(snapshot.FactionAffiliationCount))
            .Append('\n');

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
            output.Append("Death day: ")
                .Append(person.DeathAbsoluteDay.HasValue
                    ? WorldStateCanonicalWriter.Int64Value(person.DeathAbsoluteDay.Value)
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

        if (snapshot.HasArmedForceState)
        {
            output.Append("\nArmed force revision: ")
                .Append(WorldStateCanonicalWriter.Int64Value(snapshot.ArmedForceRevision.Value))
                .Append('\n');
            foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces)
            {
                if (force == null) continue;
                output.Append("ARMED FORCE ")
                    .Append(Value(force.ArmedForceId))
                    .Append(" parent ")
                    .Append(Value(force.ParentForceId))
                    .Append(" lifecycle ")
                    .Append(WorldStateCanonicalWriter.EnumValue(force.LifecycleState))
                    .Append(" detached ")
                    .Append(WorldStateCanonicalWriter.BoolValue(force.IsDetached))
                    .Append(" location ")
                    .Append(Value(force.OperationalLocationReference))
                    .Append('\n');
            }

            foreach (WorldStateArmedForceContingentSnapshot contingent in snapshot.ArmedForceContingents)
            {
                if (contingent == null) continue;
                output.Append("ARMED FORCE CONTINGENT ")
                    .Append(Value(contingent.ContingentId))
                    .Append(" force ")
                    .Append(Value(contingent.ForceId))
                    .Append(" amount ")
                    .Append(WorldStateCanonicalWriter.Int64Value(contingent.Amount))
                    .Append('\n');
            }
        }

        AppendConflictWarBattle(output, snapshot);

        foreach (WorldStateParentageSnapshot parentage in snapshot.Parentages)
        {
            if (parentage != null)
            {
                output.Append("PARENTAGE ")
                    .Append(Value(parentage.ParentPersonId))
                    .Append(" -> ")
                    .Append(Value(parentage.ChildPersonId))
                    .Append('\n');
            }
        }

        foreach (WorldStatePropertyOwnershipSnapshot ownership in snapshot.PropertyOwnerships)
        {
            if (ownership != null)
            {
                output.Append("PROPERTY ")
                    .Append(Value(ownership.PropertyId))
                    .Append(" owner ")
                    .Append(Value(ownership.OwnerPersonId))
                    .Append('\n');
            }
        }

        foreach (WorldStatePropertyTransferSnapshot transfer in snapshot.PropertyTransfers)
        {
            if (transfer != null)
            {
                output.Append("PROPERTY TRANSFER ")
                    .Append(Value(transfer.PropertyId))
                    .Append(" ")
                    .Append(Value(transfer.PreviousOwnerPersonId))
                    .Append(" -> ")
                    .Append(Value(transfer.NewOwnerPersonId))
                    .Append(" day ")
                    .Append(WorldStateCanonicalWriter.Int64Value(transfer.TransferAbsoluteDay))
                    .Append('\n');
            }
        }

        foreach (WorldStateEstateSnapshot estate in snapshot.Estates)
        {
            if (estate != null)
            {
                output.Append("ESTATE ")
                    .Append(Value(estate.EstateId))
                    .Append(" deceased ")
                    .Append(Value(estate.DeceasedPersonId))
                    .Append(" opened ")
                    .Append(WorldStateCanonicalWriter.Int64Value(estate.OpenedAbsoluteDay))
                    .Append('\n');
            }
        }

        foreach (WorldStatePoliticalClaimSnapshot claim in snapshot.PoliticalClaims)
        {
            if (claim != null)
            {
                output.Append("POLITICAL CLAIM ")
                    .Append(Value(claim.ClaimId))
                    .Append(" claimant ")
                    .Append(Value(claim.ClaimantPersonId))
                    .Append(" type ")
                    .Append(WorldStateCanonicalWriter.EnumValue(claim.ClaimType))
                    .Append(" target ")
                    .Append(WorldStateCanonicalWriter.EnumValue(claim.TargetKind))
                    .Append(":")
                    .Append(Value(claim.TargetId))
                    .Append(" status ")
                    .Append(WorldStateCanonicalWriter.EnumValue(claim.Status))
                    .Append(" resolved-day ")
                    .Append(WorldStateCanonicalWriter.NullableInt64Value(claim.ResolutionAbsoluteDay))
                    .Append(" recognition ")
                    .Append(WorldStateCanonicalWriter.EnumValue(claim.RecognitionState))
                    .Append('\n');
            }
        }

        foreach (WorldStatePoliticalClaimRecognitionSnapshot recognition in snapshot.PoliticalClaimRecognitions)
        {
            if (recognition != null)
            {
                output.Append("POLITICAL CLAIM RECOGNITION ")
                    .Append(Value(recognition.ClaimId))
                    .Append(" institution ")
                    .Append(Value(recognition.InstitutionId))
                    .Append(" state ")
                    .Append(WorldStateCanonicalWriter.EnumValue(recognition.State))
                    .Append(" day ")
                    .Append(WorldStateCanonicalWriter.Int64Value(recognition.RecognitionAbsoluteDay))
                    .Append('\n');
            }
        }

        foreach (WorldStateFactionSnapshot faction in snapshot.Factions)
        {
            if (faction != null)
            {
                output.Append("FACTION ")
                    .Append(Value(faction.FactionId))
                    .Append(" name ")
                    .Append(Value(faction.DisplayName))
                    .Append(" created ")
                    .Append(WorldStateCanonicalWriter.Int64Value(faction.CreatedAbsoluteDay))
                    .Append('\n');
            }
        }

        foreach (WorldStateFactionAffiliationSnapshot affiliation in snapshot.FactionAffiliations)
        {
            if (affiliation != null)
            {
                output.Append("FACTION AFFILIATION ")
                    .Append(Value(affiliation.FactionId))
                    .Append(" person ")
                    .Append(Value(affiliation.PersonId))
                    .Append(" joined ")
                    .Append(WorldStateCanonicalWriter.Int64Value(affiliation.JoinedAbsoluteDay))
                    .Append(" ended ")
                    .Append(WorldStateCanonicalWriter.NullableInt64Value(affiliation.EndedAbsoluteDay))
                    .Append('\n');
            }
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

    private static void AppendConflictWarBattle(StringBuilder output, WorldStateSnapshot snapshot)
    {
        if (snapshot.HasConflictState)
        {
            output.Append("\nConflict revision: ").Append(WorldStateCanonicalWriter.Int64Value(snapshot.ConflictRevision.Value)).Append('\n');
            foreach (WorldStateConflictSnapshot conflict in snapshot.Conflicts)
            {
                if (conflict == null) continue;
                output.Append("CONFLICT ").Append(Value(conflict.ConflictId)).Append(" lifecycle ")
                    .Append(WorldStateCanonicalWriter.EnumValue(conflict.LifecycleState)).Append(" created ")
                    .Append(WorldStateCanonicalWriter.Int64Value(conflict.CreatedAbsoluteDay)).Append(" ended ")
                    .Append(WorldStateCanonicalWriter.NullableInt64Value(conflict.EndedAbsoluteDay)).Append('\n');
            }
            foreach (WorldStateConflictSideSnapshot side in snapshot.ConflictSides)
            {
                if (side != null) output.Append("CONFLICT SIDE ").Append(Value(side.ConflictId)).Append('/').Append(Value(side.SideId)).Append(" ").Append(Value(side.DisplayName)).Append('\n');
            }
            foreach (WorldStateConflictParticipantBindingSnapshot binding in snapshot.ConflictParticipantBindings)
            {
                if (binding != null) output.Append("CONFLICT PARTICIPANT ").Append(Value(binding.ConflictId)).Append('/').Append(Value(binding.BindingId)).Append(" side ").Append(Value(binding.SideId)).Append(" force ").Append(Value(binding.ArmedForceId)).Append('\n');
            }
        }

        if (snapshot.HasWarState)
        {
            output.Append("\nWar revision: ").Append(WorldStateCanonicalWriter.Int64Value(snapshot.WarRevision.Value)).Append('\n');
            foreach (WorldStateWarSnapshot war in snapshot.Wars)
            {
                if (war != null) output.Append("WAR ").Append(Value(war.WarId)).Append(" lifecycle ").Append(WorldStateCanonicalWriter.EnumValue(war.LifecycleState)).Append(" created ").Append(WorldStateCanonicalWriter.Int64Value(war.CreatedAbsoluteDay)).Append(" ended ").Append(WorldStateCanonicalWriter.NullableInt64Value(war.EndedAbsoluteDay)).Append(" conflict ").Append(Value(war.ConflictId)).Append('\n');
            }
            foreach (WorldStateWarSideSnapshot side in snapshot.WarSides)
            {
                if (side != null) output.Append("WAR SIDE ").Append(Value(side.WarId)).Append('/').Append(Value(side.SideId)).Append(" ").Append(Value(side.DisplayName)).Append('\n');
            }
            foreach (WorldStateWarParticipantBindingSnapshot binding in snapshot.WarParticipantBindings)
            {
                if (binding != null) output.Append("WAR PARTICIPANT ").Append(Value(binding.WarId)).Append('/').Append(Value(binding.BindingId)).Append(" side ").Append(Value(binding.SideId)).Append(" force ").Append(Value(binding.ArmedForceId)).Append('\n');
            }
        }

        if (snapshot.HasBattleState)
        {
            output.Append("\nBattle revision: ").Append(WorldStateCanonicalWriter.Int64Value(snapshot.BattleRevision.Value)).Append('\n');
            foreach (WorldStateBattleSnapshot battle in snapshot.Battles)
            {
                if (battle != null) output.Append("BATTLE ").Append(Value(battle.BattleId)).Append(" lifecycle ").Append(WorldStateCanonicalWriter.EnumValue(battle.LifecycleState)).Append(" created ").Append(WorldStateCanonicalWriter.Int64Value(battle.CreatedAbsoluteDay)).Append(" started ").Append(WorldStateCanonicalWriter.NullableInt64Value(battle.StartedAbsoluteDay)).Append(" conflict ").Append(Value(battle.ConflictId)).Append(" war ").Append(Value(battle.WarId)).Append('\n');
            }
            foreach (WorldStateBattleSideSnapshot side in snapshot.BattleSides)
            {
                if (side != null) output.Append("BATTLE SIDE ").Append(Value(side.BattleId)).Append('/').Append(Value(side.SideId)).Append(" ").Append(Value(side.DisplayName)).Append('\n');
            }
            foreach (WorldStateBattleParticipantBindingSnapshot binding in snapshot.BattleParticipantBindings)
            {
                if (binding != null) output.Append("BATTLE PARTICIPANT ").Append(Value(binding.BattleId)).Append('/').Append(Value(binding.BindingId)).Append(" side ").Append(Value(binding.SideId)).Append(" force ").Append(Value(binding.ArmedForceId)).Append('\n');
            }
        }
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
