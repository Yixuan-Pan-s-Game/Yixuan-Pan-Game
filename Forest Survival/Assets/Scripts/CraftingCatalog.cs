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
// crafting item definition script that owns this feature's runtime behavior.
public class CraftingItemDefinition
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: itemId.
    public string itemId;
    // Runtime flag that drives control flow, UI state, or gameplay availability: displayName.
    public string displayName;
    // Asset reference used for spawning, rendering, audio, or animation: heldPrefab.
    public GameObject heldPrefab;
    // Asset reference used for spawning, rendering, audio, or animation: worldPrefab.
    public GameObject worldPrefab;
    // Identifier or category used for lookup, routing, or state selection: category.
    public ToolCategory category = ToolCategory.Crafted;
    // Important runtime data or configuration used by this component: holdPose.
    public ToolHoldPose holdPose = ToolHoldPose.OneHandTool;
    public Vector3 heldLocalPosition = new Vector3(0.03f, -0.02f, 0.05f);
    public Vector3 heldLocalEuler = new Vector3(0f, 160f, -18f);
    // Spatial value used for positioning, rotation, scale, or collision math: heldLocalScale.
    public Vector3 heldLocalScale = Vector3.one * 0.35f;
    // Inventory or crafting data for items, recipes, slots, or stack counts: maxStack.
    public int maxStack = 99;
    // Important runtime data or configuration used by this component: placeable.
    public bool placeable = true;
    // Identifier or category used for lookup, routing, or state selection: placeableType.
    public PlaceableType placeableType = PlaceableType.None;
    // Spatial value used for positioning, rotation, scale, or collision math: worldEulerOffset.
    public Vector3 worldEulerOffset;
    // Spatial value used for positioning, rotation, scale, or collision math: worldScale.
    public Vector3 worldScale = Vector3.one;
    // Gameplay stat that affects damage, health, healing, defense, or durability: durability.
    public int durability;
    // Gameplay stat that affects damage, health, healing, defense, or durability: damage.
    public int damage;
    // Gameplay stat that affects damage, health, healing, defense, or durability: defense.
    public int defense;
    // Gameplay stat that affects damage, health, healing, defense, or durability: healAmount.
    public int healAmount;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: harvestSeconds.
    public float harvestSeconds;
}

[System.Serializable]
// recipe ingredient script that owns this feature's runtime behavior.
public class RecipeIngredient
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: itemId.
    public string itemId;
    // Inventory or crafting data for items, recipes, slots, or stack counts: amount.
    public int amount;
}

[System.Serializable]
// crafting recipe script that owns this feature's runtime behavior.
public class CraftingRecipe
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: recipeId.
    public string recipeId;
    // Runtime flag that drives control flow, UI state, or gameplay availability: displayName.
    public string displayName;
    // Inventory or crafting data for items, recipes, slots, or stack counts: outputItemId.
    public string outputItemId;
    // Inventory or crafting data for items, recipes, slots, or stack counts: outputCount.
    public int outputCount = 1;
    // Important runtime data or configuration used by this component: requiresWorkbench.
    public bool requiresWorkbench;
    // Important runtime data or configuration used by this component: station.
    public CraftingStation station = CraftingStation.Player;
    [TextArea(2, 4)] public string description;
    // Important runtime data or configuration used by this component: ingredients.
    public RecipeIngredient[] ingredients;
}

