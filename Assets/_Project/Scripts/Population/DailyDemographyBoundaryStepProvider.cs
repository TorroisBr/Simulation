using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Adapts the existing daily demographic owner to one resumable P18-D boundary
/// step. The owner retains its report and progress; the boundary coordinator only
/// sees this step's stable descriptor and completion receipt.
/// </summary>
public sealed class DailyDemographyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    public const string OwnerId = "daily-demography";
    public const string StepId = "daily-demography";
    public const string OperationKind = "daily-demography";
    public const string OperationVersion = "1";

    private readonly DailyDemographyBoundaryOwner owner;

    public DailyDemographyBoundaryStepProvider(
        SimulationRuntime world,
        IPersonNaturalMortalitySampleProvider mortalitySamples = null,
        IAggregateDemographyProvider aggregateProvider = null)
    {
        owner = new DailyDemographyBoundaryOwner(world, mortalitySamples, aggregateProvider);
    }

    public bool TryCreateSteps(
        DailyBoundaryOperation operation,
        int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps,
        out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.None;
        if (firstOrdinal < 0
            || !owner.TryCreateStep(operation, firstOrdinal,
                out BoundaryContinuationStep step, out failure))
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
        && string.Equals(step.OwnerId, OwnerId, StringComparison.Ordinal)
        && string.Equals(step.StepId, StepId, StringComparison.Ordinal)
        && string.Equals(step.OperationKind, OperationKind, StringComparison.Ordinal)
        && string.Equals(step.OperationVersion, OperationVersion, StringComparison.Ordinal);

    public bool TryPrepareStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!OwnsStep(step))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return owner.TryPrepareStep(manifest, step, out prepared, out failure);
    }

    public bool TryResolveReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out DailyDemographyBoundaryReceipt receipt,
        out TimelineFailure failure) =>
        owner.TryResolveReceipt(manifest, step, out receipt, out failure);
}

/// <summary>
/// Runtime-scoped domain owner for the P18-D daily demography occurrence. A
/// resumed call continues at the first uncommitted Person or City target, using
/// retained policy results and exact owner transitions where an effect response
/// was uncertain. It never delegates per-Person or per-City ordering to P18.
/// </summary>
public sealed class DailyDemographyBoundaryOwner
{
    private const string PayloadVersion = "daily-demography-boundary-v1";

    private readonly SimulationRuntime world;
    private readonly IPersonNaturalMortalitySampleProvider mortalitySamples;
    private readonly IAggregateDemographyProvider aggregateProvider;
#if UNITY_EDITOR
    // Private editor-only seam used by the focused retry test to interrupt after
    // proposal retention but before the aggregate owner receives the install call.
    private Func<AggregateDemographyTransition, bool> interruptAggregateInstallForTest;
#endif
    private Dictionary<string, DailyDemographyBoundaryReceipt> receipts =
        new Dictionary<string, DailyDemographyBoundaryReceipt>(StringComparer.Ordinal);
    private readonly Dictionary<string, OccurrenceState> inProgress =
        new Dictionary<string, OccurrenceState>(StringComparer.Ordinal);
    private long revision;

