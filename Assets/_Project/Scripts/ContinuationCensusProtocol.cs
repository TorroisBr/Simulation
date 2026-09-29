using System;
using System.Collections.Generic;
using System.Threading;

public enum OwnerSectionRole
{
    Required,
    ExplicitlyEmpty,
    Excluded
}

public enum ContinuationCensusFailure
{
    None,
    OwnerCoverageIncomplete,
    OwnerThreadUnbound,
    WrongOwnerThread,
    OperationInProgress,
    ProtocolFaulted
}

/// <summary>A versioned logical owner section expected by a census protocol.</summary>
public sealed class OwnerSectionContract
{
    public OwnerSectionContract(string sectionId, int schemaVersion, OwnerSectionRole role)
    {
        if (string.IsNullOrWhiteSpace(sectionId)) throw new ArgumentException("Section identity is required.", nameof(sectionId));
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        if (!Enum.IsDefined(typeof(OwnerSectionRole), role)) throw new ArgumentOutOfRangeException(nameof(role));

        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        Role = role;
    }

    public string SectionId { get; }
    public int SchemaVersion { get; }
    public OwnerSectionRole Role { get; }
}

/// <summary>Reads a live witness from one concrete owner instance.</summary>
public interface IOwnerSectionCensusProvider
{
    OwnerSectionCensusWitness GetCurrentCensus();
}

/// <summary>
/// A non-admitting P12-B protocol kernel for owner-section census and operation
/// accounting. It never issues a capture token and is not wired to a runtime.
/// Its scopes account only for operation IDs explicitly registered by a caller;
/// they are not locks and do not make owner stores thread-safe.
/// </summary>
public sealed class ContinuationCensusProtocol
{
    private sealed class OwnerThreadBinding
    {
        public readonly Thread Thread;
        public readonly int ManagedThreadId;

        public OwnerThreadBinding(Thread thread)
        {
            Thread = thread ?? throw new ArgumentNullException(nameof(thread));
            ManagedThreadId = thread.ManagedThreadId;
        }
    }

    private sealed class RegisteredSection
    {
        public readonly OwnerSectionContract Contract;
        public readonly IOwnerSectionCensusProvider Provider;
        public object OwnerInstanceIdentity;
        public int LastCardinality;
        public long LastRevision;
        public bool HasBaseline;

        public RegisteredSection(OwnerSectionContract contract, IOwnerSectionCensusProvider provider)
        {
            Contract = contract;
            Provider = provider;
        }
    }

    private readonly Dictionary<string, OwnerSectionContract> expectedSections =
        new Dictionary<string, OwnerSectionContract>(StringComparer.Ordinal);
    private readonly Dictionary<string, RegisteredSection> registeredSections =
        new Dictionary<string, RegisteredSection>(StringComparer.Ordinal);
    private readonly HashSet<string> expectedOperations = new HashSet<string>(StringComparer.Ordinal);

    private bool expectedSectionsSealed;
    private bool providersSealed;
    private bool operationsSealed;
    private OwnerThreadBinding ownerThreadBinding;
    private int activeOperationCount;
    private long mutationEpoch;
    private int protocolFaulted;

