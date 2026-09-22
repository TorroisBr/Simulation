using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// World-owned authoritative store for armed-force identity, hierarchy,
/// aggregate contingents, relevant Person references, and lifecycle.
///
/// The store intentionally contains no Unity objects, NpcRuntime membership,
/// daily processing, recruitment, movement, battle, or war state.
/// </summary>
public sealed class ArmedForceStore
{
    private readonly PersonStore personStore;
    private readonly Dictionary<string, ArmedForceRecord> forcesById =
        new Dictionary<string, ArmedForceRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, ContingentRecord> contingentsById =
        new Dictionary<string, ContingentRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, ArmedForcePersonReference> relevantPersonsById =
        new Dictionary<string, ArmedForcePersonReference>(StringComparer.Ordinal);
    private long revision;

    public ArmedForceStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    public PersonStore PersonStore => personStore;
    public int Count => forcesById.Count;
    public int ContingentCount => contingentsById.Count;
    public int RelevantPersonCount => relevantPersonsById.Count;
    public long Revision => revision;

    public IReadOnlyList<ArmedForceRecord> Forces
    {
        get
        {
            List<ArmedForceRecord> values = new List<ArmedForceRecord>(forcesById.Values);
            values.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
            return new ReadOnlyCollection<ArmedForceRecord>(values);
        }
    }

    public IReadOnlyList<ContingentRecord> Contingents
    {
        get
        {
            List<ContingentRecord> values = new List<ContingentRecord>(contingentsById.Values);
            values.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
            return new ReadOnlyCollection<ContingentRecord>(values);
        }
    }

    public IReadOnlyList<ArmedForcePersonReference> RelevantPersons
    {
        get
        {
            List<ArmedForcePersonReference> values =
                new List<ArmedForcePersonReference>(relevantPersonsById.Values);
            values.Sort(CompareRelevantPersons);
            return new ReadOnlyCollection<ArmedForcePersonReference>(values);
        }
    }

    public bool TryRegister(
        ArmedForceRecord record,
        out ArmedForceFoundationFailure failure)
    {
        if (record == null || record.Id == null)
        {
            return Fail(
                ArmedForceFoundationFailureCode.InvalidForce,
                "A force with a stable ArmedForceId is required.",
                out failure);
        }

        if (forcesById.ContainsKey(record.Id.Value))
        {
            return Fail(
                ArmedForceFoundationFailureCode.DuplicateForceId,
                "The ArmedForceId is already registered.",
                out failure);
        }

        if (record.ParentForceId == record.Id)
        {
            return Fail(
                ArmedForceFoundationFailureCode.SelfParent,
                "An armed force cannot be its own parent.",
                out failure);
        }

        if (record.ParentForceId != null
            && forcesById.TryGetValue(record.ParentForceId.Value, out ArmedForceRecord parent) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ParentNotRegistered,
                "The parent ArmedForceId is not registered.",
                out failure);
        }

