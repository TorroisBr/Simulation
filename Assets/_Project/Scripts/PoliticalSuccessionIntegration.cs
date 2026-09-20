using System;

public enum PoliticalSuccessionFailureCode
{
    None = 0,
    InvalidWorld = 1,
    InvalidDecisionId = 2,
    DecisionNotFound = 3,
    DecisionNotCurrent = 4,
    InvalidDecisionKind = 5,
    CandidateNotSelected = 6,
    CandidateNotInDecisionSet = 7,
    CandidateFingerprintMismatch = 8,
    InvalidTransition = 9,
    StaleWorldDay = 10,
    OfficeSuccessionRejected = 11
}

/// <summary>
/// Typed political boundary failure. The existing office succession failure is
/// retained so political selection cannot hide domain validation details.
/// </summary>
public sealed class PoliticalSuccessionFailure : IEquatable<PoliticalSuccessionFailure>
{
    private static readonly PoliticalSuccessionFailure none =
        new PoliticalSuccessionFailure(
            PoliticalSuccessionFailureCode.None,
            string.Empty,
            OfficeSuccessionFailure.None);

    private PoliticalSuccessionFailure(
        PoliticalSuccessionFailureCode code,
        string message,
        OfficeSuccessionFailure officeFailure)
    {
        Code = code;
        Message = message ?? string.Empty;
        OfficeFailure = officeFailure ?? OfficeSuccessionFailure.None;
    }

    public static PoliticalSuccessionFailure None => none;
    public PoliticalSuccessionFailureCode Code { get; }
    public string Message { get; }
    public OfficeSuccessionFailure OfficeFailure { get; }
    public bool IsFailure => Code != PoliticalSuccessionFailureCode.None;

    public static PoliticalSuccessionFailure Create(
        PoliticalSuccessionFailureCode code,
        string message,
        OfficeSuccessionFailure officeFailure = null)
    {
        return code == PoliticalSuccessionFailureCode.None
            ? None
            : new PoliticalSuccessionFailure(code, message, officeFailure);
    }

    public bool Equals(PoliticalSuccessionFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal)
            && Equals(OfficeFailure, other.OfficeFailure);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalSuccessionFailure);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
            return (hash * 397) ^ (OfficeFailure == null ? 0 : OfficeFailure.GetHashCode());
        }
    }

    public override string ToString()
    {
        string result = Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
        return OfficeFailure != null && OfficeFailure.IsFailure
            ? result + " (office: " + OfficeFailure + ")"
            : result;
    }
}

/// <summary>
/// Immutable political selection transition. It wraps, rather than replaces,
/// the validated domain office transition and records the decision that caused
/// the selection. Applying this transition still delegates to office succession.
/// </summary>
public sealed class PoliticalOfficeSuccessionTransition : IEquatable<PoliticalOfficeSuccessionTransition>
{
    internal PoliticalOfficeSuccessionTransition(
        PoliticalDecisionRecord decision,
        OfficeSuccessionTransition officeTransition,
        long expectedWorldDay,
        string expectedCandidateFingerprint)
    {
        Decision = decision ?? throw new ArgumentNullException(nameof(decision));
        OfficeTransition = officeTransition ?? throw new ArgumentNullException(nameof(officeTransition));
        ExpectedWorldDay = expectedWorldDay;
        ExpectedCandidateFingerprint = expectedCandidateFingerprint
            ?? throw new ArgumentNullException(nameof(expectedCandidateFingerprint));
    }

    public PoliticalDecisionRecord Decision { get; }
    public PoliticalDecisionRecord DecisionRecord => Decision;
    public OfficeSuccessionTransition OfficeTransition { get; }
    public OfficeSuccessionTransition DomainTransition => OfficeTransition;
    public OfficeId OfficeId => OfficeTransition.OfficeId;
    public PersonId SelectedCandidateId => OfficeTransition.SelectedCandidateId;
    public long ExpectedWorldDay { get; }
    public string ExpectedCandidateFingerprint { get; }
    public string AuthoritativeTransitionCandidateFingerprint =>
        OfficeTransition.ExpectedCandidateFingerprint;

    public bool Equals(PoliticalOfficeSuccessionTransition other)
    {
        return other != null
            && ReferenceEquals(Decision, other.Decision)
            && OfficeTransition.Equals(other.OfficeTransition)
            && ExpectedWorldDay == other.ExpectedWorldDay
            && string.Equals(
                ExpectedCandidateFingerprint,
                other.ExpectedCandidateFingerprint,
                StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalOfficeSuccessionTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Decision.GetHashCode();
            hash = (hash * 397) ^ OfficeTransition.GetHashCode();
            hash = (hash * 397) ^ ExpectedWorldDay.GetHashCode();
            return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ExpectedCandidateFingerprint);
        }
    }
}

