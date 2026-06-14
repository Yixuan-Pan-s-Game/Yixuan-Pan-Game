using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ForestRuntimePlayerBootstrap : MonoBehaviour
{
    public const string AlpineSceneName = "AlpineWoodlandDemo";
    public const string MiningSceneName = "Demo_01";

    private const float GroundRayHeight = 80f;
    private const float GroundRayDistance = 180f;
    private const float SpawnClearance = 0.08f;
    private static bool firstSpawnDone;
    private static string pendingSpawnId;
    private static bool suppressNextPlacement;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRuntimeState()
    {
        firstSpawnDone = false;
        pendingSpawnId = null;
        suppressNextPlacement = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapExists()
    {
        if (FindObjectOfType<ForestRuntimePlayerBootstrap>() == null)
        {
            DontDestroyOnLoad(new GameObject("ForestRuntimePlayerBootstrap").AddComponent<ForestRuntimePlayerBootstrap>().gameObject);
        }
    }

    public static void RequestSpawn(string spawnId)
    {
        pendingSpawnId = spawnId;
        suppressNextPlacement = false;
    }

    public static void TeleportCurrentPlayerToSpawn(string spawnId)
    {
        GameObject player = FindScenePlayer();
        if (player == null)
        {
            Debug.LogError("ForestRuntimePlayerBootstrap: Cannot teleport because no Player was found.");
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!CanResolveRequiredSpawn(spawnId, activeSceneName))
        {
            return;
        }

        pendingSpawnId = spawnId;
        suppressNextPlacement = true;
        EnsureWalkableCollidersForCurrentScene();
        Vector3 anchor = ResolveSpawnAnchor(spawnId, activeSceneName);
        Quaternion rotation = Quaternion.identity;
        if (TryFindNamedSpawnInScene(spawnId, activeSceneName, out Transform namedSpawn))
        {
            anchor = namedSpawn.position;
            rotation = namedSpawn.rotation;
        }

        Vector3 grounded = SnapPlayerPositionToGround(player, anchor);
        TeleportPlayer(player, grounded, rotation);
        BindCamera(player.transform);
        pendingSpawnId = null;
        suppressNextPlacement = false;
        firstSpawnDone = true;
        EnsureScenePortals();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private IEnumerator Start()
    {
        yield return null;
        BootstrapCurrentScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(BootstrapAfterSceneLoad());
    }

    private IEnumerator BootstrapAfterSceneLoad()
    {
        yield return null;
        BootstrapCurrentScene();
    }

    private void BootstrapCurrentScene()
    {
        GameObject player = FindScenePlayer();
        if (player == null)
        {
            Debug.LogError("ForestRuntimePlayerBootstrap: No Player was found in a loaded gameplay scene. Keep the copied Forest Player inside AlpineWoodlandDemo or Demo_01 and tag it Player.");
            return;
        }

        ConfigureScenePlayer(player);
        EnsureWalkableCollidersForCurrentScene();
        PlacePlayerForCurrentScene(player);
        BindCamera(player.transform);
        EnsureRuntimeUI();
        EnsurePlayerStatusUI();
        ConfigureResourcesForCurrentScene();
        EnsureScenePortals();
    }

    private static GameObject FindScenePlayer()
    {
        GameObject taggedPlayer = null;
        try { taggedPlayer = GameObject.FindWithTag("Player"); } catch (UnityException) { }
        if (IsInLoadedGameplayScene(taggedPlayer)) return taggedPlayer;

        PlayerMovement[] movements = FindObjectsOfType<PlayerMovement>(true);
        for (int i = 0; i < movements.Length; i++)
        {
            if (movements[i] != null && IsInLoadedGameplayScene(movements[i].gameObject)) return movements[i].gameObject;
        }

        PlayerToolController[] toolControllers = FindObjectsOfType<PlayerToolController>(true);
        for (int i = 0; i < toolControllers.Length; i++)
        {
            if (toolControllers[i] != null && IsInLoadedGameplayScene(toolControllers[i].gameObject)) return toolControllers[i].gameObject;
        }

        return null;
    }

    private static bool IsInLoadedGameplayScene(GameObject target)
    {
        if (target == null || !target.scene.isLoaded) return false;
        string sceneName = target.scene.name;
        return sceneName == AlpineSceneName || sceneName == MiningSceneName;
    }
    private static void ConfigureScenePlayer(GameObject player)
    {
        TrySetTag(player, "Player");

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.isTrigger = false;
        }

        ConfigureMovementForGameplay(player);
        DisableAlpineSpawnCubeSolidColliders();
        DisableAlpineStonePathSolidColliders();
        BindExistingCharacter(player);
    }
    private static void ConfigureMovementForGameplay(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null) return;

        movement.moveSpeed = 6f;
        movement.acceleration = 18f;
        movement.braking = 24f;
        movement.airControlPercent = 0.55f;
        movement.resolveSpawnOverlaps = true;
        movement.collisionSkin = 0.03f;
        movement.maxDepenetrationStep = 0.5f;
        movement.depenetrationPasses = 3;
        movement.jumpForce = 7.5f;
        movement.groundProbeDistance = 0.35f;
        movement.maxGroundAngle = 55f;
        movement.groundedDownForce = 2f;
        movement.coyoteTime = 0.12f;
        movement.jumpBufferTime = 0.15f;
        movement.rescueWhenFallingThroughTerrain = true;
    }

    private static void DisableAlpineSpawnCubeSolidColliders()
    {
        Transform cube = FindTransformByExactNameInScene("Cube", AlpineSceneName);
        if (cube == null) return;

        Collider[] colliders = cube.GetComponentsInChildren<Collider>(true);
        int disabled = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger || !collider.enabled) continue;
            collider.enabled = false;
            disabled++;
        }

        if (disabled > 0)
        {
            Debug.Log("ForestRuntimePlayerBootstrap: Disabled " + disabled + " solid collider(s) on Alpine spawn Cube so the player is not trapped near spawn.");
        }
    }

    private static void DisableAlpineStonePathSolidColliders()
    {
        Scene alpineScene = SceneManager.GetSceneByName(AlpineSceneName);
        if (!alpineScene.isLoaded) return;

        int disabled = 0;
        GameObject[] roots = alpineScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                Transform current = transforms[j];
                if (current == null || !current.name.StartsWith("StonePath", System.StringComparison.OrdinalIgnoreCase)) continue;

                Collider[] colliders = current.GetComponentsInChildren<Collider>(true);
                for (int k = 0; k < colliders.Length; k++)
                {
                    Collider collider = colliders[k];
                    if (collider == null || collider.isTrigger || !collider.enabled) continue;
                    collider.enabled = false;
                    disabled++;
                }
            }
        }

        if (disabled > 0)
        {
            Debug.Log("ForestRuntimePlayerBootstrap: Disabled " + disabled + " decorative StonePath collider(s) in AlpineWoodlandDemo so ground probing and jumps use the real terrain.");
        }
    }
    private static void BindExistingCharacter(GameObject player)
    {
        Transform visual = player.transform.Find("CharacterVisual");
        if (visual == null)
        {
            Debug.LogError("ForestRuntimePlayerBootstrap: Scene Player has no CharacterVisual. Copy the original Forest player visual instead of using runtime fallback.");
            return;
        }

        visual.gameObject.SetActive(true);
        CharacterRunAnimator runAnimator = player.GetComponent<CharacterRunAnimator>();
        if (runAnimator != null)
        {
            runAnimator.SetVisualRoot(visual);
        }

        PlayerToolController tools = player.GetComponent<PlayerToolController>();
        if (tools != null)
        {
            tools.visualRoot = visual;
        }

        PolytopeModularCharacterVisual modularVisual = visual.GetComponent<PolytopeModularCharacterVisual>();
        if (modularVisual == null)
        {
            modularVisual = visual.gameObject.AddComponent<PolytopeModularCharacterVisual>();
        }

        modularVisual.shirtColor = new Color(0.34f, 0.17f, 0.07f, 1f);
        modularVisual.trousersColor = new Color(0.18f, 0.09f, 0.04f, 1f);
        modularVisual.ApplyDefaultPartVisibility();
    }
    private static void PlacePlayerForCurrentScene(GameObject player)
    {
        if (player == null) return;

        if (suppressNextPlacement) return;
        bool shouldPlace = !firstSpawnDone || !string.IsNullOrEmpty(pendingSpawnId);
        if (!shouldPlace) return;

        string spawnSceneName = string.IsNullOrEmpty(pendingSpawnId) || pendingSpawnId.Equals("Cube", System.StringComparison.OrdinalIgnoreCase)
            ? AlpineSceneName
            : SceneManager.GetActiveScene().name;
        Scene spawnScene = SceneManager.GetSceneByName(spawnSceneName);
        if (spawnScene.isLoaded)
        {
            SceneManager.SetActiveScene(spawnScene);
        }

        Vector3 anchor = ResolveSpawnAnchor(pendingSpawnId, spawnSceneName);
        Quaternion rotation = Quaternion.identity;
        if (TryFindNamedSpawnInScene(pendingSpawnId, spawnSceneName, out Transform namedSpawn))
        {
            anchor = namedSpawn.position;
            rotation = namedSpawn.rotation;
        }

        Vector3 grounded = SnapPlayerPositionToGround(player, anchor);
        TeleportPlayer(player, grounded, rotation);
        Debug.Log("ForestRuntimePlayerBootstrap: Player ready in " + SceneManager.GetActiveScene().name + " at " + grounded + ".");
        firstSpawnDone = true;
        EnsureScenePortals();
        pendingSpawnId = null;
        suppressNextPlacement = false;
    }

    private static bool CanResolveRequiredSpawn(string spawnId, string sceneName)
    {
        if (sceneName == MiningSceneName && !string.IsNullOrEmpty(spawnId) && spawnId.Equals("Trans_Demo", System.StringComparison.OrdinalIgnoreCase))
        {
            if (FindTransformByExactNameInScene("Trans_Demo", MiningSceneName) != null)
            {
                return true;
            }

            Debug.LogError("ForestRuntimePlayerBootstrap: Demo_01 has no Trans_Demo. Teleport cancelled.");
            return false;
        }

        if (sceneName == AlpineSceneName && !string.IsNullOrEmpty(spawnId) && spawnId.Equals("trans", System.StringComparison.OrdinalIgnoreCase))
        {
            if (FindTransformByExactNameInScene("trans", AlpineSceneName) != null)
            {
                return true;
            }

            Debug.LogError("ForestRuntimePlayerBootstrap: AlpineWoodlandDemo has no trans. Teleport cancelled.");
            return false;
        }

        return true;
    }
    private static Vector3 ResolveSpawnAnchor(string spawnId, string sceneName)
    {
        if (sceneName == AlpineSceneName)
        {
            if (!string.IsNullOrEmpty(spawnId) && spawnId.Equals("trans", System.StringComparison.OrdinalIgnoreCase))
            {
                Transform alpinePortal = FindTransformByExactNameInScene("trans", AlpineSceneName);
                if (alpinePortal != null) return GetSpawnPointAbove(alpinePortal);
            }

            Transform spawnCube = FindTransformByExactNameInScene("Cube", AlpineSceneName);
            if (spawnCube != null) return GetSpawnPointAbove(spawnCube);
            return new Vector3(0f, 20f, 0f);
        }

        if (sceneName == MiningSceneName)
        {
            Transform demoSpawn = FindTransformByExactNameInScene("Trans_Demo", MiningSceneName);
            if (demoSpawn != null) return GetSpawnPointAbove(demoSpawn);

            Debug.LogError("ForestRuntimePlayerBootstrap: Demo_01 has no Trans_Demo spawn point.");
            return new Vector3(0f, 20f, 0f);
        }

        return new Vector3(0f, 20f, 0f);
    }

    private static bool TryFindNamedSpawnInScene(string spawnId, string sceneName, out Transform spawn)
    {
        spawn = null;
        if (string.IsNullOrEmpty(spawnId) || string.IsNullOrEmpty(sceneName)) return false;

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current.gameObject.scene.name != sceneName) continue;
            if (current.name.Equals(spawnId, System.StringComparison.OrdinalIgnoreCase))
            {
                spawn = current;
                return true;
            }
        }

        return false;
    }

    private static Vector3 SnapPlayerPositionToGround(GameObject player, Vector3 anchor)
    {
        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        float bottomOffset = capsule != null ? capsule.bounds.min.y - player.transform.position.y : 0f;
        RaycastHit[] hits = Physics.RaycastAll(anchor + Vector3.up * GroundRayHeight, Vector3.down, GroundRayDistance, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        RaycastHit bestHit = default;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || !hit.collider.enabled || hit.collider.isTrigger || hit.collider.transform.IsChildOf(player.transform) || hit.normal.y < 0.35f)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        if (!found) return anchor;
        return new Vector3(anchor.x, bestHit.point.y - bottomOffset + SpawnClearance, anchor.z);
    }

    private static void TeleportPlayer(GameObject player, Vector3 position, Quaternion rotation)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }

        player.transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
    }

    private static void BindCamera(Transform player)
    {
        if (player == null) return;

        Camera main = Camera.main;
        if (main == null)
        {
            main = FindObjectOfType<Camera>(true);
        }
        if (main == null)
        {
            main = new GameObject("Main Camera").AddComponent<Camera>();
        }

        main.gameObject.SetActive(true);
        main.enabled = true;
        main.name = "Main Camera";
        TrySetTag(main.gameObject, "MainCamera");
        DisableOtherCameras(main);
        if (main.GetComponent<AudioListener>() == null) main.gameObject.AddComponent<AudioListener>();
        ForestBackgroundMusic music = GetOrAdd<ForestBackgroundMusic>(main.gameObject);
        music.musicResourcePath = "happy_adveture";

        ThirdPersonCameraFollow follow = GetOrAdd<ThirdPersonCameraFollow>(main.gameObject);
        follow.target = player;
        follow.targetOffset = new Vector3(0f, 1.35f, 0f);
        follow.lookOffset = new Vector3(0f, 1.35f, 0f);
        follow.distance = 4.5f;
        follow.minDistance = 2.6f;
        follow.maxDistance = 7f;
        follow.SnapToTarget();

        PlayerToolController tools = player.GetComponent<PlayerToolController>();
        if (tools != null) tools.playerCamera = main;

        EnsureSingleAudioListener(main.GetComponent<AudioListener>());
    }

    private static void DisableOtherCameras(Camera keep)
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera != keep)
            {
                camera.enabled = false;
            }
        }
    }
    private static void EnsureSingleAudioListener(AudioListener keep)
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener != keep)
            {
                listener.enabled = false;
            }
        }
    }

    private static void EnsurePlayerStatusUI()
    {
        if (FindObjectOfType<PlayerStatusUI>(true) == null)
        {
            new GameObject("PlayerStatusUI").AddComponent<PlayerStatusUI>();
        }
    }
    private static void EnsureRuntimeUI()
    {
        GameUIBootstrap ui = FindObjectOfType<GameUIBootstrap>() ?? new GameObject("GameUIBootstrap").AddComponent<GameUIBootstrap>();
        ui.BootstrapUI();
    }

    private static void EnsureWalkableCollidersForCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded || activeScene.name != MiningSceneName) return;

        int added = EnsureNamedTerrainMeshColliders(activeScene.name);
        if (added > 0)
        {
            Debug.Log("ForestRuntimePlayerBootstrap: Added " + added + " MeshCollider(s) to Demo_01 terrain meshes before player placement.");
        }
    }

    private static int EnsureNamedTerrainMeshColliders(string sceneName)
    {
        MeshFilter[] filters = FindObjectsOfType<MeshFilter>(true);
        int added = 0;
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null || filter.gameObject.scene.name != sceneName) continue;
            if (!filter.gameObject.activeInHierarchy) continue;
            if (!IsDemoWalkableTerrainMesh(filter.transform)) continue;

            MeshCollider meshCollider = filter.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                added++;
            }

            meshCollider.sharedMesh = filter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshCollider.enabled = true;
        }

        return added;
    }

    private static bool IsDemoWalkableTerrainMesh(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            string name = current.name;
            if (name == "Terrain" || name == "Terrain_Stone") return true;

            string lower = name.ToLowerInvariant();
            if (lower.Contains("ground") || lower.Contains("floor") || lower.Contains("path")) return true;
        }

        return false;
    }
    private static void ConfigureResourcesForCurrentScene()
    {
        ConfigureTreeResources();
        HarvestDropBootstrap drops = FindObjectOfType<HarvestDropBootstrap>() ?? new GameObject("HarvestDropBootstrap").AddComponent<HarvestDropBootstrap>();
        drops.ConfigureExistingResources();
    }

    private static void ConfigureTreeResources()
    {
        Transform[] transforms = FindObjectsOfType<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current.GetComponentInParent<PlayerToolController>() != null || current.GetComponentInParent<HarvestableResource>() != null) continue;
            string lower = current.name.ToLowerInvariant();
            if (!lower.Contains("tree") || current.GetComponentInChildren<Renderer>(true) == null) continue;
            if (lower.Contains("forest") || lower.Contains("terrain") || lower.Contains("island")) continue;

            HarvestableResource resource = current.gameObject.AddComponent<HarvestableResource>();
            resource.resourceType = HarvestResourceType.Tree;
            resource.maxHealth = 5;
            resource.dropItemName = "Wood";
            resource.dropCount = 5;
            resource.ResetHealth();
            resource.CaptureOriginalScale();
            EnsureSolidCollider(current.gameObject);
        }
    }

    private static void EnsureSolidCollider(GameObject target)
    {
        if (target.GetComponent<Collider>() != null || target.GetComponentInChildren<Collider>(true) != null) return;
        if (target.GetComponent<MeshFilter>() != null)
        {
            MeshCollider mesh = target.AddComponent<MeshCollider>();
            mesh.convex = true;
            return;
        }

        BoxCollider box = target.AddComponent<BoxCollider>();
        if (TryGetRendererBounds(target.transform, out Bounds bounds))
        {
            box.center = target.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(Mathf.Max(0.5f, bounds.size.x), Mathf.Max(0.5f, bounds.size.y), Mathf.Max(0.5f, bounds.size.z));
        }
    }

    private static void EnsureScenePortals()
    {
        Transform alpinePortal = FindTransformByExactNameInScene("trans", AlpineSceneName);
        if (alpinePortal != null)
        {
            ForestScenePortal.EnsurePortal("Portal_To_Mining", MiningSceneName, "Trans_Demo", alpinePortal.position + Vector3.up * 0.2f);
        }

    }

    private static Transform FindTransformByExactNameInScene(string text, string sceneName)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(sceneName)) return null;
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name == text && current.gameObject.scene.name == sceneName)
            {
                return current;
            }
        }
        return null;
    }
    private static Transform FindTransformByExactName(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name == text) return current;
        }
        return null;
    }
    private static Transform FindTransformByNameContains(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        string lowerText = text.ToLowerInvariant();
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name.ToLowerInvariant().Contains(lowerText)) return current;
        }
        return null;
    }

    private static Vector3 GetSpawnPointAbove(Transform root)
    {
        if (root == null) return new Vector3(0f, 20f, 0f);
        if (TryGetRendererBounds(root, out Bounds rendererBounds))
        {
            return new Vector3(rendererBounds.center.x, rendererBounds.max.y + 4f, rendererBounds.center.z);
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        bool foundCollider = false;
        Bounds colliderBounds = default;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger) continue;
            if (!foundCollider)
            {
                colliderBounds = collider.bounds;
                foundCollider = true;
            }
            else
            {
                colliderBounds.Encapsulate(collider.bounds);
            }
        }

        if (foundCollider)
        {
            return new Vector3(colliderBounds.center.x, colliderBounds.max.y + 4f, colliderBounds.center.z);
        }

        return root.position + Vector3.up * 20f;
    }
    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void TrySetTag(GameObject target, string tag)
    {
        try { target.tag = tag; } catch (UnityException) { }
    }
}


















