// Catalog of craftable items and recipes, including default data and lookup helpers.
public class CraftingCatalog : MonoBehaviour
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: itemDefinitions.
    public CraftingItemDefinition[] itemDefinitions;
    // Inventory or crafting data for items, recipes, slots, or stack counts: recipes.
    public CraftingRecipe[] recipes;

    private readonly Dictionary<string, CraftingItemDefinition> itemLookup = new Dictionary<string, CraftingItemDefinition>();

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        EnsureDefaultsIfEmpty();
        RebuildLookup();
    }

    // Ensures the objects, references, or configuration required for ensure defaults if empty exist.
    public void EnsureDefaultsIfEmpty()
    {
        bool needsDefinitions = itemDefinitions == null || itemDefinitions.Length == 0 || FindDefinitionInArray("workbench") == null || FindDefinitionInArray("forest_heart_detector") == null;
        bool needsRecipes = recipes == null || recipes.Length == 0 || FindRecipeInArray("craft_workbench") == null || FindRecipeInArray("bench_furnace") == null || FindRecipeInArray("forge_iron_bar") == null;

        if (needsDefinitions)
        {
            itemDefinitions = CreateDefaultItemDefinitions();
        }

        if (needsRecipes)
        {
            recipes = CreateDefaultRecipes();
        }
    }

    // Handles the rebuild lookup workflow.
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

    // Finds, loads, or caches the references needed for find item.
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


    // Calculates and returns the result for get recipes.
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

    // Finds, loads, or caches the references needed for find definition in array.
    private CraftingItemDefinition FindDefinitionInArray(string itemId)
    {
        if (itemDefinitions == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        for (int i = 0; i < itemDefinitions.Length; i++)
        {
            CraftingItemDefinition definition = itemDefinitions[i];
            if (definition != null && definition.itemId == itemId)
            {
                return definition;
            }
        }

        return null;
    }

    // Finds, loads, or caches the references needed for find recipe in array.
    private CraftingRecipe FindRecipeInArray(string recipeId)
    {
        if (recipes == null || string.IsNullOrEmpty(recipeId))
        {
            return null;
        }

        for (int i = 0; i < recipes.Length; i++)
        {
            CraftingRecipe recipe = recipes[i];
            if (recipe != null && recipe.recipeId == recipeId)
            {
                return recipe;
            }
        }

        return null;
    }

    // Finds, loads, or caches the references needed for load prefab asset.
    private static GameObject LoadPrefabAsset(string assetPath, string resourcesPath)
    {
#if UNITY_EDITOR
        GameObject editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (editorAsset != null)
        {
            return editorAsset;
        }
#endif
        return string.IsNullOrEmpty(resourcesPath) ? null : Resources.Load<GameObject>(resourcesPath);
    }
    // Creates or rebuilds the runtime objects, assets, or UI for create default item definitions.
    private static CraftingItemDefinition[] CreateDefaultItemDefinitions()
    {
        return new[]
        {
            Item("wood", "Wood", ToolCategory.Materials, 64),
            Item("stone", "Stone", ToolCategory.Materials, 64, Resources.Load<GameObject>("RuntimePrefabs/Held_LowPolyStone"), false, PlaceableType.None, Vector3.one, 0, 0, 0, 0, 0f, null, null, new Vector3(0.01f, 0.34f, 0.01f)),
            Item("herb", "Herb", ToolCategory.Materials, 64),
            Item("stick", "Stick", ToolCategory.Materials, 64, Resources.Load<GameObject>("RuntimePrefabs/Held_Stick"), heldScale: Vector3.one * 0.05f),
            Item("workbench", "Workbench", ToolCategory.Crafted, 64, null, true, PlaceableType.Workbench, Vector3.one * 50f),
            Item("chest", "Chest", ToolCategory.Crafted, 64, LoadPrefabAsset("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Treasure_Chest_04_Blue.prefab", "RuntimePrefabs/PP_Treasure_Chest_01_Blue"), true, PlaceableType.Chest),
            Item("door", "Door", ToolCategory.Crafted, 64, LoadPrefabAsset("Assets/Free Wood Door Pack/Prefab/Wood/Door_1/Door_1_Brown.prefab", "RuntimePrefabs/Door_1_Brown"), true, PlaceableType.Door),
            Item("wood_fence", "Wood Fence", ToolCategory.Crafted, 64, null, true, PlaceableType.Fence, Vector3.one * 1.2f),
            Item("bed", "Bed", ToolCategory.Crafted, 64, null, true, PlaceableType.Bed),
            Item("torch", "Torch", ToolCategory.Materials, 64, null, true, PlaceableType.Torch, Vector3.one * 0.85f),
            Item("bandage", "Bandage", ToolCategory.Consumable, 64, null, false, PlaceableType.None, Vector3.one, 0, 0, 0, 10),
            Item("first_aid_kit", "First Aid Kit", ToolCategory.Consumable, 64, null, false, PlaceableType.None, Vector3.one, 0, 0, 0, 60),
            Item("furnace", "Furnace", ToolCategory.Crafted, 64, null, true, PlaceableType.Furnace),
            Item("auto_forge", "Auto Forge", ToolCategory.Crafted, 64, Resources.Load<GameObject>("AutoForge/Forge"), true, PlaceableType.Forge, Vector3.one * 0.6f),
            Item("wood_axe", "Wood Axe", ToolCategory.Tools, 1, null, false, PlaceableType.None, Vector3.one, 15, 12, 0, 0, 8f),
            Item("wood_pickaxe", "Wood Pickaxe", ToolCategory.Tools, 1, null, false, PlaceableType.None, Vector3.one, 20, 12, 0, 0, 5f),
            Item("wood_sword", "Wood Sword", ToolCategory.Melee, 1, null, false, PlaceableType.None, Vector3.one, 15, 20),
            Item("stone_axe", "Stone Axe", ToolCategory.Tools, 1, null, false, PlaceableType.None, Vector3.one, 15, 13, 0, 0, 5f),
            Item("stone_pickaxe", "Stone Pickaxe", ToolCategory.Tools, 1, null, false, PlaceableType.None, Vector3.one, 20, 13, 0, 0, 5f),
            Item("stone_sword", "Stone Sword", ToolCategory.Melee, 1, null, false, PlaceableType.None, Vector3.one, 15, 15),
            Item("coal_ore", "Coal Ore", ToolCategory.Materials, 64),
            Item("coal", "Coal", ToolCategory.Materials, 64),
            Item("charcoal", "Charcoal", ToolCategory.Materials, 64),
            Item("iron_ore", "Iron Ore", ToolCategory.Materials, 64),
            Item("iron_bar", "Iron Bar", ToolCategory.Materials, 64, Resources.Load<GameObject>("RuntimePrefabs/Held_IronBar")),
            Item("repair_armor_kit", "Repair Armor Kit", ToolCategory.Materials, 64),
            Item("scissors", "Scissors", ToolCategory.Tools, 1),
            Item("iron_axe", "Iron Axe", ToolCategory.Tools, 1, null, false, PlaceableType.None, Vector3.one, 15, 15, 0, 0, 3f),
            Item("iron_pickaxe", "Iron Pickaxe", ToolCategory.Tools, 1, null, false, PlaceableType.None, Vector3.one, 20, 15, 0, 0, 5f),
            Item("iron_sword", "Iron Sword", ToolCategory.Melee, 1, null, false, PlaceableType.None, Vector3.one, 15, 18),
            Item("forest_guide", "Forest Guide", ToolCategory.Materials, 64, Resources.Load<GameObject>("RuntimePrefabs/Held_ForestGuide")),
            Item("forest_heart", "Forest Heart", ToolCategory.Materials, 1, Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeart")),
            Item("forest_heart_detector", "Forest Heart Detector", ToolCategory.Tools, 1, Resources.Load<GameObject>("RuntimePrefabs/Held_ForestHeartDetector"), false, PlaceableType.None, Vector3.one, 0, 0, 0, 0, 0f, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(20f, 150f, -12f), Vector3.one * 0.26f),
            Item("wood_helmet", "Wood Helmet", ToolCategory.Armor, 1, null, false, PlaceableType.None, Vector3.one, 50, 0, 5),
            Item("wood_armor", "Wood Armor", ToolCategory.Armor, 1, null, false, PlaceableType.None, Vector3.one, 50, 0, 10),
            Item("stone_helmet", "Stone Helmet", ToolCategory.Armor, 1, null, false, PlaceableType.None, Vector3.one, 70, 0, 8),
            Item("stone_armor", "Stone Armor", ToolCategory.Armor, 1, null, false, PlaceableType.None, Vector3.one, 70, 0, 12),
            Item("iron_helmet", "Iron Helmet", ToolCategory.Armor, 1, null, false, PlaceableType.None, Vector3.one, 80, 0, 15),
            Item("iron_armor", "Iron Armor", ToolCategory.Armor, 1, null, false, PlaceableType.None, Vector3.one, 80, 0, 23),
            Item("ruby_ore", "Ruby Ore", ToolCategory.Materials, 64),
            Item("sapphire_ore", "Sapphire Ore", ToolCategory.Materials, 64),
            Item("emerald_ore", "Emerald Ore", ToolCategory.Materials, 64),
            Item("copper_ore", "Copper Ore", ToolCategory.Materials, 64),
            Item("gold_ore", "Gold Ore", ToolCategory.Materials, 64),
            Item("ruby_axe", "Ruby Axe", ToolCategory.Tools, 1),
            Item("sapphire_axe", "Sapphire Axe", ToolCategory.Tools, 1),
            Item("emerald_axe", "Emerald Axe", ToolCategory.Tools, 1),
            Item("copper_axe", "Copper Axe", ToolCategory.Tools, 1),
            Item("gold_axe", "Gold Axe", ToolCategory.Tools, 1),
            Item("ruby_pickaxe", "Ruby Pickaxe", ToolCategory.Tools, 1),
            Item("sapphire_pickaxe", "Sapphire Pickaxe", ToolCategory.Tools, 1),
            Item("emerald_pickaxe", "Emerald Pickaxe", ToolCategory.Tools, 1),
            Item("copper_pickaxe", "Copper Pickaxe", ToolCategory.Tools, 1),
            Item("gold_pickaxe", "Gold Pickaxe", ToolCategory.Tools, 1),
            Item("ruby_sword", "Ruby Sword", ToolCategory.Melee, 1),
            Item("sapphire_sword", "Sapphire Sword", ToolCategory.Melee, 1),
            Item("emerald_sword", "Emerald Sword", ToolCategory.Melee, 1),
            Item("copper_sword", "Copper Sword", ToolCategory.Melee, 1),
            Item("gold_sword", "Gold Sword", ToolCategory.Melee, 1),
            Item("wool", "Wool", ToolCategory.Materials, 64)
        };
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create default recipes.
    private static CraftingRecipe[] CreateDefaultRecipes()
    {
        return new[]
        {
            Recipe("craft_workbench", "Workbench", "workbench", 1, CraftingStation.Player, "Player crafting: 5 Wood => 1 Workbench", Ing("wood", 5)),
            Recipe("craft_stick", "Stick", "stick", 3, CraftingStation.Player, "Player crafting: 1 Wood => 3 Stick", Ing("wood", 1)),
            Recipe("craft_torch", "Torch", "torch", 3, CraftingStation.Player, "1 Coal + 3 Stick => 3 Torch", Ing("coal", 1), Ing("stick", 3)),
            Recipe("craft_bandage", "Bandage", "bandage", 1, CraftingStation.Player, "3 Herb => 1 Bandage, heals 10", Ing("herb", 3)),
            Recipe("craft_first_aid", "First Aid Kit", "first_aid_kit", 1, CraftingStation.Player, "6 Herb => 1 First Aid Kit, heals 60", Ing("herb", 6)),
            Recipe("bench_workbench", "Workbench", "workbench", 1, CraftingStation.Workbench, "Workbench crafting: 5 Wood => 1 Workbench", Ing("wood", 5)),
            Recipe("bench_stick", "Stick", "stick", 3, CraftingStation.Workbench, "Workbench crafting: 1 Wood => 3 Stick", Ing("wood", 1)),
            Recipe("bench_wood_axe", "Wood Axe", "wood_axe", 1, CraftingStation.Workbench, "3 Wood + 2 Stick => Wood Axe", Ing("wood", 3), Ing("stick", 2)),
            Recipe("bench_wood_pickaxe", "Wood Pickaxe", "wood_pickaxe", 1, CraftingStation.Workbench, "4 Wood + 2 Stick => Wood Pickaxe", Ing("wood", 4), Ing("stick", 2)),
            Recipe("bench_wood_sword", "Wood Sword", "wood_sword", 1, CraftingStation.Workbench, "6 Wood + 3 Stick => Wood Sword", Ing("wood", 6), Ing("stick", 3)),
            Recipe("bench_bed", "Bed", "bed", 1, CraftingStation.Workbench, "6 Wood + 2 Stick + 2 Wool => Bed", Ing("wood", 6), Ing("stick", 2), Ing("wool", 2)),
            Recipe("bench_chest", "Chest", "chest", 1, CraftingStation.Workbench, "8 Wood => Chest", Ing("wood", 8)),
            Recipe("bench_door", "Door", "door", 1, CraftingStation.Workbench, "6 Wood => Door", Ing("wood", 6)),
            Recipe("bench_fence", "Wood Fence", "wood_fence", 2, CraftingStation.Workbench, "4 Wood + 3 Stick => 2 Fences", Ing("wood", 4), Ing("stick", 3)),
            Recipe("bench_furnace", "Furnace", "furnace", 1, CraftingStation.Workbench, "8 Stone => Furnace", Ing("stone", 8)),
            Recipe("bench_stone_axe", "Stone Axe", "stone_axe", 1, CraftingStation.Workbench, "3 Stone + 2 Stick => Stone Axe", Ing("stone", 3), Ing("stick", 2)),
            Recipe("bench_stone_pickaxe", "Stone Pickaxe", "stone_pickaxe", 1, CraftingStation.Workbench, "3 Stone + 2 Stick => Stone Pickaxe", Ing("stone", 3), Ing("stick", 2)),
            Recipe("bench_stone_sword", "Stone Sword", "stone_sword", 1, CraftingStation.Workbench, "6 Stone + 1 Stick => Stone Sword", Ing("stone", 6), Ing("stick", 1)),
            Recipe("bench_auto_forge", "Auto Forge", "auto_forge", 1, CraftingStation.Workbench, "6 Iron Ore + 4 Stone + 2 Stick + 2 Stone Hammer => Auto Forge", Ing("iron_ore", 6), Ing("stone", 4), Ing("stick", 2), Ing("stone_hammer", 2)),
            Recipe("bench_wood_helmet", "Wood Helmet", "wood_helmet", 1, CraftingStation.Workbench, "8 Wood => Wood Helmet", Ing("wood", 8)),
            Recipe("bench_wood_armor", "Wood Armor", "wood_armor", 1, CraftingStation.Workbench, "12 Wood => Wood Armor", Ing("wood", 12)),
            Recipe("bench_stone_helmet", "Stone Helmet", "stone_helmet", 1, CraftingStation.Workbench, "8 Stone => Stone Helmet", Ing("stone", 8)),
            Recipe("bench_stone_armor", "Stone Armor", "stone_armor", 1, CraftingStation.Workbench, "12 Stone => Stone Armor", Ing("stone", 12)),
            Recipe("forge_forest_heart_detector", "Forest Heart Detector", "forest_heart_detector", 1, CraftingStation.Forge, "30 Forest Guide + 6 Iron Ore => Forest Heart Detector", Ing("forest_guide", 30), Ing("iron_ore", 6)),
            Recipe("furnace_charcoal", "Charcoal", "charcoal", 1, CraftingStation.Furnace, "1 Wood => 1 Charcoal. One coal or charcoal fuels 3 furnace crafts.", Ing("wood", 1)),
            Recipe("furnace_coal", "Coal", "coal", 1, CraftingStation.Furnace, "1 Coal Ore => 1 Coal. One coal fuels 3 furnace crafts.", Ing("coal_ore", 1)),
            Recipe("forge_iron_bar", "Iron Bar", "iron_bar", 3, CraftingStation.Forge, "1 Iron Ore => 3 Iron Bars. Auto forge consumes 2 Coal Ore per craft.", Ing("iron_ore", 1)),
            Recipe("forge_iron_axe", "Iron Axe", "iron_axe", 1, CraftingStation.Forge, "3 Iron Ore + 2 Stick => Iron Axe", Ing("iron_ore", 3), Ing("stick", 2)),
            Recipe("forge_iron_pickaxe", "Iron Pickaxe", "iron_pickaxe", 1, CraftingStation.Forge, "3 Iron Ore + 2 Stick => Iron Pickaxe", Ing("iron_ore", 3), Ing("stick", 2)),
            Recipe("forge_iron_sword", "Iron Sword", "iron_sword", 1, CraftingStation.Forge, "2 Iron Ore + 1 Stick => Iron Sword", Ing("iron_ore", 2), Ing("stick", 1)),
            Recipe("forge_repair_armor_kit", "Repair Armor Kit", "repair_armor_kit", 2, CraftingStation.Forge, "1 Iron Ore => 2 Repair Armor Kits", Ing("iron_ore", 1)),
            Recipe("forge_scissors", "Scissors", "scissors", 1, CraftingStation.Forge, "1 Iron Ore => Scissors", Ing("iron_ore", 1)),
            Recipe("forge_iron_helmet", "Iron Helmet", "iron_helmet", 1, CraftingStation.Forge, "8 Iron Ore => Iron Helmet", Ing("iron_ore", 8)),
            Recipe("forge_iron_armor", "Iron Armor", "iron_armor", 1, CraftingStation.Forge, "12 Iron Ore => Iron Armor", Ing("iron_ore", 12)),
            Recipe("forge_forest_heart", "Forest Heart", "forest_heart", 1, CraftingStation.Forge, "30 Forest Guide + 6 Iron Ore => Forest Heart", Ing("forest_guide", 30), Ing("iron_ore", 6)),
            Recipe("forge_ruby_axe", "Ruby Axe", "ruby_axe", 1, CraftingStation.Forge, "3 Ruby Ore + 2 Iron Bars => Ruby Axe", Ing("ruby_ore", 3), Ing("iron_bar", 2)),
            Recipe("forge_sapphire_axe", "Sapphire Axe", "sapphire_axe", 1, CraftingStation.Forge, "3 Sapphire Ore + 2 Iron Bars => Sapphire Axe", Ing("sapphire_ore", 3), Ing("iron_bar", 2)),
            Recipe("forge_emerald_axe", "Emerald Axe", "emerald_axe", 1, CraftingStation.Forge, "3 Emerald Ore + 2 Iron Bars => Emerald Axe", Ing("emerald_ore", 3), Ing("iron_bar", 2)),
            Recipe("forge_copper_axe", "Copper Axe", "copper_axe", 1, CraftingStation.Forge, "3 Copper Ore + 2 Iron Bars => Copper Axe", Ing("copper_ore", 3), Ing("iron_bar", 2)),
            Recipe("forge_gold_axe", "Gold Axe", "gold_axe", 1, CraftingStation.Forge, "3 Gold Ore + 2 Iron Bars => Gold Axe", Ing("gold_ore", 3), Ing("iron_bar", 2)),
            Recipe("forge_ruby_pickaxe", "Ruby Pickaxe", "ruby_pickaxe", 1, CraftingStation.Forge, "4 Ruby Ore + 2 Iron Bars => Ruby Pickaxe", Ing("ruby_ore", 4), Ing("iron_bar", 2)),
            Recipe("forge_sapphire_pickaxe", "Sapphire Pickaxe", "sapphire_pickaxe", 1, CraftingStation.Forge, "4 Sapphire Ore + 2 Iron Bars => Sapphire Pickaxe", Ing("sapphire_ore", 4), Ing("iron_bar", 2)),
            Recipe("forge_emerald_pickaxe", "Emerald Pickaxe", "emerald_pickaxe", 1, CraftingStation.Forge, "4 Emerald Ore + 2 Iron Bars => Emerald Pickaxe", Ing("emerald_ore", 4), Ing("iron_bar", 2)),
            Recipe("forge_copper_pickaxe", "Copper Pickaxe", "copper_pickaxe", 1, CraftingStation.Forge, "4 Copper Ore + 2 Iron Bars => Copper Pickaxe", Ing("copper_ore", 4), Ing("iron_bar", 2)),
            Recipe("forge_gold_pickaxe", "Gold Pickaxe", "gold_pickaxe", 1, CraftingStation.Forge, "4 Gold Ore + 2 Iron Bars => Gold Pickaxe", Ing("gold_ore", 4), Ing("iron_bar", 2)),
            Recipe("forge_ruby_sword", "Ruby Sword", "ruby_sword", 1, CraftingStation.Forge, "3 Ruby Ore + 1 Iron Bar => Ruby Sword", Ing("ruby_ore", 3), Ing("iron_bar", 1)),
            Recipe("forge_sapphire_sword", "Sapphire Sword", "sapphire_sword", 1, CraftingStation.Forge, "3 Sapphire Ore + 1 Iron Bar => Sapphire Sword", Ing("sapphire_ore", 3), Ing("iron_bar", 1)),
            Recipe("forge_emerald_sword", "Emerald Sword", "emerald_sword", 1, CraftingStation.Forge, "3 Emerald Ore + 1 Iron Bar => Emerald Sword", Ing("emerald_ore", 3), Ing("iron_bar", 1)),
            Recipe("forge_copper_sword", "Copper Sword", "copper_sword", 1, CraftingStation.Forge, "3 Copper Ore + 1 Iron Bar => Copper Sword", Ing("copper_ore", 3), Ing("iron_bar", 1)),
            Recipe("forge_gold_sword", "Gold Sword", "gold_sword", 1, CraftingStation.Forge, "3 Gold Ore + 1 Iron Bar => Gold Sword", Ing("gold_ore", 3), Ing("iron_bar", 1))
        };
    }

    // Handles the item workflow.
    // AI teaches me how to biding the codes to the tools
    private static CraftingItemDefinition Item(string id, string name, ToolCategory category, int maxStack, GameObject prefab = null, bool placeable = false, PlaceableType placeableType = PlaceableType.None, Vector3? worldScale = null, int durability = 0, int damage = 0, int defense = 0, int heal = 0, float harvestSeconds = 0f, Vector3? heldPosition = null, Vector3? heldEuler = null, Vector3? heldScale = null)
    {
        return new CraftingItemDefinition
        {
            itemId = id,
            displayName = name,
            heldPrefab = prefab,
            worldPrefab = prefab,
            category = category,
            maxStack = Mathf.Max(1, maxStack),
            placeable = placeable,
            placeableType = placeableType,
            worldScale = worldScale ?? Vector3.one,
            durability = durability,
            damage = damage,
            defense = defense,
            healAmount = heal,
            harvestSeconds = harvestSeconds,
            heldLocalPosition = heldPosition ?? new Vector3(0.03f, -0.02f, 0.05f),
            heldLocalEuler = heldEuler ?? new Vector3(0f, 160f, -18f),
            heldLocalScale = heldScale ?? Vector3.one * 0.35f
        };
    }

    // Handles the ing workflow.
    private static RecipeIngredient Ing(string itemId, int amount)
    {
        return new RecipeIngredient { itemId = itemId, amount = amount };
    }

    // Handles the recipe workflow.
    private static CraftingRecipe Recipe(string id, string name, string output, int count, CraftingStation station, string description, params RecipeIngredient[] ingredients)
    {
        return new CraftingRecipe
        {
            recipeId = id,
            displayName = name,
            outputItemId = output,
            outputCount = count,
            station = station,
            requiresWorkbench = station == CraftingStation.Workbench,
            description = description,
            ingredients = ingredients
        };
    }
}




