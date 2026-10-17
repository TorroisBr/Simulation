using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;

public sealed class RuntimeIdAllocatorCounterSnapshot
{
    public string FamilyId { get; }
    public long NextSequence { get; }

    public RuntimeIdAllocatorCounterSnapshot(string familyId, long nextSequence)
    {
        FamilyId = familyId;
        NextSequence = nextSequence;
    }
}

public sealed class RuntimeIdAllocatorSnapshot
{
    public const string CurrentSchemaId = "runtime-id-allocator";
    public const int CurrentSchemaVersion = 1;

    private readonly ReadOnlyCollection<RuntimeIdAllocatorCounterSnapshot> counters;

    public string SchemaId { get; }
    public int SchemaVersion { get; }
    public IReadOnlyList<RuntimeIdAllocatorCounterSnapshot> Counters => counters;

    public RuntimeIdAllocatorSnapshot(
        string schemaId,
        int schemaVersion,
        IEnumerable<RuntimeIdAllocatorCounterSnapshot> counters)
    {
        SchemaId = schemaId;
        SchemaVersion = schemaVersion;

        if (counters != null)
        {
            this.counters = new ReadOnlyCollection<RuntimeIdAllocatorCounterSnapshot>(
                new List<RuntimeIdAllocatorCounterSnapshot>(counters));
        }
    }
}

public sealed class RuntimeIdAllocator
{
    private static readonly string[] SupportedFamilyIds =
    {
        "npc", "city", "location", "route", "event", "directive", "decision",
        "travel-party", "organization", "site", "expedition", "local-place",
        "local-connection", "notable-item"
    };

    private readonly object censusOwnerIdentity = new object();
    private long nextNpcSequence = 1;
    private long nextCitySequence = 1;
    private long nextLocationSequence = 1;
    private long nextRouteSequence = 1;
    private long nextEventSequence = 1;
    private long nextDirectiveSequence = 1;
    private long nextDecisionSequence = 1;
    private long nextTravelPartySequence = 1;
    private long nextOrganizationSequence = 1;
    private long nextExplorableSiteSequence = 1;
    private long nextExpeditionSequence = 1;
    private long nextLocalPlaceSequence = 1;
    private long nextLocalConnectionSequence = 1;
    private long nextNotableItemSequence = 1;
    private Func<bool> p12EventIdMutationAdmission;
    private Action p12EventIdMutationCommitted;
    private Func<bool> p12DecisionIdMutationAdmission;
    private Action p12DecisionIdMutationCommitted;
    private Func<bool> p12TravelPartyIdMutationAdmission;
    private Action p12TravelPartyIdMutationCommitted;

    public RuntimeIdAllocator()
    {
    }

    internal object CensusOwnerIdentity => censusOwnerIdentity;

    internal long GetCensusRevision(RuntimeIdAllocatorCensusCounter counter)
    {
        switch (counter)
        {
            case RuntimeIdAllocatorCensusCounter.Npcs: return nextNpcSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Cities: return nextCitySequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Locations: return nextLocationSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Routes: return nextRouteSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Events: return nextEventSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Directives: return nextDirectiveSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Decisions: return nextDecisionSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.TravelParties: return nextTravelPartySequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Organizations: return nextOrganizationSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.ExplorableSites: return nextExplorableSiteSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.Expeditions: return nextExpeditionSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.LocalPlaces: return nextLocalPlaceSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.LocalConnections: return nextLocalConnectionSequence - 1L;
            case RuntimeIdAllocatorCensusCounter.NotableItems: return nextNotableItemSequence - 1L;
            default: throw new ArgumentOutOfRangeException(nameof(counter));
        }
    }

