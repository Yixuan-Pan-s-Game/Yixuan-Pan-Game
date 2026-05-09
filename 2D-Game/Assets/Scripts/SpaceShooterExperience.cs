using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpaceShooterExperience : MonoBehaviour
{
    private enum GameState { Menu, LevelSelect, Playing, Paused, Instructions, GameOver, Victory }

    private class LevelConfig
    {
        public string name;
        public string description;
        public string backgroundPath;
        public string enemyPath;
        public string enemySpriteName;
        public string planetPath;
        public string asteroidPath;
        public int targetKills;
        public int asteroidCount;
        public float baseSpawnDelay;
        public float enemySpeedBonus;
        public Color messageColor;
    }

    private static SpaceShooterExperience instance;

    private GameState state = GameState.Menu;
    private Camera mainCamera;
    private Canvas canvas;
    private GameObject menuPage;
    private GameObject levelSelectPage;
    private GameObject instructionsPage;
    private GameObject hudPage;
    private GameObject pausePage;
    private GameObject gameOverPage;
    private GameObject victoryPage;
    private Text scoreText;
    private Text livesText;
    private Text timerText;
    private Text waveText;
    private Text objectiveText;
    private Text messageText;
    private Text finalText;
    private Transform worldRoot;
    private Transform bulletRoot;
    private AudioSource sfxSource;
    private AudioSource musicSource;
    private AudioClip playerFireClip;
    private AudioClip enemyFireClip;
    private AudioClip enemyHitClip;
    private AudioClip enemyExplodeClip;
    private AudioClip playerHitClip;
    private AudioClip gameOverClip;
    private AudioClip gameWinClip;
    private AudioClip pauseClip;
    private AudioClip menuMusicClip;
    private AudioClip levelMusicClip;
    private SpacePlayer player;
    private Sprite squareSprite;
    private Sprite circleSprite;
    private Sprite shipSprite;
    private Sprite enemySprite;
    private Sprite backgroundSprite;
    private Sprite playerProjectileSprite;
    private Sprite enemyProjectileSprite;
    private Sprite asteroidSprite;
    private Sprite planetSprite;
    private LevelConfig[] levels;
    private LevelConfig currentLevel;
    private int selectedLevelIndex;
    private float elapsedTime;
    private float nextSpawnTime;
    private float messageClearTime;
    private int score;
    private int highScore;
    private int enemiesDefeated;
    private int wave = 1;
    private int targetKills = 20;
    private float baseSpawnDelay = 1.6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject("SpaceShooterExperience");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<SpaceShooterExperience>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Start()
    {
        BuildExperience();
        ShowMenu();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildExperience();
        ShowMenu();
    }

    private void Update()
    {
        if (state == GameState.Playing)
        {
            elapsedTime += Time.deltaTime;
            wave = 1 + Mathf.FloorToInt(score / 100f);
            SpawnEnemies();
            UpdateHud();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowPause();
            }

            if (enemiesDefeated >= targetKills)
            {
                ShowVictory();
            }
        }
        else if (state == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeGame();
        }

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime > messageClearTime)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void BuildExperience()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5.5f;
        mainCamera.transform.position = new Vector3(0, 0, -10);
        mainCamera.backgroundColor = new Color(0.015f, 0.02f, 0.045f);

        GameObject oldPreviewEffect = GameObject.Find("Player Explosion_0");
        if (oldPreviewEffect != null)
        {
            Destroy(oldPreviewEffect);
        }

        CreateSprites();
        LoadAudio();
        SetupAudioSources();
        RebuildWorldForCurrentLevel();
        CreateBackground();
        BuildCanvas();
        EnsureEventSystem();
    }

    private void RebuildWorldForCurrentLevel()
    {
        if (worldRoot != null)
        {
            Destroy(worldRoot.gameObject);
        }

        worldRoot = new GameObject("Generated Space Shooter World").transform;
        bulletRoot = new GameObject("Projectiles").transform;
        bulletRoot.SetParent(worldRoot);
    }

    private void CreateSprites()
    {
        squareSprite = MakeSprite(16, 16, (x, y) => Color.white);
        circleSprite = MakeSprite(32, 32, (x, y) =>
        {
            Vector2 center = new Vector2(15.5f, 15.5f);
            float distance = Vector2.Distance(new Vector2(x, y), center);
            return distance <= 15f ? Color.white : Color.clear;
        });
        shipSprite = MakeSprite(32, 40, (x, y) =>
        {
            float halfWidth = Mathf.Lerp(15f, 3f, y / 39f);
            bool inside = Mathf.Abs(x - 15.5f) <= halfWidth && y < 38;
            bool cockpit = inside && y > 19 && Mathf.Abs(x - 15.5f) < 4f;
            if (cockpit) return new Color(0.25f, 0.95f, 1f, 1f);
            return inside ? new Color(0.95f, 0.96f, 0.88f, 1f) : Color.clear;
        });
        enemySprite = MakeSprite(36, 36, (x, y) =>
        {
            Vector2 center = new Vector2(17.5f, 17.5f);
            float distance = Vector2.Distance(new Vector2(x, y), center);
            bool ring = distance < 17f && distance > 5f;
            bool core = distance <= 6f;
            if (core) return new Color(1f, 0.35f, 0.35f, 1f);
            return ring ? new Color(0.95f, 0.62f, 0.18f, 1f) : Color.clear;
        });

        playerProjectileSprite = LoadSpriteFromProject("Assets/Art/Projectiles/Player Projectile/Player_Projectile.png", "Player_Projectile_A", circleSprite);
        enemyProjectileSprite = LoadSpriteFromProject("Assets/Art/Projectiles/Enemy Projectiles/Straight Projectile/Enemy_StraightProjectile.png", "Enemy_StraightProjectile_A", circleSprite);
        shipSprite = LoadSpriteFromProject("Assets/Art/Player/Player Sprites.png", "Player Sprites_0", shipSprite);
        EnsureLevels();
        ApplyLevelVisuals(selectedLevelIndex);
    }

    private void EnsureLevels()
    {
        if (levels != null)
        {
            return;
        }

        levels = new LevelConfig[]
        {
            new LevelConfig
            {
                name = "Level 1: Patrol",
                description = "Balanced opening sector. Clear 16 enemies.",
                backgroundPath = "Assets/Art/Environment/Background/A_CompleteSpaceBackground.png",
                enemyPath = "Assets/Art/Enemies/Chaser/Chaser Sprites.png",
                enemySpriteName = "Chaser Sprites_0",
                planetPath = "Assets/Art/Environment/Planets/Medium/Medium_BluePlanet.png",
                asteroidPath = "Assets/Art/Environment/Asteroids/Asteroid_1.png",
                targetKills = 16,
                asteroidCount = 5,
                baseSpawnDelay = 1.65f,
                enemySpeedBonus = 0f,
                messageColor = new Color(0.76f, 0.9f, 1f)
            },
            new LevelConfig
            {
                name = "Level 2: Nebula",
                description = "Dense star clouds. Faster enemies, 22 kills.",
                backgroundPath = "Assets/Art/Environment/Nebula/B_NebulaWithStars.png",
                enemyPath = "Assets/Art/Enemies/Diagonal Shooter/DShoot Sprites.png",
                enemySpriteName = "DShoot Sprites_0",
                planetPath = "Assets/Art/Environment/Planets/Small/Small_GreenPlanet.png",
                asteroidPath = "Assets/Art/Environment/Asteroids/Asteroid_2.png",
                targetKills = 22,
                asteroidCount = 8,
                baseSpawnDelay = 1.35f,
                enemySpeedBonus = 0.25f,
                messageColor = new Color(0.95f, 0.82f, 1f)
            },
            new LevelConfig
            {
                name = "Level 3: Asteroid Field",
                description = "High pressure sector. Stronger waves, 28 kills.",
                backgroundPath = "Assets/Art/Environment/Background/D_CompleteSpaceBackground.png",
                enemyPath = "Assets/Art/Enemies/Straight Shooter/SShoot Sprites.png",
                enemySpriteName = "SShoot Sprites_0",
                planetPath = "Assets/Art/Environment/Planets/Big/BigRedPlanet.png",
                asteroidPath = "Assets/Art/Environment/Asteroids/Asteroid_4.png",
                targetKills = 28,
                asteroidCount = 12,
                baseSpawnDelay = 1.1f,
                enemySpeedBonus = 0.45f,
                messageColor = new Color(1f, 0.86f, 0.68f)
            }
        };
    }

    private void ApplyLevelVisuals(int levelIndex)
    {
        EnsureLevels();
        selectedLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
        currentLevel = levels[selectedLevelIndex];
        enemySprite = LoadSpriteFromProject(currentLevel.enemyPath, currentLevel.enemySpriteName, enemySprite);
        backgroundSprite = LoadSpriteFromProject(currentLevel.backgroundPath, "", null);
        asteroidSprite = LoadSpriteFromProject(currentLevel.asteroidPath, "", null);
        planetSprite = LoadSpriteFromProject(currentLevel.planetPath, "", null);
        targetKills = currentLevel.targetKills;
        baseSpawnDelay = currentLevel.baseSpawnDelay;
    }

    private void LoadAudio()
    {
        playerFireClip = LoadAudioFromProject("Assets/Audio/Sound Effects/PlayerFire.wav", playerFireClip);
        enemyFireClip = LoadAudioFromProject("Assets/Audio/Sound Effects/EnemyFire.wav", enemyFireClip);
        enemyHitClip = LoadAudioFromProject("Assets/Audio/Sound Effects/EnemyHit.wav", enemyHitClip);
        enemyExplodeClip = LoadAudioFromProject("Assets/Audio/Sound Effects/EnemyExplode.wav", enemyExplodeClip);
        playerHitClip = LoadAudioFromProject("Assets/Audio/Sound Effects/PlayerHit.wav", playerHitClip);
        gameOverClip = LoadAudioFromProject("Assets/Audio/Sound Effects/GameOver.wav", gameOverClip);
        gameWinClip = LoadAudioFromProject("Assets/Audio/Sound Effects/GameWin.wav", gameWinClip);
        pauseClip = LoadAudioFromProject("Assets/Audio/Sound Effects/PauseMenu.wav", pauseClip);
        menuMusicClip = LoadAudioFromProject("Assets/Audio/Music/Menu.wav", menuMusicClip);
        levelMusicClip = LoadAudioFromProject("Assets/Audio/Music/SongA.wav", levelMusicClip);
    }

    private AudioClip LoadAudioFromProject(string assetPath, AudioClip fallback)
    {
#if UNITY_EDITOR
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip != null)
        {
            return clip;
        }
