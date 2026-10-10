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

public sealed class RuntimeIdentityRegistry
{
    private readonly Dictionary<string, NpcRuntime> npcsByRuntimeId = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, CityRuntime> citiesByRuntimeId = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialLocationRuntime> locationsByRuntimeId = new Dictionary<string, SpatialLocationRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialRouteRuntime> routesByRuntimeId = new Dictionary<string, SpatialRouteRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, ExplorableSiteRuntime> explorableSitesByRuntimeId = new Dictionary<string, ExplorableSiteRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalPlaceRuntime> localPlacesByRuntimeId = new Dictionary<string, LocalPlaceRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalTopologyConnectionRuntime> localConnectionsByRuntimeId = new Dictionary<string, LocalTopologyConnectionRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, NotableItemRuntime> notableItemsByRuntimeId = new Dictionary<string, NotableItemRuntime>(StringComparer.Ordinal);
    private readonly SimulationLogger logger;
    private long censusRevision;

    internal long CensusRevision => censusRevision;

    public RuntimeIdentityRegistry(SimulationLogger logger = null)
    {
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool RegisterNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            logger.LogError("Cannot register NPC runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId) == true)
        {
            logger.LogError("Cannot register NPC runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(npcRuntime.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{npcRuntime.RuntimeId}' while registering NPC; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("NPC") == false)
        {
            return false;
        }

        npcsByRuntimeId.Add(npcRuntime.RuntimeId, npcRuntime);
        AdvanceCensusRevision();
        return true;
    }

    internal bool TryGetNpcForMembership(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (!string.IsNullOrWhiteSpace(runtimeId)
            && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime))
            return true;

        npcRuntime = null;
        return false;
    }

    internal bool CanRegisterNpcForMembership(NpcRuntime npcRuntime, out bool duplicateIdentity)
    {
        duplicateIdentity = false;
        if (npcRuntime == null || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId))
            return false;
        if (TryGetRegisteredType(npcRuntime.RuntimeId, out _))
        {
            duplicateIdentity = true;
            return false;
        }
        return censusRevision < long.MaxValue;
    }

    public bool RegisterCity(CityRuntime cityRuntime)
    {
        if (cityRuntime == null)
        {
            logger.LogError("Cannot register City runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(cityRuntime.RuntimeId) == true)
        {
            logger.LogError("Cannot register City runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(cityRuntime.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{cityRuntime.RuntimeId}' while registering City; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("City") == false)
        {
            return false;
        }

        citiesByRuntimeId.Add(cityRuntime.RuntimeId, cityRuntime);
        AdvanceCensusRevision();
        return true;
    }

    public bool RegisterLocation(SpatialLocationRuntime location)
    {
        return RegisterLocationCore(location, null);
    }

    internal bool RegisterLocationForP10Genesis(SpatialLocationRuntime location, Action<string> completedMutation)
    {
        return RegisterLocationCore(location, completedMutation);
    }

    private bool RegisterLocationCore(SpatialLocationRuntime location, Action<string> completedMutation)
    {
        if (location == null)
        {
            logger.LogError("Cannot register Location runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(location.RuntimeId) == true)
        {
            logger.LogError("Cannot register Location runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(location.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{location.RuntimeId}' while registering Location; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("Location") == false)
        {
            return false;
        }

        bool indexAdded = false;
        bool revisionAdvanced = false;
        try
        {
            locationsByRuntimeId.Add(location.RuntimeId, location);
            indexAdded = true;
            completedMutation?.Invoke("RuntimeIdentity.Location.Index");
            AdvanceCensusRevision();
            revisionAdvanced = true;
            completedMutation?.Invoke("RuntimeIdentity.Location.Revision");
            return true;
        }
        catch
        {
            if (revisionAdvanced) censusRevision--;
            if (indexAdded) locationsByRuntimeId.Remove(location.RuntimeId);
            throw;
        }
    }

    public bool RegisterRoute(SpatialRouteRuntime route)
    {
        if (route == null)
        {
            logger.LogError("Cannot register Route runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(route.RuntimeId) == true)
        {
            logger.LogError("Cannot register Route runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(route.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{route.RuntimeId}' while registering Route; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("Route") == false)
        {
            return false;
        }

        routesByRuntimeId.Add(route.RuntimeId, route);
        AdvanceCensusRevision();
        return true;
    }

    public bool RegisterExplorableSite(ExplorableSiteRuntime site)
    {
        return RegisterExplorableSiteCore(site, null);
    }

    internal bool RegisterExplorableSiteForP10Genesis(ExplorableSiteRuntime site, Action<string> completedMutation)
    {
        return RegisterExplorableSiteCore(site, completedMutation);
    }

    private bool RegisterExplorableSiteCore(ExplorableSiteRuntime site, Action<string> completedMutation)
    {
        if (site == null)
        {
            logger.LogError("Cannot register ExplorableSite runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(site.RuntimeId) == true)
        {
            logger.LogError("Cannot register ExplorableSite runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(site.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{site.RuntimeId}' while registering ExplorableSite; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("ExplorableSite") == false)
        {
            return false;
        }

        bool indexAdded = false;
        bool revisionAdvanced = false;
        try
        {
            explorableSitesByRuntimeId.Add(site.RuntimeId, site);
            indexAdded = true;
            completedMutation?.Invoke("RuntimeIdentity.ExplorableSite.Index");
            AdvanceCensusRevision();
            revisionAdvanced = true;
            completedMutation?.Invoke("RuntimeIdentity.ExplorableSite.Revision");
            return true;
        }
        catch
        {
            if (revisionAdvanced) censusRevision--;
            if (indexAdded) explorableSitesByRuntimeId.Remove(site.RuntimeId);
            throw;
        }
    }

    public bool RegisterLocalPlace(LocalPlaceRuntime localPlace)
    {
        if (localPlace == null)
        {
            logger.LogError("Cannot register LocalPlace runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(localPlace.RuntimeId) == true)
        {
            logger.LogError("Cannot register LocalPlace runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(localPlace.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{localPlace.RuntimeId}' while registering LocalPlace; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("LocalPlace") == false)
        {
            return false;
        }

        localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
        AdvanceCensusRevision();
        return true;
    }

    public bool RegisterLocalConnection(LocalTopologyConnectionRuntime localConnection)
    {
        if (localConnection == null)
        {
            logger.LogError("Cannot register LocalConnection runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(localConnection.RuntimeId) == true)
        {
            logger.LogError("Cannot register LocalConnection runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(localConnection.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{localConnection.RuntimeId}' while registering LocalConnection; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("LocalConnection") == false)
        {
            return false;
        }

        localConnectionsByRuntimeId.Add(localConnection.RuntimeId, localConnection);
        AdvanceCensusRevision();
        return true;
    }

    public bool RegisterNotableItem(NotableItemRuntime notableItem)
    {
        if (notableItem == null)
        {
            logger.LogError("Cannot register NotableItem runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(notableItem.RuntimeId) == true)
        {
            logger.LogError("Cannot register NotableItem runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(notableItem.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{notableItem.RuntimeId}' while registering NotableItem; it is already registered as {registeredType}.");
            return false;
        }

        if (CanAdvanceCensusRevision("NotableItem") == false)
        {
            return false;
        }

        notableItemsByRuntimeId.Add(notableItem.RuntimeId, notableItem);
        AdvanceCensusRevision();
        return true;
    }

    internal bool TryRegisterLocalTopologyMembers(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        out string diagnostic)
    {
        return TryRegisterLocalTopologyMembersCore(localPlaces, localConnections, null, out diagnostic);
    }

    internal bool TryRegisterLocalTopologyMembersForP10Genesis(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        Action<string> completedMutation,
        out string diagnostic)
    {
        return TryRegisterLocalTopologyMembersCore(localPlaces, localConnections, completedMutation, out diagnostic);
    }

    private bool TryRegisterLocalTopologyMembersCore(
        IReadOnlyList<LocalPlaceRuntime> localPlaces,
        IReadOnlyList<LocalTopologyConnectionRuntime> localConnections,
        Action<string> completedMutation,
        out string diagnostic)
    {
        diagnostic = null;

        if (localPlaces == null || localConnections == null)
        {
            diagnostic = "Local topology member collections cannot be null.";
            return false;
        }

        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (LocalPlaceRuntime localPlace in localPlaces)
        {
            if (localPlace == null)
            {
                diagnostic = "Local topology contains a null LocalPlace.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(localPlace.RuntimeId) == true)
            {
                diagnostic = "Local topology contains a LocalPlace with an empty RuntimeId.";
                return false;
            }

            if (runtimeIds.Add(localPlace.RuntimeId) == false)
            {
                diagnostic = $"Local topology contains duplicate RuntimeId '{localPlace.RuntimeId}'.";
                return false;
            }

            if (IsRuntimeIdAvailable(localPlace.RuntimeId) == false)
            {
                diagnostic = $"LocalPlace RuntimeId '{localPlace.RuntimeId}' is already registered.";
                return false;
            }
        }

        foreach (LocalTopologyConnectionRuntime localConnection in localConnections)
        {
            if (localConnection == null)
            {
                diagnostic = "Local topology contains a null LocalConnection.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(localConnection.RuntimeId) == true)
            {
                diagnostic = "Local topology contains a LocalConnection with an empty RuntimeId.";
                return false;
            }

            if (runtimeIds.Add(localConnection.RuntimeId) == false)
            {
                diagnostic = $"Local topology contains duplicate RuntimeId '{localConnection.RuntimeId}'.";
                return false;
            }

            if (IsRuntimeIdAvailable(localConnection.RuntimeId) == false)
            {
                diagnostic = $"LocalConnection RuntimeId '{localConnection.RuntimeId}' is already registered.";
                return false;
            }
        }

        if (runtimeIds.Count > 0 && CanAdvanceCensusRevision("local topology batch") == false)
        {
            diagnostic = "Runtime identity census revision is exhausted.";
            return false;
        }

        int addedPlaces = 0;
        int addedConnections = 0;
        bool revisionAdvanced = false;
        try
        {
            for (int i = 0; i < localPlaces.Count; i++)
            {
                LocalPlaceRuntime localPlace = localPlaces[i];
                localPlacesByRuntimeId.Add(localPlace.RuntimeId, localPlace);
                addedPlaces++;
                completedMutation?.Invoke("RuntimeIdentity.LocalPlace.Index." + i.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < localConnections.Count; i++)
            {
                LocalTopologyConnectionRuntime localConnection = localConnections[i];
                localConnectionsByRuntimeId.Add(localConnection.RuntimeId, localConnection);
                addedConnections++;
                completedMutation?.Invoke("RuntimeIdentity.LocalConnection.Index." + i.ToString(CultureInfo.InvariantCulture));
            }

            if (runtimeIds.Count > 0)
            {
                AdvanceCensusRevision();
                revisionAdvanced = true;
                completedMutation?.Invoke("RuntimeIdentity.LocalTopology.Revision");
            }

            return true;
        }
        catch
        {
            if (revisionAdvanced) censusRevision--;
            for (int i = addedConnections - 1; i >= 0; i--)
                localConnectionsByRuntimeId.Remove(localConnections[i].RuntimeId);
            for (int i = addedPlaces - 1; i >= 0; i--)
                localPlacesByRuntimeId.Remove(localPlaces[i].RuntimeId);
            throw;
        }
    }

    internal int GetCensusCardinality(RuntimeIdentityCensusIndex index)
    {
        switch (index)
        {
            case RuntimeIdentityCensusIndex.Npcs: return npcsByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.Cities: return citiesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.Locations: return locationsByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.Routes: return routesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.ExplorableSites: return explorableSitesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.LocalPlaces: return localPlacesByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.LocalConnections: return localConnectionsByRuntimeId.Count;
            case RuntimeIdentityCensusIndex.NotableItems: return notableItemsByRuntimeId.Count;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal IReadOnlyList<KeyValuePair<string, string>> CaptureRegisteredRuntimeIdentityValues()
    {
        List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
        AddIdentityValues(values, "npc", npcsByRuntimeId);
        AddIdentityValues(values, "city", citiesByRuntimeId);
        AddIdentityValues(values, "location", locationsByRuntimeId);
        AddIdentityValues(values, "route", routesByRuntimeId);
        AddIdentityValues(values, "site", explorableSitesByRuntimeId);
        AddIdentityValues(values, "local-place", localPlacesByRuntimeId);
        AddIdentityValues(values, "local-connection", localConnectionsByRuntimeId);
        AddIdentityValues(values, "notable-item", notableItemsByRuntimeId);
        values.Sort((left, right) =>
        {
            int familyOrder = string.Compare(left.Key, right.Key, StringComparison.Ordinal);
            return familyOrder != 0
                ? familyOrder
                : string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        });
        return values.AsReadOnly();
    }

    private static void AddIdentityValues<TValue>(
        List<KeyValuePair<string, string>> values,
        string familyId,
        Dictionary<string, TValue> identities)
    {
        foreach (KeyValuePair<string, TValue> identity in identities)
            values.Add(new KeyValuePair<string, string>(familyId, identity.Key));
    }

    private bool CanAdvanceCensusRevision(string identityKind)
    {
        if (censusRevision < long.MaxValue)
        {
            return true;
        }

        logger.LogError($"Cannot register {identityKind} runtime identity: census revision is exhausted.");
        return false;
    }

    private void AdvanceCensusRevision()
    {
        censusRevision++;
    }

    internal void RollbackGenesisExplorableSite(ExplorableSiteRuntime site)
    {
        if (site == null || !explorableSitesByRuntimeId.TryGetValue(site.RuntimeId, out ExplorableSiteRuntime current)
            || !ReferenceEquals(site, current) || censusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B ExplorableSite identity insertion.");
        explorableSitesByRuntimeId.Remove(site.RuntimeId);
        censusRevision--;
    }

    internal void RollbackGenesisLocation(SpatialLocationRuntime location)
    {
        if (location == null || !locationsByRuntimeId.TryGetValue(location.RuntimeId, out SpatialLocationRuntime current)
            || !ReferenceEquals(location, current) || censusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B Location identity insertion.");
        locationsByRuntimeId.Remove(location.RuntimeId);
        censusRevision--;
    }

    internal void RollbackGenesisLocalTopologyMembers(
        IReadOnlyList<LocalPlaceRuntime> places, IReadOnlyList<LocalTopologyConnectionRuntime> connections)
    {
        if (places == null || connections == null || censusRevision <= 0)
            throw new InvalidOperationException("Cannot roll back the P10-B LocalTopology identity batch.");
        foreach (LocalPlaceRuntime place in places)
            if (place == null || !localPlacesByRuntimeId.TryGetValue(place.RuntimeId, out LocalPlaceRuntime currentPlace)
                || !ReferenceEquals(place, currentPlace))
                throw new InvalidOperationException("P10-B LocalPlace identity rollback did not match the inserted object.");
        foreach (LocalTopologyConnectionRuntime connection in connections)
            if (connection == null || !localConnectionsByRuntimeId.TryGetValue(connection.RuntimeId, out LocalTopologyConnectionRuntime currentConnection)
                || !ReferenceEquals(connection, currentConnection))
                throw new InvalidOperationException("P10-B LocalConnection identity rollback did not match the inserted object.");
        foreach (LocalPlaceRuntime place in places) localPlacesByRuntimeId.Remove(place.RuntimeId);
        foreach (LocalTopologyConnectionRuntime connection in connections) localConnectionsByRuntimeId.Remove(connection.RuntimeId);
        censusRevision--;
    }

    public bool TryGetNpc(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime) == true)
        {
            return true;
        }

        npcRuntime = null;

        LogResolutionFailure("NPC", runtimeId);
        return false;
    }

    public bool TryGetCity(string runtimeId, out CityRuntime cityRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && citiesByRuntimeId.TryGetValue(runtimeId, out cityRuntime) == true)
        {
            return true;
        }

        cityRuntime = null;

        LogResolutionFailure("City", runtimeId);
        return false;
    }

    public bool TryGetLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && locationsByRuntimeId.TryGetValue(runtimeId, out location) == true)
        {
            return true;
        }

        location = null;
        LogResolutionFailure("Location", runtimeId);
        return false;
    }

    public bool TryGetRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && routesByRuntimeId.TryGetValue(runtimeId, out route) == true)
        {
            return true;
        }

        route = null;
        LogResolutionFailure("Route", runtimeId);
        return false;
    }

    public bool TryFindRouteBetweenLocations(
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        out SpatialRouteRuntime route)
    {
        route = null;
        if (string.IsNullOrWhiteSpace(originLocationRuntimeId) == true
            || string.IsNullOrWhiteSpace(destinationLocationRuntimeId) == true)
        {
            return false;
        }

        foreach (SpatialRouteRuntime candidate in routesByRuntimeId.Values)
        {
            if (candidate != null
                && candidate.Origin != null
                && candidate.Destination != null
                && string.Equals(candidate.Origin.RuntimeId, originLocationRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(candidate.Destination.RuntimeId, destinationLocationRuntimeId, StringComparison.Ordinal) == true)
            {
                if (route != null)
                {
                    route = null;
                    return false;
                }

                route = candidate;
            }
        }

        return route != null;
    }

    public bool TryGetExplorableSite(string runtimeId, out ExplorableSiteRuntime site)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && explorableSitesByRuntimeId.TryGetValue(runtimeId, out site) == true)
        {
            return true;
        }

        site = null;

        LogResolutionFailure("ExplorableSite", runtimeId);
        return false;
    }

    public bool TryGetLocalPlace(string runtimeId, out LocalPlaceRuntime localPlace)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && localPlacesByRuntimeId.TryGetValue(runtimeId, out localPlace) == true)
        {
            return true;
        }

        localPlace = null;

        LogResolutionFailure("LocalPlace", runtimeId);
        return false;
    }

    public bool TryGetLocalConnection(string runtimeId, out LocalTopologyConnectionRuntime localConnection)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && localConnectionsByRuntimeId.TryGetValue(runtimeId, out localConnection) == true)
        {
            return true;
        }

        localConnection = null;

        LogResolutionFailure("LocalConnection", runtimeId);
        return false;
    }

    public bool TryGetNotableItem(string runtimeId, out NotableItemRuntime notableItem)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && notableItemsByRuntimeId.TryGetValue(runtimeId, out notableItem) == true)
        {
            return true;
        }

        notableItem = null;
        LogResolutionFailure("NotableItem", runtimeId);
        return false;
    }

    public bool IsRuntimeIdAvailable(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && TryGetRegisteredType(runtimeId, out _) == false;
    }

    internal bool TryGetCityWithoutLogging(string runtimeId, out CityRuntime cityRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && citiesByRuntimeId.TryGetValue(runtimeId, out cityRuntime) == true)
        {
            return true;
        }

        cityRuntime = null;
        return false;
    }

    internal bool TryGetNpcWithoutLogging(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime) == true)
        {
            return true;
        }

        npcRuntime = null;
        return false;
    }

    internal bool TryGetExplorableSiteWithoutLogging(string runtimeId, out ExplorableSiteRuntime site)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && explorableSitesByRuntimeId.TryGetValue(runtimeId, out site) == true)
        {
            return true;
        }

        site = null;
        return false;
    }

    internal bool TryGetNotableItemWithoutLogging(string runtimeId, out NotableItemRuntime notableItem)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && notableItemsByRuntimeId.TryGetValue(runtimeId, out notableItem) == true)
        {
            return true;
        }

        notableItem = null;
        return false;
    }

    private bool TryGetRegisteredType(string runtimeId, out string registeredType)
    {
        if (npcsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "NPC";
            return true;
        }

        if (citiesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "City";
            return true;
        }

        if (locationsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "Location";
            return true;
        }

        if (routesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "Route";
            return true;
        }

        if (explorableSitesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "ExplorableSite";
            return true;
        }

        if (localPlacesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "LocalPlace";
            return true;
        }

        if (localConnectionsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "LocalConnection";
            return true;
        }

        if (notableItemsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "NotableItem";
            return true;
        }

        registeredType = null;
        return false;
    }

    private void LogResolutionFailure(string requestedType, string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && TryGetRegisteredType(runtimeId, out string registeredType) == true)
        {
            logger.LogWarning($"{requestedType} runtime resolution failed: RuntimeId '{runtimeId}' is registered as {registeredType}, not {requestedType}.");
            return;
        }

        logger.LogWarning($"{requestedType} runtime resolution failed: RuntimeId '{FormatRuntimeId(runtimeId)}' is not registered.");
    }

    private static string FormatRuntimeId(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == true ? "<empty>" : runtimeId;
    }
}
