using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldObserverTimeController : MonoBehaviour
{
    private SimulationRuntime simulationRuntime;
    private Action<int> advanceDaysAction;

    public void Bind(SimulationRuntime runtime)
    {
        simulationRuntime = runtime;
        advanceDaysAction = null;
    }

    public void SetAdvanceDaysCallback(Action<int> callback)
    {
        advanceDaysAction = callback;
        simulationRuntime = null;
    }

    public bool AdvanceOneDay()
    {
        if (advanceDaysAction != null)
        {
            advanceDaysAction(1);
            return true;
        }

        if (simulationRuntime == null)
        {
            return false;
        }

        simulationRuntime.AdvanceDay();
        return true;
    }
}

public sealed class WorldObserverCanvasView : MonoBehaviour
{
    [SerializeField] private TMP_Text worldGraphText;
    [SerializeField] private TMP_Text selectedPlaceText;
    [SerializeField] private TMP_Text topologyText;
    [SerializeField] private TMP_Text activityFeedText;
    [SerializeField] private Button advanceDayButton;

    private WorldObserverQueryService queryService;
    private WorldObserverTimeController timeController;
    private string selectedPlaceRuntimeId;

    public TMP_Text WorldGraphText => worldGraphText;
    public TMP_Text SelectedPlaceText => selectedPlaceText;
    public TMP_Text TopologyText => topologyText;
    public TMP_Text ActivityFeedText => activityFeedText;

    private void Awake()
    {
        EnsureCanvas();
        EnsureTextFields();
        if (advanceDayButton != null)
        {
            advanceDayButton.onClick.AddListener(AdvanceOneDay);
        }
    }

    public void Initialize(
        WorldObserverQueryService service,
        WorldObserverTimeController controller = null)
    {
        queryService = service;
        timeController = controller;
        Refresh();
    }

    public void EnsureReady()
    {
        EnsureCanvas();
        EnsureTextFields();
    }

    public void SelectPlace(string runtimeId)
    {
        selectedPlaceRuntimeId = string.IsNullOrWhiteSpace(runtimeId) == true ? null : runtimeId;
        Refresh();
    }

    public void Refresh()
    {
        EnsureCanvas();
        EnsureTextFields();
        if (queryService == null)
        {
            worldGraphText.text = "World observer aguardando um read-model.";
            selectedPlaceText.text = "Nenhum lugar selecionado.";
            topologyText.text = "Topologia indisponível.";
            activityFeedText.text = "Chronicle vazia.";
            return;
        }

        WorldObserverReadModel model = queryService.BuildReadModel(selectedPlaceRuntimeId);
        worldGraphText.text = FormatWorldGraph(model);
        selectedPlaceText.text = FormatSelectedPlace(model.SelectedPlace);
        topologyText.text = FormatTopology(model.SelectedPlace);
        activityFeedText.text = FormatActivity(model);
    }

    public void AdvanceOneDay()
    {
        if (timeController != null && timeController.AdvanceOneDay() == true)
        {
            Refresh();
        }
    }

    private void EnsureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (GetComponent<CanvasScaler>() == null)
        {
            gameObject.AddComponent<CanvasScaler>();
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureTextFields()
    {
        worldGraphText = worldGraphText != null ? worldGraphText : CreateText("WorldGraph", 22f);
        selectedPlaceText = selectedPlaceText != null ? selectedPlaceText : CreateText("SelectedPlace", 18f);
        topologyText = topologyText != null ? topologyText : CreateText("Topology", 16f);
        activityFeedText = activityFeedText != null ? activityFeedText : CreateText("ActivityFeed", 16f);
    }

    private TMP_Text CreateText(string objectName, float fontSize)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        TextMeshProUGUI text = child.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.raycastTarget = false;
        RectTransform rectTransform = text.rectTransform;
        rectTransform.sizeDelta = new Vector2(720f, 180f);
        return text;
    }

    private static string FormatWorldGraph(WorldObserverReadModel model)
    {
        StringBuilder builder = new StringBuilder("WORLD GRAPH\n");
        foreach (WorldObserverLocationReadModel location in model.Locations)
        {
            builder.Append(location.DisplayName).Append(" [").Append(location.RuntimeId).Append("]");
            if (location.SiteRuntimeIds.Count > 0)
            {
                builder.Append(" · sites: ").Append(string.Join(", ", location.SiteRuntimeIds));
            }

            builder.AppendLine();
        }

        foreach (WorldObserverTravelerReadModel traveler in model.Travelers)
        {
            if (traveler.IsTraveling == true)
            {
                builder.Append("  traveler ").Append(traveler.DisplayName).Append(" → ")
                    .Append(traveler.DestinationLocationRuntimeId).AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string FormatSelectedPlace(WorldObserverPlaceReadModel place)
    {
        if (place == null)
        {
            return "Nenhum lugar selecionado.";
        }

        StringBuilder builder = new StringBuilder("PLACE\n");
        builder.Append(place.DisplayName).Append(" [").Append(place.OwnerKind).AppendLine("]");
        builder.Append("NPCs presentes: ").Append(place.PresentNpcRuntimeIds.Count).AppendLine();
        builder.Append("Expedições: ").Append(place.Expeditions.Count).AppendLine();
        builder.Append("Conteúdo: ").Append(place.Content.Count).AppendLine();
        if (place.HasSiteState == true)
        {
            builder.Append("Estado: ").Append(place.SiteState).Append(" / acesso: ").AppendLine(place.AccessState.ToString());
        }

        return builder.ToString();
    }

    private static string FormatTopology(WorldObserverPlaceReadModel place)
    {
        if (place == null || place.TopologyNodes.Count == 0)
        {
            return "TOPOLOGY\nNão há topologia publicada para este lugar.";
        }

        StringBuilder builder = new StringBuilder("TOPOLOGY\n");
        foreach (WorldObserverTopologyNodeReadModel node in place.TopologyNodes)
        {
            builder.Append(' ', node.Depth * 2).Append(node.DisplayName).Append(" [")
                .Append(node.RuntimeId).Append("]");
            if (node.ParentRuntimeId != null)
            {
                builder.Append(" parent=").Append(node.ParentRuntimeId);
            }

            builder.AppendLine();
        }

        builder.AppendLine("Connections");
        foreach (WorldObserverTopologyConnectionReadModel connection in place.TopologyConnections)
        {
            builder.Append(connection.OriginRuntimeId).Append(" → ").Append(connection.DestinationRuntimeId).AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatActivity(WorldObserverReadModel model)
    {
        StringBuilder builder = new StringBuilder("ACTIVITY\n");
        foreach (WorldObserverActivityReadModel activity in model.ActivityFeed)
        {
            builder.Append("day ").Append(activity.AbsoluteDay).Append(" · ").AppendLine(activity.EventType.ToString());
        }

        return builder.ToString();
    }
}
