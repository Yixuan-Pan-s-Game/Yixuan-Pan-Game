using System.Collections.Generic;
using UnityEngine;

public enum ToolActionType
{
    None,
    ChopTree,
    MineRock
}

public enum ToolCategory
{
    Tools,
    Melee,
    Materials,
    Crafted,
    Consumable,
    Armor,
    Ammo
}

public enum ToolHoldPose
{
    OneHandTool
}

[System.Serializable]
// tool slot script that owns this feature's runtime behavior.
public class ToolSlot
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: itemId.
    public string itemId;
    // Runtime flag that drives control flow, UI state, or gameplay availability: displayName.
    public string displayName;
    // Asset reference used for spawning, rendering, audio, or animation: prefab.
    public GameObject prefab;
    // Asset reference used for spawning, rendering, audio, or animation: worldPrefab.
    public GameObject worldPrefab;
    // Identifier or category used for lookup, routing, or state selection: actionType.
    public ToolActionType actionType;
    // Identifier or category used for lookup, routing, or state selection: category.
    public ToolCategory category;
    // Important runtime data or configuration used by this component: holdPose.
    public ToolHoldPose holdPose;
    // Spatial value used for positioning, rotation, scale, or collision math: heldLocalPosition.
    public Vector3 heldLocalPosition;
    // Spatial value used for positioning, rotation, scale, or collision math: heldLocalEuler.
    public Vector3 heldLocalEuler;
    // Spatial value used for positioning, rotation, scale, or collision math: heldLocalScale.
    public Vector3 heldLocalScale = Vector3.one;
    public Vector3 rightHandGripLocal = new Vector3(-0.24f, -0.03f, -0.02f);
    public Vector3 leftHandGripLocal = new Vector3(0.22f, -0.02f, 0.02f);
    // Inventory or crafting data for items, recipes, slots, or stack counts: stackCount.
    public int stackCount = 1;
    // Inventory or crafting data for items, recipes, slots, or stack counts: maxStack.
    public int maxStack = 1;
    // Important runtime data or configuration used by this component: placeable.
    public bool placeable;
    // Identifier or category used for lookup, routing, or state selection: placeableType.
    public PlaceableType placeableType;
    // Spatial value used for positioning, rotation, scale, or collision math: worldEulerOffset.
    public Vector3 worldEulerOffset;
    // Spatial value used for positioning, rotation, scale, or collision math: worldScale.
    public Vector3 worldScale = Vector3.one;
    // Gameplay stat that affects damage, health, healing, defense, or durability: durability.
    public int durability;
    // Gameplay stat that affects damage, health, healing, defense, or durability: maxDurability.
    public int maxDurability;
    // Gameplay stat that affects damage, health, healing, defense, or durability: damage.
    public int damage;
    // Gameplay stat that affects damage, health, healing, defense, or durability: defense.
    public int defense;
    // Gameplay stat that affects damage, health, healing, defense, or durability: healAmount.
    public int healAmount;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: harvestSeconds.
    public float harvestSeconds;
    // Cached component or scene reference to avoid repeated lookups: nonGunDirectionFlipped.
    [HideInInspector] public bool nonGunDirectionFlipped;
}

// Central controller for the player inventory, tools, harvesting, placement, equipment, and interactions.
public class PlayerToolController : MonoBehaviour
{
    [Header("Inventory")]
    // Inventory or crafting data for items, recipes, slots, or stack counts: slots.
    public ToolSlot[] slots;
    // Inventory or crafting data for items, recipes, slots, or stack counts: hotbarSize.
    public int hotbarSize = 9;
    // Inventory or crafting data for items, recipes, slots, or stack counts: hotbarSlotIndices.
    public int[] hotbarSlotIndices;
    // Runtime flag that drives control flow, UI state, or gameplay availability: hasBackpack.
    public bool hasBackpack;
    // Important runtime data or configuration used by this component: noBackpackColumns.
    public int noBackpackColumns = 4;
    // Important runtime data or configuration used by this component: noBackpackRows.
    public int noBackpackRows = 4;
    // Important runtime data or configuration used by this component: backpackColumns.
    public int backpackColumns = 9;
    // Important runtime data or configuration used by this component: backpackRows.
    public int backpackRows = 8;
    // Important runtime data or configuration used by this component: furnaceFuelCharges.
    public int furnaceFuelCharges;
    // Important runtime data or configuration used by this component: equippedHelmet.
    public ToolSlot equippedHelmet;
    // Important runtime data or configuration used by this component: equippedArmor.
    public ToolSlot equippedArmor;

    [Header("Equipped Armor Visual Tuning")]
    // Spatial value used for positioning, rotation, scale, or collision math: equippedHelmetLocalPosition.
    public Vector3 equippedHelmetLocalPosition = Vector3.zero;
    // Spatial value used for positioning, rotation, scale, or collision math: equippedHelmetLocalEuler.
    public Vector3 equippedHelmetLocalEuler = Vector3.zero;
    // Spatial value used for positioning, rotation, scale, or collision math: equippedHelmetLocalScale.
    public Vector3 equippedHelmetLocalScale = Vector3.one * 1.025f;
    // Spatial value used for positioning, rotation, scale, or collision math: equippedArmorLocalPosition.
    public Vector3 equippedArmorLocalPosition = Vector3.zero;
    // Spatial value used for positioning, rotation, scale, or collision math: equippedArmorLocalEuler.
    public Vector3 equippedArmorLocalEuler = Vector3.zero;
    // Spatial value used for positioning, rotation, scale, or collision math: equippedArmorLocalScale.
    public Vector3 equippedArmorLocalScale = Vector3.one * 1.045f;

    [Header("References")]
    // Layer or mask filter used by physics queries or rendering: playerCamera.
    public Camera playerCamera;
    // Cached component or scene reference to avoid repeated lookups: visualRoot.
    public Transform visualRoot;
    // Inventory or crafting data for items, recipes, slots, or stack counts: craftingCatalog.
    public CraftingCatalog craftingCatalog;

    [Header("Use")]
    // Distance or radius used for detection, interaction, or physics checks: useRange.
    public float useRange = 4f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: useCooldown.
    public float useCooldown = 0.35f;
    // Gameplay stat that affects damage, health, healing, defense, or durability: toolDamage.
    public int toolDamage = 1;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: treeChopHoldSeconds.
    public float treeChopHoldSeconds = 5f;
    // Distance or radius used for detection, interaction, or physics checks: treeChopAimRadius.
    public float treeChopAimRadius = 0.45f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: treeChopTargetGraceSeconds.
    public float treeChopTargetGraceSeconds = 0.35f;
    // Distance or radius used for detection, interaction, or physics checks: treeChopSnapRadius.
    public float treeChopSnapRadius = 1.35f;
    // Important runtime data or configuration used by this component: treeChopSnapSamples.
    public int treeChopSnapSamples = 6;
    // Distance or radius used for detection, interaction, or physics checks: treeChopCameraAcquireRange.
    public float treeChopCameraAcquireRange = 18f;
    // Distance or radius used for detection, interaction, or physics checks: treeChopPlayerReachPadding.
    public float treeChopPlayerReachPadding = 1.25f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: pickupPlacedHoldSeconds.
    public float pickupPlacedHoldSeconds = 3f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: harvestSwingDuration.
    public float harvestSwingDuration = 1.05f;

    [Header("Audio")]
    // Asset reference used for spawning, rendering, audio, or animation: treeHitClipResourcePath.
    public string treeHitClipResourcePath = "hits/Hits/Wood Hits 1";
    // Asset reference used for spawning, rendering, audio, or animation: rockHitClipResourcePath.
    public string rockHitClipResourcePath = "hits/Hits/Hammer Hit";
    // Important runtime data or configuration used by this component: harvestHitVolume.
    public float harvestHitVolume = 0f;

    [Header("Debug")]
    // Runtime flag that drives control flow, UI state, or gameplay availability: showHarvestDebug.
    public bool showHarvestDebug = true;

    [Header("Selected Tool Grip Tuning")]
    [Tooltip("Runtime slot index used for tuning the selected tool grip. You can edit it manually to choose another slot.")]
    // Inventory or crafting data for items, recipes, slots, or stack counts: gripTuningSlotIndex.
    public int gripTuningSlotIndex;
    [Tooltip("Read-only display name for the tool currently being tuned.")]
    // Inventory or crafting data for items, recipes, slots, or stack counts: gripTuningSlotName.
    public string gripTuningSlotName;
    [Tooltip("When enabled, writes the grip values below back to the selected slot and applies them live.")]
    // Inventory or crafting data for items, recipes, slots, or stack counts: applyGripTuningToSelectedSlot.
    public bool applyGripTuningToSelectedSlot;
    // Spatial value used for positioning, rotation, scale, or collision math: gripHeldLocalPosition.
    public Vector3 gripHeldLocalPosition;
    // Spatial value used for positioning, rotation, scale, or collision math: gripHeldLocalEuler.
    public Vector3 gripHeldLocalEuler;
    // Spatial value used for positioning, rotation, scale, or collision math: gripHeldLocalScale.
    public Vector3 gripHeldLocalScale = Vector3.one;
    public Vector3 gripRightHandLocal = new Vector3(-0.24f, -0.03f, -0.02f);
    public Vector3 gripLeftHandLocal = new Vector3(0.22f, -0.02f, 0.02f);
    [Tooltip("Runtime key that flips the selected tool 180 degrees around the Y axis.")]
    // Input setting or cached input value read from player controls: flipSelectedToolKey.
    public KeyCode flipSelectedToolKey = KeyCode.F;
    [Tooltip("Runtime key that flips the selected tool 180 degrees around the Z axis.")]
    // Input setting or cached input value read from player controls: rollSelectedToolKey.
    public KeyCode rollSelectedToolKey = KeyCode.G;
    [Tooltip("Runtime key that rotates the selected tool 90 degrees counterclockwise around the Z axis.")]
    // Input setting or cached input value read from player controls: rollSelectedToolLeftKey.
    public KeyCode rollSelectedToolLeftKey = KeyCode.H;

    private readonly List<GameObject> heldToolInstances = new List<GameObject>();
    // Important runtime data or configuration used by this component: handAnchor.
    private Transform handAnchor;
    // Runtime flag that drives control flow, UI state, or gameplay availability: selectedIndex.
    [SerializeField] private int selectedIndex;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: nextUseTime.
    private float nextUseTime;
    // Inventory or crafting data for items, recipes, slots, or stack counts: hotbarUI.
    private HotbarUI hotbarUI;
    // Important runtime data or configuration used by this component: backpackUI.
    private BackpackUI backpackUI;
    // Cached component or scene reference to avoid repeated lookups: characterAnimator.
    private CharacterRunAnimator characterAnimator;
    // Runtime flag that drives control flow, UI state, or gameplay availability: activeSwingRoutine.
    private Coroutine activeSwingRoutine;
    // Runtime flag that drives control flow, UI state, or gameplay availability: pendingHarvestHitSoundRoutine.
    private Coroutine pendingHarvestHitSoundRoutine;
    // Cached component or scene reference to avoid repeated lookups: harvestAudioSource.
    private AudioSource harvestAudioSource;
    // Asset reference used for spawning, rendering, audio, or animation: treeHitClip.
    private AudioClip treeHitClip;
    // Asset reference used for spawning, rendering, audio, or animation: rockHitClip.
    private AudioClip rockHitClip;
    // Current interaction target or gameplay object being processed: heldChopTarget.
    private HarvestableResource heldChopTarget;
    // Identifier or category used for lookup, routing, or state selection: heldHarvestType.
    private HarvestResourceType heldHarvestType;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: heldChopTime.
    private float heldChopTime;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastChopTargetSeenTime.
    private float lastChopTargetSeenTime;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: nextChopSwingTime.
    private float nextChopSwingTime;
    // Runtime flag that drives control flow, UI state, or gameplay availability: wasHoldingUse.
    private bool wasHoldingUse;
    // Cached component or scene reference to avoid repeated lookups: harvestDebugText.
    private string harvestDebugText = string.Empty;
    // Asset reference used for spawning, rendering, audio, or animation: woodMaterial.
    private Material woodMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: metalMaterial.
    private Material metalMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: darkMetalMaterial.
    private Material darkMetalMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: ironSwordMaterial.
    private Material ironSwordMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: copperSwordMaterial.
    private Material copperSwordMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: goldSwordMaterial.
    private Material goldSwordMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: rubySwordMaterial.
    private Material rubySwordMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: sapphireSwordMaterial.
    private Material sapphireSwordMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: emeraldSwordMaterial.
    private Material emeraldSwordMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: heldDropWoodMaterial.
    private Material heldDropWoodMaterial;
    // Asset reference used for spawning, rendering, audio, or animation: heldDropStoneMaterial.
    private Material heldDropStoneMaterial;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeWoodPrefab.
    private GameObject runtimeWoodPrefab;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeStickPrefab.
    private GameObject runtimeStickPrefab;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeIronBarPrefab.
    private GameObject runtimeIronBarPrefab;
    // Current interaction target or gameplay object being processed: heldPickupTarget.
    private PlaceableObject heldPickupTarget;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: heldPickupStartedAt.
    private float heldPickupStartedAt;
    // Current interaction target or gameplay object being processed: heldHerbTarget.
    private Transform heldHerbTarget;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: heldHerbStartedAt.
    private float heldHerbStartedAt;
    // Runtime flag that drives control flow, UI state, or gameplay availability: equippedHelmetVisual.
    private GameObject equippedHelmetVisual;
    // Runtime flag that drives control flow, UI state, or gameplay availability: equippedArmorVisual.
    private GameObject equippedArmorVisual;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastEquippedHelmetLocalPosition.
    private Vector3 lastEquippedHelmetLocalPosition;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastEquippedHelmetLocalEuler.
    private Vector3 lastEquippedHelmetLocalEuler;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastEquippedHelmetLocalScale.
    private Vector3 lastEquippedHelmetLocalScale;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastEquippedArmorLocalPosition.
    private Vector3 lastEquippedArmorLocalPosition;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastEquippedArmorLocalEuler.
    private Vector3 lastEquippedArmorLocalEuler;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastEquippedArmorLocalScale.
    private Vector3 lastEquippedArmorLocalScale;
    // Current interaction target or gameplay object being processed: heldShearTarget.
    private SheepShearable heldShearTarget;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: heldShearStartedAt.
    private float heldShearStartedAt;
    // Asset reference used for spawning, rendering, audio, or animation: hudFrameTexture.
    private Texture2D hudFrameTexture;
    // Important runtime data or configuration used by this component: hudTitleStyle.
    private GUIStyle hudTitleStyle;
    // Important runtime data or configuration used by this component: hudLabelStyle.
    private GUIStyle hudLabelStyle;
    private ToolSlot[] configuredSlotTemplates = new ToolSlot[0];

    // Read-only state exposed to other systems: SelectedIndex.
    public int SelectedIndex => selectedIndex;
    // Read-only state exposed to other systems: SelectedInventoryIndex.
    public int SelectedInventoryIndex => GetSelectedInventoryIndex();
    // Read-only state exposed to other systems: HotbarSlotCount.
    public int HotbarSlotCount => Mathf.Clamp(hotbarSize <= 0 ? 9 : hotbarSize, 1, 9);
    // Read-only state exposed to other systems: Slots.
    public ToolSlot[] Slots => slots;
    // Read-only state exposed to other systems: EquippedHelmet.
    public ToolSlot EquippedHelmet => equippedHelmet;
    // Read-only state exposed to other systems: EquippedArmor.
    public ToolSlot EquippedArmor => equippedArmor;
    // Read-only state exposed to other systems: CraftingCatalog.
    public CraftingCatalog CraftingCatalog => craftingCatalog;
    // Read-only state exposed to other systems: InventorySlotCapacity.
    public int InventorySlotCapacity => (hasBackpack ? backpackColumns * backpackRows : noBackpackColumns * noBackpackRows);

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        backpackColumns = 9;
        backpackRows = 8;

        useRange = Mathf.Max(useRange, 6f);
        treeChopHoldSeconds = Mathf.Max(0.5f, treeChopHoldSeconds);
        treeChopAimRadius = Mathf.Max(0.35f, treeChopAimRadius);
        treeChopSnapRadius = Mathf.Max(treeChopAimRadius, treeChopSnapRadius);
        treeChopSnapSamples = Mathf.Clamp(treeChopSnapSamples, 2, 12);
        treeChopCameraAcquireRange = Mathf.Max(useRange + 4f, treeChopCameraAcquireRange);
        treeChopPlayerReachPadding = Mathf.Max(0.5f, treeChopPlayerReachPadding);
        harvestSwingDuration = Mathf.Max(0.1f, harvestSwingDuration);
        characterAnimator = GetComponent<CharacterRunAnimator>();
        CreateRuntimeMaterials();

        visualRoot = ResolveVisualRoot();
        if (characterAnimator != null && visualRoot != null)
        {
            characterAnimator.SetVisualRoot(visualRoot);
        }

        if (craftingCatalog == null)
        {
            craftingCatalog = FindObjectOfType<CraftingCatalog>();
        }

        if (craftingCatalog == null)
        {
            craftingCatalog = new GameObject("CraftingCatalog").AddComponent<CraftingCatalog>();
        }

        craftingCatalog.EnsureDefaultsIfEmpty();
        MoveForestHeartDetectorRecipeToForge();
        craftingCatalog.RebuildLookup();
        configuredSlotTemplates = InventoryUtility.Compact(slots);
        ConfigureReleaseStartingInventory();
        EnsureToolActions();
        ApplyRuntimeSlotFixes();
        EnsureSceneSheepShearables();
        EnsureSceneHerbPickables();
        RemoveProceduralArmSwing();
        RemoveMeatFromInventory();
        RemoveRangedWeaponsFromInventory();
        ApplySavedNonGunDirectionFlip();
        EnsureHotbarMapping();
        BuildHeldTools();
        RefreshEquippedArmorVisuals();

