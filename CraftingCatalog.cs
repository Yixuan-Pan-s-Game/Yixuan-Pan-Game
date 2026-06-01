using System.Collections.Generic;
using UnityEngine;

public enum PlaceableType
{
    None,
    Workbench,
    Furnace,
    Forge,
    Chest,
    Chair,
    Bed,
    Door,
    LargePlank,
    Fence,
    Torch
}

public enum CraftingStation
{
    Player,
    Workbench,
    Furnace,
    Forge
}

[System.Serializable]
public class CraftingItemDefinition
{
    public string itemId;
    public string displayName;
    public GameObject heldPrefab;
    public GameObject worldPrefab;
    public ToolCategory category = ToolCategory.Crafted;
    public ToolHoldPose holdPose = ToolHoldPose.OneHandTool;
    public Vector3 heldLocalPosition = new Vector3(0.03f, -0.02f, 0.05f);
    public Vector3 heldLocalEuler = new Vector3(0f, 160f, -18f);
    public Vector3 heldLocalScale = Vector3.one * 0.35f;
    public int maxStack = 99;
    public bool placeable = true;
    public PlaceableType placeableType = PlaceableType.None;
    public Vector3 worldEulerOffset;
    public Vector3 worldScale = Vector3.one;
    public int durability;
    public int damage;
    public int defense;
    public int healAmount;
    public float harvestSeconds;
}

[System.Serializable]
public class RecipeIngredient
{
    public string itemId;
    public int amount;
}

[System.Serializable]
public class CraftingRecipe
{
    public string recipeId;
    public string displayName;
    public string outputItemId;
    public int outputCount = 1;
    public bool requiresWorkbench;
    public CraftingStation station = CraftingStation.Player;
    [TextArea(2, 4)] public string description;
    public RecipeIngredient[] ingredients;
}

public class CraftingCatalog : MonoBehaviour
{
    public CraftingItemDefinition[] itemDefinitions;
    public CraftingRecipe[] recipes;

    private readonly Dictionary<string, CraftingItemDefinition> itemLookup = new Dictionary<string, CraftingItemDefinition>();

    private void Awake()
    {
        RebuildLookup();
    }

    public void RebuildLookup()
    {
        itemLookup.Clear();
        if (itemDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < itemDefinitions.Length; i++)
        {
            CraftingItemDefinition definition = itemDefinitions[i];
            if (definition == null || string.IsNullOrEmpty(definition.itemId))
            {
                continue;
            }

            itemLookup[definition.itemId] = definition;
        }
    }

    public CraftingItemDefinition FindItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        if (itemLookup.Count == 0)
        {
            RebuildLookup();
        }

        itemLookup.TryGetValue(itemId, out CraftingItemDefinition definition);
        return definition;
    }

    public List<CraftingRecipe> GetRecipes(bool requiresWorkbench)
    {
        return GetRecipes(requiresWorkbench ? CraftingStation.Workbench : CraftingStation.Player);
    }

    public List<CraftingRecipe> GetRecipes(CraftingStation station)
    {
        List<CraftingRecipe> results = new List<CraftingRecipe>();
        if (recipes == null)
        {
            return results;
        }

        for (int i = 0; i < recipes.Length; i++)
        {
            CraftingRecipe recipe = recipes[i];
            if (recipe == null)
            {
                continue;
            }

            CraftingStation recipeStation = recipe.station;
            if (recipe.requiresWorkbench && recipe.station == CraftingStation.Player)
            {
                recipeStation = CraftingStation.Workbench;
            }

            if (recipeStation == station)
            {
                results.Add(recipe);
            }
        }

        return results;
    }
}
