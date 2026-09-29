using System;
using System.Collections.Generic;
using System.Globalization;

public partial class MerchantSystem
{
    private const string P18DTradeStateOperationKind = "merchant.trade-state";
    private const string P18DTradeStateOperationVersion = "1";
    private const string P18DTradeStateStepOwnerId = "merchant";
    private readonly Dictionary<string, P18DMerchantTradeStateWorkflow> p18dTradeStateWorkflows =
        new Dictionary<string, P18DMerchantTradeStateWorkflow>(StringComparer.Ordinal);

    /// <summary>
    /// Advances one frozen merchant trade-state occurrence with owner-local receipts.
    /// Composition/provider registration is intentionally owned by the later P18-D
    /// integration; this method consumes the exact step that integration freezes.
    /// </summary>
    public bool TryAdvanceNpcTradeStateOccurrence(NpcRuntime actor,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out NpcMerchantTradeStateReceipt receipt, out TimelineFailure failure)
    {
        receipt = null;
        failure = TimelineFailure.ContinuationFailed;
        ThrowIfFaulted();
        if (!TryGetP18DTradeStateIdentity(actor, manifest, step,
                out string operationIdentity, out string descriptorFingerprint)) return false;

        if (p18dTradeStateWorkflows.TryGetValue(operationIdentity,
                out P18DMerchantTradeStateWorkflow retained))
        {
            if (retained.DescriptorFingerprint != descriptorFingerprint) return false;
            return TryResumeP18DTradeStateWorkflow(retained, out receipt, out failure);
        }

        MerchantTradePlanState expectedPlan = actor.MerchantTradePlan.CaptureOwnerState();
        NpcTravelPlanState expectedTravelPlan = actor.TravelPlan.CaptureOwnerState();
        MerchantTradePlanRuntime nextPlan = new MerchantTradePlanRuntime();
        nextPlan.InstallOwnerState(expectedPlan);
        NpcTravelPlanRuntime nextTravelPlan = new NpcTravelPlanRuntime();
        nextTravelPlan.InstallOwnerState(expectedTravelPlan);
        string diagnostic = null;
        MerchantTradeOpportunity redirectOpportunity = null;
        bool eligible = IsMerchant(actor) && actor.IsAlive && actor.CurrentCity != null
            && !actor.IsTraveling;

        if (eligible)
        {
            if (!actor.LocalKnowledgeObservationRuntime.TryPreparePostShareMarketObservation(
                    actor, manifest, step,
                    out IBoundaryContinuationStepCommit marketObservation,
                    out failure)
                || !marketObservation.TryCommit(out failure)) return false;

            if (IsLocalMerchant(actor) && nextPlan.HasData)
            {
                ClearP18DTradePlan(nextPlan, nextTravelPlan);
            }
            else if (nextPlan.HasData && !nextPlan.IsActive)
            {
                ClearP18DTradePlan(nextPlan, nextTravelPlan);
            }
            else if (nextPlan.IsActive && !actor.Inventory.HasItem(nextPlan.Item, 1))
            {
                ClearP18DTradePlan(nextPlan, nextTravelPlan);
            }

            if (nextPlan.IsActive)
            {
                if (nextPlan.TargetCity != actor.CurrentCity)
                {
                    if (CanStartTradeTravel(actor, nextPlan.TargetCity))
                    {
                        SetP18DTradeTravelPlan(actor, nextPlan, nextTravelPlan,
                            nextPlan.TargetCity);
                        return CreateAndResumeP18DWorkflow(actor, manifest, step,
                            operationIdentity, descriptorFingerprint, expectedPlan,
                            expectedTravelPlan, nextPlan, nextTravelPlan, null, null,
                            out receipt, out failure);
                    }

                    ClearP18DTradeTravelPlan(nextTravelPlan);
                    nextPlan.RedirectTo(actor.CurrentCity);
                    diagnostic = $"{actor.NpcName} nao consegue viajar para cumprir o plano comercial e vai reavaliar venda local.";
                }

                int amount = GetPlannedTradeAmount(actor, nextPlan);
                if (amount <= 0)
                {
                    ClearP18DTradePlan(nextPlan, nextTravelPlan);
                }
                else if (!TryGetUsefulObservation(actor, actor.CurrentCity,
                             nextPlan.Item, out CommercialMarketObservation localObservation, out _))
                {
                    nextPlan.IncrementWaitDayAtDestination();
                }
                else
                {
                    float localPrice = localObservation.ObservedPrice;
                    float profitPerItem = localPrice - nextPlan.PurchasePricePerItem;
                    MerchantTradeOpportunity localBuyer = FindBestLocalMerchantBuyer(
                        actor, nextPlan.Item, amount, nextPlan.PurchasePricePerItem);
                    int marketAmount = LimitMarketSaleAmountFromKnowledge(
                        actor, actor.CurrentCity, localPrice, amount);
                    float minimumProfitPerItem = GetMinimumProfitPerItem(actor);

                    if (localBuyer != null
                        || (marketAmount > 0 && profitPerItem >= minimumProfitPerItem))
                    {
                        nextPlan.ResetWaitDaysAtDestination();
                    }
                    else
                    {
                        redirectOpportunity = FindBestTradeDestinationForPlan(actor, nextPlan);
                        if (redirectOpportunity == null)
                        {
                            nextPlan.IncrementWaitDayAtDestination();
                        }
                    }
                }
            }
        }

        return CreateAndResumeP18DWorkflow(actor, manifest, step,
            operationIdentity, descriptorFingerprint, expectedPlan,
            expectedTravelPlan, nextPlan, nextTravelPlan,
            redirectOpportunity, diagnostic, out receipt, out failure);
    }

