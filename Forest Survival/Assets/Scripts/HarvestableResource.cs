using UnityEngine;

public enum HarvestResourceType
{
    Tree,
    Rock
}

// Harvestable world resource that manages health, required tools, drops, and harvest progress.
public class HarvestableResource : MonoBehaviour
{
    // Current interaction target or gameplay object being processed: resourceType.
    public HarvestResourceType resourceType;
    // Gameplay stat that affects damage, health, healing, defense, or durability: maxHealth.
    public int maxHealth = 3;
    // Asset reference used for spawning, rendering, audio, or animation: dropPrefab.
    public GameObject dropPrefab;
    // Inventory or crafting data for items, recipes, slots, or stack counts: dropItemName.
    public string dropItemName = "Wood";
    // Inventory or crafting data for items, recipes, slots, or stack counts: dropCount.
    public int dropCount = 5;
    // Distance or radius used for detection, interaction, or physics checks: dropScatterRadius.
    public float dropScatterRadius = 0.55f;
    // Important runtime data or configuration used by this component: requiredToolTier.
    public int requiredToolTier;
    // Cached component or scene reference to avoid repeated lookups: maxChildRenderersForSafeDestroy.
    public int maxChildRenderersForSafeDestroy = 8;
    // Cached component or scene reference to avoid repeated lookups: maxChildCollidersForSafeDestroy.
    public int maxChildCollidersForSafeDestroy = 12;

