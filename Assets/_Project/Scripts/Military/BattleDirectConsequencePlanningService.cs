using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/// <summary>
/// World-composed, non-mutating D6B2 orchestration. D5 is the sole outcome
/// authority; D6A supplies roster/source facts; D6B1 plans grouped deaths.
/// This service never applies consequences or changes Battle lifecycle.
/// </summary>
public sealed class BattleDirectConsequencePlanningService
{
    public const string PlanSchemaVersion = "battle-direct-consequence-plan:v1";
    public const string CoverageVersion = "available-free-cohorts:explicit-complete-partition:v1";

    private readonly SimulationRuntime world;
    private readonly BattleDirectConsequencePolicy policy;

    internal BattleDirectConsequencePlanningService(
        SimulationRuntime world,
        BattleDirectConsequencePolicy policy)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.policy = policy;
    }

    public bool IsConfigured => policy != null;
    public string AuthorizedPolicyFingerprint => policy?.SemanticFingerprint;

    public bool TryCreatePlan(
        BattleId battleId,
        out BattleDirectConsequencePlan plan,
        out BattleDirectConsequenceFailure failure)
    {
        return TryCreatePlan(battleId, null, out plan, out failure);
    }

    /// <summary>
    /// The optional execution plan carries only D3-allowed Battle execution
    /// metadata. Outcome, rule, and D6B1 planner are always world-derived.
    /// </summary>
    public bool TryCreatePlan(
        BattleId battleId,
        BattleExecutionPlan executionPlan,
        out BattleDirectConsequencePlan plan,
        out BattleDirectConsequenceFailure failure)
    {
        plan = null;
        failure = null;
        if (battleId == null)
            return Fail(BattleDirectConsequenceFailureCode.InvalidRequest,
                "A stable BattleId is required.", out failure);
        if (policy == null)
            return Fail(BattleDirectConsequenceFailureCode.PolicyNotConfigured,
                "The world has no explicitly configured D6B2 consequence policy.", out failure);
        if (!policy.IdentityRemainsStable())
            return Fail(BattleDirectConsequenceFailureCode.PolicyIdentityChanged,
                "The world-composed D6B2 rule identity changed before planning.", out failure);

        if (!world.BattleOutcomePlanningService.TryCreateApplicationPlan(
                battleId,
                executionPlan,
                null,
                out BattleOutcomeApplicationPlan d5Plan,
                out BattleOutcomePlanningFailure d5Failure))
        {
            failure = new BattleDirectConsequenceFailure(
                BattleDirectConsequenceFailureCode.D5PlanningFailed,
                "World-authorized D5 outcome planning failed.",
                d5Failure);
            return false;
        }
        if (!IsValidD5Plan(d5Plan, battleId))
            return Fail(BattleDirectConsequenceFailureCode.D5PlanInvalid,
                "World-authorized D5 returned a malformed or non-semantic application plan.", out failure);

        long planningDay = world.CurrentDay;
        if (planningDay != d5Plan.Outcome.ResolvedAbsoluteDay)
            return Fail(BattleDirectConsequenceFailureCode.ContextBecameStale,
                "The logical day changed during D5 outcome planning.", out failure);
        if (!world.BattleExecutionContextBuilder.TryCreate(
                battleId,
                planningDay,
                d5Plan.ExecutionPlan,
                out BattleExecutionContext sourceContext,
                out BattleExecutionFailure contextFailure))
        {
            failure = new BattleDirectConsequenceFailure(
                BattleDirectConsequenceFailureCode.ContextCreationFailed,
                "The exact D5 execution plan could not rebuild its D3 source context.",
                null,
                contextFailure);
            return false;
        }
        if (!string.Equals(sourceContext.StableKey, d5Plan.SourceContextFingerprint, StringComparison.Ordinal))
            return Fail(BattleDirectConsequenceFailureCode.ContextMismatch,
                "The rebuilt D3 source-context fingerprint differs from D5 provenance.", out failure);

        if (!TryBuildRuleInput(
                sourceContext,
                d5Plan.Outcome,
                out BattleDirectConsequenceInput input,
                out Dictionary<string, string> participantSides,
                out List<ContingentManpowerState> participantStates,
                out failure))
            return false;

        if (!policy.IdentityRemainsStable())
            return Fail(BattleDirectConsequenceFailureCode.PolicyIdentityChanged,
                "The world-composed D6B2 rule identity changed before evaluation.", out failure);

        IReadOnlyList<BattleCohortConsequencePartition> rawPartitions = null;
        bool ruleThrew = false;
        try
        {
            // One call for this Battle, covering every canonical exposed input.
            rawPartitions = policy.Rule.Evaluate(input);
        }
        catch
        {
            ruleThrew = true;
        }
        if (!policy.IdentityRemainsStable())
            return Fail(BattleDirectConsequenceFailureCode.PolicyIdentityChanged,
                "The world-composed D6B2 rule identity changed during evaluation.", out failure);
        if (ruleThrew)
            return Fail(BattleDirectConsequenceFailureCode.RuleFailed,
                "The D6B2 rule threw while evaluating the immutable Battle input.", out failure);

        if (!TryNormalizePartitions(
                input,
                rawPartitions,
                participantSides,
                out List<BattleCohortConsequencePartition> partitions,
                out failure))
            return false;

        if (!TryPlanSourceDeaths(
                input,
                partitions,
                out List<BattleDirectConsequenceSourceGroup> sourceGroups,
                out failure))
            return false;

        if (!TryBuildProjections(
                sourceContext,
                participantStates,
                input.Cohorts,
                partitions,
                out List<BattleDirectConsequenceContingentProjection> projections,
                out failure))
            return false;

        string fingerprint = BuildPlanFingerprint(
            d5Plan,
            policy.SemanticFingerprint,
            input.Cohorts,
            partitions,
            sourceGroups,
            projections);
        BattleDirectConsequencePlan candidate = new BattleDirectConsequencePlan(
            d5Plan,
            sourceContext,
            policy.SemanticFingerprint,
            input.Cohorts,
            partitions,
            sourceGroups,
            projections,
            fingerprint);

        if (!TryValidateCurrent(
                candidate,
                out BattleDirectConsequenceValidationReport validation,
                out failure))
        {
            plan = null;
            return false;
        }
        plan = candidate;
        failure = null;
        return true;
    }

    public bool TryValidateCurrent(
        BattleDirectConsequencePlan plan,
        out BattleDirectConsequenceValidationReport report,
        out BattleDirectConsequenceFailure failure)
    {
        report = null;
        failure = null;
        if (!ValidatePlanShape(plan))
        {
            report = new BattleDirectConsequenceValidationReport(
                BattleDirectConsequenceValidationStatus.Invalid,
                null,
                "The supplied D6B2 plan is malformed or incomplete.");
            failure = new BattleDirectConsequenceFailure(
                BattleDirectConsequenceFailureCode.PlanInvalid,
                report.Message);
            return false;
        }
        if (policy == null
            || !policy.IdentityRemainsStable()
            || !string.Equals(plan.D6B2PolicyFingerprint, policy.SemanticFingerprint, StringComparison.Ordinal))
        {
            report = new BattleDirectConsequenceValidationReport(
                BattleDirectConsequenceValidationStatus.Invalid,
                null,
                "The plan policy identity does not match the current world-composed D6B2 authority.");
            failure = new BattleDirectConsequenceFailure(
                BattleDirectConsequenceFailureCode.PolicyIdentityChanged,
                report.Message);
            return false;
        }

        List<BattleDirectConsequenceStalenessReason> reasons =
            new List<BattleDirectConsequenceStalenessReason>();
        long currentDay = world.CurrentDay;
        if (currentDay != plan.AbsoluteDay)
            reasons.Add(BattleDirectConsequenceStalenessReason.CurrentDayChanged);

        if (!world.BattleOutcomePlanningService.TryValidateCurrent(
                plan.D5ApplicationPlan,
                out BattleOutcomePlanValidationReport d5Report,
                out BattleOutcomePlanningFailure d5Failure))
        {
            if (d5Report != null && d5Report.IsInvalid)
            {
                report = new BattleDirectConsequenceValidationReport(
                    BattleDirectConsequenceValidationStatus.Invalid,
                    null,
                    "The underlying D5 application plan is invalid in this world.");
                failure = new BattleDirectConsequenceFailure(
                    BattleDirectConsequenceFailureCode.D5PlanInvalid,
                    report.Message,
                    d5Failure);
                return false;
            }
            reasons.Add(BattleDirectConsequenceStalenessReason.D5PlanChanged);
        }

        if (!world.BattleExecutionContextBuilder.TryCreate(
                plan.BattleId,
                currentDay,
                plan.D5ApplicationPlan.ExecutionPlan,
                out BattleExecutionContext currentContext,
                out BattleExecutionFailure executionFailure))
        {
            reasons.Add(BattleDirectConsequenceStalenessReason.ParticipantManpowerChanged);
            if (!CapturedCustodiansRemainActive(plan))
                reasons.Add(BattleDirectConsequenceStalenessReason.CustodianChanged);
        }
        else
        {
            if (!string.Equals(currentContext.StableKey, plan.D5SourceContextFingerprint, StringComparison.Ordinal)
                || !string.Equals(currentContext.StableKey, plan.SourceContext.StableKey, StringComparison.Ordinal))
                reasons.Add(BattleDirectConsequenceStalenessReason.ParticipantManpowerChanged);
            if (!ValidateCustodiansAgainstCurrentContext(plan, currentContext))
                reasons.Add(BattleDirectConsequenceStalenessReason.CustodianChanged);
        }

        foreach (BattleDirectConsequenceSourceGroup group in plan.SourceGroups)
        {
            if (group?.Proposal == null)
            {
                report = new BattleDirectConsequenceValidationReport(
                    BattleDirectConsequenceValidationStatus.Invalid,
                    null,
                    "A grouped source death has no D6B1 proposal.");
                failure = new BattleDirectConsequenceFailure(
                    BattleDirectConsequenceFailureCode.SourceProposalInvalid,
                    report.Message);
                return false;
            }
            ManpowerSourceConsequenceProposal proposal = group.Proposal;
            if (proposal.SourceId != group.SourceId
                || proposal.Effect == null
                || proposal.Effect.Kind != ManpowerSourceEffectKind.Death
                || proposal.Effect.Amount != group.DeathAmount)
            {
                report = new BattleDirectConsequenceValidationReport(
                    BattleDirectConsequenceValidationStatus.Invalid,
                    null,
                    "A D6B1 proposal does not exactly represent its grouped source death.");
                failure = new BattleDirectConsequenceFailure(
                    BattleDirectConsequenceFailureCode.SourceProposalInvalid,
                    report.Message);
                return false;
            }
            ManpowerSourceConsequenceValidationResult sourceValidation =
                world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);
            if (sourceValidation == null
                || sourceValidation.Status == ManpowerSourceConsequenceValidationStatus.Invalid)
            {
                report = new BattleDirectConsequenceValidationReport(
                    BattleDirectConsequenceValidationStatus.Invalid,
                    null,
                    "A grouped D6B1 source proposal is invalid.");
                failure = new BattleDirectConsequenceFailure(
                    BattleDirectConsequenceFailureCode.SourceProposalInvalid,
                    report.Message);
                return false;
            }
            if (!sourceValidation.IsCurrent)
                reasons.Add(BattleDirectConsequenceStalenessReason.SourceProposalChanged);
        }

        if (!policy.IdentityRemainsStable())
        {
            report = new BattleDirectConsequenceValidationReport(
                BattleDirectConsequenceValidationStatus.Invalid,
                null,
                "The D6B2 rule identity changed during plan validation.");
            failure = new BattleDirectConsequenceFailure(
                BattleDirectConsequenceFailureCode.PolicyIdentityChanged,
                report.Message);
            return false;
        }
        if (reasons.Count > 0)
        {
            report = new BattleDirectConsequenceValidationReport(
                BattleDirectConsequenceValidationStatus.Stale,
                reasons,
                "One or more causal D5/D6B2/D6B1 dependencies changed.");
            failure = new BattleDirectConsequenceFailure(
                BattleDirectConsequenceFailureCode.PlanStale,
                report.Message,
                d5Failure,
                executionFailure);
            return false;
        }
        report = new BattleDirectConsequenceValidationReport(
            BattleDirectConsequenceValidationStatus.Current,
            null,
            "The complete D6B2 proposal remains current.");
        failure = null;
        return true;
    }

    private bool TryBuildRuleInput(
        BattleExecutionContext context,
        BattleOutcome outcome,
        out BattleDirectConsequenceInput input,
        out Dictionary<string, string> participantSides,
        out List<ContingentManpowerState> participantStates,
        out BattleDirectConsequenceFailure failure)
    {
        input = null;
        participantSides = new Dictionary<string, string>(StringComparer.Ordinal);
        participantStates = new List<ContingentManpowerState>();
        List<BattleDirectConsequenceCohortInput> cohorts = new List<BattleDirectConsequenceCohortInput>();
        List<BattleDirectConsequenceParticipant> participants = new List<BattleDirectConsequenceParticipant>();
        List<BattleSideId> sides = new List<BattleSideId>();
        List<BattleDirectConsequenceContingentBinding> contingentBindings =
            new List<BattleDirectConsequenceContingentBinding>();
        HashSet<string> seenContingents = new HashSet<string>(StringComparer.Ordinal);

        foreach (BattleExecutionSideContext side in context.Sides)
        {
            if (side == null || side.SideId == null)
                return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                    "The D3 context contains a malformed side.", out failure);
            sides.Add(side.SideId);
            foreach (BattleExecutionForceContext force in side.Forces)
            {
                if (force == null || force.ForceId == null
                    || participantSides.ContainsKey(force.ForceId.Value))
                    return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                        "The D3 context contains a missing or duplicate participant force.", out failure);
                participantSides.Add(force.ForceId.Value, side.SideId.Value);
                participants.Add(new BattleDirectConsequenceParticipant(side.SideId, force.ForceId));

                foreach (BattleExecutionContingentSnapshot contingent in force.DirectContingents)
                {
                    if (contingent == null || contingent.ContingentId == null
                        || !seenContingents.Add(contingent.ContingentId.Value)
                        || !world.ContingentManpowerStateStore.TryGet(
                            contingent.ContingentId,
                            out ContingentManpowerState manpowerState)
                        || manpowerState == null
                        || manpowerState.ContingentId != contingent.ContingentId
                        || !string.Equals(
                            manpowerState.Fingerprint,
                            contingent.ManpowerStateFingerprint,
                            StringComparison.Ordinal))
                        return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                            "A D3 contingent lacks the exact matching D6A roster/source snapshot.", out failure);
                    participantStates.Add(manpowerState);
                    contingentBindings.Add(new BattleDirectConsequenceContingentBinding(
                        side.SideId,
                        force.ForceId,
                        contingent.ContingentId,
                        manpowerState.SourceId));

                    foreach (ContingentManpowerCohort cohort in contingent.ManpowerCohorts)
                    {
                        if (cohort == null || cohort.Amount <= 0L
                            || !Enum.IsDefined(typeof(ManpowerInjuryState), cohort.InjuryState)
                            || !Enum.IsDefined(typeof(ManpowerCustodyState), cohort.CustodyState)
                            || !Enum.IsDefined(typeof(ManpowerAvailabilityState), cohort.AvailabilityState)
                            || (cohort.CustodyState == ManpowerCustodyState.Captured
                                && (cohort.CustodianForceId == null
                                    || cohort.AvailabilityState != ManpowerAvailabilityState.Unavailable))
                            || (cohort.CustodyState == ManpowerCustodyState.Free
                                && cohort.CustodianForceId != null))
                            return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                                "The D6A roster contains an invalid semantic cohort.", out failure);

                        // Captured and unavailable cohorts remain represented in
                        // the projection but are not exposed for another outcome.
                        if (cohort.AvailabilityState != ManpowerAvailabilityState.Available
                            || cohort.CustodyState != ManpowerCustodyState.Free)
                            continue;
                        BattleDirectConsequenceCohortIdentity identity =
                            new BattleDirectConsequenceCohortIdentity(
                                side.SideId,
                                force.ForceId,
                                contingent.ContingentId,
                                cohort.InjuryState,
                                cohort.CustodyState,
                                cohort.CustodianForceId,
                                cohort.AvailabilityState);
                        cohorts.Add(new BattleDirectConsequenceCohortInput(
                            identity,
                            cohort.Amount,
                            manpowerState.SourceId));
                    }
                }
            }
        }

        cohorts.Sort(CompareInputs);
        participants.Sort((left, right) =>
        {
            int c = StringComparer.Ordinal.Compare(left.SideId.Value, right.SideId.Value);
            return c != 0 ? c : StringComparer.Ordinal.Compare(left.ForceId.Value, right.ForceId.Value);
        });
        sides.Sort((left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value));
        contingentBindings.Sort((left, right) =>
        {
            int c = StringComparer.Ordinal.Compare(left.SideId.Value, right.SideId.Value);
            if (c != 0) return c;
            c = StringComparer.Ordinal.Compare(left.ForceId.Value, right.ForceId.Value);
            return c != 0 ? c : StringComparer.Ordinal.Compare(left.ContingentId.Value, right.ContingentId.Value);
        });
        participantStates.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.ContingentId.Value,
            right.ContingentId.Value));
        input = new BattleDirectConsequenceInput(
            context.BattleId,
            context.ExecutionAbsoluteDay,
            outcome,
            cohorts,
            participants,
            sides,
            contingentBindings);
        failure = null;
        return true;
    }

    private bool TryNormalizePartitions(
        BattleDirectConsequenceInput input,
        IReadOnlyList<BattleCohortConsequencePartition> rawPartitions,
        Dictionary<string, string> participantSides,
        out List<BattleCohortConsequencePartition> normalized,
        out BattleDirectConsequenceFailure failure)
    {
        normalized = new List<BattleCohortConsequencePartition>();
        if (rawPartitions == null)
            return Fail(BattleDirectConsequenceFailureCode.InvalidRuleOutput,
                "The D6B2 rule returned no partition collection.", out failure);

        Dictionary<string, BattleDirectConsequenceCohortInput> knownInputs =
            new Dictionary<string, BattleDirectConsequenceCohortInput>(StringComparer.Ordinal);
        foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
        {
            if (cohort == null || cohort.Identity == null || cohort.Amount <= 0L
                || knownInputs.ContainsKey(cohort.Identity.StableKey))
                return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                    "The canonical D6B2 input contains an invalid or duplicate cohort identity.", out failure);
            knownInputs.Add(cohort.Identity.StableKey, cohort);
        }

        Dictionary<string, BattleCohortConsequencePartition> byInput =
            new Dictionary<string, BattleCohortConsequencePartition>(StringComparer.Ordinal);
        List<BattleCohortConsequencePartition> orderedRawPartitions =
            new List<BattleCohortConsequencePartition>(rawPartitions);
        orderedRawPartitions.Sort((left, right) => StringComparer.Ordinal.Compare(
            RawPartitionSortKey(left),
            RawPartitionSortKey(right)));
        foreach (BattleCohortConsequencePartition partition in orderedRawPartitions)
        {
            if (partition == null || partition.InputIdentity == null)
                return Fail(BattleDirectConsequenceFailureCode.InvalidRuleOutput,
                    "The D6B2 rule returned a null partition or identity.", out failure);
            string key = partition.InputIdentity.StableKey;
            if (!knownInputs.ContainsKey(key))
                return Fail(BattleDirectConsequenceFailureCode.UnknownPartition,
                    "The D6B2 rule returned a partition for an unknown cohort.", out failure);
            if (byInput.ContainsKey(key))
                return Fail(BattleDirectConsequenceFailureCode.DuplicatePartition,
                    "The D6B2 rule returned more than one partition for a cohort.", out failure);
            byInput.Add(key, partition);
        }

        foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
        {
            if (!byInput.TryGetValue(cohort.Identity.StableKey, out BattleCohortConsequencePartition raw))
                return Fail(BattleDirectConsequenceFailureCode.MissingPartition,
                    "The D6B2 rule omitted an exposed cohort partition.", out failure);
            if (raw.DeathAmount < 0L)
                return Fail(BattleDirectConsequenceFailureCode.InvalidAmount,
                    "A cohort partition death amount cannot be negative.", out failure);
            if (raw.DeathAmount > 0L && cohort.SourceId == null)
                return Fail(BattleDirectConsequenceFailureCode.UnboundDeath,
                    "A positive cohort death has no explicit D6A manpower source binding.", out failure);

            Dictionary<string, DestinationAccumulator> destinations =
                new Dictionary<string, DestinationAccumulator>(StringComparer.Ordinal);
            long total = raw.DeathAmount;
            List<BattleCohortConsequenceLivingDestination> orderedDestinations =
                new List<BattleCohortConsequenceLivingDestination>(raw.LivingDestinations);
            orderedDestinations.Sort((left, right) => StringComparer.Ordinal.Compare(
                RawDestinationSortKey(left),
                RawDestinationSortKey(right)));
            foreach (BattleCohortConsequenceLivingDestination destination in orderedDestinations)
            {
                if (destination == null || destination.Amount <= 0L)
                    return Fail(BattleDirectConsequenceFailureCode.InvalidAmount,
                        "Living destination amounts must be positive.", out failure);
                if (!IsValidDestination(cohort, destination, participantSides))
                    return Fail(DestinationFailureCode(cohort, destination, participantSides),
                        "A living destination violates injury, custody, availability, or participant-side policy.", out failure);
                string destinationKey = DestinationKey(destination);
                if (!destinations.TryGetValue(destinationKey, out DestinationAccumulator accumulator))
                {
                    accumulator = new DestinationAccumulator(destination);
                    destinations.Add(destinationKey, accumulator);
                }
                try
                {
                    accumulator.Amount = checked(accumulator.Amount + destination.Amount);
                    total = checked(total + destination.Amount);
                }
                catch (OverflowException)
                {
                    return Fail(BattleDirectConsequenceFailureCode.AmountOverflow,
                        "Cohort partition aggregation exceeds Int64 capacity.", out failure);
                }
            }
            if (total != cohort.Amount)
                return Fail(BattleDirectConsequenceFailureCode.ConservationFailure,
                    "Living destinations plus deaths must exactly conserve the input cohort amount.", out failure);

            List<BattleCohortConsequenceLivingDestination> canonicalDestinations =
                new List<BattleCohortConsequenceLivingDestination>();
            foreach (DestinationAccumulator destination in destinations.Values)
            {
                canonicalDestinations.Add(new BattleCohortConsequenceLivingDestination(
                    destination.InjuryState,
                    destination.CustodyState,
                    destination.CustodianForceId,
                    destination.AvailabilityState,
                    destination.Amount));
            }
            canonicalDestinations.Sort(CompareDestinations);
            normalized.Add(new BattleCohortConsequencePartition(
                cohort.Identity,
                cohort.Amount,
                canonicalDestinations,
                raw.DeathAmount));
        }
        failure = null;
        return true;
    }

    private static bool IsValidDestination(
        BattleDirectConsequenceCohortInput input,
        BattleCohortConsequenceLivingDestination destination,
        Dictionary<string, string> participantSides)
    {
        if (!Enum.IsDefined(typeof(ManpowerInjuryState), destination.InjuryState)
            || !Enum.IsDefined(typeof(ManpowerCustodyState), destination.CustodyState)
            || !Enum.IsDefined(typeof(ManpowerAvailabilityState), destination.AvailabilityState))
            return false;
        if (input.Identity.InjuryState == ManpowerInjuryState.Wounded
            && destination.InjuryState != ManpowerInjuryState.Wounded)
            return false;
        if (input.Identity.InjuryState == ManpowerInjuryState.Healthy
            && destination.InjuryState != ManpowerInjuryState.Healthy
            && destination.InjuryState != ManpowerInjuryState.Wounded)
            return false;
        if (destination.CustodyState == ManpowerCustodyState.Free)
            return destination.CustodianForceId == null;
        if (destination.CustodyState != ManpowerCustodyState.Captured
            || destination.AvailabilityState != ManpowerAvailabilityState.Unavailable
            || destination.CustodianForceId == null
            || !participantSides.TryGetValue(destination.CustodianForceId.Value, out string custodianSide))
            return false;
        return !string.Equals(custodianSide, input.Identity.SideId.Value, StringComparison.Ordinal);
    }

    private static BattleDirectConsequenceFailureCode DestinationFailureCode(
        BattleDirectConsequenceCohortInput input,
        BattleCohortConsequenceLivingDestination destination,
        Dictionary<string, string> participantSides)
    {
        if (destination != null && destination.CustodyState == ManpowerCustodyState.Captured)
        {
            if (destination.CustodianForceId == null
                || !participantSides.ContainsKey(destination.CustodianForceId.Value)
                || participantSides.TryGetValue(destination.CustodianForceId.Value, out string side)
                    && string.Equals(side, input.Identity.SideId.Value, StringComparison.Ordinal))
                return BattleDirectConsequenceFailureCode.InvalidCustodian;
        }
        if (destination != null && input.Identity.InjuryState == ManpowerInjuryState.Wounded
            && destination.InjuryState != ManpowerInjuryState.Wounded)
            return BattleDirectConsequenceFailureCode.InvalidTransition;
        return BattleDirectConsequenceFailureCode.InvalidTransition;
    }

    private bool TryPlanSourceDeaths(
        BattleDirectConsequenceInput input,
        IReadOnlyList<BattleCohortConsequencePartition> partitions,
        out List<BattleDirectConsequenceSourceGroup> groups,
        out BattleDirectConsequenceFailure failure)
    {
        groups = new List<BattleDirectConsequenceSourceGroup>();
        Dictionary<string, SourceDeathAccumulator> bySource =
            new Dictionary<string, SourceDeathAccumulator>(StringComparer.Ordinal);
        Dictionary<string, BattleDirectConsequenceCohortInput> inputByIdentity =
            new Dictionary<string, BattleDirectConsequenceCohortInput>(StringComparer.Ordinal);
        foreach (BattleDirectConsequenceCohortInput cohort in input.Cohorts)
            inputByIdentity.Add(cohort.Identity.StableKey, cohort);

        foreach (BattleCohortConsequencePartition partition in partitions)
        {
            if (partition.DeathAmount == 0L) continue;
            BattleDirectConsequenceCohortInput cohort = inputByIdentity[partition.InputIdentity.StableKey];
            if (cohort.SourceId == null)
                return Fail(BattleDirectConsequenceFailureCode.UnboundDeath,
                    "A positive cohort death has no explicit D6A manpower source binding.", out failure);
            string sourceKey = cohort.SourceId.Value;
            if (!bySource.TryGetValue(sourceKey, out SourceDeathAccumulator accumulator))
            {
                accumulator = new SourceDeathAccumulator(cohort.SourceId);
                bySource.Add(sourceKey, accumulator);
            }
            try
            {
                accumulator.DeathAmount = checked(accumulator.DeathAmount + partition.DeathAmount);
            }
            catch (OverflowException)
            {
                return Fail(BattleDirectConsequenceFailureCode.AmountOverflow,
                    "Grouped source deaths exceed Int64 capacity.", out failure);
            }
            accumulator.Traces.Add(new BattleDirectConsequenceDeathTrace(
                partition.InputIdentity,
                partition.DeathAmount));
        }

        List<string> sourceKeys = new List<string>(bySource.Keys);
        sourceKeys.Sort(StringComparer.Ordinal);
        foreach (string sourceKey in sourceKeys)
        {
            SourceDeathAccumulator accumulator = bySource[sourceKey];
            accumulator.Traces.Sort((left, right) => StringComparer.Ordinal.Compare(
                left.CohortIdentity.StableKey,
                right.CohortIdentity.StableKey));
            ManpowerSourceConsequencePlanningResult result;
            try
            {
                // Exactly one D6B1 proposal request per affected source.
                result = world.ManpowerSourceConsequencePlanningService.TryPlan(
                    new ManpowerSourceConsequenceRequest(
                        accumulator.SourceId,
                        new ManpowerSourceEffect(ManpowerSourceEffectKind.Death, accumulator.DeathAmount)));
            }
            catch
            {
                return Fail(BattleDirectConsequenceFailureCode.SourceConsequencePlanningFailed,
                    "The D6B1 source planner threw while planning a grouped death.", out failure);
            }
            if (result == null || !result.IsPlanned || result.Proposal == null)
            {
                failure = new BattleDirectConsequenceFailure(
                    BattleDirectConsequenceFailureCode.SourceConsequencePlanningFailed,
                    "D6B1 rejected a grouped source death.",
                    null,
                    null,
                    result?.Failure);
                return false;
            }
            ManpowerSourceConsequenceProposal proposal = result.Proposal;
            if (proposal.SourceId != accumulator.SourceId
                || proposal.Effect == null
                || proposal.Effect.Kind != ManpowerSourceEffectKind.Death
                || proposal.Effect.Amount != accumulator.DeathAmount
                || proposal.Disposition != ManpowerSourceConsequenceDisposition.DomainTransitionRequired)
                return Fail(BattleDirectConsequenceFailureCode.SourceProposalInvalid,
                    "D6B1 returned a proposal inconsistent with the grouped source death.", out failure);

            ManpowerSourceConsequenceValidationResult validation =
                world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);
            if (validation == null || validation.Status == ManpowerSourceConsequenceValidationStatus.Invalid)
                return Fail(BattleDirectConsequenceFailureCode.SourceProposalInvalid,
                    "D6B1 returned a structurally invalid source proposal.", out failure);
            if (!validation.IsCurrent)
                return Fail(BattleDirectConsequenceFailureCode.SourceProposalStale,
                    "A D6B1 source proposal became stale before D6B2 completion.", out failure);

            groups.Add(new BattleDirectConsequenceSourceGroup(
                accumulator.SourceId,
                accumulator.DeathAmount,
                accumulator.Traces,
                proposal));
        }
        failure = null;
        return true;
    }

    private bool TryBuildProjections(
        BattleExecutionContext context,
        IReadOnlyList<ContingentManpowerState> participantStates,
        IReadOnlyList<BattleDirectConsequenceCohortInput> inputs,
        IReadOnlyList<BattleCohortConsequencePartition> partitions,
        out List<BattleDirectConsequenceContingentProjection> projections,
        out BattleDirectConsequenceFailure failure)
    {
        projections = new List<BattleDirectConsequenceContingentProjection>();
        Dictionary<string, ContingentManpowerState> stateById =
            new Dictionary<string, ContingentManpowerState>(StringComparer.Ordinal);
        foreach (ContingentManpowerState state in participantStates)
            stateById.Add(state.ContingentId.Value, state);
        Dictionary<string, HashSet<string>> exposedByContingent =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (BattleDirectConsequenceCohortInput input in inputs)
        {
            if (!exposedByContingent.TryGetValue(input.Identity.ContingentId.Value, out HashSet<string> keys))
            {
                keys = new HashSet<string>(StringComparer.Ordinal);
                exposedByContingent.Add(input.Identity.ContingentId.Value, keys);
            }
            keys.Add(input.Identity.StableKey);
        }
        Dictionary<string, BattleCohortConsequencePartition> partitionByInput =
            new Dictionary<string, BattleCohortConsequencePartition>(StringComparer.Ordinal);
        foreach (BattleCohortConsequencePartition partition in partitions)
            partitionByInput.Add(partition.InputIdentity.StableKey, partition);

        foreach (BattleExecutionSideContext side in context.Sides)
        {
            foreach (BattleExecutionForceContext force in side.Forces)
            {
                foreach (BattleExecutionContingentSnapshot contingent in force.DirectContingents)
                {
                    if (!stateById.TryGetValue(contingent.ContingentId.Value, out ContingentManpowerState state)
                        || !string.Equals(state.Fingerprint, contingent.ManpowerStateFingerprint, StringComparison.Ordinal))
                        return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                            "A contingent projection lost its exact D6A source snapshot.", out failure);
                    exposedByContingent.TryGetValue(contingent.ContingentId.Value, out HashSet<string> exposedKeys);
                    Dictionary<string, DestinationAccumulator> merged =
                        new Dictionary<string, DestinationAccumulator>(StringComparer.Ordinal);
                    foreach (ContingentManpowerCohort existing in state.Cohorts)
                    {
                        string fullKey = BattleDirectConsequenceCohortIdentity.BuildStableKey(
                            side.SideId.Value,
                            force.ForceId.Value,
                            contingent.ContingentId.Value,
                            existing.InjuryState,
                            existing.CustodyState,
                            existing.CustodianForceId?.Value,
                            existing.AvailabilityState);
                        if (exposedKeys != null && exposedKeys.Contains(fullKey)) continue;
                        if (!TryAddDestination(merged, existing, out failure)) return false;
                    }

                    if (exposedKeys != null)
                    {
                        foreach (string exposedKey in exposedKeys)
                        {
                            if (!partitionByInput.TryGetValue(exposedKey, out BattleCohortConsequencePartition partition))
                                return Fail(BattleDirectConsequenceFailureCode.MissingPartition,
                                    "A post-state projection lacks an exposed cohort partition.", out failure);
                            foreach (BattleCohortConsequenceLivingDestination destination in partition.LivingDestinations)
                            {
                                if (!TryAddDestination(merged, destination, out failure)) return false;
                            }
                        }
                    }

                    List<ContingentManpowerCohort> cohorts = new List<ContingentManpowerCohort>();
                    foreach (DestinationAccumulator value in merged.Values)
                    {
                        if (value.Amount <= 0L) continue;
                        cohorts.Add(new ContingentManpowerCohort(
                            value.InjuryState,
                            value.CustodyState,
                            value.CustodianForceId,
                            value.AvailabilityState,
                            value.Amount));
                    }
                    projections.Add(new BattleDirectConsequenceContingentProjection(
                        contingent.ContingentId,
                        state.SourceId,
                        cohorts));
                }
            }
        }
        projections.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.ContingentId.Value,
            right.ContingentId.Value));
        failure = null;
        return true;
    }

    private static bool TryAddDestination(
        Dictionary<string, DestinationAccumulator> values,
        ContingentManpowerCohort cohort,
        out BattleDirectConsequenceFailure failure)
    {
        if (cohort == null || cohort.Amount <= 0L)
            return Fail(BattleDirectConsequenceFailureCode.InvalidManpowerState,
                "A source projection contains a non-positive cohort.", out failure);
        return TryAddDestination(values, new BattleCohortConsequenceLivingDestination(
            cohort.InjuryState,
            cohort.CustodyState,
            cohort.CustodianForceId,
            cohort.AvailabilityState,
            cohort.Amount), out failure);
    }

    private static bool TryAddDestination(
        Dictionary<string, DestinationAccumulator> values,
        BattleCohortConsequenceLivingDestination destination,
        out BattleDirectConsequenceFailure failure)
    {
        string key = DestinationKey(destination);
        if (!values.TryGetValue(key, out DestinationAccumulator accumulator))
        {
            accumulator = new DestinationAccumulator(destination);
            values.Add(key, accumulator);
        }
        try
        {
            accumulator.Amount = checked(accumulator.Amount + destination.Amount);
        }
        catch (OverflowException)
        {
            return Fail(BattleDirectConsequenceFailureCode.AmountOverflow,
                "The derived contingent post-state exceeds Int64 capacity.", out failure);
        }
        failure = null;
        return true;
    }

    private static string BuildPlanFingerprint(
        BattleOutcomeApplicationPlan d5Plan,
        string d6b2PolicyFingerprint,
        IReadOnlyList<BattleDirectConsequenceCohortInput> inputs,
        IReadOnlyList<BattleCohortConsequencePartition> partitions,
        IReadOnlyList<BattleDirectConsequenceSourceGroup> sourceGroups,
        IReadOnlyList<BattleDirectConsequenceContingentProjection> projections)
    {
        StringBuilder canonical = new StringBuilder();
        BattleResolutionProvenance provenance = d5Plan.Outcome.Provenance;
        BattleResolutionStableEncoding.Append(canonical, PlanSchemaVersion);
        BattleResolutionStableEncoding.Append(canonical, CoverageVersion);
        BattleResolutionStableEncoding.Append(canonical, d5Plan.BattleId.Value);
        BattleResolutionStableEncoding.Append(canonical, d5Plan.Outcome.ResolvedAbsoluteDay.ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, ((int)d5Plan.Outcome.OutcomeType).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, d5Plan.Outcome.WinningBattleSideId?.Value);
        BattleResolutionStableEncoding.Append(canonical, provenance.PolicyFingerprint);
        BattleResolutionStableEncoding.Append(canonical, provenance.NumericExecutionProfileKey);
        BattleResolutionStableEncoding.Append(canonical, provenance.ProjectionVersion);
        BattleResolutionStableEncoding.Append(canonical, provenance.CausalResolutionFingerprint);
        BattleResolutionStableEncoding.Append(canonical, provenance.SourceContextFingerprint);
        BattleResolutionStableEncoding.Append(canonical, provenance.CapabilityRuleKey);
        BattleResolutionStableEncoding.Append(canonical, provenance.RandomAuthorityRuleKey);
        BattleResolutionStableEncoding.Append(canonical, provenance.ResolverSettingsIdentity);
        BattleResolutionStableEncoding.Append(canonical, d6b2PolicyFingerprint);

        BattleResolutionStableEncoding.Append(canonical, "inputs");
        foreach (BattleDirectConsequenceCohortInput input in inputs)
        {
            BattleResolutionStableEncoding.Append(canonical, input.Identity.StableKey);
            BattleResolutionStableEncoding.Append(canonical, input.Amount.ToString(CultureInfo.InvariantCulture));
            BattleResolutionStableEncoding.Append(canonical, input.SourceId?.Value);
        }
        BattleResolutionStableEncoding.Append(canonical, "partitions");
        foreach (BattleCohortConsequencePartition partition in partitions)
        {
            BattleResolutionStableEncoding.Append(canonical, partition.InputIdentity.StableKey);
            BattleResolutionStableEncoding.Append(canonical, partition.InputAmount.ToString(CultureInfo.InvariantCulture));
            BattleResolutionStableEncoding.Append(canonical, partition.DeathAmount.ToString(CultureInfo.InvariantCulture));
            foreach (BattleCohortConsequenceLivingDestination destination in partition.LivingDestinations)
            {
                BattleResolutionStableEncoding.Append(canonical, DestinationKey(destination));
                BattleResolutionStableEncoding.Append(canonical, destination.Amount.ToString(CultureInfo.InvariantCulture));
            }
        }
        BattleResolutionStableEncoding.Append(canonical, "source-groups");
        foreach (BattleDirectConsequenceSourceGroup group in sourceGroups)
        {
            ManpowerSourceConsequenceProposal proposal = group.Proposal;
            BattleResolutionStableEncoding.Append(canonical, group.SourceId.Value);
            BattleResolutionStableEncoding.Append(canonical, group.DeathAmount.ToString(CultureInfo.InvariantCulture));
            BattleResolutionStableEncoding.Append(canonical, proposal.DependencyFingerprint);
            BattleResolutionStableEncoding.Append(canonical, proposal.PlannerRuleKey);
            BattleResolutionStableEncoding.Append(canonical, proposal.PlannerConfigurationIdentity);
            BattleResolutionStableEncoding.Append(canonical, proposal.ProposalVersion.ToString(CultureInfo.InvariantCulture));
            BattleResolutionStableEncoding.Append(canonical, ((int)proposal.Disposition).ToString(CultureInfo.InvariantCulture));
            foreach (BattleDirectConsequenceDeathTrace trace in group.Traces)
            {
                BattleResolutionStableEncoding.Append(canonical, trace.CohortIdentity.StableKey);
                BattleResolutionStableEncoding.Append(canonical, trace.DeathAmount.ToString(CultureInfo.InvariantCulture));
            }
        }
        BattleResolutionStableEncoding.Append(canonical, "contingent-projections");
        foreach (BattleDirectConsequenceContingentProjection projection in projections)
        {
            BattleResolutionStableEncoding.Append(canonical, projection.ContingentId.Value);
            BattleResolutionStableEncoding.Append(canonical, projection.SourceId?.Value);
            foreach (ContingentManpowerCohort cohort in projection.Cohorts)
            {
                BattleResolutionStableEncoding.Append(canonical, ((int)cohort.InjuryState).ToString(CultureInfo.InvariantCulture));
                BattleResolutionStableEncoding.Append(canonical, ((int)cohort.CustodyState).ToString(CultureInfo.InvariantCulture));
                BattleResolutionStableEncoding.Append(canonical, cohort.CustodianForceId?.Value);
                BattleResolutionStableEncoding.Append(canonical, ((int)cohort.AvailabilityState).ToString(CultureInfo.InvariantCulture));
                BattleResolutionStableEncoding.Append(canonical, cohort.Amount.ToString(CultureInfo.InvariantCulture));
            }
        }
        return "battle-direct-consequence-plan:sha256-v1:"
            + BattleResolutionStableEncoding.Sha256Key(canonical.ToString())
                .Substring("battle-causal:sha256-v1:".Length);
    }

    private bool ValidatePlanShape(BattleDirectConsequencePlan plan)
    {
        if (plan == null
            || plan.BattleId == null
            || plan.SourceContext == null
            || plan.D5ApplicationPlan == null
            || plan.Inputs == null
            || plan.Partitions == null
            || plan.SourceGroups == null
            || plan.ContingentProjections == null
            || !IsValidD5Plan(plan.D5ApplicationPlan, plan.BattleId)
            || plan.AbsoluteDay != plan.D5ApplicationPlan.Outcome.ResolvedAbsoluteDay
            || plan.OutcomeType != plan.D5ApplicationPlan.Outcome.OutcomeType
            || plan.WinningSideId != plan.D5ApplicationPlan.Outcome.WinningBattleSideId
            || !string.Equals(plan.D5SourceContextFingerprint, plan.SourceContext.StableKey, StringComparison.Ordinal)
            || !string.Equals(plan.D5SourceContextFingerprint, plan.D5ApplicationPlan.SourceContextFingerprint, StringComparison.Ordinal)
            || plan.Inputs.Count != plan.Partitions.Count)
            return false;

        Dictionary<string, BattleDirectConsequenceCohortInput> inputs =
            new Dictionary<string, BattleDirectConsequenceCohortInput>(StringComparer.Ordinal);
        foreach (BattleDirectConsequenceCohortInput input in plan.Inputs)
        {
            if (input == null || input.Identity == null || input.Amount <= 0L
                || inputs.ContainsKey(input.Identity.StableKey))
                return false;
            inputs.Add(input.Identity.StableKey, input);
        }
        Dictionary<string, string> participantSides = BuildParticipantSideMap(plan.SourceContext);
        Dictionary<string, BattleCohortConsequencePartition> partitions =
            new Dictionary<string, BattleCohortConsequencePartition>(StringComparer.Ordinal);
        foreach (BattleCohortConsequencePartition partition in plan.Partitions)
        {
            if (partition == null || partition.InputIdentity == null
                || !inputs.TryGetValue(partition.InputIdentity.StableKey, out BattleDirectConsequenceCohortInput input)
                || partitions.ContainsKey(partition.InputIdentity.StableKey)
                || partition.InputAmount != input.Amount
                || partition.DeathAmount < 0L
                || partition.DeathAmount > 0L && input.SourceId == null)
                return false;
            partitions.Add(partition.InputIdentity.StableKey, partition);
            long total = partition.DeathAmount;
            HashSet<string> destinationKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (BattleCohortConsequenceLivingDestination destination in partition.LivingDestinations)
            {
                if (destination == null || destination.Amount <= 0L
                    || !IsValidDestination(input, destination, participantSides)
                    || !destinationKeys.Add(DestinationKey(destination)))
                    return false;
                try { total = checked(total + destination.Amount); }
                catch (OverflowException) { return false; }
            }
            if (total != input.Amount) return false;
        }
        if (partitions.Count != inputs.Count) return false;

        Dictionary<string, long> expectedDeaths = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (BattleCohortConsequencePartition partition in plan.Partitions)
        {
            if (partition.DeathAmount <= 0L) continue;
            BattleDirectConsequenceCohortInput input = inputs[partition.InputIdentity.StableKey];
            string sourceKey = input.SourceId.Value;
            expectedDeaths.TryGetValue(sourceKey, out long current);
            try { expectedDeaths[sourceKey] = checked(current + partition.DeathAmount); }
            catch (OverflowException) { return false; }
        }
        List<string> groupKeys = new List<string>();
        string previousSource = null;
        foreach (BattleDirectConsequenceSourceGroup group in plan.SourceGroups)
        {
            if (group == null || group.SourceId == null || group.DeathAmount <= 0L
                || group.Proposal == null || group.Traces == null
                || !expectedDeaths.TryGetValue(group.SourceId.Value, out long expected)
                || expected != group.DeathAmount
                || group.Proposal.SourceId != group.SourceId
                || group.Proposal.Effect == null
                || group.Proposal.Effect.Kind != ManpowerSourceEffectKind.Death
                || group.Proposal.Effect.Amount != group.DeathAmount
                || previousSource != null && StringComparer.Ordinal.Compare(previousSource, group.SourceId.Value) >= 0)
                return false;
            previousSource = group.SourceId.Value;
            groupKeys.Add(group.SourceId.Value);
            long tracedAmount = 0L;
            foreach (BattleDirectConsequenceDeathTrace trace in group.Traces)
            {
                if (trace == null || trace.CohortIdentity == null || trace.DeathAmount <= 0L
                    || !inputs.TryGetValue(trace.CohortIdentity.StableKey, out BattleDirectConsequenceCohortInput tracedInput)
                    || tracedInput.SourceId != group.SourceId
                    || !partitions.TryGetValue(trace.CohortIdentity.StableKey, out BattleCohortConsequencePartition tracedPartition)
                    || tracedPartition.DeathAmount != trace.DeathAmount)
                    return false;
                try { tracedAmount = checked(tracedAmount + trace.DeathAmount); }
                catch (OverflowException) { return false; }
            }
            if (tracedAmount != group.DeathAmount) return false;
        }
        if (groupKeys.Count != expectedDeaths.Count) return false;
        foreach (string sourceKey in expectedDeaths.Keys)
            if (!groupKeys.Contains(sourceKey)) return false;

        string expectedFingerprint;
        try
        {
            expectedFingerprint = BuildPlanFingerprint(
                plan.D5ApplicationPlan,
                plan.D6B2PolicyFingerprint,
                plan.Inputs,
                plan.Partitions,
                plan.SourceGroups,
                plan.ContingentProjections);
        }
        catch
        {
            return false;
        }
        return !string.IsNullOrWhiteSpace(plan.Fingerprint)
            && string.Equals(plan.Fingerprint, expectedFingerprint, StringComparison.Ordinal);
    }

    private static bool IsValidD5Plan(BattleOutcomeApplicationPlan plan, BattleId battleId)
    {
        if (plan == null || plan.BattleId == null || plan.Outcome == null
            || plan.ResolutionComputation == null || plan.SourceDependencies == null
            || plan.Outcome.Provenance == null || plan.BattleId != battleId
            || plan.Outcome.BattleId != battleId
            || plan.Outcome.OutcomeType != BattleOutcomeType.Victory
                && plan.Outcome.OutcomeType != BattleOutcomeType.Draw
            || plan.Outcome.OutcomeType == BattleOutcomeType.Victory
                && plan.Outcome.WinningBattleSideId == null
            || plan.Outcome.OutcomeType == BattleOutcomeType.Draw
                && plan.Outcome.WinningBattleSideId != null
            || plan.DirectConsequenceStatus != BattleDirectConsequencePlanStatus.NotProvided
            || plan.HasCompleteDirectConsequencePlan
            || plan.IsCommitReady)
            return false;
        BattleResolutionProvenance provenance = plan.Outcome.Provenance;
        return !string.IsNullOrWhiteSpace(provenance.PolicyFingerprint)
            && !string.IsNullOrWhiteSpace(provenance.CausalResolutionFingerprint)
            && !string.IsNullOrWhiteSpace(provenance.SourceContextFingerprint)
            && string.Equals(provenance.SourceContextFingerprint, plan.SourceContextFingerprint, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> BuildParticipantSideMap(BattleExecutionContext context)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (BattleExecutionSideContext side in context.Sides)
        {
            foreach (BattleExecutionForceContext force in side.Forces)
            {
                if (side?.SideId != null && force?.ForceId != null)
                    result[force.ForceId.Value] = side.SideId.Value;
            }
        }
        return result;
    }

    private static bool ValidateCustodiansAgainstCurrentContext(
        BattleDirectConsequencePlan plan,
        BattleExecutionContext currentContext)
    {
        Dictionary<string, string> currentSides = BuildParticipantSideMap(currentContext);
        foreach (BattleDirectConsequenceCohortInput input in plan.Inputs)
        {
            if (!currentSides.TryGetValue(input.Identity.ForceId.Value, out string inputSide)
                || !string.Equals(inputSide, input.Identity.SideId.Value, StringComparison.Ordinal))
                return false;
        }
        foreach (BattleCohortConsequencePartition partition in plan.Partitions)
        {
            foreach (BattleCohortConsequenceLivingDestination destination in partition.LivingDestinations)
            {
                if (destination.CustodyState != ManpowerCustodyState.Captured) continue;
                if (!currentSides.TryGetValue(destination.CustodianForceId?.Value ?? string.Empty, out string custodianSide)
                    || string.Equals(custodianSide, partition.InputIdentity.SideId.Value, StringComparison.Ordinal))
                    return false;
            }
        }
        return true;
    }

    private bool CapturedCustodiansRemainActive(BattleDirectConsequencePlan plan)
    {
        foreach (BattleCohortConsequencePartition partition in plan.Partitions)
        {
            foreach (BattleCohortConsequenceLivingDestination destination in partition.LivingDestinations)
            {
                if (destination.CustodyState != ManpowerCustodyState.Captured) continue;
                if (destination.CustodianForceId == null
                    || !world.ArmedForceStore.TryGet(destination.CustodianForceId, out ArmedForceRecord force)
                    || !force.IsActive)
                    return false;
            }
        }
        return true;
    }

    private static int CompareInputs(
        BattleDirectConsequenceCohortInput left,
        BattleDirectConsequenceCohortInput right)
    {
        int c = StringComparer.Ordinal.Compare(left.Identity.SideId.Value, right.Identity.SideId.Value);
        if (c != 0) return c;
        c = StringComparer.Ordinal.Compare(left.Identity.ForceId.Value, right.Identity.ForceId.Value);
        if (c != 0) return c;
        c = StringComparer.Ordinal.Compare(left.Identity.ContingentId.Value, right.Identity.ContingentId.Value);
        return c != 0 ? c : StringComparer.Ordinal.Compare(left.Identity.StableKey, right.Identity.StableKey);
    }

    private static int CompareDestinations(
        BattleCohortConsequenceLivingDestination left,
        BattleCohortConsequenceLivingDestination right)
    {
        int c = left.InjuryState.CompareTo(right.InjuryState);
        if (c != 0) return c;
        c = left.CustodyState.CompareTo(right.CustodyState);
        if (c != 0) return c;
        c = StringComparer.Ordinal.Compare(left.CustodianForceId?.Value, right.CustodianForceId?.Value);
        return c != 0 ? c : left.AvailabilityState.CompareTo(right.AvailabilityState);
    }

    private static string DestinationKey(BattleCohortConsequenceLivingDestination destination)
    {
        StringBuilder builder = new StringBuilder();
        BattleResolutionStableEncoding.Append(builder, ((int)destination.InjuryState).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(builder, ((int)destination.CustodyState).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(builder, destination.CustodianForceId?.Value);
        BattleResolutionStableEncoding.Append(builder, ((int)destination.AvailabilityState).ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static string RawPartitionSortKey(BattleCohortConsequencePartition partition)
    {
        StringBuilder canonical = new StringBuilder();
        if (partition == null)
        {
            BattleResolutionStableEncoding.Append(canonical, "null-partition");
            return canonical.ToString();
        }
        BattleResolutionStableEncoding.Append(canonical, "partition");
        BattleResolutionStableEncoding.Append(canonical, partition.InputIdentity?.StableKey);
        BattleResolutionStableEncoding.Append(canonical, partition.InputAmount.ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, partition.DeathAmount.ToString(CultureInfo.InvariantCulture));
        List<string> destinations = new List<string>();
        foreach (BattleCohortConsequenceLivingDestination destination in partition.LivingDestinations)
            destinations.Add(RawDestinationSortKey(destination));
        destinations.Sort(StringComparer.Ordinal);
        BattleResolutionStableEncoding.Append(canonical, destinations.Count.ToString(CultureInfo.InvariantCulture));
        foreach (string destination in destinations)
            BattleResolutionStableEncoding.Append(canonical, destination);
        return canonical.ToString();
    }

    private static string RawDestinationSortKey(BattleCohortConsequenceLivingDestination destination)
    {
        StringBuilder canonical = new StringBuilder();
        if (destination == null)
        {
            BattleResolutionStableEncoding.Append(canonical, "null-destination");
            return canonical.ToString();
        }
        BattleResolutionStableEncoding.Append(canonical, "destination");
        BattleResolutionStableEncoding.Append(canonical, ((int)destination.InjuryState).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, ((int)destination.CustodyState).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, destination.CustodianForceId?.Value);
        BattleResolutionStableEncoding.Append(canonical, ((int)destination.AvailabilityState).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, destination.Amount.ToString(CultureInfo.InvariantCulture));
        return canonical.ToString();
    }

    private static bool Fail(
        BattleDirectConsequenceFailureCode code,
        string message,
        out BattleDirectConsequenceFailure failure)
    {
        failure = new BattleDirectConsequenceFailure(code, message);
        return false;
    }

    private sealed class DestinationAccumulator
    {
        public readonly ManpowerInjuryState InjuryState;
        public readonly ManpowerCustodyState CustodyState;
        public readonly ArmedForceId CustodianForceId;
        public readonly ManpowerAvailabilityState AvailabilityState;
        public long Amount;

        public DestinationAccumulator(BattleCohortConsequenceLivingDestination destination)
        {
            InjuryState = destination.InjuryState;
            CustodyState = destination.CustodyState;
            CustodianForceId = destination.CustodianForceId;
            AvailabilityState = destination.AvailabilityState;
            Amount = 0L;
        }
    }

    private sealed class SourceDeathAccumulator
    {
        public readonly ManpowerSourceId SourceId;
        public readonly List<BattleDirectConsequenceDeathTrace> Traces =
            new List<BattleDirectConsequenceDeathTrace>();
        public long DeathAmount;

        public SourceDeathAccumulator(ManpowerSourceId sourceId)
        {
            SourceId = sourceId;
        }
    }
}
