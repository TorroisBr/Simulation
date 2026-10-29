using System;
using System.Collections.Generic;

internal enum ArmedForceStoreCensusSection
{
    Forces,
    Contingents,
    RelevantPersonReferences
}

/// <summary>Fixed passive census providers for an installed ArmedForceStore.</summary>
public static class ArmedForceStoreCensusProvider
{
    public const int SchemaVersion = 1;
    public const string ForcesSectionId = "p12e.armed-force.forces";
    public const string ContingentsSectionId = "p12e.armed-force.contingents";
    public const string RelevantPersonReferencesSectionId = "p12e.armed-force.relevant-person-references";

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly ArmedForceStore owner;
        private readonly ArmedForceStoreCensusSection section;
        private readonly string sectionId;

        public SectionProvider(ArmedForceStore owner, ArmedForceStoreCensusSection section, string sectionId)
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
                case ArmedForceStoreCensusSection.Forces:
                    cardinality = owner.Count;
                    break;
                case ArmedForceStoreCensusSection.Contingents:
                    cardinality = owner.ContingentCount;
                    break;
                case ArmedForceStoreCensusSection.RelevantPersonReferences:
                    cardinality = owner.RelevantPersonCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(section));
            }

            return new OwnerSectionCensusWitness(sectionId, SchemaVersion, owner, cardinality, owner.Revision);
        }
    }

    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(ArmedForceStore owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        IOwnerSectionCensusProvider[] providers =
        {
            new SectionProvider(owner, ArmedForceStoreCensusSection.Forces, ForcesSectionId),
            new SectionProvider(owner, ArmedForceStoreCensusSection.Contingents, ContingentsSectionId),
            new SectionProvider(owner, ArmedForceStoreCensusSection.RelevantPersonReferences, RelevantPersonReferencesSectionId)
        };
        return Array.AsReadOnly(providers);
    }
}
