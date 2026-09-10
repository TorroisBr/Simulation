using System.Collections.Generic;
using UnityEngine;

public class CrimeSystem : INpcActionProvider, INpcActionFailureHandler
{
    private const float EscapeSuccessMultiplierPerFailure = 0.8f;
    private readonly JusticeSystem justiceSystem;
    private readonly TravelSystem travelSystem;
    private readonly NpcStatusData hiddenStatus;
    private readonly SimulationLogger logger;

    public CrimeSystem(JusticeSystem justiceSystem, TravelSystem travelSystem, NpcStatusData hiddenStatus, SimulationLogger logger = null)
    {
        this.justiceSystem = justiceSystem;
        this.travelSystem = travelSystem;
        this.hiddenStatus = hiddenStatus;
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool HandlesAction(NpcActionData action)
    {
        if (action == null)
        {
            return false;
        }

        return action.actionType == NpcActionType.Steal
            || action.actionType == NpcActionType.Hide
            || action.actionType == NpcActionType.FleeCity
            || action.actionType == NpcActionType.EscapePrison;
    }

    public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (action == null || justiceSystem == null)
        {
            utility = 0f;
            return null;
        }

        if (action.actionType == NpcActionType.Steal)
        {
            return CreateStealAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.Hide)
        {
            return CreateHideAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.FleeCity)
        {
            return CreateFleeCityAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.EscapePrison)
        {
            return CreateEscapePrisonAction(npcRuntime, action, ref utility);
        }

        utility = 0f;
        return null;
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (actionRuntime == null || actionRuntime.Action == null)
        {
            return NpcActionResult.Failed();
        }

        if (actionRuntime.Action.actionType == NpcActionType.Steal)
        {
            return TryExecuteSteal(npcRuntime, actionRuntime);
        }

        if (actionRuntime.Action.actionType == NpcActionType.Hide)
        {
            return TryExecuteHide(npcRuntime, actionRuntime);
        }

        if (actionRuntime.Action.actionType == NpcActionType.FleeCity)
        {
            return TryExecuteFleeCity(npcRuntime, actionRuntime);
        }

        if (actionRuntime.Action.actionType == NpcActionType.EscapePrison)
        {
            return TryExecuteEscapePrison(npcRuntime, actionRuntime);
        }

        return NpcActionResult.Failed();
    }

