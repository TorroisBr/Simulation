using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal sealed class P12FPoliticalKnowledgeOwnerSnapshot
{
    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> vector;
    private readonly List<PoliticalKnowledgeRuntime> detachedRuntimes;

    internal int Cardinality => detachedRuntimes.Count;
    internal long Revision { get; }

    private P12FPoliticalKnowledgeOwnerSnapshot(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        List<PoliticalKnowledgeRuntime> runtimes,
        long revision)
    {
        this.token = token;
        this.vector = vector;
        detachedRuntimes = runtimes;
        Revision = revision;
    }

    /// <summary>Creates detached Political Knowledge values decoded from a continuation image.</summary>
    internal P12FPoliticalKnowledgeOwnerSnapshot(
        IEnumerable<PoliticalKnowledgeRuntime> runtimes,
        long revision)
    {
        if (runtimes == null) throw new ArgumentNullException(nameof(runtimes));
        token = null;
        vector = null;
        detachedRuntimes = new List<PoliticalKnowledgeRuntime>();
        foreach (PoliticalKnowledgeRuntime runtime in runtimes)
            detachedRuntimes.Add(runtime == null ? null : runtime.Clone());
        Revision = revision;
    }

    internal IReadOnlyList<PoliticalKnowledgeRuntime> CopyDetachedRuntimes()
    {
        List<PoliticalKnowledgeRuntime> copies = new List<PoliticalKnowledgeRuntime>(detachedRuntimes.Count);
        foreach (PoliticalKnowledgeRuntime runtime in detachedRuntimes)
        {
            if (runtime == null) return null;
            copies.Add(runtime.Clone());
        }
        return new ReadOnlyCollection<PoliticalKnowledgeRuntime>(copies);
    }

    internal static bool TryCapture(
        PoliticalKnowledgeStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactVector,
        out P12FPoliticalKnowledgeOwnerSnapshot snapshot,
        out string failure)
    {
        snapshot = null;
        failure = null;
        if (owner == null || token == null || exactVector == null
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L
            || !P12FStandaloneOwnerSnapshotValidation.TryGetRequiredSection(
                token, exactVector, PoliticalKnowledgeStoreCensusProvider.SectionId,
                PoliticalKnowledgeStoreCensusProvider.SchemaVersion, owner, owner.Count, owner.Revision, out failure))
        {
            failure = failure ?? "Political Knowledge requires its exact Required Daily-v1 owner witness.";
            return false;
        }

        int count = owner.Count;
        long revision = owner.Revision;
        IReadOnlyList<PoliticalKnowledgeRuntime> liveRows = owner.Runtimes;
        if (liveRows == null || liveRows.Count != count || revision < 0L)
        {
            failure = "Political Knowledge rows, cardinality, or local revision are inconsistent.";
            return false;
        }

        List<PoliticalKnowledgeRuntime> rows = new List<PoliticalKnowledgeRuntime>(count);
        string previous = null;
        foreach (PoliticalKnowledgeRuntime row in liveRows)
        {
            if (row == null || row.Holder == null || string.IsNullOrWhiteSpace(row.Holder.StableId)
                || (previous != null && StringComparer.Ordinal.Compare(previous, row.Holder.StableId) >= 0))
            {
                failure = "Political Knowledge holder identities must be valid, unique, and deterministically ordered.";
                return false;
            }
            previous = row.Holder.StableId;
            rows.Add(row.Clone());
        }

        if (owner.Count != count || owner.Revision != revision
            || !P12FStandaloneOwnerSnapshotValidation.TryGetRequiredSection(
                token, exactVector, PoliticalKnowledgeStoreCensusProvider.SectionId,
                PoliticalKnowledgeStoreCensusProvider.SchemaVersion, owner, count, revision, out failure))
        {
            failure = failure ?? "Political Knowledge changed while its detached values were captured.";
            return false;
        }

        snapshot = new P12FPoliticalKnowledgeOwnerSnapshot(token, exactVector, rows, revision);
        return true;
    }

    internal bool TryStage(
        DailyCaptureEligibilityToken stagedToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> stagedVector,
        PersonStore persons,
        InstitutionStore institutions,
        PoliticalClaimStore claims,
        FactionStore factions,
        OfficeStore offices,
        PropertyOwnershipStore properties,
        long stagedAbsoluteDay,
        out PoliticalKnowledgeStore staged,
        out string failure)
    {
        staged = null;
        failure = null;
        if (!ReferenceEquals(token, stagedToken) || !ReferenceEquals(vector, stagedVector)
            || stagedToken == null || stagedAbsoluteDay != stagedToken.AbsoluteDay)
        {
            failure = "Political Knowledge staging must use the same boundary token/vector and captured AbsoluteDay.";
            return false;
        }

        IReadOnlyList<PoliticalKnowledgeRuntime> runtimes = CopyDetachedRuntimes();
        if (runtimes == null)
        {
            failure = "Political Knowledge detached values contain a null owner row.";
            return false;
        }

        if (!PoliticalKnowledgeStore.TryCreateP12FStaged(
            persons, institutions, claims, factions, offices, properties,
            runtimes, Revision, stagedAbsoluteDay, out staged))
        {
            failure = "Political Knowledge values could not be privately staged against the supplied roots and day.";
            return false;
        }
        return true;
    }

    /// <summary>Stages decoded values against fresh target roots without source capture evidence.</summary>
    internal bool TryStageForImage(
        PersonStore persons,
        InstitutionStore institutions,
        PoliticalClaimStore claims,
        FactionStore factions,
        OfficeStore offices,
        PropertyOwnershipStore properties,
        long stagedAbsoluteDay,
        out PoliticalKnowledgeStore staged,
        out string failure)
    {
        staged = null;
        failure = null;
        if (token != null || vector != null || stagedAbsoluteDay < 0L)
        {
            failure = "Detached Political Knowledge staging requires image values and target roots only.";
            return false;
        }

        IReadOnlyList<PoliticalKnowledgeRuntime> runtimes = CopyDetachedRuntimes();
        if (runtimes == null)
        {
            failure = "Political Knowledge detached values contain a null owner row.";
            return false;
        }

        if (!PoliticalKnowledgeStore.TryCreateP12FStaged(
            persons, institutions, claims, factions, offices, properties,
            runtimes, Revision, stagedAbsoluteDay, out staged))
        {
            failure = "Political Knowledge values could not be privately staged against the supplied roots and day.";
            return false;
        }
        return true;
    }
}
