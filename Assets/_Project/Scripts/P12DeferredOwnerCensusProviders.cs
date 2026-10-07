using System;

/// <summary>Passive census witness for the selected Daily-v1 political Knowledge owner.</summary>
public sealed class PoliticalKnowledgeStoreCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12f.political-knowledge.holders";
    public const int SchemaVersion = 1;

    private readonly PoliticalKnowledgeStore owner;

    public PoliticalKnowledgeStoreCensusProvider(PoliticalKnowledgeStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}

/// <summary>Passive census witness for the selected Daily-v1 political decision owner.</summary>
public sealed class PoliticalDecisionStoreCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12f.political-decisions.records";
    public const int SchemaVersion = 1;

    private readonly PoliticalDecisionStore owner;

    public PoliticalDecisionStoreCensusProvider(PoliticalDecisionStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(SectionId, SchemaVersion, owner, owner.Count, owner.Revision);
    }
}
