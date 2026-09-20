using System;

/// <summary>
/// Immutable policy inputs for a derived legitimacy assessment. This is a
/// snapshot of facts owned by other domains; it is not a world-state record.
/// </summary>
public sealed class PoliticalLegitimacyInputs
{
    public PersonId CandidatePersonId { get; }
    public bool IsAlive { get; }
    public bool IsMature { get; }
    public bool IsEligible { get; }
    public PoliticalClaimRecognitionState ClaimRecognitionState { get; }
    public int SupportCount { get; }
    public int OpposeCount { get; }

    public PoliticalLegitimacyInputs(
        PersonId candidatePersonId,
        bool isAlive,
        bool isMature,
        bool isEligible,
        PoliticalClaimRecognitionState claimRecognitionState,
        int supportCount,
        int opposeCount)
    {
        CandidatePersonId = candidatePersonId ?? throw new ArgumentNullException(nameof(candidatePersonId));
        if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), claimRecognitionState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(claimRecognitionState));
        }

        if (supportCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(supportCount));
        }

        if (opposeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(opposeCount));
        }

        IsAlive = isAlive;
        IsMature = isMature;
        IsEligible = isEligible;
        ClaimRecognitionState = claimRecognitionState;
        SupportCount = supportCount;
        OpposeCount = opposeCount;
    }
}

/// <summary>
/// Explicit weights used by <see cref="PoliticalLegitimacy.Assess"/>.
/// Signed weights are allowed so a policy can penalize facts such as
/// opposition. No policy is installed globally or mutated by the assessment.
/// </summary>
public sealed class PoliticalLegitimacyPolicy
{
    public long AliveWeight { get; }
    public long MatureWeight { get; }
    public long EligibleWeight { get; }
    public long RecognizedClaimWeight { get; }
    public long ContestedClaimWeight { get; }
    public long UnrecognizedClaimWeight { get; }
    public long RejectedClaimWeight { get; }
    public long SupportWeight { get; }
    public long OpposeWeight { get; }
    public long Threshold { get; }

    public PoliticalLegitimacyPolicy(
        long aliveWeight,
        long matureWeight,
        long eligibleWeight,
        long recognizedClaimWeight,
        long contestedClaimWeight,
        long unrecognizedClaimWeight,
        long rejectedClaimWeight,
        long supportWeight,
        long opposeWeight,
        long threshold)
    {
        AliveWeight = aliveWeight;
        MatureWeight = matureWeight;
        EligibleWeight = eligibleWeight;
        RecognizedClaimWeight = recognizedClaimWeight;
        ContestedClaimWeight = contestedClaimWeight;
        UnrecognizedClaimWeight = unrecognizedClaimWeight;
        RejectedClaimWeight = rejectedClaimWeight;
        SupportWeight = supportWeight;
        OpposeWeight = opposeWeight;
        Threshold = threshold;
    }

    public long GetRecognitionWeight(PoliticalClaimRecognitionState recognitionState)
    {
        switch (recognitionState)
        {
            case PoliticalClaimRecognitionState.Recognized:
                return RecognizedClaimWeight;
            case PoliticalClaimRecognitionState.Contested:
                return ContestedClaimWeight;
            case PoliticalClaimRecognitionState.Rejected:
                return RejectedClaimWeight;
            case PoliticalClaimRecognitionState.Unrecognized:
                return UnrecognizedClaimWeight;
            default:
                throw new ArgumentOutOfRangeException(nameof(recognitionState));
        }
    }
}

/// <summary>
/// The result of evaluating explicit legitimacy inputs under one explicit
/// policy. It is a derived read model, never primary political truth.
/// </summary>
public sealed class PoliticalLegitimacyAssessment
{
    public PersonId CandidatePersonId { get; }
    public long Score { get; }
    public long Threshold { get; }
    public bool MeetsThreshold { get; }
    public PoliticalLegitimacyInputs Inputs { get; }
    public PoliticalLegitimacyPolicy Policy { get; }

    internal PoliticalLegitimacyAssessment(
        PoliticalLegitimacyInputs inputs,
        PoliticalLegitimacyPolicy policy,
        long score)
    {
        Inputs = inputs;
        Policy = policy;
        CandidatePersonId = inputs.CandidatePersonId;
        Score = score;
        Threshold = policy.Threshold;
        MeetsThreshold = score >= policy.Threshold;
    }
}

/// <summary>
/// Pure deterministic legitimacy derivation. It accepts current world truth,
/// explicit recognition, and relation-derived counts as inputs only.
/// </summary>
public static class PoliticalLegitimacy
{
    public static PoliticalLegitimacyAssessment Assess(
        PoliticalLegitimacyInputs inputs,
        PoliticalLegitimacyPolicy policy)
    {
        if (inputs == null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        long score = 0L;
        checked
        {
            if (inputs.IsAlive)
            {
                score += policy.AliveWeight;
            }

            if (inputs.IsMature)
            {
                score += policy.MatureWeight;
            }

            if (inputs.IsEligible)
            {
                score += policy.EligibleWeight;
            }

            score += policy.GetRecognitionWeight(inputs.ClaimRecognitionState);
            score += checked((long)inputs.SupportCount * policy.SupportWeight);
            score += checked((long)inputs.OpposeCount * policy.OpposeWeight);
        }

        return new PoliticalLegitimacyAssessment(inputs, policy, score);
    }
}
