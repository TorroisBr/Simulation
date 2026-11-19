using System;
using System.Collections.Generic;
using System.Threading;

internal interface IDailyV1SelectedPersonDeathP12Scope : IDisposable
{
    bool Complete(bool operationSucceeded);
}

internal interface IDailyV1SelectedPersonDeathBoundary
    : IDailyV1SelectedPersonDeathProjectionBoundary
{
    bool TryBeginPersonDeathOperation(
        DailyV1SelectedPersonDeathPlan plan,
        bool currentActionChanged,
        string operationId,
        IReadOnlyList<string> sectionIds,
        Func<bool> localRevisionCapacity,
        out IDailyV1SelectedPersonDeathP12Scope scope);
}

/// <summary>
/// Optional host projection callbacks for the selected Person-death transaction.
/// P12 admission, Core owner writes and mutation-epoch publication are always
/// owned by Core. A Unity host supplies this adapter to keep its compatibility
/// projection synchronized; a detached host passes no projection.
/// </summary>
internal interface IDailyV1SelectedPersonDeathProjectionBoundary
{
    bool TryBeginPersonDeathProjectionMutation(
        DailyV1SelectedPersonDeathPlan plan,
        string operationId,
        bool currentActionChanged,
        IReadOnlyList<string> sectionIds);
    bool TryCompletePersonDeathProjectionMutation(
        DailyV1SelectedPersonDeathPlan plan,
        string operationId,
        bool currentActionChanged,
        IReadOnlyList<string> sectionIds,
        bool operationSucceeded);
    bool TryValidatePersonDeathProjection(
        DailyV1SelectedPersonDeathPlan plan,
        out bool currentActionChanged,
        out string failure);
    bool TryPublishPersonDeathProjection(
        DailyV1SelectedPersonDeathPlan plan,
        bool currentActionChanged);
    void FaultPersonDeathRuntime();
    void AdvancePersonDeathWorldRevision();
}

/// <summary>
/// Owns selected Person-death P12 admission and mutation-epoch publication for
/// the exact Core execution state. The host projection cannot enter or close
/// the operation and cannot publish its census epoch.
/// </summary>
internal sealed class DailyV1CoreSelectedPersonDeathBoundary
    : IDailyV1SelectedDemographyBoundary
{
    private sealed class OperationContext
    {
        internal readonly IReadOnlyList<string> SectionIds;
        internal readonly DailyV1SelectedPersonDeathPlan Plan;
        internal readonly string OperationId;
        internal readonly bool CurrentActionChanged;
        internal readonly DailyV1MutationEpochReservation MutationEpochReservation;
        internal readonly Thread OwnerThread;
        internal readonly int OwnerManagedThreadId;
        internal bool Completed;
        internal bool Succeeded;

        internal OperationContext(
            IReadOnlyList<string> sectionIds,
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            DailyV1MutationEpochReservation mutationEpochReservation)
        {
            SectionIds = sectionIds;
            Plan = plan;
            OperationId = operationId;
            CurrentActionChanged = currentActionChanged;
            MutationEpochReservation = mutationEpochReservation;
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
        }

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class OperationScope : IDailyV1SelectedPersonDeathP12Scope
    {
        private readonly DailyV1CoreSelectedPersonDeathBoundary owner;
        private readonly OperationContext context;
        private bool disposed;

        internal OperationScope(
            DailyV1CoreSelectedPersonDeathBoundary owner,
            OperationContext context)
        {
            this.owner = owner;
            this.context = context;
        }

        public bool Complete(bool operationSucceeded)
        {
            DailyV1CoreSelectedPersonDeathBoundary current = owner;
            if (disposed || context.Completed)
            {
                current.FaultPersonDeathRuntime();
                return false;
            }

            if (operationSucceeded)
            {
                // Complete synchronously so the caller can roll back every
                // authoritative owner before aborting a rejected commit.
                if (!current.TryCommitOperation(context))
                    return false;
                context.Completed = true;
                context.Succeeded = true;
                return true;
            }

            bool aborted = current.TryAbortOperation(context);
            context.Completed = true;
            context.Succeeded = false;
            if (!aborted) current.FaultPersonDeathRuntime();
            return aborted;
        }

        public void Dispose()
        {
            if (disposed) return;
            if (!context.Completed)
                Complete(operationSucceeded: false);
            disposed = true;
        }
    }

    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;
    private readonly IDailyV1SelectedPersonDeathProjectionBoundary projection;
    private readonly IDailyV1SelectedDemographyBoundary compatibilityBoundary;
    private readonly DailyV1CoreSelectedOwnerSectionMutationProtocol mutationProtocol;
    private OperationContext activeOperation;
    internal string LastPersonDeathCommitFailure { get; private set; }

    internal DailyV1CoreSelectedPersonDeathBoundary(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedPersonDeathProjectionBoundary projection)
    {
        this.executionState = executionState
            ?? throw new ArgumentNullException(nameof(executionState));
        this.ownerGraph = ownerGraph
            ?? throw new ArgumentNullException(nameof(ownerGraph));
        this.projection = projection;
        compatibilityBoundary = projection as IDailyV1SelectedDemographyBoundary;
        mutationProtocol = new DailyV1CoreSelectedOwnerSectionMutationProtocol(
            executionState, ownerGraph);
    }

    internal bool IsBoundTo(
        DailyV1ExecutionBoundaryState candidateExecutionState,
        DailyV1SelectedRuntimeOwnerGraph candidateOwnerGraph) =>
        ReferenceEquals(executionState, candidateExecutionState)
        && ReferenceEquals(ownerGraph, candidateOwnerGraph)
        && mutationProtocol.IsBoundTo(candidateExecutionState, candidateOwnerGraph);

    public bool TryValidatePersonDeathProjection(
        DailyV1SelectedPersonDeathPlan plan,
        out bool currentActionChanged,
        out string failure)
    {
        currentActionChanged = false;
        failure = "CorePersonDeathOwnerOrSessionMismatch";
        if (plan == null
            || !mutationProtocol.IsBoundTo(executionState, ownerGraph)
            || executionState.AdmissionContext?.IsOwnedByCurrentThread() != true
            || executionState.SimulationTime?.AbsoluteDay != plan.AbsoluteDay
            || plan.Person?.PersonId == null
            || ownerGraph.PersonStore == null
            || !ownerGraph.PersonStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person))
            return false;

        if (plan.MaterializedNpc != null)
        {
            DailyV1NpcRuntimeOwners npc = plan.MaterializedNpc;
            if (!ownerGraph.ContainsExactNpcOwner(npc)
                || npc.Lifecycle?.LifeState != NpcLifeState.Alive
                || npc.ActionSlot == null
                || !ownerGraph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime materializedPerson)
                || !ReferenceEquals(materializedPerson, plan.Person)
                || !ReferenceEquals(npc.ActionSlot.CurrentAction, plan.ExpectedAction)
                || !ReferenceEquals(npc.ActionSlot.CurrentPayload, plan.ExpectedPayload))
            {
                failure = "CorePersonDeathNpcOwnerMismatch";
                return false;
            }
            currentActionChanged = npc.ActionSlot.CurrentAction != null;
        }

        if (plan.IsResident
            && (!ownerGraph.ContainsExactCityOwner(plan.ResidenceCity)
                || plan.ResidenceCity.Population == null))
        {
            failure = "CorePersonDeathResidenceOwnerMismatch";
            return false;
        }

        if (currentActionChanged != (plan.ExpectedAction != null))
        {
            failure = "PersonDeathProjectionMismatch";
            return false;
        }

        if (projection != null
            && (!projection.TryValidatePersonDeathProjection(
                    plan, out bool projectionActionChanged, out failure)
                || projectionActionChanged != currentActionChanged))
        {
            failure = "PersonDeathProjectionMismatch";
            return false;
        }
        failure = null;
        return true;
    }

    public bool TryBeginPersonDeathOperation(
        DailyV1SelectedPersonDeathPlan plan,
        bool currentActionChanged,
        string operationId,
        IReadOnlyList<string> sectionIds,
        Func<bool> localRevisionCapacity,
        out IDailyV1SelectedPersonDeathP12Scope scope)
    {
        scope = null;
        if (activeOperation != null
            || !IsSupportedOperation(operationId)
            || plan == null
            || currentActionChanged != (plan.ExpectedAction != null)
            || sectionIds == null || sectionIds.Count == 0
            || localRevisionCapacity == null
            || !mutationProtocol.IsBoundTo(executionState, ownerGraph)
            || executionState.AdmissionContext?.IsOwnedByCurrentThread() != true
            || !DailyV1SelectedPersonDeathOperation.TryCreateP12SectionIds(
                plan, out IReadOnlyList<string> expectedSectionIds)
            || !HasExactSectionIds(expectedSectionIds, sectionIds))
            return false;

        bool hasCapacity;
        try { hasCapacity = localRevisionCapacity(); }
        catch { hasCapacity = false; }
        if (!hasCapacity) return false;

        if (!executionState.TryBeginRuntimeProtocolOperation(operationId))
        {
            FaultPersonDeathRuntime();
            return false;
        }

        object reservation = null;
        if (!mutationProtocol.TryValidateUnchangedSections(sectionIds)
            || !mutationProtocol.TryReserveMutationEpoch(out reservation)
            || !(reservation is DailyV1MutationEpochReservation coreReservation))
        {
            FaultPersonDeathRuntime();
            ReleaseMutationEpochSafely(reservation);
            if (!executionState.TryEndRuntimeProtocolOperation())
                mutationProtocol.FaultClosed();
            return false;
        }

        OperationContext context = new OperationContext(
            sectionIds, plan, operationId, currentActionChanged, coreReservation);
        activeOperation = context;
        bool projectionAdmitted = projection == null;
        try
        {
            if (projection != null)
                projectionAdmitted = projection.TryBeginPersonDeathProjectionMutation(
                    plan, operationId, currentActionChanged, sectionIds);
        }
        catch
        {
            projectionAdmitted = false;
        }
        if (!projectionAdmitted)
        {
            activeOperation = null;
            ReleaseMutationEpochSafely(coreReservation);
            try
            {
                if (!executionState.TryEndRuntimeProtocolOperation())
                    mutationProtocol.FaultClosed();
            }
            catch { mutationProtocol.FaultClosed(); }
            return false;
        }

        scope = new OperationScope(this, context);
        return true;
    }

    public bool TryPublishPersonDeathProjection(
        DailyV1SelectedPersonDeathPlan plan,
        bool currentActionChanged) =>
        projection == null
        || projection.TryPublishPersonDeathProjection(plan, currentActionChanged);

    public bool TryBeginPersonDeathProjectionMutation(
        DailyV1SelectedPersonDeathPlan plan,
        string operationId,
        bool currentActionChanged,
        IReadOnlyList<string> sectionIds) =>
        projection == null
        || projection.TryBeginPersonDeathProjectionMutation(
            plan, operationId, currentActionChanged, sectionIds);

    public bool TryCompletePersonDeathProjectionMutation(
        DailyV1SelectedPersonDeathPlan plan,
        string operationId,
        bool currentActionChanged,
        IReadOnlyList<string> sectionIds,
        bool operationSucceeded) =>
        projection == null
        || projection.TryCompletePersonDeathProjectionMutation(
            plan, operationId, currentActionChanged, sectionIds, operationSucceeded);

    public void FaultPersonDeathRuntime()
    {
        try { mutationProtocol.FaultClosed(); }
        catch { }
        try { projection?.FaultPersonDeathRuntime(); }
        catch { }
    }

    public void AdvancePersonDeathWorldRevision() =>
        projection?.AdvancePersonDeathWorldRevision();

    private bool TryCommitOperation(OperationContext context)
    {
        LastPersonDeathCommitFailure = null;
        if (context == null
            || !context.IsOwnedByCurrentThread()
            || !ReferenceEquals(activeOperation, context))
        {
            FaultPersonDeathRuntime();
            return false;
        }

        try
        {
            // When Unity is present, stage its compatibility baseline before
            // Core publishes the reserved epoch. A detached Core host has no
            // projection baseline to stage; its exact owner vector is still
            // validated and published by the same Core mutation protocol.
            if (projection != null
                && !projection.TryCompletePersonDeathProjectionMutation(
                        context.Plan,
                        context.OperationId,
                        context.CurrentActionChanged,
                        context.SectionIds,
                        operationSucceeded: true))
            {
                LastPersonDeathCommitFailure = "CompatibilityBaselinePreparationRejected";
                return false;
            }

            // Core atomically validates and refreshes its census, then
            // publishes exactly one reserved mutation epoch.
            if (!mutationProtocol.TryNotifyReservedCommittedMutations(
                    context.MutationEpochReservation,
                    ToSectionSet(context.SectionIds)))
            {
                LastPersonDeathCommitFailure = "CoreCensusOrEpochCommitRejected:"
                    + executionState.LastSelectedOwnerSectionCensusRefreshRejection;
                return false;
            }
        }
        catch (Exception exception)
        {
            LastPersonDeathCommitFailure = "CommitException:" + exception.GetType().Name;
            return false;
        }

        // This second host completion only revokes the exact temporary writer
        // context; its reads and baseline staging already succeeded above.
        try
        {
            if (projection != null
                && !projection.TryCompletePersonDeathProjectionMutation(
                        context.Plan,
                        context.OperationId,
                        context.CurrentActionChanged,
                        context.SectionIds,
                        operationSucceeded: true))
                FaultPersonDeathRuntime();
        }
        catch { FaultPersonDeathRuntime(); }

        try
        {
            if (!executionState.TryEndRuntimeProtocolOperation())
                FaultPersonDeathRuntime();
        }
        catch { FaultPersonDeathRuntime(); }
        activeOperation = null;
        return true;
    }

    private bool TryAbortOperation(OperationContext context)
    {
        if (context == null
            || !context.IsOwnedByCurrentThread()
            || !ReferenceEquals(activeOperation, context))
        {
            FaultPersonDeathRuntime();
            return false;
        }

        // Revoke the host writer permit and restore its exact compatibility
        // snapshot before releasing the Core reservation.
        bool projectionRestored = projection == null;
        try
        {
            if (projection != null)
                projectionRestored = projection.TryCompletePersonDeathProjectionMutation(
                    context.Plan,
                    context.OperationId,
                    context.CurrentActionChanged,
                    context.SectionIds,
                    operationSucceeded: false);
        }
        catch { projectionRestored = false; }
        if (!projectionRestored) FaultPersonDeathRuntime();

        bool ownersUnchanged;
        try
        {
            ownersUnchanged = executionState.TryValidateCoreSelectedOwnerSectionCensusRows(
                ownerGraph,
                context.SectionIds,
                context.MutationEpochReservation,
                out _);
        }
        catch { ownersUnchanged = false; }
        if (!ownersUnchanged) FaultPersonDeathRuntime();

        ReleaseMutationEpochSafely(context.MutationEpochReservation);

        try
        {
            if (!executionState.TryEndRuntimeProtocolOperation())
            {
                FaultPersonDeathRuntime();
                ownersUnchanged = false;
            }
        }
        catch
        {
            FaultPersonDeathRuntime();
            ownersUnchanged = false;
        }
        activeOperation = null;
        return ownersUnchanged && projectionRestored;
    }

    private void ReleaseMutationEpochSafely(object reservation)
    {
        try { mutationProtocol.ReleaseMutationEpoch(reservation); }
        catch { FaultPersonDeathRuntime(); }
    }

    private static IReadOnlyCollection<string> ToSectionSet(
        IReadOnlyList<string> sectionIds)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        if (sectionIds != null)
            foreach (string sectionId in sectionIds)
                if (!string.IsNullOrWhiteSpace(sectionId)) result.Add(sectionId);
        return result;
    }

    private static bool HasExactSectionIds(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual)
    {
        if (expected == null || actual == null || expected.Count != actual.Count)
            return false;
        for (int index = 0; index < expected.Count; index++)
            if (!string.Equals(expected[index], actual[index], StringComparison.Ordinal))
                return false;
        return true;
    }

    public IReadOnlyDictionary<string, int> BuildCompatibilityResidentFloorSupplement(
        IReadOnlyCollection<string> selectedNpcRuntimeIds)
    {
        if (compatibilityBoundary != null)
            return compatibilityBoundary.BuildCompatibilityResidentFloorSupplement(
                selectedNpcRuntimeIds);
        return new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private static bool IsSupportedOperation(string operationId) =>
        string.Equals(operationId, DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            StringComparison.Ordinal)
        || string.Equals(operationId, DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            StringComparison.Ordinal);
}

