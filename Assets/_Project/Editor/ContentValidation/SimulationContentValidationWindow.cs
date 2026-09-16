using UnityEditor;
using UnityEngine;

public sealed class SimulationContentValidationWindow : EditorWindow
{
    private enum IssueFilter
    {
        All,
        Errors,
        Warnings
    }

    private ContentValidationReport report;
    private IssueFilter filter;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Simulation/Validate Content")]
    public static void Open()
    {
        SimulationContentValidationWindow window = GetWindow<SimulationContentValidationWindow>();
        window.titleContent = new GUIContent("Simulation Content Validator");
        window.minSize = new Vector2(560f, 320f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("SIMULATION CONTENT VALIDATOR", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Project", GUILayout.Height(24f)) == true)
        {
            report = SimulationContentValidator.ValidateProject();
            Repaint();
        }

        if (GUILayout.Button("Clear", GUILayout.Width(80f), GUILayout.Height(24f)) == true)
        {
            report = null;
            Repaint();
        }

        if (GUILayout.Button("Log Report", GUILayout.Width(100f), GUILayout.Height(24f)) == true)
        {
            LogReport();
        }

        EditorGUILayout.EndHorizontal();

        if (report == null)
        {
            EditorGUILayout.HelpBox("Run Validate Project to audit ScriptableObject content.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(
            "Errors " + report.ErrorCount + "   Warnings " + report.WarningCount + "   Info " + report.InfoCount,
            EditorStyles.boldLabel);
        filter = (IssueFilter)GUILayout.Toolbar((int)filter, new[] { "All", "Errors", "Warnings" });
        EditorGUILayout.Space(4f);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < report.Issues.Count; i++)
        {
            ContentValidationIssue issue = report.Issues[i];
            if (ShouldShow(issue) == false)
            {
                continue;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(issue.Severity + "  " + issue.Code, EditorStyles.boldLabel);
            if (issue.Asset != null && GUILayout.Button("Ping", GUILayout.Width(48f)) == true)
            {
                Selection.activeObject = issue.Asset;
                EditorGUIUtility.PingObject(issue.Asset);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(
                (string.IsNullOrEmpty(issue.AssetPath) ? "<in-memory>" : issue.AssetPath)
                + "  |  " + issue.AssetType
                + (string.IsNullOrEmpty(issue.FieldPath) ? string.Empty : "  |  " + issue.FieldPath),
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private bool ShouldShow(ContentValidationIssue issue)
    {
        if (filter == IssueFilter.Errors)
        {
            return issue.Severity == ContentValidationSeverity.Error;
        }

        if (filter == IssueFilter.Warnings)
        {
            return issue.Severity == ContentValidationSeverity.Warning;
        }

        return true;
    }

    private void LogReport()
    {
        if (report == null)
        {
            return;
        }

        for (int i = 0; i < report.Issues.Count; i++)
        {
            ContentValidationIssue issue = report.Issues[i];
            switch (issue.Severity)
            {
                case ContentValidationSeverity.Error:
                    Debug.LogError(issue.ToString(), issue.Asset);
                    break;
                case ContentValidationSeverity.Warning:
                    Debug.LogWarning(issue.ToString(), issue.Asset);
                    break;
                default:
                    Debug.Log(issue.ToString(), issue.Asset);
                    break;
            }
        }
    }
}
