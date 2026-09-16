using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldObserverGmConsoleContext
{
    public WorldCommandService CommandService { get; }
    public IWorldCommandDefinitionResolver DefinitionResolver { get; }
    public RuntimeIdentityRegistry IdentityRegistry { get; }
    public IReadOnlyList<CityRuntime> Cities { get; }
    public IReadOnlyList<NpcRuntime> Npcs { get; }
    public ExplorableSiteStore SiteStore { get; }
    public LocalTopologyStore TopologyStore { get; }
    public PlaceContentStore ContentStore { get; }
    public Func<string> SelectedPlaceRuntimeId { get; }
    public Action RefreshObserver { get; }

    public WorldObserverGmConsoleContext(
        WorldCommandService commandService,
        IWorldCommandDefinitionResolver definitionResolver,
        RuntimeIdentityRegistry identityRegistry,
        IEnumerable<CityRuntime> cities,
        IEnumerable<NpcRuntime> npcs,
        ExplorableSiteStore siteStore,
        LocalTopologyStore topologyStore,
        PlaceContentStore contentStore,
        Func<string> selectedPlaceRuntimeId = null,
        Action refreshObserver = null)
    {
        CommandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        DefinitionResolver = definitionResolver;
        IdentityRegistry = identityRegistry;
        Cities = new List<CityRuntime>(cities ?? Array.Empty<CityRuntime>()).AsReadOnly();
        Npcs = new List<NpcRuntime>(npcs ?? Array.Empty<NpcRuntime>()).AsReadOnly();
        SiteStore = siteStore;
        TopologyStore = topologyStore;
        ContentStore = contentStore;
        SelectedPlaceRuntimeId = selectedPlaceRuntimeId;
        RefreshObserver = refreshObserver;
    }
}

public sealed class GMConsolePanel : MonoBehaviour
{
    private static readonly WorldCommandKind[] CommandKinds =
    {
        WorldCommandKind.RelocateNpc,
        WorldCommandKind.DeclareStackResource,
        WorldCommandKind.DeclareNotableItem,
        WorldCommandKind.AddLocalPlace,
        WorldCommandKind.AddLocalConnection,
        WorldCommandKind.GrantSiteKnowledge,
        WorldCommandKind.GrantAdventureIntel,
        WorldCommandKind.ResolveConflict,
        WorldCommandKind.PlaceOpposition
    };

    private static readonly WorldCommandAuthorityMode[] AuthorityModes =
    {
        WorldCommandAuthorityMode.Suggest,
        WorldCommandAuthorityMode.Request,
        WorldCommandAuthorityMode.Declare,
        WorldCommandAuthorityMode.ForceOutcome
    };

    private readonly Dictionary<string, TMP_InputField> inputs = new Dictionary<string, TMP_InputField>(StringComparer.Ordinal);
    private readonly Dictionary<string, TMP_Dropdown> dropdowns = new Dictionary<string, TMP_Dropdown>(StringComparer.Ordinal);
    private readonly Dictionary<string, GameObject> formGroups = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private RectTransform rootPanel;
    private RectTransform formContent;
    private RectTransform previewContent;
    private RectTransform historyContent;
    private TMP_Text previewText;
    private TMP_Dropdown commandTypeDropdown;
    private TMP_Dropdown authorityDropdown;
    private Button previewButton;
    private Button applyButton;
    private Button closeButton;
    private WorldObserverGmConsoleContext context;
    private WorldCommand pendingCommand;
    private WorldCommandPreview lastPreview;
    private WorldCommandResult lastResult;
    private bool ready;

    public TMP_Dropdown CommandTypeDropdown => commandTypeDropdown ?? FindChildDropdown("CommandTypeDropdown");
    public TMP_Dropdown AuthorityDropdown => authorityDropdown ?? FindChildDropdown("AuthorityModeDropdown");
    public RectTransform DynamicFormArea => formContent;
    public Button PreviewButton => previewButton;
    public Button ApplyButton => applyButton;
    public TMP_Text PreviewText => previewText;
    public RectTransform CommandHistoryPanel => historyContent;
    public WorldCommandService CommandService => context?.CommandService;
    public WorldCommand PendingCommand => pendingCommand;
    public WorldCommandPreview LastPreview => lastPreview;
    public WorldCommandResult LastResult => lastResult;
    public bool IsReady => ready;

    public TMP_InputField GetInput(string key)
    {
        return inputs.TryGetValue(key, out TMP_InputField input) ? input : null;
    }

    public TMP_Dropdown GetFormDropdown(string key)
    {
        return dropdowns.TryGetValue(key, out TMP_Dropdown dropdown) ? dropdown : null;
    }

    public void Awake()
    {
        EnsureReady();
    }

    public void EnsureReady()
    {
        // Asset inspection (for example, an Editor test loading the prefab) must not
        // try to parent runtime-generated controls into the immutable prefab asset.
        if (Application.isPlaying == false && gameObject.scene.IsValid() == false)
        {
            return;
        }

        if (ready)
        {
            return;
        }

        if (TryCacheSerializedUi())
        {
            ready = true;
            SetVisibleForm();
            applyButton.interactable = false;
            return;
        }

        bool wasActive = gameObject.activeSelf;
        EnsurePanelLayout();
        BuildHeader();
        BuildForms();
        BuildPreviewAndHistory();
        ready = true;
        if (wasActive)
        {
            gameObject.SetActive(false);
        }
    }

