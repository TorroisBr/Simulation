using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class TheftOutcomeId : IEquatable<TheftOutcomeId>
{
    public string Value { get; }

    public TheftOutcomeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("TheftOutcomeId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public static TheftOutcomeId Create(
        PersonId perpetratorPersonId,
        PersonId victimPersonId,
        long occurredAbsoluteDay,
        string occurrenceKey)
    {
        if (perpetratorPersonId == null) throw new ArgumentNullException(nameof(perpetratorPersonId));
        if (victimPersonId == null) throw new ArgumentNullException(nameof(victimPersonId));
        if (occurredAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(occurredAbsoluteDay));
        if (string.IsNullOrWhiteSpace(occurrenceKey)) throw new ArgumentException("A stable occurrence key is required.", nameof(occurrenceKey));

        return new TheftOutcomeId(
            "theft|"
            + LengthPrefix(perpetratorPersonId.Value)
            + LengthPrefix(victimPersonId.Value)
            + occurredAbsoluteDay.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + LengthPrefix(occurrenceKey));
    }

    public bool Equals(TheftOutcomeId other)
    {
        return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as TheftOutcomeId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;

    private static string LengthPrefix(string value)
    {
        return value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
    }
}

public sealed class TheftOutcome
{
    public TheftOutcomeId OutcomeId { get; }
    public PersonId PerpetratorPersonId { get; }
    public PersonId VictimPersonId { get; }
    public int LossAmount { get; }
    public long OccurredAbsoluteDay { get; }
    public string OriginDecisionId { get; }

    public TheftOutcome(
        TheftOutcomeId outcomeId,
        PersonId perpetratorPersonId,
        PersonId victimPersonId,
        int lossAmount,
        long occurredAbsoluteDay,
        string originDecisionId = null)
    {
        OutcomeId = outcomeId ?? throw new ArgumentNullException(nameof(outcomeId));
        PerpetratorPersonId = perpetratorPersonId ?? throw new ArgumentNullException(nameof(perpetratorPersonId));
        VictimPersonId = victimPersonId ?? throw new ArgumentNullException(nameof(victimPersonId));
        if (lossAmount <= 0) throw new ArgumentOutOfRangeException(nameof(lossAmount));
        if (occurredAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(occurredAbsoluteDay));
        LossAmount = lossAmount;
        OccurredAbsoluteDay = occurredAbsoluteDay;
        OriginDecisionId = string.IsNullOrWhiteSpace(originDecisionId) ? null : originDecisionId;
    }
}

public enum CrimeOutcomeStoreFailureCode
{
    None = 0,
    InvalidOutcome = 1,
    DuplicateOutcomeId = 2,
    PerpetratorNotRegistered = 3,
    VictimNotRegistered = 4
}

public sealed class CrimeOutcomeStoreFailure
{
    private CrimeOutcomeStoreFailure(CrimeOutcomeStoreFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public CrimeOutcomeStoreFailureCode Code { get; }
    public string Message { get; }
    public static CrimeOutcomeStoreFailure None =>
        new CrimeOutcomeStoreFailure(CrimeOutcomeStoreFailureCode.None, string.Empty);

    public static CrimeOutcomeStoreFailure Create(CrimeOutcomeStoreFailureCode code, string message)
    {
        return new CrimeOutcomeStoreFailure(code, message);
    }

    public override string ToString() => Code + ": " + Message;
}

public sealed class TheftOutcomeStore : ITheftOutcomeSink
{
    private readonly PersonStore personStore;
    private readonly Dictionary<string, TheftOutcome> outcomesById =
        new Dictionary<string, TheftOutcome>(StringComparer.Ordinal);

    public TheftOutcomeStore(PersonStore personStore = null)
    {
        this.personStore = personStore;
    }

    public int Count => outcomesById.Count;

    public IReadOnlyList<TheftOutcome> Outcomes => SortedSnapshot(outcomesById.Values);

    public bool TryGet(TheftOutcomeId outcomeId, out TheftOutcome outcome)
    {
        outcome = null;
        return outcomeId != null && outcomesById.TryGetValue(outcomeId.Value, out outcome);
    }

    public bool TryRecord(TheftOutcome outcome, out CrimeOutcomeStoreFailure failure)
    {
        failure = CrimeOutcomeStoreFailure.None;
        if (outcome == null)
        {
            failure = CrimeOutcomeStoreFailure.Create(
                CrimeOutcomeStoreFailureCode.InvalidOutcome,
                "A theft outcome is required.");
            return false;
        }

        if (personStore != null)
        {
            if (personStore.TryGet(outcome.PerpetratorPersonId, out _) == false)
            {
                failure = CrimeOutcomeStoreFailure.Create(
                    CrimeOutcomeStoreFailureCode.PerpetratorNotRegistered,
                    "The theft perpetrator PersonId is not registered.");
                return false;
            }

            if (personStore.TryGet(outcome.VictimPersonId, out _) == false)
            {
                failure = CrimeOutcomeStoreFailure.Create(
                    CrimeOutcomeStoreFailureCode.VictimNotRegistered,
                    "The theft victim PersonId is not registered.");
                return false;
            }
        }

        if (outcomesById.ContainsKey(outcome.OutcomeId.Value))
        {
            failure = CrimeOutcomeStoreFailure.Create(
                CrimeOutcomeStoreFailureCode.DuplicateOutcomeId,
                "The theft outcome id is already registered.");
            return false;
        }

        outcomesById.Add(outcome.OutcomeId.Value, outcome);
        return true;
    }

    public bool TryAcceptTheftOutcome(TheftOutcome outcome)
    {
        return TryRecord(outcome, out _);
    }

    private static IReadOnlyList<TheftOutcome> SortedSnapshot(IEnumerable<TheftOutcome> source)
    {
        List<TheftOutcome> snapshot = new List<TheftOutcome>(source);
        snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.OutcomeId?.Value,
            right?.OutcomeId?.Value));
        return new ReadOnlyCollection<TheftOutcome>(snapshot);
    }
}