internal interface IDailyV1SelectedDemographyBoundary : IDailyV1SelectedPersonDeathBoundary
{
    /// <summary>
    /// Returns represented residents from host compatibility NPCs outside the
    /// exact selected Core owner graph. PersonStore residents and selected Core
    /// NPC residence owners are counted by Core and must not be included here.
    /// </summary>
    IReadOnlyDictionary<string, int> BuildCompatibilityResidentFloorSupplement(
        IReadOnlyCollection<string> selectedNpcRuntimeIds);
}

/// <summary>
/// Exact selected-owner resident census shared by Core demography and the
/// Unity population-operation adapter. Person-backed NPCs are counted only
/// through PersonStore; selected NPC-only residences are counted through the
/// exact lifecycle and residence owners.
/// </summary>
internal static class DailyV1SelectedResidentCensus
{
    internal static bool TryCountNamedResidents(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string cityRuntimeId,
        out int residentCount)
    {
        residentCount = 0;
        if (graph?.PersonStore == null || string.IsNullOrWhiteSpace(cityRuntimeId))
            return false;

        foreach (PersonRuntime person in graph.PersonStore.Persons)
        {
            if (person == null || person.PersonId == null)
                return false;
            if (!person.DeathAbsoluteDay.HasValue
                && string.Equals(person.ResidenceSettlementRuntimeId, cityRuntimeId, StringComparison.Ordinal))
            {
                if (residentCount == int.MaxValue) return false;
                residentCount++;
            }
        }

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.Lifecycle == null || npc.Residence == null
                || !string.Equals(npc.Residence.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                return false;
            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime materializedPerson))
            {
                if (materializedPerson == null
                    || materializedPerson.PersonId == null
                    || !ReferenceEquals(
                        materializedPerson,
                        graph.PersonStore.TryGet(materializedPerson.PersonId, out PersonRuntime registered)
                            ? registered
                            : null)
                    || !string.Equals(materializedPerson.MaterializedNpcRuntimeId,
                        npc.RuntimeId, StringComparison.Ordinal))
                    return false;
                continue;
            }
            if (npc.Lifecycle.LifeState == NpcLifeState.Alive
                && string.Equals(npc.Residence.SettlementRuntimeId, cityRuntimeId, StringComparison.Ordinal))
            {
                if (residentCount == int.MaxValue) return false;
                residentCount++;
            }
        }

        return true;
    }
}
internal enum DailyV1SelectedPersonResidenceBindingFailure
{
    None = 0,
    InvalidOwnerGraph = 1,
    InvalidPerson = 2,
    InvalidSettlement = 3,
    SettlementNotInGraph = 4,
    PersonNotRegistered = 5,
    PersonDead = 6,
    ResidenceAlreadyAssigned = 7,
    AggregateCapacityExceeded = 8,
    InvalidResidentCensus = 9,
    RevisionOverflow = 10,
    StaleState = 11,
    MutationRejected = 12
}

internal sealed class DailyV1SelectedPersonResidenceBindingPlan
{
    internal PersonRuntime Person { get; }
    internal DailyV1CityRuntimeOwners City { get; }
    internal string ResidenceBefore { get; }
    internal long ResidenceRevision { get; }
    internal int NamedResidentsBefore { get; }
    internal long PopulationRevision { get; }
    internal int PopulationBefore { get; }
    internal bool IsIdempotent { get; }

