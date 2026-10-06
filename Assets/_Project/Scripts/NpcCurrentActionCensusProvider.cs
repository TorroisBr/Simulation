using System;
using System.Collections.Generic;

/// <summary>Passive per-NPC witness for its optional current action slot.</summary>
public sealed class NpcCurrentActionCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionPrefix = "p12b.npc-current-action/";
    public const int SchemaVersion = 1;

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
            owner,
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
                || !npc.HasConsistentCurrentActionSlot
                || (npc.CurrentActionRuntime != null
                    && (!actionOwners.Add(npc.CurrentActionRuntime)
                        || (validateActionReferences != null
                            && !validateActionReferences(npc, npc.CurrentActionRuntime)))))
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
}
