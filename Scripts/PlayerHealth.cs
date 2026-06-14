using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int temporaryDefense;
    public float temporaryDefenseUntil;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
    }

    private void Update()
    {
        if (temporaryDefense > 0 && Time.time >= temporaryDefenseUntil)
        {
            temporaryDefense = 0;
        }
    }

    public bool Heal(int amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth)
        {
            return false;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        return true;
    }

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
        return finalDamage;
    }

    public void AddTemporaryDefense(int amount, float seconds)
    {
        temporaryDefense = Mathf.Max(temporaryDefense, amount);
        temporaryDefenseUntil = Mathf.Max(temporaryDefenseUntil, Time.time + Mathf.Max(0f, seconds));
    }
}
