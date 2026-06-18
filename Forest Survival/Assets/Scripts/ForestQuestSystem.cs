using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Main forest quest controller that tracks objectives, rewards, prompts, and completion state.
public class ForestQuestSystem : MonoBehaviour
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: RewardItemName.
    private const string RewardItemName = "Forest Guide";
    // Important runtime data or configuration used by this component: instance.
    private static ForestQuestSystem instance;

    private readonly string[] questTexts =
    {
        "Chop your first tree and get Wood",
        "Mine your first rock and get Stone",
        "Craft a Workbench",
        "Craft a Furnace",
        "Mine Coal Ore",
        "Mine Iron Ore",
        "Craft an Auto Forge",
        "Craft the full Wood set",
        "Craft the full Stone set",
        "Craft the full Iron set"
    };

    // Important runtime data or configuration used by this component: rewards.
    private readonly int[] rewards = { 2, 2, 2, 2, 2, 2, 3, 5, 5, 5 };
    // Important runtime data or configuration used by this component: completed.
    private bool[] completed;
    // Layer or mask filter used by physics queries or rendering: player.
    private PlayerToolController player;
    // Cached component or scene reference to avoid repeated lookups: panel.
    private GameObject panel;
    // Cached component or scene reference to avoid repeated lookups: hintPanel.
    private GameObject hintPanel;
    // Cached component or scene reference to avoid repeated lookups: progressText.
    private Text progressText;
    // Cached component or scene reference to avoid repeated lookups: checkmarkTexts.
    private Text[] checkmarkTexts;
    // Cached component or scene reference to avoid repeated lookups: toastText.
    private Text toastText;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: toastTimer.
    private float toastTimer;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        instance = this;
        completed = new bool[questTexts.Length];
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        player = FindObjectOfType<PlayerToolController>();
        EnsureCatalogEntries();
        BuildUI();
        RefreshUI();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        bool menuOpen = ForestMenuUI.IsMenuOpen;
        if (hintPanel != null && hintPanel.activeSelf == menuOpen)
        {
            hintPanel.SetActive(!menuOpen);
        }

        if (menuOpen)
        {
            if (panel != null) panel.SetActive(false);
            if (toastText != null) toastText.gameObject.SetActive(false);
            return;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            panel.SetActive(!panel.activeSelf);
        }

        if (toastText != null && toastText.gameObject.activeSelf)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f)
            {
                toastText.gameObject.SetActive(false);
            }
        }
    }

    // Handles the notify item added workflow.
    public static void NotifyItemAdded(string itemId, int amount)
    {
        if (instance != null)
        {
            instance.OnItemAdded(itemId, amount);
        }
    }

    // Handles the notify crafted workflow.
    public static void NotifyCrafted(string itemId)
    {
        if (instance != null)
        {
            instance.OnCrafted(itemId);
        }
    }

    // Handles the on item added workflow.
    private void OnItemAdded(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
        {
            return;
        }

        string normalized = InventoryUtility.NormalizeItemId(itemId);
        if (normalized == "wood") CompleteQuest(0);
        if (normalized == "stone") CompleteQuest(1);
        if (normalized == "coal_ore" || normalized == "coal") CompleteQuest(4);
        if (normalized == "iron_ore") CompleteQuest(5);
    }

    // Handles the on crafted workflow.
    private void OnCrafted(string itemId)
    {
        string normalized = InventoryUtility.NormalizeItemId(itemId);
        if (normalized == "workbench") CompleteQuest(2);
        if (normalized == "furnace") CompleteQuest(3);
        if (normalized == "auto_forge") CompleteQuest(6);
        RefreshFromInventory();
    }

    // Refreshes and applies configuration or runtime state for refresh from inventory.
    private void RefreshFromInventory()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerToolController>();
        }

        if (player == null)
        {
            return;
        }

        if (HasAll("wood_helmet", "wood_armor", "wood_sword", "wood_axe", "wood_pickaxe")) CompleteQuest(7);
        if (HasAll("stone_helmet", "stone_armor", "stone_sword", "stone_axe", "stone_pickaxe")) CompleteQuest(8);
        if (HasAll("iron_helmet", "iron_armor", "iron_sword", "iron_axe", "iron_pickaxe")) CompleteQuest(9);
    }

    // Calculates and returns the result for has all.
    private bool HasAll(params string[] itemIds)
    {
        for (int i = 0; i < itemIds.Length; i++)
        {
            if (player.GetItemCount(itemIds[i]) <= 0)
            {
                return false;
            }
        }

        return true;
    }

    // Handles the complete quest workflow.
    private void CompleteQuest(int index)
    {
        if (index < 0 || index >= completed.Length || completed[index])
        {
            return;
        }

        completed[index] = true;
        GiveReward(rewards[index]);
        ShowToast("Quest Complete: " + questTexts[index] + "\nReward: +" + rewards[index] + " Forest Guide");
        RefreshUI();
    }

    // Handles the give reward workflow.
    private void GiveReward(int amount)
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerToolController>();
        }

        if (player != null)
        {
            player.TryAddInventoryItem(RewardItemName, null, amount);
        }
    }

    // Ensures the objects, references, or configuration required for ensure catalog entries exist.
    private void EnsureCatalogEntries()
    {
        CraftingCatalog catalog = player != null ? player.CraftingCatalog : FindObjectOfType<CraftingCatalog>();
        if (catalog == null)
        {
            return;
        }

        List<CraftingItemDefinition> definitions = new List<CraftingItemDefinition>(catalog.itemDefinitions ?? new CraftingItemDefinition[0]);
        AddOrUpdateDefinition(definitions, "forest_guide", "Forest Guide", ToolCategory.Materials, 64, Resources.Load<GameObject>("RuntimePrefabs/Held_ForestGuide"));
        AddOrUpdateDefinition(definitions, "forest_heart", "Forest Heart", ToolCategory.Materials, 1, Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeart"));
        AddOrUpdateDefinition(definitions, "forest_heart_detector", "Forest Heart Detector", ToolCategory.Tools, 1, Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector"));
        catalog.itemDefinitions = definitions.ToArray();

        List<CraftingRecipe> recipes = new List<CraftingRecipe>(catalog.recipes ?? new CraftingRecipe[0]);
        bool hasDetectorRecipe = false;
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].recipeId == "bench_forest_heart_detector")
            {
                hasDetectorRecipe = true;
                break;
            }
        }

        if (!hasDetectorRecipe)
        {
            recipes.Add(new CraftingRecipe
            {
                recipeId = "bench_forest_heart_detector",
                displayName = "Forest Heart Detector",
                outputItemId = "forest_heart_detector",
                outputCount = 1,
                requiresWorkbench = true,
                station = CraftingStation.Workbench,
                description = "30 Forest Guide + 6 Iron Ore => Forest Heart Detector",
                ingredients = new[]
                {
                    new RecipeIngredient { itemId = "forest_guide", amount = 30 },
                    new RecipeIngredient { itemId = "iron_ore", amount = 6 }
                }
            });
            catalog.recipes = recipes.ToArray();
        }

        bool hasForestHeartRecipe = false;
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].recipeId == "forge_forest_heart")
            {
                hasForestHeartRecipe = true;
                break;
            }
        }

        if (!hasForestHeartRecipe)
        {
            recipes.Add(new CraftingRecipe
            {
                recipeId = "forge_forest_heart",
                displayName = "Forest Heart",
                outputItemId = "forest_heart",
                outputCount = 1,
                requiresWorkbench = false,
                station = CraftingStation.Forge,
                description = "30 Forest Guide + 6 Iron Ore => Forest Heart",
                ingredients = new[]
                {
                    new RecipeIngredient { itemId = "forest_guide", amount = 30 },
                    new RecipeIngredient { itemId = "iron_ore", amount = 6 }
                }
            });
            catalog.recipes = recipes.ToArray();
        }

        catalog.RebuildLookup();
    }

    // Ensures the objects, references, or configuration required for ensure testing items exist.
    private void EnsureTestingItems()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerToolController>();
        }

        if (player == null)
        {
            return;
        }

        int guideCount = player.GetItemCount("forest_guide");
        if (guideCount < 30)
        {
            player.TryAddInventoryItem("Forest Guide", null, 30 - guideCount);
        }

        if (player.GetItemCount("forest_heart_detector") <= 0)
        {
            player.TryAddInventoryItem("Forest Heart Detector", Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector"), 1);
        }
    }

    // Adds, spawns, or attaches the objects and data for add or update definition.
    private static void AddOrUpdateDefinition(List<CraftingItemDefinition> definitions, string itemId, string displayName, ToolCategory category, int maxStack, GameObject prefab)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null && definitions[i].itemId == itemId)
            {
                definitions[i].displayName = displayName;
                definitions[i].category = category;
                definitions[i].maxStack = maxStack;
                definitions[i].placeable = false;
                definitions[i].placeableType = PlaceableType.None;
                if (itemId == "forest_heart")
                {
                    definitions[i].heldLocalPosition = new Vector3(0.03f, 0.15f, definitions[i].heldLocalPosition.z);
                }
                else if (itemId == "forest_guide")
                {
                    definitions[i].heldLocalPosition = new Vector3(0f, 0.1f, definitions[i].heldLocalPosition.z);
                }
                else if (itemId == "forest_heart_detector")
                {
                    definitions[i].heldLocalPosition = new Vector3(definitions[i].heldLocalPosition.x, 0.1f, definitions[i].heldLocalPosition.z);
                }

                if (prefab != null)
                {
                    definitions[i].heldPrefab = prefab;
                    definitions[i].worldPrefab = prefab;
                }

                return;
            }
        }

        definitions.Add(new CraftingItemDefinition
        {
            itemId = itemId,
            displayName = displayName,
            category = category,
            holdPose = ToolHoldPose.OneHandTool,
            heldLocalPosition = GetDefaultHeldPosition(itemId),
            heldLocalEuler = new Vector3(20f, 150f, -12f),
            heldLocalScale = Vector3.one * 0.24f,
            maxStack = maxStack,
            placeable = false,
            placeableType = PlaceableType.None,
            heldPrefab = prefab,
            worldPrefab = prefab
        });
    }

    // Creates or rebuilds the runtime objects, assets, or UI for build ui.
    private void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("QuestCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObject.transform, false);
        }

        panel = CreatePanel("QuestPanel", canvas.transform, new Vector2(0f, -54f), new Vector2(814f, 610f), Color.white, AnchorPreset.TopCenter, "task_transparent");
        panel.SetActive(false);
        GameObject progressPatch = CreatePanel("ProgressPatch", panel.transform, new Vector2(438f, -116f), new Vector2(24f, 24f), new Color(0.18f, 0.075f, 0.02f, 1f));
        progressText = CreateText("ProgressValue", progressPatch.transform, new Vector2(0f, 0f), new Vector2(24f, 24f), "0", 19, FontStyle.Bold);
        progressText.alignment = TextAnchor.MiddleCenter;
        progressText.color = new Color(1f, 0.95f, 0.76f, 1f);

        checkmarkTexts = new Text[questTexts.Length];
        for (int i = 0; i < checkmarkTexts.Length; i++)
        {
            checkmarkTexts[i] = CreateText("TaskCheck_" + i, panel.transform, new Vector2(126f, -154f - i * 35.5f), new Vector2(34f, 30f), string.Empty, 26, FontStyle.Bold);
            checkmarkTexts[i].alignment = TextAnchor.MiddleCenter;
            checkmarkTexts[i].color = new Color(0.63f, 1f, 0.25f, 1f);
        }

        hintPanel = CreatePanel("QuestHint", canvas.transform, new Vector2(0f, -24f), new Vector2(300f, 68f), Color.white, AnchorPreset.TopCenter, "frame");
        Text hintText = CreateText("HintText", hintPanel.transform, new Vector2(42f, -17f), new Vector2(216f, 36f), "L  OBJECTIVES", 22, FontStyle.Bold);
        hintText.color = new Color(0.78f, 1f, 0.64f, 1f);

        toastText = CreateText("QuestToast", canvas.transform, new Vector2(0f, -570f), new Vector2(700f, 92f), string.Empty, 25, FontStyle.Bold, AnchorPreset.TopCenter);
        toastText.color = new Color(0.72f, 1f, 0.58f, 1f);
        toastText.gameObject.SetActive(false);
    }

    // Refreshes and applies configuration or runtime state for refresh ui.
    private void RefreshUI()
    {
        if (progressText == null || checkmarkTexts == null)
        {
            return;
        }

        int finished = 0;
        for (int i = 0; i < completed.Length; i++)
        {
            if (completed[i])
            {
                finished++;
            }
        }

        progressText.text = finished.ToString();
        for (int i = 0; i < completed.Length && i < checkmarkTexts.Length; i++)
        {
            checkmarkTexts[i].text = completed[i] ? "\u2713" : string.Empty;
        }
    }

    // Calculates and returns the result for get completed snapshot.
    public bool[] GetCompletedSnapshot()
    {
        return completed != null ? (bool[])completed.Clone() : new bool[0];
    }

    public bool AreAllQuestsCompleted()
    {
        if (completed == null || completed.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < completed.Length; i++)
        {
            if (!completed[i])
            {
                return false;
            }
        }

        return true;
    }

    // Handles the restore completed workflow.
    public void RestoreCompleted(bool[] savedCompleted)
    {
        completed = new bool[questTexts.Length];
        if (savedCompleted != null)
        {
            for (int i = 0; i < completed.Length && i < savedCompleted.Length; i++)
            {
                completed[i] = savedCompleted[i];
            }
        }

        RefreshUI();
    }

    // Handles the show toast workflow.
    private void ShowToast(string message)
    {
        if (toastText == null)
        {
            return;
        }

        toastText.text = message;
        toastText.gameObject.SetActive(true);
        toastTimer = 3.2f;
    }

    private enum AnchorPreset
    {
        TopLeft,
        TopRight,
        TopCenter
    }

    // Calculates and returns the result for get anchor.
    private static Vector2 GetAnchor(AnchorPreset preset)
    {
        if (preset == AnchorPreset.TopRight)
        {
            return new Vector2(1f, 1f);
        }

        if (preset == AnchorPreset.TopCenter)
        {
            return new Vector2(0.5f, 1f);
        }

        return new Vector2(0f, 1f);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create panel.
    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color, AnchorPreset anchorPreset = AnchorPreset.TopLeft, string backgroundResource = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        Vector2 anchor = GetAnchor(anchorPreset);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchorPreset == AnchorPreset.TopCenter ? new Vector2(0.5f, 1f) : anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        if (!string.IsNullOrEmpty(backgroundResource))
        {
            Texture2D texture = Resources.Load<Texture2D>(backgroundResource);
            if (texture != null)
            {
                image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                image.color = Color.white;
            }
        }

        return obj;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create text.
    private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string text, int fontSize, FontStyle style, AnchorPreset anchorPreset = AnchorPreset.TopLeft)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        Vector2 anchor = GetAnchor(anchorPreset);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchorPreset == AnchorPreset.TopCenter ? new Vector2(0.5f, 1f) : anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Text label = obj.AddComponent<Text>();
        label.font = GetBuiltinUIFont();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        return label;
    }

    // Calculates and returns the result for get builtin uifont.
    private static Font GetBuiltinUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    // Calculates and returns the result for get default held position.
    private static Vector3 GetDefaultHeldPosition(string itemId)
    {
        if (itemId == "forest_heart")
        {
            return new Vector3(0.03f, 0.15f, 0.05f);
        }

        if (itemId == "forest_guide")
        {
            return new Vector3(0f, 0.1f, 0.05f);
        }

        if (itemId == "forest_heart_detector")
        {
            return new Vector3(0.03f, 0.1f, 0.05f);
        }

        return new Vector3(0.03f, -0.02f, 0.05f);
    }
}

