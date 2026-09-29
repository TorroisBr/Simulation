using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Adapts the existing PlaceContentStore receipt-backed daily-aging operation
/// to the P18-D boundary coordinator. The store remains the sole effect and
/// receipt owner.
/// </summary>
public sealed class PlaceContentDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private readonly PlaceContentStore owner;

    public PlaceContentDailyBoundaryStepProvider(PlaceContentStore owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0
            || !owner.TryCreateDayAdvanceStep(operation, firstOrdinal,
                out BoundaryContinuationStep step, out failure)
            || step == null)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        steps = Array.AsReadOnly(new[] { step });
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) =>
        step != null
        && step.OwnerId == PlaceContentStore.DayAdvanceOwnerId
        && step.StepId == PlaceContentStore.DayAdvanceStepId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return owner.TryPrepareDayAdvanceStep(manifest, step, out prepared, out failure);
    }
}

/// <summary>
/// Adapts the existing SimulationLogger begin-day heading receipt to P18-D.
/// The captured include/no-output disposition is frozen by the logger owner.
/// </summary>
public sealed class LoggerDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private const string OwnerId = "logger";
    private const string StepId = "logger-begin-day";
    private readonly SimulationLogger owner;

    public LoggerDailyBoundaryStepProvider(SimulationLogger owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0
            || !owner.TryCreateBeginDayStep(operation, firstOrdinal,
                out BoundaryContinuationStep step, out failure)
            || step == null)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        steps = Array.AsReadOnly(new[] { step });
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) =>
        step != null && step.OwnerId == OwnerId && step.StepId == StepId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return owner.TryPrepareBeginDayStep(manifest, step, out prepared, out failure);
    }
}

/// <summary>
/// Adapts one existing receipt-backed operation to the boundary coordinator.
/// The wrapped domain owner remains the sole effect and receipt authority.
/// </summary>
public abstract class SingleOwnerDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private readonly string ownerId;
    private readonly string stepId;

    protected SingleOwnerDailyBoundaryStepProvider(string ownerId, string stepId)
    {
        this.ownerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        this.stepId = stepId ?? throw new ArgumentNullException(nameof(stepId));
    }

    protected abstract bool TryCreateOwnerStep(DailyBoundaryOperation operation, int ordinal,
        out BoundaryContinuationStep step, out TimelineFailure failure);

    protected abstract bool TryPrepareOwnerStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure);

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0
            || !TryCreateOwnerStep(operation, firstOrdinal, out BoundaryContinuationStep step, out failure)
            || step == null || !OwnsStep(step) || step.Ordinal != firstOrdinal)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        steps = Array.AsReadOnly(new[] { step });
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) => step != null
        && step.OwnerId == ownerId && step.StepId == stepId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return TryPrepareOwnerStep(manifest, step, out prepared, out failure);
    }
}

/// <summary>Places the Justice-owned day reset at its declared daily position.</summary>
public sealed class JusticeBeginDayDailyBoundaryStepProvider : SingleOwnerDailyBoundaryStepProvider
{
    private readonly JusticeSystem owner;

    public JusticeBeginDayDailyBoundaryStepProvider(JusticeSystem owner)
        : base("justice", "justice-begin-day")
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override bool TryCreateOwnerStep(DailyBoundaryOperation operation, int ordinal,
        out BoundaryContinuationStep step, out TimelineFailure failure) =>
        owner.TryCreateBeginDayStep(operation, ordinal, out step, out failure);

    protected override bool TryPrepareOwnerStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure) =>
        owner.TryPrepareBeginDayStep(manifest, step, out prepared, out failure);
}

/// <summary>Places Crime's hidden-status expiration after Justice BeginDay.</summary>
public sealed class CrimeHiddenStatusesDailyBoundaryStepProvider : SingleOwnerDailyBoundaryStepProvider
{
    private readonly CrimeSystem owner;
    private readonly List<NpcRuntime> roster;

