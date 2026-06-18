using UnityEngine;

// Enemy behavior controller that handles targeting, movement, attacks, hit timing, and death flow.
public class EnemyHealth : MonoBehaviour
{
    // Gameplay stat that affects damage, health, healing, defense, or durability: maxHealth.
    public int maxHealth = 100;
    // Gameplay stat that affects damage, health, healing, defense, or durability: currentHealth.
    public int currentHealth = 100;
    // Important runtime data or configuration used by this component: destroyOnDeath.
    public bool destroyOnDeath = false;
    // Gameplay stat that affects damage, health, healing, defense, or durability: showHealthBar.
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0f, 2.35f, 0f);
    public Vector2 healthBarSize = new Vector2(78f, 10f);
    // Distance or radius used for detection, interaction, or physics checks: healthBarVisibleDistance.
    public float healthBarVisibleDistance = 35f;

    // Runtime flag that drives control flow, UI state, or gameplay availability: dead.
    private bool dead;

    // Read-only state exposed to other systems: IsDead.
    public bool IsDead => dead || currentHealth <= 0;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
    }

    // Handles the take damage workflow.
    public bool TakeDamage(int amount, GameObject attacker = null)
    {
        if (amount <= 0 || IsDead)
        {
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth > 0)
        {
            return true;
        }

        Die(attacker);
        return true;
    }

    // Handles the die workflow.
    private void Die(GameObject attacker)
    {
        dead = true;

        SendMessage("OnEnemyDied", attacker, SendMessageOptions.DontRequireReceiver);

        if (destroyOnDeath)
        {
            Destroy(gameObject, 2f);
        }
    }

    // Unity lifecycle: draws immediate-mode HUD or debug information.
    private void OnGUI()
    {
        if (!showHealthBar || IsDead || Camera.main == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (Vector3.Distance(camera.transform.position, transform.position) > healthBarVisibleDistance)
        {
            return;
        }

        Vector3 screenPoint = camera.WorldToScreenPoint(transform.position + healthBarOffset);
        if (screenPoint.z <= 0f)
        {
            return;
        }

        DrawHealthBar(screenPoint);
    }

    // Handles the draw health bar workflow.
    private void DrawHealthBar(Vector3 screenPoint)
    {
        float width = Mathf.Max(24f, healthBarSize.x);
        float height = Mathf.Max(6f, healthBarSize.y);
        float x = screenPoint.x - width * 0.5f;
        float y = Screen.height - screenPoint.y;
        float fill = Mathf.Clamp01(currentHealth / (float)Mathf.Max(1, maxHealth));

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(x - 1f, y - 1f, width + 2f, height + 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0.55f, 0.06f, 0.04f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = new Color(0.15f, 0.85f, 0.2f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, width * fill, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x - 8f, y - 17f, width + 16f, 18f), currentHealth + "/" + maxHealth);
        GUI.color = oldColor;
    }
}
