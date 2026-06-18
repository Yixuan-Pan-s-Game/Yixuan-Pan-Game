using UnityEngine;

// Animal behavior component for patrol, harvesting, or interaction state.
public class SheepShearable : MonoBehaviour
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: woolItemId.
    public string woolItemId = "wool";
    // Runtime flag that drives control flow, UI state, or gameplay availability: woolDisplayName.
    public string woolDisplayName = "Wool";
    // Inventory or crafting data for items, recipes, slots, or stack counts: woolAmount.
    public int woolAmount = 1;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: shearHoldSeconds.
    public float shearHoldSeconds = 2f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: woolRegrowSeconds.
    public float woolRegrowSeconds = 300f;
    public Color shornColor = new Color(0.72f, 0.68f, 0.58f, 1f);

    // Cached component or scene reference to avoid repeated lookups: renderers.
    private Renderer[] renderers;
    // Asset reference used for spawning, rendering, audio, or animation: originalMaterials.
    private Material[][] originalMaterials;
    // Asset reference used for spawning, rendering, audio, or animation: shornMaterial.
    private Material shornMaterial;
    // Runtime flag that drives control flow, UI state, or gameplay availability: hasWool.
    private bool hasWool = true;
    // Important runtime data or configuration used by this component: woolRegrowAt.
    private float woolRegrowAt;

    // Read-only state exposed to other systems: HasWool.
    public bool HasWool => hasWool;
    // Read-only state exposed to other systems: IsReadyToShear.
    public bool IsReadyToShear => hasWool && Time.time >= woolRegrowAt;
    // Read-only state exposed to other systems: ShearHoldSeconds.
    public float ShearHoldSeconds => Mathf.Max(0.2f, shearHoldSeconds);
    // Read-only state exposed to other systems: RemainingRegrowSeconds.
    public float RemainingRegrowSeconds => hasWool ? 0f : Mathf.Max(0f, woolRegrowAt - Time.time);

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        CacheRenderers();
        ApplyWoolVisual();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (!hasWool && Time.time >= woolRegrowAt)
        {
            hasWool = true;
            ApplyWoolVisual();
        }
    }

    // Attempts to try shear and returns whether the operation succeeded.
    public bool TryShear(PlayerToolController player)
    {
        if (player == null || !IsReadyToShear)
        {
            return false;
        }

        ToolSlot woolSlot = CreateWoolSlot(player);
        if (woolSlot == null || !player.TryAddOrMergeInventorySlot(woolSlot))
        {
            return false;
        }

        hasWool = false;
        woolRegrowAt = Time.time + Mathf.Max(1f, woolRegrowSeconds);
        ApplyWoolVisual();
        return true;
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create wool slot.
    private ToolSlot CreateWoolSlot(PlayerToolController player)
    {
        ToolSlot inventoryTemplate = FindInventoryWoolTemplate(player);
        if (inventoryTemplate != null)
        {
            return InventoryUtility.CloneSlot(inventoryTemplate, Mathf.Max(1, woolAmount));
        }

        CraftingCatalog catalog = player.CraftingCatalog;
        CraftingItemDefinition definition = catalog != null ? catalog.FindItem(woolItemId) : null;
        if (definition != null)
        {
            return InventoryUtility.CreateSlotFromDefinition(definition, Mathf.Max(1, woolAmount));
        }

        return new ToolSlot
        {
            itemId = woolItemId,
            displayName = woolDisplayName,
            category = ToolCategory.Materials,
            stackCount = Mathf.Max(1, woolAmount),
            maxStack = 99,
            heldLocalPosition = new Vector3(0f, -0.08f, 0.02f),
            heldLocalEuler = Vector3.zero,
            heldLocalScale = Vector3.one * 0.15f
        };
    }

    // Finds, loads, or caches the references needed for find inventory wool template.
    private ToolSlot FindInventoryWoolTemplate(PlayerToolController player)
    {
        if (player == null || player.Slots == null)
        {
            return null;
        }

        ToolSlot[] slots = player.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (slot == null || InventoryUtility.GetItemId(slot) != woolItemId)
            {
                continue;
            }

            if (slot.prefab != null || slot.worldPrefab != null)
            {
                return slot;
            }
        }

        return null;
    }

    // Finds, loads, or caches the references needed for cache renderers.
    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : new Material[0];
        }
    }

    // Refreshes and applies configuration or runtime state for apply wool visual.
    private void ApplyWoolVisual()
    {
        if (renderers == null || renderers.Length == 0)
        {
            CacheRenderers();
        }

        if (hasWool)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials != null && i < originalMaterials.Length)
                {
                    renderers[i].sharedMaterials = originalMaterials[i];
                }
            }

            return;
        }

        if (shornMaterial == null)
        {
            Shader shader = FindFirstRendererShader();
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            shornMaterial = new Material(shader);
            shornMaterial.name = "Runtime_ShornSheep";
            shornMaterial.color = shornColor;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material[] materials = renderers[i].sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                materials = new Material[1];
            }

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = shornMaterial;
            }

            renderers[i].sharedMaterials = materials;
        }
    }

    // Finds, loads, or caches the references needed for find first renderer shader.
    private Shader FindFirstRendererShader()
    {
        if (renderers == null)
        {
            return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].sharedMaterial == null)
            {
                continue;
            }

            return renderers[i].sharedMaterial.shader;
        }

        return null;
    }
}
