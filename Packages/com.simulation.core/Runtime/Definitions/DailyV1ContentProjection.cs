using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>One selected serialized asset record using the source CLR tokens and field order.</summary>
internal sealed class DailyV1ContentRecord
{
    public string TypeToken { get; }
    public IReadOnlyList<DailyV1DefinitionField> Fields { get; }

    public DailyV1ContentRecord(string typeToken, IEnumerable<DailyV1DefinitionField> fields)
    {
        TypeToken = string.IsNullOrWhiteSpace(typeToken) ? throw new ArgumentException("Content type token is required.", nameof(typeToken)) : typeToken;
        Fields = fields == null ? throw new ArgumentNullException(nameof(fields))
            : new System.Collections.ObjectModel.ReadOnlyCollection<DailyV1DefinitionField>(new List<DailyV1DefinitionField>(fields));
    }
}

/// <summary>
/// The sole P12 selected-catalog encoder. It consumes captured compatibility tokens and never
/// asks the executing CLR to synthesize legacy Unity type identities.
/// </summary>
internal static class DailyV1ContentProjection
{
    public const string CatalogIdentity = "p12a.official-content-catalog/v1";
    public const int MaxCatalogAssets = 65536;
    public const int MaxCatalogBytes = 64 * 1024 * 1024;
    public const int MaxValueDepth = 64;

