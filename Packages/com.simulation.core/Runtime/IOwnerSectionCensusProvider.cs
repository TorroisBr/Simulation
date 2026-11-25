/// <summary>Reads a live witness from one concrete owner instance.</summary>
public interface IOwnerSectionCensusProvider
{
    OwnerSectionCensusWitness GetCurrentCensus();
}
