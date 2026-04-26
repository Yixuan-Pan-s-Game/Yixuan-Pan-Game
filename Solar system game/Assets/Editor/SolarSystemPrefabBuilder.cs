using System.IO;
using UnityEditor;
using UnityEngine;

public static class SolarSystemPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string MaterialFolder = "Assets/Materials";

    [MenuItem("Tools/Children Solar System/Create Prefabs And Scene")]
    public static void CreatePrefabsAndScene()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        Material sunMaterial = GetOrCreateMaterial("SunMat", new Color(1f, 0.42f, 0.08f), true);
        Material earthMaterial = GetOrCreateMaterial("EarthMat", new Color(0.25f, 0.65f, 1f), false);
        Material moonMaterial = GetOrCreateMaterial("MoonMat", new Color(0.78f, 0.78f, 0.73f), false);

        GameObject sunPrefab = CreateBodyPrefab("Sun", 2.1f, sunMaterial, false);
        GameObject earthPrefab = CreateBodyPrefab("Earth", 1f, earthMaterial, true);
        GameObject moonPrefab = CreateBodyPrefab("Moon", 0.32f, moonMaterial, true);

        PlacePrefabInScene(sunPrefab, Vector3.zero);
        PlacePrefabInScene(earthPrefab, new Vector3(5.8f, 0f, 0f));
        PlacePrefabInScene(moonPrefab, new Vector3(7.55f, 0f, 0f));

        EditorUtility.DisplayDialog(
            "Children Solar System",
            "Sun, Earth, and Moon prefabs are ready. Press Play to explore, click Earth or Moon, and press R to return.",
            "OK");
    }

    private static GameObject CreateBodyPrefab(string bodyName, float diameter, Material material, bool clickable)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = bodyName;
        body.transform.localScale = Vector3.one * diameter;
        body.GetComponent<Renderer>().sharedMaterial = material;

        if (body.GetComponent<CelestialBodyMotion>() == null)
        {
            body.AddComponent<CelestialBodyMotion>();
        }

        if (clickable && body.GetComponent<ClickableCelestialBody>() == null)
        {
            ClickableCelestialBody clickableBody = body.AddComponent<ClickableCelestialBody>();
            if (bodyName == "Earth")
            {
                clickableBody.Configure("地球 Earth", "地球是我们的家，它有海洋、空气和许多生命。", 3.1f);
            }
            else
            {
                clickableBody.Configure("月球 Moon", "月球围着地球转。晚上看到的月亮，就是它反射太阳光。", 1.4f);
            }
        }

        string path = $"{PrefabFolder}/{bodyName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(body, path);
        Object.DestroyImmediate(body);
        return prefab;
    }

    private static void PlacePrefabInScene(GameObject prefab, Vector3 position)
    {
        GameObject existing = GameObject.Find(prefab.name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = prefab.name;
        instance.transform.position = position;
        Undo.RegisterCreatedObjectUndo(instance, $"Create {prefab.name}");
    }

    private static Material GetOrCreateMaterial(string materialName, Color color, bool emissive)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            material = new Material(shader != null ? shader : Shader.Find("Diffuse"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.2f);
        }

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string child = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
