using System;
using System.Collections.Generic;

internal enum RuntimeIdAllocatorCensusCounter
{
    Npcs,
    Cities,
    Locations,
    Routes,
    Events,
    Directives,
    Decisions,
    TravelParties,
    Organizations,
    ExplorableSites,
    Expeditions,
    LocalPlaces,
    LocalConnections,
    NotableItems
}

/// <summary>Fixed passive census providers for the selected runtime ID allocator.</summary>
public static class RuntimeIdAllocatorCensusProvider
{
    public const int SchemaVersion = 1;
    public const string NpcsSectionId = "p12c.runtime-id-allocator.npcs";
    public const string CitiesSectionId = "p12c.runtime-id-allocator.cities";
    public const string LocationsSectionId = "p12c.runtime-id-allocator.locations";
    public const string RoutesSectionId = "p12c.runtime-id-allocator.routes";
    public const string EventsSectionId = "p12c.runtime-id-allocator.events";
    public const string DirectivesSectionId = "p12c.runtime-id-allocator.directives";
    public const string DecisionsSectionId = "p12c.runtime-id-allocator.decisions";
    public const string TravelPartiesSectionId = "p12c.runtime-id-allocator.travel-parties";
    public const string OrganizationsSectionId = "p12c.runtime-id-allocator.organizations";
    public const string ExplorableSitesSectionId = "p12c.runtime-id-allocator.explorable-sites";
    public const string ExpeditionsSectionId = "p12c.runtime-id-allocator.expeditions";
    public const string LocalPlacesSectionId = "p12c.runtime-id-allocator.local-places";
    public const string LocalConnectionsSectionId = "p12c.runtime-id-allocator.local-connections";
    public const string NotableItemsSectionId = "p12c.runtime-id-allocator.notable-items";

    private sealed class CounterProvider : IOwnerSectionCensusProvider
    {
        private readonly RuntimeIdAllocator owner;
        private readonly RuntimeIdAllocatorCensusCounter counter;
        private readonly string sectionId;

        public CounterProvider(RuntimeIdAllocator owner, RuntimeIdAllocatorCensusCounter counter, string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.counter = counter;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner.CensusOwnerIdentity,
                1,
                owner.GetCensusRevision(counter));
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(RuntimeIdAllocator owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Npcs, NpcsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Cities, CitiesSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Locations, LocationsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Routes, RoutesSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Events, EventsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Directives, DirectivesSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Decisions, DecisionsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.TravelParties, TravelPartiesSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Organizations, OrganizationsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.ExplorableSites, ExplorableSitesSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Expeditions, ExpeditionsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.LocalPlaces, LocalPlacesSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.LocalConnections, LocalConnectionsSectionId),
            new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.NotableItems, NotableItemsSectionId)
        };
        return Array.AsReadOnly(providers);
    }

    public static IOwnerSectionCensusProvider CreateEventCounterProvider(RuntimeIdAllocator owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        return new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Events, EventsSectionId);
    }

    public static IOwnerSectionCensusProvider CreateDecisionCounterProvider(RuntimeIdAllocator owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        return new CounterProvider(owner, RuntimeIdAllocatorCensusCounter.Decisions, DecisionsSectionId);
    }
}
