using System;
using System.Collections.Generic;

internal sealed class DailyV1CoreSelectedOwnerCensusProviderComposition
{
    private DailyV1CoreSelectedOwnerCensusProviderComposition(
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personStore,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personLifeResidence,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> cities,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> identitySpatial,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> selectedNpcs,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> all)
    {
        OwnerGraph = ownerGraph;
        PersonStoreRegistrations = personStore;
        PersonLifeResidenceRegistrations = personLifeResidence;
        CityRegistrations = cities;
        IdentitySpatialRegistrations = identitySpatial;
        SelectedNpcRegistrations = selectedNpcs;
        Registrations = all;
    }

    internal DailyV1SelectedRuntimeOwnerGraph OwnerGraph { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> PersonStoreRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> PersonLifeResidenceRegistrations { get; }
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
            || !DailyV1CorePersonStoreCensusComposition.TryCreateLifeResidenceProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personLifeResidenceSource,
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
                personLifeResidenceSource,
                IsPersonLifeResidenceSection,
                expectedOwner: null,
                "Person life/residence",
                expectedCount: graph.PersonStore.Persons.Count,
                allowEmpty: true,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personLifeResidence,
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
                personStore.Count + personLifeResidence.Count + cities.Count
                + identitySpatial.Count + selectedNpcs.Count);
        HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
        if (!TryAddGroup(personStore, all, allIds, out diagnostic)
            || !TryAddGroup(personLifeResidence, all, allIds, out diagnostic)
            || !TryAddGroup(cities, all, allIds, out diagnostic)
            || !TryAddGroup(identitySpatial, all, allIds, out diagnostic)
            || !TryAddGroup(selectedNpcs, all, allIds, out diagnostic))
            return false;
        all.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        composition = new DailyV1CoreSelectedOwnerCensusProviderComposition(
            graph,
            personStore,
            personLifeResidence,
            cities,
            identitySpatial,
            selectedNpcs,
            all.AsReadOnly());
        return true;
    }

    /// <summary>
    /// Installs and seals the existing Core-owned selected-profile census for
    /// a detached application start. This is intentionally fail-closed: the
    /// Core composition must cover the exact live graph's complete P12 section
    /// inventory before anything is registered in the execution boundary.
    /// Host-projected rows are never synthesized here.
    /// </summary>
    internal static bool TryInstallCompleteProfileForApplicationStart(
        DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        diagnostic = null;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        DailyV1ExecutionBoundaryState executionState = session?.ExecutionBoundaryState;
        if (session == null || graph == null || executionState == null
            || !ReferenceEquals(graph.Definitions, session.Definitions)
            || !graph.MatchesExactOwnerInstances(graph.Cities, graph.Npcs))
        {
            diagnostic = "The detached Core start requires the exact admitted session owner graph.";
            return false;
        }

        if (executionState.HasSealedSelectedOwnerSectionCensusRegistry)
        {
            if (executionState.IsSelectedRuntimeCensusProtocolReadyForPublication)
                return true;
            diagnostic = "The detached Core start found a sealed but unpublished selected-owner census protocol.";
            return false;
        }

        if (!TryCreate(graph,
                out DailyV1CoreSelectedOwnerCensusProviderComposition composition,
                out diagnostic)
            || composition == null)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The detached Core start could not compose live selected-owner census providers.";
            return false;
        }

        if (!TryCreateExpectedSectionIds(graph, out HashSet<string> expectedIds, out diagnostic))
            return false;

        HashSet<string> actualIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in composition.Registrations)
        {
            if (registration == null || string.IsNullOrWhiteSpace(registration.SectionId)
                || !actualIds.Add(registration.SectionId))
            {
                diagnostic = "The detached Core start found an invalid or duplicate Core census provider registration.";
                return false;
            }
        }

        List<string> missingIds = new List<string>();
        foreach (string expectedId in expectedIds)
            if (!actualIds.Contains(expectedId)) missingIds.Add(expectedId);
        List<string> unexpectedIds = new List<string>();
        foreach (string actualId in actualIds)
            if (!expectedIds.Contains(actualId)) unexpectedIds.Add(actualId);
        missingIds.Sort(StringComparer.Ordinal);
        unexpectedIds.Sort(StringComparer.Ordinal);
        if (missingIds.Count != 0 || unexpectedIds.Count != 0)
        {
            diagnostic = "The detached Core start cannot establish the complete 89-family selected Daily-v1 census. "
                + "Missing provider sections: [" + string.Join(", ", missingIds) + "]; "
                + "unexpected provider sections: [" + string.Join(", ", unexpectedIds) + "]. "
                + "No partial census was registered.";
            return false;
        }

        List<DailyV1OwnerSectionContractIdentity> contracts =
            new List<DailyV1OwnerSectionContractIdentity>(composition.Registrations.Count);
        List<OwnerSectionCensusSnapshot> snapshots =
            new List<OwnerSectionCensusSnapshot>(composition.Registrations.Count);
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in composition.Registrations)
        {
            if (!TryGetExpectedRole(registration.SectionId, out OwnerSectionRole expectedRole)
                || registration.Role != expectedRole
                || registration.SchemaVersion <= 0
                || registration.Provider == null)
            {
                diagnostic = "The detached Core start found a provider with an invalid selected-profile contract at "
                    + registration.SectionId + ".";
                return false;
            }

            OwnerSectionCensusWitness witness;
            try { witness = registration.Provider.GetCurrentCensus(); }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "The detached Core start could not read the live provider at "
                    + registration.SectionId + ".";
                return false;
            }

            if (witness == null
                || !string.Equals(witness.SectionId, registration.SectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != registration.SchemaVersion
                || witness.OwnerInstanceIdentity == null
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || (expectedRole == OwnerSectionRole.ExplicitlyEmpty && witness.Cardinality != 0)
                || (registration.HasBaseline
                    && (!ReferenceEquals(registration.BaselineOwnerInstanceIdentity,
                            witness.OwnerInstanceIdentity)
                        || registration.BaselineCardinality != witness.Cardinality
                        || registration.BaselineRevision != witness.Revision)))
            {
                diagnostic = "The detached Core start found a stale or invalid live provider witness at "
                    + registration.SectionId + ".";
                return false;
            }

            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                registration.SectionId, registration.SchemaVersion, expectedRole));
            snapshots.Add(new OwnerSectionCensusSnapshot(
                registration.SectionId, registration.SchemaVersion, expectedRole,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision));
        }

        if (!DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(snapshots.AsReadOnly()))
        {
            diagnostic = "The detached Core start's live provider witnesses do not satisfy the exact 89-family "
                + "selected Daily-v1 census shape, cardinality, or alias-identity contract.";
            return false;
        }

        if (!executionState.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
                contracts, composition.Registrations)
            || !executionState.TrySealExpectedOwnerSectionContractInventory())
        {
            diagnostic = "The detached Core start could not bind the complete census to its exact owner thread and execution boundary.";
            return false;
        }

        if (!executionState.TrySealSelectedOwnerSectionCensusRegistryFromLiveProviders(
                composition.Registrations,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> sealedProviders)
            || sealedProviders == null
            || !executionState.IsSelectedRuntimeCensusProtocolReadyForPublication)
        {
            diagnostic = "The detached Core start could not establish a stable complete census baseline on the admitted owner thread.";
            return false;
        }

        return true;
    }

    internal static bool TryCreateExpectedSectionIds(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out HashSet<string> sectionIds,
        out string diagnostic)
    {
        sectionIds = new HashSet<string>(StringComparer.Ordinal);
        diagnostic = null;
        if (graph?.PersonStore == null || graph.Cities == null || graph.Npcs == null
            || graph.Cities.Count == 0 || graph.Npcs.Count == 0)
        {
            diagnostic = "The detached Core start requires the admitted Daily-v1 City, NPC, and PersonStore owner graph.";
            return false;
        }

        foreach (string sectionId in DailyV1SelectedOwnerSectionCensusContract.FixedRequired)
            if (!TryAddExpectedId(sectionIds, sectionId, out diagnostic)) return false;
        foreach (string sectionId in DailyV1SelectedOwnerSectionCensusContract.FixedEmpty)
            if (!TryAddExpectedId(sectionIds, sectionId, out diagnostic)) return false;

        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<PersonRuntime> personOwners = new HashSet<PersonRuntime>();
        foreach (PersonRuntime person in graph.PersonStore.Persons)
        {
            if (person == null || person.PersonId == null
                || string.IsNullOrWhiteSpace(person.PersonId.Value)
                || !personIds.Add(person.PersonId.Value)
                || !personOwners.Add(person))
            {
                diagnostic = "The detached Core start found a missing or duplicate PersonStore life/residence owner.";
                return false;
            }

            string personResidenceId;
            try
            {
                personResidenceId = DailyV1SelectedOwnerSectionCensusContract
                    .NpcLifeResidenceSectionIdFor(person.PersonId);
            }
            catch (ArgumentException)
            {
                diagnostic = "The detached Core start found a PersonId that cannot be represented by the P12 section contract.";
                return false;
            }

            if (!TryAddExpectedId(sectionIds, personResidenceId, out diagnostic)) return false;
        }

        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId))
            {
                diagnostic = "The detached Core start found an NPC without its admitted RuntimeId.";
                return false;
            }
            foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
                if (!TryAddExpectedId(sectionIds, prefix + owner.RuntimeId, out diagnostic)) return false;

            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(owner.RuntimeId, out PersonRuntime person))
            {
                if (person == null || person.PersonId == null
                    || !personIds.Contains(person.PersonId.Value)
                    || !graph.PersonStore.TryGet(person.PersonId, out PersonRuntime indexedPerson)
                    || !ReferenceEquals(indexedPerson, person))
                {
                    diagnostic = "The detached Core start found an NPC-to-Person binding outside the exact PersonStore owner set.";
                    return false;
                }
            }
            else
            {
                if (!TryAddExpectedId(sectionIds,
                        DailyV1SelectedOwnerSectionCensusContract.NpcResidenceSectionIdFor(owner.RuntimeId),
                        out diagnostic)) return false;
            }
        }

        foreach (DailyV1CityRuntimeOwners owner in graph.Cities)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId))
            {
                diagnostic = "The detached Core start found a City without its admitted RuntimeId.";
                return false;
            }
            string encodedCityKey;
            try
            {
                encodedCityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(owner.RuntimeId);
            }
            catch (ArgumentException)
            {
                diagnostic = "The detached Core start found a City RuntimeId that cannot be represented by the P12 section contract.";
                return false;
            }
            if (!TryAddExpectedId(sectionIds,
                    DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(owner.RuntimeId),
                    out diagnostic)
                || !TryAddExpectedId(sectionIds,
                    DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + encodedCityKey,
                    out diagnostic)
                || !TryAddExpectedId(sectionIds,
                    DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + encodedCityKey,
                    out diagnostic)
                || !TryAddExpectedId(sectionIds,
                    DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + encodedCityKey,
                    out diagnostic))
                return false;
        }

        return true;
    }

    private static bool TryAddExpectedId(HashSet<string> ids, string sectionId, out string diagnostic)
    {
        diagnostic = null;
        if (string.IsNullOrWhiteSpace(sectionId) || !ids.Add(sectionId))
        {
            diagnostic = "The detached Core start found an invalid or duplicate section identity in the selected-profile census contract.";
            return false;
        }
        return true;
    }

    private static bool TryGetExpectedRole(string sectionId, out OwnerSectionRole role)
    {
        if (ContainsOrdinal(DailyV1SelectedOwnerSectionCensusContract.FixedEmpty, sectionId))
        {
            role = OwnerSectionRole.ExplicitlyEmpty;
            return true;
        }
        if (ContainsOrdinal(DailyV1SelectedOwnerSectionCensusContract.FixedRequired, sectionId))
        {
            role = OwnerSectionRole.Required;
            return true;
        }
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
            if (sectionId.StartsWith(prefix, StringComparison.Ordinal))
            {
                role = OwnerSectionRole.Required;
                return true;
            }
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes)
            if (sectionId.StartsWith(prefix, StringComparison.Ordinal))
            {
                role = OwnerSectionRole.Required;
                return true;
            }
        if (sectionId.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcResidencePrefix, StringComparison.Ordinal)
            || sectionId.StartsWith(DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix, StringComparison.Ordinal))
        {
            role = OwnerSectionRole.Required;
            return true;
        }
        role = default;
        return false;
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> values, string value)
    {
        if (values == null || value == null) return false;
        for (int index = 0; index < values.Count; index++)
            if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
        return false;
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

    private static bool IsPersonLifeResidenceSection(string sectionId) =>
        !string.IsNullOrWhiteSpace(sectionId)
        && sectionId.StartsWith(
            DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix,
            StringComparison.Ordinal);

    private static bool IsCitySection(string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId)) return false;
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes)
            if (sectionId.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }
}