public interface ITheftOutcomeSink
{
    bool TryAcceptTheftOutcome(TheftOutcome outcome);
}

public enum CrimeKnowledgeRole
{
    Victim = 0,
    Perpetrator = 1,
    Other = 2
}

public sealed class CrimeKnowledgeObservation
{
    public PersonId EvaluatorPersonId { get; }
    public TheftOutcomeId OutcomeId { get; }
    public CrimeKnowledgeRole Role { get; }
    public bool KnowsLoss { get; }
    public SocialPerceivedAttribution PerceivedPerpetrator { get; }
    public PersonId KnownInvestigatorPersonId { get; }
    public InstitutionId KnownInvestigatorInstitutionId { get; }
    public SocialCognitiveBasis CognitiveBasis { get; }
    public long ObservedAbsoluteDay { get; }
    public string StableKey => BuildStableKey();

    public CrimeKnowledgeObservation(
        PersonId evaluatorPersonId,
        TheftOutcomeId outcomeId,
        CrimeKnowledgeRole role,
        bool knowsLoss,
        SocialPerceivedAttribution perceivedPerpetrator,
        SocialCognitiveBasis cognitiveBasis,
        long observedAbsoluteDay,
        PersonId knownInvestigatorPersonId = null,
        InstitutionId knownInvestigatorInstitutionId = null)
    {
        EvaluatorPersonId = evaluatorPersonId ?? throw new ArgumentNullException(nameof(evaluatorPersonId));
        OutcomeId = outcomeId ?? throw new ArgumentNullException(nameof(outcomeId));
        CognitiveBasis = cognitiveBasis ?? throw new ArgumentNullException(nameof(cognitiveBasis));
        PerceivedPerpetrator = perceivedPerpetrator ?? throw new ArgumentNullException(nameof(perceivedPerpetrator));
        if (Enum.IsDefined(typeof(CrimeKnowledgeRole), role) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        if (observedAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedAbsoluteDay));
        }

        if (knownInvestigatorPersonId != null && knownInvestigatorInstitutionId != null)
        {
            throw new ArgumentException("Crime knowledge cannot identify both a Person and an Institution investigator.");
        }

        if (knowsLoss == false
            && perceivedPerpetrator.Kind != SocialPerceivedAttributionKind.NotApplicable)
        {
            throw new ArgumentException(
                "Perceived perpetrator attribution requires knowledge of the loss.",
                nameof(perceivedPerpetrator));
        }