#endif
        return fallback;
    }

    private void SetupAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.75f;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.28f;
        }
    }

    private void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    private Sprite LoadSpriteFromProject(string assetPath, string spriteName, Sprite fallback)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(spriteName))
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (UnityEngine.Object asset in assets)
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
        }

        Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (singleSprite != null)
        {
            return singleSprite;
        }
#endif
        return fallback;
    }

    private Sprite MakeSprite(int width, int height, System.Func<int, int, Color> colorAt)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, colorAt(x, y));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
    }

    private void CreateBackground()
    {
        GameObject backdrop = CreateSpriteObject("Starfield Backdrop", backgroundSprite != null ? backgroundSprite : squareSprite, Color.white, new Vector3(0, 0, 3), backgroundSprite != null ? new Vector3(4.8f, 4.8f, 1) : new Vector3(24, 16, 1));
        backdrop.transform.SetParent(worldRoot);

        if (planetSprite != null)
        {
            GameObject planet = CreateSpriteObject("Distant Planet", planetSprite, new Color(0.75f, 0.9f, 1f, 0.85f), new Vector3(6.5f, 3.2f, 2), Vector3.one * 1.35f);
            planet.transform.SetParent(worldRoot);
        }

        if (asteroidSprite != null)
        {
            int asteroidCount = currentLevel != null ? currentLevel.asteroidCount : 7;
            for (int i = 0; i < asteroidCount; i++)
            {
                GameObject asteroid = CreateSpriteObject("Asteroid", asteroidSprite, Color.white,
                    new Vector3(Random.Range(-8.5f, 8.5f), Random.Range(-5f, 5f), 1), Vector3.one * Random.Range(0.35f, 0.7f));
                asteroid.transform.SetParent(worldRoot);
            }
        }

        for (int i = 0; i < 90; i++)
        {
            float size = Random.Range(0.025f, 0.06f);
            GameObject star = CreateSpriteObject("Star", circleSprite, new Color(0.6f, 0.85f, 1f, Random.Range(0.35f, 0.9f)),
                new Vector3(Random.Range(-9f, 9f), Random.Range(-5.2f, 5.2f), 2), Vector3.one * size);
            star.transform.SetParent(worldRoot);
        }
    }

    private GameObject CreateSpriteObject(string objectName, Sprite sprite, Color color, Vector3 position, Vector3 scale)
    {
        GameObject created = new GameObject(objectName);
        created.transform.position = position;
        created.transform.localScale = scale;
        SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        return created;
    }

    private void BuildCanvas()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }

        GameObject canvasObject = new GameObject("Space Shooter UI");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        canvasObject.AddComponent<GraphicRaycaster>();

        menuPage = CreatePage("Main Menu");
        levelSelectPage = CreatePage("Level Select");
        instructionsPage = CreatePage("Instructions");
        hudPage = CreatePage("HUD");
        pausePage = CreatePage("Pause");
        gameOverPage = CreatePage("Game Over");
        victoryPage = CreatePage("Victory");

        BuildMenu();
        BuildLevelSelect();
        BuildInstructions();
        BuildHud();
        BuildPause();
        BuildEndPages();
    }

    private GameObject CreatePage(string pageName)
    {
        GameObject page = new GameObject(pageName);
        page.transform.SetParent(canvas.transform, false);
        RectTransform rect = page.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return page;
    }

    private void BuildMenu()
    {
        AddPanel(menuPage.transform, new Color(0.02f, 0.03f, 0.08f, 0.82f));
        AddText(menuPage.transform, "STAR MOUSE STRIKE", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.72f), new Vector2(760, 70), Color.white);
        AddText(menuPage.transform, "Follow the cursor, survive the swarm, clear each sector.", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(760, 44), new Color(0.76f, 0.9f, 1f));
        AddButton(menuPage.transform, "New Game", new Vector2(0.5f, 0.48f), ShowLevelSelect);
        AddButton(menuPage.transform, "Instructions", new Vector2(0.5f, 0.36f), ShowInstructions);
        AddButton(menuPage.transform, "Exit", new Vector2(0.5f, 0.24f), ExitGame);
    }

    private void BuildLevelSelect()
    {
        EnsureLevels();
        AddPanel(levelSelectPage.transform, new Color(0.02f, 0.03f, 0.08f, 0.9f));
        AddText(levelSelectPage.transform, "Select Level", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.78f), new Vector2(620, 60), Color.white);
        AddText(levelSelectPage.transform, levels[0].description, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.65f), new Vector2(720, 40), levels[0].messageColor);
        AddButton(levelSelectPage.transform, "Level 1: Patrol", new Vector2(0.5f, 0.54f), StartLevelOne);
        AddText(levelSelectPage.transform, levels[1].description, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.44f), new Vector2(720, 40), levels[1].messageColor);
        AddButton(levelSelectPage.transform, "Level 2: Nebula", new Vector2(0.5f, 0.34f), StartLevelTwo);
        AddText(levelSelectPage.transform, levels[2].description, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.24f), new Vector2(720, 40), levels[2].messageColor);
        AddButton(levelSelectPage.transform, "Level 3: Asteroids", new Vector2(0.5f, 0.14f), StartLevelThree);
        AddButton(levelSelectPage.transform, "Back", new Vector2(0.18f, 0.10f), ShowMenu);
    }

    private void BuildInstructions()
    {
        AddPanel(instructionsPage.transform, new Color(0.02f, 0.03f, 0.08f, 0.9f));
        AddText(instructionsPage.transform, "Instructions", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.74f), new Vector2(620, 60), Color.white);
        AddText(instructionsPage.transform,
            "Move: ship follows your mouse cursor\nFire: hold left mouse button\nPause: Esc\nGoal: choose a level and destroy its target number of enemies\nPower-ups: blue boosts fire rate, green restores life",
            24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.52f), new Vector2(840, 210), new Color(0.86f, 0.96f, 1f));
        AddButton(instructionsPage.transform, "Back", new Vector2(0.5f, 0.24f), ShowMenu);
    }

    private void BuildHud()
    {
        scoreText = AddText(hudPage.transform, "Score: 0", 22, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.95f), new Vector2(230, 32), Color.white);
        livesText = AddText(hudPage.transform, "Lives: 3", 22, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.90f), new Vector2(230, 32), Color.white);
        timerText = AddText(hudPage.transform, "Time: 0", 22, TextAnchor.MiddleRight, new Vector2(0.98f, 0.95f), new Vector2(230, 32), Color.white);
        waveText = AddText(hudPage.transform, "Wave: 1", 22, TextAnchor.MiddleRight, new Vector2(0.98f, 0.90f), new Vector2(230, 32), Color.white);
        objectiveText = AddText(hudPage.transform, "Objective: destroy enemies. Follow mouse, hold left click to fire.", 19, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.05f), new Vector2(900, 36), new Color(0.8f, 0.92f, 1f));
        messageText = AddText(hudPage.transform, "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.83f), new Vector2(760, 44), new Color(1f, 0.92f, 0.45f));
        messageText.gameObject.SetActive(false);
    }

    private void BuildPause()
    {
        AddPanel(pausePage.transform, new Color(0.02f, 0.03f, 0.08f, 0.86f));
        AddText(pausePage.transform, "Paused", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.66f), new Vector2(520, 62), Color.white);
        AddButton(pausePage.transform, "Resume", new Vector2(0.5f, 0.50f), ResumeGame);
        AddButton(pausePage.transform, "Retry", new Vector2(0.5f, 0.38f), StartNewGame);
        AddButton(pausePage.transform, "Back", new Vector2(0.5f, 0.26f), ShowMenu);
    }

    private void BuildEndPages()
    {
        AddPanel(gameOverPage.transform, new Color(0.10f, 0.02f, 0.03f, 0.88f));
        AddText(gameOverPage.transform, "Game Over", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.66f), new Vector2(520, 62), Color.white);
        finalText = AddText(gameOverPage.transform, "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.54f), new Vector2(700, 44), new Color(1f, 0.86f, 0.76f));
        AddButton(gameOverPage.transform, "Retry", new Vector2(0.5f, 0.38f), StartNewGame);
        AddButton(gameOverPage.transform, "Back", new Vector2(0.5f, 0.26f), ShowMenu);

        AddPanel(victoryPage.transform, new Color(0.02f, 0.08f, 0.06f, 0.88f));
        AddText(victoryPage.transform, "Victory", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.66f), new Vector2(520, 62), Color.white);
        AddText(victoryPage.transform, "Sector cleared. The swarm is broken.", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.54f), new Vector2(700, 44), new Color(0.8f, 1f, 0.86f));
        AddButton(victoryPage.transform, "Retry", new Vector2(0.5f, 0.38f), StartNewGame);
        AddButton(victoryPage.transform, "Back", new Vector2(0.5f, 0.26f), ShowMenu);
    }

    private void AddPanel(Transform parent, Color color)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = color;
    }

    private Text AddText(Transform parent, string text, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject textObject = new GameObject(text.Length > 18 ? "Text" : text);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        Text label = textObject.AddComponent<Text>();
        label.text = text;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private void AddButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260, 54);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.28f, 0.96f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.14f, 0.34f, 0.50f, 1f);
        colors.pressedColor = new Color(0.95f, 0.62f, 0.18f, 1f);
        button.colors = colors;

        AddText(buttonObject.transform, label, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(250, 48), Color.white);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    private void SetPage(GameObject activePage)
    {
        menuPage.SetActive(activePage == menuPage);
        levelSelectPage.SetActive(activePage == levelSelectPage);
        instructionsPage.SetActive(activePage == instructionsPage);
        hudPage.SetActive(activePage == hudPage || activePage == pausePage);
        pausePage.SetActive(activePage == pausePage);
        gameOverPage.SetActive(activePage == gameOverPage);
        victoryPage.SetActive(activePage == victoryPage);
    }

    private void ShowMenu()
    {
        ClearRunObjects();
        Time.timeScale = 0f;
        state = GameState.Menu;
        PlayMusic(menuMusicClip);
        SetPage(menuPage);
    }

    private void ShowInstructions()
    {
        state = GameState.Instructions;
        SetPage(instructionsPage);
    }

    private void ShowLevelSelect()
    {
        state = GameState.LevelSelect;
        SetPage(levelSelectPage);
    }

    private void StartLevelOne()
    {
        selectedLevelIndex = 0;
        StartNewGame();
    }

    private void StartLevelTwo()
    {
        selectedLevelIndex = 1;
        StartNewGame();
    }

    private void StartLevelThree()
    {
        selectedLevelIndex = 2;
        StartNewGame();
    }

    private void StartNewGame()
    {
        ClearRunObjects();
        ApplyLevelVisuals(selectedLevelIndex);
        RebuildWorldForCurrentLevel();
        CreateBackground();
        Time.timeScale = 1f;
        state = GameState.Playing;
        PlayMusic(levelMusicClip);
        SetPage(hudPage);
        score = 0;
        enemiesDefeated = 0;
        wave = 1;
        elapsedTime = 0;
        nextSpawnTime = Time.time + 0.7f;
        highScore = PlayerPrefs.GetInt("SpaceShooterHighScore", 0);

        GameObject playerObject = CreateSpriteObject("Player", shipSprite, Color.white, Vector3.zero, Vector3.one * 0.9f);
        playerObject.transform.SetParent(worldRoot);
        playerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;
        player = playerObject.AddComponent<SpacePlayer>();
        player.Setup(this, bulletRoot, playerProjectileSprite);
        objectiveText.text = "Objective: destroy " + targetKills + " enemies. Follow mouse, hold left click to fire.";
        ShowMessage(currentLevel.name + ": destroy " + targetKills + " enemies");
        UpdateHud();
    }

    private void ClearRunObjects()
    {
        if (worldRoot == null)
        {
            return;
        }

        foreach (SpaceEnemy enemy in FindObjectsOfType<SpaceEnemy>())
        {
            Destroy(enemy.gameObject);
        }
        foreach (SpaceProjectile projectile in FindObjectsOfType<SpaceProjectile>())
        {
            Destroy(projectile.gameObject);
        }
        foreach (SpacePowerUp powerUp in FindObjectsOfType<SpacePowerUp>())
        {
            Destroy(powerUp.gameObject);
        }
        if (player != null)
        {
            Destroy(player.gameObject);
            player = null;
        }
    }

    private void SpawnEnemies()
    {
        if (Time.time < nextSpawnTime || player == null)
        {
            return;
        }

        float spawnDelay = Mathf.Max(0.38f, baseSpawnDelay - wave * 0.12f);
        nextSpawnTime = Time.time + spawnDelay;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 spawnPosition = new Vector3(Mathf.Cos(angle) * 8.5f, Mathf.Sin(angle) * 5.4f, 0);

        GameObject enemyObject = CreateSpriteObject("Enemy", enemySprite, Color.white, spawnPosition, Vector3.one * Random.Range(0.75f, 1.05f));
        enemyObject.transform.SetParent(worldRoot);
        enemyObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D collider = enemyObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;
        SpaceEnemy enemy = enemyObject.AddComponent<SpaceEnemy>();
        float levelSpeedBonus = currentLevel != null ? currentLevel.enemySpeedBonus : 0f;
        enemy.Setup(this, player.transform, enemyProjectileSprite, 1.2f + levelSpeedBonus + wave * 0.18f, 1 + Mathf.FloorToInt(wave / 3f));
    }

    private void ShowPause()
    {
        Time.timeScale = 0f;
        state = GameState.Paused;
        PlayClip(pauseClip, 0.9f);
        SetPage(pausePage);
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        state = GameState.Playing;
        PlayClip(pauseClip, 0.65f);
        SetPage(hudPage);
    }

    private void ShowGameOver()
    {
        Time.timeScale = 0f;
        state = GameState.GameOver;
        SaveHighScore();
        PlayClip(gameOverClip, 1f);
        finalText.text = "Score: " + score + "   High: " + highScore;
        SetPage(gameOverPage);
    }

    private void ShowVictory()
    {
        Time.timeScale = 0f;
        state = GameState.Victory;
        SaveHighScore();
        PlayClip(gameWinClip, 1f);
        SetPage(victoryPage);
    }

    private void SaveHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("SpaceShooterHighScore", highScore);
        }
    }

    private void UpdateHud()
    {
        if (player == null)
        {
            return;
        }

        scoreText.text = "Score: " + score + " / " + (targetKills * 10);
        livesText.text = "Lives: " + player.Lives;
        timerText.text = "Time: " + Mathf.FloorToInt(elapsedTime);
        waveText.text = "Wave: " + wave;
    }

    private void ShowMessage(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        messageClearTime = Time.unscaledTime + 2.0f;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateHud();
    }

    public void EnemyDestroyed(Vector3 position)
    {
        enemiesDefeated++;
        AddScore(10);
        PlayClip(enemyExplodeClip, 0.85f);
        StartCoroutine(FlashAt(position, new Color(1f, 0.55f, 0.16f), 0.36f));

        if (Random.value < 0.18f)
        {
            SpawnPowerUp(position);
        }

        if (enemiesDefeated % 5 == 0)
        {
            ShowMessage("Wave " + wave + ": enemies are faster now");
        }
    }

    private void SpawnPowerUp(Vector3 position)
    {
        bool heal = Random.value > 0.5f;
        GameObject powerObject = CreateSpriteObject(heal ? "Repair Power-up" : "Rapid Fire Power-up", circleSprite,
            heal ? new Color(0.25f, 1f, 0.45f) : new Color(0.25f, 0.85f, 1f), position, Vector3.one * 0.34f);
        powerObject.transform.SetParent(worldRoot);
        powerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D collider = powerObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        powerObject.AddComponent<SpacePowerUp>().Setup(this, heal);
    }

    public void PlayerHit()
    {
        if (player == null)
        {
            return;
        }

        StartCoroutine(FlashAt(player.transform.position, new Color(1f, 0.12f, 0.12f), 0.46f));
        PlayClip(playerHitClip, 1f);
        ShowMessage("Hull hit!");
        UpdateHud();

        if (player.Lives <= 0)
        {
            ShowGameOver();
        }
    }

    public void ApplyPowerUp(bool heal)
    {
        if (player == null)
        {
            return;
        }

        if (heal)
        {
            player.Heal();
            ShowMessage("Repair collected: +1 life");
        }
        else
        {
            player.BoostFireRate(5f);
            ShowMessage("Rapid fire online");
        }
        PlayClip(pauseClip, 0.55f);
        UpdateHud();
    }

    public void PlayPlayerFire()
    {
        PlayClip(playerFireClip, 0.65f);
    }

    public void PlayEnemyFire()
    {
        PlayClip(enemyFireClip, 0.35f);
    }

    public void PlayEnemyHit()
    {
        PlayClip(enemyHitClip, 0.75f);
    }

    public void SpawnHitFlash(Vector3 position, Color color)
    {
        StartCoroutine(FlashAt(position, color, 0.2f));
    }

    public void SpawnHitFlash(Vector3 position, Color color, float size)
    {
        StartCoroutine(FlashAt(position, color, size));
    }

    private IEnumerator FlashAt(Vector3 position, Color color, float size)
    {
        GameObject flash = CreateSpriteObject("Hit Flash", circleSprite, color, position, Vector3.one * size);
        flash.transform.SetParent(worldRoot);
        float duration = 0.18f;
        float timer = 0f;
        SpriteRenderer renderer = flash.GetComponent<SpriteRenderer>();
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            flash.transform.localScale += Vector3.one * Time.unscaledDeltaTime * 3f;
            renderer.color = new Color(color.r, color.g, color.b, 1f - timer / duration);
            yield return null;
        }
        Destroy(flash);
    }

    public bool IsPlaying()
    {
        return state == GameState.Playing;
    }
}