    internal DailyV1SelectedPersonResidenceBindingPlan(
        PersonRuntime person,
        DailyV1CityRuntimeOwners city,
        string residenceBefore,
        long residenceRevision,
        int namedResidentsBefore,
        long populationRevision,
        int populationBefore,
        bool isIdempotent)
    {
        Person = person ?? throw new ArgumentNullException(nameof(person));
        City = city ?? throw new ArgumentNullException(nameof(city));
        ResidenceBefore = residenceBefore;
        ResidenceRevision = residenceRevision;
        NamedResidentsBefore = namedResidentsBefore;
        PopulationRevision = populationRevision;
        PopulationBefore = populationBefore;
        IsIdempotent = isIdempotent;
    }
}

internal static class DailyV1SelectedPersonResidenceBindingOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        PersonRuntime person,
        DailyV1CityRuntimeOwners city,
        out DailyV1SelectedPersonResidenceBindingPlan plan,
        out DailyV1SelectedPersonResidenceBindingFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedPersonResidenceBindingFailure.InvalidOwnerGraph;
        if (graph == null || personStore == null
            || !ReferenceEquals(graph.PersonStore, personStore))
            return false;

        if (person == null || person.PersonId == null)
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.InvalidPerson;
            return false;
        }
        if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.InvalidSettlement;
            return false;
        }
        if (!graph.ContainsExactCityOwner(city))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.SettlementNotInGraph;
            return false;
        }
        if (!ReferenceEquals(city.Definitions, graph.Definitions))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.InvalidOwnerGraph;
            return false;
        }
        if (!personStore.TryGet(person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, person))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.PersonNotRegistered;
            return false;
        }
        if (person.DeathAbsoluteDay.HasValue)
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.PersonDead;
            return false;
        }

        string residence = person.ResidenceSettlementRuntimeId;
        long residenceRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        if (!string.IsNullOrWhiteSpace(residence))
        {
            if (!string.Equals(residence, city.RuntimeId, StringComparison.Ordinal))
            {
                failure = DailyV1SelectedPersonResidenceBindingFailure.ResidenceAlreadyAssigned;
                return false;
            }

            plan = new DailyV1SelectedPersonResidenceBindingPlan(
                person, city, residence, residenceRevision, 0, 0L, 0, true);
            failure = DailyV1SelectedPersonResidenceBindingFailure.None;
            return true;
        }

        SettlementPopulationRuntime population = city.Population;
        if (population == null
            || !string.Equals(population.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.InvalidOwnerGraph;
            return false;
        }
        if (!DailyV1SelectedResidentCensus.TryCountNamedResidents(
                graph, city.RuntimeId, out int namedResidents))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.InvalidResidentCensus;
            return false;
        }
        if (namedResidents >= population.CurrentPopulation)
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.AggregateCapacityExceeded;
            return false;
        }
        if (residenceRevision == long.MaxValue
            || !PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(person, 1))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.RevisionOverflow;
            return false;
        }

        plan = new DailyV1SelectedPersonResidenceBindingPlan(
            person,
            city,
            residence,
            residenceRevision,
            namedResidents,
            population.Revision,
            population.CurrentPopulation,
            false);
        failure = DailyV1SelectedPersonResidenceBindingFailure.None;
        return true;
    }

    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonResidenceBindingPlan plan,
        out DailyV1SelectedPersonResidenceBindingFailure failure)
    {
        failure = DailyV1SelectedPersonResidenceBindingFailure.StaleState;
        if (graph == null || personStore == null || plan == null
            || !ReferenceEquals(graph.PersonStore, personStore)
            || !graph.ContainsExactCityOwner(plan.City)
            || !ReferenceEquals(plan.City.Definitions, graph.Definitions)
            || plan.Person.PersonId == null
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person)
            || plan.Person.DeathAbsoluteDay.HasValue
            || !string.Equals(
                plan.Person.ResidenceSettlementRuntimeId,
                plan.ResidenceBefore,
                StringComparison.Ordinal)
            || PersonRuntimeOwnerBridge.GetLifeResidenceRevision(plan.Person)
                != plan.ResidenceRevision)
            return false;

        if (plan.IsIdempotent)
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.None;
            return true;
        }

        SettlementPopulationRuntime population = plan.City.Population;
        if (population == null
            || population.Revision != plan.PopulationRevision
            || population.CurrentPopulation != plan.PopulationBefore
            || !string.IsNullOrWhiteSpace(plan.ResidenceBefore))
            return false;
        if (!DailyV1SelectedResidentCensus.TryCountNamedResidents(
                graph, plan.City.RuntimeId, out int currentNamedResidents)
            || currentNamedResidents != plan.NamedResidentsBefore
            || currentNamedResidents >= population.CurrentPopulation)
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.StaleState;
            return false;
        }
        if (plan.ResidenceRevision == long.MaxValue
            || !PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(plan.Person, 1))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.RevisionOverflow;
            return false;
        }
        if (!PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
                plan.Person, plan.City.RuntimeId))
        {
            failure = DailyV1SelectedPersonResidenceBindingFailure.MutationRejected;
            return false;
        }

        failure = DailyV1SelectedPersonResidenceBindingFailure.None;
        return true;
    }
}

/// <summary>
/// Immutable expected-state witness for one selected Person death. The
/// captured object references are the exact Core owners in the admitted
/// graph; the host uses them only to bind the existing P12 census operation.
/// </summary>
internal sealed class DailyV1SelectedPersonDeathPlan
{
    internal PersonRuntime Person { get; }
    internal long AbsoluteDay { get; }
    internal DailyV1NpcRuntimeOwners MaterializedNpc { get; }
    internal DailyV1CityRuntimeOwners ResidenceCity { get; }
    internal long ExpectedPopulationRevision { get; }
    internal int ExpectedPopulation { get; }
    internal long ExpectedPersonLifeResidenceRevision { get; }
    internal NpcLifeState ExpectedNpcLifeState { get; }
    internal NpcInjurySeverity ExpectedNpcInjurySeverity { get; }
    internal long ExpectedNpcLifeStateRevision { get; }
    internal long ExpectedNpcActionRevision { get; }
    internal SettlementPopulationTransition PopulationTransition { get; }
    internal DailyV1ActionDefinition ExpectedAction { get; }
    internal DailyV1NpcActionPayloadOwner ExpectedPayload { get; }
    internal bool IsResident => ResidenceCity != null;

    internal DailyV1SelectedPersonDeathPlan(
        PersonRuntime person,
        long absoluteDay,
        DailyV1NpcRuntimeOwners materializedNpc,
        DailyV1CityRuntimeOwners residenceCity,
        long expectedPopulationRevision,
        int expectedPopulation,
        long expectedPersonLifeResidenceRevision,
        NpcLifeState expectedNpcLifeState,
        NpcInjurySeverity expectedNpcInjurySeverity,
        long expectedNpcLifeStateRevision,
        long expectedNpcActionRevision,
        SettlementPopulationTransition populationTransition,
        DailyV1ActionDefinition expectedAction,
        DailyV1NpcActionPayloadOwner expectedPayload)
    {
        Person = person ?? throw new ArgumentNullException(nameof(person));
        AbsoluteDay = absoluteDay;
        MaterializedNpc = materializedNpc;
        ResidenceCity = residenceCity;
        ExpectedPopulationRevision = expectedPopulationRevision;
        ExpectedPopulation = expectedPopulation;
        ExpectedPersonLifeResidenceRevision = expectedPersonLifeResidenceRevision;
        ExpectedNpcLifeState = expectedNpcLifeState;
        ExpectedNpcInjurySeverity = expectedNpcInjurySeverity;
        ExpectedNpcLifeStateRevision = expectedNpcLifeStateRevision;
        ExpectedNpcActionRevision = expectedNpcActionRevision;
        PopulationTransition = populationTransition;
        ExpectedAction = expectedAction;
        ExpectedPayload = expectedPayload;
    }
}