    public NpcActionResult HandleActionFailure(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (actionRuntime == null || actionRuntime.Action == null || actionRuntime.Action.actionType != NpcActionType.EscapePrison)
        {
            return null;
        }

        CrimeActionSettings settings = GetCrimeSettings(actionRuntime.Action);
        int sentencePenalty = Mathf.Max(0, settings.failedEscapeSentencePenalty);

        if (justiceSystem.RegisterFailedEscape(npcRuntime, sentencePenalty) == false)
        {
            return null;
        }

        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} tentou fugir da prisao e falhou. Pena aumentou em {sentencePenalty} dias. Vigilancia aumentou.");
        return NpcActionResult.Failed();
    }

    public void AdvanceHiddenStatuses(List<NpcRuntime> npcRuntimeList)
    {
        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            if (npcRuntime == null)
            {
                continue;
            }

            if (npcRuntime.IsHidden == false)
            {
                npcRuntime.RemoveStatus(hiddenStatus);
                continue;
            }

            if (hiddenStatus != null && npcRuntime.CurrentStatus.Contains(hiddenStatus) == false)
            {
                npcRuntime.AddStatus(hiddenStatus);
            }

            if (npcRuntime.AdvanceHiddenDay() == true)
            {
                npcRuntime.RemoveStatus(hiddenStatus);
                logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} nao esta mais escondido.");
            }
        }
    }

    private NpcActionRuntime CreateStealAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (CanActInCity(npcRuntime) == false || npcRuntime.TravelPlan.IsActive == true || HasNpcDefaultAction(npcRuntime, action) == false)
        {
            utility = 0f;
            return null;
        }

        NpcRuntime target = FindStealTarget(npcRuntime);

        if (target == null)
        {
            utility = 0f;
            return null;
        }

        int amount = GetConfiguredAmount(action, target);

        if (amount <= 0)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(0f, utility) + GetMoneyPressureBonus(npcRuntime) + Mathf.Min(8f, amount * 0.2f);
        return new NpcActionRuntime(action, target, amount);
    }

    private NpcActionRuntime CreateHideAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (CanActInCity(npcRuntime) == false || npcRuntime.IsHidden == true || npcRuntime.TravelPlan.IsActive == true || justiceSystem.HasActiveWarrantInCity(npcRuntime, npcRuntime.CurrentCity) == false)
        {
            utility = 0f;
            return null;
        }

        float bounty = justiceSystem.GetBounty(npcRuntime, npcRuntime.CurrentCity);
        utility = Mathf.Max(utility, 20f + bounty * 0.15f);
        return new NpcActionRuntime(action);
    }

    private NpcActionRuntime CreateFleeCityAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (CanActInCity(npcRuntime) == false || npcRuntime.IsHidden == true || npcRuntime.TravelPlan.IsActive == true || justiceSystem.HasActiveWarrantInCity(npcRuntime, npcRuntime.CurrentCity) == false)
        {
            utility = 0f;
            return null;
        }

        FleeDestinationOption option = FindBestFleeDestination(npcRuntime);

        if (option == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, option.Utility);
        return new NpcActionRuntime(action, option.TargetCity, null, 0, option.TravelCost);
    }

    private NpcActionRuntime CreateEscapePrisonAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (npcRuntime == null || justiceSystem.IsArrested(npcRuntime) == false || justiceSystem.WasArrestedToday(npcRuntime) == true)
        {
            utility = 0f;
            return null;
        }

        int remainingDays = justiceSystem.GetRemainingSentenceDays(npcRuntime);

        if (remainingDays <= 1)
        {
            utility = 0f;
            return null;
        }

        float remainingSentenceUrgency = Mathf.Clamp01((remainingDays - 1f) / 7f);
        utility = Mathf.Clamp(10f + remainingSentenceUrgency * 20f, 10f, 30f);

        NpcActionRuntime actionRuntime = new NpcActionRuntime(action);
        int failedAttempts = justiceSystem.GetFailedEscapeAttempts(npcRuntime);
        actionRuntime.SetSuccessChanceMultiplier(Mathf.Pow(EscapeSuccessMultiplierPerFailure, failedAttempts));
        return actionRuntime;
    }

    private NpcActionResult TryExecuteSteal(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (CanActInCity(npcRuntime) == false || IsValidStealTarget(npcRuntime, actionRuntime.TargetNpc) == false)
        {
            return NpcActionResult.Failed();
        }

        int amount = Mathf.Min(actionRuntime.Amount, Mathf.FloorToInt(actionRuntime.TargetNpc.Money));

        if (amount <= 0 || actionRuntime.TargetNpc.TrySpendMoney(amount) == false)
        {
            return NpcActionResult.Failed();
        }

        npcRuntime.AddMoney(amount);
        CrimeActionSettings settings = GetCrimeSettings(actionRuntime.Action);
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} roubou {actionRuntime.TargetNpc.NpcName} e levou {amount} moedas.");
        justiceSystem.CreateOrIncreaseWarrant(npcRuntime, npcRuntime.CurrentCity, settings.bounty, settings.sentenceDays);
        return NpcActionResult.Succeeded();
    }

    private NpcActionResult TryExecuteHide(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (CanActInCity(npcRuntime) == false || justiceSystem.HasActiveWarrantInCity(npcRuntime, npcRuntime.CurrentCity) == false)
        {
            return NpcActionResult.Failed();
        }

        int hiddenDays = Mathf.Max(1, GetCrimeSettings(actionRuntime.Action).hiddenDays);
        npcRuntime.HideForDays(hiddenDays);
        npcRuntime.AddStatus(hiddenStatus);
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} esta escondido.");
        return NpcActionResult.Succeeded();
    }

    private NpcActionResult TryExecuteFleeCity(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (CanActInCity(npcRuntime) == false || actionRuntime.TargetCity == null)
        {
            return NpcActionResult.Failed();
        }

        if (travelSystem == null || travelSystem.CanStartTravel(npcRuntime, actionRuntime.TargetCity, out _, out float travelCost) == false)
        {
            return NpcActionResult.Failed();
        }

        npcRuntime.SetTravelPlan(actionRuntime.TargetCity, NpcTravelReason.Flee, 80f, travelCost);
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} decidiu fugir para {actionRuntime.TargetCity.CityName}. Custo de viagem: {travelCost:0.##}.");
        return NpcActionResult.Succeeded();
    }

    private NpcActionResult TryExecuteEscapePrison(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        CrimeActionSettings settings = GetCrimeSettings(actionRuntime.Action);

        if (justiceSystem.EscapePrison(npcRuntime, settings.escapeBountyPenalty) == false)
        {
            return NpcActionResult.Failed();
        }

        string wantedText = justiceSystem.HasAnyActiveWarrant(npcRuntime) == true ? "continua procurado" : "nao possui mandado ativo";
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} esta livre e {wantedText}.");
        return NpcActionResult.Succeeded();
    }

    private NpcRuntime FindStealTarget(NpcRuntime thiefRuntime)
    {
        float totalWeight = 0f;

        foreach (NpcRuntime candidate in thiefRuntime.CurrentCity.ImportantNpcs)
        {
            if (IsValidStealTarget(thiefRuntime, candidate) == false)
            {
                continue;
            }

            totalWeight += GetStealTargetWeight(candidate);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (NpcRuntime candidate in thiefRuntime.CurrentCity.ImportantNpcs)
        {
            if (IsValidStealTarget(thiefRuntime, candidate) == false)
            {
                continue;
            }

            currentWeight += GetStealTargetWeight(candidate);

            if (randomValue <= currentWeight)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsValidStealTarget(NpcRuntime thiefRuntime, NpcRuntime targetRuntime)
    {
        return targetRuntime != null
            && targetRuntime != thiefRuntime
            && targetRuntime.CurrentCity == thiefRuntime.CurrentCity
            && targetRuntime.IsTraveling == false
            && targetRuntime.IsHidden == false
            && justiceSystem.IsArrested(targetRuntime) == false
            && targetRuntime.Money > 0f;
    }

    private FleeDestinationOption FindBestFleeDestination(NpcRuntime npcRuntime)
    {
        if (travelSystem == null || npcRuntime == null || npcRuntime.CurrentCity == null || npcRuntime.CurrentCity.CityData == null || npcRuntime.CurrentCity.CityData.connections == null)
        {
            return null;
        }

        float currentBounty = justiceSystem.GetBounty(npcRuntime, npcRuntime.CurrentCity);
        FleeDestinationOption bestOption = null;

        foreach (CityConnection connection in npcRuntime.CurrentCity.CityData.connections)
        {
            if (connection == null || connection.destination == null)
            {
                continue;
            }

            CityRuntime targetCity = travelSystem.GetSingleCityRuntimeByDefinition(connection.destination);

            if (targetCity == null || targetCity == npcRuntime.CurrentCity)
            {
                continue;
            }

            if (travelSystem.CanStartTravel(npcRuntime, targetCity, out int travelDays, out float travelCost) == false)
            {
                continue;
            }

            float destinationBounty = justiceSystem.GetBounty(npcRuntime, targetCity);
            float safetyGain = currentBounty - destinationBounty;

            if (safetyGain <= 0f)
            {
                continue;
            }

            float score = (safetyGain - travelCost) / Mathf.Max(1, travelDays);

            if (score <= 0f)
            {
                continue;
            }

            float utility = 30f + score;

            if (bestOption == null || utility > bestOption.Utility)
            {
                bestOption = new FleeDestinationOption(targetCity, travelCost, utility);
            }
        }

        return bestOption;
    }

    private bool CanActInCity(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && npcRuntime.CurrentCity != null
            && npcRuntime.IsTraveling == false
            && justiceSystem.IsArrested(npcRuntime) == false;
    }

    private bool HasNpcDefaultAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (npcRuntime == null || npcRuntime.NpcData == null || npcRuntime.NpcData.acoesPadrao == null)
        {
            return false;
        }

        return npcRuntime.NpcData.acoesPadrao.Exists(x => x != null && x.action == action);
    }

    private int GetConfiguredAmount(NpcActionData action, NpcRuntime targetRuntime)
    {
        int configuredAmount = Mathf.Max(0, GetCrimeSettings(action).amount);
        return Mathf.Min(configuredAmount, Mathf.FloorToInt(targetRuntime.Money));
    }

    private float GetMoneyPressureBonus(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return 0f;
        }

        return Mathf.Clamp((50f - npcRuntime.Money) * 0.35f, 0f, 18f);
    }

    private float GetStealTargetWeight(NpcRuntime candidate)
    {
        return candidate != null ? Mathf.Max(1f, candidate.Money) : 0f;
    }

    private CrimeActionSettings GetCrimeSettings(NpcActionData action)
    {
        return action != null && action.crimeSettings != null ? action.crimeSettings : new CrimeActionSettings();
    }

    private class FleeDestinationOption
    {
        public CityRuntime TargetCity { get; }
        public float TravelCost { get; }
        public float Utility { get; }

        public FleeDestinationOption(CityRuntime targetCity, float travelCost, float utility)
        {
            TargetCity = targetCity;
            TravelCost = travelCost;
            Utility = utility;
        }
    }
}
