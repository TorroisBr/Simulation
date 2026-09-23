using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum SocialReactionTargetKind
{
    TheftOutcome = 0,
    Person = 1,
    Institution = 2
}

public enum SocialPerceivedAttributionKind
{
    NotApplicable = 0,
    Unknown = 1,
    BelievedPerson = 2,
    BelievedInstitution = 3
}

public enum SocialCognitiveBasisKind
{
    DirectExperience = 0,
    DirectObservation = 1,
    KnownFact = 2,
    ReceivedInformation = 3,
    BelievedAttribution = 4,
    InstitutionalRecord = 5
}

public enum SocialReactionValence
{
    Positive = 0,
    Negative = 1
}

public enum SocialReactionSalience
{
    Low = 0,
    Medium = 1,
    High = 2,
    Exceptional = 3
}

public sealed class SocialSourceReference : IEquatable<SocialSourceReference>
{
    public string Domain { get; }
    public string StableId { get; }

    public SocialSourceReference(string domain, string stableId)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("A social source domain is required.", nameof(domain));
        }

        if (string.IsNullOrWhiteSpace(stableId))
        {
            throw new ArgumentException("A social source stable id is required.", nameof(stableId));
        }

        Domain = domain;
        StableId = stableId;
    }

    public bool Equals(SocialSourceReference other)
    {
        return other != null
            && string.Equals(Domain, other.Domain, StringComparison.Ordinal)
            && string.Equals(StableId, other.StableId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as SocialSourceReference);

    public override int GetHashCode()
    {
        unchecked
        {
            return (StringComparer.Ordinal.GetHashCode(Domain) * 397)
                ^ StringComparer.Ordinal.GetHashCode(StableId);
        }
    }

    public override string ToString() => Domain + ":" + StableId;
}

public sealed class SocialReactionTarget : IEquatable<SocialReactionTarget>
{
    public SocialReactionTargetKind Kind { get; }
    public string StableId { get; }

    private SocialReactionTarget(SocialReactionTargetKind kind, string stableId)
    {
        if (Enum.IsDefined(typeof(SocialReactionTargetKind), kind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(stableId))
        {
            throw new ArgumentException("A social reaction target stable id is required.", nameof(stableId));
        }

        Kind = kind;
        StableId = stableId;
    }

    public static SocialReactionTarget ForTheftOutcome(string theftOutcomeId)
    {
        return new SocialReactionTarget(SocialReactionTargetKind.TheftOutcome, theftOutcomeId);
    }

    public static SocialReactionTarget ForPerson(PersonId personId)
    {
        return new SocialReactionTarget(
            SocialReactionTargetKind.Person,
            RequirePersonId(personId));
    }

    public static SocialReactionTarget ForInstitution(InstitutionId institutionId)
    {
        return new SocialReactionTarget(
            SocialReactionTargetKind.Institution,
            RequireInstitutionId(institutionId));
    }

    public static SocialReactionTarget ForStableId(SocialReactionTargetKind kind, string stableId)
    {
        return new SocialReactionTarget(kind, stableId);
    }

    public bool Equals(SocialReactionTarget other)
    {
        return other != null
            && Kind == other.Kind
            && string.Equals(StableId, other.StableId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as SocialReactionTarget);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(StableId);
        }
    }

    public override string ToString() => Kind + ":" + StableId;

    private static string RequirePersonId(PersonId personId)
    {
        return personId == null
            ? throw new ArgumentNullException(nameof(personId))
            : personId.Value;
    }

    private static string RequireInstitutionId(InstitutionId institutionId)
    {
        return institutionId == null
            ? throw new ArgumentNullException(nameof(institutionId))
            : institutionId.Value;
    }
}

public sealed class SocialPerceivedAttribution : IEquatable<SocialPerceivedAttribution>
{
    public SocialPerceivedAttributionKind Kind { get; }
    public PersonId PersonId { get; }
    public InstitutionId InstitutionId { get; }

