using NUnit.Framework;

public sealed class DeterministicRandomCoreTests
{
    [Test]
    public void SameSeedContextAndDrawProduceTheSameValue()
    {
        DeterministicRandomSource first = new DeterministicRandomSource(12345);
        DeterministicRandomSource second = new DeterministicRandomSource(12345);

        Assert.That(second.NextUnit("operation|npc-a|day-4", 7L), Is.EqualTo(
            first.NextUnit("operation|npc-a|day-4", 7L)));
    }

    [Test]
    public void UnrelatedStreamConsumptionDoesNotShiftOperationStream()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(77);
        float expected = source.NextUnit("operation-a", 3L);

        source.NextUnit("diagnostic", 0L);
        source.NextUnit("diagnostic", 1L);
        source.NextUnit("other-operation", 0L);

        Assert.That(source.NextUnit("operation-a", 3L), Is.EqualTo(expected));
    }

    [Test]
    public void StreamStateMakesDrawPositionExplicitAndIndependent()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(9);
        DeterministicRandomStream first = source.CreateStream("operation");
        DeterministicRandomStream second = source.CreateStream("operation");

        float firstValue = first.NextUnit();
        float secondValue = second.NextUnit();

        Assert.That(secondValue, Is.EqualTo(firstValue));
        Assert.That(first.State.DrawIndex, Is.EqualTo(1L));
        Assert.That(second.State.DrawIndex, Is.EqualTo(1L));
    }

    [Test]
    public void ProviderAcceptsMaximumDrawIndexAndRejectsNegativeDrawIndex()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(2);

        Assert.That(() => source.NextUnit("boundary", long.MaxValue), Throws.Nothing);
        Assert.That(() => source.NextUnit("boundary", long.MinValue),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }
}
