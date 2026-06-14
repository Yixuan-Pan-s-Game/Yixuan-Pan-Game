using System.Collections.Generic;
using UnityEngine;

public static class InventoryUtility
{
    public static string NormalizeItemId(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return string.Empty;
        }

        return itemName.Trim().ToLowerInvariant().Replace(" ", "_");
    }

    public static string GetItemId(ToolSlot slot)
    {
        if (slot == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrEmpty(slot.itemId) ? slot.itemId : NormalizeItemId(slot.displayName);
    }

    public static int CountItem(ToolSlot[] slots, string itemId)
    {
        if (slots == null || string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (slot == null || GetItemId(slot) != itemId)
            {
                continue;
            }

            count += Mathf.Max(0, slot.stackCount);
        }

        return count;
    }

    public static ToolSlot CreateSlotFromDefinition(CraftingItemDefinition definition, int amount)
    {
        if (definition == null)
        {
            return null;
        }

        return new ToolSlot
        {
            itemId = definition.itemId,
            displayName = definition.displayName,
            prefab = definition.heldPrefab != null ? definition.heldPrefab : definition.worldPrefab,
            worldPrefab = definition.worldPrefab != null ? definition.worldPrefab : definition.heldPrefab,
            actionType = ToolActionType.None,
            category = definition.category,
            holdPose = definition.holdPose,
            heldLocalPosition = definition.heldLocalPosition,
            heldLocalEuler = definition.heldLocalEuler,
            heldLocalScale = definition.heldLocalScale,
            stackCount = amount,
            maxStack = Mathf.Max(1, definition.maxStack),
            placeable = definition.placeable,
            placeableType = definition.placeableType,
            worldEulerOffset = definition.worldEulerOffset,
            worldScale = definition.worldScale == Vector3.zero ? Vector3.one : definition.worldScale,
            durability = definition.durability,
            maxDurability = definition.durability,
            damage = definition.damage,
            defense = definition.defense,
            healAmount = definition.healAmount,
            harvestSeconds = definition.harvestSeconds,
            nonGunDirectionFlipped = true
        };
    }

    public static ToolSlot CloneSlot(ToolSlot source, int amount)
    {
        if (source == null)
        {
            return null;
        }

        return new ToolSlot
        {
            itemId = source.itemId,
            displayName = source.displayName,
            prefab = source.prefab,
            worldPrefab = source.worldPrefab,
            actionType = source.actionType,
            category = source.category,
            holdPose = source.holdPose,
            heldLocalPosition = source.heldLocalPosition,
            heldLocalEuler = source.heldLocalEuler,
            heldLocalScale = source.heldLocalScale,
            rightHandGripLocal = source.rightHandGripLocal,
            leftHandGripLocal = source.leftHandGripLocal,
            stackCount = amount,
            maxStack = Mathf.Max(1, source.maxStack),
            placeable = source.placeable,
            placeableType = source.placeableType,
            worldEulerOffset = source.worldEulerOffset,
            worldScale = source.worldScale == Vector3.zero ? Vector3.one : source.worldScale,
            durability = source.durability,
            maxDurability = source.maxDurability,
            damage = source.damage,
            defense = source.defense,
            healAmount = source.healAmount,
            harvestSeconds = source.harvestSeconds,
            nonGunDirectionFlipped = source.nonGunDirectionFlipped
        };
    }

    public static bool CanStack(ToolSlot a, ToolSlot b)
    {
        return a != null
            && b != null
            && Mathf.Max(1, a.maxStack) > 1
            && Mathf.Max(1, b.maxStack) > 1
            && GetItemId(a) == GetItemId(b);
    }

    public static ToolSlot[] AddItem(ToolSlot[] slots, ToolSlot incoming)
    {
        if (!IsValidSlot(incoming))
        {
            return Compact(slots);
        }

        List<ToolSlot> result = new List<ToolSlot>(slots ?? new ToolSlot[0]);
        int remaining = incoming.stackCount;

        for (int i = 0; i < result.Count && remaining > 0; i++)
        {
            ToolSlot existing = result[i];
            if (!CanStack(existing, incoming))
            {
                continue;
            }

            int capacity = Mathf.Max(1, existing.maxStack) - existing.stackCount;
            if (capacity <= 0)
            {
                continue;
            }

            int moved = Mathf.Min(capacity, remaining);
            existing.stackCount += moved;
            remaining -= moved;
        }

        while (remaining > 0)
        {
            int stackAmount = Mathf.Min(Mathf.Max(1, incoming.maxStack), remaining);
            result.Add(CloneSlot(incoming, stackAmount));
            remaining -= stackAmount;
        }

        return Compact(result.ToArray());
    }

    public static bool CanAddItem(ToolSlot[] slots, ToolSlot incoming, int maxSlots)
    {
        if (!IsValidSlot(incoming))
        {
            return true;
        }

        if (maxSlots <= 0)
        {
            return true;
        }

        List<ToolSlot> result = new List<ToolSlot>(Compact(slots));
        int remaining = incoming.stackCount;

        for (int i = 0; i < result.Count && remaining > 0; i++)
        {
            ToolSlot existing = result[i];
            if (!CanStack(existing, incoming))
            {
                continue;
            }

            int capacity = Mathf.Max(1, existing.maxStack) - existing.stackCount;
            int moved = Mathf.Min(Mathf.Max(0, capacity), remaining);
            remaining -= moved;
        }

        int maxStack = Mathf.Max(1, incoming.maxStack);
        int neededNewSlots = Mathf.CeilToInt(remaining / (float)maxStack);
        return result.Count + neededNewSlots <= maxSlots;
    }

    public static bool RemoveItem(ToolSlot[] slots, string itemId, int amount, out ToolSlot[] result)
    {
        result = Compact(slots);
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
        {
            return false;
        }

        if (CountItem(result, itemId) < amount)
        {
            return false;
        }

        int remaining = amount;
        for (int i = result.Length - 1; i >= 0 && remaining > 0; i--)
        {
            ToolSlot slot = result[i];
            if (slot == null || GetItemId(slot) != itemId)
            {
                continue;
            }

            int removed = Mathf.Min(slot.stackCount, remaining);
            slot.stackCount -= removed;
            remaining -= removed;
        }

        result = Compact(result);
        return true;
    }

    public static bool IsValidSlot(ToolSlot slot)
    {
        if (slot == null || slot.stackCount <= 0)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(GetItemId(slot))
            || !string.IsNullOrWhiteSpace(slot.displayName)
            || slot.prefab != null
            || slot.worldPrefab != null;
    }

    public static ToolSlot[] Compact(ToolSlot[] slots)
    {
        if (slots == null)
        {
            return new ToolSlot[0];
        }

        List<ToolSlot> compact = new List<ToolSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (IsValidSlot(slot))
            {
                compact.Add(slot);
            }
        }

        return compact.ToArray();
    }
}



