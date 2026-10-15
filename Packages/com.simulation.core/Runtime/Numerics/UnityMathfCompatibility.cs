using System;

/// <summary>Unity 6000.3.9f1 numeric behavior used by the selected shared Core sources.</summary>
public static class UnityMathfCompatibility
{
    private const float Epsilon = 1.401298E-45f;

    public static float Max(float a, float b) => a > b ? a : b;
    public static float Min(float a, float b) => a < b ? a : b;
    public static int Max(int a, int b) => a > b ? a : b;
    public static int Min(int a, int b) => a < b ? a : b;
    public static int Max(int a, int b, int c) => Max(Max(a, b), c);
    public static int Min(int a, int b, int c) => Min(Min(a, b), c);

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float Clamp01(float value)
    {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }

    public static int FloorToInt(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return int.MinValue;
        double floor = Math.Floor((double)value);
        if (floor < int.MinValue || floor > int.MaxValue) return int.MinValue;
        return (int)floor;
    }

    public static int RoundToInt(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return int.MinValue;
        double rounded = Math.Round((double)value, MidpointRounding.ToEven);
        if (rounded < int.MinValue || rounded > int.MaxValue) return int.MinValue;
        return (int)rounded;
    }

    public static bool Approximately(float a, float b)
    {
        float difference = Math.Abs(b - a);
        float scale = Max(Math.Abs(a), Math.Abs(b));
        float threshold = Max(1e-6f * scale, 8f * Epsilon);
        return difference < threshold;
    }

    public static float Pow(float value, float power) => (float)Math.Pow((double)value, (double)power);
}
