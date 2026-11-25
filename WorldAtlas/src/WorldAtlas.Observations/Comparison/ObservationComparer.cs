using System.Collections.Immutable;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Observations.Comparison;

public enum ComparisonKind
{
    ObservedValueDifference
}

public sealed record ObservationDifference(
    string Key,
    string Label,
    FactValue Before,
    FactValue After,
    ComparisonKind Kind);

public sealed record ObservationComparison(
    bool IsComparable,
    string Summary,
    ObservationBoundary? BeforeBoundary,
    ObservationBoundary? AfterBoundary,
    string? ScopeDescription,
    ImmutableArray<ObservationDifference> Differences)
{
    public static ObservationComparison Rejected(string reason, ObservationDocument before, ObservationDocument after) =>
        new(false, reason, before.Boundary, after.Boundary, null, ImmutableArray<ObservationDifference>.Empty);
}

public sealed class ObservationComparer
{
    public ObservationComparison Compare(ObservationDocument before, ObservationDocument after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var rejection = CompatibilityFailure(before, after);
        if (rejection is not null)
        {
            return ObservationComparison.Rejected(rejection, before, after);
        }

        var cityId = before.CityMarketStock.Scope.CityId;
        var itemId = before.CityMarketStock.Scope.ItemDefinitionId;
        var beforeFact = StockFact(before, cityId, itemId);
        var afterFact = StockFact(after, cityId, itemId);
        var differences = EqualityComparer<object?>.Default.Equals(beforeFact.Value, afterFact.Value) &&
                          beforeFact.Availability == afterFact.Availability
            ? ImmutableArray<ObservationDifference>.Empty
            : ImmutableArray.Create(new ObservationDifference(
                $"city-market-stock/v1:{cityId}:{itemId}",
                $"{itemId} stock at {cityId}",
                beforeFact,
                afterFact,
                ComparisonKind.ObservedValueDifference));

        var boundaryText = $"Day {before.Boundary.AbsoluteDay}, sequence {before.Boundary.CompletedSequence} → " +
                           $"Day {after.Boundary.AbsoluteDay}, sequence {after.Boundary.CompletedSequence}";
        var summary = differences.IsEmpty ? "No stock value difference was observed." : "Observed value difference; causal events are not covered.";
        return new ObservationComparison(
            true,
            $"{summary} {boundaryText}.",
            before.Boundary,
            after.Boundary,
            $"City {cityId} · item {itemId}",
            differences);
    }

    private static string? CompatibilityFailure(ObservationDocument before, ObservationDocument after)
    {
        if (before.SourceKind != after.SourceKind || before.Profile != after.Profile)
        {
            return "These observations have different source kinds or profiles.";
        }

        if (before.SourceKind != ObservationSourceKind.Demo)
        {
            return "ATLAS-A only compares the explicitly labeled Demo profile.";
        }

        if (!StringComparer.Ordinal.Equals(before.WorldId, after.WorldId) ||
            !StringComparer.Ordinal.Equals(before.RunId, after.RunId))
        {
            return "These observations are from different worlds or runs.";
        }

        if (!StringComparer.Ordinal.Equals(before.Schema, after.Schema) || before.Version != after.Version ||
            before.Producer != after.Producer)
        {
            return "These observations have different schema or producer identities.";
        }

        if (!StringComparer.Ordinal.Equals(before.Geography.Scope, after.Geography.Scope) ||
            !StringComparer.Ordinal.Equals(before.Cities.Scope, after.Cities.Scope) ||
            before.CityMarketStock.Scope != after.CityMarketStock.Scope)
        {
            return "These observations cover different named capability scopes.";
        }

        if (before.Geography.Coverage != after.Geography.Coverage || before.Cities.Coverage != after.Cities.Coverage ||
            before.Cities.BindingCoverage != after.Cities.BindingCoverage ||
            before.CityMarketStock.Coverage != after.CityMarketStock.Coverage)
        {
            return "These observations declare different capability coverage.";
        }

        if (before.ObservationIndex == long.MaxValue || after.ObservationIndex != before.ObservationIndex + 1)
        {
            return "The observations are not adjacent in the recorded run.";
        }

        if (!StringComparer.Ordinal.Equals(after.PreviousDigest, before.RawDigest))
        {
            return "The later observation does not name the exact bytes of the earlier file as its predecessor.";
        }

        if (after.Boundary.AbsoluteDay <= before.Boundary.AbsoluteDay ||
            after.Boundary.CompletedSequence <= before.Boundary.CompletedSequence)
        {
            return "The later observation does not have a strictly increasing completed boundary.";
        }

        return null;
    }

    private static FactValue StockFact(ObservationDocument observation, string cityId, string itemId)
    {
        if (!StringComparer.Ordinal.Equals(observation.CityMarketStock.Scope.CityId, cityId) ||
            !StringComparer.Ordinal.Equals(observation.CityMarketStock.Scope.ItemDefinitionId, itemId) ||
            observation.CityMarketStock.Coverage != CapabilityCoverage.Complete)
        {
            return new FactValue("Quantity", null, FactAvailability.NotCovered);
        }

        return observation.TryGetStock(cityId, itemId, out var row)
            ? new FactValue("Quantity", row!.Quantity, FactAvailability.Known)
            : new FactValue("Quantity", null, FactAvailability.Unknown, "No row in complete scope; this does not mean zero.");
    }
}