    private SocialPerceivedAttribution(
        SocialPerceivedAttributionKind kind,
        PersonId personId,
        InstitutionId institutionId)
    {
        if (Enum.IsDefined(typeof(SocialPerceivedAttributionKind), kind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == SocialPerceivedAttributionKind.BelievedPerson && personId == null)
        {
            throw new ArgumentNullException(nameof(personId));
        }

        if (kind == SocialPerceivedAttributionKind.BelievedInstitution && institutionId == null)
        {
            throw new ArgumentNullException(nameof(institutionId));
        }

        if (kind != SocialPerceivedAttributionKind.BelievedPerson && personId != null
            || kind != SocialPerceivedAttributionKind.BelievedInstitution && institutionId != null)
        {
            throw new ArgumentException("Attribution endpoint does not match the attribution kind.");
        }

        Kind = kind;
        PersonId = personId;
        InstitutionId = institutionId;
    }

    public static SocialPerceivedAttribution NotApplicable()
    {
        return new SocialPerceivedAttribution(
            SocialPerceivedAttributionKind.NotApplicable,
            null,
            null);
    }

    public static SocialPerceivedAttribution Unknown()
    {
        return new SocialPerceivedAttribution(
            SocialPerceivedAttributionKind.Unknown,
            null,
            null);
    }

    public static SocialPerceivedAttribution BelievedPerson(PersonId personId)
    {
        return new SocialPerceivedAttribution(
            SocialPerceivedAttributionKind.BelievedPerson,
            personId,
            null);
    }

    public static SocialPerceivedAttribution BelievedInstitution(InstitutionId institutionId)
    {
        return new SocialPerceivedAttribution(
            SocialPerceivedAttributionKind.BelievedInstitution,
            null,
            institutionId);
    }

    public bool Equals(SocialPerceivedAttribution other)
    {
        return other != null
            && Kind == other.Kind
            && PersonId == other.PersonId
            && InstitutionId == other.InstitutionId;
    }

    public override bool Equals(object obj) => Equals(obj as SocialPerceivedAttribution);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind * 397;
            hash = (hash * 397) ^ (PersonId == null ? 0 : PersonId.GetHashCode());
            return (hash * 397) ^ (InstitutionId == null ? 0 : InstitutionId.GetHashCode());
        }
    }

    public override string ToString()
    {
        return Kind == SocialPerceivedAttributionKind.BelievedPerson
            ? Kind + ":" + PersonId.Value
            : Kind == SocialPerceivedAttributionKind.BelievedInstitution
                ? Kind + ":" + InstitutionId.Value
                : Kind.ToString();
    }
}

public sealed class SocialCognitiveBasis : IEquatable<SocialCognitiveBasis>
{
    public SocialCognitiveBasisKind Kind { get; }
    public string Reference { get; }
    public PersonId SourcePersonId { get; }
    public InstitutionId SourceInstitutionId { get; }

    public SocialCognitiveBasis(
        SocialCognitiveBasisKind kind,
        string reference = null,
        PersonId sourcePersonId = null,
        InstitutionId sourceInstitutionId = null)
    {
        if (Enum.IsDefined(typeof(SocialCognitiveBasisKind), kind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (sourcePersonId != null && sourceInstitutionId != null)
        {
            throw new ArgumentException("A cognitive basis cannot have both Person and Institution provenance.");
        }

        Kind = kind;
        Reference = reference;
        SourcePersonId = sourcePersonId;
        SourceInstitutionId = sourceInstitutionId;
    }

    public bool Equals(SocialCognitiveBasis other)
    {
        return other != null
            && Kind == other.Kind
            && string.Equals(Reference, other.Reference, StringComparison.Ordinal)
            && SourcePersonId == other.SourcePersonId
            && SourceInstitutionId == other.SourceInstitutionId;
    }

    public override bool Equals(object obj) => Equals(obj as SocialCognitiveBasis);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ((int)Kind * 397)
                ^ StringComparer.Ordinal.GetHashCode(Reference ?? string.Empty);
            hash = (hash * 397) ^ (SourcePersonId == null ? 0 : SourcePersonId.GetHashCode());
            return (hash * 397) ^ (SourceInstitutionId == null ? 0 : SourceInstitutionId.GetHashCode());
        }
    }
}