public class SpacePlayer : MonoBehaviour
{
    private SpaceShooterExperience game;
    private Transform bulletRoot;
    private Sprite bulletSprite;
    private float baseFireDelay = 0.22f;
    private float currentFireDelay = 0.22f;
    private float nextFireTime;
    private float rapidFireEndTime;
    private float invincibleUntil;
    private SpriteRenderer spriteRenderer;

    public int Lives { get; private set; } = 3;

    public void Setup(SpaceShooterExperience owner, Transform projectileRoot, Sprite projectileSprite)
    {
        game = owner;
        bulletRoot = projectileRoot;
        bulletSprite = projectileSprite;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!game.IsPlaying())
        {
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        transform.position = Vector3.MoveTowards(transform.position, mouseWorld, 7.5f * Time.deltaTime);
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, -8.3f, 8.3f), Mathf.Clamp(transform.position.y, -4.8f, 4.8f), 0);

        Vector3 direction = mouseWorld - transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.up = direction.normalized;
        }

        currentFireDelay = Time.time < rapidFireEndTime ? 0.08f : baseFireDelay;
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Fire();
        }

        if (Time.time < invincibleUntil)
        {
            float blink = Mathf.PingPong(Time.time * 12f, 1f);
            spriteRenderer.color = Color.Lerp(Color.white, new Color(0.4f, 0.9f, 1f), blink);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    private void Fire()
    {
        nextFireTime = Time.time + currentFireDelay;
        GameObject projectile = new GameObject("Player Bullet");
        projectile.transform.position = transform.position + transform.up * 0.48f;
        projectile.transform.rotation = transform.rotation;
        projectile.transform.SetParent(bulletRoot);
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = bulletSprite;
        renderer.color = new Color(0.2f, 0.95f, 1f);
        projectile.transform.localScale = Vector3.one * 0.34f;
        projectile.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.65f;
        projectile.AddComponent<SpaceProjectile>().Setup(game, transform.up, 12f, true);
        game.PlayPlayerFire();
        game.SpawnHitFlash(transform.position + transform.up * 0.32f, new Color(0.2f, 0.95f, 1f), 0.12f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < invincibleUntil)
        {
            return;
        }

        SpaceEnemy enemy = other.GetComponent<SpaceEnemy>();
        SpaceProjectile projectile = other.GetComponent<SpaceProjectile>();
        if (enemy != null || (projectile != null && !projectile.IsPlayerProjectile))
        {
            Lives--;
            invincibleUntil = Time.time + 1.2f;
            if (enemy != null)
            {
                enemy.DestroyEnemy(false);
            }
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
            game.PlayerHit();
        }
    }

    public void Heal()
    {
        Lives = Mathf.Min(5, Lives + 1);
    }

    public void BoostFireRate(float seconds)
    {
        rapidFireEndTime = Time.time + seconds;
    }
}

