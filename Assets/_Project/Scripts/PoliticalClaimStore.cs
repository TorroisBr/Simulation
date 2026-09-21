using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class PoliticalClaimStore
{
    private readonly Dictionary<string, PoliticalClaimRecord> recordsByClaimId =
        new Dictionary<string, PoliticalClaimRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, PoliticalClaimRecognitionRecord> recognitionsByKey =
        new Dictionary<string, PoliticalClaimRecognitionRecord>(StringComparer.Ordinal);
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

    public IReadOnlyList<PoliticalClaimRecognitionRecord> RecognitionRecords
    {
        get
        {
            List<PoliticalClaimRecognitionRecord> snapshot =
                new List<PoliticalClaimRecognitionRecord>(recognitionsByKey.Values);
            snapshot.Sort(CompareRecognitionRecords);
            return new ReadOnlyCollection<PoliticalClaimRecognitionRecord>(snapshot);
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

    public bool TryGetRecognition(
        PoliticalClaimId claimId,
        InstitutionId institutionId,
        out PoliticalClaimRecognitionRecord record)
    {
        record = null;
        string key = RecognitionKey(claimId, institutionId);
        return key != null && recognitionsByKey.TryGetValue(key, out record);
    }

    internal bool TryRegisterRecognition(
        PoliticalClaimRecognitionRecord record,
        out PoliticalClaimFailure failure)
    {
        if (record == null || record.ClaimId == null || record.InstitutionId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaim,
                "A claim recognition relation requires a claim and institution.");
            return false;
        }

        if (recordsByClaimId.ContainsKey(record.ClaimId.Value) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaimId,
                "The recognition claim is not registered.");
            return false;
        }

        string key = RecognitionKey(record.ClaimId, record.InstitutionId);
        if (recognitionsByKey.ContainsKey(key))
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.DuplicateClaimId,
                "The claim/institution recognition relation is already registered.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RevisionOverflow,
                "The political claim store revision cannot advance further.");
            return false;
        }

        recognitionsByKey.Add(key, record);
        revision++;
        failure = PoliticalClaimFailure.None;
        return true;
    }

    public IReadOnlyList<PoliticalClaimRecognitionRecord> GetRecognitionsForClaim(PoliticalClaimId claimId)
    {
        List<PoliticalClaimRecognitionRecord> result = new List<PoliticalClaimRecognitionRecord>();
        if (claimId != null)
        {
            foreach (PoliticalClaimRecognitionRecord record in recognitionsByKey.Values)
            {
                if (record.ClaimId == claimId)
                {
                    result.Add(record);
                }
            }
        }

        result.Sort(CompareRecognitionRecords);
        return new ReadOnlyCollection<PoliticalClaimRecognitionRecord>(result);
    }

    internal bool TryApplyRecognition(
        PoliticalClaimRecognitionTransition transition,
        PoliticalClaimRecognitionRecord nextRecord,
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

        string key = RecognitionKey(transition.ClaimId, transition.RecognizingInstitutionId);
        recognitionsByKey.TryGetValue(key, out PoliticalClaimRecognitionRecord current);
        if ((current == null && transition.ExpectedRecognition != null)
            || (current != null && ReferenceEquals(current, transition.ExpectedRecognition) == false))
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

        recognitionsByKey[key] = nextRecord;
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

        foreach (KeyValuePair<string, PoliticalClaimRecognitionRecord> entry in recognitionsByKey)
        {
            clone.recognitionsByKey.Add(entry.Key, entry.Value);
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

    private static int CompareRecognitionRecords(
        PoliticalClaimRecognitionRecord left,
        PoliticalClaimRecognitionRecord right)
    {
        int claim = StringComparer.Ordinal.Compare(left.ClaimId.Value, right.ClaimId.Value);
        return claim != 0
            ? claim
            : StringComparer.Ordinal.Compare(left.InstitutionId.Value, right.InstitutionId.Value);
    }

    private static string RecognitionKey(PoliticalClaimId claimId, InstitutionId institutionId)
    {
        return PoliticalClaimRecognitionRecord.BuildRecognitionId(claimId, institutionId);
    }
}