    // Gameplay stat that affects damage, health, healing, defense, or durability: health.
    private int health;
    // Spatial value used for positioning, rotation, scale, or collision math: originalScale.
    private Vector3 originalScale;
    // Important runtime data or configuration used by this component: harvested.
    private bool harvested;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeWoodMaterial.
    private static Material runtimeWoodMaterial;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeStoneMaterial.
    private static Material runtimeStoneMaterial;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeWoodDropPrefab.
    private static GameObject runtimeWoodDropPrefab;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        ResetHealth();
        originalScale = transform.localScale;
    }

    // Handles the reset health workflow.
    public void ResetHealth()
    {
        health = maxHealth;
    }

    // Handles the capture original scale workflow.
    public void CaptureOriginalScale()
    {
        originalScale = transform.localScale;
    }

    // Ensures the objects, references, or configuration required for ensure default drops exist.
    public void EnsureDefaultDrops()
    {
        if (resourceType == HarvestResourceType.Tree)
        {
            if (string.IsNullOrEmpty(dropItemName))
            {
                dropItemName = "Wood";
            }

            if (dropCount <= 0)
            {
                dropCount = 5;
            }

            return;
        }

        if (resourceType != HarvestResourceType.Rock)
        {
            return;
        }

        if (string.IsNullOrEmpty(dropItemName))
        {
            dropItemName = "Stone";
        }

        if (dropCount <= 0)
        {
            dropCount = 5;
        }
    }

    // Handles the hit workflow.
    public bool Hit(HarvestResourceType toolTargetType, int damage)
    {
        if (toolTargetType != resourceType)
        {
            return false;
        }

        health -= Mathf.Max(1, damage);
        float punch = 1f + Mathf.Clamp01(health / (float)maxHealth) * 0.03f;
        transform.localScale = originalScale * punch;

        if (health <= 0)
        {
            HarvestCompletely(toolTargetType);
        }

        return true;
    }

    // Handles the harvest completely workflow.
    public void HarvestCompletely(HarvestResourceType toolTargetType)
    {
        if (harvested || toolTargetType != resourceType)
        {
            return;
        }

        if (!CanSafelyDestroySelf())
        {
            ResetHealth();
            transform.localScale = originalScale;
            Debug.LogWarning("HarvestableResource: Refused to harvest an unsafe large scene object: " + name, this);
            return;
        }

        harvested = true;
        SpawnDrops();
        Destroy(gameObject);
    }

    // Calculates and returns the result for is safe harvest target.
    public bool IsSafeHarvestTarget()
    {
        return CanSafelyDestroySelf();
    }

    // Calculates and returns the result for can safely destroy self.
    private bool CanSafelyDestroySelf()
    {
        Transform current = transform;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName == "forest_island"
                || lowerName == "mining_island"
                || lowerName == "combined_mining_alpine_scene"
                || lowerName == "terrain_stone"
                || lowerName == "stones&rocks"
                || lowerName == "crystals&ores&veins")
            {
                if (current == transform)
                {
                    return false;
                }
            }

            current = current.parent;
        }

        if (transform.parent == null && transform.childCount > 2)
        {
            return false;
        }

        Bounds bounds;
        if (TryGetRenderBounds(gameObject, out bounds) && !IsReasonableResourceSize(bounds))
        {
            return false;
        }

        return true;
    }

    // Calculates and returns the result for is reasonable resource size.
    private bool IsReasonableResourceSize(Bounds bounds)
    {
        Vector3 size = bounds.size;
        if (resourceType == HarvestResourceType.Tree)
        {
            return size.x <= 100f && size.y <= 160f && size.z <= 100f;
        }

        return size.x <= 80f && size.y <= 80f && size.z <= 80f;
    }

    // Adds, spawns, or attaches the objects and data for spawn drops.
    private void SpawnDrops()
    {
        if (dropCount <= 0)
        {
            return;
        }

        for (int i = 0; i < dropCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dropScatterRadius;
            bool isStoneDrop = !string.IsNullOrEmpty(dropItemName) && dropItemName.ToLowerInvariant().Contains("stone");
            GameObject resolvedDropPrefab = ResolveDropPrefab(dropItemName, dropPrefab);
            float baseSpawnHeight = isStoneDrop ? 0.9f : 0.65f;
            Vector3 spawnPosition = transform.position + new Vector3(offset.x, baseSpawnHeight + i * 0.05f, offset.y);
            GameObject drop = resolvedDropPrefab != null
                ? Instantiate(resolvedDropPrefab, spawnPosition, Random.rotation)
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (resolvedDropPrefab == null)
            {
                drop.transform.position = spawnPosition;
                drop.transform.rotation = Random.rotation;
            }
            drop.SetActive(true);

            drop.name = dropItemName + "Pickup";
            drop.transform.localScale = GetDropScale(dropItemName);
            StripHarvestComponents(drop);
            PrepareDropVisuals(drop);
            PrepareDropPhysics(drop);

            Rigidbody rigidbody = drop.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = drop.AddComponent<Rigidbody>();
            }

            rigidbody.mass = 0.25f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.AddForce(new Vector3(offset.x * 0.35f, isStoneDrop ? 0.85f : 0.65f, offset.y * 0.35f), ForceMode.Impulse);

            SphereCollider pickupTrigger = drop.GetComponent<SphereCollider>();
            if (pickupTrigger == null || !pickupTrigger.isTrigger)
            {
                pickupTrigger = drop.AddComponent<SphereCollider>();
            }

            pickupTrigger.isTrigger = true;
            pickupTrigger.radius = 1.15f;

            CollectibleItem collectible = drop.AddComponent<CollectibleItem>();
            collectible.itemName = dropItemName;
            collectible.amount = 1;
            collectible.inventoryPrefab = resolvedDropPrefab;
            collectible.pickupDelay = isStoneDrop ? 0.55f : 0.45f;
        }
    }

    // Finds, loads, or caches the references needed for resolve drop prefab.
    private static GameObject ResolveDropPrefab(string itemName, GameObject configuredPrefab)
    {
        if (configuredPrefab != null)
        {
            return configuredPrefab;
        }

        string itemId = InventoryUtility.NormalizeItemId(itemName);
        if (itemId == "stone")
        {
            return Resources.Load<GameObject>("RuntimePrefabs/Held_LowPolyStone");
        }

        if (itemId == "wood")
        {
            return GetOrCreateRuntimeWoodDropPrefab();
        }

        return null;
    }

    // Calculates and returns the result for get or create runtime wood drop prefab.
    private static GameObject GetOrCreateRuntimeWoodDropPrefab()
    {
        if (runtimeWoodDropPrefab != null)
        {
            return runtimeWoodDropPrefab;
        }

        runtimeWoodDropPrefab = new GameObject("Held_Wood_Runtime");
        runtimeWoodDropPrefab.name = "Held_Wood_Runtime";
        runtimeWoodDropPrefab.hideFlags = HideFlags.HideAndDontSave;
        runtimeWoodDropPrefab.transform.position = new Vector3(10000f, 10000f, 10000f);

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bar.name = "Bar";
        bar.transform.SetParent(runtimeWoodDropPrefab.transform, false);
        bar.transform.localPosition = Vector3.zero;
        bar.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        bar.transform.localScale = new Vector3(0.13f, 0.42f, 0.13f);

        Collider collider = bar.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }

        runtimeWoodDropPrefab.SetActive(false);

        Renderer renderer = bar.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateFallbackTintMaterial("Wood");
        }

        Object.DontDestroyOnLoad(runtimeWoodDropPrefab);
        return runtimeWoodDropPrefab;
    }

    // Handles the strip harvest components workflow.
    private static void StripHarvestComponents(GameObject drop)
    {
        if (drop == null)
        {
            return;
        }

        HarvestableResource[] harvestables = drop.GetComponentsInChildren<HarvestableResource>(true);
        for (int i = 0; i < harvestables.Length; i++)
        {
            if (harvestables[i] != null)
            {
                Destroy(harvestables[i]);
            }
        }

        LockableTarget[] lockables = drop.GetComponentsInChildren<LockableTarget>(true);
        for (int i = 0; i < lockables.Length; i++)
        {
            if (lockables[i] != null)
            {
                Destroy(lockables[i]);
            }
        }
    }

    // Handles the prepare drop physics workflow.
    private static void PrepareDropPhysics(GameObject drop)
    {
        MeshCollider[] meshColliders = drop.GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            meshColliders[i].enabled = false;
        }

        if (HasSolidCollider(drop))
        {
            return;
        }

        if (!TryGetRenderBounds(drop, out Bounds bounds))
        {
            SphereCollider fallbackCollider = drop.GetComponent<SphereCollider>();
            if (fallbackCollider == null || fallbackCollider.isTrigger)
            {
                fallbackCollider = drop.AddComponent<SphereCollider>();
            }

            fallbackCollider.isTrigger = false;
            fallbackCollider.radius = 0.3f;
            fallbackCollider.center = Vector3.zero;
            return;
        }

        BoxCollider boxCollider = drop.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = drop.AddComponent<BoxCollider>();
        }

        Vector3 localCenter = drop.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = DivideByLossyScale(bounds.size, drop.transform.lossyScale);
        boxCollider.center = localCenter;
        boxCollider.size = localSize;
        boxCollider.isTrigger = false;
    }

    // Handles the prepare drop visuals workflow.
    private static void PrepareDropVisuals(GameObject drop)
    {
        if (drop == null)
        {
            return;
        }

        if (IsOreDropName(drop.name))
        {
            return;
        }

        Renderer[] renderers = drop.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int j = 0; j < materials.Length; j++)
            {
                Material replacement = CreateCompatibleDropMaterial(materials[j], drop.name);
                if (replacement != null)
                {
                    materials[j] = replacement;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create compatible drop material.
    private static Material CreateCompatibleDropMaterial(Material sourceMaterial, string dropName)
    {
        if (sourceMaterial == null)
        {
            return CreateFallbackTintMaterial(dropName);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        bool isStone = !string.IsNullOrEmpty(dropName) && dropName.ToLowerInvariant().Contains("stone");
        if (!isStone && runtimeWoodMaterial == null)
        {
            runtimeWoodMaterial = new Material(shader);
            runtimeWoodMaterial.name = "RuntimeWoodDropMaterial";
        }
        else if (isStone && runtimeStoneMaterial == null)
        {
            runtimeStoneMaterial = new Material(shader);
            runtimeStoneMaterial.name = "RuntimeStoneDropMaterial";
        }

        Material materialInstance = new Material(isStone ? runtimeStoneMaterial : runtimeWoodMaterial);
        Color tint = isStone ? new Color(0.48f, 0.50f, 0.53f, 1f) : new Color(0.46f, 0.30f, 0.16f, 1f);
        materialInstance.color = tint;
        if (materialInstance.HasProperty("_BaseColor"))
        {
            materialInstance.SetColor("_BaseColor", tint);
        }

        Texture baseTexture = GetFirstTexture(sourceMaterial, "_BaseColorMap", "_BaseMap", "_MainTex");
        Texture normalTexture = GetFirstTexture(sourceMaterial, "_NormalMap", "_BumpMap");

        if (shader.name.Contains("Universal Render Pipeline"))
        {
            if (baseTexture != null)
            {
                materialInstance.SetTexture("_BaseMap", baseTexture);
            }

            if (normalTexture != null)
            {
                materialInstance.EnableKeyword("_NORMALMAP");
                materialInstance.SetTexture("_BumpMap", normalTexture);
            }

            materialInstance.SetFloat("_Smoothness", 0.28f);
            materialInstance.SetFloat("_Metallic", 0f);
        }
        else
        {
            if (baseTexture != null)
            {
                materialInstance.SetTexture("_MainTex", baseTexture);
            }

            if (normalTexture != null)
            {
                materialInstance.EnableKeyword("_NORMALMAP");
                materialInstance.SetTexture("_BumpMap", normalTexture);
            }

            materialInstance.SetFloat("_Glossiness", 0.28f);
            materialInstance.SetFloat("_Metallic", 0f);
        }

        return materialInstance;
    }

    // Calculates and returns the result for is ore drop name.
    private static bool IsOreDropName(string dropName)
    {
        string lower = (dropName ?? string.Empty).ToLowerInvariant();
        return lower.Contains(" ore") || lower.Contains("_ore") || lower.Contains("orepickup");
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create fallback tint material.
    private static Material CreateFallbackTintMaterial(string dropName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        bool isStone = !string.IsNullOrEmpty(dropName) && dropName.ToLowerInvariant().Contains("stone");
        Material material = new Material(shader);
        Color tint = isStone ? new Color(0.48f, 0.50f, 0.53f, 1f) : new Color(0.46f, 0.30f, 0.16f, 1f);
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", isStone ? 0.18f : 0.28f);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", isStone ? 0.18f : 0.28f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        return material;
    }

    // Calculates and returns the result for get first texture.
    private static Texture GetFirstTexture(Material material, params string[] propertyNames)
    {
        for (int i = 0; i < propertyNames.Length; i++)
        {
            string property = propertyNames[i];
            if (material.HasProperty(property))
            {
                Texture texture = material.GetTexture(property);
                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    // Calculates and returns the result for has solid collider.
    private static bool HasSolidCollider(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled && !colliders[i].isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    // Attempts to try get render bounds and returns whether the operation succeeded.
    private static bool TryGetRenderBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    // Handles the divide by lossy scale workflow.
    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            size.x / Mathf.Max(0.0001f, lossyScale.x),
            size.y / Mathf.Max(0.0001f, lossyScale.y),
            size.z / Mathf.Max(0.0001f, lossyScale.z));
    }

    // Calculates and returns the result for get drop scale.
    private static Vector3 GetDropScale(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return Vector3.one * 0.42f;
        }

        string lowerName = itemName.ToLowerInvariant();
        if (lowerName.Contains("wood"))
        {
            return new Vector3(0.95f, 0.78f, 0.95f);
        }

        if (lowerName.Contains("stone") || lowerName.Contains("rock"))
        {
            return Vector3.one * 0.24f;
        }

        return Vector3.one * 0.42f;
    }
}


