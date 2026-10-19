using System;
using System.Collections.Generic;

[Serializable]
public sealed class TravelPartyMemberCost
{
    private readonly string runtimeId;
    private readonly float amount;

    public string RuntimeId => runtimeId;
    public float Amount => amount;

    public TravelPartyMemberCost(string runtimeId, float amount)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Travel party cost requires a member RuntimeId.", nameof(runtimeId));
        }

        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        this.runtimeId = runtimeId;
        this.amount = amount;
    }
}

[Serializable]
public sealed class TravelPartyRuntime
{
    private readonly string travelPartyId;
    private readonly string originLocationRuntimeId;
    private readonly string destinationLocationRuntimeId;
    private readonly string routeRuntimeId;
    private readonly IReadOnlyList<string> travelerRuntimeIds;
    private readonly IReadOnlyList<string> escortRuntimeIds;
    private readonly IReadOnlyList<string> memberRuntimeIds;
    private readonly IReadOnlyList<TravelPartyMemberCost> memberCosts;
    private readonly int travelDaysTotal;
    private readonly string originDecisionId;
    private bool completed;

    public string TravelPartyId => travelPartyId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;
    public IReadOnlyList<string> TravelerRuntimeIds => travelerRuntimeIds;
    public IReadOnlyList<string> EscortRuntimeIds => escortRuntimeIds;
    public IReadOnlyList<string> MemberRuntimeIds => memberRuntimeIds;
    public IReadOnlyList<TravelPartyMemberCost> MemberCosts => memberCosts;
    public int TravelDaysTotal => travelDaysTotal;
    public string OriginDecisionId => originDecisionId;
    public bool IsCompleted => completed;
    public bool IsActive => completed == false;

    public TravelPartyRuntime(
        string travelPartyId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string routeRuntimeId,
        IEnumerable<string> travelerRuntimeIds,
        IEnumerable<string> escortRuntimeIds,
        int travelDaysTotal,
        string originDecisionId,
        IEnumerable<TravelPartyMemberCost> memberCosts)
    {
        this.travelPartyId = RequireId(travelPartyId, nameof(travelPartyId));
        this.originLocationRuntimeId = RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.destinationLocationRuntimeId = RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
        this.routeRuntimeId = RequireId(routeRuntimeId, nameof(routeRuntimeId));

        if (travelDaysTotal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(travelDaysTotal));
        }

        this.travelDaysTotal = travelDaysTotal;
        this.originDecisionId = NormalizeOptionalId(originDecisionId);
        this.travelerRuntimeIds = CaptureIds(travelerRuntimeIds, nameof(travelerRuntimeIds));
        this.escortRuntimeIds = CaptureIds(escortRuntimeIds, nameof(escortRuntimeIds));

        if (this.travelerRuntimeIds.Count == 0)
        {
            throw new ArgumentException("Travel party requires at least one traveler.", nameof(travelerRuntimeIds));
        }

        HashSet<string> allMemberIds = new HashSet<string>(StringComparer.Ordinal);
        List<string> allMembers = new List<string>();

        foreach (string runtimeId in this.travelerRuntimeIds)
        {
            if (allMemberIds.Add(runtimeId) == false)
            {
                throw new ArgumentException("Travel party members must be distinct.", nameof(travelerRuntimeIds));
            }

            allMembers.Add(runtimeId);
        }

        foreach (string runtimeId in this.escortRuntimeIds)
        {
            if (allMemberIds.Add(runtimeId) == false)
            {
                throw new ArgumentException("Travel party members must be distinct.", nameof(escortRuntimeIds));
            }

            allMembers.Add(runtimeId);
        }

        memberRuntimeIds = allMembers.AsReadOnly();
        this.memberCosts = CaptureCosts(memberCosts);

        if (this.memberCosts.Count != memberRuntimeIds.Count)
        {
            throw new ArgumentException("Travel party requires one cost snapshot per member.", nameof(memberCosts));
        }

