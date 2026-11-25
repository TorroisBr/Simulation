/// <summary>
/// Passive P12-B evidence for the installed genealogy owner's direct
/// parentage edges. The witness is unsynchronized and does not establish
/// capture eligibility.
/// </summary>
public sealed class GenealogyCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12d.genealogy.parentage";
    public const int SchemaVersion = 1;

    private readonly GenealogyStore owner;

    public GenealogyCensusProvider(GenealogyStore owner)
    {
        this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            owner,
            owner.Count,
            owner.Revision);
    }
}
