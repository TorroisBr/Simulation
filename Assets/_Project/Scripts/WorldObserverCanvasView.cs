using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class WorldObserverWorldNodeView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string runtimeId;
    [SerializeField] private string selectionRuntimeId;
    [SerializeField] private bool siteNode;

    public string RuntimeId => runtimeId;
    public string SelectionRuntimeId => selectionRuntimeId;
    public bool IsSiteNode => siteNode;
    public Button Button => button;
    public TMP_Text Label => label;
    public Vector2 AnchoredPosition => ((RectTransform)transform).anchoredPosition;

    public void Bind(
        string visualRuntimeId,
        string selectableRuntimeId,
        string displayName,
        bool isSite,
        Vector2 position,
        Action<string> onSelected)
    {
        EnsureVisuals();
        runtimeId = visualRuntimeId;
        selectionRuntimeId = selectableRuntimeId;
        siteNode = isSite;
        name = isSite ? "SiteNode_" + visualRuntimeId : "WorldNode_" + visualRuntimeId;
        label.text = displayName;
        background.color = isSite
            ? new Color(0.28f, 0.20f, 0.42f, 0.98f)
            : new Color(0.12f, 0.32f, 0.42f, 0.98f);
        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = isSite ? new Vector2(126f, 38f) : new Vector2(156f, 54f);
        button.onClick.RemoveAllListeners();
        if (string.IsNullOrWhiteSpace(selectableRuntimeId) == false && onSelected != null)
        {
            button.onClick.AddListener(() => onSelected(selectableRuntimeId));
        }
    }

    private void EnsureVisuals()
    {
        background = background != null ? background : gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        button = button != null ? button : gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        if (label == null)
        {
            label = WorldObserverUiFactory.CreateText(transform, "Label", 15f, TextAlignmentOptions.Center);
            WorldObserverUiFactory.Stretch(label.rectTransform, 6f, 4f, 6f, 4f);
        }
    }
}

public sealed class WorldObserverRouteView : MonoBehaviour
{
    [SerializeField] private Image line;
    [SerializeField] private string routeRuntimeId;
    [SerializeField] private string originRuntimeId;
    [SerializeField] private string destinationRuntimeId;

    public string RouteRuntimeId => routeRuntimeId;
    public string OriginRuntimeId => originRuntimeId;
    public string DestinationRuntimeId => destinationRuntimeId;

    public void Bind(WorldObserverRouteReadModel route, Vector2 origin, Vector2 destination)
    {
        line = line != null ? line : gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        line.color = new Color(0.35f, 0.45f, 0.52f, 0.8f);
        routeRuntimeId = route.RuntimeId;
        originRuntimeId = route.OriginLocationRuntimeId;
        destinationRuntimeId = route.DestinationLocationRuntimeId;
        name = "Route_" + routeRuntimeId;
        WorldObserverUiFactory.PlaceLine((RectTransform)transform, origin, destination, 5f);
    }
}

public sealed class WorldObserverTravelerMarkerView : MonoBehaviour
{
    [SerializeField] private Image marker;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string travelerRuntimeId;
    [SerializeField] private string routeRuntimeId;
    [SerializeField] private float progress01;

    public string TravelerRuntimeId => travelerRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;
    public float Progress01 => progress01;
    public Vector2 AnchoredPosition => ((RectTransform)transform).anchoredPosition;

    public void Bind(
        WorldObserverTravelerReadModel traveler,
        Vector2 origin,
        Vector2 destination,
        Vector2 deterministicOffset)
    {
        EnsureVisuals();
        travelerRuntimeId = traveler.RuntimeId;
        routeRuntimeId = traveler.RouteRuntimeId;
        progress01 = Mathf.Clamp01(traveler.Progress01);
        label.text = traveler.DisplayName;
        name = "Traveler_" + travelerRuntimeId;
        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(112f, 28f);
        rect.anchoredPosition = Vector2.Lerp(origin, destination, progress01) + deterministicOffset;
    }

    private void EnsureVisuals()
    {
        marker = marker != null ? marker : gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        marker.color = new Color(0.94f, 0.66f, 0.16f, 1f);
        if (label == null)
        {
            label = WorldObserverUiFactory.CreateText(transform, "Label", 12f, TextAlignmentOptions.Center);
            label.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            WorldObserverUiFactory.Stretch(label.rectTransform, 4f, 2f, 4f, 2f);
        }
    }
}

