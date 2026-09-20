using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum PoliticalKnowledgeHolderKind
{
    Person = 0,
    Institution = 1
}

public sealed class PoliticalKnowledgeHolder : IEquatable<PoliticalKnowledgeHolder>
{
    private readonly PoliticalKnowledgeHolderKind kind;
    private readonly PersonId personId;
    private readonly InstitutionId institutionId;

    public PoliticalKnowledgeHolderKind Kind => kind;
    public PersonId PersonId => personId;
    public InstitutionId InstitutionId => institutionId;
    public string RawStableId => personId != null ? personId.Value : institutionId.Value;
    public string StableId => kind + ":" + RawStableId;

    private PoliticalKnowledgeHolder(PersonId personId)
    {
        this.personId = personId ?? throw new ArgumentNullException(nameof(personId));
        kind = PoliticalKnowledgeHolderKind.Person;
    }

    private PoliticalKnowledgeHolder(InstitutionId institutionId)
    {
        this.institutionId = institutionId ?? throw new ArgumentNullException(nameof(institutionId));
        kind = PoliticalKnowledgeHolderKind.Institution;
    }

    public static PoliticalKnowledgeHolder ForPerson(PersonId personId)
    {
        return new PoliticalKnowledgeHolder(personId);
    }

    public static PoliticalKnowledgeHolder ForInstitution(InstitutionId institutionId)
    {
        return new PoliticalKnowledgeHolder(institutionId);
    }

    public bool Equals(PoliticalKnowledgeHolder other)
    {
        return other != null
            && kind == other.kind
            && ((personId != null && personId == other.personId)
                || (institutionId != null && institutionId == other.institutionId));
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalKnowledgeHolder);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)kind * 397) ^ StringComparer.Ordinal.GetHashCode(StableId);
        }
    }

    public override string ToString()
    {
        return StableId;
    }
}

public enum PoliticalKnowledgeSource
{
    DirectObservation = 0,
    InitialScenarioKnowledge = 1,
    SharedByPerson = 2,
    SharedByInstitution = 3,
    InstitutionalRecord = 4
}

public sealed class PoliticalKnowledgeProvenance : IEquatable<PoliticalKnowledgeProvenance>
{
    public PoliticalKnowledgeSource Source { get; }
    public string SourceReference { get; }
    public string SourceId => SourceReference;
    public PersonId SourcePersonId { get; }
    public InstitutionId SourceInstitutionId { get; }

    public PoliticalKnowledgeProvenance(
        PoliticalKnowledgeSource source,
        string sourceReference = null,
        PersonId sourcePersonId = null,
        InstitutionId sourceInstitutionId = null)
    {
        if (Enum.IsDefined(typeof(PoliticalKnowledgeSource), source) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        if (sourcePersonId != null && sourceInstitutionId != null)
        {
            throw new ArgumentException(
                "Political knowledge provenance cannot identify both a Person and an Institution.");
        }

        if (source == PoliticalKnowledgeSource.SharedByPerson && sourcePersonId == null)
        {
            throw new ArgumentException(
                "SharedByPerson provenance requires a stable source PersonId.",
                nameof(sourcePersonId));
        }

        if (source == PoliticalKnowledgeSource.SharedByInstitution
            || source == PoliticalKnowledgeSource.InstitutionalRecord)
        {
            if (sourceInstitutionId == null)
            {
                throw new ArgumentException(
                    "Institutional political knowledge provenance requires a stable source InstitutionId.",
                    nameof(sourceInstitutionId));
            }
        }

        if ((source == PoliticalKnowledgeSource.DirectObservation
                || source == PoliticalKnowledgeSource.InitialScenarioKnowledge)
            && (sourcePersonId != null || sourceInstitutionId != null))
        {
            throw new ArgumentException(
                "Direct and initial political knowledge provenance does not accept a transmission holder.");
        }

        Source = source;
        SourceReference = sourceReference;
        SourcePersonId = sourcePersonId;
        SourceInstitutionId = sourceInstitutionId;
    }

    public bool Equals(PoliticalKnowledgeProvenance other)
    {
        return other != null
            && Source == other.Source
            && string.Equals(SourceReference, other.SourceReference, StringComparison.Ordinal)
            && SourcePersonId == other.SourcePersonId
            && SourceInstitutionId == other.SourceInstitutionId;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalKnowledgeProvenance);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ((int)Source * 397)
                ^ StringComparer.Ordinal.GetHashCode(SourceReference ?? string.Empty);
            hash = (hash * 397) ^ (SourcePersonId == null ? 0 : SourcePersonId.GetHashCode());
            return (hash * 397) ^ (SourceInstitutionId == null ? 0 : SourceInstitutionId.GetHashCode());
        }
    }
}

