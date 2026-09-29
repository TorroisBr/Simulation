using System;
using System.Collections.Generic;

internal enum RuntimeIdentityCensusIndex
{
    Npcs,
    Cities,
    Locations,
    Routes,
    ExplorableSites,
    LocalPlaces,
    LocalConnections,
    NotableItems
}

/// <summary>
/// Fixed passive census providers for the existing RuntimeIdentityRegistry
/// indexes. This is owner evidence, not a registration or extension surface.
/// </summary>
public sealed class RuntimeIdentityRegistryCensusProvider : IOwnerSectionCensusProvider
{
    public const int SchemaVersion = 1;
    public const string NpcsSectionId = "p12c.runtime-identities.npcs";
    public const string CitiesSectionId = "p12c.runtime-identities.cities";
    public const string LocationsSectionId = "p12c.runtime-identities.locations";
    public const string RoutesSectionId = "p12c.runtime-identities.routes";
    public const string ExplorableSitesSectionId = "p12c.runtime-identities.explorable-sites";
    public const string LocalPlacesSectionId = "p12c.runtime-identities.local-places";
    public const string LocalConnectionsSectionId = "p12c.runtime-identities.local-connections";
    public const string NotableItemsSectionId = "p12c.runtime-identities.notable-items";

    private readonly RuntimeIdentityRegistry owner;
    private readonly RuntimeIdentityCensusIndex index;
    private readonly string sectionId;

    private RuntimeIdentityRegistryCensusProvider(
        RuntimeIdentityRegistry owner,
        RuntimeIdentityCensusIndex index,
        string sectionId)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.index = index;
        this.sectionId = sectionId;
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(RuntimeIdentityRegistry owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.Npcs, NpcsSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.Cities, CitiesSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.Locations, LocationsSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.Routes, RoutesSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.ExplorableSites, ExplorableSitesSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.LocalPlaces, LocalPlacesSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.LocalConnections, LocalConnectionsSectionId),
            new RuntimeIdentityRegistryCensusProvider(owner, RuntimeIdentityCensusIndex.NotableItems, NotableItemsSectionId)
        };
        return Array.AsReadOnly(providers);
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            sectionId,
            SchemaVersion,
            owner,
            owner.GetCensusCardinality(index),
            owner.CensusRevision);
    }
}
