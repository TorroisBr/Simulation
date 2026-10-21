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

internal sealed class P12FScheduledDirectiveOwnerSnapshot
{
    internal sealed class Row
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
            ActionDefinitionId = directive.ActionDefinitionId;
            State = directive.State;
            ProcessedDay = directive.ProcessedDay;
            ResultReason = directive.ResultReason;
        }

        internal Row(string id, long absoluteDay, ScheduledDirectiveMode mode,
            ScheduledDirectiveOperation operation, string actorRuntimeId, string actionDefinitionId,
            ScheduledDirectiveState state, long processedDay, string resultReason)
        {
            Id = id;
            AbsoluteDay = absoluteDay;
            Mode = mode;
            Operation = operation;
            ActorRuntimeId = actorRuntimeId;
            ActionDefinitionId = actionDefinitionId;
            State = state;
            ProcessedDay = processedDay;
            ResultReason = resultReason;
        }

        internal ScheduledDirective Create(NpcActionData action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            ScheduledDirective directive = new ScheduledDirective(
                Id, AbsoluteDay, Mode, Operation, ActorRuntimeId,
                action.DefinitionId, (int)action.actionType);
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

    internal P12FScheduledDirectiveOwnerSnapshot(IEnumerable<Row> rows, long revision)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        token = null;
        vector = null;
        this.rows = new ReadOnlyCollection<Row>(new List<Row>(rows));
        Revision = revision;
    }

    internal IReadOnlyList<Row> Rows => rows;

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
                || !identities.Add(row.DirectiveId) || string.IsNullOrWhiteSpace(row.ActionDefinitionId)
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

        return TryStageCore(stagedTime, stagedNpcs, stagedActionDefinitions, out staged, out failure);
    }

    /// <summary>Stages detached P12-A rows without source capture evidence.</summary>
    internal bool TryStageForImage(
        SimulationTime stagedTime,
        IReadOnlyDictionary<string, NpcRuntime> stagedNpcs,
        IReadOnlyDictionary<string, NpcActionData> stagedActionDefinitions,
        out ScheduledDirectiveStore staged,
        out string failure)
    {
        staged = null;
        failure = null;
        if (token != null || vector != null || stagedTime == null
            || stagedActionDefinitions == null || stagedNpcs == null)
        {
            failure = "Detached Scheduled Directive staging requires image values and target roots only.";
            return false;
        }
        return TryStageCore(stagedTime, stagedNpcs, stagedActionDefinitions, out staged, out failure);
    }

    private bool TryStageCore(
        SimulationTime stagedTime,
        IReadOnlyDictionary<string, NpcRuntime> stagedNpcs,
        IReadOnlyDictionary<string, NpcActionData> stagedActionDefinitions,
        out ScheduledDirectiveStore staged,
        out string failure)
    {
        staged = null;
        failure = null;

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
            if (action.actionType != NpcActionType.EscapePrison)
            {
                failure = "A Scheduled Directive action definition is not the admitted EscapePrison action.";
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
