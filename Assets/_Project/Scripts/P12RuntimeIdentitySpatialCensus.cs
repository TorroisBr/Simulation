using System;
using System.Collections.Generic;

public sealed partial class SimulationRuntime
{
    private static readonly string[] P12RuntimeIdentitySectionIds =
    {
        RuntimeIdentityRegistryCensusProvider.NpcsSectionId,
        RuntimeIdentityRegistryCensusProvider.CitiesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocationsSectionId,
        RuntimeIdentityRegistryCensusProvider.RoutesSectionId,
        RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId,
        RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId
    };

    private static readonly string[] P12RuntimeIdentitySpatialSectionIds =
    {
        RuntimeIdentityRegistryCensusProvider.NpcsSectionId,
        RuntimeIdentityRegistryCensusProvider.CitiesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocationsSectionId,
        RuntimeIdentityRegistryCensusProvider.RoutesSectionId,
        RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId,
        RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId,
        SpatialNetworkCensusProvider.LocationsSectionId,
        SpatialNetworkCensusProvider.RoutesSectionId,
        ExplorableSiteCensusProvider.SectionId
    };

    private readonly RuntimeIdentityRegistry p12RuntimeIdentityRegistry;
    private readonly SpatialNetworkRuntime p12SpatialNetworkRuntime;
    private readonly bool requireP12RuntimeIdentitySpatialCensusOwners;
    private IReadOnlyList<IOwnerSectionCensusProvider> p12RuntimeIdentityCensusProviders;
    private IReadOnlyList<IOwnerSectionCensusProvider> p12SpatialNetworkCensusProviders;
    private ExplorableSiteCensusProvider p12ExplorableSiteCensusProvider;

    private bool TryRegisterP12RuntimeIdentitySpatialCensusOwners(ContinuationCensusProtocol protocol)
    {
        if (!requireP12RuntimeIdentitySpatialCensusOwners)
            return true;
        if (protocol == null
            || p12RuntimeIdentityRegistry == null
            || p12SpatialNetworkRuntime == null
            || explorableSiteStore == null
            || !p12SpatialNetworkRuntime.UsesIdentityRegistry(p12RuntimeIdentityRegistry))
            return false;

        IReadOnlyList<IOwnerSectionCensusProvider> identityProviders;
        IReadOnlyList<IOwnerSectionCensusProvider> spatialProviders;
        ExplorableSiteCensusProvider siteProvider;
        try
        {
            identityProviders = RuntimeIdentityRegistryCensusProvider.CreateProviders(p12RuntimeIdentityRegistry);
            spatialProviders = SpatialNetworkCensusProvider.CreateProviders(p12SpatialNetworkRuntime);
            siteProvider = new ExplorableSiteCensusProvider(explorableSiteStore);
        }
        catch
        {
            return false;
        }

        OwnerSectionRole[] identityRoles =
        {
            OwnerSectionRole.Required,
            OwnerSectionRole.Required,
            OwnerSectionRole.Required,
            OwnerSectionRole.Required,
            OwnerSectionRole.ExplicitlyEmpty,
            OwnerSectionRole.ExplicitlyEmpty,
            OwnerSectionRole.ExplicitlyEmpty,
            OwnerSectionRole.ExplicitlyEmpty
        };
        if (identityProviders.Count != P12RuntimeIdentitySectionIds.Length
            || spatialProviders.Count != 2)
            return false;

        for (int i = 0; i < identityProviders.Count; i++)
        {
            if (!TryRegisterP12FixedOwnerSection(
                    protocol,
                    identityProviders[i],
                    P12RuntimeIdentitySectionIds[i],
                    RuntimeIdentityRegistryCensusProvider.SchemaVersion,
                    identityRoles[i],
                    p12RuntimeIdentityRegistry))
                return false;
        }

        if (!TryRegisterP12FixedOwnerSection(
                protocol,
                spatialProviders[0],
                SpatialNetworkCensusProvider.LocationsSectionId,
                SpatialNetworkCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                p12SpatialNetworkRuntime)
            || !TryRegisterP12FixedOwnerSection(
                protocol,
                spatialProviders[1],
                SpatialNetworkCensusProvider.RoutesSectionId,
                SpatialNetworkCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                p12SpatialNetworkRuntime)
            || !TryRegisterP12FixedOwnerSection(
                protocol,
                siteProvider,
                ExplorableSiteCensusProvider.SectionId,
                ExplorableSiteCensusProvider.SchemaVersion,
                OwnerSectionRole.ExplicitlyEmpty,
                explorableSiteStore))
            return false;

        p12RuntimeIdentityCensusProviders = identityProviders;
        p12SpatialNetworkCensusProviders = spatialProviders;
        p12ExplorableSiteCensusProvider = siteProvider;
        return true;
    }

    private static bool TryRegisterP12FixedOwnerSection(
        ContinuationCensusProtocol protocol,
        IOwnerSectionCensusProvider provider,
        string sectionId,
        int schemaVersion,
        OwnerSectionRole role,
        object expectedOwner,
        bool requireZeroRevision = false)
    {
        if (protocol == null || provider == null || expectedOwner == null)
            return false;

        OwnerSectionCensusWitness witness;
        try
        {
            witness = provider.GetCurrentCensus();
        }
        catch
        {
            return false;
        }

        if (witness == null
            || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
            || witness.SchemaVersion != schemaVersion
            || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwner)
            || witness.Cardinality < 0
            || witness.Revision < 0L
            || (requireZeroRevision && witness.Revision != 0L)
            || ((role == OwnerSectionRole.ExplicitlyEmpty || role == OwnerSectionRole.Excluded)
                && witness.Cardinality != 0))
            return false;

