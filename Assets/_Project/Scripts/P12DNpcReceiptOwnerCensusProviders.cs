using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Passive selected-profile witnesses for the two P18 actor-owned receipt
/// ledgers that are deliberately absent from UnityBootstrap-Daily-v1.
/// </summary>
public static class P12DNpcReceiptOwnerCensusProvider
{
    public const string LocalObservationSectionPrefix =
        "p12d.npc-local-observation-receipts/";
    public const string MerchantTradeStateSectionPrefix =
        "p12d.npc-merchant-trade-state-receipts/";
    public const int SchemaVersion = 1;

    internal enum ReceiptOwnerKind
    {
        LocalObservation,
        MerchantTradeState
    }

    internal interface IReceiptOwnerSectionCensusProvider : IOwnerSectionCensusProvider
    {
        string RuntimeId { get; }
        NpcRuntime NpcOwner { get; }
        object ReceiptOwner { get; }
        ReceiptOwnerKind Kind { get; }
    }

    private sealed class SectionProvider : IReceiptOwnerSectionCensusProvider
    {
        private readonly NpcRuntime npcOwner;
        private readonly object receiptOwner;
        private readonly ReceiptOwnerKind kind;
        private readonly string sectionId;

        public SectionProvider(
            NpcRuntime npcOwner,
            object receiptOwner,
            ReceiptOwnerKind kind,
            string sectionId)
        {
            this.npcOwner = npcOwner ?? throw new ArgumentNullException(nameof(npcOwner));
            this.receiptOwner = receiptOwner ?? throw new ArgumentNullException(nameof(receiptOwner));
            this.kind = kind;
            this.sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
        }

        public string RuntimeId => npcOwner.RuntimeId;
        public NpcRuntime NpcOwner => npcOwner;
        public object ReceiptOwner => receiptOwner;
        public ReceiptOwnerKind Kind => kind;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            object currentOwner = kind == ReceiptOwnerKind.LocalObservation
                ? (object)npcOwner.ExistingLocalKnowledgeObservationRuntime
                : npcOwner.ExistingMerchantTradeStateRuntime;
            if (!ReferenceEquals(currentOwner, receiptOwner)
                || !TryReadExactZero(receiptOwner, kind, out int cardinality, out long revision)
                || cardinality != 0
                || revision != 0L)
            {
                throw new InvalidOperationException(
                    "The excluded P18 NPC receipt owner is missing, replaced, malformed, or populated.");
            }

            return new OwnerSectionCensusWitness(
                sectionId, SchemaVersion, receiptOwner, cardinality, revision);
        }
    }

    private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) =>
            value == null ? 0 : RuntimeHelpers.GetHashCode(value);
    }

    public static string LocalObservationSectionIdFor(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("Receipt census requires an NPC RuntimeId.", nameof(runtimeId));
        return LocalObservationSectionPrefix + runtimeId;
    }

    public static string MerchantTradeStateSectionIdFor(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("Receipt census requires an NPC RuntimeId.", nameof(runtimeId));
        return MerchantTradeStateSectionPrefix + runtimeId;
    }

    /// <summary>
    /// Creates exactly two required exact-zero witnesses per current NPC,
    /// sorted by ordinal RuntimeId and stable family order.
    /// </summary>
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));

        List<NpcRuntime> orderedNpcs = new List<NpcRuntime>(roster.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<object> npcOwners = new HashSet<object>(new ReferenceIdentityComparer());
        HashSet<object> receiptOwners = new HashSet<object>(new ReferenceIdentityComparer());
        for (int i = 0; i < roster.Count; i++)
        {
            NpcRuntime npc = roster[i];
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !runtimeIds.Add(npc.RuntimeId)
                || !npcOwners.Add(npc))
            {
                throw new ArgumentException(
                    "NPC receipt census requires non-null exact NPC owners with unique RuntimeIds.",
                    nameof(roster));
            }

            NpcLocalKnowledgeObservationRuntime localObservation =
                npc.ExistingLocalKnowledgeObservationRuntime;
            NpcMerchantTradeStateRuntime merchantTradeState =
                npc.ExistingMerchantTradeStateRuntime;
            if (localObservation == null
                || merchantTradeState == null
                || !receiptOwners.Add(localObservation)
                || !receiptOwners.Add(merchantTradeState))
            {
                throw new InvalidOperationException(
                    "Every NPC must own two distinct installed P18 receipt owners.");
            }

            orderedNpcs.Add(npc);
        }

        orderedNpcs.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers =
            new IOwnerSectionCensusProvider[orderedNpcs.Count * 2];
        int providerIndex = 0;
        foreach (NpcRuntime npc in orderedNpcs)
        {
            SectionProvider localObservationProvider = new SectionProvider(
                npc,
                npc.ExistingLocalKnowledgeObservationRuntime,
                ReceiptOwnerKind.LocalObservation,
                LocalObservationSectionIdFor(npc.RuntimeId));
            SectionProvider merchantTradeStateProvider = new SectionProvider(
                npc,
                npc.ExistingMerchantTradeStateRuntime,
                ReceiptOwnerKind.MerchantTradeState,
                MerchantTradeStateSectionIdFor(npc.RuntimeId));

            // A Required role permits populated values. The provider itself
            // therefore enforces the stricter profile-specific exact-zero rule
            // on its first read as well as every later read.
            localObservationProvider.GetCurrentCensus();
            merchantTradeStateProvider.GetCurrentCensus();
            providers[providerIndex++] = localObservationProvider;
            providers[providerIndex++] = merchantTradeStateProvider;
        }

        return Array.AsReadOnly(providers);
    }

    /// <summary>
    /// Confirms that a surfaced selected-profile provider list is the exact
    /// two-per-current-NPC inventory and still names its live embedded owners.
    /// </summary>
    internal static bool IsExactCoverage(
        IReadOnlyList<NpcRuntime> roster,
        IReadOnlyList<IOwnerSectionCensusProvider> providers)
    {
        if (roster == null || providers == null) return false;
        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> expected = CreateProviders(roster);
            if (expected.Count != providers.Count) return false;
            for (int i = 0; i < expected.Count; i++)
            {
                if (!(expected[i] is IReceiptOwnerSectionCensusProvider expectedProvider)
                    || !(providers[i] is IReceiptOwnerSectionCensusProvider actualProvider)
                    || !string.Equals(expectedProvider.RuntimeId, actualProvider.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(expectedProvider.GetCurrentCensus().SectionId,
                        actualProvider.GetCurrentCensus().SectionId, StringComparison.Ordinal)
                    || expectedProvider.Kind != actualProvider.Kind
                    || !ReferenceEquals(expectedProvider.NpcOwner, actualProvider.NpcOwner)
                    || !ReferenceEquals(expectedProvider.ReceiptOwner, actualProvider.ReceiptOwner))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadExactZero(
        object owner,
        ReceiptOwnerKind kind,
        out int cardinality,
        out long revision)
    {
        cardinality = 0;
        revision = 0L;
        if (kind == ReceiptOwnerKind.LocalObservation
            && owner is NpcLocalKnowledgeObservationRuntime localObservation)
        {
            return localObservation.TryReadP12ReceiptCensus(out cardinality, out revision);
        }
        if (kind == ReceiptOwnerKind.MerchantTradeState
            && owner is NpcMerchantTradeStateRuntime merchantTradeState)
        {
            return merchantTradeState.TryReadP12ReceiptCensus(out cardinality, out revision);
        }
        return false;
    }
}
