using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1CommercialKnowledgeOwnerTests
{
    [Test]
    public void OwnerKeepsExactItemIdentityAndUsesExistingRevisionCallbacks()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreignDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1CommercialKnowledgeOwner owner = new DailyV1CommercialKnowledgeOwner(definitions);
        int admitted = 0;
        int committed = 0;
        owner.BindMutationBoundary(() => { admitted++; return true; }, () => committed++);

        DailyV1CommercialMarketObservation first = Market("city/a", item, 12f, 4, 1, 1,
            DailyV1CommercialKnowledgeSource.DirectObservation);
        Assert.That(owner.RecordObservation(first), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(owner.Observations, Has.Count.EqualTo(1));
        Assert.That(owner.Observations[0], Is.SameAs(first));

        DailyV1CommercialMarketObservation foreign = Market("city/a",
            foreignDefinitions.Items["FERRO"], 15f, 5, 2, 2,
            DailyV1CommercialKnowledgeSource.DirectObservation);
        Assert.That(owner.RecordObservation(foreign), Is.False,
            "A same-ID item node from another effective graph is not the selected owner identity.");
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(admitted, Is.EqualTo(1));
        Assert.That(committed, Is.EqualTo(1));
    }

    [Test]
    public void PreparedDirectBatchPreservesReplacementOrderAndAdvancesRevisionPerAppliedRow()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition firstItem = definitions.Items["FERRO"];
        DailyV1ItemDefinition secondItem = definitions.Items["TRIGO"];
        DailyV1CommercialKnowledgeOwner owner = new DailyV1CommercialKnowledgeOwner(definitions);
        Assert.That(owner.RecordObservation(Market("city/a", firstItem, 10f, 2, 1, 1,
            DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge)), Is.True);
        Assert.That(owner.RecordObservation(Market("city/a", secondItem, 8f, 3, 1, 1,
            DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge)), Is.True);

        List<string> events = new List<string>();
        owner.BindMutationBoundary(() => { events.Add("admit"); return true; }, () => events.Add("commit"));
        Assert.That(owner.TryPrepareDirectObservationBatch(new[]
        {
            Market("city/a", firstItem, 12f, 4, 2, 2, DailyV1CommercialKnowledgeSource.DirectObservation),
            Market("city/b", firstItem, 7f, 1, 2, 2, DailyV1CommercialKnowledgeSource.DirectObservation)
        }, new DailyV1CommercialLiquidityObservation("city/a", MarketLiquidityMode.Open,
            999f, 2, 2, DailyV1CommercialKnowledgeSource.DirectObservation),
            out DailyV1CommercialKnowledgeObservationInstall prepared), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(2), "Preparation leaves the live authority unchanged.");
        Assert.That(owner.CanInstall(prepared), Is.True);
        owner.InstallPrepared(prepared);

        Assert.That(owner.Revision, Is.EqualTo(5), "Each applied observation keeps its existing local revision semantics.");
        Assert.That(events, Is.EqualTo(new[] { "admit", "admit", "commit" }),
            "The prepared batch retains preflight and install admission plus one post-install callback.");
        Assert.That(owner.Observations[0].ItemDefinition, Is.SameAs(firstItem));
        Assert.That(owner.Observations[0].ObservedPrice, Is.EqualTo(12f));
        Assert.That(owner.Observations[1].ItemDefinition, Is.SameAs(secondItem));
        Assert.That(owner.Observations[2].LocationRuntimeId, Is.EqualTo("city/b"));
        Assert.That(owner.LiquidityObservations, Has.Count.EqualTo(1));
        Assert.That(owner.LiquidityObservations[0].ObservedPurchasingPower, Is.EqualTo(0f));
    }

    [Test]
    public void ShareReceiptReplayKeepsExistingFingerprintAndReceiptIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CommercialKnowledgeOwner owner = new DailyV1CommercialKnowledgeOwner(definitions);
        DailyV1CommercialMarketObservation source = Market("city/a", definitions.Items["FERRO"],
            13.5f, 7, 3, 4, DailyV1CommercialKnowledgeSource.DirectObservation);
        DailyV1CommercialKnowledgeShareValue value = DailyV1CommercialKnowledgeShareValue.Capture(source);
        string expectedValueFingerprint = StableKeyEncoding.Encode(
            "market", "city/a", "FERRO", "13.5", "7", "0", "0",
            "3", "4", "0", string.Empty);
        Assert.That(value.StableFingerprint, Is.EqualTo(expectedValueFingerprint),
            "The selected owner preserves the Unity/P18-D market-value fingerprint marker and fields.");
        DailyV1CommercialKnowledgeShareBatch batch = new DailyV1CommercialKnowledgeShareBatch(
            "boundary/1", "npc/sender", "person/sender", "npc/receiver", "person/receiver",
            "snapshot/source", 5, 2, new[] { value });
        string expectedOperationIdentity = StableKeyEncoding.Encode(
            "commercial-sharing-edge/v1", "boundary/1", "npc/sender", "npc/receiver");
        Assert.That(batch.OperationIdentity, Is.EqualTo(expectedOperationIdentity));
        Assert.That(batch.Fingerprint, Is.EqualTo(StableKeyEncoding.Encode(
            expectedOperationIdentity, "person/sender", "person/receiver", "snapshot/source",
            "5", "2", "1", expectedValueFingerprint)),
            "The selected Core batch retains the existing P18-D operation identity and fingerprint composition.");
        int committed = 0;
        owner.BindMutationBoundary(() => true, () => committed++);

        Assert.That(owner.TryPrepareShareBatch(batch,
            out DailyV1CommercialKnowledgeShareBatchCommit prepared), Is.True);
        DailyV1CommercialKnowledgeShareReceipt pendingReceipt = prepared.Receipt;
        Assert.That(owner.TryCommitShareBatch(prepared, out DailyV1CommercialKnowledgeShareReceipt first), Is.True);
        Assert.That(first, Is.SameAs(pendingReceipt));
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(owner.Observations[0].ItemDefinition, Is.SameAs(definitions.Items["FERRO"]));
        Assert.That(owner.Observations[0].Source, Is.EqualTo(DailyV1CommercialKnowledgeSource.SharedByNpc));
        Assert.That(owner.Observations[0].SourceRuntimeId, Is.EqualTo("npc/sender"));
        Assert.That(committed, Is.EqualTo(1));

        Assert.That(owner.TryPrepareShareBatch(batch,
            out DailyV1CommercialKnowledgeShareBatchCommit replay), Is.True);
        Assert.That(replay.IsReplay, Is.True);
        Assert.That(owner.TryCommitShareBatch(replay, out DailyV1CommercialKnowledgeShareReceipt replayed), Is.True);
        Assert.That(replayed, Is.SameAs(first));
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(committed, Is.EqualTo(1), "An idempotent replay does not rewrite owner state.");
    }

    [Test]
    public void SnapshotHydrationRetainsExactTypedRowsAndRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1CommercialKnowledgeOwner owner = new DailyV1CommercialKnowledgeOwner(definitions);
        Assert.That(owner.RecordObservation(Market("city/a", item, 6f, 2, 1, 2,
            DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge)), Is.True);
        Assert.That(owner.TryCopyOwnerSnapshot(out IReadOnlyList<DailyV1CommercialMarketObservation> markets,
            out IReadOnlyList<DailyV1CommercialLiquidityObservation> liquidity,
            out IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> receipts), Is.True);
        Assert.That(DailyV1CommercialKnowledgeOwner.TryCreateFromOwnerSnapshot(
            definitions, markets, liquidity, receipts, owner.Revision,
            out DailyV1CommercialKnowledgeOwner restored), Is.True);
        Assert.That(restored.Revision, Is.EqualTo(owner.Revision));
        Assert.That(restored.Observations, Has.Count.EqualTo(1));
        Assert.That(restored.Observations[0].ItemDefinition, Is.SameAs(item));
        Assert.That(restored.Observations[0].ObservedPrice, Is.EqualTo(6f));
    }

    private static DailyV1CommercialMarketObservation Market(string location,
        DailyV1ItemDefinition item, float price, int stock, long observedDay, long receivedDay,
        DailyV1CommercialKnowledgeSource source) => new DailyV1CommercialMarketObservation(
            location, item, price, stock, observedDay, receivedDay, source);
}
