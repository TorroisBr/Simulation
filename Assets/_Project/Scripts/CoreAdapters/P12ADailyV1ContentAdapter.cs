using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Unity reflection boundary for the selected Daily-v1 serialized content graph.</summary>
internal static class P12ADailyV1ContentAdapter
{
    private const int MaxAssets = 65536;
    private const int MaxDepth = 64;
    private const string CatalogIdentity = "p12a.official-content-catalog/v1";

    internal static bool TryCapture(SimulationConfigData configuration,
        out DailyV1SelectedContentProjection projection, out string diagnostic)
    {
        projection = null;
        diagnostic = null;
        if (configuration == null)
        {
            diagnostic = "A Daily-v1 content profile is required.";
            return false;
        }

        try
        {
            SortedDictionary<string, ScriptableObject> assets = new SortedDictionary<string, ScriptableObject>(StringComparer.Ordinal);
            Dictionary<string, DailyV1SelectedContentAsset> records = new Dictionary<string, DailyV1SelectedContentAsset>(StringComparer.Ordinal);
            Queue<ScriptableObject> pending = new Queue<ScriptableObject>();
            CaptureByteBudget budget = new CaptureByteBudget();
            DailyV1SelectedContentAsset profile = CaptureAssetRecord(
                DailyV1SelectedContentAsset.ProfileKey, configuration, assets, pending, 0, budget);
            while (pending.Count != 0)
            {
                ScriptableObject asset = pending.Dequeue();
                string key = AssetKey(asset);
                if (records.ContainsKey(key)) continue;
                if (records.Count >= MaxAssets)
                    throw new InvalidOperationException("The Daily-v1 content catalog exceeds its asset bound.");
                if (!assets.TryGetValue(key, out ScriptableObject registered) || !ReferenceEquals(asset, registered))
                    throw new InvalidOperationException("The Daily-v1 content catalog has an ambiguous stable definition identity.");
                records.Add(key, CaptureAssetRecord(key, asset, assets, pending, 0, budget));
            }

            if (!DailyV1SelectedContentProjectionFactory.TryCreate(records.Values, profile, out projection, out diagnostic))
                return false;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is System.Text.EncoderFallbackException
            || exception is System.IO.IOException || exception is TargetInvocationException
            || exception is P12ALimitException)
        {
            projection = null;
            diagnostic = exception.Message;
            return false;
        }
    }

    private static DailyV1SelectedContentAsset CaptureAssetRecord(string key, object value,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending, int depth,
        CaptureByteBudget budget)
    {
        Type type = value.GetType();
        List<FieldInfo> sourceFields = SerializableFields(type);
        budget.BeginRecord(key, type.FullName ?? type.Name, sourceFields.Count);
        List<DailyV1SelectedContentField> fields = CaptureFields(value, assets, pending, depth - 1, budget, sourceFields);
        return new DailyV1SelectedContentAsset(key, type.FullName ?? type.Name, fields);
    }