public sealed class WorldObserverTopologyNodeView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string runtimeId;
    [SerializeField] private int depth;

    public string RuntimeId => runtimeId;
    public int Depth => depth;
    public Button Button => button;
    public Vector2 AnchoredPosition => ((RectTransform)transform).anchoredPosition;

    public void Bind(WorldObserverTopologyNodeReadModel node, Vector2 position, Action<string> onSelected)
    {
        EnsureVisuals();
        runtimeId = node.RuntimeId;
        depth = node.Depth;
        name = "LocalNode_" + runtimeId;
        label.text = node.DisplayName + (node.IsEntryPoint ? "  ◇" : string.Empty);
        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(142f, 36f);
        button.onClick.RemoveAllListeners();
        if (onSelected != null)
        {
            button.onClick.AddListener(() => onSelected(runtimeId));
        }
    }

    private void EnsureVisuals()
    {
        background = background != null ? background : gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.20f, 0.38f, 0.28f, 0.98f);
        button = button != null ? button : gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        if (label == null)
        {
            label = WorldObserverUiFactory.CreateText(transform, "Label", 13f, TextAlignmentOptions.Center);
            WorldObserverUiFactory.Stretch(label.rectTransform, 4f, 2f, 4f, 2f);
        }
    }
}

public enum WorldObserverTopologyEdgeKind
{
    Hierarchy,
    Connection
}

public sealed class WorldObserverTopologyEdgeView : MonoBehaviour
{
    [SerializeField] private Image line;
    [SerializeField] private WorldObserverTopologyEdgeKind edgeKind;
    [SerializeField] private string originRuntimeId;
    [SerializeField] private string destinationRuntimeId;

    public WorldObserverTopologyEdgeKind EdgeKind => edgeKind;
    public string OriginRuntimeId => originRuntimeId;
    public string DestinationRuntimeId => destinationRuntimeId;

    public void Bind(
        WorldObserverTopologyEdgeKind kind,
        string originId,
        string destinationId,
        Vector2 origin,
        Vector2 destination)
    {
        line = line != null ? line : gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        edgeKind = kind;
        originRuntimeId = originId;
        destinationRuntimeId = destinationId;
        name = (kind == WorldObserverTopologyEdgeKind.Hierarchy ? "Hierarchy_" : "Connection_")
            + originId + "_" + destinationId;
        line.color = kind == WorldObserverTopologyEdgeKind.Hierarchy
            ? new Color(0.34f, 0.55f, 0.40f, 0.55f)
            : new Color(0.84f, 0.55f, 0.18f, 0.95f);
        WorldObserverUiFactory.PlaceLine((RectTransform)transform, origin, destination, kind == WorldObserverTopologyEdgeKind.Hierarchy ? 3f : 5f);
    }
}

public sealed class WorldObserverDetailRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    public TMP_Text Label => label;

    public void Bind(string value)
    {
        label = label != null ? label : WorldObserverUiFactory.CreateText(transform, "Text", 14f, TextAlignmentOptions.TopLeft);
        label.text = value ?? string.Empty;
        label.enableWordWrapping = true;
        LayoutElement layout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 24f;
        layout.preferredHeight = 30f;
        WorldObserverUiFactory.Stretch(label.rectTransform, 6f, 2f, 6f, 2f);
    }
}

public sealed class WorldObserverActivityRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string eventId;
    [SerializeField] private DomainEventType eventType;

    public string EventId => eventId;
    public DomainEventType EventType => eventType;
    public TMP_Text Label => label;

    public void Bind(WorldObserverActivityReadModel activity)
    {
        label = label != null ? label : WorldObserverUiFactory.CreateText(transform, "Text", 13f, TextAlignmentOptions.TopLeft);
        eventId = activity.EventId;
        eventType = activity.EventType;
        label.text = activity.Summary;
        LayoutElement layout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 24f;
        layout.preferredHeight = 28f;
        WorldObserverUiFactory.Stretch(label.rectTransform, 6f, 2f, 6f, 2f);
    }
}

