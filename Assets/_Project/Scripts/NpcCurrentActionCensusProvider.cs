using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witness for its optional current action slot.</summary>
public sealed class NpcCurrentActionCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix;
    public const int SchemaVersion = DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion;

    private readonly NpcRuntime owner;
    private readonly Func<NpcRuntime, NpcActionRuntime, bool> validateActionReferences;

    public NpcCurrentActionCensusProvider(
        NpcRuntime owner,
        Func<NpcRuntime, NpcActionRuntime, bool> validateActionReferences = null)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        if (string.IsNullOrWhiteSpace(owner.RuntimeId))
            throw new ArgumentException("NPC current-action census requires a RuntimeId.", nameof(owner));
        this.validateActionReferences = validateActionReferences;
    }

    public NpcRuntime NpcOwner => owner;
    public string SectionId => SectionIdFor(owner.RuntimeId);

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        if (owner.SelectedRuntimeOwners != null)
        {
            if (!TryReadSelectedCoreActionSlot(
                    owner,
                    out DailyV1NpcActionSlotOwner selectedActionSlot,
                    out int selectedCardinality))
                throw new InvalidOperationException("NPC current-action witness is not a valid exact Core owner slot.");

            NpcActionRuntime selectedProjection = owner.CurrentActionRuntime;
            if (selectedProjection != null
                && validateActionReferences != null
                && !validateActionReferences(owner, selectedProjection))
                throw new InvalidOperationException("NPC current-action projection does not match its exact Core owner slot.");

            return new OwnerSectionCensusWitness(
                SectionId,
                SchemaVersion,
                owner.SelectedCensusOwnerIdentity,
                selectedCardinality,
                selectedActionSlot.Revision);
        }

        NpcActionRuntime actionRuntime = owner.CurrentActionRuntime;
        if (!owner.HasConsistentCurrentActionSlot
            || (actionRuntime != null
                && (actionRuntime.Action == null
                    || (validateActionReferences != null
                        && !validateActionReferences(owner, actionRuntime)))))
        {
            throw new InvalidOperationException("NPC current-action witness is not a valid exact owner slot.");
        }

        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner.SelectedCensusOwnerIdentity,
            actionRuntime == null ? 0 : 1,
            owner.CurrentActionRevision);
    }

    public static string SectionIdFor(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("NPC current-action section requires a RuntimeId.", nameof(runtimeId));
        return SectionPrefix + runtimeId;
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<NpcRuntime> roster,
        Func<NpcRuntime, NpcActionRuntime, bool> validateActionReferences = null)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));

        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<NpcRuntime> owners = new HashSet<NpcRuntime>();
        HashSet<NpcActionRuntime> actionOwners = new HashSet<NpcActionRuntime>();
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !runtimeIds.Add(npc.RuntimeId)
                || !owners.Add(npc)
                || !new NpcCurrentActionCensusProvider(npc, validateActionReferences)
                    .IsCurrentActionSlotValid()
                || (npc.CurrentActionRuntime != null
                    && !actionOwners.Add(npc.CurrentActionRuntime)))
            {
                throw new ArgumentException(
                    "NPC current-action census requires unique exact owners and valid, exclusively owned action runtimes.",
                    nameof(roster));
            }
            ordered.Add(npc);
        }

        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
            providers[i] = new NpcCurrentActionCensusProvider(ordered[i], validateActionReferences);
        return Array.AsReadOnly(providers);
    }

    private bool IsCurrentActionSlotValid()
    {
        if (owner.SelectedRuntimeOwners != null)
        {
            if (!TryReadSelectedCoreActionSlot(owner, out _, out _)) return false;
            NpcActionRuntime selectedProjection = owner.CurrentActionRuntime;
            return selectedProjection == null
                || validateActionReferences == null
                || validateActionReferences(owner, selectedProjection);
        }

        NpcActionRuntime actionRuntime = owner.CurrentActionRuntime;
        return owner.HasConsistentCurrentActionSlot
            && (actionRuntime == null
                || (actionRuntime.Action != null
                    && (validateActionReferences == null
                        || validateActionReferences(owner, actionRuntime))));
    }

    private static bool TryReadSelectedCoreActionSlot(
        NpcRuntime npc,
        out DailyV1NpcActionSlotOwner actionSlot,
        out int cardinality)
    {
        actionSlot = null;
        cardinality = 0;
        DailyV1NpcRuntimeOwners selectedOwners = npc?.SelectedRuntimeOwners;
        DailyV1SelectedRuntimeOwnerGraph graph = npc?.SelectedRuntimeOwnerGraph;
        if (npc == null
            || selectedOwners == null
            || !string.Equals(selectedOwners.RuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            return false;

        actionSlot = selectedOwners.ActionSlot;
        if (actionSlot == null
            || actionSlot.Revision < 0L
            || !actionSlot.TryCapturePayload(out DailyV1NpcActionPayloadSnapshot payloadSnapshot))
            return false;

        // Some legacy/partial-roster hosts register their exact selected owner
        // before binding the complete runtime graph. Preserve their established
        // Unity projection path. A projection-free action requires the graph
        // below so its stable target IDs can be verified against Core owners.
        if (graph == null)
        {
            if (!npc.HasConsistentCurrentActionSlot) return false;
            cardinality = actionSlot.CurrentAction == null ? 0 : 1;
            return true;
        }

        bool graphOwnsExactNpc = graph != null
            && ReferenceEquals(selectedOwners.Definitions, graph.Definitions)
            && graph.TryGetNpcOwner(npc.RuntimeId, out DailyV1NpcRuntimeOwners exactOwner)
            && ReferenceEquals(exactOwner, selectedOwners)
            && graph.ContainsExactNpcOwner(selectedOwners);
        if (!graphOwnsExactNpc)
        {
            // Preserve legacy projection-backed hosts that have not completed
            // a whole selected-owner graph join. They cannot claim a
            // projection-free populated Core action slot.
            if (!npc.HasConsistentCurrentActionSlot
                || (actionSlot.CurrentAction != null && npc.CurrentActionRuntime == null))
                return false;
            cardinality = actionSlot.CurrentAction == null ? 0 : 1;
            return true;
        }

        DailyV1ActionDefinition action = actionSlot.CurrentAction;
        DailyV1NpcActionPayloadOwner payload = actionSlot.CurrentPayload;
        if (action == null)
        {
            if (payload != null || payloadSnapshot != null || npc.CurrentActionRuntime != null
                || npc.CurrentAction != null)
                return false;
            return true;
        }

        if (payload == null
            || payloadSnapshot == null
            || !graph.Definitions.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, action)
            || !ReferenceEquals(payload.Action, action))
            return false;

        NpcActionRuntime actionRuntime = npc.CurrentActionRuntime;
        if (actionRuntime == null)
        {
            if (payloadSnapshot.TargetNpcRuntimeId != null
                && (!graph.TryGetNpcOwner(
                        payloadSnapshot.TargetNpcRuntimeId, out DailyV1NpcRuntimeOwners targetNpc)
                    || targetNpc == null))
                return false;

            if (payloadSnapshot.TargetCityRuntimeId != null)
            {
                bool foundTargetCity = false;
                foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                    if (city != null && string.Equals(
                            city.RuntimeId, payloadSnapshot.TargetCityRuntimeId, StringComparison.Ordinal))
                    {
                        foundTargetCity = true;
                        break;
                    }
                if (!foundTargetCity) return false;
            }
        }

        if (payloadSnapshot.TargetItemDefinitionId != null
            && (!graph.Definitions.Items.TryGetValue(
                    payloadSnapshot.TargetItemDefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(admittedItem, payload.TargetItem)))
            return false;

        if (actionRuntime != null
            && (!ReferenceEquals(actionRuntime.SelectedOwner, payload)
                || !actionRuntime.MatchesSelectedOwner()
                || actionRuntime.Action == null
                || !ReferenceEquals(npc.CurrentAction, actionRuntime.Action)))
            return false;

        cardinality = 1;
        return true;
    }
}
