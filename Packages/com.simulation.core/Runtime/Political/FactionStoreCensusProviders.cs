using System;
using System.Collections.Generic;

internal enum FactionStoreCensusSection
{
    Factions,
    Affiliations
}

/// <summary>Fixed passive witnesses for the installed runtime FactionStore.</summary>
public static class FactionStoreCensusProvider
{
    public const int SchemaVersion = 1;
    public const string FactionsSectionId = "p12e.faction.records";
    public const string AffiliationsSectionId = "p12e.faction.affiliations";

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly FactionStore owner;
        private readonly FactionStoreCensusSection section;
        private readonly string sectionId;

        public SectionProvider(
            FactionStore owner,
            FactionStoreCensusSection section,
            string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == FactionStoreCensusSection.Factions
                ? owner.Count
                : owner.AffiliationCount;
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                cardinality,
                owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(FactionStore owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new SectionProvider(owner, FactionStoreCensusSection.Factions, FactionsSectionId),
            new SectionProvider(owner, FactionStoreCensusSection.Affiliations, AffiliationsSectionId)
        };
        return Array.AsReadOnly(providers);
    }
}
