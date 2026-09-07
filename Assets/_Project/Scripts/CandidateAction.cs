using System;
using System.Collections.Generic;
using UnityEngine;

public class CandidateAction
{
    public string Id;
    public string Label;
    public int Weight;
    public List<string> Reasons = new List<string>();
    public Action Resolve;
}