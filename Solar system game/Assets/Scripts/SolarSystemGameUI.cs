using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SolarSystemGameUI : MonoBehaviour
{
    [SerializeField] private Font uiFont;

    private Text titleText;
    private Text factText;
    private Button returnButton;
    private SolarSystemCameraController cameraController;
    private Font cachedRuntimeFont;

    private void Awake()
    {
        cameraController = FindObjectOfType<SolarSystemCameraController>();
        CreateCanvas();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            HideFact();
        }
    }

    public void ShowFact(string title, string fact)
    {
        titleText.text = title;
        factText.text = fact;
        titleText.transform.parent.gameObject.SetActive(true);
    }

    public void HideFact()
    {
        if (titleText != null)
        {
            titleText.transform.parent.gameObject.SetActive(false);
        }
    }

    private void CreateCanvas()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        GameObject canvasObject = new GameObject("Solar System UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Fact Panel", typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.04f);
        panelRect.anchorMax = new Vector2(0.92f, 0.23f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.04f, 0.09f, 0.16f, 0.86f);

        titleText = CreateText("Title", panel.transform, 26, FontStyle.Bold, TextAnchor.UpperLeft);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.03f, 0.58f);
        titleRect.anchorMax = new Vector2(0.76f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        factText = CreateText("Fact", panel.transform, 22, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform factRect = factText.GetComponent<RectTransform>();
        factRect.anchorMin = new Vector2(0.03f, 0.12f);
        factRect.anchorMax = new Vector2(0.76f, 0.58f);
        factRect.offsetMin = Vector2.zero;
        factRect.offsetMax = Vector2.zero;

        returnButton = CreateReturnButton(panel.transform);
        panel.SetActive(false);
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = GetRuntimeFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Font GetRuntimeFont()
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        if (cachedRuntimeFont != null)
        {
            return cachedRuntimeFont;
        }

        cachedRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedRuntimeFont == null)
        {
            cachedRuntimeFont = Font.CreateDynamicFontFromOSFont("Arial", 18);
        }

        return cachedRuntimeFont;
    }

    private Button CreateReturnButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Return Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.79f, 0.18f);
        rect.anchorMax = new Vector2(0.97f, 0.82f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 0.79f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            cameraController = cameraController != null ? cameraController : FindObjectOfType<SolarSystemCameraController>();
            if (cameraController != null)
            {
                cameraController.ReturnHome();
            }

            HideFact();
        });

        Text label = CreateText("Label", buttonObject.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        label.text = "返回 R";
        label.color = new Color(0.05f, 0.07f, 0.11f, 1f);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }
}
