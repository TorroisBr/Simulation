using System;

internal enum DailyV1SelectedNpcPersonBindingFailure
{
    None = 0,
    InvalidOwnerGraph = 1,
    InvalidPerson = 2,
    InvalidNpcRuntimeId = 3,
    NpcNotRegistered = 4,
    PersonNotRegistered = 5,
    AlreadyMaterialized = 6,
    NpcAlreadyBoundToDifferentPerson = 7,
    LifeStateConflict = 8,
    ResidenceConflict = 9,
    RevisionOverflow = 10,
    StaleState = 11,
    MutationRejected = 12,
    CompensationFailed = 13
}

/// <summary>
/// Captures exact Core owner references and revisions for one selected NPC to
/// Person binding. It is ephemeral operation state, never a persistence row.
/// </summary>
internal sealed class DailyV1SelectedNpcPersonBindingPlan
{
    internal DailyV1SelectedRuntimeOwnerGraph OwnerGraph { get; }
    internal PersonStore PersonStore { get; }
    internal PersonRuntime Person { get; }
    internal DailyV1NpcRuntimeOwners Npc { get; }
    internal PersonId PersonId { get; }
    internal string NpcRuntimeId { get; }
    internal long AbsoluteDay { get; }
    internal long PersonStoreRevision { get; }
    internal long? PersonDeathAbsoluteDay { get; }
    internal string PersonResidenceBefore { get; }
    internal long PersonLifeResidenceRevision { get; }
    internal NpcLifeState NpcLifeState { get; }
    internal long NpcLifeStateRevision { get; }
    internal string NpcResidenceBefore { get; }
    internal long NpcResidenceRevision { get; }

    internal DailyV1SelectedNpcPersonBindingPlan(
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        PersonStore personStore,
        PersonRuntime person,
        DailyV1NpcRuntimeOwners npc,
        PersonId personId,
        string npcRuntimeId,
        long absoluteDay)
    {
        OwnerGraph = ownerGraph ?? throw new ArgumentNullException(nameof(ownerGraph));
        PersonStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        Person = person ?? throw new ArgumentNullException(nameof(person));
        Npc = npc ?? throw new ArgumentNullException(nameof(npc));
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));
        NpcRuntimeId = npcRuntimeId ?? throw new ArgumentNullException(nameof(npcRuntimeId));
        AbsoluteDay = absoluteDay;
        PersonStoreRevision = personStore.Revision;
        PersonDeathAbsoluteDay = person.DeathAbsoluteDay;
        PersonResidenceBefore = person.ResidenceSettlementRuntimeId;
        PersonLifeResidenceRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        NpcLifeState = npc.Lifecycle.LifeState;
        NpcLifeStateRevision = npc.Lifecycle.LifeStateRevision;
        NpcResidenceBefore = npc.Residence.SettlementRuntimeId;
        NpcResidenceRevision = npc.Residence.Revision;
    }

    internal bool RequiresPersonResidenceUpdate =>
        string.IsNullOrWhiteSpace(PersonResidenceBefore)
        && !string.IsNullOrWhiteSpace(NpcResidenceBefore);
}

