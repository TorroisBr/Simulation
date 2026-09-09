using System;
using System.Collections.Generic;
using UnityEngine;

public class JusticeSystem
{
    private readonly List<WantedRecordRuntime> wantedRecords = new List<WantedRecordRuntime>();
    private readonly List<PrisonSentenceRuntime> prisonSentences = new List<PrisonSentenceRuntime>();
    private readonly NpcStatusData freeStatus;
    private readonly NpcStatusData wantedStatus;
    private readonly NpcStatusData arrestedStatus;
    private readonly NpcStatusData hiddenStatus;

    public JusticeSystem(NpcStatusData freeStatus, NpcStatusData wantedStatus, NpcStatusData arrestedStatus, NpcStatusData hiddenStatus)
    {
        this.freeStatus = freeStatus;
        this.wantedStatus = wantedStatus;
        this.arrestedStatus = arrestedStatus;
        this.hiddenStatus = hiddenStatus;
    }

    public void CreateInitialWarrants(SimulationConfigData config, Func<NpcData, NpcRuntime> getNpcRuntime, Func<CityData, CityRuntime> getCityRuntime)
    {
        if (config == null || getNpcRuntime == null || getCityRuntime == null)
        {
            return;
        }

        foreach (InitialWantedRecordConfig warrantConfig in config.InitialWarrants)
        {
            if (warrantConfig == null || warrantConfig.target == null || warrantConfig.city == null)
            {
                continue;
            }

            NpcRuntime target = getNpcRuntime(warrantConfig.target);
            CityRuntime city = getCityRuntime(warrantConfig.city);
            CreateOrIncreaseWarrant(target, city, warrantConfig.bounty, warrantConfig.sentenceDays);
        }
    }

    public WantedRecordRuntime CreateOrIncreaseWarrant(NpcRuntime target, CityRuntime city, float bounty, int sentenceDays)
    {
        if (target == null || city == null)
        {
            return null;
        }

        WantedRecordRuntime record = GetActiveWarrant(target, city);

        if (record == null)
        {
            record = new WantedRecordRuntime(target, city, bounty, sentenceDays);
            wantedRecords.Add(record);
        }
        else
        {
            record.AddPenalty(bounty, sentenceDays);
        }

        SyncWantedStatus(target);
        Debug.Log($"{target.NpcName} agora possui mandado em {city.CityName}. Recompensa: {record.Bounty:0.##}. Pena: {record.SentenceDays} dias.");
        return record;
    }

    public bool Arrest(NpcRuntime guardRuntime, NpcRuntime targetRuntime, CityRuntime city)
    {
        if (guardRuntime == null || targetRuntime == null || city == null || arrestedStatus == null || IsArrested(targetRuntime) == true)
        {
            return false;
        }

        WantedRecordRuntime record = GetActiveWarrant(targetRuntime, city);

        if (record == null)
        {
            return false;
        }

        PrisonSentenceRuntime currentSentence = GetActiveSentence(targetRuntime);

        if (currentSentence != null)
        {
            return false;
        }

        PrisonSentenceRuntime sentence = new PrisonSentenceRuntime(targetRuntime, city, record, record.SentenceDays);
        prisonSentences.Add(sentence);
        targetRuntime.ClearHidden();
        targetRuntime.RemoveStatus(hiddenStatus);
        targetRuntime.RemoveStatus(freeStatus);
        targetRuntime.AddStatus(arrestedStatus);
        SyncWantedStatus(targetRuntime);
        Debug.Log($"{guardRuntime.NpcName} prendeu {targetRuntime.NpcName} em {city.CityName}. Pena restante: {sentence.RemainingDays} dias.");
        return true;
    }

    public void AdvanceSentences(List<NpcRuntime> npcRuntimeList)
    {
        for (int i = prisonSentences.Count - 1; i >= 0; i--)
        {
            PrisonSentenceRuntime sentence = prisonSentences[i];

            if (sentence == null || sentence.Target == null)
            {
                prisonSentences.RemoveAt(i);
                continue;
            }

            if (sentence.Warrant == null || sentence.Warrant.IsActive == false)
            {
                ReleasePrisoner(sentence.Target);
                prisonSentences.RemoveAt(i);
                Debug.Log($"{sentence.Target.NpcName} foi libertado porque seu mandado nao esta mais ativo.");
                continue;
            }

            if (IsArrested(sentence.Target) == false)
            {
                prisonSentences.RemoveAt(i);
                SyncWantedStatus(sentence.Target);
                continue;
            }

            sentence.AdvanceDay();

            if (sentence.RemainingDays > 0)
            {
                continue;
            }

            ResolveWarrant(sentence.Warrant);
            ReleasePrisoner(sentence.Target);
            prisonSentences.RemoveAt(i);
            Debug.Log($"{sentence.Target.NpcName} cumpriu sua pena em {sentence.City.CityName} e foi libertado.");
        }

        ReleasePrisonersWithoutActiveSentence(npcRuntimeList);
        SyncWantedStatuses(npcRuntimeList);
    }

    public bool EscapePrison(NpcRuntime targetRuntime, float escapeBountyPenalty)
    {
        if (targetRuntime == null || IsArrested(targetRuntime) == false)
        {
            return false;
        }

        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);

        if (sentence == null || sentence.Warrant == null || sentence.Warrant.IsActive == false)
        {
            ReleasePrisoner(targetRuntime);
            SyncWantedStatus(targetRuntime);
            return true;
        }

        if (escapeBountyPenalty > 0f)
        {
            sentence.Warrant.AddPenalty(escapeBountyPenalty, 0);
        }