        EvaluatorPersonId = evaluatorPersonId;
        OutcomeId = outcomeId;
        Role = role;
        KnowsLoss = knowsLoss;
        KnownInvestigatorPersonId = knownInvestigatorPersonId;
        KnownInvestigatorInstitutionId = knownInvestigatorInstitutionId;
        ObservedAbsoluteDay = observedAbsoluteDay;
    }

    public static CrimeKnowledgeObservation VictimKnowsLoss(
        TheftOutcome outcome,
        SocialCognitiveBasis basis,
        long observedAbsoluteDay)
    {
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        return new CrimeKnowledgeObservation(
            outcome.VictimPersonId,
            outcome.OutcomeId,
            CrimeKnowledgeRole.Victim,
            true,
            SocialPerceivedAttribution.Unknown(),
            basis,
            observedAbsoluteDay);
    }

    private static string LengthPrefix(string value)
    {
        value = value ?? string.Empty;
        return value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
    }

    private string BuildStableKey()
    {
        return ((int)Role).ToString()
            + (KnowsLoss ? "1" : "0")
            + ((int)PerceivedPerpetrator.Kind).ToString()
            + LengthPrefix(PerceivedPerpetrator.PersonId?.Value)
            + LengthPrefix(PerceivedPerpetrator.InstitutionId?.Value)
            + LengthPrefix(KnownInvestigatorPersonId?.Value)
            + LengthPrefix(KnownInvestigatorInstitutionId?.Value)
            + ((int)CognitiveBasis.Kind).ToString()
            + LengthPrefix(CognitiveBasis.Reference)
            + LengthPrefix(CognitiveBasis.SourcePersonId?.Value)
            + LengthPrefix(CognitiveBasis.SourceInstitutionId?.Value);
    }
}

public enum CrimeKnowledgeStoreFailureCode
{
    None = 0,
    InvalidObservation = 1,
    EvaluatorNotRegistered = 2,
    OutcomeNotRegistered = 3,
    OlderObservation = 4
}

public sealed class CrimeKnowledgeStoreFailure
{
    private CrimeKnowledgeStoreFailure(CrimeKnowledgeStoreFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public CrimeKnowledgeStoreFailureCode Code { get; }
    public string Message { get; }
    public static CrimeKnowledgeStoreFailure None =>
        new CrimeKnowledgeStoreFailure(CrimeKnowledgeStoreFailureCode.None, string.Empty);

    public static CrimeKnowledgeStoreFailure Create(CrimeKnowledgeStoreFailureCode code, string message)
    {
        return new CrimeKnowledgeStoreFailure(code, message);
    }

    public override string ToString() => Code + ": " + Message;
}

public sealed class CrimeKnowledgeStore
{
    private readonly PersonStore personStore;
    private readonly TheftOutcomeStore outcomeStore;
    private readonly Dictionary<string, CrimeKnowledgeObservation> currentByKey =
        new Dictionary<string, CrimeKnowledgeObservation>(StringComparer.Ordinal);

    public CrimeKnowledgeStore(PersonStore personStore, TheftOutcomeStore outcomeStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.outcomeStore = outcomeStore ?? throw new ArgumentNullException(nameof(outcomeStore));
    }

    public IReadOnlyList<CrimeKnowledgeObservation> CurrentObservations
    {
        get
        {
            List<CrimeKnowledgeObservation> result = new List<CrimeKnowledgeObservation>(currentByKey.Values);
            result.Sort((left, right) => StringComparer.Ordinal.Compare(
                Key(left.EvaluatorPersonId, left.OutcomeId),
                Key(right.EvaluatorPersonId, right.OutcomeId)));
            return new ReadOnlyCollection<CrimeKnowledgeObservation>(result);
        }
    }

    public bool TryGet(
        PersonId evaluatorPersonId,
        TheftOutcomeId outcomeId,
        out CrimeKnowledgeObservation observation)
    {
        observation = null;
        return evaluatorPersonId != null
            && outcomeId != null
            && currentByKey.TryGetValue(Key(evaluatorPersonId, outcomeId), out observation);
    }

    public bool TryRecord(
        CrimeKnowledgeObservation observation,
        out CrimeKnowledgeStoreFailure failure)
    {
        failure = CrimeKnowledgeStoreFailure.None;
        if (observation == null)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.InvalidObservation,
                "A crime knowledge observation is required.");
            return false;
        }

        if (personStore.TryGet(observation.EvaluatorPersonId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.EvaluatorNotRegistered,
                "The crime knowledge evaluator PersonId is not registered.");
            return false;
        }

        if (outcomeStore.TryGet(observation.OutcomeId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.OutcomeNotRegistered,
                "The crime knowledge outcome is not registered.");
            return false;
        }

        string key = Key(observation.EvaluatorPersonId, observation.OutcomeId);
        if (currentByKey.TryGetValue(key, out CrimeKnowledgeObservation current)
            && (observation.ObservedAbsoluteDay < current.ObservedAbsoluteDay
                || (observation.ObservedAbsoluteDay == current.ObservedAbsoluteDay
                    && string.CompareOrdinal(observation.StableKey, current.StableKey) < 0)))
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.OlderObservation,
                "An older crime knowledge observation cannot replace a newer one.");
            return false;
        }

        currentByKey[key] = observation;
        return true;
    }

    private static string Key(PersonId evaluatorPersonId, TheftOutcomeId outcomeId)
    {
        return evaluatorPersonId.Value.Length + ":" + evaluatorPersonId.Value
            + outcomeId.Value.Length + ":" + outcomeId.Value;
    }
}