    /// <summary>
    /// Declares one logical section requirement. This is generic protocol input;
    /// callers must not seal a selected-profile inventory until that inventory
    /// has been independently established from actual composition evidence.
    /// </summary>
    public bool RegisterExpectedSection(OwnerSectionContract contract, out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (IsOwnerThreadBound())
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (contract == null || expectedSectionsSealed)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (expectedSections.ContainsKey(contract.SectionId))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        expectedSections.Add(contract.SectionId, contract);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public bool SealExpectedSectionInventory(out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (IsOwnerThreadBound())
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (expectedSectionsSealed || expectedSections.Count == 0)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        expectedSectionsSealed = true;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>Registers an owner-backed census provider for an expected section.</summary>
    public bool RegisterCensusProvider(
        string sectionId,
        IOwnerSectionCensusProvider provider,
        out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (IsOwnerThreadBound())
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (provider == null || providersSealed || string.IsNullOrWhiteSpace(sectionId))
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (!expectedSections.ContainsKey(sectionId) || registeredSections.ContainsKey(sectionId))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        registeredSections.Add(sectionId,
            new RegisteredSection(expectedSections[sectionId], provider));
        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>
    /// Closes provider registration. Missing or extra providers remain a
    /// fail-closed coverage result; sealing does not assert completeness.
    /// </summary>
    public bool SealCensusProviderInventory(out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (IsOwnerThreadBound())
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (providersSealed || !expectedSectionsSealed)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        providersSealed = true;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>Declares one supported synchronous operation contract.</summary>
    public bool RegisterExpectedOperation(string operationContractId, out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (IsOwnerThreadBound())
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (string.IsNullOrWhiteSpace(operationContractId) || operationsSealed)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (!expectedOperations.Add(operationContractId))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    public bool SealOperationInventory(out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (IsOwnerThreadBound())
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (operationsSealed || expectedOperations.Count == 0)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        operationsSealed = true;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>Binds once to the current managed thread after bootstrap completes.</summary>
    public bool BindOwnerThread(out ContinuationCensusFailure failure)
    {
        failure = ContinuationCensusFailure.None;
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        if (!expectedSectionsSealed || !providersSealed || !operationsSealed
            || expectedSections.Count == 0 || expectedOperations.Count == 0)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        OwnerThreadBinding binding = new OwnerThreadBinding(Thread.CurrentThread);
        if (Interlocked.CompareExchange(ref ownerThreadBinding, binding, null) != null)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Assesses only the registered owner-section census at this instant.
    /// Success is not profile admission, capture eligibility, or proof that an
    /// external runtime has exhaustive owner or mutation coverage.
    /// </summary>
    public bool TryAssessOwnerSectionInventory(out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        if (!expectedSectionsSealed || !providersSealed || expectedSections.Count == 0
            || registeredSections.Count != expectedSections.Count)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!TryRequireOwnerThread(out failure)) return false;
        if (activeOperationCount != 0)
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }

        foreach (KeyValuePair<string, OwnerSectionContract> pair in expectedSections)
        {
            if (!registeredSections.TryGetValue(pair.Key, out RegisteredSection section))
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            if (!TryReadAndValidate(section, allowRevisionAdvance: false, out _, out failure))
            {
                Fault();
                return false;
            }
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>
    /// Records one successful authoritative commit notification. The owning
    /// code calls this only after its complete commit; it does not perform or
    /// infer a mutation. Unknown sections and unusable witnesses fail closed.
    /// </summary>
    public bool NotifyCommittedMutation(string sectionId, out ContinuationCensusFailure failure)
    {
        if (!TryRequireOwnerThread(out failure)) return false;
        if (mutationEpoch == long.MaxValue)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        mutationEpoch++;
        if (string.IsNullOrWhiteSpace(sectionId)
            || !registeredSections.TryGetValue(sectionId, out RegisteredSection section))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        bool hadBaseline = section.HasBaseline;
        long priorRevision = section.LastRevision;
        if (!TryReadAndValidate(section, allowRevisionAdvance: true, out OwnerSectionCensusWitness witness, out failure))
        {
            Fault();
            return false;
        }

        if (hadBaseline && witness.Revision == priorRevision)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        SetBaseline(section, witness);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public bool TryReadMutationEpoch(out long epoch, out ContinuationCensusFailure failure)
    {
        epoch = 0L;
        if (!TryRequireOwnerThread(out failure)) return false;
        epoch = mutationEpoch;
        return true;
    }

    /// <summary>Enters a registered synchronous operation on the bound owner thread.</summary>
    public bool TryEnterOperation(
        string operationContractId,
        out SimulationOperationScope scope,
        out ContinuationCensusFailure failure)
    {
        scope = null;
        if (!TryRequireOwnerThread(out failure)) return false;
        if (!operationsSealed)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!expectedOperations.Contains(operationContractId ?? string.Empty))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (activeOperationCount == int.MaxValue)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        activeOperationCount++;
        OwnerThreadBinding binding = Volatile.Read(ref ownerThreadBinding);
        scope = new SimulationOperationScope(this, binding.Thread, binding.ManagedThreadId);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public bool TryReadActiveOperationCount(out int count, out ContinuationCensusFailure failure)
    {
        count = 0;
        if (!operationsSealed || expectedOperations.Count == 0)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (!TryRequireOwnerThread(out failure)) return false;
        count = activeOperationCount;
        return true;
    }

    /// <summary>
    /// Reports whether the explicitly registered operation tracker is idle.
    /// This is not proof that an external runtime has registered every possible
    /// operation or that its stores are safe for concurrent access.
    /// </summary>
    public bool TryAssessRegisteredOperationQuiescence(out ContinuationCensusFailure failure)
    {
        if (!operationsSealed || expectedOperations.Count == 0)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (!TryRequireOwnerThread(out failure)) return false;

        if (activeOperationCount != 0)
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    internal void ExitOperation(Thread scopeOwnerThread, int scopeOwnerThreadId)
    {
        if (!TryRequireOwnerThread(out _)
            || !ReferenceEquals(scopeOwnerThread, Thread.CurrentThread)
            || scopeOwnerThreadId != Thread.CurrentThread.ManagedThreadId
            || activeOperationCount <= 0)
        {
            Fault();
            return;
        }

        activeOperationCount--;
    }

    private bool TryReadAndValidate(
        RegisteredSection section,
        bool allowRevisionAdvance,
        out OwnerSectionCensusWitness witness,
        out ContinuationCensusFailure failure)
    {
        witness = null;
        try
        {
            witness = section.Provider.GetCurrentCensus();
        }
        catch
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (witness == null
            || !string.Equals(witness.SectionId, section.Contract.SectionId, StringComparison.Ordinal)
            || witness.SchemaVersion != section.Contract.SchemaVersion
            || witness.OwnerInstanceIdentity == null
            || witness.Cardinality < 0
            || witness.Revision < 0L
            || ((section.Contract.Role == OwnerSectionRole.ExplicitlyEmpty
                    || section.Contract.Role == OwnerSectionRole.Excluded)
                && witness.Cardinality != 0))
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (section.HasBaseline)
        {
            if (!ReferenceEquals(section.OwnerInstanceIdentity, witness.OwnerInstanceIdentity))
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            if (witness.Revision < section.LastRevision)
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            bool changed = witness.Revision != section.LastRevision
                || witness.Cardinality != section.LastCardinality;
            if (changed && !allowRevisionAdvance)
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
        }
        else
        {
            SetBaseline(section, witness);
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private static void SetBaseline(RegisteredSection section, OwnerSectionCensusWitness witness)
    {
        section.OwnerInstanceIdentity = witness.OwnerInstanceIdentity;
        section.LastCardinality = witness.Cardinality;
        section.LastRevision = witness.Revision;
        section.HasBaseline = true;
    }

    private bool TryRequireOwnerThread(out ContinuationCensusFailure failure)
    {
        if (IsFaulted())
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        OwnerThreadBinding binding = Volatile.Read(ref ownerThreadBinding);
        if (binding == null)
        {
            failure = ContinuationCensusFailure.OwnerThreadUnbound;
            return false;
        }

        if (!ReferenceEquals(Thread.CurrentThread, binding.Thread)
            || Thread.CurrentThread.ManagedThreadId != binding.ManagedThreadId)
        {
            Fault();
            failure = ContinuationCensusFailure.WrongOwnerThread;
            return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool IsFaulted() => Volatile.Read(ref protocolFaulted) != 0;

    private bool IsOwnerThreadBound() => Volatile.Read(ref ownerThreadBinding) != null;

    private void Fault() => Interlocked.Exchange(ref protocolFaulted, 1);
}

/// <summary>
/// Idempotent accounting handle for a registered synchronous operation.
/// Disposal off the bound owner thread faults the protocol and leaves the
/// active count positive, so it cannot appear quiescent afterward.
/// </summary>
public sealed class SimulationOperationScope : IDisposable
{
    private readonly ContinuationCensusProtocol owner;
    private readonly Thread ownerThread;
    private readonly int ownerThreadId;
    private int disposed;

    internal SimulationOperationScope(ContinuationCensusProtocol owner, Thread ownerThread, int ownerThreadId)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.ownerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
        this.ownerThreadId = ownerThreadId;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        owner.ExitOperation(ownerThread, ownerThreadId);
    }
}