    public void Bind(WorldObserverGmConsoleContext nextContext)
    {
        EnsureReady();
        context = nextContext ?? throw new ArgumentNullException(nameof(nextContext));
        ApplySelectedPlaceDefault();
        RefreshDefinitionDefaults();
        RefreshCommandHistory();
        SetPreviewMessage("Choose a structured command and press Preview.");
    }

    public void Toggle()
    {
        EnsureReady();
        gameObject.SetActive(!gameObject.activeSelf);
        if (gameObject.activeSelf)
        {
            ApplySelectedPlaceDefault();
            RefreshCommandHistory();
        }
    }

    public void Show()
    {
        EnsureReady();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void PreviewCurrentCommand()
    {
        EnsureReady();
        if (TryBuildCommand(out WorldCommand command, out string diagnostic) == false)
        {
            pendingCommand = null;
            lastPreview = new WorldCommandPreview(
                commandTypeDropdown != null && commandTypeDropdown.value < CommandKinds.Length ? CommandKinds[commandTypeDropdown.value] : WorldCommandKind.RelocateNpc,
                GetAuthority(),
                false,
                presentation: diagnostic,
                warnings: new[] { diagnostic });
            SetPreviewMessage(diagnostic);
            applyButton.interactable = false;
            return;
        }

        pendingCommand = command;
        lastPreview = context.CommandService.Preview(command);
        SetPreviewMessage(FormatPreview(lastPreview));
        applyButton.interactable = lastPreview.IsValid;
    }

    public void ApplyCurrentCommand()
    {
        EnsureReady();
        if (context == null)
        {
            SetPreviewMessage("GM Console is not bound to a WorldCommandService.");
            return;
        }

        WorldCommand rebuiltCommand = null;
        if (pendingCommand == null && TryBuildCommand(out rebuiltCommand, out string diagnostic) == false)
        {
            SetPreviewMessage(diagnostic);
            return;
        }

        if (pendingCommand == null)
        {
            pendingCommand = rebuiltCommand;
        }

        // Execute intentionally receives the original structured command again: the service revalidates Truth.
        lastResult = context.CommandService.Execute(pendingCommand);
        pendingCommand = null;
        SetPreviewMessage(FormatResult(lastResult));
        applyButton.interactable = false;
        RefreshCommandHistory();
        if (lastResult.Success)
        {
            context.RefreshObserver?.Invoke();
        }
    }

    public bool TryBuildCommand(out WorldCommand command, out string diagnostic)
    {
        command = null;
        diagnostic = null;
        if (context == null || context.CommandService == null)
        {
            diagnostic = "GM Console is not bound to a WorldCommandService.";
            return false;
        }

        try
        {
            WorldCommandKind kind = GetCommandKind();
            WorldCommandAuthorityMode authority = GetAuthority();
            WorldCommandPayload payload;
            switch (kind)
            {
                case WorldCommandKind.RelocateNpc:
                    payload = new RelocateNpcWorldCommandPayload(Value("relocate.npc"), Value("relocate.destination"));
                    break;
                case WorldCommandKind.DeclareStackResource:
                    payload = new DeclareStackResourceWorldCommandPayload(
                        BuildOwnerPayload(),
                        Value("content.item"),
                        ParsePositiveInt("content.amount"),
                        GetEnum("content.persistence", PlaceContentPersistencePolicy.Durable),
                        ParseNonNegativeInt("content.decay"),
                        ParseOptionalFloat("content.averageCost"));
                    break;
                case WorldCommandKind.DeclareNotableItem:
                    payload = new DeclareNotableItemWorldCommandPayload(BuildOwnerPayload(), Value("content.item"));
                    break;
                case WorldCommandKind.AddLocalPlace:
                    payload = new AddLocalPlaceWorldCommandPayload(
                        Value("topology.owner"),
                        Value("topology.displayName"),
                        OptionalValue("topology.placeType"),
                        OptionalValue("topology.parent"),
                        GetDropdownValue("topology.entryPoint"));
                    break;
                case WorldCommandKind.AddLocalConnection:
                    payload = new AddLocalConnectionWorldCommandPayload(
                        Value("topology.owner"),
                        Value("connection.origin"),
                        Value("connection.destination"),
                        ParsePositiveFloat("connection.cost"),
                        OptionalValue("connection.type"));
                    break;
                case WorldCommandKind.GrantSiteKnowledge:
                    payload = new GrantSiteKnowledgeWorldCommandPayload(
                        Value("knowledge.npc"),
                        Value("knowledge.site"),
                        GetEnum("knowledge.source", ExplorableSiteKnowledgeSource.DirectObservation));
                    break;
                case WorldCommandKind.GrantAdventureIntel:
                    payload = BuildAdventureIntelPayload();
                    break;
                case WorldCommandKind.ResolveConflict:
                case WorldCommandKind.PlaceOpposition:
                    payload = BuildConflictPayload(kind);
                    break;
                default:
                    diagnostic = "This command kind is not supported by the GM Console form.";
                    return false;
            }

            command = new WorldCommand(kind, WorldCommandOrigin.GM, authority, payload);
            return true;
        }
        catch (ArgumentException exception)
        {
            diagnostic = exception.Message;
            return false;
        }
        catch (FormatException exception)
        {
            diagnostic = exception.Message;
            return false;
        }
        catch (InvalidOperationException exception)
        {
            diagnostic = exception.Message;
            return false;
        }
    }

    private void EnsurePanelLayout()
    {
        rootPanel = (RectTransform)transform;
        Image background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.055f, 0.075f, 0.99f);
        WorldObserverUiFactory.SetAnchors(rootPanel, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
    }

    private bool TryCacheSerializedUi()
    {
        commandTypeDropdown = transform.Find("CommandTypeDropdown")?.GetComponent<TMP_Dropdown>();
        authorityDropdown = transform.Find("AuthorityModeDropdown")?.GetComponent<TMP_Dropdown>();
        previewButton = transform.Find("PreviewButton")?.GetComponent<Button>();
        applyButton = transform.Find("ApplyButton")?.GetComponent<Button>();
        closeButton = transform.Find("CloseButton")?.GetComponent<Button>();
        formContent = transform.Find("DynamicFormArea/Viewport/Content") as RectTransform;
        previewContent = transform.Find("PreviewAndHistory/PreviewPanel/Viewport/Content") as RectTransform;
        historyContent = transform.Find("PreviewAndHistory/CommandHistoryPanel/Viewport/Content") as RectTransform;
        previewText = transform.Find("PreviewAndHistory/PreviewPanel/Viewport/Content/PreviewText")?.GetComponent<TMP_Text>();

        if (commandTypeDropdown == null
            || authorityDropdown == null
            || previewButton == null
            || applyButton == null
            || closeButton == null
            || formContent == null
            || previewContent == null
            || historyContent == null
            || previewText == null)
        {
            return false;
        }

        inputs.Clear();
        foreach (TMP_InputField input in GetComponentsInChildren<TMP_InputField>(true))
        {
            const string suffix = "Input";
            if (input.name.EndsWith(suffix, StringComparison.Ordinal))
            {
                inputs[input.name.Substring(0, input.name.Length - suffix.Length)] = input;
            }
        }

        dropdowns.Clear();
        foreach (TMP_Dropdown dropdown in GetComponentsInChildren<TMP_Dropdown>(true))
        {
            const string suffix = "Dropdown";
            if (dropdown != commandTypeDropdown
                && dropdown != authorityDropdown
                && dropdown.name.EndsWith(suffix, StringComparison.Ordinal))
            {
                dropdowns[dropdown.name.Substring(0, dropdown.name.Length - suffix.Length)] = dropdown;
            }
        }

        formGroups.Clear();
        CacheFormGroup("RelocateNpcForm");
        CacheFormGroup("ContentForm");
        CacheFormGroup("LocalTopologyForm");
        CacheFormGroup("KnowledgeForm");
        CacheFormGroup("ConflictForm");
        closeButton.onClick.RemoveListener(Hide);
        closeButton.onClick.AddListener(Hide);
        previewButton.onClick.RemoveListener(PreviewCurrentCommand);
        previewButton.onClick.AddListener(PreviewCurrentCommand);
        applyButton.onClick.RemoveListener(ApplyCurrentCommand);
        applyButton.onClick.AddListener(ApplyCurrentCommand);
        commandTypeDropdown.onValueChanged.RemoveListener(_ => SetVisibleForm());
        commandTypeDropdown.onValueChanged.AddListener(_ => SetVisibleForm());
        return inputs.Count > 0 && dropdowns.Count > 0;
    }

    private void CacheFormGroup(string groupName)
    {
        Transform group = formContent?.Find(groupName);
        if (group != null)
        {
            formGroups[groupName] = group.gameObject;
        }
    }

    private void BuildHeader()
    {
        TMP_Text title = WorldObserverUiFactory.CreateText(transform, "Title", 22f, TextAlignmentOptions.MidlineLeft);
        title.text = "GM Console · Structured WorldCommands";
        WorldObserverUiFactory.SetAnchors(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -48f), new Vector2(-150f, -10f));

        closeButton = WorldObserverUiFactory.CreateButton(transform, "CloseButton", "Close", new Vector2(110f, 34f));
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        closeButton.onClick.AddListener(Hide);

        commandTypeDropdown = CreateDropdown(transform, "CommandTypeDropdown", "Command");
        SetDropdownOptions(commandTypeDropdown, ToLabels(CommandKinds));
        PlaceHeaderControl(commandTypeDropdown, 210f, 46f);
        commandTypeDropdown.onValueChanged.AddListener(_ => SetVisibleForm());

        authorityDropdown = CreateDropdown(transform, "AuthorityModeDropdown", "Authority");
        SetDropdownOptions(authorityDropdown, ToLabels(AuthorityModes));
        authorityDropdown.value = 2;
        authorityDropdown.RefreshShownValue();
        PlaceHeaderControl(authorityDropdown, 410f, 46f);

        previewButton = WorldObserverUiFactory.CreateButton(transform, "PreviewButton", "Preview", new Vector2(110f, 34f));
        PlaceHeaderButton(previewButton, 610f);
        previewButton.onClick.AddListener(PreviewCurrentCommand);
        applyButton = WorldObserverUiFactory.CreateButton(transform, "ApplyButton", "Apply", new Vector2(110f, 34f));
        PlaceHeaderButton(applyButton, 730f);
        applyButton.onClick.AddListener(ApplyCurrentCommand);
        applyButton.interactable = false;
    }