    public CrimeHiddenStatusesDailyBoundaryStepProvider(CrimeSystem owner, List<NpcRuntime> roster)
        : base("crime", "crime-hidden-statuses")
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
    }

    protected override bool TryCreateOwnerStep(DailyBoundaryOperation operation, int ordinal,
        out BoundaryContinuationStep step, out TimelineFailure failure) =>
        owner.TryCreateAdvanceHiddenStatusesStep(operation, roster, ordinal, out step, out failure);

    protected override bool TryPrepareOwnerStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure) =>
        owner.TryPrepareAdvanceHiddenStatusesStep(manifest, step, roster, out prepared, out failure);
}

/// <summary>
/// Keeps sentence advancement (including its embedded first wanted pass) and
/// the explicit later wanted-status pass as two ordered owner receipts.
/// </summary>
public sealed class JusticeResolutionDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private const string OwnerId = "justice";
    private const string AdvanceSentencesStepId = "justice-advance-sentences";
    private const string SyncWantedStatusesStepId = "justice-sync-wanted-statuses";
    private readonly JusticeSystem owner;
    private readonly List<NpcRuntime> roster;

    public JusticeResolutionDailyBoundaryStepProvider(JusticeSystem owner, List<NpcRuntime> roster)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0 || firstOrdinal == int.MaxValue
            || !owner.TryCreateAdvanceSentencesStep(operation, roster, firstOrdinal,
                out BoundaryContinuationStep sentences, out failure)
            || sentences == null || sentences.StepId != AdvanceSentencesStepId
            || !owner.TryCreateSyncWantedStatusesStep(operation, roster, firstOrdinal + 1,
                out BoundaryContinuationStep wantedStatuses, out failure)
            || wantedStatuses == null || wantedStatuses.StepId != SyncWantedStatusesStepId)
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        steps = Array.AsReadOnly(new[] { sentences, wantedStatuses });
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) => step != null
        && step.OwnerId == OwnerId
        && (step.StepId == AdvanceSentencesStepId || step.StepId == SyncWantedStatusesStepId);

    public bool TryPrepareStep(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (step.StepId == AdvanceSentencesStepId)
            return owner.TryPrepareAdvanceSentencesStep(manifest, step, roster, out prepared, out failure);
        return owner.TryPrepareSyncWantedStatusesStep(manifest, step, roster, out prepared, out failure);
    }
}

/// <summary>Places the merchant-plan urgency pass after Justice's daily passes.</summary>
public sealed class MerchantPlanUrgencyDailyBoundaryStepProvider : SingleOwnerDailyBoundaryStepProvider
{
    private readonly MerchantSystem owner;
    private readonly IReadOnlyList<NpcRuntime> roster;

    public MerchantPlanUrgencyDailyBoundaryStepProvider(MerchantSystem owner, IReadOnlyList<NpcRuntime> roster)
        : base("merchant", "merchant-plan-urgency")
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
    }

    protected override bool TryCreateOwnerStep(DailyBoundaryOperation operation, int ordinal,
        out BoundaryContinuationStep step, out TimelineFailure failure) =>
        owner.TryCreatePlanUrgencyStep(operation, roster, ordinal, out step, out failure);

    protected override bool TryPrepareOwnerStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure) =>
        owner.TryPreparePlanUrgencyStep(manifest, step, roster, out prepared, out failure);
}

