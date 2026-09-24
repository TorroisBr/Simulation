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
        AppendLine(output, "METADATA", "SettlementCount", IntValue(snapshot.SettlementCount));
        AppendLine(output, "METADATA", "KnownNpcCount", IntValue(snapshot.KnownNpcCount));
        AppendLine(output, "METADATA", "PoliticalClaimCount", IntValue(snapshot.PoliticalClaimCount));
        AppendLine(output, "METADATA", "PoliticalClaimRecognitionCount", IntValue(snapshot.PoliticalClaimRecognitions.Count));
        AppendLine(output, "METADATA", "FactionCount", IntValue(snapshot.FactionCount));
        AppendLine(output, "METADATA", "FactionAffiliationCount", IntValue(snapshot.FactionAffiliationCount));
        AppendLine(output, "METADATA", "PoliticalSupportCount", IntValue(snapshot.PoliticalSupportCount));
        AppendLine(output, "METADATA", "PoliticalDecisionCount", IntValue(snapshot.PoliticalDecisionCount));
        AppendLine(output, "METADATA", "PoliticalKnowledgeStatePresent", BoolValue(snapshot.HasPoliticalKnowledgeState));
        AppendLine(output, "METADATA", "PoliticalKnowledgeHolderCount", IntValue(snapshot.PoliticalKnowledgeHolderCount));
        AppendLine(output, "METADATA", "PoliticalKnowledgeRevision", Int64Value(snapshot.PoliticalKnowledgeRevision));
        AppendLine(output, "METADATA", "TheftOutcomeCount", IntValue(snapshot.TheftOutcomeCount));
        AppendLine(output, "METADATA", "CrimeKnowledgeCount", IntValue(snapshot.CrimeKnowledgeCount));
        AppendLine(output, "METADATA", "SocialReactionCount", IntValue(snapshot.SocialReactionCount));
        if (snapshot.Spatial.AuthorityRevision.HasValue)
        {
            AppendLine(output, "METADATA", "SpatialAuthorityStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "SpatialAuthorityRevision", Int64Value(snapshot.Spatial.AuthorityRevision.Value));
        }
        AppendLine(output, "METADATA", "PersonSpatialPositionStatePresent",
            BoolValue(snapshot.Spatial.PersonSpatialPositionRevision.HasValue));
        if (snapshot.Spatial.PersonSpatialPositionRevision.HasValue)
        {
            AppendLine(output, "METADATA", "PersonSpatialPositionRevision",
                Int64Value(snapshot.Spatial.PersonSpatialPositionRevision.Value));
        }
        AppendLine(output, "METADATA", "LegacySpatialAnchorBindingStatePresent",
            BoolValue(snapshot.Spatial.LegacySpatialAnchorBindingRevision.HasValue));
        if (snapshot.Spatial.LegacySpatialAnchorBindingRevision.HasValue)
        {
            AppendLine(output, "METADATA", "LegacySpatialAnchorBindingRevision",
                Int64Value(snapshot.Spatial.LegacySpatialAnchorBindingRevision.Value));
        }
        if (snapshot.HasArmedForceState)
        {
            AppendLine(output, "METADATA", "ArmedForceStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "ArmedForceRevision", Int64Value(snapshot.ArmedForceRevision.Value));
        }
        if (snapshot.HasArmedForceSpatialState)
        {
            AppendLine(output, "METADATA", "ArmedForceSpatialStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "ArmedForceSpatialRevision", Int64Value(snapshot.ArmedForceSpatialRevision.Value));
        }
        if (snapshot.HasContingentManpowerState)
        {
            AppendLine(output, "METADATA", "ContingentManpowerStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "ContingentManpowerRevision", Int64Value(snapshot.ContingentManpowerRevision.Value));
        }
        if (snapshot.HasConflictState)
        {
            AppendLine(output, "METADATA", "ConflictStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "ConflictRevision", Int64Value(snapshot.ConflictRevision.Value));
        }
        if (snapshot.HasWarState)
        {
            AppendLine(output, "METADATA", "WarStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "WarRevision", Int64Value(snapshot.WarRevision.Value));
        }
        if (snapshot.HasBattleState)
        {
            AppendLine(output, "METADATA", "BattleStatePresent", BoolValue(true));
            AppendLine(output, "METADATA", "BattleRevision", Int64Value(snapshot.BattleRevision.Value));
        }
        if (snapshot.Metadata.CalendarDate != null)
        {
            WorldStateCalendarSnapshot calendar = snapshot.Metadata.CalendarDate;
            AppendLine(output, "CALENDAR",
                Int64Value(calendar.AbsoluteDay),
                Int64Value(calendar.Year),
                IntValue(calendar.Month),
                IntValue(calendar.WeekOfMonth),
                IntValue(calendar.DayOfMonth),
                IntValue(calendar.DayOfWeek),
                Int64Value(calendar.DayOfYear),
                Int64Value(calendar.DaysPerMonth),
                Int64Value(calendar.DaysPerYear));
        }

        if (snapshot.Spatial.AuthorityRevision.HasValue)
        {
            foreach (WorldStateHexSnapshot hex in snapshot.Spatial.Hexes)
            {
                if (hex.HasGeographicFacts)
                {
                    AppendLine(output, "SPATIAL_HEX",
                        hex.HexId,
                        NullableIntValue(hex.Q),
                        NullableIntValue(hex.R),
                        hex.TerrainDefinitionId,
                        hex.AuthoredRevisionToken);
                }
                else
                {
                    AppendLine(output, "SPATIAL_HEX", hex.HexId);
                }
            }

            foreach (WorldStateAnchoredLocationSnapshot location in snapshot.Spatial.AnchoredLocations)
            {
                AppendLine(output, "SPATIAL_LOCATION", location.LocationId, location.AnchorHexId);
            }

            foreach (WorldStateCrossingSnapshot crossing in snapshot.Spatial.Crossings)
            {
                AppendLine(output, "SPATIAL_CROSSING",
                    crossing.CrossingId,
                    crossing.FirstHexId,
                    crossing.SecondHexId,
                    crossing.AnchorHexId,
                    crossing.ContentIdentity,
                    crossing.ContentRevision,
                    DecimalValue(crossing.EffortMultiplier),
                    EnumValue(crossing.Condition));
                foreach (string barrierId in crossing.OvercomesBarrierIds)
                {
                    AppendLine(output, "SPATIAL_CROSSING_OVERCOMES_BARRIER", crossing.CrossingId, barrierId);
                }
            }

            foreach (WorldStatePassageOptionSnapshot option in snapshot.Spatial.PassageOptions)
            {
                AppendLine(output, "SPATIAL_PASSAGE_OPTION",
                    option.StableKey,
                    EnumValue(option.Kind),
                    option.ConnectionId,
                    option.CrossingId,
                    option.RuleIdentity,
                    option.RuleVersion,
                    option.FirstHexId,
                    option.SecondHexId,
                    EnumValue(option.Condition),
                    BoolValue(option.IsCrossing),
                    option.ContentIdentity,
                    option.ContentRevision,
                    DecimalValue(option.EffortMultiplier));
                foreach (string barrierId in option.OvercomesBarrierIds)
                {
                    AppendLine(output, "SPATIAL_PASSAGE_OPTION_OVERCOMES_BARRIER", option.StableKey, barrierId);
                }
            }

            foreach (WorldStateBarrierSnapshot barrier in snapshot.Spatial.Barriers)
            {
                AppendLine(output, "SPATIAL_BARRIER",
                    barrier.BarrierId,
                    barrier.ContentIdentity,
                    barrier.ContentRevision,
                    EnumValue(barrier.Condition));
                foreach (WorldStateHexBoundarySnapshot boundary in barrier.Boundaries)
                {
                    AppendLine(output, "SPATIAL_BARRIER_BOUNDARY",
                        barrier.BarrierId,
                        boundary.FirstHexId,
                        boundary.SecondHexId);
                }
            }

            if (snapshot.Spatial.CoordinateConventionVersion != null
                || snapshot.Spatial.CoordinateCanonicalOrder != null)
            {
                AppendLine(output, "SPATIAL_COORDINATE_CONVENTION",
                    snapshot.Spatial.CoordinateConventionVersion,
                    snapshot.Spatial.CoordinateCanonicalOrder);
            }

            if (snapshot.Spatial.ScaleContext != null)
            {
                WorldStateSpatialScaleContextSnapshot scale = snapshot.Spatial.ScaleContext;
                AppendLine(output, "SPATIAL_WORLD_SCALE",
                    scale.ResolvedConventionId,
                    scale.SourceIdentity,
                    scale.SourceVersion,
                    scale.DistancePerNeighborStep.HasValue
                        ? DecimalValue(scale.DistancePerNeighborStep.Value)
                        : null,
                    scale.Unit);
            }
        }

        if (snapshot.HasArmedForceState)
        {
            foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces)
            {
                AppendLine(output, "ARMED_FORCE",
                    force.ArmedForceId,
                    force.DisplayName,
                    Int64Value(force.CreatedAbsoluteDay),
                    EnumValue(force.LifecycleState),
                    NullableInt64Value(force.TerminatedAbsoluteDay),
                    force.ParentForceId,
                    BoolValue(force.IsDetached),
                    force.CommanderPersonId);
            }

            foreach (WorldStateArmedForceSnapshot force in snapshot.ArmedForces)
            {
                if (force != null && string.IsNullOrWhiteSpace(force.OperationalLocationReference) == false)
                {
                    AppendLine(output, "ARMED_FORCE_LEGACY_OPERATIONAL_REFERENCE",
                        force.ArmedForceId,
                        force.OperationalLocationReference);
                }
            }

            foreach (WorldStateArmedForceContingentSnapshot contingent in snapshot.ArmedForceContingents)
            {
                AppendLine(output, "ARMED_FORCE_CONTINGENT",
                    contingent.ContingentId,
                    contingent.ForceId,
                    Int64Value(contingent.Amount),
                    contingent.OriginDomain,
                    contingent.OriginValue,
                    contingent.ServiceType);

                foreach (WorldStateArmedForceCharacteristicSnapshot characteristic in contingent.Characteristics)
                {
                    AppendLine(output, "ARMED_FORCE_CONTINGENT_CHARACTERISTIC",
                        contingent.ContingentId,
                        characteristic.Key,
                        characteristic.Value);
                }
            }

            foreach (WorldStateArmedForcePersonReferenceSnapshot reference in snapshot.ArmedForceRelevantPersons)
            {
                AppendLine(output, "ARMED_FORCE_PERSON_REFERENCE",
                    reference.ReferenceId,
                    reference.ForceId,
                    reference.PersonId,
                    reference.RoleKey);
            }
        }

        if (snapshot.HasArmedForceSpatialState)
        {
            foreach (WorldStateArmedForcePositionSnapshot position in snapshot.ArmedForcePositions)
            {
                if (position != null)
                {
                    AppendLine(output, "ARMED_FORCE_POSITION",
                        position.ArmedForceId,
                        position.CurrentPositionStableKey);
                }
            }
        }

        if (snapshot.HasContingentManpowerState)
        {
            foreach (WorldStateContingentManpowerSnapshot state in snapshot.ContingentManpowerStates)
            {
                AppendLine(output, "CONTINGENT_MANPOWER",
                    state.ContingentId,
                    state.SourceId,
                    Int64Value(state.Revision),
                    Int64Value(state.LivingRosterAmount),
                    Int64Value(state.AvailableAmount),
                    state.Fingerprint,
                    BoolValue(state.SourceResolved),
                    NullableInt64Value(state.SourceCapacity),
                    NullableInt64Value(state.SourceFactualLivingAmount),
                    state.SourceFingerprint);
                foreach (WorldStateManpowerCohortSnapshot cohort in state.Cohorts)
                {
                    AppendLine(output, "CONTINGENT_MANPOWER_COHORT",
                        state.ContingentId,
                        EnumValue(cohort.InjuryState),
                        EnumValue(cohort.CustodyState),
                        cohort.CustodianForceId,
                        EnumValue(cohort.AvailabilityState),
                        Int64Value(cohort.Amount));
                }
            }
        }

        if (snapshot.HasConflictState)
        {
            foreach (WorldStateConflictSnapshot conflict in snapshot.Conflicts)
            {
                AppendLine(output, "CONFLICT", conflict.ConflictId, Int64Value(conflict.CreatedAbsoluteDay), EnumValue(conflict.LifecycleState), NullableInt64Value(conflict.EndedAbsoluteDay));
            }
            foreach (WorldStateConflictSideSnapshot side in snapshot.ConflictSides)
            {
                AppendLine(output, "CONFLICT_SIDE", side.ConflictId, side.SideId, side.DisplayName);
            }
            foreach (WorldStateConflictParticipantBindingSnapshot binding in snapshot.ConflictParticipantBindings)
            {
                AppendLine(output, "CONFLICT_PARTICIPANT_BINDING", binding.ConflictId, binding.BindingId, binding.SideId, binding.ArmedForceId);
            }
        }

        if (snapshot.HasWarState)
        {
            foreach (WorldStateWarSnapshot war in snapshot.Wars)
            {
                AppendLine(output, "WAR", war.WarId, Int64Value(war.CreatedAbsoluteDay), EnumValue(war.LifecycleState), NullableInt64Value(war.EndedAbsoluteDay), war.ConflictId);
            }
            foreach (WorldStateWarSideSnapshot side in snapshot.WarSides)
            {
                AppendLine(output, "WAR_SIDE", side.WarId, side.SideId, side.DisplayName);
            }
            foreach (WorldStateWarParticipantBindingSnapshot binding in snapshot.WarParticipantBindings)
            {
                AppendLine(output, "WAR_PARTICIPANT_BINDING", binding.WarId, binding.BindingId, binding.SideId, binding.ArmedForceId);
            }
        }

        if (snapshot.HasBattleState)
        {
            foreach (WorldStateBattleSnapshot battle in snapshot.Battles)
            {
                AppendLine(output, "BATTLE", battle.BattleId, Int64Value(battle.CreatedAbsoluteDay), NullableInt64Value(battle.StartedAbsoluteDay), EnumValue(battle.LifecycleState), battle.ConflictId, battle.WarId, battle.LocationReferenceKey);
                if (battle.TerminalOutcome != null)
                {
                    WorldStateBattleTerminalOutcomeSnapshot outcome = battle.TerminalOutcome;
                    AppendLine(output, "BATTLE_OUTCOME", outcome.BattleId, EnumValue(outcome.OutcomeType), outcome.WinningBattleSideId, Int64Value(outcome.ResolvedAbsoluteDay));
                    AppendLine(output, "BATTLE_OUTCOME_PROVENANCE", outcome.BattleId,
                        outcome.D5PolicyFingerprint, outcome.D5NumericExecutionProfileKey,
                        outcome.D5ProjectionVersion, outcome.D5CausalResolutionFingerprint,
                        outcome.D5SourceContextFingerprint, outcome.D5CapabilityRuleKey,
                        outcome.D5RandomAuthorityRuleKey, outcome.D5ResolverSettingsIdentity,
                        outcome.D6B2PolicyFingerprint, outcome.D6B2PlanSchemaVersion,
                        outcome.D6B2CoverageVersion, outcome.D6B2PlanFingerprint);
                }
            }
            foreach (WorldStateBattleSideSnapshot side in snapshot.BattleSides)
            {
                AppendLine(output, "BATTLE_SIDE", side.BattleId, side.SideId, side.DisplayName);
            }
            foreach (WorldStateBattleParticipantBindingSnapshot binding in snapshot.BattleParticipantBindings)
            {
                AppendLine(output, "BATTLE_PARTICIPANT_BINDING", binding.BattleId, binding.BindingId, binding.SideId, binding.ArmedForceId);
            }
        }

        foreach (WorldStatePersonSnapshot person in snapshot.Persons)
        {
            AppendLine(output, "PERSON",
                person.PersonId,
                person.MaterializedNpcRuntimeId,
                BoolValue(person.IsMaterialized),
                person.BirthAbsoluteDay.HasValue ? Int64Value(person.BirthAbsoluteDay.Value) : null,
                person.CompletedYears.HasValue ? Int64Value(person.CompletedYears.Value) : null,
                person.ResidenceSettlementRuntimeId,
                person.DeathAbsoluteDay.HasValue ? Int64Value(person.DeathAbsoluteDay.Value) : null);
        }

        foreach (WorldStateParentageSnapshot parentage in snapshot.Parentages)
        {
            AppendLine(output, "PARENTAGE",
                parentage.ParentPersonId,
                parentage.ChildPersonId);
        }

        foreach (WorldStatePropertyOwnershipSnapshot ownership in snapshot.PropertyOwnerships)
        {
            AppendLine(output, "PROPERTY_OWNERSHIP",
                ownership.PropertyId,
                ownership.OwnerPersonId);
        }

        foreach (WorldStatePropertyTransferSnapshot transfer in snapshot.PropertyTransfers)
        {
            AppendLine(output, "PROPERTY_TRANSFER",
                transfer.PropertyId,
                transfer.PreviousOwnerPersonId,
                transfer.NewOwnerPersonId,
                Int64Value(transfer.TransferAbsoluteDay));
        }

        foreach (WorldStateEstateSnapshot estate in snapshot.Estates)
        {
            AppendLine(output, "ESTATE",
                estate.EstateId,
                estate.DeceasedPersonId,
                Int64Value(estate.OpenedAbsoluteDay));
        }

        foreach (WorldStatePoliticalClaimSnapshot claim in snapshot.PoliticalClaims)
        {
            AppendLine(output, "POLITICAL_CLAIM",
                claim.ClaimId,
                claim.ClaimantPersonId,
                EnumValue(claim.ClaimType),
                EnumValue(claim.TargetKind),
                claim.TargetId,
                EnumValue(claim.Basis),
                claim.BasisDescription,
                Int64Value(claim.CreatedAbsoluteDay),
                EnumValue(claim.Status),
                NullableInt64Value(claim.ResolutionAbsoluteDay),
                EnumValue(claim.RecognitionState),
                claim.RecognizingInstitutionId,
                claim.RecognitionAbsoluteDay.HasValue
                    ? Int64Value(claim.RecognitionAbsoluteDay.Value)
                    : null,
                claim.RecognitionReason,
                StringListValue(claim.EvidenceReferences));
        }

        foreach (WorldStatePoliticalClaimRecognitionSnapshot recognition in snapshot.PoliticalClaimRecognitions)
        {
            AppendLine(output, "POLITICAL_CLAIM_RECOGNITION",
                recognition.ClaimId,
                recognition.InstitutionId,
                EnumValue(recognition.State),
                Int64Value(recognition.RecognitionAbsoluteDay),
                recognition.Reason,
                RecognitionHistoryValue(recognition.History));
        }

        foreach (WorldStateFactionSnapshot faction in snapshot.Factions)
        {
            AppendLine(output, "FACTION",
                faction.FactionId,
                faction.DisplayName,
                Int64Value(faction.CreatedAbsoluteDay),
                EnumValue(faction.MembershipPolicy),
                BoolValue(faction.ExpulsionAllowed));
        }

        foreach (WorldStateFactionAffiliationSnapshot affiliation in snapshot.FactionAffiliations)
        {
            AppendLine(output, "FACTION_AFFILIATION",
                affiliation.FactionId,
                affiliation.PersonId,
                affiliation.AffiliationId,
                Int64Value(affiliation.JoinedAbsoluteDay),
                NullableInt64Value(affiliation.EndedAbsoluteDay),
                EnumValueOrNull(affiliation.EndReason));
        }

        foreach (WorldStatePoliticalSupportSnapshot support in snapshot.PoliticalSupports)
        {
            AppendLine(output, "POLITICAL_SUPPORT",
                support.RelationId,
                EnumValue(support.SourceKind),
                support.SourceId,
                EnumValue(support.TargetKind),
                support.TargetId,
                EnumValue(support.Disposition),
                Int64Value(support.StartedAbsoluteDay),
                NullableInt64Value(support.EndedAbsoluteDay));
        }

        foreach (WorldStatePoliticalDecisionSnapshot decision in snapshot.PoliticalDecisions)
        {
            AppendLine(output, "POLITICAL_DECISION",
                decision.DecisionId,
                decision.DeciderStableId,
                EnumValue(decision.DecisionKind),
                decision.OfficeId,
                decision.RecognizingInstitutionId,
                StringListValue(decision.CandidatePersonIds),
                decision.CandidateFingerprint,
                EnumValue(decision.OutcomeKind),
                decision.SelectedCandidatePersonId,
                decision.ReferencedClaimId,
                StringListValue(decision.EvidenceReferences),
                StringListValue(decision.KnowledgeReferences),
                Int64Value(decision.ObservedAbsoluteDay),
                Int64Value(decision.DecisionAbsoluteDay),
                Int64Value(decision.ExpectedWorldRevision),
                Int64Value(decision.ExpectedKnowledgeRevision));
        }

        foreach (WorldStatePoliticalKnowledgeSnapshot knowledge in snapshot.PoliticalKnowledge)
        {
            AppendLine(output, "POLITICAL_KNOWLEDGE",
                knowledge.HolderStableId,
                EnumValue(knowledge.HolderKind),
                knowledge.HolderPersonId,
                knowledge.HolderInstitutionId,
                knowledge.HolderFactionId);

            foreach (WorldStatePoliticalKnowledgeObservationSnapshot observation in knowledge.Observations)
            {
                AppendLine(output, "POLITICAL_KNOWLEDGE_OBSERVATION",
                    knowledge.HolderStableId,
                    EnumValue(observation.FactKind),
                    observation.IdentityKey,
                    observation.StateKey,
                    Int64Value(observation.ObservedAbsoluteDay),
                    Int64Value(observation.ReceivedAbsoluteDay),
                    EnumValue(observation.Source),
                    observation.SourceReference,
                    observation.SourcePersonId,
                    observation.SourceInstitutionId);
            }
        }

        foreach (WorldStateTheftOutcomeSnapshot outcome in snapshot.TheftOutcomes)
        {
            AppendLine(output, "THEFT_OUTCOME",
                outcome.OutcomeId,
                outcome.PerpetratorPersonId,
                outcome.VictimPersonId,
                IntValue(outcome.LossAmount),
                Int64Value(outcome.OccurredAbsoluteDay),
                outcome.OccurrenceKey,
                outcome.OriginDecisionId);
        }

        foreach (WorldStateCrimeKnowledgeSnapshot knowledge in snapshot.CrimeKnowledge)
        {
            AppendLine(output, "CRIME_KNOWLEDGE",
                knowledge.EvaluatorPersonId,
                knowledge.OutcomeId,
                EnumValue(knowledge.Role),
                BoolValue(knowledge.KnowsLoss),
                EnumValue(knowledge.PerceivedPerpetratorKind),
                knowledge.PerceivedPerpetratorPersonId,
                knowledge.PerceivedPerpetratorInstitutionId,
                knowledge.KnownInvestigatorPersonId,
                knowledge.KnownInvestigatorInstitutionId,
                EnumValue(knowledge.CognitiveBasisKind),
                knowledge.CognitiveBasisReference,
                knowledge.CognitiveBasisSourcePersonId,
                knowledge.CognitiveBasisSourceInstitutionId,
                Int64Value(knowledge.ObservedAbsoluteDay));
        }

        foreach (WorldStateSocialReactionSnapshot reaction in snapshot.SocialReactions)
        {
            AppendLine(output, "SOCIAL_REACTION",
                reaction.ReactionId,
                reaction.EvaluatorPersonId,
                reaction.SourceDomain,
                reaction.SourceStableId,
                EnumValue(reaction.TargetKind),
                reaction.TargetStableId,
                EnumValue(reaction.AttributionKind),
                reaction.AttributionPersonId,
                reaction.AttributionInstitutionId,
                EnumValue(reaction.Valence),
                EnumValue(reaction.Salience),
                EnumValue(reaction.CognitiveBasisKind),
                reaction.CognitiveBasisReference,
                reaction.CognitiveBasisSourcePersonId,
                reaction.CognitiveBasisSourceInstitutionId,
                Int64Value(reaction.CreatedAbsoluteDay),
                reaction.SupersedesReactionId);
        }

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

            if (string.IsNullOrWhiteSpace(npc.PersonId) == false)
            {
                AppendLine(output, "NPC_PERSON", npc.RuntimeId, npc.PersonId);
            }

            foreach (string statusName in npc.StatusNames)
            {
                AppendLine(output, "NPC_STATUS", npc.RuntimeId, statusName);
            }

            if (npc.CurrentAction != null)
            {
                WorldStateActionSnapshot action = npc.CurrentAction;
                AppendLine(output, "NPC_ACTION",
                    npc.RuntimeId,
                    action.DefinitionId,
                    EnumValue(action.Category),
                    EnumValue(action.Type),
                    action.TargetNpcRuntimeId,
                    action.TargetCityRuntimeId,
                    action.TargetItemDefinitionId,
                    IntValue(action.Amount),
                    FloatValue(action.ExpectedUnitPrice),
                    FloatValue(action.SuccessChanceMultiplier),
                    EnumValue(action.TravelReason),
                    FloatValue(action.ExpectedNetValue),
                    action.OriginDecisionId);
            }

            if (npc.MerchantTradePlan != null)
            {
                WorldStateMerchantTradePlanSnapshot plan = npc.MerchantTradePlan;
                AppendLine(output, "NPC_TRADE_PLAN",
                    npc.RuntimeId,
                    BoolValue(plan.HasData),
                    BoolValue(plan.IsActive),
                    plan.ItemDefinitionId,
                    plan.OriginCityRuntimeId,
                    plan.TargetCityRuntimeId,
                    IntValue(plan.PlannedAmount),
                    IntValue(plan.RemainingAmount),
                    FloatValue(plan.PurchasePricePerItem),
                    IntValue(plan.WaitDaysAtDestination),
                    IntValue(plan.PendingTravelDays),
                    plan.OriginDecisionId);
            }

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
                Int64Value(city.PopulationRevision),
                IntValue(city.NamedResidentCount),
                IntValue(city.NamedPresentCount),
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

        foreach (WorldStatePersonSpatialPositionSnapshot position in snapshot.Spatial.PersonSpatialPositions)
        {
            if (position.IsInTransit)
            {
                WorldStateTransitSnapshot transit = position.Transit;
                AppendLine(output, "PERSON_SPATIAL_TRANSIT",
                    position.PersonId,
                    EnumValue(transit.OptionKind),
                    transit.OptionStableKey,
                    transit.ConnectionId,
                    transit.CrossingId,
                    transit.RuleIdentity,
                    transit.RuleVersion,
                    transit.FirstHexId,
                    transit.SecondHexId,
                    transit.FromHexId,
                    transit.ToHexId,
                    EnumValue(transit.LastFullyReachedReference.Kind),
                    transit.LastFullyReachedReference.StableKey,
                    transit.LastFullyReachedReference.HexId,
                    transit.LastFullyReachedReference.LocationId,
                    transit.LastFullyReachedReference.CrossingId,
                    IntValue(transit.ProgressTicks));
            }
            else if (position.Position != null)
            {
                AppendLine(output, "PERSON_SPATIAL_AT",
                    position.PersonId,
                    EnumValue(position.Position.Kind),
                    position.Position.StableKey,
                    position.Position.HexId,
                    position.Position.LocationId,
                    position.Position.CrossingId);
            }
        }

        foreach (WorldStateSpatialAnchorBindingSnapshot binding in snapshot.Spatial.LegacySpatialAnchorBindings)
        {
            AppendLine(output, "LEGACY_SPATIAL_ANCHOR_BINDING",
                EnumValue(binding.OwnerKind),
                binding.OwnerId,
                binding.LocationId);
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

    public static string DecimalValue(decimal value)
    {
        return value.ToString("G29", CultureInfo.InvariantCulture);
    }

    public static string NullableIntValue(int? value)
    {
        return value.HasValue ? IntValue(value.Value) : "~";
    }

    public static string NullableInt64Value(long? value)
    {
        return value.HasValue ? Int64Value(value.Value) : "~";
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

    private static string RecognitionHistoryValue(
        IReadOnlyList<WorldStatePoliticalClaimRecognitionHistorySnapshot> history)
    {
        if (history == null || history.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();
        foreach (WorldStatePoliticalClaimRecognitionHistorySnapshot entry in history)
        {
            if (result.Length > 0) result.Append(';');
            result.Append(EnumValue(entry.State));
            result.Append('@');
            result.Append(Int64Value(entry.RecognitionAbsoluteDay));
            result.Append('@');
            result.Append(entry.Reason ?? string.Empty);
        }

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