    private bool CreateAndResumeP18DWorkflow(NpcRuntime actor,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        string operationIdentity, string descriptorFingerprint,
        MerchantTradePlanState expectedPlan, NpcTravelPlanState expectedTravelPlan,
        MerchantTradePlanRuntime nextPlan, NpcTravelPlanRuntime nextTravelPlan,
        MerchantTradeOpportunity redirectOpportunity, string diagnostic,
        out NpcMerchantTradeStateReceipt receipt, out TimelineFailure failure)
    {
        string workflowFingerprint = SpatialStableKey.Encode(descriptorFingerprint,
            CreatePlanStateFingerprint(expectedPlan),
            CreateTravelPlanStateFingerprint(expectedTravelPlan),
            simulationTime.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            actor.CommercialKnowledge.Revision.ToString(CultureInfo.InvariantCulture),
            actor.SpatialKnowledge.Revision.ToString(CultureInfo.InvariantCulture),
            CreateOpportunityFingerprint(redirectOpportunity), diagnostic ?? string.Empty);
        P18DMerchantTradeStateWorkflow workflow = new P18DMerchantTradeStateWorkflow(
            operationIdentity, descriptorFingerprint, workflowFingerprint, actor,
            manifest, step, expectedPlan, expectedTravelPlan,
            nextPlan.CaptureOwnerState(), nextTravelPlan.CaptureOwnerState(),
            redirectOpportunity, diagnostic);
        p18dTradeStateWorkflows.Add(operationIdentity, workflow);
        return TryResumeP18DTradeStateWorkflow(workflow, out receipt, out failure);
    }

