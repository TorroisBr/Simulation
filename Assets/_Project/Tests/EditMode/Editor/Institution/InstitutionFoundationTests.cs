using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public sealed class InstitutionFoundationTests
{
    [Test]
    public void InstitutionId_UsesOrdinalValueSemanticsAndRejectsInvalidValues()
    {
        InstitutionId first = new InstitutionId("crown");
        InstitutionId equal = new InstitutionId("crown");
        InstitutionId differentCase = new InstitutionId("CROWN");

        Assert.That(first, Is.EqualTo(equal));
        Assert.That(first == equal, Is.True);
        Assert.That(first != differentCase, Is.True);
        Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        Assert.That(first.ToString(), Is.EqualTo("crown"));
        Assert.Throws<ArgumentException>(() => new InstitutionId("  "));

        Assert.That(InstitutionId.TryCreate(
            "",
            out InstitutionId invalid,
            out InstitutionFoundationFailure failure), Is.False);
        Assert.That(invalid, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.InvalidInstitutionId));
    }

    [Test]
    public void OfficeId_UsesOrdinalValueSemanticsAndRejectsInvalidValues()
    {
        OfficeId first = new OfficeId("crown.king");
        OfficeId equal = new OfficeId("crown.king");

        Assert.That(first.Equals(equal), Is.True);
        Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        Assert.That(OfficeId.TryCreate(
            "\t",
            out OfficeId invalid,
            out InstitutionFoundationFailure failure), Is.False);
        Assert.That(invalid, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.InvalidOfficeId));
    }

    [Test]
    public void InstitutionStore_RegistersOnceAndEnumeratesReadOnlyInStableOrder()
    {
        InstitutionStore store = new InstitutionStore();
        InstitutionRecord zeta = new InstitutionRecord(new InstitutionId("zeta"), "Zeta");
        InstitutionRecord alpha = new InstitutionRecord(new InstitutionId("alpha"), "Alpha");

        Assert.That(store.TryRegister(zeta, out InstitutionFoundationFailure firstFailure), Is.True);
        Assert.That(firstFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        Assert.That(store.TryRegister(alpha, out _), Is.True);
        Assert.That(store.TryRegister(new InstitutionRecord(new InstitutionId("alpha")), out InstitutionFoundationFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(InstitutionFoundationFailureCode.DuplicateInstitutionId));
        Assert.That(store.TryGet(new InstitutionId("zeta"), out InstitutionRecord resolved), Is.True);
        Assert.That(resolved, Is.SameAs(zeta));

        Assert.That(store.Institutions[0].Id.Value, Is.EqualTo("alpha"));
        Assert.That(store.Institutions[1].Id.Value, Is.EqualTo("zeta"));
        IList<InstitutionRecord> readOnly = store.Institutions as IList<InstitutionRecord>;
        Assert.That(readOnly, Is.Not.Null);
        Assert.That(readOnly.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => readOnly.Add(new InstitutionRecord(new InstitutionId("other"))));
    }

    [Test]
    public void InstitutionStore_DuplicateRegistrationDoesNotReplaceExistingRecord()
    {
        InstitutionStore store = new InstitutionStore();
        InstitutionRecord original = new InstitutionRecord(new InstitutionId("guild"), "Original");
        InstitutionRecord replacement = new InstitutionRecord(new InstitutionId("guild"), "Replacement");

        Assert.That(store.TryRegister(original, out _), Is.True);
        Assert.That(store.TryRegister(replacement, out _), Is.False);
        Assert.That(store.TryGet(new InstitutionId("guild"), out InstitutionRecord resolved), Is.True);
        Assert.That(resolved.DisplayName, Is.EqualTo("Original"));
    }

    [Test]
    public void OfficeStore_RejectsMissingInstitutionAndDuplicateOffice()
    {
        InstitutionStore institutions = new InstitutionStore();
        OfficeStore offices = new OfficeStore(institutions);
        OfficeRecord missingParent = new OfficeRecord(
            new OfficeId("missing.office"),
            new InstitutionId("missing"));

        Assert.That(offices.TryRegister(missingParent, out InstitutionFoundationFailure missingFailure), Is.False);
        Assert.That(missingFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.InstitutionNotFoundForOffice));

        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeRecord original = new OfficeRecord(new OfficeId("council.chair"), institutionId, "Chair");
        Assert.That(offices.TryRegister(original, out _), Is.True);
        Assert.That(offices.TryRegister(new OfficeRecord(new OfficeId("council.chair"), institutionId), out InstitutionFoundationFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(InstitutionFoundationFailureCode.DuplicateOfficeId));
    }

    [Test]
    public void NewOffice_IsVacantWithoutFakePersonIdentity()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "council.chair");

        Assert.That(offices.IsVacant(officeId), Is.True);
        Assert.That(offices.TryGetCurrentIncumbent(officeId, out PersonId incumbent), Is.False);
        Assert.That(incumbent, Is.Null);
        Assert.That(offices.TryGetIncumbency(officeId, out OfficeIncumbency state), Is.False);
        Assert.That(state, Is.Null);
        Assert.That(offices.GetVacantOffices(), Has.Count.EqualTo(1));
    }

    [Test]
    public void OfficeStore_AssignsPersonWithOptionalStartDay()
    {
        OfficeStore offices = CreateOfficeStore(out _, out OfficeId officeId);
        PersonId personId = new PersonId("person-mayor");

        Assert.That(offices.TryAssignIncumbent(officeId, personId, 865L, out InstitutionFoundationFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        Assert.That(offices.TryGetIncumbency(officeId, out OfficeIncumbency incumbency), Is.True);
        Assert.That(incumbency.Incumbent, Is.EqualTo(personId));
        Assert.That(incumbency.StartAbsoluteDay, Is.EqualTo(865L));
        Assert.That(offices.IsVacant(officeId), Is.False);
    }

    [Test]
    public void OfficeStore_RejectsNegativeStartDayAndNullIncumbentWithoutMutation()
    {
        OfficeStore offices = CreateOfficeStore(out _, out OfficeId officeId);

        Assert.That(offices.TryAssignIncumbent(officeId, null, 0L, out InstitutionFoundationFailure nullPersonFailure), Is.False);
        Assert.That(nullPersonFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.InvalidPersonId));
        Assert.That(offices.TryAssignIncumbent(officeId, new PersonId("person"), -1L, out InstitutionFoundationFailure negativeDayFailure), Is.False);
        Assert.That(negativeDayFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.InvalidStartAbsoluteDay));
        Assert.That(offices.IsVacant(officeId), Is.True);
    }

    [Test]
    public void OfficeStore_DoubleAssignmentFailsAtomicallyAndPreservesFirstIncumbent()
    {
        OfficeStore offices = CreateOfficeStore(out _, out OfficeId officeId);
        PersonId first = new PersonId("person-first");
        PersonId second = new PersonId("person-second");

        Assert.That(offices.TryAssignIncumbent(officeId, first, 10L, out _), Is.True);
        Assert.That(offices.TryAssignIncumbent(officeId, second, 20L, out InstitutionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.OfficeAlreadyOccupied));
        Assert.That(offices.TryGetIncumbency(officeId, out OfficeIncumbency preserved), Is.True);
        Assert.That(preserved.Incumbent, Is.EqualTo(first));
        Assert.That(preserved.StartAbsoluteDay, Is.EqualTo(10L));
    }

    [Test]
    public void OfficeStore_VacatesWithoutRemovingOfficeOrInstitution()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "council.chair");
        Assert.That(offices.TryAssignIncumbent(officeId, new PersonId("person"), null, out _), Is.True);

        Assert.That(offices.TryVacateOffice(officeId, out InstitutionFoundationFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        Assert.That(offices.TryGet(officeId, out OfficeRecord office), Is.True);
        Assert.That(office.InstitutionId, Is.EqualTo(institutionId));
        Assert.That(institutions.TryGet(institutionId, out InstitutionRecord institution), Is.True);
        Assert.That(institution.Id, Is.EqualTo(institutionId));
        Assert.That(offices.IsVacant(officeId), Is.True);
    }

    [Test]
    public void OfficeStore_AlreadyVacantIsAnExplicitExpectedFailure()
    {
        OfficeStore offices = CreateOfficeStore(out _, out OfficeId officeId);

        Assert.That(offices.TryVacateOffice(officeId, out InstitutionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.OfficeAlreadyVacant));
        Assert.That(offices.TryGet(officeId, out _), Is.True);
    }

    [Test]
    public void OnePersonMayHoldMultipleOfficesAndQueriesAreDeterministic()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId lower = RegisterOffice(offices, institutionId, "council.a");
        OfficeId higher = RegisterOffice(offices, institutionId, "council.z");
        PersonId personId = new PersonId("person-multiple");

        Assert.That(offices.TryAssignIncumbent(higher, personId, null, out _), Is.True);
        Assert.That(offices.TryAssignIncumbent(lower, personId, null, out _), Is.True);

        IReadOnlyList<OfficeRecord> held = offices.GetOfficesHeldBy(new PersonId("person-multiple"));
        Assert.That(held, Has.Count.EqualTo(2));
        Assert.That(held[0].Id, Is.EqualTo(lower));
        Assert.That(held[1].Id, Is.EqualTo(higher));
    }

    [Test]
    public void OfficeQueriesSupportMultipleInstitutionsAndVacancies()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId firstInstitution = RegisterInstitution(institutions, "first");
        InstitutionId secondInstitution = RegisterInstitution(institutions, "second");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId firstB = RegisterOffice(offices, firstInstitution, "first.b");
        RegisterOffice(offices, firstInstitution, "first.a");
        OfficeId secondOffice = RegisterOffice(offices, secondInstitution, "second.a");
        Assert.That(offices.TryAssignIncumbent(firstB, new PersonId("person-a"), null, out _), Is.True);

        IReadOnlyList<OfficeRecord> firstOffices = offices.GetOfficesForInstitution(firstInstitution);
        Assert.That(firstOffices, Has.Count.EqualTo(2));
        Assert.That(firstOffices[0].Id.Value, Is.EqualTo("first.a"));
        Assert.That(firstOffices[1].Id.Value, Is.EqualTo("first.b"));
        Assert.That(offices.GetOfficesForInstitution(secondInstitution), Has.Count.EqualTo(1));
        Assert.That(offices.GetOfficesForInstitution(new InstitutionId("unknown")), Is.Empty);
        Assert.That(offices.GetVacantOffices(), Has.Count.EqualTo(2));
        Assert.That(offices.GetVacantOffices()[0].Id, Is.Not.EqualTo(firstB));
        Assert.That(offices.GetVacantOffices()[1].Id, Is.EqualTo(secondOffice));
    }

    [Test]
    public void SeparateStoresWithSameIdsRemainWorldIsolated()
    {
        InstitutionStore firstWorldInstitutions = new InstitutionStore();
        InstitutionStore secondWorldInstitutions = new InstitutionStore();
        InstitutionId firstId = RegisterInstitution(firstWorldInstitutions, "shared-id");
        InstitutionId secondId = RegisterInstitution(secondWorldInstitutions, "shared-id");
        OfficeStore firstWorldOffices = new OfficeStore(firstWorldInstitutions);
        OfficeStore secondWorldOffices = new OfficeStore(secondWorldInstitutions);
        OfficeId firstOffice = RegisterOffice(firstWorldOffices, firstId, "shared.office");
        OfficeId secondOffice = RegisterOffice(secondWorldOffices, secondId, "shared.office");

        Assert.That(firstWorldOffices.TryAssignIncumbent(firstOffice, new PersonId("first-person"), null, out _), Is.True);
        Assert.That(firstWorldOffices.IsVacant(firstOffice), Is.False);
        Assert.That(secondWorldOffices.IsVacant(secondOffice), Is.True);
        Assert.That(secondWorldOffices.TryGetCurrentIncumbent(secondOffice, out _), Is.False);
    }

    [Test]
    public void QueryingAndReadOnlySnapshotsDoNotMutateStoreState()
    {
        OfficeStore offices = CreateOfficeStore(out _, out OfficeId officeId);
        PersonId personId = new PersonId("person-stable");
        Assert.That(offices.TryAssignIncumbent(officeId, personId, 100L, out _), Is.True);

        IReadOnlyList<OfficeRecord> officesBefore = offices.Offices;
        IReadOnlyList<OfficeIncumbency> incumbenciesBefore = offices.Incumbencies;
        IReadOnlyList<OfficeRecord> held = offices.GetOfficesHeldBy(new PersonId("person-stable"));

        Assert.That(held, Has.Count.EqualTo(1));
        Assert.That(offices.Offices, Has.Count.EqualTo(officesBefore.Count));
        Assert.That(offices.Incumbencies, Has.Count.EqualTo(incumbenciesBefore.Count));
        Assert.That(offices.TryGetIncumbency(officeId, out OfficeIncumbency current), Is.True);
        Assert.That(current, Is.EqualTo(incumbenciesBefore[0]));
    }

    [Test]
    public void CoreInstitutionFilesAreUnityFreeAndDoNotReferenceRuntimeAdapters()
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets",
            "_Project",
            "Scripts",
            "Institution");

        string[] files = Directory.GetFiles(directory, "*.cs");
        Assert.That(files, Is.Not.Empty);
        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            Assert.That(source, Does.Not.Contain("using UnityEngine;"), file);
            Assert.That(source, Does.Not.Contain("NpcRuntime"), file);
            Assert.That(source, Does.Not.Contain("SimulationRuntime"), file);
            Assert.That(source, Does.Not.Contain("PersonStore"), file);
            Assert.That(source, Does.Not.Contain("DateTime.Now"), file);
        }
    }

    [Test]
    public void CoreContractsExposeReadOnlyPropertiesWithoutPublicSetters()
    {
        Type[] types =
        {
            typeof(InstitutionRecord),
            typeof(OfficeRecord),
            typeof(OfficeIncumbency),
            typeof(InstitutionFoundationFailure)
        };

        foreach (Type type in types)
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(property.CanWrite, Is.False, type.Name + "." + property.Name);
            }
        }
    }

    private static InstitutionId RegisterInstitution(InstitutionStore store, string value)
    {
        InstitutionId id = new InstitutionId(value);
        Assert.That(store.TryRegister(new InstitutionRecord(id), out InstitutionFoundationFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        return id;
    }

    private static OfficeId RegisterOffice(OfficeStore store, InstitutionId institutionId, string value)
    {
        OfficeId id = new OfficeId(value);
        Assert.That(store.TryRegister(new OfficeRecord(id, institutionId), out InstitutionFoundationFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        return id;
    }

    private static OfficeStore CreateOfficeStore(
        out InstitutionStore institutions,
        out OfficeId officeId)
    {
        institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "institution");
        OfficeStore offices = new OfficeStore(institutions);
        officeId = RegisterOffice(offices, institutionId, "institution.office");
        return offices;
    }
}