public enum PoliticalKnowledgeFactKind
{
    PoliticalClaim = 0,
    Faction = 1,
    FactionAffiliation = 2,
    OfficeVacancy = 3,
    PersonDeath = 4
}

public abstract class PoliticalKnowledgeObservation
{
    public PoliticalKnowledgeFactKind FactKind { get; }
    public long ObservedAbsoluteDay { get; }
    public long ReceivedAbsoluteDay { get; }
    public PoliticalKnowledgeProvenance Provenance { get; }
    public PoliticalKnowledgeSource Source => Provenance.Source;

    internal abstract string IdentityKey { get; }
    internal abstract string SnapshotSortKey { get; }

    protected PoliticalKnowledgeObservation(
        PoliticalKnowledgeFactKind factKind,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeProvenance provenance)
    {
        if (Enum.IsDefined(typeof(PoliticalKnowledgeFactKind), factKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(factKind));
        }

        ValidateDays(observedAbsoluteDay, receivedAbsoluteDay);

        FactKind = factKind;
        ObservedAbsoluteDay = observedAbsoluteDay;
        ReceivedAbsoluteDay = receivedAbsoluteDay;
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
    }

    public long GetAgeDays(long currentAbsoluteDay)
    {
        if (currentAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(currentAbsoluteDay));
        }

        if (currentAbsoluteDay < ObservedAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentAbsoluteDay),
                "The current world day cannot precede the observation day.");
        }

        return currentAbsoluteDay - ObservedAbsoluteDay;
    }

    protected static void ValidateDays(long observedAbsoluteDay, long receivedAbsoluteDay)
    {
        if (observedAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAbsoluteDay),
                "ObservedAbsoluteDay cannot be negative.");
        }

        if (receivedAbsoluteDay < observedAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(receivedAbsoluteDay),
                "ReceivedAbsoluteDay cannot be earlier than ObservedAbsoluteDay.");
        }
    }

    protected static void ValidateOptionalDay(long? absoluteDay, string parameterName)
    {
        if (absoluteDay.HasValue && absoluteDay.Value < 0L)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    protected static string BoolKey(bool value)
    {
        return value ? "1" : "0";
    }
}

public sealed class PoliticalClaimKnowledgeObservation : PoliticalKnowledgeObservation
{
    public PoliticalClaimId ClaimId { get; }
    public bool Exists { get; }
    public PersonId ClaimantPersonId { get; }
    public PoliticalClaimType ClaimType { get; }
    public PoliticalClaimTarget Target { get; }
    public PoliticalClaimBasis Basis { get; }
    public long CreatedAbsoluteDay { get; }
    public PoliticalClaimStatus Status { get; }
    public long? ResolutionAbsoluteDay { get; }
    public PoliticalClaimRecognitionState RecognitionState { get; }
    public InstitutionId RecognizingInstitutionId { get; }
    public long? RecognitionAbsoluteDay { get; }

