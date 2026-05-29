using UnityEngine;

public class PlayerStatusUI : MonoBehaviour
{
    public Vector2 position = new Vector2(16f, 16f);
    public Vector2 size = new Vector2(300f, 104f);
    public bool showWhenBackpackOpen = true;

    private PlayerHealth health;
    private PlayerToolController inventory;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureStatusUIExists()
    {
        if (FindObjectOfType<PlayerStatusUI>() != null)
        {
            return;
        }

        new GameObject("PlayerStatusUI").AddComponent<PlayerStatusUI>();
    }

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

    private void OnGUI()
    {
        if (!showWhenBackpackOpen && BackpackUI.IsAnyOpen)
        {
            return;
        }

        EnsureStyles();

        int currentHealth = health != null ? health.currentHealth : 0;
        int maxHealth = health != null ? health.maxHealth : 100;
        GetArmorStats(out int armorValue, out int armorDurability, out int armorMaxDurability);

        GUI.color = new Color(1f, 1f, 1f, 0.95f);
        GUI.Box(new Rect(position.x, position.y, size.x, size.y), string.Empty);
        GUI.Label(new Rect(position.x + 12f, position.y + 8f, size.x - 24f, 22f), "Player Status", titleStyle);
        GUI.Label(new Rect(position.x + 12f, position.y + 34f, size.x - 24f, 20f), "Health: " + currentHealth + " / " + maxHealth, labelStyle);
        GUI.Label(new Rect(position.x + 12f, position.y + 56f, size.x - 24f, 20f), "Armor: " + armorValue, labelStyle);
        GUI.Label(new Rect(position.x + 12f, position.y + 78f, size.x - 24f, 20f), "Armor Durability: " + armorDurability + " / " + armorMaxDurability, labelStyle);
    }

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

    private void EnsureStyles()
    {
        if (labelStyle != null && titleStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = Color.white }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }
}
