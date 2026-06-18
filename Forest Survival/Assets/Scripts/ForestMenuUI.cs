using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu and pause menu UI controller.
public class ForestMenuUI : MonoBehaviour
{
    // Runtime flag that drives control flow, UI state, or gameplay availability: IsMenuOpen.
    public static bool IsMenuOpen { get; private set; }

    // Important runtime data or configuration used by this component: instance.
    private static ForestMenuUI instance;

    [Header("Panels")]
    // Timing value or timestamp used for cooldowns, delays, or progress checks: startPanel.
    [SerializeField] private GameObject startPanel;
    // Cached component or scene reference to avoid repeated lookups: deathPanel.
    [SerializeField] private GameObject deathPanel;
    // Cached component or scene reference to avoid repeated lookups: instructionPanel.
    [SerializeField] private GameObject instructionPanel;
    // Cached component or scene reference to avoid repeated lookups: overwritePanel.
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private GameObject quitConfirmPanel;

    [Header("Main Menu Buttons")]
    // Timing value or timestamp used for cooldowns, delays, or progress checks: startButton.
    [SerializeField] private Button startButton;
    // Cached component or scene reference to avoid repeated lookups: continueButton.
    [SerializeField] private Button continueButton;
    // Cached component or scene reference to avoid repeated lookups: instructionButton.
    [SerializeField] private Button instructionButton;
    // Cached component or scene reference to avoid repeated lookups: instructionBackButton.
    [SerializeField] private Button instructionBackButton;

    [Header("Overwrite Save Buttons")]
    // Cached component or scene reference to avoid repeated lookups: overwriteYesButton.
    [SerializeField] private Button overwriteYesButton;
    // Cached component or scene reference to avoid repeated lookups: overwriteNoButton.
    [SerializeField] private Button overwriteNoButton;

    [Header("Death Menu Buttons")]
    // Timing value or timestamp used for cooldowns, delays, or progress checks: restartButton.
    [SerializeField] private Button restartButton;

    [Header("Quit Buttons")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button quitYesButton;
    [SerializeField] private Button quitNoButton;

    // Gameplay stat that affects damage, health, healing, defense, or durability: playerHealth.
    private PlayerHealth playerHealth;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: startingNewGame.
    private bool startingNewGame;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: gameStarted.
    private bool gameStarted;
    // Runtime flag that drives control flow, UI state, or gameplay availability: deathShown.
    private bool deathShown;
    // Runtime flag that drives control flow, UI state, or gameplay availability: instructionShownInGame.
    private bool instructionShownInGame;
    private bool quitResumeGameplayOnCancel;
    private readonly List<GameObject> hiddenHudCanvases = new List<GameObject>();

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BindSceneReferences();
        RegisterButtons();
        HideSecondaryPanels();
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        ShowStartMenu();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (IsMenuOpen)
        {
            SetGameplayHudVisible(false);
        }

        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (gameStarted && !deathShown && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            ShowDeathMenu();
        }

        if (gameStarted && !deathShown && Input.GetKeyDown(KeyCode.I))
        {
            ToggleInstructionPanelInGame();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            ShowQuitConfirmation();
        }
    }

    // Handles the bind scene references workflow.
    private void BindSceneReferences()
    {
        if (startPanel == null || startPanel == gameObject)
        {
            startPanel = FindChildGameObject("StartPanel", "MainPanel", "MenuPanel", "MainMenuPanel");
        }

        if (deathPanel == null)
        {
            deathPanel = FindChildGameObject("DeathPanel", "GameOverPanel", "DeathMenuPanel");
        }

        if (instructionPanel == null)
        {
            instructionPanel = FindChildGameObject("InstructionPanel");
        }

        if (startButton == null)
        {
            startButton = FindChildButton("StartButton", "startBUtton", "Start Button");
        }

        if (continueButton == null)
        {
            continueButton = FindChildButton("ContinueButton", "Continue button", "Continue Button");
        }

        if (instructionButton == null)
        {
            instructionButton = FindChildButton("InstructionButton", "Instruction Button");
        }

        if (instructionBackButton == null)
        {
            instructionBackButton = FindChildButton("BackButton", "Back Button");
        }

        if (overwriteYesButton == null)
        {
            overwriteYesButton = FindChildButton("OverwriteYesButton", "YesButton", "Yes Button");
        }

        if (overwriteNoButton == null)
        {
            overwriteNoButton = FindChildButton("OverwriteNoButton", "NoButton", "No Button");
        }

        if (restartButton == null)
        {
            restartButton = FindChildButton("RestartButton", "Restart Button");
        }

        if (quitConfirmPanel == null)
        {
            quitConfirmPanel = FindChildGameObject("QuitConfirmPanel", "QuitPanel", "ExitConfirmPanel");
        }

        if (quitButton == null)
        {
            quitButton = FindChildButton("QuitButton", "ExitButton", "CloseButton");
        }

        if (quitYesButton == null)
        {
            quitYesButton = FindChildButton("QuitYesButton", "ExitYesButton");
        }

        if (quitNoButton == null)
        {
            quitNoButton = FindChildButton("QuitNoButton", "ExitNoButton");
        }

        if (deathPanel == null)
        {
            deathPanel = BuildRuntimeDeathPanel();
        }

        if (restartButton == null && deathPanel != null)
        {
            restartButton = deathPanel.GetComponentInChildren<Button>(true);
        }

        if (quitButton == null)
        {
            quitButton = BuildRuntimeQuitButton();
        }

        if (quitConfirmPanel == null)
        {
            quitConfirmPanel = BuildRuntimeQuitConfirmPanel();
        }
    }

