using UnityEngine;

public enum HarvestResourceType
{
    Tree,
    Rock
}

public class HarvestableResource : MonoBehaviour
{
    public HarvestResourceType resourceType;
    public int maxHealth = 3;
    public GameObject dropPrefab;
    public string dropItemName = "Wood";
    public int dropCount = 5;
    public float dropScatterRadius = 1.2f;
    public int requiredToolTier;
    public int maxChildRenderersForSafeDestroy = 8;
    public int maxChildCollidersForSafeDestroy = 12;

    private int health;
    private Vector3 originalScale;
    private bool harvested;
    private static Material runtimeWoodMaterial;
    private static Material runtimeStoneMaterial;

    private void Awake()
    {
        ResetHealth();
        originalScale = transform.localScale;
    }

    public void ResetHealth()
    {
        health = maxHealth;
    }

    public void CaptureOriginalScale()
    {
        originalScale = transform.localScale;
    }

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

    public bool IsSafeHarvestTarget()
    {
        return CanSafelyDestroySelf();
    }

    private bool CanSafelyDestroySelf()
    {
        if (transform.parent == null && transform.childCount > 2)
        {
            return false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > Mathf.Max(1, maxChildRenderersForSafeDestroy))
        {
            return false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length > Mathf.Max(1, maxChildCollidersForSafeDestroy))
        {
            return false;
        }

        Bounds bounds;
        if (TryGetRenderBounds(gameObject, out bounds) && bounds.size.sqrMagnitude > 900f)
        {
            return false;
        }

        return true;
    }

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
            float baseSpawnHeight = isStoneDrop ? 0.9f : 0.65f;
            Vector3 spawnPosition = transform.position + new Vector3(offset.x, baseSpawnHeight + i * 0.05f, offset.y);
            GameObject drop = dropPrefab != null
                ? Instantiate(dropPrefab, spawnPosition, Random.rotation)
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (dropPrefab == null)
            {
                drop.transform.position = spawnPosition;
                drop.transform.rotation = Random.rotation;
            }

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
            rigidbody.AddForce(new Vector3(offset.x, isStoneDrop ? 2.35f : 1.8f, offset.y), ForceMode.Impulse);

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
            collectible.inventoryPrefab = dropPrefab;
            collectible.pickupDelay = isStoneDrop ? 0.95f : 0.75f;
        }
    }

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

    private static bool IsOreDropName(string dropName)
    {
        string lower = (dropName ?? string.Empty).ToLowerInvariant();
        return lower.Contains(" ore") || lower.Contains("_ore") || lower.Contains("orepickup");
    }

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

    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            size.x / Mathf.Max(0.0001f, lossyScale.x),
            size.y / Mathf.Max(0.0001f, lossyScale.y),
            size.z / Mathf.Max(0.0001f, lossyScale.z));
    }

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
            return Vector3.one * 0.48f;
        }

        return Vector3.one * 0.42f;
    }
}