    internal static bool TryEncode(IReadOnlyDictionary<string, DailyV1ContentRecord> records,
        out byte[] bytes, out string diagnostic)
    {
        bytes = null;
        // The fixed profile is a catalog record but is not one of the selected assets.
        if (records == null || records.Count == 0 || records.Count - 1 > MaxCatalogAssets)
        { diagnostic = "The Daily-v1 content catalog has an invalid record count."; return false; }
        try
        {
            long exactLength = MeasureCatalog(records);
            using (MemoryStream stream = new BoundedMemoryStream(MaxCatalogBytes))
            using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
            {
                ContinuationWire.WriteString(writer, CatalogIdentity);
                writer.Write(records.Count);
                foreach (KeyValuePair<string, DailyV1ContentRecord> record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Key) || record.Value == null)
                        throw new InvalidOperationException("The Daily-v1 catalog contains an invalid record.");
                    ContinuationWire.WriteString(writer, record.Key);
                    byte[] payload = SerializeObject(record.Value);
                    writer.Write(payload.Length);
                    writer.Write(payload);
                }
                writer.Flush();
                if (stream.Length != exactLength)
                    throw new InvalidOperationException("The Daily-v1 content preflight length did not match its encoded bytes.");
                bytes = stream.ToArray();
            }
            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is EncoderFallbackException || exception is IOException)
        {
            bytes = null;
            diagnostic = exception.Message;
            return false;
        }
    }

    private static long MeasureCatalog(IReadOnlyDictionary<string, DailyV1ContentRecord> records)
    {
        ByteBudget budget = new ByteBudget(MaxCatalogBytes);
        budget.AddString(CatalogIdentity);
        budget.Add(4);
        foreach (KeyValuePair<string, DailyV1ContentRecord> record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Key) || record.Value == null)
                throw new InvalidOperationException("The Daily-v1 catalog contains an invalid record.");
            budget.AddString(record.Key);
            long payload = MeasureRecord(record.Value);
            if (payload > int.MaxValue) throw new InvalidOperationException("A Daily-v1 content definition exceeds its byte bound.");
            budget.Add(4);
            budget.Add(payload);
        }
        return budget.Total;
    }

    private static long MeasureRecord(DailyV1ContentRecord record)
    {
        ByteBudget budget = new ByteBudget(MaxCatalogBytes);
        budget.AddString(record.TypeToken);
        budget.Add(4);
        foreach (DailyV1DefinitionField field in record.Fields)
        {
            budget.AddString(field.DeclaringTypeToken);
            budget.AddString(field.Name);
            MeasureValue(field.Value, 0, budget);
        }
        return budget.Total;
    }

    private static void MeasureValue(DailyV1DefinitionValue value, int depth, ByteBudget budget)
    {
        if (depth > MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null || value.Kind == DailyV1DefinitionValue.ValueKind.Null) { budget.Add(1); return; }
        budget.AddString(value.TypeToken);
        switch (value.Kind)
        {
            case DailyV1DefinitionValue.ValueKind.Reference:
                budget.Add(1); budget.AddString(value.ReferenceKey); return;
            case DailyV1DefinitionValue.ValueKind.Scalar:
                MeasureScalar(value.Scalar, budget); return;
            case DailyV1DefinitionValue.ValueKind.Enum:
                budget.Add(1); budget.AddString(value.EnumSpelling); return;
            case DailyV1DefinitionValue.ValueKind.List:
                budget.Add(5);
                foreach (DailyV1DefinitionValue item in value.Items) MeasureValue(item, depth + 1, budget);
                return;
            case DailyV1DefinitionValue.ValueKind.Object:
                budget.Add(5);
                foreach (DailyV1DefinitionField field in value.Fields)
                {
                    budget.AddString(field.DeclaringTypeToken);
                    budget.AddString(field.Name);
                    MeasureValue(field.Value, depth + 1, budget);
                }
                return;
            default: throw new InvalidOperationException("Unsupported Daily-v1 content value kind.");
        }
    }

    private static void MeasureScalar(object value, ByteBudget budget)
    {
        if (value is string text) { budget.Add(1); budget.AddString(text); return; }
        if (value is bool || value is byte || value is sbyte) { budget.Add(2); return; }
        if (value is short || value is ushort || value is char) { budget.Add(3); return; }
        if (value is int || value is uint || value is float) { budget.Add(5); return; }
        if (value is long || value is ulong || value is double || value is DateTime || value is TimeSpan) { budget.Add(9); return; }
        if (value is decimal) { budget.Add(17); return; }
        if (value is Guid) { budget.Add(1); budget.AddString(((Guid)value).ToString("N")); return; }
        throw new InvalidOperationException("The Daily-v1 content catalog contains an unsupported scalar.");
    }

    private sealed class ByteBudget
    {
        private readonly long _maximum;
        public long Total { get; private set; }
        public ByteBudget(long maximum) { _maximum = maximum; }
        public void Add(long amount)
        {
            Total = checked(Total + amount);
            if (amount < 0 || Total > _maximum)
                throw new InvalidOperationException("The Daily-v1 content catalog exceeds its byte bound.");
        }
        public void AddString(string value)
        {
            if (value == null) { Add(4); return; }
            int bytes = ContinuationWire.CreateStrictUtf8Encoding().GetByteCount(value);
            if (bytes > ContinuationWire.MaxStringBytes)
                throw new InvalidOperationException("A Daily-v1 content string exceeds its byte bound.");
            Add(checked(4L + bytes));
        }
    }

    private sealed class BoundedMemoryStream : MemoryStream
    {
        private readonly long _maximum;
        public BoundedMemoryStream(long maximum) { _maximum = maximum; }
        private void EnsureWrite(long count)
        {
            if (count < 0 || checked(Position + count) > _maximum)
                throw new IOException("The Daily-v1 content catalog exceeds its byte bound.");
        }
        public override void Write(byte[] buffer, int offset, int count) { EnsureWrite(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { EnsureWrite(buffer.Length); base.Write(buffer); }
        public override void WriteByte(byte value) { EnsureWrite(1); base.WriteByte(value); }
        public override void SetLength(long value)
        {
            if (value < 0 || value > _maximum) throw new IOException("The Daily-v1 content catalog exceeds its byte bound.");
            base.SetLength(value);
        }
    }

    internal static string FingerprintRecords(IReadOnlyDictionary<string, DailyV1ContentRecord> records)
    {
        if (!TryEncode(records, out byte[] bytes, out string diagnostic))
            throw new InvalidOperationException(diagnostic ?? "The Daily-v1 content projection is invalid.");
        return FingerprintBytes(bytes);
    }

    internal static string FingerprintBytes(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
    }

    internal static byte[] SerializeObject(DailyV1ContentRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        using (MemoryStream stream = new BoundedMemoryStream(MaxCatalogBytes))
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            ContinuationWire.WriteString(writer, record.TypeToken);
            writer.Write(record.Fields.Count);
            foreach (DailyV1DefinitionField field in record.Fields)
            {
                ContinuationWire.WriteString(writer, field.DeclaringTypeToken);
                ContinuationWire.WriteString(writer, field.Name);
                WriteValue(writer, field.Value, 0);
            }
            writer.Flush();
            if (stream.Length > MaxCatalogBytes)
                throw new InvalidOperationException("A Daily-v1 content definition exceeds its byte bound.");
            return stream.ToArray();
        }
    }

    private static void WriteValue(BinaryWriter writer, DailyV1DefinitionValue value, int depth)
    {
        if (depth > MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null || value.Kind == DailyV1DefinitionValue.ValueKind.Null)
        { writer.Write((byte)0); return; }
        ContinuationWire.WriteString(writer, value.TypeToken);
        switch (value.Kind)
        {
            case DailyV1DefinitionValue.ValueKind.Reference:
                writer.Write((byte)1);
                ContinuationWire.WriteString(writer, value.ReferenceKey);
                return;
            case DailyV1DefinitionValue.ValueKind.Scalar:
                WriteScalar(writer, value.Scalar);
                return;
            case DailyV1DefinitionValue.ValueKind.Enum:
                writer.Write((byte)3);
                ContinuationWire.WriteString(writer, value.EnumSpelling);
                return;
            case DailyV1DefinitionValue.ValueKind.List:
                writer.Write((byte)20);
                writer.Write(value.Items.Count);
                foreach (DailyV1DefinitionValue item in value.Items) WriteValue(writer, item, depth + 1);
                return;
            case DailyV1DefinitionValue.ValueKind.Object:
                writer.Write((byte)21);
                writer.Write(value.Fields.Count);
                foreach (DailyV1DefinitionField field in value.Fields)
                {
                    ContinuationWire.WriteString(writer, field.DeclaringTypeToken);
                    ContinuationWire.WriteString(writer, field.Name);
                    WriteValue(writer, field.Value, depth + 1);
                }
                return;
            default:
                throw new InvalidOperationException("Unsupported Daily-v1 content value kind.");
        }
    }

    private static void WriteScalar(BinaryWriter writer, object value)
    {
        if (value is string text) { writer.Write((byte)2); ContinuationWire.WriteString(writer, text); return; }
        if (value is bool boolean) { writer.Write((byte)4); writer.Write(boolean); return; }
        if (value is byte byteValue) { writer.Write((byte)5); writer.Write(byteValue); return; }
        if (value is sbyte sbyteValue) { writer.Write((byte)6); writer.Write(sbyteValue); return; }
        if (value is short shortValue) { writer.Write((byte)7); writer.Write(shortValue); return; }
        if (value is ushort ushortValue) { writer.Write((byte)8); writer.Write(ushortValue); return; }
        if (value is int intValue) { writer.Write((byte)9); writer.Write(intValue); return; }
        if (value is uint uintValue) { writer.Write((byte)10); writer.Write(uintValue); return; }
        if (value is long longValue) { writer.Write((byte)11); writer.Write(longValue); return; }
        if (value is ulong ulongValue) { writer.Write((byte)12); writer.Write(ulongValue); return; }
        if (value is char charValue) { writer.Write((byte)13); writer.Write((ushort)charValue); return; }
        if (value is float singleValue) { writer.Write((byte)14); writer.Write(BitConverter.ToInt32(BitConverter.GetBytes(singleValue), 0)); return; }
        if (value is double doubleValue) { writer.Write((byte)15); writer.Write(BitConverter.ToInt64(BitConverter.GetBytes(doubleValue), 0)); return; }
        if (value is decimal decimalValue)
        { writer.Write((byte)16); foreach (int part in decimal.GetBits(decimalValue)) writer.Write(part); return; }
        if (value is Guid guid) { writer.Write((byte)17); ContinuationWire.WriteString(writer, guid.ToString("N")); return; }
        if (value is DateTime dateTime) { writer.Write((byte)18); writer.Write(dateTime.ToBinary()); return; }
        if (value is TimeSpan timeSpan) { writer.Write((byte)19); writer.Write(timeSpan.Ticks); return; }
        throw new InvalidOperationException("The Daily-v1 content catalog contains an unsupported scalar.");
    }

    private static string Hex(byte[] bytes)
    {
        char[] chars = new char[bytes.Length * 2];
        const string digits = "0123456789abcdef";
        for (int i = 0; i < bytes.Length; i++)
        { chars[i * 2] = digits[bytes[i] >> 4]; chars[i * 2 + 1] = digits[bytes[i] & 15]; }
        return new string(chars);
    }
}