    private void BuildForms()
    {
        formContent = WorldObserverUiFactory.CreateScrollContent(transform, "DynamicFormArea", 62f);
        WorldObserverUiFactory.SetAnchors(formContent, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(14f, 14f), new Vector2(-8f, -62f));

        GameObject relocate = CreateGroup("RelocateNpcForm");
        AddInput(relocate.transform, "relocate.npc", "NPC RuntimeId");
        AddInput(relocate.transform, "relocate.destination", "Destination macro LocationRuntimeId");

        GameObject content = CreateGroup("ContentForm");
        AddDropdown(content.transform, "content.ownerKind", "Owner kind", ToLabels(new[] { PlaceContentOwnerKind.City, PlaceContentOwnerKind.ExplorableSite, PlaceContentOwnerKind.LocalPlace }));
        AddInput(content.transform, "content.owner", "Owner RuntimeId");
        AddInput(content.transform, "content.macro", "Macro LocationRuntimeId");
        AddInput(content.transform, "content.topologyOwner", "Topology owner RuntimeId (local only)");
        AddInput(content.transform, "content.item", "Item DefinitionId");
        AddInput(content.transform, "content.amount", "Amount");
        AddDropdown(content.transform, "content.persistence", "Persistence", ToLabels(new[] { PlaceContentPersistencePolicy.Transient, PlaceContentPersistencePolicy.Perishable, PlaceContentPersistencePolicy.Durable }));
        AddInput(content.transform, "content.decay", "Decay per day");
        AddInput(content.transform, "content.averageCost", "Average unit cost (optional)");

        GameObject topology = CreateGroup("LocalTopologyForm");
        AddInput(topology.transform, "topology.owner", "Topology owner RuntimeId");
        AddInput(topology.transform, "topology.displayName", "Display name");
        AddInput(topology.transform, "topology.placeType", "Place type DefinitionId (optional)");
        AddInput(topology.transform, "topology.parent", "Parent LocalPlaceRuntimeId (optional)");
        AddDropdown(topology.transform, "topology.entryPoint", "Entry point", new[] { "False", "True" });
        AddInput(topology.transform, "connection.origin", "Origin LocalPlaceRuntimeId");
        AddInput(topology.transform, "connection.destination", "Destination LocalPlaceRuntimeId");
        AddInput(topology.transform, "connection.cost", "Traversal cost");
        AddInput(topology.transform, "connection.type", "Connection type DefinitionId (optional)");

        GameObject knowledge = CreateGroup("KnowledgeForm");
        AddInput(knowledge.transform, "knowledge.npc", "NPC RuntimeId");
        AddInput(knowledge.transform, "knowledge.site", "Site RuntimeId");
        AddDropdown(knowledge.transform, "knowledge.source", "Site knowledge source", ToLabels(new[] { ExplorableSiteKnowledgeSource.InitialScenarioKnowledge, ExplorableSiteKnowledgeSource.DirectObservation }));
        AddDropdown(knowledge.transform, "intel.kind", "Intel kind", ToLabels(new[] { AdventureIntelDeclarationKind.Opposition, AdventureIntelDeclarationKind.NotableItem, AdventureIntelDeclarationKind.CommonResource, AdventureIntelDeclarationKind.Access }));
        AddInput(knowledge.transform, "intel.place", "Observed LocalPlaceRuntimeId (optional)");
        AddInput(knowledge.transform, "intel.opposition", "OppositionRuntimeId (optional)");
        AddDropdown(knowledge.transform, "intel.oppositionState", "Opposition state", ToLabels(new[] { AdventureOppositionObservedState.Active, AdventureOppositionObservedState.Resolved }));
        AddInput(knowledge.transform, "intel.notable", "NotableItemRuntimeId (optional)");
        AddInput(knowledge.transform, "intel.item", "Item DefinitionId (optional)");
        AddInput(knowledge.transform, "intel.amount", "Observed amount (optional)");
        AddDropdown(knowledge.transform, "intel.access", "Observed access", ToLabels(new[] { PlaceAccessState.Inaccessible, PlaceAccessState.Contested, PlaceAccessState.Accessible }));

        GameObject conflict = CreateGroup("ConflictForm");
        AddInput(conflict.transform, "conflict.location", "LocationRuntimeId (optional)");
        AddInput(conflict.transform, "conflict.opposition", "Place OppositionRuntimeId (optional)");
        AddInput(conflict.transform, "conflict.attacker", "Attacker NPC RuntimeId");
        AddInput(conflict.transform, "conflict.defender", "Defender NPC RuntimeId");
        AddInput(conflict.transform, "conflict.forcedWinner", "Forced winning SideId (optional)");

        SetVisibleForm();
    }