    public RuntimeIdAllocatorSnapshot CaptureSnapshot()
    {
        return new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            new[]
            {
                new RuntimeIdAllocatorCounterSnapshot("npc", nextNpcSequence),
                new RuntimeIdAllocatorCounterSnapshot("city", nextCitySequence),
                new RuntimeIdAllocatorCounterSnapshot("location", nextLocationSequence),
                new RuntimeIdAllocatorCounterSnapshot("route", nextRouteSequence),
                new RuntimeIdAllocatorCounterSnapshot("event", nextEventSequence),
                new RuntimeIdAllocatorCounterSnapshot("directive", nextDirectiveSequence),
                new RuntimeIdAllocatorCounterSnapshot("decision", nextDecisionSequence),
                new RuntimeIdAllocatorCounterSnapshot("travel-party", nextTravelPartySequence),
                new RuntimeIdAllocatorCounterSnapshot("organization", nextOrganizationSequence),
                new RuntimeIdAllocatorCounterSnapshot("site", nextExplorableSiteSequence),
                new RuntimeIdAllocatorCounterSnapshot("expedition", nextExpeditionSequence),
                new RuntimeIdAllocatorCounterSnapshot("local-place", nextLocalPlaceSequence),
                new RuntimeIdAllocatorCounterSnapshot("local-connection", nextLocalConnectionSequence),
                new RuntimeIdAllocatorCounterSnapshot("notable-item", nextNotableItemSequence)
            });
    }

    internal static bool TryCreateStagedFromSnapshot(
        RuntimeIdAllocatorSnapshot snapshot,
        out RuntimeIdAllocator allocator,
        out string diagnostic)
    {
        allocator = null;
        diagnostic = null;

        if (snapshot == null)
        {
            diagnostic = "Runtime ID allocator snapshot is null.";
            return false;
        }

        if (string.Equals(snapshot.SchemaId, RuntimeIdAllocatorSnapshot.CurrentSchemaId, StringComparison.Ordinal) == false
            || snapshot.SchemaVersion != RuntimeIdAllocatorSnapshot.CurrentSchemaVersion)
        {
            diagnostic = "Runtime ID allocator snapshot schema is unsupported.";
            return false;
        }

        if (snapshot.Counters == null)
        {
            diagnostic = "Runtime ID allocator snapshot counters are missing.";
            return false;
        }

        Dictionary<string, long> nextSequences = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (RuntimeIdAllocatorCounterSnapshot counter in snapshot.Counters)
        {
            if (counter == null || string.IsNullOrWhiteSpace(counter.FamilyId) == true)
            {
                diagnostic = "Runtime ID allocator snapshot contains a missing family.";
                return false;
            }

            if (ArrayContains(SupportedFamilyIds, counter.FamilyId) == false)
            {
                diagnostic = "Runtime ID allocator snapshot contains an unknown family '" + counter.FamilyId + "'.";
                return false;
            }

            if (nextSequences.ContainsKey(counter.FamilyId) == true)
            {
                diagnostic = "Runtime ID allocator snapshot contains duplicate family '" + counter.FamilyId + "'.";
                return false;
            }

            if (counter.NextSequence < 1L || counter.NextSequence == long.MaxValue)
            {
                diagnostic = "Runtime ID allocator snapshot contains an invalid or exhausted next sequence for '" + counter.FamilyId + "'.";
                return false;
            }

            nextSequences.Add(counter.FamilyId, counter.NextSequence);
        }

        if (nextSequences.Count != SupportedFamilyIds.Length)
        {
            diagnostic = "Runtime ID allocator snapshot does not contain the exact supported family set.";
            return false;
        }

        foreach (string familyId in SupportedFamilyIds)
        {
            if (nextSequences.ContainsKey(familyId) == false)
            {
                diagnostic = "Runtime ID allocator snapshot is missing family '" + familyId + "'.";
                return false;
            }
        }

        allocator = new RuntimeIdAllocator(nextSequences);
        return true;
    }

    private RuntimeIdAllocator(IDictionary<string, long> nextSequences)
    {
        nextNpcSequence = nextSequences["npc"];
        nextCitySequence = nextSequences["city"];
        nextLocationSequence = nextSequences["location"];
        nextRouteSequence = nextSequences["route"];
        nextEventSequence = nextSequences["event"];
        nextDirectiveSequence = nextSequences["directive"];
        nextDecisionSequence = nextSequences["decision"];
        nextTravelPartySequence = nextSequences["travel-party"];
        nextOrganizationSequence = nextSequences["organization"];
        nextExplorableSiteSequence = nextSequences["site"];
        nextExpeditionSequence = nextSequences["expedition"];
        nextLocalPlaceSequence = nextSequences["local-place"];
        nextLocalConnectionSequence = nextSequences["local-connection"];
        nextNotableItemSequence = nextSequences["notable-item"];
    }

    private static bool ArrayContains(string[] values, string expected)
    {
        for (int index = 0; index < values.Length; index++)
        {
            if (string.Equals(values[index], expected, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    public string AllocateNpcId()
    {
        return Allocate("npc", ref nextNpcSequence);
    }

    public string AllocateCityId()
    {
        return Allocate("city", ref nextCitySequence);
    }

    public string AllocateLocationId()
    {
        return Allocate("location", ref nextLocationSequence);
    }

    public string AllocateRouteId()
    {
        return Allocate("route", ref nextRouteSequence);
    }

    public string AllocateEventId()
    {
        if (nextEventSequence == long.MaxValue)
        {
            throw new InvalidOperationException("RuntimeId sequence exhausted for type 'event'.");
        }

        if (p12EventIdMutationAdmission != null && !CanCommitP12EventIdMutation())
        {
            throw new InvalidOperationException("The P12 Event-ID mutation was rejected before allocation.");
        }

        string runtimeId = "event-" + nextEventSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextEventSequence++;
        p12EventIdMutationCommitted?.Invoke();
        return runtimeId;
    }

    internal void BindP12EventIdMutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12EventIdMutationAdmission != null || p12EventIdMutationCommitted != null)
            throw new InvalidOperationException("RuntimeIdAllocator Event IDs are already bound to a P12 mutation boundary.");
        p12EventIdMutationAdmission = admission;
        p12EventIdMutationCommitted = committed;
    }

    private bool CanCommitP12EventIdMutation()
    {
        try { return p12EventIdMutationAdmission(); }
        catch { return false; }
    }

    public string AllocateDirectiveId()
    {
        return Allocate("directive", ref nextDirectiveSequence);
    }

    public string AllocateDecisionId()
    {
        if (nextDecisionSequence == long.MaxValue)
        {
            throw new InvalidOperationException("RuntimeId sequence exhausted for type 'decision'.");
        }

        if (p12DecisionIdMutationAdmission != null && !CanCommitP12DecisionIdMutation())
        {
            throw new InvalidOperationException("The P12 Decision-ID mutation was rejected before allocation.");
        }

        string runtimeId = "decision-" + nextDecisionSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextDecisionSequence++;
        p12DecisionIdMutationCommitted?.Invoke();
        return runtimeId;
    }

    internal void BindP12DecisionIdMutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12DecisionIdMutationAdmission != null || p12DecisionIdMutationCommitted != null)
            throw new InvalidOperationException("RuntimeIdAllocator Decision IDs are already bound to a P12 mutation boundary.");
        p12DecisionIdMutationAdmission = admission;
        p12DecisionIdMutationCommitted = committed;
    }

    private bool CanCommitP12DecisionIdMutation()
    {
        try { return p12DecisionIdMutationAdmission(); }
        catch { return false; }
    }

    public string AllocateTravelPartyId()
    {
        if (nextTravelPartySequence == long.MaxValue)
        {
            throw new InvalidOperationException("RuntimeId sequence exhausted for type 'travel-party'.");
        }

        if (p12TravelPartyIdMutationAdmission != null && !CanCommitP12TravelPartyIdMutation())
        {
            throw new InvalidOperationException("The P12 TravelParty-ID mutation was rejected before allocation.");
        }

        string runtimeId = "travel-party-" + nextTravelPartySequence.ToString("D6", CultureInfo.InvariantCulture);
        nextTravelPartySequence++;
        p12TravelPartyIdMutationCommitted?.Invoke();
        return runtimeId;
    }

    internal void BindP12TravelPartyIdMutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12TravelPartyIdMutationAdmission != null || p12TravelPartyIdMutationCommitted != null)
            throw new InvalidOperationException("RuntimeIdAllocator TravelParty IDs are already bound to a P12 mutation boundary.");
        p12TravelPartyIdMutationAdmission = admission;
        p12TravelPartyIdMutationCommitted = committed;
    }

    private bool CanCommitP12TravelPartyIdMutation()
    {
        try { return p12TravelPartyIdMutationAdmission(); }
        catch { return false; }
    }

    public string AllocateOrganizationId()
    {
        return Allocate("organization", ref nextOrganizationSequence);
    }

    public string AllocateExplorableSiteId()
    {
        return Allocate("site", ref nextExplorableSiteSequence);
    }

    public string AllocateExpeditionId()
    {
        return Allocate("expedition", ref nextExpeditionSequence);
    }

    public string AllocateLocalPlaceId()
    {
        return Allocate("local-place", ref nextLocalPlaceSequence);
    }

    public string AllocateLocalConnectionId()
    {
        return Allocate("local-connection", ref nextLocalConnectionSequence);
    }

    public string AllocateNotableItemId()
    {
        return Allocate("notable-item", ref nextNotableItemSequence);
    }

    private static string Allocate(string prefix, ref long nextSequence)
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException($"RuntimeId sequence exhausted for type '{prefix}'.");
        }

        string runtimeId = prefix + "-" + nextSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextSequence++;
        return runtimeId;
    }
}
