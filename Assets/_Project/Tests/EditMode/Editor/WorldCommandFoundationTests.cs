using NUnit.Framework;

public sealed class WorldCommandFoundationTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void PreviewDoesNotMutateWorld()
    {
        ProbeWorld world = new ProbeWorld();
        WorldCommandService service = CreateService(world, out _);
        WorldCommandPreview preview = service.Preview(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(preview.IsValid, Is.True);
        Assert.That(world.Value, Is.EqualTo(0));
    }

    [Test]
    public void PreviewDoesNotMutateKnowledge()
    {
        NpcRuntime npc = new NpcRuntime("npc-knowledge", SimulationTestFactory.CreateNpc("knowledge"));
        WorldCommandService service = CreateService(new ProbeWorld(), out _);
        service.Preview(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(npc.ExplorableSiteKnowledge.Observations, Is.Empty);
    }

    [Test]
    public void PreviewDoesNotConsumeRng()
    {
        WorldCommandService service = CreateService(new ProbeWorld(), out _);
        WorldCommandPreview first = service.Preview(CreateCommand(WorldCommandAuthorityMode.Request));
        WorldCommandPreview second = service.Preview(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(second.Presentation, Is.EqualTo(first.Presentation));
    }

    [Test]
    public void PreviewDoesNotAllocateWorldRuntimeId()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        WorldCommandService service = CreateService(new ProbeWorld(), out _, allocator);
        service.Preview(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000001"));
    }

    [Test]
    public void PreviewDoesNotAllocateWorldCommandId()
    {
        WorldCommandIdAllocator allocator = new WorldCommandIdAllocator();
        WorldCommandService service = CreateService(new ProbeWorld(), out _, commandIdAllocator: allocator);
        service.Preview(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(allocator.Allocate(), Is.EqualTo("world-command-000001"));
    }

    [Test]
    public void ExecuteRevalidatesAfterPreview()
    {
        ProbeWorld world = new ProbeWorld();
        WorldCommandService service = CreateService(world, out ProbeHandler handler);
        Assert.That(service.Preview(CreateCommand(WorldCommandAuthorityMode.Request)).IsValid, Is.True);
        handler.Allowed = false;
        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(result.Success, Is.False);
        Assert.That(world.Value, Is.EqualTo(0));
    }

    [Test]
    public void InvalidExecuteCannotBypassStructuralInvariant()
    {
        ProbeWorld world = new ProbeWorld();
        WorldCommandService service = CreateService(world, out ProbeHandler handler);
        handler.StructurallyValid = false;
        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Declare));
        Assert.That(result.Success, Is.False);
        Assert.That(world.Value, Is.EqualTo(0));
    }

    [Test]
    public void SuggestNeverMutatesWorld()
    {
        ProbeWorld world = new ProbeWorld();
        WorldCommandService service = CreateService(world, out _);
        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Suggest));
        Assert.That(result.Success, Is.False);
        Assert.That(world.Value, Is.EqualTo(0));
    }

    [Test]
    public void WorldCommandIdUsesIndependentSequence()
    {
        RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
        runtimeIds.AllocateNpcId();
        WorldCommandService service = CreateService(new ProbeWorld(), out _, runtimeIds);
        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Declare));
        Assert.That(result.WorldCommandId, Is.EqualTo("world-command-000001"));
        Assert.That(runtimeIds.AllocateNpcId(), Is.EqualTo("npc-000002"));
    }

    [Test]
    public void WorldCommandRecordPreservesOriginAndAuthority()
    {
        WorldCommandService service = CreateService(new ProbeWorld(), out _);
        WorldCommand command = new WorldCommand(
            WorldCommandKind.RelocateNpc,
            WorldCommandOrigin.GM,
            WorldCommandAuthorityMode.Declare,
            new RelocateNpcWorldCommandPayload("npc", "location"));
        WorldCommandResult result = service.Execute(command);
        Assert.That(result.Record.Origin, Is.EqualTo(WorldCommandOrigin.GM));
        Assert.That(result.Record.Authority, Is.EqualTo(WorldCommandAuthorityMode.Declare));
    }

    [Test]
    public void RejectedExecutionCanBeAuditedWithoutDomainMutation()
    {
        ProbeWorld world = new ProbeWorld();
        WorldCommandService service = CreateService(world, out ProbeHandler handler);
        handler.Allowed = false;
        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Request));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Record, Is.Not.Null);
        Assert.That(service.RecordStore.Records, Has.Count.EqualTo(1));
        Assert.That(world.Value, Is.EqualTo(0));
    }

    [Test]
    public void CommandRecordIsNotRuntimeIdentity()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        WorldCommandService service = CreateService(new ProbeWorld(), out _, identityRegistry: registry);
        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Declare));
        Assert.That(registry.TryGetNpc(result.WorldCommandId, out _), Is.False);
    }

    private static WorldCommandService CreateService(
        ProbeWorld world,
        out ProbeHandler handler,
        RuntimeIdAllocator runtimeIds = null,
        WorldCommandIdAllocator commandIdAllocator = null,
        RuntimeIdentityRegistry identityRegistry = null)
    {
        handler = new ProbeHandler(world);
        WorldCommandService service = new WorldCommandService(
            commandIdAllocator,
            null,
            runtimeIds ?? new RuntimeIdAllocator(),
            identityRegistry ?? new RuntimeIdentityRegistry(),
            null,
            new SimulationTime());
        Assert.That(service.RegisterHandler(handler), Is.True);
        return service;
    }

    private static WorldCommand CreateCommand(WorldCommandAuthorityMode authority)
    {
        return new WorldCommand(
            WorldCommandKind.RelocateNpc,
            WorldCommandOrigin.ExternalImport,
            authority,
            new RelocateNpcWorldCommandPayload("npc", "location"));
    }

    private sealed class ProbeWorld
    {
        public int Value;
    }

    private sealed class ProbeHandler : IWorldCommandHandler
    {
        private readonly ProbeWorld world;
        public bool Allowed = true;
        public bool StructurallyValid = true;
        public WorldCommandKind Kind => WorldCommandKind.RelocateNpc;

        public ProbeHandler(ProbeWorld world)
        {
            this.world = world;
        }

        public WorldCommandPreview Preview(WorldCommand command)
        {
            return new WorldCommandPreview(Kind, command.Authority, Allowed && StructurallyValid, presentation: "probe");
        }

        public WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
        {
            if (Allowed == false) return new WorldCommandHandlerResult(false, "Truth changed after preview.");
            if (StructurallyValid == false) return new WorldCommandHandlerResult(false, "Structural invariant rejected.");
            world.Value++;
            return new WorldCommandHandlerResult(true, affectedRuntimeIds: new[] { "npc" });
        }
    }
}
