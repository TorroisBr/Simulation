public interface INpcActionProvider
{
    bool HandlesAction(NpcActionData action);
    NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility);
    NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime);
}
