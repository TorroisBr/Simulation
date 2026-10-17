public interface INpcActionProvider
{
    bool HandlesAction(NpcActionData action);
    NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility);
    NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime);
}

/// <summary>Optional provider dispatch that reads action type from an admitted Daily-v1 graph.</summary>
internal interface ISelectedDefinitionNpcActionProvider
{
    bool HandlesAction(NpcRuntime npcRuntime, NpcActionData action);
}

public interface INpcActionFailureHandler
{
    NpcActionResult HandleActionFailure(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime);
}

public interface IAutonomousNpcActionPolicy
{
    bool AllowAutonomousAction(NpcActionData action);
}
