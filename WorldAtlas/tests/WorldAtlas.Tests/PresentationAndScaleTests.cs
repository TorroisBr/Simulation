using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using WorldAtlas.Desktop;
using WorldAtlas.Desktop.Settings;
using WorldAtlas.Desktop.ViewModels;
using WorldAtlas.Observations.Models;

namespace WorldAtlas.Tests;

[TestFixture]
public sealed class PresentationAndScaleTests
{
    [AvaloniaTest]
    public void MainWindowAutomaticallyLoadsBundledDemoWithoutFilePicker()
    {
        var window = new MainWindow();
        window.Show();
        window.UpdateLayout();

        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            Assert.That(viewModel.ObservationChoices, Has.Count.EqualTo(2));
            Assert.That(viewModel.CurrentObservation.WorldId, Is.EqualTo("demo:world"));
            Assert.That(viewModel.CurrentObservation.Boundary.AbsoluteDay, Is.EqualTo(1));
            Assert.That(viewModel.ComparisonRows.Single().DeltaText, Is.EqualTo("+5"));
            Assert.That(window.FindControl<Button>("OpenObservationButton"), Is.Not.Null);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void BundledDemoProvidesHumanReadableChoicesAndObservedStockDelta()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory, CreateSettingsStore());

        Assert.That(viewModel.ObservationChoices.Select(choice => choice.Label),
            Is.EqualTo(new[] { "Day 1 · sequence 1", "Day 2 · sequence 2" }));
        Assert.That(viewModel.BeforeChoice.Label, Does.StartWith("Day 1"));
        Assert.That(viewModel.AfterChoice.Label, Does.StartWith("Day 2"));
        Assert.That(viewModel.ComparisonPeriod, Is.EqualTo("Day 1 · sequence 1 → Day 2 · sequence 2"));

