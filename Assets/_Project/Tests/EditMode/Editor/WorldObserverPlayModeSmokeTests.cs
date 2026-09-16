using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class WorldObserverPlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator ObserverSceneCanRenderAndSelectNode()
    {
        EditorSceneManager.OpenScene(WorldObserverAssetBuilder.ScenePath, OpenSceneMode.Single);

        yield return new EnterPlayMode();
        yield return null;

        WorldObserverDemoBootstrap bootstrap = Object.FindFirstObjectByType<WorldObserverDemoBootstrap>();
        Assert.That(bootstrap, Is.Not.Null);
        Assert.That(bootstrap.ObserverView.WorldNodes, Is.Not.Empty);
        WorldObserverWorldNodeView selectable = null;
        foreach (WorldObserverWorldNodeView node in bootstrap.ObserverView.WorldNodes)
        {
            if (string.IsNullOrWhiteSpace(node.SelectionRuntimeId) == false)
            {
                selectable = node;
                break;
            }
        }

        Assert.That(selectable, Is.Not.Null);
        selectable.Button.onClick.Invoke();
        yield return null;

        Assert.That(bootstrap.ObserverView.SelectedPlaceRuntimeId, Is.EqualTo(selectable.SelectionRuntimeId));
        Assert.That(bootstrap.ObserverView.SelectedPlaceTitle.text, Is.Not.Empty);

        yield return new ExitPlayMode();
    }
}
