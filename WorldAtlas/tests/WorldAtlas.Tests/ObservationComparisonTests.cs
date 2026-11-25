using System.Collections.Immutable;
using WorldAtlas.Observations.Comparison;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class ObservationComparisonTests
{
    private readonly ObservationComparer _comparer = new();

    [Test]
    public void ReportsWheatChangeAsObservedDifferenceWithBothBoundaries()
    {
        var before = ObservationFixture.Read("observation-day-1.json");
        var after = ObservationFixture.Read("observation-day-2.json");
        var result = _comparer.Compare(before, after);

        Assert.That(result.IsComparable, Is.True);
        Assert.That(result.BeforeBoundary, Is.EqualTo(new ObservationBoundary(1, 1)));
        Assert.That(result.AfterBoundary, Is.EqualTo(new ObservationBoundary(2, 2)));
        Assert.That(result.ScopeDescription, Is.EqualTo("City city-1 · item wheat"));
        Assert.That(result.Differences, Has.Length.EqualTo(1));
        Assert.That(result.Differences[0].Kind, Is.EqualTo(ComparisonKind.ObservedValueDifference));
        Assert.That(result.Differences[0].Before.Value, Is.EqualTo(20L));
        Assert.That(result.Differences[0].After.Value, Is.EqualTo(25L));
        Assert.That(result.Summary, Does.Contain("causal events are not covered"));
    }

    [Test]
    public void RejectsDifferentRunWorldProducerScopeGapAndUnlinkedPredecessor()
    {
        var before = ObservationFixture.Read("observation-day-1.json");
        var after = ObservationFixture.Read("observation-day-2.json");

        AssertRejected(before, after with { WorldId = "demo:other" }, "different worlds or runs");
        AssertRejected(before, after with { RunId = "demo:other-run" }, "different worlds or runs");
        AssertRejected(before, after with { Producer = after.Producer with { Build = "other-build" } }, "different schema or producer");
        AssertRejected(before, after with { CityMarketStock = after.CityMarketStock with { Scope = new StockScope("city-2", "wheat") } }, "different named capability scopes");
        AssertRejected(before, after with { ObservationIndex = 3 }, "not adjacent");
        AssertRejected(before, after with { PreviousDigest = new string('0', 64) }, "exact bytes");
        AssertRejected(before, after with { Boundary = new ObservationBoundary(1, 2) }, "strictly increasing");
    }

    [Test]
    public void DistinguishesUnknownAbsentRowFromUncoveredAndNotApplicableValues()
    {
        var observation = ObservationFixture.Read("observation-day-1.json");
        var emptyScopedStock = observation with
        {
            CityMarketStock = observation.CityMarketStock with { Rows = ImmutableArray<StockRow>.Empty }
        };

        var missingRow = WorldInspector.GetStockFact(emptyScopedStock, "city-1", "wheat");
        var otherCity = WorldInspector.GetStockFact(observation, "city-2", "wheat");
        var hexFacts = WorldInspector.BuildFacts(observation, new AtlasEntityKey(AtlasEntityKind.Hex, "hex-1"));
        var cityFacts = WorldInspector.BuildFacts(observation, new AtlasEntityKey(AtlasEntityKind.City, "city-2"));

        Assert.That(missingRow.Availability, Is.EqualTo(FactAvailability.Unknown));
        Assert.That(missingRow.Value, Is.Null);
        Assert.That(missingRow.DisplayValue, Does.Contain("not the same as quantity zero"));
        Assert.That(otherCity.Availability, Is.EqualTo(FactAvailability.NotCovered));
        Assert.That(hexFacts.Single(fact => fact.Name == "City market stock").Availability,
            Is.EqualTo(FactAvailability.NotApplicable));
        Assert.That(cityFacts.Single(fact => fact.Name == "P8 Location").Availability,
            Is.EqualTo(FactAvailability.Unknown));
    }

    [Test]
    public void InvalidFileLoadRetainsLastGoodObservation()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = WorldAtlas.Desktop.ViewModels.MainWindowViewModel.LoadBundledDemo(fixtureDirectory);
        var before = viewModel.CurrentObservation;
        var badFile = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"invalid-{Guid.NewGuid():N}.json");
        File.WriteAllText(badFile, "{ not json }");
        try
        {
            var wasLoaded = viewModel.LoadObservationFileAsync(badFile).GetAwaiter().GetResult();
            Assert.That(wasLoaded, Is.False);
            Assert.That(viewModel.CurrentObservation.RawDigest, Is.EqualTo(before.RawDigest));
            Assert.That(viewModel.StatusMessage, Does.Contain("last good observation remains active"));
        }
        finally
        {
            File.Delete(badFile);
        }
    }

    private void AssertRejected(ObservationDocument before, ObservationDocument after, string message)
    {
        var result = _comparer.Compare(before, after);
        Assert.That(result.IsComparable, Is.False);
        Assert.That(result.Summary, Does.Contain(message));
        Assert.That(result.Differences, Is.Empty);
    }
}
