using System;

public interface IPersonNaturalMortalitySampleProvider
{
    double GetSample(PersonId personId, long currentAbsoluteDay);
}

public enum AggregateDemographicOutcomeKind
{
    Birth = 0,
    Death = 1
}

public interface IAggregateDemographySampleProvider
{
    double GetSample(
        string settlementRuntimeId,
        long currentAbsoluteDay,
        AggregateDemographicOutcomeKind outcomeKind);
}


/// <summary>
/// Stable, registration-order-independent samples for autonomous demographic work.
/// This is deliberately separate from UnityEngine.Random and from action randomness.
/// </summary>
public sealed class DeterministicDemographicSampleProvider :
    IPersonNaturalMortalitySampleProvider,
    IAggregateDemographySampleProvider
{
    public double GetSample(PersonId personId, long currentAbsoluteDay)
    {
        return GetSample(personId?.Value, currentAbsoluteDay, "person");
    }

    public double GetSample(
        string settlementRuntimeId,
        long currentAbsoluteDay,
        AggregateDemographicOutcomeKind outcomeKind)
    {
        return GetSample(
            settlementRuntimeId,
            currentAbsoluteDay,
            outcomeKind == AggregateDemographicOutcomeKind.Birth ? "birth" : "death");
    }

    private static double GetSample(string entityId, long currentAbsoluteDay, string outcome)
    {
        if (string.IsNullOrWhiteSpace(entityId) || currentAbsoluteDay < 0L)
        {
            return 0.5d;
        }

        ulong hash = 14695981039346656037UL;
        Mix(ref hash, outcome);
        Mix(ref hash, entityId);
        unchecked
        {
            hash ^= (ulong)currentAbsoluteDay;
            hash *= 1099511628211UL;
            hash ^= (ulong)currentAbsoluteDay >> 32;
            hash *= 1099511628211UL;
        }

        // Keep the result in [0, 1), exactly representable as a 53-bit fraction.
        return (hash >> 11) * (1d / 9007199254740992d);
    }

    private static void Mix(ref ulong hash, string value)
    {
        foreach (char character in value ?? string.Empty)
        {
            unchecked
            {
                hash ^= character;
                hash *= 1099511628211UL;
            }
        }

        unchecked
        {
            hash ^= 0xffUL;
            hash *= 1099511628211UL;
        }
    }
}
