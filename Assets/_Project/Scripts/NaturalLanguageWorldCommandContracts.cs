using System;
using System.Collections.Generic;

public enum NaturalLanguageTranslationStatus
{
    Resolved,
    Ambiguous,
    MissingInformation,
    Unsupported,
    Invalid
}

public enum WorldCommandTranslationEntityKind
{
    Npc,
    MacroLocation,
    City,
    ExplorableSite,
    LocalPlace,
    ItemDefinition,
    LocalPlaceType,
    LocalConnectionType,
    Opposition
}

public enum WorldCommandTranslationDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed class NaturalLanguageWorldCommandRequest
{
    public string Text { get; }
    public WorldCommandOrigin Origin { get; }
    public WorldCommandAuthorityMode? PreferredAuthority { get; }

    public NaturalLanguageWorldCommandRequest(
        string text,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode? preferredAuthority = null)
    {
        Text = text ?? string.Empty;
        Origin = origin;
        PreferredAuthority = preferredAuthority;
    }
}

public sealed class WorldCommandTranslationEntity
{
    public WorldCommandTranslationEntityKind Kind { get; }
    public string RuntimeId { get; }
    public string DefinitionId { get; }
    public string DisplayName { get; }

    public string StableId => string.IsNullOrWhiteSpace(RuntimeId) ? DefinitionId : RuntimeId;

    public WorldCommandTranslationEntity(
        WorldCommandTranslationEntityKind kind,
        string runtimeId = null,
        string definitionId = null,
        string displayName = null)
    {
        Kind = kind;
        RuntimeId = Normalize(runtimeId);
        DefinitionId = Normalize(definitionId);
        DisplayName = Normalize(displayName);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

public sealed class WorldCommandTranslationCandidate
{
    public WorldCommandTranslationEntity Entity { get; }
    public string CandidateId => Entity?.StableId;
    public WorldCommandTranslationEntityKind EntityKind => Entity == null
        ? WorldCommandTranslationEntityKind.Npc
        : Entity.Kind;
    public string RuntimeId => Entity?.RuntimeId;
    public string DefinitionId => Entity?.DefinitionId;
    public string DisplayName => Entity?.DisplayName;

    public WorldCommandTranslationCandidate(WorldCommandTranslationEntity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }
}

public sealed class WorldCommandTranslationDiagnostic
{
    public string Code { get; }
    public string Field { get; }
    public string Message { get; }
    public WorldCommandTranslationDiagnosticSeverity Severity { get; }

    public WorldCommandTranslationDiagnostic(
        string code,
        string message,
        string field = null,
        WorldCommandTranslationDiagnosticSeverity severity = WorldCommandTranslationDiagnosticSeverity.Error)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "TranslationDiagnostic" : code;
        Message = message ?? string.Empty;
        Field = string.IsNullOrWhiteSpace(field) ? null : field;
        Severity = severity;
    }
}

public sealed class NaturalLanguageTranslationResult
{
    private readonly IReadOnlyList<WorldCommandTranslationCandidate> candidates;
    private readonly IReadOnlyList<string> missingFields;
    private readonly IReadOnlyList<WorldCommandTranslationDiagnostic> diagnostics;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> resolvedEntities;

    public NaturalLanguageTranslationStatus Status { get; }
    public WorldCommand Command { get; }
    public IReadOnlyList<WorldCommandTranslationCandidate> Candidates => candidates;
    public IReadOnlyList<string> MissingFields => missingFields;
    public IReadOnlyList<WorldCommandTranslationDiagnostic> Diagnostics => diagnostics;
    public IReadOnlyList<WorldCommandTranslationEntity> ResolvedEntities => resolvedEntities;

    private NaturalLanguageTranslationResult(
        NaturalLanguageTranslationStatus status,
        WorldCommand command,
        IEnumerable<WorldCommandTranslationCandidate> candidates,
        IEnumerable<string> missingFields,
        IEnumerable<WorldCommandTranslationDiagnostic> diagnostics,
        IEnumerable<WorldCommandTranslationEntity> resolvedEntities)
    {
        Status = status;
        Command = command;
        this.candidates = Copy(candidates);
        this.missingFields = CopyStrings(missingFields);
        this.diagnostics = Copy(diagnostics);
        this.resolvedEntities = Copy(resolvedEntities);
    }

