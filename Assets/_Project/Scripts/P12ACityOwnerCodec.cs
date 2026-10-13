using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit value codecs for the four selected Daily-v1 City rows.</summary>
internal static class P12ACityOwnerCodec
{
    private const string PayloadContract = "p12a.city-owner/v1";
    private const string NpcOwnerPrefix = "npc/";
    private const string MarketItemOwnerPrefix = "market-item/";
    private const string PopulationOwnerPrefix = "population/";
    private const string ReceiptOwnerPrefix = "population-receipt/";

    internal static bool TryEncode(P12DCityRootOwnerSnapshot snapshot,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (!IsSupported(snapshot)) return false;

        try
        {
            string cityId = snapshot.CityRuntimeId;
            string cityKey = EncodeCityKey(cityId);
            long importantRevision = snapshot.ImportantNpcRevision;
            long marketRevision = snapshot.MarketRevision;
            long populationRevision = snapshot.PopulationRevision;
            long receiptRevision = snapshot.PopulationReceiptRevision;

            List<P12AOwnerRow> presenceRows = new List<P12AOwnerRow>(snapshot.ImportantNpcRuntimeIds.Count);
            foreach (string npcId in snapshot.ImportantNpcRuntimeIds)
                presenceRows.Add(new P12AOwnerRow(NpcOwnerPrefix + npcId, importantRevision, Array.Empty<byte>()));

            List<P12AOwnerRow> marketRows = new List<P12AOwnerRow>(snapshot.MarketItems.Count);
            foreach (P12DCityMarketItemSnapshot item in snapshot.MarketItems)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    WritePayloadHeader(writer, cityId);
                    P12AWire.WriteString(writer, item.ItemDefinitionId);
                    writer.Write(item.Amount);
                    writer.Write(item.DesiredAmount);
                    writer.Write(item.CurrentPrice);
                    writer.Flush();
                    marketRows.Add(new P12AOwnerRow(MarketItemOwnerPrefix + item.ItemDefinitionId,
                        marketRevision, stream.ToArray()));
                }
            }

            marketRows.Sort((left, right) => StringComparer.Ordinal.Compare(left.OwnerKey, right.OwnerKey));