/// <summary>
/// Adapts the existing CityRuntime economy operations in their P18-D order:
/// production for every city, then consumption and price refresh per city.
/// Construct this provider only when the economy owner is included by the host.
/// It does not establish the host's enabled/disabled profile disposition.
/// </summary>
public sealed class CityEconomyDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private readonly Func<IReadOnlyList<CityRuntime>> cityRoster;
    private string activeBoundaryOccurrenceId;
    private Dictionary<string, CityRuntime> activeOwners =
        new Dictionary<string, CityRuntime>(StringComparer.Ordinal);

    public CityEconomyDailyBoundaryStepProvider(Func<IReadOnlyList<CityRuntime>> cityRoster)
    {
        this.cityRoster = cityRoster ?? throw new ArgumentNullException(nameof(cityRoster));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0) return false;

        IReadOnlyList<CityRuntime> currentCities = cityRoster();
        if (currentCities == null) return false;
        long stepCount = (long)currentCities.Count * 3L;
        if (stepCount > int.MaxValue - (long)firstOrdinal) return false;

        Dictionary<string, CityRuntime> nextOwners = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
        CityRuntime[] frozenCities = new CityRuntime[currentCities.Count];
        for (int i = 0; i < currentCities.Count; i++)
        {
            CityRuntime city = currentCities[i];
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || !nextOwners.TryAdd(city.RuntimeId, city)) return false;
            frozenCities[i] = city;
        }

        List<BoundaryContinuationStep> created = new List<BoundaryContinuationStep>((int)stepCount);
        foreach (CityRuntime city in frozenCities)
            if (!TryCreate(city, operation, firstOrdinal + created.Count,
                CityDailyEconomyStepKind.Production, created, out failure)) return false;
        foreach (CityRuntime city in frozenCities)
        {
            if (!TryCreate(city, operation, firstOrdinal + created.Count,
                CityDailyEconomyStepKind.Consumption, created, out failure)
                || !TryCreate(city, operation, firstOrdinal + created.Count,
                    CityDailyEconomyStepKind.PriceRefresh, created, out failure)) return false;
        }

        activeBoundaryOccurrenceId = operation.OccurrenceId;
        activeOwners = nextOwners;
        steps = created.AsReadOnly();
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step)
    {
        if (step == null || !activeOwners.ContainsKey(step.OwnerId)) return false;
        string cityId = step.OwnerId;
        return step.StepId == GetStepId(CityDailyEconomyStepKind.Production, cityId)
            || step.StepId == GetStepId(CityDailyEconomyStepKind.Consumption, cityId)
            || step.StepId == GetStepId(CityDailyEconomyStepKind.PriceRefresh, cityId);
    }

    public bool TryPrepareStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (manifest == null || manifest.BoundaryOccurrenceId != activeBoundaryOccurrenceId
            || !OwnsStep(step) || !activeOwners.TryGetValue(step.OwnerId, out CityRuntime owner))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return owner.TryPrepareDailyEconomyStep(manifest, step, out prepared, out failure);
    }

    private static bool TryCreate(CityRuntime city, DailyBoundaryOperation operation, int ordinal,
        CityDailyEconomyStepKind kind, List<BoundaryContinuationStep> target,
        out TimelineFailure failure)
    {
        if (!city.TryCreateDailyEconomyStep(operation, ordinal, kind,
            out BoundaryContinuationStep step, out failure) || step == null
            || step.OwnerId != city.RuntimeId || step.StepId != GetStepId(kind, city.RuntimeId))
        {
            if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        target.Add(step);
        return true;
    }

    private static string GetStepId(CityDailyEconomyStepKind kind, string cityRuntimeId) =>
        "city-economy-" + kind.ToString().ToLowerInvariant() + ":" + cityRuntimeId;
}

