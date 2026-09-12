using System;
using System.Collections.Generic;

public sealed class OrganizationStore
{
    private readonly List<OrganizationRuntime> organizations = new List<OrganizationRuntime>();
    private readonly Dictionary<string, OrganizationRuntime> organizationsByRuntimeId =
        new Dictionary<string, OrganizationRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<OrganizationRuntime> readOnlyOrganizations;

    public IReadOnlyList<OrganizationRuntime> Organizations => readOnlyOrganizations;

    public OrganizationStore()
    {
        readOnlyOrganizations = organizations.AsReadOnly();
    }

    public OrganizationRuntime GetByRuntimeId(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && organizationsByRuntimeId.TryGetValue(runtimeId, out OrganizationRuntime organization) == true
            ? organization
            : null;
    }

    public bool AddOrganization(OrganizationRuntime organization)
    {
        if (organization == null
            || string.IsNullOrWhiteSpace(organization.RuntimeId) == true
            || organizationsByRuntimeId.ContainsKey(organization.RuntimeId) == true)
        {
            return false;
        }

        organizationsByRuntimeId.Add(organization.RuntimeId, organization);
        organizations.Add(organization);
        return true;
    }

    public bool RemoveOrganization(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true
            || organizationsByRuntimeId.TryGetValue(runtimeId, out OrganizationRuntime organization) == false)
        {
            return false;
        }

        organizationsByRuntimeId.Remove(runtimeId);
        organizations.Remove(organization);
        return true;
    }

    public IReadOnlyList<OrganizationRuntime> GetOrganizationsForNpc(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return Array.Empty<OrganizationRuntime>();
        }

        List<OrganizationRuntime> result = new List<OrganizationRuntime>();

        foreach (OrganizationRuntime organization in organizations)
        {
            if (organization != null && organization.ContainsMember(npcRuntimeId) == true)
            {
                result.Add(organization);
            }
        }

        return result.AsReadOnly();
    }
}
