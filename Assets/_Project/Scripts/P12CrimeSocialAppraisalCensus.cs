using System;
using System.Collections.Generic;
using System.Threading;

internal enum P12CrimeSocialAppraisalOperation
{
    TheftAcceptance,
    KnowledgeAndAppraisal
}

internal interface IP12CrimeSocialAppraisalOwnerMutationBoundary
{
    bool CanCommit(object owner);
    void Committed(object owner);
    void Fault();
}

internal interface IP12CrimeSocialAppraisalOperationBoundary
{
    bool TryBeginOperation(P12CrimeSocialAppraisalOperation operation);
    bool EndOperation(P12CrimeSocialAppraisalOperation operation);
}

/// <summary>Exact selected-profile witnesses for the three CrimeSocialAppraisal stores.</summary>
public static class P12CrimeSocialAppraisalCensusProvider
{
    public const string OutcomesSectionId = "p12.crime-social-appraisal.outcomes";
    public const string KnowledgeSectionId = "p12.crime-social-appraisal.knowledge";
    public const string ReactionsSectionId = "p12.crime-social-appraisal.reactions";
    public const int SchemaVersion = 1;

    private sealed class SectionProvider<TOwner> : IOwnerSectionCensusProvider where TOwner : class
    {
        private readonly string sectionId;
        private readonly TOwner owner;
        private readonly Func<TOwner, int> count;
        private readonly Func<TOwner, long> revision;

        public SectionProvider(string sectionId, TOwner owner, Func<TOwner, int> count, Func<TOwner, long> revision)
        {
            this.sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.count = count ?? throw new ArgumentNullException(nameof(count));
            this.revision = revision ?? throw new ArgumentNullException(nameof(revision));
        }

        public TOwner Owner => owner;

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, count(owner), revision(owner));
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        CrimeSocialAppraisalWorldState world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        return Array.AsReadOnly(new IOwnerSectionCensusProvider[]
        {
            new SectionProvider<TheftOutcomeStore>(
                OutcomesSectionId, world.TheftOutcomes, owner => owner.Count, owner => owner.P12CensusRevision),
            new SectionProvider<CrimeKnowledgeStore>(
                KnowledgeSectionId, world.CrimeKnowledge, owner => owner.Count, owner => owner.P12CensusRevision),
            new SectionProvider<SocialReactionStore>(
                ReactionsSectionId, world.SocialReactions, owner => owner.Count, owner => owner.P12CensusRevision)
        });
    }

    internal static bool TryRegister(
        ContinuationCensusProtocol protocol,
        CrimeSocialAppraisalWorldState world)
    {
        if (protocol == null || world == null
            || world.TheftOutcomes == null || world.CrimeKnowledge == null
            || world.SocialReactions == null || world.Integration == null)
            return false;

        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateProviders(world);
            string[] requiredIds = { OutcomesSectionId, KnowledgeSectionId, ReactionsSectionId };
            if (providers.Count != requiredIds.Length) return false;
            for (int i = 0; i < providers.Count; i++)
            {
                IOwnerSectionCensusProvider provider = providers[i];
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (witness == null
                    || !string.Equals(witness.SectionId, requiredIds[i], StringComparison.Ordinal)
                    || witness.SchemaVersion != SchemaVersion
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, GetExpectedOwner(world, i))
                    || witness.Cardinality != 0
                    || witness.Revision != 0L)
                    return false;

                OwnerSectionContract contract = new OwnerSectionContract(
                    requiredIds[i], SchemaVersion, OwnerSectionRole.Required);
                if (!protocol.RegisterExpectedSection(contract, out _)
                    || !protocol.RegisterCensusProvider(requiredIds[i], provider, out _))
                    return false;
            }
            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private static object GetExpectedOwner(CrimeSocialAppraisalWorldState world, int index) => index switch
    {
        0 => world.TheftOutcomes,
        1 => world.CrimeKnowledge,
        2 => world.SocialReactions,
        _ => null
    };
}

