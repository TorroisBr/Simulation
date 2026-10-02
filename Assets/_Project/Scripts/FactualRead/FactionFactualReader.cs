using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>A copied factual projection of the current Faction owner state.</summary>
public sealed class FactionTruthFacts
{
    private readonly ReadOnlyCollection<FactionFact> factions;
    private readonly ReadOnlyCollection<ActiveFactionAffiliationFact> activeAffiliations;

    internal FactionTruthFacts(
        IEnumerable<FactionFact> factions,
        IEnumerable<ActiveFactionAffiliationFact> activeAffiliations)
    {
        if (factions == null) throw new ArgumentNullException(nameof(factions));
        if (activeAffiliations == null) throw new ArgumentNullException(nameof(activeAffiliations));
        this.factions = new List<FactionFact>(factions).AsReadOnly();
        this.activeAffiliations = new List<ActiveFactionAffiliationFact>(activeAffiliations).AsReadOnly();
    }

    public IReadOnlyList<FactionFact> Factions => factions;
    public IReadOnlyList<ActiveFactionAffiliationFact> ActiveAffiliations => activeAffiliations;

    public FactReadResult<FactionFact> GetFaction(FactionId factionId)
    {
        if (factionId == null) throw new ArgumentNullException(nameof(factionId));
        for (int i = 0; i < factions.Count; i++)
        {
            if (factions[i].FactionId == factionId)
                return FactReadResult<FactionFact>.Present(factions[i]);
        }
        return FactReadResult<FactionFact>.Absent();
    }
}

/// <summary>Immutable source fact for one registered Faction.</summary>
public sealed class FactionFact
{
    internal FactionFact(
        FactionId factionId,
        string displayName,
        long createdAbsoluteDay,
        FactionMembershipPolicy membershipPolicy,
        bool expulsionAllowed)
    {
        FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
        CreatedAbsoluteDay = createdAbsoluteDay;
        MembershipPolicy = membershipPolicy;
        ExpulsionAllowed = expulsionAllowed;
    }

    public FactionId FactionId { get; }
    public string DisplayName { get; }
    public long CreatedAbsoluteDay { get; }
    public FactionMembershipPolicy MembershipPolicy { get; }
    public bool ExpulsionAllowed { get; }
}

/// <summary>Immutable source fact for a current, active Faction affiliation.</summary>
public sealed class ActiveFactionAffiliationFact
{
    internal ActiveFactionAffiliationFact(
        FactionAffiliationId factionAffiliationId,
        FactionId factionId,
        PersonId personId,
        long joinedAbsoluteDay)
    {
        FactionAffiliationId = factionAffiliationId ?? throw new ArgumentNullException(nameof(factionAffiliationId));
        FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));
        JoinedAbsoluteDay = joinedAbsoluteDay;
    }

    public FactionAffiliationId FactionAffiliationId { get; }
    public FactionId FactionId { get; }
    public PersonId PersonId { get; }
    public long JoinedAbsoluteDay { get; }
}

/// <summary>
/// Copies current Faction facts under the FR-B coherent-read lease. Returned
/// values contain copied IDs and no Store references.
/// </summary>
internal sealed class FactionFactualReader : IFactualReader<FactionTruthFacts>
{
    internal const string FactionTruthCapabilityId = "simulation.faction-truth/v1";
    private const string PersonEndpointMissingCode = "faction.active-affiliation.person-endpoint-missing";
    private const string PersonEndpointMissingMessage = "An active affiliation references an unavailable Person endpoint.";

    private readonly FactionStore factionStore;
    private readonly PersonStore personStore;

    internal FactionFactualReader(FactionStore factionStore, PersonStore personStore)
    {
        this.factionStore = factionStore ?? throw new ArgumentNullException(nameof(factionStore));
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        if (!factionStore.IsBoundToPersonStore(personStore))
            throw new ArgumentException("The factual reader requires the FactionStore's exact PersonStore.", nameof(personStore));
    }

    public string CapabilityId => FactionTruthCapabilityId;
    public int Version => 1;
    public Type ValueType => typeof(FactionTruthFacts);

