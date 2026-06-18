using UnityEngine;

// collectible item script that owns this feature's runtime behavior.
public class CollectibleItem : MonoBehaviour
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: itemName.
    public string itemName = "Wood";
    // Inventory or crafting data for items, recipes, slots, or stack counts: amount.
    public int amount = 1;
    // Asset reference used for spawning, rendering, audio, or animation: inventoryPrefab.
    public GameObject inventoryPrefab;
    // Distance or radius used for detection, interaction, or physics checks: pickupRadius.
    public float pickupRadius = 2.2f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: pickupDelay.
    public float pickupDelay = 0.6f;

    // Important runtime data or configuration used by this component: collected.
    private bool collected;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: spawnedAtTime.
    private float spawnedAtTime;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        spawnedAtTime = Time.time;
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (collected || !CanPickupYet())
        {
            return;
        }

        PlayerToolController inventory = FindObjectOfType<PlayerToolController>();
        if (inventory != null && Vector3.Distance(transform.position, inventory.transform.position) <= pickupRadius)
        {
            Collect(inventory);
        }
    }

    // Unity physics callback: reacts to trigger or collision events.
    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    // Unity physics callback: reacts to trigger or collision events.
    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    // Calculates and returns the result for can pickup yet.
    private bool CanPickupYet()
    {
        return Time.time - spawnedAtTime >= pickupDelay;
    }

    // Attempts to try collect and returns whether the operation succeeded.
    private void TryCollect(Collider other)
    {
        if (collected || !CanPickupYet() || other == null)
        {
            return;
        }

        PlayerToolController inventory = other.GetComponentInParent<PlayerToolController>();
        if (inventory != null)
        {
            Collect(inventory);
        }
    }

    // Handles the collect workflow.
    private void Collect(PlayerToolController inventory)
    {
        if (collected || inventory == null)
        {
            return;
        }

        if (inventory.TryAddInventoryItem(itemName, inventoryPrefab, amount))
        {
            collected = true;
            Destroy(gameObject);
        }
    }
}

