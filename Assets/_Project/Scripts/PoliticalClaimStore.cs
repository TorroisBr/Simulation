using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class PoliticalClaimStore
{
    private readonly Dictionary<string, PoliticalClaimRecord> recordsByClaimId =
        new Dictionary<string, PoliticalClaimRecord>(StringComparer.Ordinal);
    private long revision;

    public int Count => recordsByClaimId.Count;
    public long Revision => revision;

    public IReadOnlyList<PoliticalClaimRecord> Records
    {
        get
        {
            List<PoliticalClaimRecord> snapshot = new List<PoliticalClaimRecord>(recordsByClaimId.Values);
            snapshot.Sort(CompareRecords);
            return new ReadOnlyCollection<PoliticalClaimRecord>(snapshot);
        }
    }

    public bool TryRegister(PoliticalClaimRecord record, out PoliticalClaimFailure failure)
    {
        if (record == null
            || record.ClaimId == null
            || record.ClaimantPersonId == null
            || record.Target == null
            || PoliticalClaimRecord.IsTargetCompatible(record.ClaimType, record.Target.Kind) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaim,
                "A valid political claim with a compatible target is required.");
            return false;
        }

        if (recordsByClaimId.ContainsKey(record.ClaimId.Value))
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.DuplicateClaimId,
                "The political claim id is already registered.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RevisionOverflow,
                "The political claim store revision cannot advance further.");
            return false;
        }

        recordsByClaimId.Add(record.ClaimId.Value, record);
        revision++;
        failure = PoliticalClaimFailure.None;
        return true;
    }

    public bool TryGet(PoliticalClaimId claimId, out PoliticalClaimRecord record)
    {
        record = null;
        return claimId != null && recordsByClaimId.TryGetValue(claimId.Value, out record);
    }

    public IReadOnlyList<PoliticalClaimRecord> GetForClaimant(PersonId claimantPersonId)
    {
        List<PoliticalClaimRecord> result = new List<PoliticalClaimRecord>();
        if (claimantPersonId == null)
        {
            return new ReadOnlyCollection<PoliticalClaimRecord>(result);
        }

        foreach (PoliticalClaimRecord record in recordsByClaimId.Values)
        {
            if (record.ClaimantPersonId == claimantPersonId)
            {
                result.Add(record);
            }
        }

        result.Sort(CompareRecords);
        return new ReadOnlyCollection<PoliticalClaimRecord>(result);
    }

    public IReadOnlyList<PoliticalClaimRecord> GetForTarget(PoliticalClaimTarget target)
    {
        List<PoliticalClaimRecord> result = new List<PoliticalClaimRecord>();
        if (target == null)
        {
            return new ReadOnlyCollection<PoliticalClaimRecord>(result);
        }

        foreach (PoliticalClaimRecord record in recordsByClaimId.Values)
        {
            if (record.Target.Equals(target))
            {
                result.Add(record);
            }
        }

        result.Sort(CompareRecords);
        return new ReadOnlyCollection<PoliticalClaimRecord>(result);
    }

    internal bool TryApplyRecognition(
        PoliticalClaimRecognitionTransition transition,
        PoliticalClaimRecord nextRecord,
        out PoliticalClaimFailure failure)
    {
        if (transition == null || nextRecord == null || transition.ClaimId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidTransition,
                "A valid political claim recognition transition is required.");
            return false;
        }

        if (revision != transition.ExpectedStoreRevision)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim store changed after the recognition transition was proposed.");
            return false;
        }

        if (recordsByClaimId.TryGetValue(transition.ClaimId.Value, out PoliticalClaimRecord current) == false
            || ReferenceEquals(current, transition.ExpectedClaim) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim changed after the recognition transition was proposed.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RevisionOverflow,
                "The political claim store revision cannot advance further.");
            return false;
        }

        recordsByClaimId[transition.ClaimId.Value] = nextRecord;
        revision++;
        failure = PoliticalClaimFailure.None;
        return true;
    }

    internal bool TryApplyResolution(
        PoliticalClaimResolutionTransition transition,
        PoliticalClaimRecord nextRecord,
        out PoliticalClaimFailure failure)
    {
        if (transition == null || nextRecord == null || transition.ClaimId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidTransition,
                "A valid political claim resolution transition is required.");
            return false;
        }

        if (revision != transition.ExpectedStoreRevision)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim store changed after the resolution transition was proposed.");
            return false;
        }

        if (recordsByClaimId.TryGetValue(transition.ClaimId.Value, out PoliticalClaimRecord current) == false
            || ReferenceEquals(current, transition.ExpectedClaim) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim changed after the resolution transition was proposed.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RevisionOverflow,
                "The political claim store revision cannot advance further.");
            return false;
        }

        recordsByClaimId[transition.ClaimId.Value] = nextRecord;
        revision++;
        failure = PoliticalClaimFailure.None;
        return true;
    }

    internal PoliticalClaimStore Clone()
    {
        PoliticalClaimStore clone = new PoliticalClaimStore();
        foreach (KeyValuePair<string, PoliticalClaimRecord> entry in recordsByClaimId)
        {
            clone.recordsByClaimId.Add(entry.Key, entry.Value);
        }

        clone.revision = revision;
        return clone;
    }

    private static int CompareRecords(PoliticalClaimRecord left, PoliticalClaimRecord right)
    {
        int claimId = StringComparer.Ordinal.Compare(left.ClaimId.Value, right.ClaimId.Value);
        if (claimId != 0)
        {
            return claimId;
        }

        return StringComparer.Ordinal.Compare(left.ClaimantPersonId.Value, right.ClaimantPersonId.Value);
    }
}