    private static List<DailyV1SelectedContentField> CaptureFields(object value,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending, int depth,
        CaptureByteBudget budget, List<FieldInfo> fields = null)
    {
        if (depth > MaxDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        Type type = value.GetType();
        fields = fields ?? SerializableFields(type);
        List<DailyV1SelectedContentField> output = new List<DailyV1SelectedContentField>(fields.Count);
        foreach (FieldInfo field in fields)
        {
            string declaringType = field.DeclaringType?.FullName ?? type.FullName ?? type.Name;
            object fieldValue = field.GetValue(value);
            if (budget != null)
            {
                budget.AddString(declaringType);
                budget.AddString(field.Name);
                MeasureValue(fieldValue, field.FieldType, depth + 1, budget);
            }
            output.Add(new DailyV1SelectedContentField(
                declaringType,
                field.Name,
                CaptureValue(fieldValue, field.FieldType, assets, pending, depth + 1)));
        }
        return output;
    }

    private static DailyV1SelectedContentValue CaptureValue(object value, Type declaredType,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending, int depth)
    {
        if (depth > MaxDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null)
            return DailyV1SelectedContentValue.Null(declaredType.FullName ?? declaredType.Name);
        Type type = value.GetType();
        string token = type.FullName ?? type.Name;

        if (value is UnityEngine.Object unityObject)
        {
            if (!(unityObject is ScriptableObject asset))
                throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported Unity object reference.");
            if (asset == null) throw new InvalidOperationException("A Daily-v1 definition reference is missing.");
            string key = AssetKey(asset);
            if (assets.TryGetValue(key, out ScriptableObject prior))
            {
                if (!ReferenceEquals(prior, asset))
                    throw new InvalidOperationException("The Daily-v1 catalog has duplicate definition identities.");
            }
            else
            {
                assets.Add(key, asset);
                pending.Enqueue(asset);
                if (assets.Count > MaxAssets) throw new InvalidOperationException("The Daily-v1 content catalog exceeds its asset bound.");
            }
            return DailyV1SelectedContentValue.Reference(token, key);
        }

        if (value is IDictionary)
            throw new InvalidOperationException("Dictionary values are not admitted in the authored Daily-v1 catalog.");
        if (value is string || type.IsPrimitive || value is decimal || value is Guid || value is DateTime || value is TimeSpan)
        {
            if (type.IsEnum)
            {
                string spelling = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                return DailyV1SelectedContentValue.Enum(token, spelling);
            }
            return DailyV1SelectedContentValue.Scalar(token, value);
        }
        if (type.IsEnum)
        {
            string spelling = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            return DailyV1SelectedContentValue.Enum(token, spelling);
        }
        if (value is IList list)
        {
            Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            List<DailyV1SelectedContentValue> items = new List<DailyV1SelectedContentValue>(list.Count);
            foreach (object item in list) items.Add(CaptureValue(item, elementType, assets, pending, depth + 1));
            return DailyV1SelectedContentValue.List(token, items);
        }
        List<FieldInfo> objectFields = SerializableFields(type);
        List<DailyV1SelectedContentField> fields = CaptureFields(value, assets, pending, depth, null, objectFields);
        if (fields.Count == 0)
            throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported serialized value: " + token);
        return DailyV1SelectedContentValue.Object(token, fields);
    }

    // Preflight a complete value subtree before CaptureValue allocates its DTO lists and fields.
    private static void MeasureValue(object value, Type declaredType, int depth, CaptureByteBudget budget)
    {
        if (depth > MaxDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null) { budget.Add(1); return; }
        Type type = value.GetType();
        string token = type.FullName ?? type.Name;
        budget.AddString(token);
        if (value is UnityEngine.Object unityObject)
        {
            if (!(unityObject is ScriptableObject asset))
                throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported Unity object reference.");
            if (asset == null) throw new InvalidOperationException("A Daily-v1 definition reference is missing.");
            budget.Add(1);
            budget.AddString(AssetKey(asset));
            return;
        }
        if (value is IDictionary)
            throw new InvalidOperationException("Dictionary values are not admitted in the authored Daily-v1 catalog.");
        if (value is string || type.IsPrimitive || value is decimal || value is Guid || value is DateTime || value is TimeSpan)
        {
            if (type.IsEnum)
            {
                budget.Add(1);
                budget.AddString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
            }
            else budget.AddScalar(value);
            return;
        }
        if (type.IsEnum)
        {
            budget.Add(1);
            budget.AddString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (value is IList list)
        {
            Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            budget.Add(5);
            budget.EnsureMinimumItems(list.Count, elementType);
            foreach (object item in list) MeasureValue(item, elementType, depth + 1, budget);
            return;
        }
        List<FieldInfo> fields = SerializableFields(type);
        budget.Add(5);
        foreach (FieldInfo field in fields)
        {
            budget.AddString(field.DeclaringType?.FullName ?? type.FullName ?? type.Name);
            budget.AddString(field.Name);
            MeasureValue(field.GetValue(value), field.FieldType, depth + 1, budget);
        }
        if (fields.Count == 0)
            throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported serialized value: " + token);
    }

    /// <summary>Counts legacy P12 bytes during reflection so oversized collections are rejected before DTO allocation.</summary>
    private sealed class CaptureByteBudget
    {
        private long _bytes;

        public CaptureByteBudget()
        {
            AddString(CatalogIdentity);
            Add(4); // catalog record count
        }

        public void BeginRecord(string key, string typeToken, int fieldCount)
        {
            AddString(key);
            Add(4); // serialized payload length
            AddString(typeToken);
            Add(4); // serialized field count
            if (fieldCount < 0) throw new InvalidOperationException("The Daily-v1 content record has an invalid field count.");
        }

        public void AddString(string value)
        {
            int count = ContinuationWire.GetEncodedStringByteCount(value);
            Add(checked(4L + count));
        }

        public void AddScalar(object value)
        {
            if (value is string text) { Add(1); AddString(text); return; }
            if (value is bool || value is byte || value is sbyte) { Add(2); return; }
            if (value is short || value is ushort || value is char) { Add(3); return; }
            if (value is int || value is uint || value is float) { Add(5); return; }
            if (value is long || value is ulong || value is double || value is DateTime || value is TimeSpan) { Add(9); return; }
            if (value is decimal) { Add(17); return; }
            if (value is Guid guid) { Add(1); AddString(guid.ToString("N")); return; }
            throw new InvalidOperationException("The Daily-v1 content catalog contains an unsupported scalar.");
        }

        public void EnsureMinimumItems(int itemCount, Type elementType)
        {
            if (itemCount < 0) throw new InvalidOperationException("A Daily-v1 collection has a negative count.");
            long minimum = MinimumValueBytes(elementType);
            long required = checked((long)itemCount * minimum);
            if (required > DailyV1ContentProjection.MaxCatalogBytes - _bytes)
                throw new InvalidOperationException("The Daily-v1 content catalog exceeds its byte bound during Unity capture.");
        }

        private static long MinimumValueBytes(Type type)
        {
            if (!type.IsValueType) return 1; // null is a valid serialized value
            string token = type.FullName ?? type.Name;
            long tokenBytes = checked(4L + ContinuationWire.GetEncodedStringByteCount(token));
            if (type.IsEnum) return checked(tokenBytes + 1 + 4); // tag and minimum empty spelling
            if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte)) return checked(tokenBytes + 2);
            if (type == typeof(short) || type == typeof(ushort) || type == typeof(char)) return checked(tokenBytes + 3);
            if (type == typeof(int) || type == typeof(uint) || type == typeof(float)) return checked(tokenBytes + 5);
            if (type == typeof(long) || type == typeof(ulong) || type == typeof(double)
                || type == typeof(DateTime) || type == typeof(TimeSpan)) return checked(tokenBytes + 9);
            if (type == typeof(decimal)) return checked(tokenBytes + 17);
            if (type == typeof(Guid)) return checked(tokenBytes + 1 + 4 + 32);
            return 1;
        }

        private void Add(long count)
        {
            _bytes = checked(_bytes + count);
            if (count < 0 || _bytes > DailyV1ContentProjection.MaxCatalogBytes)
                throw new InvalidOperationException("The Daily-v1 content catalog exceeds its byte bound during Unity capture.");
        }
    }

    private static List<FieldInfo> SerializableFields(Type type)
    {
        List<FieldInfo> fields = new List<FieldInfo>();
        for (Type current = type; current != null && current != typeof(object)
            && current != typeof(UnityEngine.Object); current = current.BaseType)
        {
            FieldInfo[] declared = current.GetFields(BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in declared)
            {
                if (field.IsStatic || field.IsNotSerialized || field.IsInitOnly) continue;
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField), true)
                    || Attribute.IsDefined(field, typeof(SerializeReference), true)) fields.Add(field);
            }
        }
        fields.Sort((left, right) =>
        {
            int compare = StringComparer.Ordinal.Compare(left.DeclaringType?.FullName, right.DeclaringType?.FullName);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Name, right.Name);
        });
        return fields;
    }

    private static string AssetKey(ScriptableObject asset)
    {
        if (asset == null) throw new InvalidOperationException("A Daily-v1 definition reference is missing.");
        Type type = asset.GetType();
        PropertyInfo property = type.GetProperty("DefinitionId", BindingFlags.Public | BindingFlags.Instance);
        string id = property?.GetValue(asset, null) as string;
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("A Daily-v1 definition lacks a stable DefinitionId.");
        return (type.FullName ?? type.Name) + "/" + id;
    }
}
