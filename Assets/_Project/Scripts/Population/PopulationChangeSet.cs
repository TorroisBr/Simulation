using System;

/// <summary>
/// Explicit aggregate population components for one atomic state transition.
/// Negative values are retained so the proposal boundary can reject them structurally.
/// </summary>
public readonly struct PopulationChangeSet : IEquatable<PopulationChangeSet>
{
    public int Births { get; }
    public int Deaths { get; }
    public int Arrivals { get; }
    public int Departures { get; }

    public long NetChange => (long)Births + Arrivals - Deaths - Departures;
    public bool HasNegativeChange => Births < 0 || Deaths < 0 || Arrivals < 0 || Departures < 0;

    public PopulationChangeSet(int births, int deaths, int arrivals, int departures)
    {
        Births = births;
        Deaths = deaths;
        Arrivals = arrivals;
        Departures = departures;
    }

    public bool Equals(PopulationChangeSet other)
    {
        return Births == other.Births
            && Deaths == other.Deaths
            && Arrivals == other.Arrivals
            && Departures == other.Departures;
    }

    public override bool Equals(object obj)
    {
        return obj is PopulationChangeSet other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Births;
            hash = (hash * 397) ^ Deaths;
            hash = (hash * 397) ^ Arrivals;
            hash = (hash * 397) ^ Departures;
            return hash;
        }
    }

    public static bool operator ==(PopulationChangeSet left, PopulationChangeSet right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PopulationChangeSet left, PopulationChangeSet right)
    {
        return !left.Equals(right);
    }
}
