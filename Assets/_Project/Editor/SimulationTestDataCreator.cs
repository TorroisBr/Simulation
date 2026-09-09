using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SimulationTestDataCreator
{
    private const string DataFolder = "Assets/_Project/Data";
    private const string EconomyFolder = DataFolder + "/Economy";
    private const string ItemFolder = EconomyFolder + "/Items";
    private const string CityFolder = EconomyFolder + "/Cities";
    private const string ActionFolder = DataFolder + "/Actions";
    private const string JobFolder = DataFolder + "/Jobs";
    private const string NpcFolder = DataFolder + "/Npcs";
    private const string StatusFolder = DataFolder + "/Status";
    private const string SimulationFolder = DataFolder + "/Simulations";

    [MenuItem("Simulation/Create Economy Test")]
    public static void CreateEconomyTest()
    {
        TestData data = CreateSharedTestData();
        SimulationConfigData config = CreateSimulationConfig(
            "Simulation-EconomyTest",
            "Economy Test",
            new[] { SimulationModule.Economy, SimulationModule.Merchant },
            new[] { data.campoVerde, data.serraDeFerro },
            new[]
            {
                CreateNpcConfig(data.jorge, data.serraDeFerro, 200f)
            },
            new[] { data.buyGoods, data.sellGoods, data.travel },
            new[] { data.livre },
            new[] { data.merchantJob },
            null,
            data.livre,
            null,
            null,
            null,
            10f);

        FinishCreation(config, "Economy Test");
    }

    [MenuItem("Simulation/Create Test Economy Data")]
    public static void CreateLegacyEconomyTest()
    {
        CreateEconomyTest();
    }

    [MenuItem("Simulation/Create Guard Test")]
    public static void CreateGuardTest()
    {
        TestData data = CreateSharedTestData();
        SimulationConfigData config = CreateSimulationConfig(
            "Simulation-GuardTest",
            "Guard Test",
            new[] { SimulationModule.GuardCrime },
            new[] { data.guardTestCity },
            new[]
            {
                CreateNpcConfig(data.marcus, data.guardTestCity, 0f),
                CreateNpcConfig(data.jobson, data.guardTestCity, 0f)
            },
            new[] { data.arrest },
            new[] { data.livre, data.procurado, data.preso, data.escondido },
            new[] { data.guardJob },
            new[] { CreateInitialWarrantConfig(data.jobson, data.guardTestCity, 100f, 3) },
            data.livre,
            data.procurado,
            data.preso,
            data.escondido,
            10f);

        FinishCreation(config, "Guard Test");
    }

    [MenuItem("Simulation/Create Crime Justice Test")]
    public static void CreateCrimeJusticeTest()
    {
        TestData data = CreateSharedTestData();
        SimulationConfigData config = CreateSimulationConfig(
            "Simulation-CrimeJusticeTest",
            "Crime Justice Test",
            new[] { SimulationModule.Crime, SimulationModule.GuardCrime },
            new[] { data.campoVerde, data.serraDeFerro },
            new[]
            {
                CreateNpcConfig(data.jobson, data.campoVerde, 35f),
                CreateNpcConfig(data.marcus, data.campoVerde, 0f),
                CreateNpcConfig(data.jorge, data.campoVerde, 120f)
            },
            new[] { data.steal, data.hide, data.fleeCity, data.escapePrison, data.travel, data.arrest },
            new[] { data.livre, data.procurado, data.preso, data.escondido },
            new[] { data.guardJob, data.merchantJob },
            null,
            data.livre,
            data.procurado,
            data.preso,
            data.escondido,
            10f);

        FinishCreation(config, "Crime Justice Test");
    }

    [MenuItem("Simulation/Create General Test")]
    public static void CreateGeneralTest()
    {
        TestData data = CreateSharedTestData();
        SimulationConfigData config = CreateSimulationConfig(
            "Simulation-GeneralTest",
            "General Test",
            new[] { SimulationModule.Economy, SimulationModule.Merchant, SimulationModule.Crime, SimulationModule.GuardCrime },
            new[] { data.campoVerde, data.serraDeFerro },
            new[]
            {
                CreateNpcConfig(data.jorge, data.campoVerde, 200f),
                CreateNpcConfig(data.jobson, data.campoVerde, 35f),
                CreateNpcConfig(data.marcus, data.campoVerde, 0f)
            },
            new[] { data.buyGoods, data.sellGoods, data.travel, data.steal, data.hide, data.fleeCity, data.escapePrison, data.arrest },
            new[] { data.livre, data.procurado, data.preso, data.escondido },
            new[] { data.merchantJob, data.guardJob },
            null,
            data.livre,
            data.procurado,
            data.preso,
            data.escondido,
            10f);

        FinishCreation(config, "General Test");
    }

    private static TestData CreateSharedTestData()
    {
        EnsureBaseFolders();

        TestData data = new TestData();

        data.trigo = CreateItem("Item-Trigo", "TRIGO", 4f);
        data.ferro = CreateItem("Item-Ferro", "FERRO", 20f);
        data.vinho = CreateItem("Item-Vinho", "VINHO", 14f);
        data.tecido = CreateItem("Item-Tecido", "TECIDO", 10f);
        data.sal = CreateItem("Item-Sal", "SAL", 6f);

        data.livre = CreateStatus("Status-Livre", "LIVRE");
        data.procurado = CreateStatus("Status-Procurado", "PROCURADO");
        data.preso = CreateStatus("Status-Preso", "PRESO");
        data.escondido = CreateStatus("Status-Escondido", "ESCONDIDO");

        data.buyGoods = CreateAction("Action-ComprarMercadoria", "COMPRAR_MERCADORIA", NpcActionCategory.Commerce, NpcActionType.BuyGoods, 10f, false, 1f, data.livre);
        data.sellGoods = CreateAction("Action-VenderMercadoria", "VENDER_MERCADORIA", NpcActionCategory.Commerce, NpcActionType.SellGoods, 10f, false, 1f, data.livre);
        data.travel = CreateAction("Action-Viajar", "VIAJAR", NpcActionCategory.Travel, NpcActionType.Travel, 5f, false, 1f, data.livre);
        data.arrest = CreateAction("Action-Prender", "PRENDER", NpcActionCategory.Justice, NpcActionType.Arrest, 10f, true, 0.5f, data.livre);
        data.steal = CreateAction("Action-Roubar", "ROUBAR", NpcActionCategory.Crime, NpcActionType.Steal, 35f, true, 0.75f, data.livre);
        ConfigureCrimeSettings(data.steal, 20, 50f, 3, 1, 0f);
        data.hide = CreateAction("Action-EsconderSe", "ESCONDER_SE", NpcActionCategory.Crime, NpcActionType.Hide, 25f, true, 0.6f, data.livre);
        ConfigureCrimeSettings(data.hide, 0, 0f, 0, 1, 0f);
        data.fleeCity = CreateAction("Action-FugirDaCidade", "FUGIR_DA_CIDADE", NpcActionCategory.Crime, NpcActionType.FleeCity, 20f, false, 1f, data.livre);
        ConfigureCrimeSettings(data.fleeCity, 0, 0f, 0, 1, 0f);
        data.escapePrison = CreateAction("Action-Fugir", "FUGIR_DA_PRISAO", NpcActionCategory.Crime, NpcActionType.EscapePrison, 30f, true, 0.35f, data.preso);
        ConfigureCrimeSettings(data.escapePrison, 0, 0f, 0, 1, 25f);

        data.merchantJob = CreateJob("Job-Mercador", "Mercador", NpcJobType.Merchant, data.buyGoods, 80f);
        data.guardJob = CreateJob("Job-Guarda", "Guarda", NpcJobType.Guard, data.arrest, 80f);

        data.jorge = CreateNpc("NPC-Jorge", "JORGE", "Jorge", data.merchantJob, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 40f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 80f },
            new NPCDefaultAction { action = data.travel, baseUtility = 60f });

        data.marcus = CreateNpc("NPC-Marcus", "MARCUS", "Marcus", data.guardJob, new[] { data.livre },
            new NPCDefaultAction { action = data.arrest, baseUtility = 70f });

        data.jobson = CreateNpc("NPC-Jobson", "JOBSON", "Jobson", null, new[] { data.livre },
            new NPCDefaultAction { action = data.steal, baseUtility = 70f },
            new NPCDefaultAction { action = data.hide, baseUtility = 35f },
            new NPCDefaultAction { action = data.fleeCity, baseUtility = 45f },
            new NPCDefaultAction { action = data.escapePrison, baseUtility = 35f });

        data.campoVerde = CreateCampoVerde(data);
        data.serraDeFerro = CreateSerraDeFerro(data);
        data.guardTestCity = CreateGuardTestCity();

        data.campoVerde.connections = new List<CityConnection>
        {
            new CityConnection { destination = data.serraDeFerro, travelDays = 3 }
        };

        data.serraDeFerro.connections = new List<CityConnection>
        {
            new CityConnection { destination = data.campoVerde, travelDays = 3 }
        };

        EditorUtility.SetDirty(data.campoVerde);
        EditorUtility.SetDirty(data.serraDeFerro);

        return data;
    }

    private static ItemData CreateItem(string assetName, string itemName, float basePrice)
    {
        ItemData item = CreateOrLoadAsset<ItemData>($"{ItemFolder}/{assetName}.asset");
        item.itemName = itemName;
        item.basePrice = basePrice;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static NpcStatusData CreateStatus(string assetName, string statusName)
    {
        NpcStatusData status = CreateOrLoadAsset<NpcStatusData>($"{StatusFolder}/{assetName}.asset");
        status.statusName = statusName;
        EditorUtility.SetDirty(status);
        return status;
    }

    private static NpcActionData CreateAction(string assetName, string actionName, NpcActionCategory actionCategory, NpcActionType actionType, float baseUtility, bool canFail, float baseSuccessChance, params NpcStatusData[] requiredStatus)
    {
        NpcActionData action = CreateOrLoadAsset<NpcActionData>($"{ActionFolder}/{assetName}.asset");
        action.actionName = actionName;
        action.actionCategory = actionCategory;
        action.actionType = actionType;
        action.baseUtility = baseUtility;
        action.canFail = canFail;
        action.baseSuccessChance = Mathf.Clamp01(baseSuccessChance);

        EnsureList(ref action.statusNecessariosParaFazerAcao);
        EnsureList(ref action.statusModifiers);
        EnsureList(ref action.statusToAdd);
        EnsureList(ref action.statusToRemove);
        EnsureList(ref action.targetStatusToAdd);
        EnsureList(ref action.targetStatusToRemove);
        if (action.crimeSettings == null)
        {
            action.crimeSettings = new CrimeActionSettings();
        }

        ReplaceList(action.statusNecessariosParaFazerAcao, requiredStatus);
        action.statusModifiers.Clear();
        action.statusToAdd.Clear();
        action.statusToRemove.Clear();
        action.targetStatusToAdd.Clear();
        action.targetStatusToRemove.Clear();

        EditorUtility.SetDirty(action);
        return action;
    }

    private static void ConfigureCrimeSettings(NpcActionData action, int amount, float bounty, int sentenceDays, int hiddenDays, float escapeBountyPenalty)
    {
        if (action == null)
        {
            return;
        }

        if (action.crimeSettings == null)
        {
            action.crimeSettings = new CrimeActionSettings();
        }
        action.crimeSettings.amount = Mathf.Max(0, amount);
        action.crimeSettings.bounty = Mathf.Max(0f, bounty);
        action.crimeSettings.sentenceDays = Mathf.Max(0, sentenceDays);
        action.crimeSettings.hiddenDays = Mathf.Max(1, hiddenDays);
        action.crimeSettings.escapeBountyPenalty = Mathf.Max(0f, escapeBountyPenalty);
        EditorUtility.SetDirty(action);
    }

    private static NpcJobData CreateJob(string assetName, string jobName, NpcJobType jobType, NpcActionData workAction, float workUtility)
    {
        NpcJobData job = CreateOrLoadAsset<NpcJobData>($"{JobFolder}/{assetName}.asset");
        job.jobName = jobName;
        job.jobType = jobType;
        job.workAction = workAction;
        job.workUtility = workUtility;
        EnsureList(ref job.preferredTradeItems);
        EditorUtility.SetDirty(job);
        return job;
    }

    private static NpcData CreateNpc(string assetName, string id, string npcName, NpcJobData job, NpcStatusData[] defaultStatus, params NPCDefaultAction[] defaultActions)
    {
        NpcData npc = CreateOrLoadAsset<NpcData>($"{NpcFolder}/{assetName}.asset");
        npc.id = id;
        npc.name = npcName;
        npc.job = job;

        EnsureList(ref npc.statusPadrao);
        EnsureList(ref npc.acoesPadrao);
        ReplaceList(npc.statusPadrao, defaultStatus);
        ReplaceList(npc.acoesPadrao, defaultActions);

        EditorUtility.SetDirty(npc);
        return npc;
    }

    private static CityData CreateCampoVerde(TestData data)
    {
        CityData campoVerde = CreateOrLoadAsset<CityData>($"{CityFolder}/City-CampoVerde.asset");
        campoVerde.id = "CAMPO_VERDE";
        campoVerde.cityName = "Campo Verde";
        campoVerde.initialPopulation = 1000;
        campoVerde.marketItems = new List<MarketItemConfig>
        {
            new MarketItemConfig { item = data.trigo, initialAmount = 420, desiredAmount = 250, consumptionPer1000Population = 10f },
            new MarketItemConfig { item = data.ferro, initialAmount = 30, desiredAmount = 180, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = data.vinho, initialAmount = 120, desiredAmount = 120, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = data.tecido, initialAmount = 90, desiredAmount = 120, consumptionPer1000Population = 2f },
            new MarketItemConfig { item = data.sal, initialAmount = 80, desiredAmount = 100, consumptionPer1000Population = 1f }
        };
        campoVerde.productionConfigs = new List<CityProductionConfig>
        {
            new CityProductionConfig { item = data.trigo, amountPerDay = 40 },
            new CityProductionConfig { item = data.vinho, amountPerDay = 4 },
            new CityProductionConfig { item = data.tecido, amountPerDay = 3 }
        };

        EditorUtility.SetDirty(campoVerde);
        return campoVerde;
    }

    private static CityData CreateSerraDeFerro(TestData data)
    {
        CityData serraDeFerro = CreateOrLoadAsset<CityData>($"{CityFolder}/City-SerraDeFerro.asset");
        serraDeFerro.id = "SERRA_DE_FERRO";
        serraDeFerro.cityName = "Serra de Ferro";
        serraDeFerro.initialPopulation = 800;
        serraDeFerro.marketItems = new List<MarketItemConfig>
        {
            new MarketItemConfig { item = data.trigo, initialAmount = 45, desiredAmount = 220, consumptionPer1000Population = 8f },
            new MarketItemConfig { item = data.ferro, initialAmount = 420, desiredAmount = 180, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = data.vinho, initialAmount = 55, desiredAmount = 100, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = data.tecido, initialAmount = 75, desiredAmount = 110, consumptionPer1000Population = 2f },
            new MarketItemConfig { item = data.sal, initialAmount = 60, desiredAmount = 90, consumptionPer1000Population = 1f }
        };
        serraDeFerro.productionConfigs = new List<CityProductionConfig>
        {
            new CityProductionConfig { item = data.ferro, amountPerDay = 35 },
            new CityProductionConfig { item = data.sal, amountPerDay = 3 }
        };

        EditorUtility.SetDirty(serraDeFerro);
        return serraDeFerro;
    }

    private static CityData CreateGuardTestCity()
    {
        CityData city = CreateOrLoadAsset<CityData>($"{CityFolder}/City-GuardTest.asset");
        city.id = "GUARD_TEST";
        city.cityName = "Vila Teste";
        city.initialPopulation = 0;
        city.marketItems = new List<MarketItemConfig>();
        city.productionConfigs = new List<CityProductionConfig>();
        city.connections = new List<CityConnection>();
        EditorUtility.SetDirty(city);
        return city;
    }

    private static SimulationConfigData CreateSimulationConfig(string assetName, string simulationName, SimulationModule[] modules, CityData[] cities, NpcSimulationConfig[] npcs, NpcActionData[] actions, NpcStatusData[] statuses, NpcJobData[] jobs, InitialWantedRecordConfig[] initialWarrants, NpcStatusData freeStatus, NpcStatusData wantedStatus, NpcStatusData arrestedStatus, NpcStatusData hiddenStatus, float travelCostPerDay)
    {
        SimulationConfigData config = CreateOrLoadAsset<SimulationConfigData>($"{SimulationFolder}/{assetName}.asset");
        config.simulationName = simulationName;
        config.freeStatus = freeStatus;
        config.wantedStatus = wantedStatus;
        config.arrestedStatus = arrestedStatus;
        config.hiddenStatus = hiddenStatus;
        config.travelCostPerDay = Mathf.Max(0f, travelCostPerDay);

        EnsureList(ref config.enabledModules);
        EnsureList(ref config.cities);
        EnsureList(ref config.npcs);
        EnsureList(ref config.actions);
        EnsureList(ref config.statuses);
        EnsureList(ref config.jobs);
        EnsureList(ref config.initialWarrants);

        ReplaceList(config.enabledModules, modules);
        ReplaceList(config.cities, cities);
        ReplaceList(config.npcs, npcs);
        ReplaceList(config.actions, actions);
        ReplaceList(config.statuses, statuses);
        ReplaceList(config.jobs, jobs);
        ReplaceList(config.initialWarrants, initialWarrants);

        EditorUtility.SetDirty(config);
        return config;
    }

    private static NpcSimulationConfig CreateNpcConfig(NpcData npc, CityData startingCity, float initialMoney)
    {
        return new NpcSimulationConfig
        {
            npc = npc,
            startingCity = startingCity,
            initialMoney = initialMoney,
            initialInventory = new List<NpcInitialInventoryItemConfig>()
        };
    }

    private static InitialWantedRecordConfig CreateInitialWarrantConfig(NpcData target, CityData city, float bounty, int sentenceDays)
    {
        return new InitialWantedRecordConfig
        {
            target = target,
            city = city,
            bounty = Mathf.Max(0f, bounty),
            sentenceDays = Mathf.Max(1, sentenceDays)
        };
    }

    private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureBaseFolders()
    {
        EnsureFolder(DataFolder, "Economy");
        EnsureFolder(EconomyFolder, "Items");
        EnsureFolder(EconomyFolder, "Cities");
        EnsureFolder(DataFolder, "Actions");
        EnsureFolder(DataFolder, "Jobs");
        EnsureFolder(DataFolder, "Npcs");
        EnsureFolder(DataFolder, "Status");
        EnsureFolder(DataFolder, "Simulations");
    }

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        string fullPath = $"{parentFolder}/{folderName}";

        if (AssetDatabase.IsValidFolder(fullPath) == false)
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    private static void EnsureList<T>(ref List<T> list)
    {
        if (list == null)
        {
            list = new List<T>();
        }
    }

    private static void ReplaceList<T>(List<T> list, params T[] values)
    {
        if (list == null)
        {
            return;
        }

        list.Clear();

        if (values == null)
        {
            return;
        }

        foreach (T value in values)
        {
            if (CanAddListValue(value) == true)
            {
                list.Add(value);
            }
        }
    }

    private static bool CanAddListValue<T>(T value)
    {
        if (typeof(T).IsValueType == true)
        {
            return true;
        }

        object boxedValue = value;
        return boxedValue != null;
    }

    private static void FinishCreation(SimulationConfigData config, string label)
    {
        ConfigureOpenSimulation(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{label} criado em {SimulationFolder}.");
    }

    private static void ConfigureOpenSimulation(SimulationConfigData config)
    {
        TesteSimulacao simulation = Object.FindFirstObjectByType<TesteSimulacao>();

        if (simulation == null)
        {
            Debug.Log("Nenhum TesteSimulacao encontrado na cena aberta. Os assets foram criados; atribua um SimulationConfigData ao componente quando quiser testar.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(simulation);
        SetInt(serializedObject, "daysToSimulate", 8);
        SetInt(serializedObject, "maxMerchantTradeAmount", 5);
        SetFloat(serializedObject, "minimumProfitPerItem", 1f);
        SetObject(serializedObject, "simulationConfig", config);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(simulation);
        EditorSceneManager.MarkSceneDirty(simulation.gameObject.scene);
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private class TestData
    {
        public ItemData trigo;
        public ItemData ferro;
        public ItemData vinho;
        public ItemData tecido;
        public ItemData sal;
        public NpcStatusData livre;
        public NpcStatusData procurado;
        public NpcStatusData preso;
        public NpcStatusData escondido;
        public NpcActionData buyGoods;
        public NpcActionData sellGoods;
        public NpcActionData travel;
        public NpcActionData arrest;
        public NpcActionData steal;
        public NpcActionData hide;
        public NpcActionData fleeCity;
        public NpcActionData escapePrison;
        public NpcJobData merchantJob;
        public NpcJobData guardJob;
        public NpcData jorge;
        public NpcData marcus;
        public NpcData jobson;
        public CityData campoVerde;
        public CityData serraDeFerro;
        public CityData guardTestCity;
    }
}
