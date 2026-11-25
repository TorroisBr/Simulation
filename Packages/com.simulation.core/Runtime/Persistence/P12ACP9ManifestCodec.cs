using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit v1 codec for the immutable P9 genesis lineage snapshot.</summary>
internal static class P12ACP9ManifestCodec
{
    internal static bool TryEncode(P12CP9GenesisManifestCoreSnapshot snapshot, out byte[] bytes, out P12AResult result)
    {
        bytes = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p9.genesis-manifest");
        if (snapshot == null || !snapshot.TryValidate(out _)) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                WriteString(writer, snapshot.SnapshotContract); writer.Write(snapshot.SnapshotSchemaVersion);
                WriteEnum(writer, snapshot.P12AdmissionProfile);
                WriteString(writer, snapshot.ContractIdentity); writer.Write(snapshot.SchemaVersion);
                WriteString(writer, snapshot.Fingerprint); WriteString(writer, snapshot.SelectedP9ProfileFingerprint);
                WriteString(writer, snapshot.SelectedP9ContractIdentity); writer.Write(snapshot.SelectedP9SchemaVersion);
                WriteConfiguration(writer, snapshot.EffectiveConfiguration);
                WriteStrings(writer, snapshot.CanonicalProvenanceRecords); WriteStrings(writer, snapshot.AuthoredDefinitionIds);
                WriteStrings(writer, snapshot.OutputOwners); WriteStrings(writer, snapshot.StageDependencyRecords);
                writer.Write(snapshot.CalendarMonthsPerYear); writer.Write(snapshot.CalendarWeeksPerMonth);
                writer.Write(snapshot.CalendarDaysPerWeek); WriteInts(writer, snapshot.MonthLengths);
                writer.Write(snapshot.Seed); WriteString(writer, snapshot.SeedSource);
                WriteStrings(writer, snapshot.StageOrder); WriteString(writer, snapshot.FirstSimulatedBoundary);
                writer.Flush();
                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                { result = new P12AResult(P12AResultCode.LimitExceeded, "p9.genesis-manifest"); return false; }
                bytes = stream.ToArray(); result = null; return true;
            }
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException || exception is EncoderFallbackException)
        { result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p9.genesis-manifest"); return false; }
    }

    internal static bool TryDecode(byte[] bytes, out P12CP9GenesisManifestCoreSnapshot snapshot, out P12AResult result)
    {
        snapshot = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p9.genesis-manifest");
        if (bytes == null) return false;
        if (bytes.Length > P12AContinuationCodec.MaxSectionBytes)
        { result = new P12AResult(P12AResultCode.LimitExceeded, "p9.genesis-manifest"); return false; }
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                string snapshotContract = RequiredString(reader); int snapshotVersion = reader.ReadInt32();
                SimulationRuntimeAdmissionProfile profile = ReadProfile(reader);
                string contract = RequiredString(reader); int schemaVersion = reader.ReadInt32();
                string fingerprint = RequiredString(reader), selectedFingerprint = RequiredString(reader);
                string selectedContract = RequiredString(reader); int selectedVersion = reader.ReadInt32();
                if (!ReadConfiguration(reader, out EffectiveSimulationConfiguration configuration)) return false;
                List<string> provenance = ReadStrings(reader); List<string> definitions = ReadStrings(reader);
                List<string> outputOwners = ReadStrings(reader); List<string> dependencies = ReadStrings(reader);
                int months = reader.ReadInt32(), weeks = reader.ReadInt32(), days = reader.ReadInt32();
                List<int> monthLengths = ReadInts(reader); int seed = reader.ReadInt32();
                string seedSource = RequiredString(reader); List<string> stages = ReadStrings(reader);
                string firstBoundary = RequiredString(reader);
                if (stream.Position != stream.Length) return false;
                snapshot = new P12CP9GenesisManifestCoreSnapshot(snapshotContract, snapshotVersion, profile,
                    contract, schemaVersion, fingerprint, selectedFingerprint, selectedContract, selectedVersion,
                    configuration, provenance, definitions, outputOwners, dependencies,
                    months, weeks, days, monthLengths, seed, seedSource, stages, firstBoundary);
                if (!snapshot.TryValidate(out _)) { snapshot = null; return false; }
                result = null; return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is P12ALimitException
            || exception is IOException || exception is ArgumentException || exception is DecoderFallbackException
            || exception is OverflowException)
        { snapshot = null; return false; }
    }

    private static void WriteConfiguration(BinaryWriter writer, EffectiveSimulationConfiguration value)
    {
        if (value == null || value.Population == null || value.Economy == null || value.Travel == null
            || value.Crime == null || value.GuardCrime == null || value.MerchantTrade == null
            || value.CommercialKnowledge == null || value.NaturalMortality == null || value.AggregateDemography == null)
            throw new ArgumentException("The effective configuration is incomplete.");
        WriteEnum(writer, value.Population.RepresentationMode); WriteEnum(writer, value.Population.DecisionScope);
        writer.Write(value.Population.MaturityAgeYears); WriteBool(writer, value.Economy.Enabled);
        writer.Write(value.Travel.TravelCostPerDay); WriteBool(writer, value.Crime.Enabled);
        WriteBool(writer, value.Crime.AutonomousEnabled); WriteBool(writer, value.GuardCrime.Enabled);
        EffectiveMerchantTradeConfiguration merchant = value.MerchantTrade;
        WriteBool(writer, merchant.Enabled); WriteBool(writer, merchant.AllowAutonomousTradeRepositioning);
        writer.Write(merchant.MaxTradeAmount); writer.Write(merchant.LocalWholesalePriceMultiplier);
        writer.Write(merchant.LocalReserveRatio); writer.Write(merchant.MaxUnprofitablePlanWaitDays);
        EffectiveCommercialKnowledgeConfiguration knowledge = value.CommercialKnowledge;
        writer.Write(knowledge.FreshForDays); writer.Write(knowledge.MaxUsefulAgeDays);
        writer.Write(knowledge.MaxSharedObservationsPerInteraction);
        WriteEnum(writer, value.NaturalMortality.Policy); writer.Write(value.NaturalMortality.AnnualProbability);
        WriteEnum(writer, value.AggregateDemography.Policy); writer.Write(value.AggregateDemography.AnnualBirthRate);
        writer.Write(value.AggregateDemography.AnnualDeathRate);
    }

    private static bool ReadConfiguration(BinaryReader reader, out EffectiveSimulationConfiguration value)
    {
        value = null;
        if (!ReadEnum(reader, 2, out PopulationRepresentationMode representation)
            || !ReadEnum(reader, 1, out NpcDecisionSimulationScope scope)) return false;
        long maturity = reader.ReadInt64(); bool economy = ReadBool(reader); float travel = reader.ReadSingle();
        bool crime = ReadBool(reader), autonomousCrime = ReadBool(reader), guard = ReadBool(reader);
        bool merchantEnabled = ReadBool(reader), repositioning = ReadBool(reader);
        int maxAmount = reader.ReadInt32(); float wholesale = reader.ReadSingle(), reserve = reader.ReadSingle();
        int maxWait = reader.ReadInt32(); int fresh = reader.ReadInt32(), maxAge = reader.ReadInt32(), maxShared = reader.ReadInt32();
        if (!ReadEnum(reader, 1, out NaturalMortalityPolicy mortalityPolicy)) return false;
        double mortalityProbability = reader.ReadDouble();
        if (!ReadEnum(reader, 1, out AggregateDemographyPolicy demographyPolicy)) return false;
        double birthRate = reader.ReadDouble(), deathRate = reader.ReadDouble();
        value = new EffectiveSimulationConfiguration(
            new EffectivePopulationConfiguration(representation, scope, maturity),
            new EffectiveEconomyConfiguration(economy), new EffectiveTravelConfiguration(travel),
            new EffectiveCrimeConfiguration(crime, autonomousCrime), new EffectiveGuardCrimeConfiguration(guard),
            new EffectiveNaturalMortalityConfiguration(mortalityPolicy, mortalityProbability),
            new EffectiveAggregateDemographyConfiguration(demographyPolicy, birthRate, deathRate),
            new EffectiveMerchantTradeConfiguration(merchantEnabled, repositioning, maxAmount, wholesale, reserve, maxWait),
            new EffectiveCommercialKnowledgeConfiguration(fresh, maxAge, maxShared));
        return true;
    }

    private static void WriteStrings(BinaryWriter writer, IReadOnlyList<string> values)
    {
        if (values == null || values.Count > P12AContinuationCodec.MaxRows) throw new ArgumentException("Invalid P9 string list.");
        writer.Write(values.Count);
        foreach (string value in values) WriteString(writer, value);
    }
    private static List<string> ReadStrings(BinaryReader reader)
    {
        int count = ReadCount(reader); List<string> values = new List<string>(count);
        for (int i = 0; i < count; i++) values.Add(RequiredString(reader)); return values;
    }
    private static void WriteInts(BinaryWriter writer, IReadOnlyList<int> values)
    {
        if (values == null || values.Count > P12AContinuationCodec.MaxRows) throw new ArgumentException("Invalid P9 integer list.");
        writer.Write(values.Count); foreach (int value in values) writer.Write(value);
    }
    private static List<int> ReadInts(BinaryReader reader)
    {
        int count = ReadCount(reader); List<int> values = new List<int>(count);
        for (int i = 0; i < count; i++) values.Add(reader.ReadInt32()); return values;
    }
    private static int ReadCount(BinaryReader reader)
    { int count = reader.ReadInt32(); if (count < 0 || count > P12AContinuationCodec.MaxRows) throw new P12ALimitException(); return count; }
    private static void WriteString(BinaryWriter writer, string value)
    { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("P9 strings must be non-empty."); P12AWire.WriteString(writer, value); }
    private static string RequiredString(BinaryReader reader)
    { if (!P12AWire.ReadString(reader, out string value) || string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(); return value; }
    private static void WriteBool(BinaryWriter writer, bool value) => writer.Write((byte)(value ? 1 : 0));
    private static bool ReadBool(BinaryReader reader)
    { byte value = reader.ReadByte(); if (value > 1) throw new InvalidDataException(); return value == 1; }
    private static void WriteEnum<T>(BinaryWriter writer, T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentException("Unknown enum value.");
        writer.Write(Convert.ToUInt16(value, System.Globalization.CultureInfo.InvariantCulture));
    }
    private static bool ReadEnum<T>(BinaryReader reader, ushort maxValue, out T value) where T : struct, Enum
    {
        value = default; ushort raw = reader.ReadUInt16();
        if (raw > maxValue) return false;
        value = (T)Enum.ToObject(typeof(T), raw);
        return Enum.IsDefined(typeof(T), value);
    }
    private static SimulationRuntimeAdmissionProfile ReadProfile(BinaryReader reader)
    {
        if (!ReadEnum(reader, (ushort)SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            out SimulationRuntimeAdmissionProfile profile)
            || profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1) throw new InvalidDataException();
        return profile;
    }
}
