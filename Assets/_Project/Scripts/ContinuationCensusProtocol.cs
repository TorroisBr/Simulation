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

internal sealed class ContinuationMutationEpochReservation
{
    internal readonly ContinuationCensusProtocol Owner;
    internal readonly object Token;
    internal readonly Thread OwnerThread;

    internal ContinuationMutationEpochReservation(
        ContinuationCensusProtocol owner,
        object token,
        Thread ownerThread)
    {
        Owner = owner;
        Token = token;
        OwnerThread = ownerThread;
    }
}

/// <summary>
/// A non-admitting P12-B protocol kernel for owner-section census and operation
/// accounting. It never issues a capture token and is not wired to a runtime.
/// Setup registration is serialized before binding; bound calls are serialized
/// on the owner thread. The class is not generally thread-safe. Its scopes
/// account only for caller-registered operation IDs; they are not locks and do
/// not make owner stores thread-safe.
/// </summary>
public sealed class ContinuationCensusProtocol
{
    private sealed class OwnerThreadBinding
    {
        public readonly Thread Thread;
        public readonly int ManagedThreadId;

        public OwnerThreadBinding(Thread thread, int managedThreadId)
        {
            Thread = thread ?? throw new ArgumentNullException(nameof(thread));
            ManagedThreadId = managedThreadId;
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

    private sealed class PendingSectionBaseline
    {
        public readonly RegisteredSection Section;
        public readonly OwnerSectionCensusWitness Witness;

        public PendingSectionBaseline(RegisteredSection section, OwnerSectionCensusWitness witness)
        {
            Section = section;
            Witness = witness;
        }
    }

    private sealed class SpatialKnowledgeSectionCandidate
    {
        public readonly string SectionId;
        public readonly OwnerSectionContract Contract;
        public readonly IOwnerSectionCensusProvider Provider;
        public readonly NpcRuntime NpcOwner;
        public readonly SpatialKnowledgeRuntime SpatialKnowledgeOwner;
        public readonly OwnerSectionCensusWitness Witness;

        public SpatialKnowledgeSectionCandidate(
            string sectionId,
            OwnerSectionContract contract,
            IOwnerSectionCensusProvider provider,
            NpcRuntime npcOwner,
            SpatialKnowledgeRuntime spatialKnowledgeOwner,
            OwnerSectionCensusWitness witness)
        {
            SectionId = sectionId;
            Contract = contract;
            Provider = provider;
            NpcOwner = npcOwner;
            SpatialKnowledgeOwner = spatialKnowledgeOwner;
            Witness = witness;
        }
    }

    private sealed class NpcTravelStateCandidate
    {
        public string SectionId;
        public OwnerSectionContract Contract;
        public IOwnerSectionCensusProvider Provider;
        public NpcRuntime NpcOwner;
        public OwnerSectionCensusWitness Witness;
    }

    private sealed class NpcKnowledgeCandidate
    {
        public string SectionId; public OwnerSectionContract Contract; public IOwnerSectionCensusProvider Provider;
        public NpcRuntime NpcOwner; public object TypedOwner; public OwnerSectionCensusWitness Witness;
    }

    private sealed class NpcPlanCandidate
    {
        public string SectionId; public OwnerSectionContract Contract; public IOwnerSectionCensusProvider Provider;
        public NpcRuntime NpcOwner; public object PlanOwner; public int Kind; public OwnerSectionCensusWitness Witness;
    }

    private sealed class LifecycleOwnerCandidate
    {
        public string SectionId;
        public OwnerSectionContract Contract;
        public IOwnerSectionCensusProvider Provider;
        public object OwnerIdentity;
        public OwnerSectionCensusWitness Witness;
    }

    private sealed class MoneyAccountCandidate
    {
        public string SectionId; public OwnerSectionContract Contract; public IOwnerSectionCensusProvider Provider;
        public NpcRuntime NpcOwner; public MoneyAccountRuntime MoneyAccountOwner; public OwnerSectionCensusWitness Witness;
    }

    private Dictionary<string, OwnerSectionContract> expectedSections =
        new Dictionary<string, OwnerSectionContract>(StringComparer.Ordinal);
    private Dictionary<string, RegisteredSection> registeredSections =
        new Dictionary<string, RegisteredSection>(StringComparer.Ordinal);
    private readonly HashSet<string> expectedOperations = new HashSet<string>(StringComparer.Ordinal);
    private HashSet<string> spatialKnowledgeSectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, NpcRuntime> spatialKnowledgeNpcOwnersBySection =
        new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private IReadOnlyList<NpcRuntime> spatialKnowledgeRoster;
    private IReadOnlyList<IOwnerSectionCensusProvider> spatialKnowledgeFamilyProviders =
        Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private HashSet<string> npcTravelStateSectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, NpcRuntime> npcTravelStateNpcOwnersBySection =
        new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private IReadOnlyList<NpcRuntime> npcTravelStateRoster;
    private IReadOnlyList<IOwnerSectionCensusProvider> npcTravelStateFamilyProviders =
        Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private HashSet<string> inventorySectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, NpcRuntime> inventoryNpcOwnersBySection = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private IReadOnlyList<NpcRuntime> inventoryRoster;
    private IReadOnlyList<IOwnerSectionCensusProvider> inventoryFamilyProviders = Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private HashSet<string> moneyAccountSectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, NpcRuntime> moneyAccountNpcOwnersBySection = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private IReadOnlyList<NpcRuntime> moneyAccountRoster;
    private IReadOnlyList<IOwnerSectionCensusProvider> moneyAccountFamilyProviders = Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private HashSet<string> npcKnowledgeSectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, NpcRuntime> npcKnowledgeNpcOwnersBySection = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private IReadOnlyList<NpcRuntime> npcKnowledgeRoster;
    private IReadOnlyList<IOwnerSectionCensusProvider> npcKnowledgeFamilyProviders = Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private HashSet<string> npcPlanSectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, NpcRuntime> npcPlanNpcOwnersBySection = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private IReadOnlyList<NpcRuntime> npcPlanRoster;
    private IReadOnlyList<IOwnerSectionCensusProvider> npcPlanFamilyProviders = Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private HashSet<string> lifecycleSectionIds = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, object> lifecycleOwnersBySection = new Dictionary<string, object>(StringComparer.Ordinal);
    private IReadOnlyList<IOwnerSectionCensusProvider> lifecycleFamilyProviders =
        Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);

    private bool expectedSectionsSealed;
    private bool providersSealed;
    private bool operationsSealed;
    private OwnerThreadBinding ownerThreadBinding;
    private int activeOperationCount;
    private long mutationEpoch;
    private object activeMutationEpochReservation;
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
    /// Declares the one supported dynamic census family for the world's current
    /// NPC roster. The roster is world-owned; callers cannot supply a detached
    /// provider list as the family source.
    /// </summary>
    public bool RegisterSpatialKnowledgeRosterFamily(
        IReadOnlyList<NpcRuntime> roster,
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
        if (roster == null || expectedSectionsSealed || providersSealed || spatialKnowledgeRoster != null)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!TryBuildSpatialKnowledgeFamily(roster,
                out List<SpatialKnowledgeSectionCandidate> candidates, out failure))
        {
            Fault();
            return false;
        }

        Dictionary<string, OwnerSectionContract> stagedExpected =
            new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        Dictionary<string, RegisteredSection> stagedRegistered =
            new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        HashSet<string> stagedIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedNpcOwners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedProviders =
            new IOwnerSectionCensusProvider[candidates.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            SpatialKnowledgeSectionCandidate candidate = candidates[i];
            if (!stagedIds.Add(candidate.SectionId)
                || stagedExpected.ContainsKey(candidate.SectionId)
                || stagedRegistered.ContainsKey(candidate.SectionId))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            stagedExpected.Add(candidate.SectionId, candidate.Contract);
            stagedRegistered.Add(candidate.SectionId,
                new RegisteredSection(candidate.Contract, candidate.Provider));
            stagedNpcOwners.Add(candidate.SectionId, candidate.NpcOwner);
            stagedProviders[i] = candidate.Provider;
        }

        expectedSections = stagedExpected;
        registeredSections = stagedRegistered;
        spatialKnowledgeSectionIds = stagedIds;
        spatialKnowledgeNpcOwnersBySection = stagedNpcOwners;
        spatialKnowledgeRoster = roster;
        spatialKnowledgeFamilyProviders = Array.AsReadOnly(stagedProviders);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>Latest reconciled per-NPC provider snapshot for the declared family.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> SpatialKnowledgeFamilyProviders =>
        spatialKnowledgeFamilyProviders;

