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
    public const int SchemaVersion = P12CRuntimeIdentitySectionContract.SchemaVersion;
    public const string NpcsSectionId = P12CRuntimeIdentitySectionContract.NpcsSectionId;
    public const string CitiesSectionId = P12CRuntimeIdentitySectionContract.CitiesSectionId;
    public const string LocationsSectionId = P12CRuntimeIdentitySectionContract.LocationsSectionId;
    public const string RoutesSectionId = P12CRuntimeIdentitySectionContract.RoutesSectionId;
    public const string ExplorableSitesSectionId = P12CRuntimeIdentitySectionContract.ExplorableSitesSectionId;
    public const string LocalPlacesSectionId = P12CRuntimeIdentitySectionContract.LocalPlacesSectionId;
    public const string LocalConnectionsSectionId = P12CRuntimeIdentitySectionContract.LocalConnectionsSectionId;
    public const string NotableItemsSectionId = P12CRuntimeIdentitySectionContract.NotableItemsSectionId;

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
        return owner.CoreIdentityIndex.GetOwnerSectionCensusWitness(ToIdentityKind(index));
    }

    private static DailyV1RuntimeIdentityKind ToIdentityKind(RuntimeIdentityCensusIndex index)
    {
        switch (index)
        {
            case RuntimeIdentityCensusIndex.Npcs: return DailyV1RuntimeIdentityKind.Npc;
            case RuntimeIdentityCensusIndex.Cities: return DailyV1RuntimeIdentityKind.City;
            case RuntimeIdentityCensusIndex.Locations: return DailyV1RuntimeIdentityKind.Location;
            case RuntimeIdentityCensusIndex.Routes: return DailyV1RuntimeIdentityKind.Route;
            case RuntimeIdentityCensusIndex.ExplorableSites: return DailyV1RuntimeIdentityKind.ExplorableSite;
            case RuntimeIdentityCensusIndex.LocalPlaces: return DailyV1RuntimeIdentityKind.LocalPlace;
            case RuntimeIdentityCensusIndex.LocalConnections: return DailyV1RuntimeIdentityKind.LocalConnection;
            case RuntimeIdentityCensusIndex.NotableItems: return DailyV1RuntimeIdentityKind.NotableItem;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
