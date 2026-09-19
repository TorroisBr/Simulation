using System;

public enum PersonGenealogyFailure
{
    None = 0,
    InvalidWorld = 1,
    InvalidParent = 2,
    InvalidChild = 3,
    ParentNotRegistered = 4,
    ChildNotRegistered = 5,
    SelfParent = 6,
    DuplicateParentage = 7,
    WouldCreateCycle = 8,
    ParentageNotFound = 9,
    StoreFailure = 10
}

/// <summary>
/// World-owned parentage boundary. Person existence is validated here; graph
/// semantics remain implemented only by GenealogyStore.
/// </summary>
public static class PersonGenealogySystem
{
    public static bool TryAddParentage(
        SimulationRuntime world,
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        failure = PersonGenealogyFailure.None;

        if (world == null)
        {
            failure = PersonGenealogyFailure.InvalidWorld;
            return false;
        }

        if (parentId == null)
        {
            failure = PersonGenealogyFailure.InvalidParent;
            return false;
        }

        if (childId == null)
        {
            failure = PersonGenealogyFailure.InvalidChild;
            return false;
        }

        if (world.PersonStore.TryGet(parentId, out _) == false)
        {
            failure = PersonGenealogyFailure.ParentNotRegistered;
            return false;
        }

        if (world.PersonStore.TryGet(childId, out _) == false)
        {
            failure = PersonGenealogyFailure.ChildNotRegistered;
            return false;
        }

        return TryStoreAdd(world, parentId, childId, out failure);
    }

    public static bool TryRemoveParentage(
        SimulationRuntime world,
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        failure = PersonGenealogyFailure.None;

        if (world == null)
        {
            failure = PersonGenealogyFailure.InvalidWorld;
            return false;
        }

        if (parentId == null)
        {
            failure = PersonGenealogyFailure.InvalidParent;
            return false;
        }

        if (childId == null)
        {
            failure = PersonGenealogyFailure.InvalidChild;
            return false;
        }

        if (world.PersonStore.TryGet(parentId, out _) == false)
        {
            failure = PersonGenealogyFailure.ParentNotRegistered;
            return false;
        }

        if (world.PersonStore.TryGet(childId, out _) == false)
        {
            failure = PersonGenealogyFailure.ChildNotRegistered;
            return false;
        }

        GenealogyFailure storeFailure;
        if (world.GenealogyStoreForWorldBoundary.TryRemoveParentage(
                parentId,
                childId,
                out storeFailure) == false)
        {
            failure = MapStoreFailure(storeFailure);
            return false;
        }

        return true;
    }

    internal static bool TryStoreAdd(
        SimulationRuntime world,
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        GenealogyFailure storeFailure;
        if (world.GenealogyStoreForWorldBoundary.TryAddParentage(
                parentId,
                childId,
                out storeFailure) == false)
        {
            failure = MapStoreFailure(storeFailure);
            return false;
        }

        failure = PersonGenealogyFailure.None;
        return true;
    }

    internal static bool TryStoreRemove(
        SimulationRuntime world,
        PersonId parentId,
        PersonId childId)
    {
        return world.GenealogyStoreForWorldBoundary.TryRemoveParentage(
            parentId,
            childId,
            out _);
    }

    private static PersonGenealogyFailure MapStoreFailure(GenealogyFailure failure)
    {
        if (failure == null)
        {
            return PersonGenealogyFailure.StoreFailure;
        }

        switch (failure.Code)
        {
            case GenealogyFailureCode.InvalidParent:
                return PersonGenealogyFailure.InvalidParent;
            case GenealogyFailureCode.InvalidChild:
                return PersonGenealogyFailure.InvalidChild;
            case GenealogyFailureCode.SelfParent:
                return PersonGenealogyFailure.SelfParent;
            case GenealogyFailureCode.DuplicateParentage:
                return PersonGenealogyFailure.DuplicateParentage;
            case GenealogyFailureCode.WouldCreateCycle:
                return PersonGenealogyFailure.WouldCreateCycle;
            case GenealogyFailureCode.ParentageNotFound:
                return PersonGenealogyFailure.ParentageNotFound;
            default:
                return PersonGenealogyFailure.StoreFailure;
        }
    }
}
