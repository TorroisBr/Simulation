using System.Collections.Immutable;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Observations.Navigation;

public static class WorldInspector
{
    public static ImmutableArray<FactValue> BuildFacts(ObservationDocument observation, AtlasEntityKey key)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var facts = ImmutableArray.CreateBuilder<FactValue>();
        facts.Add(new FactValue("Identity", key.SourceId, FactAvailability.Known));

        switch (key.Kind)
        {
            case AtlasEntityKind.World:
                facts.Add(new FactValue("World ID", observation.WorldId, FactAvailability.Known));
                facts.Add(new FactValue("Regions", null, FactAvailability.NotCovered, "Not covered by this observation."));
                facts.Add(new FactValue("Recorded events", null, FactAvailability.NotCovered, "No causal event capability is included."));
                break;
            case AtlasEntityKind.Hex:
                AddHexFacts(facts, WorldNavigator.GetHex(observation, key.SourceId));
                facts.Add(new FactValue("City market stock", null, FactAvailability.NotApplicable,
                    "Stock is a City scoped value, not a Hex property."));
                facts.Add(new FactValue("Recorded events", null, FactAvailability.NotCovered,
                    "No causal event capability is included."));
                break;
            case AtlasEntityKind.Location:
                AddLocationFacts(facts, observation, WorldNavigator.GetLocation(observation, key.SourceId));
                facts.Add(new FactValue("City market stock", null, FactAvailability.NotApplicable,
                    "Stock is a City scoped value."));
                break;
            case AtlasEntityKind.City:
                AddCityFacts(facts, observation, WorldNavigator.GetCity(observation, key.SourceId));
                break;
        }

        return facts.ToImmutable();
    }

    public static FactValue GetStockFact(ObservationDocument observation, string cityId, string itemId)
    {
        if (!StringComparer.Ordinal.Equals(observation.CityMarketStock.Scope.CityId, cityId) ||
            !StringComparer.Ordinal.Equals(observation.CityMarketStock.Scope.ItemDefinitionId, itemId) ||
            observation.CityMarketStock.Coverage != CapabilityCoverage.Complete)
        {
            return new FactValue("Stock quantity", null, FactAvailability.NotCovered,
                "This observation does not cover the requested City and item scope.");
        }

        return observation.TryGetStock(cityId, itemId, out var row)
            ? new FactValue("Stock quantity", row!.Quantity, FactAvailability.Known)
            : new FactValue("Stock quantity", null, FactAvailability.Unknown,
                "No row in complete scope; this is not the same as quantity zero.");
    }

    private static void AddHexFacts(ImmutableArray<FactValue>.Builder facts, HexFact? hex)
    {
        if (hex is null)
        {
            facts.Add(new FactValue("Coordinates", null, FactAvailability.NotCovered));
            return;
        }

        if (hex.HasCoordinates)
        {
            facts.Add(new FactValue("Axial coordinates", $"q={hex.Q}, r={hex.R}", FactAvailability.Known));
            facts.Add(new FactValue("Terrain identity", $"{hex.TerrainId} · revision {hex.TerrainRevision}", FactAvailability.Known));
        }
        else
        {
            facts.Add(new FactValue("Axial coordinates", null, FactAvailability.Unknown, "No registered coordinates; this Hex is unplaced."));
            facts.Add(new FactValue("Terrain identity", null, FactAvailability.Unknown, "No terrain identity was recorded for this unplaced Hex."));
        }
    }

    private static void AddLocationFacts(ImmutableArray<FactValue>.Builder facts, ObservationDocument observation, LocationFact? location)
    {
        if (location is null)
        {
            facts.Add(new FactValue("Anchor Hex", null, FactAvailability.NotCovered));
            return;
        }

        facts.Add(new FactValue("Anchor Hex", location.AnchorHexId, FactAvailability.Known));
        var hex = WorldNavigator.GetHex(observation, location.AnchorHexId);
        facts.Add(hex?.HasCoordinates == true
            ? new FactValue("Anchor coordinates", $"q={hex.Q}, r={hex.R}", FactAvailability.Known)
            : new FactValue("Anchor coordinates", null, FactAvailability.Unknown, "The anchor Hex has no registered coordinates."));
    }

    private static void AddCityFacts(ImmutableArray<FactValue>.Builder facts, ObservationDocument observation, CityFact? city)
    {
        if (city is null)
        {
            facts.Add(new FactValue("Label", null, FactAvailability.NotCovered));
            return;
        }

        facts.Add(city.Label is null
            ? new FactValue("Label", null, FactAvailability.Unknown, "No label was recorded.")
            : new FactValue("Label", city.Label, FactAvailability.Known));
        facts.Add(city.P8LocationId is null
            ? new FactValue("P8 Location", null, FactAvailability.Unknown, "No binding was recorded in this complete binding scope.")
            : new FactValue("P8 Location", city.P8LocationId, FactAvailability.Known));
        facts.Add(GetStockFact(observation, city.CityId, observation.CityMarketStock.Scope.ItemDefinitionId));
        facts.Add(new FactValue("Causal stock event", null, FactAvailability.NotCovered,
            "The observation reports values and does not include causal events."));
    }
}