/// <summary>
/// Core-owned selected Person-death proposal and commit. The host participates
/// only in P12 admission/notification around this operation.
/// </summary>
internal static class DailyV1SelectedPersonDeathOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        PersonRuntime person,
        long absoluteDay,
        out DailyV1SelectedPersonDeathPlan plan,
        out string failure)
    {
        plan = null;
        failure = "Selected Person death requires the exact Core owner graph and PersonStore.";
        if (graph == null || personStore == null || person == null
            || absoluteDay < 0L
            || !ReferenceEquals(graph.PersonStore, personStore)
            || person.PersonId == null
            || !personStore.TryGet(person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, person))
            return false;

        if (person.DeathAbsoluteDay.HasValue
            || (person.BirthAbsoluteDay.HasValue && person.BirthAbsoluteDay.Value > absoluteDay))
        {
            failure = "The selected Person is already dead or has a future birth day.";
            return false;
        }

        DailyV1NpcRuntimeOwners materializedNpc = null;
        if (!string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId))
        {
            if (!personStore.TryGetByMaterializedNpcRuntimeId(
                    person.MaterializedNpcRuntimeId, out PersonRuntime materializedPerson)
                || !ReferenceEquals(materializedPerson, person)
                || !graph.TryGetNpcOwner(person.MaterializedNpcRuntimeId, out materializedNpc)
                || !graph.ContainsExactNpcOwner(materializedNpc)
                || materializedNpc.Lifecycle?.LifeState != NpcLifeState.Alive)
            {
                failure = "The selected Person materialization does not bind to its exact living Core NPC owner.";
                return false;
            }
        }

        DailyV1CityRuntimeOwners residenceCity = null;
        long expectedPopulationRevision = 0L;
        int expectedPopulation = 0;
        SettlementPopulationTransition populationTransition = null;
        if (!string.IsNullOrWhiteSpace(person.ResidenceSettlementRuntimeId))
        {
            int matches = 0;
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            {
                if (candidate == null
                    || !string.Equals(candidate.RuntimeId,
                        person.ResidenceSettlementRuntimeId, StringComparison.Ordinal))
                    continue;
                matches++;
                residenceCity = candidate;
            }

            if (matches != 1 || residenceCity?.Population == null
                || residenceCity.Population.CurrentPopulation <= 0
                || !graph.ContainsExactCityOwner(residenceCity)
                || !SettlementPopulationSystem.TryPropose(
                    residenceCity.Population,
                    new PopulationChangeSet(0, 1, 0, 0),
                    out populationTransition,
                    out _))
            {
                failure = "The selected resident Person has no unique live Core City population owner.";
                return false;
            }

            expectedPopulationRevision = residenceCity.Population.Revision;
            expectedPopulation = residenceCity.Population.CurrentPopulation;
        }

        DailyV1ActionDefinition expectedAction = materializedNpc?.ActionSlot?.CurrentAction;
        DailyV1NpcActionPayloadOwner expectedPayload = materializedNpc?.ActionSlot?.CurrentPayload;
        if (materializedNpc != null
            && !materializedNpc.CanCommitPersonDeathAfterValidation(
                NpcInjurySeverity.None, expectedAction, expectedPayload))
        {
            failure = "The materialized Core NPC owner cannot admit its death transition.";
            return false;
        }

        long expectedPersonLifeResidenceRevision =
            PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        NpcLifeState expectedNpcLifeState = materializedNpc?.Lifecycle?.LifeState
            ?? NpcLifeState.Alive;
        NpcInjurySeverity expectedNpcInjurySeverity =
            materializedNpc?.Lifecycle?.InjurySeverity ?? NpcInjurySeverity.None;
        long expectedNpcLifeStateRevision = materializedNpc?.Lifecycle?.LifeStateRevision ?? 0L;
        long expectedNpcActionRevision = materializedNpc?.ActionSlot?.Revision ?? 0L;

        plan = new DailyV1SelectedPersonDeathPlan(
            person, absoluteDay, materializedNpc, residenceCity,
            expectedPopulationRevision, expectedPopulation,
            expectedPersonLifeResidenceRevision, expectedNpcLifeState,
            expectedNpcInjurySeverity, expectedNpcLifeStateRevision,
            expectedNpcActionRevision, populationTransition, expectedAction, expectedPayload);
        failure = null;
        return true;
    }

    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonDeathPlan plan,
        out bool rollbackSucceeded,
        out string failure)
    {
        rollbackSucceeded = true;
        failure = "Selected Person death expected state changed before Core commit.";
        if (graph == null || personStore == null || plan == null
            || !ReferenceEquals(graph.PersonStore, personStore)
            || plan.Person.PersonId == null
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person)
            || plan.Person.DeathAbsoluteDay.HasValue
            || (plan.Person.BirthAbsoluteDay.HasValue
                && plan.Person.BirthAbsoluteDay.Value > plan.AbsoluteDay)
            || PersonRuntimeOwnerBridge.GetLifeResidenceRevision(plan.Person)
                != plan.ExpectedPersonLifeResidenceRevision)
            return false;

        string expectedResidenceId = plan.ResidenceCity?.RuntimeId;
        if (!string.Equals(plan.Person.ResidenceSettlementRuntimeId,
                expectedResidenceId, StringComparison.Ordinal))
            return false;

        DailyV1NpcRuntimeOwners materializedNpc = plan.MaterializedNpc;
        string expectedNpcId = materializedNpc?.RuntimeId;
        if (!string.Equals(plan.Person.MaterializedNpcRuntimeId,
                expectedNpcId, StringComparison.Ordinal)
            || (materializedNpc != null
                && (!graph.ContainsExactNpcOwner(materializedNpc)
                    || !personStore.TryGetByMaterializedNpcRuntimeId(
                        materializedNpc.RuntimeId, out PersonRuntime boundPerson)
                    || !ReferenceEquals(boundPerson, plan.Person)
                    || materializedNpc.Lifecycle.LifeState != plan.ExpectedNpcLifeState
                    || materializedNpc.Lifecycle.InjurySeverity != plan.ExpectedNpcInjurySeverity
                    || materializedNpc.Lifecycle.LifeStateRevision != plan.ExpectedNpcLifeStateRevision
                    || materializedNpc.ActionSlot.Revision != plan.ExpectedNpcActionRevision
                    || !materializedNpc.CanCommitPersonDeathAfterValidation(
                        NpcInjurySeverity.None, plan.ExpectedAction, plan.ExpectedPayload))))
            return false;

        // P12 writer admission is established only after Core opens the exact
        // death operation and the Unity projection binds its scoped permit.
        // Preparation checks revision capacity here; checking the writer guard
        // before that operation exists would reject a valid selected death.
        if (!PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(
                plan.Person, plan.IsResident ? 2 : 1))
        {
            failure = "The selected Person lifecycle revision is unavailable.";
            return false;
        }

        if (plan.IsResident)
        {
            SettlementPopulationRuntime population = plan.ResidenceCity?.Population;
            if (population == null
                || !graph.ContainsExactCityOwner(plan.ResidenceCity)
                || population.Revision != plan.ExpectedPopulationRevision
                || population.CurrentPopulation != plan.ExpectedPopulation
                || plan.ExpectedPopulation <= 0
                || !PopulationOwnerBridge.CanAdvanceP12AggregateRevision(population)
                || plan.PopulationTransition == null
                || plan.PopulationTransition.ExpectedRevision != plan.ExpectedPopulationRevision
                || plan.PopulationTransition.PopulationBefore != plan.ExpectedPopulation)
            {
                failure = "The selected resident population owner changed or cannot advance its revision.";
                return false;
            }
        }

        // All owner identities, revisions, bindings, and the population
        // transition are checked before the first authoritative write. Any
        // later failure restores those exact owner instances and revisions
        // before the enclosing P12 scope releases its epoch reservation.
        try
        {
            if (plan.IsResident
                && !SettlementPopulationSystem.TryApply(
                    plan.ResidenceCity.Population, plan.PopulationTransition, out _))
                return AbortAndRollback(graph, personStore, plan,
                    "The prepared selected resident population transition was rejected.",
                    out rollbackSucceeded, out failure);

            if (!PersonRuntimeOwnerBridge.RecordDeathAfterValidation(
                    plan.Person, plan.AbsoluteDay))
                return AbortAndRollback(graph, personStore, plan,
                    "The selected Person death write was rejected after preflight.",
                    out rollbackSucceeded, out failure);

            if (plan.IsResident
                && !PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(plan.Person, null))
                return AbortAndRollback(graph, personStore, plan,
                    "The selected Person residence clear was rejected after preflight.",
                    out rollbackSucceeded, out failure);

            if (materializedNpc != null
                && !materializedNpc.TryCommitPersonDeathAfterValidation(
                    NpcInjurySeverity.None,
                    plan.ExpectedAction,
                    plan.ExpectedPayload,
                    out _))
                return AbortAndRollback(graph, personStore, plan,
                    "The selected materialized NPC death write was rejected after preflight.",
                    out rollbackSucceeded, out failure);

            failure = null;
            return true;
        }
        catch (Exception exception)
        {
            return AbortAndRollback(graph, personStore, plan,
                "The selected Person-death transaction threw before commit: " + exception.GetType().Name,
                out rollbackSucceeded, out failure);
        }
    }

    private static bool AbortAndRollback(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonDeathPlan plan,
        string failure,
        out bool rollbackSucceeded,
        out string resultFailure)
    {
        rollbackSucceeded = TryRollback(graph, personStore, plan);
        if (rollbackSucceeded)
        {
            resultFailure = failure + " All attempted owner writes were rolled back.";
            return false;
        }

        resultFailure = failure + " Rollback could not restore the exact prepared owner state.";
        return false;
    }

    private static bool TryRollback(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonDeathPlan plan)
    {
        if (graph == null || personStore == null || plan == null
            || !ReferenceEquals(graph.PersonStore, personStore)
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person))
            return false;

        string residenceBefore = plan.ResidenceCity?.RuntimeId;
        int personRevisionIncrements = plan.IsResident ? 2 : 1;
        if (!PersonRuntimeOwnerBridge.CanRollbackSelectedDeathMutation(
                plan.Person, plan.AbsoluteDay, residenceBefore,
                plan.ExpectedPersonLifeResidenceRevision, personRevisionIncrements))
            return false;

        SettlementPopulationRuntime population = plan.ResidenceCity?.Population;
        if (plan.IsResident
            && (!graph.ContainsExactCityOwner(plan.ResidenceCity)
                || population == null
                || !population.CanRollbackSelectedDeathTransition(plan.PopulationTransition)))
            return false;

        DailyV1NpcRuntimeOwners npc = plan.MaterializedNpc;
        if (npc != null
            && (!graph.ContainsExactNpcOwner(npc)
                || !npc.Lifecycle.CanRollbackSelectedDeathMutation(
                    plan.ExpectedNpcLifeState,
                    plan.ExpectedNpcInjurySeverity,
                    plan.ExpectedNpcLifeStateRevision)
                || !npc.ActionSlot.CanRollbackSelectedDeathMutation(
                    plan.ExpectedAction,
                    plan.ExpectedPayload,
                    plan.ExpectedNpcActionRevision)))
            return false;

        // Check every owner before restoring any of them. These private Core
        // assignments restore exact revisions and do not notify P12 or publish
        // an epoch, so the enclosing operation remains failure-atomic.
        if (npc != null)
        {
            npc.ActionSlot.RollbackSelectedDeathMutation(
                plan.ExpectedAction, plan.ExpectedPayload, plan.ExpectedNpcActionRevision);
            npc.Lifecycle.RollbackSelectedDeathMutation(
                plan.ExpectedNpcLifeState,
                plan.ExpectedNpcInjurySeverity,
                plan.ExpectedNpcLifeStateRevision);
        }
        PersonRuntimeOwnerBridge.RollbackSelectedDeathMutation(
            plan.Person, residenceBefore, plan.ExpectedPersonLifeResidenceRevision);
        if (plan.IsResident)
            population.RollbackSelectedDeathTransition(plan.PopulationTransition);
        return true;
    }

    internal static bool TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonDeathPlan plan,
        IDailyV1SelectedPersonDeathBoundary boundary,
        out string failure)
    {
        return TryExecute(
            graph,
            personStore,
            plan,
            DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            boundary,
            out failure);
    }

    internal static bool TryExecute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonDeathPlan plan,
        string operationContractId,
        IDailyV1SelectedPersonDeathBoundary boundary,
        out string failure)
    {
        failure = "RuntimeFaulted";
        if (graph == null || personStore == null || plan == null || boundary == null
            || (!string.Equals(
                    operationContractId,
                    DailyV1RuntimeProtocolOperationContracts.PersonDeath,
                    StringComparison.Ordinal)
                && !string.Equals(
                    operationContractId,
                    DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
                    StringComparison.Ordinal))
            || !ReferenceEquals(graph.PersonStore, personStore)
            || plan.Person?.PersonId == null
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person))
            return false;

        if (!boundary.TryValidatePersonDeathProjection(
                plan, out bool currentActionChanged, out failure)
            || (plan.MaterializedNpc == null && currentActionChanged)
            || (plan.MaterializedNpc != null
                && currentActionChanged != (plan.ExpectedAction != null)))
        {
            failure = "RuntimeFaulted";
            return false;
        }

        if (!TryCreateP12SectionIds(plan, out IReadOnlyList<string> sectionIds))
        {
            failure = "RuntimeFaulted";
            return false;
        }

        Func<bool> localRevisionCapacity = () =>
            PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(
                plan.Person, plan.IsResident ? 2 : 1)
            && (plan.MaterializedNpc == null
                || (plan.MaterializedNpc.Lifecycle != null
                    && plan.MaterializedNpc.Lifecycle.CanAdvanceRevision(1)
                    && (!currentActionChanged
                        || (plan.MaterializedNpc.ActionSlot != null
                            && plan.MaterializedNpc.ActionSlot.CanAdvanceRevision(1)))
                    && plan.MaterializedNpc.CanCommitPersonDeathAfterValidation(
                        NpcInjurySeverity.None, plan.ExpectedAction, plan.ExpectedPayload)))
            && (!plan.IsResident
                || PopulationOwnerBridge.CanAdvanceP12AggregateRevision(
                    plan.ResidenceCity?.Population));

        if (!boundary.TryBeginPersonDeathOperation(
                plan,
                currentActionChanged,
                operationContractId,
                sectionIds,
                localRevisionCapacity,
                out IDailyV1SelectedPersonDeathP12Scope scope))
        {
            failure = "RuntimeFaulted";
            return false;
        }

        using (scope)
        {
            if (!TryCommit(graph, personStore, plan, out bool rollbackSucceeded, out failure))
            {
                if (!rollbackSucceeded) boundary.FaultPersonDeathRuntime();
                scope?.Complete(false);
                return false;
            }

            bool projectionPublished = plan.MaterializedNpc == null;
            if (plan.MaterializedNpc != null)
            {
                try
                {
                    projectionPublished = boundary.TryPublishPersonDeathProjection(
                        plan, currentActionChanged);
                }
                catch
                {
                    projectionPublished = false;
                }
            }

            if (!projectionPublished)
            {
                bool rolledBack = TryRollback(graph, personStore, plan);
                if (!rolledBack) boundary.FaultPersonDeathRuntime();
                failure = rolledBack
                    ? "The Unity compatibility projection rejected the death; all Core owner writes were rolled back."
                    : "The Unity compatibility projection rejected the death and exact Core owner rollback failed.";
                scope?.Complete(false);
                return false;
            }

            boundary.AdvancePersonDeathWorldRevision();
            if (scope == null || !scope.Complete(true))
            {
                bool rolledBack = TryRollback(graph, personStore, plan);
                if (!rolledBack) boundary.FaultPersonDeathRuntime();
                bool aborted = scope?.Complete(false) ?? false;
                string commitFailure = boundary is DailyV1CoreSelectedPersonDeathBoundary coreBoundary
                    ? coreBoundary.LastPersonDeathCommitFailure
                    : null;
                failure = rolledBack && aborted
                    ? "The selected Person-death census commit was rejected"
                        + (string.IsNullOrWhiteSpace(commitFailure) ? string.Empty : " (" + commitFailure + ")")
                        + " and all attempted owner and projection writes were rolled back."
                    : "The selected Person-death census commit was rejected"
                        + (string.IsNullOrWhiteSpace(commitFailure) ? string.Empty : " (" + commitFailure + ")")
                        + " and exact rollback could not be confirmed.";
                return false;
            }

            failure = null;
            return true;
        }
    }

    internal static bool TryCreateP12SectionIds(
        DailyV1SelectedPersonDeathPlan plan,
        out IReadOnlyList<string> sectionIds)
    {
        sectionIds = null;
        if (plan?.Person?.PersonId == null)
            return false;

        List<string> ids = new List<string>
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(
                plan.Person.PersonId)
        };

        if (plan.MaterializedNpc != null)
        {
            if (string.IsNullOrWhiteSpace(plan.MaterializedNpc.RuntimeId))
                return false;
            ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix + plan.MaterializedNpc.RuntimeId);
            if (plan.ExpectedAction != null)
                ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + plan.MaterializedNpc.RuntimeId);
        }

        if (plan.IsResident)
        {
            if (string.IsNullOrWhiteSpace(plan.ResidenceCity?.RuntimeId)
                || plan.ResidenceCity.Population == null)
                return false;
            string cityKey;
            try
            {
                cityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(
                    plan.ResidenceCity.RuntimeId);
            }
            catch
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(cityKey))
                return false;
            ids.Add(DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                + cityKey);
        }

        sectionIds = ids.AsReadOnly();
        return true;
    }
}