public sealed class SocialAppraisalResult
{
    private SocialAppraisalResult(
        bool producesReaction,
        SocialReactionValence? valence,
        SocialReactionSalience? salience)
    {
        ProducesReaction = producesReaction;
        Valence = valence;
        Salience = salience;
    }

    public bool ProducesReaction { get; }
    public SocialReactionValence? Valence { get; }
    public SocialReactionSalience? Salience { get; }

    public static SocialAppraisalResult NoReaction()
    {
        return new SocialAppraisalResult(false, null, null);
    }

    public static SocialAppraisalResult Reaction(
        SocialReactionValence valence,
        SocialReactionSalience salience)
    {
        return new SocialAppraisalResult(true, valence, salience);
    }
}

public sealed class SocialReactionId : IEquatable<SocialReactionId>
{
    public string Value { get; }

    public SocialReactionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SocialReactionId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public static SocialReactionId Create(
        PersonId evaluatorPersonId,
        SocialSourceReference source,
        SocialReactionTarget target,
        SocialPerceivedAttribution attribution,
        SocialCognitiveBasis basis,
        SocialReactionValence valence,
        SocialReactionSalience salience,
        long createdAbsoluteDay,
        SocialReactionId supersedesReactionId = null)
    {
        if (evaluatorPersonId == null) throw new ArgumentNullException(nameof(evaluatorPersonId));
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (attribution == null) throw new ArgumentNullException(nameof(attribution));
        if (basis == null) throw new ArgumentNullException(nameof(basis));
        if (createdAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));

        string value = "reaction|"
            + LengthPrefix(evaluatorPersonId.Value)
            + LengthPrefix(source.Domain)
            + LengthPrefix(source.StableId)
            + ((int)target.Kind).ToString() + LengthPrefix(target.StableId)
            + AttributionKey(attribution)
            + ((int)basis.Kind).ToString() + LengthPrefix(basis.Reference)
            + LengthPrefix(basis.SourcePersonId?.Value)
            + LengthPrefix(basis.SourceInstitutionId?.Value)
            + ((int)valence).ToString()
            + ((int)salience).ToString()
            + createdAbsoluteDay.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + LengthPrefix(supersedesReactionId?.Value);
        return new SocialReactionId(value);
    }

    public bool Equals(SocialReactionId other)
    {
        return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as SocialReactionId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;

    private static string AttributionKey(SocialPerceivedAttribution attribution)
    {
        return ((int)attribution.Kind).ToString()
            + LengthPrefix(attribution.PersonId?.Value)
            + LengthPrefix(attribution.InstitutionId?.Value);
    }

    private static string LengthPrefix(string value)
    {
        value = value ?? string.Empty;
        return value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
    }
}

public sealed class SocialReaction
{
    public SocialReactionId ReactionId { get; }
    public PersonId EvaluatorPersonId { get; }
    public SocialSourceReference Source { get; }
    public SocialReactionTarget Target { get; }
    public SocialPerceivedAttribution PerceivedAttribution { get; }
    public SocialReactionValence Valence { get; }
    public SocialReactionSalience Salience { get; }
    public SocialCognitiveBasis CognitiveBasis { get; }
    public long CreatedAbsoluteDay { get; }
    public SocialReactionId SupersedesReactionId { get; }