/// <summary>
/// Core authority for binding a registered Person to one exact selected NPC
/// owner. The Unity runtime adapter still owns the outer P12
/// <c>runtime.npc-membership</c> reservation and its compatibility projection;
/// this operation owns selected-profile validation and Core owner writes.
/// </summary>
internal static class DailyV1SelectedNpcPersonBindingOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        PersonId personId,
        string npcRuntimeId,
        long absoluteDay,
        out DailyV1SelectedNpcPersonBindingPlan plan,
        out DailyV1SelectedNpcPersonBindingFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedNpcPersonBindingFailure.InvalidOwnerGraph;
        if (graph == null || personStore == null
            || !ReferenceEquals(graph.PersonStore, personStore)
            || graph.Definitions == null)
            return false;

        if (personId == null)
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.InvalidPerson;
            return false;
        }
        if (string.IsNullOrWhiteSpace(npcRuntimeId))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.InvalidNpcRuntimeId;
            return false;
        }
        if (absoluteDay < 0L)
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.InvalidOwnerGraph;
            return false;
        }
        if (!graph.TryGetNpcOwner(npcRuntimeId, out DailyV1NpcRuntimeOwners npc)
            || !graph.ContainsExactNpcOwner(npc))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.NpcNotRegistered;
            return false;
        }
        if (!ReferenceEquals(npc.Definitions, graph.Definitions)
            || npc.Lifecycle == null
            || npc.Residence == null
            || !string.Equals(npc.Residence.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !NpcInjuryRules.IsValid(npc.Lifecycle.LifeState))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.InvalidOwnerGraph;
            return false;
        }
        if (!personStore.TryGet(personId, out PersonRuntime person))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.PersonNotRegistered;
            return false;
        }
        if (person.IsMaterialized)
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.AlreadyMaterialized;
            return false;
        }
        if (personStore.TryGetByMaterializedNpcRuntimeId(npcRuntimeId, out PersonRuntime boundPerson))
        {
            failure = ReferenceEquals(boundPerson, person)
                ? DailyV1SelectedNpcPersonBindingFailure.AlreadyMaterialized
                : DailyV1SelectedNpcPersonBindingFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        bool personIsDead = person.IsDeadAt(absoluteDay);
        bool npcIsDead = npc.Lifecycle.LifeState == NpcLifeState.Dead;
        if (personIsDead != npcIsDead)
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.LifeStateConflict;
            return false;
        }

        string personResidence = person.ResidenceSettlementRuntimeId;
        string npcResidence = npc.Residence.SettlementRuntimeId;
        if (!string.IsNullOrWhiteSpace(personResidence)
            && !string.IsNullOrWhiteSpace(npcResidence)
            && !string.Equals(personResidence, npcResidence, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.ResidenceConflict;
            return false;
        }
        if (string.IsNullOrWhiteSpace(personResidence)
            && !string.IsNullOrWhiteSpace(npcResidence)
            && !PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(person, 1))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.RevisionOverflow;
            return false;
        }

        plan = new DailyV1SelectedNpcPersonBindingPlan(
            graph, personStore, person, npc, personId, npcRuntimeId, absoluteDay);
        failure = DailyV1SelectedNpcPersonBindingFailure.None;
        return true;
    }

    /// <summary>
    /// Commits the exact Core PersonStore binding after the Unity adapter has
    /// prepared its compatibility projection. The caller must account for
    /// every reported PersonStore revision increment in the enclosing P12
    /// membership scope, including a successful compensation.
    /// </summary>
    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedNpcPersonBindingPlan plan,
        long absoluteDay,
        out DailyV1SelectedNpcPersonBindingFailure failure,
        out int personStoreRevisionDelta,
        out bool compensationFailed)
    {
        failure = DailyV1SelectedNpcPersonBindingFailure.StaleState;
        personStoreRevisionDelta = 0;
        compensationFailed = false;
        if (graph == null || personStore == null || plan == null
            || !ReferenceEquals(graph, plan.OwnerGraph)
            || !ReferenceEquals(personStore, plan.PersonStore)
            || !ReferenceEquals(graph.PersonStore, personStore)
            || absoluteDay != plan.AbsoluteDay
            || !graph.ContainsExactNpcOwner(plan.Npc)
            || !graph.TryGetNpcOwner(plan.NpcRuntimeId, out DailyV1NpcRuntimeOwners currentNpc)
            || !ReferenceEquals(currentNpc, plan.Npc)
            || !ReferenceEquals(plan.Npc.Definitions, graph.Definitions)
            || plan.Npc.Lifecycle == null
            || plan.Npc.Residence == null
            || !ReferenceEquals(plan.Npc.Residence, currentNpc.Residence)
            || !string.Equals(plan.Npc.Residence.OwnerRuntimeId, plan.NpcRuntimeId, StringComparison.Ordinal)
            || personStore.Revision != plan.PersonStoreRevision
            || !personStore.TryGet(plan.PersonId, out PersonRuntime currentPerson)
            || !ReferenceEquals(currentPerson, plan.Person)
            || plan.Person.IsMaterialized
            || plan.Person.DeathAbsoluteDay != plan.PersonDeathAbsoluteDay
            || !string.Equals(plan.Person.ResidenceSettlementRuntimeId,
                plan.PersonResidenceBefore, StringComparison.Ordinal)
            || PersonRuntimeOwnerBridge.GetLifeResidenceRevision(plan.Person)
                != plan.PersonLifeResidenceRevision
            || plan.Npc.Lifecycle.LifeState != plan.NpcLifeState
            || plan.Npc.Lifecycle.LifeStateRevision != plan.NpcLifeStateRevision
            || !string.Equals(plan.Npc.Residence.SettlementRuntimeId,
                plan.NpcResidenceBefore, StringComparison.Ordinal)
            || plan.Npc.Residence.Revision != plan.NpcResidenceRevision
            || !NpcInjuryRules.IsValid(plan.Npc.Lifecycle.LifeState))
            return false;

        if (personStore.TryGetByMaterializedNpcRuntimeId(plan.NpcRuntimeId, out _))
            return false;
        if (plan.Person.IsDeadAt(absoluteDay)
            != (plan.Npc.Lifecycle.LifeState == NpcLifeState.Dead))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.LifeStateConflict;
            return false;
        }

        string currentPersonResidence = plan.Person.ResidenceSettlementRuntimeId;
        string currentNpcResidence = plan.Npc.Residence.SettlementRuntimeId;
        if (!string.IsNullOrWhiteSpace(currentPersonResidence)
            && !string.IsNullOrWhiteSpace(currentNpcResidence)
            && !string.Equals(currentPersonResidence, currentNpcResidence, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.ResidenceConflict;
            return false;
        }
        if (plan.RequiresPersonResidenceUpdate
            && !PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(plan.Person, 1))
        {
            failure = DailyV1SelectedNpcPersonBindingFailure.RevisionOverflow;
            return false;
        }

        if (!personStore.TryBindMaterializedNpc(
                plan.PersonId, plan.NpcRuntimeId, out PersonStoreFailure storeFailure))
        {
            failure = MapStoreFailure(storeFailure);
            return false;
        }
        personStoreRevisionDelta = 1;

        if (plan.RequiresPersonResidenceUpdate
            && !PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
                plan.Person, plan.NpcResidenceBefore))
        {
            if (personStore.TryRollbackMaterializedNpcBinding(plan.PersonId, plan.NpcRuntimeId))
            {
                personStoreRevisionDelta++;
                failure = DailyV1SelectedNpcPersonBindingFailure.ResidenceConflict;
            }
            else
            {
                compensationFailed = true;
                failure = DailyV1SelectedNpcPersonBindingFailure.CompensationFailed;
            }
            return false;
        }

        failure = DailyV1SelectedNpcPersonBindingFailure.None;
        return true;
    }

    private static DailyV1SelectedNpcPersonBindingFailure MapStoreFailure(PersonStoreFailure failure)
    {
        switch (failure)
        {
            case PersonStoreFailure.PersonNotRegistered:
                return DailyV1SelectedNpcPersonBindingFailure.PersonNotRegistered;
            case PersonStoreFailure.AlreadyMaterialized:
                return DailyV1SelectedNpcPersonBindingFailure.AlreadyMaterialized;
            case PersonStoreFailure.NpcAlreadyBoundToAnotherPerson:
                return DailyV1SelectedNpcPersonBindingFailure.NpcAlreadyBoundToDifferentPerson;
            case PersonStoreFailure.RevisionOverflow:
                return DailyV1SelectedNpcPersonBindingFailure.RevisionOverflow;
            case PersonStoreFailure.RuntimeFaulted:
                return DailyV1SelectedNpcPersonBindingFailure.MutationRejected;
            default:
                return DailyV1SelectedNpcPersonBindingFailure.InvalidPerson;
        }
    }
}
