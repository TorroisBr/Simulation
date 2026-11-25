using System.Xml.Linq;
using WorldAtlas.Desktop.ViewModels;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class NavigationAndBoundaryTests
{
    [Test]
    public void NavigationExposesWorldHexesLocationsAndAllCitiesInStableIdentityOrder()
    {
        var observation = ObservationFixture.Read("observation-day-1.json");
        var world = WorldNavigator.List(observation, AtlasEntityKind.World);
        var hexes = WorldNavigator.List(observation, AtlasEntityKind.Hex);
        var locations = WorldNavigator.List(observation, AtlasEntityKind.Location);
        var cities = WorldNavigator.List(observation, AtlasEntityKind.City);

        Assert.That(world, Has.Length.EqualTo(1));
        Assert.That(hexes.Select(item => item.Key.SourceId), Is.EqualTo(new[] { "hex-1", "hex-2" }));
        Assert.That(locations.Select(item => item.Key.SourceId), Is.EqualTo(new[] { "location-1" }));
        Assert.That(cities.Select(item => item.Key.SourceId), Is.EqualTo(new[] { "city-1", "city-2" }));
        Assert.That(cities.Single(item => item.Key.SourceId == "city-2").Detail, Is.EqualTo("No binding recorded"));
        Assert.That(WorldNavigator.ResolveAnchorHexId(observation,
            new AtlasEntityKey(AtlasEntityKind.City, "city-2")), Is.Null);
    }

    [Test]
    public void DesktopAndObservationAssembliesHaveNoCoreUnityOrNetworkReferences()
    {
        var assemblies = new[]
        {
            typeof(MainWindowViewModel).Assembly,
            typeof(ObservationDocument).Assembly
        };
        var forbidden = assemblies.SelectMany(assembly => assembly.GetReferencedAssemblies())
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Simulation.Core", StringComparison.OrdinalIgnoreCase) ||
                           name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Http", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("WebSocket", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.That(forbidden, Is.Empty);
        Assert.That(typeof(ObservationDocument).GetProperty(nameof(ObservationDocument.Geography))!.PropertyType.Name,
            Is.EqualTo(nameof(GeographyCapability)));
        Assert.That(typeof(GeographyCapability).GetProperty(nameof(GeographyCapability.Hexes))!.PropertyType.Name,
            Does.Contain("ImmutableArray"));
    }

    [Test]
    public void BundleAndProjectBoundaryOnlyIncludeAtlasOwnedProjects()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var desktopProject = XDocument.Load(Path.Combine(testDirectory, "boundary", "WorldAtlas.Desktop.csproj"));
        var observationsProject = XDocument.Load(Path.Combine(testDirectory, "boundary", "WorldAtlas.Observations.csproj"));
        var references = desktopProject.Descendants("ProjectReference").Select(element => (string?)element.Attribute("Include")).ToArray();

        Assert.That(references, Has.Length.EqualTo(1));
        Assert.That(references[0], Does.Contain("WorldAtlas.Observations"));
        Assert.That(references.Any(reference => reference!.Contains("Simulation", StringComparison.OrdinalIgnoreCase) ||
                                               reference.Contains("Unity", StringComparison.OrdinalIgnoreCase)), Is.False);
        Assert.That(observationsProject.Descendants("PackageReference"), Is.Empty);
    }
}
