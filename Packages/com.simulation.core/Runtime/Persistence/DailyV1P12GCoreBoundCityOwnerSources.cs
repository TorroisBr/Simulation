using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum DailyV1P12GCoreCitySourceCaptureFailure
{
    None,
    InvalidSaveOperation,
    MissingCoreOwnerBinding,
    OwnerCensusMismatch,
    OwnerCaptureFailed,
    StaleSourceBoundary
}

/// <summary>One detached P12-D City source captured from its Core owner.</summary>
internal sealed class DailyV1P12GCoreCityOwnerSource
{
    internal DailyV1CityRuntimeOwners Owner { get; }
    internal DailyV1P12DCityCoreOwnerHydrationInput Input { get; }
    internal IReadOnlyList<P12AOwnerSection> Sections { get; }

    internal DailyV1P12GCoreCityOwnerSource(
        DailyV1CityRuntimeOwners owner,
        DailyV1P12DCityCoreOwnerHydrationInput input,
        IReadOnlyList<P12AOwnerSection> sections)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Sections = sections ?? throw new ArgumentNullException(nameof(sections));
    }
}

/// <summary>
/// Captures the complete four-section P12-D City source set directly from the
/// active Core owner graph. This value is a partial P12-G input: NPC owner
/// groups, causal roots, image composition, target validation, and publication
/// remain separate requirements.
/// </summary>
internal sealed class DailyV1P12GCoreBoundCityOwnerSources
{
    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections;
    private readonly ReadOnlyCollection<DailyV1P12GCoreCityOwnerSource> cities;

    private DailyV1P12GCoreBoundCityOwnerSources(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        List<DailyV1P12GCoreCityOwnerSource> cities)
    {
        this.token = token;
        this.ownerSections = ownerSections;
        this.cities = cities.AsReadOnly();
    }

    internal IReadOnlyList<DailyV1P12GCoreCityOwnerSource> Cities => cities;

    internal bool IsFor(DailyV1P12GContinuationSaveOperation operation) =>
        operation != null
        && ReferenceEquals(token, operation.SourceWitness.Token)
        && ReferenceEquals(ownerSections, operation.SourceWitness.TokenOwnerSections);

    internal static bool TryCapture(
        DailyV1P12GContinuationSaveOperation operation,
        out DailyV1P12GCoreBoundCityOwnerSources sources,
        out DailyV1P12GCoreCitySourceCaptureFailure failure,
        out string diagnostic)
    {
        sources = null;
        failure = DailyV1P12GCoreCitySourceCaptureFailure.InvalidSaveOperation;
        diagnostic = null;
        DailyCaptureEligibilityFailure initialFailure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (operation == null || !operation.IsActive
            || !operation.TryRevalidate(out initialFailure))
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.StaleSourceBoundary;
            diagnostic = "A current active Core continuation-save witness is required: "
                + initialFailure + ".";
            return false;
        }

