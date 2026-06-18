using UnityEngine;

// Unity editor utility for generating, connecting, or configuring project assets.
public class SimpleCharacterBuilder : MonoBehaviour
{
    // Layer or mask filter used by physics queries or rendering: playerMaterial.
    public Material playerMaterial;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        if (transform.Find("CharacterVisual") != null)
        {
            return;
        }

        Build();
    }

    [ContextMenu("Rebuild Character")]
    // Creates or rebuilds the runtime objects, assets, or UI for build.
    public void Build()
    {
        Transform existingRoot = transform.Find("CharacterVisual");
        if (existingRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }
        }

        Transform root = new GameObject("CharacterVisual").transform;
        root.SetParent(transform, false);

        CreatePart(root, PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), new Vector3(0.65f, 0.85f, 0.5f));
        CreatePart(root, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.95f, 0f), new Vector3(0.45f, 0.45f, 0.45f));
        CreatePart(root, PrimitiveType.Cylinder, "Arm_L", new Vector3(-0.45f, 1.18f, 0f), new Vector3(0.12f, 0.45f, 0.12f), new Vector3(0f, 0f, 20f));
        CreatePart(root, PrimitiveType.Cylinder, "Arm_R", new Vector3(0.45f, 1.18f, 0f), new Vector3(0.12f, 0.45f, 0.12f), new Vector3(0f, 0f, -20f));
        CreatePart(root, PrimitiveType.Cylinder, "Leg_L", new Vector3(-0.18f, 0.42f, 0f), new Vector3(0.14f, 0.42f, 0.14f));
        CreatePart(root, PrimitiveType.Cylinder, "Leg_R", new Vector3(0.18f, 0.42f, 0f), new Vector3(0.14f, 0.42f, 0.14f));
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create part.
    private void CreatePart(Transform parent, PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale)
    {
        CreatePart(parent, type, partName, localPosition, localScale, Vector3.zero);
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create part.
    private void CreatePart(Transform parent, PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localEulerAngles = localEulerAngles;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        if (playerMaterial != null)
        {
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = playerMaterial;
            }
        }
    }
}


