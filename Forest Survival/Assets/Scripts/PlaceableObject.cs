using UnityEngine;

// placeable object script that owns this feature's runtime behavior.
public class PlaceableObject : MonoBehaviour
{
    // Inventory or crafting data for items, recipes, slots, or stack counts: itemId.
    public string itemId;
    // Runtime flag that drives control flow, UI state, or gameplay availability: displayName.
    public string displayName;
    // Identifier or category used for lookup, routing, or state selection: placeableType.
    public PlaceableType placeableType;

    // Runtime flag that drives control flow, UI state, or gameplay availability: doorOpen.
    private bool doorOpen;
    // Spatial value used for positioning, rotation, scale, or collision math: closedRotation.
    private Quaternion closedRotation;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        closedRotation = transform.rotation;
    }

    // Sets state, selection, or placement data for toggle door.
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
