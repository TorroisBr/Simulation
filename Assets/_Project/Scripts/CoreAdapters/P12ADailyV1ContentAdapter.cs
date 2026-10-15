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
            DailyV1SelectedContentAsset profile = CaptureAssetRecord(
                DailyV1SelectedContentAsset.ProfileKey, configuration, assets, pending, 0);
            while (pending.Count != 0)
            {
                ScriptableObject asset = pending.Dequeue();
                string key = AssetKey(asset);
                if (records.ContainsKey(key)) continue;
                if (!assets.TryGetValue(key, out ScriptableObject registered) || !ReferenceEquals(asset, registered))
                    throw new InvalidOperationException("The Daily-v1 content catalog has an ambiguous stable definition identity.");
                records.Add(key, CaptureAssetRecord(key, asset, assets, pending, 0));
                if (records.Count > MaxAssets) throw new InvalidOperationException("The Daily-v1 content catalog exceeds its asset bound.");
            }

            if (!DailyV1SelectedContentProjectionFactory.TryCreate(records.Values, profile, out projection, out diagnostic))
                return false;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is System.Text.EncoderFallbackException
            || exception is System.IO.IOException || exception is TargetInvocationException)
        {
            projection = null;
            diagnostic = exception.Message;
            return false;
        }
    }

    private static DailyV1SelectedContentAsset CaptureAssetRecord(string key, object value,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending, int depth)
    {
        Type type = value.GetType();
        List<DailyV1SelectedContentField> fields = CaptureFields(value, assets, pending, depth - 1);
        return new DailyV1SelectedContentAsset(key, type.FullName ?? type.Name, fields);
    }

    private static List<DailyV1SelectedContentField> CaptureFields(object value,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending, int depth)
    {
        if (depth > MaxDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        Type type = value.GetType();
        List<FieldInfo> fields = SerializableFields(type);
        List<DailyV1SelectedContentField> output = new List<DailyV1SelectedContentField>(fields.Count);
        foreach (FieldInfo field in fields)
        {
            object fieldValue = field.GetValue(value);
            output.Add(new DailyV1SelectedContentField(
                field.DeclaringType?.FullName ?? type.FullName ?? type.Name,
                field.Name,
                CaptureValue(fieldValue, field.FieldType, assets, pending, depth + 1)));
        }
        return output;
    }

    private static DailyV1SelectedContentValue CaptureValue(object value, Type declaredType,
        SortedDictionary<string, ScriptableObject> assets, Queue<ScriptableObject> pending, int depth)
    {
        if (depth > MaxDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null) return DailyV1SelectedContentValue.Null(declaredType.FullName ?? declaredType.Name);
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
            if (type.IsEnum) return DailyV1SelectedContentValue.Enum(token, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
            return DailyV1SelectedContentValue.Scalar(token, value);
        }
        if (type.IsEnum)
            return DailyV1SelectedContentValue.Enum(token, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
        if (value is IList list)
        {
            List<DailyV1SelectedContentValue> items = new List<DailyV1SelectedContentValue>(list.Count);
            Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            foreach (object item in list) items.Add(CaptureValue(item, elementType, assets, pending, depth + 1));
            return DailyV1SelectedContentValue.List(token, items);
        }
        List<DailyV1SelectedContentField> fields = CaptureFields(value, assets, pending, depth);
        if (fields.Count == 0)
            throw new InvalidOperationException("The Daily-v1 catalog contains an unsupported serialized value: " + token);
        return DailyV1SelectedContentValue.Object(token, fields);
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