    private void BuildPreviewAndHistory()
    {
        RectTransform right = WorldObserverUiFactory.CreatePanel(transform, "PreviewAndHistory", new Color(0.07f, 0.10f, 0.13f, 1f));
        WorldObserverUiFactory.SetAnchors(right, new Vector2(0.53f, 0f), new Vector2(1f, 1f), new Vector2(8f, 14f), new Vector2(-14f, -62f));
        TMP_Text previewTitle = WorldObserverUiFactory.CreateText(right, "PreviewTitle", 18f, TextAlignmentOptions.MidlineLeft);
        previewTitle.text = "Preview / Result";
        WorldObserverUiFactory.SetAnchors(previewTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -40f), new Vector2(-12f, -4f));
        previewContent = WorldObserverUiFactory.CreateScrollContent(right, "PreviewPanel", 46f);
        WorldObserverUiFactory.SetAnchors(previewContent, new Vector2(0f, 0.48f), new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(-8f, -46f));
        previewText = WorldObserverUiFactory.CreateText(previewContent, "PreviewText", 14f, TextAlignmentOptions.TopLeft);
        previewText.enableWordWrapping = true;
        previewText.text = "Choose a structured command and press Preview.";
        LayoutElement previewLayout = previewText.gameObject.AddComponent<LayoutElement>();
        previewLayout.minHeight = 150f;
        previewLayout.preferredHeight = 220f;

