using UnityEngine;

public class StorageChest : MonoBehaviour
{
    public int columns = 8;
    public int rows = 8;
    public ToolSlot[] storedSlots = new ToolSlot[0];

    public int SlotCount => Mathf.Max(1, columns * rows);

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

    public bool RemoveItem(string itemId, int amount)
    {
        if (!InventoryUtility.RemoveItem(storedSlots, itemId, amount, out ToolSlot[] updated))
        {
            return false;
        }

        storedSlots = updated;
        return true;
    }

    public int CountItem(string itemId)
    {
        return InventoryUtility.CountItem(storedSlots, itemId);
    }
}
