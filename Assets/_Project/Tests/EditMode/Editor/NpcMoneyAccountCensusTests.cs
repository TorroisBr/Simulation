using System.Reflection;
using NUnit.Framework;

public sealed class NpcMoneyAccountCensusTests
{
    [Test]
    public void ProviderPublishesOneOrderedExactOwnerSectionPerNpc()
    {
        NpcRuntime zulu = new NpcRuntime("npc-zulu", null, null, 7f);
        NpcRuntime alpha = new NpcRuntime("npc-alpha", null, null, 3f);
        var providers = NpcMoneyAccountCensusProvider.CreateProviders(new[] { zulu, alpha });

        Assert.That(providers.Count, Is.EqualTo(2));
        OwnerSectionCensusWitness first = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness second = providers[1].GetCurrentCensus();
        Assert.That(first.SectionId, Is.EqualTo(NpcMoneyAccountCensusProvider.SectionPrefix + "npc-alpha"));
        Assert.That(second.SectionId, Is.EqualTo(NpcMoneyAccountCensusProvider.SectionPrefix + "npc-zulu"));
        Assert.That(first.SchemaVersion, Is.EqualTo(NpcMoneyAccountCensusProvider.SchemaVersion));
        Assert.That(first.OwnerInstanceIdentity, Is.SameAs(alpha.MoneyAccount));
        Assert.That(second.OwnerInstanceIdentity, Is.SameAs(zulu.MoneyAccount));
        Assert.That(first.Cardinality, Is.EqualTo(1));
        Assert.That(second.Cardinality, Is.EqualTo(1));
        Assert.That(first.Revision, Is.Zero);
        Assert.That(second.Revision, Is.Zero);
    }

    [Test]
    public void ProviderReadsRevisionWithoutChangingOwner()
    {
        NpcRuntime npc = new NpcRuntime("npc-account", null, null, 4f);
        IOwnerSectionCensusProvider provider = NpcMoneyAccountCensusProvider.CreateProviders(new[] { npc })[0];
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();
        OwnerSectionCensusWitness repeated = provider.GetCurrentCensus();

        Assert.That(repeated.OwnerInstanceIdentity, Is.SameAs(initial.OwnerInstanceIdentity));
        Assert.That(repeated.Cardinality, Is.EqualTo(initial.Cardinality));
        Assert.That(repeated.Revision, Is.EqualTo(initial.Revision));
        Assert.That(npc.MoneyAccount.Revision, Is.Zero);
    }

    [Test]
    public void SuccessfulCreditAdvancesOnlyTheOwnerRevision()
    {
        NpcRuntime npc = new NpcRuntime("npc-credit", null, null, 4f);
        IOwnerSectionCensusProvider provider = NpcMoneyAccountCensusProvider.CreateProviders(new[] { npc })[0];
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();

        Assert.That(npc.MoneyAccount.TryCredit(2f), Is.True);

        OwnerSectionCensusWitness after = provider.GetCurrentCensus();
        Assert.That(after.OwnerInstanceIdentity, Is.SameAs(initial.OwnerInstanceIdentity));
        Assert.That(after.Cardinality, Is.EqualTo(1));
        Assert.That(after.Revision, Is.EqualTo(initial.Revision + 1));
    }

    [Test]
    public void SuccessfulDebitAdvancesOnlyTheOwnerRevision()
    {
        NpcRuntime npc = new NpcRuntime("npc-debit", null, null, 4f);
        IOwnerSectionCensusProvider provider = NpcMoneyAccountCensusProvider.CreateProviders(new[] { npc })[0];
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();

        Assert.That(npc.MoneyAccount.TryDebit(1f), Is.True);

        OwnerSectionCensusWitness after = provider.GetCurrentCensus();
        Assert.That(after.OwnerInstanceIdentity, Is.SameAs(initial.OwnerInstanceIdentity));
        Assert.That(after.Cardinality, Is.EqualTo(1));
        Assert.That(after.Revision, Is.EqualTo(initial.Revision + 1));
    }

    [Test]
    public void ZeroAndFailedWritesLeaveFreshProtocolAssessmentValid()
    {
        NpcRuntime npc = new NpcRuntime("npc-noop", null, null, 4f);
        ContinuationCensusProtocol protocol = CreateBoundProtocol(npc);

        Assert.That(npc.MoneyAccount.TryCredit(0f), Is.True);
        Assert.That(npc.MoneyAccount.TryDebit(5f), Is.False);
        Assert.That(npc.MoneyAccount.TryCredit(-1f), Is.False);

        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
        Assert.That(protocol.TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.True,
            epochFailure.ToString());
        Assert.That(epoch, Is.Zero);
    }

