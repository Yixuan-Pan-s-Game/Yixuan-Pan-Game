using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ForestMenuUI : MonoBehaviour
{
    public static bool IsMenuOpen { get; private set; }

    private static ForestMenuUI instance;

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject overwritePanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button instructionButton;
    [SerializeField] private Button instructionBackButton;

    [Header("Overwrite Save Buttons")]
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Death Menu Buttons")]
    [SerializeField] private Button restartButton;

    private PlayerHealth playerHealth;
    private bool startingNewGame;
    private bool gameStarted;
    private bool deathShown;
    private readonly List<GameObject> hiddenHudCanvases = new List<GameObject>();

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

    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        ShowStartMenu();
    }

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
    }

    private void BindSceneReferences()
    {
        if (startPanel == null)
        {
            startPanel = gameObject;
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
    }

    private GameObject FindChildGameObject(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child != null ? child.gameObject : null;
    }

    private Button FindChildButton(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child != null ? child.GetComponent<Button>() : null;
    }

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
    private void RegisterButtons()
    {
        RegisterButton(startButton, CreateNewGame);
        RegisterButton(continueButton, ContinueGame);
        RegisterButton(instructionButton, ShowInstructionPanel);
        RegisterButton(instructionBackButton, HideInstructionPanel);
        RegisterButton(overwriteYesButton, ConfirmCreateNewGame);
        RegisterButton(overwriteNoButton, HideOverwritePanel);
        RegisterButton(restartButton, RestartGame);
    }

    private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ShowStartMenu()
    {
        gameStarted = false;
        deathShown = false;
        SetPanelActive(startPanel, true);
        SetPanelActive(deathPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
        SetPaused(true);
    }

    private void StartGame()
    {
        IsMenuOpen = false;
        gameStarted = true;
        deathShown = false;
        HideAllPanels();
        SetPaused(false);
    }

    public void CreateNewGame()
    {
        if (ForestSaveSystem.HasSave && overwritePanel != null)
        {
            SetPanelActive(overwritePanel, true);
            return;
        }

        ConfirmCreateNewGame();
    }

    public void ConfirmCreateNewGame()
    {
        if (startingNewGame)
        {
            return;
        }

        StartCoroutine(CreateNewGameRoutine());
    }

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
        GrantAllToolsAndWeapons();
        StartGame();
        ForestSaveSystem.SaveGame();
        startingNewGame = false;
    }

    public void ContinueGame()
    {
        ForestSaveSystem.LoadGame();
        GrantAllToolsAndWeapons();
        StartGame();
        ForestSaveSystem.SaveGame();
    }

    private static void GrantAllToolsAndWeapons()
    {
        PlayerToolController player = FindObjectOfType<PlayerToolController>();
        if (player != null)
        {
            player.EnsureAllToolsAndWeaponsInInventory();
        }
    }

    private void ShowDeathMenu()
    {
        deathShown = true;
        SetPanelActive(deathPanel, true);
        SetPanelActive(startPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
        SetPaused(true);
    }

    public void ShowInstructionPanel()
    {
        SetPanelActive(instructionPanel, true);
    }

    public void HideInstructionPanel()
    {
        SetPanelActive(instructionPanel, false);
    }

    public void HideOverwritePanel()
    {
        SetPanelActive(overwritePanel, false);
    }

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

    private void HideSecondaryPanels()
    {
        SetPanelActive(deathPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
    }

    private void HideAllPanels()
    {
        SetPanelActive(startPanel, false);
        SetPanelActive(deathPanel, false);
        SetPanelActive(instructionPanel, false);
        SetPanelActive(overwritePanel, false);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static void SetPaused(bool paused)
    {
        IsMenuOpen = paused;
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
        SetGameplayHudVisible(!paused);
    }

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

    private void ShowHudCanvas(string canvasName)
    {
        GameObject canvas = FindHudCanvas(canvasName);
        if (canvas != null)
        {
            canvas.SetActive(true);
        }
    }

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





