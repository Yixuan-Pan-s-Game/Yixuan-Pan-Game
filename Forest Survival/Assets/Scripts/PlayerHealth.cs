using UnityEngine;

// player health script that owns this feature's runtime behavior.
public class PlayerHealth : MonoBehaviour
{
    // Gameplay stat that affects damage, health, healing, defense, or durability: maxHealth.
    public int maxHealth = 100;
    // Gameplay stat that affects damage, health, healing, defense, or durability: currentHealth.
    public int currentHealth = 100;
    // Gameplay stat that affects damage, health, healing, defense, or durability: temporaryDefense.
    public int temporaryDefense;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: temporaryDefenseUntil.
    public float temporaryDefenseUntil;

    // Important runtime data or configuration used by this component: deathNotified.
    private bool deathNotified;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
        deathNotified = currentHealth <= 0;
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (temporaryDefense > 0 && Time.time >= temporaryDefenseUntil)
        {
            temporaryDefense = 0;
        }

        if (currentHealth > 0)
        {
            deathNotified = false;
        }
        else if (!deathNotified)
        {
            deathNotified = true;
            ForestMenuUI.NotifyPlayerDied(this);
        }
    }

    // Handles the heal workflow.
    public bool Heal(int amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth)
        {
            return false;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        return true;
    }

    // Handles the take damage workflow.
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
        {
            return 0;
        }

        int defense = temporaryDefense;
        PlayerToolController inventory = GetComponent<PlayerToolController>();
        if (inventory != null)
        {
            defense += inventory.GetEquippedDefense();
        }

        int finalDamage = Mathf.Max(1, amount - Mathf.Max(0, defense));
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        if (currentHealth <= 0 && !deathNotified)
        {
            deathNotified = true;
            ForestMenuUI.NotifyPlayerDied(this);
        }

        return finalDamage;
    }

    // Adds, spawns, or attaches the objects and data for add temporary defense.
    public void AddTemporaryDefense(int amount, float seconds)
    {
        temporaryDefense = Mathf.Max(temporaryDefense, amount);
        temporaryDefenseUntil = Mathf.Max(temporaryDefenseUntil, Time.time + Mathf.Max(0f, seconds));
    }
}