    public PoliticalClaimKnowledgeObservation(
        PoliticalClaimId claimId,
        bool exists,
        PersonId claimantPersonId,
        PoliticalClaimType claimType,
        PoliticalClaimTarget target,
        PoliticalClaimBasis basis,
        long createdAbsoluteDay,
        PoliticalClaimStatus status,
        long? resolutionAbsoluteDay,
        PoliticalClaimRecognitionState recognitionState,
        InstitutionId recognizingInstitutionId,
        long? recognitionAbsoluteDay,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeProvenance provenance)
        : base(
            PoliticalKnowledgeFactKind.PoliticalClaim,
            observedAbsoluteDay,
            receivedAbsoluteDay,
            provenance)
    {
        if (claimId == null)
        {
            throw new ArgumentNullException(nameof(claimId));
        }

        if (claimantPersonId == null)
        {
            throw new ArgumentNullException(nameof(claimantPersonId));
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (Enum.IsDefined(typeof(PoliticalClaimType), claimType) == false
            || PoliticalClaimRecord.IsTargetCompatible(claimType, target.Kind) == false)
        {
            throw new ArgumentException("Claim type and target kind are incompatible.", nameof(claimType));
        }

        if (Enum.IsDefined(typeof(PoliticalClaimBasis), basis) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(basis));
        }

        if (createdAbsoluteDay < 0L || createdAbsoluteDay > observedAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(createdAbsoluteDay),
                "Claim creation cannot occur after the observation day.");
        }

        if (Enum.IsDefined(typeof(PoliticalClaimStatus), status) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ValidateClaimResolution(
            status,
            createdAbsoluteDay,
            resolutionAbsoluteDay,
            observedAbsoluteDay);

        if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), recognitionState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(recognitionState));
        }

        ValidateOptionalDay(recognitionAbsoluteDay, nameof(recognitionAbsoluteDay));
        if (recognitionState == PoliticalClaimRecognitionState.Unrecognized
            && (recognizingInstitutionId != null || recognitionAbsoluteDay.HasValue))
        {
            throw new ArgumentException("An unrecognized claim cannot carry recognition metadata.");
        }

        if (recognitionState != PoliticalClaimRecognitionState.Unrecognized
            && (recognizingInstitutionId == null || recognitionAbsoluteDay.HasValue == false))
        {
            throw new ArgumentException("A recognized, contested, or rejected claim requires recognition metadata.");
        }

        if (recognitionAbsoluteDay.HasValue && recognitionAbsoluteDay.Value > observedAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recognitionAbsoluteDay),
                "Recognition cannot occur after the observation day.");
        }

        if (recognitionAbsoluteDay.HasValue && recognitionAbsoluteDay.Value < createdAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recognitionAbsoluteDay),
                "Recognition cannot occur before claim creation.");
        }

        ClaimId = claimId;
        Exists = exists;
        ClaimantPersonId = claimantPersonId;
        ClaimType = claimType;
        Target = target;
        Basis = basis;
        CreatedAbsoluteDay = createdAbsoluteDay;
        Status = status;
        ResolutionAbsoluteDay = resolutionAbsoluteDay;
        RecognitionState = recognitionState;
        RecognizingInstitutionId = recognizingInstitutionId;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
    }

    internal override string IdentityKey => ClaimId.Value;

    internal override string SnapshotSortKey
    {
        get
        {
            return string.Concat(
                BoolKey(Exists), "\u001F",
                ClaimantPersonId.Value, "\u001F",
                ((int)ClaimType).ToString(), "\u001F",
                ((int)Target.Kind).ToString(), "\u001F",
                Target.TargetId, "\u001F",
                ((int)Basis).ToString(), "\u001F",
                CreatedAbsoluteDay.ToString(), "\u001F",
                ((int)Status).ToString(), "\u001F",
                ResolutionAbsoluteDay.HasValue ? ResolutionAbsoluteDay.Value.ToString() : string.Empty, "\u001F",
                ((int)RecognitionState).ToString(), "\u001F",
                RecognizingInstitutionId == null ? string.Empty : RecognizingInstitutionId.Value, "\u001F",
                RecognitionAbsoluteDay.HasValue ? RecognitionAbsoluteDay.Value.ToString() : string.Empty);
            }
        }

        private static void ValidateClaimResolution(
            PoliticalClaimStatus status,
            long createdAbsoluteDay,
            long? resolutionAbsoluteDay,
            long observedAbsoluteDay)
        {
            if (status == PoliticalClaimStatus.Active)
            {
                if (resolutionAbsoluteDay.HasValue)
                {
                    throw new ArgumentException(
                        "An active claim cannot carry a resolution day.",
                        nameof(resolutionAbsoluteDay));
                }

                return;
            }

            if (resolutionAbsoluteDay.HasValue == false
                || resolutionAbsoluteDay.Value < createdAbsoluteDay
                || resolutionAbsoluteDay.Value > observedAbsoluteDay)
            {
                throw new ArgumentException(
                    "A terminal claim requires a resolution day within its observed timeline.",
                    nameof(resolutionAbsoluteDay));
            }
        }
}

