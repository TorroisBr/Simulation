using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class DeterministicWorldCommandNaturalLanguageTranslatorTests
{
    private readonly List<Object> assets = new List<Object>();
    private DeterministicWorldCommandNaturalLanguageTranslator translator;
    private WorldCommandTranslationContext context;
    private NpcRuntime john;
    private NpcRuntime orc;
    private CityRuntime city;
    private ExplorableSiteRuntime site;
    private ItemData iron;
    private ItemData kingSword;
    private LocalPlaceRuntime depot;
    private LocalPlaceRuntime crypt;
    private PlaceContentStore contentStore;

    [SetUp]
    public void SetUp()
    {
        translator = new DeterministicWorldCommandNaturalLanguageTranslator();

        NpcData johnData = Create<NpcData>();
        johnData.id = "npc-john";
        johnData.name = "João";
        john = new NpcRuntime("npc-john-1", johnData);

        NpcData orcData = Create<NpcData>();
        orcData.id = "npc-orc";
        orcData.name = "Orc";
        orc = new NpcRuntime("npc-orc-1", orcData);

        iron = Create<ItemData>();
        iron.id = "iron";
        iron.itemName = "Ferro";

        kingSword = Create<ItemData>();
        kingSword.id = "king-sword";
        kingSword.itemName = "Espada do Rei";

        CityData cityData = Create<CityData>();
        cityData.id = "winterhold-definition";
        cityData.cityName = "Winterhold";
        city = new CityRuntime("city-winterhold", cityData, new SpatialLocationRuntime("winterhold-location"));

        ExplorableSiteData siteData = Create<ExplorableSiteData>();
        siteData.id = "black-cavern-definition";
        siteData.siteName = "Caverna Negra";
        site = new ExplorableSiteRuntime("site-black-cavern", siteData, new SpatialLocationRuntime("black-cavern-location"));

        ExplorableSiteStore siteStore = new ExplorableSiteStore();
        Assert.That(siteStore.Add(site), Is.True);

        RuntimeIdentityRegistry identityRegistry = new RuntimeIdentityRegistry();
        Assert.That(identityRegistry.RegisterLocation(site.Location), Is.True);
        Assert.That(identityRegistry.RegisterExplorableSite(site), Is.True);
        LocalTopologyStore topologyStore = new LocalTopologyStore(identityRegistry);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(site),
            identityRegistry);
        depot = new LocalPlaceRuntime("depot-place", "Depósito");
        crypt = new LocalPlaceRuntime("crypt-place", "Cripta");
        Assert.That(topology.AddPlace(depot, isEntryPoint: true), Is.True);
        Assert.That(topology.AddPlace(crypt), Is.True);
        Assert.That(topologyStore.Add(topology), Is.True);

        contentStore = new PlaceContentStore();
        Assert.That(contentStore.TryAddOpposition(
            site,
            new PlaceOppositionRuntime("opposition-1", "Guardiões"),
            out string oppositionDiagnostic), Is.True, oppositionDiagnostic);

        DeterministicWorldCommandEntityResolver entityResolver = new DeterministicWorldCommandEntityResolver(
            new[] { john, orc },
            new[] { city },
            new[] { city.Location, site.Location },
            siteStore,
            topologyStore,
            contentStore);
        DeterministicWorldCommandDefinitionLookup definitionLookup = new DeterministicWorldCommandDefinitionLookup(
            new[] { iron, kingSword });
        context = new WorldCommandTranslationContext(entityResolver, definitionLookup);
    }

    [TearDown]
    public void TearDown()
    {
        for (int index = 0; index < assets.Count; index++)
        {
            if (assets[index] != null)
            {
                Object.DestroyImmediate(assets[index]);
            }
        }

        assets.Clear();
    }

    [Test]
    public void PortugueseDeclareRelocationTranslatesToRelocateNpc()
    {
        NaturalLanguageTranslationResult result = Translate("coloque npc-john-1 em winterhold-location");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.RelocateNpc));
        Assert.That(result.Command.Authority, Is.EqualTo(WorldCommandAuthorityMode.Declare));
        RelocateNpcWorldCommandPayload payload = result.Command.Payload as RelocateNpcWorldCommandPayload;
        Assert.That(payload.NpcRuntimeId, Is.EqualTo(john.RuntimeId));
        Assert.That(payload.DestinationMacroLocationRuntimeId, Is.EqualTo(city.Location.RuntimeId));
    }

    [Test]
    public void TravelPhraseDoesNotBecomeDeclareRelocation()
    {
        NaturalLanguageTranslationResult result = Translate("João viaje para Winterhold");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Unsupported));
        Assert.That(result.Command, Is.Null);
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("NormalTravelUnsupported"));
    }

    [Test]
    public void ExactNpcRuntimeIdResolvesDeterministically()
    {
        IReadOnlyList<WorldCommandTranslationEntity> matches = context.EntityResolver.FindNpcs(john.RuntimeId);

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].RuntimeId, Is.EqualTo(john.RuntimeId));
    }

    [Test]
    public void AmbiguousNpcDisplayNameReturnsAmbiguous()
    {
        NpcData duplicateData = Create<NpcData>();
        duplicateData.id = "npc-john-duplicate";
        duplicateData.name = "João";
        NpcRuntime duplicate = new NpcRuntime("npc-john-2", duplicateData);
        WorldCommandTranslationContext duplicateContext = CreateContext(new[] { john, duplicate });

        NaturalLanguageTranslationResult result = Translate("coloque João em Winterhold", duplicateContext);

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous));
        Assert.That(result.Command, Is.Null);
        Assert.That(result.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void UnknownNpcReturnsMissingOrInvalid()
    {
        NaturalLanguageTranslationResult result = Translate("coloque npc-unknown em Winterhold");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.MissingInformation).Or.EqualTo(NaturalLanguageTranslationStatus.Invalid));
        Assert.That(result.Command, Is.Null);
    }

    [Test]
    public void DeclareCommonResourceProducesTypedPayload()
    {
        NaturalLanguageTranslationResult result = Translate("adicione 100 ferro no depósito");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.DeclareStackResource));
        DeclareStackResourceWorldCommandPayload payload = result.Command.Payload as DeclareStackResourceWorldCommandPayload;
        Assert.That(payload.ItemDefinitionId, Is.EqualTo(iron.DefinitionId));
        Assert.That(payload.Amount, Is.EqualTo(100));
        Assert.That(payload.Owner.OwnerKind, Is.EqualTo(PlaceContentOwnerKind.LocalPlace));
        Assert.That(payload.Owner.OwnerRuntimeId, Is.EqualTo(depot.RuntimeId));
    }

    [Test]
    public void MissingResourceAmountReturnsMissingInformation()
    {
        NaturalLanguageTranslationResult result = Translate("adicione ferro no depósito");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.MissingInformation));
        Assert.That(result.MissingFields, Does.Contain("amount"));
        Assert.That(result.Command, Is.Null);
    }

    [Test]
    public void AmbiguousItemDefinitionDoesNotChooseSilently()
    {
        ItemData duplicate = Create<ItemData>();
        duplicate.id = "iron-duplicate";
        duplicate.itemName = "Ferro";
        WorldCommandTranslationContext duplicateContext = CreateContext(
            new[] { john, orc },
            new[] { iron, duplicate });

        NaturalLanguageTranslationResult result = Translate("adicione 10 ferro no depósito", duplicateContext);

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous));
        Assert.That(result.Command, Is.Null);
        Assert.That(result.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void DeclareNotableItemProducesTypedPayload()
    {
        NaturalLanguageTranslationResult result = Translate("coloque a Espada do Rei na Caverna Negra");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.DeclareNotableItem));
        DeclareNotableItemWorldCommandPayload payload = result.Command.Payload as DeclareNotableItemWorldCommandPayload;
        Assert.That(payload.ItemDefinitionId, Is.EqualTo(kingSword.DefinitionId));
        Assert.That(payload.Owner.OwnerKind, Is.EqualTo(PlaceContentOwnerKind.ExplorableSite));
        Assert.That(payload.Owner.OwnerRuntimeId, Is.EqualTo(site.RuntimeId));
    }

    [Test]
    public void GrantSiteKnowledgeDoesNotCreateWorldTruth()
    {
        int placesBefore = contentStore.Places.Count;
        NaturalLanguageTranslationResult result = Translate("João sabe que existe a Caverna Negra");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.GrantSiteKnowledge));
        Assert.That(contentStore.Places, Has.Count.EqualTo(placesBefore));
    }

    [Test]
    public void RumorAboutItemProducesAdventureIntelNotItemCreation()
    {
        NaturalLanguageTranslationResult result = Translate("João ouviu dizer que há uma Espada do Rei na Caverna Negra");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.GrantAdventureIntel));
        GrantAdventureIntelWorldCommandPayload payload = result.Command.Payload as GrantAdventureIntelWorldCommandPayload;
        Assert.That(payload.IntelKind, Is.EqualTo(AdventureIntelDeclarationKind.NotableItem));
        Assert.That(payload.ItemDefinitionId, Is.EqualTo(kingSword.DefinitionId));
        Assert.That(contentStore.NotableItems, Is.Empty);
    }

    [Test]
    public void TruthAboutItemDoesNotBecomeKnowledgeCommand()
    {
        NaturalLanguageTranslationResult result = Translate("há uma Espada do Rei na Caverna Negra");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous), Diagnostics(result));
        Assert.That(result.Command, Is.Null);
    }

    [Test]
    public void ResolveConflictPhraseUsesRequest()
    {
        NaturalLanguageTranslationResult result = Translate("resolva o combate entre João e Orc");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.ResolveConflict));
        Assert.That(result.Command.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
    }

    [Test]
    public void DeclaredAttackUsesDeclare()
    {
        NaturalLanguageTranslationResult result = Translate("João atacou Orc");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.ResolveConflict));
        Assert.That(result.Command.Authority, Is.EqualTo(WorldCommandAuthorityMode.Declare));
    }

    [Test]
    public void DeclaredWinnerUsesForceOutcome()
    {
        NaturalLanguageTranslationResult result = Translate("João atacou Orc e venceu");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Authority, Is.EqualTo(WorldCommandAuthorityMode.ForceOutcome));
        ResolveConflictWorldCommandPayload payload = result.Command.Payload as ResolveConflictWorldCommandPayload;
        Assert.That(payload.ForcedWinningSideId, Is.EqualTo("attacker"));
        Assert.That(payload.ForcedOverallOutcome, Is.EqualTo(ConflictOutcomeType.Victory));
    }

    [Test]
    public void DeclaredParticipantDeathUsesExistingConstraint()
    {
        NaturalLanguageTranslationResult result = Translate("João venceu e Orc morreu");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        ResolveConflictWorldCommandPayload payload = result.Command.Payload as ResolveConflictWorldCommandPayload;
        Assert.That(payload.Constraints, Has.Count.EqualTo(1));
        Assert.That(payload.Constraints[0].ParticipantId, Is.EqualTo(orc.RuntimeId));
        Assert.That(payload.Constraints[0].ForceDeath, Is.True);
    }

    [Test]
    public void ConflictTranslationNeverRunsConflictResolver()
    {
        WorldCommandService service = new WorldCommandService();
        NaturalLanguageTranslationResult result = Translate("resolva o combate entre João e Orc");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved));
        Assert.That(service.RecordStore.Records, Is.Empty);
    }

    [Test]
    public void UnsupportedNormalTravelReturnsUnsupported()
    {
        NaturalLanguageTranslationResult result = Translate("faça João viajar até Winterhold");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Unsupported));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("NormalTravelUnsupported"));
    }

    [Test]
    public void UnsupportedNewEntityCreationReturnsUnsupported()
    {
        NaturalLanguageTranslationResult result = Translate("crie uma pessoa nova em Winterhold");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Unsupported));
        Assert.That(result.Command, Is.Null);
    }

    [Test]
    public void TranslationDoesNotInferHiddenOpposition()
    {
        NaturalLanguageTranslationResult result = Translate("João atacou Orc");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved));
        ResolveConflictWorldCommandPayload payload = result.Command.Payload as ResolveConflictWorldCommandPayload;
        Assert.That(payload.OppositionRuntimeId, Is.Null);
    }

    [Test]
    public void RuntimeIdResolutionTakesPrecedenceOverDisplayName()
    {
        NpcData duplicateData = Create<NpcData>();
        duplicateData.id = "npc-john-runtime-priority";
        duplicateData.name = "João";
        NpcRuntime duplicate = new NpcRuntime("npc-john-2", duplicateData);
        WorldCommandTranslationContext duplicateContext = CreateContext(new[] { john, duplicate });

        NaturalLanguageTranslationResult result = Translate("coloque npc-john-2 em winterhold-location", duplicateContext);

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        RelocateNpcWorldCommandPayload payload = result.Command.Payload as RelocateNpcWorldCommandPayload;
        Assert.That(payload.NpcRuntimeId, Is.EqualTo("npc-john-2"));
    }

    [Test]
    public void TwoNpcsWithSameDisplayNameProduceCandidates()
    {
        NpcData duplicateData = Create<NpcData>();
        duplicateData.id = "npc-john-duplicate";
        duplicateData.name = "João";
        NpcRuntime duplicate = new NpcRuntime("npc-john-2", duplicateData);
        NaturalLanguageTranslationResult result = Translate(
            "coloque João em Winterhold",
            CreateContext(new[] { john, duplicate }));

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous), Diagnostics(result));
        Assert.That(result.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void TwoLocationsWithSameDisplayNameProduceCandidates()
    {
        CityData secondData = Create<CityData>();
        secondData.id = "winterhold-south-definition";
        secondData.cityName = "Winterhold";
        CityRuntime secondCity = new CityRuntime(
            "city-winterhold-south",
            secondData,
            new SpatialLocationRuntime("winterhold-south-location"));
        WorldCommandTranslationResultAndContext pair = CreateContextWithCities(secondCity);

        NaturalLanguageTranslationResult result = Translate("coloque npc-john-1 em Winterhold", pair.Context);

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous));
        Assert.That(result.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void TwoItemDefinitionsWithSameDisplayNameProduceCandidates()
    {
        ItemData duplicate = Create<ItemData>();
        duplicate.id = "king-sword-duplicate";
        duplicate.itemName = "Espada do Rei";
        NaturalLanguageTranslationResult result = Translate(
            "coloque a Espada do Rei na Caverna Negra",
            CreateContext(new[] { john, orc }, new[] { iron, kingSword, duplicate }));

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous));
        Assert.That(result.Candidates, Has.Count.EqualTo(2));
    }

    [Test]
    public void AmbiguousResultContainsStableCandidateOrdering()
    {
        NpcData duplicateData = Create<NpcData>();
        duplicateData.id = "npc-john-duplicate";
        duplicateData.name = "João";
        NpcRuntime duplicate = new NpcRuntime("npc-john-0", duplicateData);
        NaturalLanguageTranslationResult result = Translate(
            "coloque João em Winterhold",
            CreateContext(new[] { john, duplicate }));

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Ambiguous));
        Assert.That(result.Candidates[0].CandidateId, Is.EqualTo("npc-john-0"));
        Assert.That(result.Candidates[1].CandidateId, Is.EqualTo("npc-john-1"));
    }

    [Test]
    public void DeterministicTranslationReturnsSameResultForSameContext()
    {
        NaturalLanguageTranslationResult first = Translate("coloque npc-john-1 em winterhold-location");
        NaturalLanguageTranslationResult second = Translate("coloque npc-john-1 em winterhold-location");

        Assert.That(first.Status, Is.EqualTo(second.Status));
        Assert.That(first.Command.Kind, Is.EqualTo(second.Command.Kind));
        Assert.That(first.Command.Origin, Is.EqualTo(second.Command.Origin));
        Assert.That(first.Command.Authority, Is.EqualTo(second.Command.Authority));
        Assert.That((first.Command.Payload as RelocateNpcWorldCommandPayload).NpcRuntimeId,
            Is.EqualTo((second.Command.Payload as RelocateNpcWorldCommandPayload).NpcRuntimeId));
    }

    [Test]
    public void AddLocalPlaceProducesTypedPayloadWithoutAllocatingIdentity()
    {
        NaturalLanguageTranslationResult result = Translate("adicione um lugar Sala do Trono em Caverna Negra");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.AddLocalPlace));
        AddLocalPlaceWorldCommandPayload payload = result.Command.Payload as AddLocalPlaceWorldCommandPayload;
        Assert.That(payload.TopologyOwnerRuntimeId, Is.EqualTo(site.RuntimeId));
        Assert.That(payload.DisplayName, Is.EqualTo("Sala do Trono"));
    }

    [Test]
    public void AddLocalConnectionRequiresExplicitTraversalCost()
    {
        NaturalLanguageTranslationResult result = Translate("adicione uma conexão entre Depósito e Cripta em Caverna Negra");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.MissingInformation));
        Assert.That(result.MissingFields, Does.Contain("traversal cost"));
    }

    [Test]
    public void AddLocalConnectionProducesTypedPayload()
    {
        NaturalLanguageTranslationResult result = Translate("adicione uma conexão entre Depósito e Cripta em Caverna Negra com custo 2");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.AddLocalConnection));
        AddLocalConnectionWorldCommandPayload payload = result.Command.Payload as AddLocalConnectionWorldCommandPayload;
        Assert.That(payload.TopologyOwnerRuntimeId, Is.EqualTo(site.RuntimeId));
        Assert.That(payload.OriginLocalPlaceRuntimeId, Is.EqualTo(depot.RuntimeId));
        Assert.That(payload.DestinationLocalPlaceRuntimeId, Is.EqualTo(crypt.RuntimeId));
        Assert.That(payload.TraversalCost, Is.EqualTo(2f));
    }

    [Test]
    public void ExplicitOppositionReferenceProducesPlaceOppositionCommand()
    {
        NaturalLanguageTranslationResult result = Translate("resolva a oposição opposition-1 entre João e Orc");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Kind, Is.EqualTo(WorldCommandKind.PlaceOpposition));
        ResolveConflictWorldCommandPayload payload = result.Command.Payload as ResolveConflictWorldCommandPayload;
        Assert.That(payload.OppositionRuntimeId, Is.EqualTo("opposition-1"));
    }

    [Test]
    public void SuggestPrefixProducesPreviewOnlyAuthority()
    {
        NaturalLanguageTranslationResult result = Translate("sugira coloque npc-john-1 em winterhold-location");

        Assert.That(result.Status, Is.EqualTo(NaturalLanguageTranslationStatus.Resolved), Diagnostics(result));
        Assert.That(result.Command.Authority, Is.EqualTo(WorldCommandAuthorityMode.Suggest));
    }

    private NaturalLanguageTranslationResult Translate(string text, WorldCommandTranslationContext translationContext = null)
    {
        return translator.Translate(
            new NaturalLanguageWorldCommandRequest(text, WorldCommandOrigin.GM),
            translationContext ?? context);
    }

    private WorldCommandTranslationContext CreateContext(
        IEnumerable<NpcRuntime> npcs,
        IEnumerable<ItemData> items = null)
    {
        DeterministicWorldCommandEntityResolver entityResolver = new DeterministicWorldCommandEntityResolver(
            npcs,
            new[] { city },
            new[] { city.Location, site.Location },
            new ExplorableSiteStoreWithSite(site).Store,
            null,
            contentStore);
        DeterministicWorldCommandDefinitionLookup definitions = new DeterministicWorldCommandDefinitionLookup(
            items ?? new[] { iron, kingSword });
        return new WorldCommandTranslationContext(entityResolver, definitions);
    }

    private WorldCommandTranslationResultAndContext CreateContextWithCities(CityRuntime secondCity)
    {
        DeterministicWorldCommandEntityResolver entityResolver = new DeterministicWorldCommandEntityResolver(
            new[] { john, orc },
            new[] { city, secondCity },
            new[] { city.Location, secondCity.Location, site.Location },
            new ExplorableSiteStoreWithSite(site).Store,
            null,
            contentStore);
        DeterministicWorldCommandDefinitionLookup definitions = new DeterministicWorldCommandDefinitionLookup(new[] { iron, kingSword });
        return new WorldCommandTranslationResultAndContext(new WorldCommandTranslationContext(entityResolver, definitions));
    }

    private T Create<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        assets.Add(asset);
        return asset;
    }

    private static string Diagnostics(NaturalLanguageTranslationResult result)
    {
        if (result == null || result.Diagnostics == null || result.Diagnostics.Count == 0)
        {
            return "no diagnostics";
        }

        return result.Diagnostics[0].Code + ": " + result.Diagnostics[0].Message;
    }

    private sealed class ExplorableSiteStoreWithSite
    {
        public ExplorableSiteStore Store { get; }

        public ExplorableSiteStoreWithSite(ExplorableSiteRuntime site)
        {
            Store = new ExplorableSiteStore();
            Store.Add(site);
        }
    }

    private sealed class WorldCommandTranslationResultAndContext
    {
        public WorldCommandTranslationContext Context { get; }

        public WorldCommandTranslationResultAndContext(WorldCommandTranslationContext context)
        {
            Context = context;
        }
    }
}
