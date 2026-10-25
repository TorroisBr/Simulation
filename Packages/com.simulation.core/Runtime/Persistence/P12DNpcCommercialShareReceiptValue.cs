using System;

/// <summary>
/// Detached P12-D value for the legacy commercial-share receipt section.
/// This is persistence data only; Unity's serializable receipt projection
/// remains owned by its host adapter.
/// </summary>
internal sealed class P12DNpcCommercialShareReceiptValue
{
    internal string OperationIdentity { get; }
    internal string Fingerprint { get; }
    internal string BoundaryOccurrenceId { get; }
    internal string SenderRuntimeId { get; }
    internal string SenderPersonId { get; }
    internal string ReceiverRuntimeId { get; }
    internal string ReceiverPersonId { get; }
    internal string SourceSnapshotFingerprint { get; }
    internal int AppliedObservationCount { get; }

    private P12DNpcCommercialShareReceiptValue(
        string operationIdentity, string fingerprint, string boundaryOccurrenceId,
        string senderRuntimeId, string senderPersonId, string receiverRuntimeId,
        string receiverPersonId, string sourceSnapshotFingerprint, int appliedObservationCount)
    {
        OperationIdentity = operationIdentity;
        Fingerprint = fingerprint;
        BoundaryOccurrenceId = boundaryOccurrenceId;
        SenderRuntimeId = senderRuntimeId;
        SenderPersonId = senderPersonId;
        ReceiverRuntimeId = receiverRuntimeId;
        ReceiverPersonId = receiverPersonId;
        SourceSnapshotFingerprint = sourceSnapshotFingerprint;
        AppliedObservationCount = appliedObservationCount;
    }

    internal static bool TryCreateFromOwnerSnapshot(
        string operationIdentity, string fingerprint, string boundaryOccurrenceId,
        string senderRuntimeId, string senderPersonId, string receiverRuntimeId,
        string receiverPersonId, string sourceSnapshotFingerprint,
        int appliedObservationCount, out P12DNpcCommercialShareReceiptValue receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(boundaryOccurrenceId)
            || string.IsNullOrWhiteSpace(senderRuntimeId)
            || string.IsNullOrWhiteSpace(receiverRuntimeId)
            || string.Equals(senderRuntimeId, receiverRuntimeId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(sourceSnapshotFingerprint)
            || string.IsNullOrWhiteSpace(fingerprint) || appliedObservationCount < 0
            || senderPersonId == null || receiverPersonId == null
            || !string.Equals(operationIdentity, StableKeyEncoding.Encode(
                "commercial-sharing-edge/v1", boundaryOccurrenceId,
                senderRuntimeId, receiverRuntimeId), StringComparison.Ordinal))
            return false;

        receipt = new P12DNpcCommercialShareReceiptValue(
            operationIdentity, fingerprint, boundaryOccurrenceId, senderRuntimeId,
            senderPersonId, receiverRuntimeId, receiverPersonId,
            sourceSnapshotFingerprint, appliedObservationCount);
        return true;
    }
}
