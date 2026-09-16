using System;
using System.Collections.Generic;

public enum PlaceContentOwnerKind
{
    City,
    ExplorableSite,
    LocalPlace
}

public enum NotableItemCustodyKind
{
    Place,
    Npc
}

public enum PlaceContentPersistencePolicy
{
    Transient,
    Perishable,
    Durable,
    Notable
}

public enum PlaceSiteState
{
    Threatened,
    Cleared,
    Secured
}

public enum PlaceAccessState
{
    Inaccessible,
    Contested,
    Accessible
}

[Serializable]
public sealed class PlaceContentOwnerReference
{
    private readonly string ownerRuntimeId;
    private readonly PlaceContentOwnerKind ownerKind;
    private readonly string macroLocationRuntimeId;
    private readonly string topologyOwnerRuntimeId;

    public string OwnerRuntimeId => ownerRuntimeId;
    public PlaceContentOwnerKind OwnerKind => ownerKind;
    public string MacroLocationRuntimeId => macroLocationRuntimeId;
    public string TopologyOwnerRuntimeId => topologyOwnerRuntimeId;
    public string StableKey => ownerKind + ":" + ownerRuntimeId;

    private PlaceContentOwnerReference(
        string ownerRuntimeId,
        PlaceContentOwnerKind ownerKind,
        string macroLocationRuntimeId,
        string topologyOwnerRuntimeId = null)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Place content owners require a non-empty RuntimeId.", nameof(ownerRuntimeId));
        }

        if (Enum.IsDefined(typeof(PlaceContentOwnerKind), ownerKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerKind));
        }

        if (string.IsNullOrWhiteSpace(macroLocationRuntimeId) == true)
        {
            throw new ArgumentException("Place content owners require a macro location RuntimeId.", nameof(macroLocationRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
        this.ownerKind = ownerKind;
        this.macroLocationRuntimeId = macroLocationRuntimeId;
        this.topologyOwnerRuntimeId = topologyOwnerRuntimeId;
    }

    public static PlaceContentOwnerReference ForCity(CityRuntime cityRuntime)
    {
        if (cityRuntime == null || cityRuntime.Location == null)
        {
            throw new ArgumentNullException(nameof(cityRuntime));
        }

        return new PlaceContentOwnerReference(
            cityRuntime.RuntimeId,
            PlaceContentOwnerKind.City,
            cityRuntime.Location.RuntimeId);
    }

    public static PlaceContentOwnerReference ForExplorableSite(ExplorableSiteRuntime siteRuntime)
    {
        if (siteRuntime == null || siteRuntime.Location == null)
        {
            throw new ArgumentNullException(nameof(siteRuntime));
        }

        return new PlaceContentOwnerReference(
            siteRuntime.RuntimeId,
            PlaceContentOwnerKind.ExplorableSite,
            siteRuntime.Location.RuntimeId);
    }

    public static PlaceContentOwnerReference ForLocalPlace(
        LocalPlaceRuntime localPlace,
        string macroLocationRuntimeId = null)
    {
        if (localPlace == null)
        {
            throw new ArgumentNullException(nameof(localPlace));
        }

        string resolvedMacroLocationRuntimeId = macroLocationRuntimeId;
        string topologyOwnerRuntimeId = null;
        if (localPlace.OwningTopology != null)
        {
            topologyOwnerRuntimeId = localPlace.OwningTopology.Owner.OwnerRuntimeId;
            if (string.IsNullOrWhiteSpace(resolvedMacroLocationRuntimeId) == true)
            {
                resolvedMacroLocationRuntimeId = localPlace.OwningTopology.Owner.MacroLocationRuntimeId;
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedMacroLocationRuntimeId) == true)
        {
            throw new InvalidOperationException(
                "A LocalPlace content owner requires an owning topology or an explicit macro location RuntimeId.");
        }

        return new PlaceContentOwnerReference(
            localPlace.RuntimeId,
            PlaceContentOwnerKind.LocalPlace,
            resolvedMacroLocationRuntimeId,
            topologyOwnerRuntimeId);
    }
}

[Serializable]
public sealed class PlaceContentStackRuntime
{
    private readonly ItemData item;
    private readonly PlaceContentPersistencePolicy persistencePolicy;
    private readonly int decayPerDay;
    private readonly InventoryRuntime inventory = new InventoryRuntime();

    public ItemData Item => item;
    public string ItemDefinitionId => item != null ? item.DefinitionId : string.Empty;
    public PlaceContentPersistencePolicy PersistencePolicy => persistencePolicy;
    public int DecayPerDay => decayPerDay;
    public InventoryRuntime Inventory => inventory;
    public int Amount => inventory.GetAmount(item);
    public float AverageUnitCost => inventory.GetAverageUnitCost(item);

    public PlaceContentStackRuntime(
        ItemData item,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        int decayPerDay = 0,
        float averageUnitCost = 0f)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (Enum.IsDefined(typeof(PlaceContentPersistencePolicy), persistencePolicy) == false
            || persistencePolicy == PlaceContentPersistencePolicy.Notable)
        {
            throw new ArgumentException("Stacked content must use Transient, Perishable, or Durable persistence.", nameof(persistencePolicy));
        }

        if (decayPerDay < 0 || (persistencePolicy == PlaceContentPersistencePolicy.Perishable && decayPerDay == 0))
        {
            throw new ArgumentOutOfRangeException(nameof(decayPerDay));
        }

        this.item = item;
        this.persistencePolicy = persistencePolicy;
        this.decayPerDay = decayPerDay;
        if (inventory.CanAddItem(item, amount, averageUnitCost) == false)
        {
            throw new ArgumentException("Stacked content could not be added to its InventoryRuntime.", nameof(amount));
        }

        inventory.AddItem(item, amount, averageUnitCost);
    }

    public bool CanAdd(int amount, float averageUnitCost)
    {
        return amount > 0 && inventory.CanAddItem(item, amount, averageUnitCost);
    }

    public bool TryAdd(int amount, float averageUnitCost = 0f)
    {
        if (CanAdd(amount, averageUnitCost) == false)
        {
            return false;
        }

        inventory.AddItem(item, amount, averageUnitCost);
        return true;
    }

    public bool TryRemove(int amount)
    {
        return inventory.RemoveItem(item, amount);
    }

    internal void AdvanceDays(int dayCount)
    {
        if (dayCount <= 0 || Amount <= 0)
        {
            return;
        }

        int amountToRemove = persistencePolicy == PlaceContentPersistencePolicy.Transient
            ? Amount
            : CalculateDecay(dayCount);
        if (amountToRemove > 0)
        {
            inventory.RemoveItem(item, Math.Min(Amount, amountToRemove));
        }
    }

    private int CalculateDecay(int dayCount)
    {
        if (persistencePolicy != PlaceContentPersistencePolicy.Perishable || decayPerDay <= 0)
        {
            return 0;
        }

        long decay = (long)decayPerDay * dayCount;
        return decay >= int.MaxValue ? int.MaxValue : (int)decay;
    }
}

