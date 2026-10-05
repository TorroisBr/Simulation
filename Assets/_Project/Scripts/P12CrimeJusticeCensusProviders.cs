using System;
using System.Collections.Generic;

/// <summary>Exact selected-profile witnesses for NPC status and legacy Justice facts.</summary>
public static class P12CrimeJusticeCensusProvider
{
    public const string NpcStatusSectionPrefix = "p12b.npc-status-crime-state/";
    public const string JusticeRecordsSectionId = "p12b.justice-records";
    public const string CrimeP18ReceiptsSectionId = "p12b.crime-p18-receipts";
    public const string JusticeP18ReceiptsSectionId = "p12b.justice-p18-receipts";
    public const int SchemaVersion = 1;

    public static string NpcStatusSectionIdFor(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("NPC status census requires a RuntimeId.", nameof(runtimeId));
        return NpcStatusSectionPrefix + runtimeId;
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateNpcStatusProviders(
        IReadOnlyList<NpcRuntime> roster)
    {
        if (roster == null) throw new ArgumentNullException(nameof(roster));
        List<NpcRuntime> ordered = new List<NpcRuntime>(roster.Count);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<NpcRuntime> owners = new HashSet<NpcRuntime>();
        foreach (NpcRuntime npc in roster)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !ids.Add(npc.RuntimeId) || !owners.Add(npc))
                throw new InvalidOperationException(
                    "Crime/Justice census requires exact NPC owners with unique RuntimeIds.");
            ordered.Add(npc);
        }

        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
            providers[i] = new NpcStatusSectionProvider(ordered[i]);
        return Array.AsReadOnly(providers);
    }

    public sealed class NpcStatusSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly NpcRuntime owner;

        public NpcStatusSectionProvider(NpcRuntime owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (string.IsNullOrWhiteSpace(owner.RuntimeId))
                throw new ArgumentException("NPC status census requires a RuntimeId.", nameof(owner));
        }

        public string RuntimeId => owner.RuntimeId;
        public NpcRuntime NpcOwner => owner;
        public string SectionId => NpcStatusSectionIdFor(owner.RuntimeId);

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(
                SectionId,
                SchemaVersion,
                owner,
                1,
                owner.P12CrimeJusticeRevision);
    }

    public sealed class JusticeRecordsSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly JusticeSystem owner;
        private readonly Func<NpcRuntime, bool> isNpcInstalled;
        private readonly Func<CityRuntime, bool> isCityInstalled;

        public JusticeRecordsSectionProvider(
            JusticeSystem owner,
            Func<NpcRuntime, bool> isNpcInstalled,
            Func<CityRuntime, bool> isCityInstalled)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.isNpcInstalled = isNpcInstalled ?? throw new ArgumentNullException(nameof(isNpcInstalled));
            this.isCityInstalled = isCityInstalled ?? throw new ArgumentNullException(nameof(isCityInstalled));
        }

        public JusticeSystem JusticeOwner => owner;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (!owner.TryReadP12CrimeJusticeCensus(
                    isNpcInstalled,
                    isCityInstalled,
                    out int wantedCount,
                    out int sentenceCount,
                    out long revision))
                throw new InvalidOperationException("Justice rows do not resolve to the current selected owners.");

            int cardinality = checked(wantedCount + sentenceCount);
            return new OwnerSectionCensusWitness(
                JusticeRecordsSectionId,
                SchemaVersion,
                owner,
                cardinality,
                revision);
        }
    }

    public sealed class CrimeP18ReceiptsSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly CrimeSystem owner;

        public CrimeP18ReceiptsSectionProvider(CrimeSystem owner) =>
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

        public CrimeSystem CrimeOwner => owner;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return new OwnerSectionCensusWitness(
                CrimeP18ReceiptsSectionId,
                SchemaVersion,
                owner,
                1,
                owner.P12P18ReceiptCensusRevision);
        }
    }

    public sealed class JusticeP18ReceiptsSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly JusticeSystem owner;

        public JusticeP18ReceiptsSectionProvider(JusticeSystem owner) =>
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

        public JusticeSystem JusticeOwner => owner;

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return new OwnerSectionCensusWitness(
                JusticeP18ReceiptsSectionId,
                SchemaVersion,
                owner,
                1,
                owner.P12P18ReceiptCensusRevision);
        }
    }
}
