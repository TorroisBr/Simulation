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
            10f,
            CreateLogSettings(true, true, true, true, false, false, true, true, true));

        FinishCreation(config, "Economy Test");
    }

    [MenuItem("Simulation/Create Economic Network Test")]
    public static void CreateEconomicNetworkTest()
    {
        TestData data = CreateEconomicNetworkTestData();
        SimulationConfigData config = CreateSimulationConfig(
            "Simulation-EconomicNetworkTest",
            "Economic Network Test",
            new[] { SimulationModule.Economy, SimulationModule.Merchant },
            new[] { data.networkCampoVerde, data.networkSerraDeFerro, data.portoAzul, data.bosqueAlto, data.valeDoCouro, data.feiraCentral },
            new[]
            {
                CreateNpcConfig(data.networkCampoTravelerOne, data.networkCampoVerde, 320f),
                CreateNpcConfig(data.networkCampoTravelerTwo, data.networkCampoVerde, 260f),
                CreateNpcConfig(data.networkSerraTravelerOne, data.networkSerraDeFerro, 400f),
                CreateNpcConfig(data.networkSerraTravelerTwo, data.networkSerraDeFerro, 280f),
                CreateNpcConfig(data.networkPortoTravelerOne, data.portoAzul, 350f),
                CreateNpcConfig(data.networkPortoTravelerTwo, data.portoAzul, 300f),
                CreateNpcConfig(data.networkBosqueTravelerOne, data.bosqueAlto, 420f),
                CreateNpcConfig(data.networkBosqueTravelerTwo, data.bosqueAlto, 240f),
                CreateNpcConfig(data.networkValeTravelerOne, data.valeDoCouro, 390f),
                CreateNpcConfig(data.networkValeTravelerTwo, data.valeDoCouro, 310f),
                CreateNpcConfig(data.networkFeiraTravelerOne, data.feiraCentral, 450f),
                CreateNpcConfig(data.networkFeiraTravelerTwo, data.feiraCentral, 275f),
                CreateNpcConfig(data.networkCampoLocal, data.networkCampoVerde, 520f,
                    CreateInitialInventoryConfig(data.trigo, 4, 3f),
                    CreateInitialInventoryConfig(data.vinho, 2, 10f)),
                CreateNpcConfig(data.networkSerraLocal, data.networkSerraDeFerro, 440f,
                    CreateInitialInventoryConfig(data.ferro, 4, 12f),
                    CreateInitialInventoryConfig(data.carvao, 3, 7f)),
                CreateNpcConfig(data.networkPortoLocal, data.portoAzul, 580f,
                    CreateInitialInventoryConfig(data.peixe, 5, 5f),
                    CreateInitialInventoryConfig(data.sal, 3, 3f)),
                CreateNpcConfig(data.networkBosqueLocal, data.bosqueAlto, 360f,
                    CreateInitialInventoryConfig(data.madeira, 5, 4f),
                    CreateInitialInventoryConfig(data.ervas, 3, 3f)),
                CreateNpcConfig(data.networkValeLocal, data.valeDoCouro, 490f,
                    CreateInitialInventoryConfig(data.couro, 4, 10f),
                    CreateInitialInventoryConfig(data.tecido, 3, 7f)),
                CreateNpcConfig(data.networkFeiraLocal, data.feiraCentral, 600f,
                    CreateInitialInventoryConfig(data.trigo, 3, 4f),
                    CreateInitialInventoryConfig(data.couro, 2, 13f))
            },
            new[] { data.buyGoods, data.sellGoods, data.travel, data.rest, data.walk, data.tavern },
            new[] { data.livre },
            new[]
            {
                data.networkAgricultureJob,
                data.networkMiningJob,
                data.networkMaritimeJob,
                data.networkForestryJob,
                data.networkManufacturingJob,
                data.networkGeneralJob,
                data.networkLocalJob
            },
            null,
            data.livre,
            null,
            null,
            null,
            10f,
            CreateLogSettings(true, true, true, true, false, false, true, true, true),
            true,
            10,
            true,
            true,
            12345);

        FinishCreation(config, "Economic Network Test", 20);
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
            10f,
            CreateLogSettings(true, true, false, false, false, true, false, false, false));

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
                CreateNpcConfig(data.jorge, data.campoVerde, 120f),
                CreateNpcConfig(data.ana, data.campoVerde, 60f)
            },
            new[] { data.steal, data.hide, data.fleeCity, data.escapePrison, data.travel, data.arrest, data.rest, data.walk, data.tavern, data.serveSentence },
            new[] { data.livre, data.procurado, data.preso, data.escondido },
            new[] { data.guardJob, data.merchantJob },
            null,
            data.livre,
            data.procurado,
            data.preso,
            data.escondido,
            10f,
            CreateLogSettings(true, true, false, true, true, true, false, false, false));

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
                CreateNpcConfig(data.jorge, data.campoVerde, 220f),
                CreateNpcConfig(data.afonso, data.serraDeFerro, 180f),
                CreateNpcConfig(data.helena, data.serraDeFerro, 170f),
                CreateNpcConfig(data.pedro, data.campoVerde, 220f, CreateInitialInventoryConfig(data.tecido, 4, 6f)),
                CreateNpcConfig(data.maria, data.serraDeFerro, 220f, CreateInitialInventoryConfig(data.ferro, 4, 8f)),
                CreateNpcConfig(data.marcus, data.campoVerde, 20f),
                CreateNpcConfig(data.arthur, data.serraDeFerro, 20f),
                CreateNpcConfig(data.jobson, data.campoVerde, 35f),
                CreateNpcConfig(data.carlos, data.serraDeFerro, 80f),
                CreateNpcConfig(data.ana, data.campoVerde, 65f)
            },
            new[] { data.buyGoods, data.sellGoods, data.travel, data.steal, data.hide, data.fleeCity, data.escapePrison, data.arrest, data.rest, data.walk, data.tavern, data.serveSentence },
            new[] { data.livre, data.procurado, data.preso, data.escondido },
            new[] { data.merchantJob, data.ironMerchantJob, data.wineMerchantJob, data.localMerchantJob, data.guardJob },
            null,
            data.livre,
            data.procurado,
            data.preso,
            data.escondido,
            10f,
            CreateLogSettings(true, true, true, true, true, true, false, false, false));

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
        data.steal = CreateAction("Action-Roubar", "ROUBAR", NpcActionCategory.Crime, NpcActionType.Steal, 20f, true, 0.75f, data.livre);
        ConfigureCrimeSettings(data.steal, 20, 50f, 3, 1, 0f);
        data.hide = CreateAction("Action-EsconderSe", "ESCONDER_SE", NpcActionCategory.Crime, NpcActionType.Hide, 25f, true, 0.6f, data.livre);
        ConfigureCrimeSettings(data.hide, 0, 0f, 0, 1, 0f);
        data.fleeCity = CreateAction("Action-FugirDaCidade", "FUGIR_DA_CIDADE", NpcActionCategory.Crime, NpcActionType.FleeCity, 20f, false, 1f, data.livre);
        ConfigureCrimeSettings(data.fleeCity, 0, 0f, 0, 1, 0f);
        data.escapePrison = CreateAction("Action-Fugir", "FUGIR_DA_PRISAO", NpcActionCategory.Crime, NpcActionType.EscapePrison, 30f, true, 0.25f, data.preso);
        ConfigureCrimeSettings(data.escapePrison, 0, 0f, 0, 1, 25f, 2);
        data.rest = CreateNeutralAction("Action-Descansar", "DESCANSAR", "descansou.", 18f, data.livre);
        data.walk = CreateNeutralAction("Action-Passear", "PASSEAR", "passeou pela cidade.", 20f, data.livre);
        data.tavern = CreateNeutralAction("Action-IrATaverna", "IR_A_TAVERNA", "foi a taverna.", 18f, data.livre);
        data.serveSentence = CreateNeutralAction("Action-CumprirPena", "CUMPRIR_PENA", "permaneceu na prisão.", 60f, data.preso);
        data.serveSentence.actionCategory = NpcActionCategory.Justice;
        EditorUtility.SetDirty(data.serveSentence);

        data.merchantJob = CreateJob("Job-Mercador", "Mercador", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f);
        data.ironMerchantJob = CreateJob("Job-MercadorFerro", "Mercador de Ferro", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.ferro, utilityMultiplier = 2f });
        data.wineMerchantJob = CreateJob("Job-MercadorVinho", "Mercador de Vinho", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.vinho, utilityMultiplier = 2f });
        data.localMerchantJob = CreateJob("Job-MercadorLocal", "Mercador Local", NpcJobType.Merchant, MerchantBehavior.Local, data.buyGoods, 55f);
        data.guardJob = CreateJob("Job-Guarda", "Guarda", NpcJobType.Guard, MerchantBehavior.Traveling, data.arrest, 80f);

        data.jorge = CreateNpc("NPC-Jorge", "JORGE", "Jorge", data.merchantJob, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 40f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 80f },
            new NPCDefaultAction { action = data.travel, baseUtility = 60f },
            new NPCDefaultAction { action = data.rest, baseUtility = 16f },
            new NPCDefaultAction { action = data.walk, baseUtility = 18f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 14f });

        data.afonso = CreateNpc("NPC-Afonso", "AFONSO", "Afonso", data.ironMerchantJob, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 42f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 78f },
            new NPCDefaultAction { action = data.travel, baseUtility = 58f },
            new NPCDefaultAction { action = data.rest, baseUtility = 16f },
            new NPCDefaultAction { action = data.walk, baseUtility = 18f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 14f });

        data.helena = CreateNpc("NPC-Helena", "HELENA", "Helena", data.wineMerchantJob, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 42f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 78f },
            new NPCDefaultAction { action = data.travel, baseUtility = 58f },
            new NPCDefaultAction { action = data.rest, baseUtility = 18f },
            new NPCDefaultAction { action = data.walk, baseUtility = 16f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 16f });

        data.pedro = CreateNpc("NPC-Pedro", "PEDRO", "Pedro", data.localMerchantJob, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 35f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 45f },
            new NPCDefaultAction { action = data.rest, baseUtility = 18f },
            new NPCDefaultAction { action = data.walk, baseUtility = 18f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 16f });

        data.maria = CreateNpc("NPC-Maria", "MARIA", "Maria", data.localMerchantJob, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 35f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 45f },
            new NPCDefaultAction { action = data.rest, baseUtility = 18f },
            new NPCDefaultAction { action = data.walk, baseUtility = 18f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 16f });

        data.marcus = CreateNpc("NPC-Marcus", "MARCUS", "Marcus", data.guardJob, new[] { data.livre },
            new NPCDefaultAction { action = data.arrest, baseUtility = 70f },
            new NPCDefaultAction { action = data.rest, baseUtility = 12f },
            new NPCDefaultAction { action = data.walk, baseUtility = 20f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 10f });

        data.arthur = CreateNpc("NPC-Arthur", "ARTHUR", "Arthur", data.guardJob, new[] { data.livre },
            new NPCDefaultAction { action = data.arrest, baseUtility = 68f },
            new NPCDefaultAction { action = data.rest, baseUtility = 12f },
            new NPCDefaultAction { action = data.walk, baseUtility = 20f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 10f });

        data.jobson = CreateNpc("NPC-Jobson", "JOBSON", "Jobson", null, new[] { data.livre },
            new NPCDefaultAction { action = data.steal, baseUtility = 22f },
            new NPCDefaultAction { action = data.hide, baseUtility = 35f },
            new NPCDefaultAction { action = data.fleeCity, baseUtility = 45f },
            new NPCDefaultAction { action = data.escapePrison, baseUtility = 35f },
            new NPCDefaultAction { action = data.rest, baseUtility = 20f },
            new NPCDefaultAction { action = data.walk, baseUtility = 25f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 22f });

        data.carlos = CreateNpc("NPC-Carlos", "CARLOS", "Carlos", null, new[] { data.livre },
            new NPCDefaultAction { action = data.rest, baseUtility = 22f },
            new NPCDefaultAction { action = data.walk, baseUtility = 28f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 20f });

        data.ana = CreateNpc("NPC-Ana", "ANA", "Ana", null, new[] { data.livre },
            new NPCDefaultAction { action = data.rest, baseUtility = 24f },
            new NPCDefaultAction { action = data.walk, baseUtility = 24f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 18f });

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

    private static TestData CreateEconomicNetworkTestData()
    {
        EnsureBaseFolders();

        TestData data = new TestData();
        data.trigo = CreateItem("Item-Trigo", "TRIGO", 4f);
        data.ferro = CreateItem("Item-Ferro", "FERRO", 20f);
        data.vinho = CreateItem("Item-Vinho", "VINHO", 14f);
        data.tecido = CreateItem("Item-Tecido", "TECIDO", 10f);
        data.sal = CreateItem("Item-Sal", "SAL", 6f);
        data.madeira = CreateItem("Item-Madeira", "MADEIRA", 8f);
        data.peixe = CreateItem("Item-Peixe", "PEIXE", 9f);
        data.couro = CreateItem("Item-Couro", "COURO", 16f);
        data.ervas = CreateItem("Item-Ervas", "ERVAS", 7f);
        data.carvao = CreateItem("Item-Carvao", "CARVAO", 12f);

        data.livre = CreateStatus("Status-Livre", "LIVRE");
        data.buyGoods = CreateAction("Action-ComprarMercadoria", "COMPRAR_MERCADORIA", NpcActionCategory.Commerce, NpcActionType.BuyGoods, 10f, false, 1f, data.livre);
        data.sellGoods = CreateAction("Action-VenderMercadoria", "VENDER_MERCADORIA", NpcActionCategory.Commerce, NpcActionType.SellGoods, 10f, false, 1f, data.livre);
        data.travel = CreateAction("Action-Viajar", "VIAJAR", NpcActionCategory.Travel, NpcActionType.Travel, 5f, false, 1f, data.livre);
        data.rest = CreateNeutralAction("Action-Descansar", "DESCANSAR", "descansou.", 18f, data.livre);
        data.walk = CreateNeutralAction("Action-Passear", "PASSEAR", "passeou pela cidade.", 20f, data.livre);
        data.tavern = CreateNeutralAction("Action-IrATaverna", "IR_A_TAVERNA", "foi a taverna.", 18f, data.livre);

        data.networkAgricultureJob = CreateJob("Job-EconomicNetwork-Agricola", "Mercador Agricola", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.trigo, utilityMultiplier = 2f },
            new TradeItemPreference { item = data.vinho, utilityMultiplier = 1.8f });
        data.networkMiningJob = CreateJob("Job-EconomicNetwork-Minerador", "Mercador Minerador", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.ferro, utilityMultiplier = 2f },
            new TradeItemPreference { item = data.carvao, utilityMultiplier = 1.8f });
        data.networkMaritimeJob = CreateJob("Job-EconomicNetwork-Maritimo", "Mercador Maritimo", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.peixe, utilityMultiplier = 2f },
            new TradeItemPreference { item = data.sal, utilityMultiplier = 1.8f });
        data.networkForestryJob = CreateJob("Job-EconomicNetwork-Florestal", "Mercador Florestal", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.madeira, utilityMultiplier = 2f },
            new TradeItemPreference { item = data.ervas, utilityMultiplier = 1.8f });
        data.networkManufacturingJob = CreateJob("Job-EconomicNetwork-Manufatureiro", "Mercador Manufatureiro", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f,
            new TradeItemPreference { item = data.couro, utilityMultiplier = 2f },
            new TradeItemPreference { item = data.tecido, utilityMultiplier = 1.8f });
        data.networkGeneralJob = CreateJob("Job-EconomicNetwork-Geral", "Mercador Geral", NpcJobType.Merchant, MerchantBehavior.Traveling, data.buyGoods, 80f);
        data.networkLocalJob = CreateJob("Job-EconomicNetwork-Local", "Mercador Local", NpcJobType.Merchant, MerchantBehavior.Local, data.buyGoods, 55f);

        data.networkCampoTravelerOne = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Campo-01", "ECON_CAMPO_01", "Caio", data.networkAgricultureJob);
        data.networkCampoTravelerTwo = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Campo-02", "ECON_CAMPO_02", "Livia", data.networkAgricultureJob);
        data.networkSerraTravelerOne = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Serra-01", "ECON_SERRA_01", "Bruno", data.networkMiningJob);
        data.networkSerraTravelerTwo = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Serra-02", "ECON_SERRA_02", "Iris", data.networkMiningJob);
        data.networkPortoTravelerOne = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Porto-01", "ECON_PORTO_01", "Marina", data.networkMaritimeJob);
        data.networkPortoTravelerTwo = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Porto-02", "ECON_PORTO_02", "Tiago", data.networkMaritimeJob);
        data.networkBosqueTravelerOne = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Bosque-01", "ECON_BOSQUE_01", "Raul", data.networkForestryJob);
        data.networkBosqueTravelerTwo = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Bosque-02", "ECON_BOSQUE_02", "Flora", data.networkForestryJob);
        data.networkValeTravelerOne = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Vale-01", "ECON_VALE_01", "Hugo", data.networkManufacturingJob);
        data.networkValeTravelerTwo = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Vale-02", "ECON_VALE_02", "Clara", data.networkManufacturingJob);
        data.networkFeiraTravelerOne = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Feira-01", "ECON_FEIRA_01", "Nilo", data.networkGeneralJob);
        data.networkFeiraTravelerTwo = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Feira-02", "ECON_FEIRA_02", "Sofia", data.networkGeneralJob);
        data.networkCampoLocal = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Local-Campo", "ECON_LOCAL_CAMPO", "Olivia", data.networkLocalJob);
        data.networkSerraLocal = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Local-Serra", "ECON_LOCAL_SERRA", "Mateus", data.networkLocalJob);
        data.networkPortoLocal = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Local-Porto", "ECON_LOCAL_PORTO", "Lara", data.networkLocalJob);
        data.networkBosqueLocal = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Local-Bosque", "ECON_LOCAL_BOSQUE", "Dario", data.networkLocalJob);
        data.networkValeLocal = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Local-Vale", "ECON_LOCAL_VALE", "Beatriz", data.networkLocalJob);
        data.networkFeiraLocal = CreateEconomicNetworkMerchantNpc(data, "NPC-EconomicNetwork-Local-Feira", "ECON_LOCAL_FEIRA", "Celso", data.networkLocalJob);

        data.networkCampoVerde = CreateEconomicNetworkCampoVerde(data);
        data.networkSerraDeFerro = CreateEconomicNetworkSerraDeFerro(data);
        data.portoAzul = CreateEconomicNetworkPortoAzul(data);
        data.bosqueAlto = CreateEconomicNetworkBosqueAlto(data);
        data.valeDoCouro = CreateEconomicNetworkValeDoCouro(data);
        data.feiraCentral = CreateEconomicNetworkFeiraCentral(data);
        ConfigureEconomicNetworkConnections(data);

        return data;
    }

    private static CityData CreateEconomicNetworkCampoVerde(TestData data)
    {
        return CreateEconomicNetworkCity("City-EconomicNetwork-CampoVerde", "ECONOMIC_NETWORK_CAMPO_VERDE", "Campo Verde", 1200,
            new[]
            {
                CreateMarketItemConfig(data.trigo, 520, 260, 10f), CreateMarketItemConfig(data.ferro, 35, 180, 2f),
                CreateMarketItemConfig(data.vinho, 160, 130, 2f), CreateMarketItemConfig(data.tecido, 90, 120, 2f),
                CreateMarketItemConfig(data.sal, 80, 100, 1f), CreateMarketItemConfig(data.madeira, 50, 150, 3f),
                CreateMarketItemConfig(data.peixe, 45, 130, 4f), CreateMarketItemConfig(data.couro, 70, 100, 1f),
                CreateMarketItemConfig(data.ervas, 80, 100, 1f), CreateMarketItemConfig(data.carvao, 35, 120, 2f)
            },
            new[]
            {
                CreateProductionConfig(data.trigo, 36), CreateProductionConfig(data.vinho, 5), CreateProductionConfig(data.tecido, 3)
            });
    }

    private static CityData CreateEconomicNetworkSerraDeFerro(TestData data)
    {
        return CreateEconomicNetworkCity("City-EconomicNetwork-SerraDeFerro", "ECONOMIC_NETWORK_SERRA_DE_FERRO", "Serra de Ferro", 900,
            new[]
            {
                CreateMarketItemConfig(data.trigo, 50, 220, 10f), CreateMarketItemConfig(data.ferro, 430, 180, 2f),
                CreateMarketItemConfig(data.vinho, 60, 110, 2f), CreateMarketItemConfig(data.tecido, 80, 120, 2f),
                CreateMarketItemConfig(data.sal, 120, 100, 1f), CreateMarketItemConfig(data.madeira, 45, 130, 2f),
                CreateMarketItemConfig(data.peixe, 35, 100, 3f), CreateMarketItemConfig(data.couro, 50, 100, 1f),
                CreateMarketItemConfig(data.ervas, 45, 110, 2f), CreateMarketItemConfig(data.carvao, 360, 170, 2f)
            },
            new[]
            {
                CreateProductionConfig(data.ferro, 32), CreateProductionConfig(data.carvao, 26), CreateProductionConfig(data.sal, 4)
            });
    }

    private static CityData CreateEconomicNetworkPortoAzul(TestData data)
    {
        return CreateEconomicNetworkCity("City-EconomicNetwork-PortoAzul", "ECONOMIC_NETWORK_PORTO_AZUL", "Porto Azul", 1100,
            new[]
            {
                CreateMarketItemConfig(data.trigo, 150, 230, 10f), CreateMarketItemConfig(data.ferro, 70, 150, 2f),
                CreateMarketItemConfig(data.vinho, 90, 120, 2f), CreateMarketItemConfig(data.tecido, 80, 110, 2f),
                CreateMarketItemConfig(data.sal, 380, 180, 2f), CreateMarketItemConfig(data.madeira, 45, 160, 4f),
                CreateMarketItemConfig(data.peixe, 480, 220, 12f), CreateMarketItemConfig(data.couro, 50, 100, 1f),
                CreateMarketItemConfig(data.ervas, 60, 110, 2f), CreateMarketItemConfig(data.carvao, 70, 130, 2f)
            },
            new[]
            {
                CreateProductionConfig(data.peixe, 38), CreateProductionConfig(data.sal, 12)
            });
    }

    private static CityData CreateEconomicNetworkBosqueAlto(TestData data)
    {
        return CreateEconomicNetworkCity("City-EconomicNetwork-BosqueAlto", "ECONOMIC_NETWORK_BOSQUE_ALTO", "Bosque Alto", 700,
            new[]
            {
                CreateMarketItemConfig(data.trigo, 70, 180, 10f), CreateMarketItemConfig(data.ferro, 30, 160, 2f),
                CreateMarketItemConfig(data.vinho, 55, 110, 2f), CreateMarketItemConfig(data.tecido, 40, 100, 2f),
                CreateMarketItemConfig(data.sal, 55, 100, 2f), CreateMarketItemConfig(data.madeira, 420, 190, 5f),
                CreateMarketItemConfig(data.peixe, 35, 100, 4f), CreateMarketItemConfig(data.couro, 45, 100, 1f),
                CreateMarketItemConfig(data.ervas, 300, 150, 4f), CreateMarketItemConfig(data.carvao, 45, 120, 2f)
            },
            new[]
            {
                CreateProductionConfig(data.madeira, 30), CreateProductionConfig(data.ervas, 16)
            });
    }

    private static CityData CreateEconomicNetworkValeDoCouro(TestData data)
    {
        return CreateEconomicNetworkCity("City-EconomicNetwork-ValeDoCouro", "ECONOMIC_NETWORK_VALE_DO_COURO", "Vale do Couro", 850,
            new[]
            {
                CreateMarketItemConfig(data.trigo, 100, 190, 10f), CreateMarketItemConfig(data.ferro, 80, 160, 2f),
                CreateMarketItemConfig(data.vinho, 70, 110, 2f), CreateMarketItemConfig(data.tecido, 300, 170, 3f),
                CreateMarketItemConfig(data.sal, 35, 100, 2f), CreateMarketItemConfig(data.madeira, 80, 130, 3f),
                CreateMarketItemConfig(data.peixe, 50, 100, 4f), CreateMarketItemConfig(data.couro, 360, 180, 4f),
                CreateMarketItemConfig(data.ervas, 70, 110, 2f), CreateMarketItemConfig(data.carvao, 60, 120, 2f)
            },
            new[]
            {
                CreateProductionConfig(data.couro, 26), CreateProductionConfig(data.tecido, 20)
            });
    }

    private static CityData CreateEconomicNetworkFeiraCentral(TestData data)
    {
        return CreateEconomicNetworkCity("City-EconomicNetwork-FeiraCentral", "ECONOMIC_NETWORK_FEIRA_CENTRAL", "Feira Central", 2500,
            new[]
            {
                CreateMarketItemConfig(data.trigo, 220, 500, 20f), CreateMarketItemConfig(data.ferro, 160, 400, 3f),
                CreateMarketItemConfig(data.vinho, 120, 260, 6f), CreateMarketItemConfig(data.tecido, 140, 260, 5f),
                CreateMarketItemConfig(data.sal, 140, 300, 4f), CreateMarketItemConfig(data.madeira, 130, 280, 6f),
                CreateMarketItemConfig(data.peixe, 160, 300, 8f), CreateMarketItemConfig(data.couro, 100, 220, 3f),
                CreateMarketItemConfig(data.ervas, 100, 220, 5f), CreateMarketItemConfig(data.carvao, 120, 260, 4f)
            },
            new CityProductionConfig[0]);
    }

    private static CityData CreateEconomicNetworkCity(string assetName, string id, string cityName, int population, MarketItemConfig[] marketItems, CityProductionConfig[] productionConfigs)
    {
        CityData city = CreateOrLoadAsset<CityData>($"{CityFolder}/{assetName}.asset");
        city.id = id;
        city.cityName = cityName;
        city.initialPopulation = population;
        city.marketItems = new List<MarketItemConfig>(marketItems ?? new MarketItemConfig[0]);
        city.productionConfigs = new List<CityProductionConfig>(productionConfigs ?? new CityProductionConfig[0]);
        city.connections = new List<CityConnection>();
        EditorUtility.SetDirty(city);
        return city;
    }

    private static MarketItemConfig CreateMarketItemConfig(ItemData item, int initialAmount, int desiredAmount, float consumptionPer1000Population)
    {
        return new MarketItemConfig
        {
            item = item,
            initialAmount = initialAmount,
            desiredAmount = desiredAmount,
            consumptionPer1000Population = consumptionPer1000Population
        };
    }

    private static CityProductionConfig CreateProductionConfig(ItemData item, int amountPerDay)
    {
        return new CityProductionConfig
        {
            item = item,
            amountPerDay = amountPerDay
        };
    }

    private static void ConfigureEconomicNetworkConnections(TestData data)
    {
        ConnectBidirectionally(data.networkCampoVerde, data.feiraCentral, 1);
        ConnectBidirectionally(data.networkSerraDeFerro, data.feiraCentral, 2);
        ConnectBidirectionally(data.bosqueAlto, data.feiraCentral, 2);
        ConnectBidirectionally(data.valeDoCouro, data.feiraCentral, 3);
        ConnectBidirectionally(data.portoAzul, data.feiraCentral, 4);
        ConnectBidirectionally(data.networkCampoVerde, data.valeDoCouro, 3);
        ConnectBidirectionally(data.networkCampoVerde, data.portoAzul, 5);
        ConnectBidirectionally(data.networkSerraDeFerro, data.bosqueAlto, 4);
        ConnectBidirectionally(data.portoAzul, data.valeDoCouro, 2);
    }

    private static void ConnectBidirectionally(CityData first, CityData second, int travelDays)
    {
        UpsertConnection(first, second, travelDays);
        UpsertConnection(second, first, travelDays);
    }

    private static void UpsertConnection(CityData source, CityData destination, int travelDays)
    {
        if (source == null || destination == null || source == destination)
        {
            return;
        }

        EnsureList(ref source.connections);
        CityConnection connection = source.connections.Find(x => x != null && x.destination == destination);

        if (connection == null)
        {
            source.connections.Add(new CityConnection { destination = destination, travelDays = Mathf.Max(1, travelDays) });
        }
        else
        {
            connection.travelDays = Mathf.Max(1, travelDays);
        }

        EditorUtility.SetDirty(source);
    }

    private static NpcData CreateEconomicNetworkMerchantNpc(TestData data, string assetName, string id, string npcName, NpcJobData job)
    {
        return CreateNpc(assetName, id, npcName, job, new[] { data.livre },
            new NPCDefaultAction { action = data.buyGoods, baseUtility = 40f },
            new NPCDefaultAction { action = data.sellGoods, baseUtility = 75f },
            new NPCDefaultAction { action = data.travel, baseUtility = 60f },
            new NPCDefaultAction { action = data.rest, baseUtility = 16f },
            new NPCDefaultAction { action = data.walk, baseUtility = 18f },
            new NPCDefaultAction { action = data.tavern, baseUtility = 14f });
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
        action.normalActionLogText = string.Empty;
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

    private static NpcActionData CreateNeutralAction(string assetName, string actionName, string logText, float baseUtility, params NpcStatusData[] requiredStatus)
    {
        NpcActionData action = CreateAction(assetName, actionName, NpcActionCategory.General, NpcActionType.Normal, baseUtility, false, 1f, requiredStatus);
        action.normalActionLogText = logText;
        EditorUtility.SetDirty(action);
        return action;
    }

    private static void ConfigureCrimeSettings(NpcActionData action, int amount, float bounty, int sentenceDays, int hiddenDays, float escapeBountyPenalty, int failedEscapeSentencePenalty = 2)
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
        action.crimeSettings.failedEscapeSentencePenalty = Mathf.Max(0, failedEscapeSentencePenalty);
        EditorUtility.SetDirty(action);
    }

    private static NpcJobData CreateJob(string assetName, string jobName, NpcJobType jobType, MerchantBehavior merchantBehavior, NpcActionData workAction, float workUtility, params TradeItemPreference[] preferredTradeItems)
    {
        NpcJobData job = CreateOrLoadAsset<NpcJobData>($"{JobFolder}/{assetName}.asset");
        job.jobName = jobName;
        job.jobType = jobType;
        job.merchantBehavior = merchantBehavior;
        job.workAction = workAction;
        job.workUtility = workUtility;
        EnsureList(ref job.preferredTradeItems);
        ReplaceList(job.preferredTradeItems, preferredTradeItems);
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

    private static SimulationConfigData CreateSimulationConfig(string assetName, string simulationName, SimulationModule[] modules, CityData[] cities, NpcSimulationConfig[] npcs, NpcActionData[] actions, NpcStatusData[] statuses, NpcJobData[] jobs, InitialWantedRecordConfig[] initialWarrants, NpcStatusData freeStatus, NpcStatusData wantedStatus, NpcStatusData arrestedStatus, NpcStatusData hiddenStatus, float travelCostPerDay, SimulationLogSettings logSettings, bool includeEconomySnapshots = false, int economySnapshotIntervalDays = 10, bool allowMerchantTradeRepositioning = false, bool useFixedSimulationSeed = false, int simulationSeed = 12345)
    {
        SimulationConfigData config = CreateOrLoadAsset<SimulationConfigData>($"{SimulationFolder}/{assetName}.asset");
        config.simulationName = simulationName;
        config.freeStatus = freeStatus;
        config.wantedStatus = wantedStatus;
        config.arrestedStatus = arrestedStatus;
        config.hiddenStatus = hiddenStatus;
        config.travelCostPerDay = Mathf.Max(0f, travelCostPerDay);
        config.allowMerchantTradeRepositioning = allowMerchantTradeRepositioning;
        config.useFixedSimulationSeed = useFixedSimulationSeed;
        config.simulationSeed = simulationSeed;
        config.includeEconomySnapshots = includeEconomySnapshots;
        config.economySnapshotIntervalDays = Mathf.Max(1, economySnapshotIntervalDays);
        config.logSettings = logSettings ?? new SimulationLogSettings();

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

    private static NpcSimulationConfig CreateNpcConfig(NpcData npc, CityData startingCity, float initialMoney, params NpcInitialInventoryItemConfig[] initialInventory)
    {
        NpcSimulationConfig config = new NpcSimulationConfig
        {
            npc = npc,
            startingCity = startingCity,
            initialMoney = initialMoney,
            initialInventory = new List<NpcInitialInventoryItemConfig>()
        };

        if (initialInventory != null)
        {
            foreach (NpcInitialInventoryItemConfig item in initialInventory)
            {
                if (item != null && item.item != null && item.amount > 0)
                {
                    config.initialInventory.Add(item);
                }
            }
        }

        return config;
    }

    private static NpcInitialInventoryItemConfig CreateInitialInventoryConfig(ItemData item, int amount, float averageUnitCost)
    {
        return new NpcInitialInventoryItemConfig
        {
            item = item,
            amount = Mathf.Max(0, amount),
            averageUnitCost = Mathf.Max(0f, averageUnitCost)
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

    private static SimulationLogSettings CreateLogSettings(bool showDay, bool npcActions, bool trade, bool travel, bool crime, bool justice, bool economyProduction, bool economyConsumption, bool market)
    {
        return new SimulationLogSettings
        {
            showDay = showDay,
            npcActions = npcActions,
            trade = trade,
            travel = travel,
            crime = crime,
            justice = justice,
            economyProduction = economyProduction,
            economyConsumption = economyConsumption,
            market = market
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

    private static void FinishCreation(SimulationConfigData config, string label, int maxMerchantTradeAmount = 5)
    {
        ConfigureOpenSimulation(config, maxMerchantTradeAmount);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{label} criado em {SimulationFolder}.");
    }

    private static void ConfigureOpenSimulation(SimulationConfigData config, int maxMerchantTradeAmount = 5)
    {
        TesteSimulacao simulation = Object.FindFirstObjectByType<TesteSimulacao>();

        if (simulation == null)
        {
            Debug.Log("Nenhum TesteSimulacao encontrado na cena aberta. Os assets foram criados; atribua um SimulationConfigData ao componente quando quiser testar.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(simulation);
        SetInt(serializedObject, "daysToSimulate", 8);
        SetInt(serializedObject, "maxMerchantTradeAmount", Mathf.Max(1, maxMerchantTradeAmount));
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
        public ItemData madeira;
        public ItemData peixe;
        public ItemData couro;
        public ItemData ervas;
        public ItemData carvao;
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
        public NpcActionData serveSentence;
        public NpcActionData rest;
        public NpcActionData walk;
        public NpcActionData tavern;
        public NpcJobData merchantJob;
        public NpcJobData ironMerchantJob;
        public NpcJobData wineMerchantJob;
        public NpcJobData localMerchantJob;
        public NpcJobData guardJob;
        public NpcJobData networkAgricultureJob;
        public NpcJobData networkMiningJob;
        public NpcJobData networkMaritimeJob;
        public NpcJobData networkForestryJob;
        public NpcJobData networkManufacturingJob;
        public NpcJobData networkGeneralJob;
        public NpcJobData networkLocalJob;
        public NpcData jorge;
        public NpcData afonso;
        public NpcData helena;
        public NpcData pedro;
        public NpcData maria;
        public NpcData marcus;
        public NpcData arthur;
        public NpcData jobson;
        public NpcData carlos;
        public NpcData ana;
        public NpcData networkCampoTravelerOne;
        public NpcData networkCampoTravelerTwo;
        public NpcData networkSerraTravelerOne;
        public NpcData networkSerraTravelerTwo;
        public NpcData networkPortoTravelerOne;
        public NpcData networkPortoTravelerTwo;
        public NpcData networkBosqueTravelerOne;
        public NpcData networkBosqueTravelerTwo;
        public NpcData networkValeTravelerOne;
        public NpcData networkValeTravelerTwo;
        public NpcData networkFeiraTravelerOne;
        public NpcData networkFeiraTravelerTwo;
        public NpcData networkCampoLocal;
        public NpcData networkSerraLocal;
        public NpcData networkPortoLocal;
        public NpcData networkBosqueLocal;
        public NpcData networkValeLocal;
        public NpcData networkFeiraLocal;
        public CityData campoVerde;
        public CityData serraDeFerro;
        public CityData guardTestCity;
        public CityData networkCampoVerde;
        public CityData networkSerraDeFerro;
        public CityData portoAzul;
        public CityData bosqueAlto;
        public CityData valeDoCouro;
        public CityData feiraCentral;
    }
}
