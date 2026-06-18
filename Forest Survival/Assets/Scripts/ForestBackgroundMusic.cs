using UnityEngine;

// forest background music script that owns this feature's runtime behavior.
public class ForestBackgroundMusic : MonoBehaviour
{
    // Current interaction target or gameplay object being processed: musicResourcePath.
    public string musicResourcePath = "happy_adveture";
    // Important runtime data or configuration used by this component: volume.
    public float volume = 0.05f;

    // Important runtime data or configuration used by this component: source.
    private AudioSource source;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        AudioClip clip = Resources.Load<AudioClip>(musicResourcePath);
        if (clip == null)
        {
            Debug.LogWarning("ForestBackgroundMusic: Could not find background music at Resources/" + musicResourcePath + ".");
            return;
        }

        source.clip = clip;
        source.volume = volume;
        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (source != null)
        {
            source.volume = volume;
        }
    }
}