/// <summary>
/// Knowledge-driven office succession boundary. It may select a candidate, but
/// it owns no office, vacancy, genealogy, or Person mutation authority.
/// </summary>
public static class PoliticalSuccessionSystem
{
    public static bool TryPropose(
        SimulationRuntime world,
        PoliticalDecisionId decisionId,
        OfficeId officeId,
        long startAbsoluteDay,
        out PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        transition = null;
        failure = PoliticalSuccessionFailure.None;
        if (world == null)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.InvalidWorld,
                "A SimulationRuntime is required.");
            return false;
        }

        if (decisionId == null)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.InvalidDecisionId,
                "A political decision id is required.");
            return false;
        }

        if (world.TryGetPoliticalDecision(decisionId, out PoliticalDecisionRecord decision) == false)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.DecisionNotFound,
                "The political succession decision is not registered in this world.");
            return false;
        }

        if (decision.DecisionAbsoluteDay != world.CurrentDay)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.DecisionNotCurrent,
                "Political succession requires a decision recorded for the current world day.");
            return false;
        }

        if (decision.DecisionKind != PoliticalDecisionKind.SuccessionSelection)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.InvalidDecisionKind,
                "The decision must be a succession selection.");
            return false;
        }

        PersonId selectedCandidateId = decision.Outcome?.SelectedCandidatePersonId;
        if (decision.Outcome == null
            || decision.Outcome.Kind != PoliticalDecisionOutcomeKind.CandidateSelected
            || selectedCandidateId == null)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.CandidateNotSelected,
                "A succession decision must select a candidate PersonId.");
            return false;
        }

        if (ContainsCandidate(decision, selectedCandidateId) == false)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.CandidateNotInDecisionSet,
                "The selected candidate is not in the decision's deterministic candidate set.");
            return false;
        }

        if (world.TryProposeOfficeSuccession(
                officeId,
                selectedCandidateId,
                startAbsoluteDay,
                out OfficeSuccessionTransition officeTransition,
                out OfficeSuccessionFailure officeFailure) == false)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.OfficeSuccessionRejected,
                "The current world rejected the office succession proposal.",
                officeFailure);
            return false;
        }

        string authoritativeCandidateSetFingerprint =
            BuildCandidateSetFingerprint(officeTransition);
        if (string.Equals(
                decision.CandidateFingerprint,
                authoritativeCandidateSetFingerprint,
                StringComparison.Ordinal) == false)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.CandidateFingerprintMismatch,
                "The decision candidate fingerprint does not match the authoritative succession candidates.");
            return false;
        }

        transition = new PoliticalOfficeSuccessionTransition(
            decision,
            officeTransition,
            world.CurrentDay,
            decision.CandidateFingerprint);
        return true;
    }

    public static bool TryApply(
        SimulationRuntime world,
        PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        failure = PoliticalSuccessionFailure.None;
        if (world == null)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.InvalidWorld,
                "A SimulationRuntime is required.");
            return false;
        }

        if (transition == null
            || transition.Decision == null
            || transition.OfficeTransition == null
            || transition.ExpectedCandidateFingerprint == null)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.InvalidTransition,
                "A valid political office succession transition is required.");
            return false;
        }

        if (transition.ExpectedWorldDay != world.CurrentDay)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.StaleWorldDay,
                "The world day changed after political succession was proposed.");
            return false;
        }

        if (transition.Decision.DecisionAbsoluteDay != world.CurrentDay
            || string.Equals(
                transition.Decision.CandidateFingerprint,
                transition.ExpectedCandidateFingerprint,
                StringComparison.Ordinal) == false
            || string.Equals(
                transition.Decision.CandidateFingerprint,
                BuildCandidateSetFingerprint(transition.OfficeTransition),
                StringComparison.Ordinal) == false
            || world.TryGetPoliticalDecision(
                transition.Decision.DecisionId,
                out PoliticalDecisionRecord currentDecision) == false
            || ReferenceEquals(currentDecision, transition.Decision) == false)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.DecisionNotCurrent,
                "The political succession decision is no longer the current authoritative decision.");
            return false;
        }

        if (world.TryApplyOfficeSuccession(
                transition.OfficeTransition,
                out OfficeSuccessionFailure officeFailure) == false)
        {
            failure = PoliticalSuccessionFailure.Create(
                PoliticalSuccessionFailureCode.OfficeSuccessionRejected,
                "The current world rejected the office succession application.",
                officeFailure);
            return false;
        }

        return true;
    }

    private static string BuildCandidateSetFingerprint(
        OfficeSuccessionTransition officeTransition)
    {
        if (officeTransition == null || officeTransition.ExpectedCandidates == null)
        {
            return string.Empty;
        }

        PersonId[] candidateIds = new PersonId[officeTransition.ExpectedCandidates.Candidates.Count];
        for (int index = 0; index < candidateIds.Length; index++)
        {
            candidateIds[index] = officeTransition.ExpectedCandidates.Candidates[index].CandidatePersonId;
        }

        return PoliticalDecisionRecord.BuildCandidateFingerprint(candidateIds);
    }

    private static bool ContainsCandidate(
        PoliticalDecisionRecord decision,
        PersonId candidateId)
    {
        foreach (PersonId candidate in decision.CandidatePersonIds)
        {
            if (candidate == candidateId)
            {
                return true;
            }
        }

        return false;
    }
}
