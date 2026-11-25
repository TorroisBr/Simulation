using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GuardSystem : INpcActionProvider, ISelectedDefinitionNpcActionProvider
{
    private readonly JusticeSystem justiceSystem;
    private readonly NpcStatusData hiddenStatus;
    private readonly EffectiveGuardCrimeConfiguration configuration;

    public GuardSystem(JusticeSystem justiceSystem, NpcStatusData hiddenStatus)
        : this(justiceSystem, hiddenStatus, new EffectiveGuardCrimeConfiguration(true))
    {
    }

    public GuardSystem(
        JusticeSystem justiceSystem,
        NpcStatusData hiddenStatus,
        EffectiveGuardCrimeConfiguration configuration)
    {
        this.justiceSystem = justiceSystem;
        this.hiddenStatus = hiddenStatus;
        this.configuration = configuration ?? new EffectiveGuardCrimeConfiguration(true);
    }

    public bool HandlesAction(NpcActionData action)
    {
        return action != null && action.actionType == NpcActionType.Arrest;
    }

    bool ISelectedDefinitionNpcActionProvider.HandlesAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        return npcRuntime != null && action != null
            && npcRuntime.GetConfiguredActionType(action) == NpcActionType.Arrest;
    }

    public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (configuration.Enabled == false
            || IsGuard(npcRuntime) == false
            || npcRuntime.CurrentCity == null
            || IsHidden(npcRuntime) == true
            || justiceSystem == null)
        {
            utility = 0f;
            return null;
        }

        NpcRuntime target;
        if (npcRuntime.UsesSelectedDailyV1Definitions)
        {
            if (!TryCreateSelectedArrestTarget(npcRuntime, action, utility, out target, out float selectedUtility))
            {
                utility = 0f;
                return null;
            }

            utility = selectedUtility;
        }
        else
        {
            target = FindArrestTarget(npcRuntime, out float bounty);

            if (target == null)
            {
                utility = 0f;
                return null;
            }

            utility = UnityMathfCompatibility.Max(utility, 55f + bounty * 0.25f);
        }

        return new NpcActionRuntime(action, target);
    }

    private bool TryCreateSelectedArrestTarget(
        NpcRuntime guardRuntime,
        NpcActionData actionAsset,
        float currentUtility,
        out NpcRuntime targetRuntime,
        out float resultingUtility)
    {
        targetRuntime = null;
        resultingUtility = 0f;
        DailyV1SelectedRuntimeOwnerGraph ownerGraph = guardRuntime?.SelectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners guardOwner = guardRuntime?.SelectedRuntimeOwners;
        if (guardRuntime == null || ownerGraph == null || guardOwner == null
            || !guardRuntime.TryGetSelectedDailyV1ActionNode(actionAsset, out DailyV1ActionDefinition actionNode)
            || !DailyV1GuardActionPlanning.TryPlanArrest(
                ownerGraph, guardOwner, actionNode, currentUtility,
                out DailyV1NpcRuntimeOwners targetOwner, out resultingUtility)
            || targetOwner == null || guardRuntime.CurrentCity == null)
        {
            resultingUtility = 0f;
            return false;
        }

        DailyV1CityRuntimeOwners exactCity = null;
        foreach (DailyV1CityRuntimeOwners city in ownerGraph.Cities)
            if (city != null && string.Equals(city.RuntimeId,
                    guardRuntime.CurrentCity.RuntimeId, System.StringComparison.Ordinal))
            {
                if (exactCity != null)
                {
                    resultingUtility = 0f;
                    return false;
                }
                exactCity = city;
            }
        if (!ReferenceEquals(guardRuntime.CurrentCity.SelectedRuntimeOwners, exactCity))
        {
            resultingUtility = 0f;
            return false;
        }

        // The Core graph made the choice. Unity is used only to recover the
        // exact object projection required by the existing action API.
        foreach (NpcRuntime candidate in guardRuntime.CurrentCity.ImportantNpcs)
        {
            if (!ReferenceEquals(candidate?.SelectedRuntimeOwners, targetOwner)) continue;
            if (targetRuntime != null)
            {
                resultingUtility = 0f;
                return false;
            }
            targetRuntime = candidate;
        }

        if (targetRuntime == null)
            resultingUtility = 0f;
        return targetRuntime != null;
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (configuration.Enabled == false
            || IsGuard(npcRuntime) == false
            || actionRuntime == null
            || IsValidArrestTarget(npcRuntime, actionRuntime.TargetNpc) == false)
        {
            return NpcActionResult.Failed();
        }

        return justiceSystem.Arrest(npcRuntime, actionRuntime.TargetNpc, npcRuntime.CurrentCity, actionRuntime.OriginDecisionId) == true
            ? NpcActionResult.Succeeded()
            : NpcActionResult.Failed();
    }

    private NpcRuntime FindArrestTarget(NpcRuntime guardRuntime, out float bestBounty)
    {
        bestBounty = 0f;

        if (guardRuntime == null || guardRuntime.CurrentCity == null)
        {
            return null;
        }

        NpcRuntime bestTarget = null;

        foreach (NpcRuntime candidate in guardRuntime.CurrentCity.ImportantNpcs)
        {
            if (IsValidArrestTarget(guardRuntime, candidate) == false)
            {
                continue;
            }

            float bounty = justiceSystem.GetBounty(candidate, guardRuntime.CurrentCity);

            if (bestTarget == null
                || bounty > bestBounty
                || (UnityMathfCompatibility.Approximately(bounty, bestBounty)
                    && string.CompareOrdinal(candidate.RuntimeId, bestTarget.RuntimeId) < 0))
            {
                bestTarget = candidate;
                bestBounty = bounty;
            }
        }

        return bestTarget;
    }

    private bool IsValidArrestTarget(NpcRuntime guardRuntime, NpcRuntime targetRuntime)
    {
        if (guardRuntime == null || guardRuntime.IsAlive == false || targetRuntime == null || targetRuntime.IsAlive == false || targetRuntime == guardRuntime || justiceSystem == null)
        {
            return false;
        }

        if (targetRuntime.IsTraveling == true || targetRuntime.CurrentCity != guardRuntime.CurrentCity)
        {
            return false;
        }

        if (justiceSystem.IsArrested(targetRuntime) == true || IsHidden(targetRuntime) == true)
        {
            return false;
        }

        return justiceSystem.HasActiveWarrantInCity(targetRuntime, guardRuntime.CurrentCity);
    }

    private bool IsGuard(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && npcRuntime.IsAlive == true
            && npcRuntime.HasJobType(NpcJobType.Guard);
    }

    private bool IsHidden(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && (npcRuntime.IsHidden == true || (hiddenStatus != null && npcRuntime.CurrentStatus.Contains(hiddenStatus) == true));
    }
}