    public SocialReaction(
        SocialReactionId reactionId,
        PersonId evaluatorPersonId,
        SocialSourceReference source,
        SocialReactionTarget target,
        SocialPerceivedAttribution perceivedAttribution,
        SocialReactionValence valence,
        SocialReactionSalience salience,
        SocialCognitiveBasis cognitiveBasis,
        long createdAbsoluteDay,
        SocialReactionId supersedesReactionId = null)
    {
        ReactionId = reactionId ?? throw new ArgumentNullException(nameof(reactionId));
        EvaluatorPersonId = evaluatorPersonId ?? throw new ArgumentNullException(nameof(evaluatorPersonId));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        PerceivedAttribution = perceivedAttribution ?? throw new ArgumentNullException(nameof(perceivedAttribution));
        CognitiveBasis = cognitiveBasis ?? throw new ArgumentNullException(nameof(cognitiveBasis));
        if (Enum.IsDefined(typeof(SocialReactionValence), valence) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(valence));
        }

        if (Enum.IsDefined(typeof(SocialReactionSalience), salience) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(salience));
        }

        if (createdAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        }

        if (supersedesReactionId != null && supersedesReactionId == reactionId)
        {
            throw new ArgumentException("A reaction cannot supersede itself.", nameof(supersedesReactionId));
        }

        SupersedesReactionId = supersedesReactionId;
        Valence = valence;
        Salience = salience;
        CreatedAbsoluteDay = createdAbsoluteDay;
    }
}

public enum SocialReactionStoreFailureCode
{
    None = 0,
    InvalidReaction = 1,
    DuplicateReactionId = 2,
    EvaluatorNotRegistered = 3,
    SupersededReactionMissing = 4,
    SupersessionThreadMismatch = 5,
    SupersessionDayInvalid = 6,
    SupersessionCycle = 7,
    FutureReaction = 8,
    SupersessionAlreadyUsed = 9,
    RuntimeFaulted = 10
}

public sealed class SocialReactionStoreFailure
{
    private SocialReactionStoreFailure(SocialReactionStoreFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public SocialReactionStoreFailureCode Code { get; }
    public string Message { get; }

    public static SocialReactionStoreFailure None =>
        new SocialReactionStoreFailure(SocialReactionStoreFailureCode.None, string.Empty);

    public static SocialReactionStoreFailure Create(
        SocialReactionStoreFailureCode code,
        string message)
    {
        return new SocialReactionStoreFailure(code, message);
    }

    public override string ToString() => Code + ": " + Message;
}

public sealed class SocialReactionStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly SimulationTime simulationTime;
    private readonly Dictionary<string, SocialReaction> reactionsById =
        new Dictionary<string, SocialReaction>(StringComparer.Ordinal);

    public SocialReactionStore(PersonStore personStore = null, SimulationTime simulationTime = null)
    {
        this.personStore = personStore;
        this.simulationTime = simulationTime;
    }

    public int Count => reactionsById.Count;
    public PersonStore PersonStore => personStore;
    public SimulationTime SimulationTime => simulationTime;

    public IReadOnlyList<SocialReaction> HistoricalReactions =>
        SortedSnapshot(reactionsById.Values);

