using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ForestSaveSystem : MonoBehaviour
{
    [Serializable]
    public class SavedSlot
    {
        public string itemId;
        public int stackCount;
        public int durability;
    }

    [Serializable]
    private class SaveData
    {
        public List<SavedSlot> inventory = new List<SavedSlot>();
        public bool[] completedQuests;
        public string exploredMap;
        public Vector3 playerPosition;
        public string sceneName;
    }

    private static ForestSaveSystem instance;
    private static bool isLoadingSave;
    private float nextAutosaveTime;
    private static string SavePath => Path.Combine(Application.persistentDataPath, "forest_survival_save.json");

    public static bool HasSave => File.Exists(SavePath);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindObjectOfType<ForestSaveSystem>() == null)
        {
            new GameObject("ForestSaveSystem").AddComponent<ForestSaveSystem>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        nextAutosaveTime = Time.unscaledTime + 12f;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextAutosaveTime)
        {
            nextAutosaveTime = Time.unscaledTime + 12f;
            if (!ForestMenuUI.IsMenuOpen)
            {
                SaveGame();
            }
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }

    public static void SaveGame()
    {
        if (instance == null || isLoadingSave)
        {
            return;
        }

        PlayerToolController player = FindObjectOfType<PlayerToolController>();
        if (player == null)
        {
            return;
        }

        SaveData data = new SaveData
        {
            playerPosition = player.transform.position,
            sceneName = SceneManager.GetActiveScene().name
        };

        ToolSlot[] slots = player.Slots;
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                ToolSlot slot = slots[i];
                if (slot == null || slot.stackCount <= 0)
                {
                    continue;
                }

                data.inventory.Add(new SavedSlot
                {
                    itemId = InventoryUtility.GetItemId(slot),
                    stackCount = slot.stackCount,
                    durability = slot.durability
                });
            }
        }

        ForestQuestSystem quests = FindObjectOfType<ForestQuestSystem>();
        ForestMapUI map = FindObjectOfType<ForestMapUI>();
        data.completedQuests = quests != null ? quests.GetCompletedSnapshot() : null;
        data.exploredMap = map != null ? map.GetExploredSnapshot() : string.Empty;
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public static bool LoadGame()
    {
        if (!File.Exists(SavePath) || instance == null)
        {
            return false;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return false;
            }

            instance.StartCoroutine(instance.LoadGameRoutine(data));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("ForestSaveSystem: Failed to load save. " + exception.Message);
            isLoadingSave = false;
            return false;
        }
    }

    private IEnumerator LoadGameRoutine(SaveData data)
    {
        isLoadingSave = true;
        string targetScene = string.IsNullOrEmpty(data.sceneName) ? SceneManager.GetActiveScene().name : data.sceneName;
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        Scene loadedScene = SceneManager.GetSceneByName(targetScene);
        if (loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        yield return null;
        RestoreLoadedData(data);
        isLoadingSave = false;
    }

    private static void RestoreLoadedData(SaveData data)
    {
        PlayerToolController player = FindObjectOfType<PlayerToolController>();
        if (data == null || player == null)
        {
            return;
        }

        player.RestoreSavedInventory(data.inventory);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.TeleportToRespawn(data.playerPosition, player.transform.rotation);
        }
        else
        {
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = data.playerPosition;
            }

            player.transform.position = data.playerPosition;
            Physics.SyncTransforms();
        }

        ForestQuestSystem quests = FindObjectOfType<ForestQuestSystem>();
        ForestMapUI map = FindObjectOfType<ForestMapUI>();
        if (quests != null) quests.RestoreCompleted(data.completedQuests);
        if (map != null) map.RestoreExplored(data.exploredMap);
    }
}

