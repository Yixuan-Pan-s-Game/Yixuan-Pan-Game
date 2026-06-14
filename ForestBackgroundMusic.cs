using UnityEngine;

public class ForestBackgroundMusic : MonoBehaviour
{
    public string musicResourcePath = "happy_adveture";
    public float volume = 0.42f;

    private AudioSource source;

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
}
