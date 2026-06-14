using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth = 100;
    public bool destroyOnDeath = false;
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0f, 2.35f, 0f);
    public Vector2 healthBarSize = new Vector2(78f, 10f);
    public float healthBarVisibleDistance = 35f;

    private bool dead;

    public bool IsDead => dead || currentHealth <= 0;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
    }

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

    private void Die(GameObject attacker)
    {
        dead = true;

        // AI：旧敌人 AI 仍然用 OnEnemyDied 接死亡动画；先保留这个兼容点，避免改坏现有怪物。
        SendMessage("OnEnemyDied", attacker, SendMessageOptions.DontRequireReceiver);

        if (destroyOnDeath)
        {
            Destroy(gameObject, 2f);
        }
    }

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
