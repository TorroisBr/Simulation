using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Passive per-City population and operation-receipt census providers.</summary>
public static class SettlementPopulationCensusProvider
{
    public const int SchemaVersion = 1;
    public const string AggregateSectionPrefix = "p12d.city-population.aggregate/";
    public const string OperationReceiptsSectionPrefix = "p12d.city-population.operation-receipts/";

    private enum Section
    {
        Aggregate,
        OperationReceipts
    }

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly SettlementPopulationRuntime owner;
        private readonly Section section;
        private readonly string sectionId;

        public SectionProvider(SettlementPopulationRuntime owner, Section section, string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (section == Section.Aggregate)
            {
                return new OwnerSectionCensusWitness(
                    sectionId,
                    SchemaVersion,
                    owner,
                    1,
                    owner.Revision);
            }

            owner.GetOperationReceiptCensus(out int cardinality, out long receiptRevision);
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                cardinality,
                receiptRevision);
        }
    }

    /// <summary>
    /// Creates two fixed single-section providers per City, ordered by RuntimeId.
    /// Each section is bound to that City's exact installed population owner.
    /// </summary>
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<CityRuntime> cities)
    {
        if (cities == null) throw new ArgumentNullException(nameof(cities));

        List<CityRuntime> orderedCities = new List<CityRuntime>(cities.Count);
        for (int i = 0; i < cities.Count; i++)
        {
            CityRuntime city = cities[i];
            if (city == null)
            {
                throw new ArgumentException("Population census requires every composed City.", nameof(cities));
            }

            string runtimeId = city.RuntimeId;
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new InvalidOperationException("Population census requires each City to have a RuntimeId.");
            }

            SettlementPopulationRuntime owner = city.Population;
            if (owner == null)
            {
                throw new InvalidOperationException(
                    "Population census requires each composed City to have an installed population owner.");
            }

            if (!string.Equals(owner.SettlementRuntimeId, runtimeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A City population owner must use the RuntimeId of its owning City.");
            }

            orderedCities.Add(city);
        }

        orderedCities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[orderedCities.Count * 2];
        string previousRuntimeId = null;
        int providerIndex = 0;
        foreach (CityRuntime city in orderedCities)
        {
            if (string.Equals(previousRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Population census requires unique City RuntimeIds.",
                    nameof(cities));
            }

            string encodedRuntimeId = city.RuntimeId.Length.ToString(CultureInfo.InvariantCulture)
                + ":" + city.RuntimeId;
            SettlementPopulationRuntime owner = city.Population;
            providers[providerIndex++] = new SectionProvider(
                owner,
                Section.Aggregate,
                AggregateSectionPrefix + encodedRuntimeId);
            providers[providerIndex++] = new SectionProvider(
                owner,
                Section.OperationReceipts,
                OperationReceiptsSectionPrefix + encodedRuntimeId);
            previousRuntimeId = city.RuntimeId;
        }

        return Array.AsReadOnly(providers);
    }
}
