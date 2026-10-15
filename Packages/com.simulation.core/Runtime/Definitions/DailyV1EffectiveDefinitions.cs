using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>A copied, engine-free value in the selected Daily-v1 definition graph.</summary>
internal sealed class DailyV1DefinitionValue
{
    public enum ValueKind { Null, Scalar, Enum, Reference, List, Object }

    private readonly object scalar;
    private readonly IReadOnlyList<DailyV1DefinitionValue> items;
    private readonly IReadOnlyList<DailyV1DefinitionField> fields;

    public ValueKind Kind { get; }
    public string TypeToken { get; }
    public string ReferenceId { get; }
    public string ReferenceKey { get; }
    public object Scalar => scalar;
    public int EnumValue { get; }
    public string EnumSpelling { get; }
    public IReadOnlyList<DailyV1DefinitionValue> Items => items;
    public IReadOnlyList<DailyV1DefinitionField> Fields => fields;

    private DailyV1DefinitionValue(ValueKind kind, string typeToken, object scalar,
        int enumValue, string enumSpelling, string referenceId, string referenceKey,
        IEnumerable<DailyV1DefinitionValue> items, IEnumerable<DailyV1DefinitionField> fields)
    {
        Kind = kind;
        TypeToken = typeToken;
        this.scalar = scalar;
        EnumValue = enumValue;
        EnumSpelling = enumSpelling;
        ReferenceId = referenceId;
        ReferenceKey = referenceKey;
        this.items = items == null ? Array.Empty<DailyV1DefinitionValue>()
            : new ReadOnlyCollection<DailyV1DefinitionValue>(new List<DailyV1DefinitionValue>(items));
        this.fields = fields == null ? Array.Empty<DailyV1DefinitionField>()
            : new ReadOnlyCollection<DailyV1DefinitionField>(new List<DailyV1DefinitionField>(fields));
    }

    public static DailyV1DefinitionValue Null(string typeToken) =>
        new DailyV1DefinitionValue(ValueKind.Null, typeToken, null, 0, null, null, null, null, null);

    public static DailyV1DefinitionValue ScalarValue(string typeToken, object value) =>
        new DailyV1DefinitionValue(ValueKind.Scalar, typeToken, CopyScalar(typeToken, value), 0, null, null, null, null, null);

    public static DailyV1DefinitionValue Enum(string typeToken, int numericValue, string spelling) =>
        new DailyV1DefinitionValue(ValueKind.Enum, typeToken, null, numericValue, spelling ?? string.Empty, null, null, null, null);

    public static DailyV1DefinitionValue Reference(string typeToken, string definitionId, string referenceKey = null)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("Reference identity is required.", nameof(definitionId));
        return new DailyV1DefinitionValue(ValueKind.Reference, typeToken, null, 0, null, definitionId,
            referenceKey ?? (typeToken + "/" + definitionId), null, null);
    }

    public static DailyV1DefinitionValue List(string typeToken, IEnumerable<DailyV1DefinitionValue> values) =>
        new DailyV1DefinitionValue(ValueKind.List, typeToken, null, 0, null, null, null,
            values ?? throw new ArgumentNullException(nameof(values)), null);

    public static DailyV1DefinitionValue Object(string typeToken, IEnumerable<DailyV1DefinitionField> values) =>
        new DailyV1DefinitionValue(ValueKind.Object, typeToken, null, 0, null, null, null, null,
            values ?? throw new ArgumentNullException(nameof(values)));

    public DailyV1DefinitionValue Get(string fieldName)
    {
        for (int i = 0; i < fields.Count; i++)
            if (string.Equals(fields[i].Name, fieldName, StringComparison.Ordinal)) return fields[i].Value;
        return null;
    }

    private static object CopyScalar(string typeToken, object value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        Type type = value.GetType();
        if (type == typeof(string) || type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint)
            || type == typeof(long) || type == typeof(ulong) || type == typeof(char) || type == typeof(float)
            || type == typeof(double) || type == typeof(decimal) || type == typeof(Guid)
            || type == typeof(DateTime) || type == typeof(TimeSpan)) return value;
        throw new ArgumentException("Unsupported Daily-v1 scalar type: " + (typeToken ?? type.FullName), nameof(value));
    }
}

