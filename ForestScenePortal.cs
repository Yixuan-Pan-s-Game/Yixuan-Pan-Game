using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ForestScenePortal : MonoBehaviour
{
    public string targetSceneName;
    public string targetSpawnId;
    public Vector3 portalSize = new Vector3(5f, 3f, 5f);
    private static float portalCooldownUntil;
    private bool loading;

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

    private void OnTriggerEnter(Collider other)
    {
        if (loading || Time.time < portalCooldownUntil || other == null || other.GetComponentInParent<PlayerMovement>() == null) return;
        StartCoroutine(LoadTargetScene());
    }

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



