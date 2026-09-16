using UnityEngine;

public sealed class WorldObserverDemoBootstrap : MonoBehaviour
{
    [SerializeField] private WorldObserverCanvasView observerView;
    [SerializeField] private WorldObserverTimeController timeController;

    public WorldObserverCanvasView ObserverView
    {
        get
        {
            EnsureReady();
            return observerView;
        }
    }

    private void Awake()
    {
        EnsureReady();
    }

    public void EnsureReady()
    {
        if (observerView == null)
        {
            GameObject viewObject = new GameObject("ObserverView");
            viewObject.transform.SetParent(transform, false);
            observerView = viewObject.AddComponent<WorldObserverCanvasView>();
        }

        if (timeController == null)
        {
            GameObject controllerObject = new GameObject("ObserverTimeController");
            controllerObject.transform.SetParent(transform, false);
            timeController = controllerObject.AddComponent<WorldObserverTimeController>();
        }

        observerView?.EnsureReady();
    }

    public void Initialize(WorldObserverQueryService service, SimulationRuntime runtime = null)
    {
        EnsureReady();
        if (timeController != null)
        {
            timeController.Bind(runtime);
        }

        observerView?.Initialize(service, timeController);
    }
}
