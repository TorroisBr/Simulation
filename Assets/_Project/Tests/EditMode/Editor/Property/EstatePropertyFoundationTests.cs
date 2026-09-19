using NUnit.Framework;

public sealed class EstatePropertyFoundationTests
{
    [Test]
    public void PropertyOwnershipIsIndependentFromNpcRuntimeAndDeterministicallyOrdered()
    {
        PropertyOwnershipStore store = new PropertyOwnershipStore();
        PersonId owner = new PersonId("owner");
        PropertyOwnershipRecord zeta = new PropertyOwnershipRecord(new PropertyId("zeta"), owner);
        PropertyOwnershipRecord alpha = new PropertyOwnershipRecord(new PropertyId("alpha"), owner);

        Assert.That(store.TryRegister(zeta, out _), Is.True);
        Assert.That(store.TryRegister(alpha, out _), Is.True);
        Assert.That(store.OwnershipRecords[0], Is.SameAs(alpha));
        Assert.That(store.GetOwnedBy(owner), Has.Count.EqualTo(2));
    }

    [Test]
    public void DeadPersonKeepsPropertyTitleAndDoesNotOpenEstateAutomatically()
    {
        PersonRuntime dead = new PersonRuntime(new PersonId("dead"), 0L, 20L);
        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        EstateStore estates = new EstateStore();

        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("home"), dead.PersonId), out _), Is.True);
        Assert.That(properties.GetOwnedBy(dead.PersonId), Has.Count.EqualTo(1));
        Assert.That(estates.Count, Is.EqualTo(0));
    }

    [Test]
    public void EstateOpeningIsExplicitAndRequiresFactualDeath()
    {
        PersonStore people = new PersonStore();
        PersonRuntime living = new PersonRuntime(new PersonId("living"), 0L);
        EstateStore estates = new EstateStore();
        Assert.That(people.TryRegister(living, out _), Is.True);

        Assert.That(EstateCreationSystem.TryCreate(
            people,
            estates,
            living.PersonId,
            new EstateId("estate-living"),
            5L,
            out _,
            out EstateFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(EstateFoundationFailureCode.PersonNotDeceased));
        Assert.That(estates.Count, Is.EqualTo(0));
    }

    [Test]
    public void EstateOpeningPreservesPropertyOwnershipAndGuardsStaleTransitions()
    {
        PersonStore people = new PersonStore();
        PersonRuntime dead = new PersonRuntime(new PersonId("dead"), 0L, 20L);
        EstateStore estates = new EstateStore();
        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        PropertyOwnershipRecord property = new PropertyOwnershipRecord(
            new PropertyId("home"),
            dead.PersonId);
        Assert.That(people.TryRegister(dead, out _), Is.True);
        Assert.That(properties.TryRegister(property, out _), Is.True);

        Assert.That(EstateCreationSystem.TryPropose(
            people,
            estates,
            dead.PersonId,
            new EstateId("estate-dead"),
            21L,
            out EstateCreationTransition transition,
            out _), Is.True);
        Assert.That(EstateCreationSystem.TryCreate(
            people,
            estates,
            dead.PersonId,
            new EstateId("other-estate"),
            22L,
            out _,
            out _), Is.True);

        Assert.That(EstateCreationSystem.TryApply(
            people,
            estates,
            transition,
            21L,
            out EstateFoundationFailure stale), Is.False);
        Assert.That(stale.Code, Is.EqualTo(EstateFoundationFailureCode.StaleEstateStore));
        Assert.That(properties.GetOwnedBy(dead.PersonId), Is.EqualTo(new[] { property }));
    }

    [Test]
    public void PropertyTransferUsesExplicitOptimisticTransition()
    {
        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        PersonId first = new PersonId("first");
        EstateId estate = new EstateId("estate");
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("home"), first), out _), Is.True);
        Assert.That(properties.TryProposeTransfer(
            new PropertyId("home"),
            PropertyOwnerReference.ForEstate(estate),
            out PropertyOwnershipTransition transition,
            out _), Is.True);
        Assert.That(properties.TryApplyTransfer(transition, out _), Is.True);
        Assert.That(properties.GetOwnedBy(estate), Has.Count.EqualTo(1));
        Assert.That(properties.GetOwnedBy(first), Is.Empty);
    }
}