    [Test]
    public void ProtocolDetectsUnnotifiedPerNpcAccountRevisionChange()
    {
        NpcRuntime npc = new NpcRuntime("npc-account-drift", null, null, 4f);
        ContinuationCensusProtocol protocol = CreateBoundProtocol(npc);

        Assert.That(npc.MoneyAccount.TryCredit(1f), Is.True);
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure changed), Is.False);
        Assert.That(changed, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure faulted), Is.False);
        Assert.That(faulted, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void RosterAddRemoveAndSameIdReRegistrationPublishOnlyCurrentAccountOwners()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null, economyEnabled: false);
        Assert.That(runtime.MoneyAccountCensusProviders, Is.Empty);

        NpcRuntime first = new NpcRuntime("account-roster-a", null);
        Assert.That(runtime.TryRegisterNpc(first, out _), Is.True);
        var afterFirst = runtime.MoneyAccountCensusProviders;
        Assert.That(afterFirst, Has.Count.EqualTo(1));
        Assert.That(afterFirst[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(first.MoneyAccount));

        NpcRuntime second = new NpcRuntime("account-roster-b", null);
        Assert.That(runtime.TryRegisterNpc(second, out _), Is.True);
        var afterAdd = runtime.MoneyAccountCensusProviders;
        Assert.That(afterAdd, Has.Count.EqualTo(2));
        Assert.That(afterAdd[0], Is.SameAs(afterFirst[0]));
        Assert.That(afterAdd[1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(second.MoneyAccount));

        object oldSecondOwner = afterAdd[1].GetCurrentCensus().OwnerInstanceIdentity;
        Assert.That(runtime.TryUnregisterNpc(second.RuntimeId, out _), Is.True);
        var afterRemove = runtime.MoneyAccountCensusProviders;
        Assert.That(afterRemove, Has.Count.EqualTo(1));
        Assert.That(afterRemove[0], Is.SameAs(afterFirst[0]));

        NpcRuntime replacement = new NpcRuntime(second.RuntimeId, null);
        Assert.That(runtime.TryRegisterNpc(replacement, out _), Is.True);
        var afterReRegistration = runtime.MoneyAccountCensusProviders;
        Assert.That(afterReRegistration, Has.Count.EqualTo(2));
        Assert.That(afterReRegistration[0], Is.SameAs(afterFirst[0]));
        Assert.That(afterReRegistration[1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(replacement.MoneyAccount));
        Assert.That(afterReRegistration[1].GetCurrentCensus().OwnerInstanceIdentity, Is.Not.SameAs(oldSecondOwner));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure),
            Is.True, epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(4));
    }

    [Test]
    public void ProviderRejectsDuplicateRuntimeIdsAndAliasedAccountOwners()
    {
        NpcRuntime duplicateA = new NpcRuntime("duplicate-account-id", null);
        NpcRuntime duplicateB = new NpcRuntime("duplicate-account-id", null);
        Assert.Throws<System.InvalidOperationException>(() =>
            NpcMoneyAccountCensusProvider.CreateProviders(new[] { duplicateA, duplicateB }));

        NpcRuntime first = new NpcRuntime("account-alias-a", null);
        NpcRuntime aliased = new NpcRuntime("account-alias-b", null);
        SetAccount(aliased, first.MoneyAccount);
        Assert.Throws<System.InvalidOperationException>(() =>
            NpcMoneyAccountCensusProvider.CreateProviders(new[] { first, aliased }));

        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterMoneyAccountRosterFamily(new[] { first, aliased }, out _), Is.False);
        Assert.That(protocol.MoneyAccountFamilyProviders, Is.Empty,
            "A failed owner-family registration must not publish a partial provider view.");
    }

    [Test]
    public void MissingAccountOwnerIsRejectedWithoutMaterializingReplacement()
    {
        NpcRuntime npc = new NpcRuntime("missing-account", null);
        SetAccount(npc, null);

        Assert.Throws<System.InvalidOperationException>(() =>
            NpcMoneyAccountCensusProvider.CreateProviders(new[] { npc }));
        Assert.That(typeof(NpcRuntime).GetField("moneyAccount", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(npc), Is.Null);
    }

    [Test]
    public void InPlaceAccountReplacementFailsClosedAndRetainsPublishedProviderView()
    {
        NpcRuntime original = new NpcRuntime("same-roster-account", null);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, new[] { original }, economyEnabled: false);
        var publishedBefore = runtime.MoneyAccountCensusProviders;
        IOwnerSectionCensusProvider priorProvider = publishedBefore[0];
        object priorOwner = priorProvider.GetCurrentCensus().OwnerInstanceIdentity;
        SetAccount(original, new MoneyAccountRuntime(9f));

        Assert.That(runtime.TryRegisterNpc(new NpcRuntime("unrelated-account-add", null), out _), Is.True);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(runtime.MoneyAccountCensusProviders, Is.SameAs(publishedBefore));
        Assert.That(runtime.MoneyAccountCensusProviders[0], Is.SameAs(priorProvider));
        Assert.That(runtime.MoneyAccountCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(priorOwner));
    }

    private static ContinuationCensusProtocol CreateBoundProtocol(NpcRuntime npc)
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        Assert.That(protocol.RegisterMoneyAccountRosterFamily(new[] { npc }, out ContinuationCensusFailure registration),
            Is.True, registration.ToString());
        Assert.That(protocol.SealExpectedSectionInventory(out ContinuationCensusFailure expected), Is.True,
            expected.ToString());
        Assert.That(protocol.SealCensusProviderInventory(out ContinuationCensusFailure providers), Is.True,
            providers.ToString());
        Assert.That(protocol.RegisterExpectedOperation("p12.test.owner-census", out ContinuationCensusFailure operation),
            Is.True, operation.ToString());
        Assert.That(protocol.SealOperationInventory(out ContinuationCensusFailure operations), Is.True,
            operations.ToString());
        Assert.That(protocol.BindOwnerThread(out ContinuationCensusFailure bind), Is.True, bind.ToString());
        Assert.That(protocol.TryAssessOwnerSectionInventory(out ContinuationCensusFailure baseline), Is.True,
            baseline.ToString());
        return protocol;
    }

    private static void SetAccount(NpcRuntime npc, MoneyAccountRuntime account)
    {
        typeof(NpcRuntime).GetField("moneyAccount", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc, account);
    }
}