        foreach (TravelPartyMemberCost cost in this.memberCosts)
        {
            if (allMemberIds.Contains(cost.RuntimeId) == false)
            {
                throw new ArgumentException("Travel party cost references an unknown member.", nameof(memberCosts));
            }
        }
    }

    public TravelPartyMemberCost GetCostForMember(string runtimeId)
    {
        foreach (TravelPartyMemberCost cost in memberCosts)
        {
            if (cost != null && string.Equals(cost.RuntimeId, runtimeId, StringComparison.Ordinal) == true)
            {
                return cost;
            }
        }

        return null;
    }

    internal void MarkCompleted()
    {
        completed = true;
    }

    internal static TravelPartyRuntime CreateActiveFromOwnerSnapshot(
        string travelPartyId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string routeRuntimeId,
        IEnumerable<string> travelerRuntimeIds,
        IEnumerable<string> escortRuntimeIds,
        int travelDaysTotal,
        string originDecisionId,
        IEnumerable<TravelPartyMemberCost> memberCosts)
    {
        return new TravelPartyRuntime(
            travelPartyId, originLocationRuntimeId, destinationLocationRuntimeId, routeRuntimeId,
            travelerRuntimeIds, escortRuntimeIds, travelDaysTotal, originDecisionId, memberCosts);
    }

    private static IReadOnlyList<string> CaptureIds(IEnumerable<string> source, string parameterName)
    {
        List<string> snapshot = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        if (source != null)
        {
            foreach (string runtimeId in source)
            {
                if (string.IsNullOrWhiteSpace(runtimeId) == true || uniqueIds.Add(runtimeId) == false)
                {
                    throw new ArgumentException("Travel party member RuntimeIds must be non-empty and unique.", parameterName);
                }

                snapshot.Add(runtimeId);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static IReadOnlyList<TravelPartyMemberCost> CaptureCosts(IEnumerable<TravelPartyMemberCost> source)
    {
        List<TravelPartyMemberCost> snapshot = new List<TravelPartyMemberCost>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        if (source != null)
        {
            foreach (TravelPartyMemberCost cost in source)
            {
                if (cost == null || uniqueIds.Add(cost.RuntimeId) == false)
                {
                    throw new ArgumentException("Travel party member costs must be unique.", nameof(source));
                }

                snapshot.Add(cost);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Travel party requires stable IDs.", parameterName);
        }

        return value;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

public sealed class TravelPartyStore : IAuthoritativeMutationGuardBindable
{
    private readonly object mutationSync = new object();
    private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    private readonly List<TravelPartyRuntime> activeParties = new List<TravelPartyRuntime>();
    private readonly Dictionary<string, TravelPartyRuntime> partiesById = new Dictionary<string, TravelPartyRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<TravelPartyRuntime> readOnlyActiveParties;
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();

    public IReadOnlyList<TravelPartyRuntime> ActiveParties => readOnlyActiveParties;
    public long Revision => revision;

    public TravelPartyStore()
    {
        readOnlyActiveParties = activeParties.AsReadOnly();
    }

    public TravelPartyRuntime GetById(string travelPartyId)
    {
        return travelPartyId != null && partiesById.TryGetValue(travelPartyId, out TravelPartyRuntime party) == true
            ? party
            : null;
    }

    public bool Add(TravelPartyRuntime party)
    {
        using (EnterMutationWindow())
        {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        if (party == null || party.IsActive == false || partiesById.ContainsKey(party.TravelPartyId) == true)
        {
            return false;
        }

        foreach (string memberRuntimeId in party.MemberRuntimeIds)
        {
            if (TryGetPartyForNpc(memberRuntimeId, out _))
            {
                return false;
            }
        }

        if (!CanCommitMutations(1)) return false;
        if (!CanCommitP12Mutation()) return false;

        partiesById.Add(party.TravelPartyId, party);
        activeParties.Add(party);
        revision++;
        NotifyP12MutationCommitted();
        return true;
        }
    }

    public bool TryGetPartyForNpc(string npcRuntimeId, out TravelPartyRuntime party)
    {
        party = null;

        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return false;
        }

        foreach (TravelPartyRuntime candidate in activeParties)
        {
            if (candidate != null && ContainsMember(candidate.MemberRuntimeIds, npcRuntimeId) == true)
            {
                party = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMember(IReadOnlyList<string> memberRuntimeIds, string npcRuntimeId)
    {
        if (memberRuntimeIds == null)
        {
            return false;
        }

        foreach (string memberRuntimeId in memberRuntimeIds)
        {
            if (string.Equals(memberRuntimeId, npcRuntimeId, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    public bool Complete(string travelPartyId)
    {
        using (EnterMutationWindow())
        {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        TravelPartyRuntime party = GetById(travelPartyId);

        if (party == null)
        {
            return false;
        }

        if (!CanCommitMutations(1)) return false;
        if (!CanCommitP12Mutation()) return false;

        party.MarkCompleted();
        partiesById.Remove(travelPartyId);
        activeParties.Remove(party);
        revision++;
        NotifyP12MutationCommitted();
        return true;
        }
    }

    public bool Remove(string travelPartyId)
    {
        using (EnterMutationWindow())
        {
        if (!mutationGuardBinding.CanMutate)
        {
            return false;
        }

        TravelPartyRuntime party = GetById(travelPartyId);

        if (party == null)
        {
            return false;
        }

        if (!CanCommitMutations(1)) return false;
        if (!CanCommitP12Mutation()) return false;

        partiesById.Remove(travelPartyId);
        activeParties.Remove(party);
        revision++;
        NotifyP12MutationCommitted();
        return true;
        }
    }

    internal IDisposable EnterMutationWindow()
    {
        System.Threading.Monitor.Enter(mutationSync);
        return new MutationWindow(mutationSync);
    }

    internal IDisposable EnterReadWindow()
    {
        System.Threading.Monitor.Enter(mutationSync);
        return new MutationWindow(mutationSync);
    }

    internal bool ValidateCensus(out int count, out long currentRevision)
    {
        count = 0;
        currentRevision = revision;
        if (revision < 0 || activeParties.Count != partiesById.Count) return false;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> members = new HashSet<string>(StringComparer.Ordinal);
        foreach (TravelPartyRuntime party in activeParties)
        {
            if (party == null || !party.IsActive || string.IsNullOrWhiteSpace(party.TravelPartyId)
                || !ids.Add(party.TravelPartyId)
                || !partiesById.TryGetValue(party.TravelPartyId, out TravelPartyRuntime indexed)
                || !ReferenceEquals(indexed, party)) return false;
            foreach (string memberId in party.MemberRuntimeIds)
                if (string.IsNullOrWhiteSpace(memberId) || !members.Add(memberId)) return false;
        }
        foreach (KeyValuePair<string, TravelPartyRuntime> pair in partiesById)
            if (pair.Value == null || !ids.Contains(pair.Key)
                || !string.Equals(pair.Key, pair.Value.TravelPartyId, StringComparison.Ordinal)) return false;
        count = activeParties.Count;
        return true;
    }

    internal static bool TryCreateFromOwnerRows(
        IEnumerable<TravelPartyRuntime> parties,
        long ownerRevision,
        out TravelPartyStore stagedStore)
    {
        stagedStore = null;
        if (parties == null || ownerRevision < 0) return false;

        TravelPartyStore staged = new TravelPartyStore();
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        int partyCount = 0;
        foreach (TravelPartyRuntime party in parties)
        {
            if (party == null || !party.IsActive || !staged.Add(party)) return false;
            partyCount++;
            foreach (string memberId in party.MemberRuntimeIds)
                if (!memberIds.Add(memberId)) return false;
        }

        if (ownerRevision < partyCount) return false;
        staged.revision = ownerRevision;
        stagedStore = staged;
        return true;
    }

    internal bool CanCommitMutations(int count)
    {
        return count >= 0 && revision <= long.MaxValue - count;
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("TravelPartyStore is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }

    private sealed class MutationWindow : IDisposable
    {
        private object sync;
        public MutationWindow(object sync) { this.sync = sync; }
        public void Dispose()
        {
            object current = sync;
            if (current == null) return;
            sync = null;
            System.Threading.Monitor.Exit(current);
        }
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }
}