public sealed class CrimeSocialAppraisalIntegration : ITheftOutcomeSink
{
    private readonly TheftOutcomeStore outcomeStore;
    private readonly CrimeKnowledgeStore knowledgeStore;
    private readonly SocialReactionStore reactionStore;

    public CrimeSocialAppraisalIntegration(
        TheftOutcomeStore outcomeStore,
        CrimeKnowledgeStore knowledgeStore,
        SocialReactionStore reactionStore)
    {
        this.outcomeStore = outcomeStore ?? throw new ArgumentNullException(nameof(outcomeStore));
        this.knowledgeStore = knowledgeStore ?? throw new ArgumentNullException(nameof(knowledgeStore));
        this.reactionStore = reactionStore ?? throw new ArgumentNullException(nameof(reactionStore));
    }

    public bool TryAcceptTheftOutcome(TheftOutcome outcome)
    {
        if (outcomeStore.TryRecord(outcome, out _) == false)
        {
            return false;
        }

        CrimeKnowledgeObservation victimKnowledge = CrimeKnowledgeObservation.VictimKnowsLoss(
            outcome,
            new SocialCognitiveBasis(
                SocialCognitiveBasisKind.DirectExperience,
                "theft-loss"),
            outcome.OccurredAbsoluteDay);
        return TryRecordKnowledgeAndAppraise(victimKnowledge, out _);
    }

    public bool TryRecordKnowledgeAndAppraise(
        CrimeKnowledgeObservation observation,
        out SocialReactionStoreFailure failure)
    {
        failure = SocialReactionStoreFailure.None;
        if (knowledgeStore.TryRecord(observation, out CrimeKnowledgeStoreFailure knowledgeFailure) == false)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.InvalidReaction,
                knowledgeFailure.ToString());
            return false;
        }

        if (outcomeStore.TryGet(observation.OutcomeId, out TheftOutcome outcome) == false)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.InvalidReaction,
                "Theft outcome is not registered.");
            return false;
        }

        SocialReactionTarget theftTarget = SocialReactionTarget.ForTheftOutcome(
            outcome.OutcomeId.Value);
        if (observation.KnowsLoss)
        {
            SocialReactionId previous = FindCurrent(
                observation.EvaluatorPersonId,
                new SocialSourceReference("crime.theft", outcome.OutcomeId.Value),
                theftTarget);
            if (reactionStore.TryRecordAppraisal(
                observation.EvaluatorPersonId,
                new SocialSourceReference("crime.theft", outcome.OutcomeId.Value),
                theftTarget,
                observation.PerceivedPerpetrator,
                observation.CognitiveBasis,
                SocialAppraisalResult.Reaction(
                    SocialReactionValence.Negative,
                    SocialReactionSalience.High),
                observation.ObservedAbsoluteDay,
                previous,
                out _,
                out failure) == false)
            {
                return false;
            }
        }

        SocialReactionTarget investigatorTarget = observation.KnownInvestigatorPersonId != null
            ? SocialReactionTarget.ForPerson(observation.KnownInvestigatorPersonId)
            : observation.KnownInvestigatorInstitutionId != null
                ? SocialReactionTarget.ForInstitution(observation.KnownInvestigatorInstitutionId)
                : null;
        if (investigatorTarget == null)
        {
            return true;
        }

        SocialSourceReference investigationSource = new SocialSourceReference(
            "crime.investigation",
            outcome.OutcomeId.Value);
        SocialReactionId investigatorPrevious = FindCurrent(
            observation.EvaluatorPersonId,
            investigationSource,
            investigatorTarget);
        return reactionStore.TryRecordAppraisal(
            observation.EvaluatorPersonId,
            investigationSource,
            investigatorTarget,
            SocialPerceivedAttribution.NotApplicable(),
            observation.CognitiveBasis,
            SocialAppraisalResult.Reaction(
                observation.Role == CrimeKnowledgeRole.Perpetrator
                    ? SocialReactionValence.Negative
                    : SocialReactionValence.Positive,
                SocialReactionSalience.Medium),
            observation.ObservedAbsoluteDay,
            investigatorPrevious,
            out _,
            out failure);
    }

    private SocialReactionId FindCurrent(
        PersonId evaluatorPersonId,
        SocialSourceReference source,
        SocialReactionTarget target)
    {
        foreach (SocialReaction reaction in reactionStore.GetCurrentReactions())
        {
            if (reaction.EvaluatorPersonId == evaluatorPersonId
                && reaction.Source.Equals(source)
                && reaction.Target.Equals(target))
            {
                return reaction.ReactionId;
            }
        }

        return null;
    }
}
