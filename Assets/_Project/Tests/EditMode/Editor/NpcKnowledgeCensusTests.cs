using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class NpcKnowledgeCensusTests
{
    [Test]
    public void ProviderPublishesTenOrdinalSectionsPerNpcBoundToExactOwners()
    {
        NpcRuntime z = new NpcRuntime("z-knowledge", null);
        NpcRuntime a = new NpcRuntime("a-knowledge", null);
        var providers = NpcKnowledgeCensusProvider.CreateProviders(new[] { z, a });
        Assert.That(providers, Has.Count.EqualTo(20));
        string[] ids = providers.Select(p => p.GetCurrentCensus().SectionId).ToArray();
        Assert.That(ids[0], Does.EndWith("/a-knowledge"));
        Assert.That(ids[10], Does.EndWith("/z-knowledge"));
        Assert.That(ids.Count(x => x.EndsWith("/a-knowledge", StringComparison.Ordinal)), Is.EqualTo(10));
        Assert.That(ids.Count(x => x.EndsWith("/z-knowledge", StringComparison.Ordinal)), Is.EqualTo(10));
        foreach (var group in providers.GroupBy(p => p.GetCurrentCensus().SectionId.Substring(p.GetCurrentCensus().SectionId.LastIndexOf('/') + 1)))
        {
            Assert.That(group.Select(p => p.GetCurrentCensus().SectionId).Distinct().Count(), Is.EqualTo(10));
            var witnesses = group.Select(p => p.GetCurrentCensus()).ToArray();
            Assert.That(witnesses.Select(w => w.OwnerInstanceIdentity).Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(4));
            Assert.That(witnesses.All(w => w.SchemaVersion == 1 && w.Cardinality == 0 && w.Revision == 0), Is.True);
        }
    }

    [Test]
    public void MissingOwnerAndNullCommercialBackingCollectionsFailWithoutMaterialization()
    {
        foreach (string ownerFieldName in new[] { "explorableSiteKnowledge", "localTopologyKnowledge", "adventureSiteIntelKnowledge", "commercialKnowledge" })
        {
            NpcRuntime missingOwnerNpc = new NpcRuntime("missing-" + ownerFieldName, null);
            FieldInfo ownerField = typeof(NpcRuntime).GetField(ownerFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            ownerField.SetValue(missingOwnerNpc, null);
            Assert.Throws<InvalidOperationException>(() => NpcKnowledgeCensusProvider.CreateProviders(new[] { missingOwnerNpc }));
            Assert.That(ownerField.GetValue(missingOwnerNpc), Is.Null);
        }

        NpcRuntime npc = new NpcRuntime("missing-commercial", null);
        FieldInfo commercialField = typeof(NpcRuntime).GetField("commercialKnowledge", BindingFlags.Instance | BindingFlags.NonPublic);
        commercialField.SetValue(npc, null);
        Assert.Throws<InvalidOperationException>(() => NpcKnowledgeCensusProvider.CreateProviders(new[] { npc }));
        Assert.That(commercialField.GetValue(npc), Is.Null);

        foreach (string fieldName in new[] { "observations", "liquidityObservations", "shareReceipts" })
        {
            CommercialKnowledgeRuntime owner = new CommercialKnowledgeRuntime();
            FieldInfo selected = typeof(CommercialKnowledgeRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            selected.SetValue(owner, null);
            FieldInfo markets = typeof(CommercialKnowledgeRuntime).GetField("observations", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo liquidity = typeof(CommercialKnowledgeRuntime).GetField("liquidityObservations", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo receipts = typeof(CommercialKnowledgeRuntime).GetField("shareReceipts", BindingFlags.Instance | BindingFlags.NonPublic);
            object marketsBefore = markets.GetValue(owner);
            object liquidityBefore = liquidity.GetValue(owner);
            object receiptsBefore = receipts.GetValue(owner);
            FieldInfo revision = typeof(CommercialKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic);
            revision.SetValue(owner, 7L);
            MethodInfo read = typeof(CommercialKnowledgeRuntime).GetMethod("TryReadCensus", BindingFlags.Instance | BindingFlags.NonPublic);
            object[] args = { 0, 0, 0, 0L };
            Assert.That(read.Invoke(owner, args), Is.EqualTo(false));
            Assert.That(markets.GetValue(owner), Is.SameAs(marketsBefore));
            Assert.That(liquidity.GetValue(owner), Is.SameAs(liquidityBefore));
            Assert.That(receipts.GetValue(owner), Is.SameAs(receiptsBefore));
            Assert.That(revision.GetValue(owner), Is.EqualTo(7L));
        }
    }

    [Test]
    public void OwnerRevisionsAdvanceOnlyForSuccessfulKnowledgeChangesAndSaturateClosed()
    {
        ExplorableSiteKnowledgeRuntime explorable = new ExplorableSiteKnowledgeRuntime("owner");
        var first = new ExplorableSiteKnowledgeObservation("site", "loc", 1, 1, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        var replacement = new ExplorableSiteKnowledgeObservation("site", "loc", 2, 2, ExplorableSiteKnowledgeSource.DirectObservation);
        Assert.That(explorable.RecordObservation(first), Is.True);
        Assert.That(explorable.Revision, Is.EqualTo(1));
        Assert.That(explorable.RecordObservation(first), Is.False);
        Assert.That(explorable.RecordObservation(replacement), Is.True);
        Assert.That(explorable.Revision, Is.EqualTo(2));
        typeof(ExplorableSiteKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(explorable, long.MaxValue);
        Assert.That(explorable.RecordObservation(new ExplorableSiteKnowledgeObservation("other", "loc", 1, 1, ExplorableSiteKnowledgeSource.DirectObservation)), Is.False);
        Assert.That(explorable.Observations, Has.Count.EqualTo(1));
        Assert.That(explorable.Revision, Is.EqualTo(long.MaxValue));

        LocalTopologyKnowledgeRuntime local = new LocalTopologyKnowledgeRuntime("owner");
        Assert.That(local.RecordPlaceObservation(new LocalPlaceKnowledgeObservation("site", "place", null, null, "A", false, 1, 1, LocalTopologyKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(local.Revision, Is.EqualTo(1));
        typeof(LocalTopologyKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(local, long.MaxValue);
        Assert.That(local.RecordPlaceObservation(new LocalPlaceKnowledgeObservation("site", "other-place", null, null, "B", false, 1, 1, LocalTopologyKnowledgeSource.DirectObservation)), Is.False);
        Assert.That(local.PlaceObservations, Has.Count.EqualTo(1));

        AdventureSiteIntelKnowledgeRuntime adventure = new AdventureSiteIntelKnowledgeRuntime("owner");
        Assert.That(adventure.RecordObservation(new AdventureOppositionObservation("site", null, "opposition", AdventureOppositionObservedState.Active, 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.Revision, Is.EqualTo(1));
        Assert.That(adventure.RecordObservation(new AdventureNotableItemObservation("site", null, "item", "definition", 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.RecordObservation(new AdventureCommonResourceObservation("site", null, "resource", 1, null, 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.RecordObservation(new AdventureAccessObservation("site", null, default(PlaceAccessState), 1, 1, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(adventure.Revision, Is.EqualTo(4));
        typeof(AdventureSiteIntelKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(adventure, long.MaxValue);
        Assert.That(adventure.RecordObservation(new AdventureOppositionObservation("site", null, "another-opposition", AdventureOppositionObservedState.Active, 1, 1, AdventureIntelSource.DirectObservation)), Is.False);
        Assert.That(adventure.OppositionObservations, Has.Count.EqualTo(1));
    }

    [Test]
    public void ProtocolRegistersKnowledgeFamilyAndDetectsUnnotifiedWrite()
    {
        NpcRuntime npc = new NpcRuntime("protocol-knowledge", null);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(new[] { npc }, out ContinuationCensusFailure registerFailure), Is.True, registerFailure.ToString());
        Assert.That(protocol.NpcKnowledgeFamilyProviders, Has.Count.EqualTo(10));
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure baseline), Is.True, baseline.ToString());
        npc.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation("site", "loc", 1, 1, ExplorableSiteKnowledgeSource.DirectObservation));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure drift), Is.False);
        Assert.That(drift, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void ExactZeroKnowledgeSectionsRemainReadableAtRevisionSaturation()
    {
        NpcRuntime npc = new NpcRuntime("saturated-knowledge", null);
        typeof(ExplorableSiteKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.ExistingExplorableSiteKnowledge, long.MaxValue);
        typeof(LocalTopologyKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.ExistingLocalTopologyKnowledge, long.MaxValue);
        typeof(AdventureSiteIntelKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.ExistingAdventureSiteIntelKnowledge, long.MaxValue);
        typeof(CommercialKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.ExistingCommercialKnowledge, long.MaxValue);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterNpcKnowledgeRosterFamily(new[] { npc }, out ContinuationCensusFailure registration), Is.True, registration.ToString());
        Assert.That(protocol.SealExpectedSectionInventory(out _), Is.True);
        Assert.That(protocol.SealCensusProviderInventory(out _), Is.True);
        Assert.That(protocol.RegisterExpectedOperation("runtime.advance", out _), Is.True);
        Assert.That(protocol.SealOperationInventory(out _), Is.True);
        Assert.That(protocol.BindOwnerThread(out _), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure assessment), Is.True, assessment.ToString());
        Assert.That(protocol.NpcKnowledgeFamilyProviders.All(p => p.GetCurrentCensus().Cardinality == 0), Is.True);
    }
}

internal sealed class ReferenceEqualityComparer : System.Collections.Generic.IEqualityComparer<object>
{
    public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
    public new bool Equals(object x, object y) => ReferenceEquals(x, y);
    public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