        if (record.IsActive
            && record.ParentForceId != null
            && forcesById[record.ParentForceId.Value].IsActive == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ParentTerminated,
                "An active force cannot be registered under a terminated parent.",
                out failure);
        }

        if (record.IsDetached && record.ParentForceId == null)
        {
            return Fail(
                ArmedForceFoundationFailureCode.DetachedRoot,
                "A detached force must remain structurally subordinate to a parent.",
                out failure);
        }

        if (record.CommanderPersonId != null
            && personStore.TryGet(record.CommanderPersonId, out _) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.PersonNotRegistered,
                "The commander PersonId is not registered in the world PersonStore.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById.Add(record.Id.Value, record);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public bool TryGet(ArmedForceId forceId, out ArmedForceRecord record)
    {
        record = null;
        return forceId != null && forcesById.TryGetValue(forceId.Value, out record);
    }

    public bool TryGetContingent(ContingentId contingentId, out ContingentRecord record)
    {
        record = null;
        return contingentId != null && contingentsById.TryGetValue(contingentId.Value, out record);
    }

    public bool TryGetRelevantPerson(
        ArmedForcePersonReferenceId referenceId,
        out ArmedForcePersonReference reference)
    {
        reference = null;
        return referenceId != null
            && relevantPersonsById.TryGetValue(referenceId.Value, out reference);
    }

    /// <summary>
    /// Performs an ordinary structural reorganization while preserving the
    /// force identity. It is not a secession, schism, absorption, or merger.
    /// A detached subforce must be reattached before it is structurally
    /// reparented.
    /// </summary>
    public bool TryReparent(
        ArmedForceId forceId,
        ArmedForceId parentForceId,
        out ArmedForceFoundationFailure failure)
    {
        if (TryResolveActiveForce(forceId, out ArmedForceRecord force, out failure) == false)
        {
            return false;
        }

        if (force.IsDetached)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceDetached,
                "A detached force must be reattached before structural reorganization.",
                out failure);
        }

        if (parentForceId == force.Id)
        {
            return Fail(
                ArmedForceFoundationFailureCode.SelfParent,
                "An armed force cannot be its own parent.",
                out failure);
        }

        ArmedForceRecord parent = null;
        if (parentForceId != null)
        {
            if (forcesById.TryGetValue(parentForceId.Value, out parent) == false)
            {
                return Fail(
                    ArmedForceFoundationFailureCode.ParentNotRegistered,
                    "The parent ArmedForceId is not registered.",
                    out failure);
            }

            if (parent.IsActive == false)
            {
                return Fail(
                    ArmedForceFoundationFailureCode.ParentTerminated,
                    "An active force cannot be reparented under a terminated parent.",
                    out failure);
            }
        }

        if (parentForceId != null && WouldCreateCycle(force.Id, parentForceId))
        {
            return Fail(
                ArmedForceFoundationFailureCode.ParentWouldCreateCycle,
                "The proposed parent would create an armed-force hierarchy cycle.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById[force.Id.Value] = force.WithParent(parentForceId);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    /// <summary>
    /// Changes operational separation only. Parent linkage and force identity
    /// remain unchanged, so this is not secession.
    /// </summary>
    public bool TryDetach(
        ArmedForceId forceId,
        string operationalLocationReference,
        out ArmedForceFoundationFailure failure)
    {
        if (TryResolveActiveForce(forceId, out ArmedForceRecord force, out failure) == false)
        {
            return false;
        }

        if (force.ParentForceId == null)
        {
            return Fail(
                ArmedForceFoundationFailureCode.DetachedRoot,
                "A root force cannot be detached as a subordinate subforce.",
                out failure);
        }

        if (force.IsDetached)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceDetached,
                "The subforce is already detached.",
                out failure);
        }

        if (string.IsNullOrWhiteSpace(operationalLocationReference) == false)
        {
            force = force.WithOperationalLocation(operationalLocationReference);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById[force.Id.Value] = force.WithDetached(true);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public bool TryReattach(
        ArmedForceId forceId,
        out ArmedForceFoundationFailure failure)
    {
        if (TryResolveActiveForce(forceId, out ArmedForceRecord force, out failure) == false)
        {
            return false;
        }

        if (force.ParentForceId == null)
        {
            return Fail(
                ArmedForceFoundationFailureCode.DetachedRoot,
                "A root force has no subordinate parent attachment to restore.",
                out failure);
        }

        if (force.IsDetached == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceNotDetached,
                "The subforce is not detached.",
                out failure);
        }

        if (forcesById.TryGetValue(force.ParentForceId.Value, out ArmedForceRecord parent) == false
            || parent.IsActive == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ParentTerminated,
                "The original parent force is not active.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById[force.Id.Value] = force.WithDetached(false);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    /// <summary>
    /// Records a current location reference without implementing movement,
    /// routing, travel time, or military operations.
    /// </summary>
    public bool TrySetOperationalLocation(
        ArmedForceId forceId,
        string operationalLocationReference,
        out ArmedForceFoundationFailure failure)
    {
        if (TryResolveActiveForce(forceId, out ArmedForceRecord force, out failure) == false)
        {
            return false;
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById[force.Id.Value] = force.WithOperationalLocation(operationalLocationReference);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    /// <summary>
    /// Assigns or clears a PersonId-based commander reference. This does not
    /// imply ownership, allegiance, loyalty, membership, funding, control, or
    /// political office.
    /// </summary>
    public bool TryAssignCommander(
        ArmedForceId forceId,
        PersonId commanderPersonId,
        out ArmedForceFoundationFailure failure)
    {
        if (TryResolveActiveForce(forceId, out ArmedForceRecord force, out failure) == false)
        {
            return false;
        }

        if (commanderPersonId != null
            && personStore.TryGet(commanderPersonId, out _) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.PersonNotRegistered,
                "The commander PersonId is not registered in the world PersonStore.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById[force.Id.Value] = force.WithCommander(commanderPersonId);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public bool TryAddRelevantPerson(
        ArmedForceId forceId,
        PersonId personId,
        string roleKey,
        out ArmedForcePersonReference reference,
        out ArmedForceFoundationFailure failure)
    {
        reference = null;
        if (TryResolveActiveForce(forceId, out _, out failure) == false)
        {
            return false;
        }

        if (personId == null || string.IsNullOrWhiteSpace(roleKey))
        {
            return Fail(
                ArmedForceFoundationFailureCode.InvalidRelevantPerson,
                "A relevant Person reference requires PersonId and role key.",
                out failure);
        }

        if (personStore.TryGet(personId, out _) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.PersonNotRegistered,
                "The relevant PersonId is not registered in the world PersonStore.",
                out failure);
        }

        reference = new ArmedForcePersonReference(forceId, personId, roleKey);
        if (relevantPersonsById.ContainsKey(reference.Id.Value))
        {
            reference = null;
            return Fail(
                ArmedForceFoundationFailureCode.DuplicateRelevantPerson,
                "The force already has this PersonId and role reference.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            reference = null;
            return false;
        }

        relevantPersonsById.Add(reference.Id.Value, reference);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public IReadOnlyList<ArmedForcePersonReference> GetRelevantPersons(ArmedForceId forceId)
    {
        List<ArmedForcePersonReference> result = new List<ArmedForcePersonReference>();
        if (forceId == null) return new ReadOnlyCollection<ArmedForcePersonReference>(result);

        foreach (ArmedForcePersonReference reference in relevantPersonsById.Values)
        {
            if (reference.ForceId == forceId) result.Add(reference);
        }

        result.Sort(CompareRelevantPersons);
        return new ReadOnlyCollection<ArmedForcePersonReference>(result);
    }

    public bool TryRegisterContingent(
        ContingentRecord contingent,
        out ArmedForceFoundationFailure failure)
    {
        if (contingent == null || contingent.Id == null || contingent.ForceId == null)
        {
            return Fail(
                ArmedForceFoundationFailureCode.InvalidContingent,
                "A contingent requires stable ContingentId and ArmedForceId values.",
                out failure);
        }

        if (contingentsById.ContainsKey(contingent.Id.Value))
        {
            return Fail(
                ArmedForceFoundationFailureCode.DuplicateContingentId,
                "The ContingentId is already registered.",
                out failure);
        }

        if (forcesById.TryGetValue(contingent.ForceId.Value, out ArmedForceRecord force) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceNotRegistered,
                "The contingent force is not registered.",
                out failure);
        }

        if (force.IsActive == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ContingentForceNotActive,
                "A new contingent cannot be attached to a terminated force.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        contingentsById.Add(contingent.Id.Value, contingent);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    /// <summary>
    /// Replaces composition under the same contingent identity. This is a
    /// current-state update only; recruitment, mobilization, and casualty
    /// accounting remain outside this checkpoint.
    /// </summary>
    public bool TryReplaceContingent(
        ContingentRecord replacement,
        out ArmedForceFoundationFailure failure)
    {
        if (replacement == null || replacement.Id == null || replacement.ForceId == null)
        {
            return Fail(
                ArmedForceFoundationFailureCode.InvalidContingent,
                "A contingent replacement requires stable identity values.",
                out failure);
        }

        if (contingentsById.TryGetValue(replacement.Id.Value, out ContingentRecord current) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ContingentNotRegistered,
                "The contingent replacement target is not registered.",
                out failure);
        }

        if (current.ForceId != replacement.ForceId)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ContingentForceMismatch,
                "A contingent replacement cannot move a contingent to another force.",
                out failure);
        }

        if (forcesById.TryGetValue(current.ForceId.Value, out ArmedForceRecord force) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceNotRegistered,
                "The contingent force is not registered.",
                out failure);
        }

        if (force.IsActive == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ContingentForceNotActive,
                "A terminated force cannot receive composition updates.",
                out failure);
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        contingentsById[replacement.Id.Value] = replacement;
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public IReadOnlyList<ContingentRecord> GetContingents(ArmedForceId forceId)
    {
        List<ContingentRecord> result = new List<ContingentRecord>();
        if (forceId == null) return new ReadOnlyCollection<ContingentRecord>(result);

        foreach (ContingentRecord contingent in contingentsById.Values)
        {
            if (contingent.ForceId == forceId) result.Add(contingent);
        }

        result.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<ContingentRecord>(result);
    }

    /// <summary>
    /// Returns aggregate contingent amount for a force and its current active
    /// descendant subforces. Terminated descendants are excluded by default so
    /// historical composition does not become current manpower implicitly.
    /// </summary>
    public bool TryGetAggregateContingentAmount(
        ArmedForceId forceId,
        out long amount,
        out ArmedForceFoundationFailure failure,
        bool includeTerminatedDescendants = false)
    {
        amount = 0L;
        if (TryGet(forceId, out _) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceNotRegistered,
                "The aggregate amount root force is not registered.",
                out failure);
        }

        try
        {
            foreach (ArmedForceRecord force in GetHierarchyPreOrder(forceId))
            {
                if (force.Id != forceId
                    && includeTerminatedDescendants == false
                    && force.IsActive == false)
                {
                    continue;
                }

                foreach (ContingentRecord contingent in GetContingents(force.Id))
                {
                    amount = checked(amount + contingent.Amount);
                }
            }
        }
        catch (OverflowException)
        {
            amount = 0L;
            return Fail(
                ArmedForceFoundationFailureCode.AggregateAmountOverflow,
                "The aggregate contingent amount exceeded Int64 capacity.",
                out failure);
        }

        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public bool TryTerminate(
        ArmedForceId forceId,
        long terminatedAbsoluteDay,
        out ArmedForceFoundationFailure failure)
    {
        if (TryResolveActiveForce(forceId, out ArmedForceRecord force, out failure) == false)
        {
            return false;
        }

        if (terminatedAbsoluteDay < force.CreatedAbsoluteDay)
        {
            return Fail(
                ArmedForceFoundationFailureCode.InvalidDay,
                "A force cannot terminate before it was created.",
                out failure);
        }

        foreach (ArmedForceRecord child in GetChildren(force.Id))
        {
            if (child.IsActive)
            {
                return Fail(
                    ArmedForceFoundationFailureCode.ActiveChildrenPreventTermination,
                    "A force with active subordinate forces cannot be terminated in this foundation.",
                    out failure);
            }
        }

        if (CanAdvanceRevision(out failure) == false) return false;

        forcesById[force.Id.Value] = force.WithTerminated(terminatedAbsoluteDay);
        revision++;
        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    public IReadOnlyList<ArmedForceRecord> GetChildren(ArmedForceId parentForceId)
    {
        List<ArmedForceRecord> result = new List<ArmedForceRecord>();
        if (parentForceId == null) return new ReadOnlyCollection<ArmedForceRecord>(result);

        foreach (ArmedForceRecord force in forcesById.Values)
        {
            if (force.ParentForceId == parentForceId) result.Add(force);
        }

        result.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<ArmedForceRecord>(result);
    }

    public IReadOnlyList<ArmedForceRecord> GetHierarchyPreOrder(ArmedForceId rootForceId)
    {
        List<ArmedForceRecord> result = new List<ArmedForceRecord>();
        if (rootForceId == null || forcesById.ContainsKey(rootForceId.Value) == false)
        {
            return new ReadOnlyCollection<ArmedForceRecord>(result);
        }

        AppendHierarchy(rootForceId, result, new HashSet<string>(StringComparer.Ordinal));
        return new ReadOnlyCollection<ArmedForceRecord>(result);
    }

    public ArmedForceInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();

        foreach (ArmedForceRecord force in Forces)
        {
            if (force == null || force.Id == null)
            {
                violations.Add("Force record has no stable identity.");
                continue;
            }

            if (force.ParentForceId == force.Id)
            {
                violations.Add("Force " + force.Id.Value + " is its own parent.");
            }

            if (force.ParentForceId != null
                && forcesById.TryGetValue(force.ParentForceId.Value, out ArmedForceRecord parent) == false)
            {
                violations.Add("Force " + force.Id.Value + " has a missing parent.");
            }
            else if (force.IsActive
                && force.ParentForceId != null
                && forcesById[force.ParentForceId.Value].IsActive == false)
            {
                violations.Add("Active force " + force.Id.Value + " has a terminated parent.");
            }

            if (force.IsDetached && force.ParentForceId == null)
            {
                violations.Add("Detached force " + force.Id.Value + " has no parent.");
            }

            if (force.CommanderPersonId != null
                && personStore.TryGet(force.CommanderPersonId, out _) == false)
            {
                violations.Add("Force " + force.Id.Value + " has an unregistered commander PersonId.");
            }

            if (ContainsParentCycle(force.Id))
            {
                violations.Add("Force " + force.Id.Value + " participates in a parent cycle.");
            }
        }

        foreach (ContingentRecord contingent in Contingents)
        {
            if (contingent == null || contingent.Id == null || contingent.ForceId == null)
            {
                violations.Add("Contingent record has incomplete identity.");
            }
            else if (forcesById.ContainsKey(contingent.ForceId.Value) == false)
            {
                violations.Add("Contingent " + contingent.Id.Value + " has a missing force.");
            }
        }

        foreach (ArmedForcePersonReference reference in RelevantPersons)
        {
            if (reference == null || reference.Id == null || reference.ForceId == null || reference.PersonId == null)
            {
                violations.Add("Relevant Person reference has incomplete identity.");
                continue;
            }

            if (forcesById.ContainsKey(reference.ForceId.Value) == false)
            {
                violations.Add("Relevant Person reference " + reference.Id.Value + " has a missing force.");
            }

            if (personStore.TryGet(reference.PersonId, out _) == false)
            {
                violations.Add("Relevant Person reference " + reference.Id.Value + " has a missing PersonId.");
            }
        }

        return new ArmedForceInvariantReport(violations);
    }

    private bool TryResolveActiveForce(
        ArmedForceId forceId,
        out ArmedForceRecord force,
        out ArmedForceFoundationFailure failure)
    {
        force = null;
        if (forceId == null || forcesById.TryGetValue(forceId.Value, out force) == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceNotRegistered,
                "The ArmedForceId is not registered.",
                out failure);
        }

        if (force.IsActive == false)
        {
            return Fail(
                ArmedForceFoundationFailureCode.ForceTerminated,
                "The armed force is terminated.",
                out failure);
        }

        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    private bool CanAdvanceRevision(out ArmedForceFoundationFailure failure)
    {
        if (revision == long.MaxValue)
        {
            return Fail(
                ArmedForceFoundationFailureCode.RevisionOverflow,
                "The ArmedForceStore revision cannot advance further.",
                out failure);
        }

        failure = ArmedForceFoundationFailure.None;
        return true;
    }

    private bool WouldCreateCycle(ArmedForceId forceId, ArmedForceId candidateParentId)
    {
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        ArmedForceId currentId = candidateParentId;
        while (currentId != null)
        {
            if (currentId == forceId) return true;
            if (visited.Add(currentId.Value) == false) return true;
            if (forcesById.TryGetValue(currentId.Value, out ArmedForceRecord current) == false)
            {
                return false;
            }

            currentId = current.ParentForceId;
        }

        return false;
    }

    private bool ContainsParentCycle(ArmedForceId forceId)
    {
        if (forceId == null) return false;

        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        ArmedForceId currentId = forceId;
        while (currentId != null)
        {
            if (visited.Add(currentId.Value) == false) return true;
            if (forcesById.TryGetValue(currentId.Value, out ArmedForceRecord current) == false)
            {
                return false;
            }

            currentId = current.ParentForceId;
        }

        return false;
    }

    private void AppendHierarchy(
        ArmedForceId forceId,
        List<ArmedForceRecord> result,
        HashSet<string> visited)
    {
        if (forceId == null
            || visited.Add(forceId.Value) == false
            || forcesById.TryGetValue(forceId.Value, out ArmedForceRecord force) == false)
        {
            return;
        }

        result.Add(force);
        foreach (ArmedForceRecord child in GetChildren(forceId))
        {
            AppendHierarchy(child.Id, result, visited);
        }
    }

    private static int CompareRelevantPersons(
        ArmedForcePersonReference left,
        ArmedForcePersonReference right)
    {
        int comparison = StringComparer.Ordinal.Compare(left.ForceId.Value, right.ForceId.Value);
        if (comparison != 0) return comparison;
        comparison = StringComparer.Ordinal.Compare(left.RoleKey, right.RoleKey);
        if (comparison != 0) return comparison;
        comparison = StringComparer.Ordinal.Compare(left.PersonId.Value, right.PersonId.Value);
        if (comparison != 0) return comparison;
        return StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value);
    }

    private static bool Fail(
        ArmedForceFoundationFailureCode code,
        string message,
        out ArmedForceFoundationFailure failure)
    {
        failure = ArmedForceFoundationFailure.Create(code, message);
        return false;
    }
}
