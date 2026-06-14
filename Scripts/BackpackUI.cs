using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    private enum ViewMode
    {
        Inventory,
        PlayerCrafting,
        WorkbenchCrafting,
        FurnaceCrafting,
        ForgeCrafting,
        Chest
    }

    public static bool IsAnyOpen { get; private set; }

    private const float CraftingPanelRightOffset = 136f;
    private const float BackpackPanelRightOffset = 66f;
    private const int PlayerPreviewLayer = 31;

    public int columns = 9;
    public int rows = 6;
    public Color panelColor = new Color(0.06f, 0.05f, 0.04f, 0.94f);
    public Color emptyColor = new Color(0.1f, 0.09f, 0.08f, 0.85f);
    public Color itemColor = new Color(0.19f, 0.15f, 0.11f, 0.96f);
    public Color hotbarItemColor = new Color(0.27f, 0.2f, 0.12f, 0.96f);
    public Color selectedHotbarColor = new Color(1f, 0.82f, 0.25f, 0.96f);
    public Color selectedTransferColor = new Color(0.3f, 0.62f, 0.32f, 0.96f);
    public Color selectedChestColor = new Color(0.28f, 0.46f, 0.76f, 0.96f);

    private GameObject panel;
    private RectTransform panelRect;
    private Image panelBackground;
    private Transform inventoryGridRoot;
    private RectTransform inventoryGridRect;
    private Transform sideContentRoot;
    private RectTransform sideContentRect;
    private Text titleText;
    private Text detailText;
    private Button inventoryTabButton;
    private Button craftingTabButton;
    private Button craftButton;
    private Button transferToChestButton;
    private Button transferToPlayerButton;
    private Text transferAmountText;
    private RawImage playerPreviewImage;
    private RenderTexture playerPreviewTexture;
    private Camera playerPreviewCamera;
    private Sprite frameSprite;
    private GameObject playerPreviewStage;
    private GameObject playerPreviewModel;
    private PlayerToolController controller;
    private StorageChest activeChest;
    private ViewMode currentMode = ViewMode.Inventory;
    private bool isOpen;
    private CraftingRecipe selectedRecipe;
    private int selectedPlayerTransferIndex = -1;
    private int selectedChestTransferIndex = -1;
    private int transferAmount = 1;
    private readonly Dictionary<string, Texture2D> itemIconCache = new Dictionary<string, Texture2D>();
    private GameObject itemIconStage;
    private Camera itemIconCamera;

    public bool IsBuilt => panel != null && inventoryGridRoot != null && sideContentRoot != null;

    private void OnDisable()
    {
        if (isOpen)
        {
            SetOpen(false);
        }

        DestroyPlayerPreview();
        DestroyItemIcons();
    }

    public void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyChild(transform.GetChild(i).gameObject);
        }

        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        panel = new GameObject("BackpackPanel");
        panel.transform.SetParent(transform, false);
        panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1600f, 1080f);
        panelRect.anchoredPosition = new Vector2(BackpackPanelRightOffset, 0f);

        panelBackground = panel.AddComponent<Image>();
        panelBackground.color = panelColor;

        titleText = CreateText("Title", panel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -54f), new Vector2(-18f, -12f), 22, FontStyle.Bold, TextAnchor.MiddleLeft);

        inventoryTabButton = CreateButton("InventoryTab", panel.transform, new Vector2(920f, -770f), new Vector2(150f, 42f), "Inventory", () =>
        {
            currentMode = ViewMode.Inventory;
            Refresh(controller);
        });

        craftingTabButton = CreateButton("CraftingTab", panel.transform, new Vector2(1080f, -770f), new Vector2(150f, 42f), "Crafting", () =>
        {
            currentMode = ViewMode.PlayerCrafting;
            Refresh(controller);
        });

        ApplyFrameSprite(inventoryTabButton.GetComponent<Image>());
        ApplyFrameSprite(craftingTabButton.GetComponent<Image>());

        inventoryGridRoot = CreateSectionRoot("InventoryGrid", panel.transform, new Vector2(102f, -178f), new Vector2(780f, 660f));
        inventoryGridRect = inventoryGridRoot as RectTransform;
        sideContentRoot = CreateSectionRoot("SideContent", panel.transform, new Vector2(900f, -178f), new Vector2(560f, 730f));
        sideContentRect = sideContentRoot as RectTransform;
        detailText = CreateText("DetailText", sideContentRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -16f), new Vector2(-16f, -172f), 16, FontStyle.Normal, TextAnchor.UpperLeft);

        craftButton = CreateButton("CraftButton", sideContentRoot, new Vector2(16f, -560f), new Vector2(170f, 48f), "Craft", () =>
        {
            if (controller != null && selectedRecipe != null && controller.TryCraft(selectedRecipe))
            {
                Refresh(controller);
            }
        });

        transferToPlayerButton = CreateButton("TransferToPlayer", sideContentRoot, new Vector2(200f, -560f), new Vector2(150f, 48f), "< Take", () =>
        {
            TransferFromChestToPlayer();
        });

        transferToChestButton = CreateButton("TransferToChest", sideContentRoot, new Vector2(360f, -560f), new Vector2(150f, 48f), "Store >", () =>
        {
            TransferFromPlayerToChest();
        });

        CreateButton("MinusAmount", sideContentRoot, new Vector2(200f, -504f), new Vector2(46f, 40f), "-", () =>
        {
            transferAmount = Mathf.Max(1, transferAmount - 1);
            Refresh(controller);
        });

        transferAmountText = CreateText("TransferAmount", sideContentRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(252f, -506f), new Vector2(322f, -470f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateButton("PlusAmount", sideContentRoot, new Vector2(330f, -504f), new Vector2(46f, 40f), "+", () =>
        {
            transferAmount = Mathf.Min(GetSelectedTransferLimit(), transferAmount + 1);
            Refresh(controller);
        });

        panel.SetActive(false);
    }

    public void Refresh(PlayerToolController toolController)
    {
        controller = toolController;
        if (controller == null || !IsBuilt)
        {
            return;
        }

        if (currentMode == ViewMode.WorkbenchCrafting && activeChest != null)
        {
            activeChest = null;
        }

        ApplyLayoutForCurrentMode();
        UpdateTitle();
        ApplyCraftingFrameBackground();
        if (currentMode == ViewMode.Inventory || currentMode == ViewMode.Chest)
        {
            BuildInventoryGrid();
        }

        if (currentMode == ViewMode.PlayerCrafting)
        {
            BuildCraftingPanel(CraftingStation.Player);
        }
        else if (currentMode == ViewMode.WorkbenchCrafting)
        {
            BuildCraftingPanel(CraftingStation.Workbench);
        }
        else if (currentMode == ViewMode.FurnaceCrafting)
        {
            BuildCraftingPanel(CraftingStation.Furnace);
        }
        else if (currentMode == ViewMode.ForgeCrafting)
        {
            BuildCraftingPanel(CraftingStation.Forge);
        }
        else if (currentMode == ViewMode.Chest)
        {
            BuildChestPanel();
        }
        else
        {
            BuildInventoryInfo();
        }
    }

    public void Toggle()
    {
        if (!isOpen)
        {
            currentMode = ViewMode.Inventory;
            activeChest = null;
            selectedRecipe = null;
            selectedPlayerTransferIndex = -1;
            selectedChestTransferIndex = -1;
        }

        SetOpen(!isOpen);
    }

    public void OpenWorkbenchCrafting(PlayerToolController toolController)
    {
        controller = toolController;
        activeChest = null;
        currentMode = ViewMode.WorkbenchCrafting;
        SetOpen(true);
    }

    public void OpenStationCrafting(PlayerToolController toolController, CraftingStation station)
    {
        controller = toolController;
        activeChest = null;
        currentMode = station == CraftingStation.Furnace ? ViewMode.FurnaceCrafting : ViewMode.ForgeCrafting;
        SetOpen(true);
    }

    public void OpenChest(PlayerToolController toolController, StorageChest chest)
    {
        controller = toolController;
        activeChest = chest;
        currentMode = ViewMode.Chest;
        selectedPlayerTransferIndex = -1;
        selectedChestTransferIndex = -1;
        transferAmount = 1;
        SetOpen(true);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        IsAnyOpen = open;
        if (panel != null)
        {
            panel.SetActive(isOpen);
        }

        if (isOpen)
        {
            Refresh(controller);
        }

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    private void UpdateTitle()
    {
        if (titleText == null)
        {
            return;
        }

        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(18f, -54f);
        titleRect.offsetMax = new Vector2(-18f, -12f);

        if (currentMode == ViewMode.WorkbenchCrafting)
        {
            titleText.text = "Workbench Crafting";
            return;
        }

        if (currentMode == ViewMode.FurnaceCrafting)
        {
            titleText.text = "Furnace";
            return;
        }

        if (currentMode == ViewMode.ForgeCrafting)
        {
            titleText.text = "Auto Forge";
            return;
        }

        if (currentMode == ViewMode.Chest)
        {
            titleRect.offsetMin = new Vector2(128f, -98f);
            titleRect.offsetMax = new Vector2(-18f, -56f);
            titleText.text = "Wood Chest";
            return;
        }

        if (currentMode == ViewMode.PlayerCrafting)
        {
            titleText.text = "Player Crafting";
            return;
        }

        titleText.text = "Backpack";
    }
    private void ApplyLayoutForCurrentMode()
    {
        bool standaloneWorkbench = currentMode == ViewMode.WorkbenchCrafting
            || currentMode == ViewMode.FurnaceCrafting
            || currentMode == ViewMode.ForgeCrafting;
        bool playerCrafting = currentMode == ViewMode.PlayerCrafting;
        bool chestMode = currentMode == ViewMode.Chest;
        bool craftingOnly = standaloneWorkbench || playerCrafting;

        if (inventoryTabButton != null)
        {
            inventoryTabButton.gameObject.SetActive(!standaloneWorkbench && !chestMode);
        }

        if (craftingTabButton != null)
        {
            craftingTabButton.gameObject.SetActive(!standaloneWorkbench && !chestMode);
        }

        if (inventoryGridRoot != null)
        {
            inventoryGridRoot.gameObject.SetActive(!craftingOnly);
        }

        if (panelRect == null || inventoryGridRect == null || sideContentRect == null)
        {
            return;
        }

        panelRect.anchoredPosition = new Vector2(BackpackPanelRightOffset, 0f);

        if (standaloneWorkbench)
        {
            panelRect.sizeDelta = new Vector2(1600f, 1080f);
            sideContentRect.anchoredPosition = new Vector2(28f, -104f);
            sideContentRect.sizeDelta = new Vector2(1324f, 748f);
        }
        else if (chestMode)
        {
            panelRect.sizeDelta = new Vector2(1680f, 1140f);
            inventoryGridRect.anchoredPosition = new Vector2(148f, -188f);
            inventoryGridRect.sizeDelta = new Vector2(690f, 700f);
            sideContentRect.anchoredPosition = new Vector2(640f, -170f);
            sideContentRect.sizeDelta = new Vector2(720f, 720f);
        }
        else if (playerCrafting)
        {
            panelRect.sizeDelta = new Vector2(1600f, 1080f);
            sideContentRect.anchoredPosition = new Vector2(82f, -178f);
            sideContentRect.sizeDelta = new Vector2(1396f, 800f);
        }
        else
        {
            panelRect.sizeDelta = new Vector2(1600f, 1080f);
            inventoryGridRect.anchoredPosition = new Vector2(102f, -198f);
            inventoryGridRect.sizeDelta = new Vector2(780f, 660f);
            sideContentRect.anchoredPosition = new Vector2(880f, -198f);
            sideContentRect.sizeDelta = new Vector2(560f, 730f);
        }
    }
    private void ApplyCraftingFrameBackground()
    {
        if (panelBackground == null) return;

        Sprite frame = GetFrameSprite();
        if (frame == null) return;

        panelBackground.sprite = frame;
        panelBackground.type = Image.Type.Simple;
        panelBackground.preserveAspect = false;
        panelBackground.color = Color.white;
        panelBackground.enabled = true;
    }

    private void ClearCraftingFrameBackground()
    {
        if (panelBackground == null) return;

        panelBackground.sprite = null;
        panelBackground.type = Image.Type.Simple;
        panelBackground.preserveAspect = false;
        panelBackground.color = panelColor;
        panelBackground.enabled = true;
    }

    private void ApplyFrameSprite(Image image)
    {
        if (image == null) return;

        Sprite frame = GetFrameSprite();
        if (frame == null) return;

        image.sprite = frame;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }

    private void AddSelectionFrame(Transform parent, Color color)
    {
        if (parent == null) return;

        GameObject frameObject = new GameObject("SelectionFrame");
        frameObject.transform.SetParent(parent, false);
        RectTransform rect = frameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-3f, -3f);
        rect.offsetMax = new Vector2(3f, 3f);

        Image image = frameObject.AddComponent<Image>();
        ApplyFrameSprite(image);
        image.color = color;
        image.raycastTarget = false;
    }

    private Sprite GetFrameSprite()
    {
        if (frameSprite != null) return frameSprite;

        Texture2D texture = Resources.Load<Texture2D>("frame");
        if (texture == null) return null;

        frameSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return frameSprite;
    }
    private void BuildInventoryGrid()
    {
        ClearChildren(inventoryGridRoot);
        int previousColumns = columns;
        int previousRows = rows;
        if (controller != null)
        {
            columns = controller.hasBackpack ? controller.backpackColumns : controller.noBackpackColumns;
            rows = controller.hasBackpack ? controller.backpackRows : controller.noBackpackRows;
        }

        int totalCells = columns * rows;
        for (int i = 0; i < totalCells; i++)
        {
            ToolSlot slot = controller.Slots != null && i < controller.Slots.Length ? controller.Slots[i] : null;
            CreateInventoryCell(i, InventoryUtility.IsValidSlot(slot) ? slot : null);
        }

        columns = previousColumns;
        rows = previousRows;
    }

    private void CreateInventoryCell(int cellIndex, ToolSlot slot)
    {
        if (!InventoryUtility.IsValidSlot(slot))
        {
            slot = null;
        }
        int x = cellIndex % columns;
        int y = cellIndex / columns;
        float gap = Mathf.Clamp(8f - Mathf.Max(0, columns - 8) * 0.75f, 3f, 8f);
        float availableWidth = inventoryGridRect != null ? Mathf.Max(200f, inventoryGridRect.rect.width - 24f) : 576f;
        float availableHeight = inventoryGridRect != null ? Mathf.Max(200f, inventoryGridRect.rect.height - 24f) : 596f;
        float cellSize = Mathf.Clamp(
            Mathf.Min((availableWidth - Mathf.Max(0, columns - 1) * gap) / Mathf.Max(1, columns),
                (availableHeight - Mathf.Max(0, rows - 1) * gap) / Mathf.Max(1, rows)),
            28f,
            64f);
        float totalWidth = (columns * cellSize) + ((columns - 1) * gap);
        float totalHeight = (rows * cellSize) + ((rows - 1) * gap);
        float startX = -totalWidth * 0.5f + cellSize * 0.5f;
        float startY = totalHeight * 0.5f - cellSize * 0.5f;

        GameObject cellObject = new GameObject("InventoryCell_" + cellIndex);
        cellObject.transform.SetParent(inventoryGridRoot, false);
        RectTransform rect = cellObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(startX + x * (cellSize + gap), startY - y * (cellSize + gap));

        Image image = cellObject.AddComponent<Image>();
        bool inHotbar = slot != null && controller.IsInventoryIndexInHotbar(cellIndex);
        bool selectedHotbar = slot != null && controller.SelectedInventoryIndex == cellIndex;
        bool selectedTransfer = currentMode == ViewMode.Chest && selectedPlayerTransferIndex == cellIndex;
        image.color = selectedTransfer ? new Color(1f, 0.86f, 0.18f, 1f) : selectedHotbar ? selectedHotbarColor : inHotbar ? hotbarItemColor : slot != null ? itemColor : emptyColor;
        if (selectedTransfer)
        {
            Outline outline = cellObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.95f, 0.25f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            AddSelectionFrame(cellObject.transform, new Color(1f, 0.95f, 0.25f, 1f));
        }

        Button button = cellObject.AddComponent<Button>();
        int capturedIndex = cellIndex;
        button.onClick.AddListener(() =>
        {
            if (currentMode == ViewMode.Chest)
            {
                selectedPlayerTransferIndex = slot != null ? capturedIndex : -1;
                selectedChestTransferIndex = -1;
                ClampTransferAmountToSelection();
                Refresh(controller);
                return;
            }

            if (currentMode == ViewMode.Inventory && IsArmorSlot(slot))
            {
                controller.EquipArmorFromInventoryIndex(capturedIndex);
                Refresh(controller);
                return;
            }

            controller.EquipInventorySlotToSelectedHotbar(slot != null ? capturedIndex : -1);
            Refresh(controller);
        });

        bool hasIcon = slot != null && TryCreateItemIcon(cellObject.transform, slot.prefab != null ? slot.prefab : slot.worldPrefab, new Vector2(7f, 13f), new Vector2(-7f, -7f));
        Text text = CreateCenteredCellText(cellObject.transform, slot != null ? (hasIcon ? (slot.stackCount > 1 ? "x" + slot.stackCount : string.Empty) : slot.displayName + (slot.stackCount > 1 ? "\nx" + slot.stackCount : string.Empty)) : "Empty");
        text.fontSize = hasIcon ? 11 : cellSize < 40f ? 10 : 13;
        text.alignment = hasIcon ? TextAnchor.LowerRight : TextAnchor.MiddleCenter;
    }

    private void BuildInventoryInfo()
    {
        ClearChildren(sideContentRoot);
        Transform previewRoot = CreateSectionRoot("PlayerPreview", sideContentRoot, new Vector2(18f, -18f), new Vector2(300f, 540f));
        Image previewImage = previewRoot.gameObject.AddComponent<Image>();
        previewImage.color = new Color(0.08f, 0.075f, 0.065f, 0.94f);

        Text previewTitle = CreateText("PreviewTitle", previewRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -38f), new Vector2(-12f, -10f), 17, FontStyle.Bold, TextAnchor.MiddleCenter);
        previewTitle.text = "Player";

        CreatePlayerPreview(previewRoot);

        Transform equipmentRoot = CreateSectionRoot("EquipmentSlots", sideContentRoot, new Vector2(336f, -18f), new Vector2(210f, 540f));
        Image equipmentImage = equipmentRoot.gameObject.AddComponent<Image>();
        equipmentImage.color = new Color(0.1f, 0.085f, 0.065f, 0.9f);

        Text equipmentTitle = CreateText("EquipmentTitle", equipmentRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -38f), new Vector2(-12f, -10f), 17, FontStyle.Bold, TextAnchor.MiddleCenter);
        equipmentTitle.text = "Equipment";

        CreateEquipmentSlot(equipmentRoot, "Helmet", controller != null ? controller.EquippedHelmet : null, new Vector2(18f, -72f), () =>
        {
            if (controller != null && controller.UnequipHelmet())
            {
                Refresh(controller);
            }
        });

        CreateEquipmentSlot(equipmentRoot, "Armor", controller != null ? controller.EquippedArmor : null, new Vector2(18f, -212f), () =>
        {
            if (controller != null && controller.UnequipArmor())
            {
                Refresh(controller);
            }
        });

        int defense = controller != null ? controller.GetEquippedDefense() : 0;
        Text stats = CreateText("EquipmentStats", equipmentRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 16f), new Vector2(-14f, 96f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
        stats.text = "Armor: " + defense + "\nBackpack armor gives 0";
    }

    private void CreatePlayerPreview(Transform parent)
    {
        GameObject imageObject = new GameObject("PlayerRender");
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(18f, -500f);
        rect.offsetMax = new Vector2(-18f, -52f);

        playerPreviewImage = imageObject.AddComponent<RawImage>();
        playerPreviewImage.color = Color.white;
        EnsurePlayerPreview();
    }

    private void EnsurePlayerPreview()
    {
        if (playerPreviewTexture == null)
        {
            playerPreviewTexture = new RenderTexture(512, 768, 16, RenderTextureFormat.ARGB32);
            playerPreviewTexture.name = "BackpackPlayerPreviewTexture";
        }

        if (playerPreviewStage == null)
        {
            playerPreviewStage = new GameObject("BackpackPlayerPreviewStage");
            playerPreviewStage.hideFlags = HideFlags.HideAndDontSave;
            playerPreviewStage.transform.position = new Vector3(10000f, 10000f, 10000f);
            SetLayerRecursively(playerPreviewStage, PlayerPreviewLayer);
        }

        SetLayerRecursively(playerPreviewStage, PlayerPreviewLayer);

        if (playerPreviewCamera == null)
        {
            GameObject cameraObject = new GameObject("BackpackPlayerPreviewCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.transform.SetParent(playerPreviewStage.transform, false);
            playerPreviewCamera = cameraObject.AddComponent<Camera>();
            playerPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            playerPreviewCamera.backgroundColor = new Color(0.08f, 0.075f, 0.065f, 0f);
            playerPreviewCamera.nearClipPlane = 0.03f;
            playerPreviewCamera.farClipPlane = 20f;
            playerPreviewCamera.fieldOfView = 26f;
            playerPreviewCamera.targetTexture = playerPreviewTexture;
            playerPreviewCamera.cullingMask = 1 << PlayerPreviewLayer;
            playerPreviewCamera.enabled = false;
        }

        playerPreviewCamera.targetTexture = playerPreviewTexture;
        playerPreviewCamera.cullingMask = 1 << PlayerPreviewLayer;

        if (playerPreviewImage != null)
        {
            playerPreviewImage.texture = playerPreviewTexture;
        }

        RebuildPlayerPreviewModel();
    }

    private void RebuildPlayerPreviewModel()
    {
        if (playerPreviewStage == null || playerPreviewCamera == null || controller == null)
        {
            return;
        }

        if (playerPreviewModel != null)
        {
            playerPreviewModel.SetActive(false);
            DestroyChild(playerPreviewModel);
            playerPreviewModel = null;
        }

        ClearPreviewModels();

        Transform sourceVisual = controller.GetVisualRoot();
        if (sourceVisual == null)
        {
            return;
        }

        playerPreviewModel = Instantiate(sourceVisual.gameObject, playerPreviewStage.transform);
        playerPreviewModel.name = "BackpackPlayerPreviewModel";
        playerPreviewModel.transform.localPosition = Vector3.zero;
        playerPreviewModel.transform.localRotation = Quaternion.identity;
        playerPreviewModel.transform.localScale = Vector3.one;
        SetLayerRecursively(playerPreviewModel, PlayerPreviewLayer);

        StripPreviewBehaviours(playerPreviewModel);
        RemovePreviewHeldObjects(playerPreviewModel);
        HidePreviewNonBodyProps(playerPreviewModel);
        FitPreviewCameraToModel(playerPreviewModel);
        playerPreviewCamera.Render();
    }

    private void ClearPreviewModels()
    {
        if (playerPreviewStage == null)
        {
            return;
        }

        for (int i = playerPreviewStage.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = playerPreviewStage.transform.GetChild(i);
            if (child == null || (playerPreviewCamera != null && child == playerPreviewCamera.transform))
            {
                continue;
            }

            child.gameObject.SetActive(false);
            DestroyChild(child.gameObject);
        }
    }

    private void StripPreviewBehaviours(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            DestroyUnityObject(colliders[i]);
        }

        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            DestroyUnityObject(bodies[i]);
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            DestroyUnityObject(lights[i]);
        }

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            DestroyUnityObject(particles[i].gameObject);
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].applyRootMotion = false;
                animators[i].Rebind();
                animators[i].Update(0f);
                animators[i].enabled = false;
            }
        }

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
            {
                behaviours[i].enabled = false;
            }
        }
    }

    private void RemovePreviewHeldObjects(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = children.Length - 1; i >= 0; i--)
        {
            Transform child = children[i];
            if (child == null || child == root.transform)
            {
                continue;
            }

            string lowerName = child.name.ToLowerInvariant();
            if (IsPreviewHeldObjectName(lowerName))
            {
                HideRenderersImmediately(child.gameObject);
                child.gameObject.SetActive(false);
                DestroyChild(child.gameObject);
            }
        }
    }

    private static bool IsPreviewHeldObjectName(string lowerName)
    {
        if (string.IsNullOrEmpty(lowerName))
        {
            return false;
        }

        if (IsPreviewWearableOrBodyName(lowerName))
        {
            return false;
        }

        return lowerName.Contains("held")
            || lowerName.Contains("handanchor")
            || lowerName.Contains("runtime")
            || lowerName.Contains("pickup")
            || lowerName.Contains("wood")
            || lowerName.Contains("stone")
            || lowerName.Contains("bar")
            || lowerName.Contains("axe")
            || lowerName.Contains("pickaxe")
            || lowerName.Contains("torch")
            || lowerName.Contains("tool");
    }

    private void HidePreviewNonBodyProps(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is SkinnedMeshRenderer)
            {
                continue;
            }

            string hierarchyName = GetHierarchyName(renderer.transform).ToLowerInvariant();
            if (IsPreviewWearableOrBodyName(hierarchyName))
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private static void HideRenderersImmediately(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private static bool IsPreviewWearableOrBodyName(string lowerName)
    {
        return lowerName.Contains("helmet")
            || lowerName.Contains("armor")
            || lowerName.Contains("body")
            || lowerName.Contains("shirt")
            || lowerName.Contains("pants")
            || lowerName.Contains("cloth")
            || lowerName.Contains("hair")
            || lowerName.Contains("head")
            || lowerName.Contains("face")
            || lowerName.Contains("eye")
            || lowerName.Contains("kate")
            || lowerName.Contains("character");
    }

    private static string GetHierarchyName(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        string name = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            name += "/" + current.name;
            current = current.parent;
        }

        return name;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
        {
            return;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
            {
                transforms[i].gameObject.layer = layer;
            }
        }
    }

    private void FitPreviewCameraToModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = default;
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled || renderers[i] is ParticleSystemRenderer)
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

        Vector3 center = bounds.center;
        playerPreviewCamera.transform.position = center + new Vector3(0f, 0.05f, 4.2f);
        playerPreviewCamera.transform.LookAt(center + new Vector3(0f, 0.18f, 0f));
    }

    private void DestroyPlayerPreview()
    {
        if (playerPreviewTexture != null)
        {
            playerPreviewTexture.Release();
            DestroyUnityObject(playerPreviewTexture);
            playerPreviewTexture = null;
        }

        if (playerPreviewStage != null)
        {
            DestroyChild(playerPreviewStage);
            playerPreviewStage = null;
            playerPreviewCamera = null;
            playerPreviewModel = null;
        }
    }
    private void CreateEquipmentSlot(Transform parent, string label, ToolSlot slot, Vector2 anchoredPosition, UnityEngine.Events.UnityAction unequipAction)
    {
        GameObject slotObject = new GameObject(label + "Slot");
        slotObject.transform.SetParent(parent, false);
        RectTransform rect = slotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(174f, 112f);
        Image image = slotObject.AddComponent<Image>();
        image.color = slot != null ? itemColor : emptyColor;

        bool hasIcon = slot != null && TryCreateItemIcon(slotObject.transform, slot.prefab != null ? slot.prefab : slot.worldPrefab, new Vector2(10f, 20f), new Vector2(-10f, -8f));
        Text text = CreateCenteredCellText(slotObject.transform, hasIcon ? label : label + "\n" + (slot != null ? slot.displayName : "Empty"));
        text.fontSize = hasIcon ? 12 : 16;
        text.alignment = hasIcon ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;

        Button button = slotObject.AddComponent<Button>();
        button.onClick.AddListener(unequipAction);
        button.interactable = slot != null;
    }

    private static bool IsArmorSlot(ToolSlot slot)
    {
        return slot != null && slot.category == ToolCategory.Armor;
    }

    private void BuildCraftingPanel(CraftingStation station)
    {
        ClearChildren(sideContentRoot);
        ApplyCraftingFrameBackground();
        CraftingCatalog catalog = controller != null ? controller.CraftingCatalog : null;
        if (catalog == null)
        {
            detailText = CreateText("DetailText", sideContentRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -16f), new Vector2(-16f, -16f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            detailText.text = "Crafting catalog is missing.";
            return;
        }

        bool standaloneWorkbench = station != CraftingStation.Player;
        Transform recipeListRoot = CreateSectionRoot(
            "RecipeList",
            sideContentRoot,
            standaloneWorkbench ? (station == CraftingStation.Forge ? new Vector2(CraftingPanelRightOffset + 35f, -84f) : new Vector2(CraftingPanelRightOffset + 35f, -114f)) : new Vector2(180f, -116f),
            standaloneWorkbench ? new Vector2(760f, 720f) : new Vector2(330f, 700f));
        Transform recipeDetailRoot = CreateSectionRoot(
            "RecipeDetail",
            sideContentRoot,
            standaloneWorkbench ? new Vector2(795f + CraftingPanelRightOffset, -112f) : new Vector2(540f, -116f),
            standaloneWorkbench ? new Vector2(654f, 672f) : new Vector2(640f, 650f));
        List<CraftingRecipe> recipes = catalog.GetRecipes(station);
        if (selectedRecipe != null && GetRecipeStation(selectedRecipe) != station)
        {
            selectedRecipe = null;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipe recipe = recipes[i];
            Vector2 recipeButtonSize = GetRecipeButtonSize(station);
            Vector2 recipeButtonPosition = GetRecipeButtonPosition(i, standaloneWorkbench, recipeButtonSize);
            Button recipeButton = CreateButton("Recipe_" + i, recipeListRoot, recipeButtonPosition, recipeButtonSize, recipe.displayName, () =>
            {
                selectedRecipe = recipe;
                Refresh(controller);
            });

            Image recipeImage = recipeButton.GetComponent<Image>();
            ApplyFrameSprite(recipeImage);
            recipeImage.color = selectedRecipe == recipe ? selectedHotbarColor : new Color(1f, 1f, 1f, 0.9f);
            CraftingItemDefinition outputDefinition = catalog.FindItem(recipe.outputItemId);
            if (outputDefinition != null && TryCreateItemIcon(recipeButton.transform, ResolveCraftingIconPrefab(outputDefinition, catalog), new Vector2(8f, 8f), new Vector2(-154f, -8f)))
            {
                SetButtonLabel(recipeButton, recipe.outputCount > 1 ? "x" + recipe.outputCount : string.Empty, TextAnchor.MiddleRight, 13);
            }
        }

        detailText = CreateText(
            "RecipeDetailText",
            recipeDetailRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(12f, -12f),
            standaloneWorkbench ? new Vector2(-12f, -246f) : new Vector2(-12f, -220f),
            15,
            FontStyle.Normal,
            TextAnchor.UpperLeft);
        if (selectedRecipe == null && recipes.Count > 0)
        {
            selectedRecipe = recipes[0];
        }

        if (selectedRecipe == null)
        {
            detailText.text = "No recipes available.";
            return;
        }

        BuildSelectedRecipePreview(recipeDetailRoot, selectedRecipe, catalog, station);
        string recipeText = selectedRecipe.displayName + "\n\n" + selectedRecipe.description + "\n\nNeed:\n";
        for (int i = 0; i < selectedRecipe.ingredients.Length; i++)
        {
            RecipeIngredient ingredient = selectedRecipe.ingredients[i];
            int owned = controller.GetItemCount(ingredient.itemId);
            recipeText += "- " + ingredient.itemId.Replace("_", " ") + ": " + ingredient.amount + " (You have " + owned + ")\n";
        }

        recipeText += "\nOutput: " + selectedRecipe.outputCount;
        detailText.text = recipeText;
        BuildRecipeFormula(recipeDetailRoot, selectedRecipe, catalog, standaloneWorkbench);
        craftButton = CreateButton("CraftButton", recipeDetailRoot, standaloneWorkbench ? new Vector2(12f, -470f) : new Vector2(130f, -510f), new Vector2(170f, 50f), controller.CanCraft(selectedRecipe) ? "Craft" : "Missing Items", () =>
        {
            if (controller.TryCraft(selectedRecipe))
            {
                Refresh(controller);
            }
        });
        Image craftButtonImage = craftButton.GetComponent<Image>();
        ApplyFrameSprite(craftButtonImage);
        craftButtonImage.color = controller.CanCraft(selectedRecipe) ? new Color(1f, 1f, 1f, 0.92f) : new Color(1f, 1f, 1f, 0.74f);
        craftButton.interactable = controller.CanCraft(selectedRecipe);
    }

    private void BuildSelectedRecipePreview(Transform parent, CraftingRecipe recipe, CraftingCatalog catalog, CraftingStation station)
    {
        if (parent == null || recipe == null || catalog == null) return;

        CraftingItemDefinition outputDefinition = catalog.FindItem(recipe.outputItemId);
        GameObject prefab = ResolveCraftingIconPrefab(outputDefinition, catalog);
        bool standaloneWorkbench = station != CraftingStation.Player;
        Vector2 position = GetSelectedRecipePreviewPosition(station);
        Vector2 size = GetSelectedRecipePreviewSize(station);
        Transform previewRoot = CreateSectionRoot("SelectedRecipePreview", parent, position, size);
        Image image = previewRoot.gameObject.AddComponent<Image>();
        ApplyFrameSprite(image);
        image.color = new Color(1f, 1f, 1f, 0.9f);

        bool hasIcon = TryCreateItemIcon(previewRoot, prefab, new Vector2(24f, 34f), new Vector2(-24f, -26f));
        Text label = CreateCenteredCellText(previewRoot, outputDefinition != null ? outputDefinition.displayName : FormatItemName(recipe.outputItemId));
        label.fontSize = standaloneWorkbench ? 16 : 14;
        label.alignment = hasIcon ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter;
    }

    private static Vector2 GetRecipeButtonSize(CraftingStation station)
    {
        if (station == CraftingStation.Player) return new Vector2(260f, 80f);
        if (station == CraftingStation.Furnace) return new Vector2(250f, 80f);
        return new Vector2(230f, 62f);
    }

    private static Vector2 GetSelectedRecipePreviewPosition(CraftingStation station)
    {
        if (station == CraftingStation.Player) return new Vector2(200f, -28f);
        if (station == CraftingStation.Furnace) return new Vector2(176f, -36f);
        if (station == CraftingStation.Forge) return new Vector2(170f, -34f);
        return new Vector2(190f, -40f);
    }

    private static Vector2 GetSelectedRecipePreviewSize(CraftingStation station)
    {
        if (station == CraftingStation.Player) return new Vector2(220f, 220f);
        if (station == CraftingStation.Furnace) return new Vector2(250f, 250f);
        if (station == CraftingStation.Forge) return new Vector2(250f, 250f);
        return new Vector2(210f, 210f);
    }
    private static Vector2 GetRecipeButtonPosition(int index, bool standaloneWorkbench, Vector2 buttonSize)
    {
        if (!standaloneWorkbench)
        {
            return new Vector2(18f, -18f - index * (buttonSize.y + 12f));
        }

        const int recipeColumns = 3;
        float gapX = 14f;
        float gapY = 12f;
        int column = index % recipeColumns;
        int row = index / recipeColumns;
        return new Vector2(18f + column * (buttonSize.x + gapX), -18f - row * (buttonSize.y + gapY));
    }

    private void BuildRecipeFormula(Transform parent, CraftingRecipe recipe, CraftingCatalog catalog, bool standaloneWorkbench)
    {
        Transform formulaRoot = CreateSectionRoot(
            "RecipeFormula",
            parent,
            standaloneWorkbench ? new Vector2(72f, -360f) : new Vector2(120f, -340f),
            standaloneWorkbench ? new Vector2(620f, 170f) : new Vector2(500f, 170f));
        int ingredientCount = recipe.ingredients != null ? recipe.ingredients.Length : 0;
        float x = 6f;
        float y = -18f;
        float maxFormulaWidth = standaloneWorkbench ? 596f : 476f;
        for (int i = 0; i < ingredientCount; i++)
        {
            RecipeIngredient ingredient = recipe.ingredients[i];
            CraftingItemDefinition ingredientDefinition = catalog.FindItem(ingredient.itemId);
            string itemLabel = FormatItemName(ingredient.itemId) + "\nx" + ingredient.amount;
            if (x + 66f > maxFormulaWidth)
            {
                x = 6f;
                y -= 66f;
            }

            CreateFormulaCell(formulaRoot, "Ingredient_" + i, new Vector2(x, y), itemLabel, false, ingredientDefinition, ingredient.amount, catalog);
            x += 72f;
            if (i < ingredientCount - 1)
            {
                if (x + 34f > maxFormulaWidth)
                {
                    x = 6f;
                    y -= 66f;
                }

                CreateFormulaCell(formulaRoot, "Plus_" + i, new Vector2(x, y), "+", true, null, 0, catalog);
                x += 40f;
            }
        }

        if (x + 106f > maxFormulaWidth)
        {
            x = 6f;
            y -= 66f;
        }

        CreateFormulaCell(formulaRoot, "FormulaSeparator", new Vector2(x, y), "=>", true, null, 0, catalog);
        x += 48f;
        CraftingItemDefinition outputDefinition = catalog.FindItem(recipe.outputItemId);
        string outputLabel = (outputDefinition != null ? outputDefinition.displayName : FormatItemName(recipe.outputItemId)) + "\nx" + recipe.outputCount;
        CreateFormulaCell(formulaRoot, "Output", new Vector2(x, y), outputLabel, false, outputDefinition, recipe.outputCount, catalog);
    }

    private void CreateFormulaCell(Transform parent, string name, Vector2 anchoredPosition, string label, bool compact, CraftingItemDefinition definition, int amount, CraftingCatalog catalog)
    {
        GameObject cellObject = new GameObject(name);
        cellObject.transform.SetParent(parent, false);
        RectTransform rect = cellObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = compact ? new Vector2(34f, 54f) : new Vector2(66f, 66f);

        Image image = cellObject.AddComponent<Image>();
        ApplyFrameSprite(image);
        image.color = compact ? new Color(1f, 1f, 1f, 0.86f) : new Color(1f, 1f, 1f, 0.92f);

        GameObject prefab = ResolveCraftingIconPrefab(definition, catalog);
        bool hasIcon = !compact && TryCreateItemIcon(cellObject.transform, prefab, new Vector2(7f, 15f), new Vector2(-7f, -7f));
        Text text = CreateCenteredCellText(cellObject.transform, hasIcon ? (amount > 1 ? "x" + amount : string.Empty) : label);
        text.fontSize = compact ? 16 : hasIcon ? 11 : 13;
        text.alignment = hasIcon ? TextAnchor.LowerRight : TextAnchor.MiddleCenter;
    }

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
    private GameObject ResolveCraftingIconPrefab(CraftingItemDefinition definition, CraftingCatalog catalog)
    {
        if (definition == null) return null;

        GameObject inventoryPrefab = ResolveInventoryIconPrefab(definition.itemId);
        if (inventoryPrefab != null) return inventoryPrefab;
        if (definition.heldPrefab != null) return definition.heldPrefab;
        if (definition.worldPrefab != null) return definition.worldPrefab;

        if (catalog != null)
        {
            CraftingItemDefinition catalogDefinition = catalog.FindItem(definition.itemId);
            if (catalogDefinition != null && catalogDefinition != definition)
            {
                if (catalogDefinition.heldPrefab != null) return catalogDefinition.heldPrefab;
                if (catalogDefinition.worldPrefab != null) return catalogDefinition.worldPrefab;
            }
        }

        switch (definition.itemId)
        {
            case "auto_forge":
                return Resources.Load<GameObject>("AutoForge/Forge");
            case "chest":
                return LoadPrefabAsset("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Treasure_Chest_04_Blue.prefab", "RuntimePrefabs/PP_Treasure_Chest_01_Blue");
            case "door":
                return LoadPrefabAsset("Assets/Free Wood Door Pack/Prefab/Wood/Door_1/Door_1_Brown.prefab", "RuntimePrefabs/Door_1_Brown");
            case "stone":
                return Resources.Load<GameObject>("RuntimePrefabs/Held_LowPolyStone");
            case "stick":
                return Resources.Load<GameObject>("RuntimePrefabs/Held_Stick");
            case "iron_bar":
                return Resources.Load<GameObject>("RuntimePrefabs/Held_IronBar");
            case "forest_guide":
                return Resources.Load<GameObject>("RuntimePrefabs/Held_ForestGuide");
            case "forest_heart":
                return Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeart");
            case "forest_heart_detector":
                return Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector");
            default:
                return null;
        }
    }

    private GameObject ResolveInventoryIconPrefab(string itemId)
    {
        if (controller == null || controller.Slots == null || string.IsNullOrEmpty(itemId)) return null;

        ToolSlot[] slots = controller.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (slot == null || InventoryUtility.GetItemId(slot) != itemId) continue;
            if (slot.prefab != null) return slot.prefab;
            if (slot.worldPrefab != null) return slot.worldPrefab;
        }

        return null;
    }
    private static string FormatItemName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return "Unknown";
        }

        string[] parts = itemId.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
        }

        return string.Join(" ", parts);
    }

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

    private void BuildChestPanel()
    {
        ApplyCraftingFrameBackground();
        ClearChildren(sideContentRoot);
        if (activeChest == null)
        {
            detailText = CreateText("ChestMissing", sideContentRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f), new Vector2(-24f, -64f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            detailText.text = "Chest is missing.";
            return;
        }

        detailText = CreateText("ChestLabel", sideContentRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(42f, -20f), new Vector2(-24f, -52f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
        detailText.text = "Chest Storage";

        Transform chestGridRoot = CreateSectionRoot("ChestGrid", sideContentRoot, new Vector2(210f, -88f), new Vector2(610f, 560f));
        int totalCells = activeChest.SlotCount;
        for (int i = 0; i < totalCells; i++)
        {
            ToolSlot slot = activeChest.storedSlots != null && i < activeChest.storedSlots.Length ? activeChest.storedSlots[i] : null;
            CreateChestCell(chestGridRoot, i, slot);
        }

        ClampTransferAmountToSelection();
        transferAmountText = CreateText("TransferAmount", sideContentRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(400f, -682f), new Vector2(462f, -642f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        transferAmountText.text = transferAmount.ToString();
        Button minusButton = CreateButton("MinusAmount", sideContentRoot, new Vector2(350f, -642f), new Vector2(44f, 40f), "-", () =>
        {
            transferAmount = Mathf.Max(1, transferAmount - 1);
            Refresh(controller);
        });
        Button plusButton = CreateButton("PlusAmount", sideContentRoot, new Vector2(468f, -642f), new Vector2(44f, 40f), "+", () =>
        {
            transferAmount = Mathf.Min(GetSelectedTransferLimit(), transferAmount + 1);
            Refresh(controller);
        });
        ApplyFrameSprite(minusButton.GetComponent<Image>());
        ApplyFrameSprite(plusButton.GetComponent<Image>());

        transferToPlayerButton = CreateButton("TakeButton", sideContentRoot, new Vector2(294f, -676f), new Vector2(150f, 48f), "< Take", TransferFromChestToPlayer);
        transferToChestButton = CreateButton("StoreButton", sideContentRoot, new Vector2(464f, -676f), new Vector2(150f, 48f), "Store >", TransferFromPlayerToChest);
        ApplyFrameSprite(transferToPlayerButton.GetComponent<Image>());
        ApplyFrameSprite(transferToChestButton.GetComponent<Image>());
    }
    private void CreateChestCell(Transform root, int cellIndex, ToolSlot slot)
    {
        const int chestColumns = 8;
        int x = cellIndex % chestColumns;
        int y = cellIndex / chestColumns;
        float cellSize = 64f;
        float gap = 7f;
        float totalWidth = (chestColumns * cellSize) + ((chestColumns - 1) * gap);
        float startX = -totalWidth * 0.5f + cellSize * 0.5f;
        float startY = 252f;

        GameObject cellObject = new GameObject("ChestCell_" + cellIndex);
        cellObject.transform.SetParent(root, false);
        RectTransform rect = cellObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(startX + x * (cellSize + gap), startY - y * (cellSize + gap));

        Image image = cellObject.AddComponent<Image>();
        ApplyFrameSprite(image);
        image.color = selectedChestTransferIndex == cellIndex ? new Color(0.45f, 0.72f, 1f, 1f) : slot != null ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.58f);
        if (selectedChestTransferIndex == cellIndex)
        {
            Outline outline = cellObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.86f, 1f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            AddSelectionFrame(cellObject.transform, new Color(0.55f, 0.86f, 1f, 1f));
        }

        Button button = cellObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            selectedChestTransferIndex = slot != null ? cellIndex : -1;
            selectedPlayerTransferIndex = -1;
            ClampTransferAmountToSelection();
            Refresh(controller);
        });

        bool hasIcon = slot != null && TryCreateItemIcon(cellObject.transform, slot.prefab != null ? slot.prefab : slot.worldPrefab, new Vector2(7f, 13f), new Vector2(-7f, -7f));
        Text text = CreateCenteredCellText(cellObject.transform, slot != null ? (hasIcon ? (slot.stackCount > 1 ? "x" + slot.stackCount : string.Empty) : slot.displayName + (slot.stackCount > 1 ? "\nx" + slot.stackCount : string.Empty)) : "Empty");
        text.fontSize = hasIcon ? 11 : 13;
        text.alignment = hasIcon ? TextAnchor.LowerRight : TextAnchor.MiddleCenter;
    }

    private void ClampTransferAmountToSelection()
    {
        transferAmount = Mathf.Clamp(transferAmount, 1, GetSelectedTransferLimit());
    }

    private int GetSelectedTransferLimit()
    {
        ToolSlot selectedSlot = GetSelectedTransferSlot();
        return selectedSlot != null ? Mathf.Max(1, selectedSlot.stackCount) : 1;
    }

    private ToolSlot GetSelectedTransferSlot()
    {
        if (currentMode != ViewMode.Chest)
        {
            return null;
        }

        if (selectedPlayerTransferIndex >= 0 && controller != null && controller.Slots != null && selectedPlayerTransferIndex < controller.Slots.Length)
        {
            ToolSlot slot = controller.Slots[selectedPlayerTransferIndex];
            return InventoryUtility.IsValidSlot(slot) ? slot : null;
        }

        if (selectedChestTransferIndex >= 0 && activeChest != null && activeChest.storedSlots != null && selectedChestTransferIndex < activeChest.storedSlots.Length)
        {
            ToolSlot slot = activeChest.storedSlots[selectedChestTransferIndex];
            return InventoryUtility.IsValidSlot(slot) ? slot : null;
        }

        return null;
    }
    private void TransferFromPlayerToChest()
    {
        if (controller == null || activeChest == null || selectedPlayerTransferIndex < 0 || controller.Slots == null || selectedPlayerTransferIndex >= controller.Slots.Length)
        {
            return;
        }

        ToolSlot slot = controller.Slots[selectedPlayerTransferIndex];
        if (slot == null)
        {
            return;
        }

        int amountToMove = Mathf.Clamp(transferAmount, 1, slot.stackCount);
        if (!activeChest.AddItem(slot, amountToMove))
        {
            return;
        }

        if (!controller.RemoveInventoryItemAtIndex(selectedPlayerTransferIndex, amountToMove))
        {
            activeChest.RemoveItem(InventoryUtility.GetItemId(slot), amountToMove);
            return;
        }

        transferAmount = 1;
        selectedPlayerTransferIndex = -1;
        Refresh(controller);
    }

    private void TransferFromChestToPlayer()
    {
        if (controller == null || activeChest == null || selectedChestTransferIndex < 0 || activeChest.storedSlots == null || selectedChestTransferIndex >= activeChest.storedSlots.Length)
        {
            return;
        }

        ToolSlot slot = activeChest.storedSlots[selectedChestTransferIndex];
        if (slot == null)
        {
            return;
        }

        int amountToMove = Mathf.Clamp(transferAmount, 1, slot.stackCount);
        if (!controller.TryAddOrMergeInventorySlot(InventoryUtility.CloneSlot(slot, amountToMove)))
        {
            return;
        }

        activeChest.RemoveItem(InventoryUtility.GetItemId(slot), amountToMove);
        transferAmount = 1;
        selectedChestTransferIndex = -1;
        Refresh(controller);
    }

    private Transform CreateSectionRoot(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject rootObject = new GameObject(name);
        rootObject.transform.SetParent(parent, false);
        RectTransform rect = rootObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rootObject.transform;
    }

    private bool TryCreateItemIcon(Transform parent, GameObject prefab, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (parent == null || prefab == null)
        {
            return false;
        }

        Texture2D icon = GetItemIcon(prefab);
        if (icon == null)
        {
            return false;
        }

        GameObject iconObject = new GameObject("ItemIcon");
        iconObject.transform.SetParent(parent, false);
        RectTransform rect = iconObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        RawImage image = iconObject.AddComponent<RawImage>();
        image.texture = icon;
        image.color = Color.white;
        image.raycastTarget = false;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        return true;
    }

    private Texture2D GetItemIcon(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        string key = prefab.GetInstanceID().ToString();
        if (itemIconCache.TryGetValue(key, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D rendered = RenderItemIcon(prefab);
        if (rendered != null)
        {
            itemIconCache[key] = rendered;
        }

        return rendered;
    }

    private Texture2D RenderItemIcon(GameObject prefab)
    {
        EnsureItemIconRenderer();
        if (itemIconCamera == null || itemIconStage == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, itemIconStage.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        StripPreviewBehaviours(instance);

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            instance.SetActive(false);
            DestroyChild(instance);
            return null;
        }

        Bounds bounds = default;
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
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

        if (!found)
        {
            instance.SetActive(false);
            DestroyChild(instance);
            return null;
        }

        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.1f);
        itemIconCamera.transform.position = bounds.center + new Vector3(0f, size * 0.12f, -size * 2.6f);
        itemIconCamera.transform.LookAt(bounds.center);
        itemIconCamera.orthographicSize = size * 0.72f;

        RenderTexture renderTexture = RenderTexture.GetTemporary(192, 192, 16, RenderTextureFormat.ARGB32);
        itemIconCamera.targetTexture = renderTexture;
        itemIconCamera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(192, 192, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0f, 0f, 192f, 192f), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;
        itemIconCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        instance.SetActive(false);
        DestroyChild(instance);
        return texture;
    }

    private void EnsureItemIconRenderer()
    {
        if (itemIconStage == null)
        {
            itemIconStage = new GameObject("BackpackItemIconStage");
            itemIconStage.hideFlags = HideFlags.HideAndDontSave;
            itemIconStage.transform.position = new Vector3(12000f, 12000f, 12000f);
        }

        if (itemIconCamera != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("BackpackItemIconCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(itemIconStage.transform, false);
        itemIconCamera = cameraObject.AddComponent<Camera>();
        itemIconCamera.clearFlags = CameraClearFlags.SolidColor;
        itemIconCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        itemIconCamera.orthographic = true;
        itemIconCamera.nearClipPlane = 0.01f;
        itemIconCamera.farClipPlane = 100f;
        itemIconCamera.enabled = false;

        Light light = new GameObject("BackpackItemIconLight").AddComponent<Light>();
        light.transform.SetParent(itemIconStage.transform, false);
        light.transform.localPosition = new Vector3(0f, 2f, -2.5f);
        light.type = LightType.Point;
        light.range = 7f;
        light.intensity = 2.4f;
    }

    private void DestroyItemIcons()
    {
        foreach (KeyValuePair<string, Texture2D> pair in itemIconCache)
        {
            if (pair.Value != null)
            {
                DestroyUnityObject(pair.Value);
            }
        }

        itemIconCache.Clear();
        if (itemIconStage != null)
        {
            DestroyUnityObject(itemIconStage);
            itemIconStage = null;
            itemIconCamera = null;
        }
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.18f, 0.12f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        Text text = CreateCenteredCellText(buttonObject.transform, label);
        text.fontSize = 17;
        return button;
    }

    private static void SetButtonLabel(Button button, string label, TextAnchor alignment, int fontSize)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>(true);
        if (text == null)
        {
            return;
        }

        text.text = label;
        text.alignment = alignment;
        text.fontSize = fontSize;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Text CreateCenteredCellText(Transform parent, string content)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 13;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = content;
        return text;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            DestroyChild(root.GetChild(i).gameObject);
        }
    }

    private static void DestroyChild(GameObject child)
    {
        DestroyUnityObject(child);
    }

    private static void DestroyUnityObject(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
























































