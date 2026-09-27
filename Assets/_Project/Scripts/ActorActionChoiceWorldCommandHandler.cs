using System;

/// <summary>
/// Bridges a trusted local UI request into the runtime's stable actor-choice
/// queue. Dispatch and domain effects are performed later by the simulation.
/// </summary>
public sealed class ActorActionChoiceWorldCommandHandler : IWorldCommandHandler, IRuntimeMutationGuardSource
{
    private readonly SimulationRuntime worldRuntime;

    public WorldCommandKind Kind => WorldCommandKind.ActorActionChoice;

    internal AuthoritativeMutationGuard RuntimeMutationGuard => worldRuntime.MutationGuard;
    AuthoritativeMutationGuard IRuntimeMutationGuardSource.RuntimeMutationGuard => RuntimeMutationGuard;

    public ActorActionChoiceWorldCommandHandler(SimulationRuntime worldRuntime)
    {
        this.worldRuntime = worldRuntime ?? throw new ArgumentNullException(nameof(worldRuntime));
    }

    public WorldCommandPreview Preview(WorldCommand command)
    {
        if (!TryValidate(command, allowSuggest: true, out string diagnostic))
        {
            return new WorldCommandPreview(
                Kind,
                command == null ? WorldCommandAuthorityMode.Suggest : command.Authority,
                false,
                presentation: diagnostic,
                warnings: new[] { diagnostic });
        }

        string presentation = command.Authority == WorldCommandAuthorityMode.Suggest
            ? "Actor action choice can be requested."
            : "Actor action choice is ready to queue.";
        return new WorldCommandPreview(Kind, command.Authority, true, presentation: presentation);
    }

    public WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (!TryValidate(command, allowSuggest: false, out string diagnostic))
        {
            return new WorldCommandHandlerResult(false, diagnostic);
        }

        if (context == null || string.IsNullOrWhiteSpace(context.WorldCommandId))
        {
            return new WorldCommandHandlerResult(false, "World command correlation ID is required.");
        }

        ActorActionChoiceWorldCommandPayload payload = (ActorActionChoiceWorldCommandPayload)command.Payload;
        if (!worldRuntime.TryCaptureActorChoiceInput(
                context.WorldCommandId,
                payload.PersonId,
                payload.ActionDefinitionId,
                command.Origin,
                command.Authority,
                out ActorChoiceStoreFailureCode failure))
        {
            return new WorldCommandHandlerResult(false, "Actor action choice could not be queued: " + failure + ".");
        }

        return new WorldCommandHandlerResult(true, "Actor action choice queued.");
    }

    private bool TryValidate(WorldCommand command, bool allowSuggest, out string diagnostic)
    {
        diagnostic = null;
        if (command == null)
        {
            diagnostic = "Actor action choice command is required.";
            return false;
        }

        if (command.Origin != WorldCommandOrigin.LocalPlayer)
        {
            diagnostic = "Actor action choice requires LocalPlayer provenance.";
            return false;
        }

        if (!(command.Payload is ActorActionChoiceWorldCommandPayload))
        {
            diagnostic = "Actor action choice payload is invalid.";
            return false;
        }

        if (allowSuggest && command.Authority == WorldCommandAuthorityMode.Suggest)
        {
            return true;
        }

        if (command.Authority == WorldCommandAuthorityMode.Request)
        {
            return true;
        }

        diagnostic = allowSuggest
            ? "Actor action choice accepts Suggest preview or Request authority."
            : "Actor action choice requires Request authority.";
        return false;
    }
}
