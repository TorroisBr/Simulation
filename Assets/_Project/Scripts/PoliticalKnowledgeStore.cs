using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum PoliticalKnowledgeFailureCode
{
    None = 0,
    InvalidHolder = 1,
    HolderPersonNotRegistered = 2,
    HolderInstitutionNotRegistered = 3,
    DuplicateHolder = 4,
    InvalidObservation = 5,
    FutureObservation = 6,
    KnowledgeNotImproved = 7
}

public sealed class PoliticalKnowledgeFailure : IEquatable<PoliticalKnowledgeFailure>
{
    private static readonly PoliticalKnowledgeFailure none =
        new PoliticalKnowledgeFailure(PoliticalKnowledgeFailureCode.None, string.Empty);

    private PoliticalKnowledgeFailure(PoliticalKnowledgeFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PoliticalKnowledgeFailure None => none;
    public PoliticalKnowledgeFailureCode Code { get; }
    public string Message { get; }

    public static PoliticalKnowledgeFailure Create(
        PoliticalKnowledgeFailureCode code,
        string message)
    {
        return code == PoliticalKnowledgeFailureCode.None
            ? None
            : new PoliticalKnowledgeFailure(code, message);
    }

    public bool Equals(PoliticalKnowledgeFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalKnowledgeFailure);
    public override int GetHashCode() => ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

/// <summary>
/// World-owned container for political knowledge. It stores beliefs by stable PersonId
/// or InstitutionId and never rewrites the factual political stores.
/// </summary>
public sealed class PoliticalKnowledgeStore
{
    private readonly PersonStore personStore;
    private readonly InstitutionStore institutionStore;
    private readonly Dictionary<string, PoliticalKnowledgeRuntime> runtimesByHolder =
        new Dictionary<string, PoliticalKnowledgeRuntime>(StringComparer.Ordinal);

    public PoliticalKnowledgeStore(PersonStore personStore, InstitutionStore institutionStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.institutionStore = institutionStore ?? throw new ArgumentNullException(nameof(institutionStore));
    }

    public int Count => runtimesByHolder.Count;
    public long Revision { get; private set; }

    public IReadOnlyList<PoliticalKnowledgeRuntime> Runtimes
    {
        get
        {
            List<PoliticalKnowledgeRuntime> result = new List<PoliticalKnowledgeRuntime>();
            foreach (PoliticalKnowledgeRuntime runtime in runtimesByHolder.Values)
            {
                result.Add(runtime.Clone());
            }

            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.Holder.StableId, right.Holder.StableId));
            return new ReadOnlyCollection<PoliticalKnowledgeRuntime>(result);
        }
    }

