using UnityEngine;

/// <summary>
/// Automatically prepares a playable child-friendly solar system scene.
/// Replace the generated textures/materials with real open data if desired:
/// - NASA image/texture assets: follow NASA media usage guidelines and credit the source.
/// - Solar System Scope textures: many are CC BY 4.0; keep attribution and license notes.
/// Put replacement materials on Sun/Earth/Moon prefabs or scene objects with the same names.
/// </summary>
public class SolarSystemSceneBootstrap : MonoBehaviour
{
    private const string BootstrapName = "Interactive Solar System Bootstrap";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnSceneLoad()
    {
        if (FindObjectOfType<SolarSystemSceneBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject(BootstrapName);
        bootstrap.AddComponent<SolarSystemSceneBootstrap>();
    }

    private void Start()
    {
        Camera mainCamera = EnsureCamera();
        EnsureManagers(mainCamera);
        BuildSolarSystem();
    }

    private Camera EnsureCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.GetComponent<Camera>();
        }

        mainCamera.transform.position = new Vector3(0f, 4.2f, -12f);
        mainCamera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
        mainCamera.fieldOfView = 55f;

        if (mainCamera.GetComponent<SolarSystemCameraController>() == null)
        {
            mainCamera.gameObject.AddComponent<SolarSystemCameraController>();
        }

        return mainCamera;
    }

    private void EnsureManagers(Camera mainCamera)
    {
        if (FindObjectOfType<SolarSystemAudioFeedback>() == null)
        {
            GameObject audioObject = new GameObject("Friendly Audio", typeof(AudioSource), typeof(SolarSystemAudioFeedback));
            audioObject.transform.SetParent(transform);
        }

        if (FindObjectOfType<SolarSystemGameUI>() == null)
        {
            GameObject uiObject = new GameObject("Solar System Game UI", typeof(SolarSystemGameUI));
            uiObject.transform.SetParent(transform);
        }

        RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.22f);
        mainCamera.backgroundColor = new Color(0.01f, 0.02f, 0.07f);
    }

    private void BuildSolarSystem()
    {
        GameObject solarSystemCenter = GameObject.Find("Solar System Center");
        if (solarSystemCenter == null)
        {
            solarSystemCenter = new GameObject("Solar System Center");
            solarSystemCenter.transform.position = new Vector3(-0.1f, 0f, 0f);
        }

        GameObject sun = FindOrCreateSphere("Sun", Vector3.zero, 2.1f, CreateSunMaterial());
        ConfigureMotion(sun, solarSystemCenter.transform, 4f, 0.35f);
        EnsureLight(sun.transform);

        GameObject earth = FindOrCreateSphere("Earth", new Vector3(5.8f, 0f, 0f), 1f, CreateEarthMaterial());
        ConfigureMotion(earth, sun.transform, 28f, 8f);
        ConfigureClickable(
            earth,
            "地球 Earth",
            "地球是我们的家，它有海洋、空气和许多生命。",
            3.1f);

        GameObject moon = FindOrCreateSphere("Moon", earth.transform.position + new Vector3(1.75f, 0f, 0f), 0.32f, CreateMoonMaterial());
        ConfigureMotion(moon, earth.transform, 16f, 42f);
        ConfigureClickable(
            moon,
            "月球 Moon",
            "月球围着地球转。晚上看到的月亮，就是它反射太阳光。",
            1.4f);
    }

    private GameObject FindOrCreateSphere(string objectName, Vector3 position, float diameter, Material material)
    {
        GameObject body = GameObject.Find(objectName);
        if (body == null)
        {
            body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = objectName;
        }

        body.transform.position = position;
        body.transform.localScale = Vector3.one * diameter;

        Renderer renderer = body.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        if (body.GetComponent<Collider>() == null)
        {
            body.AddComponent<SphereCollider>();
        }

        return body;
    }

    private void ConfigureMotion(GameObject body, Transform orbitCenter, float spinSpeed, float orbitSpeed)
    {
        CelestialBodyMotion motion = body.GetComponent<CelestialBodyMotion>();
        if (motion == null)
        {
            motion = body.AddComponent<CelestialBodyMotion>();
        }

        motion.ConfigureRotation(spinSpeed, Vector3.up);
        motion.ConfigureOrbit(orbitCenter, orbitSpeed, Vector3.up);
    }

    private void ConfigureClickable(GameObject body, string displayName, string fact, float cameraDistance)
    {
        ClickableCelestialBody clickable = body.GetComponent<ClickableCelestialBody>();
        if (clickable == null)
        {
            clickable = body.AddComponent<ClickableCelestialBody>();
        }

        clickable.Configure(displayName, fact, cameraDistance);
    }

    private void EnsureLight(Transform sun)
    {
        Light existingLight = FindObjectOfType<Light>();
        if (existingLight != null)
        {
            existingLight.transform.position = sun.position;
            existingLight.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            existingLight.type = LightType.Directional;
            existingLight.intensity = 1.25f;
            return;
        }

        GameObject lightObject = new GameObject("Sun Light", typeof(Light));
        lightObject.transform.position = sun.position;
        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
    }

    private Material CreateSunMaterial()
    {
        Material material = CreateStandardMaterial("Generated Sun Material", new Color(1f, 0.55f, 0.12f));
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.38f, 0.05f) * 1.2f);
        material.mainTexture = CreateGradientTexture(
            "Generated Sun Gradient",
            new Color(1f, 0.92f, 0.22f),
            new Color(1f, 0.28f, 0.05f));
        return material;
    }

    private Material CreateEarthMaterial()
    {
        Material material = CreateStandardMaterial("Generated Earth Material", new Color(0.25f, 0.65f, 1f));
        material.mainTexture = CreateCheckerTexture(
            "Generated Earth Ocean Land Checker",
            new Color(0.12f, 0.48f, 0.92f),
            new Color(0.25f, 0.74f, 0.35f),
            16);
        return material;
    }

    private Material CreateMoonMaterial()
    {
        Material material = CreateStandardMaterial("Generated Moon Material", new Color(0.76f, 0.77f, 0.72f));
        material.mainTexture = CreateCheckerTexture(
            "Generated Moon Checker",
            new Color(0.62f, 0.63f, 0.62f),
            new Color(0.86f, 0.86f, 0.8f),
            10);
        return material;
    }

    private Material CreateStandardMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader != null ? shader : Shader.Find("Diffuse"));
        material.name = materialName;
        material.color = color;
        return material;
    }

    private Texture2D CreateGradientTexture(string textureName, Color top, Color bottom)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size);
        texture.name = textureName;

        for (int y = 0; y < size; y++)
        {
            Color rowColor = Color.Lerp(bottom, top, y / (float)(size - 1));
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, rowColor);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateCheckerTexture(string textureName, Color a, Color b, int cells)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size);
        texture.name = textureName;

        int cellSize = Mathf.Max(1, size / Mathf.Max(1, cells));
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool useA = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                texture.SetPixel(x, y, useA ? a : b);
            }
        }

        texture.Apply();
        return texture;
    }
}