    private bool TryResumeP18DTradeStateWorkflow(P18DMerchantTradeStateWorkflow workflow,
        out NpcMerchantTradeStateReceipt receipt, out TimelineFailure failure)
    {
        receipt = null;
        failure = TimelineFailure.ContinuationFailed;
        if (workflow == null || workflow.Actor == null) return false;

        MerchantTradePlanState nextPlan = workflow.NextPlan;
        NpcTravelPlanState nextTravelPlan = workflow.NextTravelPlan;
        if (workflow.RedirectOpportunity != null)
        {
            MerchantTradeOpportunity opportunity = workflow.RedirectOpportunity;
            string decisionOperationIdentity = SpatialStableKey.Encode(
                workflow.OperationIdentity, "trade-redirect-decision");
            string decisionFingerprint = SpatialStableKey.Encode(
                workflow.WorkflowFingerprint, CreateOpportunityFingerprint(opportunity),
                workflow.Actor.RuntimeId, opportunity.TargetCity?.Location?.RuntimeId ?? string.Empty);
            if (decisionRecorder != null)
            {
                if (!decisionRecorder.TryRecordOccurrenceOnce(decisionOperationIdentity,
                        decisionFingerprint, workflow.Actor.RuntimeId,
                        NpcDecisionType.TradeRedirect, NpcDecisionOrigin.Autonomous,
                        null, opportunity.TargetCity?.Location?.RuntimeId,
                        opportunity.Evidence, out NpcDecisionRecord decision)) return false;
                if (decision != null)
                {
                    nextPlan = new MerchantTradePlanState(nextPlan.Item, nextPlan.OriginCity,
                        opportunity.TargetCity, nextPlan.PlannedAmount,
                        nextPlan.RawRemainingAmount, nextPlan.PurchasePricePerItem,
                        nextPlan.WaitDaysAtDestination, nextPlan.PendingTravelDays,
                        decision.DecisionId);
                }
            }

            if (IsTravelingMerchant(workflow.Actor)
                && opportunity.TargetCity != null
                && workflow.Actor.CurrentCity != null
                && opportunity.TargetCity != workflow.Actor.CurrentCity
                && travelSystem != null)
            {
                float travelCost = travelSystem.GetTravelCost(
                    workflow.Actor.CurrentCity, opportunity.TargetCity);
                if (travelCost >= 0f)
                {
                    nextTravelPlan = new NpcTravelPlanState(
                        opportunity.TargetCity.Location, opportunity.TargetCity,
                        NpcTravelReason.Trade, 70f, travelCost, nextPlan.OriginDecisionId);
                }
            }
        }

        if (!workflow.Actor.MerchantTradeStateRuntime.TryPrepareInstall(
                workflow.Actor, workflow.OperationIdentity, workflow.WorkflowFingerprint,
                workflow.ExpectedPlan, workflow.ExpectedTravelPlan, nextPlan,
                nextTravelPlan,
                out IBoundaryContinuationStepCommit actorInstall)
            || !actorInstall.TryCommit(out failure)) return false;

        string logMessage = workflow.Diagnostic;
        if (workflow.RedirectOpportunity != null)
        {
            MerchantTradeOpportunity opportunity = workflow.RedirectOpportunity;
            logMessage = $"{workflow.Actor.NpcName} reavaliou o plano comercial e mudou o destino para {opportunity.TargetCity.CityName}.";
        }
        if (!string.IsNullOrEmpty(logMessage))
        {
            string logOperationIdentity = SpatialStableKey.Encode(
                workflow.OperationIdentity, "trade-state-diagnostic");
            if (!logger.TryPrepareKeyedOccurrence(logOperationIdentity,
                    SpatialStableKey.Encode(workflow.WorkflowFingerprint, logMessage),
                    SimulationLogCategory.Trade, logMessage,
                    out LoggerKeyedOccurrenceCommit logCommit)
                || !logCommit.TryCommit(out failure)) return false;
        }

        if (!workflow.Actor.MerchantTradeStateRuntime.TryResolve(
                workflow.OperationIdentity, workflow.WorkflowFingerprint, out receipt)) return false;
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryGetP18DTradeStateIdentity(NpcRuntime actor,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out string operationIdentity, out string descriptorFingerprint)
    {
        operationIdentity = null;
        descriptorFingerprint = null;
        if (actor == null || manifest == null || step == null
            || step.Ordinal < 0 || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.StepId != "merchant-trade-state:" + actor.RuntimeId
            || step.OwnerId != actor.RuntimeId
            || step.OperationKind != P18DTradeStateOperationKind
            || step.OperationVersion != P18DTradeStateOperationVersion
            || step.PersonId != (actor.PersonId != null ? actor.PersonId.Value : string.Empty)) return false;

        operationIdentity = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId, step.StepId,
            P18DTradeStateOperationKind, P18DTradeStateOperationVersion);
        descriptorFingerprint = SpatialStableKey.Encode(
            manifest.WorldId, manifest.ProfileId, manifest.BoundaryOccurrenceId,
            manifest.ContinuationId, manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            manifest.ConfigurationIdentity, manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture), step.StepId,
            step.OwnerId, step.PersonId, step.OperationKind, step.OperationVersion,
            step.OwnerRevision, step.Payload, step.Disposition);
        return true;
    }

    private static void ClearP18DTradePlan(MerchantTradePlanRuntime plan,
        NpcTravelPlanRuntime travelPlan)
    {
        plan.Clear();
        if (travelPlan.Reason == NpcTravelReason.Trade) travelPlan.Clear();
    }

    private static void ClearP18DTradeTravelPlan(NpcTravelPlanRuntime travelPlan)
    {
        if (travelPlan.Reason == NpcTravelReason.Trade) travelPlan.Clear();
    }

    private void SetP18DTradeTravelPlan(NpcRuntime actor, MerchantTradePlanRuntime plan,
        NpcTravelPlanRuntime travelPlan, CityRuntime targetCity)
    {
        if (actor == null || !IsTravelingMerchant(actor) || targetCity == null
            || actor.CurrentCity == null || targetCity == actor.CurrentCity || travelSystem == null) return;
        float travelCost = travelSystem.GetTravelCost(actor.CurrentCity, targetCity);
        if (travelCost < 0f) return;
        travelPlan.Set(targetCity, NpcTravelReason.Trade, 70f, travelCost, plan.OriginDecisionId);
    }

    private static string CreatePlanStateFingerprint(MerchantTradePlanState state) =>
        SpatialStableKey.Encode(state.Item?.DefinitionId ?? string.Empty,
            state.OriginCity?.RuntimeId ?? string.Empty, state.TargetCity?.RuntimeId ?? string.Empty,
            state.PlannedAmount.ToString(CultureInfo.InvariantCulture),
            state.RawRemainingAmount.ToString(CultureInfo.InvariantCulture),
            state.PurchasePricePerItem.ToString("R", CultureInfo.InvariantCulture),
            state.WaitDaysAtDestination.ToString(CultureInfo.InvariantCulture),
            state.PendingTravelDays.ToString(CultureInfo.InvariantCulture),
            state.OriginDecisionId ?? string.Empty);

    private static string CreateTravelPlanStateFingerprint(NpcTravelPlanState state) =>
        SpatialStableKey.Encode(state.TargetLocation?.RuntimeId ?? string.Empty,
            state.TargetCity?.RuntimeId ?? string.Empty, state.Reason.ToString(),
            state.Utility.ToString("R", CultureInfo.InvariantCulture),
            state.ExpectedCost.ToString("R", CultureInfo.InvariantCulture),
            state.OriginDecisionId ?? string.Empty);

    private static string CreateOpportunityFingerprint(MerchantTradeOpportunity opportunity)
    {
        if (opportunity == null) return "no-redirect";
        CommercialDecisionEvidence evidence = opportunity.Evidence;
        return SpatialStableKey.Encode(opportunity.Item?.DefinitionId ?? string.Empty,
            opportunity.TargetCity?.RuntimeId ?? string.Empty,
            opportunity.Amount.ToString(CultureInfo.InvariantCulture),
            opportunity.BuyPrice.ToString("R", CultureInfo.InvariantCulture),
            opportunity.SellPrice.ToString("R", CultureInfo.InvariantCulture),
            opportunity.ProfitPerItem.ToString("R", CultureInfo.InvariantCulture),
            opportunity.NetProfit.ToString("R", CultureInfo.InvariantCulture),
            opportunity.Score.ToString("R", CultureInfo.InvariantCulture),
            evidence?.ItemDefinitionId ?? string.Empty,
            evidence?.CurrentLocationRuntimeId ?? string.Empty,
            evidence?.TradeOriginLocationRuntimeId ?? string.Empty,
            evidence?.TradeDestinationLocationRuntimeId ?? string.Empty,
            evidence?.PreviousDestinationLocationRuntimeId ?? string.Empty,
            evidence?.ExpectedQuantity.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            evidence?.ExpectedPurchaseUnitPrice.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
            evidence?.ExpectedSaleUnitPrice.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
            evidence?.ExpectedTravelCost.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
            evidence?.ExpectedGrossProfit.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
            evidence?.ExpectedNetProfit.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
            evidence?.ExpectedScore.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty);
    }
}