internal sealed class DailyV1DefinitionField
{
    public string DeclaringTypeToken { get; }
    public string Name { get; }
    public DailyV1DefinitionValue Value { get; }

    public DailyV1DefinitionField(string declaringTypeToken, string name, DailyV1DefinitionValue value)
    {
        DeclaringTypeToken = Need(declaringTypeToken, nameof(declaringTypeToken));
        Name = Need(name, nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static string Need(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable field token is required.", parameter);
        return value;
    }
}

public abstract class DailyV1DefinitionRecord
{
    public string DefinitionId { get; }
    public string TypeToken { get; }

    protected DailyV1DefinitionRecord(string typeToken, string definitionId)
    {
        TypeToken = string.IsNullOrWhiteSpace(typeToken) ? throw new ArgumentException("Type token is required.", nameof(typeToken)) : typeToken;
        DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("DefinitionId is required.", nameof(definitionId)) : definitionId;
    }
}

public sealed class DailyV1CityDefinition : DailyV1DefinitionRecord
{ public DailyV1CityData Values { get; } internal DailyV1CityDefinition(string type, string id, DailyV1CityData values) : base(type, id) { Values = values ?? throw new ArgumentNullException(nameof(values)); } }
public sealed class DailyV1ItemDefinition : DailyV1DefinitionRecord
{ public DailyV1ItemData Values { get; } internal DailyV1ItemDefinition(string type, string id, DailyV1ItemData values) : base(type, id) { Values = values ?? throw new ArgumentNullException(nameof(values)); } }
public sealed class DailyV1NpcDefinition : DailyV1DefinitionRecord
{ public DailyV1NpcData Values { get; } internal DailyV1NpcDefinition(string type, string id, DailyV1NpcData values) : base(type, id) { Values = values ?? throw new ArgumentNullException(nameof(values)); } }
public sealed class DailyV1JobDefinition : DailyV1DefinitionRecord
{ public DailyV1NpcJobData Values { get; } internal DailyV1JobDefinition(string type, string id, DailyV1NpcJobData values) : base(type, id) { Values = values ?? throw new ArgumentNullException(nameof(values)); } }
public sealed class DailyV1ActionDefinition : DailyV1DefinitionRecord
{ public DailyV1NpcActionData Values { get; } internal DailyV1ActionDefinition(string type, string id, DailyV1NpcActionData values) : base(type, id) { Values = values ?? throw new ArgumentNullException(nameof(values)); } }
public sealed class DailyV1StatusDefinition : DailyV1DefinitionRecord
{ public DailyV1NpcStatusData Values { get; } internal DailyV1StatusDefinition(string type, string id, DailyV1NpcStatusData values) : base(type, id) { Values = values ?? throw new ArgumentNullException(nameof(values)); } }

/// <summary>Immutable resolved calendar values, preserving both uniform and custom-month shape.</summary>
/// <summary>Immutable effective Daily-v1 configuration and selected definition graph.</summary>
public sealed class DailyV1EffectiveDefinitions
{
    private readonly IReadOnlyDictionary<string, DailyV1CityDefinition> cities;
    private readonly IReadOnlyDictionary<string, DailyV1ItemDefinition> items;
    private readonly IReadOnlyDictionary<string, DailyV1NpcDefinition> npcs;
    private readonly IReadOnlyDictionary<string, DailyV1JobDefinition> jobs;
    private readonly IReadOnlyDictionary<string, DailyV1ActionDefinition> actions;
    private readonly IReadOnlyDictionary<string, DailyV1StatusDefinition> statuses;
    private readonly IReadOnlyDictionary<string, DailyV1ContentRecord> contentRecords;

    public DailyV1SimulationConfigData Configuration { get; }
    public EffectiveSimulationConfiguration EffectiveConfiguration { get; }
    public DailyV1CalendarDefinition Calendar { get; }
    public string ContentFingerprint { get; }
    public DailyV1SelectedContentProjection ContentProjection { get; }
    internal IReadOnlyDictionary<string, DailyV1ContentRecord> ContentRecords => contentRecords;
    public IReadOnlyDictionary<string, DailyV1CityDefinition> Cities => cities;
    public IReadOnlyDictionary<string, DailyV1ItemDefinition> Items => items;
    public IReadOnlyDictionary<string, DailyV1NpcDefinition> Npcs => npcs;
    public IReadOnlyDictionary<string, DailyV1JobDefinition> Jobs => jobs;
    public IReadOnlyDictionary<string, DailyV1ActionDefinition> Actions => actions;
    public IReadOnlyDictionary<string, DailyV1StatusDefinition> Statuses => statuses;
    public IReadOnlyList<string> StageOrder { get; }
    public string ProfileContractIdentity { get; }
    public string GeographyProfileContractIdentity { get; }
    public string GeographyStageIdentity { get; }
    public string HexCoordinateConvention { get; }
    public string HexCoordinateCanonicalOrder { get; }

    /// <summary>
    /// Creates the selected typed Daily-v1 graph. Serialized compatibility projection is
    /// attached separately by the selected content-capture path.
    /// </summary>
    public static DailyV1EffectiveDefinitions CreateSelectedSchema(
        DailyV1SimulationConfigData configuration,
        EffectiveSimulationConfiguration effectiveConfiguration,
        DailyV1CalendarDefinition resolvedCalendar,
        IEnumerable<DailyV1CityData> cities, IEnumerable<DailyV1ItemData> items,
        IEnumerable<DailyV1NpcData> npcs, IEnumerable<DailyV1NpcJobData> jobs,
        IEnumerable<DailyV1NpcActionData> actions, IEnumerable<DailyV1NpcStatusData> statuses,
        IEnumerable<string> stageOrder, string profileContractIdentity,
        string geographyProfileContractIdentity, string geographyStageIdentity,
        string hexCoordinateConvention, string hexCoordinateCanonicalOrder)
    {
        return new DailyV1EffectiveDefinitions(configuration, effectiveConfiguration, resolvedCalendar, null,
            Wrap(cities, value => new DailyV1CityDefinition("CityData", value.id, value)),
            Wrap(items, value => new DailyV1ItemDefinition("ItemData", value.id, value)),
            Wrap(npcs, value => new DailyV1NpcDefinition("NpcData", value.id, value)),
            Wrap(jobs, value => new DailyV1JobDefinition("NpcJobData", value.id, value)),
            Wrap(actions, value => new DailyV1ActionDefinition("NpcActionData", value.id, value)),
            Wrap(statuses, value => new DailyV1StatusDefinition("NpcStatusData", value.id, value)),
            null, stageOrder, profileContractIdentity, geographyProfileContractIdentity,
            geographyStageIdentity, hexCoordinateConvention, hexCoordinateCanonicalOrder, null);
    }

    /// <summary>Returns a copied graph with the validated selected P12 content projection attached.</summary>
    public DailyV1EffectiveDefinitions WithContentProjection(DailyV1SelectedContentProjection projection)
    {
        if (projection == null) throw new ArgumentNullException(nameof(projection));
        return new DailyV1EffectiveDefinitions(Configuration, EffectiveConfiguration, Calendar, null,
            cities.Values, items.Values, npcs.Values, jobs.Values, actions.Values, statuses.Values,
            contentRecords, StageOrder, ProfileContractIdentity, GeographyProfileContractIdentity,
            GeographyStageIdentity, HexCoordinateConvention, HexCoordinateCanonicalOrder, projection);
    }

    internal DailyV1EffectiveDefinitions(DailyV1SimulationConfigData configuration,
        EffectiveSimulationConfiguration effectiveConfiguration,
        DailyV1CalendarDefinition resolvedCalendar,
        string contentFingerprint,
        IEnumerable<DailyV1CityDefinition> cities, IEnumerable<DailyV1ItemDefinition> items,
        IEnumerable<DailyV1NpcDefinition> npcs, IEnumerable<DailyV1JobDefinition> jobs,
        IEnumerable<DailyV1ActionDefinition> actions, IEnumerable<DailyV1StatusDefinition> statuses,
        IEnumerable<KeyValuePair<string, DailyV1ContentRecord>> contentRecords,
        IEnumerable<string> stageOrder, string profileContractIdentity,
        string geographyProfileContractIdentity, string geographyStageIdentity,
        string hexCoordinateConvention, string hexCoordinateCanonicalOrder,
        DailyV1SelectedContentProjection contentProjection)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        EffectiveConfiguration = effectiveConfiguration ?? throw new ArgumentNullException(nameof(effectiveConfiguration));
        Calendar = resolvedCalendar ?? throw new ArgumentNullException(nameof(resolvedCalendar));
        this.cities = Freeze(cities);
        this.items = Freeze(items);
        this.npcs = Freeze(npcs);
        this.jobs = Freeze(jobs);
        this.actions = Freeze(actions);
        this.statuses = Freeze(statuses);
        this.contentRecords = FreezeContent(contentRecords);
        ContentProjection = contentProjection;
        ContentFingerprint = contentProjection != null ? contentProjection.Fingerprint
            : this.contentRecords.Count == 0 ? null : DailyV1ContentProjection.FingerprintRecords(this.contentRecords);
        if (contentProjection == null && !string.IsNullOrWhiteSpace(contentFingerprint)
            && !string.Equals(ContentFingerprint, contentFingerprint, StringComparison.Ordinal))
            throw new ArgumentException("The supplied content identity does not match the copied projection.", nameof(contentFingerprint));
        StageOrder = FreezeList(stageOrder);
        ProfileContractIdentity = Need(profileContractIdentity, nameof(profileContractIdentity));
        GeographyProfileContractIdentity = Need(geographyProfileContractIdentity, nameof(geographyProfileContractIdentity));
        GeographyStageIdentity = Need(geographyStageIdentity, nameof(geographyStageIdentity));
        HexCoordinateConvention = Need(hexCoordinateConvention, nameof(hexCoordinateConvention));
        HexCoordinateCanonicalOrder = Need(hexCoordinateCanonicalOrder, nameof(hexCoordinateCanonicalOrder));
    }

    private static IReadOnlyDictionary<string, T> Freeze<T>(IEnumerable<T> values) where T : DailyV1DefinitionRecord
    {
        SortedDictionary<string, T> copy = new SortedDictionary<string, T>(StringComparer.Ordinal);
        if (values != null)
        {
            foreach (T value in values)
            {
                if (value == null) continue;
                if (copy.ContainsKey(value.DefinitionId))
                    throw new ArgumentException("Duplicate Daily-v1 definition ID: " + value.DefinitionId, nameof(values));
                copy.Add(value.DefinitionId, value);
            }
        }
        return new ReadOnlyDictionary<string, T>(copy);
    }

    private static IEnumerable<TWrapper> Wrap<TValue, TWrapper>(IEnumerable<TValue> values,
        Func<TValue, TWrapper> create)
        where TValue : class where TWrapper : DailyV1DefinitionRecord
    {
        if (values == null) return null;
        List<TWrapper> result = new List<TWrapper>();
        foreach (TValue value in values)
        {
            if (value == null) { result.Add(null); continue; }
            result.Add(create(value));
        }
        return result;
    }

    private static IReadOnlyList<string> FreezeList(IEnumerable<string> values) => values == null
        ? Array.Empty<string>() : new ReadOnlyCollection<string>(new List<string>(values));

    private static IReadOnlyDictionary<string, DailyV1ContentRecord> FreezeContent(
        IEnumerable<KeyValuePair<string, DailyV1ContentRecord>> values)
    {
        SortedDictionary<string, DailyV1ContentRecord> copy = new SortedDictionary<string, DailyV1ContentRecord>(StringComparer.Ordinal);
        if (values != null)
            foreach (KeyValuePair<string, DailyV1ContentRecord> value in values)
            {
                if (string.IsNullOrWhiteSpace(value.Key) || value.Value == null)
                    throw new ArgumentException("Content projection rows require a key and copied record.", nameof(values));
                if (copy.ContainsKey(value.Key)) throw new ArgumentException("Duplicate content projection key: " + value.Key, nameof(values));
                copy.Add(value.Key, value.Value);
            }
        return new ReadOnlyDictionary<string, DailyV1ContentRecord>(copy);
    }

    private static string Need(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("A stable Daily-v1 identity value is required.", name) : value;
}

