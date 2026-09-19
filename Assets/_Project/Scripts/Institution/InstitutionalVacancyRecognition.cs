using System;

public enum InstitutionalVacancyRecognitionFailure
{
    None = 0,
    InvalidStore = 1,
    InvalidOfficeId = 2,
    OfficeNotFound = 3,
    OfficeAlreadyVacant = 4,
    InvalidRecognitionDay = 5,
    InvalidRecognitionReason = 6,
    StaleIncumbency = 7,
    IncumbentNotFactuallyDead = 8,
    InvalidTransition = 9
}

/// <summary>
/// Explicit institutional recognition that an occupied office is vacant.
/// Producing this transition is a separate step from factual Person death.
/// </summary>
public sealed class InstitutionalVacancyRecognitionTransition : IEquatable<InstitutionalVacancyRecognitionTransition>
{
    internal OfficeIncumbency ExpectedIncumbency { get; }
    public OfficeId OfficeId { get; }
    public PersonId ExpectedIncumbent { get; }
    public long RecognitionAbsoluteDay { get; }
    public InstitutionalVacancyRecognitionReason Reason { get; }

    internal InstitutionalVacancyRecognitionTransition(
        OfficeIncumbency expectedIncumbency,
        long recognitionAbsoluteDay,
        InstitutionalVacancyRecognitionReason reason)
    {
        ExpectedIncumbency = expectedIncumbency;
        OfficeId = expectedIncumbency?.OfficeId;
        ExpectedIncumbent = expectedIncumbency?.Incumbent;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        Reason = reason;
    }

    public bool Equals(InstitutionalVacancyRecognitionTransition other)
    {
        return other != null
            && OfficeId == other.OfficeId
            && ExpectedIncumbent == other.ExpectedIncumbent
            && RecognitionAbsoluteDay == other.RecognitionAbsoluteDay
            && Reason == other.Reason
            && (ExpectedIncumbency?.StartAbsoluteDay == other.ExpectedIncumbency?.StartAbsoluteDay);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as InstitutionalVacancyRecognitionTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = OfficeId != null ? OfficeId.GetHashCode() : 0;
            hash = (hash * 397) ^ (ExpectedIncumbent != null ? ExpectedIncumbent.GetHashCode() : 0);
            hash = (hash * 397) ^ RecognitionAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ (int)Reason;
            return (hash * 397) ^ (ExpectedIncumbency?.StartAbsoluteDay?.GetHashCode() ?? 0);
        }
    }
}

/// <summary>
/// Pure proposal/apply boundary for institutional vacancy recognition. It does
/// not run from AdvanceDay and never infers institutional vacancy automatically
/// from a factual death.
/// </summary>
public static class InstitutionalVacancyRecognitionSystem
{
    public static bool TryPropose(
        OfficeStore officeStore,
        OfficeId officeId,
        long recognitionAbsoluteDay,
        InstitutionalVacancyRecognitionReason reason,
        out InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        transition = null;
        if (officeStore == null)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidStore;
            return false;
        }

        if (officeId == null)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidOfficeId;
            return false;
        }

        if (recognitionAbsoluteDay < 0L)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidRecognitionDay;
            return false;
        }

        if (Enum.IsDefined(typeof(InstitutionalVacancyRecognitionReason), reason) == false)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidRecognitionReason;
            return false;
        }

        if (officeStore.TryGet(officeId, out _) == false)
        {
            failure = InstitutionalVacancyRecognitionFailure.OfficeNotFound;
            return false;
        }

        if (officeStore.TryGetIncumbency(officeId, out OfficeIncumbency incumbency) == false)
        {
            failure = InstitutionalVacancyRecognitionFailure.OfficeAlreadyVacant;
            return false;
        }

        if (incumbency.StartAbsoluteDay.HasValue
            && recognitionAbsoluteDay < incumbency.StartAbsoluteDay.Value)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidRecognitionDay;
            return false;
        }

        transition = new InstitutionalVacancyRecognitionTransition(
            incumbency,
            recognitionAbsoluteDay,
            reason);
        failure = InstitutionalVacancyRecognitionFailure.None;
        return true;
    }

    public static bool TryProposeForFactualDeath(
        OfficeStore officeStore,
        OfficeId officeId,
        PersonRuntime incumbent,
        long recognitionAbsoluteDay,
        out InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        transition = null;
        if (incumbent == null
            || incumbent.DeathAbsoluteDay.HasValue == false
            || incumbent.DeathAbsoluteDay.Value > recognitionAbsoluteDay)
        {
            failure = InstitutionalVacancyRecognitionFailure.IncumbentNotFactuallyDead;
            return false;
        }

        if (officeStore == null
            || officeStore.TryGetIncumbency(officeId, out OfficeIncumbency current) == false
            || current.Incumbent != incumbent.PersonId)
        {
            failure = InstitutionalVacancyRecognitionFailure.StaleIncumbency;
            return false;
        }

        return TryPropose(
            officeStore,
            officeId,
            recognitionAbsoluteDay,
            InstitutionalVacancyRecognitionReason.FactualDeath,
            out transition,
            out failure);
    }

    public static bool TryApply(
        OfficeStore officeStore,
        InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        failure = InstitutionalVacancyRecognitionFailure.None;
        if (officeStore == null)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidStore;
            return false;
        }

        if (transition == null
            || transition.OfficeId == null
            || transition.ExpectedIncumbent == null
            || transition.ExpectedIncumbency == null
            || transition.RecognitionAbsoluteDay < 0L)
        {
            failure = InstitutionalVacancyRecognitionFailure.InvalidTransition;
            return false;
        }

        if (officeStore.TryGetIncumbency(
                transition.OfficeId,
                out OfficeIncumbency current) == false)
        {
            failure = InstitutionalVacancyRecognitionFailure.OfficeAlreadyVacant;
            return false;
        }

        if (current.Incumbent != transition.ExpectedIncumbent
            || current.StartAbsoluteDay != transition.ExpectedIncumbency.StartAbsoluteDay)
        {
            failure = InstitutionalVacancyRecognitionFailure.StaleIncumbency;
            return false;
        }

        if (officeStore.TryVacateOffice(
                transition.OfficeId,
                transition.RecognitionAbsoluteDay,
                transition.Reason,
                out InstitutionFoundationFailure officeFailure) == false)
        {
            failure = officeFailure.Code == InstitutionFoundationFailureCode.OfficeAlreadyVacant
                ? InstitutionalVacancyRecognitionFailure.OfficeAlreadyVacant
                : InstitutionalVacancyRecognitionFailure.StaleIncumbency;
            return false;
        }

        return true;
    }
}
