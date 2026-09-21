using UnityEngine;

public class GuardSystem : INpcActionProvider
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

        NpcRuntime target = FindArrestTarget(npcRuntime, out float bounty);

        if (target == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 55f + bounty * 0.25f);
        return new NpcActionRuntime(action, target);
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
                || (Mathf.Approximately(bounty, bestBounty)
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
            && npcRuntime.NpcData != null
            && npcRuntime.NpcData.job != null
            && npcRuntime.NpcData.job.jobType == NpcJobType.Guard;
    }

    private bool IsHidden(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && (npcRuntime.IsHidden == true || (hiddenStatus != null && npcRuntime.CurrentStatus.Contains(hiddenStatus) == true));
    }
}