        DailyV1P12GSourceBoundaryWitness witness = operation.SourceWitness;
        DailyV1ActiveRuntimeSession session = operation.Session;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        DailyV1EffectiveDefinitions definitions = graph?.Definitions;
        if (graph == null || definitions == null || graph.Cities == null || graph.Cities.Count == 0
            || !ReferenceEquals(session.Definitions, definitions))
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.MissingCoreOwnerBinding;
            diagnostic = "The active Core session has no admitted City owner graph.";
            return false;
        }

        List<DailyV1P12GCoreCityOwnerSource> captured =
            new List<DailyV1P12GCoreCityOwnerSource>(graph.Cities.Count);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                if (!TryCaptureCity(city, definitions, witness.TokenOwnerSections,
                        out DailyV1P12GCoreCityOwnerSource source,
                        out DailyV1P12GCoreCitySourceCaptureFailure cityFailure,
                        out diagnostic))
                {
                    failure = cityFailure;
                    return false;
                }
                if (!cityIds.Add(city.RuntimeId))
                {
                    failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCaptureFailed;
                    diagnostic = "The Core City graph contains duplicate runtime identities.";
                    return false;
                }
                captured.Add(source);
            }

            if (!operation.TryRevalidate(out DailyCaptureEligibilityFailure finalFailure))
            {
                failure = DailyV1P12GCoreCitySourceCaptureFailure.StaleSourceBoundary;
                diagnostic = "The Core save witness changed during City capture: "
                    + finalFailure + ".";
                return false;
            }

            sources = new DailyV1P12GCoreBoundCityOwnerSources(
                witness.Token, witness.TokenOwnerSections, captured);
            failure = DailyV1P12GCoreCitySourceCaptureFailure.None;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "A Core City owner failed while making a detached source snapshot: "
                + exception.GetType().Name + ".";
            return false;
        }
    }

    private static bool TryCaptureCity(
        DailyV1CityRuntimeOwners city,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyV1P12GCoreCityOwnerSource source,
        out DailyV1P12GCoreCitySourceCaptureFailure failure,
        out string diagnostic)
    {
        source = null;
        failure = DailyV1P12GCoreCitySourceCaptureFailure.MissingCoreOwnerBinding;
        diagnostic = "A selected City is missing a Core-owned P12-D source.";
        if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
            || string.IsNullOrWhiteSpace(city.LocationRuntimeId)
            || city.Definitions == null || !ReferenceEquals(city.Definitions, definitions)
            || city.Definition == null
            || !definitions.Cities.TryGetValue(city.Definition.DefinitionId,
                out DailyV1CityDefinition admittedCity)
            || !ReferenceEquals(admittedCity, city.Definition)
            || city.NpcPresence == null || city.Market == null || city.Population == null
            || city.Counterparty == null || city.PopulationEconomy == null
            || !ReferenceEquals(city.Market.Definitions, definitions)
            || !ReferenceEquals(city.Market.Counterparty, city.Counterparty)
            || !string.Equals(city.Counterparty.CounterpartyRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(city.Population.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(city.PopulationEconomy.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            return false;

        string cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId);
        string presenceId = DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId);
        string marketId = DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix + cityKey;
        string populationId = DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix + cityKey;
        string receiptsId = DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix + cityKey;

        if (!TryFindSection(ownerSections, presenceId, out OwnerSectionCensusSnapshot presenceSection)
            || !MatchesSection(presenceSection, DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.NpcPresence, city.NpcPresence.OrderedNpcRuntimeIds.Count, city.NpcPresence.Revision)
            || !TryFindSection(ownerSections, marketId, out OwnerSectionCensusSnapshot marketSection)
            || !MatchesSection(marketSection, DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Market, city.Market.Items.Count, city.Market.Revision)
            || !TryFindSection(ownerSections, populationId, out OwnerSectionCensusSnapshot populationSection)
            || !MatchesSection(populationSection, DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Population, 1, city.Population.Revision)
            || !TryFindSection(ownerSections, receiptsId, out OwnerSectionCensusSnapshot receiptSection)
            || !ReferenceEquals(populationSection.OwnerInstanceIdentity, receiptSection.OwnerInstanceIdentity))
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCensusMismatch;
            diagnostic = "A selected City source does not match its exact P12-D owner identity/cardinality/revision.";
            return false;
        }

        if (!city.Population.TryCaptureOperationReceiptSnapshot(
                out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts,
                out long receiptRevision)
            || receipts == null
            || !MatchesSection(receiptSection,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Population, receipts.Count, receiptRevision))
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCensusMismatch;
            diagnostic = "The City population receipt snapshot differs from its witnessed owner census.";
            return false;
        }

        List<P12DCityMarketItemSnapshot> marketItems =
            new List<P12DCityMarketItemSnapshot>(city.Market.Items.Count);
        foreach (DailyV1MarketItemState row in city.Market.Items)
        {
            if (row == null || row.Item == null
                || !definitions.Items.TryGetValue(row.Item.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(admittedItem, row.Item))
            {
                failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCaptureFailed;
                diagnostic = "A City market row does not retain its exact admitted item definition.";
                return false;
            }
            marketItems.Add(new P12DCityMarketItemSnapshot(
                row.Item.DefinitionId, row.Amount, row.DesiredAmount, row.CurrentPrice));
        }

        DailyV1P12DCityCoreOwnerHydrationInput input = new DailyV1P12DCityCoreOwnerHydrationInput(
            city.RuntimeId, city.Definition.DefinitionId,
            city.LocationRuntimeId, city.LocationRuntimeId,
            city.NpcPresence.Revision, city.NpcPresence.OrderedNpcRuntimeIds,
            city.Counterparty.CounterpartyRuntimeId, city.Counterparty.LiquidityMode,
            city.Counterparty.MoneyAccount != null,
            city.Market.Revision, marketItems,
            city.PopulationEconomy.PopulationEconomicRuntimeId,
            city.PopulationEconomy.PaymentMode,
            city.PopulationEconomy.MoneyAccount != null,
            city.Population.SettlementRuntimeId, city.Population.CurrentPopulation,
            city.Population.Revision, receiptRevision, receipts,
            P12DCityOwnerImageSectionCodec.SchemaVersion);

        if (!P12DCityOwnerImageSectionCodec.TryEncode(input,
                out IReadOnlyList<P12AOwnerSection> encoded, out P12AResult encodeResult))
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "The Core City source failed its existing P12-D encoder: "
                + (encodeResult?.Code.ToString() ?? "InvalidOwnerValue") + ".";
            return false;
        }

        if (encoded.Count != 4
            || !EncodedSectionMatches(encoded, presenceId, city.NpcPresence.OrderedNpcRuntimeIds.Count,
                city.NpcPresence.Revision)
            || !EncodedSectionMatches(encoded, marketId, marketItems.Count, city.Market.Revision)
            || !EncodedSectionMatches(encoded, populationId, 1, city.Population.Revision)
            || !EncodedSectionMatches(encoded, receiptsId, receipts.Count, receiptRevision))
        {
            failure = DailyV1P12GCoreCitySourceCaptureFailure.OwnerCaptureFailed;
            diagnostic = "The existing P12-D encoder did not preserve the Core City owner vector.";
            return false;
        }

        source = new DailyV1P12GCoreCityOwnerSource(city, input, encoded);
        failure = DailyV1P12GCoreCitySourceCaptureFailure.None;
        diagnostic = null;
        return true;
    }

    private static bool TryFindSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        out OwnerSectionCensusSnapshot match)
    {
        match = null;
        if (sections == null || string.IsNullOrWhiteSpace(sectionId)) return false;
        int matches = 0;
        foreach (OwnerSectionCensusSnapshot section in sections)
            if (section != null && string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
            {
                match = section;
                matches++;
            }
        return matches == 1;
    }

    private static bool MatchesSection(
        OwnerSectionCensusSnapshot section,
        int schemaVersion,
        object ownerIdentity,
        int cardinality,
        long revision) =>
        section != null
        && section.SchemaVersion == schemaVersion
        && section.Role == OwnerSectionRole.Required
        && ReferenceEquals(section.OwnerInstanceIdentity, ownerIdentity)
        && section.Cardinality == cardinality
        && section.Revision == revision;

    private static bool EncodedSectionMatches(
        IReadOnlyList<P12AOwnerSection> sections,
        string sectionId,
        int cardinality,
        long revision)
    {
        P12AOwnerSection match = null;
        int matches = 0;
        foreach (P12AOwnerSection section in sections)
            if (section != null && string.Equals(section.Id, sectionId, StringComparison.Ordinal))
            {
                match = section;
                matches++;
            }
        return matches == 1 && match.Rows.Count == cardinality && match.Revision == revision;
    }
}
