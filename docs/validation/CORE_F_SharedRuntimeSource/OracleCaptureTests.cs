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
        return "sender=" + B64((string)GetField(value, "senderRuntimeId"))
            + ";receiver=" + B64((string)GetField(value, "receiverRuntimeId"));
    }

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
