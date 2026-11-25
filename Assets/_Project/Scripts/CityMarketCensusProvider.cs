using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Fixed passive row-count and local-revision witnesses for installed City Market owners.
/// Reads are unsynchronized and do not establish capture eligibility.
/// </summary>
public static class CityMarketCensusProvider
{
    public const int SchemaVersion = DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion;
    public const string SectionIdPrefix = DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix;

    public static string SectionIdFor(string cityRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(cityRuntimeId))
            throw new ArgumentException("Market section requires a City RuntimeId.", nameof(cityRuntimeId));
        string encodedRuntimeId = cityRuntimeId.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + cityRuntimeId;
        return SectionIdPrefix + encodedRuntimeId;
    }

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly CityRuntime city;
        private readonly string sectionId;

        public SectionProvider(CityRuntime city, string sectionId)
        {
            this.city = city ?? throw new ArgumentNullException(nameof(city));
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            MarketRuntime projection = city.Market;
            DailyV1MarketOwner selectedOwner = city.SelectedRuntimeOwners?.Market;
            if (selectedOwner == null)
            {
                return new OwnerSectionCensusWitness(
                    sectionId,
                    SchemaVersion,
                    projection,
                    projection.Items.Count,
                    projection.Revision);
            }

            if (!ReferenceEquals(projection.SelectedDailyV1Owner, selectedOwner)
                || !MatchesSelectedProjection(projection, selectedOwner))
            {
                throw new InvalidOperationException(
                    "Selected City Market owner differs from its exact Unity projection.");
            }

            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                selectedOwner,
                selectedOwner.Items.Count,
                selectedOwner.Revision);
        }

        private static bool MatchesSelectedProjection(
            MarketRuntime projection,
            DailyV1MarketOwner selectedOwner)
        {
            IReadOnlyList<MarketItemRuntime> projectedRows = projection.Items;
            IReadOnlyList<DailyV1MarketItemState> ownerRows = selectedOwner.Items;
            if (projectedRows == null || ownerRows == null
                || projectedRows.Count != ownerRows.Count)
                return false;

            for (int index = 0; index < ownerRows.Count; index++)
            {
                MarketItemRuntime projected = projectedRows[index];
                DailyV1MarketItemState row = ownerRows[index];
                if (projected == null || row == null
                    || !ReferenceEquals(projected.SelectedDefinition, row.Item)
                    || projected.Amount != row.Amount
                    || projected.DesiredAmount != row.DesiredAmount
                    || !projected.CurrentPrice.Equals(row.CurrentPrice))
                    return false;
            }

            return true;
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

            providers[i] = new SectionProvider(
                city,
                SectionIdFor(city.RuntimeId));
            previousRuntimeId = city.RuntimeId;
        }

        return Array.AsReadOnly(providers);
    }
}
