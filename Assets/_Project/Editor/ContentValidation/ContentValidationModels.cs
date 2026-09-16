using System;
using System.Collections.Generic;
using UnityEngine;

public enum ContentValidationSeverity
{
    Info,
    Warning,
    Error
}

public static class ContentValidationCodes
{
    public const string MissingDefinitionId = "MISSING_DEFINITION_ID";
    public const string DuplicateDefinitionId = "DUPLICATE_DEFINITION_ID";
    public const string NullRequiredReference = "NULL_REQUIRED_REFERENCE";
    public const string NullOptionalReference = "NULL_OPTIONAL_REFERENCE";
    public const string NullCollectionElement = "NULL_COLLECTION_ELEMENT";
    public const string DuplicateReference = "DUPLICATE_REFERENCE";
    public const string InvalidNumericValue = "INVALID_NUMERIC_VALUE";
    public const string IncompatibleConfiguration = "INCOMPATIBLE_CONFIGURATION";
}

public sealed class ContentValidationIssue
{
    public ContentValidationSeverity Severity { get; }
    public string Code { get; }
    public string AssetPath { get; }
    public string AssetType { get; }
    public string DefinitionId { get; }
    public string FieldPath { get; }
    public string Message { get; }
    public UnityEngine.Object Asset { get; }

    public ContentValidationIssue(
        ContentValidationSeverity severity,
        string code,
        string assetPath,
        string assetType,
        string definitionId,
        string fieldPath,
        string message,
        UnityEngine.Object asset = null)
    {
        Severity = severity;
        Code = code ?? string.Empty;
        AssetPath = assetPath ?? string.Empty;
        AssetType = assetType ?? string.Empty;
        DefinitionId = definitionId ?? string.Empty;
        FieldPath = fieldPath ?? string.Empty;
        Message = message ?? string.Empty;
        Asset = asset;
    }

    public override string ToString()
    {
        string location = string.IsNullOrEmpty(AssetPath) == false ? AssetPath : "<in-memory>";
        string field = string.IsNullOrEmpty(FieldPath) == false ? " [" + FieldPath + "]" : string.Empty;
        return Severity + " " + Code + " " + location + field + ": " + Message;
    }
}

public sealed class ContentValidationReport
{
    private readonly List<ContentValidationIssue> issues;
    private readonly IReadOnlyList<ContentValidationIssue> readOnlyIssues;
    private readonly int errorCount;
    private readonly int warningCount;
    private readonly int infoCount;

    public IReadOnlyList<ContentValidationIssue> Issues => readOnlyIssues;
    public int ErrorCount => errorCount;
    public int WarningCount => warningCount;
    public int InfoCount => infoCount;
    public bool HasErrors => errorCount > 0;

    public ContentValidationReport(IEnumerable<ContentValidationIssue> sourceIssues)
    {
        issues = new List<ContentValidationIssue>();

        if (sourceIssues != null)
        {
            foreach (ContentValidationIssue issue in sourceIssues)
            {
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }
        }

        issues.Sort(ContentValidationIssueComparer.Instance);
        readOnlyIssues = issues.AsReadOnly();

        for (int i = 0; i < issues.Count; i++)
        {
            switch (issues[i].Severity)
            {
                case ContentValidationSeverity.Error:
                    errorCount++;
                    break;
                case ContentValidationSeverity.Warning:
                    warningCount++;
                    break;
                case ContentValidationSeverity.Info:
                    infoCount++;
                    break;
            }
        }
    }

    private sealed class ContentValidationIssueComparer : IComparer<ContentValidationIssue>
    {
        public static readonly ContentValidationIssueComparer Instance = new ContentValidationIssueComparer();

        // Stable order: severity (Error, Warning, Info), asset path, code, definition ID, type, field, message.
        public int Compare(ContentValidationIssue left, ContentValidationIssue right)
        {
            if (ReferenceEquals(left, right) == true)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int result = GetSeverityRank(left.Severity).CompareTo(GetSeverityRank(right.Severity));
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left.AssetPath, right.AssetPath);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left.Code, right.Code);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left.DefinitionId, right.DefinitionId);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left.AssetType, right.AssetType);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left.FieldPath, right.FieldPath);
            if (result != 0)
            {
                return result;
            }

            return string.CompareOrdinal(left.Message, right.Message);
        }

        private static int GetSeverityRank(ContentValidationSeverity severity)
        {
            switch (severity)
            {
                case ContentValidationSeverity.Error:
                    return 0;
                case ContentValidationSeverity.Warning:
                    return 1;
                default:
                    return 2;
            }
        }
    }
}
