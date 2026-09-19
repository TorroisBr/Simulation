using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum SuccessionCandidateQueryFailureCode
{
    None = 0,
    InvalidStore = 1,
    InvalidSubject = 2,
    SubjectNotRegistered = 3,
    InvalidCurrentDay = 4,
    InvalidMaturityAge = 5,
    InvalidCalendar = 6,
    CandidateNotRegistered = 7
}

public sealed class SuccessionCandidateQueryFailure : IEquatable<SuccessionCandidateQueryFailure>
{
    private static readonly SuccessionCandidateQueryFailure none =
        new SuccessionCandidateQueryFailure(
            SuccessionCandidateQueryFailureCode.None,
            string.Empty);

    private SuccessionCandidateQueryFailure(
        SuccessionCandidateQueryFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static SuccessionCandidateQueryFailure None => none;
    public SuccessionCandidateQueryFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != SuccessionCandidateQueryFailureCode.None;

    public static SuccessionCandidateQueryFailure Create(
        SuccessionCandidateQueryFailureCode code,
        string message)
    {
        return code == SuccessionCandidateQueryFailureCode.None
            ? None
            : new SuccessionCandidateQueryFailure(code, message);
    }

    public bool Equals(SuccessionCandidateQueryFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SuccessionCandidateQueryFailure);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
        }
    }

    public override string ToString()
    {
        return Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
    }
}

public enum SuccessionCandidateRelation
{
    DirectChild = 0
}

/// <summary>
/// Domain-neutral succession subject. Estate and office integration add their
/// own subject requirements without changing candidate eligibility semantics.
/// </summary>
public sealed class SuccessionSubject
{
    public SuccessionSubject(PersonId subjectPersonId)
    {
        SubjectPersonId = subjectPersonId
            ?? throw new ArgumentNullException(nameof(subjectPersonId));
    }

    public PersonId SubjectPersonId { get; }
}

public sealed class SuccessionCandidateRecord
{
    public SuccessionCandidateRecord(
        PersonId subjectPersonId,
        PersonId candidatePersonId,
        SuccessionCandidateRelation relation,
        long ageInDays,
        long completedYears)
    {
        SubjectPersonId = subjectPersonId
            ?? throw new ArgumentNullException(nameof(subjectPersonId));
        CandidatePersonId = candidatePersonId
            ?? throw new ArgumentNullException(nameof(candidatePersonId));
        Relation = relation;
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
    }

    public PersonId SubjectPersonId { get; }
    public PersonId CandidatePersonId { get; }
    public SuccessionCandidateRelation Relation { get; }
    public long AgeInDays { get; }
    public long CompletedYears { get; }
}

/// <summary>
/// Deterministic candidate observation. It contains no selected successor and
/// performs no mutation. DiscoveryFingerprint lets a later integration
/// transition revalidate genealogy and eligibility before applying an outcome.
/// </summary>
public sealed class SuccessionCandidateSnapshot
{
    public SuccessionCandidateSnapshot(
        SuccessionSubject subject,
        long currentAbsoluteDay,
        long maturityAgeYears,
        IEnumerable<SuccessionCandidateRecord> candidates,
        string discoveryFingerprint)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        if (currentAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(currentAbsoluteDay));
        }

        if (maturityAgeYears < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(maturityAgeYears));
        }

        CurrentAbsoluteDay = currentAbsoluteDay;
        MaturityAgeYears = maturityAgeYears;
        List<SuccessionCandidateRecord> copied =
            new List<SuccessionCandidateRecord>(candidates ?? Array.Empty<SuccessionCandidateRecord>());
        copied.Sort((left, right) => string.CompareOrdinal(
            left.CandidatePersonId.Value,
            right.CandidatePersonId.Value));
        Candidates = new ReadOnlyCollection<SuccessionCandidateRecord>(copied);
        DiscoveryFingerprint = discoveryFingerprint ?? string.Empty;
    }

    public SuccessionSubject Subject { get; }
    public long CurrentAbsoluteDay { get; }
    public long MaturityAgeYears { get; }
    public IReadOnlyList<SuccessionCandidateRecord> Candidates { get; }
    public string DiscoveryFingerprint { get; }

    public bool ContainsCandidate(PersonId personId)
    {
        if (personId == null)
        {
            return false;
        }

        foreach (SuccessionCandidateRecord candidate in Candidates)
        {
            if (candidate.CandidatePersonId == personId)
            {
                return true;
            }
        }

        return false;
    }
}
