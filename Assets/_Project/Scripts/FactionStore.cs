using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class FactionStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly object ownerToken = new object();
    private readonly Dictionary<string, FactionRecord> factionsById =
        new Dictionary<string, FactionRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, FactionAffiliationRecord> affiliationsById =
        new Dictionary<string, FactionAffiliationRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> activeAffiliationIdByPair =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private long revision;

    public FactionStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    internal object OwnerToken => ownerToken;
    public int Count => factionsById.Count;
    public int AffiliationCount => affiliationsById.Count;
    public long Revision => revision;

    public IReadOnlyList<FactionRecord> Factions
    {
        get
        {
            List<FactionRecord> values = new List<FactionRecord>(factionsById.Values);
            values.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
            return new ReadOnlyCollection<FactionRecord>(values);
        }
    }

    public IReadOnlyList<FactionAffiliationRecord> Affiliations
    {
        get
        {
            List<FactionAffiliationRecord> values = new List<FactionAffiliationRecord>(affiliationsById.Values);
            values.Sort(CompareAffiliations);
            return new ReadOnlyCollection<FactionAffiliationRecord>(values);
        }
    }

    public bool TryRegister(FactionRecord record, out FactionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
            return false;
        }

        if (record?.Id == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidFaction, "A faction with a stable FactionId is required.");
            return false;
        }
        if (factionsById.ContainsKey(record.Id.Value))
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.DuplicateFactionId, "The FactionId is already registered.");
            return false;
        }
        if (revision == long.MaxValue)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RevisionOverflow, "The faction store revision cannot advance further.");
            return false;
        }

        factionsById.Add(record.Id.Value, record);
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    public bool TryGet(FactionId factionId, out FactionRecord record)
    {
        record = null;
        return factionId != null && factionsById.TryGetValue(factionId.Value, out record);
    }

    public bool TryGetAffiliation(FactionId factionId, PersonId personId, out FactionAffiliationRecord record)
    {
        record = null;
        return TryGetKey(factionId, personId, out string key)
            && activeAffiliationIdByPair.TryGetValue(key, out string affiliationId)
            && affiliationsById.TryGetValue(affiliationId, out record);
    }

    public bool TryGetAffiliation(FactionAffiliationId affiliationId, out FactionAffiliationRecord record)
    {
        record = null;
        return affiliationId != null && affiliationsById.TryGetValue(affiliationId.Value, out record);
    }

    public IReadOnlyList<FactionAffiliationRecord> GetAffiliationsForFaction(FactionId factionId)
    {
        return GetAffiliations(record => record.FactionId == factionId);
    }

    public IReadOnlyList<FactionAffiliationRecord> GetAffiliationsForPerson(PersonId personId)
    {
        return GetAffiliations(record => record.PersonId == personId);
    }

    internal int GetHistoricalAffiliationCount(FactionId factionId, PersonId personId)
    {
        int count = 0;
        foreach (FactionAffiliationRecord record in affiliationsById.Values)
        {
            if (record.FactionId == factionId && record.PersonId == personId)
            {
                count++;
            }
        }

        return count;
    }

    public bool TryRegisterAffiliation(FactionAffiliationRecord record, out FactionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
            return false;
        }

        if (ValidateAffiliationEndpoints(record, out failure) == false
            || ValidateNewAffiliation(record, out failure) == false)
        {
            return false;
        }
        if (revision == long.MaxValue)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RevisionOverflow, "The faction store revision cannot advance further.");
            return false;
        }

        affiliationsById.Add(record.AffiliationId.Value, record);
        if (record.IsActive)
        {
            activeAffiliationIdByPair.Add(Key(record.FactionId, record.PersonId), record.AffiliationId.Value);
        }
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    internal bool TryApplyAdd(FactionAffiliationAddTransition transition, out FactionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
            return false;
        }

        if (transition == null || transition.Affiliation == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A valid affiliation add transition is required.");
            return false;
        }
        if (ReferenceEquals(transition.ExpectedStore, this) == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.WrongFactionStore, "The affiliation transition belongs to another faction store.");
            return false;
        }
        if (revision != transition.ExpectedStoreRevision)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.StaleAffiliation, "The faction store changed after the affiliation was proposed.");
            return false;
        }
        if (ValidateAffiliationEndpoints(transition.Affiliation, out failure) == false
            || ValidateNewAffiliation(transition.Affiliation, out failure) == false)
        {
            return false;
        }
        if (revision == long.MaxValue)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RevisionOverflow, "The faction store revision cannot advance further.");
            return false;
        }

        affiliationsById.Add(transition.Affiliation.AffiliationId.Value, transition.Affiliation);
        activeAffiliationIdByPair.Add(Key(transition.Affiliation.FactionId, transition.Affiliation.PersonId), transition.Affiliation.AffiliationId.Value);
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    internal bool TryApplyEnd(FactionAffiliationEndTransition transition, out FactionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
            return false;
        }

        if (transition == null || transition.ExpectedAffiliation == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A valid affiliation end transition is required.");
            return false;
        }
        if (ReferenceEquals(transition.ExpectedStore, this) == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.WrongFactionStore, "The affiliation transition belongs to another faction store.");
            return false;
        }
        if (revision != transition.ExpectedStoreRevision)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.StaleAffiliation, "The faction store changed after the affiliation end was proposed.");
            return false;
        }
        if (affiliationsById.TryGetValue(transition.ExpectedAffiliation.AffiliationId.Value, out FactionAffiliationRecord current) == false
            || ReferenceEquals(current, transition.ExpectedAffiliation) == false
            || current.IsActive == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.StaleAffiliation, "The faction affiliation changed after the proposal.");
            return false;
        }
        if (revision == long.MaxValue)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RevisionOverflow, "The faction store revision cannot advance further.");
            return false;
        }

        affiliationsById[current.AffiliationId.Value] = current.WithEnd(
            transition.EndedAbsoluteDay,
            transition.EndReason);
        activeAffiliationIdByPair.Remove(Key(current.FactionId, current.PersonId));
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    internal FactionStore Clone(PersonStore targetPersonStore = null)
    {
        FactionStore clone = new FactionStore(targetPersonStore ?? personStore);
        foreach (KeyValuePair<string, FactionRecord> entry in factionsById) clone.factionsById.Add(entry.Key, entry.Value);
        foreach (KeyValuePair<string, FactionAffiliationRecord> entry in affiliationsById) clone.affiliationsById.Add(entry.Key, entry.Value);
        foreach (KeyValuePair<string, string> entry in activeAffiliationIdByPair) clone.activeAffiliationIdByPair.Add(entry.Key, entry.Value);
        clone.revision = revision;
        return clone;
    }

    private bool ValidateAffiliationEndpoints(FactionAffiliationRecord record, out FactionFoundationFailure failure)
    {
        if (record?.FactionId == null || record.PersonId == null || record.AffiliationId == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A faction affiliation requires stable affiliation, FactionId, and PersonId values.");
            return false;
        }
        if (factionsById.ContainsKey(record.FactionId.Value) == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.FactionNotRegistered, "The affiliation faction is not registered.");
            return false;
        }
        if (personStore.TryGet(record.PersonId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.PersonNotRegistered, "The affiliation PersonId is not registered.");
            return false;
        }
        failure = FactionFoundationFailure.None;
        return true;
    }

    private bool ValidateNewAffiliation(FactionAffiliationRecord record, out FactionFoundationFailure failure)
    {
        if (affiliationsById.ContainsKey(record.AffiliationId.Value))
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.DuplicateAffiliation, "The affiliation id is already registered.");
            return false;
        }
        string key = Key(record.FactionId, record.PersonId);
        if (activeAffiliationIdByPair.ContainsKey(key))
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.DuplicateAffiliation, "The faction/person affiliation already has an active tenure.");
            return false;
        }
        failure = FactionFoundationFailure.None;
        return true;
    }

    private IReadOnlyList<FactionAffiliationRecord> GetAffiliations(Func<FactionAffiliationRecord, bool> predicate)
    {
        List<FactionAffiliationRecord> result = new List<FactionAffiliationRecord>();
        foreach (FactionAffiliationRecord record in affiliationsById.Values)
        {
            if (predicate == null || predicate(record)) result.Add(record);
        }
        result.Sort(CompareAffiliations);
        return new ReadOnlyCollection<FactionAffiliationRecord>(result);
    }

    private static bool TryGetKey(FactionId factionId, PersonId personId, out string key)
    {
        key = null;
        if (factionId == null || personId == null) return false;
        key = Key(factionId, personId);
        return true;
    }

    private static string Key(FactionId factionId, PersonId personId) => factionId.Value + "\u001f" + personId.Value;

    private static int CompareAffiliations(FactionAffiliationRecord left, FactionAffiliationRecord right)
    {
        int faction = StringComparer.Ordinal.Compare(left.FactionId.Value, right.FactionId.Value);
        if (faction != 0) return faction;
        int person = StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value);
        return person != 0 ? person : StringComparer.Ordinal.Compare(left.AffiliationId.Value, right.AffiliationId.Value);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard) && personStore.CanBindMutationGuard(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard)
            && personStore.TryBindMutationGuard(guard)
            && mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