/// <summary>A value captured by the Unity Daily-v1 schema adapter.</summary>
public sealed class DailyV1SelectedContentValue
{
    internal enum ContentKind { Null, Scalar, Enum, Reference, List, Object }
    internal ContentKind Kind { get; }
    internal string TypeToken { get; }
    internal object ScalarValue { get; }
    internal string Text { get; }
    internal IReadOnlyList<DailyV1SelectedContentValue> Items { get; }
    internal IReadOnlyList<DailyV1SelectedContentField> Fields { get; }

    private DailyV1SelectedContentValue(ContentKind kind, string typeToken, object scalar,
        string text, IEnumerable<DailyV1SelectedContentValue> items,
        IEnumerable<DailyV1SelectedContentField> fields)
    {
        Kind = kind;
        TypeToken = typeToken;
        ScalarValue = scalar;
        Text = text;
        Items = items == null ? Array.Empty<DailyV1SelectedContentValue>()
            : new System.Collections.ObjectModel.ReadOnlyCollection<DailyV1SelectedContentValue>(new List<DailyV1SelectedContentValue>(items));
        Fields = fields == null ? Array.Empty<DailyV1SelectedContentField>()
            : new System.Collections.ObjectModel.ReadOnlyCollection<DailyV1SelectedContentField>(new List<DailyV1SelectedContentField>(fields));
    }

    public static DailyV1SelectedContentValue Null(string typeToken) =>
        new DailyV1SelectedContentValue(ContentKind.Null, Need(typeToken, nameof(typeToken)), null, null, null, null);
    public static DailyV1SelectedContentValue Scalar(string typeToken, object value) =>
        new DailyV1SelectedContentValue(ContentKind.Scalar, Need(typeToken, nameof(typeToken)),
            value ?? throw new ArgumentNullException(nameof(value)), null, null, null);
    public static DailyV1SelectedContentValue Enum(string typeToken, string spelling) =>
        new DailyV1SelectedContentValue(ContentKind.Enum, Need(typeToken, nameof(typeToken)), null,
            spelling ?? string.Empty, null, null);
    public static DailyV1SelectedContentValue Reference(string typeToken, string referenceKey) =>
        new DailyV1SelectedContentValue(ContentKind.Reference, Need(typeToken, nameof(typeToken)), null,
            Need(referenceKey, nameof(referenceKey)), null, null);
    public static DailyV1SelectedContentValue List(string typeToken, IEnumerable<DailyV1SelectedContentValue> values) =>
        new DailyV1SelectedContentValue(ContentKind.List, Need(typeToken, nameof(typeToken)), null, null,
            values ?? throw new ArgumentNullException(nameof(values)), null);
    public static DailyV1SelectedContentValue Object(string typeToken, IEnumerable<DailyV1SelectedContentField> fields) =>
        new DailyV1SelectedContentValue(ContentKind.Object, Need(typeToken, nameof(typeToken)), null, null, null,
            fields ?? throw new ArgumentNullException(nameof(fields)));

    private static string Need(string value, string parameter) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("A captured Daily-v1 token is required.", parameter) : value;
}

/// <summary>A captured field. Core validates its token/name pair against the closed source schema.</summary>
public sealed class DailyV1SelectedContentField
{
    public string DeclaringTypeToken { get; }
    public string Name { get; }
    public DailyV1SelectedContentValue Value { get; }