    public bool RegisterNpcTravelStateRosterFamily(
        IReadOnlyList<NpcRuntime> roster,
        out ContinuationCensusFailure failure)
    {
        if (IsFaulted()) { failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (IsOwnerThreadBound()) { Fault(); failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (roster == null || expectedSectionsSealed || providersSealed || npcTravelStateRoster != null)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        if (!TryBuildNpcTravelStateFamily(roster, out List<NpcTravelStateCandidate> candidates, out failure))
        { Fault(); return false; }

        Dictionary<string, OwnerSectionContract> stagedExpected =
            new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        Dictionary<string, RegisteredSection> stagedRegistered =
            new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> owners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            NpcTravelStateCandidate candidate = candidates[i];
            if (!ids.Add(candidate.SectionId)
                || stagedExpected.ContainsKey(candidate.SectionId)
                || stagedRegistered.ContainsKey(candidate.SectionId))
            { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
            stagedExpected.Add(candidate.SectionId, candidate.Contract);
            RegisteredSection registered = new RegisteredSection(candidate.Contract, candidate.Provider);
            SetBaseline(registered, candidate.Witness);
            stagedRegistered.Add(candidate.SectionId, registered);
            owners.Add(candidate.SectionId, candidate.NpcOwner);
            providers[i] = candidate.Provider;
        }

        expectedSections = stagedExpected;
        registeredSections = stagedRegistered;
        npcTravelStateSectionIds = ids;
        npcTravelStateNpcOwnersBySection = owners;
        npcTravelStateRoster = roster;
        npcTravelStateFamilyProviders = Array.AsReadOnly(providers);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public IReadOnlyList<IOwnerSectionCensusProvider> NpcTravelStateFamilyProviders =>
        npcTravelStateFamilyProviders;

    public bool RegisterInventoryRosterFamily(IReadOnlyList<NpcRuntime> roster, out ContinuationCensusFailure failure)
    {
        if (IsFaulted()) { failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (IsOwnerThreadBound()) { Fault(); failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (roster == null || expectedSectionsSealed || providersSealed || inventoryRoster != null)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        if (!TryBuildInventoryFamily(roster, out List<InventoryCandidate> candidates, out failure)) { Fault(); return false; }
        var stagedExpected = new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        var stagedRegistered = new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var owners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        var providers = new IOwnerSectionCensusProvider[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            InventoryCandidate c = candidates[i];
            if (!ids.Add(c.SectionId) || stagedExpected.ContainsKey(c.SectionId)) { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
            stagedExpected.Add(c.SectionId, c.Contract);
            var registered = new RegisteredSection(c.Contract, c.Provider);
            SetBaseline(registered, c.Witness);
            stagedRegistered.Add(c.SectionId, registered);
            owners.Add(c.SectionId, c.NpcOwner);
            providers[i] = c.Provider;
        }
        expectedSections = stagedExpected; registeredSections = stagedRegistered;
        inventorySectionIds = ids; inventoryNpcOwnersBySection = owners; inventoryRoster = roster;
        inventoryFamilyProviders = Array.AsReadOnly(providers);
        failure = ContinuationCensusFailure.None; return true;
    }

    public IReadOnlyList<IOwnerSectionCensusProvider> InventoryFamilyProviders => inventoryFamilyProviders;

    public bool RegisterMoneyAccountRosterFamily(IReadOnlyList<NpcRuntime> roster, out ContinuationCensusFailure failure)
    {
        if (IsFaulted()) { failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (IsOwnerThreadBound()) { Fault(); failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (roster == null || expectedSectionsSealed || providersSealed || moneyAccountRoster != null)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        if (!TryBuildMoneyAccountFamily(roster, out List<MoneyAccountCandidate> candidates, out failure)) { Fault(); return false; }

        var stagedExpected = new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        var stagedRegistered = new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var owners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        var providers = new IOwnerSectionCensusProvider[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            MoneyAccountCandidate candidate = candidates[i];
            if (!ids.Add(candidate.SectionId) || stagedExpected.ContainsKey(candidate.SectionId))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            stagedExpected.Add(candidate.SectionId, candidate.Contract);
            var registered = new RegisteredSection(candidate.Contract, candidate.Provider);
            SetBaseline(registered, candidate.Witness);
            stagedRegistered.Add(candidate.SectionId, registered);
            owners.Add(candidate.SectionId, candidate.NpcOwner);
            providers[i] = candidate.Provider;
        }

        expectedSections = stagedExpected;
        registeredSections = stagedRegistered;
        moneyAccountSectionIds = ids;
        moneyAccountNpcOwnersBySection = owners;
        moneyAccountRoster = roster;
        moneyAccountFamilyProviders = Array.AsReadOnly(providers);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public IReadOnlyList<IOwnerSectionCensusProvider> MoneyAccountFamilyProviders => moneyAccountFamilyProviders;

    public bool RegisterNpcKnowledgeRosterFamily(IReadOnlyList<NpcRuntime> roster, out ContinuationCensusFailure failure)
    {
        if (IsFaulted()) { failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (IsOwnerThreadBound()) { Fault(); failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (roster == null || expectedSectionsSealed || providersSealed || npcKnowledgeRoster != null)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        if (!TryBuildNpcKnowledgeFamily(roster, out List<NpcKnowledgeCandidate> candidates, out failure)) { Fault(); return false; }
        var stagedExpected = new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        var stagedRegistered = new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var owners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        var providers = new IOwnerSectionCensusProvider[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            NpcKnowledgeCandidate c = candidates[i];
            if (!ids.Add(c.SectionId) || stagedExpected.ContainsKey(c.SectionId)) { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
            stagedExpected.Add(c.SectionId, c.Contract);
            var section = new RegisteredSection(c.Contract, c.Provider); SetBaseline(section, c.Witness);
            stagedRegistered.Add(c.SectionId, section); owners.Add(c.SectionId, c.NpcOwner);
            providers[i] = c.Provider;
        }
        expectedSections = stagedExpected; registeredSections = stagedRegistered;
        npcKnowledgeSectionIds = ids; npcKnowledgeNpcOwnersBySection = owners; npcKnowledgeRoster = roster;
        npcKnowledgeFamilyProviders = Array.AsReadOnly(providers);
        failure = ContinuationCensusFailure.None; return true;
    }

    public IReadOnlyList<IOwnerSectionCensusProvider> NpcKnowledgeFamilyProviders => npcKnowledgeFamilyProviders;

    public bool RegisterNpcPlanRosterFamily(
        IReadOnlyList<NpcRuntime> roster,
        out ContinuationCensusFailure failure)
    {
        if (IsFaulted()) { failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (IsOwnerThreadBound()) { Fault(); failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (roster == null || expectedSectionsSealed || providersSealed || npcPlanRoster != null)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        if (!TryBuildNpcPlanFamily(roster, out List<NpcPlanCandidate> candidates, out failure))
        { Fault(); return false; }

        Dictionary<string, OwnerSectionContract> stagedExpected =
            new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        Dictionary<string, RegisteredSection> stagedRegistered =
            new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> owners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            NpcPlanCandidate candidate = candidates[i];
            if (!ids.Add(candidate.SectionId) || stagedExpected.ContainsKey(candidate.SectionId))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            stagedExpected.Add(candidate.SectionId, candidate.Contract);
            RegisteredSection section = new RegisteredSection(candidate.Contract, candidate.Provider);
            SetBaseline(section, candidate.Witness);
            stagedRegistered.Add(candidate.SectionId, section);
            owners.Add(candidate.SectionId, candidate.NpcOwner);
            providers[i] = candidate.Provider;
        }

        expectedSections = stagedExpected;
        registeredSections = stagedRegistered;
        npcPlanSectionIds = ids;
        npcPlanNpcOwnersBySection = owners;
        npcPlanRoster = roster;
        npcPlanFamilyProviders = Array.AsReadOnly(providers);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public IReadOnlyList<IOwnerSectionCensusProvider> NpcPlanFamilyProviders => npcPlanFamilyProviders;

    public bool RegisterLifecycleOwnerRosterFamily(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        out ContinuationCensusFailure failure)
    {
        if (IsFaulted()) { failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (IsOwnerThreadBound()) { Fault(); failure = ContinuationCensusFailure.ProtocolFaulted; return false; }
        if (providers == null || expectedSectionsSealed || providersSealed || lifecycleSectionIds.Count != 0)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        if (!TryBuildLifecycleFamily(providers, out List<LifecycleOwnerCandidate> candidates, out failure))
        { Fault(); return false; }

        Dictionary<string, OwnerSectionContract> stagedExpected =
            new Dictionary<string, OwnerSectionContract>(expectedSections, StringComparer.Ordinal);
        Dictionary<string, RegisteredSection> stagedRegistered =
            new Dictionary<string, RegisteredSection>(registeredSections, StringComparer.Ordinal);
        HashSet<string> stagedIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, object> stagedOwners = new Dictionary<string, object>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedProviders = new IOwnerSectionCensusProvider[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            LifecycleOwnerCandidate candidate = candidates[i];
            if (!stagedIds.Add(candidate.SectionId)
                || stagedExpected.ContainsKey(candidate.SectionId)
                || stagedRegistered.ContainsKey(candidate.SectionId))
            { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
            stagedExpected.Add(candidate.SectionId, candidate.Contract);
            RegisteredSection section = new RegisteredSection(candidate.Contract, candidate.Provider);
            SetBaseline(section, candidate.Witness);
            stagedRegistered.Add(candidate.SectionId, section);
            stagedOwners.Add(candidate.SectionId, candidate.OwnerIdentity);
            stagedProviders[i] = candidate.Provider;
        }

        expectedSections = stagedExpected;
        registeredSections = stagedRegistered;
        lifecycleSectionIds = stagedIds;
        lifecycleOwnersBySection = stagedOwners;
        lifecycleFamilyProviders = Array.AsReadOnly(stagedProviders);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    public IReadOnlyList<IOwnerSectionCensusProvider> LifecycleOwnerFamilyProviders =>
        lifecycleFamilyProviders;

    internal bool TryAssessLifecycleOwnerRoster(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        out ContinuationCensusFailure failure)
    {
        if (!TryRequireOwnerThread(out failure)) return false;
        if (!TryBuildLifecycleFamily(
                providers,
                out List<LifecycleOwnerCandidate> candidates,
                out failure))
        {
            Fault();
            return false;
        }
        if (candidates.Count != lifecycleSectionIds.Count)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (LifecycleOwnerCandidate candidate in candidates)
        {
            if (!seen.Add(candidate.SectionId)
                || !lifecycleSectionIds.Contains(candidate.SectionId)
                || !lifecycleOwnersBySection.TryGetValue(candidate.SectionId, out object registeredOwner)
                || !ReferenceEquals(registeredOwner, candidate.OwnerIdentity)
                || !registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection section)
                || !TryReadAndValidate(section, allowRevisionAdvance: false, out _, out failure))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
        }
        failure = ContinuationCensusFailure.None;
        return true;
    }

    private static bool TryBuildLifecycleFamily(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        out List<LifecycleOwnerCandidate> candidates,
        out ContinuationCensusFailure failure)
    {
        candidates = new List<LifecycleOwnerCandidate>();
        failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        if (providers == null) return false;
        try
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in providers)
            {
                if (provider == null) return false;
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                object ownerIdentity;
                string sectionId;
                int schemaVersion;
                if (provider is PersonLifeResidenceCensusProvider personProvider)
                {
                    ownerIdentity = personProvider.PersonOwner;
                    sectionId = PersonLifeResidenceCensusProvider.SectionIdFor(
                        personProvider.PersonOwner.PersonId);
                    schemaVersion = PersonLifeResidenceCensusProvider.SchemaVersion;
                }
                else if (provider is NpcLifecycleCensusProvider npcProvider)
                {
                    ownerIdentity = npcProvider.NpcOwner;
                    sectionId = NpcLifecycleCensusProvider.SectionIdFor(
                        npcProvider.NpcOwner.RuntimeId,
                        npcProvider.IsResidence);
                    schemaVersion = NpcLifecycleCensusProvider.SchemaVersion;
                }
                else if (provider is P12CrimeJusticeCensusProvider.NpcStatusSectionProvider crimeJusticeProvider)
                {
                    ownerIdentity = crimeJusticeProvider.NpcOwner;
                    sectionId = P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor(
                        crimeJusticeProvider.RuntimeId);
                    schemaVersion = P12CrimeJusticeCensusProvider.SchemaVersion;
                }
                else
                {
                    return false;
                }

                if (!ids.Add(sectionId)
                    || witness == null
                    || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != schemaVersion
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, ownerIdentity)
                    || witness.Cardinality != 1)
                    return false;

                candidates.Add(new LifecycleOwnerCandidate
                {
                    SectionId = sectionId,
                    Contract = new OwnerSectionContract(sectionId, schemaVersion, OwnerSectionRole.Required),
                    Provider = provider,
                    OwnerIdentity = ownerIdentity,
                    Witness = witness
                });
            }
        }
        catch
        {
            return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private static bool TryBuildNpcPlanFamily(
        IReadOnlyList<NpcRuntime> roster,
        out List<NpcPlanCandidate> candidates,
        out ContinuationCensusFailure failure)
    {
        candidates = new List<NpcPlanCandidate>();
        failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        try
        {
            HashSet<object> uniqueOwners = new HashSet<object>();
            HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, int> kindsByRuntimeId = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in NpcPlanCensusProvider.CreateProviders(roster))
            {
                if (!(provider is NpcPlanCensusProvider.INpcPlanSectionCensusProvider planProvider))
                    return false;
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                string sectionId = NpcPlanCensusProvider.SectionIdFor(
                    planProvider.Kind, planProvider.RuntimeId);
                OwnerSectionContract contract = new OwnerSectionContract(
                    sectionId, NpcPlanCensusProvider.SchemaVersion, OwnerSectionRole.Required);
                object expectedOwner = planProvider.Kind == NpcPlanCensusProvider.MerchantTradePlanKind
                    ? (object)planProvider.NpcOwner.ExistingMerchantTradePlan
                    : planProvider.NpcOwner.ExistingTravelPlan;
                if (string.IsNullOrWhiteSpace(planProvider.RuntimeId)
                    || planProvider.NpcOwner == null
                    || planProvider.PlanOwner == null
                    || !ReferenceEquals(expectedOwner, planProvider.PlanOwner)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, planProvider.PlanOwner)
                    || witness.Cardinality != 1
                    || !sectionIds.Add(sectionId)
                    || !IsWitnessValidForContract(contract, witness)
                    || !uniqueOwners.Add(planProvider.PlanOwner))
                    return false;

                int kindBit = 1 << planProvider.Kind;
                kindsByRuntimeId[planProvider.RuntimeId] =
                    kindsByRuntimeId.TryGetValue(planProvider.RuntimeId, out int priorKinds)
                        ? priorKinds | kindBit
                        : kindBit;
                candidates.Add(new NpcPlanCandidate
                {
                    SectionId = sectionId,
                    Contract = contract,
                    Provider = provider,
                    NpcOwner = planProvider.NpcOwner,
                    PlanOwner = planProvider.PlanOwner,
                    Kind = planProvider.Kind,
                    Witness = witness
                });
            }

            if (roster == null || candidates.Count != roster.Count * 2
                || kindsByRuntimeId.Count != roster.Count)
                return false;
            foreach (int kinds in kindsByRuntimeId.Values)
                if (kinds != 3) return false;
        }
        catch { return false; }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryValidateNpcPlanFamilyMatchesRoster(out ContinuationCensusFailure failure)
    {
        if (!TryBuildNpcPlanFamily(npcPlanRoster, out List<NpcPlanCandidate> candidates, out failure))
            return false;
        if (candidates.Count != npcPlanSectionIds.Count)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        foreach (NpcPlanCandidate candidate in candidates)
        {
            if (!npcPlanSectionIds.Contains(candidate.SectionId)
                || !registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection section)
                || !npcPlanNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime npc)
                || !ReferenceEquals(npc, candidate.NpcOwner)
                || (section.HasBaseline && !ReferenceEquals(section.OwnerInstanceIdentity, candidate.PlanOwner)))
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
        }
        failure = ContinuationCensusFailure.None;
        return true;
    }

    private static bool TryBuildNpcKnowledgeFamily(IReadOnlyList<NpcRuntime> roster, out List<NpcKnowledgeCandidate> candidates, out ContinuationCensusFailure failure)
    {
        candidates = new List<NpcKnowledgeCandidate>(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers = NpcKnowledgeCensusProvider.CreateProviders(roster);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var ownersByRuntimeGroup = new Dictionary<string, Tuple<NpcRuntime, object, long>>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in providers)
            {
                if (!(provider is NpcKnowledgeCensusProvider.INpcKnowledgeSectionCensusProvider p)) return false;
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                string sectionId = NpcKnowledgeCensusProvider.SectionIdFor(p.SectionKind, p.RuntimeId);
                var contract = new OwnerSectionContract(sectionId, NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required);
                if (p.NpcOwner == null || p.TypedOwner == null || !ReferenceEquals(witness.OwnerInstanceIdentity, p.TypedOwner)
                    || !seen.Add(sectionId) || !IsWitnessValidForContract(contract, witness)) return false;
                int group = p.SectionKind == 0 ? 0 : p.SectionKind <= 2 ? 1 : p.SectionKind <= 6 ? 2 : 3;
                string groupKey = p.RuntimeId.Length + "#" + p.RuntimeId + "#" + group;
                if (ownersByRuntimeGroup.TryGetValue(groupKey, out var prior))
                {
                    if (!ReferenceEquals(prior.Item1, p.NpcOwner) || !ReferenceEquals(prior.Item2, p.TypedOwner)
                        || prior.Item3 != witness.Revision) return false;
                }
                else
                {
                    ownersByRuntimeGroup.Add(groupKey, Tuple.Create(p.NpcOwner, p.TypedOwner, witness.Revision));
                }
                candidates.Add(new NpcKnowledgeCandidate { SectionId=sectionId, Contract=contract, Provider=provider, NpcOwner=p.NpcOwner, TypedOwner=p.TypedOwner, Witness=witness });
            }
            if (candidates.Count != roster.Count * 10 || ownersByRuntimeGroup.Count != roster.Count * 4) return false;
        }
        catch { return false; }
        failure = ContinuationCensusFailure.None; return true;
    }

    private sealed class InventoryCandidate
    {
        public string SectionId; public OwnerSectionContract Contract; public IOwnerSectionCensusProvider Provider;
        public NpcRuntime NpcOwner; public InventoryRuntime InventoryOwner; public OwnerSectionCensusWitness Witness;
    }

    private static bool TryBuildInventoryFamily(IReadOnlyList<NpcRuntime> roster, out List<InventoryCandidate> candidates, out ContinuationCensusFailure failure)
    {
        candidates = new List<InventoryCandidate>(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        try
        {
            foreach (IOwnerSectionCensusProvider provider in NpcInventoryCensusProvider.CreateProviders(roster))
            {
                if (!(provider is NpcInventoryCensusProvider.INpcInventorySectionCensusProvider p)) return false;
                OwnerSectionCensusWitness w = provider.GetCurrentCensus();
                string id = NpcInventoryCensusProvider.SectionPrefix + p.RuntimeId;
                var contract = new OwnerSectionContract(id, NpcInventoryCensusProvider.SchemaVersion, OwnerSectionRole.Required);
                if (string.IsNullOrWhiteSpace(p.RuntimeId) || p.NpcOwner == null || p.InventoryOwner == null
                    || !ReferenceEquals(p.NpcOwner.ExistingInventory, p.InventoryOwner)
                    || !ReferenceEquals(w.OwnerInstanceIdentity, p.InventoryOwner) || !IsWitnessValidForContract(contract, w)) return false;
                candidates.Add(new InventoryCandidate { SectionId=id, Contract=contract, Provider=provider, NpcOwner=p.NpcOwner, InventoryOwner=p.InventoryOwner, Witness=w });
            }
        }
        catch { return false; }
        if (candidates.Count != roster.Count) return false;
        failure = ContinuationCensusFailure.None; return true;
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
        Thread currentThread = Thread.CurrentThread;
        return BindOwnerThread(currentThread, currentThread.ManagedThreadId, out failure);
    }

    /// <summary>Binds to an explicitly captured owner thread after verifying the current caller.</summary>
    internal bool BindOwnerThread(
        Thread expectedOwnerThread,
        int expectedOwnerThreadId,
        out ContinuationCensusFailure failure)
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

        if (expectedOwnerThread == null
            || expectedOwnerThreadId <= 0
            || expectedOwnerThread.ManagedThreadId != expectedOwnerThreadId
            || !ReferenceEquals(Thread.CurrentThread, expectedOwnerThread)
            || Thread.CurrentThread.ManagedThreadId != expectedOwnerThreadId)
        {
            Fault();
            failure = ContinuationCensusFailure.WrongOwnerThread;
            return false;
        }

        OwnerThreadBinding binding = new OwnerThreadBinding(
            expectedOwnerThread,
            expectedOwnerThreadId);
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

        if (!expectedSectionsSealed || !providersSealed || expectedSections.Count == 0)
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

        if (registeredSections.Count != expectedSections.Count
            || (spatialKnowledgeRoster != null
                && !TryValidateSpatialKnowledgeFamilyMatchesRoster(out failure))
            || (npcTravelStateRoster != null
                && !TryValidateNpcTravelStateFamilyMatchesRoster(out failure))
            || (inventoryRoster != null && !TryValidateInventoryFamilyMatchesRoster(out failure))
            || (moneyAccountRoster != null && !TryValidateMoneyAccountFamilyMatchesRoster(out failure))
            || (npcKnowledgeRoster != null && !TryValidateNpcKnowledgeFamilyMatchesRoster(out failure))
            || (npcPlanRoster != null && !TryValidateNpcPlanFamilyMatchesRoster(out failure)))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        List<PendingSectionBaseline> observedSections = new List<PendingSectionBaseline>(expectedSections.Count);
        foreach (KeyValuePair<string, OwnerSectionContract> pair in expectedSections)
        {
            if (!registeredSections.TryGetValue(pair.Key, out RegisteredSection section))
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            if (!TryReadAndValidate(section, allowRevisionAdvance: false,
                    out OwnerSectionCensusWitness witness, out failure))
            {
                Fault();
                return false;
            }

            observedSections.Add(new PendingSectionBaseline(section, witness));
        }

        foreach (PendingSectionBaseline observed in observedSections)
            SetBaseline(observed.Section, observed.Witness);

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
        return NotifyCommittedMutations(new[] { sectionId }, out failure);
    }

    /// <summary>
    /// Checks that the named registered owner sections still match their last
    /// accepted witnesses without changing the mutation epoch. This may be used
    /// inside a broader active operation to reject a stale child boundary before
    /// its first commit.
    /// </summary>
    internal bool TryValidateUnchangedSections(
        IEnumerable<string> sectionIds,
        out ContinuationCensusFailure failure)
    {
        if (!TryRequireOwnerThread(out failure)) return false;
        if (sectionIds == null)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        int validatedCount = 0;
        try
        {
            foreach (string sectionId in sectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId)
                    || !seen.Add(sectionId)
                    || !registeredSections.TryGetValue(sectionId, out RegisteredSection section)
                    || !section.HasBaseline
                    || !TryReadAndValidate(section, allowRevisionAdvance: false, out _, out failure))
                {
                    Fault();
                    if (failure == ContinuationCensusFailure.None)
                        failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                    return false;
                }

                validatedCount++;
            }
        }
        catch
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (validatedCount == 0)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>
    /// Records one successful outer commit that changed one or more owner
    /// sections. All changed witnesses are validated before any baseline or
    /// epoch is updated, so one logical commit advances the epoch exactly once.
    /// A malformed or incomplete notification faults the protocol closed.
    /// </summary>
    public bool NotifyCommittedMutations(
        IEnumerable<string> sectionIds,
        out ContinuationCensusFailure failure)
    {
        if (activeMutationEpochReservation != null)
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }
        return NotifyCommittedMutationsCore(sectionIds, out failure);
    }

    private bool NotifyCommittedMutationsCore(
        IEnumerable<string> sectionIds,
        out ContinuationCensusFailure failure)
    {
        if (!TryRequireOwnerThread(out failure)) return false;
        if (mutationEpoch == long.MaxValue)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        if (sectionIds == null)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        List<RegisteredSection> changedSections = new List<RegisteredSection>();
        HashSet<string> changedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (string sectionId in sectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId)
                    || !changedSectionIds.Add(sectionId)
                    || !registeredSections.TryGetValue(sectionId, out RegisteredSection section))
                {
                    Fault();
                    failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                    return false;
                }

                changedSections.Add(section);
            }
        }
        catch
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (changedSections.Count == 0)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        List<PendingSectionBaseline> observedSections = new List<PendingSectionBaseline>(changedSections.Count);
        foreach (RegisteredSection section in changedSections)
        {
            bool hadBaseline = section.HasBaseline;
            long priorRevision = section.LastRevision;
            if (!TryReadAndValidate(section, allowRevisionAdvance: true,
                    out OwnerSectionCensusWitness witness, out failure))
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

            observedSections.Add(new PendingSectionBaseline(section, witness));
        }

        foreach (PendingSectionBaseline observed in observedSections)
            SetBaseline(observed.Section, observed.Witness);

        mutationEpoch++;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    /// <summary>
    /// Atomically reconciles the declared NPC SpatialKnowledge family together
    /// with fixed sections changed by one outer roster/materialization boundary.
    /// This is intentionally specialized; it is not a general post-seal provider
    /// registration API.
    /// </summary>
    public bool TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
        IEnumerable<string> changedFixedSectionIds,
        long personStoreRevisionAtOperationStart,
        out ContinuationCensusFailure failure)
    {
        return TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
            changedFixedSectionIds,
            personStoreRevisionAtOperationStart,
            lifecycleFamilyProviders,
            null,
            out failure);
    }

    internal bool TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
        IEnumerable<string> changedFixedSectionIds,
        long personStoreRevisionAtOperationStart,
        IReadOnlyList<IOwnerSectionCensusProvider> currentLifecycleProviders,
        ContinuationMutationEpochReservation epochReservation,
        out ContinuationCensusFailure failure)
    {
        if (!TryRequireOwnerThread(out failure)) return false;
        if (spatialKnowledgeRoster == null
            || activeOperationCount == 0
            || personStoreRevisionAtOperationStart < 0L)
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        if (epochReservation != null
            && (!ReferenceEquals(epochReservation.Owner, this)
                || !ReferenceEquals(epochReservation.Token, activeMutationEpochReservation)
                || !ReferenceEquals(epochReservation.OwnerThread, Thread.CurrentThread)
                || epochReservation.OwnerThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId))
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        HashSet<string> changedIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            if (changedFixedSectionIds == null)
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
            foreach (string sectionId in changedFixedSectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId)
                    || spatialKnowledgeSectionIds.Contains(sectionId)
                    || inventorySectionIds.Contains(sectionId)
                    || moneyAccountSectionIds.Contains(sectionId)
                    || npcTravelStateSectionIds.Contains(sectionId)
                    || npcKnowledgeSectionIds.Contains(sectionId)
                    || npcPlanSectionIds.Contains(sectionId)
                    || !registeredSections.ContainsKey(sectionId)
                    || !changedIds.Add(sectionId))
                {
                    Fault();
                    failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                    return false;
                }
            }
        }
        catch
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!TryBuildSpatialKnowledgeFamily(spatialKnowledgeRoster,
                out List<SpatialKnowledgeSectionCandidate> candidates, out failure))
        {
            Fault();
            return false;
        }
        List<NpcTravelStateCandidate> npcTravelStateCandidates = new List<NpcTravelStateCandidate>();
        if (npcTravelStateRoster != null
            && !TryBuildNpcTravelStateFamily(npcTravelStateRoster,
                out npcTravelStateCandidates, out failure))
        { Fault(); return false; }
        List<InventoryCandidate> inventoryCandidates = new List<InventoryCandidate>();
        if (inventoryRoster != null
            && !TryBuildInventoryFamily(inventoryRoster, out inventoryCandidates, out failure))
        { Fault(); return false; }
        List<MoneyAccountCandidate> moneyAccountCandidates = new List<MoneyAccountCandidate>();
        if (moneyAccountRoster != null
            && !TryBuildMoneyAccountFamily(moneyAccountRoster, out moneyAccountCandidates, out failure))
        { Fault(); return false; }
        List<NpcKnowledgeCandidate> npcKnowledgeCandidates = new List<NpcKnowledgeCandidate>();
        if (npcKnowledgeRoster != null && !TryBuildNpcKnowledgeFamily(npcKnowledgeRoster, out npcKnowledgeCandidates, out failure))
        { Fault(); return false; }
        List<NpcPlanCandidate> npcPlanCandidates = new List<NpcPlanCandidate>();
        if (npcPlanRoster != null && !TryBuildNpcPlanFamily(npcPlanRoster, out npcPlanCandidates, out failure))
        { Fault(); return false; }
        if (!TryBuildLifecycleFamily(
                currentLifecycleProviders,
                out List<LifecycleOwnerCandidate> lifecycleCandidates,
                out failure))
        { Fault(); return false; }

        Dictionary<string, RegisteredSection> stagedRegistered =
            new Dictionary<string, RegisteredSection>(StringComparer.Ordinal);
        Dictionary<string, OwnerSectionContract> stagedExpected =
            new Dictionary<string, OwnerSectionContract>(StringComparer.Ordinal);
        Dictionary<string, IOwnerSectionCensusProvider> currentFixedProviders =
            new Dictionary<string, IOwnerSectionCensusProvider>(StringComparer.Ordinal);
        bool fixedSectionsChanged = false;
        bool personStoreSectionsChanged = false;
        OwnerSectionCensusWitness personMembershipWitness = null;
        OwnerSectionCensusWitness personBindingWitness = null;
        bool hasPersonMembership = false;
        bool hasPersonBinding = false;

        foreach (KeyValuePair<string, OwnerSectionContract> pair in expectedSections)
        {
            if (spatialKnowledgeSectionIds.Contains(pair.Key)
                || inventorySectionIds.Contains(pair.Key)
                || moneyAccountSectionIds.Contains(pair.Key)
                || npcTravelStateSectionIds.Contains(pair.Key)
                || npcKnowledgeSectionIds.Contains(pair.Key)
                || npcPlanSectionIds.Contains(pair.Key)
                || lifecycleSectionIds.Contains(pair.Key)) continue;
            if (!registeredSections.TryGetValue(pair.Key, out RegisteredSection current))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            bool notified = changedIds.Contains(pair.Key);
            if (!TryReadAndValidate(current, allowRevisionAdvance: notified,
                    out OwnerSectionCensusWitness witness, out failure))
            {
                Fault();
                return false;
            }

            bool sectionChanged = current.HasBaseline
                && (witness.Revision != current.LastRevision
                    || witness.Cardinality != current.LastCardinality);
            if (notified != sectionChanged
                || (notified && (!current.HasBaseline || witness.Revision == current.LastRevision)))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            if (notified)
            {
                fixedSectionsChanged = true;
                RegisteredSection stagedSection = CloneRegisteredSection(current);
                SetBaseline(stagedSection, witness);
                stagedRegistered.Add(pair.Key, stagedSection);
            }
            else
            {
                stagedRegistered.Add(pair.Key, current);
            }
            stagedExpected.Add(pair.Key, pair.Value);

            if (pair.Key == PersonMembershipCensusProvider.SectionId)
            {
                hasPersonMembership = true;
                personMembershipWitness = witness;
                personStoreSectionsChanged |= sectionChanged;
            }
            else if (pair.Key == PersonMaterializationBindingCensusProvider.SectionId)
            {
                hasPersonBinding = true;
                personBindingWitness = witness;
                personStoreSectionsChanged |= sectionChanged;
            }
        }

        if (!hasPersonMembership
            || !hasPersonBinding
            || !ReferenceEquals(
                    personMembershipWitness.OwnerInstanceIdentity,
                    personBindingWitness.OwnerInstanceIdentity)
                || personMembershipWitness.Revision != personBindingWitness.Revision
                || personMembershipWitness.Revision < personStoreRevisionAtOperationStart
                || personStoreRevisionAtOperationStart != GetBaselineRevision(
                    registeredSections[PersonMembershipCensusProvider.SectionId])
                || personStoreRevisionAtOperationStart != GetBaselineRevision(
                    registeredSections[PersonMaterializationBindingCensusProvider.SectionId])
                || (personStoreSectionsChanged
                    ? (!changedIds.Contains(PersonMembershipCensusProvider.SectionId)
                        || !changedIds.Contains(PersonMaterializationBindingCensusProvider.SectionId))
                    : (changedIds.Contains(PersonMembershipCensusProvider.SectionId)
                        || changedIds.Contains(PersonMaterializationBindingCensusProvider.SectionId))))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        HashSet<string> stagedDynamicIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedDynamicNpcOwners =
            new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedFamilyProviders =
            new IOwnerSectionCensusProvider[candidates.Count];
        bool familyChanged = candidates.Count != spatialKnowledgeSectionIds.Count;

        for (int i = 0; i < candidates.Count; i++)
        {
            SpatialKnowledgeSectionCandidate candidate = candidates[i];
            if (!stagedDynamicIds.Add(candidate.SectionId)
                || stagedExpected.ContainsKey(candidate.SectionId))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            bool existed = registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection current);
            bool sameNpcOwner = existed
                && spatialKnowledgeNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime oldNpcOwner)
                && ReferenceEquals(oldNpcOwner, candidate.NpcOwner);
            bool sameKnowledgeOwner = existed
                && ReferenceEquals(current.OwnerInstanceIdentity, candidate.SpatialKnowledgeOwner);