            byte[] populationPayload;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                WritePayloadHeader(writer, cityId);
                P12AWire.WriteString(writer, snapshot.CityDefinitionId);
                P12AWire.WriteString(writer, snapshot.LegacyLocationRuntimeId);
                P12AWire.WriteString(writer, snapshot.PopulationEconomicRuntimeId);
                WriteEnum(writer, snapshot.PopulationPaymentMode);
                WriteBool(writer, snapshot.PopulationEconomyHasAccount);
                P12AWire.WriteString(writer, snapshot.PopulationRuntimeId);
                writer.Write(snapshot.CurrentPopulation);
                writer.Flush();
                populationPayload = stream.ToArray();
            }

            P12AOwnerSection presence = Section(CityNpcPresenceCensusProvider.SectionIdFor(cityId),
                CityNpcPresenceCensusProvider.SchemaVersion, null, importantRevision, presenceRows);
            P12AOwnerSection market = Section(CityMarketCensusProvider.SectionIdPrefix + cityKey,
                CityMarketCensusProvider.SchemaVersion, null, marketRevision, marketRows);
            P12AOwnerSection population = Section(SettlementPopulationCensusProvider.AggregateSectionPrefix + cityKey,
                SettlementPopulationCensusProvider.SchemaVersion,
                P12ADailyV1Census.ExpectedAliasGroupForSection(SettlementPopulationCensusProvider.AggregateSectionPrefix + cityKey),
                populationRevision,
                new[] { new P12AOwnerRow(PopulationOwnerPrefix + cityId, populationRevision, populationPayload) });

            List<P12AOwnerRow> receiptRows = new List<P12AOwnerRow>(snapshot.PopulationReceipts.Count);
            foreach (PopulationOperationReceiptSnapshot receipt in snapshot.PopulationReceipts)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    WritePayloadHeader(writer, cityId);
                    P12AWire.WriteString(writer, receipt.Identity);
                    P12AWire.WriteString(writer, receipt.Fingerprint);
                    WriteTransition(writer, receipt.Transition);
                    writer.Flush();
                    receiptRows.Add(new P12AOwnerRow(ReceiptOwnerPrefix + receipt.Identity,
                        receiptRevision, stream.ToArray()));
                }
            }
            string receiptSectionId = SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + cityKey;
            P12AOwnerSection receipts = Section(receiptSectionId,
                SettlementPopulationCensusProvider.SchemaVersion,
                P12ADailyV1Census.ExpectedAliasGroupForSection(receiptSectionId),
                receiptRevision, receiptRows);

            List<P12AOwnerSection> ordered = new List<P12AOwnerSection>(4)
                { presence, market, population, receipts };
            ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            sections = ordered.AsReadOnly();
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException || exception is EncoderFallbackException)
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection presence, P12AOwnerSection market,
        P12AOwnerSection population, P12AOwnerSection receipts,
        out P12DCityRootOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (!TryReadPopulationIdentity(population, out string cityId, out result)) return false;
        string cityKey = EncodeCityKey(cityId);
        string presenceId = CityNpcPresenceCensusProvider.SectionIdFor(cityId);
        string marketId = CityMarketCensusProvider.SectionIdPrefix + cityKey;
        string populationId = SettlementPopulationCensusProvider.AggregateSectionPrefix + cityKey;
        string receiptsId = SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + cityKey;
        string populationAlias = P12ADailyV1Census.ExpectedAliasGroupForSection(populationId);
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, marketId);

        if (!IsSection(presence, presenceId, CityNpcPresenceCensusProvider.SchemaVersion, null)
            || !IsSection(market, marketId, CityMarketCensusProvider.SchemaVersion, null)
            || !IsSection(population, populationId, SettlementPopulationCensusProvider.SchemaVersion, populationAlias)
            || !IsSection(receipts, receiptsId, SettlementPopulationCensusProvider.SchemaVersion, populationAlias)
            || !RowsStrictlyAscending(market))
            return false;

        List<string> importantNpcIds = new List<string>(presence.Rows.Count);
        HashSet<string> uniqueNpcs = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in presence.Rows)
        {
            if (row == null || row.Revision != presence.Revision || row.PayloadLength != 0
                || !TryOwnerSuffix(row.OwnerKey, NpcOwnerPrefix, out string npcId)
                || !uniqueNpcs.Add(npcId)) return false;
            importantNpcIds.Add(npcId);
        }

        List<P12DCityMarketItemSnapshot> marketItems = new List<P12DCityMarketItemSnapshot>(market.Rows.Count);
        HashSet<string> uniqueMarketItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in market.Rows)
        {
            if (row == null || row.Revision != market.Revision
                || !TryOwnerSuffix(row.OwnerKey, MarketItemOwnerPrefix, out string ownerItemId)
                || !ReadMarketItem(row.Payload, cityId, ownerItemId, out P12DCityMarketItemSnapshot item)
                || !uniqueMarketItems.Add(item.ItemDefinitionId)) return false;
            marketItems.Add(item);
        }

        if (!ReadPopulation(population, cityId, out string cityDefinitionId,
                out string legacyLocationRuntimeId, out string economicId, out ConsumptionPaymentMode paymentMode,
                out bool economyHasAccount, out string populationRuntimeId, out int currentPopulation)
            || population.Rows.Count != 1 || population.Rows[0].Revision != population.Revision
            || population.Rows[0].OwnerKey != PopulationOwnerPrefix + cityId
            || currentPopulation < 0 || paymentMode != ConsumptionPaymentMode.Free || economyHasAccount
            || populationRuntimeId != cityId || economicId != "population-" + cityId)
            return false;

        List<PopulationOperationReceiptSnapshot> populationReceipts =
            new List<PopulationOperationReceiptSnapshot>(receipts.Rows.Count);
        HashSet<string> uniqueReceiptIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in receipts.Rows)
        {
            if (row == null || row.Revision != receipts.Revision
                || !TryOwnerSuffix(row.OwnerKey, ReceiptOwnerPrefix, out string ownerReceiptId)
                || !ReadReceipt(row.Payload, cityId, ownerReceiptId,
                    out PopulationOperationReceiptSnapshot receipt)
                || !uniqueReceiptIds.Add(receipt.Identity)) return false;
            populationReceipts.Add(receipt);
        }
        if (receipts.Revision < populationReceipts.Count) return false;

        snapshot = new P12DCityRootOwnerSnapshot(cityId, cityDefinitionId,
            legacyLocationRuntimeId, presence.Revision, importantNpcIds,
            cityId, MarketLiquidityMode.Open, market.Revision, marketItems,
            economicId, paymentMode, populationRuntimeId, currentPopulation,
            population.Revision, receipts.Revision, populationReceipts);
        result = null;
        return true;
    }

    internal static string EncodeCityKeyForSection(string cityRuntimeId)
        => EncodeCityKey(cityRuntimeId);

    private static bool TryReadPopulationIdentity(P12AOwnerSection section, out string cityId,
        out P12AResult result)
    {
        cityId = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id);
        if (section == null || section.Rows == null || section.Rows.Count != 1 || section.Rows[0] == null
            || !TryOwnerSuffix(section.Rows[0].OwnerKey, PopulationOwnerPrefix, out cityId)) return false;
        result = null;
        return true;
    }

    private static bool ReadPopulation(P12AOwnerSection section, string cityId,
        out string cityDefinitionId, out string legacyLocationRuntimeId,
        out string economicId, out ConsumptionPaymentMode paymentMode, out bool hasAccount,
        out string populationRuntimeId, out int currentPopulation)
    {
        cityDefinitionId = null; legacyLocationRuntimeId = null; economicId = null;
        paymentMode = default; hasAccount = true;
        populationRuntimeId = null; currentPopulation = -1;
        if (section?.Rows == null || section.Rows.Count != 1 || section.Rows[0]?.Payload == null) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(section.Rows[0].Payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!ReadPayloadHeader(reader, cityId)
                    || !Required(reader, out cityDefinitionId)
                    || !Required(reader, out legacyLocationRuntimeId)
                    || !Required(reader, out economicId)
                    || !ReadEnum(reader, out paymentMode)
                    || !ReadBool(reader, out hasAccount)
                    || !Required(reader, out populationRuntimeId)) return false;
                currentPopulation = reader.ReadInt32();
                return stream.Position == stream.Length;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is DecoderFallbackException || exception is ArgumentException)
        { return false; }
    }

    private static bool ReadMarketItem(byte[] bytes, string cityId, string ownerItemId,
        out P12DCityMarketItemSnapshot item)
    {
        item = null;
        if (bytes == null || bytes.Length > P12AContinuationCodec.MaxSectionBytes) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!ReadPayloadHeader(reader, cityId) || !Required(reader, out string itemId)
                    || itemId != ownerItemId) return false;
                int amount = reader.ReadInt32(), desired = reader.ReadInt32(); float price = reader.ReadSingle();
                if (stream.Position != stream.Length || amount < 0 || desired <= 0
                    || price <= 0f || float.IsNaN(price) || float.IsInfinity(price)) return false;
                item = new P12DCityMarketItemSnapshot(itemId, amount, desired, price);
                return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is DecoderFallbackException || exception is ArgumentException)
        { item = null; return false; }
    }

    private static bool ReadReceipt(byte[] bytes, string cityId, string ownerIdentity,
        out PopulationOperationReceiptSnapshot receipt)
    {
        receipt = null;
        if (bytes == null || bytes.Length > P12AContinuationCodec.MaxSectionBytes) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!ReadPayloadHeader(reader, cityId) || !Required(reader, out string identity)
                    || identity != ownerIdentity || !Required(reader, out string fingerprint)
                    || !ReadTransition(reader, out SettlementPopulationTransition transition)
                    || stream.Position != stream.Length || transition.SettlementRuntimeId != cityId) return false;
                receipt = new PopulationOperationReceiptSnapshot(identity, fingerprint, transition);
                return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is DecoderFallbackException || exception is ArgumentException)
        { receipt = null; return false; }
    }

    private static bool ReadTransition(BinaryReader reader, out SettlementPopulationTransition transition)
    {
        transition = null;
        if (!Required(reader, out string settlementId)) return false;
        long expectedRevision = reader.ReadInt64(); int populationBefore = reader.ReadInt32();
        int births = reader.ReadInt32(), deaths = reader.ReadInt32(), immigrations = reader.ReadInt32(), emigrations = reader.ReadInt32();
        long net = reader.ReadInt64(); int populationAfter = reader.ReadInt32();
        if (expectedRevision < 0L || populationBefore < 0 || births < 0 || deaths < 0
            || immigrations < 0 || emigrations < 0 || populationAfter < 0
            || net != (long)births + immigrations - deaths - emigrations
            || populationBefore + net != populationAfter) return false;
        transition = new SettlementPopulationTransition(settlementId, expectedRevision,
            populationBefore, new PopulationChangeSet(births, deaths, immigrations, emigrations),
            net, populationAfter);
        return true;
    }

    private static void WriteTransition(BinaryWriter writer, SettlementPopulationTransition transition)
    {
        if (transition == null || string.IsNullOrWhiteSpace(transition.SettlementRuntimeId)
            || transition.ExpectedRevision < 0L || transition.PopulationBefore < 0
            || transition.Births < 0 || transition.Deaths < 0 || transition.Immigrations < 0
            || transition.Emigrations < 0 || transition.PopulationAfter < 0
            || transition.NetChange != (long)transition.Births + transition.Immigrations
                - transition.Deaths - transition.Emigrations
            || (long)transition.PopulationBefore + transition.NetChange != transition.PopulationAfter)
            throw new ArgumentException("Invalid population receipt transition.");
        P12AWire.WriteString(writer, transition.SettlementRuntimeId);
        writer.Write(transition.ExpectedRevision); writer.Write(transition.PopulationBefore);
        writer.Write(transition.Births); writer.Write(transition.Deaths);
        writer.Write(transition.Immigrations); writer.Write(transition.Emigrations);
        writer.Write(transition.NetChange); writer.Write(transition.PopulationAfter);
    }

    private static bool IsSupported(P12DCityRootOwnerSnapshot snapshot)
    {
        if (snapshot == null || snapshot.SchemaVersion != P12DCityRootOwnerSnapshot.CurrentSchemaVersion
            || !ValidId(snapshot.CityRuntimeId) || !ValidId(snapshot.CityDefinitionId)
            || !ValidId(snapshot.LegacyLocationRuntimeId) || snapshot.ImportantNpcRevision < 0L
            || snapshot.ImportantNpcRuntimeIds == null || snapshot.MarketCounterpartyHasAccount
            || snapshot.MarketLiquidityMode != MarketLiquidityMode.Open
            || snapshot.MarketCounterpartyRuntimeId != snapshot.CityRuntimeId
            || snapshot.ImportantNpcRuntimeIds.Count > P12AContinuationCodec.MaxRows
            || snapshot.MarketRevision < 0L || snapshot.MarketItems == null
            || snapshot.MarketItems.Count > P12AContinuationCodec.MaxRows
            || snapshot.PopulationEconomyHasAccount || snapshot.PopulationPaymentMode != ConsumptionPaymentMode.Free
            || snapshot.PopulationEconomicRuntimeId != "population-" + snapshot.CityRuntimeId
            || snapshot.PopulationRuntimeId != snapshot.CityRuntimeId || snapshot.CurrentPopulation < 0
            || snapshot.PopulationRevision < 0L || snapshot.PopulationReceiptRevision < 0L
            || snapshot.PopulationReceipts == null || snapshot.PopulationReceipts.Count > P12AContinuationCodec.MaxRows
            || snapshot.PopulationReceiptRevision < snapshot.PopulationReceipts.Count)
            return false;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in snapshot.ImportantNpcRuntimeIds)
            if (!ValidId(id) || !ids.Add(id)) return false;
        ids.Clear();
        foreach (P12DCityMarketItemSnapshot item in snapshot.MarketItems)
            if (item == null || !ValidId(item.ItemDefinitionId) || !ids.Add(item.ItemDefinitionId)
                || item.Amount < 0 || item.DesiredAmount <= 0 || item.CurrentPrice <= 0f
                || float.IsNaN(item.CurrentPrice) || float.IsInfinity(item.CurrentPrice)) return false;
        ids.Clear();
        foreach (PopulationOperationReceiptSnapshot receipt in snapshot.PopulationReceipts)
            if (receipt == null || !ValidId(receipt.Identity) || !ValidId(receipt.Fingerprint)
                || !ids.Add(receipt.Identity) || receipt.Transition == null
                || receipt.Transition.SettlementRuntimeId != snapshot.PopulationRuntimeId) return false;
        return true;
    }

    private static P12AOwnerSection Section(string id, int schema, string alias, long revision,
        IEnumerable<P12AOwnerRow> rows) => new P12AOwnerSection(id, schema, 0,
            OwnerSectionRole.Required, alias, revision, rows);

    private static bool IsSection(P12AOwnerSection section, string id, int schema, string alias)
        => section != null && section.Id == id && section.Major == schema && section.Minor == 0
            && section.Role == OwnerSectionRole.Required && section.AliasGroup == alias
            && section.Revision >= 0 && section.Rows != null
            && section.Rows.Count <= P12AContinuationCodec.MaxRows;

    private static bool RowsStrictlyAscending(P12AOwnerSection section)
    {
        if (section?.Rows == null) return false;
        string previous = null;
        for (int i = 0; i < section.Rows.Count; i++)
        {
            string current = section.Rows[i]?.OwnerKey;
            if (string.IsNullOrWhiteSpace(current)
                || (previous != null && StringComparer.Ordinal.Compare(previous, current) >= 0)) return false;
            previous = current;
        }
        return true;
    }

    private static string EncodeCityKey(string cityId)
        => cityId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + cityId;

    private static bool TryOwnerSuffix(string ownerKey, string prefix, out string suffix)
    {
        suffix = null;
        if (ownerKey == null || !ownerKey.StartsWith(prefix, StringComparison.Ordinal)) return false;
        suffix = ownerKey.Substring(prefix.Length);
        return ValidId(suffix);
    }

    private static bool ReadPayloadHeader(BinaryReader reader, string expectedCityId)
    {
        return P12AWire.ReadString(reader, out string contract) && contract == PayloadContract
            && reader.ReadInt32() == P12DCityRootOwnerSnapshot.CurrentSchemaVersion
            && P12AWire.ReadString(reader, out string cityId) && cityId == expectedCityId;
    }

    private static void WritePayloadHeader(BinaryWriter writer, string cityId)
    {
        P12AWire.WriteString(writer, PayloadContract);
        writer.Write(P12DCityRootOwnerSnapshot.CurrentSchemaVersion);
        P12AWire.WriteString(writer, cityId);
    }

    private static bool Required(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && ValidId(value);

    private static bool ReadBool(BinaryReader reader, out bool value)
    {
        value = false; byte raw = reader.ReadByte();
        if (raw > 1) return false;
        value = raw == 1; return true;
    }

    private static void WriteBool(BinaryWriter writer, bool value) => writer.Write((byte)(value ? 1 : 0));

    private static void WriteEnum<T>(BinaryWriter writer, T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentException("Unknown owner enum value.");
        writer.Write(Convert.ToUInt16(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static bool ReadEnum<T>(BinaryReader reader, out T value) where T : struct, Enum
    {
        value = default;
        ushort raw = reader.ReadUInt16();
        value = (T)Enum.ToObject(typeof(T), raw);
        return Enum.IsDefined(typeof(T), value);
    }

    private static bool ValidId(string value) => !string.IsNullOrWhiteSpace(value)
        && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;
}
