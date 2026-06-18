using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// forest scene portal script that owns this feature's runtime behavior.
public class ForestScenePortal : MonoBehaviour
{
    // Current interaction target or gameplay object being processed: targetSceneName.
    public string targetSceneName;
    // Current interaction target or gameplay object being processed: targetSpawnId.
    public string targetSpawnId;
    public Vector3 portalSize = new Vector3(5f, 3f, 5f);
    // Timing value or timestamp used for cooldowns, delays, or progress checks: portalCooldownUntil.
    private static float portalCooldownUntil;
    // Important runtime data or configuration used by this component: loading.
    private bool loading;

    // Ensures the objects, references, or configuration required for ensure portal exist.
    public static ForestScenePortal EnsurePortal(string portalName, string targetSceneName, string targetSpawnId, Vector3 position)
    {
        GameObject existing = GameObject.Find(portalName);
        GameObject portalObject = existing != null ? existing : new GameObject(portalName);
        portalObject.transform.position = position;

        BoxCollider collider = portalObject.GetComponent<BoxCollider>();
        if (collider == null) collider = portalObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(5f, 3f, 5f);
        collider.center = Vector3.up * 1.5f;

        ForestScenePortal portal = portalObject.GetComponent<ForestScenePortal>();
        if (portal == null) portal = portalObject.AddComponent<ForestScenePortal>();
        portal.targetSceneName = targetSceneName;
        portal.targetSpawnId = targetSpawnId;
        portal.portalSize = collider.size;
        return portal;
    }

    // Unity physics callback: reacts to trigger or collision events.
    private void OnTriggerEnter(Collider other)
    {
        if (loading || Time.time < portalCooldownUntil || other == null || other.GetComponentInParent<PlayerMovement>() == null) return;
        StartCoroutine(LoadTargetScene());
    }

    // Finds, loads, or caches the references needed for load target scene.
    private IEnumerator LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName)) yield break;
        loading = true;
        portalCooldownUntil = Time.time + 1.25f;

        Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
        if (!targetScene.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
            targetScene = SceneManager.GetSceneByName(targetSceneName);
        }

        if (targetScene.isLoaded)
        {
            SceneManager.SetActiveScene(targetScene);
        }

        yield return null;
        ForestRuntimePlayerBootstrap.TeleportCurrentPlayerToSpawn(targetSpawnId);
        loading = false;
    }
}



