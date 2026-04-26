using UnityEngine;

/// <summary>
/// Attach to Earth, Moon, or any clickable celestial body with a Collider.
/// On click it zooms the camera, shows a friendly fact, and plays a short beep.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClickableCelestialBody : MonoBehaviour
{
    [SerializeField] private string displayName = "Planet";
    [TextArea(2, 4)]
    [SerializeField] private string childFriendlyFact = "This world has something amazing to discover!";
    [SerializeField] private float cameraDistance = 3f;

    private SolarSystemCameraController cameraController;
    private SolarSystemGameUI gameUI;
    private SolarSystemAudioFeedback audioFeedback;

    public string DisplayName => displayName;
    public string ChildFriendlyFact => childFriendlyFact;

    private void Awake()
    {
        cameraController = FindObjectOfType<SolarSystemCameraController>();
        gameUI = FindObjectOfType<SolarSystemGameUI>();
        audioFeedback = FindObjectOfType<SolarSystemAudioFeedback>();
    }

    private void OnMouseDown()
    {
        cameraController = cameraController != null ? cameraController : FindObjectOfType<SolarSystemCameraController>();
        gameUI = gameUI != null ? gameUI : FindObjectOfType<SolarSystemGameUI>();
        audioFeedback = audioFeedback != null ? audioFeedback : FindObjectOfType<SolarSystemAudioFeedback>();

        if (cameraController != null)
        {
            cameraController.FocusOn(transform, cameraDistance);
        }

        if (gameUI != null)
        {
            gameUI.ShowFact(displayName, childFriendlyFact);
        }

        if (audioFeedback != null)
        {
            audioFeedback.PlayBeep();
        }
    }

    public void Configure(string newDisplayName, string newFact, float newCameraDistance)
    {
        displayName = newDisplayName;
        childFriendlyFact = newFact;
        cameraDistance = newCameraDistance;
    }
}
