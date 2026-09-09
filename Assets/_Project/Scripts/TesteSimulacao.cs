using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private SimulationConfigData simulationConfig;
    [SerializeField] private int daysToSimulate = 1;
    [SerializeField] private int maxMerchantTradeAmount = 5;
    [SerializeField] private float minimumProfitPerItem = 1f;

    private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    private List<CityRuntime> cityRuntimeList = new List<CityRuntime>();

    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();
    private Dictionary<CityData, CityRuntime> cityRuntimeByData = new Dictionary<CityData, CityRuntime>();
    private Dictionary<NpcData, NpcRuntime> npcRuntimeByData = new Dictionary<NpcData, NpcRuntime>();
    private SimulationModuleSet enabledModules;
    private JusticeSystem justiceSystem;
    private CrimeSystem crimeSystem;
    private NpcDecisionSystem npcDecisionSystem;
    private TravelSystem travelSystem;
    private SimulationLogger logger;
    private int currentDay;

    public void Start()
    {
        InitializeSimulation();
    }

    public void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            Simulate(Mathf.Max(1, daysToSimulate));
        }
    }

    private void InitializeSimulation()
    {
        currentDay = 0;
        logger = new SimulationLogger(simulationConfig != null ? simulationConfig.LogSettings : null);
        enabledModules = new SimulationModuleSet(simulationConfig);

        CityRuntimeList.Clear();
        cityRuntimeByData.Clear();
        CreateCityRuntimes();

        NpcRuntimeList.Clear();
        npcRuntimeByData.Clear();
        CreateNpcRuntimes();

        RebuildSystems();
        InitializeJusticeState();
    }

    private void Simulate(int daysToSimulate)
    {
        for (int i = 0; i < daysToSimulate; i++)
        {
            currentDay++;
            logger.Log(SimulationLogCategory.Day, $"Dia {currentDay}");
            BeginSimulationDay();

            if (enabledModules.IsEnabled(SimulationModule.Economy) == true)
            {
                SimulateEconomyDay();
            }

            foreach (NpcRuntime npcRuntime in NpcRuntimeList)
            {
                if (npcRuntime == null || npcRuntime.IsTraveling == true)
                {
                    continue;
                }

                EvaluateStatus(npcRuntime);
                EvaluateAction(npcRuntime);
                TryExecuteCurrentAction(npcRuntime);
            }

            travelSystem.AdvanceTravels(NpcRuntimeList);
        }
    }

    private List<NpcRuntime> NpcRuntimeList
    {
        get
        {
            if (npcRuntimeList == null)
            {
                npcRuntimeList = new List<NpcRuntime>();
            }

            return npcRuntimeList;
        }
    }

    private List<CityRuntime> CityRuntimeList
    {
        get
        {
            if (cityRuntimeList == null)
            {
                cityRuntimeList = new List<CityRuntime>();
            }

            return cityRuntimeList;
        }
    }

    private List<NpcActionData> ConfiguredActions
    {
        get
        {
            if (simulationConfig == null)
            {
                return null;
            }

            return simulationConfig.Actions;
        }
    }

    private void CreateCityRuntimes()
    {
        if (simulationConfig == null)
        {
            logger.LogWarning("Nenhum SimulationConfigData configurado. A simulacao iniciara sem cidades nem NPCs.");
            return;
        }

        foreach (CityData cityData in simulationConfig.Cities)
        {
            if (cityData == null)
            {
                continue;
            }

            CityRuntime cityRuntime = new CityRuntime(cityData, logger);
            CityRuntimeList.Add(cityRuntime);
            cityRuntimeByData[cityData] = cityRuntime;
        }
    }

    private void CreateNpcRuntimes()
    {
        if (simulationConfig == null)
        {
            return;
        }

        foreach (NpcSimulationConfig npcConfig in simulationConfig.Npcs)
        {
            if (npcConfig == null || npcConfig.npc == null)
            {
                continue;
            }

            CityRuntime startingCity = GetCityRuntime(npcConfig.startingCity);
            NpcRuntime npcRuntime = new NpcRuntime(npcConfig.npc, startingCity, npcConfig.initialMoney);
            ApplyInitialInventory(npcRuntime, npcConfig);
            NpcRuntimeList.Add(npcRuntime);
            npcRuntimeByData[npcConfig.npc] = npcRuntime;
        }
    }

    private void ApplyInitialInventory(NpcRuntime npcRuntime, NpcSimulationConfig npcConfig)
    {
        if (npcRuntime == null || npcConfig == null)
        {
            return;
        }

        foreach (NpcInitialInventoryItemConfig inventoryConfig in npcConfig.InitialInventory)
        {
            if (inventoryConfig == null)
            {
                continue;
            }

            npcRuntime.Inventory.AddItem(inventoryConfig.item, inventoryConfig.amount, inventoryConfig.averageUnitCost);
        }
    }

    private void RebuildSystems()
    {
        enabledModules = new SimulationModuleSet(simulationConfig);
        logger = logger ?? new SimulationLogger(simulationConfig != null ? simulationConfig.LogSettings : null);
        float travelCostPerDay = simulationConfig != null ? simulationConfig.travelCostPerDay : 0f;
        travelSystem = new TravelSystem(GetCityRuntime, travelCostPerDay, logger);
        justiceSystem = simulationConfig != null
            ? new JusticeSystem(simulationConfig.freeStatus, simulationConfig.wantedStatus, simulationConfig.arrestedStatus, simulationConfig.hiddenStatus, logger)
            : null;
        crimeSystem = null;
        actionProviders.Clear();
        actionProviders.Add(new TravelActionProvider(travelSystem));

        if (enabledModules.IsEnabled(SimulationModule.Merchant) == true)
        {
            actionProviders.Add(new MerchantSystem(maxMerchantTradeAmount, minimumProfitPerItem, travelSystem, logger));
        }

        if (enabledModules.IsEnabled(SimulationModule.Crime) == true && justiceSystem != null)
        {
            crimeSystem = new CrimeSystem(justiceSystem, travelSystem, simulationConfig.hiddenStatus, logger);
            actionProviders.Add(crimeSystem);
        }

        if (enabledModules.IsEnabled(SimulationModule.GuardCrime) == true && justiceSystem != null)
        {
            actionProviders.Add(new GuardSystem(justiceSystem, simulationConfig.hiddenStatus));
        }

        npcDecisionSystem = new NpcDecisionSystem(actionProviders);
    }

    private void InitializeJusticeState()
    {
        if (justiceSystem == null)
        {
            return;
        }

        justiceSystem.CreateInitialWarrants(simulationConfig, GetNpcRuntime, GetCityRuntime);
        justiceSystem.SyncWantedStatuses(NpcRuntimeList);
    }

    private void BeginSimulationDay()
    {
        if (crimeSystem != null)
        {
            crimeSystem.AdvanceHiddenStatuses(NpcRuntimeList);
        }

        if (enabledModules.IsEnabled(SimulationModule.GuardCrime) == true && justiceSystem != null)
        {
            justiceSystem.AdvanceSentences(NpcRuntimeList);
        }

        if (justiceSystem != null)
        {
            justiceSystem.SyncWantedStatuses(NpcRuntimeList);
        }
    }

    private void SimulateEconomyDay()
    {
        foreach (CityRuntime cityRuntime in CityRuntimeList)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            cityRuntime.SimulateProductionDay();
        }

        foreach (CityRuntime cityRuntime in CityRuntimeList)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            cityRuntime.SimulateConsumptionDay();
            cityRuntime.UpdateMarketPrices();
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime chosenAction = npcDecisionSystem.ChooseAction(npcRuntime, ConfiguredActions);
        npcRuntime.SetCurrentActionRuntime(chosenAction);
    }

    private void TryExecuteCurrentAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime actionRuntime = npcRuntime.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npcRuntime.CurrentAction;

        if (action == null)
        {
            return;
        }

        LogChosenTargetAction(npcRuntime, actionRuntime);
        NpcActionResult actionResult = TryExecuteAction(npcRuntime, actionRuntime, action);

        if (actionResult != null && string.IsNullOrEmpty(actionResult.Message) == false)
        {
            logger.Log(SimulationLogCategory.NpcAction, actionResult.Message);
        }

        if (actionResult != null && actionResult.Success == true)
        {
            ApplySuccessStatusChanges(npcRuntime, actionRuntime, action);
        }
    }

    private NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        if (RollActionSuccess(action) == false)
        {
            return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
        }

        if (action.actionType == NpcActionType.Normal)
        {
            return NpcActionResult.Succeeded(CreateNormalActionMessage(npcRuntime, action));
        }

        INpcActionProvider actionProvider = npcDecisionSystem.GetProviderForAction(action);

        if (actionProvider == null)
        {
            return NpcActionResult.Failed();
        }

        return actionProvider.TryExecuteAction(npcRuntime, actionRuntime);
    }

    private bool RollActionSuccess(NpcActionData action)
    {
        if (action == null || action.canFail == false)
        {
            return true;
        }

        return Random.value <= Mathf.Clamp01(action.baseSuccessChance);
    }

    private void ApplySuccessStatusChanges(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        ApplyStatusChanges(npcRuntime, action.statusToRemove, action.statusToAdd);

        if (actionRuntime != null && actionRuntime.TargetNpc != null)
        {
            ApplyStatusChanges(actionRuntime.TargetNpc, action.targetStatusToRemove, action.targetStatusToAdd);
        }
    }

    private void ApplyStatusChanges(NpcRuntime npcRuntime, List<NpcStatusData> statusToRemove, List<NpcStatusData> statusToAdd)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (statusToRemove != null)
        {
            foreach (NpcStatusData status in statusToRemove)
            {
                npcRuntime.RemoveStatus(status);
            }
        }

        if (statusToAdd != null)
        {
            foreach (NpcStatusData status in statusToAdd)
            {
                npcRuntime.AddStatus(status);
            }
        }
    }

    private void LogChosenTargetAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || actionRuntime == null || actionRuntime.Action == null || actionRuntime.TargetNpc == null)
        {
            if (npcRuntime == null || actionRuntime == null || actionRuntime.Action == null || actionRuntime.TargetCity == null)
            {
                return;
            }

            logger.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetCity.CityName}.");
            return;
        }

        logger.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetNpc.NpcName}.");
    }

    private string CreateFailureMessage(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";
        string targetName = actionRuntime != null && actionRuntime.TargetNpc != null ? $" {actionRuntime.TargetNpc.NpcName}" : string.Empty;
        string targetCityName = actionRuntime != null && actionRuntime.TargetCity != null ? $" {actionRuntime.TargetCity.CityName}" : string.Empty;
        return $"{actorName} tentou {GetActionName(action)}{targetName}{targetCityName}, mas falhou.";
    }

    private string CreateNormalActionMessage(NpcRuntime npcRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";

        if (action != null && string.IsNullOrEmpty(action.normalActionLogText) == false)
        {
            return $"{actorName} {action.normalActionLogText}";
        }

        return $"{actorName} realizou {GetActionName(action)}.";
    }

    private string GetActionName(NpcActionData action)
    {
        if (action == null)
        {
            return "acao desconhecida";
        }

        return string.IsNullOrEmpty(action.actionName) == false ? action.actionName : action.actionType.ToString();
    }

    private CityRuntime GetCityRuntime(CityData cityData)
    {
        if (cityData == null)
        {
            return null;
        }

        cityRuntimeByData.TryGetValue(cityData, out CityRuntime cityRuntime);
        return cityRuntime;
    }

    private NpcRuntime GetNpcRuntime(NpcData npcData)
    {
        if (npcData == null)
        {
            return null;
        }

        npcRuntimeByData.TryGetValue(npcData, out NpcRuntime npcRuntime);
        return npcRuntime;
    }
}
