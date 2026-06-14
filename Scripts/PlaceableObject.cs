using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public string itemId;
    public string displayName;
    public PlaceableType placeableType;

    private bool doorOpen;
    private Quaternion closedRotation;

    private void Awake()
    {
        closedRotation = transform.rotation;
    }

    public void ToggleDoor()
    {
        if (placeableType != PlaceableType.Door)
        {
            return;
        }

        doorOpen = !doorOpen;
        transform.rotation = doorOpen
            ? closedRotation * Quaternion.Euler(0f, 90f, 0f)
            : closedRotation;
    }
}
