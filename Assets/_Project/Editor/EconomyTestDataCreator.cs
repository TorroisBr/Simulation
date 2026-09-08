using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EconomyTestDataCreator
{
    private const string EconomyFolder = "Assets/_Project/Data/Economy";
    private const string ItemFolder = EconomyFolder + "/Items";
    private const string CityFolder = EconomyFolder + "/Cities";
    private const string ActionFolder = "Assets/_Project/Data/Actions";
    private const string JobFolder = "Assets/_Project/Data/Jobs";
    private const string NpcFolder = "Assets/_Project/Data/Npcs";
    private const string StatusFolder = "Assets/_Project/Data/Status";

    [MenuItem("Simulation/Create Test Economy Data")]
    public static void CreateTestEconomyData()
    {
        EnsureFolder("Assets/_Project/Data", "Economy");
        EnsureFolder(EconomyFolder, "Items");
        EnsureFolder(EconomyFolder, "Cities");

        ItemData trigo = CreateItem("Item-Trigo", "TRIGO", 4f);
        ItemData ferro = CreateItem("Item-Ferro", "FERRO", 20f);
        ItemData vinho = CreateItem("Item-Vinho", "VINHO", 14f);
        ItemData tecido = CreateItem("Item-Tecido", "TECIDO", 10f);
        ItemData sal = CreateItem("Item-Sal", "SAL", 6f);

        NpcStatusData livre = CreateOrLoadAsset<NpcStatusData>($"{StatusFolder}/Status-Livre.asset");
        livre.statusName = "Livre";
        EditorUtility.SetDirty(livre);

        NpcActionData buyGoods = CreateAction("Action-ComprarMercadoria", "COMPRAR_MERCADORIA", NpcActionType.BuyGoods, livre, 10f);
        NpcActionData sellGoods = CreateAction("Action-VenderMercadoria", "VENDER_MERCADORIA", NpcActionType.SellGoods, livre, 10f);
        NpcActionData travel = CreateAction("Action-Viajar", "VIAJAR", NpcActionType.Travel, livre, 5f);

        NpcJobData merchantJob = CreateOrLoadAsset<NpcJobData>($"{JobFolder}/Job-Mercador.asset");
        merchantJob.jobName = "Mercador";
        merchantJob.jobType = NpcJobType.Merchant;
        merchantJob.workAction = buyGoods;
        merchantJob.workUtility = 80f;
        EditorUtility.SetDirty(merchantJob);

        NpcData jorge = CreateOrLoadAsset<NpcData>($"{NpcFolder}/NPC-Jorge.asset");
        jorge.id = "JORGE";
        jorge.name = "Jorge";
        jorge.job = merchantJob;

        if (jorge.statusPadrao == null)
        {
            jorge.statusPadrao = new List<NpcStatusData>();
        }

        if (jorge.acoesPadrao == null)
        {
            jorge.acoesPadrao = new List<NPCDefaultAction>();
        }

        ReplaceList(jorge.statusPadrao, livre);
        ReplaceList(jorge.acoesPadrao,
            new NPCDefaultAction { action = buyGoods, baseUtility = 40f },
            new NPCDefaultAction { action = sellGoods, baseUtility = 80f },
            new NPCDefaultAction { action = travel, baseUtility = 60f });
        EditorUtility.SetDirty(jorge);

        CityData campoVerde = CreateOrLoadAsset<CityData>($"{CityFolder}/City-CampoVerde.asset");
        campoVerde.id = "CAMPO_VERDE";
        campoVerde.cityName = "Campo Verde";
        campoVerde.initialPopulation = 1000;
        campoVerde.marketItems = new List<MarketItemConfig>
        {
            new MarketItemConfig { item = trigo, initialAmount = 420, desiredAmount = 250, consumptionPer1000Population = 10f },
            new MarketItemConfig { item = ferro, initialAmount = 30, desiredAmount = 180, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = vinho, initialAmount = 120, desiredAmount = 120, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = tecido, initialAmount = 90, desiredAmount = 120, consumptionPer1000Population = 2f },
            new MarketItemConfig { item = sal, initialAmount = 80, desiredAmount = 100, consumptionPer1000Population = 1f }
        };
        campoVerde.productionConfigs = new List<CityProductionConfig>
        {
            new CityProductionConfig { item = trigo, amountPerDay = 40 },
            new CityProductionConfig { item = vinho, amountPerDay = 4 },
            new CityProductionConfig { item = tecido, amountPerDay = 3 }
        };

        CityData serraDeFerro = CreateOrLoadAsset<CityData>($"{CityFolder}/City-SerraDeFerro.asset");
        serraDeFerro.id = "SERRA_DE_FERRO";
        serraDeFerro.cityName = "Serra de Ferro";
        serraDeFerro.initialPopulation = 800;
        serraDeFerro.marketItems = new List<MarketItemConfig>
        {
            new MarketItemConfig { item = trigo, initialAmount = 45, desiredAmount = 220, consumptionPer1000Population = 8f },
            new MarketItemConfig { item = ferro, initialAmount = 420, desiredAmount = 180, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = vinho, initialAmount = 55, desiredAmount = 100, consumptionPer1000Population = 1f },
            new MarketItemConfig { item = tecido, initialAmount = 75, desiredAmount = 110, consumptionPer1000Population = 2f },
            new MarketItemConfig { item = sal, initialAmount = 60, desiredAmount = 90, consumptionPer1000Population = 1f }
        };
        serraDeFerro.productionConfigs = new List<CityProductionConfig>
        {
            new CityProductionConfig { item = ferro, amountPerDay = 35 },
            new CityProductionConfig { item = sal, amountPerDay = 3 }
        };

        campoVerde.connections = new List<CityConnection>
        {
            new CityConnection { destination = serraDeFerro, travelDays = 3 }
        };

        serraDeFerro.connections = new List<CityConnection>
        {
            new CityConnection { destination = campoVerde, travelDays = 3 }
        };

        EditorUtility.SetDirty(campoVerde);
        EditorUtility.SetDirty(serraDeFerro);

        ConfigureOpenSimulation(jorge, livre, buyGoods, sellGoods, travel, campoVerde, serraDeFerro);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Dados de teste de economia criados em Assets/_Project/Data/Economy.");
    }

    private static ItemData CreateItem(string assetName, string itemName, float basePrice)
    {
        ItemData item = CreateOrLoadAsset<ItemData>($"{ItemFolder}/{assetName}.asset");
        item.itemName = itemName;
        item.basePrice = basePrice;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static NpcActionData CreateAction(string assetName, string actionName, NpcActionType actionType, NpcStatusData requiredStatus, float baseUtility)
    {
        NpcActionData action = CreateOrLoadAsset<NpcActionData>($"{ActionFolder}/{assetName}.asset");
        action.actionName = actionName;
        action.actionType = actionType;
        action.baseUtility = baseUtility;

        ReplaceList(action.statusNecessariosParaFazerAcao, requiredStatus);

        if (action.statusModifiers == null)
        {
            action.statusModifiers = new List<StatusWeightModifier>();
        }

        if (action.statusToAdd == null)
        {
            action.statusToAdd = new List<NpcStatusData>();
        }

        if (action.statusToRemove == null)
        {
            action.statusToRemove = new List<NpcStatusData>();
        }

        EditorUtility.SetDirty(action);
        return action;
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

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        string fullPath = $"{parentFolder}/{folderName}";

        if (AssetDatabase.IsValidFolder(fullPath) == false)
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    private static void ReplaceList<T>(List<T> list, params T[] values)
    {
        if (list == null)
        {
            return;
        }

        list.Clear();

        foreach (T value in values)
        {
            if (value != null)
            {
                list.Add(value);
            }
        }
    }

    private static void ConfigureOpenSimulation(NpcData jorge, NpcStatusData livre, NpcActionData buyGoods, NpcActionData sellGoods, NpcActionData travel, CityData campoVerde, CityData serraDeFerro)
    {
        TesteSimulacao simulation = Object.FindFirstObjectByType<TesteSimulacao>();

        if (simulation == null)
        {
            Debug.Log("Nenhum TesteSimulacao encontrado na cena aberta. Os assets foram criados; atribua-os manualmente ao componente quando quiser testar.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(simulation);

        SetInt(serializedObject, "daysToSimulate", 8);
        SetInt(serializedObject, "maxMerchantTradeAmount", 5);
        SetFloat(serializedObject, "minimumProfitPerItem", 1f);
        SetObjectList(serializedObject.FindProperty("npcList"), jorge);
        SetObjectList(serializedObject.FindProperty("npcStatusList"), livre);
        SetObjectList(serializedObject.FindProperty("npcActionList"), buyGoods, sellGoods, travel);
        SetObjectList(serializedObject.FindProperty("cityList"), campoVerde, serraDeFerro);

        SerializedProperty startingCities = serializedObject.FindProperty("npcStartingCities");

        if (startingCities != null)
        {
            startingCities.ClearArray();
            startingCities.arraySize = 1;

            SerializedProperty entry = startingCities.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("npc").objectReferenceValue = jorge;
            entry.FindPropertyRelative("startingCity").objectReferenceValue = serraDeFerro;
            entry.FindPropertyRelative("initialMoney").floatValue = 200f;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(simulation);
        EditorSceneManager.MarkSceneDirty(simulation.gameObject.scene);
    }

    private static void SetObjectList(SerializedProperty property, params Object[] values)
    {
        if (property == null)
        {
            return;
        }

        property.ClearArray();
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
}
