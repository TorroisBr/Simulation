using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/// <summary>Stable identity of a domain-owned offer of allocatable manpower.</summary>
public sealed class ManpowerSourceId : IEquatable<ManpowerSourceId>
{
    public string Value { get; }

    public ManpowerSourceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ManpowerSourceId requires a non-empty value.", nameof(value));
        Value = value;
    }

    public bool Equals(ManpowerSourceId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as ManpowerSourceId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(ManpowerSourceId left, ManpowerSourceId right)
        => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && left.Equals(right));
    public static bool operator !=(ManpowerSourceId left, ManpowerSourceId right) => !(left == right);
}

public enum ManpowerInjuryState { Healthy = 0, Wounded = 1 }
public enum ManpowerCustodyState { Free = 0, Captured = 1 }
public enum ManpowerAvailabilityState { Available = 0, Unavailable = 1 }

/// <summary>A positive, aggregate cohort. The three independent dimensions may overlap.</summary>
public sealed class ContingentManpowerCohort
{
    public ManpowerInjuryState InjuryState { get; }
    public ManpowerCustodyState CustodyState { get; }
    public ArmedForceId CustodianForceId { get; }
    public ManpowerAvailabilityState AvailabilityState { get; }
    public long Amount { get; }

    public ContingentManpowerCohort(
        ManpowerInjuryState injuryState,
        ManpowerCustodyState custodyState,
        ArmedForceId custodianForceId,
        ManpowerAvailabilityState availabilityState,
        long amount)
    {
        InjuryState = injuryState;
        CustodyState = custodyState;
        CustodianForceId = custodianForceId;
        AvailabilityState = availabilityState;
        Amount = amount;
    }

    internal string Key => ((int)InjuryState).ToString(CultureInfo.InvariantCulture) + ":"
        + ((int)CustodyState).ToString(CultureInfo.InvariantCulture) + ":"
        + (CustodianForceId?.Value ?? string.Empty) + ":"
        + ((int)AvailabilityState).ToString(CultureInfo.InvariantCulture);
}

/// <summary>Immutable, read-only capacity/factual view supplied by the source domain.</summary>
public sealed class ManpowerSourceCapacitySnapshot
{
    public ManpowerSourceId SourceId { get; }
    public long Capacity { get; }
    public long? FactualLivingAmount { get; }
    public string Fingerprint { get; }

    public ManpowerSourceCapacitySnapshot(
        ManpowerSourceId sourceId,
        long capacity,
        long? factualLivingAmount,
        string fingerprint)
    {
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        if (capacity < 0L || (factualLivingAmount.HasValue && factualLivingAmount.Value < 0L))
            throw new ArgumentOutOfRangeException(nameof(capacity), "Source quantities cannot be negative.");
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException("A source snapshot requires a stable fingerprint.", nameof(fingerprint));
        Capacity = capacity;
        FactualLivingAmount = factualLivingAmount;
        Fingerprint = fingerprint;
    }
}

/// <summary>Read-only bridge into the domain that owns a manpower source.</summary>
public interface IManpowerSourceSnapshotProvider
{
    /// <summary>Fingerprint must change whenever capacity or factual living amount changes.</summary>
    bool TryGetSnapshot(ManpowerSourceId sourceId, out ManpowerSourceCapacitySnapshot snapshot);
}

public enum ContingentManpowerFailureCode
{
    None = 0,
    InvalidContingent = 1,
    ContingentNotRegistered = 2,
    ContingentAlreadyRegistered = 3,
    ForceNotRegistered = 4,
    ForceNotActive = 5,
    InvalidAmount = 6,
    InvalidCohort = 7,
    CapturedCohortMustBeUnavailable = 8,
    InvalidCustodian = 9,
    CohortAmountOverflow = 10,
    RosterAmountOverflow = 11,
    RevisionOverflow = 12,
    StaleContingentState = 13,
    StaleSourceSnapshot = 14,
    SourceAuthorityUnavailable = 15,
    SourceUnresolved = 16,
    SourceCapacityExceeded = 17,
    SourceFactualAmountExceeded = 18,
    SourceBindingRequired = 19,
    SourceBindingChangeRequiresEmptyRoster = 20,
    AmountMirrorMismatch = 21,
    InitialRosterMustBeAllocated = 22,
    CohortNotPresent = 23,
    CapturedRosterCannotBeDemobilized = 24,
    RedistributionChangesLivingRoster = 25,
    ForceHasDirectLivingRoster = 26,
    ForceIsCustodianOfCapturedRoster = 27,
    InvalidInvariant = 28
}

public sealed class ContingentManpowerFailure
{
    private static readonly ContingentManpowerFailure none =
        new ContingentManpowerFailure(ContingentManpowerFailureCode.None, string.Empty);