    public DailyV1SelectedContentField(string declaringTypeToken, string name, DailyV1SelectedContentValue value)
    {
        DeclaringTypeToken = Need(declaringTypeToken, nameof(declaringTypeToken));
        Name = Need(name, nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static string Need(string value, string parameter) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("A captured Daily-v1 field token is required.", parameter) : value;
}

/// <summary>A captured selected Daily-v1 asset record, keyed by its legacy Unity identity.</summary>
public sealed class DailyV1SelectedContentAsset
{
    public const string ProfileKey = "profile-config/SimulationConfigData";
    public string Key { get; }
    public string TypeToken { get; }
    public IReadOnlyList<DailyV1SelectedContentField> Fields { get; }

    public DailyV1SelectedContentAsset(string key, string typeToken,
        IEnumerable<DailyV1SelectedContentField> fields)
    {
        Key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("A catalog key is required.", nameof(key)) : key;
        TypeToken = string.IsNullOrWhiteSpace(typeToken) ? throw new ArgumentException("A type token is required.", nameof(typeToken)) : typeToken;
        Fields = fields == null ? throw new ArgumentNullException(nameof(fields))
            : new System.Collections.ObjectModel.ReadOnlyCollection<DailyV1SelectedContentField>(new List<DailyV1SelectedContentField>(fields));
    }
}

/// <summary>Immutable bytes and identity produced by the sole Daily-v1 content encoder.</summary>
public sealed class DailyV1SelectedContentProjection
{
    private readonly byte[] bytes;
    public string Fingerprint { get; }
    public byte[] Bytes => (byte[])bytes.Clone();

    internal DailyV1SelectedContentProjection(string fingerprint, byte[] bytes)
    {
        Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
        this.bytes = bytes == null ? throw new ArgumentNullException(nameof(bytes)) : (byte[])bytes.Clone();
    }
}

/// <summary>
/// Closed factory for the serialized fields of the selected Daily-v1 profile. The DTOs are
/// transport values only: every type, member, scalar kind and reference key is checked against
/// the source member map and captured Unity token table before reaching the internal encoder.
/// </summary>
public static class DailyV1SelectedContentProjectionFactory
{
    /// <summary>The selected compatibility catalog byte limit, shared with host capture guards.</summary>
    public const int MaxCatalogBytes = DailyV1ContentProjection.MaxCatalogBytes;
    private const string Schema =
        "SimulationConfigData|simulationName|string\n" +
        "SimulationConfigData|calendar|CalendarDefinition\n" +
        "SimulationConfigData|enabledModules|List<SimulationModule>\n" +
        "SimulationConfigData|cities|List<CityData>\n" +
        "SimulationConfigData|explorableSites|List<ExplorableSiteConfig>\n" +
        "SimulationConfigData|npcs|List<NpcSimulationConfig>\n" +
        "SimulationConfigData|actions|List<NpcActionData>\n" +
        "SimulationConfigData|statuses|List<NpcStatusData>\n" +
        "SimulationConfigData|jobs|List<NpcJobData>\n" +
        "SimulationConfigData|initialWarrants|List<InitialWantedRecordConfig>\n" +
        "SimulationConfigData|scheduledDirectives|List<ScheduledDirectiveConfig>\n" +
        "SimulationConfigData|useAuthoredGeographyProfile|bool\n" +
        "SimulationConfigData|authoredHexId|string\n" +
        "SimulationConfigData|authoredHexQ|int\n" +
        "SimulationConfigData|authoredHexR|int\n" +
        "SimulationConfigData|authoredTerrainDefinitionId|string\n" +
        "SimulationConfigData|authoredTerrainRevisionToken|string\n" +
        "SimulationConfigData|authoredLocationId|string\n" +
        "SimulationConfigData|authoredScaleConventionId|string\n" +
        "SimulationConfigData|authoredScaleSourceIdentity|string\n" +
        "SimulationConfigData|authoredScaleSourceVersion|string\n" +
        "SimulationConfigData|authoredDistancePerNeighborStep|string\n" +
        "SimulationConfigData|authoredScaleUnit|string\n" +
        "SimulationConfigData|authoredP10RuinSite|ExplorableSiteData\n" +
        "SimulationConfigData|genesisProfileContractIdentity|string\n" +
        "SimulationConfigData|p10bStableSiteKey|string\n" +
        "SimulationConfigData|travelCostPerDay|float\n" +
        "SimulationConfigData|allowMerchantTradeRepositioning|bool\n" +
        "SimulationConfigData|maxMerchantTradeAmount|int\n" +
        "SimulationConfigData|localMerchantWholesalePriceMultiplier|float\n" +
        "SimulationConfigData|localMerchantReserveRatio|float\n" +
        "SimulationConfigData|maxUnprofitablePlanWaitDays|int\n" +
        "SimulationConfigData|crimeAutonomousEnabled|bool\n" +
        "SimulationConfigData|commercialKnowledge|CommercialKnowledgeSettings\n" +
        "SimulationConfigData|useFixedSimulationSeed|bool\n" +
        "SimulationConfigData|simulationSeed|int\n" +
        "SimulationConfigData|includeEconomySnapshots|bool\n" +
        "SimulationConfigData|economySnapshotIntervalDays|int\n" +
        "SimulationConfigData|logSettings|SimulationLogSettings\n" +
        "SimulationConfigData|naturalMortalityEnabled|bool\n" +
        "SimulationConfigData|naturalMortalityAnnualProbability|double\n" +
        "SimulationConfigData|aggregateDemographyEnabled|bool\n" +
        "SimulationConfigData|aggregateAnnualBirthRate|double\n" +
        "SimulationConfigData|aggregateAnnualDeathRate|double\n" +
        "SimulationConfigData|freeStatus|NpcStatusData\n" +
        "SimulationConfigData|wantedStatus|NpcStatusData\n" +
        "SimulationConfigData|arrestedStatus|NpcStatusData\n" +
        "SimulationConfigData|hiddenStatus|NpcStatusData\n" +
        "ExplorableSiteConfig|site|ExplorableSiteData\n" +
        "ExplorableSiteConfig|anchorCity|CityData\n" +
        "ExplorableSiteConfig|travelDaysFromAnchor|int\n" +
        "NpcSimulationConfig|npc|NpcData\n" +
        "NpcSimulationConfig|startingCity|CityData\n" +
        "NpcSimulationConfig|initialMoney|float\n" +
        "NpcSimulationConfig|initialInventory|List<NpcInitialInventoryItemConfig>\n" +
        "NpcSimulationConfig|initialKnownExplorableSites|List<ExplorableSiteData>\n" +
        "NpcInitialInventoryItemConfig|item|ItemData\n" +
        "NpcInitialInventoryItemConfig|amount|int\n" +
        "NpcInitialInventoryItemConfig|averageUnitCost|float\n" +
        "InitialWantedRecordConfig|target|NpcData\n" +
        "InitialWantedRecordConfig|city|CityData\n" +
        "InitialWantedRecordConfig|bounty|float\n" +
        "InitialWantedRecordConfig|sentenceDays|int\n" +
        "ScheduledDirectiveConfig|absoluteDay|long\n" +
        "ScheduledDirectiveConfig|mode|ScheduledDirectiveMode\n" +
        "ScheduledDirectiveConfig|operation|ScheduledDirectiveOperation\n" +
        "ScheduledDirectiveConfig|actor|NpcData\n" +
        "ScheduledDirectiveConfig|action|NpcActionData\n" +
        "CityData|id|string\n" +
        "CityData|cityName|string\n" +
        "CityData|settlementSemanticId|string\n" +
        "CityData|materialFlowLocationId|string\n" +
        "CityData|marketStoreSemanticId|string\n" +
        "CityData|materialFlowProfile|LocalMaterialFlowProfile\n" +
        "CityData|initialPopulation|int\n" +
        "CityData|marketLiquidity|MarketLiquidityConfig\n" +
        "CityData|populationConsumption|PopulationConsumptionConfig\n" +
        "CityData|marketItems|List<MarketItemConfig>\n" +
        "CityData|productionConfigs|List<CityProductionConfig>\n" +
        "CityData|connections|List<CityConnection>\n" +
        "MarketLiquidityConfig|liquidityMode|MarketLiquidityMode\n" +
        "MarketLiquidityConfig|initialPurchasingPower|float\n" +
        "PopulationConsumptionConfig|paymentMode|ConsumptionPaymentMode\n" +
        "PopulationConsumptionConfig|initialPurchasingPower|float\n" +
        "MarketItemConfig|item|ItemData\n" +
        "MarketItemConfig|initialAmount|int\n" +
        "MarketItemConfig|desiredAmount|int\n" +
        "MarketItemConfig|consumptionPer1000Population|float\n" +
        "CityProductionConfig|item|ItemData\n" +
        "CityProductionConfig|amountPerDay|int\n" +
        "CityProductionConfig|initialReserve|int\n" +
        "CityProductionConfig|productionSourceId|string\n" +
        "CityProductionConfig|contentRevision|string\n" +
        "CityProductionConfig|sourceKind|CityProductionSourceKind\n" +
        "CityConnection|destination|CityData\n" +
        "CityConnection|travelDays|int\n" +
        "ItemData|id|string\n" +
        "ItemData|itemName|string\n" +
        "ItemData|basePrice|float\n" +
        "ItemData|capabilityModifiers|List<CapabilityAttributeModifier>\n" +
        "NpcData|id|string\n" +
        "NpcData|name|string\n" +
        "NpcData|job|NpcJobData\n" +
        "NpcData|acoesPadrao|List<NPCDefaultAction>\n" +
        "NpcData|statusPadrao|List<NpcStatusData>\n" +
        "NpcData|capabilityValues|List<CapabilityAttributeValue>\n" +
        "NpcData|traits|List<TraitData>\n" +
        "NPCDefaultAction|action|NpcActionData\n" +
        "NPCDefaultAction|baseUtility|float\n" +
        "NpcJobData|id|string\n" +
        "NpcJobData|jobName|string\n" +
        "NpcJobData|jobType|NpcJobType\n" +
        "NpcJobData|merchantBehavior|MerchantBehavior\n" +
        "NpcJobData|minimumProfitPerItem|float\n" +
        "NpcJobData|workAction|NpcActionData\n" +
        "NpcJobData|workUtility|float\n" +
        "NpcJobData|preferredTradeItems|List<TradeItemPreference>\n" +
        "TradeItemPreference|item|ItemData\n" +
        "TradeItemPreference|utilityMultiplier|float\n" +
        "NpcActionData|id|string\n" +
        "NpcActionData|statusNecessariosParaFazerAcao|List<NpcStatusData>\n" +
        "NpcActionData|actionName|string\n" +
        "NpcActionData|normalActionLogText|string\n" +
        "NpcActionData|actionCategory|NpcActionCategory\n" +
        "NpcActionData|actionType|NpcActionType\n" +
        "NpcActionData|baseUtility|float\n" +
        "NpcActionData|statusModifiers|List<StatusWeightModifier>\n" +
        "NpcActionData|canFail|bool\n" +
        "NpcActionData|baseSuccessChance|float\n" +
        "NpcActionData|statusToAdd|List<NpcStatusData>\n" +
        "NpcActionData|statusToRemove|List<NpcStatusData>\n" +
        "NpcActionData|targetStatusToAdd|List<NpcStatusData>\n" +
        "NpcActionData|targetStatusToRemove|List<NpcStatusData>\n" +
        "NpcActionData|crimeSettings|CrimeActionSettings\n" +
        "CrimeActionSettings|amount|int\n" +
        "CrimeActionSettings|bounty|float\n" +
        "CrimeActionSettings|sentenceDays|int\n" +
        "CrimeActionSettings|hiddenDays|int\n" +
        "CrimeActionSettings|escapeBountyPenalty|float\n" +
        "CrimeActionSettings|failedEscapeSentencePenalty|int\n" +
        "StatusWeightModifier|status|NpcStatusData\n" +
        "StatusWeightModifier|multiplier|float\n" +
        "NpcStatusData|id|string\n" +
        "NpcStatusData|statusName|string\n" +
        "CommercialKnowledgeSettings|freshForDays|int\n" +
        "CommercialKnowledgeSettings|maxUsefulAgeDays|int\n" +
        "CommercialKnowledgeSettings|maxSharedObservationsPerInteraction|int\n" +
        "SimulationLogSettings|showDay|bool\n" +
        "SimulationLogSettings|npcActions|bool\n" +
        "SimulationLogSettings|trade|bool\n" +
        "SimulationLogSettings|travel|bool\n" +
        "SimulationLogSettings|crime|bool\n" +
        "SimulationLogSettings|justice|bool\n" +
        "SimulationLogSettings|economyProduction|bool\n" +
        "SimulationLogSettings|economyConsumption|bool\n" +
        "SimulationLogSettings|market|bool\n" +
        "CapabilityAttributeValue|attribute|CapabilityAttributeData\n" +
        "CapabilityAttributeValue|value|float\n" +
        "CapabilityAttributeModifier|attribute|CapabilityAttributeData\n" +
        "CapabilityAttributeModifier|additiveValue|float\n" +
        "CalendarDefinition|monthsPerYear|int\n" +
        "CalendarDefinition|weeksPerMonth|int\n" +
        "CalendarDefinition|daysPerWeek|int\n" +
        "CalendarDefinition|monthLengths|List<int>\n";

    private static readonly Dictionary<string, Dictionary<string, string>> FieldTypes = CreateFieldTypes();
    private static readonly HashSet<string> EnumTypes = new HashSet<string>(new[]
    {
        "CityProductionSourceKind", "ConsumptionPaymentMode", "LocalMaterialFlowProfile",
        "MarketLiquidityMode", "MerchantBehavior", "NpcActionCategory", "NpcActionType",
        "NpcJobType", "ScheduledDirectiveMode", "ScheduledDirectiveOperation", "SimulationModule"
    }, StringComparer.Ordinal);
    private static readonly HashSet<string> AssetTypes = new HashSet<string>(new[]
    {
        "CityData", "ItemData", "NpcData", "NpcJobData", "NpcActionData", "NpcStatusData"
    }, StringComparer.Ordinal);
    private static readonly HashSet<string> ReferenceTypes = new HashSet<string>(new[]
    {
        "CityData", "ItemData", "NpcData", "NpcJobData", "NpcActionData", "NpcStatusData",
        "CapabilityAttributeData", "ExplorableSiteData", "TraitData"
    }, StringComparer.Ordinal);

    public static bool TryCreate(IEnumerable<DailyV1SelectedContentAsset> assets,
        DailyV1SelectedContentAsset profile, out DailyV1SelectedContentProjection projection,
        out string diagnostic)
    {
        projection = null;
        diagnostic = null;
        try
        {
            Dictionary<string, DailyV1SelectedContentAsset> records = new Dictionary<string, DailyV1SelectedContentAsset>(StringComparer.Ordinal);
            if (profile == null || !string.Equals(profile.Key, DailyV1SelectedContentAsset.ProfileKey, StringComparison.Ordinal)
                || !string.Equals(profile.TypeToken, "SimulationConfigData", StringComparison.Ordinal))
                throw new InvalidOperationException("The selected Daily-v1 content profile record is invalid.");
            records.Add(profile.Key, profile);
            if (assets == null) throw new ArgumentNullException(nameof(assets));
            foreach (DailyV1SelectedContentAsset asset in assets)
            {
                if (asset == null || !AssetTypes.Contains(asset.TypeToken) || !HasAssetKeyPrefix(asset.Key, asset.TypeToken))
                    throw new InvalidOperationException("The selected Daily-v1 content graph contains an unsupported asset record.");
                if (records.ContainsKey(asset.Key))
                    throw new InvalidOperationException("The selected Daily-v1 content graph contains a duplicate stable key.");
                records.Add(asset.Key, asset);
                if (records.Count - 1 > DailyV1ContentProjection.MaxCatalogAssets)
                    throw new InvalidOperationException("The Daily-v1 content catalog exceeds its asset bound.");
            }

            PreflightSelectedCatalog(records);

            SortedDictionary<string, DailyV1ContentRecord> copied = new SortedDictionary<string, DailyV1ContentRecord>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, DailyV1SelectedContentAsset> record in records)
            {
                List<DailyV1DefinitionField> fields = new List<DailyV1DefinitionField>();
                ValidateFields(record.Value.TypeToken, record.Value.Fields, fields, records, -1);
                copied.Add(record.Key, new DailyV1ContentRecord(record.Value.TypeToken, fields));
            }
            Dictionary<string, HashSet<string>> graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, DailyV1ContentRecord> record in copied)
            {
                HashSet<string> targets = new HashSet<string>(StringComparer.Ordinal);
                foreach (DailyV1DefinitionField field in record.Value.Fields)
                    CollectReferences(field.Value, targets, records, 0);
                graph.Add(record.Key, targets);
            }
            HashSet<string> reachable = new HashSet<string>(StringComparer.Ordinal);
            Queue<string> pending = new Queue<string>();
            pending.Enqueue(DailyV1SelectedContentAsset.ProfileKey);
            while (pending.Count != 0)
            {
                string key = pending.Dequeue();
                if (!reachable.Add(key)) continue;
                if (!graph.TryGetValue(key, out HashSet<string> targets))
                    throw new InvalidOperationException("The selected Daily-v1 content graph has an unresolved reference.");
                foreach (string target in targets) pending.Enqueue(target);
            }
            if (reachable.Count != copied.Count)
                throw new InvalidOperationException("The selected Daily-v1 content projection contains an asset outside the profile reference graph.");

            if (!DailyV1ContentProjection.TryEncode(copied, out byte[] bytes, out diagnostic)) return false;
            projection = new DailyV1SelectedContentProjection(DailyV1ContentProjection.FingerprintBytes(bytes), bytes);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is System.Text.EncoderFallbackException || exception is IOException)
        {
            projection = null;
            diagnostic = exception.Message;
            return false;
        }
    }

