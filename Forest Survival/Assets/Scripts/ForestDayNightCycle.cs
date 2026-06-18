using UnityEngine;

// forest day night cycle script that owns this feature's runtime behavior.
public class ForestDayNightCycle : MonoBehaviour
{
    // Timing value or timestamp used for cooldowns, delays, or progress checks: FullCycleSeconds.
    private const float FullCycleSeconds = 7200f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: StartTimeOfDay.
    private const float StartTimeOfDay = 0.3f;

    // Timing value or timestamp used for cooldowns, delays, or progress checks: timeOfDay.
    [SerializeField] private float timeOfDay = StartTimeOfDay;
    // Cached component or scene reference to avoid repeated lookups: dayLightIntensity.
    [SerializeField] private float dayLightIntensity = 1.05f;
    // Cached component or scene reference to avoid repeated lookups: nightLightIntensity.
    [SerializeField] private float nightLightIntensity = 0.045f;
    [SerializeField] private Color dayAmbientColor = new Color(0.66f, 0.72f, 0.78f);
    [SerializeField] private Color nightAmbientColor = new Color(0.035f, 0.05f, 0.09f);

    // Important runtime data or configuration used by this component: sun.
    private Light sun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Ensures the objects, references, or configuration required for ensure cycle exists exist.
    private static void EnsureCycleExists()
    {
        if (FindObjectOfType<ForestDayNightCycle>() == null)
        {
            new GameObject("ForestDayNightCycle").AddComponent<ForestDayNightCycle>();
        }
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        FindPrimarySunAndDisableDuplicates();
        ApplyLighting();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (sun == null)
        {
            FindPrimarySunAndDisableDuplicates();
        }

        timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / FullCycleSeconds, 1f);
        ApplyLighting();
    }

    // Finds, loads, or caches the references needed for find primary sun and disable duplicates.
    private void FindPrimarySunAndDisableDuplicates()
    {
        Light[] lights = FindObjectsOfType<Light>(true);
        Light firstActiveDirectional = null;
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || light.type != LightType.Directional)
            {
                continue;
            }

            if (firstActiveDirectional == null && light.gameObject.activeInHierarchy && light.enabled)
            {
                firstActiveDirectional = light;
            }
        }

        sun = firstActiveDirectional;
        if (sun == null)
        {
            GameObject sunObject = new GameObject("Runtime Sun");
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light != null && light != sun && light.type == LightType.Directional)
            {
                light.enabled = false;
            }
        }

        RenderSettings.sun = sun;
    }

    // Refreshes and applies configuration or runtime state for apply lighting.
    private void ApplyLighting()
    {
        if (sun == null)
        {
            return;
        }

        float sunHeight = Mathf.Sin(timeOfDay * Mathf.PI * 2f);
        float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.18f, sunHeight));
        sun.transform.rotation = Quaternion.Euler((timeOfDay * 360f) - 90f, 25f, 0f);
        sun.intensity = Mathf.Lerp(nightLightIntensity, dayLightIntensity, daylight);
        sun.color = Color.Lerp(new Color(0.42f, 0.5f, 0.72f), Color.white, daylight);
        sun.shadows = daylight > 0.06f ? LightShadows.Soft : LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, daylight);
    }
}