    private ContingentManpowerFailure(ContingentManpowerFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static ContingentManpowerFailure None => none;
    public ContingentManpowerFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != ContingentManpowerFailureCode.None;
    internal static ContingentManpowerFailure Create(ContingentManpowerFailureCode code, string message)
        => code == ContingentManpowerFailureCode.None ? None : new ContingentManpowerFailure(code, message);
    public override string ToString() => Code + (Message.Length == 0 ? string.Empty : ": " + Message);
}

/// <summary>Canonical per-contingent military roster and source binding.</summary>
public sealed class ContingentManpowerState
{
    public ContingentId ContingentId { get; }
    public ManpowerSourceId SourceId { get; }
    public IReadOnlyList<ContingentManpowerCohort> Cohorts { get; }
    public long Revision { get; }
    public long LivingRosterAmount { get; }
    public long AvailableAmount { get; }
    public string Fingerprint { get; }

    internal ContingentManpowerState(
        ContingentId contingentId,
        ManpowerSourceId sourceId,
        IEnumerable<ContingentManpowerCohort> cohorts,
        long revision)
    {
        ContingentId = contingentId ?? throw new ArgumentNullException(nameof(contingentId));
        SourceId = sourceId;
        List<ContingentManpowerCohort> values = cohorts == null
            ? new List<ContingentManpowerCohort>()
            : new List<ContingentManpowerCohort>(cohorts);
        values.Sort(CompareCohorts);
        Cohorts = new ReadOnlyCollection<ContingentManpowerCohort>(values);
        Revision = revision;
        long living = 0L;
        long available = 0L;
        foreach (ContingentManpowerCohort cohort in values)
        {
            living = checked(living + cohort.Amount);
            if (cohort.AvailabilityState == ManpowerAvailabilityState.Available)
                available = checked(available + cohort.Amount);
        }
        LivingRosterAmount = living;
        AvailableAmount = available;
        Fingerprint = BuildFingerprint(contingentId, sourceId, values);
    }

    internal static ContingentManpowerState FromLegacy(ContingentRecord contingent)
    {
        List<ContingentManpowerCohort> cohorts = new List<ContingentManpowerCohort>();
        if (contingent.Amount > 0L)
            cohorts.Add(new ContingentManpowerCohort(
                ManpowerInjuryState.Healthy,
                ManpowerCustodyState.Free,
                null,
                ManpowerAvailabilityState.Available,
                contingent.Amount));
        return new ContingentManpowerState(contingent.Id, null, cohorts, 0L);
    }

    private static string BuildFingerprint(
        ContingentId id,
        ManpowerSourceId sourceId,
        IEnumerable<ContingentManpowerCohort> cohorts)
    {
        StringBuilder result = new StringBuilder();
        Append(result, id?.Value);
        Append(result, sourceId?.Value);
        foreach (ContingentManpowerCohort cohort in cohorts)
        {
            Append(result, cohort.InjuryState.ToString());
            Append(result, cohort.CustodyState.ToString());
            Append(result, cohort.CustodianForceId?.Value);
            Append(result, cohort.AvailabilityState.ToString());
            Append(result, cohort.Amount.ToString(CultureInfo.InvariantCulture));
        }
        return result.ToString();
    }

    internal static int CompareCohorts(ContingentManpowerCohort left, ContingentManpowerCohort right)
    {
        int c = left.InjuryState.CompareTo(right.InjuryState);
        if (c != 0) return c;
        c = left.CustodyState.CompareTo(right.CustodyState);
        if (c != 0) return c;
        c = StringComparer.Ordinal.Compare(left.CustodianForceId?.Value, right.CustodianForceId?.Value);
        return c != 0 ? c : left.AvailabilityState.CompareTo(right.AvailabilityState);
    }

    private static void Append(StringBuilder builder, string value)
    {
        if (value == null) { builder.Append("-;"); return; }
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
    }
}

public sealed class ContingentManpowerInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;

    internal ContingentManpowerInvariantReport(IEnumerable<string> violations)
    {
        List<string> values = violations == null ? new List<string>() : new List<string>(violations);
        values.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(values);
    }
}

/// <summary>
/// Owns contingent source bindings and cohort rosters. It never mutates the source domain or population.
/// </summary>
public sealed class ContingentManpowerStateStore
{
    private readonly ArmedForceStore armedForceStore;
    private readonly IManpowerSourceSnapshotProvider sourceProvider;
    private readonly Dictionary<string, ContingentManpowerState> states =
        new Dictionary<string, ContingentManpowerState>(StringComparer.Ordinal);
    private long revision;

    public ContingentManpowerStateStore(
        ArmedForceStore armedForceStore,
        IManpowerSourceSnapshotProvider sourceProvider = null)
        : this(armedForceStore, sourceProvider, true)
    {
    }