    public static NaturalLanguageTranslationResult Resolved(
        WorldCommand command,
        IEnumerable<WorldCommandTranslationEntity> resolvedEntities = null,
        IEnumerable<WorldCommandTranslationDiagnostic> diagnostics = null)
    {
        if (command == null)
        {
            return Invalid(new[]
            {
                new WorldCommandTranslationDiagnostic(
                    "ResolvedCommandMissing",
                    "A resolved translation requires exactly one WorldCommand.")
            });
        }

        return new NaturalLanguageTranslationResult(
            NaturalLanguageTranslationStatus.Resolved,
            command,
            null,
            null,
            diagnostics,
            resolvedEntities);
    }

    public static NaturalLanguageTranslationResult Ambiguous(
        IEnumerable<WorldCommandTranslationCandidate> candidates,
        IEnumerable<WorldCommandTranslationDiagnostic> diagnostics = null)
    {
        return new NaturalLanguageTranslationResult(
            NaturalLanguageTranslationStatus.Ambiguous,
            null,
            candidates,
            null,
            diagnostics,
            null);
    }

    public static NaturalLanguageTranslationResult MissingInformation(
        IEnumerable<string> missingFields,
        IEnumerable<WorldCommandTranslationDiagnostic> diagnostics = null)
    {
        return new NaturalLanguageTranslationResult(
            NaturalLanguageTranslationStatus.MissingInformation,
            null,
            null,
            missingFields,
            diagnostics,
            null);
    }

    public static NaturalLanguageTranslationResult Unsupported(
        IEnumerable<WorldCommandTranslationDiagnostic> diagnostics)
    {
        return new NaturalLanguageTranslationResult(
            NaturalLanguageTranslationStatus.Unsupported,
            null,
            null,
            null,
            diagnostics,
            null);
    }

    public static NaturalLanguageTranslationResult Invalid(
        IEnumerable<WorldCommandTranslationDiagnostic> diagnostics)
    {
        return new NaturalLanguageTranslationResult(
            NaturalLanguageTranslationStatus.Invalid,
            null,
            null,
            null,
            diagnostics,
            null);
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
    {
        List<T> values = new List<T>();
        if (source != null)
        {
            foreach (T value in source)
            {
                if (value != null)
                {
                    values.Add(value);
                }
            }
        }

        return values.AsReadOnly();
    }

    private static IReadOnlyList<string> CopyStrings(IEnumerable<string> source)
    {
        List<string> values = new List<string>();
        if (source != null)
        {
            foreach (string value in source)
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    values.Add(value);
                }
            }
        }

        return values.AsReadOnly();
    }
}

public interface IWorldCommandEntityResolver
{
    IReadOnlyList<WorldCommandTranslationEntity> FindNpcs(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindMacroLocations(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindCities(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindExplorableSites(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindLocalPlaces(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindOppositions(string reference);
}

public interface IWorldCommandDefinitionLookup
{
    IReadOnlyList<WorldCommandTranslationEntity> FindItemDefinitions(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindLocalPlaceTypes(string reference);
    IReadOnlyList<WorldCommandTranslationEntity> FindLocalConnectionTypes(string reference);
}

public sealed class WorldCommandTranslationContext
{
    public IWorldCommandEntityResolver EntityResolver { get; }
    public IWorldCommandDefinitionLookup DefinitionLookup { get; }

    public WorldCommandTranslationContext(
        IWorldCommandEntityResolver entityResolver = null,
        IWorldCommandDefinitionLookup definitionLookup = null)
    {
        EntityResolver = entityResolver;
        DefinitionLookup = definitionLookup;
    }
}

public interface IWorldCommandNaturalLanguageTranslator
{
    NaturalLanguageTranslationResult Translate(
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context);
}
