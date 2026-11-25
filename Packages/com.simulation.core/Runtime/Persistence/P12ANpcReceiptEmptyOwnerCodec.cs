using System;

/// <summary>
/// Daily-v1 exact-empty witnesses for the two P18 receipt owners deliberately excluded by the accepted profile.
/// The codec serializes only the Required empty owner sections; it does not serialize P18 receipt data.
/// </summary>
internal static class P12ANpcReceiptEmptyOwnerCodec
{
    internal static bool TryEncode(string runtimeId,
        OwnerSectionCensusSnapshot localObservationWitness,
        OwnerSectionCensusSnapshot merchantTradeStateWitness,
        out P12AOwnerSection localObservation, out P12AOwnerSection merchantTradeState,
        out P12AResult result)
    {
        localObservation = merchantTradeState = null;
        string localId = DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix + (runtimeId ?? string.Empty);
        string merchantId = DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsPrefix + (runtimeId ?? string.Empty);
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, localId, runtimeId);
        if (!IsRuntimeId(runtimeId) || localObservationWitness == null || merchantTradeStateWitness == null
            || !IsExactEmpty(localObservationWitness, localId)
            || !IsExactEmpty(merchantTradeStateWitness, merchantId)
            || ReferenceEquals(localObservationWitness.OwnerInstanceIdentity,
                merchantTradeStateWitness.OwnerInstanceIdentity)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(localId);
            P12AWire.GetEncodedStringByteCount(merchantId);
        }
        catch (Exception exception) when (exception is P12ALimitException
            || exception is ArgumentException || exception is OverflowException)
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, localId, runtimeId);
            return false;
        }
        localObservation = EmptySection(localId);
        merchantTradeState = EmptySection(merchantId);
        result = null;
        return true;
    }

    internal static bool TryDecode(P12AOwnerSection localObservation,
        P12AOwnerSection merchantTradeState, out string runtimeId, out P12AResult result)
    {
        runtimeId = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            localObservation?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix);
        if (localObservation == null || merchantTradeState == null
            || string.IsNullOrWhiteSpace(localObservation.Id)
            || !localObservation.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix,
                StringComparison.Ordinal)) return false;
        string candidate = localObservation.Id.Substring(
            DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsPrefix.Length);
        if (!IsRuntimeId(candidate)) return false;
        string localId = DailyV1SelectedOwnerSectionCensusContract.NpcLocalObservationReceiptsSectionIdFor(candidate);
        string merchantId = DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradeStateReceiptsSectionIdFor(candidate);
        if (!IsRuntimeId(candidate) || !IsExactEmpty(localObservation, localId)
            || !IsExactEmpty(merchantTradeState, merchantId)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(localId);
            P12AWire.GetEncodedStringByteCount(merchantId);
        }
        catch (Exception exception) when (exception is P12ALimitException
            || exception is ArgumentException || exception is OverflowException)
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, localId, candidate);
            return false;
        }
        runtimeId = candidate;
        result = null;
        return true;
    }

    private static P12AOwnerSection EmptySection(string id)
        => new P12AOwnerSection(id, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
            OwnerSectionRole.Required, null, 0L, Array.Empty<P12AOwnerRow>());

    private static bool IsExactEmpty(OwnerSectionCensusSnapshot witness, string id)
        => witness != null && witness.SectionId == id
            && witness.SchemaVersion == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
            && witness.Role == OwnerSectionRole.Required && witness.Cardinality == 0
            && witness.Revision == 0L && witness.OwnerInstanceIdentity != null;

    private static bool IsExactEmpty(P12AOwnerSection section, string id)
        => section != null && section.Id == id
            && section.Major == DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required && section.AliasGroup == null
            && section.Revision == 0L && section.Rows != null && section.Rows.Count == 0;

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;
}