/// <summary>
/// Freezes the merchant trade-state cadence after commercial sharing. Every
/// roster slot receives an explicit scheduled/skipped disposition; eligible
/// merchant effects and receipts remain owned by MerchantSystem/NpcRuntime.
/// </summary>
public sealed class MerchantTradeStateDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private const string OperationKind = "merchant.trade-state";
    private const string OperationVersion = "1";
    private readonly MerchantSystem owner;
    private readonly Func<IReadOnlyList<NpcRuntime>> roster;
    private string activeOccurrenceId;
    private IReadOnlyList<BoundaryContinuationStep> activeSteps;
    private Dictionary<string, NpcRuntime> activeActors =
        new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);

    public MerchantTradeStateDailyBoundaryStepProvider(
        MerchantSystem owner, Func<IReadOnlyList<NpcRuntime>> roster)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0) return false;
        if (activeOccurrenceId == operation.OccurrenceId && activeSteps != null)
        {
            List<BoundaryContinuationStep> rebased = new List<BoundaryContinuationStep>(activeSteps.Count);
            foreach (BoundaryContinuationStep item in activeSteps)
                rebased.Add(new BoundaryContinuationStep(firstOrdinal + rebased.Count,
                    item.StepId, item.OwnerId, item.OperationKind, item.OperationVersion,
                    item.OwnerRevision, item.Payload, item.PersonId, item.Disposition));
            activeSteps = rebased.AsReadOnly();
            steps = activeSteps;
            failure = TimelineFailure.None;
            return true;
        }

        IReadOnlyList<NpcRuntime> current = roster();
        if (current == null || current.Count > int.MaxValue - firstOrdinal) return false;
        List<BoundaryContinuationStep> created = new List<BoundaryContinuationStep>(current.Count);
        Dictionary<string, NpcRuntime> actors = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime actor in current)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.RuntimeId)
                || !runtimeIds.Add(actor.RuntimeId)) return false;
            string stepId = "merchant-trade-state:" + actor.RuntimeId;
            bool eligible = owner.IsP18DTradeStateEligible(actor);
            created.Add(new BoundaryContinuationStep(
                firstOrdinal + created.Count,
                stepId,
                actor.RuntimeId,
                OperationKind,
                OperationVersion,
                actor.MerchantTradeStateRuntime.Revision.ToString(CultureInfo.InvariantCulture),
                eligible ? "merchant-trade-state-v1" : string.Empty,
                actor.PersonId != null ? actor.PersonId.Value : string.Empty,
                eligible ? "included" : "skipped:merchant-trade-ineligible"));
            actors.Add(stepId, actor);
        }

        activeOccurrenceId = operation.OccurrenceId;
        activeSteps = created.AsReadOnly();
        activeActors = actors;
        steps = activeSteps;
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) => step != null
        && step.OperationKind == OperationKind
        && step.OperationVersion == OperationVersion
        && activeActors.TryGetValue(step.StepId, out NpcRuntime actor)
        && actor.RuntimeId == step.OwnerId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (manifest == null || manifest.BoundaryOccurrenceId != activeOccurrenceId
            || !OwnsStep(step) || step.Ordinal < 0
            || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || !activeActors.TryGetValue(step.StepId, out NpcRuntime actor))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (step.Disposition == "skipped:merchant-trade-ineligible")
        {
            prepared = SkippedCommit.Instance;
            failure = TimelineFailure.None;
            return true;
        }
        if (step.Disposition != "included" || step.Payload != "merchant-trade-state-v1")
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        prepared = new MerchantTradeStateCommit(owner, actor, manifest, step);
        failure = TimelineFailure.None;
        return true;
    }

    private sealed class MerchantTradeStateCommit : IBoundaryContinuationStepCommit
    {
        private static readonly IReadOnlyList<DueWorkReference> NoFacts = Array.Empty<DueWorkReference>();
        private static readonly IReadOnlyList<string> NoSignals = Array.Empty<string>();
        private readonly MerchantSystem owner;
        private readonly NpcRuntime actor;
        private readonly BoundaryContinuationManifest manifest;
        private readonly BoundaryContinuationStep step;

        public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => NoFacts;
        public IReadOnlyList<string> RetainedSourceSignals => NoSignals;

        public MerchantTradeStateCommit(MerchantSystem owner, NpcRuntime actor,
            BoundaryContinuationManifest manifest, BoundaryContinuationStep step)
        {
            this.owner = owner;
            this.actor = actor;
            this.manifest = manifest;
            this.step = step;
        }

        public bool TryCommit(out TimelineFailure failure) =>
            owner.TryAdvanceNpcTradeStateOccurrence(actor, manifest, step,
                out NpcMerchantTradeStateReceipt receipt, out failure)
            && receipt != null;
    }

    private sealed class SkippedCommit : IBoundaryContinuationStepCommit
    {
        public static readonly SkippedCommit Instance = new SkippedCommit();
        public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
        public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
        public bool TryCommit(out TimelineFailure failure)
        {
            failure = TimelineFailure.None;
            return true;
        }
    }
}