public class SpaceEnemy : MonoBehaviour
{
    private SpaceShooterExperience game;
    private Transform target;
    private Sprite projectileSprite;
    private float speed;
    private int health;
    private float nextFireTime;

    public void Setup(SpaceShooterExperience owner, Transform followTarget, Sprite bulletSprite, float moveSpeed, int startingHealth)
    {
        game = owner;
        target = followTarget;
        projectileSprite = bulletSprite;
        speed = moveSpeed;
        health = startingHealth;
        nextFireTime = Time.time + Random.Range(0.8f, 1.8f);
    }

    private void Update()
    {
        if (!game.IsPlaying() || target == null)
        {
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.up = -direction;

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + Random.Range(1.3f, 2.1f);
            Fire(direction);
        }
    }

    private void Fire(Vector3 direction)
    {
        GameObject projectile = new GameObject("Enemy Bullet");
        projectile.transform.position = transform.position + direction * 0.45f;
        projectile.transform.up = direction;
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(1f, 0.35f, 0.25f);
        projectile.transform.localScale = Vector3.one * 0.28f;
        projectile.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.62f;
        projectile.AddComponent<SpaceProjectile>().Setup(game, direction, 5.5f, false);
        game.PlayEnemyFire();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SpaceProjectile projectile = other.GetComponent<SpaceProjectile>();
        if (projectile != null && projectile.IsPlayerProjectile)
        {
            health--;
            game.PlayEnemyHit();
            game.SpawnHitFlash(transform.position, new Color(1f, 0.58f, 0.18f), 0.24f);
            Destroy(projectile.gameObject);
            if (health <= 0)
            {
                DestroyEnemy(true);
            }
        }
    }

