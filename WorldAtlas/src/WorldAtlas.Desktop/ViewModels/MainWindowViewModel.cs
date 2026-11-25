using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WorldAtlas.Observations.Comparison;
using WorldAtlas.Observations.Geometry;
using WorldAtlas.Observations.Models;
using WorldAtlas.Observations.Navigation;
using WorldAtlas.Observations.Parsing;

namespace WorldAtlas.Desktop.ViewModels;

public sealed record ObservationChoice(string Label, ObservationDocument Observation);

public sealed record InspectorFactViewModel(string Name, string DisplayValue, string AvailabilityLabel);

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ObservationFileReader _reader;
    private readonly ObservationComparer _comparer = new();
    private readonly ObservableCollection<ObservationChoice> _observationChoices = [];
    private readonly ObservableCollection<NavigationItem> _entityItems = [];
    private ObservationChoice _beforeChoice;
    private ObservationChoice _afterChoice;
    private AtlasEntityKind _navigationKind = AtlasEntityKind.Hex;
    private NavigationItem? _selectedNavigationItem;
    private AtlasEntityKey? _selectedEntity;
    private string? _selectedHexId;
    private string _statusMessage = "Offline Demo fixtures are ready.";
    private ObservationComparison? _comparison;

    private MainWindowViewModel(ObservationFileReader reader, ObservationChoice beforeChoice, ObservationChoice afterChoice)
    {
        _reader = reader;
        _beforeChoice = beforeChoice;
        _afterChoice = afterChoice;
        _observationChoices.Add(beforeChoice);
        if (!StringComparer.Ordinal.Equals(beforeChoice.Observation.RawDigest, afterChoice.Observation.RawDigest))
        {
            _observationChoices.Add(afterChoice);
        }

        RefreshEntities();
        SelectDefaultEntity();
        CompareSelectedObservations();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ObservationChoice> ObservationChoices => _observationChoices;

    public ObservableCollection<NavigationItem> EntityItems => _entityItems;

    public AtlasEntityKind NavigationKind => _navigationKind;

    public ImmutableArray<HexFact> MapHexes => CurrentObservation.Geography.Hexes;

    public ImmutableArray<MapMarker> MapMarkers => MapMarkerFactory.Build(CurrentObservation);

    public ObservationDocument CurrentObservation => _beforeChoice.Observation;

    public int EntityCount => _entityItems.Count;

    public IEnumerable<InspectorFactViewModel> InspectorFacts => _selectedEntity is { } key
        ? WorldInspector.BuildFacts(CurrentObservation, key).Select(fact => new InspectorFactViewModel(
            fact.Name, fact.DisplayValue, AvailabilityLabel(fact.Availability)))
        : [];

    public ImmutableArray<ObservationDifference> Differences => _comparison?.Differences ?? ImmutableArray<ObservationDifference>.Empty;

    public string ComparisonHeadline => _comparison switch
    {
        null => "Select two observations",
        { IsComparable: false } => "Not comparable",
        { Differences.IsEmpty: true } => "No value difference",
        { Differences.Length: 1 } => "1 observed value difference",
        _ => $"{_comparison.Differences.Length} observed value differences"
    };

    public string ComparisonScope => _comparison?.ScopeDescription ?? "Same Demo run and named capability scope required.";

    public string ComparisonSummary => _comparison?.Summary ?? "Choose the recorded Demo boundaries to compare.";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ViewTitle => _navigationKind switch
    {
        AtlasEntityKind.World => "World overview",
        AtlasEntityKind.Hex => "Hex map",
        AtlasEntityKind.Location => "Locations",
        AtlasEntityKind.City => "Cities",
        _ => "World Atlas"
    };

    public string ViewSubtitle =>
        $"{CurrentObservation.WorldId}  ·  Day {CurrentObservation.Boundary.AbsoluteDay}  ·  completed sequence {CurrentObservation.Boundary.CompletedSequence}  ·  {StatusMessage}";

    public string MapFooter
    {
        get
        {
            var placed = CurrentObservation.PlacedHexes.Length;
            var unplaced = CurrentObservation.UnplacedHexes.Length;
            var placement = unplaced == 0 ? $"{placed} placed Hexes" : $"{placed} placed · {unplaced} unplaced Hex in list";
            return $"{placement} · arrow keys navigate visible Hexes";
        }
    }

    public string SelectedEntityTitle => _selectedEntity is { } key
        ? key.Kind switch
        {
            AtlasEntityKind.World => "Demo World",
            AtlasEntityKind.Hex => $"Hex {key.SourceId}",
            AtlasEntityKind.Location => $"Location {key.SourceId}",
            AtlasEntityKind.City => WorldNavigator.GetCity(CurrentObservation, key.SourceId)?.Label is { Length: > 0 } cityLabel
                ? cityLabel
                : $"City {key.SourceId}",
            _ => key.SourceId
        }
        : "No selection";

    public string SelectedEntityKindLabel => _selectedEntity?.Kind.ToString().ToUpperInvariant() ?? "Select an entity";

    public string SelectedEntityIdentity => _selectedEntity is { } key
        ? $"{CurrentObservation.WorldId} · {key.Kind.ToString().ToLowerInvariant()} · {key.SourceId}"
        : "Select a world entity to inspect.";

    public string? SelectedHexId
    {
        get => _selectedHexId;
        private set
        {
            if (SetProperty(ref _selectedHexId, value))
            {
                OnPropertyChanged(nameof(MapMarkers));
            }
        }
    }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set => SetProperty(ref _selectedNavigationItem, value);
    }

    public ObservationChoice BeforeChoice
    {
        get => _beforeChoice;
        set
        {
            if (SetProperty(ref _beforeChoice, value))
            {
                OnPropertyChanged(nameof(CurrentObservation));
                OnPropertyChanged(nameof(MapHexes));
                OnPropertyChanged(nameof(MapMarkers));
                OnPropertyChanged(nameof(ViewSubtitle));
                OnPropertyChanged(nameof(MapFooter));
                RefreshEntities();
                if (_selectedEntity is { } selected && !EntityExists(CurrentObservation, selected))
                {
                    SelectDefaultEntity();
                }
                else if (_selectedEntity is { } current)
                {
                    SelectEntity(current);
                }
            }
        }
    }

    public ObservationChoice AfterChoice
    {
        get => _afterChoice;
        set => SetProperty(ref _afterChoice, value);
    }

    public static MainWindowViewModel LoadBundledDemo(string fixtureDirectory)
    {
        var reader = new ObservationFileReader();
        var firstPath = Path.Combine(fixtureDirectory, "observation-day-1.json");
        var secondPath = Path.Combine(fixtureDirectory, "observation-day-2.json");
        var first = reader.Read(File.ReadAllBytes(firstPath), firstPath);
        var second = reader.Read(File.ReadAllBytes(secondPath), secondPath);
        return new MainWindowViewModel(reader,
            new ObservationChoice("Day 1 · sequence 1", first),
            new ObservationChoice("Day 2 · sequence 2", second));
    }

    public void SetNavigationKind(AtlasEntityKind kind)
    {
        if (_navigationKind == kind)
        {
            return;
        }

        _navigationKind = kind;
        OnPropertyChanged(nameof(NavigationKind));
        OnPropertyChanged(nameof(ViewTitle));
        RefreshEntities();
        var next = _entityItems.FirstOrDefault();
        if (next is not null)
        {
            SelectEntity(next.Key);
        }
        else if (kind == AtlasEntityKind.World)
        {
            SelectEntity(new AtlasEntityKey(AtlasEntityKind.World, CurrentObservation.WorldId));
        }
    }

    public void SelectEntity(AtlasEntityKey key)
    {
        _selectedEntity = key;
        if (_navigationKind != key.Kind)
        {
            _navigationKind = key.Kind;
            OnPropertyChanged(nameof(NavigationKind));
            OnPropertyChanged(nameof(ViewTitle));
            RefreshEntities();
        }

        SelectedNavigationItem = _entityItems.FirstOrDefault(item => item.Key == key);
        SelectedHexId = WorldNavigator.ResolveAnchorHexId(CurrentObservation, key);
        OnPropertyChanged(nameof(SelectedEntityTitle));
        OnPropertyChanged(nameof(SelectedEntityKindLabel));
        OnPropertyChanged(nameof(SelectedEntityIdentity));
        OnPropertyChanged(nameof(InspectorFacts));
        OnPropertyChanged(nameof(ViewSubtitle));
    }

    public void SelectMapHex(string hexId) => SelectEntity(new AtlasEntityKey(AtlasEntityKind.Hex, hexId));

    public async Task<bool> LoadObservationFileAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var observation = await _reader.ReadFileAsync(path, cancellationToken).ConfigureAwait(true);
            var choice = new ObservationChoice($"Opened · {Path.GetFileName(path)}", observation);
            _observationChoices.Add(choice);
            BeforeChoice = choice;
            StatusMessage = $"Opened validated Demo observation · {Path.GetFileName(path)}";
            CompareSelectedObservations();
            OnPropertyChanged(nameof(ViewSubtitle));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObservationValidationException)
        {
            StatusMessage = $"Load failed; last good observation remains active. {exception.Message}";
            OnPropertyChanged(nameof(ViewSubtitle));
            return false;
        }
    }

    public ObservationComparison CompareSelectedObservations()
    {
        _comparison = _comparer.Compare(BeforeChoice.Observation, AfterChoice.Observation);
        OnPropertyChanged(nameof(Differences));
        OnPropertyChanged(nameof(ComparisonHeadline));
        OnPropertyChanged(nameof(ComparisonScope));
        OnPropertyChanged(nameof(ComparisonSummary));
        return _comparison;
    }

    private void RefreshEntities()
    {
        _entityItems.Clear();
        foreach (var item in WorldNavigator.List(CurrentObservation, _navigationKind))
        {
            _entityItems.Add(item);
        }

        OnPropertyChanged(nameof(EntityItems));
        OnPropertyChanged(nameof(EntityCount));
    }

    private void SelectDefaultEntity()
    {
        if (_navigationKind == AtlasEntityKind.World)
        {
            SelectEntity(new AtlasEntityKey(AtlasEntityKind.World, CurrentObservation.WorldId));
            return;
        }

        var first = _entityItems.FirstOrDefault();
        if (first is not null)
        {
            SelectEntity(first.Key);
        }
        else
        {
            SelectEntity(new AtlasEntityKey(AtlasEntityKind.World, CurrentObservation.WorldId));
        }
    }

    private static bool EntityExists(ObservationDocument observation, AtlasEntityKey key) =>
        WorldNavigator.List(observation, key.Kind).Any(item => item.Key == key);

    private static string AvailabilityLabel(FactAvailability availability) => availability switch
    {
        FactAvailability.Known => "KNOWN",
        FactAvailability.Unknown => "UNKNOWN",
        FactAvailability.NotCovered => "NOT COVERED",
        FactAvailability.NotApplicable => "NOT APPLICABLE",
        _ => "UNKNOWN"
    };

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