    public bool CanRecord(
        SocialReaction reaction,
        out SocialReactionStoreFailure failure)
    {
        failure = SocialReactionStoreFailure.None;
        if (reaction == null)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.InvalidReaction,
                "A social reaction is required.");
            return false;
        }

        if (personStore != null && personStore.TryGet(reaction.EvaluatorPersonId, out _) == false)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.EvaluatorNotRegistered,
                "The reaction evaluator PersonId is not registered.");
            return false;
        }

        if (simulationTime != null && reaction.CreatedAbsoluteDay > simulationTime.AbsoluteDay)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.FutureReaction,
                "A social reaction cannot be recorded in the future.");
            return false;
        }

        if (reactionsById.ContainsKey(reaction.ReactionId.Value))
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.DuplicateReactionId,
                "The reaction id is already registered.");
            return false;
        }

        if (reaction.SupersedesReactionId != null)
        {
            if (reactionsById.TryGetValue(
                reaction.SupersedesReactionId.Value,
                out SocialReaction superseded) == false)
            {
                failure = SocialReactionStoreFailure.Create(
                    SocialReactionStoreFailureCode.SupersededReactionMissing,
                    "The superseded reaction is not registered.");
                return false;
            }

            if (reaction.CreatedAbsoluteDay < superseded.CreatedAbsoluteDay)
            {
                failure = SocialReactionStoreFailure.Create(
                    SocialReactionStoreFailureCode.SupersessionDayInvalid,
                    "A superseding reaction cannot predate its predecessor.");
                return false;
            }

            if (reaction.EvaluatorPersonId != superseded.EvaluatorPersonId
                || reaction.Source.Equals(superseded.Source) == false
                || reaction.Target.Equals(superseded.Target) == false)
            {
                failure = SocialReactionStoreFailure.Create(
                    SocialReactionStoreFailureCode.SupersessionThreadMismatch,
                    "A superseding reaction must remain in the same evaluator/source/target thread.");
                return false;
            }

            if (WouldCreateCycle(reaction, superseded))
            {
                failure = SocialReactionStoreFailure.Create(
                    SocialReactionStoreFailureCode.SupersessionCycle,
                    "The reaction supersession would create a cycle.");
                return false;
            }

            foreach (SocialReaction existing in reactionsById.Values)
            {
                if (existing.SupersedesReactionId != null
                    && existing.SupersedesReactionId.Equals(reaction.SupersedesReactionId))
                {
                    failure = SocialReactionStoreFailure.Create(
                        SocialReactionStoreFailureCode.SupersessionAlreadyUsed,
                        "A reaction predecessor can have only one superseding reaction.");
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryRecord(
        SocialReaction reaction,
        out SocialReactionStoreFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = SocialReactionStoreFailure.Create(SocialReactionStoreFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
            return false;
        }

        if (CanRecord(reaction, out failure) == false)
        {
            return false;
        }

        reactionsById.Add(reaction.ReactionId.Value, reaction);
        failure = SocialReactionStoreFailure.None;
        return true;
    }

    internal bool TryRemove(SocialReactionId reactionId)
    {
        return reactionId != null && reactionsById.Remove(reactionId.Value);
    }

    public bool TryRecordAppraisal(
        PersonId evaluatorPersonId,
        SocialSourceReference source,
        SocialReactionTarget target,
        SocialPerceivedAttribution attribution,
        SocialCognitiveBasis basis,
        SocialAppraisalResult appraisal,
        long createdAbsoluteDay,
        SocialReactionId supersedesReactionId,
        out SocialReaction reaction,
        out SocialReactionStoreFailure failure)
    {
        reaction = null;
        if (!mutationGuardBinding.CanMutate)
        {
            failure = SocialReactionStoreFailure.Create(SocialReactionStoreFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
            return false;
        }

        if (TryBuildAppraisalReaction(
            evaluatorPersonId,
            source,
            target,
            attribution,
            basis,
            appraisal,
            createdAbsoluteDay,
            supersedesReactionId,
            out reaction,
            out failure) == false)
        {
            return false;
        }

        if (reaction == null)
        {
            return true;
        }

        if (TryRecord(reaction, out failure) == false)
        {
            reaction = null;
            return false;
        }

        return true;
    }

    public bool CanRecordAppraisal(
        PersonId evaluatorPersonId,
        SocialSourceReference source,
        SocialReactionTarget target,
        SocialPerceivedAttribution attribution,
        SocialCognitiveBasis basis,
        SocialAppraisalResult appraisal,
        long createdAbsoluteDay,
        SocialReactionId supersedesReactionId,
        out SocialReactionStoreFailure failure)
    {
        if (TryBuildAppraisalReaction(
            evaluatorPersonId,
            source,
            target,
            attribution,
            basis,
            appraisal,
            createdAbsoluteDay,
            supersedesReactionId,
            out SocialReaction reaction,
            out failure) == false)
        {
            return false;
        }

        return reaction == null || CanRecord(reaction, out failure);
    }

    private static bool TryBuildAppraisalReaction(
        PersonId evaluatorPersonId,
        SocialSourceReference source,
        SocialReactionTarget target,
        SocialPerceivedAttribution attribution,
        SocialCognitiveBasis basis,
        SocialAppraisalResult appraisal,
        long createdAbsoluteDay,
        SocialReactionId supersedesReactionId,
        out SocialReaction reaction,
        out SocialReactionStoreFailure failure)
    {
        reaction = null;
        failure = SocialReactionStoreFailure.None;
        if (appraisal == null)
        {
            failure = SocialReactionStoreFailure.Create(
                SocialReactionStoreFailureCode.InvalidReaction,
                "A social appraisal result is required.");
            return false;
        }

        if (appraisal.ProducesReaction == false)
        {
            return true;
        }

        reaction = new SocialReaction(
            SocialReactionId.Create(
                evaluatorPersonId,
                source,
                target,
                attribution,
                basis,
                appraisal.Valence.Value,
                appraisal.Salience.Value,
                createdAbsoluteDay,
                supersedesReactionId),
            evaluatorPersonId,
            source,
            target,
            attribution,
            appraisal.Valence.Value,
            appraisal.Salience.Value,
            basis,
            createdAbsoluteDay,
            supersedesReactionId);
        return true;
    }

    public IReadOnlyList<SocialReaction> GetCurrentReactions()
    {
        HashSet<string> supersededIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (SocialReaction reaction in reactionsById.Values)
        {
            if (reaction.SupersedesReactionId != null)
            {
                supersededIds.Add(reaction.SupersedesReactionId.Value);
            }
        }

        List<SocialReaction> current = new List<SocialReaction>();
        foreach (SocialReaction reaction in reactionsById.Values)
        {
            if (supersededIds.Contains(reaction.ReactionId.Value) == false)
            {
                current.Add(reaction);
            }
        }

        current.Sort(CompareReactions);
        return new ReadOnlyCollection<SocialReaction>(current);
    }

    public bool TryGet(SocialReactionId reactionId, out SocialReaction reaction)
    {
        reaction = null;
        return reactionId != null && reactionsById.TryGetValue(reactionId.Value, out reaction);
    }

    private bool WouldCreateCycle(SocialReaction reaction, SocialReaction superseded)
    {
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        SocialReaction current = superseded;
        while (current != null)
        {
            if (visited.Add(current.ReactionId.Value) == false)
            {
                return true;
            }

            if (current.SupersedesReactionId == null)
            {
                return false;
            }

            if (current.SupersedesReactionId == reaction.ReactionId)
            {
                return true;
            }

            reactionsById.TryGetValue(current.SupersedesReactionId.Value, out current);
        }

        return false;
    }

    private static IReadOnlyList<SocialReaction> SortedSnapshot(
        IEnumerable<SocialReaction> source)
    {
        List<SocialReaction> snapshot = new List<SocialReaction>(source);
        snapshot.Sort(CompareReactions);
        return new ReadOnlyCollection<SocialReaction>(snapshot);
    }

    private static int CompareReactions(SocialReaction left, SocialReaction right)
    {
        int comparison = StringComparer.Ordinal.Compare(
            left?.ReactionId?.Value,
            right?.ReactionId?.Value);
        if (comparison != 0) return comparison;
        return (left?.CreatedAbsoluteDay ?? 0L).CompareTo(right?.CreatedAbsoluteDay ?? 0L);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard)
            && (personStore == null || personStore.CanBindMutationGuard(guard))
            && (simulationTime == null || simulationTime.CanBindMutationGuard(guard));
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard)
            && (personStore == null || personStore.TryBindMutationGuard(guard))
            && (simulationTime == null || simulationTime.TryBindMutationGuard(guard))
            && mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
