using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ForestGameplayInstaller
{
    private const string GeneratedFolder = "Assets/Generated";
    private const string HeldCraftsFolder = "Assets/Generated/HeldCrafts";
    private const string GeneratedMaterialsFolder = "Assets/Generated/Materials";
    private const string PlayerVisualPrefabPath = "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_Armors/PT_Lowpoly_Armors_Male_Moduar_Free.prefab";
    private const string AutoInstallSessionKey = "ForestGameplayInstaller.AutoInstalledActiveScene";
    private const string OldSpawnPlatformName = "FishingVillagePlayerSpawnGround";
    private static readonly Vector3 FallbackSpawnPosition = new Vector3(0f, 1f, 0f);
    private static readonly float[] SpawnExtraDistances = { 4f, 7f, 10f, 14f, 20f, 28f };
    private const int SpawnAngleSamples = 24;

    static ForestGameplayInstaller()
    {
        EditorApplication.delayCall -= AutoInstallActiveForestSceneIfNeeded;
        EditorApplication.playModeStateChanged -= InstallBeforePlayIfNeeded;
        EditorApplication.delayCall += UpgradeStickPrefabsInOpenScene;
    }

    private static void UpgradeStickPrefabsInOpenScene()
    {
        EditorApplication.delayCall -= UpgradeStickPrefabsInOpenScene;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject stick = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Blacksmith House/PP_Roof_Top_04.prefab");
        if (stick == null)
        {
            return;
        }

        EnsureGeneratedFolders();
        GameObject ironBar = CreateColoredToolPrefab("Held_IronBar.prefab", stick, "IronBarBlack.mat", new Color(0.035f, 0.04f, 0.045f));
        bool changed = false;
        CraftingCatalog[] catalogs = Object.FindObjectsOfType<CraftingCatalog>(true);
        for (int i = 0; i < catalogs.Length; i++)
        {
            CraftingCatalog catalog = catalogs[i];
            if (catalog == null || catalog.itemDefinitions == null)
            {
                continue;
            }

            for (int definitionIndex = 0; definitionIndex < catalog.itemDefinitions.Length; definitionIndex++)
            {
                CraftingItemDefinition definition = catalog.itemDefinitions[definitionIndex];
                if (definition == null)
                {
                    continue;
                }

                GameObject replacement = definition.itemId == "stick" ? stick : definition.itemId == "iron_bar" ? ironBar : null;
                if (replacement == null || definition.heldPrefab == replacement && definition.worldPrefab == replacement)
                {
                    continue;
                }

                definition.heldPrefab = replacement;
                definition.worldPrefab = replacement;
                changed = true;
            }

            if (changed)
            {
                catalog.RebuildLookup();
                EditorUtility.SetDirty(catalog);
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("ForestGameplayInstaller: Updated Stick and Iron Bar prefabs without reinstalling gameplay.");
        }
    }

    private static void InstallBeforePlayIfNeeded(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode || EditorApplication.isCompiling)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!IsSupportedGameplayScene(scene))
        {
            return;
        }

        InstallGameplayInOpenScene();
        EditorSceneManager.SaveScene(scene);
    }

    private static void AutoInstallActiveForestSceneIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!IsSupportedGameplayScene(scene))
        {
            return;
        }

        string sessionKey = AutoInstallSessionKey + "." + scene.path;
        if (SessionState.GetBool(sessionKey, false) && HasGame1StylePlayerSetup() && !NeedsTestSetupRefresh())
        {
            return;
        }

        InstallGameplayInOpenScene();
        EditorSceneManager.SaveScene(scene);
        SessionState.SetBool(sessionKey, true);
    }

    [MenuItem("Tools/Forest/Install Game1 Gameplay In Open Scene")]
    public static void InstallGameplayInOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("ForestGameplayInstaller: No valid active scene.");
            return;
        }

        EnsureGeneratedFolders();
        EnsureCraftingCatalog();
        GameObject player = EnsurePlayer();
        EnsureCamera(player.transform);
        EnsureSingleAudioListener();
        EnsureCrosshair();
        EnsureEventSystem();
        EnsureHarvestDropBootstrap();
        ConfigureExistingForestResources();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("ForestGameplayInstaller: Installed player, tools, UI, crafting, and harvestable tree setup in the open scene.");
    }

    private static bool IsSupportedGameplayScene(Scene scene)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            return false;
        }

        return scene.path.StartsWith("Assets/Scenes/", System.StringComparison.OrdinalIgnoreCase)
            || scene.path.Equals("Assets/Lowpoly Style/ForestPack/DemoScene/Forest.unity", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasGame1StylePlayerSetup()
    {
        GameObject player = FindScenePlayer();
        if (player == null)
        {
            return false;
        }

        Transform visual = player.transform.Find("CharacterVisual");
        if (visual == null || visual.GetComponentInChildren<Animator>(true) == null || visual.GetComponentInChildren<Renderer>(true) == null)
        {
            return false;
        }

        Camera camera = Camera.main;
        ThirdPersonCameraFollow follow = camera != null ? camera.GetComponent<ThirdPersonCameraFollow>() : null;
        return follow != null && follow.target == player.transform;
    }

    private static bool NeedsTestSetupRefresh()
    {
        GameObject player = FindScenePlayer();
        PlayerToolController controller = player != null ? player.GetComponent<PlayerToolController>() : null;
        if (controller == null)
        {
            return true;
        }

        if (!controller.hasBackpack || controller.backpackColumns != 12 || controller.backpackRows != 12 || controller.hotbarSize != 9)
        {
            return true;
        }

        if (controller.slots == null || controller.slots.Length < 40)
        {
            return true;
        }

        if (controller.hotbarSlotIndices == null || controller.hotbarSlotIndices.Length != 9)
        {
            return true;
        }

        if (AssetDatabase.LoadAssetAtPath<Material>(GeneratedMaterialsFolder + "/RubySword.mat") == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(HeldCraftsFolder + "/Held_RubySword.prefab") == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(HeldCraftsFolder + "/Held_WoodHelmet.prefab") == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(HeldCraftsFolder + "/Held_StoneArmor.prefab") == null)
        {
            return true;
        }

        if (GameObject.Find(OldSpawnPlatformName) != null)
        {
            return true;
        }

        for (int i = 0; i < controller.hotbarSlotIndices.Length; i++)
        {
            if (controller.hotbarSlotIndices[i] != i)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject EnsurePlayer()
    {
        GameObject player = FindScenePlayer();
        bool createdPlayer = player == null;
        if (player == null)
        {
            player = new GameObject("Player");
        }

        player.tag = "Player";

        Rigidbody rb = GetOrAdd<Rigidbody>(player);
        CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(player);
        PlayerMovement movement = GetOrAdd<PlayerMovement>(player);
        CharacterRunAnimator runAnimator = GetOrAdd<CharacterRunAnimator>(player);
        PlayerToolController toolController = GetOrAdd<PlayerToolController>(player);

        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.height = 2f;
        capsule.radius = 0.4f;

        rb.mass = 1.2f;
        rb.drag = 0f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.angularDrag = 8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        movement.enabled = true;
        ConfigureMovement(movement);

        Transform visual = EnsureCharacterVisual(player.transform, runAnimator);
        toolController.visualRoot = visual;
        toolController.useRange = 6f;
        toolController.useCooldown = 0.32f;
        toolController.toolDamage = 1;
        toolController.treeChopHoldSeconds = 5f;
        toolController.treeChopAimRadius = 0.55f;
        toolController.treeChopSnapRadius = 1.35f;
        toolController.treeChopCameraAcquireRange = 18f;
        toolController.showHarvestDebug = true;
        toolController.hasBackpack = true;
        toolController.backpackColumns = 12;
        toolController.backpackRows = 12;
        toolController.craftingCatalog = Object.FindObjectOfType<CraftingCatalog>();
        toolController.hotbarSize = 9;
        ToolSlot[] existingSlots = toolController.slots;
        toolController.slots = CreateForestToolSlots(toolController.craftingCatalog);
        PreserveExistingSlotTuning(toolController.slots, existingSlots);
        ApplyKnownInstallSlotTuning(toolController.slots);
        toolController.ResetHotbarToFirstSlots();
        toolController.SyncGripTuningFields();

        ApplySpawnPlacement(player, capsule, createdPlayer);

        return player;
    }

    private static void ConfigureMovement(PlayerMovement movement)
    {
        if (movement == null)
        {
            return;
        }

        movement.moveSpeed = 6f;
        movement.acceleration = 18f;
        movement.airControlPercent = 0.55f;
        movement.rotationSpeed = 10f;
        movement.braking = 24f;
        movement.analogDeadZone = 0.2f;
        movement.stopSpeed = 0.05f;
        movement.resolveSpawnOverlaps = true;
        movement.collisionSkin = 0.03f;
        movement.maxDepenetrationStep = 0.5f;
        movement.depenetrationPasses = 3;
        movement.collisionMask = ~0;
        movement.jumpForce = 7.5f;
        movement.groundProbeDistance = 0.35f;
        movement.maxGroundAngle = 55f;
        movement.groundedDownForce = 2f;
        movement.coyoteTime = 0.12f;
        movement.jumpBufferTime = 0.15f;
        movement.enableClimb = false;
        movement.climbCheckDistance = 0.95f;
        movement.climbMinHeight = 1.15f;
        movement.climbMaxHeight = 2.65f;
        movement.climbForwardOffset = 0.65f;
        movement.climbDuration = 0.85f;
    }

    private static void EnsureWoodenHouse3InScene()
    {
        Transform village = FindFishingVillageRoot();
        if (village == null)
        {
            return;
        }

        if (FindDescendantByNameContains(village, "WoodHouse3") != null
            || FindDescendantByNameContains(village, "WoodenHouse3") != null
            || FindDescendantByNameContains(village, "Wooden House3") != null)
        {
            return;
        }

        Transform existingHouse3 = FindTransformByNameContains("WoodHouse3");
        if (existingHouse3 == null)
        {
            existingHouse3 = FindTransformByNameContains("WoodenHouse3");
        }

        Transform neighbor = FindDescendantByNameContains(village, "WoodenHouse2");
        if (neighbor == null)
        {
            neighbor = FindDescendantByNameContains(village, "WoodenHouse1");
        }

        if (existingHouse3 != null && !existingHouse3.IsChildOf(village))
        {
            existingHouse3.SetParent(village, true);
            if (neighbor != null && Vector3.Distance(existingHouse3.position, neighbor.position) > 80f)
            {
                existingHouse3.position = neighbor.position + new Vector3(16f, 0f, -14f);
                existingHouse3.rotation = neighbor.rotation;
                SnapObjectToGround(existingHouse3.gameObject);
            }

            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Lowpoly Style/Alpine Woodland/Prefabs/WoodenHouse3.prefab");
        if (prefab == null)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Lowpoly Style/Desert/Prefabs/WoodenHouse3.prefab");
        }

        if (prefab == null)
        {
            Debug.LogWarning("ForestGameplayInstaller: Could not find WoodenHouse3 prefab, so player spawn will use the nearest existing wooden house.");
            return;
        }

        GameObject house = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (house == null)
        {
            return;
        }

        house.name = "WoodenHouse3";
        house.transform.SetParent(village, true);
        house.transform.rotation = neighbor != null ? neighbor.rotation : Quaternion.identity;
        house.transform.position = neighbor != null
            ? neighbor.position + new Vector3(16f, 0f, -14f)
            : new Vector3(-55f, 1f, -138f);
        SnapObjectToGround(house);
    }

    private static void SnapObjectToGround(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Physics.SyncTransforms();
        if (!TryFindGroundHit(root.transform, root.transform.position, out RaycastHit hit))
        {
            return;
        }

        if (TryGetRenderBounds(root, out Bounds bounds))
        {
            root.transform.position += Vector3.up * (hit.point.y - bounds.min.y);
        }
        else
        {
            Vector3 position = root.transform.position;
            position.y = hit.point.y;
            root.transform.position = position;
        }

        Physics.SyncTransforms();
    }

    private static void DestroyOldSpawnPlatform()
    {
        GameObject platform = GameObject.Find(OldSpawnPlatformName);
        if (platform != null)
        {
            Object.DestroyImmediate(platform);
        }
    }

    private static Transform EnsureCharacterVisual(Transform player, CharacterRunAnimator runAnimator)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisualPrefabPath);
        Transform existing = player.Find("CharacterVisual");
        if (existing != null
            && existing.GetComponentInChildren<Renderer>(true) != null
            && existing.GetComponentInChildren<Animator>(true) != null
            && IsPolytopeMaleVisual(existing))
        {
            Animator existingAnimator = existing.GetComponentInChildren<Animator>(true);
            existingAnimator.runtimeAnimatorController = CreateAnimatorController();
            existingAnimator.applyRootMotion = false;
            PreparePolytopeMaleVisual(existing.gameObject);
            runAnimator.SetVisualRoot(existing);
            runAnimator.SetAnimator(existingAnimator);

            return existing;
        }

        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            existing = null;
        }

        if (model != null)
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual != null)
            {
                visual.name = "CharacterVisual";
                visual.transform.SetParent(player, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                RemoveColliders(visual);

                Animator animator = GetOrAdd<Animator>(visual);
                animator.runtimeAnimatorController = CreateAnimatorController();
                animator.applyRootMotion = false;
                PreparePolytopeMaleVisual(visual);
                runAnimator.SetVisualRoot(visual.transform);
                runAnimator.SetAnimator(animator);
                return visual.transform;
            }
        }

        Debug.LogError("ForestGameplayInstaller: Player visual prefab is missing at " + PlayerVisualPrefabPath + ". No fallback character was created.");
        return existing;
    }

    private static bool IsPolytopeMaleVisual(Transform visual)
    {
        if (visual == null)
        {
            return false;
        }

        SkinnedMeshRenderer[] renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].name.StartsWith("PT_Male_Armor_", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void PreparePolytopeMaleVisual(GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        PolytopeModularCharacterVisual modularVisual = visual.GetComponent<PolytopeModularCharacterVisual>();
        if (modularVisual == null)
        {
            modularVisual = visual.AddComponent<PolytopeModularCharacterVisual>();
        }

        modularVisual.ApplyDefaultPartVisibility();
    }

    private static RuntimeAnimatorController CreateAnimatorController()
    {
        string path = GeneratedFolder + "/ForestPlayer.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        for (int i = stateMachine.states.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveState(stateMachine.states[i].state);
        }

        ConfigureAnimationLoop("Assets/Characters/Kate/Animations/Ch21_nonPBR@Orc Idle.fbx", true);
        ConfigureAnimationLoop("Assets/Characters/Kate/Animations/Ch21_nonPBR@Walking.fbx", true);
        ConfigureAnimationLoop("Assets/Characters/Kate/Animations/Ch21_nonPBR@Jump.fbx", false);
        AnimatorState idle = AddState(stateMachine, "Idle", "Assets/Characters/Kate/Animations/Ch21_nonPBR@Orc Idle.fbx", new Vector3(260f, 80f, 0f), 1f);
        AddState(stateMachine, "Walk", "Assets/Characters/Kate/Animations/Ch21_nonPBR@Walking.fbx", new Vector3(260f, 150f, 0f), 1.05f);
        AddState(stateMachine, "Jump", "Assets/Characters/Kate/Animations/Ch21_nonPBR@Jump.fbx", new Vector3(260f, 220f, 0f), 1f);
        stateMachine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, string clipPath, Vector3 position, float speed)
    {
        AnimatorState state = stateMachine.AddState(stateName, position);
        state.motion = LoadClip(clipPath);
        state.speed = speed;
        return state;
    }

    private static AnimationClip LoadClip(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        return null;
    }

    private static void ConfigureAnimationLoop(string path, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].loopTime != loop)
            {
                clips[i].loopTime = loop;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static ToolSlot[] CreateForestToolSlots(CraftingCatalog catalog)
    {
        List<ToolSlot> slots = new List<ToolSlot>();
        HashSet<string> included = new HashSet<string>();
        AddTool(slots, "Wood Axe", "PP_Axe_New_01_Copper", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 8f);
        AddTool(slots, "Wood Pickaxe", "PP_Pickaxe_New_03_Copper", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        AddTool(slots, "Wood Sword", "PP_Sword_01", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 20, 0f);
        AddTool(slots, "Stone Axe", "PP_Axe_New_01_Silver", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 5f);
        AddTool(slots, "Stone Pickaxe", "PP_Pickaxe_New_03_Silver", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        AddTool(slots, "Stone Sword", "PP_Sword_03", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 25, 0f);
        AddTool(slots, "Iron Axe", "PP_Axe_New_04_Iron", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 3f);
        AddTool(slots, "Iron Pickaxe", "PP_Pickaxe_New_03_Iron", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        if (!AddCatalogSlot(slots, catalog, "iron_sword")) AddTool(slots, "Iron Sword", "PP_Sword_04", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 18, 0f);
        AddTool(slots, "Ruby Axe", "PP_Axe_New_02_Red", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 2.5f);
        AddTool(slots, "Sapphire Axe", "PP_Axe_New_02_Blue", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 2.5f);
        AddTool(slots, "Emerald Axe", "PP_Axe_New_03_Green", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 2.5f);
        AddTool(slots, "Copper Axe", "PP_Axe_New_01_Copper", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 2.5f);
        AddTool(slots, "Gold Axe", "PP_Axe_New_03_Gold", ToolActionType.ChopTree, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 0, 2.5f);
        AddTool(slots, "Ruby Pickaxe", "PP_Pickaxe_New_03_Red", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        AddTool(slots, "Sapphire Pickaxe", "PP_Pickaxe_New_03_Blue", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        AddTool(slots, "Emerald Pickaxe", "PP_Pickaxe_New_04_Green", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        AddTool(slots, "Copper Pickaxe", "PP_Pickaxe_New_03_Copper", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        AddTool(slots, "Gold Pickaxe", "PP_Pickaxe_New_03_Gold", ToolActionType.MineRock, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), 0.62f, 20, 0, 5f);
        if (!AddCatalogSlot(slots, catalog, "ruby_sword")) AddTool(slots, "Ruby Sword", "PP_Sword_04", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 24, 0f);
        if (!AddCatalogSlot(slots, catalog, "sapphire_sword")) AddTool(slots, "Sapphire Sword", "PP_Sword_04", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 21, 0f);
        if (!AddCatalogSlot(slots, catalog, "emerald_sword")) AddTool(slots, "Emerald Sword", "PP_Sword_04", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 22, 0f);
        if (!AddCatalogSlot(slots, catalog, "copper_sword")) AddTool(slots, "Copper Sword", "PP_Sword_04", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 18, 0f);
        if (!AddCatalogSlot(slots, catalog, "gold_sword")) AddTool(slots, "Gold Sword", "PP_Sword_04", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.58f, 15, 18, 0f);
        AddTool(slots, "Scissors", "3_scissors1", ToolActionType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), 0.5f, 0, 0, 0f);

        for (int i = 0; i < slots.Count; i++)
        {
            included.Add(InventoryUtility.GetItemId(slots[i]));
        }

        if (catalog != null && catalog.itemDefinitions != null)
        {
            for (int i = 0; i < catalog.itemDefinitions.Length; i++)
            {
                CraftingItemDefinition definition = catalog.itemDefinitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.itemId) || included.Contains(definition.itemId))
                {
                    continue;
                }

                if (definition.itemId == "forest_heart")
                {
                    continue;
                }

                int amount = Mathf.Clamp(definition.maxStack > 1 ? 16 : 1, 1, Mathf.Max(1, definition.maxStack));
                ToolSlot slot = InventoryUtility.CreateSlotFromDefinition(definition, amount);
                if (slot != null)
                {
                    slots.Add(slot);
                    included.Add(definition.itemId);
                }
            }
        }

        AddTestMaterial(slots, included, "wood", "Wood", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Log_Pile_01.prefab"));
        AddTestMaterial(slots, included, "stone", "Stone", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BrokenVector/LowPolyRockPack/Prefabs/Rock Type1 01.prefab"));
        AddTestMaterial(slots, included, "iron_ore", "Iron Ore", LoadCrystal05("Iron"));
        AddTestMaterial(slots, included, "ruby_ore", "Ruby Ore", LoadCrystal05("Red"));
        AddTestMaterial(slots, included, "sapphire_ore", "Sapphire Ore", LoadCrystal05("Blue"));
        AddTestMaterial(slots, included, "emerald_ore", "Emerald Ore", LoadCrystal05("Green"));
        AddTestMaterial(slots, included, "copper_ore", "Copper Ore", LoadCrystal05("Copper"));
        AddTestMaterial(slots, included, "gold_ore", "Gold Ore", LoadCrystal05("Gold"));
        AddTestMaterial(slots, included, "coal_ore", "Coal Ore", LoadCrystal05("Iron"));
        AddTestMaterial(slots, included, "forest_guide", "Forest Guide", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Ores and Crystals/PP_Gemstone_06_Green.prefab"));
        EnsureInventoryItem(slots, included, catalog, "forest_guide", 30);
        EnsureInventoryItem(slots, included, catalog, "forest_heart_detector", 1);
        AddTestMaterial(slots, included, "herb", "Herb", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/resources/herbs-spiral/source/spirale1.fbx"));
        AddTestMaterial(slots, included, "wool", "Wool", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/HeldCrafts/Held_Wool.prefab"));
        AddTestMaterial(slots, included, "stone_hammer", "Stone Hammer", FindToolPrefab("PP_Hammer"));
        ApplyKnownInstallSlotTuning(slots.ToArray());
        return slots.ToArray();
    }

    private static void PreserveExistingSlotTuning(ToolSlot[] newSlots, ToolSlot[] oldSlots)
    {
        if (newSlots == null || oldSlots == null)
        {
            return;
        }

        Dictionary<string, ToolSlot> oldById = new Dictionary<string, ToolSlot>();
        for (int i = 0; i < oldSlots.Length; i++)
        {
            ToolSlot oldSlot = oldSlots[i];
            string itemId = InventoryUtility.GetItemId(oldSlot);
            if (!string.IsNullOrEmpty(itemId) && !oldById.ContainsKey(itemId))
            {
                oldById.Add(itemId, oldSlot);
            }
        }

        for (int i = 0; i < newSlots.Length; i++)
        {
            ToolSlot newSlot = newSlots[i];
            string itemId = InventoryUtility.GetItemId(newSlot);
            if (string.IsNullOrEmpty(itemId) || !oldById.TryGetValue(itemId, out ToolSlot oldSlot) || oldSlot == null)
            {
                continue;
            }

            if (!IsForcedInstallTuningItem(itemId))
            {
                newSlot.heldLocalPosition = oldSlot.heldLocalPosition;
                newSlot.heldLocalEuler = oldSlot.heldLocalEuler;
                newSlot.heldLocalScale = oldSlot.heldLocalScale;
                newSlot.rightHandGripLocal = oldSlot.rightHandGripLocal;
                newSlot.leftHandGripLocal = oldSlot.leftHandGripLocal;
                newSlot.nonGunDirectionFlipped = oldSlot.nonGunDirectionFlipped;
            }

            newSlot.stackCount = Mathf.Max(newSlot.stackCount, oldSlot.stackCount);
            newSlot.durability = oldSlot.durability > 0 ? oldSlot.durability : newSlot.durability;
            newSlot.maxDurability = oldSlot.maxDurability > 0 ? oldSlot.maxDurability : newSlot.maxDurability;
        }
    }

    private static bool IsForcedInstallTuningItem(string itemId)
    {
        return itemId == "wood_helmet"
            || itemId == "wood_armor"
            || itemId == "stone_helmet"
            || itemId == "stone_armor"
            || itemId == "iron_helmet"
            || itemId == "iron_armor"
            || itemId == "forest_guide"
            || itemId == "forest_heart"
            || itemId == "forest_heart_detector";
    }

    private static void ApplyKnownInstallSlotTuning(ToolSlot[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            ToolSlot slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            string itemId = InventoryUtility.GetItemId(slot);
            if (ApplyDefaultHeldTuning(itemId, ref slot.heldLocalPosition, ref slot.heldLocalEuler, ref slot.heldLocalScale))
            {
                slot.nonGunDirectionFlipped = true;
            }

            ApplyInstallForcedSlotTuning(slot);
        }
    }

    private static void EnsureInventoryItem(List<ToolSlot> slots, HashSet<string> included, CraftingCatalog catalog, string itemId, int amount)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (InventoryUtility.GetItemId(slots[i]) == itemId)
            {
                slots[i].stackCount = Mathf.Max(slots[i].stackCount, amount);
                slots[i].maxStack = Mathf.Max(slots[i].maxStack, amount);
                ApplyInstallForcedSlotTuning(slots[i]);
                included.Add(itemId);
                return;
            }
        }

        CraftingItemDefinition definition = catalog != null ? catalog.FindItem(itemId) : null;
        ToolSlot slot = definition != null
            ? InventoryUtility.CreateSlotFromDefinition(definition, amount)
            : null;
        if (slot == null)
        {
            return;
        }

        ApplyInstallForcedSlotTuning(slot);
        slots.Add(slot);
        included.Add(itemId);
    }

    private static void ApplyInstallForcedSlotTuning(ToolSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        if (itemId.Contains("helmet"))
        {
            slot.heldLocalPosition = new Vector3(-0.1f, -0.6f, 0.08f);
        }
        else if (itemId.Contains("armor"))
        {
            slot.heldLocalPosition = new Vector3(slot.heldLocalPosition.x, -0.2f, slot.heldLocalPosition.z);
        }
        ApplySpecificHeldTuning(slot);
    }

    private static void ApplySpecificHeldTuning(ToolSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        string itemId = InventoryUtility.GetItemId(slot);
        switch (itemId)
        {
            case "workbench":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.1f, 0f);
                break;
            case "chair":
                slot.heldLocalPosition = new Vector3(-0.15f, -0.1f, 0.09f);
                break;
            case "table":
                slot.heldLocalPosition = new Vector3(-0.14f, -0.1f, -0.09f);
                break;
            case "wood":
                slot.heldLocalPosition = new Vector3(-0.06f, -0.1f, 0.03f);
                break;
            case "stone":
                slot.heldLocalPosition = new Vector3(0f, -0.07f, 0f);
                slot.heldLocalScale = Vector3.one * 0.04f;
                break;
            case "iron_ore":
            case "ruby_ore":
            case "sapphire_ore":
            case "emerald_ore":
            case "copper_ore":
            case "gold_ore":
            case "coal_ore":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.06f, -0.01f);
                break;
            case "herb":
                slot.heldLocalPosition = new Vector3(-0.2f, -0.1f, 0.1f);
                break;
            case "stone_hammer":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.06f, -0.1f);
                slot.heldLocalEuler = new Vector3(100f, 180f, 0f);
                break;
            case "forest_guide":
            case "forest_heart_detector":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.06f, -0.01f);
                break;
            case "scissors":
                slot.heldLocalPosition = new Vector3(-0.2f, -0.02f, 0f);
                slot.heldLocalEuler = new Vector3(0f, -94f, -92f);
                break;
            case "chest":
                slot.heldLocalPosition = new Vector3(0.02f, -0.09f, 0.08f);
                slot.worldScale = Vector3.one;
                break;
            case "bed":
                slot.heldLocalPosition = new Vector3(-0.01f, -0.04f, 0.07f);
                break;
            case "door":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.09f, -0.09f);
                slot.worldScale = Vector3.one;
                break;
            case "furnace":
                slot.heldLocalPosition = new Vector3(0f, -0.1f, 0.05f);
                slot.worldScale = Vector3.one;
                break;
            case "auto_forge":
                slot.heldLocalPosition = new Vector3(-0.16f, 0.5f, 0f);
                slot.worldScale = Vector3.one;
                break;
            case "torch":
                slot.heldLocalPosition = new Vector3(-0.1f, 0f, 0f);
                slot.heldLocalEuler = new Vector3(100f, slot.heldLocalEuler.y, slot.heldLocalEuler.z);
                break;
            case "bandage":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.09f, 0f);
                slot.heldLocalScale = Vector3.one * 0.07f;
                break;
            case "first_aid_kit":
                slot.heldLocalPosition = new Vector3(-0.1f, -0.09f, 0f);
                slot.heldLocalScale = Vector3.one * 0.1f;
                break;
        }
    }

    private static void AddStackItem(List<ToolSlot> slots, string displayName, ToolCategory category, int amount, int damage = 0)
    {
        slots.Add(new ToolSlot
        {
            itemId = InventoryUtility.NormalizeItemId(displayName),
            displayName = displayName,
            actionType = ToolActionType.None,
            category = category,
            holdPose = ToolHoldPose.OneHandTool,
            stackCount = amount,
            maxStack = 64,
            damage = damage,
            nonGunDirectionFlipped = true
        });
    }

    private static bool AddCatalogSlot(List<ToolSlot> slots, CraftingCatalog catalog, string itemId)
    {
        CraftingItemDefinition definition = catalog != null ? catalog.FindItem(itemId) : null;
        if (definition == null)
        {
            return false;
        }

        ToolSlot slot = InventoryUtility.CreateSlotFromDefinition(definition, 1);
        if (slot == null)
        {
            return false;
        }

        ApplyDefaultHeldTuning(itemId, ref slot.heldLocalPosition, ref slot.heldLocalEuler, ref slot.heldLocalScale);
        slots.Add(slot);
        return true;
    }

    private static void AddTool(List<ToolSlot> slots, string displayName, string containsName, ToolActionType actionType, Vector3 heldPosition, Vector3 heldEuler, float scale, int durability = 0, int damage = 0, float harvestSeconds = 0f)
    {
        GameObject prefab = FindToolPrefab(containsName);
        if (prefab == null)
        {
            Debug.LogWarning("ForestGameplayInstaller: Could not find tool prefab containing '" + containsName + "'.");
            return;
        }

        string itemId = InventoryUtility.NormalizeItemId(displayName);
        Vector3 heldScale = Vector3.one * scale;
        ApplyDefaultHeldTuning(itemId, ref heldPosition, ref heldEuler, ref heldScale);
        slots.Add(new ToolSlot
        {
            itemId = itemId,
            displayName = displayName,
            prefab = prefab,
            worldPrefab = prefab,
            actionType = actionType,
            category = damage > 0 && actionType == ToolActionType.None ? ToolCategory.Melee : ToolCategory.Tools,
            holdPose = ToolHoldPose.OneHandTool,
            heldLocalPosition = heldPosition,
            heldLocalEuler = heldEuler,
            heldLocalScale = heldScale,
            maxStack = 1,
            stackCount = 1,
            durability = durability,
            maxDurability = durability,
            damage = damage,
            harvestSeconds = harvestSeconds,
            nonGunDirectionFlipped = true
        });
    }

    private static void AddTestMaterial(List<ToolSlot> slots, HashSet<string> included, string itemId, string displayName, GameObject prefab, ToolCategory category = ToolCategory.Materials)
    {
        if (included.Contains(itemId))
        {
            return;
        }

        slots.Add(new ToolSlot
        {
            itemId = itemId,
            displayName = displayName,
            prefab = prefab,
            worldPrefab = prefab,
            actionType = ToolActionType.None,
            category = category,
            holdPose = ToolHoldPose.OneHandTool,
            heldLocalPosition = new Vector3(0.03f, -0.02f, 0.05f),
            heldLocalEuler = new Vector3(0f, 160f, -18f),
            heldLocalScale = Vector3.one * 0.35f,
            stackCount = 64,
            maxStack = 64,
            nonGunDirectionFlipped = true
        });
        included.Add(itemId);
    }

    private static GameObject LoadCrystal05(string suffix)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Ores and Crystals/PP_Crystal_05_" + suffix + ".prefab");
    }

    private static GameObject FindToolPrefab(string containsName)
    {
        if (!string.IsNullOrEmpty(containsName) && containsName.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(containsName);
        }

        string[] guids = AssetDatabase.FindAssets(containsName);
        GameObject fallback = null;
        string lowerNeedle = containsName.ToLowerInvariant();
        string expectedFileName = lowerNeedle + ".prefab";
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lowerPath = path.ToLowerInvariant();
            if (!lowerPath.Contains(lowerNeedle))
            {
                continue;
            }

            if (lowerPath.Contains("gun") || lowerPath.Contains("rifle") || lowerPath.Contains("pistol") || lowerPath.Contains("shotgun") || lowerPath.Contains("sniper"))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (System.IO.Path.GetFileName(lowerPath) == expectedFileName)
            {
                return prefab;
            }

            if (fallback == null)
            {
                fallback = prefab;
            }
        }

        return fallback;
    }

    private static GameObject FindFirstModelInFolder(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
            {
                return model;
            }
        }

        return null;
    }

    private static void EnsureCamera(Transform player)
    {
        Camera camera = Camera.main;
        GameObject cameraObject = camera != null ? camera.gameObject : new GameObject("Main Camera");
        camera = GetOrAdd<Camera>(cameraObject);
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 600f;
        camera.fieldOfView = 58f;
        GetOrAdd<AudioListener>(cameraObject);

        ThirdPersonCameraFollow follow = GetOrAdd<ThirdPersonCameraFollow>(cameraObject);
        follow.target = player;
        follow.targetOffset = new Vector3(0f, 1.35f, 0f);
        follow.lookOffset = new Vector3(0f, 1.35f, 0f);
        follow.distance = 4.5f;
        follow.minDistance = 2.6f;
        follow.maxDistance = 7f;
        follow.minPitch = 10f;
        follow.maxPitch = 50f;
        follow.positionSmoothTime = 0.06f;
        camera.fieldOfView = 62f;
        camera.depth = 10f;
        cameraObject.transform.position = player.position + new Vector3(0f, 2.3f, -4.3f);
        cameraObject.transform.LookAt(player.position + new Vector3(0f, 1.25f, 0f));
    }

    private static GameObject FindScenePlayer()
    {
        try
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged != null)
            {
                return tagged;
            }
        }
        catch
        {
        }

        return GameObject.Find("Player");
    }

    private static void EnsureSingleAudioListener()
    {
        Camera mainCamera = Camera.main;
        AudioListener keep = mainCamera != null ? mainCamera.GetComponent<AudioListener>() : null;
        if (keep == null && mainCamera != null)
        {
            keep = mainCamera.gameObject.AddComponent<AudioListener>();
        }

        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        if (keep == null && listeners.Length > 0)
        {
            keep = listeners[0];
        }

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener != keep)
            {
                Object.DestroyImmediate(listener);
            }
        }
    }

    private static void EnsureCraftingCatalog()
    {
        CraftingCatalog catalog = Object.FindObjectOfType<CraftingCatalog>();
        if (catalog == null)
        {
            catalog = new GameObject("CraftingCatalog").AddComponent<CraftingCatalog>();
        }

        GameObject chair = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/old_wooden_chair.fbx");
        GameObject bed = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/uploads_files_2276421_Bed_Single_Combined.fbx");
        GameObject table = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/uploads_files_3301556_Mesa+madera+exterior.fbx");
        GameObject stick = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Blacksmith House/PP_Roof_Top_04.prefab");
        GameObject box = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/uploads_files_5189758_Wood_Box.fbx");
        GameObject door = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/uploads_files_5914106_Door.fbx");
        GameObject workbench = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/uploads_files_6096385_workbench.fbx");
        GameObject largePlank = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wooden tools/Wood_Floor_001_FBX.fbx");
        GameObject logFence = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Log_Fence_03.prefab");
        GameObject furnace = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Furnace_03.prefab");
        GameObject forge = FindToolPrefab("forge");
        GameObject torch = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Tools/PP_Torch_01.prefab");
        GameObject standingTorch = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Tools/PP_Torch_Standing_01.prefab");
        GameObject woodAxe = FindToolPrefab("PP_Axe_New_01_Copper");
        GameObject stoneAxe = FindToolPrefab("PP_Axe_New_01_Silver");
        GameObject ironAxe = FindToolPrefab("PP_Axe_New_04_Iron");
        GameObject redAxe = FindToolPrefab("PP_Axe_New_02_Red");
        GameObject blueAxe = FindToolPrefab("PP_Axe_New_02_Blue");
        GameObject greenAxe = FindToolPrefab("PP_Axe_New_03_Green");
        GameObject copperAxe = FindToolPrefab("PP_Axe_New_01_Copper");
        GameObject goldAxe = FindToolPrefab("PP_Axe_New_03_Gold");
        GameObject woodPickaxe = FindToolPrefab("PP_Pickaxe_New_03_Copper");
        GameObject stonePickaxe = FindToolPrefab("PP_Pickaxe_New_03_Silver");
        GameObject ironPickaxe = FindToolPrefab("PP_Pickaxe_New_03_Iron");
        GameObject redPickaxe = FindToolPrefab("PP_Pickaxe_New_03_Red");
        GameObject bluePickaxe = FindToolPrefab("PP_Pickaxe_New_03_Blue");
        GameObject greenPickaxe = FindToolPrefab("PP_Pickaxe_New_04_Green");
        GameObject copperPickaxe = FindToolPrefab("PP_Pickaxe_New_03_Copper");
        GameObject goldPickaxe = FindToolPrefab("PP_Pickaxe_New_03_Gold");
        GameObject sword01 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Swords and Shields/PP_Sword_01.prefab");
        GameObject sword03 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Swords and Shields/PP_Sword_03.prefab");
        GameObject sword04 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Swords and Shields/PP_Sword_04.prefab");
        GameObject ironSword = CreateColoredToolPrefab("Held_IronSword.prefab", sword04, "IronSword.mat", new Color(0.16f, 0.17f, 0.18f));
        GameObject rubySword = CreateColoredToolPrefab("Held_RubySword.prefab", sword04, "RubySword.mat", new Color(0.86f, 0.06f, 0.08f));
        GameObject sapphireSword = CreateColoredToolPrefab("Held_SapphireSword.prefab", sword04, "SapphireSword.mat", new Color(0.05f, 0.22f, 0.95f));
        GameObject emeraldSword = CreateColoredToolPrefab("Held_EmeraldSword.prefab", sword04, "EmeraldSword.mat", new Color(0.05f, 0.72f, 0.22f));
        GameObject copperSword = CreateColoredToolPrefab("Held_CopperSword.prefab", sword04, "CopperSword.mat", new Color(0.72f, 0.36f, 0.16f));
        GameObject goldSword = CreateColoredToolPrefab("Held_GoldSword.prefab", sword04, "GoldSword.mat", new Color(1f, 0.72f, 0.14f));
        GameObject shield = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Swords and Shields/PP_Shield_02.prefab");
        GameObject scissors = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/resources/day-3-scissors/source/3_scissors1.obj");
        GameObject armorHelmet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_Armors/Separate_Parts/PT_Male_Armor_01_A_helmet.prefab");
        GameObject armorBody = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_Armors/Separate_Parts/PT_Male_Armor_01_A_body.prefab");
        GameObject fallbackHelmet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/resources/combat-helmet-k6-3/source/extracted/bm_lp_1.obj");
        GameObject fallbackArmor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/resources/old-king-armor/source/Armor.fbx");
        GameObject ironHelmet = armorHelmet != null ? armorHelmet : fallbackHelmet;
        GameObject ironArmor = armorBody != null ? armorBody : fallbackArmor;
        GameObject woodHelmet = CreateColoredToolPrefab("Held_WoodHelmet.prefab", ironHelmet, "WoodArmorBrown.mat", new Color(0.38f, 0.22f, 0.11f));
        GameObject woodArmor = CreateColoredToolPrefab("Held_WoodArmor.prefab", ironArmor, "WoodArmorBrown.mat", new Color(0.38f, 0.22f, 0.11f));
        GameObject stoneHelmet = CreateColoredToolPrefab("Held_StoneHelmet.prefab", ironHelmet, "StoneArmorSilver.mat", new Color(0.66f, 0.69f, 0.72f));
        GameObject stoneArmor = CreateColoredToolPrefab("Held_StoneArmor.prefab", ironArmor, "StoneArmorSilver.mat", new Color(0.66f, 0.69f, 0.72f));
        GameObject bandage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GeeKay3D/First-Aid-Set/Assets/Prefabs/FirstAidKit_White.prefab");
        GameObject firstAidKit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GeeKay3D/First-Aid-Set/Assets/Prefabs/FirstAidKit_Red.prefab");
        GameObject coal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Ores and Crystals/PP_Rock_Gem_01_Mined.prefab");
        GameObject charcoal = CreateColoredToolPrefab("Held_Charcoal.prefab", coal, "CharcoalBrown.mat", new Color(0.32f, 0.18f, 0.09f));
        GameObject ironBar = CreateColoredToolPrefab("Held_IronBar.prefab", stick, "IronBarBlack.mat", new Color(0.035f, 0.04f, 0.045f));
        GameObject wool = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/HeldCrafts/Held_Wool.prefab");
        if (wool == null)
        {
            wool = CreateWoolPrefab();
        }
        GameObject forestHeart = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Ores and Crystals/PP_Gemstone_09_Green.prefab");
        GameObject forestGuide = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Ores and Crystals/PP_Gemstone_06_Green.prefab");
        GameObject forestHeartDetector = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Ores and Crystals/PP_Stone_Crystal_01_Green.prefab");
        catalog.itemDefinitions = new[]
        {
            CreateCraftItem("stick", "Stick", stick, stick, false, PlaceableType.None, new Vector3(0.02f, -0.02f, 0.05f), new Vector3(8f, 154f, -12f), Vector3.one * 0.45f, 64),
            CreateCraftItem("workbench", "Workbench", workbench, workbench, true, PlaceableType.Workbench, new Vector3(-0.1f, -0.1f, 0f), new Vector3(0f, 150f, -8f), Vector3.one * 0.22f, 64, new Vector3(270f, -180f, 0f), Vector3.one * 50f),
            CreateCraftItem("chair", "Chair", chair, chair, true, PlaceableType.Chair, new Vector3(-0.15f, -0.1f, 0.09f), new Vector3(10f, 146f, -10f), Vector3.one * 0.18f, 64, new Vector3(-90f, 0f, 0f)),
            CreateCraftItem("bed", "Bed", bed, bed, true, PlaceableType.Bed, new Vector3(-0.01f, -0.04f, 0.07f), new Vector3(0f, 150f, -10f), Vector3.one * 0.16f, 64),
            CreateCraftItem("chest", "Chest", CreateHeldCraftProxyPrefab("Held_Chest.prefab", box), box, true, PlaceableType.Chest, new Vector3(0.02f, -0.09f, 0.08f), new Vector3(-8f, 146f, -6f), Vector3.one * 0.34f, 64, Vector3.zero, Vector3.one),
            CreateCraftItem("door", "Door", CreateHeldCraftProxyPrefab("Held_Door.prefab", door), door, true, PlaceableType.Door, new Vector3(-0.1f, -0.09f, -0.09f), new Vector3(-6f, 142f, -4f), Vector3.one * 0.32f, 64, Vector3.zero, Vector3.one),
            CreateCraftItem("table", "Table", table, table, true, PlaceableType.LargePlank, new Vector3(-0.14f, -0.1f, -0.09f), new Vector3(0f, 148f, -10f), Vector3.one * 0.18f, 64),
            CreateCraftItem("torch", "Torch", torch, standingTorch != null ? standingTorch : torch, true, PlaceableType.Torch, new Vector3(-0.1f, 0f, 0f), new Vector3(100f, 160f, -18f), Vector3.one * 0.35f, 64, Vector3.zero, Vector3.one * 0.85f, ToolCategory.Materials),
            CreateCraftItem("bandage", "Bandage", bandage, bandage, false, PlaceableType.None, Vector3.zero, Vector3.zero, Vector3.one * 0.35f, 64, null, null, ToolCategory.Consumable, 0, 0, 0, 10),
            CreateCraftItem("first_aid_kit", "First Aid Kit", firstAidKit, firstAidKit, false, PlaceableType.None, Vector3.zero, Vector3.zero, Vector3.one * 0.35f, 64, null, null, ToolCategory.Consumable, 0, 0, 0, 60),
            CreateCraftItem("wood_axe", "Wood Axe", woodAxe, woodAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 12, 0, 0, 8f),
            CreateCraftItem("wood_pickaxe", "Wood Pickaxe", woodPickaxe, woodPickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 12, 0, 0, 5f),
            CreateCraftItem("wood_sword", "Wood Sword", sword01, sword01, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 12),
            CreateCraftItem("wood_fence", "Wood Fence", logFence != null ? logFence : largePlank, logFence != null ? logFence : largePlank, true, PlaceableType.Fence, new Vector3(0.025f, -0.03f, 0.09f), new Vector3(-10f, 142f, -4f), Vector3.one * 0.34f, 64, Vector3.zero, Vector3.one * 1.2f),
            CreateCraftItem("furnace", "Furnace", furnace, furnace, true, PlaceableType.Furnace, new Vector3(0f, -0.1f, 0.05f), new Vector3(0f, 150f, -8f), Vector3.one * 0.18f, 64, Vector3.zero, Vector3.one),
            CreateCraftItem("auto_forge", "Auto Forge", forge, forge, true, PlaceableType.Forge, new Vector3(-0.16f, 0.5f, 0f), new Vector3(0f, 150f, -8f), Vector3.one * 0.18f, 64, Vector3.zero, Vector3.one * 0.6f),
            CreateCraftItem("stone_axe", "Stone Axe", stoneAxe, stoneAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 13, 0, 0, 5f),
            CreateCraftItem("stone_pickaxe", "Stone Pickaxe", stonePickaxe, stonePickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 13, 0, 0, 5f),
            CreateCraftItem("stone_sword", "Stone Sword", sword03, sword03, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 15),
            CreateCraftItem("charcoal", "Charcoal", charcoal, charcoal, false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), Vector3.zero, Vector3.one * 0.12f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("coal", "Coal", coal, coal, false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), Vector3.zero, Vector3.one * 0.12f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("coal_ore", "Coal Ore", LoadCrystal05("Silver"), LoadCrystal05("Silver"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("iron_ore", "Iron Ore", LoadCrystal05("Iron"), LoadCrystal05("Iron"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("ruby_ore", "Ruby Ore", LoadCrystal05("Red"), LoadCrystal05("Red"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("sapphire_ore", "Sapphire Ore", LoadCrystal05("Blue"), LoadCrystal05("Blue"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("emerald_ore", "Emerald Ore", LoadCrystal05("Green"), LoadCrystal05("Green"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("copper_ore", "Copper Ore", LoadCrystal05("Copper"), LoadCrystal05("Copper"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("gold_ore", "Gold Ore", LoadCrystal05("Gold"), LoadCrystal05("Gold"), false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(0f, 160f, -18f), Vector3.one * 0.35f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("forest_heart", "Forest Heart", forestHeart, forestHeart, false, PlaceableType.None, new Vector3(0.03f, 0.15f, 0.05f), new Vector3(12f, 150f, -10f), Vector3.one * 0.18f, 1, null, null, ToolCategory.Materials),
            CreateCraftItem("forest_guide", "Forest Guide", forestGuide, forestGuide, false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(12f, 150f, -10f), Vector3.one * 0.2f, 64, null, null, ToolCategory.Materials),
            CreateCraftItem("forest_heart_detector", "Forest Heart Detector", forestHeartDetector, forestHeartDetector, false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), new Vector3(20f, 150f, -12f), Vector3.one * 0.26f, 1, null, null, ToolCategory.Tools),
            CreateCraftItem("iron_bar", "Iron Bar", ironBar != null ? ironBar : stick, ironBar != null ? ironBar : stick, false, PlaceableType.None, new Vector3(0.02f, -0.02f, 0.05f), new Vector3(8f, 154f, -12f), Vector3.one * 0.45f, 64),
            CreateCraftItem("iron_axe", "Iron Axe", ironAxe, ironAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 15, 0, 0, 3f),
            CreateCraftItem("iron_pickaxe", "Iron Pickaxe", ironPickaxe, ironPickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 15, 0, 0, 5f),
            CreateCraftItem("iron_sword", "Iron Sword", ironSword, ironSword, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 18),
            CreateCraftItem("repair_armor_kit", "Repair Armor Kit", shield, shield, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.4f, 64),
            CreateCraftItem("scissors", "Scissors", scissors, scissors, false, PlaceableType.None, new Vector3(-0.2f, -0.02f, 0f), new Vector3(0f, -94f, -92f), Vector3.one * 0.5f, 1, null, null, ToolCategory.Tools),
            CreateCraftItem("wood_helmet", "Wood Helmet", woodHelmet != null ? woodHelmet : ironHelmet, woodHelmet != null ? woodHelmet : ironHelmet, false, PlaceableType.None, new Vector3(-0.1f, -0.6f, 0.08f), Vector3.zero, Vector3.one * 0.4f, 1, null, null, ToolCategory.Armor, 50, 0, 5),
            CreateCraftItem("wood_armor", "Wood Armor", woodArmor != null ? woodArmor : ironArmor, woodArmor != null ? woodArmor : ironArmor, false, PlaceableType.None, new Vector3(0f, -0.2f, 0f), Vector3.zero, Vector3.one * 0.4f, 1, null, null, ToolCategory.Armor, 50, 0, 10),
            CreateCraftItem("stone_helmet", "Stone Helmet", stoneHelmet != null ? stoneHelmet : ironHelmet, stoneHelmet != null ? stoneHelmet : ironHelmet, false, PlaceableType.None, new Vector3(-0.1f, -0.6f, 0.08f), Vector3.zero, Vector3.one * 0.4f, 1, null, null, ToolCategory.Armor, 70, 0, 8),
            CreateCraftItem("stone_armor", "Stone Armor", stoneArmor != null ? stoneArmor : ironArmor, stoneArmor != null ? stoneArmor : ironArmor, false, PlaceableType.None, new Vector3(0f, -0.2f, 0f), Vector3.zero, Vector3.one * 0.4f, 1, null, null, ToolCategory.Armor, 70, 0, 12),
            CreateCraftItem("iron_helmet", "Iron Helmet", ironHelmet, ironHelmet, false, PlaceableType.None, new Vector3(-0.1f, -0.6f, 0.08f), Vector3.zero, Vector3.one * 0.4f, 1, null, null, ToolCategory.Armor, 80, 0, 15),
            CreateCraftItem("iron_armor", "Iron Armor", ironArmor, ironArmor, false, PlaceableType.None, new Vector3(0f, -0.2f, 0f), Vector3.zero, Vector3.one * 0.4f, 1, null, null, ToolCategory.Armor, 80, 0, 23),
            CreateCraftItem("ruby_axe", "Ruby Axe", redAxe, redAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 15, 0, 10, 2.5f),
            CreateCraftItem("sapphire_axe", "Sapphire Axe", blueAxe, blueAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 15, 0, 0, 2.5f),
            CreateCraftItem("emerald_axe", "Emerald Axe", greenAxe, greenAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 15, 0, 10, 2.5f),
            CreateCraftItem("copper_axe", "Copper Axe", copperAxe, copperAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 15, 0, 0, 2.5f),
            CreateCraftItem("gold_axe", "Gold Axe", goldAxe, goldAxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Tools, 15, 15, 0, 0, 2.5f),
            CreateCraftItem("ruby_pickaxe", "Ruby Pickaxe", redPickaxe, redPickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 15, 0, 0, 5f),
            CreateCraftItem("sapphire_pickaxe", "Sapphire Pickaxe", bluePickaxe, bluePickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 15, 0, 0, 5f),
            CreateCraftItem("emerald_pickaxe", "Emerald Pickaxe", greenPickaxe, greenPickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 15, 0, 10, 5f),
            CreateCraftItem("copper_pickaxe", "Copper Pickaxe", copperPickaxe, copperPickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 15, 0, 0, 5f),
            CreateCraftItem("gold_pickaxe", "Gold Pickaxe", goldPickaxe, goldPickaxe, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(8f, 168f, -84f), Vector3.one * 0.62f, 1, null, null, ToolCategory.Tools, 20, 15, 0, 0, 5f),
            CreateCraftItem("ruby_sword", "Ruby Sword", rubySword, rubySword, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 24),
            CreateCraftItem("sapphire_sword", "Sapphire Sword", sapphireSword, sapphireSword, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 21),
            CreateCraftItem("emerald_sword", "Emerald Sword", emeraldSword, emeraldSword, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 22),
            CreateCraftItem("copper_sword", "Copper Sword", copperSword, copperSword, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 18),
            CreateCraftItem("gold_sword", "Gold Sword", goldSword, goldSword, false, PlaceableType.None, new Vector3(0.04f, -0.03f, 0.04f), new Vector3(10f, 170f, -82f), Vector3.one * 0.58f, 1, null, null, ToolCategory.Melee, 15, 18),
            CreateCraftItem("wool", "Wool", wool, wool, false, PlaceableType.None, new Vector3(-0.1f, -0.06f, -0.01f), Vector3.zero, Vector3.one * 0.12f, 64, null, null, ToolCategory.Materials)
        };

        catalog.recipes = new[]
        {
            CreateRecipe("craft_workbench", "Workbench", "workbench", 1, false, "Player crafting: 5 Wood => 1 Workbench", new RecipeIngredient { itemId = "wood", amount = 5 }),
            CreateRecipe("craft_stick", "Stick", "stick", 3, false, "Player crafting: 1 Wood => 3 Stick", new RecipeIngredient { itemId = "wood", amount = 1 }),
            CreateRecipe("craft_torch", "Torch", "torch", 3, false, "1 Coal + 3 Stick => 3 Torch", new RecipeIngredient { itemId = "coal", amount = 1 }, new RecipeIngredient { itemId = "stick", amount = 3 }),
            CreateRecipe("craft_bandage", "Bandage", "bandage", 1, false, "3 Herb => 1 Bandage, heals 10", new RecipeIngredient { itemId = "herb", amount = 3 }),
            CreateRecipe("craft_first_aid", "First Aid Kit", "first_aid_kit", 1, false, "6 Herb => 1 First Aid Kit, heals 60", new RecipeIngredient { itemId = "herb", amount = 6 }),
            CreateRecipe("bench_workbench", "Workbench", "workbench", 1, true, "Workbench crafting: 5 Wood => 1 Workbench", new RecipeIngredient { itemId = "wood", amount = 5 }),
            CreateRecipe("bench_stick", "Stick", "stick", 3, true, "Workbench crafting: 1 Wood => 3 Stick", new RecipeIngredient { itemId = "wood", amount = 1 }),
            CreateRecipe("bench_wood_axe", "Wood Axe", "wood_axe", 1, true, "3 Wood + 2 Stick => Wood Axe, chops trees in 8 seconds", new RecipeIngredient { itemId = "wood", amount = 3 }, new RecipeIngredient { itemId = "stick", amount = 2 }),
            CreateRecipe("bench_wood_pickaxe", "Wood Pickaxe", "wood_pickaxe", 1, true, "4 Wood + 2 Stick => Wood Pickaxe, mines stone in 5 seconds", new RecipeIngredient { itemId = "wood", amount = 4 }, new RecipeIngredient { itemId = "stick", amount = 2 }),
            CreateRecipe("bench_wood_sword", "Wood Sword", "wood_sword", 1, true, "6 Wood + 3 Stick => Wood Sword, damage 20", new RecipeIngredient { itemId = "wood", amount = 6 }, new RecipeIngredient { itemId = "stick", amount = 3 }),
            CreateRecipe("bench_bed", "Bed", "bed", 1, true, "6 Wood + 2 Stick + 2 Wool => Bed", new RecipeIngredient { itemId = "wood", amount = 6 }, new RecipeIngredient { itemId = "stick", amount = 2 }, new RecipeIngredient { itemId = "wool", amount = 2 }),
            CreateRecipe("bench_chest", "Chest", "chest", 1, true, "8 Wood => Chest", new RecipeIngredient { itemId = "wood", amount = 8 }),
            CreateRecipe("bench_door", "Door", "door", 1, true, "6 Wood => Door", new RecipeIngredient { itemId = "wood", amount = 6 }),
            CreateRecipe("bench_fence", "Wood Fence", "wood_fence", 2, true, "4 Wood + 3 Stick => 2 Fences", new RecipeIngredient { itemId = "wood", amount = 4 }, new RecipeIngredient { itemId = "stick", amount = 3 }),
            CreateRecipe("bench_furnace", "Furnace", "furnace", 1, true, "8 Stone => Furnace", new RecipeIngredient { itemId = "stone", amount = 8 }),
            CreateRecipe("bench_stone_axe", "Stone Axe", "stone_axe", 1, true, "3 Stone + 2 Stick => Stone Axe", new RecipeIngredient { itemId = "stone", amount = 3 }, new RecipeIngredient { itemId = "stick", amount = 2 }),
            CreateRecipe("bench_stone_pickaxe", "Stone Pickaxe", "stone_pickaxe", 1, true, "3 Stone + 2 Stick => Stone Pickaxe", new RecipeIngredient { itemId = "stone", amount = 3 }, new RecipeIngredient { itemId = "stick", amount = 2 }),
            CreateRecipe("bench_stone_sword", "Stone Sword", "stone_sword", 1, true, "6 Stone + 1 Stick => Stone Sword", new RecipeIngredient { itemId = "stone", amount = 6 }, new RecipeIngredient { itemId = "stick", amount = 1 }),
            CreateRecipe("bench_auto_forge", "Auto Forge", "auto_forge", 1, true, "6 Iron Ore + 4 Stone + 2 Stick + 2 Stone Hammer => Auto Forge", new RecipeIngredient { itemId = "iron_ore", amount = 6 }, new RecipeIngredient { itemId = "stone", amount = 4 }, new RecipeIngredient { itemId = "stick", amount = 2 }, new RecipeIngredient { itemId = "stone_hammer", amount = 2 }),
            CreateRecipe("bench_wood_helmet", "Wood Helmet", "wood_helmet", 1, true, "8 Wood => Wood Helmet", new RecipeIngredient { itemId = "wood", amount = 8 }),
            CreateRecipe("bench_wood_armor", "Wood Armor", "wood_armor", 1, true, "12 Wood => Wood Armor", new RecipeIngredient { itemId = "wood", amount = 12 }),
            CreateRecipe("bench_stone_helmet", "Stone Helmet", "stone_helmet", 1, true, "8 Stone => Stone Helmet", new RecipeIngredient { itemId = "stone", amount = 8 }),
            CreateRecipe("bench_stone_armor", "Stone Armor", "stone_armor", 1, true, "12 Stone => Stone Armor", new RecipeIngredient { itemId = "stone", amount = 12 }),
            CreateRecipe("bench_forest_heart_detector", "Forest Heart Detector", "forest_heart_detector", 1, true, "30 Forest Guide + 6 Iron Ore => Forest Heart Detector", new RecipeIngredient { itemId = "forest_guide", amount = 30 }, new RecipeIngredient { itemId = "iron_ore", amount = 6 }),
            CreateRecipe("furnace_charcoal", "Charcoal", "charcoal", 1, CraftingStation.Furnace, "1 Wood => 1 Charcoal. One coal or charcoal fuels 3 furnace crafts.", new RecipeIngredient { itemId = "wood", amount = 1 }),
            CreateRecipe("furnace_coal", "Coal", "coal", 1, CraftingStation.Furnace, "1 Coal Ore => 1 Coal. One coal fuels 3 furnace crafts.", new RecipeIngredient { itemId = "coal_ore", amount = 1 }),
            CreateRecipe("forge_iron_bar", "Iron Bar", "iron_bar", 3, CraftingStation.Forge, "1 Iron Ore => 3 Iron Bars. Auto forge consumes 2 Coal Ore per craft.", new RecipeIngredient { itemId = "iron_ore", amount = 1 }),
            CreateRecipe("forge_iron_axe", "Iron Axe", "iron_axe", 1, CraftingStation.Forge, "3 Iron Ore + 2 Stick => Iron Axe", new RecipeIngredient { itemId = "iron_ore", amount = 3 }, new RecipeIngredient { itemId = "stick", amount = 2 }),
            CreateRecipe("forge_iron_pickaxe", "Iron Pickaxe", "iron_pickaxe", 1, CraftingStation.Forge, "3 Iron Ore + 2 Stick => Iron Pickaxe", new RecipeIngredient { itemId = "iron_ore", amount = 3 }, new RecipeIngredient { itemId = "stick", amount = 2 }),
            CreateRecipe("forge_iron_sword", "Iron Sword", "iron_sword", 1, CraftingStation.Forge, "2 Iron Ore + 1 Stick => Iron Sword", new RecipeIngredient { itemId = "iron_ore", amount = 2 }, new RecipeIngredient { itemId = "stick", amount = 1 }),
            CreateRecipe("forge_repair_armor_kit", "Repair Armor Kit", "repair_armor_kit", 2, CraftingStation.Forge, "1 Iron Ore => 2 Repair Armor Kits", new RecipeIngredient { itemId = "iron_ore", amount = 1 }),
            CreateRecipe("forge_scissors", "Scissors", "scissors", 1, CraftingStation.Forge, "1 Iron Ore => Scissors", new RecipeIngredient { itemId = "iron_ore", amount = 1 }),
            CreateRecipe("forge_iron_helmet", "Iron Helmet", "iron_helmet", 1, CraftingStation.Forge, "8 Iron Ore => Iron Helmet", new RecipeIngredient { itemId = "iron_ore", amount = 8 }),
            CreateRecipe("forge_iron_armor", "Iron Armor", "iron_armor", 1, CraftingStation.Forge, "12 Iron Ore => Iron Armor", new RecipeIngredient { itemId = "iron_ore", amount = 12 }),
            CreateRecipe("forge_forest_heart", "Forest Heart", "forest_heart", 1, CraftingStation.Forge, "30 Forest Guide + 6 Iron Ore => Forest Heart", new RecipeIngredient { itemId = "forest_guide", amount = 30 }, new RecipeIngredient { itemId = "iron_ore", amount = 6 }),
            CreateRecipe("forge_ruby_axe", "Ruby Axe", "ruby_axe", 1, CraftingStation.Forge, "3 Ruby Ore + 2 Iron Bars => Ruby Axe", new RecipeIngredient { itemId = "ruby_ore", amount = 3 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_sapphire_axe", "Sapphire Axe", "sapphire_axe", 1, CraftingStation.Forge, "3 Sapphire Ore + 2 Iron Bars => Sapphire Axe", new RecipeIngredient { itemId = "sapphire_ore", amount = 3 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_emerald_axe", "Emerald Axe", "emerald_axe", 1, CraftingStation.Forge, "3 Emerald Ore + 2 Iron Bars => Emerald Axe", new RecipeIngredient { itemId = "emerald_ore", amount = 3 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_copper_axe", "Copper Axe", "copper_axe", 1, CraftingStation.Forge, "3 Copper Ore + 2 Iron Bars => Copper Axe", new RecipeIngredient { itemId = "copper_ore", amount = 3 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_gold_axe", "Gold Axe", "gold_axe", 1, CraftingStation.Forge, "3 Gold Ore + 2 Iron Bars => Gold Axe", new RecipeIngredient { itemId = "gold_ore", amount = 3 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_ruby_pickaxe", "Ruby Pickaxe", "ruby_pickaxe", 1, CraftingStation.Forge, "4 Ruby Ore + 2 Iron Bars => Ruby Pickaxe", new RecipeIngredient { itemId = "ruby_ore", amount = 4 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_sapphire_pickaxe", "Sapphire Pickaxe", "sapphire_pickaxe", 1, CraftingStation.Forge, "4 Sapphire Ore + 2 Iron Bars => Sapphire Pickaxe", new RecipeIngredient { itemId = "sapphire_ore", amount = 4 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_emerald_pickaxe", "Emerald Pickaxe", "emerald_pickaxe", 1, CraftingStation.Forge, "4 Emerald Ore + 2 Iron Bars => Emerald Pickaxe", new RecipeIngredient { itemId = "emerald_ore", amount = 4 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_copper_pickaxe", "Copper Pickaxe", "copper_pickaxe", 1, CraftingStation.Forge, "4 Copper Ore + 2 Iron Bars => Copper Pickaxe", new RecipeIngredient { itemId = "copper_ore", amount = 4 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_gold_pickaxe", "Gold Pickaxe", "gold_pickaxe", 1, CraftingStation.Forge, "4 Gold Ore + 2 Iron Bars => Gold Pickaxe", new RecipeIngredient { itemId = "gold_ore", amount = 4 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_ruby_sword", "Ruby Sword", "ruby_sword", 1, CraftingStation.Forge, "6 Ruby Ore + 2 Iron Bars => Ruby Sword", new RecipeIngredient { itemId = "ruby_ore", amount = 6 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_sapphire_sword", "Sapphire Sword", "sapphire_sword", 1, CraftingStation.Forge, "6 Sapphire Ore + 2 Iron Bars => Sapphire Sword", new RecipeIngredient { itemId = "sapphire_ore", amount = 6 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_emerald_sword", "Emerald Sword", "emerald_sword", 1, CraftingStation.Forge, "6 Emerald Ore + 2 Iron Bars => Emerald Sword", new RecipeIngredient { itemId = "emerald_ore", amount = 6 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_copper_sword", "Copper Sword", "copper_sword", 1, CraftingStation.Forge, "6 Copper Ore + 2 Iron Bars => Copper Sword", new RecipeIngredient { itemId = "copper_ore", amount = 6 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 }),
            CreateRecipe("forge_gold_sword", "Gold Sword", "gold_sword", 1, CraftingStation.Forge, "6 Gold Ore + 2 Iron Bars => Gold Sword", new RecipeIngredient { itemId = "gold_ore", amount = 6 }, new RecipeIngredient { itemId = "iron_bar", amount = 2 })
        };

        catalog.RebuildLookup();
        EditorUtility.SetDirty(catalog);
    }

    private static void ConfigureExistingForestResources()
    {
        GameObject woodDrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Log_Pile_01.prefab");
        GameObject stoneDrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Environment/PP_Stone_Ground_01.prefab");
        GameObject ironOreDrop = LoadCrystal05("Iron");
        GameObject rubyOreDrop = LoadCrystal05("Red");
        GameObject sapphireOreDrop = LoadCrystal05("Blue");
        GameObject emeraldOreDrop = LoadCrystal05("Green");
        GameObject copperOreDrop = LoadCrystal05("Copper");
        GameObject goldOreDrop = LoadCrystal05("Gold");
        GameObject coalOreDrop = LoadCrystal05("Silver");
        int configuredTrees = 0;
        int configuredRocks = 0;
        Transform[] transforms = Object.FindObjectsOfType<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current.GetComponentInParent<PlayerToolController>() != null)
            {
                continue;
            }

            string lowerName = current.name.ToLowerInvariant();
            if (ShouldSkipHarvestResourceCandidate(current, lowerName))
            {
                continue;
            }

            bool treeLike = lowerName.Contains("tree") || lowerName.Contains("fir") || lowerName.Contains("birch") || lowerName.Contains("conifer");
            bool nonLivingTree = lowerName.Contains("stump") || lowerName.Contains("trunk") || lowerName.Contains("log") || lowerName.Contains("dead");
            bool crystalLike = lowerName.Contains("pp_crystal");
            bool rockLike = crystalLike || lowerName.Contains("rock") || lowerName.Contains("stone") || lowerName.Contains("ore");
            bool nonMineableStone = lowerName.Contains("wall")
                || lowerName.Contains("floor")
                || lowerName.Contains("slab")
                || lowerName.Contains("fragment")
                || lowerName.Contains("path")
                || lowerName.Contains("road")
                || lowerName.Contains("bridge")
                || lowerName.Contains("stair");

            if (current.GetComponentInParent<HarvestableResource>() != null || current.GetComponentInChildren<Renderer>() == null)
            {
                continue;
            }

            if (!treeLike && !rockLike)
            {
                continue;
            }

            if (treeLike && nonLivingTree)
            {
                continue;
            }

            if (rockLike && nonMineableStone)
            {
                continue;
            }

            if (!IsSafeHarvestResourceCandidate(current))
            {
                continue;
            }

            HarvestableResource harvestable = current.gameObject.AddComponent<HarvestableResource>();
            harvestable.resourceType = treeLike ? HarvestResourceType.Tree : HarvestResourceType.Rock;
            harvestable.maxHealth = 5;
            GameObject rockDropPrefab = stoneDrop;
            string rockDropItemName = "Stone";
            if (rockLike)
            {
                ResolveOreDrop(lowerName, ironOreDrop, rubyOreDrop, sapphireOreDrop, emeraldOreDrop, copperOreDrop, goldOreDrop, coalOreDrop, ref rockDropPrefab, ref rockDropItemName);
            }

            harvestable.dropPrefab = treeLike ? woodDrop : rockDropPrefab;
            harvestable.dropItemName = treeLike ? "Wood" : rockDropItemName;
            harvestable.dropCount = treeLike ? 5 : 4;
            harvestable.requiredToolTier = treeLike ? 0 : GetRequiredMiningTier(rockDropItemName);
            harvestable.ResetHealth();
            harvestable.CaptureOriginalScale();

            if (current.GetComponent<LockableTarget>() == null)
            {
                current.gameObject.AddComponent<LockableTarget>();
            }

            EnsureHarvestCollider(current.gameObject, treeLike);
            EditorUtility.SetDirty(current.gameObject);
            if (treeLike)
            {
                configuredTrees++;
            }
            else
            {
                configuredRocks++;
            }
        }

        Debug.Log("ForestGameplayInstaller: Configured " + configuredTrees + " trees and " + configuredRocks + " rocks as harvestable resources.");
    }

    private static bool ShouldSkipHarvestResourceCandidate(Transform current, string lowerName)
    {
        if (current == null)
        {
            return true;
        }

        if (lowerName == "forest_island"
            || lowerName == "mining_island"
            || lowerName == "combined_mining_alpine_scene"
            || lowerName == "terrain"
            || lowerName == "terrain_stone"
            || lowerName == "stones&rocks"
            || lowerName == "crystals&ores&veins")
        {
            return true;
        }

        if (current.childCount < 12)
        {
            return false;
        }

        if (!TryGetRenderBounds(current.gameObject, out Bounds bounds))
        {
            return false;
        }

        return bounds.size.x > 80f || bounds.size.y > 80f || bounds.size.z > 80f;
    }

    private static bool IsSafeHarvestResourceCandidate(Transform current)
    {
        if (current == null)
        {
            return false;
        }

        if (current.parent == null && current.childCount > 2)
        {
            return false;
        }

        Renderer[] renderers = current.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0 || renderers.Length > 8)
        {
            return false;
        }

        Collider[] colliders = current.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 12)
        {
            return false;
        }

        if (TryGetRenderBounds(current.gameObject, out Bounds bounds) && bounds.size.sqrMagnitude > 900f)
        {
            return false;
        }

        return true;
    }

    private static void ResolveOreDrop(string lowerName, GameObject ironOreDrop, GameObject rubyOreDrop, GameObject sapphireOreDrop, GameObject emeraldOreDrop, GameObject copperOreDrop, GameObject goldOreDrop, GameObject coalOreDrop, ref GameObject dropPrefab, ref string dropItemName)
    {
        if (lowerName.Contains("silver") || lowerName.Contains("coal"))
        {
            dropPrefab = coalOreDrop != null ? coalOreDrop : dropPrefab;
            dropItemName = "Coal Ore";
            return;
        }

        if (lowerName.Contains("iron"))
        {
            dropPrefab = ironOreDrop != null ? ironOreDrop : dropPrefab;
            dropItemName = "Iron Ore";
            return;
        }

        if (lowerName.Contains("red") || lowerName.Contains("ruby"))
        {
            dropPrefab = rubyOreDrop != null ? rubyOreDrop : dropPrefab;
            dropItemName = "Ruby Ore";
            return;
        }

        if (lowerName.Contains("blue") || lowerName.Contains("sapphire"))
        {
            dropPrefab = sapphireOreDrop != null ? sapphireOreDrop : dropPrefab;
            dropItemName = "Sapphire Ore";
            return;
        }

        if (lowerName.Contains("green") || lowerName.Contains("emerald"))
        {
            dropPrefab = emeraldOreDrop != null ? emeraldOreDrop : dropPrefab;
            dropItemName = "Emerald Ore";
            return;
        }

        if (lowerName.Contains("copper"))
        {
            dropPrefab = copperOreDrop != null ? copperOreDrop : dropPrefab;
            dropItemName = "Copper Ore";
            return;
        }

        if (lowerName.Contains("gold"))
        {
            dropPrefab = goldOreDrop != null ? goldOreDrop : dropPrefab;
            dropItemName = "Gold Ore";
        }
    }

    private static int GetRequiredMiningTier(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return 1;
        }

        string lower = itemName.ToLowerInvariant();
        if (lower.Contains("emerald") || lower.Contains("green")) return 5;
        if (lower.Contains("sapphire") || lower.Contains("blue")) return 4;
        if (lower.Contains("ruby") || lower.Contains("red") || lower.Contains("gold") || lower.Contains("copper")) return 3;
        if (lower.Contains("iron") || lower.Contains("coal") || lower.Contains("silver")) return 2;
        return 1;
    }

    private static void EnsureHarvestDropBootstrap()
    {
        HarvestDropBootstrap bootstrap = Object.FindObjectOfType<HarvestDropBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = new GameObject("HarvestDropBootstrap").AddComponent<HarvestDropBootstrap>();
        }

        bootstrap.woodDropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Props/PP_Log_Pile_01.prefab");
        bootstrap.stoneDropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Pack/Prefabs/Environment/PP_Stone_Ground_01.prefab");
        EditorUtility.SetDirty(bootstrap);
    }

    private static void EnsureHarvestCollider(GameObject root, bool isTree)
    {
        if (root.GetComponent<Collider>() != null)
        {
            return;
        }

        if (!TryGetRenderBounds(root, out Bounds bounds))
        {
            return;
        }

        if (isTree)
        {
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.center = root.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.42f, bounds.center.z));
            collider.height = Mathf.Max(1f, bounds.size.y * 0.84f / Mathf.Max(0.0001f, Mathf.Abs(root.transform.lossyScale.y)));
            collider.radius = Mathf.Max(0.25f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.22f / Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(root.transform.lossyScale.x), Mathf.Abs(root.transform.lossyScale.z))));
            return;
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = DivideByLossyScale(bounds.size, root.transform.lossyScale);
    }

    private static void EnsureCrosshair()
    {
        if (GameObject.Find("CrosshairCanvas") != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("CrosshairCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject root = new GameObject("Crosshair");
        root.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(32f, 32f);

        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Color color = new Color(1f, 1f, 1f, 0.92f);
        CreateCrosshairPart(root.transform, sprite, "CenterDot", Vector2.zero, new Vector2(4f, 4f), color);
        CreateCrosshairPart(root.transform, sprite, "Top", new Vector2(0f, 9f), new Vector2(2f, 8f), color);
        CreateCrosshairPart(root.transform, sprite, "Bottom", new Vector2(0f, -9f), new Vector2(2f, 8f), color);
        CreateCrosshairPart(root.transform, sprite, "Left", new Vector2(-9f, 0f), new Vector2(8f, 2f), color);
        CreateCrosshairPart(root.transform, sprite, "Right", new Vector2(9f, 0f), new Vector2(8f, 2f), color);
    }

    private static void CreateCrosshairPart(Transform parent, Sprite sprite, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        RectTransform rect = part.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = part.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
    }

    private static CraftingItemDefinition CreateCraftItem(string itemId, string displayName, GameObject heldPrefab, GameObject worldPrefab, bool placeable, PlaceableType placeableType, Vector3 heldPosition, Vector3 heldEuler, Vector3 heldScale, int maxStack, Vector3? worldEulerOffset = null, Vector3? worldScale = null, ToolCategory category = ToolCategory.Crafted, int durability = 0, int damage = 0, int defense = 0, int healAmount = 0, float harvestSeconds = 0f)
    {
        ApplyDefaultHeldTuning(itemId, ref heldPosition, ref heldEuler, ref heldScale);
        return new CraftingItemDefinition
        {
            itemId = itemId,
            displayName = displayName,
            heldPrefab = heldPrefab != null ? heldPrefab : worldPrefab,
            worldPrefab = worldPrefab != null ? worldPrefab : heldPrefab,
            placeable = placeable,
            placeableType = placeableType,
            heldLocalPosition = heldPosition,
            heldLocalEuler = heldEuler,
            heldLocalScale = heldScale,
            maxStack = maxStack,
            category = category,
            holdPose = ToolHoldPose.OneHandTool,
            worldEulerOffset = worldEulerOffset ?? Vector3.zero,
            worldScale = worldScale ?? Vector3.one,
            durability = durability,
            damage = damage,
            defense = defense,
            healAmount = healAmount,
            harvestSeconds = harvestSeconds
        };
    }

    private static bool ApplyDefaultHeldTuning(string itemId, ref Vector3 heldPosition, ref Vector3 heldEuler, ref Vector3 heldScale)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        switch (itemId)
        {
            case "wood_axe":
                heldPosition = new Vector3(-0.1f, -0.04f, -0.2f);
                heldEuler = new Vector3(1000f, 90f, -94f);
                heldScale = Vector3.one * 0.52f;
                return true;
            case "wood_pickaxe":
                heldPosition = new Vector3(-0.1f, -0.04f, -0.2f);
                heldEuler = new Vector3(1000f, 94f, -92f);
                heldScale = Vector3.one * 0.55f;
                return true;
            case "wood_sword":
                heldPosition = new Vector3(-0.1f, -0.04f, -0.1f);
                heldEuler = new Vector3(1000f, -86f, -92f);
                heldScale = Vector3.one * 0.52f;
                return true;
            case "stone_sword":
                heldPosition = new Vector3(-0.1f, -0.04f, -0.1f);
                heldEuler = new Vector3(1000f, -86f, -92f);
                heldScale = Vector3.one * 0.52f;
                return true;
            case "iron_sword":
            case "ruby_sword":
            case "sapphire_sword":
            case "emerald_sword":
            case "copper_sword":
            case "gold_sword":
                heldPosition = new Vector3(-0.1f, -0.04f, -0.1f);
                heldEuler = new Vector3(1000f, -86f, -92f);
                heldScale = Vector3.one * 0.52f;
                return true;
        }

        if (itemId.EndsWith("_axe", System.StringComparison.Ordinal))
        {
            heldPosition = new Vector3(-0.1f, -0.04f, -0.2f);
            heldEuler = new Vector3(1000f, 90f, -94f);
            heldScale = Vector3.one * 0.52f;
            return true;
        }

        if (itemId.EndsWith("_pickaxe", System.StringComparison.Ordinal))
        {
            heldPosition = new Vector3(-0.1f, -0.04f, -0.2f);
            heldEuler = new Vector3(1000f, 94f, -92f);
            heldScale = Vector3.one * 0.55f;
            return true;
        }

        return false;
    }

    private static CraftingRecipe CreateRecipe(string recipeId, string displayName, string outputItemId, int outputCount, bool requiresWorkbench, string description, params RecipeIngredient[] ingredients)
    {
        return CreateRecipe(recipeId, displayName, outputItemId, outputCount, requiresWorkbench ? CraftingStation.Workbench : CraftingStation.Player, description, ingredients);
    }

    private static CraftingRecipe CreateRecipe(string recipeId, string displayName, string outputItemId, int outputCount, CraftingStation station, string description, params RecipeIngredient[] ingredients)
    {
        return new CraftingRecipe
        {
            recipeId = recipeId,
            displayName = displayName,
            outputItemId = outputItemId,
            outputCount = outputCount,
            requiresWorkbench = station == CraftingStation.Workbench,
            station = station,
            description = description,
            ingredients = ingredients
        };
    }

    private static GameObject CreateHeldCraftProxyPrefab(string assetName, GameObject sourcePrefab)
    {
        if (sourcePrefab == null)
        {
            return null;
        }

        string assetPath = HeldCraftsFolder + "/" + assetName;
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject wrapper = new GameObject(System.IO.Path.GetFileNameWithoutExtension(assetName));
        GameObject visual = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (visual == null)
        {
            Object.DestroyImmediate(wrapper);
            return null;
        }

        visual.name = "Visual";
        visual.transform.SetParent(wrapper.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        RemoveColliders(wrapper);
        NormalizeProxyBounds(visual.transform);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, assetPath);
        Object.DestroyImmediate(wrapper);
        return prefab;
    }

    private static GameObject CreateColoredToolPrefab(string assetName, GameObject sourcePrefab, string materialName, Color color)
    {
        if (sourcePrefab == null)
        {
            return null;
        }

        string assetPath = HeldCraftsFolder + "/" + assetName;
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        Material material = CreateOrUpdateGeneratedMaterial(materialName, color);
        GameObject wrapper = new GameObject(System.IO.Path.GetFileNameWithoutExtension(assetName));
        GameObject visual = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (visual == null)
        {
            Object.DestroyImmediate(wrapper);
            return null;
        }

        visual.name = "Visual";
        visual.transform.SetParent(wrapper.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        ApplyMaterialToRenderers(visual, material);
        RemoveColliders(wrapper);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, assetPath);
        Object.DestroyImmediate(wrapper);
        return prefab;
    }

    private static GameObject CreateWoolPrefab()
    {
        string assetPath = HeldCraftsFolder + "/Held_Wool.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        Material material = CreateOrUpdateGeneratedMaterial("WoolWhite.mat", Color.white);
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Held_Wool";
        ApplyMaterialToRenderers(sphere, material);
        RemoveColliders(sphere);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sphere, assetPath);
        Object.DestroyImmediate(sphere);
        return prefab;
    }

    private static Material CreateOrUpdateGeneratedMaterial(string materialName, Color color)
    {
        string materialPath = GeneratedMaterialsFolder + "/" + materialName;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyMaterialToRenderers(GameObject root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void NormalizeProxyBounds(Transform visualRoot)
    {
        if (visualRoot == null || !TryGetRenderBounds(visualRoot.gameObject, out Bounds bounds))
        {
            return;
        }

        float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxSize <= 0.0001f)
        {
            return;
        }

        visualRoot.localScale = Vector3.one / maxSize;
        if (TryGetRenderBounds(visualRoot.gameObject, out bounds))
        {
            visualRoot.localPosition = -visualRoot.InverseTransformPoint(bounds.center);
        }
    }

    private static Vector3 FindFallbackSpawnPosition(GameObject player, CapsuleCollider capsule)
    {
        if (TryGetGroundedPlayerPosition(player, capsule, FallbackSpawnPosition, out Vector3 groundedSpawn))
        {
            return groundedSpawn;
        }

        return FallbackSpawnPosition;
    }

    private static void ApplySpawnPlacement(GameObject player, CapsuleCollider capsule, bool allowFallbackSpawn)
    {
        if (player == null || capsule == null)
        {
            return;
        }

        GameObject spawnObject = GameObject.Find("SpawnPoint");
        Transform spawnPoint = spawnObject != null ? spawnObject.transform : null;
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            Physics.SyncTransforms();
            return;
        }

        if (allowFallbackSpawn)
        {
            player.transform.position = FindFallbackSpawnPosition(player, capsule);
            player.transform.rotation = Quaternion.identity;
            SnapPlayerToGround(player, capsule);
            Physics.SyncTransforms();
        }
    }

    private static Vector3 FindGroundedPosition(GameObject player, CapsuleCollider capsule, Vector3 anchorPosition)
    {
        if (TryGetGroundedPlayerPosition(player, capsule, anchorPosition, out Vector3 groundedPosition))
        {
            return groundedPosition;
        }

        return anchorPosition;
    }

    private static bool TryFindSpawnPoint(out Transform spawnPoint)
    {
        spawnPoint = FindTaggedSpawnPoint();
        if (spawnPoint != null)
        {
            return true;
        }

        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && IsSpawnPointName(candidate.name))
            {
                spawnPoint = candidate;
                return true;
            }
        }

        return false;
    }

    private static Transform FindTaggedSpawnPoint()
    {
        try
        {
            GameObject taggedSpawn = GameObject.FindWithTag("SpawnPoint");
            return taggedSpawn != null ? taggedSpawn.transform : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSpawnPointName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string normalized = objectName.Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();

        return normalized == "spawnpoint"
            || normalized == "playerspawn"
            || normalized == "playerstart"
            || normalized == "startpoint";
    }

    private static void DisableSpawnPointColliders(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return;
        }

        Collider[] colliders = spawnPoint.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    private static bool TryFindSafeSpawnPosition(GameObject player, CapsuleCollider capsule, out Vector3 spawnPosition)
    {
        spawnPosition = player != null ? player.transform.position : FallbackSpawnPosition;

        Transform house = FindPreferredVillageHouse();
        if (house != null)
        {
            Vector3 anchor = house.position;
            float baseDistance = 8f;
            if (TryGetRenderBounds(house.gameObject, out Bounds bounds))
            {
                anchor = bounds.center;
                baseDistance = Mathf.Max(bounds.extents.x, bounds.extents.z) + 3f;
            }

            if (TryFindSafeSpawnNear(player, capsule, anchor, baseDistance, out spawnPosition))
            {
                return true;
            }
        }

        if (house != null)
        {
            Vector3 side = house.right;
            side.y = 0f;
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.right;
            }

            spawnPosition = house.position + side.normalized * 8f;
            spawnPosition.y = house.position.y + 0.03f;
            return true;
        }

        if (TryGetGroundedPlayerPosition(player, capsule, spawnPosition, out spawnPosition)
            && HasStandingRoom(player.transform, capsule, spawnPosition))
        {
            return true;
        }

        return false;
    }

    private static Transform FindPreferredVillageHouse()
    {
        Transform village = FindFishingVillageRoot();
        string[] names =
        {
            "WoodHouse3",
            "WoodenHouse3",
            "Wooden House3",
            "Wood House3",
            "WoodHouse2",
            "WoodHouse",
            "WoodenHouse2",
            "Wooden House2",
            "WoodenHouse1",
            "Wooden House1",
            "WoodenHouse"
        };

        for (int i = 0; i < names.Length; i++)
        {
            Transform match = village != null
                ? FindDescendantByNameContains(village, names[i])
                : FindTransformByNameContains(names[i]);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Transform FindFishingVillageRoot()
    {
        Transform village = FindTransformByNameContains("FISHING VILLAGE");
        if (village != null)
        {
            return village;
        }

        return FindTransformByNameContains("FINISHING VILLAGE");
    }

    private static Transform FindDescendantByNameContains(Transform root, string namePart)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return transform;
            }
        }

        return null;
    }

    private static Transform FindTransformByNameContains(string namePart)
    {
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return transform;
            }
        }

        return null;
    }

    private static bool TryFindSafeSpawnNear(GameObject player, CapsuleCollider capsule, Vector3 anchor, float baseDistance, out Vector3 spawnPosition)
    {
        spawnPosition = anchor;
        float bestScore = float.MaxValue;
        bool found = false;

        for (int distanceIndex = 0; distanceIndex < SpawnExtraDistances.Length; distanceIndex++)
        {
            float distance = baseDistance + SpawnExtraDistances[distanceIndex];
            int samples = distance <= 0.01f ? 1 : SpawnAngleSamples;

            for (int angleIndex = 0; angleIndex < samples; angleIndex++)
            {
                float angle = samples == 1 ? 0f : angleIndex * Mathf.PI * 2f / samples;
                Vector3 candidate = anchor;
                candidate.x += Mathf.Cos(angle) * distance;
                candidate.z += Mathf.Sin(angle) * distance;

                if (!TryGetGroundedPlayerPosition(player, capsule, candidate, out Vector3 grounded)
                    || !HasStandingRoom(player.transform, capsule, grounded))
                {
                    continue;
                }

                float score = Vector2.Distance(
                    new Vector2(anchor.x, anchor.z),
                    new Vector2(grounded.x, grounded.z));

                if (score < bestScore)
                {
                    bestScore = score;
                    spawnPosition = grounded;
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }
        }

        return found;
    }

    private static bool TryFindOpenGroundNear(GameObject player, CapsuleCollider capsule, Vector3 anchorPosition, out Vector3 openPosition)
    {
        openPosition = anchorPosition;

        for (int distanceIndex = 0; distanceIndex < SpawnExtraDistances.Length; distanceIndex++)
        {
            float radius = SpawnExtraDistances[distanceIndex];

            for (int angleIndex = 0; angleIndex < SpawnAngleSamples; angleIndex++)
            {
                float angle = angleIndex * Mathf.PI * 2f / SpawnAngleSamples;
                Vector3 candidate = anchorPosition;
                candidate.x += Mathf.Cos(angle) * radius;
                candidate.z += Mathf.Sin(angle) * radius;

                if (!TryGetGroundedPlayerPosition(player, capsule, candidate, out Vector3 groundedPosition)
                    || !HasStandingRoom(player.transform, capsule, groundedPosition))
                {
                    continue;
                }

                openPosition = groundedPosition;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetGroundedPlayerPosition(GameObject player, CapsuleCollider capsule, Vector3 anchorPosition, out Vector3 groundedPosition)
    {
        groundedPosition = anchorPosition;

        if (!TryGetGroundHeight(player.transform, anchorPosition, out float groundY))
        {
            return false;
        }

        groundedPosition = new Vector3(
            anchorPosition.x,
            groundY + 0.03f,
            anchorPosition.z);
        return true;
    }

    private static bool TryGetGroundHeight(Transform player, Vector3 anchorPosition, out float groundY)
    {
        groundY = 0f;

        if (TrySampleUnityTerrain(anchorPosition, out groundY))
        {
            return true;
        }

        if (TrySampleProceduralTerrain(anchorPosition, out groundY))
        {
            return true;
        }

        if (TryFindGroundHit(player, anchorPosition, out RaycastHit hit))
        {
            groundY = hit.point.y;
            return true;
        }

        return false;
    }

    private static bool TrySampleUnityTerrain(Vector3 position, out float groundY)
    {
        groundY = 0f;
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (position.x < terrainPosition.x
                || position.z < terrainPosition.z
                || position.x > terrainPosition.x + size.x
                || position.z > terrainPosition.z + size.z)
            {
                continue;
            }

            groundY = terrain.SampleHeight(position) + terrainPosition.y;
            return true;
        }

        return false;
    }

    private static bool TrySampleProceduralTerrain(Vector3 position, out float groundY)
    {
        groundY = 0f;
        ProceduralTerrain[] terrains = Object.FindObjectsOfType<ProceduralTerrain>(true);
        for (int i = 0; i < terrains.Length; i++)
        {
            ProceduralTerrain terrain = terrains[i];
            if (terrain == null || !terrain.ContainsWorldPosition(position))
            {
                continue;
            }

            groundY = terrain.SampleHeightWorld(position.x, position.z);
            return true;
        }

        return false;
    }

    private static bool TryFindGroundHit(Transform player, Vector3 anchorPosition, out RaycastHit bestHit)
    {
        bestHit = default;
        Physics.SyncTransforms();

        Vector3 origin = anchorPosition + Vector3.up * 160f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 500f, ~0, QueryTriggerInteraction.Ignore);
        float bestScore = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null
                || !hit.collider.enabled
                || hit.collider.isTrigger
                || hit.normal.y < 0.45f
                || hit.collider.transform.IsChildOf(player)
                || IsBadSpawnGroundCollider(hit.collider))
            {
                continue;
            }

            float score = hit.distance + (IsPreferredSpawnGroundCollider(hit.collider) ? 0f : 100000f);
            if (score < bestScore)
            {
                bestScore = score;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private static bool HasStandingRoom(Transform player, CapsuleCollider capsule, Vector3 position)
    {
        float radiusScale = Mathf.Max(Mathf.Abs(player.lossyScale.x), Mathf.Abs(player.lossyScale.z));
        float radius = Mathf.Max(0.05f, capsule.radius * radiusScale * 0.92f);
        float height = Mathf.Max(radius * 2f, capsule.height * Mathf.Abs(player.lossyScale.y));
        Vector3 center = position + Vector3.Scale(capsule.center, player.lossyScale);
        float halfLine = Mathf.Max(0f, height * 0.5f - radius - 0.02f);
        Vector3 bottom = center + Vector3.down * halfLine;
        Vector3 top = center + Vector3.up * halfLine;
        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null
                || !hit.enabled
                || hit.isTrigger
                || hit.transform.IsChildOf(player)
                || IsPreferredSpawnGroundCollider(hit))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static void SnapPlayerToGround(GameObject player, CapsuleCollider capsule)
    {
        if (player == null || capsule == null)
        {
            return;
        }

        if (!TryGetGroundHeight(player.transform, player.transform.position, out float groundY))
        {
            Debug.LogWarning("ForestGameplayInstaller: Could not find walkable ground below Player spawn.");
            return;
        }

        Vector3 position = player.transform.position;
        position.y = groundY + 0.03f;
        player.transform.position = position;
        Physics.SyncTransforms();
    }

    private static bool IsBadSpawnGroundCollider(Collider collider)
    {
        if (collider == null)
        {
            return true;
        }

        if (collider.GetComponentInParent<HarvestableResource>() != null)
        {
            return true;
        }

        if (IsPreferredSpawnGroundCollider(collider))
        {
            return false;
        }

        string lowerName = GetColliderSearchName(collider);
        return lowerName.Contains("tree")
            || lowerName.Contains("crystal")
            || lowerName.Contains("ore")
            || lowerName.Contains("rock")
            || lowerName.Contains("stone");
    }

    private static bool IsPreferredSpawnGroundCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider is TerrainCollider
            || collider.GetComponentInParent<Terrain>() != null
            || collider.GetComponentInParent<ProceduralTerrain>() != null)
        {
            return true;
        }

        string lowerName = GetColliderSearchName(collider);
        return lowerName.Contains("terrain")
            || lowerName.Contains("ground")
            || lowerName.Contains("floor")
            || lowerName.Contains("land");
    }

    private static string GetColliderSearchName(Collider collider)
    {
        Transform transform = collider.transform;
        string name = collider.name;
        if (transform.parent != null)
        {
            name += " " + transform.parent.name;
        }

        if (transform.root != null)
        {
            name += " " + transform.root.name;
        }

        return name.ToLowerInvariant();
    }

    private static float GetCapsuleBottomOffset(Transform player, CapsuleCollider capsule)
    {
        if (capsule.direction != 1)
        {
            return capsule.bounds.min.y - player.position.y;
        }

        float centerY = player.TransformPoint(capsule.center).y;
        float scaledHalfHeight = capsule.height * 0.5f * Mathf.Abs(player.lossyScale.y);
        return centerY - scaledHalfHeight - player.position.y;
    }

    private static void EnsureGeneratedFolders()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(HeldCraftsFolder))
        {
            AssetDatabase.CreateFolder(GeneratedFolder, "HeldCrafts");
        }

        if (!AssetDatabase.IsValidFolder(GeneratedMaterialsFolder))
        {
            AssetDatabase.CreateFolder(GeneratedFolder, "Materials");
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void RemoveColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = rigidbodies.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(rigidbodies[i]);
        }
    }

    private static bool TryGetRenderBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : null;
        if (renderers == null || renderers.Length == 0)
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
            size.x / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)),
            size.y / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)),
            size.z / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