        prisonSentences.Remove(sentence);
        targetRuntime.RemoveStatus(arrestedStatus);
        targetRuntime.AddStatus(freeStatus);
        SyncWantedStatus(targetRuntime);
        Debug.Log($"{targetRuntime.NpcName} fugiu da prisao em {sentence.City.CityName}. Recompensa atual: {sentence.Warrant.Bounty:0.##}.");
        return true;
    }

    public void SyncWantedStatuses(List<NpcRuntime> npcRuntimeList)
    {
        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            SyncWantedStatus(npcRuntime);
        }
    }

    public void SyncWantedStatus(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null || wantedStatus == null)
        {
            return;
        }

        if (HasAnyActiveWarrant(npcRuntime) == true)
        {
            npcRuntime.AddStatus(wantedStatus);
        }
        else
        {
            npcRuntime.RemoveStatus(wantedStatus);
        }
    }

    public bool HasActiveWarrantInCity(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        return GetActiveWarrant(targetRuntime, cityRuntime) != null;
    }

    public bool HasAnyActiveWarrant(NpcRuntime targetRuntime)
    {
        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.IsActive == true && record.Target == targetRuntime)
            {
                return true;
            }
        }

        return false;
    }

    public float GetBounty(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        WantedRecordRuntime record = GetActiveWarrant(targetRuntime, cityRuntime);
        return record != null ? record.Bounty : 0f;
    }

    public int GetRemainingSentenceDays(NpcRuntime targetRuntime)
    {
        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);
        return sentence != null ? sentence.RemainingDays : 0;
    }

    public bool IsArrested(NpcRuntime targetRuntime)
    {
        return targetRuntime != null && arrestedStatus != null && targetRuntime.CurrentStatus.Contains(arrestedStatus) == true;
    }

    public WantedRecordRuntime GetActiveWarrant(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        if (targetRuntime == null || cityRuntime == null)
        {
            return null;
        }

        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.IsActive == true && record.Target == targetRuntime && record.City == cityRuntime)
            {
                return record;
            }
        }

        return null;
    }

    public List<WantedRecordRuntime> GetActiveWarrants(NpcRuntime targetRuntime)
    {
        List<WantedRecordRuntime> records = new List<WantedRecordRuntime>();

        if (targetRuntime == null)
        {
            return records;
        }

        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.IsActive == true && record.Target == targetRuntime)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private PrisonSentenceRuntime GetActiveSentence(NpcRuntime targetRuntime)
    {
        if (targetRuntime == null)
        {
            return null;
        }

        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence != null && sentence.IsActive == true && sentence.Target == targetRuntime)
            {
                return sentence;
            }
        }

        return null;
    }

    private void ResolveWarrant(WantedRecordRuntime record)
    {
        if (record == null)
        {
            return;
        }

        record.Resolve();
        SyncWantedStatus(record.Target);
    }

    private void ReleasePrisonersWithoutActiveSentence(List<NpcRuntime> npcRuntimeList)
    {
        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            if (npcRuntime == null || IsArrested(npcRuntime) == false || GetActiveSentence(npcRuntime) != null)
            {
                continue;
            }

            ReleasePrisoner(npcRuntime);
            Debug.LogWarning($"{npcRuntime.NpcName} estava preso sem sentenca ativa e foi libertado.");
        }
    }

    private void ReleasePrisoner(NpcRuntime targetRuntime)
    {
        if (targetRuntime == null)
        {
            return;
        }

        targetRuntime.RemoveStatus(arrestedStatus);
        targetRuntime.AddStatus(freeStatus);
        targetRuntime.ClearHidden();
        targetRuntime.RemoveStatus(hiddenStatus);
        SyncWantedStatus(targetRuntime);
    }
}

[Serializable]
public class WantedRecordRuntime
{
    [NonSerialized] private NpcRuntime target;
    [NonSerialized] private CityRuntime city;
    [SerializeField] private float bounty;
    [SerializeField] private int sentenceDays;
    [SerializeField] private bool resolved;

    public NpcRuntime Target => target;
    public CityRuntime City => city;
    public float Bounty => bounty;
    public int SentenceDays => sentenceDays;
    public bool IsActive => resolved == false && target != null && city != null;

    public WantedRecordRuntime(NpcRuntime target, CityRuntime city, float bounty, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        this.bounty = Mathf.Max(0f, bounty);
        this.sentenceDays = Mathf.Max(1, sentenceDays);
    }

    public void AddPenalty(float additionalBounty, int additionalSentenceDays)
    {
        bounty += Mathf.Max(0f, additionalBounty);
        sentenceDays += Mathf.Max(0, additionalSentenceDays);
        sentenceDays = Mathf.Max(1, sentenceDays);
    }

    public void Resolve()
    {
        resolved = true;
    }
}

[Serializable]
public class PrisonSentenceRuntime
{
    [NonSerialized] private NpcRuntime target;
    [NonSerialized] private CityRuntime city;
    [NonSerialized] private WantedRecordRuntime warrant;
    [SerializeField] private int remainingDays;

    public NpcRuntime Target => target;
    public CityRuntime City => city;
    public WantedRecordRuntime Warrant => warrant;
    public int RemainingDays => remainingDays;
    public bool IsActive => target != null && city != null && remainingDays > 0;

    public PrisonSentenceRuntime(NpcRuntime target, CityRuntime city, WantedRecordRuntime warrant, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        this.warrant = warrant;
        remainingDays = Mathf.Max(1, sentenceDays);
    }

    public void AdvanceDay()
    {
        remainingDays = Mathf.Max(0, remainingDays - 1);
    }
}
