using WorldAtlas.Desktop.ViewModels;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class MapMarkerFactoryTests
{
    [Test]
    public void MarkersUseStableOffsetsScopedToTheirAnchorHex()
    {
        var source = ObservationFixture.Read("observation-day-1.json");
        var observation = source with
        {
            Geography = source.Geography with
            {
                Locations = source.Geography.Locations.Add(new LocationFact("location-2", "hex-2"))
            }
        };

        var markers = MapMarkerFactory.Build(observation);

        Assert.That(markers.Length, Is.EqualTo(3));
        Assert.That(markers.Single(marker => marker.Id == "location-1").OffsetOrder, Is.Zero);
        Assert.That(markers.Single(marker => marker.Id == "city-1").OffsetOrder, Is.EqualTo(1));
        Assert.That(markers.Single(marker => marker.Id == "location-2").OffsetOrder, Is.Zero);
    }
}
