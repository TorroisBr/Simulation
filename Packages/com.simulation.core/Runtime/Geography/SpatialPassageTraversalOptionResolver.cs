using System.Collections.Generic;

/// <summary>Resolves P8-C transit options against the existing passage authority.</summary>
internal sealed class SpatialPassageTraversalOptionResolver : ISpatialTraversalOptionResolver
{
    private readonly SpatialPassageAuthority passageAuthority;

    public SpatialPassageTraversalOptionResolver(SpatialPassageAuthority passageAuthority)
    {
        this.passageAuthority = passageAuthority
            ?? throw new System.ArgumentNullException(nameof(passageAuthority));
    }

    public bool TryResolveTraversalOption(
        TraversalOptionRef option,
        HexBoundaryKey boundary,
        out string failure)
    {
        if (option == null || boundary == null)
        {
            failure = "Traversal option and boundary are required.";
            return false;
        }

        if (!passageAuthority.TryGetTraversalOptions(
                boundary,
                out IReadOnlyList<TraversalOptionRef> options,
                out SpatialAuthorityFailure authorityFailure))
        {
            failure = authorityFailure?.Message ?? "Traversal boundary is not registered.";
            return false;
        }

        foreach (TraversalOptionRef candidate in options)
        {
            if (option.Equals(candidate))
            {
                failure = string.Empty;
                return true;
            }
        }

        failure = "Traversal option is not registered on the supplied boundary.";
        return false;
    }
}
