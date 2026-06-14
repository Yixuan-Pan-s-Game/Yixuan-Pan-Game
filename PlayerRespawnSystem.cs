using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerRespawnSystem
{
    public static bool Respawn(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
        {
            return false;
        }

        GameObject player = playerHealth.gameObject;
        Vector3 targetPosition = ResolveRespawnAnchor();
        Quaternion targetRotation = Quaternion.identity;
        targetPosition = SnapRespawnPositionToGround(player, targetPosition);

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.TeleportToRespawn(targetPosition, targetRotation);
        }
        else
        {
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = targetPosition;
                body.rotation = targetRotation;
            }

            player.transform.SetPositionAndRotation(targetPosition, targetRotation);
            Physics.SyncTransforms();
        }

        playerHealth.currentHealth = Mathf.Max(1, playerHealth.maxHealth);
        playerHealth.temporaryDefense = 0;
        playerHealth.temporaryDefenseUntil = 0f;
        ForestSaveSystem.SaveGame();
        return true;
    }

    private static Vector3 ResolveRespawnAnchor()
    {
        Transform alpineSpawn = FindExactInScene("Cube", ForestRuntimePlayerBootstrap.AlpineSceneName)
            ?? FindByNameContains("WoodenHouse3")
            ?? FindByNameContains("WoodenHouse2")
            ?? FindByNameContains("WoodenHouse1")
            ?? FindByNameContains("WoodenHouse");
        if (alpineSpawn != null)
        {
            return GetSpawnPointAbove(alpineSpawn);
        }

        return new Vector3(0f, 20f, 0f);
    }

    private static Vector3 SnapRespawnPositionToGround(GameObject player, Vector3 targetPosition)
    {
        if (player == null)
        {
            return targetPosition;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        float bottomOffset = capsule != null ? capsule.bounds.min.y - player.transform.position.y : 0f;
        Vector3 origin = targetPosition + Vector3.up * 80f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 180f, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        bool found = false;
        RaycastHit bestHit = default;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null
                || !hit.collider.enabled
                || hit.collider.isTrigger
                || hit.collider.transform.IsChildOf(player.transform)
                || hit.normal.y < 0.35f)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        if (!found)
        {
            return targetPosition;
        }

        return new Vector3(targetPosition.x, bestHit.point.y - bottomOffset + 0.08f, targetPosition.z);
    }

    private static Transform FindExactInScene(string name, string sceneName)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(sceneName)) return null;
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == name && candidate.gameObject.scene.name == sceneName)
            {
                return candidate;
            }
        }

        return null;
    }
    private static Transform FindByNameContains(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        string lowerText = text.ToLowerInvariant();
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name.ToLowerInvariant().Contains(lowerText))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Vector3 GetSpawnPointAbove(Transform root)
    {
        if (root == null) return new Vector3(0f, 20f, 0f);
        if (TryGetRendererBounds(root, out Bounds rendererBounds))
        {
            return new Vector3(rendererBounds.center.x, rendererBounds.max.y + 4f, rendererBounds.center.z);
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        bool foundCollider = false;
        Bounds colliderBounds = default;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger) continue;
            if (!foundCollider)
            {
                colliderBounds = collider.bounds;
                foundCollider = true;
            }
            else
            {
                colliderBounds.Encapsulate(collider.bounds);
            }
        }

        if (foundCollider)
        {
            return new Vector3(colliderBounds.center.x, colliderBounds.max.y + 4f, colliderBounds.center.z);
        }

        return root.position + Vector3.up * 20f;
    }
    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }
}











