using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Fixed passive row-count and local-revision witnesses for installed City Market owners.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public static class CityMarketCensusProvider
{
    public const int SchemaVersion = 1;
    public const string SectionIdPrefix = "p12e.city-market-stock-rows/";

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly MarketRuntime owner;
        private readonly string sectionId;

        public SectionProvider(MarketRuntime owner, string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                owner.Items.Count,
                owner.Revision);
        }
    }

    /// <summary>Creates one provider per City, ordered by its stable RuntimeId.</summary>
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<CityRuntime> cities)
    {
        if (cities == null) throw new ArgumentNullException(nameof(cities));

        List<CityRuntime> orderedCities = new List<CityRuntime>(cities.Count);
        HashSet<MarketRuntime> owners = new HashSet<MarketRuntime>();
        for (int i = 0; i < cities.Count; i++)
        {
            CityRuntime city = cities[i];
            if (city == null)
            {
                throw new ArgumentException("Market census requires every composed City.", nameof(cities));
            }

            if (string.IsNullOrWhiteSpace(city.RuntimeId))
            {
                throw new InvalidOperationException("Market census requires each City to have a RuntimeId.");
            }

            MarketRuntime owner = city.Market;
            if (owner == null || !owners.Add(owner))
            {
                throw new InvalidOperationException(
                    "Market census requires a distinct installed Market owner for every City.");
            }

            orderedCities.Add(city);
        }

        orderedCities.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[orderedCities.Count];
        string previousRuntimeId = null;
        for (int i = 0; i < orderedCities.Count; i++)
        {
            CityRuntime city = orderedCities[i];
            if (string.Equals(previousRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Market census requires unique City RuntimeIds.",
                    nameof(cities));
            }

            string encodedRuntimeId = city.RuntimeId.Length.ToString(CultureInfo.InvariantCulture)
                + ":" + city.RuntimeId;
            providers[i] = new SectionProvider(
                city.Market,
                SectionIdPrefix + encodedRuntimeId);
            previousRuntimeId = city.RuntimeId;
        }

        return Array.AsReadOnly(providers);
    }
}
