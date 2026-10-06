using System;
using System.Collections.Generic;

internal enum PoliticalClaimStoreCensusSection
{
    Claims,
    Recognitions
}

/// <summary>Fixed passive witnesses for the installed runtime PoliticalClaimStore.</summary>
public static class PoliticalClaimStoreCensusProvider
{
    public const int SchemaVersion = 1;
    public const string ClaimsSectionId = "p12e.political-claim.records";
    public const string RecognitionsSectionId = "p12e.political-claim.recognitions";

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly PoliticalClaimStore owner;
        private readonly PoliticalClaimStoreCensusSection section;
        private readonly string sectionId;

        public SectionProvider(
            PoliticalClaimStore owner,
            PoliticalClaimStoreCensusSection section,
            string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == PoliticalClaimStoreCensusSection.Claims
                ? owner.Count
                : owner.RecognitionCount;
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                cardinality,
                owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(PoliticalClaimStore owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new SectionProvider(owner, PoliticalClaimStoreCensusSection.Claims, ClaimsSectionId),
            new SectionProvider(owner, PoliticalClaimStoreCensusSection.Recognitions, RecognitionsSectionId)
        };
        return Array.AsReadOnly(providers);
    }
}
