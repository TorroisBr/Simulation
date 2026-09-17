using System;

/// <summary>
/// Explicit aggregate population components for one atomic state transition.
/// Negative values are retained so the proposal boundary can reject them structurally.
/// </summary>
public readonly struct PopulationChangeSet : IEquatable<PopulationChangeSet>
{
    public int Births { get; }
    public int Deaths { get; }
    public int Immigrations { get; }
    public int Emigrations { get; }

    public long NetChange => (long)Births + Immigrations - Deaths - Emigrations;
    public bool HasNegativeChange => Births < 0 || Deaths < 0 || Immigrations < 0 || Emigrations < 0;

    public PopulationChangeSet(int births, int deaths, int immigrations, int emigrations)
    {
        Births = births;
        Deaths = deaths;
        Immigrations = immigrations;
        Emigrations = emigrations;
    }

    public bool Equals(PopulationChangeSet other)
    {
        return Births == other.Births
            && Deaths == other.Deaths
            && Immigrations == other.Immigrations
            && Emigrations == other.Emigrations;
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
            hash = (hash * 397) ^ Immigrations;
            hash = (hash * 397) ^ Emigrations;
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
