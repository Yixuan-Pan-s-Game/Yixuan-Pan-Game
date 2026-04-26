using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SolarSystemAudioFeedback : MonoBehaviour
{
    [SerializeField] private float beepFrequency = 880f;
    [SerializeField] private float beepDuration = 0.12f;
    [SerializeField] private float beepVolume = 0.35f;

    private AudioSource audioSource;
    private AudioClip beepClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        beepClip = CreateSineBeep(beepFrequency, beepDuration, beepVolume);
    }

    public void PlayBeep()
    {
        if (beepClip != null)
        {
            audioSource.PlayOneShot(beepClip);
        }
    }

    private static AudioClip CreateSineBeep(float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * Mathf.Max(0.02f, duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create("FriendlyBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
