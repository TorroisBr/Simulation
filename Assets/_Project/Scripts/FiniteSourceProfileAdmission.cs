using System.Collections.Generic;

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
        for (int i = 0; i < authoredCities.Count; i++)
        {
            CityData city = authoredCities[i];
            if (city != null && city.materialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily)
                finiteProfileCount++;
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
}
