using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GMConsolePlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator GmConsoleCanPreviewApplyAndRefreshObserver()
    {
        EditorSceneManager.OpenScene(WorldObserverAssetBuilder.ScenePath, OpenSceneMode.Single);

        yield return new EnterPlayMode();
        yield return null;

        WorldObserverDemoBootstrap bootstrap = Object.FindFirstObjectByType<WorldObserverDemoBootstrap>();
        Assert.That(bootstrap, Is.Not.Null);
        GMConsolePanel panel = bootstrap.GmConsolePanel;
        Assert.That(panel, Is.Not.Null);
        Assert.That(bootstrap.WorldCommandService, Is.Not.Null);
        Assert.That(bootstrap.ContentStore, Is.Not.Null);

        panel.Show();
        Select(panel.CommandTypeDropdown, WorldCommandKind.DeclareStackResource.ToString());
        Select(panel.AuthorityDropdown, WorldCommandAuthorityMode.Declare.ToString());
        Select(panel.GetFormDropdown("content.ownerKind"), PlaceContentOwnerKind.ExplorableSite.ToString());
        Select(panel.GetFormDropdown("content.persistence"), PlaceContentPersistencePolicy.Durable.ToString());
        SetInput(panel, "content.owner", "observer-demo-ruin-runtime");
        SetInput(panel, "content.macro", "observer-demo-ruin-location");
        SetInput(panel, "content.item", "observer-demo-ore");
        SetInput(panel, "content.amount", "1");
        SetInput(panel, "content.decay", "0");
        SetInput(panel, "content.averageCost", "4");

        int refreshBefore = bootstrap.ObserverView.RefreshCount;
        panel.PreviewButton.onClick.Invoke();
        yield return null;

        Assert.That(panel.LastPreview, Is.Not.Null);
        Assert.That(panel.LastPreview.IsValid, Is.True);
        Assert.That(bootstrap.WorldCommandService.RecordStore.Records, Is.Empty);

        panel.ApplyButton.onClick.Invoke();
        yield return null;

        Assert.That(panel.LastResult, Is.Not.Null);
        Assert.That(panel.LastResult.Success, Is.True);
        Assert.That(bootstrap.WorldCommandService.RecordStore.Records, Has.Count.EqualTo(1));
        Assert.That(bootstrap.ContentStore.Places, Has.Count.EqualTo(1));
        Assert.That(bootstrap.ContentStore.Places[0].StackedContent, Has.Count.EqualTo(1));
        Assert.That(bootstrap.ObserverView.RefreshCount, Is.EqualTo(refreshBefore + 1));

        yield return new ExitPlayMode();
    }

    private static void SetInput(GMConsolePanel panel, string key, string value)
    {
        TMP_InputField input = panel.GetInput(key);
        Assert.That(input, Is.Not.Null, key);
        input.text = value;
    }

    private static void Select(TMP_Dropdown dropdown, string value)
    {
        Assert.That(dropdown, Is.Not.Null);
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == value)
            {
                dropdown.value = i;
                dropdown.RefreshShownValue();
                return;
            }
        }

        Assert.Fail("Dropdown value was not present: " + value);
    }
}
