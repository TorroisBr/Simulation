using NUnit.Framework;
using UnityEngine;

public sealed class CapabilityFoundationTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void CoreSupportsArbitraryCapabilityAttributes()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        CapabilityAttributeData magic = SimulationTestFactory.CreateCapabilityAttribute("magic");
        NpcData npcData = SimulationTestFactory.CreateNpc("arbitrary-capability");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 10f));
        npcData.capabilityValues.Add(new CapabilityAttributeValue(mental, 20f));
        npcData.capabilityValues.Add(new CapabilityAttributeValue(magic, 30f));

        Assert.That(CapabilityAuthoringValidator.ValidateDefinitions(new[] { physical, mental, magic }, out string diagnostic), Is.True, diagnostic);
        Assert.That(CapabilityAuthoringValidator.ValidateNpc(npcData, out diagnostic), Is.True, diagnostic);

        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });
        CapabilityEvaluationResult result = model.Evaluate(new NpcRuntime("npc-arbitrary", npcData));

        Assert.That(result.EffectiveCapability, Is.EqualTo(20f).Within(0.001f));
        Assert.That(result.Breakdown, Has.Count.EqualTo(2));
        Assert.That(result.Breakdown, Has.None.Property("AttributeDefinitionId").EqualTo("magic"));
    }

    [Test]
    public void GenericModelUsesConfiguredPhysicalAndMentalAttributes()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("body");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mind");
        NpcData npcData = SimulationTestFactory.CreateNpc("configured-model");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 40f));
        npcData.capabilityValues.Add(new CapabilityAttributeValue(mental, 10f));

        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental,
            physicalWeight = 1.5f,
            mentalWeight = 2f
        });

        CapabilityEvaluationResult result = model.Evaluate(new NpcRuntime("npc-configured", npcData));

        Assert.That(result.BaseCapability, Is.EqualTo(80f).Within(0.001f));
        Assert.That(result.EffectiveCapability, Is.EqualTo(80f).Within(0.001f));
    }

    [Test]
    public void TraitCanModifyGenericCapabilityWithoutCoreKnowingTraitMeaning()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        TraitData swordsman = SimulationTestFactory.CreateTrait("swordsman", "Swordsman");
        NpcData npcData = SimulationTestFactory.CreateNpc("trait-capability");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 10f));
        npcData.traits.Add(swordsman);

        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental,
            traitModifiers = new System.Collections.Generic.List<GenericTraitCapabilityModifier>
            {
                new GenericTraitCapabilityModifier(swordsman, physical, 25f)
            }
        });

        CapabilityEvaluationResult result = model.Evaluate(new NpcRuntime("npc-trait", npcData));

        Assert.That(result.EffectiveCapability, Is.EqualTo(35f).Within(0.001f));
        Assert.That(result.Breakdown, Has.Some.Property("Source").EqualTo(CapabilityContributionSource.Trait));
        Assert.That(result.Breakdown, Has.Some.Property("SourceId").EqualTo("swordsman"));
    }

    [Test]
    public void ItemCanModifyGenericCapabilityWithoutMultiplyingByInventoryStack()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        ItemData sword = SimulationTestFactory.CreateItem("sword");
        sword.capabilityModifiers.Add(new CapabilityAttributeModifier(physical, 30f));
        NpcData npcData = SimulationTestFactory.CreateNpc("item-capability");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 10f));

        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });
        NpcRuntime oneSword = new NpcRuntime("npc-one-sword", npcData);
        NpcRuntime tenSwords = new NpcRuntime("npc-ten-swords", npcData);
        oneSword.Inventory.AddItem(sword, 1);
        tenSwords.Inventory.AddItem(sword, 10);

        Assert.That(model.Evaluate(oneSword).EffectiveCapability, Is.EqualTo(40f).Within(0.001f));
        Assert.That(model.Evaluate(tenSwords).EffectiveCapability, Is.EqualTo(40f).Within(0.001f));
    }

    [Test]
    public void LegendaryItemBenefitsLowCapabilityNpc()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        ItemData legendarySword = SimulationTestFactory.CreateItem("legendary-sword");
        legendarySword.capabilityModifiers.Add(new CapabilityAttributeModifier(physical, 100f));
        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });
        NpcData weakData = SimulationTestFactory.CreateNpc("weak");
        weakData.capabilityValues.Add(new CapabilityAttributeValue(physical, 5f));
        NpcData strongData = SimulationTestFactory.CreateNpc("strong");
        strongData.capabilityValues.Add(new CapabilityAttributeValue(physical, 50f));
        NpcRuntime weak = new NpcRuntime("npc-weak", weakData);
        NpcRuntime strong = new NpcRuntime("npc-strong", strongData);

        float weakWithoutItem = model.Evaluate(weak).EffectiveCapability;
        float strongWithoutItem = model.Evaluate(strong).EffectiveCapability;
        weak.Inventory.AddItem(legendarySword, 1);
        strong.Inventory.AddItem(legendarySword, 1);

        Assert.That(model.Evaluate(weak).EffectiveCapability, Is.GreaterThan(weakWithoutItem));
        Assert.That(model.Evaluate(strong).EffectiveCapability, Is.GreaterThan(strongWithoutItem));
    }

    [Test]
    public void CapabilityEvaluationDoesNotMutateNpcOrInventory()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        ItemData sword = SimulationTestFactory.CreateItem("sword");
        sword.capabilityModifiers.Add(new CapabilityAttributeModifier(physical, 10f));
        NpcData npcData = SimulationTestFactory.CreateNpc("pure-evaluation");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 20f));
        NpcRuntime npc = new NpcRuntime("npc-pure", npcData);
        npc.Inventory.AddItem(sword, 4, 12f);
        int amountBefore = npc.Inventory.GetAmount(sword);
        float moneyBefore = npc.Money;
        int traitCountBefore = npc.NpcData.traits.Count;

        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });
        model.Evaluate(npc);

        Assert.That(npc.Inventory.GetAmount(sword), Is.EqualTo(amountBefore));
        Assert.That(npc.Inventory.GetAverageUnitCost(sword), Is.EqualTo(12f).Within(0.001f));
        Assert.That(npc.Money, Is.EqualTo(moneyBefore).Within(0.001f));
        Assert.That(npc.NpcData.traits.Count, Is.EqualTo(traitCountBefore));
        Assert.That(npc.NpcData.capabilityValues[0].Value, Is.EqualTo(20f).Within(0.001f));
    }

    [Test]
    public void AlternateCapabilityModelCanBeInjected()
    {
        NpcRuntime npc = new NpcRuntime("npc-alternate", SimulationTestFactory.CreateNpc("alternate"));
        FakeCapabilityModel fakeModel = new FakeCapabilityModel(777f);
        CapabilityEvaluationService service = new CapabilityEvaluationService(fakeModel);

        CapabilityEvaluationResult result = service.Evaluate(npc);

        Assert.That(result.EffectiveCapability, Is.EqualTo(777f).Within(0.001f));
        Assert.That(fakeModel.LastParticipant, Is.SameAs(npc));
    }

    [Test]
    public void DuplicateAndInvalidCapabilityAuthoringIsValidatedDeterministically()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData duplicatePhysical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        string duplicateDiagnostic;
        Assert.That(CapabilityAuthoringValidator.ValidateDefinitions(new[] { physical, duplicatePhysical }, out duplicateDiagnostic), Is.False);
        Assert.That(duplicateDiagnostic, Is.EqualTo("Capability definitions contain duplicate DefinitionId 'physical'."));

        NpcData invalidNpc = SimulationTestFactory.CreateNpc("invalid");
        invalidNpc.capabilityValues.Add(new CapabilityAttributeValue(physical, -1f));
        Assert.That(CapabilityAuthoringValidator.ValidateNpc(invalidNpc, out string invalidDiagnostic), Is.False);
        Assert.That(invalidDiagnostic, Does.Contain("finite and non-negative"));

        NpcData duplicateNpc = SimulationTestFactory.CreateNpc("duplicate-values");
        duplicateNpc.capabilityValues.Add(new CapabilityAttributeValue(physical, 1f));
        duplicateNpc.capabilityValues.Add(new CapabilityAttributeValue(duplicatePhysical, 2f));
        Assert.That(CapabilityAuthoringValidator.ValidateNpc(duplicateNpc, out string duplicateValueDiagnostic), Is.False);
        Assert.That(duplicateValueDiagnostic, Is.EqualTo("Capability values contain duplicate attribute DefinitionId 'physical'."));
    }

    private sealed class FakeCapabilityModel : ICapabilityModel
    {
        private readonly float value;

        public NpcRuntime LastParticipant { get; private set; }

        public FakeCapabilityModel(float value)
        {
            this.value = value;
        }

        public CapabilityEvaluationResult Evaluate(
            NpcRuntime participant,
            CapabilityEvaluationContext context = null)
        {
            LastParticipant = participant;
            return new CapabilityEvaluationResult(value, value, null);
        }
    }
}