    internal ContingentManpowerStateStore(
        ArmedForceStore armedForceStore,
        IManpowerSourceSnapshotProvider sourceProvider,
        bool initializeLegacyStates)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.sourceProvider = sourceProvider;
        if (initializeLegacyStates)
        {
            foreach (ContingentRecord contingent in armedForceStore.Contingents)
                states.Add(contingent.Id.Value, ContingentManpowerState.FromLegacy(contingent));
            AttachToArmedForceStore();
        }
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public IManpowerSourceSnapshotProvider SourceProvider => sourceProvider;
    public long Revision => revision;
    public IReadOnlyList<ContingentManpowerState> States
    {
        get
        {
            List<ContingentManpowerState> result = new List<ContingentManpowerState>(states.Values);
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.ContingentId.Value, b.ContingentId.Value));
            return new ReadOnlyCollection<ContingentManpowerState>(result);
        }
    }

    public bool TryGet(ContingentId id, out ContingentManpowerState state)
    {
        state = null;
        return id != null && states.TryGetValue(id.Value, out state);
    }

    /// <summary>Creates the explicit D6A migration projection for pre-D6A records.</summary>
    public static ContingentManpowerStateStore CreateLegacyBootstrap(
        ArmedForceStore armedForceStore,
        IManpowerSourceSnapshotProvider sourceProvider = null)
    {
        return new ContingentManpowerStateStore(armedForceStore, sourceProvider);
    }

    internal ContingentManpowerStateStore CloneForRuntime(
        ArmedForceStore targetStore,
        IManpowerSourceSnapshotProvider targetProvider)
    {
        ContingentManpowerStateStore copy = new ContingentManpowerStateStore(targetStore, targetProvider, false);
        foreach (ContingentManpowerState state in States)
        {
            if (!targetStore.TryGetContingent(state.ContingentId, out ContingentRecord contingent))
                throw new ArgumentException("A manpower state references a contingent absent from the runtime ArmedForceStore.");
            if (contingent.Amount != state.LivingRosterAmount)
                throw new ArgumentException("A manpower state does not match its contingent Amount mirror.");
            copy.states.Add(state.ContingentId.Value,
                new ContingentManpowerState(state.ContingentId, state.SourceId, state.Cohorts, state.Revision));
        }
        copy.revision = revision;
        ContingentManpowerInvariantReport report = copy.ValidateInvariants(false, false);
        if (!report.IsValid)
            throw new ArgumentException("The supplied manpower state is invalid: " + string.Join("; ", report.Violations));
        return copy;
    }

    internal void AttachToArmedForceStore()
    {
        ArmedForceInvariantReport forceReport = armedForceStore.ValidateInvariants();
        if (!forceReport.IsValid)
            throw new ArgumentException("ArmedForce state is invalid before manpower composition.");
        ContingentManpowerInvariantReport report = ValidateInvariants(false, false);
        if (!report.IsValid)
            throw new ArgumentException("Manpower state is invalid before runtime composition: " + string.Join("; ", report.Violations));
        armedForceStore.AttachManpowerAuthority(this);
    }

    /// <summary>Managed registration is deliberately empty; positive roster must use TryAllocate.</summary>
    public bool TryRegisterContingent(ContingentRecord contingent, out ContingentManpowerFailure failure)
    {
        if (contingent == null || contingent.Id == null || contingent.ForceId == null)
            return Fail(ContingentManpowerFailureCode.InvalidContingent, "A contingent with stable identity is required.", out failure);
        if (contingent.Amount != 0L)
            return Fail(ContingentManpowerFailureCode.InitialRosterMustBeAllocated, "Managed contingent registration must start at zero; allocate through the source-backed roster operation.", out failure);
        if (states.ContainsKey(contingent.Id.Value))
            return Fail(ContingentManpowerFailureCode.ContingentAlreadyRegistered, "The contingent manpower state already exists.", out failure);
        if (!CanAdvanceRevision(out failure)) return false;
        if (!armedForceStore.TryRegisterContingentFromManpower(this, contingent, out ArmedForceFoundationFailure forceFailure))
            return Fail(ContingentManpowerFailureCode.InvalidContingent, forceFailure.ToString(), out failure);
        states.Add(contingent.Id.Value, new ContingentManpowerState(contingent.Id, null, null, 0L));
        revision++;
        failure = ContingentManpowerFailure.None;
        return true;
    }

    public bool TrySetSourceBinding(
        ContingentId contingentId,
        ManpowerSourceId sourceId,
        long expectedContingentRevision,
        string expectedSourceFingerprint,
        out ContingentManpowerFailure failure)
    {
        if (!TryGetMutableState(contingentId, expectedContingentRevision, out ContingentManpowerState current, out failure)) return false;
        // Binding is an accounting association, not roster mobilization. It
        // may explicitly reconcile a pre-D6A roster on a terminated force;
        // allocation and status redistribution still require an active force.
        if (!armedForceStore.TryGetContingent(contingentId, out ContingentRecord boundContingent)
            || !armedForceStore.TryGet(boundContingent.ForceId, out _))
            return Fail(ContingentManpowerFailureCode.ForceNotRegistered, "The contingent force is not registered.", out failure);
        if (current.LivingRosterAmount > 0L && current.SourceId != null && current.SourceId != sourceId)
            return Fail(ContingentManpowerFailureCode.SourceBindingChangeRequiresEmptyRoster, "A non-empty roster cannot change or clear its source binding.", out failure);
        if (current.LivingRosterAmount > 0L && sourceId == null)
            return Fail(ContingentManpowerFailureCode.SourceBindingChangeRequiresEmptyRoster, "A non-empty roster cannot be unbound.", out failure);
        if (current.SourceId == sourceId)
        {
            if (sourceId != null)
            {
                if (!TryResolveSource(sourceId, expectedSourceFingerprint, out ManpowerSourceCapacitySnapshot currentSource, out failure)) return false;
                if (!TryGetSourceAllocation(sourceId, out long currentAllocation, out failure)) return false;
                if (!TryValidateSourceAllocation(currentSource, currentAllocation, out failure)) return false;
            }
            failure = ContingentManpowerFailure.None;
            return true;
        }
        ManpowerSourceCapacitySnapshot source = null;
        if (sourceId != null)
        {
            if (!TryResolveSource(sourceId, expectedSourceFingerprint, out source, out failure)) return false;
            if (!TryValidateSourceTotals(source, current.LivingRosterAmount, out failure)) return false;
        }
        if (!CanAdvanceRevision(out failure) || !CanAdvanceContingentRevision(current, out failure)) return false;
        states[contingentId.Value] = new ContingentManpowerState(
            current.ContingentId, sourceId, current.Cohorts, current.Revision + 1L);
        revision++;
        failure = ContingentManpowerFailure.None;
        return true;
    }

    public bool TryAllocate(
        ContingentId contingentId,
        long amount,
        ManpowerInjuryState injuryState,
        ManpowerCustodyState custodyState,
        ArmedForceId custodianForceId,
        ManpowerAvailabilityState availabilityState,
        long expectedContingentRevision,
        string expectedSourceFingerprint,
        out ContingentManpowerFailure failure)
    {
        if (amount <= 0L) return Fail(ContingentManpowerFailureCode.InvalidAmount, "Allocation must be positive.", out failure);
        if (!TryGetMutableState(contingentId, expectedContingentRevision, out ContingentManpowerState current, out failure)) return false;
        if (!IsForceActive(contingentId, out failure)) return false;
        if (current.SourceId == null) return Fail(ContingentManpowerFailureCode.SourceBindingRequired, "Source-backed allocation requires an explicit source binding.", out failure);
        if (!TryValidateCohort(new ContingentManpowerCohort(injuryState, custodyState, custodianForceId, availabilityState, amount), out failure)) return false;
        if (!TryResolveSource(current.SourceId, expectedSourceFingerprint, out ManpowerSourceCapacitySnapshot source, out failure)) return false;
        if (!TryGetSourceAllocation(current.SourceId, out long allocated, out failure)) return false;
        long updatedSourceAllocation;
        long updatedRoster;
        List<ContingentManpowerCohort> nextCohorts;
        try
        {
            updatedSourceAllocation = checked(allocated + amount);
            updatedRoster = checked(current.LivingRosterAmount + amount);
            nextCohorts = new List<ContingentManpowerCohort>(current.Cohorts) { new ContingentManpowerCohort(injuryState, custodyState, custodianForceId, availabilityState, amount) };
        }
        catch (OverflowException)
        {
            return Fail(ContingentManpowerFailureCode.RosterAmountOverflow, "The requested roster allocation exceeds Int64 capacity.", out failure);
        }
        if (!TryValidateSourceAllocation(source, updatedSourceAllocation, out failure)) return false;
        if (!TryMakeNext(current, nextCohorts, current.SourceId, out ContingentManpowerState next, out failure)) return false;
        if (!armedForceStore.TryUpdateContingentAmountFromManpower(this, contingentId, current.LivingRosterAmount, updatedRoster, out ArmedForceFoundationFailure mirrorFailure))
            return Fail(ContingentManpowerFailureCode.AmountMirrorMismatch, mirrorFailure.ToString(), out failure);
        states[contingentId.Value] = next;
        revision++;
        failure = ContingentManpowerFailure.None;
        return true;
    }

    public bool TryDemobilize(
        ContingentId contingentId,
        long amount,
        ManpowerInjuryState injuryState,
        ManpowerCustodyState custodyState,
        ArmedForceId custodianForceId,
        ManpowerAvailabilityState availabilityState,
        long expectedContingentRevision,
        string expectedSourceFingerprint,
        out ContingentManpowerFailure failure)
    {
        if (amount <= 0L) return Fail(ContingentManpowerFailureCode.InvalidAmount, "Demobilization must be positive.", out failure);
        if (!TryGetMutableState(contingentId, expectedContingentRevision, out ContingentManpowerState current, out failure)) return false;
        // Release remains possible for historical terminated forces and when
        // a source disappears or its capacity falls. This operation only
        // reduces the military-owned allocation; it never mutates source truth.
        if (!armedForceStore.TryGetContingent(contingentId, out ContingentRecord demobilizedContingent)
            || !armedForceStore.TryGet(demobilizedContingent.ForceId, out _))
            return Fail(ContingentManpowerFailureCode.ForceNotRegistered, "The contingent force is not registered.", out failure);
        if (current.SourceId == null) return Fail(ContingentManpowerFailureCode.SourceBindingRequired, "Source-backed demobilization requires an explicit source binding.", out failure);
        if (custodyState == ManpowerCustodyState.Captured)
            return Fail(ContingentManpowerFailureCode.CapturedRosterCannotBeDemobilized, "Captured roster must first be explicitly released or retargeted.", out failure);
        ContingentManpowerCohort target = new ContingentManpowerCohort(injuryState, custodyState, custodianForceId, availabilityState, amount);
        if (!TryValidateCohort(target, out failure)) return false;
        if (sourceProvider != null
            && sourceProvider.TryGetSnapshot(current.SourceId, out ManpowerSourceCapacitySnapshot source))
        {
            if (source == null || source.SourceId != current.SourceId)
                return Fail(ContingentManpowerFailureCode.SourceUnresolved, "The bound manpower source returned an invalid snapshot.", out failure);
            if (string.IsNullOrWhiteSpace(expectedSourceFingerprint)
                || !string.Equals(expectedSourceFingerprint, source.Fingerprint, StringComparison.Ordinal))
                return Fail(ContingentManpowerFailureCode.StaleSourceSnapshot, "The current source fingerprint differs from the expected source snapshot.", out failure);
        }
        List<ContingentManpowerCohort> nextCohorts = new List<ContingentManpowerCohort>();
        long remaining = amount;
        foreach (ContingentManpowerCohort cohort in current.Cohorts)
        {
            if (SameKey(cohort, target) && remaining > 0L)
            {
                if (cohort.Amount < remaining)
                    return Fail(ContingentManpowerFailureCode.CohortNotPresent, "The requested demobilization exceeds that explicit cohort.", out failure);
                if (cohort.Amount > remaining)
                    nextCohorts.Add(new ContingentManpowerCohort(injuryState, custodyState, custodianForceId, availabilityState, cohort.Amount - remaining));
                remaining = 0L;
            }
            else nextCohorts.Add(cohort);
        }
        if (remaining > 0L) return Fail(ContingentManpowerFailureCode.CohortNotPresent, "The requested cohort does not exist.", out failure);
        if (!CanAdvanceRevision(out failure) || !CanAdvanceContingentRevision(current, out failure)) return false;
        long updatedRoster = current.LivingRosterAmount - amount;
        if (!TryGetSourceAllocationAfterDemobilization(current.SourceId, contingentId, updatedRoster, out _, out failure)) return false;
        if (!TryMakeNext(current, nextCohorts, current.SourceId, out ContingentManpowerState next, out failure)) return false;
        if (!armedForceStore.TryUpdateContingentAmountFromManpower(this, contingentId, current.LivingRosterAmount, updatedRoster, out ArmedForceFoundationFailure mirrorFailure))
            return Fail(ContingentManpowerFailureCode.AmountMirrorMismatch, mirrorFailure.ToString(), out failure);
        states[contingentId.Value] = next;
        revision++;
        failure = ContingentManpowerFailure.None;
        return true;
    }

    /// <summary>Changes only factual cohort status; total living roster and binding are invariant.</summary>
    public bool TryRedistribute(
        ContingentId contingentId,
        IEnumerable<ContingentManpowerCohort> replacementCohorts,
        long expectedContingentRevision,
        out ContingentManpowerFailure failure)
    {
        if (!TryGetMutableState(contingentId, expectedContingentRevision, out ContingentManpowerState current, out failure)) return false;
        if (!IsForceActive(contingentId, out failure)) return false;
        if (!TryNormalize(replacementCohorts, out List<ContingentManpowerCohort> normalized, out long living, out failure)) return false;
        if (living != current.LivingRosterAmount)
            return Fail(ContingentManpowerFailureCode.RedistributionChangesLivingRoster, "A status redistribution must preserve the exact living roster amount.", out failure);
        foreach (ContingentManpowerCohort cohort in normalized)
            if (!ValidateCustodian(cohort, out failure)) return false;
        if (!CanAdvanceRevision(out failure) || !CanAdvanceContingentRevision(current, out failure)) return false;
        states[contingentId.Value] = new ContingentManpowerState(current.ContingentId, current.SourceId, normalized, current.Revision + 1L);
        revision++;
        failure = ContingentManpowerFailure.None;
        return true;
    }

    public bool TryGetSourceAllocation(ManpowerSourceId sourceId, out long amount, out ContingentManpowerFailure failure)
    {
        amount = 0L;
        if (sourceId == null) return Fail(ContingentManpowerFailureCode.SourceUnresolved, "A source identity is required.", out failure);
        try
        {
            foreach (ContingentManpowerState state in States)
                if (state.SourceId == sourceId) amount = checked(amount + state.LivingRosterAmount);
        }
        catch (OverflowException)
        {
            amount = 0L;
            return Fail(ContingentManpowerFailureCode.RosterAmountOverflow, "Allocated source roster exceeds Int64 capacity.", out failure);
        }
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool TryGetSourceAllocationAfterDemobilization(
        ManpowerSourceId sourceId,
        ContingentId demobilizedId,
        long demobilizedRoster,
        out long amount,
        out ContingentManpowerFailure failure)
    {
        amount = 0L;
        try
        {
            foreach (ContingentManpowerState state in States)
            {
                if (state.SourceId != sourceId) continue;
                amount = checked(amount + (state.ContingentId == demobilizedId
                    ? demobilizedRoster
                    : state.LivingRosterAmount));
            }
        }
        catch (OverflowException)
        {
            amount = 0L;
            return Fail(ContingentManpowerFailureCode.RosterAmountOverflow, "The remaining source allocation exceeds Int64 capacity.", out failure);
        }
        failure = ContingentManpowerFailure.None;
        return true;
    }

    public ContingentManpowerInvariantReport ValidateInvariants()
    {
        return ValidateInvariants(true, true);
    }

    private ContingentManpowerInvariantReport ValidateInvariants(
        bool validateSourceAuthority,
        bool validateTerminatedRoster)
    {
        List<string> violations = new List<string>();
        HashSet<string> knownContingents = new HashSet<string>(StringComparer.Ordinal);
        foreach (ContingentRecord contingent in armedForceStore.Contingents)
        {
            knownContingents.Add(contingent.Id.Value);
            if (!states.TryGetValue(contingent.Id.Value, out ContingentManpowerState state))
            {
                violations.Add("Contingent " + contingent.Id.Value + " is missing its manpower state.");
                continue;
            }
            if (state.LivingRosterAmount != contingent.Amount)
                violations.Add("Contingent " + contingent.Id.Value + " Amount mirror differs from its living roster.");
            ValidateStateInvariants(state, violations);
            if (validateTerminatedRoster
                && armedForceStore.TryGet(contingent.ForceId, out ArmedForceRecord force)
                && force.LifecycleState == ArmedForceLifecycleState.Terminated
                && state.LivingRosterAmount > 0L)
                violations.Add("Terminated force " + force.Id.Value + " retains direct living manpower.");
            foreach (ContingentManpowerCohort cohort in state.Cohorts)
            {
                if (cohort == null) continue;
                if (cohort.CustodyState == ManpowerCustodyState.Captured)
                {
                    if (cohort.CustodianForceId == null
                        || !armedForceStore.TryGet(cohort.CustodianForceId, out ArmedForceRecord custodian)
                        || !custodian.IsActive)
                        violations.Add("Contingent " + state.ContingentId.Value + " has an unresolved or inactive captured-manpower custodian.");
                }
                else if (cohort.CustodianForceId != null)
                    violations.Add("Contingent " + state.ContingentId.Value + " has a custodian on free manpower.");
            }
        }
        foreach (ContingentManpowerState state in States)
        {
            if (!knownContingents.Contains(state.ContingentId.Value))
                violations.Add("Manpower state " + state.ContingentId.Value + " references a missing contingent.");
        }

        Dictionary<string, long> sourceTotals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (ContingentManpowerState state in States)
        {
            if (state.SourceId == null) continue;
            try { sourceTotals[state.SourceId.Value] = checked(sourceTotals.TryGetValue(state.SourceId.Value, out long old) ? old + state.LivingRosterAmount : state.LivingRosterAmount); }
            catch (OverflowException) { violations.Add("Allocated roster for source " + state.SourceId.Value + " exceeds Int64 capacity."); }
        }
        if (validateSourceAuthority) foreach (KeyValuePair<string, long> total in sourceTotals)
        {
            ManpowerSourceId id = new ManpowerSourceId(total.Key);
            if (sourceProvider == null
                || !sourceProvider.TryGetSnapshot(id, out ManpowerSourceCapacitySnapshot source)
                || source == null
                || source.SourceId != id)
            {
                violations.Add("Bound manpower source " + total.Key + " is unresolved.");
                continue;
            }
            if (total.Value > source.Capacity)
                violations.Add("Allocated roster for source " + total.Key + " exceeds current capacity.");
            if (source.FactualLivingAmount.HasValue && total.Value > source.FactualLivingAmount.Value)
                violations.Add("Allocated roster for source " + total.Key + " exceeds factual living amount.");
        }
        return new ContingentManpowerInvariantReport(violations);
    }

    internal bool CanTerminateForce(ArmedForceId forceId, out ArmedForceFoundationFailure failure)
    {
        foreach (ContingentManpowerState state in States)
        {
            if (!armedForceStore.TryGetContingent(state.ContingentId, out ContingentRecord contingent)) continue;
            if (contingent.ForceId == forceId && state.LivingRosterAmount > 0L)
            {
                failure = ArmedForceFoundationFailure.Create(ArmedForceFoundationFailureCode.ForceHasManagedManpower, "A force with direct living manpower cannot be terminated.");
                return false;
            }
            foreach (ContingentManpowerCohort cohort in state.Cohorts)
                if (cohort.CustodyState == ManpowerCustodyState.Captured && cohort.CustodianForceId == forceId && cohort.Amount > 0L)
                {
                    failure = ArmedForceFoundationFailure.Create(ArmedForceFoundationFailureCode.ForceCustodiesManagedManpower, "A force holding captured manpower cannot be terminated.");
                    return false;
                }
        }
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    private bool TryGetMutableState(ContingentId id, long expectedRevision, out ContingentManpowerState state, out ContingentManpowerFailure failure)
    {
        state = null;
        if (id == null || !states.TryGetValue(id.Value, out state))
            return Fail(ContingentManpowerFailureCode.ContingentNotRegistered, "The contingent manpower state is not registered.", out failure);
        if (state.Revision != expectedRevision)
            return Fail(ContingentManpowerFailureCode.StaleContingentState, "The contingent manpower revision changed.", out failure);
        if (!armedForceStore.TryGetContingent(id, out ContingentRecord contingent) || contingent.Amount != state.LivingRosterAmount)
            return Fail(ContingentManpowerFailureCode.AmountMirrorMismatch, "The contingent Amount mirror is inconsistent with the roster.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool IsForceActive(ContingentId id, out ContingentManpowerFailure failure)
    {
        if (armedForceStore.TryGetContingent(id, out ContingentRecord contingent) == false)
            return Fail(ContingentManpowerFailureCode.ContingentNotRegistered, "The contingent is not registered.", out failure);
        if (!armedForceStore.TryGet(contingent.ForceId, out ArmedForceRecord force))
            return Fail(ContingentManpowerFailureCode.ForceNotRegistered, "The contingent force is not registered.", out failure);
        if (!force.IsActive)
            return Fail(ContingentManpowerFailureCode.ForceNotActive, "Manpower cannot change for a terminated force.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool TryResolveSource(ManpowerSourceId sourceId, string expectedFingerprint, out ManpowerSourceCapacitySnapshot source, out ContingentManpowerFailure failure)
    {
        source = null;
        if (sourceProvider == null)
            return Fail(ContingentManpowerFailureCode.SourceAuthorityUnavailable, "No manpower source authority is composed.", out failure);
        if (!sourceProvider.TryGetSnapshot(sourceId, out source) || source == null || source.SourceId != sourceId)
            return Fail(ContingentManpowerFailureCode.SourceUnresolved, "The bound manpower source is not resolvable.", out failure);
        if (string.IsNullOrWhiteSpace(expectedFingerprint)
            || !string.Equals(expectedFingerprint, source.Fingerprint, StringComparison.Ordinal))
            return Fail(ContingentManpowerFailureCode.StaleSourceSnapshot, "The current source fingerprint differs from the expected source snapshot.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool TryValidateSourceTotals(ManpowerSourceCapacitySnapshot source, long prospective, out ContingentManpowerFailure failure)
    {
        if (!TryGetSourceAllocation(source.SourceId, out long allocated, out failure)) return false;
        long total;
        try { total = checked(allocated + prospective); }
        catch (OverflowException) { return Fail(ContingentManpowerFailureCode.RosterAmountOverflow, "The source allocation exceeds Int64 capacity.", out failure); }
        return TryValidateSourceAllocation(source, total, out failure);
    }

    private static bool TryValidateSourceAllocation(ManpowerSourceCapacitySnapshot source, long amount, out ContingentManpowerFailure failure)
    {
        if (amount > source.Capacity)
            return Fail(ContingentManpowerFailureCode.SourceCapacityExceeded, "The prospective allocation exceeds current source capacity.", out failure);
        if (source.FactualLivingAmount.HasValue && amount > source.FactualLivingAmount.Value)
            return Fail(ContingentManpowerFailureCode.SourceFactualAmountExceeded, "The prospective allocation exceeds current factual living amount.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool TryValidateCohort(ContingentManpowerCohort cohort, out ContingentManpowerFailure failure)
    {
        if (cohort == null || cohort.Amount <= 0L
            || !Enum.IsDefined(typeof(ManpowerInjuryState), cohort.InjuryState)
            || !Enum.IsDefined(typeof(ManpowerCustodyState), cohort.CustodyState)
            || !Enum.IsDefined(typeof(ManpowerAvailabilityState), cohort.AvailabilityState))
            return Fail(ContingentManpowerFailureCode.InvalidCohort, "A cohort must have valid dimensions and positive amount.", out failure);
        if (cohort.CustodyState == ManpowerCustodyState.Captured
            && cohort.AvailabilityState == ManpowerAvailabilityState.Available)
            return Fail(ContingentManpowerFailureCode.CapturedCohortMustBeUnavailable, "Captured manpower must be unavailable to the original contingent.", out failure);
        if ((cohort.CustodyState == ManpowerCustodyState.Free) != (cohort.CustodianForceId == null))
            return Fail(ContingentManpowerFailureCode.InvalidCohort, "Free cohorts have no custodian; captured cohorts require one.", out failure);
        return ValidateCustodian(cohort, out failure);
    }

    private bool ValidateCustodian(ContingentManpowerCohort cohort, out ContingentManpowerFailure failure)
    {
        if (cohort.CustodyState == ManpowerCustodyState.Free)
        {
            failure = ContingentManpowerFailure.None;
            return true;
        }
        if (cohort.CustodianForceId == null || !armedForceStore.TryGet(cohort.CustodianForceId, out ArmedForceRecord custodian) || !custodian.IsActive)
            return Fail(ContingentManpowerFailureCode.InvalidCustodian, "Captured manpower requires a registered active ArmedForce custodian.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool TryNormalize(IEnumerable<ContingentManpowerCohort> source, out List<ContingentManpowerCohort> result, out long living, out ContingentManpowerFailure failure)
    {
        result = new List<ContingentManpowerCohort>();
        living = 0L;
        Dictionary<string, ContingentManpowerCohort> merged = new Dictionary<string, ContingentManpowerCohort>(StringComparer.Ordinal);
        try
        {
            if (source != null)
            {
                foreach (ContingentManpowerCohort cohort in source)
                {
                    if (!TryValidateCohort(cohort, out failure)) return false;
                    if (merged.TryGetValue(cohort.Key, out ContingentManpowerCohort previous))
                        merged[cohort.Key] = new ContingentManpowerCohort(cohort.InjuryState, cohort.CustodyState, cohort.CustodianForceId, cohort.AvailabilityState, checked(previous.Amount + cohort.Amount));
                    else merged.Add(cohort.Key, cohort);
                }
            }
            result.AddRange(merged.Values);
            result.Sort(ContingentManpowerState.CompareCohorts);
            foreach (ContingentManpowerCohort cohort in result) living = checked(living + cohort.Amount);
        }
        catch (OverflowException)
        {
            return Fail(ContingentManpowerFailureCode.CohortAmountOverflow, "Cohort normalization exceeded Int64 capacity.", out failure);
        }
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private bool TryMakeNext(ContingentManpowerState current, IEnumerable<ContingentManpowerCohort> cohorts, ManpowerSourceId sourceId, out ContingentManpowerState next, out ContingentManpowerFailure failure)
    {
        next = null;
        if (!CanAdvanceRevision(out failure) || !CanAdvanceContingentRevision(current, out failure)) return false;
        if (!TryNormalize(cohorts, out List<ContingentManpowerCohort> normalized, out _, out failure)) return false;
        next = new ContingentManpowerState(current.ContingentId, sourceId, normalized, current.Revision + 1L);
        return true;
    }

    private bool CanAdvanceRevision(out ContingentManpowerFailure failure)
    {
        if (revision == long.MaxValue) return Fail(ContingentManpowerFailureCode.RevisionOverflow, "The manpower store revision cannot advance further.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private static bool CanAdvanceContingentRevision(ContingentManpowerState current, out ContingentManpowerFailure failure)
    {
        if (current.Revision == long.MaxValue) return Fail(ContingentManpowerFailureCode.RevisionOverflow, "The contingent manpower revision cannot advance further.", out failure);
        failure = ContingentManpowerFailure.None;
        return true;
    }

    private static bool SameKey(ContingentManpowerCohort left, ContingentManpowerCohort right)
        => ContingentManpowerState.CompareCohorts(left, right) == 0;

    private static void ValidateStateInvariants(ContingentManpowerState state, List<string> violations)
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        long total = 0L;
        foreach (ContingentManpowerCohort cohort in state.Cohorts)
        {
            if (cohort == null || cohort.Amount <= 0L)
            {
                violations.Add("Contingent " + state.ContingentId.Value + " has a non-positive cohort.");
                continue;
            }
            if (!keys.Add(cohort.Key)) violations.Add("Contingent " + state.ContingentId.Value + " has a duplicate cohort key.");
            if (cohort.CustodyState == ManpowerCustodyState.Captured && cohort.AvailabilityState == ManpowerAvailabilityState.Available)
                violations.Add("Contingent " + state.ContingentId.Value + " has captured available manpower.");
            if (!Enum.IsDefined(typeof(ManpowerInjuryState), cohort.InjuryState)
                || !Enum.IsDefined(typeof(ManpowerCustodyState), cohort.CustodyState)
                || !Enum.IsDefined(typeof(ManpowerAvailabilityState), cohort.AvailabilityState))
                violations.Add("Contingent " + state.ContingentId.Value + " has an invalid cohort status.");
            try { total = checked(total + cohort.Amount); }
            catch (OverflowException) { violations.Add("Contingent " + state.ContingentId.Value + " roster exceeds Int64 capacity."); break; }
        }
    }

    private static bool Fail(ContingentManpowerFailureCode code, string message, out ContingentManpowerFailure failure)
    {
        failure = ContingentManpowerFailure.Create(code, message);
        return false;
    }
}
