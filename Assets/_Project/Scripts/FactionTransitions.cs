using System;

public sealed class FactionAffiliationAddTransition : IEquatable<FactionAffiliationAddTransition>
{
    internal FactionStore ExpectedStore { get; }
    internal long ExpectedStoreRevision { get; }
    public FactionAffiliationRecord Affiliation { get; }
    public FactionId FactionId => Affiliation?.FactionId;
    public PersonId PersonId => Affiliation?.PersonId;
    public long ExpectedWorldDay { get; }

    internal FactionAffiliationAddTransition(FactionStore expectedStore, FactionAffiliationRecord affiliation, long expectedStoreRevision, long expectedWorldDay)
    {
        ExpectedStore = expectedStore;
        Affiliation = affiliation;
        ExpectedStoreRevision = expectedStoreRevision;
        ExpectedWorldDay = expectedWorldDay;
    }

    public bool Equals(FactionAffiliationAddTransition other) => other != null
        && Equals(Affiliation, other.Affiliation)
        && ExpectedStoreRevision == other.ExpectedStoreRevision
        && ExpectedWorldDay == other.ExpectedWorldDay;
    public override bool Equals(object obj) => Equals(obj as FactionAffiliationAddTransition);
    public override int GetHashCode() => (Affiliation?.GetHashCode() ?? 0) ^ ExpectedStoreRevision.GetHashCode();
}

public sealed class FactionAffiliationEndTransition : IEquatable<FactionAffiliationEndTransition>
{
    internal FactionStore ExpectedStore { get; }
    internal FactionAffiliationRecord ExpectedAffiliation { get; }
    internal long ExpectedStoreRevision { get; }
    public FactionId FactionId => ExpectedAffiliation?.FactionId;
    public PersonId PersonId => ExpectedAffiliation?.PersonId;
    public long ExpectedWorldDay { get; }
    public long EndedAbsoluteDay { get; }

    internal FactionAffiliationEndTransition(FactionStore expectedStore, FactionAffiliationRecord expectedAffiliation, long expectedStoreRevision, long expectedWorldDay, long endedAbsoluteDay)
    {
        ExpectedStore = expectedStore;
        ExpectedAffiliation = expectedAffiliation;
        ExpectedStoreRevision = expectedStoreRevision;
        ExpectedWorldDay = expectedWorldDay;
        EndedAbsoluteDay = endedAbsoluteDay;
    }

    public bool Equals(FactionAffiliationEndTransition other) => other != null
        && ReferenceEquals(ExpectedAffiliation, other.ExpectedAffiliation)
        && ExpectedStoreRevision == other.ExpectedStoreRevision
        && ExpectedWorldDay == other.ExpectedWorldDay
        && EndedAbsoluteDay == other.EndedAbsoluteDay;
    public override bool Equals(object obj) => Equals(obj as FactionAffiliationEndTransition);
    public override int GetHashCode() => (ExpectedAffiliation?.GetHashCode() ?? 0) ^ EndedAbsoluteDay.GetHashCode();
}

internal static class FactionAffiliationSystem
{
    public static bool TryProposeAdd(FactionStore store, FactionId factionId, PersonId personId, long expectedWorldDay, out FactionAffiliationAddTransition transition, out FactionFoundationFailure failure)
    {
        transition = null;
        if (store == null || factionId == null || personId == null || expectedWorldDay < 0L)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A faction, PersonId, and valid world day are required.");
            return false;
        }

        if (store.TryGet(factionId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.FactionNotRegistered, "The faction is not registered.");
            return false;
        }

        if (store.TryGetAffiliation(factionId, personId, out FactionAffiliationRecord existing))
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.DuplicateAffiliation,
                existing.IsActive
                    ? "The faction/person affiliation is already active."
                    : "The faction/person affiliation already has a recorded lifecycle.");
            return false;
        }

        transition = new FactionAffiliationAddTransition(
            store,
            new FactionAffiliationRecord(factionId, personId, expectedWorldDay),
            store.Revision,
            expectedWorldDay);
        failure = FactionFoundationFailure.None;
        return true;
    }

    public static bool TryApplyAdd(FactionStore store, FactionAffiliationAddTransition transition, out FactionFoundationFailure failure)
    {
        if (store == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidTransition,
                "A faction store is required.");
            return false;
        }

        return store.TryApplyAdd(transition, out failure);
    }

    public static bool TryProposeEnd(FactionStore store, FactionId factionId, PersonId personId, long expectedWorldDay, out FactionAffiliationEndTransition transition, out FactionFoundationFailure failure)
    {
        transition = null;
        if (store == null || factionId == null || personId == null || expectedWorldDay < 0L)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A faction, PersonId, and valid world day are required.");
            return false;
        }

        if (store.TryGetAffiliation(factionId, personId, out FactionAffiliationRecord existing) == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.AffiliationNotRegistered, "The faction/person affiliation is not registered.");
            return false;
        }

        if (existing.IsActive == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.AffiliationAlreadyEnded, "The faction/person affiliation has already ended.");
            return false;
        }

        transition = new FactionAffiliationEndTransition(
            store,
            existing,
            store.Revision,
            expectedWorldDay,
            expectedWorldDay);
        failure = FactionFoundationFailure.None;
        return true;
    }

    public static bool TryApplyEnd(FactionStore store, FactionAffiliationEndTransition transition, out FactionFoundationFailure failure)
    {
        if (store == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidTransition,
                "A faction store is required.");
            return false;
        }

        return store.TryApplyEnd(transition, out failure);
    }
}
