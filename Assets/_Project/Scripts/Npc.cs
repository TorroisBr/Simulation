using System.Collections.Generic;
using UnityEngine;

public class Npc
{
    public string Id;
    public string Name;
    public string Role;

    public int Courage;
    public int Greed;
    public int Caution;
    public int Loyalty;
    public int Resources;

    public List<string> Tags = new List<string>();
    public HashSet<string> Status = new HashSet<string>();

    public bool HasStatus(string status)
    {
        return Status.Contains(status);
    }

    public void RemoveStatus(string status)
    {
        if (Status.Contains(status))
            Status.Remove(status);
    }
}