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
        CompareValue("Metadata", "world", "PoliticalClaimRecognitionCount",
            WorldStateCanonicalWriter.IntValue(before.PoliticalClaimRecognitions.Count),
            WorldStateCanonicalWriter.IntValue(after.PoliticalClaimRecognitions.Count),
            differences);
        CompareValue("Metadata", "world", "FactionCount",
            WorldStateCanonicalWriter.IntValue(before.FactionCount),
            WorldStateCanonicalWriter.IntValue(after.FactionCount),
            differences);
        CompareValue("Metadata", "world", "FactionAffiliationCount",
            WorldStateCanonicalWriter.IntValue(before.FactionAffiliationCount),
            WorldStateCanonicalWriter.IntValue(after.FactionAffiliationCount),
            differences);
        CompareValue("Metadata", "world", "PoliticalSupportCount",
            WorldStateCanonicalWriter.IntValue(before.PoliticalSupportCount),
            WorldStateCanonicalWriter.IntValue(after.PoliticalSupportCount),
            differences);
        CompareValue("Metadata", "world", "PoliticalDecisionCount",
            WorldStateCanonicalWriter.IntValue(before.PoliticalDecisionCount),
            WorldStateCanonicalWriter.IntValue(after.PoliticalDecisionCount),
            differences);
        CompareValue("Metadata", "world", "PoliticalKnowledgeStatePresent",
            WorldStateCanonicalWriter.BoolValue(before.HasPoliticalKnowledgeState),
            WorldStateCanonicalWriter.BoolValue(after.HasPoliticalKnowledgeState),
            differences);
        CompareValue("Metadata", "world", "PoliticalKnowledgeHolderCount",
            WorldStateCanonicalWriter.IntValue(before.PoliticalKnowledgeHolderCount),
            WorldStateCanonicalWriter.IntValue(after.PoliticalKnowledgeHolderCount),
            differences);
        CompareValue("Metadata", "world", "PoliticalKnowledgeRevision",
            WorldStateCanonicalWriter.Int64Value(before.PoliticalKnowledgeRevision),
            WorldStateCanonicalWriter.Int64Value(after.PoliticalKnowledgeRevision),
            differences);
        CompareValue("Metadata", "world", "TheftOutcomeCount",
            WorldStateCanonicalWriter.IntValue(before.TheftOutcomeCount),
            WorldStateCanonicalWriter.IntValue(after.TheftOutcomeCount),
            differences);
        CompareValue("Metadata", "world", "CrimeKnowledgeCount",
            WorldStateCanonicalWriter.IntValue(before.CrimeKnowledgeCount),
            WorldStateCanonicalWriter.IntValue(after.CrimeKnowledgeCount),
            differences);
        CompareValue("Metadata", "world", "SocialReactionCount",
            WorldStateCanonicalWriter.IntValue(before.SocialReactionCount),
            WorldStateCanonicalWriter.IntValue(after.SocialReactionCount),
            differences);
        CompareCalendar(before.Metadata.CalendarDate, after.Metadata.CalendarDate, differences);

        CompareValue("SpatialAuthorityStore", "world", "StatePresent",
            WorldStateCanonicalWriter.BoolValue(before.Spatial.AuthorityRevision.HasValue),
            WorldStateCanonicalWriter.BoolValue(after.Spatial.AuthorityRevision.HasValue),
            differences);
        CompareValue("SpatialAuthorityStore", "world", "Revision",
            before.Spatial.AuthorityRevision.HasValue
                ? WorldStateCanonicalWriter.Int64Value(before.Spatial.AuthorityRevision.Value)
                : null,
            after.Spatial.AuthorityRevision.HasValue
                ? WorldStateCanonicalWriter.Int64Value(after.Spatial.AuthorityRevision.Value)
                : null,
            differences);
        CompareEntities("SpatialHex", before.Spatial.Hexes, after.Spatial.Hexes,
            hex => hex.HexId,
            (identity, left, right) => { },
            differences);
        CompareEntities("SpatialLocation", before.Spatial.AnchoredLocations, after.Spatial.AnchoredLocations,
            location => location.LocationId,
            (identity, left, right) => CompareValue(
                "SpatialLocation",
                identity,
                "AnchorHexId",
                WorldStateCanonicalWriter.StringValue(left.AnchorHexId),
                WorldStateCanonicalWriter.StringValue(right.AnchorHexId),
                differences),
            differences);

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

        CompareValue("ArmedForceStore", "world", "StatePresent",
            WorldStateCanonicalWriter.BoolValue(before.HasArmedForceState),
            WorldStateCanonicalWriter.BoolValue(after.HasArmedForceState),
            differences);
        CompareValue("ArmedForceStore", "world", "Revision",
            before.ArmedForceRevision.HasValue
                ? WorldStateCanonicalWriter.Int64Value(before.ArmedForceRevision.Value)
                : null,
            after.ArmedForceRevision.HasValue
                ? WorldStateCanonicalWriter.Int64Value(after.ArmedForceRevision.Value)
                : null,
            differences);

        CompareEntities("ArmedForce", before.ArmedForces, after.ArmedForces,
            force => force.ArmedForceId,
            (identity, left, right) =>
            {
                CompareValue("ArmedForce", identity, "DisplayName",
                    WorldStateCanonicalWriter.StringValue(left.DisplayName),
                    WorldStateCanonicalWriter.StringValue(right.DisplayName), differences);
                CompareValue("ArmedForce", identity, "CreatedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay), differences);
                CompareValue("ArmedForce", identity, "LifecycleState",
                    WorldStateCanonicalWriter.EnumValue(left.LifecycleState),
                    WorldStateCanonicalWriter.EnumValue(right.LifecycleState), differences);
                CompareValue("ArmedForce", identity, "TerminatedAbsoluteDay",
                    WorldStateCanonicalWriter.NullableInt64Value(left.TerminatedAbsoluteDay),
                    WorldStateCanonicalWriter.NullableInt64Value(right.TerminatedAbsoluteDay), differences);
                CompareValue("ArmedForce", identity, "ParentForceId",
                    WorldStateCanonicalWriter.StringValue(left.ParentForceId),
                    WorldStateCanonicalWriter.StringValue(right.ParentForceId), differences);
                CompareValue("ArmedForce", identity, "IsDetached",
                    WorldStateCanonicalWriter.BoolValue(left.IsDetached),
                    WorldStateCanonicalWriter.BoolValue(right.IsDetached), differences);
                CompareValue("ArmedForce", identity, "OperationalLocationReference",
                    WorldStateCanonicalWriter.StringValue(left.OperationalLocationReference),
                    WorldStateCanonicalWriter.StringValue(right.OperationalLocationReference), differences);
                CompareValue("ArmedForce", identity, "CommanderPersonId",
                    WorldStateCanonicalWriter.StringValue(left.CommanderPersonId),
                    WorldStateCanonicalWriter.StringValue(right.CommanderPersonId), differences);
            },
            differences);

        CompareEntities("ArmedForceContingent", before.ArmedForceContingents, after.ArmedForceContingents,
            contingent => contingent.ContingentId,
            (identity, left, right) =>
            {
                CompareValue("ArmedForceContingent", identity, "ForceId",
                    WorldStateCanonicalWriter.StringValue(left.ForceId),
                    WorldStateCanonicalWriter.StringValue(right.ForceId), differences);
                CompareValue("ArmedForceContingent", identity, "Amount",
                    WorldStateCanonicalWriter.Int64Value(left.Amount),
                    WorldStateCanonicalWriter.Int64Value(right.Amount), differences);
                CompareValue("ArmedForceContingent", identity, "OriginDomain",
                    WorldStateCanonicalWriter.StringValue(left.OriginDomain),
                    WorldStateCanonicalWriter.StringValue(right.OriginDomain), differences);
                CompareValue("ArmedForceContingent", identity, "OriginValue",
                    WorldStateCanonicalWriter.StringValue(left.OriginValue),
                    WorldStateCanonicalWriter.StringValue(right.OriginValue), differences);
                CompareValue("ArmedForceContingent", identity, "ServiceType",
                    WorldStateCanonicalWriter.StringValue(left.ServiceType),
                    WorldStateCanonicalWriter.StringValue(right.ServiceType), differences);
                CompareValue("ArmedForceContingent", identity, "Characteristics",
                    ArmedForceCharacteristicsValue(left.Characteristics),
                    ArmedForceCharacteristicsValue(right.Characteristics), differences);
            },
            differences);

        CompareEntities("ArmedForcePersonReference", before.ArmedForceRelevantPersons, after.ArmedForceRelevantPersons,
            reference => reference.ReferenceId,
            (identity, left, right) =>
            {
                CompareValue("ArmedForcePersonReference", identity, "ForceId",
                    WorldStateCanonicalWriter.StringValue(left.ForceId),
                    WorldStateCanonicalWriter.StringValue(right.ForceId), differences);
                CompareValue("ArmedForcePersonReference", identity, "PersonId",
                    WorldStateCanonicalWriter.StringValue(left.PersonId),
                    WorldStateCanonicalWriter.StringValue(right.PersonId), differences);
                CompareValue("ArmedForcePersonReference", identity, "RoleKey",
                    WorldStateCanonicalWriter.StringValue(left.RoleKey),
                    WorldStateCanonicalWriter.StringValue(right.RoleKey), differences);
            },
            differences);

        ComparePersistentState("ConflictStore", before.HasConflictState, before.ConflictRevision, after.HasConflictState, after.ConflictRevision, differences);
        CompareEntities("Conflict", before.Conflicts, after.Conflicts,
            conflict => conflict.ConflictId,
            (identity, left, right) =>
            {
                CompareValue("Conflict", identity, "CreatedAbsoluteDay", WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay), WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay), differences);
                CompareValue("Conflict", identity, "LifecycleState", WorldStateCanonicalWriter.EnumValue(left.LifecycleState), WorldStateCanonicalWriter.EnumValue(right.LifecycleState), differences);
                CompareValue("Conflict", identity, "EndedAbsoluteDay", WorldStateCanonicalWriter.NullableInt64Value(left.EndedAbsoluteDay), WorldStateCanonicalWriter.NullableInt64Value(right.EndedAbsoluteDay), differences);
            }, differences);
        CompareEntities("ConflictSide", before.ConflictSides, after.ConflictSides,
            side => side.ConflictId + "\u001f" + side.SideId,
            (identity, left, right) => CompareValue("ConflictSide", identity, "DisplayName", WorldStateCanonicalWriter.StringValue(left.DisplayName), WorldStateCanonicalWriter.StringValue(right.DisplayName), differences),
            differences);
        CompareEntities("ConflictParticipantBinding", before.ConflictParticipantBindings, after.ConflictParticipantBindings,
            binding => binding.ConflictId + "\u001f" + binding.BindingId,
            (identity, left, right) =>
            {
                CompareValue("ConflictParticipantBinding", identity, "SideId", WorldStateCanonicalWriter.StringValue(left.SideId), WorldStateCanonicalWriter.StringValue(right.SideId), differences);
                CompareValue("ConflictParticipantBinding", identity, "ArmedForceId", WorldStateCanonicalWriter.StringValue(left.ArmedForceId), WorldStateCanonicalWriter.StringValue(right.ArmedForceId), differences);
            }, differences);

        ComparePersistentState("WarStore", before.HasWarState, before.WarRevision, after.HasWarState, after.WarRevision, differences);
        CompareEntities("War", before.Wars, after.Wars,
            war => war.WarId,
            (identity, left, right) =>
            {
                CompareValue("War", identity, "CreatedAbsoluteDay", WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay), WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay), differences);
                CompareValue("War", identity, "LifecycleState", WorldStateCanonicalWriter.EnumValue(left.LifecycleState), WorldStateCanonicalWriter.EnumValue(right.LifecycleState), differences);
                CompareValue("War", identity, "EndedAbsoluteDay", WorldStateCanonicalWriter.NullableInt64Value(left.EndedAbsoluteDay), WorldStateCanonicalWriter.NullableInt64Value(right.EndedAbsoluteDay), differences);
                CompareValue("War", identity, "ConflictId", WorldStateCanonicalWriter.StringValue(left.ConflictId), WorldStateCanonicalWriter.StringValue(right.ConflictId), differences);
            }, differences);
        CompareEntities("WarSide", before.WarSides, after.WarSides,
            side => side.WarId + "\u001f" + side.SideId,
            (identity, left, right) => CompareValue("WarSide", identity, "DisplayName", WorldStateCanonicalWriter.StringValue(left.DisplayName), WorldStateCanonicalWriter.StringValue(right.DisplayName), differences),
            differences);
        CompareEntities("WarParticipantBinding", before.WarParticipantBindings, after.WarParticipantBindings,
            binding => binding.WarId + "\u001f" + binding.BindingId,
            (identity, left, right) =>
            {
                CompareValue("WarParticipantBinding", identity, "SideId", WorldStateCanonicalWriter.StringValue(left.SideId), WorldStateCanonicalWriter.StringValue(right.SideId), differences);
                CompareValue("WarParticipantBinding", identity, "ArmedForceId", WorldStateCanonicalWriter.StringValue(left.ArmedForceId), WorldStateCanonicalWriter.StringValue(right.ArmedForceId), differences);
            }, differences);

        ComparePersistentState("BattleStore", before.HasBattleState, before.BattleRevision, after.HasBattleState, after.BattleRevision, differences);
        CompareEntities("Battle", before.Battles, after.Battles,
            battle => battle.BattleId,
            (identity, left, right) =>
            {
                CompareValue("Battle", identity, "CreatedAbsoluteDay", WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay), WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay), differences);
                CompareValue("Battle", identity, "StartedAbsoluteDay", WorldStateCanonicalWriter.NullableInt64Value(left.StartedAbsoluteDay), WorldStateCanonicalWriter.NullableInt64Value(right.StartedAbsoluteDay), differences);
                CompareValue("Battle", identity, "LifecycleState", WorldStateCanonicalWriter.EnumValue(left.LifecycleState), WorldStateCanonicalWriter.EnumValue(right.LifecycleState), differences);
                CompareValue("Battle", identity, "ConflictId", WorldStateCanonicalWriter.StringValue(left.ConflictId), WorldStateCanonicalWriter.StringValue(right.ConflictId), differences);
                CompareValue("Battle", identity, "WarId", WorldStateCanonicalWriter.StringValue(left.WarId), WorldStateCanonicalWriter.StringValue(right.WarId), differences);
            }, differences);
        CompareEntities("BattleSide", before.BattleSides, after.BattleSides,
            side => side.BattleId + "\u001f" + side.SideId,
            (identity, left, right) => CompareValue("BattleSide", identity, "DisplayName", WorldStateCanonicalWriter.StringValue(left.DisplayName), WorldStateCanonicalWriter.StringValue(right.DisplayName), differences),
            differences);
        CompareEntities("BattleParticipantBinding", before.BattleParticipantBindings, after.BattleParticipantBindings,
            binding => binding.BattleId + "\u001f" + binding.BindingId,
            (identity, left, right) =>
            {
                CompareValue("BattleParticipantBinding", identity, "SideId", WorldStateCanonicalWriter.StringValue(left.SideId), WorldStateCanonicalWriter.StringValue(right.SideId), differences);
                CompareValue("BattleParticipantBinding", identity, "ArmedForceId", WorldStateCanonicalWriter.StringValue(left.ArmedForceId), WorldStateCanonicalWriter.StringValue(right.ArmedForceId), differences);
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

        CompareEntities("PoliticalClaimRecognition", before.PoliticalClaimRecognitions, after.PoliticalClaimRecognitions,
            recognition => recognition.ClaimId + "\u001f" + recognition.InstitutionId,
            (identity, left, right) =>
            {
                CompareValue("PoliticalClaimRecognition", identity, "State",
                    WorldStateCanonicalWriter.EnumValue(left.State),
                    WorldStateCanonicalWriter.EnumValue(right.State),
                    differences);
                CompareValue("PoliticalClaimRecognition", identity, "RecognitionAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.RecognitionAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.RecognitionAbsoluteDay),
                    differences);
                CompareValue("PoliticalClaimRecognition", identity, "Reason",
                    WorldStateCanonicalWriter.StringValue(left.Reason),
                    WorldStateCanonicalWriter.StringValue(right.Reason),
                    differences);
                CompareValue("PoliticalClaimRecognition", identity, "History",
                    RecognitionHistoryValue(left.History),
                    RecognitionHistoryValue(right.History),
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
                CompareValue("Faction", identity, "MembershipPolicy",
                    WorldStateCanonicalWriter.EnumValue(left.MembershipPolicy),
                    WorldStateCanonicalWriter.EnumValue(right.MembershipPolicy),
                    differences);
                CompareValue("Faction", identity, "ExpulsionAllowed",
                    WorldStateCanonicalWriter.BoolValue(left.ExpulsionAllowed),
                    WorldStateCanonicalWriter.BoolValue(right.ExpulsionAllowed),
                    differences);
            },
            differences);

        CompareEntities("FactionAffiliation", before.FactionAffiliations, after.FactionAffiliations,
            affiliation => affiliation.AffiliationId ?? affiliation.FactionId + "\u001f" + affiliation.PersonId,
            (identity, left, right) =>
            {
                CompareValue("FactionAffiliation", identity, "AffiliationId",
                    WorldStateCanonicalWriter.StringValue(left.AffiliationId),
                    WorldStateCanonicalWriter.StringValue(right.AffiliationId),
                    differences);
                CompareValue("FactionAffiliation", identity, "JoinedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.JoinedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.JoinedAbsoluteDay),
                    differences);
                CompareValue("FactionAffiliation", identity, "EndedAbsoluteDay",
                    WorldStateCanonicalWriter.NullableInt64Value(left.EndedAbsoluteDay),
                    WorldStateCanonicalWriter.NullableInt64Value(right.EndedAbsoluteDay),
                    differences);
                CompareValue("FactionAffiliation", identity, "EndReason",
                    WorldStateCanonicalWriter.EnumValueOrNull(left.EndReason),
                    WorldStateCanonicalWriter.EnumValueOrNull(right.EndReason),
                    differences);
            },
            differences);

        CompareEntities("PoliticalSupport", before.PoliticalSupports, after.PoliticalSupports,
            support => support.RelationId,
            (identity, left, right) =>
            {
                CompareValue("PoliticalSupport", identity, "SourceKind",
                    WorldStateCanonicalWriter.EnumValue(left.SourceKind),
                    WorldStateCanonicalWriter.EnumValue(right.SourceKind),
                    differences);
                CompareValue("PoliticalSupport", identity, "SourceId",
                    WorldStateCanonicalWriter.StringValue(left.SourceId),
                    WorldStateCanonicalWriter.StringValue(right.SourceId),
                    differences);
                CompareValue("PoliticalSupport", identity, "TargetKind",
                    WorldStateCanonicalWriter.EnumValue(left.TargetKind),
                    WorldStateCanonicalWriter.EnumValue(right.TargetKind),
                    differences);
                CompareValue("PoliticalSupport", identity, "TargetId",
                    WorldStateCanonicalWriter.StringValue(left.TargetId),
                    WorldStateCanonicalWriter.StringValue(right.TargetId),
                    differences);
                CompareValue("PoliticalSupport", identity, "Disposition",
                    WorldStateCanonicalWriter.EnumValue(left.Disposition),
                    WorldStateCanonicalWriter.EnumValue(right.Disposition),
                    differences);
                CompareValue("PoliticalSupport", identity, "StartedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.StartedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.StartedAbsoluteDay),
                    differences);
                CompareValue("PoliticalSupport", identity, "EndedAbsoluteDay",
                    WorldStateCanonicalWriter.NullableInt64Value(left.EndedAbsoluteDay),
                    WorldStateCanonicalWriter.NullableInt64Value(right.EndedAbsoluteDay),
                    differences);
            },
            differences);

        CompareEntities("PoliticalDecision", before.PoliticalDecisions, after.PoliticalDecisions,
            decision => decision.DecisionId,
            (identity, left, right) =>
            {
                CompareValue("PoliticalDecision", identity, "DeciderStableId",
                    WorldStateCanonicalWriter.StringValue(left.DeciderStableId),
                    WorldStateCanonicalWriter.StringValue(right.DeciderStableId),
                    differences);
                CompareValue("PoliticalDecision", identity, "DecisionKind",
                    WorldStateCanonicalWriter.EnumValue(left.DecisionKind),
                    WorldStateCanonicalWriter.EnumValue(right.DecisionKind),
                    differences);
                CompareValue("PoliticalDecision", identity, "OfficeId",
                    WorldStateCanonicalWriter.StringValue(left.OfficeId),
                    WorldStateCanonicalWriter.StringValue(right.OfficeId),
                    differences);
                CompareValue("PoliticalDecision", identity, "RecognizingInstitutionId",
                    WorldStateCanonicalWriter.StringValue(left.RecognizingInstitutionId),
                    WorldStateCanonicalWriter.StringValue(right.RecognizingInstitutionId),
                    differences);
                CompareValue("PoliticalDecision", identity, "CandidateFingerprint",
                    WorldStateCanonicalWriter.StringValue(left.CandidateFingerprint),
                    WorldStateCanonicalWriter.StringValue(right.CandidateFingerprint),
                    differences);
                CompareValue("PoliticalDecision", identity, "OutcomeKind",
                    WorldStateCanonicalWriter.EnumValue(left.OutcomeKind),
                    WorldStateCanonicalWriter.EnumValue(right.OutcomeKind),
                    differences);
                CompareValue("PoliticalDecision", identity, "SelectedCandidatePersonId",
                    WorldStateCanonicalWriter.StringValue(left.SelectedCandidatePersonId),
                    WorldStateCanonicalWriter.StringValue(right.SelectedCandidatePersonId),
                    differences);
                CompareValue("PoliticalDecision", identity, "ReferencedClaimId",
                    WorldStateCanonicalWriter.StringValue(left.ReferencedClaimId),
                    WorldStateCanonicalWriter.StringValue(right.ReferencedClaimId),
                    differences);
                CompareValue("PoliticalDecision", identity, "EvidenceReferences",
                    WorldStateCanonicalWriter.StringListValue(left.EvidenceReferences),
                    WorldStateCanonicalWriter.StringListValue(right.EvidenceReferences),
                    differences);
                CompareValue("PoliticalDecision", identity, "KnowledgeReferences",
                    WorldStateCanonicalWriter.StringListValue(left.KnowledgeReferences),
                    WorldStateCanonicalWriter.StringListValue(right.KnowledgeReferences),
                    differences);
                CompareValue("PoliticalDecision", identity, "ObservedAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.ObservedAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.ObservedAbsoluteDay),
                    differences);
                CompareValue("PoliticalDecision", identity, "DecisionAbsoluteDay",
                    WorldStateCanonicalWriter.Int64Value(left.DecisionAbsoluteDay),
                    WorldStateCanonicalWriter.Int64Value(right.DecisionAbsoluteDay),
                    differences);
                CompareValue("PoliticalDecision", identity, "ExpectedWorldRevision",
                    WorldStateCanonicalWriter.Int64Value(left.ExpectedWorldRevision),
                    WorldStateCanonicalWriter.Int64Value(right.ExpectedWorldRevision),
                    differences);
                CompareValue("PoliticalDecision", identity, "ExpectedKnowledgeRevision",
                    WorldStateCanonicalWriter.Int64Value(left.ExpectedKnowledgeRevision),
                    WorldStateCanonicalWriter.Int64Value(right.ExpectedKnowledgeRevision),
                    differences);
            },
            differences);

        CompareEntities("PoliticalKnowledge", before.PoliticalKnowledge, after.PoliticalKnowledge,
            knowledge => knowledge.HolderStableId,
            (identity, left, right) =>
            {
                CompareValue("PoliticalKnowledge", identity, "HolderKind",
                    WorldStateCanonicalWriter.EnumValue(left.HolderKind),
                    WorldStateCanonicalWriter.EnumValue(right.HolderKind),
                    differences);
                CompareValue("PoliticalKnowledge", identity, "HolderPersonId",
                    WorldStateCanonicalWriter.StringValue(left.HolderPersonId),
                    WorldStateCanonicalWriter.StringValue(right.HolderPersonId),
                    differences);
                CompareValue("PoliticalKnowledge", identity, "HolderInstitutionId",
                    WorldStateCanonicalWriter.StringValue(left.HolderInstitutionId),
                    WorldStateCanonicalWriter.StringValue(right.HolderInstitutionId),
                    differences);
                CompareValue("PoliticalKnowledge", identity, "HolderFactionId",
                    WorldStateCanonicalWriter.StringValue(left.HolderFactionId),
                    WorldStateCanonicalWriter.StringValue(right.HolderFactionId),
                    differences);
                CompareEntities("PoliticalKnowledgeObservation", left.Observations, right.Observations,
                    observation => identity + "/" + observation.IdentityKey,
                    (observationIdentity, observationLeft, observationRight) =>
                    {
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "FactKind",
                            WorldStateCanonicalWriter.EnumValue(observationLeft.FactKind),
                            WorldStateCanonicalWriter.EnumValue(observationRight.FactKind),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "StateKey",
                            WorldStateCanonicalWriter.StringValue(observationLeft.StateKey),
                            WorldStateCanonicalWriter.StringValue(observationRight.StateKey),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "ObservedAbsoluteDay",
                            WorldStateCanonicalWriter.Int64Value(observationLeft.ObservedAbsoluteDay),
                            WorldStateCanonicalWriter.Int64Value(observationRight.ObservedAbsoluteDay),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "ReceivedAbsoluteDay",
                            WorldStateCanonicalWriter.Int64Value(observationLeft.ReceivedAbsoluteDay),
                            WorldStateCanonicalWriter.Int64Value(observationRight.ReceivedAbsoluteDay),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "Source",
                            WorldStateCanonicalWriter.EnumValue(observationLeft.Source),
                            WorldStateCanonicalWriter.EnumValue(observationRight.Source),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "SourceReference",
                            WorldStateCanonicalWriter.StringValue(observationLeft.SourceReference),
                            WorldStateCanonicalWriter.StringValue(observationRight.SourceReference),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "SourcePersonId",
                            WorldStateCanonicalWriter.StringValue(observationLeft.SourcePersonId),
                            WorldStateCanonicalWriter.StringValue(observationRight.SourcePersonId),
                            differences);
                        CompareValue("PoliticalKnowledgeObservation", observationIdentity, "SourceInstitutionId",
                            WorldStateCanonicalWriter.StringValue(observationLeft.SourceInstitutionId),
                            WorldStateCanonicalWriter.StringValue(observationRight.SourceInstitutionId),
                            differences);
                    },
                    differences);
            },
            differences);

        CompareEntities("TheftOutcome", before.TheftOutcomes, after.TheftOutcomes,
            outcome => outcome.OutcomeId,
            (identity, left, right) =>
            {
                CompareValue("TheftOutcome", identity, "PerpetratorPersonId", WorldStateCanonicalWriter.StringValue(left.PerpetratorPersonId), WorldStateCanonicalWriter.StringValue(right.PerpetratorPersonId), differences);
                CompareValue("TheftOutcome", identity, "VictimPersonId", WorldStateCanonicalWriter.StringValue(left.VictimPersonId), WorldStateCanonicalWriter.StringValue(right.VictimPersonId), differences);
                CompareValue("TheftOutcome", identity, "LossAmount", WorldStateCanonicalWriter.IntValue(left.LossAmount), WorldStateCanonicalWriter.IntValue(right.LossAmount), differences);
                CompareValue("TheftOutcome", identity, "OccurredAbsoluteDay", WorldStateCanonicalWriter.Int64Value(left.OccurredAbsoluteDay), WorldStateCanonicalWriter.Int64Value(right.OccurredAbsoluteDay), differences);
                CompareValue("TheftOutcome", identity, "OccurrenceKey", WorldStateCanonicalWriter.StringValue(left.OccurrenceKey), WorldStateCanonicalWriter.StringValue(right.OccurrenceKey), differences);
                CompareValue("TheftOutcome", identity, "OriginDecisionId", WorldStateCanonicalWriter.StringValue(left.OriginDecisionId), WorldStateCanonicalWriter.StringValue(right.OriginDecisionId), differences);
            },
            differences);

        CompareEntities("CrimeKnowledge", before.CrimeKnowledge, after.CrimeKnowledge,
            observation => observation.EvaluatorPersonId + "\u001f" + observation.OutcomeId,
            (identity, left, right) =>
            {
                CompareValue("CrimeKnowledge", identity, "Role", WorldStateCanonicalWriter.EnumValue(left.Role), WorldStateCanonicalWriter.EnumValue(right.Role), differences);
                CompareValue("CrimeKnowledge", identity, "KnowsLoss", WorldStateCanonicalWriter.BoolValue(left.KnowsLoss), WorldStateCanonicalWriter.BoolValue(right.KnowsLoss), differences);
                CompareValue("CrimeKnowledge", identity, "PerceivedPerpetratorKind", WorldStateCanonicalWriter.EnumValue(left.PerceivedPerpetratorKind), WorldStateCanonicalWriter.EnumValue(right.PerceivedPerpetratorKind), differences);
                CompareValue("CrimeKnowledge", identity, "PerceivedPerpetratorPersonId", WorldStateCanonicalWriter.StringValue(left.PerceivedPerpetratorPersonId), WorldStateCanonicalWriter.StringValue(right.PerceivedPerpetratorPersonId), differences);
                CompareValue("CrimeKnowledge", identity, "PerceivedPerpetratorInstitutionId", WorldStateCanonicalWriter.StringValue(left.PerceivedPerpetratorInstitutionId), WorldStateCanonicalWriter.StringValue(right.PerceivedPerpetratorInstitutionId), differences);
                CompareValue("CrimeKnowledge", identity, "KnownInvestigatorPersonId", WorldStateCanonicalWriter.StringValue(left.KnownInvestigatorPersonId), WorldStateCanonicalWriter.StringValue(right.KnownInvestigatorPersonId), differences);
                CompareValue("CrimeKnowledge", identity, "KnownInvestigatorInstitutionId", WorldStateCanonicalWriter.StringValue(left.KnownInvestigatorInstitutionId), WorldStateCanonicalWriter.StringValue(right.KnownInvestigatorInstitutionId), differences);
                CompareValue("CrimeKnowledge", identity, "CognitiveBasisKind", WorldStateCanonicalWriter.EnumValue(left.CognitiveBasisKind), WorldStateCanonicalWriter.EnumValue(right.CognitiveBasisKind), differences);
                CompareValue("CrimeKnowledge", identity, "CognitiveBasisReference", WorldStateCanonicalWriter.StringValue(left.CognitiveBasisReference), WorldStateCanonicalWriter.StringValue(right.CognitiveBasisReference), differences);
                CompareValue("CrimeKnowledge", identity, "CognitiveBasisSourcePersonId", WorldStateCanonicalWriter.StringValue(left.CognitiveBasisSourcePersonId), WorldStateCanonicalWriter.StringValue(right.CognitiveBasisSourcePersonId), differences);
                CompareValue("CrimeKnowledge", identity, "CognitiveBasisSourceInstitutionId", WorldStateCanonicalWriter.StringValue(left.CognitiveBasisSourceInstitutionId), WorldStateCanonicalWriter.StringValue(right.CognitiveBasisSourceInstitutionId), differences);
                CompareValue("CrimeKnowledge", identity, "ObservedAbsoluteDay", WorldStateCanonicalWriter.Int64Value(left.ObservedAbsoluteDay), WorldStateCanonicalWriter.Int64Value(right.ObservedAbsoluteDay), differences);
            },
            differences);

        CompareEntities("SocialReaction", before.SocialReactions, after.SocialReactions,
            reaction => reaction.ReactionId,
            (identity, left, right) =>
            {
                CompareValue("SocialReaction", identity, "EvaluatorPersonId", WorldStateCanonicalWriter.StringValue(left.EvaluatorPersonId), WorldStateCanonicalWriter.StringValue(right.EvaluatorPersonId), differences);
                CompareValue("SocialReaction", identity, "SourceDomain", WorldStateCanonicalWriter.StringValue(left.SourceDomain), WorldStateCanonicalWriter.StringValue(right.SourceDomain), differences);
                CompareValue("SocialReaction", identity, "SourceStableId", WorldStateCanonicalWriter.StringValue(left.SourceStableId), WorldStateCanonicalWriter.StringValue(right.SourceStableId), differences);
                CompareValue("SocialReaction", identity, "TargetKind", WorldStateCanonicalWriter.EnumValue(left.TargetKind), WorldStateCanonicalWriter.EnumValue(right.TargetKind), differences);
                CompareValue("SocialReaction", identity, "TargetStableId", WorldStateCanonicalWriter.StringValue(left.TargetStableId), WorldStateCanonicalWriter.StringValue(right.TargetStableId), differences);
                CompareValue("SocialReaction", identity, "AttributionKind", WorldStateCanonicalWriter.EnumValue(left.AttributionKind), WorldStateCanonicalWriter.EnumValue(right.AttributionKind), differences);
                CompareValue("SocialReaction", identity, "AttributionPersonId", WorldStateCanonicalWriter.StringValue(left.AttributionPersonId), WorldStateCanonicalWriter.StringValue(right.AttributionPersonId), differences);
                CompareValue("SocialReaction", identity, "AttributionInstitutionId", WorldStateCanonicalWriter.StringValue(left.AttributionInstitutionId), WorldStateCanonicalWriter.StringValue(right.AttributionInstitutionId), differences);
                CompareValue("SocialReaction", identity, "Valence", WorldStateCanonicalWriter.EnumValue(left.Valence), WorldStateCanonicalWriter.EnumValue(right.Valence), differences);
                CompareValue("SocialReaction", identity, "Salience", WorldStateCanonicalWriter.EnumValue(left.Salience), WorldStateCanonicalWriter.EnumValue(right.Salience), differences);
                CompareValue("SocialReaction", identity, "CognitiveBasisKind", WorldStateCanonicalWriter.EnumValue(left.CognitiveBasisKind), WorldStateCanonicalWriter.EnumValue(right.CognitiveBasisKind), differences);
                CompareValue("SocialReaction", identity, "CognitiveBasisReference", WorldStateCanonicalWriter.StringValue(left.CognitiveBasisReference), WorldStateCanonicalWriter.StringValue(right.CognitiveBasisReference), differences);
                CompareValue("SocialReaction", identity, "CognitiveBasisSourcePersonId", WorldStateCanonicalWriter.StringValue(left.CognitiveBasisSourcePersonId), WorldStateCanonicalWriter.StringValue(right.CognitiveBasisSourcePersonId), differences);
                CompareValue("SocialReaction", identity, "CognitiveBasisSourceInstitutionId", WorldStateCanonicalWriter.StringValue(left.CognitiveBasisSourceInstitutionId), WorldStateCanonicalWriter.StringValue(right.CognitiveBasisSourceInstitutionId), differences);
                CompareValue("SocialReaction", identity, "CreatedAbsoluteDay", WorldStateCanonicalWriter.Int64Value(left.CreatedAbsoluteDay), WorldStateCanonicalWriter.Int64Value(right.CreatedAbsoluteDay), differences);
                CompareValue("SocialReaction", identity, "SupersedesReactionId", WorldStateCanonicalWriter.StringValue(left.SupersedesReactionId), WorldStateCanonicalWriter.StringValue(right.SupersedesReactionId), differences);
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

    private static void ComparePersistentState(
        string section,
        bool beforePresent,
        long? beforeRevision,
        bool afterPresent,
        long? afterRevision,
        List<WorldStateDifference> differences)
    {
        CompareValue(section, "world", "StatePresent", WorldStateCanonicalWriter.BoolValue(beforePresent), WorldStateCanonicalWriter.BoolValue(afterPresent), differences);
        CompareValue(section, "world", "Revision", beforeRevision.HasValue ? WorldStateCanonicalWriter.Int64Value(beforeRevision.Value) : null, afterRevision.HasValue ? WorldStateCanonicalWriter.Int64Value(afterRevision.Value) : null, differences);
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

    private static string RecognitionHistoryValue(
        IReadOnlyList<WorldStatePoliticalClaimRecognitionHistorySnapshot> history)
    {
        if (history == null || history.Count == 0)
        {
            return string.Empty;
        }

        List<string> entries = new List<string>();
        foreach (WorldStatePoliticalClaimRecognitionHistorySnapshot entry in history)
        {
            if (entry == null)
            {
                continue;
            }

            entries.Add(
                WorldStateCanonicalWriter.EnumValue(entry.State)
                + "@"
                + WorldStateCanonicalWriter.Int64Value(entry.RecognitionAbsoluteDay)
                + "@"
                + WorldStateCanonicalWriter.StringValue(entry.Reason));
        }

        return string.Join(";", entries.ToArray());
    }

    private static string ArmedForceCharacteristicsValue(
        IReadOnlyList<WorldStateArmedForceCharacteristicSnapshot> characteristics)
    {
        if (characteristics == null || characteristics.Count == 0)
        {
            return string.Empty;
        }

        List<string> entries = new List<string>();
        foreach (WorldStateArmedForceCharacteristicSnapshot characteristic in characteristics)
        {
            if (characteristic != null)
            {
                entries.Add(
                    WorldStateCanonicalWriter.StringValue(characteristic.Key)
                    + "="
                    + WorldStateCanonicalWriter.StringValue(characteristic.Value));
            }
        }

        return string.Join(";", entries.ToArray());
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
            case "ArmedForceStore": return 4;
            case "ArmedForce": return 5;
            case "ArmedForceContingent": return 6;
            case "ArmedForcePersonReference": return 7;
            case "ConflictStore": return 8;
            case "Conflict": return 9;
            case "ConflictSide": return 10;
            case "ConflictParticipantBinding": return 11;
            case "WarStore": return 12;
            case "War": return 13;
            case "WarSide": return 14;
            case "WarParticipantBinding": return 15;
            case "BattleStore": return 16;
            case "Battle": return 17;
            case "BattleSide": return 18;
            case "BattleParticipantBinding": return 19;
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
