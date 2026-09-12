using System;

/// <summary>
/// Centralized ranges and normalization for the social dimensions used by NPC relations.
/// </summary>
public static class NpcRelationValueRanges
{
    public const float AffinityMin = -100f;
    public const float AffinityMax = 100f;
    public const float TrustMin = -100f;
    public const float TrustMax = 100f;
    public const float FearMin = 0f;
    public const float FearMax = 100f;

    public static float ClampAffinity(float value)
    {
        return Clamp(value, AffinityMin, AffinityMax, 0f);
    }

    public static float ClampTrust(float value)
    {
        return Clamp(value, TrustMin, TrustMax, 0f);
    }

    public static float ClampFear(float value)
    {
        return Clamp(value, FearMin, FearMax, 0f);
    }

    private static float Clamp(float value, float minimum, float maximum, float nanFallback)
    {
        if (float.IsNaN(value) == true)
        {
            return nanFallback;
        }

        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }
}

/// <summary>
/// One directed relationship from a source NPC to a target NPC.
/// RuntimeIds are the canonical identity; no NpcRuntime references are retained here.
/// </summary>
[Serializable]
public sealed class NpcRelationRuntime
{
    private readonly string sourceNpcRuntimeId;
    private readonly string targetNpcRuntimeId;
    private float affinity;
    private float trust;
    private float fear;

    public string SourceNpcRuntimeId => sourceNpcRuntimeId;
    public string TargetNpcRuntimeId => targetNpcRuntimeId;
    public float Affinity => affinity;
    public float Trust => trust;
    public float Fear => fear;

    public NpcRelationRuntime(string sourceNpcRuntimeId, string targetNpcRuntimeId)
    {
        sourceNpcRuntimeId = RequireRuntimeId(sourceNpcRuntimeId, nameof(sourceNpcRuntimeId));
        targetNpcRuntimeId = RequireRuntimeId(targetNpcRuntimeId, nameof(targetNpcRuntimeId));

        if (string.Equals(sourceNpcRuntimeId, targetNpcRuntimeId, StringComparison.Ordinal) == true)
        {
            throw new ArgumentException("NpcRelationRuntime cannot relate an NPC to itself.", nameof(targetNpcRuntimeId));
        }

        this.sourceNpcRuntimeId = sourceNpcRuntimeId;
        this.targetNpcRuntimeId = targetNpcRuntimeId;
        affinity = 0f;
        trust = 0f;
        fear = 0f;
    }

    public void SetAffinity(float value)
    {
        affinity = NpcRelationValueRanges.ClampAffinity(value);
    }

    public void SetTrust(float value)
    {
        trust = NpcRelationValueRanges.ClampTrust(value);
    }

    public void SetFear(float value)
    {
        fear = NpcRelationValueRanges.ClampFear(value);
    }

    public void AdjustAffinity(float amount)
    {
        SetAffinity(affinity + amount);
    }

    public void AdjustTrust(float amount)
    {
        SetTrust(trust + amount);
    }

    public void AdjustFear(float amount)
    {
        SetFear(fear + amount);
    }

    private static string RequireRuntimeId(string runtimeId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("NPC relation requires non-empty NPC RuntimeIds.", parameterName);
        }

        return runtimeId;
    }
}
