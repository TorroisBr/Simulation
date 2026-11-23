using System;
using System.Collections.Generic;

internal sealed class DailyV1CoreSelectedOwnerCensusProviderComposition
{
    private DailyV1CoreSelectedOwnerCensusProviderComposition(
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personStore,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> cities,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> identitySpatial,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> selectedNpcs,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> all)
    {
        OwnerGraph = ownerGraph;
        PersonStoreRegistrations = personStore;
        CityRegistrations = cities;
        IdentitySpatialRegistrations = identitySpatial;
        SelectedNpcRegistrations = selectedNpcs;
        Registrations = all;
    }

    internal DailyV1SelectedRuntimeOwnerGraph OwnerGraph { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> PersonStoreRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> CityRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> IdentitySpatialRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> SelectedNpcRegistrations { get; }

    // This is intentionally the currently Core-composed subset. It does not
    // represent the complete selected-profile/P12 census inventory.
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> Registrations { get; }

    internal static bool TryCreate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1CoreSelectedOwnerCensusProviderComposition composition,
        out string diagnostic)
    {
        composition = null;
        diagnostic = null;
        if (graph == null)
        {
            diagnostic = "The Core census provider composition requires its exact owner graph.";
            return false;
        }

        if (!DailyV1CorePersonStoreCensusComposition.TryCreateProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personStoreSource,
                out diagnostic)
            || !DailyV1SelectedRuntimeOwnerCensusSource
                .TryCreateCoreOwnedCityCensusProviderRegistrations(
                    graph,
                    out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> citySource,
                    out diagnostic)
            || !DailyV1SelectedRuntimeOwnerCensusSource
                .TryCreateCoreOwnedIdentitySpatialCensusProviderRegistrations(
                    graph,
                    out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> identitySpatialSource,
                    out diagnostic)
            || !DailyV1SelectedRuntimeOwnerCensusSource
                .TryCreateCoreOwnedSelectedNpcCensusProviderRegistrations(
                    graph,
                    out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> npcSource,
                    out diagnostic))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core selected-owner provider composition could not be created.";
            return false;
        }

        if (!TryNormalizeGroup(
                personStoreSource,
                IsPersonStoreSection,
                graph.PersonStore,
                "PersonStore",
                expectedCount: 2,
                allowEmpty: false,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personStore,
                out diagnostic)
            || !TryNormalizeGroup(
                citySource,
                IsCitySection,
                expectedOwner: null,
                "City",
                expectedCount: null,
                allowEmpty: false,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> cities,
                out diagnostic)
            || !TryNormalizeGroup(
                identitySpatialSource,
                DailyV1SelectedRuntimeOwnerCensusSource.IsCoreOwnedIdentitySpatialSection,
                expectedOwner: null,
                "identity/spatial",
                expectedCount: null,
                allowEmpty: false,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> identitySpatial,
                out diagnostic)
            || !TryNormalizeGroup(
                npcSource,
                DailyV1SelectedRuntimeOwnerCensusSource.IsCoreOwnedSelectedNpcCensusSectionId,
                expectedOwner: null,
                "selected NPC",
                expectedCount: null,
                allowEmpty: true,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> selectedNpcs,
                out diagnostic))
            return false;

        List<DailyV1OwnerSectionCensusProviderRegistration> all =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(
                personStore.Count + cities.Count + identitySpatial.Count + selectedNpcs.Count);
        HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
        if (!TryAddGroup(personStore, all, allIds, out diagnostic)
            || !TryAddGroup(cities, all, allIds, out diagnostic)
            || !TryAddGroup(identitySpatial, all, allIds, out diagnostic)
            || !TryAddGroup(selectedNpcs, all, allIds, out diagnostic))
            return false;
        all.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        composition = new DailyV1CoreSelectedOwnerCensusProviderComposition(
            graph,
            personStore,
            cities,
            identitySpatial,
            selectedNpcs,
            all.AsReadOnly());
        return true;
    }

    private static bool TryNormalizeGroup(
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> source,
        Predicate<string> sectionPredicate,
        object expectedOwner,
        string groupName,
        int? expectedCount,
        bool allowEmpty,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> normalized,
        out string diagnostic)
    {
        normalized = null;
        diagnostic = null;
        if (source == null || sectionPredicate == null)
        {
            diagnostic = "The Core " + groupName + " census provider group is unavailable.";
            return false;
        }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(source.Count);
        HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in source)
        {
            if (registration == null
                || registration.Provider == null
                || string.IsNullOrWhiteSpace(registration.SectionId)
                || !sectionPredicate(registration.SectionId)
                || !Enum.IsDefined(typeof(OwnerSectionRole), registration.Role)
                || registration.SchemaVersion <= 0
                || !observed.Add(registration.SectionId))
            {
                diagnostic = "The Core " + groupName + " census provider group has an invalid or duplicate section.";
                return false;
            }

            OwnerSectionCensusWitness witness;
            try
            {
                witness = registration.Provider.GetCurrentCensus();
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is InvalidOperationException
                || exception is OverflowException)
            {
                diagnostic = "The Core " + groupName + " provider could not read "
                    + registration.SectionId + ".";
                return false;
            }

            if (witness == null
                || !string.Equals(witness.SectionId, registration.SectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != registration.SchemaVersion
                || witness.OwnerInstanceIdentity == null
                || (expectedOwner != null && !ReferenceEquals(expectedOwner, witness.OwnerInstanceIdentity))
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || (registration.Role == OwnerSectionRole.ExplicitlyEmpty && witness.Cardinality != 0)
                || (registration.HasBaseline
                    && (!ReferenceEquals(registration.BaselineOwnerInstanceIdentity, witness.OwnerInstanceIdentity)
                        || registration.BaselineCardinality != witness.Cardinality
                        || registration.BaselineRevision != witness.Revision)))
            {
                diagnostic = "The Core " + groupName + " provider baseline differs from its live owner at "
                    + registration.SectionId + ".";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                registration.SectionId,
                registration.SchemaVersion,
                registration.Role,
                registration.Provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        if ((expectedCount.HasValue && built.Count != expectedCount.Value)
            || (!allowEmpty && built.Count == 0))
        {
            diagnostic = "The Core " + groupName + " census provider group has incomplete coverage.";
            return false;
        }

        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        normalized = built.AsReadOnly();
        return true;
    }

    private static bool TryAddGroup(
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> group,
        List<DailyV1OwnerSectionCensusProviderRegistration> all,
        HashSet<string> allIds,
        out string diagnostic)
    {
        diagnostic = null;
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in group)
        {
            if (!allIds.Add(registration.SectionId))
            {
                diagnostic = "Core-selected census owner groups overlap at "
                    + registration.SectionId + ".";
                return false;
            }
            all.Add(registration);
        }
        return true;
    }

    private static bool IsPersonStoreSection(string sectionId) =>
        string.Equals(sectionId,
            DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
            StringComparison.Ordinal)
        || string.Equals(sectionId,
            DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
            StringComparison.Ordinal);

    private static bool IsCitySection(string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId)) return false;
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes)
            if (sectionId.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }
}
