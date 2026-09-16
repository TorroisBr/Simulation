using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class CoreWorldCommandHandlerTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DeclareRelocateMovesStationaryNpcWithoutTravel()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Destination.RuntimeId));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Actor.CurrentLocation, Is.SameAs(fixture.Destination));
        Assert.That(fixture.Actor.IsTraveling, Is.False);
    }

    [Test]
    public void RelocateDoesNotCreateFakeTravelDecision()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Destination.RuntimeId));

        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(0));
    }

    [Test]
    public void RelocateDoesNotCreateFakeTravelStartedEvent()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Destination.RuntimeId));

        Assert.That(HasEvent(fixture, DomainEventType.NpcTravelStarted), Is.False);
    }

    [Test]
    public void RelocateRejectsActiveExpeditionMember()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture(includeExpedition: true);
        fixture.AddActiveExpeditionForActor();
        SpatialLocationRuntime before = fixture.Actor.CurrentLocation;

        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Destination.RuntimeId));

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Actor.CurrentLocation, Is.SameAs(before));
    }

    [Test]
    public void RelocateRejectsTravelPartyMember()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        fixture.Actor.SetActiveTravelPartyId("party-active");
        SpatialLocationRuntime before = fixture.Actor.CurrentLocation;

        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Destination.RuntimeId));

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Actor.CurrentLocation, Is.SameAs(before));
    }

    [Test]
    public void InvalidDestinationDoesNotMutateNpc()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        SpatialLocationRuntime before = fixture.Actor.CurrentLocation;
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, "location-missing"));

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Actor.CurrentLocation, Is.SameAs(before));
    }

    [Test]
    public void DeclareStackResourceCreatesWorldTruthContent()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareStackResource,
            WorldCommandAuthorityMode.Declare,
            new DeclareStackResourceWorldCommandPayload(
                fixture.SiteOwnerPayload(),
                fixture.Item.DefinitionId,
                8,
                PlaceContentPersistencePolicy.Durable,
                averageUnitCost: 3f));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Content.TryGet(fixture.Site, out PlaceContentRuntime content), Is.True);
        Assert.That(content.GetAmount(fixture.Item), Is.EqualTo(8));
    }

    [Test]
    public void ResourceCommandUsesItemDefinitionId()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareStackResource,
            WorldCommandAuthorityMode.Declare,
            new DeclareStackResourceWorldCommandPayload(
                fixture.SiteOwnerPayload(),
                fixture.Item.DefinitionId,
                4,
                PlaceContentPersistencePolicy.Durable));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Content.TryGet(fixture.Site, out PlaceContentRuntime content), Is.True);
        Assert.That(content.StackedContent[0].Item, Is.SameAs(fixture.Item));
    }

    [Test]
    public void InvalidDefinitionDoesNotMutateContent()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareStackResource,
            WorldCommandAuthorityMode.Declare,
            new DeclareStackResourceWorldCommandPayload(
                fixture.SiteOwnerPayload(),
                "item-unknown",
                4,
                PlaceContentPersistencePolicy.Durable));

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Content.Places, Is.Empty);
    }

    [Test]
    public void PreviewDoesNotCreatePlaceContentRuntimeWhenAbsent()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommand command = fixture.Command(
            WorldCommandKind.DeclareStackResource,
            WorldCommandAuthorityMode.Declare,
            new DeclareStackResourceWorldCommandPayload(
                fixture.SiteOwnerPayload(),
                fixture.Item.DefinitionId,
                4,
                PlaceContentPersistencePolicy.Durable));

        Assert.That(fixture.Service.Preview(command).IsValid, Is.True);
        Assert.That(fixture.Content.Places, Is.Empty);
    }

    [Test]
    public void DeclareNotableCreatesStableRuntimeIdentity()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareNotableItem,
            WorldCommandAuthorityMode.Declare,
            new DeclareNotableItemWorldCommandPayload(fixture.SiteOwnerPayload(), fixture.Item.DefinitionId));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(result.CreatedRuntimeIds, Has.Count.EqualTo(1));
        Assert.That(fixture.Registry.TryGetNotableItem(result.CreatedRuntimeIds[0], out NotableItemRuntime notable), Is.True);
        Assert.That(notable.IsAtPlace, Is.True);
    }

    [Test]
    public void DeclareNotableReturnsCreatedRuntimeId()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareNotableItem,
            WorldCommandAuthorityMode.Declare,
            new DeclareNotableItemWorldCommandPayload(fixture.SiteOwnerPayload(), fixture.Item.DefinitionId));

        Assert.That(result.CreatedRuntimeIds[0], Is.EqualTo("notable-item-000001"));
        Assert.That(result.Record.CreatedRuntimeIds[0], Is.EqualTo(result.CreatedRuntimeIds[0]));
    }

    [Test]
    public void PreviewDoesNotConsumeNotableId()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommand command = fixture.Command(
            WorldCommandKind.DeclareNotableItem,
            WorldCommandAuthorityMode.Declare,
            new DeclareNotableItemWorldCommandPayload(fixture.SiteOwnerPayload(), fixture.Item.DefinitionId));

        Assert.That(fixture.Service.Preview(command).IsValid, Is.True);
        WorldCommandResult result = fixture.Service.Execute(command);
        Assert.That(result.CreatedRuntimeIds[0], Is.EqualTo("notable-item-000001"));
    }

    [Test]
    public void FailedNotableCommandDoesNotLeakIdentity()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareNotableItem,
            WorldCommandAuthorityMode.Declare,
            new DeclareNotableItemWorldCommandPayload(
                new WorldContentOwnerReferencePayload(PlaceContentOwnerKind.ExplorableSite, fixture.Site.RuntimeId, "location-wrong"),
                fixture.Item.DefinitionId));

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Content.NotableItems, Is.Empty);
        Assert.That(fixture.Registry.TryGetNotableItem("notable-item-000001", out _), Is.False);
    }

    [Test]
    public void AddLocalPlaceUsesPublishedMutationBoundary()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.AddLocalPlace,
            WorldCommandAuthorityMode.Declare,
            new AddLocalPlaceWorldCommandPayload(fixture.Site.RuntimeId, "Vault", fixture.PlaceType.DefinitionId, fixture.Entry.RuntimeId, true));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Topology.TryGetPlace(result.CreatedRuntimeIds[0], out LocalPlaceRuntime place), Is.True);
        Assert.That(place.Parent, Is.SameAs(fixture.Entry));
        Assert.That(fixture.Topology.EntryPointCount, Is.EqualTo(2));
    }

    [Test]
    public void AddLocalPlacePreviewDoesNotAllocateId()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandPreview preview = fixture.Service.Preview(fixture.Command(
            WorldCommandKind.AddLocalPlace,
            WorldCommandAuthorityMode.Declare,
            new AddLocalPlaceWorldCommandPayload(fixture.Site.RuntimeId, "Vault")));

        Assert.That(preview.IsValid, Is.True);
        Assert.That(fixture.Allocator.AllocateLocalPlaceId(), Is.EqualTo("local-place-000001"));
    }

    [Test]
    public void AddLocalConnectionUsesPublishedMutationBoundary()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.AddLocalConnection,
            WorldCommandAuthorityMode.Declare,
            new AddLocalConnectionWorldCommandPayload(fixture.Site.RuntimeId, fixture.Entry.RuntimeId, fixture.Exit.RuntimeId, 2f, fixture.ConnectionType.DefinitionId));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Topology.TryGetConnection(result.CreatedRuntimeIds[0], out LocalTopologyConnectionRuntime connection), Is.True);
        Assert.That(connection.OwningTopology, Is.SameAs(fixture.Topology));
    }

    [Test]
    public void AddLocalConnectionRemainsDirected()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.AddLocalConnection,
            WorldCommandAuthorityMode.Declare,
            new AddLocalConnectionWorldCommandPayload(fixture.Site.RuntimeId, fixture.Entry.RuntimeId, fixture.Exit.RuntimeId, 2f));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Topology.GetOutgoingConnections(fixture.Entry), Has.Count.EqualTo(1));
        Assert.That(fixture.Topology.GetOutgoingConnections(fixture.Exit), Is.Empty);
    }

    [Test]
    public void InvalidConnectionDoesNotLeakRuntimeId()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.AddLocalConnection,
            WorldCommandAuthorityMode.Declare,
            new AddLocalConnectionWorldCommandPayload(fixture.Site.RuntimeId, fixture.Entry.RuntimeId, "missing-place", 2f));

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Allocator.AllocateLocalConnectionId(), Is.EqualTo("local-connection-000001"));
        Assert.That(fixture.Topology.ConnectionCount, Is.EqualTo(0));
    }

    [Test]
    public void GrantSiteKnowledgeMutatesKnowledgeNotTruth()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.GrantSiteKnowledge,
            WorldCommandAuthorityMode.Declare,
            new GrantSiteKnowledgeWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Site.RuntimeId));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Actor.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.True);
        Assert.That(fixture.Content.Places, Is.Empty);
    }

    [Test]
    public void GrantFalseAdventureIntelIsAllowedAsKnowledge()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.GrantAdventureIntel,
            WorldCommandAuthorityMode.Declare,
            new GrantAdventureIntelWorldCommandPayload(
                fixture.Actor.RuntimeId,
                AdventureIntelDeclarationKind.Opposition,
                fixture.Site.RuntimeId,
                oppositionRuntimeId: "opposition-fiction",
                oppositionState: AdventureOppositionObservedState.Active));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Actor.AdventureSiteIntelKnowledge.KnowsOpposition(fixture.Site.RuntimeId, "opposition-fiction"), Is.True);
    }

    [Test]
    public void FalseIntelDoesNotCreateResourceOrOpposition()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.GrantAdventureIntel,
            WorldCommandAuthorityMode.Declare,
            new GrantAdventureIntelWorldCommandPayload(
                fixture.Actor.RuntimeId,
                AdventureIntelDeclarationKind.CommonResource,
                fixture.Site.RuntimeId,
                itemDefinitionId: "item-fiction",
                observedAmount: 99));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Content.Places, Is.Empty);
        Assert.That(fixture.Registry.TryGetNotableItem("notable-item-000001", out _), Is.False);
    }

    [Test]
    public void KnowledgeCommandCannotCreateFakeRuntimeIdentity()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.GrantAdventureIntel,
            WorldCommandAuthorityMode.Declare,
            new GrantAdventureIntelWorldCommandPayload(
                fixture.Actor.RuntimeId,
                AdventureIntelDeclarationKind.NotableItem,
                fixture.Site.RuntimeId,
                notableItemRuntimeId: "notable-fiction",
                itemDefinitionId: "item-fiction"));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Registry.TryGetNotableItem("notable-fiction", out _), Is.False);
    }

    [Test]
    public void RequestConflictUsesNormalResolver()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(WorldCommandKind.ResolveConflict, WorldCommandAuthorityMode.Request, fixture.ConflictPayload());

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(HasEvent(fixture, DomainEventType.ConflictResolved), Is.True);
        Assert.That(LastConflictEvent(fixture).Resolution.OutcomeSource, Is.EqualTo(ConflictOutcomeSource.Simulated));
    }

    [Test]
    public void DeclareConflictCanResolveUnspecifiedOutcome()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(WorldCommandKind.ResolveConflict, WorldCommandAuthorityMode.Declare, fixture.ConflictPayload());

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(HasEvent(fixture, DomainEventType.ConflictResolved), Is.True);
    }

    [Test]
    public void ForceOutcomeCanConstrainWinner()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.ResolveConflict,
            WorldCommandAuthorityMode.ForceOutcome,
            new ResolveConflictWorldCommandPayload(
                fixture.City.Location.RuntimeId,
                fixture.ConflictPayload().Sides,
                forcedWinningSideId: "attacker"));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(LastConflictEvent(fixture).Resolution.WinningSideId, Is.EqualTo("attacker"));
    }

    [Test]
    public void ForceOutcomeCanConstrainParticipantConsequence()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.ResolveConflict,
            WorldCommandAuthorityMode.ForceOutcome,
            new ResolveConflictWorldCommandPayload(
                fixture.City.Location.RuntimeId,
                fixture.ConflictPayload().Sides,
                forcedWinningSideId: "attacker",
                constraints: new[]
                {
                    new WorldConflictParticipantConstraintPayload(fixture.Defender.RuntimeId, forceDeath: true)
                }));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Defender.IsDead, Is.True);
    }

    [Test]
    public void ForceOutcomeCannotBypassStructuralIdentity()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldConflictSidePayload fakeSide = new WorldConflictSidePayload(
            "attacker",
            ConflictObjectiveType.Defeat,
            ConflictStakes.Meaningful,
            new[] { new WorldConflictParticipantPayload("npc-fake") });
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.ResolveConflict,
            WorldCommandAuthorityMode.ForceOutcome,
            new ResolveConflictWorldCommandPayload(
                fixture.City.Location.RuntimeId,
                new[] { fakeSide, new WorldConflictSidePayload("defender", ConflictObjectiveType.Defend, ConflictStakes.Meaningful, new[] { new WorldConflictParticipantPayload(fixture.Defender.RuntimeId) }) },
                forcedWinningSideId: "attacker"));

        Assert.That(result.Success, Is.False);
        Assert.That(HasEvent(fixture, DomainEventType.ConflictResolved), Is.False);
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(0));
    }

    [Test]
    public void PlaceOppositionCommandUsesExistingBinding()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture(includeOpposition: true);
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.PlaceOpposition,
            WorldCommandAuthorityMode.ForceOutcome,
            new ResolveConflictWorldCommandPayload(
                fixture.Site.Location.RuntimeId,
                fixture.OppositionConflictPayload().Sides,
                oppositionRuntimeId: fixture.Opposition.RuntimeId,
                forcedWinningSideId: "attacker"));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Opposition.IsResolved, Is.True);
    }

    [Test]
    public void GenericConflictDoesNotMutateUnrelatedPlaceOpposition()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture(includeOpposition: true);
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.ResolveConflict,
            WorldCommandAuthorityMode.ForceOutcome,
            new ResolveConflictWorldCommandPayload(
                fixture.City.Location.RuntimeId,
                fixture.ConflictPayload().Sides,
                forcedWinningSideId: "attacker"));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Opposition.IsActive, Is.True);
    }

    [Test]
    public void ExternalConflictDoesNotInventNpcDecision()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(WorldCommandKind.ResolveConflict, WorldCommandAuthorityMode.Request, fixture.ConflictPayload(), WorldCommandOrigin.ExternalImport);

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(0));
    }

    [Test]
    public void SuccessfulCommandProducesAuditRecord()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareStackResource,
            WorldCommandAuthorityMode.Declare,
            new DeclareStackResourceWorldCommandPayload(fixture.SiteOwnerPayload(), fixture.Item.DefinitionId, 2, PlaceContentPersistencePolicy.Durable));

        Assert.That(result.Success, Is.True);
        Assert.That(fixture.Service.RecordStore.Records, Has.Count.EqualTo(1));
        Assert.That(result.Record.Success, Is.True);
        Assert.That(result.Record.WorldCommandId, Is.EqualTo(result.WorldCommandId));
    }

    [Test]
    public void CreatedRuntimeIdsAppearInResultAndRecord()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.AddLocalPlace,
            WorldCommandAuthorityMode.Declare,
            new AddLocalPlaceWorldCommandPayload(fixture.Site.RuntimeId, "Chamber"));

        Assert.That(result.Success, Is.True, result.Diagnostic);
        Assert.That(result.CreatedRuntimeIds, Is.EqualTo(result.Record.CreatedRuntimeIds));
        Assert.That(result.CreatedRuntimeIds, Has.Count.EqualTo(1));
    }

    [Test]
    public void FailedCommandRecordsFailureWithoutPartialMutation()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.DeclareStackResource,
            WorldCommandAuthorityMode.Declare,
            new DeclareStackResourceWorldCommandPayload(fixture.SiteOwnerPayload(), "missing-item", 2, PlaceContentPersistencePolicy.Durable));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Record.Success, Is.False);
        Assert.That(fixture.Service.RecordStore.Records, Has.Count.EqualTo(1));
        Assert.That(fixture.Content.Places, Is.Empty);
    }

    [Test]
    public void CommandOriginIsPreserved()
    {
        WorldCommandTestFixture fixture = new WorldCommandTestFixture();
        WorldCommandResult result = fixture.Execute(
            WorldCommandKind.RelocateNpc,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload(fixture.Actor.RuntimeId, fixture.Destination.RuntimeId),
            WorldCommandOrigin.GM);

        Assert.That(result.Record.Origin, Is.EqualTo(WorldCommandOrigin.GM));
    }

    private static bool HasEvent(WorldCommandTestFixture fixture, DomainEventType eventType)
    {
        foreach (DomainEvent domainEvent in fixture.Records.Events.Events)
        {
            if (domainEvent != null && domainEvent.EventType == eventType)
            {
                return true;
            }
        }

        return false;
    }

    private static ConflictResolvedEvent LastConflictEvent(WorldCommandTestFixture fixture)
    {
        for (int i = fixture.Records.Events.Events.Count - 1; i >= 0; i--)
        {
            if (fixture.Records.Events.Events[i] is ConflictResolvedEvent conflictEvent)
            {
                return conflictEvent;
            }
        }

        return null;
    }

    private sealed class WorldCommandTestFixture
    {
        public RecordFixture Records { get; } = SimulationTestFactory.CreateRecordFixture();
        public RuntimeIdAllocator Allocator => Records.Allocator;
        public RuntimeIdentityRegistry Registry { get; } = new RuntimeIdentityRegistry();
        public CityRuntime City { get; }
        public SpatialLocationRuntime Destination { get; } = new SpatialLocationRuntime("location-destination");
        public ExplorableSiteRuntime Site { get; }
        public NpcRuntime Actor { get; }
        public NpcRuntime Defender { get; }
        public ItemData Item { get; }
        public LocalPlaceTypeData PlaceType { get; }
        public LocalConnectionTypeData ConnectionType { get; }
        public SpatialNetworkRuntime Network { get; }
        public PlaceContentStore Content { get; }
        public LocalTopologyStore Topologies { get; }
        public LocalTopologyRuntime Topology { get; }
        public LocalPlaceRuntime Entry { get; }
        public LocalPlaceRuntime Exit { get; }
        public PlaceOppositionRuntime Opposition { get; private set; }
        public WorldCommandDefinitionCatalog Definitions { get; } = new WorldCommandDefinitionCatalog();
        public WorldCommandService Service { get; }
        public ExpeditionSystem Expeditions { get; }
        private readonly ExpeditionStore expeditionStore;

        public WorldCommandTestFixture(bool includeExpedition = false, bool includeOpposition = false)
        {
            City = SimulationTestFactory.CreateCity("city-command", "location-city");
            Site = new ExplorableSiteRuntime("site-command", SimulationTestFactory.CreateExplorableSite("site-command-definition"), new SpatialLocationRuntime("location-site"));
            Item = SimulationTestFactory.CreateItem("item-command", 12f);
            PlaceType = SimulationTestFactory.CreateLocalPlaceType("place-command-type");
            ConnectionType = SimulationTestFactory.CreateLocalConnectionType("connection-command-type");
            Definitions.RegisterItem(Item);
            Definitions.RegisterLocalPlaceType(PlaceType);
            Definitions.RegisterLocalConnectionType(ConnectionType);

            Network = new SpatialNetworkRuntime(Registry);
            Network.RegisterLocation(City.Location);
            Network.RegisterLocation(Site.Location);
            Network.RegisterLocation(Destination);
            Registry.RegisterCity(City);
            Registry.RegisterExplorableSite(Site);

            Actor = new NpcRuntime("npc-command-actor", SimulationTestFactory.CreateNpc("command-actor"));
            Actor.SetCurrentPresence(City.Location);
            Defender = new NpcRuntime("npc-command-defender", SimulationTestFactory.CreateNpc("command-defender"));
            Defender.SetCurrentPresence(Site.Location);
            Registry.RegisterNpc(Actor);
            Registry.RegisterNpc(Defender);

            Content = new PlaceContentStore(Records.Allocator, Registry);
            Topologies = new LocalTopologyStore(Registry);
            Topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(Site), Registry);
            Entry = new LocalPlaceRuntime("local-command-entry", "Entry");
            Exit = new LocalPlaceRuntime("local-command-exit", "Exit");
            Topology.AddPlace(Entry, isEntryPoint: true);
            Topology.AddPlace(Exit);
            Topologies.Add(Topology);

            expeditionStore = new ExpeditionStore();
            Expeditions = CreateExpeditionSystem();
            ConflictResolutionService conflictService = new ConflictResolutionService(
                new ConflictResolver(new FixedCapabilityModel(), new SequenceConflictRandomSource(0.5f, 0.5f, 0.5f)),
                null,
                Records.EventRecorder);
            Service = new WorldCommandService(
                runtimeIdAllocator: Records.Allocator,
                identityRegistry: Registry,
                definitionResolver: Definitions,
                simulationTime: Records.Time);
            Assert.That(WorldCommandHandlerRegistration.RegisterCoreHandlers(
                Service,
                Records.Allocator,
                Registry,
                Content,
                Topologies,
                conflictService,
                Definitions,
                includeExpedition ? Expeditions : null,
                null,
                Records.Events), Is.True);

            if (includeOpposition)
            {
                Opposition = new PlaceOppositionRuntime("opposition-command", "Command Opposition");
                Opposition.AddNamedParticipant(Defender);
                Assert.That(Content.TryAddOpposition(Site, Opposition, out string diagnostic), Is.True, diagnostic);
            }
        }

        public WorldContentOwnerReferencePayload SiteOwnerPayload()
        {
            return new WorldContentOwnerReferencePayload(PlaceContentOwnerKind.ExplorableSite, Site.RuntimeId, Site.Location.RuntimeId);
        }

        public WorldCommand Command(WorldCommandKind kind, WorldCommandAuthorityMode authority, WorldCommandPayload payload, WorldCommandOrigin origin = WorldCommandOrigin.GM)
        {
            return new WorldCommand(kind, origin, authority, payload);
        }

        public WorldCommandResult Execute(WorldCommandKind kind, WorldCommandAuthorityMode authority, WorldCommandPayload payload, WorldCommandOrigin origin = WorldCommandOrigin.GM)
        {
            return Service.Execute(Command(kind, authority, payload, origin));
        }

        public ResolveConflictWorldCommandPayload ConflictPayload()
        {
            return new ResolveConflictWorldCommandPayload(
                City.Location.RuntimeId,
                new[]
                {
                    new WorldConflictSidePayload(
                        "attacker",
                        ConflictObjectiveType.Defeat,
                        ConflictStakes.Meaningful,
                        new[] { new WorldConflictParticipantPayload(Actor.RuntimeId) }),
                    new WorldConflictSidePayload(
                        "defender",
                        ConflictObjectiveType.Defend,
                        ConflictStakes.Meaningful,
                        new[] { new WorldConflictParticipantPayload(Defender.RuntimeId) })
                });
        }

        public ResolveConflictWorldCommandPayload OppositionConflictPayload()
        {
            return new ResolveConflictWorldCommandPayload(
                Site.Location.RuntimeId,
                new[]
                {
                    new WorldConflictSidePayload(
                        "attacker",
                        ConflictObjectiveType.Defeat,
                        ConflictStakes.Meaningful,
                        new[] { new WorldConflictParticipantPayload(Actor.RuntimeId) }),
                    new WorldConflictSidePayload(
                        "opposition",
                        ConflictObjectiveType.Defend,
                        ConflictStakes.Meaningful,
                        new[] { new WorldConflictParticipantPayload(Defender.RuntimeId) })
                });
        }

        public void AddActiveExpeditionForActor()
        {
            ExpeditionRuntime expedition = new ExpeditionRuntime(
                "expedition-command",
                Site.RuntimeId,
                City.Location.RuntimeId,
                Site.Location.RuntimeId,
                "route-command",
                null,
                null,
                new[] { Actor.RuntimeId },
                new[] { Actor.RuntimeId },
                Array.Empty<string>(),
                ExpeditionState.AtSite);
            Assert.That(expeditionStore.Add(expedition), Is.True);
        }

        private ExpeditionSystem CreateExpeditionSystem()
        {
            SpatialRouteRuntime route = new SpatialRouteRuntime("route-command", City.Location, Site.Location, 1);
            Network.RegisterRoute(route);
            TravelPartyStore parties = new TravelPartyStore();
            TravelSystem travel = new TravelSystem(Network, _ => null, 0f, Records.EventRecorder, null);
            TravelPartySystem partySystem = new TravelPartySystem(
                parties,
                Records.Allocator,
                Registry,
                travel,
                Records.Time,
                Records.Sequence,
                Records.EventRecorder);
            return new ExpeditionSystem(
                expeditionStore,
                Records.Allocator,
                Registry,
                new ExplorableSiteStore(),
                partySystem,
                parties,
                new ExplorableSiteKnowledgeSystem(),
                Records.Time,
                Records.EventRecorder,
                null,
                Content,
                Topologies);
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