[Serializable]
public sealed class NotableItemCustodyReference
{
    private readonly NotableItemCustodyKind custodyKind;
    private readonly PlaceContentOwnerReference placeOwner;
    private readonly string npcRuntimeId;

    public NotableItemCustodyKind CustodyKind => custodyKind;
    public PlaceContentOwnerReference PlaceOwner => placeOwner;
    public string NpcRuntimeId => npcRuntimeId;
    public string StableKey => custodyKind == NotableItemCustodyKind.Place
        ? "Place:" + placeOwner.StableKey
        : "Npc:" + npcRuntimeId;

    private NotableItemCustodyReference(
        NotableItemCustodyKind custodyKind,
        PlaceContentOwnerReference placeOwner,
        string npcRuntimeId)
    {
        custodyKind = ValidateKind(custodyKind);
        if (custodyKind == NotableItemCustodyKind.Place && placeOwner == null)
        {
            throw new ArgumentNullException(nameof(placeOwner));
        }

        if (custodyKind == NotableItemCustodyKind.Npc && string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            throw new ArgumentException("NPC custody requires a RuntimeId.", nameof(npcRuntimeId));
        }

        this.custodyKind = custodyKind;
        this.placeOwner = placeOwner;
        this.npcRuntimeId = npcRuntimeId;
    }

    public static NotableItemCustodyReference ForPlace(PlaceContentOwnerReference owner)
    {
        return new NotableItemCustodyReference(NotableItemCustodyKind.Place, owner, null);
    }

    public static NotableItemCustodyReference ForNpc(NpcRuntime npc)
    {
        if (npc == null)
        {
            throw new ArgumentNullException(nameof(npc));
        }

        return ForNpcRuntimeId(npc.RuntimeId);
    }

    public static NotableItemCustodyReference ForNpcRuntimeId(string npcRuntimeId)
    {
        return new NotableItemCustodyReference(NotableItemCustodyKind.Npc, null, npcRuntimeId);
    }

    public bool Matches(NotableItemCustodyReference other)
    {
        return other != null && string.Equals(StableKey, other.StableKey, StringComparison.Ordinal);
    }

    private static NotableItemCustodyKind ValidateKind(NotableItemCustodyKind custodyKind)
    {
        if (Enum.IsDefined(typeof(NotableItemCustodyKind), custodyKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(custodyKind));
        }

        return custodyKind;
    }
}

[Serializable]
public sealed class NotableItemRuntime
{
    private readonly string runtimeId;
    private readonly ItemData definition;
    private NotableItemCustodyReference custody;

    public string RuntimeId => runtimeId;
    public ItemData Definition => definition;
    public ItemData Item => definition;
    public string DefinitionId => definition != null ? definition.DefinitionId : string.Empty;
    public NotableItemCustodyReference Custody => custody;
    public PlaceContentOwnerReference Owner => custody?.PlaceOwner;
    public string CustodianNpcRuntimeId => custody?.NpcRuntimeId;
    public bool IsPresent => custody != null;
    public bool IsAtPlace => custody?.CustodyKind == NotableItemCustodyKind.Place;
    public bool IsHeldByNpc => custody?.CustodyKind == NotableItemCustodyKind.Npc;
    public PlaceContentPersistencePolicy PersistencePolicy => PlaceContentPersistencePolicy.Notable;

    public NotableItemRuntime(string runtimeId, ItemData definition)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Notable items require a stable RuntimeId.", nameof(runtimeId));
        }

        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        this.runtimeId = runtimeId;
        this.definition = definition;
    }

    internal bool TryAssignCustody(NotableItemCustodyReference nextCustody)
    {
        if (nextCustody == null || custody != null)
        {
            return false;
        }

        custody = nextCustody;
        return true;
    }

    internal bool TryTransferCustody(
        NotableItemCustodyReference expectedCustody,
        NotableItemCustodyReference nextCustody)
    {
        if (expectedCustody == null || nextCustody == null
            || custody?.Matches(expectedCustody) != true
            || expectedCustody.Matches(nextCustody) == true)
        {
            return false;
        }

        custody = nextCustody;
        return true;
    }
}

[Serializable]
public sealed class PlaceOppositionRuntime
{
    private readonly string runtimeId;
    private readonly string displayName;
    private readonly string oppositionSideId;
    private readonly List<NpcRuntime> namedParticipants = new List<NpcRuntime>();
    private readonly List<AggregateParticipantSnapshot> aggregateParticipants = new List<AggregateParticipantSnapshot>();
    private readonly IReadOnlyList<NpcRuntime> readOnlyNamedParticipants;
    private readonly IReadOnlyList<AggregateParticipantSnapshot> readOnlyAggregateParticipants;
    private bool resolved;
    private ConflictResolutionResult resolution;