[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
public sealed class WorldObserverCanvasView : MonoBehaviour
{
    [Header("Top bar")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Button advanceDayButton;

    [Header("World graph")]
    [SerializeField] private RectTransform worldGraphPanel;
    [SerializeField] private RectTransform routeLayer;
    [SerializeField] private RectTransform travelerLayer;
    [SerializeField] private RectTransform nodeLayer;

    [Header("Detail")]
    [SerializeField] private RectTransform detailPanel;
    [SerializeField] private TMP_Text selectedPlaceTitle;
    [SerializeField] private RectTransform detailContent;

    [Header("Topology")]
    [SerializeField] private RectTransform topologyPanel;
    [SerializeField] private TMP_Text breadcrumbText;
    [SerializeField] private Button topologyBackButton;
    [SerializeField] private RectTransform topologyEdgeLayer;
    [SerializeField] private RectTransform topologyNodeLayer;

    [Header("Activity")]
    [SerializeField] private RectTransform activityPanel;
    [SerializeField] private RectTransform activityContent;

    private readonly List<WorldObserverWorldNodeView> worldNodes = new List<WorldObserverWorldNodeView>();
    private readonly List<WorldObserverRouteView> routeViews = new List<WorldObserverRouteView>();
    private readonly List<WorldObserverTravelerMarkerView> travelerMarkers = new List<WorldObserverTravelerMarkerView>();
    private readonly List<WorldObserverTopologyNodeView> topologyNodes = new List<WorldObserverTopologyNodeView>();
    private readonly List<WorldObserverTopologyEdgeView> topologyEdges = new List<WorldObserverTopologyEdgeView>();
    private readonly List<WorldObserverDetailRowView> detailRows = new List<WorldObserverDetailRowView>();
    private readonly List<WorldObserverActivityRowView> activityRows = new List<WorldObserverActivityRowView>();
    private WorldObserverQueryService queryService;
    private WorldObserverTimeController timeController;
    private string selectedPlaceRuntimeId;
    private bool topologyVisible;

    public TMP_Text DayText => dayText;
    public Button AdvanceDayButton => advanceDayButton;
    public RectTransform WorldGraphPanel => worldGraphPanel;
    public RectTransform DetailPanel => detailPanel;
    public RectTransform TopologyPanel => topologyPanel;
    public RectTransform ActivityPanel => activityPanel;
    public TMP_Text SelectedPlaceTitle => selectedPlaceTitle;
    public TMP_Text BreadcrumbText => breadcrumbText;
    public Button TopologyBackButton => topologyBackButton;
    public string SelectedPlaceRuntimeId => selectedPlaceRuntimeId;
    public bool IsTopologyVisible => topologyVisible;
    public IReadOnlyList<WorldObserverWorldNodeView> WorldNodes => worldNodes.AsReadOnly();
    public IReadOnlyList<WorldObserverRouteView> RouteViews => routeViews.AsReadOnly();
    public IReadOnlyList<WorldObserverTravelerMarkerView> TravelerMarkers => travelerMarkers.AsReadOnly();
    public IReadOnlyList<WorldObserverTopologyNodeView> TopologyNodes => topologyNodes.AsReadOnly();
    public IReadOnlyList<WorldObserverTopologyEdgeView> TopologyEdges => topologyEdges.AsReadOnly();
    public IReadOnlyList<WorldObserverDetailRowView> DetailRows => detailRows.AsReadOnly();
    public IReadOnlyList<WorldObserverActivityRowView> ActivityRows => activityRows.AsReadOnly();

    private void Awake()
    {
        EnsureReady();
    }

    public void Initialize(WorldObserverQueryService service, WorldObserverTimeController controller = null)
    {
        queryService = service;
        timeController = controller;
        Refresh();
    }

    public void EnsureReady()
    {
        EnsureCanvas();
        EnsureStructure();
        EnsureEventSystem();
        advanceDayButton.onClick.RemoveListener(AdvanceOneDay);
        advanceDayButton.onClick.AddListener(AdvanceOneDay);
        topologyBackButton.onClick.RemoveListener(ReturnToWorld);
        topologyBackButton.onClick.AddListener(ReturnToWorld);
    }

    public void SelectPlace(string runtimeId)
    {
        selectedPlaceRuntimeId = string.IsNullOrWhiteSpace(runtimeId) ? null : runtimeId;
        topologyVisible = false;
        if (queryService != null)
        {
            WorldObserverReadModel model = queryService.BuildReadModel(selectedPlaceRuntimeId);
            topologyVisible = model.SelectedPlace != null && model.SelectedPlace.TopologyNodes.Count > 0;
            Render(model);
        }
        else
        {
            Refresh();
        }
    }

    public void ReturnToWorld()
    {
        topologyVisible = false;
        ApplyNavigationState();
    }

    public void Refresh()
    {
        EnsureReady();
        WorldObserverReadModel model = queryService?.BuildReadModel(selectedPlaceRuntimeId);
        Render(model);
    }

    public void AdvanceOneDay()
    {
        if (timeController != null && timeController.AdvanceOneDay() == true)
        {
            Refresh();
        }
    }

    private void Render(WorldObserverReadModel model)
    {
        ClearGeneratedViews();
        if (model == null)
        {
            dayText.text = "Day —";
            selectedPlaceTitle.text = "No place selected";
            breadcrumbText.text = "World";
            AddDetailRow("Observer awaiting a read model.");
            ApplyNavigationState();
            return;
        }

        dayText.text = "Day " + model.CurrentDay;
        RenderWorldGraph(model);
        RenderSelectedPlace(model.SelectedPlace);
        RenderTopology(model.SelectedPlace);
        RenderActivity(model.ActivityFeed);
        ApplyNavigationState();
    }

    private void RenderWorldGraph(WorldObserverReadModel model)
    {
        Dictionary<string, Vector2> locationPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        Dictionary<string, WorldObserverRouteReadModel> routesById = new Dictionary<string, WorldObserverRouteReadModel>(StringComparer.Ordinal);
        int locationCount = model.Locations.Count;
        float radiusX = locationCount <= 2 ? 220f : 285f;
        float radiusY = locationCount <= 2 ? 120f : 175f;

        for (int i = 0; i < locationCount; i++)
        {
            WorldObserverLocationReadModel location = model.Locations[i];
            float angle = locationCount == 1
                ? Mathf.PI * 0.5f
                : Mathf.PI * 0.5f - Mathf.PI * 2f * i / locationCount;
            Vector2 position = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            locationPositions[location.RuntimeId] = position;
        }

        foreach (WorldObserverRouteReadModel route in model.Routes)
        {
            routesById[route.RuntimeId] = route;
            if (locationPositions.TryGetValue(route.OriginLocationRuntimeId, out Vector2 origin) == false
                || locationPositions.TryGetValue(route.DestinationLocationRuntimeId, out Vector2 destination) == false)
            {
                continue;
            }

            GameObject routeObject = WorldObserverUiFactory.CreateUiObject("Route", routeLayer);
            WorldObserverRouteView routeView = routeObject.AddComponent<WorldObserverRouteView>();
            routeView.Bind(route, origin, destination);
            routeViews.Add(routeView);
        }

        for (int i = 0; i < locationCount; i++)
        {
            WorldObserverLocationReadModel location = model.Locations[i];
            Vector2 position = locationPositions[location.RuntimeId];
            GameObject nodeObject = WorldObserverUiFactory.CreateUiObject("WorldNode", nodeLayer);
            WorldObserverWorldNodeView node = nodeObject.AddComponent<WorldObserverWorldNodeView>();
            node.Bind(location.RuntimeId, location.CityRuntimeId, location.DisplayName, false, position, SelectPlace);
            worldNodes.Add(node);

            List<WorldObserverSiteReadModel> sitesAtLocation = new List<WorldObserverSiteReadModel>();
            foreach (WorldObserverSiteReadModel site in model.Sites)
            {
                if (string.Equals(site.LocationRuntimeId, location.RuntimeId, StringComparison.Ordinal))
                {
                    sitesAtLocation.Add(site);
                }
            }

            sitesAtLocation.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
            for (int siteIndex = 0; siteIndex < sitesAtLocation.Count; siteIndex++)
            {
                WorldObserverSiteReadModel site = sitesAtLocation[siteIndex];
                float centeredIndex = siteIndex - (sitesAtLocation.Count - 1) * 0.5f;
                Vector2 sitePosition = position + new Vector2(centeredIndex * 138f, -58f);
                GameObject siteObject = WorldObserverUiFactory.CreateUiObject("SiteNode", nodeLayer);
                WorldObserverWorldNodeView siteNode = siteObject.AddComponent<WorldObserverWorldNodeView>();
                siteNode.Bind(site.RuntimeId, site.RuntimeId, site.DisplayName, true, sitePosition, SelectPlace);
                worldNodes.Add(siteNode);
            }
        }

        Dictionary<string, int> markerCountsByRoute = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (WorldObserverTravelerReadModel traveler in model.Travelers)
        {
            if (traveler.IsTraveling == false
                || string.IsNullOrWhiteSpace(traveler.RouteRuntimeId)
                || routesById.TryGetValue(traveler.RouteRuntimeId, out WorldObserverRouteReadModel route) == false
                || locationPositions.TryGetValue(route.OriginLocationRuntimeId, out Vector2 origin) == false
                || locationPositions.TryGetValue(route.DestinationLocationRuntimeId, out Vector2 destination) == false)
            {
                continue;
            }

            markerCountsByRoute.TryGetValue(route.RuntimeId, out int markerIndex);
            markerCountsByRoute[route.RuntimeId] = markerIndex + 1;
            Vector2 direction = (destination - origin).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 offset = perpendicular * (markerIndex % 2 == 0 ? markerIndex * 10f : -(markerIndex + 1) * 10f);
            GameObject markerObject = WorldObserverUiFactory.CreateUiObject("Traveler", travelerLayer);
            WorldObserverTravelerMarkerView marker = markerObject.AddComponent<WorldObserverTravelerMarkerView>();
            marker.Bind(traveler, origin, destination, offset);
            travelerMarkers.Add(marker);
        }
    }

    private void RenderSelectedPlace(WorldObserverPlaceReadModel place)
    {
        if (place == null)
        {
            selectedPlaceTitle.text = "No place selected";
            AddDetailRow("Select a city, site, or local place on the graph.");
            return;
        }

        selectedPlaceTitle.text = place.DisplayName;
        AddDetailRow("RuntimeId · " + place.RuntimeId);
        AddDetailRow("Kind · " + place.OwnerKind);
        AddDetailRow("Macro location · " + place.MacroLocationRuntimeId);
        AddDetailRow("NPCs present · " + (place.PresentNpcRuntimeIds.Count == 0
            ? "none"
            : string.Join(", ", place.PresentNpcRuntimeIds)));
        AddDetailRow("Expeditions · " + place.Expeditions.Count);
        if (place.HasSiteState)
        {
            AddDetailRow("State · " + place.SiteState + "  |  Access · " + place.AccessState);
            AddDetailRow("Controller · " + (string.IsNullOrWhiteSpace(place.ControllerRuntimeId) ? "none" : place.ControllerRuntimeId));
        }

        foreach (WorldObserverExpeditionReadModel expedition in place.Expeditions)
        {
            AddDetailRow(
                "Expedition " + expedition.ExpeditionId + " · " + expedition.State
                + " · " + expedition.ObjectiveType + " " + expedition.Progress + "/" + expedition.RequiredProgress);
        }

        foreach (WorldObserverOppositionReadModel opposition in place.Oppositions)
        {
            AddDetailRow("Opposition " + opposition.DisplayName + " · " + (opposition.IsActive ? "active" : "resolved"));
        }

        foreach (WorldObserverContentReadModel content in place.Content)
        {
            string identity = string.IsNullOrWhiteSpace(content.NotableRuntimeId)
                ? content.ItemDefinitionId
                : content.ItemDefinitionId + " [" + content.NotableRuntimeId + "]";
            AddDetailRow("Content · " + identity + " ×" + content.Amount + " · " + content.PersistencePolicy);
        }
    }

    private void RenderTopology(WorldObserverPlaceReadModel place)
    {
        breadcrumbText.text = place == null ? "World" : "World  ›  " + place.DisplayName;
        if (place == null || place.TopologyNodes.Count == 0)
        {
            return;
        }

        Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        Dictionary<int, List<WorldObserverTopologyNodeReadModel>> byDepth = new Dictionary<int, List<WorldObserverTopologyNodeReadModel>>();
        foreach (WorldObserverTopologyNodeReadModel node in place.TopologyNodes)
        {
            if (byDepth.TryGetValue(node.Depth, out List<WorldObserverTopologyNodeReadModel> layer) == false)
            {
                layer = new List<WorldObserverTopologyNodeReadModel>();
                byDepth.Add(node.Depth, layer);
            }

            layer.Add(node);
        }

        List<int> depths = new List<int>(byDepth.Keys);
        depths.Sort();
        foreach (int depth in depths)
        {
            List<WorldObserverTopologyNodeReadModel> layer = byDepth[depth];
            layer.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
            for (int i = 0; i < layer.Count; i++)
            {
                float centeredIndex = i - (layer.Count - 1) * 0.5f;
                positions[layer[i].RuntimeId] = new Vector2(centeredIndex * 172f, 90f - depth * 72f);
            }
        }

        foreach (WorldObserverTopologyNodeReadModel node in place.TopologyNodes)
        {
            if (string.IsNullOrWhiteSpace(node.ParentRuntimeId)
                || positions.TryGetValue(node.ParentRuntimeId, out Vector2 parentPosition) == false)
            {
                continue;
            }

            GameObject edgeObject = WorldObserverUiFactory.CreateUiObject("HierarchyEdge", topologyEdgeLayer);
            WorldObserverTopologyEdgeView edge = edgeObject.AddComponent<WorldObserverTopologyEdgeView>();
            edge.Bind(WorldObserverTopologyEdgeKind.Hierarchy, node.ParentRuntimeId, node.RuntimeId, parentPosition, positions[node.RuntimeId]);
            topologyEdges.Add(edge);
        }

        foreach (WorldObserverTopologyConnectionReadModel connection in place.TopologyConnections)
        {
            if (positions.TryGetValue(connection.OriginRuntimeId, out Vector2 origin) == false
                || positions.TryGetValue(connection.DestinationRuntimeId, out Vector2 destination) == false)
            {
                continue;
            }

            GameObject edgeObject = WorldObserverUiFactory.CreateUiObject("ConnectionEdge", topologyEdgeLayer);
            WorldObserverTopologyEdgeView edge = edgeObject.AddComponent<WorldObserverTopologyEdgeView>();
            edge.Bind(
                WorldObserverTopologyEdgeKind.Connection,
                connection.OriginRuntimeId,
                connection.DestinationRuntimeId,
                origin + new Vector2(0f, -6f),
                destination + new Vector2(0f, -6f));
            topologyEdges.Add(edge);
        }

        foreach (WorldObserverTopologyNodeReadModel node in place.TopologyNodes)
        {
            GameObject nodeObject = WorldObserverUiFactory.CreateUiObject("LocalNode", topologyNodeLayer);
            WorldObserverTopologyNodeView nodeView = nodeObject.AddComponent<WorldObserverTopologyNodeView>();
            nodeView.Bind(node, positions[node.RuntimeId], SelectLocalPlace);
            topologyNodes.Add(nodeView);
        }
    }

    private void RenderActivity(IReadOnlyList<WorldObserverActivityReadModel> activities)
    {
        foreach (WorldObserverActivityReadModel activity in activities)
        {
            GameObject rowObject = WorldObserverUiFactory.CreateUiObject("ActivityRow", activityContent);
            WorldObserverActivityRowView row = rowObject.AddComponent<WorldObserverActivityRowView>();
            row.Bind(activity);
            activityRows.Add(row);
        }
    }

    private void SelectLocalPlace(string runtimeId)
    {
        selectedPlaceRuntimeId = runtimeId;
        topologyVisible = true;
        Refresh();
    }

    private void AddDetailRow(string value)
    {
        GameObject rowObject = WorldObserverUiFactory.CreateUiObject("DetailRow", detailContent);
        WorldObserverDetailRowView row = rowObject.AddComponent<WorldObserverDetailRowView>();
        row.Bind(value);
        detailRows.Add(row);
    }

    private void ApplyNavigationState()
    {
        if (topologyPanel != null)
        {
            topologyPanel.gameObject.SetActive(topologyVisible);
        }
    }

    private void ClearGeneratedViews()
    {
        WorldObserverUiFactory.DestroyChildren(routeLayer);
        WorldObserverUiFactory.DestroyChildren(travelerLayer);
        WorldObserverUiFactory.DestroyChildren(nodeLayer);
        WorldObserverUiFactory.DestroyChildren(detailContent);
        WorldObserverUiFactory.DestroyChildren(topologyEdgeLayer);
        WorldObserverUiFactory.DestroyChildren(topologyNodeLayer);
        WorldObserverUiFactory.DestroyChildren(activityContent);
        worldNodes.Clear();
        routeViews.Clear();
        travelerMarkers.Clear();
        topologyNodes.Clear();
        topologyEdges.Clear();
        detailRows.Clear();
        activityRows.Clear();
    }

    private void EnsureCanvas()
    {
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureStructure()
    {
        topBar = topBar != null ? topBar : WorldObserverUiFactory.CreatePanel(transform, "TopBar", new Color(0.08f, 0.12f, 0.16f, 1f));
        WorldObserverUiFactory.SetAnchors(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), Vector2.zero);
        dayText = dayText != null ? dayText : WorldObserverUiFactory.CreateText(topBar, "DayLabel", 24f, TextAlignmentOptions.MidlineLeft);
        WorldObserverUiFactory.SetAnchors(dayText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(22f, 8f), new Vector2(-8f, -8f));
        advanceDayButton = advanceDayButton != null
            ? advanceDayButton
            : WorldObserverUiFactory.CreateButton(topBar, "AdvanceDayButton", "Advance 1 Day", new Vector2(170f, 42f));
        RectTransform advanceRect = (RectTransform)advanceDayButton.transform;
        advanceRect.anchorMin = new Vector2(1f, 0.5f);
        advanceRect.anchorMax = new Vector2(1f, 0.5f);
        advanceRect.pivot = new Vector2(1f, 0.5f);
        advanceRect.anchoredPosition = new Vector2(-18f, 0f);

        worldGraphPanel = worldGraphPanel != null ? worldGraphPanel : WorldObserverUiFactory.CreatePanel(transform, "WorldGraphPanel", new Color(0.06f, 0.09f, 0.12f, 0.96f));
        WorldObserverUiFactory.SetAnchors(worldGraphPanel, new Vector2(0f, 0.34f), new Vector2(0.68f, 1f), new Vector2(12f, 12f), new Vector2(-6f, -76f));
        routeLayer = routeLayer != null ? routeLayer : WorldObserverUiFactory.CreateLayer(worldGraphPanel, "RouteLayer");
        travelerLayer = travelerLayer != null ? travelerLayer : WorldObserverUiFactory.CreateLayer(worldGraphPanel, "TravelerLayer");
        nodeLayer = nodeLayer != null ? nodeLayer : WorldObserverUiFactory.CreateLayer(worldGraphPanel, "NodeLayer");

        detailPanel = detailPanel != null ? detailPanel : WorldObserverUiFactory.CreatePanel(transform, "DetailPanel", new Color(0.10f, 0.14f, 0.18f, 0.98f));
        WorldObserverUiFactory.SetAnchors(detailPanel, new Vector2(0.68f, 0.34f), new Vector2(1f, 1f), new Vector2(6f, 12f), new Vector2(-12f, -76f));
        selectedPlaceTitle = selectedPlaceTitle != null ? selectedPlaceTitle : WorldObserverUiFactory.CreateText(detailPanel, "SelectedPlaceTitle", 21f, TextAlignmentOptions.MidlineLeft);
        WorldObserverUiFactory.SetAnchors(selectedPlaceTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -48f), new Vector2(-12f, -8f));
        detailContent = detailContent != null ? detailContent : WorldObserverUiFactory.CreateScrollContent(detailPanel, "DetailScroll", 56f);

        topologyPanel = topologyPanel != null ? topologyPanel : WorldObserverUiFactory.CreatePanel(transform, "TopologyPanel", new Color(0.07f, 0.12f, 0.10f, 0.99f));
        WorldObserverUiFactory.SetAnchors(topologyPanel, new Vector2(0f, 0f), new Vector2(0.68f, 0.34f), new Vector2(12f, 12f), new Vector2(-6f, -6f));
        breadcrumbText = breadcrumbText != null ? breadcrumbText : WorldObserverUiFactory.CreateText(topologyPanel, "Breadcrumb", 16f, TextAlignmentOptions.MidlineLeft);
        WorldObserverUiFactory.SetAnchors(breadcrumbText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(112f, -40f), new Vector2(-12f, -4f));
        topologyBackButton = topologyBackButton != null
            ? topologyBackButton
            : WorldObserverUiFactory.CreateButton(topologyPanel, "BackToWorldButton", "‹ World", new Vector2(92f, 32f));
        RectTransform backRect = (RectTransform)topologyBackButton.transform;
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(10f, -6f);
        topologyEdgeLayer = topologyEdgeLayer != null ? topologyEdgeLayer : WorldObserverUiFactory.CreateLayer(topologyPanel, "TopologyEdgeLayer", 44f);
        topologyNodeLayer = topologyNodeLayer != null ? topologyNodeLayer : WorldObserverUiFactory.CreateLayer(topologyPanel, "TopologyNodeLayer", 44f);

        activityPanel = activityPanel != null ? activityPanel : WorldObserverUiFactory.CreatePanel(transform, "ActivityPanel", new Color(0.12f, 0.10f, 0.14f, 0.99f));
        WorldObserverUiFactory.SetAnchors(activityPanel, new Vector2(0.68f, 0f), new Vector2(1f, 0.34f), new Vector2(6f, 12f), new Vector2(-12f, -6f));
        TMP_Text activityTitle = activityPanel.Find("ActivityTitle")?.GetComponent<TMP_Text>();
        if (activityTitle == null)
        {
            activityTitle = WorldObserverUiFactory.CreateText(activityPanel, "ActivityTitle", 18f, TextAlignmentOptions.MidlineLeft);
            activityTitle.text = "Activity";
            WorldObserverUiFactory.SetAnchors(activityTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -42f), new Vector2(-12f, -4f));
        }
        activityContent = activityContent != null ? activityContent : WorldObserverUiFactory.CreateScrollContent(activityPanel, "ActivityScroll", 48f);
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = GetComponentInChildren<EventSystem>(true);
        if (existing != null)
        {
            return;
        }

        GameObject eventObject = new GameObject("ObserverEventSystem");
        eventObject.transform.SetParent(transform, false);
        eventObject.AddComponent<EventSystem>();
        eventObject.AddComponent<InputSystemUIInputModule>();
    }
}

