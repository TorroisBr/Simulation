using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Detached private snapshot for the two standalone F owner sections.</summary>
internal static class P12FStandaloneOwnerSnapshotValidation
{
    internal static bool TryGetRequiredSection(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId,
        int schemaVersion,
        object owner,
        int cardinality,
        long revision,
        out string failure)
    {
        failure = null;
        if (token == null || vector == null || !ReferenceEquals(token.OwnerSections, vector))
        {
            failure = "The exact completed-boundary token and owner-section vector are required.";
            return false;
        }

        OwnerSectionCensusSnapshot found = null;
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section != null && string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
            {
                if (found != null)
                {
                    failure = "The required owner section occurs more than once.";
                    return false;
                }
                found = section;
            }
        }

        if (found == null || found.SchemaVersion != schemaVersion
            || found.Role != OwnerSectionRole.Required
            || !ReferenceEquals(found.OwnerInstanceIdentity, owner)
            || found.Cardinality != cardinality || found.Revision != revision
            || cardinality < 0 || revision < 0L)
        {
            failure = "The Required owner-section witness does not match the concrete owner identity, schema, cardinality, and revision.";
            return false;
        }
        return true;
    }
}

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

    internal IReadOnlyList<PoliticalKnowledgeRuntime> CopyDetachedRuntimes()
    {
        List<PoliticalKnowledgeRuntime> copies = new List<PoliticalKnowledgeRuntime>(detachedRuntimes.Count);
        foreach (PoliticalKnowledgeRuntime runtime in detachedRuntimes) copies.Add(runtime.Clone());
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

        if (!PoliticalKnowledgeStore.TryCreateP12FStaged(
            persons, institutions, claims, factions, offices, properties,
            CopyDetachedRuntimes(), Revision, stagedAbsoluteDay, out staged))
        {
            failure = "Political Knowledge values could not be privately staged against the supplied roots and day.";
            return false;
        }
        return true;
    }
}

internal sealed class P12FScheduledDirectiveOwnerSnapshot
{
    private sealed class Row
    {
        internal string Id { get; }
        internal long AbsoluteDay { get; }
        internal ScheduledDirectiveMode Mode { get; }
        internal ScheduledDirectiveOperation Operation { get; }
        internal string ActorRuntimeId { get; }
        internal string ActionDefinitionId { get; }
        internal ScheduledDirectiveState State { get; }
        internal long ProcessedDay { get; }
        internal string ResultReason { get; }

        internal Row(ScheduledDirective directive)
        {
            Id = directive.DirectiveId;
            AbsoluteDay = directive.AbsoluteDay;
            Mode = directive.Mode;
            Operation = directive.Operation;
            ActorRuntimeId = directive.ActorRuntimeId;
            ActionDefinitionId = directive.Action?.DefinitionId;
            State = directive.State;
            ProcessedDay = directive.ProcessedDay;
            ResultReason = directive.ResultReason;
        }

        internal ScheduledDirective Create(NpcActionData action)
        {
            ScheduledDirective directive = new ScheduledDirective(
                Id, AbsoluteDay, Mode, Operation, ActorRuntimeId, action);
            if (!directive.RestoreP12FState(State, ProcessedDay, ResultReason))
                throw new InvalidOperationException("The captured ScheduledDirective disposition is invalid.");
            return directive;
        }
    }

    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> vector;
    private readonly ReadOnlyCollection<Row> rows;
    internal int Cardinality => rows.Count;
    internal long Revision { get; }

    private P12FScheduledDirectiveOwnerSnapshot(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        List<Row> rows,
        long revision)
    {
        this.token = token;
        this.vector = vector;
        this.rows = rows.AsReadOnly();
        Revision = revision;
    }

