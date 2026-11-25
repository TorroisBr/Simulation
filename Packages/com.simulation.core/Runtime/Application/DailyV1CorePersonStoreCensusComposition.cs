using System;
using System.Collections.Generic;

internal static class DailyV1CorePersonStoreCensusComposition
{
    internal static bool TryCreateProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        PersonStore owner = graph?.PersonStore;
        if (owner == null)
        {
            diagnostic = "The Core PersonStore census requires its exact selected owner graph.";
            return false;
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers;
        try
        {
            providers = PersonStoreCensusProvider.CreateProviders(owner);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            diagnostic = "The Core PersonStore census provider set could not be created.";
            return false;
        }

        if (providers == null || providers.Count != 2)
        {
            diagnostic = "The Core PersonStore census provider set is missing or incomplete.";
            return false;
        }

        OwnerSectionCensusWitness membership = null;
        OwnerSectionCensusWitness materialization = null;
        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(providers.Count);
        HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);

        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness;
            try
            {
                witness = provider?.GetCurrentCensus();
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is InvalidOperationException
                || exception is OverflowException)
            {
                diagnostic = "A Core PersonStore census provider could not read its live witness.";
                return false;
            }

            if (witness == null
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || !ReferenceEquals(witness.OwnerInstanceIdentity, owner)
                || witness.Cardinality < 0
                || witness.Revision < 0L
                || !observed.Add(witness.SectionId))
            {
                diagnostic = "A Core PersonStore census provider returned an invalid or substituted witness.";
                return false;
            }

            if (string.Equals(
                    witness.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                    StringComparison.Ordinal))
            {
                membership = witness;
            }
            else if (string.Equals(
                         witness.SectionId,
                         DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
                         StringComparison.Ordinal))
            {
                materialization = witness;
            }
            else
            {
                diagnostic = "A Core PersonStore census provider returned a section outside its exact owner pair.";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        if (membership == null || materialization == null
            || !ReferenceEquals(membership.OwnerInstanceIdentity, materialization.OwnerInstanceIdentity)
            || membership.Revision != materialization.Revision)
        {
            diagnostic = "The Core PersonStore census alias pair does not share its exact owner and revision.";
            return false;
        }

        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        registrations = built.AsReadOnly();
        return true;
    }

    internal static bool TryCreateLifeResidenceProviderRegistrations(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out string diagnostic)
    {
        registrations = null;
        diagnostic = null;
        PersonStore owner = graph?.PersonStore;
        if (owner == null || owner.Persons == null)
        {
            diagnostic = "The Core Person life/residence census requires its exact selected owner graph.";
            return false;
        }

        List<DailyV1OwnerSectionCensusProviderRegistration> built =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(owner.Persons.Count);
        HashSet<string> observedIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<PersonRuntime> observedOwners = new HashSet<PersonRuntime>();
        foreach (PersonRuntime person in owner.Persons)
        {
            if (person == null || person.PersonId == null
                || string.IsNullOrWhiteSpace(person.PersonId.Value)
                || !observedOwners.Add(person))
            {
                diagnostic = "The Core PersonStore contains a missing or duplicate life/residence owner.";
                return false;
            }

            string sectionId;
            try
            {
                sectionId = DailyV1SelectedOwnerSectionCensusContract
                    .NpcLifeResidenceSectionIdFor(person.PersonId);
            }
            catch (ArgumentException)
            {
                diagnostic = "A Core PersonStore member has an invalid life/residence census identity.";
                return false;
            }

            IOwnerSectionCensusProvider provider =
                new DailyV1CorePersonLifeResidenceCensusProvider(person);
            OwnerSectionCensusWitness witness;
            try
            {
                witness = provider.GetCurrentCensus();
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException)
            {
                diagnostic = "A Core Person life/residence provider could not read its live witness.";
                return false;
            }

            if (witness == null
                || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion
                || !ReferenceEquals(witness.OwnerInstanceIdentity, person)
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || !observedIds.Add(sectionId))
            {
                diagnostic = "A Core Person life/residence provider returned a duplicate or substituted owner witness.";
                return false;
            }

            built.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                sectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                provider,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision,
                hasBaseline: true));
        }

        built.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        registrations = built.AsReadOnly();
        return true;
    }
}
