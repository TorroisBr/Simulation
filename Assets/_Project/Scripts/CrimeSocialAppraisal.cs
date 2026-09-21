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
    VictimNotRegistered = 4,
    FutureOutcome = 5
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
    private readonly SimulationTime simulationTime;
    private readonly Dictionary<string, TheftOutcome> outcomesById =
        new Dictionary<string, TheftOutcome>(StringComparer.Ordinal);

    public TheftOutcomeStore(PersonStore personStore, SimulationTime simulationTime)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
    }

    public int Count => outcomesById.Count;
    public PersonStore PersonStore => personStore;
    public SimulationTime SimulationTime => simulationTime;

    public IReadOnlyList<TheftOutcome> Outcomes => SortedSnapshot(outcomesById.Values);

    public bool TryGet(TheftOutcomeId outcomeId, out TheftOutcome outcome)
    {
        outcome = null;
        return outcomeId != null && outcomesById.TryGetValue(outcomeId.Value, out outcome);
    }

    public bool TryRecord(TheftOutcome outcome, out CrimeOutcomeStoreFailure failure)
    {
        if (CanRecord(outcome, out failure) == false)
        {
            return false;
        }

        outcomesById.Add(outcome.OutcomeId.Value, outcome);
        failure = CrimeOutcomeStoreFailure.None;
        return true;
    }

    public bool CanRecord(TheftOutcome outcome, out CrimeOutcomeStoreFailure failure)
    {
        failure = CrimeOutcomeStoreFailure.None;
        if (outcome == null)
        {
            failure = CrimeOutcomeStoreFailure.Create(
                CrimeOutcomeStoreFailureCode.InvalidOutcome,
                "A theft outcome is required.");
            return false;
        }

        if (outcome.OccurredAbsoluteDay > simulationTime.AbsoluteDay)
        {
            failure = CrimeOutcomeStoreFailure.Create(
                CrimeOutcomeStoreFailureCode.FutureOutcome,
                "A theft outcome cannot be recorded in the future.");
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

        return true;
    }

    public bool TryAcceptTheftOutcome(TheftOutcome outcome)
    {
        return TryRecord(outcome, out _);
    }

    public bool CanAcceptTheftOutcome(TheftOutcome outcome)
    {
        return CanRecord(outcome, out _);
    }

    internal bool TryRemove(TheftOutcomeId outcomeId)
    {
        return outcomeId != null && outcomesById.Remove(outcomeId.Value);
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
    bool CanAcceptTheftOutcome(TheftOutcome outcome);
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
    OlderObservation = 4,
    FutureObservation = 5,
    BeforeOutcome = 6,
    RoleEndpointMismatch = 7,
    EndpointNotRegistered = 8
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
    private readonly SimulationTime simulationTime;
    private readonly InstitutionStore institutionStore;
    private readonly Dictionary<string, CrimeKnowledgeObservation> currentByKey =
        new Dictionary<string, CrimeKnowledgeObservation>(StringComparer.Ordinal);

    public CrimeKnowledgeStore(
        PersonStore personStore,
        TheftOutcomeStore outcomeStore,
        SimulationTime simulationTime,
        InstitutionStore institutionStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.outcomeStore = outcomeStore ?? throw new ArgumentNullException(nameof(outcomeStore));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.institutionStore = institutionStore ?? throw new ArgumentNullException(nameof(institutionStore));
        if (ReferenceEquals(personStore, outcomeStore.PersonStore) == false
            || ReferenceEquals(simulationTime, outcomeStore.SimulationTime) == false)
        {
            throw new ArgumentException("Crime knowledge stores must belong to the same world boundary.");
        }
    }

    public PersonStore PersonStore => personStore;
    public TheftOutcomeStore OutcomeStore => outcomeStore;
    public SimulationTime SimulationTime => simulationTime;

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

    public bool CanRecord(
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

        if (outcomeStore.TryGet(observation.OutcomeId, out TheftOutcome outcome) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.OutcomeNotRegistered,
                "The crime knowledge outcome is not registered.");
            return false;
        }

        return CanRecordAgainstOutcome(observation, outcome, out failure);
    }

    internal bool CanRecordAgainstOutcome(
        CrimeKnowledgeObservation observation,
        TheftOutcome outcome,
        out CrimeKnowledgeStoreFailure failure)
    {
        failure = CrimeKnowledgeStoreFailure.None;
        if (observation == null || outcome == null
            || observation.OutcomeId.Equals(outcome.OutcomeId) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.OutcomeNotRegistered,
                "The crime knowledge outcome does not match the supplied outcome.");
            return false;
        }

        if (personStore.TryGet(observation.EvaluatorPersonId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.EvaluatorNotRegistered,
                "The crime knowledge evaluator PersonId is not registered.");
            return false;
        }

        if (observation.ObservedAbsoluteDay > simulationTime.AbsoluteDay)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.FutureObservation,
                "A crime knowledge observation cannot be recorded in the future.");
            return false;
        }

        if (observation.ObservedAbsoluteDay < outcome.OccurredAbsoluteDay)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.BeforeOutcome,
                "A crime knowledge observation cannot predate its outcome.");
            return false;
        }

        if (observation.Role == CrimeKnowledgeRole.Victim
            && observation.EvaluatorPersonId != outcome.VictimPersonId)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.RoleEndpointMismatch,
                "Victim knowledge must be held by the theft victim.");
            return false;
        }

        if (observation.Role == CrimeKnowledgeRole.Perpetrator
            && observation.EvaluatorPersonId != outcome.PerpetratorPersonId)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.RoleEndpointMismatch,
                "Perpetrator knowledge must be held by the factual perpetrator.");
            return false;
        }

        if (observation.PerceivedPerpetrator.Kind == SocialPerceivedAttributionKind.BelievedPerson
            && personStore.TryGet(observation.PerceivedPerpetrator.PersonId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.EndpointNotRegistered,
                "The believed perpetrator PersonId is not registered.");
            return false;
        }

        if (observation.PerceivedPerpetrator.Kind == SocialPerceivedAttributionKind.BelievedInstitution
            && institutionStore.TryGet(observation.PerceivedPerpetrator.InstitutionId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.EndpointNotRegistered,
                "The believed perpetrator InstitutionId is not registered.");
            return false;
        }

        if (observation.KnownInvestigatorPersonId != null
            && personStore.TryGet(observation.KnownInvestigatorPersonId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.EndpointNotRegistered,
                "The known investigator PersonId is not registered.");
            return false;
        }

        if (observation.KnownInvestigatorInstitutionId != null
            && institutionStore.TryGet(observation.KnownInvestigatorInstitutionId, out _) == false)
        {
            failure = CrimeKnowledgeStoreFailure.Create(
                CrimeKnowledgeStoreFailureCode.EndpointNotRegistered,
                "The known investigator InstitutionId is not registered.");
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

        return true;
    }

    public bool TryRecord(
        CrimeKnowledgeObservation observation,
        out CrimeKnowledgeStoreFailure failure)
    {
        if (CanRecord(observation, out failure) == false)
        {
            return false;
        }

        currentByKey[Key(observation.EvaluatorPersonId, observation.OutcomeId)] = observation;
        failure = CrimeKnowledgeStoreFailure.None;
        return true;
    }

    internal bool TryRemove(PersonId evaluatorPersonId, TheftOutcomeId outcomeId)
    {
        return evaluatorPersonId != null
            && outcomeId != null
            && currentByKey.Remove(Key(evaluatorPersonId, outcomeId));
    }

    internal bool TryRestore(CrimeKnowledgeObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        currentByKey[Key(observation.EvaluatorPersonId, observation.OutcomeId)] = observation;
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
        if (ReferenceEquals(outcomeStore.PersonStore, knowledgeStore.PersonStore) == false
            || ReferenceEquals(outcomeStore.SimulationTime, knowledgeStore.SimulationTime) == false
            || (reactionStore.PersonStore != null
                && ReferenceEquals(outcomeStore.PersonStore, reactionStore.PersonStore) == false)
            || (reactionStore.SimulationTime != null
                && ReferenceEquals(outcomeStore.SimulationTime, reactionStore.SimulationTime) == false))
        {
            throw new ArgumentException("Crime appraisal stores must belong to the same world boundary.");
        }
    }

    public bool CanAcceptTheftOutcome(TheftOutcome outcome)
    {
        if (outcomeStore.CanRecord(outcome, out _) == false)
        {
            return false;
        }

        CrimeKnowledgeObservation victimKnowledge = CrimeKnowledgeObservation.VictimKnowsLoss(
            outcome,
            new SocialCognitiveBasis(
                SocialCognitiveBasisKind.DirectExperience,
                "theft-loss"),
            outcome.OccurredAbsoluteDay);
        return knowledgeStore.CanRecordAgainstOutcome(
                victimKnowledge,
                outcome,
                out _)
            && CanAppraiseKnowledge(victimKnowledge, outcome, out _);
    }

    public bool TryAcceptTheftOutcome(TheftOutcome outcome)
    {
        if (CanAcceptTheftOutcome(outcome) == false)
        {
            return false;
        }

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
        if (TryRecordKnowledgeAndAppraise(victimKnowledge, out _) == false)
        {
            outcomeStore.TryRemove(outcome.OutcomeId);
            return false;
        }

        return true;
    }

    public bool CanRecordKnowledgeAndAppraise(
        CrimeKnowledgeObservation observation,
        out SocialReactionStoreFailure failure)
    {
        failure = SocialReactionStoreFailure.None;
        if (knowledgeStore.CanRecord(observation, out CrimeKnowledgeStoreFailure knowledgeFailure) == false)
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

        return CanAppraiseKnowledge(observation, outcome, out failure);
    }

    public bool TryRecordKnowledgeAndAppraise(
        CrimeKnowledgeObservation observation,
        out SocialReactionStoreFailure failure)
    {
        if (CanRecordKnowledgeAndAppraise(observation, out failure) == false)
        {
            return false;
        }

        if (outcomeStore.TryGet(observation.OutcomeId, out TheftOutcome outcome) == false)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.InvalidReaction,
                "Theft outcome is not registered.");
            return false;
        }

        knowledgeStore.TryGet(
            observation.EvaluatorPersonId,
            observation.OutcomeId,
            out CrimeKnowledgeObservation previousKnowledge);
        HashSet<string> existingReactionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (SocialReaction existingReaction in reactionStore.HistoricalReactions)
        {
            existingReactionIds.Add(existingReaction.ReactionId.Value);
        }

        if (knowledgeStore.TryRecord(observation, out CrimeKnowledgeStoreFailure knowledgeFailure) == false)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.InvalidReaction,
                knowledgeFailure.ToString());
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
                RollbackObservation(observation, previousKnowledge, existingReactionIds);
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
        if (reactionStore.TryRecordAppraisal(
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
            out failure) == false)
        {
            RollbackObservation(observation, previousKnowledge, existingReactionIds);
            return false;
        }

        return true;
    }

    private bool CanAppraiseKnowledge(
        CrimeKnowledgeObservation observation,
        TheftOutcome outcome,
        out SocialReactionStoreFailure failure)
    {
        failure = SocialReactionStoreFailure.None;
        SocialReactionTarget theftTarget = SocialReactionTarget.ForTheftOutcome(
            outcome.OutcomeId.Value);
        if (observation.KnowsLoss)
        {
            SocialSourceReference theftSource = new SocialSourceReference(
                "crime.theft",
                outcome.OutcomeId.Value);
            SocialReactionId previous = FindCurrent(
                observation.EvaluatorPersonId,
                theftSource,
                theftTarget);
            if (reactionStore.CanRecordAppraisal(
                observation.EvaluatorPersonId,
                theftSource,
                theftTarget,
                observation.PerceivedPerpetrator,
                observation.CognitiveBasis,
                SocialAppraisalResult.Reaction(
                    SocialReactionValence.Negative,
                    SocialReactionSalience.High),
                observation.ObservedAbsoluteDay,
                previous,
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
        return reactionStore.CanRecordAppraisal(
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
            out failure);
    }

    private void RollbackObservation(
        CrimeKnowledgeObservation observation,
        CrimeKnowledgeObservation previousKnowledge,
        HashSet<string> existingReactionIds)
    {
        foreach (SocialReaction reaction in reactionStore.HistoricalReactions)
        {
            if (existingReactionIds.Contains(reaction.ReactionId.Value) == false)
            {
                reactionStore.TryRemove(reaction.ReactionId);
            }
        }

        if (previousKnowledge == null)
        {
            knowledgeStore.TryRemove(observation.EvaluatorPersonId, observation.OutcomeId);
        }
        else
        {
            knowledgeStore.TryRestore(previousKnowledge);
        }
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

/// <summary>
/// World-owned C1/C2 state. All stores share the exact PersonStore and
/// SimulationTime instances, preventing accidental cross-world composition.
/// </summary>
public sealed class CrimeSocialAppraisalWorldState
{
    public PersonStore PersonStore { get; }
    public InstitutionStore InstitutionStore { get; }
    public SimulationTime SimulationTime { get; }
    public TheftOutcomeStore TheftOutcomes { get; }
    public CrimeKnowledgeStore CrimeKnowledge { get; }
    public SocialReactionStore SocialReactions { get; }
    public CrimeSocialAppraisalIntegration Integration { get; }

    public CrimeSocialAppraisalWorldState(
        PersonStore personStore,
        InstitutionStore institutionStore,
        SimulationTime simulationTime)
    {
        PersonStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        InstitutionStore = institutionStore ?? throw new ArgumentNullException(nameof(institutionStore));
        SimulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        TheftOutcomes = new TheftOutcomeStore(PersonStore, SimulationTime);
        CrimeKnowledge = new CrimeKnowledgeStore(
            PersonStore,
            TheftOutcomes,
            SimulationTime,
            InstitutionStore);
        SocialReactions = new SocialReactionStore(PersonStore, SimulationTime);
        Integration = new CrimeSocialAppraisalIntegration(
            TheftOutcomes,
            CrimeKnowledge,
            SocialReactions);
    }
}
