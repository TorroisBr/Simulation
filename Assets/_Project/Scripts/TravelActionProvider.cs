using UnityEngine;

public class TravelActionProvider : INpcActionProvider
{
    private readonly TravelSystem travelSystem;

    public TravelActionProvider(TravelSystem travelSystem)
    {
        this.travelSystem = travelSystem;
    }

    public bool HandlesAction(NpcActionData action)
    {
        return action != null && action.actionType == NpcActionType.Travel;
    }

    public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (npcRuntime == null || npcRuntime.CurrentCity == null || travelSystem == null)
        {
            utility = 0f;
            return null;
        }

        NpcTravelPlanRuntime plan = npcRuntime.TravelPlan;

        if (plan.IsActive == false || plan.TargetCity == null || plan.TargetCity == npcRuntime.CurrentCity)
        {
            utility = 0f;
            return null;
        }

        if (travelSystem.CanStartTravel(npcRuntime, plan.TargetCity, out _, out float travelCost) == false)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, plan.Utility);
        return new NpcActionRuntime(action, plan.TargetCity, null, 0, travelCost);
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (travelSystem == null || travelSystem.TryStartTravel(npcRuntime, actionRuntime) == false)
        {
            return NpcActionResult.Failed();
        }

        npcRuntime.ClearTravelPlan();
        return NpcActionResult.Succeeded();
    }
}
