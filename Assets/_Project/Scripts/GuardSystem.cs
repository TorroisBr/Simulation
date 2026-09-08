using UnityEngine;

public class GuardSystem : INpcActionProvider
{
    private readonly NpcStatusData wantedStatus;
    private readonly NpcStatusData arrestedStatus;

    public GuardSystem(NpcStatusData wantedStatus, NpcStatusData arrestedStatus)
    {
        this.wantedStatus = wantedStatus;
        this.arrestedStatus = arrestedStatus;
    }

    public bool HandlesAction(NpcActionData action)
    {
        return action != null && action.actionType == NpcActionType.Arrest;
    }

    public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (IsGuard(npcRuntime) == false || npcRuntime.CurrentCity == null)
        {
            utility = 0f;
            return null;
        }

        NpcRuntime target = FindArrestTarget(npcRuntime);

        if (target == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, 75f);
        return new NpcActionRuntime(action, target);
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (IsGuard(npcRuntime) == false || actionRuntime == null || IsValidArrestTarget(npcRuntime, actionRuntime.TargetNpc) == false)
        {
            return NpcActionResult.Failed();
        }

        Debug.Log($"{npcRuntime.NpcName} prendeu {actionRuntime.TargetNpc.NpcName} com sucesso.");
        return NpcActionResult.Succeeded();
    }

    private NpcRuntime FindArrestTarget(NpcRuntime guardRuntime)
    {
        if (guardRuntime == null || guardRuntime.CurrentCity == null)
        {
            return null;
        }

        foreach (NpcRuntime candidate in guardRuntime.CurrentCity.ImportantNpcs)
        {
            if (IsValidArrestTarget(guardRuntime, candidate) == true)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsValidArrestTarget(NpcRuntime guardRuntime, NpcRuntime targetRuntime)
    {
        if (guardRuntime == null || targetRuntime == null || targetRuntime == guardRuntime || wantedStatus == null)
        {
            return false;
        }

        if (targetRuntime.IsTraveling == true || targetRuntime.CurrentCity != guardRuntime.CurrentCity)
        {
            return false;
        }

        if (targetRuntime.CurrentStatus.Contains(wantedStatus) == false)
        {
            return false;
        }

        return arrestedStatus == null || targetRuntime.CurrentStatus.Contains(arrestedStatus) == false;
    }

    private bool IsGuard(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && npcRuntime.NpcData != null
            && npcRuntime.NpcData.job != null
            && npcRuntime.NpcData.job.jobType == NpcJobType.Guard;
    }
}
