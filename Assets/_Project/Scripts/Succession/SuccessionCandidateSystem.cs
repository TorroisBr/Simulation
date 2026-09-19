using System;
using System.Collections.Generic;
using System.Text;

public static class SuccessionCandidateSystem
{
    public static bool TryBuildCandidates(
        PersonStore personStore,
        GenealogyStore genealogyStore,
        SuccessionSubject subject,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        long maturityAgeYears,
        out SuccessionCandidateSnapshot snapshot,
        out SuccessionCandidateQueryFailure failure)
    {
        snapshot = null;
        failure = SuccessionCandidateQueryFailure.None;

        if (personStore == null || genealogyStore == null || calendar == null)
        {
            failure = SuccessionCandidateQueryFailure.Create(
                SuccessionCandidateQueryFailureCode.InvalidStore,
                "PersonStore, GenealogyStore, and SimulationCalendar are required.");
            return false;
        }

        if (subject == null || subject.SubjectPersonId == null)
        {
            failure = SuccessionCandidateQueryFailure.Create(
                SuccessionCandidateQueryFailureCode.InvalidSubject,
                "A succession subject PersonId is required.");
            return false;
        }

        if (currentAbsoluteDay < 0L)
        {
            failure = SuccessionCandidateQueryFailure.Create(
                SuccessionCandidateQueryFailureCode.InvalidCurrentDay,
                "CurrentAbsoluteDay cannot be negative.");
            return false;
        }

        if (maturityAgeYears < 0L)
        {
            failure = SuccessionCandidateQueryFailure.Create(
                SuccessionCandidateQueryFailureCode.InvalidMaturityAge,
                "MaturityAgeYears cannot be negative.");
            return false;
        }

        if (personStore.TryGet(subject.SubjectPersonId, out _) == false)
        {
            failure = SuccessionCandidateQueryFailure.Create(
                SuccessionCandidateQueryFailureCode.SubjectNotRegistered,
                "The succession subject PersonId must be registered.");
            return false;
        }

        List<PersonId> directChildren = new List<PersonId>(
            genealogyStore.GetChildren(subject.SubjectPersonId));
        directChildren.Sort((left, right) => string.CompareOrdinal(left.Value, right.Value));

        List<SuccessionCandidateRecord> candidates =
            new List<SuccessionCandidateRecord>();
        StringBuilder fingerprint = new StringBuilder();
        fingerprint.Append(subject.SubjectPersonId.Value).Append('|');
        StringBuilder eligibleFingerprint = new StringBuilder();

        foreach (PersonId childId in directChildren)
        {
            fingerprint.Append(childId.Value).Append(';');
            if (personStore.TryGet(childId, out PersonRuntime child) == false)
            {
                failure = SuccessionCandidateQueryFailure.Create(
                    SuccessionCandidateQueryFailureCode.CandidateNotRegistered,
                    "Every genealogical candidate endpoint must be registered.");
                return false;
            }

            if (child.IsDeadAt(currentAbsoluteDay))
            {
                continue;
            }

            if (PersonMaturityQuery.TryCalculate(
                    child,
                    currentAbsoluteDay,
                    calendar,
                    maturityAgeYears,
                    out PersonMaturitySnapshot maturity,
                    out PersonMaturityQueryFailure maturityFailure) == false)
            {
                if (maturityFailure == PersonMaturityQueryFailure.InvalidCalendar
                    || maturityFailure == PersonMaturityQueryFailure.InvalidCurrentDay
                    || maturityFailure == PersonMaturityQueryFailure.InvalidMaturityAge)
                {
                    failure = SuccessionCandidateQueryFailure.Create(
                        SuccessionCandidateQueryFailureCode.InvalidCalendar,
                        "The maturity query could not evaluate the succession candidate set.");
                    return false;
                }

                continue;
            }

            if (maturity.IsMature == false)
            {
                continue;
            }

            candidates.Add(new SuccessionCandidateRecord(
                subject.SubjectPersonId,
                child.PersonId,
                SuccessionCandidateRelation.DirectChild,
                maturity.AgeInDays,
                maturity.CompletedYears));
            eligibleFingerprint.Append(child.PersonId.Value).Append(';');
        }

        candidates.Sort((left, right) => string.CompareOrdinal(
            left.CandidatePersonId.Value,
            right.CandidatePersonId.Value));
        snapshot = new SuccessionCandidateSnapshot(
            subject,
            currentAbsoluteDay,
            maturityAgeYears,
            candidates,
            fingerprint.Append("eligible:").Append(eligibleFingerprint).ToString());
        return true;
    }
}
