using System;
using System.Threading;

/// <summary>
/// Live runtime facts used to bind the FR-B seam. SimulationRuntime supplies
/// its private adapter during the later serialized integration.
/// </summary>
internal interface IFactualReadRuntimeState
{
    SimulationRuntimeAdmissionContext AdmissionContext { get; }
    bool IsWorldPublished { get; }
    bool IsHealthy { get; }
    bool IsBootstrapOrAdvanceActive { get; }
    bool TryReadCompletedLogicalBoundary(out long logicalBoundary);
}

internal struct FactualReadBoundary
{
    internal FactualReadBoundary(long logicalBoundary, long factionStoreRevision, long personStoreRevision)
    {
        LogicalBoundary = logicalBoundary;
        FactionStoreRevision = factionStoreRevision;
        PersonStoreRevision = personStoreRevision;
    }

    internal long LogicalBoundary { get; }
    internal long FactionStoreRevision { get; }
    internal long PersonStoreRevision { get; }

    internal bool Matches(FactualReadBoundary other)
    {
        return LogicalBoundary == other.LogicalBoundary
            && FactionStoreRevision == other.FactionStoreRevision
            && PersonStoreRevision == other.PersonStoreRevision;
    }
}

internal sealed class FactualReadReadLease : IDisposable
{
    private FactualReadAdmission owner;

    internal FactualReadReadLease(FactualReadAdmission owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Dispose()
    {
        FactualReadAdmission current = owner;
        if (current == null)
            return;
        current.EndRead();
        owner = null;
    }
}

/// <summary>
/// One non-reentrant owner-thread read admission bound to the exact composed
/// FactionStore and PersonStore. It does not represent a global P12 epoch.
/// </summary>
internal sealed class FactualReadAdmission
{
    private readonly IFactualReadRuntimeState runtimeState;
    private readonly SimulationRuntimeAdmissionContext admissionContext;
    private readonly Thread ownerThread;
    private readonly int ownerThreadId;
    private FactionStore factionStore;
    private PersonStore personStore;
    private bool storesBound;
    private bool readActive;

    internal FactualReadAdmission(IFactualReadRuntimeState runtimeState)
    {
        this.runtimeState = runtimeState;
        admissionContext = runtimeState?.AdmissionContext;
        ownerThread = admissionContext?.ExpectedOwnerThread;
        ownerThreadId = admissionContext?.ExpectedOwnerThreadId ?? 0;
    }

    internal bool TryBindStores(FactionStore factionStore, PersonStore personStore)
    {
        if (factionStore == null || personStore == null
            || admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !IsOwnerThreadCurrent()
            || storesBound
            || !factionStore.IsBoundToPersonStore(personStore)
            || !factionStore.CanBindFactualReadAdmission(this)
            || !personStore.CanBindFactualReadAdmission(this))
        {
            return false;
        }

        this.factionStore = factionStore;
        this.personStore = personStore;
        if (!factionStore.TryBindFactualReadAdmission(this))
        {
            ClearStoreBindingReferences();
            return false;
        }

        if (!personStore.TryBindFactualReadAdmission(this))
        {
            factionStore.TryUnbindFactualReadAdmission(this);
            ClearStoreBindingReferences();
            return false;
        }

        storesBound = true;
        return true;
    }

    internal bool TryBeginRead(out FactualReadReadLease lease)
    {
        lease = null;
        if (!CanStartRead() || readActive)
            return false;

        readActive = true;
        if (!CanStartRead())
        {
            readActive = false;
            return false;
        }

        lease = new FactualReadReadLease(this);
        return true;
    }

    internal bool TryReadCurrentBoundary(out FactualReadBoundary boundary)
    {
        boundary = default(FactualReadBoundary);
        if (!storesBound || !readActive || !IsOwnerThreadCurrent()
            || runtimeState == null || admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !runtimeState.IsWorldPublished
            || !runtimeState.IsHealthy
            || runtimeState.IsBootstrapOrAdvanceActive
            || !runtimeState.TryReadCompletedLogicalBoundary(out long logicalBoundary)
            || logicalBoundary < 0L)
        {
            return false;
        }

        long factionRevision = factionStore.Revision;
        long personRevision = personStore.Revision;
        if (factionRevision < 0L || personRevision < 0L)
            return false;

        boundary = new FactualReadBoundary(logicalBoundary, factionRevision, personRevision);
        return true;
    }

    internal bool CanMutateFactionStore(FactionStore store)
    {
        return storesBound && ReferenceEquals(store, factionStore)
            && IsOwnerThreadCurrent() && !readActive;
    }

    internal bool CanMutatePersonStore(PersonStore store)
    {
        return storesBound && ReferenceEquals(store, personStore)
            && IsOwnerThreadCurrent() && !readActive;
    }

    internal void EndRead()
    {
        if (!IsOwnerThreadCurrent())
            throw new InvalidOperationException("A factual read lease must be released by its owner thread.");
        if (!readActive)
            throw new InvalidOperationException("The factual read admission is not active.");
        readActive = false;
    }

    private bool CanStartRead()
    {
        return storesBound
            && runtimeState != null
            && admissionContext != null
            && admissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && IsOwnerThreadCurrent()
            && runtimeState.IsWorldPublished
            && runtimeState.IsHealthy
            && !runtimeState.IsBootstrapOrAdvanceActive;
    }

    private bool IsOwnerThreadCurrent()
    {
        return ownerThread != null
            && ReferenceEquals(ownerThread, Thread.CurrentThread)
            && ownerThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private void ClearStoreBindingReferences()
    {
        factionStore = null;
        personStore = null;
        storesBound = false;
    }
}