    // Count the exact P12 wire bytes before copying the caller's immutable DTO graph into
    // encoder nodes. Every string is strict UTF-8 and every recursive edge is depth checked.
    private static void PreflightSelectedCatalog(Dictionary<string, DailyV1SelectedContentAsset> records)
    {
        long total = 0;
        AddPreflightString(ref total, DailyV1ContentProjection.CatalogIdentity);
        AddPreflight(ref total, 4);
        foreach (KeyValuePair<string, DailyV1SelectedContentAsset> record in records)
        {
            AddPreflightString(ref total, record.Key);
            long payload = 0;
            AddPreflightString(ref payload, record.Value.TypeToken);
            AddPreflight(ref payload, 4);
            foreach (DailyV1SelectedContentField field in record.Value.Fields)
            {
                if (field == null) throw new InvalidOperationException("The selected Daily-v1 object contains a null field.");
                AddPreflightString(ref payload, field.DeclaringTypeToken);
                AddPreflightString(ref payload, field.Name);
                MeasureSelectedValue(field.Value, 0, ref payload);
            }
            AddPreflight(ref total, 4);
            AddPreflight(ref total, payload);
        }
    }

    private static void MeasureSelectedValue(DailyV1SelectedContentValue value, int depth, ref long total)
    {
        if (depth > DailyV1ContentProjection.MaxValueDepth)
            throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null || value.Kind == DailyV1SelectedContentValue.ContentKind.Null)
        { AddPreflight(ref total, 1); return; }
        AddPreflightString(ref total, value.TypeToken);
        switch (value.Kind)
        {
            case DailyV1SelectedContentValue.ContentKind.Reference:
                AddPreflight(ref total, 1); AddPreflightString(ref total, value.Text); return;
            case DailyV1SelectedContentValue.ContentKind.Enum:
                AddPreflight(ref total, 1); AddPreflightString(ref total, value.Text); return;
            case DailyV1SelectedContentValue.ContentKind.Scalar:
                MeasureSelectedScalar(value.ScalarValue, ref total); return;
            case DailyV1SelectedContentValue.ContentKind.List:
                AddPreflight(ref total, 5);
                foreach (DailyV1SelectedContentValue item in value.Items)
                    MeasureSelectedValue(item, depth + 1, ref total);
                return;
            case DailyV1SelectedContentValue.ContentKind.Object:
                AddPreflight(ref total, 5);
                foreach (DailyV1SelectedContentField field in value.Fields)
                {
                    if (field == null) throw new InvalidOperationException("The selected Daily-v1 object contains a null field.");
                    AddPreflightString(ref total, field.DeclaringTypeToken);
                    AddPreflightString(ref total, field.Name);
                    MeasureSelectedValue(field.Value, depth + 1, ref total);
                }
                return;
            default: throw new InvalidOperationException("The selected Daily-v1 value tag is unsupported.");
        }
    }

    private static void MeasureSelectedScalar(object value, ref long total)
    {
        if (value is string text) { AddPreflight(ref total, 1); AddPreflightString(ref total, text); return; }
        if (value is bool || value is byte || value is sbyte) { AddPreflight(ref total, 2); return; }
        if (value is short || value is ushort || value is char) { AddPreflight(ref total, 3); return; }
        if (value is int || value is uint || value is float) { AddPreflight(ref total, 5); return; }
        if (value is long || value is ulong || value is double || value is DateTime || value is TimeSpan) { AddPreflight(ref total, 9); return; }
        if (value is decimal) { AddPreflight(ref total, 17); return; }
        if (value is Guid) { AddPreflight(ref total, 1); AddPreflightString(ref total, ((Guid)value).ToString("N")); return; }
        throw new InvalidOperationException("The Daily-v1 content catalog contains an unsupported scalar.");
    }

    private static void AddPreflightString(ref long total, string value)
    {
        int bytes = value == null ? 0 : ContinuationWire.CreateStrictUtf8Encoding().GetByteCount(value);
        if (bytes > ContinuationWire.MaxStringBytes)
            throw new InvalidOperationException("A Daily-v1 content string exceeds its byte bound.");
        AddPreflight(ref total, checked(4L + bytes));
    }

    private static void AddPreflight(ref long total, long amount)
    {
        total = checked(total + amount);
        if (amount < 0 || total > DailyV1ContentProjection.MaxCatalogBytes)
            throw new InvalidOperationException("The Daily-v1 content catalog exceeds its byte bound.");
    }

    private static Dictionary<string, Dictionary<string, string>> CreateFieldTypes()
    {
        Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        int count = 0;
        foreach (string row in Schema.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = row.Split('|');
            if (parts.Length != 3) throw new InvalidOperationException("The embedded selected Daily-v1 schema is malformed.");
            if (!result.TryGetValue(parts[0], out Dictionary<string, string> members))
                result.Add(parts[0], members = new Dictionary<string, string>(StringComparer.Ordinal));
            if (members.ContainsKey(parts[1])) throw new InvalidOperationException("The embedded selected Daily-v1 schema has duplicate members.");
            members.Add(parts[1], parts[2]);
            count++;
        }
        if (count != 164) throw new InvalidOperationException("The selected Daily-v1 source member schema changed.");
        return result;
    }

    private static void ValidateFields(string objectType, IReadOnlyList<DailyV1SelectedContentField> input,
        List<DailyV1DefinitionField> output, Dictionary<string, DailyV1SelectedContentAsset> records, int depth)
    {
        if (depth > DailyV1ContentProjection.MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (!FieldTypes.TryGetValue(objectType, out Dictionary<string, string> schema))
            throw new InvalidOperationException("The selected Daily-v1 schema contains an unsupported object type: " + objectType);
        if (input == null || input.Count != schema.Count)
            throw new InvalidOperationException("A selected Daily-v1 serialized object is missing or has extra fields: " + objectType);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1SelectedContentField field in input)
        {
            if (field == null || !string.Equals(field.DeclaringTypeToken, objectType, StringComparison.Ordinal)
                || !schema.TryGetValue(field.Name, out string sourceType) || !seen.Add(field.Name))
                throw new InvalidOperationException("The selected Daily-v1 serialized field is not in the approved source schema.");
            ValidateValue(field.Value, sourceType, records, depth + 1);
            output.Add(new DailyV1DefinitionField(field.DeclaringTypeToken, field.Name, CopyValue(field.Value, records, depth + 1)));
        }
        output.Sort((left, right) =>
        {
            int compare = StringComparer.Ordinal.Compare(left.DeclaringTypeToken, right.DeclaringTypeToken);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Name, right.Name);
        });
    }

    private static void ValidateValue(DailyV1SelectedContentValue value, string sourceType,
        Dictionary<string, DailyV1SelectedContentAsset> records, int depth)
    {
        if (depth > DailyV1ContentProjection.MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        string expectedToken = TypeToken(sourceType);
        if (value == null || !string.Equals(value.TypeToken, expectedToken, StringComparison.Ordinal))
            throw new InvalidOperationException("A selected Daily-v1 value has a type token outside its declared source field.");
        if (value.Kind == DailyV1SelectedContentValue.ContentKind.Null) return;
        if (sourceType.StartsWith("List<", StringComparison.Ordinal) && sourceType.EndsWith(">", StringComparison.Ordinal))
        {
            if (value.Kind != DailyV1SelectedContentValue.ContentKind.List)
                throw new InvalidOperationException("A selected Daily-v1 collection has an invalid shape.");
            string elementType = sourceType.Substring(5, sourceType.Length - 6);
            foreach (DailyV1SelectedContentValue item in value.Items) ValidateValue(item, elementType, records, depth + 1);
            return;
        }
        if (EnumTypes.Contains(sourceType))
        {
            if (value.Kind != DailyV1SelectedContentValue.ContentKind.Enum)
                throw new InvalidOperationException("A selected Daily-v1 enum has an invalid serialized tag.");
            return;
        }
        if (ReferenceTypes.Contains(sourceType))
        {
            if (value.Kind != DailyV1SelectedContentValue.ContentKind.Reference)
                throw new InvalidOperationException("A selected Daily-v1 asset reference has an invalid serialized tag.");
            string prefix = sourceType + "/";
            if (!value.Text.StartsWith(prefix, StringComparison.Ordinal) || value.Text.Length == prefix.Length
                || !records.ContainsKey(value.Text))
                throw new InvalidOperationException("A selected Daily-v1 reference is unsupported or unresolved.");
            return;
        }
        if (IsScalar(sourceType))
        {
            if (value.Kind != DailyV1SelectedContentValue.ContentKind.Scalar || !ScalarMatches(sourceType, value.ScalarValue))
                throw new InvalidOperationException("A selected Daily-v1 scalar has an invalid runtime value or serialized tag.");
            return;
        }
        if (value.Kind != DailyV1SelectedContentValue.ContentKind.Object)
            throw new InvalidOperationException("A selected Daily-v1 inline value has an invalid serialized tag.");
        ValidateFields(sourceType, value.Fields, new List<DailyV1DefinitionField>(), records, depth);
    }

    private static DailyV1DefinitionValue CopyValue(DailyV1SelectedContentValue value,
        Dictionary<string, DailyV1SelectedContentAsset> records, int depth)
    {
        if (depth > DailyV1ContentProjection.MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        switch (value.Kind)
        {
            case DailyV1SelectedContentValue.ContentKind.Null:
                return DailyV1DefinitionValue.Null(value.TypeToken);
            case DailyV1SelectedContentValue.ContentKind.Scalar:
                return DailyV1DefinitionValue.ScalarValue(value.TypeToken, value.ScalarValue);
            case DailyV1SelectedContentValue.ContentKind.Enum:
                return DailyV1DefinitionValue.Enum(value.TypeToken, 0, value.Text);
            case DailyV1SelectedContentValue.ContentKind.Reference:
                return DailyV1DefinitionValue.Reference(value.TypeToken, value.Text.Substring(value.TypeToken.Length + 1), value.Text);
            case DailyV1SelectedContentValue.ContentKind.List:
                List<DailyV1DefinitionValue> items = new List<DailyV1DefinitionValue>(value.Items.Count);
                foreach (DailyV1SelectedContentValue item in value.Items) items.Add(CopyValue(item, records, depth + 1));
                return DailyV1DefinitionValue.List(value.TypeToken, items);
            case DailyV1SelectedContentValue.ContentKind.Object:
                List<DailyV1DefinitionField> fields = new List<DailyV1DefinitionField>();
                ValidateFields(value.TypeToken, value.Fields, fields, records, depth);
                return DailyV1DefinitionValue.Object(value.TypeToken, fields);
            default:
                throw new InvalidOperationException("The selected Daily-v1 value tag is unsupported.");
        }
    }

    private static void CollectReferences(DailyV1DefinitionValue value, HashSet<string> targets,
        Dictionary<string, DailyV1SelectedContentAsset> records, int depth)
    {
        if (depth > DailyV1ContentProjection.MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null) return;
        if (value.Kind == DailyV1DefinitionValue.ValueKind.Reference)
        {
            if (!records.ContainsKey(value.ReferenceKey))
                throw new InvalidOperationException("The selected Daily-v1 content graph has an unresolved reference.");
            targets.Add(value.ReferenceKey);
        }
        foreach (DailyV1DefinitionValue item in value.Items) CollectReferences(item, targets, records, depth + 1);
        foreach (DailyV1DefinitionField field in value.Fields) CollectReferences(field.Value, targets, records, depth + 1);
    }

    private static string TypeToken(string sourceType)
    {
        switch (sourceType)
        {
            case "bool": return "System.Boolean";
            case "int": return "System.Int32";
            case "long": return "System.Int64";
            case "float": return "System.Single";
            case "double": return "System.Double";
            case "string": return "System.String";
        }
        if (sourceType.StartsWith("List<", StringComparison.Ordinal) && sourceType.EndsWith(">", StringComparison.Ordinal))
        {
            string element = sourceType.Substring(5, sourceType.Length - 6);
            string elementToken = TypeToken(element);
            string assembly = element == "int"
                ? "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                : "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            return "System.Collections.Generic.List`1[[" + elementToken + ", " + assembly + "]]";
        }
        return sourceType;
    }

    private static bool IsScalar(string sourceType) => sourceType == "bool" || sourceType == "int"
        || sourceType == "long" || sourceType == "float" || sourceType == "double" || sourceType == "string";

    private static bool ScalarMatches(string sourceType, object value)
    {
        if (sourceType == "bool") return value is bool;
        if (sourceType == "int") return value is int;
        if (sourceType == "long") return value is long;
        if (sourceType == "float") return value is float;
        if (sourceType == "double") return value is double;
        return sourceType == "string" && value is string;
    }

    private static bool HasAssetKeyPrefix(string key, string typeToken) => key != null
        && key.StartsWith(typeToken + "/", StringComparison.Ordinal) && key.Length > typeToken.Length + 1;
}
