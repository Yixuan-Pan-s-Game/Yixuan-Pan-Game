using UnityEngine;
using UnityEngine.UI;

public class ForestHeartLiberation : MonoBehaviour
{
    public float interactDistance = 2.2f;
    public float riseHeight = 5.5f;
    public float riseDuration = 4.5f;
    public float messageDuration = 6f;

    private Transform player;
    private Transform heart;
    private Canvas canvas;
    private Text promptText;
    private Text successText;
    private bool liberating;
    private bool liberated;
    private float liberationStartedAt;
    private float successShownAt = -999f;
    private Vector3 heartStartPosition;
    private Vector3 heartEndPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindObjectOfType<ForestHeartLiberation>(true) != null)
        {
            return;
        }

        new GameObject("ForestHeartLiberation").AddComponent<ForestHeartLiberation>();
    }

    private void Start()
    {
        BuildUI();
    }

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
