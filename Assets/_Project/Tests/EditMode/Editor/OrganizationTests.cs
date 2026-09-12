using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class OrganizationTests
{
    private readonly List<UnityEngine.Object> createdDefinitions = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdDefinitions.Count - 1; i >= 0; i--)
        {
            if (createdDefinitions[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdDefinitions[i]);
            }
        }

        createdDefinitions.Clear();
    }

    [Test]
    public void RuntimeIdAllocator_OrganizationIdsUseStableNamespace()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateOrganizationId(), Is.EqualTo("organization-000001"));
        Assert.That(allocator.AllocateOrganizationId(), Is.EqualTo("organization-000002"));
    }

    [Test]
    public void DistinctOrganizationsHaveDistinctRuntimeIds()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        OrganizationData definition = CreateDefinition("guild-definition", "Guild");

        OrganizationRuntime first = new OrganizationRuntime(allocator, definition);
        OrganizationRuntime second = new OrganizationRuntime(allocator, definition);

        Assert.That(first.RuntimeId, Is.Not.EqualTo(second.RuntimeId));
    }

    [Test]
    public void DefinitionIdAndRuntimeIdAreDifferentIdentities()
    {
        OrganizationData definition = CreateDefinition("guild-definition", "Guild");
        OrganizationRuntime organization = new OrganizationRuntime("organization-runtime", definition);

        Assert.That(organization.DefinitionId, Is.EqualTo("guild-definition"));
        Assert.That(organization.RuntimeId, Is.EqualTo("organization-runtime"));
        Assert.That(organization.DefinitionId, Is.Not.EqualTo(organization.RuntimeId));
        Assert.That(organization.DisplayName, Is.EqualTo("Guild"));
    }

    [Test]
    public void AddMemberWorks()
    {
        OrganizationRuntime organization = CreateOrganization();

        Assert.That(organization.AddMember("npc-000001", OrganizationRole.Leader), Is.True);
        Assert.That(organization.Members.Count, Is.EqualTo(1));
        Assert.That(organization.Members[0].NpcRuntimeId, Is.EqualTo("npc-000001"));
        Assert.That(organization.Members[0].Role, Is.EqualTo(OrganizationRole.Leader));
    }

    [Test]
    public void SameNpcCannotAppearTwiceInOrganization()
    {
        OrganizationRuntime organization = CreateOrganization();

        Assert.That(organization.AddMember("npc-000001"), Is.True);
        Assert.That(organization.AddMember("npc-000001", OrganizationRole.Leader), Is.False);
        Assert.That(organization.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public void TwoDifferentNpcsCanBeMembers()
    {
        OrganizationRuntime organization = CreateOrganization();

        Assert.That(organization.AddMember("npc-000001"), Is.True);
        Assert.That(organization.AddMember("npc-000002"), Is.True);
        Assert.That(organization.Members.Count, Is.EqualTo(2));
    }

    [Test]
    public void RemoveMemberWorks()
    {
        OrganizationRuntime organization = CreateOrganization();
        organization.AddMember("npc-000001");
        organization.AddMember("npc-000002");

        Assert.That(organization.RemoveMember("npc-000001"), Is.True);
        Assert.That(organization.ContainsMember("npc-000001"), Is.False);
        Assert.That(organization.Members.Count, Is.EqualTo(1));
        Assert.That(organization.Members[0].NpcRuntimeId, Is.EqualTo("npc-000002"));
    }

    [Test]
    public void ChangeRoleWorksWithoutChangingMembershipOrder()
    {
        OrganizationRuntime organization = CreateOrganization();
        organization.AddMember("npc-000001");
        organization.AddMember("npc-000002", OrganizationRole.Leader);

        Assert.That(organization.ChangeRole("npc-000001", OrganizationRole.Leader), Is.True);
        Assert.That(organization.Members[0].NpcRuntimeId, Is.EqualTo("npc-000001"));
        Assert.That(organization.Members[0].Role, Is.EqualTo(OrganizationRole.Leader));
        Assert.That(organization.Members[1].NpcRuntimeId, Is.EqualTo("npc-000002"));
    }

    [Test]
    public void QueriesDoNotMutateOrganization()
    {
        OrganizationRuntime organization = CreateOrganization();
        organization.AddMember("npc-000001");
        int countBeforeQuery = organization.Members.Count;

        Assert.That(organization.ContainsMember("npc-000001"), Is.True);
        Assert.That(organization.GetMembershipForNpc("npc-absent"), Is.Null);
        Assert.That(organization.Members.Count, Is.EqualTo(countBeforeQuery));
        Assert.Throws<NotSupportedException>(() => ((IList<OrganizationMembership>)organization.Members).Add(
            new OrganizationMembership("npc-000002", OrganizationRole.Member)));
    }

    [Test]
    public void StoreResolvesByRuntimeId()
    {
        OrganizationRuntime organization = CreateOrganization();
        OrganizationStore store = new OrganizationStore();

        Assert.That(store.AddOrganization(organization), Is.True);
        Assert.That(store.GetByRuntimeId(organization.RuntimeId), Is.SameAs(organization));
    }

    [Test]
    public void StoreRejectsDuplicateOrganizationRuntimeId()
    {
        OrganizationStore store = new OrganizationStore();
        OrganizationRuntime first = CreateOrganization("organization-000001");
        OrganizationRuntime second = CreateOrganization("organization-000001");

        Assert.That(store.AddOrganization(first), Is.True);
        Assert.That(store.AddOrganization(second), Is.False);
        Assert.That(store.Organizations.Count, Is.EqualTo(1));
    }

    [Test]
    public void StoreRemovesOrganizationByRuntimeId()
    {
        OrganizationStore store = new OrganizationStore();
        OrganizationRuntime organization = CreateOrganization();
        store.AddOrganization(organization);

        Assert.That(store.RemoveOrganization(organization.RuntimeId), Is.True);
        Assert.That(store.GetByRuntimeId(organization.RuntimeId), Is.Null);
        Assert.That(store.Organizations.Count, Is.EqualTo(0));
    }

    [Test]
    public void StoreGetsOrganizationsForNpc()
    {
        OrganizationStore store = new OrganizationStore();
        OrganizationRuntime first = CreateOrganization("organization-000001");
        OrganizationRuntime second = CreateOrganization("organization-000002");
        first.AddMember("npc-000001");
        second.AddMember("npc-000001");
        second.AddMember("npc-000002");
        store.AddOrganization(first);
        store.AddOrganization(second);

        IReadOnlyList<OrganizationRuntime> organizations = store.GetOrganizationsForNpc("npc-000001");

        Assert.That(organizations.Count, Is.EqualTo(2));
        Assert.That(organizations[0], Is.SameAs(first));
        Assert.That(organizations[1], Is.SameAs(second));
    }

    [Test]
    public void MembershipOrderIsDeterministic()
    {
        OrganizationRuntime organization = CreateOrganization();
        organization.AddMember("npc-000003");
        organization.AddMember("npc-000001");
        organization.AddMember("npc-000002");
        organization.ChangeRole("npc-000001", OrganizationRole.Leader);

        Assert.That(organization.Members[0].NpcRuntimeId, Is.EqualTo("npc-000003"));
        Assert.That(organization.Members[1].NpcRuntimeId, Is.EqualTo("npc-000001"));
        Assert.That(organization.Members[2].NpcRuntimeId, Is.EqualTo("npc-000002"));
    }

    [Test]
    public void OrganizationCanBeCreatedWithoutTravelParty()
    {
        OrganizationRuntime organization = new OrganizationRuntime(
            new RuntimeIdAllocator(),
            CreateDefinition("organization-definition", "Organization"));

        Assert.That(organization, Is.Not.Null);
        Assert.That(organization.RuntimeId, Does.StartWith("organization-"));
    }

    [Test]
    public void OrganizationContinuesExistingWithoutActiveTravelParty()
    {
        OrganizationStore store = new OrganizationStore();
        OrganizationRuntime organization = CreateOrganization();
        organization.AddMember("npc-000001");

        store.AddOrganization(organization);

        Assert.That(store.GetByRuntimeId(organization.RuntimeId), Is.SameAs(organization));
        Assert.That(store.GetOrganizationsForNpc("npc-000001").Count, Is.EqualTo(1));
    }

    [Test]
    public void EmptyIdsAreRejected()
    {
        OrganizationData definition = CreateDefinition("organization-definition", "Organization");

        Assert.Throws<ArgumentException>(() => new OrganizationRuntime(string.Empty, definition));
        Assert.Throws<ArgumentException>(() => new OrganizationMembership(" ", OrganizationRole.Member));

        OrganizationRuntime organization = new OrganizationRuntime("organization-runtime", definition);
        Assert.That(organization.AddMember(string.Empty), Is.False);
        Assert.That(organization.AddMember("npc-runtime", (OrganizationRole)999), Is.False);
        Assert.That(organization.ChangeRole("npc-runtime", (OrganizationRole)999), Is.False);
    }

    private OrganizationRuntime CreateOrganization(string runtimeId = "organization-runtime")
    {
        return new OrganizationRuntime(runtimeId, CreateDefinition("organization-definition", "Organization"));
    }

    private OrganizationData CreateDefinition(string id, string displayName)
    {
        OrganizationData definition = ScriptableObject.CreateInstance<OrganizationData>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.id = id;
        definition.displayName = displayName;
        createdDefinitions.Add(definition);
        return definition;
    }
}
