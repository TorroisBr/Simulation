using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class GMConsolePanelTests
{
    private ConsoleFixture fixture;

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
        fixture = new ConsoleFixture();
    }

    [TearDown]
    public void TearDown()
    {
        fixture?.Dispose();
        fixture = null;
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void ObserverPrefabContainsGmConsole()
    {
        GameObject prefab = LoadPrefab();
        WorldObserverCanvasView view = prefab.GetComponentInChildren<WorldObserverCanvasView>(true);

        Assert.That(view.GmConsoleButton, Is.Not.Null);
        Assert.That(view.GmConsolePanel, Is.Not.Null);
        Assert.That(view.GmConsolePanel.GetComponentInChildren<TMP_Dropdown>(true), Is.Not.Null);
    }

    [Test]
    public void GmConsoleUsesTmpForAllTextAndInputs()
    {
        GMConsolePanel panel = LoadPrefab().GetComponentInChildren<GMConsolePanel>(true);

        Assert.That(panel.GetComponentsInChildren<TMP_Text>(true), Is.Not.Empty);
        Assert.That(panel.GetComponentsInChildren<TMP_InputField>(true), Is.Not.Empty);
        Assert.That(panel.GetComponentsInChildren<TMP_Dropdown>(true), Is.Not.Empty);
    }

    [Test]
    public void GmConsoleContainsNoLegacyUiText()
    {
        GMConsolePanel panel = LoadPrefab().GetComponentInChildren<GMConsolePanel>(true);

        Assert.That(panel.GetComponentsInChildren<Text>(true), Is.Empty);
        Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.InputField>(true), Is.Empty);
        Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Dropdown>(true), Is.Empty);
    }

    [Test]
    public void CommandTypeDropdownUsesTmp()
    {
        GMConsolePanel panel = LoadPrefab().GetComponentInChildren<GMConsolePanel>(true);

        Assert.That(panel.CommandTypeDropdown, Is.Not.Null);
        Assert.That(panel.CommandTypeDropdown, Is.TypeOf<TMP_Dropdown>());
    }

    [Test]
    public void AuthorityDropdownUsesTmp()
    {
        GMConsolePanel panel = LoadPrefab().GetComponentInChildren<GMConsolePanel>(true);

        Assert.That(panel.AuthorityDropdown, Is.Not.Null);
        Assert.That(panel.AuthorityDropdown, Is.TypeOf<TMP_Dropdown>());
    }

    [Test]
    public void PreviewButtonDoesNotMutateWorld()
    {
        SetStackResourceForm();
        int placesBefore = fixture.Content.Places.Count;

        fixture.Panel.PreviewButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastPreview.IsValid, Is.True);
        Assert.That(fixture.Content.Places.Count, Is.EqualTo(placesBefore));
        Assert.That(fixture.Content.TryGet(fixture.Site, out PlaceContentRuntime content), Is.True);
        Assert.That(content.StackedContent, Is.Empty);
        Assert.That(fixture.Service.RecordStore.Records, Is.Empty);
    }

    [Test]
    public void PreviewButtonShowsValidationResult()
    {
        SetStackResourceForm();
        SetInput("content.item", "missing-item-definition");

        fixture.Panel.PreviewButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastPreview.IsValid, Is.False);
        StringAssert.Contains("INVALID", fixture.Panel.PreviewText.text);
    }

    [Test]
    public void GmConsoleInvalidAuthorityPreviewDisablesApply()
    {
        SetStackResourceForm();
        SelectAuthority(WorldCommandAuthorityMode.Request);

        fixture.Panel.PreviewButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastPreview.IsValid, Is.False);
        Assert.That(fixture.Panel.ApplyButton.interactable, Is.False);
        Assert.That(fixture.Content.Places[0].StackedContent, Is.Empty);
    }

    [Test]
    public void GmConsoleDeclareRelocateCanStillPreviewAndApply()
    {
        SelectKind(WorldCommandKind.RelocateNpc);
        SelectAuthority(WorldCommandAuthorityMode.Declare);
        SetInput("relocate.npc", fixture.Actor.RuntimeId);
        SetInput("relocate.destination", fixture.Site.Location.RuntimeId);

        fixture.Panel.PreviewButton.onClick.Invoke();
        Assert.That(fixture.Panel.LastPreview.IsValid, Is.True, fixture.Panel.LastPreview.Presentation);
        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastResult.Success, Is.True, fixture.Panel.LastResult.Diagnostic);
        Assert.That(fixture.Actor.CurrentLocation, Is.SameAs(fixture.Site.Location));
    }

    [Test]
    public void GmConsoleRequestConflictCanPreviewAndApply()
    {
        SelectKind(WorldCommandKind.ResolveConflict);
        SelectAuthority(WorldCommandAuthorityMode.Request);
        SetInput("conflict.location", fixture.Site.Location.RuntimeId);
        SetInput("conflict.attacker", fixture.Actor.RuntimeId);
        SetInput("conflict.defender", fixture.Defender.RuntimeId);

        fixture.Panel.PreviewButton.onClick.Invoke();
        Assert.That(fixture.Panel.LastPreview.IsValid, Is.True);
        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastResult.Success, Is.True, fixture.Panel.LastResult.Diagnostic);
        Assert.That(fixture.Service.RecordStore.Records, Has.Count.EqualTo(1));
    }

    [Test]
    public void GmConsoleForceOutcomeConflictCanPreviewWhenConstraintsValid()
    {
        SelectKind(WorldCommandKind.ResolveConflict);
        SelectAuthority(WorldCommandAuthorityMode.ForceOutcome);
        SetInput("conflict.location", fixture.Site.Location.RuntimeId);
        SetInput("conflict.attacker", fixture.Actor.RuntimeId);
        SetInput("conflict.defender", fixture.Defender.RuntimeId);
        SetInput("conflict.forcedWinner", "attacker");

        fixture.Panel.PreviewButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastPreview.IsValid, Is.True, fixture.Panel.LastPreview.Presentation);
        Assert.That(fixture.Panel.ApplyButton.interactable, Is.True);
    }

    [Test]
    public void ApplyButtonUsesWorldCommandService()
    {
        SetStackResourceForm();
        fixture.Panel.PreviewButton.onClick.Invoke();
        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.Panel.CommandService, Is.SameAs(fixture.Service));
        Assert.That(fixture.Panel.LastResult, Is.Not.Null);
        Assert.That(fixture.Panel.LastResult.Success, Is.True);
        Assert.That(fixture.Service.RecordStore.Records, Has.Count.EqualTo(1));
    }

    [Test]
    public void ApplyRevalidatesAfterPreview()
    {
        SetStackResourceForm();
        fixture.Panel.PreviewButton.onClick.Invoke();
        Assert.That(fixture.Panel.LastPreview.IsValid, Is.True);
        Assert.That(fixture.Content.TryAddStack(
            fixture.Site,
            fixture.Item,
            1,
            PlaceContentPersistencePolicy.Transient,
            out _,
            0,
            0f), Is.True);

        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastResult.Success, Is.False);
        Assert.That(fixture.Service.RecordStore.Records, Has.Count.EqualTo(1));
    }

    [Test]
    public void SuccessfulApplyRefreshesObserver()
    {
        SetStackResourceForm();
        fixture.Panel.PreviewButton.onClick.Invoke();
        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.RefreshCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedApplyDoesNotPartiallyRefreshFalseState()
    {
        SetStackResourceForm();
        fixture.Panel.PreviewButton.onClick.Invoke();
        Assert.That(fixture.Panel.LastPreview.IsValid, Is.True);
        Assert.That(fixture.Content.TryAddStack(
            fixture.Site,
            fixture.Item,
            1,
            PlaceContentPersistencePolicy.Transient,
            out _,
            0,
            0f), Is.True);

        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.Panel.LastResult.Success, Is.False);
        Assert.That(fixture.RefreshCount, Is.Zero);
        Assert.That(fixture.Service.RecordStore.Records[0].Success, Is.False);
    }

    [Test]
    public void SelectedObserverPlaceCanPopulateCommandTarget()
    {
        fixture.SelectedPlaceRuntimeId = fixture.Site.RuntimeId;
        fixture.Panel.Bind(fixture.Context);

        Assert.That(fixture.Panel.GetInput("content.owner").text, Is.EqualTo(fixture.Site.RuntimeId));
        Assert.That(fixture.Panel.GetInput("content.macro").text, Is.EqualTo(fixture.Site.Location.RuntimeId));
        Assert.That(fixture.Panel.GetInput("knowledge.site").text, Is.EqualTo(fixture.Site.RuntimeId));
    }

    [Test]
    public void DeclareResourceFormCreatesTypedCommand()
    {
        SetStackResourceForm();

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        Assert.That(command.Payload, Is.TypeOf<DeclareStackResourceWorldCommandPayload>());
    }

    [Test]
    public void DeclareNotableFormCreatesTypedCommand()
    {
        SelectKind(WorldCommandKind.DeclareNotableItem);
        SetOwnerFields();
        SetInput("content.item", fixture.Item.DefinitionId);

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        Assert.That(command.Payload, Is.TypeOf<DeclareNotableItemWorldCommandPayload>());
    }

    [Test]
    public void RelocateNpcFormCreatesTypedCommand()
    {
        SelectKind(WorldCommandKind.RelocateNpc);
        SetInput("relocate.npc", fixture.Actor.RuntimeId);
        SetInput("relocate.destination", fixture.Site.Location.RuntimeId);

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        RelocateNpcWorldCommandPayload payload = command.Payload as RelocateNpcWorldCommandPayload;
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload.NpcRuntimeId, Is.EqualTo(fixture.Actor.RuntimeId));
    }

    [Test]
    public void AddLocalPlaceFormCreatesTypedCommand()
    {
        SelectKind(WorldCommandKind.AddLocalPlace);
        SetInput("topology.owner", fixture.Site.RuntimeId);
        SetInput("topology.displayName", "Hidden Chamber");
        SetInput("topology.placeType", fixture.PlaceType.DefinitionId);
        SetInput("topology.parent", fixture.Entry.RuntimeId);
        SetFormDropdown("topology.entryPoint", "False");

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        Assert.That(command.Payload, Is.TypeOf<AddLocalPlaceWorldCommandPayload>());
    }

    [Test]
    public void AddLocalConnectionFormCreatesTypedCommand()
    {
        SelectKind(WorldCommandKind.AddLocalConnection);
        SetInput("topology.owner", fixture.Site.RuntimeId);
        SetInput("connection.origin", fixture.Entry.RuntimeId);
        SetInput("connection.destination", fixture.Exit.RuntimeId);
        SetInput("connection.cost", "2.5");
        SetInput("connection.type", fixture.ConnectionType.DefinitionId);

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        Assert.That(command.Payload, Is.TypeOf<AddLocalConnectionWorldCommandPayload>());
    }

    [Test]
    public void KnowledgeFormCreatesTypedCommand()
    {
        SelectKind(WorldCommandKind.GrantSiteKnowledge);
        SetInput("knowledge.npc", fixture.Actor.RuntimeId);
        SetInput("knowledge.site", fixture.Site.RuntimeId);
        SetFormDropdown("knowledge.source", ExplorableSiteKnowledgeSource.DirectObservation.ToString());

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        Assert.That(command.Payload, Is.TypeOf<GrantSiteKnowledgeWorldCommandPayload>());

        SelectKind(WorldCommandKind.GrantAdventureIntel);
        SetFormDropdown("intel.kind", AdventureIntelDeclarationKind.CommonResource.ToString());
        SetInput("intel.item", fixture.Item.DefinitionId);
        SetInput("intel.amount", "4");
        Assert.That(fixture.Panel.TryBuildCommand(out command, out diagnostic), Is.True, diagnostic);
        Assert.That(command.Payload, Is.TypeOf<GrantAdventureIntelWorldCommandPayload>());
    }

    [Test]
    public void ConflictFormCreatesTypedCommand()
    {
        SelectKind(WorldCommandKind.ResolveConflict);
        SetInput("conflict.location", fixture.Site.Location.RuntimeId);
        SetInput("conflict.attacker", fixture.Actor.RuntimeId);
        SetInput("conflict.defender", fixture.Defender.RuntimeId);

        Assert.That(fixture.Panel.TryBuildCommand(out WorldCommand command, out string diagnostic), Is.True, diagnostic);
        ResolveConflictWorldCommandPayload payload = command.Payload as ResolveConflictWorldCommandPayload;
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload.Sides, Has.Count.EqualTo(2));
        Assert.That(payload.Sides[0].Participants[0].NpcRuntimeId, Is.EqualTo(fixture.Actor.RuntimeId));
    }

    [Test]
    public void CommandHistoryUsesWorldCommandRecordsNotLoggerParsing()
    {
        SetStackResourceForm();
        fixture.Panel.PreviewButton.onClick.Invoke();
        fixture.Panel.ApplyButton.onClick.Invoke();

        Assert.That(fixture.Service.RecordStore.Records, Has.Count.EqualTo(1));
        StringAssert.Contains("DeclareStackResource", HistoryText());
        StringAssert.Contains("SUCCESS", HistoryText());
    }

    private void SetStackResourceForm()
    {
        SelectKind(WorldCommandKind.DeclareStackResource);
        SetOwnerFields();
        SetInput("content.item", fixture.Item.DefinitionId);
        SetInput("content.amount", "3");
        SetInput("content.decay", "0");
        SetInput("content.averageCost", "4.5");
        SetFormDropdown("content.persistence", PlaceContentPersistencePolicy.Durable.ToString());
    }

    private void SelectAuthority(WorldCommandAuthorityMode authority)
    {
        for (int i = 0; i < fixture.Panel.AuthorityDropdown.options.Count; i++)
        {
            if (fixture.Panel.AuthorityDropdown.options[i].text == authority.ToString())
            {
                fixture.Panel.AuthorityDropdown.value = i;
                fixture.Panel.AuthorityDropdown.RefreshShownValue();
                return;
            }
        }

        Assert.Fail("Authority mode was not present in the GM Console dropdown: " + authority);
    }

    private void SetOwnerFields()
    {
        SetFormDropdown("content.ownerKind", PlaceContentOwnerKind.ExplorableSite.ToString());
        SetInput("content.owner", fixture.Site.RuntimeId);
        SetInput("content.macro", fixture.Site.Location.RuntimeId);
        SetInput("content.topologyOwner", string.Empty);
    }

    private void SelectKind(WorldCommandKind kind)
    {
        for (int i = 0; i < fixture.Panel.CommandTypeDropdown.options.Count; i++)
        {
            if (fixture.Panel.CommandTypeDropdown.options[i].text == kind.ToString())
            {
                fixture.Panel.CommandTypeDropdown.value = i;
                fixture.Panel.CommandTypeDropdown.RefreshShownValue();
                return;
            }
        }

        Assert.Fail("Command kind was not present in the GM Console dropdown: " + kind);
    }

    private void SetFormDropdown(string key, string value)
    {
        TMP_Dropdown dropdown = fixture.Panel.GetFormDropdown(key);
        Assert.That(dropdown, Is.Not.Null, key);
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == value)
            {
                dropdown.value = i;
                dropdown.RefreshShownValue();
                return;
            }
        }

        Assert.Fail("Dropdown value was not present: " + key + " = " + value);
    }

    private void SetInput(string key, string value)
    {
        TMP_InputField input = fixture.Panel.GetInput(key);
        Assert.That(input, Is.Not.Null, key);
        input.text = value ?? string.Empty;
    }

    private string HistoryText()
    {
        List<string> values = new List<string>();
        foreach (TMP_Text text in fixture.Panel.CommandHistoryPanel.GetComponentsInChildren<TMP_Text>(true))
        {
            values.Add(text.text);
        }

        return string.Join("\n", values);
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldObserverAssetBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        return prefab;
    }

    private sealed class ConsoleFixture
    {
        public readonly RecordFixture Records = SimulationTestFactory.CreateRecordFixture();
        public readonly RuntimeIdentityRegistry Identity = new RuntimeIdentityRegistry();
        public readonly CityRuntime City;
        public readonly ExplorableSiteRuntime Site;
        public readonly NpcRuntime Actor;
        public readonly NpcRuntime Defender;
        public readonly ItemData Item;
        public readonly LocalPlaceTypeData PlaceType;
        public readonly LocalConnectionTypeData ConnectionType;
        public readonly ExplorableSiteStore Sites = new ExplorableSiteStore();
        public readonly LocalTopologyStore Topologies;
        public readonly LocalTopologyRuntime Topology;
        public readonly LocalPlaceRuntime Entry;
        public readonly LocalPlaceRuntime Exit;
        public readonly PlaceContentStore Content;
        public readonly WorldCommandDefinitionCatalog Definitions = new WorldCommandDefinitionCatalog();
        public readonly WorldCommandService Service;
        public readonly GameObject Root;
        public readonly GMConsolePanel Panel;
        public readonly WorldObserverGmConsoleContext Context;
        public string SelectedPlaceRuntimeId;
        public int RefreshCount;

        public ConsoleFixture()
        {
            City = SimulationTestFactory.CreateCity("gm-city", "gm-city-location");
            Site = new ExplorableSiteRuntime(
                "gm-site",
                SimulationTestFactory.CreateExplorableSite("gm-site-definition"),
                new SpatialLocationRuntime("gm-site-location"));
            Actor = new NpcRuntime("gm-actor", SimulationTestFactory.CreateNpc("gm-actor-definition"));
            Defender = new NpcRuntime("gm-defender", SimulationTestFactory.CreateNpc("gm-defender-definition"));
            Actor.SetCurrentPresence(City.Location);
            Defender.SetCurrentPresence(Site.Location);
            Assert.That(Identity.RegisterLocation(City.Location), Is.True);
            Assert.That(Identity.RegisterLocation(Site.Location), Is.True);
            Assert.That(Identity.RegisterCity(City), Is.True);
            Assert.That(Identity.RegisterExplorableSite(Site), Is.True);
            Assert.That(Identity.RegisterNpc(Actor), Is.True);
            Assert.That(Identity.RegisterNpc(Defender), Is.True);
            Assert.That(Sites.Add(Site), Is.True);

            Item = SimulationTestFactory.CreateItem("gm-item", 10f);
            PlaceType = SimulationTestFactory.CreateLocalPlaceType("gm-place-type");
            ConnectionType = SimulationTestFactory.CreateLocalConnectionType("gm-connection-type");
            Definitions.RegisterItem(Item);
            Definitions.RegisterLocalPlaceType(PlaceType);
            Definitions.RegisterLocalConnectionType(ConnectionType);

            Topologies = new LocalTopologyStore(Identity);
            Topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(Site), Identity);
            Entry = new LocalPlaceRuntime("gm-entry", "Entry");
            Exit = new LocalPlaceRuntime("gm-exit", "Exit");
            Assert.That(Topology.AddPlace(Entry, isEntryPoint: true), Is.True);
            Assert.That(Topology.AddPlace(Exit), Is.True);
            Assert.That(Topology.AddConnection(new LocalTopologyConnectionRuntime("gm-entry-exit", Entry, Exit, 1f)), Is.True);
            Assert.That(Topologies.Add(Topology), Is.True);

            Content = new PlaceContentStore(Records.Allocator, Identity);
            Content.GetOrCreate(Site);
            Service = new WorldCommandService(
                runtimeIdAllocator: Records.Allocator,
                identityRegistry: Identity,
                definitionResolver: Definitions,
                simulationTime: Records.Time);
            ConflictResolutionService conflictService = new ConflictResolutionService(
                new ConflictResolver(new FixedCapabilityModel(), new SequenceConflictRandomSource(0.5f, 0.5f, 0.5f)),
                null,
                Records.EventRecorder);
            Assert.That(WorldCommandHandlerRegistration.RegisterCoreHandlers(
                Service,
                Records.Allocator,
                Identity,
                Content,
                Topologies,
                conflictService,
                Definitions,
                domainEventStore: Records.Events), Is.True);

            Context = new WorldObserverGmConsoleContext(
                Service,
                Definitions,
                Identity,
                new[] { City },
                new[] { Actor, Defender },
                Sites,
                Topologies,
                Content,
                () => SelectedPlaceRuntimeId,
                () => RefreshCount++);

            Root = new GameObject("GmConsoleTestRoot", typeof(RectTransform));
            Panel = Root.AddComponent<GMConsolePanel>();
            Panel.Bind(Context);
            Panel.Show();
        }

        public void Dispose()
        {
            if (Root != null)
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        public CapabilityEvaluationResult Evaluate(NpcRuntime participant, CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(100f, 100f, null);
        }
    }
}
