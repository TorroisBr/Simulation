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

        MerchantTradePlanRuntime plan = npcRuntime.MerchantTradePlan;

        if (plan.IsActive == false || plan.TargetCity == null || plan.TargetCity == npcRuntime.CurrentCity)
        {
            utility = 0f;
            return null;
        }

        int travelDays = travelSystem.GetTravelDays(npcRuntime.CurrentCity, plan.TargetCity);

        if (travelDays <= 0)
        {
            plan.RedirectTo(npcRuntime.CurrentCity);
            Debug.Log($"{npcRuntime.NpcName} nao encontrou rota para o destino do plano e vai reavaliar venda local.");
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 70f);
        return new NpcActionRuntime(action, plan.TargetCity, plan.Item, plan.RemainingAmount, 0f);
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (travelSystem == null || travelSystem.TryStartTravel(npcRuntime, actionRuntime) == false)
        {
            return NpcActionResult.Failed();
        }

        return NpcActionResult.Succeeded();
    }
}