/// <summary>
/// Immutable witness for one selected NPC-only resident death. It binds the
/// exact Core NPC and City owners and the population transition before P12
/// admission; the host participates only in mutation admission and notification.
/// </summary>
internal sealed class DailyV1SelectedNpcResidentDeathPlan
{
    internal DailyV1NpcRuntimeOwners Npc { get; }
    internal DailyV1CityRuntimeOwners City { get; }
    internal int NamedResidentsBefore { get; }
    internal long LifeStateRevision { get; }
    internal long ResidenceRevision { get; }
    internal long ActionRevision { get; }
    internal string ResidenceBefore { get; }
    internal DailyV1ActionDefinition ExpectedAction { get; }
    internal DailyV1NpcActionPayloadOwner ExpectedPayload { get; }
    internal long PopulationRevision { get; }
    internal int PopulationBefore { get; }
    internal SettlementPopulationTransition PopulationTransition { get; }
    internal bool CurrentActionChanged => ExpectedAction != null;

    internal DailyV1SelectedNpcResidentDeathPlan(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        int namedResidentsBefore,
        long lifeStateRevision,
        long residenceRevision,
        long actionRevision,
        string residenceBefore,
        DailyV1ActionDefinition expectedAction,
        DailyV1NpcActionPayloadOwner expectedPayload,
        long populationRevision,
        int populationBefore,
        SettlementPopulationTransition populationTransition)
    {
        Npc = npc ?? throw new ArgumentNullException(nameof(npc));
        City = city ?? throw new ArgumentNullException(nameof(city));
        NamedResidentsBefore = namedResidentsBefore;
        LifeStateRevision = lifeStateRevision;
        ResidenceRevision = residenceRevision;
        ActionRevision = actionRevision;
        ResidenceBefore = residenceBefore;
        ExpectedAction = expectedAction;
        ExpectedPayload = expectedPayload;
        PopulationRevision = populationRevision;
        PopulationBefore = populationBefore;
        PopulationTransition = populationTransition
            ?? throw new ArgumentNullException(nameof(populationTransition));
    }
}