        TMP_Text historyTitle = WorldObserverUiFactory.CreateText(right, "HistoryTitle", 18f, TextAlignmentOptions.MidlineLeft);
        historyTitle.text = "Command History";
        WorldObserverUiFactory.SetAnchors(historyTitle.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.48f), new Vector2(12f, -32f), new Vector2(-12f, 0f));
        historyContent = WorldObserverUiFactory.CreateScrollContent(right, "CommandHistoryPanel", 0f);
        WorldObserverUiFactory.SetAnchors(historyContent, new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(8f, 8f), new Vector2(-8f, -32f));
    }

    private GameObject CreateGroup(string groupName)
    {
        GameObject group = WorldObserverUiFactory.CreateUiObject(groupName, formContent);
        LayoutElement layout = group.AddComponent<LayoutElement>();
        layout.minHeight = 40f;
        layout.preferredHeight = 40f;
        VerticalLayoutGroup vertical = group.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 3f;
        vertical.padding = new RectOffset(4, 4, 4, 4);
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        formGroups.Add(groupName, group);
        return group;
    }

    private TMP_InputField AddInput(Transform parent, string key, string label)
    {
        GameObject row = CreateRow(parent, label);
        GameObject inputObject = WorldObserverUiFactory.CreateUiObject(key + "Input", row.transform);
        Image background = inputObject.AddComponent<Image>();
        background.color = new Color(0.10f, 0.14f, 0.18f, 1f);
        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        RectTransform inputRect = (RectTransform)inputObject.transform;
        inputRect.sizeDelta = new Vector2(0f, 28f);
        TextMeshProUGUI text = (TextMeshProUGUI)WorldObserverUiFactory.CreateText(inputObject.transform, "Text", 13f, TextAlignmentOptions.MidlineLeft);
        text.enableWordWrapping = false;
        input.textComponent = text;
        input.text = string.Empty;
        inputs[key] = input;
        return input;
    }

    private TMP_Dropdown AddDropdown(Transform parent, string key, string label, IEnumerable<string> options)
    {
        GameObject row = CreateRow(parent, label);
        TMP_Dropdown dropdown = CreateDropdown(row.transform, key + "Dropdown", label);
        SetDropdownOptions(dropdown, options);
        dropdowns[key] = dropdown;
        return dropdown;
    }

    private GameObject CreateRow(Transform parent, string label)
    {
        GameObject row = WorldObserverUiFactory.CreateUiObject(label + "Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 54f;
        rowLayout.preferredHeight = 54f;
        VerticalLayoutGroup vertical = row.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 1f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        WorldObserverUiFactory.CreateText(row.transform, "Label", 12f, TextAlignmentOptions.MidlineLeft).text = label;
        return row;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string objectName, string label)
    {
        GameObject dropdownObject = WorldObserverUiFactory.CreateUiObject(objectName, parent);
        Image background = dropdownObject.AddComponent<Image>();
        background.color = new Color(0.10f, 0.14f, 0.18f, 1f);
        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
        ((RectTransform)dropdownObject.transform).sizeDelta = new Vector2(0f, 30f);
        TextMeshProUGUI caption = (TextMeshProUGUI)WorldObserverUiFactory.CreateText(dropdownObject.transform, "Caption", 13f, TextAlignmentOptions.MidlineLeft);
        WorldObserverUiFactory.Stretch(caption.rectTransform, 8f, 3f, 8f, 3f);
        dropdown.captionText = caption;

        GameObject templateObject = WorldObserverUiFactory.CreateUiObject("Template", dropdownObject.transform);
        RectTransform template = (RectTransform)templateObject.transform;
        template.sizeDelta = new Vector2(0f, 160f);
        templateObject.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.12f, 1f);
        GameObject viewportObject = WorldObserverUiFactory.CreateUiObject("Viewport", templateObject.transform);
        WorldObserverUiFactory.Stretch((RectTransform)viewportObject.transform, 2f, 2f, 2f, 2f);
        viewportObject.AddComponent<RectMask2D>();
        GameObject contentObject = WorldObserverUiFactory.CreateUiObject("Content", viewportObject.transform);
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        GameObject itemObject = WorldObserverUiFactory.CreateUiObject("Item", contentObject.transform);
        Toggle toggle = itemObject.AddComponent<Toggle>();
        toggle.isOn = false;
        TextMeshProUGUI itemText = (TextMeshProUGUI)WorldObserverUiFactory.CreateText(itemObject.transform, "ItemLabel", 13f, TextAlignmentOptions.MidlineLeft);
        WorldObserverUiFactory.Stretch(itemText.rectTransform, 8f, 2f, 8f, 2f);
        toggle.targetGraphic = itemObject.GetComponent<Image>() ?? itemObject.AddComponent<Image>();
        dropdown.template = template;
        dropdown.itemText = itemText;
        templateObject.SetActive(false);
        return dropdown;
    }

    private void SetVisibleForm()
    {
        if (commandTypeDropdown == null)
        {
            return;
        }

        WorldCommandKind kind = GetCommandKind();
        SetGroupActive("RelocateNpcForm", kind == WorldCommandKind.RelocateNpc);
        SetGroupActive("ContentForm", kind == WorldCommandKind.DeclareStackResource || kind == WorldCommandKind.DeclareNotableItem);
        SetGroupActive("LocalTopologyForm", kind == WorldCommandKind.AddLocalPlace || kind == WorldCommandKind.AddLocalConnection);
        SetGroupActive("KnowledgeForm", kind == WorldCommandKind.GrantSiteKnowledge || kind == WorldCommandKind.GrantAdventureIntel);
        SetGroupActive("ConflictForm", kind == WorldCommandKind.ResolveConflict || kind == WorldCommandKind.PlaceOpposition);
    }

    private void SetGroupActive(string groupName, bool active)
    {
        if (formGroups.TryGetValue(groupName, out GameObject group) && group != null)
        {
            group.SetActive(active);
        }
    }

    private WorldCommandKind GetCommandKind()
    {
        int index = commandTypeDropdown == null ? 0 : Mathf.Clamp(commandTypeDropdown.value, 0, CommandKinds.Length - 1);
        return CommandKinds[index];
    }

    private WorldCommandAuthorityMode GetAuthority()
    {
        int index = authorityDropdown == null ? 2 : Mathf.Clamp(authorityDropdown.value, 0, AuthorityModes.Length - 1);
        return AuthorityModes[index];
    }

    private string Value(string key)
    {
        return inputs.TryGetValue(key, out TMP_InputField input) ? input.text.Trim() : string.Empty;
    }

    private string OptionalValue(string key)
    {
        string value = Value(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private int ParsePositiveInt(string key)
    {
        if (int.TryParse(Value(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) == false || value <= 0)
        {
            throw new ArgumentException("Amount must be a positive integer.");
        }

        return value;
    }

    private int ParseNonNegativeInt(string key)
    {
        string raw = Value(key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) == false || value < 0)
        {
            throw new ArgumentException("Value must be a non-negative integer.");
        }

        return value;
    }

    private float ParsePositiveFloat(string key)
    {
        if (float.TryParse(Value(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) == false
            || LocalTopologyConnectionRuntime.IsValidTraversalCost(value) == false)
        {
            throw new ArgumentException("Traversal cost must be a positive finite number.");
        }

        return value;
    }

    private float? ParseOptionalFloat(string key)
    {
        string raw = Value(key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) == false || value < 0f || float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentException("Average unit cost must be a non-negative finite number.");
        }

        return value;
    }

    private T GetEnum<T>(string key, T fallback) where T : struct
    {
        if (dropdowns.TryGetValue(key, out TMP_Dropdown dropdown) == false || dropdown.options.Count == 0)
        {
            return fallback;
        }

        string label = dropdown.options[Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1)].text;
        if (Enum.TryParse(label, out T parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private bool GetDropdownValue(string key)
    {
        return dropdowns.TryGetValue(key, out TMP_Dropdown dropdown) && dropdown.value == 1;
    }

    private WorldContentOwnerReferencePayload BuildOwnerPayload()
    {
        PlaceContentOwnerKind ownerKind = GetEnum("content.ownerKind", PlaceContentOwnerKind.ExplorableSite);
        return new WorldContentOwnerReferencePayload(
            ownerKind,
            Value("content.owner"),
            Value("content.macro"),
            OptionalValue("content.topologyOwner"));
    }

    private GrantAdventureIntelWorldCommandPayload BuildAdventureIntelPayload()
    {
        AdventureIntelDeclarationKind kind = GetEnum("intel.kind", AdventureIntelDeclarationKind.Opposition);
        string itemDefinitionId = OptionalValue("intel.item");
        int? amount = null;
        string amountRaw = OptionalValue("intel.amount");
        if (amountRaw != null)
        {
            if (int.TryParse(amountRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedAmount) == false || parsedAmount < 0)
            {
                throw new ArgumentException("Observed amount must be a non-negative integer.");
            }

            amount = parsedAmount;
        }

        return new GrantAdventureIntelWorldCommandPayload(
            Value("knowledge.npc"),
            kind,
            Value("knowledge.site"),
            OptionalValue("intel.place"),
            OptionalValue("intel.opposition"),
            GetEnum("intel.oppositionState", AdventureOppositionObservedState.Active),
            OptionalValue("intel.notable"),
            itemDefinitionId,
            amount,
            GetEnum("intel.access", PlaceAccessState.Accessible),
            0L,
            0L,
            AdventureIntelSource.DirectObservation);
    }

    private ResolveConflictWorldCommandPayload BuildConflictPayload(WorldCommandKind kind)
    {
        string attackerRuntimeId = Value("conflict.attacker");
        string defenderRuntimeId = Value("conflict.defender");
        string oppositionSideId = kind == WorldCommandKind.PlaceOpposition ? "opposition" : "defender";
        return new ResolveConflictWorldCommandPayload(
            OptionalValue("conflict.location"),
            new[]
            {
                new WorldConflictSidePayload(
                    "attacker",
                    ConflictObjectiveType.Defeat,
                    ConflictStakes.Meaningful,
                    new[] { new WorldConflictParticipantPayload(attackerRuntimeId) }),
                new WorldConflictSidePayload(
                    oppositionSideId,
                    ConflictObjectiveType.Defend,
                    ConflictStakes.Meaningful,
                    new[] { new WorldConflictParticipantPayload(defenderRuntimeId) })
            },
            OptionalValue("conflict.opposition"),
            OptionalValue("conflict.forcedWinner"));
    }

    private void ApplySelectedPlaceDefault()
    {
        if (context == null || context.SelectedPlaceRuntimeId == null || !inputs.ContainsKey("content.owner"))
        {
            return;
        }

        string selected = context.SelectedPlaceRuntimeId();
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        if (context.IdentityRegistry != null && context.IdentityRegistry.TryGetLocalPlace(selected, out LocalPlaceRuntime localPlace) && localPlace?.OwningTopology != null)
        {
            SetInput("content.owner", localPlace.RuntimeId);
            SetInput("content.macro", localPlace.OwningTopology.Owner.MacroLocationRuntimeId);
            SetInput("content.topologyOwner", localPlace.OwningTopology.Owner.OwnerRuntimeId);
            SetDropdownByText("content.ownerKind", PlaceContentOwnerKind.LocalPlace.ToString());
            SetInput("topology.owner", localPlace.OwningTopology.Owner.OwnerRuntimeId);
            SetInput("topology.parent", localPlace.RuntimeId);
            return;
        }

        if (context.SiteStore != null && context.SiteStore.TryGetByRuntimeId(selected, out ExplorableSiteRuntime site) && site != null)
        {
            SetInput("content.owner", site.RuntimeId);
            SetInput("content.macro", site.Location.RuntimeId);
            SetDropdownByText("content.ownerKind", PlaceContentOwnerKind.ExplorableSite.ToString());
            SetInput("topology.owner", site.RuntimeId);
            SetInput("knowledge.site", site.RuntimeId);
            SetInput("intel.place", string.Empty);
            return;
        }

        if (context.IdentityRegistry != null && context.IdentityRegistry.TryGetCity(selected, out CityRuntime city) && city != null)
        {
            SetInput("content.owner", city.RuntimeId);
            SetInput("content.macro", city.Location.RuntimeId);
            SetDropdownByText("content.ownerKind", PlaceContentOwnerKind.City.ToString());
        }
    }

    private void RefreshDefinitionDefaults()
    {
        WorldCommandDefinitionCatalog catalog = context?.DefinitionResolver as WorldCommandDefinitionCatalog;
        if (catalog == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Value("content.item")))
        {
            foreach (string definitionId in catalog.ItemDefinitionIds)
            {
                SetInput("content.item", definitionId);
                SetInput("intel.item", definitionId);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(Value("content.amount")))
        {
            SetInput("content.amount", "1");
        }

        if (string.IsNullOrWhiteSpace(Value("content.decay")))
        {
            SetInput("content.decay", "0");
        }

        if (string.IsNullOrWhiteSpace(Value("connection.cost")))
        {
            SetInput("connection.cost", "1");
        }

        if (string.IsNullOrWhiteSpace(Value("knowledge.npc")) && context.Npcs.Count > 0)
        {
            SetInput("knowledge.npc", context.Npcs[0].RuntimeId);
        }

        if (string.IsNullOrWhiteSpace(Value("relocate.npc")) && context.Npcs.Count > 0)
        {
            SetInput("relocate.npc", context.Npcs[0].RuntimeId);
        }

        if (string.IsNullOrWhiteSpace(Value("relocate.destination")) && context.Cities.Count > 0)
        {
            SetInput("relocate.destination", context.Cities[0].Location.RuntimeId);
        }

        if (string.IsNullOrWhiteSpace(Value("content.owner")) && context.SiteStore != null)
        {
            foreach (ExplorableSiteRuntime site in context.SiteStore.Sites)
            {
                if (site == null)
                {
                    continue;
                }

                SetInput("content.owner", site.RuntimeId);
                SetInput("content.macro", site.Location.RuntimeId);
                SetDropdownByText("content.ownerKind", PlaceContentOwnerKind.ExplorableSite.ToString());
                SetInput("topology.owner", site.RuntimeId);
                SetInput("knowledge.site", site.RuntimeId);
                break;
            }
        }
    }

    private void RefreshCommandHistory()
    {
        if (historyContent == null)
        {
            return;
        }

        WorldObserverUiFactory.DestroyChildren(historyContent);
        IReadOnlyList<WorldCommandRecord> records = context?.CommandService?.RecordStore?.Records;
        if (records == null || records.Count == 0)
        {
            WorldObserverUiFactory.CreateText(historyContent, "EmptyHistory", 13f, TextAlignmentOptions.TopLeft).text = "No commands recorded.";
            return;
        }

        for (int i = records.Count - 1; i >= 0; i--)
        {
            WorldCommandRecord record = records[i];
            if (record == null)
            {
                continue;
            }

            TMP_Text row = WorldObserverUiFactory.CreateText(historyContent, "CommandRow_" + i, 12f, TextAlignmentOptions.TopLeft);
            row.enableWordWrapping = true;
            row.text = "Day " + record.AbsoluteDay
                + " · " + record.Kind
                + " · " + record.Origin
                + " · " + record.Authority
                + " · " + (record.Success ? "SUCCESS" : "REJECTED");
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 28f;
            layout.preferredHeight = 34f;
        }
    }

    private string FormatPreview(WorldCommandPreview preview)
    {
        if (preview == null)
        {
            return "No preview available.";
        }

        string result = preview.Kind + " · " + preview.Authority + "\n"
            + (preview.IsValid ? "VALID" : "INVALID") + "\n"
            + preview.Presentation;
        if (preview.AffectedRuntimeIds.Count > 0)
        {
            result += "\nAffected: " + string.Join(", ", preview.AffectedRuntimeIds);
        }

        if (preview.Warnings.Count > 0)
        {
            result += "\nWarnings: " + string.Join(" | ", preview.Warnings);
        }

        if (string.IsNullOrWhiteSpace(preview.PendingCreatedRuntimeId) == false)
        {
            result += "\nPending created identity: " + preview.PendingCreatedRuntimeId;
        }

        return result;
    }

    private static string FormatResult(WorldCommandResult result)
    {
        if (result == null)
        {
            return "No command result.";
        }

        string text = result.Kind + " · " + (result.Success ? "APPLIED" : "REJECTED") + "\n"
            + "WorldCommandId: " + result.WorldCommandId;
        if (string.IsNullOrWhiteSpace(result.Diagnostic) == false)
        {
            text += "\n" + result.Diagnostic;
        }

        if (result.CreatedRuntimeIds.Count > 0)
        {
            text += "\nCreated: " + string.Join(", ", result.CreatedRuntimeIds);
        }

        return text;
    }

    private void SetPreviewMessage(string message)
    {
        if (previewText != null)
        {
            previewText.text = message ?? string.Empty;
        }
    }

    private void SetInput(string key, string value)
    {
        if (inputs.TryGetValue(key, out TMP_InputField input))
        {
            input.text = value ?? string.Empty;
        }
    }

    private TMP_Dropdown FindChildDropdown(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TMP_Dropdown>();
    }

    private void SetDropdownByText(string key, string value)
    {
        if (dropdowns.TryGetValue(key, out TMP_Dropdown dropdown) == false)
        {
            return;
        }

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (string.Equals(dropdown.options[i].text, value, StringComparison.Ordinal))
            {
                dropdown.value = i;
                dropdown.RefreshShownValue();
                return;
            }
        }
    }

    private static void PlaceHeaderControl(TMP_Dropdown dropdown, float x, float width)
    {
        RectTransform rect = (RectTransform)dropdown.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, 32f);
        rect.anchoredPosition = new Vector2(x, -12f);
    }

    private static void PlaceHeaderButton(Button button, float x)
    {
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -12f);
    }

    private static void SetDropdownOptions(TMP_Dropdown dropdown, IEnumerable<string> options)
    {
        dropdown.ClearOptions();
        List<string> captured = new List<string>(options ?? Array.Empty<string>());
        if (captured.Count == 0)
        {
            captured.Add("<none>");
        }

        dropdown.AddOptions(captured);
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    private static string[] ToLabels<T>(IEnumerable<T> values)
    {
        List<string> labels = new List<string>();
        foreach (T value in values)
        {
            labels.Add(value.ToString());
        }

        return labels.ToArray();
    }
}