    public void DestroyEnemy(bool awardScore)
    {
        if (awardScore)
        {
            game.EnemyDestroyed(transform.position);
        }
        else
        {
            game.SpawnHitFlash(transform.position, new Color(1f, 0.35f, 0.2f), 0.32f);
        }
        Destroy(gameObject);
    }
}

public class SpaceProjectile : MonoBehaviour
{
    private SpaceShooterExperience game;
    private Vector3 velocity;
    private float deathTime;

    public bool IsPlayerProjectile { get; private set; }

    public void Setup(SpaceShooterExperience owner, Vector3 direction, float speed, bool fromPlayer)
    {
        game = owner;
        velocity = direction.normalized * speed;
        IsPlayerProjectile = fromPlayer;
        deathTime = Time.time + 3f;
    }

    private void Update()
    {
        if (!game.IsPlaying())
        {
            return;
        }

        transform.position += velocity * Time.deltaTime;
        if (Time.time >= deathTime || Mathf.Abs(transform.position.x) > 10f || Mathf.Abs(transform.position.y) > 7f)
        {
            Destroy(gameObject);
        }
    }
}

public class SpacePowerUp : MonoBehaviour
{
    private SpaceShooterExperience game;
    private bool heal;
    private float deathTime;

    public void Setup(SpaceShooterExperience owner, bool healsPlayer)
    {
        game = owner;
        heal = healsPlayer;
        deathTime = Time.time + 8f;
    }

    private void Update()
    {
        if (!game.IsPlaying())
        {
            return;
        }

        transform.Rotate(Vector3.forward, 180f * Time.deltaTime);
        if (Time.time >= deathTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SpacePlayer>() != null)
        {
            game.ApplyPowerUp(heal);
            Destroy(gameObject);
        }
    }
}