    internal static bool TryCapture(
        ScheduledDirectiveStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactVector,
        out P12FScheduledDirectiveOwnerSnapshot snapshot,
        out string failure)
    {
        snapshot = null;
        failure = null;
        if (owner == null || token == null || exactVector == null
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L
            || !P12FStandaloneOwnerSnapshotValidation.TryGetRequiredSection(
                token, exactVector, ScheduledDirectiveCensusProvider.SectionId,
                ScheduledDirectiveCensusProvider.SchemaVersion, owner, owner.GetCurrentCensus().Cardinality,
                owner.Revision, out failure))
        {
            failure = failure ?? "Scheduled Directives require their exact Required Daily-v1 owner witness.";
            return false;
        }

        if (!owner.TryCaptureP12FState(out IReadOnlyList<ScheduledDirective> liveRows, out long revision)
            || liveRows == null || liveRows.Count != owner.GetCurrentCensus().Cardinality)
        {
            failure = "Scheduled Directive rows could not be copied coherently.";
            return false;
        }
        int count = liveRows.Count;
        List<Row> detached = new List<Row>(count);
        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (ScheduledDirective row in liveRows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.DirectiveId)
                || !identities.Add(row.DirectiveId) || string.IsNullOrWhiteSpace(row.Action?.DefinitionId)
                || !Enum.IsDefined(typeof(ScheduledDirectiveState), row.State))
            {
                failure = "Scheduled Directive identity, action definition, or terminal disposition is invalid.";
                return false;
            }
            detached.Add(new Row(row));
        }

        OwnerSectionCensusWitness after = owner.GetCurrentCensus();
        if (after.Cardinality != count || after.Revision != revision
            || !P12FStandaloneOwnerSnapshotValidation.TryGetRequiredSection(
                token, exactVector, ScheduledDirectiveCensusProvider.SectionId,
                ScheduledDirectiveCensusProvider.SchemaVersion, owner, count, revision, out failure))
        {
            failure = failure ?? "Scheduled Directives changed while detached values were captured.";
            return false;
        }

        snapshot = new P12FScheduledDirectiveOwnerSnapshot(token, exactVector, detached, revision);
        return true;
    }

    internal bool TryStage(
        DailyCaptureEligibilityToken stagedToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> stagedVector,
        SimulationTime stagedTime,
        IReadOnlyDictionary<string, NpcRuntime> stagedNpcs,
        IReadOnlyDictionary<string, NpcActionData> stagedActionDefinitions,
        out ScheduledDirectiveStore staged,
        out string failure)
    {
        staged = null;
        failure = null;
        if (!ReferenceEquals(token, stagedToken) || !ReferenceEquals(vector, stagedVector)
            || stagedToken == null || stagedTime == null || stagedActionDefinitions == null || stagedNpcs == null)
        {
            failure = "Scheduled Directive staging requires the same boundary metadata, staged SimulationTime, NPC roots, and action definitions.";
            return false;
        }

        foreach (KeyValuePair<string, NpcRuntime> entry in stagedNpcs)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null
                || !string.Equals(entry.Key, entry.Value.RuntimeId, StringComparison.Ordinal))
            {
                failure = "A staged NPC dictionary key must match its concrete RuntimeId.";
                return false;
            }
        }

        List<ScheduledDirective> stagedRows = new List<ScheduledDirective>(rows.Count);
        foreach (Row row in rows)
        {
            if (!stagedActionDefinitions.TryGetValue(row.ActionDefinitionId, out NpcActionData action)
                || action == null || !string.Equals(action.DefinitionId, row.ActionDefinitionId, StringComparison.Ordinal))
            {
                failure = "A Scheduled Directive action definition is missing or has a mismatched stable identity.";
                return false;
            }
            try { stagedRows.Add(row.Create(action)); }
            catch (ArgumentException)
            {
                failure = "A Scheduled Directive could not be reconstructed from the staged action definition.";
                return false;
            }
        }

        if (!ScheduledDirectiveStore.TryCreateP12FStaged(stagedTime, stagedRows, Revision, out staged))
        {
            failure = "Scheduled Directives could not be privately staged without processing or dispatch.";
            return false;
        }
        return true;
    }
}
