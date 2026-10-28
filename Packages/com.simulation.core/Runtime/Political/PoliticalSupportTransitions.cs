using System;

public sealed class PoliticalSupportAddTransition : IEquatable<PoliticalSupportAddTransition>
{
    internal PoliticalSupportStore ExpectedStore { get; }
    internal PoliticalSupportRelationRecord ExpectedRelation { get; }
    internal long ExpectedStoreRevision { get; }

    public PoliticalSupportRelationId RelationId => ExpectedRelation?.RelationId;
    public PoliticalSupportSource Source => ExpectedRelation?.Source;
    public PoliticalSupportTarget Target => ExpectedRelation?.Target;
    public PoliticalSupportDisposition Disposition => ExpectedRelation == null
        ? default(PoliticalSupportDisposition)
        : ExpectedRelation.Disposition;
    public long ExpectedWorldDay { get; }
    public long StartedAbsoluteDay => ExpectedRelation?.StartedAbsoluteDay ?? -1L;

    internal PoliticalSupportAddTransition(
        PoliticalSupportStore expectedStore,
        PoliticalSupportRelationRecord expectedRelation,
        long expectedStoreRevision,
        long expectedWorldDay)
    {
        ExpectedStore = expectedStore;
        ExpectedRelation = expectedRelation;
        ExpectedStoreRevision = expectedStoreRevision;
        ExpectedWorldDay = expectedWorldDay;
    }

    public bool Equals(PoliticalSupportAddTransition other)
    {
        return other != null
            && RelationId == other.RelationId
            && ExpectedStoreRevision == other.ExpectedStoreRevision
            && ExpectedWorldDay == other.ExpectedWorldDay;
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportAddTransition);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = RelationId?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ ExpectedStoreRevision.GetHashCode();
            return (hash * 397) ^ ExpectedWorldDay.GetHashCode();
        }
    }
}

public sealed class PoliticalSupportEndTransition : IEquatable<PoliticalSupportEndTransition>
{
    internal PoliticalSupportStore ExpectedStore { get; }
    internal PoliticalSupportRelationRecord ExpectedRelation { get; }
    internal long ExpectedStoreRevision { get; }

    public PoliticalSupportRelationId RelationId => ExpectedRelation?.RelationId;
    public PoliticalSupportSource Source => ExpectedRelation?.Source;
    public PoliticalSupportTarget Target => ExpectedRelation?.Target;
    public PoliticalSupportDisposition Disposition => ExpectedRelation == null
        ? default(PoliticalSupportDisposition)
        : ExpectedRelation.Disposition;
    public long ExpectedWorldDay { get; }
    public long EndedAbsoluteDay { get; }

    internal PoliticalSupportEndTransition(
        PoliticalSupportStore expectedStore,
        PoliticalSupportRelationRecord expectedRelation,
        long expectedStoreRevision,
        long expectedWorldDay,
        long endedAbsoluteDay)
    {
        ExpectedStore = expectedStore;
        ExpectedRelation = expectedRelation;
        ExpectedStoreRevision = expectedStoreRevision;
        ExpectedWorldDay = expectedWorldDay;
        EndedAbsoluteDay = endedAbsoluteDay;
    }

    public bool Equals(PoliticalSupportEndTransition other)
    {
        return other != null
            && RelationId == other.RelationId
            && ExpectedStoreRevision == other.ExpectedStoreRevision
            && ExpectedWorldDay == other.ExpectedWorldDay
            && EndedAbsoluteDay == other.EndedAbsoluteDay;
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportEndTransition);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = RelationId?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ ExpectedStoreRevision.GetHashCode();
            return (hash * 397) ^ ExpectedWorldDay.GetHashCode();
        }
    }
}