internal static class WorldObserverUiFactory
{
    public static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    public static RectTransform CreatePanel(Transform parent, string objectName, Color color)
    {
        GameObject panelObject = CreateUiObject(objectName, parent);
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return (RectTransform)panelObject.transform;
    }

    public static RectTransform CreateLayer(Transform parent, string objectName, float topOffset = 0f)
    {
        GameObject layerObject = CreateUiObject(objectName, parent);
        RectTransform layer = (RectTransform)layerObject.transform;
        Stretch(layer, 0f, topOffset, 0f, 0f);
        return layer;
    }

    public static TMP_Text CreateText(Transform parent, string objectName, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.92f, 0.94f, 0.96f, 1f);
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    public static Button CreateButton(Transform parent, string objectName, string labelValue, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.42f, 0.54f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        ((RectTransform)buttonObject.transform).sizeDelta = size;
        TMP_Text label = CreateText(buttonObject.transform, "Label", 15f, TextAlignmentOptions.Center);
        label.text = labelValue;
        Stretch(label.rectTransform, 4f, 2f, 4f, 2f);
        return button;
    }

    public static RectTransform CreateScrollContent(Transform panel, string objectName, float topOffset)
    {
        GameObject scrollObject = CreateUiObject(objectName, panel);
        RectTransform scrollRectTransform = (RectTransform)scrollObject.transform;
        Stretch(scrollRectTransform, 8f, topOffset, 8f, 8f);
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        GameObject viewportObject = CreateUiObject("Viewport", scrollObject.transform);
        RectTransform viewport = (RectTransform)viewportObject.transform;
        Stretch(viewport, 0f, 0f, 0f, 0f);
        viewportObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
        viewportObject.AddComponent<RectMask2D>();
        GameObject contentObject = CreateUiObject("Content", viewportObject.transform);
        RectTransform content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        layout.padding = new RectOffset(2, 2, 2, 2);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        return content;
    }

    public static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void PlaceLine(RectTransform rect, Vector2 origin, Vector2 destination, float thickness)
    {
        Vector2 delta = destination - origin;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = (origin + destination) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    public static void DestroyChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(child);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(child);
            }
        }
    }
}
