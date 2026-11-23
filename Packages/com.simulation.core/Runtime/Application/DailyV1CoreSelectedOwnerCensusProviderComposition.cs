using System;
using System.Collections.Generic;

internal sealed class DailyV1CoreSelectedOwnerCensusProviderComposition
{
    private DailyV1CoreSelectedOwnerCensusProviderComposition(
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personStore,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> personLifeResidence,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12cRoots,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12eOwners,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> genealogy,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> justiceRecords,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12dExactEmptyOwners,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12fOwners,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> cities,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> identitySpatial,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> selectedNpcs,
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> all)
    {
        OwnerGraph = ownerGraph;
        PersonStoreRegistrations = personStore;
        PersonLifeResidenceRegistrations = personLifeResidence;
        P12CRootRegistrations = p12cRoots;
        P12EOwnerRegistrations = p12eOwners;
        GenealogyRegistrations = genealogy;
        JusticeRecordsRegistrations = justiceRecords;
        ExactEmptyProfileRegistrations = p12dExactEmptyOwners;
        P12FOwnerRegistrations = p12fOwners;
        CityRegistrations = cities;
        IdentitySpatialRegistrations = identitySpatial;
        SelectedNpcRegistrations = selectedNpcs;
        Registrations = all;
    }

    internal DailyV1SelectedRuntimeOwnerGraph OwnerGraph { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> PersonStoreRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> PersonLifeResidenceRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> P12CRootRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> P12EOwnerRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> GenealogyRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> JusticeRecordsRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> ExactEmptyProfileRegistrations { get; }
    internal IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> P12FOwnerRegistrations { get; }
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
            || !TryCreateP12CRootProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12cRootSource,
                out diagnostic)
            || !TryCreateP12EOwnerProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12eOwnerSource,
                out diagnostic)
            || !TryCreateGenealogyProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> genealogySource,
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
                p12cRootSource,
                IsP12CRootSection,
                expectedOwner: null,
                "P12-C allocator and record-sequence roots",
                expectedCount: 4,
                allowEmpty: false,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12cRoots,
                out diagnostic)
            || !TryNormalizeGroup(
                p12eOwnerSource,
                IsP12EOwnerSection,
                expectedOwner: null,
                "P12-E",
                expectedCount: graph.P12EOwners == null ? 0 : 23,
                allowEmpty: graph.P12EOwners == null,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12eOwners,
                out diagnostic)
            || !TryNormalizeGroup(
                genealogySource,
                IsGenealogySection,
                graph.GenealogyStore,
                "Genealogy",
                expectedCount: graph.GenealogyStore == null ? 0 : 1,
                allowEmpty: graph.GenealogyStore == null,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> genealogy,
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
                personStore.Count + personLifeResidence.Count + p12cRoots.Count
                + p12eOwners.Count + genealogy.Count + cities.Count
                + identitySpatial.Count + selectedNpcs.Count);
        HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
        if (!TryAddGroup(personStore, all, allIds, out diagnostic)
            || !TryAddGroup(personLifeResidence, all, allIds, out diagnostic)
            || !TryAddGroup(p12cRoots, all, allIds, out diagnostic)
            || !TryAddGroup(p12eOwners, all, allIds, out diagnostic)
            || !TryAddGroup(genealogy, all, allIds, out diagnostic)
            || !TryAddGroup(cities, all, allIds, out diagnostic)
            || !TryAddGroup(identitySpatial, all, allIds, out diagnostic)
            || !TryAddGroup(selectedNpcs, all, allIds, out diagnostic))
            return false;
        all.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        composition = new DailyV1CoreSelectedOwnerCensusProviderComposition(
            graph,
            personStore,
            personLifeResidence,
            p12cRoots,
            p12eOwners,
            genealogy,
            Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>(),
            Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>(),
            Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>(),
            cities,
            identitySpatial,
            selectedNpcs,
            all.AsReadOnly());
        return true;
    }

    /// <summary>
    /// Extends the reusable Core owner composition with the exact P12-F and
    /// PoliticalDecision authorities held by an unpublished Core session.
    /// The detached startup path uses this form; the graph-only form remains a
    /// partial inventory helper and is never sufficient to seal application
    /// readiness.
    /// </summary>
    internal static bool TryCreateForApplicationStart(
        DailyV1ActiveRuntimeSession session,
        out DailyV1CoreSelectedOwnerCensusProviderComposition composition,
        out string diagnostic)
    {
        composition = null;
        diagnostic = null;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        DailyV1ExecutionBoundaryState executionState = session?.ExecutionBoundaryState;
        ActorChoiceStore actorChoiceStore = executionState?.SelectedActorChoiceStore;
        if (session == null || graph == null || executionState == null
            || actorChoiceStore == null
            || !ReferenceEquals(graph.Definitions, session.Definitions)
            || !graph.MatchesExactOwnerInstances(graph.Cities, graph.Npcs)
            || !ReferenceEquals(actorChoiceStore.PersonStore, graph.PersonStore))
        {
            diagnostic = "The Core application-start census requires the exact selected runtime session and actor-choice owner.";
            return false;
        }

        if (!TryCreate(graph,
                out DailyV1CoreSelectedOwnerCensusProviderComposition existing,
                out diagnostic)
            || existing == null)
            return false;

        if (!TryCreateP12FOwnerProviderRegistrations(
                graph, actorChoiceStore,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12fSource,
                out diagnostic)
            || !TryNormalizeGroup(
                p12fSource,
                IsP12FOwnerSection,
                expectedOwner: null,
                "P12-F and PoliticalDecision",
                expectedCount: 6,
                allowEmpty: false,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12fOwners,
                out diagnostic))
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core application-start census could not compose its exact P12-F owners.";
            return false;
        }

        if (!TryCreateExactEmptyProfileProviderRegistrations(
                session,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> p12dExactEmptyOwners,
                out diagnostic))
            return false;

        if (!TryCreateJusticeRecordsProviderRegistrations(
                graph,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> justiceRecords,
                out diagnostic))
            return false;

        List<DailyV1OwnerSectionCensusProviderRegistration> all =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(
                existing.Registrations.Count + p12fOwners.Count
                + p12dExactEmptyOwners.Count + justiceRecords.Count);
        HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
        if (!TryAddGroup(existing.Registrations, all, allIds, out diagnostic)
            || !TryAddGroup(justiceRecords, all, allIds, out diagnostic)
            || !TryAddGroup(p12dExactEmptyOwners, all, allIds, out diagnostic)
            || !TryAddGroup(p12fOwners, all, allIds, out diagnostic))
            return false;
        all.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        composition = new DailyV1CoreSelectedOwnerCensusProviderComposition(
            graph,
            existing.PersonStoreRegistrations,
            existing.PersonLifeResidenceRegistrations,
            existing.P12CRootRegistrations,
            existing.P12EOwnerRegistrations,
            existing.GenealogyRegistrations,
            justiceRecords,
            p12dExactEmptyOwners,
            p12fOwners,
            existing.CityRegistrations,
            existing.IdentitySpatialRegistrations,
            existing.SelectedNpcRegistrations,
            all.AsReadOnly());
        return true;
    }

    private static bool TryCreateJusticeRecordsProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>();
        diagnostic = null;
        if (graph?.JusticeRecordsOwner == null)
        {
            diagnostic = "The selected Core startup requires its exact JusticeRecords owner.";
            return false;
        }

        IOwnerSectionCensusProvider provider = new JusticeRecordsOwnerCensusProvider(
            graph, graph.JusticeRecordsOwner);
        if (!TryReadWitness(provider, out OwnerSectionCensusWitness witness, out diagnostic)
            || witness == null
            || !string.Equals(witness.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                StringComparison.Ordinal)
            || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
            || !ReferenceEquals(witness.OwnerInstanceIdentity, graph.JusticeRecordsOwner))
        {
            diagnostic = "The selected Core startup could not witness its exact JusticeRecords owner.";
            return false;
        }

        registrations = Array.AsReadOnly(new[]
        {
            new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true)
        });
        return true;
    }

    private static bool TryCreateExactEmptyProfileProviderRegistrations(
        DailyV1ActiveRuntimeSession session,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>();
        diagnostic = null;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        ExplorableSiteStore owner = graph?.ExplorableSites;
        DailyV1ExecutionBoundaryState executionState = session?.ExecutionBoundaryState;
        DailyV1DecisionOccurrenceReceiptAuthority decisionReceipts =
            session?.EventAuthority?.DecisionOccurrenceReceipts;
        if (owner == null || owner.Count != 0 || owner.Revision != 0L
            || executionState == null || decisionReceipts == null
            || session.EventAuthority == null
            || !session.EventAuthority.IsFor(
                executionState.SelectedRuntimeIdAllocator,
                executionState.SimulationTime,
                executionState.SelectedRecordSequence,
                executionState.SelectedDecisionStore))
        {
            diagnostic = "The selected Core startup requires its exact Core session event authority and exact-empty P12-D ExplorableSite owner.";
            return false;
        }

        IOwnerSectionCensusProvider explorableSitesProvider =
            new ExactEmptyExplorableSiteCensusProvider(owner);
        if (!TryReadWitness(explorableSitesProvider, out OwnerSectionCensusWitness siteWitness, out diagnostic)
            || siteWitness == null
            || !string.Equals(siteWitness.SectionId,
                DailyV1SelectedOwnerSectionCensusContract.ExplorableSiteSectionId,
                StringComparison.Ordinal)
            || siteWitness.SchemaVersion != ExactEmptyExplorableSiteCensusProvider.SchemaVersion
            || !ReferenceEquals(siteWitness.OwnerInstanceIdentity, owner)
            || siteWitness.Cardinality != 0
            || siteWitness.Revision != 0L)
        {
            diagnostic = "The selected Core startup could not witness the exact-empty P12-D ExplorableSite owner.";
            return false;
        }

        IOwnerSectionCensusProvider decisionReceiptsProvider =
            new ExactEmptyDecisionOccurrenceReceiptCensusProvider(decisionReceipts);
        if (!TryReadWitness(decisionReceiptsProvider, out OwnerSectionCensusWitness decisionWitness, out diagnostic)
            || decisionWitness == null
            || !string.Equals(decisionWitness.SectionId,
                DailyV1DecisionOccurrenceReceiptAuthority.SectionId,
                StringComparison.Ordinal)
            || decisionWitness.SchemaVersion != DailyV1DecisionOccurrenceReceiptAuthority.SectionSchemaVersion
            || decisionWitness.Cardinality != 0
            || decisionWitness.Revision != 0L)
        {
            diagnostic = "The selected Core startup could not witness its exact-empty P12-F decision occurrence receipt authority.";
            return false;
        }

        registrations = Array.AsReadOnly(new[]
        {
            new DailyV1OwnerSectionCensusProviderRegistration(
                siteWitness.SectionId,
                siteWitness.SchemaVersion,
                OwnerSectionRole.ExplicitlyEmpty,
                explorableSitesProvider,
                siteWitness.OwnerInstanceIdentity,
                siteWitness.Cardinality,
                siteWitness.Revision,
                hasBaseline: true),
            new DailyV1OwnerSectionCensusProviderRegistration(
                decisionWitness.SectionId,
                decisionWitness.SchemaVersion,
                OwnerSectionRole.ExplicitlyEmpty,
                decisionReceiptsProvider,
                decisionWitness.OwnerInstanceIdentity,
                decisionWitness.Cardinality,
                decisionWitness.Revision,
                hasBaseline: true)
        });
        return true;
    }

    private static bool TryReadWitness(
        IOwnerSectionCensusProvider provider,
        out OwnerSectionCensusWitness witness,
        out string diagnostic)
    {
        witness = null;
        diagnostic = null;
        try
        {
            witness = provider?.GetCurrentCensus();
            return witness != null;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The exact-empty P12-D ExplorableSite owner could not be read.";
            return false;
        }
    }

    private static bool TryCreateP12FOwnerProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        ActorChoiceStore actorChoiceStore,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedP12FSectionWitnesses(
                graph, actorChoiceStore,
                out OwnerSectionCensusWitness[] expectedP12FWitnesses,
                out diagnostic))
            return false;

        IOwnerSectionCensusProvider[] p12fProviders =
        {
            new ActorChoiceP11CensusProvider(actorChoiceStore),
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives),
            new TravelPartyCensusProvider(graph.TravelParties),
            new ExpeditionCensusProvider(graph.Expeditions),
            new PoliticalKnowledgeStoreCensusProvider(graph.PoliticalKnowledgeStore)
        };
        Dictionary<string, OwnerSectionCensusWitness> expectedById =
            new Dictionary<string, OwnerSectionCensusWitness>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusWitness expected in expectedP12FWitnesses)
            if (expected == null || string.IsNullOrWhiteSpace(expected.SectionId)
                || !expectedById.TryAdd(expected.SectionId, expected))
            {
                diagnostic = "The Core P12-F census produced a missing or duplicate expected owner witness.";
                return false;
            }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(6);
        HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (IOwnerSectionCensusProvider provider in p12fProviders)
        {
            OwnerSectionCensusWitness witness;
            try
            {
                witness = provider?.GetCurrentCensus();
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is InvalidOperationException
                || exception is OverflowException)
            {
                diagnostic = "A Core P12-F census provider could not read its exact live owner.";
                return false;
            }

            if (witness == null
                || !expectedById.TryGetValue(witness.SectionId, out OwnerSectionCensusWitness expected)
                || witness.SchemaVersion != expected.SchemaVersion
                || !ReferenceEquals(witness.OwnerInstanceIdentity, expected.OwnerInstanceIdentity)
                || witness.Cardinality != expected.Cardinality
                || witness.Revision != expected.Revision
                || !observed.Add(witness.SectionId))
            {
                diagnostic = "A Core P12-F provider differs from its exact selected-session owner witness.";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        if (observed.Count != expectedP12FWitnesses.Length)
        {
            diagnostic = "The Core P12-F provider registrations do not cover every exact owner witness.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedPoliticalDecisionSection(
                graph, out OwnerSectionCensusSnapshot politicalDecision,
                out diagnostic))
            return false;

        IOwnerSectionCensusProvider politicalDecisionProvider =
            new PoliticalDecisionStoreCensusProvider(graph.PoliticalDecisionStore);
        OwnerSectionCensusWitness politicalDecisionWitness;
        try
        {
            politicalDecisionWitness = politicalDecisionProvider.GetCurrentCensus();
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The Core PoliticalDecision provider could not read its exact live owner.";
            return false;
        }

        if (politicalDecisionWitness == null
            || !string.Equals(politicalDecisionWitness.SectionId,
                politicalDecision.SectionId, StringComparison.Ordinal)
            || politicalDecisionWitness.SchemaVersion != politicalDecision.SchemaVersion
            || !ReferenceEquals(politicalDecisionWitness.OwnerInstanceIdentity,
                politicalDecision.OwnerInstanceIdentity)
            || politicalDecisionWitness.Cardinality != politicalDecision.Cardinality
            || politicalDecisionWitness.Revision != politicalDecision.Revision)
        {
            diagnostic = "The Core PoliticalDecision provider differs from its exact selected-session owner witness.";
            return false;
        }

        built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
            politicalDecisionWitness.SectionId,
            politicalDecisionWitness.SchemaVersion,
            OwnerSectionRole.Required,
            politicalDecisionProvider,
            politicalDecisionWitness.OwnerInstanceIdentity,
            politicalDecisionWitness.Cardinality,
            politicalDecisionWitness.Revision,
            hasBaseline: true));
        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        registrations = built.AsReadOnly();
        return true;
    }

    private static bool TryCreateP12EOwnerProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        return DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12EOwnerSections(
            graph,
            Array.Empty<OwnerSectionCensusSnapshot>(),
            out _,
            out registrations,
            out diagnostic);
    }

    private static bool TryCreateGenealogyProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = Array.Empty<DailyV1OwnerSectionCensusProviderRegistration>();
        diagnostic = null;
        if (graph == null)
        {
            diagnostic = "The Core Genealogy census requires its exact owner graph.";
            return false;
        }
        if (graph.GenealogyStore == null)
            return true;

        IOwnerSectionCensusProvider provider = new GenealogyCensusProvider(graph.GenealogyStore);
        OwnerSectionCensusWitness witness;
        try
        {
            witness = provider.GetCurrentCensus();
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            diagnostic = "The Core Genealogy provider could not read its live owner witness.";
            return false;
        }

        if (witness == null
            || !string.Equals(witness.SectionId, GenealogyCensusProvider.SectionId, StringComparison.Ordinal)
            || witness.SchemaVersion != GenealogyCensusProvider.SchemaVersion
            || !ReferenceEquals(witness.OwnerInstanceIdentity, graph.GenealogyStore)
            || witness.Cardinality < 0
            || witness.Revision < 0L)
        {
            diagnostic = "The Core Genealogy provider returned a missing or mismatched owner witness.";
            return false;
        }

        registrations = new[]
        {
            new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true)
        };
        return true;
    }

    private static bool TryCreateP12CRootProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        if (graph?.RuntimeIdAllocator == null || graph.RecordSequence == null)
        {
            diagnostic = "The Core P12-C census requires the exact RuntimeIdAllocator and SimulationRecordSequence bound to the selected owner graph.";
            return false;
        }

        IOwnerSectionCensusProvider[] providers =
        {
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(graph.RuntimeIdAllocator),
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(graph.RuntimeIdAllocator),
            RuntimeIdAllocatorCensusProvider.CreateTravelPartyCounterProvider(graph.RuntimeIdAllocator),
            new SimulationRecordSequenceCensusProvider(graph.RecordSequence)
        };
        string[] expectedIds =
        {
            RuntimeIdAllocatorCensusProvider.EventsSectionId,
            RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
            RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
            SimulationRecordSequenceCensusProvider.SectionId
        };
        int[] expectedSchemas =
        {
            RuntimeIdAllocatorCensusProvider.SchemaVersion,
            RuntimeIdAllocatorCensusProvider.SchemaVersion,
            RuntimeIdAllocatorCensusProvider.SchemaVersion,
            SimulationRecordSequenceCensusProvider.SchemaVersion
        };
        object[] expectedOwnerIdentities = new object[providers.Length];
        long[] expectedRevisions = new long[providers.Length];
        try
        {
            RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
                graph.RuntimeIdAllocator, RuntimeIdAllocatorCensusCounter.Events,
                out expectedOwnerIdentities[0], out expectedRevisions[0]);
            RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
                graph.RuntimeIdAllocator, RuntimeIdAllocatorCensusCounter.Decisions,
                out expectedOwnerIdentities[1], out expectedRevisions[1]);
            RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
                graph.RuntimeIdAllocator, RuntimeIdAllocatorCensusCounter.TravelParties,
                out expectedOwnerIdentities[2], out expectedRevisions[2]);
            SimulationRecordSequenceOwnerBridge.CaptureCensusIdentity(
                graph.RecordSequence, out expectedOwnerIdentities[3], out expectedRevisions[3]);
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            diagnostic = "The Core P12-C census could not capture the exact allocator or record-sequence owner baseline.";
            return false;
        }
        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(providers.Length);
        HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < providers.Length; i++)
        {
            OwnerSectionCensusWitness witness;
            try
            {
                witness = providers[i].GetCurrentCensus();
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "A Core P12-C allocator or record-sequence provider could not read its live witness.";
                return false;
            }

            if (witness == null
                || !string.Equals(witness.SectionId, expectedIds[i], StringComparison.Ordinal)
                || witness.SchemaVersion != expectedSchemas[i]
                || expectedOwnerIdentities[i] == null
                || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwnerIdentities[i])
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || witness.Revision != expectedRevisions[i]
                || !observed.Add(witness.SectionId))
            {
                diagnostic = "A Core P12-C provider returned a missing, duplicate, or invalid owner witness.";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                providers[i],
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        registrations = built.AsReadOnly();
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

        if (!TryCreateForApplicationStart(session,
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

    private static bool IsP12CRootSection(string sectionId) =>
        string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.DecisionsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal);

    private static bool IsP12EOwnerSection(string sectionId) =>
        string.Equals(sectionId, InstitutionOfficeCensusProvider.InstitutionsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, InstitutionOfficeCensusProvider.OfficesSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, InstitutionOfficeCensusProvider.IncumbenciesSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, InstitutionOfficeCensusProvider.TenuresSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PropertyOwnershipCensusProvider.OwnershipSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PropertyOwnershipCensusProvider.TransferHistorySectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, EstateCensusProvider.SectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, FactionStoreCensusProvider.FactionsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, FactionStoreCensusProvider.AffiliationsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PoliticalClaimStoreCensusProvider.ClaimsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PoliticalClaimStoreCensusProvider.RecognitionsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PoliticalSupportStoreCensusProvider.RelationsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, ArmedForceStoreCensusProvider.ForcesSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, ArmedForceStoreCensusProvider.ContingentsSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, ContingentManpowerCensusProvider.SectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, ArmedForceSpatialCensusProvider.SectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PersistentConflictCensusProvider.SectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PersistentWarCensusProvider.SectionId, StringComparison.Ordinal)
        || string.Equals(sectionId, PersistentBattleCensusProvider.SectionId, StringComparison.Ordinal);

    private static bool IsGenealogySection(string sectionId) =>
        string.Equals(sectionId, GenealogyCensusProvider.SectionId, StringComparison.Ordinal);

    private static bool IsP12FOwnerSection(string sectionId)
    {
        foreach (string expected in DailyV1SelectedRuntimeOwnerCensusSource.CoreOwnedP12FSectionIds)
            if (string.Equals(sectionId, expected, StringComparison.Ordinal))
                return true;
        return string.Equals(sectionId,
            PoliticalDecisionStoreCensusProvider.SectionId,
            StringComparison.Ordinal);
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

    private sealed class ExactEmptyExplorableSiteCensusProvider : IOwnerSectionCensusProvider
    {
        internal const int SchemaVersion = 1;
        private readonly ExplorableSiteStore owner;

        internal ExactEmptyExplorableSiteCensusProvider(ExplorableSiteStore owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (owner.Count != 0 || owner.Revision != 0L)
                throw new InvalidOperationException(
                    "The selected Daily-v1 P12-D ExplorableSite owner must remain exact-empty.");
            return new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.ExplorableSiteSectionId,
                SchemaVersion,
                owner,
                cardinality: 0,
                revision: 0L);
        }
    }

    private sealed class ExactEmptyDecisionOccurrenceReceiptCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1DecisionOccurrenceReceiptAuthority owner;
        private readonly object expectedOwnerIdentity;

        internal ExactEmptyDecisionOccurrenceReceiptCensusProvider(
            DailyV1DecisionOccurrenceReceiptAuthority owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            OwnerSectionCensusWitness baseline = owner.GetCensus();
            expectedOwnerIdentity = baseline?.OwnerInstanceIdentity
                ?? throw new InvalidOperationException("The Core decision receipt authority has no census identity.");
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            OwnerSectionCensusWitness witness = owner.GetCensus();
            if (witness == null
                || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwnerIdentity)
                || witness.Cardinality != 0
                || witness.Revision != 0L)
                throw new InvalidOperationException(
                    "The selected Daily-v1 P12-F decision occurrence receipt owner must remain exact-empty.");
            return witness;
        }
    }

    private sealed class JusticeRecordsOwnerCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly DailyV1JusticeRecordsOwner owner;

        internal JusticeRecordsOwnerCensusProvider(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1JusticeRecordsOwner owner)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (!ReferenceEquals(graph.JusticeRecordsOwner, owner)
                || owner.WantedRecords == null
                || owner.Sentences == null
                || owner.Revision < 0L)
                throw new InvalidOperationException("The selected Core Justice owner is unavailable or malformed.");

            HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
                if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !npcIds.Add(npc.RuntimeId))
                    throw new InvalidOperationException("The selected Core Justice owner has no exact NPC identity set.");
            HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId) || !cityIds.Add(city.RuntimeId))
                    throw new InvalidOperationException("The selected Core Justice owner has no exact City identity set.");

            HashSet<DailyV1JusticeWantedRecordOwner> warrants =
                new HashSet<DailyV1JusticeWantedRecordOwner>();
            foreach (DailyV1JusticeWantedRecordOwner warrant in owner.WantedRecords)
                if (warrant == null || !warrants.Add(warrant)
                    || string.IsNullOrWhiteSpace(warrant.TargetRuntimeId)
                    || string.IsNullOrWhiteSpace(warrant.CityRuntimeId)
                    || !npcIds.Contains(warrant.TargetRuntimeId)
                    || !cityIds.Contains(warrant.CityRuntimeId))
                    throw new InvalidOperationException("A selected Core Justice warrant does not resolve to exact admitted owners.");

            HashSet<DailyV1JusticeSentenceOwner> sentences =
                new HashSet<DailyV1JusticeSentenceOwner>();
            foreach (DailyV1JusticeSentenceOwner sentence in owner.Sentences)
                if (sentence == null || !sentences.Add(sentence)
                    || string.IsNullOrWhiteSpace(sentence.TargetRuntimeId)
                    || string.IsNullOrWhiteSpace(sentence.CityRuntimeId)
                    || !npcIds.Contains(sentence.TargetRuntimeId)
                    || !cityIds.Contains(sentence.CityRuntimeId)
                    || sentence.Warrant == null || !warrants.Contains(sentence.Warrant))
                    throw new InvalidOperationException("A selected Core Justice sentence does not resolve to its exact admitted owners.");

            int cardinality = checked(owner.WantedRecords.Count + owner.Sentences.Count);
            return new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                owner,
                cardinality,
                owner.Revision);
        }
    }
}