        OwnerSectionContract contract = new OwnerSectionContract(sectionId, schemaVersion, role);
        return protocol.RegisterExpectedSection(contract, out _)
            && protocol.RegisterCensusProvider(sectionId, provider, out _);
    }

    private bool TryRegisterP12P8DExactZeroOwnerSections(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || spatialRouteKnowledgeStore == null
            || personRoutePlanStore == null)
            return false;

        try
        {
            IReadOnlyList<PersonRoutePlan> planHistory = personRoutePlanStore.History;
            if (planHistory == null
                || personRoutePlanStore.PlanCount != 0
                || planHistory.Count != 0)
                return false;

            SpatialRouteObservationCensusProvider routeObservationsProvider =
                new SpatialRouteObservationCensusProvider(spatialRouteKnowledgeStore);
            PersonRoutePlanHistoryCensusProvider routePlanHistoryProvider =
                new PersonRoutePlanHistoryCensusProvider(personRoutePlanStore);

            return TryRegisterP12FixedOwnerSection(
                    protocol,
                    routeObservationsProvider,
                    SpatialRouteObservationCensusProvider.SectionId,
                    SpatialRouteObservationCensusProvider.SchemaVersion,
                    OwnerSectionRole.ExplicitlyEmpty,
                    spatialRouteKnowledgeStore,
                    requireZeroRevision: true)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    routePlanHistoryProvider,
                    PersonRoutePlanHistoryCensusProvider.SectionId,
                    PersonRoutePlanHistoryCensusProvider.SchemaVersion,
                    OwnerSectionRole.ExplicitlyEmpty,
                    personRoutePlanStore,
                    requireZeroRevision: true);
        }
        catch
        {
            return false;
        }
    }

    private bool TryRegisterP12EMilitaryOwnerSections(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || armedForceStore == null
            || contingentManpowerStateStore == null
            || armedForceSpatialStateStore == null
            || conflictStore == null
            || warStore == null
            || battleStore == null)
            return false;

        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> armedForceProviders =
                ArmedForceStoreCensusProvider.CreateProviders(armedForceStore);
            if (armedForceProviders == null || armedForceProviders.Count != 3)
                return false;

            return TryRegisterP12FixedOwnerSection(
                    protocol,
                    armedForceProviders[0],
                    ArmedForceStoreCensusProvider.ForcesSectionId,
                    ArmedForceStoreCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    armedForceStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    armedForceProviders[1],
                    ArmedForceStoreCensusProvider.ContingentsSectionId,
                    ArmedForceStoreCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    armedForceStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    armedForceProviders[2],
                    ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId,
                    ArmedForceStoreCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    armedForceStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    new ContingentManpowerCensusProvider(contingentManpowerStateStore),
                    ContingentManpowerCensusProvider.SectionId,
                    ContingentManpowerCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    contingentManpowerStateStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    new ArmedForceSpatialCensusProvider(armedForceSpatialStateStore),
                    ArmedForceSpatialCensusProvider.SectionId,
                    ArmedForceSpatialCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    armedForceSpatialStateStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    new PersistentConflictCensusProvider(conflictStore),
                    PersistentConflictCensusProvider.SectionId,
                    PersistentConflictCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    conflictStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    new PersistentWarCensusProvider(warStore),
                    PersistentWarCensusProvider.SectionId,
                    PersistentWarCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    warStore)
                && TryRegisterP12FixedOwnerSection(
                    protocol,
                    new PersistentBattleCensusProvider(battleStore),
                    PersistentBattleCensusProvider.SectionId,
                    PersistentBattleCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required,
                    battleStore);
        }
        catch
        {
            return false;
        }
    }

    private bool TryValidateP12RuntimeIdentitySpatialOwnerBaselines()
    {
        if (!requireP12RuntimeIdentitySpatialCensusOwners)
            return true;
        return npcRosterCensusProtocol != null
            && npcRosterCensusProtocol.TryValidateUnchangedSections(
                P12RuntimeIdentitySpatialSectionIds,
                out _);
    }

    internal bool HasSameP12RuntimeIdentitySpatialOwners(
        RuntimeIdentityRegistry identityRegistry,
        SpatialNetworkRuntime spatialNetwork,
        ExplorableSiteStore siteStore)
    {
        if (runtimeAdmissionContext == null)
            return true;
        return requireP12RuntimeIdentitySpatialCensusOwners
            && ReferenceEquals(identityRegistry, p12RuntimeIdentityRegistry)
            && ReferenceEquals(spatialNetwork, p12SpatialNetworkRuntime)
            && ReferenceEquals(siteStore, explorableSiteStore)
            && p12RuntimeIdentityCensusProviders != null
            && p12RuntimeIdentityCensusProviders.Count == P12RuntimeIdentitySectionIds.Length
            && p12SpatialNetworkCensusProviders != null
            && p12SpatialNetworkCensusProviders.Count == 2
            && p12ExplorableSiteCensusProvider != null;
    }
}
