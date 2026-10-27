using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1RuntimeProtocolOperationContractsTests
{
    [TestCase(false, false, false, 1)]
    [TestCase(false, true, false, 1)]
    [TestCase(false, false, true, 1)]
    [TestCase(false, true, true, 1)]
    [TestCase(true, false, false, 21)]
    [TestCase(true, true, false, 23)]
    [TestCase(true, false, true, 22)]
    [TestCase(true, true, true, 24)]
    public void ExpectedOperationCatalogueHasStableOptionalCardinality(
        bool hasRuntimeAdmission,
        bool hasTravelPartySystem,
        bool hasTravelSystem,
        int expectedCount)
    {
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission,
                hasTravelPartySystem,
                hasTravelSystem);

        Assert.That(operationContractIds, Has.Count.EqualTo(expectedCount));
        var uniqueOperationContractIds = new HashSet<string>(operationContractIds);
        Assert.That(uniqueOperationContractIds, Has.Count.EqualTo(expectedCount),
            "Every enabled protocol operation must have one unique stable identity.");
    }

    [Test]
    public void SelectedRuntimeCataloguePreservesCanonicalIdentityAndRegistrationOrder()
    {
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: true);

        CollectionAssert.AreEqual(new[]
        {
            "runtime.npc-membership",
            "runtime.bootstrap-publication",
            "runtime.advance-day",
            "runtime.person.parentage",
            "runtime.travel-party.start",
            "runtime.travel-party.advance",
            "runtime.travel.start",
            "runtime.economy.npc-trade",
            "runtime.economy.money-transfer",
            "runtime.economy.market-purchase",
            "runtime.economy.market-sale",
            "runtime.merchant.advance-npc-trade-state",
            "p12.institution-office.owner-commit",
            "p12.faction.owner-commit",
            "p12.political-claim.owner-commit",
            "p12.political-support.owner-commit",
            "p12.property.owner-commit",
            "p12.estate.owner-commit",
            "runtime.population.immigration",
            "runtime.population.emigration",
            "runtime.population.resident-death",
            "runtime.population.residence-migration",
            "runtime.person.death",
            "runtime.person.residence-bind"
        }, operationContractIds);
    }

    [Test]
    public void RuntimeWithoutAdmissionKeepsOnlyNpcMembershipProtocol()
    {
        IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: false,
                hasTravelPartySystem: true,
                hasTravelSystem: true);

        CollectionAssert.AreEqual(
            new[] { "runtime.npc-membership" }, operationContractIds);
    }
}
