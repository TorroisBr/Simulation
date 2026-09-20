using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class FactionStore
{
    private readonly PersonStore personStore;
    private readonly object ownerToken = new object();
    private readonly Dictionary<string, FactionRecord> factionsById = new Dictionary<string, FactionRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, FactionAffiliationRecord> affiliationsByKey = new Dictionary<string, FactionAffiliationRecord>(StringComparer.Ordinal);
    private long revision;

    public FactionStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    internal object OwnerToken => ownerToken;

    public int Count => factionsById.Count;
    public int AffiliationCount => affiliationsByKey.Count;
    public long Revision => revision;

    public IReadOnlyList<FactionRecord> Factions
    {
        get
        {
            List<FactionRecord> values = new List<FactionRecord>(factionsById.Values);
            values.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id.Value, b.Id.Value));
            return new ReadOnlyCollection<FactionRecord>(values);
        }
    }

    public IReadOnlyList<FactionAffiliationRecord> Affiliations
    {
        get
        {
            List<FactionAffiliationRecord> values = new List<FactionAffiliationRecord>(affiliationsByKey.Values);
            values.Sort(CompareAffiliations);
            return new ReadOnlyCollection<FactionAffiliationRecord>(values);
        }
    }

    public bool TryRegister(FactionRecord record, out FactionFoundationFailure failure)
    {
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
        return TryGetKey(factionId, personId, out string key) && affiliationsByKey.TryGetValue(key, out record);
    }

    public IReadOnlyList<FactionAffiliationRecord> GetAffiliationsForFaction(FactionId factionId) => GetAffiliations(record => record.FactionId == factionId);
    public IReadOnlyList<FactionAffiliationRecord> GetAffiliationsForPerson(PersonId personId) => GetAffiliations(record => record.PersonId == personId);

    public bool TryRegisterAffiliation(FactionAffiliationRecord record, out FactionFoundationFailure failure)
    {
        if (record?.FactionId == null || record.PersonId == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A faction affiliation requires a FactionId and PersonId.");
            return false;
        }

        if (factionsById.ContainsKey(record.FactionId.Value) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.FactionNotRegistered,
                "The affiliation faction is not registered.");
            return false;
        }

        if (personStore.TryGet(record.PersonId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.PersonNotRegistered,
                "The affiliation PersonId is not registered.");
            return false;
        }

        string key = Key(record.FactionId, record.PersonId);
        if (affiliationsByKey.ContainsKey(key))
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.DuplicateAffiliation, "The faction/person affiliation is already registered.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RevisionOverflow, "The faction store revision cannot advance further.");
            return false;
        }

        affiliationsByKey.Add(key, record);
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    internal bool TryApplyAdd(FactionAffiliationAddTransition transition, out FactionFoundationFailure failure)
    {
        if (transition == null || transition.Affiliation == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A valid affiliation add transition is required.");
            return false;
        }

        if (ReferenceEquals(transition.ExpectedStore, this) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.WrongFactionStore,
                "The affiliation transition belongs to another faction store.");
            return false;
        }

        if (revision != transition.ExpectedStoreRevision)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.StaleAffiliation, "The faction store changed after the affiliation was proposed.");
            return false;
        }

        string key = Key(transition.Affiliation.FactionId, transition.Affiliation.PersonId);
        if (affiliationsByKey.ContainsKey(key))
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.StaleAffiliation, "The faction/person affiliation changed after the proposal.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.RevisionOverflow, "The faction store revision cannot advance further.");
            return false;
        }

        affiliationsByKey.Add(key, transition.Affiliation);
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    internal bool TryApplyEnd(FactionAffiliationEndTransition transition, out FactionFoundationFailure failure)
    {
        if (transition == null || transition.ExpectedAffiliation == null)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.InvalidTransition, "A valid affiliation end transition is required.");
            return false;
        }

        if (ReferenceEquals(transition.ExpectedStore, this) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.WrongFactionStore,
                "The affiliation transition belongs to another faction store.");
            return false;
        }

        if (revision != transition.ExpectedStoreRevision)
        {
            failure = FactionFoundationFailure.Create(FactionFoundationFailureCode.StaleAffiliation, "The faction store changed after the affiliation end was proposed.");
            return false;
        }

        string key = Key(transition.ExpectedAffiliation.FactionId, transition.ExpectedAffiliation.PersonId);
        if (affiliationsByKey.TryGetValue(key, out FactionAffiliationRecord current) == false
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

        affiliationsByKey[key] = current.WithEnd(transition.EndedAbsoluteDay);
        revision++;
        failure = FactionFoundationFailure.None;
        return true;
    }

    internal FactionStore Clone()
    {
        FactionStore clone = new FactionStore(personStore);
        foreach (KeyValuePair<string, FactionRecord> entry in factionsById) clone.factionsById.Add(entry.Key, entry.Value);
        foreach (KeyValuePair<string, FactionAffiliationRecord> entry in affiliationsByKey) clone.affiliationsByKey.Add(entry.Key, entry.Value);
        clone.revision = revision;
        return clone;
    }

    private IReadOnlyList<FactionAffiliationRecord> GetAffiliations(Func<FactionAffiliationRecord, bool> predicate)
    {
        List<FactionAffiliationRecord> result = new List<FactionAffiliationRecord>();
        if (predicate != null) foreach (FactionAffiliationRecord record in affiliationsByKey.Values) if (predicate(record)) result.Add(record);
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
        return faction != 0 ? faction : StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value);
    }
}