internal sealed class P18DMerchantTradeStateWorkflow
{
    internal string OperationIdentity { get; }
    internal string DescriptorFingerprint { get; }
    internal string WorkflowFingerprint { get; }
    internal NpcRuntime Actor { get; }
    internal BoundaryContinuationManifest Manifest { get; }
    internal BoundaryContinuationStep Step { get; }
    internal MerchantTradePlanState ExpectedPlan { get; }
    internal NpcTravelPlanState ExpectedTravelPlan { get; }
    internal MerchantTradePlanState NextPlan { get; }
    internal NpcTravelPlanState NextTravelPlan { get; }
    internal MerchantSystem.MerchantTradeOpportunity RedirectOpportunity { get; }
    internal string Diagnostic { get; }

    internal P18DMerchantTradeStateWorkflow(string operationIdentity,
        string descriptorFingerprint, string workflowFingerprint, NpcRuntime actor,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        MerchantTradePlanState expectedPlan, NpcTravelPlanState expectedTravelPlan,
        MerchantTradePlanState nextPlan, NpcTravelPlanState nextTravelPlan,
        MerchantSystem.MerchantTradeOpportunity redirectOpportunity, string diagnostic)
    {
        OperationIdentity = operationIdentity;
        DescriptorFingerprint = descriptorFingerprint;
        WorkflowFingerprint = workflowFingerprint;
        Actor = actor;
        Manifest = manifest;
        Step = step;
        ExpectedPlan = expectedPlan;
        ExpectedTravelPlan = expectedTravelPlan;
        NextPlan = nextPlan;
        NextTravelPlan = nextTravelPlan;
        RedirectOpportunity = redirectOpportunity;
        Diagnostic = diagnostic;
    }
}
