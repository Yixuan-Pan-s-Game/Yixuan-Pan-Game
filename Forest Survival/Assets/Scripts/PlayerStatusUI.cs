using UnityEngine;

// player status ui script that owns this feature's runtime behavior.
public class PlayerStatusUI : MonoBehaviour
{
    public Vector2 position = new Vector2(-18f, 18f);
    public Vector2 size = new Vector2(560f, 196f);
    // Runtime flag that drives control flow, UI state, or gameplay availability: showWhenBackpackOpen.
    public bool showWhenBackpackOpen = true;

    // Important runtime data or configuration used by this component: instance.
    private static PlayerStatusUI instance;

    // Gameplay stat that affects damage, health, healing, defense, or durability: health.
    private PlayerHealth health;
    // Inventory or crafting data for items, recipes, slots, or stack counts: inventory.
    private PlayerToolController inventory;
    // Important runtime data or configuration used by this component: labelStyle.
    private GUIStyle labelStyle;
    // Important runtime data or configuration used by this component: titleStyle.
    private GUIStyle titleStyle;
    // Asset reference used for spawning, rendering, audio, or animation: frameTexture.
    private Texture2D frameTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Ensures the objects, references, or configuration required for ensure status uiexists exist.
    private static void EnsureStatusUIExists()
    {
        if (FindObjectOfType<PlayerStatusUI>() != null)
        {
            return;
        }

        new GameObject("PlayerStatusUI").AddComponent<PlayerStatusUI>();
    }

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
    }
    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (health == null)
        {
            health = FindObjectOfType<PlayerHealth>();
        }

        if (inventory == null)
        {
            inventory = FindObjectOfType<PlayerToolController>();
        }
    }

    // Unity lifecycle: draws immediate-mode HUD or debug information.
    private void OnGUI()
    {
        if (ForestMenuUI.IsMenuOpen)
        {
            return;
        }

        if (health == null)
        {
            health = FindObjectOfType<PlayerHealth>();
        }

        if (inventory == null)
        {
            inventory = FindObjectOfType<PlayerToolController>();
        }

        if (!showWhenBackpackOpen && BackpackUI.IsAnyOpen)
        {
            return;
        }

        EnsureStyles();

        int currentHealth = health != null ? health.currentHealth : 0;
        int maxHealth = health != null ? health.maxHealth : 100;
        GetArmorStats(out int armorValue, out int armorDurability, out int armorMaxDurability);

        Rect panel = new Rect(position.x, position.y, size.x, size.y);
        DrawPanel(panel);
        GUI.Label(new Rect(position.x + 62f, position.y + 30f, size.x - 104f, 30f), "SURVIVOR", titleStyle);
        DrawBar(new Rect(position.x + 62f, position.y + 68f, 360f, 22f), currentHealth / (float)Mathf.Max(1, maxHealth), new Color(0.8f, 0.12f, 0.08f, 1f));
        GUI.Label(new Rect(position.x + 66f, position.y + 66f, size.x - 132f, 26f), "Health  " + currentHealth + " / " + maxHealth, labelStyle);
        GUI.Label(new Rect(position.x + 62f, position.y + 101f, size.x - 104f, 26f), "Armor  " + armorValue, labelStyle);
        GUI.Label(new Rect(position.x + 62f, position.y + 128f, size.x - 104f, 26f), "Armor Durability  " + armorDurability + " / " + armorMaxDurability, labelStyle);
    }

    // Calculates and returns the result for get armor stats.
    private void GetArmorStats(out int armorValue, out int armorDurability, out int armorMaxDurability)
    {
        armorValue = 0;
        armorDurability = 0;
        armorMaxDurability = 0;

        if (inventory == null)
        {
            return;
        }

        armorValue += inventory.GetEquippedDefense();
        inventory.GetEquippedArmorDurability(out armorDurability, out armorMaxDurability);

        if (health != null && health.temporaryDefense > 0)
        {
            armorValue += health.temporaryDefense;
        }
    }

    // Ensures the objects, references, or configuration required for ensure styles exist.
    private void EnsureStyles()
    {
        if (labelStyle != null && titleStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = Color.white }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.78f, 1f, 0.66f, 1f) }
        };
    }

    // Handles the draw panel workflow.
    private void DrawPanel(Rect rect)
    {
        Color old = GUI.color;
        if (frameTexture == null)
        {
            frameTexture = Resources.Load<Texture2D>("frame");
        }

        GUI.color = Color.white;
        GUI.DrawTexture(rect, frameTexture != null ? frameTexture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        GUI.color = old;
    }

    // Handles the draw bar workflow.
    private static void DrawBar(Rect rect, float fill, Color color)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}

