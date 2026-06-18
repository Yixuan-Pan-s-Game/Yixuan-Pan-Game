using UnityEngine;
using UnityEngine.UI;

// Handles the Forest Heart liberation interaction and final world-state changes.
public class ForestHeartLiberation : MonoBehaviour
{
    // Distance or radius used for detection, interaction, or physics checks: interactDistance.
    public float interactDistance = 2.2f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: riseHeight.
    public float riseHeight = 11f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: riseDuration.
    public float riseDuration = 4.5f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: messageDuration.
    public float messageDuration = 6f;

    // Layer or mask filter used by physics queries or rendering: player.
    private Transform player;
    // Important runtime data or configuration used by this component: heart.
    private Transform heart;
    // Cached component or scene reference to avoid repeated lookups: canvas.
    private Canvas canvas;
    // Cached component or scene reference to avoid repeated lookups: promptText.
    private Text promptText;
    // Cached component or scene reference to avoid repeated lookups: successText.
    private Text successText;
    // Important runtime data or configuration used by this component: liberating.
    private bool liberating;
    // Important runtime data or configuration used by this component: liberated.
    private bool liberated;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: liberationStartedAt.
    private float liberationStartedAt;
    // Runtime flag that drives control flow, UI state, or gameplay availability: successShownAt.
    private float successShownAt = -999f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: heartStartPosition.
    private Vector3 heartStartPosition;
    // Spatial value used for positioning, rotation, scale, or collision math: heartEndPosition.
    private Vector3 heartEndPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Ensures the objects, references, or configuration required for ensure exists exist.
    private static void EnsureExists()
    {
        if (FindObjectOfType<ForestHeartLiberation>(true) != null)
        {
            return;
        }

        new GameObject("ForestHeartLiberation").AddComponent<ForestHeartLiberation>();
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        BuildUI();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (player == null)
        {
            PlayerToolController toolController = FindObjectOfType<PlayerToolController>();
            player = toolController != null ? toolController.transform : null;
        }

        if (heart == null)
        {
            heart = FindForestHeart();
        }

        UpdateLiberation();
        UpdatePrompt();
        UpdateSuccessMessage();
    }

    // Refreshes and applies configuration or runtime state for update prompt.
    private void UpdatePrompt()
    {
        if (promptText == null)
        {
            return;
        }

        bool canInteract = !liberating && !liberated && player != null && heart != null
            && Vector3.Distance(player.position, heart.position) <= interactDistance;
        promptText.gameObject.SetActive(canInteract);
        if (!canInteract)
        {
            return;
        }

        promptText.text = "Press E to liberate the Forest Heart";
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartLiberation();
        }
    }

    // Handles the start liberation workflow.
    private void StartLiberation()
    {
        if (heart == null)
        {
            return;
        }

        liberating = true;
        liberationStartedAt = Time.time;
        heartStartPosition = heart.position;
        heartEndPosition = heartStartPosition + Vector3.up * riseHeight;
        promptText.gameObject.SetActive(false);
    }

    // Refreshes and applies configuration or runtime state for update liberation.
    private void UpdateLiberation()
    {
        if (!liberating || heart == null)
        {
            return;
        }

        float t = Mathf.Clamp01((Time.time - liberationStartedAt) / Mathf.Max(0.1f, riseDuration));
        float eased = Mathf.SmoothStep(0f, 1f, t);
        heart.position = Vector3.Lerp(heartStartPosition, heartEndPosition, eased);
        heart.Rotate(Vector3.up, 28f * Time.deltaTime, Space.World);

        if (t >= 1f)
        {
            liberating = false;
            liberated = true;
            successShownAt = Time.time;
            if (successText != null)
            {
                successText.gameObject.SetActive(true);
                successText.text = "Congratulations!\nYou have saved the entire forest.";
            }
        }
    }

    // Refreshes and applies configuration or runtime state for update success message.
    private void UpdateSuccessMessage()
    {
        if (successText == null || !successText.gameObject.activeSelf)
        {
            return;
        }

        if (Time.time - successShownAt >= messageDuration)
        {
            successText.gameObject.SetActive(false);
        }
    }

    // Finds, loads, or caches the references needed for find forest heart.
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

    // Creates or rebuilds the runtime objects, assets, or UI for build ui.
    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("ForestHeartLiberationCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 145;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        promptText = CreateText("LiberationPrompt", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360f, -248f), new Vector2(360f, -196f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        promptText.color = new Color(0.78f, 1f, 0.65f, 0.96f);
        promptText.gameObject.SetActive(false);

        successText = CreateText("LiberationSuccess", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -86f), new Vector2(520f, 86f), 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        successText.color = new Color(0.8f, 1f, 0.64f, 0.98f);
        successText.gameObject.SetActive(false);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create text.
    private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle style, TextAnchor alignment)
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
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
