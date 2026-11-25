using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;
using WorldAtlas.Desktop.Settings;
using WorldAtlas.Desktop.ViewModels;

namespace WorldAtlas.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow() : this(LoadBundledViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        AtlasTypography.Apply(viewModel.UiScalePercent);
        MapView.HexSelected += viewModel.SelectMapHex;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.NavigationKind))
            {
                RefreshNavigationTabs();
            }
            else if (args.PropertyName == nameof(MainWindowViewModel.UiScalePercent))
            {
                AtlasTypography.Apply(viewModel.UiScalePercent);
            }
        };
        RefreshNavigationTabs();
    }

    private static MainWindowViewModel LoadBundledViewModel()
    {
        var fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "fixtures", "demo");
        return MainWindowViewModel.LoadBundledDemo(fixtureDirectory);
    }

    private void OnWorldClick(object? sender, RoutedEventArgs e) => _viewModel.SetNavigationKind(AtlasEntityKind.World);

    private void OnHexesClick(object? sender, RoutedEventArgs e) => _viewModel.SetNavigationKind(AtlasEntityKind.Hex);

    private void OnLocationsClick(object? sender, RoutedEventArgs e) => _viewModel.SetNavigationKind(AtlasEntityKind.Location);

    private void OnCitiesClick(object? sender, RoutedEventArgs e) => _viewModel.SetNavigationKind(AtlasEntityKind.City);

    private void OnEntitySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.OfType<NavigationItem>().FirstOrDefault() is { } item)
        {
            _viewModel.SelectEntity(item.Key);
        }
    }

    private void OnCompareClick(object? sender, RoutedEventArgs e) => _viewModel.CompareSelectedObservations();

    private void OnComparisonEntryClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is ComparisonDifferenceViewModel row)
        {
            _viewModel.SelectComparisonEntity(row);
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open an additional validated Atlas observation",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Atlas observation JSON")
                    {
                        Patterns = ["*.atlas-observation.json", "*.json"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            var path = files[0].TryGetLocalPath();
            if (path is not null)
            {
                await _viewModel.LoadObservationFileAsync(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await ShowNoticeAsync("File open failed", exception.Message);
        }
    }

    private void OnFitClick(object? sender, RoutedEventArgs e) => MapView.FitToView();

    private void OnZoomInClick(object? sender, RoutedEventArgs e) => MapView.ZoomFromCenter(1.2d);

    private void OnZoomOutClick(object? sender, RoutedEventArgs e) => MapView.ZoomFromCenter(1d / 1.2d);

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialogWindow(
            "Standalone offline Demo viewer. Explore the bundled world, select Hexes, Locations, or Cities, inspect the selected record, and compare compatible observations. This shell has no Core authority or Unity connection.",
            _viewModel.UiScalePercent);
        await dialog.ShowDialog(this);
    }

    private async Task ShowNoticeAsync(string title, string message)
    {
        var dialog = new AboutDialogWindow(message) { Title = title };
        await dialog.ShowDialog(this);
    }

    private void RefreshNavigationTabs()
    {
        WorldNav.IsChecked = _viewModel.NavigationKind == AtlasEntityKind.World;
        HexNav.IsChecked = _viewModel.NavigationKind == AtlasEntityKind.Hex;
        LocationNav.IsChecked = _viewModel.NavigationKind == AtlasEntityKind.Location;
        CityNav.IsChecked = _viewModel.NavigationKind == AtlasEntityKind.City;
    }
}