        var row = viewModel.ComparisonRows.Single();
        Assert.That(row.Label, Is.EqualTo("City city-1 — Wheat"));
        Assert.That(row.BeforeText, Is.EqualTo("Day 1 = 20"));
        Assert.That(row.AfterText, Is.EqualTo("Day 2 = 25"));
        Assert.That(row.DeltaText, Is.EqualTo("+5"));
        Assert.That(row.AvailabilityText, Does.Contain("causal events are not covered"));
    }

    [Test]
    public void ComparisonEntrySelectsItsCityAndChangingBoundaryRecomputesComparison()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory, CreateSettingsStore());

        viewModel.SelectComparisonEntity(viewModel.ComparisonRows.Single());
        Assert.That(viewModel.SelectedEntityIdentity, Does.EndWith("city-1"));
        Assert.That(viewModel.SelectedEntityKindLabel, Is.EqualTo("CITY"));

        viewModel.AfterChoice = viewModel.ObservationChoices[0];
        Assert.That(viewModel.ComparisonHeadline, Is.EqualTo("Not comparable"));
        Assert.That(viewModel.ComparisonSummary, Does.Contain("not adjacent"));
        Assert.That(viewModel.ComparisonRows, Is.Empty);
    }

    [AvaloniaTest]
    public void ClickingVisibleComparisonRowSynchronizesCitySelection()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory, CreateSettingsStore());
        var window = new MainWindow(viewModel) { Width = 1440, Height = 960 };
        window.Show();
        window.UpdateLayout();

        try
        {
            var entry = window.GetVisualDescendants().OfType<Button>()
                .First(button => button.DataContext is ComparisonDifferenceViewModel);
            var localPoint = new Point(entry.Bounds.Width / 2, entry.Bounds.Height / 2);
            var windowPoint = entry.TranslatePoint(localPoint, window)
                ?? throw new InvalidOperationException("Comparison row point could not be translated.");

            window.MouseDown(windowPoint, MouseButton.Left);
            window.MouseUp(windowPoint, MouseButton.Left);

            Assert.That(viewModel.SelectedEntityKindLabel, Is.EqualTo("CITY"));
            Assert.That(viewModel.SelectedEntityIdentity, Does.EndWith("city-1"));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void ScalePreferenceSupportsOnlyTheFourUserChoicesAndPersistsOutsideObservationFiles()
    {
        var store = CreateSettingsStore();
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory, store);

        Assert.That(viewModel.UiScalePercent, Is.EqualTo(100));
        Assert.That(viewModel.UiScaleOptions.Select(option => option.Percentage), Is.EqualTo(new[] { 100, 125, 150, 175 }));
        viewModel.UiScaleChoice = viewModel.UiScaleOptions.Single(option => option.Percentage == 150);

        Assert.That(store.Load(), Is.EqualTo(150));
        Assert.That(File.ReadAllText(store.SettingsPath), Does.Contain("UiScalePercent"));
        Assert.That(File.ReadAllText(store.SettingsPath), Does.Not.Contain("observation-day"));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Save(200));
    }

    [Test]
    public void ManualFileFallbackLoadsThroughTheValidatedReader()
    {
        var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
        var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory, CreateSettingsStore());
        var secondObservation = Path.Combine(fixtureDirectory, "observation-day-2.json");

        var loaded = viewModel.LoadObservationFileAsync(secondObservation).GetAwaiter().GetResult();

        Assert.That(loaded, Is.True);
        Assert.That(viewModel.CurrentObservation.Boundary, Is.EqualTo(new ObservationBoundary(2, 2)));
        Assert.That(viewModel.BeforeChoice.Label, Is.EqualTo("Day 2 · sequence 2 · opened"));
        Assert.That(viewModel.StatusMessage, Does.Contain("Opened validated Demo observation"));
    }

    [AvaloniaTest]
    public void MainLayoutKeepsComparisonAndToolbarVisibleAcrossSupportedScales()
    {
        // The 1280x720 and 1097x617 cases approximate logical client sizes at
        // 150% and 175% Windows display scaling on a 1920x1080 monitor.
        foreach (var (width, height) in new[]
                 {
                     (1920d, 1080d), (3840d, 2160d), (1280d, 720d), (2560d, 1440d), (1097d, 617d)
                 })
        {
            var fixtureDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "demo");
            var viewModel = MainWindowViewModel.LoadBundledDemo(fixtureDirectory, CreateSettingsStore());
            var window = new MainWindow(viewModel) { Width = width, Height = height };
            window.Show();
            window.UpdateLayout();

            try
            {
                var beforeCombo = window.FindControl<ComboBox>("BeforeCombo")!;
                var afterCombo = window.FindControl<ComboBox>("AfterCombo")!;
                var scaleCombo = window.FindControl<ComboBox>("UiScaleCombo")!;
                var openButton = window.FindControl<Button>("OpenObservationButton")!;
                var comparisonPanel = window.FindControl<Border>("ComparisonPanel")!;
                var map = window.FindControl<WorldAtlas.Desktop.Controls.HexMapView>("MapView")!;

                foreach (var option in viewModel.UiScaleOptions)
                {
                    scaleCombo.SelectedItem = option;
                    window.UpdateLayout();

                    Assert.That(viewModel.UiScaleChoice, Is.EqualTo(option));
                    Assert.That(beforeCombo.ItemTemplate, Is.Not.Null);
                    Assert.That(afterCombo.ItemTemplate, Is.Not.Null);
                    Assert.That(comparisonPanel.Bounds.Height, Is.GreaterThanOrEqualTo(220));
                    Assert.That(map.Bounds.Width, Is.GreaterThan(0));
                    Assert.That(ToWindowRect(beforeCombo, window).Right, Is.LessThanOrEqualTo(window.Bounds.Width + 1));
                    Assert.That(ToWindowRect(afterCombo, window).Right, Is.LessThanOrEqualTo(window.Bounds.Width + 1));
                    Assert.That(ToWindowRect(scaleCombo, window).Right, Is.LessThanOrEqualTo(window.Bounds.Width + 1));
                    Assert.That(ToWindowRect(openButton, window).Right, Is.LessThanOrEqualTo(window.Bounds.Width + 1));
                }

                Assert.That(scaleCombo.FontSize, Is.EqualTo(24.5d).Within(0.1d));
            }
            finally
            {
                window.Close();
            }
        }
    }

    [AvaloniaTest]
    public void AboutWindowKeepsFixedBoundsWhenCloseButtonIsHoveredAtLargeScale()
    {
        var owner = new Window { Width = 1000, Height = 700 };
        owner.Show();
        var dialog = new AboutDialogWindow(new string('A', 1000), 175);
        dialog.Show(owner);
        dialog.UpdateLayout();

        try
        {
            var closeButton = dialog.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Name == "AboutCloseButton");
            var originalBounds = dialog.Bounds;
            var point = closeButton.TranslatePoint(
                new Point(closeButton.Bounds.Width / 2, closeButton.Bounds.Height / 2), dialog)
                ?? throw new InvalidOperationException("Close button location could not be translated.");

            dialog.MouseMove(point);
            dialog.UpdateLayout();

            Assert.That(dialog.SizeToContent, Is.EqualTo(SizeToContent.Manual));
            Assert.That(dialog.CanResize, Is.False);
            Assert.That(dialog.Bounds, Is.EqualTo(originalBounds));
        }
        finally
        {
            dialog.Close();
            owner.Close();
        }
    }

    private static Rect ToWindowRect(Control control, Window window)
    {
        var origin = control.TranslatePoint(new Point(0, 0), window)
            ?? throw new InvalidOperationException("Control origin could not be translated.");
        return new Rect(origin, control.Bounds.Size);
    }

    private static UiScalePreferenceStore CreateSettingsStore()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"atlas-settings-{Guid.NewGuid():N}", "settings.json");
        return new UiScalePreferenceStore(path);
    }
}
