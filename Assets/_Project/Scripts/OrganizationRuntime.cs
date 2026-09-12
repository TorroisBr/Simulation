using System;
using System.Collections.Generic;

public enum OrganizationRole
{
    Member,
    Leader
}

// Leader uniqueness is intentionally not enforced in this foundation; future rules can add it without changing membership identity.

[Serializable]
public sealed class OrganizationMembership
{
    private readonly string npcRuntimeId;
    private readonly OrganizationRole role;

    public string NpcRuntimeId => npcRuntimeId;
    public OrganizationRole Role => role;

    public OrganizationMembership(string npcRuntimeId, OrganizationRole role)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            throw new ArgumentException("Organization membership requires a non-empty NPC RuntimeId.", nameof(npcRuntimeId));
        }

        if (OrganizationRuntime.IsValidRole(role) == false)
        {
            throw new ArgumentException("Organization membership requires a valid role.", nameof(role));
        }

        this.npcRuntimeId = npcRuntimeId;
        this.role = role;
    }
}

[Serializable]
public sealed class OrganizationRuntime
{
    private readonly string runtimeId;
    private readonly OrganizationData organizationData;
    private readonly List<OrganizationMembership> members = new List<OrganizationMembership>();
    private readonly Dictionary<string, OrganizationMembership> membershipsByNpcRuntimeId =
        new Dictionary<string, OrganizationMembership>(StringComparer.Ordinal);
    private readonly IReadOnlyList<OrganizationMembership> readOnlyMembers;

    public string RuntimeId => runtimeId;
    public OrganizationData OrganizationData => organizationData;
    public string DefinitionId => organizationData != null ? organizationData.DefinitionId : string.Empty;
    public string DisplayName => organizationData != null ? organizationData.DisplayName : string.Empty;
    public IReadOnlyList<OrganizationMembership> Members => readOnlyMembers;
    public IReadOnlyList<OrganizationMembership> Memberships => readOnlyMembers;

    public OrganizationRuntime(string runtimeId, OrganizationData organizationData)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("OrganizationRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
        this.organizationData = organizationData;
        readOnlyMembers = members.AsReadOnly();
    }

    public OrganizationRuntime(RuntimeIdAllocator idAllocator, OrganizationData organizationData)
        : this(
            (idAllocator ?? throw new ArgumentNullException(nameof(idAllocator))).AllocateOrganizationId(),
            organizationData)
    {
    }

    public bool AddMember(string npcRuntimeId, OrganizationRole role = OrganizationRole.Member)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true
            || IsValidRole(role) == false
            || membershipsByNpcRuntimeId.ContainsKey(npcRuntimeId) == true)
        {
            return false;
        }

        OrganizationMembership membership = new OrganizationMembership(npcRuntimeId, role);
        members.Add(membership);
        membershipsByNpcRuntimeId.Add(npcRuntimeId, membership);
        return true;
    }

    public bool AddMember(OrganizationMembership membership)
    {
        if (membership == null)
        {
            return false;
        }

        return AddMember(membership.NpcRuntimeId, membership.Role);
    }

    public bool RemoveMember(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true
            || membershipsByNpcRuntimeId.TryGetValue(npcRuntimeId, out OrganizationMembership membership) == false)
        {
            return false;
        }

        membershipsByNpcRuntimeId.Remove(npcRuntimeId);
        members.Remove(membership);
        return true;
    }

    public bool ChangeRole(string npcRuntimeId, OrganizationRole role)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true
            || IsValidRole(role) == false
            || membershipsByNpcRuntimeId.TryGetValue(npcRuntimeId, out OrganizationMembership currentMembership) == false)
        {
            return false;
        }

        if (currentMembership.Role == role)
        {
            return true;
        }

        OrganizationMembership replacement = new OrganizationMembership(npcRuntimeId, role);
        int memberIndex = members.IndexOf(currentMembership);
        members[memberIndex] = replacement;
        membershipsByNpcRuntimeId[npcRuntimeId] = replacement;
        return true;
    }

    public bool ContainsMember(string npcRuntimeId)
    {
        return string.IsNullOrWhiteSpace(npcRuntimeId) == false
            && membershipsByNpcRuntimeId.ContainsKey(npcRuntimeId);
    }

    public bool TryGetMembership(string npcRuntimeId, out OrganizationMembership membership)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == false
            && membershipsByNpcRuntimeId.TryGetValue(npcRuntimeId, out membership) == true)
        {
            return true;
        }

        membership = null;
        return false;
    }

    public OrganizationMembership GetMembershipForNpc(string npcRuntimeId)
    {
        return TryGetMembership(npcRuntimeId, out OrganizationMembership membership) == true
            ? membership
            : null;
    }

    internal static bool IsValidRole(OrganizationRole role)
    {
        return role == OrganizationRole.Member || role == OrganizationRole.Leader;
    }
}