        if (slots != null && slots.Length > 0)
        {
            SelectSlot(0);
        }
        else
        {
            Debug.LogWarning("PlayerToolController: slots is empty. Backpack can open, but it will show no tools.");
        }
    }

    private void ConfigureReleaseStartingInventory()
    {
        slots = new ToolSlot[0];
        equippedHelmet = null;
        equippedArmor = null;
        furnaceFuelCharges = 0;
        hasBackpack = false;
        AddStartingTool("wood_axe");
        AddStartingTool("wood_pickaxe");
        AddStartingTool("wood_sword");
        ResetHotbarToFirstSlots();
    }

    private void AddStartingTool(string itemId)
    {
        ToolSlot template = FindConfiguredSlotTemplate(itemId);
        ToolSlot slot = template != null
            ? InventoryUtility.CloneSlot(template, 1)
            : CreateSlotFromCatalog(itemId, 1);

        if (slot == null)
        {
            return;
        }

        ApplyRuntimeSlotFix(slot);
        slots = InventoryUtility.AddItem(slots, slot);
    }

    private ToolSlot CreateSlotFromCatalog(string itemId, int amount)
    {
        CraftingItemDefinition definition = craftingCatalog != null ? craftingCatalog.FindItem(itemId) : null;
        return definition != null ? InventoryUtility.CreateSlotFromDefinition(definition, Mathf.Max(1, amount)) : null;
    }

    // Handles the move forest heart detector recipe to forge workflow.
    private void MoveForestHeartDetectorRecipeToForge()
    {
        if (craftingCatalog == null || craftingCatalog.recipes == null) return;

        for (int i = 0; i < craftingCatalog.recipes.Length; i++)
        {
            CraftingRecipe recipe = craftingCatalog.recipes[i];
            if (recipe == null || recipe.outputItemId != "forest_heart_detector") continue;
            recipe.recipeId = "forge_forest_heart_detector";
            recipe.requiresWorkbench = false;
            recipe.station = CraftingStation.Forge;
            recipe.description = "30 Forest Guide + 6 Iron Ore => Forest Heart Detector";
        }
    }
    // Ensures the objects, references, or configuration required for ensure forest heart detector in inventory exist.
    private void EnsureForestHeartDetectorInInventory()
    {
        const string detectorItemId = "forest_heart_detector";
        if (GetItemCount(detectorItemId) > 0)
        {
            return;
        }

        if (slots == null)
        {
            slots = new ToolSlot[0];
        }

        CraftingItemDefinition definition = craftingCatalog != null ? craftingCatalog.FindItem(detectorItemId) : null;
        ToolSlot detectorSlot = definition != null
            ? InventoryUtility.CreateSlotFromDefinition(definition, 1)
            : new ToolSlot
            {
                itemId = detectorItemId,
                displayName = "Forest Heart Detector",
                prefab = Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector"),
                worldPrefab = Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector"),
                category = ToolCategory.Tools,
                holdPose = ToolHoldPose.OneHandTool,
                heldLocalPosition = new Vector3(-0.1f, -0.06f, -0.01f),
                heldLocalEuler = new Vector3(20f, 150f, -12f),
                heldLocalScale = Vector3.one * 0.26f,
                stackCount = 1,
                maxStack = 1,
                nonGunDirectionFlipped = true
            };

        hasBackpack = true;
        backpackColumns = Mathf.Max(1, backpackColumns);
        int capacity = InventorySlotCapacity;
        if (!InventoryUtility.CanAddItem(slots, detectorSlot, capacity))
        {
            backpackRows = Mathf.Max(backpackRows + 1, Mathf.CeilToInt(((slots != null ? slots.Length : 0) + 1) / (float)backpackColumns));
        }

        TryAddOrMergeInventorySlot(detectorSlot);
    }
    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && backpackUI != null)
        {
            ResetHeldHarvest();
            backpackUI.Toggle();
            return;
        }

        HandleEquippedArmorVisualTuning();
        if (BackpackUI.IsAnyOpen)
        {
            ResetHeldHarvest();
            return;
        }

        HandleSelectionInput();
        HandleGripTuningInput();
        HandleGripFlipInput();
        HandleGripRollInput();
        HandleGripRollLeftInput();
        HandleToolUseInput();
    }

    // Handles the handle tool use input workflow.
    private void HandleToolUseInput()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        ToolSlot selectedSlot = slots != null && inventoryIndex >= 0 && inventoryIndex < slots.Length
            ? slots[inventoryIndex]
            : null;
        UpdateHarvestDebugSelectedSlot(selectedSlot);

        if (!Input.GetKey(KeyCode.LeftAlt) && HandlePlacedObjectPickupHold())
        {
            return;
        }

        if (!Input.GetKey(KeyCode.LeftAlt) && HandleHerbHarvestInput())
        {
            return;
        }

        if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0) && TryHandlePlacementOrWorldInteraction(selectedSlot))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextUseTime && TryAttackEnemy(selectedSlot, inventoryIndex))
        {
            return;
        }

        if (HandleShearingInput(selectedSlot))
        {
            return;
        }

        if (TryGetContinuousHarvestTargetType(selectedSlot, out HarvestResourceType targetType))
        {
            SetHarvestDebug(targetType == HarvestResourceType.Tree
                ? "Axe hold active, trying to find tree..."
                : "Pickaxe hold active, trying to find rock...");
            HandleContinuousHarvest(targetType);
            return;
        }

        ResetHeldHarvest();
        if (selectedSlot != null && IsTreeChopTool(selectedSlot))
        {
            SetHarvestDebug("Axe equipped. Hold left mouse and aim at a tree.");
        }
        else if (selectedSlot != null && IsRockMineTool(selectedSlot))
        {
            SetHarvestDebug("Pickaxe equipped. Hold left mouse and aim at a rock.");
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextUseTime)
        {
            UseSelectedTool();
        }
    }

    // Attempts to try handle placement or world interaction and returns whether the operation succeeded.
    private bool TryHandlePlacementOrWorldInteraction(ToolSlot selectedSlot)
    {
        if (playerCamera == null)
        {
            return false;
        }

        if (TryFindWorldInteractable(out PlaceableObject placeableObject) && InteractWithPlaceableObject(placeableObject))
        {
            return true;
        }

        if (selectedSlot != null && (selectedSlot.placeable || IsTorchItem(selectedSlot)))
        {
            return TryPlaceSelectedItem(selectedSlot);
        }

        return false;
    }

    // Handles the interact with placeable object workflow.
    private bool InteractWithPlaceableObject(PlaceableObject placeableObject)
    {
        if (placeableObject == null)
        {
            return false;
        }

        if (placeableObject.placeableType == PlaceableType.Door)
        {
            placeableObject.ToggleDoor();
            return true;
        }

        if (backpackUI == null)
        {
            return false;
        }

        if (placeableObject.placeableType == PlaceableType.Workbench)
        {
            backpackUI.OpenWorkbenchCrafting(this);
            return true;
        }

        if (placeableObject.placeableType == PlaceableType.Furnace)
        {
            backpackUI.OpenStationCrafting(this, CraftingStation.Furnace);
            return true;
        }

        if (placeableObject.placeableType == PlaceableType.Forge)
        {
            backpackUI.OpenStationCrafting(this, CraftingStation.Forge);
            return true;
        }

        if (placeableObject.placeableType == PlaceableType.Chest)
        {
            StorageChest chest = placeableObject.GetComponent<StorageChest>();
            if (chest != null)
            {
                backpackUI.OpenChest(this, chest);
                return true;
            }
        }

        return false;
    }

    // Handles the handle placed object pickup hold workflow.
    private bool HandlePlacedObjectPickupHold()
    {
        if (playerCamera == null)
        {
            ResetPlacedObjectPickupHold();
            return false;
        }

        if (Input.GetMouseButtonUp(0) && heldPickupTarget != null)
        {
            PlaceableObject releasedTarget = heldPickupTarget;
            float heldSeconds = Time.time - heldPickupStartedAt;
            ResetPlacedObjectPickupHold();
            if (heldSeconds < pickupPlacedHoldSeconds)
            {
                InteractWithPlaceableObject(releasedTarget);
            }

            return true;
        }

        if (!Input.GetMouseButton(0))
        {
            ResetPlacedObjectPickupHold();
            return false;
        }

        if (!TryFindWorldInteractable(out PlaceableObject target))
        {
            ResetPlacedObjectPickupHold();
            return false;
        }

        if (Input.GetMouseButtonDown(0) || heldPickupTarget != target)
        {
            heldPickupTarget = target;
            heldPickupStartedAt = Time.time;
            return true;
        }

        if (Time.time - heldPickupStartedAt < pickupPlacedHoldSeconds)
        {
            return true;
        }

        TryPickupPlacedObject(target);
        ResetPlacedObjectPickupHold();
        return true;
    }

    // Handles the reset placed object pickup hold workflow.
    private void ResetPlacedObjectPickupHold()
    {
        heldPickupTarget = null;
        heldPickupStartedAt = 0f;
    }

    // Attempts to try pickup placed object and returns whether the operation succeeded.
    private bool TryPickupPlacedObject(PlaceableObject placeableObject)
    {
        if (placeableObject == null || string.IsNullOrEmpty(placeableObject.itemId))
        {
            return false;
        }

        ToolSlot slot = CreateSlotForPlacedObject(placeableObject);
        if (slot == null || !CanFitInventorySlot(slot))
        {
            return false;
        }

        AddOrMergeInventorySlot(slot);
        Destroy(placeableObject.gameObject);
        return true;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create slot for placed object.
    private ToolSlot CreateSlotForPlacedObject(PlaceableObject placeableObject)
    {
        CraftingItemDefinition definition = craftingCatalog != null ? craftingCatalog.FindItem(placeableObject.itemId) : null;
        ToolSlot slot = definition != null
            ? InventoryUtility.CreateSlotFromDefinition(definition, 1)
            : new ToolSlot
            {
                itemId = placeableObject.itemId,
                displayName = string.IsNullOrEmpty(placeableObject.displayName) ? placeableObject.itemId : placeableObject.displayName,
                stackCount = 1,
                maxStack = 64,
                category = ToolCategory.Crafted,
                placeable = true,
                placeableType = placeableObject.placeableType,
                heldLocalScale = Vector3.one * 0.35f,
                worldScale = Vector3.one
            };

        ApplyRuntimeSlotFix(slot);
        return slot;
    }

    // Attempts to try find world interactable and returns whether the operation succeeded.
    private bool TryFindWorldInteractable(out PlaceableObject placeableObject)
    {
        placeableObject = null;
        if (playerCamera == null)
        {
            return false;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Max(18f, useRange + 12f), ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            PlaceableObject candidate = hit.collider.GetComponentInParent<PlaceableObject>();
            if (candidate == null)
            {
                continue;
            }

            if (IsLiveEnemyPlaceable(candidate))
            {
                continue;
            }

            placeableObject = candidate;
            return true;
        }

        return false;
    }

    // Calculates and returns the result for is live enemy placeable.
    private static bool IsLiveEnemyPlaceable(PlaceableObject placeableObject)
    {
        if (placeableObject == null)
        {
            return false;
        }

        EnemyHealth enemy = placeableObject.GetComponent<EnemyHealth>();
        return enemy != null && !enemy.IsDead;
    }

    // Handles the register hotbar ui workflow.
    public void RegisterHotbarUI(HotbarUI ui)
    {
        hotbarUI = ui;
        if (hotbarUI == null)
        {
            return;
        }

        if (hotbarUI != null && hotbarUI.SlotCount != HotbarSlotCount)
        {
            hotbarUI.Build(HotbarSlotCount);
        }

        hotbarUI.Refresh(this);
    }

    // Handles the register backpack ui workflow.
    public void RegisterBackpackUI(BackpackUI ui)
    {
        backpackUI = ui;
        if (backpackUI == null)
        {
            return;
        }

        backpackUI.Refresh(this);
    }

    // Handles the handle selection input workflow.
    private void HandleSelectionInput()
    {
        if (slots == null || slots.Length == 0)
        {
            return;
        }

        EnsureHotbarMapping();
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.01f)
        {
            SelectSlot((selectedIndex - 1 + hotbarSize) % hotbarSize);
        }
        else if (scroll < -0.01f)
        {
            SelectSlot((selectedIndex + 1) % hotbarSize);
        }

        for (int i = 0; i < Mathf.Min(hotbarSize, 9); i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                SelectSlot(i);
            }
        }
    }

    // Attempts to try place selected item and returns whether the operation succeeded.
    private bool TryPlaceSelectedItem(ToolSlot slot)
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (slot == null || inventoryIndex < 0 || playerCamera == null)
        {
            return false;
        }

        if (!slot.placeable && !IsTorchItem(slot))
        {
            return false;
        }

        if (IsTorchItem(slot))
        {
            slot.placeable = true;
            slot.placeableType = PlaceableType.Torch;
            slot.worldScale = Vector3.one * 0.85f;
        }

        GameObject worldPrefab = ResolveSlotPrefab(slot.worldPrefab != null ? slot.worldPrefab : slot.prefab, slot);
        if (worldPrefab == null)
        {
            return false;
        }

        if (!TryGetPlacementHit(slot, out RaycastHit hit))
        {
            return false;
        }

        Vector3 placePosition = GetSafePlacementPosition(hit.point);
        if (slot.placeableType == PlaceableType.Torch && hit.normal.y < 0.65f)
        {
            placePosition = hit.point + hit.normal * 0.12f;
        }

        Quaternion rotation = GetPlacementRotation(slot, hit.normal);
        GameObject placed = Instantiate(worldPrefab, placePosition, rotation);
        placed.name = slot.displayName + "_Placed";
        ApplyPlacedTransformOverrides(placed, slot);
        EnsurePlaceableSetup(placed, slot);
        if (slot.placeableType != PlaceableType.Torch || hit.normal.y >= 0.65f)
        {
            SnapPlacedObjectToGround(placed, hit.point.y);
        }

        ConsumeInventoryIndex(inventoryIndex, 1);
        return true;
    }

    // Ensures the objects, references, or configuration required for ensure scene herb pickables exist.
    private void EnsureSceneHerbPickables()
    {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || !target.name.ToLowerInvariant().Contains("agavelow"))
            {
                continue;
            }

            if (target.GetComponentInChildren<Collider>(true) != null)
            {
                continue;
            }

            if (!TryGetRenderBounds(target.gameObject, out Bounds bounds))
            {
                continue;
            }

            BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
            collider.center = target.InverseTransformPoint(bounds.center);
            collider.size = DivideByLossyScale(bounds.size, target.lossyScale);
            collider.isTrigger = false;
        }
    }
    // Handles the divide by lossy scale workflow.
    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            size.x / Mathf.Max(0.0001f, lossyScale.x),
            size.y / Mathf.Max(0.0001f, lossyScale.y),
            size.z / Mathf.Max(0.0001f, lossyScale.z));
    }
    // Handles the handle herb harvest input workflow.
    private bool HandleHerbHarvestInput()
    {
        if (!Input.GetMouseButton(0))
        {
            ResetHeldHerbHarvest();
            return false;
        }

        if (!TryFindHerbTarget(out Transform herbTarget))
        {
            ResetHeldHerbHarvest();
            return false;
        }

        if (heldHerbTarget != herbTarget)
        {
            heldHerbTarget = herbTarget;
            heldHerbStartedAt = Time.time;
        }

        float progress = Mathf.Clamp01((Time.time - heldHerbStartedAt) / 3f);
        SetHarvestDebug("Picking Herb " + Mathf.RoundToInt(progress * 100f) + "%");
        if (progress < 1f)
        {
            return true;
        }

        if (TryAddInventoryItem("Herb", null, 3))
        {
            Destroy(herbTarget.gameObject);
            SetHarvestDebug("Picked Herb x3.");
        }
        else
        {
            SetHarvestDebug("Cannot pick herb: inventory is full.");
        }

        ResetHeldHerbHarvest();
        return true;
    }

    // Attempts to try find herb target and returns whether the operation succeeded.
    private bool TryFindHerbTarget(out Transform herbTarget)
    {
        herbTarget = null;
        if (playerCamera == null)
        {
            return false;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.SphereCastAll(ray, 0.35f, Mathf.Max(useRange, 5f), ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider.transform.IsChildOf(transform))
            {
                continue;
            }

            Transform target = FindHerbRoot(hits[i].collider.transform);
            if (target != null)
            {
                herbTarget = target;
                return true;
            }
        }

        return false;
    }

    // Finds, loads, or caches the references needed for find herb root.
    private static Transform FindHerbRoot(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("agavelow"))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    // Handles the reset held herb harvest workflow.
    private void ResetHeldHerbHarvest()
    {
        heldHerbTarget = null;
        heldHerbStartedAt = 0f;
    }

    // Calculates and returns the result for is torch item.
    private static bool IsTorchItem(ToolSlot slot)
    {
        return slot != null && InventoryUtility.GetItemId(slot) == "torch";
    }

    // Calculates and returns the result for get safe placement position.
    private Vector3 GetSafePlacementPosition(Vector3 hitPoint)
    {
        Vector3 flattenedForward = transform.forward;
        flattenedForward.y = 0f;
        if (flattenedForward.sqrMagnitude < 0.001f)
        {
            flattenedForward = Vector3.forward;
        }

        flattenedForward.Normalize();
        Vector3 playerFeet = transform.position;
        playerFeet.y = hitPoint.y;
        Vector3 safePoint = hitPoint;
        Vector3 toPlacement = safePoint - playerFeet;
        toPlacement.y = 0f;
        float minimumDistance = 1.85f;
        if (toPlacement.magnitude < minimumDistance)
        {
            safePoint = playerFeet + flattenedForward * minimumDistance;
            safePoint.y = hitPoint.y;
        }

        return safePoint;
    }

    // Attempts to try get placement hit and returns whether the operation succeeded.
    private bool TryGetPlacementHit(ToolSlot slot, out RaycastHit placementHit)
    {
        placementHit = default;
        bool placingTorch = IsTorchItem(slot) || (slot != null && slot.placeableType == PlaceableType.Torch);
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Max(10f, useRange + 10f), ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.collider.GetComponentInParent<PlaceableObject>() != null)
            {
                continue;
            }

            if (!placingTorch && hit.normal.y < 0.55f)
            {
                continue;
            }

            if (placingTorch && hit.normal.y < -0.2f)
            {
                continue;
            }

            placementHit = hit;
            return true;
        }

        if (placingTorch && TryGetTorchGroundFallback(out placementHit))
        {
            return true;
        }

        return false;
    }

    // Attempts to try get torch ground fallback and returns whether the operation succeeded.
    private bool TryGetTorchGroundFallback(out RaycastHit placementHit)
    {
        placementHit = default;
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        Vector3 origin = transform.position + forward * 2.2f + Vector3.up * 4f;
        Ray ray = new Ray(origin, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 12f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null
                || hit.collider.isTrigger
                || hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<PlaceableObject>() != null
                || hit.normal.y < 0.35f)
            {
                continue;
            }

            placementHit = hit;
            return true;
        }

        return false;
    }

    // Ensures the objects, references, or configuration required for ensure placeable setup exist.
    private void EnsurePlaceableSetup(GameObject placed, ToolSlot slot)
    {
        if (placed == null || slot == null)
        {
            return;
        }

        ApplyCraftedPlaceableVisuals(placed, slot);
        StripPlacementPhysics(placed);
        AddPlacementCollider(placed);

        PlaceableObject placeableObject = placed.GetComponent<PlaceableObject>();
        if (placeableObject == null)
        {
            placeableObject = placed.AddComponent<PlaceableObject>();
        }

        placeableObject.itemId = InventoryUtility.GetItemId(slot);
        placeableObject.displayName = slot.displayName;
        placeableObject.placeableType = slot.placeableType;

        if (placeableObject.placeableType == PlaceableType.Torch)
        {
            TorchFireLight fire = placed.GetComponent<TorchFireLight>();
            if (fire == null)
            {
                fire = placed.AddComponent<TorchFireLight>();
            }

            fire.LocalOffset = new Vector3(0f, 1.05f, 0f);
        }

        if (slot.placeableType == PlaceableType.Chest && placed.GetComponent<StorageChest>() == null)
        {
            placed.AddComponent<StorageChest>();
        }
    }

    // Handles the strip placement physics workflow.
    private static void StripPlacementPhysics(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            Destroy(colliders[i]);
        }

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = rigidbodies.Length - 1; i >= 0; i--)
        {
            Destroy(rigidbodies[i]);
        }

        Joint[] joints = root.GetComponentsInChildren<Joint>(true);
        for (int i = joints.Length - 1; i >= 0; i--)
        {
            Destroy(joints[i]);
        }
    }

    // Adds, spawns, or attaches the objects and data for add placement collider.
    private static void AddPlacementCollider(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        collider.size = new Vector3(
            bounds.size.x / Mathf.Max(0.0001f, root.transform.lossyScale.x),
            bounds.size.y / Mathf.Max(0.0001f, root.transform.lossyScale.y),
            bounds.size.z / Mathf.Max(0.0001f, root.transform.lossyScale.z));
    }

    // Sets state, selection, or placement data for snap placed object to ground.
    private static void SnapPlacedObjectToGround(GameObject placed, float groundY)
    {
        if (placed == null)
        {
            return;
        }

        Renderer[] renderers = placed.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = default;
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        if (!found)
        {
            return;
        }

        float offset = groundY - bounds.min.y;
        Vector3 position = placed.transform.position;
        position.y += offset;
        placed.transform.position = position;
    }

    // Calculates and returns the result for get placement rotation.
    private Quaternion GetPlacementRotation(ToolSlot slot, Vector3 hitNormal)
    {
        float yaw = Mathf.Round(transform.eulerAngles.y / 90f) * 90f;
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        if (slot == null)
        {
            return rotation;
        }

        if (slot.placeableType == PlaceableType.Door)
        {
            return Quaternion.Euler(0f, yaw, 0f);
        }

        if (slot.placeableType == PlaceableType.Torch && hitNormal.y < 0.65f)
        {
            Vector3 wallNormal = new Vector3(hitNormal.x, 0f, hitNormal.z);
            if (wallNormal.sqrMagnitude < 0.001f)
            {
                return rotation;
            }

            wallNormal.Normalize();
            Vector3 torchUp = (wallNormal + Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(-wallNormal, torchUp);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, torchUp);
            }

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(forward.normalized, torchUp);
        }

        return rotation;
    }

    // Refreshes and applies configuration or runtime state for apply placed transform overrides.
    private static void ApplyPlacedTransformOverrides(GameObject placed, ToolSlot slot)
    {
        if (placed == null || slot == null)
        {
            return;
        }

        Vector3 scale = GetPlacementWorldScale(slot);
        placed.transform.localScale = scale;
        placed.transform.rotation *= Quaternion.Euler(slot.worldEulerOffset);
    }

    // Handles the use selected tool workflow.
    private void UseSelectedTool()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (Input.GetKey(KeyCode.LeftAlt) || slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length)
        {
            return;
        }

        ToolSlot selectedSlot = slots[inventoryIndex];
        if (IsTreeChopTool(selectedSlot) || IsRockMineTool(selectedSlot))
        {
            return;
        }

        if (selectedSlot.actionType == ToolActionType.None || playerCamera == null)
        {
            if (TryUseConsumable(inventoryIndex, selectedSlot))
            {
                AnimateToolSwing();
            }
            else if (selectedSlot != null && selectedSlot.category == ToolCategory.Melee)
            {
                AnimateToolSwing();
            }

            nextUseTime = Time.time + useCooldown;
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, useRange, ~0, QueryTriggerInteraction.Ignore))
        {
            HarvestableResource resource = ResolveHarvestTargetFromCollider(hit.collider, selectedSlot.actionType == ToolActionType.ChopTree ? HarvestResourceType.Tree : HarvestResourceType.Rock);
            if (resource != null)
            {
                HarvestResourceType targetType = selectedSlot.actionType == ToolActionType.ChopTree ? HarvestResourceType.Tree : HarvestResourceType.Rock;
                resource.Hit(targetType, toolDamage);
            }
        }

        AnimateToolSwing();
        SpendSelectedToolDurability(inventoryIndex, 1);
        nextUseTime = Time.time + useCooldown;
    }

    // Attempts to try attack enemy and returns whether the operation succeeded.
    private bool TryAttackEnemy(ToolSlot selectedSlot, int inventoryIndex)
    {
        if (selectedSlot == null || playerCamera == null)
        {
            return false;
        }

        int damage = GetPlayerWeaponDamage(selectedSlot);
        if (damage <= 0)
        {
            return false;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.SphereCastAll(ray, 0.45f, Mathf.Max(useRange, 3.2f), ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            if (enemy.TakeDamage(damage, gameObject))
            {
                ApplyWeaponHitEffects(selectedSlot);
                AnimateToolSwing();
                SpendSelectedToolDurability(inventoryIndex, 1);
                nextUseTime = Time.time + useCooldown;
                return true;
            }
        }

        return false;
    }

    // Handles the handle shearing input workflow.
    private bool HandleShearingInput(ToolSlot selectedSlot)
    {
        if (!IsScissorsTool(selectedSlot))
        {
            ResetHeldShearing();
            return false;
        }

        if (!Input.GetMouseButton(0))
        {
            ResetHeldShearing();
            return false;
        }

        SheepShearable sheep = FindShearTarget();
        if (sheep == null)
        {
            ResetHeldShearing();
            SetHarvestDebug("Scissors equipped. Hold left mouse while aiming at a sheep.");
            return true;
        }

        if (!sheep.IsReadyToShear)
        {
            ResetHeldShearing();
            SetHarvestDebug("This sheep has no wool. Regrows in " + Mathf.CeilToInt(sheep.RemainingRegrowSeconds) + "s.");
            return true;
        }

        if (heldShearTarget != sheep)
        {
            heldShearTarget = sheep;
            heldShearStartedAt = Time.time;
        }

        float holdSeconds = sheep.ShearHoldSeconds;
        float progress = Mathf.Clamp01((Time.time - heldShearStartedAt) / holdSeconds);
        SetHarvestDebug("Shearing sheep... " + Mathf.RoundToInt(progress * 100f) + "%");
        if (progress < 1f || Time.time < nextUseTime)
        {
            return true;
        }

        if (sheep.TryShear(this))
        {
            AnimateToolSwing();
            nextUseTime = Time.time + useCooldown;
            SetHarvestDebug("Collected wool.");
        }
        else
        {
            SetHarvestDebug("Could not collect wool. Check inventory space.");
        }

        ResetHeldShearing();
        return true;
    }

    // Finds, loads, or caches the references needed for find shear target.
    private SheepShearable FindShearTarget()
    {
        if (playerCamera == null)
        {
            return null;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.SphereCastAll(ray, 0.55f, Mathf.Max(useRange, 3.5f), ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            SheepShearable sheep = hit.collider.GetComponentInParent<SheepShearable>();
            if (sheep != null)
            {
                return sheep;
            }

            sheep = EnsureShearableSheep(hit.collider.transform);
            if (sheep != null)
            {
                return sheep;
            }
        }

        return FindClosestSheepInAimCone();
    }

    // Ensures the objects, references, or configuration required for ensure shearable sheep exist.
    private SheepShearable EnsureShearableSheep(Transform target)
    {
        Transform current = target;
        Transform sheepRoot = null;
        while (current != null)
        {
            if (LooksLikeSheep(current))
            {
                sheepRoot = current;
            }

            current = current.parent;
        }

        if (sheepRoot == null)
        {
            return null;
        }

        SheepShearable existing = sheepRoot.GetComponent<SheepShearable>();
        return existing != null ? existing : sheepRoot.gameObject.AddComponent<SheepShearable>();
    }

    // Finds, loads, or caches the references needed for find closest sheep in aim cone.
    private SheepShearable FindClosestSheepInAimCone()
    {
        if (playerCamera == null)
        {
            return null;
        }

        EnsureSceneSheepShearables();
        SheepShearable[] sheep = FindObjectsOfType<SheepShearable>();
        SheepShearable best = null;
        float bestScore = float.MaxValue;
        float range = Mathf.Max(useRange, 8f);
        for (int i = 0; i < sheep.Length; i++)
        {
            SheepShearable candidate = sheep[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 center = GetApproximateObjectCenter(candidate.transform);
            Vector3 toTarget = center - playerCamera.transform.position;
            float distance = toTarget.magnitude;
            if (distance > range || distance <= 0.01f)
            {
                continue;
            }

            float angle = Vector3.Angle(playerCamera.transform.forward, toTarget);
            if (angle > 18f)
            {
                continue;
            }

            Vector3 viewport = playerCamera.WorldToViewportPoint(center);
            if (viewport.z <= 0f)
            {
                continue;
            }

            float screenOffset = Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(0.5f, 0.5f));
            float score = screenOffset * 10f + distance * 0.1f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    // Calculates and returns the result for get approximate object center.
    private static Vector3 GetApproximateObjectCenter(Transform root)
    {
        Renderer renderer = root != null ? root.GetComponentInChildren<Renderer>(true) : null;
        if (renderer != null)
        {
            return renderer.bounds.center;
        }

        Collider collider = root != null ? root.GetComponentInChildren<Collider>(true) : null;
        if (collider != null)
        {
            return collider.bounds.center;
        }

        return root != null ? root.position + Vector3.up * 0.55f : Vector3.zero;
    }

    // Handles the looks like sheep workflow.
    private static bool LooksLikeSheep(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        string objectName = target.name.ToLowerInvariant();
        if (objectName.Contains("sheep"))
        {
            return true;
        }

        PlaceableObject placeable = target.GetComponent<PlaceableObject>();
        if (placeable != null)
        {
            string itemId = placeable.itemId != null ? placeable.itemId.ToLowerInvariant() : string.Empty;
            string displayName = placeable.displayName != null ? placeable.displayName.ToLowerInvariant() : string.Empty;
            return itemId.Contains("sheep") || displayName.Contains("sheep");
        }

        return false;
    }

    // Ensures the objects, references, or configuration required for ensure scene sheep shearables exist.
    private void EnsureSceneSheepShearables()
    {
        PlaceableObject[] placeables = FindObjectsOfType<PlaceableObject>();
        for (int i = 0; i < placeables.Length; i++)
        {
            PlaceableObject placeable = placeables[i];
            if (placeable == null || !LooksLikeSheep(placeable.transform))
            {
                continue;
            }

            if (placeable.GetComponent<SheepShearable>() == null)
            {
                placeable.gameObject.AddComponent<SheepShearable>();
            }
        }
    }

    // Handles the reset held shearing workflow.
    private void ResetHeldShearing()
    {
        heldShearTarget = null;
        heldShearStartedAt = 0f;
    }

    // Calculates and returns the result for is scissors tool.
    private static bool IsScissorsTool(ToolSlot slot)
    {
        if (slot == null)
        {
            return false;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        string displayName = slot.displayName != null ? slot.displayName.ToLowerInvariant() : string.Empty;
        return itemId == "scissors"
            || itemId.Contains("scissor")
            || itemId.Contains("shear")
            || displayName.Contains("scissor")
            || displayName.Contains("shear");
    }

    // Calculates and returns the result for get player weapon damage.
    private int GetPlayerWeaponDamage(ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        return GetConfiguredWeaponDamage(itemId, slot.damage);
    }

    // Calculates and returns the result for get configured weapon damage.
    private static int GetConfiguredWeaponDamage(string itemId, int fallbackDamage)
    {
        switch (itemId)
        {
            case "wood_sword": return 12;
            case "stone_sword": return 15;
            case "iron_sword": return 18;
            case "ruby_sword": return 24;
            case "sapphire_sword": return 21;
            case "emerald_sword": return 22;
            case "wood_axe":
            case "wood_pickaxe":
                return 12;
            case "stone_axe":
            case "stone_pickaxe":
                return 13;
        }

        if (!string.IsNullOrEmpty(itemId)
            && (itemId.EndsWith("_axe", System.StringComparison.Ordinal)
                || itemId.EndsWith("_pickaxe", System.StringComparison.Ordinal)))
        {
            return 15;
        }

        return Mathf.Max(0, fallbackDamage);
    }

    // Refreshes and applies configuration or runtime state for apply weapon hit effects.
    private void ApplyWeaponHitEffects(ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<PlayerHealth>();
        }

        if (itemId == "emerald_sword")
        {
            health.Heal(5);
        }
        else if (itemId == "sapphire_sword")
        {
            health.AddTemporaryDefense(10, 60f);
        }
    }

    // Clears runtime objects, cached data, or temporary state for remove meat from inventory.
    private void RemoveMeatFromInventory()
    {
        if (slots == null || slots.Length == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && IsMeatItem(InventoryUtility.GetItemId(slots[i])))
            {
                slots[i].stackCount = 0;
                changed = true;
            }
        }

        if (changed)
        {
            slots = InventoryUtility.Compact(slots);
        }
    }

    // Clears runtime objects, cached data, or temporary state for remove ranged weapons from inventory.
    private void RemoveRangedWeaponsFromInventory()
    {
        if (slots == null || slots.Length == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && IsRemovedRangedItem(InventoryUtility.GetItemId(slots[i])))
            {
                slots[i].stackCount = 0;
                changed = true;
            }
        }

        if (changed)
        {
            slots = InventoryUtility.Compact(slots);
        }
    }

    // Clears runtime objects, cached data, or temporary state for remove meat from catalog.
    private void RemoveMeatFromCatalog()
    {
        if (craftingCatalog == null)
        {
            return;
        }

        if (craftingCatalog.itemDefinitions != null)
        {
            List<CraftingItemDefinition> definitions = new List<CraftingItemDefinition>();
            for (int i = 0; i < craftingCatalog.itemDefinitions.Length; i++)
            {
                CraftingItemDefinition definition = craftingCatalog.itemDefinitions[i];
                if (definition != null && !IsMeatItem(definition.itemId))
                {
                    definitions.Add(definition);
                }
            }

            if (definitions.Count != craftingCatalog.itemDefinitions.Length)
            {
                craftingCatalog.itemDefinitions = definitions.ToArray();
            }
        }

        if (craftingCatalog.recipes != null)
        {
            List<CraftingRecipe> recipes = new List<CraftingRecipe>();
            for (int i = 0; i < craftingCatalog.recipes.Length; i++)
            {
                CraftingRecipe recipe = craftingCatalog.recipes[i];
                if (recipe != null && !IsMeatRecipe(recipe))
                {
                    recipes.Add(recipe);
                }
            }

            if (recipes.Count != craftingCatalog.recipes.Length)
            {
                craftingCatalog.recipes = recipes.ToArray();
            }
        }

        craftingCatalog.RebuildLookup();
    }

    // Clears runtime objects, cached data, or temporary state for remove ranged weapons from catalog.
    private void RemoveRangedWeaponsFromCatalog()
    {
        if (craftingCatalog == null)
        {
            return;
        }

        if (craftingCatalog.itemDefinitions != null)
        {
            List<CraftingItemDefinition> definitions = new List<CraftingItemDefinition>();
            for (int i = 0; i < craftingCatalog.itemDefinitions.Length; i++)
            {
                CraftingItemDefinition definition = craftingCatalog.itemDefinitions[i];
                if (definition != null && !IsRemovedRangedItem(definition.itemId))
                {
                    definitions.Add(definition);
                }
            }

            if (definitions.Count != craftingCatalog.itemDefinitions.Length)
            {
                craftingCatalog.itemDefinitions = definitions.ToArray();
            }
        }

        if (craftingCatalog.recipes != null)
        {
            List<CraftingRecipe> recipes = new List<CraftingRecipe>();
            for (int i = 0; i < craftingCatalog.recipes.Length; i++)
            {
                CraftingRecipe recipe = craftingCatalog.recipes[i];
                if (recipe != null && !IsRemovedRangedRecipe(recipe))
                {
                    recipes.Add(recipe);
                }
            }

            if (recipes.Count != craftingCatalog.recipes.Length)
            {
                craftingCatalog.recipes = recipes.ToArray();
            }
        }

        craftingCatalog.RebuildLookup();
    }

    // Calculates and returns the result for is removed ranged recipe.
    private static bool IsRemovedRangedRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || IsRemovedRangedItem(recipe.outputItemId) || IsRemovedRangedItem(recipe.recipeId) || IsRemovedRangedItem(recipe.displayName))
        {
            return true;
        }

        if (recipe.ingredients == null)
        {
            return false;
        }

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            if (recipe.ingredients[i] != null && IsRemovedRangedItem(recipe.ingredients[i].itemId))
            {
                return true;
            }
        }

        return false;
    }

    // Calculates and returns the result for is removed ranged item.
    private static bool IsRemovedRangedItem(string itemIdOrName)
    {
        if (string.IsNullOrEmpty(itemIdOrName))
        {
            return false;
        }

        string value = itemIdOrName.ToLowerInvariant();
        return value == "bow"
            || value == "wood_arrow"
            || value == "stone_arrow"
            || value == "iron_arrow"
            || value.Contains("arrow")
            || value.EndsWith(" bow")
            || value.Contains(" bow ");
    }

    // Calculates and returns the result for is meat recipe.
    private static bool IsMeatRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || IsMeatItem(recipe.outputItemId) || IsMeatItem(recipe.recipeId) || IsMeatItem(recipe.displayName))
        {
            return true;
        }

        if (recipe.ingredients == null)
        {
            return false;
        }

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            if (recipe.ingredients[i] != null && IsMeatItem(recipe.ingredients[i].itemId))
            {
                return true;
            }
        }

        return false;
    }

    // Calculates and returns the result for is meat item.
    private static bool IsMeatItem(string itemIdOrName)
    {
        if (string.IsNullOrEmpty(itemIdOrName))
        {
            return false;
        }

        string value = itemIdOrName.ToLowerInvariant();
        return value.Contains("mutton")
            || value.Contains("beef")
            || value.Contains("pork")
            || value.Contains("duck")
            || value.Contains("meat")
            || value.StartsWith("raw_")
            || value.StartsWith("cooked_");
    }

    // Calculates and returns the result for is ore item.
    private static bool IsOreItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && itemId.EndsWith("_ore", System.StringComparison.Ordinal);
    }

    // Attempts to try use consumable and returns whether the operation succeeded.
    private bool TryUseConsumable(int inventoryIndex, ToolSlot selectedSlot)
    {
        if (selectedSlot == null || selectedSlot.healAmount <= 0)
        {
            return false;
        }

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<PlayerHealth>();
        }

        if (!health.Heal(selectedSlot.healAmount))
        {
            return false;
        }

        ConsumeInventoryIndex(inventoryIndex, 1);
        return true;
    }

    // Handles the handle continuous harvest workflow.
    private void HandleContinuousHarvest(HarvestResourceType targetType)
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (!Input.GetMouseButton(0) || slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || playerCamera == null)
        {
            SetHarvestDebug("Cannot harvest: mouse not held, slot invalid, or camera missing.");
            ResetHeldHarvest();
            return;
        }

        ToolSlot selectedSlot = slots[inventoryIndex];
        bool validTool = targetType == HarvestResourceType.Tree ? IsTreeChopTool(selectedSlot) : IsRockMineTool(selectedSlot);
        if (!validTool)
        {
            SetHarvestDebug(targetType == HarvestResourceType.Tree
                ? "Current tool is not an axe/chop tool."
                : "Current tool is not a pickaxe/mining tool.");
            ResetHeldHarvest();
            return;
        }

        if (!TryFindTargetResource(targetType, out HarvestableResource resource))
        {
            SetHarvestDebug(targetType == HarvestResourceType.Tree
                ? "No tree detected near crosshair."
                : "No rock detected near crosshair.");
            if (Time.time - lastChopTargetSeenTime > treeChopTargetGraceSeconds)
            {
                ResetHeldHarvest();
            }

            return;
        }

        if (targetType == HarvestResourceType.Rock && selectedSlot != null && GetMiningTier(selectedSlot) < resource.requiredToolTier)
        {
            SetHarvestDebug("This rock needs a stronger pickaxe.");
            ResetHeldHarvest();
            return;
        }

        lastChopTargetSeenTime = Time.time;
        resource.EnsureDefaultDrops();

        if (heldChopTarget != resource || heldHarvestType != targetType)
        {
            heldChopTarget = resource;
            heldHarvestType = targetType;
            heldChopTime = 0f;
            SetHarvestDebug("Locked " + GetHarvestDebugTargetName(targetType, resource) + " target.");
        }

        heldChopTime += Time.deltaTime;
        float requiredSeconds = GetHarvestSeconds(selectedSlot, targetType);
        SetHarvestDebug(GetHarvestDebugActionName(targetType, resource) + "  " + heldChopTime.ToString("0.0") + " / " + requiredSeconds.ToString("0.0") + " s");
        if (heldChopTime >= requiredSeconds)
        {
            SetHarvestDebug(GetHarvestDebugTargetName(targetType, resource) + " harvested.");
            resource.HarvestCompletely(targetType);
            SpendSelectedToolDurability(inventoryIndex, 1);
            ResetHeldHarvest();
            return;
        }

        if (!wasHoldingUse || Time.time >= nextChopSwingTime)
        {
            AnimateToolSwing();
            PlayHarvestHitSoundOnStrike(selectedSlot, targetType);
            nextChopSwingTime = Time.time + GetHarvestSwingInterval(selectedSlot);
        }

        wasHoldingUse = true;
    }

    // Calculates and returns the result for get harvest seconds.
    private float GetHarvestSeconds(ToolSlot slot, HarvestResourceType targetType)
    {
        if (slot != null && slot.harvestSeconds > 0.01f)
        {
            return slot.harvestSeconds;
        }

        return targetType == HarvestResourceType.Tree ? treeChopHoldSeconds : 5f;
    }

    // Calculates and returns the result for get harvest debug action name.
    private static string GetHarvestDebugActionName(HarvestResourceType targetType, HarvestableResource resource)
    {
        string targetName = GetHarvestDebugTargetName(targetType, resource);
        return targetType == HarvestResourceType.Tree ? "Chopping " + targetName : "Mining " + targetName;
    }

    // Calculates and returns the result for get harvest debug target name.
    private static string GetHarvestDebugTargetName(HarvestResourceType targetType, HarvestableResource resource)
    {
        if (targetType == HarvestResourceType.Tree)
        {
            return "Tree";
        }

        string sourceName = resource != null ? resource.name : string.Empty;
        string dropName = resource != null ? resource.dropItemName : string.Empty;
        string lowerSource = sourceName.ToLowerInvariant();
        string lowerDrop = dropName.ToLowerInvariant();

        if (!string.IsNullOrEmpty(lowerDrop) && lowerDrop != "stone")
        {
            return dropName;
        }

        if (lowerSource.Contains("rock") || lowerSource.Contains("stone") || lowerDrop.Contains("rock") || lowerDrop.Contains("stone"))
        {
            return "Rock";
        }

        string resolvedOreName = ResolveMineableRockDropName(sourceName);
        return resolvedOreName == "Stone" ? "Rock" : resolvedOreName;
    }

    // Attempts to try get continuous harvest target type and returns whether the operation succeeded.
    private bool TryGetContinuousHarvestTargetType(ToolSlot selectedSlot, out HarvestResourceType targetType)
    {
        targetType = HarvestResourceType.Tree;
        if (Input.GetKey(KeyCode.LeftAlt) || !Input.GetMouseButton(0) || selectedSlot == null)
        {
            return false;
        }

        if (IsTreeChopTool(selectedSlot))
        {
            targetType = HarvestResourceType.Tree;
            return true;
        }

        if (IsRockMineTool(selectedSlot))
        {
            targetType = HarvestResourceType.Rock;
            return true;
        }

        return false;
    }

    // Handles the reset held harvest workflow.
    private void ResetHeldHarvest()
    {
        ResetSelectedToolPose();
        StopHarvestHitSound();
        heldChopTarget = null;
        heldHarvestType = HarvestResourceType.Tree;
        heldChopTime = 0f;
        lastChopTargetSeenTime = 0f;
        nextChopSwingTime = 0f;
        wasHoldingUse = false;
        ResetHeldHerbHarvest();
    }

    // Attempts to try find target resource and returns whether the operation succeeded.
    private bool TryFindTargetResource(HarvestResourceType targetType, out HarvestableResource resource)
    {
        resource = null;
        if (playerCamera == null)
        {
            return false;
        }

        float acquireRange = Mathf.Max(useRange, treeChopCameraAcquireRange);
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.SphereCastAll(ray, treeChopAimRadius, acquireRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            resource = ResolveHarvestTargetFromCollider(hits[i].collider, targetType);
            if (resource == null && targetType == HarvestResourceType.Rock)
            {
                resource = TryCreateRockHarvestTargetFromCollider(hits[i].collider);
            }

            if (resource != null && resource.resourceType == targetType && IsResourceWithinChopReach(resource))
            {
                return true;
            }
        }

        float stepDistance = acquireRange / Mathf.Max(1, treeChopSnapSamples);
        for (int i = 1; i <= treeChopSnapSamples; i++)
        {
            Vector3 samplePoint = ray.origin + ray.direction * (stepDistance * i);
            Collider[] nearby = Physics.OverlapSphere(samplePoint, treeChopSnapRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int j = 0; j < nearby.Length; j++)
            {
                resource = ResolveHarvestTargetFromCollider(nearby[j], targetType);
                if (resource == null && targetType == HarvestResourceType.Rock)
                {
                    resource = TryCreateRockHarvestTargetFromCollider(nearby[j]);
                }

                if (resource != null && resource.resourceType == targetType && IsResourceWithinChopReach(resource))
                {
                    return true;
                }
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit, acquireRange, ~0, QueryTriggerInteraction.Ignore))
        {
            resource = ResolveHarvestTargetFromCollider(hit.collider, targetType);
            if (resource == null && targetType == HarvestResourceType.Rock)
            {
                resource = TryCreateRockHarvestTargetFromCollider(hit.collider);
            }

            if (resource != null && resource.resourceType == targetType && IsResourceWithinChopReach(resource))
            {
                return true;
            }
        }

        return false;
    }

    // Finds, loads, or caches the references needed for resolve harvest target from collider.
    private static HarvestableResource ResolveHarvestTargetFromCollider(Collider hitCollider, HarvestResourceType targetType)
    {
        if (hitCollider == null)
        {
            return null;
        }

        HarvestableResource direct = hitCollider.GetComponent<HarvestableResource>();
        if (IsValidHarvestTarget(direct, targetType))
        {
            return direct;
        }

        direct = hitCollider.GetComponentInChildren<HarvestableResource>();
        if (IsValidHarvestTarget(direct, targetType))
        {
            return direct;
        }

        HarvestableResource parent = hitCollider.GetComponentInParent<HarvestableResource>();
        if (!IsValidHarvestTarget(parent, targetType))
        {
            return null;
        }

        Transform colliderTransform = hitCollider.transform;
        Transform resourceTransform = parent.transform;
        return colliderTransform == resourceTransform || colliderTransform.IsChildOf(resourceTransform) ? parent : null;
    }

    // Attempts to try create rock harvest target from collider and returns whether the operation succeeded.
    private static HarvestableResource TryCreateRockHarvestTargetFromCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        Transform target = FindMineableRockRoot(hitCollider.transform);
        if (target == null)
        {
            return null;
        }

        HarvestableResource resource = target.GetComponent<HarvestableResource>();
        if (resource == null)
        {
            resource = target.gameObject.AddComponent<HarvestableResource>();
        }

        resource.enabled = true;
        resource.resourceType = HarvestResourceType.Rock;
        resource.maxHealth = Mathf.Max(1, resource.maxHealth);
        resource.dropItemName = ResolveMineableRockDropName(target.name);
        resource.dropCount = Mathf.Max(1, resource.dropCount > 0 ? resource.dropCount : 4);
        if (resource.dropItemName != "Stone")
        {
            resource.dropPrefab = target.gameObject;
        }

        resource.requiredToolTier = Mathf.Max(resource.requiredToolTier, GetRequiredMiningTierForDrop(resource.dropItemName));
        resource.ResetHealth();
        resource.CaptureOriginalScale();

        if (target.GetComponent<LockableTarget>() == null)
        {
            target.gameObject.AddComponent<LockableTarget>();
        }

        return resource.IsSafeHarvestTarget() ? resource : null;
    }

    // Finds, loads, or caches the references needed for find mineable rock root.
    private static Transform FindMineableRockRoot(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (IsRuntimeMineableRockName(lowerName) && IsRuntimeMineableRockSizeSafe(current.gameObject))
            {
                return current;
            }

            if (current.GetComponent<PlayerToolController>() != null)
            {
                break;
            }

            current = current.parent;
        }

        return null;
    }

    // Calculates and returns the result for is runtime mineable rock name.
    private static bool IsRuntimeMineableRockName(string lowerName)
    {
        return !string.IsNullOrEmpty(lowerName)
            && (lowerName.Contains("rockgrey1")
                || lowerName.Contains("pp_crystal")
                || (LooksLikeLooseStone(lowerName) && !LooksLikeNonMineableStone(lowerName)));
    }

    // Handles the looks like loose stone workflow.
    private static bool LooksLikeLooseStone(string lowerName)
    {
        return lowerName.Contains("rock") || lowerName.Contains("stone");
    }

    // Handles the looks like non mineable stone workflow.
    private static bool LooksLikeNonMineableStone(string lowerName)
    {
        return lowerName.Contains("stones&rocks")
            || lowerName.Contains("crystals&ores&veins")
            || lowerName.Contains("wall")
            || lowerName.Contains("floor")
            || lowerName.Contains("slab")
            || lowerName.Contains("fragment")
            || lowerName.Contains("path")
            || lowerName.Contains("road")
            || lowerName.Contains("bridge")
            || lowerName.Contains("stair")
            || lowerName.Contains("terrain")
            || lowerName.Contains("island");
    }

    // Calculates and returns the result for is runtime mineable rock size safe.
    private static bool IsRuntimeMineableRockSizeSafe(GameObject root)
    {
        if (root == null || !TryGetRenderBounds(root, out Bounds bounds))
        {
            return false;
        }

        if (root.name.ToLowerInvariant().Contains("rockgrey1") || root.name.ToLowerInvariant().Contains("pp_crystal"))
        {
            return bounds.size.x <= 30f && bounds.size.y <= 30f && bounds.size.z <= 30f;
        }

        return bounds.size.x <= 25f && bounds.size.y <= 25f && bounds.size.z <= 25f;
    }

    // Finds, loads, or caches the references needed for resolve mineable rock drop name.
    private static string ResolveMineableRockDropName(string objectName)
    {
        string lower = (objectName ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("green") || lower.Contains("emerald")) return "Emerald Ore";
        if (lower.Contains("blue") || lower.Contains("sapphire")) return "Sapphire Ore";
        if (lower.Contains("red") || lower.Contains("ruby")) return "Ruby Ore";
        if (lower.Contains("gold")) return "Gold Ore";
        if (lower.Contains("copper")) return "Copper Ore";
        if (lower.Contains("iron")) return "Iron Ore";
        if (lower.Contains("silver") || lower.Contains("coal")) return "Coal Ore";
        return "Stone";
    }

    // Calculates and returns the result for get required mining tier for drop.
    private static int GetRequiredMiningTierForDrop(string itemName)
    {
        string lower = (itemName ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("emerald") || lower.Contains("green")) return 5;
        if (lower.Contains("sapphire") || lower.Contains("blue")) return 4;
        if (lower.Contains("ruby") || lower.Contains("red") || lower.Contains("gold") || lower.Contains("copper")) return 3;
        if (lower.Contains("iron") || lower.Contains("coal") || lower.Contains("silver")) return 2;
        return 1;
    }

    // Attempts to try get render bounds and returns whether the operation succeeded.
    private static bool TryGetRenderBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    // Calculates and returns the result for is valid harvest target.
    private static bool IsValidHarvestTarget(HarvestableResource resource, HarvestResourceType targetType)
    {
        return resource != null
            && resource.resourceType == targetType
            && resource.isActiveAndEnabled
            && resource.IsSafeHarvestTarget();
    }

    // Calculates and returns the result for is resource within chop reach.
    private bool IsResourceWithinChopReach(HarvestableResource resource)
    {
        if (resource == null)
        {
            return false;
        }

        Collider[] colliders = resource.GetComponentsInChildren<Collider>();
        Vector3 reachOrigin = transform.position + Vector3.up * 1f;
        float maxReach = useRange + treeChopPlayerReachPadding;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || !colliders[i].enabled || colliders[i].isTrigger)
            {
                continue;
            }

            Vector3 closestPoint = GetReachCheckPoint(colliders[i], reachOrigin);
            if (Vector3.Distance(reachOrigin, closestPoint) <= maxReach)
            {
                return true;
            }
        }

        return Vector3.Distance(reachOrigin, resource.transform.position) <= maxReach;
    }

    // Calculates and returns the result for get reach check point.
    private static Vector3 GetReachCheckPoint(Collider collider, Vector3 origin)
    {
        if (collider == null)
        {
            return origin;
        }

        MeshCollider meshCollider = collider as MeshCollider;
        if (meshCollider != null && !meshCollider.convex)
        {
            return meshCollider.bounds.ClosestPoint(origin);
        }

        return collider.ClosestPoint(origin);
    }

    // Ensures the objects, references, or configuration required for ensure tool actions exist.
    private void EnsureToolActions()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            string lowerName = GetToolLookupName(slot);
            slot.actionType = ToolActionType.None;

            if (LooksLikePickaxe(lowerName))
            {
                slot.actionType = ToolActionType.MineRock;
                slot.category = ToolCategory.Tools;
                slot.holdPose = ToolHoldPose.OneHandTool;
            }
            else if (LooksLikeTreeAxe(lowerName))
            {
                slot.actionType = ToolActionType.ChopTree;
                slot.category = ToolCategory.Tools;
                slot.holdPose = ToolHoldPose.OneHandTool;
            }
        }
    }

    // Calculates and returns the result for is tree chop tool.
    private static bool IsTreeChopTool(ToolSlot slot)
    {
        if (slot == null)
        {
            return false;
        }

        string lookupName = GetToolLookupName(slot);
        return LooksLikeTreeAxe(lookupName) || slot.actionType == ToolActionType.ChopTree;
    }

    // Calculates and returns the result for is rock mine tool.
    private static bool IsRockMineTool(ToolSlot slot)
    {
        if (slot == null)
        {
            return false;
        }

        string lookupName = GetToolLookupName(slot);
        return LooksLikePickaxe(lookupName) || slot.actionType == ToolActionType.MineRock;
    }

    // Calculates and returns the result for get tool lookup name.
    private static string GetToolLookupName(ToolSlot slot)
    {
        if (slot == null)
        {
            return string.Empty;
        }

        string prefabName = slot.prefab != null ? slot.prefab.name : string.Empty;
        string worldPrefabName = slot.worldPrefab != null ? slot.worldPrefab.name : string.Empty;
        return ((slot.itemId ?? string.Empty) + " "
            + (slot.displayName ?? string.Empty) + " "
            + prefabName + " "
            + worldPrefabName).ToLowerInvariant();
    }

    // Handles the looks like pickaxe workflow.
    private static bool LooksLikePickaxe(string lowerName)
    {
        return !string.IsNullOrEmpty(lowerName) && (lowerName.Contains("pickaxe") || lowerName.Contains("pick axe"));
    }

    // Handles the looks like tree axe workflow.
    private static bool LooksLikeTreeAxe(string lowerName)
    {
        if (string.IsNullOrEmpty(lowerName) || LooksLikePickaxe(lowerName))
        {
            return false;
        }

        return lowerName.Contains("axe") || lowerName.Contains("hatchet") || lowerName.Contains("fire axe");
    }

    // Calculates and returns the result for get mining tier.
    private static int GetMiningTier(ToolSlot slot)
    {
        string name = GetToolLookupName(slot);
        if (name.Contains("green") || name.Contains("emerald"))
        {
            return 99;
        }

        if (name.Contains("blue") || name.Contains("sapphire"))
        {
            return 5;
        }

        if (name.Contains("red") || name.Contains("ruby"))
        {
            return 4;
        }

        if (name.Contains("iron"))
        {
            return 3;
        }

        if (name.Contains("stone") || name.Contains("silver"))
        {
            return 2;
        }

        if (name.Contains("wood"))
        {
            return 1;
        }

        if (name.Contains("copper") || name.Contains("gold"))
        {
            return 3;
        }

        return 0;
    }

    // Refreshes and applies configuration or runtime state for update harvest debug selected slot.
    private void UpdateHarvestDebugSelectedSlot(ToolSlot selectedSlot)
    {
        if (!showHarvestDebug)
        {
            return;
        }

        if (selectedSlot == null)
        {
            harvestDebugText = "Selected Tool: Empty";
            return;
        }

        harvestDebugText = "Selected Tool: " + selectedSlot.displayName;
    }

    // Sets state, selection, or placement data for set harvest debug.
    private void SetHarvestDebug(string message)
    {
        if (!showHarvestDebug)
        {
            return;
        }

        harvestDebugText = message;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for build held tools.
    private void BuildHeldTools()
    {
        EnsureHandAnchor();
        ClearHeldTools();

        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            heldToolInstances.Add(null);
        }
    }

    // Ensures the objects, references, or configuration required for ensure held tool visual exist.
    private void EnsureHeldToolVisual(int inventoryIndex)
    {
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length)
        {
            return;
        }

        while (heldToolInstances.Count < slots.Length)
        {
            heldToolInstances.Add(null);
        }

        if (heldToolInstances[inventoryIndex] != null)
        {
            return;
        }

        GameObject instance = CreateToolVisual(slots[inventoryIndex]);
        if (instance == null)
        {
            return;
        }

        EnsureHandAnchor();
        instance.transform.SetParent(handAnchor, false);
        ApplyToolTransform(instance.transform, slots[inventoryIndex]);
        SetCollidersEnabled(instance, false);
        instance.SetActive(false);
        heldToolInstances[inventoryIndex] = instance;
    }

    // Adds, spawns, or attaches the objects and data for add held tool visual.
    private void AddHeldToolVisual(ToolSlot slot)
    {
        GameObject instance = CreateToolVisual(slot);
        if (instance == null)
        {
            heldToolInstances.Add(null);
            return;
        }

        instance.transform.SetParent(handAnchor, false);
        ApplyToolTransform(instance.transform, slot);
        SetCollidersEnabled(instance, false);
        instance.SetActive(false);
        heldToolInstances.Add(instance);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create tool visual.
    private GameObject CreateToolVisual(ToolSlot slot)
    {
        if (slot == null)
        {
            return null;
        }

        if (slot.displayName == "Pickaxe")
        {
            return CreatePickaxeVisual();
        }

        if (slot.prefab != null)
        {
            GameObject instance = Instantiate(slot.prefab);
            if (slot.category == ToolCategory.Crafted)
            {
                return CreateHeldCraftedVisual(instance, slot);
            }

            ApplyToolMaterials(instance, slot);
            ForceHeldMaterialIdentity(instance, slot);
            EnsureTorchFireLight(instance, slot);
            return instance;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        if (!string.IsNullOrEmpty(itemId))
        {
            if (itemId.EndsWith("_pickaxe", System.StringComparison.Ordinal) || itemId == "stone_hammer")
            {
                return CreatePickaxeVisual();
            }

            if (itemId.EndsWith("_axe", System.StringComparison.Ordinal) || itemId.EndsWith("_sword", System.StringComparison.Ordinal))
            {
                return CreatePickaxeVisual();
            }
        }

        return null;
    }

    // Ensures the objects, references, or configuration required for ensure torch fire light exist.
    private static void EnsureTorchFireLight(GameObject instance, ToolSlot slot)
    {
        if (instance == null || slot == null)
        {
            return;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        string displayName = slot.displayName == null ? string.Empty : slot.displayName.ToLowerInvariant();
        if (itemId != "torch" && !displayName.Contains("torch"))
        {
            return;
        }

        TorchFireLight fire = instance.GetComponent<TorchFireLight>();
        if (fire == null)
        {
            fire = instance.AddComponent<TorchFireLight>();
        }

        fire.LocalOffset = new Vector3(0f, 1.05f, 0f);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create pickaxe visual.
    private GameObject CreatePickaxeVisual()
    {
        GameObject root = new GameObject("PickaxeVisual");
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        handle.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.62f, 0f);
        head.transform.localScale = new Vector3(0.95f, 0.12f, 0.12f);

        GameObject tipLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tipLeft.name = "TipLeft";
        tipLeft.transform.SetParent(root.transform, false);
        tipLeft.transform.localPosition = new Vector3(-0.52f, 0.58f, 0f);
        tipLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        tipLeft.transform.localScale = new Vector3(0.35f, 0.1f, 0.1f);

        GameObject tipRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tipRight.name = "TipRight";
        tipRight.transform.SetParent(root.transform, false);
        tipRight.transform.localPosition = new Vector3(0.52f, 0.58f, 0f);
        tipRight.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        tipRight.transform.localScale = new Vector3(0.35f, 0.1f, 0.1f);

        AssignMaterial(handle, woodMaterial);
        AssignMaterial(head, metalMaterial);
        AssignMaterial(tipLeft, metalMaterial);
        AssignMaterial(tipRight, metalMaterial);

        return root;
    }

    // Calculates and returns the result for get or create runtime material prefab.
    private GameObject GetOrCreateRuntimeMaterialPrefab(string itemId)
    {
        if (itemId == "wood")
        {
            if (runtimeWoodPrefab == null)
            {
                runtimeWoodPrefab = CreateRuntimeBarPrefab("Held_Wood_Runtime", heldDropWoodMaterial != null ? heldDropWoodMaterial : woodMaterial, new Vector3(0.13f, 0.42f, 0.13f));
            }

            return runtimeWoodPrefab;
        }

        if (itemId == "stick")
        {
            if (runtimeStickPrefab == null)
            {
                runtimeStickPrefab = CreateRuntimeBarPrefab("Held_Stick_Runtime", woodMaterial, new Vector3(0.055f, 0.55f, 0.055f));
            }

            return runtimeStickPrefab;
        }

        if (itemId == "iron_bar")
        {
            if (runtimeIronBarPrefab == null)
            {
                runtimeIronBarPrefab = CreateRuntimeBarPrefab("Held_IronBar_Runtime", darkMetalMaterial != null ? darkMetalMaterial : metalMaterial, new Vector3(0.06f, 0.5f, 0.06f));
            }

            return runtimeIronBarPrefab;
        }

        return null;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create runtime bar prefab.
    private GameObject CreateRuntimeBarPrefab(string objectName, Material material, Vector3 scale)
    {
        GameObject root = new GameObject(objectName);
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = new Vector3(10000f, 10000f, 10000f);
        root.SetActive(true);

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bar.name = "Bar";
        bar.transform.SetParent(root.transform, false);
        bar.transform.localPosition = Vector3.zero;
        bar.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        bar.transform.localScale = scale;
        AssignMaterial(bar, material);

        Collider collider = bar.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        return root;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create runtime materials.
    private void CreateRuntimeMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return;
        }

        woodMaterial = new Material(shader) { color = new Color(0.47f, 0.28f, 0.13f) };
        metalMaterial = new Material(shader) { color = new Color(0.64f, 0.62f, 0.58f) };
        darkMetalMaterial = new Material(shader) { color = new Color(0.08f, 0.085f, 0.095f) };
        ironSwordMaterial = new Material(shader) { color = new Color(0.16f, 0.17f, 0.18f) };
        copperSwordMaterial = new Material(shader) { color = new Color(0.72f, 0.36f, 0.16f) };
        goldSwordMaterial = new Material(shader) { color = new Color(1f, 0.72f, 0.14f) };
        rubySwordMaterial = new Material(shader) { color = new Color(0.86f, 0.06f, 0.08f) };
        sapphireSwordMaterial = new Material(shader) { color = new Color(0.05f, 0.22f, 0.95f) };
        emeraldSwordMaterial = new Material(shader) { color = new Color(0.05f, 0.72f, 0.22f) };
        heldDropWoodMaterial = new Material(shader) { color = new Color(0.46f, 0.30f, 0.16f) };
        heldDropStoneMaterial = new Material(shader) { color = new Color(0.48f, 0.50f, 0.53f) };
    }

    // Handles the force held material identity workflow.
    private void ForceHeldMaterialIdentity(GameObject instance, ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        if (itemId == "iron_bar")
        {
            ApplyMaterialToRenderers(instance, darkMetalMaterial != null ? darkMetalMaterial : metalMaterial);
        }
        else if (itemId == "stick")
        {
            ApplyMaterialToRenderers(instance, heldDropWoodMaterial != null ? heldDropWoodMaterial : woodMaterial);
        }
    }
    // Refreshes and applies configuration or runtime state for apply tool materials.
    private void ApplyToolMaterials(GameObject instance, ToolSlot slot)
    {
        if (instance == null || slot == null)
        {
            return;
        }

        if (slot.category == ToolCategory.Materials)
        {
            string itemId = InventoryUtility.GetItemId(slot);
            if (itemId == "iron_bar")
            {
                ApplyMaterialToRenderers(instance, darkMetalMaterial != null ? darkMetalMaterial : metalMaterial);
                return;
            }

            if (itemId == "stick")
            {
                ApplyMaterialToRenderers(instance, heldDropWoodMaterial != null ? heldDropWoodMaterial : woodMaterial);
                return;
            }

            if (!IsOreItem(itemId))
            {
                ApplyHeldMaterialItemVisual(instance, slot);
            }

            return;
        }

        Material material = GetForcedHeldToolMaterial(slot);
        if (material != null)
        {
            ApplyMaterialToRenderers(instance, material);
            return;
        }

        if (slot.category == ToolCategory.Crafted)
        {
            ApplyCraftedPlaceableVisuals(instance, slot);
            return;
        }
    }

    // Refreshes and applies configuration or runtime state for apply runtime slot fixes.
    private void ApplyRuntimeSlotFixes()
    {
        RemoveMeatFromCatalog();
        RemoveRangedWeaponsFromCatalog();

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                ApplyRuntimeSlotFix(slots[i]);
            }
        }

        if (craftingCatalog == null || craftingCatalog.itemDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < craftingCatalog.itemDefinitions.Length; i++)
        {
            CraftingItemDefinition definition = craftingCatalog.itemDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            if (IsMeatItem(definition.itemId) || IsRemovedRangedItem(definition.itemId))
            {
                continue;
            }

            definition.heldPrefab = ResolveSlotPrefab(definition.heldPrefab, definition.itemId);
            definition.worldPrefab = ResolveSlotPrefab(definition.worldPrefab, definition.itemId);

            if (definition.itemId == "auto_forge")
            {
                definition.heldLocalPosition = new Vector3(-0.16f, 0.5f, 0f);
                definition.worldScale = Vector3.one * 0.3f;
            }

            if (definition.itemId == "furnace")
            {
                definition.heldLocalPosition = new Vector3(0f, -0.1f, 0.05f);
                definition.worldScale = Vector3.one;
            }

            if (definition.itemId == "wood_fence")
            {
                definition.worldScale = Vector3.one * 1.2f;
            }

            if (definition.itemId == "torch")
            {
                definition.placeable = true;
                definition.placeableType = PlaceableType.Torch;
                definition.worldScale = Vector3.one * 0.85f;
                definition.worldEulerOffset = Vector3.zero;
            }

            definition.damage = GetConfiguredWeaponDamage(definition.itemId, definition.damage);
            ApplySpecificHeldTuning(definition.itemId, ref definition.heldLocalPosition, ref definition.heldLocalEuler, ref definition.heldLocalScale);

            if (definition.itemId.Contains("helmet"))
            {
                definition.heldLocalPosition = new Vector3(-0.1f, -0.6f, 0.08f);
            }

            if (definition.itemId.Contains("armor"))
            {
                definition.heldLocalPosition = new Vector3(definition.heldLocalPosition.x, -0.2f, definition.heldLocalPosition.z);
            }
        }

        craftingCatalog.RebuildLookup();
    }


    // Refreshes and applies configuration or runtime state for apply harvest tool action fix.
    private static void ApplyHarvestToolActionFix(ToolSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        string lookupName = GetToolLookupName(slot);
        if (LooksLikePickaxe(lookupName))
        {
            slot.actionType = ToolActionType.MineRock;
            slot.category = ToolCategory.Tools;
            slot.holdPose = ToolHoldPose.OneHandTool;
        }
        else if (LooksLikeTreeAxe(lookupName))
        {
            slot.actionType = ToolActionType.ChopTree;
            slot.category = ToolCategory.Tools;
            slot.holdPose = ToolHoldPose.OneHandTool;
        }
    }
    // Refreshes and applies configuration or runtime state for apply runtime slot fix.
    private void ApplyRuntimeSlotFix(ToolSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        if (IsMeatItem(itemId) || IsRemovedRangedItem(itemId))
        {
            slot.stackCount = 0;
            return;
        }

        slot.prefab = ResolveSlotPrefab(slot.prefab, itemId);
        slot.worldPrefab = ResolveSlotPrefab(slot.worldPrefab, itemId);
        if ((itemId == "stick" || itemId == "iron_bar") && craftingCatalog != null)
        {
            CraftingItemDefinition replacement = craftingCatalog.FindItem(itemId);
            GameObject replacementHeldPrefab = replacement != null ? replacement.heldPrefab : null;
            GameObject replacementWorldPrefab = replacement != null ? replacement.worldPrefab : null;
            if (replacementHeldPrefab != null || replacementWorldPrefab != null)
            {
                slot.prefab = replacementHeldPrefab != null ? replacementHeldPrefab : replacementWorldPrefab;
                slot.worldPrefab = replacementWorldPrefab != null ? replacementWorldPrefab : replacementHeldPrefab;
            }
        }

        if ((itemId == "chest" || itemId == "door") && craftingCatalog != null)
        {
            CraftingItemDefinition replacement = craftingCatalog.FindItem(itemId);
            GameObject replacementHeldPrefab = replacement != null ? replacement.heldPrefab : null;
            GameObject replacementWorldPrefab = replacement != null ? replacement.worldPrefab : null;
            if (replacementHeldPrefab != null || replacementWorldPrefab != null)
            {
                slot.prefab = replacementHeldPrefab != null ? replacementHeldPrefab : replacementWorldPrefab;
                slot.worldPrefab = replacementWorldPrefab != null ? replacementWorldPrefab : replacementHeldPrefab;
            }
        }

        if ((slot.prefab == null || slot.worldPrefab == null) && craftingCatalog != null)
        {
            CraftingItemDefinition definition = craftingCatalog.FindItem(itemId);
            if (definition != null)
            {
                if (slot.prefab == null)
                {
                    slot.prefab = definition.heldPrefab != null ? definition.heldPrefab : definition.worldPrefab;
                }

                if (slot.worldPrefab == null)
                {
                    slot.worldPrefab = definition.worldPrefab != null ? definition.worldPrefab : definition.heldPrefab;
                }
            }
        }

        ApplyPolytopeHandWeaponTuning(slot, itemId);

        if (itemId == "auto_forge")
        {
            slot.heldLocalPosition = new Vector3(-0.16f, 0.5f, 0f);
            slot.worldScale = Vector3.one * 0.3f;
        }

        if (itemId == "furnace")
        {
            slot.heldLocalPosition = new Vector3(0f, -0.1f, 0.05f);
            slot.worldScale = Vector3.one;
        }

        if (itemId == "wood_fence")
        {
            slot.worldScale = Vector3.one * 1.2f;
        }

        if (itemId == "torch")
        {
            slot.placeable = true;
            slot.placeableType = PlaceableType.Torch;
            slot.worldScale = Vector3.one * 0.85f;
            slot.worldEulerOffset = Vector3.zero;
        }

        slot.damage = GetConfiguredWeaponDamage(itemId, slot.damage);
        ApplySpecificHeldTuning(itemId, ref slot.heldLocalPosition, ref slot.heldLocalEuler, ref slot.heldLocalScale);
        ApplyHarvestToolActionFix(slot);

        if (itemId.Contains("helmet"))
        {
            slot.heldLocalPosition = new Vector3(-0.1f, -0.6f, 0.08f);
        }

        if (itemId.Contains("armor"))
        {
            slot.heldLocalPosition = new Vector3(slot.heldLocalPosition.x, -0.2f, slot.heldLocalPosition.z);
        }
    }

    // Refreshes and applies configuration or runtime state for apply polytope hand weapon tuning.
    private static void ApplyPolytopeHandWeaponTuning(ToolSlot slot, string itemId)
    {
        if (slot == null || string.IsNullOrEmpty(itemId))
        {
            return;
        }

        if (itemId.EndsWith("_pickaxe", System.StringComparison.Ordinal))
        {
            slot.heldLocalPosition = new Vector3(-0.1f, -0.04f, -0.2f);
            slot.heldLocalEuler = new Vector3(1000f, 94f, -92f);
            slot.heldLocalScale = Vector3.one * 0.55f;
            slot.nonGunDirectionFlipped = true;
        }
        else if (itemId.EndsWith("_axe", System.StringComparison.Ordinal))
        {
            slot.heldLocalPosition = new Vector3(-0.1f, -0.04f, -0.2f);
            slot.heldLocalEuler = new Vector3(1000f, 90f, -94f);
            slot.heldLocalScale = Vector3.one * 0.52f;
            slot.nonGunDirectionFlipped = true;
        }
        else if (itemId.EndsWith("_sword", System.StringComparison.Ordinal))
        {
            slot.heldLocalPosition = new Vector3(-0.1f, -0.04f, -0.1f);
            slot.heldLocalEuler = new Vector3(1000f, -86f, -92f);
            slot.heldLocalScale = Vector3.one * 0.52f;
            slot.nonGunDirectionFlipped = true;
        }
    }

    // Refreshes and applies configuration or runtime state for apply specific held tuning.
    private static void ApplySpecificHeldTuning(string itemId, ref Vector3 position, ref Vector3 euler, ref Vector3 scale)
    {
        switch (itemId)
        {
            case "workbench":
                position = new Vector3(-0.1f, -0.1f, 0f);
                break;
            case "chair":
                position = new Vector3(-0.15f, -0.1f, 0.09f);
                break;
            case "table":
                position = new Vector3(-0.14f, -0.1f, -0.09f);
                break;
            case "wood":
                position = new Vector3(-0.06f, -0.1f, 0.03f);
                euler = new Vector3(92f, 0f, -10f);
                scale = Vector3.one * 0.72f;
                break;
            case "stick":
                position = new Vector3(-0.08f, -0.07f, 0.02f);
                euler = new Vector3(92f, 0f, -10f);
                scale = Vector3.one * 0.05f;
                break;
            case "iron_bar":
                position = new Vector3(-0.08f, -0.07f, 0.02f);
                euler = new Vector3(92f, 0f, -10f);
                scale = Vector3.one * 0.72f;
                break;
            case "stone":
                position = new Vector3(0f, -0.07f, 0f);
                scale = new Vector3(0.01f, 0.0001f, 0.01f);
                break;
            case "iron_ore":
            case "ruby_ore":
            case "sapphire_ore":
            case "emerald_ore":
            case "copper_ore":
            case "gold_ore":
            case "coal_ore":
            case "forest_guide":
            case "forest_heart_detector":
                position = new Vector3(-0.1f, -0.06f, -0.01f);
                break;
            case "herb":
                position = new Vector3(-0.2f, -0.1f, 0.1f);
                break;
            case "stone_hammer":
                position = new Vector3(-0.1f, -0.06f, -0.1f);
                euler = new Vector3(100f, 180f, 0f);
                break;
            case "scissors":
                position = new Vector3(-0.2f, -0.02f, 0f);
                euler = new Vector3(0f, -94f, -92f);
                break;
            case "chest":
                position = new Vector3(0.02f, -0.09f, 0.08f);
                break;
            case "bed":
                position = new Vector3(-0.01f, -0.04f, 0.07f);
                break;
            case "door":
                position = new Vector3(-0.1f, -0.09f, -0.09f);
                break;
            case "furnace":
                position = new Vector3(0f, -0.1f, 0.05f);
                break;
            case "auto_forge":
                position = new Vector3(-0.16f, 0.5f, 0f);
                break;
            case "torch":
                position = new Vector3(-0.1f, 0f, 0f);
                euler = new Vector3(100f, euler.y, euler.z);
                break;
            case "bandage":
                position = new Vector3(-0.1f, -0.09f, 0f);
                scale = Vector3.one * 0.07f;
                break;
            case "first_aid_kit":
                position = new Vector3(-0.1f, -0.09f, 0f);
                scale = Vector3.one * 0.1f;
                break;
        }
    }

    // Finds, loads, or caches the references needed for load prefab asset.
    private static GameObject LoadPrefabAsset(string assetPath, string resourcesPath)
    {
#if UNITY_EDITOR
        GameObject editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (editorAsset != null)
        {
            return editorAsset;
        }
#endif
        return string.IsNullOrEmpty(resourcesPath) ? null : Resources.Load<GameObject>(resourcesPath);
    }
    // Finds, loads, or caches the references needed for resolve slot prefab.
    private GameObject ResolveSlotPrefab(GameObject prefab, ToolSlot slot)
    {
        return ResolveSlotPrefab(prefab, InventoryUtility.GetItemId(slot));
    }

    // Finds, loads, or caches the references needed for resolve slot prefab.
    private GameObject ResolveSlotPrefab(GameObject prefab, string itemId)
    {
        if (itemId == "wood")
        {
            return GetOrCreateRuntimeMaterialPrefab("wood");
        }

        if (itemId == "stick")
        {
            GameObject stickPrefab = Resources.Load<GameObject>("RuntimePrefabs/Held_Stick");
            return stickPrefab != null ? stickPrefab : GetOrCreateRuntimeMaterialPrefab("stick");
        }

        if (itemId == "iron_bar")
        {
            GameObject ironBarPrefab = Resources.Load<GameObject>("RuntimePrefabs/Held_IronBar");
            return ironBarPrefab != null ? ironBarPrefab : GetOrCreateRuntimeMaterialPrefab("iron_bar");
        }

        if (itemId == "door")
        {
            GameObject doorPrefab = LoadPrefabAsset("Assets/Free Wood Door Pack/Prefab/Wood/Door_1/Door_1_Brown.prefab", "RuntimePrefabs/Door_1_Brown");
            return doorPrefab != null ? doorPrefab : prefab;
        }

        if (itemId == "chest")
        {
            GameObject chestPrefab = LoadPrefabAsset("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Treasure_Chest_04_Blue.prefab", "RuntimePrefabs/PP_Treasure_Chest_01_Blue");
            return chestPrefab != null ? chestPrefab : prefab;
        }

        if (!string.IsNullOrEmpty(itemId))
        {
            ToolSlot template = FindInventoryTemplate(itemId);
            if (template != null)
            {
                if (template.prefab != null)
                {
                    return template.prefab;
                }

                if (template.worldPrefab != null)
                {
                    return template.worldPrefab;
                }
            }
        }

        GameObject replacement = null;
        if (itemId == "auto_forge")
        {
            replacement = Resources.Load<GameObject>("AutoForge/Forge");
        }
        else if (itemId == "door")
        {
            replacement = LoadPrefabAsset("Assets/Free Wood Door Pack/Prefab/Wood/Door_1/Door_1_Brown.prefab", "RuntimePrefabs/Door_1_Brown");
        }
        else if (itemId == "chest")
        {
            replacement = LoadPrefabAsset("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Treasure_Chest_04_Blue.prefab", "RuntimePrefabs/PP_Treasure_Chest_01_Blue");
        }
        else if (itemId == "stone")
        {
            replacement = Resources.Load<GameObject>("RuntimePrefabs/Held_LowPolyStone");
        }
        else if (itemId == "forest_heart_detector")
        {
            replacement = Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector");
        }
        else if (itemId == "forest_guide")
        {
            replacement = Resources.Load<GameObject>("RuntimePrefabs/Held_ForestGuide");
        }
        else if (itemId == "forest_heart")
        {
            replacement = Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeart");
        }

        return replacement != null ? replacement : prefab;
    }
    // Calculates and returns the result for get placement world scale.
    private static Vector3 GetPlacementWorldScale(ToolSlot slot)
    {
        if (slot == null)
        {
            return Vector3.one;
        }

        if (InventoryUtility.GetItemId(slot) == "wood_fence")
        {
            return Vector3.one * 1.2f;
        }

        if (InventoryUtility.GetItemId(slot) == "auto_forge")
        {
            return Vector3.one * 0.3f;
        }

        if (InventoryUtility.GetItemId(slot) == "furnace")
        {
            return Vector3.one;
        }

        if (InventoryUtility.GetItemId(slot) == "door")
        {
            return Vector3.one;
        }

        if (InventoryUtility.GetItemId(slot) == "chest")
        {
            return Vector3.one;
        }

        return slot.worldScale == Vector3.zero ? Vector3.one : slot.worldScale;
    }

    // Calculates and returns the result for get forced held tool material.
    private Material GetForcedHeldToolMaterial(ToolSlot slot)
    {
        string lowerName = (slot.displayName ?? string.Empty).ToLowerInvariant();
        if (lowerName.Contains("helmet"))
        {
            if (lowerName.Contains("wood"))
            {
                return woodMaterial;
            }

            if (lowerName.Contains("stone"))
            {
                return metalMaterial;
            }

            if (lowerName.Contains("iron"))
            {
                return darkMetalMaterial;
            }
        }

        if (lowerName.Contains("scissors") || lowerName.Contains("shears"))
        {
            return darkMetalMaterial;
        }

        if (!lowerName.Contains("sword"))
        {
            return slot.displayName == "Bat" ? woodMaterial : null;
        }

        if (lowerName.Contains("ruby"))
        {
            return rubySwordMaterial;
        }

        if (lowerName.Contains("sapphire"))
        {
            return sapphireSwordMaterial;
        }

        if (lowerName.Contains("emerald"))
        {
            return emeraldSwordMaterial;
        }

        if (lowerName.Contains("copper"))
        {
            return copperSwordMaterial;
        }

        if (lowerName.Contains("gold"))
        {
            return goldSwordMaterial;
        }

        if (lowerName.Contains("iron"))
        {
            return ironSwordMaterial;
        }

        if (lowerName.Contains("wood"))
        {
            return woodMaterial;
        }

        if (lowerName.Contains("stone"))
        {
            return heldDropStoneMaterial != null ? heldDropStoneMaterial : metalMaterial;
        }

        return metalMaterial;
    }

    // Refreshes and applies configuration or runtime state for apply material to renderers.
    private static void ApplyMaterialToRenderers(GameObject instance, Material material)
    {
        if (instance == null || material == null)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }

    // Refreshes and applies configuration or runtime state for apply crafted placeable visuals.
    private void ApplyCraftedPlaceableVisuals(GameObject instance, ToolSlot slot = null)
    {
        if (instance == null)
        {
            return;
        }

        Material material = InventoryUtility.GetItemId(slot) == "auto_forge" && darkMetalMaterial != null
            ? darkMetalMaterial
            : woodMaterial;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    // Handles the prepare held crafted visual workflow.
    private GameObject PrepareHeldCraftedVisual(GameObject instance, ToolSlot slot)
    {
        if (instance == null)
        {
            return null;
        }

        GameObject wrapper = new GameObject((slot.displayName ?? "Crafted") + "_Held");
        instance.transform.SetParent(wrapper.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        ApplyCraftedPlaceableVisuals(instance, slot);
        RemoveHeldPhysics(wrapper);
        RemoveHeldSceneComponents(wrapper);
        NormalizeHeldCraftedBounds(instance.transform);
        return wrapper;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create held crafted visual.
    private GameObject CreateHeldCraftedVisual(GameObject instance, ToolSlot slot)
    {
        if (instance == null || slot == null)
        {
            return null;
        }

        ApplyCraftedPlaceableVisuals(instance, slot);
        RemoveHeldPhysics(instance);
        RemoveHeldSceneComponents(instance);
        SetCollidersEnabled(instance, false);

        bool hasDedicatedHeldPrefab = slot.prefab != null && slot.worldPrefab != null && slot.prefab != slot.worldPrefab;
        return hasDedicatedHeldPrefab && !IsPlaceableHeldPrefab(slot) ? instance : PrepareHeldCraftedVisual(instance, slot);
    }

    // Calculates and returns the result for is placeable held prefab.
    private static bool IsPlaceableHeldPrefab(ToolSlot slot)
    {
        return slot != null
            && slot.category == ToolCategory.Crafted
            && slot.placeable
            && slot.placeableType != PlaceableType.None;
    }

    // Clears runtime objects, cached data, or temporary state for remove held physics.
    private static void RemoveHeldPhysics(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = rigidbodies.Length - 1; i >= 0; i--)
        {
            Destroy(rigidbodies[i]);
        }

        Joint[] joints = root.GetComponentsInChildren<Joint>(true);
        for (int i = joints.Length - 1; i >= 0; i--)
        {
            Destroy(joints[i]);
        }
    }

    // Clears runtime objects, cached data, or temporary state for remove held scene components.
    private static void RemoveHeldSceneComponents(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = lights.Length - 1; i >= 0; i--)
        {
            Destroy(lights[i]);
        }

        Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
        for (int i = cameras.Length - 1; i >= 0; i--)
        {
            Destroy(cameras[i]);
        }

        AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = audioSources.Length - 1; i >= 0; i--)
        {
            audioSources[i].enabled = false;
        }
    }

    // Handles the normalize held crafted bounds workflow.
    private static void NormalizeHeldCraftedBounds(Transform visualRoot)
    {
        if (visualRoot == null || !TryGetRendererBounds(visualRoot.gameObject, out Bounds bounds))
        {
            return;
        }

        float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxSize <= 0.0001f)
        {
            return;
        }

        float normalizedScale = 1f / maxSize;
        visualRoot.localScale = Vector3.one * normalizedScale;

        if (!TryGetRendererBounds(visualRoot.gameObject, out bounds))
        {
            return;
        }

        Vector3 localCenter = visualRoot.InverseTransformPoint(bounds.center);
        visualRoot.localPosition = -localCenter;
    }

    // Attempts to try get renderer bounds and returns whether the operation succeeded.
    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    // Refreshes and applies configuration or runtime state for apply held material item visual.
    private void ApplyHeldMaterialItemVisual(GameObject instance, ToolSlot slot)
    {
        if (instance == null || slot == null)
        {
            return;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        string lowerName = (slot.displayName ?? string.Empty).ToLowerInvariant();
        Material fallback = itemId == "iron_bar"
            ? (darkMetalMaterial != null ? darkMetalMaterial : metalMaterial)
            : lowerName.Contains("stone") || lowerName.Contains("rock")
                ? heldDropStoneMaterial
                : heldDropWoodMaterial;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int j = 0; j < materials.Length; j++)
            {
                Material replacement = CreateCompatibleHeldDropMaterial(materials[j], fallback);
                if (replacement != null)
                {
                    materials[j] = replacement;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create compatible held drop material.
    private Material CreateCompatibleHeldDropMaterial(Material sourceMaterial, Material fallback)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null || fallback == null)
        {
            return fallback;
        }

        Material materialInstance = new Material(shader);
        Color tint = fallback.color;
        materialInstance.color = tint;
        if (materialInstance.HasProperty("_BaseColor"))
        {
            materialInstance.SetColor("_BaseColor", tint);
        }

        if (sourceMaterial != null)
        {
            Texture baseTexture = GetFirstTexture(sourceMaterial, "_BaseColorMap", "_BaseMap", "_MainTex");
            Texture normalTexture = GetFirstTexture(sourceMaterial, "_NormalMap", "_BumpMap");
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                if (baseTexture != null)
                {
                    materialInstance.SetTexture("_BaseMap", baseTexture);
                }

                if (normalTexture != null)
                {
                    materialInstance.EnableKeyword("_NORMALMAP");
                    materialInstance.SetTexture("_BumpMap", normalTexture);
                }

                materialInstance.SetFloat("_Smoothness", 0.22f);
            }
            else
            {
                if (baseTexture != null)
                {
                    materialInstance.SetTexture("_MainTex", baseTexture);
                }

                if (normalTexture != null)
                {
                    materialInstance.EnableKeyword("_NORMALMAP");
                    materialInstance.SetTexture("_BumpMap", normalTexture);
                }

                materialInstance.SetFloat("_Glossiness", 0.22f);
            }
        }

        if (materialInstance.HasProperty("_Metallic"))
        {
            materialInstance.SetFloat("_Metallic", 0f);
        }

        return materialInstance;
    }

    // Calculates and returns the result for get first texture.
    private static Texture GetFirstTexture(Material material, params string[] propertyNames)
    {
        if (material == null)
        {
            return null;
        }

        for (int i = 0; i < propertyNames.Length; i++)
        {
            string property = propertyNames[i];
            if (material.HasProperty(property))
            {
                Texture texture = material.GetTexture(property);
                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    // Handles the assign material workflow.
    private static void AssignMaterial(GameObject gameObject, Material material)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    // Ensures the objects, references, or configuration required for ensure hand anchor exist.
    private void EnsureHandAnchor()
    {
        visualRoot = ResolveVisualRoot();
        Animator animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>(true) : null;
        Transform hand = animator != null && animator.avatar != null && animator.avatar.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.RightHand)
            : null;
        if (hand == null)
        {
            hand = FindDeepChild(visualRoot, "mixamorig:RightHand");
        }

        if (hand == null)
        {
            hand = FindDeepChild(visualRoot, "RightHand");
        }

        if (hand == null)
        {
            hand = FindDeepChild(visualRoot, "PT_RightHand");
        }

        if (hand == null)
        {
            hand = FindDeepChild(visualRoot, "hand.R");
        }

        if (handAnchor != null)
        {
            Destroy(handAnchor.gameObject);
            handAnchor = null;
        }

        handAnchor = new GameObject("HeldToolAnchor").transform;
        handAnchor.SetParent(hand != null ? hand : transform, false);
        handAnchor.localPosition = Vector3.zero;
        handAnchor.localRotation = Quaternion.identity;
        handAnchor.localScale = Vector3.one;
    }

    // Sets state, selection, or placement data for select slot.
    public void SelectSlot(int index)
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning("PlayerToolController: Cannot select slot because slots is empty.");
            return;
        }

        ResetHeldHarvest();
        EnsureHotbarMapping();
        selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, hotbarSize - 1));
        int inventoryIndex = GetSelectedInventoryIndex();
        EnsureHeldToolVisual(inventoryIndex);

        for (int i = 0; i < heldToolInstances.Count; i++)
        {
            if (heldToolInstances[i] != null)
            {
                heldToolInstances[i].SetActive(i == inventoryIndex);
            }
        }

        ApplyHeldToolTransform();

        if (characterAnimator != null)
        {
            characterAnimator.SetHoldPose(inventoryIndex >= 0 && inventoryIndex < slots.Length ? slots[inventoryIndex].holdPose : ToolHoldPose.OneHandTool);
        }

        UpdateGunIKTarget();

        if (hotbarUI != null)
        {
            hotbarUI.Refresh(this);
        }

        if (backpackUI != null)
        {
            backpackUI.Refresh(this);
        }
    }

    // Unity lifecycle: clears temporary state or subscriptions when the component is disabled.
    private void OnDisable()
    {
        ResetHeldHarvest();
    }

    // Unity lifecycle: draws immediate-mode HUD or debug information.
    private void OnGUI()
    {
        if (ForestMenuUI.IsMenuOpen)
        {
            return;
        }

        DrawSelectedDurabilityUI();

        if (!showHarvestDebug || string.IsNullOrEmpty(harvestDebugText))
        {
            return;
        }

        EnsureHudStyles();
        Rect panel = new Rect(-18f, 356f, 390f, 112f);
        DrawHudFrame(panel);
        GUI.Label(new Rect(42f, 380f, 290f, 28f), "HARVEST", hudTitleStyle);
        GUI.Label(new Rect(42f, 412f, 290f, 34f), harvestDebugText, hudLabelStyle);
    }

    // Handles the draw selected durability ui workflow.
    private void DrawSelectedDurabilityUI()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length)
        {
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        if (slot == null || slot.maxDurability <= 0)
        {
            return;
        }

        int maxDurability = Mathf.Max(1, slot.maxDurability);
        int durability = Mathf.Clamp(slot.durability > 0 ? slot.durability : maxDurability, 0, maxDurability);
        string name = string.IsNullOrEmpty(slot.displayName) ? InventoryUtility.GetItemId(slot) : slot.displayName;
        float fill = durability / (float)maxDurability;

        Color oldColor = GUI.color;
        EnsureHudStyles();
        float top = 190f;
        Rect panel = new Rect(-18f, top, 410f, 144f);
        DrawHudFrame(panel);
        GUI.color = Color.white;
        GUI.Label(new Rect(42f, top + 28f, 300f, 30f), name + " Durability", hudTitleStyle);

        Rect back = new Rect(42f, top + 76f, 238f, 20f);
        GUI.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        GUI.DrawTexture(back, Texture2D.whiteTexture);
        GUI.color = fill > 0.3f ? new Color(0.2f, 0.85f, 0.25f, 0.95f) : new Color(0.9f, 0.25f, 0.15f, 0.95f);
        GUI.DrawTexture(new Rect(back.x, back.y, back.width * fill, back.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(292f, top + 72f, 72f, 30f), durability + "/" + maxDurability, hudLabelStyle);
        GUI.color = oldColor;
    }

    // Ensures the objects, references, or configuration required for ensure hud styles exist.
    private void EnsureHudStyles()
    {
        if (hudFrameTexture == null)
        {
            hudFrameTexture = Resources.Load<Texture2D>("frame");
        }

        if (hudTitleStyle == null)
        {
            hudTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 1f, 0.8f, 1f) }
            };
        }

        if (hudLabelStyle == null)
        {
            hudLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.white }
            };
        }
    }

    // Handles the draw hud frame workflow.
    private void DrawHudFrame(Rect rect)
    {
        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, hudFrameTexture != null ? hudFrameTexture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        GUI.color = oldColor;
    }

    // Calculates and returns the result for get hotbar slot.
    public ToolSlot GetHotbarSlot(int hotbarIndex)
    {
        int inventoryIndex = GetInventoryIndexForHotbar(hotbarIndex);
        return inventoryIndex >= 0 && slots != null && inventoryIndex < slots.Length ? slots[inventoryIndex] : null;
    }

    // Calculates and returns the result for get inventory index for hotbar.
    public int GetInventoryIndexForHotbar(int hotbarIndex)
    {
        EnsureHotbarMapping();
        if (hotbarSlotIndices == null || hotbarIndex < 0 || hotbarIndex >= hotbarSlotIndices.Length)
        {
            return -1;
        }

        return hotbarSlotIndices[hotbarIndex];
    }

    // Calculates and returns the result for is inventory index in hotbar.
    public bool IsInventoryIndexInHotbar(int inventoryIndex)
    {
        EnsureHotbarMapping();
        if (hotbarSlotIndices == null)
        {
            return false;
        }

        for (int i = 0; i < hotbarSlotIndices.Length; i++)
        {
            if (hotbarSlotIndices[i] == inventoryIndex)
            {
                return true;
            }
        }

        return false;
    }

    // Handles the reset hotbar to first slots workflow.
    public void ResetHotbarToFirstSlots()
    {
        if (slots == null)
        {
            hotbarSlotIndices = new int[0];
            return;
        }

        hotbarSize = Mathf.Clamp(hotbarSize <= 0 ? 9 : hotbarSize, 1, 9);
        hotbarSlotIndices = new int[hotbarSize];
        for (int i = 0; i < hotbarSlotIndices.Length; i++)
        {
            hotbarSlotIndices[i] = i < slots.Length ? i : -1;
        }
    }

    // Handles the equip inventory slot to selected hotbar workflow.
    public void EquipInventorySlotToSelectedHotbar(int inventoryIndex)
    {
        if (slots == null || inventoryIndex >= slots.Length)
        {
            return;
        }

        EnsureHotbarMapping();
        if (inventoryIndex < 0)
        {
            hotbarSlotIndices[selectedIndex] = -1;
            SelectSlot(selectedIndex);
            return;
        }

        int existingHotbarIndex = -1;
        for (int i = 0; i < hotbarSlotIndices.Length; i++)
        {
            if (hotbarSlotIndices[i] == inventoryIndex)
            {
                existingHotbarIndex = i;
                break;
            }
        }

        int previousInventoryIndex = hotbarSlotIndices[selectedIndex];
        hotbarSlotIndices[selectedIndex] = inventoryIndex;
        if (existingHotbarIndex >= 0 && existingHotbarIndex != selectedIndex)
        {
            hotbarSlotIndices[existingHotbarIndex] = previousInventoryIndex;
        }

        SelectSlot(selectedIndex);
    }

    // Adds, spawns, or attaches the objects and data for add inventory item.
    public void AddInventoryItem(string itemName, GameObject prefab, int amount)
    {
        TryAddInventoryItem(itemName, prefab, amount);
    }

    // Ensures the objects, references, or configuration required for ensure all tools and weapons in inventory exist.
    public void EnsureAllToolsAndWeaponsInInventory()
    {
        hasBackpack = true;
        if (craftingCatalog == null)
        {
            craftingCatalog = FindObjectOfType<CraftingCatalog>();
        }

        if (craftingCatalog == null || craftingCatalog.itemDefinitions == null)
        {
            return;
        }

        int missingCount = 0;
        for (int i = 0; i < craftingCatalog.itemDefinitions.Length; i++)
        {
            CraftingItemDefinition definition = craftingCatalog.itemDefinitions[i];
            if (definition != null
                && !string.IsNullOrEmpty(definition.itemId)
                && (definition.category == ToolCategory.Tools || definition.category == ToolCategory.Melee)
                && GetItemCount(definition.itemId) <= 0)
            {
                missingCount++;
            }
        }

        int requiredSlots = (slots != null ? slots.Length : 0) + missingCount;
        backpackColumns = Mathf.Max(1, backpackColumns);
        backpackRows = Mathf.Max(backpackRows, Mathf.CeilToInt(requiredSlots / (float)backpackColumns));

        for (int i = 0; i < craftingCatalog.itemDefinitions.Length; i++)
        {
            CraftingItemDefinition definition = craftingCatalog.itemDefinitions[i];
            if (definition == null
                || string.IsNullOrEmpty(definition.itemId)
                || definition.category != ToolCategory.Tools && definition.category != ToolCategory.Melee
                || GetItemCount(definition.itemId) > 0)
            {
                continue;
            }

            TryAddOrMergeInventorySlot(InventoryUtility.CreateSlotFromDefinition(definition, 1));
        }

        ApplyRuntimeSlotFixes();
        ResetHotbarToFirstSlots();
    }

    // Attempts to try add inventory item and returns whether the operation succeeded.
    public bool TryAddInventoryItem(string itemName, GameObject prefab, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
        {
            return false;
        }

        if (slots == null)
        {
            slots = new ToolSlot[0];
        }

        string normalizedId = InventoryUtility.NormalizeItemId(itemName);
        CraftingItemDefinition itemDefinition = craftingCatalog != null ? craftingCatalog.FindItem(normalizedId) : null;
        ToolSlot existingTemplate = FindInventoryTemplate(normalizedId);
        bool isStoneMaterial = itemName.ToLowerInvariant().Contains("stone") || itemName.ToLowerInvariant().Contains("rock");
        Vector3 fallbackHeldPosition = isStoneMaterial ? new Vector3(0.02f, -0.01f, 0.05f) : new Vector3(0.03f, -0.02f, 0.05f);
        Vector3 fallbackHeldEuler = isStoneMaterial ? new Vector3(8f, 150f, -10f) : new Vector3(0f, 160f, -18f);
        Vector3 fallbackHeldScale = isStoneMaterial ? Vector3.one * 0.34f : Vector3.one * 0.4f;
        ToolSlot slot = new ToolSlot
        {
            itemId = normalizedId,
            displayName = itemName,
            prefab = ResolveSlotPrefab(ResolvePickupHeldPrefab(itemDefinition, existingTemplate, prefab), normalizedId),
            worldPrefab = ResolveSlotPrefab(ResolvePickupWorldPrefab(itemDefinition, existingTemplate, prefab), normalizedId),
            actionType = ToolActionType.None,
            category = itemDefinition != null ? itemDefinition.category : existingTemplate != null ? existingTemplate.category : ToolCategory.Materials,
            holdPose = itemDefinition != null ? itemDefinition.holdPose : existingTemplate != null ? existingTemplate.holdPose : ToolHoldPose.OneHandTool,
            heldLocalPosition = itemDefinition != null ? itemDefinition.heldLocalPosition : existingTemplate != null ? existingTemplate.heldLocalPosition : fallbackHeldPosition,
            heldLocalEuler = itemDefinition != null ? itemDefinition.heldLocalEuler : existingTemplate != null ? existingTemplate.heldLocalEuler : fallbackHeldEuler,
            heldLocalScale = itemDefinition != null ? itemDefinition.heldLocalScale : existingTemplate != null ? existingTemplate.heldLocalScale : fallbackHeldScale,
            stackCount = amount,
            maxStack = itemDefinition != null ? Mathf.Max(1, itemDefinition.maxStack) : existingTemplate != null ? Mathf.Max(1, existingTemplate.maxStack) : GetDefaultStackLimit(normalizedId),
            nonGunDirectionFlipped = true
        };
        bool added = TryAddOrMergeInventorySlot(slot);
        if (added)
        {
            ForestQuestSystem.NotifyItemAdded(normalizedId, amount);
        }

        return added;
    }

    // Finds, loads, or caches the references needed for find inventory template.
    private ToolSlot FindInventoryTemplate(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        ToolSlot template = FindSlotTemplate(slots, itemId);
        return template != null ? template : FindConfiguredSlotTemplate(itemId);
    }

    private ToolSlot FindConfiguredSlotTemplate(string itemId)
    {
        return FindSlotTemplate(configuredSlotTemplates, itemId);
    }

    private static ToolSlot FindSlotTemplate(ToolSlot[] sourceSlots, string itemId)
    {
        if (sourceSlots == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        for (int i = 0; i < sourceSlots.Length; i++)
        {
            ToolSlot slot = sourceSlots[i];
            if (slot != null && InventoryUtility.GetItemId(slot) == itemId)
            {
                return slot;
            }
        }

        return null;
    }

    // Finds, loads, or caches the references needed for resolve pickup held prefab.
    private static GameObject ResolvePickupHeldPrefab(CraftingItemDefinition definition, ToolSlot template, GameObject fallback)
    {
        if (definition != null && definition.heldPrefab != null) return definition.heldPrefab;
        if (template != null && template.prefab != null) return template.prefab;
        if (definition != null && definition.worldPrefab != null) return definition.worldPrefab;
        if (template != null && template.worldPrefab != null) return template.worldPrefab;
        return fallback;
    }

    // Finds, loads, or caches the references needed for resolve pickup world prefab.
    private static GameObject ResolvePickupWorldPrefab(CraftingItemDefinition definition, ToolSlot template, GameObject fallback)
    {
        if (definition != null && definition.worldPrefab != null) return definition.worldPrefab;
        if (template != null && template.worldPrefab != null) return template.worldPrefab;
        if (definition != null && definition.heldPrefab != null) return definition.heldPrefab;
        if (template != null && template.prefab != null) return template.prefab;
        return fallback;
    }

    // Attempts to try craft and returns whether the operation succeeded.
    public bool TryCraft(CraftingRecipe recipe)
    {
        if (recipe == null || craftingCatalog == null)
        {
            return false;
        }

        if (!CanCraft(recipe))
        {
            return false;
        }

        CraftingItemDefinition itemDefinition = craftingCatalog.FindItem(recipe.outputItemId);
        if (itemDefinition == null)
        {
            return false;
        }

        ToolSlot outputSlot = CreateCraftedOutputSlot(
            itemDefinition,
            Mathf.Max(1, recipe.outputCount));
        if (!CanFitInventorySlot(outputSlot))
        {
            return false;
        }

        if (!ConsumeRecipeFuel(recipe))
        {
            return false;
        }

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            RecipeIngredient ingredient = recipe.ingredients[i];
            if (!RemoveInventoryItem(ingredient.itemId, ingredient.amount))
            {
                return false;
            }
        }

        AddOrMergeInventorySlot(outputSlot);
        ForestQuestSystem.NotifyCrafted(recipe.outputItemId);
        return true;
    }

    // Calculates and returns the result for can craft.
    public bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.ingredients == null)
        {
            return false;
        }

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            RecipeIngredient ingredient = recipe.ingredients[i];
            if (GetItemCount(ingredient.itemId) < ingredient.amount)
            {
                return false;
            }
        }

        if (!HasRecipeFuel(recipe))
        {
            return false;
        }

        if (craftingCatalog != null)
        {
            CraftingItemDefinition itemDefinition = craftingCatalog.FindItem(recipe.outputItemId);
            if (itemDefinition != null)
            {
                ToolSlot outputSlot = CreateCraftedOutputSlot(
                    itemDefinition,
                    Mathf.Max(1, recipe.outputCount));
                if (!CanFitInventorySlot(outputSlot))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create crafted output slot.
    private ToolSlot CreateCraftedOutputSlot(CraftingItemDefinition definition, int amount)
    {
        if (definition == null)
        {
            return null;
        }

        ToolSlot template = FindInventoryTemplate(definition.itemId);
        ToolSlot output = template != null
            ? InventoryUtility.CloneSlot(template, Mathf.Max(1, amount))
            : InventoryUtility.CreateSlotFromDefinition(definition, Mathf.Max(1, amount));

        if (output == null)
        {
            return null;
        }

        output.itemId = definition.itemId;
        output.displayName = definition.displayName;
        output.stackCount = Mathf.Max(1, amount);
        output.maxStack = Mathf.Max(1, definition.maxStack);
        if (definition.durability > 0 || template == null)
        {
            output.durability = definition.durability;
            output.maxDurability = definition.durability;
        }

        if (definition.damage > 0 || template == null) output.damage = definition.damage;
        if (definition.defense > 0 || template == null) output.defense = definition.defense;
        if (definition.healAmount > 0 || template == null) output.healAmount = definition.healAmount;
        if (definition.harvestSeconds > 0f || template == null) output.harvestSeconds = definition.harvestSeconds;

        ApplyRuntimeSlotFix(output);
        return output;
    }

    // Calculates and returns the result for get item count.
    public int GetItemCount(string itemId)
    {
        int count = InventoryUtility.CountItem(slots, itemId);
        if (!string.IsNullOrEmpty(itemId))
        {
            if (equippedHelmet != null && InventoryUtility.GetItemId(equippedHelmet) == itemId)
            {
                count += Mathf.Max(1, equippedHelmet.stackCount);
            }

            if (equippedArmor != null && InventoryUtility.GetItemId(equippedArmor) == itemId)
            {
                count += Mathf.Max(1, equippedArmor.stackCount);
            }
        }

        return count;
    }

    // Calculates and returns the result for get equipped defense.
    public int GetEquippedDefense()
    {
        int defense = 0;
        if (equippedHelmet != null)
        {
            defense += Mathf.Max(0, equippedHelmet.defense);
        }

        if (equippedArmor != null)
        {
            defense += Mathf.Max(0, equippedArmor.defense);
        }

        return defense;
    }

    // Calculates and returns the result for get visual root.
    public Transform GetVisualRoot()
    {
        visualRoot = ResolveVisualRoot();
        return visualRoot;
    }

    // Calculates and returns the result for get equipped armor durability.
    public void GetEquippedArmorDurability(out int durability, out int maxDurability)
    {
        durability = 0;
        maxDurability = 0;
        AddArmorDurability(equippedHelmet, ref durability, ref maxDurability);
        AddArmorDurability(equippedArmor, ref durability, ref maxDurability);
    }

    // Adds, spawns, or attaches the objects and data for add armor durability.
    private static void AddArmorDurability(ToolSlot slot, ref int durability, ref int maxDurability)
    {
        if (slot == null)
        {
            return;
        }

        int slotMax = Mathf.Max(0, slot.maxDurability);
        maxDurability += slotMax;
        durability += Mathf.Clamp(slot.durability > 0 ? slot.durability : slotMax, 0, slotMax);
    }

    // Handles the equip armor from inventory index workflow.
    public bool EquipArmorFromInventoryIndex(int inventoryIndex)
    {
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || slots[inventoryIndex] == null)
        {
            return false;
        }

        ToolSlot slot = slots[inventoryIndex];
        if (!IsHelmetSlot(slot) && !IsBodyArmorSlot(slot))
        {
            return false;
        }

        ToolSlot previous = IsHelmetSlot(slot) ? equippedHelmet : equippedArmor;
        ToolSlot equippedCopy = InventoryUtility.CloneSlot(slot, 1);
        slots[inventoryIndex].stackCount -= 1;
        slots = InventoryUtility.Compact(slots);

        if (IsHelmetSlot(equippedCopy))
        {
            equippedHelmet = equippedCopy;
        }
        else
        {
            equippedArmor = equippedCopy;
        }

        if (previous != null)
        {
            slots = InventoryUtility.AddItem(slots, previous);
        }

        RebuildHeldToolsAfterInventoryChange();
        RefreshEquippedArmorVisuals();
        return true;
    }

    // Handles the unequip helmet workflow.
    public bool UnequipHelmet()
    {
        return UnequipArmorSlot(true);
    }

    // Handles the unequip armor workflow.
    public bool UnequipArmor()
    {
        return UnequipArmorSlot(false);
    }

    // Handles the unequip armor slot workflow.
    private bool UnequipArmorSlot(bool helmet)
    {
        ToolSlot slot = helmet ? equippedHelmet : equippedArmor;
        if (slot == null || !CanFitInventorySlot(slot))
        {
            return false;
        }

        if (helmet)
        {
            equippedHelmet = null;
        }
        else
        {
            equippedArmor = null;
        }

        slots = InventoryUtility.AddItem(slots, slot);
        RebuildHeldToolsAfterInventoryChange();
        RefreshEquippedArmorVisuals();
        return true;
    }

    // Calculates and returns the result for is helmet slot.
    private static bool IsHelmetSlot(ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        return slot != null && slot.category == ToolCategory.Armor && itemId.Contains("helmet");
    }

    // Calculates and returns the result for is body armor slot.
    private static bool IsBodyArmorSlot(ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        return slot != null && slot.category == ToolCategory.Armor && itemId.Contains("armor") && !itemId.Contains("helmet");
    }

    // Clears runtime objects, cached data, or temporary state for remove inventory item.
    public bool RemoveInventoryItem(string itemId, int amount)
    {
        if (!InventoryUtility.RemoveItem(slots, itemId, amount, out ToolSlot[] updated))
        {
            return false;
        }

        slots = updated;
        RebuildHeldToolsAfterInventoryChange();
        return true;
    }

    // Adds, spawns, or attaches the objects and data for add or merge inventory slot.
    public void AddOrMergeInventorySlot(ToolSlot slot)
    {
        TryAddOrMergeInventorySlot(slot);
    }

    // Handles the restore saved inventory workflow.
    public void RestoreSavedInventory(System.Collections.Generic.List<ForestSaveSystem.SavedSlot> savedSlots)
    {
        slots = new ToolSlot[0];
        equippedHelmet = null;
        equippedArmor = null;

        if (savedSlots != null && craftingCatalog != null)
        {
            for (int i = 0; i < savedSlots.Count; i++)
            {
                ForestSaveSystem.SavedSlot saved = savedSlots[i];
                CraftingItemDefinition definition = craftingCatalog.FindItem(saved.itemId);
                if (definition == null || saved.stackCount <= 0)
                {
                    continue;
                }

                ToolSlot slot = InventoryUtility.CreateSlotFromDefinition(definition, saved.stackCount);
                slot.durability = saved.durability;
                ApplyRuntimeSlotFix(slot);
                slots = InventoryUtility.AddItem(slots, slot);
            }
        }

        EnsureToolActions();
        ApplyRuntimeSlotFixes();
        ResetHotbarToFirstSlots();
        RebuildHeldToolsAfterInventoryChange();
        RefreshEquippedArmorVisuals();
    }

    // Attempts to try add or merge inventory slot and returns whether the operation succeeded.
    public bool TryAddOrMergeInventorySlot(ToolSlot slot)
    {
        if (slot == null || slot.stackCount <= 0)
        {
            return false;
        }

        ApplyRuntimeSlotFix(slot);
        slots = InventoryUtility.Compact(slots);

        if (!CanFitInventorySlot(slot))
        {
            return false;
        }

        slots = InventoryUtility.AddItem(slots, slot);
        RebuildHeldToolsAfterInventoryChange();
        return true;
    }

    public int ApplyDeathInventoryPenalty()
    {
        slots = InventoryUtility.Compact(slots);
        if (slots == null || slots.Length == 0)
        {
            return 0;
        }

        int eligibleCount = CountDeathPenaltyEligibleItems();
        if (eligibleCount <= 0)
        {
            return 0;
        }

        int removeCount = Mathf.Clamp(Mathf.FloorToInt(eligibleCount * 0.2f), 1, eligibleCount);
        int removed = 0;
        for (int i = 0; i < removeCount; i++)
        {
            if (!RemoveRandomDeathPenaltyItem())
            {
                break;
            }

            removed++;
        }

        if (removed > 0)
        {
            RebuildHeldToolsAfterInventoryChange();
        }

        return removed;
    }

    private int CountDeathPenaltyEligibleItems()
    {
        int count = 0;
        if (slots == null)
        {
            return count;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (IsDeathPenaltyEligibleSlot(slot))
            {
                count += Mathf.Max(0, slot.stackCount);
            }
        }

        return count;
    }

    private bool RemoveRandomDeathPenaltyItem()
    {
        int eligibleCount = CountDeathPenaltyEligibleItems();
        if (eligibleCount <= 0)
        {
            return false;
        }

        int target = Random.Range(0, eligibleCount);
        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (!IsDeathPenaltyEligibleSlot(slot))
            {
                continue;
            }

            int stackCount = Mathf.Max(0, slot.stackCount);
            if (target >= stackCount)
            {
                target -= stackCount;
                continue;
            }

            slot.stackCount--;
            slots = InventoryUtility.Compact(slots);
            return true;
        }

        return false;
    }

    private static bool IsDeathPenaltyEligibleSlot(ToolSlot slot)
    {
        if (!InventoryUtility.IsValidSlot(slot))
        {
            return false;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        return itemId != "wood_axe"
            && itemId != "wood_pickaxe"
            && itemId != "wood_sword";
    }

    // Calculates and returns the result for can fit inventory slot.
    public bool CanFitInventorySlot(ToolSlot slot)
    {
        return InventoryUtility.CanAddItem(slots, slot, InventorySlotCapacity);
    }

    // Calculates and returns the result for has recipe fuel.
    private bool HasRecipeFuel(CraftingRecipe recipe)
    {
        CraftingStation station = GetRecipeStation(recipe);
        if (station == CraftingStation.Furnace)
        {
            return furnaceFuelCharges > 0 || GetItemCount("coal") > 0 || GetItemCount("charcoal") > 0;
        }

        if (station == CraftingStation.Forge)
        {
            return GetItemCount("coal_ore") >= 2 || GetItemCount("coal") >= 2;
        }

        return true;
    }

    // Handles the consume recipe fuel workflow.
    private bool ConsumeRecipeFuel(CraftingRecipe recipe)
    {
        CraftingStation station = GetRecipeStation(recipe);
        if (station == CraftingStation.Furnace)
        {
            if (furnaceFuelCharges <= 0)
            {
                if (RemoveInventoryItem("coal", 1) || RemoveInventoryItem("charcoal", 1))
                {
                    furnaceFuelCharges = 3;
                }
            }

            if (furnaceFuelCharges <= 0)
            {
                return false;
            }

            furnaceFuelCharges--;
            return true;
        }

        if (station == CraftingStation.Forge)
        {
            if (RemoveInventoryItem("coal_ore", 2))
            {
                return true;
            }

            return RemoveInventoryItem("coal", 2);
        }

        return true;
    }

    // Calculates and returns the result for get recipe station.
    private static CraftingStation GetRecipeStation(CraftingRecipe recipe)
    {
        if (recipe == null)
        {
            return CraftingStation.Player;
        }

        if (recipe.requiresWorkbench && recipe.station == CraftingStation.Player)
        {
            return CraftingStation.Workbench;
        }

        return recipe.station;
    }

    // Calculates and returns the result for get default stack limit.
    private static int GetDefaultStackLimit(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 64;
        }

        if (itemId.Contains("axe") || itemId.Contains("pick") || itemId.Contains("sword")
            || itemId.Contains("helmet") || itemId.Contains("armor") || itemId.Contains("shears") || itemId.Contains("scissors"))
        {
            return 1;
        }

        return 64;
    }

    // Refreshes and applies configuration or runtime state for refresh inventory ui.
    private void RefreshInventoryUI()
    {
        if (hotbarUI != null)
        {
            hotbarUI.Refresh(this);
        }

        if (backpackUI != null)
        {
            backpackUI.Refresh(this);
        }
    }

    // Handles the rebuild held tools after inventory change workflow.
    private void RebuildHeldToolsAfterInventoryChange()
    {
        string[] hotbarItemIds = CaptureHotbarItemIds();
        slots = InventoryUtility.Compact(slots);
        RestoreHotbarMapping(hotbarItemIds);
        BuildHeldTools();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, hotbarSize - 1));
        SelectSlot(selectedIndex);
        RefreshInventoryUI();
    }

    // Handles the capture hotbar item ids workflow.
    private string[] CaptureHotbarItemIds()
    {
        EnsureHotbarMapping();
        if (hotbarSlotIndices == null)
        {
            return new string[0];
        }

        string[] itemIds = new string[hotbarSlotIndices.Length];
        for (int i = 0; i < hotbarSlotIndices.Length; i++)
        {
            int inventoryIndex = hotbarSlotIndices[i];
            itemIds[i] = slots != null && inventoryIndex >= 0 && inventoryIndex < slots.Length
                ? InventoryUtility.GetItemId(slots[inventoryIndex])
                : string.Empty;
        }

        return itemIds;
    }

    // Handles the restore hotbar mapping workflow.
    private void RestoreHotbarMapping(string[] itemIds)
    {
        hotbarSize = Mathf.Clamp(hotbarSize <= 0 ? 9 : hotbarSize, 1, 9);
        hotbarSlotIndices = new int[hotbarSize];
        bool[] used = slots != null ? new bool[slots.Length] : new bool[0];
        for (int i = 0; i < hotbarSlotIndices.Length; i++)
        {
            string itemId = itemIds != null && i < itemIds.Length ? itemIds[i] : string.Empty;
            hotbarSlotIndices[i] = FindInventoryIndexByItemId(itemId, used);
        }
    }

    // Finds, loads, or caches the references needed for find inventory index by item id.
    private int FindInventoryIndexByItemId(string itemId, bool[] used)
    {
        if (slots == null || string.IsNullOrEmpty(itemId))
        {
            return -1;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (used != null && i < used.Length && used[i])
            {
                continue;
            }

            if (InventoryUtility.GetItemId(slots[i]) == itemId)
            {
                if (used != null && i < used.Length)
                {
                    used[i] = true;
                }

                return i;
            }
        }

        return -1;
    }

    // Refreshes and applies configuration or runtime state for refresh equipped armor visuals.
    private void RefreshEquippedArmorVisuals()
    {
        DestroyEquippedVisual(ref equippedHelmetVisual);
        DestroyEquippedVisual(ref equippedArmorVisual);
        CacheEquippedArmorTuning();

        equippedHelmetVisual = CreateEquippedArmorVisual(
            equippedHelmet,
            "EquippedHelmetVisual",
            equippedHelmetLocalPosition,
            equippedHelmetLocalEuler,
            equippedHelmetLocalScale);
        equippedArmorVisual = CreateEquippedArmorVisual(
            equippedArmor,
            "EquippedArmorVisual",
            equippedArmorLocalPosition,
            equippedArmorLocalEuler,
            equippedArmorLocalScale);
    }

    // Handles the handle equipped armor visual tuning workflow.
    private void HandleEquippedArmorVisualTuning()
    {
        if (equippedHelmetVisual != null)
        {
            ApplyEquippedVisualTransform(equippedHelmetVisual.transform, equippedHelmetLocalPosition, equippedHelmetLocalEuler, equippedHelmetLocalScale);
        }

        if (equippedArmorVisual != null)
        {
            ApplyEquippedVisualTransform(equippedArmorVisual.transform, equippedArmorLocalPosition, equippedArmorLocalEuler, equippedArmorLocalScale);
        }

        CacheEquippedArmorTuning();
    }

    // Finds, loads, or caches the references needed for cache equipped armor tuning.
    private void CacheEquippedArmorTuning()
    {
        lastEquippedHelmetLocalPosition = equippedHelmetLocalPosition;
        lastEquippedHelmetLocalEuler = equippedHelmetLocalEuler;
        lastEquippedHelmetLocalScale = equippedHelmetLocalScale;
        lastEquippedArmorLocalPosition = equippedArmorLocalPosition;
        lastEquippedArmorLocalEuler = equippedArmorLocalEuler;
        lastEquippedArmorLocalScale = equippedArmorLocalScale;
    }

    // Refreshes and applies configuration or runtime state for apply equipped visual transform.
    private static void ApplyEquippedVisualTransform(Transform visual, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        if (visual == null)
        {
            return;
        }

        if (visual.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
        {
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = ResolveEquippedVisualScale(visual.name, localScale);
            return;
        }

        visual.localPosition = localPosition;
        visual.localRotation = Quaternion.Euler(localEuler);
        visual.localScale = localScale == Vector3.zero ? Vector3.one : localScale;
    }


    // Finds, loads, or caches the references needed for resolve equipped visual scale.
    private static Vector3 ResolveEquippedVisualScale(string visualName, Vector3 requestedScale)
    {
        Vector3 scale = requestedScale == Vector3.zero ? Vector3.one : requestedScale;
        string lowerName = (visualName ?? string.Empty).ToLowerInvariant();
        if (lowerName.Contains("armor") && IsApproximatelyOne(scale))
        {
            return Vector3.one * 1.045f;
        }

        if (lowerName.Contains("helmet") && IsApproximatelyOne(scale))
        {
            return Vector3.one * 1.025f;
        }

        return scale;
    }

    // Calculates and returns the result for is approximately one.
    private static bool IsApproximatelyOne(Vector3 value)
    {
        return Mathf.Abs(value.x - 1f) < 0.001f
            && Mathf.Abs(value.y - 1f) < 0.001f
            && Mathf.Abs(value.z - 1f) < 0.001f;
    }
    // Clears runtime objects, cached data, or temporary state for destroy equipped visual.
    private void DestroyEquippedVisual(ref GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        Destroy(visual);
        visual = null;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create equipped armor visual.
    private GameObject CreateEquippedArmorVisual(ToolSlot slot, string name, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        if (slot == null || slot.prefab == null)
        {
            return null;
        }

        visualRoot = ResolveVisualRoot();
        Transform parent = visualRoot != null ? visualRoot : transform;
        Dictionary<string, Transform> playerBones = BuildPlayerBoneMap(parent);
        GameObject instance = Instantiate(slot.prefab, parent);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        Vector3 visualScale = ResolveEquippedVisualScale(name, localScale);
        instance.transform.localScale = visualScale;
        RemoveHeldPhysics(instance);
        SetCollidersEnabled(instance, false);
        if (RemapArmorBonesToPlayer(instance, playerBones))
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = visualScale;
        }

        HideUnneededArmorSkeleton(instance);
        return instance;
    }

    private Dictionary<string, Transform> BuildPlayerBoneMap(Transform playerVisualRoot)
    {
        Dictionary<string, Transform> playerBones = new Dictionary<string, Transform>();
        if (playerVisualRoot == null)
        {
            return playerBones;
        }

        Transform[] playerTransforms = playerVisualRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < playerTransforms.Length; i++)
        {
            if (IsEquippedArmorVisualTransform(playerTransforms[i]))
            {
                continue;
            }

            string normalizedName = NormalizeBoneName(playerTransforms[i].name);
            if (!playerBones.ContainsKey(normalizedName))
            {
                playerBones.Add(normalizedName, playerTransforms[i]);
            }
        }

        return playerBones;
    }

    // Handles the remap armor bones to player workflow.
    private bool RemapArmorBonesToPlayer(GameObject armorInstance, Dictionary<string, Transform> playerBones)
    {
        if (armorInstance == null || playerBones == null || playerBones.Count == 0)
        {
            return false;
        }

        bool remappedAny = false;
        SkinnedMeshRenderer[] renderers = armorInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            Transform[] sourceBones = renderer.bones;
            Transform[] mappedBones = new Transform[sourceBones.Length];
            for (int b = 0; b < sourceBones.Length; b++)
            {
                Transform sourceBone = sourceBones[b];
                Transform mapped = null;
                if (sourceBone != null)
                {
                    playerBones.TryGetValue(NormalizeBoneName(sourceBone.name), out mapped);
                }

                mappedBones[b] = mapped != null ? mapped : sourceBone;
                remappedAny |= mapped != null;
            }

            renderer.bones = mappedBones;
            if (renderer.rootBone != null && playerBones.TryGetValue(NormalizeBoneName(renderer.rootBone.name), out Transform mappedRoot))
            {
                renderer.rootBone = mappedRoot;
            }
        }

        return remappedAny;
    }

    // Calculates and returns the result for is equipped armor visual transform.
    private static bool IsEquippedArmorVisualTransform(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.name == "EquippedHelmetVisual" || candidate.name == "EquippedArmorVisual")
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }

    // Handles the hide unneeded armor skeleton workflow.
    private static void HideUnneededArmorSkeleton(GameObject armorInstance)
    {
        if (armorInstance == null)
        {
            return;
        }

        SkinnedMeshRenderer[] renderers = armorInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        HashSet<Transform> keep = new HashSet<Transform>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform current = renderers[i].transform;
            while (current != null && current != armorInstance.transform)
            {
                keep.Add(current);
                current = current.parent;
            }
        }

        Transform[] children = armorInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == armorInstance.transform || keep.Contains(child))
            {
                continue;
            }

            if (child.GetComponent<Renderer>() == null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    // Handles the normalize bone name workflow.
    private static string NormalizeBoneName(string boneName)
    {
        if (string.IsNullOrEmpty(boneName))
        {
            return string.Empty;
        }

        int colonIndex = boneName.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < boneName.Length - 1)
        {
            boneName = boneName.Substring(colonIndex + 1);
        }

        return boneName.Replace("PT_", string.Empty).Replace("mixamorig", string.Empty).ToLowerInvariant();
    }

    // Clears runtime objects, cached data, or temporary state for remove inventory item at index.
    public bool RemoveInventoryItemAtIndex(int inventoryIndex, int amount)
    {
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || amount <= 0)
        {
            return false;
        }

        ToolSlot slot = slots[inventoryIndex];
        if (!InventoryUtility.IsValidSlot(slot) || slot.stackCount < amount)
        {
            return false;
        }

        slot.stackCount -= amount;
        RebuildHeldToolsAfterInventoryChange();
        return true;
    }
    // Handles the consume inventory index workflow.
    private void ConsumeInventoryIndex(int inventoryIndex, int amount)
    {
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || amount <= 0)
        {
            return;
        }

        slots[inventoryIndex].stackCount -= amount;
        RebuildHeldToolsAfterInventoryChange();
    }

    // Handles the spend selected tool durability workflow.
    private void SpendSelectedToolDurability(int inventoryIndex, int amount)
    {
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || amount <= 0)
        {
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        if (slot == null || slot.maxDurability <= 0)
        {
            return;
        }

        slot.durability -= amount;
        if (slot.durability <= 0)
        {
            ConsumeInventoryIndex(inventoryIndex, 1);
            return;
        }

        RefreshInventoryUI();
    }

    // Refreshes and applies configuration or runtime state for sync grip tuning fields.
    public void SyncGripTuningFields()
    {
        if (slots == null || slots.Length == 0)
        {
            selectedIndex = 0;
            gripTuningSlotIndex = 0;
            gripTuningSlotName = string.Empty;
            return;
        }

        EnsureHotbarMapping();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, hotbarSize - 1));
        SyncGripTuningFromSelectedSlot();
    }

    // Refreshes and applies configuration or runtime state for apply held tool transform.
    private void ApplyHeldToolTransform()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (inventoryIndex < 0 || inventoryIndex >= heldToolInstances.Count || heldToolInstances[inventoryIndex] == null)
        {
            return;
        }

        Transform tool = heldToolInstances[inventoryIndex].transform;
        ToolSlot slot = slots[inventoryIndex];
        ApplyToolTransform(tool, slot);
    }

    // Refreshes and applies configuration or runtime state for refresh held material slot visual.
    private void RefreshHeldMaterialSlotVisual(int inventoryIndex)
    {
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || heldToolInstances == null || inventoryIndex >= heldToolInstances.Count)
        {
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        GameObject instance = heldToolInstances[inventoryIndex];
        if (slot == null || instance == null || slot.category != ToolCategory.Materials)
        {
            return;
        }

        ApplyHeldMaterialItemVisual(instance, slot);
    }

    // Refreshes and applies configuration or runtime state for apply tool transform.
    private void ApplyToolTransform(Transform tool, ToolSlot slot)
    {
        if (tool == null || slot == null)
        {
            return;
        }

        tool.localPosition = slot.heldLocalPosition;
        tool.localRotation = Quaternion.Euler(slot.heldLocalEuler);
        tool.localScale = slot.heldLocalScale == Vector3.zero ? Vector3.one : slot.heldLocalScale;
    }

    // Handles the handle grip tuning input workflow.
    private void HandleGripTuningInput()
    {
        if (slots == null || slots.Length == 0)
        {
            return;
        }

        int selectedInventoryIndex = GetSelectedInventoryIndex();
        if (selectedInventoryIndex < 0 || selectedInventoryIndex >= slots.Length)
        {
            return;
        }

        if (!applyGripTuningToSelectedSlot)
        {
            return;
        }

        ToolSlot slot = slots[selectedInventoryIndex];
        slot.heldLocalPosition = gripHeldLocalPosition;
        slot.heldLocalEuler = gripHeldLocalEuler;
        slot.heldLocalScale = gripHeldLocalScale == Vector3.zero ? Vector3.one : gripHeldLocalScale;
        slot.rightHandGripLocal = gripRightHandLocal;
        slot.leftHandGripLocal = gripLeftHandLocal;

        ApplyHeldToolTransform();
        UpdateGunIKTarget();
    }

    // Handles the handle grip flip input workflow.
    private void HandleGripFlipInput()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (!Input.GetKeyDown(flipSelectedToolKey) || slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length)
        {
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        slot.heldLocalEuler = new Vector3(slot.heldLocalEuler.x, NormalizeEuler(slot.heldLocalEuler.y + 180f), slot.heldLocalEuler.z);
        SyncGripTuningFromSelectedSlot();
        ApplyHeldToolTransform();
        UpdateGunIKTarget();
    }

    // Refreshes and applies configuration or runtime state for apply saved non gun direction flip.
    private void ApplySavedNonGunDirectionFlip()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (slot == null || slot.nonGunDirectionFlipped)
            {
                continue;
            }

            slot.heldLocalEuler = new Vector3(slot.heldLocalEuler.x, NormalizeEuler(slot.heldLocalEuler.y + 180f), slot.heldLocalEuler.z);
            slot.nonGunDirectionFlipped = true;
        }
    }

    // Handles the handle grip roll input workflow.
    private void HandleGripRollInput()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (!Input.GetKeyDown(rollSelectedToolKey) || slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length)
        {
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        slot.heldLocalEuler = new Vector3(slot.heldLocalEuler.x, slot.heldLocalEuler.y, NormalizeEuler(slot.heldLocalEuler.z + 180f));
        SyncGripTuningFromSelectedSlot();
        ApplyHeldToolTransform();
        UpdateGunIKTarget();
    }

    // Handles the handle grip roll left input workflow.
    private void HandleGripRollLeftInput()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (!Input.GetKeyDown(rollSelectedToolLeftKey) || slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length)
        {
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        slot.heldLocalEuler = new Vector3(slot.heldLocalEuler.x, slot.heldLocalEuler.y, NormalizeEuler(slot.heldLocalEuler.z - 90f));
        SyncGripTuningFromSelectedSlot();
        ApplyHeldToolTransform();
        UpdateGunIKTarget();
    }

    // Handles the normalize euler workflow.
    private static float NormalizeEuler(float value)
    {
        value %= 360f;
        if (value > 180f) value -= 360f;
        if (value < -180f) value += 360f;
        return value;
    }

    // Refreshes and applies configuration or runtime state for sync grip tuning from selected slot.
    private void SyncGripTuningFromSelectedSlot()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (slots == null || inventoryIndex < 0 || inventoryIndex >= slots.Length || slots[inventoryIndex] == null)
        {
            gripTuningSlotName = string.Empty;
            return;
        }

        ToolSlot slot = slots[inventoryIndex];
        gripTuningSlotIndex = inventoryIndex;
        gripTuningSlotName = slot.displayName;
        gripHeldLocalPosition = slot.heldLocalPosition;
        gripHeldLocalEuler = slot.heldLocalEuler;
        gripHeldLocalScale = slot.heldLocalScale == Vector3.zero ? Vector3.one : slot.heldLocalScale;
        gripRightHandLocal = slot.rightHandGripLocal;
        gripLeftHandLocal = slot.leftHandGripLocal;
    }

    // Sets state, selection, or placement data for setup gun ik.
    private void SetupGunIK()
    {
    }

    // Ensures the objects, references, or configuration required for ensure hotbar mapping exist.
    private void EnsureHotbarMapping()
    {
        if (slots == null)
        {
            hotbarSlotIndices = new int[0];
            return;
        }

        hotbarSize = Mathf.Clamp(hotbarSize <= 0 ? 9 : hotbarSize, 1, 9);
        bool needsReset = hotbarSlotIndices == null || hotbarSlotIndices.Length != hotbarSize;
        if (!needsReset && slots.Length > 0)
        {
            bool[] used = new bool[slots.Length];
            for (int i = 0; i < hotbarSlotIndices.Length; i++)
            {
                int inventoryIndex = hotbarSlotIndices[i];
                if (inventoryIndex < 0)
                {
                    continue;
                }

                if (inventoryIndex >= slots.Length || used[inventoryIndex])
                {
                    needsReset = true;
                    break;
                }

                used[inventoryIndex] = true;
            }
        }

        if (needsReset)
        {
            int[] newMapping = new int[hotbarSize];
            for (int i = 0; i < hotbarSize; i++)
            {
                newMapping[i] = i < slots.Length ? i : -1;
            }

            hotbarSlotIndices = newMapping;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, hotbarSize - 1);
            return;
        }

        for (int i = 0; i < hotbarSlotIndices.Length; i++)
        {
            if (slots.Length == 0 || hotbarSlotIndices[i] >= slots.Length)
            {
                hotbarSlotIndices[i] = -1;
            }
        }
    }

    // Calculates and returns the result for get selected inventory index.
    private int GetSelectedInventoryIndex()
    {
        return GetInventoryIndexForHotbar(selectedIndex);
    }

    // Refreshes and applies configuration or runtime state for update gun iktarget.
    private void UpdateGunIKTarget()
    {
    }

    // Handles the animate tool swing workflow.
    private void AnimateToolSwing()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        if (inventoryIndex < 0 || inventoryIndex >= heldToolInstances.Count || heldToolInstances[inventoryIndex] == null)
        {
            return;
        }

        ToolSlot selectedSlot = slots != null && inventoryIndex >= 0 && inventoryIndex < slots.Length ? slots[inventoryIndex] : null;
        ResetSelectedToolPose();
        if (activeSwingRoutine != null)
        {
            StopCoroutine(activeSwingRoutine);
            activeSwingRoutine = null;
        }

        if (IsSwordSlot(selectedSlot))
        {
            PlaySlashAnimation();
            return;
        }

        if (IsTreeChopTool(selectedSlot) || IsRockMineTool(selectedSlot))
        {
            PlayHarvestDownwardAnimation();
        }
    }

    // Starts or stops the animation, audio, or gameplay flow for play slash animation.
    private void PlaySlashAnimation()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<CharacterRunAnimator>();
        }

        if (characterAnimator != null)
        {
            characterAnimator.PlaySlash(0.75f);
        }
    }

    // Starts or stops the animation, audio, or gameplay flow for play harvest downward animation.
    private void PlayHarvestDownwardAnimation()
    {
        int inventoryIndex = GetSelectedInventoryIndex();
        ToolSlot selectedSlot = slots != null && inventoryIndex >= 0 && inventoryIndex < slots.Length ? slots[inventoryIndex] : null;
        PlayProceduralShoulderSwing(selectedSlot);
    }

    // Starts or stops the animation, audio, or gameplay flow for play harvest hit sound on strike.
    private void PlayHarvestHitSoundOnStrike(ToolSlot selectedSlot, HarvestResourceType targetType)
    {
        if (pendingHarvestHitSoundRoutine != null)
        {
            StopCoroutine(pendingHarvestHitSoundRoutine);
            pendingHarvestHitSoundRoutine = null;
        }

        pendingHarvestHitSoundRoutine = StartCoroutine(PlayHarvestHitSoundAfterDelay(GetHarvestStrikeDelay(selectedSlot), targetType));
    }

    // Starts or stops the animation, audio, or gameplay flow for play harvest hit sound after delay.
    private System.Collections.IEnumerator PlayHarvestHitSoundAfterDelay(float delay, HarvestResourceType targetType)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        pendingHarvestHitSoundRoutine = null;
        if (Input.GetMouseButton(0) && heldChopTarget != null && heldHarvestType == targetType)
        {
            PlayHarvestHitSound(targetType);
        }
    }

    // Calculates and returns the result for get harvest strike delay.
    private static float GetHarvestStrikeDelay(ToolSlot selectedSlot)
    {
        float speedScale = Mathf.Max(0.2f, GetSwingSpeedScale(selectedSlot));
        return (selectedSlot != null && selectedSlot.category == ToolCategory.Melee ? 0.26f : 0.28f) / speedScale;
    }

    // Calculates and returns the result for get harvest swing interval.
    private float GetHarvestSwingInterval(ToolSlot selectedSlot)
    {
        float speedScale = Mathf.Max(0.2f, GetSwingSpeedScale(selectedSlot));
        float fullSwingSeconds = (0.28f + 0.34f + 0.24f) / speedScale;
        return Mathf.Max(harvestSwingDuration, fullSwingSeconds + 0.08f);
    }

    // Starts or stops the animation, audio, or gameplay flow for play harvest hit sound.
    private void PlayHarvestHitSound(HarvestResourceType targetType)
    {
        if (harvestHitVolume <= 0.001f)
        {
            return;
        }

        AudioClip clip = GetHarvestHitClip(targetType);
        if (clip == null)
        {
            return;
        }

        EnsureHarvestAudioSource();

        harvestAudioSource.Stop();
        harvestAudioSource.clip = clip;
        harvestAudioSource.volume = harvestHitVolume;
        harvestAudioSource.Play();
    }

    // Ensures the objects, references, or configuration required for ensure harvest audio source exist.
    private void EnsureHarvestAudioSource()
    {
        if (harvestAudioSource != null)
        {
            return;
        }

        harvestAudioSource = gameObject.AddComponent<AudioSource>();
        harvestAudioSource.playOnAwake = false;
        harvestAudioSource.loop = false;
        harvestAudioSource.spatialBlend = 0.35f;
        harvestAudioSource.volume = harvestHitVolume;
    }

    // Starts or stops the animation, audio, or gameplay flow for stop harvest hit sound.
    private void StopHarvestHitSound()
    {
        if (pendingHarvestHitSoundRoutine != null)
        {
            StopCoroutine(pendingHarvestHitSoundRoutine);
            pendingHarvestHitSoundRoutine = null;
        }

        if (harvestAudioSource != null && harvestAudioSource.isPlaying)
        {
            harvestAudioSource.Stop();
        }
    }

    // Calculates and returns the result for get harvest hit clip.
    private AudioClip GetHarvestHitClip(HarvestResourceType targetType)
    {
        if (targetType == HarvestResourceType.Tree)
        {
            if (treeHitClip == null)
            {
                treeHitClip = Resources.Load<AudioClip>(treeHitClipResourcePath);
            }

            return treeHitClip;
        }

        if (rockHitClip == null)
        {
            rockHitClip = Resources.Load<AudioClip>(rockHitClipResourcePath);
        }

        return rockHitClip;
    }

    // Calculates and returns the result for is sword slot.
    private static bool IsSwordSlot(ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        return !string.IsNullOrEmpty(itemId) && itemId.EndsWith("_sword", System.StringComparison.Ordinal);
    }

    // Calculates and returns the result for is axe slot.
    private static bool IsAxeSlot(ToolSlot slot)
    {
        string itemId = InventoryUtility.GetItemId(slot);
        return !string.IsNullOrEmpty(itemId) && itemId.EndsWith("_axe", System.StringComparison.Ordinal);
    }

    // Clears runtime objects, cached data, or temporary state for remove procedural arm swing.
    private void RemoveProceduralArmSwing()
    {
        ProceduralShoulderSwing swing = GetComponent<ProceduralShoulderSwing>();
        if (swing != null)
        {
            Destroy(swing);
        }
    }

    // Starts or stops the animation, audio, or gameplay flow for play procedural shoulder swing.
    private void PlayProceduralShoulderSwing(ToolSlot selectedSlot)
    {
        ProceduralShoulderSwing swing = GetComponent<ProceduralShoulderSwing>();
        if (swing == null)
        {
            swing = gameObject.AddComponent<ProceduralShoulderSwing>();
        }

        Animator animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>(true) : null;
        if (animator != null)
        {
            swing.animator = animator;
        }

        swing.Play(GetSwingStyle(selectedSlot), GetSwingSpeedScale(selectedSlot));
    }

    // Handles the swing routine workflow.
    private System.Collections.IEnumerator SwingRoutine(Transform tool, ToolSlot selectedSlot)
    {
        bool thrust = GetSwingStyle(selectedSlot) == ProceduralShoulderSwing.SwingStyle.ForwardThrust;
        float speedScale = GetSwingSpeedScale(selectedSlot);
        Vector3 startPosition = tool.localPosition;
        Quaternion startRotation = tool.localRotation;
        Vector3 windupPosition = thrust ? startPosition + new Vector3(0.05f, 0.03f, -0.18f) : startPosition + new Vector3(0.04f, 0.2f, -0.04f);
        Vector3 strikePosition = thrust ? startPosition + new Vector3(0.02f, -0.02f, 0.24f) : startPosition + new Vector3(-0.02f, -0.22f, 0.05f);
        Quaternion windupRotation = thrust ? startRotation * Quaternion.Euler(-8f, -18f, -18f) : startRotation * Quaternion.Euler(-50f, 0f, -8f);
        Quaternion strikeRotation = thrust ? startRotation * Quaternion.Euler(8f, 18f, 10f) : startRotation * Quaternion.Euler(58f, 0f, 12f);
        float windupDuration = (thrust ? 0.08f : 0.14f) / Mathf.Max(0.2f, speedScale);
        float strikeDuration = (thrust ? 0.11f : 0.2f) / Mathf.Max(0.2f, speedScale);
        float recoverDuration = (thrust ? 0.13f : 0.18f) / Mathf.Max(0.2f, speedScale);

        for (float t = 0f; t < windupDuration; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / windupDuration);
            tool.localPosition = Vector3.Lerp(startPosition, windupPosition, p);
            tool.localRotation = Quaternion.Slerp(startRotation, windupRotation, p);
            yield return null;
        }

        for (float t = 0f; t < strikeDuration; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / strikeDuration);
            tool.localPosition = Vector3.Lerp(windupPosition, strikePosition, p);
            tool.localRotation = Quaternion.Slerp(windupRotation, strikeRotation, p);
            yield return null;
        }

        for (float t = 0f; t < recoverDuration; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / recoverDuration);
            tool.localPosition = Vector3.Lerp(strikePosition, startPosition, p);
            tool.localRotation = Quaternion.Slerp(strikeRotation, startRotation, p);
            yield return null;
        }

        tool.localPosition = startPosition;
        tool.localRotation = startRotation;
        activeSwingRoutine = null;
    }

    // Calculates and returns the result for get swing style.
    private static ProceduralShoulderSwing.SwingStyle GetSwingStyle(ToolSlot slot)
    {
        return slot != null && slot.category == ToolCategory.Melee
            ? ProceduralShoulderSwing.SwingStyle.ForwardThrust
            : ProceduralShoulderSwing.SwingStyle.OverheadChop;
    }

    // Calculates and returns the result for get swing speed scale.
    private static float GetSwingSpeedScale(ToolSlot slot)
    {
        if (slot != null && IsRockMineTool(slot))
        {
            return 0.72f;
        }

        return 1f;
    }

    // Handles the reset selected tool pose workflow.
    private void ResetSelectedToolPose()
    {
        ProceduralShoulderSwing swing = GetComponent<ProceduralShoulderSwing>();
        if (swing != null)
        {
            swing.ResetPose();
        }

        if (activeSwingRoutine != null)
        {
            StopCoroutine(activeSwingRoutine);
            activeSwingRoutine = null;
        }

        ApplyHeldToolTransform();
    }

    // Clears runtime objects, cached data, or temporary state for clear held tools.
    private void ClearHeldTools()
    {
        for (int i = 0; i < heldToolInstances.Count; i++)
        {
            if (heldToolInstances[i] != null)
            {
                Destroy(heldToolInstances[i]);
            }
        }

        heldToolInstances.Clear();
    }

    // Sets state, selection, or placement data for set colliders enabled.
    private static void SetCollidersEnabled(GameObject root, bool enabled)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = enabled;
        }
    }

    // Handles the ignore owner collisions workflow.
    private void IgnoreOwnerCollisions(GameObject projectile)
    {
        if (projectile == null)
        {
            return;
        }

        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
        Collider[] ownerColliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < projectileColliders.Length; i++)
        {
            if (projectileColliders[i] == null)
            {
                continue;
            }

            for (int j = 0; j < ownerColliders.Length; j++)
            {
                if (ownerColliders[j] != null)
                {
                    Physics.IgnoreCollision(projectileColliders[i], ownerColliders[j], true);
                }
            }
        }
    }

    // Finds, loads, or caches the references needed for resolve visual root.
    private Transform ResolveVisualRoot()
    {
        Transform namedVisual = transform.Find("CharacterVisual");
        if (HasCharacterVisual(namedVisual))
        {
            return namedVisual;
        }

        if (HasCharacterVisual(visualRoot))
        {
            return visualRoot;
        }

        if (characterAnimator != null)
        {
            Transform animatorVisualRoot = characterAnimator.ResolveVisualRoot();
            if (HasCharacterVisual(animatorVisualRoot))
            {
                return animatorVisualRoot;
            }
        }

        Animator childAnimator = transform.GetComponentInChildren<Animator>(true);
        if (childAnimator != null)
        {
            Transform animatorRoot = GetTopChildUnder(transform, childAnimator.transform);
            return animatorRoot != null ? animatorRoot : childAnimator.transform;
        }

        Renderer childRenderer = transform.GetComponentInChildren<Renderer>(true);
        if (childRenderer != null)
        {
            Transform rendererRoot = GetTopChildUnder(transform, childRenderer.transform);
            return rendererRoot != null ? rendererRoot : childRenderer.transform;
        }

        return visualRoot != null ? visualRoot : transform;
    }

    // Calculates and returns the result for has character visual.
    private static bool HasCharacterVisual(Transform root)
    {
        return root != null
            && (root.GetComponentInChildren<Animator>(true) != null
                || root.GetComponentInChildren<Renderer>(true) != null);
    }

    // Calculates and returns the result for get top child under.
    private static Transform GetTopChildUnder(Transform owner, Transform child)
    {
        if (owner == null || child == null)
        {
            return null;
        }

        if (child == owner)
        {
            return owner;
        }

        Transform current = child;
        while (current.parent != null && current.parent != owner)
        {
            current = current.parent;
        }

        return current.parent == owner ? current : null;
    }

    // Finds, loads, or caches the references needed for find deep child.
    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }
}






































