/// <summary>
/// Opaque, owner-bound population witness captured for trusted rollback.
/// It carries no receipt snapshot or authority to choose arbitrary target values.
/// </summary>
public sealed class PopulationRollbackPoint
{
    internal SettlementPopulationRuntime Owner { get; }
    public int Population { get; }
    public long Revision { get; }

    internal PopulationRollbackPoint(
        SettlementPopulationRuntime owner,
        int population,
        long revision)
    {
        Owner = owner;
        Population = population;
        Revision = revision;
    }
}