public sealed class FactionKnowledgeObservation : PoliticalKnowledgeObservation
{
    public FactionId FactionId { get; }
    public bool Exists { get; }

    public FactionKnowledgeObservation(
        FactionId factionId,
        bool exists,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeProvenance provenance)
        : base(
            PoliticalKnowledgeFactKind.Faction,
            observedAbsoluteDay,
            receivedAbsoluteDay,
            provenance)
    {
        FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        Exists = exists;
    }

    internal override string IdentityKey => FactionId.Value;
    internal override string SnapshotSortKey => BoolKey(Exists);
}

public sealed class FactionAffiliationKnowledgeObservation : PoliticalKnowledgeObservation
{
    public FactionId FactionId { get; }
    public PersonId PersonId { get; }
    public bool IsActive { get; }

    public FactionAffiliationKnowledgeObservation(
        FactionId factionId,
        PersonId personId,
        bool isActive,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeProvenance provenance)
        : base(
            PoliticalKnowledgeFactKind.FactionAffiliation,
            observedAbsoluteDay,
            receivedAbsoluteDay,
            provenance)
    {
        FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));
        IsActive = isActive;
    }

    internal override string IdentityKey => ComposeIdentityKey(FactionId.Value, PersonId.Value);
    internal override string SnapshotSortKey => BoolKey(IsActive);

    private static string ComposeIdentityKey(string factionId, string personId)
    {
        return factionId.Length + ":" + factionId + personId.Length + ":" + personId;
    }
}

public sealed class OfficeVacancyKnowledgeObservation : PoliticalKnowledgeObservation
{
    public OfficeId OfficeId { get; }
    public InstitutionId InstitutionId { get; }
    public bool IsVacant { get; }
    public bool IsRecognizedVacant { get; }
    public bool RecognizesVacancy => IsRecognizedVacant;

    public OfficeVacancyKnowledgeObservation(
        OfficeId officeId,
        InstitutionId institutionId,
        bool isVacant,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeProvenance provenance,
        bool isRecognizedVacant = false)
        : base(
            PoliticalKnowledgeFactKind.OfficeVacancy,
            observedAbsoluteDay,
            receivedAbsoluteDay,
            provenance)
    {
        OfficeId = officeId ?? throw new ArgumentNullException(nameof(officeId));
        InstitutionId = institutionId ?? throw new ArgumentNullException(nameof(institutionId));
        IsVacant = isVacant;
        IsRecognizedVacant = isRecognizedVacant;
    }

    internal override string IdentityKey => OfficeId.Value;
    internal override string SnapshotSortKey => InstitutionId.Value + "\u001F" + BoolKey(IsVacant);
}

public sealed class PersonDeathKnowledgeObservation : PoliticalKnowledgeObservation
{
    public PersonId PersonId { get; }
    public bool IsDead { get; }
    public bool IsAlive => IsDead == false;
    public long? DeathAbsoluteDay { get; }

