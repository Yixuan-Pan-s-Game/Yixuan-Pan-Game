using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameUIBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapExists()
    {
        if (FindObjectOfType<GameUIBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject("GameUIBootstrap");
        bootstrap.AddComponent<GameUIBootstrap>();
    }

    private IEnumerator Start()
    {
        // Let PlayerToolController.Start finish building slots before UI registers.
        yield return null;
        BootstrapUI();
    }

    public void BootstrapUI()
    {
        PlayerToolController toolController = FindObjectOfType<PlayerToolController>();
        if (toolController == null)
        {
            return;
        }

        EnsureEventSystem();
        EnsureHotbar(toolController);
        EnsureBackpack(toolController);
        EnsureQuestSystem();
        EnsureForestMap();
        EnsureMenu();
    }

    private static void EnsureHotbar(PlayerToolController toolController)
    {
        if (toolController.Slots == null || toolController.Slots.Length == 0)
        {
            return;
        }

        HotbarUI hotbar = FindObjectOfType<HotbarUI>(true);
        if (hotbar == null)
        {
            GameObject canvasObject = CreateCanvas("HotbarCanvas", 10);
            GameObject hotbarObject = new GameObject("Hotbar");
            hotbarObject.transform.SetParent(canvasObject.transform, false);
            hotbarObject.AddComponent<RectTransform>();

            hotbar = hotbarObject.AddComponent<HotbarUI>();
            hotbar.Build(toolController.HotbarSlotCount);
        }
        else if (hotbar.transform.childCount == 0)
        {
            hotbar.Build(toolController.HotbarSlotCount);
        }

        toolController.RegisterHotbarUI(hotbar);
    }

    private static void EnsureBackpack(PlayerToolController toolController)
    {
        BackpackUI backpack = FindObjectOfType<BackpackUI>(true);
        if (backpack == null)
        {
            GameObject canvasObject = CreateCanvas("BackpackCanvas", 100);
            backpack = canvasObject.AddComponent<BackpackUI>();
            backpack.Build();
        }
        else if (!backpack.IsBuilt)
        {
            backpack.Build();
        }

        toolController.RegisterBackpackUI(backpack);
    }

    private static void EnsureQuestSystem()
    {
        if (FindObjectOfType<ForestQuestSystem>(true) != null)
        {
            return;
        }

        new GameObject("ForestQuestSystem").AddComponent<ForestQuestSystem>();
    }

    private static void EnsureForestMap()
    {
        if (FindObjectOfType<ForestMapUI>(true) != null)
        {
            return;
        }

        new GameObject("ForestMapUI").AddComponent<ForestMapUI>();
    }

    private static void EnsureMenu()
    {
        // ForestMenuUI is built in the scene and wired through the Inspector.
    }

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}