/// <summary>
/// Core-owned selected NPC-only resident death transaction. The exact Core
/// lifecycle, residence, action-slot and City population owners are committed
/// together. The host supplies the existing P12 writer admission and notification.
/// </summary>
internal static class DailyV1SelectedNpcResidentDeathOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        PersonId persistedPersonId,
        out DailyV1SelectedNpcResidentDeathPlan plan,
        out string failure)
    {
        plan = null;
        failure = "Selected NPC resident death requires the exact Core owner graph.";
        if (graph == null || npc == null || city == null
            || !graph.ContainsExactNpcOwner(npc)
            || !graph.ContainsExactCityOwner(city)
            || graph.PersonStore == null
            || !ReferenceEquals(npc.Definitions, graph.Definitions)
            || !ReferenceEquals(city.Definitions, graph.Definitions)
            || npc.Lifecycle == null || npc.Residence == null || npc.ActionSlot == null
            || city.Population == null
            || !string.Equals(city.Population.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npc.RuntimeId, npc.Residence.OwnerRuntimeId, StringComparison.Ordinal))
            return false;

        if (persistedPersonId != null)
        {
            failure = graph.PersonStore.TryGet(persistedPersonId, out _)
                ? "NPC-only resident death cannot alias a PersonStore-backed PersonId."
                : "NPC-only resident death cannot carry a persisted PersonId without its PersonStore owner.";
            return false;
        }

        if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(npc.RuntimeId, out _))
        {
            failure = "A PersonStore-backed NPC must use the selected Person-death operation.";
            return false;
        }
        if (npc.Lifecycle.LifeState != NpcLifeState.Alive)
        {
            failure = "The selected NPC is not alive.";
            return false;
        }
        if (!string.Equals(npc.Residence.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
        {
            failure = "The selected NPC is not resident in the exact selected City.";
            return false;
        }

        SettlementPopulationRuntime population = city.Population;
        int namedResidents = 0;
        if (population.CurrentPopulation <= 0
            || !DailyV1SelectedResidentCensus.TryCountNamedResidents(
                graph, city.RuntimeId, out namedResidents)
            || namedResidents <= 0
            || namedResidents > population.CurrentPopulation)
        {
            failure = "The exact selected resident census is unavailable or inconsistent with the City aggregate.";
            return false;
        }

        if (!PopulationOwnerBridge.CanAdvanceP12AggregateRevision(population)
            || !npc.Lifecycle.CanAdvanceRevision(1)
            || !npc.Residence.CanAdvanceRevision(1))
        {
            failure = "A selected resident-death owner revision cannot advance.";
            return false;
        }

        DailyV1ActionDefinition expectedAction = npc.ActionSlot.CurrentAction;
        DailyV1NpcActionPayloadOwner expectedPayload = npc.ActionSlot.CurrentPayload;
        int actionRevisionIncrement = expectedAction == null ? 0 : 1;
        if (!npc.ActionSlot.CanAdvanceRevision(actionRevisionIncrement)
            || !npc.CanCommitResidentDeathAfterValidation(expectedAction, expectedPayload))
        {
            failure = "The selected NPC lifecycle or action owner cannot admit resident death.";
            return false;
        }

        if (!SettlementPopulationSystem.TryPropose(
                population,
                new PopulationChangeSet(0, 1, 0, 0),
                out SettlementPopulationTransition populationTransition,
                out PopulationTransitionFailure populationFailure))
        {
            failure = populationFailure.ToString();
            return false;
        }

        int namedResidentsAfter = namedResidents - 1;
        if (populationTransition.PopulationAfter != population.CurrentPopulation - 1
            || namedResidentsAfter < 0
            || namedResidentsAfter > populationTransition.PopulationAfter)
        {
            failure = "The resident-death population transition violates the exact selected resident floor.";
            return false;
        }

        plan = new DailyV1SelectedNpcResidentDeathPlan(
            npc,
            city,
            namedResidents,
            npc.Lifecycle.LifeStateRevision,
            npc.Residence.Revision,
            npc.ActionSlot.Revision,
            npc.Residence.SettlementRuntimeId,
            expectedAction,
            expectedPayload,
            population.Revision,
            population.CurrentPopulation,
            populationTransition);
        failure = null;
        return true;
    }

    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedNpcResidentDeathPlan plan,
        PersonId currentPersistedPersonId,
        Func<bool> npcMutationAdmission,
        Action npcMutationCommitted,
        out string failure)
    {
        failure = "Selected NPC resident-death expected state changed before Core commit.";
        if (graph == null || plan == null || npcMutationAdmission == null
            || !graph.ContainsExactNpcOwner(plan.Npc)
            || !graph.ContainsExactCityOwner(plan.City)
            || graph.PersonStore == null
            || currentPersistedPersonId != null
            || graph.PersonStore.TryGetByMaterializedNpcRuntimeId(plan.Npc.RuntimeId, out _)
            || !ReferenceEquals(plan.Npc.Definitions, graph.Definitions)
            || !ReferenceEquals(plan.City.Definitions, graph.Definitions))
            return false;

        DailyV1NpcRuntimeOwners npc = plan.Npc;
        DailyV1CityRuntimeOwners city = plan.City;
        SettlementPopulationRuntime population = city.Population;
        if (population == null
            || npc.Lifecycle == null || npc.Residence == null || npc.ActionSlot == null
            || npc.Lifecycle.LifeState != NpcLifeState.Alive
            || npc.Lifecycle.LifeStateRevision != plan.LifeStateRevision
            || !npc.Residence.Matches(plan.ResidenceBefore, plan.ResidenceRevision)
            || !string.Equals(plan.ResidenceBefore, city.RuntimeId, StringComparison.Ordinal)
            || npc.ActionSlot.Revision != plan.ActionRevision
            || !ReferenceEquals(npc.ActionSlot.CurrentAction, plan.ExpectedAction)
            || !ReferenceEquals(npc.ActionSlot.CurrentPayload, plan.ExpectedPayload)
            || !npc.CanCommitResidentDeathAfterValidation(plan.ExpectedAction, plan.ExpectedPayload)
            || population.Revision != plan.PopulationRevision
            || population.CurrentPopulation != plan.PopulationBefore
            || plan.PopulationBefore <= 0
            || plan.PopulationTransition == null
            || plan.PopulationTransition.ExpectedRevision != plan.PopulationRevision
            || plan.PopulationTransition.PopulationBefore != plan.PopulationBefore
            || plan.PopulationTransition.PopulationAfter != plan.PopulationBefore - 1
            || !PopulationOwnerBridge.CanAdvanceP12AggregateRevision(population)
            || !npc.Lifecycle.CanAdvanceRevision(1)
            || !npc.Residence.CanAdvanceRevision(1)
            || !npc.ActionSlot.CanAdvanceRevision(plan.CurrentActionChanged ? 1 : 0))
            return false;

        if (!DailyV1SelectedResidentCensus.TryCountNamedResidents(
                graph, city.RuntimeId, out int currentNamedResidents)
            || currentNamedResidents != plan.NamedResidentsBefore
            || currentNamedResidents <= 0
            || currentNamedResidents - 1 > plan.PopulationTransition.PopulationAfter)
            return false;

        bool CommitNpcOwners()
        {
            bool admitted;
            try { admitted = npcMutationAdmission(); }
            catch { admitted = false; }
            if (!admitted
                || !npc.TryCommitResidentDeathAfterValidation(
                    plan.ExpectedAction,
                    plan.ExpectedPayload,
                    out bool currentActionChanged)
                || currentActionChanged != plan.CurrentActionChanged
                || !npc.Residence.TrySetValue(null))
                return false;

            npc.Residence.NotifyMutationCommitted();
            return true;
        }

        if (!PopulationOwnerBridge.TryApplyWithResidenceCommit(
                population,
                plan.PopulationTransition,
                CommitNpcOwners,
                out PopulationTransitionFailure applyFailure))
        {
            failure = applyFailure.ToString();
            return false;
        }

        try { npcMutationCommitted?.Invoke(); }
        catch { }
        failure = null;
        return true;
    }
}

internal enum DailyV1SelectedResidenceMigrationFailure
{
    None = 0,
    InvalidNpc = 1,
    DeadNpc = 2,
    InvalidOrigin = 3,
    InvalidDestination = 4,
    SameSettlement = 5,
    ResidenceMismatch = 6,
    OriginUnderflow = 7,
    DestinationOverflow = 8,
    OriginStaleState = 9,
    DestinationStaleState = 10,
    OriginRevisionOverflow = 11,
    DestinationRevisionOverflow = 12,
    InvalidTransition = 13,
    AlreadyApplied = 14,
    RuntimeFaulted = 15
}

/// <summary>
/// Prepared selected-owner residence migration. The plan binds the exact Core
/// NPC, paired City population owners, and the authoritative Person or
/// non-Person residence owner before any member is written.
/// </summary>
internal sealed class DailyV1SelectedResidenceMigrationPlan
{
    internal DailyV1NpcRuntimeOwners Npc { get; }
    internal DailyV1CityRuntimeOwners Origin { get; }
    internal DailyV1CityRuntimeOwners Destination { get; }
    internal PersonRuntime Person { get; }
    internal DailyV1NpcResidenceOwner NpcResidence { get; }
    internal long OriginExpectedRevision { get; }
    internal long DestinationExpectedRevision { get; }
    internal int OriginPopulationBefore { get; }
    internal int DestinationPopulationBefore { get; }
    internal long ResidenceExpectedRevision { get; }
    internal string OriginRuntimeId => Origin.RuntimeId;
    internal string DestinationRuntimeId => Destination.RuntimeId;
    internal bool IsPersonBacked => Person != null;

    internal DailyV1SelectedResidenceMigrationPlan(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners origin,
        DailyV1CityRuntimeOwners destination,
        PersonRuntime person,
        DailyV1NpcResidenceOwner npcResidence,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int destinationPopulationBefore,
        long residenceExpectedRevision)
    {
        Npc = npc ?? throw new ArgumentNullException(nameof(npc));
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        Person = person;
        NpcResidence = npcResidence;
        OriginExpectedRevision = originExpectedRevision;
        DestinationExpectedRevision = destinationExpectedRevision;
        OriginPopulationBefore = originPopulationBefore;
        DestinationPopulationBefore = destinationPopulationBefore;
        ResidenceExpectedRevision = residenceExpectedRevision;
    }

    internal bool MatchesCommand(
        string npcRuntimeId,
        string originRuntimeId,
        string destinationRuntimeId,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int originPopulationAfter,
        int destinationPopulationBefore,
        int destinationPopulationAfter)
    {
        return string.Equals(Npc.RuntimeId, npcRuntimeId, StringComparison.Ordinal)
            && string.Equals(Origin.RuntimeId, originRuntimeId, StringComparison.Ordinal)
            && string.Equals(Destination.RuntimeId, destinationRuntimeId, StringComparison.Ordinal)
            && OriginExpectedRevision == originExpectedRevision
            && DestinationExpectedRevision == destinationExpectedRevision
            && OriginPopulationBefore == originPopulationBefore
            && OriginPopulationBefore > 0
            && OriginPopulationBefore - 1 == originPopulationAfter
            && DestinationPopulationBefore == destinationPopulationBefore
            && DestinationPopulationBefore < int.MaxValue
            && DestinationPopulationBefore + 1 == destinationPopulationAfter;
    }
}

