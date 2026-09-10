using UnityEngine;

public class TravelActionProvider : INpcActionProvider
{
    private readonly TravelSystem travelSystem;
    private readonly MerchantSystem merchantSystem;

    public TravelActionProvider(TravelSystem travelSystem, MerchantSystem merchantSystem = null)
    {
        this.travelSystem = travelSystem;
        this.merchantSystem = merchantSystem;
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
            if (merchantSystem == null)
            {
                utility = 0f;
                return null;
            }

            return merchantSystem.CreateTradeRepositionAction(npcRuntime, action, ref utility);
        }

        if (travelSystem.CanStartTravel(npcRuntime, plan.TargetCity, out _, out float travelCost) == false)
        {
            utility = 0f;
            return null;
        }

        float planUtility = plan.Utility;

        if (plan.Reason == NpcTravelReason.Trade
            && npcRuntime.MerchantTradePlan.IsActive == true
            && npcRuntime.MerchantTradePlan.TargetCity == plan.TargetCity)
        {
            planUtility += Mathf.Min(30f, npcRuntime.MerchantTradePlan.PendingTravelDays * 10f);
        }

        utility = Mathf.Max(utility, Mathf.Clamp(planUtility, 0f, 100f));
        return new NpcActionRuntime(action, plan.TargetCity, null, plan.Reason, travelCost, 0f);
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        CityRuntime originCity = npcRuntime != null ? npcRuntime.CurrentCity : null;

        if (actionRuntime != null && actionRuntime.TravelReason == NpcTravelReason.TradeReposition)
        {
            if (travelSystem == null || travelSystem.CanStartTravel(npcRuntime, actionRuntime.TargetCity, out _, out _) == false)
            {
                return NpcActionResult.Failed();
            }

            merchantSystem?.LogTradeRepositionDecision(npcRuntime, originCity, actionRuntime);
        }

        if (travelSystem == null || travelSystem.TryStartTravel(npcRuntime, actionRuntime) == false)
        {
            return NpcActionResult.Failed();
        }

        npcRuntime.ClearTravelPlan();
        return NpcActionResult.Succeeded();
    }
}