    public bool TryRegisterHolder(
        PoliticalKnowledgeHolder holder,
        long currentWorldDay,
        out PoliticalKnowledgeFailure failure)
    {
        if (ValidateHolder(holder, out failure) == false)
        {
            return false;
        }

        if (currentWorldDay < 0L)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.FutureObservation,
                "The world day cannot be negative.");
            return false;
        }

        if (runtimesByHolder.ContainsKey(holder.StableId))
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.DuplicateHolder,
                "Political knowledge for the holder is already registered.");
            return false;
        }

        runtimesByHolder.Add(holder.StableId, new PoliticalKnowledgeRuntime(holder));
        if (Revision == long.MaxValue)
        {
            runtimesByHolder.Remove(holder.StableId);
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.KnowledgeNotImproved,
                "The political knowledge store revision cannot advance further.");
            return false;
        }

        Revision++;
        failure = PoliticalKnowledgeFailure.None;
        return true;
    }

    public bool TryRegister(
        PoliticalKnowledgeRuntime runtime,
        long currentWorldDay,
        out PoliticalKnowledgeFailure failure)
    {
        failure = PoliticalKnowledgeFailure.None;
        if (runtime == null)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.InvalidHolder,
                "A political knowledge runtime is required.");
            return false;
        }

        if (ValidateHolder(runtime.Holder, out failure) == false
            || ValidateObservations(runtime.Observations, currentWorldDay, out failure) == false)
        {
            return false;
        }

        if (runtimesByHolder.ContainsKey(runtime.Holder.StableId))
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.DuplicateHolder,
                "Political knowledge for the holder is already registered.");
            return false;
        }

        runtimesByHolder.Add(runtime.Holder.StableId, runtime.Clone());
        if (Revision == long.MaxValue)
        {
            runtimesByHolder.Remove(runtime.Holder.StableId);
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.KnowledgeNotImproved,
                "The political knowledge store revision cannot advance further.");
            return false;
        }

        Revision++;
        return true;
    }

    public bool TryGet(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeRuntime runtime)
    {
        runtime = null;
        if (holder == null || runtimesByHolder.TryGetValue(holder.StableId, out PoliticalKnowledgeRuntime owned) == false)
        {
            return false;
        }

        runtime = owned.Clone();
        return true;
    }

    public bool TryRecordObservation(
        PoliticalKnowledgeHolder holder,
        PoliticalKnowledgeObservation observation,
        long currentWorldDay,
        out PoliticalKnowledgeFailure failure)
    {
        failure = PoliticalKnowledgeFailure.None;
        if (observation == null)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.InvalidObservation,
                "A political knowledge observation is required.");
            return false;
        }

        if (ValidateObservation(observation, currentWorldDay, out failure) == false
            || TryGetOwned(holder, out PoliticalKnowledgeRuntime runtime) == false)
        {
            if (failure.Code == PoliticalKnowledgeFailureCode.None)
            {
                failure = PoliticalKnowledgeFailure.Create(
                    PoliticalKnowledgeFailureCode.InvalidHolder,
                    "The political knowledge holder is not registered in this world.");
            }

            return false;
        }

        if (Revision == long.MaxValue)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.KnowledgeNotImproved,
                "The political knowledge store revision cannot advance further.");
            return false;
        }

        bool recorded;
        switch (observation.FactKind)
        {
            case PoliticalKnowledgeFactKind.PoliticalClaim:
                recorded = runtime.RecordClaimObservation((PoliticalClaimKnowledgeObservation)observation);
                break;
            case PoliticalKnowledgeFactKind.Faction:
                recorded = runtime.RecordFactionObservation((FactionKnowledgeObservation)observation);
                break;
            case PoliticalKnowledgeFactKind.FactionAffiliation:
                recorded = runtime.RecordFactionAffiliationObservation((FactionAffiliationKnowledgeObservation)observation);
                break;
            case PoliticalKnowledgeFactKind.OfficeVacancy:
                recorded = runtime.RecordOfficeVacancyObservation((OfficeVacancyKnowledgeObservation)observation);
                break;
            case PoliticalKnowledgeFactKind.PersonDeath:
                recorded = runtime.RecordPersonDeathObservation((PersonDeathKnowledgeObservation)observation);
                break;
            default:
                failure = PoliticalKnowledgeFailure.Create(
                    PoliticalKnowledgeFailureCode.InvalidObservation,
                    "The political knowledge observation kind is invalid.");
                return false;
        }

        if (recorded == false)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.KnowledgeNotImproved,
                "The observation did not improve the holder's current knowledge.");
            return false;
        }

        Revision++;
        return true;
    }

    internal PoliticalKnowledgeStore Clone(
        PersonStore targetPersonStore,
        InstitutionStore targetInstitutionStore,
        long currentWorldDay)
    {
        PoliticalKnowledgeStore clone = new PoliticalKnowledgeStore(
            targetPersonStore ?? throw new ArgumentNullException(nameof(targetPersonStore)),
            targetInstitutionStore ?? throw new ArgumentNullException(nameof(targetInstitutionStore)));

        foreach (PoliticalKnowledgeRuntime runtime in runtimesByHolder.Values)
        {
            if (clone.TryRegister(runtime, currentWorldDay, out PoliticalKnowledgeFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalKnowledgeStore contains invalid knowledge: " + failure + ".");
            }
        }

        clone.Revision = Revision;
        if (clone.Revision != Revision)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalKnowledgeStore revision is inconsistent with its state.");
        }

        return clone;
    }

    private bool TryGetOwned(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeRuntime runtime)
    {
        runtime = null;
        return holder != null && runtimesByHolder.TryGetValue(holder.StableId, out runtime);
    }

    private bool ValidateHolder(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeFailure failure)
    {
        failure = PoliticalKnowledgeFailure.None;
        if (holder == null)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.InvalidHolder,
                "A political knowledge holder is required.");
            return false;
        }

        if (holder.Kind == PoliticalKnowledgeHolderKind.Person
            && personStore.TryGet(holder.PersonId, out _) == false)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.HolderPersonNotRegistered,
                "The political knowledge PersonId is not registered in this world.");
            return false;
        }

        if (holder.Kind == PoliticalKnowledgeHolderKind.Institution
            && institutionStore.TryGet(holder.InstitutionId, out _) == false)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.HolderInstitutionNotRegistered,
                "The political knowledge InstitutionId is not registered in this world.");
            return false;
        }

        return true;
    }

    private static bool ValidateObservations(
        IReadOnlyList<PoliticalKnowledgeObservation> observations,
        long currentWorldDay,
        out PoliticalKnowledgeFailure failure)
    {
        foreach (PoliticalKnowledgeObservation observation in observations)
        {
            if (ValidateObservation(observation, currentWorldDay, out failure) == false)
            {
                return false;
            }
        }

        failure = PoliticalKnowledgeFailure.None;
        return true;
    }

    private static bool ValidateObservation(
        PoliticalKnowledgeObservation observation,
        long currentWorldDay,
        out PoliticalKnowledgeFailure failure)
    {
        failure = PoliticalKnowledgeFailure.None;
        if (observation == null)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.InvalidObservation,
                "A political knowledge observation is required.");
            return false;
        }

        if (currentWorldDay < 0L
            || observation.ObservedAbsoluteDay > currentWorldDay
            || observation.ReceivedAbsoluteDay > currentWorldDay)
        {
            failure = PoliticalKnowledgeFailure.Create(
                PoliticalKnowledgeFailureCode.FutureObservation,
                "Political knowledge cannot be observed or received in the future of the world timeline.");
            return false;
        }

        return true;
    }
}