/// <summary>
/// Core-owned selected residence migration. P12 admission remains at the
/// runtime boundary, while this operation owns the exact Core facts and
/// commits the paired City owners with their residence owner as one prepared
/// transaction.
/// </summary>
internal static class DailyV1SelectedResidenceMigrationOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners origin,
        DailyV1CityRuntimeOwners destination,
        out DailyV1SelectedResidenceMigrationPlan plan,
        out DailyV1SelectedResidenceMigrationFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedResidenceMigrationFailure.InvalidNpc;
        if (graph == null || npc == null || !graph.ContainsExactNpcOwner(npc))
            return false;
        if (npc.Lifecycle?.LifeState != NpcLifeState.Alive)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.DeadNpc;
            return false;
        }

        if (origin == null || !graph.ContainsExactCityOwner(origin)
            || origin.Population == null
            || !string.Equals(origin.Population.SettlementRuntimeId, origin.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedResidenceMigrationFailure.InvalidOrigin;
            return false;
        }
        if (destination == null || !graph.ContainsExactCityOwner(destination)
            || destination.Population == null
            || !string.Equals(destination.Population.SettlementRuntimeId, destination.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedResidenceMigrationFailure.InvalidDestination;
            return false;
        }
        if (ReferenceEquals(origin, destination)
            || string.Equals(origin.RuntimeId, destination.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedResidenceMigrationFailure.SameSettlement;
            return false;
        }

        PersonRuntime person = null;
        DailyV1NpcResidenceOwner npcResidence = null;
        long residenceRevision;
        string residenceRuntimeId;
        if (graph.PersonStore != null
            && graph.PersonStore.TryGetByMaterializedNpcRuntimeId(npc.RuntimeId, out PersonRuntime materializedPerson))
        {
            if (!string.Equals(materializedPerson.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            {
                failure = DailyV1SelectedResidenceMigrationFailure.RuntimeFaulted;
                return false;
            }
            person = materializedPerson;
            if (person.DeathAbsoluteDay.HasValue)
            {
                failure = DailyV1SelectedResidenceMigrationFailure.DeadNpc;
                return false;
            }
            residenceRuntimeId = person.ResidenceSettlementRuntimeId;
            residenceRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        }
        else
        {
            npcResidence = npc.Residence;
            if (npcResidence == null
                || !string.Equals(npcResidence.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            {
                failure = DailyV1SelectedResidenceMigrationFailure.RuntimeFaulted;
                return false;
            }
            residenceRuntimeId = npcResidence.SettlementRuntimeId;
            residenceRevision = npcResidence.Revision;
        }

        if (!string.Equals(residenceRuntimeId, origin.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedResidenceMigrationFailure.ResidenceMismatch;
            return false;
        }

        SettlementPopulationRuntime originPopulation = origin.Population;
        SettlementPopulationRuntime destinationPopulation = destination.Population;
        if (originPopulation.CurrentPopulation <= 0)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.OriginUnderflow;
            return false;
        }
        if (destinationPopulation.CurrentPopulation == int.MaxValue)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.DestinationOverflow;
            return false;
        }
        if (originPopulation.Revision == long.MaxValue)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.OriginRevisionOverflow;
            return false;
        }
        if (destinationPopulation.Revision == long.MaxValue)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.DestinationRevisionOverflow;
            return false;
        }

        plan = new DailyV1SelectedResidenceMigrationPlan(
            npc,
            origin,
            destination,
            person,
            npcResidence,
            originPopulation.Revision,
            destinationPopulation.Revision,
            originPopulation.CurrentPopulation,
            destinationPopulation.CurrentPopulation,
            residenceRevision);
        failure = DailyV1SelectedResidenceMigrationFailure.None;
        return true;
    }

    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedResidenceMigrationPlan plan,
        Func<bool> npcResidenceMutationAdmission,
        Action notifyNpcResidenceCommitted,
        out DailyV1SelectedResidenceMigrationFailure failure)
    {
        failure = DailyV1SelectedResidenceMigrationFailure.RuntimeFaulted;
        if (graph == null || plan == null
            || !graph.ContainsExactNpcOwner(plan.Npc)
            || !graph.ContainsExactCityOwner(plan.Origin)
            || !graph.ContainsExactCityOwner(plan.Destination)
            || plan.Npc.Lifecycle?.LifeState != NpcLifeState.Alive
            || !ReferenceEquals(plan.Npc.Definitions, plan.Origin.Definitions)
            || !ReferenceEquals(plan.Npc.Definitions, plan.Destination.Definitions))
            return false;

        SettlementPopulationRuntime originPopulation = plan.Origin.Population;
        SettlementPopulationRuntime destinationPopulation = plan.Destination.Population;
        if (originPopulation == null || destinationPopulation == null)
            return false;
        if (originPopulation.Revision != plan.OriginExpectedRevision
            || originPopulation.CurrentPopulation != plan.OriginPopulationBefore)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.OriginStaleState;
            return false;
        }
        if (destinationPopulation.Revision != plan.DestinationExpectedRevision
            || destinationPopulation.CurrentPopulation != plan.DestinationPopulationBefore)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.DestinationStaleState;
            return false;
        }
        if (!PopulationOwnerBridge.CanAdvanceP12AggregateRevision(originPopulation))
        {
            failure = DailyV1SelectedResidenceMigrationFailure.OriginRevisionOverflow;
            return false;
        }
        if (!PopulationOwnerBridge.CanAdvanceP12AggregateRevision(destinationPopulation))
        {
            failure = DailyV1SelectedResidenceMigrationFailure.DestinationRevisionOverflow;
            return false;
        }

        if (plan.Person != null)
        {
            if (graph.PersonStore == null
                || !graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    plan.Npc.RuntimeId, out PersonRuntime registeredPerson)
                || !ReferenceEquals(registeredPerson, plan.Person)
                || plan.Person.DeathAbsoluteDay.HasValue
                || !string.Equals(plan.Person.MaterializedNpcRuntimeId,
                    plan.Npc.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(plan.Person.ResidenceSettlementRuntimeId,
                    plan.Origin.RuntimeId, StringComparison.Ordinal)
                || PersonRuntimeOwnerBridge.GetLifeResidenceRevision(plan.Person)
                    != plan.ResidenceExpectedRevision
                || !PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(plan.Person, 1)
                || !PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(plan.Person))
            {
                failure = DailyV1SelectedResidenceMigrationFailure.RuntimeFaulted;
                return false;
            }
        }
        else if (plan.NpcResidence == null
            || !ReferenceEquals(plan.NpcResidence, plan.Npc.Residence)
            || !plan.NpcResidence.Matches(plan.Origin.RuntimeId, plan.ResidenceExpectedRevision)
            || !plan.NpcResidence.CanAdvanceRevision(1)
            || npcResidenceMutationAdmission == null)
        {
            failure = DailyV1SelectedResidenceMigrationFailure.RuntimeFaulted;
            return false;
        }

        bool CommitResidence()
        {
            if (plan.Person != null)
                return PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
                    plan.Person, plan.Destination.RuntimeId);

            bool admitted;
            try { admitted = npcResidenceMutationAdmission(); }
            catch { admitted = false; }
            if (!admitted
                || !plan.NpcResidence.TrySetValue(plan.Destination.RuntimeId))
                return false;
            plan.NpcResidence.NotifyMutationCommitted();
            try { notifyNpcResidenceCommitted?.Invoke(); }
            catch { /* The host notification is not an authoritative write. */ }
            return true;
        }

        if (!PopulationOwnerBridge.TryApplyPairedMigrationWithResidenceCommit(
                originPopulation,
                destinationPopulation,
                plan.OriginExpectedRevision,
                plan.DestinationExpectedRevision,
                plan.OriginPopulationBefore,
                plan.DestinationPopulationBefore,
                CommitResidence,
                out PopulationTransitionFailure populationFailure))
        {
            failure = populationFailure switch
            {
                PopulationTransitionFailure.StaleState
                    when originPopulation.Revision != plan.OriginExpectedRevision
                        || originPopulation.CurrentPopulation != plan.OriginPopulationBefore
                    => DailyV1SelectedResidenceMigrationFailure.OriginStaleState,
                PopulationTransitionFailure.StaleState
                    => DailyV1SelectedResidenceMigrationFailure.DestinationStaleState,
                PopulationTransitionFailure.WouldUnderflow
                    => DailyV1SelectedResidenceMigrationFailure.OriginUnderflow,
                PopulationTransitionFailure.WouldOverflow
                    => DailyV1SelectedResidenceMigrationFailure.DestinationOverflow,
                PopulationTransitionFailure.RevisionOverflow
                    when originPopulation.Revision == long.MaxValue
                    => DailyV1SelectedResidenceMigrationFailure.OriginRevisionOverflow,
                PopulationTransitionFailure.RevisionOverflow
                    => DailyV1SelectedResidenceMigrationFailure.DestinationRevisionOverflow,
                _ => DailyV1SelectedResidenceMigrationFailure.RuntimeFaulted
            };
            return false;
        }

        failure = DailyV1SelectedResidenceMigrationFailure.None;
        return true;
    }
}

internal sealed class DailyV1SelectedDemographyDiagnostic
{
    internal bool IsError { get; }
    internal string Code { get; }
    internal string Identity { get; }
    internal string Message { get; }

    internal DailyV1SelectedDemographyDiagnostic(bool isError, string code, string identity, string message)
    {
        IsError = isError;
        Code = code ?? string.Empty;
        Identity = identity ?? string.Empty;
        Message = message ?? string.Empty;
    }
}

internal sealed class DailyV1SelectedDemographyResult
{
    private readonly List<DailyV1SelectedDemographyDiagnostic> diagnostics =
        new List<DailyV1SelectedDemographyDiagnostic>();

    internal long AbsoluteDay { get; }
    internal int NamedPersonsEvaluated { get; set; }
    internal int NamedDeathsApplied { get; set; }
    internal int AggregateSettlementsProcessed { get; set; }
    internal int AggregateBirthsApplied { get; set; }
    internal int AggregateDeathsApplied { get; set; }
    internal IReadOnlyList<DailyV1SelectedDemographyDiagnostic> Diagnostics => diagnostics.AsReadOnly();

