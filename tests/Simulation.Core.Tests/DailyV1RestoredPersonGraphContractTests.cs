using NUnit.Framework;

public sealed class DailyV1RestoredPersonGraphContractTests
{
    [Test]
    public void AcceptsCompleteCorePersonNpcGenealogyGraph()
    {
        CreateGraph(out PersonStore persons, out GenealogyStore genealogy,
            out PersonId parent, out PersonId child);
        Assert.That(persons.TryBindMaterializedNpc(parent, "npc-parent", out _), Is.True);
        Assert.That(genealogy.TryAddParentage(parent, child, out _), Is.True);

        Assert.That(DailyV1RestoredPersonGraphContract.TryValidate(
            persons, genealogy, new[] { "npc-parent", "npc-child" }, out string diagnostic), Is.True);
        Assert.That(diagnostic, Is.Null);
    }

    [Test]
    public void RejectsPersonMaterializedToUnselectedNpc()
    {
        CreateGraph(out PersonStore persons, out GenealogyStore genealogy,
            out PersonId parent, out PersonId child);
        Assert.That(persons.TryBindMaterializedNpc(parent, "npc-parent", out _), Is.True);
        Assert.That(genealogy.TryAddParentage(parent, child, out _), Is.True);

        Assert.That(DailyV1RestoredPersonGraphContract.TryValidate(
            persons, genealogy, new[] { "other-npc" }, out string diagnostic), Is.False);
        Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void RejectsGenealogyEndpointOutsidePersonStore()
    {
        CreateGraph(out PersonStore persons, out GenealogyStore genealogy,
            out PersonId parent, out _);
        Assert.That(genealogy.TryAddParentage(parent, new PersonId("missing-child"), out _), Is.True);

        Assert.That(DailyV1RestoredPersonGraphContract.TryValidate(
            persons, genealogy, new[] { "npc-parent" }, out string diagnostic), Is.False);
        Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void RejectsDuplicateSelectedNpcIdentities()
    {
        CreateGraph(out PersonStore persons, out GenealogyStore genealogy,
            out _, out _);

        Assert.That(DailyV1RestoredPersonGraphContract.TryValidate(
            persons, genealogy, new[] { "npc", "npc" }, out string diagnostic), Is.False);
        Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
    }

    private static void CreateGraph(
        out PersonStore persons,
        out GenealogyStore genealogy,
        out PersonId parent,
        out PersonId child)
    {
        persons = new PersonStore();
        genealogy = new GenealogyStore();
        parent = new PersonId("person-parent");
        child = new PersonId("person-child");
        Assert.That(persons.TryRegister(new PersonRuntime(parent), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(child), out _), Is.True);
    }
}
