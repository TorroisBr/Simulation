using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SimulationBootstrapCompositionTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DisabledCrimeBootstrapAdvancesExistingHiddenTimerWithoutRegisteringCrimeProvider()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("disabled-crime-city");
        NpcData npcData = SimulationTestFactory.CreateNpc("disabled-crime-npc");
        NpcActionData hide = SimulationTestFactory.CreateAction(
            "disabled-crime-hide",
            NpcActionType.Hide,
            NpcActionCategory.Crime);
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("disabled-crime-free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("disabled-crime-wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("disabled-crime-arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("disabled-crime-hidden");

        npcData.acoesPadrao.Add(new NPCDefaultAction
        {
            action = hide,
            baseUtility = 100f
        });
        config.Cities.Add(city);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcData,
            startingCity = city
        });
        config.Actions.Add(hide);
        config.freeStatus = freeStatus;
        config.wantedStatus = wantedStatus;
        config.arrestedStatus = arrestedStatus;
        config.hiddenStatus = hiddenStatus;

        GameObject simulationObject = new GameObject("disabled-crime-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        FieldInfo configField = typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic);
        configField.SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.Runtime.Configuration.Crime.Enabled, Is.False);
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);

        FieldInfo crimeField = typeof(TesteSimulacao).GetField(
            "crimeSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo decisionField = typeof(TesteSimulacao).GetField(
            "npcDecisionSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        CrimeSystem composedCrime = crimeField.GetValue(simulation) as CrimeSystem;
        NpcDecisionSystem decisions = decisionField.GetValue(simulation) as NpcDecisionSystem;

        Assert.That(composedCrime, Is.Not.Null);
        Assert.That(decisions, Is.Not.Null);
        Assert.That(decisions.HasProvider<CrimeSystem>(), Is.False);

        npc.HideForDays(1);
        npc.AddStatus(hiddenStatus);
        simulation.Runtime.AdvanceDays(2);

        Assert.That(npc.IsHidden, Is.False);
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(0));
        Assert.That(npc.CurrentStatus.Contains(hiddenStatus), Is.False);
        Assert.That(simulation.Decisions.Decisions, Is.Empty);
    }
}
