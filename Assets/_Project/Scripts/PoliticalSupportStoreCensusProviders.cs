using System.Collections.Generic;

/// <summary>Passive witness for the installed runtime PoliticalSupportStore.</summary>
public static class PoliticalSupportStoreCensusProvider
{
    public const int SchemaVersion = 1;
    public const string RelationsSectionId = "p12e.political-support.relations";

    private sealed class RelationProvider : IOwnerSectionCensusProvider
    {
        private readonly PoliticalSupportStore owner;

        public RelationProvider(PoliticalSupportStore owner)
        {
            this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return new OwnerSectionCensusWitness(
                RelationsSectionId,
                SchemaVersion,
                owner,
                owner.Count,
                owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        PoliticalSupportStore owner)
    {
        if (owner == null) throw new System.ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new RelationProvider(owner)
        };
        return System.Array.AsReadOnly(providers);
    }
}
