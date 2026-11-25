using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Headless.NUnit;
using WorldAtlas.Desktop;
using WorldAtlas.Desktop.Controls;
using WorldAtlas.Desktop.ViewModels;
using WorldAtlas.Observations.Geometry;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class HeadlessInteractionTests
{
    [AvaloniaTest]
    public void ClickingMapSelectsHexAndSynchronizesInspectorWhileCityListKeepsUnboundCityAccessible()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory);
        var window = new MainWindow(viewModel) { Width = 1280, Height = 800 };
        window.Show();
        window.UpdateLayout();

        var map = window.FindControl<HexMapView>("MapView")!;
        var localPoint = map.PointForHex("hex-2");
        Assert.That(double.IsFinite(localPoint.X) && double.IsFinite(localPoint.Y), Is.True);
        var windowPoint = map.TranslatePoint(localPoint, window) ?? throw new InvalidOperationException("Map point could not be translated.");
        window.MouseDown(windowPoint, MouseButton.Left);
        window.MouseUp(windowPoint, MouseButton.Left);

        Assert.That(viewModel.SelectedEntityKindLabel, Is.EqualTo("HEX"));
        Assert.That(viewModel.SelectedEntityIdentity, Does.EndWith("hex-2"));
        Assert.That(viewModel.InspectorFacts.Single(fact => fact.Name == "Axial coordinates").DisplayValue,
            Is.EqualTo("q=0, r=0"));
        Assert.That(map.SelectedHexId, Is.EqualTo("hex-2"));

        viewModel.SetNavigationKind(AtlasEntityKind.City);
        window.UpdateLayout();
        var entityList = window.FindControl<ListBox>("EntityList")!;
        entityList.SelectedItem = viewModel.EntityItems.Single(item => item.Key.SourceId == "city-2");
        window.UpdateLayout();

        Assert.That(viewModel.SelectedEntityTitle, Is.EqualTo("Unbound Town"));
        Assert.That(viewModel.SelectedHexId, Is.Null);
        Assert.That(viewModel.InspectorFacts.Single(fact => fact.Name == "P8 Location").AvailabilityLabel,
            Is.EqualTo("UNKNOWN"));
        Assert.That(viewModel.InspectorFacts.Single(fact => fact.Name == "Stock quantity").AvailabilityLabel,
            Is.EqualTo("NOT COVERED"));
    }

    [AvaloniaTest]
    public void FitAndZoomButtonsKeepMapTransformInsideDeclaredLimits()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory);
        var window = new MainWindow(viewModel) { Width = 1280, Height = 800 };
        window.Show();
        window.UpdateLayout();
        var map = window.FindControl<HexMapView>("MapView")!;

        map.ZoomFromCenter(2d);
        Assert.That(map.Zoom, Is.GreaterThan(HexProjection.MinimumZoom));
        map.FitToView();
        Assert.That(map.Zoom, Is.InRange(HexProjection.MinimumZoom, HexProjection.MaximumZoom));
        Assert.That(map.Pan, Is.EqualTo(new WorldAtlas.Observations.Geometry.MapPoint(0d, 0d)));
    }

    [AvaloniaTest]
    public void ArrowKeysMoveSelectionBetweenVisibleHexesInScreenDirection()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory);
        var window = new MainWindow(viewModel) { Width = 1280, Height = 800 };
        window.Show();
        window.UpdateLayout();

        var map = window.FindControl<HexMapView>("MapView")!;
        var firstPoint = map.PointForHex("hex-1");
        var windowPoint = map.TranslatePoint(firstPoint, window)
            ?? throw new InvalidOperationException("Map point could not be translated.");
        window.MouseDown(windowPoint, MouseButton.Left);
        window.MouseUp(windowPoint, MouseButton.Left);
        Assert.That(viewModel.SelectedEntityIdentity, Does.EndWith("hex-1"));

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.That(viewModel.SelectedEntityIdentity, Does.EndWith("hex-2"));
        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Assert.That(viewModel.SelectedEntityIdentity, Does.EndWith("hex-1"));
        Assert.That(viewModel.MapFooter, Does.Contain("arrow keys navigate visible Hexes"));
    }
}