    public PersonDeathKnowledgeObservation(
        PersonId personId,
        bool isDead,
        long? deathAbsoluteDay,
        long observedAbsoluteDay,
        long receivedAbsoluteDay,
        PoliticalKnowledgeProvenance provenance)
        : base(
            PoliticalKnowledgeFactKind.PersonDeath,
            observedAbsoluteDay,
            receivedAbsoluteDay,
            provenance)
    {
        if (personId == null)
        {
            throw new ArgumentNullException(nameof(personId));
        }

        ValidateOptionalDay(deathAbsoluteDay, nameof(deathAbsoluteDay));
        if (isDead == false && deathAbsoluteDay.HasValue)
        {
            throw new ArgumentException(
                "An alive snapshot cannot include a death day.",
                nameof(deathAbsoluteDay));
        }

        if (isDead == true
            && deathAbsoluteDay.HasValue
            && deathAbsoluteDay.Value > observedAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deathAbsoluteDay),
                "A death snapshot cannot place death after the observation day.");
        }

        PersonId = personId;
        IsDead = isDead;
        DeathAbsoluteDay = deathAbsoluteDay;
    }

    internal override string IdentityKey => PersonId.Value;
    internal override string SnapshotSortKey => BoolKey(IsDead) + "\u001F" + (DeathAbsoluteDay.HasValue ? DeathAbsoluteDay.Value.ToString() : string.Empty);
}

public sealed class PoliticalKnowledgeRuntime
{
    private readonly PoliticalKnowledgeHolder holder;
    private readonly Dictionary<string, PoliticalClaimKnowledgeObservation> claimObservations =
        new Dictionary<string, PoliticalClaimKnowledgeObservation>(StringComparer.Ordinal);
    private readonly Dictionary<string, FactionKnowledgeObservation> factionObservations =
        new Dictionary<string, FactionKnowledgeObservation>(StringComparer.Ordinal);
    private readonly Dictionary<string, FactionAffiliationKnowledgeObservation> affiliationObservations =
        new Dictionary<string, FactionAffiliationKnowledgeObservation>(StringComparer.Ordinal);
    private readonly Dictionary<string, OfficeVacancyKnowledgeObservation> officeVacancyObservations =
        new Dictionary<string, OfficeVacancyKnowledgeObservation>(StringComparer.Ordinal);
    private readonly Dictionary<string, PersonDeathKnowledgeObservation> personDeathObservations =
        new Dictionary<string, PersonDeathKnowledgeObservation>(StringComparer.Ordinal);

    public PoliticalKnowledgeHolder Holder => holder;
    public PoliticalKnowledgeHolderKind HolderKind => holder.Kind;
    public PersonId HolderPersonId => holder.PersonId;
    public InstitutionId HolderInstitutionId => holder.InstitutionId;

    public PoliticalKnowledgeRuntime(PoliticalKnowledgeHolder holder)
    {
        this.holder = holder ?? throw new ArgumentNullException(nameof(holder));
    }

    public PoliticalKnowledgeRuntime(PersonId holderPersonId)
        : this(PoliticalKnowledgeHolder.ForPerson(holderPersonId))
    {
    }

    public PoliticalKnowledgeRuntime(InstitutionId holderInstitutionId)
        : this(PoliticalKnowledgeHolder.ForInstitution(holderInstitutionId))
    {
    }

    public IReadOnlyList<PoliticalClaimKnowledgeObservation> ClaimObservations =>
        CreateSortedSnapshot(claimObservations.Values, CompareClaimObservations);

    public IReadOnlyList<FactionKnowledgeObservation> FactionObservations =>
        CreateSortedSnapshot(factionObservations.Values, CompareFactionObservations);

    public IReadOnlyList<FactionAffiliationKnowledgeObservation> FactionAffiliationObservations =>
        CreateSortedSnapshot(affiliationObservations.Values, CompareAffiliationObservations);

    public IReadOnlyList<OfficeVacancyKnowledgeObservation> OfficeVacancyObservations =>
        CreateSortedSnapshot(officeVacancyObservations.Values, CompareOfficeVacancyObservations);