    internal DailyV1SelectedDemographyResult(long absoluteDay)
    {
        AbsoluteDay = absoluteDay;
    }

    internal void Add(bool isError, string code, string identity, string message)
    {
        diagnostics.Add(new DailyV1SelectedDemographyDiagnostic(isError, code, identity, message));
    }
}

/// <summary>
/// Owns the selected Daily-v1 demographic ordering, Person-death proposals and
/// Core owner writes. The host supplies only the existing P12 census admission
/// boundary and represented-resident facts for unselected compatibility NPCs.
/// </summary>
internal static class DailyV1SelectedDemographyStage
{
    internal static DailyV1SelectedDemographyResult Advance(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        long absoluteDay,
        SimulationCalendar calendar,
        EffectiveNaturalMortalityConfiguration mortalityConfiguration,
        EffectiveAggregateDemographyConfiguration aggregateConfiguration,
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        IAggregateDemographyProvider aggregateProvider,
        IDailyV1SelectedDemographyBoundary boundary)
    {
        if (graph == null || personStore == null || calendar == null || boundary == null)
            throw new ArgumentNullException("Selected demography requires its exact owner graph, people, calendar and host boundary.");
        if (graph.PersonStore != null && !ReferenceEquals(graph.PersonStore, personStore))
            throw new ArgumentException("Selected demography must use the PersonStore already bound to its owner graph.", nameof(personStore));
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));

        DailyV1SelectedDemographyResult result = new DailyV1SelectedDemographyResult(absoluteDay);
        if (mortalityConfiguration != null && mortalityConfiguration.Enabled)
            ApplyNaturalMortality(graph, personStore, absoluteDay, calendar,
                mortalityConfiguration, mortalitySamples, boundary, result);

        if (aggregateConfiguration != null && aggregateConfiguration.Enabled)
        {
            IAggregateDemographyProvider provider = aggregateProvider
                ?? new DeterministicAggregateDemographyProvider(
                    calendar,
                    aggregateConfiguration.AnnualBirthRate,
                    aggregateConfiguration.AnnualDeathRate);
            List<string> selectedNpcRuntimeIds = BuildSelectedNpcRuntimeIds(graph);
            IReadOnlyDictionary<string, int> floors = BuildRepresentedResidentFloors(
                graph,
                personStore,
                boundary.BuildCompatibilityResidentFloorSupplement(selectedNpcRuntimeIds));
            ApplyAggregateDemography(graph, provider, floors, absoluteDay, result);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, int> BuildRepresentedResidentFloors(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        IReadOnlyDictionary<string, int> residentFloorSupplement)
    {
        Dictionary<string, int> personResidentFloors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city != null && !string.IsNullOrWhiteSpace(city.RuntimeId))
                personResidentFloors[city.RuntimeId] = 0;
        }

        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null || person.PersonId == null || person.DeathAbsoluteDay.HasValue
                || string.IsNullOrWhiteSpace(person.ResidenceSettlementRuntimeId)
                || !personResidentFloors.TryGetValue(person.ResidenceSettlementRuntimeId, out int count))
                continue;

            if (count < int.MaxValue)
                personResidentFloors[person.ResidenceSettlementRuntimeId] = count + 1;
        }

        Dictionary<string, int> selectedLegacyResidentFloors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city != null && !string.IsNullOrWhiteSpace(city.RuntimeId))
                selectedLegacyResidentFloors[city.RuntimeId] = 0;
        }

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.Lifecycle == null || npc.Residence == null)
                throw new InvalidOperationException(
                    "Selected demography requires the exact Core NPC lifecycle and residence owners.");

            if (personStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime person))
            {
                if (person == null || person.PersonId == null || !person.IsMaterialized
                    || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Selected demography found an inconsistent Core PersonStore materialization binding.");

                // Person-backed NPC residence is authoritative in PersonStore
                // and is already included in personResidentFloors.
                continue;
            }

            string settlementRuntimeId = npc.Residence.SettlementRuntimeId;
            if (npc.Lifecycle.LifeState != NpcLifeState.Alive
                || string.IsNullOrWhiteSpace(settlementRuntimeId)
                || !selectedLegacyResidentFloors.TryGetValue(
                    settlementRuntimeId, out int selectedResidents))
                continue;

            if (selectedResidents == int.MaxValue)
                throw new InvalidOperationException(
                    "Selected demography exceeded the representable legacy resident count.");
            selectedLegacyResidentFloors[settlementRuntimeId] = selectedResidents + 1;
        }

        Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || residentFloorSupplement == null
                || !personResidentFloors.TryGetValue(city.RuntimeId, out int personFloor)
                || !selectedLegacyResidentFloors.TryGetValue(city.RuntimeId, out int selectedLegacyFloor)
                || !residentFloorSupplement.TryGetValue(city.RuntimeId, out int supplement)
                || personFloor < 0 || selectedLegacyFloor < 0 || supplement < 0
                || personFloor > int.MaxValue - selectedLegacyFloor
                || personFloor + selectedLegacyFloor > int.MaxValue - supplement)
                continue;

            floors[city.RuntimeId] = personFloor + selectedLegacyFloor + supplement;
        }

        return floors;
    }

    private static List<string> BuildSelectedNpcRuntimeIds(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph?.Npcs == null)
            throw new ArgumentNullException(nameof(graph));

        List<string> runtimeIds = new List<string>(graph.Npcs.Count);
        HashSet<string> uniqueRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !uniqueRuntimeIds.Add(npc.RuntimeId))
                throw new InvalidOperationException(
                    "Selected demography requires unique exact Core NPC runtime identities.");
            runtimeIds.Add(npc.RuntimeId);
        }

        runtimeIds.Sort(StringComparer.Ordinal);
        return runtimeIds;
    }

    private static void ApplyNaturalMortality(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        long absoluteDay,
        SimulationCalendar calendar,
        EffectiveNaturalMortalityConfiguration configuration,
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        IDailyV1SelectedDemographyBoundary boundary,
        DailyV1SelectedDemographyResult result)
    {
        IPersonNaturalMortalitySampleProvider samples = mortalitySamples
            ?? new DeterministicDemographicSampleProvider();
        List<PersonRuntime> snapshot = new List<PersonRuntime>(personStore.Persons);
        snapshot.RemoveAll(person => person == null || person.PersonId == null);
        snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.PersonId.Value,
            right.PersonId.Value));

        foreach (PersonRuntime person in snapshot)
        {
            if (person.DeathAbsoluteDay.HasValue) continue;
            result.NamedPersonsEvaluated++;

            double sample;
            try
            {
                sample = samples.GetSample(person.PersonId, absoluteDay);
            }
            catch (Exception exception)
            {
                result.Add(true, "NaturalMortalitySampleFailed", person.PersonId.Value, exception.Message);
                continue;
            }

            if (!PersonNaturalMortalityQuery.TryEvaluate(
                    person,
                    absoluteDay,
                    calendar,
                    configuration.AnnualProbability,
                    sample,
                    out PersonNaturalMortalityEvaluation evaluation,
                    out PersonNaturalMortalityQueryFailure evaluationFailure))
            {
                result.Add(
                    evaluationFailure != PersonNaturalMortalityQueryFailure.BirthDateUnknown,
                    "NaturalMortalityEvaluationRejected",
                    person.PersonId.Value,
                    evaluationFailure.ToString());
                continue;
            }

            if (!evaluation.ShouldDie) continue;
            if (!DailyV1SelectedPersonDeathOperation.TryPrepare(
                    graph, personStore, person, absoluteDay,
                    out DailyV1SelectedPersonDeathPlan plan,
                    out string prepareFailure))
            {
                result.Add(true, "NaturalMortalityApplyRejected", person.PersonId.Value,
                    prepareFailure ?? string.Empty);
                continue;
            }

            if (!DailyV1SelectedPersonDeathOperation.TryExecute(
                    graph, personStore, plan, boundary, out string deathFailure))
            {
                result.Add(true, "NaturalMortalityApplyRejected", person.PersonId.Value,
                    deathFailure ?? string.Empty);
                continue;
            }

            result.NamedDeathsApplied++;
        }
    }

    private static void ApplyAggregateDemography(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IAggregateDemographyProvider provider,
        IReadOnlyDictionary<string, int> floors,
        long absoluteDay,
        DailyV1SelectedDemographyResult result)
    {
        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>(graph.Cities);
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId) || city.Population == null)
            {
                result.Add(true, "AggregatePopulationOwnerMissing", city?.RuntimeId ?? string.Empty,
                    "The selected owner graph did not provide its exact aggregate population owner.");
                continue;
            }

            if (floors == null || !floors.TryGetValue(city.RuntimeId, out int representedResidentFloor))
            {
                result.Add(true, "RepresentedResidentFloorMissing", city.RuntimeId,
                    "The world did not provide a represented-resident floor.");
                continue;
            }

            try
            {
                if (!AggregateDemographySystem.TryPropose(
                        city.Population,
                        representedResidentFloor,
                        provider,
                        absoluteDay,
                        out AggregateDemographyTransition transition,
                        out AggregateDemographyFailure proposalFailure))
                {
                    result.Add(true, "AggregateDemographyProposalRejected", city.RuntimeId,
                        proposalFailure.ToString());
                    continue;
                }

                if (!AggregateDemographySystem.TryApply(
                        city.Population,
                        representedResidentFloor,
                        transition,
                        out AggregateDemographyFailure applyFailure))
                {
                    result.Add(true, "AggregateDemographyApplyRejected", city.RuntimeId,
                        applyFailure.ToString());
                    continue;
                }

                result.AggregateSettlementsProcessed++;
                result.AggregateBirthsApplied += transition.Births;
                result.AggregateDeathsApplied += transition.Deaths;
            }
            catch (Exception exception)
            {
                result.Add(true, "AggregateDemographyProviderFailed", city.RuntimeId, exception.Message);
            }
        }
    }
}
