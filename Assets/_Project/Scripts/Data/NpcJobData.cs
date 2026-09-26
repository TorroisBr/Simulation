using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "World Simulation/Job")]
public class NpcJobData : ScriptableObject
{
    public string id;
    public string jobName;
    public string DefinitionId => id;
    public NpcJobType jobType = NpcJobType.None;
    public MerchantBehavior merchantBehavior = MerchantBehavior.Traveling;
    public float minimumProfitPerItem = 1f;
    public NpcActionData workAction;
    public float workUtility = 50;
    public List<TradeItemPreference> preferredTradeItems = new List<TradeItemPreference>();

    public List<TradeItemPreference> PreferredTradeItems => preferredTradeItems ?? (preferredTradeItems = new List<TradeItemPreference>());
}

[Serializable]
public class TradeItemPreference
{
    public ItemData item;
    public float utilityMultiplier = 1f;
}

public enum NpcJobType
{
    None,
    Merchant,
    Guard
}

public enum MerchantBehavior
{
    Traveling,
    Local
}