    public IReadOnlyList<PersonDeathKnowledgeObservation> PersonDeathObservations =>
        CreateSortedSnapshot(personDeathObservations.Values, ComparePersonDeathObservations);

    public IReadOnlyList<PoliticalKnowledgeObservation> Observations
    {
        get
        {
            List<PoliticalKnowledgeObservation> observations = new List<PoliticalKnowledgeObservation>();
            observations.AddRange(claimObservations.Values);
            observations.AddRange(factionObservations.Values);
            observations.AddRange(affiliationObservations.Values);
            observations.AddRange(officeVacancyObservations.Values);
            observations.AddRange(personDeathObservations.Values);
            observations.Sort(CompareObservations);
            return new ReadOnlyCollection<PoliticalKnowledgeObservation>(observations);
        }
    }

    public bool RecordClaimObservation(PoliticalClaimKnowledgeObservation observation)
    {
        return Record(claimObservations, observation);
    }

    public bool CanImproveWith(PoliticalClaimKnowledgeObservation observation)
    {
        return CanImprove(claimObservations, observation);
    }

    public bool TryGetLatestClaimObservation(
        PoliticalClaimId claimId,
        out PoliticalClaimKnowledgeObservation observation)
    {
        return TryGet(claimObservations, claimId == null ? null : claimId.Value, out observation);
    }

    public bool TryGetCurrentClaimObservation(
        PoliticalClaimId claimId,
        out PoliticalClaimKnowledgeObservation observation)
    {
        return TryGetLatestClaimObservation(claimId, out observation);
    }

    public bool RecordFactionObservation(FactionKnowledgeObservation observation)
    {
        return Record(factionObservations, observation);
    }

    public bool CanImproveWith(FactionKnowledgeObservation observation)
    {
        return CanImprove(factionObservations, observation);
    }

    public bool TryGetLatestFactionObservation(
        FactionId factionId,
        out FactionKnowledgeObservation observation)
    {
        return TryGet(factionObservations, factionId == null ? null : factionId.Value, out observation);
    }

    public bool TryGetCurrentFactionObservation(
        FactionId factionId,
        out FactionKnowledgeObservation observation)
    {
        return TryGetLatestFactionObservation(factionId, out observation);
    }

    public bool RecordFactionAffiliationObservation(FactionAffiliationKnowledgeObservation observation)
    {
        return Record(affiliationObservations, observation);
    }

    public bool CanImproveWith(FactionAffiliationKnowledgeObservation observation)
    {
        return CanImprove(affiliationObservations, observation);
    }

    public bool TryGetLatestFactionAffiliationObservation(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationKnowledgeObservation observation)
    {
        string key = factionId == null || personId == null
            ? null
            : factionId.Value.Length + ":" + factionId.Value + personId.Value.Length + ":" + personId.Value;
        return TryGet(affiliationObservations, key, out observation);
    }

    public bool TryGetCurrentFactionAffiliationObservation(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationKnowledgeObservation observation)
    {
        return TryGetLatestFactionAffiliationObservation(factionId, personId, out observation);
    }

    public bool RecordOfficeVacancyObservation(OfficeVacancyKnowledgeObservation observation)
    {
        return Record(officeVacancyObservations, observation);
    }

    public bool CanImproveWith(OfficeVacancyKnowledgeObservation observation)
    {
        return CanImprove(officeVacancyObservations, observation);
    }

    public bool TryGetLatestOfficeVacancyObservation(
        OfficeId officeId,
        out OfficeVacancyKnowledgeObservation observation)
    {
        return TryGet(officeVacancyObservations, officeId == null ? null : officeId.Value, out observation);
    }

    public bool TryGetCurrentOfficeVacancyObservation(
        OfficeId officeId,
        out OfficeVacancyKnowledgeObservation observation)
    {
        return TryGetLatestOfficeVacancyObservation(officeId, out observation);
    }

    public bool RecordPersonDeathObservation(PersonDeathKnowledgeObservation observation)
    {
        return Record(personDeathObservations, observation);
    }

