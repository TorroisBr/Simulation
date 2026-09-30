using System;
using System.Collections.Generic;

internal enum OfficeCensusSection
{
    Offices,
    Incumbencies,
    Tenures
}

/// <summary>Fixed passive census providers for installed Institution and Office owners.</summary>
public static class InstitutionOfficeCensusProvider
{
    public const int SchemaVersion = 1;
    public const string InstitutionsSectionId = "p12e.institution.records";
    public const string OfficesSectionId = "p12e.office.records";
    public const string IncumbenciesSectionId = "p12e.office.incumbencies";
    public const string TenuresSectionId = "p12e.office.tenures";

    private sealed class InstitutionSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly InstitutionStore owner;

        public InstitutionSectionProvider(InstitutionStore owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return new OwnerSectionCensusWitness(
                InstitutionsSectionId,
                SchemaVersion,
                owner,
                owner.Count,
                owner.Revision);
        }
    }

    private sealed class OfficeSectionProvider : IOwnerSectionCensusProvider
    {
        private readonly OfficeStore owner;
        private readonly OfficeCensusSection section;
        private readonly string sectionId;

        public OfficeSectionProvider(
            OfficeStore owner,
            OfficeCensusSection section,
            string sectionId)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.section = section;
            this.sectionId = sectionId;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            int cardinality;
            switch (section)
            {
                case OfficeCensusSection.Offices:
                    cardinality = owner.Count;
                    break;
                case OfficeCensusSection.Incumbencies:
                    cardinality = owner.IncumbencyCount;
                    break;
                case OfficeCensusSection.Tenures:
                    cardinality = owner.TenureCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(section));
            }

            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                cardinality,
                owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        InstitutionStore institutionOwner,
        OfficeStore officeOwner)
    {
        if (institutionOwner == null) throw new ArgumentNullException(nameof(institutionOwner));
        if (officeOwner == null) throw new ArgumentNullException(nameof(officeOwner));
        if (!ReferenceEquals(officeOwner.InstitutionStoreForWorldBoundary, institutionOwner))
        {
            throw new ArgumentException(
                "The Office census owner must belong to the supplied installed Institution owner.",
                nameof(officeOwner));
        }

        IOwnerSectionCensusProvider[] providers =
        {
            new InstitutionSectionProvider(institutionOwner),
            new OfficeSectionProvider(officeOwner, OfficeCensusSection.Offices, OfficesSectionId),
            new OfficeSectionProvider(officeOwner, OfficeCensusSection.Incumbencies, IncumbenciesSectionId),
            new OfficeSectionProvider(officeOwner, OfficeCensusSection.Tenures, TenuresSectionId)
        };
        return Array.AsReadOnly(providers);
    }
}