    public DailyDemographyBoundaryOwner(
        SimulationRuntime world,
        IPersonNaturalMortalitySampleProvider mortalitySamples = null,
        IAggregateDemographyProvider aggregateProvider = null)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.mortalitySamples = mortalitySamples ?? new DeterministicDemographicSampleProvider();
        this.aggregateProvider = aggregateProvider;
    }

    public bool TryCreateStep(
        DailyBoundaryOperation operation,
        int ordinal,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        if (operation == null || ordinal < 0 || operation.AbsoluteDay != world.CurrentDay
            || revision == long.MaxValue
            || !TryCaptureDescriptor(operation.AbsoluteDay, out FrozenDescriptor descriptor))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        string ownerRevision = revision.ToString(CultureInfo.InvariantCulture);
        step = new BoundaryContinuationStep(
            ordinal,
            DailyDemographyBoundaryStepProvider.StepId,
            DailyDemographyBoundaryStepProvider.OwnerId,
            DailyDemographyBoundaryStepProvider.OperationKind,
            DailyDemographyBoundaryStepProvider.OperationVersion,
            ownerRevision,
            EncodeDescriptor(descriptor));
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryPrepareStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryValidateStep(manifest, step, out string identity, out string fingerprint,
                out FrozenDescriptor descriptor))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (receipts.TryGetValue(identity, out DailyDemographyBoundaryReceipt existingReceipt))
        {
            if (!string.Equals(existingReceipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            prepared = new DailyDemographyBoundaryStepCommit(this, identity, fingerprint, null, existingReceipt);
            failure = TimelineFailure.None;
            return true;
        }

        if (!inProgress.TryGetValue(identity, out OccurrenceState state))
        {
            failure = TimelineFailure.None;
            if (!long.TryParse(step.OwnerRevision, NumberStyles.None, CultureInfo.InvariantCulture,
                    out long expectedRevision)
                || expectedRevision != revision
                || !TryValidateCurrentTargets(descriptor, out failure))
            {
                if (failure == TimelineFailure.None) failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            state = new OccurrenceState(
                identity,
                BuildSemanticOccurrenceIdentity(manifest, step),
                fingerprint,
                descriptor);
            inProgress.Add(identity, state);
        }
        else if (!string.Equals(state.DescriptorFingerprint, fingerprint, StringComparison.Ordinal)
            || !string.Equals(state.Descriptor.Fingerprint, descriptor.Fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (state.CommitPrepared)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        state.CommitPrepared = true;
        prepared = new DailyDemographyBoundaryStepCommit(this, identity, fingerprint, state, null);
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryResolveReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out DailyDemographyBoundaryReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryValidateStep(manifest, step, out string identity, out string fingerprint, out _))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (!receipts.TryGetValue(identity, out receipt))
        {
            failure = TimelineFailure.None;
            return false;
        }

        if (!string.Equals(receipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
        {
            receipt = null;
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        failure = TimelineFailure.None;
        return true;
    }

    private bool TryCommit(
        string identity,
        string fingerprint,
        OccurrenceState state,
        DailyDemographyBoundaryReceipt replayReceipt,
        out TimelineFailure failure)
    {
        if (replayReceipt != null)
        {
            if (receipts.TryGetValue(identity, out DailyDemographyBoundaryReceipt current)
                && ReferenceEquals(current, replayReceipt)
                && string.Equals(current.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.None;
                return true;
            }

            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (state == null || !state.CommitPrepared
            || !inProgress.TryGetValue(identity, out OccurrenceState currentState)
            || !ReferenceEquals(state, currentState)
            || !string.Equals(state.DescriptorFingerprint, fingerprint, StringComparison.Ordinal)
            || !string.Equals(state.Descriptor.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        try
        {
            if (!ContinueOccurrence(state, out failure))
            {
                state.CommitPrepared = false;
                return false;
            }

            if (revision == long.MaxValue)
            {
                state.CommitPrepared = false;
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            long afterRevision = revision + 1L;
            DailyDemographyBoundaryReceipt receipt = new DailyDemographyBoundaryReceipt(
                identity,
                fingerprint,
                revision,
                afterRevision,
                state.Report.Copy());
            Dictionary<string, DailyDemographyBoundaryReceipt> nextReceipts =
                new Dictionary<string, DailyDemographyBoundaryReceipt>(receipts, StringComparer.Ordinal)
                {
                    [identity] = receipt
                };

            // Receipt, result, and owner revision become visible through one owner-state swap.
            receipts = nextReceipts;
            revision = afterRevision;
            state.CommitPrepared = false;
            inProgress.Remove(identity);
            failure = TimelineFailure.None;
            return true;
        }
        catch (Exception)
        {
            state.CommitPrepared = false;
            failure = TimelineFailure.DispatchFailed;
            return false;
        }
    }

    private bool ContinueOccurrence(OccurrenceState state, out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        if (world.CurrentDay != state.Descriptor.AbsoluteDay)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (state.Descriptor.NaturalMortalityEnabled)
        {
            while (state.NextPersonIndex < state.Descriptor.Persons.Count)
            {
                FrozenPersonTarget target = state.Descriptor.Persons[state.NextPersonIndex];
                if (state.PendingDeath != null)
                {
                    if (!CommitPendingDeath(state, target, out failure)) return false;
                    continue;
                }

                if (!world.PersonStore.TryGet(new PersonId(target.PersonId), out PersonRuntime person))
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "NaturalMortalityApplyRejected", target.PersonId,
                        PersonDeathLifecycleFailure.PersonNotRegistered.ToString());
                    state.NextPersonIndex++;
                    continue;
                }

                // A dead-at-activation target was present in the frozen roster but was
                // skipped by the legacy owner. A later death is likewise skipped unless
                // it is the exact pending transition retained by this owner.
                if (person.DeathAbsoluteDay.HasValue)
                {
                    state.NextPersonIndex++;
                    continue;
                }

                if (!MatchesFrozenMaterialization(target, person))
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "NaturalMortalityApplyRejected", target.PersonId,
                        PersonDeathLifecycleFailure.StaleMaterialization.ToString());
                    state.NextPersonIndex++;
                    continue;
                }

                state.Report.NamedPersonsEvaluated++;
                double sample;
                try
                {
                    sample = mortalitySamples.GetSample(person.PersonId, state.Descriptor.AbsoluteDay);
                }
                catch (Exception exception)
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "NaturalMortalitySampleFailed", target.PersonId, exception.Message);
                    state.NextPersonIndex++;
                    continue;
                }

                if (!PersonNaturalMortalityQuery.TryEvaluate(
                        person,
                        state.Descriptor.AbsoluteDay,
                        world.Calendar,
                        state.Descriptor.NaturalMortalityAnnualProbability,
                        sample,
                        out PersonNaturalMortalityEvaluation evaluation,
                        out PersonNaturalMortalityQueryFailure evaluationFailure))
                {
                    DailyDemographyDiagnosticSeverity severity =
                        evaluationFailure == PersonNaturalMortalityQueryFailure.BirthDateUnknown
                            ? DailyDemographyDiagnosticSeverity.Warning
                            : DailyDemographyDiagnosticSeverity.Error;
                    state.Report.Add(severity, "NaturalMortalityEvaluationRejected",
                        target.PersonId, evaluationFailure.ToString());
                    state.NextPersonIndex++;
                    continue;
                }

                if (!evaluation.ShouldDie)
                {
                    state.NextPersonIndex++;
                    continue;
                }

                if (!world.TryProposePersonDeath(person.PersonId,
                        out PersonDeathTransition transition,
                        out PersonDeathLifecycleFailure proposalFailure))
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "NaturalMortalityApplyRejected", target.PersonId, proposalFailure.ToString());
                    state.NextPersonIndex++;
                    continue;
                }

                transition = transition.WithOperationIdentity(
                    BuildPersonDeathEffectIdentity(state, target.PersonId));

                // Retain the exact decision and validated transition before invoking
                // the owning death lifecycle, so an uncertain response cannot resample.
                state.PendingDeath = transition;
                state.PendingDeathPersonId = target.PersonId;
                if (!CommitPendingDeath(state, target, out failure)) return false;
            }
        }

        if (!state.AggregateStarted)
        {
            state.AggregateStarted = true;
            if (state.Descriptor.AggregateDemographyEnabled)
            {
                try
                {
                    state.Floors = world.BuildRepresentedResidentFloorSnapshot();
                }
                catch (Exception)
                {
                    failure = TimelineFailure.DispatchFailed;
                    return false;
                }
            }
        }

        if (state.Descriptor.AggregateDemographyEnabled)
        {
            while (state.NextCityIndex < state.Descriptor.Cities.Count)
            {
                FrozenCityTarget target = state.Descriptor.Cities[state.NextCityIndex];
                if (state.PendingAggregate != null)
                {
                    if (!CommitPendingAggregate(state, target, out failure)) return false;
                    continue;
                }

                if (!TryResolveCity(target.RuntimeId, out CityRuntime city))
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "AggregateDemographyApplyRejected", target.RuntimeId,
                        AggregateDemographyFailure.InvalidSettlement.ToString());
                    state.NextCityIndex++;
                    continue;
                }

                if (state.Floors == null
                    || !state.Floors.TryGetFloor(target.RuntimeId, out int floor))
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "RepresentedResidentFloorMissing", target.RuntimeId,
                        "The world did not provide a represented-resident floor.");
                    state.NextCityIndex++;
                    continue;
                }

                IAggregateDemographyProvider provider = aggregateProvider
                    ?? new DeterministicAggregateDemographyProvider(
                        world.Calendar,
                        state.Descriptor.AggregateAnnualBirthRate,
                        state.Descriptor.AggregateAnnualDeathRate);
                try
                {
                    if (!AggregateDemographySystem.TryPropose(
                            city.Population,
                            floor,
                            provider,
                            state.Descriptor.AbsoluteDay,
                            out AggregateDemographyTransition transition,
                            out AggregateDemographyFailure proposalFailure))
                    {
                        state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                            "AggregateDemographyProposalRejected", target.RuntimeId,
                            proposalFailure.ToString());
                        state.NextCityIndex++;
                        continue;
                    }

                    state.PendingAggregate = transition;
#if UNITY_EDITOR
                    if (interruptAggregateInstallForTest != null
                        && interruptAggregateInstallForTest(transition))
                    {
                        failure = TimelineFailure.DispatchFailed;
                        return false;
                    }
#endif
                    if (!CommitPendingAggregate(state, target, out failure)) return false;
                }
                catch (Exception exception)
                {
                    state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                        "AggregateDemographyProviderFailed", target.RuntimeId, exception.Message);
                    state.NextCityIndex++;
                }
            }
        }

        return true;
    }

    private bool CommitPendingDeath(
        OccurrenceState state,
        FrozenPersonTarget target,
        out TimelineFailure failure)
    {
        PersonDeathTransition transition = state.PendingDeath;
        if (transition == null || !string.Equals(
                transition.PersonId?.Value, state.PendingDeathPersonId, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        PersonDeathOperationReceiptResolution receiptResolution =
            PersonDeathLifecycleSystem.ResolveOperationReceipt(
                world, transition, out PersonDeathLifecycleFailure receiptFailure);
        if (receiptResolution == PersonDeathOperationReceiptResolution.Conflicting)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (receiptResolution == PersonDeathOperationReceiptResolution.Matching)
        {
            RecordDeathSuccess(state);
            failure = TimelineFailure.None;
            return true;
        }

        try
        {
            if (world.TryApplyPersonDeath(transition, out PersonDeathLifecycleFailure deathFailure))
            {
                RecordDeathSuccess(state);
                failure = TimelineFailure.None;
                return true;
            }

            receiptResolution = PersonDeathLifecycleSystem.ResolveOperationReceipt(
                world, transition, out receiptFailure);
            if (receiptResolution == PersonDeathOperationReceiptResolution.Matching)
            {
                RecordDeathSuccess(state);
                failure = TimelineFailure.None;
                return true;
            }

            if (receiptResolution == PersonDeathOperationReceiptResolution.Conflicting)
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            if (deathFailure == PersonDeathLifecycleFailure.OperationIdentityConflict)
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                "NaturalMortalityApplyRejected", target.PersonId, deathFailure.ToString());
            state.PendingDeath = null;
            state.PendingDeathPersonId = null;
            state.NextPersonIndex++;
            failure = TimelineFailure.None;
            return true;
        }
        catch (Exception)
        {
            receiptResolution = PersonDeathLifecycleSystem.ResolveOperationReceipt(
                world, transition, out receiptFailure);
            if (receiptResolution == PersonDeathOperationReceiptResolution.Matching)
            {
                RecordDeathSuccess(state);
                failure = TimelineFailure.None;
                return true;
            }

            failure = TimelineFailure.DispatchFailed;
            return false;
        }
    }

    private bool CommitPendingAggregate(
        OccurrenceState state,
        FrozenCityTarget target,
        out TimelineFailure failure)
    {
        AggregateDemographyTransition transition = state.PendingAggregate;
        if (transition == null
            || !string.Equals(transition.SettlementRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            || !TryResolveCity(target.RuntimeId, out CityRuntime city))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        string operationIdentity = BuildCityAggregateEffectIdentity(state, target.RuntimeId);

        try
        {
            if (AggregateDemographySystem.TryApplyWithReceipt(
                    city.Population,
                    transition.RepresentedResidentFloor,
                    transition,
                    operationIdentity,
                    out _,
                    out AggregateDemographyFailure applyFailure))
            {
                RecordAggregateSuccess(state, transition);
                failure = TimelineFailure.None;
                return true;
            }

            if (applyFailure == AggregateDemographyFailure.OperationIdentityConflict)
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            state.Report.Add(DailyDemographyDiagnosticSeverity.Error,
                "AggregateDemographyApplyRejected", target.RuntimeId, applyFailure.ToString());
            state.PendingAggregate = null;
            state.NextCityIndex++;
            failure = TimelineFailure.None;
            return true;
        }
        catch (Exception)
        {
            // The exact proposal stays in PendingAggregate. Retrying this call
            // resolves the population receipt and never re-runs the provider.
            if (AggregateDemographySystem.TryApplyWithReceipt(
                    city.Population,
                    transition.RepresentedResidentFloor,
                    transition,
                    operationIdentity,
                    out _,
                    out _))
            {
                RecordAggregateSuccess(state, transition);
                failure = TimelineFailure.None;
                return true;
            }

            failure = TimelineFailure.DispatchFailed;
            return false;
        }
    }

    private static void RecordAggregateSuccess(
        OccurrenceState state,
        AggregateDemographyTransition transition)
    {
        state.Report.AggregateSettlementsProcessed++;
        state.Report.AggregateBirthsApplied += transition.Births;
        state.Report.AggregateDeathsApplied += transition.Deaths;
        state.PendingAggregate = null;
        state.NextCityIndex++;
    }

    private static void RecordDeathSuccess(OccurrenceState state)
    {
        state.Report.NamedDeathsApplied++;
        state.PendingDeath = null;
        state.PendingDeathPersonId = null;
        state.NextPersonIndex++;
    }

    private static string BuildPersonDeathEffectIdentity(OccurrenceState state, string personId)
    {
        return SpatialStableKey.Encode(
            state.SemanticOccurrenceIdentity,
            "natural-mortality-person-death-v1",
            personId);
    }

    private static string BuildCityAggregateEffectIdentity(OccurrenceState state, string cityRuntimeId) =>
        SpatialStableKey.Encode(
            state.SemanticOccurrenceIdentity,
            "city-aggregate-demography-v1",
            cityRuntimeId);

    private static string BuildSemanticOccurrenceIdentity(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step) =>
        SpatialStableKey.Encode(
            "daily-demography-occurrence-v1",
            manifest.BoundaryOccurrenceId,
            manifest.WorldId,
            manifest.ProfileId,
            manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            manifest.ConfigurationIdentity,
            manifest.ContentIdentity,
            step.OwnerId,
            step.StepId,
            step.OperationKind,
            step.OperationVersion);

    private bool TryResolveCity(string runtimeId, out CityRuntime city)
    {
        city = null;
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        int matches = 0;
        foreach (CityRuntime candidate in world.Cities)
        {
            if (candidate == null || !string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                continue;
            city = candidate;
            matches++;
        }
        return matches == 1;
    }

    private bool MatchesFrozenMaterialization(FrozenPersonTarget target, PersonRuntime person)
    {
        if (!string.Equals(person.PersonId?.Value, target.PersonId, StringComparison.Ordinal)
            || !string.Equals(person.MaterializedNpcRuntimeId ?? string.Empty,
                target.MaterializedNpcRuntimeId, StringComparison.Ordinal))
        {
            return false;
        }

        int boundNpcCount = 0;
        int personNpcCount = 0;
        foreach (NpcRuntime npc in world.NpcRuntimes)
        {
            if (npc == null) continue;
            if (target.MaterializedNpcRuntimeId.Length > 0
                && string.Equals(npc.RuntimeId, target.MaterializedNpcRuntimeId, StringComparison.Ordinal))
            {
                boundNpcCount++;
            }
            if (npc.PersonId != null && string.Equals(npc.PersonId.Value, target.PersonId, StringComparison.Ordinal))
            {
                personNpcCount++;
            }
        }

        return boundNpcCount == target.BoundNpcRuntimeCardinality
            && personNpcCount == target.PersonNpcCardinality;
    }

    private bool TryValidateStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out string identity,
        out string fingerprint,
        out FrozenDescriptor descriptor)
    {
        identity = null;
        fingerprint = null;
        descriptor = null;
        if (manifest == null || step == null
            || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.Ordinal < 0
            || step.OwnerId != DailyDemographyBoundaryStepProvider.OwnerId
            || step.StepId != DailyDemographyBoundaryStepProvider.StepId
            || step.OperationKind != DailyDemographyBoundaryStepProvider.OperationKind
            || step.OperationVersion != DailyDemographyBoundaryStepProvider.OperationVersion
            || !long.TryParse(step.OwnerRevision, NumberStyles.None, CultureInfo.InvariantCulture, out _)
            || !TryDecodeDescriptor(step.Payload, out descriptor)
            || descriptor.AbsoluteDay != manifest.AbsoluteDay)
        {
            return false;
        }

        identity = manifest.GetExecutionStepIdentity(step);
        fingerprint = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId,
            manifest.ContinuationId,
            manifest.WorldId,
            manifest.ProfileId,
            manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            manifest.ConfigurationIdentity,
            manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture),
            step.StepId,
            step.OwnerId,
            step.OperationKind,
            step.OperationVersion,
            step.OwnerRevision,
            step.Payload,
            step.PersonId,
            step.Disposition);
        descriptor.Fingerprint = fingerprint;
        return true;
    }

    private bool TryValidateCurrentTargets(FrozenDescriptor descriptor, out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (world.CurrentDay != descriptor.AbsoluteDay)
        {
            return false;
        }

        foreach (FrozenPersonTarget target in descriptor.Persons)
        {
            if (!world.PersonStore.TryGet(new PersonId(target.PersonId), out PersonRuntime person)
                || !MatchesFrozenMaterialization(target, person))
            {
                return false;
            }
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (FrozenCityTarget target in descriptor.Cities)
        {
            if (!ids.Add(target.RuntimeId) || !TryResolveCity(target.RuntimeId, out _))
            {
                return false;
            }
        }

        failure = TimelineFailure.None;
        return true;
    }

    private bool TryCaptureDescriptor(long absoluteDay, out FrozenDescriptor descriptor)
    {
        descriptor = null;
        if (world.Configuration == null || world.Configuration.NaturalMortality == null
            || world.Configuration.AggregateDemography == null || world.CurrentDay != absoluteDay)
        {
            return false;
        }

        List<PersonRuntime> persons = new List<PersonRuntime>(world.PersonStore.Persons);
        persons.RemoveAll(person => person == null || person.PersonId == null);
        persons.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.PersonId.Value, right.PersonId.Value));
        List<FrozenPersonTarget> frozenPersons = new List<FrozenPersonTarget>(persons.Count);
        foreach (PersonRuntime person in persons)
        {
            int boundNpcCount = 0;
            int personNpcCount = 0;
            foreach (NpcRuntime npc in world.NpcRuntimes)
            {
                if (npc == null) continue;
                if (!string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId)
                    && string.Equals(npc.RuntimeId, person.MaterializedNpcRuntimeId, StringComparison.Ordinal))
                {
                    boundNpcCount++;
                }
                if (npc.PersonId != null && npc.PersonId == person.PersonId)
                {
                    personNpcCount++;
                }
            }

            frozenPersons.Add(new FrozenPersonTarget(
                person.PersonId.Value,
                person.MaterializedNpcRuntimeId ?? string.Empty,
                boundNpcCount,
                personNpcCount));
        }

        List<CityRuntime> cities = new List<CityRuntime>();
        foreach (CityRuntime city in world.Cities)
        {
            if (city != null && !string.IsNullOrWhiteSpace(city.RuntimeId)) cities.Add(city);
        }
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        List<FrozenCityTarget> frozenCities = new List<FrozenCityTarget>(cities.Count);
        string previousId = null;
        foreach (CityRuntime city in cities)
        {
            if (string.Equals(previousId, city.RuntimeId, StringComparison.Ordinal)) return false;
            previousId = city.RuntimeId;
            frozenCities.Add(new FrozenCityTarget(
                city.RuntimeId));
        }

        descriptor = new FrozenDescriptor(
            absoluteDay,
            world.Configuration.NaturalMortality.Enabled,
            world.Configuration.NaturalMortality.AnnualProbability,
            world.Configuration.AggregateDemography.Enabled,
            world.Configuration.AggregateDemography.AnnualBirthRate,
            world.Configuration.AggregateDemography.AnnualDeathRate,
            frozenPersons.AsReadOnly(),
            frozenCities.AsReadOnly());
        return true;
    }

    private static string EncodeDescriptor(FrozenDescriptor descriptor)
    {
        List<string> parts = new List<string>
        {
            PayloadVersion,
            descriptor.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            descriptor.NaturalMortalityEnabled ? "1" : "0",
            descriptor.NaturalMortalityAnnualProbability.ToString("R", CultureInfo.InvariantCulture),
            descriptor.AggregateDemographyEnabled ? "1" : "0",
            descriptor.AggregateAnnualBirthRate.ToString("R", CultureInfo.InvariantCulture),
            descriptor.AggregateAnnualDeathRate.ToString("R", CultureInfo.InvariantCulture),
            descriptor.Persons.Count.ToString(CultureInfo.InvariantCulture)
        };
        foreach (FrozenPersonTarget person in descriptor.Persons)
        {
            parts.Add(person.PersonId);
            parts.Add(person.MaterializedNpcRuntimeId);
            parts.Add(person.BoundNpcRuntimeCardinality.ToString(CultureInfo.InvariantCulture));
            parts.Add(person.PersonNpcCardinality.ToString(CultureInfo.InvariantCulture));
        }

        parts.Add(descriptor.Cities.Count.ToString(CultureInfo.InvariantCulture));
        foreach (FrozenCityTarget city in descriptor.Cities)
        {
            parts.Add(city.RuntimeId);
        }

        return SpatialStableKey.Encode(parts.ToArray());
    }

    private static bool TryDecodeDescriptor(string payload, out FrozenDescriptor descriptor)
    {
        descriptor = null;
        if (!TryDecodeFields(payload, out List<string> parts) || parts.Count < 9
            || parts[0] != PayloadVersion
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long day)
            || day <= 0L
            || !TryParseBoolean(parts[2], out bool mortalityEnabled)
            || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double mortalityProbability)
            || !IsValidProbability(mortalityProbability)
            || !TryParseBoolean(parts[4], out bool aggregateEnabled)
            || !double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double births)
            || !IsValidRate(births)
            || !double.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out double deaths)
            || !IsValidRate(deaths)
            || !int.TryParse(parts[7], NumberStyles.None, CultureInfo.InvariantCulture, out int personCount)
            || personCount < 0)
        {
            return false;
        }

        int index = 8;
        if (personCount > (parts.Count - index) / 4) return false;
        List<FrozenPersonTarget> persons = new List<FrozenPersonTarget>(personCount);
        string previousPersonId = null;
        for (int i = 0; i < personCount; i++)
        {
            string personId = parts[index++];
            if (string.IsNullOrWhiteSpace(personId)
                || (previousPersonId != null && StringComparer.Ordinal.Compare(previousPersonId, personId) >= 0))
            {
                return false;
            }
            string materializedNpcId = parts[index++];
            if (!int.TryParse(parts[index++], NumberStyles.None, CultureInfo.InvariantCulture, out int boundNpcCount)
                || boundNpcCount < 0
                || !int.TryParse(parts[index++], NumberStyles.None, CultureInfo.InvariantCulture, out int personNpcCount)
                || personNpcCount < 0)
            {
                return false;
            }

            persons.Add(new FrozenPersonTarget(
                personId, materializedNpcId, boundNpcCount, personNpcCount));
            previousPersonId = personId;
        }

        if (index >= parts.Count
            || !int.TryParse(parts[index++], NumberStyles.None, CultureInfo.InvariantCulture, out int cityCount)
            || cityCount < 0
            || cityCount > parts.Count - index
            || parts.Count != index + cityCount)
        {
            return false;
        }

        List<FrozenCityTarget> cities = new List<FrozenCityTarget>(cityCount);
        string previousCityId = null;
        for (int i = 0; i < cityCount; i++)
        {
            string runtimeId = parts[index++];
            if (string.IsNullOrWhiteSpace(runtimeId)
                || (previousCityId != null && StringComparer.Ordinal.Compare(previousCityId, runtimeId) >= 0))
            {
                return false;
            }
            cities.Add(new FrozenCityTarget(runtimeId));
            previousCityId = runtimeId;
        }

        descriptor = new FrozenDescriptor(day, mortalityEnabled, mortalityProbability,
            aggregateEnabled, births, deaths, persons.AsReadOnly(), cities.AsReadOnly());
        return true;
    }

    private static bool TryDecodeFields(string payload, out List<string> fields)
    {
        fields = new List<string>();
        if (payload == null) return false;
        int offset = 0;
        while (offset < payload.Length)
        {
            int separator = payload.IndexOf(':', offset);
            if (separator <= offset
                || !int.TryParse(payload.Substring(offset, separator - offset),
                    NumberStyles.None, CultureInfo.InvariantCulture, out int length)
                || length < 0)
            {
                return false;
            }
            int valueStart = separator + 1;
            if (length > payload.Length - valueStart) return false;
            fields.Add(payload.Substring(valueStart, length));
            offset = valueStart + length;
        }
        return fields.Count > 0;
    }

    private static bool TryParseBoolean(string value, out bool parsed)
    {
        parsed = value == "1";
        return parsed || value == "0";
    }

    private static bool IsValidProbability(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d && value <= 1d;

    private static bool IsValidRate(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;

    private sealed class OccurrenceState
    {
        public readonly string Identity;
        public readonly string SemanticOccurrenceIdentity;
        public readonly string DescriptorFingerprint;
        public readonly FrozenDescriptor Descriptor;
        public readonly DailyDemographyReport Report;
        public int NextPersonIndex;
        public int NextCityIndex;
        public bool AggregateStarted;
        public RepresentedResidentFloorSnapshot Floors;
        public PersonDeathTransition PendingDeath;
        public string PendingDeathPersonId;
        public AggregateDemographyTransition PendingAggregate;
        public bool CommitPrepared;

        public OccurrenceState(string identity, string semanticOccurrenceIdentity,
            string fingerprint, FrozenDescriptor descriptor)
        {
            Identity = identity;
            SemanticOccurrenceIdentity = semanticOccurrenceIdentity;
            DescriptorFingerprint = fingerprint;
            Descriptor = descriptor;
            Report = new DailyDemographyReport(descriptor.AbsoluteDay);
        }
    }

    private sealed class FrozenDescriptor
    {
        public readonly long AbsoluteDay;
        public readonly bool NaturalMortalityEnabled;
        public readonly double NaturalMortalityAnnualProbability;
        public readonly bool AggregateDemographyEnabled;
        public readonly double AggregateAnnualBirthRate;
        public readonly double AggregateAnnualDeathRate;
        public readonly IReadOnlyList<FrozenPersonTarget> Persons;
        public readonly IReadOnlyList<FrozenCityTarget> Cities;
        public string Fingerprint;

        public FrozenDescriptor(long absoluteDay, bool naturalMortalityEnabled,
            double naturalMortalityAnnualProbability, bool aggregateDemographyEnabled,
            double aggregateAnnualBirthRate, double aggregateAnnualDeathRate,
            IReadOnlyList<FrozenPersonTarget> persons, IReadOnlyList<FrozenCityTarget> cities)
        {
            AbsoluteDay = absoluteDay;
            NaturalMortalityEnabled = naturalMortalityEnabled;
            NaturalMortalityAnnualProbability = naturalMortalityAnnualProbability;
            AggregateDemographyEnabled = aggregateDemographyEnabled;
            AggregateAnnualBirthRate = aggregateAnnualBirthRate;
            AggregateAnnualDeathRate = aggregateAnnualDeathRate;
            Persons = persons;
            Cities = cities;
        }
    }

    private sealed class FrozenPersonTarget
    {
        public readonly string PersonId;
        public readonly string MaterializedNpcRuntimeId;
        public readonly int BoundNpcRuntimeCardinality;
        public readonly int PersonNpcCardinality;

        public FrozenPersonTarget(string personId, string materializedNpcRuntimeId,
            int boundNpcRuntimeCardinality, int personNpcCardinality)
        {
            PersonId = personId;
            MaterializedNpcRuntimeId = materializedNpcRuntimeId;
            BoundNpcRuntimeCardinality = boundNpcRuntimeCardinality;
            PersonNpcCardinality = personNpcCardinality;
        }
    }

    private sealed class FrozenCityTarget
    {
        public readonly string RuntimeId;

        public FrozenCityTarget(string runtimeId)
        {
            RuntimeId = runtimeId;
        }
    }

    private sealed class DailyDemographyBoundaryStepCommit : IBoundaryContinuationStepCommit
    {
        private readonly DailyDemographyBoundaryOwner owner;
        private readonly string identity;
        private readonly string fingerprint;
        private readonly OccurrenceState state;
        private readonly DailyDemographyBoundaryReceipt replayReceipt;

        public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
        public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();

        public DailyDemographyBoundaryStepCommit(DailyDemographyBoundaryOwner owner,
            string identity, string fingerprint, OccurrenceState state,
            DailyDemographyBoundaryReceipt replayReceipt)
        {
            this.owner = owner;
            this.identity = identity;
            this.fingerprint = fingerprint;
            this.state = state;
            this.replayReceipt = replayReceipt;
        }

        public bool TryCommit(out TimelineFailure failure) =>
            owner.TryCommit(identity, fingerprint, state, replayReceipt, out failure);
    }
}

/// <summary>Immutable completion receipt retained by the demographic owner.</summary>
public sealed class DailyDemographyBoundaryReceipt
{
    public string ExecutionStepIdentity { get; }
    public string DescriptorFingerprint { get; }
    public long OwnerRevisionBefore { get; }
    public long OwnerRevisionAfter { get; }
    public DailyDemographyReport Report { get; }
    public IReadOnlyList<string> RetainedSourceSignals { get; } = Array.Empty<string>();

    internal DailyDemographyBoundaryReceipt(string executionStepIdentity,
        string descriptorFingerprint, long ownerRevisionBefore, long ownerRevisionAfter,
        DailyDemographyReport report)
    {
        ExecutionStepIdentity = executionStepIdentity;
        DescriptorFingerprint = descriptorFingerprint;
        OwnerRevisionBefore = ownerRevisionBefore;
        OwnerRevisionAfter = ownerRevisionAfter;
        Report = report ?? throw new ArgumentNullException(nameof(report));
    }
}
