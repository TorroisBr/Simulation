using System;
using System.Collections.Generic;

/// <summary>
/// Fixed passive witnesses for the installed legacy spatial-network owners.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public static class SpatialNetworkCensusProvider
{
    public const string LocationsSectionId = "p12d.legacy-spatial-network.locations";
    public const string RoutesSectionId = "p12d.legacy-spatial-network.routes";
    public const int SchemaVersion = 1;

    private enum Section
    {
        Locations,
        Routes
    }

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly SpatialNetworkRuntime owner;
        private readonly Section section;
        private readonly string sectionId;

        public SectionProvider(SpatialNetworkRuntime owner, Section section, string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality = section == Section.Locations ? owner.LocationCount : owner.RouteCount;
            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, cardinality, owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(SpatialNetworkRuntime owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        IOwnerSectionCensusProvider[] providers =
        {
            new SectionProvider(owner, Section.Locations, LocationsSectionId),
            new SectionProvider(owner, Section.Routes, RoutesSectionId)
        };
        return Array.AsReadOnly(providers);
    }
}