    public string RuntimeId => runtimeId;
    public string DisplayName => displayName;
    public string OppositionSideId => oppositionSideId;
    public bool IsResolved => resolved;
    public bool IsActive => resolved == false;
    public ConflictResolutionResult Resolution => resolution;
    public IReadOnlyList<NpcRuntime> NamedParticipants => readOnlyNamedParticipants;
    public IReadOnlyList<NpcRuntime> NpcParticipants => readOnlyNamedParticipants;
    public IReadOnlyList<AggregateParticipantSnapshot> AggregateParticipants => readOnlyAggregateParticipants;

    public PlaceOppositionRuntime(
        string runtimeId,
        string displayName = null,
        string oppositionSideId = "opposition")
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Place opposition requires a stable RuntimeId.", nameof(runtimeId));
        }

        if (string.IsNullOrWhiteSpace(oppositionSideId) == true)
        {
            throw new ArgumentException("Place opposition requires a stable conflict side ID.", nameof(oppositionSideId));
        }

        this.runtimeId = runtimeId;
        this.displayName = string.IsNullOrWhiteSpace(displayName) == true ? runtimeId : displayName;
        this.oppositionSideId = oppositionSideId;
        readOnlyNamedParticipants = namedParticipants.AsReadOnly();
        readOnlyAggregateParticipants = aggregateParticipants.AsReadOnly();
    }

    public bool TryAddNamedParticipant(NpcRuntime npcRuntime, out string diagnostic)
    {
        if (npcRuntime == null)
        {
            diagnostic = "Named opposition participant is null.";
            return false;
        }

        foreach (NpcRuntime existing in namedParticipants)
        {
            if (existing != null && existing.RuntimeId == npcRuntime.RuntimeId)
            {
                diagnostic = "Named opposition participant IDs must be unique.";
                return false;
            }
        }

        namedParticipants.Add(npcRuntime);
        diagnostic = null;
        return true;
    }

    public void AddNamedParticipant(NpcRuntime npcRuntime)
    {
        if (TryAddNamedParticipant(npcRuntime, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    public bool TryAddAggregateParticipant(AggregateParticipantSnapshot aggregate, out string diagnostic)
    {
        if (aggregate == null)
        {
            diagnostic = "Aggregate opposition participant is null.";
            return false;
        }

        foreach (AggregateParticipantSnapshot existing in aggregateParticipants)
        {
            if (existing != null && existing.SourceId == aggregate.SourceId)
            {
                diagnostic = "Aggregate opposition SourceIds must be unique.";
                return false;
            }
        }

        aggregateParticipants.Add(aggregate);
        diagnostic = null;
        return true;
    }

    public void AddAggregateParticipant(AggregateParticipantSnapshot aggregate)
    {
        if (TryAddAggregateParticipant(aggregate, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    public Conflict CreateConflict(
        string conflictId,
        IEnumerable<NpcRuntime> opposingSideParticipants = null,
        string attackingSideId = "expedition",
        ConflictObjectiveType objective = ConflictObjectiveType.Defeat,
        ConflictStakes stakes = ConflictStakes.Meaningful)
    {
        Conflict conflict = new Conflict(conflictId);
        ConflictSide attackingSide = conflict.AddSide(attackingSideId, objective, stakes);
        ConflictSide oppositionSide = conflict.AddSide(oppositionSideId, ConflictObjectiveType.Defend, stakes);

        if (opposingSideParticipants != null)
        {
            foreach (NpcRuntime participant in opposingSideParticipants)
            {
                if (participant != null)
                {
                    attackingSide.AddNpc(participant);
                }
            }
        }

        foreach (NpcRuntime participant in namedParticipants)
        {
            oppositionSide.AddNpc(participant);
        }

        foreach (AggregateParticipantSnapshot aggregate in aggregateParticipants)
        {
            oppositionSide.AddAggregate(aggregate);
        }

        return conflict;
    }

    internal void MarkResolved(ConflictResolutionResult nextResolution)
    {
        resolved = true;
        resolution = nextResolution;
    }
}

[Serializable]
public sealed class PlaceContentRuntime
{
    private readonly PlaceContentOwnerReference owner;
    private readonly List<PlaceContentStackRuntime> stackedContent = new List<PlaceContentStackRuntime>();
    private readonly List<NotableItemRuntime> notableContent = new List<NotableItemRuntime>();
    private readonly List<PlaceOppositionRuntime> oppositions = new List<PlaceOppositionRuntime>();
    private readonly IReadOnlyList<PlaceContentStackRuntime> readOnlyStackedContent;
    private readonly IReadOnlyList<NotableItemRuntime> readOnlyNotableContent;
    private readonly IReadOnlyList<PlaceOppositionRuntime> readOnlyOppositions;
    private PlaceSiteState siteState;
    private PlaceAccessState accessState;
    private string controllerRuntimeId;

    public PlaceContentOwnerReference Owner => owner;
    public PlaceSiteState SiteState => siteState;
    public PlaceAccessState AccessState => accessState;
    public string ControllerRuntimeId => controllerRuntimeId;
    public bool IsAccessible => accessState == PlaceAccessState.Accessible;
    public IReadOnlyList<PlaceContentStackRuntime> StackedContent => readOnlyStackedContent;
    public IReadOnlyList<PlaceContentStackRuntime> CommonStacks => readOnlyStackedContent;
    public IReadOnlyList<NotableItemRuntime> NotableContent => readOnlyNotableContent;
    public IReadOnlyList<PlaceOppositionRuntime> Oppositions => readOnlyOppositions;
    public IReadOnlyList<PlaceOppositionRuntime> ActiveOppositions
    {
        get
        {
            List<PlaceOppositionRuntime> active = new List<PlaceOppositionRuntime>();
            foreach (PlaceOppositionRuntime opposition in oppositions)
            {
                if (opposition != null && opposition.IsActive == true)
                {
                    active.Add(opposition);
                }
            }

            return active.AsReadOnly();
        }
    }

    public PlaceContentRuntime(
        PlaceContentOwnerReference owner,
        PlaceSiteState siteState = PlaceSiteState.Cleared,
        PlaceAccessState accessState = PlaceAccessState.Accessible)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.siteState = siteState;
        this.accessState = accessState;
        readOnlyStackedContent = stackedContent.AsReadOnly();
        readOnlyNotableContent = notableContent.AsReadOnly();
        readOnlyOppositions = oppositions.AsReadOnly();
    }

    public PlaceContentRuntime(CityRuntime owner)
        : this(PlaceContentOwnerReference.ForCity(owner))
    {
    }

    public PlaceContentRuntime(ExplorableSiteRuntime owner)
        : this(PlaceContentOwnerReference.ForExplorableSite(owner))
    {
    }

    public PlaceContentRuntime(LocalPlaceRuntime owner, string macroLocationRuntimeId = null)
        : this(PlaceContentOwnerReference.ForLocalPlace(owner, macroLocationRuntimeId))
    {
    }

    public PlaceContentStackRuntime GetStack(ItemData item)
    {
        foreach (PlaceContentStackRuntime stack in stackedContent)
        {
            if (stack != null && stack.Item == item)
            {
                return stack;
            }
        }

        return null;
    }

    public NotableItemRuntime GetNotable(string runtimeId)
    {
        foreach (NotableItemRuntime notable in notableContent)
        {
            if (notable != null && notable.RuntimeId == runtimeId)
            {
                return notable;
            }
        }

        return null;
    }

    public PlaceOppositionRuntime GetOpposition(string runtimeId)
    {
        foreach (PlaceOppositionRuntime opposition in oppositions)
        {
            if (opposition != null && opposition.RuntimeId == runtimeId)
            {
                return opposition;
            }
        }

        return null;
    }

    public int GetAmount(ItemData item)
    {
        PlaceContentStackRuntime stack = GetStack(item);
        return stack != null ? stack.Amount : 0;
    }

    public int GetTotalStackedAmount()
    {
        int total = 0;
        foreach (PlaceContentStackRuntime stack in stackedContent)
        {
            if (stack != null && stack.Amount > 0 && total <= int.MaxValue - stack.Amount)
            {
                total += stack.Amount;
            }
        }

        return total;
    }

    public bool MarkSecured()
    {
        if (ActiveOppositions.Count > 0)
        {
            return false;
        }

        siteState = PlaceSiteState.Secured;
        accessState = PlaceAccessState.Accessible;
        return true;
    }

    public bool MarkInaccessible()
    {
        if (ActiveOppositions.Count > 0)
        {
            return false;
        }

        accessState = PlaceAccessState.Inaccessible;
        return true;
    }

    public bool TrySetController(string nextControllerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(nextControllerRuntimeId) == true)
        {
            return false;
        }

        controllerRuntimeId = nextControllerRuntimeId;
        return true;
    }

    internal void AddStack(PlaceContentStackRuntime stack)
    {
        stackedContent.Add(stack);
    }

    internal void RemoveStack(PlaceContentStackRuntime stack)
    {
        stackedContent.Remove(stack);
    }

    internal void AddNotable(NotableItemRuntime notable)
    {
        notableContent.Add(notable);
    }

    internal void RemoveNotable(NotableItemRuntime notable)
    {
        notableContent.Remove(notable);
    }

    internal void AddOpposition(PlaceOppositionRuntime opposition)
    {
        oppositions.Add(opposition);
        siteState = PlaceSiteState.Threatened;
        accessState = PlaceAccessState.Contested;
    }

    internal void MarkOppositionResolved(PlaceOppositionRuntime opposition)
    {
        if (opposition == null || oppositions.Contains(opposition) == false)
        {
            return;
        }

        if (ActiveOppositions.Count == 0)
        {
            siteState = PlaceSiteState.Cleared;
            accessState = PlaceAccessState.Accessible;
        }
    }

    internal void AdvanceDays(int dayCount)
    {
        for (int i = stackedContent.Count - 1; i >= 0; i--)
        {
            PlaceContentStackRuntime stack = stackedContent[i];
            stack?.AdvanceDays(dayCount);
            if (stack == null || stack.Amount <= 0)
            {
                stackedContent.RemoveAt(i);
            }
        }
    }
}

public sealed class PlaceContentStore
{
    private readonly RuntimeIdAllocator notableItemIdAllocator;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly List<PlaceContentRuntime> places = new List<PlaceContentRuntime>();
    private readonly List<NotableItemRuntime> notableItems = new List<NotableItemRuntime>();
    private readonly Dictionary<string, PlaceContentRuntime> placesByOwnerKey =
        new Dictionary<string, PlaceContentRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, NotableItemRuntime> notableByRuntimeId =
        new Dictionary<string, NotableItemRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<PlaceContentRuntime> readOnlyPlaces;
    private readonly IReadOnlyList<NotableItemRuntime> readOnlyNotableItems;

    public IReadOnlyList<PlaceContentRuntime> Places => readOnlyPlaces;
    public IReadOnlyList<NotableItemRuntime> NotableItems => readOnlyNotableItems;

    public PlaceContentStore()
        : this(null, null)
    {
    }

    public PlaceContentStore(
        RuntimeIdAllocator notableItemIdAllocator,
        RuntimeIdentityRegistry identityRegistry)
    {
        this.notableItemIdAllocator = notableItemIdAllocator;
        this.identityRegistry = identityRegistry;
        readOnlyPlaces = places.AsReadOnly();
        readOnlyNotableItems = notableItems.AsReadOnly();
    }

    public bool TryCreateNotableItem(
        ItemData definition,
        out NotableItemRuntime notable,
        out string diagnostic)
    {
        notable = null;
        diagnostic = null;
        if (definition == null)
        {
            diagnostic = "Notable item creation requires an ItemData definition.";
            return false;
        }

        if (notableItemIdAllocator == null || identityRegistry == null)
        {
            diagnostic = "Notable item creation requires a RuntimeIdAllocator and RuntimeIdentityRegistry.";
            return false;
        }

        string runtimeId;
        do
        {
            runtimeId = notableItemIdAllocator.AllocateNotableItemId();
        }
        while (identityRegistry.IsRuntimeIdAvailable(runtimeId) == false);

        NotableItemRuntime created = new NotableItemRuntime(runtimeId, definition);
        if (TryRegisterNotableItem(created, out diagnostic) == false)
        {
            return false;
        }

        notable = created;
        return true;
    }

    public NotableItemRuntime CreateNotableItem(ItemData definition)
    {
        if (TryCreateNotableItem(definition, out NotableItemRuntime notable, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic);
        }

        return notable;
    }

    public bool TryRegisterNotableItem(NotableItemRuntime notable, out string diagnostic)
    {
        diagnostic = null;
        if (notable == null)
        {
            diagnostic = "Cannot register a null NotableItemRuntime.";
            return false;
        }

        if (identityRegistry == null)
        {
            diagnostic = "Notable items require a shared RuntimeIdentityRegistry.";
            return false;
        }

        if (notableByRuntimeId.TryGetValue(notable.RuntimeId, out NotableItemRuntime existing) == true)
        {
            if (ReferenceEquals(existing, notable) == true)
            {
                return true;
            }

            diagnostic = $"Notable item RuntimeId '{notable.RuntimeId}' already belongs to another instance.";
            return false;
        }

        if (identityRegistry.TryGetNotableItemWithoutLogging(notable.RuntimeId, out NotableItemRuntime registered) == true)
        {
            if (ReferenceEquals(registered, notable) == false)
            {
                diagnostic = $"Notable item RuntimeId '{notable.RuntimeId}' is already globally registered.";
                return false;
            }
        }
        else if (identityRegistry.RegisterNotableItem(notable) == false)
        {
            diagnostic = $"Notable item RuntimeId '{notable.RuntimeId}' conflicts with another runtime identity.";
            return false;
        }

        notableByRuntimeId.Add(notable.RuntimeId, notable);
        notableItems.Add(notable);
        return true;
    }

    public bool TryGetNotableItem(string runtimeId, out NotableItemRuntime notable)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && notableByRuntimeId.TryGetValue(runtimeId, out notable) == true)
        {
            return true;
        }

        notable = null;
        return false;
    }

    public IReadOnlyList<NotableItemRuntime> GetNotableItemsAtPlace(PlaceContentOwnerReference owner)
    {
        return owner != null && TryGet(owner, out PlaceContentRuntime content) == true
            ? content.NotableContent
            : Array.Empty<NotableItemRuntime>();
    }

    public IReadOnlyList<NotableItemRuntime> GetNotableItemsHeldByNpc(NpcRuntime npc)
    {
        return npc != null ? GetNotableItemsHeldByNpc(npc.RuntimeId) : Array.Empty<NotableItemRuntime>();
    }

    public IReadOnlyList<NotableItemRuntime> GetNotableItemsHeldByNpc(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return Array.Empty<NotableItemRuntime>();
        }

        List<NotableItemRuntime> held = new List<NotableItemRuntime>();
        foreach (NotableItemRuntime notable in notableItems)
        {
            if (notable != null
                && notable.Custody?.CustodyKind == NotableItemCustodyKind.Npc
                && string.Equals(notable.Custody.NpcRuntimeId, npcRuntimeId, StringComparison.Ordinal) == true)
            {
                held.Add(notable);
            }
        }

        held.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        return held.AsReadOnly();
    }

    public PlaceContentRuntime GetOrCreate(CityRuntime cityRuntime)
    {
        return GetOrCreate(PlaceContentOwnerReference.ForCity(cityRuntime));
    }

    public PlaceContentRuntime GetOrCreate(ExplorableSiteRuntime siteRuntime)
    {
        return GetOrCreate(PlaceContentOwnerReference.ForExplorableSite(siteRuntime));
    }

    public PlaceContentRuntime GetOrCreate(LocalPlaceRuntime localPlace, string macroLocationRuntimeId = null)
    {
        return GetOrCreate(PlaceContentOwnerReference.ForLocalPlace(localPlace, macroLocationRuntimeId));
    }

    public PlaceContentRuntime GetOrCreate(PlaceContentOwnerReference owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (placesByOwnerKey.TryGetValue(owner.StableKey, out PlaceContentRuntime existing) == true)
        {
            return existing;
        }

        PlaceContentRuntime created = new PlaceContentRuntime(owner);
        placesByOwnerKey.Add(owner.StableKey, created);
        places.Add(created);
        return created;
    }

    public bool TryGet(PlaceContentOwnerReference owner, out PlaceContentRuntime content)
    {
        if (owner != null && placesByOwnerKey.TryGetValue(owner.StableKey, out content) == true)
        {
            return true;
        }

        content = null;
        return false;
    }

    public bool TryGet(CityRuntime cityRuntime, out PlaceContentRuntime content)
    {
        content = null;
        return cityRuntime != null && TryGet(PlaceContentOwnerReference.ForCity(cityRuntime), out content);
    }

    public bool TryGet(ExplorableSiteRuntime siteRuntime, out PlaceContentRuntime content)
    {
        content = null;
        return siteRuntime != null && TryGet(PlaceContentOwnerReference.ForExplorableSite(siteRuntime), out content);
    }

    public bool TryGet(LocalPlaceRuntime localPlace, out PlaceContentRuntime content)
    {
        content = null;
        return localPlace != null && TryGet(PlaceContentOwnerReference.ForLocalPlace(localPlace), out content);
    }

    public bool TryAddStack(
        PlaceContentOwnerReference owner,
        ItemData item,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        out PlaceContentStackRuntime stack,
        int decayPerDay = 0,
        float averageUnitCost = 0f)
    {
        stack = null;
        if (TryValidateStackInput(owner, item, amount, persistencePolicy, decayPerDay, averageUnitCost, out string diagnostic) == false)
        {
            return false;
        }

        PlaceContentRuntime content = GetOrCreate(owner);
        PlaceContentStackRuntime existing = content.GetStack(item);
        if (existing != null)
        {
            if (existing.PersistencePolicy != persistencePolicy || existing.DecayPerDay != decayPerDay
                || existing.CanAdd(amount, averageUnitCost) == false)
            {
                return false;
            }

            existing.TryAdd(amount, averageUnitCost);
            stack = existing;
            return true;
        }

        PlaceContentStackRuntime created = new PlaceContentStackRuntime(
            item,
            amount,
            persistencePolicy,
            decayPerDay,
            averageUnitCost);
        content.AddStack(created);
        stack = created;
        return true;
    }

    public bool TryAddStack(
        CityRuntime cityRuntime,
        ItemData item,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        out PlaceContentStackRuntime stack,
        int decayPerDay = 0,
        float averageUnitCost = 0f)
    {
        stack = null;
        return cityRuntime != null && TryAddStack(
            PlaceContentOwnerReference.ForCity(cityRuntime), item, amount, persistencePolicy, out stack, decayPerDay, averageUnitCost);
    }

    public bool TryAddStack(
        ExplorableSiteRuntime siteRuntime,
        ItemData item,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        out PlaceContentStackRuntime stack,
        int decayPerDay = 0,
        float averageUnitCost = 0f)
    {
        stack = null;
        return siteRuntime != null && TryAddStack(
            PlaceContentOwnerReference.ForExplorableSite(siteRuntime), item, amount, persistencePolicy, out stack, decayPerDay, averageUnitCost);
    }

    public bool TryAddStack(
        LocalPlaceRuntime localPlace,
        ItemData item,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        out PlaceContentStackRuntime stack,
        int decayPerDay = 0,
        float averageUnitCost = 0f)
    {
        stack = null;
        return localPlace != null && TryAddStack(
            PlaceContentOwnerReference.ForLocalPlace(localPlace), item, amount, persistencePolicy, out stack, decayPerDay, averageUnitCost);
    }

    public bool TryTakeStack(
        PlaceContentOwnerReference owner,
        ItemData item,
        int amount,
        out int removedAmount)
    {
        removedAmount = 0;
        if (owner == null || item == null || amount <= 0 || TryGet(owner, out PlaceContentRuntime content) == false)
        {
            return false;
        }

        PlaceContentStackRuntime stack = content.GetStack(item);
        if (stack == null || stack.Amount < amount)
        {
            return false;
        }

        if (stack.TryRemove(amount) == false)
        {
            return false;
        }

        removedAmount = amount;
        if (stack.Amount <= 0)
        {
            content.RemoveStack(stack);
        }

        return true;
    }

    public bool TryTakeStack(CityRuntime cityRuntime, ItemData item, int amount, out int removedAmount)
    {
        removedAmount = 0;
        return cityRuntime != null && TryTakeStack(
            PlaceContentOwnerReference.ForCity(cityRuntime), item, amount, out removedAmount);
    }

    public bool TryTakeStack(ExplorableSiteRuntime siteRuntime, ItemData item, int amount, out int removedAmount)
    {
        removedAmount = 0;
        return siteRuntime != null && TryTakeStack(
            PlaceContentOwnerReference.ForExplorableSite(siteRuntime), item, amount, out removedAmount);
    }

    public bool TryTakeStack(LocalPlaceRuntime localPlace, ItemData item, int amount, out int removedAmount)
    {
        removedAmount = 0;
        return localPlace != null && TryTakeStack(
            PlaceContentOwnerReference.ForLocalPlace(localPlace), item, amount, out removedAmount);
    }

    public bool TryTakeNotable(
        PlaceContentOwnerReference owner,
        string notableRuntimeId,
        NpcRuntime destinationNpc,
        out NotableItemRuntime notable)
    {
        return TryTransferNotableFromPlaceToNpc(
            owner,
            notableRuntimeId,
            destinationNpc,
            out notable,
            out _);
    }

    public bool TryTransferNotableFromPlaceToNpc(
        PlaceContentOwnerReference sourceOwner,
        string notableRuntimeId,
        NpcRuntime destinationNpc,
        out NotableItemRuntime notable,
        out string diagnostic)
    {
        notable = null;
        diagnostic = null;
        if (sourceOwner == null || destinationNpc == null || string.IsNullOrWhiteSpace(notableRuntimeId) == true)
        {
            diagnostic = "Place-to-NPC transfer requires a source place, notable RuntimeId, and destination NPC.";
            return false;
        }

        if (TryValidateNpcIdentity(destinationNpc, out diagnostic) == false)
        {
            return false;
        }

        if (TryGet(sourceOwner, out PlaceContentRuntime sourceContent) == false
            || TryGetNotableItem(notableRuntimeId, out notable) == false
            || ReferenceEquals(sourceContent.GetNotable(notableRuntimeId), notable) == false)
        {
            notable = null;
            diagnostic = "The notable item is not present at the expected source place.";
            return false;
        }

        NotableItemCustodyReference expectedCustody = NotableItemCustodyReference.ForPlace(sourceOwner);
        NotableItemCustodyReference nextCustody = NotableItemCustodyReference.ForNpc(destinationNpc);
        if (notable.Custody?.Matches(expectedCustody) != true)
        {
            notable = null;
            diagnostic = "The notable item's custody does not match the expected source place.";
            return false;
        }

        if (notable.TryTransferCustody(expectedCustody, nextCustody) == false)
        {
            notable = null;
            diagnostic = "The notable item custody transfer was rejected before mutation.";
            return false;
        }

        sourceContent.RemoveNotable(notable);
        return true;
    }

    public bool TryTransferNotableFromNpcToPlace(
        NpcRuntime sourceNpc,
        string notableRuntimeId,
        PlaceContentOwnerReference destinationOwner,
        out NotableItemRuntime notable,
        out string diagnostic)
    {
        notable = null;
        diagnostic = null;
        if (sourceNpc == null || destinationOwner == null || string.IsNullOrWhiteSpace(notableRuntimeId) == true)
        {
            diagnostic = "NPC-to-place transfer requires a source NPC, notable RuntimeId, and destination place.";
            return false;
        }

        if (TryValidateNpcIdentity(sourceNpc, out diagnostic) == false)
        {
            return false;
        }

        if (TryGetNotableItem(notableRuntimeId, out notable) == false)
        {
            diagnostic = "The notable item is not globally registered in this store.";
            return false;
        }

        NotableItemCustodyReference expectedCustody = NotableItemCustodyReference.ForNpc(sourceNpc);
        if (notable.Custody?.Matches(expectedCustody) != true)
        {
            notable = null;
            diagnostic = "The source NPC does not hold the notable item.";
            return false;
        }

        if (TryGet(destinationOwner, out PlaceContentRuntime existingDestination) == true
            && existingDestination.GetNotable(notableRuntimeId) != null)
        {
            notable = null;
            diagnostic = "The destination place already contains this notable RuntimeId.";
            return false;
        }

        PlaceContentRuntime destinationContent = existingDestination ?? GetOrCreate(destinationOwner);
        NotableItemCustodyReference nextCustody = NotableItemCustodyReference.ForPlace(destinationOwner);
        if (notable.TryTransferCustody(expectedCustody, nextCustody) == false)
        {
            notable = null;
            diagnostic = "The notable item custody transfer was rejected before mutation.";
            return false;
        }

        destinationContent.AddNotable(notable);
        return true;
    }

    public bool TryTransferNotableBetweenPlaces(
        PlaceContentOwnerReference sourceOwner,
        PlaceContentOwnerReference destinationOwner,
        string notableRuntimeId,
        out NotableItemRuntime notable,
        out string diagnostic)
    {
        notable = null;
        diagnostic = null;
        if (sourceOwner == null || destinationOwner == null
            || string.IsNullOrWhiteSpace(notableRuntimeId) == true
            || string.Equals(sourceOwner.StableKey, destinationOwner.StableKey, StringComparison.Ordinal) == true)
        {
            diagnostic = "Place-to-place transfer requires distinct valid source and destination places.";
            return false;
        }

        if (TryGet(sourceOwner, out PlaceContentRuntime sourceContent) == false
            || TryGetNotableItem(notableRuntimeId, out notable) == false
            || ReferenceEquals(sourceContent.GetNotable(notableRuntimeId), notable) == false)
        {
            notable = null;
            diagnostic = "The notable item is not present at the expected source place.";
            return false;
        }

        if (TryGet(destinationOwner, out PlaceContentRuntime existingDestination) == true
            && existingDestination.GetNotable(notableRuntimeId) != null)
        {
            notable = null;
            diagnostic = "The destination place already contains this notable RuntimeId.";
            return false;
        }

        NotableItemCustodyReference expectedCustody = NotableItemCustodyReference.ForPlace(sourceOwner);
        NotableItemCustodyReference nextCustody = NotableItemCustodyReference.ForPlace(destinationOwner);
        if (notable.Custody?.Matches(expectedCustody) != true)
        {
            notable = null;
            diagnostic = "The notable item's custody does not match the expected source place.";
            return false;
        }

        PlaceContentRuntime destinationContent = existingDestination ?? GetOrCreate(destinationOwner);
        if (notable.TryTransferCustody(expectedCustody, nextCustody) == false)
        {
            notable = null;
            diagnostic = "The notable item custody transfer was rejected before mutation.";
            return false;
        }

        sourceContent.RemoveNotable(notable);
        destinationContent.AddNotable(notable);
        return true;
    }

    public bool TryAddNotable(
        PlaceContentOwnerReference owner,
        NotableItemRuntime notable,
        out string diagnostic)
    {
        diagnostic = null;
        if (owner == null || notable == null)
        {
            diagnostic = "Notable content requires an owner and an item.";
            return false;
        }

        if (notable.IsPresent == true)
        {
            diagnostic = "A NotableItemRuntime can have only one place or NPC custody at a time.";
            return false;
        }

        if (TryGet(owner, out PlaceContentRuntime existingContent) == true
            && existingContent.GetNotable(notable.RuntimeId) != null)
        {
            diagnostic = "The destination place already contains this notable RuntimeId.";
            return false;
        }

        if (TryRegisterNotableItem(notable, out diagnostic) == false)
        {
            return false;
        }

        PlaceContentRuntime content = existingContent ?? GetOrCreate(owner);
        if (notable.TryAssignCustody(NotableItemCustodyReference.ForPlace(owner)) == false)
        {
            diagnostic = "Notable content mutation was rejected before insertion.";
            return false;
        }

        content.AddNotable(notable);
        return true;
    }

    public bool TryAddNotable(CityRuntime cityRuntime, NotableItemRuntime notable, out string diagnostic)
    {
        diagnostic = null;
        return cityRuntime != null && TryAddNotable(PlaceContentOwnerReference.ForCity(cityRuntime), notable, out diagnostic);
    }

    public bool TryAddNotable(ExplorableSiteRuntime siteRuntime, NotableItemRuntime notable, out string diagnostic)
    {
        diagnostic = null;
        return siteRuntime != null && TryAddNotable(PlaceContentOwnerReference.ForExplorableSite(siteRuntime), notable, out diagnostic);
    }

    public bool TryAddNotable(LocalPlaceRuntime localPlace, NotableItemRuntime notable, out string diagnostic)
    {
        diagnostic = null;
        return localPlace != null && TryAddNotable(PlaceContentOwnerReference.ForLocalPlace(localPlace), notable, out diagnostic);
    }

    public bool TryAddOpposition(
        PlaceContentOwnerReference owner,
        PlaceOppositionRuntime opposition,
        out string diagnostic)
    {
        diagnostic = null;
        if (owner == null || opposition == null)
        {
            diagnostic = "Opposition content requires an owner and an opposition runtime.";
            return false;
        }

        PlaceContentRuntime content = GetOrCreate(owner);
        if (content.GetOpposition(opposition.RuntimeId) != null)
        {
            diagnostic = "An opposition RuntimeId cannot be duplicated at a place.";
            return false;
        }

        content.AddOpposition(opposition);
        return true;
    }

    public bool TryAddOpposition(CityRuntime cityRuntime, PlaceOppositionRuntime opposition, out string diagnostic)
    {
        diagnostic = null;
        return cityRuntime != null && TryAddOpposition(
            PlaceContentOwnerReference.ForCity(cityRuntime), opposition, out diagnostic);
    }

    public bool TryAddOpposition(ExplorableSiteRuntime siteRuntime, PlaceOppositionRuntime opposition, out string diagnostic)
    {
        diagnostic = null;
        return siteRuntime != null && TryAddOpposition(
            PlaceContentOwnerReference.ForExplorableSite(siteRuntime), opposition, out diagnostic);
    }

    public bool TryAddOpposition(LocalPlaceRuntime localPlace, PlaceOppositionRuntime opposition, out string diagnostic)
    {
        diagnostic = null;
        return localPlace != null && TryAddOpposition(
            PlaceContentOwnerReference.ForLocalPlace(localPlace), opposition, out diagnostic);
    }

    public bool TryResolveOpposition(
        PlaceContentOwnerReference owner,
        PlaceOppositionRuntime opposition,
        Conflict conflict,
        ConflictResolutionService conflictResolutionService,
        out ConflictResolutionResult result,
        out string diagnostic)
    {
        return TryResolveOpposition(owner, opposition, conflict, conflictResolutionService, null, out result, out diagnostic);
    }

    public bool TryResolveOpposition(
        PlaceContentOwnerReference owner,
        PlaceOppositionRuntime opposition,
        Conflict conflict,
        ConflictResolutionService conflictResolutionService,
        ConflictResolutionConstraints constraints,
        out ConflictResolutionResult result,
        out string diagnostic)
    {
        result = null;
        diagnostic = null;
        if (owner == null || opposition == null || conflict == null || conflictResolutionService == null)
        {
            diagnostic = "Opposition resolution requires an owner, active opposition, conflict, and generic resolver service.";
            return false;
        }

        if (TryGet(owner, out PlaceContentRuntime content) == false
            || content.GetOpposition(opposition.RuntimeId) != opposition
            || opposition.IsActive == false)
        {
            diagnostic = "Opposition is not an active World Truth member of this place.";
            return false;
        }

        if (conflict.TryValidate(out diagnostic) == false)
        {
            return false;
        }

        if (conflictResolutionService.TryResolveAndApply(conflict, constraints, out result, out diagnostic) == false)
        {
            return false;
        }

        if (result.Outcome == ConflictOutcomeType.Victory
            && result.WinningSideId != opposition.OppositionSideId)
        {
            opposition.MarkResolved(result);
            content.MarkOppositionResolved(opposition);
        }

        return true;
    }

    public bool TrySecure(PlaceContentOwnerReference owner, out string diagnostic)
    {
        diagnostic = null;
        if (owner == null || TryGet(owner, out PlaceContentRuntime content) == false)
        {
            diagnostic = "Cannot secure an unregistered place content owner.";
            return false;
        }

        if (content.MarkSecured() == false)
        {
            diagnostic = "A place with active opposition cannot become Secured.";
            return false;
        }

        return true;
    }

    public bool TrySecure(CityRuntime cityRuntime, out string diagnostic)
    {
        diagnostic = null;
        return cityRuntime != null && TrySecure(PlaceContentOwnerReference.ForCity(cityRuntime), out diagnostic);
    }

    public bool TrySecure(ExplorableSiteRuntime siteRuntime, out string diagnostic)
    {
        diagnostic = null;
        return siteRuntime != null && TrySecure(PlaceContentOwnerReference.ForExplorableSite(siteRuntime), out diagnostic);
    }

    public bool TrySecure(LocalPlaceRuntime localPlace, out string diagnostic)
    {
        diagnostic = null;
        return localPlace != null && TrySecure(PlaceContentOwnerReference.ForLocalPlace(localPlace), out diagnostic);
    }

    public void AdvanceDays(int dayCount)
    {
        if (dayCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount));
        }

        foreach (PlaceContentRuntime content in places)
        {
            content?.AdvanceDays(dayCount);
        }
    }

    private bool TryValidateNpcIdentity(NpcRuntime npc, out string diagnostic)
    {
        diagnostic = null;
        if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) == true)
        {
            diagnostic = "Notable item NPC custody requires a valid NpcRuntime identity.";
            return false;
        }

        if (identityRegistry != null
            && (identityRegistry.TryGetNpcWithoutLogging(npc.RuntimeId, out NpcRuntime registeredNpc) == false
                || ReferenceEquals(registeredNpc, npc) == false))
        {
            diagnostic = $"NPC RuntimeId '{npc.RuntimeId}' is not the globally registered NPC instance.";
            return false;
        }

        return true;
    }

    private static bool TryValidateStackInput(
        PlaceContentOwnerReference owner,
        ItemData item,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        int decayPerDay,
        float averageUnitCost,
        out string diagnostic)
    {
        diagnostic = null;
        if (owner == null || item == null || amount <= 0)
        {
            diagnostic = "Stacked content requires an owner, an ItemData definition, and a positive amount.";
            return false;
        }

        if (Enum.IsDefined(typeof(PlaceContentPersistencePolicy), persistencePolicy) == false
            || persistencePolicy == PlaceContentPersistencePolicy.Notable)
        {
            diagnostic = "Stacked content cannot use the Notable persistence policy.";
            return false;
        }

        if (decayPerDay < 0 || (persistencePolicy == PlaceContentPersistencePolicy.Perishable && decayPerDay == 0))
        {
            diagnostic = "Perishable content requires a positive deterministic decay rate.";
            return false;
        }

        if (float.IsNaN(averageUnitCost) == true || float.IsInfinity(averageUnitCost) == true || averageUnitCost < 0f)
        {
            diagnostic = "Stacked content average unit cost must be finite and non-negative.";
            return false;
        }

        return true;
    }
}
