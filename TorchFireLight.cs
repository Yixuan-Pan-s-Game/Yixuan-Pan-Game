using UnityEngine;

public class TorchFireLight : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.05f, 0f);
    [SerializeField] private float lightRange = 7f;
    [SerializeField] private float lightIntensity = 2.2f;
    [SerializeField] private Color lightColor = new Color(1f, 0.58f, 0.22f);

    private Light torchLight;
    private ParticleSystem flame;
    private float flickerSeed;

    public Vector3 LocalOffset
    {
        get { return localOffset; }
        set
        {
            localOffset = value;
            UpdateFireAnchor();
        }
    }

    private void Awake()
    {
        flickerSeed = Random.value * 100f;
        EnsureFireObjects();
        UpdateFireAnchor();
    }

    private void OnEnable()
    {
        EnsureFireObjects();
        UpdateFireAnchor();
    }

    private void LateUpdate()
    {
        if (torchLight == null)
        {
            return;
        }

        float flicker = Mathf.PerlinNoise(Time.time * 5.5f, flickerSeed);
        torchLight.intensity = lightIntensity * Mathf.Lerp(0.82f, 1.18f, flicker);
        torchLight.range = lightRange * Mathf.Lerp(0.92f, 1.08f, flicker);
    }

    private void EnsureFireObjects()
    {
        Transform anchor = transform.Find("TorchFireAnchor");
        if (anchor == null)
        {
            GameObject anchorObject = new GameObject("TorchFireAnchor");
            anchor = anchorObject.transform;
            anchor.SetParent(transform, false);
        }

        torchLight = anchor.GetComponent<Light>();
        if (torchLight == null)
        {
            torchLight = anchor.gameObject.AddComponent<Light>();
        }

        torchLight.type = LightType.Point;
        torchLight.color = lightColor;
        torchLight.range = lightRange;
        torchLight.intensity = lightIntensity;
        torchLight.shadows = LightShadows.None;

        flame = anchor.GetComponentInChildren<ParticleSystem>(true);
        if (flame == null)
        {
            GameObject flameObject = new GameObject("TorchFlame");
            flameObject.transform.SetParent(anchor, false);
            flame = flameObject.AddComponent<ParticleSystem>();
            ConfigureFlame(flame);
        }
    }

    private void ConfigureFlame(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.2f, 0.9f), new Color(1f, 0.18f, 0.03f, 0.45f));
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.035f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.82f, 0.25f), 0f),
                new GradientColorKey(new Color(1f, 0.32f, 0.04f), 0.55f),
                new GradientColorKey(new Color(0.22f, 0.08f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.55f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                renderer.sharedMaterial = new Material(shader);
            }
        }

        particles.Play();
    }

    private void UpdateFireAnchor()
    {
        Transform anchor = transform.Find("TorchFireAnchor");
        if (anchor != null)
        {
            anchor.localPosition = localOffset;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
        }
    }
}
