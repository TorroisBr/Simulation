using System;
using NUnit.Framework;

public sealed class PropertyTransferFoundationTests
{
    [Test]
    public void TransferIsExplicitDeterministicAndPreservesHistory()
    {
        PersonStore people = new PersonStore();
        PersonRuntime formerOwner = new PersonRuntime(new PersonId("former-owner"), 0L, 5L);
        PersonRuntime successor = new PersonRuntime(new PersonId("successor"), 0L);
        Assert.That(people.TryRegister(formerOwner, out _), Is.True);
        Assert.That(people.TryRegister(successor, out _), Is.True);

        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("home"), formerOwner.PersonId),
            out _), Is.True);

        Assert.That(PropertyTransferSystem.TryTransfer(
            people,
            properties,
            new PropertyId("home"),
            successor.PersonId,
            6L,
            out PropertyOwnershipRecord ownership,
            out PropertyTransferFailure failure), Is.True, failure.ToString());
        Assert.That(ownership.OwnerPersonId, Is.EqualTo(successor.PersonId));
        Assert.That(properties.Revision, Is.EqualTo(2L));
        Assert.That(properties.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(properties.TransferHistory[0].PreviousOwnerPersonId, Is.EqualTo(formerOwner.PersonId));
        Assert.That(properties.TransferHistory[0].NewOwnerPersonId, Is.EqualTo(successor.PersonId));
        Assert.That(properties.TransferHistory[0].TransferAbsoluteDay, Is.EqualTo(6L));
    }

    [Test]
    public void StaleTransferCannotOverwriteAChangedProperty()
    {
        PersonStore people = new PersonStore();
        PersonRuntime owner = Register(people, "owner");
        PersonRuntime first = Register(people, "first");
        PersonRuntime second = Register(people, "second");
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("estate-home"), owner.PersonId),
            out _), Is.True);

        Assert.That(PropertyTransferSystem.TryProposeTransfer(
            people,
            properties,
            new PropertyId("estate-home"),
            first.PersonId,
            1L,
            out PropertyOwnershipTransferTransition stale,
            out _), Is.True);
        Assert.That(PropertyTransferSystem.TryTransfer(
            people,
            properties,
            new PropertyId("estate-home"),
            second.PersonId,
            1L,
            out _,
            out _), Is.True);

        Assert.That(PropertyTransferSystem.TryApplyTransfer(
            people,
            properties,
            stale,
            out PropertyTransferFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PropertyTransferFailureCode.StalePropertyStore));
        Assert.That(properties.TryGet(new PropertyId("estate-home"), out PropertyOwnershipRecord preserved), Is.True);
        Assert.That(preserved.OwnerPersonId, Is.EqualTo(second.PersonId));
    }

    [Test]
    public void TransferRequiresRegisteredLivingNewOwnerAndMatchingWorld()
    {
        PersonStore people = new PersonStore();
        PersonRuntime owner = Register(people, "world-owner");
        PersonRuntime dead = new PersonRuntime(new PersonId("dead-successor"), 0L, 2L);
        Assert.That(people.TryRegister(dead, out _), Is.True);
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("world-property"), owner.PersonId),
            out _), Is.True);

        Assert.That(PropertyTransferSystem.TryProposeTransfer(
            people,
            properties,
            new PropertyId("world-property"),
            dead.PersonId,
            3L,
            out _,
            out PropertyTransferFailure deadFailure), Is.False);
        Assert.That(deadFailure.Code, Is.EqualTo(PropertyTransferFailureCode.NewOwnerNotLiving));

        PersonStore otherPeople = new PersonStore();
        Assert.That(PropertyTransferSystem.TryProposeTransfer(
            otherPeople,
            properties,
            new PropertyId("world-property"),
            owner.PersonId,
            1L,
            out _,
            out PropertyTransferFailure worldFailure), Is.False);
        Assert.That(worldFailure.Code, Is.EqualTo(PropertyTransferFailureCode.InvalidStore));
    }

    [Test]
    public void TransferDoesNotRequireNpcMaterialization()
    {
        PersonStore people = new PersonStore();
        PersonRuntime owner = Register(people, "dormant-owner");
        PersonRuntime successor = Register(people, "dormant-successor");
        PropertyOwnershipStore properties = new PropertyOwnershipStore(people);
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("dormant-property"), owner.PersonId),
            out _), Is.True);

        Assert.That(PropertyTransferSystem.TryTransfer(
            people,
            properties,
            new PropertyId("dormant-property"),
            successor.PersonId,
            0L,
            out _,
            out PropertyTransferFailure failure), Is.True, failure.ToString());
        Assert.That(owner.IsMaterialized, Is.False);
        Assert.That(successor.IsMaterialized, Is.False);
    }

    private static PersonRuntime Register(PersonStore people, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), 0L);
        Assert.That(people.TryRegister(person, out _), Is.True);
        return person;
    }
}
