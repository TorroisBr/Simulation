using System;

/// <summary>
/// Immutable population representation and NPC decision-scope settings.
/// </summary>
public sealed class EffectivePopulationConfiguration : IEquatable<EffectivePopulationConfiguration>
{
    public PopulationRepresentationMode RepresentationMode { get; }
    public NpcDecisionSimulationScope DecisionScope { get; }

    public EffectivePopulationConfiguration(
        PopulationRepresentationMode representationMode,
        NpcDecisionSimulationScope decisionScope)
    {
        RepresentationMode = representationMode;
        DecisionScope = decisionScope;
    }

    public bool IsValid => IsKnownRepresentationMode(RepresentationMode)
        && IsKnownDecisionScope(DecisionScope);

    public bool Equals(EffectivePopulationConfiguration other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return RepresentationMode == other.RepresentationMode
            && DecisionScope == other.DecisionScope;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EffectivePopulationConfiguration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)RepresentationMode * 397) ^ (int)DecisionScope;
        }
    }

    private static bool IsKnownRepresentationMode(PopulationRepresentationMode mode)
    {
        return mode == PopulationRepresentationMode.Aggregate
            || mode == PopulationRepresentationMode.Hybrid
            || mode == PopulationRepresentationMode.FullyIndividualized;
    }

    private static bool IsKnownDecisionScope(NpcDecisionSimulationScope scope)
    {
        return scope == NpcDecisionSimulationScope.RelevantOnly
            || scope == NpcDecisionSimulationScope.AllMaterialized;
    }
}
