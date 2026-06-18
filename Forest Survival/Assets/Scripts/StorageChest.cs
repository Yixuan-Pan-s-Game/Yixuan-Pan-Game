using UnityEngine;

// Storage container data source for adding, removing, and counting chest items.
public class StorageChest : MonoBehaviour
{
    // Important runtime data or configuration used by this component: columns.
    public int columns = 8;
    // Important runtime data or configuration used by this component: rows.
    public int rows = 8;
    // Inventory or crafting data for items, recipes, slots, or stack counts: storedSlots.
    public ToolSlot[] storedSlots = new ToolSlot[0];

    // Read-only state exposed to other systems: SlotCount.
    public int SlotCount => Mathf.Max(1, columns * rows);

    // Adds, spawns, or attaches the objects and data for add item.
    public bool AddItem(ToolSlot slot, int amount)
    {
        if (slot == null || amount <= 0)
        {
            return false;
        }

        ToolSlot[] updated = InventoryUtility.AddItem(storedSlots, InventoryUtility.CloneSlot(slot, amount));
        if (updated.Length > SlotCount)
        {
            return false;
        }

        storedSlots = updated;
        return true;
    }

    // Clears runtime objects, cached data, or temporary state for remove item.
    public bool RemoveItem(string itemId, int amount)
    {
        if (!InventoryUtility.RemoveItem(storedSlots, itemId, amount, out ToolSlot[] updated))
        {
            return false;
        }

        storedSlots = updated;
        return true;
    }

    // Calculates and returns the result for count item.
    public int CountItem(string itemId)
    {
        return InventoryUtility.CountItem(storedSlots, itemId);
    }
}
