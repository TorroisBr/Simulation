using System;
using System.IO;
using System.Text;

/// <summary>Detached Daily-v1 encoding for one NPC's optional current-action slot.</summary>
internal static class P12ANpcCurrentActionOwnerCodec
{
    private const string PayloadContract = "p12a.npc-current-action/v1";

    internal static bool TryEncode(P12DNpcDRow dRow, P12DNpcFRow fRow,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix, dRow?.RuntimeId);
        if (dRow == null || fRow == null
            || !string.Equals(dRow.RuntimeId, fRow.RuntimeId, StringComparison.Ordinal)
            || (dRow.CurrentActionDefinitionId == null) != (fRow.Action == null)
            || (fRow.Action != null && !string.Equals(dRow.CurrentActionDefinitionId,
                fRow.Action.ActionDefinitionId, StringComparison.Ordinal))) return false;
        return TryEncode(dRow.RuntimeId, dRow.CurrentActionDefinitionId, fRow.Action,
            dRow.CurrentActionRevision, out section, out result);
    }

    internal static bool TryEncode(string runtimeId, string actionDefinitionId,
        P12DNpcActionValue action, long revision, out P12AOwnerSection section,
        out P12AResult result)
    {
        section = null;
        string sectionId = IsRuntimeId(runtimeId)
            ? DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, runtimeId)
            : DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
        if (!IsRuntimeId(runtimeId) || revision < 0L
            || (action == null) != (actionDefinitionId == null)
            || (action != null && (!string.Equals(action.ActionDefinitionId, actionDefinitionId,
                    StringComparison.Ordinal) || !Validate(action)))) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(runtimeId);
            string alias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId);
            if (string.IsNullOrWhiteSpace(alias)) return false;
            P12AWire.GetEncodedStringByteCount(alias);
            if (action == null)
            {
                section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                    0, OwnerSectionRole.Required, alias, revision, Array.Empty<P12AOwnerRow>());
                result = null;
                return true;
            }

            byte[] payload;
            using (BoundedMemoryStream stream = new BoundedMemoryStream(P12AContinuationCodec.MaxSectionBytes))
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, runtimeId);
                writer.Write(revision);
                WriteAction(writer, action);
                writer.Flush();
                payload = stream.ToArray();
            }
            section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
                0, OwnerSectionRole.Required, alias, revision,
                new[] { new P12AOwnerRow(runtimeId, revision, payload) });
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(IsLimitException(exception)
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section, out string runtimeId,
        out string actionDefinitionId, out P12DNpcActionValue action, out long revision,
        out P12AResult result)
    {
        runtimeId = actionDefinitionId = null;
        action = null;
        revision = 0L;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix);
        string prefix = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix;
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(prefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows == null || section.Rows.Count > 1) return false;
        string candidate = section.Id.Substring(prefix.Length);
        if (!IsRuntimeId(candidate) || section.Id != DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(20, candidate)
            || section.AliasGroup != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(section.AliasGroup);
            if (section.Rows.Count == 0)
            {
                runtimeId = candidate;
                revision = section.Revision;
                result = null;
                return true;
            }

            P12AOwnerRow row = section.Rows[0];
            if (row == null || row.OwnerKey != candidate || row.Revision != section.Revision
                || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
            {
                if (row?.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
                    result = new P12AResult(P12AResultCode.LimitExceeded, section.Id, candidate);
                return false;
            }
            P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            byte[] payload = row.Payload;
            if (payload == null || payload.Length != row.PayloadLength) return false;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                    || !P12AWire.ReadString(reader, out string payloadRuntimeId) || payloadRuntimeId != candidate)
                    return false;
                long payloadRevision = reader.ReadInt64();
                if (payloadRevision != row.Revision || payloadRevision != section.Revision
                    || !TryReadAction(reader, out P12DNpcActionValue decodedAction)
                    || stream.Position != stream.Length) return false;
                runtimeId = candidate;
                actionDefinitionId = decodedAction.ActionDefinitionId;
                action = decodedAction;
                revision = payloadRevision;
                result = null;
                return true;
            }
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            runtimeId = actionDefinitionId = null;
            action = null;
            revision = 0L;
            result = new P12AResult(IsLimitException(exception)
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id, candidate);
            return false;
        }
    }

    private static void WriteAction(BinaryWriter writer, P12DNpcActionValue value)
    {
        P12AWire.WriteString(writer, value.ActionDefinitionId);
        P12AWire.WriteString(writer, value.TargetNpcRuntimeId);
        P12AWire.WriteString(writer, value.TargetCityRuntimeId);
        P12AWire.WriteString(writer, value.TargetItemDefinitionId);
        writer.Write(value.Amount);
        writer.Write(value.ExpectedUnitPrice);
        writer.Write(value.SuccessChanceMultiplier);
        writer.Write((ushort)value.TravelReason);
        writer.Write(value.ExpectedNetValue);
        P12AWire.WriteString(writer, value.OriginDecisionId);
        P12AWire.WriteString(writer, value.StableOccurrenceKey);
        writer.Write(value.CommercialDecisionEvidence != null);
        if (value.CommercialDecisionEvidence != null) WriteDecision(writer, value.CommercialDecisionEvidence);
        writer.Write(value.CommercialScoutingEvidence != null);
        if (value.CommercialScoutingEvidence != null) WriteScouting(writer, value.CommercialScoutingEvidence);
    }

    private static bool TryReadAction(BinaryReader reader, out P12DNpcActionValue value)
    {
        value = null;
        if (!P12AWire.ReadString(reader, out string actionId) || string.IsNullOrWhiteSpace(actionId)
            || !P12AWire.ReadString(reader, out string targetNpc)
            || !P12AWire.ReadString(reader, out string targetCity)
            || !P12AWire.ReadString(reader, out string targetItem)) return false;
        int amount = reader.ReadInt32();
        float expectedPrice = reader.ReadSingle();
        float successMultiplier = reader.ReadSingle();
        ushort reason = reader.ReadUInt16();
        float expectedNet = reader.ReadSingle();
        if (!P12AWire.ReadString(reader, out string originDecision)
            || !P12AWire.ReadString(reader, out string occurrence)
            || !TryReadOptional(reader, out CommercialDecisionEvidence commercialDecision)
            || !TryReadOptional(reader, out CommercialScoutingEvidence commercialScouting)
            || !Enum.IsDefined(typeof(NpcTravelReason), (int)reason)) return false;
        value = new P12DNpcActionValue(actionId, targetNpc, targetCity, targetItem, amount,
            expectedPrice, successMultiplier, (NpcTravelReason)reason, expectedNet,
            originDecision, occurrence, commercialDecision, commercialScouting);
        return Validate(value);
    }

    private static void WriteDecision(BinaryWriter writer, CommercialDecisionEvidence value)
    {
        P12AWire.WriteString(writer, value.ItemDefinitionId);
        P12AWire.WriteString(writer, value.CurrentLocationRuntimeId);
        P12AWire.WriteString(writer, value.TradeOriginLocationRuntimeId);
        P12AWire.WriteString(writer, value.TradeDestinationLocationRuntimeId);
        P12AWire.WriteString(writer, value.PreviousDestinationLocationRuntimeId);
        WriteObservation(writer, value.OriginObservation);
        WriteObservation(writer, value.DestinationObservation);
        WriteLiquidity(writer, value.DestinationLiquidityObservation);
        writer.Write(value.ExpectedQuantity);
        writer.Write(value.ExpectedPurchaseUnitPrice);
        writer.Write(value.ExpectedSaleUnitPrice);
        writer.Write(value.ExpectedTravelCost);
        writer.Write(value.ExpectedGrossProfit);
        writer.Write(value.ExpectedNetProfit);
        writer.Write(value.ExpectedScore);
    }

    private static bool TryReadDecision(BinaryReader reader, out CommercialDecisionEvidence value)
    {
        value = null;
        if (!ReadRequired(reader, out string itemId) || !ReadRequired(reader, out string currentLocation)
            || !ReadRequired(reader, out string originLocation) || !ReadRequired(reader, out string destinationLocation)
            || !P12AWire.ReadString(reader, out string previousDestination)
            || !TryReadObservation(reader, out CommercialObservationEvidence originObservation)
            || !TryReadObservation(reader, out CommercialObservationEvidence destinationObservation)
            || !TryReadLiquidity(reader, out CommercialLiquidityEvidence liquidity)) return false;
        int quantity = reader.ReadInt32();
        float purchase = reader.ReadSingle(), sale = reader.ReadSingle(), travel = reader.ReadSingle();
        float gross = reader.ReadSingle(), net = reader.ReadSingle(), score = reader.ReadSingle();
        if (quantity < 0 || !IsFiniteNonNegative(purchase) || !IsFiniteNonNegative(sale)
            || !IsFiniteNonNegative(travel) || !IsFinite(gross) || !IsFinite(net) || !IsFinite(score)
            || !IsOptionalId(previousDestination)) return false;
        value = new CommercialDecisionEvidence(itemId, currentLocation, originLocation, destinationLocation,
            previousDestination, originObservation, destinationObservation, quantity, purchase, sale,
            travel, gross, net, score, liquidity);
        return true;
    }

    private static void WriteObservation(BinaryWriter writer, CommercialObservationEvidence value)
    {
        writer.Write(value != null);
        if (value == null) return;
        P12AWire.WriteString(writer, value.LocationRuntimeId);
        writer.Write(value.KnownPrice); writer.Write(value.KnownStock);
        writer.Write(value.ObservedDay); writer.Write(value.ReceivedDay); writer.Write(value.Freshness);
        writer.Write((int)value.Source); P12AWire.WriteString(writer, value.SourceRuntimeId);
    }

    private static bool TryReadObservation(BinaryReader reader, out CommercialObservationEvidence value)
    {
        value = null;
        if (!TryReadBoolean(reader, out bool present)) return false;
        if (!present) return true;
        if (!ReadRequired(reader, out string location)) return false;
        float price = reader.ReadSingle(); int stock = reader.ReadInt32();
        long observed = reader.ReadInt64(), received = reader.ReadInt64(); float freshness = reader.ReadSingle();
        int source = reader.ReadInt32();
        if (!P12AWire.ReadString(reader, out string sourceRuntimeId)
            || !IsValidSource(source, sourceRuntimeId) || !IsFiniteNonNegative(price) || stock < 0
            || observed < 0L || received < observed || !IsUnitInterval(freshness)) return false;
        value = new CommercialObservationEvidence(location, price, stock, observed, received,
            freshness, (CommercialKnowledgeSource)source, sourceRuntimeId);
        return true;
    }

    private static void WriteLiquidity(BinaryWriter writer, CommercialLiquidityEvidence value)
    {
        writer.Write(value != null);
        if (value == null) return;
        P12AWire.WriteString(writer, value.LocationRuntimeId);
        writer.Write((int)value.LiquidityMode); writer.Write(value.ObservedPurchasingPower);
        writer.Write(value.ObservedDay); writer.Write(value.ReceivedDay); writer.Write(value.Freshness);
        writer.Write((int)value.Source); P12AWire.WriteString(writer, value.SourceRuntimeId);
    }

    private static bool TryReadLiquidity(BinaryReader reader, out CommercialLiquidityEvidence value)
    {
        value = null;
        if (!TryReadBoolean(reader, out bool present)) return false;
        if (!present) return true;
        if (!ReadRequired(reader, out string location)) return false;
        int mode = reader.ReadInt32(); float power = reader.ReadSingle();
        long observed = reader.ReadInt64(), received = reader.ReadInt64(); float freshness = reader.ReadSingle();
        int source = reader.ReadInt32();
        if (!P12AWire.ReadString(reader, out string sourceRuntimeId)
            || !Enum.IsDefined(typeof(MarketLiquidityMode), mode)
            || !IsValidSource(source, sourceRuntimeId) || observed < 0L || received < observed
            || !IsUnitInterval(freshness)
            || (MarketLiquidityMode)mode == MarketLiquidityMode.Open && power != 0f
            || (MarketLiquidityMode)mode == MarketLiquidityMode.AccountBacked && !IsFiniteNonNegative(power))
            return false;
        try
        {
            value = CommercialLiquidityEvidence.Capture(location, (MarketLiquidityMode)mode,
                power, observed, received, freshness, (CommercialKnowledgeSource)source,
                sourceRuntimeId);
            return value != null;
        }
        catch (ArgumentException) { return false; }
    }

    private static void WriteScouting(BinaryWriter writer, CommercialScoutingEvidence value)
    {
        P12AWire.WriteString(writer, value.TargetLocationRuntimeId);
        P12AWire.WriteString(writer, value.KnownRouteRuntimeId);
        writer.Write(value.UnknownObservationCount); writer.Write(value.StaleObservationCount);
        writer.Write(value.HasOldestObservation); writer.Write(value.OldestObservationDay);
        writer.Write(value.ExpectedTravelCost); writer.Write(value.ExpectedTravelDays); writer.Write(value.ExpectedScore);
    }

    private static bool TryReadScouting(BinaryReader reader, out CommercialScoutingEvidence value)
    {
        value = null;
        if (!ReadRequired(reader, out string target) || !ReadRequired(reader, out string route)) return false;
        int unknown = reader.ReadInt32(), stale = reader.ReadInt32();
        if (!TryReadBoolean(reader, out bool hasOldest)) return false;
        long oldest = reader.ReadInt64();
        float cost = reader.ReadSingle(); int days = reader.ReadInt32(); float score = reader.ReadSingle();
        if (unknown < 0 || stale < 0 || (!hasOldest && oldest != 0L) || (hasOldest && oldest < 0L)
            || !IsFiniteNonNegative(cost) || days < 1 || !IsFiniteNonNegative(score)) return false;
        value = new CommercialScoutingEvidence(target, route, unknown, stale, hasOldest,
            oldest, cost, days, score);
        return true;
    }

    private static bool TryReadOptional<T>(BinaryReader reader, out T value) where T : class
    {
        value = null;
        if (!TryReadBoolean(reader, out bool present)) return false;
        if (!present) return true;
        if (typeof(T) == typeof(CommercialDecisionEvidence))
        {
            if (!TryReadDecision(reader, out CommercialDecisionEvidence decision)) return false;
            value = decision as T;
            return true;
        }
        if (typeof(T) == typeof(CommercialScoutingEvidence))
        {
            if (!TryReadScouting(reader, out CommercialScoutingEvidence scouting)) return false;
            value = scouting as T;
            return true;
        }
        return false;
    }

    private static bool Validate(P12DNpcActionValue value)
        => value != null && IsRequiredId(value.ActionDefinitionId)
            && IsOptionalId(value.TargetNpcRuntimeId) && IsOptionalId(value.TargetCityRuntimeId)
            && IsOptionalId(value.TargetItemDefinitionId) && value.Amount >= 0
            && IsFiniteNonNegative(value.ExpectedUnitPrice) && IsFiniteNonNegative(value.SuccessChanceMultiplier)
            && Enum.IsDefined(typeof(NpcTravelReason), value.TravelReason) && IsFinite(value.ExpectedNetValue)
            && IsOptionalId(value.OriginDecisionId) && IsOptionalId(value.StableOccurrenceKey)
            && Validate(value.CommercialDecisionEvidence) && Validate(value.CommercialScoutingEvidence);

    private static bool Validate(CommercialDecisionEvidence value)
        => value == null || (IsRequiredId(value.ItemDefinitionId)
            && IsRequiredId(value.CurrentLocationRuntimeId) && IsRequiredId(value.TradeOriginLocationRuntimeId)
            && IsRequiredId(value.TradeDestinationLocationRuntimeId) && IsOptionalId(value.PreviousDestinationLocationRuntimeId)
            && Validate(value.OriginObservation) && Validate(value.DestinationObservation)
            && Validate(value.DestinationLiquidityObservation) && value.ExpectedQuantity >= 0
            && IsFiniteNonNegative(value.ExpectedPurchaseUnitPrice) && IsFiniteNonNegative(value.ExpectedSaleUnitPrice)
            && IsFiniteNonNegative(value.ExpectedTravelCost) && IsFinite(value.ExpectedGrossProfit)
            && IsFinite(value.ExpectedNetProfit) && IsFinite(value.ExpectedScore));

    private static bool Validate(CommercialObservationEvidence value)
        => value == null || (IsRequiredId(value.LocationRuntimeId) && IsFiniteNonNegative(value.KnownPrice)
            && value.KnownStock >= 0 && value.ObservedDay >= 0L && value.ReceivedDay >= value.ObservedDay
            && IsUnitInterval(value.Freshness) && IsValidSource((int)value.Source, value.SourceRuntimeId));

    private static bool Validate(CommercialLiquidityEvidence value)
        => value == null || (IsRequiredId(value.LocationRuntimeId)
            && Enum.IsDefined(typeof(MarketLiquidityMode), value.LiquidityMode)
            && (value.LiquidityMode == MarketLiquidityMode.Open
                ? value.ObservedPurchasingPower == 0f : IsFiniteNonNegative(value.ObservedPurchasingPower))
            && value.ObservedDay >= 0L && value.ReceivedDay >= value.ObservedDay
            && IsUnitInterval(value.Freshness) && IsValidSource((int)value.Source, value.SourceRuntimeId));

    private static bool Validate(CommercialScoutingEvidence value)
        => value == null || (IsRequiredId(value.TargetLocationRuntimeId) && IsRequiredId(value.KnownRouteRuntimeId)
            && value.UnknownObservationCount >= 0 && value.StaleObservationCount >= 0
            && (value.HasOldestObservation ? value.OldestObservationDay >= 0L : value.OldestObservationDay == 0L)
            && IsFiniteNonNegative(value.ExpectedTravelCost) && value.ExpectedTravelDays >= 1
            && IsFiniteNonNegative(value.ExpectedScore));

    private static bool IsValidSource(int source, string sourceRuntimeId)
        => Enum.IsDefined(typeof(CommercialKnowledgeSource), source)
            && ((CommercialKnowledgeSource)source == CommercialKnowledgeSource.SharedByNpc
                ? IsRequiredId(sourceRuntimeId) : sourceRuntimeId == null);

    private static bool ReadRequired(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && IsRequiredId(value);

    internal static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte encoded = reader.ReadByte();
        if (encoded > 1) return false;
        value = encoded == 1;
        return true;
    }

    private static bool IsRuntimeId(string value)
        => IsRequiredId(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static bool IsRequiredId(string value) => !string.IsNullOrWhiteSpace(value);
    private static bool IsOptionalId(string value) => value == null || !string.IsNullOrWhiteSpace(value);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFiniteNonNegative(float value) => IsFinite(value) && value >= 0f;
    private static bool IsUnitInterval(float value) => IsFinite(value) && value >= 0f && value <= 1f;

    private static bool IsLimitException(Exception exception)
        => exception is P12ALimitException || exception is OverflowException;

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;

    private sealed class BoundedMemoryStream : MemoryStream
    {
        private readonly int limit;
        internal BoundedMemoryStream(int limit) { this.limit = limit; }
        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureWithinLimit(count);
            base.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            EnsureWithinLimit(1);
            base.WriteByte(value);
        }
        private void EnsureWithinLimit(int count)
        {
            if (count < 0 || Length > limit - count) throw new P12ALimitException();
        }
    }
}
