using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 wire codec for retained terminal P11 actor choices.</summary>
internal static class P12AActorChoiceCodec
{
    private const string PayloadContract = "p12a.actor-choice-input/v1";
    private const string OwnerPrefix = "input/";

    internal static bool TryEncode(P12FActorChoiceSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, ActorChoiceP11CensusProvider.SectionId);
        if (!IsValidSnapshot(snapshot)) return false;

        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Inputs.Count);
            foreach (ActorChoiceInput input in snapshot.Inputs)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    writer.Write(snapshot.NextInputSequence);
                    WriteInput(writer, input);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            ActorChoiceP11CensusProvider.SectionId, OwnerPrefix + input.InputId.Value);
                        return false;
                    }
                    // ActorChoiceStore has one owner-wide census revision and no per-input revision.
                    rows.Add(new P12AOwnerRow(OwnerPrefix + input.InputId.Value, 0L, stream.ToArray()));
                }
            }

            section = new P12AOwnerSection(ActorChoiceP11CensusProvider.SectionId,
                ActorChoiceP11CensusProvider.SchemaVersion, 0, OwnerSectionRole.Required, null,
                snapshot.CensusRevision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException || exception is EncoderFallbackException)
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                ActorChoiceP11CensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12FActorChoiceSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, ActorChoiceP11CensusProvider.SectionId);
        if (section == null || section.Id != ActorChoiceP11CensusProvider.SectionId
            || section.Major != ActorChoiceP11CensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0 || section.Rows.Count > P12AContinuationCodec.MaxRows)
            return false;

        try
        {
            List<ActorChoiceInput> inputs = new List<ActorChoiceInput>(section.Rows.Count);
            HashSet<string> inputIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> commandIds = new HashSet<string>(StringComparer.Ordinal);
            long nextSequence = 1L;
            long priorSequence = 0L;
            for (int i = 0; i < section.Rows.Count; i++)
            {
                P12AOwnerRow row = section.Rows[i];
                if (row == null || row.Revision != 0L || row.Payload == null
                    || !TryDecodeRow(row, out long rowNextSequence, out ActorChoiceInput input)
                    || rowNextSequence <= 0L || (i > 0 && rowNextSequence != nextSequence)
                    || input.InputSequence <= priorSequence
                    || row.OwnerKey != OwnerPrefix + input.InputId.Value
                    || !inputIds.Add(input.InputId.Value) || !commandIds.Add(input.WorldCommandId)
                    || !ActorChoiceStore.IsValidP12FDailyTerminalInput(input))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        ActorChoiceP11CensusProvider.SectionId, row?.OwnerKey);
                    return false;
                }
                if (i == 0) nextSequence = rowNextSequence;
                priorSequence = input.InputSequence;
                inputs.Add(input);
            }

            if (inputs.Count == 0)
            {
                if (section.Revision != 0L) return false;
                nextSequence = 1L;
            }
            if (nextSequence <= priorSequence) return false;

            P12FActorChoiceSnapshot candidate = new P12FActorChoiceSnapshot(
                P12FActorChoiceSnapshot.CurrentSchemaVersion, inputs, nextSequence, section.Revision);
            if (!IsValidSnapshot(candidate)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException
            || exception is OverflowException)
        {
            snapshot = null;
            return false;
        }
    }

    private static bool IsValidSnapshot(P12FActorChoiceSnapshot snapshot)
    {
        if (snapshot == null || snapshot.SchemaVersion != P12FActorChoiceSnapshot.CurrentSchemaVersion
            || snapshot.Inputs == null || snapshot.Inputs.Count > P12AContinuationCodec.MaxRows
            || snapshot.NextInputSequence <= 0L || snapshot.CensusRevision < snapshot.Inputs.Count)
            return false;

        HashSet<string> inputIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> commandIds = new HashSet<string>(StringComparer.Ordinal);
        long priorSequence = 0L;
        foreach (ActorChoiceInput input in snapshot.Inputs)
        {
            if (input == null || input.InputId == null || string.IsNullOrWhiteSpace(input.InputId.Value)
                || string.IsNullOrWhiteSpace(input.WorldCommandId) || input.InputSequence <= priorSequence
                || !inputIds.Add(input.InputId.Value) || !commandIds.Add(input.WorldCommandId)
                || !ActorChoiceStore.IsValidP12FDailyTerminalInput(input)) return false;
            priorSequence = input.InputSequence;
        }
        if (snapshot.NextInputSequence <= priorSequence) return false;
        return snapshot.Inputs.Count != 0 || (snapshot.NextInputSequence == 1L && snapshot.CensusRevision == 0L);
    }

    private static void WriteInput(BinaryWriter writer, ActorChoiceInput input)
    {
        P12AWire.WriteString(writer, input.InputId.Value);
        P12AWire.WriteString(writer, input.WorldCommandId);
        writer.Write(input.InputSequence);
        P12AWire.WriteString(writer, input.PersonId.Value);
        P12AWire.WriteString(writer, input.ActionDefinitionId);
        writer.Write((ushort)input.Origin);
        writer.Write((ushort)input.Authority);
        writer.Write(input.CapturedAbsoluteDay);
        writer.Write((ushort)input.Status);
        writer.Write(input.Dispositions.Count);
        foreach (ActorChoiceDisposition disposition in input.Dispositions)
        {
            writer.Write(disposition.TransitionOrdinal);
            writer.Write((ushort)disposition.Kind);
            writer.Write(disposition.AbsoluteDay);
            writer.Write(disposition.ActorTurnRosterOrdinal);
            WriteNullableEnum(writer, disposition.DeferralReason);
            WriteNullableEnum(writer, disposition.Failure);
            P12AWire.WriteString(writer, disposition.DecisionRecordId);
            WriteNullableEnum(writer, disposition.AttemptOutcome);
            WriteNullableEnum(writer, disposition.ReturnedResultStatus);
        }
    }

    private static bool TryDecodeRow(P12AOwnerRow row, out long nextInputSequence, out ActorChoiceInput input)
    {
        nextInputSequence = 0L;
        input = null;
        using (MemoryStream stream = new MemoryStream(row.Payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract) return false;
            nextInputSequence = reader.ReadInt64();
            if (!P12AWire.ReadString(reader, out string inputId) || string.IsNullOrWhiteSpace(inputId)
                || !P12AWire.ReadString(reader, out string commandId) || string.IsNullOrWhiteSpace(commandId)) return false;
            long inputSequence = reader.ReadInt64();
            if (!P12AWire.ReadString(reader, out string personId) || string.IsNullOrWhiteSpace(personId)
                || !P12AWire.ReadString(reader, out string actionId) || string.IsNullOrWhiteSpace(actionId)) return false;
            if (!ReadEnum(reader, out WorldCommandOrigin origin)
                || !ReadEnum(reader, out WorldCommandAuthorityMode authority)) return false;
            long capturedDay = reader.ReadInt64();
            if (!ReadEnum(reader, out ActorChoiceInputStatus status)) return false;
            int dispositionCount = reader.ReadInt32();
            if (dispositionCount < 0 || dispositionCount > P12AContinuationCodec.MaxRows) return false;
            List<ActorChoiceDisposition> dispositions = new List<ActorChoiceDisposition>(dispositionCount);
            for (int i = 0; i < dispositionCount; i++)
            {
                long ordinal = reader.ReadInt64();
                if (!ReadEnum(reader, out ActorChoiceDispositionKind kind)) return false;
                long absoluteDay = reader.ReadInt64();
                int rosterOrdinal = reader.ReadInt32();
                if (!ReadNullableEnum(reader, out ActorChoiceDeferralReason? deferral)
                    || !ReadNullableEnum(reader, out ActorChoiceFailure? failure)
                    || !P12AWire.ReadString(reader, out string decisionId)
                    || (decisionId != null && string.IsNullOrWhiteSpace(decisionId))
                    || !ReadNullableEnum(reader, out ActorChoiceAttemptOutcome? outcome)
                    || !ReadNullableEnum(reader, out NpcActionResultType? resultStatus)) return false;
                dispositions.Add(new ActorChoiceDisposition(ordinal, kind, absoluteDay, rosterOrdinal,
                    deferral, failure, decisionId, outcome, resultStatus));
            }
            if (stream.Position != stream.Length) return false;
            input = new ActorChoiceInput(new ActorChoiceInputId(inputId), commandId, inputSequence,
                new PersonId(personId), actionId, origin, authority, capturedDay, status, dispositions);
            return true;
        }
    }

    private static void WriteNullableEnum<T>(BinaryWriter writer, T? value) where T : struct, Enum
    {
        writer.Write((byte)(value.HasValue ? 1 : 0));
        if (value.HasValue) writer.Write(Convert.ToUInt16(value.Value));
    }

    private static bool ReadNullableEnum<T>(BinaryReader reader, out T? value) where T : struct, Enum
    {
        value = null;
        byte present = reader.ReadByte();
        if (present == 0) return true;
        if (present != 1 || !ReadEnum(reader, out T decoded)) return false;
        value = decoded;
        return true;
    }

    private static bool ReadEnum<T>(BinaryReader reader, out T value) where T : struct, Enum
    {
        value = default;
        ushort raw = reader.ReadUInt16();
        T decoded = (T)Enum.ToObject(typeof(T), raw);
        if (!Enum.IsDefined(typeof(T), decoded)) return false;
        value = decoded;
        return true;
    }
}
