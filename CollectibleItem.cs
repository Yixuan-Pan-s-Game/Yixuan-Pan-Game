using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public string itemName = "Wood";
    public int amount = 1;
    public GameObject inventoryPrefab;
    public float pickupRadius = 2.2f;
    public float pickupDelay = 0.6f;

    private bool collected;
    private PlayerToolController playerInventory;
    private float spawnedAtTime;

    private void Awake()
    {
        spawnedAtTime = Time.time;
    }

    private void Update()
    {
        if (collected)
        {
            return;
        }

        if (Time.time - spawnedAtTime < pickupDelay)
        {
            return;
        }

        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerToolController>();
        }

        if (playerInventory != null && Vector3.Distance(transform.position, playerInventory.transform.position) <= pickupRadius)
        {
            Collect(playerInventory);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (collected)
        {
            return;
        }

        if (Time.time - spawnedAtTime < pickupDelay)
        {
            return;
        }

        PlayerToolController inventory = other.GetComponentInParent<PlayerToolController>();
        if (inventory == null)
        {
            return;
        }

        Collect(inventory);
    }

    private void Collect(PlayerToolController inventory)
    {
        if (collected || inventory == null)
        {
            return;
        }

        if (!inventory.TryAddInventoryItem(itemName, inventoryPrefab, amount))
        {
            return;
        }

        collected = true;
        Destroy(gameObject);
    }
}
