using System.Collections.Generic;
using System;

/// <summary>
/// Pure authored-profile validation that can run before any CityRuntime is constructed.
/// The caller owns the bootstrap/admission boundary and decides when to invoke it.
/// </summary>
public static class FiniteSourceProfileAdmission
{
    public static bool HasAuthoredMaterialFlowCity(IReadOnlyList<CityData> authoredCities)
    {
        if (authoredCities == null) return false;
        for (int i = 0; i < authoredCities.Count; i++)
        {
            CityData city = authoredCities[i];
            if (city == null) continue;
            if (city.materialFlowProfile != LocalMaterialFlowProfile.ExogenousDaily
                || !string.IsNullOrWhiteSpace(city.settlementSemanticId)
                || !string.IsNullOrWhiteSpace(city.materialFlowLocationId)
                || !string.IsNullOrWhiteSpace(city.marketStoreSemanticId)
                || (city.productionConfigs != null && city.productionConfigs.Exists(source =>
                    source != null && (!string.IsNullOrWhiteSpace(source.productionSourceId) || source.initialReserve != 0))))
                return true;
        }

        return false;
    }
    public static bool HasFiniteReserveProfile(IReadOnlyList<CityData> authoredCities)
    {
        if (authoredCities == null) return false;
        for (int i = 0; i < authoredCities.Count; i++)
        {
            CityData city = authoredCities[i];
            if (city != null && city.materialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily)
                return true;
        }

        return false;
    }

    public static bool HasMixedSourceProfile(IReadOnlyList<CityData> authoredCities)
    {
        if (authoredCities == null) return false;
        for (int i = 0; i < authoredCities.Count; i++)
            if (authoredCities[i] != null
                && authoredCities[i].materialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
                return true;
        return false;
    }

    public static bool TryValidateAuthoredCityCardinality(
        IReadOnlyList<CityData> authoredCities,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (authoredCities == null)
        {
            rejectionReason = "MissingAuthoredCities";
            return false;
        }

        int finiteProfileCount = 0;
        int mixedProfileCount = 0;
        for (int i = 0; i < authoredCities.Count; i++)
        {
            CityData city = authoredCities[i];
            if (city != null && city.materialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily)
                finiteProfileCount++;
            if (city != null && city.materialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
                mixedProfileCount++;
        }

        if (mixedProfileCount > 0)
        {
            if (authoredCities.Count != 1 || mixedProfileCount != 1
                || authoredCities[0] == null
                || authoredCities[0].materialFlowProfile != LocalMaterialFlowProfile.MixedSourcesDaily)
            {
                rejectionReason = "MixedSourcesProfileRequiresExactlyOneAuthoredCity";
                return false;
            }
            return TryValidateMixedSourceCity(authoredCities[0], out rejectionReason);
        }

        if (finiteProfileCount == 0) return true;
        if (authoredCities.Count != 1 || finiteProfileCount != 1
            || authoredCities[0] == null
            || authoredCities[0].materialFlowProfile != LocalMaterialFlowProfile.FiniteReserveDaily)
        {
            rejectionReason = "FiniteReserveProfileRequiresExactlyOneAuthoredCity";
            return false;
        }

        return true;
    }

    public static bool TryValidateMixedSourceCity(CityData city, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (city == null || city.materialFlowProfile != LocalMaterialFlowProfile.MixedSourcesDaily)
        { rejectionReason = "MissingMixedSourcesCity"; return false; }
        if (string.IsNullOrWhiteSpace(city.settlementSemanticId)
            || string.IsNullOrWhiteSpace(city.materialFlowLocationId)
            || string.IsNullOrWhiteSpace(city.marketStoreSemanticId))
        { rejectionReason = "MixedSourcesCityIdentityIncomplete"; return false; }
        if (city.productionConfigs == null || city.productionConfigs.Count != 2)
        { rejectionReason = "MixedSourcesProfileRequiresExactlyTwoSources"; return false; }

        HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
        bool hasExogenous = false;
        bool hasFinite = false;
        string itemDefinitionId = null;
        for (int i = 0; i < city.productionConfigs.Count; i++)
        {
            CityProductionConfig source = city.productionConfigs[i];
            if (source == null || source.item == null || string.IsNullOrWhiteSpace(source.item.DefinitionId)
                || source.amountPerDay <= 0 || string.IsNullOrWhiteSpace(source.productionSourceId)
                || string.IsNullOrWhiteSpace(source.contentRevision)
                || !Enum.IsDefined(typeof(CityProductionSourceKind), source.sourceKind)
                || !sourceIds.Add(source.productionSourceId))
            { rejectionReason = "MixedSourcesSourceIdentityOrQuantityInvalid"; return false; }

            if (itemDefinitionId == null) itemDefinitionId = source.item.DefinitionId;
            else if (!string.Equals(itemDefinitionId, source.item.DefinitionId, StringComparison.Ordinal))
            { rejectionReason = "MixedSourcesItemMismatch"; return false; }

            if (source.sourceKind == CityProductionSourceKind.ExogenousDaily)
            {
                if (hasExogenous || source.initialReserve != 0)
                { rejectionReason = "MixedSourcesExogenousPolicyInvalid"; return false; }
                hasExogenous = true;
            }
            else if (source.sourceKind == CityProductionSourceKind.FiniteReserveDaily)
            {
                if (hasFinite || source.initialReserve < 0)
                { rejectionReason = "MixedSourcesFinitePolicyInvalid"; return false; }
                hasFinite = true;
            }
            else
            { rejectionReason = "MixedSourcesExplicitKindRequired"; return false; }
        }

        if (!hasExogenous || !hasFinite)
        { rejectionReason = "MixedSourcesRequiresOneSourceOfEachKind"; return false; }
        if (city.marketItems == null || city.marketItems.Count != 1
            || city.marketItems[0] == null || city.marketItems[0].item == null
            || !string.Equals(city.marketItems[0].item.DefinitionId, itemDefinitionId, StringComparison.Ordinal)
            || city.PopulationConsumption.paymentMode != ConsumptionPaymentMode.Free)
        { rejectionReason = "MixedSourcesRequiresMatchingSingleMarketItemAndFreeConsumption"; return false; }

        return true;
    }
}
