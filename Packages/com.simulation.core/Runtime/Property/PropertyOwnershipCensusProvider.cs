using System;
using System.Collections.Generic;

internal enum PropertyOwnershipCensusSection
{
    Ownership,
    TransferHistory
}

/// <summary>Fixed passive census providers for the installed property owner.</summary>
public static class PropertyOwnershipCensusProvider
{
    public const int SchemaVersion = 1;
    public const string OwnershipSectionId = "p12e.property.ownership";
    public const string TransferHistorySectionId = "p12e.property.transfer-history";

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly PropertyOwnershipStore owner;
        private readonly PropertyOwnershipCensusSection section;
        private readonly string sectionId;

        public SectionProvider(
            PropertyOwnershipStore owner,
            PropertyOwnershipCensusSection section,
            string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == PropertyOwnershipCensusSection.Ownership
                ? owner.Count
                : owner.TransferHistory.Count;
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                cardinality,
                owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        PropertyOwnershipStore owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new SectionProvider(
                owner,
                PropertyOwnershipCensusSection.Ownership,
                OwnershipSectionId),
            new SectionProvider(
                owner,
                PropertyOwnershipCensusSection.TransferHistory,
                TransferHistorySectionId)
        };
        return Array.AsReadOnly(providers);
    }
}
