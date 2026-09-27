using NUnit.Framework;

public sealed class ActorActionChoiceCommandTests
{
    [Test]
    public void PayloadContainsOnlyStablePersonAndActionIdentifiers()
    {
        PersonId personId = new PersonId("person.local-player");
        ActorActionChoiceWorldCommandPayload payload = new ActorActionChoiceWorldCommandPayload(personId, "action.sell-goods");

        Assert.That(payload.PersonId, Is.EqualTo(personId));
        Assert.That(payload.ActionDefinitionId, Is.EqualTo("action.sell-goods"));
    }

    [Test]
    public void PreviewIsReadOnlyAndSuggestIsPreviewOnly()
    {
        SimulationRuntime world = CreateWorld();
        WorldCommandService service = CreateService(world);

        WorldCommandPreview preview = service.Preview(CreateCommand(WorldCommandAuthorityMode.Suggest));
        Assert.That(preview.IsValid, Is.True);
        Assert.That(world.ActorChoiceStore.Count, Is.Zero);

        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Suggest));
        Assert.That(result.Success, Is.False);
        Assert.That(world.ActorChoiceStore.Count, Is.Zero);
    }

    [Test]
    public void RequestQueuesChoiceWithoutExecutingActionOrDomainEffects()
    {
        SimulationRuntime world = CreateWorld();
        WorldCommandService service = CreateService(world);

        WorldCommandResult result = service.Execute(CreateCommand(WorldCommandAuthorityMode.Request));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Record.Origin, Is.EqualTo(WorldCommandOrigin.LocalPlayer));
        Assert.That(result.Record.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
        Assert.That(result.Record.Kind, Is.EqualTo(WorldCommandKind.ActorActionChoice));
        Assert.That(result.AffectedRuntimeIds, Is.Empty);
        Assert.That(result.CreatedRuntimeIds, Is.Empty);
        Assert.That(result.EventIds, Is.Empty);

        Assert.That(world.ActorChoiceStore.PendingInputs, Has.Count.EqualTo(1));
        ActorChoiceInput input = world.ActorChoiceStore.PendingInputs[0];
        Assert.That(input.WorldCommandId, Is.EqualTo(result.WorldCommandId));
        Assert.That(input.PersonId, Is.EqualTo(new PersonId("person.local-player")));
        Assert.That(input.ActionDefinitionId, Is.EqualTo("action.sell-goods"));
        Assert.That(input.Origin, Is.EqualTo(WorldCommandOrigin.LocalPlayer));
        Assert.That(input.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
        Assert.That(input.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(input.Dispositions, Is.Empty);
    }

    [TestCase(WorldCommandAuthorityMode.Declare)]
    [TestCase(WorldCommandAuthorityMode.ForceOutcome)]
    public void DeclareAndForceOutcomeAreRejected(WorldCommandAuthorityMode authority)
    {
        SimulationRuntime world = CreateWorld();
        WorldCommandService service = CreateService(world);

        WorldCommandPreview preview = service.Preview(CreateCommand(authority));
        WorldCommandResult result = service.Execute(CreateCommand(authority));

        Assert.That(preview.IsValid, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(world.ActorChoiceStore.Count, Is.Zero);
    }

    [Test]
    public void NonLocalPlayerProvenanceIsRejected()
    {
        SimulationRuntime world = CreateWorld();
        WorldCommandService service = CreateService(world);
        WorldCommand command = CreateCommand(WorldCommandAuthorityMode.Request, WorldCommandOrigin.GM);

        Assert.That(service.Preview(command).IsValid, Is.False);
        Assert.That(service.Execute(command).Success, Is.False);
        Assert.That(world.ActorChoiceStore.Count, Is.Zero);
    }

    private static SimulationRuntime CreateWorld()
    {
        return new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
    }

    private static WorldCommandService CreateService(SimulationRuntime world)
    {
        WorldCommandService service = new WorldCommandService(simulationTime: world.SimulationTime);
        Assert.That(
            TrustedLocalUiWorldCommandHandlerRegistration.RegisterActorActionChoiceHandler(service, world),
            Is.True);
        return service;
    }

    private static WorldCommand CreateCommand(
        WorldCommandAuthorityMode authority,
        WorldCommandOrigin origin = WorldCommandOrigin.LocalPlayer)
    {
        return new WorldCommand(
            WorldCommandKind.ActorActionChoice,
            origin,
            authority,
            new ActorActionChoiceWorldCommandPayload(
                new PersonId("person.local-player"),
                "action.sell-goods"));
    }
}
