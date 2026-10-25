using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Detached captured values for one NPC's market, liquidity and share-receipt owners.</summary>
internal sealed class P12ANpcCommercialKnowledgeOwnerValue
{
    internal P12ANpcCommercialKnowledgeOwnerValue(string runtimeId,
        IEnumerable<P12DNpcCommercialMarketValue> markets,
        IEnumerable<P12DNpcCommercialLiquidityValue> liquidity,
        IEnumerable<P12DNpcCommercialShareReceiptValue> receipts, long revision)
    {
        RuntimeId = runtimeId;
        Markets = Copy(markets);
        Liquidity = Copy(liquidity);
        Receipts = Copy(receipts);
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal IReadOnlyList<P12DNpcCommercialMarketValue> Markets { get; }
    internal IReadOnlyList<P12DNpcCommercialLiquidityValue> Liquidity { get; }
    internal IReadOnlyList<P12DNpcCommercialShareReceiptValue> Receipts { get; }
    internal long Revision { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        => Array.AsReadOnly((source ?? throw new ArgumentNullException(nameof(source))).ToArray());
}

/// <summary>Exact P12-A codecs for per-NPC CommercialKnowledge owner sections.</summary>
internal static class P12ANpcCommercialKnowledgeOwnerCodec
{
    private const string PayloadContract = "p12a.npc-commercial-knowledge/v1";
    private const int MarketKind = 7;
    private const int LiquidityKind = 8;
    private const int ReceiptKind = 9;

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection markets, out P12AOwnerSection liquidity,
        out P12AOwnerSection receipts, out P12AResult result)
    {
        markets = liquidity = receipts = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, source?.RuntimeId), source?.RuntimeId);
        if (source == null || source.CommercialMarkets == null || source.CommercialLiquidity == null
            || source.CommercialShareReceipts == null) return false;
        if (source.CommercialMarkets.Count > P12AContinuationCodec.MaxRows
            || source.CommercialLiquidity.Count > P12AContinuationCodec.MaxRows
            || source.CommercialShareReceipts.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, source.RuntimeId), source.RuntimeId);
            return false;
        }
        return TryEncode(new P12ANpcCommercialKnowledgeOwnerValue(source.RuntimeId,
            source.CommercialMarkets, source.CommercialLiquidity, source.CommercialShareReceipts,
            source.CommercialKnowledgeRevision), out markets, out liquidity, out receipts, out result);
    }

    internal static bool TryEncode(P12ANpcCommercialKnowledgeOwnerValue value,
        out P12AOwnerSection markets, out P12AOwnerSection liquidity,
        out P12AOwnerSection receipts, out P12AResult result)
    {
        markets = liquidity = receipts = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, value?.RuntimeId), value?.RuntimeId);
        if (value != null && (value.Markets?.Count > P12AContinuationCodec.MaxRows
            || value.Liquidity?.Count > P12AContinuationCodec.MaxRows
            || value.Receipts?.Count > P12AContinuationCodec.MaxRows))
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, value.RuntimeId), value.RuntimeId);
            return false;
        }
        if (!Validate(value)) return false;
        try
        {
            string marketId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, value.RuntimeId);
            string liquidityId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(LiquidityKind, value.RuntimeId);
            string receiptId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(ReceiptKind, value.RuntimeId);
            string alias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(marketId);
            if (string.IsNullOrWhiteSpace(alias)
                || alias != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(liquidityId)
                || alias != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(receiptId)) return false;
            P12AWire.GetEncodedStringByteCount(marketId);
            P12AWire.GetEncodedStringByteCount(liquidityId);
            P12AWire.GetEncodedStringByteCount(receiptId);
            P12AWire.GetEncodedStringByteCount(alias);

            if (!TryEncodeRows(value.Markets, value.RuntimeId, value.Revision, marketId,
                    row => StableKeyEncoding.Encode(row.LocationRuntimeId, row.ItemDefinitionId),
                    row => EncodeMarket(value.RuntimeId, value.Revision, row),
                    out List<P12AOwnerRow> marketRows, out result)
                || !TryEncodeRows(value.Liquidity, value.RuntimeId, value.Revision, liquidityId,
                    row => row.LocationRuntimeId,
                    row => EncodeLiquidity(value.RuntimeId, value.Revision, row),
                    out List<P12AOwnerRow> liquidityRows, out result)
                || !TryEncodeRows(value.Receipts, value.RuntimeId, value.Revision, receiptId,
                    row => row.OperationIdentity,
                    row => EncodeReceipt(value.RuntimeId, value.Revision, row),
                    out List<P12AOwnerRow> receiptRows, out result)) return false;

            markets = Section(marketId, alias, value.Revision, marketRows);
            liquidity = Section(liquidityId, alias, value.Revision, liquidityRows);
            receipts = Section(receiptId, alias, value.Revision, receiptRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            markets = liquidity = receipts = null;
            result = new P12AResult(IsLimitException(exception)
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, value.RuntimeId), value.RuntimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection markets, P12AOwnerSection liquidity,
        P12AOwnerSection receipts, out P12ANpcCommercialKnowledgeOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            markets?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(MarketKind, string.Empty));
        if (!TryReadSection(markets, MarketKind, enforceCardinalityBound: false, out string runtimeId, out long revision,
                out IReadOnlyList<P12AOwnerRow> marketRows, out result)
            || !TryReadSection(liquidity, LiquidityKind, enforceCardinalityBound: false, out string liquidityRuntimeId,
                out long liquidityRevision, out IReadOnlyList<P12AOwnerRow> liquidityRows, out result)
            || !TryReadSection(receipts, ReceiptKind, enforceCardinalityBound: true, out string receiptRuntimeId,
                out long receiptRevision, out IReadOnlyList<P12AOwnerRow> receiptRows, out result)) return false;
        if (runtimeId != liquidityRuntimeId || runtimeId != receiptRuntimeId
            || revision != liquidityRevision || revision != receiptRevision)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, markets.Id, runtimeId);
            return false;
        }
        if (receiptRows.Count > revision)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, receipts.Id, runtimeId);
            return false;
        }

        if (!TryDecodeRows(marketRows, runtimeId, revision, "market", MarketIdentity,
                DecodeMarket, out List<P12DNpcCommercialMarketValue> marketValues)
            || !TryDecodeRows(liquidityRows, runtimeId, revision, "liquidity", LiquidityIdentity,
                DecodeLiquidity, out List<P12DNpcCommercialLiquidityValue> liquidityValues)
            || !TryDecodeRows(receiptRows, runtimeId, revision, "share-receipt", ReceiptIdentity,
                DecodeReceipt, out List<P12DNpcCommercialShareReceiptValue> receiptValues))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, markets.Id, runtimeId);
            return false;
        }
        value = new P12ANpcCommercialKnowledgeOwnerValue(runtimeId,
            marketValues, liquidityValues, receiptValues, revision);
        result = null;
        return true;
    }

    private static bool Validate(P12ANpcCommercialKnowledgeOwnerValue value)
    {
        if (value == null || !IsRuntimeId(value.RuntimeId) || value.Revision < 0L
            || value.Markets == null || value.Liquidity == null || value.Receipts == null
            || value.Receipts.Count > value.Revision) return false;
        HashSet<string> marketKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12DNpcCommercialMarketValue row in value.Markets)
        {
            if (!IsValidMarket(row) || !marketKeys.Add(MarketIdentity(row))) return false;
        }
        HashSet<string> liquidityKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12DNpcCommercialLiquidityValue row in value.Liquidity)
            if (!IsValidLiquidity(row) || !liquidityKeys.Add(LiquidityIdentity(row))) return false;
        HashSet<string> receiptKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12DNpcCommercialShareReceiptValue row in value.Receipts)
            if (!IsValidReceipt(row) || !receiptKeys.Add(ReceiptIdentity(row))) return false;
        return true;
    }

    private static bool IsValidMarket(P12DNpcCommercialMarketValue value)
        => value != null && !string.IsNullOrWhiteSpace(value.LocationRuntimeId)
            && !string.IsNullOrWhiteSpace(value.ItemDefinitionId)
            && value.ObservedStock >= 0 && IsFiniteNonNegative(value.ObservedPrice)
            && value.ObservedDay >= 0L && value.ReceivedDay >= value.ObservedDay
            && Enum.IsDefined(typeof(CommercialKnowledgeSource), value.Source)
            && (value.Source == CommercialKnowledgeSource.SharedByNpc
                ? !string.IsNullOrWhiteSpace(value.SourceRuntimeId)
                : value.SourceRuntimeId == null);

    private static bool IsValidLiquidity(P12DNpcCommercialLiquidityValue value)
        => value != null && !string.IsNullOrWhiteSpace(value.LocationRuntimeId)
            && (value.Mode == MarketLiquidityMode.Open || value.Mode == MarketLiquidityMode.AccountBacked)
            && value.ObservedDay >= 0L && value.ReceivedDay >= value.ObservedDay
            && Enum.IsDefined(typeof(CommercialKnowledgeSource), value.Source)
            && (value.Source == CommercialKnowledgeSource.SharedByNpc
                ? !string.IsNullOrWhiteSpace(value.SourceRuntimeId)
                : value.SourceRuntimeId == null)
            && (value.Mode == MarketLiquidityMode.Open
                ? value.PurchasingPower == 0f
                : !float.IsNaN(value.PurchasingPower) && !float.IsInfinity(value.PurchasingPower)
                    && value.PurchasingPower >= 0f);

    private static bool IsValidReceipt(P12DNpcCommercialShareReceiptValue value)
    {
        if (value == null) return false;
        return P12DNpcCommercialShareReceiptValue.TryCreateFromOwnerSnapshot(value.OperationIdentity,
            value.Fingerprint, value.BoundaryOccurrenceId, value.SenderRuntimeId, value.SenderPersonId,
            value.ReceiverRuntimeId, value.ReceiverPersonId, value.SourceSnapshotFingerprint,
            value.AppliedObservationCount, out _);
    }

    private static bool IsValidSource(CommercialKnowledgeSource source, string sourceRuntimeId)
        => Enum.IsDefined(typeof(CommercialKnowledgeSource), source)
            && (source == CommercialKnowledgeSource.SharedByNpc
                ? !string.IsNullOrWhiteSpace(sourceRuntimeId) : sourceRuntimeId == null);

    private static P12AOwnerSection Section(string id, string alias, long revision, List<P12AOwnerRow> rows)
        => new P12AOwnerSection(id, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
            OwnerSectionRole.Required, alias, revision, rows);

    private static bool TryEncodeRows<T>(IReadOnlyList<T> values, string runtimeId, long revision,
        string sectionId, Func<T, string> identity, Func<T, byte[]> encode,
        out List<P12AOwnerRow> rows, out P12AResult result) where T : class
    {
        rows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
        if (values == null || values.Count > P12AContinuationCodec.MaxRows)
        {
            if (values?.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
            return false;
        }
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        List<P12AOwnerRow> encoded = new List<P12AOwnerRow>(values.Count);
        long encodedLength = sizeof(int);
        foreach (T value in values)
        {
            if (value == null) return false;
            string key = identity(value);
            if (string.IsNullOrWhiteSpace(key) || !keys.Add(key)) return false;
            byte[] payload = encode(value);
            P12AWire.GetEncodedStringByteCount(key);
            encodedLength = checked(encodedLength + EncodedRowLength(key, payload.Length));
            if (encodedLength > P12AContinuationCodec.MaxSectionBytes)
            {
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
                return false;
            }
            encoded.Add(new P12AOwnerRow(key, revision, payload));
        }
        rows = encoded;
        result = null;
        return true;
    }

    private static bool TryReadSection(P12AOwnerSection section, int kind, bool enforceCardinalityBound,
        out string runtimeId, out long revision, out IReadOnlyList<P12AOwnerRow> rows, out P12AResult result)
    {
        string prefix = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(kind, string.Empty);
        runtimeId = null; revision = 0L; rows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id ?? prefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id) || !section.Id.StartsWith(prefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows == null || section.Rows.Count > P12AContinuationCodec.MaxRows
            || (enforceCardinalityBound && section.Rows.Count > section.Revision))
        {
            if (section?.Rows?.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, section.Id);
            return false;
        }
        string candidate = section.Id.Substring(prefix.Length);
        if (!IsRuntimeId(candidate) || section.Id != prefix + candidate
            || section.AliasGroup != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(section.AliasGroup);
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            long encodedLength = sizeof(int);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision || string.IsNullOrWhiteSpace(row.OwnerKey)
                    || !keys.Add(row.OwnerKey) || row.Payload == null || row.Payload.Length != row.PayloadLength
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
                P12AWire.GetEncodedStringByteCount(row.OwnerKey);
                encodedLength = checked(encodedLength + EncodedRowLength(row.OwnerKey, row.PayloadLength));
                if (encodedLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, section.Id, candidate);
                    return false;
                }
            }
            runtimeId = candidate; revision = section.Revision; rows = section.Rows;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(IsLimitException(exception)
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id, candidate);
            return false;
        }
    }

    private static bool TryDecodeRows<T>(IReadOnlyList<P12AOwnerRow> rows, string runtimeId,
        long revision, string kind, Func<T, string> identity, Func<byte[], T> decode,
        out List<T> values) where T : class
    {
        values = new List<T>(rows.Count);
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in rows)
        {
            T value;
            try { value = DecodePayload(row.Payload, runtimeId, revision, kind, decode); }
            catch (Exception exception) when (IsWireException(exception))
            { values = null; return false; }
            if (value == null || identity(value) != row.OwnerKey || !keys.Add(row.OwnerKey))
            { values = null; return false; }
            values.Add(value);
        }
        return true;
    }

    private static T DecodePayload<T>(byte[] payload, string runtimeId, long revision,
        string kind, Func<byte[], T> decode) where T : class
    {
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !P12AWire.ReadString(reader, out string owner) || owner != runtimeId
                || !P12AWire.ReadString(reader, out string rowKind) || rowKind != kind
                || reader.ReadInt64() != revision) return null;
            long remaining = stream.Length - stream.Position;
            if (remaining < 0L || remaining > int.MaxValue) return null;
            byte[] data = reader.ReadBytes((int)remaining);
            if (stream.Position != stream.Length || data.Length != remaining) return null;
            return decode(data);
        }
    }

    private static byte[] EncodeMarket(string runtimeId, long revision, P12DNpcCommercialMarketValue value)
        => Encode(runtimeId, revision, "market", writer =>
        {
            P12AWire.WriteString(writer, value.LocationRuntimeId);
            P12AWire.WriteString(writer, value.ItemDefinitionId);
            writer.Write(value.ObservedPrice); writer.Write(value.ObservedStock);
            writer.Write(value.ObservedDay); writer.Write(value.ReceivedDay);
            writer.Write((int)value.Source); P12AWire.WriteString(writer, value.SourceRuntimeId);
        });

    private static byte[] EncodeLiquidity(string runtimeId, long revision, P12DNpcCommercialLiquidityValue value)
        => Encode(runtimeId, revision, "liquidity", writer =>
        {
            P12AWire.WriteString(writer, value.LocationRuntimeId);
            writer.Write((int)value.Mode); writer.Write(value.PurchasingPower);
            writer.Write(value.ObservedDay); writer.Write(value.ReceivedDay);
            writer.Write((int)value.Source); P12AWire.WriteString(writer, value.SourceRuntimeId);
        });

    private static byte[] EncodeReceipt(string runtimeId, long revision, P12DNpcCommercialShareReceiptValue value)
        => Encode(runtimeId, revision, "share-receipt", writer =>
        {
            P12AWire.WriteString(writer, value.OperationIdentity);
            P12AWire.WriteString(writer, value.Fingerprint);
            P12AWire.WriteString(writer, value.BoundaryOccurrenceId);
            P12AWire.WriteString(writer, value.SenderRuntimeId);
            P12AWire.WriteString(writer, value.SenderPersonId);
            P12AWire.WriteString(writer, value.ReceiverRuntimeId);
            P12AWire.WriteString(writer, value.ReceiverPersonId);
            P12AWire.WriteString(writer, value.SourceSnapshotFingerprint);
            writer.Write(value.AppliedObservationCount);
        });

    private static byte[] Encode(string runtimeId, long revision, string kind, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, kind);
            writer.Write(revision);
            write(writer);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return stream.ToArray();
        }
    }

    private static P12DNpcCommercialMarketValue DecodeMarket(byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string location)
                || !P12AWire.ReadString(reader, out string item)) return null;
            float price = reader.ReadSingle(); int stock = reader.ReadInt32();
            long observed = reader.ReadInt64(); long received = reader.ReadInt64();
            CommercialKnowledgeSource source = (CommercialKnowledgeSource)reader.ReadInt32();
            if (!P12AWire.ReadString(reader, out string sourceNpc) || stream.Position != stream.Length
                || string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(item)
                || stock < 0 || !IsFiniteNonNegative(price)
                || observed < 0L || received < observed || !IsValidSource(source, sourceNpc)) return null;
            return new P12DNpcCommercialMarketValue(location, item, price, stock,
                observed, received, source, sourceNpc);
        }
    }

    private static P12DNpcCommercialLiquidityValue DecodeLiquidity(byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string location)) return null;
            MarketLiquidityMode mode = (MarketLiquidityMode)reader.ReadInt32();
            float power = reader.ReadSingle(); long observed = reader.ReadInt64(); long received = reader.ReadInt64();
            CommercialKnowledgeSource source = (CommercialKnowledgeSource)reader.ReadInt32();
            if (!P12AWire.ReadString(reader, out string sourceNpc) || stream.Position != stream.Length
                || string.IsNullOrWhiteSpace(location) || observed < 0L || received < observed
                || !IsValidSource(source, sourceNpc)
                || (mode != MarketLiquidityMode.Open && mode != MarketLiquidityMode.AccountBacked)
                || (mode == MarketLiquidityMode.Open ? power != 0f
                    : float.IsNaN(power) || float.IsInfinity(power) || power < 0f)) return null;
            return new P12DNpcCommercialLiquidityValue(location, mode, power,
                observed, received, source, sourceNpc);
        }
    }

    private static P12DNpcCommercialShareReceiptValue DecodeReceipt(byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string operation)
                || !P12AWire.ReadString(reader, out string fingerprint)
                || !P12AWire.ReadString(reader, out string boundary)
                || !P12AWire.ReadString(reader, out string sender)
                || !P12AWire.ReadString(reader, out string senderPerson)
                || !P12AWire.ReadString(reader, out string receiver)
                || !P12AWire.ReadString(reader, out string receiverPerson)
                || !P12AWire.ReadString(reader, out string sourceSnapshot)) return null;
            int applied = reader.ReadInt32();
            if (stream.Position != stream.Length) return null;
            return P12DNpcCommercialShareReceiptValue.TryCreateFromOwnerSnapshot(operation, fingerprint,
                boundary, sender, senderPerson, receiver, receiverPerson, sourceSnapshot,
                applied, out P12DNpcCommercialShareReceiptValue receipt) ? receipt : null;
        }
    }

    private static string MarketIdentity(P12DNpcCommercialMarketValue value)
        => StableKeyEncoding.Encode(value.LocationRuntimeId, value.ItemDefinitionId);
    private static string LiquidityIdentity(P12DNpcCommercialLiquidityValue value)
        => value.LocationRuntimeId;
    private static string ReceiptIdentity(P12DNpcCommercialShareReceiptValue value) => value.OperationIdentity;
    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;
    private static bool IsFiniteNonNegative(float value)
        => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    private static long EncodedRowLength(string key, int payloadLength)
        => sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(key) + sizeof(long) + sizeof(int) + payloadLength;
    private static bool IsLimitException(Exception exception)
        => exception is P12ALimitException || exception is OverflowException;
    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException || exception is DecoderFallbackException;
}