    public bool CanImproveWith(PersonDeathKnowledgeObservation observation)
    {
        return CanImprove(personDeathObservations, observation);
    }

    public bool TryGetLatestPersonDeathObservation(
        PersonId personId,
        out PersonDeathKnowledgeObservation observation)
    {
        return TryGet(personDeathObservations, personId == null ? null : personId.Value, out observation);
    }

    public bool TryGetCurrentPersonDeathObservation(
        PersonId personId,
        out PersonDeathKnowledgeObservation observation)
    {
        return TryGetLatestPersonDeathObservation(personId, out observation);
    }

    public PoliticalKnowledgeRuntime Clone()
    {
        PoliticalKnowledgeRuntime clone = new PoliticalKnowledgeRuntime(holder);
        foreach (KeyValuePair<string, PoliticalClaimKnowledgeObservation> entry in claimObservations)
        {
            clone.claimObservations.Add(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<string, FactionKnowledgeObservation> entry in factionObservations)
        {
            clone.factionObservations.Add(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<string, FactionAffiliationKnowledgeObservation> entry in affiliationObservations)
        {
            clone.affiliationObservations.Add(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<string, OfficeVacancyKnowledgeObservation> entry in officeVacancyObservations)
        {
            clone.officeVacancyObservations.Add(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<string, PersonDeathKnowledgeObservation> entry in personDeathObservations)
        {
            clone.personDeathObservations.Add(entry.Key, entry.Value);
        }

        return clone;
    }

    private static bool Record<T>(Dictionary<string, T> records, T observation)
        where T : PoliticalKnowledgeObservation
    {
        if (observation == null)
        {
            return false;
        }

        if (records.TryGetValue(observation.IdentityKey, out T existing) == false)
        {
            records.Add(observation.IdentityKey, observation);
            return true;
        }

        if (ShouldReplace(existing, observation) == false)
        {
            return false;
        }

        records[observation.IdentityKey] = observation;
        return true;
    }

    private static bool CanImprove<T>(Dictionary<string, T> records, T observation)
        where T : PoliticalKnowledgeObservation
    {
        return observation != null
            && (records.TryGetValue(observation.IdentityKey, out T existing) == false
                || ShouldReplace(existing, observation));
    }

    private static bool TryGet<T>(Dictionary<string, T> records, string key, out T observation)
        where T : PoliticalKnowledgeObservation
    {
        observation = null;
        return string.IsNullOrWhiteSpace(key) == false && records.TryGetValue(key, out observation);
    }

    private static bool ShouldReplace(
        PoliticalKnowledgeObservation existing,
        PoliticalKnowledgeObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        if (incoming.ObservedAbsoluteDay != existing.ObservedAbsoluteDay)
        {
            return incoming.ObservedAbsoluteDay > existing.ObservedAbsoluteDay;
        }

        int incomingSourcePriority = GetSourcePriority(incoming.Source);
        int existingSourcePriority = GetSourcePriority(existing.Source);
        if (incomingSourcePriority != existingSourcePriority)
        {
            return incomingSourcePriority > existingSourcePriority;
        }

        if (incoming.ReceivedAbsoluteDay != existing.ReceivedAbsoluteDay)
        {
            return incoming.ReceivedAbsoluteDay > existing.ReceivedAbsoluteDay;
        }

        return StringComparer.Ordinal.Compare(incoming.SnapshotSortKey, existing.SnapshotSortKey) > 0;
    }

    public static int GetSourcePriority(PoliticalKnowledgeSource source)
    {
        switch (source)
        {
            case PoliticalKnowledgeSource.DirectObservation:
                return 4;
            case PoliticalKnowledgeSource.InstitutionalRecord:
                return 3;
            case PoliticalKnowledgeSource.SharedByPerson:
            case PoliticalKnowledgeSource.SharedByInstitution:
                return 2;
            case PoliticalKnowledgeSource.InitialScenarioKnowledge:
                return 1;
            default:
                return 0;
        }
    }

    private static int CompareClaimObservations(
        PoliticalClaimKnowledgeObservation left,
        PoliticalClaimKnowledgeObservation right)
    {
        return StringComparer.Ordinal.Compare(left.ClaimId.Value, right.ClaimId.Value);
    }

    private static int CompareFactionObservations(
        FactionKnowledgeObservation left,
        FactionKnowledgeObservation right)
    {
        return StringComparer.Ordinal.Compare(left.FactionId.Value, right.FactionId.Value);
    }

    private static int CompareAffiliationObservations(
        FactionAffiliationKnowledgeObservation left,
        FactionAffiliationKnowledgeObservation right)
    {
        int faction = StringComparer.Ordinal.Compare(left.FactionId.Value, right.FactionId.Value);
        return faction != 0
            ? faction
            : StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value);
    }

    private static int CompareOfficeVacancyObservations(
        OfficeVacancyKnowledgeObservation left,
        OfficeVacancyKnowledgeObservation right)
    {
        return StringComparer.Ordinal.Compare(left.OfficeId.Value, right.OfficeId.Value);
    }

    private static int ComparePersonDeathObservations(
        PersonDeathKnowledgeObservation left,
        PersonDeathKnowledgeObservation right)
    {
        return StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value);
    }

    private static int CompareObservations(
        PoliticalKnowledgeObservation left,
        PoliticalKnowledgeObservation right)
    {
        int factKind = ((int)left.FactKind).CompareTo((int)right.FactKind);
        if (factKind != 0)
        {
            return factKind;
        }

        int identity = StringComparer.Ordinal.Compare(left.IdentityKey, right.IdentityKey);
        if (identity != 0)
        {
            return identity;
        }

        return StringComparer.Ordinal.Compare(left.SnapshotSortKey, right.SnapshotSortKey);
    }

    private static IReadOnlyList<T> CreateSortedSnapshot<T>(
        ICollection<T> source,
        Comparison<T> comparison)
    {
        List<T> snapshot = new List<T>(source);
        snapshot.Sort(comparison);
        return new ReadOnlyCollection<T>(snapshot);
    }
}

public sealed class PoliticalKnowledgePolicy
{
    private const int DefaultFreshForDays = 7;
    private const int DefaultMaxUsefulAgeDays = 30;

    private readonly int freshForDays;
    private readonly int maxUsefulAgeDays;

    public int FreshForDays => freshForDays;
    public int MaxUsefulAgeDays => maxUsefulAgeDays;

    public PoliticalKnowledgePolicy(
        int freshForDays = DefaultFreshForDays,
        int maxUsefulAgeDays = DefaultMaxUsefulAgeDays)
    {
        if (freshForDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(freshForDays));
        }

        if (maxUsefulAgeDays <= freshForDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxUsefulAgeDays),
                "MaxUsefulAgeDays must be greater than FreshForDays.");
        }

        this.freshForDays = freshForDays;
        this.maxUsefulAgeDays = maxUsefulAgeDays;
    }

    public long GetAgeDays(PoliticalKnowledgeObservation observation, long currentAbsoluteDay)
    {
        if (observation == null)
        {
            return 0L;
        }

        return observation.GetAgeDays(currentAbsoluteDay);
    }

    public bool IsFresh(PoliticalKnowledgeObservation observation, long currentAbsoluteDay)
    {
        return observation != null && GetAgeDays(observation, currentAbsoluteDay) <= freshForDays;
    }

    public float GetFreshness(PoliticalKnowledgeObservation observation, long currentAbsoluteDay)
    {
        if (observation == null)
        {
            return 0f;
        }

        long ageDays = GetAgeDays(observation, currentAbsoluteDay);
        if (ageDays <= freshForDays)
        {
            return 1f;
        }

        if (ageDays >= maxUsefulAgeDays)
        {
            return 0f;
        }

        float decayRange = maxUsefulAgeDays - freshForDays;
        return 1f - (ageDays - freshForDays) / decayRange;
    }
}
