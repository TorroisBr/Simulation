using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Immutable value object describing whether a simulation domain exists and how it may begin behavior.
/// </summary>
public sealed class SimulationDomainPolicy : IEquatable<SimulationDomainPolicy>
{
    public SimulationFeatureAvailability Availability { get; }
    public SimulationAutonomyMode Autonomy { get; }

    public bool IsAvailable => Availability == SimulationFeatureAvailability.Available;
    public bool AllowsAutonomy => IsAvailable && Autonomy == SimulationAutonomyMode.Autonomous;
    public bool IsValid => GetValidationErrors().Count == 0;

    public SimulationDomainPolicy(
        SimulationFeatureAvailability availability,
        SimulationAutonomyMode autonomy)
    {
        Availability = availability;
        Autonomy = autonomy;
    }

    /// <summary>
    /// Returns deterministic diagnostics without changing this policy.
    /// </summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        List<string> errors = new List<string>();

        if (IsKnownAvailability(Availability) == false)
        {
            errors.Add("Availability has an unknown value.");
        }

        if (IsKnownAutonomy(Autonomy) == false)
        {
            errors.Add("Autonomy has an unknown value.");
        }

        if (Availability == SimulationFeatureAvailability.Unavailable
            && Autonomy == SimulationAutonomyMode.Autonomous)
        {
            errors.Add("An unavailable domain cannot be autonomous.");
        }

        return new ReadOnlyCollection<string>(errors);
    }

    public bool Equals(SimulationDomainPolicy other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return Availability == other.Availability && Autonomy == other.Autonomy;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SimulationDomainPolicy);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Availability * 397) ^ (int)Autonomy;
        }
    }

    public static bool operator ==(SimulationDomainPolicy left, SimulationDomainPolicy right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(SimulationDomainPolicy left, SimulationDomainPolicy right)
    {
        return (left == right) == false;
    }

    private static bool IsKnownAvailability(SimulationFeatureAvailability availability)
    {
        return availability == SimulationFeatureAvailability.Unavailable
            || availability == SimulationFeatureAvailability.Available;
    }

    private static bool IsKnownAutonomy(SimulationAutonomyMode autonomy)
    {
        return autonomy == SimulationAutonomyMode.ManualOnly
            || autonomy == SimulationAutonomyMode.Autonomous;
    }
}
