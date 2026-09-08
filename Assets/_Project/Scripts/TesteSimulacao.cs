using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private int daysToSimulate = 1;
    [SerializeField] private int maxMerchantTradeAmount = 5;
    [SerializeField] private float minimumProfitPerItem = 1f;

    [SerializeField] private List<NpcData> npcList = new List<NpcData>();
    [SerializeField] private List<NpcStatusData> npcStatusList = new List<NpcStatusData>();
    [SerializeField] private List<NpcActionData> npcActionList = new List<NpcActionData>();
    [SerializeField] private List<CityData> cityList = new List<CityData>();
    [SerializeField] private List<NpcStartingCityConfig> npcStartingCities = new List<NpcStartingCityConfig>();

    [SerializeField] private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    [SerializeField] private List<CityRuntime> cityRuntimeList = new List<CityRuntime>();

    private Dictionary<CityData, CityRuntime> cityRuntimeByData = new Dictionary<CityData, CityRuntime>();
    private NpcDecisionSystem npcDecisionSystem;
    private MerchantSystem merchantSystem;
    private TravelSystem travelSystem;
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
        CityRuntimeList.Clear();
        cityRuntimeByData.Clear();

        if (cityList != null)
        {
            foreach (CityData cityData in cityList)
            {
                if (cityData == null)
                {
                    continue;
                }

                CityRuntime cityRuntime = new CityRuntime(cityData);
                CityRuntimeList.Add(cityRuntime);
                cityRuntimeByData[cityData] = cityRuntime;
            }
        }

        RebuildSystems();

        NpcRuntimeList.Clear();

        if (npcList == null)
        {
            return;
        }

        foreach (NpcData npcData in npcList)
        {
            if (npcData == null)
            {
                continue;
            }

            NpcStartingCityConfig startingConfig = NpcStartingCities.Find(x => x != null && x.npc == npcData);
            CityRuntime startingCity = null;
            float initialMoney = 0f;

            if (startingConfig != null)
            {
                startingCity = GetCityRuntime(startingConfig.startingCity);
                initialMoney = startingConfig.initialMoney;
            }
            else if (CityRuntimeList.Count > 0)
            {
                startingCity = CityRuntimeList[0];
            }

            NpcRuntimeList.Add(new NpcRuntime(npcData, startingCity, initialMoney));
        }
    }

    private void Simulate(int daysToSimulate)
    {
        RebuildSystems();

        for (int i = 0; i < daysToSimulate; i++)
        {
            currentDay++;
            Debug.Log($"Dia {currentDay}");

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

    private List<NpcStartingCityConfig> NpcStartingCities
    {
        get
        {
            if (npcStartingCities == null)
            {
                npcStartingCities = new List<NpcStartingCityConfig>();
            }

            return npcStartingCities;
        }
    }

    private void RebuildSystems()
    {
        travelSystem = new TravelSystem(GetCityRuntime);
        merchantSystem = new MerchantSystem(maxMerchantTradeAmount, minimumProfitPerItem, travelSystem);
        npcDecisionSystem = new NpcDecisionSystem(merchantSystem);
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime chosenAction = npcDecisionSystem.ChooseAction(npcRuntime, npcActionList);
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

        bool actionSucceeded = TryExecuteAction(npcRuntime, actionRuntime, action);

        if (actionSucceeded == true)
        {
            ApplySuccessStatusChanges(npcRuntime, action);
        }
    }

    private bool TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        if (action.actionType == NpcActionType.BuyGoods)
        {
            return merchantSystem.TryExecuteBuyGoods(npcRuntime, actionRuntime);
        }

        if (action.actionType == NpcActionType.SellGoods)
        {
            return merchantSystem.TryExecuteSellGoods(npcRuntime, actionRuntime);
        }

        if (action.actionType == NpcActionType.Travel)
        {
            return travelSystem.TryStartTravel(npcRuntime, actionRuntime);
        }

        return true;
    }

    private void ApplySuccessStatusChanges(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (action.statusToRemove != null)
        {
            foreach (NpcStatusData status in action.statusToRemove)
            {
                npcRuntime.RemoveStatus(status);
            }
        }

        if (action.statusToAdd != null)
        {
            foreach (NpcStatusData status in action.statusToAdd)
            {
                npcRuntime.AddStatus(status);
            }
        }
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
}

[Serializable]
public class NpcStartingCityConfig
{
    public NpcData npc;
    public CityData startingCity;
    public float initialMoney = 100f;
}
