using UnityEngine;
using UnityEngine.UI;

public class ForestHeartDetector : MonoBehaviour
{
    private const string DetectorItemId = "forest_heart_detector";
    private PlayerToolController player;
    private Camera playerCamera;
    private Camera heartCamera;
    private RectTransform arrow;
    private Text statusText;
    private Transform heartTarget;
    private Renderer[] heartRenderers;
    private int[] originalHeartLayers;
    private int heartVisionLayer = 30;
    private bool heartVisionActive;

    private void Start()
    {
        player = FindObjectOfType<PlayerToolController>();
        playerCamera = Camera.main;
        BuildUI();
        EnsureHeartCamera();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerToolController>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        bool active = IsDetectorHeld();
        SetHeartVisionActive(active);
        arrow.gameObject.SetActive(false);
        statusText.gameObject.SetActive(active);
        if (!active)
        {
            return;
        }

        if (heartTarget == null)
        {
            heartTarget = FindForestHeart();
        }

        if (heartTarget == null || playerCamera == null)
        {
            statusText.text = "Forest Heart not found";
            return;
        }

        SyncHeartCamera();

        Vector3 flatDirection = heartTarget.position - playerCamera.transform.position;
        flatDirection.y = 0f;
        Vector3 flatForward = playerCamera.transform.forward;
        flatForward.y = 0f;
        if (flatDirection.sqrMagnitude < 0.001f || flatForward.sqrMagnitude < 0.001f)
        {
            return;
        }

        float angle = Vector3.SignedAngle(flatForward.normalized, flatDirection.normalized, Vector3.up);
        arrow.localEulerAngles = new Vector3(0f, 0f, -angle);
        statusText.text = "Forest Heart";
    }

    private void OnDisable()
    {
        SetHeartVisionActive(false);
    }

    private bool IsDetectorHeld()
    {
        if (player == null || player.Slots == null)
        {
            return false;
        }

        int inventoryIndex = player.SelectedInventoryIndex;
        if (inventoryIndex < 0 || inventoryIndex >= player.Slots.Length || player.Slots[inventoryIndex] == null)
        {
            return false;
        }

        return InventoryUtility.GetItemId(player.Slots[inventoryIndex]) == DetectorItemId;
    }

    private static Transform FindForestHeart()
    {
        Transform[] exactTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < exactTransforms.Length; i++)
        {
            Transform current = exactTransforms[i];
            if (current != null && current.name.ToLowerInvariant().Contains("pp_gemstone_09_green"))
            {
                return current;
            }
        }

        ForestHeartMarker marker = FindObjectOfType<ForestHeartMarker>(true);
        if (marker != null)
        {
            return marker.transform;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null)
            {
                continue;
            }

            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("forestheart")
                || lowerName.Contains("forest_heart")
                || lowerName.Contains("森林之心")
                || lowerName.Contains("pp_gemstone_09_green"))
            {
                return current;
            }
        }

        return null;
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("ForestHeartDetectorCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject arrowObject = new GameObject("HeartDirectionArrow");
        arrowObject.transform.SetParent(canvasObject.transform, false);
        arrow = arrowObject.AddComponent<RectTransform>();
        arrow.anchorMin = new Vector2(0.5f, 0.5f);
        arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.pivot = new Vector2(0.5f, 0.5f);
        arrow.anchoredPosition = new Vector2(0f, 190f);
        arrow.sizeDelta = new Vector2(92f, 92f);
        Text arrowText = arrowObject.AddComponent<Text>();
        arrowText.font = GetBuiltinUIFont();
        arrowText.text = "^";
        arrowText.fontSize = 86;
        arrowText.fontStyle = FontStyle.Bold;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = new Color(0.45f, 1f, 0.32f, 0.95f);

        GameObject statusObject = new GameObject("HeartDirectionStatus");
        statusObject.transform.SetParent(canvasObject.transform, false);
        RectTransform statusRect = statusObject.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0f, 122f);
        statusRect.sizeDelta = new Vector2(360f, 42f);
        statusText = statusObject.AddComponent<Text>();
        statusText.font = GetBuiltinUIFont();
        statusText.fontSize = 24;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(0.75f, 1f, 0.62f, 0.95f);

        arrow.gameObject.SetActive(false);
        statusText.gameObject.SetActive(false);
    }

    private void EnsureHeartCamera()
    {
        if (heartCamera != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("ForestHeartVisionCamera");
        heartCamera = cameraObject.AddComponent<Camera>();
        heartCamera.enabled = false;
        heartCamera.clearFlags = CameraClearFlags.Depth;
        heartCamera.cullingMask = 1 << heartVisionLayer;
        heartCamera.depth = playerCamera != null ? playerCamera.depth + 20f : 120f;
    }

    private void SetHeartVisionActive(bool active)
    {
        if (active)
        {
            if (heartTarget == null)
            {
                heartTarget = FindForestHeart();
            }

            if (heartTarget != null && !heartVisionActive)
            {
                CacheAndMoveHeartToVisionLayer();
                heartVisionActive = true;
            }
        }
        else if (heartVisionActive)
        {
            RestoreHeartLayers();
            heartVisionActive = false;
        }

        if (heartCamera != null)
        {
            heartCamera.enabled = active && heartTarget != null;
        }
    }

    private void CacheAndMoveHeartToVisionLayer()
    {
        if (heartTarget == null)
        {
            return;
        }

        heartRenderers = heartTarget.GetComponentsInChildren<Renderer>(true);
        originalHeartLayers = new int[heartRenderers.Length];
        for (int i = 0; i < heartRenderers.Length; i++)
        {
            if (heartRenderers[i] == null)
            {
                continue;
            }

            originalHeartLayers[i] = heartRenderers[i].gameObject.layer;
            heartRenderers[i].gameObject.layer = heartVisionLayer;
        }
    }

    private void RestoreHeartLayers()
    {
        if (heartRenderers == null || originalHeartLayers == null)
        {
            return;
        }

        for (int i = 0; i < heartRenderers.Length && i < originalHeartLayers.Length; i++)
        {
            if (heartRenderers[i] != null)
            {
                heartRenderers[i].gameObject.layer = originalHeartLayers[i];
            }
        }
    }

    private void SyncHeartCamera()
    {
        EnsureHeartCamera();
        if (heartCamera == null || playerCamera == null)
        {
            return;
        }

        heartCamera.transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
        heartCamera.fieldOfView = playerCamera.fieldOfView;
        heartCamera.nearClipPlane = playerCamera.nearClipPlane;
        heartCamera.farClipPlane = playerCamera.farClipPlane;
        heartCamera.aspect = playerCamera.aspect;
        heartCamera.orthographic = playerCamera.orthographic;
        heartCamera.orthographicSize = playerCamera.orthographicSize;
        heartCamera.depth = playerCamera.depth + 20f;
    }

    private static Font GetBuiltinUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }
}

public class ForestHeartMarker : MonoBehaviour
{
}