/// <summary>
/// P12-B bridge for one selected CrimeSocialAppraisalWorldState. It admits only
/// the existing outer theft and knowledge/appraisal call graphs, or one direct
/// store write at a time.
/// </summary>
internal sealed class P12CrimeSocialAppraisalMutationCoordinator :
    IP12CrimeSocialAppraisalOwnerMutationBoundary,
    IP12CrimeSocialAppraisalOperationBoundary
{
    private enum EpochMode
    {
        Reserved,
        Immediate
    }

    private enum TheftStage
    {
        BeforeNestedKnowledge,
        NestedKnowledge,
        AfterNestedKnowledge
    }

    private sealed class OwnerEntry
    {
        public readonly object Owner;
        public readonly string SectionId;
        public readonly Func<long> ReadRevision;

        public OwnerEntry(object owner, string sectionId, Func<long> readRevision)
        {
            Owner = owner;
            SectionId = sectionId;
            ReadRevision = readRevision;
        }
    }

    private sealed class MutationContext
    {
        public readonly bool IsDirect;
        public readonly object DirectOwner;
        public readonly P12CrimeSocialAppraisalOperation Operation;
        public readonly Thread OwnerThread;
        public readonly int OwnerThreadId;
        public readonly EpochMode Mode;
        public readonly ContinuationMutationEpochReservation Reservation;
        public readonly Dictionary<object, int> RemainingWrites = new Dictionary<object, int>();
        public readonly Dictionary<object, long> LastObservedRevision = new Dictionary<object, long>();
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public TheftStage Stage;
        public bool NestedKnowledgeUsed;

        public MutationContext(
            bool isDirect,
            object directOwner,
            P12CrimeSocialAppraisalOperation operation,
            EpochMode mode,
            ContinuationMutationEpochReservation reservation)
        {
            IsDirect = isDirect;
            DirectOwner = directOwner;
            Operation = operation;
            Mode = mode;
            Reservation = reservation;
            OwnerThread = Thread.CurrentThread;
            OwnerThreadId = OwnerThread.ManagedThreadId;
        }

        public bool IsOnOwnerThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private readonly ContinuationCensusProtocol protocol;
    private readonly Action faultRuntime;
    private readonly CrimeSocialAppraisalWorldState world;
    private readonly OwnerEntry[] owners;
    private MutationContext activeContext;

    public P12CrimeSocialAppraisalMutationCoordinator(
        ContinuationCensusProtocol protocol,
        CrimeSocialAppraisalWorldState world,
        Action faultRuntime)
    {
        this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.faultRuntime = faultRuntime ?? throw new ArgumentNullException(nameof(faultRuntime));
        owners = new[]
        {
            new OwnerEntry(world.TheftOutcomes, P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId,
                () => world.TheftOutcomes.P12CensusRevision),
            new OwnerEntry(world.CrimeKnowledge, P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId,
                () => world.CrimeKnowledge.P12CensusRevision),
            new OwnerEntry(world.SocialReactions, P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId,
                () => world.SocialReactions.P12CensusRevision)
        };
    }

    public bool TryBind()
    {
        return world.TheftOutcomes.TryBindP12MutationBoundary(this)
            && world.CrimeKnowledge.TryBindP12MutationBoundary(this)
            && world.SocialReactions.TryBindP12MutationBoundary(this)
            && world.Integration.TryBindP12OperationBoundary(this);
    }

    public bool TryBeginOperation(P12CrimeSocialAppraisalOperation operation)
    {
        if (activeContext != null)
        {
            MutationContext current = activeContext;
            if (operation == P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal
                && !current.IsDirect
                && current.Operation == P12CrimeSocialAppraisalOperation.TheftAcceptance
                && current.Stage == TheftStage.BeforeNestedKnowledge
                && !current.NestedKnowledgeUsed
                && current.IsOnOwnerThread())
            {
                current.NestedKnowledgeUsed = true;
                current.Stage = TheftStage.NestedKnowledge;
                return true;
            }
            Fault();
            return false;
        }

        int[] writeBudgets = operation == P12CrimeSocialAppraisalOperation.TheftAcceptance
            ? new[] { 2, 2, 4 }
            : new[] { 0, 2, 4 };
        List<OwnerEntry> relevant = new List<OwnerEntry>();
        for (int i = 0; i < owners.Length; i++)
            if (writeBudgets[i] > 0) relevant.Add(owners[i]);
        if (!TryOpenContext(false, null, operation, relevant, writeBudgets)) return false;
        if (operation == P12CrimeSocialAppraisalOperation.TheftAcceptance)
            activeContext.Stage = TheftStage.BeforeNestedKnowledge;
        return true;
    }

    public bool EndOperation(P12CrimeSocialAppraisalOperation operation)
    {
        MutationContext current = activeContext;
        if (current == null || !current.IsOnOwnerThread())
        {
            Fault();
            return false;
        }

        if (!current.IsDirect
            && current.Operation == P12CrimeSocialAppraisalOperation.TheftAcceptance
            && operation == P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal
            && current.Stage == TheftStage.NestedKnowledge)
        {
            current.Stage = TheftStage.AfterNestedKnowledge;
            return true;
        }

        if (current.IsDirect || current.Operation != operation)
        {
            Fault();
            return false;
        }
        return CloseContext(current);
    }

    public bool CanCommit(object owner)
    {
        OwnerEntry entry = FindOwner(owner);
        if (entry == null)
        {
            Fault();
            return false;
        }

        MutationContext current = activeContext;
        if (current == null)
        {
            int index = Array.IndexOf(owners, entry);
            int[] budget = { 0, 0, 0 };
            budget[index] = 1;
            if (!TryOpenContext(true, owner, P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal,
                    new[] { entry }, budget))
                return false;
            current = activeContext;
        }

        if (!current.IsOnOwnerThread()
            || (current.IsDirect && !ReferenceEquals(current.DirectOwner, owner))
            || !IsOwnerAllowedInCurrentStage(current, entry)
            || !current.RemainingWrites.TryGetValue(owner, out int remaining)
            || remaining <= 0
            || entry.ReadRevision() != current.LastObservedRevision[owner]
            || entry.ReadRevision() == long.MaxValue)
        {
            Fault();
            return false;
        }

        current.RemainingWrites[owner] = remaining - 1;
        return true;
    }

    public void Committed(object owner)
    {
        MutationContext current = activeContext;
        OwnerEntry entry = FindOwner(owner);
        if (current == null || entry == null || !current.IsOnOwnerThread()
            || !current.LastObservedRevision.TryGetValue(owner, out long lastRevision)
            || lastRevision == long.MaxValue
            || entry.ReadRevision() != lastRevision + 1L)
        {
            Fault();
            return;
        }

        current.LastObservedRevision[owner] = entry.ReadRevision();
        current.ChangedSectionIds.Add(entry.SectionId);
        if (current.IsDirect) CloseContext(current);
    }

    public void Fault() => faultRuntime();

    private bool TryOpenContext(
        bool isDirect,
        object directOwner,
        P12CrimeSocialAppraisalOperation operation,
        IReadOnlyList<OwnerEntry> relevantOwners,
        int[] writeBudgets)
    {
        if (activeContext != null || relevantOwners == null || relevantOwners.Count == 0)
        {
            Fault();
            return false;
        }

        string[] sectionIds = new string[relevantOwners.Count];
        for (int i = 0; i < relevantOwners.Count; i++)
        {
            OwnerEntry entry = relevantOwners[i];
            int ownerIndex = Array.IndexOf(owners, entry);
            if (ownerIndex < 0 || writeBudgets[ownerIndex] <= 0
                || entry.ReadRevision() > long.MaxValue - writeBudgets[ownerIndex])
            {
                return false;
            }
            sectionIds[i] = entry.SectionId;
        }

        if (!protocol.TryValidateUnchangedSections(sectionIds, out _))
            return false;

        EpochMode mode;
        ContinuationMutationEpochReservation reservation = null;
        if (protocol.TryReserveMutationEpochCapacity(out reservation, out ContinuationCensusFailure reserveFailure))
        {
            mode = EpochMode.Reserved;
        }
        else if (reserveFailure == ContinuationCensusFailure.OperationInProgress
            && protocol.TryValidateMutationEpochCapacity(out _))
        {
            mode = EpochMode.Immediate;
        }
        else
        {
            return false;
        }

        MutationContext context = new MutationContext(isDirect, directOwner, operation, mode, reservation);
        foreach (OwnerEntry entry in relevantOwners)
        {
            int ownerIndex = Array.IndexOf(owners, entry);
            context.RemainingWrites.Add(entry.Owner, writeBudgets[ownerIndex]);
            context.LastObservedRevision.Add(entry.Owner, entry.ReadRevision());
        }
        activeContext = context;
        return true;
    }

    private bool IsOwnerAllowedInCurrentStage(MutationContext context, OwnerEntry entry)
    {
        if (context.IsDirect) return true;
        if (context.Operation == P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal)
            return !ReferenceEquals(entry.Owner, world.TheftOutcomes);
        if (context.Operation != P12CrimeSocialAppraisalOperation.TheftAcceptance) return false;
        if (context.Stage == TheftStage.BeforeNestedKnowledge
            || context.Stage == TheftStage.AfterNestedKnowledge)
            return ReferenceEquals(entry.Owner, world.TheftOutcomes);
        return context.Stage == TheftStage.NestedKnowledge
            && !ReferenceEquals(entry.Owner, world.TheftOutcomes);
    }

    private bool CloseContext(MutationContext context)
    {
        if (!ReferenceEquals(activeContext, context) || !context.IsOnOwnerThread())
        {
            Fault();
            return false;
        }

        activeContext = null;
        if (context.ChangedSectionIds.Count == 0)
        {
            if (context.Reservation != null)
                protocol.ReleaseMutationEpochReservation(context.Reservation);
            return true;
        }

        bool notified = context.Mode == EpochMode.Reserved
            ? protocol.TryNotifyReservedCommittedMutations(
                context.Reservation, context.ChangedSectionIds, out _)
            : protocol.NotifyCommittedMutations(context.ChangedSectionIds, out _);
        if (!notified) Fault();
        return notified;
    }

    private OwnerEntry FindOwner(object owner)
    {
        foreach (OwnerEntry entry in owners)
            if (ReferenceEquals(entry.Owner, owner)) return entry;
        return null;
    }
}

public sealed partial class SimulationRuntime
{
    private P12CrimeSocialAppraisalMutationCoordinator p12CrimeSocialAppraisalMutationCoordinator;

    private bool TryRegisterP12CrimeSocialAppraisalOwnerSections(ContinuationCensusProtocol protocol)
    {
        return P12CrimeSocialAppraisalCensusProvider.TryRegister(protocol, crimeSocialAppraisalWorldState);
    }

    private bool TryBindP12CrimeSocialAppraisalMutationBoundaries()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent() || npcRosterCensusProtocol == null
            || crimeSocialAppraisalWorldState == null)
            return false;

        if (p12CrimeSocialAppraisalMutationCoordinator != null)
            return p12CrimeSocialAppraisalMutationCoordinator.TryBind();

        P12CrimeSocialAppraisalMutationCoordinator coordinator =
            new P12CrimeSocialAppraisalMutationCoordinator(
                npcRosterCensusProtocol,
                crimeSocialAppraisalWorldState,
                FaultRuntimeAdmission);
        if (!coordinator.TryBind()) return false;
        p12CrimeSocialAppraisalMutationCoordinator = coordinator;
        return true;
    }
}
