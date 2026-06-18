using UnityEngine;

// Shared world-resource setup helpers for harvestables, collectibles, colliders, and ground snapping.
public static class WorldResourceUtility
{
    // Refreshes and applies configuration or runtime state for configure harvestable node.
    public static void ConfigureHarvestableNode(
        GameObject instance,
        HarvestResourceType resourceType,
        GameObject dropPrefab,
        string dropItemName,
        int dropCount,
        int maxHealth,
        bool addLockableTarget = true)
    {
        if (instance == null)
        {
            return;
        }

        if (addLockableTarget && instance.GetComponentInChildren<LockableTarget>() == null)
        {
            instance.AddComponent<LockableTarget>();
        }

        HarvestableResource harvestable = instance.GetComponent<HarvestableResource>();
        if (harvestable == null)
        {
            harvestable = instance.AddComponent<HarvestableResource>();
        }

        harvestable.resourceType = resourceType;
        harvestable.maxHealth = Mathf.Max(1, maxHealth);
        harvestable.dropPrefab = dropPrefab;
        harvestable.dropItemName = dropItemName;
        harvestable.dropCount = Mathf.Max(1, dropCount);
        harvestable.ResetHealth();
        harvestable.CaptureOriginalScale();
    }

    // Refreshes and applies configuration or runtime state for configure collectible node.
    public static void ConfigureCollectibleNode(
        GameObject instance,
        string itemName,
        int amount,
        GameObject inventoryPrefab,
        float triggerRadius = 1.1f,
        bool addLockableTarget = false)
    {
        if (instance == null)
        {
            return;
        }

        if (addLockableTarget && instance.GetComponentInChildren<LockableTarget>() == null)
        {
            instance.AddComponent<LockableTarget>();
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        SphereCollider trigger = instance.GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = instance.AddComponent<SphereCollider>();
        }

        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(0.35f, triggerRadius);

        CollectibleItem collectible = instance.GetComponent<CollectibleItem>();
        if (collectible == null)
        {
            collectible = instance.AddComponent<CollectibleItem>();
        }

        collectible.itemName = itemName;
        collectible.amount = Mathf.Max(1, amount);
        collectible.inventoryPrefab = inventoryPrefab;
        collectible.pickupDelay = 0f;
    }

    // Adds, spawns, or attaches the objects and data for add obstacle collider.
    public static void AddObstacleCollider(
        GameObject instance,
        bool isTree,
        float scale,
        float treeColliderHeightPerScale = 2.25f,
        float treeColliderRadiusPerScale = 0.42f,
        float rockColliderWidthShrink = 0.92f,
        float rockColliderHeightShrink = 0.9f)
    {
        if (instance == null)
        {
            return;
        }

        RemoveExistingColliders(instance);
        if (!TryGetRenderBounds(instance, out Bounds bounds))
        {
            return;
        }

        if (isTree)
        {
            CapsuleCollider collider = instance.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            float treeColliderHeight = Mathf.Max(treeColliderHeightPerScale * scale, bounds.size.y * 0.72f);
            float treeColliderRadius = Mathf.Max(treeColliderRadiusPerScale * scale, Mathf.Min(bounds.size.x, bounds.size.z) * 0.18f);
            Vector3 treeWorldCenter = new Vector3(bounds.center.x, bounds.min.y + treeColliderHeight * 0.5f, bounds.center.z);
            collider.center = instance.transform.InverseTransformPoint(treeWorldCenter);
            collider.height = Mathf.Max(treeColliderHeight / Mathf.Max(0.0001f, Mathf.Abs(instance.transform.lossyScale.y)), treeColliderRadius * 2f);
            collider.radius = treeColliderRadius / Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(instance.transform.lossyScale.x), Mathf.Abs(instance.transform.lossyScale.z)));
            return;
        }

        BoxCollider boxCollider = instance.AddComponent<BoxCollider>();
        float colliderHeight = Mathf.Max(0.25f, bounds.size.y * rockColliderHeightShrink);
        float colliderWidth = Mathf.Max(0.3f, bounds.size.x * rockColliderWidthShrink);
        float colliderDepth = Mathf.Max(0.3f, bounds.size.z * rockColliderWidthShrink);
        float centerYWorld = bounds.min.y + (colliderHeight * 0.5f);
        Vector3 worldCenter = new Vector3(bounds.center.x, centerYWorld, bounds.center.z);
        Vector3 localCenter = instance.transform.InverseTransformPoint(worldCenter);
        Vector3 localSize = DivideByLossyScale(new Vector3(colliderWidth, colliderHeight, colliderDepth), instance.transform.lossyScale);

        boxCollider.center = localCenter;
        boxCollider.size = localSize;
    }

    // Sets state, selection, or placement data for snap object to ground by bounds.
    public static bool SnapObjectToGroundByBounds(GameObject instance, float groundY, float offset = 0f)
    {
        if (instance == null || !TryGetRenderBounds(instance, out Bounds bounds))
        {
            return false;
        }

        Vector3 position = instance.transform.position;
        position.y += groundY - bounds.min.y + offset;
        instance.transform.position = position;
        return true;
    }

    // Sets state, selection, or placement data for snap object to ground by bounds.
    public static bool SnapObjectToGroundByBounds(GameObject instance, Terrain terrain, float offset = 0f)
    {
        if (instance == null || terrain == null)
        {
            return false;
        }

        Vector3 position = instance.transform.position;
        float groundY = terrain.SampleHeight(position) + terrain.transform.position.y;
        return SnapObjectToGroundByBounds(instance, groundY, offset);
    }

    // Sets state, selection, or placement data for snap object to ground by bounds.
    public static bool SnapObjectToGroundByBounds(GameObject instance, ProceduralTerrain terrain, float offset = 0f)
    {
        if (instance == null || terrain == null)
        {
            return false;
        }

        Vector3 position = instance.transform.position;
        float groundY = terrain.SampleHeightWorld(position.x, position.z);
        return SnapObjectToGroundByBounds(instance, groundY, offset);
    }

    // Sets state, selection, or placement data for snap object to ground by bounds raycast.
    public static bool SnapObjectToGroundByBoundsRaycast(
        GameObject instance,
        float offset = 0f,
        float rayStartHeight = 50f,
        float rayDistance = 120f,
        int layerMask = Physics.DefaultRaycastLayers)
    {
        if (instance == null)
        {
            return false;
        }

        Vector3 origin = instance.transform.position + Vector3.up * Mathf.Max(0f, rayStartHeight);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Mathf.Max(0.01f, rayDistance), layerMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return SnapObjectToGroundByBounds(instance, hit.point.y, offset);
    }

    // Attempts to try get render bounds and returns whether the operation succeeded.
    public static bool TryGetRenderBounds(GameObject instance, out Bounds bounds)
    {
        Renderer[] renderers = instance != null ? instance.GetComponentsInChildren<Renderer>(true) : null;
        if (renderers == null || renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return true;
    }

    // Clears runtime objects, cached data, or temporary state for remove existing colliders.
    public static void RemoveExistingColliders(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(colliders[i]);
            }
            else
            {
                Object.DestroyImmediate(colliders[i]);
            }
        }
    }

    // Handles the divide by lossy scale workflow.
    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            size.x / Mathf.Max(0.0001f, lossyScale.x),
            size.y / Mathf.Max(0.0001f, lossyScale.y),
            size.z / Mathf.Max(0.0001f, lossyScale.z));
    }
}
