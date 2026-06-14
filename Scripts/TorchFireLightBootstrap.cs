using System.Collections;
using UnityEngine;

public class TorchFireLightBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrap()
    {
        if (FindObjectOfType<TorchFireLightBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject("TorchFireLightBootstrap");
        bootstrap.AddComponent<TorchFireLightBootstrap>();
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < 5; i++)
        {
            AttachToSceneTorches();
            yield return new WaitForSeconds(2f);
        }
    }

    private static void AttachToSceneTorches()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            GameObject root = FindTorchRoot(renderer.transform);
            if (root == null || root.GetComponent<TorchFireLight>() != null)
            {
                continue;
            }

            TorchFireLight fire = root.AddComponent<TorchFireLight>();
            fire.LocalOffset = EstimateTorchTopOffset(root.transform, renderer.bounds);
        }
    }

    private static GameObject FindTorchRoot(Transform start)
    {
        Transform current = start;
        GameObject best = null;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("torch") && !lowerName.Contains("fireanchor") && !lowerName.Contains("flame"))
            {
                best = current.gameObject;
            }

            current = current.parent;
        }

        if (best == null || best.GetComponentInChildren<ParticleSystem>(true) != null)
        {
            return null;
        }

        return best;
    }

    private static Vector3 EstimateTorchTopOffset(Transform root, Bounds rendererBounds)
    {
        Vector3 top = rendererBounds.center;
        top.y = rendererBounds.max.y;
        Vector3 localTop = root.InverseTransformPoint(top);
        if (localTop.sqrMagnitude < 0.0001f)
        {
            return new Vector3(0f, 1.05f, 0f);
        }

        return localTop;
    }
}
