using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CORE_F_SharedRuntimeOracleCaptureTests
{
    [Test]
    public void CaptureSelectedUnityMathfResults()
    {
        CaptureMaxMin("ordinary", 2f, 5f);
        CaptureMaxMin("negative", -9f, -2f);
        CaptureMaxMin("signed-zero", -0f, 0f);
        CaptureMaxMin("nan-left", float.NaN, 1f);
        CaptureMaxMin("nan-right", 1f, float.NaN);
        CaptureMaxMin("infinity", float.NegativeInfinity, float.PositiveInfinity);

        CaptureClamp("ordinary", 12f, 0f, 10f);
        CaptureClamp("negative", -4f, -2f, 3f);
        CaptureClamp("signed-zero", -0f, -0f, 0f);
        CaptureClamp("reversed", 4f, 8f, 2f);
        CaptureClamp("nan", float.NaN, 0f, 1f);
        CaptureClamp01("negative", -1f);
        CaptureClamp01("ordinary", 0.375f);
        CaptureClamp01("above", 2f);
        CaptureClamp01("nan", float.NaN);
        CaptureClamp01("positive-infinity", float.PositiveInfinity);

        foreach (float value in new[] { -2.5f, -1.5f, -0.5f, -0.1f, -0f, 0f, 0.1f, 0.5f, 1.5f, 2.5f,
            -2147483904f, -2147483648f, 2147483520f, 2147483648f,
            float.Epsilon, float.NaN, float.NegativeInfinity, float.PositiveInfinity, float.MaxValue, float.MinValue })
        {
            TestContext.WriteLine("ROUND|" + F(value) + "|" + Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture));
            TestContext.WriteLine("FLOOR|" + F(value) + "|" + Mathf.FloorToInt(value).ToString(CultureInfo.InvariantCulture));
        }

        CaptureApproximately("equal-zero", 0f, 0f);
        CaptureApproximately("signed-zero", -0f, 0f);
        CaptureApproximately("minimum-subnormal", 0f, float.Epsilon);
        CaptureApproximately("within-small", 0f, 0.0000005f);
        CaptureApproximately("outside-small", 0f, 0.000002f);
        CaptureApproximately("relative-within", 1000f, 1000.0005f);
        CaptureApproximately("relative-outside", 1000f, 1000.01f);
        CaptureApproximately("nan", float.NaN, 0f);
        CaptureApproximately("infinity", float.PositiveInfinity, float.PositiveInfinity);

        foreach (var pair in new[] {
            Tuple.Create(2f, 3f), Tuple.Create(-2f, 3f), Tuple.Create(-2f, 0.5f),
            Tuple.Create(0f, 0f), Tuple.Create(0f, -1f), Tuple.Create(float.PositiveInfinity, 2f),
            Tuple.Create(float.NaN, 2f) })
        {
            TestContext.WriteLine("POW|" + F(pair.Item1) + "|" + F(pair.Item2) + "|" + F(Mathf.Pow(pair.Item1, pair.Item2)));
        }
        foreach (float exponent in new[] { 0f, 1f, 2f, 3f, 7f, 31f })
            TestContext.WriteLine("POW_ESCAPECONTEXT|0.8|" + F(exponent) + "|" + F(Mathf.Pow(0.8f, exponent)));

        foreach (var pair in new[] {
            Tuple.Create(0, 0), Tuple.Create(-2, 1), Tuple.Create(1, -2),
            Tuple.Create(int.MinValue, int.MaxValue), Tuple.Create(int.MaxValue, int.MinValue) })
        {
            TestContext.WriteLine("MAXINT|" + pair.Item1.ToString(CultureInfo.InvariantCulture) + "|" + pair.Item2.ToString(CultureInfo.InvariantCulture)
                + "|" + Mathf.Max(pair.Item1, pair.Item2).ToString(CultureInfo.InvariantCulture));
            TestContext.WriteLine("MININT|" + pair.Item1.ToString(CultureInfo.InvariantCulture) + "|" + pair.Item2.ToString(CultureInfo.InvariantCulture)
                + "|" + Mathf.Min(pair.Item1, pair.Item2).ToString(CultureInfo.InvariantCulture));
        }

        TestContext.WriteLine("CLAMPINT|4|8|2|" + Mathf.Clamp(4, 8, 2).ToString(CultureInfo.InvariantCulture));
        TestContext.WriteLine("MININT3|8|5|3|" + Mathf.Min(8, 5, 3).ToString(CultureInfo.InvariantCulture));
        TestContext.WriteLine("MAXINT3|8|5|3|" + Mathf.Max(8, 5, 3).ToString(CultureInfo.InvariantCulture));
        Assert.Pass();
    }

    [Test]
    public void CaptureCommercialSharingPayloadAndReaderBehavior()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-oracle", "location-oracle");
        NpcRuntime first = new NpcRuntime("npc-a", SimulationTestFactory.CreateNpc("first", NpcJobType.Merchant), city, 100f);
        NpcRuntime second = new NpcRuntime("npc-b", SimulationTestFactory.CreateNpc("second", NpcJobType.Merchant), city, 100f);
        NpcRuntime[] roster = { second, first };
        CommercialKnowledgeSharingSystem owner = new CommercialKnowledgeSharingSystem(new SimulationTime(2L),
            new EffectiveCommercialKnowledgeConfiguration());
        CommercialKnowledgeSharingDailyBoundaryStepProvider provider = new CommercialKnowledgeSharingDailyBoundaryStepProvider(
            () => roster, id => id == first.RuntimeId ? first : id == second.RuntimeId ? second : null, owner);
        Assert.That(provider.TryCreateSteps(new DailyBoundaryOperation("world", "profile", 2L), 0,
            out var steps, out _), Is.True);
        foreach (BoundaryContinuationStep step in steps)
            TestContext.WriteLine("PROVIDER|" + step.StepId + "|" + step.OperationKind + "|" + B64(step.Payload));

        Type providerType = typeof(CommercialKnowledgeSharingDailyBoundaryStepProvider);
        Type planType = providerType.GetNestedType("Plan", BindingFlags.NonPublic);
        Type rosterType = providerType.GetNestedType("RosterMember", BindingFlags.NonPublic);
        Type participantType = providerType.GetNestedType("Participant", BindingFlags.NonPublic);
        Type edgeType = providerType.GetNestedType("Edge", BindingFlags.NonPublic);
        object plan = Activator.CreateInstance(planType, true);
        Array rosterRows = Array.CreateInstance(rosterType, 1);
        rosterRows.SetValue(CreateRow(rosterType, new[] { "runtimeId", "personId" }, new[] { "npc-\"α\\id", "person-Ω" }), 0);
        Array participantRows = Array.CreateInstance(participantType, 1);
        participantRows.SetValue(CreateRow(participantType, new[] { "runtimeId", "personId", "cityRuntimeId", "locationRuntimeId" },
            new[] { "npc-\"α\\id", "person-Ω", "city-雪", "loc-\nline" }), 0);
        Array edgeRows = Array.CreateInstance(edgeType, 1);
        edgeRows.SetValue(CreateRow(edgeType, new[] { "senderRuntimeId", "receiverRuntimeId" }, new[] { "npc-\"α\\id", "npc-β" }), 0);
        SetField(plan, "runtimeRoster", rosterRows);
        SetField(plan, "participants", participantRows);
        SetField(plan, "edges", edgeRows);
        string specialPlan = JsonUtility.ToJson(plan);
        TestContext.WriteLine("SPECIAL_PLAN|" + B64(specialPlan));
        object edge = CreateRow(edgeType, new[] { "senderRuntimeId", "receiverRuntimeId" }, new[] { "npc-\"α\\id", "npc-β" });
        TestContext.WriteLine("SPECIAL_EDGE|" + B64(JsonUtility.ToJson(edge)));

        MethodInfo tryRead = providerType.GetMethod("TryRead", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (string json in new[] { "", " ", "{", "[]", "null", "{}", "{\"runtimeRoster\":null,\"participants\":[],\"edges\":[]}",
            "{\"runtimeRoster\":[],\"participants\":[],\"edges\":[]}",
            "{\"runtimeRoster\":[],\"participants\":[],\"edges\":[],\"futureField\":7}",
            "{\"runtimeRoster\":[],\"participants\":[],\"edges\":[]} trailing",
            "{\"runtimeRoster\":7,\"participants\":[],\"edges\":[]}",
            "{\"runtimeRoster\":[],\"participants\":[],\"edges\":[],\"edges\":[null]}",
            "{\"runtimeRoster\":[null],\"participants\":[],\"edges\":[]}" })
            CaptureRead(tryRead, planType, json, "PLAN_READ");

        foreach (string json in new[] { "", "null", "{}", "{\"senderRuntimeId\":\"a\",\"receiverRuntimeId\":\"b\"}",
            "{\"senderRuntimeId\":\"a\",\"receiverRuntimeId\":\"b\",\"futureField\":9}",
            "{\"senderRuntimeId\":\"a\",\"receiverRuntimeId\":\"b\"} trailing", "{\"senderRuntimeId\":7,\"receiverRuntimeId\":\"b\"}" })
            CaptureRead(tryRead, edgeType, json, "EDGE_READ");
        Assert.Pass();
    }

    [Test]
    public void CaptureExpandedJsonEscapingAndLegacyReaderBehavior()
    {
        Type providerType = typeof(CommercialKnowledgeSharingDailyBoundaryStepProvider);
        Type edgeType = providerType.GetNestedType("Edge", BindingFlags.NonPublic);
        MethodInfo tryRead = providerType.GetMethod("TryRead", BindingFlags.NonPublic | BindingFlags.Static);

        char[] controlCharacters = new char[32];
        for (int i = 0; i < controlCharacters.Length; i++)
        {
            controlCharacters[i] = (char)i;
            CaptureJsonStringRoundTrip(edgeType, "control-U+" + i.ToString("X4", CultureInfo.InvariantCulture),
                new string(new[] { (char)i }));
        }
        CaptureJsonStringRoundTrip(edgeType, "all-control-characters", new string(controlCharacters));
        CaptureJsonStringRoundTrip(edgeType, "null-string", null);
        CaptureJsonStringRoundTrip(edgeType, "empty-string", string.Empty);
        CaptureJsonStringRoundTrip(edgeType, "supplementary-surrogate-pair", char.ConvertFromUtf32(0x1F642));
        CaptureJsonStringRoundTrip(edgeType, "isolated-high-surrogate", new string(new[] { '\uD800' }));
        CaptureJsonStringRoundTrip(edgeType, "isolated-low-surrogate", new string(new[] { '\uDC00' }));

        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"first\",\"senderRuntimeId\":\"second\",\"receiverRuntimeId\":\"receiver\"}",
            "EDGE_DUPLICATE_STRING_KEY");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":null,\"receiverRuntimeId\":\"receiver\"}", "EDGE_NULL_STRING");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"\",\"receiverRuntimeId\":\"receiver\"}", "EDGE_EMPTY_STRING");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"first\",\"senderRuntimeId\":null,\"receiverRuntimeId\":\"receiver\"}",
            "EDGE_DUPLICATE_NULL_KEY");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"\\uD83D\\uDE42\",\"receiverRuntimeId\":\"\\uD800\"}",
            "EDGE_VALID_PAIR_AND_ISOLATED_HIGH");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"\\uD83D\\uDE42\",\"receiverRuntimeId\":\"receiver\"}",
            "EDGE_VALID_SURROGATE_PAIR");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"\\uD800\",\"receiverRuntimeId\":\"receiver\"}",
            "EDGE_ISOLATED_HIGH_SURROGATE");
        CaptureRead(tryRead, edgeType,
            "{\"senderRuntimeId\":\"\\uDC00\",\"receiverRuntimeId\":\"receiver\"}",
            "EDGE_ISOLATED_LOW_SURROGATE");

        Type planType = providerType.GetNestedType("Plan", BindingFlags.NonPublic);
        CaptureRead(tryRead, planType,
            "{\"runtimeRoster\":[],\"runtimeRoster\":[null],\"participants\":[],\"edges\":[]}",
            "PLAN_DUPLICATE_ARRAY_KEY");
        CaptureRead(tryRead, planType,
            "{\"runtimeRoster\":null,\"participants\":[],\"edges\":[]}", "PLAN_NULL_ARRAY");
        CaptureRead(tryRead, planType,
            "{\"runtimeRoster\":[],\"participants\":[],\"edges\":[]}", "PLAN_EMPTY_ARRAYS");
        Assert.Pass();
    }

    [Test]
    public void CaptureExpandedApproximatelyAndPowBoundaries()
    {
        Type mathfInternal = typeof(Mathf).Assembly.GetType("UnityEngineInternal.MathfInternal");
        FieldInfo flushToZero = mathfInternal == null ? null : mathfInternal.GetField(
            "IsFlushToZeroEnabled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        object flushToZeroValue = null;
        try { if (flushToZero != null) flushToZeroValue = flushToZero.GetValue(null); }
        catch (Exception ex) { flushToZeroValue = "throws:" + ex.GetType().FullName; }
        TestContext.WriteLine("APPROX_ENV|epsilon=" + F(Mathf.Epsilon) + "|flushToZero=" + (flushToZeroValue ?? "unavailable"));

        float epsilonFloor = 8f * Mathf.Epsilon;
        CaptureApproximately("zero-floor-7-epsilon", 0f, 7f * Mathf.Epsilon);
        CaptureApproximately("zero-floor-8-epsilon", 0f, epsilonFloor);
        CaptureApproximately("zero-floor-9-epsilon", 0f, 9f * Mathf.Epsilon);
        CaptureApproximately("positive-infinity-self", float.PositiveInfinity, float.PositiveInfinity);
        CaptureApproximately("negative-infinity-self", float.NegativeInfinity, float.NegativeInfinity);
        CaptureApproximately("opposite-infinities", float.NegativeInfinity, float.PositiveInfinity);
        CaptureApproximately("finite-infinity", 1f, float.PositiveInfinity);
        CaptureApproximately("nan-self", float.NaN, float.NaN);

        foreach (float scale in new[] { 0.000001f, 0.01f, 1f, 1000f, 1000000f })
        {
            float tolerance = Mathf.Max(1e-6f * Mathf.Abs(scale), epsilonFloor);
            float roundedBoundary = scale + tolerance;
            uint boundaryBits = FloatBits(roundedBoundary);
            CaptureApproximately("relative-below-" + F(scale), scale, FloatFromBits(boundaryBits - 1));
            CaptureApproximately("relative-boundary-" + F(scale), scale, roundedBoundary);
            CaptureApproximately("relative-above-" + F(scale), scale, FloatFromBits(boundaryBits + 1));
        }

        int[] escapeAttempts = {
            0, 1, -1,
            458, 459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470,
            -394, -395, -396, -397, -398, -399, -400, -401, -402,
            -458, -459, -460, -461, -462, -463, -464, -465, -466, -467, -468, -469, -470,
            16777214, 16777215, 16777216, 16777217, 16777218,
            -16777214, -16777215, -16777216, -16777217, -16777218,
            int.MinValue, int.MinValue + 1, int.MaxValue - 1, int.MaxValue
        };
        foreach (int sourceValue in escapeAttempts)
        {
            float exponent = sourceValue;
            TestContext.WriteLine("POW_ESCAPE_INT|" + sourceValue.ToString(CultureInfo.InvariantCulture)
                + "|" + F(exponent) + "|" + F(Mathf.Pow(0.8f, exponent)));
        }
        Assert.Pass();
    }

    private static void CaptureJsonStringRoundTrip(Type dtoType, string label, string value)
    {
        try
        {
            object dto = CreateRow(dtoType, new[] { "senderRuntimeId", "receiverRuntimeId" }, new[] { value, "receiver" });
            string json = JsonUtility.ToJson(dto);
            object decoded = JsonUtility.FromJson(json, dtoType);
            string decodedValue = decoded == null ? null : (string)GetField(decoded, "senderRuntimeId");
            TestContext.WriteLine("JSON_STRING_ROUNDTRIP|" + label + "|jsonUtf16=" + Utf16Hex(json)
                + "|decodedUtf16=" + Utf16Hex(decodedValue));
        }
        catch (Exception ex)
        {
            TestContext.WriteLine("JSON_STRING_ROUNDTRIP|" + label + "|throws="
                + (ex is TargetInvocationException && ex.InnerException != null
                    ? ex.InnerException.GetType().FullName : ex.GetType().FullName));
        }
    }

    private static string Utf16Hex(string value)
    {
        if (value == null) return "~";
        char[] hex = new char[value.Length * 4];
        const string digits = "0123456789ABCDEF";
        for (int i = 0; i < value.Length; i++)
        {
            int codeUnit = value[i];
            int offset = i * 4;
            hex[offset] = digits[(codeUnit >> 12) & 0xF];
            hex[offset + 1] = digits[(codeUnit >> 8) & 0xF];
            hex[offset + 2] = digits[(codeUnit >> 4) & 0xF];
            hex[offset + 3] = digits[codeUnit & 0xF];
        }
        return new string(hex);
    }

    private static uint FloatBits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);

    private static float FloatFromBits(uint bits) => BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

    private static void CaptureRead(MethodInfo genericReader, Type dtoType, string json, string label)
    {
        MethodInfo reader = genericReader.MakeGenericMethod(dtoType);
        object[] arguments = { json, null };
        string outcome;
        try
        {
            bool success = (bool)reader.Invoke(null, arguments);
            outcome = success ? "true:" + Describe(arguments[1], dtoType) : "false:" + Describe(arguments[1], dtoType);
        }
        catch (TargetInvocationException ex)
        {
            outcome = "throws:" + (ex.InnerException == null ? ex.GetType().FullName : ex.InnerException.GetType().FullName);
        }
        TestContext.WriteLine(label + "|" + B64(json) + "|" + outcome);
    }

    private static string Describe(object value, Type dtoType)
    {
        if (value == null) return "null";
        if (dtoType.Name == "Plan")
        {
            return "roster=" + DescribeArray(GetField(value, "runtimeRoster"))
                + ";participants=" + DescribeArray(GetField(value, "participants"))
                + ";edges=" + DescribeArray(GetField(value, "edges"));
        }
        return "sender=" + DescribeString(GetField(value, "senderRuntimeId"))
            + ";receiver=" + DescribeString(GetField(value, "receiverRuntimeId"));
    }

    private static string DescribeString(object value) => value == null ? "~" : Utf16Hex((string)value);

    private static string DescribeArray(object value)
    {
        if (value == null) return "null";
        Array array = (Array)value;
        string[] rows = new string[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            object row = array.GetValue(i);
            if (row == null) { rows[i] = "null"; continue; }
            FieldInfo[] fields = row.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string[] values = new string[fields.Length];
            for (int j = 0; j < fields.Length; j++) values[j] = fields[j].Name + "=" + B64((string)fields[j].GetValue(row));
            rows[i] = string.Join(",", values);
        }
        return "len=" + array.Length.ToString(CultureInfo.InvariantCulture) + "[" + string.Join(";", rows) + "]";
    }

    private static object GetField(object value, string name) =>
        value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(value);

    private static string B64(string value) => value == null ? "~" : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    private static object CreateRow(Type type, string[] names, string[] values)
    {
        object row = Activator.CreateInstance(type, true);
        for (int i = 0; i < names.Length; i++) SetField(row, names[i], values[i]);
        return row;
    }

    private static void SetField(object value, string name, object fieldValue) =>
        value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(value, fieldValue);

    private static void CaptureMaxMin(string label, float left, float right)
    {
        TestContext.WriteLine("MAXFLOAT|" + label + "|" + F(left) + "|" + F(right) + "|" + F(Mathf.Max(left, right)));
        TestContext.WriteLine("MINFLOAT|" + label + "|" + F(left) + "|" + F(right) + "|" + F(Mathf.Min(left, right)));
    }

    private static void CaptureClamp(string label, float value, float minimum, float maximum) =>
        TestContext.WriteLine("CLAMPFLOAT|" + label + "|" + F(value) + "|" + F(minimum) + "|" + F(maximum) + "|" + F(Mathf.Clamp(value, minimum, maximum)));

    private static void CaptureClamp01(string label, float value) =>
        TestContext.WriteLine("CLAMP01|" + label + "|" + F(value) + "|" + F(Mathf.Clamp01(value)));

    private static void CaptureApproximately(string label, float left, float right) =>
        TestContext.WriteLine("APPROX|" + label + "|" + F(left) + "|" + F(right) + "|" + Mathf.Approximately(left, right));

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture)
        + "[0x" + BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("X8", CultureInfo.InvariantCulture) + "]";
}