            if (sameNpcOwner && sameKnowledgeOwner)
            {
                if (!TryReadAndValidate(current, allowRevisionAdvance: false,
                        out _, out failure))
                {
                    Fault();
                    return false;
                }
                stagedRegistered.Add(candidate.SectionId, current);
                stagedExpected.Add(candidate.SectionId, current.Contract);
                stagedFamilyProviders[i] = current.Provider;
            }
            else
            {
                familyChanged = true;
                RegisteredSection replacement = new RegisteredSection(candidate.Contract, candidate.Provider);
                SetBaseline(replacement, candidate.Witness);
                stagedRegistered.Add(candidate.SectionId, replacement);
                stagedExpected.Add(candidate.SectionId, candidate.Contract);
                stagedFamilyProviders[i] = candidate.Provider;
            }

            if (!existed) familyChanged = true;
            stagedDynamicNpcOwners.Add(candidate.SectionId, candidate.NpcOwner);
        }

        foreach (string oldSectionId in spatialKnowledgeSectionIds)
        {
            if (!stagedDynamicIds.Contains(oldSectionId)) familyChanged = true;
        }

        HashSet<string> stagedTravelStateIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedTravelStateOwners =
            new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedTravelStateProviders =
            new IOwnerSectionCensusProvider[npcTravelStateCandidates.Count];
        bool travelStateChanged = npcTravelStateCandidates.Count != npcTravelStateSectionIds.Count;
        for (int i = 0; i < npcTravelStateCandidates.Count; i++)
        {
            NpcTravelStateCandidate candidate = npcTravelStateCandidates[i];
            if (!stagedTravelStateIds.Add(candidate.SectionId)
                || stagedExpected.ContainsKey(candidate.SectionId))
            { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }

            bool existed = registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection prior);
            bool sameNpc = existed
                && npcTravelStateNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime oldNpc)
                && ReferenceEquals(oldNpc, candidate.NpcOwner);
            bool sameOwner = existed && ReferenceEquals(prior.OwnerInstanceIdentity, candidate.NpcOwner);
            if (sameNpc && sameOwner)
            {
                if (!TryReadAndValidate(prior, allowRevisionAdvance: false, out _, out failure))
                { Fault(); return false; }
                stagedRegistered.Add(candidate.SectionId, prior);
                stagedExpected.Add(candidate.SectionId, prior.Contract);
                stagedTravelStateProviders[i] = prior.Provider;
            }
            else
            {
                travelStateChanged = true;
                RegisteredSection replacement = new RegisteredSection(candidate.Contract, candidate.Provider);
                SetBaseline(replacement, candidate.Witness);
                stagedRegistered.Add(candidate.SectionId, replacement);
                stagedExpected.Add(candidate.SectionId, candidate.Contract);
                stagedTravelStateProviders[i] = candidate.Provider;
            }
            if (!existed) travelStateChanged = true;
            stagedTravelStateOwners.Add(candidate.SectionId, candidate.NpcOwner);
        }
        foreach (string oldId in npcTravelStateSectionIds)
            if (!stagedTravelStateIds.Contains(oldId)) travelStateChanged = true;

        HashSet<string> stagedInventoryIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedInventoryOwners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedInventoryProviders = new IOwnerSectionCensusProvider[inventoryCandidates.Count];
        bool inventoryChanged = inventoryCandidates.Count != inventorySectionIds.Count;
        for (int i = 0; i < inventoryCandidates.Count; i++)
        {
            InventoryCandidate c = inventoryCandidates[i];
            if (!stagedInventoryIds.Add(c.SectionId) || stagedExpected.ContainsKey(c.SectionId))
            { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
            bool existed = registeredSections.TryGetValue(c.SectionId, out RegisteredSection prior);
            bool sameNpc = existed && inventoryNpcOwnersBySection.TryGetValue(c.SectionId, out NpcRuntime oldNpc) && ReferenceEquals(oldNpc, c.NpcOwner);
            bool sameOwner = existed && ReferenceEquals(prior.OwnerInstanceIdentity, c.InventoryOwner);
            if (existed && (!sameNpc || !sameOwner))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
            if (sameNpc && sameOwner)
            {
                if (!TryReadAndValidate(prior, false, out _, out failure)) { Fault(); return false; }
                stagedRegistered.Add(c.SectionId, prior); stagedExpected.Add(c.SectionId, prior.Contract); stagedInventoryProviders[i] = prior.Provider;
            }
            else
            {
                inventoryChanged = true;
                RegisteredSection replacement = new RegisteredSection(c.Contract, c.Provider); SetBaseline(replacement, c.Witness);
                stagedRegistered.Add(c.SectionId, replacement); stagedExpected.Add(c.SectionId, c.Contract); stagedInventoryProviders[i] = c.Provider;
            }
            if (!existed) inventoryChanged = true;
            stagedInventoryOwners.Add(c.SectionId, c.NpcOwner);
        }
        foreach (string oldId in inventorySectionIds) if (!stagedInventoryIds.Contains(oldId)) inventoryChanged = true;

        HashSet<string> stagedMoneyAccountIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedMoneyAccountOwners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedMoneyAccountProviders = new IOwnerSectionCensusProvider[moneyAccountCandidates.Count];
        bool moneyAccountChanged = moneyAccountCandidates.Count != moneyAccountSectionIds.Count;
        for (int i = 0; i < moneyAccountCandidates.Count; i++)
        {
            MoneyAccountCandidate candidate = moneyAccountCandidates[i];
            if (!stagedMoneyAccountIds.Add(candidate.SectionId) || stagedExpected.ContainsKey(candidate.SectionId))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            bool existed = registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection prior);
            bool sameNpc = existed
                && moneyAccountNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime oldNpc)
                && ReferenceEquals(oldNpc, candidate.NpcOwner);
            bool sameOwner = existed && ReferenceEquals(prior.OwnerInstanceIdentity, candidate.MoneyAccountOwner);
            if (existed && (!sameNpc || !sameOwner))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            if (sameNpc && sameOwner)
            {
                if (!TryReadAndValidate(prior, allowRevisionAdvance: false, out _, out failure))
                {
                    Fault();
                    return false;
                }
                stagedRegistered.Add(candidate.SectionId, prior);
                stagedExpected.Add(candidate.SectionId, prior.Contract);
                stagedMoneyAccountProviders[i] = prior.Provider;
            }
            else
            {
                moneyAccountChanged = true;
                var replacement = new RegisteredSection(candidate.Contract, candidate.Provider);
                SetBaseline(replacement, candidate.Witness);
                stagedRegistered.Add(candidate.SectionId, replacement);
                stagedExpected.Add(candidate.SectionId, candidate.Contract);
                stagedMoneyAccountProviders[i] = candidate.Provider;
            }

            if (!existed) moneyAccountChanged = true;
            stagedMoneyAccountOwners.Add(candidate.SectionId, candidate.NpcOwner);
        }
        foreach (string oldId in moneyAccountSectionIds)
            if (!stagedMoneyAccountIds.Contains(oldId)) moneyAccountChanged = true;

        HashSet<string> stagedKnowledgeIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedKnowledgeOwners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedKnowledgeProviders = new IOwnerSectionCensusProvider[npcKnowledgeCandidates.Count];
        bool knowledgeChanged = npcKnowledgeCandidates.Count != npcKnowledgeSectionIds.Count;
        for (int i = 0; i < npcKnowledgeCandidates.Count; i++)
        {
            NpcKnowledgeCandidate c = npcKnowledgeCandidates[i];
            if (!stagedKnowledgeIds.Add(c.SectionId) || stagedExpected.ContainsKey(c.SectionId))
            { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
            bool existed = registeredSections.TryGetValue(c.SectionId, out RegisteredSection prior);
            bool sameNpc = existed && npcKnowledgeNpcOwnersBySection.TryGetValue(c.SectionId, out NpcRuntime oldNpc) && ReferenceEquals(oldNpc, c.NpcOwner);
            bool sameOwner = existed && ReferenceEquals(prior.OwnerInstanceIdentity, c.TypedOwner);
            if (sameNpc && sameOwner)
            {
                if (!TryReadAndValidate(prior, false, out _, out failure)) { Fault(); return false; }
                stagedRegistered.Add(c.SectionId, prior); stagedExpected.Add(c.SectionId, prior.Contract); stagedKnowledgeProviders[i] = prior.Provider;
            }
            else
            {
                knowledgeChanged = true;
                RegisteredSection replacement = new RegisteredSection(c.Contract, c.Provider); SetBaseline(replacement, c.Witness);
                stagedRegistered.Add(c.SectionId, replacement); stagedExpected.Add(c.SectionId, c.Contract); stagedKnowledgeProviders[i] = c.Provider;
            }
            if (!existed) knowledgeChanged = true;
            stagedKnowledgeOwners.Add(c.SectionId, c.NpcOwner);
        }
        foreach (string oldId in npcKnowledgeSectionIds) if (!stagedKnowledgeIds.Contains(oldId)) knowledgeChanged = true;

        HashSet<string> stagedPlanIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> stagedPlanNpcOwners = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedPlanProviders = new IOwnerSectionCensusProvider[npcPlanCandidates.Count];
        bool plansChanged = npcPlanCandidates.Count != npcPlanSectionIds.Count;
        for (int i = 0; i < npcPlanCandidates.Count; i++)
        {
            NpcPlanCandidate candidate = npcPlanCandidates[i];
            if (!stagedPlanIds.Add(candidate.SectionId) || stagedExpected.ContainsKey(candidate.SectionId))
            { Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }

            bool existed = registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection prior);
            bool sameNpc = existed
                && npcPlanNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime oldNpc)
                && ReferenceEquals(oldNpc, candidate.NpcOwner);
            bool sameOwner = existed && ReferenceEquals(prior.OwnerInstanceIdentity, candidate.PlanOwner);
            if (existed && sameNpc && !sameOwner)
            {
                Fault(); failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false;
            }

            if (sameNpc && sameOwner)
            {
                if (!TryReadAndValidate(prior, allowRevisionAdvance: false, out _, out failure))
                { Fault(); return false; }
                stagedRegistered.Add(candidate.SectionId, prior);
                stagedExpected.Add(candidate.SectionId, prior.Contract);
                stagedPlanProviders[i] = prior.Provider;
            }
            else
            {
                plansChanged = true;
                RegisteredSection replacement = new RegisteredSection(candidate.Contract, candidate.Provider);
                SetBaseline(replacement, candidate.Witness);
                stagedRegistered.Add(candidate.SectionId, replacement);
                stagedExpected.Add(candidate.SectionId, candidate.Contract);
                stagedPlanProviders[i] = candidate.Provider;
            }

            if (!existed) plansChanged = true;
            stagedPlanNpcOwners.Add(candidate.SectionId, candidate.NpcOwner);
        }
        foreach (string oldId in npcPlanSectionIds)
            if (!stagedPlanIds.Contains(oldId)) plansChanged = true;

        HashSet<string> stagedLifecycleIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, object> stagedLifecycleOwners = new Dictionary<string, object>(StringComparer.Ordinal);
        IOwnerSectionCensusProvider[] stagedLifecycleProviders =
            new IOwnerSectionCensusProvider[lifecycleCandidates.Count];
        bool lifecycleChanged = lifecycleCandidates.Count != lifecycleSectionIds.Count;
        for (int i = 0; i < lifecycleCandidates.Count; i++)
        {
            LifecycleOwnerCandidate candidate = lifecycleCandidates[i];
            if (!stagedLifecycleIds.Add(candidate.SectionId)
                || stagedExpected.ContainsKey(candidate.SectionId))
            {
                Fault();
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }

            bool existed = registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection prior);
            bool sameOwner = existed
                && lifecycleOwnersBySection.TryGetValue(candidate.SectionId, out object previousOwner)
                && ReferenceEquals(previousOwner, candidate.OwnerIdentity);
            if (sameOwner)
            {
                bool notified = changedIds.Contains(candidate.SectionId);
                if (!TryReadAndValidate(
                        prior,
                        allowRevisionAdvance: notified,
                        out OwnerSectionCensusWitness witness,
                        out failure))
                {
                    Fault();
                    return false;
                }

                bool sectionChanged = prior.HasBaseline
                    && (witness.Revision != prior.LastRevision
                        || witness.Cardinality != prior.LastCardinality);
                if (notified != sectionChanged
                    || (notified && (!prior.HasBaseline || witness.Revision == prior.LastRevision)))
                {
                    Fault();
                    failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                    return false;
                }

                if (notified)
                {
                    fixedSectionsChanged = true;
                    RegisteredSection changedSection = CloneRegisteredSection(prior);
                    SetBaseline(changedSection, witness);
                    stagedRegistered.Add(candidate.SectionId, changedSection);
                }
                else
                {
                    stagedRegistered.Add(candidate.SectionId, prior);
                }
                stagedExpected.Add(candidate.SectionId, prior.Contract);
                stagedLifecycleProviders[i] = prior.Provider;
            }
            else
            {
                if (changedIds.Contains(candidate.SectionId))
                {
                    Fault();
                    failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                    return false;
                }
                lifecycleChanged = true;
                RegisteredSection replacement = new RegisteredSection(candidate.Contract, candidate.Provider);
                SetBaseline(replacement, candidate.Witness);
                stagedRegistered.Add(candidate.SectionId, replacement);
                stagedExpected.Add(candidate.SectionId, candidate.Contract);
                stagedLifecycleProviders[i] = candidate.Provider;
            }

            if (!existed) lifecycleChanged = true;
            stagedLifecycleOwners.Add(candidate.SectionId, candidate.OwnerIdentity);
        }
        foreach (string oldId in lifecycleSectionIds)
            if (!stagedLifecycleIds.Contains(oldId)) lifecycleChanged = true;

        if (personStoreSectionsChanged
            && changedIds.Contains(PersonMembershipCensusProvider.SectionId)
            != changedIds.Contains(PersonMaterializationBindingCensusProvider.SectionId))
        {
            Fault();
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        bool anySectionChanged = fixedSectionsChanged || familyChanged || travelStateChanged
            || inventoryChanged || moneyAccountChanged || knowledgeChanged || plansChanged || lifecycleChanged;
        if (anySectionChanged
            && mutationEpoch == long.MaxValue)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        expectedSections = stagedExpected;
        registeredSections = stagedRegistered;
        spatialKnowledgeSectionIds = stagedDynamicIds;
        spatialKnowledgeNpcOwnersBySection = stagedDynamicNpcOwners;
        npcTravelStateSectionIds = stagedTravelStateIds;
        npcTravelStateNpcOwnersBySection = stagedTravelStateOwners;
        inventorySectionIds = stagedInventoryIds;
        inventoryNpcOwnersBySection = stagedInventoryOwners;
        moneyAccountSectionIds = stagedMoneyAccountIds;
        moneyAccountNpcOwnersBySection = stagedMoneyAccountOwners;
        npcKnowledgeSectionIds = stagedKnowledgeIds;
        npcKnowledgeNpcOwnersBySection = stagedKnowledgeOwners;
        npcPlanSectionIds = stagedPlanIds;
        npcPlanNpcOwnersBySection = stagedPlanNpcOwners;
        lifecycleSectionIds = stagedLifecycleIds;
        lifecycleOwnersBySection = stagedLifecycleOwners;
        if (familyChanged)
            spatialKnowledgeFamilyProviders = Array.AsReadOnly(stagedFamilyProviders);
        if (travelStateChanged)
            npcTravelStateFamilyProviders = Array.AsReadOnly(stagedTravelStateProviders);
        if (inventoryChanged) inventoryFamilyProviders = Array.AsReadOnly(stagedInventoryProviders);
        if (moneyAccountChanged) moneyAccountFamilyProviders = Array.AsReadOnly(stagedMoneyAccountProviders);
        if (knowledgeChanged) npcKnowledgeFamilyProviders = Array.AsReadOnly(stagedKnowledgeProviders);
        if (plansChanged) npcPlanFamilyProviders = Array.AsReadOnly(stagedPlanProviders);
        if (lifecycleChanged)
            lifecycleFamilyProviders = Array.AsReadOnly(stagedLifecycleProviders);
        if (epochReservation != null)
        {
            if (!TryCompleteMutationEpochReservation(epochReservation, anySectionChanged, out failure))
                return false;
        }
        else if (anySectionChanged)
        {
            mutationEpoch++;
        }
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

    internal bool TryValidateMutationEpochCapacity(out ContinuationCensusFailure failure)
    {
        if (!TryRequireOwnerThread(out failure)) return false;
        if (activeMutationEpochReservation != null)
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }
        if (mutationEpoch == long.MaxValue)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    internal bool TryReserveMutationEpochCapacity(
        out ContinuationMutationEpochReservation reservation,
        out ContinuationCensusFailure failure)
    {
        reservation = null;
        if (!TryRequireOwnerThread(out failure)) return false;
        if (activeOperationCount != 1 || activeMutationEpochReservation != null)
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }
        if (mutationEpoch == long.MaxValue)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        object token = new object();
        activeMutationEpochReservation = token;
        OwnerThreadBinding binding = Volatile.Read(ref ownerThreadBinding);
        reservation = new ContinuationMutationEpochReservation(this, token, binding.Thread);
        failure = ContinuationCensusFailure.None;
        return true;
    }

    internal bool TryNotifyReservedCommittedMutations(
        ContinuationMutationEpochReservation reservation,
        IEnumerable<string> sectionIds,
        out ContinuationCensusFailure failure)
    {
        if (reservation == null
            || !ReferenceEquals(reservation.Owner, this)
            || !ReferenceEquals(reservation.Token, activeMutationEpochReservation)
            || !ReferenceEquals(reservation.OwnerThread, Thread.CurrentThread)
            || reservation.OwnerThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        bool notified = NotifyCommittedMutationsCore(sectionIds, out failure);
        activeMutationEpochReservation = null;
        return notified;
    }

    internal void ReleaseMutationEpochReservation(
        ContinuationMutationEpochReservation reservation)
    {
        if (reservation == null) return;
        if (!ReferenceEquals(reservation.Owner, this)
            || !ReferenceEquals(reservation.OwnerThread, Thread.CurrentThread))
        {
            Fault();
            return;
        }
        if (activeMutationEpochReservation == null)
            return;
        if (!ReferenceEquals(reservation.Token, activeMutationEpochReservation))
        {
            Fault();
            return;
        }
        activeMutationEpochReservation = null;
    }

    internal bool TryCompleteMutationEpochReservation(
        ContinuationMutationEpochReservation reservation,
        bool committedMutation,
        out ContinuationCensusFailure failure)
    {
        if (reservation == null
            || !ReferenceEquals(reservation.Owner, this)
            || !ReferenceEquals(reservation.Token, activeMutationEpochReservation)
            || !ReferenceEquals(reservation.OwnerThread, Thread.CurrentThread)
            || reservation.OwnerThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
        {
            Fault();
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }
        if (committedMutation)
        {
            if (mutationEpoch == long.MaxValue)
            {
                Fault();
                activeMutationEpochReservation = null;
                failure = ContinuationCensusFailure.ProtocolFaulted;
                return false;
            }
            mutationEpoch++;
        }
        activeMutationEpochReservation = null;
        failure = ContinuationCensusFailure.None;
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
        if (activeMutationEpochReservation != null)
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }
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
        OwnerThreadBinding binding = Volatile.Read(ref ownerThreadBinding);
        if (binding == null
            || !ReferenceEquals(Thread.CurrentThread, binding.Thread)
            || Thread.CurrentThread.ManagedThreadId != binding.ManagedThreadId
            || !ReferenceEquals(scopeOwnerThread, Thread.CurrentThread)
            || scopeOwnerThreadId != Thread.CurrentThread.ManagedThreadId
            || activeOperationCount <= 0)
        {
            Fault();
            return;
        }

        activeOperationCount--;
    }

    private bool TryBuildSpatialKnowledgeFamily(
        IReadOnlyList<NpcRuntime> roster,
        out List<SpatialKnowledgeSectionCandidate> candidates,
        out ContinuationCensusFailure failure)
    {
        candidates = new List<SpatialKnowledgeSectionCandidate>();
        failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        if (roster == null) return false;

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = SpatialKnowledgeCensusProvider.CreateProviders(roster);
        }
        catch
        {
            return false;
        }

        HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, Tuple<NpcRuntime, SpatialKnowledgeRuntime, int>> ownersByRuntimeId =
            new Dictionary<string, Tuple<NpcRuntime, SpatialKnowledgeRuntime, int>>(StringComparer.Ordinal);
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            if (!(provider is SpatialKnowledgeCensusProvider.ISpatialKnowledgeSectionCensusProvider spatialProvider))
                return false;

            OwnerSectionCensusWitness witness;
            try
            {
                witness = provider.GetCurrentCensus();
            }
            catch
            {
                return false;
            }

            string runtimeId = spatialProvider.RuntimeId;
            string expectedSectionId;
            int sectionKind;
            if (witness != null
                && string.Equals(witness.SectionId,
                    SpatialKnowledgeCensusProvider.LocationsSectionPrefix + runtimeId,
                    StringComparison.Ordinal))
            {
                expectedSectionId = SpatialKnowledgeCensusProvider.LocationsSectionPrefix + runtimeId;
                sectionKind = 1;
            }
            else if (witness != null
                && string.Equals(witness.SectionId,
                    SpatialKnowledgeCensusProvider.RoutesSectionPrefix + runtimeId,
                    StringComparison.Ordinal))
            {
                expectedSectionId = SpatialKnowledgeCensusProvider.RoutesSectionPrefix + runtimeId;
                sectionKind = 2;
            }
            else
            {
                return false;
            }

            OwnerSectionContract contract;
            try
            {
                contract = new OwnerSectionContract(
                    expectedSectionId,
                    SpatialKnowledgeCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(runtimeId)
                || spatialProvider.NpcOwner == null
                || spatialProvider.SpatialKnowledgeOwner == null
                || !ReferenceEquals(spatialProvider.SpatialKnowledgeOwner, witness.OwnerInstanceIdentity)
                || !string.Equals(spatialProvider.SpatialKnowledgeOwner.OwnerRuntimeId, runtimeId, StringComparison.Ordinal)
                || !sectionIds.Add(expectedSectionId)
                || !IsWitnessValidForContract(contract, witness))
            {
                return false;
            }

            if (ownersByRuntimeId.TryGetValue(runtimeId,
                    out Tuple<NpcRuntime, SpatialKnowledgeRuntime, int> existingOwner))
            {
                if (!ReferenceEquals(existingOwner.Item1, spatialProvider.NpcOwner)
                    || !ReferenceEquals(existingOwner.Item2, spatialProvider.SpatialKnowledgeOwner)
                    || (existingOwner.Item3 & sectionKind) != 0)
                {
                    return false;
                }
                ownersByRuntimeId[runtimeId] = Tuple.Create(
                    existingOwner.Item1,
                    existingOwner.Item2,
                    existingOwner.Item3 | sectionKind);
            }
            else
            {
                ownersByRuntimeId.Add(runtimeId, Tuple.Create(
                    spatialProvider.NpcOwner,
                    spatialProvider.SpatialKnowledgeOwner,
                    sectionKind));
            }

            candidates.Add(new SpatialKnowledgeSectionCandidate(
                expectedSectionId,
                contract,
                provider,
                spatialProvider.NpcOwner,
                spatialProvider.SpatialKnowledgeOwner,
                witness));
        }

        if (providers.Count != roster.Count * 2 || ownersByRuntimeId.Count != roster.Count)
            return false;
        foreach (Tuple<NpcRuntime, SpatialKnowledgeRuntime, int> owner in ownersByRuntimeId.Values)
        {
            if (owner.Item3 != 3) return false;
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryValidateSpatialKnowledgeFamilyMatchesRoster(out ContinuationCensusFailure failure)
    {
        if (!TryBuildSpatialKnowledgeFamily(spatialKnowledgeRoster,
                out List<SpatialKnowledgeSectionCandidate> candidates, out failure))
        {
            return false;
        }

        if (candidates.Count != spatialKnowledgeSectionIds.Count)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        foreach (SpatialKnowledgeSectionCandidate candidate in candidates)
        {
            if (!spatialKnowledgeSectionIds.Contains(candidate.SectionId)
                || !registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection section)
                || !spatialKnowledgeNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime npcOwner)
                || !ReferenceEquals(npcOwner, candidate.NpcOwner)
                || (section.HasBaseline
                    && !ReferenceEquals(section.OwnerInstanceIdentity, candidate.SpatialKnowledgeOwner)))
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryBuildNpcTravelStateFamily(
        IReadOnlyList<NpcRuntime> roster,
        out List<NpcTravelStateCandidate> candidates,
        out ContinuationCensusFailure failure)
    {
        candidates = new List<NpcTravelStateCandidate>();
        failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        if (roster == null) return false;

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try { providers = NpcTravelStateCensusProvider.CreateProviders(roster); }
        catch { return false; }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (IOwnerSectionCensusProvider censusProvider in providers)
        {
            if (!(censusProvider is NpcTravelStateCensusProvider provider)) return false;
            OwnerSectionCensusWitness witness;
            try { witness = provider.GetCurrentCensus(); }
            catch { return false; }

            string sectionId;
            OwnerSectionContract contract;
            try
            {
                sectionId = NpcTravelStateCensusProvider.SectionIdFor(provider.RuntimeId);
                contract = new OwnerSectionContract(
                    sectionId,
                    NpcTravelStateCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
            }
            catch { return false; }

            if (string.IsNullOrWhiteSpace(provider.RuntimeId)
                || provider.NpcOwner == null
                || !ReferenceEquals(provider.NpcOwner, witness.OwnerInstanceIdentity)
                || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                || witness.Cardinality != 1
                || witness.Revision != provider.NpcOwner.TravelStateRevision
                || !ids.Add(sectionId)
                || !IsWitnessValidForContract(contract, witness))
                return false;

            candidates.Add(new NpcTravelStateCandidate
            {
                SectionId = sectionId,
                Contract = contract,
                Provider = censusProvider,
                NpcOwner = provider.NpcOwner,
                Witness = witness
            });
        }

        if (candidates.Count != roster.Count) return false;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryValidateNpcTravelStateFamilyMatchesRoster(out ContinuationCensusFailure failure)
    {
        if (!TryBuildNpcTravelStateFamily(npcTravelStateRoster,
                out List<NpcTravelStateCandidate> candidates, out failure)) return false;
        if (candidates.Count != npcTravelStateSectionIds.Count)
        { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        foreach (NpcTravelStateCandidate candidate in candidates)
        {
            if (!npcTravelStateSectionIds.Contains(candidate.SectionId)
                || !registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection section)
                || !npcTravelStateNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime npc)
                || !ReferenceEquals(npc, candidate.NpcOwner)
                || (section.HasBaseline && !ReferenceEquals(section.OwnerInstanceIdentity, candidate.NpcOwner)))
            { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        }
        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryValidateInventoryFamilyMatchesRoster(out ContinuationCensusFailure failure)
    {
        if (!TryBuildInventoryFamily(inventoryRoster, out List<InventoryCandidate> candidates, out failure)) return false;
        if (candidates.Count != inventorySectionIds.Count) { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        foreach (InventoryCandidate c in candidates)
        {
            if (!inventorySectionIds.Contains(c.SectionId)
                || !registeredSections.TryGetValue(c.SectionId, out RegisteredSection section)
                || !inventoryNpcOwnersBySection.TryGetValue(c.SectionId, out NpcRuntime npc)
                || !ReferenceEquals(npc, c.NpcOwner)
                || (section.HasBaseline && !ReferenceEquals(section.OwnerInstanceIdentity, c.InventoryOwner)))
            { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        }
        failure = ContinuationCensusFailure.None; return true;
    }

    private static bool TryBuildMoneyAccountFamily(
        IReadOnlyList<NpcRuntime> roster,
        out List<MoneyAccountCandidate> candidates,
        out ContinuationCensusFailure failure)
    {
        candidates = new List<MoneyAccountCandidate>();
        failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
        try
        {
            foreach (IOwnerSectionCensusProvider provider in NpcMoneyAccountCensusProvider.CreateProviders(roster))
            {
                if (!(provider is NpcMoneyAccountCensusProvider.INpcMoneyAccountSectionCensusProvider accountProvider))
                    return false;

                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (witness == null) return false;
                string sectionId = NpcMoneyAccountCensusProvider.SectionPrefix + accountProvider.RuntimeId;
                var contract = new OwnerSectionContract(
                    sectionId,
                    NpcMoneyAccountCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
                if (string.IsNullOrWhiteSpace(accountProvider.RuntimeId)
                    || accountProvider.NpcOwner == null
                    || accountProvider.MoneyAccountOwner == null
                    || !ReferenceEquals(accountProvider.NpcOwner.MoneyAccount, accountProvider.MoneyAccountOwner)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, accountProvider.MoneyAccountOwner)
                    || !IsWitnessValidForContract(contract, witness))
                    return false;

                candidates.Add(new MoneyAccountCandidate
                {
                    SectionId = sectionId,
                    Contract = contract,
                    Provider = provider,
                    NpcOwner = accountProvider.NpcOwner,
                    MoneyAccountOwner = accountProvider.MoneyAccountOwner,
                    Witness = witness
                });
            }
        }
        catch
        {
            return false;
        }

        if (candidates.Count != roster.Count) return false;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryValidateMoneyAccountFamilyMatchesRoster(out ContinuationCensusFailure failure)
    {
        if (!TryBuildMoneyAccountFamily(moneyAccountRoster, out List<MoneyAccountCandidate> candidates, out failure))
            return false;
        if (candidates.Count != moneyAccountSectionIds.Count)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        foreach (MoneyAccountCandidate candidate in candidates)
        {
            if (!moneyAccountSectionIds.Contains(candidate.SectionId)
                || !registeredSections.TryGetValue(candidate.SectionId, out RegisteredSection section)
                || !moneyAccountNpcOwnersBySection.TryGetValue(candidate.SectionId, out NpcRuntime npc)
                || !ReferenceEquals(npc, candidate.NpcOwner)
                || (section.HasBaseline
                    && !ReferenceEquals(section.OwnerInstanceIdentity, candidate.MoneyAccountOwner)))
            {
                failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                return false;
            }
        }

        failure = ContinuationCensusFailure.None;
        return true;
    }

    private bool TryValidateNpcKnowledgeFamilyMatchesRoster(out ContinuationCensusFailure failure)
    {
        if (!TryBuildNpcKnowledgeFamily(npcKnowledgeRoster, out List<NpcKnowledgeCandidate> candidates, out failure)) return false;
        if (candidates.Count != npcKnowledgeSectionIds.Count) { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        foreach (NpcKnowledgeCandidate c in candidates)
        {
            if (!npcKnowledgeSectionIds.Contains(c.SectionId)
                || !registeredSections.TryGetValue(c.SectionId, out RegisteredSection section)
                || !npcKnowledgeNpcOwnersBySection.TryGetValue(c.SectionId, out NpcRuntime npc)
                || !ReferenceEquals(npc, c.NpcOwner)
                || (section.HasBaseline && !ReferenceEquals(section.OwnerInstanceIdentity, c.TypedOwner)))
            { failure = ContinuationCensusFailure.OwnerCoverageIncomplete; return false; }
        }
        failure = ContinuationCensusFailure.None; return true;
    }

    private static bool IsWitnessValidForContract(
        OwnerSectionContract contract,
        OwnerSectionCensusWitness witness)
    {
        return contract != null
            && witness != null
            && string.Equals(witness.SectionId, contract.SectionId, StringComparison.Ordinal)
            && witness.SchemaVersion == contract.SchemaVersion
            && witness.OwnerInstanceIdentity != null
            && witness.Cardinality >= 0
            && witness.Revision >= 0L
            && ((contract.Role != OwnerSectionRole.ExplicitlyEmpty
                    && contract.Role != OwnerSectionRole.Excluded)
                || witness.Cardinality == 0);
    }

    private static RegisteredSection CloneRegisteredSection(RegisteredSection source)
    {
        RegisteredSection clone = new RegisteredSection(source.Contract, source.Provider)
        {
            OwnerInstanceIdentity = source.OwnerInstanceIdentity,
            LastCardinality = source.LastCardinality,
            LastRevision = source.LastRevision,
            HasBaseline = source.HasBaseline
        };
        return clone;
    }

    private static long GetBaselineRevision(RegisteredSection section)
    {
        return section != null && section.HasBaseline ? section.LastRevision : -1L;
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

    internal void FaultClosed() => Fault();

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
