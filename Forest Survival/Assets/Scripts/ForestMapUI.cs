using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Map UI controller that renders explored areas, player position, transfer points, and heart markers.
public class ForestMapUI : MonoBehaviour
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: DetectorItemId.
    private const string DetectorItemId = "forest_heart_detector";
    // Important runtime data or configuration used by this component: MapPixels.
    private const int MapPixels = 256;
    private static readonly Vector3 ForestHeartFallbackPosition = new Vector3(191.2f, -2.52f, -179.22f);

    // Important runtime data or configuration used by this component: worldSize.
    public float worldSize = 1300f;
    // Distance or radius used for detection, interaction, or physics checks: exploreRadius.
    public float exploreRadius = 42f;
    // Input setting or cached input value read from player controls: toggleMapKey.
    public KeyCode toggleMapKey = KeyCode.M;

    // Layer or mask filter used by physics queries or rendering: player.
    private PlayerToolController player;
    // Current interaction target or gameplay object being processed: heartTarget.
    private Transform heartTarget;
    // Spatial value used for positioning, rotation, scale, or collision math: transferPoint.
    private Transform transferPoint;
    // Cached component or scene reference to avoid repeated lookups: canvas.
    private Canvas canvas;
    // Cached component or scene reference to avoid repeated lookups: mapImage.
    private RawImage mapImage;
    // Cached component or scene reference to avoid repeated lookups: overlayImage.
    private RawImage overlayImage;
    // Cached component or scene reference to avoid repeated lookups: mapRect.
    private RectTransform mapRect;
    // Cached component or scene reference to avoid repeated lookups: mapImageRect.
    private RectTransform mapImageRect;
    // Cached component or scene reference to avoid repeated lookups: overlayRect.
    private RectTransform overlayRect;
    // Cached component or scene reference to avoid repeated lookups: heartIconRect.
    private RectTransform heartIconRect;
    // Cached component or scene reference to avoid repeated lookups: hintText.
    private Text hintText;
    // Asset reference used for spawning, rendering, audio, or animation: mapTexture.
    private Texture2D mapTexture;
    // Asset reference used for spawning, rendering, audio, or animation: terrainRenderTexture.
    private RenderTexture terrainRenderTexture;
    // Cached component or scene reference to avoid repeated lookups: mapCamera.
    private Camera mapCamera;
    // Important runtime data or configuration used by this component: pixels.
    private Color32[] pixels;
    // Important runtime data or configuration used by this component: explored.
    private bool[] explored;
    // Important runtime data or configuration used by this component: largeMap.
    private bool largeMap;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: nextMapRenderTime.
    private float nextMapRenderTime;

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        player = FindObjectOfType<PlayerToolController>();
        BuildUI();
        CreateMapTexture();
        RefreshMap(true);
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerToolController>();
        }

        if (heartTarget == null)
        {
            heartTarget = FindForestHeart();
        }

        if (transferPoint == null)
        {
            transferPoint = FindAlpineTransferPoint();
        }

        if (Input.GetKeyDown(toggleMapKey))
        {
            largeMap = !largeMap;
            ApplyMapSize();
        }

        ExploreAroundPlayer();
        RenderTerrainMapIfNeeded();
        RefreshMap(false);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for build ui.
    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("ForestMapCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 118;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject frame = new GameObject("MapFrame");
        frame.transform.SetParent(canvasObject.transform, false);
        mapRect = frame.AddComponent<RectTransform>();
        Image frameImage = frame.AddComponent<Image>();
        frameImage.sprite = CreateResourceSprite("frame");
        frameImage.color = Color.white;

        GameObject mapObject = new GameObject("MapImage");
        mapObject.transform.SetParent(frame.transform, false);
        mapImageRect = mapObject.AddComponent<RectTransform>();
        mapImageRect.anchorMin = Vector2.zero;
        mapImageRect.anchorMax = Vector2.one;
        mapImage = mapObject.AddComponent<RawImage>();
        mapImage.color = Color.white;

        GameObject overlayObject = new GameObject("MapOverlay");
        overlayObject.transform.SetParent(frame.transform, false);
        overlayRect = overlayObject.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayImage = overlayObject.AddComponent<RawImage>();
        overlayImage.color = Color.white;

        GameObject heartIconObject = new GameObject("ForestHeartMapMarker");
        heartIconObject.transform.SetParent(frame.transform, false);
        heartIconRect = heartIconObject.AddComponent<RectTransform>();
        heartIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        heartIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        heartIconRect.pivot = new Vector2(0.5f, 0.5f);
        heartIconRect.sizeDelta = new Vector2(16f, 16f);
        Image heartIconImage = heartIconObject.AddComponent<Image>();
        heartIconImage.sprite = CreateCircleSprite();
        heartIconImage.color = new Color(0.56f, 0.9f, 0.58f, 0.78f);
        heartIconImage.raycastTarget = false;

        GameObject hintObject = new GameObject("MapHint");
        hintObject.transform.SetParent(frame.transform, false);
        RectTransform hintRect = hintObject.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -6f);
        hintRect.sizeDelta = new Vector2(0f, 48f);
        Image hintFrame = hintObject.AddComponent<Image>();
        hintFrame.sprite = CreateResourceSprite("frame");
        hintFrame.color = Color.white;
        hintFrame.raycastTarget = false;

        GameObject hintLabelObject = new GameObject("MapHintLabel");
        hintLabelObject.transform.SetParent(hintObject.transform, false);
        RectTransform hintLabelRect = hintLabelObject.AddComponent<RectTransform>();
        hintLabelRect.anchorMin = Vector2.zero;
        hintLabelRect.anchorMax = Vector2.one;
        hintLabelRect.offsetMin = new Vector2(20f, 6f);
        hintLabelRect.offsetMax = new Vector2(-20f, -6f);
        hintText = hintLabelObject.AddComponent<Text>();
        hintText.font = GetBuiltinUIFont();
        hintText.text = "M  MAP";
        hintText.fontSize = 20;
        hintText.fontStyle = FontStyle.Bold;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(0.78f, 1f, 0.64f, 0.95f);

        ApplyMapSize();
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create map texture.
    private void CreateMapTexture()
    {
        terrainRenderTexture = new RenderTexture(MapPixels, MapPixels, 16, RenderTextureFormat.ARGB32);
        terrainRenderTexture.name = "ForestMapTerrainRT";
        terrainRenderTexture.filterMode = FilterMode.Bilinear;
        mapImage.texture = terrainRenderTexture;

        mapTexture = new Texture2D(MapPixels, MapPixels, TextureFormat.RGBA32, false);
        mapTexture.filterMode = FilterMode.Point;
        pixels = new Color32[MapPixels * MapPixels];
        explored = new bool[MapPixels * MapPixels];
        overlayImage.texture = mapTexture;
        CreateTopDownMapCamera();
        RenderTerrainMap();
    }

    // Refreshes and applies configuration or runtime state for apply map size.
    private void ApplyMapSize()
    {
        if (mapRect == null)
        {
            return;
        }

        mapRect.anchorMin = largeMap ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        mapRect.anchorMax = largeMap ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        mapRect.pivot = largeMap ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        mapRect.anchoredPosition = largeMap ? Vector2.zero : new Vector2(-28f, -28f);
        mapRect.sizeDelta = largeMap ? new Vector2(760f, 760f) : new Vector2(250f, 250f);
        float mapPadding = largeMap ? 58f : 24f;
        if (mapImageRect != null)
        {
            mapImageRect.offsetMin = new Vector2(mapPadding, mapPadding);
            mapImageRect.offsetMax = new Vector2(-mapPadding, -mapPadding);
        }

        if (overlayRect != null)
        {
            overlayRect.offsetMin = new Vector2(mapPadding, mapPadding);
            overlayRect.offsetMax = new Vector2(-mapPadding, -mapPadding);
        }

        if (hintText != null)
        {
            hintText.fontSize = largeMap ? 26 : 20;
        }

        UpdateHeartIconUI();
    }

    // Handles the explore around player workflow.
    private void ExploreAroundPlayer()
    {
        if (player == null)
        {
            return;
        }

        if (!WorldToPixel(player.transform.position, out int centerX, out int centerY))
        {
            return;
        }

        int radiusPixels = Mathf.CeilToInt(exploreRadius / worldSize * MapPixels);
        int radiusSqr = radiusPixels * radiusPixels;
        for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
        {
            if (y < 0 || y >= MapPixels)
            {
                continue;
            }

            for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
            {
                if (x < 0 || x >= MapPixels)
                {
                    continue;
                }

                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSqr)
                {
                    explored[y * MapPixels + x] = true;
                }
            }
        }
    }

    // Refreshes and applies configuration or runtime state for refresh map.
    private void RefreshMap(bool force)
    {
        if (mapTexture == null || pixels == null)
        {
            return;
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = explored[i] ? new Color32(0, 0, 0, 0) : new Color32(0, 0, 0, 245);
        }

        if (largeMap)
        {
            DrawGrid();
        }
        DrawPlayerMarker();
        DrawTransferPointMarker();
        mapTexture.SetPixels32(pixels);
        mapTexture.Apply(false);
        UpdateHeartIconUI();
    }

    // Handles the draw grid workflow.
    private void DrawGrid()
    {
        for (int i = 0; i < MapPixels; i += 32)
        {
            DrawLine(i, 0, i, MapPixels - 1, new Color32(56, 92, 74, 120));
            DrawLine(0, i, MapPixels - 1, i, new Color32(56, 92, 74, 120));
        }
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create top down map camera.
    private void CreateTopDownMapCamera()
    {
        if (mapCamera != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("ForestTopDownMapCamera");
        mapCamera = cameraObject.AddComponent<Camera>();
        mapCamera.enabled = false;
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = worldSize * 0.5f;
        mapCamera.transform.position = new Vector3(0f, 850f, 0f);
        mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.08f, 0.12f, 0.1f, 1f);
        mapCamera.cullingMask = ~0;
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = 1800f;
        mapCamera.targetTexture = terrainRenderTexture;
    }

    // Handles the render terrain map if needed workflow.
    private void RenderTerrainMapIfNeeded()
    {
        if (Time.unscaledTime < nextMapRenderTime)
        {
            return;
        }

        RenderTerrainMap();
    }

    // Handles the render terrain map workflow.
    private void RenderTerrainMap()
    {
        if (mapCamera == null)
        {
            return;
        }

        mapCamera.Render();
        nextMapRenderTime = Time.unscaledTime + (largeMap ? 0.25f : 1f);
    }

    // Handles the draw player marker workflow.
    private void DrawPlayerMarker()
    {
        if (player == null || !WorldToPixel(player.transform.position, out int x, out int y))
        {
            return;
        }

        DrawDisc(x, y, largeMap ? 4 : 3, new Color32(72, 190, 255, 255));
        DrawLine(x, y, x + Mathf.RoundToInt(player.transform.forward.x * 8f), y + Mathf.RoundToInt(player.transform.forward.z * 8f), new Color32(210, 245, 255, 255));
    }

    // Handles the draw transfer point marker workflow.
    private void DrawTransferPointMarker()
    {
        if (!ShouldShowTransferPointMarker()
            || transferPoint == null
            || !WorldToPixel(transferPoint.position, out int x, out int y))
        {
            return;
        }

        int radius = largeMap ? 4 : 4;
        DrawDisc(x, y, radius + 1, new Color32(220, 240, 255, 255));
        DrawDisc(x, y, radius, new Color32(20, 90, 255, 255));
    }

    private static bool ShouldShowTransferPointMarker()
    {
        ForestQuestSystem quests = FindObjectOfType<ForestQuestSystem>();
        return quests != null && quests.AreAllQuestsCompleted();
    }

    // Finds, loads, or caches the references needed for find alpine transfer point.
    private static Transform FindAlpineTransferPoint()
    {
        Scene alpineScene = SceneManager.GetSceneByName(ForestRuntimePlayerBootstrap.AlpineSceneName);
        if (!alpineScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = alpineScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                Transform current = transforms[j];
                if (current != null &&
                    string.Equals(current.name, "trans", System.StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }
            }
        }

        return null;
    }

    // Handles the draw heart marker workflow.
    private void DrawHeartMarker()
    {
        if (heartTarget == null)
        {
            heartTarget = FindForestHeart();
        }

        Vector3 heartPosition = heartTarget != null ? heartTarget.position : ForestHeartFallbackPosition;
        if (!WorldToPixel(heartPosition, out int x, out int y))
        {
            return;
        }

        int outer = largeMap ? 12 : 9;
        DrawDiamond(x, y, outer, new Color32(255, 236, 68, 255));
        DrawDiamond(x, y, Mathf.Max(4, outer - 4), new Color32(62, 255, 86, 255));
        DrawDiamond(x, y, Mathf.Max(2, outer - 8), new Color32(245, 255, 230, 255));
    }

    // Refreshes and applies configuration or runtime state for update heart icon ui.
    private void UpdateHeartIconUI()
    {
        if (heartIconRect == null || mapRect == null)
        {
            return;
        }

        bool detectorHeld = IsDetectorHeld();
        heartIconRect.gameObject.SetActive(detectorHeld);
        if (!detectorHeld)
        {
            return;
        }

        Vector3 heartPosition = heartTarget != null
            ? heartTarget.position
            : ForestHeartFallbackPosition;
        float half = worldSize * 0.5f;
        float normalizedX = Mathf.InverseLerp(-half, half, heartPosition.x);
        float normalizedY = Mathf.InverseLerp(-half, half, heartPosition.z);
        bool insideMap = normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f;
        heartIconRect.gameObject.SetActive(insideMap);
        if (!insideMap)
        {
            return;
        }

        Vector2 mapSize = mapRect.sizeDelta;
        float markerSize = largeMap ? 18f : 11f;
        float innerPadding = (largeMap ? 62f : 28f) + markerSize * 0.5f;
        float usableWidth = Mathf.Max(1f, mapSize.x - innerPadding * 2f);
        float usableHeight = Mathf.Max(1f, mapSize.y - innerPadding * 2f);
        Vector2 anchoredPosition = new Vector2((normalizedX - 0.5f) * usableWidth, (normalizedY - 0.5f) * usableHeight);
        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, -mapSize.x * 0.5f + innerPadding, mapSize.x * 0.5f - innerPadding);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, -mapSize.y * 0.5f + innerPadding, mapSize.y * 0.5f - innerPadding);
        heartIconRect.anchoredPosition = anchoredPosition;
        heartIconRect.sizeDelta = new Vector2(markerSize, markerSize);
    }

    // Calculates and returns the result for is detector held.
    private bool IsDetectorHeld()
    {
        if (player == null || player.Slots == null)
        {
            return false;
        }

        int inventoryIndex = player.SelectedInventoryIndex;
        if (inventoryIndex >= 0 && inventoryIndex < player.Slots.Length && IsDetectorSlot(player.Slots[inventoryIndex]))
        {
            return true;
        }

        return false;
    }

    // Calculates and returns the result for is detector slot.
    private static bool IsDetectorSlot(ToolSlot slot)
    {
        if (slot == null)
        {
            return false;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        if (itemId == DetectorItemId)
        {
            return true;
        }

        string displayName = string.IsNullOrEmpty(slot.displayName) ? string.Empty : slot.displayName.ToLowerInvariant();
        string prefabName = slot.prefab != null ? slot.prefab.name.ToLowerInvariant() : string.Empty;
        string worldPrefabName = slot.worldPrefab != null ? slot.worldPrefab.name.ToLowerInvariant() : string.Empty;
        return displayName.Contains("forest heart detector")
            || displayName.Contains("forest_heart_detector")
            || (displayName.Contains("forest") && displayName.Contains("detector"))
            || prefabName.Contains("forestheartdetector")
            || prefabName.Contains("forest_heart_detector")
            || (prefabName.Contains("forest") && prefabName.Contains("detector"))
            || worldPrefabName.Contains("forestheartdetector")
            || worldPrefabName.Contains("forest_heart_detector")
            || (worldPrefabName.Contains("forest") && worldPrefabName.Contains("detector"));
    }

    // Handles the world to pixel workflow.
    private bool WorldToPixel(Vector3 worldPosition, out int x, out int y)
    {
        float half = worldSize * 0.5f;
        x = Mathf.RoundToInt((worldPosition.x + half) / worldSize * (MapPixels - 1));
        y = Mathf.RoundToInt((worldPosition.z + half) / worldSize * (MapPixels - 1));
        return x >= 0 && x < MapPixels && y >= 0 && y < MapPixels;
    }

    // Finds, loads, or caches the references needed for find forest heart.
    private static Transform FindForestHeart()
    {
        Transform[] exactTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < exactTransforms.Length; i++)
        {
            Transform current = exactTransforms[i];
            if (current != null &&
                current.gameObject.scene.name == ForestRuntimePlayerBootstrap.MiningSceneName &&
                current.name.ToLowerInvariant().Contains("pp_gemstone_09_green"))
            {
                if (current.GetComponent<ForestHeartMarker>() == null)
                {
                    current.gameObject.AddComponent<ForestHeartMarker>();
                }

                return current;
            }
        }

        ForestHeartMarker[] markers = FindObjectsOfType<ForestHeartMarker>(true);
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null && IsSceneForestHeartCandidate(markers[i].transform))
            {
                return markers[i].transform;
            }
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (!IsSceneForestHeartCandidate(current))
            {
                continue;
            }

            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("forestheart")
                || lowerName.Contains("forest_heart")
                || lowerName.Contains("森林之心")
                || lowerName.Contains("pp_gemstone_09_green"))
            {
                if (current.GetComponent<ForestHeartMarker>() == null)
                {
                    current.gameObject.AddComponent<ForestHeartMarker>();
                }

                return current;
            }
        }

        return null;
    }

    // Calculates and returns the result for is scene forest heart candidate.
    private static bool IsSceneForestHeartCandidate(Transform current)
    {
        if (current == null || !current.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (current.GetComponentInParent<PlayerToolController>() != null
            || current.GetComponentInParent<BackpackUI>() != null)
        {
            return false;
        }

        string lowerName = current.name.ToLowerInvariant();
        if (lowerName.Contains("held_forestheart") || lowerName.Contains("held_forest_heart"))
        {
            return false;
        }

        return lowerName.Contains("pp_gemstone_09_green")
            || lowerName.Contains("forestheart")
            || lowerName.Contains("forest_heart")
            || lowerName.Contains("妫灄涔嬪績");
    }

    // Handles the draw disc workflow.
    private void DrawDisc(int centerX, int centerY, int radius, Color32 color)
    {
        int radiusSqr = radius * radius;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSqr)
                {
                    SetPixel(x, y, color);
                }
            }
        }
    }

    // Handles the draw diamond workflow.
    private void DrawDiamond(int centerX, int centerY, int radius, Color32 color)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY) <= radius)
                {
                    SetPixel(x, y, color);
                }
            }
        }
    }

    // Handles the draw line workflow.
    private void DrawLine(int x0, int y0, int x1, int y1, Color32 color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int doubleError = 2 * error;
            if (doubleError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubleError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    // Sets state, selection, or placement data for set pixel.
    private void SetPixel(int x, int y, Color32 color)
    {
        if (x < 0 || x >= MapPixels || y < 0 || y >= MapPixels)
        {
            return;
        }

        pixels[y * MapPixels + x] = color;
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

    // Creates or rebuilds the runtime objects, assets, or UI for create circle sprite.
    private static Sprite CreateCircleSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ForestHeartMapCircle";
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 fill = new Color32(255, 255, 255, 255);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                texture.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? fill : clear);
            }
        }

        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create resource sprite.
    private static Sprite CreateResourceSprite(string resourceName)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourceName);
        return texture != null
            ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f))
            : null;
    }

    // Calculates and returns the result for get explored snapshot.
    public string GetExploredSnapshot()
    {
        if (explored == null)
        {
            return string.Empty;
        }

        byte[] bytes = new byte[(explored.Length + 7) / 8];
        for (int i = 0; i < explored.Length; i++)
        {
            if (explored[i])
            {
                bytes[i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return System.Convert.ToBase64String(bytes);
    }

    // Handles the restore explored workflow.
    public void RestoreExplored(string snapshot)
    {
        if (explored == null || string.IsNullOrEmpty(snapshot))
        {
            return;
        }

        try
        {
            byte[] bytes = System.Convert.FromBase64String(snapshot);
            for (int i = 0; i < explored.Length; i++)
            {
                explored[i] = i / 8 < bytes.Length && (bytes[i / 8] & (1 << (i % 8))) != 0;
            }

            RefreshMap(true);
        }
        catch (System.FormatException)
        {
            Debug.LogWarning("ForestMapUI: Ignored invalid explored-map save data.");
        }
    }
}