    // Finds, loads, or caches the references needed for find child game object.
    private GameObject FindChildGameObject(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child != null ? child.gameObject : null;
    }

    // Finds, loads, or caches the references needed for find child button.
    private Button FindChildButton(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child != null ? child.GetComponent<Button>() : null;
    }

    // Finds, loads, or caches the references needed for find child transform.
    private Transform FindChildTransform(params string[] names)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
            {
                continue;
            }

            for (int j = 0; j < names.Length; j++)
            {
                if (child.name == names[j])
                {
                    return child;
                }
            }
        }

        return null;
    }
    // Handles the register buttons workflow.
    private void RegisterButtons()
    {
        RegisterButton(startButton, CreateNewGame);
        RegisterButton(continueButton, ContinueGame);
        RegisterButton(instructionButton, ShowInstructionPanel);
        RegisterButton(instructionBackButton, HideInstructionPanel);
        RegisterButton(overwriteYesButton, ConfirmCreateNewGame);
        RegisterButton(overwriteNoButton, HideOverwritePanel);
        RegisterButton(restartButton, RestartGame);
        RegisterButton(quitButton, ShowQuitConfirmation);
        RegisterButton(quitYesButton, ConfirmQuitGame);
        RegisterButton(quitNoButton, HideQuitConfirmation);
    }

    // Handles the register button workflow.
    private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    // Handles the show start menu workflow.
    private void ShowStartMenu()
    {
        gameStarted = false;
        deathShown = false;
        instructionShownInGame = false;
        SetPanelActive(startPanel, true);
        SetPanelActive(deathPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
        SetPanelActive(quitConfirmPanel, false);
        SetPaused(true);
    }

    // Handles the start game workflow.
    private void StartGame()
    {
        IsMenuOpen = false;
        gameStarted = true;
        deathShown = false;
        instructionShownInGame = false;
        HideAllPanels();
        SetPaused(false);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create new game.
    public void CreateNewGame()
    {
        if (gameStarted)
        {
            return;
        }

        if (ForestSaveSystem.HasSave && overwritePanel != null)
        {
            SetPanelActive(overwritePanel, true);
            return;
        }

        ConfirmCreateNewGame();
    }

    // Handles the confirm create new game workflow.
    public void ConfirmCreateNewGame()
    {
        if (startingNewGame)
        {
            return;
        }

        StartCoroutine(CreateNewGameRoutine());
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create new game routine.
    private IEnumerator CreateNewGameRoutine()
    {
        startingNewGame = true;
        ForestSaveSystem.DeleteSave();
        SetPanelActive(overwritePanel, false);
        ForestRuntimePlayerBootstrap.RequestSpawn("Cube");

        Scene alpineScene = SceneManager.GetSceneByName(ForestRuntimePlayerBootstrap.AlpineSceneName);
        if (!alpineScene.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(ForestRuntimePlayerBootstrap.AlpineSceneName, LoadSceneMode.Additive);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            alpineScene = SceneManager.GetSceneByName(ForestRuntimePlayerBootstrap.AlpineSceneName);
        }

        if (alpineScene.isLoaded)
        {
            SceneManager.SetActiveScene(alpineScene);
        }

        yield return null;
        yield return null;
        playerHealth = FindObjectOfType<PlayerHealth>();
        StartGame();
        ForestSaveSystem.SaveGame();
        startingNewGame = false;
    }

    // Handles the continue game workflow.
    public void ContinueGame()
    {
        SetPanelActive(overwritePanel, false);
        bool loadStarted = ForestSaveSystem.LoadGame();
        if (!loadStarted)
        {
            return;
        }

        StartGame();
        SetPanelActive(overwritePanel, false);
    }

    // Handles the notify player died workflow.
    public static void NotifyPlayerDied(PlayerHealth health)
    {
        if (instance == null)
        {
            instance = FindObjectOfType<ForestMenuUI>();
        }

        if (instance == null)
        {
            return;
        }

        if (health != null)
        {
            instance.playerHealth = health;
        }

        instance.ShowDeathMenu();
    }

    // Handles the show death menu workflow.
    private void ShowDeathMenu()
    {
        if (deathShown)
        {
            return;
        }

        deathShown = true;
        instructionShownInGame = false;
        SetPanelActive(deathPanel, true);
        SetPanelActive(startPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
        SetPanelActive(quitConfirmPanel, false);
        SetPaused(true);
    }

    // Handles the show instruction panel workflow.
    public void ShowInstructionPanel()
    {
        SetPanelActive(instructionPanel, true);
        if (gameStarted && !deathShown)
        {
            instructionShownInGame = true;
            SetPaused(true);
        }
    }

    // Handles the hide instruction panel workflow.
    public void HideInstructionPanel()
    {
        SetPanelActive(instructionPanel, false);
        if (gameStarted && !deathShown && instructionShownInGame)
        {
            instructionShownInGame = false;
            SetPaused(false);
        }
    }

    // Handles the hide overwrite panel workflow.
    public void HideOverwritePanel()
    {
        SetPanelActive(overwritePanel, false);
    }

    public void ShowQuitConfirmation()
    {
        if (quitConfirmPanel == null)
        {
            quitConfirmPanel = BuildRuntimeQuitConfirmPanel();
            RegisterButton(quitYesButton, ConfirmQuitGame);
            RegisterButton(quitNoButton, HideQuitConfirmation);
        }

        quitResumeGameplayOnCancel = gameStarted && !deathShown && !IsMenuOpen;
        SetPanelActive(overwritePanel, false);
        SetPanelActive(quitConfirmPanel, true);
        SetPaused(true);
    }

    public void HideQuitConfirmation()
    {
        SetPanelActive(quitConfirmPanel, false);
        if (quitResumeGameplayOnCancel)
        {
            quitResumeGameplayOnCancel = false;
            SetPaused(false);
        }
    }

    public void ConfirmQuitGame()
    {
        ForestSaveSystem.SaveGame();
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Handles the restart game workflow.
    public void RestartGame()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (!PlayerRespawnSystem.Respawn(playerHealth))
        {
            return;
        }

        deathShown = false;
        StartGame();
    }

    // Sets state, selection, or placement data for toggle instruction panel in game.
    private void ToggleInstructionPanelInGame()
    {
        if (instructionPanel == null)
        {
            return;
        }

        if (instructionShownInGame || instructionPanel.activeSelf)
        {
            HideInstructionPanel();
        }
        else
        {
            ShowInstructionPanel();
        }
    }

    // Handles the hide secondary panels workflow.
    private void HideSecondaryPanels()
    {
        SetPanelActive(deathPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
        SetPanelActive(quitConfirmPanel, false);
    }

    // Handles the hide all panels workflow.
    private void HideAllPanels()
    {
        SetPanelActive(startPanel, false);
        SetPanelActive(deathPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
        SetPanelActive(quitConfirmPanel, false);
    }

    private Button BuildRuntimeQuitButton()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        Button button = CreateRuntimeButton(parent, "QuitButton", "X", new Color(0.45f, 0.08f, 0.07f, 0.95f));
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.sizeDelta = new Vector2(54f, 48f);
        buttonRect.anchoredPosition = new Vector2(-24f, -22f);
        button.transform.SetAsLastSibling();
        return button;
    }

    private GameObject BuildRuntimeQuitConfirmPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject panel = new GameObject("QuitConfirmPanel");
        panel.transform.SetParent(parent, false);
        panel.transform.SetAsLastSibling();

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject box = new GameObject("QuitConfirmBox");
        box.transform.SetParent(panel.transform, false);
        RectTransform boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(520f, 240f);
        boxRect.anchoredPosition = Vector2.zero;

        Image boxImage = box.AddComponent<Image>();
        boxImage.color = new Color(0.08f, 0.1f, 0.08f, 0.96f);

        Text title = CreateRuntimeText("QuitTitle", box.transform, "Exit Game?", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(460f, 58f);
        titleRect.anchoredPosition = new Vector2(0f, -26f);

        Text message = CreateRuntimeText("QuitMessage", box.transform, "Are you sure you want to quit?", 22, FontStyle.Normal, TextAnchor.MiddleCenter);
        RectTransform messageRect = message.rectTransform;
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.sizeDelta = new Vector2(460f, 44f);
        messageRect.anchoredPosition = new Vector2(0f, 18f);

        quitYesButton = CreateRuntimeButton(box.transform, "QuitYesButton", "Yes", new Color(0.45f, 0.08f, 0.07f, 0.95f));
        RectTransform yesRect = quitYesButton.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(0.5f, 0f);
        yesRect.anchorMax = new Vector2(0.5f, 0f);
        yesRect.pivot = new Vector2(0.5f, 0f);
        yesRect.sizeDelta = new Vector2(150f, 54f);
        yesRect.anchoredPosition = new Vector2(-92f, 32f);

        quitNoButton = CreateRuntimeButton(box.transform, "QuitNoButton", "No", new Color(0.16f, 0.42f, 0.2f, 0.95f));
        RectTransform noRect = quitNoButton.GetComponent<RectTransform>();
        noRect.anchorMin = new Vector2(0.5f, 0f);
        noRect.anchorMax = new Vector2(0.5f, 0f);
        noRect.pivot = new Vector2(0.5f, 0f);
        noRect.sizeDelta = new Vector2(150f, 54f);
        noRect.anchoredPosition = new Vector2(92f, 32f);

        SetPanelActive(panel, false);
        return panel;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for build runtime death panel.
    private GameObject BuildRuntimeDeathPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject panel = new GameObject("DeathPanel");
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);

        Text title = CreateRuntimeText("DeathTitle", panel.transform, "YOU DIED", 54, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(620f, 90f);
        titleRect.anchoredPosition = new Vector2(0f, 70f);

        Button button = CreateRuntimeButton(panel.transform);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(260f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, -55f);
        restartButton = button;

        SetPanelActive(panel, false);
        return panel;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create runtime text.
    private static Text CreateRuntimeText(string name, Transform parent, string text, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text label = textObject.AddComponent<Text>();
        label.text = text;
        label.font = GetBuiltinUIFont();
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create runtime button.
    private static Button CreateRuntimeButton(Transform parent)
    {
        return CreateRuntimeButton(parent, "RestartButton", "Restart", new Color(0.16f, 0.42f, 0.2f, 0.95f));
    }

    private static Button CreateRuntimeButton(Transform parent, string name, string labelText, Color color)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.22f, 0.55f, 0.27f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.14f, 1f);
        button.colors = colors;

        Text label = CreateRuntimeText("Text", buttonObject.transform, labelText, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    // Calculates and returns the result for get builtin uifont.
    private static Font GetBuiltinUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    // Sets state, selection, or placement data for set panel active.
    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    // Sets state, selection, or placement data for set paused.
    private static void SetPaused(bool paused)
    {
        IsMenuOpen = paused;
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
        SetGameplayHudVisible(!paused);
    }

    // Sets state, selection, or placement data for set gameplay hud visible.
    private static void SetGameplayHudVisible(bool visible)
    {
        if (instance == null)
        {
            return;
        }

        if (!visible)
        {
            instance.HideHudCanvas("HotbarCanvas");
            instance.HideHudCanvas("QuestCanvas");
            instance.HideHudCanvas("ForestMapCanvas");
            return;
        }

        instance.ShowHudCanvas("HotbarCanvas");
        instance.ShowHudCanvas("QuestCanvas");
        instance.ShowHudCanvas("ForestMapCanvas");
        instance.hiddenHudCanvases.Clear();
    }

    // Handles the hide hud canvas workflow.
    private void HideHudCanvas(string canvasName)
    {
        GameObject canvas = FindHudCanvas(canvasName);
        if (canvas == null || !canvas.activeSelf)
        {
            return;
        }

        if (!hiddenHudCanvases.Contains(canvas))
        {
            hiddenHudCanvases.Add(canvas);
        }

        canvas.SetActive(false);
    }

    // Handles the show hud canvas workflow.
    private void ShowHudCanvas(string canvasName)
    {
        GameObject canvas = FindHudCanvas(canvasName);
        if (canvas != null)
        {
            canvas.SetActive(true);
        }
    }

    // Finds, loads, or caches the references needed for find hud canvas.
    private static GameObject FindHudCanvas(string canvasName)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.name == canvasName && canvas.gameObject.scene.IsValid())
            {
                return canvas.gameObject;
            }
        }

        return null;
    }
}





