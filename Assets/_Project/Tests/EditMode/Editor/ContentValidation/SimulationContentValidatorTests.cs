using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SimulationContentValidatorTests
{
    private const string TemporaryFolder = "Assets/_Project/Tests/TempContentValidation";
    private readonly List<UnityEngine.Object> createdAssets = new List<UnityEngine.Object>();
    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [SetUp]
    public void SetUp()
    {
        EnsureTemporaryFolder();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
        AssetDatabase.SaveAssets();

        for (int i = 0; i < createdAssets.Count; i++)
        {
            string path = AssetDatabase.GetAssetPath(createdAssets[i]);
            if (string.IsNullOrEmpty(path) == false)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        createdAssets.Clear();
        AssetDatabase.DeleteAsset(TemporaryFolder);
        AssetDatabase.Refresh();
    }

    [Test]
    public void ValidDefinitionProducesNoMissingIdError()
    {
        ItemData item = CreateObject<ItemData>("valid-item");
        item.id = "sword";

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(new[] { item });

        Assert.That(report.Issues.Any(x => x.Code == ContentValidationCodes.MissingDefinitionId), Is.False);
    }

    [Test]
    public void WhitespaceDefinitionIdProducesError()
    {
        ItemData item = CreateObject<ItemData>("whitespace-item");
        item.id = " \t";

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(new[] { item });

        Assert.That(report.Issues.Any(x => x.Code == ContentValidationCodes.MissingDefinitionId), Is.True);
        Assert.That(report.HasErrors, Is.True);
    }

    [Test]
    public void DuplicateDefinitionIdsWithinSameTypeProduceError()
    {
        ItemData first = CreateObject<ItemData>("duplicate-first");
        ItemData second = CreateObject<ItemData>("duplicate-second");
        first.id = "iron";
        second.id = "iron";

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { second, first });

        Assert.That(report.Issues.Count(x => x.Code == ContentValidationCodes.DuplicateDefinitionId), Is.EqualTo(2));
    }

    [Test]
    public void SameDefinitionIdAcrossDifferentTypesIsNotAutomaticallyError()
    {
        ItemData item = CreateObject<ItemData>("same-id-item");
        CityData city = CreateObject<CityData>("same-id-city");
        item.id = "shared";
        city.id = "shared";

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { item, city });

        Assert.That(report.Issues.Any(x => x.Code == ContentValidationCodes.DuplicateDefinitionId), Is.False);
    }

    [Test]
    public void NullOptionalReferenceIsNotReportedAsRequired()
    {
        NpcData npc = CreateObject<NpcData>("optional-job-npc");
        npc.id = "npc";
        npc.job = null;

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(new[] { npc });

        Assert.That(report.Issues.Any(x => x.Code == ContentValidationCodes.NullRequiredReference), Is.False);
    }

    [Test]
    public void NullRequiredReferenceProducesStructuredIssue()
    {
        ItemData item = CreateObject<ItemData>("null-attribute-item");
        item.id = "item";
        item.capabilityModifiers.Add(new CapabilityAttributeModifier());

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(new[] { item });
        ContentValidationIssue issue = report.Issues.Single(x => x.Code == ContentValidationCodes.NullRequiredReference);

        Assert.That(issue.Severity, Is.EqualTo(ContentValidationSeverity.Error));
        Assert.That(issue.FieldPath, Is.EqualTo("capabilityModifiers[0].attribute"));
        Assert.That(issue.AssetType, Is.EqualTo(nameof(ItemData)));
    }

    [Test]
    public void NullElementInRequiredDefinitionListIsReported()
    {
        NpcData npc = CreateObject<NpcData>("null-trait-npc");
        npc.id = "npc";
        npc.traits.Add(null);

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(new[] { npc });

        Assert.That(report.Issues.Any(x => x.Code == ContentValidationCodes.NullCollectionElement), Is.True);
    }

    [Test]
    public void InvalidFiniteNumericValueIsReported()
    {
        ItemData item = CreateObject<ItemData>("nan-item");
        CapabilityAttributeData attribute = CreateObject<CapabilityAttributeData>("numeric-attribute");
        item.id = "item";
        attribute.id = "attribute";
        item.capabilityModifiers.Add(new CapabilityAttributeModifier(attribute, float.NaN));

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { item, attribute });

        Assert.That(report.Issues.Any(x => x.Code == ContentValidationCodes.InvalidNumericValue), Is.True);
    }

    [Test]
    public void ValidationDoesNotModifyAsset()
    {
        ItemData item = CreateObject<ItemData>("read-only-item");
        item.id = "before";
        item.capabilityModifiers.Add(new CapabilityAttributeModifier());
        string beforeId = item.id;
        int beforeCount = item.capabilityModifiers.Count;

        SimulationContentValidator.ValidateAssets(new[] { item });

        Assert.That(item.id, Is.EqualTo(beforeId));
        Assert.That(item.capabilityModifiers.Count, Is.EqualTo(beforeCount));
    }

    [Test]
    public void ValidationDoesNotGenerateDefinitionId()
    {
        ItemData item = CreateObject<ItemData>("no-generated-id-item");
        item.id = string.Empty;

        SimulationContentValidator.ValidateAssets(new[] { item });

        Assert.That(item.id, Is.Empty);
    }

    [Test]
    public void ReportOrderingIsDeterministic()
    {
        ItemData first = CreateObject<ItemData>("ordering-a");
        ItemData second = CreateObject<ItemData>("ordering-b");
        first.id = string.Empty;
        second.id = string.Empty;

        ContentValidationReport firstReport = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { second, first });
        ContentValidationReport secondReport = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { first, second });

        Assert.That(firstReport.Issues.Select(x => x.ToString()), Is.EqualTo(secondReport.Issues.Select(x => x.ToString())));
    }

    [Test]
    public void ReportCountsAreCorrect()
    {
        ItemData invalid = CreateObject<ItemData>("count-invalid");
        invalid.id = string.Empty;
        ItemData duplicateA = CreateObject<ItemData>("count-duplicate-a");
        ItemData duplicateB = CreateObject<ItemData>("count-duplicate-b");
        duplicateA.id = "duplicate";
        duplicateB.id = "duplicate";
        NpcData nullEntry = CreateObject<NpcData>("count-null-entry");
        nullEntry.id = "npc";
        nullEntry.traits.Add(null);

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { invalid, duplicateA, duplicateB, nullEntry });

        Assert.That(report.ErrorCount, Is.EqualTo(4));
        Assert.That(report.WarningCount, Is.EqualTo(0));
        Assert.That(report.InfoCount, Is.EqualTo(0));
        Assert.That(report.HasErrors, Is.True);
    }

    [Test]
    public void IssueContainsAssetPathWhenProjectAssetIsKnown()
    {
        ItemData item = CreateAsset<ItemData>("known-path-item");
        item.id = string.Empty;
        AssetDatabase.SaveAssets();

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(new[] { item });
        ContentValidationIssue issue = report.Issues.Single(x => x.Code == ContentValidationCodes.MissingDefinitionId);

        Assert.That(issue.AssetPath, Is.EqualTo(TemporaryFolder + "/known-path-item.asset"));
    }

    [Test]
    public void IssueUsesStructuredCodeNotMessageParsing()
    {
        ItemData item = CreateObject<ItemData>("structured-code-item");
        item.id = string.Empty;

        ContentValidationIssue issue = SimulationContentValidator.ValidateAssets(new[] { item }).Issues.Single();

        Assert.That(issue.Code, Is.EqualTo(ContentValidationCodes.MissingDefinitionId));
        Assert.That(issue.Message, Does.Not.EqualTo(issue.Code));
    }

    [Test]
    public void ProjectValidationCanDiscoverKnownTemporaryTestAssets()
    {
        ItemData item = CreateAsset<ItemData>("project-discovery-item");
        item.id = string.Empty;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ContentValidationReport report = SimulationContentValidator.ValidateProject();

        Assert.That(report.Issues.Any(x => x.AssetPath == TemporaryFolder + "/project-discovery-item.asset"
            && x.Code == ContentValidationCodes.MissingDefinitionId), Is.True);
    }

    [Test]
    public void ProjectValidationDoesNotModifyOrSaveAssets()
    {
        ItemData item = CreateAsset<ItemData>("project-read-only-item");
        item.id = "saved";
        AssetDatabase.SaveAssets();
        string path = AssetDatabase.GetAssetPath(item);
        string before = File.ReadAllText(path);
        item.id = "unsaved";
        EditorUtility.SetDirty(item);

        SimulationContentValidator.ValidateProject();

        string after = File.ReadAllText(path);
        Assert.That(after, Is.EqualTo(before));
        Assert.That(item.id, Is.EqualTo("unsaved"));
    }

    [Test]
    public void DuplicateSearchIsOrdinalAndDeterministic()
    {
        ItemData lower = CreateObject<ItemData>("ordinal-lower");
        ItemData upper = CreateObject<ItemData>("ordinal-upper");
        ItemData exact = CreateObject<ItemData>("ordinal-exact");
        lower.id = "iron";
        upper.id = "IRON";
        exact.id = "iron";

        ContentValidationReport report = SimulationContentValidator.ValidateAssets(
            new UnityEngine.Object[] { upper, exact, lower });

        Assert.That(report.Issues.Count(x => x.Code == ContentValidationCodes.DuplicateDefinitionId), Is.EqualTo(2));
        Assert.That(report.Issues[0].DefinitionId, Is.EqualTo("iron"));
    }

    [Test]
    public void EditorWindowCanBeCreatedWithoutException()
    {
        SimulationContentValidationWindow window = null;
        Assert.DoesNotThrow(() => window = ScriptableObject.CreateInstance<SimulationContentValidationWindow>());
        Assert.That(window, Is.Not.Null);
        UnityEngine.Object.DestroyImmediate(window);
    }

    private T CreateObject<T>(string objectName) where T : ScriptableObject
    {
        T result = ScriptableObject.CreateInstance<T>();
        result.name = objectName;
        createdObjects.Add(result);
        return result;
    }

    private T CreateAsset<T>(string assetName) where T : ScriptableObject
    {
        T result = CreateObject<T>(assetName);
        string path = TemporaryFolder + "/" + assetName + ".asset";
        AssetDatabase.CreateAsset(result, path);
        createdObjects.Remove(result);
        createdAssets.Add(result);
        return result;
    }

    private static void EnsureTemporaryFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/_Project/Tests") == false)
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Tests");
        }

        if (AssetDatabase.IsValidFolder(TemporaryFolder) == false)
        {
            AssetDatabase.CreateFolder("Assets/_Project/Tests", "TempContentValidation");
        }

        AssetDatabase.Refresh();
    }
}