    public FactualReadOutcome<FactionTruthFacts> Read()
    {
        IReadOnlyList<FactionRecord> sourceFactions = factionStore.Factions;
        IReadOnlyList<FactionAffiliationRecord> sourceAffiliations = factionStore.Affiliations;
        if (sourceFactions == null || sourceAffiliations == null)
            return Invalid("faction.source.unavailable", "Faction owner state could not be read.");

        List<FactionFact> factionFacts = new List<FactionFact>(sourceFactions.Count);
        HashSet<string> factionIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, long> factionCreatedDays = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (FactionRecord source in sourceFactions)
        {
            if (source == null || source.Id == null || string.IsNullOrWhiteSpace(source.Id.Value)
                || source.CreatedAbsoluteDay < 0L
                || !Enum.IsDefined(typeof(FactionMembershipPolicy), source.MembershipPolicy))
            {
                return Invalid("faction.record.invalid", "A registered Faction record is invalid.");
            }
            if (!factionIds.Add(source.Id.Value))
                return Invalid("faction.record.duplicate-id", "Faction owner state contains duplicate Faction identifiers.");
            factionCreatedDays.Add(source.Id.Value, source.CreatedAbsoluteDay);

            factionFacts.Add(new FactionFact(
                new FactionId(source.Id.Value),
                source.DisplayName,
                source.CreatedAbsoluteDay,
                source.MembershipPolicy,
                source.ExpulsionAllowed));
        }
        factionFacts.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.FactionId.Value,
            right.FactionId.Value));

        List<ActiveFactionAffiliationFact> activeFacts = new List<ActiveFactionAffiliationFact>();
        HashSet<string> affiliationIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<Tuple<string, string>> activePairs = new HashSet<Tuple<string, string>>();
        foreach (FactionAffiliationRecord source in sourceAffiliations)
        {
            if (source == null || source.AffiliationId == null || string.IsNullOrWhiteSpace(source.AffiliationId.Value)
                || source.FactionId == null || string.IsNullOrWhiteSpace(source.FactionId.Value)
                || source.PersonId == null || string.IsNullOrWhiteSpace(source.PersonId.Value)
                || source.JoinedAbsoluteDay < 0L
                || (source.EndedAbsoluteDay.HasValue && source.EndedAbsoluteDay.Value < source.JoinedAbsoluteDay)
                || (source.EndReason.HasValue
                    && (!source.EndedAbsoluteDay.HasValue
                        || !Enum.IsDefined(typeof(FactionAffiliationEndReason), source.EndReason.Value))))
            {
                return Invalid("faction.affiliation.invalid", "Faction owner state contains an invalid affiliation record.");
            }
            if (!affiliationIds.Add(source.AffiliationId.Value))
                return Invalid("faction.affiliation.duplicate-id", "Faction owner state contains duplicate affiliation identifiers.");
            if (!source.IsActive)
                continue;

            if (!factionCreatedDays.TryGetValue(source.FactionId.Value, out long createdAbsoluteDay))
            {
                return Invalid(
                    "faction.active-affiliation.faction-endpoint-missing",
                    "An active affiliation references an unavailable Faction endpoint.");
            }
            if (!personStore.TryGet(source.PersonId, out _))
                return FactualReadOutcome<FactionTruthFacts>.Unavailable(PersonEndpointMissingCode, PersonEndpointMissingMessage);
            if (!activePairs.Add(Tuple.Create(source.FactionId.Value, source.PersonId.Value)))
            {
                return Invalid("faction.active-affiliation.duplicate-pair", "Faction owner state contains duplicate active faction/person affiliations.");
            }
            if (source.JoinedAbsoluteDay < createdAbsoluteDay)
            {
                return Invalid("faction.active-affiliation.invalid-day", "An active affiliation has an impossible source day.");
            }

            activeFacts.Add(new ActiveFactionAffiliationFact(
                new FactionAffiliationId(source.AffiliationId.Value),
                new FactionId(source.FactionId.Value),
                new PersonId(source.PersonId.Value),
                source.JoinedAbsoluteDay));
        }
        activeFacts.Sort(CompareAffiliations);

        return FactualReadOutcome<FactionTruthFacts>.FromResult(
            FactReadResult<FactionTruthFacts>.Present(new FactionTruthFacts(factionFacts, activeFacts)));
    }

    private static FactualReadOutcome<FactionTruthFacts> Invalid(string code, string message)
    {
        return FactualReadOutcome<FactionTruthFacts>.Unavailable(code, message);
    }

    private static int CompareAffiliations(
        ActiveFactionAffiliationFact left,
        ActiveFactionAffiliationFact right)
    {
        int faction = StringComparer.Ordinal.Compare(left.FactionId.Value, right.FactionId.Value);
        if (faction != 0) return faction;
        int person = StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value);
        if (person != 0) return person;
        int joined = left.JoinedAbsoluteDay.CompareTo(right.JoinedAbsoluteDay);
        return joined != 0
            ? joined
            : StringComparer.Ordinal.Compare(
                left.FactionAffiliationId.Value,
                right.FactionAffiliationId.Value);
    }

    FactualReadOutcome<FactionTruthFacts> IFactualReader<FactionTruthFacts>.Read() => Read();
    IFactualReadOutcome IFactualReader.ReadUntyped() => Read();
}
